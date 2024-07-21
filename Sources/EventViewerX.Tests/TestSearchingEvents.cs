using Xunit;

namespace EventViewerX.Tests {
    public class TestSearchingEvents {
        [Fact]
        public void QuerySecurityEvents4932and4933() {
            var fields = new List<string>() { "DestinationDRA", "SourceDRA", "NamingContext", "Options", "SessionID", "EndUSN", "StatusCode", "StartUSN" };
            foreach (var eventObject in SearchEvents.QueryLog("Security", [4932, 4933], "AD1")) {
                Assert.True(eventObject.Id == 4932 || eventObject.Id == 4933);
                Assert.True(eventObject.Data.Count == 6 || eventObject.Data.Count == 7);

                foreach (var data in eventObject.Data) {
                    Assert.Contains(data.Key, fields);
                }
            }
        }
        [Fact]
        public void QuerySetupEventID2() {
            var fields = new List<string>() { "PackageIdentifier" };
            foreach (var eventObject in SearchEvents.QueryLog("Setup", [2])) {
                Assert.True(eventObject.Id == 2);
                Assert.True(eventObject.Data.Count == 5);

                foreach (var field in fields) {
                    Assert.Contains(field, eventObject.Data.Keys);
                }

            }
        }
        [Fact]
        public void QuerySetupEventID1() {
            var fields = new List<string>() { "PackageIdentifier", "InitialPackageState" };
            foreach (var eventObject in SearchEvents.QueryLog("Setup", [1])) {
                Assert.True(eventObject.Id == 1);
                Assert.True(eventObject.Data.Count == 6);

                foreach (var field in fields) {
                    Assert.Contains(field, eventObject.Data.Keys);
                }

            }
        }
        [Fact]
        public void QuerySystemEventID566() {
            var fields = new List<string>() { "BootId", "Reason", "MonitorReason" };
            foreach (var eventObject in SearchEvents.QueryLog("System", [566])) {
                Assert.True(eventObject.Id == 566);
                Assert.True(eventObject.Data.Count == 13);

                foreach (var field in fields) {
                    Assert.Contains(field, eventObject.Data.Keys);
                }

            }
        }

        [Fact]
        public void QueryApplicationEvent10005() {
            var fields = new List<string>() { "RmSessionId", "nApplications", "RebootReasons", "Applications" };
            foreach (var eventObject in SearchEvents.QueryLog("Application", [10005])) {
                Assert.True(eventObject.Id == 10005);

                foreach (var field in fields) {
                    Assert.Contains(field, eventObject.Data.Keys);
                }

            }
        }
        [Fact]
        public void QueryApplicationEvent100() {
            var fields = new List<string>() { "NoNameA0" };
            foreach (var eventObject in SearchEvents.QueryLog("Application", [100])) {
                Assert.True(eventObject.Id == 100);

                foreach (var field in fields) {
                    Assert.Contains(field, eventObject.Data.Keys);
                }

            }
        }
    }
}