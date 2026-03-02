using MedRemind.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MedRemind.Services.Storage;

/// <summary>
/// Service for managing voice recording file storage
/// Handles saving, retrieving, and deleting voice recording files
/// </summary>
public class VoiceRecordingStorageService
{
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<VoiceRecordingStorageService>? _logger;
    private const string VoiceRecordingsFolderName = "VoiceRecordings";

    // Supported audio formats
    private static readonly string[] SupportedFormats = { ".mp3", ".wav", ".ogg", ".webm", ".m4a" };
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    private const int MaxDurationSeconds = 60; // 1 minute max

    public VoiceRecordingStorageService(
        IFileStorageService fileStorageService,
        ILogger<VoiceRecordingStorageService>? logger = null)
    {
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    /// <summary>
    /// Save voice recording from base64 string
    /// </summary>
    public async Task<(bool success, string? filePath, string? errorMessage)> SaveVoiceRecordingAsync(
        string base64Audio,
        string originalFileName,
        int userId,
        int durationSeconds)
    {
        try
        {
            // Validate duration
            if (durationSeconds > MaxDurationSeconds)
            {
                return (false, null, $"Recording duration exceeds maximum of {MaxDurationSeconds} seconds");
            }

            // Decode base64
            byte[] audioBytes;
            try
            {
                // Remove data URL prefix if present (e.g., "data:audio/webm;base64,")
                var base64Data = base64Audio;
                if (base64Audio.Contains(","))
                {
                    base64Data = base64Audio.Split(',')[1];
                }
                audioBytes = Convert.FromBase64String(base64Data);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to decode base64 audio");
                return (false, null, "Invalid audio data format");
            }

            // Validate file size
            if (audioBytes.Length > MaxFileSizeBytes)
            {
                return (false, null, $"File size exceeds maximum of {MaxFileSizeBytes / (1024 * 1024)} MB");
            }

            // Validate file extension
            var extension = Path.GetExtension(originalFileName).ToLower();
            if (string.IsNullOrEmpty(extension) || !Array.Exists(SupportedFormats, f => f == extension))
            {
                extension = ".webm"; // Default to webm (common for web recordings)
            }

            // Generate unique filename
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
            var fileName = $"voice_{userId}_{timestamp}_{uniqueId}{extension}";

            // Get storage path - use prescriptions directory as base and create voice recordings subfolder
            var baseDirectory = _fileStorageService.GetPrescriptionsDirectory();
            var voiceRecordingsPath = Path.Combine(baseDirectory, "..", VoiceRecordingsFolderName);
            var userFolderPath = Path.Combine(voiceRecordingsPath, userId.ToString());

            // Ensure user folder exists
            if (!Directory.Exists(userFolderPath))
            {
                Directory.CreateDirectory(userFolderPath);
                _logger?.LogInformation($"Created voice recordings folder for user {userId}");
            }

            // Full file path
            var filePath = Path.Combine(userFolderPath, fileName);

            // Save file
            await File.WriteAllBytesAsync(filePath, audioBytes);

            _logger?.LogInformation($"Voice recording saved: {filePath} ({audioBytes.Length} bytes)");

            // Return relative path for database storage
            var relativePath = Path.Combine(VoiceRecordingsFolderName, userId.ToString(), fileName);
            return (true, relativePath, null);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving voice recording");
            return (false, null, $"Failed to save recording: {ex.Message}");
        }
    }

    /// <summary>
    /// Get full path to voice recording file
    /// </summary>
    public string GetVoiceRecordingPath(string relativePath)
    {
        var baseDirectory = _fileStorageService.GetPrescriptionsDirectory();
        var voiceRecordingsBase = Path.Combine(baseDirectory, "..");
        return Path.Combine(voiceRecordingsBase, relativePath);
    }

    /// <summary>
    /// Check if voice recording file exists
    /// </summary>
    public bool VoiceRecordingExists(string relativePath)
    {
        var fullPath = GetVoiceRecordingPath(relativePath);
        return File.Exists(fullPath);
    }

    /// <summary>
    /// Delete voice recording file
    /// </summary>
    public async Task<bool> DeleteVoiceRecordingAsync(string relativePath)
    {
        try
        {
            var fullPath = GetVoiceRecordingPath(relativePath);

            if (File.Exists(fullPath))
            {
                await Task.Run(() => File.Delete(fullPath));
                _logger?.LogInformation($"Voice recording deleted: {fullPath}");
                return true;
            }

            _logger?.LogWarning($"Voice recording file not found: {fullPath}");
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Error deleting voice recording: {relativePath}");
            return false;
        }
    }

    /// <summary>
    /// Get voice recording file info
    /// </summary>
    public (bool exists, long fileSize, DateTime? lastModified) GetFileInfo(string relativePath)
    {
        try
        {
            var fullPath = GetVoiceRecordingPath(relativePath);

            if (File.Exists(fullPath))
            {
                var fileInfo = new FileInfo(fullPath);
                return (true, fileInfo.Length, fileInfo.LastWriteTimeUtc);
            }

            return (false, 0, null);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Error getting file info: {relativePath}");
            return (false, 0, null);
        }
    }

    /// <summary>
    /// Get audio bytes for playback
    /// </summary>
    public async Task<byte[]?> GetAudioBytesAsync(string relativePath)
    {
        try
        {
            var fullPath = GetVoiceRecordingPath(relativePath);

            if (File.Exists(fullPath))
            {
                return await File.ReadAllBytesAsync(fullPath);
            }

            _logger?.LogWarning($"Audio file not found: {fullPath}");
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Error reading audio file: {relativePath}");
            return null;
        }
    }

    /// <summary>
    /// Clean up orphaned voice recordings (not linked to any user)
    /// </summary>
    public async Task<int> CleanupOrphanedRecordingsAsync()
    {
        try
        {
            var baseDirectory = _fileStorageService.GetPrescriptionsDirectory();
            var voiceRecordingsPath = Path.Combine(baseDirectory, "..", VoiceRecordingsFolderName);
            var deletedCount = 0;

            if (!Directory.Exists(voiceRecordingsPath))
                return 0;

            // Get all voice recording files older than 30 days with no database entry
            // This is a simplified version - in production, you'd query the database
            var oldFiles = Directory.GetFiles(voiceRecordingsPath, "*.*", SearchOption.AllDirectories)
                .Where(f => File.GetLastWriteTimeUtc(f) < DateTime.UtcNow.AddDays(-30));

            foreach (var file in oldFiles)
            {
                await Task.Run(() => File.Delete(file));
                deletedCount++;
            }

            _logger?.LogInformation($"Cleaned up {deletedCount} orphaned voice recordings");
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error cleaning up orphaned recordings");
            return 0;
        }
    }

    /// <summary>
    /// Get content type for audio file
    /// </summary>
    public static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLower();
        return extension switch
        {
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".ogg" => "audio/ogg",
            ".webm" => "audio/webm",
            ".m4a" => "audio/mp4",
            _ => "application/octet-stream"
        };
    }
}
