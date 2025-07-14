using System;
using System.Diagnostics;
using Xunit;

namespace EventViewerX.Tests
{

    /// <summary>
    /// Unit tests for event writing helpers.
    /// </summary>
    public class TestWriteEvent {
        [Fact]
        public void InvalidCategoryThrows() {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SearchEvents.WriteEvent("TestSource", "Application", "Test", EventLogEntryType.Information, short.MaxValue + 1, 1, null)
            );
        }
    }
}
