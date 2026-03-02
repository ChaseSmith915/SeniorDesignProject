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

        // How many consecutive days the user has stayed within all their limits
        public int CurrentStreak { get; set; } = 0;

        // The last date the user successfully stayed within their limits (stored as a string "yyyy-MM-dd")
        // Used to detect if a day was missed, which resets the streak back to 0
        public string? LastCompliantDate { get; set; }
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

        // Gets just the current streak count.
        // Lazily checks if the streak is still alive — no background job needed.
        // If the last compliant date is older than yesterday, the streak is considered broken.
        public async Task<int> GetCurrentStreakCountAsync()
        {
            var streak = await GetStreakAsync();
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var yesterday = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");

            // If the last compliant date isn't today or yesterday, the streak is broken
            if (streak.LastCompliantDate != today && streak.LastCompliantDate != yesterday)
                return 0;

            return streak.CurrentStreak;
        }

        // Call this when the user successfully completes a compliant day.
        // Increments the streak if it's still alive, otherwise starts a new one from 1.
        public async Task IncrementStreakAsync()
        {
            var streak = await GetStreakAsync();
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var yesterday = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");

            if (streak.LastCompliantDate == today)
            {
                // Already recorded today, do nothing
                return;
            }
            else if (streak.LastCompliantDate == yesterday)
            {
                // Continued the streak — increment it
                streak.CurrentStreak++;
            }
            else
            {
                // Missed one or more days — start a new streak from 1
                streak.CurrentStreak = 1;
            }

            streak.LastCompliantDate = today;
            await db.InsertOrReplaceAsync(streak);
        }
    }
}