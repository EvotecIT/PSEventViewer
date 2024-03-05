using System;
using System.Collections.Generic;
using System.Text;

namespace EventViewer.Rules.ActiveDirectory {

    /// <summary>
    /// Active Directory Group Membership Changes
    /// 4728: A member was added to a security-enabled global group
    /// 4729: A member was removed from a security-enabled global group
    /// 4732: A member was added to a security-enabled local group
    /// 4733: A member was removed from a security-enabled local group
    /// 4746: A member was added to a security-enabled universal group
    /// 4747: A member was removed from a security-enabled universal group
    /// 4751: A member was added to a distribution group
    /// 4752: A member was removed from a distribution group
    /// 4756: A member was added to a security-enabled universal group
    /// 4757: A member was removed from a security-enabled universal group
    /// 4761: A member was added to a security-enabled global group
    /// 4762: A member was removed from a security-enabled global group
    /// 4785: A member was added to a security-enabled universal group
    /// 4786: A member was removed from a security-enabled universal group
    /// 4787: A member was added to a security-enabled universal group
    /// 4788: A member was removed from a security-enabled universal group
    /// </summary>
    public class ADGroupMembershipChange : EventObjectSlim {

        public string Computer;
        public string Action;
        public string GroupName;
        public string MemberName;
        public string Who;
        public DateTime When;

        public ADGroupMembershipChange(EventObject eventObject) : base(eventObject) {
            _eventObject = eventObject;
            Type = "ADGroupMembershipChange";

            Computer = _eventObject.ComputerName;
            Action = _eventObject.MessageSubject;

            GroupName = _eventObject.GetValueFromDataDictionary("TargetUserName", "TargetDomainName", "\\", reverseOrder: true);
            MemberName = _eventObject.GetValueFromDataDictionary("MemberNameWithoutCN");

            // common fields
            Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
            When = _eventObject.TimeCreated;
        }
    }

    /// <summary>
    /// Active Directory Group Enumeration
    /// 4798: A user's local group membership was enumerated
    /// 4799: A security-enabled local group membership was enumerated
    /// </summary>
    public class ADGroupEnumeration : EventObjectSlim {

        public string Computer;
        public string Action;
        public string GroupName;
        public string Who;
        public DateTime When;

        public ADGroupEnumeration(EventObject eventObject) : base(eventObject) {
            _eventObject = eventObject;
            Type = "ADGroupEnumeration";

            Computer = _eventObject.ComputerName;
            Action = _eventObject.MessageSubject;

            GroupName = _eventObject.GetValueFromDataDictionary("TargetUserName", "TargetDomainName", "\\", reverseOrder: true);

            // common fields
            Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
            When = _eventObject.TimeCreated;
        }
    }

    /// <summary>
    /// Active Directory Group Change
    /// 4735: A security-enabled local group was created
    /// 4737: A security-enabled global group was created
    /// 4745: A security-enabled universal group was created
    /// 4750: A security-enabled universal group was changed
    /// 4760: A security-enabled global group was changed
    /// 4764: A security-enabled local group was changed
    /// 4784: A security-enabled universal group was deleted
    /// 4791: A security-enabled global group was deleted
    /// </summary>
    public class ADGroupChange : EventObjectSlim {

        public string Computer;
        public string Action;
        public string GroupName;
        public string Who;
        public DateTime When;
        public string GroupTypeChange;
        public string SamAccountName;
        public string SidHistory;
        public ADGroupChange(EventObject eventObject) : base(eventObject) {
            _eventObject = eventObject;
            Type = "ADGroupChange";

            Computer = _eventObject.ComputerName;
            Action = _eventObject.MessageSubject;

            GroupName = _eventObject.GetValueFromDataDictionary("TargetUserName", "TargetDomainName", "\\", reverseOrder: true);
            GroupTypeChange = _eventObject.GetValueFromDataDictionary("GroupTypeChange");
            SamAccountName = _eventObject.GetValueFromDataDictionary("SamAccountName");
            SidHistory = _eventObject.GetValueFromDataDictionary("SidHistory");

            // common fields
            Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
            When = _eventObject.TimeCreated;
        }
    }


    /// <summary>
    /// Active Directory Group Create Delete
    /// 4727:
    /// 4730:
    /// 4731:
    /// 4734:
    /// 4744:
    /// 4748:
    /// 4749:
    /// 4753: 
    /// 4754: 
    /// 4758: 
    /// 4759: 
    /// 4763: 
    /// </summary>
    public class ADGroupCreateDelete : EventObjectSlim {
        public string Computer;
        public string Action;
        public string GroupName;
        public string Who;
        public DateTime When;

        public ADGroupCreateDelete(EventObject eventObject) : base(eventObject) {
            _eventObject = eventObject;
            Type = "ADGroupCreateDelete";

            Computer = _eventObject.ComputerName;
            Action = _eventObject.MessageSubject;

            GroupName = _eventObject.GetValueFromDataDictionary("TargetUserName", "TargetDomainName", "\\", reverseOrder: true);

            // common fields
            Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
            When = _eventObject.TimeCreated;
        }
    }

    /// <summary>
    /// Active Directory Group Change Detailed
    /// 5136: A directory service object was modified
    /// 5137: A directory service object was created
    /// 5139: A directory service object was deleted
    /// 5141: A directory service object was moved
    /// </summary>
    public class ADGroupChangeDetailed : EventObjectSlim {
        public string Computer;
        public string Action;
        public string ObjectClass;
        public string OperationType;
        public string Who;
        public DateTime When;
        public string Group; // 'User Object'
        public string FieldChanged; // 'Field Changed'
        public string FieldValue; // 'Field Value'

        public ADGroupChangeDetailed(EventObject eventObject) : base(eventObject) {
            _eventObject = eventObject;
            Type = "ADGroupChangeDetailed";

            Computer = _eventObject.ComputerName;
            Action = _eventObject.MessageSubject;

            Computer = _eventObject.ComputerName;
            Action = _eventObject.MessageSubject;
            ObjectClass = _eventObject.GetValueFromDataDictionary("ObjectClass");
            OperationType = ConvertFromOperationType(_eventObject.Data["OperationType"]);
            Group = _eventObject.GetValueFromDataDictionary("ObjectDN");
            FieldChanged = _eventObject.GetValueFromDataDictionary("AttributeLDAPDisplayName");
            FieldValue = _eventObject.GetValueFromDataDictionary("AttributeValue");

            // OverwriteByField logic
            Group = OverwriteByField(Action, "A directory service object was moved.", Group, _eventObject.GetValueFromDataDictionary("OldObjectDN"));
            FieldValue = OverwriteByField(Action, "A directory service object was moved.", FieldValue, _eventObject.GetValueFromDataDictionary("NewObjectDN"));

            // common fields
            Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
            When = _eventObject.TimeCreated;
        }
    }
}
