namespace MedRemind.Web.Services;

public interface INotificationService
{
    Task<bool> InitializeAsync();
    Task<string> GetPermissionStatusAsync();
    Task<string> RequestPermissionAsync();
    Task<bool> ShowNotificationAsync(string title, string body, string? voiceUrl = null, string? tag = null);
    Task<bool> ShowMedicationReminderAsync(int reminderId, string medicationName, string dosage, string unit, string? voiceUrl);
    Task<bool> TestNotificationAsync(string medicationName, string? voiceUrl);
    Task StartReminderSchedulerAsync();
    Task StopReminderSchedulerAsync();
}
