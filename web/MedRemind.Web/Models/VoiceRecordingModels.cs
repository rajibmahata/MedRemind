namespace MedRemind.Web.Models;

public class VoiceRecordingModel
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ReminderCount { get; set; }
}

public class CreateVoiceRecordingModel
{
    public string Name { get; set; } = string.Empty;
    public string Base64Audio { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
}

public class UpdateVoiceRecordingModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class VoiceRecordingUploadResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? VoiceRecordingId { get; set; }
    public string? FilePath { get; set; }
    public long? FileSize { get; set; }
}
