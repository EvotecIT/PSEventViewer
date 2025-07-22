using Xunit;
using EventViewerX;
using System.Linq;

namespace EventViewerX.Tests {
    public class TestDynamicLinq {
        [Fact]
        public void QueryLogWithDynamicExpressionFiltersResults() {
            if (!OperatingSystem.IsWindows()) return;
            var events = SearchEvents.QueryLog(KnownLog.System, new List<int> { 6005, 6006 }, linqExpression: "Id == 6005").ToList();
            Assert.True(events.All(e => e.Id == 6005));
        }
    }
}
