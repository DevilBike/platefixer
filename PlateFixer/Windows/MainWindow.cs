using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using PlateFixer.Rules.Model;

namespace PlateFixer.Windows;

public class MainWindow : Window
{
    private readonly Plugin plugin;
    private bool showFixPreview;
    private string? exportMessage;
    private string? fixMessage;

    public MainWindow(Plugin plugin) : base("Plate Fixer###PlateFixerMain")
    {
        this.plugin = plugin;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(420, 260),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    public override void OnOpen()
    {
        plugin.RunCheck();
        showFixPreview = false;
    }

    public override void Draw()
    {
        if (ImGui.Button("Check plate"))
        {
            plugin.RunCheck();
            showFixPreview = false;
            exportMessage = null;
            fixMessage = null;
        }

        var report = plugin.LastReport;

        ImGui.SameLine();
        using (ImRaii.Disabled(report is not { HasFixes: true }))
        {
            if (ImGui.Button("Preview fix"))
                showFixPreview = true;
        }

        ImGui.SameLine();
        using (ImRaii.Disabled(report is null))
        {
            if (ImGui.Button("Export snapshot"))
            {
                var path = plugin.ExportLastSnapshot();
                exportMessage = path is null ? "Nothing to export yet." : $"Saved to {path}";
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Settings"))
            plugin.ToggleConfigUi();

        if (exportMessage is not null)
            ImGui.TextWrapped(exportMessage);
        if (fixMessage is not null)
            ImGui.TextColored(ImGuiColors.HealerGreen, fixMessage);
        if (plugin.CalibrationNote is { } note)
            ImGui.TextColored(ImGuiColors.DalamudYellow, note);

        ImGui.Separator();

        if (plugin.LastCheckProblem is { } problem)
        {
            ImGui.TextColored(ImGuiColors.DalamudYellow, problem);
            return;
        }

        if (report is null)
        {
            ImGui.TextUnformatted("Press \"Check plate\" to look for problems.");
            return;
        }

        if (report.Issues.Count == 0)
        {
            ImGui.TextColored(ImGuiColors.HealerGreen, "Your plate looks fine. Nothing the game would reset.");
            return;
        }

        foreach (var issue in report.Issues)
        {
            ImGui.TextColored(ColorFor(issue.Severity), $"[{issue.Severity}]");
            ImGui.SameLine();
            ImGui.TextWrapped(issue.Message);
        }

        if (showFixPreview)
            DrawFixPreview(report);
    }

    private void DrawFixPreview(CheckReport report)
    {
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.TextUnformatted("The fix would do this:");
        var plan = report.FixPlan;
        for (var i = 0; i < plan.Count; i++)
        {
            var supported = Plugin.CanApply(plan[i].Kind);
            ImGui.BulletText($"{i + 1}. {plan[i].Description}{(supported ? "" : " (not supported yet, skipped)")}");
        }

        ImGui.Spacing();
        var whyNot = Game.PortraitFixer.WhyNotNow();
        using (ImRaii.Disabled(whyNot is not null || !plan.Any(f => Plugin.CanApply(f.Kind))))
        {
            if (ImGui.Button("Apply fix"))
            {
                fixMessage = plugin.ApplyFixes();
                showFixPreview = false;
            }
        }

        if (whyNot is not null)
        {
            ImGui.SameLine();
            ImGui.TextDisabled(whyNot);
        }

        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
            showFixPreview = false;
    }

    private static Vector4 ColorFor(IssueSeverity severity) => severity switch
    {
        IssueSeverity.Error => ImGuiColors.DalamudRed,
        IssueSeverity.Warning => ImGuiColors.DalamudOrange,
        _ => ImGuiColors.DalamudGrey,
    };
}
