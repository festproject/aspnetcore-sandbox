using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PageState.Internal;

namespace PageState.IntegrationTests.TestSupport;

public sealed class SpyProtectorWebApplicationFactory : CustomWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            services.Replace(ServiceDescriptor.Singleton<IPageStateProtector>(sp =>
                new SpyPageStateProtector(ActivatorUtilities.CreateInstance<PageStateProtector>(sp))));
        });
    }
}
