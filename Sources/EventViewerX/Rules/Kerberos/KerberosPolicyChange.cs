namespace EventViewerX.Rules.Kerberos;

/// <summary>
/// Kerberos policy configuration change event details.
/// </summary>
public class KerberosPolicyChange : EventRuleBase
{
    public override List<int> EventIds => new() { 4713 };
    public override string LogName => "Security";
    public override NamedEvents NamedEvent => NamedEvents.KerberosPolicyChange;

    public override bool CanHandle(EventObject eventObject)
    {
        // Simple rule - always handle if event ID and log name match
        return true;
    }
    public string Computer;
    public string Who;
    public string PolicyChanges;
    public double? KerProxyMinutes;
    public double? KerMaxRDays;
    public double? KerMaxTHours;
    public double? KerMinTMinutes;
    public bool? EnforceUserLogonRestrictions;
    public DateTime When;

    public KerberosPolicyChange(EventObject eventObject) : base(eventObject)
    {
        _eventObject = eventObject;
        Type = "KerberosPolicyChange";
        Computer = _eventObject.ComputerName;
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        PolicyChanges = _eventObject.GetValueFromDataDictionary("KerberosPolicyChange");
        When = _eventObject.TimeCreated;

        if (!string.IsNullOrEmpty(PolicyChanges) && PolicyChanges != "--")
        {
            foreach (var part in PolicyChanges.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var section = part.Trim();
                if (section.Length == 0) continue;
                var kv = section.Split(new[] { ':' }, 2);
                if (kv.Length != 2) continue;
                var name = kv[0].Trim();
                var valuePart = kv[1];
                var hexMatch = System.Text.RegularExpressions.Regex.Match(valuePart, "0x([0-9A-Fa-f]+)");
                if (!hexMatch.Success) continue;
                var newValue = Convert.ToInt64(hexMatch.Groups[1].Value, 16);

                switch (name)
                {
                    case "KerProxy":
                        KerProxyMinutes = newValue / 600000000d;
                        break;
                    case "KerMaxR":
                        KerMaxRDays = newValue / 864000000000d;
                        break;
                    case "KerMaxT":
                        KerMaxTHours = newValue / 36000000000d;
                        break;
                    case "KerMinT":
                        KerMinTMinutes = newValue / 600000000d;
                        break;
                    case "KerOpts":
                        EnforceUserLogonRestrictions = newValue == 0x80;
                        break;
                }
            }
        }
    }
}

