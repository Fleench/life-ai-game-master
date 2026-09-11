using System;

namespace GameMaster.App.Services;

public class DesktopAppLauncherService : IAppLauncherService
{
    public void LaunchApp(string packageName)
    {
        Console.WriteLine($"[DesktopAppLauncherService] LaunchApp: {packageName}");
    }

    public void OpenAppInfo(string packageName)
    {
        Console.WriteLine($"[DesktopAppLauncherService] OpenAppInfo: {packageName}");
    }

    public void UninstallApp(string packageName)
    {
        Console.WriteLine($"[DesktopAppLauncherService] UninstallApp: {packageName}");
    }
}
