using PlateFixer.Rules.Model;

namespace PlateFixer.Game;

/// <summary>
/// Reads the plate from game memory.
///
/// Placeholder for now. Step 3 fills this in using FFXIVClientStructs:
///   - CharaCard.Instance()->CurrentCharaCardData for the plate, its design items and the hidden flag
///   - BannerModule entries for gear set portraits and their saved gear checksums
///   - BannerHelper.IsCharacterDataOutdated / the current gear checksum for the comparison
///   - ExdModule.GetBannerConditionUnlockState for design item unlocks (0 = unlocked)
/// </summary>
public sealed class GamePlateSource : IPlateSource
{
    public PlateSnapshot? TryRead(out string? reason)
    {
        if (!Plugin.ClientState.IsLoggedIn)
        {
            reason = "Log in to a character first.";
            return null;
        }

        reason = "Reading the plate from the game isn't built yet. That's the next step.";
        return null;
    }
}
