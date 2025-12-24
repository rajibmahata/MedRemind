using MedRemind.Mobile.ViewModels;

namespace MedRemind.Mobile.Views;

public partial class PrescriptionUploadPage : ContentPage
{
    private readonly PrescriptionUploadViewModel _viewModel;

    public PrescriptionUploadPage(PrescriptionUploadViewModel viewModel)
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
