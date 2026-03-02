namespace MedRemind.Web.Models;

public class ValidationWorkflowResponse
{
    public int PrescriptionId { get; set; }
    public string PrescriptionImagePath { get; set; } = string.Empty;
    public string? DoctorName { get; set; }
    public DateTime? PrescriptionDate { get; set; }
    public string Status { get; set; } = "Pending";
    public int TotalMedications { get; set; }
    public int ConfirmedMedications { get; set; }
    public int CorrectedMedications { get; set; }
    public bool IsComplete { get; set; }
    public bool HasHighRiskMedications { get; set; }
    public bool RequiresPharmacistReview { get; set; }
    public int TotalSafetyWarnings { get; set; }
    public int DrugInteractionsDetected { get; set; }
    public List<MedicationValidationResponse> Medications { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class MedicationValidationResponse
{
    public int MedicationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public int? DurationDays { get; set; }
    public string? Instructions { get; set; }
    public string? MedicineDetails { get; set; }
    public string? SideEffects { get; set; }
    public bool IsConfirmed { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public bool HasCorrections { get; set; }
    public string? ValidationNotes { get; set; }
    public double? SafetyScore { get; set; }
    public bool RequiresPharmacistReview { get; set; }
    public List<SafetyWarningResponse> SafetyWarnings { get; set; } = new();
    public bool? AgeAppropriate { get; set; }
    public string? AgeSpecificWarning { get; set; }
}

public class SafetyWarningResponse
{
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Recommendation { get; set; }
    public bool RequiresPharmacistConsultation { get; set; }
}

public class CorrectMedicationRequest
{
    public int MedicationId { get; set; }
    public string? CorrectedName { get; set; }
    public string? CorrectedDosage { get; set; }
    public string? CorrectedUnit { get; set; }
    public string? CorrectedFrequency { get; set; }
    public int? CorrectedDurationDays { get; set; }
    public string? CorrectedInstructions { get; set; }
    public string CorrectionReason { get; set; } = string.Empty;
    public string? ValidationNotes { get; set; }
}
