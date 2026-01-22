using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace MedRemind.Services.AI
{

    /// <summary>
    /// Generic prescription OCR text preprocessor that works for all prescription formats
    /// </summary>
    public class PrescriptionOcrTextPreprocessor
    {
        private readonly PreprocessorConfiguration _config;

        public PrescriptionOcrTextPreprocessor(PreprocessorConfiguration config = null)
        {
            _config = config ?? new PreprocessorConfiguration();
        }

        public string Preprocess(string ocrText, ProcessingMode mode = ProcessingMode.Balanced)
        {
            if (string.IsNullOrWhiteSpace(ocrText))
                return ocrText;

            var pipeline = new ProcessingPipeline();

            // Apply processing steps based on mode
            if (mode == ProcessingMode.Aggressive || mode == ProcessingMode.Minimal)
            {
                pipeline.AddStep(NormalizeText);
                pipeline.AddStep(RemoveAllNoise);
                pipeline.AddStep(ExtractEssentialContent);
                pipeline.AddStep(RemoveDuplicates);
                pipeline.AddStep(LimitLength);
            }
            else // Balanced mode
            {
                pipeline.AddStep(NormalizeText);
                pipeline.AddStep(RemoveCommonNoise);
                pipeline.AddStep(ExtractPrescriptionSections);
                pipeline.AddStep(RemoveDuplicates);
                pipeline.AddStep(FixCommonOcrErrors);
            }

            return pipeline.Process(ocrText);
        }

        #region Processing Steps

        private string NormalizeText(string text)
        {
            // Remove all bracketed content first (handwritten markers, etc.)
            text = Regex.Replace(text, @"\[.*?\]", " ");

            // Normalize Unicode escape sequences
            text = Regex.Replace(text, @"\\u[0-9a-fA-F]{4}", " ");

            // Normalize line endings and spaces
            text = text.Replace("\r\n", "\n")
                      .Replace("\r", "\n")
                      .Replace("\\r\\n", "\n")
                      .Replace("\\n", "\n")
                      .Replace("\t", " ")
                      .Replace("  ", " ")
                      .Trim();

            // Fix common OCR errors in symbols
            text = text.Replace("\\u00D7", "×")
                      .Replace("\\u00B0", "°")
                      .Replace("\\u0022", "\"")
                      .Replace("\\u0027", "'");

            // Remove excessive whitespace
            text = Regex.Replace(text, @"\s+", " ");
            text = Regex.Replace(text, @"\n\s*\n+", "\n");

            return text;
        }

        private string RemoveCommonNoise(string text)
        {
            var noisePatterns = GetNoisePatterns();

            foreach (var pattern in noisePatterns)
            {
                text = Regex.Replace(text, pattern, "\n", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            }

            return text;
        }

        private string RemoveAllNoise(string text)
        {
            // More aggressive noise removal
            var aggressivePatterns = new[]
            {
                // Remove everything that's clearly not prescription
                @"(?i)(?:clinic|hospital|pharmacy|diagnostic|lab|center|limited|ltd|pvt|inc)[\s\S]{0,100}\n",
                @"(?i)(?:address|location|landmark|plot|street|road|area|city|state|country|pin|zip)[\s:].*?\n",
                @"(?i)(?:phone|mobile|tel|contact|whatsapp|email|website|url)[\s:].*?\n",
                @"\b\d{8,15}\b", // Phone numbers
                @"https?://\S+", // URLs
                @"www\.\S+",
                @"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b", // Email
                @"(?i)(?:terms|conditions|disclaimer|confidential|private|note|remark)[\s\S]{0,100}\n",
                @"(?i)(?:visit|book|appointment|home collection|sample collection|call us)[\s\S]{0,100}\n",
                @"={10,}|-{10,}|_{10,}|\*{10,}", // Separators
                @"\[.*?\]", // Any remaining brackets
                @"Page\s*\d+\s*of\s*\d+",
                @"^\s*\d+\s*$", // Standalone numbers
                @"^\s*[=×+*\-]\s*$", // Standalone symbols
            };

            foreach (var pattern in aggressivePatterns)
            {
                text = Regex.Replace(text, pattern, "\n", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            }

            return text;
        }

        private string ExtractPrescriptionSections(string text)
        {
            var sections = new StringBuilder();
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            bool foundMedication = false;
            bool inPrescriptionSection = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine)) continue;

                // Detect if this is prescription content
                var isPrescriptionContent = IsPrescriptionContent(trimmedLine);
                var isHeader = IsPrescriptionHeader(trimmedLine);

                if (isHeader)
                {
                    inPrescriptionSection = true;
                    sections.AppendLine(trimmedLine);
                    continue;
                }

                if (inPrescriptionSection || isPrescriptionContent)
                {
                    // Once we've found medications, skip obvious noise
                    if (foundMedication && !IsMedicationContent(trimmedLine) &&
                        !IsPatientDoctorInfo(trimmedLine) && !IsDateInfo(trimmedLine))
                    {
                        continue;
                    }

                    if (IsMedicationContent(trimmedLine))
                        foundMedication = true;

                    sections.AppendLine(trimmedLine);
                }
                else if (IsPatientDoctorInfo(trimmedLine) || IsDateInfo(trimmedLine))
                {
                    sections.AppendLine(trimmedLine);
                }
            }

            return sections.ToString().Trim();
        }

        private string ExtractEssentialContent(string text)
        {
            var essentialLines = new List<string>();
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine)) continue;

                // Keep only essential content
                if (IsEssentialPrescriptionContent(trimmedLine))
                {
                    essentialLines.Add(trimmedLine);
                }
            }

            // Prioritize important lines
            var prioritizedLines = essentialLines
                .OrderByDescending(line => GetLinePriority(line))
                .Take(_config.MaxLines)
                .OrderBy(line => line) // Re-sort naturally
                .ToList();

            return string.Join("\n", prioritizedLines);
        }

        private string RemoveDuplicates(string text)
        {
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                           .Select(line => line.Trim())
                           .Where(line => !string.IsNullOrWhiteSpace(line))
                           .Distinct()
                           .ToList();

            // Remove near-duplicates (lines that are 80% similar)
            var uniqueLines = new List<string>();
            foreach (var line in lines)
            {
                if (!uniqueLines.Any(existing => CalculateSimilarity(existing, line) > 0.8))
                {
                    uniqueLines.Add(line);
                }
            }

            return string.Join("\n", uniqueLines);
        }

        private string FixCommonOcrErrors(string text)
        {
            var corrections = GetCommonCorrections();

            foreach (var correction in corrections)
            {
                text = text.Replace(correction.Key, correction.Value);
            }

            // Fix spacing in medications
            text = Regex.Replace(text, @"(\b(?:Syp|Tab|Sup|Cap|Inj)\.)([A-Z])", "$1 $2");

            // Fix dosage patterns
            text = Regex.Replace(text, @"(\d+)([a-zA-Z])", "$1 $2");
            text = Regex.Replace(text, @"(\d)\s*/\s*(\d)", "$1/$2");

            // Fix frequency patterns
            text = Regex.Replace(text, @"0\s+0\s+0", "0-0-0");
            text = Regex.Replace(text, @"\s*x\s*", " × ");
            text = Regex.Replace(text, @"\s*/\s*", "/");

            return text;
        }

        private string LimitLength(string text)
        {
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length <= _config.MaxLines) return text;

            // Keep most important lines
            var importantLines = lines
                .Select(line => new { Line = line, Priority = GetLinePriority(line) })
                .OrderByDescending(x => x.Priority)
                .Take(_config.MaxLines)
                .Select(x => x.Line)
                .ToList();

            return string.Join("\n", importantLines);
        }

        #endregion

        #region Content Detection

        private bool IsPrescriptionContent(string line)
        {
            return IsMedicationContent(line) ||
                   IsPatientDoctorInfo(line) ||
                   IsDateInfo(line) ||
                   IsInstructionInfo(line) ||
                   IsPrescriptionHeader(line);
        }

        private bool IsEssentialPrescriptionContent(string line)
        {
            // Essential content gets highest priority
            return IsMedicationContent(line) ||
                   line.StartsWith("Dr.", StringComparison.OrdinalIgnoreCase) ||
                   line.StartsWith("Patient:", StringComparison.OrdinalIgnoreCase) ||
                   line.StartsWith("Name:", StringComparison.OrdinalIgnoreCase) ||
                   Regex.IsMatch(line, @"\d{1,2}/\d{1,2}/\d{2,4}") ||
                   Regex.IsMatch(line, @"Reg\s*[Nn]o:", RegexOptions.IgnoreCase);
        }

        private bool IsMedicationContent(string line)
        {
            var patterns = new[]
            {
                // Medication patterns
                @"\b(?:Syp|Tab|Sup|Cap|Inj|Cream|Oint|Drop|Inhal|Patch|Powder)\.\s+[A-Z]",
                @"\b(?:tablet|capsule|syrup|suspension|injection|cream|ointment|drops|inhaler|patch)\b",
                
                // Dosage patterns
                @"\d+\s*(?:mg|ml|g|mcg|iu|%|units?)\b",
                @"\d+\s*(?:tablet|capsule|tab|cap|spray|puff)\b",
                @"\d+\.\d+\s*(?:mg|ml|g)\b",
                
                // Frequency/duration patterns
                @"\b(?:od|bd|td|qd|bid|tid|qid|sos|prn|stat|pc|ac|hs)\b",
                @"×\s*\d+\s*(?:days?|day|weeks?|months?)\b",
                @"\b(?:once|twice|thrice|\d+\s*times)\s*(?:daily|a\s*day|per\s*day)\b",
                @"\b(?:every\s*\d+\s*(?:hours?|days?|weeks?|months?))\b",
                
                // Timing patterns
                @"\b(?:morning|noon|afternoon|evening|night|bedtime|before\s*sleep)\b",
                @"\b(?:after\s*meals|before\s*meals|with\s*food|empty\s*stomach)\b",
            };

            return patterns.Any(pattern => Regex.IsMatch(line, pattern, RegexOptions.IgnoreCase));
        }

        private bool IsPatientDoctorInfo(string line)
        {
            var patterns = new[]
            {
                @"^(?:Dr\.|Doctor|Physician|Consultant|Prescriber)[\s:].*",
                @"^(?:Patient|Pt\.|Name|DOB|Age|Gender|Sex|M/F|Weight|Height)[\s:].*",
                @"^(?:Registration|Reg\.|License|MD|MBBS|DNB|Qualification)[\s:].*",
                @"^(?:Speciality|Specialization|Department|Dept\.)[\s:].*",
                @"\b(?:years?\s*old|y/o|m/f|male|female|boy|girl)\b",
                @"\b\d+\s*(?:years|yrs|months|mos|weeks|wks)\b",
            };

            return patterns.Any(pattern => Regex.IsMatch(line, pattern, RegexOptions.IgnoreCase));
        }

        private bool IsDateInfo(string line)
        {
            return Regex.IsMatch(line, @"\b\d{1,2}[-/]\d{1,2}[-/]\d{2,4}\b") ||
                   Regex.IsMatch(line, @"\b(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]*\s+\d{1,2},?\s+\d{4}\b", RegexOptions.IgnoreCase) ||
                   line.Contains("Date:", StringComparison.OrdinalIgnoreCase) ||
                   line.Contains("Dated:", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsInstructionInfo(string line)
        {
            var patterns = new[]
            {
                @"\b(?:take|use|apply|inhale|insert|swallow|chew|dissolve|shake\s*well)\b",
                @"\b(?:before|after|with|without)\s*(?:food|meals|breakfast|lunch|dinner)\b",
                @"\b(?:continue|stop|start|change|increase|decrease|add|discontinue)\b",
                @"\b(?:if\s*needed|as\s*required|when\s*necessary|on\s*demand)\b",
                @"\b(?:avoid|do\s*not|never|should\s*not|must\s*not)\b",
            };

            return patterns.Any(pattern => Regex.IsMatch(line, pattern, RegexOptions.IgnoreCase));
        }

        private bool IsPrescriptionHeader(string line)
        {
            var patterns = new[]
            {
                @"^(?:PRESCRIPTION|RX|Rx|Medication|Medicines|Drugs|Treatment|Therapy|Advice|Instructions)$",
                @"^(?:Diagnosis|Diagnoses|Findings|Observations|Assessment)$",
                @"^(?:Allergies|Allergy|Contraindications|Precautions)$",
                @"^(?:Follow\s*up|Review|Next\s*visit|Return\s*visit)$",
            };

            return patterns.Any(pattern => Regex.IsMatch(line, pattern, RegexOptions.IgnoreCase));
        }

        private int GetLinePriority(string line)
        {
            // Priority scoring for lines
            int priority = 0;

            if (line.StartsWith("Dr.", StringComparison.OrdinalIgnoreCase)) priority += 10;
            if (line.StartsWith("Patient:", StringComparison.OrdinalIgnoreCase)) priority += 9;
            if (line.Contains("Reg No:", StringComparison.OrdinalIgnoreCase)) priority += 8;
            if (Regex.IsMatch(line, @"\d{1,2}/\d{1,2}/\d{2,4}")) priority += 7;
            if (IsMedicationContent(line)) priority += 6;
            if (line.Contains("mg") || line.Contains("ml")) priority += 5;
            if (line.Contains("×") || line.Contains("x")) priority += 4;
            if (line.Contains("days", StringComparison.OrdinalIgnoreCase)) priority += 3;
            if (line.Contains(":")) priority += 2;

            return priority;
        }

        #endregion

        #region Helper Methods

        private Dictionary<string, string> GetCommonCorrections()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Syp.", "Syp." },
                { "Tab.", "Tab." },
                { "Sup.", "Sup." },
                { "Cap.", "Cap." },
                { "Inj.", "Inj." },
                { "0 0 0", "0-0-0" },
                { "OD", "OD" },
                { "BD", "BD" },
                { "TDS", "TDS" },
                { "QID", "QID" },
                { "SOS", "SOS" },
                { "PRN", "PRN" },
                { "PC", "PC" },
                { "AC", "AC" },
                { "HS", "HS" },
                { "cos", "SOS" },
                { "odpc", "OD PC" },
                { "mls", "ml" },
                { "mgs", "mg" },
            };
        }

        private string[] GetNoisePatterns()
        {
            return new[]
            {
                // Clinic/Hospital info
                @"(?i)(?:apollo|fortis|max|manipal|medanta|asian|city)\s+(?:hospital|clinic|health|diagnostic|medical).*?\n",
                
                // Address and contact
                @"(?i)address\s*:.*?\n",
                @"(?i)phone\s*:.*?\n",
                @"(?i)email\s*:.*?\n",
                @"(?i)website\s*:.*?\n",
                @"\b\d{10,}\b", // Long numbers
                
                // Legal and administrative
                @"(?i)(?:this\s+prescription|computerized\s+prescription|electronic\s+prescription).*?\n",
                @"(?i)(?:terms\s+and\s+conditions|disclaimer|confidentiality).*?\n",
                @"(?i)(?:signature|sign|stamp|seal).*?\n",
                
                // Generic noise
                @"^[\s\W]*$", // Lines with only symbols/whitespace
                @"^\d+$", // Standalone numbers
                @"^[A-Z\s]{20,}$", // ALL CAPS long lines
            };
        }

        private double CalculateSimilarity(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
                return 0.0;

            s1 = s1.ToLower();
            s2 = s2.ToLower();

            var words1 = s1.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var words2 = s2.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var intersect = words1.Intersect(words2).Count();
            var union = words1.Union(words2).Count();

            if (union == 0) return 0.0;

            return (double)intersect / union;
        }

        #endregion

        #region Supporting Classes

        public class PreprocessorConfiguration
        {
            public int MaxLines { get; set; } = 50;  // Maximum number of lines to retain after processing
            public bool RemovePhoneNumbers { get; set; } = true;
            public bool RemoveAddresses { get; set; } = true;
            public bool RemoveDisclaimers { get; set; } = true;
            public bool FixOcrErrors { get; set; } = true;
        }

        public enum ProcessingMode
        {
            Balanced,    // Good balance of cleanup and preservation
            Aggressive,  // Remove all non-essential content
            Minimal      // Extract only essential information
        }

        private class ProcessingPipeline
        {
            private readonly List<Func<string, string>> _steps = new();

            public void AddStep(Func<string, string> step)
            {
                _steps.Add(step);
            }

            public string Process(string input)
            {
                var result = input;
                foreach (var step in _steps)
                {
                    result = step(result);
                }
                return result;
            }
        }

        #endregion

    }
}
