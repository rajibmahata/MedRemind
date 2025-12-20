using SQLite;

namespace MedRemind.Models;

public class Prescription
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string DoctorName { get; set; } = string.Empty;

    public DateTime PrescriptionDate { get; set; } = DateTime.Now;

    public string ImagePath { get; set; } = string.Empty;

    public string ExtractedText { get; set; } = string.Empty;

    public bool IsProcessed { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
