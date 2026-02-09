using System;

namespace MedRemind.Core.Models;

/// <summary>
/// Tracks user validation and confirmation of parsed medications
/// Ensures users review and approve each medication before setting reminders
/// </summary>
public class MedicationValidation
{
    public int Id { get; set; }
    public int MedicationId { get; set; }
    public int UserId { get; set; }

    // Validation State
    public bool IsConfirmed { get; set; } = false;
    public DateTime? ConfirmedAt { get; set; }

    // User Corrections
    public bool HasCorrections { get; set; } = false;
    public string? OriginalName { get; set; }
    public string? OriginalDosage { get; set; }
    public string? OriginalFrequency { get; set; }
    public string? OriginalInstructions { get; set; }
    public string? CorrectionReason { get; set; } // Why user made corrections
    public DateTime? CorrectedAt { get; set; }

    // User Notes
    public string? ValidationNotes { get; set; } // User's personal notes about this medication
    public bool RequiresPharmacistConsultation { get; set; } = false;
    public bool UserAcknowledgedWarnings { get; set; } = false; // User saw and acknowledged safety warnings

    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Medication Medication { get; set; } = null!;
    public User User { get; set; } = null!;
}
