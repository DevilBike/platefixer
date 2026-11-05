using PlateFixer.Rules;
using PlateFixer.Rules.Model;

namespace PlateFixer.Rules.Tests;

public class PlateCheckerTests
{
    private static readonly PlateChecker Checker = new();

    [Fact]
    public void CleanPlateHasNoIssues()
    {
        var snapshot = new PlateSnapshot
        {
            PlatePortrait = new PortraitInfo { SavedGearChecksum = 42, CurrentGearChecksum = 42 },
            DesignItems = [new DesignItem { Kind = DesignItemKind.Background, RowId = 5, IsUnlocked = true }],
        };

        var report = Checker.Check(snapshot);

        Assert.Empty(report.Issues);
        Assert.True(report.IsValid);
        Assert.False(report.HasFixes);
    }

    [Fact]
    public void GearMismatchIsAnErrorWithRecaptureFix()
    {
        var snapshot = new PlateSnapshot
        {
            PlatePortrait = new PortraitInfo { SavedGearChecksum = 1, CurrentGearChecksum = 2 },
        };

        var report = Checker.Check(snapshot);

        var issue = Assert.Single(report.Issues);
        Assert.Equal("plate-portrait-outdated", issue.RuleId);
        Assert.Equal(IssueSeverity.Error, issue.Severity);
        Assert.False(report.IsValid);
        Assert.Equal(FixKind.RecapturePlatePortrait, Assert.Single(report.FixPlan).Kind);
    }

    [Fact]
    public void UnknownCurrentChecksumIsNotFlagged()
    {
        var snapshot = new PlateSnapshot
        {
            PlatePortrait = new PortraitInfo { SavedGearChecksum = 1, CurrentGearChecksum = null },
        };

        Assert.Empty(Checker.Check(snapshot).Issues);
    }

    [Fact]
    public void RevertedAndOutdatedPortraitGivesOneFixInPlan()
    {
        var snapshot = new PlateSnapshot
        {
            PlateShowsDefaultPortrait = true,
            PlatePortrait = new PortraitInfo { SavedGearChecksum = 1, CurrentGearChecksum = 2 },
        };

        var report = Checker.Check(snapshot);

        Assert.Equal(2, report.Issues.Count);
        Assert.Single(report.FixPlan);
    }

    [Fact]
    public void LockedDesignItemIsAnError()
    {
        var snapshot = new PlateSnapshot
        {
            DesignItems =
            [
                new DesignItem { Kind = DesignItemKind.Frame, RowId = 12, Name = "Moogle Frame", IsUnlocked = false },
                new DesignItem { Kind = DesignItemKind.Accent, RowId = 0, IsUnlocked = false },
                new DesignItem { Kind = DesignItemKind.Backing, RowId = 3, IsUnlocked = null },
            ],
        };

        var report = Checker.Check(snapshot);

        var issue = Assert.Single(report.Issues);
        Assert.Contains("Moogle Frame", issue.Message);
        Assert.Equal(DesignItemKind.Frame, issue.SuggestedFix?.DesignItemKind);
    }

    [Fact]
    public void OutdatedGearsetPortraitsAreWarnings()
    {
        var snapshot = new PlateSnapshot
        {
            GearsetPortraits =
            [
                new PortraitInfo { GearsetIndex = 0, GearsetName = "PLD", SavedGearChecksum = 1, CurrentGearChecksum = 1 },
                new PortraitInfo { GearsetIndex = 3, GearsetName = "WHM", SavedGearChecksum = 1, CurrentGearChecksum = 9 },
            ],
        };

        var report = Checker.Check(snapshot);

        var issue = Assert.Single(report.Issues);
        Assert.Equal(IssueSeverity.Warning, issue.Severity);
        Assert.Equal(3, issue.SuggestedFix?.GearsetIndex);
        Assert.True(report.IsValid);
    }

    [Fact]
    public void DisabledRulesAreSkipped()
    {
        var checker = new PlateChecker(disabledRuleIds: ["plate-hidden"]);

        var report = checker.Check(new PlateSnapshot { PlateHidden = true });

        Assert.Empty(report.Issues);
    }

    [Fact]
    public void IssuesAreSortedErrorsFirst()
    {
        var snapshot = new PlateSnapshot
        {
            PlateHidden = true,
            PlatePortrait = new PortraitInfo { SavedGearChecksum = 1, CurrentGearChecksum = 2 },
        };

        var report = Checker.Check(snapshot);

        Assert.Equal(IssueSeverity.Error, report.Issues[0].Severity);
        Assert.Equal(IssueSeverity.Info, report.Issues[^1].Severity);
    }

    [Fact]
    public void SnapshotRoundTripsThroughJson()
    {
        var snapshot = new PlateSnapshot
        {
            CharacterName = "Test Character",
            CurrentClassJobId = 19,
            PlatePortrait = new PortraitInfo { SavedGearChecksum = 1, CurrentGearChecksum = 2 },
            DesignItems = [new DesignItem { Kind = DesignItemKind.Frame, RowId = 12, IsUnlocked = false }],
        };

        var json = SnapshotJson.Serialize(snapshot);
        var back = SnapshotJson.Deserialize(json);

        Assert.Contains("\"frame\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(snapshot.CharacterName, back.CharacterName);
        Assert.Equal(snapshot.PlatePortrait, back.PlatePortrait);
        Assert.Equal(Checker.Check(snapshot).Issues.Count, Checker.Check(back).Issues.Count);
    }
}
