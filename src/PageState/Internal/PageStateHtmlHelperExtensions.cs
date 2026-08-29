using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace PageState.Internal;

// Must be public: this is the app-facing escape hatch for multiple forms on one page
// (@Html.PageState(saveState) / @Html.PageState(deleteState)), overriding the single-form
// convenience the <page-state /> tag helper + IPageStateAccessor provide.
public static class PageStateHtmlHelperExtensions
{
    public static IHtmlContent PageState<T>(this IHtmlHelper html, T state)
    {
        var httpContext = html.ViewContext.HttpContext;
        var services = httpContext.RequestServices;

        var protector = services.GetRequiredService<IPageStateProtector>();
        var ownerProvider = services.GetRequiredService<IPageStateOwnerProvider>();
        var options = services.GetRequiredService<IOptions<PageStateOptions>>().Value;

        var owner = ownerProvider.GetOwner(httpContext);
        var token = protector.Protect(state, owner);

        return PageStateRendering.BuildHiddenInputHtml(options.FormFieldName, token);
    }
}
