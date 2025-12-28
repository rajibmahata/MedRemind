using MedRemind.Core.Interfaces;

namespace MedRemind.Mobile.Services;

/// <summary>
/// MAUI platform-specific SecureStorage implementation
/// Uses Microsoft.Maui.Storage.SecureStorage for encrypted key-value storage
/// </summary>
public class MauiSecureStorageService : ISecureStorageService
{
    public async Task SetAsync(string key, string value)
    {
        try
        {
            await SecureStorage.Default.SetAsync(key, value);
            System.Diagnostics.Debug.WriteLine($"?? SecureStorage: Stored '{key}' = '{(key.Contains("token") ? "***" : value)}'");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? SecureStorage: Error storing '{key}': {ex.Message}");
            throw;
        }
    }

    public async Task<string?> GetAsync(string key)
    {
        try
        {
            var value = await SecureStorage.Default.GetAsync(key);
            System.Diagnostics.Debug.WriteLine($"?? SecureStorage: Retrieved '{key}' = {(value != null ? (key.Contains("token") ? "***" : value) : "NULL")}");
            return value;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? SecureStorage: Error retrieving '{key}': {ex.Message}");
            return null;
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            SecureStorage.Default.Remove(key);
            System.Diagnostics.Debug.WriteLine($"?? SecureStorage: Removed '{key}'");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? SecureStorage: Error removing '{key}': {ex.Message}");
        }
    }

    public async Task ClearAllAsync()
    {
        try
        {
            SecureStorage.Default.RemoveAll();
            System.Diagnostics.Debug.WriteLine($"?? SecureStorage: Cleared all data");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? SecureStorage: Error clearing: {ex.Message}");
        }
    }
}
