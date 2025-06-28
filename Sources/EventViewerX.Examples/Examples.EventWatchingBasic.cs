﻿namespace EventViewerX.Examples {
    internal partial class Examples {

        public static void EventWatchingBasic() {
            WatchEvents c1 = new WatchEvents {
                Warning = true,
                Verbose = true
            };
            c1.Watch("AD1", "Security", new List<int>() { 4627, 4624 });
        }
    }
}