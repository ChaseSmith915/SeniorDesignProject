using Microsoft.Maui.Animations;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HourGuard.Platforms.Android
{
    internal class HourGuardTimer
    {
        // Timer status constants returned by TickTimers
        public const int TIMER_RUNNING = 0;
        public const int TIMER_WARNING = 1;
        public const int TIMER_EXCEEDED = 2;
        public const int TIMER_NOT_RUNNING = 3;

        // Duration before time being up that warning status is returned
        public static TimeSpan WARN_DURATION = TimeSpan.FromMinutes(5);

        private TimeSpan dailyTimeLimit;
        private TimeSpan dailyTimeUsed;

        // Duration inbetween warnings if you have gone over your daily limit
        private TimeSpan dailyTimeExpiredInterval = TimeSpan.Zero;

        private Boolean dailyWarningIssued = false;

        private TimeSpan sessionTimeLimit;
        private DateTime sessionStartTime;

        // Constructors (Time limits <= 0 indicate no limit)
        public HourGuardTimer(TimeSpan dailyTimeLimit, TimeSpan dailyTimeUsed, TimeSpan sessionTimeLimit)
        {
            this.dailyTimeLimit = dailyTimeLimit;
            this.dailyTimeUsed = dailyTimeUsed;
            this.sessionTimeLimit = sessionTimeLimit;
        }

        public HourGuardTimer(TimeSpan dailyTimeLimit, TimeSpan sessionTimeLimit)
        {
            this.dailyTimeLimit = dailyTimeLimit;
            this.dailyTimeUsed = TimeSpan.Zero;
            this.sessionTimeLimit = sessionTimeLimit;
        }

        public HourGuardTimer(TimeSpan dailyTimeLimit)
        {
            this.dailyTimeLimit = dailyTimeLimit;
            this.dailyTimeUsed = TimeSpan.Zero;
            this.sessionTimeLimit = TimeSpan.Zero;
        }

        public HourGuardTimer()
        {
            this.dailyTimeLimit = TimeSpan.Zero;
            this.dailyTimeUsed = TimeSpan.Zero;
            this.sessionTimeLimit = TimeSpan.Zero;
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
            this.dailyWarningIssued = false;
        }

        public TimeSpan GetSessionTimeLimit()
        {
            return sessionTimeLimit;
        }

        public DateTime GetSessionStartTime()
        {
            return this.sessionStartTime;
        }

        public void StartSessionTimer(TimeSpan sessionDuration)
        {
            this.sessionStartTime = DateTime.UtcNow;
            this.sessionTimeLimit = sessionDuration;
        }

        public void StopSessionTimer()
        {
            this.sessionStartTime = DateTime.MinValue;
            this.sessionTimeLimit = TimeSpan.Zero;
        }

        /** Tick the timer by the specified elapsed time.
         * 
         * @param timeElapsed The time elapsed since the last tick.
         * @return A tuple containing the status of the daily timer and session timer:
         *         - 0: Timer is still running without issue
         *         - 1: Timer is about to reach its limit (5 minutes)
         *         - 2: Timer has reached its limit
         *         - 3: Timer is not running
         */
        public (int dailyTimerStatus, int sessionTimerStatus) TickTimers(TimeSpan timeElapsed)
        {
            return (GetDailyTimerStatus(timeElapsed), GetSessionTimerStatus());
        }

        /** Check the status of the daily timer after ticking by the specified elapsed time.
         * 
         * @param timeElapsed The time elapsed since the last tick.
         * @return A tuple containing the status of the daily timer and session timer:
         *         - 0: Timer is still running without issue
         *         - 1: Timer is about to reach its limit (5 minutes)
         *         - 2: Timer has reached its limit
         *         - 3: Timer is not running
         */
        private int GetDailyTimerStatus(TimeSpan timeElapsed)
        {
            int dailyStatus = TIMER_RUNNING;

            // If there is no daily time limit, never return warnings or exceeded status
            if (dailyTimeLimit > TimeSpan.Zero)
            {
                this.dailyTimeUsed += timeElapsed;

                // If it has been 15 minutes since the last notification that the daily limit has been exceeded, notify again
                if (dailyTimeLimit - dailyTimeUsed <= TimeSpan.Zero)
                {
                    this.dailyTimeExpiredInterval -= timeElapsed;

                    if (this.dailyTimeExpiredInterval <= TimeSpan.Zero)
                    {
                        this.dailyTimeExpiredInterval = TimeSpan.FromMinutes(15);
                        dailyStatus = TIMER_EXCEEDED;
                    }
                }
                else if (dailyTimeLimit - dailyTimeUsed <= WARN_DURATION && !dailyWarningIssued)
                {
                    dailyStatus = TIMER_WARNING;
                    dailyWarningIssued = true;
                }
            }
            else
            {
                dailyStatus = TIMER_NOT_RUNNING;
            }

                return dailyStatus;
        }

        /** Check the status of the session timer after ticking it by the specified elapsed time.
         * 
         * @return A tuple containing the status of the daily timer and session timer:
         *         - 0: Timer is still running without issue or not running at all
         *         - 2: Timer has reached its limit
         *         - 3: Timer is not running
         */
        private int GetSessionTimerStatus()
        {
            int sessionStatus = TIMER_RUNNING;

            // If there is no session time limit, never return exceeded status
            if (sessionTimeLimit > TimeSpan.Zero)
            {
                if (DateTime.UtcNow - sessionStartTime >= sessionTimeLimit)
                {
                    sessionStatus = TIMER_EXCEEDED;
                }
            }
            else
            {
                sessionStatus = TIMER_NOT_RUNNING;
            }

            return sessionStatus;
        }
    }
}
