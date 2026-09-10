using System;
using System.IO;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using GameMaster.Core.Models;

namespace GameMaster.App.ViewModels;

public partial class AppItemViewModel : ViewModelBase
{
    private readonly ConnectedApp _app;
    
    [ObservableProperty]
    private Bitmap? _icon;

    public AppItemViewModel(ConnectedApp app)
    {
        _app = app;
        
        if (app.IconData != null && app.IconData.Length > 0)
        {
            try
            {
                using var ms = new MemoryStream(app.IconData);
                Icon = new Bitmap(ms);
            }
            catch
            {
                // Fallback to null if invalid
                Icon = null;
            }
        }
    }
    
    public ConnectedApp App => _app;
    public Guid AppId => _app.AppId;
    public string AppName => _app.AppName;
    public Platform Platform => _app.Platform;
    public DateTime RegisteredAt => _app.RegisteredAt;
}
