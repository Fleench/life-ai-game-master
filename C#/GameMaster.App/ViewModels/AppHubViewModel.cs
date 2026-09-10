using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameMaster.Core.Models;
using GameMaster.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using CoreResource = GameMaster.Core.Models.Resource;

namespace GameMaster.App.ViewModels;

public partial class AppHubViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<AppItemViewModel> _apps = new();

    [ObservableProperty]
    private AppItemViewModel? _selectedApp;

    [ObservableProperty]
    private ObservableCollection<PermissionItemViewModel> _permissions = new();

    public AppHubViewModel()
    {
        LoadApps();
    }
    
    private async void LoadApps()
    {
        if (AppHost.Services == null) return;
        var appService = AppHost.Services.GetService<IAppRegistryService>();
        if (appService != null)
        {
            var appsList = await appService.ListAppsAsync();
            Apps = new ObservableCollection<AppItemViewModel>(appsList.Select(a => new AppItemViewModel(a)));


        }
    }

    partial void OnSelectedAppChanged(AppItemViewModel? value)
    {
        if (value != null)
        {
            _ = LoadPermissionsAsync(value.AppId);
        }
        else
        {
            Permissions.Clear();
        }
    }

    private async Task LoadPermissionsAsync(Guid appId)
    {
        if (AppHost.Services == null) return;
        var permissionsService = AppHost.Services.GetService<IPermissionsService>();
        if (permissionsService == null) return;

        var existing = (await permissionsService.GetPermissionsAsync(appId)).ToList();
        var pendingRequests = (await permissionsService.GetPendingRequestsAsync()).Where(p => p.AppId == appId).ToList();

        var viewModels = new ObservableCollection<PermissionItemViewModel>();

        foreach (CoreResource resource in Enum.GetValues(typeof(CoreResource)))
        {
            foreach (PermissionAction action in Enum.GetValues(typeof(PermissionAction)))
            {
                var granted = existing.Any(p => p.Resource == resource && p.Action == action && p.Granted);
                var pending = pendingRequests.Any(p => p.Resource == resource && p.Action == action && !p.Granted);
                
                viewModels.Add(new PermissionItemViewModel(permissionsService, appId, resource, action, granted, pending));
            }
        }

        Permissions = viewModels;
    }

    [RelayCommand]
    private async Task RevokeAppAsync()
    {
        if (SelectedApp == null || AppHost.Services == null) return;
        var appService = AppHost.Services.GetService<IAppRegistryService>();
        if (appService != null)
        {
            await appService.DeregisterAppAsync(SelectedApp.AppId);
            Apps.Remove(SelectedApp);
            SelectedApp = null;
        }
    }

    [RelayCommand]
    private void ClearSelection()
    {
        SelectedApp = null;
    }
}
