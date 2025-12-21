namespace MedRemind.Core.DTOs;

public class AdherenceData
{
    public double OverallPercentage { get; set; }
    public int TotalDoses { get; set; }
    public int TakenDoses { get; set; }
    public int MissedDoses { get; set; }
    public int LongestStreak { get; set; }
    public List<DailyAdherence> DailyData { get; set; } = new();
}

public class DailyAdherence
{
    public DateTime Date { get; set; }
    public int TotalDoses { get; set; }
    public int TakenDoses { get; set; }
    public double Percentage { get; set; }
}

public class ReminderSchedule
{
    public TimeSpan Time { get; set; }
    public string Description { get; set; } = string.Empty;
}
