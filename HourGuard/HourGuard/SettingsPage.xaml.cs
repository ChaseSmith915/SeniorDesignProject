using Android.Content.PM;
using Android.Util;
using HourGuard.Database;

namespace HourGuard
{
    public partial class SettingsPage : ContentPage
    {
        // database file used to store (among other things) the settings for this app
        private HourGuardDatabase db = App.Database;

        private string packageName;

        private bool enabled;
        private TimeSpan dailyLimit;

        private ApplicationInfo appInfo;
        private string appName;

        // Variables holding ui elements editable by the user so their values can be accessed on a save
        private Switch enabledSwitch;
        private Entry dailyTimeLimitEntry;

        public SettingsPage(string packageName)
        {
            this.appInfo = Android.App.Application.Context.PackageManager.GetApplicationInfo(packageName, PackageInfoFlags.MatchAll);
            this.appName = appInfo.LoadLabel(Android.App.Application.Context.PackageManager)?.ToString();

            InitializeComponent();

            this.packageName = packageName;
            PopulateSettingsFromDB();

            InitializeUI();

            RemoveFromTrackedAppsButton.Clicked += (s, e) =>
            {
                RemoveFromTrackedApps();
            };

            SaveSettingsButton.Clicked += (s, e) =>
            {
                SaveSettings();
            };
        }

        private void PopulateSettingsFromDB()
        {
            AppSettings prexistingSettings = db.GetSettingAsync(this.packageName).Result;

            enabled = prexistingSettings.Enabled;
            dailyLimit = prexistingSettings.DailyTimeLimit;
        }

        private void InitializeUI()
        {
            InitializeHeader();

            InitializeSettingOptions();
        }

        private void InitializeHeader()
        {
            Image appIcon = new Image
            {
                Source = GetIconFromAppInfo.GetAppIcon(appInfo),
                HeightRequest = 60,
                WidthRequest = 60,
                VerticalOptions = LayoutOptions.Center
            };

            Label appNameLabel = new Label
            {
                FontSize = 24,
                Text = $"{appName}:",
                VerticalTextAlignment = TextAlignment.Center,
                Padding = 10
            };

            HeaderBar.Children.Add(appIcon);
            HeaderBar.Children.Add(appNameLabel);
        }

        private void InitializeSettingOptions()
        {
            InitializeEnabledSwitch();
            InitializeDailyLimit();
        }

        private void InitializeEnabledSwitch()
        {
            Grid enabledToggleOption = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star }, // Setting name
                    new ColumnDefinition { Width = GridLength.Auto }  // Switch
                },
                Padding = 10,
                Margin = 5
            };

            // Label showing the setting name
            Label settingName = new Label
            {
                FontSize = 16,
                Text = $"Togle management of {appName}:",
                VerticalTextAlignment = TextAlignment.Center,
                Padding = 10
            };

            // Switch that enables/disables monitoring for this app
            this.enabledSwitch = new Switch
            {
                IsToggled = this.enabled,
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Center
            };

            enabledToggleOption.Add(settingName);
            enabledToggleOption.Add(this.enabledSwitch);

            SettingOptions.Children.Add(enabledToggleOption);
        }

        private void InitializeDailyLimit()
        {
            Grid dailyLimitOption = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star }, // Setting name
                    new ColumnDefinition { Width = GridLength.Auto }  // Number input
                },
                Padding = 10,
                Margin = 5
            };

            // Label showing the setting name
            Label settingName = new Label
            {
                FontSize = 16,
                Text = $"Enter a limit in minutes:",
                VerticalTextAlignment = TextAlignment.Center,
                Padding = 10
            };

            // Switch that enables/disables monitoring for this app
            this.dailyTimeLimitEntry = new Entry
            {
                Keyboard = Microsoft.Maui.Keyboard.Numeric,
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Center
            };

            dailyLimitOption.Add(settingName);
            dailyLimitOption.Add(this.dailyTimeLimitEntry);

            SettingOptions.Children.Add(dailyLimitOption);
        }

        private void RemoveFromTrackedApps()
        {
            db.DeleteSetting(packageName);

            Navigation.PopAsync();
        }

        private void SaveSettings()
        {
            if (double.TryParse(this.dailyTimeLimitEntry.Text, out double newLimit))
            {
                AppSettings newSettings = new AppSettings
                {
                    PackageName = this.packageName,
                    Enabled = this.enabledSwitch.IsToggled,
                    DailyTimeLimit = TimeSpan.FromMinutes(newLimit)
                };

                db.SaveSettingAsync(newSettings);
            }
            else
            {
                Log.Error("HourGuardService", "Time limit is not a number. Cannot save.");
            }
        }
    }
}