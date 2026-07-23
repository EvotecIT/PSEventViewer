using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace EventViewerX.Native;

internal static class BoundedNativeOperation {
    internal const int MaximumConcurrentOperations = 16;
    private static readonly SemaphoreSlim Slots = new(
        MaximumConcurrentOperations,
        MaximumConcurrentOperations);

    internal static IDisposable Acquire(
        int timeoutMilliseconds,
        string timeoutMessage) {

        if (!Slots.Wait(timeoutMilliseconds)) {
            throw new TimeoutException(timeoutMessage);
        }
        return new SlotLease();
    }

    internal static T Execute<T>(
        Func<T> operation,
        int timeoutMilliseconds,
        string timeoutMessage,
        Action<T>? lateResultCleanup = null) {

        if (timeoutMilliseconds <= 0) {
            return operation();
        }

        var timeoutBudget = Stopwatch.StartNew();
        if (!Slots.Wait(timeoutMilliseconds)) {
            throw new TimeoutException(timeoutMessage);
        }

        int remainingTimeout = timeoutMilliseconds -
            (int)Math.Min(timeoutBudget.ElapsedMilliseconds, timeoutMilliseconds);
        if (remainingTimeout <= 0) {
            Slots.Release();
            throw new TimeoutException(timeoutMessage);
        }

        Task<T> task;
        try {
            var completion = new TaskCompletionSource<T>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var thread = new Thread(() => {
                try {
                    completion.TrySetResult(operation());
                } catch (Exception ex) {
                    completion.TrySetException(ex);
                } finally {
                    Slots.Release();
                }
            }) {
                IsBackground = true,
                Name = "EventViewerX bounded native operation"
            };
            thread.Start();
            task = completion.Task;
        } catch {
            Slots.Release();
            throw;
        }

        bool completed;
        try {
            completed = task.Wait(remainingTimeout);
        } catch (AggregateException) {
            return task.GetAwaiter().GetResult();
        }

        if (completed) {
            return task.GetAwaiter().GetResult();
        }

        _ = task.ContinueWith(
            completedTask => {
                if (completedTask.Status == TaskStatus.RanToCompletion) {
                    try {
                        lateResultCleanup?.Invoke(completedTask.Result);
                    } catch {
                    }
                } else if (completedTask.IsFaulted) {
                    _ = completedTask.Exception;
                }
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
        throw new TimeoutException(timeoutMessage);
    }

    private sealed class SlotLease : IDisposable {
        private int _released;

        public void Dispose() {
            if (Interlocked.Exchange(ref _released, 1) == 0) {
                Slots.Release();
            }
        }
    }
}
