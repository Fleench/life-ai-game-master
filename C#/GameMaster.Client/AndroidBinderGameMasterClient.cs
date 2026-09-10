#if ANDROID
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.OS;
using GameMaster.Client.Models;
using System.Text.Json.Serialization;

namespace GameMaster.Client;

public class AndroidBinderGameMasterClient : Java.Lang.Object, IGameMasterClient, IServiceConnection
{
    private readonly Context _context;
    private IBinder? _binder;
    private bool _isBound;
    private TaskCompletionSource<bool> _boundTcs = new();
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _connectLock = new SemaphoreSlim(1, 1);

    public AndroidBinderGameMasterClient(Context context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
    }

    public async Task ConnectAsync()
    {
        if (_isBound) return;

        await _connectLock.WaitAsync();
        try
        {
            if (_isBound) return;
            
            // Note: If multiple calls wait on _connectLock, the first one resolves _boundTcs. 
            // We should ensure we don't recreate it if it's already pending, or we just rely on _isBound.
            // Wait, if _isBound is true, we return early above. But what if it's currently binding?
            // If it's binding, the first caller creates _boundTcs and calls BindService, then awaits _boundTcs.Task.
            // The second caller will wait on _connectLock. This means the second caller won't proceed until 
            // the first caller finishes. But wait, if we await _boundTcs.Task INSIDE the lock, the lock won't be released until binding completes.
            // Is that what we want? Yes, we want to prevent concurrent bindings.
            
            _boundTcs = new TaskCompletionSource<bool>();

            var intent = new Intent("com.gamemaster.BIND_SERVICE");
            intent.SetPackage("com.gamemaster.app");
            
            bool bound = false;
            try
            {
                bound = _context.BindService(intent, this, Bind.AutoCreate);
            }
            catch (Java.Lang.SecurityException ex)
            {
                throw new Exception($"SecurityException during BindService: {ex.Message}", ex);
            }

            if (!bound)
            {
                throw new Exception("Failed to bind to GameMaster service. Ensure the app is installed and com.gamemaster.BIND permission is granted.");
            }

            await _boundTcs.Task;
        }
        finally
        {
            _connectLock.Release();
        }
    }

    public void OnServiceConnected(ComponentName? name, IBinder? service)
    {
        _binder = service;
        _isBound = true;
        _boundTcs.TrySetResult(true);
    }

    public void OnServiceDisconnected(ComponentName? name)
    {
        _binder = null;
        _isBound = false;
    }

    private async Task<T?> TransactAsync<T>(string method, object? request = null)
    {
        await ConnectAsync();
        if (_binder == null) throw new Exception("Not connected to GameMaster Service.");

        using var data = Parcel.Obtain();
        using var reply = Parcel.Obtain();
        try
        {
            data.WriteInterfaceToken("com.gamemaster.IGameMaster");
            data.WriteString(method);
            if (request != null)
            {
                data.WriteString(JsonSerializer.Serialize(request, _jsonOptions));
            }
            else
            {
                data.WriteString("");
            }

            bool status = _binder.Transact(1, data, reply, 0);
            if (!status) throw new Exception("Transact failed");

            reply.ReadException();
            string? responseJson = reply.ReadString();
            
            if (string.IsNullOrEmpty(responseJson)) return default;
            return JsonSerializer.Deserialize<T>(responseJson, _jsonOptions);
        }
        finally
        {
            data.Recycle();
            reply.Recycle();
        }
    }
    
    private async Task TransactVoidAsync(string method, object? request = null)
    {
        await ConnectAsync();
        if (_binder == null) throw new Exception("Not connected to GameMaster Service.");

        using var data = Parcel.Obtain();
        using var reply = Parcel.Obtain();
        try
        {
            data.WriteInterfaceToken("com.gamemaster.IGameMaster");
            data.WriteString(method);
            if (request != null)
            {
                data.WriteString(JsonSerializer.Serialize(request, _jsonOptions));
            }
            else
            {
                data.WriteString("");
            }

            bool status = _binder.Transact(1, data, reply, 0);
            if (!status) throw new Exception("Transact failed");

            reply.ReadException();
        }
        finally
        {
            data.Recycle();
            reply.Recycle();
        }
    }

    public async Task<RegisterResponse> RegisterAppAsync(RegisterAppRequest request, CancellationToken cancellationToken = default)
        => await TransactAsync<RegisterResponse>("RegisterAppAsync", request) ?? new RegisterResponse();

    public async Task<Player> GetPlayerAsync(CancellationToken cancellationToken = default)
        => await TransactAsync<Player>("GetPlayerAsync") ?? new Player();

    public async Task<IEnumerable<CurrencyPool>> GetPointsAsync(CancellationToken cancellationToken = default)
        => await TransactAsync<IEnumerable<CurrencyPool>>("GetPointsAsync") ?? Array.Empty<CurrencyPool>();

    public async Task<IEnumerable<CurrencyPool>> AwardPointsAsync(PointsRequest request, CancellationToken cancellationToken = default)
        => await TransactAsync<IEnumerable<CurrencyPool>>("AwardPointsAsync", request) ?? Array.Empty<CurrencyPool>();

    public async Task<IEnumerable<CurrencyPool>> SpendPointsAsync(PointsRequest request, CancellationToken cancellationToken = default)
        => await TransactAsync<IEnumerable<CurrencyPool>>("SpendPointsAsync", request) ?? Array.Empty<CurrencyPool>();

    public async Task<IEnumerable<InventoryItem>> GetInventoryAsync(CancellationToken cancellationToken = default)
        => await TransactAsync<IEnumerable<InventoryItem>>("GetInventoryAsync") ?? Array.Empty<InventoryItem>();

    public Task AddInventoryItemAsync(AddInventoryItemRequest request, CancellationToken cancellationToken = default)
        => TransactVoidAsync("AddInventoryItemAsync", request);

    public Task RemoveInventoryItemAsync(RemoveInventoryItemRequest request, CancellationToken cancellationToken = default)
        => TransactVoidAsync("RemoveInventoryItemAsync", request);

    public async Task<IEnumerable<AppPermission>> GetMyPermissionsAsync(CancellationToken cancellationToken = default)
        => await TransactAsync<IEnumerable<AppPermission>>("GetMyPermissionsAsync") ?? Array.Empty<AppPermission>();
        
    public async Task<IEnumerable<ConnectedApp>> ListAppsAsync(CancellationToken cancellationToken = default)
        => await TransactAsync<IEnumerable<ConnectedApp>>("ListAppsAsync") ?? Array.Empty<ConnectedApp>();
        
    public Task RevokeAppAsync(Guid appId, CancellationToken cancellationToken = default)
        => TransactVoidAsync("RevokeAppAsync", appId);
        
    public Task GrantPermissionAsync(Guid appId, GameMaster.Client.Models.Resource resource, GameMaster.Client.Models.PermissionAction action, CancellationToken cancellationToken = default)
        => TransactVoidAsync("GrantPermissionAsync", new { AppId = appId, Resource = resource, Action = action });
        
    public Task RevokePermissionAsync(Guid appId, GameMaster.Client.Models.Resource resource, GameMaster.Client.Models.PermissionAction action, CancellationToken cancellationToken = default)
        => TransactVoidAsync("RevokePermissionAsync", new { AppId = appId, Resource = resource, Action = action });

    public Task RequestPermissionAsync(GameMaster.Client.Models.Resource resource, GameMaster.Client.Models.PermissionAction action, CancellationToken cancellationToken = default)
        => TransactVoidAsync("RequestPermissionAsync", new { Resource = resource, Action = action });
}
#endif
