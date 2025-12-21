using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedRemind.Core.Interfaces;

namespace MedRemind.Mobile.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly ISecureStorageService _secureStorage;

    [ObservableProperty]
    private string _userName = "User";

    [ObservableProperty]
    private string _userPhone = string.Empty;

    [ObservableProperty]
    private bool _notificationsEnabled = true;

    [ObservableProperty]
    private bool _biometricEnabled = false;

    [ObservableProperty]
    private string _theme = "System";

    [ObservableProperty]
    private int _fontSize = 14;

    [ObservableProperty]
    private string _appVersion = "1.0.0";

    public SettingsViewModel(ISecureStorageService secureStorage)
    {
        _secureStorage = secureStorage;
        Title = "Settings";
    }

    [RelayCommand]
    private async Task LoadSettingsAsync()
    {
        await ExecuteAsync(async () =>
        {
            UserPhone = await _secureStorage.GetAsync("phone_number") ?? "Not set";
            UserName = await _secureStorage.GetAsync("user_name") ?? "User";
        });
    }

    [RelayCommand]
    private async Task UpdateProfileAsync()
    {
        var name = await Application.Current!.MainPage!.DisplayPromptAsync(
            "Update Profile",
            "Enter your name:",
            initialValue: UserName);

        if (!string.IsNullOrWhiteSpace(name))
        {
            UserName = name;
            await _secureStorage.SetAsync("user_name", name);
            
            await Application.Current!.MainPage!.DisplayAlert(
                "Success",
                "Profile updated successfully",
                "OK");
        }
    }

    [RelayCommand]
    private async Task ToggleNotificationsAsync()
    {
        await ExecuteAsync(async () =>
        {
            await _secureStorage.SetAsync("notifications_enabled", NotificationsEnabled.ToString());
            
            await Application.Current!.MainPage!.DisplayAlert(
                "Notifications",
                NotificationsEnabled ? "Notifications enabled" : "Notifications disabled",
                "OK");
        });
    }

    [RelayCommand]
    private async Task ToggleBiometricAsync()
    {
        await ExecuteAsync(async () =>
        {
            await _secureStorage.SetAsync("biometric_enabled", BiometricEnabled.ToString());
            
            await Application.Current!.MainPage!.DisplayAlert(
                "Biometric Authentication",
                BiometricEnabled ? "Biometric enabled" : "Biometric disabled",
                "OK");
        });
    }

    [RelayCommand]
    private async Task ChangeThemeAsync(string theme)
    {
        Theme = theme;
        await _secureStorage.SetAsync("theme", theme);
        
        await Application.Current!.MainPage!.DisplayAlert(
            "Theme",
            $"Theme changed to {theme}",
            "OK");
    }

    [RelayCommand]
    private async Task ViewPrivacyPolicyAsync()
    {
        await Application.Current!.MainPage!.DisplayAlert(
            "Privacy Policy",
            "Your data is stored locally on your device and never shared with third parties.",
            "OK");
    }

    [RelayCommand]
    private async Task ViewTermsAsync()
    {
        await Application.Current!.MainPage!.DisplayAlert(
            "Terms of Service",
            "By using MedRemind, you agree to take your medications as prescribed by your doctor.",
            "OK");
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        var confirm = await Application.Current!.MainPage!.DisplayAlert(
            "Logout",
            "Are you sure you want to logout?",
            "Yes",
            "No");

        if (confirm)
        {
            await _secureStorage.ClearAllAsync();
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }

    public void OnAppearing()
    {
        LoadSettingsCommand.Execute(null);
    }
}
