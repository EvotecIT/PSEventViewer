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
        cancellationToken.ThrowIfCancellationRequested();

        try {
            using EventLogSessionOpenResult sessionResult = EventLogSessionManager.OpenSessionResult(
                machineName: request.MachineName,
                timeoutMs: request.SessionTimeoutMs,
                purpose: "EventCatalogChannels",
                logName: "*");

            if (!sessionResult.Success || sessionResult.Session is null) {
                result = new EventChannelListResult();
                failure = MapSessionFailure(sessionResult);
                return false;
            }

            var rows = BuildNameRows(
                source: sessionResult.Session.GetLogNames(),
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
        } catch (OperationCanceledException) {
            throw;
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
        cancellationToken.ThrowIfCancellationRequested();

        try {
            using EventLogSessionOpenResult sessionResult = EventLogSessionManager.OpenSessionResult(
                machineName: request.MachineName,
                timeoutMs: request.SessionTimeoutMs,
                purpose: "EventCatalogProviders",
                logName: "*");

            if (!sessionResult.Success || sessionResult.Session is null) {
                result = new EventProviderListResult();
                failure = MapSessionFailure(sessionResult);
                return false;
            }

            var rows = BuildNameRows(
                source: sessionResult.Session.GetProviderNames(),
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
        } catch (OperationCanceledException) {
            throw;
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

    private static EventCatalogFailure MapSessionFailure(EventLogSessionOpenResult sessionResult) {
        EventCatalogFailureKind kind = sessionResult.Status switch {
            EventLogSessionOpenStatus.AccessDenied => EventCatalogFailureKind.AccessDenied,
            EventLogSessionOpenStatus.Timeout => EventCatalogFailureKind.Timeout,
            EventLogSessionOpenStatus.NegativeCache => EventCatalogFailureKind.HostUnavailable,
            EventLogSessionOpenStatus.RpcUnavailable => EventCatalogFailureKind.HostUnavailable,
            EventLogSessionOpenStatus.EventLogSessionUnavailable => EventCatalogFailureKind.HostUnavailable,
            _ => EventCatalogFailureKind.Exception
        };
        return new EventCatalogFailure {
            Kind = kind,
            Message = string.IsNullOrWhiteSpace(sessionResult.ErrorMessage)
                ? $"Failed to open Event Log session to '{sessionResult.TargetHost}'."
                : sessionResult.ErrorMessage
        };
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

    internal static List<T> BuildNameRows<T>(
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

        int resultCount = request.MaxResults > 0
            ? Math.Min(request.MaxResults, names.Count)
            : names.Count;
        truncated = request.MaxResults > 0 && names.Count > request.MaxResults;
        var rows = new List<T>(Math.Min(resultCount, 256));
        for (int index = 0; index < resultCount; index++) {
            cancellationToken.ThrowIfCancellationRequested();
            rows.Add(rowFactory(names[index]));
        }

        return rows;
    }
}
