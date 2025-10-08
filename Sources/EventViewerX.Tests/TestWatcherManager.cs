using System;
using System.Collections.Generic;
using System.Reflection;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using Xunit;

namespace EventViewerX.Tests {
    public class TestWatcherManager {
        [Fact]
        public void StartWatcherReturnsExistingInstance() {
            var first = WatcherManager.StartWatcher(
                "unit", Environment.MachineName, "Application", new List<int>(), new List<NamedEvents>(), _ => { }, 1, false, false, 0, null);
            var second = WatcherManager.StartWatcher(
                "unit", Environment.MachineName, "Application", new List<int>(), new List<NamedEvents>(), _ => { }, 1, false, false, 0, null);
            Assert.Same(first, second);
            WatcherManager.StopAll();
        }

        [Fact]
        public void StartWatcherThrowsWhenDuplicatesExist() {
            var field = typeof(WatcherManager).GetField("_watchers", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(field);
            var dict = (ConcurrentDictionary<Guid, WatcherInfo>)field!.GetValue(null)!;
            var watcher1 = new WatcherInfo("dup", Environment.MachineName, "Application", new List<int>(), new List<NamedEvents>(), _ => { }, 1, false, false, 0, null);
            var watcher2 = new WatcherInfo("dup", Environment.MachineName, "Application", new List<int>(), new List<NamedEvents>(), _ => { }, 1, false, false, 0, null);
            dict.TryAdd(Guid.NewGuid(), watcher1);
            dict.TryAdd(Guid.NewGuid(), watcher2);

            var ex = Assert.Throws<InvalidOperationException>(() =>
                WatcherManager.StartWatcher("dup", Environment.MachineName, "Application", new List<int>(), new List<NamedEvents>(), _ => { }, 1, false, false, 0, null));
            Assert.Contains("Multiple watchers", ex.Message);
            WatcherManager.StopAll();
        }

        [Fact]
        public void OnEventLogsWarningOnException() {
            TimeSpan? timeout = null;
            object?[] args = {
                "test", Environment.MachineName, "Application", new List<int> { 1 },
                new List<NamedEvents>(), new Action<EventObject>(_ => throw new InvalidOperationException("fail")),
                1, false, false, 0, timeout
            };
            var info = (WatcherInfo)Activator.CreateInstance(
                typeof(WatcherInfo),
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                args: args,
                culture: null)!;

            Exception? captured = null;
            info.ActionException += (_, ex) => captured = ex;
            string? message = null;
            EventHandler<LogEventArgs> handler = (_, e) => message = e.FullMessage;
            Settings._logger.OnWarningMessage += handler;
            try {
                #pragma warning disable SYSLIB0050 // Formatter-based serialization is obsolete; used only to synthesize instance for private method invocation.
                var dummy = (EventObject)FormatterServices.GetUninitializedObject(typeof(EventObject));
                #pragma warning restore SYSLIB0050
                var method = typeof(WatcherInfo).GetMethod("OnEvent", BindingFlags.Instance | BindingFlags.NonPublic)!;
                method.Invoke(info, new object[] { dummy });
                Assert.NotNull(captured);
                Assert.Contains("fail", captured!.Message);
                Assert.NotNull(message);
            } finally {
                Settings._logger.OnWarningMessage -= handler;
            }
        }

        [Fact]
        public async Task StartWatcherIsThreadSafe() {
            var tasks = new List<Task<WatcherInfo>>();
            for (int i = 0; i < 5; i++) {
                tasks.Add(Task.Run(() => WatcherManager.StartWatcher(
                    "sync", Environment.MachineName, "Application", new List<int>(),
                    new List<NamedEvents>(), _ => { }, 1, false, false, 0, null)));
            }
            var results = await Task.WhenAll(tasks);
            var first = results[0];
            foreach (var r in results) {
                Assert.Same(first, r);
            }
            WatcherManager.StopAll();
        }
    }
}
