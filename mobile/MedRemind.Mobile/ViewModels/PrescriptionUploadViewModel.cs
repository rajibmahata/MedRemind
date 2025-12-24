using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedRemind.Core.DTOs;
using MedRemind.Core.Interfaces;
using MedRemind.Core.Models;
using MedRemind.Services.Medications;
using MedRemind.Services.Prescriptions;

namespace MedRemind.Mobile.ViewModels;

public partial class PrescriptionUploadViewModel : BaseViewModel
{
    private readonly IPrescriptionReaderService _prescriptionReader;
    private readonly IValidationAgentService _validationAgent;
    private readonly PrescriptionService _prescriptionService;
    private readonly MedicationService _medicationService;
    private readonly IReminderSchedulingService _reminderScheduling;

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

    private string? _imageBase64;
    private string? _imagePath;

    public PrescriptionUploadViewModel(
        IPrescriptionReaderService prescriptionReader,
        IValidationAgentService validationAgent,
        PrescriptionService prescriptionService,
        MedicationService medicationService,
        IReminderSchedulingService reminderScheduling)
    {
        _prescriptionReader = prescriptionReader;
        _validationAgent = validationAgent;
        _prescriptionService = prescriptionService;
        _medicationService = medicationService;
        _reminderScheduling = reminderScheduling;
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

            // Convert to base64 for AI processing
            var bytes = await File.ReadAllBytesAsync(localFilePath);
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
                System.Diagnostics.Debug.WriteLine("?? ProcessPrescription: Starting...");
                
                // Check SecureStorage state
                var userId_check = await SecureStorage.GetAsync("user_id");
                var sessionToken_check = await SecureStorage.GetAsync("session_token");
                System.Diagnostics.Debug.WriteLine($"   SecureStorage check - user_id: {userId_check ?? "NULL"}, session: {(string.IsNullOrEmpty(sessionToken_check) ? "NULL" : "EXISTS")}");

                // Get user ID - user is already authenticated by AppShell
                var userId = await GetCurrentUserIdAsync();
                System.Diagnostics.Debug.WriteLine($"?? Processing prescription for user ID: {userId}");

                // Save prescription to database
                var prescription = new Prescription
                {
                    UserId = userId,
                    ImagePath = _imagePath ?? "base64_image",
                    PrescriptionDate = DateTime.UtcNow,
                    Status = "Processing",
                    CreatedAt = DateTime.UtcNow
                };

                System.Diagnostics.Debug.WriteLine($"?? Saving prescription for user {userId}");
                prescription = await _prescriptionService.AddPrescriptionAsync(prescription);
                System.Diagnostics.Debug.WriteLine($"? Prescription saved with ID: {prescription.Id}");

                // Process with AI
                System.Diagnostics.Debug.WriteLine($"?? Starting AI processing...");
                var result = await _prescriptionReader.ReadPrescriptionFromBase64Async(_imageBase64);
                Result = result;
                ConfidenceScore = result.ConfidenceScore;
                HasResult = true;

                System.Diagnostics.Debug.WriteLine($"? AI processing complete. Success: {result.Success}, Medications: {result.Medications?.Count ?? 0}");

                if (result.Success && result.Medications.Any())
                {
                    ExtractedMedications = new ObservableCollection<MedicationData>(result.Medications);
                    
                    // Update prescription with doctor and date info
                    if (!string.IsNullOrEmpty(result.DoctorName))
                    {
                        prescription.DoctorName = result.DoctorName;
                    }
                    if (result.PrescriptionDate.HasValue)
                    {
                        prescription.PrescriptionDate = result.PrescriptionDate.Value;
                    }

                    // Update prescription status
                    await _prescriptionService.UpdatePrescriptionStatusAsync(
                        prescription.Id,
                        "Processed",
                        null,
                        result.ConfidenceScore);

                    System.Diagnostics.Debug.WriteLine($"? Prescription status updated to Processed");

                    // Check for warnings
                    if (result.Warnings != null && result.Warnings.Any())
                    {
                        var warningMessages = result.Warnings
                            .Select(w => $"• {w.MedicationName}: {w.Message}")
                            .ToList();
                        ResultMessage = $"?? Warnings:\n{string.Join("\n", warningMessages)}";
                    }
                    else
                    {
                        ResultMessage = "? All medications validated successfully!";
                    }

                    var currentPage = GetCurrentPage();
                    if (currentPage != null)
                    {
                        await currentPage.DisplayAlertAsync(
                            "Success",
                            $"Found {result.Medications.Count} medication(s).\nReview and tap 'Save Medications' to continue.",
                            "OK"
                        );
                    }
                }
                else
                {
                    ResultMessage = result.ErrorMessage ?? "Failed to extract medications. Please try again with a clearer image.";
                    await _prescriptionService.UpdatePrescriptionStatusAsync(
                        prescription.Id,
                        "Failed",
                        result.ErrorMessage);
                    
                    System.Diagnostics.Debug.WriteLine($"? AI processing failed: {result.ErrorMessage}");
                }
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("User not logged in"))
            {
                System.Diagnostics.Debug.WriteLine("? User not logged in exception caught!");
                System.Diagnostics.Debug.WriteLine($"   Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   Stack: {ex.StackTrace}");
                
                // Check SecureStorage state again
                var userId_final = await SecureStorage.GetAsync("user_id");
                var sessionToken_final = await SecureStorage.GetAsync("session_token");
                System.Diagnostics.Debug.WriteLine($"   Final check - user_id: {userId_final ?? "NULL"}, session: {(string.IsNullOrEmpty(sessionToken_final) ? "NULL" : "EXISTS")}");
                
                throw; // Re-throw to be caught by ExecuteAsync
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Exception in ProcessPrescriptionAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   Type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"   Stack trace: {ex.StackTrace}");
                ResultMessage = $"? Error: {ex.Message}";
                HasResult = true;
            }
        });

        IsProcessing = false;
    }

    [RelayCommand]
    private async Task SaveMedicationsAsync()
    {
        if (!ExtractedMedications.Any())
        {
            ShowError("No medications to save");
            return;
        }

        await ExecuteAsync(async () =>
        {
            // Get user ID - user is already authenticated by AppShell
            var userId = await GetCurrentUserIdAsync();
            System.Diagnostics.Debug.WriteLine($"?? Saving medications for user ID: {userId}");

            int savedCount = 0;

            foreach (var medData in ExtractedMedications)
            {
                try
                {
                    // Validate medication data
                    if (string.IsNullOrWhiteSpace(medData.Name))
                    {
                        System.Diagnostics.Debug.WriteLine($"?? Skipping medication with empty name");
                        continue;
                    }

                    System.Diagnostics.Debug.WriteLine($"?? Saving medication: {medData.Name}");

                    // Create medication
                    var savedMed = await _medicationService.CreateMedicationAsync(userId, medData);
                    System.Diagnostics.Debug.WriteLine($"? Medication saved with ID: {savedMed.Id}");

                    // Create reminders based on frequency
                    await _medicationService.CreateRemindersAsync(savedMed.Id, medData.FrequencyCount);
                    System.Diagnostics.Debug.WriteLine($"? Created {medData.FrequencyCount} reminder(s) for {medData.Name}");

                    savedCount++;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"? Error saving medication {medData.Name}: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }

            var savePage = GetCurrentPage();
            if (savePage != null)
            {
                if (savedCount > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"? Successfully saved {savedCount} medication(s)");
                    
                    await savePage.DisplayAlertAsync(
                        "Success",
                        $"Saved {savedCount} medication(s) with reminders!",
                        "OK");

                    // Clear and navigate to medications
                    ClearData();
                    await Shell.Current.GoToAsync("///MedicationsPage");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"? Failed to save any medications");
                    
                    await savePage.DisplayAlertAsync(
                        "Error",
                        "Failed to save medications. Please verify the extracted data and try again.",
                        "OK");
                }
            }
        });
    }

    [RelayCommand]
    private async Task EditMedicationAsync(MedicationData medication)
    {
        if (medication == null) return;

        var page = GetCurrentPage();
        if (page == null) return;

        // Edit medication name
        var name = await page.DisplayPromptAsync(
            "Edit Medication",
            $"Medication name:",
            initialValue: medication.Name);

        if (!string.IsNullOrWhiteSpace(name))
        {
            medication.Name = name;
        }

        // Edit dosage
        var dosage = await page.DisplayPromptAsync(
            "Edit Dosage",
            $"Dosage for {medication.Name}:",
            initialValue: medication.Dosage);

        if (!string.IsNullOrWhiteSpace(dosage))
        {
            medication.Dosage = dosage;
        }

        // Edit frequency
        var frequency = await page.DisplayPromptAsync(
            "Edit Frequency",
            $"Frequency (e.g., 'Twice daily'):",
            initialValue: medication.Frequency);

        if (!string.IsNullOrWhiteSpace(frequency))
        {
            medication.Frequency = frequency;
        }

        // Edit duration
        var durationStr = await page.DisplayPromptAsync(
            "Edit Duration",
            $"Duration in days:",
            initialValue: medication.DurationDays.ToString(),
            keyboard: Keyboard.Numeric);

        if (!string.IsNullOrWhiteSpace(durationStr) && int.TryParse(durationStr, out var duration))
        {
            medication.DurationDays = duration;
        }

        // Refresh the collection view
        var index = ExtractedMedications.IndexOf(medication);
        if (index >= 0)
        {
            ExtractedMedications[index] = medication;
        }
    }

    [RelayCommand]
    private void ClearData()
    {
        SelectedImage = null;
        HasResult = false;
        Result = null;
        ExtractedMedications.Clear();
        ResultMessage = string.Empty;
        ConfidenceScore = 0;
        _imageBase64 = null;
        _imagePath = null;

        // Clean up cached image
        if (!string.IsNullOrEmpty(_imagePath) && File.Exists(_imagePath))
        {
            try
            {
                File.Delete(_imagePath);
            }
            catch
            {
                // Ignore file deletion errors
            }
        }
    }
}
