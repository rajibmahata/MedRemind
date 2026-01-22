using MedRemind.Core.DTOs;

namespace MedRemind.Services.AI;

/// <summary>
/// Service to merge prescription parsing results from multiple AI providers
/// Combines data from DeepSeek, OpenAI, and Claude to create most complete result
/// </summary>
public class PrescriptionResultMergerService
{
    public PrescriptionResultMergerService()
    {
        System.Diagnostics.Debug.WriteLine("? Result Merger Service: Initialized");
    }

    /// <summary>
    /// Merge two prescription parsing results, preferring non-null/more complete data
    /// </summary>
    public PrescriptionReadResult MergeResults(
        PrescriptionReadResult primary,
        PrescriptionReadResult secondary,
        string primaryProvider,
        string secondaryProvider)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"\n?? Merger: Merging {primaryProvider} + {secondaryProvider}");
            System.Diagnostics.Debug.WriteLine($"   Primary medications: {primary.Medications.Count}");
            System.Diagnostics.Debug.WriteLine($"   Secondary medications: {secondary.Medications.Count}");

            var merged = new PrescriptionReadResult
            {
                Success = primary.Success || secondary.Success,
                Patient = MergePatientData(primary.Patient, secondary.Patient),
                Doctor = MergeDoctorData(primary.Doctor, secondary.Doctor),
                PrescriptionDate = primary.PrescriptionDate ?? secondary.PrescriptionDate,
                Medications = MergeMedications(primary.Medications, secondary.Medications)
            };

            System.Diagnostics.Debug.WriteLine($"? Merger: Complete");
            System.Diagnostics.Debug.WriteLine($"   Final medications: {merged.Medications.Count}");
            System.Diagnostics.Debug.WriteLine($"   Patient: {merged.Patient?.Name ?? "N/A"}");
            System.Diagnostics.Debug.WriteLine($"   Doctor: {merged.Doctor?.Name ?? "N/A"}");

            return merged;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Merger: Error: {ex.Message}");
            return primary; // Return primary if merge fails
        }
    }

    private PatientData? MergePatientData(PatientData? primary, PatientData? secondary)
    {
        if (primary == null && secondary == null) return null;
        if (primary == null) return secondary;
        if (secondary == null) return primary;

        return new PatientData
        {
            Name = ChooseBetter(primary.Name, secondary.Name),
            Age = primary.Age ?? secondary.Age,
            Gender = ChooseBetter(primary.Gender, secondary.Gender)
        };
    }

    private DoctorData? MergeDoctorData(DoctorData? primary, DoctorData? secondary)
    {
        if (primary == null && secondary == null) return null;
        if (primary == null) return secondary;
        if (secondary == null) return primary;

        return new DoctorData
        {
            Name = ChooseBetter(primary.Name, secondary.Name),
            RegistrationNumber = ChooseBetter(primary.RegistrationNumber, secondary.RegistrationNumber),
            Specialization = ChooseBetter(primary.Specialization, secondary.Specialization)
        };
    }

    private List<MedicationData> MergeMedications(
        List<MedicationData> primary,
        List<MedicationData> secondary)
    {
        var merged = new List<MedicationData>();
        var processedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Add all primary medications first
        foreach (var med in primary)
        {
            var normalizedName = NormalizeMedicationName(med.Name);
            
            // Find matching medication in secondary
            var secondaryMatch = secondary.FirstOrDefault(s =>
                NormalizeMedicationName(s.Name).Equals(normalizedName, StringComparison.OrdinalIgnoreCase));

            if (secondaryMatch != null)
            {
                // Merge the two
                merged.Add(MergeMedicationData(med, secondaryMatch));
                System.Diagnostics.Debug.WriteLine($"   Merged: {med.Name}");
            }
            else
            {
                // Use primary only
                merged.Add(med);
                System.Diagnostics.Debug.WriteLine($"   From primary: {med.Name}");
            }

            processedNames.Add(normalizedName);
        }

        // Add medications only in secondary
        foreach (var med in secondary)
        {
            var normalizedName = NormalizeMedicationName(med.Name);
            if (!processedNames.Contains(normalizedName))
            {
                merged.Add(med);
                processedNames.Add(normalizedName);
                System.Diagnostics.Debug.WriteLine($"   From secondary: {med.Name}");
            }
        }

        return merged;
    }

    private MedicationData MergeMedicationData(MedicationData primary, MedicationData secondary)
    {
        return new MedicationData
        {
            Name = ChooseBetter(primary.Name, secondary.Name),
            Dosage = ChooseBetter(primary.Dosage, secondary.Dosage, "0"),
            Unit = ChooseBetter(primary.Unit, secondary.Unit, "tablet"),
            Frequency = ChooseBetter(primary.Frequency, secondary.Frequency, "Once daily"),
            FrequencyCount = primary.FrequencyCount > 0 ? primary.FrequencyCount : secondary.FrequencyCount,
            DurationDays = primary.DurationDays > 0 ? primary.DurationDays : secondary.DurationDays,
            Instructions = ChooseBetter(primary.Instructions, secondary.Instructions),
            ConfidenceScore = Math.MaxMagnitude((double)primary?.ConfidenceScore, (double)secondary?.ConfidenceScore)
        };
    }

    private string? ChooseBetter(string? primary, string? secondary, string? defaultValue = null)
    {
        // Prefer non-null and non-default values
        if (!string.IsNullOrWhiteSpace(primary) && primary != defaultValue)
            return primary;
        if (!string.IsNullOrWhiteSpace(secondary) && secondary != defaultValue)
            return secondary;
        return primary ?? secondary ?? defaultValue;
    }

    private string NormalizeMedicationName(string name)
    {
        // Normalize for comparison
        return name.ToLowerInvariant()
            .Replace("-", "")
            .Replace(" ", "")
            .Trim();
    }
}

