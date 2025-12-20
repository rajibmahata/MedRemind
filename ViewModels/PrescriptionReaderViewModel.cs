using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedRemind.Services;
using System.Collections.ObjectModel;
using MedRemind.Models;

namespace MedRemind.ViewModels;

public partial class PrescriptionReaderViewModel : ObservableObject
{
    private readonly IOcrService _ocrService;
    private readonly DatabaseService _databaseService;

    [ObservableProperty]
    private string? imagePath;

    [ObservableProperty]
    private string extractedText = string.Empty;

    [ObservableProperty]
    private bool isProcessing;

    [ObservableProperty]
    private bool hasImage;

    public PrescriptionReaderViewModel(IOcrService ocrService, DatabaseService databaseService)
    {
        _ocrService = ocrService;
        _databaseService = databaseService;
    }

    [RelayCommand]
    private async Task TakePhotoAsync()
    {
        try
        {
            if (MediaPicker.Default.IsCaptureSupported)
            {
                var photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo != null)
                {
                    var localFilePath = Path.Combine(FileSystem.CacheDirectory, photo.FileName);
                    using var stream = await photo.OpenReadAsync();
                    using var newStream = File.OpenWrite(localFilePath);
                    await stream.CopyToAsync(newStream);

                    ImagePath = localFilePath;
                    HasImage = true;
                    await ProcessImageAsync();
                }
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", $"Unable to take photo: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task PickPhotoAsync()
    {
        try
        {
#pragma warning disable CS0618 // Type or member is obsolete - using single photo selection for simplicity
            var photo = await MediaPicker.Default.PickPhotoAsync();
#pragma warning restore CS0618
            if (photo != null)
            {
                var localFilePath = Path.Combine(FileSystem.CacheDirectory, photo.FileName);
                using var stream = await photo.OpenReadAsync();
                using var newStream = File.OpenWrite(localFilePath);
                await stream.CopyToAsync(newStream);

                ImagePath = localFilePath;
                HasImage = true;
                await ProcessImageAsync();
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", $"Unable to pick photo: {ex.Message}", "OK");
        }
    }

    private async Task ProcessImageAsync()
    {
        if (string.IsNullOrEmpty(ImagePath))
            return;

        IsProcessing = true;
        try
        {
            ExtractedText = await _ocrService.ExtractTextFromImageAsync(ImagePath);

            // Save prescription to database
            var prescription = new Prescription
            {
                ImagePath = ImagePath,
                ExtractedText = ExtractedText,
                PrescriptionDate = DateTime.Now,
                IsProcessed = true,
                CreatedAt = DateTime.Now
            };

            await _databaseService.SavePrescriptionAsync(prescription);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", $"Failed to process image: {ex.Message}", "OK");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private void ClearImage()
    {
        ImagePath = null;
        ExtractedText = string.Empty;
        HasImage = false;
    }
}
