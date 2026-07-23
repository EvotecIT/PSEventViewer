using DnsClientX;
using EventViewerX.Rules.ActiveDirectory;
using System.Runtime.CompilerServices;
using Xunit;

namespace EventViewerX.Tests;

public class TestNamedEventDnsEnrichment {
    [Fact]
    public void SmbProjectionDoesNotPerformDnsInItsConstructor() {
        SMBServerAudit projected = CreateSmbEvent("\\\\10.0.0.5");

        Assert.Equal(ReverseDnsResolutionStatus.NotRequested, projected.ClientDnsResolutionStatus);
        Assert.Equal(string.Empty, projected.ClientDNSName);
    }

    [Fact]
    public async Task SmbEnrichmentUsesOneNormalizedPtrQueryAndRetainsTheEvent() {
        SMBServerAudit projected = CreateSmbEvent("\\\\10.0.0.5");
        int calls = 0;
        string? queriedAddress = null;
        using var enricher = new NamedEventEnricher(
            new NamedEventEnrichmentOptions { ResolveDns = true },
            (address, _) => {
                calls++;
                queriedAddress = address;
                return Task.FromResult(new DnsResponse {
                    Status = DnsResponseCode.NoError,
                    Answers = new[] {
                        new DnsAnswer { Type = DnsRecordType.PTR, DataRaw = "client.ad.evotec.xyz." },
                        new DnsAnswer { Type = DnsRecordType.PTR, DataRaw = "CLIENT.ad.evotec.xyz." }
                    }
                });
            });

        await enricher.EnrichAsync(projected, CancellationToken.None);

        Assert.Equal(1, calls);
        Assert.Equal("10.0.0.5", queriedAddress);
        Assert.Equal("client.ad.evotec.xyz", projected.ClientDNSName, ignoreCase: true);
        Assert.Equal(ReverseDnsResolutionStatus.Resolved, projected.ClientDnsResolutionStatus);
        Assert.Equal(string.Empty, projected.ClientDnsResolutionError);
    }

    [Fact]
    public async Task ExistingHostNameDoesNotIssueAPtrQuery() {
        SMBServerAudit projected = CreateSmbEvent("client.ad.evotec.xyz.");
        int calls = 0;
        using var enricher = new NamedEventEnricher(
            new NamedEventEnrichmentOptions { ResolveDns = true },
            (_, _) => {
                calls++;
                return Task.FromResult(new DnsResponse());
            });

        await enricher.EnrichAsync(projected, CancellationToken.None);

        Assert.Equal(0, calls);
        Assert.Equal("client.ad.evotec.xyz", projected.ClientDNSName);
        Assert.Equal(ReverseDnsResolutionStatus.AlreadyNamed, projected.ClientDnsResolutionStatus);
    }

    [Fact]
    public async Task DnsTimeoutIsReportedWithoutDroppingTheProjectedEvent() {
        SMBServerAudit projected = CreateSmbEvent("10.0.0.6");
        using var enricher = new NamedEventEnricher(
            new NamedEventEnrichmentOptions { ResolveDns = true },
            (_, _) => Task.FromException<DnsResponse>(new TimeoutException("PTR lookup timed out.")));

        await enricher.EnrichAsync(projected, CancellationToken.None);

        Assert.Equal(ReverseDnsResolutionStatus.TimedOut, projected.ClientDnsResolutionStatus);
        Assert.Equal("PTR lookup timed out.", projected.ClientDnsResolutionError);
        Assert.Equal(string.Empty, projected.ClientDNSName);
    }

    [Fact]
    public async Task ResolverCancellationIsReportedAsTimeoutWhenCallerDidNotCancel() {
        SMBServerAudit projected = CreateSmbEvent("10.0.0.8");
        using var resolverTimeout = new CancellationTokenSource();
        resolverTimeout.Cancel();
        using var enricher = new NamedEventEnricher(
            new NamedEventEnrichmentOptions { ResolveDns = true },
            (_, _) => Task.FromCanceled<DnsResponse>(resolverTimeout.Token));

        await enricher.EnrichAsync(projected, CancellationToken.None);

        Assert.Equal(ReverseDnsResolutionStatus.TimedOut, projected.ClientDnsResolutionStatus);
        Assert.NotEmpty(projected.ClientDnsResolutionError);
        Assert.Equal(string.Empty, projected.ClientDNSName);
    }

    [Fact]
    public async Task ResolverFailureIsVisibleOnTheEvent() {
        SMBServerAudit projected = CreateSmbEvent("10.0.0.9");
        using var enricher = new NamedEventEnricher(
            new NamedEventEnrichmentOptions { ResolveDns = true },
            (_, _) => throw new InvalidOperationException("No usable system resolver."));

        await enricher.EnrichAsync(projected, CancellationToken.None);

        Assert.Equal(ReverseDnsResolutionStatus.Failed, projected.ClientDnsResolutionStatus);
        Assert.Equal("No usable system resolver.", projected.ClientDnsResolutionError);
    }

