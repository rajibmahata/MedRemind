using MedRemind.Mobile.ViewModels;

namespace MedRemind.Mobile.Views;

public partial class AdherencePage : ContentPage
{
    private readonly AdherenceViewModel _viewModel;

    public AdherencePage(AdherenceViewModel viewModel)
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
