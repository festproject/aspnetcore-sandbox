using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace PageState.Internal;

internal sealed class PageStateModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context.BindingInfo.BindingSource != PageStateAttribute.Source)
        {
            return null;
        }

        // ModelBinderFactory caches the resulting binder per property-specific ModelMetadata, so
        // baking this property's site into the constructor here is safe — one binder instance per
        // distinct [PageState] property, not shared across properties or requests.
        var site = new PageStateSite(context.Metadata.ContainerType!, context.Metadata.PropertyName!);
        var binderType = typeof(PageStateModelBinder<>).MakeGenericType(context.Metadata.ModelType);
        return (IModelBinder)ActivatorUtilities.CreateInstance(context.Services, binderType, site);
    }
}
