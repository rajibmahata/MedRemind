using MedRemind.Core.Interfaces;
using MedRemind.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace MedRemind.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PrescriptionsController : ControllerBase
{
    private readonly IPrescriptionReaderService _prescriptionReader;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PrescriptionsController> _logger;

    public PrescriptionsController(
        IPrescriptionReaderService prescriptionReader,
        IUnitOfWork unitOfWork,
        ILogger<PrescriptionsController> logger)
    {
        _prescriptionReader = prescriptionReader;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Upload and process prescription image
    /// </summary>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadPrescription([FromBody] UploadPrescriptionRequest request)
    {
        try
        {
            // Process with AI
            var result = await _prescriptionReader.ReadPrescriptionFromBase64Async(request.ImageBase64);

            if (!result.Success)
            {
                return BadRequest(new { message = result.ErrorMessage });
            }

            // Save prescription to database
            var prescription = new Prescription
            {
                UserId = request.UserId,
                ImagePath = $"data:image/jpeg;base64,{request.ImageBase64.Substring(0, Math.Min(50, request.ImageBase64.Length))}...",
                PrescriptionDate = result.PrescriptionDate ?? DateTime.UtcNow,
                DoctorName = result.DoctorName,
                Status = "Processed",
                ConfidenceScore = result.ConfidenceScore
            };

            var prescriptionRepo = _unitOfWork.Repository<Prescription>();
            await prescriptionRepo.AddAsync(prescription);
            await _unitOfWork.SaveChangesAsync();

            return Ok(new
            {
                prescriptionId = prescription.Id,
                medications = result.Medications,
                doctorName = result.DoctorName,
                prescriptionDate = result.PrescriptionDate,
                confidenceScore = result.ConfidenceScore,
                warnings = result.Warnings
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing prescription");
            return StatusCode(500, new { message = "An error occurred while processing prescription" });
        }
    }

    /// <summary>
    /// Get user's prescriptions
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserPrescriptions(int userId)
    {
        try
        {
            var prescriptionRepo = _unitOfWork.Repository<Prescription>();
            var prescriptions = await prescriptionRepo.FindAsync(p => p.UserId == userId);
            return Ok(prescriptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting prescriptions for user {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while retrieving prescriptions" });
        }
    }

    /// <summary>
    /// Get prescription by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPrescription(int id)
    {
        try
        {
            var prescriptionRepo = _unitOfWork.Repository<Prescription>();
            var prescription = await prescriptionRepo.GetByIdAsync(id);
            
            if (prescription == null)
            {
                return NotFound(new { message = "Prescription not found" });
            }

            return Ok(prescription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting prescription {Id}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving prescription" });
        }
    }
}

public record UploadPrescriptionRequest(int UserId, string ImageBase64);
