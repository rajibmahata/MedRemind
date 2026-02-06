using MedRemind.Core.DTOs;
using MedRemind.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace MedRemind.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthenticationService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Send OTP to phone number
    /// </summary>
    [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
    {
        try
        {
            var (success, errorMessage) = await _authService.SendOtpAsync(request.PhoneNumber);
            
            if (success)
            {
                return Ok(new { message = "OTP sent successfully", phoneNumber = request.PhoneNumber });
            }
            
            return BadRequest(new { message = errorMessage ?? "Failed to send OTP" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending OTP to {PhoneNumber}", request.PhoneNumber);
            return StatusCode(500, new { message = "An error occurred while sending OTP" });
        }
    }

    /// <summary>
    /// Verify OTP and login
    /// </summary>
    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        try
        {
            var (success, token, errorMessage) = await _authService.VerifyOtpAsync(request.PhoneNumber, request.Otp);
            
            if (!success)
            {
                return Unauthorized(new { message = errorMessage ?? "Invalid OTP" });
            }

            return Ok(new 
            { 
                message = "Login successful",
                phoneNumber = request.PhoneNumber,
                token = token
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying OTP for {PhoneNumber}", request.PhoneNumber);
            return StatusCode(500, new { message = "An error occurred during verification" });
        }
    }

    /// <summary>
    /// Validate session token
    /// </summary>
    [HttpPost("validate-token")]
    public async Task<IActionResult> ValidateToken([FromBody] ValidateTokenRequest request)
    {
        try
        {
            var isValid = await _authService.ValidateSessionTokenAsync(request.Token);
            
            if (isValid)
            {
                return Ok(new { valid = true });
            }
            
            return Unauthorized(new { valid = false, message = "Invalid or expired token" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return StatusCode(500, new { message = "An error occurred during validation" });
        }
    }

    /// <summary>
    /// Login with email/phone and password
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(typeof(LoginResponse), 400)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            _logger.LogInformation("Login attempt for: {Identifier}", request.Identifier);

            var result = await _authService.LoginWithPasswordAsync(request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("? User logged in successfully");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, new LoginResponse 
            { 
                Success = false, 
                ErrorMessage = "An error occurred during login" 
            });
        }
    }

    /// <summary>
    /// Request password reset email
    /// </summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ForgotPasswordResponse), 200)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        try
        {
            _logger.LogInformation("Password reset requested for: {Email}", request.Email);

            var result = await _authService.ForgotPasswordAsync(request);

            _logger.LogInformation("? Password reset request processed");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in forgot password");
            return StatusCode(500, new ForgotPasswordResponse 
            { 
                Success = false, 
                ErrorMessage = "An error occurred" 
            });
        }
    }

    /// <summary>
    /// Reset password with token
    /// </summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ResetPasswordResponse), 200)]
    [ProducesResponseType(typeof(ResetPasswordResponse), 400)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            _logger.LogInformation("Password reset for: {Email}", request.Email);

            var result = await _authService.ResetPasswordAsync(request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("? Password reset successfully");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password");
            return StatusCode(500, new ResetPasswordResponse 
            { 
                Success = false, 
                ErrorMessage = "An error occurred" 
            });
        }
    }

    /// <summary>
    /// Change password for authenticated user
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ChangePasswordResponse), 200)]
    [ProducesResponseType(typeof(ChangePasswordResponse), 400)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            // Get user ID from token
            var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new ChangePasswordResponse 
                { 
                    Success = false, 
                    ErrorMessage = "Invalid token" 
                });
            }

            _logger.LogInformation("Change password for user: {UserId}", userId);

            var result = await _authService.ChangePasswordAsync(userId, request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("? Password changed successfully");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password");
            return StatusCode(500, new ChangePasswordResponse 
            { 
                Success = false, 
                ErrorMessage = "An error occurred" 
            });
        }
    }
}

public record SendOtpRequest(string PhoneNumber);
public record VerifyOtpRequest(string PhoneNumber, string Otp);
public record ValidateTokenRequest(string Token);
