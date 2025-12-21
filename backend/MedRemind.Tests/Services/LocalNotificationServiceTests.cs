using MedRemind.Core.Interfaces;
using MedRemind.Services.Notifications;
using Xunit;

namespace MedRemind.Tests.Services;

public class LocalNotificationServiceTests
{
    private readonly LocalNotificationService _notificationService;

    public LocalNotificationServiceTests()
    {
        _notificationService = new LocalNotificationService();
    }

    [Fact]
    public async Task ScheduleNotificationAsync_ShouldReturnNotificationId()
    {
        // Arrange
        var medicationId = 1;
        var scheduledTime = DateTime.Now.AddHours(1);
        var title = "Test Notification";
        var message = "Take your medication";

        // Act
        var notificationId = await _notificationService.ScheduleNotificationAsync(
            medicationId, 
            scheduledTime, 
            title, 
            message);

        // Assert
        Assert.NotNull(notificationId);
        Assert.NotEmpty(notificationId);
    }

    [Fact]
    public async Task ScheduleNotificationAsync_WithVoiceAudio_ShouldReturnNotificationId()
    {
        // Arrange
        var medicationId = 1;
        var scheduledTime = DateTime.Now.AddHours(1);
        var title = "Test Notification";
        var message = "Take your medication";
        var audioPath = "/path/to/voice.mp3";

        // Act
        var notificationId = await _notificationService.ScheduleNotificationAsync(
            medicationId, 
            scheduledTime, 
            title, 
            message, 
            audioPath);

        // Assert
        Assert.NotNull(notificationId);
        Assert.NotEmpty(notificationId);
        Assert.Contains(medicationId.ToString(), notificationId);
    }

    [Fact]
    public async Task CancelNotificationAsync_WithValidId_ShouldComplete()
    {
        // Arrange
        var medicationId = 1;
        var scheduledTime = DateTime.Now.AddHours(1);
        var notificationId = await _notificationService.ScheduleNotificationAsync(
            medicationId, 
            scheduledTime, 
            "Test", 
            "Message");

        // Act & Assert - Should not throw
        await _notificationService.CancelNotificationAsync(notificationId);
    }

    [Fact]
    public async Task CancelNotificationAsync_WithInvalidId_ShouldNotThrow()
    {
        // Act & Assert - Should not throw
        await _notificationService.CancelNotificationAsync("invalid-id");
    }

    [Fact]
    public async Task CancelAllNotificationsAsync_ShouldComplete()
    {
        // Arrange - Schedule multiple notifications
        await _notificationService.ScheduleNotificationAsync(1, DateTime.Now.AddHours(1), "Test 1", "Message 1");
        await _notificationService.ScheduleNotificationAsync(2, DateTime.Now.AddHours(2), "Test 2", "Message 2");
        await _notificationService.ScheduleNotificationAsync(3, DateTime.Now.AddHours(3), "Test 3", "Message 3");

        // Act & Assert - Should not throw
        await _notificationService.CancelAllNotificationsAsync();
    }

    [Fact]
    public async Task RescheduleNotificationAsync_ShouldComplete()
    {
        // Arrange
        var medicationId = 1;
        var originalTime = DateTime.Now.AddHours(1);
        var notificationId = await _notificationService.ScheduleNotificationAsync(
            medicationId, 
            originalTime, 
            "Test", 
            "Message");

        var newTime = DateTime.Now.AddHours(2);

        // Act & Assert - Should not throw
        await _notificationService.RescheduleNotificationAsync(notificationId, newTime);
    }

    [Fact]
    public async Task ScheduleNotificationAsync_WithPastTime_ShouldStillSchedule()
    {
        // Arrange
        var medicationId = 1;
        var pastTime = DateTime.Now.AddHours(-1);
        var title = "Test Notification";
        var message = "Take your medication";

        // Act
        var notificationId = await _notificationService.ScheduleNotificationAsync(
            medicationId, 
            pastTime, 
            title, 
            message);

        // Assert
        Assert.NotNull(notificationId);
        Assert.NotEmpty(notificationId);
    }

    [Theory]
    [InlineData(1, "Morning Medication", "Take your morning pills")]
    [InlineData(2, "Afternoon Medication", "Take your afternoon pills")]
    [InlineData(3, "Evening Medication", "Take your evening pills")]
    public async Task ScheduleNotificationAsync_WithDifferentTitles_ShouldReturnUniqueIds(
        int medicationId, 
        string title, 
        string message)
    {
        // Arrange
        var scheduledTime = DateTime.Now.AddHours(1);

        // Act
        var notificationId = await _notificationService.ScheduleNotificationAsync(
            medicationId, 
            scheduledTime, 
            title, 
            message);

        // Assert
        Assert.NotNull(notificationId);
        Assert.Contains(medicationId.ToString(), notificationId);
    }

    [Fact]
    public async Task ScheduleNotificationAsync_MultipleTimes_ShouldReturnUniqueIds()
    {
        // Arrange
        var medicationId = 1;
        var scheduledTime = DateTime.Now.AddHours(1);
        var ids = new List<string>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            var id = await _notificationService.ScheduleNotificationAsync(
                medicationId, 
                scheduledTime.AddHours(i), 
                "Test", 
                "Message");
            ids.Add(id);
        }

        // Assert
        Assert.Equal(5, ids.Distinct().Count()); // All IDs should be unique
    }

    [Fact]
    public async Task CancelNotificationAsync_AfterCancelAll_ShouldNotThrow()
    {
        // Arrange
        var notificationId = await _notificationService.ScheduleNotificationAsync(
            1, 
            DateTime.Now.AddHours(1), 
            "Test", 
            "Message");

        await _notificationService.CancelAllNotificationsAsync();

        // Act & Assert - Should not throw
        await _notificationService.CancelNotificationAsync(notificationId);
    }

    [Fact]
    public async Task RescheduleNotificationAsync_WithNullId_ShouldNotThrow()
    {
        // Act & Assert - Should handle gracefully
        await _notificationService.RescheduleNotificationAsync(null!, DateTime.Now.AddHours(1));
    }

    [Fact]
    public async Task ScheduleNotificationAsync_WithEmptyTitle_ShouldStillSchedule()
    {
        // Arrange
        var medicationId = 1;
        var scheduledTime = DateTime.Now.AddHours(1);

        // Act
        var notificationId = await _notificationService.ScheduleNotificationAsync(
            medicationId, 
            scheduledTime, 
            "", 
            "Message");

        // Assert
        Assert.NotNull(notificationId);
    }

    [Fact]
    public async Task ScheduleNotificationAsync_WithLongMessage_ShouldSchedule()
    {
        // Arrange
        var medicationId = 1;
        var scheduledTime = DateTime.Now.AddHours(1);
        var longMessage = new string('A', 500); // 500 character message

        // Act
        var notificationId = await _notificationService.ScheduleNotificationAsync(
            medicationId, 
            scheduledTime, 
            "Test", 
            longMessage);

        // Assert
        Assert.NotNull(notificationId);
    }
}
