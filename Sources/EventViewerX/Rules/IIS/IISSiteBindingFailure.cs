using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace EventViewerX.Rules.IIS;

/// <summary>
/// IIS site binding failure
/// 1007: The World Wide Web Publishing Service did not register the URL prefix for a site
/// </summary>
public class IISSiteBindingFailure : EventRuleBase {
    /// <inheritdoc />
    public override List<int> EventIds => new() { 1007 };
    /// <inheritdoc />
    public override string LogName => "System";
    /// <inheritdoc />
    public override NamedEvents NamedEvent => NamedEvents.IISSiteBindingFailure;

    /// <summary>Accepts IIS binding failure events (1007).</summary>
    public override bool CanHandle(EventObject eventObject) {
        return true;
    }

    /// <summary>Machine hosting IIS.</summary>
    public string Computer;
    /// <summary>Site name reported by the event.</summary>
    public string SiteName;
    /// <summary>Binding that failed to register.</summary>
    public string Binding;
    /// <summary>Event timestamp.</summary>
    public DateTime When;

    /// <summary>Initialises an IIS binding failure wrapper from an event record.</summary>
    public IISSiteBindingFailure(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "IISSiteBindingFailure";
        Computer = _eventObject.ComputerName;
        SiteName = _eventObject.GetValueFromDataDictionary("SiteName", "Site");
        Binding = _eventObject.GetValueFromDataDictionary("BindingInfo", "BindingInformation");
        When = _eventObject.TimeCreated;

        if (string.IsNullOrEmpty(SiteName) || string.IsNullOrEmpty(Binding)) {
            ParseMessage(_eventObject.Message);
        }
    }

    private void ParseMessage(string message) {
        if (string.IsNullOrEmpty(message)) return;
        var match = Regex.Match(message, @"URL prefix\s+(?<binding>\S+).*?site\s+['""]?(?<site>[^'""]+)");
        if (match.Success) {
            if (string.IsNullOrEmpty(Binding)) {
                Binding = match.Groups["binding"].Value.Trim('"', '\'');
            }
            if (string.IsNullOrEmpty(SiteName)) {
                SiteName = match.Groups["site"].Value.Trim('"', '\'');
            }
        }
    }
}
