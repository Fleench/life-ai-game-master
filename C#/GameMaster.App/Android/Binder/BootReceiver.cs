using Android.App;
using Android.Content;
using Android.OS;

namespace GameMaster.Android;

[BroadcastReceiver(Enabled = true, Exported = true, Name = "com.gamemaster.android.BootReceiver")]
[IntentFilter(new[] { Intent.ActionBootCompleted })]
public class BootReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null) return;
        if (intent?.Action == Intent.ActionBootCompleted)
        {
            var serviceIntent = new Intent(context, typeof(GameMasterBinderService));
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                context?.StartForegroundService(serviceIntent);
            }
            else
            {
                context?.StartService(serviceIntent);
            }
        }
    }
}
