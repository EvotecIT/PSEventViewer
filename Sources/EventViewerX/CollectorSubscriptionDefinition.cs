using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace EventViewerX;

/// <summary>Delivery mode used by a collector-initiated Windows Event Collector subscription.</summary>
public enum CollectorSubscriptionDeliveryMode {
    /// <summary>The collector polls event sources.</summary>
    Pull,
    /// <summary>Event sources push to the collector.</summary>
    Push
}

/// <summary>Payload representation delivered to the collector.</summary>
public enum CollectorSubscriptionContentFormat {
    /// <summary>Raw event XML without publisher-rendered text.</summary>
    Events,
    /// <summary>Event XML with publisher-rendered localized text.</summary>
    RenderedText
}

/// <summary>One collector-initiated event source.</summary>
public sealed class CollectorSubscriptionSource {
    /// <summary>Creates an enabled source for the supplied computer address.</summary>
    public CollectorSubscriptionSource(string address) {
        if (string.IsNullOrWhiteSpace(address)) {
            throw new ArgumentException("Event source address cannot be empty.", nameof(address));
        }
        Address = address.Trim();
    }

    /// <summary>FQDN, NetBIOS name, or IP address of the event source.</summary>
    public string Address { get; }

    /// <summary>Whether this source participates in collection.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Optional source-specific user name. Passwords are intentionally not stored in definitions.</summary>
    public string? UserName { get; set; }
}

/// <summary>
/// Typed, serializable definition for a collector-initiated Windows Event Collector subscription.
/// </summary>
public sealed class CollectorSubscriptionDefinition {
    /// <summary>Unique subscription identifier.</summary>
    public string SubscriptionId { get; set; } = string.Empty;

    /// <summary>Operator-facing description.</summary>
    public string? Description { get; set; }

    /// <summary>Whether collection starts enabled.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Complete Windows Event Log QueryList XML.</summary>
    public string QueryXml { get; set; } = string.Empty;

    /// <summary>Source computers in a collector-initiated subscription.</summary>
    public IReadOnlyList<CollectorSubscriptionSource> Sources { get; set; } =
        Array.Empty<CollectorSubscriptionSource>();

    /// <summary>Whether existing source events are collected before future events.</summary>
    public bool ReadExistingEvents { get; set; }

    /// <summary>Pull or push delivery.</summary>
    public CollectorSubscriptionDeliveryMode DeliveryMode { get; set; } =
        CollectorSubscriptionDeliveryMode.Pull;

    /// <summary>Maximum items in a delivery batch.</summary>
    public int MaxItems { get; set; } = 1;

    /// <summary>Maximum delivery latency in milliseconds.</summary>
    public int MaxLatencyMilliseconds { get; set; } = 1000;

    /// <summary>Heartbeat or polling interval in milliseconds.</summary>
    public int HeartbeatIntervalMilliseconds { get; set; } = 40000;

    /// <summary>HTTP or HTTPS transport.</summary>
    public string TransportName { get; set; } = "HTTP";

    /// <summary>Optional explicit transport port. Zero uses the Windows default.</summary>
    public int TransportPort { get; set; }

    /// <summary>Raw events or publisher-rendered text.</summary>
    public CollectorSubscriptionContentFormat ContentFormat { get; set; } =
        CollectorSubscriptionContentFormat.Events;

    /// <summary>Locale used for rendered text.</summary>
    public CultureInfo Locale { get; set; } = CultureInfo.GetCultureInfo("en-US");

    /// <summary>Destination channel on the collector.</summary>
    public string DestinationLog { get; set; } = "ForwardedEvents";

    /// <summary>Publisher that owns or imports the destination channel.</summary>
    public string PublisherName { get; set; } = "Microsoft-Windows-EventCollector";

