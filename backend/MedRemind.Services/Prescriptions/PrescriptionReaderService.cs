using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MedRemind.Core.DTOs;
using MedRemind.Core.Enums;
using MedRemind.Core.Interfaces;
using MedRemind.Core.Models;
using MedRemind.Services.AI;
using MedRemind.Services.AI.Agents;

namespace MedRemind.Services.Prescriptions;

public class PrescriptionReaderService : IPrescriptionReaderService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly IValidationAgentService _validationAgent;
    private readonly AzureDocumentIntelligenceService _azureDocService;
    private readonly MultiLlmAPIOrchestrator _multiLlmAPIOrchestrator;
    private readonly PrescriptionDeduplicationService? _deduplicationService;
    private readonly PrescriptionService? _prescriptionService;
    private readonly IUnitOfWork? _unitOfWork;

    public PrescriptionReaderService(
        HttpClient httpClient,
        string apiKey,
        IValidationAgentService validationAgent,
        AzureDocumentIntelligenceService azureDocService,
        MultiLlmAPIOrchestrator multiLlmAPIOrchestrator,
        PrescriptionDeduplicationService? deduplicationService = null,
        PrescriptionService? prescriptionService = null,
        IUnitOfWork? unitOfWork = null)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _validationAgent = validationAgent;
        _azureDocService = azureDocService;
        _multiLlmAPIOrchestrator = multiLlmAPIOrchestrator ?? throw new ArgumentNullException(nameof(multiLlmAPIOrchestrator));
        _deduplicationService = deduplicationService;
        _prescriptionService = prescriptionService;
        _unitOfWork = unitOfWork;
        
        System.Diagnostics.Debug.WriteLine($"✅ PrescriptionReaderService initialized with MultiLlmAPIOrchestrator");
        System.Diagnostics.Debug.WriteLine($"   UnitOfWork: {(_unitOfWork != null ? "Available" : "Not Available")}");
    }

    public async Task<PrescriptionReadResult> ReadPrescriptionAsync(
        string imagePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Read image and convert to base64
            var imageBytes = await File.ReadAllBytesAsync(imagePath, cancellationToken);
            var base64Image = Convert.ToBase64String(imageBytes);

            return await ReadPrescriptionFromBase64Async(base64Image, cancellationToken);
        }
        catch (Exception ex)
        {
            return new PrescriptionReadResult
            {
                Success = false,
                ErrorMessage = $"Error reading prescription: {ex.Message}"
            };
        }
    }

    public async Task<PrescriptionReadResult> ReadPrescriptionFromBase64Async(
        string base64Image,
        CancellationToken cancellationToken = default)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine(" Prescription Processing: Starting...");
            System.Diagnostics.Debug.WriteLine($"   Image size: {base64Image?.Length ?? 0} bytes");

            if (string.IsNullOrEmpty(base64Image))
            {
                System.Diagnostics.Debug.WriteLine("? Image is empty!");
                return new PrescriptionReadResult
                {
                    Success = false,
                    ErrorMessage = "Image data is empty"
                };
            }

            // Step 1: Extract text using Azure Document Intelligence
            System.Diagnostics.Debug.WriteLine(" Step 1: Extracting text with Azure Document Intelligence...");
            string extractedText;

            try
            {
                extractedText = await _azureDocService.ExtractTextFromImageAsync(base64Image, null, cancellationToken);
                System.Diagnostics.Debug.WriteLine($"? Text extracted: {extractedText.Length} characters");
                System.Diagnostics.Debug.WriteLine($"   Preview: {extractedText.Substring(0, Math.Min(200, extractedText.Length))}...");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Azure DI failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine("?? Fallback: Using OpenAI Vision API...");

                // Fallback to OpenAI Vision if Azure DI fails
                throw ex;
            }

            // Step 2: Process with AgentOrchestrator V2
            System.Diagnostics.Debug.WriteLine("⚡ Step 2: Processing with AgentOrchestrator V2...");

            if (string.IsNullOrEmpty(_apiKey))
            {
                System.Diagnostics.Debug.WriteLine("❌ OpenAI: API key is empty!");
                return new PrescriptionReadResult
                {
                    Success = false,
                    ErrorMessage = "OpenAI API key is not configured"
                };
            }

            try
            {
                // Use orchestrator for comprehensive processing
                var orchestratorResult = await _multiLlmAPIOrchestrator.ProcessPrescriptionAsync(
                    extractedText,
                    "prescription_image.jpg",
                    0); // Temp ID for non-database processing

                if (!orchestratorResult.Success || orchestratorResult.ParseResult == null)
                {
                    return new PrescriptionReadResult
                    {
                        Success = false,
                        ErrorMessage = orchestratorResult.ErrorMessage ?? "Failed to parse prescription"
                    };
                }

                var parseResult = orchestratorResult.ParseResult;

                System.Diagnostics.Debug.WriteLine($"✅ Orchestrator V2: Success");
                System.Diagnostics.Debug.WriteLine($"   Patient: {parseResult.Patient?.Name ?? "N/A"}");
                System.Diagnostics.Debug.WriteLine($"   Doctor: {parseResult.Doctor?.Name ?? "N/A"}");
                System.Diagnostics.Debug.WriteLine($"   Medications: {parseResult.Medications.Count}");
                System.Diagnostics.Debug.WriteLine($"   Provider: {orchestratorResult.SelectedProvider}");
                System.Diagnostics.Debug.WriteLine($"   Match Score: {orchestratorResult.MatchScore:P0}");

                // Create result from parsed data
                var result = new PrescriptionReadResult
                {
                    Success = true,
                    Doctor = parseResult.Doctor,
                    PrescriptionDate = parseResult.PrescriptionDate,
                    Medications = parseResult.Medications,
                    ConfidenceScore = orchestratorResult.MatchScore
                };

                if (parseResult.Medications.Any())
                {
                    foreach (var med in parseResult.Medications)
                    {
                        System.Diagnostics.Debug.WriteLine($"   - {med.Name}: {med.Dosage} {med.Unit}, {med.Frequency}");
                    }

                    // Validate medications
                    System.Diagnostics.Debug.WriteLine("🔍 Validation: Starting medication validation...");

                    var warnings = await _validationAgent.ValidateMedicationsAsync(
                        result.Medications,
                        cancellationToken);

                    result.ValidationWarnings = warnings;
                    System.Diagnostics.Debug.WriteLine($"✅ Validation: Complete. Warnings: {warnings.Count}");
                }

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Orchestrator V2 failed: {ex.Message}");
                return new PrescriptionReadResult
                {
                    Success = false,
                    ErrorMessage = $"Processing failed: {ex.Message}"
                };
            }
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"? HTTP error: {ex.Message}");
            return new PrescriptionReadResult
            {
                Success = false,
                ErrorMessage = $"Network error: {ex.Message}"
            };
        }
        catch (TaskCanceledException ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Timeout: {ex.Message}");
            return new PrescriptionReadResult
            {
                Success = false,
                ErrorMessage = "Request timed out. Please try again."
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Unexpected error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"   Stack trace: {ex.StackTrace}");
            return new PrescriptionReadResult
            {
                Success = false,
                ErrorMessage = $"Error processing prescription: {ex.Message}"
            };
        }
    }

    private double CalculateOverallConfidence(List<MedicationData> medications)
    {
        if (!medications.Any())
            return 0.0;

        return medications.Average(m => (double)m.ConfidenceScore);
    }

    /// <summary>
    /// Comprehensive prescription processing with AgentOrchestrator V2, deduplication, and database integration
    /// </summary>
    /// <param name="imageBase64">Base64 encoded image data</param>
    /// <param name="imagePath">Relative path where image is stored</param>
    /// <param name="uniqueFileName">Unique file name generated by storage service (e.g., user_1_20250129_143022.jpg)</param>
    /// <param name="originalFileName">Original file name uploaded by user (for extension/display)</param>
    /// <param name="userId">User ID who owns the prescription</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<PrescriptionProcessingResult> ProcessPrescriptionComprehensiveAsync(
        string imageBase64,
        string? imagePath,
        string uniqueFileName,
        string? originalFileName,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var result = new PrescriptionProcessingResult();
        
        try
        {
            System.Diagnostics.Debug.WriteLine("🚀 ProcessPrescriptionComprehensive: Starting with AgentOrchestrator V2...");
            System.Diagnostics.Debug.WriteLine($"   Image size: {imageBase64.Length} bytes ({(imageBase64.Length / 1024.0):F2} KB)");
            System.Diagnostics.Debug.WriteLine($"   User ID: {userId}");
            System.Diagnostics.Debug.WriteLine($"   Unique file name: {uniqueFileName}");
            System.Diagnostics.Debug.WriteLine($"   Original file name: {originalFileName ?? "N/A"}");

            // Validate services are available
            if (_deduplicationService == null || _prescriptionService == null)
            {
                result.Success = false;
                result.ErrorMessage = "Required services not configured. Cannot process prescription.";
                System.Diagnostics.Debug.WriteLine("❌ Missing required services (dedup or prescription service)");
                return result;
            }

            // Step 1: Create prescription record
            var prescription = new Prescription
            {
                UserId = userId,
                ImagePath = imagePath ?? uniqueFileName,
                FileName = originalFileName ?? uniqueFileName, // Store original file name
                FileSize = imageBase64.Length * 3 / 4, // Approximate bytes from base64 (base64 is ~33% larger)
                PrescriptionDate = DateTime.UtcNow,
                Status = "Processing",
                CreatedAt = DateTime.UtcNow
            };

            System.Diagnostics.Debug.WriteLine($"💾 Saving prescription for user {userId}");
            System.Diagnostics.Debug.WriteLine($"   File name: {prescription.FileName}");
            System.Diagnostics.Debug.WriteLine($"   File size: {prescription.FileSize / 1024.0:F2} KB");
            prescription = await _prescriptionService.AddPrescriptionAsync(prescription);
            result.PrescriptionId = prescription.Id;
            System.Diagnostics.Debug.WriteLine($"✅ Prescription saved with ID: {prescription.Id}");

            // Step 2: Extract OCR text using Azure Document Intelligence
            System.Diagnostics.Debug.WriteLine("📄 Extracting OCR text...");
            var ocrText = await _azureDocService.ExtractTextFromImageAsync(
                imageBase64, 
                uniqueFileName, 
                cancellationToken);
            System.Diagnostics.Debug.WriteLine($"✅ OCR text extracted: {ocrText?.Length ?? 0} characters");

            if (string.IsNullOrWhiteSpace(ocrText))
            {
                result.Success = false;
                result.ErrorMessage = "Failed to extract text from image. Please ensure the image is clear and contains text.";
                await _prescriptionService.UpdatePrescriptionStatusAsync(
                    prescription.Id, "Failed");
                return result;
            }

            // Step 2.5: Create early OCR result entry with "OcrComplete" status
            System.Diagnostics.Debug.WriteLine("💾 Creating early OCR result entry...");
            await CreateEarlyOcrResultEntryAsync(prescription.Id, ocrText);
            System.Diagnostics.Debug.WriteLine("✅ Early OCR result entry created (Status: OcrComplete)");

            // Step 3: Check for duplicate prescription
            System.Diagnostics.Debug.WriteLine("\n🔍 Checking for duplicate prescription...");
            var duplicateCheck = await _deduplicationService.CheckForDuplicateAsync(ocrText, userId);

            if (duplicateCheck.IsDuplicate && duplicateCheck.ExistingResult != null)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ DUPLICATE FOUND!");
                System.Diagnostics.Debug.WriteLine($"   Similarity: {duplicateCheck.SimilarityScore:P0}");
                System.Diagnostics.Debug.WriteLine($"   Existing Prescription ID: {duplicateCheck.ExistingResult.PrescriptionId}");
                
                result.IsDuplicate = true;
                result.DuplicateMessage = duplicateCheck.Message;
                result.SimilarityScore = duplicateCheck.SimilarityScore;
                result.ExistingPrescriptionId = duplicateCheck.ExistingResult.PrescriptionId;
                result.ExistingProcessedDate = duplicateCheck.ExistingResult.ProcessedAt;

                // Load existing result
                var existingParseResult = JsonSerializer.Deserialize<PrescriptionReadResult>(
                    duplicateCheck.ExistingResult.SelectedResponse ?? "{}");

                if (existingParseResult != null && existingParseResult.Medications.Any())
                {
                    result.Success = true;
                    result.PrescriptionResult = new PrescriptionReadResult
                    {
                        Success = true,
                        Doctor = existingParseResult.Doctor,
                        PrescriptionDate = existingParseResult.PrescriptionDate,
                        Medications = existingParseResult.Medications,
                        ConfidenceScore = duplicateCheck.ExistingResult.ComparisonScore
                    };

                    // Update current prescription to reference existing result
                    prescription.DoctorName = result.PrescriptionResult.Doctor?.Name;
                    if (result.PrescriptionResult.PrescriptionDate.HasValue)
                        prescription.PrescriptionDate = result.PrescriptionResult.PrescriptionDate.Value;

                    await _prescriptionService.UpdatePrescriptionStatusAsync(
                        prescription.Id,
                        "Processed");

                    System.Diagnostics.Debug.WriteLine("✅ Returning existing result (duplicate detected)");
                    return result;
                }
            }

            // Step 4: Process with _multiLlmAPIOrchestrator  using unique file name for OCR file mapping
            System.Diagnostics.Debug.WriteLine("\n⚡ Starting MultiLlmAPIOrchestrator V2 (Parallel Mode)");

            var orchestratorResult = await _multiLlmAPIOrchestrator.ProcessPrescriptionAsync(
                ocrText,
                uniqueFileName, // Use unique file name for OCR file mapping
                prescription.Id);

            result.ProcessingAttempts = orchestratorResult.TotalAttempts;
            result.MatchScore = orchestratorResult.MatchScore;
            result.SelectedProvider = orchestratorResult.SelectedProvider;
            result.ProcessingTime = orchestratorResult.ProcessingTime;
            result.WarningMessage = orchestratorResult.WarningMessage;

            System.Diagnostics.Debug.WriteLine($"\n📊 Orchestrator V2 Results:");
            System.Diagnostics.Debug.WriteLine($"   Success: {orchestratorResult.Success}");
            System.Diagnostics.Debug.WriteLine($"   Parsers Used: {orchestratorResult.SelectedProvider}");
            System.Diagnostics.Debug.WriteLine($"   Total Attempts: {orchestratorResult.TotalAttempts}");
            System.Diagnostics.Debug.WriteLine($"   Match Score: {orchestratorResult.MatchScore:P0}");
            System.Diagnostics.Debug.WriteLine($"   Processing Time: {orchestratorResult.ProcessingTime.TotalSeconds:F2}s");

            if (orchestratorResult.Success && orchestratorResult.ParseResult != null)
            {
                var parseResult = orchestratorResult.ParseResult;

                result.Success = true;
                result.PrescriptionResult = new PrescriptionReadResult
                {
                    Success = parseResult.Success,
                    Doctor = parseResult.Doctor,
                    PrescriptionDate = parseResult.PrescriptionDate,
                    Medications = parseResult.Medications,
                    ConfidenceScore = orchestratorResult.MatchScore
                };

                System.Diagnostics.Debug.WriteLine($"✅ AI processing complete. Medications: {parseResult.Medications?.Count ?? 0}");

                if (parseResult.Medications.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"💊 Extracted {parseResult.Medications.Count} medications:");
                    foreach (var med in parseResult.Medications)
                    {
                        System.Diagnostics.Debug.WriteLine($"   • {med.Name} - {med.Dosage} {med.Unit} - {med.Frequency}");
                    }

                    // Update prescription with doctor and date info
                    if (!string.IsNullOrEmpty(parseResult.Doctor.Name))
                    {
                        prescription.DoctorName = parseResult.Doctor.Name;
                        System.Diagnostics.Debug.WriteLine($"   Doctor: {parseResult.Doctor.Name}");
                    }
                    if (parseResult.PrescriptionDate.HasValue)
                    {
                        prescription.PrescriptionDate = parseResult.PrescriptionDate.Value;
                        System.Diagnostics.Debug.WriteLine($"   Date: {parseResult.PrescriptionDate.Value:yyyy-MM-dd}");
                    }

                    // Update prescription status
                    await _prescriptionService.UpdatePrescriptionStatusAsync(
                        prescription.Id,
                        "Processed");

                    System.Diagnostics.Debug.WriteLine($"✅ Prescription status updated to Processed");
                }
                else
                {
                    result.WarningMessage = "No medications found in the prescription.";
                    await _prescriptionService.UpdatePrescriptionStatusAsync(
                        prescription.Id, "Processed");
                }
            }
            else
            {
                // AI processing failed
                result.Success = false;
                result.ErrorMessage = orchestratorResult.ErrorMessage ?? "Failed to extract medications. Please try again with a clearer image.";

                await _prescriptionService.UpdatePrescriptionStatusAsync(
                    prescription.Id,
                    "Failed");

                System.Diagnostics.Debug.WriteLine($"❌ AI processing failed: {orchestratorResult.ErrorMessage}");
            }

            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Exception in ProcessPrescriptionComprehensive: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"   Type: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"   Stack trace: {ex.StackTrace}");

            result.Success = false;
            result.ErrorMessage = $"Error: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// Create early OCR result entry with "OcrComplete" status
    /// This allows tracking of OCR processing before AI analysis completes
    /// </summary>
    private async Task CreateEarlyOcrResultEntryAsync(int prescriptionId, string ocrText)
    {
        try
        {
            if (_unitOfWork == null)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ UnitOfWork not available - skipping early OCR entry creation");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"💾 Creating early OCR result entry for prescription {prescriptionId}...");

            // Calculate hash for duplicate detection
            var hash = ComputeHash(ocrText);

            var ocrResult = new PrescriptionOCRResult
            {
                PrescriptionId = prescriptionId,
                OCRText = ocrText,
                OCRTextHash = hash,
                Status = OcrProcessingStatus.OcrComplete, // OCR complete, AI processing next
                ProcessedAt = DateTime.UtcNow,
                ProcessingTime = TimeSpan.Zero, // Will be updated later
                ProcessingAttempts = 1
            };

            var repository = _unitOfWork.Repository<PrescriptionOCRResult>();
            await repository.AddAsync(ocrResult);
            await _unitOfWork.SaveChangesAsync();

            System.Diagnostics.Debug.WriteLine($"✅ Early OCR result entry created - ID: {ocrResult.Id}");
            System.Diagnostics.Debug.WriteLine($"   Status: {ocrResult.Status}");
            System.Diagnostics.Debug.WriteLine($"   OCR Text Length: {ocrText.Length} characters");
            System.Diagnostics.Debug.WriteLine($"   Hash: {hash.Substring(0, 16)}...");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Failed to create early OCR result entry: {ex.Message}");
            // Don't fail the entire process if early entry creation fails
        }
    }

    /// <summary>
    /// Compute SHA256 hash of OCR text for duplicate detection
    /// </summary>
    private string ComputeHash(string text)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
        return BitConverter.ToString(hashBytes).Replace("-", "");
    }
}
