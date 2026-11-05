using PlateFixer.Rules.Model;

namespace PlateFixer.Rules.Rules;

/// <summary>
/// Gear set portraits that no longer match their gear set or the character's body.
/// The game shows the default portrait in duties for these.
/// </summary>
public sealed class GearsetPortraitOutdatedRule : IPlateRule
{
    public string Id => "gearset-portrait-outdated";

    public string Description => "Gear set portrait doesn't match its gear set or your character.";

    public IEnumerable<PlateIssue> Check(PlateSnapshot snapshot)
    {
        foreach (var portrait in snapshot.GearsetPortraits)
        {
            var name = NameOf(portrait);

            if (portrait.IsOutdated)
            {
                var why = (portrait.GearMismatch, portrait.AppearanceMatches == false) switch
                {
                    (true, true) => "was saved on a different body type and in different gear",
                    (false, true) => "was saved on a different body type (copied from another character, or from before a Fantasia)",
                    _ => "was saved in different gear or glamour",
                };

                yield return new PlateIssue(Id, IssueSeverity.Error,
                    $"The portrait for {name} {why}, so duties will show the default portrait.")
                {
                    SuggestedFix = new FixAction(FixKind.RecaptureGearsetPortrait,
                        $"Update the portrait for {name} to match that gear set and your character. Pose, camera and background stay the same.")
                    {
                        GearsetIndex = portrait.GearsetIndex,
                    },
                };
            }
            else if (portrait.CannotCheckReason is { } reason)
            {
                yield return new PlateIssue(Id, IssueSeverity.Info, $"Couldn't check the portrait for {name}: {reason}");
            }
        }
    }

    private static string NameOf(PortraitInfo portrait) =>
        string.IsNullOrWhiteSpace(portrait.GearsetName)
            ? $"gear set {portrait.GearsetIndex + 1}"
            : $"gear set {portrait.GearsetIndex + 1} \"{portrait.GearsetName}\"";
}
