using Microsoft.Maui.Animations;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HourGuard.Platforms.Android
{
    class HourGuardTimer
    {
        // Timer status constants returned by TickTimers
        public const int TIMER_RUNNING = 0;
        public const int TIMER_WARNING = 1;
        public const int TIMER_EXCEEDED = 2;

        // Durration before time being up that warning status is returned
        public static TimeSpan WARN_DURATION = TimeSpan.FromMinutes(5);

        private TimeSpan dailyTimeLimit;
        private TimeSpan dailyTimeUsed;

        private TimeSpan sessionTimeLimit;
        private TimeSpan sessionTimeUsed;

        public HourGuardTimer(TimeSpan dailyTimeLimit, TimeSpan dailyTimeUsed, TimeSpan sessionTimeLimit, TimeSpan sessionTimeUsed)
        {
            this.dailyTimeLimit = dailyTimeLimit;
            this.dailyTimeUsed = dailyTimeUsed;
            this.sessionTimeLimit = sessionTimeLimit;
            this.sessionTimeUsed = sessionTimeUsed;
        }

        public TimeSpan GetDailyTimeLimit()
        {
            return dailyTimeLimit;
        }

        public void SetDailyTimeLimit(TimeSpan newLimit)
        {
            this.dailyTimeLimit = newLimit;
        }

        public TimeSpan GetDailyTimeUsed()
        {
            return dailyTimeUsed;
        }

        public void ResetDailyTimer()
        {
            this.dailyTimeUsed = TimeSpan.Zero;
        }

        public TimeSpan GetSessionTimeLimit()
        {
            return sessionTimeLimit;
        }

        public void SetSessionTimeLimit(TimeSpan newLimit)
        {
            this.sessionTimeLimit = newLimit;
        }

        public TimeSpan GetSessionTimeUsed()
        {
            return sessionTimeUsed;
        }

        public void ResetSessionTimer()
        {
            this.sessionTimeUsed = TimeSpan.Zero;
        }

        /** Tick the timer by the specified elapsed time.
         * 
         * @param timeElapsed The time elapsed since the last tick.
         * @return A tuple containing the status of the daily timer and session timer:
         *         - 0: Timer is still running without issue
         *         - 1: Timer is about to reach its limit (5 minutes)
         *         - 2: Timer has reached its limit
         */
        public (int dailyTimerStatus, int sessionTimerStatus) TickTimers(TimeSpan timeElapsed)
        {
            int dailyStatus = TIMER_RUNNING;
            int sessionStatus = TIMER_RUNNING;

            this.dailyTimeUsed += timeElapsed;
            this.sessionTimeUsed += timeElapsed;

            if (dailyTimeLimit - dailyTimeUsed <= TimeSpan.Zero)
            {
                dailyStatus = TIMER_EXCEEDED;
            }
            else if (dailyTimeLimit - dailyTimeUsed <= WARN_DURATION)
            {
                dailyStatus = TIMER_WARNING;
            }

            if (sessionTimeLimit - sessionTimeUsed <= TimeSpan.Zero)
            {
                sessionStatus = TIMER_EXCEEDED;
            }
            else if (sessionTimeLimit - sessionTimeUsed <= WARN_DURATION)
            {
                sessionStatus = TIMER_WARNING;
            }

            return (dailyStatus, sessionStatus);
        }
    }
}
