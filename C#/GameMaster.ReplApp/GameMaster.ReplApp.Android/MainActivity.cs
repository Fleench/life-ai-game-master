using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Avalonia;
using Avalonia.Android;
using System;

namespace GameMaster.ReplApp.Android;

[Activity(
    Label = "GameMaster.ReplApp.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    Exported = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        RequestPermissions(new[] { 
            "com.gamemaster.permission.READ_COINS",
            "com.gamemaster.permission.AWARD_COINS",
            "com.gamemaster.permission.SPEND_COINS",
            "com.gamemaster.permission.MANAGE_COINS",
            "com.gamemaster.permission.READ_EXPPOINTS",
            "com.gamemaster.permission.AWARD_EXPPOINTS",
            "com.gamemaster.permission.SPEND_EXPPOINTS",
            "com.gamemaster.permission.MANAGE_EXPPOINTS",
            "com.gamemaster.permission.READ_INVENTORY",
            "com.gamemaster.permission.AWARD_INVENTORY",
            "com.gamemaster.permission.SPEND_INVENTORY",
            "com.gamemaster.permission.MANAGE_INVENTORY"
        }, 0);
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }
}
