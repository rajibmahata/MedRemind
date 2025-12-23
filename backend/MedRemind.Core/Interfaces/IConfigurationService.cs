namespace MedRemind.Core.Interfaces;

/// <summary>
/// Secure configuration service for managing encrypted app settings
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Initialize configuration on first app launch
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Get configuration value by key
    /// </summary>
    Task<string?> GetAsync(string key);

    /// <summary>
    /// Set configuration value (encrypted)
    /// </summary>
    Task SetAsync(string key, string value);

    /// <summary>
    /// Check if configuration exists
    /// </summary>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// Remove configuration value
    /// </summary>
    Task RemoveAsync(string key);

    /// <summary>
    /// Get strongly-typed configuration
    /// </summary>
    Task<T?> GetConfigurationAsync<T>() where T : class, new();

    /// <summary>
    /// Save strongly-typed configuration
    /// </summary>
    Task SaveConfigurationAsync<T>(T configuration) where T : class;

    /// <summary>
    /// Check if app is configured (first-time setup complete)
    /// </summary>
    Task<bool> IsConfiguredAsync();

    /// <summary>
    /// Get configuration version for migration support
    /// </summary>
    Task<int> GetConfigurationVersionAsync();

    /// <summary>
    /// Set configuration version
    /// </summary>
    Task SetConfigurationVersionAsync(int version);
}
