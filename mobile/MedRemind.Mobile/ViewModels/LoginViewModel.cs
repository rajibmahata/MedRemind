using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedRemind.Core.Interfaces;

namespace MedRemind.Mobile.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthenticationService _authService;
    private readonly IBiometricService _biometricService;

    [ObservableProperty]
    private string _phoneNumber = string.Empty;

    [ObservableProperty]
    private string _otp = string.Empty;

    [ObservableProperty]
    private bool _isOtpSent;

    [ObservableProperty]
    private bool _isOtpVisible;

    [ObservableProperty]
    private string _buttonText = "Send OTP";

    [ObservableProperty]
    private bool _biometricAvailable;

    [ObservableProperty]
    private bool _showBiometricButton;

    [ObservableProperty]
    private string _biometricButtonText = "Login with Biometric";

    public LoginViewModel(
        IAuthenticationService authService,
        IBiometricService biometricService)
    {
        _authService = authService;
        _biometricService = biometricService;
        Title = "Welcome to MedRemind";
    }

    public async void OnAppearing()
    {
        await CheckBiometricAvailabilityAsync();
    }

    private async Task CheckBiometricAvailabilityAsync()
    {
        try
        {
            BiometricAvailable = await _biometricService.IsBiometricAvailableAsync();
            var biometricEnabled = await _biometricService.IsBiometricEnabledAsync();
            var hasStoredPhone = !string.IsNullOrEmpty(await SecureStorage.GetAsync("phone_number"));

            ShowBiometricButton = BiometricAvailable && biometricEnabled && hasStoredPhone;

            if (ShowBiometricButton)
            {
                var biometricType = await _biometricService.GetBiometricTypeAsync();
                BiometricButtonText = $"?? Login with {biometricType}";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking biometric: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task BiometricLoginAsync()
    {
        await ExecuteAsync(async () =>
        {
            var result = await _biometricService.AuthenticateAsync("Login to MedRemind");

            if (result.Success)
            {
                // Biometric authentication successful, navigate to home
                await Shell.Current.GoToAsync("///HomePage");
            }
            else
            {
                ShowError(result.ErrorMessage ?? "Biometric authentication failed");
            }
        });
    }

    [RelayCommand]
    private async Task SendOtpAsync()
    {
        if (string.IsNullOrWhiteSpace(PhoneNumber) || PhoneNumber.Length != 10)
        {
            ShowError("Please enter a valid 10-digit phone number");
            return;
        }

        await ExecuteAsync(async () =>
        {
            var result = await _authService.SendOtpAsync(PhoneNumber);
            
            if (result.Success)
            {
                IsOtpSent = true;
                IsOtpVisible = true;
                ButtonText = "Verify OTP";
                
                var page = GetCurrentPage();
                if (page != null)
                {
                    await page.DisplayAlertAsync(
                        "Success", 
                        "OTP sent successfully to your phone", 
                        "OK");
                }
            }
            else
            {
                ShowError(result.ErrorMessage ?? "Failed to send OTP");
            }
        });
    }

    [RelayCommand]
    private async Task VerifyOtpAsync()
    {
        if (!IsOtpSent)
        {
            await SendOtpAsync();
            return;
        }

        if (string.IsNullOrWhiteSpace(Otp) || Otp.Length != 6)
        {
            ShowError("Please enter a valid 6-digit OTP");
            return;
        }

        await ExecuteAsync(async () =>
        {
            System.Diagnostics.Debug.WriteLine($"?? Login: Verifying OTP for {PhoneNumber}...");
            
            var result = await _authService.VerifyOtpAsync(PhoneNumber, Otp);
            
            if (result.Success)
            {
                System.Diagnostics.Debug.WriteLine($"? Login: OTP verified successfully");
                
                // Verify session was stored
                var storedUserId = await SecureStorage.GetAsync("user_id");
                var storedSession = await SecureStorage.GetAsync("session_token");
                var storedPhone = await SecureStorage.GetAsync("phone_number");
                
                System.Diagnostics.Debug.WriteLine($"?? Login: Session verification:");
                System.Diagnostics.Debug.WriteLine($"   User ID stored: {storedUserId ?? "NULL"}");
                System.Diagnostics.Debug.WriteLine($"   Session stored: {(string.IsNullOrEmpty(storedSession) ? "NULL" : "EXISTS")}");
                System.Diagnostics.Debug.WriteLine($"   Phone stored: {storedPhone ?? "NULL"}");
                
                if (string.IsNullOrEmpty(storedUserId))
                {
                    System.Diagnostics.Debug.WriteLine($"? Login: Session not stored properly!");
                    ShowError("Login succeeded but session storage failed. Please try again.");
                    return;
                }
                
                // Store phone number for biometric login (redundant but safe)
                await SecureStorage.SetAsync("phone_number", PhoneNumber);

                // Ask user if they want to enable biometric
                if (BiometricAvailable && !await _biometricService.IsBiometricEnabledAsync())
                {
                    var page = GetCurrentPage();
                    if (page != null)
                    {
                        var enableBiometric = await page.DisplayAlertAsync(
                            "Enable Biometric Login?",
                            "Would you like to enable fingerprint/face authentication for faster login?",
                            "Yes, Enable",
                            "Not Now");

                        if (enableBiometric)
                        {
                            await _biometricService.EnableBiometricAsync();
                            await page.DisplayAlertAsync(
                                "Success",
                                "Biometric authentication enabled! You can now login using your fingerprint/face.",
                                "OK");
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine("? Login successful - navigating to HomePage");
                await Shell.Current.GoToAsync("///HomePage");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"? Login: OTP verification failed: {result.ErrorMessage}");
                ShowError(result.ErrorMessage ?? "Invalid OTP. Please try again.");
            }
        });
    }

    [RelayCommand]
    private void ResendOtp()
    {
        Otp = string.Empty;
        IsOtpSent = false;
        IsOtpVisible = false;
        ButtonText = "Send OTP";
    }
}
