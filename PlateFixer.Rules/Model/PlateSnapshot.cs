namespace PlateFixer.Rules.Model;

/// <summary>
/// Everything the rules need to know about the player's adventurer plate at one moment.
/// Plain data only, so it can be built from game memory by the plugin or loaded from JSON.
/// </summary>
public sealed record PlateSnapshot
{
    /// <summary>Format version of this snapshot, bumped when fields change meaning.</summary>
    public int SchemaVersion { get; init; } = 1;

    public DateTimeOffset TakenAt { get; init; } = DateTimeOffset.UtcNow;

    public string? CharacterName { get; init; }

    /// <summary>The ClassJob row id the character is currently on.</summary>
    public uint CurrentClassJobId { get; init; }

    /// <summary>The portrait shown on the plate.</summary>
    public PortraitInfo? PlatePortrait { get; init; }

    /// <summary>True when the plate is showing the game's default portrait (what a revert looks like).</summary>
    public bool PlateShowsDefaultPortrait { get; init; }

    /// <summary>True when the plate is set to not be shown to others (for example after using a Fantasia).</summary>
    public bool PlateHidden { get; init; }

    /// <summary>Backgrounds, frames, accents and other design items used on the plate.</summary>
    public IReadOnlyList<DesignItem> DesignItems { get; init; } = [];

    /// <summary>
    /// Design items the character has unlocked, so an outside editor can offer them by name.
    /// Optional: the plugin may leave it empty.
    /// </summary>
    public IReadOnlyList<DesignItem> AvailableDesignItems { get; init; } = [];

    /// <summary>Portraits saved against gear sets (the Portraits list in the game).</summary>
    public IReadOnlyList<PortraitInfo> GearsetPortraits { get; init; } = [];
}

/// <summary>A saved portrait and whether it still matches the gear it was taken in.</summary>
public sealed record PortraitInfo
{
    /// <summary>Gear set index (0 based) this portrait belongs to, or null for the plate's own portrait.</summary>
    public int? GearsetIndex { get; init; }

    public string? GearsetName { get; init; }

    public uint ClassJobId { get; init; }

    /// <summary>Checksum of gear, dyes, glasses and visor/hat/weapon flags when the portrait was saved.</summary>
    public uint SavedGearChecksum { get; init; }

    /// <summary>Checksum of the matching gear right now, or null if it could not be computed.</summary>
    public uint? CurrentGearChecksum { get; init; }

    public bool IsOutdated => CurrentGearChecksum is { } current && current != SavedGearChecksum;
}

public enum DesignItemKind
{
    Background,
    Frame,
    Accent,
    BasePlate,
    TopBorder,
    BottomBorder,
    Backing,
    PatternOverlay,
    PortraitFrame,
    PlateFrame,
    PortraitBackground,
    Pose,
    Expression,
    Other,
}

/// <summary>A design item on the plate, and whether the character still has it unlocked.</summary>
public sealed record DesignItem
{
    public DesignItemKind Kind { get; init; }

    /// <summary>Row id in the item's sheet. 0 usually means "none" or default.</summary>
    public uint RowId { get; init; }

    public string? Name { get; init; }

    /// <summary>Unlock state from the game, or null if it could not be read.</summary>
    public bool? IsUnlocked { get; init; }
}
