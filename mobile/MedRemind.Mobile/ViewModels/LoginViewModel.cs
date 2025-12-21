using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedRemind.Core.Interfaces;

namespace MedRemind.Mobile.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthenticationService _authService;

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

    public LoginViewModel(IAuthenticationService authService)
    {
        _authService = authService;
        Title = "Welcome to MedRemind";
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
            var result = await _authService.VerifyOtpAsync(PhoneNumber, Otp);
            
            if (result.Success)
            {
                await Shell.Current.GoToAsync("///HomePage");
            }
            else
            {
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
