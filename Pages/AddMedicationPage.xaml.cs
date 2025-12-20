using MedRemind.ViewModels;

namespace MedRemind.Pages;

public partial class AddMedicationPage : ContentPage
{
    public AddMedicationPage(AddMedicationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
