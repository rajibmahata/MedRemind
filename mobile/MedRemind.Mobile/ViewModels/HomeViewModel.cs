using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedRemind.Core.Models;
using MedRemind.Services.Medications;

namespace MedRemind.Mobile.ViewModels;

public partial class HomeViewModel : BaseViewModel
{
    private readonly MedicationService _medicationService;
    private readonly AdherenceService _adherenceService;

    [ObservableProperty]
    private ObservableCollection<Medication> _todaysMedications = new();

    [ObservableProperty]
    private int _currentStreak;

    [ObservableProperty]
    private double _adherencePercentage;

    [ObservableProperty]
    private int _todaysTotalDoses;

    [ObservableProperty]
    private int _todaysTakenDoses;

    [ObservableProperty]
    private string _greetingMessage = string.Empty;

    public HomeViewModel(
        MedicationService medicationService,
        AdherenceService adherenceService)
    {
        _medicationService = medicationService;
        _adherenceService = adherenceService;
        Title = "Home";
        
        UpdateGreeting();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Get user ID from secure storage (simplified for now)
            int userId = 1;

            // Load today's medications
            var medications = await _medicationService.GetActiveMedicationsAsync(userId);
            TodaysMedications = new ObservableCollection<Medication>(medications);

            // Load adherence data
            var adherenceData = await _adherenceService.GetWeeklyAdherenceAsync(userId);
            CurrentStreak = adherenceData.LongestStreak;
            AdherencePercentage = adherenceData.OverallPercentage;

            // Calculate today's progress
            var today = DateTime.UtcNow.Date;
            var todayData = adherenceData.DailyData.FirstOrDefault(d => d.Date.Date == today);
            if (todayData != null)
            {
                TodaysTotalDoses = todayData.TotalDoses;
                TodaysTakenDoses = todayData.TakenDoses;
            }
        });
    }

    [RelayCommand]
    private async Task NavigateToUploadAsync()
    {
        await Shell.Current.GoToAsync("///PrescriptionUploadPage");
    }

    [RelayCommand]
    private async Task NavigateToMedicationsAsync()
    {
        await Shell.Current.GoToAsync("///MedicationsPage");
    }

    [RelayCommand]
    private async Task TakeDoseAsync(Medication medication)
    {
        if (medication == null) return;

        await ExecuteAsync(async () =>
        {
            await _medicationService.LogDoseAsync(
                medication.Id,
                DateTime.UtcNow,
                "Taken");

            await LoadDataAsync();

            await Application.Current!.MainPage!.DisplayAlert(
                "Success",
                "Dose logged successfully!",
                "OK");
        });
    }

    private void UpdateGreeting()
    {
        var hour = DateTime.Now.Hour;
        GreetingMessage = hour switch
        {
            < 12 => "Good Morning",
            < 17 => "Good Afternoon",
            _ => "Good Evening"
        };
    }

    public void OnAppearing()
    {
        LoadDataCommand.Execute(null);
    }
}
