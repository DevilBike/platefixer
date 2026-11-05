using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace PlateFixer.Game;

/// <summary>One gear set that has a portrait linked to it, and what that portrait should look like now.</summary>
public sealed record GearsetPortrait(
    int GearsetIndex,
    string Name,
    bool IsCurrent,
    bool HasGlamourPlate,
    uint SavedChecksum,
    uint? ExpectedChecksum,
    bool? AppearanceMatches,
    string? CannotCheckReason)
{
    public bool GearMismatch => ExpectedChecksum is { } expected && expected != SavedChecksum;

    public bool NeedsFix => ExpectedChecksum is not null && (GearMismatch || AppearanceMatches == false);
}

/// <summary>
/// Reads every gear set's portrait and works out whether the game would treat it as out of date.
///
/// For the gear set you're wearing, the game's own checksum of your equipped gear is used. For the others,
/// the checksum is calculated from the gear set (and its glamour plate) with the game's checksum function.
/// That calculation is only trusted after it has matched the game on your current gear set on this game version.
/// </summary>
public static unsafe class PortraitScanner
{
    private const int MaxGearsets = 100;

    // Glamour plate slots, in order, mapped to gear set item slots. Plates have no belt slot.
    private static readonly int[] PlateSlotToGearsetSlot = [0, 1, 2, 3, 4, 6, 7, 8, 9, 10, 11, 12];

    public static List<GearsetPortrait> Scan(Configuration config, string gameVersion, out string? calibrationNote)
    {
        var result = new List<GearsetPortrait>();
        calibrationNote = null;

        var gearsets = RaptureGearsetModule.Instance();
        var player = (Character*)Control.GetLocalPlayer();
        if (gearsets == null || player == null || BannerModule.Instance() == null)
            return result;

        var current = gearsets->CurrentGearsetIndex;
        var equippedChecksum = UIGlobals.GenerateEquippedItemsChecksum();

        calibrationNote = Calibrate(config, gameVersion, gearsets, current, equippedChecksum);

        for (var i = 0; i < MaxGearsets; i++)
        {
            if (!gearsets->IsValidGearset(i))
                continue;

            var gearset = gearsets->GetGearset(i);
            if (gearset == null || gearset->BannerIndex == 0)
                continue;

            var banner = gearset->GetBanner();
            if (banner == null)
                continue;

            var hasPlate = gearset->GlamourSetLink > 0;
            uint? expected = null;
            string? reason = null;

            if (i == current)
            {
                expected = equippedChecksum;
            }
            else if ((hasPlate ? config.ChecksumVerifiedGlamourPlateVersion : config.ChecksumVerifiedPlainVersion) != gameVersion)
            {
                reason = hasPlate
                    ? "switch to a gear set that uses a glamour plate once, so Plate Fixer can confirm it reads glamour plates the same way the game does."
                    : "switch to a gear set without a glamour plate once, so Plate Fixer can confirm it works out gear the same way the game does.";
            }
            else
            {
                expected = CalculateChecksum(gearset, out reason);
            }

            result.Add(new GearsetPortrait(
                i,
                gearset->NameString,
                i == current,
                hasPlate,
                banner->Checksum,
                expected,
                AppearanceMatches(banner, player),
                reason));
        }

        return result;
    }

    /// <summary>
    /// Checks the plugin's checksum maths against the game's on the gear set you're wearing.
    /// Only counts when that gear set's portrait already matches what you're wearing, which means the gear set
    /// and your equipped gear agree.
    /// </summary>
    private static string? Calibrate(Configuration config, string gameVersion, RaptureGearsetModule* gearsets, int current, uint equippedChecksum)
    {
        if (!gearsets->IsValidGearset(current))
            return null;

        var gearset = gearsets->GetGearset(current);
        if (gearset == null)
            return null;

        var calculated = CalculateChecksum(gearset, out _);
        if (calculated is null)
            return null;

        var hasPlate = gearset->GlamourSetLink > 0;
        var banner = gearset->GetBanner();
        var inSync = banner != null && banner->Checksum == equippedChecksum;

        if (calculated == equippedChecksum)
        {
            if (hasPlate)
                config.ChecksumVerifiedGlamourPlateVersion = gameVersion;
            else
                config.ChecksumVerifiedPlainVersion = gameVersion;
            config.Save();
            return null;
        }

        if (inSync)
        {
            // The game says this gear set's portrait is fine, but our maths disagrees: our maths is wrong.
            if (hasPlate)
                config.ChecksumVerifiedGlamourPlateVersion = null;
            else
                config.ChecksumVerifiedPlainVersion = null;
            config.Save();
            Plugin.Log.Warning($"Checksum maths disagrees with the game (calculated {calculated:X8}, game {equippedChecksum:X8}). Other gear sets won't be checked.");
            return "Plate Fixer's gear maths didn't match the game on your current gear set, so only the gear set you're wearing can be checked.";
        }

        return null;
    }

    private static uint? CalculateChecksum(RaptureGearsetModule.GearsetEntry* gearset, out string? reason)
    {
        reason = null;
        var items = stackalloc uint[14];
        var stains = stackalloc byte[28];
        var glasses = stackalloc ushort[2];

        for (var slot = 0; slot < 14; slot++)
        {
            ref var item = ref gearset->Items[slot];
            var id = item.GlamourId != 0 ? item.GlamourId : item.ItemId;
            items[slot] = id % 1_000_000; // strip the high quality marker
            stains[slot] = item.Stain0Id;
            stains[14 + slot] = item.Stain1Id;
        }

        if (gearset->GlamourSetLink > 0)
        {
            var mirage = MirageManager.Instance();
            if (mirage == null || !mirage->GlamourPlatesLoaded)
            {
                reason = "its glamour plate isn't loaded yet. Open the glamour dresser once this session, then check again.";
                return null;
            }

            ref var plate = ref mirage->GlamourPlates[gearset->GlamourSetLink - 1];
            for (var p = 0; p < PlateSlotToGearsetSlot.Length; p++)
            {
                if (plate.ItemIds[p] == 0)
                    continue;
                var slot = PlateSlotToGearsetSlot[p];
                items[slot] = plate.ItemIds[p] % 1_000_000;
                stains[slot] = plate.Stain0Ids[p];
                stains[14 + slot] = plate.Stain1Ids[p];
            }
        }

        glasses[0] = gearset->GlassesIds[0];
        glasses[1] = gearset->GlassesIds[1];

        var flags = BannerGearVisibilityFlag.None;
        if (!gearset->Flags.HasFlag(RaptureGearsetModule.GearsetFlag.HeadgearVisible))
            flags |= BannerGearVisibilityFlag.HeadgearHidden;
        if (!gearset->Flags.HasFlag(RaptureGearsetModule.GearsetFlag.WeaponsVisible))
            flags |= BannerGearVisibilityFlag.WeaponHidden;
        if (gearset->Flags.HasFlag(RaptureGearsetModule.GearsetFlag.VisorEnabled))
            flags |= BannerGearVisibilityFlag.VisorClosed;

        return BannerModuleEntry.GenerateChecksum(items, stains, glasses, flags);
    }

    private static bool AppearanceMatches(BannerModuleEntry* banner, Character* player)
    {
        ref var customize = ref player->DrawData.CustomizeData;
        return banner->Race == customize.Race
               && banner->Sex == customize.Sex
               && banner->Height == customize.Height
               && banner->Tribe == customize.Tribe;
    }
}
