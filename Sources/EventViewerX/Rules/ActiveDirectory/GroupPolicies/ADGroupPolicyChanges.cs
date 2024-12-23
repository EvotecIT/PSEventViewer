namespace EventViewerX.Rules.ActiveDirectory;

public class ADGroupPolicyChanges : EventObjectSlim {
    public string Computer;
    public string Action;
    public string ObjectClass;
    public string OperationType;
    public string Who;


    public ADGroupPolicyChanges(EventObject eventObject) : base(eventObject) {

    }
}