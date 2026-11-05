using PlateFixer.Rules;
using PlateFixer.Rules.Model;

namespace PlateFixer.Rules.Tests;

public class PlateEditsTests
{
    private static readonly PlateSnapshot Broken = new()
    {
        CharacterName = "Test Character",
        PlateHidden = true,
        PlateShowsDefaultPortrait = true,
        PlatePortrait = new PortraitInfo { SavedGearChecksum = 1, CurrentGearChecksum = 2 },
        DesignItems = [new DesignItem { Kind = DesignItemKind.Frame, RowId = 12, IsUnlocked = false }],
        GearsetPortraits = [new PortraitInfo { GearsetIndex = 2, SavedGearChecksum = 5, CurrentGearChecksum = 6 }],
    };

    [Fact]
    public void EditsThatFixEverythingLeaveNoIssues()
    {
        var edits = new PlateEdits
        {
            DesignItems = [new DesignItem { Kind = DesignItemKind.Frame, RowId = 1, IsUnlocked = true }],
            PlateHidden = false,
            RecapturePlatePortrait = true,
            RecaptureGearsetPortraits = [2],
        };

        var after = edits.ApplyTo(Broken);

        Assert.Empty(new PlateChecker().Check(after).Issues);
        Assert.Equal(12u, Broken.DesignItems[0].RowId);
    }

    [Fact]
    public void EmptyEditsChangeNothing()
    {
        var edits = new PlateEdits();

        Assert.True(edits.IsEmpty);
        Assert.Equal(new PlateChecker().Check(Broken).Issues.Count,
                     new PlateChecker().Check(edits.ApplyTo(Broken)).Issues.Count);
    }

    [Fact]
    public void EditsRoundTripThroughJson()
    {
        var edits = new PlateEdits
        {
            CharacterName = "Test Character",
            PlateHidden = false,
            RecaptureGearsetPortraits = [2, 4],
        };

        var back = SnapshotJson.DeserializeEdits(SnapshotJson.SerializeEdits(edits));

        Assert.Equal(edits.CharacterName, back.CharacterName);
        Assert.Equal(edits.PlateHidden, back.PlateHidden);
        Assert.Equal([2, 4], back.RecaptureGearsetPortraits);
    }
}
