using System.Reflection;
using Xunit;

namespace EventViewerX.Tests;

public class TestProviderNormalization {
    [Fact]
    public void TrimmedProviderNameReturned() {
        var method = typeof(SearchEvents).GetMethod("NormalizeProviderName", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        string result = (string)method!.Invoke(null, new object?[]{"  TestProvider  "})!;
        Assert.Equal("TestProvider", result);
    }
}
