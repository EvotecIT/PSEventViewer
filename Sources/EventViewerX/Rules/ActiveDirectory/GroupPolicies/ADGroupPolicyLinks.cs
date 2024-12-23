namespace EventViewerX.Rules.ActiveDirectory;

public class ADGroupPolicyLinks : EventObjectSlim {
    public string Computer;
    public string Action;
    public string ObjectClass;
    public string OperationType;
    public string Who;


    public ADGroupPolicyLinks(EventObject eventObject) : base(eventObject) {

    }
}