namespace EventViewerX;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Linq;

/// <summary>
/// Shared collector subscription XML normalization and comparison helpers.
/// </summary>
public static class CollectorSubscriptionXml {
    /// <summary>
    /// Tries to validate and normalize a collector subscription XML payload.
    /// </summary>
    public static bool TryNormalize(string? xml, out CollectorSubscriptionXmlDetails? details, out string? error) {
        details = null;
        error = null;

        if (string.IsNullOrWhiteSpace(xml)) {
            error = "XML cannot be null or empty.";
            return false;
        }

        try {
            using var reader = XmlReader.Create(new StringReader(xml), CreateReaderSettings());
            var document = XDocument.Load(reader, LoadOptions.None);
            if (!TryReadDetails(document, out var description, out var queries, out error)) {
                return false;
            }

            details = new CollectorSubscriptionXmlDetails {
                NormalizedXml = NormalizeXml(document),
                Description = description,
                Queries = queries
            };
            return true;
        } catch (XmlException ex) {
            error = $"Invalid XML content: {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// Determines whether two collector subscription XML payloads are equivalent after normalization.
    /// </summary>
    public static bool AreEquivalent(string? left, string? right) {
        return string.Equals(
            NormalizeForComparison(left),
            NormalizeForComparison(right),
            StringComparison.Ordinal);
    }

    private static XmlReaderSettings CreateReaderSettings() {
        return new XmlReaderSettings {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            IgnoreWhitespace = true
        };
    }

    private static bool TryReadDetails(
        XDocument document,
        out string? description,
        out IReadOnlyList<string> queries,
        out string? error) {
        description = null;
        error = null;

        var root = document.Root;
        if (root == null || !root.Name.LocalName.Equals("Subscription", StringComparison.OrdinalIgnoreCase)) {
            queries = Array.Empty<string>();
            error = "Root element must be <Subscription>.";
            return false;
        }

        description = NormalizeOptional(
            root.Elements()
                .FirstOrDefault(static element => element.Name.LocalName.Equals("Description", StringComparison.OrdinalIgnoreCase))
                ?.Value);
        queries = root
            .Descendants()
            .Where(static element => element.Name.LocalName.Equals("Select", StringComparison.OrdinalIgnoreCase))
            .Select(static element => NormalizeOptional(element.Value))
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .ToArray()!;
        return true;
    }

    private static string NormalizeXml(XDocument document) {
        using var writerBuffer = new StringWriter(CultureInfo.InvariantCulture);
        using var writer = XmlWriter.Create(writerBuffer, new XmlWriterSettings {
            OmitXmlDeclaration = true,
            Indent = false,
            NewLineHandling = NewLineHandling.None
        });

        document.WriteTo(writer);
        writer.Flush();
        return writerBuffer.ToString();
    }

    private static string? NormalizeForComparison(string? xml) {
        if (string.IsNullOrWhiteSpace(xml)) {
            return null;
        }

        return TryNormalize(xml, out var details, out _)
            ? details!.NormalizedXml
            : xml.Trim();
    }

    private static string? NormalizeOptional(string? value) {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
