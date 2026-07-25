using EventViewerX.Providers;
using Xunit;

namespace EventViewerX.Tests;

public sealed class TestEventProviderCompatibility {
    [Fact]
    public void RejectsTaskEventGuidChanges() {
        EventProviderDefinition baseline =
            TestEventProviderPackages.CreateDefinition();
        baseline.Tasks.Add(
            new EventProviderTaskDefinition {
                Name = "ScanTask",
                Value = 1,
                EventGuid = Guid.NewGuid()
            });
        EventProviderDefinition candidate =
            TestEventProviderPackages.CreateDefinition();
        candidate.Tasks.Add(
            new EventProviderTaskDefinition {
                Name = "ScanTask",
                Value = 1,
                EventGuid = Guid.NewGuid()
            });

        EventProviderCompatibilityResult result =
            EventProviderCompatibility.Compare(
                baseline,
                candidate);

        Assert.False(result.IsCompatible);
        Assert.Contains(
            result.Issues,
            issue => issue.Code ==
                     "TaskEventGuidChanged");
    }
}
