using Azure;
using Azure.AI.DocumentIntelligence;
using System.Text;
using System.Text.Json;
using SkiaSharp;
using MedRemind.Services.AI.Extensions;
using MedRemind.Core.Interfaces;

namespace MedRemind.Services.AI;

/// <summary>
/// Azure Document Intelligence service for prescription text extraction
/// Uses Azure Cognitive Services SDK to perform OCR on prescription images
/// Automatically resizes images larger than 4MB to meet Azure DI requirements
/// </summary>
public class AzureDocumentIntelligenceService
{
    private readonly DocumentIntelligenceClient _client;
    private readonly PrescriptionOcrTextPreprocessor _prescriptionOcrTextPreprocessor;
    private readonly IFileStorageService _fileStorageService;
    private readonly string _endpoint;

    // Azure Document Intelligence size limits (4MB for Read API)
    private const int MAX_IMAGE_SIZE_BYTES = 4 * 1024 * 1024; // 4MB
    private const int TARGET_IMAGE_SIZE_BYTES = 3 * 1024 * 1024; // 3MB target for safety margin

    public AzureDocumentIntelligenceService(
        HttpClient httpClient, 
        string endpoint, 
        string apiKey, 
        PrescriptionOcrTextPreprocessor prescriptionOcrTextPreprocessor,
        IFileStorageService fileStorageService)
    {
        _endpoint = endpoint.TrimEnd('/');

        // Create Azure Document Intelligence client with credentials
        var credential = new AzureKeyCredential(apiKey);
        _client = new DocumentIntelligenceClient(new Uri(_endpoint), credential);

        _prescriptionOcrTextPreprocessor = prescriptionOcrTextPreprocessor;
        _fileStorageService = fileStorageService;

        System.Diagnostics.Debug.WriteLine($"📄 Azure DI: Client initialized");
        System.Diagnostics.Debug.WriteLine($"   Endpoint: {_endpoint}");
        System.Diagnostics.Debug.WriteLine($"   File Storage: {(_fileStorageService.IsFileLoggingEnabled() ? "Enabled" : "Disabled")}");
    }

    /// <summary>
    /// Extract text from prescription image using Azure Document Intelligence
    /// Automatically resizes images larger than 4MB while preserving document content quality
    /// </summary>
    public async Task<string> ExtractTextFromImageAsync(string base64Image, CancellationToken cancellationToken = default)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("📄 Azure DI: Starting text extraction...");
            System.Diagnostics.Debug.WriteLine($"   Base64 string length: {base64Image.Length} characters");

            // Convert base64 to bytes
            var imageBytes = Convert.FromBase64String(base64Image);

            System.Diagnostics.Debug.WriteLine($"📊 Original image size: {imageBytes.Length / 1024.0:F2} KB ({imageBytes.Length / (1024.0 * 1024.0):F2} MB)");

