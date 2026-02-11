using Android.Content.PM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Android.App.ActivityManager;
using Android.Graphics.Drawables;

using Android.Graphics;
using HourGuard.Database;



#if ANDROID
using Android.Content;
#endif

namespace HourGuard
{
    public partial class TargetApps : ContentPage
    {
        private MainPage mainPage;
        private HourGuardDatabase db = App.Database;

        private string[] targetedApps;

        public TargetApps(MainPage mainPage)
        {
            this.mainPage = mainPage;
            this.targetedApps = db.GetAllSettingsAsync().Result.Select(s => s.PackageName).ToArray();

            InitializeComponent();
            InitializeAppList();
        }

        // Initialize the list of installed apps as buttons
        private void InitializeAppList()
        {
            PackageManager pm = Android.App.Application.Context.PackageManager;

            List<ApplicationInfo> installedApps = pm.GetInstalledApplications(PackageInfoFlags.MatchAll)
                .OrderBy(app => app.LoadLabel(pm)?.ToString())
                .ToList();

            foreach (ApplicationInfo appInfo in installedApps)
            {
                string appName = appInfo.LoadLabel(pm)?.ToString();
                string packageName = appInfo.PackageName;
                ImageSource appIcon = GetAppIcon(appInfo);

                if (ShouldNotDisplayApp(appName, packageName))
                {
                    continue;
                }

                AppStack.Add(CreateAppRow(appName, packageName, appIcon));
            }
        }

        // Retrieves the app icon as an ImageSource (if it has an icon)
        private ImageSource GetAppIcon(ApplicationInfo appInfo)
        {
            try
            {
                var pm = Android.App.Application.Context.PackageManager;
                Drawable iconDrawable = appInfo.LoadIcon(pm);

                if (iconDrawable is BitmapDrawable bitmapDrawable)
                {
                    Bitmap bitmap = bitmapDrawable.Bitmap;
                    return ImageSource.FromStream(() =>
                    {
                        var stream = new MemoryStream();
                        bitmap.Compress(Bitmap.CompressFormat.Png, 100, stream);
                        stream.Position = 0;
                        return stream;
                    });
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        // Returns true if the app should NOT be displayed in the list
        private bool ShouldNotDisplayApp(string appName, string packageName)
        {
            if (string.IsNullOrEmpty(appName)) return true;
            if (appName.Equals("HourGuard", StringComparison.OrdinalIgnoreCase)) return true;
            if (appName.StartsWith("com.", StringComparison.OrdinalIgnoreCase)) return true;
            if (targetedApps.Contains(packageName)) return true;
            return false;
        }

        // Creates a row of the listed apps with the icon, name, and a select button
        private Grid CreateAppRow(string appName, string packageName, ImageSource appIcon)
        {
            Grid appRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(50) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(100) }
                },
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto }
                },
                Margin = new Thickness(0, 5),
                Padding = new Thickness(10, 5)
            };

            Image appImage = new Image
            {
                Source = appIcon,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            Label appLabel = new Label
            {
                Text = appName,
                VerticalOptions = LayoutOptions.Center,
                FontSize = 16,
                Padding = new Thickness(10, 0)
            };

            Button selectButton = new Button
            {
                Text = "Target app",
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            selectButton.Clicked += (s, e) => TargetNewApp(s, e, appName, packageName);

            appRow.Add(appImage, 0, 0);
            appRow.Add(appLabel, 1, 0);
            appRow.Add(selectButton, 2, 0);

            return appRow;
        }

        // When an app button is clicked, add it to the targeted apps list and return to main page
        private void TargetNewApp(object sender, EventArgs e, string appName, string packageName)
        {
            db.SaveSettingAsync(new AppSettings{PackageName = packageName, Enabled = true, DailyTimeLimit = TimeSpan.FromMinutes(5)}).Wait();

            this.mainPage.AddTargetedApp(appName, packageName);
            Navigation.PopAsync();
        }
    }
}
