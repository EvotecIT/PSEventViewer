using System;
using System.Reflection;
using Xunit;

namespace EventViewerX.Tests {
    [Collection("FqdnCache")]
    public class TestFqdnCache {
        [Fact]
        public void GetFqdnCachesResult() {
            if (!OperatingSystem.IsWindows()) return;

            var type = typeof(SearchEvents);
            var field = type.GetField("_fqdn", BindingFlags.NonPublic | BindingFlags.Static);
            var method = type.GetMethod("GetFQDN", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(field);
            Assert.NotNull(method);

            field!.SetValue(null, null);
            var first = (string)method!.Invoke(null, null)!;
            Assert.False(string.IsNullOrEmpty(first));
            Assert.Equal(first, (string)field.GetValue(null)!);

            field.SetValue(null, "cached.example.com");
            var second = (string)method.Invoke(null, null)!;
            Assert.Equal("cached.example.com", second);
            Assert.Equal("cached.example.com", (string)field.GetValue(null)!);

            field.SetValue(null, null);
        }
    }
}
