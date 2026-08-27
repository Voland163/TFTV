using Base.Core;
using HarmonyLib;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Entities.PhoenixBases.FacilityComponents;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

        // Tracks last known SoldierCapacity per faction to detect drops.
        private static readonly ConditionalWeakTable<GeoPhoenixFaction, CapacityTracker> _capacityTrackers =
            new ConditionalWeakTable<GeoPhoenixFaction, CapacityTracker>();

        private sealed class CapacityTracker
        {
            public int LastCapacity = -1;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PhoenixBaseStats), "Update")]
        private static void PhoenixBaseStats_Update_Postfix(PhoenixBaseStats __instance)
        {
            if (!BaseReworkCheck.BaseReworkEnabled)
            {
                return;
            }

            GeoPhoenixBaseLayout layout = LayoutField(__instance);
            if (layout == null)
            {
                return;
            }

            // ── Living capacity change detection ─────────────────────────────
            GeoPhoenixFaction faction = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction;
            if (faction != null)
            {
                CapacityTracker tracker = _capacityTrackers.GetOrCreateValue(faction);
                int currentCapacity = faction.SoldierCapacity;

                if (tracker.LastCapacity != currentCapacity)
                {
                    bool capacityDropped = tracker.LastCapacity > 0 && currentCapacity < tracker.LastCapacity;
                    tracker.LastCapacity = currentCapacity;

                    if (capacityDropped)
                    {
                        PersonnelData.EnforceLivingCapacity(faction);
                        Workers.RefreshInfoBar(faction);
                    }
                }
            }

            // ── Heal / stamina rework ────────────────────────────────────────
            // Single pass: the previous version built three intermediate lists and evaluated
            // IsLivingQuartersComponent (a Unity GetComponent) twice per facility. This runs on
            // every base and roster change, so it is worth keeping allocation-free.
            int livingQuartersCount = 0;
            int livingQuartersStaminaTotal = 0;
            int livingQuartersStaminaMax = 0;

            int medicalCount = 0;
            int medicalHpTotal = 0;
            int medicalHpMax = 0;

            foreach (HealFacilityComponent component in layout.QueryFacilitiesWithComponent<HealFacilityComponent>(onlyWorking: true))
            {
                if (component == null || !component.HealSoldier)
                {
                    continue;
                }

                if (IsLivingQuartersComponent(component))
                {
                    int stamina = (int)component.StaminaHealOutput;
                    livingQuartersCount++;
                    livingQuartersStaminaTotal += stamina;
                    if (stamina > livingQuartersStaminaMax)
                    {
                        livingQuartersStaminaMax = stamina;
                    }
                }
                else
                {
                    int hp = (int)component.HealOutput;
                    medicalCount++;
                    medicalHpTotal += hp;
                    if (hp > medicalHpMax)
                    {
                        medicalHpMax = hp;
                    }
                }
            }

            if (livingQuartersCount > 1)
            {
                int reduction = livingQuartersStaminaTotal - livingQuartersStaminaMax;
                if (reduction > 0)
                {
                    int current = HealSoldiersStaminaField(__instance);
                    HealSoldiersStaminaField(__instance) = Math.Max(0, current - reduction);
                }
            }

            if (medicalCount > 1)
            {
                int reduction = medicalHpTotal - medicalHpMax;
                if (reduction > 0)
                {
                    int current = HealSoldiersHPField(__instance);
                    HealSoldiersHPField(__instance) = Math.Max(0, current - reduction);
                }
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
    }
}