    /// <summary>Validates and serializes the definition as WEC subscription XML.</summary>
    public string ToXml() {
        Validate();
        XNamespace ns = "http://schemas.microsoft.com/2006/03/windows/events/subscription";
        var root = new XElement(ns + "Subscription",
            new XElement(ns + "SubscriptionId", SubscriptionId.Trim()),
            new XElement(ns + "SubscriptionType", "CollectorInitiated"),
            new XElement(ns + "Description", Description?.Trim() ?? string.Empty),
            new XElement(ns + "Enabled", Enabled.ToString().ToLowerInvariant()),
            new XElement(ns + "Uri", "http://schemas.microsoft.com/wbem/wsman/1/windows/EventLog"),
            new XElement(ns + "ConfigurationMode", "Custom"),
            new XElement(ns + "Delivery",
                new XAttribute("Mode", DeliveryMode.ToString()),
                new XElement(ns + "Batching",
                    new XElement(ns + "MaxItems", MaxItems),
                    new XElement(ns + "MaxLatencyTime", MaxLatencyMilliseconds)),
                new XElement(ns + "PushSettings",
                    new XElement(ns + "Heartbeat",
                        new XAttribute("Interval", HeartbeatIntervalMilliseconds)))),
            new XElement(ns + "Query", new XCData(NormalizeQueryXml(QueryXml))),
            new XElement(ns + "ReadExistingEvents", ReadExistingEvents.ToString().ToLowerInvariant()),
            new XElement(ns + "TransportName", TransportName.Trim().ToUpperInvariant()),
            new XElement(ns + "ContentFormat", ContentFormat.ToString()),
            ContentFormat == CollectorSubscriptionContentFormat.RenderedText
                ? new XElement(ns + "Locale", new XAttribute("Language", Locale.Name))
                : null,
            new XElement(ns + "LogFile", DestinationLog.Trim()),
            new XElement(ns + "PublisherName", PublisherName.Trim()),
            new XElement(ns + "CredentialsType", "Default"));
        if (TransportPort > 0) {
            root.Add(new XElement(ns + "TransportPort", TransportPort));
        }
        root.Add(new XElement(ns + "EventSources",
            Sources.Select(source =>
                new XElement(ns + "EventSource",
                    new XAttribute("Enabled", source.Enabled.ToString().ToLowerInvariant()),
                    new XElement(ns + "Address", source.Address),
                    string.IsNullOrWhiteSpace(source.UserName)
                        ? null
                        : new XElement(ns + "UserName", source.UserName!.Trim())))));

        return new XDocument(new XDeclaration("1.0", "utf-8", null), root)
            .ToString(SaveOptions.DisableFormatting);
    }

    /// <summary>Validates the definition without changing machine state.</summary>
    public void Validate() {
        if (string.IsNullOrWhiteSpace(SubscriptionId)) {
            throw new ArgumentException("SubscriptionId cannot be empty.", nameof(SubscriptionId));
        }
        if (Sources == null || Sources.Count == 0) {
            throw new ArgumentException("At least one collector event source is required.", nameof(Sources));
        }
        if (Sources.Any(static source => source == null)) {
            throw new ArgumentException("Sources cannot contain null entries.", nameof(Sources));
        }
        if (MaxItems <= 0) {
            throw new ArgumentOutOfRangeException(nameof(MaxItems), "MaxItems must be greater than zero.");
        }
        if (MaxLatencyMilliseconds <= 0 || HeartbeatIntervalMilliseconds <= 0) {
            throw new ArgumentOutOfRangeException(nameof(MaxLatencyMilliseconds), "Delivery intervals must be greater than zero.");
        }
        string transport = TransportName?.Trim() ?? string.Empty;
        if (!string.Equals(transport, "HTTP", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(transport, "HTTPS", StringComparison.OrdinalIgnoreCase)) {
            throw new ArgumentException("TransportName must be HTTP or HTTPS.", nameof(TransportName));
        }
        if (TransportPort < 0 || TransportPort > ushort.MaxValue) {
            throw new ArgumentOutOfRangeException(nameof(TransportPort));
        }
        if (string.IsNullOrWhiteSpace(DestinationLog) || string.IsNullOrWhiteSpace(PublisherName)) {
            throw new ArgumentException("DestinationLog and PublisherName cannot be empty.");
        }
        if (ContentFormat == CollectorSubscriptionContentFormat.RenderedText && Locale == null) {
            throw new ArgumentException("Locale is required when ContentFormat is RenderedText.", nameof(Locale));
        }
        _ = NormalizeQueryXml(QueryXml);
    }

    internal static string NormalizeQueryXml(string queryXml) {
        if (string.IsNullOrWhiteSpace(queryXml)) {
            throw new ArgumentException("QueryXml cannot be empty.", nameof(queryXml));
        }
        try {
            using var reader = XmlReader.Create(new StringReader(queryXml), new XmlReaderSettings {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreWhitespace = true
            });
            XDocument query = XDocument.Load(reader, LoadOptions.None);
            if (!string.Equals(query.Root?.Name.LocalName, "QueryList", StringComparison.Ordinal)) {
                throw new ArgumentException("QueryXml root must be QueryList.", nameof(queryXml));
            }
            return query.ToString(SaveOptions.DisableFormatting);
        } catch (XmlException exception) {
            throw new ArgumentException("QueryXml is not valid XML.", nameof(queryXml), exception);
        }
    }
}