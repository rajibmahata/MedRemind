using System.Text.RegularExpressions;
using MedRemind.Core.DTOs;
using MedRemind.Core.Enums;
using MedRemind.Core.Interfaces;
using MedRemind.Core.Models;
using MedRemind.Core.Services;
using Microsoft.Extensions.Logging;

namespace MedRemind.Services.Users;

/// <summary>
/// Service for managing user registration, profiles, and related operations
/// </summary>
public class UserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserService>? _logger;
    private readonly Communication.OtpCodeService? _otpService;
    private readonly Communication.EmailService? _emailService;
    private readonly PasswordHashingService _passwordHashingService;

    public UserService(
        IUnitOfWork unitOfWork,
        Communication.OtpCodeService? otpService = null,
        Communication.EmailService? emailService = null,
        ILogger<UserService>? logger = null)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _otpService = otpService;
        _emailService = emailService;
        _logger = logger;
        _passwordHashingService = new PasswordHashingService();
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    public async Task<UserRegistrationResponse> RegisterUserAsync(UserRegistrationRequest request)
    {
        try
        {
            _logger?.LogInformation("?? Registering new user with phone: {PhoneNumber}", request.PhoneNumber);

            // Validate phone number
            if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                return new UserRegistrationResponse
                {
                    Success = false,
                    ErrorMessage = "Phone number is required"
                };
            }

            // Validate email is provided
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return new UserRegistrationResponse
                {
                    Success = false,
                    ErrorMessage = "Email is required"
                };
            }

            // Validate phone number format (10 digits)
            if (!IsValidPhoneNumber(request.PhoneNumber))
            {
                return new UserRegistrationResponse
                {
                    Success = false,
                    ErrorMessage = "Invalid phone number format. Please enter a 10-digit phone number"
                };
            }

            // Validate email format
            if (!IsValidEmail(request.Email))
            {
                return new UserRegistrationResponse
                {
                    Success = false,
                    ErrorMessage = "Invalid email format"
                };
            }

            // Validate password if provided
            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                var (isValid, passwordError) = _passwordHashingService.ValidatePasswordStrength(request.Password);
                if (!isValid)
                {
                    return new UserRegistrationResponse
                    {
                        Success = false,
                        ErrorMessage = passwordError
                    };
                }
            }

            var userRepo = _unitOfWork.Repository<User>();

            // Check if user already exists
            var existingUser = await userRepo.FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);
            if (existingUser != null)
            {
                return new UserRegistrationResponse
                {
                    Success = false,
                    ErrorMessage = "User with this phone number already exists"
                };
            }

            // Check if email already exists (required field)
            var existingEmailUser = await userRepo.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existingEmailUser != null)
            {
                return new UserRegistrationResponse
                {
                    Success = false,
                    ErrorMessage = "User with this email already exists"
                };
            }

            // Create new user
            var user = new User
            {
                PhoneNumber = request.PhoneNumber,
                Email = request.Email,
                Name = request.Name,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
                
                // Hash password if provided
                PasswordHash = !string.IsNullOrWhiteSpace(request.Password) 
                    ? _passwordHashingService.HashPassword(request.Password) 
                    : null,
                
                // Set authentication flags as not verified (null/false)
                IsEmailVerified = false,
                IsPhoneVerified = false,
                AuthenticationMethod = MedRemind.Core.Enums.AuthenticationMethod.None,
                LastAuthenticationMethod = MedRemind.Core.Enums.AuthenticationMethod.None,
                EmailVerifiedAt = null,
                PhoneVerifiedAt = null
            };

            await userRepo.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            _logger?.LogInformation("? User registered successfully - ID: {UserId}, Phone: {PhoneNumber}", 
                user.Id, user.PhoneNumber);
            _logger?.LogInformation("   Verification Status: Email={IsEmailVerified}, Phone={IsPhoneVerified}, HasPassword={HasPassword}", 
                user.IsEmailVerified, user.IsPhoneVerified, !string.IsNullOrWhiteSpace(user.PasswordHash));

            return new UserRegistrationResponse
            {
                Success = true,
                UserId = user.Id,
                Profile = MapToProfileData(user)
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "? Error registering user");
            return new UserRegistrationResponse
            {
                Success = false,
                ErrorMessage = $"Registration failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Send OTP after registration for verification
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> SendPostRegistrationOtpAsync(
        int userId,
        string? senderInfo = null)
    {
        try
        {
            if (_otpService == null)
            {
                _logger?.LogWarning("?? OTP Service not available");
                return (false, "OTP service is not configured");
            }

            // Get user
            var userRepo = _unitOfWork.Repository<User>();
            var user = await userRepo.GetByIdAsync(userId);

            if (user == null)
            {
                return (false, "User not found");
            }

            _logger?.LogInformation("?? Sending post-registration OTP for user {UserId}", userId);

            // Send OTP (will send via SMS, Email, or Both based on configuration)
            var result = await _otpService.GenerateAndSendOtpAsync(
                phoneNumber: user.PhoneNumber,
                email: user.Email,
                purpose: "Registration",
                userId: user.Id,
                senderInfo: senderInfo);

            if (result.Success)
            {
                _logger?.LogInformation("? Post-registration OTP sent for user {UserId}", userId);
            }
            else
            {
                _logger?.LogWarning("?? Failed to send post-registration OTP: {Error}", result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "? Error sending post-registration OTP");
            return (false, $"Failed to send OTP: {ex.Message}");
        }
    }

    /// <summary>
    /// Send welcome email to new user
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> SendWelcomeEmailAsync(int userId)
    {
        try
        {
            if (_emailService == null)
            {
                _logger?.LogWarning("?? Email Service not available");
                return (false, "Email service is not configured");
            }

            // Get user
            var userRepo = _unitOfWork.Repository<User>();
            var user = await userRepo.GetByIdAsync(userId);

            if (user == null)
            {
                return (false, "User not found");
            }

            _logger?.LogInformation("?? Sending welcome email to user {UserId}", userId);

            // Send welcome email
            var result = await _emailService.SendWelcomeEmailAsync(
                email: user.Email,
                userName: user.Name ?? "User",
                phoneNumber: user.PhoneNumber,
                registrationDate: user.CreatedAt);

            if (result.Success)
            {
                _logger?.LogInformation("? Welcome email sent to user {UserId}", userId);
            }
            else
            {
                _logger?.LogWarning("?? Failed to send welcome email: {Error}", result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "? Error sending welcome email");
            return (false, $"Failed to send welcome email: {ex.Message}");
        }
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    public async Task<UserProfileData?> GetUserByIdAsync(int userId)
    {
        try
        {
            var userRepo = _unitOfWork.Repository<User>();
            var user = await userRepo.GetByIdAsync(userId);
            
            if (user == null)
            {
                _logger?.LogWarning("?? User not found: {UserId}", userId);
                return null;
            }

            return MapToProfileData(user);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "? Error getting user {UserId}", userId);
            return null;
        }
    }

    /// <summary>
    /// Get user by phone number
    /// </summary>
    public async Task<UserProfileData?> GetUserByPhoneNumberAsync(string phoneNumber)
    {
        try
        {
            var userRepo = _unitOfWork.Repository<User>();
            var user = await userRepo.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
            
            if (user == null)
            {
                _logger?.LogWarning("?? User not found with phone: {PhoneNumber}", phoneNumber);
                return null;
            }

            return MapToProfileData(user);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "? Error getting user by phone");
            return null;
        }
    }

    /// <summary>
    /// Update user profile
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> UpdateUserProfileAsync(
        int userId,
        UserProfileUpdateRequest request)
    {
        try
        {
            _logger?.LogInformation("?? Updating profile for user {UserId}", userId);

            // Validate email if provided (email update is optional during profile update)
            if (!string.IsNullOrWhiteSpace(request.Email) && !IsValidEmail(request.Email))
            {
                return (false, "Invalid email format");
            }

            var userRepo = _unitOfWork.Repository<User>();
            var user = await userRepo.GetByIdAsync(userId);

            if (user == null)
            {
                return (false, "User not found");
            }

            // Check if email already exists (if changing email)
            if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
            {
                var existingEmailUser = await userRepo.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (existingEmailUser != null)
                {
                    return (false, "Email already in use by another user");
                }
                
                // Update email (can't be set to empty)
                user.Email = request.Email;
            }

            // Update other fields
            if (!string.IsNullOrWhiteSpace(request.Name))
                user.Name = request.Name;
            
            if (request.DateOfBirth.HasValue)
                user.DateOfBirth = request.DateOfBirth;
            
            if (!string.IsNullOrWhiteSpace(request.Gender))
                user.Gender = request.Gender;

            await userRepo.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            _logger?.LogInformation("? Profile updated for user {UserId}", userId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "? Error updating user profile");
            return (false, $"Update failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete user account
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> DeleteUserAsync(int userId)
    {
        try
        {
            _logger?.LogInformation("??? Deleting user {UserId}", userId);

            var userRepo = _unitOfWork.Repository<User>();
            var user = await userRepo.GetByIdAsync(userId);

            if (user == null)
            {
                return (false, "User not found");
            }

            await userRepo.DeleteAsync(user);
            await _unitOfWork.SaveChangesAsync();

            _logger?.LogInformation("? User {UserId} deleted successfully", userId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "? Error deleting user");
            return (false, $"Delete failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Check if user exists by phone number
    /// </summary>
    public async Task<bool> UserExistsAsync(string phoneNumber)
    {
        try
        {
            var userRepo = _unitOfWork.Repository<User>();
            var user = await userRepo.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
            return user != null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "? Error checking user existence");
            return false;
        }
    }

    /// <summary>
    /// Validate phone number format (10 digits)
    /// </summary>
    private bool IsValidPhoneNumber(string phoneNumber)
    {
        return !string.IsNullOrWhiteSpace(phoneNumber) && 
               phoneNumber.Length == 10 && 
               phoneNumber.All(char.IsDigit);
    }

    /// <summary>
    /// Validate email format
    /// </summary>
    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            return emailRegex.IsMatch(email);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Map User entity to UserProfileData DTO
    /// </summary>
    private UserProfileData MapToProfileData(User user)
    {
        return new UserProfileData
        {
            Id = user.Id,
            PhoneNumber = user.PhoneNumber,
            Email = user.Email,
            Name = user.Name,
            DateOfBirth = user.DateOfBirth,
            Gender = user.Gender,
            ProfilePhotoPath = user.ProfilePhotoPath,
            IsBiometricEnabled = user.IsBiometricEnabled,
            IsEmailVerified = user.IsEmailVerified,
            IsPhoneVerified = user.IsPhoneVerified,
            AuthenticationMethod = user.AuthenticationMethod.ToString(),
            LastAuthenticationMethod = user.LastAuthenticationMethod.ToString(),
            EmailVerifiedAt = user.EmailVerifiedAt,
            PhoneVerifiedAt = user.PhoneVerifiedAt,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }
}
