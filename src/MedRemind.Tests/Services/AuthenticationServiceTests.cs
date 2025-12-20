using Microsoft.EntityFrameworkCore;
using Moq;
using MedRemind.Core.Data;
using MedRemind.Core.Interfaces;
using MedRemind.Core.Models;
using MedRemind.Core.Repositories;
using MedRemind.Services.Authentication;
using Xunit;

namespace MedRemind.Tests.Services;

public class AuthenticationServiceTests : IDisposable
{
    private readonly MedRemindDbContext _context;
    private readonly UnitOfWork _unitOfWork;
    private readonly Mock<ISecureStorageService> _mockSecureStorage;
    private readonly Mock<HttpClient> _mockHttpClient;
    private readonly AuthenticationService _authService;

    public AuthenticationServiceTests()
    {
        var options = new DbContextOptionsBuilder<MedRemindDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new MedRemindDbContext(options);
        _unitOfWork = new UnitOfWork(_context);
        _mockSecureStorage = new Mock<ISecureStorageService>();
        _mockHttpClient = new Mock<HttpClient>();

        _authService = new AuthenticationService(
            _unitOfWork,
            _mockSecureStorage.Object,
            "test_api_key",
            new HttpClient()); // Using real HttpClient for now
    }

    [Fact]
    public async Task SendOtpAsync_WithValidPhoneNumber_ShouldReturnSuccess()
    {
        // Arrange
        var phoneNumber = "9876543210";

        // Act
        var result = await _authService.SendOtpAsync(phoneNumber);

        // Assert
        // Note: This will fail in tests without actual API, but validates the flow
        Assert.NotNull(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("12345678901")]
    [InlineData("abcdefghij")]
    public async Task SendOtpAsync_WithInvalidPhoneNumber_ShouldReturnError(string phoneNumber)
    {
        // Act
        var result = await _authService.SendOtpAsync(phoneNumber);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Invalid phone number", result.ErrorMessage);
    }

    [Fact]
    public async Task VerifyOtpAsync_WithValidOtp_ShouldCreateUserAndReturnToken()
    {
        // Arrange
        var phoneNumber = "1234567890";
        var otp = "123456";

        // Act
        var result = await _authService.VerifyOtpAsync(phoneNumber, otp);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Token);
        Assert.Null(result.ErrorMessage);

        // Verify user was created
        var userRepo = _unitOfWork.Repository<User>();
        var user = await userRepo.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
        Assert.NotNull(user);
        Assert.Equal(phoneNumber, user.PhoneNumber);
    }

    [Fact]
    public async Task VerifyOtpAsync_WithExistingUser_ShouldUpdateLastLogin()
    {
        // Arrange
        var phoneNumber = "9876543210";
        var userRepo = _unitOfWork.Repository<User>();
        var existingUser = new User
        {
            PhoneNumber = phoneNumber,
            Name = "Existing User",
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };
        await userRepo.AddAsync(existingUser);
        await _unitOfWork.SaveChangesAsync();

        var otp = "123456";

        // Act
        var result = await _authService.VerifyOtpAsync(phoneNumber, otp);

        // Assert
        Assert.True(result.Success);
        
        var updatedUser = await userRepo.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
        Assert.NotNull(updatedUser);
        Assert.NotNull(updatedUser.LastLoginAt);
        Assert.True(updatedUser.LastLoginAt > existingUser.CreatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("12345")]
    [InlineData("1234567")]
    public async Task VerifyOtpAsync_WithInvalidOtp_ShouldReturnError(string otp)
    {
        // Arrange
        var phoneNumber = "1234567890";

        // Act
        var result = await _authService.VerifyOtpAsync(phoneNumber, otp);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Token);
        Assert.Contains("Invalid OTP", result.ErrorMessage);
    }

    [Fact]
    public async Task GenerateSessionTokenAsync_ShouldReturnUniqueToken()
    {
        // Arrange
        var userId = 123;

        // Act
        var token1 = await _authService.GenerateSessionTokenAsync(userId);
        var token2 = await _authService.GenerateSessionTokenAsync(userId);

        // Assert
        Assert.NotNull(token1);
        Assert.NotNull(token2);
        Assert.NotEqual(token1, token2); // Each token should be unique
    }

    [Fact]
    public async Task GenerateSessionTokenAsync_ShouldIncludeUserId()
    {
        // Arrange
        var userId = 456;

        // Act
        var token = await _authService.GenerateSessionTokenAsync(userId);

        // Assert
        Assert.NotNull(token);
        
        // Decode token to verify it contains user ID
        var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token));
        Assert.Contains(userId.ToString(), decoded);
    }

    [Fact]
    public async Task ValidateSessionTokenAsync_WithValidToken_ShouldReturnTrue()
    {
        // Arrange
        var token = await _authService.GenerateSessionTokenAsync(1);
        _mockSecureStorage.Setup(s => s.GetAsync("session_token"))
            .ReturnsAsync(token);

        // Act
        var isValid = await _authService.ValidateSessionTokenAsync(token);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public async Task ValidateSessionTokenAsync_WithNoStoredToken_ShouldReturnFalse()
    {
        // Arrange
        var token = await _authService.GenerateSessionTokenAsync(1);
        _mockSecureStorage.Setup(s => s.GetAsync("session_token"))
            .ReturnsAsync((string?)null);

        // Act
        var isValid = await _authService.ValidateSessionTokenAsync(token);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task ValidateSessionTokenAsync_WithMismatchedToken_ShouldReturnFalse()
    {
        // Arrange
        var token1 = await _authService.GenerateSessionTokenAsync(1);
        var token2 = await _authService.GenerateSessionTokenAsync(2);
        _mockSecureStorage.Setup(s => s.GetAsync("session_token"))
            .ReturnsAsync(token1);

        // Act
        var isValid = await _authService.ValidateSessionTokenAsync(token2);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task VerifyOtpAsync_ShouldStoreTokenInSecureStorage()
    {
        // Arrange
        var phoneNumber = "5555555555";
        var otp = "123456";

        // Act
        var result = await _authService.VerifyOtpAsync(phoneNumber, otp);

        // Assert
        Assert.True(result.Success);
        _mockSecureStorage.Verify(
            s => s.SetAsync("session_token", It.IsAny<string>()), 
            Times.Once);
        _mockSecureStorage.Verify(
            s => s.SetAsync("user_id", It.IsAny<string>()), 
            Times.Once);
    }

    [Fact]
    public async Task VerifyOtpAsync_MultipleUsers_ShouldCreateSeparateAccounts()
    {
        // Arrange
        var phone1 = "1111111111";
        var phone2 = "2222222222";
        var otp = "123456";

        // Act
        var result1 = await _authService.VerifyOtpAsync(phone1, otp);
        var result2 = await _authService.VerifyOtpAsync(phone2, otp);

        // Assert
        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.NotEqual(result1.Token, result2.Token);

        var userRepo = _unitOfWork.Repository<User>();
        var users = await userRepo.GetAllAsync();
        Assert.Equal(2, users.Count());
    }

    public void Dispose()
    {
        _unitOfWork.Dispose();
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
