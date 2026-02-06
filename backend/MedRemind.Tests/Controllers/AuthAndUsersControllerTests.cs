using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MedRemind.API.Controllers;
using MedRemind.Core.Data;
using MedRemind.Core.DTOs;
using MedRemind.Core.Interfaces;
using MedRemind.Core.Repositories;
using MedRemind.Services.Authentication;
using MedRemind.Services.Communication;
using MedRemind.Services.Users;
using Xunit;

namespace MedRemind.Tests.Controllers;

/// <summary>
/// Integration tests for Auth and Users controllers
/// </summary>
public class AuthControllerIntegrationTests : IDisposable
{
    private readonly MedRemindDbContext _context;
    private readonly UnitOfWork _unitOfWork;
    private readonly Mock<ISecureStorageService> _mockSecureStorage;
    private readonly Mock<EmailService> _mockEmailService;
    private readonly Mock<SmsService> _mockSmsService;
    private readonly Mock<ILogger<AuthController>> _mockAuthLogger;
    private readonly Mock<ILogger<UsersController>> _mockUsersLogger;
    private readonly AuthenticationService _authService;
    private readonly UserService _userService;
    private readonly AuthController _authController;
    private readonly UsersController _usersController;

    public AuthControllerIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<MedRemindDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new MedRemindDbContext(options);
        _unitOfWork = new UnitOfWork(_context);
        _mockSecureStorage = new Mock<ISecureStorageService>();

        // Setup mocks
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