            // Resize if image exceeds target size (3MB for safety margin)
            if (imageBytes.Length > TARGET_IMAGE_SIZE_BYTES)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Image exceeds {TARGET_IMAGE_SIZE_BYTES / (1024.0 * 1024.0):F1} MB target, resizing to preserve document content...");
                imageBytes = await ResizeImageForDocumentAnalysisAsync(imageBytes);
                System.Diagnostics.Debug.WriteLine($"✅ Resized to: {imageBytes.Length / 1024.0:F2} KB ({imageBytes.Length / (1024.0 * 1024.0):F2} MB)");
            }

            // Final safety check
            if (imageBytes.Length > MAX_IMAGE_SIZE_BYTES)
            {
                var sizeMB = imageBytes.Length / (1024.0 * 1024.0);
                throw new InvalidOperationException(
                    $"Image size ({sizeMB:F2} MB) still exceeds Azure Document Intelligence limit (4 MB) after resizing. " +
                    $"Please use a lower resolution image.");
            }

            System.Diagnostics.Debug.WriteLine("📄 Azure DI: Creating BinaryData from image bytes...");

            // Create BinaryData from image bytes
            using var stream = new MemoryStream(imageBytes);
            var binaryData = BinaryData.FromStream(stream);

            System.Diagnostics.Debug.WriteLine("📄 Azure DI: Submitting document for analysis with 'prebuilt-read' model...");

            // Analyze document with prebuilt-read model (optimized for text extraction)
            var operation = await _client.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                "prebuilt-read",
                binaryData,
                cancellationToken: cancellationToken);

            System.Diagnostics.Debug.WriteLine($"✅ Azure DI: Analysis completed");

            var result = operation.Value;

            // Extract text from result
            var extractedText = ExtractTextFromResult(result);

            // Preprocess and normalize OCR text for better AI parsing
            var normalize_extractedText = _prescriptionOcrTextPreprocessor.Preprocess(extractedText, PrescriptionOcrTextPreprocessor.ProcessingMode.Minimal);

            // Save OCR results using FileStorageService
            await SaveOcrResultsAsync(extractedText, normalize_extractedText, result);

            System.Diagnostics.Debug.WriteLine($"✅ Azure DI: Text extraction complete");
            System.Diagnostics.Debug.WriteLine($"   Extracted text length: {extractedText.Length} characters");

            return normalize_extractedText;
        }
        catch (RequestFailedException ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Azure DI: Request failed");
            System.Diagnostics.Debug.WriteLine($"   Status: {ex.Status}");
            System.Diagnostics.Debug.WriteLine($"   Error Code: {ex.ErrorCode}");
            System.Diagnostics.Debug.WriteLine($"   Message: {ex.Message}");

            throw new HttpRequestException($"Azure DI Error [{ex.ErrorCode}]: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Azure DI: Error: {ex.Message}");
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"   Inner exception: {ex.InnerException.Message}");
            }
            throw;
        }
    }

    /// <summary>
    /// Resize image optimized for document analysis (prescription OCR)
    /// Uses SkiaSharp for cross-platform compatibility (works on Android, iOS, Windows, Linux, macOS)
    /// High quality settings to preserve text clarity and document content
    /// </summary>
    private async Task<byte[]> ResizeImageForDocumentAnalysisAsync(byte[] imageBytes)
    {
        try
        {
            // Use SkiaSharp for cross-platform image processing
            using var originalBitmap = SKBitmap.Decode(imageBytes);

            if (originalBitmap == null)
            {
                System.Diagnostics.Debug.WriteLine("❌ Failed to decode image, returning original");
                return imageBytes;
            }

            System.Diagnostics.Debug.WriteLine($"📐 Original dimensions: {originalBitmap.Width}x{originalBitmap.Height}");

            // Calculate scale factor to target size while preserving aspect ratio
            double scaleFactor = Math.Sqrt((double)TARGET_IMAGE_SIZE_BYTES / imageBytes.Length);

            // For document analysis, don't scale down too much (minimum 50% of original)
            // to preserve text readability
            scaleFactor = Math.Max(scaleFactor, 0.5);
            scaleFactor = Math.Min(scaleFactor, 0.95); // Max 95% to ensure some size reduction

            int newWidth = (int)(originalBitmap.Width * scaleFactor);
            int newHeight = (int)(originalBitmap.Height * scaleFactor);

            System.Diagnostics.Debug.WriteLine($"📐 Target dimensions: {newWidth}x{newHeight} (scale: {scaleFactor:P0})");

            // Resize with high quality (FilterQuality.High for documents)
            using var resizedBitmap = originalBitmap.Resize(
                new SKImageInfo(newWidth, newHeight),
                SKFilterQuality.High); // High quality for text preservation

            if (resizedBitmap == null)
            {
                System.Diagnostics.Debug.WriteLine("❌ Failed to resize image, returning original");
                return imageBytes;
            }

            // Encode as JPEG with 90% quality (document standard)
            using var image = SKImage.FromBitmap(resizedBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90); // 90% quality for documents

            if (data == null)
            {
                System.Diagnostics.Debug.WriteLine("❌ Failed to encode image, returning original");
                return imageBytes;
            }

            var resizedBytes = data.ToArray();
            System.Diagnostics.Debug.WriteLine($"✅ First pass: {resizedBytes.Length / 1024.0:F2} KB ({(1.0 - (double)resizedBytes.Length / imageBytes.Length):P0} reduction)");

            // If still too large, try with 85% quality
            if (resizedBytes.Length > MAX_IMAGE_SIZE_BYTES)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Still above limit, reducing quality to 85%...");
                using var data2 = image.Encode(SKEncodedImageFormat.Jpeg, 85); // 85% quality

                if (data2 != null)
                {
                    resizedBytes = data2.ToArray();
                    System.Diagnostics.Debug.WriteLine($"✅ Second pass: {resizedBytes.Length / 1024.0:F2} KB");
                }
            }

            // If STILL too large, scale down more aggressively
            if (resizedBytes.Length > MAX_IMAGE_SIZE_BYTES)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Still above limit, scaling down further...");
                double additionalScale = Math.Sqrt((double)TARGET_IMAGE_SIZE_BYTES / resizedBytes.Length);
                return await ResizeImageWithScaleAsync(imageBytes, scaleFactor * additionalScale);
            }

            return resizedBytes;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Image resize failed: {ex.Message}");
            System.Diagnostics.Debug.WriteLine("   Returning original image");
            return imageBytes;
        }
    }

    /// <summary>
    /// Helper method to resize image with a specific scale factor using SkiaSharp
    /// </summary>
    private async Task<byte[]> ResizeImageWithScaleAsync(byte[] imageBytes, double scaleFactor)
    {
        try
        {
            using var originalBitmap = SKBitmap.Decode(imageBytes);

            if (originalBitmap == null)
            {
                return imageBytes;
            }

            int newWidth = (int)(originalBitmap.Width * scaleFactor);
            int newHeight = (int)(originalBitmap.Height * scaleFactor);

            using var resizedBitmap = originalBitmap.Resize(
                new SKImageInfo(newWidth, newHeight),
                SKFilterQuality.High);

            if (resizedBitmap == null)
            {
                return imageBytes;
            }

            using var image = SKImage.FromBitmap(resizedBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, 85); // 85% quality

            if (data == null)
            {
                return imageBytes;
            }

            return data.ToArray();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Scale resize failed: {ex.Message}");
            return imageBytes;
        }
    }

    /// <summary>
    /// Save OCR results using FileStorageService
    /// </summary>
    private async Task SaveOcrResultsAsync(string extractedText, string normalizedText, AnalyzeResult result)
    {
        if (!_fileStorageService.IsFileLoggingEnabled())
        {
            System.Diagnostics.Debug.WriteLine("📁 File logging is disabled, skipping save");
            return;
        }

        try
        {
            // Serialize Azure DI result to JSON
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            var jsonContent = JsonSerializer.Serialize(result, jsonOptions);

            // Save all files using FileStorageService
            var (rawPath, normalizedPath, jsonPath) = await _fileStorageService.SaveOcrResultsAsync(
                extractedText,
                normalizedText,
                jsonContent);

            if (!string.IsNullOrEmpty(rawPath))
            {
                System.Diagnostics.Debug.WriteLine($"✅ OCR results saved via FileStorageService:");
                System.Diagnostics.Debug.WriteLine($"   Raw: {Path.GetFileName(rawPath)}");
                System.Diagnostics.Debug.WriteLine($"   Normalized: {Path.GetFileName(normalizedPath)}");
                System.Diagnostics.Debug.WriteLine($"   JSON: {Path.GetFileName(jsonPath)}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Failed to save OCR results: {ex.Message}");
            // Don't throw - saving logs is optional and shouldn't break the main flow
        }
    }

    private string ExtractTextFromResult(AnalyzeResult result)
    {
        var textBuilder = new StringBuilder();

        try
        {
            System.Diagnostics.Debug.WriteLine("📄 Azure DI: Extracting text from result...");
            System.Diagnostics.Debug.WriteLine($"   Pages: {result.Pages.Count}");

            // Method 1: Extract from pages and lines (most reliable for prescriptions)
            foreach (DocumentPage page in result.Pages)
            {
                System.Diagnostics.Debug.WriteLine($"📄 Page {page.PageNumber}: {page.Lines.Count} line(s), {page.Words.Count} word(s)");

                for (int i = 0; i < page.Lines.Count; i++)
                {
                    DocumentLine line = page.Lines[i];

                    if (!string.IsNullOrEmpty(line.Content))
                    {
                        textBuilder.AppendLine(line.Content);

                        // Log line details for debugging
                        System.Diagnostics.Debug.WriteLine($"  Line {i}: '{line.Content}'");

                        // Optional: Log bounding box for spatial analysis
                        if (line.Polygon != null && line.Polygon.Count >= 8)
                        {
                            System.Diagnostics.Debug.WriteLine($"    Bounding box: " +
                                $"UL({line.Polygon[0]},{line.Polygon[1]}) " +
                                $"UR({line.Polygon[2]},{line.Polygon[3]}) " +
                                $"LR({line.Polygon[4]},{line.Polygon[5]}) " +
                                $"LL({line.Polygon[6]},{line.Polygon[7]})");
                        }
                    }
                }
            }

            // Check for handwritten content (important for doctor's signatures/notes)
            if (result.Styles != null && result.Styles.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"📝 Checking {result.Styles.Count} style(s) for handwritten content...");

                foreach (DocumentStyle style in result.Styles)
                {
                    bool isHandwritten = style.IsHandwritten.HasValue && style.IsHandwritten == true;

                    if (isHandwritten && style.Confidence > 0.8)
                    {
                        System.Diagnostics.Debug.WriteLine($"✍️ Handwritten content detected (confidence: {style.Confidence:P0}):");

                        if (style.Spans != null)
                        {
                            foreach (DocumentSpan span in style.Spans)
                            {
                                if (!string.IsNullOrEmpty(result.Content) &&
                                    span.Offset < result.Content.Length)
                                {
                                    int length = Math.Min(span.Length, result.Content.Length - span.Offset);
                                    string handwrittenText = result.Content.Substring(span.Offset, length);
                                    System.Diagnostics.Debug.WriteLine($"  Handwritten: '{handwrittenText}'");

                                    // Add marker for handwritten content
                                    textBuilder.AppendLine($"[Handwritten: {handwrittenText}]");
                                }
                            }
                        }
                    }
                }
            }

            // Detect document language(s)
            if (result.Languages != null && result.Languages.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"🌐 Detected {result.Languages.Count} language(s):");

                foreach (DocumentLanguage language in result.Languages)
                {
                    System.Diagnostics.Debug.WriteLine($"  Language: '{language.Locale}' (confidence: {language.Confidence:P0})");
                }
            }

            var extractedText = textBuilder.ToString();

            // Log extraction summary
            System.Diagnostics.Debug.WriteLine($"✅ Text extraction complete:");
            System.Diagnostics.Debug.WriteLine($"   Total characters: {extractedText.Length}");
            System.Diagnostics.Debug.WriteLine($"   Total lines: {result.Pages.Sum(p => p.Lines?.Count ?? 0)}");
            System.Diagnostics.Debug.WriteLine($"   Total words: {result.Pages.Sum(p => p.Words?.Count ?? 0)}");

            // Log preview of extracted text
            if (extractedText.Length > 0)
            {
                var preview = extractedText.Substring(0, Math.Min(300, extractedText.Length));
                System.Diagnostics.Debug.WriteLine($"📄 Text preview:");
                System.Diagnostics.Debug.WriteLine($"   {preview}...");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Warning: No text was extracted from the document");
            }

            return extractedText;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Azure DI: Error extracting text: {ex.Message}");
            throw;
        }
    }
}
