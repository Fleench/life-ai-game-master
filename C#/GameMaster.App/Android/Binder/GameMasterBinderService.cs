using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Microsoft.Extensions.DependencyInjection;
using GameMaster.Core.Models;
using CoreResource = GameMaster.Core.Models.Resource;
using GameMaster.Core.Services;

namespace GameMaster.Android;

[Service(Name = "com.gamemaster.GameMasterBinderService", Exported = true, ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeDataSync)]
[IntentFilter(new[] { "com.gamemaster.BIND_SERVICE" })]
public class GameMasterBinderService : Service
{
    private GameMasterBinder? _binder;
    private const int NOTIFICATION_ID = 10001;
    private const string CHANNEL_ID = "gamemaster_service_channel";

    public override IBinder? OnBind(Intent? intent)
    {
        _binder = new GameMasterBinder(this, GameMaster.App.AppHost.Services);
        return _binder;
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        CreateNotificationChannel();

        Notification.Builder notificationBuilder;
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            notificationBuilder = new Notification.Builder(this, CHANNEL_ID);
        }
        else
        {
#pragma warning disable CS0618
            notificationBuilder = new Notification.Builder(this);
#pragma warning restore CS0618
        }

        notificationBuilder
            .SetContentTitle("GameMaster Background Service")
            .SetContentText("GameMaster IPC Service is running")
            .SetSmallIcon(global::Android.Resource.Drawable.IcMenuInfoDetails)
            .SetOngoing(true);

        var notification = notificationBuilder.Build();

        if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
        {
            StartForeground(NOTIFICATION_ID, notification, global::Android.Content.PM.ForegroundService.TypeDataSync);
        }
        else
        {
            StartForeground(NOTIFICATION_ID, notification);
        }

        return StartCommandResult.Sticky;
    }

    private void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(CHANNEL_ID, "GameMaster Service", NotificationImportance.Low)
            {
                Description = "Keeps the GameMaster IPC Service running in the background"
            };

            var notificationManager = (NotificationManager?)GetSystemService(NotificationService);
            notificationManager?.CreateNotificationChannel(channel);
        }
    }
}

public class GameMasterBinder : Binder, IGameMasterBinder
{
    private readonly Context _context;
    private readonly IServiceProvider _serviceProvider;

    public GameMasterBinder(Context context, IServiceProvider serviceProvider)
    {
        _context = context;
        _serviceProvider = serviceProvider;
    }

    private async Task<ConnectedApp> GetOrRegisterCallerAppAsync(int uid)
    {
        var appRegistry = _serviceProvider.GetRequiredService<IAppRegistryService>();
        
        var apps = await appRegistry.ListAppsAsync();
        foreach (var app in apps)
        {
            if (app.Platform == Platform.Android && app.AndroidUid == uid)
            {
                return app;
            }
        }

        // Auto-register on first call
        string appName = $"AndroidApp_{uid}";
        byte[]? iconData = null;
        var packages = _context.PackageManager?.GetPackagesForUid(uid);
        if (packages != null && packages.Length > 0) {
            try {
                var appInfo = _context.PackageManager?.GetApplicationInfo(packages[0], 0);
                if (appInfo != null) {
                    var label = appInfo.LoadLabel(_context.PackageManager);
                    if (!string.IsNullOrEmpty(label)) appName = label;
                    
                    var drawable = appInfo.LoadIcon(_context.PackageManager);
                    if (drawable != null) {
                        var bitmap = global::Android.Graphics.Bitmap.CreateBitmap(drawable.IntrinsicWidth > 0 ? drawable.IntrinsicWidth : 1, drawable.IntrinsicHeight > 0 ? drawable.IntrinsicHeight : 1, global::Android.Graphics.Bitmap.Config.Argb8888!);
                        var canvas = new global::Android.Graphics.Canvas(bitmap);
                        drawable.SetBounds(0, 0, canvas.Width, canvas.Height);
                        drawable.Draw(canvas);
                        using var stream = new System.IO.MemoryStream();
                        bitmap.Compress(global::Android.Graphics.Bitmap.CompressFormat.Png!, 100, stream);
                        iconData = stream.ToArray();
                    }
                }
            } catch { /* ignore */ }
        }
        var newApp = await appRegistry.RegisterAppAsync(appName, Platform.Android, uid, iconData);
        return newApp.App;
    }





    // --- IGameMasterBinder Implementation ---

