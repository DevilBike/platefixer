using System.Buffers.Binary;

namespace PlateFixer.SaveData.Tests;

/// <summary>Builds small synthetic UISAVE.DAT files in the same layout as the game's.</summary>
internal static class TestFiles
{
    public static byte[] Portrait(ushort id, ushort background, ushort pose, byte race, uint checksum)
    {
        using var s = new MemoryStream();
        void U16(ushort v) { Span<byte> b = stackalloc byte[2]; BinaryPrimitives.WriteUInt16LittleEndian(b, v); s.Write(b); }
        void Field(ushort tag, params byte[] value) { U16(tag); s.Write(value); }
        void Prefixed(ushort tag, params byte[] value) { U16(tag); U16((ushort)value.Length); s.Write(value); }
        byte[] Le16(ushort v) => [(byte)v, (byte)(v >> 8)];
        byte[] Le32(uint v) => BitConverter.GetBytes(v);

        Field(PortraitTag.Id, Le16(id));
        Field(PortraitTag.Background, Le16(background));
        Field(PortraitTag.Frame, Le16(2));
        Field(PortraitTag.Accent, Le16(1));
        Field(PortraitTag.Pose, Le16(pose));
        Field(PortraitTag.AnimationFrame, Le32(0x431F1B17));
        Field(PortraitTag.Expression, Le16(5));
        Field(PortraitTag.CameraPosition, 1, 2, 3, 4, 5, 6);
        Field(PortraitTag.CameraTarget, 6, 5, 4, 3, 2, 1);
        Field(PortraitTag.CameraZoom, Le16(0));
        Field(PortraitTag.ImageRotation, Le16(0));
        Field(PortraitTag.DirectionalColor, 0xFF, 0xFF, 0xFF, 0);
        Field(PortraitTag.DirectionalBrightness, Le16(0x7F));
        Field(PortraitTag.DirectionalAngles, 0x85, 0, 0xB3, 0xFF);
        Field(PortraitTag.AmbientColor, 0x33, 0x33, 0x33, 0);
        Field(PortraitTag.AmbientBrightness, Le16(0x7F));
        Field(PortraitTag.HeadDirection, 0, 0, 0, 0);
        Field(PortraitTag.EyeDirection, 0, 0, 0, 0);
        Field(PortraitTag.LastUpdated, Le32(1781410753));
        Prefixed(PortraitTag.Appearance, race, 1, 100, 12);
        Field(PortraitTag.GearChecksum, Le32(checksum));
        Prefixed(PortraitTag.Unknown18, 0, 0);
        return s.ToArray();
    }

    public static byte[] BannerSection(params byte[][] portraits)
    {
        var body = portraits.SelectMany(p => p).ToArray();
        var header = new byte[32];
        header[0] = 1;
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(20), body.Length);
        return [.. header, .. body];
    }

    public static byte[] UiSave(ulong contentId, int padding, params (ushort Id, byte[] Data)[] sections)
    {
        using var decoded = new MemoryStream();
        decoded.Write([0x10, 0, 0, 0, 0x2D, 0, 0, 0]);
        decoded.Write(BitConverter.GetBytes(contentId));
        foreach (var (id, data) in sections)
        {
            var header = new byte[16];
            BinaryPrimitives.WriteUInt16LittleEndian(header, id);
            BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(8), data.Length);
            decoded.Write(header);
            decoded.Write(data);
            decoded.Write(new byte[4]);
        }

        var encoded = decoded.ToArray().Select(b => (byte)(b ^ 0x31)).ToArray();
        var file = new byte[16 + encoded.Length + padding];
        BinaryPrimitives.WriteInt32LittleEndian(file, 0x00010009);
        BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(4), file.Length - 32);
        BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(8), encoded.Length);
        encoded.CopyTo(file, 16);
        return file;
    }
}
