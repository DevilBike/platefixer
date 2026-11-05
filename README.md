# Plate Fixer

A Dalamud plugin for FINAL FANTASY XIV that checks your adventurer plate for things that make the game
reset it to the default portrait, and fixes them after showing you a preview. Personal use, not published.

The usual cause of a reset is that the plate portrait was taken in gear or glamour you're no longer
wearing. The game keeps a checksum of the gear (items, dyes, glasses, visor, hat and weapon display)
with each portrait, and when it stops matching, the portrait goes back to the default.

## What's here

| Folder | What it is |
| --- | --- |
| `PlateFixer/` | The Dalamud plugin. Window, `/platefix` command, settings. Reads the plate and runs the checks. |
| `PlateFixer.Rules/` | The checking rules, as a plain .NET library with no Dalamud or game dependency. The plugin fills in a `PlateSnapshot` and `PlateChecker` returns the issues and a fix plan. A separate viewer can load an exported snapshot and run the same rules. |
| `PlateFixer.Rules.Tests/` | Tests for the rules. These run anywhere, no game needed. |

### Status

- [x] Plugin skeleton: window, command, settings, check-on-login, snapshot export
- [x] Rules: outdated plate portrait, default portrait showing, locked design items, outdated gear set portraits, plate hidden
- [x] Gear set portraits: reads every gear set's portrait, flags ones saved in other gear/glamour or on a different body type, and fixes them all on a button press
- [ ] Reading the adventurer plate itself (its portrait and design items)
- [ ] Optional automatic mode

### How the portrait fix works

Duties show the portrait linked to the gear set you're wearing. The game keeps a checksum of the gear each portrait
was saved in, plus your race, clan, gender and height, and shows the default portrait when they don't match.

**Check plate** compares every gear set's portrait with that gear set. For the gear set you're wearing it uses the
game's own checksum of your equipped gear. For the others it calculates the checksum from the gear set (and its
glamour plate) with the game's checksum function, but only after that calculation has matched the game on the gear
set you're wearing, on the current game version. Until then the window says which gear sets it couldn't check and why.
Gear sets linked to a glamour plate need the glamour dresser opened once per session so the plate can be read.

**Apply fix** updates the stored checksum and body type of each flagged portrait, the same thing the game does when
you save in the portrait editor. Pose, camera, lighting, background and frame stay as they were. The fix is refused
during a duty, while loading, and while the portrait editor is open.

## Building

You need:

- Windows with XIVLauncher, the game and Dalamud installed, and the game run with Dalamud at least once.
  This creates the Dalamud dev folder at `%AppData%\XIVLauncher\addon\Hooks\dev`, which the build uses.
  If yours is somewhere else, set the `DALAMUD_HOME` environment variable to that folder.
- The [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). Visual Studio 2022 17.14+ or JetBrains Rider also work.

Then from the repo folder:

```
dotnet build -c Release
```

The plugin ends up at `PlateFixer\bin\x64\Release\PlateFixer.dll` (use `Debug` instead of `Release` for a debug build).
`PlateFixer\bin\x64\Release\PlateFixer\latest.zip` is the same thing packaged.

To run only the rules tests:

```
dotnet test PlateFixer.Rules.Tests
```

Every push to `main` also builds the plugin and runs the tests on GitHub Actions. The built plugin is
attached to each run as an artifact named `PlateFixer`.

## Loading it in game as a dev plugin

1. Start the game through XIVLauncher.
2. Type `/xlsettings`, open the **Experimental** tab, and under **Dev Plugin Locations** add the full path
   to `PlateFixer.dll`, for example `C:\code\PlateFixer\PlateFixer\bin\x64\Release\PlateFixer.dll`. Press the
   **+**, then **Save and Close**.
3. Type `/xlplugins`, go to **Dev Tools > Installed Dev Plugins**, and enable **Plate Fixer**.
4. Type `/platefix` to open the window, or `/platefix check` to check without opening it.

You only add the path once. After you rebuild, the plugin reloads by itself if **Auto-reload** is turned on
for it in the dev plugin list. Otherwise disable and enable it again. Logs are in `/xllog`.

## Using it

- **Check plate** reads your plate and lists anything the game would reset or reject. Nothing is changed.
- **Preview fix** shows exactly what the fix would do, in order, and **Apply fix** does it.
- **Export snapshot** saves what was checked as JSON in the plugin's config folder, for the optional viewer.
- **Settings** lets you turn individual checks off, and turn off the check when you log in.

## License

AGPL-3.0, carried over from the Dalamud [SamplePlugin](https://github.com/goatcorp/SamplePlugin) template this started from.
