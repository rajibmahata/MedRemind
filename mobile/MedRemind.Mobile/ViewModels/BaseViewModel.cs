using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MedRemind.Mobile.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    protected void ShowError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }

    protected void ClearError()
    {
        ErrorMessage = string.Empty;
        HasError = false;
    }

    protected async Task ExecuteAsync(Func<Task> operation, string? loadingMessage = null)
    {
        IsBusy = true;
        ClearError();

        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected async Task<T?> ExecuteAsync<T>(Func<Task<T>> operation, string? loadingMessage = null)
    {
        IsBusy = true;
        ClearError();

        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
            return default;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
