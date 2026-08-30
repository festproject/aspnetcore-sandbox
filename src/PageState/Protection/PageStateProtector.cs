using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace PageState.Internal;

internal sealed class PageStateProtector : IPageStateProtector
{
    private readonly IDataProtectionProvider _provider;
    private readonly PageStateOptions _options;
    private readonly TimeProvider _timeProvider;

    public PageStateProtector(
        IDataProtectionProvider provider,
        IOptions<PageStateOptions> options,
        TimeProvider timeProvider)
    {
        _provider = provider;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public string Protect<T>(T state, string? owner, PageStateSite site = default) where T : notnull
    {
        // object state = new ProductEditState(...); protector.Protect(state, owner) would infer
        // T == object — JsonSerializer.Serialize<object> writes no properties at all, a silent,
        // total failure. Guard it rather than let it happen quietly.
        if (typeof(T) == typeof(object))
        {
            throw new ArgumentException("T was inferred as object. Call Protect with the concrete state type.");
        }

        var payload = JsonSerializer.SerializeToUtf8Bytes(state, _options.SerializerOptions);
        if (payload.Length > _options.MaxPayloadBytes)
        {
            throw new PageStateTooLargeException(typeof(T), payload.Length, _options.MaxPayloadBytes);
        }

        var protector = CreateTimeLimitedProtector<T>(site, owner);
        var expiresAt = _timeProvider.GetUtcNow() + _options.DefaultLifetime;
        var ciphertext = protector.Protect(payload, expiresAt);

        return WebEncoders.Base64UrlEncode(ciphertext);
    }

    public PageStateReadResult<T> Unprotect<T>(string? token, string? owner, PageStateSite site = default)
    {
        if (string.IsNullOrEmpty(token))
        {
            return new PageStateReadResult<T>(PageStateStatus.Missing, default);
        }

        if (token.Length > _options.MaxTokenChars)
        {
            return new PageStateReadResult<T>(PageStateStatus.TooLarge, default);
        }

        byte[] payload;
        try
        {
            var ciphertext = WebEncoders.Base64UrlDecode(token);
            var protector = CreateTimeLimitedProtector<T>(site, owner);
            payload = protector.Unprotect(ciphertext, out _);
        }
        catch (FormatException)
        {
            return new PageStateReadResult<T>(PageStateStatus.Invalid, default);
        }
        catch (CryptographicException ex)
        {
            var status = LooksExpired(ex) ? PageStateStatus.Expired : PageStateStatus.Invalid;
            return new PageStateReadResult<T>(status, default);
        }

        T? state;
        try
        {
            state = JsonSerializer.Deserialize<T>(payload, _options.SerializerOptions);
        }
        catch (JsonException)
        {
            return new PageStateReadResult<T>(PageStateStatus.InvalidPayload, default);
        }

        return new PageStateReadResult<T>(PageStateStatus.Success, state);
    }

    // Owner, declaration site, and state type all live in the purpose chain, not in a serialized
    // envelope (see §4.3 of the guide): a wrong owner, wrong property, or wrong type simply fails
    // to decrypt, so there is no comparison anywhere that can be forgotten or gotten wrong.
    //
    // The site (container type + property name) matters because typeof(T).FullName alone is not
    // unique per declaration — every `[PageState] int OrderId` anywhere in the app would share
    // the purpose "System.Int32" and their tokens would be interchangeable. Keying on the site
    // closes that even for a bare primitive; typeof(T).FullName is kept alongside it as a free
    // schema guard — changing a property's type invalidates its tokens instead of failing later
    // at deserialization.
    //
    // The controller/PageModel is deliberately NOT in the chain — the same view model can
    // legitimately be served by more than one host, and including it would make a token minted
    // by one undecryptable by another (§3.7).
    private ITimeLimitedDataProtector CreateTimeLimitedProtector<T>(PageStateSite site, string? owner)
    {
        var segments = site.ContainerType is null
            ? new[] { typeof(T).FullName!, owner ?? "\0anon" }
            : new[] { $"{site.ContainerType.FullName}.{site.PropertyName}", typeof(T).FullName!, owner ?? "\0anon" };

        return _provider.CreateProtector("PageState", segments).ToTimeLimitedDataProtector();
    }

    // Data Protection has no distinct exception subclass for "token expired" — it throws a plain
    // CryptographicException whose message names the expiry. Sniffing the message is fragile but
    // the only signal available; worst case a message-format change reports Expired as Invalid,
    // which is harmless since both statuses render identically to the user (guide §6.4).
    private static bool LooksExpired(CryptographicException ex)
        => ex.Message.Contains("expired", StringComparison.OrdinalIgnoreCase);
}
