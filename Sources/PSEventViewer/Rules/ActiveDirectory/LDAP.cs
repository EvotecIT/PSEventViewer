using System;
using System.Collections.Generic;
using System.Text;

namespace PSEventViewer.Rules.ActiveDirectory {
    /// <summary>
    /// A summary of LDAP binding activity.
    /// 2887(0x0B57) - The security of this directory server can be significantly enhanced by configuring the server to reject SASL (Negotiate, Kerberos, NTLM, or Digest) LDAP binds that do not request signing (integrity verification) and LDAP simple binds that are performed on a cleartext (non-SSL/TLS-encrypted) connection. Even if this directory server is not used by Active Directory, it is recommended that this server be configured to reject such binds. For more details and information on how to make this configuration change to the server, please see http://go.microsoft.com/fwlink/?LinkID=87923.
    /// </summary>
    public class ADLdapBindingSummary : EventObjectSlim {
        public string Computer;
        public string Action;
        public string NumberOfSimpleBindsWithoutSSLTLS;
        public string NumberOfNegotiateKerberosNtlmDigestBindsPerformedWithoutSigning;
        public string TaskDisplayName;
        public string LevelDisplayName;
        public DateTime When;

        public string LogName = "Directory Service";
        public List<int> EventID = [2887];

        public ADLdapBindingSummary(EventObject eventObject) : base(eventObject) {
            _eventObject = eventObject;

            Type = "ADLdapBindingSummary";
            Computer = _eventObject.ComputerName;
            Action = _eventObject.MessageSubject;
            NumberOfSimpleBindsWithoutSSLTLS = _eventObject.GetValueFromDataDictionary("NoNameA0");
            NumberOfNegotiateKerberosNtlmDigestBindsPerformedWithoutSigning = _eventObject.GetValueFromDataDictionary("NoNameA1");
            When = _eventObject.TimeCreated;
            LevelDisplayName = eventObject.LevelDisplayName;
            TaskDisplayName = eventObject.TaskDisplayName;
        }
    }

    /// <summary>
    /// LDAP binding details.
    /// 2889: The security of this directory server can be significantly enhanced by configuring the server to reject SASL (Negotiate, Kerberos, NTLM, or Digest) LDAP binds that do not request signing (integrity verification) and LDAP simple binds that are performed on a cleartext (non-SSL/TLS-encrypted) connection. Even if this directory server is not used by Active Directory, it is recommended that this server be configured to reject such binds. For more details and information on how to make this configuration change to the server, please see http://go.microsoft.com/fwlink/?LinkID=87923.
    /// </summary>
    public class ADLdapBindingDetails : EventObjectSlim {
        public string Computer;
        public string Action;
        private string IPPort;
        public string AccountName;
        private string BindType;
        public DateTime When;

        public string LogName = "Directory Service";
        public List<int> EventID = [2887];


        public ADLdapBindingDetails(EventObject eventObject) : base(eventObject) {
            _eventObject = eventObject;

            Type = "ADLdapBindingDetails";
            Computer = _eventObject.ComputerName;
            Action = _eventObject.MessageSubject;
            IPPort = _eventObject.GetValueFromDataDictionary("NoNameA0");
            AccountName = _eventObject.GetValueFromDataDictionary("NoNameA1");
            BindType = _eventObject.GetValueFromDataDictionary("NoNameA2");
            if (BindType == "0") {
                BindType = "Unsigned";
            } else if (BindType == "1") {
                BindType = "Simple";
            }
            When = _eventObject.TimeCreated;
        }
    }
}
