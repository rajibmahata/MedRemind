using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace MedRemind.Core.DTOs
{
    /// <summary>
    /// Result of prescription processing including OCR extraction, AI parsing, validation, and duplicate detection
    /// Unified result class for both simple and comprehensive prescription processing
    /// </summary>
    public class PrescriptionProcessingResult
    {
        // Basic result properties
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? WarningMessage { get; set; }
        
        // File information
        public string? PrescriptionFileName { get; set; }
        public int? PrescriptionId { get; set; }
        
        // OCR and validation results
        public OCRSaveResult? OCRSaveResult { get; set; }
        public ValidationResult? ValidationResult { get; set; }
        
        // AI parsing results
        public PrescriptionReadResult? ParseResult { get; set; }
        public PrescriptionReadResult? PrescriptionResult { get; set; } // Alternative property name for compatibility
        
        // Duplicate detection (from ComprehensivePrescriptionResult)
        public bool IsDuplicate { get; set; }
        public string? DuplicateMessage { get; set; }
        public double SimilarityScore { get; set; }
        public int? ExistingPrescriptionId { get; set; }
        public DateTime? ExistingProcessedDate { get; set; }
        
        // Processing metrics
        public double MatchScore { get; set; }
        public int TotalAttempts { get; set; }
        public int ProcessingAttempts { get; set; } // Alternative property name for compatibility
        public string? SelectedProvider { get; set; }
        
        // Timing information
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        
        // Database storage
        public int? DatabaseId { get; set; } // PrescriptionOCRResult ID
    }
}
