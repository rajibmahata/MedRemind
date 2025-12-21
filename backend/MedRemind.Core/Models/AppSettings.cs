using System;

namespace MedRemind.Core.Models;

public class AppSettings
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public bool NotificationsEnabled { get; set; } = true;
    public bool SoundEnabled { get; set; } = true;
    public bool VibrationEnabled { get; set; } = true;
    public int SnoozeDurationMinutes { get; set; } = 10;
    public string Theme { get; set; } = "System"; // Light, Dark, System
    public string Language { get; set; } = "English";
    public double FontSize { get; set; } = 1.0;
    public int AutoLogoutDays { get; set; } = 30;
    public bool OnboardingCompleted { get; set; } = false;
    public DateTime? LastUpdated { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
}
