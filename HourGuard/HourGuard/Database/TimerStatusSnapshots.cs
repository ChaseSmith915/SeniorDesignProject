using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace HourGuard.Database
{
    [Table("AppUsageSnapshots")]
    internal class TimerStatusSnapshots
    {
        [PrimaryKey]
        public string PackageName { get; set; } = null!;

        public long TimestampTicks { get; set; }

        public long DailyElapsedMs { get; set; }

        public long SessionStartTimeMs { get; set; }

        [Ignore]
        public DateTime Timestamp
        {
            get => new DateTime(TimestampTicks, DateTimeKind.Utc);
            set => TimestampTicks = value.ToUniversalTime().Ticks;
        }

        [Ignore]
        public TimeSpan DailyElapsed
        {
            get => TimeSpan.FromMilliseconds(DailyElapsedMs);
            set => DailyElapsedMs = (long)value.TotalMilliseconds;
        }

        [Ignore]
        public DateTime SessionStartTime
        {
            get => DateTimeOffset.FromUnixTimeMilliseconds(SessionStartTimeMs).UtcDateTime;
            set => SessionStartTimeMs = new DateTimeOffset(value.ToUniversalTime()).ToUnixTimeMilliseconds();
        }
    }
}
