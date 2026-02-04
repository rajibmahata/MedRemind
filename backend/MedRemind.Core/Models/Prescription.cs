using System;

namespace MedRemind.Core.Models;

public class Prescription
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public string? FileName { get; set; } // Original or unique file name
    public long? FileSize { get; set; } // File size in bytes
    public string? DoctorName { get; set; }
    public DateTime PrescriptionDate { get; set; }
    public string Status { get; set; } = "Processing"; // Stores PrescriptionStatus enum value as string
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<Medication> Medications { get; set; } = new List<Medication>();
}
