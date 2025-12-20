using MedRemind.Core.DTOs;
using MedRemind.Core.Interfaces;
using MedRemind.Core.Models;

namespace MedRemind.Services.Medications;

public class AdherenceService
{
    private readonly IUnitOfWork _unitOfWork;

    public AdherenceService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AdherenceData> GetAdherenceDataAsync(
        int userId,
        DateTime startDate,
        DateTime endDate)
    {
        var medicationRepo = _unitOfWork.Repository<Medication>();
        var doseLogRepo = _unitOfWork.Repository<DoseLog>();

        var medications = await medicationRepo.FindAsync(m => m.UserId == userId);
        var medicationIds = medications.Select(m => m.Id).ToList();

        var doseLogs = await doseLogRepo.FindAsync(dl =>
            medicationIds.Contains(dl.MedicationId) &&
            dl.ScheduledTime >= startDate &&
            dl.ScheduledTime <= endDate);

        var doseLogsList = doseLogs.ToList();

        var totalDoses = doseLogsList.Count;
        var takenDoses = doseLogsList.Count(dl => dl.Status == "Taken");
        var missedDoses = doseLogsList.Count(dl => dl.Status == "Missed");

        var adherencePercentage = totalDoses > 0
            ? (double)takenDoses / totalDoses * 100
            : 0.0;

        var longestStreak = CalculateLongestStreak(doseLogsList);

        var dailyData = GenerateDailyAdherence(doseLogsList, startDate, endDate);

        return new AdherenceData
        {
            OverallPercentage = Math.Round(adherencePercentage, 2),
            TotalDoses = totalDoses,
            TakenDoses = takenDoses,
            MissedDoses = missedDoses,
            LongestStreak = longestStreak,
            DailyData = dailyData
        };
    }

    public async Task<AdherenceData> GetMedicationAdherenceAsync(
        int medicationId,
        DateTime startDate,
        DateTime endDate)
    {
        var doseLogRepo = _unitOfWork.Repository<DoseLog>();

        var doseLogs = await doseLogRepo.FindAsync(dl =>
            dl.MedicationId == medicationId &&
            dl.ScheduledTime >= startDate &&
            dl.ScheduledTime <= endDate);

        var doseLogsList = doseLogs.ToList();

        var totalDoses = doseLogsList.Count;
        var takenDoses = doseLogsList.Count(dl => dl.Status == "Taken");
        var missedDoses = doseLogsList.Count(dl => dl.Status == "Missed");

        var adherencePercentage = totalDoses > 0
            ? (double)takenDoses / totalDoses * 100
            : 0.0;

        var longestStreak = CalculateLongestStreak(doseLogsList);
        var dailyData = GenerateDailyAdherence(doseLogsList, startDate, endDate);

        return new AdherenceData
        {
            OverallPercentage = Math.Round(adherencePercentage, 2),
            TotalDoses = totalDoses,
            TakenDoses = takenDoses,
            MissedDoses = missedDoses,
            LongestStreak = longestStreak,
            DailyData = dailyData
        };
    }

    public async Task MarkMissedDosesAsync()
    {
        var reminderRepo = _unitOfWork.Repository<Reminder>();
        var doseLogRepo = _unitOfWork.Repository<DoseLog>();
        var medicationRepo = _unitOfWork.Repository<Medication>();

        var activeReminders = await reminderRepo.FindAsync(r => r.IsEnabled);

        foreach (var reminder in activeReminders)
        {
            var medication = await medicationRepo.GetByIdAsync(reminder.MedicationId);
            if (medication == null || !medication.IsActive)
                continue;

            // Check if dose should have been taken in the past 30 minutes
            var now = DateTime.Now;
            var scheduledTime = DateTime.Today.Add(reminder.ReminderTime);

            if (scheduledTime < now.AddMinutes(-30) && scheduledTime > now.AddHours(-24))
            {
                // Check if dose was logged
                var existingLog = await doseLogRepo.FirstOrDefaultAsync(dl =>
                    dl.MedicationId == reminder.MedicationId &&
                    dl.ScheduledTime.Date == scheduledTime.Date &&
                    dl.ScheduledTime.TimeOfDay == scheduledTime.TimeOfDay);

                if (existingLog == null)
                {
                    // Mark as missed
                    await doseLogRepo.AddAsync(new DoseLog
                    {
                        MedicationId = reminder.MedicationId,
                        ScheduledTime = scheduledTime,
                        Status = "Missed",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        await _unitOfWork.SaveChangesAsync();
    }

    private int CalculateLongestStreak(List<DoseLog> doseLogs)
    {
        if (!doseLogs.Any())
            return 0;

        var dailyAdherence = doseLogs
            .GroupBy(dl => dl.ScheduledTime.Date)
            .Select(g => new
            {
                Date = g.Key,
                Total = g.Count(),
                Taken = g.Count(dl => dl.Status == "Taken")
            })
            .OrderBy(d => d.Date)
            .ToList();

        int longestStreak = 0;
        int currentStreak = 0;
        DateTime? lastDate = null;

        foreach (var day in dailyAdherence)
        {
            var adherencePercent = (double)day.Taken / day.Total * 100;

            if (adherencePercent == 100)
            {
                if (lastDate == null || day.Date == lastDate.Value.AddDays(1))
                {
                    currentStreak++;
                    longestStreak = Math.Max(longestStreak, currentStreak);
                }
                else
                {
                    currentStreak = 1;
                }
            }
            else
            {
                currentStreak = 0;
            }

            lastDate = day.Date;
        }

        return longestStreak;
    }

    private List<DailyAdherence> GenerateDailyAdherence(
        List<DoseLog> doseLogs,
        DateTime startDate,
        DateTime endDate)
    {
        var dailyData = new List<DailyAdherence>();

        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            var dayLogs = doseLogs.Where(dl => dl.ScheduledTime.Date == date).ToList();

            if (!dayLogs.Any())
            {
                dailyData.Add(new DailyAdherence
                {
                    Date = date,
                    TotalDoses = 0,
                    TakenDoses = 0,
                    Percentage = 0
                });
                continue;
            }

            var totalDoses = dayLogs.Count;
            var takenDoses = dayLogs.Count(dl => dl.Status == "Taken");
            var percentage = (double)takenDoses / totalDoses * 100;

            dailyData.Add(new DailyAdherence
            {
                Date = date,
                TotalDoses = totalDoses,
                TakenDoses = takenDoses,
                Percentage = Math.Round(percentage, 2)
            });
        }

        return dailyData;
    }
}
