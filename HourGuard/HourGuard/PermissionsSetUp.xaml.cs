using Android.Content;
using Android.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HourGuard
{
    public partial class PermissionsSetUp : ContentPage
    {
        public PermissionsSetUp()
        {
            InitializeComponent();
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
            var serviceIntent = new Intent(context, typeof(Platforms.Android.UsageTrackingService));
            context.StartForegroundService(serviceIntent);
#endif
        }

        private void OnStopServiceClicked(object sender, EventArgs e)
        {
#if ANDROID
            Log.Debug("MainPage", "Stop Service button clicked.");
            var context = Android.App.Application.Context;
            var serviceIntent = new Intent(context, typeof(Platforms.Android.UsageTrackingService));
            context.StopService(serviceIntent);
#endif
        }

        // --- PERMISSION CHECKING LOGIC ---
        private async Task<bool> CheckAndRequestPermissions()
        {
            bool allGranted = true;

#if ANDROID
            var context = Android.App.Application.Context;

            // 1. Notifications (Android 13+)
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Tiramisu)
            {
                var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.PostNotifications>();
                }
                if (status != PermissionStatus.Granted) allGranted = false;
            }

            // 2. Overlay permission
            if (!Android.Provider.Settings.CanDrawOverlays(context))
            {
                allGranted = false;
                await DisplayAlert("Permission Needed", "Please grant 'Display over other apps' permission.", "Go to Settings");
                var intent = new Intent(Android.Provider.Settings.ActionManageOverlayPermission,
                                        Android.Net.Uri.Parse("package:" + context.PackageName));
                intent.AddFlags(ActivityFlags.NewTask);
                context.StartActivity(intent);
            }

            // 3. Usage access
            var appOps = (Android.App.AppOpsManager)context.GetSystemService(Context.AppOpsService);
            var mode = appOps.CheckOpNoThrow(Android.App.AppOpsManager.OpstrGetUsageStats,
                                             Android.OS.Process.MyUid(), context.PackageName);

            if (mode != Android.App.AppOpsManagerMode.Allowed)
            {
                allGranted = false;
                await DisplayAlert("Permission Needed", "Please grant 'Usage data access' permission.", "Go to Settings");
                var intent = new Intent(Android.Provider.Settings.ActionUsageAccessSettings);
                intent.AddFlags(ActivityFlags.NewTask);
                context.StartActivity(intent);
            }
#endif

            await Task.Delay(100);
            return allGranted;
        }
    }
}
