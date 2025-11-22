using Microsoft.Maui.Controls;
using Android.Content.PM;
using System.Security.Cryptography.X509Certificates;
using AndroidX.AppCompat.Widget;
using Android.Telephony;
using System.Collections;





#if ANDROID
using Android.Content;
using Android.Util; // For Log
#endif

namespace MauiApp1_testing_android_fesability
{
    public partial class MainPage : ContentPage
    {
#if ANDROID
        const string PREF_KEY_ENABLED = "intercept_enabled";
#endif
        public MainPage()
        {
            InitializeComponent();
            addTargetedApp("App Intercept");

#if ANDROID
            // These using directives are only valid for Android


            //var prefs = Android.App.Application.Context.GetSharedPreferences("APP_PREFS", FileCreationMode.Private);
            //bool enabled = prefs.GetBoolean(PREF_KEY_ENABLED, false);
            //AppTarget1InterceptSwitch.IsToggled = enabled;

            //AppTarget1InterceptSwitch.Toggled += (s, e) =>
            //{
            //    var editor = prefs.Edit();
            //    editor.PutBoolean(PREF_KEY_ENABLED, e.Value);
            //    editor.Apply();

            //    if (e.Value)
            //    {
            //        DisplayAlert("Enable Service",
            //            "Make sure the Accessibility service for this app is enabled in Settings → Accessibility.",
            //            "OK");
            //    }
            //};

            OpenAccessibilitySettingsButton.Clicked += (s, e) =>
            {
                //Intent intent = new Intent(Android.Provider.Settings.ActionAccessibilitySettings);
                //intent.SetFlags(ActivityFlags.NewTask);
                //Android.App.Application.Context.StartActivity(intent);
                Navigation.PushAsync(new PermissionsSetUp());
            };

            AddAppButton.Clicked += (s, e) =>
            {
                Navigation.PushAsync(new TargetApps(this));
            };
#endif
        }

        public void addTargetedApp(String appName)
        {
            Grid newTargetLine = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },// label column
                    new ColumnDefinition { Width = GridLength.Auto }//  switch column
                },
                Padding = new Thickness(0, 5)
            };

            Label newTargetLabel = new Label
            {
                FontSize = 16,
                Text = $"{appName}:",
                VerticalTextAlignment = TextAlignment.Center
            };

            Switch newTargetSwitch = new Switch
            {
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Center
            };

            newTargetLine.Add(newTargetLabel, 0, 0);
            newTargetLine.Add(newTargetSwitch, 1, 0);

            TargetAppsList.Children.Insert(TargetAppsList.Children.Count - 1, newTargetLine);
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
