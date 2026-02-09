using MedRemind.Web.Models;

namespace MedRemind.Web.Services;

public interface IValidationService
{
    Task<ValidationWorkflowResponse?> GetValidationWorkflowAsync(int prescriptionId);
    Task<MedicationValidationResponse?> ConfirmMedicationAsync(int medicationId, bool acknowledgeWarnings, string? notes = null, bool requiresPharmacist = false);
    Task<MedicationValidationResponse?> CorrectMedicationAsync(CorrectMedicationRequest request);
    Task<bool> DeleteMedicationAsync(int medicationId, string reason);
    Task<ValidationWorkflowResponse?> CompleteValidationAsync(int prescriptionId, bool consultedPharmacist = false, string? pharmacistNotes = null);
}
