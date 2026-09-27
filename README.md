# Production Summary (Star Valor mod)

Adds a **Production** tab to the station docking screen, next to Lobby / Trade / Hangar / Crafting.
It lists every known base in the current sector that is producing goods. For each base it shows:

- the base name, faction, and whether you are docked there, the base is inactive, or it uses another base's storage (cargo link)
- each production module (Fabricator, Workshop, Laboratory, Agroponics, Refinery, Mining), with:
  - the item it produces and how many per cycle
  - its status: producing (with progress %), stalled because materials are missing, production limit reached, or unpowered
  - cycle time, current stock of the product, and the production limit
- every material needed per cycle, and the supply that base can see: its own storage (or the storage it is cargo-linked to), plus your stash for player bases. Green means there is enough for a cycle and red means there isn't. It also shows how many cycles the supply lasts.
- asteroid resources left, for mining modules

The tab refreshes every second while it is open.

## Requirements

[BepInEx 5](https://github.com/BepInEx/BepInEx/releases) (x64, Windows build — use it under Proton on Linux too).

1. Extract `BepInEx_win_x64_5.4.x.zip` into the game folder (next to `Star Valor.exe`).
2. **Linux/Proton only:** set the Steam launch options to `WINEDLLOVERRIDES="winhttp=n,b" %command%`.
3. Start the game once so BepInEx creates its folders.

## Build & install

You need the .NET SDK (6 or newer). The game's `Managed` DLLs are referenced from the Steam install folder.

```bash
./install.sh                                  # build + copy to BepInEx/plugins
# or build only:
dotnet build -c Release -p:GameDir="/path/to/Star Valor"
```

The plugin is `bin/Release/ProductionSummary.dll`. Put it in `Star Valor/BepInEx/plugins/`.

## Configuration

`BepInEx/config/starvalor.productionsummary.cfg` is created on first run:

| Setting | Default | Meaning |
|---|---|---|
| `IncludeUndiscoveredBases` | `false` | Also list bases you have not discovered yet |
| `IncludeIdleModules` | `false` | Also list production modules with no product selected |
| `RefreshSeconds` | `1` | Refresh interval while the tab is open |

## How it works

Harmony postfixes on `DockingUI`:

- `Start`: clones the Crafting tab button into `BtnPanel` and builds the panel under the docking UI.
- `ShowHideDockingButtons`: shows the button in services mode, and shrinks the tab buttons so five fit.
- `OpenPanel(5)`: opens the panel. `ClosePanels` closes it, and `SetButtonBackgroundColors` highlights it.

Production data comes from each station's `SM_Fabricator` modules (and subclasses). Supply is read through `Station.GetItemStock()`, which follows cargo links. These are the same checks the game uses to decide whether production can start.
