using SQLite;

namespace MedRemind.Models;

public class Medication
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Dosage { get; set; } = string.Empty;

    public string Frequency { get; set; } = string.Empty;

    public string Instructions { get; set; } = string.Empty;

    public DateTime StartDate { get; set; } = DateTime.Now;

    public DateTime? EndDate { get; set; }

    public int PrescriptionId { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }
}
