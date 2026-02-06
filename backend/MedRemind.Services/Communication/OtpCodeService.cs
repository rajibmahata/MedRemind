using MedRemind.Core.Interfaces;
using MedRemind.Core.Models;
using Microsoft.Extensions.Logging;

namespace MedRemind.Services.Communication;

/// <summary>
/// Service for managing OTP codes - creation, storage, verification, and expiration
/// </summary>
public class OtpCodeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISmsService? _smsService;
    private readonly IEmailService? _emailService;
    private readonly bool _enableSmsOtp;
    private readonly bool _enableEmailOtp;
    private readonly ILogger<OtpCodeService>? _logger;

    public OtpCodeService(
        IUnitOfWork unitOfWork,
        ISmsService? smsService = null,
        IEmailService? emailService = null,
        bool enableSmsOtp = true,
        bool enableEmailOtp = false,
        ILogger<OtpCodeService>? logger = null)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _smsService = smsService;
        _emailService = emailService;
        _enableSmsOtp = enableSmsOtp;
        _enableEmailOtp = enableEmailOtp;
        _logger = logger;
    }

    /// <summary>
    /// Generate and send OTP based on configuration
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> GenerateAndSendOtpAsync(
        string phoneNumber,
        string? email = null,
        string purpose = "Login",
        int? userId = null,
        string? senderInfo = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("?? Generating OTP for {PhoneNumber}, Purpose: {Purpose}", phoneNumber, purpose);

            // Generate 6-digit OTP
            var otp = GenerateOtp();
            
            // Calculate expiration (10 minutes)
            var expiresAt = DateTime.UtcNow.AddMinutes(10);

            // Determine delivery method
            var deliveryMethod = GetDeliveryMethod();
            
            if (deliveryMethod == "None")
            {
                return (false, "No OTP delivery method is enabled. Please configure SMS or Email OTP.");
            }

            // Deactivate previous OTPs for this phone number
            await DeactivatePreviousOtpsAsync(phoneNumber);

            // Create OTP record
            var otpCode = new OtpCode
            {
                PhoneNumber = phoneNumber,
                Email = email,
                UserId = userId,
                Code = otp,
                Purpose = purpose,
                DeliveryMethod = deliveryMethod,
                IsActive = true,
                IsVerified = false,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                AttemptCount = 0,
                MaxAttempts = 3,
                SenderInfo = senderInfo
            };

            // Save to database
            var otpRepo = _unitOfWork.Repository<OtpCode>();
            await otpRepo.AddAsync(otpCode);
            await _unitOfWork.SaveChangesAsync();

            _logger?.LogInformation("? OTP created - ID: {OtpId}, Expires: {ExpiresAt}", otpCode.Id, expiresAt);

            // Send OTP via configured methods
            var sendResult = await SendOtpAsync(phoneNumber, email, otp, deliveryMethod, cancellationToken);

            if (!sendResult.Success)
            {
                // Mark OTP as inactive if sending failed
                otpCode.IsActive = false;
                await otpRepo.UpdateAsync(otpCode);
                await _unitOfWork.SaveChangesAsync();
                
                return sendResult;
            }

            _logger?.LogInformation("?? OTP sent successfully via {DeliveryMethod}", deliveryMethod);

            // Update user's OTP sent flags if user exists
            if (userId.HasValue)
            {
                await UpdateUserOtpSentFlagsAsync(userId.Value, deliveryMethod);
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "? Error generating and sending OTP");
            return (false, $"Failed to generate OTP: {ex.Message}");
        }
    }

    /// <summary>
    /// Verify OTP code
    /// </summary>
    public async Task<(bool Success, OtpCode? OtpCode, string? ErrorMessage)> VerifyOtpAsync(
        string phoneNumber,
        string code)
    {
        try
        {
            _logger?.LogInformation("?? Verifying OTP for {PhoneNumber}", phoneNumber);

            var otpRepo = _unitOfWork.Repository<OtpCode>();
            
            // Find active OTP for this phone number
            var otpCodes = await otpRepo.FindAsync(o => 
                o.PhoneNumber == phoneNumber && 
                o.IsActive == true &&
                o.IsVerified == false);

            var otpCode = otpCodes.OrderByDescending(o => o.CreatedAt).FirstOrDefault();

            if (otpCode == null)
            {
                _logger?.LogWarning("?? No active OTP found for {PhoneNumber}", phoneNumber);
                return (false, null, "No active OTP found. Please request a new OTP.");
            }

            // Check expiration
            if (DateTime.UtcNow > otpCode.ExpiresAt)
            {
                _logger?.LogWarning("? OTP expired for {PhoneNumber}", phoneNumber);
                otpCode.IsActive = false;
                await otpRepo.UpdateAsync(otpCode);
                await _unitOfWork.SaveChangesAsync();
                
                return (false, null, "OTP has expired. Please request a new OTP.");
            }

            // Increment attempt count
            otpCode.AttemptCount++;
            
            // Check if max attempts exceeded
            if (otpCode.AttemptCount > otpCode.MaxAttempts)
            {
                _logger?.LogWarning("?? Max attempts exceeded for {PhoneNumber}", phoneNumber);
                otpCode.IsActive = false;
                await otpRepo.UpdateAsync(otpCode);
                await _unitOfWork.SaveChangesAsync();
                
                return (false, null, "Maximum verification attempts exceeded. Please request a new OTP.");
            }

            // Verify code
            if (otpCode.Code != code)
            {
                _logger?.LogWarning("? Invalid OTP for {PhoneNumber}", phoneNumber);
                await otpRepo.UpdateAsync(otpCode);
                await _unitOfWork.SaveChangesAsync();
                
                return (false, null, $"Invalid OTP. {otpCode.MaxAttempts - otpCode.AttemptCount} attempts remaining.");
            }

            // OTP is valid
            _logger?.LogInformation("? OTP verified successfully for {PhoneNumber}", phoneNumber);
            otpCode.IsVerified = true;
            otpCode.VerifiedAt = DateTime.UtcNow;
            otpCode.IsActive = false; // Deactivate after successful verification
            
            await otpRepo.UpdateAsync(otpCode);
            await _unitOfWork.SaveChangesAsync();

            return (true, otpCode, null);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "? Error verifying OTP");
            return (false, null, $"Verification failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Cleanup expired OTPs (to be called periodically)
    /// </summary>
    public async Task<int> CleanupExpiredOtpsAsync()
    {
        try
        {
            _logger?.LogInformation("?? Starting OTP cleanup...");

            var otpRepo = _unitOfWork.Repository<OtpCode>();
            
            // Find expired OTPs (older than 10 minutes + 5 minute grace period = 15 minutes)
            var cutoffTime = DateTime.UtcNow.AddMinutes(-15);
            var expiredOtps = await otpRepo.FindAsync(o => 
                o.CreatedAt < cutoffTime || 
                (o.ExpiresAt < DateTime.UtcNow && o.IsActive == false));

            var count = 0;
            foreach (var otp in expiredOtps)
            {
                await otpRepo.DeleteAsync(otp);
                count++;
            }

            await _unitOfWork.SaveChangesAsync();
            
            _logger?.LogInformation("? Cleaned up {Count} expired OTPs", count);
            return count;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "? Error cleaning up expired OTPs");
            return 0;
        }
    }

    /// <summary>
    /// Deactivate previous OTPs for a phone number
    /// </summary>
    private async Task DeactivatePreviousOtpsAsync(string phoneNumber)
    {
        try
        {
            var otpRepo = _unitOfWork.Repository<OtpCode>();
            var activeOtps = await otpRepo.FindAsync(o => 
                o.PhoneNumber == phoneNumber && 
                o.IsActive == true);

            foreach (var otp in activeOtps)
            {
                otp.IsActive = false;
                await otpRepo.UpdateAsync(otp);
            }

            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deactivating previous OTPs");
        }
    }

    /// <summary>
    /// Send OTP via configured methods
    /// </summary>
    private async Task<(bool Success, string? ErrorMessage)> SendOtpAsync(
        string phoneNumber,
        string? email,
        string otp,
        string deliveryMethod,
        CancellationToken cancellationToken)
    {
        var smsSent = false;
        var emailSent = false;
        var errors = new List<string>();

        // Get user name for personalization (if available)
        string userName = "User";
        try
        {
            var userRepo = _unitOfWork.Repository<User>();
            var user = await userRepo.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
            if (user != null && !string.IsNullOrWhiteSpace(user.Name))
            {
                userName = user.Name;
            }
        }
        catch
        {
            // Continue with default name if lookup fails
        }

        // Send via SMS
        if ((deliveryMethod == "SMS" || deliveryMethod == "Both") && _smsService != null)
        {
            var (success, error) = await _smsService.SendOtpAsync(phoneNumber, otp, cancellationToken);
            smsSent = success;
            if (!success && !string.IsNullOrEmpty(error))
            {
                errors.Add($"SMS: {error}");
            }
        }

        // Send via Email with user name
        if ((deliveryMethod == "Email" || deliveryMethod == "Both") && _emailService != null && !string.IsNullOrEmpty(email))
        {
            // Cast to EmailService to access the overload with userName
            if (_emailService is EmailService emailService)
            {
                var (success, error) = await emailService.SendOtpAsync(email, otp, userName, cancellationToken);
                emailSent = success;
                if (!success && !string.IsNullOrEmpty(error))
                {
                    errors.Add($"Email: {error}");
                }
            }
            else
            {
                // Fallback to interface method
                var (success, error) = await _emailService.SendOtpAsync(email, otp, cancellationToken);
                emailSent = success;
                if (!success && !string.IsNullOrEmpty(error))
                {
                    errors.Add($"Email: {error}");
                }
            }
        }

        // Check if at least one method succeeded
        if (deliveryMethod == "Both")
        {
            if (smsSent && emailSent)
            {
                return (true, null);
            }
            else if (smsSent || emailSent)
            {
                return (true, $"Partial success: {string.Join(", ", errors)}");
            }
            else
            {
                return (false, string.Join(", ", errors));
            }
        }
        else if (deliveryMethod == "SMS")
        {
            return smsSent ? (true, null) : (false, errors.FirstOrDefault());
        }
        else if (deliveryMethod == "Email")
        {
            return emailSent ? (true, null) : (false, errors.FirstOrDefault());
        }

        return (false, "No delivery method configured");
    }

    /// <summary>
    /// Determine delivery method based on configuration
    /// </summary>
    private string GetDeliveryMethod()
    {
        if (_enableSmsOtp && _enableEmailOtp)
        {
            return "Both";
        }
        else if (_enableSmsOtp)
        {
            return "SMS";
        }
        else if (_enableEmailOtp)
        {
            return "Email";
        }
        else
        {
            return "None";
        }
    }

    /// <summary>
    /// Generate random 6-digit OTP
    /// </summary>
    private string GenerateOtp()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }

    /// <summary>
    /// Update user's OTP sent flags based on delivery method
    /// </summary>
    private async Task UpdateUserOtpSentFlagsAsync(int userId, string deliveryMethod)
    {
        try
        {
            var userRepo = _unitOfWork.Repository<User>();
            var user = await userRepo.GetByIdAsync(userId);

            if (user == null)
            {
                return;
            }

            var now = DateTime.UtcNow;

            // Update flags based on delivery method
            if (deliveryMethod == "SMS" || deliveryMethod == "Both")
            {
                user.IsSmsOtpSent = true;
                user.LastSmsOtpSentAt = now;
                _logger?.LogInformation("? SMS OTP sent flag updated for user {UserId}", userId);
            }

            if (deliveryMethod == "Email" || deliveryMethod == "Both")
            {
                user.IsEmailOtpSent = true;
                user.LastEmailOtpSentAt = now;
                _logger?.LogInformation("? Email OTP sent flag updated for user {UserId}", userId);
            }

            await userRepo.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating OTP sent flags for user {UserId}", userId);
            // Don't throw - this is tracking only, not critical
        }
    }
}
