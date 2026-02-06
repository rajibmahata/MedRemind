using MedRemind.Core.DTOs;
using MedRemind.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MedRemind.API.Controllers;

/// <summary>
/// Controller for user registration and profile management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        UserService userService,
        ILogger<UsersController> logger)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    /// <param name="request">User registration details</param>
    /// <returns>Registration response with user ID</returns>
    /// <response code="200">User registered successfully</response>
    /// <response code="400">Invalid request or user already exists</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserRegistrationResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<UserRegistrationResponse>> Register([FromBody] UserRegistrationRequest request)
    {
        try
        {
            _logger.LogInformation("?? Registration request received for phone: {PhoneNumber}", request.PhoneNumber);

            if (request == null)
            {
                return BadRequest(new UserRegistrationResponse
                {
                    Success = false,
                    ErrorMessage = "Request body is required"
                });
            }

            var result = await _userService.RegisterUserAsync(request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            // Send post-registration OTP for verification
            if (result.UserId.HasValue)
            {
                var senderInfo = $"{HttpContext.Connection.RemoteIpAddress}";
                
                // Send OTP
                var otpResult = await _userService.SendPostRegistrationOtpAsync(result.UserId.Value, senderInfo);
                
                if (!otpResult.Success)
                {
                    _logger.LogWarning("?? Registration successful but OTP sending failed: {Error}", otpResult.ErrorMessage);
                    // Don't fail registration, just log the warning
                }
                else
                {
                    _logger.LogInformation("? Post-registration OTP sent for user {UserId}", result.UserId);
                }

                // Send welcome email
                var welcomeResult = await _userService.SendWelcomeEmailAsync(result.UserId.Value);
                
                if (!welcomeResult.Success)
                {
                    _logger.LogWarning("?? Registration successful but welcome email failed: {Error}", welcomeResult.ErrorMessage);
                    // Don't fail registration, just log the warning
                }
                else
                {
                    _logger.LogInformation("? Welcome email sent to user {UserId}", result.UserId);
                }
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Error processing registration request");
            return StatusCode(500, new UserRegistrationResponse
            {
                Success = false,
                ErrorMessage = "Internal server error"
            });
        }
    }

    /// <summary>
    /// Get user profile by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User profile data</returns>
    /// <response code="200">User profile retrieved successfully</response>
    /// <response code="404">User not found</response>
    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileData), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<UserProfileData>> GetUserById(int id)
    {
        try
        {
            _logger.LogInformation("?? Getting user profile for ID: {UserId}", id);

            // Validate user can only access their own profile (unless admin)
            var authenticatedUserId = GetAuthenticatedUserId();
            if (authenticatedUserId != id)
            {
                _logger.LogWarning("?? User {AuthUserId} attempted to access profile {UserId}", 
                    authenticatedUserId, id);
                return Forbid();
            }

            var profile = await _userService.GetUserByIdAsync(id);

            if (profile == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Error getting user profile");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get current authenticated user's profile
    /// </summary>
    /// <returns>User profile data</returns>
    /// <response code="200">User profile retrieved successfully</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="404">User not found</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileData), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<UserProfileData>> GetCurrentUser()
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            _logger.LogInformation("?? Getting profile for current user: {UserId}", userId);

            var profile = await _userService.GetUserByIdAsync(userId);

            if (profile == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Error getting current user profile");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update user profile
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">Profile update data</param>
    /// <returns>Success status</returns>
    /// <response code="200">Profile updated successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="404">User not found</response>
    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateProfile(int id, [FromBody] UserProfileUpdateRequest request)
    {
        try
        {
            _logger.LogInformation("?? Updating profile for user: {UserId}", id);

            // Validate user can only update their own profile
            var authenticatedUserId = GetAuthenticatedUserId();
            if (authenticatedUserId != id)
            {
                _logger.LogWarning("?? User {AuthUserId} attempted to update profile {UserId}", 
                    authenticatedUserId, id);
                return Forbid();
            }

            if (request == null)
            {
                return BadRequest(new { message = "Request body is required" });
            }

            var (success, errorMessage) = await _userService.UpdateUserProfileAsync(id, request);

            if (!success)
            {
                return BadRequest(new { message = errorMessage });
            }

            return Ok(new { message = "Profile updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Error updating user profile");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete user account
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Success status</returns>
    /// <response code="200">User deleted successfully</response>
    /// <response code="404">User not found</response>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteUser(int id)
    {
        try
        {
            _logger.LogInformation("??? Deleting user: {UserId}", id);

            // Validate user can only delete their own account
            var authenticatedUserId = GetAuthenticatedUserId();
            if (authenticatedUserId != id)
            {
                _logger.LogWarning("?? User {AuthUserId} attempted to delete account {UserId}", 
                    authenticatedUserId, id);
                return Forbid();
            }

            var (success, errorMessage) = await _userService.DeleteUserAsync(id);

            if (!success)
            {
                return NotFound(new { message = errorMessage });
            }

            return Ok(new { message = "User deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Error deleting user");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Check if user exists by phone number
    /// </summary>
    /// <param name="phoneNumber">Phone number to check</param>
    /// <returns>Existence status</returns>
    /// <response code="200">Check completed</response>
    [HttpGet("exists/{phoneNumber}")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<ActionResult<object>> CheckUserExists(string phoneNumber)
    {
        try
        {
            _logger.LogInformation("?? Checking if user exists: {PhoneNumber}", phoneNumber);

            var exists = await _userService.UserExistsAsync(phoneNumber);

            return Ok(new { exists });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Error checking user existence");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get authenticated user ID from JWT claims
    /// </summary>
    private int GetAuthenticatedUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                       ?? User.FindFirst("sub")?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }

        return userId;
    }
}
