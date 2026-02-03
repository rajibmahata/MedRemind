using MedRemind.Core.Enums;

namespace MedRemind.Core.Models;

/// <summary>
/// Stores prescription OCR extraction results and AI parser responses
/// Used for duplicate detection and audit trail
/// Enhanced to store Python Middleware (CrewAI) responses that use multiple LLM APIs
/// </summary>
public class PrescriptionOCRResult
{
    public int Id { get; set; }
    public int PrescriptionId { get; set; }
    
    // Processing Status
    public OcrProcessingStatus Status { get; set; } = OcrProcessingStatus.Processing;
    
    // OCR Text (for duplicate detection)
    public string OCRText { get; set; } = string.Empty;
    public string OCRTextHash { get; set; } = string.Empty; // SHA256 hash for quick comparison
    
    // AI Parser Responses from Python Middleware
    // Python middleware uses multiple LLMs: OpenAI, DeepSeek, Claude
    public string? OpenAIResponse { get; set; } // JSON of Python middleware result (using OpenAI LLM)
    public string? DeepSeekResponse { get; set; } // JSON of Python middleware result (using DeepSeek LLM)
    public string? ClaudeResponse { get; set; } // JSON of Medicine validation from Python middleware (using Claude LLM)
    public string? SelectedResponse { get; set; } // JSON of selected best result from Python middleware
    public string? SelectedProvider { get; set; } // "Python Middleware (CrewAI)" - indicates LLMs used
    
    // Python Middleware Processing Metadata
    public string? PythonMiddlewareVersion { get; set; } // Version of Python service
    public string? LlmModelsUsed { get; set; } // Comma-separated list: "gpt-4o-mini,deepseek-chat,claude-3.5"
    public string? CrewAISummary { get; set; } // Summary from CrewAI agent orchestration
    
    // Comparison Metrics
    public double ComparisonScore { get; set; }
    public string? ComparisonReason { get; set; }
    
    // Extracted Summary (for quick access without parsing JSON)
    public int MedicationCount { get; set; }
    public string? DoctorName { get; set; }
    public string? PatientName { get; set; }
    public DateTime? PrescriptionDate { get; set; }
    
    // Medicine Validation Metadata (from Python middleware)
    public double? OverallSafetyScore { get; set; } // 0.0 to 1.0
    public bool? RequiresPharmacistReview { get; set; }
    public int? SafetyWarningsCount { get; set; }
    public int? DrugInteractionsCount { get; set; }
    
    // Processing Metadata
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan ProcessingTime { get; set; }
    public int ProcessingAttempts { get; set; } = 1;
    
    // Navigation property
    public virtual Prescription? Prescription { get; set; }
}
