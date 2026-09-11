using GameMaster.Core.Models;
using System;

namespace GameMaster.App.Services;

public class DesktopAppLauncherService : IAppLauncherService
{
    public void LaunchApp(ConnectedApp app)
    {
        Console.WriteLine($"[DesktopAppLauncherService] LaunchApp: {app.AppName}");
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
