using Microsoft.JSInterop;

namespace MedRemind.Web.Services;

public class NotificationService : INotificationService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IReminderService _reminderService;
    private readonly IVoiceRecordingService _voiceRecordingService;
    private Timer? _schedulerTimer;
    private bool _isSchedulerRunning = false;

    public NotificationService(
        IJSRuntime jsRuntime,
        IReminderService reminderService,
        IVoiceRecordingService voiceRecordingService)
    {
        _jsRuntime = jsRuntime;
        _reminderService = reminderService;
        _voiceRecordingService = voiceRecordingService;
    }

    public async Task<bool> InitializeAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<bool>("notificationService.initialize");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing notification service: {ex.Message}");
            return false;
        }
    }

    public async Task<string> GetPermissionStatusAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string>("notificationService.getPermissionStatus");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting permission status: {ex.Message}");
            return "unknown";
        }
    }

    public async Task<string> RequestPermissionAsync()
    {
        try
        {
            var permission = await _jsRuntime.InvokeAsync<string>("notificationService.requestPermission");

            // If permission granted, start scheduler
            if (permission == "granted")
            {
                await StartReminderSchedulerAsync();
            }

            return permission;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error requesting permission: {ex.Message}");
            return "denied";
        }
    }

    public async Task<bool> ShowNotificationAsync(string title, string body, string? voiceUrl = null, string? tag = null)
    {
        try
        {
            var options = new
            {
                body = body,
                voiceUrl = voiceUrl,
                tag = tag ?? $"notification-{DateTime.Now.Ticks}"
            };

            return await _jsRuntime.InvokeAsync<bool>("notificationService.showNotification", title, options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error showing notification: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ShowMedicationReminderAsync(
        int reminderId,
        string medicationName,
        string dosage,
        string unit,
        string? voiceUrl)
    {
        try
        {
            var reminderData = new
            {
                reminderId = reminderId,
                medicationName = medicationName,
                dosage = dosage,
                unit = unit,
                voiceUrl = voiceUrl,
                instructions = $"Take {dosage} {unit}"
            };

            return await _jsRuntime.InvokeAsync<bool>("notificationService.showMedicationReminder", reminderData);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error showing medication reminder: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> TestNotificationAsync(string medicationName, string? voiceUrl)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<bool>("notificationService.testNotification", medicationName, voiceUrl);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error showing test notification: {ex.Message}");
            return false;
        }
    }

    public async Task StartReminderSchedulerAsync()
    {
        if (_isSchedulerRunning)
        {
            Console.WriteLine("Reminder scheduler already running");
            return;
        }

        // Check permission first
        var permission = await GetPermissionStatusAsync();
        if (permission != "granted")
        {
            Console.WriteLine("Cannot start scheduler: notification permission not granted");
            return;
        }

        _isSchedulerRunning = true;
        Console.WriteLine("🔔 Reminder scheduler started");

        // Check reminders every minute
        _schedulerTimer = new Timer(async _ =>
        {
            await CheckRemindersAsync();
        }, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

        await Task.CompletedTask;
    }

    public Task StopReminderSchedulerAsync()
    {
        if (_schedulerTimer != null)
        {
            _schedulerTimer.Dispose();
            _schedulerTimer = null;
        }

        _isSchedulerRunning = false;
        Console.WriteLine("🔕 Reminder scheduler stopped");

        return Task.CompletedTask;
    }

    private async Task CheckRemindersAsync()
    {
        try
        {
            // Get current time
            var now = DateTime.Now;
            var currentTime = now.TimeOfDay;

            // Round to nearest minute for comparison
            var currentMinute = new TimeSpan(currentTime.Hours, currentTime.Minutes, 0);

            Console.WriteLine($"⏰ Checking reminders at {now:HH:mm}...");

            // Get all user reminders
            var reminders = await _reminderService.GetUserRemindersAsync();

            foreach (var reminder in reminders.Where(r => r.IsEnabled))
            {
                // Round reminder time to minute
                var reminderMinute = new TimeSpan(
                    reminder.ReminderTime.Hours,
                    reminder.ReminderTime.Minutes,
                    0);

                // Check if it's time for this reminder
                if (reminderMinute == currentMinute)
                {
                    Console.WriteLine($"🔔 Triggering reminder for {reminder.MedicationName}");

                    // Get voice URL if available
                    string? voiceUrl = null;
                    if (reminder.VoiceRecordingId.HasValue)
                    {
                        voiceUrl = _voiceRecordingService.GetPlaybackUrl(reminder.VoiceRecordingId.Value);
                    }

                    // Show notification
                    await ShowMedicationReminderAsync(
                        reminder.Id,
                        reminder.MedicationName,
                        reminder.MedicationDosage,
                        reminder.MedicationUnit,
                        voiceUrl);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking reminders: {ex.Message}");
        }
    }
}
