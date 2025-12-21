using System;

namespace MedRemind.Core.Models;

public class Medication
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? PrescriptionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty; // Tablet, Capsule, ml, mg, drops, puffs
    public string Frequency { get; set; } = string.Empty; // Once daily, Twice daily, etc.
    public int FrequencyCount { get; set; } // Number of times per day
    public int DurationDays { get; set; }
    public string? Instructions { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public Prescription? Prescription { get; set; }
    public ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
    public ICollection<DoseLog> DoseLogs { get; set; } = new List<DoseLog>();
}
