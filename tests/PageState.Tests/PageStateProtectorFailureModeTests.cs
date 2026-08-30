using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;

namespace PageState.Tests;

/// <summary>
/// §5.2 of the implementation guide: one test each. Every case must return a status and none may
/// throw — every input here is attacker-controlled.
/// </summary>
public class PageStateProtectorFailureModeTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Unprotect_ReturnsMissing_ForNullOrEmptyToken(string? token)
    {
        var protector = TestFactory.CreateProtector();

        var result = protector.Unprotect<TypeAState>(token, owner: null);

        Assert.Equal(PageStateStatus.Missing, result.Status);
    }

    [Fact]
    public void Unprotect_ReturnsInvalid_ForRandomBase64Garbage()
    {
        var protector = TestFactory.CreateProtector();

        var result = protector.Unprotect<TypeAState>("!!!not-valid-base64-garbage!!!", owner: null);

        Assert.Equal(PageStateStatus.Invalid, result.Status);
    }

    [Fact]
    public void Unprotect_ReturnsInvalid_WhenOneByteFlippedInTheMiddle()
    {
        var protector = TestFactory.CreateProtector();
        var token = protector.Protect(new TypeAState("value"), owner: null);

        var bytes = WebEncoders.Base64UrlDecode(token);
        bytes[bytes.Length / 2] ^= 0xFF;
        var tampered = WebEncoders.Base64UrlEncode(bytes);

        var result = protector.Unprotect<TypeAState>(tampered, owner: null);

        Assert.Equal(PageStateStatus.Invalid, result.Status);
    }

    [Fact]
    public void Unprotect_ReturnsInvalid_WhenMintedForDifferentType()
    {
        var provider = new EphemeralDataProtectionProvider();
        var protector = TestFactory.CreateProtector(dataProtectionProvider: provider);
        var token = protector.Protect(new TypeAState("value"), owner: null);

        var result = protector.Unprotect<TypeBState>(token, owner: null);

        Assert.Equal(PageStateStatus.Invalid, result.Status);
    }

    [Fact]
    public void Unprotect_ReturnsExpired_ForTokenMinted31MinutesAgo()
    {
        var fakeClock = new FixedTimeProvider(DateTimeOffset.UtcNow.AddMinutes(-31));
        var protector = TestFactory.CreateProtector(timeProvider: fakeClock);
        var token = protector.Protect(new TypeAState("value"), owner: null);

        var result = protector.Unprotect<TypeAState>(token, owner: null);

        Assert.Equal(PageStateStatus.Expired, result.Status);
    }

    [Theory]
    [InlineData("a", "b")]
    [InlineData(null, "b")]
    [InlineData("a", null)]
    public void Unprotect_ReturnsInvalid_OnOwnerMismatch(string? mintOwner, string? readOwner)
    {
        var protector = TestFactory.CreateProtector();
        var token = protector.Protect(new TypeAState("value"), mintOwner);

        var result = protector.Unprotect<TypeAState>(token, readOwner);

        Assert.Equal(PageStateStatus.Invalid, result.Status);
    }

    [Fact]
    public void Unprotect_ReturnsInvalidPayload_WhenPayloadIsValidJsonButWrongShape()
    {
        var provider = new EphemeralDataProtectionProvider();
        var protector = TestFactory.CreateProtector(dataProtectionProvider: provider);

        var wrongShapeJson = "{\"SomethingElse\":\"nope\"}"u8.ToArray();
        var token = TestFactory.MintRawPayload<WrongShapeState>(provider, wrongShapeJson, owner: null);

        var result = protector.Unprotect<WrongShapeState>(token, owner: null);

        Assert.Equal(PageStateStatus.InvalidPayload, result.Status);
    }
}
