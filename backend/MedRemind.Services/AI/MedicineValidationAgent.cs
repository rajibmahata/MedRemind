using MedRemind.Core.DTOs;
using MedRemind.Core.Interfaces;

namespace MedRemind.Services.AI;

public class MedicineValidationAgent : IValidationAgentService
{
    private readonly HashSet<string> _commonMedicines;
    private readonly Dictionary<string, string[]> _knownInteractions;

    public MedicineValidationAgent()
    {
        _commonMedicines = LoadCommonMedicines();
        _knownInteractions = LoadKnownInteractions();
    }

    public async Task<List<ValidationWarning>> ValidateMedicationsAsync(
        List<MedicationData> medications, 
        CancellationToken cancellationToken = default)
    {
        var warnings = new List<ValidationWarning>();

        foreach (var medication in medications)
        {
            // Validate medication name
            if (!IsValidMedicineName(medication.Name))
            {
                warnings.Add(new ValidationWarning
                {
                    MedicationName = medication.Name,
                    WarningType = "InvalidName",
                    Message = $"Medicine name '{medication.Name}' may be incorrect or misspelled",
                    Severity = "High"
                });
            }

            // Validate dosage
            if (IsUnusualDosage(medication))
            {
                warnings.Add(new ValidationWarning
                {
                    MedicationName = medication.Name,
                    WarningType = "UnusualDosage",
                    Message = $"Dosage {medication.Dosage} {medication.Unit} may be unusually high",
                    Severity = "Medium"
                });
            }

            // Check for drug interactions
            var interactions = CheckDrugInteractions(medication, medications);
            warnings.AddRange(interactions);
        }

        return await Task.FromResult(warnings);
    }

    public double CalculateConfidenceScore(MedicationData medication)
    {
        double score = 1.0;

        // Reduce confidence if name not recognized
        if (!IsValidMedicineName(medication.Name))
        {
            score -= 0.3;
        }

        // Reduce confidence if dosage seems unusual
        if (IsUnusualDosage(medication))
        {
            score -= 0.2;
        }

        // Reduce confidence if frequency is unusual
        if (medication.FrequencyCount > 4 || medication.FrequencyCount < 1)
        {
            score -= 0.1;
        }

        // Reduce confidence if duration is very long
        if (medication.DurationDays > 90)
        {
            score -= 0.1;
        }

        return Math.Max(0.0, score);
    }

    private bool IsValidMedicineName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        // Check if name is in common medicines list (case-insensitive)
        return _commonMedicines.Contains(name.ToLowerInvariant());
    }

    private bool IsUnusualDosage(MedicationData medication)
    {
        // Parse dosage and check if it's unusually high
        if (double.TryParse(medication.Dosage, out var dosageAmount))
        {
            return medication.Unit.ToLowerInvariant() switch
            {
                "tablet" or "capsule" => dosageAmount > 4,
                "ml" => dosageAmount > 30,
                "mg" => dosageAmount > 1000,
                "drops" => dosageAmount > 10,
                "puffs" => dosageAmount > 4,
                _ => false
            };
        }

        return false;
    }

    private List<ValidationWarning> CheckDrugInteractions(
        MedicationData medication, 
        List<MedicationData> allMedications)
    {
        var warnings = new List<ValidationWarning>();
        var medName = medication.Name.ToLowerInvariant();

        if (!_knownInteractions.ContainsKey(medName))
            return warnings;

        var interactsWith = _knownInteractions[medName];

        foreach (var otherMed in allMedications)
        {
            if (otherMed.Name == medication.Name)
                continue;

            if (interactsWith.Any(i => otherMed.Name.ToLowerInvariant().Contains(i)))
            {
                warnings.Add(new ValidationWarning
                {
                    MedicationName = medication.Name,
                    WarningType = "DrugInteraction",
                    Message = $"Possible interaction between {medication.Name} and {otherMed.Name}",
                    Severity = "High"
                });
            }
        }

        return warnings;
    }

    private HashSet<string> LoadCommonMedicines()
    {
        // Common medicines database (expanded list)
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Pain relievers
            "paracetamol", "ibuprofen", "aspirin", "acetaminophen", "naproxen",
            "diclofenac", "tramadol", "codeine",
            
            // Antibiotics
            "amoxicillin", "azithromycin", "ciprofloxacin", "doxycycline",
            "cephalexin", "metronidazole", "clarithromycin", "penicillin",
            
            // Antihypertensives
            "amlodipine", "lisinopril", "losartan", "metoprolol", "atenolol",
            "ramipril", "enalapril", "telmisartan",
            
            // Diabetes medications
            "metformin", "glimepiride", "insulin", "sitagliptin", "gliclazide",
            
            // Antihistamines
            "cetirizine", "loratadine", "fexofenadine", "diphenhydramine",
            
            // Proton pump inhibitors
            "omeprazole", "pantoprazole", "esomeprazole", "lansoprazole",
            
            // Bronchodilators
            "salbutamol", "albuterol", "ipratropium", "montelukast",
            
            // Vitamins and supplements
            "vitamin d", "vitamin b12", "vitamin c", "calcium", "iron",
            "folic acid", "multivitamin",
            
            // Antidepressants
            "sertraline", "fluoxetine", "escitalopram", "duloxetine",
            
            // Others
            "levothyroxine", "atorvastatin", "simvastatin", "warfarin",
            "clopidogrel", "prednisone", "prednisolone"
        };
    }

    private Dictionary<string, string[]> LoadKnownInteractions()
    {
        // Known drug interactions (simplified)
        return new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "warfarin", new[] { "aspirin", "ibuprofen", "naproxen" } },
            { "aspirin", new[] { "warfarin", "ibuprofen", "naproxen" } },
            { "metformin", new[] { "insulin" } },
            { "amlodipine", new[] { "simvastatin", "atorvastatin" } },
            { "ciprofloxacin", new[] { "warfarin", "metformin" } }
        };
    }
}
