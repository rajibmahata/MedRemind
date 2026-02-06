using Microsoft.EntityFrameworkCore;
using Moq;
using MedRemind.Core.Data;
using MedRemind.Core.DTOs;
using MedRemind.Core.Enums;
using MedRemind.Core.Interfaces;
using MedRemind.Core.Models;
using MedRemind.Core.Repositories;
using MedRemind.Services.Authentication;
using MedRemind.Services.Communication;
using Xunit;

namespace MedRemind.Tests.Services;

/// <summary>
/// Comprehensive tests for Password Authentication including forgot/reset password
/// </summary>
public class PasswordAuthenticationTests : IDisposable
{
    private readonly MedRemindDbContext _context;
    private readonly UnitOfWork _unitOfWork;
    private readonly Mock<ISecureStorageService> _mockSecureStorage;
    private readonly Mock<EmailService> _mockEmailService;
    private readonly AuthenticationService _authService;

    public PasswordAuthenticationTests()
    {
        var options = new DbContextOptionsBuilder<MedRemindDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new MedRemindDbContext(options);
        _unitOfWork = new UnitOfWork(_context);
        _mockSecureStorage = new Mock<ISecureStorageService>();

        _mockEmailService = new Mock<EmailService>(
            "smtp.test.com", 587, "user", "pass", "from@test.com", "Test", true, null);
        _mockEmailService.Setup(e => e.SendPasswordResetEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null));

