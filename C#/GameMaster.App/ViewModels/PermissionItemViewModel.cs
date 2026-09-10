using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameMaster.Core.Models;
using GameMaster.Core.Services;
using CoreResource = GameMaster.Core.Models.Resource;

namespace GameMaster.App.ViewModels;

public partial class PermissionItemViewModel : ViewModelBase
{
    private readonly IPermissionsService _permissionsService;
    private readonly Guid _appId;

    [ObservableProperty]
    private CoreResource _resource;

    [ObservableProperty]
    private PermissionAction _action;

    [ObservableProperty]
    private bool _isGranted;

    [ObservableProperty]
    private bool _isPending;
    
    [ObservableProperty]
    private bool _isDenied;

    public PermissionItemViewModel(
        IPermissionsService permissionsService, 
        Guid appId, 
        CoreResource resource, 
        PermissionAction action,
        bool isGranted,
        bool isPending)
    {
        _permissionsService = permissionsService;
        _appId = appId;
        _resource = resource;
        _action = action;
        _isGranted = isGranted;
        _isPending = isPending;
        _isDenied = !isGranted && !isPending;
    }

    [RelayCommand]
    public async Task GrantAsync()
    {
        await _permissionsService.GrantAsync(_appId, Resource, Action);
        IsGranted = true;
        IsPending = false;
        IsDenied = false;
    }

    [RelayCommand]
    public async Task RevokeAsync()
    {
        await _permissionsService.RevokeAsync(_appId, Resource, Action);
        IsGranted = false;
        IsPending = false;
        IsDenied = true;
    }
}
