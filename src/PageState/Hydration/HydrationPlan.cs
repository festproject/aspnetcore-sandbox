using System.Collections.Concurrent;
using System.Reflection;

namespace PageState.Internal;

internal sealed class HydrationPlan
{
    private static readonly ConcurrentDictionary<(Type Host, Type Model), HydrationPlan> Cache = new();

    private HydrationPlan(Type hostType, Type modelType, MethodInfo[] methods, PropertyInfo[] hydratedProps, PropertyInfo[] unclassifiedRef)
    {
        HostType = hostType;
        ModelType = modelType;
        Methods = methods;
        HydratedProps = hydratedProps;
        UnclassifiedRef = unclassifiedRef;
    }

    public Type HostType { get; }
    public Type ModelType { get; }
    public MethodInfo[] Methods { get; }   // matched, in run order
    public PropertyInfo[] HydratedProps { get; }
    public PropertyInfo[] UnclassifiedRef { get; }   // no attribute, reference type — §3.6

    public bool IsNoOp => HydratedProps.Length == 0 && Methods.Length == 0;

    public static HydrationPlan For(Type hostType, Type modelType) =>
        Cache.GetOrAdd((hostType, modelType), static k => Build(k.Host, k.Model));

    private static HydrationPlan Build(Type hostType, Type modelType)
    {
        var methods = FindHydrateMethods(hostType, modelType);

        var properties = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var hydratedProps = properties
            .Where(p => p.IsDefined(typeof(HydratedAttribute), inherit: true))
            .ToArray();
        var unclassifiedRef = properties
            .Where(p => !p.PropertyType.IsValueType
                && !p.IsDefined(typeof(HydratedAttribute), inherit: true)
                && !p.IsDefined(typeof(PageStateAttribute), inherit: true))
            .ToArray();

        return new HydrationPlan(hostType, modelType, methods, hydratedProps, unclassifiedRef);
    }

    // Base class first, then declaration order within a type — gives shared-chrome hydrators on a
    // base controller a chance to run before the derived controller's own hydrator (§3.4).
    private static MethodInfo[] FindHydrateMethods(Type hostType, Type modelType)
    {
        var typeChain = new List<Type>();
        for (var t = hostType; t is not null && t != typeof(object); t = t.BaseType)
        {
            typeChain.Add(t);
        }
        typeChain.Reverse();

        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        return typeChain
            .SelectMany(t => t.GetMethods(flags))
            .Where(m => m.IsDefined(typeof(HydrateAttribute), inherit: false))
            .Where(m => MatchesModel(m, modelType))
            .ToArray();
    }

    // A method with no parameters always runs (Razor Pages: mutates `this`). A method with
    // parameters runs when at least one parameter's type could receive the model instance — that
    // parameter gets the model, the rest resolve from DI (HydrationInvoker does that per-parameter
    // dispatch). A method whose parameters are all unrelated to modelType is skipped for this render.
    private static bool MatchesModel(MethodInfo method, Type modelType)
    {
        var parameters = method.GetParameters();
        return parameters.Length == 0 || parameters.Any(p => p.ParameterType.IsAssignableFrom(modelType));
    }
}
