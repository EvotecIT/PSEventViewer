using System;
using Xunit;

namespace EventViewerX.Tests {
    public class TestTimeHelper {
        [Fact]
        public void PastHourRangeIsLocal() {
            var now = DateTime.Now;
            var expectedStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddHours(-1);
            var expectedEnd = expectedStart.AddHours(1);

            var result = TimeHelper.GetTimePeriod(TimePeriod.PastHour);

            Assert.Equal(expectedStart, result.StartTime);
            Assert.Equal(expectedEnd, result.EndTime);
        }

        [Fact]
        public void LastSevenDaysStartsFromLocal() {
            var now = DateTime.Now;
            var expectedStart = now.Date.AddDays(-7);

            var result = TimeHelper.GetTimePeriod(TimePeriod.Last7Days);

            Assert.Equal(expectedStart, result.StartTime);
            Assert.Null(result.EndTime);
        }

        [Fact]
        public void TodayRangeMatchesLocalDay() {
            var now = DateTime.Now;
            var expectedStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
            var expectedEnd = expectedStart.AddDays(1);

            var result = TimeHelper.GetTimePeriod(TimePeriod.Today);

            Assert.Equal(expectedStart, result.StartTime);
            Assert.Equal(expectedEnd, result.EndTime);
        }
    }
}
