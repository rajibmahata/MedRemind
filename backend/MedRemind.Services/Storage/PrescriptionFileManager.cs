using MedRemind.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using System.IO.Compression;

namespace MedRemind.Services.Storage;

/// <summary>
/// Modern file management service for prescription images and PDFs
/// Handles compression, format validation, and delegates storage to FileStorageService
/// </summary>
public class PrescriptionFileManager : IPrescriptionFileManager
{
    private readonly ILogger<PrescriptionFileManager> _logger;
    private readonly IFileStorageService _fileStorageService;
    private const int MAX_FILE_SIZE_MB = 4; // Azure DI limit
    private const int TARGET_FILE_SIZE_MB = 3; // Target to stay safe
    private const long MAX_FILE_SIZE_BYTES = MAX_FILE_SIZE_MB * 1024 * 1024;
    private const long TARGET_FILE_SIZE_BYTES = TARGET_FILE_SIZE_MB * 1024 * 1024;

    private static readonly HashSet<string> AllowedImageExtensions = new() { ".jpg", ".jpeg", ".png", ".bmp", ".webp" };
    private static readonly HashSet<string> AllowedPdfExtensions = new() { ".pdf" };

    public PrescriptionFileManager(
        ILogger<PrescriptionFileManager> logger,
        IFileStorageService fileStorageService)
    {
        _logger = logger;
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
    }

