using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedRemind.Models;
using MedRemind.Services;

namespace MedRemind.ViewModels;

public partial class AddMedicationViewModel : ObservableObject
{
    private readonly DatabaseService _databaseService;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string dosage = string.Empty;

    [ObservableProperty]
    private string frequency = string.Empty;

    [ObservableProperty]
    private string instructions = string.Empty;

    [ObservableProperty]
    private DateTime startDate = DateTime.Now;

    [ObservableProperty]
    private string notes = string.Empty;

    public AddMedicationViewModel(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    [RelayCommand]
    private async Task SaveMedicationAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            await Shell.Current.DisplayAlertAsync("Error", "Please enter medication name", "OK");
            return;
        }

        var medication = new Medication
        {
            Name = Name,
            Dosage = Dosage,
            Frequency = Frequency,
            Instructions = Instructions,
            StartDate = StartDate,
            Notes = Notes,
            IsActive = true
        };

        await _databaseService.SaveMedicationAsync(medication);
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
