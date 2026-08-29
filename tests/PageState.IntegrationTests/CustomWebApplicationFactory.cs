using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace PageState.IntegrationTests;

/// <summary>
/// Overrides Data Protection to be hermetic — isolated from the developer machine's real key
/// ring and from other test runs — by default. ConfigureWebHost callbacks run after Program.cs's
/// own ConfigureServices, so this correctly replaces the app's real AddDataProtection() call.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
        });
    }
}
