using PageState;

namespace PageState.IntegrationTests.TestSupport;

public sealed class SpyPageStateProtector : IPageStateProtector
{
    private readonly IPageStateProtector _inner;

    public SpyPageStateProtector(IPageStateProtector inner) => _inner = inner;

    public int ProtectCallCount { get; private set; }
    public int UnprotectCallCount { get; private set; }

    public string Protect<T>(T state, string? owner)
    {
        ProtectCallCount++;
        return _inner.Protect(state, owner);
    }

    public PageStateReadResult<T> Unprotect<T>(string? token, string? owner)
    {
        UnprotectCallCount++;
        return _inner.Unprotect<T>(token, owner);
    }
}
