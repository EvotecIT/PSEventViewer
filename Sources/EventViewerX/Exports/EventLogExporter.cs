using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using EventViewerX.Exports;

namespace EventViewerX;

/// <summary>
/// Writes Windows events directly to durable streaming formats without a PowerShell object pipeline.
/// </summary>
public static class EventLogExporter {
    /// <summary>
    /// Exports an offline event query to a file and atomically promotes the completed output.
    /// </summary>
    /// <param name="query">File query and projection options.</param>
    /// <param name="outputPath">Destination file.</param>
    /// <param name="format">Streaming output format.</param>
    /// <param name="overwrite">Whether an existing destination may be replaced after a successful export.</param>
    /// <param name="cancellationToken">Token used to cancel enumeration and leave the existing destination unchanged.</param>
    /// <returns>Count, size, and SHA-256 for the completed output.</returns>
    public static EventExportResult ExportFile(
        EventLogFileQuery query,
        string outputPath,
        EventExportFormat format,
        bool overwrite = false,
        CancellationToken cancellationToken = default) {

        if (query == null) {
            throw new ArgumentNullException(nameof(query));
        }

        string destination = ResolveDestination(outputPath);
        string source = Path.GetFullPath(query.Path.Trim().Trim('"', '\''));
        if (string.Equals(source, destination, StringComparison.OrdinalIgnoreCase)) {
            throw new IOException("The export destination cannot overwrite the source event log.");
        }

        return ExportCore(
            destination,
            format,
            overwrite,
            stream => WriteFile(query, format, stream, cancellationToken));
    }

    /// <summary>
    /// Exports a local or remote channel query to a file and atomically promotes the completed output.
    /// </summary>
    /// <param name="query">Channel query and projection options.</param>
    /// <param name="outputPath">Destination file.</param>
    /// <param name="format">Streaming output format.</param>
    /// <param name="overwrite">Whether an existing destination may be replaced after a successful export.</param>
    /// <param name="cancellationToken">Token used to cancel enumeration and leave the existing destination unchanged.</param>
    /// <returns>Count, size, and SHA-256 for the completed output.</returns>
    public static EventExportResult ExportChannel(
        EventLogChannelQuery query,
        string outputPath,
        EventExportFormat format,
        bool overwrite = false,
        CancellationToken cancellationToken = default) {

        if (query == null) {
            throw new ArgumentNullException(nameof(query));
        }

        return ExportCore(
            ResolveDestination(outputPath),
            format,
            overwrite,
            stream => WriteChannel(query, format, stream, cancellationToken));
    }

    private static EventExportResult ExportCore(
        string destination,
        EventExportFormat format,
        bool overwrite,
        Func<Stream, long> write) {

        string? directory = Path.GetDirectoryName(destination);
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory)) {
            throw new DirectoryNotFoundException($"Output directory '{directory}' does not exist.");
        }
        if (File.Exists(destination) && !overwrite) {
            throw new IOException($"Output file '{destination}' already exists.");
        }

        string temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(destination)}.{Guid.NewGuid():N}.tmp");
        long count;
        try {
            using (var stream = new FileStream(
                       temporaryPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None,
                       1024 * 1024,
                       FileOptions.SequentialScan)) {
                count = write(stream);
                stream.Flush(flushToDisk: true);
            }

            if (File.Exists(destination)) {
                File.Replace(temporaryPath, destination, null);
            } else {
                File.Move(temporaryPath, destination);
            }

            var info = new FileInfo(destination);
            return new EventExportResult(
                destination,
                format,
                count,
                info.Length,
                ComputeSha256(destination));
        } finally {
            if (File.Exists(temporaryPath)) {
                File.Delete(temporaryPath);
            }
        }
    }

    private static long WriteFile(
        EventLogFileQuery query,
        EventExportFormat format,
        Stream stream,
        CancellationToken cancellationToken) {

        if (format == EventExportFormat.Xml) {
            using var writer = new EventXmlWriter(stream);
            long count = EventLogEngine.CopyFileXml(
                query,
                writer.EventStream,
                cancellationToken);
            writer.Complete();
            return count;
        }

        return WriteProjectedEvents(
            EventLogEngine.ReadFile(query, cancellationToken),
            format,
            stream,
            cancellationToken);
    }

    private static long WriteChannel(
        EventLogChannelQuery query,
        EventExportFormat format,
        Stream stream,
        CancellationToken cancellationToken) {

        if (format != EventExportFormat.Xml) {
            return WriteProjectedEvents(
                EventLogEngine.ReadChannel(query, cancellationToken),
                format,
                stream,
                cancellationToken);
        }

        EventLogChannelQuery xmlQuery = CopyChannelQuery(
            query,
            EventReadMode.StructuredData);
        long count = 0;
        using var writer = new EventXmlWriter(stream);
        foreach (EventObject eventObject in EventLogEngine.ReadChannel(
                     xmlQuery,
                     cancellationToken)) {
            cancellationToken.ThrowIfCancellationRequested();
            writer.WriteXml(eventObject.XMLData);
            count++;
        }
        writer.Complete();
        return count;
    }

    private static long WriteProjectedEvents(
        IEnumerable<EventObject> events,
        EventExportFormat format,
        Stream stream,
        CancellationToken cancellationToken) {

        long count = 0;
        using IEventExportWriter writer = CreateWriter(format, stream);
        foreach (EventObject eventObject in events) {
            cancellationToken.ThrowIfCancellationRequested();
            writer.Write(eventObject);
            count++;
        }
        writer.Complete();
        return count;
    }

    private static EventLogChannelQuery CopyChannelQuery(
        EventLogChannelQuery source,
        EventReadMode readMode) {

        return new EventLogChannelQuery(source.LogName) {
            MachineName = source.MachineName,
            XPath = source.XPath,
            Oldest = source.Oldest,
            ReadMode = readMode,
            MessageCulture = source.MessageCulture,
            MaxEvents = source.MaxEvents,
            RemoteTimeoutMilliseconds = source.RemoteTimeoutMilliseconds,
            BufferCapacity = source.BufferCapacity,
            RpcEndpointPort = source.RpcEndpointPort
        };
    }

    private static IEventExportWriter CreateWriter(
        EventExportFormat format,
        Stream stream) {

        return format switch {
            EventExportFormat.Csv => new EventCsvWriter(stream),
            EventExportFormat.JsonLines => new EventJsonLinesWriter(stream),
            _ => throw new ArgumentOutOfRangeException(
                nameof(format),
                format,
                "Unsupported event export format.")
        };
    }

    private static string ResolveDestination(string outputPath) {
        if (string.IsNullOrWhiteSpace(outputPath)) {
            throw new ArgumentException("Output path cannot be null or empty.", nameof(outputPath));
        }
        return Path.GetFullPath(outputPath.Trim().Trim('"', '\''));
    }

    private static string ComputeSha256(string path) {
        using SHA256 sha256 = SHA256.Create();
        using FileStream stream = File.OpenRead(path);
        return BitConverter.ToString(sha256.ComputeHash(stream))
            .Replace("-", string.Empty);
    }
}
