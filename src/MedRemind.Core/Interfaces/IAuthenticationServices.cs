using MedRemind.Core.DTOs;

namespace MedRemind.Core.Interfaces;

public interface IPrescriptionReaderService
{
    Task<PrescriptionReadResult> ReadPrescriptionAsync(string imagePath, CancellationToken cancellationToken = default);
    Task<PrescriptionReadResult> ReadPrescriptionFromBase64Async(string base64Image, CancellationToken cancellationToken = default);
}

public interface IValidationAgentService
{
    Task<List<ValidationWarning>> ValidateMedicationsAsync(List<MedicationData> medications, CancellationToken cancellationToken = default);
    double CalculateConfidenceScore(MedicationData medication);
}

public interface IAuthenticationService
{
    Task<(bool Success, string? ErrorMessage)> SendOtpAsync(string phoneNumber, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Token, string? ErrorMessage)> VerifyOtpAsync(string phoneNumber, string otp, CancellationToken cancellationToken = default);
    Task<string> GenerateSessionTokenAsync(int userId);
    Task<bool> ValidateSessionTokenAsync(string token);
}

public interface IBiometricService
{
    Task<bool> IsBiometricAvailableAsync();
    Task<(bool Success, string? ErrorMessage)> AuthenticateAsync(string reason, CancellationToken cancellationToken = default);
    Task<bool> EnrollBiometricAsync();
}

public interface ISecureStorageService
{
    Task SetAsync(string key, string value);
    Task<string?> GetAsync(string key);
    Task RemoveAsync(string key);
    Task ClearAllAsync();
}
