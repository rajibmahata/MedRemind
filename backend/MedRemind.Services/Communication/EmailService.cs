using System.Net;
using System.Net.Mail;
using MedRemind.Core.Interfaces;
using MedRemind.Core.Services;
using Microsoft.Extensions.Logging;

namespace MedRemind.Services.Communication;

/// <summary>
/// Service for sending email messages via SMTP
/// </summary>
public class EmailService : IEmailService
{
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly bool _enableSsl;
    private readonly ILogger<EmailService>? _logger;
    private readonly EmailTemplateService _templateService;

    public EmailService(
        string smtpHost,
        int smtpPort,
        string smtpUsername,
        string smtpPassword,
        string fromEmail,
        string fromName,
        bool enableSsl = true,
        ILogger<EmailService>? logger = null)
    {
        _smtpHost = smtpHost ?? throw new ArgumentNullException(nameof(smtpHost));
        _smtpPort = smtpPort;
        _smtpUsername = smtpUsername ?? throw new ArgumentNullException(nameof(smtpUsername));
        _smtpPassword = smtpPassword ?? throw new ArgumentNullException(nameof(smtpPassword));
        _fromEmail = fromEmail ?? throw new ArgumentNullException(nameof(fromEmail));
        _fromName = fromName ?? "MedRemind";
        _enableSsl = enableSsl;
        _logger = logger;
        _templateService = new EmailTemplateService();
    }

    /// <summary>
    /// Send OTP via Email using modern HTML template
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> SendOtpAsync(
        string email,
        string otp,
        CancellationToken cancellationToken = default)
    {
        return await SendOtpAsync(email, otp, "User", cancellationToken);
    }

    /// <summary>
    /// Send OTP via Email using modern HTML template with user name
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> SendOtpAsync(
        string email,
        string otp,
        string userName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate email format
            if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
            {
                return (false, "Invalid email address.");
            }

            // Validate OTP
            if (string.IsNullOrWhiteSpace(otp))
            {
                return (false, "OTP is required");
            }

            _logger?.LogInformation("?? Sending Email OTP to {Email}", email);

            // Get HTML from template
            var htmlBody = _templateService.GetOtpEmail(userName, otp);

            // Create email message
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_fromEmail, _fromName),
                Subject = "Your MedRemind OTP Code ??",
                Body = htmlBody,
                IsBodyHtml = true
            };
            mailMessage.To.Add(email);

            // Configure SMTP client
            using var smtpClient = new SmtpClient(_smtpHost, _smtpPort)
            {
                Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                EnableSsl = _enableSsl,
                Timeout = 30000 // 30 seconds
            };

            // Send email
            await smtpClient.SendMailAsync(mailMessage, cancellationToken);

            _logger?.LogInformation("? Email OTP sent successfully to {Email}", email);
            return (true, null);
        }
        catch (SmtpException ex)
        {
            _logger?.LogError(ex, "? SMTP error sending email OTP");
            return (false, $"Failed to send email: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "? Error sending email OTP");
            return (false, $"Error sending email: {ex.Message}");
        }
    }

    /// <summary>
    /// Send Welcome email to new user
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> SendWelcomeEmailAsync(
        string email,
        string userName,
        string phoneNumber,
        DateTime registrationDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate email format
            if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
            {
                return (false, "Invalid email address.");
            }

            _logger?.LogInformation("?? Sending Welcome email to {Email}", email);

            // Get HTML from template
            var htmlBody = _templateService.GetWelcomeEmail(
                userName, 
                email, 
                phoneNumber, 
                registrationDate);

            // Create email message
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_fromEmail, _fromName),
                Subject = "Welcome to MedRemind! ??",
                Body = htmlBody,
                IsBodyHtml = true
            };
            mailMessage.To.Add(email);

            // Configure SMTP client
            using var smtpClient = new SmtpClient(_smtpHost, _smtpPort)
            {
                Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                EnableSsl = _enableSsl,
                Timeout = 30000
            };

            // Send email
            await smtpClient.SendMailAsync(mailMessage, cancellationToken);

            _logger?.LogInformation("? Welcome email sent successfully to {Email}", email);
            return (true, null);
        }
        catch (SmtpException ex)
        {
            _logger?.LogError(ex, "? SMTP error sending welcome email");
            return (false, $"Failed to send email: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "? Error sending welcome email");
            return (false, $"Error sending email: {ex.Message}");
        }
    }

    /// <summary>
    /// Send Password Reset email
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> SendPasswordResetEmailAsync(
        string email,
        string userName,
        string resetToken,
        string resetLink,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate email format
            if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
            {
                return (false, "Invalid email address.");
            }

            _logger?.LogInformation("?? Sending Password Reset email to {Email}", email);

            // Get HTML from template
            var htmlBody = _templateService.GetPasswordResetEmail(userName, resetToken, resetLink);

            // Create email message
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_fromEmail, _fromName),
                Subject = "Reset Your MedRemind Password ??",
                Body = htmlBody,
                IsBodyHtml = true
            };
            mailMessage.To.Add(email);

            // Configure SMTP client
            using var smtpClient = new SmtpClient(_smtpHost, _smtpPort)
            {
                Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                EnableSsl = _enableSsl,
                Timeout = 30000
            };

            // Send email
            await smtpClient.SendMailAsync(mailMessage, cancellationToken);

            _logger?.LogInformation("? Password Reset email sent successfully to {Email}", email);
            return (true, null);
        }
        catch (SmtpException ex)
        {
            _logger?.LogError(ex, "? SMTP error sending password reset email");
            return (false, $"Failed to send email: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "? Error sending password reset email");
            return (false, $"Error sending email: {ex.Message}");
        }
    }

    /// <summary>
    /// Validate email format
    /// </summary>
    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
