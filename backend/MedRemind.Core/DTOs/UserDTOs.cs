namespace MedRemind.Core.DTOs;

/// <summary>
/// User registration request DTO
/// </summary>
public class UserRegistrationRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty; // Required email field
    public string? Name { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Password { get; set; } // Optional password
}

/// <summary>
/// User registration response DTO
/// </summary>
public class UserRegistrationResponse
{
    public bool Success { get; set; }
    public int? UserId { get; set; }
    public string? Token { get; set; }
    public string? ErrorMessage { get; set; }
    public UserProfileData? Profile { get; set; }
}

/// <summary>
/// User profile update request DTO
/// </summary>
public class UserProfileUpdateRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
}

/// <summary>
/// User profile data DTO
/// </summary>
public class UserProfileData
{
    public int Id { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty; // Required field
    public string? Name { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? ProfilePhotoPath { get; set; }
    public bool IsBiometricEnabled { get; set; }
    
    // Authentication tracking
    public bool IsEmailVerified { get; set; }
    public bool IsPhoneVerified { get; set; }
    public string AuthenticationMethod { get; set; } = "None"; // Enum as string
    public string LastAuthenticationMethod { get; set; } = "None";
    public DateTime? EmailVerifiedAt { get; set; }
    public DateTime? PhoneVerifiedAt { get; set; }
    
    // OTP Delivery Tracking
    public bool IsEmailOtpSent { get; set; }
    public bool IsSmsOtpSent { get; set; }
    public DateTime? LastEmailOtpSentAt { get; set; }
    public DateTime? LastSmsOtpSentAt { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
