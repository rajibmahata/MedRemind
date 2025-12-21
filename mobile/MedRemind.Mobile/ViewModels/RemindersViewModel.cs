using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedRemind.Core.Models;
using MedRemind.Core.Interfaces;

namespace MedRemind.Mobile.ViewModels;

public partial class RemindersViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAudioService _audioService;

    [ObservableProperty]
    private ObservableCollection<Reminder> _reminders = new();

    [ObservableProperty]
    private bool _groupByMedication = true;

    [ObservableProperty]
    private bool _isRecording;

    public RemindersViewModel(IUnitOfWork unitOfWork, IAudioService audioService)
    {
        _unitOfWork = unitOfWork;
        _audioService = audioService;
        Title = "Reminders";
    }

    [RelayCommand]
    private async Task LoadRemindersAsync()
    {
        await ExecuteAsync(async () =>
        {
            var reminderRepo = _unitOfWork.Repository<Reminder>();
            var allReminders = await reminderRepo.GetAllAsync();
            Reminders = new ObservableCollection<Reminder>(allReminders);
        });
    }

    [RelayCommand]
    private async Task ToggleReminderAsync(Reminder reminder)
    {
        if (reminder == null) return;

        await ExecuteAsync(async () =>
        {
            reminder.IsEnabled = !reminder.IsEnabled;
            var reminderRepo = _unitOfWork.Repository<Reminder>();
            await reminderRepo.UpdateAsync(reminder);
            await _unitOfWork.SaveChangesAsync();
        });
    }

    [RelayCommand]
    private async Task EditReminderTimeAsync(Reminder reminder)
    {
        if (reminder == null) return;

        await Application.Current!.MainPage!.DisplayAlert(
            "Edit Reminder",
            $"Reminder time: {reminder.ReminderTime}",
            "OK");
    }

    [RelayCommand]
    private async Task RecordVoiceAsync(Reminder reminder)
    {
        if (reminder == null) return;

        var available = await _audioService.IsRecordingAvailableAsync();
        if (!available)
        {
            ShowError("Audio recording not available on this device");
            return;
        }

        IsRecording = true;
        var result = await _audioService.StartRecordingAsync($"reminder_{reminder.Id}");
        
        await Task.Delay(5000); // Record for 5 seconds
        
        await _audioService.StopRecordingAsync();
        IsRecording = false;

        if (result.Success)
        {
            await Application.Current!.MainPage!.DisplayAlert(
                "Success",
                "Voice reminder recorded!",
                "OK");
        }
    }

    [RelayCommand]
    private async Task PlayVoiceAsync(VoiceRecording voice)
    {
        if (voice == null || string.IsNullOrEmpty(voice.FilePath)) return;

        var success = await _audioService.PlayAudioAsync(voice.FilePath);
        if (!success)
        {
            ShowError("Failed to play voice recording");
        }
    }

    public void OnAppearing()
    {
        LoadRemindersCommand.Execute(null);
    }
}
