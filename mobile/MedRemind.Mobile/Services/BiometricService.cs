using MedRemind.Core.Interfaces;

namespace MedRemind.Mobile.Services;

/// <summary>
/// Simple biometric service implementation
/// Note: Full biometric implementation requires platform-specific code
/// For now, this provides a stub that can be extended
/// </summary>
public class BiometricService : IBiometricService
{
    private readonly ISecureStorageService _secureStorage;
    private const string BiometricEnabledKey = "biometric_enabled";

    public BiometricService(ISecureStorageService secureStorage)
    {
        _secureStorage = secureStorage;
    }

    public async Task<bool> IsBiometricAvailableAsync()
    {
        try
        {
            // Check if device supports biometric authentication
            // For now, return false - can be extended with platform-specific implementation
            return await Task.FromResult(false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking biometric availability: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> IsBiometricEnabledAsync()
    {
        var enabled = await _secureStorage.GetAsync(BiometricEnabledKey);
        return enabled == "true";
    }

    public async Task<bool> EnableBiometricAsync()
    {
        try
        {
            // Check if biometric is available
            if (!await IsBiometricAvailableAsync())
            {
                return false;
            }

            // Enable biometric
            await _secureStorage.SetAsync(BiometricEnabledKey, "true");
            System.Diagnostics.Debug.WriteLine("? Biometric authentication enabled");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error enabling biometric: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> EnrollBiometricAsync()
    {
        // Same as EnableBiometricAsync for now
        return await EnableBiometricAsync();
    }

    public async Task DisableBiometricAsync()
    {
        await _secureStorage.RemoveAsync(BiometricEnabledKey);
        System.Diagnostics.Debug.WriteLine("?? Biometric authentication disabled");
    }

    public async Task<(bool Success, string? ErrorMessage)> AuthenticateAsync(string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if biometric is available
            if (!await IsBiometricAvailableAsync())
            {
                return (false, "Biometric authentication not available");
            }

            // Check if biometric is enabled
            if (!await IsBiometricEnabledAsync())
            {
                return (false, "Biometric authentication not enabled");
            }

            // For now, return success (stub implementation)
            // TODO: Implement platform-specific biometric authentication
            System.Diagnostics.Debug.WriteLine($"?? Biometric authentication stub called");
            return (false, "Biometric authentication not yet implemented");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Biometric authentication error: {ex.Message}");
            return (false, ex.Message);
        }
    }

    public async Task<string> GetBiometricTypeAsync()
    {
        try
        {
            // TODO: Implement platform-specific biometric type detection
            return await Task.FromResult("Biometric");
        }
        catch
        {
            return "Unknown";
        }
    }
}