/// <summary>
/// Service to validate if prescription parsing result is complete
/// </summary>
public class PrescriptionValidationService
{
    public PrescriptionValidationService()
    {
        System.Diagnostics.Debug.WriteLine("? Validation Service: Initialized");
    }

    /// <summary>
    /// Check if prescription result is complete enough to skip additional AI calls
    /// </summary>
    public ValidationQuality ValidateCompleteness(PrescriptionReadResult result)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("\n?? Validation: Checking completeness...");

            var quality = new ValidationQuality();

            // Check patient data
            quality.HasPatient = result.Patient != null && !string.IsNullOrWhiteSpace(result.Patient.Name);

            // Check doctor data
            quality.HasDoctor = result.Doctor != null && !string.IsNullOrWhiteSpace(result.Doctor.Name);

            // Check prescription date
            quality.HasPrescriptionDate = result.PrescriptionDate.HasValue;

            // Check medications
            quality.HasMedications = result.Medications.Any();
            quality.MedicationCount = result.Medications.Count;

            if (quality.HasMedications)
            {
                // Check if medications have required details
                var completeMedications = result.Medications.Where(m =>
                    !string.IsNullOrWhiteSpace(m.Name) &&
                    !string.IsNullOrWhiteSpace(m.Dosage) &&
                    !string.IsNullOrWhiteSpace(m.Frequency) &&
                    m.FrequencyCount > 0
                ).ToList();

                quality.CompleteMedicationCount = completeMedications.Count;
                quality.MedicationsHaveDetails = completeMedications.Count == result.Medications.Count;

                // Check confidence
                quality.AverageConfidence = result.Medications.Average(m =>(double) m.ConfidenceScore);
            }

            // Calculate overall completeness
            quality.IsComplete = quality.HasPatient &&
                                quality.HasDoctor &&
                                quality.HasPrescriptionDate &&
                                quality.HasMedications &&
                                quality.MedicationsHaveDetails &&
                                quality.AverageConfidence >= 0.7;

            System.Diagnostics.Debug.WriteLine($"   Patient: {(quality.HasPatient ? "?" : "?")}");
            System.Diagnostics.Debug.WriteLine($"   Doctor: {(quality.HasDoctor ? "?" : "?")}");
            System.Diagnostics.Debug.WriteLine($"   Date: {(quality.HasPrescriptionDate ? "?" : "?")}");
            System.Diagnostics.Debug.WriteLine($"   Medications: {quality.MedicationCount} ({quality.CompleteMedicationCount} complete)");
            System.Diagnostics.Debug.WriteLine($"   Confidence: {quality.AverageConfidence:P0}");
            System.Diagnostics.Debug.WriteLine($"   Overall: {(quality.IsComplete ? "? COMPLETE" : "?? INCOMPLETE")}");

            return quality;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Validation error: {ex.Message}");
            return new ValidationQuality { IsComplete = false };
        }
    }
}

/// <summary>
/// Validation quality result
/// </summary>
public class ValidationQuality
{
    public bool IsComplete { get; set; }
    public bool HasPatient { get; set; }
    public bool HasDoctor { get; set; }
    public bool HasPrescriptionDate { get; set; }
    public bool HasMedications { get; set; }
    public int MedicationCount { get; set; }
    public int CompleteMedicationCount { get; set; }
    public bool MedicationsHaveDetails { get; set; }
    public double AverageConfidence { get; set; }

    public string GetMissingItems()
    {
        var missing = new List<string>();
        if (!HasPatient) missing.Add("Patient information");
        if (!HasDoctor) missing.Add("Doctor information");
        if (!HasPrescriptionDate) missing.Add("Prescription date");
        if (!HasMedications) missing.Add("Medications");
        else if (!MedicationsHaveDetails) missing.Add("Complete medication details (dose/frequency)");
        
        return missing.Any() ? string.Join(", ", missing) : "None";
    }
}
