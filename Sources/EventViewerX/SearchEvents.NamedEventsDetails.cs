using EventViewerX.Rules.ActiveDirectory;
using EventViewerX.Rules.Logging;
using EventViewerX.Rules.Windows;
using EventViewerX.Rules.Kerberos;
using EventViewerX.Rules.CertificateAuthority;
using EventViewerX.Rules.NPS;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EventViewerX {
    /// <summary>
    /// Defines the named events that can be searched for
    /// </summary>
    public enum NamedEvents {
        /// <summary>
        /// Active Directory computer account created or modified
        /// </summary>
        ADComputerCreateChange,
        /// <summary>
        /// Active Directory computer account deleted
        /// </summary>
        ADComputerDeleted,
        /// <summary>
        /// Detailed changes for computer accounts
        /// </summary>
        ADComputerChangeDetailed,

        /// <summary>
        /// Modifications to group membership
        /// </summary>
        ADGroupMembershipChange,
        /// <summary>
        /// Group membership enumeration events
        /// </summary>
        ADGroupEnumeration,
        /// <summary>
        /// Active Directory group created, changed or deleted
        /// </summary>
        ADGroupChange,
        /// <summary>
        /// Group creation or deletion events
        /// </summary>
        ADGroupCreateDelete,
        /// <summary>
        /// Detailed changes for group objects
        /// </summary>
        ADGroupChangeDetailed,

        /// <summary>
        /// Changes to Group Policy Objects
        /// </summary>
        ADGroupPolicyChanges,
        /// <summary>
        /// Edits to Group Policy Objects
        /// </summary>
        ADGroupPolicyEdits,
        /// <summary>
        /// Links or unlinks of Group Policy Objects
        /// </summary>
        ADGroupPolicyLinks,

        /// <summary>
        /// Group Policy Object created
        /// </summary>
        GpoCreated,
        /// <summary>
        /// Group Policy Object deleted
        /// </summary>
        GpoDeleted,
        /// <summary>
        /// Group Policy Object modified
        /// </summary>
        GpoModified,

        /// <summary>
        /// Summary of LDAP binding activity
        /// </summary>
        ADLdapBindingSummary,
        /// <summary>
        /// Detailed LDAP binding information
        /// </summary>
        ADLdapBindingDetails,
        /// <summary>
        /// Active Directory user account created or changed
        /// </summary>
        ADUserCreateChange,
        /// <summary>
        /// User account enabled, disabled, unlocked or deleted
        /// </summary>
        ADUserStatus,
        /// <summary>
        /// Detailed changes for user accounts
        /// </summary>
        ADUserChangeDetailed,
        /// <summary>
        /// User account lockout events
        /// </summary>
        ADUserLockouts,
        /// <summary>
        /// Successful user logon
        /// </summary>
        ADUserLogon,
        /// <summary>
        /// NTLMv1 logon tracking
        /// </summary>
        ADUserLogonNTLMv1,
        /// <summary>
        /// Kerberos authentication ticket requests
        /// </summary>
        ADUserLogonKerberos,
        /// <summary>
        /// Failed user logon attempts
        /// </summary>
        ADUserLogonFailed,
        /// <summary>
        /// User account unlocked
        /// </summary>
        ADUserUnlocked,
        /// <summary>
        /// Kerberos service ticket requests and renewals
        /// </summary>
        KerberosServiceTicket,
        /// <summary>
        /// Kerberos ticket request failures
        /// </summary>
        KerberosTicketFailure,
        /// <summary>
        /// Organizational unit created, deleted or moved
        /// </summary>
        ADOrganizationalUnitChangeDetailed,
        /// <summary>
        /// Detailed changes for other directory objects
        /// </summary>
        ADOtherChangeDetailed,

        /// <summary>
        /// SMB1 access audit information
        /// </summary>
        ADSMBServerAuditV1,

        /// <summary>
        /// Security log cleared
        /// </summary>
        LogsClearedSecurity,
        /// <summary>
        /// Application or system log cleared
        /// </summary>
        LogsClearedOther,
        /// <summary>
        /// Security log is full
        /// </summary>
        LogsFullSecurity,

        /// <summary>
        /// NPS granted or denied network access
        /// </summary>
        NetworkAccessAuthenticationPolicy,

        /// <summary>
        /// Certificate issued by Certificate Authority
        /// </summary>
        CertificateIssued,

        /// <summary>
        /// System audit policy was changed
        /// </summary>
        AuditPolicyChange,

        /// <summary>
        /// Unexpected system shutdown
        /// </summary>
        OSCrash,
        /// <summary>
        /// System start-up, shutdown or crash events
        /// </summary>
        OSStartupShutdownCrash,
        /// <summary>
        /// System time changed
        /// </summary>
        OSTimeChange,
        /// <summary>
        /// Group Policy client-side processing events from Application log
        /// </summary>
        ClientGroupPoliciesApplication,
        /// <summary>
        /// Group Policy client-side processing events from System log
        /// </summary>
        ClientGroupPoliciesSystem,
    }

    public partial class SearchEvents : Settings {
        private sealed class NamedEventDefinition {
            public NamedEvents Name { get; }
            public IReadOnlyList<int> EventIds { get; }
            public string LogName { get; }
            private readonly Func<EventObject, EventObjectSlim?> _builder;

            private NamedEventDefinition(NamedEvents name, int[] eventIds, string logName, Func<EventObject, EventObjectSlim?> builder) {
                Name = name;
                EventIds = eventIds;
                LogName = logName;
                _builder = builder;
            }

            public EventObjectSlim? Build(EventObject eventObject) => _builder(eventObject);

            public static NamedEventDefinition Create<T>(NamedEvents name, string logName, params int[] eventIds) where T : EventObjectSlim =>
                new(name, eventIds, logName, e => Activator.CreateInstance(typeof(T), e) as EventObjectSlim);

            public static NamedEventDefinition Create<T>(NamedEvents name, string logName, Func<EventObject, bool> filter, params int[] eventIds) where T : EventObjectSlim =>
                new(name, eventIds, logName, e => filter(e) ? Activator.CreateInstance(typeof(T), e) as EventObjectSlim : null);

            public static NamedEventDefinition Create(NamedEvents name, string logName, Func<EventObject, EventObjectSlim?> builder, params int[] eventIds) =>
                new(name, eventIds, logName, builder);
        }

        /// <summary>
        /// Named events list used to build the dictionary at runtime
        /// </summary>
        private static readonly List<NamedEventDefinition> eventDefinitionsList = new()
        {
            // computer based events
            NamedEventDefinition.Create<ADComputerCreateChange>(NamedEvents.ADComputerCreateChange, "Security", 4741, 4742),
            NamedEventDefinition.Create<ADComputerChangeDetailed>(NamedEvents.ADComputerChangeDetailed, "Security", e => e.Data.TryGetValue("ObjectClass", out var obj) && obj == "computer", 5136, 5137, 5139, 5141),
            NamedEventDefinition.Create<ADComputerDeleted>(NamedEvents.ADComputerDeleted, "Security", 4743),
            // group based events
            NamedEventDefinition.Create<ADGroupMembershipChange>(NamedEvents.ADGroupMembershipChange, "Security", 4728, 4729, 4732, 4733, 4746, 4747, 4751, 4752, 4756, 4757, 4761, 4762, 4785, 4786, 4787, 4788),
            NamedEventDefinition.Create<ADGroupEnumeration>(NamedEvents.ADGroupEnumeration, "Security", 4798, 4799),
            NamedEventDefinition.Create(NamedEvents.ADGroupChange, "Security", e =>
            {
                var result = new ADGroupChange(e);
                return result.Who == "*ANONYMOUS*" ? null : result;
            }, 4735, 4737, 4745, 4750, 4760, 4764, 4784, 4791),
            NamedEventDefinition.Create<ADGroupCreateDelete>(NamedEvents.ADGroupCreateDelete, "Security", 4727, 4730, 4731, 4734, 4744, 4748, 4749, 4753, 4754, 4758, 4759, 4763),
            NamedEventDefinition.Create<ADGroupChangeDetailed>(NamedEvents.ADGroupChangeDetailed, "Security", e => e.Data.TryGetValue("ObjectClass", out var obj) && obj == "user", 5136, 5137, 5139, 5141),
            // group policy events
            NamedEventDefinition.Create<ADGroupPolicyChanges>(NamedEvents.ADGroupPolicyChanges, "Security", e => e.Data.TryGetValue("ObjectClass", out var obj) && (obj == "groupPolicyContainer" || obj == "container"), 5136, 5137, 5141),
            NamedEventDefinition.Create<ADGroupPolicyEdits>(NamedEvents.ADGroupPolicyEdits, "Security", e =>
            {
                if (e.Data.TryGetValue("ObjectClass", out var obj) && obj == "groupPolicyContainer" &&
                    e.Data.TryGetValue("AttributeLDAPDisplayName", out var ldapDisplayObjName) &&
                    ldapDisplayObjName is string ldapDisplayNameValue && ldapDisplayNameValue == "versionNumber")
                {
                    return true;
                }
                return false;
            }, 5136, 5137, 5141),
            NamedEventDefinition.Create<ADGroupPolicyLinks>(NamedEvents.ADGroupPolicyLinks, "Security", e =>
            {
                if (e.Data.TryGetValue("ObjectClass", out var obj) && (obj == "domainDNS" || obj == "organizationalUnit" || obj == "site") &&
                    e.ValueMatches("AttributeLDAPDisplayName", "gpLink"))
                {
                    return true;
                }
                return false;
            }, 5136, 5137, 5141),
            NamedEventDefinition.Create<GpoCreated>(NamedEvents.GpoCreated, "Security", e => e.Data.TryGetValue("ObjectClass", out var obj) && obj == "groupPolicyContainer", 5137),
            NamedEventDefinition.Create<GpoDeleted>(NamedEvents.GpoDeleted, "Security", e => e.Data.TryGetValue("ObjectClass", out var obj) && obj == "groupPolicyContainer", 5141),
            NamedEventDefinition.Create<GpoModified>(NamedEvents.GpoModified, "Security", e => e.Data.TryGetValue("ObjectClass", out var obj) && obj == "groupPolicyContainer", 5136),
            // user based events
            NamedEventDefinition.Create<ADUserCreateChange>(NamedEvents.ADUserCreateChange, "Security", 4720, 4738),
            NamedEventDefinition.Create<ADUserStatus>(NamedEvents.ADUserStatus, "Security", 4722, 4725, 4723, 4724, 4726),
            NamedEventDefinition.Create<ADUserChangeDetailed>(NamedEvents.ADUserChangeDetailed, "Security", e => e.Data.TryGetValue("ObjectClass", out var obj) && obj == "user", 5136, 5137, 5139, 5141),
            NamedEventDefinition.Create<ADOrganizationalUnitChangeDetailed>(NamedEvents.ADOrganizationalUnitChangeDetailed, "Security", e => e.Data.TryGetValue("ObjectClass", out var obj) && obj == "organizationalUnit" && e.Data["AttributeLDAPDisplayName"] != "qPLik", 5136, 5137, 5139, 5141),
            NamedEventDefinition.Create<ADUserLockouts>(NamedEvents.ADUserLockouts, "Security", 4740),
            NamedEventDefinition.Create<ADUserLogon>(NamedEvents.ADUserLogon, "Security", 4624),
            NamedEventDefinition.Create<ADUserLogonNTLMv1>(NamedEvents.ADUserLogonNTLMv1, "Security", e => e.ValueMatches("LmPackageName", "NTLM V1"), 4624),
            NamedEventDefinition.Create<ADUserLogonFailed>(NamedEvents.ADUserLogonFailed, "Security", 4625),
            NamedEventDefinition.Create<ADUserLogonKerberos>(NamedEvents.ADUserLogonKerberos, "Security", 4768),
            NamedEventDefinition.Create<ADUserUnlocked>(NamedEvents.ADUserUnlocked, "Security", 4767),
            NamedEventDefinition.Create<KerberosServiceTicket>(NamedEvents.KerberosServiceTicket, "Security", 4769, 4770),
            NamedEventDefinition.Create<KerberosTicketFailure>(NamedEvents.KerberosTicketFailure, "Security", 4771, 4772),
            // other based events
            NamedEventDefinition.Create<ADOtherChangeDetailed>(NamedEvents.ADOtherChangeDetailed, "Security", e =>
            {
                if (e.Data.TryGetValue("ObjectClass", out var obj) && obj is string s && s is not ("user" or "computer" or "organizationalUnit" or "group"))
                {
                    return true;
                }
                return false;
            }, 5136, 5137, 5139, 5141),
            // ldap events
            NamedEventDefinition.Create<ADLdapBindingSummary>(NamedEvents.ADLdapBindingSummary, "Directory Service", 2887),
            NamedEventDefinition.Create<ADLdapBindingDetails>(NamedEvents.ADLdapBindingDetails, "Directory Service", 2889),
            // samba
            NamedEventDefinition.Create(NamedEvents.ADSMBServerAuditV1, "Microsoft-Windows-SMBServer/Audit", SMBServerAudit.Create, 3000),
            // logs cleared
            NamedEventDefinition.Create<LogsClearedSecurity>(NamedEvents.LogsClearedSecurity, "Security", 1102, 1105),
            NamedEventDefinition.Create<LogsClearedOther>(NamedEvents.LogsClearedOther, "System", 104),
            NamedEventDefinition.Create(NamedEvents.LogsFullSecurity, "Security", _ => null, 1104),
            // network access
            NamedEventDefinition.Create<NetworkAccessAuthenticationPolicy>(NamedEvents.NetworkAccessAuthenticationPolicy, "Security", 6272, 6273),
            NamedEventDefinition.Create<CertificateIssued>(NamedEvents.CertificateIssued, "Security", 4886, 4887),
            NamedEventDefinition.Create<AuditPolicyChange>(NamedEvents.AuditPolicyChange, "Security", 4719),
            // windows OS
            NamedEventDefinition.Create<OSCrash>(NamedEvents.OSCrash, "System", 6008),
            NamedEventDefinition.Create<OSStartupShutdownCrash>(NamedEvents.OSStartupShutdownCrash, "System", 12, 13, 41, 4608, 4621, 6008),
            NamedEventDefinition.Create<OSTimeChange>(NamedEvents.OSTimeChange, "Security", 4616),
            NamedEventDefinition.Create<ClientGroupPolicies>(NamedEvents.ClientGroupPoliciesApplication, "Application", 4098),
            NamedEventDefinition.Create<ClientGroupPolicies>(NamedEvents.ClientGroupPoliciesSystem, "System", 1085),
        };

        private static readonly Dictionary<NamedEvents, NamedEventDefinition> eventDefinitions =
            eventDefinitionsList.ToDictionary(d => d.Name);
        /// <summary>
        /// Builds the appropriate event object based on the NamedEvents value
        /// </summary>
        /// <param name="eventObject"></param>
        /// <param name="typeEventsList"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private static EventObjectSlim BuildTargetEvents(EventObject eventObject, List<NamedEvents> typeEventsList) {
            foreach (var typeEvents in typeEventsList) {
                if (eventDefinitions.TryGetValue(typeEvents, out var def) &&
                    def.EventIds.Contains(eventObject.Id) &&
                    def.LogName == eventObject.LogName) {
                    var result = def.Build(eventObject);
                    if (result != null) {
                        return result;
                    }
                }
            }

            return null;
        }
    }
}