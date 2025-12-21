using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedRemind.Core.Models;
using MedRemind.Services.Medications;

namespace MedRemind.Mobile.ViewModels;

public partial class MedicationsViewModel : BaseViewModel
{
    private readonly MedicationService _medicationService;

    [ObservableProperty]
    private ObservableCollection<Medication> _medications = new();

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private Medication? _selectedMedication;

    public MedicationsViewModel(MedicationService medicationService)
    {
        _medicationService = medicationService;
        Title = "Medications";
    }

    [RelayCommand]
    private async Task LoadMedicationsAsync()
    {
        await ExecuteAsync(async () =>
        {
            int userId = 1; // TODO: Get from secure storage
            var meds = await _medicationService.GetActiveMedicationsAsync(userId);
            Medications = new ObservableCollection<Medication>(meds);
        });
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        await LoadMedicationsAsync();
        IsRefreshing = false;
    }

    [RelayCommand]
    private async Task AddMedicationAsync()
    {
        // Navigate to add medication page
        await Shell.Current.DisplayAlertAsync(
            "Add Medication",
            "This will open a form to add a new medication manually",
            "OK");
    }

    [RelayCommand]
    private async Task EditMedicationAsync(Medication medication)
    {
        if (medication == null) return;

        var page = GetCurrentPage();
        if (page == null) return;

        var result = await page.DisplayPromptAsync(
            "Edit Medication",
            "Enter new dosage:",
            initialValue: medication.Dosage,
            keyboard: Keyboard.Numeric);

        if (!string.IsNullOrWhiteSpace(result))
        {
            await ExecuteAsync(async () =>
            {
                // Create MedicationData from the medication
                var medicationData = new Core.DTOs.MedicationData
                {
                    Name = medication.Name,
                    Dosage = result,
                    Unit = medication.Unit,
                    Frequency = medication.Frequency,
                    FrequencyCount = medication.FrequencyCount,
                    DurationDays = medication.DurationDays,
                    Instructions = medication.Instructions
                };
                
                await _medicationService.UpdateMedicationAsync(medication.Id, medicationData, false);
                await LoadMedicationsAsync();
                
                await page.DisplayAlertAsync(
                    "Success",
                    "Medication updated successfully",
                    "OK");
            });
        }
    }

    [RelayCommand]
    private async Task DeleteMedicationAsync(Medication medication)
    {
        if (medication == null) return;

        var page = GetCurrentPage();
        if (page == null) return;

        var confirm = await page.DisplayAlertAsync(
            "Delete Medication",
            $"Are you sure you want to delete {medication.Name}?",
            "Yes",
            "No");

        if (confirm)
        {
            await ExecuteAsync(async () =>
            {
                await _medicationService.DeleteMedicationAsync(medication.Id);
                await LoadMedicationsAsync();
                
                await page.DisplayAlertAsync(
                    "Success",
                    "Medication deleted successfully",
                    "OK");
            });
        }
    }

    [RelayCommand]
    private async Task PauseMedicationAsync(Medication medication)
    {
        if (medication == null) return;

        await ExecuteAsync(async () =>
        {
            await _medicationService.PauseMedicationAsync(medication.Id);
            await LoadMedicationsAsync();
            
            var page = GetCurrentPage();
            if (page != null)
            {
                await page.DisplayAlertAsync(
                    "Success",
                    $"{medication.Name} has been paused",
                    "OK");
            }
        });
    }

    [RelayCommand]
    private async Task ResumeMedicationAsync(Medication medication)
    {
        if (medication == null) return;

        await ExecuteAsync(async () =>
        {
            await _medicationService.ResumeMedicationAsync(medication.Id);
            await LoadMedicationsAsync();
            
            var page = GetCurrentPage();
            if (page != null)
            {
                await page.DisplayAlertAsync(
                    "Success",
                    $"{medication.Name} has been resumed",
                    "OK");
            }
        });
    }

    [RelayCommand]
    private async Task ViewHistoryAsync(Medication medication)
    {
        if (medication == null) return;

        await ExecuteAsync(async () =>
        {
            var doseLogs = await _medicationService.GetDoseLogsAsync(medication.Id);
            var count = doseLogs.Count();
            
            var page = GetCurrentPage();
            if (page != null)
            {
                await page.DisplayAlertAsync(
                    "Dose History",
                    $"{medication.Name} has {count} dose log entries",
                    "OK");
            }
        });
    }

    partial void OnSearchTextChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            _ = LoadMedicationsAsync();
        }
        else
        {
            var filtered = Medications
                .Where(m => m.Name.Contains(value, StringComparison.OrdinalIgnoreCase))
                .ToList();
            Medications = new ObservableCollection<Medication>(filtered);
        }
    }

    public void OnAppearing()
    {
        LoadMedicationsCommand.Execute(null);
    }
}
