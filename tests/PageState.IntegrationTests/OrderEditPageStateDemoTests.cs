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

        Assert.Matches(@"<input[^>]*name=""__pagestate""[^>]*autocomplete=""off""[^>]*>", html);
    }

    [Fact]
    public async Task GetThenPost_ValidInput_Returns200_WithSuccessMessage()
    {
        var client = _factory.CreateClient();
        var html = await client.GetStringAsync("/Stateful/PageStateDemo");
        var pageState = ExtractFieldValue(html, "__pagestate");
        var afToken = ExtractFieldValue(html, "__RequestVerificationToken");

        var response = await client.PostAsync("/Stateful/PageStateDemo", BuildForm(
            ("__pagestate", pageState),
            ("__RequestVerificationToken", afToken),
            ("Input.CustomerName", "Alice"),
            ("Input.Product", "P1")));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("updated for Alice", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Post_ValidationFailure_ReRenders_WithStillValidHiddenField()
    {
        var client = _factory.CreateClient();
        var html = await client.GetStringAsync("/Stateful/PageStateDemo");
        var pageState = ExtractFieldValue(html, "__pagestate");
        var afToken = ExtractFieldValue(html, "__RequestVerificationToken");

        var response = await client.PostAsync("/Stateful/PageStateDemo", BuildForm(
            ("__pagestate", pageState),
            ("__RequestVerificationToken", afToken),
            ("Input.CustomerName", ""),
            ("Input.Product", "P1")));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var reRenderedToken = ExtractFieldValue(body, "__pagestate");
        Assert.False(string.IsNullOrWhiteSpace(reRenderedToken));

        // Prove it's valid, not just present: it must still work for a follow-up submission.
        var secondAttempt = await client.PostAsync("/Stateful/PageStateDemo", BuildForm(
            ("__pagestate", reRenderedToken),
            ("__RequestVerificationToken", afToken),
            ("Input.CustomerName", "Bob"),
            ("Input.Product", "P1")));

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

        var tokenA = ExtractFieldValue(htmlA, "__pagestate");
        var tokenB = ExtractFieldValue(htmlB, "__pagestate");
        Assert.NotEqual(tokenA, tokenB);

        var responseA = await clientA.PostAsync("/Stateful/PageStateDemo", BuildForm(
            ("__pagestate", tokenA),
            ("__RequestVerificationToken", ExtractFieldValue(htmlA, "__RequestVerificationToken")),
            ("Input.CustomerName", "Alice"),
            ("Input.Product", "P1")));
        var responseB = await clientB.PostAsync("/Stateful/PageStateDemo", BuildForm(
            ("__pagestate", tokenB),
            ("__RequestVerificationToken", ExtractFieldValue(htmlB, "__RequestVerificationToken")),
            ("Input.CustomerName", "Carol"),
            ("Input.Product", "P2")));

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
