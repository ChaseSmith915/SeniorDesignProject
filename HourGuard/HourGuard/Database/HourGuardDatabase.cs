using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HourGuard.Database
{
    // This is a database handler for the database used by HourGuard
    internal class HourGuardDatabase
    {
        private readonly SQLiteAsyncConnection db;

        // Constructor - initializes the database connection and creates tables if they don't exist
        public HourGuardDatabase()
        {
            var path = Path.Combine(
                FileSystem.AppDataDirectory,
                "appsettings.db");

            db = new SQLiteAsyncConnection(path);

            // Create tables if they doesn't exist
            db.CreateTableAsync<AppSettings>().Wait();
            db.CreateTableAsync<TimerStatusSnapshots>().Wait();
        }

        // ─────────────────────────────
        // App settings (configuration)
        // ─────────────────────────────

        // Gets the settings for a specific app by package name
        public Task<AppSettings?> GetSettingAsync(string packageName) =>
            db.Table<AppSettings>()
              .Where(x => x.PackageName == packageName)
              .FirstOrDefaultAsync();

        // Gets the settings for all apps
        public Task<List<AppSettings>> GetAllSettignsAsync() =>
            db.Table<AppSettings>().ToListAsync();

        // Gets the settings for all enabled apps
        public Task<List<AppSettings>> GetEnabledSettignsAsync() =>
            db.Table<AppSettings>()
              .Where(x => x.Enabled)
              .ToListAsync();

        // Saves or updates the settings for a specific app
        public Task SaveSettingAsync(AppSettings settings) =>
            db.InsertOrReplaceAsync(settings);

        // ─────────────────────────────
        // App usage state (runtime)
        // ─────────────────────────────

        // Gets the timer snapshot for a specific app by package name
        public Task<AppUsageState?> GetUsageStateAsync(string packageName) =>
            db.Table<AppUsageState>()
              .Where(x => x.PackageName == packageName)
              .FirstOrDefaultAsync();

        // Gets the timer snapshots for all apps
        public Task<List<AppUsageState>> GetAllUsageStatesAsync() =>
            db.Table<AppUsageState>().ToListAsync();

        // Saves or updates the timer snapshot for a specific app
        public Task SaveUsageStateAsync(AppUsageState state) =>
            db.InsertOrReplaceAsync(state);

        // Clears the timer snapshot for a specific app
        public Task ClearUsageStateAsync(string packageName) =>
            db.Table<AppUsageState>()
              .Where(x => x.PackageName == packageName)
              .DeleteAsync();

        // Clears all timer snapshots
        public Task ClearAllUsageStatesAsync() =>
            db.DeleteAllAsync<AppUsageState>();
    }
}
