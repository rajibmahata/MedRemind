using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using System.ComponentModel;

namespace MedRemind.Services.AI.Agents;

/// <summary>
/// Agent 1: Saves OCR text to file system with prescription filename
/// </summary>
public class OCRTextSaverAgent
{
    private readonly string _storageBasePath;

    public OCRTextSaverAgent(string storageBasePath)
    {
        _storageBasePath = storageBasePath ?? throw new ArgumentNullException(nameof(storageBasePath));
        
        // Ensure storage directory exists
        if (!Directory.Exists(_storageBasePath))
        {
            Directory.CreateDirectory(_storageBasePath);
        }
    }

    /// <summary>
    /// Save OCR text with the same filename as the prescription image
    /// Implements deduplication: if file exists with same content, delete duplicate; if different, rename old and create new
    /// </summary>
    [KernelFunction("save_ocr_text")]
    [Description("Saves OCR extracted text to a file with the same name as the prescription image")]
    public async Task<OCRSaveResult> SaveOCRTextAsync(
        [Description("The OCR extracted text from prescription image")] string ocrText,
        [Description("Original prescription filename (e.g., 'prescription_001.jpg')")] string prescriptionFileName)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("?? OCR Saver Agent: Starting...");
            System.Diagnostics.Debug.WriteLine($"   Prescription file: {prescriptionFileName}");
            System.Diagnostics.Debug.WriteLine($"   OCR text length: {ocrText?.Length ?? 0} characters");

            if (string.IsNullOrWhiteSpace(ocrText))
            {
                return new OCRSaveResult
                {
                    Success = false,
                    ErrorMessage = "OCR text is empty"
                };
            }

            if (string.IsNullOrWhiteSpace(prescriptionFileName))
            {
                return new OCRSaveResult
                {
                    Success = false,
                    ErrorMessage = "Prescription filename is empty"
                };
            }

            // Generate OCR text filename (replace image extension with .txt)
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(prescriptionFileName);
            var ocrFileName = $"{fileNameWithoutExtension}_ocr.txt";
            var ocrFilePath = Path.Combine(_storageBasePath, ocrFileName);

