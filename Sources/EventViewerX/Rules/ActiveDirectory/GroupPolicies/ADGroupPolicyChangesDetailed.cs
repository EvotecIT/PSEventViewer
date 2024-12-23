namespace EventViewerX.Rules.ActiveDirectory;

public class ADGroupPolicyChangesDetailed : EventObjectSlim {
    public string Computer;
    public string Action;
    public string ObjectClass;
    public string OperationType;
    public string Who;


    public ADGroupPolicyChangesDetailed(EventObject eventObject) : base(eventObject) {

    }
}