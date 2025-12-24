using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedRemind.Core.Interfaces;

namespace MedRemind.Mobile.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly IConfigurationService _configService;
    private readonly IBiometricService _biometricService;
    private readonly IAuthenticationService _authService;

    [ObservableProperty]
    private string _userName = "User";

    [ObservableProperty]
    private string _phoneNumber = string.Empty;

    [ObservableProperty]
    private bool _notificationsEnabled = true;

    [ObservableProperty]
    private bool _soundEnabled = true;

    [ObservableProperty]
    private bool _vibrationEnabled = true;

    [ObservableProperty]
    private bool _biometricEnabled;

    [ObservableProperty]
    private bool _biometricAvailable;

    [ObservableProperty]
    private string _biometricType = "Biometric";

    [ObservableProperty]
    private string _appVersion = "1.0.0";

    public SettingsViewModel(
        IConfigurationService configService,
        IBiometricService biometricService,
        IAuthenticationService authService)
    {
        _configService = configService;
        _biometricService = biometricService;
        _authService = authService;
        Title = "Settings";
    }

    public async void OnAppearing()
    {
        await LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            // Load user info
            PhoneNumber = await SecureStorage.GetAsync("phone_number") ?? "Not set";
            var storedName = await SecureStorage.GetAsync("user_name");
            if (!string.IsNullOrEmpty(storedName))
            {
                UserName = storedName;
            }

            // Load biometric settings
            BiometricAvailable = await _biometricService.IsBiometricAvailableAsync();
            BiometricEnabled = await _biometricService.IsBiometricEnabledAsync();
            BiometricType = await _biometricService.GetBiometricTypeAsync();

            // Load notification settings
            var notifEnabled = await SecureStorage.GetAsync("notifications_enabled");
            NotificationsEnabled = notifEnabled != "false";

            var sound = await SecureStorage.GetAsync("sound_enabled");
            SoundEnabled = sound != "false";

            var vibration = await SecureStorage.GetAsync("vibration_enabled");
            VibrationEnabled = vibration != "false";

            System.Diagnostics.Debug.WriteLine($"?? Settings loaded - Biometric: {BiometricEnabled}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error loading settings: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ToggleBiometricAsync()
    {
        await ExecuteAsync(async () =>
        {
            if (!BiometricAvailable)
            {
                ShowError("Biometric authentication is not available on this device");
                BiometricEnabled = false;
                return;
            }

            if (BiometricEnabled)
            {
                // User wants to enable biometric
                var result = await _biometricService.EnableBiometricAsync();
                
                if (!result)
                {
                    BiometricEnabled = false;
                    ShowError("Failed to enable biometric authentication. Please try again.");
                }
                else
                {
                    var page = GetCurrentPage();
                    if (page != null)
                    {
                        await page.DisplayAlertAsync(
                            "Success",
                            $"{BiometricType} authentication enabled! You can now use {BiometricType} to login.",
                            "OK"
                        );
                    }
                    System.Diagnostics.Debug.WriteLine($"? Biometric enabled in settings");
                }
            }
            else
            {
                // User wants to disable biometric
                var page = GetCurrentPage();
                if (page != null)
                {
                    var confirm = await page.DisplayAlertAsync(
                        "Disable Biometric?",
                        "You will need to use OTP for login. Are you sure?",
                        "Yes, Disable",
                        "Cancel"
                    );

                    if (confirm)
                    {
                        await _biometricService.DisableBiometricAsync();
                        System.Diagnostics.Debug.WriteLine($"?? Biometric disabled in settings");
                        
                        await page.DisplayAlertAsync(
                            "Disabled",
                            "Biometric authentication has been disabled.",
                            "OK"
                        );
                    }
                    else
                    {
                        BiometricEnabled = true; // Revert toggle
                    }
                }
            }
        });
    }

    [RelayCommand]
    private async Task SaveNotificationSettingsAsync()
    {
        await ExecuteAsync(async () =>
        {
            await SecureStorage.SetAsync("notifications_enabled", NotificationsEnabled.ToString().ToLower());
            await SecureStorage.SetAsync("sound_enabled", SoundEnabled.ToString().ToLower());
            await SecureStorage.SetAsync("vibration_enabled", VibrationEnabled.ToString().ToLower());

            var page = GetCurrentPage();
            if (page != null)
            {
                await page.DisplayAlertAsync("Success", "Notification settings saved", "OK");
            }

            System.Diagnostics.Debug.WriteLine("? Notification settings saved");
        });
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await ExecuteAsync(async () =>
        {
            var page = GetCurrentPage();
            if (page != null)
            {
                var confirm = await page.DisplayAlertAsync(
                    "Logout",
                    "Are you sure you want to logout?",
                    "Yes",
                    "Cancel"
                );

                if (confirm)
                {
                    // Clear all stored data
                    await SecureStorage.SetAsync("user_id", string.Empty);
                    await SecureStorage.SetAsync("session_token", string.Empty);
                    await SecureStorage.SetAsync("phone_number", string.Empty);
                    
                    // Navigate to login
                    await Shell.Current.GoToAsync("///LoginPage");
                    
                    System.Diagnostics.Debug.WriteLine("?? User logged out");
                }
            }
        });
    }

    [RelayCommand]
    private async Task EditProfileAsync()
    {
        var page = GetCurrentPage();
        if (page != null)
        {
            var name = await page.DisplayPromptAsync(
                "Edit Name",
                "Enter your name:",
                initialValue: UserName,
                maxLength: 50
            );

            if (!string.IsNullOrWhiteSpace(name))
            {
                UserName = name;
                await SecureStorage.SetAsync("user_name", name);
                
                await page.DisplayAlertAsync("Success", "Name updated successfully", "OK");
            }
        }
    }
}
