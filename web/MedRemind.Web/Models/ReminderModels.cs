namespace MedRemind.Web.Models;

public class ReminderModel
{
    public int Id { get; set; }
    public int MedicationId { get; set; }
    public int? VoiceRecordingId { get; set; }
    public TimeSpan ReminderTime { get; set; }
    public bool IsEnabled { get; set; }
    public string? NotificationId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string MedicationName { get; set; } = string.Empty;
    public string MedicationDosage { get; set; } = string.Empty;
    public string MedicationUnit { get; set; } = string.Empty;
    public string? VoiceRecordingName { get; set; }
}

public class CreateReminderModel
{
    public int MedicationId { get; set; }
    public int? VoiceRecordingId { get; set; }
    public TimeSpan ReminderTime { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class CreateMultipleRemindersModel
{
    public int MedicationId { get; set; }
    public int? VoiceRecordingId { get; set; }
    public List<TimeSpan> ReminderTimes { get; set; } = new();
}

public class ReminderSuggestionModel
{
    public string Frequency { get; set; } = string.Empty;
    public int TimesPerDay { get; set; }
    public List<TimeSpan> SuggestedTimes { get; set; } = new();
}
