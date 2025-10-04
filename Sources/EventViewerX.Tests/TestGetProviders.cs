using System.Linq;
using Xunit;

namespace EventViewerX.Tests;

public class TestGetProviders
{
    [Fact]
    public void ProvidersMetadataEnumerates()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var provider = SearchEvents.GetProviders().FirstOrDefault();
        Assert.NotNull(provider);
        Assert.NotNull(provider!.ProviderName);
        Assert.NotNull(provider.Events);

        foreach (var evt in provider.Events)
        {
            Assert.NotNull(evt);
            break;
        }
    }
}
