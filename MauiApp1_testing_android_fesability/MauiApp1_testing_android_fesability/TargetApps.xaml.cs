using Android.Content.PM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Android.App.ActivityManager;

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

        private void initializeAppList()
        {
            List<ApplicationInfo> installedApps;
            installedApps = Android.App.Application.Context.PackageManager.GetInstalledApplications(PackageInfoFlags.MetaData).ToList();

            foreach (ApplicationInfo appInfo in installedApps)
            {
                Button appButton = new Button
                {
                    Text = appInfo.LoadLabel(Android.App.Application.Context.PackageManager),
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                };

                appButton.Clicked += TargetNewApp;

                AppStack.Children.Add(appButton);
            }
        }
        private void TargetNewApp(object sender, EventArgs e)
        {
            Button senderButton = (Button)sender;

            mainPageObj.addTargetedApp(senderButton.Text);
            Navigation.PopAsync();
        }
    }
}
