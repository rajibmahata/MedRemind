namespace MedRemind.Core.Interfaces;

/// <summary>
/// Type of storage folder for file operations
/// </summary>
public enum StorageFolderType
{
    /// <summary>
    /// OCR logs directory (files/OCRs/)
    /// </summary>
    OcrLogs,

    /// <summary>
    /// Prescriptions directory (files/prescriptions/)
    /// </summary>
    Prescriptions
}
