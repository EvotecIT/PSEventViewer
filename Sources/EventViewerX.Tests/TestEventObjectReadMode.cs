using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Security.Principal;
using Xunit;

namespace EventViewerX.Tests;

public class TestEventObjectReadMode {
    [Fact]
    public void MetadataModeSnapshotsRecordAndDisposesNativeOwner() {
        var record = new TrackingEventRecord();

        var snapshot = new EventObject(record, "testhost", EventReadMode.Metadata);

        Assert.True(record.Disposed);
        Assert.Equal(0, record.FormatDescriptionCalls);
        Assert.Equal(0, record.ToXmlCalls);
        Assert.Equal(42, snapshot.Id);
        Assert.Equal(1234, snapshot.RecordId);
        Assert.Equal("System", snapshot.LogName);
        Assert.Equal("testhost", snapshot.GatheredFrom);
        Assert.Empty(snapshot.Message);
        Assert.Empty(snapshot.XMLData);
    }

    [Fact]
    public void MessageModeDoesNotReadOrParseXml() {
        var record = new TrackingEventRecord();

        var snapshot = new EventObject(record, "testhost", EventReadMode.Message);

        Assert.True(record.Disposed);
        Assert.Equal(1, record.FormatDescriptionCalls);
        Assert.Equal(0, record.ToXmlCalls);
        Assert.Equal("Subject", snapshot.MessageSubject);
        Assert.Equal("Value", snapshot.MessageData["Key"]);
        Assert.Empty(snapshot.Data);
    }

    [Fact]
    public void StructuredDataModeDoesNotFormatProviderMessage() {
        var record = new TrackingEventRecord();

        var snapshot = new EventObject(record, "testhost", EventReadMode.StructuredData);

        Assert.True(record.Disposed);
        Assert.Equal(0, record.FormatDescriptionCalls);
        Assert.Equal(1, record.ToXmlCalls);
        Assert.Equal("StructuredValue", snapshot.Data["Field"]);
        Assert.Empty(snapshot.Message);
        Assert.Empty(snapshot.Attachments);
    }

    [Fact]
    public void FullModeDecodesBinaryAttachments() {
        var record = new TrackingEventRecord();

        var snapshot = new EventObject(record, "testhost", EventReadMode.Full);

        byte[] attachment = Assert.Single(snapshot.Attachments);
        Assert.Equal(new byte[] { 1, 2, 255 }, attachment);
    }

    private sealed class TrackingEventRecord : EventRecord {
        internal bool Disposed { get; private set; }
        internal int FormatDescriptionCalls { get; private set; }
        internal int ToXmlCalls { get; private set; }

        public override string ProviderName => "TestProvider";
        public override string LogName => "System";
        public override string MachineName => "sourcehost";
        public override int Id => 42;
        public override byte? Level => 4;
        public override int? Task => 1;
        public override long? Keywords => 2;
        public override IEnumerable<string> KeywordsDisplayNames => new[] { "Keyword" };
        public override short? Opcode => 3;
        public override string OpcodeDisplayName => "Opcode";
        public override string TaskDisplayName => "Task";
        public override Guid? ProviderId => Guid.Parse("11111111-1111-1111-1111-111111111111");
        public override Guid? ActivityId => null;
        public override Guid? RelatedActivityId => null;
        public override int? ProcessId => 100;
        public override int? ThreadId => 200;
        public override string LevelDisplayName => "Information";
        public override IList<EventProperty> Properties => Array.Empty<EventProperty>();
        public override DateTime? TimeCreated => new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public override int? Qualifiers => null;
        public override long? RecordId => 1234;
        public override byte? Version => 1;
        public override SecurityIdentifier UserId => null!;
        public override EventBookmark Bookmark => null!;

        public override string FormatDescription() {
            FormatDescriptionCalls++;
            return "Subject\r\nKey: Value";
        }

        public override string FormatDescription(IEnumerable<object> values) {
            return FormatDescription();
        }

        public override string ToXml() {
            ToXmlCalls++;
            return "<Event><EventData><Data Name='Field'>StructuredValue</Data><Data Name='Payload' Type='Binary'>0102FF</Data></EventData></Event>";
        }

        protected override void Dispose(bool disposing) {
            Disposed = true;
        }
    }
}
