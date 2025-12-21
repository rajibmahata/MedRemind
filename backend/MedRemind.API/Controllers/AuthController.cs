using MedRemind.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

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
}

public record SendOtpRequest(string PhoneNumber);
public record VerifyOtpRequest(string PhoneNumber, string Otp);
public record ValidateTokenRequest(string Token);
