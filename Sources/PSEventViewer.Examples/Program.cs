using PSEventViewer;

//Console.WriteLine("Hello, World!");

//Class1.QuerySecurityLog();


EventWatching c1 = new EventWatching();
c1.Warning = true;
c1.Verbose = true;
//c1.Watch("AD1", "Security", new List<int>() { 4627, 4624 });


//EventSearching.QueryLog("Security", new List<int>() { 4627, 4624 }, "AD1");


foreach (var eventObject in EventSearching.QueryLog("Security", [4932, 4933], "AD1")) {
    Console.WriteLine("Event ID: {0}", eventObject.Id);
    Console.WriteLine("Data count: " + eventObject.Data.Count);
    foreach (var data in eventObject.Data) {
        Console.WriteLine("[-] Data: {0} - {1}", data.Key, data.Value);
    }
}

//EventSearching.QueryLog("Application", [1008, 4098, 1001], "AD1");
//EventSearching.QueryLog("Application", [1001], "AD1");

Console.WriteLine("Press any key to continue...");
Console.ReadLine();