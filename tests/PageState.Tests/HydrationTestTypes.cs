namespace PageState.Tests;

public sealed class ModelX
{
    [Hydrated]
    public string? Options { get; set; }

    public string? Unclassified { get; set; }

    public int PlainValue { get; set; }
}

public sealed class ModelY
{
    [Hydrated]
    public string? Other { get; set; }
}

public sealed class ModelNoHydration
{
    public string? PlainField { get; set; }
}

public sealed class ModelWithPageStateOnly
{
    [PageState]
    public TypeAState? State { get; set; }
}

public class PlanHostBase
{
    [Hydrate]
    protected Task HydrateBase(ModelX vm) => Task.CompletedTask;
}

public sealed class PlanHostDerived : PlanHostBase
{
    [Hydrate]
    private Task HydrateDerived(ModelX vm) => Task.CompletedTask;

    [Hydrate]
    private Task HydrateY(ModelY vm) => Task.CompletedTask;
}

public interface ITestService
{
    string Say();
}

public sealed class TestService : ITestService
{
    public string Say() => "hello";
}

public sealed class InvokerModel
{
    public string? Value { get; set; }
}

public sealed class InvokerHost
{
    public bool TaskMethodRan;
    public string? ResolvedServiceMessage;

    [Hydrate]
    public Task HydrateAsync(InvokerModel vm, ITestService svc)
    {
        TaskMethodRan = true;
        ResolvedServiceMessage = svc.Say();
        vm.Value = "set";
        return Task.CompletedTask;
    }
}

public sealed class InvokerHostValueTask
{
    public bool Ran;

    [Hydrate]
    public ValueTask HydrateValueTaskAsync(InvokerModel vm)
    {
        Ran = true;
        return ValueTask.CompletedTask;
    }
}

public sealed class ThrowingHost
{
    [Hydrate]
    public Task HydrateAsync(InvokerModel vm) => throw new InvalidOperationException("boom");
}
