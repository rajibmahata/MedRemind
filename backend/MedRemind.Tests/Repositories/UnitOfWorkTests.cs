using Microsoft.EntityFrameworkCore;
using MedRemind.Core.Data;
using MedRemind.Core.Models;
using MedRemind.Core.Repositories;
using Xunit;

namespace MedRemind.Tests.Repositories;

public class UnitOfWorkTests : IDisposable
{
    private readonly MedRemindDbContext _context;
    private readonly UnitOfWork _unitOfWork;

    public UnitOfWorkTests()
    {
        var options = new DbContextOptionsBuilder<MedRemindDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new MedRemindDbContext(options);
        _unitOfWork = new UnitOfWork(_context);
    }

    [Fact]
    public void Repository_ShouldReturnSameInstanceForSameType()
    {
        // Act
        var repo1 = _unitOfWork.Repository<User>();
        var repo2 = _unitOfWork.Repository<User>();

        // Assert
        Assert.Same(repo1, repo2);
    }

    [Fact]
    public void Repository_ShouldReturnDifferentInstancesForDifferentTypes()
    {
        // Act
        var userRepo = _unitOfWork.Repository<User>();
        var medicationRepo = _unitOfWork.Repository<Medication>();

        // Assert
        Assert.NotNull(userRepo);
        Assert.NotNull(medicationRepo);
        Assert.NotSame(userRepo, medicationRepo);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldPersistChanges()
    {
        // Arrange
        var userRepo = _unitOfWork.Repository<User>();
        var user = new User
        {
            PhoneNumber = "1234567890",
            Name = "Test User"
        };

        // Act
        await userRepo.AddAsync(user);
        var result = await _unitOfWork.SaveChangesAsync();

        // Assert
        Assert.Equal(1, result);
        var savedUser = await userRepo.FirstOrDefaultAsync(u => u.PhoneNumber == "1234567890");
        Assert.NotNull(savedUser);
    }

    [Fact]
    public async Task BeginTransaction_CommitTransaction_ShouldPersistAllChanges()
    {
        // Arrange
        var userRepo = _unitOfWork.Repository<User>();
        var user1 = new User { PhoneNumber = "1111111111", Name = "User 1" };
        var user2 = new User { PhoneNumber = "2222222222", Name = "User 2" };

        // Act
        await _unitOfWork.BeginTransactionAsync();
        await userRepo.AddAsync(user1);
        await userRepo.AddAsync(user2);
        await _unitOfWork.CommitTransactionAsync();

        // Assert
        var allUsers = await userRepo.GetAllAsync();
        Assert.Equal(2, allUsers.Count());
    }

    [Fact]
    public async Task RollbackTransaction_ShouldDiscardChanges()
    {
        // Arrange
        var userRepo = _unitOfWork.Repository<User>();
        var user = new User { PhoneNumber = "3333333333", Name = "Rollback Test" };

        // Act
        await _unitOfWork.BeginTransactionAsync();
        await userRepo.AddAsync(user);
        await _unitOfWork.RollbackTransactionAsync();

        // Assert
        var allUsers = await userRepo.GetAllAsync();
        Assert.Empty(allUsers);
    }

    [Fact]
    public async Task CommitTransaction_WithException_ShouldRollback()
    {
        // Arrange
        var userRepo = _unitOfWork.Repository<User>();
        
        // Add a user first to establish baseline
        var existingUser = new User { PhoneNumber = "4444444444", Name = "Existing" };
        await userRepo.AddAsync(existingUser);
        await _unitOfWork.SaveChangesAsync();

        // Act & Assert
        await _unitOfWork.BeginTransactionAsync();
        
        // Try to add duplicate phone number (should fail due to unique constraint)
        var duplicateUser = new User { PhoneNumber = "4444444444", Name = "Duplicate" };
        await userRepo.AddAsync(duplicateUser);
        
        await Assert.ThrowsAsync<DbUpdateException>(async () => 
            await _unitOfWork.CommitTransactionAsync());

        // Verify rollback - only original user should exist
        var allUsers = await userRepo.GetAllAsync();
        Assert.Single(allUsers);
    }

    [Fact]
    public async Task MultipleRepositories_ShouldWorkTogether()
    {
        // Arrange
        var userRepo = _unitOfWork.Repository<User>();
        var medicationRepo = _unitOfWork.Repository<Medication>();

        var user = new User { PhoneNumber = "5555555555", Name = "Med User" };
        await userRepo.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var medication = new Medication
        {
            UserId = user.Id,
            Name = "Test Medicine",
            Dosage = "500",
            Unit = "mg",
            Frequency = "Once daily",
            FrequencyCount = 1,
            DurationDays = 7,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7)
        };

        // Act
        await medicationRepo.AddAsync(medication);
        await _unitOfWork.SaveChangesAsync();

        // Assert
        var savedMed = await medicationRepo.GetByIdAsync(medication.Id);
        Assert.NotNull(savedMed);
        Assert.Equal(user.Id, savedMed.UserId);
    }

    public void Dispose()
    {
        _unitOfWork.Dispose();
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
