using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PageState.Internal;

namespace PageState;

// Escape hatch for multiple forms on one page: the <page-state /> tag helper covers the single
// [PageState] property on the page's own model, this covers additional independent states —
// @Html.PageState(saveState) / @Html.PageState(deleteState).
public static class PageStateHtmlHelperExtensions
{
    public static IHtmlContent PageState<T>(this IHtmlHelper html, T state) where T : notnull
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
