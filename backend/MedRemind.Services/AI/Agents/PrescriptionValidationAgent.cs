using MedRemind.Core.DTOs;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MedRemind.Services.AI.Agents;

/// <summary>
/// Multi-agent prescription validation system
/// Validates and analyzes prescription parsing results from multiple LLM parsers
/// Generates comprehensive analysis reports
/// </summary>
public class PrescriptionValidationAgent
{
    private readonly ILogger<PrescriptionValidationAgent>? _logger;

    public PrescriptionValidationAgent(ILogger<PrescriptionValidationAgent>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validate and analyze prescription parsing results from all parsers
    /// </summary>
    public async Task<PrescriptionAnalysisReport> ValidateAndAnalyzeAsync(
        string ocrText,
        Dictionary<string, PrescriptionReadResult> parserResults,
        string? prescriptionFileName = null,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        _logger?.LogInformation("?? Starting prescription validation and analysis...");

        var report = new PrescriptionAnalysisReport
        {
            PrescriptionFileName = prescriptionFileName,
            OcrTextLength = ocrText?.Length ?? 0
        };

        try
        {
            // Step 1: Analyze each parser's result
            _logger?.LogInformation("?? Step 1: Analyzing individual parser results...");
            report.ParserResults = AnalyzeParserResults(parserResults);

            // Step 2: Cross-validate results
            _logger?.LogInformation("?? Step 2: Cross-validating parser results...");
            report.CrossValidation = CrossValidateResults(parserResults);

            // Step 3: Calculate quality metrics
            _logger?.LogInformation("?? Step 3: Calculating quality metrics...");
            report.QualityMetrics = CalculateQualityMetrics(ocrText, parserResults, report.CrossValidation);

            // Step 4: Identify issues
            _logger?.LogInformation("?? Step 4: Identifying validation issues...");
            report.Issues = IdentifyIssues(parserResults, report.CrossValidation);

            // Step 5: Generate warnings
            _logger?.LogInformation("?? Step 5: Generating warnings...");
            report.Warnings = GenerateWarnings(parserResults, report.CrossValidation, report.QualityMetrics);

            // Step 6: Make final recommendation
            _logger?.LogInformation("? Step 6: Making final recommendation...");
            report.FinalRecommendation = MakeFinalRecommendation(parserResults, report);

            report.ProcessingTime = DateTime.UtcNow - startTime;

            _logger?.LogInformation($"? Validation complete in {report.ProcessingTime.TotalSeconds:F2}s");
            _logger?.LogInformation($"   Status: {report.FinalRecommendation.Status}");
            _logger?.LogInformation($"   Confidence: {report.FinalRecommendation.Confidence:P0}");
            _logger?.LogInformation($"   Issues: {report.Issues.Count}");

            return report;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "? Validation failed");
            report.ProcessingTime = DateTime.UtcNow - startTime;
            report.Issues.Add(new ValidationIssue
            {
                Severity = IssueSeverity.Critical,
                Category = "Validation Error",
                Description = $"Validation process failed: {ex.Message}"
            });
            return report;
        }
    }

    private List<ParserAnalysis> AnalyzeParserResults(Dictionary<string, PrescriptionReadResult> results)
    {
        var analyses = new List<ParserAnalysis>();

        foreach (var (parserName, result) in results)
        {
            var analysis = new ParserAnalysis
            {
                ParserName = parserName,
                Success = result.Success,
                MedicationCount = result.Medications?.Count ?? 0,
                HasPatientInfo = result.Patient != null && !string.IsNullOrWhiteSpace(result.Patient.Name),
                HasDoctorInfo = result.Doctor != null && !string.IsNullOrWhiteSpace(result.Doctor.Name),
                HasPrescriptionDate = result.PrescriptionDate.HasValue,
                AverageConfidence = result.Medications?.Any() == true
                    ? result.Medications.Average(m => m.ConfidenceScore ?? 0.0)
                    : 0.0
            };

            // Calculate completeness score
            analysis.CompletenessScore = CalculateCompletenessScore(result);

            analyses.Add(analysis);

            _logger?.LogInformation($"   {parserName}: {analysis.MedicationCount} medications, " +
                                  $"Confidence: {analysis.AverageConfidence:P0}, " +
                                  $"Completeness: {analysis.CompletenessScore:P0}");
        }

        return analyses;
    }

