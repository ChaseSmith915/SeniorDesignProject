using Android.App;
using Android.App.Usage;
using Android.Content;
using Android.Content.PM; // Required for ForegroundServiceType on Android 14 (API 34)
using Android.OS;
using Android.Runtime;
using Android.Util; // For Log
using AndroidX.Core.App;
using System.Linq; // Required for OrderByDescending and Any()
using System.Threading;
using HourGuard.Database;
using Microsoft;

namespace HourGuard.Platforms.Android
{
    [Service(ForegroundServiceType = ForegroundService.TypeDataSync)]
    public class UsageTrackingService : Service
    {
        private HourGuardDatabase db = App.Database;

        private Dictionary<string, HourGuardTimer> appTimers = new Dictionary<string, HourGuardTimer>();

        private Timer timer;
        private string lastForegroundApp = string.Empty;

        private const string NOTIFICATION_CHANNEL_ID = "UsageTrackingServiceChannel";
        private const int NOTIFICATION_ID = 1001;
        private const string TAG = "HourGuardService";
        private const int TICK_INTERVAL_SEC = 5;

        public override IBinder OnBind(Intent intent)
        {
            return null; // We are not using a bound service
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            Log.Debug(TAG, "Usage Tracking Service started.");

            // 0. Initialize appTimers from database settings
            InitializeAppTimers();

            // 1. Create Notification Channel (Required for Android 8.0+)
            CreateNotificationChannel();

            // 2. Create the persistent notification
            var notification = new NotificationCompat.Builder(this, NOTIFICATION_CHANNEL_ID)
                .SetContentTitle("Time Management Active")
                .SetContentText("Monitoring app usage...")
                .SetSmallIcon(Microsoft.Maui.Controls.Resource.Mipmap.appicon) // Use your app's icon
                .SetOngoing(true)
                .Build();

            // 3. Start the service in the foreground
            // On API 34 (Target SDK 34), StartForeground MUST include the ForegroundServiceType.
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
            timer = new Timer(CheckForegroundApp, null, 0, (int)TimeSpan.FromSeconds(TICK_INTERVAL_SEC).TotalMilliseconds);

            Log.Debug(TAG, $"Timer started, checking every {TICK_INTERVAL_SEC * 1000}ms."); // NEW LOG

            // 5. Return "Sticky" to ensure the service restarts if killed
            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            Log.Debug(TAG, "Usage Tracking Service stopped.");
            timer?.Dispose();
            timer = null;
            base.OnDestroy();
        }

