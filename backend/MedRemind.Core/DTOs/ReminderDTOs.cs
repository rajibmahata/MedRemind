using System;
using System.Collections.Generic;

namespace MedRemind.Core.DTOs;

/// <summary>
/// DTOs for reminder management
/// </summary>

public class ReminderDto
{
    public int Id { get; set; }
    public int MedicationId { get; set; }
    public int? VoiceRecordingId { get; set; }
    public TimeSpan ReminderTime { get; set; }
    public bool IsEnabled { get; set; }
    public string? NotificationId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Populated from relationships
    public string MedicationName { get; set; } = string.Empty;
    public string MedicationDosage { get; set; } = string.Empty;
    public string MedicationUnit { get; set; } = string.Empty;
    public string? VoiceRecordingName { get; set; }
}

public class CreateReminderRequest
{
    public int MedicationId { get; set; }
    public int? VoiceRecordingId { get; set; }
    public TimeSpan ReminderTime { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class CreateMultipleRemindersRequest
{
    public int MedicationId { get; set; }
    public int? VoiceRecordingId { get; set; }
    public List<TimeSpan> ReminderTimes { get; set; } = new();
}

public class UpdateReminderRequest
{
    public int Id { get; set; }
    public int? VoiceRecordingId { get; set; }
    public TimeSpan? ReminderTime { get; set; }
    public bool? IsEnabled { get; set; }
}

public class ReminderSuggestion
{
    public string Frequency { get; set; } = string.Empty;
    public int TimesPerDay { get; set; }
    public List<TimeSpan> SuggestedTimes { get; set; } = new();
}
