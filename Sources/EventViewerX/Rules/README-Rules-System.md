# EventViewer Rules System Documentation

## Overview

The PSEventViewer project uses a unified rule system where **all event definitions are directly embedded in EventRuleBase classes**, eliminating the need for separate event ID mappings, manual registration, or hybrid approaches. The system uses pure reflection to automatically discover and register all event rules.

## How It Works

### Unified EventRuleBase Approach

All event rules now inherit from `EventRuleBase` and define their metadata directly in the class:

```csharp
namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Active Directory Computer Created or Changed
/// 4741: A computer account was created
/// 4742: A computer account was changed
/// </summary>
public class ADComputerCreateChange : EventRuleBase {
    public override List<int> EventIds => new() { 4741, 4742 };
    public override string LogName => "Security";
    public override NamedEvents NamedEvent => NamedEvents.ADComputerCreateChange;

    public override bool CanHandle(EventObject eventObject) {
        // Simple rule - always handle if event ID and log name match
        return true;
    }

    public ADComputerCreateChange(EventObject eventObject) : base(eventObject) {
        // Initialize properties from eventObject
        Type = "ADComputerCreateChange";
        // ... your processing logic
    }
}
```

### Complex Rules with Conditions

For rules that need specific conditions, implement custom logic in `CanHandle`:

```csharp
namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Active Directory Computer Change Detailed - only for computer objects
/// </summary>
public class ADComputerChangeDetailed : EventRuleBase {
    public override List<int> EventIds => new() { 5136, 5137, 5139, 5141 };
    public override string LogName => "Security";
    public override NamedEvents NamedEvent => NamedEvents.ADComputerChangeDetailed;

    public override bool CanHandle(EventObject eventObject) {
        // Only handle computer object changes
        return eventObject.Data.TryGetValue("ObjectClass", out var objectClass) &&
               objectClass == "computer";
    }

    public ADComputerChangeDetailed(EventObject eventObject) : base(eventObject) {
        // Initialize properties from eventObject
        Type = "ADComputerChangeDetailed";
        // ... your processing logic
    }
}
```

## Adding New Event Rules

### Step 1: Add to NamedEvents Enum
Add your new event type to the `NamedEvents` enum in `Enums\NamedEvents.cs`:

```csharp
/// <summary>
/// Your new event description
/// </summary>
YourNewEvent,
```

### Step 2: Create the EventRuleBase Class

All rules use the same EventRuleBase pattern:

```csharp
namespace EventViewerX.Rules.YourCategory;

/// <summary>
/// Description of your event rule
/// Event IDs: 1234, 5678
/// </summary>
public class YourNewEvent : EventRuleBase {
    public override List<int> EventIds => new() { 1234, 5678 };
    public override string LogName => "YourLogName";
    public override NamedEvents NamedEvent => NamedEvents.YourNewEvent;

    public override bool CanHandle(EventObject eventObject) {
        // For simple rules that handle all matching events:
        return true;

        // For complex rules with conditions:
        // return eventObject.Data.TryGetValue("SomeField", out var value) &&
        //        value == "ExpectedValue";
    }

    public YourNewEvent(EventObject eventObject) : base(eventObject) {
        Type = "YourNewEvent";
        // Initialize your properties from eventObject
        // Property1 = eventObject.GetValueFromDataDictionary("DataField");
        // Property2 = eventObject.ComputerName;
        // When = eventObject.TimeCreated;
    }
}
```

### Step 3: That's It!

The system will automatically:
- ✅ Discover your rule class using reflection
- ✅ Extract event IDs and log name from your rule
- ✅ Register the rule for automatic processing
- ✅ Use your `CanHandle` method to determine when to create instances
- ✅ Query the correct logs and create rule instances as needed

**No manual registration required!**

## System Architecture

### Automatic Discovery Process

1. **Assembly Scanning**: At startup, the system scans for all classes inheriting from `EventRuleBase`
2. **Rule Registration**: For each rule class, creates a temporary instance to extract metadata:
   - `EventIds` → List of event IDs the rule handles
   - `LogName` → Windows Event Log name to query
   - `NamedEvent` → Enum value linking to the rule
