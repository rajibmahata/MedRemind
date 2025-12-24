using MedRemind.Core.Interfaces;

namespace MedRemind.Mobile;

public partial class AppShell : Shell
{
    private readonly IBiometricService? _biometricService;
    private readonly IAuthenticationService? _authService;
    private bool _isAuthChecked = false;

    public AppShell()
    {
        InitializeComponent();

        // Register routes
        Routing.RegisterRoute("LoginPage", typeof(Views.LoginPage));

        // Get services from DI
        _biometricService = Handler?.MauiContext?.Services.GetService<IBiometricService>();
        _authService = Handler?.MauiContext?.Services.GetService<IAuthenticationService>();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        if (!_isAuthChecked)
        {
            _isAuthChecked = true;
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await CheckAuthenticationAsync();
            });
        }
    }

    private async Task CheckAuthenticationAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("🔐 AppShell: Checking authentication...");

            // Check if user has valid session
            var sessionToken = await SecureStorage.GetAsync("session_token");
            var userId = await SecureStorage.GetAsync("user_id");

            if (string.IsNullOrEmpty(sessionToken) || string.IsNullOrEmpty(userId))
            {
                System.Diagnostics.Debug.WriteLine("❌ No session found - navigating to Login");
                await GoToAsync("///LoginPage");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"✅ Session found for user: {userId}");

            // Validate session token
            if (_authService != null)
            {
                var isValid = await _authService.ValidateSessionTokenAsync(sessionToken);
                if (!isValid)
                {
                    System.Diagnostics.Debug.WriteLine("❌ Session expired - clearing and navigating to Login");
                    SecureStorage.RemoveAll();
                    await GoToAsync("///LoginPage");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("✅ Session valid");
            }

            // Check if biometric is enabled
            if (_biometricService != null)
            {
                var biometricEnabled = await _biometricService.IsBiometricEnabledAsync();
                var biometricAvailable = await _biometricService.IsBiometricAvailableAsync();

                if (biometricEnabled && biometricAvailable)
                {
                    System.Diagnostics.Debug.WriteLine("🔐 Biometric enabled, prompting...");
                    
                    var result = await _biometricService.AuthenticateAsync("Unlock MedRemind", CancellationToken.None);

                    if (result.Success)
                    {
                        System.Diagnostics.Debug.WriteLine("✅ Biometric authentication succeeded - navigating to Home");
                        await GoToAsync("///HomePage");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("❌ Biometric authentication failed - navigating to Login");
                        await GoToAsync("///LoginPage");
                    }
                }
                else
                {
                    // Session valid, no biometric, go to home
                    System.Diagnostics.Debug.WriteLine("✅ Authentication complete - navigating to Home");
                    await GoToAsync("///HomePage");
                }
            }
            else
            {
                // No biometric service, just go to home
                System.Diagnostics.Debug.WriteLine("✅ No biometric service - navigating to Home");
                await GoToAsync("///HomePage");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Auth check error: {ex.Message}");
            await GoToAsync("///LoginPage");
        }
    }
}
