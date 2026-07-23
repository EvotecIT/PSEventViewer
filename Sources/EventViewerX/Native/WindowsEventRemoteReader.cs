using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace EventViewerX.Native;

internal static class WindowsEventRemoteReader {
    internal static IEnumerable<EventObject> Read(
        string machineName,
        string logName,
        string xpath,
        WindowsEventNativeMethods.QueryFlags flags,
        EventReadMode readMode,
        int messageLocale,
        int maxEvents,
        int timeoutMilliseconds,
        int bufferCapacity,
        int rpcEndpointPort,
        CancellationToken cancellationToken) {

        return ReadIterator(
            machineName,
            logName,
            xpath,
            flags,
            readMode,
            messageLocale,
            maxEvents,
            timeoutMilliseconds,
            bufferCapacity,
            rpcEndpointPort,
            cancellationToken);
    }

    private static IEnumerable<EventObject> ReadIterator(
        string machineName,
        string logName,
        string xpath,
        WindowsEventNativeMethods.QueryFlags flags,
        EventReadMode readMode,
        int messageLocale,
        int maxEvents,
        int timeoutMilliseconds,
        int bufferCapacity,
        int rpcEndpointPort,
        CancellationToken cancellationToken) {

        string timeoutMessage =
            $"Timed out reading '{logName}' on '{machineName}' after {timeoutMilliseconds} ms without progress.";
        if (!RpcEndpointProbe.TryConnect(
                machineName,
                rpcEndpointPort,
                timeoutMilliseconds)) {
            throw new System.ComponentModel.Win32Exception(
                1722,
                $"The RPC endpoint for '{machineName}' is unavailable.");
        }
        IDisposable operationSlot = BoundedNativeOperation.Acquire(
            timeoutMilliseconds,
            timeoutMessage);
        var results = new BlockingCollection<EventObject>(bufferCapacity);
        var failures = new ConcurrentQueue<Exception>();
        using var workerCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task producer;
        try {
            var completion = new TaskCompletionSource<object?>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var thread = new Thread(() => {
                try {
                    Produce(
                        machineName,
                        logName,
                        xpath,
                        flags,
                        readMode,
                        messageLocale,
                        maxEvents,
                        timeoutMilliseconds,
                        results,
                        failures,
                        operationSlot,
                        workerCancellation.Token);
                    completion.TrySetResult(null);
                } catch (Exception ex) {
                    completion.TrySetException(ex);
                }
            }) {
                IsBackground = true,
                Name = $"EventViewerX remote reader: {machineName}"
            };
            thread.Start();
            producer = completion.Task;
        } catch {
            operationSlot.Dispose();
            results.Dispose();
            throw;
        }

        var inactivity = Stopwatch.StartNew();
        try {
            while (true) {
                cancellationToken.ThrowIfCancellationRequested();
                if (results.TryTake(
                        out EventObject? eventObject,
                        Math.Min(100, timeoutMilliseconds),
                        cancellationToken)) {
                    inactivity.Restart();
                    yield return eventObject;
                    continue;
                }
                if (results.IsCompleted) {
                    break;
                }
                if (inactivity.ElapsedMilliseconds >= timeoutMilliseconds) {
                    workerCancellation.Cancel();
                    throw new TimeoutException(timeoutMessage);
                }
            }

            producer.GetAwaiter().GetResult();
            if (failures.TryDequeue(out Exception? failure)) {
                throw failure;
            }
        } finally {
            workerCancellation.Cancel();
            if (producer.IsCompleted) {
                results.Dispose();
            } else {
                _ = producer.ContinueWith(
                    completed => {
                        _ = completed.Exception;
                        results.Dispose();
                    },
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }
        }
    }

    private static void Produce(
        string machineName,
        string logName,
        string xpath,
        WindowsEventNativeMethods.QueryFlags flags,
        EventReadMode readMode,
        int messageLocale,
        int maxEvents,
        int timeoutMilliseconds,
        BlockingCollection<EventObject> results,
        ConcurrentQueue<Exception> failures,
        IDisposable operationSlot,
        CancellationToken cancellationToken) {

        using (operationSlot) {
            try {
                using WindowsEventNativeMethods.EventHandle session =
                    WindowsEventRemoteSession.OpenCore(machineName, new WindowsEventNativeMethods.RpcLogin {
                        Server = machineName,
                        User = null,
                        Domain = null,
                        Password = IntPtr.Zero,
                        Flags = 0
                    });
                var nativeQuery = new NativeEventQuery(
                    session.DangerousGetHandle(),
                    logName,
                    xpath,
                    flags,
                    $"{logName} on {machineName}",
                    messageLocale: messageLocale,
                    nextTimeoutMilliseconds: timeoutMilliseconds);
                int returned = 0;
                foreach (EventObject eventObject in WindowsEventReader.Read(
                             nativeQuery,
                             readMode,
                             machineName,
                             logName,
                             cancellationToken)) {
                    results.Add(eventObject, cancellationToken);
                    returned++;
                    if (maxEvents > 0 && returned >= maxEvents) {
                        break;
                    }
                }
            } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            } catch (Exception ex) {
                failures.Enqueue(ex);
            } finally {
                results.CompleteAdding();
            }
        }
    }
}
