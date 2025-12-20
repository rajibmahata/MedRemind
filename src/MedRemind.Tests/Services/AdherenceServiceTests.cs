using Microsoft.EntityFrameworkCore;
using MedRemind.Core.Data;
using MedRemind.Core.Models;
using MedRemind.Core.Repositories;
using MedRemind.Services.Medications;
using Xunit;

namespace MedRemind.Tests.Services;

public class AdherenceServiceTests : IDisposable
{
    private readonly MedRemindDbContext _context;
    private readonly UnitOfWork _unitOfWork;
    private readonly AdherenceService _adherenceService;

    public AdherenceServiceTests()
    {
        var options = new DbContextOptionsBuilder<MedRemindDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new MedRemindDbContext(options);
        _unitOfWork = new UnitOfWork(_context);
        _adherenceService = new AdherenceService(_unitOfWork);
    }

    [Fact]
    public async Task CalculateAdherenceAsync_With100PercentCompliance_ShouldReturn100()
    {
        // Arrange
        var user = await CreateTestUser();
        var medication = await CreateTestMedication(user.Id);
        
        // Create 10 doses, all taken
        for (int i = 0; i < 10; i++)
        {
            await CreateDoseLog(medication.Id, DateTime.UtcNow.AddDays(-i), "Taken");
        }

        var startDate = DateTime.UtcNow.AddDays(-9).Date;
        var endDate = DateTime.UtcNow.AddDays(1).Date;

        // Act
        var result = await _adherenceService.CalculateAdherenceAsync(user.Id, startDate, endDate);

        // Assert
        Assert.Equal(100.0, result.OverallPercentage);
        Assert.Equal(10, result.TotalDoses);
        Assert.Equal(10, result.TakenDoses);
        Assert.Equal(0, result.MissedDoses);
    }

    [Fact]
    public async Task CalculateAdherenceAsync_With50PercentCompliance_ShouldReturn50()
    {
        // Arrange
        var user = await CreateTestUser();
        var medication = await CreateTestMedication(user.Id);
        
        // Create 10 doses: 5 taken, 5 missed
        for (int i = 0; i < 5; i++)
        {
            await CreateDoseLog(medication.Id, DateTime.UtcNow.AddDays(-i), "Taken");
        }
        for (int i = 5; i < 10; i++)
        {
            await CreateDoseLog(medication.Id, DateTime.UtcNow.AddDays(-i), "Missed");
        }

        var startDate = DateTime.UtcNow.AddDays(-9).Date;
        var endDate = DateTime.UtcNow.AddDays(1).Date;

        // Act
        var result = await _adherenceService.CalculateAdherenceAsync(user.Id, startDate, endDate);

        // Assert
        Assert.Equal(50.0, result.OverallPercentage);
        Assert.Equal(10, result.TotalDoses);
        Assert.Equal(5, result.TakenDoses);
        Assert.Equal(5, result.MissedDoses);
    }

    [Fact]
    public async Task CalculateAdherenceAsync_WithNoDoses_ShouldReturn0()
    {
        // Arrange
        var user = await CreateTestUser();
        var startDate = DateTime.UtcNow.AddDays(-7).Date;
        var endDate = DateTime.UtcNow.AddDays(1).Date;

        // Act
        var result = await _adherenceService.CalculateAdherenceAsync(user.Id, startDate, endDate);

        // Assert
        Assert.Equal(0.0, result.OverallPercentage);
        Assert.Equal(0, result.TotalDoses);
        Assert.Equal(0, result.TakenDoses);
        Assert.Equal(0, result.MissedDoses);
    }

    [Fact]
    public async Task CalculateAdherenceAsync_ShouldIncludeDailyData()
    {
        // Arrange
        var user = await CreateTestUser();
        var medication = await CreateTestMedication(user.Id);
        
        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);
        
        // Today: 2 taken, 0 missed
        await CreateDoseLog(medication.Id, today.AddHours(9), "Taken");
        await CreateDoseLog(medication.Id, today.AddHours(21), "Taken");
        
        // Yesterday: 1 taken, 1 missed
        await CreateDoseLog(medication.Id, yesterday.AddHours(9), "Taken");
        await CreateDoseLog(medication.Id, yesterday.AddHours(21), "Missed");

