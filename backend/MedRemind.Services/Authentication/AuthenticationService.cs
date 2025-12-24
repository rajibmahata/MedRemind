using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MedRemind.Core.Interfaces;
using MedRemind.Core.Models;

namespace MedRemind.Services.Authentication;

public class AuthenticationService : IAuthenticationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecureStorageService _secureStorage;
    private readonly string _twoFactorApiKey;
    private readonly string _sendOtpUrl;
    private readonly string _verifyOtpUrl;
    private readonly string _otpTemplate;
    private readonly HttpClient _httpClient;
    private string? _lastSessionId; // Store session ID from send OTP response

    public AuthenticationService(
        IUnitOfWork unitOfWork,
        ISecureStorageService secureStorage,
        string twoFactorApiKey,
        HttpClient httpClient,
        string? sendOtpUrl = null,
        string? verifyOtpUrl = null,
        string? otpTemplate = null)
    {
        _unitOfWork = unitOfWork;
        _secureStorage = secureStorage;
        _twoFactorApiKey = twoFactorApiKey;
        _httpClient = httpClient;

        // Use provided URLs or default to 2Factor API format
        _sendOtpUrl = sendOtpUrl ?? "https://2factor.in/API/V1/{apiKey}/SMS/{phoneNumber}/{otpValue}/{templateName}";
        _verifyOtpUrl = verifyOtpUrl ?? "https://2factor.in/API/V1/{apiKey}/SMS/VERIFY3/{phoneNumber}/{otpValue}";
        _otpTemplate = otpTemplate ?? "OTP1";
    }

    public async Task<(bool Success, string? ErrorMessage)> SendOtpAsync(
        string phoneNumber, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate phone number format (10 digits)
            if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length != 10)
            {
                return (false, "Invalid phone number. Please enter a 10-digit phone number.");
            }

            // Format phone number with country code (+91 for India)
            var formattedPhone = $"+91{phoneNumber}";

            // Generate random 6-digit OTP
            var otp = GenerateOtp();

            // Build URL from template
            // URL format: https://2factor.in/API/V1/{apiKey}/SMS/{phoneNumber}/{otpValue}/{templateName}
            var url = _sendOtpUrl
                .Replace("{apiKey}", _twoFactorApiKey)
                .Replace("{phoneNumber}", formattedPhone)
                .Replace("{otpValue}", otp)
                .Replace("{templateName}", _otpTemplate);

            System.Diagnostics.Debug.WriteLine($"?? Sending OTP to {phoneNumber}");

            // Call 2Factor.in API
            var response = await _httpClient.GetAsync(url, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            System.Diagnostics.Debug.WriteLine($"?? 2Factor Response: {content}");

            if (!response.IsSuccessStatusCode)
            {
                return (false, $"Failed to send OTP. Please try again.");
            }

            // Parse response to get session ID
            try
            {
                var jsonResponse = JsonSerializer.Deserialize<TwoFactorSendResponse>(content);
                if (jsonResponse?.Status == "Success")
                {
                    _lastSessionId = jsonResponse.Details;
                    System.Diagnostics.Debug.WriteLine($"? OTP sent successfully. Session ID: {_lastSessionId}");
                    return (true, null);
                }
                else
                {
                    return (false, "Failed to send OTP. Please try again.");
                }
            }
            catch
            {
                // If JSON parsing fails, check if response indicates success
                if (content.Contains("Success"))
                {
                    return (true, null);
                }
                return (false, "Failed to send OTP. Please try again.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error sending OTP: {ex.Message}");
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

            // Build verify URL
            // URL format: https://2factor.in/API/V1/{apiKey}/SMS/VERIFY3/{phoneNumber}/{otpValue}
            var url = _verifyOtpUrl
                .Replace("{apiKey}", _twoFactorApiKey)
                .Replace("{phoneNumber}", phoneNumber) // Don't add +91 for verify endpoint
                .Replace("{otpValue}", otp);

            System.Diagnostics.Debug.WriteLine($"?? Verifying OTP for {phoneNumber}");

            // Call 2Factor.in verify API
            var response = await _httpClient.GetAsync(url, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            System.Diagnostics.Debug.WriteLine($"?? Verify Response: {content}");

            // Parse response
            try
            {
                var jsonResponse = JsonSerializer.Deserialize<TwoFactorVerifyResponse>(content);
                
                if (jsonResponse?.Status == "Success" && jsonResponse?.Details == "OTP Matched")
                {
                    System.Diagnostics.Debug.WriteLine("? OTP verified successfully");
                    
                    // Find or create user
                    var userRepo = _unitOfWork.Repository<User>();
                    var user = await userRepo.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);

                    if (user == null)
                    {
                        user = new User
                        {
                            PhoneNumber = phoneNumber,
                            CreatedAt = DateTime.UtcNow
                        };
                        await userRepo.AddAsync(user);
                        System.Diagnostics.Debug.WriteLine($"?? New user created: {phoneNumber}");
                    }

                    user.LastLoginAt = DateTime.UtcNow;
                    
                    // Generate session token
                    var token = await GenerateSessionTokenAsync(user.Id);
                    user.SessionToken = token;

                    await userRepo.UpdateAsync(user);
                    await _unitOfWork.SaveChangesAsync();

                    // Store token in secure storage
                    await _secureStorage.SetAsync("session_token", token);
                    await _secureStorage.SetAsync("user_id", user.Id.ToString());
                    await _secureStorage.SetAsync("phone_number", phoneNumber);

                    System.Diagnostics.Debug.WriteLine($"? User logged in: {user.Id}");

                    return (true, token, null);
                }
                else
                {
                    return (false, null, "Invalid OTP. Please try again.");
                }
            }
            catch
            {
                // If JSON parsing fails, check if response indicates success
                if (content.Contains("OTP Matched") || content.Contains("Success"))
                {
                    // Proceed with user creation/login
                    var userRepo = _unitOfWork.Repository<User>();
                    var user = await userRepo.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);

                    if (user == null)
                    {
                        user = new User
                        {
                            PhoneNumber = phoneNumber,
                            CreatedAt = DateTime.UtcNow
                        };
                        await userRepo.AddAsync(user);
                    }

                    user.LastLoginAt = DateTime.UtcNow;
                    var token = await GenerateSessionTokenAsync(user.Id);
                    user.SessionToken = token;

                    await userRepo.UpdateAsync(user);
                    await _unitOfWork.SaveChangesAsync();

                    await _secureStorage.SetAsync("session_token", token);
                    await _secureStorage.SetAsync("user_id", user.Id.ToString());
                    await _secureStorage.SetAsync("phone_number", phoneNumber);

                    return (true, token, null);
                }
                
                return (false, null, "Invalid OTP. Please try again.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error verifying OTP: {ex.Message}");
            return (false, null, $"Error verifying OTP: {ex.Message}");
        }
    }

    public async Task<string> GenerateSessionTokenAsync(int userId)
    {
        // Generate a secure random token
        var bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }

        var token = Convert.ToBase64String(bytes);
        var timestamp = DateTime.UtcNow.Ticks.ToString();
        var combined = $"{userId}:{timestamp}:{token}";

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(combined));
    }

    public async Task<bool> ValidateSessionTokenAsync(string token)
    {
        try
        {
            var storedToken = await _secureStorage.GetAsync("session_token");
            if (string.IsNullOrEmpty(storedToken))
            {
                return false;
            }

            // Validate token hasn't expired (30 days)
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var parts = decoded.Split(':');
            
            if (parts.Length != 3)
            {
                return false;
            }

            var timestamp = long.Parse(parts[1]);
            var tokenDate = new DateTime(timestamp);
            var daysSinceCreation = (DateTime.UtcNow - tokenDate).TotalDays;

            return daysSinceCreation <= 30 && token == storedToken;
        }
        catch
        {
            return false;
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
    /// Response model for 2Factor send OTP
    /// </summary>
    private class TwoFactorSendResponse
    {
        public string? Status { get; set; }
        public string? Details { get; set; }
    }

    /// <summary>
    /// Response model for 2Factor verify OTP
    /// </summary>
    private class TwoFactorVerifyResponse
    {
        public string? Status { get; set; }
        public string? Details { get; set; }
    }
}
