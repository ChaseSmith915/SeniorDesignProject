using Android.Content.PM;

namespace HourGuard
{
    public static class AndroidAppUtils
    {
#if ANDROID
        public static string GetAppNameFromPackage(string packageName)
        {
            string appName = "";
            PackageManager pm = Android.App.Application.Context.PackageManager;

            try
            {
                // Get metadata for the installed app
                ApplicationInfo appInfo = pm.GetApplicationInfo(packageName, 0);

                // Convert to readable label (e.g., "YouTube")
                appName = pm.GetApplicationLabel(appInfo);
            }
            catch (PackageManager.NameNotFoundException)
            {
                // App was uninstalled or package name is invalid
            }

            return appName;
        }
#endif
    }
}