    protected override bool OnTransact(int code, Parcel data, Parcel? reply, int flags)
    {
        if (code == 1)
        {
            data.EnforceInterface("com.gamemaster.IGameMaster");
            string method = data.ReadString() ?? "";
            string json = data.ReadString() ?? "";
            
            try 
            {
                string responseJson = HandleMethodCallAsync(method, json).GetAwaiter().GetResult();
                reply?.WriteNoException();
                reply?.WriteString(responseJson);
            }
            catch (Exception ex)
            {
                reply?.WriteException(new Java.Lang.RuntimeException(ex.Message));
            }
            return true;
        }
        return base.OnTransact(code, data, reply, flags);
    }
    
    private async Task<string> HandleMethodCallAsync(string method, string json)
    {
        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase) }
        };

        object? result = null;
        switch (method)
        {
            case "RegisterAppAsync":
                {
                    int uid = CallingUid;
                    var app = await GetOrRegisterCallerAppAsync(uid);
                    result = new { App = app, ApiKey = "" }; // Simplified
                }
                break;
            case "GetPlayerAsync":
                result = await GetPlayer();
                break;
            case "GetPointsAsync":
                {
                    var dict = await GetPoints();
                    var list = new List<object>();
                    foreach (var kvp in dict)
                    {
                        list.Add(new { CurrencyId = kvp.Key.ToString(), Balance = kvp.Value });
                    }
                    result = list;
                }
                break;
            case "AwardPointsAsync":
                {
                    var req = System.Text.Json.JsonSerializer.Deserialize<GameMaster.Client.Models.PointsRequest>(json, options);
                    var resEnum = Enum.Parse<CoreResource>(req!.Resource.ToString());
                    await AwardPoints(resEnum, req.Amount);
                    
                    var dict = await GetPoints();
                    var list = new List<object>();
                    foreach (var kvp in dict)
                    {
                        list.Add(new { CurrencyId = kvp.Key.ToString(), Balance = kvp.Value });
                    }
                    result = list;
                }
                break;
            case "SpendPointsAsync":
                {
                    var req = System.Text.Json.JsonSerializer.Deserialize<GameMaster.Client.Models.PointsRequest>(json, options);
                    var resEnum = Enum.Parse<CoreResource>(req!.Resource.ToString());
                    await SpendPoints(resEnum, req.Amount);
                    
                    var dict = await GetPoints();
                    var list = new List<object>();
                    foreach (var kvp in dict)
                    {
                        list.Add(new { CurrencyId = kvp.Key.ToString(), Balance = kvp.Value });
                    }
                    result = list;
                }
                break;
            case "GetInventoryAsync":
                result = await GetInventory();
                break;
            case "AddInventoryItemAsync":
                {
                    var req = System.Text.Json.JsonSerializer.Deserialize<GameMaster.Client.Models.AddInventoryItemRequest>(json, options);
                    await AddInventoryItem(req!.Name, req.Quantity, req.Metadata);
                }
                break;
            case "RemoveInventoryItemAsync":
                {
                    var req = System.Text.Json.JsonSerializer.Deserialize<GameMaster.Client.Models.RemoveInventoryItemRequest>(json, options);
                    await RemoveInventoryItem(req!.ItemId, req.Quantity);
                }
                break;
            
            case "ListAppsAsync":
                {
                    int uid = CallingUid;
                    var appRegistry = _serviceProvider.GetRequiredService<IAppRegistryService>();
                    result = await appRegistry.ListAppsAsync();
                }
                break;
            case "RevokeAppAsync":
                {
                    int uid = CallingUid;
                    var appRegistry = _serviceProvider.GetRequiredService<IAppRegistryService>();
                    var appId = System.Text.Json.JsonSerializer.Deserialize<Guid>(json, options);
                    await appRegistry.DeregisterAppAsync(appId);
                }
                break;
            
            default:
                throw new Java.Lang.IllegalArgumentException("Unknown method: " + method);
        }

        if (result != null)
        {
            return System.Text.Json.JsonSerializer.Serialize(result, options);
        }
        return "";
    }

    private void EnforceAndroidPermission(string permission)
    {
        if (_context.CheckCallingPermission(permission) != global::Android.Content.PM.Permission.Granted)
            throw new Java.Lang.SecurityException($"Requires {permission}");
    }

    public async Task<Player?> GetPlayer()
    {
        int uid = CallingUid;
        await GetOrRegisterCallerAppAsync(uid); // ensure app is registered, no permission gate for basic profile
        var playerService = _serviceProvider.GetRequiredService<IPlayerService>();
        return await playerService.GetPlayerAsync();
    }

    public async Task<Dictionary<CoreResource, int>> GetPoints()
    {
        int uid = CallingUid;
        var app = await GetOrRegisterCallerAppAsync(uid);
        var pointsService = _serviceProvider.GetRequiredService<IPointsService>();

        var dict = new Dictionary<CoreResource, int>();

        if (_context.CheckCallingPermission("com.gamemaster.permission.READ_EXPPOINTS") == global::Android.Content.PM.Permission.Granted)
        {
            dict[CoreResource.ExpPoints] = (await pointsService.GetBalanceAsync(CoreResource.ExpPoints.ToString().ToLowerInvariant()))?.Balance ?? 0;
            dict[CoreResource.PhysicalExp] = (await pointsService.GetBalanceAsync(CoreResource.PhysicalExp.ToString().ToLowerInvariant()))?.Balance ?? 0;
            dict[CoreResource.MentalExp] = (await pointsService.GetBalanceAsync(CoreResource.MentalExp.ToString().ToLowerInvariant()))?.Balance ?? 0;
            dict[CoreResource.EmotionalExp] = (await pointsService.GetBalanceAsync(CoreResource.EmotionalExp.ToString().ToLowerInvariant()))?.Balance ?? 0;
            dict[CoreResource.SocialExp] = (await pointsService.GetBalanceAsync(CoreResource.SocialExp.ToString().ToLowerInvariant()))?.Balance ?? 0;
            dict[CoreResource.SpiritualExp] = (await pointsService.GetBalanceAsync(CoreResource.SpiritualExp.ToString().ToLowerInvariant()))?.Balance ?? 0;
        }

        if (_context.CheckCallingPermission("com.gamemaster.permission.READ_COINS") == global::Android.Content.PM.Permission.Granted)
        {
            dict[CoreResource.Coins] = (await pointsService.GetBalanceAsync(CoreResource.Coins.ToString().ToLowerInvariant()))?.Balance ?? 0;
        }

        return dict;
    }

    public async Task AwardPoints(CoreResource resource, int amount)
    {
        if (resource == CoreResource.Coins)
            EnforceAndroidPermission("com.gamemaster.permission.AWARD_COINS");
        else if (resource == CoreResource.ExpPoints || resource == CoreResource.PhysicalExp || resource == CoreResource.MentalExp || resource == CoreResource.EmotionalExp || resource == CoreResource.SocialExp || resource == CoreResource.SpiritualExp)
            EnforceAndroidPermission("com.gamemaster.permission.AWARD_EXPPOINTS");

        int uid = CallingUid;
        
        var pointsService = _serviceProvider.GetRequiredService<IPointsService>();
        var app = await GetOrRegisterCallerAppAsync(uid);
        
        await pointsService.AwardPointsAsync(resource.ToString().ToLowerInvariant(), amount);
    }

    public async Task SpendPoints(CoreResource resource, int amount)
    {
        if (resource == CoreResource.Coins)
            EnforceAndroidPermission("com.gamemaster.permission.SPEND_COINS");
        else if (resource == CoreResource.ExpPoints || resource == CoreResource.PhysicalExp || resource == CoreResource.MentalExp || resource == CoreResource.EmotionalExp || resource == CoreResource.SocialExp || resource == CoreResource.SpiritualExp)
            EnforceAndroidPermission("com.gamemaster.permission.SPEND_EXPPOINTS");

        int uid = CallingUid;
        
        var pointsService = _serviceProvider.GetRequiredService<IPointsService>();
        var app = await GetOrRegisterCallerAppAsync(uid);
        
        await pointsService.SpendPointsAsync(resource.ToString().ToLowerInvariant(), amount);
    }

    public async Task<IEnumerable<InventoryItem>> GetInventory()
    {
        EnforceAndroidPermission("com.gamemaster.permission.READ_INVENTORY");

        int uid = CallingUid;
        
        var invService = _serviceProvider.GetRequiredService<IInventoryService>();
        return await invService.GetItemsAsync();
    }

    public async Task AddInventoryItem(string name, int qty, string? metadata)
    {
        int uid = CallingUid;
        
        var invService = _serviceProvider.GetRequiredService<IInventoryService>();
        var app = await GetOrRegisterCallerAppAsync(uid);
        
        await invService.AddItemAsync(name, qty, metadata ?? "", app.AppId.ToString());
    }

    public async Task RemoveInventoryItem(Guid itemId, int qty)
    {
        int uid = CallingUid;
        
        var invService = _serviceProvider.GetRequiredService<IInventoryService>();
        
        // Ensure item exists
        var items = await invService.GetItemsAsync();
        InventoryItem? target = null;
        foreach (var i in items)
        {
            if (i.ItemId == itemId) target = i;
        }

        if (target != null)
        {
            await invService.RemoveItemAsync(itemId, qty);
        }
    }

    
}
