using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace EventViewerX.Native;

internal static class RpcEndpointProbe {
    internal static bool TryConnect(
        string host,
        int port,
        int timeoutMilliseconds) {

        return TryConnect(
            host,
            port,
            timeoutMilliseconds,
            CancellationToken.None);
    }

    internal static bool TryConnect(
        string host,
        int port,
        int timeoutMilliseconds,
        CancellationToken cancellationToken) {

        cancellationToken.ThrowIfCancellationRequested();
        try {
            using var client = new TcpClient();
            Task connect = client.ConnectAsync(host, port);
            bool completed;
            try {
                completed = connect.Wait(
                    timeoutMilliseconds,
                    cancellationToken);
            } catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested) {
                ObserveLateFault(connect);
                throw;
            } catch (AggregateException) {
                connect.GetAwaiter().GetResult();
                return false;
            }

            if (completed && client.Connected) {
                return true;
            }

            ObserveLateFault(connect);
            return false;
        } catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested) {
            throw;
        } catch {
            return false;
        }
    }

    private static void ObserveLateFault(Task task) {
        _ = task.ContinueWith(
            completed => _ = completed.Exception,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted |
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }
}
