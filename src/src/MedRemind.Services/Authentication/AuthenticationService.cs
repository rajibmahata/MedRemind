using System.Security.Cryptography;
using System.Text;
using MedRemind.Core.Interfaces;
using MedRemind.Core.Models;

namespace MedRemind.Services.Authentication;

public class AuthenticationService : IAuthenticationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecureStorageService _secureStorage;
    private readonly string _twoFactorApiKey;
    private readonly HttpClient _httpClient;

    public AuthenticationService(
        IUnitOfWork unitOfWork,
        ISecureStorageService secureStorage,
        string twoFactorApiKey,
        HttpClient httpClient)
    {
        _unitOfWork = unitOfWork;
        _secureStorage = secureStorage;
        _twoFactorApiKey = twoFactorApiKey;
        _httpClient = httpClient;
    }

    public async Task<(bool Success, string? ErrorMessage)> SendOtpAsync(
        string phoneNumber, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate phone number format
            if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length != 10)
            {
                return (false, "Invalid phone number. Please enter a 10-digit phone number.");
            }

            // Call 2Factor.in API to send OTP
            var url = $"https://2factor.in/API/V1/{_twoFactorApiKey}/SMS/{phoneNumber}/AUTOGEN";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                return (false, $"Failed to send OTP: {error}");
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            // Store session ID for OTP verification if needed
            
            return (true, null);
        }
        catch (Exception ex)
        {
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
            // In production, verify with 2Factor.in API
            // For now, simple validation
            if (string.IsNullOrWhiteSpace(otp) || otp.Length != 6)
            {
                return (false, null, "Invalid OTP. Please enter a 6-digit code.");
            }

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

            return (true, token, null);
        }
        catch (Exception ex)
        {
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
}
