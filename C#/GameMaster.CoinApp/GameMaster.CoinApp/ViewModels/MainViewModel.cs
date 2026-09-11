using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameMaster.Sdk;
using GameMaster.Sdk.Models;

namespace GameMaster.CoinApp.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly GameMasterClient _client;
    private DispatcherTimer? _timer;
    private PlayerProfile? _currentProfile;

    [ObservableProperty]
    private int _coins;

    [ObservableProperty]
    private int _selectedPillarPoints;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _selectedPillar = "PhysicalExp";

    partial void OnSelectedPillarChanged(string value)
    {
        UpdateSelectedPillarPoints();
    }

    public string[] AvailablePillars { get; } = 
    {
        "PhysicalExp",
        "MentalExp",
        "EmotionalExp",
        "SocialExp",
        "SpiritualExp"
    };

    public MainViewModel()
    {
        _client = new GameMasterClient();
        
        // Initial load
        _ = LoadProfileAsync();

        // Setup timer to periodically fetch (every 3 seconds)
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _timer.Tick += async (s, e) => await LoadProfileAsync();
        _timer.Start();
    }

    [RelayCommand]
    private async Task LoadProfileAsync()
    {
        try
        {
            var profile = await _client.GetPlayerProfileAsync();
            
            // The SDK client returns DisplayName = "Error: ..." on exception
            if (profile != null && profile.DisplayName != null && profile.DisplayName.StartsWith("Error:"))
            {
                Coins = 0;
                SelectedPillarPoints = 0;
            }
            else if (profile != null)
            {
                _currentProfile = profile;
                Coins = profile.Coins;
                UpdateSelectedPillarPoints();
            }
        }
        catch (Exception)
        {
            // Ignore
        }
    }

    private void UpdateSelectedPillarPoints()
    {
        if (_currentProfile == null) return;

        SelectedPillarPoints = SelectedPillar switch
        {
            "PhysicalExp" => _currentProfile.PhysicalExp,
            "MentalExp" => _currentProfile.MentalExp,
            "EmotionalExp" => _currentProfile.EmotionalExp,
            "SocialExp" => _currentProfile.SocialExp,
            "SpiritualExp" => _currentProfile.SpiritualExp,
            _ => 0
        };
    }

    [RelayCommand]
    private async Task AddCoinAsync()
    {
        var result = await _client.AdjustResourceAsync("Coins", 1, "Added coin via UI");
        if (result.Success)
            await LoadProfileAsync();
    }

    [RelayCommand]
    private async Task RemoveCoinAsync()
    {
        var result = await _client.AdjustResourceAsync("Coins", -1, "Removed coin via UI");
        if (result.Success)
            await LoadProfileAsync();
    }

    [RelayCommand]
    private async Task AddPillarPointAsync()
    {
        var result = await _client.AdjustResourceAsync(SelectedPillar, 1, $"Added {SelectedPillar} via UI");
        if (result.Success)
            await LoadProfileAsync();
    }

    [RelayCommand]
    private async Task RemovePillarPointAsync()
    {
        var result = await _client.AdjustResourceAsync(SelectedPillar, -1, $"Removed {SelectedPillar} via UI");
        if (result.Success)
            await LoadProfileAsync();
    }
}
