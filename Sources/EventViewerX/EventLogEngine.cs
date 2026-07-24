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
        if (query.RemoteConnectionTimeoutMilliseconds <= 0) {
            throw new ArgumentOutOfRangeException(
                nameof(query),
                "Remote connection timeout must be greater than zero.");
        }
        if (query.RemoteReadTimeoutMilliseconds < 0) {
            throw new ArgumentOutOfRangeException(
                nameof(query),
                "Remote read timeout must be greater than or equal to zero.");
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

        string machineName = query.MachineName?.Trim() ?? string.Empty;
        string logName = query.LogName;
        string xpath = string.IsNullOrWhiteSpace(query.XPath) ? "*" : query.XPath;
        bool oldest = query.Oldest;
        EventReadMode readMode = query.ReadMode;
        int messageLocale = query.MessageCulture?.LCID ?? 0;
        int maxEvents = query.MaxEvents;
        int remoteConnectionTimeoutMilliseconds =
            query.RemoteConnectionTimeoutMilliseconds;
        int remoteReadTimeoutMilliseconds = query.RemoteReadTimeoutMilliseconds;
        int bufferCapacity = query.BufferCapacity;
        int rpcEndpointPort = query.RpcEndpointPort;

        return ReadChannelIterator(
            machineName,
            logName,
            xpath,
            oldest,
            readMode,
            messageLocale,
            maxEvents,
            remoteConnectionTimeoutMilliseconds,
            remoteReadTimeoutMilliseconds,
            bufferCapacity,
            rpcEndpointPort,
            cancellationToken);
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

        NativeEventQuery nativeQuery = CreateNativeFileQuery(
            query,
            out string path,
            out int maxEvents,
            out EventReadMode readMode);
        return ReadFileIterator(
            nativeQuery,
            path,
            maxEvents,
            readMode,
            cancellationToken);
    }

    internal static IEnumerable<string> ReadFileXml(
        EventLogFileQuery query,
        CancellationToken cancellationToken) {

        NativeEventQuery nativeQuery = CreateNativeFileQuery(
            query,
            out _,
            out int maxEvents,
            out _);
        return ReadFileXmlIterator(nativeQuery, maxEvents, cancellationToken);
    }

    internal static long CopyFileXml(
        EventLogFileQuery query,
        Stream destination,
        CancellationToken cancellationToken) {

        NativeEventQuery nativeQuery = CreateNativeFileQuery(
            query,
            out _,
            out int maxEvents,
            out _);
        return WindowsEventReader.CopyXml(
            nativeQuery,
            destination,
            maxEvents,
            cancellationToken);
    }

    private static IEnumerable<EventObject> ReadFileIterator(
        NativeEventQuery nativeQuery,
        string path,
        int maxEvents,
        EventReadMode readMode,
        CancellationToken cancellationToken) {

        int returned = 0;
        foreach (EventObject eventObject in WindowsEventReader.Read(
                     nativeQuery,
                     readMode,
                     path,
                     path,
                     cancellationToken)) {
            yield return eventObject;
            returned++;
            if (maxEvents > 0 && returned >= maxEvents) {
                yield break;
            }
        }
    }

    private static IEnumerable<string> ReadFileXmlIterator(
        NativeEventQuery nativeQuery,
        int maxEvents,
        CancellationToken cancellationToken) {

        int returned = 0;
        foreach (string xml in WindowsEventReader.ReadXml(
                     nativeQuery,
                     cancellationToken)) {
            yield return xml;
            returned++;
            if (maxEvents > 0 && returned >= maxEvents) {
                yield break;
            }
        }
    }

    private static NativeEventQuery CreateNativeFileQuery(
        EventLogFileQuery query,
        out string path,
        out int maxEvents,
        out EventReadMode readMode) {

        if (query == null) {
            throw new ArgumentNullException(nameof(query));
        }
        if (query.MaxEvents < 0) {
            throw new ArgumentOutOfRangeException(
                nameof(query),
                "Maximum events must be greater than or equal to zero.");
        }

        path = Path.GetFullPath(query.Path.Trim().Trim('"', '\''));
        if (!File.Exists(path)) {
            throw new FileNotFoundException(
                $"The event log file '{path}' does not exist.",
                path);
        }
        string xpath = string.IsNullOrWhiteSpace(query.XPath) ? "*" : query.XPath;
        WindowsEventNativeMethods.QueryFlags flags =
            WindowsEventNativeMethods.QueryFlags.FilePath |
            (query.Oldest
                ? WindowsEventNativeMethods.QueryFlags.ForwardDirection
                : WindowsEventNativeMethods.QueryFlags.ReverseDirection);
        maxEvents = query.MaxEvents;
        readMode = query.ReadMode;
        return new NativeEventQuery(
            IntPtr.Zero,
            path,
            xpath,
            flags,
            path,
            path,
            query.MessageCulture?.LCID ?? 0);
    }

    private static IEnumerable<EventObject> ReadChannelIterator(
        string machineName,
        string logName,
        string xpath,
        bool oldest,
        EventReadMode readMode,
        int messageLocale,
        int maxEvents,
        int remoteConnectionTimeoutMilliseconds,
        int remoteReadTimeoutMilliseconds,
        int bufferCapacity,
        int rpcEndpointPort,
        CancellationToken cancellationToken) {

        bool remote = !SearchEvents.IsLocalMachine(machineName);
        WindowsEventNativeMethods.QueryFlags flags =
            WindowsEventNativeMethods.QueryFlags.ChannelPath |
            (oldest
                ? WindowsEventNativeMethods.QueryFlags.ForwardDirection
                : WindowsEventNativeMethods.QueryFlags.ReverseDirection);
        if (remote) {
            foreach (EventObject eventObject in WindowsEventRemoteReader.Read(
                         machineName,
                         logName,
                         xpath,
                         flags,
                         readMode,
                         messageLocale,
                         maxEvents,
                         remoteConnectionTimeoutMilliseconds,
                         remoteReadTimeoutMilliseconds,
                         bufferCapacity,
                         rpcEndpointPort,
                         cancellationToken)) {
                yield return eventObject;
            }
        } else {
            var nativeQuery = new NativeEventQuery(
                IntPtr.Zero,
                logName,
                xpath,
                flags,
                logName,
                messageLocale: messageLocale);

            int returned = 0;
            foreach (EventObject eventObject in WindowsEventReader.Read(
                         nativeQuery,
                         readMode,
                         Environment.MachineName,
                         logName,
                         cancellationToken)) {
                yield return eventObject;
                returned++;
                if (maxEvents > 0 && returned >= maxEvents) {
                    yield break;
                }
            }
        }
    }
}
