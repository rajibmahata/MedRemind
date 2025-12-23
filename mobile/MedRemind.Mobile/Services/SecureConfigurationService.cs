using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MedRemind.Core.Interfaces;

namespace MedRemind.Mobile.Services;

/// <summary>
/// Secure configuration service with AES-256 encryption for sensitive data
/// Stores configuration in encrypted format in SecureStorage
/// </summary>
public class SecureConfigurationService : IConfigurationService
{
    private const string ConfigurationPrefix = "SecureConfig_";
    private const string VersionKey = "SecureConfig_Version";
    private const string InitializedKey = "SecureConfig_Initialized";
    private const int CurrentConfigVersion = 1;

    // Encryption key derived from device-specific data
    private readonly byte[] _encryptionKey;

    public SecureConfigurationService()
    {
        // Generate device-specific encryption key
        _encryptionKey = GenerateDeviceKey();
    }

    public async Task InitializeAsync()
    {
        try
        {
            var isInitialized = await IsConfiguredAsync();
            
            if (!isInitialized)
            {
                // First-time setup
                await SetConfigurationVersionAsync(CurrentConfigVersion);
                await SecureStorage.Default.SetAsync(InitializedKey, "true");
                
                // Set default configuration values
                await SetDefaultConfigurationAsync();
            }
            else
            {
                // Check for version updates and migrate if needed
                var version = await GetConfigurationVersionAsync();
                if (version < CurrentConfigVersion)
                {
                    await MigrateConfigurationAsync(version, CurrentConfigVersion);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Configuration initialization error: {ex.Message}");
            throw;
        }
    }

    public async Task<string?> GetAsync(string key)
    {
        try
        {
            var encryptedValue = await SecureStorage.Default.GetAsync(GetStorageKey(key));
            
            if (string.IsNullOrEmpty(encryptedValue))
                return null;

            return DecryptValue(encryptedValue);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting config '{key}': {ex.Message}");
            return null;
        }
    }

    public async Task SetAsync(string key, string value)
    {
        try
        {
            var encryptedValue = EncryptValue(value);
            await SecureStorage.Default.SetAsync(GetStorageKey(key), encryptedValue);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error setting config '{key}': {ex.Message}");
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        var value = await GetAsync(key);
        return !string.IsNullOrEmpty(value);
    }

    public Task RemoveAsync(string key)
    {
        SecureStorage.Default.Remove(GetStorageKey(key));
        return Task.CompletedTask;
    }

    public async Task<T?> GetConfigurationAsync<T>() where T : class, new()
    {
        try
        {
            var typeName = typeof(T).Name;
            var json = await GetAsync(typeName);
            
            if (string.IsNullOrEmpty(json))
                return null;

            return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting configuration {typeof(T).Name}: {ex.Message}");
            return null;
        }
    }

    public async Task SaveConfigurationAsync<T>(T configuration) where T : class
    {
        try
        {
            var typeName = typeof(T).Name;
            var json = JsonSerializer.Serialize(configuration, new JsonSerializerOptions
            {
                WriteIndented = false
            });
            
            await SetAsync(typeName, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving configuration {typeof(T).Name}: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> IsConfiguredAsync()
    {
        var initialized = await SecureStorage.Default.GetAsync(InitializedKey);
        return !string.IsNullOrEmpty(initialized);
    }

    public async Task<int> GetConfigurationVersionAsync()
    {
        var versionStr = await SecureStorage.Default.GetAsync(VersionKey);
        
        if (int.TryParse(versionStr, out var version))
            return version;
        
        return 0;
    }

    public async Task SetConfigurationVersionAsync(int version)
    {
        await SecureStorage.Default.SetAsync(VersionKey, version.ToString());
    }

    #region Private Methods

    private string GetStorageKey(string key)
    {
        return $"{ConfigurationPrefix}{key}";
    }

    private byte[] GenerateDeviceKey()
    {
        // Generate a device-specific key using device ID and app-specific salt
        var deviceId = DeviceInfo.Current.Model + DeviceInfo.Current.Manufacturer;
        var salt = "MedRemind_SecureConfig_2024"; // App-specific salt
        
        var keySource = $"{deviceId}_{salt}";
        
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(keySource));
    }

    private string EncryptValue(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _encryptionKey;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var msEncrypt = new MemoryStream();
        
        // Write IV first
        msEncrypt.Write(aes.IV, 0, aes.IV.Length);
        
        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        using (var swEncrypt = new StreamWriter(csEncrypt))
        {
            swEncrypt.Write(plainText);
        }

        return Convert.ToBase64String(msEncrypt.ToArray());
    }

    private string DecryptValue(string cipherText)
    {
        var fullCipher = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = _encryptionKey;

        // Extract IV from the beginning
        var iv = new byte[aes.IV.Length];
        Array.Copy(fullCipher, 0, iv, 0, iv.Length);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var msDecrypt = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length);
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);
        
        return srDecrypt.ReadToEnd();
    }

    private async Task SetDefaultConfigurationAsync()
    {
        // Set default values - these will be overwritten on first configuration
        await SetAsync("Environment", "Development");
        await SetAsync("OpenAI_APIKey", string.Empty);
        await SetAsync("2Factor_APIKey", string.Empty);
        await SetAsync("EnableAnalytics", "false");
        await SetAsync("EnableCrashReporting", "false");
        await SetAsync("MaxPrescriptionCacheSize", "50");
        await SetAsync("NotificationLeadTime", "30"); // minutes
    }

    private async Task MigrateConfigurationAsync(int fromVersion, int toVersion)
    {
        System.Diagnostics.Debug.WriteLine($"Migrating configuration from v{fromVersion} to v{toVersion}");

        // Add migration logic here when configuration schema changes
        // For now, just update version
        await SetConfigurationVersionAsync(toVersion);
    }

    #endregion
}
