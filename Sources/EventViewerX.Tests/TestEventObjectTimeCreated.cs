using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.Serialization;
using System.Security.Principal;
using EventViewerX.Reports.Stats;
using Xunit;

namespace EventViewerX.Tests;

public class TestEventObjectTimeCreated
{
    [Fact]
    public void TimeCreated_NullFallsBackToMinValue()
    {
        var record = new NullTimeEventRecord();
        var eo = new EventObject(record, "local", EventReadMode.Metadata);

        Assert.Equal(DateTime.MinValue, eo.TimeCreated);
    }

    [Fact]
    public void MissingTimeDoesNotBecomeAStatisticsExtremum()
    {
        var record = new NullTimeEventRecord();
        var eventObject =
            new EventObject(
                record,
                "local",
                EventReadMode.Metadata);
        var builder =
            new EvtxStatsReportBuilder();

        builder.Add(eventObject);

        Assert.Equal(1, builder.Scanned);
        Assert.Null(builder.MinUtc);
        Assert.Null(builder.MaxUtc);
    }

    private sealed class NullTimeEventRecord : EventRecord
    {
        public override string ProviderName => "TestProvider";
        public override string LogName => "TestLog";
        public override string MachineName => Environment.MachineName;
        public override int Id => 0;
        public override byte? Level => 4;
        public override int? Task => null;
        public override long? Keywords => null;
        public override IEnumerable<string> KeywordsDisplayNames => Array.Empty<string>();
        public override short? Opcode => 0;
        public override string OpcodeDisplayName => string.Empty;
        public override string TaskDisplayName => string.Empty;
        public override Guid? ProviderId => null;
        public override Guid? ActivityId => null;
        public override Guid? RelatedActivityId => null;
        public override int? ProcessId => 0;
        public override int? ThreadId => 0;
        public override string LevelDisplayName => "Information";
        public override string FormatDescription() => string.Empty;
        public override string FormatDescription(IEnumerable<object> values) => string.Empty;
        public override IList<EventProperty> Properties => Array.Empty<EventProperty>();
        public override DateTime? TimeCreated => null;
        public override int? Qualifiers => null;
        public override long? RecordId => 0;
        public override byte? Version => 0;
        public override SecurityIdentifier UserId => null!;
        public override EventBookmark Bookmark => null!;
        protected override void Dispose(bool disposing) { }
        public override string ToXml() => "<Event></Event>";
    }
}
