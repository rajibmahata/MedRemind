namespace MedRemind.Web.Services;

/// <summary>
/// Authentication service for managing user login and session
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Register a new user
    /// </summary>
    Task<bool> RegisterAsync(RegisterModel model);
    
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
    /// Get current user token from storage
    /// </summary>
    Task<string?> GetTokenAsync();
    
    /// <summary>
    /// Get current user profile
    /// </summary>
    Task<UserProfileData?> GetCurrentUserAsync();
    
    /// <summary>
    /// Initialize auth service (load token from storage)
    /// </summary>
    Task InitializeAsync();
    
    /// <summary>
    /// Logout user and clear session
    /// </summary>
    Task LogoutAsync();
}

public class UserProfileData
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public DateTime? DateOfBirth { get; set; }
}



