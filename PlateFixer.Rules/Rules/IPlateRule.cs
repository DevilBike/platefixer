using PlateFixer.Rules.Model;

namespace PlateFixer.Rules.Rules;

public interface IPlateRule
{
    /// <summary>Stable id, used in logs and settings to turn a rule off.</summary>
    string Id { get; }

    string Description { get; }

    IEnumerable<PlateIssue> Check(PlateSnapshot snapshot);
}
