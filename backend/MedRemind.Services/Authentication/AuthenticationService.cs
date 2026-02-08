using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MedRemind.Core.DTOs;
using MedRemind.Core.Enums;
using MedRemind.Core.Interfaces;
using MedRemind.Core.Models;
using MedRemind.Core.Services;
using Microsoft.IdentityModel.Tokens;

namespace MedRemind.Services.Authentication;

public class AuthenticationService : IAuthenticationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecureStorageService _secureStorage;
    private readonly Communication.OtpCodeService? _otpService;
    private readonly Communication.EmailService? _emailService;
    private readonly PasswordHashingService _passwordHashingService;
    private readonly string _jwtSecretKey;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly int _jwtExpirationDays;

    public AuthenticationService(
        IUnitOfWork unitOfWork,
        ISecureStorageService secureStorage,
        Communication.OtpCodeService? otpService = null,
        Communication.EmailService? emailService = null,
        string? jwtSecretKey = null,
        string? jwtIssuer = null,
        string? jwtAudience = null,
        int jwtExpirationDays = 30)
    {
        _unitOfWork = unitOfWork;
        _secureStorage = secureStorage;
        _otpService = otpService;
        _emailService = emailService;
        _passwordHashingService = new PasswordHashingService();

        // JWT Configuration
        _jwtSecretKey = jwtSecretKey ?? "YOUR_SECRET_KEY_HERE_MINIMUM_32_CHARACTERS";
        _jwtIssuer = jwtIssuer ?? "MedRemind.API";
        _jwtAudience = jwtAudience ?? "MedRemind.Mobile";
        _jwtExpirationDays = jwtExpirationDays;
    }

    public async Task<(bool Success, string? ErrorMessage)> SendOtpAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_otpService == null)
            {
                return (false, "OTP service is not configured");
            }

            // Get user to retrieve email
            var userRepo = _unitOfWork.Repository<User>();
            var user = await userRepo.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);

            if (user == null)
            {
                return (false, "User not found. Please register first.");
            }

            System.Diagnostics.Debug.WriteLine($"📤 Sending OTP for login to {phoneNumber}");

            // Use OtpCodeService to generate and send OTP
            var result = await _otpService.GenerateAndSendOtpAsync(
                phoneNumber: phoneNumber,
                email: user.Email,
                purpose: "Login",
                userId: user.Id,
                cancellationToken: cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error sending OTP: {ex.Message}");
            return (false, $"Error sending OTP: {ex.Message}");
        }
    }

    public async Task<(bool Success, string? Token, string? ErrorMessage)> VerifyOtpAsync(
        string phoneNumber,
        string otp,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length != 10)
            {
                return (false, null, "Invalid phone number.");
            }

            if (string.IsNullOrWhiteSpace(otp) || otp.Length < 4 || otp.Length > 6)
            {
                return (false, null, "Invalid OTP. Please enter the code you received.");
            }

            if (_otpService == null)
            {
                return (false, null, "OTP service is not configured");
            }

            System.Diagnostics.Debug.WriteLine($"🔍 Verifying OTP for {phoneNumber}");

            // Verify OTP using OtpCodeService
            var (success, otpCode, errorMessage) = await _otpService.VerifyOtpAsync(phoneNumber, otp);

            if (!success)
            {
                return (false, null, errorMessage);
            }

            // Find user
            var userRepo = _unitOfWork.Repository<User>();
            var user = await userRepo.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);

            if (user == null)
            {
                return (false, null, "User not found. Please register first.");
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            
            // Set authentication method and verification status based on delivery method
            if (otpCode != null)
            {
                if (otpCode.DeliveryMethod == "Email" || otpCode.DeliveryMethod == "Both")
                {
                    user.IsEmailVerified = true;
                    user.EmailVerifiedAt = DateTime.UtcNow;
                    System.Diagnostics.Debug.WriteLine($"✅ Email verified for user {user.Id}");
                }
                
                if (otpCode.DeliveryMethod == "SMS" || otpCode.DeliveryMethod == "Both")
                {
                    user.IsPhoneVerified = true;
                    user.PhoneVerifiedAt = DateTime.UtcNow;
                    System.Diagnostics.Debug.WriteLine($"✅ Phone verified for user {user.Id}");
                }

                // Determine authentication method
                var currentAuthMethod = otpCode.DeliveryMethod == "Email" 
                    ? AuthenticationMethod.EmailOtp 
                    : (otpCode.DeliveryMethod == "SMS" 
                        ? AuthenticationMethod.SmsOtp 
                        : AuthenticationMethod.Both);
                
                user.LastAuthenticationMethod = currentAuthMethod;
                
                // Set overall authentication method
                if (user.IsEmailVerified && user.IsPhoneVerified)
                {
                    user.AuthenticationMethod = AuthenticationMethod.Both;
                }
                else
                {
                    user.AuthenticationMethod = currentAuthMethod;
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"🔐 Authentication method: {user.AuthenticationMethod}");
            System.Diagnostics.Debug.WriteLine($"   Email verified: {user.IsEmailVerified}");
            System.Diagnostics.Debug.WriteLine($"   Phone verified: {user.IsPhoneVerified}");

            // Generate session token
            var token = await GenerateSessionTokenAsync(user.Id);
            user.SessionToken = token;

            await userRepo.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            // Store token in secure storage
            await _secureStorage.SetAsync("session_token", token);
            await _secureStorage.SetAsync("user_id", user.Id.ToString());
            await _secureStorage.SetAsync("phone_number", phoneNumber);

            System.Diagnostics.Debug.WriteLine($"✅ User logged in: {user.Id}");

            return (true, token, null);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error verifying OTP: {ex.Message}");
            return (false, null, $"Error verifying OTP: {ex.Message}");
        }
    }

    public async Task<string> GenerateSessionTokenAsync(int userId)
    {
        // Create claims for the JWT token
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };

        // Create the signing key
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Create the JWT token
        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(_jwtExpirationDays),
            signingCredentials: credentials
        );

        // Generate the token string
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenString = tokenHandler.WriteToken(token);

        System.Diagnostics.Debug.WriteLine($"🔑 Generated JWT token for user {userId}");

        return tokenString;
    }

    public async Task<bool> ValidateSessionTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSecretKey);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtIssuer,
                ValidateAudience = true,
                ValidAudience = _jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            System.Diagnostics.Debug.WriteLine($"✅ JWT token validated successfully");
            return true;
        }
        catch (SecurityTokenExpiredException)
        {
            System.Diagnostics.Debug.WriteLine($"❌ JWT token has expired");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ JWT token validation failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Login with email/phone and password
    /// </summary>
    public async Task<LoginResponse> LoginWithPasswordAsync(LoginRequest request)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"🔐 Login attempt for: {request.Identifier}");

            var userRepo = _unitOfWork.Repository<User>();
            
            // Find user by email or phone
            var user = await userRepo.FirstOrDefaultAsync(u => 
                u.Email == request.Identifier || u.PhoneNumber == request.Identifier);

            if (user == null)
            {
                System.Diagnostics.Debug.WriteLine("❌ User not found");
                return new LoginResponse
                {
                    Success = false,
                    ErrorMessage = "Invalid credentials"
                };
            }

            // Check if user has a password set
            if (string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                System.Diagnostics.Debug.WriteLine("❌ User has no password set");
                return new LoginResponse
                {
                    Success = false,
                    ErrorMessage = "No password set. Please use OTP login or reset your password."
                };
            }

            // Verify password
            if (!_passwordHashingService.VerifyPassword(request.Password, user.PasswordHash))
            {
                System.Diagnostics.Debug.WriteLine("❌ Invalid password");
                return new LoginResponse
                {
                    Success = false,
                    ErrorMessage = "Invalid credentials"
                };
            }

            // Password is correct - proceed with login
            System.Diagnostics.Debug.WriteLine("✅ Password verified");

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            user.LastAuthenticationMethod = AuthenticationMethod.EmailOtp; // Password-based

            // Generate session token
            var token = await GenerateSessionTokenAsync(user.Id);
            user.SessionToken = token;

            await userRepo.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            // Store token in secure storage
            await _secureStorage.SetAsync("session_token", token);
            await _secureStorage.SetAsync("user_id", user.Id.ToString());

            System.Diagnostics.Debug.WriteLine($"✅ User logged in: {user.Id}");

            return new LoginResponse
            {
                Success = true,
                Token = token,
                Profile = MapToProfileData(user)
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error during login: {ex.Message}");
            return new LoginResponse
            {
                Success = false,
                ErrorMessage = $"Login failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Initiate forgot password - send reset email
    /// </summary>
    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"🔐 Forgot password request for: {request.Email}");

            var userRepo = _unitOfWork.Repository<User>();
            var user = await userRepo.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                // Don't reveal if user exists for security
                System.Diagnostics.Debug.WriteLine("⚠️ User not found, but returning success for security");
                return new ForgotPasswordResponse
                {
                    Success = true,
                    Message = "If an account exists with this email, you will receive password reset instructions."
                };
            }

            // Generate reset token
            var resetToken = _passwordHashingService.GeneratePasswordResetToken();
            var tokenExpiry = DateTime.UtcNow.AddHours(1); // Valid for 1 hour

            // Save reset token
            user.PasswordResetToken = resetToken;
            user.PasswordResetTokenExpiry = tokenExpiry;

            await userRepo.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            System.Diagnostics.Debug.WriteLine($"✅ Reset token generated for user {user.Id}");

            // Send reset email
            if (_emailService != null)
            {
                var resetLink = $"https://medremind.com/reset-password?token={resetToken}&email={user.Email}";
                
                var emailResult = await _emailService.SendPasswordResetEmailAsync(
                    user.Email,
                    user.Name ?? "User",
                    resetToken,
                    resetLink);

                if (emailResult.Success)
                {
                    System.Diagnostics.Debug.WriteLine("✅ Password reset email sent");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Failed to send reset email: {emailResult.ErrorMessage}");
                }
            }

            return new ForgotPasswordResponse
            {
                Success = true,
                Message = "If an account exists with this email, you will receive password reset instructions."
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error in forgot password: {ex.Message}");
            return new ForgotPasswordResponse
            {
                Success = false,
                ErrorMessage = "An error occurred. Please try again later."
            };
        }
    }

    /// <summary>
    /// Reset password with token
    /// </summary>
    public async Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"🔐 Reset password attempt for: {request.Email}");

            // Validate passwords match
            if (request.NewPassword != request.ConfirmPassword)
            {
                return new ResetPasswordResponse
                {
                    Success = false,
                    ErrorMessage = "Passwords do not match"
                };
            }

            // Validate password strength
            var (isValid, errorMessage) = _passwordHashingService.ValidatePasswordStrength(request.NewPassword);
            if (!isValid)
            {
                return new ResetPasswordResponse
                {
                    Success = false,
                    ErrorMessage = errorMessage
                };
            }

            var userRepo = _unitOfWork.Repository<User>();
            var user = await userRepo.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                System.Diagnostics.Debug.WriteLine("❌ User not found");
                return new ResetPasswordResponse
                {
                    Success = false,
                    ErrorMessage = "Invalid reset request"
                };
            }

            // Validate reset token
            if (string.IsNullOrWhiteSpace(user.PasswordResetToken) ||
                user.PasswordResetToken != request.ResetToken)
            {
                System.Diagnostics.Debug.WriteLine("❌ Invalid reset token");
                return new ResetPasswordResponse
                {
                    Success = false,
                    ErrorMessage = "Invalid or expired reset token"
                };
            }

            // Check token expiry
            if (user.PasswordResetTokenExpiry == null ||
                DateTime.UtcNow > user.PasswordResetTokenExpiry)
            {
                System.Diagnostics.Debug.WriteLine("❌ Reset token expired");
                return new ResetPasswordResponse
                {
                    Success = false,
                    ErrorMessage = "Reset token has expired. Please request a new one."
                };
            }

            // Hash new password
            user.PasswordHash = _passwordHashingService.HashPassword(request.NewPassword);
            user.LastPasswordChangeAt = DateTime.UtcNow;

            // Clear reset token
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;

            await userRepo.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            System.Diagnostics.Debug.WriteLine($"✅ Password reset successful for user {user.Id}");

            return new ResetPasswordResponse
            {
                Success = true,
                Message = "Password has been reset successfully. You can now login with your new password."
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error resetting password: {ex.Message}");
            return new ResetPasswordResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while resetting password"
            };
        }
    }

    /// <summary>
    /// Change password for authenticated user
    /// </summary>
    public async Task<ChangePasswordResponse> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"🔐 Change password for user: {userId}");

            // Validate passwords match
            if (request.NewPassword != request.ConfirmPassword)
            {
                return new ChangePasswordResponse
                {
                    Success = false,
                    ErrorMessage = "Passwords do not match"
                };
            }

            // Validate password strength
            var (isValid, errorMessage) = _passwordHashingService.ValidatePasswordStrength(request.NewPassword);
            if (!isValid)
            {
                return new ChangePasswordResponse
                {
                    Success = false,
                    ErrorMessage = errorMessage
                };
            }

            var userRepo = _unitOfWork.Repository<User>();
            var user = await userRepo.GetByIdAsync(userId);

            if (user == null)
            {
                return new ChangePasswordResponse
                {
                    Success = false,
                    ErrorMessage = "User not found"
                };
            }

            // Verify current password if user has one
            if (!string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                if (!_passwordHashingService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
                {
                    System.Diagnostics.Debug.WriteLine("❌ Current password incorrect");
                    return new ChangePasswordResponse
                    {
                        Success = false,
                        ErrorMessage = "Current password is incorrect"
                    };
                }
            }

            // Hash and save new password
            user.PasswordHash = _passwordHashingService.HashPassword(request.NewPassword);
            user.LastPasswordChangeAt = DateTime.UtcNow;

            await userRepo.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            System.Diagnostics.Debug.WriteLine($"✅ Password changed for user {userId}");

            return new ChangePasswordResponse
            {
                Success = true,
                Message = "Password has been changed successfully"
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error changing password: {ex.Message}");
            return new ChangePasswordResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while changing password"
            };
        }
    }


    /// <summary>
    /// Resend OTP with rate limiting and validation
    /// </summary>
    public async Task<ResendOtpResponse> ResendOtpAsync(
        ResendOtpRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"🔄 Resend OTP request for: {request.PhoneNumber}");

            // Validate input
            if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                return new ResendOtpResponse
                {
                    Success = false,
                    ErrorMessage = "Phone number is required"
                };
            }

            // Check if OTP service is available
            if (_otpService == null)
            {
                return new ResendOtpResponse
                {
                    Success = false,
                    ErrorMessage = "OTP service is not configured"
                };
            }

            // Find user by phone number
            var userRepo = _unitOfWork.Repository<User>();
            var user = await userRepo.FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);

            if (user == null)
            {
                // For security, don't reveal if user exists
                return new ResendOtpResponse
                {
                    Success = false,
                    ErrorMessage = "User not found"
                };
            }

            // Use user's email if not provided in request
            var email = request.Email ?? user.Email;

            // Resend OTP using OtpCodeService with rate limiting
            var (success, errorMessage, nextResendAvailableAt, remainingAttempts) = 
                await _otpService.ResendOtpAsync(
                    request.PhoneNumber,
                    email,
                    request.Purpose,
                    user.Id,
                    $"Resend-{request.Purpose}",
                    cancellationToken);

            if (success)
            {
                System.Diagnostics.Debug.WriteLine($"✅ OTP resent successfully to: {request.PhoneNumber}");

                // Get updated user to check OTP sent flags
                user = await userRepo.GetByIdAsync(user.Id);

                return new ResendOtpResponse
                {
                    Success = true,
                    Message = errorMessage ?? "OTP has been resent successfully. Please check your email/SMS.",
                    RemainingAttempts = remainingAttempts,
                    IsEmailOtpSent = user?.IsEmailOtpSent ?? false,
                    IsSmsOtpSent = user?.IsSmsOtpSent ?? false
                };
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to resend OTP: {errorMessage}");

                return new ResendOtpResponse
                {
                    Success = false,
                    ErrorMessage = errorMessage,
                    NextResendAvailableAt = nextResendAvailableAt,
                    RemainingAttempts = remainingAttempts
                };
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error in ResendOtpAsync: {ex.Message}");
            return new ResendOtpResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while resending OTP. Please try again."
            };
        }
    }

    /// <summary>
    /// Check if resend OTP is available
    /// </summary>
    public async Task<ResendOtpResponse> CheckResendAvailabilityAsync(
        string phoneNumber,
        string purpose = "Registration")
    {
        try
        {
            if (_otpService == null)
            {
                return new ResendOtpResponse
                {
                    Success = false,
                    ErrorMessage = "OTP service is not configured"
                };
            }

            var (canResend, nextAvailableAt, remainingAttempts) = 
                await _otpService.CanResendOtpAsync(phoneNumber, purpose);

            if (canResend)
            {
                return new ResendOtpResponse
                {
                    Success = true,
                    Message = "Resend is available",
                    RemainingAttempts = remainingAttempts
                };
            }
            else
            {
                var message = remainingAttempts == 0
                    ? "You have reached the maximum number of OTP requests for today."
                    : $"Please wait before requesting a new OTP.";

                return new ResendOtpResponse
                {
                    Success = false,
                    ErrorMessage = message,
                    NextResendAvailableAt = nextAvailableAt,
                    RemainingAttempts = remainingAttempts
                };
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error checking resend availability: {ex.Message}");
            return new ResendOtpResponse
            {
                Success = false,
                ErrorMessage = "An error occurred. Please try again."
            };
        }
    }

    /// <summary>
    /// Map User to UserProfileData
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
            IsEmailOtpSent = user.IsEmailOtpSent,
            IsSmsOtpSent = user.IsSmsOtpSent,
            LastEmailOtpSentAt = user.LastEmailOtpSentAt,
            LastSmsOtpSentAt = user.LastSmsOtpSentAt,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }
}
