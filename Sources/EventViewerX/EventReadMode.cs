namespace EventViewerX;

/// <summary>
/// Controls how much provider data is materialized while reading an event.
/// </summary>
public enum EventReadMode {
    /// <summary>
    /// Captures event metadata only. Provider message formatting and XML parsing are skipped.
    /// </summary>
    Metadata,

    /// <summary>
    /// Captures metadata and the provider-formatted message without reading event XML.
    /// </summary>
    Message,

    /// <summary>
    /// Captures metadata and structured XML data without formatting the provider message.
    /// </summary>
    StructuredData,

    /// <summary>
    /// Captures metadata, the formatted message, structured XML data, and attachments.
    /// </summary>
    Full
}
