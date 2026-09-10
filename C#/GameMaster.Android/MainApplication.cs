using System;
using System.IO;
using Android.App;
using Android.Runtime;
using Microsoft.Extensions.DependencyInjection;
using GameMaster.Core.Services;
using GameMaster.Data.Services;
using GameMaster.Data;
namespace GameMaster.Android;

[Application]
public class MainApplication : Application
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    public MainApplication(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
    {
    }

    public override void OnCreate()
    {
        base.OnCreate();

        var services = new ServiceCollection();

        // Path for SQLite on Android (/data/data/<package>/databases/)
        var dbPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "gamemaster.db");
        var connectionString = $"Data Source={dbPath}";

        services.AddGameMasterData(connectionString);

        ServiceProvider = services.BuildServiceProvider();
        
        // Ensure DB is created/migrated
        var initializer = ServiceProvider.GetService<DatabaseInitializer>();
        if (initializer != null)
        {
            _ = initializer.InitializeAsync();
        }
    }
}
