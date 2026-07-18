using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace EventViewerX {
    /// <summary>
    /// Methods for querying events by predefined named event types.
    /// </summary>
    public partial class SearchEvents : Settings {
        /// <summary>
        /// Searches logs for events matching the provided named event types.
        /// </summary>
        /// <param name="typeEventsList">Event types to locate.</param>
        /// <param name="machineNames">Target machines to query.</param>
        /// <param name="startTime">Optional start time.</param>
        /// <param name="endTime">Optional end time.</param>
        /// <param name="timePeriod">Predefined time period.</param>
        /// <param name="maxThreads">Maximum parallel threads.</param>
        /// <param name="maxEvents">Global maximum number of matching rule results to return.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="maxEventsScanned">Global maximum number of candidate event records to evaluate before rule filtering.</param>
        /// <param name="executionInfo">Optional progress object populated while the query is enumerated.</param>
        /// <param name="minimumEventRecordIdExclusiveResolver">Optional per-machine/per-log native checkpoint resolver.</param>
        /// <param name="candidateObserver">Optional observer invoked for every native candidate before named-event projection.</param>
        /// <returns>Asynchronous sequence of simplified events.</returns>
        public static async IAsyncEnumerable<EventObjectSlim> FindEventsByNamedEvents(
            List<NamedEvents> typeEventsList,
            List<string?>? machineNames = null,
            DateTime? startTime = null,
            DateTime? endTime = null,
            TimePeriod? timePeriod = null,
            int maxThreads = 8,
            int maxEvents = 0,
            [EnumeratorCancellation] CancellationToken cancellationToken = default,
            int maxEventsScanned = 0,
            NamedEventsQueryExecutionInfo? executionInfo = null,
            Func<string?, string, long?>? minimumEventRecordIdExclusiveResolver = null,
            Action<EventObject>? candidateObserver = null) {

            if (typeEventsList == null) {
                throw new ArgumentNullException(nameof(typeEventsList));
            }
            if (maxThreads <= 0 || maxThreads > MaximumParallelism) {
                throw new ArgumentOutOfRangeException(nameof(maxThreads), $"Maximum threads must be between 1 and {MaximumParallelism}.");
            }
            if (maxEvents < 0) {
                throw new ArgumentOutOfRangeException(nameof(maxEvents), "Maximum events must be greater than or equal to zero.");
            }
            if (maxEventsScanned < 0) {
                throw new ArgumentOutOfRangeException(nameof(maxEventsScanned), "Maximum scanned events must be greater than or equal to zero.");
            }

            Dictionary<string, HashSet<int>> eventInfo = EventObjectSlim.GetEventInfoForNamedEvents(typeEventsList);
            NamedEventsQueryExecutionInfo queryInfo = executionInfo ?? new NamedEventsQueryExecutionInfo();
            queryInfo.Reset(maxEventsScanned);
            int emitted = 0;

            if (maxEventsScanned > 0) {
                int candidateLimit = maxEventsScanned == int.MaxValue ? int.MaxValue : maxEventsScanned + 1;
                var candidates = new List<EventObject>(Math.Min(candidateLimit, 256));
                foreach (KeyValuePair<string, HashSet<int>> entry in eventInfo) {
                    await foreach (EventObject foundEvent in QueryNamedEventCandidates(
                                       entry,
                                       machineNames,
                                       startTime,
                                       endTime,
                                       timePeriod,
                                       maxThreads,
                                       candidateLimit,
                                       cancellationToken,
                                       minimumEventRecordIdExclusiveResolver)) {
                        candidates.Add(foundEvent);
                        TrimNamedCandidates(candidates, candidateLimit);
                    }
                }

                candidates.Sort((left, right) => CompareEvents(left, right, oldest: false));
                foreach (EventObject foundEvent in candidates) {
                    if (!queryInfo.TryRecordCandidate()) {
                        yield break;
                    }

                    candidateObserver?.Invoke(foundEvent);
                    EventObjectSlim? targetEvent = BuildTargetEvents(foundEvent, typeEventsList);
                    if (targetEvent == null) {
                        continue;
                    }

                    emitted++;
                    queryInfo.EventsEmitted = emitted;
                    yield return targetEvent;
                    if (maxEvents > 0 && emitted >= maxEvents) {
                        yield break;
                    }
                }
                yield break;
            }

            if (maxEvents > 0) {
                var matches = new List<NamedEventMatch>(Math.Min(maxEvents, 256));
                foreach (KeyValuePair<string, HashSet<int>> entry in eventInfo) {
                    int candidateLimit = maxEvents;
                    List<EventObject> logCandidates;
                    List<NamedEventMatch> logMatches;
                    while (true) {
                        logCandidates = new List<EventObject>(Math.Min(candidateLimit, 256));
                        await foreach (EventObject foundEvent in QueryNamedEventCandidates(
                                           entry,
                                           machineNames,
                                           startTime,
                                           endTime,
                                           timePeriod,
                                           maxThreads,
                                           candidateLimit,
                                           cancellationToken,
                                           minimumEventRecordIdExclusiveResolver)) {
                            logCandidates.Add(foundEvent);
                        }

                        logMatches = new List<NamedEventMatch>(Math.Min(maxEvents, logCandidates.Count));
                        foreach (EventObject foundEvent in logCandidates) {
                            EventObjectSlim? targetEvent = BuildTargetEvents(foundEvent, typeEventsList);
                            if (targetEvent != null) {
                                logMatches.Add(new NamedEventMatch(foundEvent, targetEvent));
                                if (logMatches.Count >= maxEvents) {
                                    break;
                                }
                            }
                        }

                        if (logMatches.Count >= maxEvents || logCandidates.Count < candidateLimit || candidateLimit == int.MaxValue) {
                            break;
                        }

                        candidateLimit = candidateLimit > int.MaxValue / 2 ? int.MaxValue : candidateLimit * 2;
                    }

                    foreach (EventObject foundEvent in logCandidates) {
                        queryInfo.TryRecordCandidate();
                        candidateObserver?.Invoke(foundEvent);
                    }
                    matches.AddRange(logMatches);
                    TrimNamedMatches(matches, maxEvents);
                }

                matches.Sort(static (left, right) => CompareEvents(left.Source, right.Source, oldest: false));
                int count = Math.Min(maxEvents, matches.Count);
                for (int index = 0; index < count; index++) {
                    cancellationToken.ThrowIfCancellationRequested();
                    queryInfo.EventsEmitted = index + 1;
                    yield return matches[index].Projection;
                }
                yield break;
            }

            // Unlimited queries retain streaming/backpressure and do not need a cross-log selection buffer.
            foreach (KeyValuePair<string, HashSet<int>> entry in eventInfo) {
                await foreach (EventObject foundEvent in QueryNamedEventCandidates(
                                   entry,
                                   machineNames,
                                   startTime,
                                   endTime,
                                   timePeriod,
                                   maxThreads,
                                   maxEvents: 0,
                                   cancellationToken,
                                   minimumEventRecordIdExclusiveResolver)) {
                    queryInfo.TryRecordCandidate();

                    candidateObserver?.Invoke(foundEvent);

                    EventObjectSlim? targetEvent = BuildTargetEvents(foundEvent, typeEventsList);
                    if (targetEvent == null) {
                        continue;
                    }

                    emitted++;
                    queryInfo.EventsEmitted = emitted;
                    yield return targetEvent;
                }
            }
        }

        private static IAsyncEnumerable<EventObject> QueryNamedEventCandidates(
            KeyValuePair<string, HashSet<int>> entry,
            List<string?>? machineNames,
            DateTime? startTime,
            DateTime? endTime,
            TimePeriod? timePeriod,
            int maxThreads,
            int maxEvents,
            CancellationToken cancellationToken,
            Func<string?, string, long?>? minimumEventRecordIdExclusiveResolver) {

            return QueryLogsParallel(
                entry.Key,
                entry.Value.ToList(),
                machineNames,
                startTime: startTime,
                endTime: endTime,
                maxEvents: maxEvents,
                maxThreads: maxThreads,
                timePeriod: timePeriod,
                cancellationToken: cancellationToken,
                readMode: EventReadMode.Full,
                minimumEventRecordIdExclusiveResolver: minimumEventRecordIdExclusiveResolver == null
                    ? null
                    : machineName => minimumEventRecordIdExclusiveResolver(machineName, entry.Key));
        }

        private static void TrimNamedCandidates(List<EventObject> candidates, int limit) {
            long trimThreshold = Math.Min((long)limit * 2, (long)limit + 1024);
            if (candidates.Count >= trimThreshold) {
                SortAndTrim(candidates, limit, oldest: false);
            }
        }

        private static void TrimNamedMatches(List<NamedEventMatch> matches, int limit) {
            long trimThreshold = Math.Min((long)limit * 2, (long)limit + 1024);
            if (matches.Count < trimThreshold) {
                return;
            }

            matches.Sort(static (left, right) => CompareEvents(left.Source, right.Source, oldest: false));
            if (matches.Count > limit) {
                matches.RemoveRange(limit, matches.Count - limit);
            }
        }

        private sealed class NamedEventMatch {
            internal NamedEventMatch(EventObject source, EventObjectSlim projection) {
                Source = source;
                Projection = projection;
            }

            internal EventObject Source { get; }
            internal EventObjectSlim Projection { get; }
        }
    }
}
