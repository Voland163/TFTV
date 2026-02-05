using HarmonyLib;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Entities.PhoenixBases.FacilityComponents;
using PhoenixPoint.Geoscape.Entities.Sites;
using System;
using System.Collections.Generic;
using System.Linq;
using TFTV.TFTVBaseRework;

internal class LivingQuarters
{
    [HarmonyPatch]
    internal static class LivingQuartersReworkPatches
    {
        private static readonly AccessTools.FieldRef<PhoenixBaseStats, GeoPhoenixBaseLayout> LayoutField =
            AccessTools.FieldRefAccess<PhoenixBaseStats, GeoPhoenixBaseLayout>("_layout");

        private static readonly AccessTools.FieldRef<PhoenixBaseStats, int> HealSoldiersHPField =
            AccessTools.FieldRefAccess<PhoenixBaseStats, int>("<HealSoldiersHP>k__BackingField");

        private static readonly AccessTools.FieldRef<PhoenixBaseStats, int> HealSoldiersStaminaField =
            AccessTools.FieldRefAccess<PhoenixBaseStats, int>("<HealSoldiersStamina>k__BackingField");

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