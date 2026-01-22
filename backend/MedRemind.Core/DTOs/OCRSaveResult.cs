using System;
using System.Collections.Generic;
using System.Text;

namespace MedRemind.Core.DTOs
{
    public class OCRSaveResult
    {
        public bool Success { get; set; }
        public string? SavedFilePath { get; set; }
        public string? MetadataFilePath { get; set; }
        public int TextLength { get; set; }
        public string? ErrorMessage { get; set; }
        public bool IsDuplicate { get; set; } // NEW: Indicates if this was a duplicate
        public string? DuplicateAction { get; set; } // NEW: What action was taken (e.g., "Kept existing file", "Renamed old file")
    }
}
