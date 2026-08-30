using Microsoft.Extensions.Logging;
using PageState.Internal;

namespace PageState.Tests;

/// <summary>§3.6: the three Development-only diagnostics, matched against their specified wording.</summary>
public class HydrationDiagnosticsTests
{
    public sealed class NoHydratorModel
    {
        [Hydrated]
        public string? CategoryOptions { get; set; }
    }

    public sealed class NoHydratorHost { }

    public sealed class IncompleteModel
    {
        [Hydrated]
        public string? CategoryOptions { get; set; }

        [Hydrated]
        public string? CanDelete { get; set; }
    }

    public sealed class IncompleteHost
    {
        [Hydrate]
        public Task HydrateEdit(IncompleteModel vm)
        {
            vm.CategoryOptions = "set";
            // CanDelete deliberately left null.
            return Task.CompletedTask;
        }
    }

    public sealed class NonNullableValueTypeModel
    {
        [Hydrated]
        public bool CanDelete { get; set; }
    }

    public sealed class NonNullableValueTypeHost
    {
        [Hydrate]
        public Task Hydrate(NonNullableValueTypeModel vm) => Task.CompletedTask;
    }

    public sealed class UnclassifiedModel
    {
        [Hydrated]
        public string? CategoryOptions { get; set; }

        public string? Unattributed { get; set; }
    }

    public sealed class UnclassifiedHost
    {
        [Hydrate]
        public Task Hydrate(UnclassifiedModel vm)
        {
            vm.CategoryOptions = "set";
            return Task.CompletedTask;
        }
    }

    [Fact]
    public void VerifyHydratorExists_Throws_NamingModelHostAndProperty_WhenNoHydratorMatches()
    {
        var plan = HydrationPlan.For(typeof(NoHydratorHost), typeof(NoHydratorModel));

        var ex = Assert.Throws<InvalidOperationException>(() => HydrationDiagnostics.VerifyHydratorExists(plan));

        Assert.Contains("NoHydratorModel.CategoryOptions is declared [Hydrated]", ex.Message);
        Assert.Contains("NoHydratorHost has no", ex.Message);
        Assert.Contains("[Hydrate] method for NoHydratorModel", ex.Message);
    }

    [Fact]
    public void VerifyHydratorExists_Throws_ForNonNullableValueTypeHydratedProperty()
    {
        var plan = HydrationPlan.For(typeof(NonNullableValueTypeHost), typeof(NonNullableValueTypeModel));

        var ex = Assert.Throws<InvalidOperationException>(() => HydrationDiagnostics.VerifyHydratorExists(plan));

        Assert.Contains("NonNullableValueTypeModel.CanDelete is [Hydrated] but is a non-nullable value type", ex.Message);
        Assert.Contains("Declare it as bool?.", ex.Message);
    }

    [Fact]
    public void VerifyAllHydrated_Throws_ForThePropertyLeftNull()
    {
        var host = new IncompleteHost();
        var model = new IncompleteModel();
        var plan = HydrationPlan.For(host.GetType(), model.GetType());
        model.CategoryOptions = "set"; // simulate the hydrator having already run

        var ex = Assert.Throws<InvalidOperationException>(() => HydrationDiagnostics.VerifyAllHydrated(plan, model));

        Assert.Contains("IncompleteModel.CanDelete is declared [Hydrated] but is still null", ex.Message);
        Assert.Contains("IncompleteHost.HydrateEdit ran", ex.Message);
    }

    [Fact]
    public void VerifyAllHydrated_DoesNotThrow_WhenEveryHydratedPropertyIsSet()
    {
        var model = new IncompleteModel { CategoryOptions = "a", CanDelete = "b" };
        var plan = HydrationPlan.For(typeof(IncompleteHost), typeof(IncompleteModel));

        var ex = Record.Exception(() => HydrationDiagnostics.VerifyAllHydrated(plan, model));

        Assert.Null(ex);
    }

    [Fact]
    public void WarnUnclassified_LogsWarning_ForNullUnattributedReferenceProperty()
    {
        var model = new UnclassifiedModel { CategoryOptions = "set", Unattributed = null };
        var plan = HydrationPlan.For(typeof(UnclassifiedHost), typeof(UnclassifiedModel));
        var logger = new CapturingLogger();

        HydrationDiagnostics.WarnUnclassified(plan, model, logger);

        Assert.Single(logger.Warnings);
        Assert.Contains("UnclassifiedModel.Unattributed is null at render time and has no", logger.Warnings[0]);
    }

    [Fact]
    public void WarnUnclassified_DoesNotLog_WhenUnattributedPropertyIsSet()
    {
        var model = new UnclassifiedModel { CategoryOptions = "set", Unattributed = "present" };
        var plan = HydrationPlan.For(typeof(UnclassifiedHost), typeof(UnclassifiedModel));
        var logger = new CapturingLogger();

        HydrationDiagnostics.WarnUnclassified(plan, model, logger);

        Assert.Empty(logger.Warnings);
    }

    private sealed class CapturingLogger : ILogger
    {
        public List<string> Warnings { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                Warnings.Add(formatter(state, exception));
            }
        }

        private static readonly IDisposable NullScope = new NoopDisposable();

        private sealed class NoopDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}
