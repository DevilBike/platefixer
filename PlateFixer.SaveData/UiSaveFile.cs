using System.Buffers.Binary;

namespace PlateFixer.SaveData;

/// <summary>
/// UISAVE.DAT from an FFXIV_CHR folder.
///
/// Layout: a 16-byte header (u32 unknown, u32 file size minus 32, u32 encoded length N, u32 unknown),
/// then N bytes XORed with 0x31, then padding. Decoded: 8 unknown bytes, the character's u64 content id,
/// then sections. Each section is a 16-byte header (u16 id, u32 data length at +8), the data, and 4 trailing bytes.
/// Everything this class doesn't understand is kept byte for byte.
/// </summary>
public sealed class UiSaveFile
{
    public const byte XorKey = 0x31;
    public const ushort BannerSectionId = 0x17;

    private const int FileHeaderSize = 16;
    private const int SectionHeaderSize = 16;
    private const int SectionTrailerSize = 4;

    private readonly byte[] fileHeader;
    private readonly byte[] decodedPrefix; // 8 unknown bytes + content id
    private readonly int originalFileLength;

    public ulong ContentId => BinaryPrimitives.ReadUInt64LittleEndian(decodedPrefix.AsSpan(8));

    public List<UiSaveSection> Sections { get; }

    private UiSaveFile(byte[] fileHeader, byte[] decodedPrefix, List<UiSaveSection> sections, int originalFileLength)
    {
        this.fileHeader = fileHeader;
        this.decodedPrefix = decodedPrefix;
        Sections = sections;
        this.originalFileLength = originalFileLength;
    }

    public static UiSaveFile Load(string path) => Parse(File.ReadAllBytes(path));

    public static UiSaveFile Parse(ReadOnlySpan<byte> raw)
    {
        if (raw.Length < FileHeaderSize + 16)
            throw new InvalidDataException("File is too short to be UISAVE.DAT.");

        var encodedLength = BinaryPrimitives.ReadInt32LittleEndian(raw[8..]);
        if (encodedLength < 16 || FileHeaderSize + encodedLength > raw.Length)
            throw new InvalidDataException("UISAVE.DAT header gives a length that doesn't fit the file.");

        var decoded = raw.Slice(FileHeaderSize, encodedLength).ToArray();
        Xor(decoded);

        var sections = new List<UiSaveSection>();
        var offset = 16;
        while (offset < decoded.Length)
        {
            if (offset + SectionHeaderSize > decoded.Length)
                throw new InvalidDataException($"Section header at {offset} runs past the end of the data.");

            var header = decoded.AsSpan(offset, SectionHeaderSize).ToArray();
            var length = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(8));
            var dataStart = offset + SectionHeaderSize;
            if (length < 0 || dataStart + length + SectionTrailerSize > decoded.Length)
                throw new InvalidDataException($"Section at {offset} runs past the end of the data.");

            sections.Add(new UiSaveSection(
                header,
                decoded.AsSpan(dataStart, length).ToArray(),
                decoded.AsSpan(dataStart + length, SectionTrailerSize).ToArray()));
            offset = dataStart + length + SectionTrailerSize;
        }

        if (offset != decoded.Length)
            throw new InvalidDataException("Sections don't end exactly at the end of the data.");

        return new UiSaveFile(raw[..FileHeaderSize].ToArray(), decoded[..16], sections, raw.Length);
    }

    public UiSaveSection? FindSection(ushort id) => Sections.FirstOrDefault(s => s.Id == id);

    /// <summary>Encodes the file. Keeps the original file size when the data still fits, as the game does.</summary>
    public byte[] ToBytes()
    {
        using var body = new MemoryStream();
        body.Write(decodedPrefix);
        foreach (var section in Sections)
        {
            var header = section.Header.ToArray();
            BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(8), section.Data.Length);
            body.Write(header);
            body.Write(section.Data);
            body.Write(section.Trailer);
        }

        var encoded = body.ToArray();
        Xor(encoded);

        var total = Math.Max(originalFileLength, FileHeaderSize + encoded.Length);
        var output = new byte[total];
        fileHeader.CopyTo(output, 0);
        BinaryPrimitives.WriteInt32LittleEndian(output.AsSpan(4), total - 32);
        BinaryPrimitives.WriteInt32LittleEndian(output.AsSpan(8), encoded.Length);
        encoded.CopyTo(output, FileHeaderSize);
        return output;
    }

    private static void Xor(Span<byte> data)
    {
        for (var i = 0; i < data.Length; i++)
            data[i] ^= XorKey;
    }
}

public sealed class UiSaveSection(byte[] header, byte[] data, byte[] trailer)
{
    public ushort Id => BinaryPrimitives.ReadUInt16LittleEndian(Header);

    public byte[] Header { get; } = header;

    public byte[] Data { get; set; } = data;

    public byte[] Trailer { get; } = trailer;
}
