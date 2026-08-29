using System.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace PageState.IntegrationTests;

public class DataProtectionKeyRingTests
{
    [Fact]
    public async Task SeparateEphemeralKeyRings_RejectEachOthersTokens()
    {
        using var factoryA = new CustomWebApplicationFactory();
        using var factoryB = new CustomWebApplicationFactory();
        using var clientA = factoryA.CreateClient();
        using var clientB = factoryB.CreateClient();

        var htmlA = await clientA.GetStringAsync("/Stateful/PageStateDemo");
        var tokenFromA = OrderEditPageStateDemoTests.ExtractFieldValue(htmlA, "__pagestate");

        var htmlB = await clientB.GetStringAsync("/Stateful/PageStateDemo");
        var afTokenB = OrderEditPageStateDemoTests.ExtractFieldValue(htmlB, "__RequestVerificationToken");

        var response = await clientB.PostAsync("/Stateful/PageStateDemo", OrderEditPageStateDemoTests.BuildForm(
            ("__pagestate", tokenFromA),
            ("__RequestVerificationToken", afTokenB),
            ("Input.CustomerName", "Eve"),
            ("Input.Product", "P1")));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("This page has expired", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task SharedFileSystemKeyRing_AcceptsEachOthersTokens()
    {
        var sharedDir = Directory.CreateTempSubdirectory("pagestate-keyring-");
        try
        {
            using var factoryA = new SharedKeyRingWebApplicationFactory(sharedDir.FullName);
            using var factoryB = new SharedKeyRingWebApplicationFactory(sharedDir.FullName);
            using var clientA = factoryA.CreateClient();
            using var clientB = factoryB.CreateClient();

            var htmlA = await clientA.GetStringAsync("/Stateful/PageStateDemo");
            var tokenFromA = OrderEditPageStateDemoTests.ExtractFieldValue(htmlA, "__pagestate");

            var htmlB = await clientB.GetStringAsync("/Stateful/PageStateDemo");
            var afTokenB = OrderEditPageStateDemoTests.ExtractFieldValue(htmlB, "__RequestVerificationToken");

            var response = await clientB.PostAsync("/Stateful/PageStateDemo", OrderEditPageStateDemoTests.BuildForm(
                ("__pagestate", tokenFromA),
                ("__RequestVerificationToken", afTokenB),
                ("Input.CustomerName", "Frank"),
                ("Input.Product", "P1")));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("updated for Frank", await response.Content.ReadAsStringAsync());
        }
        finally
        {
            sharedDir.Delete(recursive: true);
        }
    }

    // Deliberately does not call base.ConfigureWebHost (CustomWebApplicationFactory's per-factory
    // ephemeral provider) — this variant needs a persisted key ring shared across two instances.
    private sealed class SharedKeyRingWebApplicationFactory(string keyRingDirectory) : CustomWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.AddDataProtection()
                    .PersistKeysToFileSystem(new DirectoryInfo(keyRingDirectory))
                    .SetApplicationName("PageStateKeyRingTest");
            });
        }
    }
}
