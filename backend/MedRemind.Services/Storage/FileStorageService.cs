using System.IO;
using MedRemind.Core.Configuration;
using MedRemind.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace MedRemind.Services.Storage;

/// <summary>
/// Cross-platform file storage service for managing OCR logs and prescription files
/// Supports both private app storage and optional public storage (for debugging)
/// </summary>
public class FileStorageService : IFileStorageService
{
    private readonly FileStorageConfiguration _config;
    private readonly string _appDataDirectory;
    private readonly string _ocrLogsDirectory;
    private readonly string _prescriptionsDirectory;
    private readonly string? _publicStorageDirectory;

    public FileStorageService(IOptions<FileStorageConfiguration> config)
    {
        _config = config.Value;

        // Get app-specific data directory (private storage)
        _appDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // Create directory paths
        _ocrLogsDirectory = Path.Combine(_appDataDirectory, _config.OcrLogsFolderName);
        _prescriptionsDirectory = Path.Combine(_appDataDirectory, _config.PrescriptionsFolderName);

        // Setup public storage if enabled (Android only)
        if (_config.CopyToPublicStorage)
        {
#if ANDROID
            // Public Documents folder: /storage/emulated/0/Documents/MedRemind_Debug
            var documentsPath = Android.OS.Environment.GetExternalStoragePublicDirectory(
                Android.OS.Environment.DirectoryDocuments)?.AbsolutePath;

            if (!string.IsNullOrEmpty(documentsPath))
            {
                _publicStorageDirectory = Path.Combine(documentsPath, _config.PublicStorageFolderName);
                System.Diagnostics.Debug.WriteLine($"📁 Public storage enabled: {_publicStorageDirectory}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Public storage path not available");
            }
#else
            System.Diagnostics.Debug.WriteLine("⚠️ Public storage is only available on Android");
#endif
        }

        // Ensure directories exist
        EnsureDirectoriesExist();

        System.Diagnostics.Debug.WriteLine($"📁 FileStorageService initialized:");
        System.Diagnostics.Debug.WriteLine($"   App Data Directory: {_appDataDirectory}");
        System.Diagnostics.Debug.WriteLine($"   OCR Logs Directory: {_ocrLogsDirectory}");
        System.Diagnostics.Debug.WriteLine($"   Prescriptions Directory: {_prescriptionsDirectory}");
        System.Diagnostics.Debug.WriteLine($"   File Logging Enabled: {_config.EnableFileLogging}");
        System.Diagnostics.Debug.WriteLine($"   Copy to Public Storage: {_config.CopyToPublicStorage}");
        System.Diagnostics.Debug.WriteLine($"   Max Log Files: {_config.MaxLogFiles}");
    }

