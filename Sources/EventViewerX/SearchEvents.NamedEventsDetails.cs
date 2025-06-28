using EventViewerX.Rules.ActiveDirectory;
using EventViewerX.Rules.Logging;
using EventViewerX.Rules.Windows;
using EventViewerX.Rules.Kerberos;
using System;
using System.Collections.Generic;

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
    }

    public partial class SearchEvents : Settings {
        /// <summary>
        /// Named events dictionary that maps NamedEvents to event IDs and log names
        /// </summary>
        private static readonly Dictionary<NamedEvents, (List<int> EventIds, string LogName)> eventIdsMap = new Dictionary<NamedEvents, (List<int>, string)> {
            // computer based events
            { NamedEvents.ADComputerCreateChange, (new List<int> {  4741, 4742 }, "Security") },
            { NamedEvents.ADComputerChangeDetailed, (new List<int> { 5136, 5137, 5139, 5141 }, "Security") },
            { NamedEvents.ADComputerDeleted, (new List<int> { 4743 }, "Security") },
            // group based events
            { NamedEvents.ADGroupMembershipChange, (new List<int> {  4728, 4729, 4732, 4733, 4746, 4747, 4751, 4752, 4756, 4757, 4761, 4762, 4785, 4786, 4787, 4788 }, "Security") },
            { NamedEvents.ADGroupEnumeration, (new List<int> { 4798, 4799}, "Security") },
            { NamedEvents.ADGroupChange, (new List<int> { 4735, 4737, 4745, 4750, 4760, 4764, 4784, 4791 }, "Security") },
            { NamedEvents.ADGroupCreateDelete, (new List<int> { 4727, 4730, 4731, 4734, 4744, 4748, 4749, 4753, 4754, 4758, 4759, 4763 }, "Security") },
            { NamedEvents.ADGroupChangeDetailed, (new List<int> { 5136, 5137, 5139, 5141 }, "Security") },
            // group policy events
            { NamedEvents.ADGroupPolicyChanges, ([5136, 5137, 5141], "Security")},
            { NamedEvents.ADGroupPolicyEdits, ([5136, 5137, 5141], "Security")},
            { NamedEvents.ADGroupPolicyLinks, ([5136, 5137, 5141], "Security")},
            { NamedEvents.GpoCreated, (new List<int> { 5137 }, "Security") },
            { NamedEvents.GpoDeleted, (new List<int> { 5141 }, "Security") },
            { NamedEvents.GpoModified, (new List<int> { 5136 }, "Security") },
            // user based events
            { NamedEvents.ADUserCreateChange, ([4720, 4738], "Security") },
            { NamedEvents.ADUserStatus, ([4722, 4725, 4723, 4724, 4726], "Security") },
            { NamedEvents.ADUserChangeDetailed, ([5136, 5137, 5139, 5141], "Security") },
            { NamedEvents.ADOrganizationalUnitChangeDetailed, ([5136, 5137, 5139, 5141], "Security") },
            { NamedEvents.ADUserLockouts, ([4740], "Security") },
            { NamedEvents.ADUserLogon, ([4624], "Security") },
            { NamedEvents.ADUserLogonNTLMv1, ([4624], "Security") },
            { NamedEvents.ADUserLogonFailed, ([4625], "Security")},
            { NamedEvents.ADUserLogonKerberos, ([4768], "Security") },
            { NamedEvents.ADUserUnlocked, ([4767], "Security") },
            { NamedEvents.KerberosServiceTicket, (new List<int> { 4769, 4770 }, "Security") },
            { NamedEvents.KerberosTicketFailure, (new List<int> { 4771, 4772 }, "Security") },
            // other based events
            { NamedEvents.ADOtherChangeDetailed, (new List<int> { 5136, 5137, 5139, 5141 }, "Security") },
            // ldap events
            { NamedEvents.ADLdapBindingSummary, (new List<int> { 2887 }, "Directory Service") },
            { NamedEvents.ADLdapBindingDetails,(new List<int> { 2889 }, "Directory Service") },
            // samba
            { NamedEvents.ADSMBServerAuditV1, (new List<int> { 3000 }, "Microsoft-Windows-SMBServer/Audit") },
            // logs cleared
            { NamedEvents.LogsClearedSecurity, (new List<int> { 1102,1105 }, "Security") },
            { NamedEvents.LogsClearedOther,(new List<int> { 104 }, "System") },
            { NamedEvents.LogsFullSecurity, (new List<int> { 1104  }, "Security") },
            // network access
            { NamedEvents.NetworkAccessAuthenticationPolicy, (new List<int> { 6272, 6273 }, "Security") },
            // windows OS
            { NamedEvents.OSCrash, (new List<int> { 6008 }, "System") },
            { NamedEvents.OSStartupShutdownCrash,  (new List<int> { 12, 13, 41, 4608, 4621, 6008 }, "System") },
            { NamedEvents.OSTimeChange, (new List<int> { 4616 }, "Security") },
        };
        /// <summary>
        /// Builds the appropriate event object based on the NamedEvents value
        /// </summary>
        /// <param name="eventObject"></param>
        /// <param name="typeEventsList"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private static EventObjectSlim BuildTargetEvents(EventObject eventObject, List<NamedEvents> typeEventsList) {
            // Check if the event ID and log name match any of the NamedEvents values
            foreach (var typeEvents in typeEventsList) {
                if (eventIdsMap.TryGetValue(typeEvents, out var eventInfo) &&
                    eventInfo.EventIds.Contains(eventObject.Id) &&
                    eventInfo.LogName == eventObject.LogName) {
                    // Try reading ObjectClass if available
                    eventObject.Data.TryGetValue("ObjectClass", out var objectClass);

                    // If they match, create the appropriate event object based on the NamedEvents value
                    switch (typeEvents) {
                        // computer based events
                        case NamedEvents.ADComputerCreateChange:
                            return new ADComputerCreateChange(eventObject);
                        case NamedEvents.ADComputerChangeDetailed:
                            if (objectClass == "computer") {
                                return new ADComputerChangeDetailed(eventObject);
                            }
                            break;
                        case NamedEvents.ADComputerDeleted:
                            return new ADComputerDeleted(eventObject);

                        // group based events
                        case NamedEvents.ADGroupMembershipChange:
                            return new ADGroupMembershipChange(eventObject);
                        case NamedEvents.ADGroupEnumeration:
                            return new ADGroupEnumeration(eventObject);
                        case NamedEvents.ADGroupChange:
                            // this is a special case where we ignore *ANONYMOUS* events
                            // those happen but are not useful at all and just clutter the view
                            var adGroupChange = new ADGroupChange(eventObject);
                            if (adGroupChange.Who == "*ANONYMOUS*") {
                                return null;
                            } else {
                                return adGroupChange;
                            }
                        case NamedEvents.ADGroupCreateDelete:
                            return new ADGroupCreateDelete(eventObject);
                        case NamedEvents.ADGroupChangeDetailed:
                            if (objectClass == "user") {
                                return new ADGroupChangeDetailed(eventObject);
                            }
                            break;

                        // user based events
                        case NamedEvents.ADUserCreateChange:
                            return new ADUserCreateChange(eventObject);
                        case NamedEvents.ADUserStatus:
                            return new ADUserStatus(eventObject);
                        case NamedEvents.ADUserChangeDetailed:
                            if (objectClass == "user") {
                                return new ADUserChangeDetailed(eventObject);
                            }
                            break;
                        case NamedEvents.ADUserLockouts:
                            return new ADUserLockouts(eventObject);
                        case NamedEvents.ADUserLogon:
                            return new ADUserLogon(eventObject);
                        case NamedEvents.ADUserLogonNTLMv1:
                            if (eventObject.ValueMatches("LmPackageName", "NTLM V1")) {
                                return new ADUserLogonNTLMv1(eventObject);
                            }
                            break;
                        case NamedEvents.ADUserLogonKerberos:
                            return new ADUserLogonKerberos(eventObject);
                        case NamedEvents.ADUserLogonFailed:
                            return new ADUserLogonFailed(eventObject);
                        case NamedEvents.ADUserUnlocked:
                            return new ADUserUnlocked(eventObject);
                        case NamedEvents.KerberosServiceTicket:
                            return new KerberosServiceTicket(eventObject);
                        case NamedEvents.KerberosTicketFailure:
                            return new KerberosTicketFailure(eventObject);
                        // organizational unit and other events
                        case NamedEvents.ADOrganizationalUnitChangeDetailed:
                            if (objectClass == "organizationalUnit" && eventObject.Data["AttributeLDAPDisplayName"] != "qPLik") {
                                return new ADOrganizationalUnitChangeDetailed(eventObject);
                            }
                            break;
                        case NamedEvents.ADOtherChangeDetailed:
                            if (objectClass != "user"
                                && objectClass != "computer"
                                && objectClass != "organizationalUnit"
                                && objectClass != "group"
                                ) {
                                return new ADOtherChangeDetailed(eventObject);
                            }
                            break;
                        // ldap events
                        case NamedEvents.ADLdapBindingSummary:
                            return new ADLdapBindingSummary(eventObject);
                        case NamedEvents.ADLdapBindingDetails:
                            return new ADLdapBindingDetails(eventObject);
                        case NamedEvents.LogsClearedSecurity:
                            return new LogsClearedSecurity(eventObject);
                        case NamedEvents.LogsClearedOther:
                            return new LogsClearedOther(eventObject);
                        case NamedEvents.OSCrash:
                            return new OSCrash(eventObject);
                        case NamedEvents.OSStartupShutdownCrash:
                            return new OSStartupShutdownCrash(eventObject);
                        case NamedEvents.OSTimeChange:
                            return new OSTimeChange(eventObject);
                        case NamedEvents.ADSMBServerAuditV1:
                            return SMBServerAudit.Create(eventObject);
                        case NamedEvents.ADGroupPolicyChanges:
                            if (objectClass == "groupPolicyContainer" || objectClass == "container") {
                                return new ADGroupPolicyChanges(eventObject);
                            }
                            break;
                        case NamedEvents.ADGroupPolicyLinks:
                            if ((objectClass == "domainDNS" || objectClass == "organizationalUnit" || objectClass == "site")
                                 && eventObject.ValueMatches("AttributeLDAPDisplayName", "gpLink")) {
                                return new ADGroupPolicyLinks(eventObject);
                            }
                            break;
                        case NamedEvents.ADGroupPolicyEdits:
                            if (objectClass == "groupPolicyContainer"
                                && eventObject.Data.TryGetValue("AttributeLDAPDisplayName", out var ldapDisplayObjName)
                                && ldapDisplayObjName is string ldapDisplayNameValue
                                && ldapDisplayNameValue == "versionNumber") {
                                return new ADGroupPolicyEdits(eventObject);
                            }
                            break;
                        case NamedEvents.GpoCreated:
                            if (objectClass == "groupPolicyContainer") {
                                return new GpoCreated(eventObject);
                            }
                            break;
                        case NamedEvents.GpoDeleted:
                            if (objectClass == "groupPolicyContainer") {
                                return new GpoDeleted(eventObject);
                            }
                            break;
                        case NamedEvents.GpoModified:
                            if (objectClass == "groupPolicyContainer") {
                                return new GpoModified(eventObject);
                            }
                            break;

                        default:
                            throw new ArgumentException($"You forgot to add NamedEvents value properly: {typeEvents}");
                    }
                }
            }

            // If no match is found, return null or throw an exception
            return null;
        }
    }
}