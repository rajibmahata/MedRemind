using Microsoft.EntityFrameworkCore;
using MedRemind.Core.Data;
using MedRemind.Core.Models;
using MedRemind.Core.Repositories;
using Xunit;

namespace MedRemind.Tests.Repositories;

public class RepositoryTests : IDisposable
{
    private readonly MedRemindDbContext _context;
    private readonly Repository<User> _userRepository;

    public RepositoryTests()
    {
        var options = new DbContextOptionsBuilder<MedRemindDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new MedRemindDbContext(options);
        _userRepository = new Repository<User>(_context);
    }

    [Fact]
    public async Task AddAsync_ShouldAddEntity()
    {
        // Arrange
        var user = new User
        {
            PhoneNumber = "9876543210",
            Name = "Test User"
        };

        // Act
        var result = await _userRepository.AddAsync(user);
        await _context.SaveChangesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.PhoneNumber, result.PhoneNumber);
        
        var savedUser = await _userRepository.GetByIdAsync(result.Id);
        Assert.NotNull(savedUser);
        Assert.Equal("Test User", savedUser.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnEntity()
    {
        // Arrange
        var user = new User
        {
            PhoneNumber = "1234567890",
            Name = "John Doe"
        };
        await _userRepository.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userRepository.GetByIdAsync(user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John Doe", result.Name);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllEntities()
    {
        // Arrange
        var users = new[]
        {
            new User { PhoneNumber = "1111111111", Name = "User 1" },
            new User { PhoneNumber = "2222222222", Name = "User 2" },
            new User { PhoneNumber = "3333333333", Name = "User 3" }
        };

        foreach (var user in users)
        {
            await _userRepository.AddAsync(user);
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _userRepository.GetAllAsync();

        // Assert
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task FindAsync_ShouldReturnMatchingEntities()
    {
        // Arrange
        var users = new[]
        {
            new User { PhoneNumber = "1111111111", Name = "Alice" },
            new User { PhoneNumber = "2222222222", Name = "Bob" },
            new User { PhoneNumber = "3333333333", Name = "Alice Smith" }
        };

        foreach (var user in users)
        {
            await _userRepository.AddAsync(user);
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _userRepository.FindAsync(u => u.Name != null && u.Name.Contains("Alice"));

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task FirstOrDefaultAsync_ShouldReturnFirstMatchingEntity()
    {
        // Arrange
        var user = new User { PhoneNumber = "5555555555", Name = "Test" };
        await _userRepository.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userRepository.FirstOrDefaultAsync(u => u.PhoneNumber == "5555555555");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateEntity()
    {
        // Arrange
        var user = new User { PhoneNumber = "6666666666", Name = "Original" };
        await _userRepository.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        user.Name = "Updated";
        await _userRepository.UpdateAsync(user);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _userRepository.GetByIdAsync(user.Id);
        Assert.NotNull(updated);
        Assert.Equal("Updated", updated.Name);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveEntity()
    {
        // Arrange
        var user = new User { PhoneNumber = "7777777777", Name = "To Delete" };
        await _userRepository.AddAsync(user);
        await _context.SaveChangesAsync();
        var userId = user.Id;

        // Act
        await _userRepository.DeleteAsync(user);
        await _context.SaveChangesAsync();

        // Assert
        var deleted = await _userRepository.GetByIdAsync(userId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task CountAsync_ShouldReturnCount()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            await _userRepository.AddAsync(new User { PhoneNumber = $"888888888{i}", Name = $"User {i}" });
        }
        await _context.SaveChangesAsync();

        // Act
        var count = await _userRepository.CountAsync();
        var conditionalCount = await _userRepository.CountAsync(u => u.Name != null && u.Name.Contains("User"));

        // Assert
        Assert.Equal(5, count);
        Assert.Equal(5, conditionalCount);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrueIfExists()
    {
        // Arrange
        var user = new User { PhoneNumber = "9999999999", Name = "Exists" };
        await _userRepository.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var exists = await _userRepository.ExistsAsync(u => u.PhoneNumber == "9999999999");
        var notExists = await _userRepository.ExistsAsync(u => u.PhoneNumber == "0000000000");

        // Assert
        Assert.True(exists);
        Assert.False(notExists);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