            // ============================================
            // NEW: Check for existing file and compare content
            // ============================================
            if (File.Exists(ocrFilePath))
            {
                System.Diagnostics.Debug.WriteLine($"   ?? File already exists: {ocrFileName}");
                
                // Read existing OCR text
                var existingOcrText = await File.ReadAllTextAsync(ocrFilePath);
                
                // Normalize texts for comparison (remove extra whitespace, normalize line endings)
                var normalizedNew = NormalizeText(ocrText);
                var normalizedExisting = NormalizeText(existingOcrText);
                
                // Calculate similarity
                var similarity = CalculateSimilarity(normalizedNew, normalizedExisting);
                System.Diagnostics.Debug.WriteLine($"   ?? Content similarity: {similarity:P0}");
                
                if (similarity >= 0.95) // 95% or more similar = same content
                {
                    System.Diagnostics.Debug.WriteLine($"   ? Content is identical (similarity: {similarity:P0})");
                    System.Diagnostics.Debug.WriteLine($"   ??? Deleting duplicate - keeping existing file");
                    
                    // Content is the same - no need to save again
                    return new OCRSaveResult
                    {
                        Success = true,
                        SavedFilePath = ocrFilePath,
                        MetadataFilePath = Path.Combine(_storageBasePath, $"{fileNameWithoutExtension}_metadata.json"),
                        TextLength = ocrText.Length,
                        IsDuplicate = true,
                        DuplicateAction = "Kept existing file"
                    };
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"   ?? Content is different (similarity: {similarity:P0})");
                    System.Diagnostics.Debug.WriteLine($"   ?? Renaming old file and creating new one");
                    
                    // Content is different - rename old file with timestamp
                    var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                    var oldFileName = $"{fileNameWithoutExtension}_ocr_{timestamp}_old.txt";
                    var oldFilePath = Path.Combine(_storageBasePath, oldFileName);
                    
                    // Rename existing file
                    File.Move(ocrFilePath, oldFilePath);
                    System.Diagnostics.Debug.WriteLine($"   ?? Old file renamed to: {oldFileName}");
                    
                    // Also rename old metadata if exists
                    var oldMetadataPath = Path.Combine(_storageBasePath, $"{fileNameWithoutExtension}_metadata.json");
                    if (File.Exists(oldMetadataPath))
                    {
                        var oldMetadataFileName = $"{fileNameWithoutExtension}_metadata_{timestamp}_old.json";
                        var newOldMetadataPath = Path.Combine(_storageBasePath, oldMetadataFileName);
                        File.Move(oldMetadataPath, newOldMetadataPath);
                        System.Diagnostics.Debug.WriteLine($"   ?? Old metadata renamed to: {oldMetadataFileName}");
                    }
                }
            }

            // Save OCR text to file
            await File.WriteAllTextAsync(ocrFilePath, ocrText);

            // Also save metadata
            var metadata = new
            {
                OriginalFile = prescriptionFileName,
                OCRFile = ocrFileName,
                ExtractedAt = DateTime.UtcNow,
                TextLength = ocrText.Length,
                LineCount = ocrText.Split('\n').Length,
                WordCount = ocrText.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length
            };

            var metadataFileName = $"{fileNameWithoutExtension}_metadata.json";
            var metadataFilePath = Path.Combine(_storageBasePath, metadataFileName);
            await File.WriteAllTextAsync(metadataFilePath, 
                System.Text.Json.JsonSerializer.Serialize(metadata, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

            System.Diagnostics.Debug.WriteLine($"? OCR Saver Agent: Success");
            System.Diagnostics.Debug.WriteLine($"   Saved to: {ocrFilePath}");
            System.Diagnostics.Debug.WriteLine($"   Metadata: {metadataFilePath}");

            return new OCRSaveResult
            {
                Success = true,
                SavedFilePath = ocrFilePath,
                MetadataFilePath = metadataFilePath,
                TextLength = ocrText.Length,
                IsDuplicate = false,
                DuplicateAction = null
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? OCR Saver Agent: Error - {ex.Message}");
            return new OCRSaveResult
            {
                Success = false,
                ErrorMessage = $"Failed to save OCR text: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Normalize text for comparison by removing extra whitespace and standardizing line endings
    /// </summary>
    private string NormalizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Normalize line endings
        text = text.Replace("\r\n", "\n").Replace("\r", "\n");
        
        // Remove extra whitespace
        var lines = text.Split('\n')
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line));
        
        return string.Join("\n", lines).ToLowerInvariant();
    }

    /// <summary>
    /// Calculate similarity between two texts using simple character-based comparison
    /// Returns value between 0.0 (completely different) and 1.0 (identical)
    /// </summary>
    private double CalculateSimilarity(string text1, string text2)
    {
        if (string.IsNullOrEmpty(text1) && string.IsNullOrEmpty(text2))
            return 1.0;
        
        if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
            return 0.0;

        // Simple approach: compare character by character
        int maxLength = Math.Max(text1.Length, text2.Length);
        int minLength = Math.Min(text1.Length, text2.Length);
        
        int matchingChars = 0;
        for (int i = 0; i < minLength; i++)
        {
            if (text1[i] == text2[i])
                matchingChars++;
        }
        
        return (double)matchingChars / maxLength;
    }
}

/// <summary>
/// Result from OCR save operation
/// </summary>
public class OCRSaveResult
{
    public bool Success { get; set; }
    public string? SavedFilePath { get; set; }
    public string? MetadataFilePath { get; set; }
    public int TextLength { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsDuplicate { get; set; } // NEW: Indicates if this was a duplicate
    public string? DuplicateAction { get; set; } // NEW: What action was taken (e.g., "Kept existing file", "Renamed old file")
}
