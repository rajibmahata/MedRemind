using MedRemind.ViewModels;

namespace MedRemind.Pages;

public partial class PrescriptionReaderPage : ContentPage
{
    public PrescriptionReaderPage(PrescriptionReaderViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
