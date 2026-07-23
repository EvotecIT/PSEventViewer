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

        try {
            using var client = new TcpClient();
            Task connect = client.ConnectAsync(host, port);
            bool completed;
            try {
                completed = connect.Wait(timeoutMilliseconds);
            } catch (AggregateException) {
                connect.GetAwaiter().GetResult();
                return false;
            }

            if (completed && client.Connected) {
                return true;
            }

            _ = connect.ContinueWith(
                task => _ = task.Exception,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted |
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
            return false;
        } catch {
            return false;
        }
    }
}
