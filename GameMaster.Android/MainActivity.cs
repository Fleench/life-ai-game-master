namespace GameMaster.Android;

[Activity(Label = "@string/app_name", MainLauncher = true, WindowSoftInputMode = global::Android.Views.SoftInput.AdjustResize)]
public class MainActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Set our view from the "main" layout resource
        SetContentView(Resource.Layout.activity_main);
    }
}