using System.Buffers.Binary;

namespace PlateFixer.SaveData;

/// <summary>Field tags in the portrait data. Names past the first few are best guesses from real files.</summary>
public static class PortraitTag
{
    public const ushort Id = 0x00;
    public const ushort Background = 0x02;
    public const ushort Frame = 0x03;
    public const ushort Accent = 0x04;
    public const ushort Pose = 0x05;
    public const ushort AnimationFrame = 0x06;
    public const ushort Expression = 0x07;
    public const ushort CameraPosition = 0x08;
    public const ushort CameraTarget = 0x09;
    public const ushort CameraZoom = 0x0A;
    public const ushort ImageRotation = 0x0B;
    public const ushort AmbientColor = 0x0E;
    public const ushort AmbientBrightness = 0x0F;
    public const ushort DirectionalColor = 0x10;
    public const ushort DirectionalBrightness = 0x11;
    public const ushort DirectionalAngles = 0x12;
    public const ushort HeadDirection = 0x13;
    public const ushort EyeDirection = 0x14;
    public const ushort LastUpdated = 0x15;
    public const ushort Appearance = 0x16;
    public const ushort GearChecksum = 0x17;
    public const ushort Unknown18 = 0x18;

    /// <summary>The fields that make up how a portrait looks, which can be copied between portraits safely.</summary>
    public static IReadOnlyList<ushort> LookFields { get; } =
    [
        Background, Frame, Accent, Pose, AnimationFrame, Expression, CameraPosition, CameraTarget, CameraZoom,
        ImageRotation, AmbientColor, AmbientBrightness, DirectionalColor, DirectionalBrightness, DirectionalAngles,
        HeadDirection, EyeDirection,
    ];
}

public sealed record PortraitField(ushort Tag, byte[] Value)
{
    public byte[] Value { get; set; } = Value;
}

/// <summary>Body type saved with a portrait. A portrait saved on a different race or clan won't match the character.</summary>
public readonly record struct PortraitAppearance(byte Race, byte Sex, byte Height, byte Tribe)
{
    public string RaceName => Race switch
    {
        1 => "Hyur", 2 => "Elezen", 3 => "Lalafell", 4 => "Miqo'te",
        5 => "Roegadyn", 6 => "Au Ra", 7 => "Hrothgar", 8 => "Viera",
        _ => $"race {Race}",
    };

    public override string ToString() => $"{RaceName}, clan {Tribe}, {(Sex == 0 ? "male" : "female")}, height {Height}";
}

/// <summary>One saved portrait. Keeps every field in its original order so writing it back changes only what was edited.</summary>
public sealed class Portrait
{
    public List<PortraitField> Fields { get; } = [];

    public ushort Id => GetU16(PortraitTag.Id) ?? 0;

    public ushort? Background { get => GetU16(PortraitTag.Background); set => SetU16(PortraitTag.Background, value); }
    public ushort? Frame { get => GetU16(PortraitTag.Frame); set => SetU16(PortraitTag.Frame, value); }
    public ushort? Accent { get => GetU16(PortraitTag.Accent); set => SetU16(PortraitTag.Accent, value); }
    public ushort? Pose { get => GetU16(PortraitTag.Pose); set => SetU16(PortraitTag.Pose, value); }
    public ushort? Expression { get => GetU16(PortraitTag.Expression); set => SetU16(PortraitTag.Expression, value); }

    public uint? GearChecksum => Get(PortraitTag.GearChecksum) is { Length: 4 } v ? BinaryPrimitives.ReadUInt32LittleEndian(v) : null;

    public DateTimeOffset? LastUpdated =>
        Get(PortraitTag.LastUpdated) is { Length: 4 } v ? DateTimeOffset.FromUnixTimeSeconds(BinaryPrimitives.ReadUInt32LittleEndian(v)) : null;

    public PortraitAppearance? Appearance =>
        Get(PortraitTag.Appearance) is { Length: >= 4 } v ? new PortraitAppearance(v[0], v[1], v[2], v[3]) : null;

    public byte[]? Get(ushort tag) => Fields.FirstOrDefault(f => f.Tag == tag)?.Value;

    /// <summary>Copies how <paramref name="source"/> looks (pose, background, camera, lighting) onto this portrait.</summary>
    public void CopyLookFrom(Portrait source)
    {
        foreach (var tag in PortraitTag.LookFields)
        {
            if (source.Get(tag) is { } value && Fields.FirstOrDefault(f => f.Tag == tag) is { } field)
                field.Value = value.ToArray();
        }
    }

    private ushort? GetU16(ushort tag) => Get(tag) is { Length: 2 } v ? BinaryPrimitives.ReadUInt16LittleEndian(v) : null;

    private void SetU16(ushort tag, ushort? value)
    {
        if (value is null)
            return;
        var field = Fields.FirstOrDefault(f => f.Tag == tag)
                    ?? throw new InvalidOperationException($"This portrait has no field 0x{tag:X2}.");
        BinaryPrimitives.WriteUInt16LittleEndian(field.Value, value.Value);
    }
}
