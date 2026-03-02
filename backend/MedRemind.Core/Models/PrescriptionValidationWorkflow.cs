using System;
using System.Collections.Generic;

namespace MedRemind.Core.Models;

/// <summary>
/// Tracks the overall validation workflow for a prescription
/// Ensures all medications are reviewed before reminders can be set
/// </summary>
public class PrescriptionValidationWorkflow
{
    public int Id { get; set; }
    public int PrescriptionId { get; set; }
    public int UserId { get; set; }

    // Workflow State
    public string Status { get; set; } = "Pending"; // Pending, InProgress, Completed, Cancelled
    public int TotalMedications { get; set; }
    public int ConfirmedMedications { get; set; }
    public int CorrectedMedications { get; set; }

    // Validation Tracking
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? TimeSpentInReview { get; set; }

    // Safety Flags
    public bool HasHighRiskMedications { get; set; } = false;
    public bool RequiresPharmacistReview { get; set; } = false;
    public int TotalSafetyWarnings { get; set; }
    public int DrugInteractionsDetected { get; set; }

    // User Actions
    public bool UserReadSafetyWarnings { get; set; } = false;
    public DateTime? SafetyWarningsReadAt { get; set; }
    public bool UserConsultedPharmacist { get; set; } = false;
    public DateTime? PharmacistConsultationAt { get; set; }
    public string? PharmacistNotes { get; set; }

    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Prescription Prescription { get; set; } = null!;
    public User User { get; set; } = null!;
}
