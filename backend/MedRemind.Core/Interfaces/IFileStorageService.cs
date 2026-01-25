namespace MedRemind.Core.Interfaces;

/// <summary>
/// Service for managing file storage operations
/// Handles saving files to app-specific storage with matched file names
/// Structure: files/prescriptions/ and files/OCRs/
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Save OCR results (raw text, normalized text, and JSON) to storage
    /// File names will match the prescription file name for easy mapping
    /// </summary>
    /// <param name="rawText">Raw extracted OCR text</param>
    /// <param name="normalizedText">Preprocessed/normalized text</param>
    /// <param name="jsonContent">JSON response from OCR service</param>
    /// <param name="prescriptionFileName">Prescription file name to match OCR files with</param>
    /// <returns>Tuple of saved file paths (raw, normalized, json)</returns>
    Task<(string rawPath, string normalizedPath, string jsonPath)> SaveOcrResultsAsync(
        string rawText, 
        string normalizedText, 
        string jsonContent, 
        string? prescriptionFileName = null);

    /// <summary>
    /// Save prescription image to storage
    /// </summary>
    /// <param name="imageBytes">Image data</param>
    /// <param name="fileName">Optional file name (without extension)</param>
    /// <returns>Path where image was saved</returns>
    Task<string> SavePrescriptionImageAsync(byte[] imageBytes, string? fileName = null);

    /// <summary>
    /// Save prescription file with user ID for better organization
    /// </summary>
    /// <param name="fileBytes">File data</param>
    /// <param name="userId">User ID for file naming</param>
    /// <param name="originalFileName">Original file name (for extension)</param>
    /// <returns>Tuple of (full path, relative path, file name)</returns>
    Task<(string filePath, string relativePath, string fileName)> SavePrescriptionFileAsync(
        byte[] fileBytes,
        int userId,
        string? originalFileName = null);

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

    /// <summary>
    /// Get list of all OCR log files
    /// </summary>
    Task<List<string>> GetOcrLogFilesAsync();

    /// <summary>
    /// Get list of all prescription image files
    /// </summary>
    Task<List<string>> GetPrescriptionImageFilesAsync();

    /// <summary>
    /// Get OCR files associated with a prescription file
    /// </summary>
    /// <param name="prescriptionFileName">Prescription file name</param>
    /// <returns>Tuple of OCR file paths (raw, normalized, json) - null if not found</returns>
    Task<(string? rawPath, string? normalizedPath, string? jsonPath)> GetOcrFilesForPrescriptionAsync(string prescriptionFileName);

    /// <summary>
    /// Read content of a specific log file
    /// </summary>
    Task<string> ReadLogFileAsync(string filePath);

    /// <summary>
    /// Delete a prescription file and its associated OCR files
    /// </summary>
    /// <param name="prescriptionFileName">Prescription file name</param>
    /// <returns>True if successfully deleted</returns>
    Task<bool> DeletePrescriptionWithOcrAsync(string prescriptionFileName);

    /// <summary>
    /// Open the files directory in the system file explorer
    /// </summary>
    Task<bool> OpenLogsDirectoryAsync();
}
