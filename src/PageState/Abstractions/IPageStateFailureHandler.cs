using Microsoft.AspNetCore.Mvc;

namespace PageState;

/// <summary>
/// Builds the result returned when a PageState token fails to bind. Register a custom
/// implementation before calling AddPageState() to override the default (a "PageStateExpired"
/// view, falling back to a plain 400 when that view is not defined by the host app).
/// </summary>
public interface IPageStateFailureHandler
{
    IActionResult HandleFailure(ActionContext context);
}
