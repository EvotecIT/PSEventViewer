using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace EventViewerX.Rules.IIS;

/// <summary>
/// IIS site binding failure
/// 1007: The World Wide Web Publishing Service did not register the URL prefix for a site
/// </summary>
public class IISSiteBindingFailure : EventRuleBase {
    public override List<int> EventIds => new() { 1007 };
    public override string LogName => "System";
    public override NamedEvents NamedEvent => NamedEvents.IISSiteBindingFailure;

    public override bool CanHandle(EventObject eventObject) {
        return true;
    }

    public string Computer;
    public string SiteName;
    public string Binding;
    public DateTime When;

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
