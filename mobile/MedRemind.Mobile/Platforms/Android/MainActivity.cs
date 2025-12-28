using Android.App;
using Android.Content.PM;
using Android.OS;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace MedRemind.Mobile;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        
        // Initialize SSL/TLS settings for Android
        InitializeSSLSettings();
        
        // Request runtime permissions for Android 13+ (API 33+)
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
        {
            RequestPermissions(new[]
            {
                Android.Manifest.Permission.Camera,
                Android.Manifest.Permission.ReadMediaImages,
                Android.Manifest.Permission.PostNotifications
            }, 0);
        }
        else if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
        {
            RequestPermissions(new[]
            {
                Android.Manifest.Permission.Camera,
                Android.Manifest.Permission.ReadExternalStorage,
                Android.Manifest.Permission.WriteExternalStorage
            }, 0);
        }
    }
    
    private void InitializeSSLSettings()
    {
        try
        {
#if DEBUG
            // In DEBUG mode, bypass SSL certificate validation for development
            // This helps with Android emulator SSL issues
            System.Net.ServicePointManager.ServerCertificateValidationCallback = 
                (sender, certificate, chain, sslPolicyErrors) =>
                {
                    if (sslPolicyErrors != SslPolicyErrors.None)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ SSL Policy Error: {sslPolicyErrors}");
                        if (certificate != null)
                        {
                            var cert = certificate as X509Certificate2 ?? new X509Certificate2(certificate);
                            System.Diagnostics.Debug.WriteLine($"   Certificate: {cert.Subject}");
                            System.Diagnostics.Debug.WriteLine($"   Issuer: {cert.Issuer}");
                            System.Diagnostics.Debug.WriteLine($"   Valid: {cert.NotBefore} to {cert.NotAfter}");
                        }
                        System.Diagnostics.Debug.WriteLine("   ✅ Accepting certificate in DEBUG mode");
                    }
                    return true; // Accept all certificates in DEBUG
                };
            
            System.Diagnostics.Debug.WriteLine("✅ SSL Certificate Validation: DEBUG mode (Accept All)");
#else
            // In RELEASE mode, use default validation
            System.Net.ServicePointManager.ServerCertificateValidationCallback = null;
            System.Diagnostics.Debug.WriteLine("✅ SSL Certificate Validation: RELEASE mode (Default)");
#endif
            
            // Set security protocol to TLS 1.2 and 1.3
            System.Net.ServicePointManager.SecurityProtocol = 
                System.Net.SecurityProtocolType.Tls12 | 
                System.Net.SecurityProtocolType.Tls13;
            
            System.Diagnostics.Debug.WriteLine("✅ SSL/TLS Protocols: TLS 1.2, TLS 1.3");
            
            // Disable certificate revocation checking for Android (can cause delays)
            System.Net.ServicePointManager.CheckCertificateRevocationList = false;
            
            System.Diagnostics.Debug.WriteLine("✅ SSL initialization complete");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ SSL initialization error: {ex.Message}");
        }
    }
}
