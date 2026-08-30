using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using PageState.Internal;

namespace PageState.Tests;

internal static class TestFactory
{
    public static PageStateOptions CreateOptions(Action<PageStateOptions>? configure = null)
    {
        var options = new PageStateOptions();
        configure?.Invoke(options);
        return options;
    }

    public static PageStateProtector CreateProtector(
        PageStateOptions? options = null,
        TimeProvider? timeProvider = null,
        IDataProtectionProvider? dataProtectionProvider = null)
    {
        return new PageStateProtector(
            dataProtectionProvider ?? new EphemeralDataProtectionProvider(),
            Options.Create(options ?? CreateOptions()),
            timeProvider ?? TimeProvider.System);
    }

    /// <summary>
    /// Reproduces PageStateProtector.Protect's crypto steps directly over hand-built payload
    /// bytes, so tests can mint a token wrapping a deliberately wrong-shape payload — something
    /// you cannot produce by tampering with a real ciphertext, since Data Protection is
    /// authenticated and a single bit flip just breaks the integrity check.
    /// </summary>
    public static string MintRawPayload<T>(
        IDataProtectionProvider provider,
        byte[] payload,
        string? owner,
        TimeSpan? lifetime = null,
        TimeProvider? timeProvider = null)
    {
        var expiresAt = (timeProvider ?? TimeProvider.System).GetUtcNow() + (lifetime ?? TimeSpan.FromMinutes(30));
        var timeLimited = provider.CreateProtector("PageState", typeof(T).FullName!, owner ?? "\0anon").ToTimeLimitedDataProtector();
        var ciphertext = timeLimited.Protect(payload, expiresAt);
        return WebEncoders.Base64UrlEncode(ciphertext);
    }
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

internal sealed class SpyDataProtectionProvider : IDataProtectionProvider
{
    private readonly IDataProtectionProvider _inner = new EphemeralDataProtectionProvider();

    public int CreateProtectorCallCount { get; private set; }

    public IDataProtector CreateProtector(string purpose)
    {
        CreateProtectorCallCount++;
        return _inner.CreateProtector(purpose);
    }
}
