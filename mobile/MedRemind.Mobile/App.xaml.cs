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
		// Check authentication status
		MainThread.BeginInvokeOnMainThread(async () =>
		{
			await CheckAuthenticationStatusAsync();
		});

		return new Window(new AppShell());
	}

	private async Task CheckAuthenticationStatusAsync()
	{
		try
		{
			// Check if user is logged in
			var userId = await SecureStorage.GetAsync("user_id");
			var sessionToken = await SecureStorage.GetAsync("session_token");

			if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(sessionToken))
			{
				// User is logged in, navigate to home
				await Shell.Current.GoToAsync("///HomePage");
			}
			else
			{
				// Not logged in, show login page
				await Shell.Current.GoToAsync("///LoginPage");
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error checking auth status: {ex.Message}");
			await Shell.Current.GoToAsync("///LoginPage");
		}
	}
}