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

[Service(Name = "com.gamemaster.GameMasterBinderService", Exported = true, Permission = "com.gamemaster.BIND")]
[IntentFilter(new[] { "com.gamemaster.BIND_SERVICE" })]
public class GameMasterBinderService : Service
{
    private GameMasterBinder? _binder;

    public override IBinder? OnBind(Intent? intent)
    {
        _binder = new GameMasterBinder(MainApplication.ServiceProvider);
        return _binder;
    }
}

public class GameMasterBinder : Binder, IGameMasterBinder
{
    private readonly IServiceProvider _serviceProvider;

    public GameMasterBinder(IServiceProvider serviceProvider)
    {
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
        var newApp = await appRegistry.RegisterAppAsync(appName, Platform.Android, uid);
        return newApp.App;
    }

    private async Task EnforcePermissionAsync(int uid, CoreResource resource, PermissionAction action)
    {
        var app = await GetOrRegisterCallerAppAsync(uid);
        var permService = _serviceProvider.GetRequiredService<IPermissionsService>();
        
        bool granted = await permService.CheckAsync(app.AppId, resource, action);
        if (!granted)
        {
            throw new Java.Lang.SecurityException($"Permission denied: UID {uid} requires {action} on {resource}");
        }
    }

    // --- IGameMasterBinder Implementation ---

    protected override bool OnTransact(int code, Parcel data, Parcel reply, int flags)
    {
        if (code == 1)
        {
            data.EnforceInterface("com.gamemaster.IGameMaster");
            string method = data.ReadString() ?? "";
            string json = data.ReadString() ?? "";
            
            try 
            {
                string responseJson = HandleMethodCallAsync(method, json).GetAwaiter().GetResult();
                reply.WriteNoException();
                reply.WriteString(responseJson);
            }
            catch (Exception ex)
            {
                reply.WriteException(new Java.Lang.RuntimeException(ex.Message));
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
            case "GetMyPermissionsAsync":
                result = await GetMyPermissions();
                break;
            case "ListAppsAsync":
                {
                    int uid = CallingUid;
                    await EnforcePermissionAsync(uid, CoreResource.ExpPoints, PermissionAction.Manage);
                    var appRegistry = _serviceProvider.GetRequiredService<IAppRegistryService>();
                    result = await appRegistry.ListAppsAsync();
                }
                break;
            case "RevokeAppAsync":
                {
                    int uid = CallingUid;
                    await EnforcePermissionAsync(uid, CoreResource.ExpPoints, PermissionAction.Manage);
                    var appRegistry = _serviceProvider.GetRequiredService<IAppRegistryService>();
                    var appId = System.Text.Json.JsonSerializer.Deserialize<Guid>(json, options);
                    await appRegistry.DeregisterAppAsync(appId);
                }
                break;
            case "GrantPermissionAsync":
                {
                    int uid = CallingUid;
                    await EnforcePermissionAsync(uid, CoreResource.ExpPoints, PermissionAction.Manage);
                    var permService = _serviceProvider.GetRequiredService<IPermissionsService>();
                    var doc = System.Text.Json.JsonDocument.Parse(json);
                    var appId = doc.RootElement.GetProperty("appId").GetGuid();
                    var resource = Enum.Parse<CoreResource>(doc.RootElement.GetProperty("resource").GetString()!, true);
                    var action = Enum.Parse<PermissionAction>(doc.RootElement.GetProperty("action").GetString()!, true);
                    await permService.GrantAsync(appId, resource, action);
                }
                break;
            case "RevokePermissionAsync":
                {
                    int uid = CallingUid;
                    await EnforcePermissionAsync(uid, CoreResource.ExpPoints, PermissionAction.Manage);
                    var permService = _serviceProvider.GetRequiredService<IPermissionsService>();
                    var doc = System.Text.Json.JsonDocument.Parse(json);
                    var appId = doc.RootElement.GetProperty("appId").GetGuid();
                    var resource = Enum.Parse<CoreResource>(doc.RootElement.GetProperty("resource").GetString()!, true);
                    var action = Enum.Parse<PermissionAction>(doc.RootElement.GetProperty("action").GetString()!, true);
                    await permService.RevokeAsync(appId, resource, action);
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

    public async Task<Player?> GetPlayer()
    {
        int uid = CallingUid;
        await EnforcePermissionAsync(uid, CoreResource.ExpPoints, PermissionAction.Read); // Using ExpPoints Read as proxy for basic read
        var playerService = _serviceProvider.GetRequiredService<IPlayerService>();
        return await playerService.GetPlayerAsync();
    }

    public async Task<Dictionary<CoreResource, int>> GetPoints()
    {
        int uid = CallingUid;
        await EnforcePermissionAsync(uid, CoreResource.ExpPoints, PermissionAction.Read);
        
        var pointsService = _serviceProvider.GetRequiredService<IPointsService>();
        var dict = new Dictionary<CoreResource, int>
        {
            [CoreResource.ExpPoints] = (await pointsService.GetBalanceAsync(CoreResource.ExpPoints.ToString().ToLowerInvariant()))?.Balance ?? 0,
            [CoreResource.Coins] = (await pointsService.GetBalanceAsync(CoreResource.Coins.ToString().ToLowerInvariant()))?.Balance ?? 0
        };
        return dict;
    }

    public async Task AwardPoints(CoreResource resource, int amount)
    {
        int uid = CallingUid;
        await EnforcePermissionAsync(uid, resource, PermissionAction.Award);
        
        var pointsService = _serviceProvider.GetRequiredService<IPointsService>();
        var app = await GetOrRegisterCallerAppAsync(uid);
        
        await pointsService.AwardPointsAsync(resource.ToString().ToLowerInvariant(), amount);
    }

    public async Task SpendPoints(CoreResource resource, int amount)
    {
        int uid = CallingUid;
        await EnforcePermissionAsync(uid, resource, PermissionAction.Spend);
        
        var pointsService = _serviceProvider.GetRequiredService<IPointsService>();
        var app = await GetOrRegisterCallerAppAsync(uid);
        
        await pointsService.SpendPointsAsync(resource.ToString().ToLowerInvariant(), amount);
    }

    public async Task<IEnumerable<InventoryItem>> GetInventory()
    {
        int uid = CallingUid;
        await EnforcePermissionAsync(uid, CoreResource.Inventory, PermissionAction.Read);
        
        var invService = _serviceProvider.GetRequiredService<IInventoryService>();
        return await invService.GetItemsAsync();
    }

    public async Task AddInventoryItem(string name, int qty, string? metadata)
    {
        int uid = CallingUid;
        await EnforcePermissionAsync(uid, CoreResource.Inventory, PermissionAction.Award);
        
        var invService = _serviceProvider.GetRequiredService<IInventoryService>();
        var app = await GetOrRegisterCallerAppAsync(uid);
        
        await invService.AddItemAsync(name, qty, metadata ?? "", app.AppId.ToString());
    }

    public async Task RemoveInventoryItem(Guid itemId, int qty)
    {
        int uid = CallingUid;
        await EnforcePermissionAsync(uid, CoreResource.Inventory, PermissionAction.Spend);
        
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

    public async Task<IEnumerable<AppPermission>> GetMyPermissions()
    {
        int uid = CallingUid;
        var app = await GetOrRegisterCallerAppAsync(uid);
        
        var permService = _serviceProvider.GetRequiredService<IPermissionsService>();
        return await permService.GetPermissionsAsync(app.AppId);
    }
}