    /// <summary>
    /// Save uploaded file with automatic compression and format validation
    /// </summary>
    public async Task<PrescriptionFileResult> SavePrescriptionFileAsync(
        IFormFile file,
        int userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate file
            var validationResult = ValidateFile(file);
            if (!validationResult.IsValid)
            {
                return new PrescriptionFileResult
                {
                    Success = false,
                    ErrorMessage = validationResult.ErrorMessage
                };
            }

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var isPdf = AllowedPdfExtensions.Contains(fileExtension);
            var isImage = AllowedImageExtensions.Contains(fileExtension);

            _logger.LogInformation("Processing file: {FileName}, Size: {Size} KB, Type: {Type}",
                file.FileName,
                file.Length / 1024.0,
                isPdf ? "PDF" : "Image");

            byte[] fileBytes;
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream, cancellationToken);
                fileBytes = memoryStream.ToArray();
            }

            var originalSize = file.Length;

            // Compress if needed
            if (fileBytes.Length > TARGET_FILE_SIZE_BYTES)
            {
                _logger.LogInformation("File exceeds target size ({TargetMB} MB), compressing...", TARGET_FILE_SIZE_MB);
                
                if (isImage)
                {
                    fileBytes = await CompressImageAsync(fileBytes, fileExtension);
                }
                else if (isPdf)
                {
                    fileBytes = await CompressPdfAsync(fileBytes);
                }

                _logger.LogInformation("Compressed to: {Size} KB", fileBytes.Length / 1024.0);
            }

            // Validate final size
            if (fileBytes.Length > MAX_FILE_SIZE_BYTES)
            {
                return new PrescriptionFileResult
                {
                    Success = false,
                    ErrorMessage = $"File size ({fileBytes.Length / (1024.0 * 1024.0):F2} MB) exceeds maximum allowed size ({MAX_FILE_SIZE_MB} MB) even after compression. Please use a smaller file."
                };
            }

            // Save file using FileStorageService
            var (filePath, relativePath, fileName) = await _fileStorageService.SavePrescriptionFileAsync(
                fileBytes,
                userId,
                file.FileName);

            if (string.IsNullOrEmpty(filePath))
            {
                return new PrescriptionFileResult
                {
                    Success = false,
                    ErrorMessage = "Failed to save file to storage"
                };
            }

            _logger.LogInformation("File saved successfully: {Path}", filePath);

            return new PrescriptionFileResult
            {
                Success = true,
                FilePath = filePath,
                RelativePath = relativePath,
                FileName = fileName,
                FileSize = fileBytes.Length,
                FileType = isPdf ? "PDF" : "Image",
                Base64Content = Convert.ToBase64String(fileBytes),
                WasCompressed = originalSize != fileBytes.Length,
                OriginalSize = originalSize,
                CompressedSize = fileBytes.Length
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving prescription file");
            return new PrescriptionFileResult
            {
                Success = false,
                ErrorMessage = $"Error saving file: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Validate file type and size
    /// </summary>
    private FileValidationResult ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return new FileValidationResult
            {
                IsValid = false,
                ErrorMessage = "No file provided or file is empty"
            };
        }

        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = AllowedImageExtensions.Union(AllowedPdfExtensions);

        if (!allowedExtensions.Contains(fileExtension))
        {
            return new FileValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Invalid file type. Allowed types: {string.Join(", ", allowedExtensions)}"
            };
        }

        // Allow larger files since we'll compress them
        const long maxUploadSize = 20 * 1024 * 1024; // 20 MB max upload
        if (file.Length > maxUploadSize)
        {
            return new FileValidationResult
            {
                IsValid = false,
                ErrorMessage = $"File size ({file.Length / (1024.0 * 1024.0):F2} MB) exceeds maximum upload size (20 MB)"
            };
        }

        return new FileValidationResult { IsValid = true };
    }

    /// <summary>
    /// Compress image using SkiaSharp while maintaining text clarity
    /// </summary>
    private async Task<byte[]> CompressImageAsync(byte[] imageBytes, string extension)
    {
        try
        {
            using var inputStream = new MemoryStream(imageBytes);
            using var inputBitmap = SKBitmap.Decode(inputStream);

            if (inputBitmap == null)
            {
                _logger.LogWarning("Failed to decode image, returning original");
                return imageBytes;
            }

            _logger.LogInformation("Original image dimensions: {Width}x{Height}", inputBitmap.Width, inputBitmap.Height);

            // Calculate optimal scale to target size
            double scaleFactor = Math.Sqrt((double)TARGET_FILE_SIZE_BYTES / imageBytes.Length);
            scaleFactor = Math.Min(scaleFactor, 0.95); // Max 95% of original to ensure reduction
            scaleFactor = Math.Max(scaleFactor, 0.5);  // Min 50% to maintain clarity

            int newWidth = (int)(inputBitmap.Width * scaleFactor);
            int newHeight = (int)(inputBitmap.Height * scaleFactor);

            // Ensure minimum dimensions for text readability (1200px on longest edge)
            const int minDimension = 1200;
            var maxCurrentDimension = Math.Max(inputBitmap.Width, inputBitmap.Height);
            
            if (maxCurrentDimension > minDimension)
            {
                var aspectRatio = (double)inputBitmap.Width / inputBitmap.Height;
                
                if (inputBitmap.Width > inputBitmap.Height)
                {
                    newWidth = Math.Max(newWidth, minDimension);
                    newHeight = (int)(newWidth / aspectRatio);
                }
                else
                {
                    newHeight = Math.Max(newHeight, minDimension);
                    newWidth = (int)(newHeight * aspectRatio);
                }
            }

            _logger.LogInformation("Resizing to: {Width}x{Height} (scale factor: {Scale:P0})", 
                newWidth, newHeight, scaleFactor);

            // Resize with high quality filter for text clarity
            var resizeInfo = new SKImageInfo(newWidth, newHeight);
            using var surface = SKSurface.Create(resizeInfo);
            using var paint = new SKPaint
            {
                IsAntialias = true,
                FilterQuality = SKFilterQuality.High // High quality for text preservation
            };

            var destRect = new SKRect(0, 0, newWidth, newHeight);
            surface.Canvas.DrawBitmap(inputBitmap, destRect, paint);
            surface.Canvas.Flush();

            using var image = surface.Snapshot();
            using var outputStream = new MemoryStream();

            // Save as JPEG with quality optimized for documents (85%)
            var encodedImage = image.Encode(SKEncodedImageFormat.Jpeg, 85);
            encodedImage.SaveTo(outputStream);

            var compressedBytes = outputStream.ToArray();
            
            // If still too large, reduce quality to 75%
            if (compressedBytes.Length > MAX_FILE_SIZE_BYTES)
            {
                _logger.LogInformation("Still too large, reducing quality to 75%");
                outputStream.SetLength(0);
                encodedImage = image.Encode(SKEncodedImageFormat.Jpeg, 75);
                encodedImage.SaveTo(outputStream);
                compressedBytes = outputStream.ToArray();
            }

            _logger.LogInformation("Compression complete: {OriginalKB} KB ? {CompressedKB} KB ({Reduction:P0} reduction)",
                imageBytes.Length / 1024.0,
                compressedBytes.Length / 1024.0,
                1.0 - ((double)compressedBytes.Length / imageBytes.Length));

            return compressedBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Image compression failed, returning original");
            return imageBytes;
        }
    }

    /// <summary>
    /// Compress PDF while maintaining text quality
    /// Note: Basic implementation - for advanced PDF compression, consider using libraries like iTextSharp
    /// </summary>
    private async Task<byte[]> CompressPdfAsync(byte[] pdfBytes)
    {
        try
        {
            _logger.LogInformation("PDF compression: Applying basic compression");
            
            // For basic PDF compression, we can use GZip (but this doesn't reduce the actual PDF size)
            // For proper PDF compression, you'd need a library like iTextSharp/iText7
            // For now, we'll just validate size and return as-is
            
            // TODO: Implement proper PDF compression with iText7 if needed
            // This would involve:
            // 1. Recompressing images within PDF
            // 2. Removing metadata
            // 3. Optimizing fonts
            // 4. Removing unused objects

            if (pdfBytes.Length > MAX_FILE_SIZE_BYTES)
            {
                _logger.LogWarning("PDF size ({SizeMB} MB) exceeds maximum. Consider converting to images first.",
                    pdfBytes.Length / (1024.0 * 1024.0));
            }

            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PDF compression failed, returning original");
            return pdfBytes;
        }
    }

    /// <summary>
    /// Get file from storage
    /// </summary>
    public async Task<byte[]?> GetFileAsync(string fileName)
    {
        try
        {
            var prescriptionsDir = _fileStorageService.GetPrescriptionsDirectory();
            var filePath = Path.Combine(prescriptionsDir, fileName);
            
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {Path}", filePath);
                return null;
            }

            return await File.ReadAllBytesAsync(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file: {FileName}", fileName);
            return null;
        }
    }

    /// <summary>
    /// Delete file from storage
    /// </summary>
    public async Task<bool> DeleteFileAsync(string fileName)
    {
        try
        {
            var prescriptionsDir = _fileStorageService.GetPrescriptionsDirectory();
            var filePath = Path.Combine(prescriptionsDir, fileName);
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("File deleted: {Path}", filePath);
                
                // Also delete associated OCR files
                await _fileStorageService.DeletePrescriptionWithOcrAsync(fileName);
                
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FileName}", fileName);
            return false;
        }
    }

    /// <summary>
    /// Get storage statistics
    /// </summary>
    public StorageStatistics GetStorageStatistics()
    {
        try
        {
            var prescriptionsDir = _fileStorageService.GetPrescriptionsDirectory();
            var files = Directory.GetFiles(prescriptionsDir);
            var totalSize = files.Sum(f => new FileInfo(f).Length);

            return new StorageStatistics
            {
                TotalFiles = files.Length,
                TotalSizeBytes = totalSize,
                TotalSizeMB = totalSize / (1024.0 * 1024.0),
                StoragePath = prescriptionsDir
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting storage statistics");
            return new StorageStatistics();
        }
    }
}

/// <summary>
/// Result of file save operation
/// </summary>
public class PrescriptionFileResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? FilePath { get; set; }
    public string? RelativePath { get; set; }
    public string? FileName { get; set; }
    public long FileSize { get; set; }
    public string? FileType { get; set; }
    public string? Base64Content { get; set; }
    public bool WasCompressed { get; set; }
    public long OriginalSize { get; set; }
    public long CompressedSize { get; set; }

    public double CompressionRatio => OriginalSize > 0 
        ? 1.0 - ((double)CompressedSize / OriginalSize) 
        : 0.0;
}

/// <summary>
/// File validation result
/// </summary>
public class FileValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Storage statistics
/// </summary>
public class StorageStatistics
{
    public int TotalFiles { get; set; }
    public long TotalSizeBytes { get; set; }
    public double TotalSizeMB { get; set; }
    public string? StoragePath { get; set; }
}
