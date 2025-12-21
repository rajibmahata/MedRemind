using Microsoft.EntityFrameworkCore;
using MedRemind.Core.DTOs;
using MedRemind.Core.Interfaces;
using MedRemind.Core.Models;

namespace MedRemind.Services.Medications;

public class MedicationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IReminderSchedulingService _reminderScheduling;
    private readonly INotificationService _notificationService;

    public MedicationService(
        IUnitOfWork unitOfWork,
        IReminderSchedulingService reminderScheduling,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _reminderScheduling = reminderScheduling;
        _notificationService = notificationService;
    }

    public async Task<Medication> CreateMedicationAsync(
        int userId,
        MedicationData medicationData,
        int? prescriptionId = null)
    {
        var medication = new Medication
        {
            UserId = userId,
            PrescriptionId = prescriptionId,
            Name = medicationData.Name,
            Dosage = medicationData.Dosage,
            Unit = medicationData.Unit,
            Frequency = medicationData.Frequency,
            FrequencyCount = medicationData.FrequencyCount,
            DurationDays = medicationData.DurationDays,
            Instructions = medicationData.Instructions,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(medicationData.DurationDays),
            IsActive = true
        };

        var medicationRepo = _unitOfWork.Repository<Medication>();
        await medicationRepo.AddAsync(medication);
        await _unitOfWork.SaveChangesAsync();

        return medication;
    }

    public async Task<List<Reminder>> CreateRemindersAsync(
        int medicationId,
        int frequencyCount,
        int? voiceRecordingId = null)
    {
        var reminderSchedules = _reminderScheduling.CalculateReminderTimes(frequencyCount);
        var reminders = new List<Reminder>();

        var reminderRepo = _unitOfWork.Repository<Reminder>();

        foreach (var schedule in reminderSchedules)
        {
            var reminder = new Reminder
            {
                MedicationId = medicationId,
                VoiceRecordingId = voiceRecordingId,
                ReminderTime = schedule.Time,
                IsEnabled = true
            };

            await reminderRepo.AddAsync(reminder);
            reminders.Add(reminder);
        }

        await _unitOfWork.SaveChangesAsync();

        // Schedule notifications
        await ScheduleNotificationsForRemindersAsync(reminders);

        return reminders;
    }

    public async Task<bool> UpdateMedicationAsync(
        int medicationId,
        MedicationData updatedData,
        bool rescheduleReminders = false)
    {
        var medicationRepo = _unitOfWork.Repository<Medication>();
        var medication = await medicationRepo.GetByIdAsync(medicationId);

        if (medication == null)
            return false;

        bool frequencyChanged = medication.FrequencyCount != updatedData.FrequencyCount;

        medication.Name = updatedData.Name;
        medication.Dosage = updatedData.Dosage;
        medication.Unit = updatedData.Unit;
        medication.Frequency = updatedData.Frequency;
        medication.FrequencyCount = updatedData.FrequencyCount;
        medication.DurationDays = updatedData.DurationDays;
        medication.Instructions = updatedData.Instructions;
        medication.EndDate = medication.StartDate.AddDays(updatedData.DurationDays);
        medication.UpdatedAt = DateTime.UtcNow;

        await medicationRepo.UpdateAsync(medication);
        await _unitOfWork.SaveChangesAsync();

        // Reschedule reminders if frequency changed and user confirmed
        if (frequencyChanged && rescheduleReminders)
        {
            await UpdateRemindersAsync(medicationId, updatedData.FrequencyCount);
        }

        return true;
    }

    public async Task<bool> PauseMedicationAsync(int medicationId)
    {
        var medicationRepo = _unitOfWork.Repository<Medication>();
        var medication = await medicationRepo.GetByIdAsync(medicationId);

        if (medication == null)
            return false;

        medication.IsActive = false;
        medication.UpdatedAt = DateTime.UtcNow;

        await medicationRepo.UpdateAsync(medication);
        await _unitOfWork.SaveChangesAsync();

        // Cancel all reminders
        await DisableRemindersAsync(medicationId);

        return true;
    }

    public async Task<bool> ResumeMedicationAsync(int medicationId)
    {
        var medicationRepo = _unitOfWork.Repository<Medication>();
        var medication = await medicationRepo.GetByIdAsync(medicationId);

        if (medication == null)
            return false;

        medication.IsActive = true;
        medication.UpdatedAt = DateTime.UtcNow;

        await medicationRepo.UpdateAsync(medication);
        await _unitOfWork.SaveChangesAsync();

        // Re-enable reminders
        await EnableRemindersAsync(medicationId);

        return true;
    }

    public async Task<bool> DeleteMedicationAsync(int medicationId)
    {
        var medicationRepo = _unitOfWork.Repository<Medication>();
        var medication = await medicationRepo.GetByIdAsync(medicationId);

        if (medication == null)
            return false;

        // Cancel all notifications first
        await CancelNotificationsForMedicationAsync(medicationId);

        // Delete will cascade to reminders and dose logs
        await medicationRepo.DeleteAsync(medication);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<DoseLog> LogDoseAsync(
        int medicationId,
        DateTime scheduledTime,
        string status,
        string? notes = null)
    {
        var doseLogRepo = _unitOfWork.Repository<DoseLog>();

        var doseLog = new DoseLog
        {
            MedicationId = medicationId,
            ScheduledTime = scheduledTime,
            TakenTime = status == "Taken" ? DateTime.UtcNow : null,
            Status = status,
            Notes = notes
        };

        await doseLogRepo.AddAsync(doseLog);
        await _unitOfWork.SaveChangesAsync();

        return doseLog;
    }

    public async Task<List<Medication>> GetActiveMedicationsAsync(int userId)
    {
        var medicationRepo = _unitOfWork.Repository<Medication>();
        var medications = await medicationRepo.FindAsync(m => 
            m.UserId == userId && 
            m.IsActive && 
            m.EndDate >= DateTime.UtcNow);

        return medications.ToList();
    }

    private async Task UpdateRemindersAsync(int medicationId, int newFrequencyCount)
    {
        var reminderRepo = _unitOfWork.Repository<Reminder>();
        var existingReminders = await reminderRepo.FindAsync(r => r.MedicationId == medicationId);

        // Cancel old notifications
        foreach (var reminder in existingReminders)
        {
            if (!string.IsNullOrEmpty(reminder.NotificationId))
            {
                await _notificationService.CancelNotificationAsync(reminder.NotificationId);
            }
        }

        // Delete old reminders
        await reminderRepo.DeleteRangeAsync(existingReminders);
        await _unitOfWork.SaveChangesAsync();

        // Create new reminders
        await CreateRemindersAsync(medicationId, newFrequencyCount);
    }

    private async Task DisableRemindersAsync(int medicationId)
    {
        var reminderRepo = _unitOfWork.Repository<Reminder>();
        var reminders = await reminderRepo.FindAsync(r => r.MedicationId == medicationId);

        foreach (var reminder in reminders)
        {
            reminder.IsEnabled = false;
            await reminderRepo.UpdateAsync(reminder);

            if (!string.IsNullOrEmpty(reminder.NotificationId))
            {
                await _notificationService.CancelNotificationAsync(reminder.NotificationId);
            }
        }

        await _unitOfWork.SaveChangesAsync();
    }

    private async Task EnableRemindersAsync(int medicationId)
    {
        var reminderRepo = _unitOfWork.Repository<Reminder>();
        var reminders = await reminderRepo.FindAsync(r => r.MedicationId == medicationId);

        foreach (var reminder in reminders)
        {
            reminder.IsEnabled = true;
            await reminderRepo.UpdateAsync(reminder);
        }

        await _unitOfWork.SaveChangesAsync();

        // Reschedule notifications
        await ScheduleNotificationsForRemindersAsync(reminders.ToList());
    }

    private async Task CancelNotificationsForMedicationAsync(int medicationId)
    {
        var reminderRepo = _unitOfWork.Repository<Reminder>();
        var reminders = await reminderRepo.FindAsync(r => r.MedicationId == medicationId);

        foreach (var reminder in reminders)
        {
            if (!string.IsNullOrEmpty(reminder.NotificationId))
            {
                await _notificationService.CancelNotificationAsync(reminder.NotificationId);
            }
        }
    }

    private async Task ScheduleNotificationsForRemindersAsync(List<Reminder> reminders)
    {
        var medicationRepo = _unitOfWork.Repository<Medication>();

        foreach (var reminder in reminders)
        {
            var medication = await medicationRepo.GetByIdAsync(reminder.MedicationId);
            if (medication == null || !medication.IsActive)
                continue;

            // Schedule daily notifications
            var today = DateTime.Today;
            var scheduledTime = today.Add(reminder.ReminderTime);

            if (scheduledTime < DateTime.Now)
            {
                scheduledTime = scheduledTime.AddDays(1);
            }

            var notificationId = await _notificationService.ScheduleNotificationAsync(
                medication.Id,
                scheduledTime,
                "Time for your medication",
                $"Take {medication.Dosage} {medication.Unit} of {medication.Name}",
                reminder.VoiceRecording?.FilePath
            );

            reminder.NotificationId = notificationId;
        }

        await _unitOfWork.SaveChangesAsync();
    }
}
