using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace HourGuard.Database
{
    [Table("AppSettings")]
    internal class AppSettings
    {
        [PrimaryKey]
        public string PackageName { get; set; } = null!;

        public bool Enabled { get; set; }

        // Store TimeSpan as milliseconds
        public long DailyLimitMs { get; set; }
        public long SessionLimitMs { get; set; }

        [Ignore]
        public TimeSpan DailyTimeLimit
        {
            get => TimeSpan.FromMilliseconds(DailyLimitMs);
            set => DailyLimitMs = (long)value.TotalMilliseconds;
        }

        [Ignore]
        public TimeSpan SessionTimeLimit
        {
            get => TimeSpan.FromMilliseconds(SessionLimitMs);
            set => SessionLimitMs = (long)value.TotalMilliseconds;
        }
    }
}