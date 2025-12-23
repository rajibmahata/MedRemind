using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MedRemind.Core.Configuration;
using MedRemind.Core.Interfaces;

namespace MedRemind.Mobile.Services;

/// <summary>
/// Environment-specific configuration service with encrypted storage
/// Keys are stored per environment and encrypted with AES-256
/// </summary>
public class EnvironmentConfigService : IEnvironmentConfigService
{
    private const string ConfigKey = "EnvironmentConfig_Encrypted";
    private const string CurrentEnvKey = "CurrentEnvironment";
    private readonly IConfigurationService _configService;
    private readonly byte[] _encryptionKey;

    // Cache to avoid frequent decryption
    private EnvironmentConfig? _cachedConfig;
    private DateTime _cacheTimestamp;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    public EnvironmentConfigService(IConfigurationService configService)
    {
        _configService = configService;
        _encryptionKey = GenerateEncryptionKey();
    }

    public async Task<EnvironmentType> GetCurrentEnvironmentAsync()
    {
        var envString = await _configService.GetAsync(CurrentEnvKey);
        
        if (Enum.TryParse<EnvironmentType>(envString, out var environment))
        {
            return environment;
        }

        // Default to Development
        return EnvironmentType.Development;
    }

    public async Task SetCurrentEnvironmentAsync(EnvironmentType environment)
    {
        await _configService.SetAsync(CurrentEnvKey, environment.ToString());
        InvalidateCache();
        
        System.Diagnostics.Debug.WriteLine($"? Environment switched to: {environment}");
    }

    public async Task<ApiKeys> GetCurrentApiKeysAsync()
    {
        var currentEnv = await GetCurrentEnvironmentAsync();
        return await GetApiKeysForEnvironmentAsync(currentEnv);
    }

    public async Task<ApiKeys> GetApiKeysForEnvironmentAsync(EnvironmentType environment)
    {
        var config = await GetEnvironmentConfigAsync();
        
        if (config.EnvironmentKeys.TryGetValue(environment, out var keys))
        {
            return keys.Clone();
        }

        return new ApiKeys();
    }

    public async Task SetApiKeysForEnvironmentAsync(EnvironmentType environment, ApiKeys keys)
    {
        var config = await GetEnvironmentConfigAsync();
        
        keys.LastUpdated = DateTime.UtcNow;
        config.SetEnvironmentKeys(environment, keys);
        
        await SaveEnvironmentConfigAsync(config);
        
        System.Diagnostics.Debug.WriteLine($"? API keys updated for environment: {environment}");
    }

    public async Task UpdateOpenAIKeyAsync(string apiKey)
    {
        var currentEnv = await GetCurrentEnvironmentAsync();
        var keys = await GetApiKeysForEnvironmentAsync(currentEnv);
        
        keys.OpenAI_APIKey = apiKey;
        keys.LastUpdated = DateTime.UtcNow;
        keys.Version++;
        
        await SetApiKeysForEnvironmentAsync(currentEnv, keys);
    }

    public async Task UpdateTwoFactorKeyAsync(string apiKey)
    {
        var currentEnv = await GetCurrentEnvironmentAsync();
        var keys = await GetApiKeysForEnvironmentAsync(currentEnv);
        
        keys.TwoFactor_APIKey = apiKey;
        keys.LastUpdated = DateTime.UtcNow;
        keys.Version++;
        
        await SetApiKeysForEnvironmentAsync(currentEnv, keys);
    }

    public async Task<EnvironmentConfig> GetEnvironmentConfigAsync()
    {
        // Check cache first
        if (_cachedConfig != null && 
            DateTime.UtcNow - _cacheTimestamp < _cacheExpiration)
        {
            return _cachedConfig;
        }

        try
        {
            var encryptedData = await _configService.GetAsync(ConfigKey);
            
            if (string.IsNullOrEmpty(encryptedData))
            {
                // First time - create default configuration
                var defaultConfig = EnvironmentDefaults.CreateDefault();
                await SaveEnvironmentConfigAsync(defaultConfig);
                return defaultConfig;
            }

            // Decrypt and deserialize
            var decryptedJson = DecryptData(encryptedData);
            var config = JsonSerializer.Deserialize<EnvironmentConfig>(decryptedJson);
            
            if (config == null)
            {
                return EnvironmentDefaults.CreateDefault();
            }

            // Update cache
            _cachedConfig = config;
            _cacheTimestamp = DateTime.UtcNow;
            
            return config;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error loading environment config: {ex.Message}");
            return EnvironmentDefaults.CreateDefault();
        }
    }

    public async Task SaveEnvironmentConfigAsync(EnvironmentConfig config)
    {
        try
        {
            // Serialize to JSON
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            // Encrypt
            var encryptedData = EncryptData(json);
            
            // Save to secure storage
            await _configService.SetAsync(ConfigKey, encryptedData);
            
            // Update cache
            _cachedConfig = config;
            _cacheTimestamp = DateTime.UtcNow;
            
            System.Diagnostics.Debug.WriteLine("? Environment configuration saved successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error saving environment config: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> IsCurrentEnvironmentConfiguredAsync()
    {
        var keys = await GetCurrentApiKeysAsync();
        return keys.IsConfigured();
    }

    public async Task RotateApiKeysAsync(EnvironmentType environment)
    {
        var keys = await GetApiKeysForEnvironmentAsync(environment);
        keys.Version++;
        keys.LastUpdated = DateTime.UtcNow;
        
        await SetApiKeysForEnvironmentAsync(environment, keys);
        
        System.Diagnostics.Debug.WriteLine($"? API keys rotated for {environment} - Version: {keys.Version}");
    }

    public async Task<string> ExportConfigurationAsync()
    {
        var config = await GetEnvironmentConfigAsync();
        var json = JsonSerializer.Serialize(config);
        
        // Double encryption for export
        var encrypted = EncryptData(json);
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(encrypted));
        
        return base64;
    }

    public async Task ImportConfigurationAsync(string encryptedConfig)
    {
        try
        {
            var encrypted = Encoding.UTF8.GetString(Convert.FromBase64String(encryptedConfig));
            var json = DecryptData(encrypted);
            
            var config = JsonSerializer.Deserialize<EnvironmentConfig>(json);
            
            if (config != null)
            {
                await SaveEnvironmentConfigAsync(config);
                System.Diagnostics.Debug.WriteLine("? Configuration imported successfully");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error importing configuration: {ex.Message}");
            throw new InvalidOperationException("Failed to import configuration. Invalid format or corrupted data.", ex);
        }
    }

    #region Private Methods

    private byte[] GenerateEncryptionKey()
    {
        // Generate device-specific encryption key
        var deviceId = DeviceInfo.Current.Model + DeviceInfo.Current.Manufacturer;
        var salt = "MedRemind_EnvConfig_2024_Secure";
        
        var keySource = $"{deviceId}_{salt}";
        
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(keySource));
    }

    private string EncryptData(string plainText)
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

    private string DecryptData(string cipherText)
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

    private void InvalidateCache()
    {
        _cachedConfig = null;
        _cacheTimestamp = DateTime.MinValue;
    }

    #endregion
}
