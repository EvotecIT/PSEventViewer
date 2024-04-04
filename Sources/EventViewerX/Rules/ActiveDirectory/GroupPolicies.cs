namespace EventViewerX.Rules.ActiveDirectory {

    //ADGroupPolicyChanges                = [ordered] @{
    //     Enabled                     = $false
    //     'Group Policy Name Changes' = @{
    //         Enabled     = $true
    //         Events      = 5136, 5137, 5141
    //         LogName     = 'Security'
    //         Filter      = [ordered] @{
    //             # Filter is special, if there is just one object on the right side
    //             # If there are more objects filter will pick all values on the right side and display them as required
    //             'ObjectClass'              = 'groupPolicyContainer'
    //             #'OperationType'            = 'Value Added'
    //             'AttributeLDAPDisplayName' = $null, 'displayName' #, 'versionNumber'
    //         }
    //         Functions   = @{
    //             'OperationType' = 'ConvertFrom-OperationType'
    //         }
    //         Fields      = [ordered] @{
    //             'RecordID'                 = 'Record ID'
    //             'Computer'                 = 'Domain Controller'
    //             'Action'                   = 'Action'
    //             'Who'                      = 'Who'
    //             'Date'                     = 'When'


    //             'ObjectDN'                 = 'ObjectDN'
    //             'ObjectGUID'               = 'ObjectGUID'
    //             'ObjectClass'              = 'ObjectClass'
    //             'AttributeLDAPDisplayName' = 'AttributeLDAPDisplayName'
    //             #'AttributeSyntaxOID'       = 'AttributeSyntaxOID'
    //             'AttributeValue'           = 'AttributeValue'
    //             'OperationType'            = 'OperationType'
    //             'OpCorrelationID'          = 'OperationCorelationID'
    //             'AppCorrelationID'         = 'OperationApplicationCorrelationID'

    //             'DSName'                   = 'DSName'
    //             'DSType'                   = 'DSType'
    //             'Task'                     = 'Task'
    //             'Version'                  = 'Version'

    //             # Common Fields
    //             'ID'                       = 'Event ID'

    //             'GatheredFrom'             = 'Gathered From'
    //             'GatheredLogName'          = 'Gathered LogName'
    //         }

    //         SortBy      = 'Record ID'
    //         Descending  = $false
    //         IgnoreWords = @{

    //         }
    //     }
    //     'Group Policy Edits'        = @{
    //         Enabled     = $true
    //         Events      = 5136, 5137, 5141
    //         LogName     = 'Security'
    //         Filter      = [ordered] @{
    //             # Filter is special, if there is just one object on the right side
    //             # If there are more objects filter will pick all values on the right side and display them as required
    //             'ObjectClass'              = 'groupPolicyContainer'
    //             #'OperationType'            = 'Value Added'
    //             'AttributeLDAPDisplayName' = 'versionNumber'
    //         }
    //         Functions   = @{
    //             'OperationType' = 'ConvertFrom-OperationType'
    //         }
    //         Fields      = [ordered] @{
    //             'RecordID'                 = 'Record ID'
    //             'Computer'                 = 'Domain Controller'
    //             'Action'                   = 'Action'
    //             'Who'                      = 'Who'
    //             'Date'                     = 'When'


    //             'ObjectDN'                 = 'ObjectDN'
    //             'ObjectGUID'               = 'ObjectGUID'
    //             'ObjectClass'              = 'ObjectClass'
    //             'AttributeLDAPDisplayName' = 'AttributeLDAPDisplayName'
    //             #'AttributeSyntaxOID'       = 'AttributeSyntaxOID'
    //             'AttributeValue'           = 'AttributeValue'
    //             'OperationType'            = 'OperationType'
    //             'OpCorrelationID'          = 'OperationCorelationID'
    //             'AppCorrelationID'         = 'OperationApplicationCorrelationID'

    //             'DSName'                   = 'DSName'
    //             'DSType'                   = 'DSType'
    //             'Task'                     = 'Task'
    //             'Version'                  = 'Version'

    //             # Common Fields
    //             'ID'                       = 'Event ID'

    //             'GatheredFrom'             = 'Gathered From'
    //             'GatheredLogName'          = 'Gathered LogName'
    //         }

    //         SortBy      = 'Record ID'
    //         Descending  = $false
    //         IgnoreWords = @{

    //         }
    //     }
    //     'Group Policy Links'        = @{
    //         Enabled     = $true
    //         Events      = 5136, 5137, 5141
    //         LogName     = 'Security'
    //         Filter      = @{
    //             # Filter is special, if there is just one object on the right side
    //             # If there are more objects filter will pick all values on the right side and display them as required
    //             'ObjectClass' = 'domainDNS'
    //             #'OperationType'            = 'Value Added'
    //             #'AttributeLDAPDisplayName' = 'versionNumber'
    //         }
    //         Functions   = @{
    //             'OperationType' = 'ConvertFrom-OperationType'
    //         }
    //         Fields      = [ordered] @{
    //             'RecordID'                 = 'Record ID'
    //             'Computer'                 = 'Domain Controller'
    //             'Action'                   = 'Action'
    //             'Who'                      = 'Who'
    //             'Date'                     = 'When'


    //             'ObjectDN'                 = 'ObjectDN'
    //             'ObjectGUID'               = 'ObjectGUID'
    //             'ObjectClass'              = 'ObjectClass'
    //             'AttributeLDAPDisplayName' = 'AttributeLDAPDisplayName'
    //             #'AttributeSyntaxOID'       = 'AttributeSyntaxOID'
    //             'AttributeValue'           = 'AttributeValue'
    //             'OperationType'            = 'OperationType'
    //             'OpCorrelationID'          = 'OperationCorelationID'
    //             'AppCorrelationID'         = 'OperationApplicationCorrelationID'

    //             'DSName'                   = 'DSName'
    //             'DSType'                   = 'DSType'
    //             'Task'                     = 'Task'
    //             'Version'                  = 'Version'

    //             # Common Fields
    //             'ID'                       = 'Event ID'

    //             'GatheredFrom'             = 'Gathered From'
    //             'GatheredLogName'          = 'Gathered LogName'
    //         }

    //         SortBy      = 'Record ID'
    //         Descending  = $false
    //         IgnoreWords = @{

    //         }
    //     }
    // }
}
