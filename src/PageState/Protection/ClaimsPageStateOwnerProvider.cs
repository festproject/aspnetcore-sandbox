using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace PageState;

/// <summary>
/// Default <see cref="IPageStateOwnerProvider"/>: reads a configurable claim type (default
/// "psid") and returns null when absent or unauthenticated. Public and unsealed so applications
/// can subclass it — e.g. to combine the claim with a tenant id — or reference it directly when
/// wiring a custom claim type.
/// </summary>
public class ClaimsPageStateOwnerProvider : IPageStateOwnerProvider
{
    private readonly PageStateOptions _options;

    public ClaimsPageStateOwnerProvider(IOptions<PageStateOptions> options)
    {
        _options = options.Value;
    }

    public virtual string? GetOwner(HttpContext context)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        return context.User.FindFirst(_options.OwnerClaimType)?.Value;
    }
}
