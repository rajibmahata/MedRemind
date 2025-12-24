using MedRemind.Core.Interfaces;

namespace MedRemind.Mobile.Services;

/// <summary>
/// Biometric authentication service - simplified for immediate functionality
/// TODO: Add platform-specific biometric APIs after adding required NuGet packages
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
            // Check if device is physical (not emulator)
            return await Task.FromResult(DeviceInfo.Current.DeviceType == DeviceType.Physical);
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
            if (!await IsBiometricAvailableAsync())
            {
                return false;
            }

            // Test authentication before enabling
            var testResult = await AuthenticateAsync("Verify your identity to enable biometric login", CancellationToken.None);
            if (!testResult.Success)
            {
                return false;
            }

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
            if (!await IsBiometricAvailableAsync())
            {
                return (false, "Biometric authentication not available on this device");
            }

            // Check if this is enrollment (skip enabled check)
            var isEnrollment = reason.Contains("enable", StringComparison.OrdinalIgnoreCase) || 
                              reason.Contains("verify your identity", StringComparison.OrdinalIgnoreCase);
            
            if (!isEnrollment && !await IsBiometricEnabledAsync())
            {
                return (false, "Biometric authentication not enabled");
            }

            // Use .NET MAUI DisplayAlert for now
            // TODO: Replace with platform-specific biometric prompts after adding NuGet packages
            return await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    var page = Application.Current?.Windows[0]?.Page;
                    if (page != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"?? Biometric prompt: {reason}");
                        
                        var result = await page.DisplayAlertAsync(
                            "Biometric Authentication",
                            reason + "\n\n(Simulated - waiting for biometric hardware integration)",
                            "Authenticate",
                            "Cancel"
                        );
                        
                        if (result)
                        {
                            System.Diagnostics.Debug.WriteLine("? Biometric authentication succeeded");
                            return (true, null);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("? Biometric authentication cancelled");
                            return (false, "Authentication cancelled");
                        }
                    }
                    return (false, "No page available");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"? Biometric error: {ex.Message}");
                    return (false, ex.Message);
                }
            });
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
#if ANDROID
            return await Task.FromResult("Fingerprint");
#elif IOS
            return await Task.FromResult("Face ID / Touch ID");
#else
            return await Task.FromResult("Biometric");
#endif
        }
        catch
        {
            return "Biometric";
        }
    }
}
