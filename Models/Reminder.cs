using SQLite;

namespace MedRemind.Models;

public class Reminder
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int MedicationId { get; set; }

    public TimeSpan Time { get; set; }

    public bool IsEnabled { get; set; } = true;

    public DateTime? LastNotified { get; set; }

    public string? CustomMessage { get; set; }
}