    private double CalculateCompletenessScore(PrescriptionReadResult result)
    {
        double score = 0.0;
        int maxPoints = 10;

        // Patient info (3 points)
        if (result.Patient != null)
        {
            if (!string.IsNullOrWhiteSpace(result.Patient.Name)) score += 1.0;
            if (result.Patient.Age.HasValue) score += 1.0;
            if (!string.IsNullOrWhiteSpace(result.Patient.Gender)) score += 1.0;
        }

        // Doctor info (3 points)
        if (result.Doctor != null)
        {
            if (!string.IsNullOrWhiteSpace(result.Doctor.Name)) score += 1.0;
            if (!string.IsNullOrWhiteSpace(result.Doctor.Specialization)) score += 1.0;
            if (!string.IsNullOrWhiteSpace(result.Doctor.RegistrationNumber)) score += 1.0;
        }

        // Prescription date (1 point)
        if (result.PrescriptionDate.HasValue) score += 1.0;

        // Medications (3 points)
        if (result.Medications?.Any() == true)
        {
            score += 1.0; // Has medications
            var avgMedCompleteness = result.Medications.Average(m =>
            {
                int medPoints = 0;
                if (!string.IsNullOrWhiteSpace(m.Name)) medPoints++;
                if (!string.IsNullOrWhiteSpace(m.Dosage)) medPoints++;
                if (m.FrequencyCount > 0) medPoints++;
                if (m.DurationDays > 0) medPoints++;
                return medPoints / 4.0;
            });
            score += avgMedCompleteness * 2.0;
        }

        return score / maxPoints;
    }

