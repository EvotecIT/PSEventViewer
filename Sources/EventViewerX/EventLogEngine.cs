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
}
