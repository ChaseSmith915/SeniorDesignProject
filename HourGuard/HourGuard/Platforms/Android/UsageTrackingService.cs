using Android.App;
using Android.App.Usage;
using Android.Content;
using Android.Content.PM; // Required for ForegroundServiceType on Android 14 (API 34)
using Android.OS;
using Android.Runtime;
using Android.Util; // For Log
using AndroidX.Core.App;
using AndroidX.Startup;
using HourGuard.Database;
using Microsoft;
using System.Linq; // Required for OrderByDescending and Any()
using System.Threading;
using System.Threading.Tasks;

namespace HourGuard.Platforms.Android
{
    [Service(ForegroundServiceType = ForegroundService.TypeDataSync)]
    public class UsageTrackingService : Service
    {
        private readonly HourGuardDatabase db = App.Database;

        private readonly Dictionary<string, HourGuardTimer> appTimers = new Dictionary<string, HourGuardTimer>();

        private Timer timer;
        private string lastForegroundApp = string.Empty;
        private bool wasCompliantToday = true; // Tracks whether the user has exceeded any limit today
        private DateTime lastRefreshDate;
        private bool isInitialized = false; //Tracks if the service has been initialized to prevent multiple initializations if OnStartCommand is called multiple times before the service is destroyed
        
        private int isCheckingForegroundApp = 0; // 0 = not running, 1 = running
        private int isPopupOpen = 0; // 0 = popup not open, 1 = popup open

        private const string NOTIFICATION_CHANNEL_ID = "UsageTrackingServiceChannel";
        private const string TAG = "HourGuardService";
        private const string LAST_REFRESH_DATE_KEY = "LastRefreshDate";
        private const int NOTIFICATION_ID = 1001;
        private const int TICK_INTERVAL_SEC = 1;

        // Constants representing actions that can be sent to the service via Intents (This lets the service be modified without being restarted)
        public const string ACTION_REFRESH_TIMERS = "com.hourguard.action.REFRESH_TIMERS";
        public const string ACTION_POPUP_CLOSED = "com.hourguard.action.POPUP_CLOSED";

        public override IBinder OnBind(Intent intent)
        {
            return null; // We are not using a bound service
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            if (!isInitialized)
            {
                Log.Debug(TAG, "Running initial HourGuard service startup");

                // Prepare to reset daily timers at midnight by storing the last refresh date
                InitializeDailyTimerReset();

                // Initialize appTimers from database settings
                InitializeAppTimers();

                // Create Notification Channel (Required for Android 8.0+)
                CreateNotificationChannel();

                // Create the persistent notification
                var notification = new NotificationCompat.Builder(this, NOTIFICATION_CHANNEL_ID)
                    .SetContentTitle("Time Management Active")
                    .SetContentText("Monitoring app usage...")
                    .SetSmallIcon(Microsoft.Maui.Controls.Resource.Mipmap.appicon)
                    .SetOngoing(true)
                    .Build();

                if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
                {
                    StartForeground(NOTIFICATION_ID, notification, ForegroundService.TypeDataSync);
                }
                else
                {
                    StartForeground(NOTIFICATION_ID, notification);
                }

                timer = new Timer(CheckForegroundApp, null, 0, (int)TimeSpan.FromSeconds(TICK_INTERVAL_SEC).TotalMilliseconds);
                Log.Debug(TAG, $"Timer started, checking every {TICK_INTERVAL_SEC * 1000}ms.");

                isInitialized = true;
            }

            if (intent?.Action == ACTION_REFRESH_TIMERS)
            {
                Log.Debug(TAG, "Refreshing timers from DB");
                InitializeAppTimers();
                return StartCommandResult.Sticky;
            }

            if (intent?.Action == ACTION_POPUP_CLOSED)
            {
                Log.Debug(TAG, "Popup closed, resuming tracking");
                Interlocked.Exchange(ref isPopupOpen, 0);
                return StartCommandResult.Sticky;
            }

            // Return "Sticky" to ensure the service restarts if killed
            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            Log.Debug(TAG, "Usage Tracking Service stopped.");
            timer?.Dispose();
            timer = null;
            base.OnDestroy();
        }

