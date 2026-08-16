using EventViewerX;
using Xunit;

namespace EventViewerX.Tests;

public class TestCollectorSubscriptionDefinition {
    [Fact]
    public void RemoveIsIdempotentWhenSubscriptionIsAlreadyAbsent() {
        bool runnerCalled = false;

        CollectorSubscriptionRemovalResult result =
            CollectorSubscriptionManager
                .RemoveCollectorSubscription(
                    "Missing",
                    _ => null,
                    (_, _) => {
                        runnerCalled = true;
                        return string.Empty;
                    },
                    1,
                    TimeSpan.Zero,
                    CancellationToken.None);

        Assert.True(result.Success);
        Assert.False(result.Changed);
        Assert.Null(result.Before);
        Assert.Null(result.After);
        Assert.False(runnerCalled);
    }

    [Fact]
    public void RemoveDeletesAndVerifiesExistingSubscription() {
        CollectorSubscriptionSnapshot snapshot =
            CreateSnapshot("Existing");
        bool exists = true;
        int deleteCount = 0;

        CollectorSubscriptionRemovalResult result =
            CollectorSubscriptionManager
                .RemoveCollectorSubscription(
                    snapshot.SubscriptionName,
                    _ => exists ? snapshot : null,
                    (arguments, _) => {
                        Assert.Equal("ds", arguments[0]);
                        deleteCount++;
                        exists = false;
                        return string.Empty;
                    },
                    1,
                    TimeSpan.Zero,
                    CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(result.Changed);
        Assert.Same(snapshot, result.Before);
        Assert.Null(result.After);
        Assert.Equal(1, deleteCount);
    }

    [Fact]
    public void RemoveFailsWhenPersistedStateRemainsPresent() {
        CollectorSubscriptionSnapshot snapshot =
            CreateSnapshot("StillPresent");

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() =>
                CollectorSubscriptionManager
                    .RemoveCollectorSubscription(
                        snapshot.SubscriptionName,
                        _ => snapshot,
                        (_, _) => string.Empty,
                        2,
                        TimeSpan.Zero,
                        CancellationToken.None));

        Assert.Contains(
            "still present",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ProducesCollectorInitiatedSubscriptionWithTypedQuery() {
        var definition = new CollectorSubscriptionDefinition {
            SubscriptionId = "Security failures",
            QueryXml = "<QueryList><Query Id=\"0\" Path=\"Security\"><Select Path=\"Security\">*[System[EventID=4625]]</Select></Query></QueryList>",
            Sources = new[] {
                new CollectorSubscriptionSource("dc01.contoso.com")
            }
        };

        string xml = definition.ToXml();

        Assert.Contains("<SubscriptionType>CollectorInitiated</SubscriptionType>", xml, StringComparison.Ordinal);
        Assert.Contains("<Address>dc01.contoso.com</Address>", xml, StringComparison.Ordinal);
        Assert.Contains("<![CDATA[<QueryList>", xml, StringComparison.Ordinal);
        Assert.DoesNotContain("<Locale", xml, StringComparison.Ordinal);
    }

    [Fact]
    public void NormalizationReadsSelectsEmbeddedInQueryCData() {
        const string xml = """
            <Subscription xmlns="http://schemas.microsoft.com/2006/03/windows/events/subscription">
              <Description>Security feed</Description>
              <Query><![CDATA[
                <QueryList>
                  <Query Id="0" Path="Security">
                    <Select Path="Security">*[System[EventID=4625]]</Select>
                  </Query>
                </QueryList>
              ]]></Query>
            </Subscription>
            """;

        Assert.True(CollectorSubscriptionXml.TryNormalize(
            xml,
            out CollectorSubscriptionXmlDetails? details,
            out string? error), error);
        Assert.Equal("Security feed", details!.Description);
        Assert.Equal(new[] { "*[System[EventID=4625]]" }, details.Queries);
    }

    [Fact]
    public void ComparisonIgnoresWindowsDefaultLocaleForRawEvents() {
        const string requested = """
            <Subscription xmlns="http://schemas.microsoft.com/2006/03/windows/events/subscription">
              <SubscriptionId>Test</SubscriptionId>
              <Query><![CDATA[<QueryList><Query Id="0" Path="System"><Select Path="System">*</Select></Query></QueryList>]]></Query>
              <ContentFormat>Events</ContentFormat>
              <EventSources><EventSource Enabled="true"><Address>server01</Address></EventSource></EventSources>
            </Subscription>
            """;
        const string persisted = """
            <Subscription xmlns="http://schemas.microsoft.com/2006/03/windows/events/subscription">
              <SubscriptionId>Test</SubscriptionId>
              <Query><![CDATA[
                <QueryList>
                  <Query Id="0" Path="System"><Select Path="System">*</Select></Query>
                </QueryList>
              ]]></Query>
              <ContentFormat>Events</ContentFormat>
              <Locale Language="en-US" />
              <EventSources><EventSource Enabled="true"><Address>server01</Address></EventSource></EventSources>
            </Subscription>
            """;

        Assert.True(CollectorSubscriptionXml.AreEquivalent(requested, persisted));
    }

    [Fact]
    public void ApplyReportsUnknownStateWhenCreateRollbackFails() {
        CollectorSubscriptionDefinition definition =
            CreateDefinition("CreateRollbackFailure");
        bool exists = false;
        string mismatched = CreatePersistedXml(
            definition.SubscriptionId,
            "Different description");

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() =>
                CollectorSubscriptionManager.ApplyCollectorSubscription(
                    definition,
                    _ => exists
                        ? CreateSnapshot(definition.SubscriptionId)
                        : null,
                    (arguments, _) => {
                        switch (arguments[0]) {
                            case "cs":
                                exists = true;
                                return string.Empty;
                            case "gs":
                                return mismatched;
                            case "ds":
                                throw new InvalidOperationException(
                                    "Injected delete rollback failure.");
                            default:
                                throw new InvalidOperationException(
                                    "Unexpected WEC operation.");
                        }
                    },
                    CancellationToken.None));

        Assert.Contains(
            "persisted state is unknown",
            exception.Message,
            StringComparison.Ordinal);
        AggregateException aggregate =
            Assert.IsType<AggregateException>(exception.InnerException);
        Assert.Equal(2, aggregate.InnerExceptions.Count);
        Assert.True(exists);
    }

    [Fact]
    public void ApplyVerifiesSuccessfulCreateRollbackBeforeRethrowingOriginalFailure() {
        CollectorSubscriptionDefinition definition =
            CreateDefinition("CreateRollbackSuccess");
        bool exists = false;
        string mismatched = CreatePersistedXml(
            definition.SubscriptionId,
            "Different description");

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() =>
                CollectorSubscriptionManager.ApplyCollectorSubscription(
                    definition,
                    _ => exists
                        ? CreateSnapshot(definition.SubscriptionId)
                        : null,
                    (arguments, _) => {
                        switch (arguments[0]) {
                            case "cs":
                                exists = true;
                                return string.Empty;
                            case "gs":
                                return mismatched;
                            case "ds":
                                exists = false;
                                return string.Empty;
                            default:
                                throw new InvalidOperationException(
                                    "Unexpected WEC operation.");
                        }
                    },
                    CancellationToken.None));

        Assert.Contains(
            "did not retain the requested definition",
            exception.Message,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "state is unknown",
            exception.Message,
            StringComparison.Ordinal);
        Assert.False(exists);
    }

