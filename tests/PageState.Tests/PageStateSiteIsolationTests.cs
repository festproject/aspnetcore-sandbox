using PageState.Internal;

namespace PageState.Tests;

/// <summary>
/// The purpose-chain site-keying added on top of §4.4: a [PageState] property is identified by
/// its declaration site (container type + property name), not just its value type, so a bare
/// primitive like `int` is exactly as isolated as a dedicated wrapper record.
/// </summary>
public class PageStateSiteIsolationTests
{
    private sealed class HostA;
    private sealed class HostB;

    [Fact]
    public void Protect_Unprotect_RoundTrips_ForBarePrimitiveWithSite()
    {
        var protector = TestFactory.CreateProtector();
        var site = new PageStateSite(typeof(HostA), "OrderId");

        var token = protector.Protect(123, owner: null, site);
        var result = protector.Unprotect<int>(token, owner: null, site);

        Assert.True(result.IsSuccess);
        Assert.Equal(123, result.State);
    }

    [Fact]
    public void Unprotect_ReturnsInvalid_WhenSiteContainerTypeDiffers()
    {
        var protector = TestFactory.CreateProtector();
        var siteA = new PageStateSite(typeof(HostA), "OrderId");
        var siteB = new PageStateSite(typeof(HostB), "OrderId");

        var token = protector.Protect(123, owner: null, siteA);
        var result = protector.Unprotect<int>(token, owner: null, siteB);

        Assert.Equal(PageStateStatus.Invalid, result.Status);
    }

    [Fact]
    public void Unprotect_ReturnsInvalid_WhenSamePropertyNameOnDifferentContainers_TokensAreNotInterchangeable()
    {
        // The exact bug this design closes: two different `[PageState] int OrderId` properties on
        // two different view models used to share the purpose "System.Int32" and were interchangeable.
        var protector = TestFactory.CreateProtector();
        var siteOnA = new PageStateSite(typeof(HostA), "OrderId");
        var siteOnB = new PageStateSite(typeof(HostB), "OrderId");

        var token = protector.Protect(123, owner: null, siteOnA);
        var result = protector.Unprotect<int>(token, owner: null, siteOnB);

        Assert.Equal(PageStateStatus.Invalid, result.Status);
    }

    [Fact]
    public void Unprotect_ReturnsInvalid_WhenPropertyRenamed_SimulatingADeploy()
    {
        var protector = TestFactory.CreateProtector();
        var before = new PageStateSite(typeof(HostA), "Foo");
        var after = new PageStateSite(typeof(HostA), "Bar");

        var token = protector.Protect(123, owner: null, before);
        var result = protector.Unprotect<int>(token, owner: null, after);

        Assert.Equal(PageStateStatus.Invalid, result.Status);
    }

    [Fact]
    public void Unprotect_ReturnsInvalid_WhenTwoDifferentPropertiesOnTheSameContainer_TokensAreNotInterchangeable()
    {
        var protector = TestFactory.CreateProtector();
        var orderId = new PageStateSite(typeof(HostA), "OrderId");
        var quantity = new PageStateSite(typeof(HostA), "Quantity");

        var token = protector.Protect(5, owner: null, orderId);
        var result = protector.Unprotect<int>(token, owner: null, quantity);

        Assert.Equal(PageStateStatus.Invalid, result.Status);
    }

    [Fact]
    public void Unprotect_ReturnsInvalid_WhenMintedWithNoSite_ButReadWithASite()
    {
        // @Html.PageState's ad-hoc escape hatch (default site) must not be confused with a
        // property-bound token for the same value type.
        var protector = TestFactory.CreateProtector();
        var site = new PageStateSite(typeof(HostA), "OrderId");

        var noSiteToken = protector.Protect(123, owner: null);
        var result = protector.Unprotect<int>(noSiteToken, owner: null, site);

        Assert.Equal(PageStateStatus.Invalid, result.Status);
    }
}

public class PageStateRenderingPropertyDiscoveryTests
{
    public sealed class MultiFieldModel
    {
        [PageState]
        public int OrderId { get; set; }

        public string? NotPageState { get; set; }

        [PageState]
        public TypeAState? Extra { get; set; }
    }

    [Fact]
    public void FindPageStateProperties_ReturnsEveryPageStateProperty_ExcludingUnattributedOnes()
    {
        var properties = PageStateRendering.FindPageStateProperties(typeof(MultiFieldModel));

        Assert.Equal(["OrderId", "Extra"], properties.Select(p => p.Name));
    }
}