        // Act
        var result = await _adherenceService.CalculateAdherenceAsync(user.Id, yesterday, today.AddDays(1));

        // Assert
        Assert.Equal(2, result.DailyData.Count);
        
        var todayData = result.DailyData.First(d => d.Date.Date == today);
        Assert.Equal(2, todayData.TotalDoses);
        Assert.Equal(2, todayData.TakenDoses);
        Assert.Equal(100.0, todayData.Percentage);
        
        var yesterdayData = result.DailyData.First(d => d.Date.Date == yesterday);
        Assert.Equal(2, yesterdayData.TotalDoses);
        Assert.Equal(1, yesterdayData.TakenDoses);
        Assert.Equal(50.0, yesterdayData.Percentage);
    }

    [Fact]
    public async Task CalculateLongestStreakAsync_WithPerfectStreak_ShouldReturnCorrectDays()
    {
        // Arrange
        var user = await CreateTestUser();
        var medication = await CreateTestMedication(user.Id);
        
        // Create perfect streak for 7 days (2 doses per day)
        for (int day = 0; day < 7; day++)
        {
            var date = DateTime.UtcNow.AddDays(-day).Date;
            await CreateDoseLog(medication.Id, date.AddHours(9), "Taken");
            await CreateDoseLog(medication.Id, date.AddHours(21), "Taken");
        }

        // Act
        var streak = await _adherenceService.CalculateLongestStreakAsync(user.Id);

        // Assert
        Assert.Equal(7, streak);
    }

    [Fact]
    public async Task CalculateLongestStreakAsync_WithBrokenStreak_ShouldReturnLongestPeriod()
    {
        // Arrange
        var user = await CreateTestUser();
        var medication = await CreateTestMedication(user.Id);
        
        // Perfect streak for 3 days
        for (int day = 0; day < 3; day++)
        {
            var date = DateTime.UtcNow.AddDays(-day).Date;
            await CreateDoseLog(medication.Id, date.AddHours(9), "Taken");
            await CreateDoseLog(medication.Id, date.AddHours(21), "Taken");
        }
        
        // Missed day (day 3)
        var missedDay = DateTime.UtcNow.AddDays(-3).Date;
        await CreateDoseLog(medication.Id, missedDay.AddHours(9), "Missed");
        await CreateDoseLog(medication.Id, missedDay.AddHours(21), "Missed");
        
        // Perfect streak for 5 days
        for (int day = 4; day < 9; day++)
        {
            var date = DateTime.UtcNow.AddDays(-day).Date;
            await CreateDoseLog(medication.Id, date.AddHours(9), "Taken");
            await CreateDoseLog(medication.Id, date.AddHours(21), "Taken");
        }

        // Act
        var streak = await _adherenceService.CalculateLongestStreakAsync(user.Id);

        // Assert
        Assert.Equal(5, streak); // Should return the longest streak (5 days)
    }

    [Fact]
    public async Task CalculateLongestStreakAsync_WithNoData_ShouldReturn0()
    {
        // Arrange
        var user = await CreateTestUser();

        // Act
        var streak = await _adherenceService.CalculateLongestStreakAsync(user.Id);

        // Assert
        Assert.Equal(0, streak);
    }

    [Fact]
    public async Task CalculateLongestStreakAsync_WithPartialCompliance_ShouldNotCountDay()
    {
        // Arrange
        var user = await CreateTestUser();
        var medication = await CreateTestMedication(user.Id);
        
        var today = DateTime.UtcNow.Date;
        
        // Day 1: Perfect (2/2)
        await CreateDoseLog(medication.Id, today.AddDays(-2).AddHours(9), "Taken");
        await CreateDoseLog(medication.Id, today.AddDays(-2).AddHours(21), "Taken");
        
        // Day 2: Partial (1/2) - breaks streak
        await CreateDoseLog(medication.Id, today.AddDays(-1).AddHours(9), "Taken");
        await CreateDoseLog(medication.Id, today.AddDays(-1).AddHours(21), "Missed");
        
        // Day 3: Perfect (2/2)
        await CreateDoseLog(medication.Id, today.AddHours(9), "Taken");
        await CreateDoseLog(medication.Id, today.AddHours(21), "Taken");

        // Act
        var streak = await _adherenceService.CalculateLongestStreakAsync(user.Id);

        // Assert
        Assert.Equal(1, streak); // Only the most recent perfect day counts
    }

    [Fact]
    public async Task GetWeeklyAdherenceAsync_ShouldReturn7Days()
    {
        // Arrange
        var user = await CreateTestUser();
        var medication = await CreateTestMedication(user.Id);
        
        for (int i = 0; i < 7; i++)
        {
            await CreateDoseLog(medication.Id, DateTime.UtcNow.AddDays(-i), "Taken");
        }

        // Act
        var result = await _adherenceService.GetWeeklyAdherenceAsync(user.Id);

        // Assert
        Assert.Equal(7, result.DailyData.Count);
    }

    [Fact]
    public async Task GetMonthlyAdherenceAsync_ShouldReturn30Days()
    {
        // Arrange
        var user = await CreateTestUser();
        var medication = await CreateTestMedication(user.Id);
        
        for (int i = 0; i < 30; i++)
        {
            await CreateDoseLog(medication.Id, DateTime.UtcNow.AddDays(-i), "Taken");
        }

        // Act
        var result = await _adherenceService.GetMonthlyAdherenceAsync(user.Id);

        // Assert
        Assert.Equal(30, result.DailyData.Count);
    }

    [Fact]
    public async Task CalculateAdherenceAsync_WithMultipleMedications_ShouldAggregateCorrectly()
    {
        // Arrange
        var user = await CreateTestUser();
        var med1 = await CreateTestMedication(user.Id, "Med 1");
        var med2 = await CreateTestMedication(user.Id, "Med 2");
        
        var today = DateTime.UtcNow.Date;
        
        // Med 1: 2 taken
        await CreateDoseLog(med1.Id, today.AddHours(9), "Taken");
        await CreateDoseLog(med1.Id, today.AddHours(21), "Taken");
        
        // Med 2: 1 taken, 1 missed
        await CreateDoseLog(med2.Id, today.AddHours(9), "Taken");
        await CreateDoseLog(med2.Id, today.AddHours(21), "Missed");

        // Act
        var result = await _adherenceService.CalculateAdherenceAsync(user.Id, today, today.AddDays(1));

        // Assert
        Assert.Equal(75.0, result.OverallPercentage); // 3 out of 4 = 75%
        Assert.Equal(4, result.TotalDoses);
        Assert.Equal(3, result.TakenDoses);
        Assert.Equal(1, result.MissedDoses);
    }

    // Helper methods
    private async Task<User> CreateTestUser()
    {
        var user = new User
        {
            PhoneNumber = $"{new Random().Next(1000000000, 2000000000)}",
            Name = "Test User",
            CreatedAt = DateTime.UtcNow
        };

        var userRepo = _unitOfWork.Repository<User>();
        await userRepo.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();
        return user;
    }

    private async Task<Medication> CreateTestMedication(int userId, string name = "Test Medication")
    {
        var medication = new Medication
        {
            UserId = userId,
            Name = name,
            Dosage = "500",
            Unit = "mg",
            Frequency = "Twice daily",
            FrequencyCount = 2,
            DurationDays = 30,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(30),
            IsActive = true
        };

        var medRepo = _unitOfWork.Repository<Medication>();
        await medRepo.AddAsync(medication);
        await _unitOfWork.SaveChangesAsync();
        return medication;
    }

    private async Task CreateDoseLog(int medicationId, DateTime scheduledTime, string status)
    {
        var doseLog = new DoseLog
        {
            MedicationId = medicationId,
            ScheduledTime = scheduledTime,
            Status = status,
            TakenTime = status == "Taken" ? scheduledTime : null,
            CreatedAt = DateTime.UtcNow
        };

        var doseLogRepo = _unitOfWork.Repository<DoseLog>();
        await doseLogRepo.AddAsync(doseLog);
        await _unitOfWork.SaveChangesAsync();
    }

    public void Dispose()
    {
        _unitOfWork.Dispose();
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
