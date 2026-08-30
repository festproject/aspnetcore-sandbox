using System.Net;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using PageState.IntegrationTests.TestSupport;

namespace PageState.IntegrationTests;

// All InvocationCount-based assertions live in this one class: xUnit runs test methods within a
// class sequentially by default, which keeps the static counter on ProbeController race-free
// without needing a [Collection] attribute.
public class PageStateShortCircuitTests : IClassFixture<ProbeWebApplicationFactory>
{
    private readonly ProbeWebApplicationFactory _factory;

    public PageStateShortCircuitTests(ProbeWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task TamperedToken_ShortCircuits_AndActionBodyNeverRuns()
    {
        ProbeController.InvocationCount = 0;
        var client = _factory.CreateClient();
        var protector = _factory.Services.GetRequiredService<PageState.IPageStateProtector>();

        var token = protector.Protect(new ProbeState(42), owner: null);
        var bytes = WebEncoders.Base64UrlDecode(token);
        bytes[bytes.Length / 2] ^= 0xFF;
        var tampered = WebEncoders.Base64UrlEncode(bytes);

        var response = await client.PostAsync("/test-support/probe", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["__pagestate.State"] = tampered }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, ProbeController.InvocationCount);
    }

    [Fact]
    public async Task MissingToken_ShortCircuits_AndActionBodyNeverRuns()
    {
        ProbeController.InvocationCount = 0;
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/test-support/probe", new FormUrlEncodedContent(
            new Dictionary<string, string>()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, ProbeController.InvocationCount);
    }

    [Fact]
    public async Task TokenFromDifferentViewModel_ShortCircuits_AndActionBodyNeverRuns()
    {
        ProbeController.InvocationCount = 0;
        var client = _factory.CreateClient();

        // Same factory, same key ring — the demo's OrderId token is minted under a different
        // purpose (its declaration site, OrderEditViewModel.OrderId) than ProbeViewModel.State,
        // so isolation here is proven by site, not incidentally by a mismatched key ring.
        var html = await client.GetStringAsync("/Stateful/PageStateDemo");
        var orderEditToken = OrderEditPageStateDemoTests.ExtractFieldValue(html, "__pagestate.OrderId");

        var response = await client.PostAsync("/test-support/probe", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["__pagestate.State"] = orderEditToken }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, ProbeController.InvocationCount);
    }
}
