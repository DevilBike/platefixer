using System.Buffers.Binary;

namespace PlateFixer.SaveData;

/// <summary>
/// The portrait list stored in UISAVE.DAT section 0x17.
///
/// A 32-byte header (u32 at +20 is the length of what follows), then one run of tagged fields per portrait.
/// Each field is a u16 tag and a value. Most tags have a fixed size; tags 0x16 and 0x18 carry a u16 length first.
/// Tag 0x00 starts a new portrait. Unknown tags stop parsing rather than guess, so nothing gets corrupted.
/// </summary>
public sealed class BannerSection
{
    private const int HeaderSize = 32;

    private static readonly Dictionary<ushort, int> FixedSizes = new()
    {
        [PortraitTag.Id] = 2,
        [PortraitTag.Background] = 2,
        [PortraitTag.Frame] = 2,
        [PortraitTag.Accent] = 2,
        [PortraitTag.Pose] = 2,
        [PortraitTag.AnimationFrame] = 4,
        [PortraitTag.Expression] = 2,
        [PortraitTag.CameraPosition] = 6,
        [PortraitTag.CameraTarget] = 6,
        [PortraitTag.CameraZoom] = 2,
        [PortraitTag.ImageRotation] = 2,
        [PortraitTag.AmbientColor] = 4,
        [PortraitTag.AmbientBrightness] = 2,
        [PortraitTag.DirectionalColor] = 4,
        [PortraitTag.DirectionalBrightness] = 2,
        [PortraitTag.DirectionalAngles] = 4,
        [PortraitTag.HeadDirection] = 4,
        [PortraitTag.EyeDirection] = 4,
        [PortraitTag.LastUpdated] = 4,
        [PortraitTag.GearChecksum] = 4,
    };

    private static readonly HashSet<ushort> LengthPrefixed = [PortraitTag.Appearance, PortraitTag.Unknown18];

    private readonly byte[] header;

    public List<Portrait> Portraits { get; }

    private BannerSection(byte[] header, List<Portrait> portraits)
    {
        this.header = header;
        Portraits = portraits;
    }

    public static BannerSection Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < HeaderSize)
            throw new InvalidDataException("Portrait section is too short.");

        var portraits = new List<Portrait>();
        Portrait? current = null;
        var offset = HeaderSize;
        while (offset < data.Length)
        {
            if (offset + 2 > data.Length)
                throw new InvalidDataException("Portrait data ends in the middle of a field.");

            var tag = BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]);
            offset += 2;

            int size;
            if (LengthPrefixed.Contains(tag))
            {
                size = BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]);
                offset += 2;
            }
            else if (!FixedSizes.TryGetValue(tag, out size))
            {
                throw new InvalidDataException($"Unknown portrait field 0x{tag:X2} at offset {offset - 2}.");
            }

            if (offset + size > data.Length)
                throw new InvalidDataException("Portrait data ends in the middle of a field.");

            if (tag == PortraitTag.Id)
            {
                current = new Portrait();
                portraits.Add(current);
            }
            else if (current is null)
            {
                throw new InvalidDataException("Portrait data doesn't start with an id field.");
            }

            current.Fields.Add(new PortraitField(tag, data.Slice(offset, size).ToArray()));
            offset += size;
        }

        return new BannerSection(data[..HeaderSize].ToArray(), portraits);
    }

    public byte[] ToBytes()
    {
        using var body = new MemoryStream();
        Span<byte> u16 = stackalloc byte[2];
        foreach (var field in Portraits.SelectMany(p => p.Fields))
        {
            BinaryPrimitives.WriteUInt16LittleEndian(u16, field.Tag);
            body.Write(u16);
            if (LengthPrefixed.Contains(field.Tag))
            {
                BinaryPrimitives.WriteUInt16LittleEndian(u16, (ushort)field.Value.Length);
                body.Write(u16);
            }
            else if (FixedSizes[field.Tag] != field.Value.Length)
            {
                throw new InvalidOperationException($"Field 0x{field.Tag:X2} must be {FixedSizes[field.Tag]} bytes.");
            }

            body.Write(field.Value);
        }

        var output = new byte[HeaderSize + body.Length];
        header.CopyTo(output, 0);
        BinaryPrimitives.WriteInt32LittleEndian(output.AsSpan(20), (int)body.Length);
        body.ToArray().CopyTo(output, HeaderSize);
        return output;
    }
}
