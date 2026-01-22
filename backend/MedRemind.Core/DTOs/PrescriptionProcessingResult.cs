using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace MedRemind.Core.DTOs
{
    public class PrescriptionProcessingResult
    {
        public bool Success { get; set; }
        public string? PrescriptionFileName { get; set; }
        public OCRSaveResult? OCRSaveResult { get; set; }
        public ValidationResult? ValidationResult { get; set; }
        public PrescriptionReadResult? ParseResult { get; set; }
        public double MatchScore { get; set; }
        public int TotalAttempts { get; set; }
        public string? WarningMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan ProcessingTime { get; set; }

        // Database storage
        public string? SelectedProvider { get; set; } // e.g., "DeepSeek + OpenAI" or "DeepSeek + OpenAI + Claude"
        public int? DatabaseId { get; set; } // PrescriptionOCRResult ID
    }

}
