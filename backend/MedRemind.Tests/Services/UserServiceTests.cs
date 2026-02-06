using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MedRemind.Core.Data;
using MedRemind.Core.DTOs;
using MedRemind.Core.Models;
using MedRemind.Core.Repositories;
using MedRemind.Services.Communication;
using MedRemind.Services.Users;
using Xunit;

namespace MedRemind.Tests.Services;

/// <summary>
/// Comprehensive tests for UserService including registration, OTP tracking, and profile management
/// </summary>
public class UserServiceTests : IDisposable
{
    private readonly MedRemindDbContext _context;
    private readonly UnitOfWork _unitOfWork;
    private readonly Mock<OtpCodeService> _mockOtpService;
    private readonly Mock<EmailService> _mockEmailService;
    private readonly Mock<ILogger<UserService>> _mockLogger;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<MedRemindDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new MedRemindDbContext(options);
        _unitOfWork = new UnitOfWork(_context);

        // Mock OTP Service
        _mockOtpService = new Mock<OtpCodeService>(
            _unitOfWork, null, null, true, false, null);
        _mockOtpService.Setup(s => s.GenerateAndSendOtpAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null));

        // Mock Email Service
        _mockEmailService = new Mock<EmailService>(
            "smtp.test.com", 587, "user", "pass", "from@test.com", "Test", true, null);
        _mockEmailService.Setup(e => e.SendWelcomeEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null));

        _mockLogger = new Mock<ILogger<UserService>>();

        _userService = new UserService(
            _unitOfWork,
            _mockOtpService.Object,
            _mockEmailService.Object,
            _mockLogger.Object);
    }

    #region Registration Tests

    [Fact]
    public async Task RegisterUserAsync_WithValidData_ShouldCreateUser()
    {
        // Arrange
        var request = new UserRegistrationRequest
        {
            PhoneNumber = "8420249020",
            Email = "test@example.com",
            Name = "Test User",
            DateOfBirth = new DateTime(1990, 1, 1),
            Gender = "Male"
        };

        // Act
        var result = await _userService.RegisterUserAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.UserId);
        Assert.NotNull(result.Profile);
        Assert.Equal(request.Email, result.Profile.Email);
        Assert.Equal(request.PhoneNumber, result.Profile.PhoneNumber);
    }

    [Fact]
    public async Task RegisterUserAsync_WithPassword_ShouldHashPassword()
    {
        // Arrange
        var request = new UserRegistrationRequest
        {
            PhoneNumber = "8420249020",
            Email = "test@example.com",
            Name = "Test User",
            Password = "Test123!@#"
        };

        // Act
        var result = await _userService.RegisterUserAsync(request);

        // Assert
        Assert.True(result.Success);
        
        // Verify password was hashed
        var userRepo = _unitOfWork.Repository<User>();
        var user = await userRepo.GetByIdAsync(result.UserId!.Value);
        Assert.NotNull(user);
        Assert.NotNull(user.PasswordHash);
        Assert.NotEqual("Test123!@#", user.PasswordHash); // Should be hashed
    }

    [Fact]
    public async Task RegisterUserAsync_WithWeakPassword_ShouldReturnError()
    {
        // Arrange
        var request = new UserRegistrationRequest
        {
            PhoneNumber = "8420249020",
            Email = "test@example.com",
            Name = "Test User",
            Password = "weak" // Doesn't meet requirements
        };

        // Act
        var result = await _userService.RegisterUserAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("password", result.ErrorMessage.ToLower());
    }

    [Fact]
    public async Task RegisterUserAsync_WithDuplicatePhone_ShouldReturnError()
    {
        // Arrange
        var request1 = new UserRegistrationRequest
        {
            PhoneNumber = "8420249020",
            Email = "test1@example.com",
            Name = "Test User 1"
        };

        var request2 = new UserRegistrationRequest
        {
            PhoneNumber = "8420249020", // Same phone
            Email = "test2@example.com",
            Name = "Test User 2"
        };

        // Act
        await _userService.RegisterUserAsync(request1);
        var result2 = await _userService.RegisterUserAsync(request2);

        // Assert
        Assert.False(result2.Success);
        Assert.Contains("already registered", result2.ErrorMessage.ToLower());
    }

    [Fact]
    public async Task RegisterUserAsync_WithInvalidEmail_ShouldReturnError()
    {
        // Arrange
        var request = new UserRegistrationRequest
        {
            PhoneNumber = "8420249020",
            Email = "invalid-email", // Invalid format
            Name = "Test User"
        };

        // Act
        var result = await _userService.RegisterUserAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("email", result.ErrorMessage.ToLower());
    }

    [Fact]
    public async Task RegisterUserAsync_WithInvalidPhoneNumber_ShouldReturnError()
    {
        // Arrange
        var request = new UserRegistrationRequest
        {
            PhoneNumber = "123", // Too short
            Email = "test@example.com",
            Name = "Test User"
        };

        // Act
        var result = await _userService.RegisterUserAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("phone", result.ErrorMessage.ToLower());
    }

    #endregion

    #region Get User Tests

    [Fact]
    public async Task GetUserByIdAsync_WithValidId_ShouldReturnUser()
    {
        // Arrange
        var userId = await CreateTestUserAsync();

        // Act
        var user = await _userService.GetUserByIdAsync(userId);

        // Assert
        Assert.NotNull(user);
        Assert.Equal(userId, user.Id);
    }

    [Fact]
    public async Task GetUserByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var user = await _userService.GetUserByIdAsync(999);

        // Assert
        Assert.Null(user);
    }

    [Fact]
    public async Task GetUserByPhoneNumberAsync_WithValidPhone_ShouldReturnUser()
    {
        // Arrange
        var phoneNumber = "8420249020";
        await CreateTestUserAsync(phoneNumber: phoneNumber);

        // Act
        var user = await _userService.GetUserByPhoneNumberAsync(phoneNumber);

        // Assert
        Assert.NotNull(user);
        Assert.Equal(phoneNumber, user.PhoneNumber);
    }

    [Fact]
    public async Task GetUserByIdAsync_WithOtpTracking_ShouldIncludeOtpFlags()
    {
        // Arrange
        var userId = await CreateTestUserAsync();
        
        // Send OTP to update flags
        await _userService.SendPostRegistrationOtpAsync(userId, "TestSender");

        // Act
        var user = await _userService.GetUserByIdAsync(userId);

        // Assert
        Assert.NotNull(user);
        Assert.True(user.IsEmailOtpSent || user.IsSmsOtpSent);
    }

    #endregion

    #region Update User Tests

    [Fact]
    public async Task UpdateUserProfileAsync_WithValidData_ShouldUpdateUser()
    {
        // Arrange
        var userId = await CreateTestUserAsync();
        var updateRequest = new UserProfileUpdateRequest
        {
            Name = "Updated Name",
            Email = "updated@example.com"
        };

        // Act
        var result = await _userService.UpdateUserProfileAsync(userId, updateRequest);

        // Assert
        Assert.True(result.Success);
        
        // Verify update
        var user = await _userService.GetUserByIdAsync(userId);
        Assert.Equal("Updated Name", user!.Name);
        Assert.Equal("updated@example.com", user.Email);
    }

    [Fact]
    public async Task UpdateUserProfileAsync_WithDuplicateEmail_ShouldReturnError()
    {
        // Arrange
        var user1Id = await CreateTestUserAsync(email: "user1@example.com");
        var user2Id = await CreateTestUserAsync(phoneNumber: "8420249021", email: "user2@example.com");

        var updateRequest = new UserProfileUpdateRequest
        {
            Email = "user2@example.com" // Try to use user2's email
        };

        // Act
        var result = await _userService.UpdateUserProfileAsync(user1Id, updateRequest);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already exists", result.ErrorMessage.ToLower());
    }

    #endregion

    #region Delete User Tests

    [Fact]
    public async Task DeleteUserAsync_WithValidId_ShouldDeleteUser()
    {
        // Arrange
        var userId = await CreateTestUserAsync();

        // Act
        var result = await _userService.DeleteUserAsync(userId);

        // Assert
        Assert.True(result.Success);
        
        // Verify deletion
        var user = await _userService.GetUserByIdAsync(userId);
        Assert.Null(user);
    }

    [Fact]
    public async Task DeleteUserAsync_WithInvalidId_ShouldReturnError()
    {
        // Act
        var result = await _userService.DeleteUserAsync(999);

        // Assert
        Assert.False(result.Success);
    }

    #endregion

    #region OTP and Email Tests

    [Fact]
    public async Task SendPostRegistrationOtpAsync_ShouldCallOtpService()
    {
        // Arrange
        var userId = await CreateTestUserAsync();

        // Act
        var result = await _userService.SendPostRegistrationOtpAsync(userId, "TestSender");

        // Assert
        Assert.True(result.Success);
        _mockOtpService.Verify(s => s.GenerateAndSendOtpAsync(
            It.IsAny<string>(), It.IsAny<string>(), "Registration",
            userId, "TestSender", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_ShouldCallEmailService()
    {
        // Arrange
        var userId = await CreateTestUserAsync();

        // Act
        var result = await _userService.SendWelcomeEmailAsync(userId);

        // Assert
        Assert.True(result.Success);
        _mockEmailService.Verify(e => e.SendWelcomeEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Helper Methods

    private async Task<int> CreateTestUserAsync(
        string phoneNumber = "8420249020",
        string email = "test@example.com")
    {
        var user = new User
        {
            PhoneNumber = phoneNumber,
            Email = email,
            Name = "Test User",
            CreatedAt = DateTime.UtcNow,
            IsEmailVerified = false,
            IsPhoneVerified = false,
            IsEmailOtpSent = false,
            IsSmsOtpSent = false
        };

        var userRepo = _unitOfWork.Repository<User>();
        await userRepo.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return user.Id;
    }

    #endregion

    public void Dispose()
    {
        _context?.Dispose();
        _unitOfWork?.Dispose();
    }
}
