namespace MedRemind.Core.Enums;

/// <summary>
/// Status of prescription processing
/// </summary>
public enum PrescriptionStatus
{
    /// <summary>
    /// Prescription is being processed
    /// </summary>
    Processing = 1,
    
    /// <summary>
    /// Duplicate prescription detected
    /// </summary>
    Duplicate = 2,
    
    /// <summary>
    /// Prescription processing completed successfully
    /// </summary>
    Processed = 3,
    
    /// <summary>
    /// Prescription processing failed
    /// </summary>
    Failed = 4
}
