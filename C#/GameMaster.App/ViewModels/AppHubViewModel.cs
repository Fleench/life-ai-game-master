using System;
using System.Collections.ObjectModel;
using System.Linq;
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
        if (_appService != null)
        {
            var appsList = await _appService.ListAppsAsync();
            Apps = new ObservableCollection<AppItemViewModel>(appsList.Select(a => new AppItemViewModel(a)));
        }
        else if (AppHost.Services != null)
        {
            // Fallback just in case
            var appService = AppHost.Services.GetService<IAppRegistryService>();
            if (appService != null)
            {
                var appsList = await appService.ListAppsAsync();
                Apps = new ObservableCollection<AppItemViewModel>(appsList.Select(a => new AppItemViewModel(a)));
            }
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
