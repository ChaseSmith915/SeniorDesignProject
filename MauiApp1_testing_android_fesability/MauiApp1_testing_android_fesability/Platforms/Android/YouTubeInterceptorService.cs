using Android.AccessibilityServices;
using Android.Views.Accessibility;
using Android.Content;
using Android.Widget;
using Android.App;
using Android.OS;
using Android.Content.PM;
using Android.Provider;
using AndroidX.Core.Content;
using Microsoft.Maui.Controls; // for Preferences if you use it
using Android.Preferences;

namespace MauiApp1_testing_android_fesability
{
    [Service(Name = "com.companyname.mauiapp1_testing_android_fesability.YouTubeInterceptorService",
             Permission = "android.permission.BIND_ACCESSIBILITY_SERVICE")]
    [IntentFilter(new[] { "android.accessibilityservice.AccessibilityService" })]
    public class YouTubeInterceptorService : AccessibilityService
    {
        const string YOUTUBE_PACKAGE = "com.google.android.youtube";
        const string PREF_KEY_ENABLED = "intercept_enabled";

        public override void OnAccessibilityEvent(AccessibilityEvent e)
        {
            try
            {
                // Only act on window state changed events
                if (e.EventType != EventTypes.WindowStateChanged && e.EventType != EventTypes.WindowContentChanged)
                    return;

                if (e.PackageName == null)
                    return;

                var packageName = e.PackageName.ToString();

                // Ignore our own app to avoid loops
                if (packageName == Android.App.Application.Context.PackageName)
                    return;

                // Check preference whether interception is on
                var prefs = Android.App.Application.Context.GetSharedPreferences("APP_PREFS", FileCreationMode.Private);
                bool interceptEnabled = prefs.GetBoolean(PREF_KEY_ENABLED, false);
                if (!interceptEnabled) return;

                if (packageName.Equals(YOUTUBE_PACKAGE, System.StringComparison.OrdinalIgnoreCase))
                {
                    // Start the dialog activity to prompt the user
                    var intent = new Intent(this, typeof(DialogActivity));
                    intent.AddFlags(ActivityFlags.NewTask);
                    intent.PutExtra("targetPackage", packageName);
                    // Optionally pass other info (app label)
                    StartActivity(intent);
                }
            }
            catch (System.Exception ex)
            {
                Android.Util.Log.Error("YouTubeInterceptor", ex.ToString());
            }
        }

        public override void OnInterrupt()
        {
            // required override
        }

        protected override void OnServiceConnected()
        {
            base.OnServiceConnected();
            // You can adjust service info here if needed
            Android.Util.Log.Info("YouTubeInterceptor", "Accessibility service connected");
        }
    }
}
