using System.Net;
using Xunit;

namespace EventViewerX.Tests;

public sealed class TestNamedEventQuerySnapshot {
    [Fact]
    public void QueryRejectsUndefinedNamedEventValues() {
        Assert.Throws<
            ArgumentOutOfRangeException>(() =>
                new NamedEventQuery(
                    new[] {
                        (NamedEvents)int.MaxValue
                    }));
    }

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
            MachineNames = new[] { "remote.contoso.test" },
            Credential = credential,
            MaxConcurrency = 1,
            Enrichment =
                new NamedEventEnrichmentOptions {
                    ResolveDns = false,
                    DnsMaxConcurrency = 1
                }
        };

        IAsyncEnumerable<NamedEventRecord> stream =
            NamedEventEngine.ReadAsync(query);
        query.MaxConcurrency = 0;
        query.SourceEventIds = new[] { -1 };
        credential.UserName = "mutated";
        query.Enrichment.DnsMaxConcurrency = 0;

        int count = 0;
        await foreach (NamedEventRecord _ in stream) {
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

    [Fact]
    public void ReadAsyncValidatesRemoteOptionsBeforeEnumeration() {
        NamedEvents namedEvent =
            Enum.GetValues(typeof(NamedEvents))
                .Cast<NamedEvents>()
                .First();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NamedEventEngine.ReadAsync(
                new NamedEventQuery(
                    new[] { namedEvent }) {
                    Authentication =
                        (EventLogAuthentication)int.MaxValue
                }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NamedEventEngine.ReadAsync(
                new NamedEventQuery(
                    new[] { namedEvent }) {
                    RemoteConnectionTimeoutMilliseconds =
                        0
                }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NamedEventEngine.ReadAsync(
                new NamedEventQuery(
                    new[] { namedEvent }) {
                    RemoteReadTimeoutMilliseconds =
                        -1
                }));
    }
}