        private void InitializeDailyTimerReset()
        {
            DateTime todayDate = DateTime.Now.Date;

            if (!Preferences.ContainsKey(LAST_REFRESH_DATE_KEY))
            {
                Preferences.Set(LAST_REFRESH_DATE_KEY, todayDate.ToString());
                lastRefreshDate = todayDate;
            }
            else
            {
                lastRefreshDate = DateTime.Parse(Preferences.Get(LAST_REFRESH_DATE_KEY, todayDate.ToString()));
            }
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
                if (timerSnapshotsDict.TryGetValue(appSetting.PackageName, out TimerStatusSnapshots? timerSnapshot))
                {
                    if (timerSnapshot.Timestamp.Date == DateTime.UtcNow.Date)
                    {
                        if (appSetting != null && appSetting.Enabled)
                        {
                            TimeSpan dailyLimit = appSetting.DailyTimeLimit;
                            TimeSpan sessionLimit = appSetting.SessionTimeLimit;
                            TimeSpan dailyUsed = timerSnapshot.DailyElapsed;
                            DateTime sessionStartTime = timerSnapshot.SessionStartTime;

                            appTimers[packageName] = new HourGuardTimer(dailyLimit, dailyUsed, sessionLimit, sessionStartTime);

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
            // Prevent overlapping executions of this method if the previous execution is still running
            if (Interlocked.CompareExchange(ref isCheckingForegroundApp, 1, 0) == 1)
            {
                Log.Debug(TAG, "Skipping tick, previous check still running.");
                return;
            }
            try
            {
                // Checks if the popup is open and skipps the timer tick if so
                if (Volatile.Read(ref isPopupOpen) == 1)
                {
                    Log.Debug(TAG, "Skipping tick, popup is still open.");
                    return;
                }

                Log.Debug(TAG, "TIMER TICK: Executing CheckForegroundApp.");

                ResetDailyTimersIfNeeded();

                var usageStatsManager = (UsageStatsManager)GetSystemService(Context.UsageStatsService);
                if (usageStatsManager == null) return;

                long currentTime = Java.Lang.JavaSystem.CurrentTimeMillis();
                long tenSecondsAgo = currentTime - (10 * 1000);

                // Query for foreground events instead of usage stats to avoid
                // background activity causing incorrect app detection
                var events = usageStatsManager.QueryEvents(tenSecondsAgo, currentTime);
                var usageEvent = new UsageEvents.Event();
                string currentForegroundApp = string.Empty;

                while (events.HasNextEvent)
                {
                    events.GetNextEvent(usageEvent);
                    // 1 = MOVE_TO_FOREGROUND — UsageEvents.Event.MoveToForeground is not exposed in Xamarin bindings
                    if ((int)usageEvent.EventType == 1)
                        currentForegroundApp = usageEvent.PackageName;
                }

                // If no foreground event was found in the last 10 seconds, fall back to last known app
                if (string.IsNullOrEmpty(currentForegroundApp))
                    currentForegroundApp = lastForegroundApp;

                Log.Debug(TAG, $"FOREGROUND APP DETECTED: {currentForegroundApp}");

                // If there is no foreground app or HourGuard is in the foreground then don't tick
                if (string.IsNullOrEmpty(currentForegroundApp) || currentForegroundApp == "com.SeniorDesign.HourGuard")
                    return;

                // Only proceed if the app has an entry in app settings and is enabled
                if (db.IsEnabledAsync(currentForegroundApp).Result)
                {
                    Log.Debug(TAG, $"Targeted app recognized: {currentForegroundApp}");

                    // Show popup if a *new* app has come to the foreground, otherwise increment timer
                    if (currentForegroundApp != lastForegroundApp)
                    {
                        Log.Debug(TAG, $"App changed: {currentForegroundApp}. Previous was: {lastForegroundApp}. Showing popup"); // Enhanced Log

                        lastForegroundApp = currentForegroundApp;

                        // App was opened! Show the popup.
                        ShowPopup(
                            currentForegroundApp,
                            appTimers[currentForegroundApp].GetDailyTimeUsed(),
                            appTimers[currentForegroundApp].GetDailyTimeLimit(),
                            appTimers[currentForegroundApp].GetSessionStartTime(),
                            appTimers[currentForegroundApp].GetSessionTimeLimit()
                        );
                    }
                    else
                    {
                        (int dailyTimerStatus, int sessionTimerStatus) = appTimers[currentForegroundApp].TickTimers(TimeSpan.FromSeconds(TICK_INTERVAL_SEC));
                        SaveUsageSnapshot();

                        TimeSpan dailyTimeLimit = appTimers[currentForegroundApp].GetDailyTimeLimit();
                        TimeSpan dailyTimeUsed = appTimers[currentForegroundApp].GetDailyTimeUsed();

                        TimeSpan sessionTimeLimit = appTimers[currentForegroundApp].GetSessionTimeLimit();
                        DateTime sessionStartTime = appTimers[currentForegroundApp].GetSessionStartTime();

                        Log.Debug(TAG, $"Timer ticked for {currentForegroundApp}.");

                        if (dailyTimerStatus != HourGuardTimer.TIMER_NOT_RUNNING)
                        {
                            Log.Debug(TAG, $"Daily time: {dailyTimeUsed.TotalMinutes}/{dailyTimeLimit.TotalMinutes} minutes, Daily Status: {dailyTimerStatus}");
                        }

                        if (sessionTimerStatus != HourGuardTimer.TIMER_NOT_RUNNING)
                        {
                            Log.Debug(TAG, $"Session timer should run for {((sessionStartTime + sessionTimeLimit) - DateTime.UtcNow).TotalMinutes} more minutes, Session Status: {sessionTimerStatus}");
                        }

                        if (dailyTimerStatus == HourGuardTimer.TIMER_EXCEEDED && sessionTimerStatus == HourGuardTimer.TIMER_EXCEEDED)
                        {
                            Log.Debug(TAG, $"Session and Daily time limit reached for {currentForegroundApp}. Showing popup.");

                            appTimers[currentForegroundApp].StopSessionTimer();
                            ShowPopup(currentForegroundApp, dailyTimeUsed, dailyTimeLimit, sessionStartTime, sessionTimeLimit);
                        }
                        else if (dailyTimerStatus == HourGuardTimer.TIMER_EXCEEDED)
                        {
                            Log.Debug(TAG, $"Time limit reached for {currentForegroundApp}. Showing popup.");
                            ShowPopup(currentForegroundApp, dailyTimeUsed, dailyTimeLimit, sessionStartTime, sessionTimeLimit);
                        }
                        else if (sessionTimerStatus == HourGuardTimer.TIMER_EXCEEDED)
                        {
                            Log.Debug(TAG, $"Session time limit reached for {currentForegroundApp}. Showing popup.");
                            appTimers[currentForegroundApp].StopSessionTimer();
                            ShowPopup(currentForegroundApp, dailyTimeUsed, dailyTimeLimit, sessionStartTime, sessionTimeLimit);
                        }
                        else if (dailyTimerStatus == HourGuardTimer.TIMER_WARNING)
                        {
                            Log.Debug(TAG, $"Daily time limit warning for {currentForegroundApp}.");
                            ShowWarningPopup(currentForegroundApp);
                        }

                        // Start a session timer if there is a limit set and one isn't already running
                        if (sessionTimerStatus == HourGuardTimer.TIMER_NOT_RUNNING)
                        {
                            TimeSpan sessionTimer = db.GetSessionTimer(currentForegroundApp).Result;

                            if (sessionTimer != TimeSpan.Zero)
                            {
                                Log.Debug(TAG, $"Starting session timer for {currentForegroundApp} for {sessionTimer.TotalMinutes} minutes.");
                                appTimers[currentForegroundApp].StartSessionTimer(sessionTimer);
                                db.SetSessionTimerAsync(currentForegroundApp, TimeSpan.Zero);
                            }
                        }
                    }
                }
                else
                {
                    lastForegroundApp = currentForegroundApp;
                }
            }
            catch (Exception ex)
            {
                Log.Error(TAG, $"Error in CheckForegroundApp: {ex.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref isCheckingForegroundApp, 0);
            }
        }

        private void ResetDailyTimersIfNeeded()
        {
            DateTime today = DateTime.Now.Date;
            if (today > lastRefreshDate)
            {
                Log.Debug(TAG, "New day detected. Resetting daily timers.");
                foreach (var timer in appTimers.Values)
                {
                    timer.ResetDailyTimer();
                }
                lastRefreshDate = today;
                Preferences.Set(LAST_REFRESH_DATE_KEY, lastRefreshDate.ToString());
                SaveUsageSnapshot();

                // Increment streak if the user was compliant yesterday, otherwise break it
                if (wasCompliantToday)
                {
                    Log.Debug(TAG, "User was compliant yesterday. Streak incremented.");
                    db.IncrementStreakAsync().Wait();
                }
                else
                {
                    Log.Debug(TAG, "User was not compliant yesterday. Streak broken.");
                    db.BreakStreakAsync().Wait();
                }

                // Reset compliance flag for the new day
                wasCompliantToday = true;
            }
        }

        private void ShowPopup(string appPackageName, TimeSpan dailyTimeUsed, TimeSpan dailyTimeLimit, DateTime sessionStartTime, TimeSpan sessionTimeLimit)
        {
            // As soon as this gets called, lock down the popup state so that no other popups can be opened until this one is closed
            Interlocked.Exchange(ref isPopupOpen, 1);

            try
            {
                // We must start an Activity from a service context, so we add NEW_TASK flag
                Intent popupIntent = new Intent(this, typeof(DialogActivity));
                popupIntent.AddFlags(ActivityFlags.NewTask);
                
                popupIntent.PutExtra("appPackageName", appPackageName);

                double dailyTimeUsedMillis = dailyTimeUsed.TotalMilliseconds;
                popupIntent.PutExtra("dailyTimeUsed", dailyTimeUsedMillis);

                double dailyTimeLimitMillis = dailyTimeLimit.TotalMilliseconds;
                popupIntent.PutExtra("dailyTimeLimit", dailyTimeLimitMillis);

                long sessionStartTimeMillis = new DateTimeOffset(sessionStartTime).ToUnixTimeMilliseconds();
                popupIntent.PutExtra("sessionStartTime", sessionStartTimeMillis);

                double sessionTimeLimitMillis = sessionTimeLimit.TotalMilliseconds;
                popupIntent.PutExtra("sessionTimeLimit", sessionTimeLimitMillis);

                StartActivity(popupIntent);
            }
            catch (Exception e)
            {
                Log.Error(TAG, $"Error showing popup: {e.Message}");
                // If there was an error showing the popup, we should still allow future popups to be shown
                Interlocked.Exchange(ref isPopupOpen, 0);
            }
        }


        private void ShowWarningPopup(string? appPackageName = null)
        {
            Intent popupIntent = new Intent(this, typeof(TimeWarningPopup));
            popupIntent.AddFlags(ActivityFlags.NewTask);

            popupIntent.PutExtra("appPackageName", appPackageName);

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

        private void SaveUsageSnapshot()
        {
            foreach (var timer in appTimers)
            {
                String packageName = timer.Key;
                HourGuardTimer timerData = timer.Value;

                var snapshot = new TimerStatusSnapshots
                {
                    PackageName = packageName,
                    Timestamp = DateTime.UtcNow,
                    DailyElapsed = timerData.GetDailyTimeUsed(),
                    SessionStartTime = timerData.GetSessionStartTime(),
                };
                db.SaveUsageStateAsync(snapshot).Wait();
            }
        }
    }
}