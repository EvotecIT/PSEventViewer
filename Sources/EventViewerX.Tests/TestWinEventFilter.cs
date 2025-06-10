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
    }
}
