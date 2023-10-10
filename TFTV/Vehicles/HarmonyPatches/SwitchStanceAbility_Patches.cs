using HarmonyLib;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Statuses;

namespace TFTVVehicleRework.HarmonyPatches
{    
    [HarmonyPatch(typeof(SwitchStanceAbility), "get_ShouldDisplay")]
    internal static class SwitchStance_Patch
    {
        public static void Postfix(SwitchStanceAbility __instance, ref bool __result)
        {
            if(__instance.TacticalActor.Status.GetStatus<StanceStatus>(__instance.SwitchStanceAbilityDef.stanceStatusDef) == null)
            {
                __result = !__instance.SwitchStanceAbilityDef.DisplayInStabilityStance;
                return;
            }
            __result = __instance.SwitchStanceAbilityDef.DisplayInStabilityStance;
        }
    }
}