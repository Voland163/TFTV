using HarmonyLib;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;

namespace TFTV.VanillaPatches
{
    /// <summary>
    /// Harmony patch that fixes the vanilla throw range calculation.
    /// The attenuation tag allows Harmony to find the targeted class/object method and apply the patch from the following class.
    /// </summary>
    [HarmonyPatch(typeof(Weapon), "GetThrowingRange")]
    internal class Weapon_GetThrowingRange_Fix
    {
        /// Using Postfix patch to be guaranteed to get executed.
        public static void Postfix(ref float __result, Weapon __instance, float rangeMultiplier)
        {
            try
            {
                float num = __instance.TacticalActor.CharacterStats.Endurance * __instance.TacticalActor.TacticalActorDef.EnduranceToThrowMultiplier;
                float num2 = __instance.TacticalActor.CharacterStats.BonusAttackRange.CalcModValueBasedOn(num);
                // MadSkunky: Extension of calculation with range multiplier divided by 12 for normalization and multiplier from configuration.
                num *= __instance.GetDamagePayload().Range / 12f;
                float multiplier = 1f; // (Main.Config as GrenadeThrowRangeFixConfig).ThrowRangeMultiplier / 100f;
                __result = ((num / __instance.Weight * rangeMultiplier) + num2) * multiplier;
                // End of changes
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
    }
}
