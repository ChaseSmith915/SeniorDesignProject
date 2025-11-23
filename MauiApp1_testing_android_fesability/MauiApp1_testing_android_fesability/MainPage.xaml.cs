#if ANDROID
using Android.Content;
using Android.Content.PM;
#endif

namespace HourGuard
{
    public partial class MainPage : ContentPage
    {

        private ISharedPreferences preferances = Android.App.Application.Context.GetSharedPreferences(HourGuardConstants.TARGETED_APPS_FILE_NAME, FileCreationMode.Private);

        public MainPage()
        {
            InitializeComponent();
            getTargetedAppsFromPreferances();

            EnablePermissionsButton.Clicked += (s, e) =>
            {
                Navigation.PushAsync(new PermissionsSetUp());
            };

            AddAppButton.Clicked += (s, e) =>
            {
                Navigation.PushAsync(new TargetApps(this));
            };
        }

        private void getTargetedAppsFromPreferances()
        {
            IDictionary<string, object> allEntries = preferances.All;
            foreach (KeyValuePair<string, object> entry in allEntries)
            {
                string packageName = entry.Key;
                bool isTargeted = (bool)entry.Value;

                string appname = getAppnameFromPackage(packageName);

                if (!string.IsNullOrEmpty(appname))
                {
                    addTargetedApp(appname, packageName, isTargeted);
                }
            }
        }

        private string getAppnameFromPackage(string packageName)
        {
            string appName = "";
            PackageManager pm = Android.App.Application.Context.PackageManager;

            try
            {
                ApplicationInfo appInfo = pm.GetApplicationInfo(packageName, 0);
                appName = pm.GetApplicationLabel(appInfo);
            }
            catch (PackageManager.NameNotFoundException)
            {
                // Package does not exist
            }

            return appName;
        }

        public void addTargetedApp(String appName, String packageName, Boolean startEnabled = true)
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
                IsToggled = startEnabled,
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Center,
                ClassId = packageName
            };

            newTargetSwitch.Toggled += onTargetToggled;

            newTargetLine.Add(newTargetLabel, 0, 0);
            newTargetLine.Add(newTargetSwitch, 1, 0);

            TargetAppsList.Children.Insert(TargetAppsList.Children.Count - 1, newTargetLine);
        }

        private void onTargetToggled(object sender, ToggledEventArgs e)
        {
            Switch toggledSwitch = (Switch)sender;
            string packageName = toggledSwitch.ClassId;

            ISharedPreferencesEditor prefsEditor = this.preferances.Edit();

            prefsEditor.PutBoolean(packageName, e.Value).Apply();
        }
    }
}