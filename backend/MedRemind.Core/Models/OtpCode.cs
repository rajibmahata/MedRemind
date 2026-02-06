using System;

namespace MedRemind.Core.Models;

/// <summary>
/// OTP Code Collection - Stores OTP codes with expiration and verification tracking
/// </summary>
public class OtpCode
{
    public int Id { get; set; }
    
    /// <summary>
    /// User's phone number
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// User's email address
    /// </summary>
    public string? Email { get; set; }
    
    /// <summary>
    /// User ID (if registered, null if not yet registered)
    /// </summary>
    public int? UserId { get; set; }
    
    /// <summary>
    /// 6-digit OTP code
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// Purpose of OTP (Registration, Login, PasswordReset, etc.)
    /// </summary>
    public string Purpose { get; set; } = string.Empty;
    
    /// <summary>
    /// Delivery method (SMS, Email, Both)
    /// </summary>
    public string DeliveryMethod { get; set; } = string.Empty;
    
    /// <summary>
    /// Is this OTP currently active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Has this OTP been verified
    /// </summary>
    public bool IsVerified { get; set; } = false;
    
    /// <summary>
    /// When the OTP was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the OTP expires (10 minutes from creation)
    /// </summary>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>
    /// When the OTP was verified (if verified)
    /// </summary>
    public DateTime? VerifiedAt { get; set; }
    
    /// <summary>
    /// Number of verification attempts
    /// </summary>
    public int AttemptCount { get; set; } = 0;
    
    /// <summary>
    /// Maximum allowed attempts
    /// </summary>
    public int MaxAttempts { get; set; } = 3;
    
    /// <summary>
    /// IP address or device info (for security)
    /// </summary>
    public string? SenderInfo { get; set; }
    
    // Navigation property
    public User? User { get; set; }
}
