using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PageState;
using PageState.Internal;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers PageState and Hydration. To override the default <see cref="IPageStateFailureHandler"/>
    /// or <see cref="IPageStateOwnerProvider"/>, register your own implementation before calling this
    /// method — the defaults are added with TryAddSingleton, which only takes effect when nothing is
    /// registered yet.
    /// </summary>
    public static IServiceCollection AddPageState(this IServiceCollection services, Action<PageStateOptions>? configure = null)
    {
        var optionsBuilder = services.AddOptions<PageStateOptions>();
        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IPageStateProtector, PageStateProtector>();
        services.TryAddSingleton<IPageStateOwnerProvider, ClaimsPageStateOwnerProvider>();
        services.TryAddSingleton<IPageStateFailureHandler, DefaultPageStateFailureHandler>();

        services.Configure<MvcOptions>(o => o.ModelBinderProviders.Insert(0, new PageStateModelBinderProvider()));
        services.Configure<MvcOptions>(o => o.Filters.Add<PageStateValidationFilter>());
        services.Configure<MvcOptions>(o => o.Filters.Add<HydrationResultFilter>());

        return services;
    }
}
