#if ANDROID
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Graphics.Drawables;
using HourGuard.Database;
#endif

namespace HourGuard
{
    public partial class MainPage : ContentPage
    {
        // database file used to store (among other things) the list of targeted apps
        private HourGuardDatabase db = App.Database;

        public MainPage()
        {
            InitializeComponent();
            GetTargetedApps();

            // Navigate to permissions setup page
            EnablePermissionsButton.Clicked += (s, e) =>
            {
                Navigation.PushAsync(new PermissionsSetUp());
            };

            // Navigate to the "Add App" page, passing this page as a callback target
            AddAppButton.Clicked += (s, e) =>
            {
                Navigation.PushAsync(new TargetApps(this));
            };
        }

        // Refreshes the list of targeted apps in the ui
        public void GetTargetedApps()
        {
            ClearCurrentAppList();

            // Retrieve all stored entries
            foreach (AppSettings entry in db.GetAllSettingsAsync().Result)
            {
                string packageName = entry.PackageName;
                bool isTargeted = entry.Enabled;

                // Convert package name to human‑readable app name
                string appName = AndroidAppUtils.GetAppNameFromPackage(packageName);

                // Only add to UI if the app still exists on the device
                if (!string.IsNullOrEmpty(appName))
                {
                    AddTargetedApp(appName, packageName, isTargeted);
                }
            }
        }

        // Removes all apps in the currently targeted list, does not remove add more button
        private void ClearCurrentAppList()
        {
            while(TargetAppsList.Children.Count > 1)
            {
                TargetAppsList.Children.RemoveAt(0);
            }
        }

        // Adds a new row to the UI list
        public void AddTargetedApp(string appName, string packageName, bool startEnabled = true)
        {
            // Create a row with three columns: icon, label, & switch
            Grid newTargetLine = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto }, // App icon
                    new ColumnDefinition { Width = GridLength.Star }, // App name
                    new ColumnDefinition { Width = GridLength.Auto }  // Settings button
                },
                Padding = 10,
                Margin = 5
            };

            var appInfo = Android.App.Application.Context.PackageManager.GetApplicationInfo(packageName, PackageInfoFlags.MatchAll);

            Image newTargetIcon = new Image
            {
                Source = GetIconFromAppInfo.GetAppIcon(appInfo),
                HeightRequest = 40,
                WidthRequest = 40,
                VerticalOptions = LayoutOptions.Center
            };

            // Label showing the app name
            Label newTargetLabel = new Label
            {
                FontSize = 16,
                Text = $"{appName}:",
                VerticalTextAlignment = TextAlignment.Center,
                Padding = 10
            };

            // Switch that enables/disables monitoring for this app
            Button newTargetSettings = new Button
            {
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Center,
                Text = "Settings",

                // Stores package name so that when clicked we can know what app settings to open.
                ClassId = packageName
            };

            // On click open settings page
            newTargetSettings.Clicked += (s, e) =>
            {
                Button settingsButton = (Button)s;
                Navigation.PushAsync(new SettingsPage(settingsButton.ClassId, this));
            };

            // Add UI elements to the row
            newTargetLine.Add(newTargetIcon, 0, 0);
            newTargetLine.Add(newTargetLabel, 1, 0);
            newTargetLine.Add(newTargetSettings, 2, 0);

            // Insert before the "Add App" button row
            TargetAppsList.Children.Insert(
                TargetAppsList.Children.Count - 1,
                newTargetLine);
        }
    }
}
