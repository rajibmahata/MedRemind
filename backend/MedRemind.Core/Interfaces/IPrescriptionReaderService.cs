using MedRemind.Core.DTOs;

namespace MedRemind.Core.Interfaces;

/// <summary>
/// Service for reading and processing prescription images
/// </summary>
public interface IPrescriptionReaderService
{
    /// <summary>
    /// Read prescription from file path
    /// </summary>
    Task<PrescriptionReadResult> ReadPrescriptionAsync(string imagePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Read prescription from base64 image string
    /// </summary>
    Task<PrescriptionReadResult> ReadPrescriptionFromBase64Async(string base64Image, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a prescription image and related metadata asynchronously, performing comprehensive extraction and
    /// analysis to produce a detailed processing result.   
    /// </summary>
    /// <remarks>Either imageBase64 or imagePath must provide a valid image. The method performs validation,
    /// extraction, and analysis steps, and may store or update prescription records as part of the process. The
    /// operation is thread-safe and can be cancelled via the provided cancellation token.</remarks>
    /// <param name="imageBase64">The base64-encoded string representation of the prescription image to be processed. Cannot be null or empty.</param>
    /// <param name="imagePath">The file system path to the prescription image, if available. Can be null if the image is provided only as a
    /// base64 string.</param>
    /// <param name="uniqueFileName">A unique file name to associate with the prescription image during processing and storage. Cannot be null or
    /// empty.</param>
    /// <param name="originalFileName">The original file name of the uploaded prescription image, if available. Can be null if not provided.</param>
    /// <param name="userId">The identifier of the user submitting the prescription for processing. Must be a positive integer.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a PrescriptionProcessingResult
    /// object with the extracted prescription data and processing status.</returns>
    Task<PrescriptionProcessingResult> ProcessPrescriptionComprehensiveAsync(
            string imageBase64,
            string? imagePath,
            string uniqueFileName,
            string? originalFileName,
            int userId,
            CancellationToken cancellationToken = default);
}
