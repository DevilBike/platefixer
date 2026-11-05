using System;
using System.Collections.Generic;
using Dalamud.Configuration;

namespace PlateFixer;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    /// <summary>Rule ids to skip, see PlateChecker.DefaultRules.</summary>
    public HashSet<string> DisabledRules { get; set; } = [];

    /// <summary>Check the plate once after logging in. The fix still waits for the button.</summary>
    public bool CheckOnLogin { get; set; } = true;

    /// <summary>
    /// Game version on which the plugin's own gear checksum maths matched the game's, per kind of gear set.
    /// Portraits of gear sets you're not wearing are only checked once this is confirmed for the current game version.
    /// </summary>
    public string? ChecksumVerifiedPlainVersion { get; set; }

    public string? ChecksumVerifiedGlamourPlateVersion { get; set; }

    /// <summary>Reserved for the later automatic mode. Not used yet.</summary>
    public bool AutoFix { get; set; } = false;

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}
