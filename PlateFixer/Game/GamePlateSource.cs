using System.Collections.Generic;
using System.Linq;
using PlateFixer.Rules.Model;

namespace PlateFixer.Game;

/// <summary>
/// Reads the plate from game memory.
///
/// Gear set portraits are read now. The adventurer plate itself (its portrait and design items) is still to come, using
/// CharaCard.Instance()->CurrentCharaCardData and ExdModule.GetBannerConditionUnlockState.
/// </summary>
public sealed class GamePlateSource(Configuration config) : IPlateSource
{
    /// <summary>The gear set portraits from the last read, for the fixer.</summary>
    public IReadOnlyList<GearsetPortrait> LastPortraits { get; private set; } = [];

    /// <summary>Set when the plugin's own checksum maths couldn't be confirmed.</summary>
    public string? CalibrationNote { get; private set; }

    public PlateSnapshot? TryRead(out string? reason)
    {
        if (!Plugin.ClientState.IsLoggedIn)
        {
            reason = "Log in to a character first.";
            return null;
        }

        if (!Plugin.PlayerState.IsLoaded)
        {
            reason = "Your character is still loading. Try again in a moment.";
            return null;
        }

        LastPortraits = PortraitScanner.Scan(config, GameVersion(), out var note);
        CalibrationNote = note;
        reason = null;

        return new PlateSnapshot
        {
            CharacterName = Plugin.PlayerState.CharacterName.ToString(),
            CurrentClassJobId = Plugin.PlayerState.ClassJob.RowId,
            GearsetPortraits = LastPortraits.Select(p => new PortraitInfo
            {
                GearsetIndex = p.GearsetIndex,
                GearsetName = p.Name,
                IsCurrentGearset = p.IsCurrent,
                SavedGearChecksum = p.SavedChecksum,
                CurrentGearChecksum = p.ExpectedChecksum,
                AppearanceMatches = p.ExpectedChecksum is null ? null : p.AppearanceMatches,
                CannotCheckReason = p.CannotCheckReason,
            }).ToList(),
        };
    }

    private static string GameVersion() =>
        Plugin.DataManager.GameData.Repositories.TryGetValue("ffxiv", out var repo) ? repo.Version : "unknown";
}
