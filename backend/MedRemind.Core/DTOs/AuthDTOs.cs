namespace MedRemind.Core.DTOs;

/// <summary>
/// Login request with email/phone and password
/// </summary>
public class LoginRequest
{
    public string Identifier { get; set; } = string.Empty; // Email or Phone Number
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Login response with token and user info
/// </summary>
public class LoginResponse
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? ErrorMessage { get; set; }
    public UserProfileData? Profile { get; set; }
}

/// <summary>
/// Forgot password request
/// </summary>
public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Forgot password response
/// </summary>
public class ForgotPasswordResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Reset password request
/// </summary>
public class ResetPasswordRequest
{
    public string Email { get; set; } = string.Empty;
    public string ResetToken { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// Reset password response
/// </summary>
public class ResetPasswordResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Change password request
/// </summary>
public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// Change password response
/// </summary>
public class ChangePasswordResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
}
