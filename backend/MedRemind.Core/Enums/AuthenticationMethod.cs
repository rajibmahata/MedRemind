namespace MedRemind.Core.Enums;

/// <summary>
/// Authentication method used by the user
/// </summary>
public enum AuthenticationMethod
{
    /// <summary>
    /// Not verified yet
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Verified via SMS OTP
    /// </summary>
    SmsOtp = 1,
    
    /// <summary>
    /// Verified via Email OTP
    /// </summary>
    EmailOtp = 2,
    
    /// <summary>
    /// Verified via both SMS and Email
    /// </summary>
    Both = 3
}
