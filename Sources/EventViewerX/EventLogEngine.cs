using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using EventViewerX.Native;

namespace EventViewerX;

/// <summary>
/// Dependency-free streaming engine for Windows event sources.
/// </summary>
public static class EventLogEngine {
    /// <summary>
    /// Streams records from a local or remote Windows event channel using the owned native engine.
    /// </summary>
    /// <param name="query">Channel, remote target, projection, and culture options.</param>
    /// <param name="cancellationToken">Token used to stop enumeration between native batches and records.</param>
    /// <returns>A lazy stream of fully detached event snapshots.</returns>
    public static IEnumerable<EventObject> ReadChannel(
        EventLogChannelQuery query,
        CancellationToken cancellationToken = default) {

        if (query == null) {
            throw new ArgumentNullException(nameof(query));
        }
        if (query.MaxEvents < 0) {
            throw new ArgumentOutOfRangeException(nameof(query), "Maximum events must be greater than or equal to zero.");
        }
        if (query.RemoteTimeoutMilliseconds <= 0) {
            throw new ArgumentOutOfRangeException(
                nameof(query),
                "Remote timeout must be greater than zero.");
        }
        if (query.BufferCapacity <= 0 || query.BufferCapacity > 4096) {
            throw new ArgumentOutOfRangeException(
                nameof(query),
                "Buffer capacity must be between 1 and 4096.");
        }
        if (query.RpcEndpointPort <= 0 || query.RpcEndpointPort > 65535) {
            throw new ArgumentOutOfRangeException(
                nameof(query),
                "RPC endpoint port must be between 1 and 65535.");
        }

        return ReadChannelIterator(query, cancellationToken);
    }

    /// <summary>
    /// Streams records from an offline Windows event log using the owned native projection engine.
    /// </summary>
    /// <param name="query">File query and projection options.</param>
    /// <param name="cancellationToken">Token used to stop enumeration between native batches and records.</param>
    /// <returns>A lazy stream of fully detached event snapshots.</returns>
    public static IEnumerable<EventObject> ReadFile(
        EventLogFileQuery query,
        CancellationToken cancellationToken = default) {

        if (query == null) {
            throw new ArgumentNullException(nameof(query));
        }
        if (query.MaxEvents < 0) {
            throw new ArgumentOutOfRangeException(nameof(query), "Maximum events must be greater than or equal to zero.");
        }

        string path = Path.GetFullPath(query.Path.Trim().Trim('"', '\''));
        if (!File.Exists(path)) {
            throw new FileNotFoundException($"The event log file '{path}' does not exist.", path);
        }
        string xpath = string.IsNullOrWhiteSpace(query.XPath) ? "*" : query.XPath;
        WindowsEventNativeMethods.QueryFlags flags =
            WindowsEventNativeMethods.QueryFlags.FilePath |
            (query.Oldest
                ? WindowsEventNativeMethods.QueryFlags.ForwardDirection
                : WindowsEventNativeMethods.QueryFlags.ReverseDirection);
        var nativeQuery = new NativeEventQuery(
            IntPtr.Zero,
            path,
            xpath,
            flags,
            path,
            path,
            query.MessageCulture?.LCID ?? 0);

        int returned = 0;
        foreach (EventObject eventObject in WindowsEventReader.Read(
                     nativeQuery,
                     query.ReadMode,
                     path,
                     path,
                     cancellationToken)) {
            yield return eventObject;
            returned++;
            if (query.MaxEvents > 0 && returned >= query.MaxEvents) {
                yield break;
            }
        }
    }

    private static IEnumerable<EventObject> ReadChannelIterator(
        EventLogChannelQuery query,
        CancellationToken cancellationToken) {

        string machineName = query.MachineName?.Trim() ?? string.Empty;
        bool remote = machineName.Length > 0 &&
            !string.Equals(machineName, ".", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(machineName, "localhost", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(machineName, Environment.MachineName, StringComparison.OrdinalIgnoreCase);
        string xpath = string.IsNullOrWhiteSpace(query.XPath) ? "*" : query.XPath;
        WindowsEventNativeMethods.QueryFlags flags =
            WindowsEventNativeMethods.QueryFlags.ChannelPath |
            (query.Oldest
                ? WindowsEventNativeMethods.QueryFlags.ForwardDirection
                : WindowsEventNativeMethods.QueryFlags.ReverseDirection);
        if (remote) {
            foreach (EventObject eventObject in WindowsEventRemoteReader.Read(
                         machineName,
                         query.LogName,
                         xpath,
                         flags,
                         query.ReadMode,
                         query.MessageCulture?.LCID ?? 0,
                         query.MaxEvents,
                         query.RemoteTimeoutMilliseconds,
                         query.BufferCapacity,
                         query.RpcEndpointPort,
                         cancellationToken)) {
                yield return eventObject;
            }
        } else {
            var nativeQuery = new NativeEventQuery(
                IntPtr.Zero,
                query.LogName,
                xpath,
                flags,
                query.LogName,
                messageLocale: query.MessageCulture?.LCID ?? 0);

            int returned = 0;
            foreach (EventObject eventObject in WindowsEventReader.Read(
                         nativeQuery,
                         query.ReadMode,
                         Environment.MachineName,
                         query.LogName,
                         cancellationToken)) {
                yield return eventObject;
                returned++;
                if (query.MaxEvents > 0 && returned >= query.MaxEvents) {
                    yield break;
                }
            }
        }
    }
}
