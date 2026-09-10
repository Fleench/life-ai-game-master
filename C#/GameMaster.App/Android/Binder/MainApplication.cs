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
        GameMaster.App.AppHost.Initialize();
        ServiceProvider = GameMaster.App.AppHost.Services;
    }
}