        private void InitializeAppTimers()
        {
            var timerSettings = db.GetAllSettingsAsync().Result;
            var timerSnapshots = db.GetAllUsageStatesAsync().Result;
            Dictionary<String, TimerStatusSnapshots> timerSnapshotsDict = timerSnapshots.ToDictionary(s => s.PackageName);

            foreach (AppSettings appSetting in timerSettings)
            {
                string packageName = appSetting.PackageName;

                // If there is a snapshot in the database for this app
                if (timerSnapshotsDict.ContainsKey(appSetting.PackageName))
                {
                    TimerStatusSnapshots timerSnapshot = timerSnapshotsDict[appSetting.PackageName];
                    if (timerSnapshot.Timestamp.Date == DateTime.Today)
                    {
                        if (appSetting != null && appSetting.Enabled)
                        {
                            TimeSpan dailyLimit = appSetting.DailyTimeLimit;
                            TimeSpan sessionLimit = appSetting.SessionTimeLimit;
                            TimeSpan dailyUsed = TimeSpan.FromMilliseconds(timerSnapshot.DailyElapsedMs);

                            appTimers[packageName] = new HourGuardTimer(dailyLimit, dailyUsed, sessionLimit);

                            Log.Debug(TAG, $"Restored timer for {packageName} with {dailyUsed.TotalMinutes} minutes elapsed");
                        }
                    }
                    else
                    {
                        Log.Debug(TAG, $"Ignoring outdated snapshot for {timerSnapshot.PackageName} from {timerSnapshot.Timestamp.Date}");
                        db.ClearUsageStateAsync(timerSnapshot.PackageName).Wait();
                    }
                }
                // If there is not a database snapshot or the snapshot was outdated
                if (appSetting != null && appSetting.Enabled && !appTimers.ContainsKey(packageName))
                {
                    appTimers[packageName] = new HourGuardTimer(appSetting.DailyTimeLimit, appSetting.SessionTimeLimit);
                    Log.Debug(TAG, $"Initialized timer for {packageName}.");
                }
            }
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

                    if (currentForegroundApp == "com.SeniorDesign.HourGuard")
                    {
                        currentForegroundApp = sortedStats.Skip(1).First()?.PackageName;
                    }

                    Log.Debug(TAG, $"FOREGROUND APP DETECTED: {currentForegroundApp}"); // NEW LOG: Log the detected app

                    if (string.IsNullOrEmpty(currentForegroundApp)) return;

                    // Only proceed if the app has an entry in app settings and is enabled
                    if (db.IsEnabledAsync(currentForegroundApp).Result)
                    {
                        // Gets readable app name
                        string appName = AndroidAppUtils.GetAppNameFromPackage(currentForegroundApp);

                        Log.Debug(TAG, $"Targeted app recognized: {currentForegroundApp}");
                        // Show popup if a *new* app has come to the foreground, otherwise incriment timer
                        if (currentForegroundApp != lastForegroundApp)
                        {
                            Log.Debug(TAG, $"App changed: {currentForegroundApp}. Previous was: {lastForegroundApp}. Showing popup"); // Enhanced Log
                            // App was opened! Show the popup.
                            ShowPopup(appName, appTimers[currentForegroundApp].GetDailyTimeUsed(), appTimers[currentForegroundApp].GetDailyTimeLimit());

                            // Update the last known app
                            lastForegroundApp = currentForegroundApp;
                        }
                        else
                        {
                            (int dailyTimerStatus, int sessionTimerStatus) timerStatuses = appTimers[currentForegroundApp].TickTimers(TimeSpan.FromSeconds(TICK_INTERVAL_SEC));
                            saveUsageSnapshot();

                            TimeSpan dailyTimeLimit = appTimers[currentForegroundApp].GetDailyTimeLimit();
                            TimeSpan dailyTimeUsed = appTimers[currentForegroundApp].GetDailyTimeUsed();

                            Log.Debug(TAG, $"Timer ticked for {currentForegroundApp}. Daily time: {dailyTimeUsed.TotalMinutes}/{dailyTimeLimit.TotalMinutes} minutes, Daily Status: {timerStatuses.dailyTimerStatus}, Session Status: {timerStatuses.sessionTimerStatus}"); // NEW LOG: Timer status

                            if (timerStatuses.dailyTimerStatus == HourGuardTimer.TIMER_EXCEEDED)
                            {
                                Log.Debug(TAG, $"Time limit reached for {currentForegroundApp}. Showing popup.");
                                //TODO: New popup for daily limit reached (add text to it?)
                                ShowPopup(appName, dailyTimeUsed, dailyTimeLimit);
                            }
                            else if (timerStatuses.sessionTimerStatus == HourGuardTimer.TIMER_EXCEEDED)
                            {
                                Log.Debug(TAG, $"Session time limit reached for {currentForegroundApp}. Showing popup.");
                                ShowPopup(appName, dailyTimeUsed, dailyTimeLimit);
                            }
                            else if (timerStatuses.dailyTimerStatus == HourGuardTimer.TIMER_WARNING)
                            {
                                Log.Debug(TAG, $"Daily time limit warning for {currentForegroundApp}.");

                                //TODO: Call warning popup for daily limit approaching
                            }
                        }
                    }
                    else
                    {
                        // Update the last known app
                        lastForegroundApp = currentForegroundApp;
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

        private void ShowPopup(string? appName = null, TimeSpan? dailyTimeUsed = null, TimeSpan? dailyTimeLimit = null, int? streak = null)
        {
            // We must start an Activity from a service context, so we add NEW_TASK flag
            Intent popupIntent = new Intent(this, typeof(DialogActivity));
            popupIntent.AddFlags(ActivityFlags.NewTask);
            popupIntent.PutExtra("appName", appName);
            if (dailyTimeUsed.HasValue)
            {
                long dailyTimeUsedMillis = (long)dailyTimeUsed.Value.TotalMilliseconds;
                popupIntent.PutExtra("dailyTimeUsed", dailyTimeUsedMillis);
            }
            if (dailyTimeLimit.HasValue)
            {
                long dailyTimeLimitMillis = (long)dailyTimeLimit.Value.TotalMilliseconds;
                popupIntent.PutExtra("dailyTimeLimit", dailyTimeLimitMillis);
            }
            popupIntent.PutExtra("streak", streak ?? 0);
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
        
        private void saveUsageSnapshot()
        {
            foreach (var timer in appTimers)
            {
                String packageName = timer.Key;
                HourGuardTimer timerData = timer.Value;

                var snapshot = new TimerStatusSnapshots
                {
                    PackageName = packageName,
                    Timestamp = DateTime.UtcNow,
                    DailyElapsedMs = (long) timerData.GetDailyTimeUsed().TotalMilliseconds
                };
                db.SaveUsageStateAsync(snapshot).Wait();
            }
        }
    }
}