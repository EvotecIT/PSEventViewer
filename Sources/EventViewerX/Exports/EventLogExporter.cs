using System;
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
        if (string.IsNullOrWhiteSpace(outputPath)) {
            throw new ArgumentException("Output path cannot be null or empty.", nameof(outputPath));
        }
        if (format == EventExportFormat.Xml &&
            query.ReadMode != EventReadMode.StructuredData &&
            query.ReadMode != EventReadMode.Full) {
            throw new ArgumentException(
                "XML export requires StructuredData or Full read mode.",
                nameof(query));
        }

        string destination = Path.GetFullPath(outputPath.Trim().Trim('"', '\''));
        string source = Path.GetFullPath(query.Path.Trim().Trim('"', '\''));
        if (string.Equals(source, destination, StringComparison.OrdinalIgnoreCase)) {
            throw new IOException("The export destination cannot overwrite the source event log.");
        }
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
        long count = 0;
        try {
            using (var stream = new FileStream(
                       temporaryPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None,
                       1024 * 1024,
                       FileOptions.SequentialScan)) {
                using IEventExportWriter writer = CreateWriter(format, stream);
                foreach (EventObject eventObject in EventLogEngine.ReadFile(query, cancellationToken)) {
                    cancellationToken.ThrowIfCancellationRequested();
                    writer.Write(eventObject);
                    count++;
                }
                writer.Complete();
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

    private static IEventExportWriter CreateWriter(
        EventExportFormat format,
        Stream stream) {

        return format switch {
            EventExportFormat.Csv => new EventCsvWriter(stream),
            EventExportFormat.JsonLines => new EventJsonLinesWriter(stream),
            EventExportFormat.Xml => new EventXmlWriter(stream),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported event export format.")
        };
    }

    private static string ComputeSha256(string path) {
        using SHA256 sha256 = SHA256.Create();
        using FileStream stream = File.OpenRead(path);
        return BitConverter.ToString(sha256.ComputeHash(stream))
            .Replace("-", string.Empty);
    }
}
