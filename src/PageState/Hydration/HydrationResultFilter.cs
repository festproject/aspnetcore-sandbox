using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PageState.Internal;

internal sealed class HydrationResultFilter : IAsyncResultFilter, IOrderedFilter
{
    private readonly IHostEnvironment _env;
    private readonly ILogger<HydrationResultFilter> _log;

    public HydrationResultFilter(IHostEnvironment env, ILogger<HydrationResultFilter> log)
    {
        _env = env;
        _log = log;
    }
    public int Order => int.MinValue;

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        var host = context.Controller;
        var model = context.Result switch
        {
            ViewResult v => v.Model,
            PartialViewResult p => p.Model,
            PageResult => host,      // Razor Pages: PageModel is its own model
            _ => null       // redirect, json, file, status — no-op
        };

        if (host is not null && model is not null)
        {
            var plan = HydrationPlan.For(host.GetType(), model.GetType());

            if (!plan.IsNoOp)
            {
                if (_env.IsDevelopment()) HydrationDiagnostics.VerifyHydratorExists(plan);
                await HydrationInvoker.InvokeAsync(plan, host, model, context.HttpContext);
                if (_env.IsDevelopment()) HydrationDiagnostics.VerifyAllHydrated(plan, model);
            }

            if (_env.IsDevelopment())
                HydrationDiagnostics.WarnUnclassified(plan, model, _log);
        }

        await next();
    }
}
