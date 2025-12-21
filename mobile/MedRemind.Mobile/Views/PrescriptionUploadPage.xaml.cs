using MedRemind.Mobile.ViewModels;

namespace MedRemind.Mobile.Views;

public partial class PrescriptionUploadPage : ContentPage
{
    public PrescriptionUploadPage(PrescriptionUploadViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
