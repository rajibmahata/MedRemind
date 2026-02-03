namespace MedRemind.Core.DTOs;

public class PrescriptionReadResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? PrescriptionId { get; set; }
    public double ConfidenceScore { get; set; }
    public PatientData? Patient { get; set; }
    public DoctorData? Doctor { get; set; }
    public DateTime? PrescriptionDate { get; set; }
    public List<MedicationData> Medications { get; set; } = new();

    // Legacy validation warnings (kept for backward compatibility)
    public List<ValidationWarning> ValidationWarnings { get; set; } = new();
    
    // Medicine validation from Python middleware
    public MedicineValidationData? MedicineValidation { get; set; }
    
    // Simple warning messages (from Python)
    public List<string> Warnings { get; set; } = new();
    
    // Processing metadata
    public double? ProcessingTime { get; set; }
    public string? CrewSummary { get; set; }
}

/// <summary>
/// Patient information
/// </summary>
public class PatientData
{
    public string? Name { get; set; }
    public int? Age { get; set; }
    public string? Gender { get; set; }
}

/// <summary>
/// Doctor information
/// </summary>
public class DoctorData
{
    public string? Name { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? Specialization { get; set; }
}

public class MedicationData
{
    public string? Name { get; set; }
    public string? Dosage { get; set; }
    public string? Unit { get; set; }
    public string? Frequency { get; set; }
    public int? FrequencyCount { get; set; }  // ✅ Made nullable
    public string? Duration { get; set; }
    public int? DurationDays { get; set; }    // ✅ Made nullable
    public string? Timing { get; set; }
    public string? Instructions { get; set; }
    public double? ConfidenceScore { get; set; }  // ✅ Made nullable for consistency
}

/// <summary>
/// Medicine validation data from Python middleware (CrewAI safety validation)
/// </summary>
public class MedicineValidationData
{
    public List<DrugInteraction> DrugInteractions { get; set; } = new();
    public List<SafetyWarning> SafetyWarnings { get; set; } = new();
    public List<string> DuplicateTherapies { get; set; } = new();
    public double OverallSafetyScore { get; set; }
    public bool RequiresPharmacistReview { get; set; }
}

/// <summary>
/// Drug interaction warning
/// </summary>
public class DrugInteraction
{
    public List<string> Medicines { get; set; } = new();
    public string Severity { get; set; } = string.Empty; // low, moderate, high, major, severe
    public string Description { get; set; } = string.Empty;
    public string? Recommendation { get; set; }
}

/// <summary>
/// Safety warning for medication
/// </summary>
public class SafetyWarning
{
    public string Medicine { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // age, dose, interaction, contraindication
    public string Severity { get; set; } = string.Empty; // low, medium, high
    public string Message { get; set; } = string.Empty;
    public string? Recommendation { get; set; }
}

/// <summary>
/// Legacy validation warning (kept for backward compatibility)
/// </summary>
public class ValidationWarning
{
    public string MedicationName { get; set; } = string.Empty;
    public string WarningType { get; set; } = string.Empty; // InvalidName, UnusualDosage, DrugInteraction
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "Medium"; // Low, Medium, High
}