    private CrossValidationResult CrossValidateResults(Dictionary<string, PrescriptionReadResult> results)
    {
        var crossVal = new CrossValidationResult();

        if (results.Count < 2)
        {
            _logger?.LogWarning("   Less than 2 parsers - skipping cross-validation");
            return crossVal;
        }

        var successfulResults = results.Where(r => r.Value.Success).ToList();

        if (successfulResults.Count < 2)
        {
            _logger?.LogWarning("   Less than 2 successful parsers - limited cross-validation");
            return crossVal;
        }

        // Check patient name consistency
        var patientNames = successfulResults
            .Select(r => r.Value.Patient?.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToList();
        crossVal.PatientNameMatch = patientNames.Count > 1 && patientNames.Distinct().Count() == 1;

        // Check doctor name consistency
        var doctorNames = successfulResults
            .Select(r => r.Value.Doctor?.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToList();
        crossVal.DoctorNameMatch = doctorNames.Count > 1 && doctorNames.Distinct().Count() == 1;

        // Check date consistency
        var dates = successfulResults
            .Select(r => r.Value.PrescriptionDate)
            .Where(d => d.HasValue)
            .Select(d => d!.Value.Date)
            .ToList();
        crossVal.DateConsistency = dates.Count > 1 && dates.Distinct().Count() == 1;

        // Find consensus medications
        var allMedications = successfulResults
            .SelectMany(r => r.Value.Medications ?? new List<MedicationData>())
            .GroupBy(m => m.Name?.ToLowerInvariant())
            .Where(g => g.Key != null);

        foreach (var medGroup in allMedications)
        {
            if (medGroup.Count() >= Math.Ceiling(successfulResults.Count / 2.0))
            {
                crossVal.ConsensusMedications.Add(medGroup.Key!);
            }
        }

        // Calculate medication consistency
        if (successfulResults.Count > 1)
        {
            var medCounts = successfulResults.Select(r => r.Value.Medications?.Count ?? 0).ToList();
            var avgCount = medCounts.Average();
            var maxDeviation = medCounts.Max() - medCounts.Min();
            crossVal.MedicationConsistency = avgCount > 0 ? 1.0 - (maxDeviation / avgCount) : 0.0;
        }

        // Find medication conflicts
        crossVal.ConflictingMedications = FindMedicationConflicts(successfulResults);

        // Calculate overall agreement
        crossVal.OverallAgreement = CalculateOverallAgreement(crossVal);

        _logger?.LogInformation($"   Patient match: {crossVal.PatientNameMatch}");
        _logger?.LogInformation($"   Doctor match: {crossVal.DoctorNameMatch}");
        _logger?.LogInformation($"   Date consistency: {crossVal.DateConsistency}");
        _logger?.LogInformation($"   Medication consistency: {crossVal.MedicationConsistency:P0}");
        _logger?.LogInformation($"   Overall agreement: {crossVal.OverallAgreement:P0}");

        return crossVal;
    }

    private List<MedicationConflict> FindMedicationConflicts(List<KeyValuePair<string, PrescriptionReadResult>> results)
    {
        var conflicts = new List<MedicationConflict>();

        // Group medications by name
        var medicationGroups = results
            .SelectMany(r => (r.Value.Medications ?? new List<MedicationData>()).Select(m => new { Parser = r.Key, Medication = m }))
            .GroupBy(x => x.Medication.Name?.ToLowerInvariant())
            .Where(g => g.Key != null && g.Count() > 1);

        foreach (var group in medicationGroups)
        {
            var medications = group.ToList();

            // Check dosage conflicts
            var dosages = medications.Select(m => m.Medication.Dosage).Distinct().ToList();
            if (dosages.Count > 1)
            {
                conflicts.Add(new MedicationConflict
                {
                    MedicationName = group.Key!,
                    ConflictType = "Dosage Mismatch",
                    ParserValues = medications.ToDictionary(m => m.Parser, m => m.Medication.Dosage ?? "N/A"),
                    SuggestedResolution = $"Most common: {dosages.GroupBy(d => d).OrderByDescending(g => g.Count()).First().Key}"
                });
            }

            // Check frequency conflicts
            var frequencies = medications.Select(m => m.Medication.FrequencyCount).Distinct().ToList();
            if (frequencies.Count > 1)
            {
                conflicts.Add(new MedicationConflict
                {
                    MedicationName = group.Key!,
                    ConflictType = "Frequency Mismatch",
                    ParserValues = medications.ToDictionary(m => m.Parser, m => m.Medication.FrequencyCount.ToString()),
                    SuggestedResolution = $"Most common: {frequencies.GroupBy(f => f).OrderByDescending(g => g.Count()).First().Key}x daily"
                });
            }
        }

        return conflicts;
    }

    private double CalculateOverallAgreement(CrossValidationResult crossVal)
    {
        double score = 0.0;
        int checks = 0;

        if (crossVal.PatientNameMatch) score += 1.0;
        checks++;

        if (crossVal.DoctorNameMatch) score += 1.0;
        checks++;

        if (crossVal.DateConsistency) score += 1.0;
        checks++;

        score += crossVal.MedicationConsistency;
        checks++;

        return checks > 0 ? score / checks : 0.0;
    }

    private QualityMetrics CalculateQualityMetrics(
        string ocrText,
        Dictionary<string, PrescriptionReadResult> results,
        CrossValidationResult crossVal)
    {
        var metrics = new QualityMetrics
        {
            ParserConsistency = crossVal.OverallAgreement
        };

        // OCR quality (based on text characteristics)
        metrics.OcrQuality = CalculateOcrQuality(ocrText);

        // Data completeness (average across all parsers)
        var successfulResults = results.Where(r => r.Value.Success).ToList();
        if (successfulResults.Any())
        {
            metrics.DataCompleteness = successfulResults.Average(r =>
            {
                int points = 0;
                int maxPoints = 4;

                if (r.Value.Patient != null && !string.IsNullOrWhiteSpace(r.Value.Patient.Name)) points++;
                if (r.Value.Doctor != null && !string.IsNullOrWhiteSpace(r.Value.Doctor.Name)) points++;
                if (r.Value.PrescriptionDate.HasValue) points++;
                if (r.Value.Medications?.Any() == true) points++;

                return (double)points / maxPoints;
            });
        }

        // Overall quality
        metrics.OverallQuality = (metrics.OcrQuality + metrics.DataCompleteness + metrics.ParserConsistency) / 3.0;

        // Readability score
        metrics.ReadabilityScore = CalculateReadabilityScore(ocrText);

        return metrics;
    }

    private double CalculateOcrQuality(string ocrText)
    {
        if (string.IsNullOrWhiteSpace(ocrText)) return 0.0;

        double score = 1.0;

        // Penalize for too many special characters
        var specialCharRatio = ocrText.Count(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c)) / (double)ocrText.Length;
        if (specialCharRatio > 0.2) score -= 0.2;

        // Penalize for excessive line breaks
        var lineBreakRatio = ocrText.Count(c => c == '\n') / (double)ocrText.Length;
        if (lineBreakRatio > 0.15) score -= 0.2;

        // Penalize for very short text
        if (ocrText.Length < 100) score -= 0.3;

        return Math.Max(0.0, score);
    }

    private double CalculateReadabilityScore(string ocrText)
    {
        if (string.IsNullOrWhiteSpace(ocrText)) return 0.0;

        double score = 1.0;

        // Check for common prescription terms
        var commonTerms = new[] { "tab", "cap", "syp", "mg", "ml", "dose", "rx", "dr", "date" };
        var foundTerms = commonTerms.Count(term => ocrText.ToLowerInvariant().Contains(term));
        var termRatio = foundTerms / (double)commonTerms.Length;

        score = termRatio * 0.7 + 0.3; // Weight found terms

        return score;
    }

    private List<ValidationIssue> IdentifyIssues(
        Dictionary<string, PrescriptionReadResult> results,
        CrossValidationResult crossVal)
    {
        var issues = new List<ValidationIssue>();

        // Check if no parsers succeeded
        if (!results.Any(r => r.Value.Success))
        {
            issues.Add(new ValidationIssue
            {
                Severity = IssueSeverity.Critical,
                Category = "Parsing Failure",
                Description = "All parsers failed to extract prescription data",
                AffectedParsers = results.Keys.ToList(),
                Recommendation = "Check OCR text quality and retry with better image"
            });
        }

        // Check for medication conflicts
        if (crossVal.ConflictingMedications.Any())
        {
            foreach (var conflict in crossVal.ConflictingMedications)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = IssueSeverity.High,
                    Category = "Data Conflict",
                    Description = $"{conflict.MedicationName}: {conflict.ConflictType}",
                    AffectedParsers = conflict.ParserValues.Keys.ToList(),
                    Recommendation = conflict.SuggestedResolution
                });
            }
        }

        // Check for missing critical data
        var successfulResults = results.Where(r => r.Value.Success).ToList();
        if (successfulResults.Any())
        {
            var hasPatient = successfulResults.Any(r => r.Value.Patient != null && !string.IsNullOrWhiteSpace(r.Value.Patient.Name));
            if (!hasPatient)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = IssueSeverity.Medium,
                    Category = "Missing Data",
                    Description = "Patient information not found by any parser",
                    Recommendation = "Verify patient details manually"
                });
            }

            var hasDate = successfulResults.Any(r => r.Value.PrescriptionDate.HasValue);
            if (!hasDate)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = IssueSeverity.Medium,
                    Category = "Missing Data",
                    Description = "Prescription date not found by any parser",
                    Recommendation = "Check prescription for date field"
                });
            }
        }

        return issues;
    }

    private List<string> GenerateWarnings(
        Dictionary<string, PrescriptionReadResult> results,
        CrossValidationResult crossVal,
        QualityMetrics metrics)
    {
        var warnings = new List<string>();

        // Low OCR quality warning
        if (metrics.OcrQuality < 0.6)
        {
            warnings.Add($"Low OCR quality detected ({metrics.OcrQuality:P0}). Results may be inaccurate.");
        }

        // Low parser consistency warning
        if (metrics.ParserConsistency < 0.7 && results.Count(r => r.Value.Success) > 1)
        {
            warnings.Add($"Parsers show low consistency ({metrics.ParserConsistency:P0}). Manual review recommended.");
        }

        // Medication count mismatch warning
        var medCounts = results.Where(r => r.Value.Success)
            .Select(r => r.Value.Medications?.Count ?? 0)
            .Where(c => c > 0)
            .ToList();

        if (medCounts.Any() && medCounts.Max() - medCounts.Min() > 2)
        {
            warnings.Add($"Significant medication count variation ({medCounts.Min()}-{medCounts.Max()}). Some medications may be missing.");
        }

        // Date inconsistency warning
        if (!crossVal.DateConsistency && results.Count(r => r.Value.PrescriptionDate.HasValue) > 1)
        {
            warnings.Add("Parsers report different prescription dates. Verify manually.");
        }

        return warnings;
    }

    private ValidationRecommendation MakeFinalRecommendation(
        Dictionary<string, PrescriptionReadResult> results,
        PrescriptionAnalysisReport report)
    {
        var recommendation = new ValidationRecommendation();

        var successfulResults = results.Where(r => r.Value.Success).ToList();

        if (!successfulResults.Any())
        {
            recommendation.Status = ValidationStatus.Failed;
            recommendation.Confidence = 0.0;
            recommendation.RequiresManualReview = true;
            recommendation.Reason = "All parsers failed to extract data";
            return recommendation;
        }

        // Find best parser
        var bestParser = report.ParserResults
            .Where(p => p.Success)
            .OrderByDescending(p => p.CompletenessScore)
            .ThenByDescending(p => p.AverageConfidence)
            .FirstOrDefault();

        recommendation.RecommendedParser = bestParser?.ParserName;
        recommendation.Confidence = bestParser?.AverageConfidence ?? 0.0;

        // Determine status based on metrics
        if (report.QualityMetrics.OverallQuality >= 0.9 && report.CrossValidation.OverallAgreement >= 0.9)
        {
            recommendation.Status = ValidationStatus.Excellent;
            recommendation.RequiresManualReview = false;
            recommendation.Reason = "High quality data with excellent parser agreement";
        }
        else if (report.QualityMetrics.OverallQuality >= 0.7 && report.CrossValidation.OverallAgreement >= 0.7)
        {
            recommendation.Status = ValidationStatus.Good;
            recommendation.RequiresManualReview = false;
            recommendation.Reason = "Good quality data with acceptable parser agreement";
        }
        else if (report.QualityMetrics.OverallQuality >= 0.5)
        {
            recommendation.Status = ValidationStatus.Acceptable;
            recommendation.RequiresManualReview = report.Issues.Any(i => i.Severity >= IssueSeverity.High);
            recommendation.Reason = "Acceptable quality but has some inconsistencies";
        }
        else if (report.Issues.Any(i => i.Severity == IssueSeverity.Critical))
        {
            recommendation.Status = ValidationStatus.Failed;
            recommendation.RequiresManualReview = true;
            recommendation.Reason = "Critical issues detected";
        }
        else
        {
            recommendation.Status = ValidationStatus.NeedsReview;
            recommendation.RequiresManualReview = true;
            recommendation.Reason = "Low quality or significant inconsistencies detected";
        }

        // Add suggestions
        if (report.Issues.Any())
        {
            recommendation.Suggestions.Add($"Address {report.Issues.Count} validation issue(s)");
        }

        if (report.QualityMetrics.OcrQuality < 0.7)
        {
            recommendation.Suggestions.Add("Consider re-scanning with better image quality");
        }

        if (report.CrossValidation.OverallAgreement < 0.7 && successfulResults.Count > 1)
        {
            recommendation.Suggestions.Add("Manual verification recommended due to parser disagreement");
        }

        return recommendation;
    }
}
