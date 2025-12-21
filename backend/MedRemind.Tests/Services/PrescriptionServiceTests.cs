using Microsoft.EntityFrameworkCore;
using MedRemind.Core.Data;
using MedRemind.Core.Models;
using MedRemind.Core.Repositories;
using MedRemind.Services.Prescriptions;
using Xunit;

namespace MedRemind.Tests.Services;

public class PrescriptionServiceTests : IDisposable
{
    private readonly MedRemindDbContext _context;
    private readonly UnitOfWork _unitOfWork;
    private readonly PrescriptionService _prescriptionService;

    public PrescriptionServiceTests()
    {
        var options = new DbContextOptionsBuilder<MedRemindDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new MedRemindDbContext(options);
        _unitOfWork = new UnitOfWork(_context);
        _prescriptionService = new PrescriptionService(_unitOfWork);
    }

    [Fact]
    public async Task AddPrescriptionAsync_ShouldAddPrescription()
    {
        // Arrange
        var user = await CreateTestUser();
        var prescription = new Prescription
        {
            UserId = user.Id,
            ImagePath = "/test/prescription.jpg",
            PrescriptionDate = DateTime.UtcNow,
            Status = "Pending"
        };

        // Act
        var result = await _prescriptionService.AddPrescriptionAsync(prescription);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("Pending", result.Status);
    }

    [Fact]
    public async Task GetPrescriptionByIdAsync_WithValidId_ShouldReturnPrescription()
    {
        // Arrange
        var user = await CreateTestUser();
        var prescription = await CreateTestPrescription(user.Id);

        // Act
        var result = await _prescriptionService.GetPrescriptionByIdAsync(prescription.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(prescription.Id, result.Id);
    }

    [Fact]
    public async Task GetPrescriptionByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = await _prescriptionService.GetPrescriptionByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPrescriptionsByUserAsync_ShouldReturnUserPrescriptions()
    {
        // Arrange
        var user1 = await CreateTestUser();
        var user2 = await CreateTestUser();

        await CreateTestPrescription(user1.Id, "Image1.jpg");
        await CreateTestPrescription(user1.Id, "Image2.jpg");
        await CreateTestPrescription(user2.Id, "Image3.jpg");

        // Act
        var result = await _prescriptionService.GetPrescriptionsByUserAsync(user1.Id);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, p => Assert.Equal(user1.Id, p.UserId));
    }

    [Fact]
    public async Task UpdatePrescriptionStatusAsync_ShouldUpdateStatus()
    {
        // Arrange
        var user = await CreateTestUser();
        var prescription = await CreateTestPrescription(user.Id);

        // Act
        await _prescriptionService.UpdatePrescriptionStatusAsync(
            prescription.Id, 
            "Processed", 
            "{\"medications\": []}", 
            0.95);

        // Assert
        var updated = await _prescriptionService.GetPrescriptionByIdAsync(prescription.Id);
        Assert.NotNull(updated);
        Assert.Equal("Processed", updated.Status);
        Assert.Equal(0.95, updated.ConfidenceScore);
        Assert.NotNull(updated.AiResponse);
        Assert.NotNull(updated.ProcessedAt);
    }

