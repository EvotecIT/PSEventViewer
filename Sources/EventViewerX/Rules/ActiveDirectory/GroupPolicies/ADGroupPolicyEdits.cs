namespace EventViewerX.Rules.ActiveDirectory;

public class ADGroupPolicyEdits : EventObjectSlim {
    public string Computer;
    public string Action;
    public string ObjectClass;
    public string OperationType;
    public string Who;


    public ADGroupPolicyEdits(EventObject eventObject) : base(eventObject) {

    }
}