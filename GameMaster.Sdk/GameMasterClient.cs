using System;
using System.Text.Json;
using System.Threading.Tasks;
using GameMaster.Sdk.Models;
using GameMaster.Sdk.Services;

#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;
#endif

namespace GameMaster.Sdk;

/// <summary>
/// The main entry point for 3rd-party applications to interact with the Game Master core.
/// </summary>
public class GameMasterClient : IPlayerEconomyService
{
#if ANDROID
    private readonly Context _context;
    private readonly BinderConnection _connection;
    private IBinder? _binder;
    private TaskCompletionSource<IBinder>? _tcs;

    private class BinderConnection : Java.Lang.Object, IServiceConnection
    {
        private readonly GameMasterClient _parent;

        public BinderConnection(GameMasterClient parent)
        {
            _parent = parent;
        }

        public void OnServiceConnected(ComponentName? name, IBinder? service)
        {
            _parent._binder = service;
            _parent._tcs?.TrySetResult(service!);
        }

        public void OnServiceDisconnected(ComponentName? name)
        {
            _parent._binder = null;
        }
    }
#endif

    public GameMasterClient()
    {
#if ANDROID
        _context = Application.Context;
        _connection = new BinderConnection(this);
#endif
    }

#if ANDROID
    private Task<IBinder> GetBinderAsync()
    {
        if (_binder != null) return Task.FromResult(_binder);
        
        if (_tcs == null || _tcs.Task.IsCompleted)
        {
            _tcs = new TaskCompletionSource<IBinder>();
            var intent = new Intent("com.gamemaster.BIND_SERVICE");
            intent.SetPackage("com.gamemaster.app");
            bool bound = _context.BindService(intent, _connection, Bind.AutoCreate);
            if (!bound)
            {
                _tcs.SetException(new Exception("Could not bind to com.gamemaster.app"));
            }
        }
        return _tcs.Task;
    }

    private async Task<string> CallMethodAsync(string method, string jsonArgs = "")
    {
        var binder = await GetBinderAsync();
        var data = Parcel.Obtain();
        var reply = Parcel.Obtain();
        try
        {
            data!.WriteInterfaceToken("com.gamemaster.IGameMaster");
            data.WriteString(method);
            data.WriteString(jsonArgs);
            binder.Transact(1, data, reply, 0);
            reply!.ReadException();
            return reply.ReadString() ?? "";
        }
        finally
        {
            data?.Recycle();
            reply?.Recycle();
        }
    }
#endif

    public async Task<PlayerProfile> GetPlayerProfileAsync()
    {
#if ANDROID
        try
        {
            var playerJson = await CallMethodAsync("GetPlayerAsync");
            var pointsJson = await CallMethodAsync("GetPointsAsync");
            
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var profile = new PlayerProfile();
            
            if (!string.IsNullOrEmpty(playerJson))
            {
                using var playerDoc = JsonDocument.Parse(playerJson);
                if (playerDoc.RootElement.ValueKind != JsonValueKind.Null)
                {
                    profile.Id = playerDoc.RootElement.GetProperty("id").GetString() ?? "";
                    profile.DisplayName = playerDoc.RootElement.GetProperty("displayName").GetString() ?? "";
                }
            }
            
            if (!string.IsNullOrEmpty(pointsJson))
            {
                using var pointsDoc = JsonDocument.Parse(pointsJson);
                foreach (var p in pointsDoc.RootElement.EnumerateArray())
                {
                    var cid = p.GetProperty("currencyId").GetString();
                    var bal = p.GetProperty("balance").GetInt32();
                    if (cid != null && cid.Equals("Coins", StringComparison.OrdinalIgnoreCase)) profile.Coins = bal;
                    if (cid != null && cid.Equals("ExpPoints", StringComparison.OrdinalIgnoreCase)) profile.Points = bal;
                }
            }
            return profile;
        }
        catch (Exception ex)
        {
            return new PlayerProfile
            {
                Id = Guid.NewGuid().ToString(),
                DisplayName = "Error: " + ex.Message,
                Points = 0,
                Coins = 0
            };
        }
#else
        // Placeholder for actual Binder/API communication
        return await Task.FromResult(new PlayerProfile
        {
            Id = Guid.NewGuid().ToString(),
            DisplayName = "Current Player (Mock)",
            Points = 0,
            Coins = 0
        });
#endif
    }

    public async Task<CoinTransactionResult> AdjustCoinsAsync(int amount, string reason = "")
    {
#if ANDROID
        var method = amount >= 0 ? "AwardPointsAsync" : "SpendPointsAsync";
        var args = new { Resource = "coins", Amount = Math.Abs(amount) };
        var jsonArgs = JsonSerializer.Serialize(args);
        
        try
        {
            var pointsJson = await CallMethodAsync(method, jsonArgs);
            int newBalance = 0;
            
            if (!string.IsNullOrEmpty(pointsJson))
            {
                using var pointsDoc = JsonDocument.Parse(pointsJson);
                foreach (var p in pointsDoc.RootElement.EnumerateArray())
                {
                    if (p.GetProperty("currencyId").GetString()?.Equals("Coins", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        newBalance = p.GetProperty("balance").GetInt32();
                    }
                }
            }
            
            return new CoinTransactionResult
            {
                Success = true,
                NewBalance = newBalance,
                Message = "Transaction successful"
            };
        }
        catch (Exception ex)
        {
            return new CoinTransactionResult
            {
                Success = false,
                NewBalance = 0,
                Message = ex.Message
            };
        }
#else
        // Placeholder for actual Binder/API communication
        return await Task.FromResult(new CoinTransactionResult
        {
            Success = true,
            NewBalance = amount,
            Message = "Transaction successful (mock)"
        });
#endif
    }
}
