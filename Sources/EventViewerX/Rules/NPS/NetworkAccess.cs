using System;

namespace EventViewerX.Rules.NPS {
    /// <summary>
    /// Network Access Authentication Policy
    /// 6272: Network Policy Server granted access to a user
    /// 6273: Network Policy Server denied access to a user
    /// </summary>
    public class NetworkAccessAuthenticationPolicy : EventObjectSlim {
        public string Computer;
        public string Action;
        public string SecurityID;
        public string AccountName;
        public string AccountDomain;
        public string CalledStationID;
        public string CallingStationID;

        public string NASIPv4Address;
        public string NASIPv6Address;
        public string NASIdentifier;
        public string NASPortType;
        public string NASPort;

        public string ClientFriendlyName;
        public string ClientFriendlyIPAddress;

        public string ConnectionRequestPolicyName;
        public string NetworkPolicyName;
        public string AuthenticationProvider;
        public string AuthenticationServer;
        public string AuthenticationType;
        public string EAPType;

        public string Reason;
        public string ReasonCode;

        public string Who;
        public DateTime When;

        public NetworkAccessAuthenticationPolicy(EventObject eventObject) : base(eventObject) {
            _eventObject = eventObject;

            Type = "NetworkAccessAuthenticationPolicy";
            Computer = _eventObject.ComputerName;
            Action = _eventObject.MessageSubject;
            SecurityID = _eventObject.GetValueFromDataDictionary("SubjectUserSid");
            AccountName = _eventObject.GetValueFromDataDictionary("SubjectUserName");
            AccountDomain = _eventObject.GetValueFromDataDictionary("SubjectDomainName");

            CalledStationID = _eventObject.GetValueFromDataDictionary("CalledStationID");
            CallingStationID = _eventObject.GetValueFromDataDictionary("CallingStationID");

            NASIPv4Address = _eventObject.GetValueFromDataDictionary("NASIPv4Address");
            NASIPv6Address = _eventObject.GetValueFromDataDictionary("NASIPv6Address");

            NASIdentifier = _eventObject.GetValueFromDataDictionary("NASIdentifier");
            NASPort = _eventObject.GetValueFromDataDictionary("NASPort");
            NASPortType = _eventObject.GetValueFromDataDictionary("NASPortType");


            AuthenticationProvider = _eventObject.GetValueFromDataDictionary("AuthenticationProvider");
            AuthenticationServer = _eventObject.GetValueFromDataDictionary("AuthenticationServer");
            AuthenticationType = _eventObject.GetValueFromDataDictionary("AuthenticationType");

            EAPType = _eventObject.GetValueFromDataDictionary("EAPType");

            ClientFriendlyIPAddress = _eventObject.GetValueFromDataDictionary("ClientIPAddress");
            ClientFriendlyName = _eventObject.GetValueFromDataDictionary("ClientName");

            ConnectionRequestPolicyName = _eventObject.GetValueFromDataDictionary("ProxyPolicyName");

            NetworkPolicyName = _eventObject.GetValueFromDataDictionary("NetworkPolicyName");

            Reason = _eventObject.GetValueFromDataDictionary("Reason");
            ReasonCode = _eventObject.GetValueFromDataDictionary("ReasonCode");
            // common fields
            Who = _eventObject.GetValueFromDataDictionary("FullyQualifiedSubjectUserName");
            When = _eventObject.TimeCreated;
        }
    }
}
