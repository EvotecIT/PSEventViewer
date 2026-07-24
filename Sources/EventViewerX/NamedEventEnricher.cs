using EventViewerX.Rules.ActiveDirectory;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace EventViewerX;

/// <summary>
/// Applies optional, failure-isolated enrichment to projected named events.
/// </summary>
internal sealed class NamedEventEnricher : IDisposable {
    private readonly NamedEventEnrichmentOptions _options;
    private readonly Func<string, CancellationToken, Task<DnsResponse>>? _rawDnsResolver;
    private readonly ConcurrentDictionary<string, Lazy<Task<DnsResponse>>> _pendingDnsRequests =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim? _dnsConcurrency;

    internal NamedEventEnricher(
        NamedEventEnrichmentOptions options,
        Func<string, CancellationToken, Task<DnsResponse>>? dnsResolver = null) {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();

        if (!_options.ResolveDns) {
            return;
        }

        _dnsConcurrency = new SemaphoreSlim(_options.DnsMaxConcurrency, _options.DnsMaxConcurrency);
        _rawDnsResolver = dnsResolver;
    }

    /// <summary>
    /// Gets the maximum number of projected events that may be enriched as one ordered batch.
    /// </summary>
    internal int MaxConcurrency => _options.ResolveDns ? _options.DnsMaxConcurrency : 1;

    /// <summary>
    /// Enriches a projected event without converting lookup failures into missing event records.
    /// </summary>
    internal async Task EnrichAsync(EventObjectSlim eventObject, CancellationToken cancellationToken) {
        if (!_options.ResolveDns || _dnsConcurrency == null) {
            cancellationToken.ThrowIfCancellationRequested();
            return;
        }

        if (eventObject is SMBServerAudit smbServerAudit) {
            await smbServerAudit.ResolveClientDnsNameAsync(ResolveDnsAsync, cancellationToken).ConfigureAwait(false);
        }
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task<DnsResponse> ResolveDnsAsync(string address, CancellationToken cancellationToken) {
        Lazy<Task<DnsResponse>> request = _pendingDnsRequests.GetOrAdd(
            address,
            key => new Lazy<Task<DnsResponse>>(
                () => RunDnsQueryAsync(key, cancellationToken),
                LazyThreadSafetyMode.ExecutionAndPublication));
        try {
            return await request.Value.ConfigureAwait(false);
        } finally {
            ((ICollection<KeyValuePair<string, Lazy<Task<DnsResponse>>>>)_pendingDnsRequests)
                .Remove(new KeyValuePair<string, Lazy<Task<DnsResponse>>>(address, request));
        }
    }

    private async Task<DnsResponse> RunDnsQueryAsync(string address, CancellationToken cancellationToken) {
        bool entered = false;
        CancellationTokenSource? lookupCancellation = null;
        try {
            await _dnsConcurrency!.WaitAsync(cancellationToken).ConfigureAwait(false);
            entered = true;
            lookupCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            lookupCancellation.CancelAfter(_options.DnsTimeoutMilliseconds);
            DnsResponse response = _rawDnsResolver == null
                ? await ResolveSystemDnsAsync(
                    address,
                    lookupCancellation.Token).ConfigureAwait(false)
                : await _rawDnsResolver(address, lookupCancellation.Token).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            if (lookupCancellation.IsCancellationRequested) {
                throw new TimeoutException($"The reverse-DNS lookup exceeded {_options.DnsTimeoutMilliseconds} ms.");
            }
            return response;
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw new OperationCanceledException(cancellationToken);
        } catch (OperationCanceledException) when (lookupCancellation?.IsCancellationRequested == true) {
            throw new TimeoutException($"The reverse-DNS lookup exceeded {_options.DnsTimeoutMilliseconds} ms.");
        } finally {
            lookupCancellation?.Dispose();
            if (entered) {
                _dnsConcurrency!.Release();
            }
        }
    }

    private async Task<DnsResponse> ResolveSystemDnsAsync(
        string address,
        CancellationToken cancellationToken) {

        int attempts = _options.RetryDnsOnTransient ? 2 : 1;
        for (int attempt = 1; attempt <= attempts; attempt++) {
            try {
                Task<IPHostEntry> lookup = Dns.GetHostEntryAsync(address);
                Task cancelled = Task.Delay(
                    Timeout.Infinite,
                    cancellationToken);
                Task completed = await Task.WhenAny(
                    lookup,
                    cancelled).ConfigureAwait(false);
                if (!ReferenceEquals(completed, lookup)) {
                    throw new OperationCanceledException(cancellationToken);
                }
                IPHostEntry entry = await lookup.ConfigureAwait(false);
                string hostName = entry.HostName?.Trim().TrimEnd('.') ??
                                  string.Empty;
                return new DnsResponse {
                    Status = DnsResponseCode.NoError,
                    Answers = hostName.Length == 0
                        ? Array.Empty<DnsAnswer>()
                        : new[] {
                            new DnsAnswer {
                                Type = DnsRecordType.PTR,
                                DataRaw = hostName
                            }
                        }
                };
            } catch (SocketException exception) when (
                exception.SocketErrorCode == SocketError.HostNotFound ||
                exception.SocketErrorCode == SocketError.NoData) {
                return new DnsResponse {
                    Status = DnsResponseCode.NXDomain,
                    Error = exception.Message
                };
            } catch (SocketException) when (attempt < attempts) {
            }
        }
        return new DnsResponse {
            Status = DnsResponseCode.ServerFailure,
            Error = "The system DNS resolver did not return a result."
        };
    }

    /// <inheritdoc />
    public void Dispose() {
        _dnsConcurrency?.Dispose();
    }
}
