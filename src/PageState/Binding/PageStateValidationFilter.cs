using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace PageState.Internal;

/// <summary>
/// Registered globally by AddPageState() so it cannot be omitted. Without this filter, a binder
/// that only adds a model error leaves the [PageState] property null and lets the action run,
/// which turns a tampered token into a NullReferenceException instead of a controlled short-circuit.
/// </summary>
internal sealed class PageStateValidationFilter : IActionFilter, IPageFilter
{
    private readonly IPageStateFailureHandler _failureHandler;

    public PageStateValidationFilter(IPageStateFailureHandler failureHandler)
    {
        _failureHandler = failureHandler;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (HasPageStateError(context.ModelState))
        {
            context.Result = _failureHandler.HandleFailure(context);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    public void OnPageHandlerSelected(PageHandlerSelectedContext context)
    {
    }

    public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        if (HasPageStateError(context.ModelState))
        {
            context.Result = _failureHandler.HandleFailure(context);
        }
    }

    public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
    {
    }

    private static bool HasPageStateError(ModelStateDictionary modelState)
        => modelState.TryGetValue(PageStateModelBinder.ErrorKey, out var entry) && entry.Errors.Count > 0;
}

internal sealed class DefaultPageStateFailureHandler : IPageStateFailureHandler
{
    private const string ViewName = "PageStateExpired";

    private readonly ICompositeViewEngine _viewEngine;

    public DefaultPageStateFailureHandler(ICompositeViewEngine viewEngine)
    {
        _viewEngine = viewEngine;
    }

    public IActionResult HandleFailure(ActionContext context)
    {
        var viewResult = _viewEngine.FindView(context, ViewName, isMainPage: true);
        if (viewResult.Success)
        {
            return new ViewResult
            {
                ViewName = ViewName,
                StatusCode = StatusCodes.Status400BadRequest
            };
        }

        return new ContentResult
        {
            StatusCode = StatusCodes.Status400BadRequest,
            Content = "The page state for this request was missing or invalid.",
            ContentType = "text/plain"
        };
    }
}
