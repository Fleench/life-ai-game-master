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
        var profile = await _client.GetPlayerProfileAsync();
        
        Coins = profile.Coins;
        Points = profile.Points;
        IsLoading = false;
    }

    [RelayCommand]
    private async Task AddCoinAsync()
    {
        var result = await _client.AdjustCoinsAsync(1, "Added coin via UI");
        if (result.Success)
        {
            Coins = result.NewBalance;
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
    }
}
