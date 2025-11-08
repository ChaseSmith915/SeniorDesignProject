using Android.App;
using Android.Runtime;
using Android.Util;

namespace MauiApp1_testing_android_fesability
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
}
