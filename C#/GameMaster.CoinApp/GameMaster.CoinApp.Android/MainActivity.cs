using Android.App;
using Android.Content;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;
using System;

namespace GameMaster.CoinApp.Android;

[Activity(
    Label = "GameMaster.CoinApp.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    Exported = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont()
            .AfterSetup(_ =>
            {
                if (global::Avalonia.Application.Current is App app)
                {
                    app.OnRequestLaunchGameMaster = () =>
                    {
                        var intent = PackageManager?.GetLaunchIntentForPackage("com.gamemaster.app");
                        if (intent != null)
                        {
                            intent.AddFlags(ActivityFlags.NewTask);
                            StartActivity(intent);
                        }
                    };
                }
            });
    }
}
