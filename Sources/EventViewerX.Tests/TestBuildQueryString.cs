using System;
using System.Reflection;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace EventViewerX.Tests {
    public class TestBuildQueryString {
        [Fact]
        public void ProviderNameEscapesSpecialCharacters() {
            var method = typeof(SearchEvents).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .First(m => m.Name == "BuildQueryString" && m.GetParameters().Length == 14);
            string result = Assert.IsType<string>(method.Invoke(null, new object?[]{"Log", null, "O'Reilly & Co", null, null, null, null, null, null, null, null, null, null, null}));
            Assert.Contains("Provider[@Name=&quot;O&apos;Reilly &amp; Co&quot;]", result);
            Assert.Contains("Provider[@Name=\"O'Reilly & Co\"]", XDocument.Parse(result).Root!.Value);
        }

        [Fact]
        public void LogNameIsXmlEscapedWithoutChangingTheEventLogPath() {
            var method = typeof(SearchEvents).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .First(m => m.Name == "BuildQueryString" && m.GetParameters().Length == 14);

            string result = Assert.IsType<string>(method.Invoke(null, new object?[] { "Company & Product/Log", null, null, null, null, null, null, null, null, null, null, null, null, null }));
            XDocument document = XDocument.Parse(result);

            Assert.Equal("Company & Product/Log", document.Root!.Element("Query")!.Attribute("Path")!.Value);
            Assert.Equal("Company & Product/Log", document.Root.Element("Query")!.Element("Select")!.Attribute("Path")!.Value);
        }

        [Fact]
        public void InvalidUserIdentifierIsRejectedBeforeBuildingQuery() {
            var method = typeof(SearchEvents).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .First(m => m.Name == "BuildQueryString" && m.GetParameters().Length == 14);

            var exception = Assert.Throws<TargetInvocationException>(() => method.Invoke(
                null,
                new object?[] { "Security", null, null, null, null, null, null, "not-a-valid-account-or-sid", null, null, null, null, null, null }));

            Assert.IsType<ArgumentException>(exception.InnerException);
        }

        [Fact]
        public void EventIdMultipleValuesOr() {
            var method = typeof(SearchEvents).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .First(m => m.Name == "BuildQueryString" && m.GetParameters().Length == 14);
            string result = (string)method.Invoke(null, new object?[]{"Log", new System.Collections.Generic.List<int>{1, 2}, null, null, null, null, null, null, null, null, null, null, null, null});
            Assert.Contains("(EventID=1) or (EventID=2)", result);
        }

        [Fact]
        public void RecordIdsCombineWithOtherFiltersInsteadOfReplacingThem() {
            var method = typeof(SearchEvents).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .First(m => m.Name == "BuildQueryString" && m.GetParameters().Length == 14);

            string result = Assert.IsType<string>(method.Invoke(null, new object?[] {
                "System",
                new System.Collections.Generic.List<int> { 100, 200 },
                "Provider",
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                new System.Collections.Generic.List<long> { 10, 20 },
                null,
                null
            }));

            Assert.Contains("(EventID=100) or (EventID=200)", result);
            Assert.Contains("(EventRecordID=10) or (EventRecordID=20)", result);
            Assert.Contains("Provider[@Name=&apos;Provider&apos;]", result);
        }

        [Fact]
        public void LocalTimeIsConvertedToUtcBeforeAppendingZuluSuffix() {
            var method = typeof(SearchEvents).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .First(m => m.Name == "BuildQueryString" && m.GetParameters().Length == 14);
            var localTime = new DateTime(2026, 1, 15, 12, 30, 45, DateTimeKind.Local);

            string result = (string)method.Invoke(null, new object?[] { "Log", null, null, null, null, localTime, null, null, null, null, null, null, null, null });

            Assert.Contains(localTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"), result);
        }

        [Fact]
        public void MaximumRecordIdBoundaryIsXmlEncodedAndParsedAsXpath() {
            var method = typeof(SearchEvents).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .First(m => m.Name == "BuildQueryString" && m.GetParameters().Length == 14);

            string result = Assert.IsType<string>(method.Invoke(null, new object?[] {
                "System", null, null, null, null, null, null, null, null, null, null, null, null, 123L
            }));

            Assert.Contains("EventRecordID&lt;123", result);
            Assert.Contains("EventRecordID<123", XDocument.Parse(result).Root!.Value);
        }
    }
}
