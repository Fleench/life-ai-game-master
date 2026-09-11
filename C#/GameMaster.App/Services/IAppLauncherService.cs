namespace GameMaster.App.Services;

public interface IAppLauncherService
{
    void LaunchApp(string packageName);
    void OpenAppInfo(string packageName);
    void UninstallApp(string packageName);
}
