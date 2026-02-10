using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using EventViewerX.Reports.QueryHelpers;

namespace EventViewerX.Reports.Evtx;

/// <summary>
/// Executes EVTX queries using a stable request/response contract.
/// </summary>
public static class EvtxQueryExecutor {
    /// <summary>
    /// Queries an EVTX file and returns either events or a typed failure.
    /// </summary>
    /// <param name="request">EVTX query request.</param>
    /// <param name="result">Result object with queried events.</param>
    /// <param name="failure">Failure details when query fails.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when query succeeds; otherwise <see langword="false"/>.</returns>
    public static bool TryRead(
        EvtxQueryRequest request,
        out EvtxQueryResult result,
        out EvtxQueryFailure? failure,
        CancellationToken cancellationToken = default) {

        if (request is null) {
            result = new EvtxQueryResult();
            failure = new EvtxQueryFailure {
                Kind = EvtxQueryFailureKind.InvalidArgument,
                Message = "request is required."
            };
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.FilePath)) {
            result = new EvtxQueryResult();
            failure = new EvtxQueryFailure {
                Kind = EvtxQueryFailureKind.InvalidArgument,
                Message = "filePath is required."
            };
            return false;
        }

        if (QueryValidationHelpers.HasInvalidUtcRange(request.StartTimeUtc, request.EndTimeUtc)) {
            result = new EvtxQueryResult();
            failure = new EvtxQueryFailure {
                Kind = EvtxQueryFailureKind.InvalidArgument,
                Message = "startTimeUtc must be less than or equal to endTimeUtc."
            };
            return false;
        }

        if (QueryValidationHelpers.IsNegative(request.MaxEvents)) {
            result = new EvtxQueryResult();
            failure = new EvtxQueryFailure {
                Kind = EvtxQueryFailureKind.InvalidArgument,
                Message = "maxEvents must be greater than or equal to 0."
            };
            return false;
        }

        if (QueryValidationHelpers.HasNonPositiveValues(request.EventIds)) {
            result = new EvtxQueryResult();
            failure = new EvtxQueryFailure {
                Kind = EvtxQueryFailureKind.InvalidArgument,
                Message = "eventIds must contain only positive values."
            };
            return false;
        }

        try {
            var eventIds = request.EventIds is null ? null : new List<int>(request.EventIds);

            var list = new List<EventObject>();
            foreach (var ev in SearchEvents.QueryLogFile(
                         filePath: request.FilePath,
                         eventIds: eventIds,
                         providerName: request.ProviderName,
                         startTime: request.StartTimeUtc,
                         endTime: request.EndTimeUtc,
                         maxEvents: request.MaxEvents,
                         oldest: request.OldestFirst,
                         cancellationToken: cancellationToken)) {
                cancellationToken.ThrowIfCancellationRequested();
                list.Add(ev);
            }

            result = new EvtxQueryResult {
                Events = list
            };
            failure = null;
            return true;
        } catch (ArgumentException ex) {
            result = new EvtxQueryResult();
            failure = new EvtxQueryFailure {
                Kind = EvtxQueryFailureKind.InvalidArgument,
                Message = ex.Message
            };
            return false;
        } catch (FileNotFoundException ex) {
            result = new EvtxQueryResult();
            failure = new EvtxQueryFailure {
                Kind = EvtxQueryFailureKind.NotFound,
                Message = ex.Message
            };
            return false;
        } catch (UnauthorizedAccessException ex) {
            result = new EvtxQueryResult();
            failure = new EvtxQueryFailure {
                Kind = EvtxQueryFailureKind.AccessDenied,
                Message = ex.Message
            };
            return false;
        } catch (IOException ex) {
            result = new EvtxQueryResult();
            failure = new EvtxQueryFailure {
                Kind = EvtxQueryFailureKind.IoError,
                Message = ex.Message
            };
            return false;
        } catch (Exception ex) {
            result = new EvtxQueryResult();
            failure = new EvtxQueryFailure {
                Kind = EvtxQueryFailureKind.Exception,
                Message = ex.Message
            };
            return false;
        }
    }
}
