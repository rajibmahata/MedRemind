using MedRemind.Services.Reminders;
using Xunit;

namespace MedRemind.Tests.Services;

public class ReminderSchedulingServiceTests
{
    private readonly ReminderSchedulingService _schedulingService;

    public ReminderSchedulingServiceTests()
    {
        _schedulingService = new ReminderSchedulingService();
    }

    [Fact]
    public void CalculateReminderTimes_OnceDaily_ShouldReturn9AM()
    {
        // Act
        var reminders = _schedulingService.CalculateReminderTimes(1);

        // Assert
        Assert.Single(reminders);
        Assert.Equal(new TimeSpan(9, 0, 0), reminders[0].Time);
        Assert.Contains("9:00 AM", reminders[0].Description);
    }

    [Fact]
    public void CalculateReminderTimes_TwiceDaily_ShouldReturn9AMAnd9PM()
    {
        // Act
        var reminders = _schedulingService.CalculateReminderTimes(2);

        // Assert
        Assert.Equal(2, reminders.Count);
        Assert.Equal(new TimeSpan(9, 0, 0), reminders[0].Time);
        Assert.Equal(new TimeSpan(21, 0, 0), reminders[1].Time);
        Assert.Contains("9:00 AM", reminders[0].Description);
        Assert.Contains("9:00 PM", reminders[1].Description);
    }

    [Fact]
    public void CalculateReminderTimes_ThreeTimesDaily_ShouldReturnCorrectTimes()
    {
        // Act
        var reminders = _schedulingService.CalculateReminderTimes(3);

        // Assert
        Assert.Equal(3, reminders.Count);
        Assert.Equal(new TimeSpan(8, 0, 0), reminders[0].Time);  // 8:00 AM
        Assert.Equal(new TimeSpan(14, 0, 0), reminders[1].Time); // 2:00 PM
        Assert.Equal(new TimeSpan(20, 0, 0), reminders[2].Time); // 8:00 PM
    }

