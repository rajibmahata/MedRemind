using Microsoft.AspNetCore.Http;

namespace MedRemind.Services.Storage;

/// <summary>
/// Interface for prescription file management operations
/// </summary>
public interface IPrescriptionFileManager
{
    Task<PrescriptionFileResult> SavePrescriptionFileAsync(IFormFile file, int userId, CancellationToken cancellationToken = default);
    Task<byte[]?> GetFileAsync(string fileName);
    Task<bool> DeleteFileAsync(string fileName);
    StorageStatistics GetStorageStatistics();
}
