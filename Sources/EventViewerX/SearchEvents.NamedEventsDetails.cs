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
        private static readonly Dictionary<NamedEvents, (List<int> EventIds, string LogName)> manualEventIdsMap = new();

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
        /// <summary>
        /// Builds the appropriate event object based on the NamedEvents value
        /// </summary>
        /// <param name="eventObject"></param>
        /// <param name="typeEventsList"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
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