    [Fact]
    public void ApplyDoesNotDeleteUnownedSubscriptionWhenCreateCommandFails() {
        CollectorSubscriptionDefinition definition =
            CreateDefinition("CreateOwnershipUnknown");
        bool exists = false;
        bool deleteCalled = false;

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() =>
                CollectorSubscriptionManager.ApplyCollectorSubscription(
                    definition,
                    _ => exists
                        ? CreateSnapshot(definition.SubscriptionId)
                        : null,
                    (arguments, _) => {
                        switch (arguments[0]) {
                            case "cs":
                                exists = true;
                                throw new InvalidOperationException(
                                    "Injected create failure after another actor won the name.");
                            case "ds":
                                deleteCalled = true;
                                exists = false;
                                return string.Empty;
                            default:
                                throw new InvalidOperationException(
                                    "Unexpected WEC operation.");
                        }
                    },
                    CancellationToken.None));

        Assert.Contains(
            "persisted state is unknown",
            exception.Message,
            StringComparison.Ordinal);
        Assert.IsType<AggregateException>(exception.InnerException);
        Assert.False(deleteCalled);
        Assert.True(exists);
    }

    [Fact]
    public void ApplyDeletesProvenOwnedCreateWithoutSnapshotVisibility() {
        CollectorSubscriptionDefinition definition =
            CreateDefinition("CreateOwnedButNotVisible");
        string mismatched = CreatePersistedXml(
            definition.SubscriptionId,
            "Different description");
        int deleteCount = 0;

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() =>
                CollectorSubscriptionManager.ApplyCollectorSubscription(
                    definition,
                    _ => null,
                    (arguments, _) => {
                        switch (arguments[0]) {
                            case "cs":
                                return string.Empty;
                            case "gs":
                                return mismatched;
                            case "ds":
                                deleteCount++;
                                return string.Empty;
                            default:
                                throw new InvalidOperationException(
                                    "Unexpected WEC operation.");
                        }
                    },
                    CancellationToken.None));

        Assert.Contains(
            "did not retain the requested definition",
            exception.Message,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "state is unknown",
            exception.Message,
            StringComparison.Ordinal);
        Assert.Equal(1, deleteCount);
    }

    [Fact]
    public void ApplyReportsUnknownStateWhenUpdateRollbackFails() {
        CollectorSubscriptionDefinition definition =
            CreateDefinition("UpdateRollbackFailure");
        string previous = CreatePersistedXml(
            definition.SubscriptionId,
            "Previous description");
        string mismatched = CreatePersistedXml(
            definition.SubscriptionId,
            "Different description");
        int getCount = 0;
        int setCount = 0;

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() =>
                CollectorSubscriptionManager.ApplyCollectorSubscription(
                    definition,
                    _ => CreateSnapshot(definition.SubscriptionId),
                    (arguments, _) => {
                        switch (arguments[0]) {
                            case "gs":
                                getCount++;
                                return getCount == 1
                                    ? previous
                                    : mismatched;
                            case "ss":
                                setCount++;
                                if (setCount == 2) {
                                    throw new InvalidOperationException(
                                        "Injected update rollback failure.");
                                }
                                return string.Empty;
                            default:
                                throw new InvalidOperationException(
                                    "Unexpected WEC operation.");
                        }
                    },
                    CancellationToken.None));

        Assert.Contains(
            "persisted state is unknown",
            exception.Message,
            StringComparison.Ordinal);
        Assert.Equal(2, setCount);
        Assert.IsType<AggregateException>(exception.InnerException);
    }

    [Fact]
    public void ApplyVerifiesSuccessfulUpdateRollbackBeforeRethrowingOriginalFailure() {
        CollectorSubscriptionDefinition definition =
            CreateDefinition("UpdateRollbackSuccess");
        string previous = CreatePersistedXml(
            definition.SubscriptionId,
            "Previous description");
        string mismatched = CreatePersistedXml(
            definition.SubscriptionId,
            "Different description");
        int getCount = 0;
        int setCount = 0;

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() =>
                CollectorSubscriptionManager.ApplyCollectorSubscription(
                    definition,
                    _ => CreateSnapshot(definition.SubscriptionId),
                    (arguments, _) => {
                        switch (arguments[0]) {
                            case "gs":
                                getCount++;
                                return getCount == 1 || getCount == 3
                                    ? previous
                                    : mismatched;
                            case "ss":
                                setCount++;
                                return string.Empty;
                            default:
                                throw new InvalidOperationException(
                                    "Unexpected WEC operation.");
                        }
                    },
                    CancellationToken.None));

        Assert.Contains(
            "did not retain the requested definition",
            exception.Message,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "state is unknown",
            exception.Message,
            StringComparison.Ordinal);
        Assert.Equal(2, setCount);
        Assert.Equal(3, getCount);
    }

    private static CollectorSubscriptionDefinition CreateDefinition(
        string subscriptionId) {

        return new CollectorSubscriptionDefinition {
            SubscriptionId = subscriptionId,
            Description = "Requested description",
            QueryXml =
                "<QueryList><Query Id=\"0\" Path=\"System\"><Select Path=\"System\">*</Select></Query></QueryList>",
            Sources = new[] {
                new CollectorSubscriptionSource("server01")
            }
        };
    }

    private static CollectorSubscriptionSnapshot CreateSnapshot(
        string subscriptionId) {

        return new CollectorSubscriptionSnapshot {
            SubscriptionName = subscriptionId
        };
    }

    private static string CreatePersistedXml(
        string subscriptionId,
        string description) {

        return $"""
            <Subscription xmlns="http://schemas.microsoft.com/2006/03/windows/events/subscription">
              <SubscriptionId>{subscriptionId}</SubscriptionId>
              <SubscriptionType>CollectorInitiated</SubscriptionType>
              <Description>{description}</Description>
              <Enabled>false</Enabled>
              <Uri>http://schemas.microsoft.com/wbem/wsman/1/windows/EventLog</Uri>
              <ConfigurationMode>Normal</ConfigurationMode>
              <Delivery Mode="Pull"><Batching><MaxItems>5</MaxItems><MaxLatencyTime>15000</MaxLatencyTime></Batching><PushSettings><Heartbeat Interval="900000" /></PushSettings></Delivery>
              <Query><![CDATA[<QueryList><Query Id="0" Path="System"><Select Path="System">*</Select></Query></QueryList>]]></Query>
              <ReadExistingEvents>false</ReadExistingEvents>
              <TransportName>HTTP</TransportName>
              <ContentFormat>Events</ContentFormat>
              <LogFile>ForwardedEvents</LogFile>
              <PublisherName>Microsoft-Windows-EventCollector</PublisherName>
              <EventSources><EventSource Enabled="true"><Address>server01</Address></EventSource></EventSources>
            </Subscription>
            """;
    }
}
