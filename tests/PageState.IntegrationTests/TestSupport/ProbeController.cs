using Microsoft.AspNetCore.Mvc;

namespace PageState.IntegrationTests.TestSupport;

// Test-only controller, added to the host via ApplicationPart by ProbeWebApplicationFactory so it
// never ships in the real app. Deliberately has no [ValidateAntiForgeryToken]: it exists purely to
// prove the PageState short-circuit path in isolation from antiforgery (that ordering is covered
// separately by AntiforgeryOrderingTests against the real demo action).
[Route("test-support/probe")]
public sealed class ProbeController : Controller
{
    public static int InvocationCount;

    [HttpGet]
    public IActionResult Get() => Content("probe-get-ok");

    [HttpPost]
    public IActionResult Post(ProbeViewModel vm)
    {
        Interlocked.Increment(ref InvocationCount);
        return Content("probe-post-ok");
    }
}
