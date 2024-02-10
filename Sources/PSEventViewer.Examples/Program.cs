using PSEventViewer;
using PSEventViewer.Examples;

//Examples.EventWatchingBasic();
//Examples.QueryBasic();
//Examples.QueryBasicWithOutput();
//Examples.QueryParallelSpeed();
//Examples.QueryParallelWithCount();

//Examples.QueryParallelCompare();
//Examples.QueryBasicParallelForEach();
//Examples.QueryBasicForwardedEvents();

//Examples.FindEventsTargetedBasic();
//Examples.FindEventsTargetedPerType();

var eventSearching = new EventSearching();
eventSearching.Verbose = true;
List<string> MachineName = new List<string> { "AD1", "AD2", "AD0" };
string LogName = "Security";
List<int> EventId = new List<int>() { 5136 };

var queryTask = eventSearching.QueryLogParallel(MachineName, LogName, EventId);
queryTask.GetAwaiter().GetResult();