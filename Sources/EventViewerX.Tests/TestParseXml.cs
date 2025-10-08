using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Xunit;

namespace EventViewerX.Tests {
    public class TestParseXml {
        [Fact]
        public void DataDictionaryIsCaseInsensitive() {
            const string xml = "<Event><EventData><Data Name='Key'>Value</Data></EventData></Event>";
            #pragma warning disable SYSLIB0050 // Formatter-based serialization is obsolete; used only for test of private method.
            var obj = (EventObject)FormatterServices.GetUninitializedObject(typeof(EventObject));
            #pragma warning restore SYSLIB0050
            var method = typeof(EventObject).GetMethod("ParseXML", BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(typeof(Dictionary<string, string>));
            var result = (Dictionary<string, string>)method.Invoke(obj, new object[] { xml })!;
            Assert.Equal("Value", result["Key"]);
            Assert.Equal("Value", result["key"]);
        }
    }
}
