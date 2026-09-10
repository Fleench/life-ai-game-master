using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameMaster.Sdk;

namespace GameMaster.CoinApp.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly GameMasterClient _client;

    [ObservableProperty]
    private int _coins;

    [ObservableProperty]
    private int _points;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isAwaitingPermission;

    [ObservableProperty]
    private string _permissionStatusMessage = string.Empty;

    public Action? OnRequestLaunchGameMaster { get; set; }

    public MainViewModel()
    {
        _client = new GameMasterClient();
        // Load initial profile data
        _ = LoadProfileAsync();
    }

    [RelayCommand]
    private async Task LoadProfileAsync()
    {
        IsLoading = true;
        IsAwaitingPermission = false;
        try
        {
            var profile = await _client.GetPlayerProfileAsync();
            
            // The SDK client returns DisplayName = "Error: ..." on exception
            if (profile != null && profile.DisplayName != null && profile.DisplayName.StartsWith("Error:"))
            {
                IsAwaitingPermission = true;
                PermissionStatusMessage = "Could not load data from GameMaster. Open GameMaster app and approve permissions for CoinApp, then tap Refresh.";
                Coins = 0;
                Points = 0;
                OnRequestLaunchGameMaster?.Invoke();
            }
            else if (profile != null)
            {
                Coins = profile.Coins;
                Points = profile.Points;
                IsAwaitingPermission = false;
                PermissionStatusMessage = string.Empty;
            }
        }
        catch (Exception ex)
        {
            IsAwaitingPermission = true;
            PermissionStatusMessage = $"Could not connect to GameMaster: {ex.Message}";
            OnRequestLaunchGameMaster?.Invoke();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddCoinAsync()
    {
        var result = await _client.AdjustCoinsAsync(1, "Added coin via UI");
        if (result.Success)
        {
            Coins = result.NewBalance;
        }
        else
        {
            IsAwaitingPermission = true;
            PermissionStatusMessage = "Action denied. Check GameMaster permissions.";
            OnRequestLaunchGameMaster?.Invoke();
        }
    }

    [RelayCommand]
    private async Task RemoveCoinAsync()
    {
        var result = await _client.AdjustCoinsAsync(-1, "Removed coin via UI");
        if (result.Success)
        {
            Coins = result.NewBalance;
        }
        else
        {
            IsAwaitingPermission = true;
            PermissionStatusMessage = "Action denied. Check GameMaster permissions.";
            OnRequestLaunchGameMaster?.Invoke();
        }
    }
}
