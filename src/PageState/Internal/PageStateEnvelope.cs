using System.Buffers.Binary;
using System.Text;

namespace PageState.Internal;

internal static class PageStateEnvelope
{
    public const byte CurrentVersion = 1;

    private const int HeaderLength = 1 + 4 + 4;

    public static byte[] Wrap(int schemaVersion, string? owner, ReadOnlySpan<byte> payload)
    {
        var ownerBytes = owner is null ? null : Encoding.UTF8.GetBytes(owner);
        var ownerByteCount = ownerBytes?.Length ?? -1;

        var frame = new byte[HeaderLength + (ownerBytes?.Length ?? 0) + payload.Length];
        var span = frame.AsSpan();

        span[0] = CurrentVersion;
        BinaryPrimitives.WriteInt32LittleEndian(span[1..5], schemaVersion);
        BinaryPrimitives.WriteInt32LittleEndian(span[5..9], ownerByteCount);

        var offset = HeaderLength;
        if (ownerBytes is not null)
        {
            ownerBytes.CopyTo(span[offset..]);
            offset += ownerBytes.Length;
        }

        payload.CopyTo(span[offset..]);

        return frame;
    }

    public static bool TryUnwrap(
        ReadOnlySpan<byte> frame,
        out byte envelopeVersion,
        out int schemaVersion,
        out string? owner,
        out ReadOnlySpan<byte> payload)
    {
        envelopeVersion = 0;
        schemaVersion = 0;
        owner = null;
        payload = default;

        if (frame.Length < HeaderLength)
        {
            return false;
        }

        envelopeVersion = frame[0];
        schemaVersion = BinaryPrimitives.ReadInt32LittleEndian(frame[1..5]);
        var ownerByteCount = BinaryPrimitives.ReadInt32LittleEndian(frame[5..9]);

        if (ownerByteCount < -1)
        {
            return false;
        }

        var offset = HeaderLength;

        if (ownerByteCount == -1)
        {
            owner = null;
        }
        else
        {
            if (ownerByteCount > frame.Length - offset)
            {
                return false;
            }

            owner = Encoding.UTF8.GetString(frame.Slice(offset, ownerByteCount));
            offset += ownerByteCount;
        }

        payload = frame[offset..];
        return true;
    }
}
