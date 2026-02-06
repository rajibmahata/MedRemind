using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MedRemind.Core.Data;
using MedRemind.Core.DTOs;
using MedRemind.Core.Interfaces;
using MedRemind.Core.Repositories;
using MedRemind.Services.Authentication;
using MedRemind.Services.Communication;
using MedRemind.Services.Users;
using Xunit;

namespace MedRemind.Tests.Integration;

/// <summary>
/// Integration tests for complete user flows including registration, OTP, login, and password reset
/// </summary>
public class UserFlowIntegrationTests : IDisposable
{
    private readonly MedRemindDbContext _context;
    private readonly UnitOfWork _unitOfWork;
    private readonly Mock<ISecureStorageService> _mockSecureStorage;
    private readonly Mock<EmailService> _mockEmailService;
    private readonly Mock<SmsService> _mockSmsService;
    private readonly OtpCodeService _otpService;
    private readonly UserService _userService;
    private readonly AuthenticationService _authService;

    public UserFlowIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<MedRemindDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new MedRemindDbContext(options);
        _unitOfWork = new UnitOfWork(_context);
        _mockSecureStorage = new Mock<ISecureStorageService>();

        // Setup Email Service Mock
        _mockEmailService = new Mock<EmailService>(
            "smtp.test.com", 587, "user", "pass", "from@test.com", "Test", true, null);
        _mockEmailService.Setup(e => e.SendOtpAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null));
        _mockEmailService.Setup(e => e.SendWelcomeEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null));
        _mockEmailService.Setup(e => e.SendPasswordResetEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null));

        // Setup SMS Service Mock
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

        // Create services
        _otpService = new OtpCodeService(
            _unitOfWork,
            _mockSmsService.Object,
            _mockEmailService.Object,
            enableSmsOtp: true,
            enableEmailOtp: true,
            null);

        _userService = new UserService(
            _unitOfWork,
            _otpService,
            _mockEmailService.Object,
            null);

        _authService = new AuthenticationService(
            _unitOfWork,
            _mockSecureStorage.Object,
            _otpService,
            _mockEmailService.Object,
            "test_secret_key_minimum_32_characters_long",
            "TestIssuer",
            "TestAudience");
    }

    #region Complete Registration and OTP Flow

    [Fact]
    public async Task CompleteFlow_RegisterWithPassword_SendOTP_VerifyOTP_Login()
    {
        // STEP 1: Register user with password
        var registerRequest = new UserRegistrationRequest
        {
            PhoneNumber = "8420249020",
            Email = "test@example.com",
            Name = "Test User",
            Password = "Test123!@#"
        };

        var registerResult = await _userService.RegisterUserAsync(registerRequest);
        Assert.True(registerResult.Success);
        Assert.NotNull(registerResult.UserId);

        // STEP 2: Send post-registration OTP
        var otpResult = await _userService.SendPostRegistrationOtpAsync(
            registerResult.UserId.Value, "TestApp");
        Assert.True(otpResult.Success);

        // Verify OTP flags updated
        var user = await _userService.GetUserByIdAsync(registerResult.UserId.Value);
        Assert.NotNull(user);
        Assert.True(user.IsEmailOtpSent || user.IsSmsOtpSent);

        // STEP 3: Get OTP code from database
        var otpRepo = _unitOfWork.Repository<MedRemind.Core.Models.OtpCode>();
        var otpCode = (await otpRepo.FindAsync(o => 
            o.PhoneNumber == registerRequest.PhoneNumber && o.IsActive))
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefault();
        Assert.NotNull(otpCode);

        // STEP 4: Verify OTP
        var verifyResult = await _authService.VerifyOtpAsync(
            registerRequest.PhoneNumber, otpCode.Code);
        Assert.True(verifyResult.Success);
        Assert.NotNull(verifyResult.Token);

        // STEP 5: Login with password
        var loginRequest = new LoginRequest
        {
            Identifier = registerRequest.Email,
            Password = registerRequest.Password
        };

        var loginResult = await _authService.LoginWithPasswordAsync(loginRequest);
        Assert.True(loginResult.Success);
        Assert.NotNull(loginResult.Token);
        Assert.NotNull(loginResult.Profile);
    }

    #endregion

    #region Complete Password Reset Flow

    [Fact]
    public async Task CompleteFlow_ForgotPassword_ResetPassword_LoginWithNewPassword()
    {
        // STEP 1: Register user with password
        var email = "test@example.com";
        var oldPassword = "OldPassword123!";
        
        var registerRequest = new UserRegistrationRequest
        {
            PhoneNumber = "8420249020",
            Email = email,
            Name = "Test User",
            Password = oldPassword
        };

        var registerResult = await _userService.RegisterUserAsync(registerRequest);
        Assert.True(registerResult.Success);

        // STEP 2: Request password reset
        var forgotRequest = new ForgotPasswordRequest
        {
            Email = email
        };

        var forgotResult = await _authService.ForgotPasswordAsync(forgotRequest);
        Assert.True(forgotResult.Success);

        // Verify reset email would be sent
        _mockEmailService.Verify(e => e.SendPasswordResetEmailAsync(
            email, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);

        // STEP 3: Get reset token from database
        var userRepo = _unitOfWork.Repository<MedRemind.Core.Models.User>();
        var user = await userRepo.FirstOrDefaultAsync(u => u.Email == email);
        Assert.NotNull(user);
        Assert.NotNull(user.PasswordResetToken);

        // STEP 4: Reset password with token
        var newPassword = "NewPassword456!";
        var resetRequest = new ResetPasswordRequest
        {
            Email = email,
            ResetToken = user.PasswordResetToken,
            NewPassword = newPassword,
            ConfirmPassword = newPassword
        };

        var resetResult = await _authService.ResetPasswordAsync(resetRequest);
        Assert.True(resetResult.Success);

        // STEP 5: Verify old password doesn't work
        var oldLoginRequest = new LoginRequest
        {
            Identifier = email,
            Password = oldPassword
        };

        var oldLoginResult = await _authService.LoginWithPasswordAsync(oldLoginRequest);
        Assert.False(oldLoginResult.Success);

        // STEP 6: Verify new password works
        var newLoginRequest = new LoginRequest
        {
            Identifier = email,
            Password = newPassword
        };

        var newLoginResult = await _authService.LoginWithPasswordAsync(newLoginRequest);
        Assert.True(newLoginResult.Success);
        Assert.NotNull(newLoginResult.Token);
    }

    #endregion

    #region OTP Delivery Tracking Flow

    [Fact]
    public async Task CompleteFlow_RegisterUser_SendMultipleOTPs_TrackDeliveryFlags()
    {
        // STEP 1: Register user
        var registerRequest = new UserRegistrationRequest
        {
            PhoneNumber = "8420249020",
            Email = "test@example.com",
            Name = "Test User"
        };

        var registerResult = await _userService.RegisterUserAsync(registerRequest);
        Assert.True(registerResult.Success);
        var userId = registerResult.UserId.Value;

        // STEP 2: Verify initial state - no OTP sent
        var user1 = await _userService.GetUserByIdAsync(userId);
        Assert.NotNull(user1);
        Assert.False(user1.IsEmailOtpSent);
        Assert.False(user1.IsSmsOtpSent);
        Assert.Null(user1.LastEmailOtpSentAt);
        Assert.Null(user1.LastSmsOtpSentAt);

        // STEP 3: Send first OTP
        await _otpService.GenerateAndSendOtpAsync(
            registerRequest.PhoneNumber, 
            registerRequest.Email, 
            "Registration", 
            userId, 
            "TestApp");

        var user2 = await _userService.GetUserByIdAsync(userId);
        Assert.True(user2.IsEmailOtpSent || user2.IsSmsOtpSent);
        var firstTimestamp = user2.LastEmailOtpSentAt ?? user2.LastSmsOtpSentAt;
        Assert.NotNull(firstTimestamp);

        // STEP 4: Wait and send second OTP
        await Task.Delay(100);
        
        await _otpService.GenerateAndSendOtpAsync(
            registerRequest.PhoneNumber, 
            registerRequest.Email, 
            "Login", 
            userId, 
            "TestApp");

        var user3 = await _userService.GetUserByIdAsync(userId);
        var secondTimestamp = user3.LastEmailOtpSentAt ?? user3.LastSmsOtpSentAt;
        Assert.NotNull(secondTimestamp);
        Assert.True(secondTimestamp > firstTimestamp); // Timestamp updated
    }

    #endregion

    #region User Profile Management Flow

    [Fact]
    public async Task CompleteFlow_Register_Update_GetProfile_Delete()
    {
        // STEP 1: Register user
        var registerRequest = new UserRegistrationRequest
        {
            PhoneNumber = "8420249020",
            Email = "test@example.com",
            Name = "Test User",
            Password = "Test123!@#"
        };

        var registerResult = await _userService.RegisterUserAsync(registerRequest);
        Assert.True(registerResult.Success);
        var userId = registerResult.UserId.Value;

        // STEP 2: Get initial profile
        var profile1 = await _userService.GetUserByIdAsync(userId);
        Assert.NotNull(profile1);
        Assert.Equal("Test User", profile1.Name);

        // STEP 3: Update profile
        var updateRequest = new UserProfileUpdateRequest
        {
            Name = "Updated User",
            Email = "updated@example.com"
        };

        var updateResult = await _userService.UpdateUserProfileAsync(userId, updateRequest);
        Assert.True(updateResult.Success);

        // STEP 4: Verify update
        var profile2 = await _userService.GetUserByIdAsync(userId);
        Assert.NotNull(profile2);
        Assert.Equal("Updated User", profile2.Name);
        Assert.Equal("updated@example.com", profile2.Email);

        // STEP 5: Delete user
        var deleteResult = await _userService.DeleteUserAsync(userId);
        Assert.True(deleteResult.Success);

        // STEP 6: Verify deletion
        var profile3 = await _userService.GetUserByIdAsync(userId);
        Assert.Null(profile3);
    }

    #endregion

    #region Change Password Flow

    [Fact]
    public async Task CompleteFlow_Register_Login_ChangePassword_LoginWithNewPassword()
    {
        // STEP 1: Register with password
        var email = "test@example.com";
        var oldPassword = "OldPassword123!";
        
        var registerRequest = new UserRegistrationRequest
        {
            PhoneNumber = "8420249020",
            Email = email,
            Name = "Test User",
            Password = oldPassword
        };

        var registerResult = await _userService.RegisterUserAsync(registerRequest);
        Assert.True(registerResult.Success);
        var userId = registerResult.UserId.Value;

        // STEP 2: Login with old password
        var loginRequest1 = new LoginRequest
        {
            Identifier = email,
            Password = oldPassword
        };

        var loginResult1 = await _authService.LoginWithPasswordAsync(loginRequest1);
        Assert.True(loginResult1.Success);

        // STEP 3: Change password
        var newPassword = "NewPassword456!";
        var changeRequest = new ChangePasswordRequest
        {
            CurrentPassword = oldPassword,
            NewPassword = newPassword,
            ConfirmPassword = newPassword
        };

        var changeResult = await _authService.ChangePasswordAsync(userId, changeRequest);
        Assert.True(changeResult.Success);

        // STEP 4: Verify old password doesn't work
        var loginResult2 = await _authService.LoginWithPasswordAsync(loginRequest1);
        Assert.False(loginResult2.Success);

        // STEP 5: Login with new password
        var loginRequest2 = new LoginRequest
        {
            Identifier = email,
            Password = newPassword
        };

        var loginResult3 = await _authService.LoginWithPasswordAsync(loginRequest2);
        Assert.True(loginResult3.Success);
    }

    #endregion

    public void Dispose()
    {
        _context?.Dispose();
        _unitOfWork?.Dispose();
    }
}
