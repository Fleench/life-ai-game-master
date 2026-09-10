using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using GameMaster.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GameMaster.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _playerName = "Unknown";

    [ObservableProperty]
    private int _expBalance = 0;

    [ObservableProperty]
    private int _coinsBalance = 0;

    [ObservableProperty]
    private int _inventoryCount = 0;

    public ReplViewModel ReplVm { get; }
    public AppHubViewModel AppHubVm { get; }
    public PendingPermissionsViewModel PendingPermissionsVm { get; }

    public MainViewModel()
    {
        ReplVm = new ReplViewModel();
        ReplVm.CommandExecuted += (s, e) => LoadStats();
        AppHubVm = new AppHubViewModel();
        PendingPermissionsVm = new PendingPermissionsViewModel();
        
        LoadStats();
    }
    
    private async void LoadStats()
    {
        if (AppHost.Services == null) return;
        
        var playerService = AppHost.Services.GetService<IPlayerService>();
        var pointsService = AppHost.Services.GetService<IPointsService>();
        var inventoryService = AppHost.Services.GetService<IInventoryService>();
        
        if (playerService != null)
        {
            var player = await playerService.GetPlayerAsync();
            if (player != null)
                PlayerName = player.DisplayName;
        }
        
        if (pointsService != null)
        {
            var balances = await pointsService.GetBalancesAsync();
            ExpBalance = System.Linq.Enumerable.FirstOrDefault(balances, b => b.CurrencyId == "exp_points")?.Balance ?? 0;
            CoinsBalance = System.Linq.Enumerable.FirstOrDefault(balances, b => b.CurrencyId == "coins")?.Balance ?? 0;
        }

        if (inventoryService != null)
        {
            var items = await inventoryService.GetItemsAsync();
            InventoryCount = items.Sum(i => i.Quantity);
        }
    }
}
