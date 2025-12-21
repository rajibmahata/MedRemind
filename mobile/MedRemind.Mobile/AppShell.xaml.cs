namespace MedRemind.Mobile;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
        
        // Register routes for navigation
        Routing.RegisterRoute(nameof(Views.LoginPage), typeof(Views.LoginPage));
        Routing.RegisterRoute("AddMedicationPage", typeof(Views.MedicationsPage));
        Routing.RegisterRoute("EditMedicationPage", typeof(Views.MedicationsPage));
	}
}
