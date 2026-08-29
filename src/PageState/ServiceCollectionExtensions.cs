using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PageState.Internal;

namespace PageState;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers PageState. To override the default <see cref="IPageStateFailureHandler"/>,
    /// register your own implementation before calling this method — the default is added with
    /// TryAddSingleton, which only takes effect when nothing is registered yet.
    /// </summary>
    public static IServiceCollection AddPageState(this IServiceCollection services, Action<PageStateOptions>? configure = null)
    {
        var optionsBuilder = services.AddOptions<PageStateOptions>();
        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<PageStateRegistry>();
        services.TryAddSingleton<IPageStateProtector, PageStateProtector>();
        services.TryAddSingleton<IPageStateOwnerProvider, ClaimsPageStateOwnerProvider>();
        services.TryAddScoped<IPageStateAccessor, PageStateAccessor>();
        services.TryAddSingleton<IPageStateFailureHandler, DefaultPageStateFailureHandler>();

        services.Configure<MvcOptions>(o => o.ModelBinderProviders.Insert(0, new PageStateModelBinderProvider()));
        services.Configure<MvcOptions>(o => o.Filters.Add<PageStateValidationFilter>());

        return services;
    }
}
