using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;

namespace EventViewerX {
    public partial class SearchEvents : Settings {
        /// <summary>
        /// Returns a list of domain controllers. When querying acrossForest is true,
        /// the entire forest is enumerated; otherwise only the current domain is used.
        /// If Active Directory is unavailable, the method falls back to the LOGONSERVER
        /// environment variable when set.
        /// </summary>
        public static List<string> GetDomainControllers(bool acrossForest = true) {
            var controllers = new List<string>();
            try {
                if (acrossForest) {
                    var forest = Forest.GetCurrentForest();
                    foreach (Domain domain in forest.Domains) {
                        foreach (DomainController dc in domain.DomainControllers) {
                            if (!controllers.Contains(dc.Name)) {
                                controllers.Add(dc.Name);
                            }
                        }
                    }
                } else {
                    var domain = Domain.GetComputerDomain();
                    foreach (DomainController dc in domain.DomainControllers) {
                        if (!controllers.Contains(dc.Name)) {
                            controllers.Add(dc.Name);
                        }
                    }
                }
            } catch {
                var logonServer = Environment.GetEnvironmentVariable("LOGONSERVER");
                if (!string.IsNullOrEmpty(logonServer)) {
                    controllers.Add(logonServer.TrimStart('\\'));
                }
            }

            return controllers;
        }
    }
}
