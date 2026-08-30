using Microsoft.AspNetCore.Http;

namespace PageState.IntegrationTests.TestSupport;

/// <summary>Test-only owner provider whose return value can change mid-test, to simulate a re-login between GET and POST.</summary>
public sealed class SwitchableOwnerProvider : IPageStateOwnerProvider
{
    public string? CurrentOwner { get; set; }

    public string? GetOwner(HttpContext context) => CurrentOwner;
}
