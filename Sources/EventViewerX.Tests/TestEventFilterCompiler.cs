using System.Xml.Linq;
using Xunit;

namespace EventViewerX.Tests;

public sealed class TestEventFilterCompiler {
    [Fact]
    public void TypedFilterBuildsTheExpectedNativeDimensions() {
        var filter = new EventFilter {
            EventIds = new[] { 4624, 4625 },
            RecordIds = new long[] { 10, 11 },
            ProviderNames = new[] {
                "Microsoft-Windows-Security-Auditing",
                "Custom Provider"
            },
            Levels = new byte[] { 2, 3 },
            Keywords = new long[] { 1, 2 },
            StartTime = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2026, 7, 2, 0, 0, 0, DateTimeKind.Utc),
            UserIds = new[] { "S-1-5-18" },
            Data = new[] { "payload" },
            NamedData = new Dictionary<string, IReadOnlyList<string>> {
                ["TargetUserName"] = new[] { "alice", "bob" }
            },
            ExcludedEventIds = new[] { 4634 }
        };

        string xpath = EventFilterCompiler.BuildXPath(filter);

        Assert.Contains("EventID=4624", xpath, StringComparison.Ordinal);
        Assert.Contains("EventID=4625", xpath, StringComparison.Ordinal);
        Assert.Contains("EventID!=4634", xpath, StringComparison.Ordinal);
        Assert.Contains("EventRecordID=10", xpath, StringComparison.Ordinal);
        Assert.Contains("Microsoft-Windows-Security-Auditing", xpath, StringComparison.Ordinal);
        Assert.Contains("Level=2", xpath, StringComparison.Ordinal);
        Assert.Contains("band(Keywords,3)", xpath, StringComparison.Ordinal);
        Assert.Contains("S-1-5-18", xpath, StringComparison.Ordinal);
        Assert.Contains("TargetUserName", xpath, StringComparison.Ordinal);
        Assert.Contains("alice", xpath, StringComparison.Ordinal);
    }

    [Fact]
    public void StructuredQuerySupportsSeveralChannelsAndSuppressions() {
        string queryXml = EventFilterCompiler.BuildChannelQueryXml(
            new[] { "System", "Application", "System" },
            new EventFilter {
                Levels = new byte[] { 2, 3 }
            },
            new EventFilter {
                ProviderNames = new[] { "Noisy Provider" }
            });

        XDocument document = XDocument.Parse(queryXml);
        XElement[] queries = document.Root!.Elements("Query").ToArray();
        Assert.Equal(2, queries.Length);
        Assert.Equal(
            new[] { "System", "Application" },
            queries.Select(static query => query.Attribute("Path")!.Value));
        Assert.All(queries, static query => {
            Assert.Contains("Level=2", query.Element("Select")!.Value, StringComparison.Ordinal);
            Assert.Contains("Noisy Provider", query.Element("Suppress")!.Value, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void ExcludedNamedDataBecomesAStructuredSuppression() {
        string queryXml = EventFilterCompiler.BuildChannelQueryXml(
            new[] { "System" },
            new EventFilter {
                EventIds = new[] { 7040 },
                ExcludedNamedData =
                    new Dictionary<string, IReadOnlyList<string>> {
                        ["param4"] = new[] { "BITS" }
                    }
            });

        XElement query = XDocument.Parse(queryXml)
            .Root!
            .Element("Query")!;
        Assert.Contains(
            "EventID=7040",
            query.Element("Select")!.Value,
            StringComparison.Ordinal);
        Assert.Equal(
            "*[EventData[Data[@Name='param4'] = 'BITS']]",
            query.Element("Suppress")!.Value);
    }

    [Fact]
    public void RawXpathRejectsExcludedNamedData() {
        var filter = new EventFilter {
            ExcludedNamedData =
                new Dictionary<string, IReadOnlyList<string>> {
                    ["FieldName"] = new[] { "Value1" }
                }
        };

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            EventFilterCompiler.BuildXPath(filter));

        Assert.Contains("QueryList", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void StructuredQueryAcceptsPartitionedSuppressionsBeyondNativeLimit() {
        var suppress = new EventFilter {
            EventIds = Enumerable.Range(1, 60).ToArray()
        };
        IReadOnlyList<EventFilter> partitions =
            EventFilterPartitioner.Partition(suppress);

        string queryXml =
            EventFilterCompiler.BuildChannelQueryXmlWithSuppressions(
            new[] { "System" },
            select: null,
            partitions);

        XElement query = XDocument.Parse(queryXml)
            .Root!
            .Element("Query")!;
        XElement[] suppressions = query
            .Elements("Suppress")
            .ToArray();
        Assert.True(suppressions.Length > 1);
        Assert.All(suppressions, static item =>
            Assert.InRange(
                item.Value.Split(new[] { "EventID=" }, StringSplitOptions.None).Length - 1,
                1,
                EventFilterCompiler.MaximumXPathExpressions));
        Assert.Equal(
            60,
            suppressions.Sum(static item =>
                item.Value.Split(new[] { "EventID=" }, StringSplitOptions.None).Length - 1));
    }

    [Fact]
    public void FilterRejectsAnInvertedTimeRange() {
        var filter = new EventFilter {
            StartTime = new DateTime(2026, 7, 2),
            EndTime = new DateTime(2026, 7, 1)
        };

        Assert.Throws<ArgumentException>(() =>
            EventFilterCompiler.BuildXPath(filter));
    }

    [Fact]
    public void ExpressionCountMatchesProviderAndNamedDataCost() {
        var filter = new EventFilter {
            EventIds = new[] { 1, 2 },
            ProviderNames = new[] { "One", "Two", "Three" },
            Keywords = new long[] { 1, 2 },
            NamedData =
                new Dictionary<string, IReadOnlyList<string>> {
                    ["User"] = new[] { "alice", "bob" }
                }
        };

        Assert.Equal(
            10,
            EventFilterCompiler.CountExpressions(filter));
        Assert.Equal(
            22,
            EventFilterCompiler.MaximumXPathExpressions);
    }

    [Fact]
    public void PartitionerPreservesCartesianAndSemanticsWithinNativeLimit() {
        var filter = new EventFilter {
            EventIds = Enumerable.Range(1, 40).ToArray(),
            ProviderNames = Enumerable.Range(1, 30)
                .Select(static value => $"Provider-{value}")
                .ToArray(),
            StartTime = new DateTime(
                2026,
                7,
                1,
                0,
                0,
                0,
                DateTimeKind.Utc)
        };

        IReadOnlyList<EventFilter> partitions =
            EventFilterPartitioner.Partition(filter);

        Assert.True(partitions.Count > 1);
        Assert.All(partitions, static partition =>
            Assert.InRange(
                EventFilterCompiler.CountExpressions(partition),
                1,
                EventFilterCompiler.MaximumXPathExpressions));
        HashSet<(int EventId, string Provider)> combinations =
            partitions
                .SelectMany(partition =>
                    partition.EventIds!.SelectMany(
                        eventId =>
                            partition.ProviderNames!.Select(
                                provider =>
                                    (eventId, provider))))
                .ToHashSet();
        Assert.Equal(40 * 30, combinations.Count);
    }
}
