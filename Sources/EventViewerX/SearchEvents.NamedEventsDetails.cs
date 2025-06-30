using EventViewerX.Rules.ActiveDirectory;
using EventViewerX.Rules.Logging;
using EventViewerX.Rules.Windows;
using EventViewerX.Rules.Kerberos;
using EventViewerX.Rules.CertificateAuthority;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
        /// Kerberos policy changed
        /// </summary>
        KerberosPolicyChange,
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
        /// Windows Firewall rule modified
        /// </summary>
        FirewallRuleChange,

        /// <summary>
        /// BitLocker protection key changed or backed up
        /// </summary>
        BitLockerKeyChange,

        /// <summary>
        /// External device recognized by the system
        /// </summary>
        DeviceRecognized,

        /// <summary>
        /// Scheduled task deleted
        /// </summary>
        ScheduledTaskDeleted,

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
        /// Windows Update installation failure
        /// </summary>
        WindowsUpdateFailure,
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
        /// <summary>
        /// Named events dictionary that maps NamedEvents to event IDs and log names
        /// discovered dynamically. Entries not discovered via attributes fallback to this manual map.
        /// </summary>

        private static readonly Dictionary<NamedEvents, (List<int> EventIds, string LogName)> eventIdsMap;
        private static readonly Dictionary<NamedEvents, Func<EventObject, EventObjectSlim?>> eventBuilders;
        private static readonly Dictionary<NamedEvents, (List<int> EventIds, string LogName)> manualEventIdsMap = new()
        {
            // computer based events
            { NamedEvents.ADComputerCreateChange, (new List<int> { 4741, 4742 }, "Security") },
            { NamedEvents.ADComputerChangeDetailed, (new List<int> { 5136, 5137, 5139, 5141 }, "Security") },
            { NamedEvents.ADComputerDeleted, (new List<int> { 4743 }, "Security") },
            // group based events
            { NamedEvents.ADGroupMembershipChange, (new List<int> { 4728, 4729, 4732, 4733, 4746, 4747, 4751, 4752, 4756, 4757, 4761, 4762, 4785, 4786, 4787, 4788 }, "Security") },
            { NamedEvents.ADGroupEnumeration, (new List<int> { 4798, 4799 }, "Security") },
            { NamedEvents.ADGroupChange, (new List<int> { 4735, 4737, 4745, 4750, 4760, 4764, 4784, 4791 }, "Security") },
            { NamedEvents.ADGroupCreateDelete, (new List<int> { 4727, 4730, 4731, 4734, 4744, 4748, 4749, 4753, 4754, 4758, 4759, 4763 }, "Security") },
            { NamedEvents.ADGroupChangeDetailed, (new List<int> { 5136, 5137, 5139, 5141 }, "Security") },
            // group policy events
            { NamedEvents.ADGroupPolicyChanges, (new List<int> { 5136, 5137, 5141 }, "Security") },
            { NamedEvents.ADGroupPolicyEdits, (new List<int> { 5136, 5137, 5141 }, "Security") },
            { NamedEvents.ADGroupPolicyLinks, (new List<int> { 5136, 5137, 5141 }, "Security") },
            { NamedEvents.GpoCreated, (new List<int> { 5137 }, "Security") },
            { NamedEvents.GpoDeleted, (new List<int> { 5141 }, "Security") },
            { NamedEvents.GpoModified, (new List<int> { 5136 }, "Security") },
            // user based events
            { NamedEvents.ADUserCreateChange, (new List<int> { 4720, 4738 }, "Security") },
            { NamedEvents.ADUserStatus, (new List<int> { 4722, 4725, 4723, 4724, 4726 }, "Security") },
            { NamedEvents.ADUserChangeDetailed, (new List<int> { 5136, 5137, 5139, 5141 }, "Security") },
            { NamedEvents.ADOrganizationalUnitChangeDetailed, (new List<int> { 5136, 5137, 5139, 5141 }, "Security") },
            { NamedEvents.ADUserLockouts, (new List<int> { 4740 }, "Security") },
            { NamedEvents.ADUserLogon, (new List<int> { 4624 }, "Security") },
            { NamedEvents.ADUserLogonNTLMv1, (new List<int> { 4624 }, "Security") },
            { NamedEvents.ADUserLogonFailed, (new List<int> { 4625 }, "Security") },
            { NamedEvents.ADUserLogonKerberos, (new List<int> { 4768 }, "Security") },
            { NamedEvents.ADUserUnlocked, (new List<int> { 4767 }, "Security") },
            { NamedEvents.KerberosServiceTicket, (new List<int> { 4769, 4770 }, "Security") },
            { NamedEvents.KerberosTicketFailure, (new List<int> { 4771, 4772 }, "Security") },
            { NamedEvents.KerberosPolicyChange, (new List<int> { 4713 }, "Security") },
            // other based events
            { NamedEvents.ADOtherChangeDetailed, (new List<int> { 5136, 5137, 5139, 5141 }, "Security") },
            // ldap events
            { NamedEvents.ADLdapBindingSummary, (new List<int> { 2887 }, "Directory Service") },
            { NamedEvents.ADLdapBindingDetails, (new List<int> { 2889 }, "Directory Service") },
            // samba
            { NamedEvents.ADSMBServerAuditV1, (new List<int> { 3000 }, "Microsoft-Windows-SMBServer/Audit") },
            // logs cleared
            { NamedEvents.LogsClearedSecurity, (new List<int> { 1102, 1105 }, "Security") },
            { NamedEvents.LogsClearedOther, (new List<int> { 104 }, "System") },
            { NamedEvents.LogsFullSecurity, (new List<int> { 1104 }, "Security") },
            // network access
            { NamedEvents.NetworkAccessAuthenticationPolicy, (new List<int> { 6272, 6273 }, "Security") },
            { NamedEvents.CertificateIssued, (new List<int> { 4886, 4887 }, "Security") },
            { NamedEvents.AuditPolicyChange, (new List<int> { 4719 }, "Security") },
            { NamedEvents.FirewallRuleChange, (new List<int> { 4947 }, "Security") },
            { NamedEvents.BitLockerKeyChange, (new List<int> { 4673, 4692 }, "Security") },
            { NamedEvents.DeviceRecognized, (new List<int> { 6416 }, "Security") },
            { NamedEvents.ScheduledTaskDeleted, (new List<int> { 4699 }, "Security") },
            // windows OS
            { NamedEvents.OSCrash, (new List<int> { 6008 }, "System") },
            { NamedEvents.OSStartupShutdownCrash, (new List<int> { 12, 13, 41, 4608, 4621, 6008 }, "System") },
            { NamedEvents.OSTimeChange, (new List<int> { 4616 }, "Security") },
            { NamedEvents.WindowsUpdateFailure, (new List<int> { 20 }, "Setup") },
            { NamedEvents.ClientGroupPoliciesApplication, (new List<int> { 4098 }, "Application") },
            { NamedEvents.ClientGroupPoliciesSystem, (new List<int> { 1085 }, "System") },
        };

        static SearchEvents()
        {
            eventBuilders = new Dictionary<NamedEvents, Func<EventObject, EventObjectSlim?>>();
            eventIdsMap = BuildEventIdsMap();
        }

        private static Dictionary<NamedEvents, (List<int> EventIds, string LogName)> BuildEventIdsMap()
        {
            var map = new Dictionary<NamedEvents, (List<int>, string)>(manualEventIdsMap);

            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                foreach (var attr in type.GetCustomAttributes<NamedEventAttribute>())
                {
                    map[attr.NamedEvent] = (attr.EventIds.ToList(), attr.LogName);

                    var method = type.GetMethod("TryCreate", BindingFlags.Public | BindingFlags.Static);
                    if (method != null)
                    {
                        var del = (Func<EventObject, EventObjectSlim?>)Delegate.CreateDelegate(typeof(Func<EventObject, EventObjectSlim?>), method);
                        eventBuilders[attr.NamedEvent] = del;
                    }
                    else
                    {
                        var ctor = type.GetConstructor(new[] { typeof(EventObject) });
                        if (ctor != null)
                        {
                            eventBuilders[attr.NamedEvent] = e => (EventObjectSlim)ctor.Invoke(new object[] { e });
                        }
                    }
                }
            }

            return map;
        }

        private static EventObjectSlim BuildTargetEvents(EventObject eventObject, List<NamedEvents> typeEventsList)
        {
            foreach (var typeEvents in typeEventsList)
            {
                if (!eventIdsMap.TryGetValue(typeEvents, out var eventInfo) ||
                    !eventInfo.EventIds.Contains(eventObject.Id) ||
                    eventInfo.LogName != eventObject.LogName)
                {
                    continue;
                }

                if (eventBuilders.TryGetValue(typeEvents, out var builder))
                {
                    var result = builder(eventObject);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return null;
        }
    }
}
