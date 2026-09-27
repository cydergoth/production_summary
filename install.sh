#!/usr/bin/env bash
# Builds the plugin and copies it into the game's BepInEx plugins folder.
set -euo pipefail
GAME_DIR="${GAME_DIR:-$HOME/.local/share/Steam/steamapps/common/Star Valor}"
cd "$(dirname "$0")"
dotnet build -c Release -p:GameDir="$GAME_DIR"
if [ ! -d "$GAME_DIR/BepInEx" ]; then
  echo "BepInEx is not installed in $GAME_DIR - see README.md" >&2
  exit 1
fi
mkdir -p "$GAME_DIR/BepInEx/plugins/ProductionSummary"
cp bin/Release/ProductionSummary.dll "$GAME_DIR/BepInEx/plugins/ProductionSummary/"
echo "Installed to $GAME_DIR/BepInEx/plugins/ProductionSummary/"
