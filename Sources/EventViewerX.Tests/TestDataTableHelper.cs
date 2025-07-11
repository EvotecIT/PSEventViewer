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

        object cache = cacheField!.GetValue(null)!;
        MethodInfo clearMethod = cacheField.FieldType.GetMethod("Clear")!;
        clearMethod.Invoke(cache, null);

        var method = helperType.GetMethod("ToDataTableInternal", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        var generic = method!.MakeGenericMethod(typeof(SimpleEvent));

        var items = new List<SimpleEvent> {
            new() { Id = 1, Name = "A", Data = new() { { "Key", "Value" } } }
        };

        generic.Invoke(null, new object[] { items });

        PropertyInfo countProperty = cacheField.FieldType.GetProperty("Count")!;
        Assert.Equal(1, (int)countProperty.GetValue(cache)!);
        PropertyInfo indexer = cacheField.FieldType.GetProperty("Item")!;
        var first = indexer.GetValue(cache, new object[] { typeof(SimpleEvent) });

        generic.Invoke(null, new object[] { items });

        Assert.Equal(1, (int)countProperty.GetValue(cache)!);
        var second = indexer.GetValue(cache, new object[] { typeof(SimpleEvent) });

        Assert.Same(first, second);
    }
}
