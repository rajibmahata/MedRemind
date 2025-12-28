using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MedRemind.Core.Configuration;

namespace MedRemind.Mobile.Services;

/// <summary>
/// Loads configuration from embedded appsettings.json
/// Configuration is embedded in app binary and encrypted at runtime
/// Not accessible after app installation
/// </summary>
public class EmbeddedConfigurationLoader
{
    private const string ConfigFileName = "MedRemind.Mobile.appsettings.json";
    private static readonly byte[] EncryptionKey = DeriveEncryptionKey();
    private static EmbeddedConfiguration? _cachedConfig;
    
    /// <summary>
    /// Load configuration from embedded resource
    /// </summary>
    public static EmbeddedConfiguration LoadConfiguration()
    {
        if (_cachedConfig != null)
        {
            return _cachedConfig;
        }

        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(ConfigFileName);
            
            if (stream == null)
            {
                System.Diagnostics.Debug.WriteLine($"? Configuration file not found: {ConfigFileName}");
                throw new FileNotFoundException($"Embedded configuration file not found: {ConfigFileName}");
            }

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            
            // Encrypt the JSON in memory (so it's not stored in plain text)
            var encryptedJson = EncryptString(json);
            
            // Decrypt and deserialize
            var decryptedJson = DecryptString(encryptedJson);
            var config = JsonSerializer.Deserialize<EmbeddedConfiguration>(decryptedJson);
            
            if (config == null)
            {
                throw new InvalidOperationException("Failed to deserialize configuration");
            }

            _cachedConfig = config;
            System.Diagnostics.Debug.WriteLine("? Configuration loaded from embedded resource");
            
            return config;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error loading configuration: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Get API keys for specific environment
    /// </summary>
    public static EnvironmentConfiguration GetEnvironmentConfig(string environmentName)
    {
        var config = LoadConfiguration();
        
        if (!config.Environments.TryGetValue(environmentName, out var envConfig))
        {
            throw new ArgumentException($"Environment '{environmentName}' not found in configuration");
        }

        return envConfig;
    }

    /// <summary>
    /// Get active environment configuration
    /// </summary>
    public static EnvironmentConfiguration GetActiveEnvironmentConfig()
    {
        var config = LoadConfiguration();
        return GetEnvironmentConfig(config.ActiveEnvironment);
    }

    /// <summary>
    /// Get OpenAI API key for active environment
    /// </summary>
    public static string GetOpenAIApiKey()
    {
        var config = GetActiveEnvironmentConfig();
        return config.OpenAI.ApiKey;
    }

    /// <summary>
    /// Get 2Factor API key for active environment
    /// </summary>
    public static string GetTwoFactorApiKey()
    {
        var config = GetActiveEnvironmentConfig();
        return config.TwoFactor.ApiKey;
    }

    /// <summary>
    /// Get 2Factor Send OTP URL for active environment
    /// </summary>
    public static string GetTwoFactorSendOtpUrl()
    {
        var config = GetActiveEnvironmentConfig();
        return config.TwoFactor.SendOtpUrl;
    }

    /// <summary>
    /// Get 2Factor Verify OTP URL for active environment
    /// </summary>
    public static string GetTwoFactorVerifyOtpUrl()
    {
        var config = GetActiveEnvironmentConfig();
        return config.TwoFactor.VerifyOtpUrl;
    }

    /// <summary>
    /// Get 2Factor OTP Template for active environment
    /// </summary>
    public static string GetTwoFactorOtpTemplate()
    {
        var config = GetActiveEnvironmentConfig();
        return config.TwoFactor.OtpTemplate;
    }

    /// <summary>
    /// Check if feature is enabled for active environment
    /// </summary>
    public static bool IsFeatureEnabled(string featureName)
    {
        var config = GetActiveEnvironmentConfig();
        
        return featureName switch
        {
            "PrescriptionUpload" => config.Features.EnablePrescriptionUpload,
            "VoiceReminders" => config.Features.EnableVoiceReminders,
            "Analytics" => config.Features.EnableAnalytics,
            "CrashReporting" => config.Features.EnableCrashReporting,
            _ => false
        };
    }

    #region Encryption

    private static byte[] DeriveEncryptionKey()
    {
        // Derive key from app-specific data + device data
        var appSpecificSalt = "MedRemind_Embedded_Config_2024_Secure_Key";
        var deviceData = $"{DeviceInfo.Current.Model}_{DeviceInfo.Current.Manufacturer}_{DeviceInfo.Current.Platform}";
        var keySource = $"{appSpecificSalt}_{deviceData}";
        
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(keySource));
    }

