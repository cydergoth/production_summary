using System;
using HarmonyLib;

namespace ProductionSummary
{
    /// <summary>
    /// Hooks the station docking screen so the Production tab behaves like the built-in
    /// Lobby / Trade / Hangar / Crafting tabs.
    /// </summary>
    [HarmonyPatch(typeof(DockingUI))]
    internal static class DockingUIPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch("Start")]
        private static void Start_Postfix(DockingUI __instance)
        {
            try
            {
                ProductionTab.Create(__instance);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Failed to create Production tab: " + e);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(DockingUI.ShowHideDockingButtons))]
        private static void ShowHideDockingButtons_Postfix(int dockingMode)
        {
            ProductionTab.Instance?.OnDockingButtonsChanged(dockingMode == 0);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(DockingUI.OpenPanel))]
        private static void OpenPanel_Postfix(int code)
        {
            if (code == ProductionTab.PanelCode)
            {
                ProductionTab.Instance?.Open();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("ClosePanels")]
        private static void ClosePanels_Postfix()
        {
            ProductionTab.Instance?.Close();
        }

        [HarmonyPostfix]
        [HarmonyPatch("SetButtonBackgroundColors")]
        private static void SetButtonBackgroundColors_Postfix()
        {
            ProductionTab.Instance?.UpdateButtonColor();
        }
    }
}
