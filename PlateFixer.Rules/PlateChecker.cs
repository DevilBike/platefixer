using PlateFixer.Rules.Model;
using PlateFixer.Rules.Rules;

namespace PlateFixer.Rules;

/// <summary>Runs every enabled rule against a snapshot and collects the issues.</summary>
public sealed class PlateChecker
{
    public static IReadOnlyList<IPlateRule> DefaultRules { get; } =
    [
        new PlatePortraitOutdatedRule(),
        new DefaultPortraitRule(),
        new LockedDesignItemRule(),
        new GearsetPortraitOutdatedRule(),
        new PlateHiddenRule(),
    ];

    private readonly IReadOnlyList<IPlateRule> rules;

    public PlateChecker(IEnumerable<IPlateRule>? rules = null, IEnumerable<string>? disabledRuleIds = null)
    {
        var disabled = new HashSet<string>(disabledRuleIds ?? [], StringComparer.OrdinalIgnoreCase);
        this.rules = (rules ?? DefaultRules).Where(r => !disabled.Contains(r.Id)).ToList();
    }

    public IReadOnlyList<IPlateRule> Rules => rules;

    public CheckReport Check(PlateSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var issues = rules.SelectMany(r => r.Check(snapshot))
                          .OrderByDescending(i => i.Severity)
                          .ToList();
        return new CheckReport(snapshot, issues);
    }
}
