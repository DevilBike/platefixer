using PlateFixer.Rules.Model;

namespace PlateFixer.Rules.Rules;

/// <summary>The plate is hidden from others, which the game does after a Fantasia.</summary>
public sealed class PlateHiddenRule : IPlateRule
{
    public string Id => "plate-hidden";

    public string Description => "Plate is hidden from other players.";

    public IEnumerable<PlateIssue> Check(PlateSnapshot snapshot)
    {
        if (snapshot.PlateHidden)
        {
            yield return new PlateIssue(Id, IssueSeverity.Info,
                "Your plate is hidden from other players. The game does this after a Fantasia.")
            {
                SuggestedFix = new FixAction(FixKind.ShowPlate, "Make the plate visible to others again."),
            };
        }
    }
}
