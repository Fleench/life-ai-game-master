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

    public ProfileViewModel ProfileVm { get; }
    public AppHubViewModel AppHubVm { get; }

    public MainViewModel()
    {
        ProfileVm = new ProfileViewModel();
        AppHubVm = new AppHubViewModel();
        
        LoadStats();

        var timer = new Avalonia.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        timer.Tick += (s, e) => { LoadStats(); };
        timer.Start();
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
            var bList = balances.ToList();
            
            var physical = bList.FirstOrDefault(b => b.CurrencyId == "physicalexp")?.Balance ?? 0;
            var mental = bList.FirstOrDefault(b => b.CurrencyId == "mentalexp")?.Balance ?? 0;
            var emotional = bList.FirstOrDefault(b => b.CurrencyId == "emotionalexp")?.Balance ?? 0;
            var social = bList.FirstOrDefault(b => b.CurrencyId == "socialexp")?.Balance ?? 0;
            var spiritual = bList.FirstOrDefault(b => b.CurrencyId == "spiritualexp")?.Balance ?? 0;
            
            ExpBalance = physical + mental + emotional + social + spiritual;
            CoinsBalance = bList.FirstOrDefault(b => b.CurrencyId == "coins")?.Balance ?? 0;
        }

        if (inventoryService != null)
        {
            var items = await inventoryService.GetItemsAsync();
            InventoryCount = items.Sum(i => i.Quantity);
        }
    }
}
