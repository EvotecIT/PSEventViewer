using System;
using System.Data;
using System.Linq;
using EventViewerX.Helpers;

namespace EventViewerX.Examples {
    internal partial class Examples {
        public static void DataTableConversion() {
            var events = SearchEvents.QueryLog("Application", [1000]).Take(5).ToList();
            DataTable table = events.ToDataTable();
            Console.WriteLine($"Converted rows: {table.Rows.Count}");
        }
    }
}
