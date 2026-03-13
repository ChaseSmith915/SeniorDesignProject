using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HourGuard.Database
{
    // Stores the user's global streak data (one row, always Id = 1)
    public class GlobalStreak
    {
        [PrimaryKey]
        public int Id { get; set; } = 1; // Always 1 — there is only ever one global streak row

        // Number of consecutive compliant days.
        // Incremented at midnight if the user was compliant, reset to 0 if not.
        // Preserved unchanged if the service was not running.
        public int CurrentStreak { get; set; } = 0;
    }

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
            db.CreateTableAsync<GlobalStreak>().Wait();
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

        public Task SetSessionTimerAsync(string packageName, TimeSpan sessionTimer)
        {
            return db.ExecuteAsync(
                "UPDATE AppSettings SET SessionLimitMs = ? WHERE PackageName = ?",
                sessionTimer.TotalMilliseconds,
                packageName);
        }

        // Checks if a specific app has settings and is enabled
        public async Task<bool> IsEnabledAsync(string packageName)
        {
            return await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM AppSettings WHERE PackageName = ? AND Enabled = 1",
                packageName) > 0;
        }

        public Task<TimeSpan> GetSessionTimer(string packageName)
        {
            return db.ExecuteScalarAsync<double>(
                "SELECT SessionLimitMs FROM AppSettings WHERE PackageName = ?",
                packageName).ContinueWith(t => TimeSpan.FromMilliseconds(t.Result));
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

        // ─────────────────────────────
        // Global streak
        // ─────────────────────────────

        // Gets the current streak data, or a fresh default if it doesn't exist yet
        public async Task<GlobalStreak> GetStreakAsync()
        {
            var streak = await db.Table<GlobalStreak>()
                                 .Where(x => x.Id == 1)
                                 .FirstOrDefaultAsync();

            // If no streak row exists yet, return a default (streak of 0)
            return streak ?? new GlobalStreak();
        }

        // Returns the current streak count.
        public async Task<int> GetCurrentStreakCountAsync()
        {
            var streak = await GetStreakAsync();
            return streak.CurrentStreak;
        }

        // Call this at midnight when the user was compliant all day.
        // Increments the streak by 1.
        public async Task IncrementStreakAsync()
        {
            var streak = await GetStreakAsync();
            streak.CurrentStreak++;
            await db.InsertOrReplaceAsync(streak);
        }

        // Call this at midnight when the user broke their limits during the day.
        // Resets the streak back to 0.
        public async Task BreakStreakAsync()
        {
            var streak = await GetStreakAsync();
            streak.CurrentStreak = 0;
            await db.InsertOrReplaceAsync(streak);
        }
    }
}