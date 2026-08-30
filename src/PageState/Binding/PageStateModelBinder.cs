using Microsoft.AspNetCore.Mvc.ModelBinding;
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
    private readonly IPageStateProtector _protector;
    private readonly IPageStateOwnerProvider _ownerProvider;
    private readonly PageStateOptions _options;
    private readonly PageStateSite _site;
    private readonly ILogger<PageStateModelBinder<T>> _logger;

    public PageStateModelBinder(
        IPageStateProtector protector,
        IPageStateOwnerProvider ownerProvider,
        IOptions<PageStateOptions> options,
        PageStateSite site,
        ILogger<PageStateModelBinder<T>> logger)
    {
        _protector = protector;
        _ownerProvider = ownerProvider;
        _options = options.Value;
        _site = site;
        _logger = logger;
    }

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var httpContext = bindingContext.HttpContext;

        string? token = null;
        if (httpContext.Request.HasFormContentType)
        {
            token = httpContext.Request.Form[PageStateRendering.FieldNameFor(_options, _site.PropertyName)];
        }

        var owner = _ownerProvider.GetOwner(httpContext);
        var result = _protector.Unprotect<T>(token, owner, _site);

        if (result.IsSuccess)
        {
            bindingContext.Result = ModelBindingResult.Success(result.State);
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
