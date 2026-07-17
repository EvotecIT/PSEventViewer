using System;
using System.Diagnostics.Eventing.Reader;
using Xunit;

namespace EventViewerX.Tests;

public class TestEventLogDetailsResult {
    [Fact]
    public void GetLogDetailsResult_KnownLog_ReturnsDetails() {
        if (!OperatingSystem.IsWindows()) return;

        EventLogDetailsResult result = SearchEvents.GetLogDetailsResult("Application");

        Assert.True(result.Success);
        Assert.Equal(EventLogDetailsStatus.Success, result.Status);
        Assert.NotNull(result.Details);
        Assert.Equal("Application", result.LogName);
    }

    [Fact]
    public void GetLogDetailsResult_MissingLog_ReturnsDiagnosticFailure() {
        if (!OperatingSystem.IsWindows()) return;

        EventLogDetailsResult result = SearchEvents.GetLogDetailsResult("Definitely-Missing-EventViewerX-UnitTest-Log");

        Assert.False(result.Success);
        Assert.Equal(EventLogDetailsStatus.LogConfigurationUnavailable, result.Status);
        Assert.Equal("Definitely-Missing-EventViewerX-UnitTest-Log", result.LogName);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorType));
    }

    [Fact]
    public void GetLogDetailsResult_ExistingSession_ReusesSession() {
        if (!OperatingSystem.IsWindows()) return;

        using var session = new EventLogSession();

        EventLogDetailsResult result = SearchEvents.GetLogDetailsResult(
            "Application",
            session,
            timeoutMs: 10000,
            machineName: Environment.MachineName);

        Assert.True(result.Success);
        Assert.Equal(EventLogDetailsStatus.Success, result.Status);
        Assert.NotNull(result.Details);
        Assert.Equal(Environment.MachineName, result.MachineName);
    }

    [Fact]
    public void OpenSessionResult_LocalMachine_ReturnsSession() {
        if (!OperatingSystem.IsWindows()) return;

        using EventLogSessionOpenResult result = SearchEvents.OpenSessionResult(null, purpose: "UnitTest", logName: "Application");

        Assert.True(result.Success);
        Assert.Equal(EventLogSessionOpenStatus.Success, result.Status);
        Assert.NotNull(result.Session);
        Assert.Equal("UnitTest", result.Purpose);
        Assert.Equal("Application", result.LogName);
    }
}
