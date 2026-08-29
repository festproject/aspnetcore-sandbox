using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace PageState.Internal;

internal sealed class PageStateProtector : IPageStateProtector
{
    private readonly IDataProtectionProvider _provider;
    private readonly PageStateRegistry _registry;
    private readonly PageStateOptions _options;
    private readonly TimeProvider _timeProvider;

    public PageStateProtector(
        IDataProtectionProvider provider,
        PageStateRegistry registry,
        IOptions<PageStateOptions> options,
        TimeProvider timeProvider)
    {
        _provider = provider;
        _registry = registry;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public string Protect<T>(T state, string? owner)
    {
        var descriptor = _registry.Get<T>();

        var payload = JsonSerializer.SerializeToUtf8Bytes(state, _options.SerializerOptions);
        if (payload.Length > _options.MaxPayloadBytes)
        {
            throw new PageStateTooLargeException(typeof(T), payload.Length, _options.MaxPayloadBytes);
        }

        var frame = PageStateEnvelope.Wrap(descriptor.SchemaVersion, owner, payload);

        var protector = CreateTimeLimitedProtector(descriptor.Workflow);
        var expiresAt = _timeProvider.GetUtcNow() + descriptor.Lifetime;
        var ciphertext = protector.Protect(frame, expiresAt);

        return WebEncoders.Base64UrlEncode(ciphertext);
    }

    public PageStateReadResult<T> Unprotect<T>(string? token, string? owner)
    {
        if (string.IsNullOrEmpty(token))
        {
            return new PageStateReadResult<T>(PageStateStatus.Missing, default);
        }

        if (token.Length > _options.MaxTokenChars)
        {
            return new PageStateReadResult<T>(PageStateStatus.TooLarge, default);
        }

        var descriptor = _registry.Get<T>();

        byte[] frame;
        try
        {
            var ciphertext = WebEncoders.Base64UrlDecode(token);
            var protector = CreateTimeLimitedProtector(descriptor.Workflow);
            frame = protector.Unprotect(ciphertext, out _);
        }
        catch (FormatException)
        {
            return new PageStateReadResult<T>(PageStateStatus.InvalidToken, default);
        }
        catch (CryptographicException ex)
        {
            var status = LooksExpired(ex) ? PageStateStatus.Expired : PageStateStatus.InvalidToken;
            return new PageStateReadResult<T>(status, default);
        }

        if (!PageStateEnvelope.TryUnwrap(frame, out var envelopeVersion, out var schemaVersion, out var frameOwner, out var payload)
            || envelopeVersion != PageStateEnvelope.CurrentVersion)
        {
            return new PageStateReadResult<T>(PageStateStatus.InvalidEnvelope, default);
        }

        if (schemaVersion != descriptor.SchemaVersion)
        {
            return new PageStateReadResult<T>(PageStateStatus.InvalidSchema, default);
        }

        if (!string.Equals(frameOwner, owner, StringComparison.Ordinal))
        {
            return new PageStateReadResult<T>(PageStateStatus.WrongOwner, default);
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

    private ITimeLimitedDataProtector CreateTimeLimitedProtector(string workflow)
        => _provider.CreateProtector("PageState", workflow, "v1").ToTimeLimitedDataProtector();

    // Data Protection has no distinct exception subclass for "token expired" — it throws a plain
    // CryptographicException whose message names the expiry. Sniffing the message is fragile but
    // the only signal available; worst case a message-format change reports Expired as InvalidToken,
    // which is harmless since both statuses render identically to the user (see PAGESTATE guide §5.5).
    private static bool LooksExpired(CryptographicException ex)
        => ex.Message.Contains("expired", StringComparison.OrdinalIgnoreCase);
}
