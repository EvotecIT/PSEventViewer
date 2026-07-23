using System;

namespace EventViewerX.Native;

internal readonly struct NativeEventQuery {
    internal NativeEventQuery(
        IntPtr session,
        string path,
        string xpath,
        WindowsEventNativeMethods.QueryFlags flags,
        string displayName,
        string? publisherMetadataPath = null,
        int messageLocale = 0,
        int nextTimeoutMilliseconds = 0) {

        Session = session;
        Path = path;
        XPath = xpath;
        Flags = flags;
        DisplayName = displayName;
        PublisherMetadataPath = publisherMetadataPath;
        MessageLocale = messageLocale;
        NextTimeoutMilliseconds = nextTimeoutMilliseconds;
    }

    internal IntPtr Session { get; }
    internal string Path { get; }
    internal string XPath { get; }
    internal WindowsEventNativeMethods.QueryFlags Flags { get; }
    internal string DisplayName { get; }
    internal string? PublisherMetadataPath { get; }
    internal int MessageLocale { get; }
    internal int NextTimeoutMilliseconds { get; }
}