    [Fact]
    public async Task FailedResponseAfterCallerCancellationDoesNotCrossTheCheckpointBoundary() {
        SMBServerAudit projected = CreateSmbEvent("10.0.0.10");
        using var cancellation = new CancellationTokenSource();
        using var enricher = new NamedEventEnricher(
            new NamedEventEnrichmentOptions { ResolveDns = true },
            (_, _) => {
                cancellation.Cancel();
                return Task.FromResult(new DnsResponse {
                    Status = DnsResponseCode.ServerFailure,
                    Error = "Dependency converted cancellation into a response."
                });
            });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => enricher.EnrichAsync(projected, cancellation.Token));

        Assert.Equal(ReverseDnsResolutionStatus.Cancelled, projected.ClientDnsResolutionStatus);
    }

    [Fact]
    public async Task DnsRequestsAreDeduplicatedAndConcurrencyIsBounded() {
        int calls = 0;
        int active = 0;
        int maximumActive = 0;
        using var enricher = new NamedEventEnricher(
            new NamedEventEnrichmentOptions {
                ResolveDns = true,
                DnsMaxConcurrency = 2,
                DnsTimeoutMilliseconds = 1000
            },
            async (address, token) => {
                Interlocked.Increment(ref calls);
                int nowActive = Interlocked.Increment(ref active);
                int observedMaximum;
                do {
                    observedMaximum = Volatile.Read(ref maximumActive);
                } while (nowActive > observedMaximum &&
                         Interlocked.CompareExchange(ref maximumActive, nowActive, observedMaximum) != observedMaximum);
                try {
                    await Task.Delay(40, token);
                    return new DnsResponse {
                        Status = DnsResponseCode.NoError,
                        Answers = new[] {
                            new DnsAnswer { Type = DnsRecordType.PTR, DataRaw = address + ".example.test." }
                        }
                    };
                } finally {
                    Interlocked.Decrement(ref active);
                }
            });
        SMBServerAudit[] projected = {
            CreateSmbEvent("10.0.0.11"),
            CreateSmbEvent("10.0.0.12"),
            CreateSmbEvent("10.0.0.13"),
            CreateSmbEvent("10.0.0.11")
        };

        await Task.WhenAll(projected.Select(item => enricher.EnrichAsync(item, CancellationToken.None)));

        Assert.Equal(3, calls);
        Assert.Equal(2, maximumActive);
        Assert.All(projected, item => Assert.Equal(ReverseDnsResolutionStatus.Resolved, item.ClientDnsResolutionStatus));
    }

    [Fact]
    public async Task ConfiguredTimeoutBoundsTheWholeResolverCall() {
        SMBServerAudit projected = CreateSmbEvent("10.0.0.14");
        using var enricher = new NamedEventEnricher(
            new NamedEventEnrichmentOptions {
                ResolveDns = true,
                DnsTimeoutMilliseconds = 30
            },
            async (_, token) => {
                await Task.Delay(TimeSpan.FromSeconds(5), token);
                return new DnsResponse();
            });
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        await enricher.EnrichAsync(projected, CancellationToken.None);

        stopwatch.Stop();
        Assert.Equal(ReverseDnsResolutionStatus.TimedOut, projected.ClientDnsResolutionStatus);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(1), $"Elapsed {stopwatch.Elapsed}.");
    }

    [Fact]
    public async Task ConcurrentEnrichmentPreservesSourceAndCheckpointOrder() {
        var observedRecordIds = new List<long?>();
        var projectedRecordIds = new List<long?>();
        EventObject[] sourceEvents = {
            CreateSmbEventObject("10.0.0.21", 21),
            CreateSmbEventObject("10.0.0.22", 22),
            CreateSmbEventObject("10.0.0.23", 23)
        };
        using var enricher = new NamedEventEnricher(
            new NamedEventEnrichmentOptions {
                ResolveDns = true,
                DnsMaxConcurrency = 3,
                DnsTimeoutMilliseconds = 1000
            },
            async (address, token) => {
                int lastOctet = int.Parse(address.Substring(address.LastIndexOf('.') + 1));
                await Task.Delay((24 - lastOctet) * 20, token);
                return new DnsResponse { Status = DnsResponseCode.NoError };
            });

        await foreach (SearchEvents.NamedEventProjection projection in SearchEvents.ProjectNamedCandidatesInOrderAsync(
                           YieldEventsAsync(sourceEvents),
                           new List<NamedEvents> { NamedEvents.ADSMBServerAuditV1 },
                           enricher,
                           () => true,
                           source => observedRecordIds.Add(source.RecordId),
                           CancellationToken.None)) {
            projectedRecordIds.Add(projection.Source.RecordId);
        }

        Assert.Equal(new long?[] { 21, 22, 23 }, projectedRecordIds);
        Assert.Equal(projectedRecordIds, observedRecordIds);
    }

    [Fact]
    public async Task DependencySwallowedCancellationNeverReachesCheckpointObserver() {
        var observedRecordIds = new List<long?>();
        using var cancellation = new CancellationTokenSource();
        using var enricher = new NamedEventEnricher(
            new NamedEventEnrichmentOptions { ResolveDns = true },
            (_, _) => {
                cancellation.Cancel();
                return Task.FromResult(new DnsResponse {
                    Status = DnsResponseCode.ServerFailure,
                    Error = "Dependency converted cancellation into a response."
                });
            });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => {
            await foreach (SearchEvents.NamedEventProjection _ in SearchEvents.ProjectNamedCandidatesInOrderAsync(
                               YieldEventsAsync(new[] { CreateSmbEventObject("10.0.0.24", 24) }),
                               new List<NamedEvents> { NamedEvents.ADSMBServerAuditV1 },
                               enricher,
                               () => true,
                               source => observedRecordIds.Add(source.RecordId),
                               cancellation.Token)) {
            }
        });

        Assert.Empty(observedRecordIds);
    }

    [Fact]
    public void ParameterlessReverseDnsMethodRemainsAvailableForBinaryCompatibility() {
        Type type = typeof(SMBServerAudit);

        Assert.NotNull(type.GetMethod(nameof(SMBServerAudit.ResolveClientDnsNameAsync), Type.EmptyTypes));
        Assert.NotNull(type.GetMethod(
            nameof(SMBServerAudit.ResolveClientDnsNameAsync),
            new[] { typeof(CancellationToken) }));
    }

    [Fact]
    public async Task CallerCancellationStillStopsEnrichment() {
        SMBServerAudit projected = CreateSmbEvent("10.0.0.7");
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        using var enricher = new NamedEventEnricher(
            new NamedEventEnrichmentOptions { ResolveDns = true },
            (_, token) => Task.FromCanceled<DnsResponse>(token));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => enricher.EnrichAsync(projected, cts.Token));
        Assert.Equal(ReverseDnsResolutionStatus.Cancelled, projected.ClientDnsResolutionStatus);
    }

    [Fact]
    public void ProjectionFailureIsReportedInsteadOfSilentlyReturningNull() {
        EventObject eventObject = CreateEventObject(
            eventId: 4713,
            logName: "Security",
            providerName: "Microsoft-Windows-Security-Auditing",
            data: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                ["KerberosPolicyChange"] = "KerProxy:0xFFFFFFFFFFFFFFFFFFFFFFFF"
            });

        EventRuleProjectionException exception = Assert.Throws<EventRuleProjectionException>(() =>
            EventObjectSlim.CreateEventRule(eventObject, new List<NamedEvents> { NamedEvents.KerberosPolicyChange }));

        Assert.Equal(4713, exception.EventId);
        Assert.Equal(42L, exception.RecordId);
        Assert.Equal("Security", exception.LogName);
        Assert.Contains(exception.RuleNames, name => name.Contains("KerberosPolicyChange", StringComparison.Ordinal));
        Assert.Contains("projection failed", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static SMBServerAudit CreateSmbEvent(string clientAddress) {
        return new SMBServerAudit(CreateSmbEventObject(clientAddress, 42));
    }

    private static EventObject CreateSmbEventObject(string clientAddress, long recordId) {
        return CreateEventObject(
            eventId: 3000,
            logName: "Microsoft-Windows-SMBServer/Audit",
            providerName: "Microsoft-Windows-SMBServer",
            data: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                ["ClientName"] = clientAddress
            },
            recordId: recordId);
    }

    private static EventObject CreateEventObject(
        int eventId,
        string logName,
        string providerName,
        Dictionary<string, string> data,
        long recordId = 42) {
        var eventObject = (EventObject)RuntimeHelpers.GetUninitializedObject(typeof(EventObject));
        SetSnapshotProperty(eventObject, nameof(EventObject.RecordId), (long?)recordId);
        SetSnapshotProperty(eventObject, nameof(EventObject.TimeCreated), DateTime.UtcNow);
        SetSnapshotProperty(eventObject, nameof(EventObject.Id), eventId);
        SetSnapshotProperty(eventObject, nameof(EventObject.LogName), logName);
        SetSnapshotProperty(eventObject, nameof(EventObject.ProviderName), providerName);
        SetSnapshotProperty(eventObject, nameof(EventObject.MachineName), "test-machine");
        foreach (KeyValuePair<string, string> field in data) {
            eventObject.Data[field.Key] = field.Value;
        }
        eventObject.ContainerLog = logName;
        eventObject.QueriedMachine = "test-machine";
        eventObject.MessageSubject = "SMB audit";
        return eventObject;
    }

    private static async IAsyncEnumerable<EventObject> YieldEventsAsync(IEnumerable<EventObject> events) {
        foreach (EventObject eventObject in events) {
            yield return eventObject;
            await Task.Yield();
        }
    }

    private static void SetSnapshotProperty<T>(EventObject eventObject, string propertyName, T value) {
        typeof(EventObject)
            .GetField($"<{propertyName}>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(eventObject, value);
    }
}
