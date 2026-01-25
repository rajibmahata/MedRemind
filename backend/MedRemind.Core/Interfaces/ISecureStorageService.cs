namespace MedRemind.Core.Interfaces;

/// <summary>
/// Service for secure storage operations
/// </summary>
public interface ISecureStorageService
{
    /// <summary>
    /// Store value securely
    /// </summary>
    Task SetAsync(string key, string value);
    
    /// <summary>
    /// Retrieve value from secure storage
    /// </summary>
    Task<string?> GetAsync(string key);
    
    /// <summary>
    /// Remove value from secure storage
    /// </summary>
    Task RemoveAsync(string key);
    
    /// <summary>
    /// Clear all values from secure storage
    /// </summary>
    Task ClearAllAsync();
}
