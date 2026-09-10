using System;
using System.Threading.Tasks;
using Android.Content;
using Android.OS;

namespace GameMaster.Android.Client;

/// <summary>
/// A helper library for third-party Android apps to easily bind to the Game Master Service.
/// </summary>
public class GameMasterClientLibrary : Java.Lang.Object, IServiceConnection
{
    private readonly Context _context;
    private IGameMasterBinder? _binder;
    private TaskCompletionSource<IGameMasterBinder>? _tcs;

    public GameMasterClientLibrary(Context context)
    {
        _context = context;
    }

    public Task<IGameMasterBinder> ConnectAsync()
    {
        if (_binder != null)
        {
            return Task.FromResult(_binder);
        }

        _tcs = new TaskCompletionSource<IGameMasterBinder>();

        var intent = new Intent("com.gamemaster.BIND_SERVICE");
        intent.SetPackage("com.gamemaster.android");

        bool bound = _context.BindService(intent, this, Bind.AutoCreate);
        if (!bound)
        {
            _tcs.SetException(new Exception("Failed to bind to Game Master Service. Is the app installed?"));
        }

        return _tcs.Task;
    }

    public void Disconnect()
    {
        if (_binder != null)
        {
            _context.UnbindService(this);
            _binder = null;
        }
    }

    public void OnServiceConnected(ComponentName? name, IBinder? service)
    {
        if (service is IGameMasterBinder gmBinder)
        {
            _binder = gmBinder;
            _tcs?.TrySetResult(gmBinder);
        }
        else
        {
            _tcs?.TrySetException(new Exception("Bound service is not IGameMasterBinder."));
        }
    }

    public void OnServiceDisconnected(ComponentName? name)
    {
        _binder = null;
    }
}
