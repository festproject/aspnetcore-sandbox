using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PageState.Internal;

/// <summary>Non-generic holder so PageStateValidationFilter has one constant to check regardless of T.</summary>
internal static class PageStateModelBinder
{
    internal const string ErrorKey = "__pagestate";
}

internal sealed class PageStateModelBinder<T> : IModelBinder
{
    // MVC's ModelBinderFactory caches the binder instance across requests for a given
    // (Type, BindingInfo), so only singleton-lifetime dependencies may be constructor-injected
    // here. IPageStateAccessor is scoped — it must be resolved per-request from
    // HttpContext.RequestServices inside BindModelAsync instead, or ActivatorUtilities throws
    // "Cannot resolve scoped service from root provider" the moment the binder is created.
    private readonly IPageStateProtector _protector;
    private readonly IPageStateOwnerProvider _ownerProvider;
    private readonly PageStateOptions _options;
    private readonly ILogger<PageStateModelBinder<T>> _logger;

    public PageStateModelBinder(
        IPageStateProtector protector,
        IPageStateOwnerProvider ownerProvider,
        IOptions<PageStateOptions> options,
        ILogger<PageStateModelBinder<T>> logger)
    {
        _protector = protector;
        _ownerProvider = ownerProvider;
        _options = options.Value;
        _logger = logger;
    }

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var httpContext = bindingContext.HttpContext;

        string? token = null;
        if (httpContext.Request.HasFormContentType)
        {
            token = httpContext.Request.Form[_options.FormFieldName];
        }

        var owner = _ownerProvider.GetOwner(httpContext);
        var result = _protector.Unprotect<T>(token, owner);

        if (result.IsSuccess)
        {
            bindingContext.Result = ModelBindingResult.Success(result.State);
            httpContext.RequestServices.GetRequiredService<IPageStateAccessor>().Set(result.State);
            return Task.CompletedTask;
        }

        bindingContext.ModelState.AddModelError(PageStateModelBinder.ErrorKey, "The page state for this request was missing or invalid.");
        _logger.LogWarning(
            "PageState bind failed for {StateType} with status {Status}",
            typeof(T).Name,
            result.Status);

        return Task.CompletedTask;
    }
}
