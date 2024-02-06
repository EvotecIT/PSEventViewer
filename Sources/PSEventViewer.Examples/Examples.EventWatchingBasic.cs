using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSEventViewer.Examples {
    internal partial class Examples {

        public static void EventWatchingBasic() {
            EventWatching c1 = new EventWatching {
                Warning = true,
                Verbose = true
            };
            c1.Watch("AD1", "Security", new List<int>() { 4627, 4624 });
        }
    }
}
