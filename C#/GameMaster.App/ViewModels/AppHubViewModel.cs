using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameMaster.Core.Models;
using GameMaster.Core.Services;
using GameMaster.App.Services;
using Microsoft.Extensions.DependencyInjection;
using CoreResource = GameMaster.Core.Models.Resource;

using Avalonia.Threading;

namespace GameMaster.App.ViewModels;

public partial class AppHubViewModel : ViewModelBase
{
    private readonly IAppLauncherService? _launcherService;
    private readonly IAppRegistryService? _appService;
    private readonly DispatcherTimer _refreshTimer;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    [ObservableProperty]
    private ObservableCollection<AppItemViewModel> _apps = new();

    public AppHubViewModel()
    {
        // Parameterless constructor for designer or backwards compatibility
        if (AppHost.Services != null)
        {
            _launcherService = AppHost.Services.GetService<IAppLauncherService>();
            _appService = AppHost.Services.GetService<IAppRegistryService>();
        }
        _ = LoadAppsAsync();
        
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _refreshTimer.Tick += async (sender, e) => await LoadAppsAsync();
        _refreshTimer.Start();
    }

    public AppHubViewModel(IAppLauncherService? launcherService, IAppRegistryService? appService)
    {
        _launcherService = launcherService;
        _appService = appService;
        _ = LoadAppsAsync();
        
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _refreshTimer.Tick += async (sender, e) => await LoadAppsAsync();
        _refreshTimer.Start();
    }
    
    [RelayCommand]
    private async Task LoadAppsAsync()
    {
        // Guard against overlapping concurrent loads (timer + manual trigger)
        if (!await _loadLock.WaitAsync(0)) return;
        try
        {
            IAppRegistryService? svc = _appService;
            if (svc == null && AppHost.Services != null)
                svc = AppHost.Services.GetService<IAppRegistryService>();

            if (svc != null)
            {
                var appsList = await svc.ListAppsAsync();
                var appIds = appsList.Select(a => a.AppId).ToHashSet();
                
                var toRemove = Apps.Where(a => !appIds.Contains(a.AppId)).ToList();
                foreach (var a in toRemove)
                {
                    Apps.Remove(a);
                }

                foreach (var la in appsList)
                {
                    if (!Apps.Any(a => a.AppId == la.AppId))
                    {
                        Apps.Add(new AppItemViewModel(la));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log to console; don't crash the UI — list simply retains its last known state
            Console.Error.WriteLine($"[AppHubViewModel] LoadAppsAsync failed: {ex.Message}");
        }
        finally
        {
            _loadLock.Release();
        }
    }

    [RelayCommand]
    private void LaunchApp(AppItemViewModel? appViewModel)
    {
        if (appViewModel == null || _launcherService == null) return;
        _launcherService.LaunchApp(appViewModel.App);
    }

    [RelayCommand]
    private void OpenAppInfo(AppItemViewModel? app)
    {
        if (app == null || _launcherService == null) return;
        _launcherService.OpenAppInfo(app.AppName);
    }

    [RelayCommand]
    private async Task DeleteAppAsync(AppItemViewModel? app)
    {
        if (app == null) return;
        
        if (_launcherService != null)
        {
            _launcherService.UninstallApp(app.AppName);
        }

        if (_appService != null)
        {
            await _appService.DeregisterAppAsync(app.AppId);
            Apps.Remove(app);
        }
    }
}
