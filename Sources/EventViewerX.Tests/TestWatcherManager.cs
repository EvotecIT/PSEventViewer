using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Xunit;

namespace EventViewerX.Tests {
    public class TestWatcherManager {
        [Fact]
        public void OnEventLogsWarningOnException() {
            var info = (WatcherInfo)Activator.CreateInstance(typeof(WatcherInfo),
                BindingFlags.Instance | BindingFlags.NonPublic, null,
                new object[] {
                    "test", Environment.MachineName, "Application", new List<int> { 1 },
                    new List<NamedEvents>(), new Action<EventObject>(_ => throw new InvalidOperationException("fail")),
                    1, false, false, 0, null
                }, null)!;

            Exception? captured = null;
            info.ActionException += (_, ex) => captured = ex;
            string? message = null;
            EventHandler<LogEventArgs> handler = (_, e) => message = e.FullMessage;
            Settings._logger.OnWarningMessage += handler;
            try {
                var dummy = (EventObject)FormatterServices.GetUninitializedObject(typeof(EventObject));
                var method = typeof(WatcherInfo).GetMethod("OnEvent", BindingFlags.Instance | BindingFlags.NonPublic)!;
                method.Invoke(info, new object[] { dummy });
                Assert.NotNull(captured);
                Assert.Contains("fail", captured!.Message);
                Assert.NotNull(message);
            } finally {
                Settings._logger.OnWarningMessage -= handler;
            }
        }
    }
}
