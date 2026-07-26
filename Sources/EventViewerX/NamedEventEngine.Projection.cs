using EventViewerX.Rules.ActiveDirectory;
using EventViewerX.Rules.Logging;
using EventViewerX.Rules.Windows;
using EventViewerX.Rules.Kerberos;
using EventViewerX.Rules.CertificateAuthority;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace EventViewerX {
    public static partial class NamedEventEngine {
        /// <summary>
        /// Builds the appropriate event object based on the NamedEvents value
        /// </summary>
    /// <param name="eventObject">Event to evaluate.</param>
    /// <param name="typeEventsList">List of target event types.</param>
    /// <returns>Concrete event rule instance or null.</returns>
    /// <exception cref="ArgumentException"></exception>
        private static EventObjectSlim? BuildTargetEvents(EventObject eventObject, IReadOnlyList<NamedEvents> typeEventsList) {
            // Use the new reflection-based system - let each rule decide if it can handle the event
            return EventObjectSlim.CreateEventRule(eventObject, typeEventsList.ToList());
        }

        /// <summary>
        /// Projects and enriches an event before it becomes eligible for checkpoint observation.
        /// </summary>
        private static async Task<EventObjectSlim?> BuildAndEnrichTargetAsync(
            EventObject eventObject,
            IReadOnlyList<NamedEvents> typeEventsList,
            NamedEventEnricher? enricher,
            CancellationToken cancellationToken) {

            EventObjectSlim? targetEvent = BuildTargetEvents(eventObject, typeEventsList);
            if (targetEvent != null && enricher != null) {
                await enricher.EnrichAsync(targetEvent, cancellationToken).ConfigureAwait(false);
            }
            return targetEvent;
        }

        /// <summary>
        /// Projects a bounded batch concurrently, then exposes it in source order so observers and checkpoints
        /// cannot move past an event whose projection or enrichment has not completed.
        /// </summary>
        internal static async IAsyncEnumerable<NamedEventProjection> ProjectCandidatesInOrderAsync(
            IAsyncEnumerable<EventObject> candidates,
            IReadOnlyList<NamedEvents> typeEventsList,
            NamedEventEnricher? enricher,
            Func<bool> candidateAdmission,
            Action<EventObject>? candidateObserver,
            [EnumeratorCancellation] CancellationToken cancellationToken) {

            int batchSize = enricher?.MaxConcurrency ?? 1;
            await using IAsyncEnumerator<EventObject> enumerator = candidates.GetAsyncEnumerator(cancellationToken);
            while (true) {
                var batch = new List<PendingNamedEventProjection>(batchSize);
                bool stopAfterBatch = false;
                while (batch.Count < batchSize && await enumerator.MoveNextAsync().ConfigureAwait(false)) {
                    cancellationToken.ThrowIfCancellationRequested();
                    EventObject source = enumerator.Current;
                    if (!candidateAdmission()) {
                        stopAfterBatch = true;
                        break;
                    }

                    batch.Add(new PendingNamedEventProjection(
                        source,
                        BuildAndEnrichTargetAsync(source, typeEventsList, enricher, cancellationToken)));
                }

                if (batch.Count == 0) {
                    yield break;
                }

                var targetTasks = new Task<EventObjectSlim?>[batch.Count];
                for (int index = 0; index < batch.Count; index++) {
                    targetTasks[index] = batch[index].TargetTask;
                }
                EventObjectSlim?[] targets = await Task.WhenAll(targetTasks).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                for (int index = 0; index < batch.Count; index++) {
                    cancellationToken.ThrowIfCancellationRequested();
                    candidateObserver?.Invoke(batch[index].Source);
                    yield return new NamedEventProjection(batch[index].Source, targets[index]);
                }

                if (stopAfterBatch) {
                    yield break;
                }
            }
        }

        private sealed class PendingNamedEventProjection {
            internal PendingNamedEventProjection(EventObject source, Task<EventObjectSlim?> targetTask) {
                Source = source;
                TargetTask = targetTask;
            }

            internal EventObject Source { get; }
            internal Task<EventObjectSlim?> TargetTask { get; }
        }

        internal readonly struct NamedEventProjection {
            internal NamedEventProjection(EventObject source, EventObjectSlim? target) {
                Source = source;
                Target = target;
            }

            internal EventObject Source { get; }
            internal EventObjectSlim? Target { get; }
        }
    }
}
