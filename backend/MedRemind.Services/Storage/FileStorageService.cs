using System.IO;
using MedRemind.Core.Configuration;
using MedRemind.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace MedRemind.Services.Storage;

/// <summary>
/// Cross-platform file storage service for managing prescription files and OCR logs
/// Structure: files/Prescriptions/ and files/OCRs/
/// OCR files are named to match their corresponding prescription files
/// </summary>
public class FileStorageService : IFileStorageService
{
    private readonly FileStorageConfiguration _config;
    private readonly string _baseDirectory;
    private readonly string _ocrLogsDirectory;
    private readonly string _prescriptionsDirectory;

    public FileStorageService(IOptions<FileStorageConfiguration> config)
    {
        _config = config.Value;

        // Get base directory for file storage
        // For backend: use current directory
        // For mobile: use LocalApplicationData
        _baseDirectory = GetBaseDirectory();

        // Create directory paths: files/prescriptions and files/OCRs
        var filesDirectory = Path.Combine(_baseDirectory, "Files");
        _prescriptionsDirectory = Path.Combine(filesDirectory, "Prescriptions");
        _ocrLogsDirectory = Path.Combine(filesDirectory, "OCRs");

        // Ensure directories exist
        EnsureDirectoriesExist();

        System.Diagnostics.Debug.WriteLine($"📁 FileStorageService initialized:");
        System.Diagnostics.Debug.WriteLine($"   Base Directory: {_baseDirectory}");
        System.Diagnostics.Debug.WriteLine($"   Prescriptions: {_prescriptionsDirectory}");
        System.Diagnostics.Debug.WriteLine($"   OCR Logs: {_ocrLogsDirectory}");
        System.Diagnostics.Debug.WriteLine($"   File Logging Enabled: {_config.EnableFileLogging}");
    }

    /// <summary>
    /// Get base directory based on platform
    /// </summary>
    private string GetBaseDirectory()
    {
        // Use application directory (current directory for backend/console apps)
        // This ensures files are stored relative to the application, not user's AppData
        return Directory.GetCurrentDirectory();
    }

