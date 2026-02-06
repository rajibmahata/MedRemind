using MedRemind.Core.DTOs;
using MedRemind.Core.Interfaces;
using MedRemind.Core.Models;
using Microsoft.Extensions.Logging;

namespace MedRemind.Services.Prescriptions;

/// <summary>
/// Service for persisting medications extracted from prescription parsing
/// Handles saving medication data from AI/OCR processing results
/// </summary>
public class MedicationPersistenceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MedicationPersistenceService>? _logger;

    public MedicationPersistenceService(
        IUnitOfWork unitOfWork,
        ILogger<MedicationPersistenceService>? logger = null)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger;
    }

    /// <summary>
    /// Save medications from prescription parsing result
    /// </summary>
    /// <param name="prescriptionId">Prescription ID</param>
    /// <param name="userId">User ID who owns the prescription</param>
    /// <param name="parseResult">Parsed prescription data containing medications</param>
    /// <returns>List of saved medications</returns>
    public async Task<List<Medication>> SaveMedicationsFromParseResultAsync(
        int prescriptionId,
        int userId,
        PrescriptionReadResult parseResult)
    {
        try
        {
            if (parseResult?.Medications == null || !parseResult.Medications.Any())
            {
                _logger?.LogWarning("?? No medications to save for prescription {PrescriptionId}", prescriptionId);
                return new List<Medication>();
            }

            _logger?.LogInformation("?? Saving {MedicationCount} medications for prescription {PrescriptionId}", 
                parseResult.Medications.Count, prescriptionId);

            var savedMedications = new List<Medication>();
            var medicationRepo = _unitOfWork.Repository<Medication>();

            foreach (var medData in parseResult.Medications)
            {
                var medication = CreateMedicationFromData(medData, prescriptionId, userId, parseResult);
                
                await medicationRepo.AddAsync(medication);
                savedMedications.Add(medication);
                
                _logger?.LogInformation("  ? {MedicationName} - {Dosage} {Unit}", 
                    medication.Name, medication.Dosage, medication.Unit);
            }

            await _unitOfWork.SaveChangesAsync();
            
            _logger?.LogInformation("? Saved {Count} medications successfully", savedMedications.Count);
            
            return savedMedications;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "? Failed to save medications for prescription {PrescriptionId}", prescriptionId);
            throw;
        }
    }

    /// <summary>
    /// Update existing medications or create new ones for a prescription
    /// </summary>
    /// <param name="prescriptionId">Prescription ID</param>
    /// <param name="userId">User ID</param>
    /// <param name="parseResult">Parsed prescription data</param>
    /// <param name="deleteExisting">If true, deletes existing medications before saving new ones</param>
    /// <returns>List of saved medications</returns>
    public async Task<List<Medication>> UpdateMedicationsForPrescriptionAsync(
        int prescriptionId,
        int userId,
        PrescriptionReadResult parseResult,
        bool deleteExisting = false)
    {
        try
        {
            var medicationRepo = _unitOfWork.Repository<Medication>();

            if (deleteExisting)
            {
                _logger?.LogInformation("??? Deleting existing medications for prescription {PrescriptionId}", prescriptionId);
                
                var existingMedications = await medicationRepo.FindAsync(m => m.PrescriptionId == prescriptionId);
                foreach (var med in existingMedications)
                {
                    await medicationRepo.DeleteAsync(med);
                }
                
                await _unitOfWork.SaveChangesAsync();
                _logger?.LogInformation("? Deleted {Count} existing medications", existingMedications.Count());
            }

            return await SaveMedicationsFromParseResultAsync(prescriptionId, userId, parseResult);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "? Failed to update medications for prescription {PrescriptionId}", prescriptionId);
            throw;
        }
    }

    /// <summary>
    /// Create Medication entity from MedicationData DTO
    /// </summary>
    private Medication CreateMedicationFromData(
        MedicationData medData,
        int prescriptionId,
        int userId,
        PrescriptionReadResult parseResult)
    {
        return new Medication
        {
            UserId = userId,
            PrescriptionId = prescriptionId,
            Name = medData.Name ?? string.Empty,
            Dosage = medData.Dosage ?? string.Empty,
            Unit = medData.Unit ?? string.Empty,
            Frequency = medData.Frequency ?? string.Empty,
            FrequencyCount = medData.FrequencyCount,
            DurationDays = medData.DurationDays,
            Instructions = medData.Instructions,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            StartDate = DateTime.UtcNow,
            EndDate = medData.DurationDays.HasValue ? DateTime.UtcNow.AddDays(medData.DurationDays.Value) : DateTime.UtcNow.AddDays(30),
            
            // Medicine information from Python middleware
            MedicineDetails = medData.MedicineDetails,
            SideEffects = medData.SideEffects,
            
            // Age-related safety fields (from medication-level data)
            AgeAppropriate = medData.AgeAppropriate,
            AgeSpecificWarning = medData.AgeSpecificWarning,
            
            // Safety validation fields (from medication-level data)
            SafetyWarningType = medData.SafetyWarningType,
            SafetyWarningSeverity = medData.SafetyWarningSeverity,
            SafetyWarningMessage = medData.SafetyWarningMessage,
            SafetyWarningRecommendation = medData.SafetyWarningRecommendation,
            SafetyScore = medData.SafetyScore,
            RequiresPharmacistReview = medData.RequiresPharmacistReview
        };
    }

    /// <summary>
    /// Get medications by prescription ID
    /// </summary>
    public async Task<List<Medication>> GetMedicationsByPrescriptionIdAsync(int prescriptionId)
    {
        var repo = _unitOfWork.Repository<Medication>();
        var medications = await repo.FindAsync(m => m.PrescriptionId == prescriptionId);
        return medications.ToList();
    }

    /// <summary>
    /// Delete medications by prescription ID
    /// </summary>
    public async Task<int> DeleteMedicationsByPrescriptionIdAsync(int prescriptionId)
    {
        try
        {
            var repo = _unitOfWork.Repository<Medication>();
            var medications = await repo.FindAsync(m => m.PrescriptionId == prescriptionId);
            
            int count = 0;
            foreach (var med in medications)
            {
                await repo.DeleteAsync(med);
                count++;
            }
            
            await _unitOfWork.SaveChangesAsync();
            
            _logger?.LogInformation("??? Deleted {Count} medications for prescription {PrescriptionId}", 
                count, prescriptionId);
            
            return count;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "? Failed to delete medications for prescription {PrescriptionId}", prescriptionId);
            throw;
        }
    }

    /// <summary>
    /// Get medication count by prescription ID
    /// </summary>
    public async Task<int> GetMedicationCountAsync(int prescriptionId)
    {
        var repo = _unitOfWork.Repository<Medication>();
        var medications = await repo.FindAsync(m => m.PrescriptionId == prescriptionId);
        return medications.Count();
    }
}
