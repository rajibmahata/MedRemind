namespace MedRemind.Web.Services;

/// <summary>
/// Authentication service for managing user login and session
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Send OTP to user's phone number
    /// </summary>
    Task<bool> SendOtpAsync(string phoneNumber);
    
    /// <summary>
    /// Verify OTP and get authentication token
    /// </summary>
    Task<(bool Success, string? Token, string? ErrorMessage)> VerifyOtpAsync(string phoneNumber, string otp);
    
    /// <summary>
    /// Resend OTP with rate limiting
    /// </summary>
    Task<bool> ResendOtpAsync(string phoneNumber, string purpose = "Registration");
    
    /// <summary>
    /// Check if user is authenticated
    /// </summary>
    bool IsAuthenticated();
    
    /// <summary>
    /// Get current authentication token
    /// </summary>
    string? GetToken();
    
    /// <summary>
    /// Initialize auth service (load token from storage)
    /// </summary>
    Task InitializeAsync();
    
    /// <summary>
    /// Logout user and clear session
    /// </summary>
    Task LogoutAsync();
}


