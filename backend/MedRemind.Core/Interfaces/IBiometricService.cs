namespace MedRemind.Core.Interfaces;

/// <summary>
/// Service for biometric authentication
/// </summary>
public interface IBiometricService
{
    /// <summary>
    /// Check if biometric authentication is available on device
    /// </summary>
    Task<bool> IsBiometricAvailableAsync();
    
    /// <summary>
    /// Check if biometric is enabled for user
    /// </summary>
    Task<bool> IsBiometricEnabledAsync();
    
    /// <summary>
    /// Enable biometric authentication
    /// </summary>
    Task<bool> EnableBiometricAsync();
    
    /// <summary>
    /// Disable biometric authentication
    /// </summary>
    Task DisableBiometricAsync();
    
    /// <summary>
    /// Authenticate user with biometric
    /// </summary>
    Task<(bool Success, string? ErrorMessage)> AuthenticateAsync(string reason, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Enroll biometric for user
    /// </summary>
    Task<bool> EnrollBiometricAsync();
    
    /// <summary>
    /// Get type of biometric (fingerprint, face, etc.)
    /// </summary>
    Task<string> GetBiometricTypeAsync();
}
