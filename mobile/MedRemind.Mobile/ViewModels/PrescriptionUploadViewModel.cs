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
            var results = await MediaPicker.Default.PickPhotosAsync();
            var photo = results?.FirstOrDefault();
            await ProcessPhotoAsync(photo);
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
            using var stream = await photo.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();
            _imageBase64 = Convert.ToBase64String(bytes);

            SelectedImage = ImageSource.FromStream(() => new MemoryStream(bytes));
            
            var page = GetCurrentPage();
            if (page != null)
            {
                await page.DisplayAlertAsync(
                    "Photo Selected",
                    "Ready to process. Tap 'Process Prescription' to extract medications.",
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

        await ExecuteAsync(async () =>
        {
            // Save prescription to database
            var prescription = new Prescription
            {
                UserId = 1, // TODO: Get from secure storage
                ImagePath = "base64_image", // In real app, save to file
                PrescriptionDate = DateTime.UtcNow,
                Status = "Processing",
                CreatedAt = DateTime.UtcNow
            };

            prescription = await _prescriptionService.AddPrescriptionAsync(prescription);

            // Process with AI
            var result = await _prescriptionReader.ReadPrescriptionAsync(_imageBase64);
            Result = result;
            ConfidenceScore = result.ConfidenceScore;
            HasResult = true;

            if (result.Success && result.Medications.Any())
            {
                ExtractedMedications = new ObservableCollection<MedicationData>(result.Medications);
                
                // Validate medications
                var warnings = await _validationAgent.ValidateMedicationsAsync(result.Medications);
                
                // Update prescription status
                await _prescriptionService.UpdatePrescriptionStatusAsync(
                    prescription.Id,
                    "Processed",
                    null, // No RawResponse property in DTO
                    result.ConfidenceScore);

                if (warnings.Any())
                {
                    var warningMessages = warnings.Select(w => $"{w.MedicationName}: {w.Message}").ToList();
                    ResultMessage = $"? Warnings:\n{string.Join("\n", warningMessages)}";
                }
                else
                {
                    ResultMessage = "? All medications validated successfully!";
                }
            }
            else
            {
                ResultMessage = result.ErrorMessage ?? "Failed to extract medications";
                await _prescriptionService.UpdatePrescriptionStatusAsync(
                    prescription.Id,
                    "Failed",
                    null); // No RawResponse property
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
            int userId = 1; // TODO: Get from secure storage
            int savedCount = 0;

            foreach (var medData in ExtractedMedications)
            {
                // Create medication using MedicationService.CreateMedicationAsync
                var savedMed = await _medicationService.CreateMedicationAsync(
                    userId,
                    medData);

                // Create reminders
                await _medicationService.CreateRemindersAsync(savedMed.Id, medData.FrequencyCount);

                savedCount++;
            }

            var page = GetCurrentPage();
            if (page != null)
            {
                await page.DisplayAlertAsync(
                    "Success",
                    $"Saved {savedCount} medication(s) with reminders!",
                    "OK");
            }

            // Clear and navigate to medications
            ClearData();
            await Shell.Current.GoToAsync("///MedicationsPage");
        });
    }

    [RelayCommand]
    private async Task EditMedicationAsync(MedicationData medication)
    {
        if (medication == null) return;

        var page = GetCurrentPage();
        if (page == null) return;

        var result = await page.DisplayPromptAsync(
            "Edit Dosage",
            $"Edit dosage for {medication.Name}:",
            initialValue: medication.Dosage);

        if (!string.IsNullOrWhiteSpace(result))
        {
            medication.Dosage = result;
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
    }
}
