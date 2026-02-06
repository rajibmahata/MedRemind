using System.Text.Json;
using MedRemind.Core.DTOs;
using MedRemind.Core.Enums;
using MedRemind.Core.Interfaces;
using MedRemind.Core.Models;
using Microsoft.Extensions.Logging;

namespace MedRemind.Services.Prescriptions;

/// <summary>
/// Service for managing PrescriptionOCRResult records
/// Handles creation, updates, and persistence of OCR processing results
/// </summary>
public class PrescriptionOCRResultService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PrescriptionOCRResultService>? _logger;

    public PrescriptionOCRResultService(
        IUnitOfWork unitOfWork,
        ILogger<PrescriptionOCRResultService>? logger = null)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger;
    }

    /// <summary>
    /// Save or update OCR result for a prescription
    /// </summary>
    /// <param name="prescriptionId">Prescription ID</param>
    /// <param name="ocrText">Extracted OCR text</param>
    /// <param name="parseResult">Parsed prescription data</param>
    /// <param name="processingTime">Time taken to process</param>
    /// <param name="selectedProvider">Provider used (e.g., "Python Middleware (CrewAI)")</param>
    /// <returns>The created or updated PrescriptionOCRResult</returns>
    public async Task<PrescriptionOCRResult> SaveOrUpdateOCRResultAsync(
        int prescriptionId,
        string ocrText,
        PrescriptionReadResult parseResult,
        TimeSpan processingTime,
        string selectedProvider = "Python Middleware (CrewAI)")
    {
        try
        {
            _logger?.LogInformation("?? Saving/Updating OCR result for prescription {PrescriptionId}", prescriptionId);

            var repo = _unitOfWork.Repository<PrescriptionOCRResult>();
            
            // Check if OCR result entry already exists
            var existingResult = (await repo.FindAsync(r => r.PrescriptionId == prescriptionId)).FirstOrDefault();

            if (existingResult != null)
            {
                _logger?.LogInformation("?? Updating existing OCR result - ID: {OcrResultId}", existingResult.Id);
                
                // Update existing entry with AI processing results
                UpdateOCRResult(existingResult, ocrText, parseResult, processingTime, selectedProvider);
                
                await repo.UpdateAsync(existingResult);
                await _unitOfWork.SaveChangesAsync();
                
                _logger?.LogInformation("? Updated OCR result - ID: {OcrResultId}", existingResult.Id);
                return existingResult;
            }
            else
            {
                _logger?.LogInformation("?? Creating new OCR result entry");
                
                // Create new entry
                var ocrResult = CreateOCRResult(prescriptionId, ocrText, parseResult, processingTime, selectedProvider);
                
                await repo.AddAsync(ocrResult);
                await _unitOfWork.SaveChangesAsync();
                
                _logger?.LogInformation("? Created new OCR result - ID: {OcrResultId}", ocrResult.Id);
                return ocrResult;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "? Failed to save OCR result for prescription {PrescriptionId}", prescriptionId);
            throw;
        }
    }

    /// <summary>
    /// Update existing OCR result with parsed data
    /// </summary>
    private void UpdateOCRResult(
        PrescriptionOCRResult ocrResult,
        string ocrText,
        PrescriptionReadResult parseResult,
        TimeSpan processingTime,
        string selectedProvider)
    {
        // Update status
        ocrResult.Status = OcrProcessingStatus.Processed;
        
        // Update OCR text if provided
        if (!string.IsNullOrEmpty(ocrText))
        {
            ocrResult.OCRText = ocrText;
            ocrResult.OCRTextHash = ComputeHash(ocrText);
        }
        
        // Store complete responses as JSON
        var jsonOptions = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
        
        ocrResult.SelectedResponse = JsonSerializer.Serialize(parseResult, jsonOptions);
        ocrResult.OpenAIResponse = JsonSerializer.Serialize(parseResult, jsonOptions);
        ocrResult.ClaudeResponse = parseResult.MedicineValidation != null 
            ? JsonSerializer.Serialize(parseResult.MedicineValidation, jsonOptions)
            : null;
        
        // Update provider information
        ocrResult.SelectedProvider = selectedProvider;
        ocrResult.PythonMiddlewareVersion = "1.0.0";
        ocrResult.LlmModelsUsed = "gpt-4o-mini,deepseek-chat,claude-3.5-sonnet";
        ocrResult.CrewAISummary = parseResult.CrewSummary;
        
        // Update comparison metrics
        ocrResult.ComparisonScore = parseResult.ConfidenceScore;
        ocrResult.ComparisonReason = parseResult.CrewSummary;
        
        // Update extracted summary
        ocrResult.MedicationCount = parseResult.Medications.Count;
        ocrResult.DoctorName = parseResult.Doctor?.Name;
        ocrResult.PatientName = parseResult.Patient?.Name;
        ocrResult.PrescriptionDate = parseResult.PrescriptionDate;
        
        // Update medicine validation metadata
        ocrResult.OverallSafetyScore = parseResult.MedicineValidation?.OverallSafetyScore;
        ocrResult.RequiresPharmacistReview = parseResult.MedicineValidation?.RequiresPharmacistReview;
        ocrResult.SafetyWarningsCount = parseResult.MedicineValidation?.SafetyWarnings?.Count;
        ocrResult.DrugInteractionsCount = parseResult.MedicineValidation?.DrugInteractions?.Count;
        
        // Update processing metadata
        ocrResult.ProcessedAt = DateTime.UtcNow;
        ocrResult.ProcessingTime = processingTime;
    }

    /// <summary>
    /// Create new OCR result from parsed data
    /// </summary>
    private PrescriptionOCRResult CreateOCRResult(
        int prescriptionId,
        string ocrText,
        PrescriptionReadResult parseResult,
        TimeSpan processingTime,
        string selectedProvider)
    {
        var jsonOptions = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
        
        return new PrescriptionOCRResult
        {
            PrescriptionId = prescriptionId,
            OCRText = ocrText,
            OCRTextHash = ComputeHash(ocrText),
            Status = OcrProcessingStatus.Processed,
            
            // Provider information
            SelectedProvider = selectedProvider,
            
            // Store complete responses
            SelectedResponse = JsonSerializer.Serialize(parseResult, jsonOptions),
            OpenAIResponse = JsonSerializer.Serialize(parseResult, jsonOptions),
            ClaudeResponse = parseResult.MedicineValidation != null 
                ? JsonSerializer.Serialize(parseResult.MedicineValidation, jsonOptions)
                : null,
            
            // Python Middleware Metadata
            PythonMiddlewareVersion = "1.0.0",
            LlmModelsUsed = "gpt-4o-mini,deepseek-chat,claude-3.5-sonnet",
            CrewAISummary = parseResult.CrewSummary,
            
            // Comparison metrics
            ComparisonScore = parseResult.ConfidenceScore,
            ComparisonReason = parseResult.CrewSummary,
            
            // Extracted summary
            MedicationCount = parseResult.Medications.Count,
            DoctorName = parseResult.Doctor?.Name,
            PatientName = parseResult.Patient?.Name,
            PrescriptionDate = parseResult.PrescriptionDate,
            
            // Medicine validation metadata
            OverallSafetyScore = parseResult.MedicineValidation?.OverallSafetyScore,
            RequiresPharmacistReview = parseResult.MedicineValidation?.RequiresPharmacistReview,
            SafetyWarningsCount = parseResult.MedicineValidation?.SafetyWarnings?.Count,
            DrugInteractionsCount = parseResult.MedicineValidation?.DrugInteractions?.Count,
            
            // Processing metadata
            ProcessedAt = DateTime.UtcNow,
            ProcessingTime = processingTime,
            ProcessingAttempts = 1
        };
    }

    /// <summary>
    /// Get OCR result by prescription ID
    /// </summary>
    public async Task<PrescriptionOCRResult?> GetByPrescriptionIdAsync(int prescriptionId)
    {
        var repo = _unitOfWork.Repository<PrescriptionOCRResult>();
        return (await repo.FindAsync(r => r.PrescriptionId == prescriptionId)).FirstOrDefault();
    }

    /// <summary>
    /// Compute SHA256 hash of text
    /// </summary>
    private string ComputeHash(string text)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
