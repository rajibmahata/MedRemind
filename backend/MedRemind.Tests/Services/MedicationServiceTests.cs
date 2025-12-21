using Microsoft.EntityFrameworkCore;
using MedRemind.Core.Data;
using MedRemind.Core.DTOs;
using MedRemind.Core.Interfaces;
using MedRemind.Core.Models;
using MedRemind.Core.Repositories;
using MedRemind.Services.Medications;
using Moq;
using Xunit;

namespace MedRemind.Tests.Services;

public class MedicationServiceTests : IDisposable
{
    private readonly MedRemindDbContext _context;
    private readonly UnitOfWork _unitOfWork;
    private readonly MedicationService _medicationService;
    private readonly Mock<IReminderSchedulingService> _mockReminderScheduling;
    private readonly Mock<INotificationService> _mockNotificationService;

    public MedicationServiceTests()
    {
        var options = new DbContextOptionsBuilder<MedRemindDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new MedRemindDbContext(options);
        _unitOfWork = new UnitOfWork(_context);
        
        // Setup mock services
        _mockReminderScheduling = new Mock<IReminderSchedulingService>();
        _mockNotificationService = new Mock<INotificationService>();
        
        // Setup default behavior for reminder scheduling
        _mockReminderScheduling
            .Setup(x => x.CalculateReminderTimes(It.IsAny<int>()))
            .Returns((int count) => Enumerable.Range(0, count)
                .Select(i => new ReminderSchedule { Time = TimeSpan.FromHours(8 + i * 4) })
                .ToList());
        
        _mockNotificationService
            .Setup(x => x.ScheduleNotificationAsync(
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>()))
            .ReturnsAsync((int id, DateTime time, string title, string body, string? audio) => 
                $"notification_{id}_{Guid.NewGuid()}");
        
        _medicationService = new MedicationService(
            _unitOfWork, 
            _mockReminderScheduling.Object,
            _mockNotificationService.Object);
    }

