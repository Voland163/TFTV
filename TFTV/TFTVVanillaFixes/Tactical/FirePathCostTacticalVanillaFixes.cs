using HarmonyLib;
using PhoenixPoint.Tactical.Levels;
using System;

namespace TFTV.TFTVVanillaFixes.Tactical
{
    /// <summary>
    /// Bounds how far a unit will walk to keep off a burning tile.
    ///
    /// TacticalNavCostFactorFuncs prices a fire tile at a multiple of a normal tile equal to
    /// the fire's damage number - Fire_DamageEffectDef, which TFTV raises to 50 for the
    /// Mephistopheles module. That multiplier is exactly the detour the pathfinder is willing
    /// to make: it takes the way round whenever the detour is shorter than 50 tiles, which is
    /// where the absurd walkabouts around a single burning tile come from.
    ///
    /// The cap below stays far above any unit's movement budget - the fastest are well under
    /// 25 tiles in a turn - so fire remains as impassable as it was, while the worst-case
    /// detour halves and pathing no longer moves whenever the fire damage number is retuned.
    /// </summary>
    internal static class FirePathCostTacticalVanillaFixes
    {
        internal const float MaxFirePathCostFactor = 25f;

        [HarmonyPatch(typeof(TacticalNavCostFactorFuncs), nameof(TacticalNavCostFactorFuncs.CostFactorFunc))]
        internal static class TacticalNavCostFactorFuncs_CostFactorFunc_Patch
        {
            private static void Postfix(ref float __result)
            {
                if (__result > MaxFirePathCostFactor)
                {
                    __result = MaxFirePathCostFactor;
                }
            }
        }

        /// <summary>
        /// The search budget is the path length multiplied by this, so it has to move with the
        /// cap above or the pathfinder keeps exploring routes it can no longer price.
        /// </summary>
        [HarmonyPatch(typeof(TacticalNavCostFactorFuncs), nameof(TacticalNavCostFactorFuncs.MaxCostFactorFunc))]
        internal static class TacticalNavCostFactorFuncs_MaxCostFactorFunc_Patch
        {
            private static void Postfix(ref float __result)
            {
                if (__result > MaxFirePathCostFactor)
                {
                    __result = MaxFirePathCostFactor;
                }
            }
        }
    }
}
