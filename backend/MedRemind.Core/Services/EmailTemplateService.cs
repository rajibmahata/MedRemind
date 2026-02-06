using System.Reflection;

namespace MedRemind.Core.Services;

/// <summary>
/// Service for loading and processing email templates
/// </summary>
public class EmailTemplateService
{
    private readonly string _templateFolder;

    public EmailTemplateService()
    {
        // Get the path to the EmailTemplates folder
        var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        _templateFolder = Path.Combine(assemblyLocation ?? "", "EmailTemplates");
        
        // Fallback: try to find templates relative to the Core project
        if (!Directory.Exists(_templateFolder))
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            _templateFolder = Path.Combine(currentDirectory, "EmailTemplates");
        }
    }

    /// <summary>
    /// Get OTP email HTML with populated data
    /// </summary>
    public string GetOtpEmail(string userName, string otpCode)
    {
        var template = LoadTemplate("OtpEmail.html");
        
        var placeholders = new Dictionary<string, string>
        {
            { "{{UserName}}", userName ?? "User" },
            { "{{OtpCode}}", otpCode }
        };

        return ReplacePlaceholders(template, placeholders);
    }

    /// <summary>
    /// Get Welcome email HTML with populated data
    /// </summary>
    public string GetWelcomeEmail(
        string userName, 
        string userEmail, 
        string userPhone, 
        DateTime registrationDate,
        string appLink = "https://medremind.com/app")
    {
        var template = LoadTemplate("WelcomeEmail.html");
        
        var placeholders = new Dictionary<string, string>
        {
            { "{{UserName}}", userName ?? "User" },
            { "{{UserEmail}}", userEmail },
            { "{{UserPhone}}", userPhone },
            { "{{RegistrationDate}}", registrationDate.ToString("MMMM dd, yyyy") },
            { "{{AppLink}}", appLink }
        };

        return ReplacePlaceholders(template, placeholders);
    }

    /// <summary>
    /// Get Password Reset email HTML with populated data
    /// </summary>
    public string GetPasswordResetEmail(
        string userName,
        string resetToken,
        string resetLink)
    {
        var template = LoadTemplate("PasswordResetEmail.html");
        
        var placeholders = new Dictionary<string, string>
        {
            { "{{UserName}}", userName ?? "User" },
            { "{{ResetToken}}", resetToken },
            { "{{ResetLink}}", resetLink }
        };

        return ReplacePlaceholders(template, placeholders);
    }

    /// <summary>
    /// Load email template from file
    /// </summary>
    private string LoadTemplate(string templateName)
    {
        try
        {
            var templatePath = Path.Combine(_templateFolder, templateName);
            
            if (!File.Exists(templatePath))
            {
                // Try embedded resource as fallback
                return LoadEmbeddedTemplate(templateName);
            }

            return File.ReadAllText(templatePath);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to load email template: {templateName}", ex);
        }
    }

    /// <summary>
    /// Load template from embedded resources
    /// </summary>
    private string LoadEmbeddedTemplate(string templateName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"MedRemind.Core.EmailTemplates.{templateName}";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new FileNotFoundException($"Email template not found: {templateName}");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Replace placeholders in template with actual values
    /// </summary>
    private string ReplacePlaceholders(string template, Dictionary<string, string> placeholders)
    {
        var result = template;

        foreach (var placeholder in placeholders)
        {
            result = result.Replace(placeholder.Key, placeholder.Value);
        }

        return result;
    }

    /// <summary>
    /// Get available template names
    /// </summary>
    public List<string> GetAvailableTemplates()
    {
        if (!Directory.Exists(_templateFolder))
        {
            return new List<string>();
        }

        return Directory.GetFiles(_templateFolder, "*.html")
            .Select(Path.GetFileName)
            .Where(f => f != null)
            .Cast<string>()
            .ToList();
    }
}
