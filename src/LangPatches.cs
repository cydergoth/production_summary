using HarmonyLib;

namespace ProductionSummary
{
    /// <summary>Redraws the Production tab when the player changes the game language.</summary>
    [HarmonyPatch(typeof(Lang))]
    internal static class LangPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Lang.SetLanguageTo))]
        private static void SetLanguageTo_Postfix()
        {
            ProductionTab.Instance?.RefreshLabels();
        }
    }
}
