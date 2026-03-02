using MedRemind.Web.Models;

namespace MedRemind.Web.Services;

public interface IReminderService
{
    Task<bool> CreateReminderAsync(CreateReminderModel model);
    Task<bool> CreateMultipleRemindersAsync(CreateMultipleRemindersModel model);
    Task<List<ReminderModel>> GetMedicationRemindersAsync(int medicationId);
    Task<List<ReminderModel>> GetUserRemindersAsync();
    Task<bool> ToggleReminderAsync(int reminderId, bool isEnabled);
    Task<bool> UpdateReminderTimeAsync(int reminderId, TimeSpan newTime);
    Task<bool> DeleteReminderAsync(int reminderId);
    Task<ReminderSuggestionModel?> CalculateReminderTimesAsync(int timesPerDay);
    Task<ReminderSuggestionModel?> CalculateCustomReminderTimesAsync(string frequency);
}
