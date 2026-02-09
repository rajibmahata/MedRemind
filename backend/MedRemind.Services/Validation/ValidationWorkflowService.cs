using MedRemind.Core.Data;
using MedRemind.Core.DTOs;
using MedRemind.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MedRemind.Services.Validation;

/// <summary>
/// Service for managing prescription validation workflow
/// Handles user review, confirmation, and correction of parsed medications
/// </summary>
public class ValidationWorkflowService
{
    private readonly IRepository<PrescriptionValidationWorkflow> _workflowRepository;
    private readonly IRepository<MedicationValidation> _validationRepository;
    private readonly IRepository<Prescription> _prescriptionRepository;
    private readonly IRepository<Medication> _medicationRepository;
    private readonly IRepository<PrescriptionOCRResult> _ocrResultRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ValidationWorkflowService(
        IRepository<PrescriptionValidationWorkflow> workflowRepository,
        IRepository<MedicationValidation> validationRepository,
        IRepository<Prescription> prescriptionRepository,
        IRepository<Medication> medicationRepository,
        IRepository<PrescriptionOCRResult> ocrResultRepository,
        IUnitOfWork unitOfWork)
    {
        _workflowRepository = workflowRepository;
        _validationRepository = validationRepository;
        _prescriptionRepository = prescriptionRepository;
        _medicationRepository = medicationRepository;
        _ocrResultRepository = ocrResultRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Create validation workflow for a prescription
    /// </summary>
    public async Task<ValidationWorkflowDto> CreateValidationWorkflowAsync(int prescriptionId, int userId)
    {
        // Get prescription with medications
        var prescription = await _prescriptionRepository
            .GetQueryable()
            .Include(p => p.Medications)
            .FirstOrDefaultAsync(p => p.Id == prescriptionId && p.UserId == userId);

        if (prescription == null)
            throw new InvalidOperationException($"Prescription {prescriptionId} not found for user {userId}");

        // Check if workflow already exists
        var existingWorkflow = await _workflowRepository
            .GetQueryable()
            .FirstOrDefaultAsync(w => w.PrescriptionId == prescriptionId);

        if (existingWorkflow != null)
        {
            // Return existing workflow
            return await GetValidationWorkflowAsync(prescriptionId, userId);
        }

        // Get OCR result for safety metrics
        var ocrResult = await _ocrResultRepository
            .GetQueryable()
            .FirstOrDefaultAsync(o => o.PrescriptionId == prescriptionId);

        // Create new workflow
        var workflow = new PrescriptionValidationWorkflow
        {
            PrescriptionId = prescriptionId,
            UserId = userId,
            Status = "Pending",
            TotalMedications = prescription.Medications.Count,
            ConfirmedMedications = 0,
            CorrectedMedications = 0,
            StartedAt = DateTime.UtcNow,
            HasHighRiskMedications = prescription.Medications.Any(m => m.SafetyScore < 0.5),
            RequiresPharmacistReview = prescription.Medications.Any(m => m.RequiresPharmacistReview),
            TotalSafetyWarnings = ocrResult?.SafetyWarningsCount ?? 0,
            DrugInteractionsDetected = ocrResult?.DrugInteractionsCount ?? 0,
            CreatedAt = DateTime.UtcNow
        };

        await _workflowRepository.AddAsync(workflow);

        // Create validation records for each medication
        foreach (var medication in prescription.Medications)
        {
            var validation = new MedicationValidation
            {
                MedicationId = medication.Id,
                UserId = userId,
                IsConfirmed = false,
                HasCorrections = false,
                CreatedAt = DateTime.UtcNow
            };
            await _validationRepository.AddAsync(validation);
        }

        await _unitOfWork.SaveChangesAsync();

        // Return workflow DTO
        return await GetValidationWorkflowAsync(prescriptionId, userId);
    }

    /// <summary>
    /// Get validation workflow status
    /// </summary>
    public async Task<ValidationWorkflowDto> GetValidationWorkflowAsync(int prescriptionId, int userId)
    {
        var workflow = await _workflowRepository
            .GetQueryable()
            .Include(w => w.Prescription)
            .FirstOrDefaultAsync(w => w.PrescriptionId == prescriptionId && w.UserId == userId);

        if (workflow == null)
        {
            // Create workflow if it doesn't exist
            return await CreateValidationWorkflowAsync(prescriptionId, userId);
        }

        var prescription = workflow.Prescription;
        var medications = await _medicationRepository
            .GetQueryable()
            .Where(m => m.PrescriptionId == prescriptionId)
            .ToListAsync();

        var validations = await _validationRepository
            .GetQueryable()
            .Where(v => medications.Select(m => m.Id).Contains(v.MedicationId))
            .ToListAsync();

        var medicationDtos = new List<MedicationValidationDto>();
        foreach (var med in medications)
        {
            var validation = validations.FirstOrDefault(v => v.MedicationId == med.Id);
            medicationDtos.Add(MapToMedicationValidationDto(med, validation));
        }

        return new ValidationWorkflowDto
        {
            PrescriptionId = prescriptionId,
            PrescriptionImagePath = prescription.ImagePath,
            DoctorName = prescription.DoctorName,
            PrescriptionDate = prescription.PrescriptionDate,
            Status = workflow.Status,
            TotalMedications = workflow.TotalMedications,
            ConfirmedMedications = workflow.ConfirmedMedications,
            CorrectedMedications = workflow.CorrectedMedications,
            IsComplete = workflow.Status == "Completed",
            HasHighRiskMedications = workflow.HasHighRiskMedications,
            RequiresPharmacistReview = workflow.RequiresPharmacistReview,
            TotalSafetyWarnings = workflow.TotalSafetyWarnings,
            DrugInteractionsDetected = workflow.DrugInteractionsDetected,
            Medications = medicationDtos,
            CreatedAt = workflow.CreatedAt
        };
    }

    /// <summary>
    /// Confirm a medication after user review
    /// </summary>
    public async Task<MedicationValidationDto> ConfirmMedicationAsync(ConfirmMedicationRequest request, int userId)
    {
        var medication = await _medicationRepository.GetByIdAsync(request.MedicationId);
        if (medication == null || medication.UserId != userId)
            throw new InvalidOperationException($"Medication {request.MedicationId} not found");

        var validation = await _validationRepository
            .GetQueryable()
            .FirstOrDefaultAsync(v => v.MedicationId == request.MedicationId);

        if (validation == null)
        {
            validation = new MedicationValidation
            {
                MedicationId = request.MedicationId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            await _validationRepository.AddAsync(validation);
        }

        validation.IsConfirmed = true;
        validation.ConfirmedAt = DateTime.UtcNow;
        validation.UserAcknowledgedWarnings = request.AcknowledgeWarnings;
        validation.RequiresPharmacistConsultation = request.RequiresPharmacistConsultation;
        validation.ValidationNotes = request.ValidationNotes;
        validation.UpdatedAt = DateTime.UtcNow;

        await _validationRepository.UpdateAsync(validation);

        // Update workflow progress
        await UpdateWorkflowProgressAsync(medication.PrescriptionId ?? 0);

        await _unitOfWork.SaveChangesAsync();

        return MapToMedicationValidationDto(medication, validation);
    }

    /// <summary>
    /// Apply user corrections to a medication
    /// </summary>
    public async Task<MedicationValidationDto> CorrectMedicationAsync(CorrectMedicationRequest request, int userId)
    {
        var medication = await _medicationRepository.GetByIdAsync(request.MedicationId);
        if (medication == null || medication.UserId != userId)
            throw new InvalidOperationException($"Medication {request.MedicationId} not found");

        var validation = await _validationRepository
            .GetQueryable()
            .FirstOrDefaultAsync(v => v.MedicationId == request.MedicationId);

        if (validation == null)
        {
            validation = new MedicationValidation
            {
                MedicationId = request.MedicationId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            await _validationRepository.AddAsync(validation);
        }

        // Store original values
        validation.OriginalName = medication.Name;
        validation.OriginalDosage = medication.Dosage;
        validation.OriginalFrequency = medication.Frequency;
        validation.OriginalInstructions = medication.Instructions;
        validation.HasCorrections = true;
        validation.CorrectionReason = request.CorrectionReason;
        validation.ValidationNotes = request.ValidationNotes;
        validation.CorrectedAt = DateTime.UtcNow;

        // Apply corrections to medication
        if (!string.IsNullOrEmpty(request.CorrectedName))
            medication.Name = request.CorrectedName;
        if (!string.IsNullOrEmpty(request.CorrectedDosage))
            medication.Dosage = request.CorrectedDosage;
        if (!string.IsNullOrEmpty(request.CorrectedUnit))
            medication.Unit = request.CorrectedUnit;
        if (!string.IsNullOrEmpty(request.CorrectedFrequency))
            medication.Frequency = request.CorrectedFrequency;
        if (request.CorrectedDurationDays.HasValue)
            medication.DurationDays = request.CorrectedDurationDays;
        if (!string.IsNullOrEmpty(request.CorrectedInstructions))
            medication.Instructions = request.CorrectedInstructions;

        medication.UpdatedAt = DateTime.UtcNow;

        // Auto-confirm after correction
        validation.IsConfirmed = true;
        validation.ConfirmedAt = DateTime.UtcNow;
        validation.UpdatedAt = DateTime.UtcNow;

        await _medicationRepository.UpdateAsync(medication);
        await _validationRepository.UpdateAsync(validation);

        // Update workflow progress
        await UpdateWorkflowProgressAsync(medication.PrescriptionId ?? 0);

        await _unitOfWork.SaveChangesAsync();

        return MapToMedicationValidationDto(medication, validation);
    }

    /// <summary>
    /// Delete a medication during validation
    /// </summary>
    public async Task<bool> DeleteMedicationAsync(DeleteMedicationRequest request, int userId)
    {
        var medication = await _medicationRepository.GetByIdAsync(request.MedicationId);
        if (medication == null || medication.UserId != userId)
            throw new InvalidOperationException($"Medication {request.MedicationId} not found");

        var prescriptionId = medication.PrescriptionId ?? 0;

        // Delete validation record
        var validation = await _validationRepository
            .GetQueryable()
            .FirstOrDefaultAsync(v => v.MedicationId == request.MedicationId);

        if (validation != null)
            await _validationRepository.DeleteAsync(validation);

        // Delete medication
        await _medicationRepository.DeleteAsync(medication);

        // Update workflow
        await UpdateWorkflowProgressAsync(prescriptionId);

        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Complete validation workflow
    /// </summary>
    public async Task<ValidationWorkflowDto> CompleteValidationAsync(CompleteValidationRequest request, int userId)
    {
        var workflow = await _workflowRepository
            .GetQueryable()
            .FirstOrDefaultAsync(w => w.PrescriptionId == request.PrescriptionId && w.UserId == userId);

        if (workflow == null)
            throw new InvalidOperationException($"Validation workflow not found for prescription {request.PrescriptionId}");

        // Check if all medications are confirmed
        var medications = await _medicationRepository
            .GetQueryable()
            .Where(m => m.PrescriptionId == request.PrescriptionId)
            .ToListAsync();

        var validations = await _validationRepository
            .GetQueryable()
            .Where(v => medications.Select(m => m.Id).Contains(v.MedicationId))
            .ToListAsync();

        var allConfirmed = validations.All(v => v.IsConfirmed);
        if (!allConfirmed)
            throw new InvalidOperationException("Cannot complete validation. Not all medications are confirmed.");

        workflow.Status = "Completed";
        workflow.CompletedAt = DateTime.UtcNow;
        workflow.TimeSpentInReview = workflow.CompletedAt - workflow.StartedAt;
        workflow.UserConsultedPharmacist = request.UserConsultedPharmacist;
        workflow.PharmacistNotes = request.PharmacistNotes;
        if (request.UserConsultedPharmacist)
            workflow.PharmacistConsultationAt = DateTime.UtcNow;
        workflow.UpdatedAt = DateTime.UtcNow;

        await _workflowRepository.UpdateAsync(workflow);
        await _unitOfWork.SaveChangesAsync();

        return await GetValidationWorkflowAsync(request.PrescriptionId, userId);
    }

    /// <summary>
    /// Update workflow progress counts
    /// </summary>
    private async Task UpdateWorkflowProgressAsync(int prescriptionId)
    {
        if (prescriptionId == 0) return;

        var workflow = await _workflowRepository
            .GetQueryable()
            .FirstOrDefaultAsync(w => w.PrescriptionId == prescriptionId);

        if (workflow == null) return;

        var medications = await _medicationRepository
            .GetQueryable()
            .Where(m => m.PrescriptionId == prescriptionId)
            .ToListAsync();

        var validations = await _validationRepository
            .GetQueryable()
            .Where(v => medications.Select(m => m.Id).Contains(v.MedicationId))
            .ToListAsync();

        workflow.TotalMedications = medications.Count;
        workflow.ConfirmedMedications = validations.Count(v => v.IsConfirmed);
        workflow.CorrectedMedications = validations.Count(v => v.HasCorrections);

        if (workflow.Status == "Pending" && workflow.ConfirmedMedications > 0)
            workflow.Status = "InProgress";

        workflow.UpdatedAt = DateTime.UtcNow;

        await _workflowRepository.UpdateAsync(workflow);
    }

    /// <summary>
    /// Map medication and validation to DTO
    /// </summary>
    private MedicationValidationDto MapToMedicationValidationDto(Medication medication, MedicationValidation? validation)
    {
        var safetyWarnings = new List<SafetyWarningDto>();

        // Add safety warnings if they exist
        if (!string.IsNullOrEmpty(medication.SafetyWarningType))
        {
            safetyWarnings.Add(new SafetyWarningDto
            {
                Type = medication.SafetyWarningType,
                Severity = medication.SafetyWarningSeverity ?? "Warning",
                Message = medication.SafetyWarningMessage ?? "Safety warning detected",
                Recommendation = medication.SafetyWarningRecommendation,
                RequiresPharmacistConsultation = medication.RequiresPharmacistReview
            });
        }

        // Add age-specific warnings
        if (!string.IsNullOrEmpty(medication.AgeSpecificWarning))
        {
            safetyWarnings.Add(new SafetyWarningDto
            {
                Type = "AgeSpecific",
                Severity = medication.AgeAppropriate == false ? "Warning" : "Info",
                Message = medication.AgeSpecificWarning,
                RequiresPharmacistConsultation = medication.AgeAppropriate == false
            });
        }

        return new MedicationValidationDto
        {
            MedicationId = medication.Id,
            Name = medication.Name,
            Dosage = medication.Dosage,
            Unit = medication.Unit,
            Frequency = medication.Frequency,
            DurationDays = medication.DurationDays,
            Instructions = medication.Instructions,
            MedicineDetails = medication.MedicineDetails,
            SideEffects = medication.SideEffects,
            IsConfirmed = validation?.IsConfirmed ?? false,
            ConfirmedAt = validation?.ConfirmedAt,
            HasCorrections = validation?.HasCorrections ?? false,
            ValidationNotes = validation?.ValidationNotes,
            SafetyScore = medication.SafetyScore,
            RequiresPharmacistReview = medication.RequiresPharmacistReview,
            SafetyWarnings = safetyWarnings,
            AgeAppropriate = medication.AgeAppropriate,
            AgeSpecificWarning = medication.AgeSpecificWarning
        };
    }
}