3. **Event Processing**: When processing events, the system:
   - Creates rule instances for matching events
   - Calls `CanHandle()` to validate the rule should process this specific event
   - Returns the rule instance if conditions are met

### No Manual Mappings Required

The system uses **pure reflection** with no hardcoded mappings:
- Event IDs and log names come directly from rule properties
- Rule conditions are evaluated by the rules themselves
- New rules are automatically discovered on startup

## Benefits

1. **Single Source of Truth**: Event IDs, log names, and conditions are defined once in the rule class
2. **Automatic Discovery**: Zero configuration - just create the rule class and it's discovered
3. **Type Safety**: Compile-time checking ensures all rules are properly configured
4. **Extensibility**: Add new rules without touching any central configuration
5. **Maintainability**: Event definitions live with their processing logic
6. **Self-Documenting**: Rules clearly show what events they handle and how
7. **Consistent**: All rules use the same EventRuleBase pattern
8. **Testable**: Each rule can be unit tested independently

## Migration Status

### ✅ Completed
- ✅ **Unified EventRuleBase System**: All rules now use the same consistent pattern
- ✅ **Pure Reflection Discovery**: Automatic rule discovery with zero manual mappings
- ✅ **30+ Rules Migrated**: All existing rules converted to EventRuleBase
- ✅ **Manual Mappings Eliminated**: No more hardcoded event ID or rule mappings
- ✅ **Build Validation**: System compiles and works with unified approach

### 🎯 Current State
The system is **fully operational** with the unified EventRuleBase approach:
- All event rules inherit from EventRuleBase
- All metadata is auto-detected from rule classes
- No manual registration or mapping required
- Consistent behavior across simple and complex rules

### 📊 Rule Examples
- **Simple Rules**: `ADComputerCreateChange`, `ADUserLogonFailed` (return `true` in CanHandle)
- **Complex Rules**: `ADComputerChangeDetailed`, `ADUserLogonNTLMv1` (custom conditions in CanHandle)
- **All Rules**: Use same EventRuleBase pattern with override properties

## Example Usage

The system works seamlessly with existing code:

```csharp
// This automatically uses the new reflection-based system
await foreach (var eventObject in SearchEvents.FindEventsByNamedEvents([
    NamedEvents.ADComputerCreateChange,
    NamedEvents.ADComputerChangeDetailed,
    NamedEvents.ADUserLogonFailed,
    NamedEvents.ADLdapBindingDetails
])) {
    Console.WriteLine($"Found event: {eventObject.Type} from {eventObject.GatheredFrom}");
}
```

### What Happens Behind the Scenes:

1. **Query Planning**: System extracts event IDs and log names from each rule:
   ```csharp
   ADComputerCreateChange: EventIds=[4741, 4742], LogName="Security"
   ADUserLogonFailed: EventIds=[4625], LogName="Security"
   ADLdapBindingDetails: EventIds=[2889], LogName="Directory Service"
   ```

2. **Log Querying**: Groups by log name and queries efficiently:
   ```csharp
   Query "Security" log for events: 4741, 4742, 4625
   Query "Directory Service" log for events: 2889
   ```

3. **Rule Processing**: For each event found:
   ```csharp
   var rule = new ADComputerCreateChange(eventObject);
   if (rule.CanHandle(eventObject)) {
       return rule; // This specific rule handles this event
   }
   ```

4. **Result**: Returns properly typed, processed event objects with all rule-specific logic applied.

## Key Files

- **`EventRuleBase.cs`**: Abstract base class all rules inherit from
- **`IEventRule.cs`**: Interface defining the CanHandle contract
- **`EventObjectSlim.cs`**: Reflection-based discovery and rule creation system
- **`SearchEvents.NamedEventsDetails.cs`**: Updated to use reflection system
- **`Rules/`**: Directory containing all EventRuleBase rule implementations

The system automatically:
1. ✅ Discovers event IDs and log names from rule classes
2. ✅ Queries the appropriate logs efficiently
3. ✅ Creates the correct rule instances based on conditions
4. ✅ Returns properly typed event objects with rule-specific processing
