using System;
using Microsoft.Extensions.DependencyInjection;
using GameMaster.Data;
using GameMaster.Core.Services;
using System.Threading.Tasks;
#if !ANDROID
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.IO;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
#endif

namespace GameMaster.App;

public static class AppHost
{
    public static IServiceProvider Services { get; private set; } = null!;
    private static readonly object _sync = new object();

    public static void Initialize()
    {
        lock (_sync)
        {
            if (Services != null) return;
            var services = new ServiceCollection();
            
            // SQLite database path
            var dbPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GameMaster", "game_master.db");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dbPath)!);
            var connString = $"Data Source={dbPath}";
            
            services.AddGameMasterData(connString);
            
#if ANDROID
            services.AddSingleton<GameMaster.App.Services.IAppLauncherService, GameMaster.App.Android.AndroidAppLauncherService>();
#else
            services.AddSingleton<GameMaster.App.Services.IAppLauncherService, GameMaster.App.Services.DesktopAppLauncherService>();
#endif
            
            Services = services.BuildServiceProvider();
            
            // Initialize DB
            var initializer = Services.GetRequiredService<GameMaster.Data.DatabaseInitializer>();
            initializer.InitializeAsync().Wait();

#if !ANDROID
            Task.Run(() => StartDesktopServer(connString));
#endif
        }
    }

#if !ANDROID
    private static void StartDesktopServer(string connString)
    {
        var builder = WebApplication.CreateBuilder();
        
        var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "gamemaster", "logs", "gamemaster-.log");
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
            .CreateLogger();
        builder.Host.UseSerilog();

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                return RateLimitPartition.GetFixedWindowLimiter("global", partition => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 100,
                    Window = TimeSpan.FromSeconds(10)
                });
            });
        });

        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.ListenAnyIP(7777);
        });

        builder.Services.AddGameMasterData(connString);

        var app = builder.Build();
        app.UseExceptionHandler(exceptionHandlerApp =>
        {
            exceptionHandlerApp.Run(async context =>
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new { Error = "An error occurred" });
            });
        });
        
        app.UseRateLimiter();
        app.MapControllers();
        app.Run();
    }
#endif
}
