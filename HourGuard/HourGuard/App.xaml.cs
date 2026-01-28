using HourGuard.Database;

namespace HourGuard
{
    public partial class App : Application
    {
        internal static HourGuardDatabase Database { get; private set; } = null!;

        public App()
        {
            InitializeComponent();

            Database = new HourGuardDatabase();

            MainPage = new AppShell();
        }
    }
}
