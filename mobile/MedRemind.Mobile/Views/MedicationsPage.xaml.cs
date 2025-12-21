using MedRemind.Mobile.ViewModels;

namespace MedRemind.Mobile.Views;

public partial class MedicationsPage : ContentPage
{
    private readonly MedicationsViewModel _viewModel;

    public MedicationsPage(MedicationsViewModel viewModel)
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
