namespace MedRemind.Core.Configuration;

/// <summary>
/// Configuration for file storage service
/// Manages storage of prescription images and OCR logs
/// Structure: files/prescriptions/ and files/OCRs/
/// </summary>
public class FileStorageConfiguration
{
    /// <summary>
    /// Folder name for OCR logs (default: "OCRs")
    /// </summary>
    public string OcrLogsFolderName { get; set; } = "OCRs";

    /// <summary>
    /// Folder name for prescription images (default: "prescriptions")
    /// </summary>
    public string PrescriptionsFolderName { get; set; } = "Prescriptions";

    /// <summary>
    /// Enable or disable file logging (default: true)
    /// </summary>
    public bool EnableFileLogging { get; set; } = true;

    /// <summary>
    /// Maximum number of log files to keep (0 = unlimited, default: 100)
    /// </summary>
    public int MaxLogFiles { get; set; } = 100;

    /// <summary>
    /// Prefix for OCR file names (default: "OCR")
    /// </summary>
    public string OcrFilePrefix { get; set; } = "OCR";
}
