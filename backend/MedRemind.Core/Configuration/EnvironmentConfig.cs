namespace MedRemind.Core.Configuration;

/// <summary>
/// Environment-specific API configuration
/// Supports Development, Staging, and Production environments
/// </summary>
public class EnvironmentConfig
{
    /// <summary>
    /// Current environment
    /// </summary>
    public EnvironmentType Environment { get; set; } = EnvironmentType.Development;

    /// <summary>
    /// Environment-specific API keys
    /// </summary>
    public Dictionary<EnvironmentType, ApiKeys> EnvironmentKeys { get; set; } = new();

    /// <summary>
    /// Get API keys for current environment
    /// </summary>
    public ApiKeys GetCurrentEnvironmentKeys()
    {
        if (EnvironmentKeys.TryGetValue(Environment, out var keys))
        {
            return keys;
        }

        return new ApiKeys();
    }

    /// <summary>
    /// Set API keys for specific environment
    /// </summary>
    public void SetEnvironmentKeys(EnvironmentType env, ApiKeys keys)
    {
        EnvironmentKeys[env] = keys;
    }
}

/// <summary>
/// Environment types
/// </summary>
public enum EnvironmentType
{
    Development = 0,
    Staging = 1,
    Production = 2
}

/// <summary>
/// API Keys for external services
/// </summary>
public class ApiKeys
{
    /// <summary>
    /// OpenAI API Key for prescription reading
    /// </summary>
    public string OpenAI_APIKey { get; set; } = string.Empty;

    /// <summary>
    /// 2Factor.in API Key for SMS OTP
    /// </summary>
    public string TwoFactor_APIKey { get; set; } = string.Empty;

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Version number for key rotation tracking
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Check if keys are configured
    /// </summary>
    public bool IsConfigured()
    {
        return !string.IsNullOrEmpty(OpenAI_APIKey);
    }

    /// <summary>
    /// Clone keys
    /// </summary>
    public ApiKeys Clone()
    {
        return new ApiKeys
        {
            OpenAI_APIKey = this.OpenAI_APIKey,
            TwoFactor_APIKey = this.TwoFactor_APIKey,
            LastUpdated = this.LastUpdated,
            Version = this.Version
        };
    }
}

/// <summary>
/// Environment configuration defaults
/// </summary>
public static class EnvironmentDefaults
{
    /// <summary>
    /// Default environment configurations
    /// </summary>
    public static EnvironmentConfig CreateDefault()
    {
        return new EnvironmentConfig
        {
            Environment = EnvironmentType.Development,
            EnvironmentKeys = new Dictionary<EnvironmentType, ApiKeys>
            {
                { 
                    EnvironmentType.Development, 
                    new ApiKeys 
                    { 
                        OpenAI_APIKey = string.Empty,
                        TwoFactor_APIKey = string.Empty,
                        Version = 1
                    } 
                },
                { 
                    EnvironmentType.Staging, 
                    new ApiKeys 
                    { 
                        OpenAI_APIKey = string.Empty,
                        TwoFactor_APIKey = string.Empty,
                        Version = 1
                    } 
                },
                { 
                    EnvironmentType.Production, 
                    new ApiKeys 
                    { 
                        OpenAI_APIKey = string.Empty,
                        TwoFactor_APIKey = string.Empty,
                        Version = 1
                    } 
                }
            }
        };
    }

    /// <summary>
    /// Get environment display name
    /// </summary>
    public static string GetDisplayName(EnvironmentType env)
    {
        return env switch
        {
            EnvironmentType.Development => "Development",
            EnvironmentType.Staging => "Staging",
            EnvironmentType.Production => "Production",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Get environment icon
    /// </summary>
    public static string GetIcon(EnvironmentType env)
    {
        return env switch
        {
            EnvironmentType.Development => "??",
            EnvironmentType.Staging => "??",
            EnvironmentType.Production => "??",
            _ => "?"
        };
    }

    /// <summary>
    /// Get environment description
    /// </summary>
    public static string GetDescription(EnvironmentType env)
    {
        return env switch
        {
            EnvironmentType.Development => "For testing with test APIs",
            EnvironmentType.Staging => "For pre-production testing",
            EnvironmentType.Production => "For live app with real users",
            _ => "Unknown environment"
        };
    }
}
