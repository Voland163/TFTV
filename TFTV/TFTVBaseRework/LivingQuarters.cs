using HarmonyLib;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Entities.PhoenixBases.FacilityComponents;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFTV.TFTVBaseRework
{
    internal class LivingQuarters
    {
        

        [HarmonyPatch]
        internal static class LivingQuartersReworkPatches
        {
            private static readonly AccessTools.FieldRef<GeoPhoenixFaction, int> SoldierCapacityField =
                AccessTools.FieldRefAccess<GeoPhoenixFaction, int>("<SoldierCapacity>k__BackingField");

            private static readonly AccessTools.FieldRef<GeoPhoenixFaction, bool> LivingQuarterFullField =
                AccessTools.FieldRefAccess<GeoPhoenixFaction, bool>("<LivingQuarterFull>k__BackingField");

            private static readonly Action<GeoPhoenixFaction> EvaluateSoldiersState =
      GetEvaluateSoldiersStateMethod();

            private static Action<GeoPhoenixFaction> GetEvaluateSoldiersStateMethod()
            {
                var method = AccessTools.Method(typeof(GeoPhoenixFaction), "EvaluateSoldiersState");
                if (method != null)
                {
                    return (Action<GeoPhoenixFaction>)Delegate.CreateDelegate(
                        typeof(Action<GeoPhoenixFaction>), null, method);
                }
                return null;
            }


            private static readonly AccessTools.FieldRef<PhoenixBaseStats, GeoPhoenixBaseLayout> LayoutField =
                AccessTools.FieldRefAccess<PhoenixBaseStats, GeoPhoenixBaseLayout>("_layout");

            private static readonly AccessTools.FieldRef<PhoenixBaseStats, int> HealSoldiersHPField =
                AccessTools.FieldRefAccess<PhoenixBaseStats, int>("<HealSoldiersHP>k__BackingField");

            private static readonly AccessTools.FieldRef<PhoenixBaseStats, int> HealSoldiersStaminaField =
                AccessTools.FieldRefAccess<PhoenixBaseStats, int>("<HealSoldiersStamina>k__BackingField");

            private static readonly AccessTools.FieldRef<UIModuleInfoBar, GeoscapeViewContext> InfoBarContextField =
                AccessTools.FieldRefAccess<UIModuleInfoBar, GeoscapeViewContext>("_context");

            [HarmonyPostfix]
            [HarmonyPatch(typeof(GeoPhoenixFaction), "UpdateStats")]
            private static void GeoPhoenixFaction_UpdateStats_Postfix(GeoPhoenixFaction __instance)
            {
                if (!BaseReworkUtils.BaseReworkEnabled)
                {
                    return;
                }

                SoldierCapacityField(__instance) = int.MaxValue;
                if (EvaluateSoldiersState != null)
                {
                    EvaluateSoldiersState(__instance);
                }
                else
                {
                    LivingQuarterFullField(__instance) = false;
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(PhoenixBaseStats), "Update")]
            private static void PhoenixBaseStats_Update_Postfix(PhoenixBaseStats __instance)
            {
                if (!BaseReworkUtils.BaseReworkEnabled)
                {
                    return;
                }

                GeoPhoenixBaseLayout layout = LayoutField(__instance);
                if (layout == null)
                {
                    return;
                }

                List<HealFacilityComponent> healComponents = layout
                    .QueryFacilitiesWithComponent<HealFacilityComponent>(onlyWorking: true)
                    .Where(component => component.HealSoldier)
                    .ToList();

                if (healComponents.Count == 0)
                {
                    return;
                }

                List<HealFacilityComponent> livingQuarters = healComponents
                    .Where(IsLivingQuartersComponent)
                    .ToList();

                List<HealFacilityComponent> medicalFacilities = healComponents
                    .Where(component => !IsLivingQuartersComponent(component))
                    .ToList();

                if (livingQuarters.Count > 1)
                {
                    int totalStamina = livingQuarters.Sum(c => (int)c.StaminaHealOutput);
                    int maxStamina = livingQuarters.Max(c => (int)c.StaminaHealOutput);
                    int reduction = totalStamina - maxStamina;
                    if (reduction > 0)
                    {
                        int current = HealSoldiersStaminaField(__instance);
                        HealSoldiersStaminaField(__instance) = Math.Max(0, current - reduction);
                    }
                }

                AdjustMedicalHealing(__instance, medicalFacilities);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(UIModuleInfoBar), "UpdateSoldierData")]
            private static void UIModuleInfoBar_UpdateSoldierData_Postfix(UIModuleInfoBar __instance)
            {
                if (!BaseReworkUtils.BaseReworkEnabled)
                {
                    return;
                }

                GeoscapeViewContext context = InfoBarContextField(__instance);
                if (context?.ViewerFaction is GeoPhoenixFaction geoPhoenixFaction && __instance.SoldiersLabel != null)
                {
                    int soldierCount = geoPhoenixFaction.Soldiers.Count();
                    __instance.SoldiersLabel.text = soldierCount.ToString();
                }
            }

            private static bool IsLivingQuartersComponent(HealFacilityComponent component)
            {
                if (component == null)
                {
                    return false;
                }

                GeoPhoenixFacility facility = component.Facility;
                if (facility == null)
                {
                    return false;
                }

                ContainerFacilityComponent container = facility.GetComponent<ContainerFacilityComponent>();
                return container != null && container.SoldiersCapacity > 0;
            }

            private static void AdjustMedicalHealing(PhoenixBaseStats stats, List<HealFacilityComponent> medicalFacilities)
            {
                if (medicalFacilities == null || medicalFacilities.Count <= 1)
                {
                    return;
                }

                int totalHp = medicalFacilities.Sum(component => (int)component.HealOutput);
                int maxHp = medicalFacilities.Max(component => (int)component.HealOutput);
                int reduction = totalHp - maxHp;
                if (reduction <= 0)
                {
                    return;
                }

                int current = HealSoldiersHPField(stats);
                HealSoldiersHPField(stats) = Math.Max(0, current - reduction);
            }
        }
    }
}
