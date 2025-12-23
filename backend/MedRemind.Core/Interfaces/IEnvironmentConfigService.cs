using MedRemind.Core.Configuration;

namespace MedRemind.Core.Interfaces;

/// <summary>
/// Environment-specific configuration service for API keys
/// </summary>
public interface IEnvironmentConfigService
{
    /// <summary>
    /// Get current environment
    /// </summary>
    Task<EnvironmentType> GetCurrentEnvironmentAsync();

    /// <summary>
    /// Set current environment
    /// </summary>
    Task SetCurrentEnvironmentAsync(EnvironmentType environment);

    /// <summary>
    /// Get API keys for current environment
    /// </summary>
    Task<ApiKeys> GetCurrentApiKeysAsync();

    /// <summary>
    /// Get API keys for specific environment
    /// </summary>
    Task<ApiKeys> GetApiKeysForEnvironmentAsync(EnvironmentType environment);

    /// <summary>
    /// Set API keys for specific environment
    /// </summary>
    Task SetApiKeysForEnvironmentAsync(EnvironmentType environment, ApiKeys keys);

    /// <summary>
    /// Update specific API key for current environment
    /// </summary>
    Task UpdateOpenAIKeyAsync(string apiKey);

    /// <summary>
    /// Update 2Factor API key for current environment
    /// </summary>
    Task UpdateTwoFactorKeyAsync(string apiKey);

    /// <summary>
    /// Get complete environment configuration
    /// </summary>
    Task<EnvironmentConfig> GetEnvironmentConfigAsync();

    /// <summary>
    /// Save complete environment configuration
    /// </summary>
    Task SaveEnvironmentConfigAsync(EnvironmentConfig config);

    /// <summary>
    /// Check if current environment is configured
    /// </summary>
    Task<bool> IsCurrentEnvironmentConfiguredAsync();

    /// <summary>
    /// Rotate API keys (increment version)
    /// </summary>
    Task RotateApiKeysAsync(EnvironmentType environment);

    /// <summary>
    /// Export environment configuration (encrypted)
    /// </summary>
    Task<string> ExportConfigurationAsync();

    /// <summary>
    /// Import environment configuration (encrypted)
    /// </summary>
    Task ImportConfigurationAsync(string encryptedConfig);
}
