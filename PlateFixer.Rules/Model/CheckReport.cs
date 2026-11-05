namespace PlateFixer.Rules.Model;

public enum IssueSeverity
{
    /// <summary>Worth knowing about, nothing will break.</summary>
    Info,

    /// <summary>Likely to cause or already caused a revert.</summary>
    Warning,

    /// <summary>The game will reject or revert this.</summary>
    Error,
}

/// <summary>Something that can be done to fix an issue. The plugin carries it out; the rules only describe it.</summary>
public enum FixKind
{
    None,
    RecapturePlatePortrait,
    RecaptureGearsetPortrait,
    ResetDesignItemToDefault,
    ShowPlate,
}

public sealed record FixAction(FixKind Kind, string Description)
{
    public int? GearsetIndex { get; init; }

    public DesignItemKind? DesignItemKind { get; init; }
}

public sealed record PlateIssue(string RuleId, IssueSeverity Severity, string Message)
{
    public FixAction? SuggestedFix { get; init; }
}

public sealed record CheckReport(PlateSnapshot Snapshot, IReadOnlyList<PlateIssue> Issues)
{
    /// <summary>True when nothing would make the game reject or revert the plate.</summary>
    public bool IsValid => Issues.All(i => i.Severity != IssueSeverity.Error);

    public bool HasFixes => Issues.Any(i => i.SuggestedFix is { Kind: not FixKind.None });

    /// <summary>The fixes to show in the preview, one per issue that has one, errors first.</summary>
    public IReadOnlyList<FixAction> FixPlan =>
        Issues.OrderByDescending(i => i.Severity)
              .Select(i => i.SuggestedFix)
              .OfType<FixAction>()
              .Where(f => f.Kind != FixKind.None)
              .Distinct()
              .ToList();
}
