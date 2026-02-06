namespace MedRemind.Core.Interfaces;

/// <summary>
/// Interface for Email communication service
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send OTP via Email
    /// </summary>
    /// <param name="email">Email address</param>
    /// <param name="otp">OTP code to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status and error message if any</returns>
    Task<(bool Success, string? ErrorMessage)> SendOtpAsync(
        string email, 
        string otp, 
        CancellationToken cancellationToken = default);
}
