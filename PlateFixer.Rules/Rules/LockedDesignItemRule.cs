using PlateFixer.Rules.Model;

namespace PlateFixer.Rules.Rules;

/// <summary>A design item on the plate that the character doesn't have unlocked. Saving would fail.</summary>
public sealed class LockedDesignItemRule : IPlateRule
{
    public string Id => "design-item-locked";

    public string Description => "Plate uses a design item that isn't unlocked.";

    public IEnumerable<PlateIssue> Check(PlateSnapshot snapshot)
    {
        foreach (var item in snapshot.DesignItems.Where(i => i.RowId != 0 && i.IsUnlocked == false))
        {
            var name = item.Name ?? $"#{item.RowId}";
            yield return new PlateIssue(Id, IssueSeverity.Error,
                $"The {Describe(item.Kind)} \"{name}\" isn't unlocked on this character, so the game won't accept it.")
            {
                SuggestedFix = new FixAction(FixKind.ResetDesignItemToDefault,
                    $"Switch the {Describe(item.Kind)} back to the default.")
                {
                    DesignItemKind = item.Kind,
                },
            };
        }
    }

    private static string Describe(DesignItemKind kind) => kind switch
    {
        DesignItemKind.BasePlate => "base plate",
        DesignItemKind.TopBorder => "top border",
        DesignItemKind.BottomBorder => "bottom border",
        DesignItemKind.PatternOverlay => "pattern overlay",
        DesignItemKind.PortraitFrame => "portrait frame",
        DesignItemKind.PlateFrame => "plate frame",
        DesignItemKind.PortraitBackground => "portrait background",
        DesignItemKind.Other => "design item",
        _ => kind.ToString().ToLowerInvariant(),
    };
}
