using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace PageState.IntegrationTests.TestSupport;

public sealed class OwnerChangeWebApplicationFactory : CustomWebApplicationFactory
{
    public SwitchableOwnerProvider OwnerProvider { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            services.Replace(ServiceDescriptor.Singleton<IPageStateOwnerProvider>(OwnerProvider));
        });
    }
}
