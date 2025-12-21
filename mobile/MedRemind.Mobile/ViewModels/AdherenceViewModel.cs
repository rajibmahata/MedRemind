using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedRemind.Core.DTOs;
using MedRemind.Services.Medications;

namespace MedRemind.Mobile.ViewModels;

public partial class AdherenceViewModel : BaseViewModel
{
    private readonly AdherenceService _adherenceService;

    [ObservableProperty]
    private int _currentStreak;

    [ObservableProperty]
    private int _longestStreak;

    [ObservableProperty]
    private double _adherencePercentage;

    [ObservableProperty]
    private int _totalDoses;

    [ObservableProperty]
    private int _takenDoses;

    [ObservableProperty]
    private int _missedDoses;

    [ObservableProperty]
    private ObservableCollection<DailyAdherence> _weeklyData = new();

    [ObservableProperty]
    private ObservableCollection<DailyAdherence> _monthlyData = new();

    [ObservableProperty]
    private string _selectedPeriod = "Week";

    public AdherenceViewModel(AdherenceService adherenceService)
    {
        _adherenceService = adherenceService;
        Title = "Adherence";
    }

    [RelayCommand]
    private async Task LoadWeeklyAsync()
    {
        await ExecuteAsync(async () =>
        {
            int userId = 1; // TODO: Get from secure storage
            var data = await _adherenceService.GetWeeklyAdherenceAsync(userId);
            
            AdherencePercentage = data.OverallPercentage;
            CurrentStreak = data.LongestStreak;
            LongestStreak = data.LongestStreak;
            TotalDoses = data.TotalDoses;
            TakenDoses = data.TakenDoses;
            MissedDoses = data.MissedDoses;
            WeeklyData = new ObservableCollection<DailyAdherence>(data.DailyData);
            
            SelectedPeriod = "Week";
        });
    }

    [RelayCommand]
    private async Task LoadMonthlyAsync()
    {
        await ExecuteAsync(async () =>
        {
            int userId = 1; // TODO: Get from secure storage
            var data = await _adherenceService.GetMonthlyAdherenceAsync(userId);
            
            AdherencePercentage = data.OverallPercentage;
            CurrentStreak = data.LongestStreak;
            LongestStreak = data.LongestStreak;
            TotalDoses = data.TotalDoses;
            TakenDoses = data.TakenDoses;
            MissedDoses = data.MissedDoses;
            MonthlyData = new ObservableCollection<DailyAdherence>(data.DailyData);
            
            SelectedPeriod = "Month";
        });
    }

    [RelayCommand]
    private async Task ChangePeriodAsync(string period)
    {
        if (period == "Week")
        {
            await LoadWeeklyAsync();
        }
        else if (period == "Month")
        {
            await LoadMonthlyAsync();
        }
    }

    public void OnAppearing()
    {
        LoadWeeklyCommand.Execute(null);
    }
}
