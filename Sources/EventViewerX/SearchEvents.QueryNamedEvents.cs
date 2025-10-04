using System;
using System.Collections.Generic;
using System.Linq;

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
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
        /// <param name="maxEvents">Maximum events to return.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Asynchronous sequence of simplified events.</returns>
        public static async IAsyncEnumerable<EventObjectSlim> FindEventsByNamedEvents(List<NamedEvents> typeEventsList, List<string?>? machineNames = null, DateTime? startTime = null, DateTime? endTime = null, TimePeriod? timePeriod = null, int maxThreads = 8, int maxEvents = 0, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
            var eventInfoDict = EventObjectSlim.GetEventInfoForNamedEvents(typeEventsList);

            var semaphore = new SemaphoreSlim(maxThreads);
            var results = new BlockingCollection<EventObjectSlim>();
            var tasks = new List<Task>();

            foreach (var kvp in eventInfoDict) {
                var logName = kvp.Key;
                var eventIds = kvp.Value.ToList();

                tasks.Add(Task.Run(async () => {
                    await semaphore.WaitAsync(cancellationToken);
                    try {
                        await foreach (var foundEvent in SearchEvents.QueryLogsParallel(logName, eventIds, machineNames, startTime: startTime, endTime: endTime, timePeriod: timePeriod, maxThreads: maxThreads, maxEvents: maxEvents, cancellationToken: cancellationToken)) {
                            var targetEvent = BuildTargetEvents(foundEvent, typeEventsList);
                            if (targetEvent != null) {
                                results.Add(targetEvent, cancellationToken);
                            }
                        }
                    } finally {
                        semaphore.Release();
                    }
                }, cancellationToken));
            }

            var whenAllTask = Task.WhenAll(tasks);
            _ = whenAllTask.ContinueWith(
                _ => results.CompleteAdding(),
                cancellationToken,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);

            foreach (var result in results.GetConsumingEnumerable(cancellationToken)) {
                yield return result;
                await Task.Yield();
            }

            await whenAllTask;
        }
    }
}