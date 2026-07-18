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

    [Fact]
    public void MapSessionFailureStatus_SessionConstructionFailureIsHostUnavailable() {
        Assert.Equal(
            EventLogDetailsStatus.HostUnavailable,
            SearchEvents.MapSessionFailureStatus(EventLogSessionOpenStatus.EventLogSessionUnavailable));
    }

    [Fact]
    public void DiagnosticProperties_DistinguishPartialFailureFromCleanSuccess() {
        var result = new EventLogDetailsResult {
            LogName = "System",
            MachineName = "AD1",
            Status = EventLogDetailsStatus.EventTimesUnavailable,
            ErrorMessage = "Newest event could not be read."
        };

        Assert.True(result.HasDiagnosticFailure);
        Assert.Contains("System", result.DiagnosticMessage, StringComparison.Ordinal);
        Assert.Contains("AD1", result.DiagnosticMessage, StringComparison.Ordinal);
        Assert.Contains(nameof(EventLogDetailsStatus.EventTimesUnavailable), result.DiagnosticMessage, StringComparison.Ordinal);
        Assert.Contains("Newest event could not be read.", result.DiagnosticMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyEventTimeFailure_ReportsTimeoutWithPartialDetailsStatus() {
        var result = new EventLogDetailsResult { Status = EventLogDetailsStatus.Success };

        SearchEvents.ApplyEventTimeFailure(result, new TimeoutException("event-time budget expired"));

        Assert.Equal(EventLogDetailsStatus.Timeout, result.Status);
        Assert.Equal(nameof(TimeoutException), result.ErrorType);
        Assert.Contains("event-time budget expired", result.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyEventTimeFailure_ReportsNonTimeoutReadFailure() {
        var result = new EventLogDetailsResult { Status = EventLogDetailsStatus.Success };

        SearchEvents.ApplyEventTimeFailure(result, new InvalidOperationException("event-time read failed"));

        Assert.Equal(EventLogDetailsStatus.EventTimesUnavailable, result.Status);
        Assert.Equal(nameof(InvalidOperationException), result.ErrorType);
        Assert.Contains("event-time read failed", result.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyEventTimeFailure_PreservesAccessDeniedClassification() {
        var result = new EventLogDetailsResult { Status = EventLogDetailsStatus.Success };

        SearchEvents.ApplyEventTimeFailure(result, new UnauthorizedAccessException("event-time access denied"));

        Assert.Equal(EventLogDetailsStatus.AccessDenied, result.Status);
        Assert.Equal(nameof(UnauthorizedAccessException), result.ErrorType);
    }

    [Fact]
    public void ApplyEventTimeFailure_PreservesEarlierPartialStatusAndAppendsDiagnostic() {
        var result = new EventLogDetailsResult {
            Status = EventLogDetailsStatus.LogInformationUnavailable,
            ErrorMessage = "Runtime information was unavailable."
        };

        SearchEvents.ApplyEventTimeFailure(result, new InvalidOperationException("event-time read failed"));

        Assert.Equal(EventLogDetailsStatus.LogInformationUnavailable, result.Status);
        Assert.Equal(nameof(InvalidOperationException), result.ErrorType);
        Assert.Contains("Runtime information was unavailable.", result.ErrorMessage, StringComparison.Ordinal);
        Assert.Contains("event-time read failed", result.ErrorMessage, StringComparison.Ordinal);
    }
}
