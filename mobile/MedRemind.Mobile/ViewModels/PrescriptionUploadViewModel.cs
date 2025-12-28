using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedRemind.Core.DTOs;
using MedRemind.Core.Interfaces;
using MedRemind.Core.Models;
using MedRemind.Services.Medications;
using MedRemind.Services.Prescriptions;
using MedRemind.Services.AI.Agents;
using MedRemind.Services.AI; // NEW: Add this for PrescriptionParseResult

namespace MedRemind.Mobile.ViewModels;

public partial class PrescriptionUploadViewModel : BaseViewModel
{
    private readonly IPrescriptionReaderService _prescriptionReader;
    private readonly IValidationAgentService _validationAgent;
    private readonly PrescriptionService _prescriptionService;
    private readonly MedicationService _medicationService;
    private readonly IReminderSchedulingService _reminderScheduling;
    private readonly AgentOrchestrator _agentOrchestrator;
    private readonly PrescriptionDeduplicationService _deduplicationService; // NEW
    private readonly AzureDocumentIntelligenceService _azureDocumentIntelligenceService; // NEW

    [ObservableProperty]
    private ImageSource? _selectedImage;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private bool _hasResult;

    [ObservableProperty]
    private PrescriptionReadResult? _result;

    [ObservableProperty]
    private ObservableCollection<MedicationData> _extractedMedications = new();

    [ObservableProperty]
    private string _resultMessage = string.Empty;

    [ObservableProperty]
    private double _confidenceScore;

    [ObservableProperty]
    private int _processingAttempts; // NEW: Show number of AI attempts

    [ObservableProperty]
    private double _matchScore; // NEW: Show structure match score

    private string? _imageBase64;
    private string? _imagePath;

    public PrescriptionUploadViewModel(
        IPrescriptionReaderService prescriptionReader,
        IValidationAgentService validationAgent,
        PrescriptionService prescriptionService,
        MedicationService medicationService,
        IReminderSchedulingService reminderScheduling,
        AgentOrchestrator agentOrchestrator,
        PrescriptionDeduplicationService deduplicationService,
        AzureDocumentIntelligenceService azureDocumentIntelligenceService) // NEW
    {
        _prescriptionReader = prescriptionReader;
        _validationAgent = validationAgent;
        _prescriptionService = prescriptionService;
        _medicationService = medicationService;
        _reminderScheduling = reminderScheduling;
        _agentOrchestrator = agentOrchestrator;
        _deduplicationService = deduplicationService; // NEW
        _azureDocumentIntelligenceService = azureDocumentIntelligenceService; // NEW
        Title = "Upload Prescription";
    }

