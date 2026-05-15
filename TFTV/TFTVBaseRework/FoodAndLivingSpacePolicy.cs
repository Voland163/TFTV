using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static TFTV.TFTVBaseRework.BaseActivation;

namespace TFTV.TFTVBaseRework
{
    internal enum FoodAndLivingSpaceMode
    {
        VanillaFatigueCharacters,
        FieldOperativesAndAssignedPersonnel,
        FieldOperativesAndAssignedPersonnelPlusBaseOverhead
    }

    internal sealed class FoodAndLivingSpaceBreakdown
    {
        public int CharacterConsumers;
        public int OutpostOverhead;
        public int ActivatedBaseOverhead;

        public int TotalFoodConsumptionPerDay => CharacterConsumers + OutpostOverhead + ActivatedBaseOverhead;
        public int TotalLivingSpaceUsed => CharacterConsumers + OutpostOverhead + ActivatedBaseOverhead;
    }

    internal static class FoodAndLivingSpacePolicy
    {
        internal const int OutpostFoodAndLivingSpaceCost = 1;
        internal const int ActivatedBaseFoodAndLivingSpaceCost = 4;

        // Change this in code when testing different rules.
        internal static readonly FoodAndLivingSpaceMode CurrentMode =
            FoodAndLivingSpaceMode.FieldOperativesAndAssignedPersonnel;

        internal static IEnumerable<GeoCharacter> GetFoodConsumerCharacters(GeoPhoenixFaction faction)
        {
            if (faction == null)
            {
                return Enumerable.Empty<GeoCharacter>();
            }

            switch (CurrentMode)
            {
                case FoodAndLivingSpaceMode.VanillaFatigueCharacters:
                    return faction.Characters.Where(HasFatigue);

                case FoodAndLivingSpaceMode.FieldOperativesAndAssignedPersonnel:
                case FoodAndLivingSpaceMode.FieldOperativesAndAssignedPersonnelPlusBaseOverhead:
                    return faction.Characters.Where(ShouldCountAsBaseReworkCharacterConsumer);

                default:
                    return faction.Characters.Where(HasFatigue);
            }
        }

        internal static FoodAndLivingSpaceBreakdown GetBreakdown(GeoPhoenixFaction faction)
        {
            FoodAndLivingSpaceBreakdown breakdown = new FoodAndLivingSpaceBreakdown
            {
                CharacterConsumers = GetFoodConsumerCharacters(faction).Count()
            };

            if (CurrentMode == FoodAndLivingSpaceMode.FieldOperativesAndAssignedPersonnelPlusBaseOverhead)
            {
                breakdown.OutpostOverhead = CountOutposts(faction) * OutpostFoodAndLivingSpaceCost;
                breakdown.ActivatedBaseOverhead = CountActivatedBases(faction) * ActivatedBaseFoodAndLivingSpaceCost;
            }

            return breakdown;
        }

        internal static int GetTotalFoodConsumptionPerDay(GeoPhoenixFaction faction)
        {
            return GetBreakdown(faction).TotalFoodConsumptionPerDay;
        }

        internal static int GetTotalLivingSpaceUsed(GeoPhoenixFaction faction)
        {
            return GetBreakdown(faction).TotalLivingSpaceUsed;
        }

        internal static int GetFixedFoodOverheadPerDay(GeoPhoenixFaction faction)
        {
            FoodAndLivingSpaceBreakdown breakdown = GetBreakdown(faction);
            return breakdown.OutpostOverhead + breakdown.ActivatedBaseOverhead;
        }

        internal static string GetBreakdownForLog(GeoPhoenixFaction faction)
        {
            FoodAndLivingSpaceBreakdown breakdown = GetBreakdown(faction);
            return $"mode={CurrentMode} characters={breakdown.CharacterConsumers} outposts={breakdown.OutpostOverhead} activatedBases={breakdown.ActivatedBaseOverhead} total={breakdown.TotalFoodConsumptionPerDay}";
        }

        private static bool HasFatigue(GeoCharacter character)
        {
            return character?.Fatigue != null;
        }

