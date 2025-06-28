using System.Collections;
using Xunit;

namespace EventViewerX.Tests {
    public class TestWinEventFilter {
        [Fact]
        public void NamedDataFilterSingleValue() {
            var ht = new Hashtable { { "FieldName", "Value1" } };
            var result = SearchEvents.BuildWinEventFilter(namedDataFilter: [ht], logName: "xx", xpathOnly: true);
            Assert.Equal("*[EventData[Data[@Name='FieldName'] = 'Value1']]", result);
        }

        [Fact]
        public void NamedDataFilterTwoValues() {
            var ht = new Hashtable { { "FieldName", new[] { "Value1", "Value2" } } };
            var result = SearchEvents.BuildWinEventFilter(namedDataFilter: [ht], logName: "xx", xpathOnly: true);
            Assert.Equal("*[EventData[Data[@Name='FieldName'] = 'Value1' or Data[@Name='FieldName'] = 'Value2']]", result);
        }

        [Fact]
        public void NamedDataExcludeFilterSingleValue() {
            var ht = new Hashtable { { "FieldName", "Value1" } };
            var result = SearchEvents.BuildWinEventFilter(namedDataExcludeFilter: [ht], logName: "xx", xpathOnly: true);
            Assert.Equal("*[EventData[Data[@Name='FieldName'] != 'Value1']]", result);
        }

        [Fact]
        public void NamedDataExcludeFilterTwoValues() {
            var ht = new Hashtable { { "FieldName", new[] { "Value1", "Value2" } } };
            var result = SearchEvents.BuildWinEventFilter(namedDataExcludeFilter: [ht], logName: "xx", xpathOnly: true);
            Assert.Equal("*[EventData[Data[@Name='FieldName'] != 'Value1' and Data[@Name='FieldName'] != 'Value2']]", result);
        }
        [Fact]
        public void PathQueryUsesFilePrefix() {
            var ht = new Hashtable { { "param4", "BITS" } };
            var result = SearchEvents.BuildWinEventFilter(id: ["7040"], namedDataExcludeFilter: [ht], path: "C:/file.evtx");
            Assert.Contains("Path=\"file://C:/file.evtx\"", result);
            Assert.DoesNotContain("Select Path", result);
        }

        [Fact]
        public void LogNameQueryAddsSelectPath() {
            var ht = new Hashtable { { "param4", "BITS" } };
            var result = SearchEvents.BuildWinEventFilter(id: ["7040"], namedDataExcludeFilter: [ht], logName: "System");
            Assert.Contains("Path=\"System\"", result);
            Assert.Contains("Select Path=\"System\"", result);
        }

        [Fact]
        public void IdMultipleValuesOr() {
            var result = SearchEvents.BuildWinEventFilter(id: ["1", "2"], logName: "x", xpathOnly: true);
            Assert.Equal("*[System[(EventID=1) or (EventID=2)]]", result);
        }
    }
}