using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MedRemind.Core.Data;
using MedRemind.Core.Models;
using MedRemind.Core.Enums;

namespace MedRemind.Services.Prescriptions;

/// <summary>
/// Service for detecting duplicate prescriptions and managing OCR results
/// Uses OCR text hash for fast duplicate detection
/// </summary>
public class PrescriptionDeduplicationService
{
    private readonly MedRemindDbContext _context;

    public PrescriptionDeduplicationService(MedRemindDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Check if prescription with similar OCR text already exists
    /// </summary>
    /// <param name="ocrText">OCR extracted text</param>
    /// <param name="userId">User ID</param>
    /// <param name="newPrescriptionId">The newly created prescription ID to update if duplicate found</param>
    public async Task<DuplicateCheckResult> CheckForDuplicateAsync(string ocrText, int userId, int? newPrescriptionId = null)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("?? Deduplication: Checking for duplicate prescription...");
            System.Diagnostics.Debug.WriteLine($"   User ID: {userId}");
            System.Diagnostics.Debug.WriteLine($"   New Prescription ID: {newPrescriptionId?.ToString() ?? "N/A"}");
            System.Diagnostics.Debug.WriteLine($"   OCR text length: {ocrText.Length} chars");

            // Generate hash for quick comparison
            var ocrHash = GenerateHash(ocrText);
            System.Diagnostics.Debug.WriteLine($"   OCR Hash: {ocrHash.Substring(0, 16)}...");

            // Check for exact match by hash
            var exactMatch = await _context.PrescriptionOCRResults
                .Include(r => r.Prescription)
                .Where(r => r.OCRTextHash == ocrHash && 
                           r.Prescription!.UserId == userId)
                .FirstOrDefaultAsync();

            if (exactMatch != null)
            {
                System.Diagnostics.Debug.WriteLine($"? Found exact duplicate!");
                System.Diagnostics.Debug.WriteLine($"   Original Prescription ID: {exactMatch.PrescriptionId}");
                System.Diagnostics.Debug.WriteLine($"   Processed: {exactMatch.ProcessedAt:yyyy-MM-dd HH:mm}");
                System.Diagnostics.Debug.WriteLine($"   Medications: {exactMatch.MedicationCount}");

                // Mark the existing OCR result as duplicate reference
                exactMatch.Status = OcrProcessingStatus.Duplicate;
                await _context.SaveChangesAsync();

                // Update the NEW prescription as duplicate and map it to the original
                if (newPrescriptionId.HasValue)
                {
                    var newPrescription = await _context.Prescriptions.FindAsync(newPrescriptionId.Value);
                    if (newPrescription != null)
                    {
                        newPrescription.Status = PrescriptionStatus.Duplicate.ToString();
                        newPrescription.MappedPrescriptionId = exactMatch.PrescriptionId; // Map to original prescription
                        newPrescription.ProcessedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        
                        System.Diagnostics.Debug.WriteLine($"? New prescription {newPrescriptionId} marked as duplicate");
                        System.Diagnostics.Debug.WriteLine($"   Mapped to original prescription: {exactMatch.PrescriptionId}");
                    }
                }

                return new DuplicateCheckResult
                {
                    IsDuplicate = true,
                    ExistingResult = exactMatch,
                    SimilarityScore = 1.0,
                    Message = "Exact duplicate found - prescription already processed"
                };
            }

            // Check for similar text (fuzzy match)
            var similarMatches = await FindSimilarPrescriptionsAsync(ocrText, userId);

            if (similarMatches.Any())
            {
                var bestMatch = similarMatches.OrderByDescending(m => m.SimilarityScore).First();
                
                System.Diagnostics.Debug.WriteLine($"?? Found similar prescription (Similarity: {bestMatch.SimilarityScore:P0})");
                System.Diagnostics.Debug.WriteLine($"   Prescription ID: {bestMatch.Result.PrescriptionId}");

                if (bestMatch.SimilarityScore >= 0.95) // 95% similar
                {
                    // Mark the existing OCR result as duplicate reference
                    bestMatch.Result.Status = OcrProcessingStatus.Duplicate;
                    await _context.SaveChangesAsync();

                    // Update the NEW prescription as duplicate and map it to the original
                    if (newPrescriptionId.HasValue)
                    {
                        var newPrescription = await _context.Prescriptions.FindAsync(newPrescriptionId.Value);
                        if (newPrescription != null)
                        {
                            newPrescription.Status = PrescriptionStatus.Duplicate.ToString();
                            newPrescription.MappedPrescriptionId = bestMatch.Result.PrescriptionId; // Map to original prescription
                            newPrescription.ProcessedAt = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                            
                            System.Diagnostics.Debug.WriteLine($"? New prescription {newPrescriptionId} marked as duplicate");
                            System.Diagnostics.Debug.WriteLine($"   Mapped to original prescription: {bestMatch.Result.PrescriptionId}");
                            System.Diagnostics.Debug.WriteLine($"   Similarity Score: {bestMatch.SimilarityScore:P0}");
                        }
                    }

                    return new DuplicateCheckResult
                    {
                        IsDuplicate = true,
                        ExistingResult = bestMatch.Result,
                        SimilarityScore = bestMatch.SimilarityScore,
                        Message = $"Very similar prescription found ({bestMatch.SimilarityScore:P0} match)"
                    };
                }
                else if (bestMatch.SimilarityScore >= 0.80) // 80-95% similar
                {
                    return new DuplicateCheckResult
                    {
                        IsDuplicate = false,
                        ExistingResult = bestMatch.Result,
                        SimilarityScore = bestMatch.SimilarityScore,
                        Message = $"Similar prescription found ({bestMatch.SimilarityScore:P0} match) - proceed with caution"
                    };
                }
            }

            System.Diagnostics.Debug.WriteLine($"? No duplicates found - prescription is unique");

            return new DuplicateCheckResult
            {
                IsDuplicate = false,
                Message = "No duplicate found"
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Deduplication error: {ex.Message}");
            
            // Return as non-duplicate if check fails (fail-safe)
            return new DuplicateCheckResult
            {
                IsDuplicate = false,
                Message = $"Duplicate check failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Store OCR processing result in database
    /// </summary>
    public async Task<int> StoreOCRResultAsync(
        int prescriptionId,
        string ocrText,
        string? openAIResponse,
        string? claudeResponse,
        string? selectedResponse,
        string? selectedProvider,
        double comparisonScore,
        string? comparisonReason,
        int medicationCount,
        string? doctorName,
        string? patientName,
        DateTime? prescriptionDate,
        TimeSpan processingTime,
        int processingAttempts = 1)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("?? Storing OCR result in database...");

            var ocrResult = new PrescriptionOCRResult
            {
                PrescriptionId = prescriptionId,
                Status = OcrProcessingStatus.Processed,
                OCRText = ocrText,
                OCRTextHash = GenerateHash(ocrText),
                OpenAIResponse = openAIResponse,
                ClaudeResponse = claudeResponse,
                SelectedResponse = selectedResponse,
                SelectedProvider = selectedProvider,
                ComparisonScore = comparisonScore,
                ComparisonReason = comparisonReason,
                MedicationCount = medicationCount,
                DoctorName = doctorName,
                PatientName = patientName,
                PrescriptionDate = prescriptionDate,
                ProcessedAt = DateTime.UtcNow,
                ProcessingTime = processingTime,
                ProcessingAttempts = processingAttempts
            };

            _context.PrescriptionOCRResults.Add(ocrResult);
            await _context.SaveChangesAsync();

            // Update prescription status to Processed
            var prescription = await _context.Prescriptions.FindAsync(prescriptionId);
            if (prescription != null)
            {
                prescription.Status = PrescriptionStatus.Processed.ToString();
                prescription.ProcessedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            System.Diagnostics.Debug.WriteLine($"? OCR result stored with ID: {ocrResult.Id}");

            return ocrResult.Id;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Failed to store OCR result: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Find similar prescriptions using Levenshtein distance
    /// </summary>
    private async Task<List<SimilarPrescription>> FindSimilarPrescriptionsAsync(string ocrText, int userId)
    {
        // Get recent prescriptions for this user (last 90 days)
        var recentResults = await _context.PrescriptionOCRResults
            .Include(r => r.Prescription)
            .Where(r => r.Prescription!.UserId == userId &&
                       r.ProcessedAt >= DateTime.UtcNow.AddDays(-90))
            .OrderByDescending(r => r.ProcessedAt)
            .Take(50) // Limit to recent 50 prescriptions
            .ToListAsync();

        var similarities = new List<SimilarPrescription>();

        foreach (var result in recentResults)
        {
            var similarity = CalculateSimilarity(ocrText, result.OCRText);
            
            if (similarity >= 0.70) // Only include if 70%+ similar
            {
                similarities.Add(new SimilarPrescription
                {
                    Result = result,
                    SimilarityScore = similarity
                });
            }
        }

        return similarities;
    }

    /// <summary>
    /// Calculate similarity between two texts using Levenshtein distance
    /// Returns value between 0.0 (completely different) and 1.0 (identical)
    /// </summary>
    private double CalculateSimilarity(string text1, string text2)
    {
        if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
            return 0.0;

        // Normalize texts
        text1 = NormalizeText(text1);
        text2 = NormalizeText(text2);

        if (text1 == text2)
            return 1.0;

        // Calculate Levenshtein distance
        var distance = LevenshteinDistance(text1, text2);
        var maxLength = Math.Max(text1.Length, text2.Length);
        
        return 1.0 - ((double)distance / maxLength);
    }

    /// <summary>
    /// Calculate Levenshtein distance between two strings
    /// </summary>
    private int LevenshteinDistance(string s, string t)
    {
        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        if (n == 0) return m;
        if (m == 0) return n;

        for (int i = 0; i <= n; d[i, 0] = i++) { }
        for (int j = 0; j <= m; d[0, j] = j++) { }

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }

    /// <summary>
    /// Normalize text for comparison (remove extra whitespace, lowercase, etc.)
    /// </summary>
    private string NormalizeText(string text)
    {
        // Convert to lowercase
        text = text.ToLowerInvariant();
        
        // Remove extra whitespace
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
        
        // Trim
        text = text.Trim();
        
        return text;
    }

    /// <summary>
    /// Generate SHA256 hash of text for fast comparison
    /// </summary>
    private string GenerateHash(string text)
    {
        // Normalize text before hashing
        text = NormalizeText(text);
        
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(hashBytes);
    }

    /// <summary>
    /// Get OCR result by prescription ID
    /// </summary>
    public async Task<PrescriptionOCRResult?> GetByPrescriptionIdAsync(int prescriptionId)
    {
        return await _context.PrescriptionOCRResults
            .FirstOrDefaultAsync(r => r.PrescriptionId == prescriptionId);
    }

    /// <summary>
    /// Get statistics about OCR results
    /// </summary>
    public async Task<OCRStatistics> GetStatisticsAsync(int userId)
    {
        var results = await _context.PrescriptionOCRResults
            .Include(r => r.Prescription)
            .Where(r => r.Prescription!.UserId == userId)
            .ToListAsync();

        return new OCRStatistics
        {
            TotalProcessed = results.Count,
            OpenAISelected = results.Count(r => r.SelectedProvider == "OpenAI"),
            ClaudeSelected = results.Count(r => r.SelectedProvider == "Claude"),
            AverageProcessingTime = results.Any() 
                ? TimeSpan.FromMilliseconds(results.Average(r => r.ProcessingTime.TotalMilliseconds))
                : TimeSpan.Zero,
            TotalMedications = results.Sum(r => r.MedicationCount)
        };
    }

    /// <summary>
    /// Get medications from a duplicate prescription
    /// </summary>
    public async Task<List<Medication>> GetDuplicatePrescriptionMedicationsAsync(int prescriptionId)
    {
        return await _context.Medications
            .Where(m => m.PrescriptionId == prescriptionId)
            .ToListAsync();
    }

    /// <summary>
    /// Get full duplicate prescription details including medications
    /// </summary>
    public async Task<DuplicatePrescriptionDetails?> GetDuplicatePrescriptionDetailsAsync(int prescriptionId)
    {
        var ocrResult = await _context.PrescriptionOCRResults
            .Include(r => r.Prescription)
            .FirstOrDefaultAsync(r => r.PrescriptionId == prescriptionId);

        if (ocrResult == null)
            return null;

        var medications = await GetDuplicatePrescriptionMedicationsAsync(prescriptionId);

        return new DuplicatePrescriptionDetails
        {
            PrescriptionId = prescriptionId,
            OcrResult = ocrResult,
            Medications = medications,
            Status = Enum.TryParse<PrescriptionStatus>(ocrResult.Prescription?.Status, out var status) 
                ? status 
                : PrescriptionStatus.Processing
        };
    }
}

/// <summary>
/// Result of duplicate check
/// </summary>
public class DuplicateCheckResult
{
    public bool IsDuplicate { get; set; }
    public PrescriptionOCRResult? ExistingResult { get; set; }
    public double SimilarityScore { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Similar prescription with similarity score
/// </summary>
public class SimilarPrescription
{
    public PrescriptionOCRResult Result { get; set; } = null!;
    public double SimilarityScore { get; set; }
}

/// <summary>
/// OCR processing statistics
/// </summary>
public class OCRStatistics
{
    public int TotalProcessed { get; set; }
    public int OpenAISelected { get; set; }
    public int ClaudeSelected { get; set; }
    public TimeSpan AverageProcessingTime { get; set; }
    public int TotalMedications { get; set; }
}

/// <summary>
/// Duplicate prescription details with medications
/// </summary>
public class DuplicatePrescriptionDetails
{
    public int PrescriptionId { get; set; }
    public PrescriptionOCRResult OcrResult { get; set; } = null!;
    public List<Medication> Medications { get; set; } = new();
    public PrescriptionStatus Status { get; set; }
}
