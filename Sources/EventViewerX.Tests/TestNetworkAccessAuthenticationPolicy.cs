using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;
using EventViewerX;

namespace EventViewerX.Tests {
    public class TestNetworkAccessAuthenticationPolicy {
        private static Dictionary<string, string> Parse(string xml) {
            #pragma warning disable SYSLIB0050 // Formatter-based serialization is obsolete; used only to create uninitialized instance for private method invocation in tests.
            var obj = (EventObject)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(EventObject));
            #pragma warning restore SYSLIB0050
            var method = typeof(EventObject).GetMethod("ParseXML", BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(typeof(Dictionary<string, string>));
            return (Dictionary<string, string>)method.Invoke(obj, new object[] { xml })!;
        }

        [Fact]
        public void ParsesKnownAuthenticationType() {
            const string xml = "<Event><EventData><Data Name='AuthenticationType'>PAP</Data></EventData></Event>";
            var data = Parse(xml);
            var result = Rules.NPS.NetworkAccessAuthenticationPolicy.ParseAuthenticationType(data["AuthenticationType"]);
            Assert.Equal(AuthenticationType.PAP, result);
        }

        [Fact]
        public void UnknownAuthenticationTypeDefaultsToUnknown() {
            const string xml = "<Event><EventData><Data Name='AuthenticationType'>UNKNOWN_VALUE</Data></EventData></Event>";
            var data = Parse(xml);
            var result = Rules.NPS.NetworkAccessAuthenticationPolicy.ParseAuthenticationType(data["AuthenticationType"]);
            Assert.Equal(AuthenticationType.Unknown, result);
        }
    }
}
