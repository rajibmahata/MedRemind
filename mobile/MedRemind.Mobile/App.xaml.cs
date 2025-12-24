using Microsoft.Extensions.DependencyInjection;
using MedRemind.Core.Interfaces;

namespace MedRemind.Mobile;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}