        private static bool ShouldCountAsBaseReworkCharacterConsumer(GeoCharacter character)
        {
            if (!HasFatigue(character))
            {
                return false;
            }

            if (!GeoCharacterFilter.HiddenOperativeMarkerFilter.ShouldHide(character))
            {
                return true;
            }

            PersonnelInfo personnel = PersonnelData.GetPersonnelByUnitId(character.Id);
            if (personnel == null)
            {
                return false;
            }

            switch (personnel.Assignment)
            {
                case PersonnelAssignment.Research:
                case PersonnelAssignment.Manufacturing:
                case PersonnelAssignment.Training:
                    return true;

                default:
                    return false;
            }
        }

        private static int CountOutposts(GeoPhoenixFaction faction)
        {
            return faction?.Bases?.Count(IsOutpostBase) ?? 0;
        }

        private static int CountActivatedBases(GeoPhoenixFaction faction)
        {
            return faction?.Bases?.Count(IsActivatedBase) ?? 0;
        }

        private static bool IsOutpostBase(GeoPhoenixBase phoenixBase)
        {
            GeoSite site = phoenixBase?.Site;
            return site != null
                && site.State == GeoSiteState.Functioning
                && site.SiteTags.Contains(PhoenixBaseReworkState.OutpostTag);
        }

        private static bool IsActivatedBase(GeoPhoenixBase phoenixBase)
        {
            GeoSite site = phoenixBase?.Site;
            return site != null
                && site.State == GeoSiteState.Functioning
                && !site.SiteTags.Contains(PhoenixBaseReworkState.OutpostTag);
        }

        [HarmonyPatch(typeof(GeoPhoenixFaction), "UpdateFeeding")]
        internal static class GeoPhoenixFaction_UpdateFeeding_Patch
        {
            private static readonly MethodInfo EvaluateSoldiersStateMethod =
                AccessTools.Method(typeof(GeoPhoenixFaction), "EvaluateSoldiersState");

            private static readonly MethodInfo OnIncomeChangedMethod =
                AccessTools.Method(typeof(GeoFaction), "OnIncomeChanged");

            private static bool Prefix(GeoPhoenixFaction __instance)
            {
                if (!BaseReworkCheck.BaseReworkEnabled)
                {
                    return true;
                }

                try
                {
                    int foodConsumptionPerDay = GetTotalFoodConsumptionPerDay(__instance);

                    __instance.ResourceIncome.SetOutput(
                        OperationReason.Maintenance,
                        new ResourceUnit(ResourceType.Supplies, -foodConsumptionPerDay) / 24f);

                    EvaluateSoldiersStateMethod?.Invoke(__instance, null);
                    OnIncomeChangedMethod?.Invoke(__instance, null);

                    TFTVLogger.Always($"[FoodAndLivingSpacePolicy] UpdateFeeding {GetBreakdownForLog(__instance)}");
                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(GeoPhoenixFaction), "FeedSoldiers")]
        internal static class GeoPhoenixFaction_FeedSoldiers_Patch
        {
            private static bool Prefix(GeoPhoenixFaction __instance, int totalFood)
            {
                if (!BaseReworkCheck.BaseReworkEnabled)
                {
                    return true;
                }

                try
                {
                    List<GeoCharacter> consumers = GetFoodConsumerCharacters(__instance).ToList();
                    int availableFoodForCharacters = totalFood - GetFixedFoodOverheadPerDay(__instance);

                    foreach (GeoCharacter character in consumers)
                    {
                        if (availableFoodForCharacters > 0)
                        {
                            character.Fatigue.AddHunger(-1);
                            availableFoodForCharacters--;
                        }
                        else
                        {
                            character.Fatigue.AddHunger(1);
                        }
                    }

                    foreach (GeoCharacter starvedCharacter in consumers.Where(c => !c.IsAlive).ToArray())
                    {
                        __instance.KillCharacter(starvedCharacter, CharacterDeathReason.Starvation);
                    }

                    TFTVLogger.Always(
                        $"[FoodAndLivingSpacePolicy] FeedSoldiers totalFood={totalFood} fixedOverhead={GetFixedFoodOverheadPerDay(__instance)} " +
                        $"characterConsumers={consumers.Count} availableForCharacters={Math.Max(0, availableFoodForCharacters)}");

                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(GeoPhoenixFaction), "EvaluateSoldiersState")]
        internal static class GeoPhoenixFaction_EvaluateSoldiersState_Patch
        {
            private static readonly AccessTools.FieldRef<GeoPhoenixFaction, bool> LowOnFoodField =
                AccessTools.FieldRefAccess<GeoPhoenixFaction, bool>("<LowOnFood>k__BackingField");

