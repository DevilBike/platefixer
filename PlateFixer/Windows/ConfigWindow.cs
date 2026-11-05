using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using PlateFixer.Rules;

namespace PlateFixer.Windows;

public class ConfigWindow : Window
{
    private readonly Configuration configuration;

    public ConfigWindow(Plugin plugin) : base("Plate Fixer Settings###PlateFixerConfig")
    {
        configuration = plugin.Configuration;
        Size = new Vector2(380, 260);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        var checkOnLogin = configuration.CheckOnLogin;
        if (ImGui.Checkbox("Check my plate when I log in", ref checkOnLogin))
        {
            configuration.CheckOnLogin = checkOnLogin;
            configuration.Save();
        }

        ImGui.Spacing();
        ImGui.TextUnformatted("Checks to run:");
        foreach (var rule in PlateChecker.DefaultRules)
        {
            var enabled = !configuration.DisabledRules.Contains(rule.Id);
            if (ImGui.Checkbox($"{rule.Description}##{rule.Id}", ref enabled))
            {
                if (enabled)
                    configuration.DisabledRules.Remove(rule.Id);
                else
                    configuration.DisabledRules.Add(rule.Id);
                configuration.Save();
            }
        }
    }
}
