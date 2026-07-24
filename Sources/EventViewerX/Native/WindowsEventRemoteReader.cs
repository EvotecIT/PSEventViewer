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
        int connectionTimeoutMilliseconds,
        int readTimeoutMilliseconds,
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
            connectionTimeoutMilliseconds,
            readTimeoutMilliseconds,
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
        int connectionTimeoutMilliseconds,
        int readTimeoutMilliseconds,
        int bufferCapacity,
        int rpcEndpointPort,
        CancellationToken cancellationToken) {

        string connectionTimeoutMessage =
            $"Timed out connecting to '{logName}' on '{machineName}' after {connectionTimeoutMilliseconds} ms.";
        string readTimeoutMessage =
            $"Timed out reading '{logName}' on '{machineName}' after {readTimeoutMilliseconds} ms without progress.";
        if (!RpcEndpointProbe.TryConnect(
                machineName,
                rpcEndpointPort,
                connectionTimeoutMilliseconds)) {
            throw new System.ComponentModel.Win32Exception(
                1722,
                $"The RPC endpoint for '{machineName}' is unavailable.");
        }
        IDisposable operationSlot = BoundedNativeOperation.Acquire(
            connectionTimeoutMilliseconds,
            connectionTimeoutMessage);
        var results = new BlockingCollection<EventObject>(bufferCapacity);
        var failures = new ConcurrentQueue<Exception>();
        var sessionOpened = new TaskCompletionSource<object?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
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
                        readTimeoutMilliseconds,
                        results,
                        failures,
                        sessionOpened,
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
        bool observedOpenSession = false;
        try {
            while (true) {
                cancellationToken.ThrowIfCancellationRequested();
                if (results.TryTake(
                        out EventObject? eventObject,
                        100,
                        cancellationToken)) {
                    inactivity.Restart();
                    observedOpenSession = true;
                    yield return eventObject;
                    continue;
                }
                if (results.IsCompleted) {
                    break;
                }
                if (!observedOpenSession &&
                    sessionOpened.Task.Status == TaskStatus.RanToCompletion) {
                    observedOpenSession = true;
                    inactivity.Restart();
                }
                int activeTimeout = observedOpenSession
                    ? readTimeoutMilliseconds
                    : connectionTimeoutMilliseconds;
                if (activeTimeout > 0 &&
                    inactivity.ElapsedMilliseconds >= activeTimeout) {
                    workerCancellation.Cancel();
                    throw new TimeoutException(observedOpenSession
                        ? readTimeoutMessage
                        : connectionTimeoutMessage);
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
        int readTimeoutMilliseconds,
        BlockingCollection<EventObject> results,
        ConcurrentQueue<Exception> failures,
        TaskCompletionSource<object?> sessionOpened,
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
                sessionOpened.TrySetResult(null);
                var nativeQuery = new NativeEventQuery(
                    session.DangerousGetHandle(),
                    logName,
                    xpath,
                    flags,
                    $"{logName} on {machineName}",
                    messageLocale: messageLocale,
                    nextTimeoutMilliseconds: readTimeoutMilliseconds);
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