            private static readonly AccessTools.FieldRef<GeoPhoenixFaction, bool> LivingQuarterFullField =
                AccessTools.FieldRefAccess<GeoPhoenixFaction, bool>("<LivingQuarterFull>k__BackingField");

            private static readonly AccessTools.FieldRef<GeoPhoenixFaction, Action<bool>> LowOnFoodChangedField =
                AccessTools.FieldRefAccess<GeoPhoenixFaction, Action<bool>>("LowOnFoodChanged");

            private static readonly AccessTools.FieldRef<GeoPhoenixFaction, Action<bool>> LivingQuarterFullChangedField =
                AccessTools.FieldRefAccess<GeoPhoenixFaction, Action<bool>>("LivingQuarterFullChanged");

            private static bool Prefix(GeoPhoenixFaction __instance)
            {
                if (!BaseReworkCheck.BaseReworkEnabled)
                {
                    return true;
                }

                try
                {
                    int foodConsumptionPerDay = GetTotalFoodConsumptionPerDay(__instance);
                    int livingSpaceUsed = GetTotalLivingSpaceUsed(__instance);

                    bool lowOnFood = __instance.Wallet[ResourceType.Supplies].RoundedValue < foodConsumptionPerDay;
                    if (lowOnFood != LowOnFoodField(__instance))
                    {
                        LowOnFoodField(__instance) = lowOnFood;
                        LowOnFoodChangedField(__instance)?.Invoke(lowOnFood);
                    }

                    bool livingQuarterFull = __instance.SoldierCapacity <= livingSpaceUsed;
                    if (livingQuarterFull != LivingQuarterFullField(__instance))
                    {
                        LivingQuarterFullField(__instance) = livingQuarterFull;
                        LivingQuarterFullChangedField(__instance)?.Invoke(livingQuarterFull);
                    }

                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(GeoPhoenixFaction), "get_LivingQuarterFreeSpace")]
        internal static class GeoPhoenixFaction_LivingQuarterFreeSpace_Patch
        {
            private static bool Prefix(GeoPhoenixFaction __instance, ref int __result)
            {
                if (!BaseReworkCheck.BaseReworkEnabled)
                {
                    return true;
                }

                __result = __instance.SoldierCapacity - GetTotalLivingSpaceUsed(__instance);
                return false;
            }
        }

        [HarmonyPatch(typeof(GeoPhoenixFaction), nameof(GeoPhoenixFaction.CanRecruitCharacter))]
        internal static class GeoPhoenixFaction_CanRecruitCharacter_Patch
        {
            private static bool Prefix(GeoPhoenixFaction __instance, GeoUnitDescriptor character, ResourcePack cost, ref bool __result)
            {
                if (!BaseReworkCheck.BaseReworkEnabled)
                {
                    return true;
                }

                try
                {
                    if (character != null && character.UnitType.IsHuman)
                    {
                        if (__instance.SoldierCapacity <= GetTotalLivingSpaceUsed(__instance))
                        {
                            __result = false;
                            return false;
                        }
                    }

                    if (character != null && (character.UnitType.IsVehicle || character.UnitType.IsMutog))
                    {
                        if (__instance.GroundVehicleCapacity <= __instance.GroundVehicles.Count())
                        {
                            __result = false;
                            return false;
                        }
                    }

                    __result = __instance.Wallet.HasResources(cost);
                    return false;
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