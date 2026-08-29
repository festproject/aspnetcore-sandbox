namespace PageState.Tests;

[PageState("RoundTrip", SchemaVersion = 1)]
public sealed record RoundTripState(string Name, int[] Numbers, NestedInfo? Nested, string? Optional);

public sealed record NestedInfo(string Value, int When);

[PageState("WorkflowA", SchemaVersion = 1)]
public sealed record WorkflowAStateV1(string Name);

// Same workflow as WorkflowAStateV1 but a different schema version — used for the
// "schema 1 at mint, type declares 2 at read" test. Deliberately legal in this scope: the
// startup registry validator that would reject duplicate-workflow-name registrations is
// Phase 6, out of scope here.
[PageState("WorkflowA", SchemaVersion = 2)]
public sealed record WorkflowAStateV2(string Name);

[PageState("WorkflowB", SchemaVersion = 1)]
public sealed record WorkflowBState(string Name);

[PageState("WrongShape", SchemaVersion = 1)]
public sealed record WrongShapeState(int RequiredNumber);

[PageState("SizeTest", SchemaVersion = 1)]
public sealed record SizeTestState(string Filler);
