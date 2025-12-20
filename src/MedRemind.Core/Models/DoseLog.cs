using System;

namespace MedRemind.Core.Models;

public class DoseLog
{
    public int Id { get; set; }
    public int MedicationId { get; set; }
    public DateTime ScheduledTime { get; set; }
    public DateTime? TakenTime { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Taken, Missed, Snoozed
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Medication Medication { get; set; } = null!;
}
