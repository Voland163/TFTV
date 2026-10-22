using HarmonyLib;
using System;
using UnityTools.AnvilCheats;

namespace TFTV.TFTVVanillaFixes
{
    /// <summary>
    /// With mods loaded, vanilla's ModManager.Initialize clears GameConsoleWindow.DisableConsoleAccess, which also
    /// unlocks the developers' "Anvil Cheats" panel. AnvilCheatsUI.Update opens that panel whenever
    /// JoystickButton9 - the right stick press on a pad - goes down, so every R3 zoom-out in tactical also popped
    /// the cheat menu. That is why setting the flag in autoexec.cfg or the game files did not help: mod loading
    /// resets it.
    ///
    /// R3 no longer opens the panel. It still opens with the console command toggle_anvil_cheats, and once it is
    /// open vanilla's Update runs as before (R3 closes it).
    /// </summary>
    internal static class AnvilCheatsPadVanillaFixes
    {
        [HarmonyPatch(typeof(AnvilCheatsUI), "Update")]
        internal static class AnvilCheatsUI_Update_NoPadToggle_Patch
        {
            // While closed, vanilla's Update does nothing but check the pad button to open the panel.
            private static bool Prefix(AnvilCheatsUI __instance)
            {
                try
                {
                    return __instance.Active;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }
            }
        }
    }
}
