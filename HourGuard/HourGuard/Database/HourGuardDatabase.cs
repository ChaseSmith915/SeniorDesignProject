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
        public Task<List<AppSettings>> GetAllSettingsAsync() =>
            db.Table<AppSettings>().ToListAsync();

        // Gets the settings for all enabled apps
        public Task<List<AppSettings>> GetEnabledSettingsAsync() =>
            db.Table<AppSettings>()
              .Where(x => x.Enabled)
              .ToListAsync();

        // Saves or updates the settings for a specific app
        public Task SaveSettingAsync(AppSettings settings) =>
            db.InsertOrReplaceAsync(settings);

        // Sets the enabled status for a specific app
        public Task SetEnabledAsync(string packageName, bool enabled)
        {
            return db.ExecuteAsync(
                "UPDATE AppSettings SET Enabled = ? WHERE PackageName = ?",
                enabled,
                packageName);
        }

        // Checks if a specific app has settings and is enabled
        public async Task<bool> IsEnabledAsync(string packageName)
        {
            return await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM AppSettings WHERE PackageName = ? AND Enabled = 1",
                packageName) > 0;
        }

        public Task DeleteSetting(string packageName) =>
            db.Table<AppSettings>()
              .Where(x => x.PackageName == packageName)
              .DeleteAsync();

        // ─────────────────────────────
        // App usage state (runtime)
        // ─────────────────────────────

        // Gets the timer snapshot for a specific app by package name
        public Task<TimerStatusSnapshots?> GetUsageStateAsync(string packageName) =>
            db.Table<TimerStatusSnapshots>()
              .Where(x => x.PackageName == packageName)
              .FirstOrDefaultAsync();

        // Gets the timer snapshots for all apps
        public Task<List<TimerStatusSnapshots>> GetAllUsageStatesAsync() =>
            db.Table<TimerStatusSnapshots>().ToListAsync();

        // Saves or updates the timer snapshot for a specific app
        public Task SaveUsageStateAsync(TimerStatusSnapshots state) =>
            db.InsertOrReplaceAsync(state);

        // Clears the timer snapshot for a specific app
        public Task ClearUsageStateAsync(string packageName) =>
            db.Table<TimerStatusSnapshots>()
              .Where(x => x.PackageName == packageName)
              .DeleteAsync();

        // Clears all timer snapshots
        public Task ClearAllUsageStatesAsync() =>
            db.DeleteAllAsync<TimerStatusSnapshots>();
    }
}
