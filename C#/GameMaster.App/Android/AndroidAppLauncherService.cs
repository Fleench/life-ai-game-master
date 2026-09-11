#if ANDROID
using Android.Content;
using GameMaster.App.Services;

namespace GameMaster.App.Android;

public class AndroidAppLauncherService : IAppLauncherService
{
    public void LaunchApp(string packageName)
    {
        var context = global::Android.App.Application.Context;
        var intent = context.PackageManager?.GetLaunchIntentForPackage(packageName);
        if (intent != null)
        {
            intent.AddFlags(ActivityFlags.NewTask);
            context.StartActivity(intent);
        }
    }

    public void OpenAppInfo(string packageName)
    {
        var context = global::Android.App.Application.Context;
        var intent = new Intent(global::Android.Provider.Settings.ActionApplicationDetailsSettings);
        intent.SetData(global::Android.Net.Uri.Parse($"package:{packageName}"));
        intent.AddFlags(ActivityFlags.NewTask);
        context.StartActivity(intent);
    }

    public void UninstallApp(string packageName)
    {
        var context = global::Android.App.Application.Context;
        var intent = new Intent(Intent.ActionDelete);
        intent.SetData(global::Android.Net.Uri.Parse($"package:{packageName}"));
        intent.AddFlags(ActivityFlags.NewTask);
        context.StartActivity(intent);
    }
}
#endif
