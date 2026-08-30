using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace PageState.Internal;

internal sealed class HydrationInvoker
{
    public static async Task InvokeAsync(
        HydrationPlan plan, object host, object model, HttpContext http)
    {
        foreach (var m in plan.Methods)
        {
            var ps = m.GetParameters();
            var args = new object[ps.Length];

            for (var i = 0; i < ps.Length; i++)
                args[i] = ps[i].ParameterType.IsInstanceOfType(model)
                    ? model
                    : http.RequestServices.GetRequiredService(ps[i].ParameterType);

            try
            {
                var result = m.Invoke(host, args);
                if (result is Task t) await t;
                else if (result is ValueTask vt) await vt;
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
        }
    }
}
