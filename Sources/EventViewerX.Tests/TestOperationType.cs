using System.Runtime.Serialization;
using Xunit;

namespace EventViewerX.Tests {
    public class TestOperationType {
        private static EventObjectSlim CreateSlim() {
            return (EventObjectSlim)FormatterServices.GetUninitializedObject(typeof(EventObjectSlim));
        }

        [Theory]
        [InlineData("%%14674", OperationType.ValueAdded)]
        [InlineData("%%14675", OperationType.ValueDeleted)]
        [InlineData("%%14676", OperationType.Unknown)]
        [InlineData("invalid", OperationType.Unknown)]
        public void ParsesCodes(string input, OperationType expected) {
            var slim = CreateSlim();
            var result = slim.ConvertFromOperationType(input);
            Assert.Equal(expected, result);
        }
    }
}
