using EventViewerX.Rules.ActiveDirectory;
using EventViewerX.Rules.Logging;
using EventViewerX.Rules.Windows;
using EventViewerX.Rules.Kerberos;
using EventViewerX.Rules.CertificateAuthority;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace EventViewerX {
    public partial class SearchEvents : Settings {
        /// <summary>
        /// Builds the appropriate event object based on the NamedEvents value
        /// </summary>
        /// <param name="eventObject"></param>
        /// <param name="typeEventsList"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private static EventObjectSlim BuildTargetEvents(EventObject eventObject, List<NamedEvents> typeEventsList) {
            // Use the new reflection-based system - let each rule decide if it can handle the event
            return EventObjectSlim.CreateEventRule(eventObject, typeEventsList);
        }
    }
}