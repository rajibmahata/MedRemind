namespace MedRemind.Core.Interfaces;

/// <summary>
/// Interface for SMS communication service
/// </summary>
public interface ISmsService
{
    /// <summary>
    /// Send OTP via SMS
    /// </summary>
    /// <param name="phoneNumber">10-digit phone number</param>
    /// <param name="otp">OTP code to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status and error message if any</returns>
    Task<(bool Success, string? ErrorMessage)> SendOtpAsync(
        string phoneNumber, 
        string otp, 
        CancellationToken cancellationToken = default);
}
