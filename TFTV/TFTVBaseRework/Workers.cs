using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Entities.PhoenixBases.FacilityComponents;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewModules;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace TFTV.TFTVBaseRework
{
    internal static class Workers
    {
        private static readonly MethodInfo UpdateResourceInfoMethod =
            AccessTools.Method(typeof(UIModuleInfoBar), "UpdateResourceInfo");

        [HarmonyPatch(typeof(GeoPhoenixFaction), nameof(GeoPhoenixFaction.UpdateBasesHourly))]
        internal static class GeoPhoenixFaction_UpdateBasesHourly_Patch
        {
            private static void Postfix(GeoPhoenixFaction __instance)
            {
                if (!BaseReworkCheck.BaseReworkEnabled)
                {
                    return;
                }

                ResearchManufacturingSlotsManager.RecalculateSlots(__instance);
                PersonnelData.TryAutoAssignUnassignedPersonnel(__instance, "UpdateBasesHourly");
            }
        }

        [HarmonyPatch(typeof(GeoFactionFacilityBuffCollection), nameof(GeoFactionFacilityBuffCollection.GetValue))]
        internal static class GeoFactionFacilityBuffCollection_GetValue_Patch
        {
            private static bool Prefix(PhoenixFacilityDef facility, GeoFacilityComponentDef component, float baseValue, float addedValue, float multiplier, ref float __result)
            {
                if (!BaseReworkCheck.BaseReworkEnabled || component == null)
                {
                    return true;
                }

                if (!IsResearchOrManufacturingComponent(component))
                {
                    return true;
                }

                __result = baseValue * multiplier + addedValue;
                return false;
            }
        }

        [HarmonyPatch(typeof(UIModuleInfoBar), "UpdateResourceInfo")]
        internal static class UIModuleInfoBar_UpdateResourceInfo_Patch
        {
            public static void Postfix(UIModuleInfoBar __instance, GeoFaction faction)
            {
                if (!BaseReworkCheck.BaseReworkEnabled)
                {
                    return;
                }

                GeoPhoenixFaction phoenix = faction as GeoPhoenixFaction;
                if (phoenix == null)
                {
                    return;
                }

                FacilitySlotPools slotPools = ResearchManufacturingSlotsManager.RecalculateSlots(phoenix);

                float totalProduction = faction.ResourceIncome.GetTotalResouce(ResourceType.Production).Value;
                float totalResearch = faction.ResourceIncome.GetTotalResouce(ResourceType.Research).Value;

                ResearchAndManufacturing.GetOutputBonuses(phoenix, out float researchBonus, out float productionBonus);

                __instance.ProductionLabel.text = FormatSlotString(Mathf.RoundToInt(totalProduction), Mathf.RoundToInt(productionBonus), slotPools.Manufacturing);
                __instance.ResearchLabel.text = FormatSlotString(Mathf.RoundToInt(totalResearch), Mathf.RoundToInt(researchBonus), slotPools.Research);
            }

            private static string FormatSlotString(int totalPerHour, int bonus, FacilitySlotPool pool)
            {
                int provided = pool.ProvidedSlots;
                int used = provided > 0 ? pool.UsedSlots : 0;

                if (bonus > 0)
                {
                    return $"{totalPerHour} (+{bonus}) \n({used}/{provided})";
                }

                return $"{totalPerHour} \n({used}/{provided})";
            }
        }

        private static bool IsResearchOrManufacturingComponent(GeoFacilityComponentDef component)
        {
            ResourceGeneratorFacilityComponentDef generator = component as ResourceGeneratorFacilityComponentDef;
            if (generator == null)
            {
                return false;
            }

            return generator.BaseResourcesOutput.ByResourceType(ResourceType.Research).Value > 0f
                || generator.BaseResourcesOutput.ByResourceType(ResourceType.Production).Value > 0f;
        }

        internal enum FacilitySlotType
        {
            Research,
            Manufacturing
        }

        internal sealed class FacilitySlotPool
        {
            public int ProvidedSlots { get; private set; }
            public int UsedSlots { get; private set; }

            internal void SetProvidedSlots(int value)
            {
                ProvidedSlots = Math.Max(0, value);
                UsedSlots = ProvidedSlots == 0
                    ? 0
                    : Math.Min(UsedSlots, ProvidedSlots);
            }

            internal void SetUsedSlots(int value)
            {
                if (ProvidedSlots == 0)
                {
                    UsedSlots = 0;
                    return;
                }

                UsedSlots = Math.Max(0, Math.Min(value, ProvidedSlots));
            }

            internal bool TryIncrement()
            {
                if (UsedSlots >= ProvidedSlots)
                {
                    return false;
                }

                UsedSlots++;
                return true;
            }

            internal bool TryDecrement()
            {
                if (UsedSlots <= 0)
                {
                    return false;
                }

                UsedSlots--;
                return true;
            }
        }

        internal sealed class FacilitySlotPools
        {
            public FacilitySlotPool Research { get; } = new FacilitySlotPool();
            public FacilitySlotPool Manufacturing { get; } = new FacilitySlotPool();
        }

        internal static class ResearchManufacturingSlotsManager
        {
            public const int SlotsPerFacility = 1;

            private static readonly ConditionalWeakTable<GeoPhoenixFaction, FacilitySlotPools> Pools =
                new ConditionalWeakTable<GeoPhoenixFaction, FacilitySlotPools>();

            public static FacilitySlotPools GetOrCreatePools(GeoPhoenixFaction faction)
            {
                if (faction == null)
                {
                    throw new ArgumentNullException(nameof(faction));
                }

                return Pools.GetValue(faction, _ => new FacilitySlotPools());
            }

            public static FacilitySlotPools RecalculateSlots(GeoPhoenixFaction faction)
            {
                FacilitySlotPools pools = GetOrCreatePools(faction);
                int researchProviders = 0;
                int manufacturingProviders = 0;

                foreach (GeoPhoenixBase geoBase in faction.Bases)
                {
                    if (geoBase?.Layout == null)
                    {
                        continue;
                    }

                    foreach (GeoPhoenixFacility facility in geoBase.Layout.Facilities)
                    {
                        if (facility == null || !facility.IsWorking || facility.Def?.GeoFacilityComponentDefs == null)
                        {
                            continue;
                        }

                        bool providesResearch = false;
                        bool providesManufacturing = false;

                        foreach (GeoFacilityComponentDef component in facility.Def.GeoFacilityComponentDefs)
                        {
                            ResourceGeneratorFacilityComponentDef generator = component as ResourceGeneratorFacilityComponentDef;
                            if (generator == null)
                            {
                                continue;
                            }

                            if (!providesResearch && generator.BaseResourcesOutput.ByResourceType(ResourceType.Research).Value > 0f)
                            {
                                providesResearch = true;
                            }

                            if (!providesManufacturing && generator.BaseResourcesOutput.ByResourceType(ResourceType.Production).Value > 0f)
                            {
                                providesManufacturing = true;
                            }

                            if (providesResearch && providesManufacturing)
                            {
                                break;
                            }
                        }

                        if (providesResearch)
                        {
                            researchProviders++;
                        }

                        if (providesManufacturing)
                        {
                            manufacturingProviders++;
                        }
                    }
                }

                pools.Research.SetProvidedSlots(researchProviders * SlotsPerFacility);
                pools.Manufacturing.SetProvidedSlots(manufacturingProviders * SlotsPerFacility);
                return pools;
            }

            public static bool IncrementUsedSlot(GeoPhoenixFaction faction, FacilitySlotType type)
            {
                FacilitySlotPools pools = GetOrCreatePools(faction);
                bool changed = false;

                switch (type)
                {
                    case FacilitySlotType.Research:
                        changed = pools.Research.TryIncrement();
                        break;

                    case FacilitySlotType.Manufacturing:
                        changed = pools.Manufacturing.TryIncrement();
                        break;
                }

                if (changed)
                {
                    TryUpdateInfoBar(faction);
                }

                return changed;
            }

            public static bool DecrementUsedSlot(GeoPhoenixFaction faction, FacilitySlotType type)
            {
                FacilitySlotPools pools = GetOrCreatePools(faction);
                bool changed = false;

                switch (type)
                {
                    case FacilitySlotType.Research:
                        changed = pools.Research.TryDecrement();
                        break;

                    case FacilitySlotType.Manufacturing:
                        changed = pools.Manufacturing.TryDecrement();
                        break;
                }

                if (changed)
                {
                    TryUpdateInfoBar(faction);
                }

                return changed;
            }

            public static void SetUsedSlots(GeoPhoenixFaction faction, FacilitySlotType slotType, int usedSlots)
            {
                FacilitySlotPools pools = GetOrCreatePools(faction);

                switch (slotType)
                {
                    case FacilitySlotType.Research:
                        pools.Research.SetUsedSlots(usedSlots);
                        break;

                    case FacilitySlotType.Manufacturing:
                        pools.Manufacturing.SetUsedSlots(usedSlots);
                        break;
                }

                TryUpdateInfoBar(faction);
            }
        }

        internal static void RefreshInfoBar(GeoPhoenixFaction faction)
        {
            TryUpdateInfoBar(faction);
        }

        private static void TryUpdateInfoBar(GeoPhoenixFaction faction)
        {
            if (faction == null)
            {
                return;
            }

            MethodInfo updateProductionMethod = AccessTools.Method(typeof(GeoFaction), "UpdateProduction");

        //TFTVLogger.Always($"[Workers] TryUpdateInfoBar called for faction {faction.Name}. UpdateProduction method found: {updateProductionMethod != null}");

            updateProductionMethod.Invoke(faction, new object[] { });

            GeoLevelController level = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
            if (level == null)
            {
                return;
            }

            UIModuleInfoBar infoBar = level.View?.GeoscapeModules?.ResourcesModule;
            if (infoBar == null)
            {
                if (!_pendingInfoBarLog)
                {
                    TFTVLogger.Always("[Workers] InfoBar not ready; skipping UpdateResourceInfo (first occurrence).");
                    _pendingInfoBarLog = true;
                }

                TFTVLogger.Always($"[Workers] Queuing pending info bar update. Pending={_pendingInfoBarUpdate} Faction={faction?.Name}");
                _pendingInfoBarUpdate = true;
                _pendingFaction = faction;
                return;
            }

            _pendingInfoBarLog = false;
            _pendingInfoBarUpdate = false;
            _pendingFaction = null;

            try
            {
                FacilitySlotPools pools = ResearchManufacturingSlotsManager.GetOrCreatePools(faction);
                TFTVLogger.Always($"[Workers] Updating info bar with Research Used/Provided={pools.Research.UsedSlots}/{pools.Research.ProvidedSlots} Manufacturing Used/Provided={pools.Manufacturing.UsedSlots}/{pools.Manufacturing.ProvidedSlots}");

                UpdateResourceInfoMethod?.Invoke(infoBar, new object[] { faction, false });
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void FlushPendingInfoBarUpdate(GeoLevelController level)
        {
            TFTVLogger.Always($"[Workers] FlushPendingInfoBarUpdate called. Pending={_pendingInfoBarUpdate} LevelReady={(level != null)}");
            if (!_pendingInfoBarUpdate)
            {
                return;
            }

            GeoPhoenixFaction faction = _pendingFaction ?? level?.PhoenixFaction;
            if (faction == null)
            {
                TFTVLogger.Always("[Workers] Pending info bar update skipped: no faction available.");
                return;
            }

            TryUpdateInfoBar(faction);
        }

        private static bool _pendingInfoBarLog;
        private static bool _pendingInfoBarUpdate;
        private static GeoPhoenixFaction _pendingFaction;
    }
}
