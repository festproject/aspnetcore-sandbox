using System.Buffers.Binary;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using PageState.Internal;

namespace PageState.Tests;

/// <summary>
/// §4.2 of the implementation guide: one test per failure mode. Every case must return a status
/// and none may throw — every input here is attacker-controlled.
/// </summary>
public class PageStateProtectorFailureModeTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Unprotect_ReturnsMissing_ForNullOrEmptyToken(string? token)
    {
        var protector = TestFactory.CreateProtector();

        var result = protector.Unprotect<WorkflowAStateV1>(token, owner: null);

        Assert.Equal(PageStateStatus.Missing, result.Status);
    }

    [Fact]
    public void Unprotect_ReturnsInvalidToken_ForRandomBase64Garbage()
    {
        var protector = TestFactory.CreateProtector();

        var result = protector.Unprotect<WorkflowAStateV1>("!!!not-valid-base64-garbage!!!", owner: null);

        Assert.Equal(PageStateStatus.InvalidToken, result.Status);
    }

    [Fact]
    public void Unprotect_ReturnsInvalidToken_WhenOneByteFlippedInTheMiddle()
    {
        var protector = TestFactory.CreateProtector();
        var token = protector.Protect(new WorkflowAStateV1("value"), owner: null);

        var bytes = WebEncoders.Base64UrlDecode(token);
        bytes[bytes.Length / 2] ^= 0xFF;
        var tampered = WebEncoders.Base64UrlEncode(bytes);

        var result = protector.Unprotect<WorkflowAStateV1>(tampered, owner: null);

        Assert.Equal(PageStateStatus.InvalidToken, result.Status);
    }

    [Fact]
    public void Unprotect_ReturnsInvalidToken_WhenMintedForDifferentWorkflow()
    {
        var provider = new EphemeralDataProtectionProvider();
        var protector = TestFactory.CreateProtector(dataProtectionProvider: provider);
        var token = protector.Protect(new WorkflowAStateV1("value"), owner: null);

        var result = protector.Unprotect<WorkflowBState>(token, owner: null);

        Assert.Equal(PageStateStatus.InvalidToken, result.Status);
    }

    [Fact]
    public void Unprotect_ReturnsExpired_ForTokenMinted31MinutesAgo()
    {
        var fakeClock = new FixedTimeProvider(DateTimeOffset.UtcNow.AddMinutes(-31));
        var protector = TestFactory.CreateProtector(timeProvider: fakeClock);
        var token = protector.Protect(new WorkflowAStateV1("value"), owner: null);

        var result = protector.Unprotect<WorkflowAStateV1>(token, owner: null);

        Assert.Equal(PageStateStatus.Expired, result.Status);
    }

    [Theory]
    [InlineData("a", "b")]
    [InlineData(null, "b")]
    [InlineData("a", null)]
    public void Unprotect_ReturnsWrongOwner_OnOwnerMismatch(string? mintOwner, string? readOwner)
    {
        var protector = TestFactory.CreateProtector();
        var token = protector.Protect(new WorkflowAStateV1("value"), mintOwner);

        var result = protector.Unprotect<WorkflowAStateV1>(token, readOwner);

        Assert.Equal(PageStateStatus.WrongOwner, result.Status);
    }

    [Fact]
    public void Unprotect_ReturnsInvalidEnvelope_WhenEnvelopeVersionForcedTo99()
    {
        var provider = new EphemeralDataProtectionProvider();
        var protector = TestFactory.CreateProtector(dataProtectionProvider: provider);

        var frame = PageStateEnvelope.Wrap(1, null, "{\"Name\":\"x\"}"u8.ToArray());
        frame[0] = 99;
        var token = TestFactory.MintRawFrame(provider, "WorkflowA", frame);

        var result = protector.Unprotect<WorkflowAStateV1>(token, owner: null);

        Assert.Equal(PageStateStatus.InvalidEnvelope, result.Status);
    }

    [Fact]
    public void Unprotect_ReturnsInvalidEnvelope_WhenFrameTruncatedTo3Bytes()
    {
        var provider = new EphemeralDataProtectionProvider();
        var protector = TestFactory.CreateProtector(dataProtectionProvider: provider);

        var frame = new byte[] { 1, 2, 3 };
        var token = TestFactory.MintRawFrame(provider, "WorkflowA", frame);

        var result = protector.Unprotect<WorkflowAStateV1>(token, owner: null);

        Assert.Equal(PageStateStatus.InvalidEnvelope, result.Status);
    }

    [Fact]
    public void Unprotect_ReturnsInvalidEnvelope_WhenOwnerByteCountRunsPastEndOfFrame()
    {
        var provider = new EphemeralDataProtectionProvider();
        var protector = TestFactory.CreateProtector(dataProtectionProvider: provider);

        var frame = PageStateEnvelope.Wrap(1, "short", "{}"u8.ToArray());
        BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(5, 4), 10_000);
        var token = TestFactory.MintRawFrame(provider, "WorkflowA", frame);

        var result = protector.Unprotect<WorkflowAStateV1>(token, owner: null);

        Assert.Equal(PageStateStatus.InvalidEnvelope, result.Status);
    }

    [Fact]
    public void Unprotect_ReturnsInvalidSchema_WhenSchemaVersionMismatches()
    {
        var provider = new EphemeralDataProtectionProvider();
        var protector = TestFactory.CreateProtector(dataProtectionProvider: provider);
        var token = protector.Protect(new WorkflowAStateV1("value"), owner: null);

        var result = protector.Unprotect<WorkflowAStateV2>(token, owner: null);

        Assert.Equal(PageStateStatus.InvalidSchema, result.Status);
    }

    [Fact]
    public void Unprotect_ReturnsInvalidPayload_WhenPayloadIsValidJsonButWrongShape()
    {
        var provider = new EphemeralDataProtectionProvider();
        var protector = TestFactory.CreateProtector(dataProtectionProvider: provider);

        var wrongShapeJson = "{\"SomethingElse\":\"nope\"}"u8.ToArray();
        var frame = PageStateEnvelope.Wrap(1, null, wrongShapeJson);
        var token = TestFactory.MintRawFrame(provider, "WrongShape", frame);

        var result = protector.Unprotect<WrongShapeState>(token, owner: null);

        Assert.Equal(PageStateStatus.InvalidPayload, result.Status);
    }
}
