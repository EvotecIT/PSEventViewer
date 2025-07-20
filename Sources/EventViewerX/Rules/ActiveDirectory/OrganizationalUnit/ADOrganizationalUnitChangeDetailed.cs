using System;

namespace EventViewerX.Rules.ActiveDirectory {
    /// <summary>
    /// Represents detailed organizational unit change events.
    /// </summary>
    public class ADOrganizationalUnitChangeDetailed : EventRuleBase {


        //        ADOrganizationalUnitChangesDetailed = [ordered] @{
        //        Enabled        = $false
        //        OUEventsModify = @{
        //            Enabled          = $true
        //            Events           = 5136, 5137, 5139, 5141
        //            LogName          = 'Security'
        //            Filter           = [ordered] @{
        //                'ObjectClass' = 'organizationalUnit'
        //            }
        //            Functions        = @{
        //                'OperationType' = 'ConvertFrom-OperationType'
        //            }

        //            Fields           = [ordered] @{
        //                'Computer'                 = 'Domain Controller'
        //                'Action'                   = 'Action'
        //                'OperationType'            = 'Action Detail'
        //                'Who'                      = 'Who'
        //                'Date'                     = 'When'
        //                'ObjectDN'                 = 'Organizational Unit'
        //                'AttributeLDAPDisplayName' = 'Field Changed'
        //                'AttributeValue'           = 'Field Value'
        //                #'OldObjectDN'              = 'OldObjectDN'
        //                #'NewObjectDN'              = 'NewObjectDN'
        //                # Common Fields
        //                'RecordID'                 = 'Record ID'
        //                'ID'                       = 'Event ID'
        //                'GatheredFrom'             = 'Gathered From'
        //                'GatheredLogName'          = 'Gathered LogName'
        //            }
        //            Overwrite        = [ordered] @{
        //                'Action Detail#1' = 'Action', 'A directory service object was created.', 'Organizational Unit Created'
        //                'Action Detail#2' = 'Action', 'A directory service object was deleted.', 'Organizational Unit Deleted'
        //                'Action Detail#3' = 'Action', 'A directory service object was moved.', 'Organizational Unit Moved'
        //                #'Organizational Unit' = 'Action', 'A directory service object was moved.', 'OldObjectDN'
        //                #'Field Changed'       = 'Action', 'A directory service object was moved.', ''
        //                #'Field Value'         = 'Action', 'A directory service object was moved.', 'NewObjectDN'
        //            }
        //            # This Overwrite works in a way where you can swap one value with another value from another field within same Event
        //            # It's useful if you have an event that already has some fields used but empty and you wnat to utilize them
        //            # for some content
        //            OverwriteByField = [ordered] @{
        //                'Organizational Unit' = 'Action', 'A directory service object was moved.', 'OldObjectDN'
        //                #'Field Changed'       = 'Action', 'A directory service object was moved.', ''
        //                'Field Value'         = 'Action', 'A directory service object was moved.', 'NewObjectDN'
        //            }
        //            SortBy           = 'Record ID'
        //            Descending       = $false
        //            IgnoreWords      = @{ }
        //        }
        //        }
        public string Computer;
        public string Action;
        public OperationType OperationType;
        public string Who;
        public DateTime When;
        public string OrganizationalUnit; // 'User Object'
        public string FieldChanged; // 'Field Changed'
        public string FieldValue; // 'Field Value'
    public override List<int> EventIds => new() { 5136, 5137, 5139, 5141 };
    public override string LogName => "Security";
    public override NamedEvents NamedEvent => NamedEvents.ADOrganizationalUnitChangeDetailed;

    public override bool CanHandle(EventObject eventObject) {
        // Check if this is an organizational unit object and not the qPLik attribute
        return eventObject.Data.TryGetValue("ObjectClass", out var objectClass) &&
               objectClass == "organizationalUnit" &&
               (!eventObject.Data.TryGetValue("AttributeLDAPDisplayName", out var attrName) || attrName != "qPLik");
    }


        public ADOrganizationalUnitChangeDetailed(EventObject eventObject) : base(eventObject) {
            // common fields
            _eventObject = eventObject;
            Type = "ADOrganizationalUnitChangeDetailed";
            Computer = _eventObject.ComputerName;
            Action = _eventObject.MessageSubject;
            Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
            When = _eventObject.TimeCreated;

            OperationType = ConvertFromOperationType(_eventObject.Data["OperationType"]);
            OrganizationalUnit = _eventObject.GetValueFromDataDictionary("ObjectDN");
            FieldChanged = _eventObject.GetValueFromDataDictionary("AttributeLDAPDisplayName");
            FieldValue = _eventObject.GetValueFromDataDictionary("AttributeValue");
            // OverwriteByField logic
            OrganizationalUnit = OverwriteByField(Action, "A directory service object was moved.", OrganizationalUnit, _eventObject.GetValueFromDataDictionary("OldObjectDN"));
            FieldValue = OverwriteByField(Action, "A directory service object was moved.", FieldValue, _eventObject.GetValueFromDataDictionary("NewObjectDN"));

        }
    }
}
