#if ANDROID
using Android.Content;
using GameMaster.App.Services;
using GameMaster.Core.Models;

namespace GameMaster.App.Android;

public class AndroidAppLauncherService : IAppLauncherService
{
    public void LaunchApp(ConnectedApp app)
    {
        var context = global::Android.App.Application.Context;
        string? packageName = null;
        if (app.AndroidUid.HasValue)
        {
            var packages = context.PackageManager?.GetPackagesForUid(app.AndroidUid.Value);
            if (packages != null && packages.Length > 0)
            {
                packageName = packages[0];
            }
        }
        
        if (packageName == null)
        {
            packageName = app.AppName;
        }
        
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
