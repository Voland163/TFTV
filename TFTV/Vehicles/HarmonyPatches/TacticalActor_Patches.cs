using HarmonyLib;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using System;
using System.Collections.Generic;
using System.Linq;
using TFTVVehicleRework.Misc;
    
namespace TFTVVehicleRework.HarmonyPatches
{    
    [HarmonyPatch(typeof(TacticalActor))]
    internal static class CostModHarmonies
    {
        [HarmonyPostfix]
        [HarmonyPatch("CalcFractActionPointCost", new Type[] {typeof(float), typeof(TacticalAbility), typeof(IEnumerable<TacticalAbilityCostModification>)})]
        public static void RemoveCostModSafeguard(ref float __result, float baseFract, TacticalAbility ability, IEnumerable<TacticalAbilityCostModification> costModifications)
        {
            if (!costModifications.Any<TacticalAbilityCostModification>() || !(ability.AbilityDef.Guid == SoldierMounting.get_ExitVehicleAbility().Guid))
            {
                return;
            }
            foreach(TacticalAbilityCostModification tacticalabilityCostModification in costModifications)
            {
                if (tacticalabilityCostModification.AbilityQualifies(ability))
                {
                    __result = tacticalabilityCostModification.GetActionPointsModValue(baseFract, 0f, 1f);
                }
            }
        }
    }
}