using System.Net;
using PageState.IntegrationTests.TestSupport;

namespace PageState.IntegrationTests;

public class OwnerChangeTests
{
    [Fact]
    public async Task OwnerChangeBetweenGetAndPost_ShortCircuits()
    {
        using var factory = new OwnerChangeWebApplicationFactory();
        factory.OwnerProvider.CurrentOwner = "user-a";
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/Stateful/PageStateDemo");
        var token = OrderEditPageStateDemoTests.ExtractFieldValue(html, "__pagestate.OrderId");
        var afToken = OrderEditPageStateDemoTests.ExtractFieldValue(html, "__RequestVerificationToken");

        factory.OwnerProvider.CurrentOwner = "user-b"; // simulated re-login between GET and POST

        var response = await client.PostAsync("/Stateful/PageStateDemo", OrderEditPageStateDemoTests.BuildForm(
            ("__pagestate.OrderId", token),
            ("__RequestVerificationToken", afToken),
            ("CustomerName", "Eve"),
            ("Product", "P1")));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("This page is no longer valid", await response.Content.ReadAsStringAsync());
    }
}
