using System.Security.Claims;
using MedRemind.Core.DTOs;
using MedRemind.Services.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedRemind.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Require JWT authentication
public class ValidationController : ControllerBase
{
    private readonly ValidationWorkflowService _validationService;
    private readonly ILogger<ValidationController> _logger;

    public ValidationController(
        ValidationWorkflowService validationService,
        ILogger<ValidationController> logger)
    {
        _validationService = validationService;
        _logger = logger;
    }

    /// <summary>
    /// Get validation workflow for a prescription
    /// Creates workflow if it doesn't exist
    /// </summary>
    [HttpGet("prescription/{prescriptionId}")]
    public async Task<IActionResult> GetValidationWorkflow(int prescriptionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var workflow = await _validationService.GetValidationWorkflowAsync(prescriptionId, userId);
            return Ok(workflow);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation workflow error for prescription {PrescriptionId}", prescriptionId);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting validation workflow for prescription {PrescriptionId}", prescriptionId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Create validation workflow for a prescription
    /// </summary>
    [HttpPost("prescription/{prescriptionId}")]
    public async Task<IActionResult> CreateValidationWorkflow(int prescriptionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var workflow = await _validationService.CreateValidationWorkflowAsync(prescriptionId, userId);
            return Ok(workflow);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error creating validation workflow for prescription {PrescriptionId}", prescriptionId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating validation workflow for prescription {PrescriptionId}", prescriptionId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Confirm a medication after user review
    /// </summary>
    [HttpPost("medication/confirm")]
    public async Task<IActionResult> ConfirmMedication([FromBody] ConfirmMedicationRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _validationService.ConfirmMedicationAsync(request, userId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error confirming medication {MedicationId}", request.MedicationId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming medication {MedicationId}", request.MedicationId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Apply user corrections to a medication
    /// </summary>
    [HttpPut("medication/correct")]
    public async Task<IActionResult> CorrectMedication([FromBody] CorrectMedicationRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _validationService.CorrectMedicationAsync(request, userId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error correcting medication {MedicationId}", request.MedicationId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error correcting medication {MedicationId}", request.MedicationId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete a medication during validation
    /// </summary>
    [HttpDelete("medication/{medicationId}")]
    public async Task<IActionResult> DeleteMedication(int medicationId, [FromBody] DeleteMedicationRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            request.MedicationId = medicationId; // Ensure ID matches
            var result = await _validationService.DeleteMedicationAsync(request, userId);
            return Ok(new { message = "Medication deleted successfully", success = result });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error deleting medication {MedicationId}", medicationId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting medication {MedicationId}", medicationId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Complete validation workflow
    /// All medications must be confirmed before completion
    /// </summary>
    [HttpPost("complete")]
    public async Task<IActionResult> CompleteValidation([FromBody] CompleteValidationRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _validationService.CompleteValidationAsync(request, userId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error completing validation for prescription {PrescriptionId}", request.PrescriptionId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing validation for prescription {PrescriptionId}", request.PrescriptionId);
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
