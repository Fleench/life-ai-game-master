using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GameMaster.Core.Models;
using GameMaster.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GameMaster.App.ViewModels;

public partial class AppHubViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<ConnectedApp> _apps = new();

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
            Apps = new ObservableCollection<ConnectedApp>(appsList);
        }
    }
}
