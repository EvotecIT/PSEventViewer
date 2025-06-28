using System;

namespace EventViewerX {
    public enum TimePeriod {
        PastHour,
        CurrentHour,
        PastDay,
        CurrentDay,
        PastMonth,
        CurrentMonth,
        PastQuarter,
        CurrentQuarter,
        Last3Days,
        Last7Days,
        Last14Days,
        Last1Hours,
        Last2Hours,
        Last3Hours,
        Last6Hours,
        Last12Hours,
        Last16Hours,
        Last24Hours,
        Today,
        Yesterday,
        Everything,
        TillLastMonday,
        TillLastTuesday,
        TillLastWednesday,
        TillLastThursday,
        TillLastFriday,
        TillLastSaturday,
        TillLastSunday,
    }

    internal static class TimeHelper {
        internal static (DateTime? StartTime, DateTime? EndTime, TimeSpan? LastPeriod) GetTimePeriod(TimePeriod timePeriod) {
            DateTime now = DateTime.UtcNow;
            DateTime? startTime = null;
            DateTime? endTime = null;
            TimeSpan? lastPeriod = null;

            switch (timePeriod) {
                case TimePeriod.PastHour:
                    startTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddHours(-1);
                    endTime = startTime.Value.AddHours(1);
                    break;
                case TimePeriod.CurrentHour:
                    startTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);
                    endTime = startTime.Value.AddHours(1);
                    break;
                case TimePeriod.PastDay:
                    startTime = now.Date.AddDays(-1);
                    endTime = startTime.Value.AddDays(1);
                    break;
                case TimePeriod.CurrentDay:
                    startTime = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
                    endTime = startTime.Value.AddDays(1);
                    break;
                case TimePeriod.PastMonth:
                    startTime = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
                    endTime = new DateTime(now.Year, now.Month, 1);
                    break;
                case TimePeriod.CurrentMonth:
                    startTime = new DateTime(now.Year, now.Month, 1);
                    endTime = startTime.Value.AddMonths(1);
                    break;
                case TimePeriod.PastQuarter:
                    int currentQuarter = (now.Month - 1) / 3 + 1;
                    DateTime startOfCurrentQuarter = new DateTime(now.Year, (currentQuarter - 1) * 3 + 1, 1);
                    startTime = startOfCurrentQuarter.AddMonths(-3);
                    endTime = startOfCurrentQuarter;
                    break;
                case TimePeriod.CurrentQuarter:
                    currentQuarter = (now.Month - 1) / 3 + 1;
                    startTime = new DateTime(now.Year, (currentQuarter - 1) * 3 + 1, 1);
                    endTime = startTime.Value.AddMonths(3);
                    break;
                case TimePeriod.Last3Days:
                    startTime = DateTime.UtcNow.Date.AddDays(-3);
                    break;
                case TimePeriod.Last7Days:
                    startTime = DateTime.UtcNow.Date.AddDays(-7);
                    break;
                case TimePeriod.Last14Days:
                    startTime = DateTime.UtcNow.Date.AddDays(-14);
                    break;
                case TimePeriod.Last1Hours:
                    lastPeriod = TimeSpan.FromHours(1);
                    break;
                case TimePeriod.Last2Hours:
                    lastPeriod = TimeSpan.FromHours(2);
                    break;
                case TimePeriod.Last3Hours:
                    lastPeriod = TimeSpan.FromHours(3);
                    break;
                case TimePeriod.Last6Hours:
                    lastPeriod = TimeSpan.FromHours(6);
                    break;
                case TimePeriod.Last12Hours:
                    lastPeriod = TimeSpan.FromHours(12);
                    break;
                case TimePeriod.Last16Hours:
                    lastPeriod = TimeSpan.FromHours(16);
                    break;
                case TimePeriod.Last24Hours:
                    lastPeriod = TimeSpan.FromHours(24);
                    break;
                case TimePeriod.Today:
                    startTime = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
                    endTime = startTime.Value.AddDays(1);
                    break;
                case TimePeriod.Yesterday:
                    startTime = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0).AddDays(-1);
                    endTime = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
                    break;
                case TimePeriod.Everything:
                    startTime = null;
                    endTime = null;
                    break;
                case TimePeriod.TillLastMonday:
                    startTime = GetLastDayOfWeek(DayOfWeek.Monday, now);
                    break;
                case TimePeriod.TillLastTuesday:
                    startTime = GetLastDayOfWeek(DayOfWeek.Tuesday, now);
                    break;
                case TimePeriod.TillLastWednesday:
                    startTime = GetLastDayOfWeek(DayOfWeek.Wednesday, now);
                    break;
                case TimePeriod.TillLastThursday:
                    startTime = GetLastDayOfWeek(DayOfWeek.Thursday, now);
                    break;
                case TimePeriod.TillLastFriday:
                    startTime = GetLastDayOfWeek(DayOfWeek.Friday, now);
                    break;
                case TimePeriod.TillLastSaturday:
                    startTime = GetLastDayOfWeek(DayOfWeek.Saturday, now);
                    break;
                case TimePeriod.TillLastSunday:
                    startTime = GetLastDayOfWeek(DayOfWeek.Sunday, now);
                    break;
            }

            return (startTime, endTime, lastPeriod);
        }
        private static DateTime GetLastDayOfWeek(DayOfWeek dayOfWeek, DateTime now) {
            int daysUntilDayOfWeek = dayOfWeek - now.DayOfWeek;
            if (daysUntilDayOfWeek > 0) daysUntilDayOfWeek -= 7;
            return now.AddDays(daysUntilDayOfWeek);
        }

    }

}