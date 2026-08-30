using System.Net;
using System.Text.RegularExpressions;

namespace PageState.IntegrationTests;

public class OrderEditPageStateDemoTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public OrderEditPageStateDemoTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Get_RendersHiddenPageStateField_WithAutocompleteOff()
    {
        var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/Stateful/PageStateDemo");

        var match = Regex.Match(html, @"<input[^>]*name=""__pagestate\.OrderId""[^>]*>");
        Assert.True(match.Success, "Expected a hidden __pagestate.OrderId input.");
        Assert.Contains(@"autocomplete=""off""", match.Value);
    }

    [Fact]
    public async Task Get_RendersHydratedProductOptions_WithNoActionCodeSettingThem()
    {
        var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/Stateful/PageStateDemo");

        Assert.Contains(@"<option value=""P1"">Product 1</option>", html);
    }

    [Fact]
    public async Task GetThenPost_ValidInput_Returns200_WithSuccessMessage()
    {
        var client = _factory.CreateClient();
        var html = await client.GetStringAsync("/Stateful/PageStateDemo");
        var pageState = ExtractFieldValue(html, "__pagestate.OrderId");
        var afToken = ExtractFieldValue(html, "__RequestVerificationToken");

        var response = await client.PostAsync("/Stateful/PageStateDemo", BuildForm(
            ("__pagestate.OrderId", pageState),
            ("__RequestVerificationToken", afToken),
            ("CustomerName", "Alice"),
            ("Product", "P1")));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("updated for Alice", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Post_ValidationFailure_ReRenders_WithStillValidHiddenField()
    {
        var client = _factory.CreateClient();
        var html = await client.GetStringAsync("/Stateful/PageStateDemo");
        var pageState = ExtractFieldValue(html, "__pagestate.OrderId");
        var afToken = ExtractFieldValue(html, "__RequestVerificationToken");

        var response = await client.PostAsync("/Stateful/PageStateDemo", BuildForm(
            ("__pagestate.OrderId", pageState),
            ("__RequestVerificationToken", afToken),
            ("CustomerName", ""),
            ("Product", "P1")));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var reRenderedToken = ExtractFieldValue(body, "__pagestate.OrderId");
        Assert.False(string.IsNullOrWhiteSpace(reRenderedToken));
        // Both mechanisms together, per guide §5.3: the re-render carries a valid PageState token
        // *and* has ProductOptions populated, with zero repopulation code in the POST handler.
        // Check an unselected option — the selected one gets a reordered `selected="selected"`
        // attribute from the asp-items tag helper, which would make an exact-order match brittle.
        Assert.Contains(@"<option value=""P2"">Product 2</option>", body);

        // Prove it's valid, not just present: it must still work for a follow-up submission.
        var secondAttempt = await client.PostAsync("/Stateful/PageStateDemo", BuildForm(
            ("__pagestate.OrderId", reRenderedToken),
            ("__RequestVerificationToken", afToken),
            ("CustomerName", "Bob"),
            ("Product", "P1")));

        Assert.Equal(HttpStatusCode.OK, secondAttempt.StatusCode);
        Assert.Contains("updated for Bob", await secondAttempt.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task TwoIndependentGets_ProduceDifferentTokens_EachPostsSuccessfully()
    {
        using var clientA = _factory.CreateClient();
        using var clientB = _factory.CreateClient();

        var htmlA = await clientA.GetStringAsync("/Stateful/PageStateDemo");
        var htmlB = await clientB.GetStringAsync("/Stateful/PageStateDemo");

        var tokenA = ExtractFieldValue(htmlA, "__pagestate.OrderId");
        var tokenB = ExtractFieldValue(htmlB, "__pagestate.OrderId");
        Assert.NotEqual(tokenA, tokenB);

        var responseA = await clientA.PostAsync("/Stateful/PageStateDemo", BuildForm(
            ("__pagestate.OrderId", tokenA),
            ("__RequestVerificationToken", ExtractFieldValue(htmlA, "__RequestVerificationToken")),
            ("CustomerName", "Alice"),
            ("Product", "P1")));
        var responseB = await clientB.PostAsync("/Stateful/PageStateDemo", BuildForm(
            ("__pagestate.OrderId", tokenB),
            ("__RequestVerificationToken", ExtractFieldValue(htmlB, "__RequestVerificationToken")),
            ("CustomerName", "Carol"),
            ("Product", "P2")));

        Assert.Equal(HttpStatusCode.OK, responseA.StatusCode);
        Assert.Equal(HttpStatusCode.OK, responseB.StatusCode);
    }

    internal static string ExtractFieldValue(string html, string fieldName)
    {
        var match = Regex.Match(html, $@"name=""{Regex.Escape(fieldName)}""[^>]*value=""([^""]*)""");
        if (!match.Success)
        {
            throw new InvalidOperationException($"Field '{fieldName}' not found in response HTML.");
        }

        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    internal static FormUrlEncodedContent BuildForm(params (string Key, string Value)[] fields)
        => new(fields.Select(f => new KeyValuePair<string, string>(f.Key, f.Value)));
}