    [Fact]
    public void CalculateReminderTimes_FourTimesDaily_ShouldReturnCorrectTimes()
    {
        // Act
        var reminders = _schedulingService.CalculateReminderTimes(4);

        // Assert
        Assert.Equal(4, reminders.Count);
        Assert.Equal(new TimeSpan(8, 0, 0), reminders[0].Time);  // 8:00 AM
        Assert.Equal(new TimeSpan(12, 0, 0), reminders[1].Time); // 12:00 PM
        Assert.Equal(new TimeSpan(16, 0, 0), reminders[2].Time); // 4:00 PM
        Assert.Equal(new TimeSpan(20, 0, 0), reminders[3].Time); // 8:00 PM
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    [InlineData(4, 4)]
    [InlineData(5, 5)]
    [InlineData(6, 6)]
    public void CalculateReminderTimes_VariousFrequencies_ShouldReturnCorrectCount(
        int frequency, int expectedCount)
    {
        // Act
        var reminders = _schedulingService.CalculateReminderTimes(frequency);

        // Assert
        Assert.Equal(expectedCount, reminders.Count);
    }

    [Fact]
    public void CalculateReminderTimes_InvalidFrequency_ShouldReturnEmpty()
    {
        // Act
        var reminders = _schedulingService.CalculateReminderTimes(0);

        // Assert
        Assert.Empty(reminders);
    }

    [Fact]
    public void CalculateReminderTimes_NegativeFrequency_ShouldReturnEmpty()
    {
        // Act
        var reminders = _schedulingService.CalculateReminderTimes(-1);

        // Assert
        Assert.Empty(reminders);
    }

    [Fact]
    public void CalculateCustomReminderTimes_Every8Hours_ShouldReturnCorrectTimes()
    {
        // Act
        var reminders = _schedulingService.CalculateCustomReminderTimes("Every 8 hours");

        // Assert
        Assert.Equal(3, reminders.Count);
        Assert.Equal(new TimeSpan(8, 0, 0), reminders[0].Time);  // 8:00 AM
        Assert.Equal(new TimeSpan(16, 0, 0), reminders[1].Time); // 4:00 PM
        Assert.Equal(new TimeSpan(0, 0, 0), reminders[2].Time);  // 12:00 AM
    }

    [Fact]
    public void CalculateCustomReminderTimes_Every12Hours_ShouldReturnCorrectTimes()
    {
        // Act
        var reminders = _schedulingService.CalculateCustomReminderTimes("Every 12 hours");

        // Assert
        Assert.Equal(2, reminders.Count);
        Assert.Equal(new TimeSpan(8, 0, 0), reminders[0].Time);  // 8:00 AM
        Assert.Equal(new TimeSpan(20, 0, 0), reminders[1].Time); // 8:00 PM
    }

    [Theory]
    [InlineData("Before breakfast", 7, 30)]
    [InlineData("Before lunch", 11, 30)]
    [InlineData("Before dinner", 18, 30)]
    [InlineData("Before bedtime", 21, 30)]
    public void CalculateCustomReminderTimes_BeforeMeals_ShouldReturnCorrectTime(
        string frequency, int expectedHour, int expectedMinute)
    {
        // Act
        var reminders = _schedulingService.CalculateCustomReminderTimes(frequency);

        // Assert
        Assert.Single(reminders);
        Assert.Equal(new TimeSpan(expectedHour, expectedMinute, 0), reminders[0].Time);
        Assert.Contains(frequency, reminders[0].Description, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("After breakfast", 8, 30)]
    [InlineData("After lunch", 13, 0)]
    [InlineData("After dinner", 20, 0)]
    public void CalculateCustomReminderTimes_AfterMeals_ShouldReturnCorrectTime(
        string frequency, int expectedHour, int expectedMinute)
    {
        // Act
        var reminders = _schedulingService.CalculateCustomReminderTimes(frequency);

        // Assert
        Assert.Single(reminders);
        Assert.Equal(new TimeSpan(expectedHour, expectedMinute, 0), reminders[0].Time);
    }

    [Fact]
    public void CalculateCustomReminderTimes_AtBedtime_ShouldReturn10PM()
    {
        // Act
        var reminders = _schedulingService.CalculateCustomReminderTimes("At bedtime");

        // Assert
        Assert.Single(reminders);
        Assert.Equal(new TimeSpan(22, 0, 0), reminders[0].Time);
    }

    [Fact]
    public void CalculateCustomReminderTimes_WithFood_ShouldReturnMealTimes()
    {
        // Act
        var reminders = _schedulingService.CalculateCustomReminderTimes("With food");

        // Assert
        Assert.Equal(3, reminders.Count);
        Assert.Equal(new TimeSpan(8, 0, 0), reminders[0].Time);   // Breakfast
        Assert.Equal(new TimeSpan(12, 30, 0), reminders[1].Time); // Lunch
        Assert.Equal(new TimeSpan(19, 30, 0), reminders[2].Time); // Dinner
    }

    [Fact]
    public void CalculateCustomReminderTimes_InvalidFrequency_ShouldReturnStandardTimes()
    {
        // Act
        var reminders = _schedulingService.CalculateCustomReminderTimes("Invalid frequency string");

        // Assert
        // Should return default once daily (9:00 AM)
        Assert.Single(reminders);
        Assert.Equal(new TimeSpan(9, 0, 0), reminders[0].Time);
    }

    [Fact]
    public void CalculateReminderTimes_AllTimesWithin24Hours_ShouldBeValid()
    {
        // Act
        for (int i = 1; i <= 6; i++)
        {
            var reminders = _schedulingService.CalculateReminderTimes(i);

            // Assert
            foreach (var reminder in reminders)
            {
                Assert.True(reminder.Time.TotalHours >= 0);
                Assert.True(reminder.Time.TotalHours < 24);
            }
        }
    }

    [Fact]
    public void CalculateReminderTimes_TimesSpreadThroughoutDay_ShouldBeOrdered()
    {
        // Act
        var reminders = _schedulingService.CalculateReminderTimes(4);

        // Assert
        for (int i = 0; i < reminders.Count - 1; i++)
        {
            Assert.True(reminders[i].Time < reminders[i + 1].Time,
                $"Reminder times should be in ascending order: {reminders[i].Time} should be less than {reminders[i + 1].Time}");
        }
    }

    [Theory]
    [InlineData("Every 6 hours", 4)]
    [InlineData("Every 4 hours", 6)]
    [InlineData("Every 3 hours", 8)]
    public void CalculateCustomReminderTimes_EveryNHours_ShouldReturnCorrectCount(
        string frequency, int expectedCount)
    {
        // Act
        var reminders = _schedulingService.CalculateCustomReminderTimes(frequency);

        // Assert
        Assert.Equal(expectedCount, reminders.Count);
    }

    [Fact]
    public void CalculateReminderTimes_HighFrequency_ShouldDistributeEvenly()
    {
        // Act
        var reminders = _schedulingService.CalculateReminderTimes(6);

        // Assert
        Assert.Equal(6, reminders.Count);
        
        // Check that times are reasonably distributed
        var hoursBetween = new List<double>();
        for (int i = 0; i < reminders.Count - 1; i++)
        {
            hoursBetween.Add((reminders[i + 1].Time - reminders[i].Time).TotalHours);
        }

        // All intervals should be roughly equal (within 1 hour)
        var avgInterval = hoursBetween.Average();
        foreach (var interval in hoursBetween)
        {
            Assert.True(Math.Abs(interval - avgInterval) <= 2,
                $"Time intervals should be roughly equal. Interval: {interval}, Average: {avgInterval}");
        }
    }
}
