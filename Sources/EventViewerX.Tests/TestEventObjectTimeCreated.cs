using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.Serialization;
using System.Security.Principal;
using Xunit;

namespace EventViewerX.Tests;

public class TestEventObjectTimeCreated
{
    [Fact]
    public void TimeCreated_NullFallsBackToMinValue()
    {
        var record = new NullTimeEventRecord();
        var eo = (EventObject)FormatterServices.GetUninitializedObject(typeof(EventObject));

        SetField(eo, "_eventRecord", record);
        eo.ContainerLog = string.Empty;
        eo.XMLData = string.Empty;
        eo.GatheredFrom = "local";
        eo.GatheredLogName = string.Empty;
        eo.MessageSubject = string.Empty;
        SetProperty(eo, nameof(EventObject.MessageData), new Dictionary<string, string>());
        SetProperty(eo, nameof(EventObject.Data), new Dictionary<string, string>());
        SetProperty(eo, nameof(EventObject.Attachments), Array.Empty<byte[]>());
        SetProperty(eo, nameof(EventObject.NicIdentifiers), new List<string>());

        Assert.Equal(DateTime.MinValue, eo.TimeCreated);
    }

    private static void SetField(object target, string name, object value)
    {
        var f = target.GetType().GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        f!.SetValue(target, value);
    }

    private static void SetProperty(object target, string name, object value)
    {
        var p = target.GetType().GetProperty(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        p!.SetValue(target, value);
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
