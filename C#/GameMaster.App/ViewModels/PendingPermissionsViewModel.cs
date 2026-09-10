using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameMaster.Core.Models;
using GameMaster.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GameMaster.App.ViewModels;

public partial class PendingPermission : ObservableObject
{
    public Guid AppId { get; init; }
    public string AppName { get; init; } = string.Empty;
    public GameMaster.Core.Models.Resource Resource { get; init; }
    public PermissionAction Action { get; init; }
    public string DisplayText => $"{AppName} wants {Action} access to {Resource}";
}

public partial class PendingPermissionsViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<PendingPermission> _pendingRequests = new();

    [ObservableProperty]
    private bool _hasPendingRequests;

    public PendingPermissionsViewModel()
    {
        _ = LoadPendingAsync();
    }

    public async Task LoadPendingAsync()
    {
        if (AppHost.Services == null) return;
        var permService = AppHost.Services.GetService<IPermissionsService>();
        var appService = AppHost.Services.GetService<IAppRegistryService>();
        if (permService == null || appService == null) return;

        var pending = await permService.GetPendingRequestsAsync();
        var apps = await appService.ListAppsAsync();
        
        var list = new ObservableCollection<PendingPermission>();
        foreach (var p in pending)
        {
            var app = System.Linq.Enumerable.FirstOrDefault(apps, a => a.AppId == p.AppId);
            list.Add(new PendingPermission
            {
                AppId = p.AppId,
                AppName = app?.AppName ?? p.AppId.ToString(),
                Resource = p.Resource,
                Action = p.Action
            });
        }
        PendingRequests = list;
        HasPendingRequests = list.Count > 0;
    }

    [RelayCommand]
    private async Task ApproveAsync(PendingPermission request)
    {
        if (AppHost.Services == null) return;
        var permService = AppHost.Services.GetRequiredService<IPermissionsService>();
        await permService.GrantAsync(request.AppId, request.Resource, request.Action);
        await LoadPendingAsync();
    }

    [RelayCommand]
    private async Task DenyAsync(PendingPermission request)
    {
        if (AppHost.Services == null) return;
        var permService = AppHost.Services.GetRequiredService<IPermissionsService>();
        await permService.RevokeAsync(request.AppId, request.Resource, request.Action);
        await LoadPendingAsync();
    }
}
