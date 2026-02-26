using Android.Content;
using Android.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace HourGuard
{
    public partial class PermissionsSetUp : ContentPage
    {
        public PermissionsSetUp()
        {
            InitializeComponent();
            if (!Preferences.ContainsKey("SelectedDifficulty"))
            {
                Preferences.Set("SelectedDifficulty", "easy");
            }

            // Set the corresponding RadioButton as checked
            foreach (var child in AppStack.Children.OfType<VerticalStackLayout>().Last().Children)
            {
                string savedDifficulty = Preferences.Get("SelectedDifficulty", null);
                if (child is RadioButton rb && rb.Value.ToString() == savedDifficulty)
                {
                    rb.IsChecked = true;
                }
            }
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
            Log.Debug("PermissionsSetUp", "Start Service button clicked.");
            var context = Android.App.Application.Context;
            var serviceIntent = new Intent(context, typeof(Platforms.Android.UsageTrackingService));
            context.StartForegroundService(serviceIntent);

            // Persist that the service should run after reboot
            var prefs = context.GetSharedPreferences("HourGuardPrefs", FileCreationMode.Private);
            prefs.Edit().PutBoolean("ServiceEnabled", true).Apply();
#endif
        }

        private void OnStopServiceClicked(object sender, EventArgs e)
        {
#if ANDROID
            Log.Debug("PermissionsSetUp", "Stop Service button clicked.");
            var context = Android.App.Application.Context;
            var serviceIntent = new Intent(context, typeof(Platforms.Android.UsageTrackingService));
            context.StopService(serviceIntent);

            // Persist that the service should NOT run after reboot
            var prefs = context.GetSharedPreferences("HourGuardPrefs", FileCreationMode.Private);
            prefs.Edit().PutBoolean("ServiceEnabled", false).Apply();
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

        private string selectedDifficulty; // default
        private void OnDifficultyChanged(object sender, CheckedChangedEventArgs e)
        {
            if (sender is RadioButton radioButton && radioButton.IsChecked)
            {
                selectedDifficulty = radioButton.Value.ToString();
                Preferences.Set("SelectedDifficulty", selectedDifficulty);
                Log.Debug("HourGuard", $"Difficulty set to: {selectedDifficulty}");
            }
        }
    }
}
