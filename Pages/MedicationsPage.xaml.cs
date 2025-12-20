using MedRemind.ViewModels;

namespace MedRemind.Pages;

public partial class MedicationsPage : ContentPage
{
    public MedicationsPage(MedicationsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var viewModel = (MedicationsViewModel)BindingContext;
        await viewModel.LoadMedicationsCommand.ExecuteAsync(null);
    }
}
