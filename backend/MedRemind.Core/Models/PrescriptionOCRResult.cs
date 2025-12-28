namespace MedRemind.Core.Models;

/// <summary>
/// Stores prescription OCR extraction results and AI parser responses
/// Used for duplicate detection and audit trail
/// </summary>
public class PrescriptionOCRResult
{
    public int Id { get; set; }
    public int PrescriptionId { get; set; }
    
    // OCR Text (for duplicate detection)
    public string OCRText { get; set; } = string.Empty;
    public string OCRTextHash { get; set; } = string.Empty; // SHA256 hash for quick comparison
    
    // AI Parser Responses
    public string? OpenAIResponse { get; set; } // JSON of OpenAI result
    public string? ClaudeResponse { get; set; } // JSON of Claude result (optional)
    public string? SelectedResponse { get; set; } // JSON of selected best result
    public string? SelectedProvider { get; set; } // "OpenAI" or "Claude"
    
    // Comparison Metrics
    public double ComparisonScore { get; set; }
    public string? ComparisonReason { get; set; }
    
    // Extracted Summary (for quick access without parsing JSON)
    public int MedicationCount { get; set; }
    public string? DoctorName { get; set; }
    public string? PatientName { get; set; }
    public DateTime? PrescriptionDate { get; set; }
    
    // Processing Metadata
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan ProcessingTime { get; set; }
    public int ProcessingAttempts { get; set; } = 1;
    
    // Navigation property
    public virtual Prescription? Prescription { get; set; }
}
