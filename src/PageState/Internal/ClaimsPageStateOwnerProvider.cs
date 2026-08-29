using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace PageState.Internal;

internal sealed class ClaimsPageStateOwnerProvider : IPageStateOwnerProvider
{
    private readonly PageStateOptions _options;

    public ClaimsPageStateOwnerProvider(IOptions<PageStateOptions> options)
    {
        _options = options.Value;
    }

    public string? GetOwner(HttpContext context)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        return context.User.FindFirst(_options.OwnerClaimType)?.Value;
    }
}
