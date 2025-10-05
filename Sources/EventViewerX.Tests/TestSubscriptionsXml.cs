using System;
using Xunit;

namespace EventViewerX.Tests;

public class TestSubscriptionsXml
{
    [Fact]
    public void SetCollectorSubscriptionXml_RejectsInvalidRoot()
    {
        // Does not hit registry because it fails validation first
        var badXml = "<NotSubscription></NotSubscription>";
        var ex = Assert.Throws<ArgumentException>(() => SearchEvents.SetCollectorSubscriptionXml("UnitTest", badXml));
        Assert.Contains("Root element must be <Subscription>", ex.Message);
    }

    [Fact]
    public void SetCollectorSubscriptionXml_RejectsDtd()
    {
        // DTD is prohibited by validation to avoid XXE-style issues
        var dtdXml = "<!DOCTYPE foo [ <!ELEMENT foo ANY > ]><Subscription></Subscription>";
        var ex = Assert.Throws<ArgumentException>(() => SearchEvents.SetCollectorSubscriptionXml("UnitTest", dtdXml));
        Assert.Contains("Invalid XML content", ex.Message);
    }
}

