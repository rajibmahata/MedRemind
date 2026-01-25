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
}
