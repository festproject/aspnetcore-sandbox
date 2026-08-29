using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace PageState.IntegrationTests.TestSupport;

public sealed class ProbeWebApplicationFactory : CustomWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            services.AddControllersWithViews()
                .ConfigureApplicationPartManager(apm =>
                    apm.ApplicationParts.Add(new AssemblyPart(typeof(ProbeController).Assembly)));
        });
    }
}
