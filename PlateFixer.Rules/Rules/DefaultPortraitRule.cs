using PlateFixer.Rules.Model;

namespace PlateFixer.Rules.Rules;

/// <summary>The plate is already showing the default portrait, which usually means it was reverted.</summary>
public sealed class DefaultPortraitRule : IPlateRule
{
    public string Id => "plate-default-portrait";

    public string Description => "Plate is showing the default portrait.";

    public IEnumerable<PlateIssue> Check(PlateSnapshot snapshot)
    {
        if (snapshot.PlateShowsDefaultPortrait)
        {
            yield return new PlateIssue(Id, IssueSeverity.Warning,
                "Your plate is showing the default portrait. It was probably reset after a gear or glamour change.")
            {
                SuggestedFix = new FixAction(FixKind.RecapturePlatePortrait,
                    "Retake the plate portrait in your current gear, keeping the same pose, camera and background."),
            };
        }
    }
}
