using System;
using System.Reflection;
using System.Linq;
using Xunit;

namespace EventViewerX.Tests {
    public class TestBuildQueryString {
        [Fact]
        public void ProviderNameEscapesSpecialCharacters() {
            var method = typeof(SearchEvents).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .First(m => m.Name == "BuildQueryString" && m.GetParameters().Length == 11);
            string result = (string?)method.Invoke(null, new object?[]{"Log", null, "O'Reilly & Co", null, null, null, null, null, null, null, null}) ?? string.Empty;
            Assert.Contains("Provider[@Name='O&apos;Reilly &amp; Co']", result);
        }

        [Fact]
        public void EventIdMultipleValuesOr() {
            var method = typeof(SearchEvents).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .First(m => m.Name == "BuildQueryString" && m.GetParameters().Length == 11);
            string result = (string?)method.Invoke(null, new object?[]{"Log", new System.Collections.Generic.List<int>{1, 2}, null, null, null, null, null, null, null, null, null}) ?? string.Empty;
            Assert.Contains("(EventID=1) or (EventID=2)", result);
        }
    }
}

