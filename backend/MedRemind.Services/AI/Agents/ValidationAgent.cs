using Microsoft.SemanticKernel;
using System.ComponentModel;
using MedRemind.Core.DTOs;

namespace MedRemind.Services.AI.Agents;

/// <summary>
/// Agent 3: Validates extracted data structure and triggers re-extraction if needed
/// </summary>
public class ValidationAgent
{
    private readonly PrescriptionDataExtractionAgent _extractionAgent;
    private const int MAX_RETRY_ATTEMPTS = 3;
    private const double MINIMUM_MATCH_THRESHOLD = 0.80; // 80% structure match required

    public ValidationAgent(PrescriptionDataExtractionAgent extractionAgent)
    {
        _extractionAgent = extractionAgent ?? throw new ArgumentNullException(nameof(extractionAgent));
    }

    /// <summary>
    /// Validate extracted prescription data and retry with more specific prompts if needed
    /// </summary>
    [KernelFunction("validate_and_retry")]
    [Description("Validates extracted prescription data structure and retries with more specific prompts if match is below 80%")]
    public async Task<ValidationResult> ValidateAndRetryAsync(
        [Description("OCR text to extract from")] string ocrText,
        [Description("Initially extracted data to validate")] ExtractionResult initialExtraction,
        CancellationToken cancellationToken = default)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("?? Validation Agent: Starting validation...");

            var validationResult = new ValidationResult
            {
                Success = true,
                Attempts = new List<ValidationAttempt>()
            };

            // First attempt - validate initial extraction
            var firstAttempt = ValidateStructure(initialExtraction, "normal", 1);
            validationResult.Attempts.Add(firstAttempt);

            System.Diagnostics.Debug.WriteLine($"   Attempt 1 - Match score: {firstAttempt.MatchScore:P0}");
            System.Diagnostics.Debug.WriteLine($"   Missing fields: {string.Join(", ", firstAttempt.MissingFields)}");

            // If first attempt meets threshold, return success
            if (firstAttempt.MatchScore >= MINIMUM_MATCH_THRESHOLD)
            {
                System.Diagnostics.Debug.WriteLine($"? Validation Agent: Success on first attempt");
                validationResult.FinalExtraction = initialExtraction;
                validationResult.FinalMatchScore = firstAttempt.MatchScore;
                validationResult.TotalAttempts = 1;
                return validationResult;
            }

            // Retry with progressively more specific prompts
            ExtractionResult currentExtraction = initialExtraction;
            string[] specificityLevels = { "detailed", "strict" };

            for (int i = 0; i < specificityLevels.Length && i < MAX_RETRY_ATTEMPTS - 1; i++)
            {
                var specificityLevel = specificityLevels[i];
                var attemptNumber = i + 2; // Attempt 2, 3, etc.

                System.Diagnostics.Debug.WriteLine($"?? Validation Agent: Retry {attemptNumber} with '{specificityLevel}' mode");

                // Re-extract with more specific prompt
                var retryExtraction = await _extractionAgent.ExtractPrescriptionDataAsync(
                    ocrText,
                    specificityLevel,
                    cancellationToken);

                // Validate retry result
                var retryAttempt = ValidateStructure(retryExtraction, specificityLevel, attemptNumber);
                validationResult.Attempts.Add(retryAttempt);

                System.Diagnostics.Debug.WriteLine($"   Attempt {attemptNumber} - Match score: {retryAttempt.MatchScore:P0}");
                System.Diagnostics.Debug.WriteLine($"   Missing fields: {string.Join(", ", retryAttempt.MissingFields)}");

                currentExtraction = retryExtraction;

                // If threshold met, return success
                if (retryAttempt.MatchScore >= MINIMUM_MATCH_THRESHOLD)
                {
                    System.Diagnostics.Debug.WriteLine($"? Validation Agent: Success on attempt {attemptNumber}");
                    validationResult.FinalExtraction = currentExtraction;
                    validationResult.FinalMatchScore = retryAttempt.MatchScore;
                    validationResult.TotalAttempts = attemptNumber;
                    return validationResult;
                }
            }

            // All retries exhausted, return best attempt
            var bestAttempt = validationResult.Attempts.OrderByDescending(a => a.MatchScore).First();
            var bestExtraction = validationResult.Attempts.First(a => a.MatchScore == bestAttempt.MatchScore);

            System.Diagnostics.Debug.WriteLine($"?? Validation Agent: Max retries reached");
            System.Diagnostics.Debug.WriteLine($"   Best match score: {bestAttempt.MatchScore:P0}");
            System.Diagnostics.Debug.WriteLine($"   Using attempt {bestExtraction.AttemptNumber}");

            validationResult.Success = bestAttempt.MatchScore >= 0.60; // Accept if at least 60%
            validationResult.FinalExtraction = currentExtraction;
            validationResult.FinalMatchScore = bestAttempt.MatchScore;
            validationResult.TotalAttempts = validationResult.Attempts.Count;
            validationResult.WarningMessage = bestAttempt.MatchScore < MINIMUM_MATCH_THRESHOLD
                ? $"Data extraction quality is {bestAttempt.MatchScore:P0}. Manual review recommended."
                : null;

