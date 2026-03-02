using System.Security.Claims;
using MedRemind.Core.DTOs;
using MedRemind.Services.VoiceRecordings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedRemind.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Require JWT authentication
public class VoiceRecordingsController : ControllerBase
{
    private readonly VoiceRecordingService _voiceRecordingService;
    private readonly ILogger<VoiceRecordingsController> _logger;

    public VoiceRecordingsController(
        VoiceRecordingService voiceRecordingService,
        ILogger<VoiceRecordingsController> logger)
    {
        _voiceRecordingService = voiceRecordingService;
        _logger = logger;
    }

    /// <summary>
    /// Upload a new voice recording
    /// </summary>
    /// <remarks>
    /// Accepts base64-encoded audio file (max 10MB, max 60 seconds)
    /// Supported formats: MP3, WAV, OGG, WEBM, M4A
    /// </remarks>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadVoiceRecording([FromBody] CreateVoiceRecordingRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _voiceRecordingService.CreateVoiceRecordingAsync(request, userId);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(new { message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading voice recording");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get all voice recordings for the current user
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUserVoiceRecordings()
    {
        try
        {
            var userId = GetCurrentUserId();
            var recordings = await _voiceRecordingService.GetUserVoiceRecordingsAsync(userId);
            return Ok(recordings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting voice recordings");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get a specific voice recording by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetVoiceRecordingById(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var recording = await _voiceRecordingService.GetVoiceRecordingByIdAsync(id, userId);

            if (recording == null)
            {
                return NotFound(new { message = "Voice recording not found" });
            }

            return Ok(recording);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting voice recording {Id}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get audio file for playback
    /// Returns the actual audio file bytes with appropriate content type
    /// </summary>
    [HttpGet("{id}/play")]
    public async Task<IActionResult> PlayVoiceRecording(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var (audioBytes, contentType) = await _voiceRecordingService.GetAudioForPlaybackAsync(id, userId);

            if (audioBytes == null)
            {
                return NotFound(new { message = "Audio file not found" });
            }

            return File(audioBytes, contentType, enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error playing voice recording {Id}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update voice recording name
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateVoiceRecording(int id, [FromBody] UpdateVoiceRecordingRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            request.Id = id; // Ensure ID matches route parameter

            var success = await _voiceRecordingService.UpdateVoiceRecordingNameAsync(request, userId);

            if (success)
            {
                return Ok(new { message = "Voice recording updated successfully" });
            }

            return BadRequest(new { message = "Failed to update voice recording" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating voice recording {Id}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete a voice recording
    /// Cannot delete if the recording is linked to active reminders
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteVoiceRecording(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _voiceRecordingService.DeleteVoiceRecordingAsync(id, userId);

            if (success)
            {
                return Ok(new { message = "Voice recording deleted successfully" });
            }

            return BadRequest(new { message = "Failed to delete voice recording. It may be in use by active reminders." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting voice recording {Id}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // Helper method to get current user ID from JWT token
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }
}