    [Fact]
    public async Task CreateMedicationAsync_ShouldAddMedication()
    {
        // Arrange
        var user = await CreateTestUser();
        var medicationData = new MedicationData
        {
            Name = "Paracetamol",
            Dosage = "500",
            Unit = "mg",
            Frequency = "Twice daily",
            FrequencyCount = 2,
            DurationDays = 7,
            Instructions = "Take with food"
        };

        // Act
        var result = await _medicationService.CreateMedicationAsync(user.Id, medicationData);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("Paracetamol", result.Name);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetActiveMedicationsAsync_ShouldReturnOnlyActiveMedications()
    {
        // Arrange
        var user = await CreateTestUser();
        var activeMed = await CreateTestMedication(user.Id, "Active Med", isActive: true);
        var inactiveMed = await CreateTestMedication(user.Id, "Inactive Med", isActive: false);

        // Act
        var result = await _medicationService.GetActiveMedicationsAsync(user.Id);

        // Assert
        Assert.Single(result);
        Assert.Equal("Active Med", result.First().Name);
    }

    [Fact]
    public async Task GetMedicationByIdAsync_WithValidId_ShouldReturnMedication()
    {
        // Arrange
        var user = await CreateTestUser();
        var medication = await CreateTestMedication(user.Id, "Test Med");

        // Act
        var medicationRepo = _unitOfWork.Repository<Medication>();
        var result = await medicationRepo.GetByIdAsync(medication.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(medication.Id, result.Id);
        Assert.Equal("Test Med", result.Name);
    }

    [Fact]
    public async Task GetMedicationByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var medicationRepo = _unitOfWork.Repository<Medication>();
        var result = await medicationRepo.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateMedicationAsync_ShouldUpdateFields()
    {
        // Arrange
        var user = await CreateTestUser();
        var medication = await CreateTestMedication(user.Id, "Original Name");
        
        var updatedData = new MedicationData
        {
            Name = "Updated Name",
            Dosage = "1000",
            Unit = "mg",
            Frequency = "Twice daily",
            FrequencyCount = 2,
            DurationDays = 7,
            Instructions = "Updated instructions"
        };

        // Act
        await _medicationService.UpdateMedicationAsync(medication.Id, updatedData, false);

        // Assert
        var medicationRepo = _unitOfWork.Repository<Medication>();
        var updated = await medicationRepo.GetByIdAsync(medication.Id);
        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated.Name);
        Assert.Equal("1000", updated.Dosage);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public async Task PauseMedicationAsync_ShouldSetIsActiveFalse()
    {
        // Arrange
        var user = await CreateTestUser();
        var medication = await CreateTestMedication(user.Id, "To Pause");

        // Act
        await _medicationService.PauseMedicationAsync(medication.Id);

        // Assert
        var medicationRepo = _unitOfWork.Repository<Medication>();
        var paused = await medicationRepo.GetByIdAsync(medication.Id);
        Assert.NotNull(paused);
        Assert.False(paused.IsActive);
    }

    [Fact]
    public async Task ResumeMedicationAsync_ShouldSetIsActiveTrue()
    {
        // Arrange
        var user = await CreateTestUser();
        var medication = await CreateTestMedication(user.Id, "To Resume", isActive: false);

        // Act
        await _medicationService.ResumeMedicationAsync(medication.Id);

        // Assert
        var medicationRepo = _unitOfWork.Repository<Medication>();
        var resumed = await medicationRepo.GetByIdAsync(medication.Id);
        Assert.NotNull(resumed);
        Assert.True(resumed.IsActive);
    }

    [Fact]
    public async Task DeleteMedicationAsync_ShouldRemoveMedication()
    {
        // Arrange
        var user = await CreateTestUser();
        var medication = await CreateTestMedication(user.Id, "To Delete");

        // Act
        await _medicationService.DeleteMedicationAsync(medication.Id);

        // Assert
        var medicationRepo = _unitOfWork.Repository<Medication>();
        var deleted = await medicationRepo.GetByIdAsync(medication.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task LogDoseAsync_ShouldCreateDoseLog()
    {
        // Arrange
        var user = await CreateTestUser();
        var medication = await CreateTestMedication(user.Id, "Test Med");
        var scheduledTime = DateTime.UtcNow;

        // Act
        await _medicationService.LogDoseAsync(medication.Id, scheduledTime, "Taken");

        // Assert
        var doseLogRepo = _unitOfWork.Repository<DoseLog>();
        var logs = await doseLogRepo.FindAsync(d => d.MedicationId == medication.Id);
        Assert.Single(logs);
        Assert.Equal("Taken", logs.First().Status);
    }

    [Fact]
    public async Task LogDoseAsync_WithTakenStatus_ShouldSetTakenTime()
    {
        // Arrange
        var user = await CreateTestUser();
        var medication = await CreateTestMedication(user.Id, "Test Med");
        var scheduledTime = DateTime.UtcNow;

        // Act
        await _medicationService.LogDoseAsync(medication.Id, scheduledTime, "Taken");

        // Assert
        var doseLogRepo = _unitOfWork.Repository<DoseLog>();
        var log = await doseLogRepo.FirstOrDefaultAsync(d => d.MedicationId == medication.Id);
        Assert.NotNull(log);
        Assert.NotNull(log.TakenTime);
        Assert.True(log.TakenTime <= DateTime.UtcNow);
    }

    [Fact]
    public async Task GetDoseLogsAsync_ShouldReturnLogsForMedication()
    {
        // Arrange
        var user = await CreateTestUser();
        var medication = await CreateTestMedication(user.Id, "Test Med");
        
        await _medicationService.LogDoseAsync(medication.Id, DateTime.UtcNow.AddHours(-2), "Taken");
        await _medicationService.LogDoseAsync(medication.Id, DateTime.UtcNow.AddHours(-1), "Missed");

        // Act
        var logs = await _medicationService.GetDoseLogsAsync(medication.Id);

        // Assert
        Assert.Equal(2, logs.Count());
        Assert.Contains(logs, l => l.Status == "Taken");
        Assert.Contains(logs, l => l.Status == "Missed");
    }

    [Fact]
    public async Task GetDoseLogsForDateRange_ShouldReturnLogsInRange()
    {
        // Arrange
        var user = await CreateTestUser();
        var medication = await CreateTestMedication(user.Id, "Test Med");
        
        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);
        var twoDaysAgo = today.AddDays(-2);

        await _medicationService.LogDoseAsync(medication.Id, twoDaysAgo.AddHours(9), "Taken");
        await _medicationService.LogDoseAsync(medication.Id, yesterday.AddHours(9), "Taken");
        await _medicationService.LogDoseAsync(medication.Id, today.AddHours(9), "Taken");

        // Act
        var doseLogRepo = _unitOfWork.Repository<DoseLog>();
        var logs = await doseLogRepo.FindAsync(d => 
            d.MedicationId == medication.Id && 
            d.ScheduledTime >= yesterday && 
            d.ScheduledTime < today.AddDays(1));

        // Assert
        Assert.Equal(2, logs.Count()); // Should only return yesterday and today
    }

    [Fact]
    public async Task GetMedicationsByPrescription_ShouldReturnRelatedMedications()
    {
        // Arrange
        var user = await CreateTestUser();
        var prescription = await CreateTestPrescription(user.Id);
        
        var med1 = await CreateTestMedication(user.Id, "Med 1", prescriptionId: prescription.Id);
        var med2 = await CreateTestMedication(user.Id, "Med 2", prescriptionId: prescription.Id);
        var med3 = await CreateTestMedication(user.Id, "Med 3"); // Different prescription

        // Act
        var medicationRepo = _unitOfWork.Repository<Medication>();
        var result = await medicationRepo.FindAsync(m => m.PrescriptionId == prescription.Id);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, m => m.Name == "Med 1");
        Assert.Contains(result, m => m.Name == "Med 2");
        Assert.DoesNotContain(result, m => m.Name == "Med 3");
    }

    [Fact]
    public async Task GetUpcomingMedications_ShouldReturnMedicationsNotEnded()
    {
        // Arrange
        var user = await CreateTestUser();
        
        var futureMed = await CreateTestMedication(user.Id, "Future Med", durationDays: 7);
        
        // Create past medication
        var medicationRepo = _unitOfWork.Repository<Medication>();
        var pastMed = new Medication
        {
            UserId = user.Id,
            Name = "Past Med",
            Dosage = "250",
            Unit = "mg",
            Frequency = "Once daily",
            FrequencyCount = 1,
            DurationDays = 7,
            StartDate = DateTime.UtcNow.AddDays(-14),
            EndDate = DateTime.UtcNow.AddDays(-7),
            IsActive = true
        };
        await medicationRepo.AddAsync(pastMed);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var result = await _medicationService.GetActiveMedicationsAsync(user.Id);

        // Assert
        Assert.Single(result);
        Assert.Equal("Future Med", result.First().Name);
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

    private async Task<Medication> CreateTestMedication(
        int userId, 
        string name, 
        bool isActive = true,
        int? prescriptionId = null,
        int durationDays = 7)
    {
        var medicationData = new MedicationData
        {
            Name = name,
            Dosage = "500",
            Unit = "mg",
            Frequency = "Twice daily",
            FrequencyCount = 2,
            DurationDays = durationDays,
            Instructions = "Take with food"
        };

        var medication = await _medicationService.CreateMedicationAsync(userId, medicationData, prescriptionId);
        
        if (!isActive)
        {
            await _medicationService.PauseMedicationAsync(medication.Id);
            var medicationRepo = _unitOfWork.Repository<Medication>();
            medication = await medicationRepo.GetByIdAsync(medication.Id) ?? medication;
        }
        
        return medication;
    }

    private async Task<Prescription> CreateTestPrescription(int userId)
    {
        var prescription = new Prescription
        {
            UserId = userId,
            ImagePath = "/test/path/image.jpg",
            PrescriptionDate = DateTime.UtcNow,
            Status = "Processed"
        };

        var prescriptionRepo = _unitOfWork.Repository<Prescription>();
        await prescriptionRepo.AddAsync(prescription);
        await _unitOfWork.SaveChangesAsync();
        return prescription;
    }

    public void Dispose()
    {
        _unitOfWork.Dispose();
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
