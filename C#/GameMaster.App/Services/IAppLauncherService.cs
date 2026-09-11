using GameMaster.Core.Models;

namespace GameMaster.App.Services;

public interface IAppLauncherService
{
    void LaunchApp(ConnectedApp app);
    void OpenAppInfo(string packageName);
    void UninstallApp(string packageName);
}