    /// <summary>
    /// Ensure all necessary directories exist
    /// </summary>
    private void EnsureDirectoriesExist()
    {
        try
        {
            if (!Directory.Exists(_prescriptionsDirectory))
            {
                Directory.CreateDirectory(_prescriptionsDirectory);
                System.Diagnostics.Debug.WriteLine($"✅ Created prescriptions directory: {_prescriptionsDirectory}");
            }

            if (!Directory.Exists(_ocrLogsDirectory))
            {
                Directory.CreateDirectory(_ocrLogsDirectory);
                System.Diagnostics.Debug.WriteLine($"✅ Created OCR logs directory: {_ocrLogsDirectory}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Failed to create directories: {ex.Message}");
        }
    }

    /// <summary>
    /// Generate unique file name with timestamp
    /// Format: {Prefix}_{yyyyMMdd_HHmmss}_{UniqueId}
    /// </summary>
    private string GenerateFileName(string prefix, string? customName = null)
    {
        if (!string.IsNullOrEmpty(customName))
        {
            return Path.GetFileNameWithoutExtension(customName);
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
        return $"{prefix}_{timestamp}_{uniqueId}";
    }

    /// <summary>
    /// Save OCR results with file name matching the prescription image
    /// Creates: {baseFileName}_raw.txt, {baseFileName}_normalized.txt, {baseFileName}_result.json
    /// </summary>
    public async Task<(string rawPath, string normalizedPath, string jsonPath)> SaveOcrResultsAsync(
        string rawText,
        string normalizedText,
        string jsonContent,
        string? prescriptionFileName = null)
    {
        if (!_config.EnableFileLogging)
        {
            System.Diagnostics.Debug.WriteLine("📁 File logging is disabled, skipping save");
            return (string.Empty, string.Empty, string.Empty);
        }

        try
        {
            // Get base file name from prescription file name or generate new one
            var baseFileName = !string.IsNullOrEmpty(prescriptionFileName)
                ? $"{_config.OcrFilePrefix ?? "OCR"}_{Path.GetFileNameWithoutExtension(prescriptionFileName)}"
                : GenerateFileName(_config.OcrFilePrefix ?? "OCR");

            // Save OCR files with matching names
            var rawPath = await SaveFileAsync(rawText, $"{baseFileName}_raw.txt", _ocrLogsDirectory);
            var normalizedPath = await SaveFileAsync(normalizedText, $"{baseFileName}_normalized.txt", _ocrLogsDirectory);
            var jsonPath = await SaveFileAsync(jsonContent, $"{baseFileName}_result.json", _ocrLogsDirectory);

            System.Diagnostics.Debug.WriteLine($"✅ Saved OCR results for: {baseFileName}");
            System.Diagnostics.Debug.WriteLine($"   Raw: {Path.GetFileName(rawPath)}");
            System.Diagnostics.Debug.WriteLine($"   Normalized: {Path.GetFileName(normalizedPath)}");
            System.Diagnostics.Debug.WriteLine($"   JSON: {Path.GetFileName(jsonPath)}");

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
    /// Returns the full file path and file name
    /// </summary>
    public async Task<string> SavePrescriptionImageAsync(byte[] imageBytes, string? fileName = null)
    {
        if (!_config.EnableFileLogging)
        {
            return string.Empty;
        }

        try
        {
            var baseFileName = GenerateFileName("prescription", fileName);
            var extension = !string.IsNullOrEmpty(fileName)
                ? Path.GetExtension(fileName)
                : ".jpg";
            
            var fullFileName = $"{baseFileName}{extension}";
            var filePath = Path.Combine(_prescriptionsDirectory, fullFileName);

            await File.WriteAllBytesAsync(filePath, imageBytes);
            System.Diagnostics.Debug.WriteLine($"💾 Saved prescription image: {fullFileName}");

            return filePath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Failed to save prescription image: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Save prescription file with user ID in filename for better organization
    /// Format: user_{userId}_{timestamp}{extension}
    /// Preserves original file extension from uploaded file
    /// Returns the full file path and relative path
    /// </summary>
    public async Task<(string filePath, string relativePath, string fileName)> SavePrescriptionFileAsync(
        byte[] fileBytes,
        int userId,
        string? originalFileName = null)
    {
        if (!_config.EnableFileLogging)
        {
            return (string.Empty, string.Empty, string.Empty);
        }

        try
        {
            // Determine file extension - preserve from original file
            var extension = ".jpg"; // default fallback
            
            if (!string.IsNullOrEmpty(originalFileName))
            {
                var extractedExtension = Path.GetExtension(originalFileName);
                
                // Only use extracted extension if it's valid
                if (!string.IsNullOrWhiteSpace(extractedExtension))
                {
                    extension = extractedExtension.ToLowerInvariant();
                    
                    // Ensure extension starts with a dot
                    if (!extension.StartsWith("."))
                    {
                        extension = "." + extension;
                    }
                }
            }

            // Generate unique filename with user ID
            //var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            //var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
            //var uniqueFileName = $"prescription_{userId}_{timestamp}{extension}";
           
            var generatePrescriptionFileName = GenerateFileName($"prescription_{userId}");
            var uniqueFileName = $"{generatePrescriptionFileName}{extension}";

            var filePath = Path.Combine(_prescriptionsDirectory, uniqueFileName);
            var relativePath = Path.Combine("Files", "Prescriptions", uniqueFileName);

            // Save file
            await File.WriteAllBytesAsync(filePath, fileBytes);
            
            System.Diagnostics.Debug.WriteLine($"💾 Saved prescription file: {uniqueFileName}");
            System.Diagnostics.Debug.WriteLine($"   Original file: {originalFileName ?? "N/A"}");
            System.Diagnostics.Debug.WriteLine($"   Extension: {extension}");
            System.Diagnostics.Debug.WriteLine($"   Size: {fileBytes.Length / 1024.0:F2} KB");
            System.Diagnostics.Debug.WriteLine($"   Path: {relativePath}");

            return (filePath, relativePath, uniqueFileName);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Failed to save prescription file: {ex.Message}");
            return (string.Empty, string.Empty, string.Empty);
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
                // Cleanup OCR logs directory
                CleanupDirectory(_ocrLogsDirectory, _config.MaxLogFiles);
                
                // Cleanup prescriptions directory (optional - can be set to higher limit)
                CleanupDirectory(_prescriptionsDirectory, _config.MaxLogFiles * 2);
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Cleanup failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Cleanup files in a specific directory
    /// </summary>
    private void CleanupDirectory(string directory, int maxFiles)
    {
        try
        {
            if (!Directory.Exists(directory))
            {
                return;
            }

            var files = Directory.GetFiles(directory)
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTimeUtc)
                .ToList();

            var filesToDelete = files.Skip(maxFiles).ToList();

            if (filesToDelete.Any())
            {
                System.Diagnostics.Debug.WriteLine($"🗑️ Cleaning up {filesToDelete.Count} old file(s) from {Path.GetFileName(directory)}...");

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
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Failed to cleanup directory {directory}: {ex.Message}");
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

    /// <summary>
    /// Get list of all OCR log files
    /// </summary>
    public async Task<List<string>> GetOcrLogFilesAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!Directory.Exists(_ocrLogsDirectory))
                {
                    return new List<string>();
                }

                var files = Directory.GetFiles(_ocrLogsDirectory)
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTimeUtc)
                    .Select(f => f.FullName)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"📂 Found {files.Count} OCR log file(s)");
                return files;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to get OCR log files: {ex.Message}");
                return new List<string>();
            }
        });
    }

    /// <summary>
    /// Get list of all prescription image files
    /// </summary>
    public async Task<List<string>> GetPrescriptionImageFilesAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!Directory.Exists(_prescriptionsDirectory))
                {
                    return new List<string>();
                }

                var files = Directory.GetFiles(_prescriptionsDirectory)
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTimeUtc)
                    .Select(f => f.FullName)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"📂 Found {files.Count} prescription image file(s)");
                return files;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to get prescription images: {ex.Message}");
                return new List<string>();
            }
        });
    }

    /// <summary>
    /// Get OCR files associated with a prescription file
    /// Returns paths to raw, normalized, and JSON files
    /// </summary>
    public async Task<(string? rawPath, string? normalizedPath, string? jsonPath)> GetOcrFilesForPrescriptionAsync(string prescriptionFileName)
    {
        return await Task.Run(() =>
        {
            try
            {
                var baseFileName = Path.GetFileNameWithoutExtension(prescriptionFileName);
                
                var rawPath = Path.Combine(_ocrLogsDirectory, $"{baseFileName}_raw.txt");
                var normalizedPath = Path.Combine(_ocrLogsDirectory, $"{baseFileName}_normalized.txt");
                var jsonPath = Path.Combine(_ocrLogsDirectory, $"{baseFileName}_result.json");

                return (
                    File.Exists(rawPath) ? rawPath : null,
                    File.Exists(normalizedPath) ? normalizedPath : null,
                    File.Exists(jsonPath) ? jsonPath : null
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to get OCR files for {prescriptionFileName}: {ex.Message}");
                return (null, null, null);
            }
        });
    }

    /// <summary>
    /// Read content of a specific log file
    /// </summary>
    public async Task<string> ReadLogFileAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ File not found: {filePath}");
                return string.Empty;
            }

            var content = await File.ReadAllTextAsync(filePath);
            System.Diagnostics.Debug.WriteLine($"✅ Read file: {Path.GetFileName(filePath)} ({content.Length} chars)");
            return content;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Failed to read file {filePath}: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Delete a prescription file and its associated OCR files
    /// </summary>
    public async Task<bool> DeletePrescriptionWithOcrAsync(string prescriptionFileName)
    {
        return await Task.Run(() =>
        {
            try
            {
                var deleted = false;
                
                // Delete prescription file
                var prescriptionPath = Path.Combine(_prescriptionsDirectory, prescriptionFileName);
                if (File.Exists(prescriptionPath))
                {
                    File.Delete(prescriptionPath);
                    System.Diagnostics.Debug.WriteLine($"🗑️ Deleted prescription: {prescriptionFileName}");
                    deleted = true;
                }

                // Delete associated OCR files
                var baseFileName = Path.GetFileNameWithoutExtension(prescriptionFileName);
                var ocrFiles = new[]
                {
                    Path.Combine(_ocrLogsDirectory, $"{baseFileName}_raw.txt"),
                    Path.Combine(_ocrLogsDirectory, $"{baseFileName}_normalized.txt"),
                    Path.Combine(_ocrLogsDirectory, $"{baseFileName}_result.json")
                };

                foreach (var ocrFile in ocrFiles)
                {
                    if (File.Exists(ocrFile))
                    {
                        File.Delete(ocrFile);
                        System.Diagnostics.Debug.WriteLine($"🗑️ Deleted OCR file: {Path.GetFileName(ocrFile)}");
                    }
                }

                return deleted;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to delete prescription and OCR files: {ex.Message}");
                return false;
            }
        });
    }

    /// <summary>
    /// Open the files directory in the system file explorer
    /// </summary>
    public async Task<bool> OpenLogsDirectoryAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var directoryToOpen = Path.GetDirectoryName(_prescriptionsDirectory) ?? _prescriptionsDirectory;

                if (!Directory.Exists(directoryToOpen))
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Directory does not exist: {directoryToOpen}");
                    return false;
                }

                // Cross-platform directory opening
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = directoryToOpen,
                    UseShellExecute = true,
                    Verb = "open"
                });
                
                System.Diagnostics.Debug.WriteLine($"📂 Opened directory: {directoryToOpen}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to open directory: {ex.Message}");
                return false;
            }
        });
    }
}
