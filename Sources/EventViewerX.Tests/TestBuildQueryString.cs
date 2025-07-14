using System;
using System.Reflection;
using System.Linq;
using Xunit;

namespace EventViewerX.Tests
{

    /// <summary>
    /// Unit tests covering query string generation helpers.
    /// </summary>
    public class TestBuildQueryString {
        [Fact]
        public void ProviderNameEscapesSpecialCharacters() {
            var method = typeof(SearchEvents).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .First(m => m.Name == "BuildQueryString" && m.GetParameters().Length == 11);
            string result = (string)method.Invoke(null, new object?[]{"Log", null, "O'Reilly & Co", null, null, null, null, null, null, null, null});
            Assert.Contains("Provider[@Name='O&apos;Reilly &amp; Co']", result);
        }
    }
}

