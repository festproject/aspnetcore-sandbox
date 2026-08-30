using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using PageState.Internal;

namespace PageState.Tests;

/// <summary>Phase 1 checkpoint: DI parameter resolution, Task/ValueTask awaiting, exception unwrapping.</summary>
public class HydrationInvokerTests
{
    private static HttpContext CreateHttpContext(IServiceProvider services)
        => new DefaultHttpContext { RequestServices = services };

    [Fact]
    public async Task InvokeAsync_ResolvesNonTargetParameter_FromRequestServices()
    {
        var services = new ServiceCollection().AddSingleton<ITestService, TestService>().BuildServiceProvider();
        var host = new InvokerHost();
        var model = new InvokerModel();
        var plan = HydrationPlan.For(host.GetType(), model.GetType());

        await HydrationInvoker.InvokeAsync(plan, host, model, CreateHttpContext(services));

        Assert.True(host.TaskMethodRan);
        Assert.Equal("hello", host.ResolvedServiceMessage);
        Assert.Equal("set", model.Value);
    }

    [Fact]
    public async Task InvokeAsync_AwaitsValueTaskHydrators()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var host = new InvokerHostValueTask();
        var model = new InvokerModel();
        var plan = HydrationPlan.For(host.GetType(), model.GetType());

        await HydrationInvoker.InvokeAsync(plan, host, model, CreateHttpContext(services));

        Assert.True(host.Ran);
    }

    [Fact]
    public async Task InvokeAsync_SurfacesInnerException_NotTargetInvocationException()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var host = new ThrowingHost();
        var model = new InvokerModel();
        var plan = HydrationPlan.For(host.GetType(), model.GetType());

        var ex = await Record.ExceptionAsync(() => HydrationInvoker.InvokeAsync(plan, host, model, CreateHttpContext(services)));

        Assert.IsType<InvalidOperationException>(ex);
        Assert.Equal("boom", ex!.Message);
    }
}
