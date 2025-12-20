using System;

namespace MedRemind.Core.Models;

public class User
{
    public int Id { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Name { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? ProfilePhotoPath { get; set; }
    public bool IsBiometricEnabled { get; set; }
    public string? SessionToken { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    
    // Navigation properties
    public ICollection<Medication> Medications { get; set; } = new List<Medication>();
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    public ICollection<VoiceRecording> VoiceRecordings { get; set; } = new List<VoiceRecording>();
}