    [Fact]
    public async Task DeletePrescriptionAsync_ShouldRemovePrescription()
    {
        // Arrange
        var user = await CreateTestUser();
        var prescription = await CreateTestPrescription(user.Id);

        // Act
        var result = await _prescriptionService.DeletePrescriptionAsync(prescription.Id);

        // Assert
        Assert.True(result);
        var deleted = await _prescriptionService.GetPrescriptionByIdAsync(prescription.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeletePrescriptionAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Act
        var result = await _prescriptionService.DeletePrescriptionAsync(999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetPrescriptionsByStatusAsync_ShouldFilterByStatus()
    {
        // Arrange
        var user = await CreateTestUser();
        var pending1 = await CreateTestPrescription(user.Id, "img1.jpg", "Pending");
        var processed1 = await CreateTestPrescription(user.Id, "img2.jpg", "Processed");
        var pending2 = await CreateTestPrescription(user.Id, "img3.jpg", "Pending");

        // Act
        var pendingResults = await _prescriptionService.GetPrescriptionsByStatusAsync(user.Id, "Pending");
        var processedResults = await _prescriptionService.GetPrescriptionsByStatusAsync(user.Id, "Processed");

        // Assert
        Assert.Equal(2, pendingResults.Count());
        Assert.Single(processedResults);
    }

    [Fact]
    public async Task GetRecentPrescriptionsAsync_ShouldReturnLatestFirst()
    {
        // Arrange
        var user = await CreateTestUser();
        
        var old = await CreateTestPrescription(user.Id, "old.jpg");
        await Task.Delay(10); // Ensure different timestamps
        var middle = await CreateTestPrescription(user.Id, "middle.jpg");
        await Task.Delay(10);
        var recent = await CreateTestPrescription(user.Id, "recent.jpg");

        // Act
        var results = await _prescriptionService.GetRecentPrescriptionsAsync(user.Id, 10);

        // Assert
        var resultsList = results.ToList();
        Assert.Equal(3, resultsList.Count);
        Assert.Equal(recent.Id, resultsList[0].Id);
        Assert.Equal(middle.Id, resultsList[1].Id);
        Assert.Equal(old.Id, resultsList[2].Id);
    }

    [Fact]
    public async Task GetRecentPrescriptionsAsync_WithLimit_ShouldRespectLimit()
    {
        // Arrange
        var user = await CreateTestUser();
        
        for (int i = 0; i < 10; i++)
        {
            await CreateTestPrescription(user.Id, $"image{i}.jpg");
        }

        // Act
        var results = await _prescriptionService.GetRecentPrescriptionsAsync(user.Id, 5);

        // Assert
        Assert.Equal(5, results.Count());
    }

    [Fact]
    public async Task CountPrescriptionsAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var user = await CreateTestUser();
        
        await CreateTestPrescription(user.Id);
        await CreateTestPrescription(user.Id);
        await CreateTestPrescription(user.Id);

        // Act
        var count = await _prescriptionService.CountPrescriptionsAsync(user.Id);

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task CountPrescriptionsAsync_WithNoData_ShouldReturnZero()
    {
        // Act
        var count = await _prescriptionService.CountPrescriptionsAsync(999);

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task UpdatePrescriptionAsync_ShouldUpdateFields()
    {
        // Arrange
        var user = await CreateTestUser();
        var prescription = await CreateTestPrescription(user.Id);

        // Act
        prescription.DoctorName = "Dr. Updated";
        prescription.PrescriptionDate = DateTime.UtcNow.AddDays(-1);
        await _prescriptionService.UpdatePrescriptionAsync(prescription);

        // Assert
        var updated = await _prescriptionService.GetPrescriptionByIdAsync(prescription.Id);
        Assert.NotNull(updated);
        Assert.Equal("Dr. Updated", updated.DoctorName);
    }

    [Fact]
    public async Task GetPrescriptionsWithMedicationsAsync_ShouldIncludeMedications()
    {
        // Arrange
        var user = await CreateTestUser();
        var prescription = await CreateTestPrescription(user.Id);
        
        var medication = new Medication
        {
            UserId = user.Id,
            PrescriptionId = prescription.Id,
            Name = "Test Med",
            Dosage = "500",
            Unit = "mg",
            Frequency = "Once daily",
            FrequencyCount = 1,
            DurationDays = 7,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7)
        };
        var medRepo = _unitOfWork.Repository<Medication>();
        await medRepo.AddAsync(medication);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var result = await _prescriptionService.GetPrescriptionWithMedicationsAsync(prescription.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Medications);
        Assert.Equal("Test Med", result.Medications.First().Name);
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

    private async Task<Prescription> CreateTestPrescription(
        int userId, 
        string imagePath = "/test/image.jpg",
        string status = "Pending")
    {
        var prescription = new Prescription
        {
            UserId = userId,
            ImagePath = imagePath,
            PrescriptionDate = DateTime.UtcNow,
            Status = status,
            CreatedAt = DateTime.UtcNow
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
