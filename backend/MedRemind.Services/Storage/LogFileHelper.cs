using MedRemind.Core.Interfaces;

namespace MedRemind.Services.Storage;

/// <summary>
/// Helper class for accessing and managing log files
/// Provides convenient methods for listing, viewing, and sharing logs
/// </summary>
public class LogFileHelper
{
    private readonly IFileStorageService _fileStorageService;

    public LogFileHelper(IFileStorageService fileStorageService)
    {
        _fileStorageService = fileStorageService;
    }

    /// <summary>
    /// Get information about all log files
    /// </summary>
    public async Task<List<LogFileInfo>> GetLogFilesInfoAsync()
    {
        var files = await _fileStorageService.GetOcrLogFilesAsync();
        var logFiles = new List<LogFileInfo>();

        foreach (var filePath in files)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                logFiles.Add(new LogFileInfo
                {
                    FileName = fileInfo.Name,
                    FilePath = filePath,
                    FileSize = fileInfo.Length,
                    CreatedDate = fileInfo.CreationTime,
                    ModifiedDate = fileInfo.LastWriteTime,
                    FileType = GetFileType(fileInfo.Extension)
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"?? Failed to get info for {filePath}: {ex.Message}");
            }
        }

        return logFiles;
    }

    /// <summary>
    /// Get the most recent OCR log files (grouped by session)
    /// </summary>
    public async Task<List<OcrLogSession>> GetRecentOcrSessionsAsync(int count = 10)
    {
        var files = await _fileStorageService.GetOcrLogFilesAsync();
        var sessions = new Dictionary<string, OcrLogSession>();

        foreach (var filePath in files)
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                
                // Extract base name (e.g., "Prescription_OCR_20250128_1738234567")
                var baseName = fileName;
                if (fileName.EndsWith("_raw") || fileName.EndsWith("_normalized") || fileName.EndsWith("_response"))
                {
                    var lastUnderscore = fileName.LastIndexOf('_');
                    baseName = fileName.Substring(0, lastUnderscore);
                }

                if (!sessions.ContainsKey(baseName))
                {
                    var fileInfo = new FileInfo(filePath);
                    sessions[baseName] = new OcrLogSession
                    {
                        SessionId = baseName,
                        CreatedDate = fileInfo.CreationTime
                    };
                }

                // Add file to session
                if (fileName.EndsWith("_raw"))
                {
                    sessions[baseName].RawTextPath = filePath;
                }
                else if (fileName.EndsWith("_normalized"))
                {
                    sessions[baseName].NormalizedTextPath = filePath;
                }
                else if (fileName.EndsWith("_response"))
                {
                    sessions[baseName].JsonResponsePath = filePath;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"?? Failed to process {filePath}: {ex.Message}");
            }
        }

        return sessions.Values
            .OrderByDescending(s => s.CreatedDate)
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// Open the logs directory in file explorer
    /// </summary>
    public async Task<bool> OpenLogsDirectoryAsync()
    {
        return await _fileStorageService.OpenLogsDirectoryAsync();
    }

    /// <summary>
    /// Get the logs directory path for display
    /// </summary>
    public string GetLogsDirectoryPath()
    {
        // Return the OCR logs directory path
        return _fileStorageService.GetOcrLogsDirectory();
    }

    /// <summary>
    /// Read specific log file content
    /// </summary>
    public async Task<string> ReadLogAsync(string filePath)
    {
        return await _fileStorageService.ReadLogFileAsync(filePath);
    }

    /// <summary>
    /// Get storage information
    /// </summary>
    public async Task<StorageInfo> GetStorageInfoAsync()
    {
        var ocrFiles = await _fileStorageService.GetOcrLogFilesAsync();
        var prescriptionFiles = await _fileStorageService.GetPrescriptionImageFilesAsync();

        long ocrTotalSize = 0;
        foreach (var file in ocrFiles)
        {
            try
            {
                ocrTotalSize += new FileInfo(file).Length;
            }
            catch { }
        }

        long prescriptionTotalSize = 0;
        foreach (var file in prescriptionFiles)
        {
            try
            {
                prescriptionTotalSize += new FileInfo(file).Length;
            }
            catch { }
        }

        return new StorageInfo
        {
            OcrLogsCount = ocrFiles.Count,
            OcrLogsTotalSizeMB = ocrTotalSize / (1024.0 * 1024.0),
            PrescriptionImagesCount = prescriptionFiles.Count,
            PrescriptionImagesTotalSizeMB = prescriptionTotalSize / (1024.0 * 1024.0),
            OcrLogsDirectory = _fileStorageService.GetOcrLogsDirectory(),
            PrescriptionsDirectory = _fileStorageService.GetPrescriptionsDirectory(),
            PublicStorageDirectory = null // Removed public storage support
        };
    }

    private static string GetFileType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".txt" => "Text",
            ".json" => "JSON",
            ".jpg" or ".jpeg" => "Image",
            ".png" => "Image",
            _ => "Unknown"
        };
    }
}

/// <summary>
/// Information about a log file
/// </summary>
public class LogFileInfo
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public string FileType { get; set; } = string.Empty;

    public string FileSizeFormatted => FormatFileSize(FileSize);

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F2} KB";
        return $"{bytes / (1024.0 * 1024.0):F2} MB";
    }
}

/// <summary>
/// Represents a single OCR logging session (raw, normalized, JSON)
/// </summary>
public class OcrLogSession
{
    public string SessionId { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string? RawTextPath { get; set; }
    public string? NormalizedTextPath { get; set; }
    public string? JsonResponsePath { get; set; }

    public bool IsComplete => !string.IsNullOrEmpty(RawTextPath) 
        && !string.IsNullOrEmpty(NormalizedTextPath) 
        && !string.IsNullOrEmpty(JsonResponsePath);
}

/// <summary>
/// Storage statistics and information
/// </summary>
public class StorageInfo
{
    public int OcrLogsCount { get; set; }
    public double OcrLogsTotalSizeMB { get; set; }
    public int PrescriptionImagesCount { get; set; }
    public double PrescriptionImagesTotalSizeMB { get; set; }
    public string OcrLogsDirectory { get; set; } = string.Empty;
    public string PrescriptionsDirectory { get; set; } = string.Empty;
    public string? PublicStorageDirectory { get; set; }
}