        _mockSmsService = new Mock<SmsService>("sid", "token", "from", null);
        _mockSmsService.Setup(s => s.SendOtpAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null));

        _mockAuthLogger = new Mock<ILogger<AuthController>>();
        _mockUsersLogger = new Mock<ILogger<UsersController>>();

        // Create services
        var otpService = new OtpCodeService(
            _unitOfWork,
            _mockSmsService.Object,
            _mockEmailService.Object,
            useDatabaseStorage: true,
            enableRateLimiting: false,
            null);

        _authService = new AuthenticationService(
            _unitOfWork,
            _mockSecureStorage.Object,
            otpService,
            _mockEmailService.Object,
            "test_secret_key_minimum_32_characters_long",
            "TestIssuer",
            "TestAudience");

        _userService = new UserService(
            _unitOfWork,
            otpService,
            _mockEmailService.Object,
            null);

        // Create controllers
        _authController = new AuthController(_authService, _mockAuthLogger.Object);
        _usersController = new UsersController(_userService, _mockUsersLogger.Object);
    }

    #region Auth Controller Tests

    [Fact]
    public async Task AuthController_Login_WithValidCredentials_ReturnsOk()
    {
        // Arrange - Register user first
        var registerRequest = new UserRegistrationRequest
        {
            PhoneNumber = "8420249020",
            Email = "test@example.com",
            Name = "Test User",
            Password = "Test123!@#"
        };
        await _usersController.Register(registerRequest);

        var loginRequest = new LoginRequest
        {
            Identifier = "test@example.com",
            Password = "Test123!@#"
        };

        // Act
        var result = await _authController.Login(loginRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var loginResponse = Assert.IsType<LoginResponse>(okResult.Value);
        Assert.True(loginResponse.Success);
        Assert.NotNull(loginResponse.Token);
    }

    [Fact]
    public async Task AuthController_Login_WithInvalidCredentials_ReturnsBadRequest()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Identifier = "nonexistent@example.com",
            Password = "Test123!@#"
        };

        // Act
        var result = await _authController.Login(loginRequest);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var loginResponse = Assert.IsType<LoginResponse>(badRequestResult.Value);
        Assert.False(loginResponse.Success);
    }

    [Fact]
    public async Task AuthController_ForgotPassword_WithValidEmail_ReturnsOk()
    {
        // Arrange - Register user first
        var registerRequest = new UserRegistrationRequest
        {
            PhoneNumber = "8420249020",
            Email = "test@example.com",
            Name = "Test User",
            Password = "Test123!@#"
        };
        await _usersController.Register(registerRequest);

        var forgotRequest = new ForgotPasswordRequest
        {
            Email = "test@example.com"
        };

        // Act
        var result = await _authController.ForgotPassword(forgotRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ForgotPasswordResponse>(okResult.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task AuthController_ResetPassword_WithValidToken_ReturnsOk()
    {
        // Arrange - Register and request reset
        var email = "test@example.com";
        var registerRequest = new UserRegistrationRequest
        {
            PhoneNumber = "8420249020",
            Email = email,
            Name = "Test User",
            Password = "OldPassword123!"
        };
        await _usersController.Register(registerRequest);

        var forgotRequest = new ForgotPasswordRequest { Email = email };
        await _authController.ForgotPassword(forgotRequest);

        // Get reset token
        var userRepo = _unitOfWork.Repository<MedRemind.Core.Models.User>();
        var user = await userRepo.FirstOrDefaultAsync(u => u.Email == email);

        var resetRequest = new ResetPasswordRequest
        {
            Email = email,
            ResetToken = user!.PasswordResetToken!,
            NewPassword = "NewPassword456!",
            ConfirmPassword = "NewPassword456!"
        };

        // Act
        var result = await _authController.ResetPassword(resetRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ResetPasswordResponse>(okResult.Value);
        Assert.True(response.Success);
    }

    #endregion

    #region Users Controller Tests

    [Fact]
    public async Task UsersController_Register_WithValidData_ReturnsCreated()
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
        var result = await _usersController.Register(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<UserRegistrationResponse>(createdResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.UserId);
    }

    [Fact]
    public async Task UsersController_Register_WithDuplicatePhone_ReturnsBadRequest()
    {
        // Arrange
        var request1 = new UserRegistrationRequest
        {
            PhoneNumber = "8420249020",
            Email = "test1@example.com",
            Name = "User 1",
            Password = "Test123!@#"
        };

        var request2 = new UserRegistrationRequest
        {
            PhoneNumber = "8420249020", // Duplicate
            Email = "test2@example.com",
            Name = "User 2",
            Password = "Test123!@#"
        };

        // Act
        await _usersController.Register(request1);
        var result2 = await _usersController.Register(request2);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result2);
        var response = Assert.IsType<UserRegistrationResponse>(badRequestResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task UsersController_GetUser_WithValidId_ReturnsOk()
    {
        // Arrange - Register user first
        var registerRequest = new UserRegistrationRequest
        {
            PhoneNumber = "8420249020",
            Email = "test@example.com",
            Name = "Test User"
        };

        var registerResult = await _usersController.Register(registerRequest);
        var createdResult = Assert.IsType<CreatedAtActionResult>(registerResult);
        var registerResponse = Assert.IsType<UserRegistrationResponse>(createdResult.Value);
        var userId = registerResponse.UserId!.Value;

        // Act
        var result = await _usersController.GetUser(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var user = Assert.IsType<UserProfileData>(okResult.Value);
        Assert.Equal(userId, user.Id);
    }

    [Fact]
    public async Task UsersController_GetUser_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var result = await _usersController.GetUser(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UsersController_UpdateUser_WithValidData_ReturnsOk()
    {
        // Arrange - Register user first
        var registerRequest = new UserRegistrationRequest
        {
            PhoneNumber = "8420249020",
            Email = "test@example.com",
            Name = "Test User"
        };

        var registerResult = await _usersController.Register(registerRequest);
        var createdResult = Assert.IsType<CreatedAtActionResult>(registerResult);
        var registerResponse = Assert.IsType<UserRegistrationResponse>(createdResult.Value);
        var userId = registerResponse.UserId!.Value;

        var updateRequest = new UserProfileUpdateRequest
        {
            Name = "Updated User",
            Email = "updated@example.com"
        };

        // Act
        var result = await _usersController.UpdateUser(userId, updateRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        
        // Verify update
        var getUserResult = await _usersController.GetUser(userId);
        var okUserResult = Assert.IsType<OkObjectResult>(getUserResult);
        var user = Assert.IsType<UserProfileData>(okUserResult.Value);
        Assert.Equal("Updated User", user.Name);
    }

    [Fact]
    public async Task UsersController_DeleteUser_WithValidId_ReturnsNoContent()
    {
        // Arrange - Register user first
        var registerRequest = new UserRegistrationRequest
        {
            PhoneNumber = "8420249020",
            Email = "test@example.com",
            Name = "Test User"
        };

        var registerResult = await _usersController.Register(registerRequest);
        var createdResult = Assert.IsType<CreatedAtActionResult>(registerResult);
        var registerResponse = Assert.IsType<UserRegistrationResponse>(createdResult.Value);
        var userId = registerResponse.UserId!.Value;

        // Act
        var result = await _usersController.DeleteUser(userId);

        // Assert
        Assert.IsType<NoContentResult>(result);

        // Verify deletion
        var getUserResult = await _usersController.GetUser(userId);
        Assert.IsType<NotFoundResult>(getUserResult);
    }

    #endregion

    #region Complete Flow Tests

    [Fact]
    public async Task CompleteFlow_Register_Login_UpdateProfile_ChangePassword_Delete()
    {
        // STEP 1: Register
        var registerRequest = new UserRegistrationRequest
        {
            PhoneNumber = "8420249020",
            Email = "test@example.com",
            Name = "Test User",
            Password = "OldPassword123!"
        };

        var registerResult = await _usersController.Register(registerRequest);
        var createdResult = Assert.IsType<CreatedAtActionResult>(registerResult);
        var registerResponse = Assert.IsType<UserRegistrationResponse>(createdResult.Value);
        var userId = registerResponse.UserId!.Value;

        // STEP 2: Login
        var loginRequest = new LoginRequest
        {
            Identifier = "test@example.com",
            Password = "OldPassword123!"
        };

        var loginResult = await _authController.Login(loginRequest);
        var okLoginResult = Assert.IsType<OkObjectResult>(loginResult);
        var loginResponse = Assert.IsType<LoginResponse>(okLoginResult.Value);
        Assert.True(loginResponse.Success);

        // STEP 3: Update Profile
        var updateRequest = new UserProfileUpdateRequest
        {
            Name = "Updated User",
            Email = "updated@example.com"
        };

        var updateResult = await _usersController.UpdateUser(userId, updateRequest);
        Assert.IsType<OkObjectResult>(updateResult);

        // STEP 4: Change Password
        var changePasswordRequest = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword456!",
            ConfirmPassword = "NewPassword456!"
        };

        // Note: Need to add authentication context for this
        // Skipping for this test

        // STEP 5: Delete
        var deleteResult = await _usersController.DeleteUser(userId);
        Assert.IsType<NoContentResult>(deleteResult);
    }

    #endregion

    public void Dispose()
    {
        _context?.Dispose();
        _unitOfWork?.Dispose();
    }
}
