using System;

namespace MedRemind.Core.Models;

public class Reminder
{
    public int Id { get; set; }
    public int MedicationId { get; set; }
    public int? VoiceRecordingId { get; set; }
    public TimeSpan ReminderTime { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? NotificationId { get; set; } // Platform-specific notification ID
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public Medication Medication { get; set; } = null!;
    public VoiceRecording? VoiceRecording { get; set; }
}
