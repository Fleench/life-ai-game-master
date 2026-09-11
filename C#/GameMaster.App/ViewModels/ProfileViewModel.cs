using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using GameMaster.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GameMaster.App.ViewModels;

public partial class ProfileViewModel : ViewModelBase
{
    [ObservableProperty] private string _playerName = "Unknown";
    [ObservableProperty] private string _bio = "Your local bio here...";
    [ObservableProperty] private string _profileImagePath = "avares://GameMaster.App/Assets/default_avatar.png";
    
    // Overall Life
    [ObservableProperty] private int _lifeLevel = 1;
    [ObservableProperty] private int _totalExp = 0;
    [ObservableProperty] private double _lifeProgress = 0;
    
    // 5 EXP Pillars
    [ObservableProperty] private int _physicalExp = 0;
    [ObservableProperty] private int _physicalLevel = 1;
    [ObservableProperty] private double _physicalProgress = 0;
    
    [ObservableProperty] private int _mentalExp = 0;
    [ObservableProperty] private int _mentalLevel = 1;
    [ObservableProperty] private double _mentalProgress = 0;
    
    [ObservableProperty] private int _emotionalExp = 0;
    [ObservableProperty] private int _emotionalLevel = 1;
    [ObservableProperty] private double _emotionalProgress = 0;
    
    [ObservableProperty] private int _socialExp = 0;
    [ObservableProperty] private int _socialLevel = 1;
    [ObservableProperty] private double _socialProgress = 0;
    
    [ObservableProperty] private int _spiritualExp = 0;
    [ObservableProperty] private int _spiritualLevel = 1;
    [ObservableProperty] private double _spiritualProgress = 0;
    
    // Others
    [ObservableProperty] private int _coinsBalance = 0;
    [ObservableProperty] private int _inventoryCount = 0;

    public ProfileViewModel()
    {
        _ = LoadProfileAsync();
        
        var timer = new Avalonia.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        timer.Tick += (s, e) => { _ = LoadProfileAsync(); };
        timer.Start();
    }

    public async Task LoadProfileAsync()
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
            var balances = (await pointsService.GetBalancesAsync()).ToList();
            
            PhysicalExp = balances.FirstOrDefault(b => b.CurrencyId == "physicalexp")?.Balance ?? 0;
            MentalExp = balances.FirstOrDefault(b => b.CurrencyId == "mentalexp")?.Balance ?? 0;
            EmotionalExp = balances.FirstOrDefault(b => b.CurrencyId == "emotionalexp")?.Balance ?? 0;
            SocialExp = balances.FirstOrDefault(b => b.CurrencyId == "socialexp")?.Balance ?? 0;
            SpiritualExp = balances.FirstOrDefault(b => b.CurrencyId == "spiritualexp")?.Balance ?? 0;
            
            CoinsBalance = balances.FirstOrDefault(b => b.CurrencyId == "coins")?.Balance ?? 0;

            TotalExp = PhysicalExp + MentalExp + EmotionalExp + SocialExp + SpiritualExp;

            UpdateLevels();
        }

        if (inventoryService != null)
        {
            var items = await inventoryService.GetItemsAsync();
            InventoryCount = items.Sum(i => i.Quantity);
        }
    }

    private void UpdateLevels()
    {
        LifeLevel = (int)Math.Floor(Math.Sqrt(TotalExp / 250.0)) + 1;
        
        int expForCurrentLife = (int)Math.Pow(LifeLevel - 1, 2) * 250;
        int expForNextLife = (int)Math.Pow(LifeLevel, 2) * 250;
        int lifeRange = expForNextLife - expForCurrentLife;
        int lifeProgress = TotalExp - expForCurrentLife;
        LifeProgress = lifeRange == 0 ? 0 : (lifeProgress / (double)lifeRange) * 100.0;
        
        PhysicalLevel = CalculatePillarLevel(PhysicalExp);
        MentalLevel = CalculatePillarLevel(MentalExp);
        EmotionalLevel = CalculatePillarLevel(EmotionalExp);
        SocialLevel = CalculatePillarLevel(SocialExp);
        SpiritualLevel = CalculatePillarLevel(SpiritualExp);

        PhysicalProgress = CalculatePillarProgress(PhysicalExp, PhysicalLevel);
        MentalProgress = CalculatePillarProgress(MentalExp, MentalLevel);
        EmotionalProgress = CalculatePillarProgress(EmotionalExp, EmotionalLevel);
        SocialProgress = CalculatePillarProgress(SocialExp, SocialLevel);
        SpiritualProgress = CalculatePillarProgress(SpiritualExp, SpiritualLevel);
    }
    
    private int CalculatePillarLevel(int exp)
    {
        return (int)Math.Floor(Math.Sqrt(exp / 100.0)) + 1;
    }

    private double CalculatePillarProgress(int exp, int currentLevel)
    {
        int expForCurrent = (int)Math.Pow(currentLevel - 1, 2) * 100;
        int expForNext = (int)Math.Pow(currentLevel, 2) * 100;
        
        int range = expForNext - expForCurrent;
        int progress = exp - expForCurrent;
        
        if (range == 0) return 0;
        return (progress / (double)range) * 100.0;
    }
}
