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
        try
        {
            var profile = await _client.GetPlayerProfileAsync();
            
            // The SDK client returns DisplayName = "Error: ..." on exception
            if (profile != null && profile.DisplayName != null && profile.DisplayName.StartsWith("Error:"))
            {
                Coins = 0;
                Points = 0;
            }
            else if (profile != null)
            {
                Coins = profile.Coins;
                Points = profile.Points;
            }
        }
        catch (Exception)
        {
            // Ignored, banner removed
        }
        finally
        {
            IsLoading = false;
        }
    }

    [ObservableProperty]
    private string _selectedResource = "Coins";

    public string[] AvailableResources { get; } = 
    {
        "Coins",
        "PhysicalExp",
        "MentalExp",
        "EmotionalExp",
        "SocialExp",
        "SpiritualExp"
    };

    [RelayCommand]
    private async Task AddResourceAsync()
    {
        var result = await _client.AdjustResourceAsync(SelectedResource, 1, "Added resource via UI");
        if (result.Success)
        {
            await LoadProfileAsync();
        }
    }

    [RelayCommand]
    private async Task RemoveResourceAsync()
    {
        var result = await _client.AdjustResourceAsync(SelectedResource, -1, "Removed resource via UI");
        if (result.Success)
        {
            await LoadProfileAsync();
        }
    }
}
