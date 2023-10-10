using HarmonyLib;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;

namespace TFTVVehicleRework.HarmonyPatches
{    
    [HarmonyPatch(typeof(TacticalAbility), "get_ShouldDisplay")]
    internal static class KNR_Patch
    {
        // [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
        public static void Postfix(TacticalAbility __instance, ref bool __result)
        {
            // Check if instance is KnR ability
            if (__instance.TacticalAbilityDef.name.Equals("E_RunAbility [KillNRun_AbilityDef]"))
            {
                //  Set return value __result = true when ability is not disabled => show
                __result = __instance.GetDisabledState() == AbilityDisabledState.NotDisabled;
            }
        }
    }
}