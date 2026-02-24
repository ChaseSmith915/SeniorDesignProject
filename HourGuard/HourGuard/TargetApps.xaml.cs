using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Graphics.Drawables;
using Microsoft.Maui.Dispatching;
using System.Collections.ObjectModel;

using Android.Graphics;

using HourGuard.Database;

namespace HourGuard
{
    public partial class TargetApps : ContentPage
    {
        private MainPage mainPage;
        private HourGuardDatabase db = App.Database;

        public ObservableCollection<AppItem> Apps { get; } = new();

        public Command<AppItem> TargetCommand { get; }

        private string[] targetedApps;

        public TargetApps(MainPage mainPage)
        {
            InitializeComponent();

            this.mainPage = mainPage;
            this.targetedApps = db.GetAllSettingsAsync().Result.Select(s => s.PackageName).ToArray();

            BindingContext = this;

            TargetCommand = new Command<AppItem>(TargetNewApp);

            LoadApps();
        }

        private async void LoadApps()
        {
            await Task.Run(() =>
            {
                var pm = Android.App.Application.Context.PackageManager;

                var installedApps = pm.GetInstalledApplications(PackageInfoFlags.MatchAll)
                    .OrderBy(app => app.LoadLabel(pm)?.ToString())
                    .ToList();

                foreach (var appInfo in installedApps)
                {
                    string appName = appInfo.LoadLabel(pm)?.ToString();
                    string packageName = appInfo.PackageName;

                    if (ShouldNotDisplayApp(appInfo, appName, packageName))
                        continue;

                    var icon = GetIconFromAppInfo.GetAppIcon(appInfo);

                    var item = new AppItem
                    {
                        Name = appName,
                        PackageName = packageName,
                        Icon = icon
                    };

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Apps.Add(item);
                    });
                }
            });

            AppListView.ItemsSource = Apps;
        }

        private bool ShouldNotDisplayApp(ApplicationInfo appInfo, string appName, string packageName)
        {
            var pm = Android.App.Application.Context.PackageManager;

            if (string.IsNullOrEmpty(appName))
                return true;

            if (packageName.Equals(Android.App.Application.Context.PackageName))
                return true;

            if (targetedApps.Contains(packageName))
                return true;

            // Check if app appears in the launcher
            Intent intent = new Intent(Intent.ActionMain);
            intent.AddCategory(Intent.CategoryLauncher);

            var launchableApps = pm.QueryIntentActivities(intent, 0);
            bool isLaunchable = launchableApps.Any(a => a.ActivityInfo.PackageName == packageName);

            if (!isLaunchable)
                return true;

            return false;
        }

        private void TargetNewApp(AppItem item)
        {
            db.SaveSettingAsync(new AppSettings{PackageName = item.PackageName, Enabled = true, DailyTimeLimit = TimeSpan.FromMinutes(10)}).Wait();
            this.mainPage.AddTargetedApp(item.Name, item.PackageName);

            Navigation.PopAsync();
        }
    }
}
