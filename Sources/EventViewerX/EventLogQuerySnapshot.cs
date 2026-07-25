using System.Globalization;
using System.Net;

namespace EventViewerX;

/// <summary>
/// Freezes mutable query options before deferred or concurrent execution.
/// </summary>
internal static class EventLogQuerySnapshot {
    internal static EventLogChannelQuery Copy(
        EventLogChannelQuery source,
        long outerLimit = 0) {

        if (source == null) {
            throw new ArgumentNullException(nameof(source));
        }
        return new EventLogChannelQuery(source.LogName) {
            MachineName = source.MachineName,
            Credential = CopyCredential(source.Credential),
            Authentication = source.Authentication,
            XPath = source.XPath,
            Oldest = source.Oldest,
            ReadMode = source.ReadMode,
            MessageCulture = CopyCulture(source.MessageCulture),
            FallbackMessageCulture =
                CopyCulture(source.FallbackMessageCulture),
            MaxEvents = ApplyLimit(source.MaxEvents, outerLimit),
            IncludeBookmark = source.IncludeBookmark,
            RemoteConnectionTimeoutMilliseconds =
                source.RemoteConnectionTimeoutMilliseconds,
            RemoteReadTimeoutMilliseconds =
                source.RemoteReadTimeoutMilliseconds,
            BufferCapacity = source.BufferCapacity,
            RpcEndpointPort = source.RpcEndpointPort,
            BookmarkXml = source.BookmarkXml,
            BookmarkOffset = source.BookmarkOffset,
            StrictBookmark = source.StrictBookmark
        };
    }

    internal static EventLogFileQuery Copy(
        EventLogFileQuery source,
        long outerLimit = 0) {

        if (source == null) {
            throw new ArgumentNullException(nameof(source));
        }
        return new EventLogFileQuery(source.Path) {
            XPath = source.XPath,
            Oldest = source.Oldest,
            ReadMode = source.ReadMode,
            MessageCulture = CopyCulture(source.MessageCulture),
            FallbackMessageCulture =
                CopyCulture(source.FallbackMessageCulture),
            MaxEvents = ApplyLimit(source.MaxEvents, outerLimit),
            IncludeBookmark = source.IncludeBookmark,
            BookmarkXml = source.BookmarkXml,
            BookmarkOffset = source.BookmarkOffset,
            StrictBookmark = source.StrictBookmark
        };
    }

    internal static EventLogStructuredQuery Copy(
        EventLogStructuredQuery source,
        long outerLimit = 0) {

        if (source == null) {
            throw new ArgumentNullException(nameof(source));
        }
        return new EventLogStructuredQuery(source.QueryXml) {
            SourceKind = source.SourceKind,
            MachineName = source.MachineName,
            Credential = CopyCredential(source.Credential),
            Authentication = source.Authentication,
            Oldest = source.Oldest,
            ReadMode = source.ReadMode,
            MessageCulture = CopyCulture(source.MessageCulture),
            FallbackMessageCulture =
                CopyCulture(source.FallbackMessageCulture),
            MaxEvents = ApplyLimit(source.MaxEvents, outerLimit),
            IncludeBookmark = source.IncludeBookmark,
            RemoteConnectionTimeoutMilliseconds =
                source.RemoteConnectionTimeoutMilliseconds,
            RemoteReadTimeoutMilliseconds =
                source.RemoteReadTimeoutMilliseconds,
            BufferCapacity = source.BufferCapacity,
            RpcEndpointPort = source.RpcEndpointPort,
            BookmarkXml = source.BookmarkXml,
            BookmarkOffset = source.BookmarkOffset,
            StrictBookmark = source.StrictBookmark,
            TolerateQueryErrors = source.TolerateQueryErrors,
            FailureHandler = source.FailureHandler
        };
    }

    private static NetworkCredential? CopyCredential(
        NetworkCredential? credential) {

        return credential == null
            ? null
            : new NetworkCredential(
                credential.UserName,
                credential.Password,
                credential.Domain);
    }

    private static CultureInfo? CopyCulture(
        CultureInfo? culture) {

        return culture == null
            ? null
            : CultureInfo.GetCultureInfo(culture.Name);
    }

    private static long ApplyLimit(
        long sourceLimit,
        long outerLimit) {

        if (outerLimit <= 0) {
            return sourceLimit;
        }
        if (sourceLimit <= 0) {
            return outerLimit;
        }
        return Math.Min(sourceLimit, outerLimit);
    }
}
