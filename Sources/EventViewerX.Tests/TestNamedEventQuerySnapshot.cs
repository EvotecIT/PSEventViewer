using System.Net;
using Xunit;

namespace EventViewerX.Tests;

public sealed class TestNamedEventQuerySnapshot {
    [Fact]
    public async Task ReadAsyncFreezesTheQueryBeforeEnumeration() {
        NamedEvents namedEvent =
            Enum.GetValues(typeof(NamedEvents))
                .Cast<NamedEvents>()
                .First();
        var credential =
            new NetworkCredential(
                "original",
                "secret",
                "domain");
        var query = new NamedEventQuery(
            new[] { namedEvent }) {
            SourceEventIds = Array.Empty<int>(),
            Credential = credential,
            MaxConcurrency = 1,
            Enrichment =
                new NamedEventEnrichmentOptions {
                    ResolveDns = false,
                    DnsMaxConcurrency = 1
                }
        };

        IAsyncEnumerable<EventObjectSlim> stream =
            NamedEventEngine.ReadAsync(query);
        query.MaxConcurrency = 0;
        query.SourceEventIds = new[] { -1 };
        credential.UserName = "mutated";
        query.Enrichment.DnsMaxConcurrency = 0;

        int count = 0;
        await foreach (EventObjectSlim _ in stream) {
            count++;
        }

        Assert.Equal(0, count);
    }

    [Fact]
    public void SnapshotClonesMutableCredentialsAndEnrichment() {
        NamedEvents namedEvent =
            Enum.GetValues(typeof(NamedEvents))
                .Cast<NamedEvents>()
                .First();
        var credential =
            new NetworkCredential(
                "original",
                "secret",
                "domain");
        var query = new NamedEventQuery(
            new[] { namedEvent }) {
            Credential = credential,
            Enrichment =
                new NamedEventEnrichmentOptions {
                    ResolveDns = true,
                    DnsMaxConcurrency = 2
                }
        };

        NamedEventQuery snapshot =
            NamedEventQuerySnapshot.Copy(query);
        credential.UserName = "mutated";
        query.Enrichment.DnsMaxConcurrency = 1;

        Assert.Equal(
            "original",
            snapshot.Credential!.UserName);
        Assert.Equal(
            2,
            snapshot.Enrichment!.DnsMaxConcurrency);
    }
}