            return validationResult;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Validation Agent: Error - {ex.Message}");
            return new ValidationResult
            {
                Success = false,
                ErrorMessage = $"Validation failed: {ex.Message}",
                TotalAttempts = 1
            };
        }
    }

    /// <summary>
    /// Validate structure of extracted data against expected schema
    /// </summary>
    private ValidationAttempt ValidateStructure(ExtractionResult extraction, string specificityLevel, int attemptNumber)
    {
        var attempt = new ValidationAttempt
        {
            AttemptNumber = attemptNumber,
            SpecificityLevel = specificityLevel,
            MissingFields = new List<string>(),
            Warnings = new List<string>()
        };

        if (!extraction.Success || extraction.StructuredData == null)
        {
            attempt.MatchScore = 0.0;
            attempt.MissingFields.Add("Extraction failed - no data");
            return attempt;
        }

        var data = extraction.StructuredData;
        int totalFields = 0;
        int presentFields = 0;

        // Validate Patient data (20% weight)
        totalFields += 3;
        if (!string.IsNullOrWhiteSpace(data.Patient?.Name))
            presentFields++;
        else
            attempt.MissingFields.Add("Patient.Name");

        if (data.Patient?.Age.HasValue == true)
            presentFields++;
        else
            attempt.Warnings.Add("Patient.Age missing");

        if (!string.IsNullOrWhiteSpace(data.Patient?.Gender))
            presentFields++;
        else
            attempt.Warnings.Add("Patient.Gender missing");

        // Validate Doctor data (20% weight)
        totalFields += 3;
        if (!string.IsNullOrWhiteSpace(data.Doctor?.Name))
            presentFields++;
        else
            attempt.MissingFields.Add("Doctor.Name");

        if (!string.IsNullOrWhiteSpace(data.Doctor?.RegistrationNumber))
            presentFields++;
        else
            attempt.Warnings.Add("Doctor.RegistrationNumber missing");

        if (!string.IsNullOrWhiteSpace(data.Doctor?.Specialization))
            presentFields++;
        else
            attempt.Warnings.Add("Doctor.Specialization missing");

        // Validate Prescription Date (10% weight)
        totalFields += 1;
        if (!string.IsNullOrWhiteSpace(data.PrescriptionDate))
            presentFields++;
        else
            attempt.MissingFields.Add("PrescriptionDate");

        // Validate Medications (50% weight - most critical)
        if (data.Medications == null || !data.Medications.Any())
        {
            attempt.MissingFields.Add("Medications (no medications found)");
            totalFields += 8; // Expected fields per medication
        }
        else
        {
            foreach (var med in data.Medications)
            {
                totalFields += 8; // 8 fields per medication

                if (!string.IsNullOrWhiteSpace(med.Name))
                    presentFields++;
                else
                    attempt.MissingFields.Add($"Medication.Name");

                if (!string.IsNullOrWhiteSpace(med.Dosage))
                    presentFields++;
                else
                    attempt.Warnings.Add($"Medication '{med.Name}': Dosage missing");

                if (!string.IsNullOrWhiteSpace(med.Unit))
                    presentFields++;
                else
                    attempt.Warnings.Add($"Medication '{med.Name}': Unit missing");

                if (!string.IsNullOrWhiteSpace(med.Frequency))
                    presentFields++;
                else
                    attempt.Warnings.Add($"Medication '{med.Name}': Frequency missing");

                if (med.FrequencyCount > 0)
                    presentFields++;
                else
                    attempt.Warnings.Add($"Medication '{med.Name}': FrequencyCount invalid");

                if (med.DurationDays > 0)
                    presentFields++;
                else
                    attempt.Warnings.Add($"Medication '{med.Name}': DurationDays missing");

                if (med.ConfidenceScore > 0)
                    presentFields++;
                else
                    attempt.Warnings.Add($"Medication '{med.Name}': ConfidenceScore missing");

                if (!string.IsNullOrWhiteSpace(med.Timing) || !string.IsNullOrWhiteSpace(med.Instructions))
                    presentFields++;
                // Instructions/Timing are optional, no warning
            }
        }

        // Calculate match score
        attempt.MatchScore = totalFields > 0 ? (double)presentFields / totalFields : 0.0;

        return attempt;
    }
}

/// <summary>
/// Result from validation with retry logic
/// </summary>
public class ValidationResult
{
    public bool Success { get; set; }
    public ExtractionResult? FinalExtraction { get; set; }
    public double FinalMatchScore { get; set; }
    public int TotalAttempts { get; set; }
    public List<ValidationAttempt> Attempts { get; set; } = new();
    public string? WarningMessage { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Single validation attempt details
/// </summary>
public class ValidationAttempt
{
    public int AttemptNumber { get; set; }
    public string SpecificityLevel { get; set; } = string.Empty;
    public double MatchScore { get; set; }
    public List<string> MissingFields { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
