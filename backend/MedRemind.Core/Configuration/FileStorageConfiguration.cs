namespace MedRemind.Core.Configuration;

/// <summary>
/// Configuration for file storage locations and settings
/// </summary>
public class FileStorageConfiguration
{
    /// <summary>
    /// Base folder name for OCR logs (relative to app data directory)
    /// Default: "MedRemind_OCR_Logs"
    /// </summary>
    public string OcrLogsFolderName { get; set; } = "MedRemind_OCR_Logs";

    /// <summary>
    /// Base folder name for prescription images (relative to app data directory)
    /// Default: "MedRemind_Prescriptions"
    /// </summary>
    public string PrescriptionsFolderName { get; set; } = "MedRemind_Prescriptions";

    /// <summary>
    /// Whether to enable file logging
    /// Default: true
    /// </summary>
    public bool EnableFileLogging { get; set; } = true;

    /// <summary>
    /// Whether to copy files to public storage (for testing/debugging only)
    /// WARNING: Public storage is accessible to all apps and users
    /// Default: false (disabled for security)
    /// </summary>
    public bool CopyToPublicStorage { get; set; } = false;

    /// <summary>
    /// Public storage folder name (if CopyToPublicStorage is enabled)
    /// Will be created in: /storage/emulated/0/Documents/{PublicStorageFolderName}
    /// Default: "MedRemind_Debug"
    /// </summary>
    public string PublicStorageFolderName { get; set; } = "MedRemind_Debug";

    /// <summary>
    /// Maximum number of log files to keep (older files will be deleted)
    /// Default: 100
    /// </summary>
    public int MaxLogFiles { get; set; } = 100;

    /// <summary>
    /// Whether to include timestamps in file names
    /// Default: true
    /// </summary>
    public bool IncludeTimestampInFileName { get; set; } = true;

    /// <summary>
    /// File name prefix for OCR logs
    /// Default: "Prescription_OCR"
    /// </summary>
    public string OcrFilePrefix { get; set; } = "Prescription_OCR";
}
