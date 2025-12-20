using MedRemind.Pages;

namespace MedRemind;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		
		// Register routes for navigation
		Routing.RegisterRoute("addmedication", typeof(AddMedicationPage));
		Routing.RegisterRoute("medicationdetail", typeof(AddMedicationPage));
	}
}
