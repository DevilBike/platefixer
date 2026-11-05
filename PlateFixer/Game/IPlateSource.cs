using PlateFixer.Rules.Model;

namespace PlateFixer.Game;

/// <summary>Reads the current plate from the game into a snapshot the rules can check.</summary>
public interface IPlateSource
{
    /// <summary>Returns null with a reason when the plate can't be read right now (not logged in, data not loaded yet).</summary>
    PlateSnapshot? TryRead(out string? reason);
}
