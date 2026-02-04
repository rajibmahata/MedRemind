using System;
using System.Collections.Generic;
using System.Text;

namespace MedRemind.Core.DTOs
{
    public class PythonPrescriptionResponse
    {
        public bool Success { get; set; }
        public string PrescriptionId { get; set; } = string.Empty;
        public PythonPatient? Patient { get; set; }
        public PythonDoctor? Doctor { get; set; }
        public string? PrescriptionDate { get; set; }
        public List<PythonMedication> Medications { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public double ProcessingTime { get; set; }
        public string? CrewSummary { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class PythonPatient
    {
        public string? Name { get; set; }
        public int? Age { get; set; }
        public string? Gender { get; set; }
    }

    public class PythonDoctor
    {
        public string? Name { get; set; }
        public string? Specialization { get; set; }
        public string? RegistrationNumber { get; set; }
    }

    public class PythonMedication
    {
        public string Name { get; set; } = string.Empty;
        public string? Dosage { get; set; }
        public string? Unit { get; set; }
        public string? Frequency { get; set; }
        public int? FrequencyCount { get; set; }
        public string? Duration { get; set; }
        public int? DurationDays { get; set; }
        public string? Timing { get; set; }
        public string? Instructions { get; set; }
        public string? Purpose { get; set; } // Medicine details/indication
        public List<string>? SideEffects { get; set; } // List of side effects
        public bool? AgeAppropriate { get; set; } // Whether dosage is appropriate for patient age
        public string? AgeSpecificWarning { get; set; } // Age-specific warnings or precautions
        public double ConfidenceScore { get; set; }
    }
}
