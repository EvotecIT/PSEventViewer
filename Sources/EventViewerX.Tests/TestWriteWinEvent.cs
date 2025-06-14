using System.Diagnostics;
using System.Linq;
using Xunit;

namespace EventViewerX.Tests {
    public class TestWriteWinEvent {
        [Fact]
        public void MachineNameParameterAllowsNull() {
            var field = typeof(PSEventViewer.CmdletWriteWinEvent).GetField("MachineName");
            Assert.NotNull(field);
            var hasAllowNull = field.CustomAttributes.Any(a => a.AttributeType.Name == "AllowNullAttribute");
            Assert.True(hasAllowNull);
        }

        [Fact]
        public void WriteEventDoesNotThrowWhenMachineIsNull() {
            if (!OperatingSystem.IsWindows()) return;
            SearchEvents.WriteEvent(
                "Windows PowerShell",
                "Application",
                "Codex test message",
                EventLogEntryType.Information,
                1,
                null!);
        }
    }
}
