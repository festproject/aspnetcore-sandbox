using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Extensions.Options;

namespace PageState.Internal;

internal sealed record PageStateDescriptor(
    Type StateType,
    string Workflow,
    int SchemaVersion,
    TimeSpan Lifetime);

internal sealed class PageStateRegistry
{
    private readonly ConcurrentDictionary<Type, PageStateDescriptor> _cache = new();
    private readonly PageStateOptions _options;
    private readonly Lazy<ImmutableArray<PageStateDescriptor>> _all;

    public PageStateRegistry(IOptions<PageStateOptions> options)
    {
        _options = options.Value;
        _all = new Lazy<ImmutableArray<PageStateDescriptor>>(ScanAll, isThreadSafe: true);
    }

    public PageStateDescriptor Get<T>() => Get(typeof(T));

    public PageStateDescriptor Get(Type stateType)
    {
        return _cache.GetOrAdd(stateType, static (type, options) => Build(type, options), _options);
    }

    public IEnumerable<PageStateDescriptor> All => _all.Value;

    private static PageStateDescriptor Build(Type stateType, PageStateOptions options)
    {
        var attribute = stateType.GetCustomAttribute<PageStateAttribute>()
            ?? throw new InvalidOperationException(
                $"Type '{stateType.FullName}' is used as a PageState state type but has no " +
                $"[PageState(\"workflow-name\")] attribute. Add one to declare its workflow identity.");

        var lifetime = attribute.LifetimeMinutes is { } minutes
            ? TimeSpan.FromMinutes(minutes)
            : options.DefaultLifetime;

        return new PageStateDescriptor(stateType, attribute.Workflow, attribute.SchemaVersion, lifetime);
    }

    private ImmutableArray<PageStateDescriptor> ScanAll()
    {
        var assemblies = new List<Assembly>(_options.ScanAssemblies);
        if (Assembly.GetEntryAssembly() is { } entryAssembly && !assemblies.Contains(entryAssembly))
        {
            assemblies.Add(entryAssembly);
        }

        var builder = ImmutableArray.CreateBuilder<PageStateDescriptor>();

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.GetCustomAttribute<PageStateAttribute>() is null)
                {
                    continue;
                }

                var descriptor = _cache.GetOrAdd(type, static (t, o) => Build(t, o), _options);
                builder.Add(descriptor);
            }
        }

        return builder.ToImmutable();
    }
}
