using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace PlateFixer.Game;

/// <summary>
/// Updates out of date gear set portraits the same way the game's portrait editor does when you save:
/// the stored gear checksum and body type are set to match the gear set and your character. Pose, camera,
/// lighting, background and frame are left alone.
/// </summary>
public static unsafe class PortraitFixer
{
    /// <summary>Returns why fixing isn't possible right now, or null if it is.</summary>
    public static string? WhyNotNow()
    {
        var c = Plugin.Condition;
        if (c[ConditionFlag.BoundByDuty] || c[ConditionFlag.BoundByDuty56] || c[ConditionFlag.BoundByDuty95])
            return "Leave the duty first.";
        if (c[ConditionFlag.BetweenAreas] || c[ConditionFlag.BetweenAreas51])
            return "Wait until you've finished loading.";
        if (c[ConditionFlag.EditingPortrait])
            return "Close the portrait editor first.";
        if (Control.GetLocalPlayer() == null)
            return "Log in first.";
        return null;
    }

    /// <summary>Fixes the given portraits and returns how many were updated.</summary>
    public static int Fix(IEnumerable<GearsetPortrait> portraits)
    {
        if (WhyNotNow() is { } reason)
            throw new InvalidOperationException(reason);

        var player = (Character*)Control.GetLocalPlayer();
        var gearsets = RaptureGearsetModule.Instance();
        var helper = UIModule.Instance()->GetUIModuleHelpers()->BannerHelper;
        var now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var fixedCount = 0;

        foreach (var target in portraits.Where(p => p.NeedsFix))
        {
            var gearset = gearsets->GetGearset(target.GearsetIndex);
            var banner = gearset == null ? null : gearset->GetBanner();
            if (banner == null || banner->Checksum != target.SavedChecksum)
            {
                Plugin.Log.Warning($"Skipped gear set {target.GearsetIndex + 1}: it changed since it was checked.");
                continue;
            }

            banner->Checksum = target.ExpectedChecksum!.Value;
            banner->LastUpdated = now;
            helper->BannerModuleEntry_ApplyRaceGenderHeightTribe(banner, player);
            fixedCount++;

            if (target.IsCurrent)
                SendCurrentPortrait(helper, banner);
        }

        if (fixedCount > 0)
            BannerModule.Instance()->UserFileEvent.HasChanges = true; // the game saves it to UISAVE.DAT

        return fixedCount;
    }

    /// <summary>Tells the server about the portrait for the gear set you're wearing, which is the one duties show.</summary>
    private static void SendCurrentPortrait(BannerHelper* helper, BannerModuleEntry* banner)
    {
        if (!helper->BannerModuleEntry_IsCurrentCharaCardBannerOutdated(banner, true)
            || !helper->BannerModuleEntry_IsCharacterDataOutdated(banner, true))
        {
            Plugin.Log.Warning("Didn't send the current portrait: the game says it can't be sent right now.");
            return;
        }

        var data = new BannerData();
        helper->BannerData_ApplyBannerModuleEntry(&data, banner);
        if (!helper->SendBannerData(&data))
            Plugin.Log.Warning("Sending the current portrait failed.");
    }
}
