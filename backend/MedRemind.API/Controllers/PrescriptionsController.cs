using System.Security.Claims;
using MedRemind.Core.Interfaces;
using MedRemind.Core.Models;
using MedRemind.Services.Prescriptions;
using MedRemind.Services.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedRemind.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Require JWT authentication
public class PrescriptionsController : ControllerBase
{
    private readonly IPrescriptionReaderService _prescriptionReader;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PrescriptionsController> _logger;
    private readonly IPrescriptionFileManager _fileManager;

    public PrescriptionsController(
        IPrescriptionReaderService prescriptionReader,
        IUnitOfWork unitOfWork,
        ILogger<PrescriptionsController> logger,
        IPrescriptionFileManager fileManager)
    {
        _prescriptionReader = prescriptionReader;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _fileManager = fileManager;
    }

    /// <summary>
    /// Upload and process prescription image or PDF with comprehensive AI processing
    /// </summary>
    /// <remarks>
    /// Supports: JPG, JPEG, PNG, BMP, WEBP, PDF
    /// Maximum file size: 20 MB (will be compressed automatically)
    /// Processing includes: OCR extraction, duplicate detection, AI parsing with multiple providers
    /// </remarks>
    [HttpPost("upload")]
    [RequestSizeLimit(20_971_520)] // 20 MB
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadPrescription([FromForm] IFormFile file, [FromForm] int? userId = null)
    {
        try
        {
            // Get authenticated user ID from JWT token
            var authenticatedUserId = GetCurrentUserId();
            var targetUserId = userId ?? authenticatedUserId;

            // Validate user authorization
            if (targetUserId != authenticatedUserId)
            {
                return Forbid("You can only upload prescriptions for your own account");
            }

            _logger.LogInformation("?? Prescription upload started - User: {UserId}, File: {FileName}, Size: {SizeKB} KB",
                targetUserId, file.FileName, file.Length / 1024.0);

            // Step 1: Save and compress file
            _logger.LogInformation("?? Step 1: Saving file to storage...");
            var fileResult = await _fileManager.SavePrescriptionFileAsync(file, targetUserId);

            if (!fileResult.Success)
            {
                _logger.LogWarning("? File save failed: {Error}", fileResult.ErrorMessage);
                return BadRequest(new { message = fileResult.ErrorMessage });
            }

            _logger.LogInformation("? File saved: {Path} ({SizeKB} KB, {Compressed})",
                                    fileResult.FileName,
                                    fileResult.FileSize / 1024.0,
                                    fileResult.WasCompressed ? $"compressed from {fileResult.OriginalSize / 1024.0:F0} KB" : "no compression needed");

            // Step 2: Check if comprehensive processing is available
            if (_prescriptionReader is PrescriptionReaderService readerService)
            {
                _logger.LogInformation("? Step 2: Processing with comprehensive service (AgentOrchestrator V2)...");
                
                var result = await readerService.ProcessPrescriptionComprehensiveAsync(
                    fileResult.Base64Content!,
                    fileResult.RelativePath,
                    fileResult.FileName,          // uniqueFileName
                    file.FileName,                // originalFileName
                    targetUserId);

                if (result.Success)
                {
                    // Build response with metrics
                    var response = new
                    {
                        success = true,
                        prescriptionId = result.PrescriptionId,
                        filePath = fileResult.RelativePath,
                        fileName = fileResult.FileName,
                        fileSize = fileResult.FileSize,
                        wasCompressed = fileResult.WasCompressed,
                        compressionRatio = fileResult.CompressionRatio,
                        
                        // Duplicate detection
                        isDuplicate = result.IsDuplicate,
                        duplicateMessage = result.DuplicateMessage,
                        similarityScore = result.SimilarityScore,
                        existingPrescriptionId = result.ExistingPrescriptionId,
                        
                        // Processing results
                        medications = result.PrescriptionResult?.Medications,
                        doctorName = result.PrescriptionResult?.Doctor?.Name,
                        prescriptionDate = result.PrescriptionResult?.PrescriptionDate,
                        
                        // Quality metrics
                        confidenceScore = result.PrescriptionResult?.ConfidenceScore,
                        matchScore = result.MatchScore,
                        processingAttempts = result.ProcessingAttempts,
                        selectedProvider = result.SelectedProvider,
                        processingTimeSeconds = result.ProcessingTime.TotalSeconds,
                        
                        warnings = result.WarningMessage
                    };

                    _logger.LogInformation("? Prescription processed successfully - ID: {PrescriptionId}, Medications: {Count}, Provider: {Provider}, Time: {Time}s",
                        result.PrescriptionId,
                        result.PrescriptionResult?.Medications?.Count ?? 0,
                        result.SelectedProvider,
                        result.ProcessingTime.TotalSeconds);

                    return Ok(response);
                }
                else
                {
                    _logger.LogError("? Comprehensive processing failed: {Error}", result.ErrorMessage);
                    return BadRequest(new { message = result.ErrorMessage });
                }
            }
            else
            {
                // Fallback to basic processing
                _logger.LogInformation("? Step 2: Processing with basic service...");
                var result = await _prescriptionReader.ReadPrescriptionFromBase64Async(fileResult.Base64Content!);

                if (!result.Success)
                {
                    return BadRequest(new { message = result.ErrorMessage });
                }

                // Save prescription to database
                var prescription = new Prescription
                {
                    UserId = targetUserId,
                    ImagePath = fileResult.RelativePath,
                    FileName = fileResult.FileName,
                    FileSize = fileResult.FileSize,
                    PrescriptionDate = result.PrescriptionDate ?? DateTime.UtcNow,
                    DoctorName = result?.Doctor?.Name,
                    Status = "Processed",
                    ProcessedAt = DateTime.UtcNow
                };

                var prescriptionRepo = _unitOfWork.Repository<Prescription>();
                await prescriptionRepo.AddAsync(prescription);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    prescriptionId = prescription.Id,
                    filePath = fileResult.RelativePath,
                    fileName = fileResult.FileName,
                    fileSize = fileResult.FileSize,
                    medications = result.Medications,
                    doctorName = result?.Doctor.Name,
                    prescriptionDate = result.PrescriptionDate,
                    confidenceScore = result.ConfidenceScore,
                    warnings = result.ValidationWarnings
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Error processing prescription upload");
            return StatusCode(500, new 
            { 
                message = "An error occurred while processing prescription",
                error = ex.Message 
            });
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
            var authenticatedUserId = GetCurrentUserId();
            
            // Validate authorization
            if (userId != authenticatedUserId)
            {
                return Forbid("You can only access your own prescriptions");
            }

            var prescriptionRepo = _unitOfWork.Repository<Prescription>();
            var prescriptions = await prescriptionRepo.FindAsync(p => p.UserId == userId);
            
            _logger.LogInformation("?? Retrieved {Count} prescriptions for user {UserId}", 
                prescriptions.Count(), userId);
            
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

            var authenticatedUserId = GetCurrentUserId();
            
            // Validate authorization
            if (prescription.UserId != authenticatedUserId)
            {
                return Forbid("You can only access your own prescriptions");
            }

            return Ok(prescription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting prescription {Id}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving prescription" });
        }
    }

    /// <summary>
    /// Get prescription image file
    /// </summary>
    [HttpGet("{id}/image")]
    public async Task<IActionResult> GetPrescriptionImage(int id)
    {
        try
        {
            var prescriptionRepo = _unitOfWork.Repository<Prescription>();
            var prescription = await prescriptionRepo.GetByIdAsync(id);
            
            if (prescription == null)
            {
                return NotFound(new { message = "Prescription not found" });
            }

            var authenticatedUserId = GetCurrentUserId();
            
            // Validate authorization
            if (prescription.UserId != authenticatedUserId)
            {
                return Forbid("You can only access your own prescriptions");
            }

            if (string.IsNullOrEmpty(prescription.ImagePath))
            {
                return NotFound(new { message = "Prescription image not found" });
            }

            // Get file name from path
            var fileName = Path.GetFileName(prescription.ImagePath);
            var fileBytes = await _fileManager.GetFileAsync(fileName);

            if (fileBytes == null)
            {
                return NotFound(new { message = "Image file not found on server" });
            }

            // Determine content type from file extension
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var contentType = extension switch
            {
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            return File(fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting prescription image {Id}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving image" });
        }
    }

    /// <summary>
    /// Delete prescription and associated file
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePrescription(int id)
    {
        try
        {
            var prescriptionRepo = _unitOfWork.Repository<Prescription>();
            var prescription = await prescriptionRepo.GetByIdAsync(id);
            
            if (prescription == null)
            {
                return NotFound(new { message = "Prescription not found" });
            }

            var authenticatedUserId = GetCurrentUserId();
            
            // Validate authorization
            if (prescription.UserId != authenticatedUserId)
            {
                return Forbid("You can only delete your own prescriptions");
            }

            // Delete file if exists
            if (!string.IsNullOrEmpty(prescription.ImagePath))
            {
                var fileName = Path.GetFileName(prescription.ImagePath);
                await _fileManager.DeleteFileAsync(fileName);
                _logger.LogInformation("??? Deleted file: {FileName}", fileName);
            }

            // Delete prescription from database
            await prescriptionRepo.DeleteAsync(prescription);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("? Prescription {Id} deleted successfully", id);
            
            return Ok(new { message = "Prescription deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting prescription {Id}", id);
            return StatusCode(500, new { message = "An error occurred while deleting prescription" });
        }
    }

    /// <summary>
    /// Get storage statistics (admin only - for monitoring)
    /// </summary>
    [HttpGet("storage/stats")]
    public IActionResult GetStorageStats()
    {
        try
        {
            var stats = _fileManager.GetStorageStatistics();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting storage statistics");
            return StatusCode(500, new { message = "An error occurred while retrieving statistics" });
        }
    }

    /// <summary>
    /// Helper method to get current user ID from JWT claims
    /// </summary>
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                          ?? User.FindFirst("sub")?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user token");
        }

        return userId;
    }
}

/// <summary>
/// Legacy request model for backward compatibility
/// </summary>
public record UploadPrescriptionRequest(int UserId, string ImageBase64);

