using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using System;
using System.Threading.Tasks;

namespace HourGuard.Platforms.Android
{
    [BroadcastReceiver(Enabled = true, Exported = true)]
    [IntentFilter(new[] { Intent.ActionBootCompleted })]
    public class BootReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            if (intent?.Action == Intent.ActionBootCompleted)
            {
                // Read persisted "service enabled" flag and restart service if needed
                var prefs = context.GetSharedPreferences("HourGuardPrefs", FileCreationMode.Private);
                bool wasEnabled = prefs.GetBoolean("ServiceEnabled", false);

                if (wasEnabled)
                {
                    Intent serviceIntent = new Intent(context, typeof(UsageTrackingService));
                    if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                    {
                        context.StartForegroundService(serviceIntent);
                    }
                    else
                    {
                        context.StartService(serviceIntent);
                    }
                }
            }
        }
    }
}
