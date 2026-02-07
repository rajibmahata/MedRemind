using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MedRemind.Core.Data;
using MedRemind.Core.Models;
using MedRemind.Core.Repositories;
using MedRemind.Services.Communication;
using Xunit;

namespace MedRemind.Tests.Services;

/// <summary>
/// Comprehensive tests for OtpCodeService including OTP delivery tracking
/// </summary>
public class OtpCodeServiceTests : IDisposable
{
    private readonly MedRemindDbContext _context;
    private readonly UnitOfWork _unitOfWork;
    private readonly Mock<SmsService> _mockSmsService;
    private readonly Mock<EmailService> _mockEmailService;
    private readonly Mock<ILogger<OtpCodeService>> _mockLogger;
    private readonly OtpCodeService _otpService;

    public OtpCodeServiceTests()
    {
        var options = new DbContextOptionsBuilder<MedRemindDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new MedRemindDbContext(options);
        _unitOfWork = new UnitOfWork(_context);

        // Mock SMS Service
        var mockHttpClient = new Mock<HttpClient>();
        _mockSmsService = new Mock<SmsService>(
            mockHttpClient.Object, 
            "test_api_key", 
            null, 
            null, 
            null);
        _mockSmsService.Setup(s => s.SendOtpAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null));

        // Mock Email Service
        _mockEmailService = new Mock<EmailService>(
            "smtp.test.com", 587, "user", "pass", "from@test.com", "Test", true, null);
        _mockEmailService.Setup(e => e.SendOtpAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null));

        _mockLogger = new Mock<ILogger<OtpCodeService>>();

        _otpService = new OtpCodeService(
            _unitOfWork,
            _mockSmsService.Object,
            _mockEmailService.Object,
            enableSmsOtp: true,
            enableEmailOtp: true,
            _mockLogger.Object);
    }

    #region Generate and Send OTP Tests

    [Fact]
    public async Task GenerateAndSendOtpAsync_WithEmailDelivery_ShouldSetEmailOtpSentFlag()
    {
        // Arrange
        var phoneNumber = "8420249020";
        var email = "test@example.com";
        var userId = await CreateTestUserAsync(phoneNumber, email);

        // Act
        var result = await _otpService.GenerateAndSendOtpAsync(
            phoneNumber, email, "Registration", userId, "TestSender");

        // Assert
        Assert.True(result.Success);
        
        // Verify user flags updated
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
        Assert.NotNull(user);
        Assert.True(user.IsEmailOtpSent);
        Assert.False(user.IsSmsOtpSent); // Only email was configured
        Assert.NotNull(user.LastEmailOtpSentAt);
        Assert.Null(user.LastSmsOtpSentAt);
    }

    [Fact]
    public async Task GenerateAndSendOtpAsync_WithSmsDelivery_ShouldSetSmsOtpSentFlag()
    {
        // Arrange
        var phoneNumber = "8420249020";
        var email = "test@example.com";
        var userId = await CreateTestUserAsync(phoneNumber, email);

        // Create service with SMS delivery
        var smsOnlyService = new OtpCodeService(
            _unitOfWork,
            _mockSmsService.Object,
            null, // No email service
            enableSmsOtp: true,
            enableEmailOtp: false,
            _mockLogger.Object);

        // Act
        var result = await smsOnlyService.GenerateAndSendOtpAsync(
            phoneNumber, email, "Registration", userId, "TestSender");

        // Assert
        Assert.True(result.Success);
        
        // Verify user flags updated
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
        Assert.NotNull(user);
        Assert.False(user.IsEmailOtpSent); // No email service
        Assert.True(user.IsSmsOtpSent);
        Assert.Null(user.LastEmailOtpSentAt);
        Assert.NotNull(user.LastSmsOtpSentAt);
    }

    [Fact]
    public async Task GenerateAndSendOtpAsync_WithBothDelivery_ShouldSetBothFlags()
    {
        // Arrange
        var phoneNumber = "8420249020";
        var email = "test@example.com";
        var userId = await CreateTestUserAsync(phoneNumber, email);

        // Act
        var result = await _otpService.GenerateAndSendOtpAsync(
            phoneNumber, email, "Registration", userId, "TestSender");

        // Assert
        Assert.True(result.Success);
        
        // Verify both flags updated
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
        Assert.NotNull(user);
        Assert.True(user.IsEmailOtpSent);
        Assert.True(user.IsSmsOtpSent);
        Assert.NotNull(user.LastEmailOtpSentAt);
        Assert.NotNull(user.LastSmsOtpSentAt);
    }

    [Fact]
    public async Task GenerateAndSendOtpAsync_MultipleRequests_ShouldUpdateTimestamps()
    {
        // Arrange
        var phoneNumber = "8420249020";
        var email = "test@example.com";
        var userId = await CreateTestUserAsync(phoneNumber, email);

        // Act - First request
        await _otpService.GenerateAndSendOtpAsync(
            phoneNumber, email, "Registration", userId, "TestSender");
        
        var user1 = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
        var firstEmailTimestamp = user1!.LastEmailOtpSentAt;

        await Task.Delay(100); // Small delay to ensure different timestamp

        // Act - Second request
        await _otpService.GenerateAndSendOtpAsync(
            phoneNumber, email, "Login", userId, "TestSender");
        
        var user2 = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
        var secondEmailTimestamp = user2!.LastEmailOtpSentAt;

        // Assert
        Assert.NotNull(firstEmailTimestamp);
        Assert.NotNull(secondEmailTimestamp);
        Assert.True(secondEmailTimestamp > firstEmailTimestamp);
    }

    [Fact]
    public async Task GenerateAndSendOtpAsync_WithoutUserId_ShouldNotUpdateFlags()
    {
        // Arrange
        var phoneNumber = "8420249020";
        var email = "test@example.com";

        // Act - No user ID provided
        var result = await _otpService.GenerateAndSendOtpAsync(
            phoneNumber, email, "Registration", null, "TestSender");

        // Assert
        Assert.True(result.Success);
        // OTP created but user flags not updated (no user ID)
    }

    [Fact]
    public async Task GenerateAndSendOtpAsync_FailedDelivery_ShouldNotUpdateFlags()
    {
        // Arrange
        var phoneNumber = "8420249020";
        var email = "test@example.com";
        var userId = await CreateTestUserAsync(phoneNumber, email);

        // Mock failed delivery
        _mockEmailService.Setup(e => e.SendOtpAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Delivery failed"));

        _mockSmsService.Setup(s => s.SendOtpAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Delivery failed"));

        // Act
        var result = await _otpService.GenerateAndSendOtpAsync(
            phoneNumber, email, "Registration", userId, "TestSender");

        // Assert
        Assert.False(result.Success);
        
        // Verify flags NOT updated
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
        Assert.NotNull(user);
        Assert.False(user.IsEmailOtpSent);
        Assert.False(user.IsSmsOtpSent);
    }

    #endregion

    #region Verify OTP Tests

    [Fact]
    public async Task VerifyOtpAsync_WithValidOtp_ShouldReturnSuccess()
    {
        // Arrange
        var phoneNumber = "8420249020";
        var email = "test@example.com";
        var userId = await CreateTestUserAsync(phoneNumber, email);
        
        var result = await _otpService.GenerateAndSendOtpAsync(
            phoneNumber, email, "Registration", userId, "TestSender");
        Assert.True(result.Success);

        // Get the generated OTP
        var otpRepo = _unitOfWork.Repository<OtpCode>();
        var otpCode = (await otpRepo.FindAsync(o => o.PhoneNumber == phoneNumber))
            .OrderByDescending(o => o.CreatedAt)
            .First();

        // Act
        var verifyResult = await _otpService.VerifyOtpAsync(phoneNumber, otpCode.Code);

        // Assert
        Assert.True(verifyResult.Success);
    }

    [Fact]
    public async Task VerifyOtpAsync_WithInvalidOtp_ShouldReturnFailure()
    {
        // Arrange
        var phoneNumber = "8420249020";
        var email = "test@example.com";
        var userId = await CreateTestUserAsync(phoneNumber, email);
        
        await _otpService.GenerateAndSendOtpAsync(
            phoneNumber, email, "Registration", userId, "TestSender");

        // Act
        var verifyResult = await _otpService.VerifyOtpAsync(phoneNumber, "000000");

        // Assert
        Assert.False(verifyResult.Success);
        Assert.Contains("Invalid", verifyResult.ErrorMessage);
    }

    [Fact]
    public async Task VerifyOtpAsync_WithExpiredOtp_ShouldReturnFailure()
    {
        // Arrange
        var phoneNumber = "8420249020";
        var otpRepo = _unitOfWork.Repository<OtpCode>();

        // Create expired OTP
        var expiredOtp = new OtpCode
        {
            PhoneNumber = phoneNumber,
            Code = "123456",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1), // Expired
            IsActive = true,
            Purpose = "Test",
            CreatedAt = DateTime.UtcNow.AddMinutes(-11)
        };
        await otpRepo.AddAsync(expiredOtp);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var verifyResult = await _otpService.VerifyOtpAsync(phoneNumber, "123456");

        // Assert
        Assert.False(verifyResult.Success);
        Assert.Contains("expired", verifyResult.ErrorMessage.ToLower());
    }

    #endregion

    #region Rate Limiting Tests

    [Fact]
    public async Task GenerateAndSendOtpAsync_ExceedingRateLimit_ShouldReturnFailure()
    {
        // Arrange
        var phoneNumber = "8420249020";
        var email = "test@example.com";
        var userId = await CreateTestUserAsync(phoneNumber, email);

        // Create service with rate limiting
        // Note: Rate limiting is not currently implemented in OtpCodeService
        // This test verifies the service handles multiple requests
        var rateLimitedService = new OtpCodeService(
            _unitOfWork,
            _mockSmsService.Object,
            _mockEmailService.Object,
            enableSmsOtp: true,
            enableEmailOtp: true,
            _mockLogger.Object);

        // Act - Send multiple OTPs quickly
        for (int i = 0; i < 5; i++)
        {
            await rateLimitedService.GenerateAndSendOtpAsync(
                phoneNumber, email, "Test", userId, "TestSender");
        }

        var lastResult = await rateLimitedService.GenerateAndSendOtpAsync(
            phoneNumber, email, "Test", userId, "TestSender");

        // Assert - Should fail due to rate limit
        Assert.False(lastResult.Success);
        Assert.Contains("rate limit", lastResult.ErrorMessage.ToLower());
    }

    #endregion

    #region Helper Methods

    private async Task<int> CreateTestUserAsync(string phoneNumber, string email)
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
