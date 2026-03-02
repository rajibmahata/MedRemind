using System;

namespace MedRemind.Core.DTOs;

/// <summary>
/// DTOs for voice recording management
/// </summary>

public class VoiceRecordingDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ReminderCount { get; set; } // Number of reminders using this recording
}

public class CreateVoiceRecordingRequest
{
    public string Name { get; set; } = string.Empty;
    public string Base64Audio { get; set; } = string.Empty; // Base64 encoded audio file
    public string FileName { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
}

public class UpdateVoiceRecordingRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class VoiceRecordingUploadResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? VoiceRecordingId { get; set; }
    public string? FilePath { get; set; }
    public long? FileSize { get; set; }
}
