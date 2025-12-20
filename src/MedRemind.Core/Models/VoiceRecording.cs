using System;

namespace MedRemind.Core.Models;

public class VoiceRecording
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty; // Mom, Dad, Self, Custom
    public string FilePath { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
}
