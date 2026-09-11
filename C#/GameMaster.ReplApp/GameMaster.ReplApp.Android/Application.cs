using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;

namespace GameMaster.ReplApp.Android
{
    [Application]
    public class Application : global::Android.App.Application
    {
        public Application(System.IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }
    }
}
