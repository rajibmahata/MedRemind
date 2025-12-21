using MedRemind.Core.DTOs;

namespace MedRemind.Core.Interfaces;

public interface IReminderSchedulingService
{
    List<ReminderSchedule> CalculateReminderTimes(int frequencyCount);
    List<ReminderSchedule> CalculateCustomReminderTimes(string frequency);
}

public interface INotificationService
{
    Task<string> ScheduleNotificationAsync(int medicationId, DateTime scheduledTime, string title, string message, string? audioFilePath = null);
    Task CancelNotificationAsync(string notificationId);
    Task CancelAllNotificationsAsync();
    Task RescheduleNotificationAsync(string notificationId, DateTime newTime);
}

public interface IAudioService
{
    Task<bool> IsRecordingAvailableAsync();
    Task<(bool Success, string? FilePath, string? ErrorMessage)> StartRecordingAsync(string fileName);
    Task StopRecordingAsync();
    Task<bool> PlayAudioAsync(string filePath);
    Task StopPlaybackAsync();
    Task<int> GetRecordingDurationAsync(string filePath);
}
