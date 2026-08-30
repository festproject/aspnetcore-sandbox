namespace PageState.IntegrationTests.TestSupport;

public sealed record ProbeState(int Value);

public sealed class ProbeViewModel
{
    [PageState]
    public ProbeState? State { get; set; }
}
