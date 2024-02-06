namespace PSEventViewer.Tests {
    public class TestSearchingEvents {
        [Fact]
        public void TestEvents4932() {
            var fields = new List<string>() { "DestinationDRA", "SourceDRA", "NamingContext", "Options", "SessionID", "EndUSN", "StatusCode", "StartUSN" };
            foreach (var eventObject in EventSearching.QueryLog("Security", [4932, 4933], "AD1")) {
                Assert.True(eventObject.Id == 4932 || eventObject.Id == 4933);
                Assert.True(eventObject.Data.Count == 6 || eventObject.Data.Count == 7);

                foreach (var data in eventObject.Data) {
                    Assert.Contains(data.Key, fields);
                }
            }
        }
    }
}