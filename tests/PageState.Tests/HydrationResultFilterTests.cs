using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using PageState.Internal;

namespace PageState.Tests;

/// <summary>
/// §5.1 priority suite, the part that doesn't need a running host: HydrationResultFilter's own
/// dispatch logic (I1: hydrate whatever will render; I3: never for what won't).
/// </summary>
public class HydrationResultFilterTests
{
    public sealed class FilterProbeModel
    {
        [Hydrated]
        public string? Options { get; set; }
    }

    public sealed class FilterProbeHost
    {
        public int InvocationCount;

        [Hydrate]
        public Task Hydrate(FilterProbeModel vm)
        {
            InvocationCount++;
            vm.Options = "x";
            return Task.CompletedTask;
        }
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "PageState.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static ResultExecutingContext CreateContext(object controller, IActionResult result)
    {
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor());
        return new ResultExecutingContext(actionContext, new List<IFilterMetadata>(), result, controller);
    }

    private static Task<ResultExecutedContext> Next(ResultExecutingContext context)
        => Task.FromResult(new ResultExecutedContext(context, context.Filters, context.Result, context.Controller));

    private static ViewResult ViewResultWithModel(object model) => new()
    {
        ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) { Model = model }
    };

    [Fact]
    public async Task Hydrates_WhenResultIsViewResult()
    {
        var host = new FilterProbeHost();
        var model = new FilterProbeModel();
        var filter = new HydrationResultFilter(new FakeHostEnvironment(), NullLogger<HydrationResultFilter>.Instance);
        var context = CreateContext(host, ViewResultWithModel(model));

        await filter.OnResultExecutionAsync(context, () => Next(context));

        Assert.Equal(1, host.InvocationCount);
        Assert.Equal("x", model.Options);
    }

    [Fact]
    public async Task Hydrates_WhenResultIsPartialViewResult()
    {
        var host = new FilterProbeHost();
        var model = new FilterProbeModel();
        var filter = new HydrationResultFilter(new FakeHostEnvironment(), NullLogger<HydrationResultFilter>.Instance);
        var context = CreateContext(host, new PartialViewResult
        {
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) { Model = model }
        });

        await filter.OnResultExecutionAsync(context, () => Next(context));

        Assert.Equal(1, host.InvocationCount);
    }

    [Theory]
    [MemberData(nameof(NonRenderingResults))]
    public async Task DoesNotHydrate_ForResultsThatDoNotRender(IActionResult result)
    {
        var host = new FilterProbeHost();
        var filter = new HydrationResultFilter(new FakeHostEnvironment(), NullLogger<HydrationResultFilter>.Instance);
        var context = CreateContext(host, result);

        await filter.OnResultExecutionAsync(context, () => Next(context));

        Assert.Equal(0, host.InvocationCount);
    }

    public static IEnumerable<object[]> NonRenderingResults()
    {
        yield return [new RedirectResult("/somewhere")];
        yield return [new JsonResult(new { ok = true })];
        yield return [new NoContentResult()];
    }
}
