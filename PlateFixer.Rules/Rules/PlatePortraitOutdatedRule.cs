using PlateFixer.Rules.Model;

namespace PlateFixer.Rules.Rules;

/// <summary>
/// The main cause of reverts: the plate portrait was taken in gear that no longer matches
/// what the character is wearing, so the game swaps in the default portrait.
/// </summary>
public sealed class PlatePortraitOutdatedRule : IPlateRule
{
    public string Id => "plate-portrait-outdated";

    public string Description => "Plate portrait no longer matches current gear or glamour.";

    public IEnumerable<PlateIssue> Check(PlateSnapshot snapshot)
    {
        if (snapshot.PlatePortrait is { IsOutdated: true })
        {
            yield return new PlateIssue(Id, IssueSeverity.Error,
                "Your plate portrait was taken in different gear or glamour than you're wearing now, so the game will reset it to the default portrait.")
            {
                SuggestedFix = new FixAction(FixKind.RecapturePlatePortrait,
                    "Retake the plate portrait in your current gear, keeping the same pose, camera and background."),
            };
        }
    }
}
