using Android.App;
using Android.Content.PM;
using Android.OS;

namespace MedRemind.Mobile;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        
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
}
