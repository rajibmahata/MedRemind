using MedRemind.Core.DTOs;

namespace MedRemind.Core.Interfaces;

/// <summary>
/// Service for authentication and OTP operations
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Send OTP to phone number
    /// </summary>
    Task<(bool Success, string? ErrorMessage)> SendOtpAsync(string phoneNumber, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Verify OTP and generate JWT token
    /// </summary>
    Task<(bool Success, string? Token, string? ErrorMessage)> VerifyOtpAsync(string phoneNumber, string otp, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generate session token for user
    /// </summary>
    Task<string> GenerateSessionTokenAsync(int userId);
    
    /// <summary>
    /// Validate session token
    /// </summary>
    Task<bool> ValidateSessionTokenAsync(string token);

    /// <summary>
    /// Login with email/phone and password
    /// </summary>
    Task<LoginResponse> LoginWithPasswordAsync(LoginRequest request);

    /// <summary>
    /// Initiate forgot password - send reset email
    /// </summary>
    Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request);

    /// <summary>
    /// Reset password with token
    /// </summary>
    Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request);

    /// <summary>
    /// Change password for authenticated user
    /// </summary>
    Task<ChangePasswordResponse> ChangePasswordAsync(int userId, ChangePasswordRequest request);

    /// <summary>
    /// Resend OTP with rate limiting
    /// </summary>
    Task<ResendOtpResponse> ResendOtpAsync(ResendOtpRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if resend OTP is available
    /// </summary>
    Task<ResendOtpResponse> CheckResendAvailabilityAsync(string phoneNumber, string purpose = "Registration");
}

