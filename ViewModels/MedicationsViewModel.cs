using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedRemind.Models;
using MedRemind.Services;
using System.Collections.ObjectModel;

namespace MedRemind.ViewModels;

public partial class MedicationsViewModel : ObservableObject
{
    private readonly DatabaseService _databaseService;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private ObservableCollection<Medication> medications = new();

    [ObservableProperty]
    private bool isRefreshing;

    public MedicationsViewModel(DatabaseService databaseService, INotificationService notificationService)
    {
        _databaseService = databaseService;
        _notificationService = notificationService;
    }

    [RelayCommand]
    private async Task LoadMedicationsAsync()
    {
        IsRefreshing = true;
        try
        {
            var meds = await _databaseService.GetMedicationsAsync();
            Medications.Clear();
            foreach (var med in meds)
            {
                Medications.Add(med);
            }
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task AddMedicationAsync()
    {
        await Shell.Current.GoToAsync("addmedication");
    }

    [RelayCommand]
    private async Task ViewMedicationAsync(Medication medication)
    {
        var navigationParameter = new Dictionary<string, object>
        {
            { "Medication", medication }
        };
        await Shell.Current.GoToAsync("medicationdetail", navigationParameter);
    }

    [RelayCommand]
    private async Task DeleteMedicationAsync(Medication medication)
    {
        bool answer = await Shell.Current.DisplayAlertAsync(
            "Delete Medication",
            $"Are you sure you want to delete {medication.Name}?",
            "Yes", "No");

        if (answer)
        {
            await _databaseService.DeleteMedicationAsync(medication);
            Medications.Remove(medication);
        }
    }
}
