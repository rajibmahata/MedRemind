namespace MedRemind.Core.DTOs;

public class PrescriptionReadResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public double ConfidenceScore { get; set; }
    public List<MedicationData> Medications { get; set; } = new();
    public List<ValidationWarning> Warnings { get; set; } = new();
    public string? DoctorName { get; set; }
    public DateTime? PrescriptionDate { get; set; }
}

public class MedicationData
{
    public string Name { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public int FrequencyCount { get; set; }
    public int DurationDays { get; set; }
    public string? Instructions { get; set; }
    public double ConfidenceScore { get; set; }
}

public class ValidationWarning
{
    public string MedicationName { get; set; } = string.Empty;
    public string WarningType { get; set; } = string.Empty; // InvalidName, UnusualDosage, DrugInteraction
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "Medium"; // Low, Medium, High
}