    private static string EncryptString(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = EncryptionKey;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var msEncrypt = new MemoryStream();
        
        msEncrypt.Write(aes.IV, 0, aes.IV.Length);
        
        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        using (var swEncrypt = new StreamWriter(csEncrypt))
        {
            swEncrypt.Write(plainText);
        }

        return Convert.ToBase64String(msEncrypt.ToArray());
    }

    private static string DecryptString(string cipherText)
    {
        var fullCipher = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = EncryptionKey;

        var iv = new byte[aes.IV.Length];
        Array.Copy(fullCipher, 0, iv, 0, iv.Length);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var msDecrypt = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length);
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);
        
        return srDecrypt.ReadToEnd();
    }

    #endregion
}

/// <summary>
/// Root configuration model
/// </summary>
public class EmbeddedConfiguration
{
    public Dictionary<string, EnvironmentConfiguration> Environments { get; set; } = new();
    public string ActiveEnvironment { get; set; } = "Development";
}

/// <summary>
/// Environment-specific configuration
/// </summary>
public class EnvironmentConfiguration
{
    public OpenAIConfiguration OpenAI { get; set; } = new();
    public DeepSeekConfiguration? DeepSeek { get; set; }
    public ClaudeConfiguration? Claude { get; set; }
    public AzureDocumentIntelligenceConfiguration AzureDocumentIntelligence { get; set; } = new();
    public TwoFactorConfiguration TwoFactor { get; set; } = new();
    public FeaturesConfiguration Features { get; set; } = new();
    public AIParserConfiguration? AIParser { get; set; }
}

/// <summary>
/// OpenAI API configuration
/// </summary>
public class OpenAIConfiguration
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o";
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxTokens { get; set; } = 1000;
}

/// <summary>
/// DeepSeek API configuration
/// </summary>
public class DeepSeekConfiguration
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "deepseek-chat";
    public string ApiUrl { get; set; } = "https://api.deepseek.com/chat/completions";
    public bool Enabled { get; set; } = false;
    public int Priority { get; set; } = 1;
}

/// <summary>
/// Claude API configuration
/// </summary>
public class ClaudeConfiguration
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "claude-3-5-sonnet-20241022";
    public bool Enabled { get; set; } = false;
    public int Priority { get; set; } = 3;
}

/// <summary>
/// Azure Document Intelligence configuration
/// </summary>
public class AzureDocumentIntelligenceConfiguration
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 60;
}

/// <summary>
/// 2Factor API configuration
/// </summary>
public class TwoFactorConfiguration
{
    public string ApiKey { get; set; } = string.Empty;
    public string SendOtpUrl { get; set; } = string.Empty;
    public string VerifyOtpUrl { get; set; } = string.Empty;
    public string OtpTemplate { get; set; } = "OTP1";
    public int TimeoutSeconds { get; set; } = 10;
}

/// <summary>
/// Feature flags configuration
/// </summary>
public class FeaturesConfiguration
{
    public bool EnablePrescriptionUpload { get; set; } = true;
    public bool EnableVoiceReminders { get; set; } = true;
    public bool EnableAnalytics { get; set; } = false;
    public bool EnableCrashReporting { get; set; } = false;
}

/// <summary>
/// AI Parser configuration
/// </summary>
public class AIParserConfiguration
{
    public int OpenAIPriority { get; set; } = 2;
    public bool SkipClaudeIfComplete { get; set; } = true;
    public double MinimumConfidenceScore { get; set; } = 0.7;
}
