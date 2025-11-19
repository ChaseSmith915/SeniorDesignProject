using Android.Content.PM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Android.App.ActivityManager;
using Android.Graphics.Drawables;

using Android.Graphics;


#if ANDROID
using Android.Content;
#endif

namespace MauiApp1_testing_android_fesability
{
    public partial class TargetApps : ContentPage
    {
        private MainPage mainPageObj;

        public TargetApps(MainPage mainPage)
        {
            InitializeComponent();
            initializeAppList();

            mainPageObj = mainPage;
        }

        // Initialize the list of installed apps as buttons
        private void initializeAppList()
        {
            PackageManager pm = Android.App.Application.Context.PackageManager;

            List<ApplicationInfo> installedApps = pm.GetInstalledApplications(PackageInfoFlags.MetaData)
                .OrderBy(app => app.LoadLabel(pm)?.ToString())
                .ToList();

            foreach (ApplicationInfo appInfo in installedApps)
            {
                String appName = appInfo.LoadLabel(pm)?.ToString();
                ImageSource appIcon = GetAppIcon(appInfo);

                if (shouldNotDisplayApp(appName))
                {
                    continue;
                }

                AppStack.Add(createAppRow(appName, appIcon));
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
        private bool shouldNotDisplayApp(String appName)
        {
            bool shouldNotDisplay;

            if (String.IsNullOrEmpty(appName)) { shouldNotDisplay = true; }
            else if (appName.Equals("HourGuard", StringComparison.OrdinalIgnoreCase)) { shouldNotDisplay = true; }
            else if (appName.StartsWith("com.", StringComparison.OrdinalIgnoreCase)) { shouldNotDisplay = true; }
            else { shouldNotDisplay = false; }

            return shouldNotDisplay;
        }

        // Creates a row of the listed apps with the icon, name, and a select button
        private Grid createAppRow(String appName, ImageSource appIcon)
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
                    new RowDefinition { Height = new GridLength(50) }
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
                VerticalOptions = LayoutOptions.Center,
                ClassId = appName
            };
            selectButton.Clicked += TargetNewApp;

            appRow.Add(appImage, 0, 0);
            appRow.Add(appLabel, 1, 0);
            appRow.Add(selectButton, 2, 0);

            return appRow;
        }

        // When an app button is clicked, add it to the targeted apps list and return to main page
        private void TargetNewApp(object sender, EventArgs e)
        {
            Button senderButton = (Button)sender;

            mainPageObj.addTargetedApp(senderButton.ClassId);
            Navigation.PopAsync();
        }
    }
}
