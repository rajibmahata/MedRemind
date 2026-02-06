using System.Text.Json;
using MedRemind.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace MedRemind.Services.Communication;

/// <summary>
/// Service for sending SMS messages via 2Factor.in API
/// </summary>
public class SmsService : ISmsService
{
    private readonly HttpClient _httpClient;
    private readonly string _twoFactorApiKey;
    private readonly string _sendOtpUrl;
    private readonly string _otpTemplate;
    private readonly ILogger<SmsService>? _logger;

    public SmsService(
        HttpClient httpClient,
        string twoFactorApiKey,
        string? sendOtpUrl = null,
        string? otpTemplate = null,
        ILogger<SmsService>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _twoFactorApiKey = twoFactorApiKey ?? throw new ArgumentNullException(nameof(twoFactorApiKey));
        
        // Use provided URLs or default to 2Factor API format
        _sendOtpUrl = sendOtpUrl ?? "https://2factor.in/API/V1/{apiKey}/SMS/{phoneNumber}/{otpValue}/{templateName}";
        _otpTemplate = otpTemplate ?? "OTP1";
        _logger = logger;
    }

    /// <summary>
    /// Send OTP via SMS using 2Factor.in API
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> SendOtpAsync(
        string phoneNumber,
        string otp,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate phone number format (10 digits)
            if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length != 10)
            {
                return (false, "Invalid phone number. Please enter a 10-digit phone number.");
            }

            // Validate OTP
            if (string.IsNullOrWhiteSpace(otp))
            {
                return (false, "OTP is required");
            }

            // Format phone number with country code (+91 for India)
            var formattedPhone = $"+91{phoneNumber}";

            _logger?.LogInformation("?? Sending SMS OTP to {PhoneNumber}", phoneNumber);

            // Build URL from template
            var url = _sendOtpUrl
                .Replace("{apiKey}", _twoFactorApiKey)
                .Replace("{phoneNumber}", formattedPhone)
                .Replace("{otpValue}", otp)
                .Replace("{templateName}", _otpTemplate);

            // Call 2Factor.in API
            var response = await _httpClient.GetAsync(url, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            
            _logger?.LogInformation("?? 2Factor Response: {Response}", content);

            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogError("? Failed to send SMS: {StatusCode}", response.StatusCode);
                return (false, "Failed to send SMS. Please try again.");
            }

            try
            {
                // Parse response to check success
                var jsonResponse = JsonSerializer.Deserialize<TwoFactorSendResponse>(content);

                if (jsonResponse?.Status == "Success")
                {
                    _logger?.LogInformation("? SMS OTP sent successfully to {PhoneNumber}", phoneNumber);
                    return (true, null);
                }
                else
                {
                    _logger?.LogWarning("?? SMS send failed: {Details}", jsonResponse?.Details);
                    return (false, "Failed to send SMS. Please try again.");
                }
            }
            catch
            {
                // If JSON parsing fails, check if response indicates success
                if (content.Contains("Success", StringComparison.OrdinalIgnoreCase))
                {
                    _logger?.LogInformation("? SMS OTP sent (parsed from text response)");
                    return (true, null);
                }
                
                _logger?.LogWarning("?? Could not parse SMS response");
                return (false, "Failed to send SMS. Please try again.");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "? Error sending SMS OTP");
            return (false, $"Error sending SMS: {ex.Message}");
        }
    }

    /// <summary>
    /// Response model for 2Factor send OTP
    /// </summary>
    private class TwoFactorSendResponse
    {
        public string? Status { get; set; }
        public string? Details { get; set; }
    }
}