        _authService = new AuthenticationService(
            _unitOfWork,
            _mockSecureStorage.Object,
            otpService: null,
            emailService: _mockEmailService.Object,
            jwtSecretKey: "test_secret_key_minimum_32_characters_long",
            jwtIssuer: "TestIssuer",
            jwtAudience: "TestAudience");
    }

    #region Login with Password Tests

    [Fact]
    public async Task LoginWithPasswordAsync_WithValidCredentials_ShouldReturnSuccess()
    {
        // Arrange
        var email = "test@example.com";
        var password = "Test123!@#";
        await CreateUserWithPasswordAsync(email, password);

        var request = new LoginRequest
        {
            Identifier = email,
            Password = password
        };

        // Act
        var result = await _authService.LoginWithPasswordAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Token);
        Assert.NotNull(result.Profile);
        Assert.Equal(email, result.Profile.Email);
    }

    [Fact]
    public async Task LoginWithPasswordAsync_WithInvalidPassword_ShouldReturnError()
    {
        // Arrange
        var email = "test@example.com";
        await CreateUserWithPasswordAsync(email, "Test123!@#");

        var request = new LoginRequest
        {
            Identifier = email,
            Password = "WrongPassword123!"
        };

        // Act
        var result = await _authService.LoginWithPasswordAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Invalid credentials", result.ErrorMessage);
    }

    [Fact]
    public async Task LoginWithPasswordAsync_WithNonExistentUser_ShouldReturnError()
    {
        // Arrange
        var request = new LoginRequest
        {
            Identifier = "nonexistent@example.com",
            Password = "Test123!@#"
        };

        // Act
        var result = await _authService.LoginWithPasswordAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Invalid credentials", result.ErrorMessage);
    }

    [Fact]
    public async Task LoginWithPasswordAsync_WithUserWithoutPassword_ShouldReturnError()
    {
        // Arrange
        var email = "test@example.com";
        await CreateUserWithoutPasswordAsync(email);

        var request = new LoginRequest
        {
            Identifier = email,
            Password = "Test123!@#"
        };

        // Act
        var result = await _authService.LoginWithPasswordAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("No password set", result.ErrorMessage);
    }

    [Fact]
    public async Task LoginWithPasswordAsync_WithPhoneNumber_ShouldWork()
    {
        // Arrange
        var phoneNumber = "8420249020";
        var password = "Test123!@#";
        await CreateUserWithPasswordAsync("test@example.com", password, phoneNumber);

        var request = new LoginRequest
        {
            Identifier = phoneNumber,
            Password = password
        };

        // Act
        var result = await _authService.LoginWithPasswordAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Token);
    }

    #endregion

    #region Forgot Password Tests

    [Fact]
    public async Task ForgotPasswordAsync_WithValidEmail_ShouldSendResetEmail()
    {
        // Arrange
        var email = "test@example.com";
        await CreateUserWithPasswordAsync(email, "Test123!@#");

        var request = new ForgotPasswordRequest
        {
            Email = email
        };

        // Act
        var result = await _authService.ForgotPasswordAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("receive password reset instructions", result.Message);
        
        // Verify reset token was set
        var userRepo = _unitOfWork.Repository<User>();
        var user = await userRepo.FirstOrDefaultAsync(u => u.Email == email);
        Assert.NotNull(user);
        Assert.NotNull(user.PasswordResetToken);
        Assert.NotNull(user.PasswordResetTokenExpiry);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WithNonExistentEmail_ShouldReturnGenericMessage()
    {
        // Arrange
        var request = new ForgotPasswordRequest
        {
            Email = "nonexistent@example.com"
        };

        // Act
        var result = await _authService.ForgotPasswordAsync(request);

        // Assert
        Assert.True(result.Success); // Don't reveal if user exists
        Assert.Contains("receive password reset instructions", result.Message);
    }

    [Fact]
    public async Task ForgotPasswordAsync_ShouldSetTokenExpiry()
    {
        // Arrange
        var email = "test@example.com";
        await CreateUserWithPasswordAsync(email, "Test123!@#");

        var request = new ForgotPasswordRequest
        {
            Email = email
        };

        // Act
        var beforeRequest = DateTime.UtcNow;
        await _authService.ForgotPasswordAsync(request);
        var afterRequest = DateTime.UtcNow;

        // Assert
        var userRepo = _unitOfWork.Repository<User>();
        var user = await userRepo.FirstOrDefaultAsync(u => u.Email == email);
        Assert.NotNull(user);
        Assert.NotNull(user.PasswordResetTokenExpiry);
        
        // Token should expire in ~1 hour
        var expectedExpiry = beforeRequest.AddHours(1);
        Assert.True(user.PasswordResetTokenExpiry >= expectedExpiry);
        Assert.True(user.PasswordResetTokenExpiry <= afterRequest.AddHours(1));
    }

    #endregion

    #region Reset Password Tests

    [Fact]
    public async Task ResetPasswordAsync_WithValidToken_ShouldResetPassword()
    {
        // Arrange
        var email = "test@example.com";
        await CreateUserWithPasswordAsync(email, "OldPassword123!");
        
        // Request reset
        var forgotRequest = new ForgotPasswordRequest { Email = email };
        await _authService.ForgotPasswordAsync(forgotRequest);

        // Get reset token
        var userRepo = _unitOfWork.Repository<User>();
        var user = await userRepo.FirstOrDefaultAsync(u => u.Email == email);
        var resetToken = user!.PasswordResetToken;

        var resetRequest = new ResetPasswordRequest
        {
            Email = email,
            ResetToken = resetToken!,
            NewPassword = "NewPassword456!",
            ConfirmPassword = "NewPassword456!"
        };

        // Act
        var result = await _authService.ResetPasswordAsync(resetRequest);

        // Assert
        Assert.True(result.Success);
        
        // Verify can login with new password
        var loginRequest = new LoginRequest
        {
            Identifier = email,
            Password = "NewPassword456!"
        };
        var loginResult = await _authService.LoginWithPasswordAsync(loginRequest);
        Assert.True(loginResult.Success);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithInvalidToken_ShouldReturnError()
    {
        // Arrange
        var email = "test@example.com";
        await CreateUserWithPasswordAsync(email, "Test123!@#");

        var request = new ResetPasswordRequest
        {
            Email = email,
            ResetToken = "invalid-token",
            NewPassword = "NewPassword456!",
            ConfirmPassword = "NewPassword456!"
        };

        // Act
        var result = await _authService.ResetPasswordAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Invalid or expired reset token", result.ErrorMessage);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithExpiredToken_ShouldReturnError()
    {
        // Arrange
        var email = "test@example.com";
        var user = await CreateUserWithPasswordAsync(email, "Test123!@#");

        // Set expired token
        user.PasswordResetToken = "expired-token";
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(-1); // Expired
        await _unitOfWork.Repository<User>().UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var request = new ResetPasswordRequest
        {
            Email = email,
            ResetToken = "expired-token",
            NewPassword = "NewPassword456!",
            ConfirmPassword = "NewPassword456!"
        };

        // Act
        var result = await _authService.ResetPasswordAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("expired", result.ErrorMessage.ToLower());
    }

    [Fact]
    public async Task ResetPasswordAsync_WithMismatchedPasswords_ShouldReturnError()
    {
        // Arrange
        var email = "test@example.com";
        await CreateUserWithPasswordAsync(email, "Test123!@#");
        
        var forgotRequest = new ForgotPasswordRequest { Email = email };
        await _authService.ForgotPasswordAsync(forgotRequest);

        var userRepo = _unitOfWork.Repository<User>();
        var user = await userRepo.FirstOrDefaultAsync(u => u.Email == email);

        var request = new ResetPasswordRequest
        {
            Email = email,
            ResetToken = user!.PasswordResetToken!,
            NewPassword = "NewPassword456!",
            ConfirmPassword = "DifferentPassword789!"
        };

        // Act
        var result = await _authService.ResetPasswordAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("do not match", result.ErrorMessage);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithWeakPassword_ShouldReturnError()
    {
        // Arrange
        var email = "test@example.com";
        await CreateUserWithPasswordAsync(email, "Test123!@#");
        
        var forgotRequest = new ForgotPasswordRequest { Email = email };
        await _authService.ForgotPasswordAsync(forgotRequest);

        var userRepo = _unitOfWork.Repository<User>();
        var user = await userRepo.FirstOrDefaultAsync(u => u.Email == email);

        var request = new ResetPasswordRequest
        {
            Email = email,
            ResetToken = user!.PasswordResetToken!,
            NewPassword = "weak",
            ConfirmPassword = "weak"
        };

        // Act
        var result = await _authService.ResetPasswordAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("password", result.ErrorMessage.ToLower());
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldClearResetToken()
    {
        // Arrange
        var email = "test@example.com";
        await CreateUserWithPasswordAsync(email, "Test123!@#");
        
        var forgotRequest = new ForgotPasswordRequest { Email = email };
        await _authService.ForgotPasswordAsync(forgotRequest);

        var userRepo = _unitOfWork.Repository<User>();
        var user = await userRepo.FirstOrDefaultAsync(u => u.Email == email);

        var request = new ResetPasswordRequest
        {
            Email = email,
            ResetToken = user!.PasswordResetToken!,
            NewPassword = "NewPassword456!",
            ConfirmPassword = "NewPassword456!"
        };

        // Act
        await _authService.ResetPasswordAsync(request);

        // Assert
        var updatedUser = await userRepo.FirstOrDefaultAsync(u => u.Email == email);
        Assert.Null(updatedUser!.PasswordResetToken);
        Assert.Null(updatedUser.PasswordResetTokenExpiry);
        Assert.NotNull(updatedUser.LastPasswordChangeAt);
    }

    #endregion

    #region Change Password Tests

    [Fact]
    public async Task ChangePasswordAsync_WithValidCurrentPassword_ShouldChangePassword()
    {
        // Arrange
        var email = "test@example.com";
        var user = await CreateUserWithPasswordAsync(email, "OldPassword123!");

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword456!",
            ConfirmPassword = "NewPassword456!"
        };

        // Act
        var result = await _authService.ChangePasswordAsync(user.Id, request);

        // Assert
        Assert.True(result.Success);
        
        // Verify can login with new password
        var loginRequest = new LoginRequest
        {
            Identifier = email,
            Password = "NewPassword456!"
        };
        var loginResult = await _authService.LoginWithPasswordAsync(loginRequest);
        Assert.True(loginResult.Success);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithIncorrectCurrentPassword_ShouldReturnError()
    {
        // Arrange
        var user = await CreateUserWithPasswordAsync("test@example.com", "Test123!@#");

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "WrongPassword123!",
            NewPassword = "NewPassword456!",
            ConfirmPassword = "NewPassword456!"
        };

        // Act
        var result = await _authService.ChangePasswordAsync(user.Id, request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Current password is incorrect", result.ErrorMessage);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithMismatchedNewPasswords_ShouldReturnError()
    {
        // Arrange
        var user = await CreateUserWithPasswordAsync("test@example.com", "Test123!@#");

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "Test123!@#",
            NewPassword = "NewPassword456!",
            ConfirmPassword = "DifferentPassword789!"
        };

        // Act
        var result = await _authService.ChangePasswordAsync(user.Id, request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("do not match", result.ErrorMessage);
    }

    #endregion

    #region Helper Methods

    private async Task<User> CreateUserWithPasswordAsync(
        string email, 
        string password, 
        string phoneNumber = "8420249020")
    {
        var passwordService = new MedRemind.Core.Services.PasswordHashingService();
        
        var user = new User
        {
            PhoneNumber = phoneNumber,
            Email = email,
            Name = "Test User",
            PasswordHash = passwordService.HashPassword(password),
            CreatedAt = DateTime.UtcNow,
            IsEmailVerified = false,
            IsPhoneVerified = false,
            AuthenticationMethod = AuthenticationMethod.None,
            LastAuthenticationMethod = AuthenticationMethod.None
        };

        var userRepo = _unitOfWork.Repository<User>();
        await userRepo.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return user;
    }

    private async Task<User> CreateUserWithoutPasswordAsync(string email)
    {
        var user = new User
        {
            PhoneNumber = "8420249020",
            Email = email,
            Name = "Test User",
            PasswordHash = null, // No password
            CreatedAt = DateTime.UtcNow,
            IsEmailVerified = false,
            IsPhoneVerified = false,
            AuthenticationMethod = AuthenticationMethod.None,
            LastAuthenticationMethod = AuthenticationMethod.None
        };

        var userRepo = _unitOfWork.Repository<User>();
        await userRepo.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return user;
    }

    #endregion

    public void Dispose()
    {
        _context?.Dispose();
        _unitOfWork?.Dispose();
    }
}
