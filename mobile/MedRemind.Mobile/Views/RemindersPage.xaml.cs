using MedRemind.Mobile.ViewModels;

namespace MedRemind.Mobile.Views;

public partial class RemindersPage : ContentPage
{
    private readonly RemindersViewModel _viewModel;

    public RemindersPage(RemindersViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.OnAppearing();
    }
}
