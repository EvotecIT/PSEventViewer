using System;
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
        public void NamedDataFilterEscapesSpecialCharacters() {
            var ht = new Hashtable { { "Field", "O'Reilly & Co" } };
            var result = SearchEvents.BuildWinEventFilter(namedDataFilter: [ht], logName: "xx", xpathOnly: true);
            Assert.Equal("*[EventData[Data[@Name='Field'] = 'O&apos;Reilly &amp; Co']]", result);
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

        [Fact]
        public void IdMultipleValuesXmlQuery() {
            var result = SearchEvents.BuildWinEventFilter(id: ["1", "2"], logName: "Log");
            Assert.StartsWith("<QueryList>", result);
            Assert.Contains("(EventID=1) or\n(EventID=2)", result);
        }

        [Fact]
        public void DateRangeFilterXpathOnly() {
            var start = DateTime.Now.AddHours(-1);
            var end = DateTime.Now.AddMinutes(-30);
            var result = SearchEvents.BuildWinEventFilter(startTime: start, endTime: end, logName: "x", xpathOnly: true);
            Assert.Contains("TimeCreated[timediff(@SystemTime) &lt;=", result);
            Assert.Contains("TimeCreated[timediff(@SystemTime) &gt;=", result);
            Assert.DoesNotContain("<QueryList>", result);
        }

        [Fact]
        public void FutureStartTimeIsClamped() {
            var start = DateTime.Now.AddMinutes(10);
            var result = SearchEvents.BuildWinEventFilter(startTime: start, logName: "x", xpathOnly: true);
            Assert.Contains("timediff(@SystemTime) &lt;= 0", result);
        }

        [Fact]
        public void FutureEndTimeIsClamped() {
            var end = DateTime.Now.AddMinutes(5);
            var result = SearchEvents.BuildWinEventFilter(endTime: end, logName: "x", xpathOnly: true);
            Assert.Contains("timediff(@SystemTime) &gt;= 0", result);
        }
    }
}