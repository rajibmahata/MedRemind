using MedRemind.Core.DTOs;
using MedRemind.Core.Interfaces;

namespace MedRemind.Services.Reminders;

public class ReminderSchedulingService : IReminderSchedulingService
{
    public List<ReminderSchedule> CalculateReminderTimes(int frequencyCount)
    {
        if (frequencyCount <= 0)
            return new List<ReminderSchedule>();

        return frequencyCount switch
        {
            1 => CreateSchedule(new[] { new TimeSpan(9, 0, 0) }, "Once daily at 9:00 AM"),
            2 => CreateSchedule(
                new[] { new TimeSpan(9, 0, 0), new TimeSpan(21, 0, 0) },
                "Twice daily at 9:00 AM and 9:00 PM"),
            3 => CreateSchedule(
                new[] { new TimeSpan(8, 0, 0), new TimeSpan(14, 0, 0), new TimeSpan(20, 0, 0) },
                "Three times daily at 8:00 AM, 2:00 PM, and 8:00 PM"),
            4 => CreateSchedule(
                new[] { new TimeSpan(8, 0, 0), new TimeSpan(12, 0, 0), new TimeSpan(16, 0, 0), new TimeSpan(20, 0, 0) },
                "Four times daily at 8:00 AM, 12:00 PM, 4:00 PM, and 8:00 PM"),
            _ => CreateDefaultSchedule(frequencyCount)
        };
    }

    public List<ReminderSchedule> CalculateCustomReminderTimes(string frequency)
    {
        var lowerFreq = frequency.ToLowerInvariant();

        if (lowerFreq.Contains("every 8 hours"))
        {
            return CreateSchedule(
                new[] { new TimeSpan(8, 0, 0), new TimeSpan(16, 0, 0), new TimeSpan(0, 0, 0) },
                "Every 8 hours");
        }

        if (lowerFreq.Contains("every 12 hours"))
        {
            return CreateSchedule(
                new[] { new TimeSpan(8, 0, 0), new TimeSpan(20, 0, 0) },
                "Every 12 hours");
        }

        if (lowerFreq.Contains("every 6 hours"))
        {
            return CreateSchedule(
                new[] { new TimeSpan(6, 0, 0), new TimeSpan(12, 0, 0), new TimeSpan(18, 0, 0), new TimeSpan(0, 0, 0) },
                "Every 6 hours");
        }

        if (lowerFreq.Contains("every 4 hours"))
        {
            return CreateSchedule(
                new[] { new TimeSpan(6, 0, 0), new TimeSpan(10, 0, 0), new TimeSpan(14, 0, 0), 
                       new TimeSpan(18, 0, 0), new TimeSpan(22, 0, 0), new TimeSpan(2, 0, 0) },
                "Every 4 hours");
        }

        if (lowerFreq.Contains("every 3 hours"))
        {
            return CreateSchedule(
                new[] { new TimeSpan(6, 0, 0), new TimeSpan(9, 0, 0), new TimeSpan(12, 0, 0),
                       new TimeSpan(15, 0, 0), new TimeSpan(18, 0, 0), new TimeSpan(21, 0, 0),
                       new TimeSpan(0, 0, 0), new TimeSpan(3, 0, 0) },
                "Every 3 hours");
        }

        if (lowerFreq.Contains("before breakfast"))
        {
            return CreateSchedule(new[] { new TimeSpan(7, 30, 0) }, "Before breakfast");
        }

        if (lowerFreq.Contains("before lunch"))
        {
            return CreateSchedule(new[] { new TimeSpan(11, 30, 0) }, "Before lunch");
        }

        if (lowerFreq.Contains("before dinner"))
        {
            return CreateSchedule(new[] { new TimeSpan(18, 30, 0) }, "Before dinner");
        }

        if (lowerFreq.Contains("before bedtime"))
        {
            return CreateSchedule(new[] { new TimeSpan(21, 30, 0) }, "Before bedtime");
        }

        if (lowerFreq.Contains("after breakfast"))
        {
            return CreateSchedule(new[] { new TimeSpan(8, 30, 0) }, "After breakfast");
        }

        if (lowerFreq.Contains("after lunch"))
        {
            return CreateSchedule(new[] { new TimeSpan(13, 0, 0) }, "After lunch");
        }

        if (lowerFreq.Contains("after dinner"))
        {
            return CreateSchedule(new[] { new TimeSpan(20, 0, 0) }, "After dinner");
        }

        if (lowerFreq.Contains("at bedtime") || lowerFreq.Contains("bedtime"))
        {
            return CreateSchedule(new[] { new TimeSpan(22, 0, 0) }, "At bedtime");
        }

        if (lowerFreq.Contains("with food") || lowerFreq.Contains("with meals"))
        {
            return CreateSchedule(
                new[] { new TimeSpan(8, 0, 0), new TimeSpan(12, 30, 0), new TimeSpan(19, 30, 0) },
                "With meals");
        }

        // Default to once daily
        return CalculateReminderTimes(1);
    }

    private List<ReminderSchedule> CreateSchedule(TimeSpan[] times, string description)
    {
        return times.Select(time => new ReminderSchedule
        {
            Time = time,
            Description = description
        }).ToList();
    }

    private List<ReminderSchedule> CreateDefaultSchedule(int count)
    {
        var times = new List<TimeSpan>();
        var hoursInterval = 24.0 / count;

        for (int i = 0; i < count; i++)
        {
            var hours = 8 + (i * hoursInterval); // Start at 8 AM
            if (hours >= 24)
                hours -= 24;
            var timeSpan = TimeSpan.FromHours(hours);
            times.Add(timeSpan);
        }

        return times.Select(time => new ReminderSchedule
        {
            Time = time,
            Description = $"Every {hoursInterval:F1} hours"
        }).ToList();
    }
}
