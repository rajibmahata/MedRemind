namespace MedRemind.Core.Interfaces;

/// <summary>
/// Service for managing file storage operations
/// Handles saving files to app-specific storage and optionally to public storage
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Save OCR results (raw text, normalized text, and JSON) to storage
    /// </summary>
    /// <param name="rawText">Raw extracted OCR text</param>
    /// <param name="normalizedText">Preprocessed/normalized text</param>
    /// <param name="jsonContent">JSON response from OCR service</param>
    /// <param name="fileName">Optional base file name (without extension)</param>
    /// <returns>Tuple of saved file paths (raw, normalized, json)</returns>
    Task<(string rawPath, string normalizedPath, string jsonPath)> SaveOcrResultsAsync(
        string rawText, 
        string normalizedText, 
        string jsonContent, 
        string? fileName = null);

    /// <summary>
    /// Save prescription image to storage
    /// </summary>
    /// <param name="imageBytes">Image data</param>
    /// <param name="fileName">Optional file name (without extension)</param>
    /// <returns>Path where image was saved</returns>
    Task<string> SavePrescriptionImageAsync(byte[] imageBytes, string? fileName = null);

    /// <summary>
    /// Save text file to storage
    /// </summary>
    /// <param name="content">Text content</param>
    /// <param name="fileName">File name with extension</param>
    /// <param name="folderType">Type of folder (OCR logs or Prescriptions)</param>
    /// <returns>Path where file was saved</returns>
    Task<string> SaveTextFileAsync(string content, string fileName, StorageFolderType folderType);

    /// <summary>
    /// Delete old log files to maintain MaxLogFiles limit
    /// </summary>
    Task CleanupOldFilesAsync();

    /// <summary>
    /// Get the full path to the OCR logs directory
    /// </summary>
    string GetOcrLogsDirectory();

    /// <summary>
    /// Get the full path to the prescriptions directory
    /// </summary>
    string GetPrescriptionsDirectory();

    /// <summary>
    /// Check if file logging is enabled
    /// </summary>
    bool IsFileLoggingEnabled();
}

/// <summary>
/// Types of storage folders
/// </summary>
public enum StorageFolderType
{
    OcrLogs,
    Prescriptions
}
