using System;
using System.Collections.Generic;
using PSEventViewer.Rules.ActiveDirectory;
using PSEventViewer.Rules.Logs;
using PSEventViewer.Rules.Windows;

namespace PSEventViewer {
    /// <summary>
    /// Defines the named events that can be searched for
    /// </summary>
    public enum NamedEvents {
        ADComputerCreateChange,
        ADComputerDeleted,
        ADComputerChangeDetailed,

        ADGroupMembershipChange,
        ADGroupEnumeration,
        ADGroupChange,
        ADGroupCreateDelete,
        ADGroupChangeDetailed,

        ADLdapBindingSummary,
        ADLdapBindingDetails,
        ADUserCreateChange,
        ADUserStatus,
        ADUserChangeDetailed,
        ADUserLockouts,
        ADUserLogon,
        ADUserLogonKerberos,
        ADUserUnlocked,
        ADOrganizationalUnitChangeDetailed,
        ADOtherChangeDetailed,

        LogsClearedSecurity,
        LogsClearedOther,
        LogsFullSecurity,

        NetworkAccessAuthenticationPolicy,

        OSCrash,
        OSStartupShutdownCrash,
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
            // user based events
            { NamedEvents.ADUserCreateChange, (new List<int> { 4720, 4738 }, "Security") },
            { NamedEvents.ADUserStatus, (new List<int> { 4722, 4725, 4723, 4724, 4726 }, "Security") },
            { NamedEvents.ADUserChangeDetailed, (new List<int> { 5136, 5137, 5139, 5141 }, "Security") },
            { NamedEvents.ADOrganizationalUnitChangeDetailed, (new List<int> { 5136, 5137, 5139, 5141 }, "Security") },
            { NamedEvents.ADUserLockouts, (new List<int> { 4740 }, "Security") },
            { NamedEvents.ADUserLogon, (new List<int> { 4624 }, "Security") },
            { NamedEvents.ADUserLogonKerberos, (new List<int> { 4768 }, "Security") },
            { NamedEvents.ADUserUnlocked, (new List<int> { 4767 }, "Security") },
            // other based events
            { NamedEvents.ADOtherChangeDetailed, (new List<int> { 5136, 5137, 5139, 5141 }, "Security") },
            // ldap events
            { NamedEvents.ADLdapBindingSummary, (new List<int> { 2887 }, "Directory Service") },
            { NamedEvents.ADLdapBindingDetails,(new List<int> { 2889 }, "Directory Service") },
            // logs cleared
            { NamedEvents.LogsClearedSecurity, (new List<int> { 1102,1105 }, "Security") },
            { NamedEvents.LogsClearedOther,(new List<int> { 104 }, "System") },
            { NamedEvents.LogsFullSecurity, (new List<int> { 1104  }, "Security") },
            // network access
            { NamedEvents.NetworkAccessAuthenticationPolicy, (new List<int> { 6272, 6273 }, "Security") },
            // windows OS
            { NamedEvents.OSCrash, (new List<int> { 6008 }, "System") },
            { NamedEvents.OSStartupShutdownCrash,  (new List<int> { 12, 13, 41, 4608, 4621, 6008 }, "System") },
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
                    // If they match, create the appropriate event object based on the NamedEvents value
                    switch (typeEvents) {
                        // computer based events
                        case NamedEvents.ADComputerCreateChange:
                            return new ADComputerCreateChange(eventObject);
                        case NamedEvents.ADComputerChangeDetailed:
                            if (eventObject.Data["ObjectClass"] == "computer") {
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
                            if (eventObject.Data["ObjectClass"] == "user") {
                                return new ADGroupChangeDetailed(eventObject);
                            }
                            break;

                        // user based events
                        case NamedEvents.ADUserCreateChange:
                            return new ADUserCreateChange(eventObject);
                        case NamedEvents.ADUserStatus:
                            return new ADUserStatus(eventObject);
                        case NamedEvents.ADUserChangeDetailed:
                            if (eventObject.Data["ObjectClass"] == "user") {
                                return new ADUserChangeDetailed(eventObject);
                            }
                            break;
                        case NamedEvents.ADUserLockouts:
                            return new ADUserLockouts(eventObject);
                        case NamedEvents.ADUserLogon:
                            return new ADUserLogon(eventObject);
                        case NamedEvents.ADUserUnlocked:
                            return new ADUserUnlocked(eventObject);

                        // organizational unit and other events
                        case NamedEvents.ADOrganizationalUnitChangeDetailed:
                            if (eventObject.Data["ObjectClass"] == "organizationalUnit") {
                                return new ADOrganizationalUnitChangeDetailed(eventObject);
                            }
                            break;
                        case NamedEvents.ADOtherChangeDetailed:
                            if (eventObject.Data["ObjectClass"] != "user"
                                && eventObject.Data["ObjectClass"] != "computer"
                                && eventObject.Data["ObjectClass"] != "organizationalUnit"
                                && eventObject.Data["ObjectClass"] != "group"
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
                        default:
                            throw new ArgumentException($"Invalid NamedEvents value: {typeEvents}");
                    }
                }
            }

            // If no match is found, return null or throw an exception
            return null;
        }
    }
}
