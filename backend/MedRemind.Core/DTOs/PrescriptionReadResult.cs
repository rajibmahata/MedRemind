namespace MedRemind.Core.DTOs;

public class PrescriptionReadResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public double ConfidenceScore { get; set; }
    public PatientData? Patient { get; set; }
    public DoctorData? Doctor { get; set; }
    public DateTime? PrescriptionDate { get; set; }
    public List<MedicationData> Medications { get; set; } = new();

    public List<ValidationWarning> ValidationWarnings { get; set; } = new();
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

public class ValidationWarning
{
    public string MedicationName { get; set; } = string.Empty;
    public string WarningType { get; set; } = string.Empty; // InvalidName, UnusualDosage, DrugInteraction
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "Medium"; // Low, Medium, High
}

