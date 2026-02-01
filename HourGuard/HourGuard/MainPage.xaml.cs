#if ANDROID
using Android.Content;
using Android.Content.PM;
#endif

namespace HourGuard
{
    public partial class MainPage : ContentPage
    {
        // SharedPreferences file used to store the list of targeted apps
        private ISharedPreferences preferences =
            Android.App.Application.Context.GetSharedPreferences(
                HourGuardConstants.TARGETED_APPS_FILE_NAME,
                FileCreationMode.Private);

        public MainPage()
        {
            InitializeComponent();
            GetTargetedAppsFromPreferences();

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

        private void GetTargetedAppsFromPreferences()
        {
            // Retrieve all stored entries (packageName → bool)
            IDictionary<string, object> allEntries = preferences.All;

            foreach (KeyValuePair<string, object> entry in allEntries)
            {
                string packageName = entry.Key;
                bool isTargeted = (bool)entry.Value;

                // Convert package name to human‑readable app name
                string appName = AndroidAppUtils.GetAppNameFromPackage(packageName);

                // Only add to UI if the app still exists on the device
                if (!string.IsNullOrEmpty(appName))
                {
                    AddTargetedApp(appName, packageName, isTargeted);
                }
            }
        }

        // Adds a new row to the UI list
        public void AddTargetedApp(string appName, string packageName, bool startEnabled = true)
        {
            // Create a row with two columns: label + switch
            Grid newTargetLine = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star }, // App name
                    new ColumnDefinition { Width = GridLength.Auto }  // Switch
                },
                Padding = new Thickness(0, 5)
            };

            // Label showing the app name
            Label newTargetLabel = new Label
            {
                FontSize = 16,
                Text = $"{appName}:",
                VerticalTextAlignment = TextAlignment.Center
            };

            // Switch that enables/disables monitoring for this app
            Switch newTargetSwitch = new Switch
            {
                IsToggled = startEnabled,
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Center,

                // Store package name inside ClassId so we know which app was toggled
                ClassId = packageName
            };

            // Save changes when toggled
            newTargetSwitch.Toggled += OnTargetToggled;

            // Add UI elements to the row
            newTargetLine.Add(newTargetLabel, 0, 0);
            newTargetLine.Add(newTargetSwitch, 1, 0);

            // Insert before the "Add App" button row
            TargetAppsList.Children.Insert(
                TargetAppsList.Children.Count - 1,
                newTargetLine);
        }

        private void OnTargetToggled(object sender, ToggledEventArgs e)
        {
            // Identify which switch was toggled
            Switch toggledSwitch = (Switch)sender;
            string packageName = toggledSwitch.ClassId;

            // Save the new toggle state to SharedPreferences
            ISharedPreferencesEditor prefsEditor = this.preferences.Edit();
            prefsEditor.PutBoolean(packageName, e.Value).Apply();
        }
    }
}
