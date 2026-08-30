using Microsoft.Extensions.Logging;

namespace PageState.Internal;

/// <summary>
/// The three Development-only runtime checks from guide §3.6, plus the plan-build-time value-type
/// rule folded into the first check (it fires at the same moment — before the first hydration
/// invocation for a given host/model pair — so a separate hook off HydrationPlan.Build isn't needed).
/// All of these read as lifecycle-contract violations, not null checks: the wording is the primary
/// interface of the library, so match it closely rather than paraphrasing.
/// </summary>
internal static class HydrationDiagnostics
{
    /// <summary>Before invocation: a non-nullable [Hydrated] value type, or no [Hydrate] method at all.</summary>
    public static void VerifyHydratorExists(HydrationPlan plan)
    {
        foreach (var prop in plan.HydratedProps)
        {
            if (prop.PropertyType.IsValueType && Nullable.GetUnderlyingType(prop.PropertyType) is null)
            {
                throw new InvalidOperationException(
                    $"{plan.ModelType.Name}.{prop.Name} is [Hydrated] but is a non-nullable value type, " +
                    $"so a missing assignment cannot be detected. Declare it as {FriendlyTypeName(prop.PropertyType)}?.");
            }
        }

        if (plan.HydratedProps.Length > 0 && plan.Methods.Length == 0)
        {
            var prop = plan.HydratedProps[0];
            throw new InvalidOperationException(
                $"{plan.ModelType.Name}.{prop.Name} is declared [Hydrated] — it must be\n" +
                $"rebuilt on the server before every render — but {plan.HostType.Name} has no\n" +
                $"[Hydrate] method for {plan.ModelType.Name}.\n\n" +
                "Add:\n" +
                "    [Hydrate]\n" +
                $"    private async Task Hydrate({plan.ModelType.Name} vm)\n" +
                $"        => vm.{prop.Name} = await Get{prop.Name}();");
        }
    }

    /// <summary>After invocation: every [Hydrated] property must be non-null.</summary>
    public static void VerifyAllHydrated(HydrationPlan plan, object model)
    {
        foreach (var prop in plan.HydratedProps)
        {
            if (prop.GetValue(model) is null)
            {
                var methodNames = string.Join(", ", plan.Methods.Select(m => m.Name));
                throw new InvalidOperationException(
                    $"{plan.ModelType.Name}.{prop.Name} is declared [Hydrated] but is still null after " +
                    $"{plan.HostType.Name}.{methodNames} ran. Every [Hydrated] property must be assigned " +
                    "on every render.");
            }
        }
    }

    /// <summary>After invocation: a reference property with no attribute at all that is still null. Warns, never throws — a legitimately null `string? Comment` is a false positive by design.</summary>
    public static void WarnUnclassified(HydrationPlan plan, object model, ILogger log)
    {
        foreach (var prop in plan.UnclassifiedRef)
        {
            if (prop.GetValue(model) is null)
            {
                log.LogWarning(
                    "{ModelType}.{Property} is null at render time and has no attribute. If the " +
                    "server rebuilds it, declare it [Hydrated]; if it comes from the form, ignore this warning.",
                    plan.ModelType.Name, prop.Name);
            }
        }
    }

    private static string FriendlyTypeName(Type type) => type switch
    {
        _ when type == typeof(bool) => "bool",
        _ when type == typeof(int) => "int",
        _ when type == typeof(long) => "long",
        _ when type == typeof(short) => "short",
        _ when type == typeof(byte) => "byte",
        _ when type == typeof(double) => "double",
        _ when type == typeof(float) => "float",
        _ when type == typeof(decimal) => "decimal",
        _ when type == typeof(char) => "char",
        _ => type.Name
    };
}
