using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace ProductionSummary
{
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "starvalor.productionsummary";
        public const string Name = "Production Summary";
        public const string Version = "1.1.0";

        internal static ManualLogSource Log;
        internal static ConfigEntry<bool> IncludeUndiscovered;
        internal static ConfigEntry<bool> IncludeIdleModules;
        internal static ConfigEntry<float> RefreshSeconds;

        private void Awake()
        {
            Log = Logger;
            IncludeUndiscovered = Config.Bind("General", "IncludeUndiscoveredBases", false,
                "List bases in the sector that the player has not discovered yet.");
            IncludeIdleModules = Config.Bind("General", "IncludeIdleModules", false,
                "Also list production modules that have no product selected.");
            RefreshSeconds = Config.Bind("General", "RefreshSeconds", 1f,
                "How often the Production tab refreshes while it is open.");

            var harmony = new Harmony(Guid);
            harmony.PatchAll(typeof(DockingUIPatches));
            harmony.PatchAll(typeof(LangPatches));
            Log.LogInfo($"{Name} {Version} loaded");
        }
    }
}
