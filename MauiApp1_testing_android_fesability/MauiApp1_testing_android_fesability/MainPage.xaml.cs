using Microsoft.Maui.Controls;
using Android.Content.PM;
using System.Security.Cryptography.X509Certificates;
using AndroidX.AppCompat.Widget;
using Android.Telephony;
using System.Collections;





#if ANDROID
using Android.Content;
#endif

namespace MauiApp1_testing_android_fesability
{
    public partial class MainPage : ContentPage
    {
#if ANDROID
        const string PREF_KEY_ENABLED = "intercept_enabled";
#endif
        // This tracks the number of target rows and incriments as more target rows are added.
        private int currentTargetNumber = 0;

        public MainPage()
        {
            InitializeComponent();

#if ANDROID
            // These using directives are only valid for Android


            var prefs = Android.App.Application.Context.GetSharedPreferences("APP_PREFS", FileCreationMode.Private);
            bool enabled = prefs.GetBoolean(PREF_KEY_ENABLED, false);
            AppTarget1InterceptSwitch.IsToggled = enabled;

            AppTarget1InterceptSwitch.Toggled += (s, e) =>
            {
                var editor = prefs.Edit();
                editor.PutBoolean(PREF_KEY_ENABLED, e.Value);
                editor.Apply();

                if (e.Value)
                {
                    DisplayAlert("Enable Service",
                        "Make sure the Accessibility service for this app is enabled in Settings → Accessibility.",
                        "OK");
                }
            };

            OpenAccessibilitySettingsButton.Clicked += (s, e) =>
            {
                Intent intent = new Intent(Android.Provider.Settings.ActionAccessibilitySettings);
                intent.SetFlags(ActivityFlags.NewTask);
                Android.App.Application.Context.StartActivity(intent);
            };

            AddAppButton.Clicked += (s, e) =>
            {
                Navigation.PushAsync(new TargetApps(this));
            };
#endif
        }

        public void addTargetedApp(String appName)
        {
            HorizontalStackLayout newTargetLine = new HorizontalStackLayout();

            Label newTargetLabel = new Label();
            newTargetLabel.FontSize = 20;
            newTargetLabel.Text = appName + ":";
            newTargetLabel.VerticalTextAlignment = TextAlignment.Center;
            newTargetLine.Add(newTargetLabel);

            Switch newTargetSwitch = new Switch();
            newTargetLine.Add(newTargetSwitch);

            TargetAppsList.Children.Insert(TargetAppsList.Children.Count - 1, newTargetLine);
        }
    }
}
