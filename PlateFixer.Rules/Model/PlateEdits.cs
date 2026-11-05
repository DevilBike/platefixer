namespace PlateFixer.Rules.Model;

/// <summary>
/// Changes made in the outside editor, for the plugin to apply in game (after showing a preview).
/// Only fields that are set are changed.
/// </summary>
public sealed record PlateEdits
{
    public int SchemaVersion { get; init; } = 1;

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Name of the character the snapshot came from, so the plugin can refuse edits meant for someone else.</summary>
    public string? CharacterName { get; init; }

    /// <summary>Design items to switch to, at most one per kind.</summary>
    public IReadOnlyList<DesignItem> DesignItems { get; init; } = [];

    /// <summary>Set to change whether the plate is shown to others.</summary>
    public bool? PlateHidden { get; init; }

    /// <summary>Retake the plate portrait in current gear, keeping pose, camera and background.</summary>
    public bool RecapturePlatePortrait { get; init; }

    /// <summary>Gear set indexes whose portraits should be retaken.</summary>
    public IReadOnlyList<int> RecaptureGearsetPortraits { get; init; } = [];

    public bool IsEmpty =>
        DesignItems.Count == 0 && PlateHidden is null && !RecapturePlatePortrait && RecaptureGearsetPortraits.Count == 0;

    /// <summary>The snapshot as it would look after these edits, so the rules can check it before anything is applied.</summary>
    public PlateSnapshot ApplyTo(PlateSnapshot snapshot)
    {
        var items = snapshot.DesignItems.ToDictionary(i => i.Kind);
        foreach (var edit in DesignItems)
            items[edit.Kind] = edit;

        PortraitInfo? Refresh(PortraitInfo? p) =>
            p is { CurrentGearChecksum: { } current } ? p with { SavedGearChecksum = current } : p;

        return snapshot with
        {
            DesignItems = items.Values.OrderBy(i => i.Kind).ToList(),
            PlateHidden = PlateHidden ?? snapshot.PlateHidden,
            PlatePortrait = RecapturePlatePortrait ? Refresh(snapshot.PlatePortrait) : snapshot.PlatePortrait,
            PlateShowsDefaultPortrait = !RecapturePlatePortrait && snapshot.PlateShowsDefaultPortrait,
            GearsetPortraits = snapshot.GearsetPortraits
                .Select(p => p.GearsetIndex is { } i && RecaptureGearsetPortraits.Contains(i) ? Refresh(p)! : p)
                .ToList(),
        };
    }
}
