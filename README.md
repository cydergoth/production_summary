# Production Summary — a Star Valor mod

Adds a **Production** tab to the station docking screen, next to Lobby / Trade / Hangar / Crafting.
It shows every base in your current sector that is producing goods, what each base is making, and
whether it has the materials to keep going.

For each base the tab shows:

- the base name, faction, and whether you are docked there, the base is inactive, or it uses another base's storage (cargo link)
- each production module (Fabricator, Workshop, Laboratory, Agroponics, Refinery, Mining), with:
  - the item it produces and how many per cycle
  - its status: **producing** (with progress %), **stalled** because materials are missing, **limit reached**, or **unpowered**
  - cycle time, current stock of the product, and the production limit
- every material needed per cycle and the supply that base can see: its own storage (or the storage it is cargo-linked to), plus your stash for player bases. Green means there is enough for a cycle and red means there isn't. It also shows how many cycles the supply lasts.
- asteroid resources left, for mining modules

The tab refreshes every second while it is open. It only reads game data. It never changes stations, stock or your save.

## Installation

The mod needs **BepInEx 5**, the standard mod loader for Star Valor. If you already use other
Star Valor BepInEx mods, skip to step 2.

### 1. Install BepInEx 5

1. Download **`BepInEx_win_x64_5.4.23.2.zip`** from the
   [BepInEx 5.4.23.2 release](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.2).
   Use the **Windows x64** build of **BepInEx 5**, even on Linux. BepInEx 6 will not work.
2. Open the game folder. In Steam: right-click **Star Valor** → **Manage** → **Browse local files**.
3. Extract the zip into that folder. `winhttp.dll`, `doorstop_config.ini` and the `BepInEx` folder
   must end up **next to `Star Valor.exe`**, not in a subfolder.
4. **Linux / Steam Deck (Proton) only:** in Steam, right-click **Star Valor** → **Properties** →
   **General** → **Launch Options**, and enter:
   ```
   WINEDLLOVERRIDES="winhttp=n,b" %command%
   ```
   Without this, Proton ignores BepInEx and no mods load.
5. Start the game once, then quit. BepInEx creates `BepInEx/plugins`, `BepInEx/config` and
   `BepInEx/LogOutput.log`.

### 2. Install Production Summary

1. Download **`ProductionSummary-<version>.zip`** from the
   [Releases page](https://github.com/cydergoth/production_summary/releases/latest).
2. Extract it into the game folder (the same folder as `Star Valor.exe`). This creates
   `BepInEx/plugins/ProductionSummary/ProductionSummary.dll`.
3. Start the game and dock at any station. A **PRODUCTION** tab appears after **CRAFTING**.

### Checking it worked

`BepInEx/LogOutput.log` should contain:

```
[Info   :Production Summary] Production Summary 1.0.0 loaded
```

If the log file doesn't exist, BepInEx isn't running. Check that the files are next to
`Star Valor.exe`, and on Linux check the launch option. If the line is there but the tab is missing,
please [open an issue](https://github.com/cydergoth/production_summary/issues) and attach the log.

### Updating

Download the new release and extract it over the old one, replacing `ProductionSummary.dll`.

### Uninstalling

- To remove only this mod, delete `BepInEx/plugins/ProductionSummary/`.
- To remove BepInEx too, delete `BepInEx/`, `winhttp.dll`, `doorstop_config.ini` and
  `changelog.txt` from the game folder, and clear the launch option.

The mod saves nothing in your save files, so you can remove it at any time.

## Configuration

`BepInEx/config/starvalor.productionsummary.cfg` is created the first time the game runs with the mod.
Edit it while the game is closed.

| Setting | Default | Meaning |
|---|---|---|
| `IncludeUndiscoveredBases` | `false` | Also list bases you have not discovered yet |
| `IncludeIdleModules` | `false` | Also list production modules with no product selected |
| `RefreshSeconds` | `1` | Refresh interval while the tab is open |

## Compatibility

- Built against Star Valor Steam build 24937123 (Unity 2019.4.41f1) and BepInEx 5.4.23.2.
- Achievements are unaffected. The game has no mod detection, and this mod doesn't touch perks,
  stats or Steam.
- A game update that changes the station docking screen could break the tab. If that happens,
  the error is written to `BepInEx/LogOutput.log` and the rest of the game keeps working.
- Other mods that add buttons to the docking tab bar may crowd it. This mod shrinks the tab buttons
  so five fit.

## Building from source

You need the [.NET SDK](https://dotnet.microsoft.com/download) 6 or newer and a Star Valor
install, because the build references the game's own DLLs. Those DLLs are not included in this
repository.

```bash
./install.sh     # build and copy the DLL into the game's BepInEx/plugins folder
./package.sh     # build and create the release zip in dist/
# or build only:
dotnet build -c Release -p:GameDir="/path/to/Star Valor"
```

`GAME_DIR` (for the scripts) and `-p:GameDir` (for `dotnet build`) default to the standard Linux
Steam location. The project file also checks the default Windows Steam path.

### How it works

Harmony postfixes on the game's `DockingUI` class:

- `Start`: clones the Crafting tab button into the tab bar and builds the panel.
- `ShowHideDockingButtons`: shows the button in services mode and resizes the tab buttons to fit.
- `OpenPanel(5)`: opens the panel. `ClosePanels` closes it, and `SetButtonBackgroundColors`
  highlights the button.

Production data comes from each station's `SM_Fabricator` modules and their subclasses. Supply is
read through `Station.GetItemStock()`, which follows cargo links. These are the same checks the game
uses to decide whether production can start.

## AI disclosure

This mod's code and documentation were written with the AI coding assistant
[Claude Code](https://claude.com/claude-code), directed and reviewed by the author. Commits made
with it carry a `Co-Authored-By: Claude` trailer. If you share the mod on sites that label AI
content, such as Nexus Mods, tag it as AI-generated code.

Star Valor is made by Rafael Burgos. This is an unofficial fan mod, and the developer does not
endorse or support it.

## License

[MIT](LICENSE) © 2026 cydergoth. The license covers this mod's source code only. Star Valor and its
assets belong to their developer, and BepInEx has its own license.
