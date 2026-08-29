using System.Buffers.Binary;
using PageState.Internal;

namespace PageState.Tests;

public class PageStateEnvelopeTests
{
    [Fact]
    public void Wrap_TryUnwrap_RoundTrips_WithOwner()
    {
        var payload = "{\"a\":1}"u8.ToArray();
        var frame = PageStateEnvelope.Wrap(3, "owner-1", payload);

        var ok = PageStateEnvelope.TryUnwrap(frame, out var version, out var schema, out var owner, out var unwrappedPayload);

        Assert.True(ok);
        Assert.Equal(PageStateEnvelope.CurrentVersion, version);
        Assert.Equal(3, schema);
        Assert.Equal("owner-1", owner);
        Assert.Equal(payload, unwrappedPayload.ToArray());
    }

    [Fact]
    public void Wrap_TryUnwrap_RoundTrips_WithNullOwner()
    {
        var payload = "{}"u8.ToArray();
        var frame = PageStateEnvelope.Wrap(1, null, payload);

        var ok = PageStateEnvelope.TryUnwrap(frame, out _, out _, out var owner, out var unwrappedPayload);

        Assert.True(ok);
        Assert.Null(owner);
        Assert.Equal(payload, unwrappedPayload.ToArray());
    }

    [Fact]
    public void TryUnwrap_ReturnsFalse_WhenFrameTruncatedTo3Bytes()
    {
        var frame = new byte[] { 1, 2, 3 };

        var ok = PageStateEnvelope.TryUnwrap(frame, out _, out _, out _, out _);

        Assert.False(ok);
    }

    [Fact]
    public void TryUnwrap_ReturnsFalse_WhenOwnerByteCountRunsPastEndOfFrame()
    {
        var frame = PageStateEnvelope.Wrap(1, "short", "{}"u8.ToArray());
        BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(5, 4), 10_000);

        var ok = PageStateEnvelope.TryUnwrap(frame, out _, out _, out _, out _);

        Assert.False(ok);
    }

    [Fact]
    public void TryUnwrap_ReturnsFalse_WhenOwnerByteCountLessThanNegativeOne()
    {
        var frame = PageStateEnvelope.Wrap(1, null, "{}"u8.ToArray());
        BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(5, 4), -5);

        var ok = PageStateEnvelope.TryUnwrap(frame, out _, out _, out _, out _);

        Assert.False(ok);
    }

    [Fact]
    public void TryUnwrap_ReportsForcedEnvelopeVersion_WithoutThrowing()
    {
        var frame = PageStateEnvelope.Wrap(1, null, "{}"u8.ToArray());
        frame[0] = 99;

        var ok = PageStateEnvelope.TryUnwrap(frame, out var version, out _, out _, out _);

        Assert.True(ok);
        Assert.Equal(99, version);
        Assert.NotEqual(PageStateEnvelope.CurrentVersion, version);
    }
}
