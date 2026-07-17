using System;
using System.Collections;
using System.Linq;
using System.Xml.Linq;
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
            Assert.Equal("*[EventData[Data[@Name='Field'] = \"O'Reilly & Co\"]]", result);
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
        public void MultipleExcludedIdsAreJoinedWithAnd() {
            var result = SearchEvents.BuildWinEventFilter(excludeId: ["1", "2"], xpathOnly: true);

            Assert.Equal("*[System[(EventID!=1) and (EventID!=2)]]", result);
        }

        [Fact]
        public void DateRangeFilterXpathOnly() {
            var start = new DateTime(2026, 7, 17, 10, 0, 0, DateTimeKind.Utc);
            var end = new DateTime(2026, 7, 17, 10, 30, 0, DateTimeKind.Utc);
            var result = SearchEvents.BuildWinEventFilter(startTime: start, endTime: end, logName: "x", xpathOnly: true);
            Assert.Contains("TimeCreated[@SystemTime>='2026-07-17T10:00:00.0000000Z']", result);
            Assert.Contains("TimeCreated[@SystemTime<='2026-07-17T10:30:00.0000000Z']", result);
            Assert.DoesNotContain("<QueryList>", result);
        }

        [Fact]
        public void FutureStartTimeRemainsAnAbsoluteBoundary() {
            var start = new DateTime(2030, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            var result = SearchEvents.BuildWinEventFilter(startTime: start, logName: "x", xpathOnly: true);
            Assert.Contains("@SystemTime>='2030-01-02T03:04:05.0000000Z'", result);
            Assert.DoesNotContain("timediff", result);
        }

        [Fact]
        public void FutureEndTimeRemainsAnAbsoluteBoundary() {
            var end = new DateTime(2030, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            var result = SearchEvents.BuildWinEventFilter(endTime: end, logName: "x", xpathOnly: true);
            Assert.Contains("@SystemTime<='2030-01-02T03:04:05.0000000Z'", result);
            Assert.DoesNotContain("timediff", result);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("-1")]
        [InlineData("1 or EventID=2")]
        [InlineData("")]
        public void InvalidEventIdIsRejectedBeforeBuildingXpath(string value) {
            Assert.Throws<ArgumentException>(() => SearchEvents.BuildWinEventFilter(id: [value], xpathOnly: true));
        }

        [Fact]
        public void ProviderNameEscapesSpecialCharactersWinFilter() {
            var result = SearchEvents.BuildWinEventFilter(providerName: ["O'Reilly & Co"], logName: "x", xpathOnly: true);
            Assert.Equal("*[System[Provider[@Name=\"O'Reilly & Co\"]]]", result);
        }

        [Fact]
        public void QueryListXmlEncodesRawXpathAndPreservesItsMeaning() {
            var result = SearchEvents.BuildWinEventFilter(
                providerName: ["O'Reilly & Co"],
                startTime: new DateTime(2026, 7, 17, 10, 0, 0, DateTimeKind.Utc),
                logName: "Company & Product/Log");

            XDocument document = XDocument.Parse(result);
            XElement select = document.Root!.Element("Query")!.Element("Select")!;

            Assert.Equal("Company & Product/Log", select.Attribute("Path")!.Value);
            Assert.Contains("Provider[@Name=\"O'Reilly & Co\"]", select.Value);
            Assert.Contains("@SystemTime>='2026-07-17T10:00:00.0000000Z'", select.Value);
        }

        [Fact]
        public void ValuesContainingBothQuoteKindsAreRejected() {
            Assert.Throws<ArgumentException>(() => SearchEvents.BuildWinEventFilter(
                providerName: ["Provider 'quoted' as \"other\""],
                xpathOnly: true));
        }

        [Fact]
        public void NullFiltersReturnWildcard() {
            var result = SearchEvents.BuildWinEventFilter(xpathOnly: true);
            Assert.Equal("*", result);
        }

        [Fact]
        public void MinimumRecordIdIsEmittedAsNativeExclusiveBoundary() {
            string result = SearchEvents.BuildWinEventFilter(
                minimumEventRecordIdExclusive: 123,
                xpathOnly: true);

            Assert.Equal("*[System[EventRecordID>123]]", result);
        }

        [Fact]
        public void FilterRejectsMoreThanTheNativeXpathExpressionBudget() {
            var namedData = new Hashtable {
                { "Correlation", Enumerable.Range(1, SearchEvents.MaxXPathExpressionCount + 1).Select(static value => value.ToString()).ToArray() }
            };

            ArgumentException exception = Assert.Throws<ArgumentException>(() => SearchEvents.BuildWinEventFilter(
                namedDataFilter: [namedData],
                xpathOnly: true));

            Assert.Contains(SearchEvents.MaxXPathExpressionCount.ToString(), exception.Message);
        }

        [Fact]
        public void NamedDataBudgetCountsBothNameAndValueComparisons() {
            var withinBudget = new Hashtable {
                { "Correlation", Enumerable.Range(1, SearchEvents.MaxXPathExpressionCount / 2).Select(static value => value.ToString()).ToArray() }
            };
            var overBudget = new Hashtable {
                { "Correlation", Enumerable.Range(1, SearchEvents.MaxXPathExpressionCount / 2 + 1).Select(static value => value.ToString()).ToArray() }
            };

            string xpath = SearchEvents.BuildWinEventFilter(namedDataFilter: [withinBudget], xpathOnly: true);
            Assert.NotEmpty(xpath);
            Assert.Throws<ArgumentException>(() => SearchEvents.BuildWinEventFilter(
                namedDataFilter: [overBudget],
                xpathOnly: true));
        }
    }
}
