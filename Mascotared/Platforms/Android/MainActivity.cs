using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace Mascotared
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]

    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (Window != null)
            {
                Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#455AEB"));
                Window.DecorView.SystemUiFlags = SystemUiFlags.Visible;
            }

            //CreateNotificationChannel(); // solo Android 8+
        }
    }
}