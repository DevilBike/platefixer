using System;
using System.IO;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using PlateFixer.Game;
using PlateFixer.Rules;
using PlateFixer.Rules.Model;
using PlateFixer.Windows;

namespace PlateFixer;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private const string CommandName = "/platefix";

    public Configuration Configuration { get; }

    public readonly WindowSystem WindowSystem = new("PlateFixer");
    private readonly MainWindow mainWindow;
    private readonly ConfigWindow configWindow;
    private readonly IPlateSource plateSource = new GamePlateSource();

    /// <summary>Result of the last check, or null if there hasn't been one.</summary>
    public CheckReport? LastReport { get; private set; }

    /// <summary>Why the last check couldn't run, if it couldn't.</summary>
    public string? LastCheckProblem { get; private set; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        mainWindow = new MainWindow(this);
        configWindow = new ConfigWindow(this);
        WindowSystem.AddWindow(mainWindow);
        WindowSystem.AddWindow(configWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open Plate Fixer. \"/platefix check\" checks your plate without opening the window.",
        });

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        ClientState.Login += OnLogin;
    }

    public void Dispose()
    {
        ClientState.Login -= OnLogin;
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        WindowSystem.RemoveAllWindows();
        CommandManager.RemoveHandler(CommandName);
    }

    /// <summary>Reads the plate and runs the rules. Never changes anything in the game.</summary>
    public void RunCheck()
    {
        try
        {
            var snapshot = plateSource.TryRead(out var reason);
            if (snapshot is null)
            {
                LastCheckProblem = reason ?? "Couldn't read the plate.";
                return;
            }

            LastCheckProblem = null;
            LastReport = new PlateChecker(disabledRuleIds: Configuration.DisabledRules).Check(snapshot);
            Log.Information($"Plate check: {LastReport.Issues.Count} issue(s), valid={LastReport.IsValid}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Plate check failed");
            LastCheckProblem = $"The check failed: {ex.Message}";
        }
    }

    /// <summary>Writes the last snapshot to the plugin's config folder, for the optional outside viewer.</summary>
    public string? ExportLastSnapshot()
    {
        if (LastReport is null)
            return null;

        var dir = PluginInterface.GetPluginConfigDirectory();
        var path = Path.Combine(dir, $"plate-snapshot-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        File.WriteAllText(path, SnapshotJson.Serialize(LastReport.Snapshot));
        return path;
    }

    private void OnLogin()
    {
        if (!Configuration.CheckOnLogin)
            return;

        RunCheck();
        if (LastReport is { IsValid: false })
            ChatGui.Print("[Plate Fixer] Your adventurer plate has problems the game may reset. Type /platefix to see them.");
    }

    private void OnCommand(string command, string args)
    {
        if (args.Trim().Equals("check", StringComparison.OrdinalIgnoreCase))
        {
            RunCheck();
            ChatGui.Print(LastReport is null
                ? $"[Plate Fixer] {LastCheckProblem}"
                : LastReport.IsValid && LastReport.Issues.Count == 0
                    ? "[Plate Fixer] Your plate looks fine."
                    : $"[Plate Fixer] Found {LastReport.Issues.Count} issue(s). Type /platefix to see them.");
            return;
        }

        mainWindow.Toggle();
    }

    public void ToggleMainUi() => mainWindow.Toggle();
    public void ToggleConfigUi() => configWindow.Toggle();
}
