# Named-event rule architecture

Named events turn raw Windows records into scenario-specific objects such as
failed logons, account lockouts, Group Policy changes, Kerberos failures, or
AAD Connect health signals.

The rule layer is a projection over the shared native engine:

1. A `NamedEventQuery` selects rules, machines, time, limits, culture, and
   enrichment.
2. `NamedEventEngine.ReadAsync` asks each rule for its source channel and event
   IDs.
3. Sources are grouped and partitioned into bounded
   `EventLogChannelQuery` instances.
4. `EventLogEngine.ReadBatchAsync` performs the native Windows queries.
5. Matching `EventObject` records are projected to `NamedEventRecord` rule
   results in source order.
6. Optional enrichment and checkpoint observation happen before a result is
   emitted.

The removed `SearchEvents` facade is not part of this flow.

## Rule contract

The normal rule inherits from `EventRuleBase` and owns its source metadata,
predicate, and projection:

```csharp
namespace EventViewerX.Rules.ActiveDirectory;

public sealed class ADComputerCreateChange : EventRuleBase {
    public override List<int> EventIds => new() { 4741, 4742 };
    public override string LogName => "Security";
    public override NamedEvents NamedEvent =>
        NamedEvents.ADComputerCreateChange;

    public override bool CanHandle(EventObject eventObject) {
        return true;
    }

    public ADComputerCreateChange(EventObject eventObject)
        : base(eventObject) {

        Type = nameof(ADComputerCreateChange);
    }
}
```

Use `CanHandle` when several rules share an event ID or when an XML payload
field distinguishes the scenario:

```csharp
public override bool CanHandle(EventObject eventObject) {
    return eventObject.Data.TryGetValue(
               "ObjectClass",
               out string? objectClass) &&
           string.Equals(
               objectClass,
               "computer",
               StringComparison.OrdinalIgnoreCase);
}
```

The constructor maps the raw event into stable, scenario-specific properties.
Keep provider-specific parsing in the rule. Keep query, culture, remote
session, cancellation, export, and checkpoint logic in the shared engines.

## Adding a rule

1. Add the public scenario name to `NamedEvents`.
2. Add one focused `EventRuleBase` implementation under the appropriate
   `Rules/<Area>` folder.
3. Declare the exact channel and positive event IDs.
4. Implement `CanHandle` for any provider-specific discriminator.
5. Map only useful, stable fields in the constructor.
6. Add a focused projection test with representative `EventObject` data.
7. Add live validation when the provider is available in the test lab.

No central event-ID table is required. Rule metadata stays with the
projection that understands it.

## Discovery modes

`NamedEventRecord` supports three discovery modes:

| Mode | Behavior | Use |
| --- | --- | --- |
| `Auto` | Combines explicitly registered factories with discovered rule types. | Default library and PowerShell use. |
| `Reflection` | Discovers concrete `EventRuleBase`/`IEventRule` types from EventViewerX. | Conventional runtime hosts. |
| `ExplicitOnly` | Uses only delegate factories registered before first query. | AOT, trimming, or tightly controlled hosts. |

Configure discovery once, before the first named-event query:

```csharp
NamedEventCatalog.Configure(EventRuleDiscoveryMode.ExplicitOnly);

NamedEventCatalog.RegisterRuleFactory(
    NamedEvents.ADUserLockouts,
    "Security",
    new[] { 4740 },
    eventObject => new ADUserLockouts(eventObject),
    eventObject => eventObject.Id == 4740,
    typeof(ADUserLockouts));
```

Registration after initialization is rejected. This avoids partially changing
the rule catalog while queries are active.

## Querying from C#

```csharp
var query = new NamedEventQuery(new[] {
    NamedEvents.ADUserLogonFailed,
    NamedEvents.ADUserLockouts
}) {
    MachineNames = new string?[] { "DC01", "DC02" },
    TimePeriod = TimePeriod.Last24Hours,
    ReadMode = EventReadMode.Full,
    MaxConcurrency = 4,
    MaxEvents = 500,
    ContinueOnRemoteFailure = true
};

var execution = new NamedEventsQueryExecutionInfo();

await foreach (NamedEventRecord item in
               NamedEventEngine.ReadAsync(query, execution)) {
    Console.WriteLine(
        $"{item.When:u} {item.Type} {item.Computer}");
}
```

`MaxEvents` limits matching projected results. `MaxCandidates` separately
limits raw records evaluated by rules. This distinction prevents a selective
rule from silently returning too few matches.

`NamedEventsQueryExecutionInfo` reports candidates examined, results emitted,
source failures, and limit state without changing the returned object stream.

## Querying from PowerShell

```powershell
Get-EVXEvent `
    -NamedEvent ADUserLogonFailed, ADUserLockouts `
    -MachineName DC01, DC02 `
    -TimePeriod Last24Hours `
    -MaxConcurrency 4 `
    -MaxEvents 500 |
    Select-Object TimeCreated, NamedEventName, MachineName, UserName, IpAddress
```

PowerShell is a thin adapter: it builds `NamedEventQuery`, supplies durable
checkpoint callbacks and optional DNS enrichment, and streams
`NamedEventEngine.ReadAsync`.

## Ordering, failures, and checkpoints

- Batch results are merged deterministically.
- Rule projection and optional enrichment complete before the checkpoint
  observer advances.
- A failed remote target can be isolated with
  `ContinueOnRemoteFailure`; local and programming failures remain terminal.
- Durable PowerShell checkpoints are scoped by machine and channel, guarded by
  a shared lock, and include generation metadata so a reset cannot be undone
  by an in-flight query.
- Native bookmarks are opt-in and remain part of the underlying event when
  requested.

## Enrichment

Enrichment is optional and ordered. Reverse DNS, for example, is bounded by a
whole-operation timeout and a concurrency limit. Failure or timeout annotates
the result rather than removing the event or advancing its checkpoint early.

Rules must remain useful without network enrichment. Provider payload data is
the source of truth; enrichment is additional context.

## Key files

- `NamedEventQuery.cs` — public scenario query contract.
- `NamedEventEngine.cs` and `NamedEventEngine.Projection.cs` — batching,
  ordered projection, and limits.
- `NamedEventRecord.cs` — discovery, explicit registration, and rule creation.
- `IEventRule.cs` — `IEventRule` and `EventRuleBase` contracts.
- `NamedEventEnricher.cs` and `Enrichment/` — optional ordered enrichment.
- `Rules/` — provider/scenario projections.

## Design rules

- One source of truth for a scenario's channel, IDs, predicate, and mapping.
- No independent query engine inside a rule.
- No network call unless enrichment is explicitly enabled.
- No swallowed projection failures: errors are reported with the affected rule.
- No placeholder rules. Add a rule when its provider contract and useful
  projection are understood and testable.
- Prefer a focused rule over a large switch statement or central mapping table.
