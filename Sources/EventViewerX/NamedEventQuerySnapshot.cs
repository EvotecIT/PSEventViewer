using System.Globalization;

namespace EventViewerX;

/// <summary>
/// Freezes a named-event query before deferred asynchronous execution.
/// </summary>
internal static class NamedEventQuerySnapshot {
    internal static NamedEventQuery Copy(
        NamedEventQuery source) {

        if (source == null) {
            throw new ArgumentNullException(nameof(source));
        }
        return new NamedEventQuery(
            source.NamedEvents.ToArray()) {
            MachineNames =
                source.MachineNames?.ToArray(),
            SourceLogName = source.SourceLogName,
            SourceEventIds =
                source.SourceEventIds?.ToArray(),
            StartTime = source.StartTime,
            EndTime = source.EndTime,
            TimePeriod = source.TimePeriod,
            MaxEvents = source.MaxEvents,
            MaxCandidates = source.MaxCandidates,
            MaxConcurrency = source.MaxConcurrency,
            Oldest = source.Oldest,
            ReadMode = source.ReadMode,
            IncludeBookmark = source.IncludeBookmark,
            Credential = EventLogCredentialIdentity.Copy(
                source.Credential),
            Authentication = source.Authentication,
            RemoteConnectionTimeoutMilliseconds =
                source.RemoteConnectionTimeoutMilliseconds,
            RemoteReadTimeoutMilliseconds =
                source.RemoteReadTimeoutMilliseconds,
            BufferCapacity = source.BufferCapacity,
            MessageCulture = CopyCulture(
                source.MessageCulture),
            FallbackMessageCulture = CopyCulture(
                source.FallbackMessageCulture),
            Enrichment = CopyEnrichment(
                source.Enrichment),
            ResultPredicate = source.ResultPredicate,
            MinimumRecordIdExclusiveResolver =
                source.MinimumRecordIdExclusiveResolver,
            CandidateObserver = source.CandidateObserver,
            ContinueOnRemoteFailure =
                source.ContinueOnRemoteFailure
        };
    }

    private static CultureInfo? CopyCulture(
        CultureInfo? culture) {

        return culture == null
            ? null
            : CultureInfo.GetCultureInfo(
                culture.Name);
    }

    private static NamedEventEnrichmentOptions? CopyEnrichment(
        NamedEventEnrichmentOptions? enrichment) {

        return enrichment == null
            ? null
            : new NamedEventEnrichmentOptions {
                ResolveDns = enrichment.ResolveDns,
                DnsTimeoutMilliseconds =
                    enrichment.DnsTimeoutMilliseconds,
                DnsMaxConcurrency =
                    enrichment.DnsMaxConcurrency,
                RetryDnsOnTransient =
                    enrichment.RetryDnsOnTransient
            };
    }
}
