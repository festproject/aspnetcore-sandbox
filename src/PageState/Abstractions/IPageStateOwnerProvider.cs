using Microsoft.AspNetCore.Http;

namespace PageState;

public interface IPageStateOwnerProvider
{
    string? GetOwner(HttpContext context);
}
