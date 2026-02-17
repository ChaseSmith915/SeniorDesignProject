using Android.App;
using Android.OS;
using Android.Widget;
using Intent = Android.Content.Intent;

namespace HourGuard
{
    [Activity(Label = "Warning", Theme = "@style/DialogTheme")]
    public class TimeWarningPopup : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetFinishOnTouchOutside(false);

            // grabs arguments
            string appPackageName = Intent.GetStringExtra("appPackageName");

            // get app name
            string appName = "this app";
            if (appPackageName == null)
            {
                appName = "this app";
            }
            else
            {
                appName = AndroidAppUtils.GetAppNameFromPackage(appPackageName);
            }

            // load layout from xml file
            SetContentView(Resource.Layout.time_warning_popup);

            // set variables
            var continueIntoAppText = FindViewById<TextView>(Resource.Id.continueIntoAppText);
            var buttonLayout = FindViewById<LinearLayout>(Resource.Id.buttonLayout);
            var yesButton = FindViewById<Android.Widget.Button>(Resource.Id.yesButton);

            // continue into app
            continueIntoAppText.Text = $"You only have 5 minutes remaining in {appName} today!";

            // buttons
            yesButton.Text = "Continue";
            yesButton.Click += (s, e) =>
            {
                //var launchIntent = PackageManager.GetLaunchIntentForPackage("com.android.chrome");

                //launchIntent.AddFlags(ActivityFlags.NewTask);
                //StartActivity(launchIntent);

                FinishAndRemoveTask();
            };
        }
    }
}
