using MedRemind.Core.Interfaces;

namespace MedRemind.Services.Notifications;

/// <summary>
/// Base notification service - will be implemented platform-specifically in MAUI app
/// </summary>
public class LocalNotificationService : INotificationService
{
    private readonly Dictionary<string, NotificationData> _scheduledNotifications = new();

    public Task<string> ScheduleNotificationAsync(
        int medicationId, 
        DateTime scheduledTime, 
        string title, 
        string message, 
        string? audioFilePath = null)
    {
        var notificationId = Guid.NewGuid().ToString();
        
        _scheduledNotifications[notificationId] = new NotificationData
        {
            Id = notificationId,
            MedicationId = medicationId,
            ScheduledTime = scheduledTime,
            Title = title,
            Message = message,
            AudioFilePath = audioFilePath
        };

        // Platform-specific implementation will override this
        Console.WriteLine($"[NOTIFICATION SCHEDULED] ID: {notificationId}, Time: {scheduledTime}, Title: {title}");
        
        return Task.FromResult(notificationId);
    }

    public Task CancelNotificationAsync(string notificationId)
    {
        if (_scheduledNotifications.ContainsKey(notificationId))
        {
            _scheduledNotifications.Remove(notificationId);
            Console.WriteLine($"[NOTIFICATION CANCELLED] ID: {notificationId}");
        }
        
        return Task.CompletedTask;
    }

    public Task CancelAllNotificationsAsync()
    {
        var count = _scheduledNotifications.Count;
        _scheduledNotifications.Clear();
        Console.WriteLine($"[ALL NOTIFICATIONS CANCELLED] Count: {count}");
        
        return Task.CompletedTask;
    }

    public Task RescheduleNotificationAsync(string notificationId, DateTime newTime)
    {
        if (_scheduledNotifications.TryGetValue(notificationId, out var notification))
        {
            notification.ScheduledTime = newTime;
            Console.WriteLine($"[NOTIFICATION RESCHEDULED] ID: {notificationId}, New Time: {newTime}");
        }
        
        return Task.CompletedTask;
    }

    private class NotificationData
    {
        public string Id { get; set; } = string.Empty;
        public int MedicationId { get; set; }
        public DateTime ScheduledTime { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? AudioFilePath { get; set; }
    }
}