    /// <summary>
    /// Ensure all necessary directories exist
    /// </summary>
    private void EnsureDirectoriesExist()
    {
        try
        {
            // Create app-specific directories
            if (!Directory.Exists(_ocrLogsDirectory))
            {
                Directory.CreateDirectory(_ocrLogsDirectory);
                System.Diagnostics.Debug.WriteLine($"✅ Created OCR logs directory: {_ocrLogsDirectory}");
            }

            if (!Directory.Exists(_prescriptionsDirectory))
            {
                Directory.CreateDirectory(_prescriptionsDirectory);
                System.Diagnostics.Debug.WriteLine($"✅ Created prescriptions directory: {_prescriptionsDirectory}");
            }

            // Create public storage directory if enabled
            if (_config.CopyToPublicStorage && !string.IsNullOrEmpty(_publicStorageDirectory))
            {
                if (!Directory.Exists(_publicStorageDirectory))
                {
                    Directory.CreateDirectory(_publicStorageDirectory);
                    System.Diagnostics.Debug.WriteLine($"✅ Created public storage directory: {_publicStorageDirectory}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Failed to create directories: {ex.Message}");
        }
    }

    /// <summary>
    /// Generate unique file name with optional timestamp
    /// Format: {Prefix}_{Date}_{UnixTimestamp} or {Prefix}_{Date}
    /// </summary>
    private string GenerateFileName(string prefix, string? customName = null)
    {
        if (!string.IsNullOrEmpty(customName))
        {
            return customName;
        }

        var date = DateTime.Now.ToString("yyyyMMdd");

        if (_config.IncludeTimestampInFileName)
        {
            var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return $"{prefix}_{date}_{unixTimestamp}";
        }

        return $"{prefix}_{date}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
    }

    /// <summary>
    /// Save OCR results (raw, normalized, JSON) to storage
    /// </summary>
    public async Task<(string rawPath, string normalizedPath, string jsonPath)> SaveOcrResultsAsync(
        string rawText,
        string normalizedText,
        string jsonContent,
        string? fileName = null)
    {
        if (!_config.EnableFileLogging)
        {
            System.Diagnostics.Debug.WriteLine("📁 File logging is disabled, skipping save");
            return (string.Empty, string.Empty, string.Empty);
        }

        try
        {
            var baseFileName = GenerateFileName(_config.OcrFilePrefix, fileName);

            // Save to private storage
            var rawPath = await SaveFileAsync(rawText, $"{baseFileName}_raw.txt", _ocrLogsDirectory);
            var normalizedPath = await SaveFileAsync(normalizedText, $"{baseFileName}_normalized.txt", _ocrLogsDirectory);
            var jsonPath = await SaveFileAsync(jsonContent, $"{baseFileName}_response.json", _ocrLogsDirectory);

            System.Diagnostics.Debug.WriteLine($"✅ Saved OCR results:");
            System.Diagnostics.Debug.WriteLine($"   Raw: {rawPath}");
            System.Diagnostics.Debug.WriteLine($"   Normalized: {normalizedPath}");
            System.Diagnostics.Debug.WriteLine($"   JSON: {jsonPath}");

            // Copy to public storage if enabled
            if (_config.CopyToPublicStorage && !string.IsNullOrEmpty(_publicStorageDirectory))
            {
                await CopyToPublicStorageAsync(rawPath, $"{baseFileName}_raw.txt");
                await CopyToPublicStorageAsync(normalizedPath, $"{baseFileName}_normalized.txt");
                await CopyToPublicStorageAsync(jsonPath, $"{baseFileName}_response.json");
            }

            // Cleanup old files
            await CleanupOldFilesAsync();

            return (rawPath, normalizedPath, jsonPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Failed to save OCR results: {ex.Message}");
            return (string.Empty, string.Empty, string.Empty);
        }
    }

    /// <summary>
    /// Save prescription image to storage
    /// </summary>
    public async Task<string> SavePrescriptionImageAsync(byte[] imageBytes, string? fileName = null)
    {
        if (!_config.EnableFileLogging)
        {
            return string.Empty;
        }

        try
        {
            var baseFileName = GenerateFileName("Prescription_Image", fileName);
            var fullFileName = $"{baseFileName}.jpg";
            var filePath = Path.Combine(_prescriptionsDirectory, fullFileName);

            await File.WriteAllBytesAsync(filePath, imageBytes);
            System.Diagnostics.Debug.WriteLine($"💾 Saved prescription image: {filePath}");

            // Copy to public storage if enabled
            if (_config.CopyToPublicStorage && !string.IsNullOrEmpty(_publicStorageDirectory))
            {
                await CopyToPublicStorageAsync(filePath, fullFileName);
            }

            return filePath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Failed to save prescription image: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Save text file to storage
    /// </summary>
    public async Task<string> SaveTextFileAsync(string content, string fileName, StorageFolderType folderType)
    {
        if (!_config.EnableFileLogging)
        {
            return string.Empty;
        }

        try
        {
            var directory = folderType == StorageFolderType.OcrLogs
                ? _ocrLogsDirectory
                : _prescriptionsDirectory;

            return await SaveFileAsync(content, fileName, directory);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Failed to save text file: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Save file to specified directory
    /// </summary>
    private async Task<string> SaveFileAsync(string content, string fileName, string directory)
    {
        var filePath = Path.Combine(directory, fileName);
        await File.WriteAllTextAsync(filePath, content);
        return filePath;
    }

    /// <summary>
    /// Copy file to public storage (for testing/debugging)
    /// </summary>
    private async Task CopyToPublicStorageAsync(string sourceFilePath, string fileName)
    {
        if (string.IsNullOrEmpty(_publicStorageDirectory) || !File.Exists(sourceFilePath))
        {
            return;
        }

        try
        {
            var publicFilePath = Path.Combine(_publicStorageDirectory, fileName);
            File.Copy(sourceFilePath, publicFilePath, overwrite: true);
            System.Diagnostics.Debug.WriteLine($"📤 Copied to public storage: {publicFilePath}");

#if ANDROID
            // Trigger media scan so file appears in file manager immediately
            await TriggerMediaScanAsync(publicFilePath);
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Failed to copy to public storage: {ex.Message}");
        }
    }

#if ANDROID
    /// <summary>
    /// Trigger media scan to make file visible in file manager (Android)
    /// </summary>
    private async Task TriggerMediaScanAsync(string filePath)
    {
        try
        {
            await Task.Run(() =>
            {
                var context = Android.App.Application.Context;
                Android.Media.MediaScannerConnection.ScanFile(
                    context,
                    new[] { filePath },
                    null,
                    null);
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Media scan failed: {ex.Message}");
        }
    }
#endif

    /// <summary>
    /// Delete old log files to maintain MaxLogFiles limit
    /// </summary>
    public async Task CleanupOldFilesAsync()
    {
        if (_config.MaxLogFiles <= 0)
        {
            return; // Cleanup disabled
        }

        try
        {
            await Task.Run(() =>
            {
                // Get all files in OCR logs directory
                var files = Directory.GetFiles(_ocrLogsDirectory)
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTimeUtc)
                    .ToList();

                // Delete files exceeding the limit
                var filesToDelete = files.Skip(_config.MaxLogFiles).ToList();

                if (filesToDelete.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"🗑️ Cleaning up {filesToDelete.Count} old file(s)...");

                    foreach (var file in filesToDelete)
                    {
                        try
                        {
                            file.Delete();
                            System.Diagnostics.Debug.WriteLine($"   Deleted: {file.Name}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"   Failed to delete {file.Name}: {ex.Message}");
                        }
                    }
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Cleanup failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Get the full path to the OCR logs directory
    /// </summary>
    public string GetOcrLogsDirectory() => _ocrLogsDirectory;

    /// <summary>
    /// Get the full path to the prescriptions directory
    /// </summary>
    public string GetPrescriptionsDirectory() => _prescriptionsDirectory;

    /// <summary>
    /// Check if file logging is enabled
    /// </summary>
    public bool IsFileLoggingEnabled() => _config.EnableFileLogging;
}
