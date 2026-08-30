namespace PageState.Tests;

public sealed record RoundTripState(string Name, int[] Numbers, NestedInfo? Nested, string? Optional);

public sealed record NestedInfo(string Value, int When);

public class BaseState
{
    public string Value { get; init; } = "";
}

public sealed class DerivedState : BaseState
{
    public string Extra { get; init; } = "";
}

public sealed record TypeAState(string Name);

public sealed record TypeBState(string Name);

public sealed record WrongShapeState(int RequiredNumber);

public sealed record SizeTestState(string Filler);
