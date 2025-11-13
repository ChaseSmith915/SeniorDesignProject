using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;
using System.Threading;
using Android.Util; // For Log
using System.Linq; // Required for OrderByDescending and Any()

// FIX: Added this using statement to resolve UsageStatsManager and UsageStatsInterval
using Android.App.Usage;
using Android.Content.PM; // ADDED: Required for ForegroundServiceType on Android 14 (API 34)

// FIXED: Namespace updated to match your project's root namespace
namespace MauiApp1_testing_android_fesability.Platforms.Android
{
    [Service(ForegroundServiceType = ForegroundService.TypeDataSync)]
    public class UsageTrackingService : Service
    {
        private Timer _timer;
        private string _lastForegroundApp = string.Empty;
        private const string _youtubePackageName = "com.google.android.youtube";

        private const string NOTIFICATION_CHANNEL_ID = "UsageTrackingServiceChannel";
        private const int NOTIFICATION_ID = 1001;
        private const string TAG = "UsageTrackingService";

        public override IBinder OnBind(Intent intent)
        {
            return null; // We are not using a bound service
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            Log.Debug(TAG, "Usage Tracking Service started.");

            // 1. Create Notification Channel (Required for Android 8.0+)
            CreateNotificationChannel();

            // 2. Create the persistent notification
            var notification = new NotificationCompat.Builder(this, NOTIFICATION_CHANNEL_ID)
                .SetContentTitle("Time Management Active")
                .SetContentText("Monitoring app usage...")
                .SetSmallIcon(Resource.Mipmap.appicon) // Use your app's icon
                .SetOngoing(true)
                .Build();

            // 3. Start the service in the foreground
            // FIX: On API 34 (Target SDK 34), StartForeground MUST include the ForegroundServiceType.
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q) // Q is API 29, when this overload was introduced
            {
                StartForeground(NOTIFICATION_ID, notification, ForegroundService.TypeDataSync);
            }
            else
            {
                // Fallback for older Android versions
                StartForeground(NOTIFICATION_ID, notification);
            }


            // 4. Start the polling timer
            // We check every 3 seconds. Adjust as needed for battery vs. responsiveness.
            _timer = new Timer(CheckForegroundApp, null, 0, (int)TimeSpan.FromSeconds(3).TotalMilliseconds);
            Log.Debug(TAG, "Timer started, checking every 3000ms."); // NEW LOG

            // 5. Return "Sticky" to ensure the service restarts if killed
            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            Log.Debug(TAG, "Usage Tracking Service stopped.");
            _timer?.Dispose();
            _timer = null;
            base.OnDestroy();
        }

        private void CheckForegroundApp(object state)
        {
            try
            {
                // NEW LOG: Confirming timer execution
                Log.Debug(TAG, "TIMER TICK: Executing CheckForegroundApp.");

                var usageStatsManager = (UsageStatsManager)GetSystemService(Context.UsageStatsService);
                if (usageStatsManager == null) return;

                long time = Java.Lang.JavaSystem.CurrentTimeMillis();

                // Query for events in the last 10 seconds
                var stats = usageStatsManager.QueryUsageStats(UsageStatsInterval.Daily, time - (10 * 1000), time);

                // FIX: Changed stats.Count > 0 to stats.Any() to resolve the "method group" error
                if (stats != null && stats.Any())
                {
                    Log.Debug(TAG, $"Stats found: {stats.Count} entries."); // NEW LOG: Count stats

                    // Sort by last time used to find the most recent
                    var sortedStats = stats.OrderByDescending(s => s.LastTimeUsed);
                    string currentForegroundApp = sortedStats.First()?.PackageName;

                    Log.Debug(TAG, $"FOREGROUND APP DETECTED: {currentForegroundApp}"); // NEW LOG: Log the detected app

                    if (string.IsNullOrEmpty(currentForegroundApp)) return;

                    // Check if a *new* app has come to the foreground
                    if (currentForegroundApp != _lastForegroundApp)
                    {
                        Log.Debug(TAG, $"App changed: {currentForegroundApp}. Previous was: {_lastForegroundApp}"); // Enhanced Log

                        // TODO: Make this package name check modular
                        if (currentForegroundApp == _youtubePackageName)
                        {
                            Log.Debug(TAG, "YOUTUBE MATCH FOUND! Displaying popup."); // NEW LOG: Match confirmed
                            // YouTube was opened! Show the popup.
                            ShowPopup();
                        }

                        // Update the last known app
                        _lastForegroundApp = currentForegroundApp;
                    }
                }
                else
                {
                    Log.Debug(TAG, "No usage stats found in the last 10 seconds."); // NEW LOG: Handle empty stats
                }
            }
            catch (Exception ex)
            {
                Log.Error(TAG, $"Error in CheckForegroundApp: {ex.Message}");
            }
        }

        private void ShowPopup()
        {
            // We must start an Activity from a service context, so we add NEW_TASK flag
            Intent popupIntent = new Intent(this, typeof(DialogActivity));
            popupIntent.AddFlags(ActivityFlags.NewTask);
            StartActivity(popupIntent);
        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                // Notification channels are not required before Android 8.0
                return;
            }

            var channelName = "Usage Tracking Service";
            var channelDescription = "Notification for the app usage monitoring service";
            var channel = new NotificationChannel(NOTIFICATION_CHANNEL_ID, channelName, NotificationImportance.Default)
            {
                Description = channelDescription
            };

            var notificationManager = (NotificationManager)GetSystemService(NotificationService);
            notificationManager.CreateNotificationChannel(channel);
        }
    }
}