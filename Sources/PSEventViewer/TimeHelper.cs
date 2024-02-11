using System;

namespace PSEventViewer {
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
        Last3Hours,
        Last6Hours,
        Last12Hours,
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
        internal static (DateTime? StartTime, DateTime? EndTime) GetTimePeriod(TimePeriod timePeriod) {
            DateTime now = DateTime.Now;
            DateTime? startTime = null;
            DateTime? endTime = null;

            switch (timePeriod) {
                case TimePeriod.PastHour:
                    startTime = now.AddHours(-1);
                    endTime = null;
                    break;
                case TimePeriod.CurrentHour:
                    startTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);
                    break;
                case TimePeriod.PastDay:
                    startTime = now.AddDays(-1);
                    endTime = null;
                    break;
                case TimePeriod.CurrentDay:
                    startTime = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
                    break;
                case TimePeriod.PastMonth:
                    startTime = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
                    endTime = new DateTime(now.Year, now.Month, 1).AddSeconds(-1);
                    break;
                case TimePeriod.CurrentMonth:
                    startTime = new DateTime(now.Year, now.Month, 1);
                    break;
                case TimePeriod.PastQuarter:
                    int currentQuarter = (now.Month - 1) / 3 + 1;
                    startTime = new DateTime(now.Year, (currentQuarter - 2) * 3 + 1, 1);
                    endTime = new DateTime(now.Year, (currentQuarter - 1) * 3 + 1, 1).AddSeconds(-1);
                    break;
                case TimePeriod.CurrentQuarter:
                    currentQuarter = (now.Month - 1) / 3 + 1;
                    startTime = new DateTime(now.Year, (currentQuarter - 1) * 3 + 1, 1);
                    break;
                case TimePeriod.Last3Days:
                    startTime = now.AddDays(-3);
                    endTime = null;
                    break;
                case TimePeriod.Last7Days:
                    startTime = now.AddDays(-7);
                    endTime = null;
                    break;
                case TimePeriod.Last14Days:
                    startTime = now.AddDays(-14);
                    endTime = null;
                    break;
                case TimePeriod.TillLastMonday:
                    startTime = now.AddDays(-(int)now.DayOfWeek + (int)DayOfWeek.Monday);
                    if (startTime > now) startTime = startTime.Value.AddDays(-7);
                    startTime = new DateTime(startTime.Value.Year, startTime.Value.Month, startTime.Value.Day, 0, 0, 0);
                    break;
                case TimePeriod.TillLastTuesday:
                    startTime = now.AddDays(-(int)now.DayOfWeek + (int)DayOfWeek.Tuesday);
                    if (startTime > now) startTime = startTime.Value.AddDays(-7);
                    startTime = new DateTime(startTime.Value.Year, startTime.Value.Month, startTime.Value.Day, 0, 0, 0);
                    break;
                case TimePeriod.TillLastWednesday:
                    startTime = now.AddDays(-(int)now.DayOfWeek + (int)DayOfWeek.Wednesday);
                    if (startTime > now) startTime = startTime.Value.AddDays(-7);
                    startTime = new DateTime(startTime.Value.Year, startTime.Value.Month, startTime.Value.Day, 0, 0, 0);
                    break;
                case TimePeriod.TillLastThursday:
                    startTime = now.AddDays(-(int)now.DayOfWeek + (int)DayOfWeek.Thursday);
                    if (startTime > now) startTime = startTime.Value.AddDays(-7);
                    startTime = new DateTime(startTime.Value.Year, startTime.Value.Month, startTime.Value.Day, 0, 0, 0);
                    break;
                case TimePeriod.TillLastFriday:
                    startTime = now.AddDays(-(int)now.DayOfWeek + (int)DayOfWeek.Friday);
                    if (startTime > now) startTime = startTime.Value.AddDays(-7);
                    startTime = new DateTime(startTime.Value.Year, startTime.Value.Month, startTime.Value.Day, 0, 0, 0);
                    break;
                case TimePeriod.TillLastSaturday:
                    startTime = now.AddDays(-(int)now.DayOfWeek + (int)DayOfWeek.Saturday);
                    if (startTime > now) startTime = startTime.Value.AddDays(-7);
                    startTime = new DateTime(startTime.Value.Year, startTime.Value.Month, startTime.Value.Day, 0, 0, 0);
                    break;
                case TimePeriod.TillLastSunday:
                    startTime = now.AddDays(-(int)now.DayOfWeek + (int)DayOfWeek.Sunday);
                    if (startTime > now) startTime = startTime.Value.AddDays(-7);
                    startTime = new DateTime(startTime.Value.Year, startTime.Value.Month, startTime.Value.Day, 0, 0, 0);
                    break;
                case TimePeriod.Last3Hours:
                    startTime = now.AddHours(-3);
                    endTime = null;
                    break;
                case TimePeriod.Last6Hours:
                    startTime = now.AddHours(-6);
                    endTime = null;
                    break;
                case TimePeriod.Last12Hours:
                    startTime = now.AddHours(-12);
                    endTime = null;
                    break;
                case TimePeriod.Today:
                    startTime = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
                    endTime = null;
                    break;
                case TimePeriod.Yesterday:
                    startTime = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0).AddDays(-1);
                    endTime = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0).AddSeconds(-1);
                    break;
                case TimePeriod.Everything:
                    startTime = null;
                    endTime = null;
                    break;
            }

            return (StartTime: startTime, EndTime: endTime);
        }

    }
}
