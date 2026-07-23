using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;

namespace EventViewerX.Native;

internal readonly struct NativeEventMessage {
    internal NativeEventMessage(
        NativeEventMetadata metadata,
        string message,
        string levelDisplayName,
        string taskDisplayName,
        string opcodeDisplayName,
        IReadOnlyList<string> keywordDisplayNames,
        EventBookmark? bookmark) {

        Metadata = metadata;
        Message = message;
        LevelDisplayName = levelDisplayName;
        TaskDisplayName = taskDisplayName;
        OpcodeDisplayName = opcodeDisplayName;
        KeywordDisplayNames = keywordDisplayNames;
        Bookmark = bookmark;
    }

    internal NativeEventMetadata Metadata { get; }
    internal string Message { get; }
    internal string LevelDisplayName { get; }
    internal string TaskDisplayName { get; }
    internal string OpcodeDisplayName { get; }
    internal IReadOnlyList<string> KeywordDisplayNames { get; }
    internal EventBookmark? Bookmark { get; }
}
