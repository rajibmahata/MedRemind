namespace MedRemind.Core.Enums;

/// <summary>
/// Status of OCR result processing
/// </summary>
public enum OcrProcessingStatus
{
    /// <summary>
    /// OCR text extraction in progress
    /// </summary>
    Processing = 1,
    
    /// <summary>
    /// OCR extraction complete, AI processing in progress
    /// </summary>
    OcrComplete = 2,
    
    /// <summary>
    /// AI processing and validation complete
    /// </summary>
    Processed = 3,
    
    /// <summary>
    /// Processing failed
    /// </summary>
    Failed = 4,
    
    /// <summary>
    /// Duplicate prescription detected, using cached result
    /// </summary>
    Duplicate = 5
}
