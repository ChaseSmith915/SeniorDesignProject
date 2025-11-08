
using Microsoft.Maui.Controls;
using Microsoft.Maui.Hosting;

#if ANDROID
using Android.Content;
using Android.Util; // For Log
#endif


namespace MauiApp1_testing_android_fesability
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
        }
        private void OnCounterClicked(object sender, EventArgs e)
        {
            count++;

            if (count == 1)
                CounterBtn.Text = $"Clicked {count} time";
            else
                CounterBtn.Text = $"Clicked {count} times";

            SemanticScreenReader.Announce(CounterBtn.Text);
        }

        private async void OnCheckPermissionsClicked(object sender, EventArgs e)
        {
            bool permissionsGranted = await CheckAndRequestPermissions();
            if (permissionsGranted)
            {
                await DisplayAlert("Success", "All required permissions are granted.", "OK");
            }
            else
            {
                await DisplayAlert("Permissions Needed", "Some permissions were not granted. The service may not work correctly.", "OK");
            }
        }

        private void OnStartServiceClicked(object sender, EventArgs e)
        {
#if ANDROID
            Log.Debug("MainPage", "Start Service button clicked.");
            var context = Android.App.Application.Context;
            // FIXED: Using relative path "Platforms.Android.UsageTrackingService"
            var serviceIntent = new Android.Content.Intent(context, typeof(Platforms.Android.UsageTrackingService));
            context.StartForegroundService(serviceIntent);
#endif
        }

        private void OnStopServiceClicked(object sender, EventArgs e)
        {
#if ANDROID
            Log.Debug("MainPage", "Stop Service button clicked.");
            var context = Android.App.Application.Context;
            // FIXED: Using relative path "Platforms.Android.UsageTrackingService"
            var serviceIntent = new Android.Content.Intent(context, typeof(Platforms.Android.UsageTrackingService));
            context.StopService(serviceIntent);
#endif
        }


        // --- PERMISSION CHECKING LOGIC ---

        private async Task<bool> CheckAndRequestPermissions()
        {
            bool allGranted = true;

#if ANDROID
            var context = Android.App.Application.Context;

            // 1. Check/Request Notification Permission (Android 13+)
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Tiramisu)
            {
                var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.PostNotifications>();
                }
                if (status != PermissionStatus.Granted) allGranted = false;
            }

            // 2. Check/Request "Display over other apps" (SYSTEM_ALERT_WINDOW)
            if (!Android.Provider.Settings.CanDrawOverlays(context))
            {
                allGranted = false;
                await DisplayAlert("Permission Needed", "Please grant 'Display over other apps' permission.", "Go to Settings");
                var intent = new Android.Content.Intent(Android.Provider.Settings.ActionManageOverlayPermission,
                                                        Android.Net.Uri.Parse("package:" + context.PackageName));
                intent.AddFlags(Android.Content.ActivityFlags.NewTask);
                context.StartActivity(intent);
            }

            // 3. Check/Request "Usage Access" (PACKAGE_USAGE_STATS)
            var appOps = (Android.App.AppOpsManager)context.GetSystemService(Android.Content.Context.AppOpsService);
            var mode = appOps.CheckOpNoThrow(Android.App.AppOpsManager.OpstrGetUsageStats, Android.OS.Process.MyUid(), context.PackageName);

            if (mode != Android.App.AppOpsManagerMode.Allowed)
            {
                allGranted = false;
                await DisplayAlert("Permission Needed", "Please grant 'Usage data access' permission.", "Go to Settings");
                var intent = new Android.Content.Intent(Android.Provider.Settings.ActionUsageAccessSettings);
                intent.AddFlags(Android.Content.ActivityFlags.NewTask);
                context.StartActivity(intent);
            }
#endif

            // Await a short delay just to give the user time to potentially see the alerts
            await Task.Delay(100);
            return allGranted;
        }
    }

}