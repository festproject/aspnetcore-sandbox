using PageState;

namespace PageState.IntegrationTests.TestSupport;

[PageState("IntegrationTestProbe")]
public sealed record ProbeState(int Value);
