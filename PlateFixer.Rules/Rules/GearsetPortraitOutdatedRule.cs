using PlateFixer.Rules.Model;

namespace PlateFixer.Rules.Rules;

/// <summary>Gear set portraits that no longer match their gear set. These show as outdated in the game.</summary>
public sealed class GearsetPortraitOutdatedRule : IPlateRule
{
    public string Id => "gearset-portrait-outdated";

    public string Description => "Gear set portrait no longer matches its gear set.";

    public IEnumerable<PlateIssue> Check(PlateSnapshot snapshot)
    {
        foreach (var portrait in snapshot.GearsetPortraits.Where(p => p.IsOutdated))
        {
            var name = string.IsNullOrWhiteSpace(portrait.GearsetName)
                ? $"gear set {portrait.GearsetIndex + 1}"
                : $"\"{portrait.GearsetName}\"";

            yield return new PlateIssue(Id, IssueSeverity.Warning,
                $"The portrait for {name} doesn't match that gear set anymore.")
            {
                SuggestedFix = new FixAction(FixKind.RecaptureGearsetPortrait,
                    $"Retake the portrait for {name} in that gear set's current gear.")
                {
                    GearsetIndex = portrait.GearsetIndex,
                },
            };
        }
    }
}
