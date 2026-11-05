using System.Diagnostics;

namespace PlateFixer.Editor;

internal sealed record CharacterFolder(string Path, DateTime LastSaved)
{
    public string Name => System.IO.Path.GetFileName(Path);

    public string UiSavePath => System.IO.Path.Combine(Path, "UISAVE.DAT");

    public override string ToString() => $"{Name}   (last saved {LastSaved:yyyy-MM-dd HH:mm})";
}

internal static class CharacterFolders
{
    public static string DefaultRoot => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "My Games", "FINAL FANTASY XIV - A Realm Reborn");

    public static List<CharacterFolder> Find(string root) =>
        !Directory.Exists(root)
            ? []
            : Directory.GetDirectories(root, "FFXIV_CHR*")
                .Where(d => File.Exists(Path.Combine(d, "UISAVE.DAT")))
                .Select(d => new CharacterFolder(d, File.GetLastWriteTime(Path.Combine(d, "UISAVE.DAT"))))
                .OrderByDescending(f => f.LastSaved)
                .ToList();

    /// <summary>The game rewrites UISAVE.DAT while it runs, so saving is only safe with it closed.</summary>
    public static bool GameIsRunning() =>
        Process.GetProcessesByName("ffxiv_dx11").Length > 0 || Process.GetProcessesByName("ffxiv").Length > 0;
}
