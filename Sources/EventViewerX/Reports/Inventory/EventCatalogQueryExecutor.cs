using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Threading;

namespace EventViewerX.Reports.Inventory;

/// <summary>
/// Executes typed event catalog queries (channels/providers).
/// </summary>
public static class EventCatalogQueryExecutor {
    /// <summary>
    /// Lists event log channels from local or remote machine.
    /// </summary>
    /// <param name="request">Catalog query request.</param>
    /// <param name="result">Result payload on success.</param>
    /// <param name="failure">Failure payload on error.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> on success; otherwise <see langword="false"/>.</returns>
    public static bool TryListChannels(
        EventCatalogQueryRequest request,
        out EventChannelListResult result,
        out EventCatalogFailure? failure,
        CancellationToken cancellationToken = default) {

        if (!TryValidateRequest(request, out result, out failure)) {
            return false;
        }

        try {
            using var session = SearchEvents.OpenSession(
                machineName: request.MachineName,
                timeoutMs: request.SessionTimeoutMs,
                purpose: "EventCatalogChannels",
                logName: "*");

            if (session is null) {
                result = new EventChannelListResult();
                failure = new EventCatalogFailure {
                    Kind = EventCatalogFailureKind.Exception,
                    Message = "Failed to open event log session."
                };
                return false;
            }

            var rows = BuildNameRows(
                source: session.GetLogNames(),
                request: request,
                cancellationToken: cancellationToken,
                rowFactory: static name => new EventChannelRow { Name = name },
                out var truncated);

            result = new EventChannelListResult {
                Count = rows.Count,
                Truncated = truncated,
                Channels = rows
            };
            failure = null;
            return true;
        } catch (UnauthorizedAccessException ex) {
            result = new EventChannelListResult();
            failure = new EventCatalogFailure {
                Kind = EventCatalogFailureKind.AccessDenied,
                Message = ex.Message
            };
            return false;
        } catch (EventLogException ex) {
            result = new EventChannelListResult();
            failure = new EventCatalogFailure {
                Kind = EventCatalogFailureKind.Exception,
                Message = ex.Message
            };
            return false;
        } catch (Exception ex) {
            result = new EventChannelListResult();
            failure = new EventCatalogFailure {
                Kind = EventCatalogFailureKind.Exception,
                Message = ex.Message
            };
            return false;
        }
    }

    /// <summary>
    /// Lists event providers from local or remote machine.
    /// </summary>
    /// <param name="request">Catalog query request.</param>
    /// <param name="result">Result payload on success.</param>
    /// <param name="failure">Failure payload on error.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> on success; otherwise <see langword="false"/>.</returns>
    public static bool TryListProviders(
        EventCatalogQueryRequest request,
        out EventProviderListResult result,
        out EventCatalogFailure? failure,
        CancellationToken cancellationToken = default) {

        if (!TryValidateRequest(request, out result, out failure)) {
            return false;
        }

        try {
            using var session = SearchEvents.OpenSession(
                machineName: request.MachineName,
                timeoutMs: request.SessionTimeoutMs,
                purpose: "EventCatalogProviders",
                logName: "*");

            if (session is null) {
                result = new EventProviderListResult();
                failure = new EventCatalogFailure {
                    Kind = EventCatalogFailureKind.Exception,
                    Message = "Failed to open event log session."
                };
                return false;
            }

            var rows = BuildNameRows(
                source: session.GetProviderNames(),
                request: request,
                cancellationToken: cancellationToken,
                rowFactory: static name => new EventProviderRow { Name = name },
                out var truncated);

            result = new EventProviderListResult {
                Count = rows.Count,
                Truncated = truncated,
                Providers = rows
            };
            failure = null;
            return true;
        } catch (UnauthorizedAccessException ex) {
            result = new EventProviderListResult();
            failure = new EventCatalogFailure {
                Kind = EventCatalogFailureKind.AccessDenied,
                Message = ex.Message
            };
            return false;
        } catch (EventLogException ex) {
            result = new EventProviderListResult();
            failure = new EventCatalogFailure {
                Kind = EventCatalogFailureKind.Exception,
                Message = ex.Message
            };
            return false;
        } catch (Exception ex) {
            result = new EventProviderListResult();
            failure = new EventCatalogFailure {
                Kind = EventCatalogFailureKind.Exception,
                Message = ex.Message
            };
            return false;
        }
    }

    private static bool TryValidateRequest<T>(
        EventCatalogQueryRequest request,
        out T result,
        out EventCatalogFailure? failure) where T : new() {

        if (request is null) {
            result = new T();
            failure = new EventCatalogFailure {
                Kind = EventCatalogFailureKind.InvalidArgument,
                Message = "request is required."
            };
            return false;
        }

        if (request.MaxResults < 0) {
            result = new T();
            failure = new EventCatalogFailure {
                Kind = EventCatalogFailureKind.InvalidArgument,
                Message = "maxResults must be greater than or equal to 0."
            };
            return false;
        }

        if (request.SessionTimeoutMs.HasValue && request.SessionTimeoutMs.Value <= 0) {
            result = new T();
            failure = new EventCatalogFailure {
                Kind = EventCatalogFailureKind.InvalidArgument,
                Message = "sessionTimeoutMs must be positive when provided."
            };
            return false;
        }

        result = new T();
        failure = null;
        return true;
    }

    private static List<T> BuildNameRows<T>(
        IEnumerable<string> source,
        EventCatalogQueryRequest request,
        CancellationToken cancellationToken,
        Func<string, T> rowFactory,
        out bool truncated) {

        var names = new List<string>();
        foreach (var item in source) {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(item)) {
                continue;
            }
            if (!string.IsNullOrWhiteSpace(request.NameContains) &&
                item.IndexOf(request.NameContains, StringComparison.OrdinalIgnoreCase) < 0) {
                continue;
            }
            names.Add(item);
        }

        names.Sort(StringComparer.OrdinalIgnoreCase);

        var rows = new List<T>();
        truncated = false;
        foreach (var name in names) {
            cancellationToken.ThrowIfCancellationRequested();
            rows.Add(rowFactory(name));
            if (request.MaxResults > 0 && rows.Count >= request.MaxResults) {
                truncated = true;
                break;
            }
        }

        return rows;
    }
}
