using System;
using System.Data;
using System.Reflection;
using EventViewerX.Helpers;
using Xunit;

namespace EventViewerX.Tests {
    public class TestDataTableHelper {
        private class Dummy {
            public int? IntValue { get; set; }
            public DateTime? DateValue { get; set; }
            public double? DoubleValue;
        }

        [Fact]
        public void NullablePrimitivePropertiesHandled() {
            var items = new[] {
                new Dummy {
                    IntValue = 5,
                    DateValue = new DateTime(2024, 1, 1),
                    DoubleValue = 1.5
                },
                new Dummy()
            };

            var method = typeof(DataTableHelper).GetMethod("ToDataTableInternal", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            var generic = method!.MakeGenericMethod(typeof(Dummy));
            var table = (DataTable)generic.Invoke(null, new object[] { items });

            Assert.Equal(3, table.Columns.Count);
            Assert.Equal(typeof(int), table.Columns["IntValue"].DataType);
            Assert.Equal(typeof(DateTime), table.Columns["DateValue"].DataType);
            Assert.Equal(typeof(double), table.Columns["DoubleValue"].DataType);

            Assert.Equal(2, table.Rows.Count);
            Assert.Equal(5, table.Rows[0]["IntValue"]);
            Assert.Equal(new DateTime(2024, 1, 1), table.Rows[0]["DateValue"]);
            Assert.Equal(1.5, table.Rows[0]["DoubleValue"]);
            Assert.Equal(DBNull.Value, table.Rows[1]["IntValue"]);
            Assert.Equal(DBNull.Value, table.Rows[1]["DateValue"]);
            Assert.Equal(DBNull.Value, table.Rows[1]["DoubleValue"]);
        }
    }
}
