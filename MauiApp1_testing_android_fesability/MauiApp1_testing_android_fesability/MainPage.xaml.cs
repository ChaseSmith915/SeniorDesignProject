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

            EnablePermissionsButton.Clicked += (s, e) =>
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
    }
}