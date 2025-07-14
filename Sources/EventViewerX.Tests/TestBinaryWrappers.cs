using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace EventViewerX.Tests
{

    /// <summary>
    /// Unit tests for BinaryWrappers helpers.
    /// </summary>
    public class TestBinaryWrappers {
        [Fact]
        public void LogNamesEmptyWhenChannelReferencesMissing() {
            if (!OperatingSystem.IsWindows()) return;
            const string metadata = "name: Test\nguid: {00000000-0000-0000-0000-000000000000}\n";
            var method = typeof(EventViewerX.BinaryWrappers).GetMethod("GetMetadataProperty", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            var channelReferences = method!.Invoke(null, new object[] { metadata, "channelReferences" }) as string;
            Assert.Null(channelReferences);
            string[] logNames = channelReferences?.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray() ?? Array.Empty<string>();
            Assert.Empty(logNames);
        }
    }
}
