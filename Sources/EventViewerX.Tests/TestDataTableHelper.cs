using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using EventViewerX.Helpers;
using Xunit;

namespace EventViewerX.Tests;

public class TestDataTableHelper {
    private class SimpleEvent {
        public int Id { get; set; }
        public string Name { get; set; }
        public Dictionary<string, string> Data { get; set; } = new();
    }

    [Fact]
    public void CachesTypeMembers() {
        var helperType = typeof(DataTableHelper);
        var cacheField = helperType.GetField("_cache", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(cacheField);

        dynamic cache = cacheField!.GetValue(null)!;
        cache.Clear();

        var method = helperType.GetMethod("ToDataTableInternal", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        var generic = method!.MakeGenericMethod(typeof(SimpleEvent));

        var items = new List<SimpleEvent> {
            new() { Id = 1, Name = "A", Data = new() { { "Key", "Value" } } }
        };

        generic.Invoke(null, new object[] { items });

        Assert.Equal(1, (int)cache.Count);
        var first = cache[typeof(SimpleEvent)];

        generic.Invoke(null, new object[] { items });

        Assert.Equal(1, (int)cache.Count);
        var second = cache[typeof(SimpleEvent)];

        Assert.Same(first, second);
    }
}
