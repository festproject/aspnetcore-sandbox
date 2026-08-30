using System.Net;
using Microsoft.Extensions.DependencyInjection;
using PageState.IntegrationTests.TestSupport;

namespace PageState.IntegrationTests;

public class AntiforgeryOrderingTests : IClassFixture<SpyProtectorWebApplicationFactory>
{
    private readonly SpyProtectorWebApplicationFactory _factory;

    public AntiforgeryOrderingTests(SpyProtectorWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task MissingAntiforgeryToken_Returns400_BeforeProtectorIsInvoked()
    {
        var client = _factory.CreateClient();
        var spy = (SpyPageStateProtector)_factory.Services.GetRequiredService<PageState.IPageStateProtector>();
        var unprotectCountBefore = spy.UnprotectCallCount;

        var response = await client.PostAsync("/Stateful/PageStateDemo", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["__pagestate.OrderId"] = "irrelevant-because-antiforgery-should-reject-first",
                ["CustomerName"] = "Mallory",
                ["Product"] = "P1"
            }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(unprotectCountBefore, spy.UnprotectCallCount);
    }
}
