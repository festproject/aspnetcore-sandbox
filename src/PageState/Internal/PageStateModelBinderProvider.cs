using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace PageState.Internal;

internal sealed class PageStateModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context.BindingInfo.BindingSource != FromPageStateAttribute.Source)
        {
            return null;
        }

        var binderType = typeof(PageStateModelBinder<>).MakeGenericType(context.Metadata.ModelType);
        return (IModelBinder)ActivatorUtilities.CreateInstance(context.Services, binderType);
    }
}