    public async void OnAppearing()
    {
        // User is authenticated by AppShell - verify session
        try
        {
            var userId = await SecureStorage.GetAsync("user_id");
            var sessionToken = await SecureStorage.GetAsync("session_token");
            
            System.Diagnostics.Debug.WriteLine($"?? PrescriptionUpload: Page loaded");
            System.Diagnostics.Debug.WriteLine($"   User ID: {userId ?? "NULL"}");
            System.Diagnostics.Debug.WriteLine($"   Session Token: {(string.IsNullOrEmpty(sessionToken) ? "NULL" : "EXISTS")}");
            
            if (string.IsNullOrEmpty(userId))
            {
                System.Diagnostics.Debug.WriteLine("?? Warning: user_id is empty in SecureStorage!");
                System.Diagnostics.Debug.WriteLine("?? User needs to login first!");
                
                // Show alert to user
                var page = GetCurrentPage();
                if (page != null)
                {
                    await page.DisplayAlertAsync(
                        "Login Required",
                        "Please login to upload and process prescriptions.",
                        "OK"
                    );
                    
                    // Navigate to login page
                    await Shell.Current.GoToAsync("///LoginPage");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error in OnAppearing: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task TakePhotoAsync()
    {
        try
        {
            if (MediaPicker.Default.IsCaptureSupported)
            {
                var photo = await MediaPicker.Default.CapturePhotoAsync();
                await ProcessPhotoAsync(photo);
            }
            else
            {
                var page = GetCurrentPage();
                if (page != null)
                {
                    await page.DisplayAlertAsync(
                        "Not Supported",
                        "Camera is not available on this device",
                        "OK");
                }
            }
        }
        catch (PermissionException)
        {
            ShowError("Camera permission denied. Please enable camera access in settings.");
        }
        catch (Exception ex)
        {
            ShowError($"Error taking photo: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task PickPhotoAsync()
    {
        try
        {
            var result = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Select prescription image"
            });
            
            await ProcessPhotoAsync(result);
        }
        catch (PermissionException)
        {
            ShowError("Storage permission denied. Please enable storage access in settings.");
        }
        catch (Exception ex)
        {
            ShowError($"Error picking photo: {ex.Message}");
        }
    }

    private async Task ProcessPhotoAsync(FileResult? photo)
    {
        if (photo == null) return;

        try
        {
            // Save image to local storage
            var localFilePath = Path.Combine(FileSystem.CacheDirectory, photo.FileName);
            using (var sourceStream = await photo.OpenReadAsync())
            using (var fileStream = File.Create(localFilePath))
            {
                await sourceStream.CopyToAsync(fileStream);
            }

            _imagePath = localFilePath;

            // Read and compress image before converting to base64
            var bytes = await File.ReadAllBytesAsync(localFilePath);
            
            System.Diagnostics.Debug.WriteLine($"📊 Original image: {bytes.Length / 1024.0:F2} KB");
            
            // Compress image if larger than 3MB target (Azure DI limit is 4MB)
            const int TARGET_SIZE_BYTES = 3 * 1024 * 1024; // 3MB target
            const int MAX_SIZE_BYTES = 4 * 1024 * 1024; // 4MB absolute limit
            
            if (bytes.Length > TARGET_SIZE_BYTES)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Image exceeds 3MB target, compressing...");
                bytes = await CompressImageAsync(bytes, TARGET_SIZE_BYTES, MAX_SIZE_BYTES);
                System.Diagnostics.Debug.WriteLine($"✅ Compressed to: {bytes.Length / 1024.0:F2} KB");
            }
            
            // Convert to base64 for AI processing
            _imageBase64 = Convert.ToBase64String(bytes);

            // Display preview
            SelectedImage = ImageSource.FromFile(localFilePath);
            
            var page = GetCurrentPage();
            if (page != null)
            {
                await page.DisplayAlertAsync(
                    "Photo Selected",
                    "Ready to process. Tap 'Process with AI' to extract medications.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Error processing photo: {ex.Message}");
        }
    }

    /// <summary>
    /// Compress image using platform-native APIs to stay under Azure Document Intelligence 4MB limit
    /// </summary>
    private async Task<byte[]> CompressImageAsync(byte[] imageBytes, int targetSize, int maxSize)
    {
        try
        {
#if ANDROID || IOS || MACCATALYST
            // Use Microsoft.Maui.Graphics for cross-platform image manipulation
            using var sourceStream = new MemoryStream(imageBytes);
            using var image = Microsoft.Maui.Graphics.Platform.PlatformImage.FromStream(sourceStream);
            
            if (image == null)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Failed to load image for compression, using original");
                return imageBytes;
            }

            System.Diagnostics.Debug.WriteLine($"📐 Original dimensions: {image.Width}x{image.Height}");

            // Calculate scale factor to target size
            double scaleFactor = Math.Sqrt((double)targetSize / imageBytes.Length);
            scaleFactor = Math.Min(scaleFactor, 0.9); // Cap at 90% to ensure size reduction

            int newWidth = (int)(image.Width * scaleFactor);
            int newHeight = (int)(image.Height * scaleFactor);

            System.Diagnostics.Debug.WriteLine($"📐 New dimensions: {newWidth}x{newHeight} (scale: {scaleFactor:P0})");

            // Resize image
            var resizedImage = image.Resize(newWidth, newHeight, ResizeMode.Fit, true);
            
            // Save as JPEG with quality 85%
            using var outputStream = new MemoryStream();
            await resizedImage.SaveAsync(outputStream, ImageFormat.Jpeg, 0.85f);
            
            var compressedBytes = outputStream.ToArray();
            System.Diagnostics.Debug.WriteLine($"✅ First pass: {compressedBytes.Length / 1024.0:F2} KB ({(1.0 - (double)compressedBytes.Length / imageBytes.Length):P0} reduction)");

            // If still too large, reduce quality further
            if (compressedBytes.Length > maxSize)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Still too large, reducing quality to 70%...");
                outputStream.SetLength(0);
                await resizedImage.SaveAsync(outputStream, ImageFormat.Jpeg, 0.70f);
                compressedBytes = outputStream.ToArray();
                System.Diagnostics.Debug.WriteLine($"✅ Second pass: {compressedBytes.Length / 1024.0:F2} KB");
            }

            resizedImage.Dispose();
            return compressedBytes;
#else
            // For other platforms or if compression fails, return original
            System.Diagnostics.Debug.WriteLine("⚠️ Image compression not available on this platform, using original");
            return imageBytes;
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Image compression failed: {ex.Message}");
            System.Diagnostics.Debug.WriteLine("   Using original image");
            return imageBytes; // Return original if compression fails
        }
    }

    [RelayCommand]
    private async Task ProcessPrescriptionAsync()
    {
        if (string.IsNullOrEmpty(_imageBase64))
        {
            ShowError("Please select a photo first");
            return;
        }

        IsProcessing = true;
        HasResult = false;
        ResultMessage = string.Empty;

        await ExecuteAsync(async () =>
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🚀 ProcessPrescription: Starting with Multi-Agent System...");
                System.Diagnostics.Debug.WriteLine($"   Image size: {_imageBase64.Length} bytes ({(_imageBase64.Length / 1024.0):F2} KB)");
                
                // Check SecureStorage state
                var userId_check = await SecureStorage.GetAsync("user_id");
                var sessionToken_check = await SecureStorage.GetAsync("session_token");
                System.Diagnostics.Debug.WriteLine($"   SecureStorage check - user_id: {userId_check ?? "NULL"}, session: {(string.IsNullOrEmpty(sessionToken_check) ? "NULL" : "EXISTS")}");

                // Check network connectivity
                var networkAccess = Connectivity.Current.NetworkAccess;
                System.Diagnostics.Debug.WriteLine($"   Network status: {networkAccess}");
                
                if (networkAccess != NetworkAccess.Internet)
                {
                    var page = GetCurrentPage();
                    if (page != null)
                    {
                        await page.DisplayAlertAsync(
                            "No Internet Connection",
                            "Please check your internet connection and try again.",
                            "OK"
                        );
                    }
                    return;
                }

                // Get user ID - user is already authenticated by AppShell
                var userId = await GetCurrentUserIdAsync();
                System.Diagnostics.Debug.WriteLine($"📝 Processing prescription for user ID: {userId}");

                // Save prescription to database
                var prescription = new Prescription
                {
                    UserId = userId,
                    ImagePath = _imagePath ?? "base64_image",
                    PrescriptionDate = DateTime.UtcNow,
                    Status = "Processing",
                    CreatedAt = DateTime.UtcNow
                };

                System.Diagnostics.Debug.WriteLine($"💾 Saving prescription for user {userId}");
                prescription = await _prescriptionService.AddPrescriptionAsync(prescription);
                System.Diagnostics.Debug.WriteLine($"✅ Prescription saved with ID: {prescription.Id}");

               
                // First, extract OCR text using existing service
                var ocrText = await _azureDocumentIntelligenceService.ExtractTextFromImageAsync(_imageBase64);
                
                System.Diagnostics.Debug.WriteLine($"📄 OCR text extracted: {ocrText?.Length ?? 0} characters");

                // If OCR extraction failed or returned empty, use OpenAI Vision directly
                if (string.IsNullOrWhiteSpace(ocrText) || ocrText.Length < 50)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ OCR text invalid - falling back to OpenAI Vision direct processing");
                    
                    // Use the existing prescription reader service which has OpenAI Vision fallback
                    var directResult = await _prescriptionReader.ReadPrescriptionFromBase64Async(_imageBase64);
                    
                    if (directResult.Success && directResult.Medications != null && directResult.Medications.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ OpenAI Vision direct processing successful");
                        System.Diagnostics.Debug.WriteLine($"   Medications: {directResult.Medications.Count}");
                        
                        Result = directResult;
                        ConfidenceScore = directResult.ConfidenceScore;
                        HasResult = true;
                        ExtractedMedications = new ObservableCollection<MedicationData>(directResult.Medications);
                        
                        // Update prescription with results
                        if (!string.IsNullOrEmpty(directResult.DoctorName))
                            prescription.DoctorName = directResult.DoctorName;
                        if (directResult.PrescriptionDate.HasValue)
                            prescription.PrescriptionDate = directResult.PrescriptionDate.Value;
                        
                        await _prescriptionService.UpdatePrescriptionStatusAsync(
                            prescription.Id,
                            "Processed",
                            null,
                            directResult.ConfidenceScore);
                        
                        ResultMessage = $"✅ Processing complete!\n\n" +
                                      $"📊 Quality: {directResult.ConfidenceScore:P0}\n" +
                                      $"💊 Medications: {directResult.Medications.Count}";
                        
                        var currentPage = GetCurrentPage();
                        if (currentPage != null)
                        {
                            await currentPage.DisplayAlertAsync(
                                "Success",
                                $"Found {directResult.Medications.Count} medication(s).\n\n" +
                                $"Confidence: {directResult.ConfidenceScore:P0}\n\n" +
                                $"Review and tap 'Save Medications' to continue.",
                                "OK"
                            );
                        }
                        
                        return; // Skip orchestrator
                    }
                    else
                    {
                        ResultMessage = "Failed to extract text from image. Please try with a clearer image.";
                        HasResult = true;
                        await _prescriptionService.UpdatePrescriptionStatusAsync(
                            prescription.Id, "Failed", "Both OCR and OpenAI Vision failed");
                        return;
                    }
                }

                // ================================================================
                // NEW: Check for Duplicate Prescription
                // ================================================================
                System.Diagnostics.Debug.WriteLine("\n🔍 Checking for duplicate prescription...");
                
                var duplicateCheck = await _deduplicationService.CheckForDuplicateAsync(ocrText, userId);
                
                if (duplicateCheck.IsDuplicate && duplicateCheck.ExistingResult != null)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ DUPLICATE FOUND!");
                    System.Diagnostics.Debug.WriteLine($"   Similarity: {duplicateCheck.SimilarityScore:P0}");
                    System.Diagnostics.Debug.WriteLine($"   Existing Prescription ID: {duplicateCheck.ExistingResult.PrescriptionId}");
                    System.Diagnostics.Debug.WriteLine($"   Processed: {duplicateCheck.ExistingResult.ProcessedAt:yyyy-MM-dd HH:mm}");
                    
                    // Show alert to user
                    var page = GetCurrentPage();
                    if (page != null)
                    {
                        var userResponse = await page.DisplayAlertAsync(
                            "Duplicate Prescription Detected",
                            $"{duplicateCheck.Message}\n\n" +
                            $"Previously processed: {duplicateCheck.ExistingResult.ProcessedAt:yyyy-MM-dd HH:mm}\n" +
                            $"Medications found: {duplicateCheck.ExistingResult.MedicationCount}\n" +
                            $"Doctor: {duplicateCheck.ExistingResult.DoctorName ?? "N/A"}\n\n" +
                            $"Do you want to use the existing result instead of processing again?",
                            "Use Existing",
                            "Process Anyway"
                        );
                        
                        if (userResponse)
                        {
                            System.Diagnostics.Debug.WriteLine("✅ User chose to use existing result");
                            
                            // Load existing result from JSON
                            var existingParseResult = System.Text.Json.JsonSerializer.Deserialize<PrescriptionParseResult>(
                                duplicateCheck.ExistingResult.SelectedResponse ?? "{}");
                            
                            if (existingParseResult != null && existingParseResult.Medications.Any())
                            {
                                // Convert to PrescriptionReadResult
                                var result = new PrescriptionReadResult
                                {
                                    Success = true,
                                    DoctorName = existingParseResult.Doctor?.Name,
                                    PrescriptionDate = existingParseResult.PrescriptionDate,
                                    Medications = existingParseResult.Medications,
                                    ConfidenceScore = duplicateCheck.ExistingResult.ComparisonScore
                                };
                                
                                Result = result;
                                ConfidenceScore = duplicateCheck.ExistingResult.ComparisonScore;
                                HasResult = true;
                                ExtractedMedications = new ObservableCollection<MedicationData>(result.Medications);
                                
                                // Update current prescription to reference existing result
                                prescription.DoctorName = result.DoctorName;
                                if (result.PrescriptionDate.HasValue)
                                    prescription.PrescriptionDate = result.PrescriptionDate.Value;
                                
                                await _prescriptionService.UpdatePrescriptionStatusAsync(
                                    prescription.Id,
                                    "Processed",
                                    "Using existing duplicate result",
                                    duplicateCheck.ExistingResult.ComparisonScore);
                                
                                ResultMessage = $"✅ Using existing prescription data!\n\n" +
                                              $"📊 Original processed: {duplicateCheck.ExistingResult.ProcessedAt:yyyy-MM-dd}\n" +
                                              $"💊 Medications: {result.Medications.Count}\n" +
                                              $"⚡ No AI processing needed - saved costs!";
                                
                                await page.DisplayAlertAsync(
                                    "Existing Data Loaded",
                                    $"Loaded {result.Medications.Count} medication(s) from previous processing.\n\n" +
                                    $"Review and tap 'Save Medications' to continue.",
                                    "OK"
                                );
                                
                                return; // Skip processing
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("ℹ️ User chose to process anyway (despite duplicate)");
                        }
                    }
                }
                else if (!duplicateCheck.IsDuplicate && duplicateCheck.SimilarityScore > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"ℹ️ Similar prescription found ({duplicateCheck.SimilarityScore:P0}), but below threshold");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("✅ No duplicate found - proceeding with fresh processing");
                }

                // Process with Multi-Agent Orchestrator (only if we have valid OCR text)
                var prescriptionFileName = Path.GetFileName(_imagePath ?? $"prescription_{prescription.Id}.jpg");
                var orchestratorResult = await _agentOrchestrator.ProcessPrescriptionAsync(
                    ocrText,
                    prescriptionFileName,
                    prescription.Id); // Pass prescription ID

                // Update UI with orchestrator results
                ProcessingAttempts = orchestratorResult.TotalAttempts;
                MatchScore = orchestratorResult.MatchScore;

                System.Diagnostics.Debug.WriteLine($"\n📊 Orchestrator Results:");
                System.Diagnostics.Debug.WriteLine($"   Success: {orchestratorResult.Success}");
                System.Diagnostics.Debug.WriteLine($"   Attempts: {orchestratorResult.TotalAttempts}");
                System.Diagnostics.Debug.WriteLine($"   Match Score: {orchestratorResult.MatchScore:P0}");
                System.Diagnostics.Debug.WriteLine($"   Processing Time: {orchestratorResult.ProcessingTime.TotalSeconds:F2}s");

                if (orchestratorResult.Success && orchestratorResult.ParseResult != null)
                {
                    var parseResult = orchestratorResult.ParseResult;
                    
                    // Convert PrescriptionParseResult to PrescriptionReadResult
                    var result = new PrescriptionReadResult
                    {
                        Success = parseResult.Success,
                        DoctorName = parseResult.Doctor?.Name,
                        PrescriptionDate = parseResult.PrescriptionDate,
                        Medications = parseResult.Medications,
                        ConfidenceScore = orchestratorResult.MatchScore
                    };
                    
                    Result = result;
                    ConfidenceScore = orchestratorResult.MatchScore;
                    HasResult = true;

                    System.Diagnostics.Debug.WriteLine($"✅ AI processing complete. Medications: {result.Medications?.Count ?? 0}");

                    if (result.Medications.Any())
                    {
                        ExtractedMedications = new ObservableCollection<MedicationData>(result.Medications);
                        
                        System.Diagnostics.Debug.WriteLine($"💊 Extracted {result.Medications.Count} medications:");
                        foreach (var med in result.Medications)
                        {
                            System.Diagnostics.Debug.WriteLine($"   • {med.Name} - {med.Dosage} {med.Unit} - {med.Frequency}");
                        }
                        
                        // Update prescription with doctor and date info
                        if (!string.IsNullOrEmpty(result.DoctorName))
                        {
                            prescription.DoctorName = result.DoctorName;
                            System.Diagnostics.Debug.WriteLine($"   Doctor: {result.DoctorName}");
                        }
                        if (result.PrescriptionDate.HasValue)
                        {
                            prescription.PrescriptionDate = result.PrescriptionDate.Value;
                            System.Diagnostics.Debug.WriteLine($"   Date: {result.PrescriptionDate.Value:yyyy-MM-dd}");
                        }

                        // Update prescription status
                        await _prescriptionService.UpdatePrescriptionStatusAsync(
                            prescription.Id,
                            "Processed",
                            null,
                            orchestratorResult.MatchScore);

                        System.Diagnostics.Debug.WriteLine($"✅ Prescription status updated to Processed");

                        // Build result message
                        var resultParts = new List<string>
                        {
                            $"✅ All medications validated successfully!",
                            $"",
                            $"📊 Quality Metrics:",
                            $"   • Match Score: {orchestratorResult.MatchScore:P0}",
                            $"   • AI Attempts: {orchestratorResult.TotalAttempts}",
                            $"   • Processing Time: {orchestratorResult.ProcessingTime.TotalSeconds:F1}s"
                        };

                        if (!string.IsNullOrWhiteSpace(orchestratorResult.WarningMessage))
                        {
                            resultParts.Add($"\n⚠️ Note: {orchestratorResult.WarningMessage}");
                        }

                        ResultMessage = string.Join("\n", resultParts);

                        var currentPage = GetCurrentPage();
                        if (currentPage != null)
                        {
                            await currentPage.DisplayAlertAsync(
                                "Success",
                                $"Found {result.Medications.Count} medication(s).\n\n" +
                                $"Match Quality: {orchestratorResult.MatchScore:P0}\n" +
                                $"AI Attempts: {orchestratorResult.TotalAttempts}\n\n" +
                                $"{(!string.IsNullOrWhiteSpace(orchestratorResult.WarningMessage) ? orchestratorResult.WarningMessage + "\n\n" : "")}" +
                                $"Review and tap 'Save Medications' to continue.",
                                "OK"
                            );
                        }
                    }
                    else
                    {
                        ResultMessage = "No medications found in the prescription.";
                        await _prescriptionService.UpdatePrescriptionStatusAsync(
                            prescription.Id, "Processed", "No medications found");
                    }
                }
                else
                {
                    // AI processing failed
                    ResultMessage = orchestratorResult.ErrorMessage ?? "Failed to extract medications. Please try again with a clearer image.";
                    
                    await _prescriptionService.UpdatePrescriptionStatusAsync(
                        prescription.Id,
                        "Failed",
                        orchestratorResult.ErrorMessage);
                    
                    System.Diagnostics.Debug.WriteLine($"❌ AI processing failed: {orchestratorResult.ErrorMessage}");
                    
                    var currentPage = GetCurrentPage();
                    if (currentPage != null)
                    {
                        await currentPage.DisplayAlertAsync(
                            "Processing Failed",
                            GetUserFriendlyErrorMessage(orchestratorResult.ErrorMessage),
                            "OK");
                    }
                }
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("User not logged in"))
            {
                System.Diagnostics.Debug.WriteLine("❌ User not logged in exception caught!");
                throw; // Re-throw to be caught by ExecuteAsync
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Network error: {ex.Message}");
                
                var page = GetCurrentPage();
                if (page != null)
                {
                    await page.DisplayAlertAsync(
                        "Network Error",
                        "Could not connect to the AI service. Please check your internet connection and try again.",
                        "OK"
                    );
                }
                
                ResultMessage = "❌ Network error. Please check your internet connection.";
                HasResult = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   Type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"   Stack trace: {ex.StackTrace}");
                
                ResultMessage = $"❌ Error: {ex.Message}";
                HasResult = true;
            }
        });

        IsProcessing = false;
    }

    
    private string GetUserFriendlyErrorMessage(string? errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage))
        {
            return "No medications could be extracted from the image.\n\n" +
                   "Tips:\n" +
                   "• Ensure the prescription is clear and well-lit\n" +
                   "• Make sure medication names are visible\n" +
                   "• Try taking a new photo\n" +
                   "• Check your internet connection";
        }

        if (errorMessage.Contains("Connection failure") || errorMessage.Contains("No such host"))
        {
            return "Cannot connect to the AI service.\n\n" +
                   "Possible causes:\n" +
                   "• No internet connection\n" +
                   "• Firewall blocking the connection\n" +
                   "• Service temporarily unavailable\n\n" +
                   "Please check your internet connection and try again.";
        }

        if (errorMessage.Contains("401") || errorMessage.Contains("Unauthorized"))
        {
            return "API authentication failed.\n\n" +
                   "This is a configuration issue. Please contact support.";
        }

        if (errorMessage.Contains("429") || errorMessage.Contains("rate limit"))
        {
            return "Too many requests.\n\n" +
                   "Please wait a moment and try again.";
        }

        if (errorMessage.Contains("timeout") || errorMessage.Contains("timed out"))
        {
            return "The request took too long.\n\n" +
                   "Tips:\n" +
                   "• Try with a smaller image\n" +
                   "• Check your internet speed\n" +
                   "• Try again in a moment";
        }

        if (errorMessage.Contains("API"))
        {
            return $"API Error: {errorMessage}\n\n" +
                   "Please check your internet connection and try again.";
        }

        // Generic error
        return $"Processing failed: {errorMessage}\n\n" +
               "Tips:\n" +
               "• Ensure good image quality\n" +
               "• Check internet connection\n" +
               "• Try again";
    }
}
