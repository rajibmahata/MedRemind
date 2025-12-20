using System;

namespace MedRemind.Core.Models;

public class Prescription
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public string? DoctorName { get; set; }
    public DateTime PrescriptionDate { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Processed
    public string? AiResponse { get; set; } // JSON string of AI response
    public double? ConfidenceScore { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<Medication> Medications { get; set; } = new List<Medication>();
}
