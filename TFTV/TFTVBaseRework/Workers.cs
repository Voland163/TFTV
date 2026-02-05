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
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace TFTV.TFTVBaseRework
{
    internal static class Workers
    {
        internal const float WorkerOutputPerSlot = 4.0f;
        private const int UnoccupiedResearchPerSlot = 1;
        private const int UnoccupiedProductionPerSlot = 2;
        private const string CentralizedAIResearchId = "NJ_CentralizedAI_ResearchDef";

        private static bool IsResearchOrManufacturingComponent(GeoFacilityComponentDef component, out bool isResearch, out bool isManufacturing)
        {
            isResearch = false;
            isManufacturing = false;
            if (component is ResourceGeneratorFacilityComponentDef generator)
            {
                if (generator.BaseResourcesOutput.ByResourceType(ResourceType.Research).Value > 0f)
                {
                    isResearch = true;
                }

                if (generator.BaseResourcesOutput.ByResourceType(ResourceType.Production).Value > 0f)
                {
                    isManufacturing = true;
                }
            }

            return isResearch || isManufacturing;
        }

        private static bool FacilityProvidesResearch(PhoenixFacilityDef facilityDef)
        {
            if (facilityDef?.GeoFacilityComponentDefs == null) return false;
            foreach (GeoFacilityComponentDef comp in facilityDef.GeoFacilityComponentDefs)
            {
                if (comp is ResourceGeneratorFacilityComponentDef gen
                    && gen.BaseResourcesOutput.ByResourceType(ResourceType.Research).Value > 0f)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool FacilityProvidesManufacturing(PhoenixFacilityDef facilityDef)
        {
            if (facilityDef?.GeoFacilityComponentDefs == null) return false;
            foreach (GeoFacilityComponentDef comp in facilityDef.GeoFacilityComponentDefs)
            {
                if (comp is ResourceGeneratorFacilityComponentDef gen
                    && gen.BaseResourcesOutput.ByResourceType(ResourceType.Production).Value > 0f)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasFacilityBuff(GeoPhoenixFaction faction, Func<PhoenixFacilityDef, bool> facilityPredicate)
        {
            if (faction?.FacilityBuffs?.FacilityBuffs == null) return false;
            return faction.FacilityBuffs.FacilityBuffs.Any(buff => buff?.FacilityDef != null && facilityPredicate(buff.FacilityDef));
        }

        private static bool TryGetUnoccupiedSlotBonuses(GeoPhoenixFaction faction, FacilitySlotPools pools, out float researchBonus, out float productionBonus)
        {
            researchBonus = 0f;
            productionBonus = 0f;

            if (faction == null || pools == null) return false;

            int unoccupiedResearch = Math.Max(0, pools.Research.ProvidedSlots - pools.Research.UsedSlots);
            int unoccupiedManufacturing = Math.Max(0, pools.Manufacturing.ProvidedSlots - pools.Manufacturing.UsedSlots);

            if (unoccupiedResearch == 0 && unoccupiedManufacturing == 0)
            {
                return false;
            }

            int researchPerSlot = UnoccupiedResearchPerSlot;
            int productionPerSlot = UnoccupiedProductionPerSlot;

            if (faction.Research != null && faction.Research.HasCompleted(CentralizedAIResearchId))
            {
                researchPerSlot++;
                productionPerSlot++;
            }

            if (HasFacilityBuff(faction, FacilityProvidesResearch) && unoccupiedResearch > 0)
            {
                researchBonus = unoccupiedResearch * researchPerSlot;
            }

            if (HasFacilityBuff(faction, FacilityProvidesManufacturing) && unoccupiedManufacturing > 0)
            {
                productionBonus = unoccupiedManufacturing * productionPerSlot;
            }

            return researchBonus > 0f || productionBonus > 0f;
        }

        private static void ApplyUnoccupiedSlotBonuses(GeoPhoenixFaction faction)
        {
            FacilitySlotPools pools = ResearchManufacturingSlotsManager.GetOrCreatePools(faction);
            if (!TryGetUnoccupiedSlotBonuses(faction, pools, out float researchBonus, out float productionBonus))
            {
                return;
            }

            if (researchBonus > 0f)
            {
                faction.Wallet.Give(new ResourceUnit(ResourceType.Research, researchBonus), OperationReason.Production);
            }

            if (productionBonus > 0f)
            {
                faction.Wallet.Give(new ResourceUnit(ResourceType.Production, productionBonus), OperationReason.Production);
            }

            TryUpdateInfoBar(faction);
        }

        [HarmonyPatch(typeof(GeoPhoenixFaction), nameof(GeoPhoenixFaction.UpdateBasesHourly))]
        internal static class GeoPhoenixFaction_UpdateBasesHourly_Patch
        {
            private static void Postfix(GeoPhoenixFaction __instance)
            {
                if (!BaseReworkUtils.BaseReworkEnabled)
                {
                    return;
                }

                ResearchManufacturingSlotsManager.RecalculateSlots(__instance);
                ApplyUnoccupiedSlotBonuses(__instance);
            }
        }

        [HarmonyPatch(typeof(GeoFactionFacilityBuffCollection), nameof(GeoFactionFacilityBuffCollection.GetValue))]
        internal static class GeoFactionFacilityBuffCollection_GetValue_Patch
        {
            private static bool Prefix(PhoenixFacilityDef facility, GeoFacilityComponentDef component, float baseValue, float addedValue, float multiplier, ref float __result)
            {
                if (!BaseReworkUtils.BaseReworkEnabled || component == null)
                {
                    return true;
                }

                if (!IsResearchOrManufacturingComponent(component, out _, out _))
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
            private static void Postfix(UIModuleInfoBar __instance, GeoFaction faction)
            {
                if (!BaseReworkUtils.BaseReworkEnabled)
                {
                    return;
                }

                if (!(faction is GeoPhoenixFaction phoenix))
                    return;

                FacilitySlotPools slotPools = ResearchManufacturingSlotsManager.GetOrCreatePools(phoenix);

                float baseProd = faction.ResourceIncome.GetTotalResouce(ResourceType.Production).Value;
                float baseResearch = faction.ResourceIncome.GetTotalResouce(ResourceType.Research).Value;

                int usedProd = slotPools.Manufacturing.UsedSlots;
                int usedResearch = slotPools.Research.UsedSlots;

                float prodBonus = usedProd * WorkerOutputPerSlot;
                float researchBonus = usedResearch * WorkerOutputPerSlot;

                TryGetUnoccupiedSlotBonuses(phoenix, slotPools, out float unoccupiedResearchBonus, out float unoccupiedProductionBonus);
                prodBonus += unoccupiedProductionBonus;
                researchBonus += unoccupiedResearchBonus;

                float effectiveProd = baseProd + prodBonus;
                float effectiveResearch = baseResearch + researchBonus;

                __instance.ProductionLabel.text = FormatSlotString(Mathf.RoundToInt(effectiveProd), Mathf.RoundToInt(prodBonus), slotPools.Manufacturing);
                __instance.ResearchLabel.text = FormatSlotString(Mathf.RoundToInt(effectiveResearch), Mathf.RoundToInt(researchBonus), slotPools.Research);
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
                if (ProvidedSlots == 0)
                {
                    UsedSlots = 0;
                }
                else
                {
                    UsedSlots = Math.Min(UsedSlots, ProvidedSlots);
                }
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
                if (UsedSlots >= ProvidedSlots) return false;
                UsedSlots++;
                return true;
            }

            internal bool TryDecrement()
            {
                if (UsedSlots <= 0) return false;
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
                if (faction == null) throw new ArgumentNullException(nameof(faction));
                return Pools.GetValue(faction, _ => new FacilitySlotPools());
            }

            public static FacilitySlotPools RecalculateSlots(GeoPhoenixFaction faction)
            {
                FacilitySlotPools pools = GetOrCreatePools(faction);
                int researchProviders = 0;
                int manufacturingProviders = 0;

                foreach (GeoPhoenixBase geoBase in faction.Bases)
                {
                    if (geoBase?.Layout == null) continue;

                    foreach (GeoPhoenixFacility facility in geoBase.Layout.Facilities)
                    {
                        if (facility == null || !facility.IsWorking || facility.Def?.GeoFacilityComponentDefs == null)
                            continue;

                        bool providesResearch = false;
                        bool providesManufacturing = false;

                        foreach (GeoFacilityComponentDef comp in facility.Def.GeoFacilityComponentDefs)
                        {
                            if (comp is ResourceGeneratorFacilityComponentDef gen)
                            {
                                if (!providesResearch && gen.BaseResourcesOutput.ByResourceType(ResourceType.Research).Value > 0f)
                                    providesResearch = true;
                                if (!providesManufacturing && gen.BaseResourcesOutput.ByResourceType(ResourceType.Production).Value > 0f)
                                    providesManufacturing = true;
                                if (providesResearch && providesManufacturing) break;
                            }
                        }

                        if (providesResearch) researchProviders++;
                        if (providesManufacturing) manufacturingProviders++;
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
                if (changed) TryUpdateInfoBar(faction);
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
                if (changed) TryUpdateInfoBar(faction);
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

        private static void TryUpdateInfoBar(GeoPhoenixFaction faction)
        {
            if (faction == null) return;

            GeoLevelController level = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
            if (level == null) return;

            var view = level.View;
            var modules = view?.GeoscapeModules;
            var infoBar = modules?.ResourcesModule;

            if (infoBar == null)
            {
                // View not ready yet during load – skip UI update safely.
                //                TFTVLogger.Always("[Workers] InfoBar not ready; skipping update.");
                // Reduce log spam: only output once per load cycle.
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

                var update = AccessTools.Method(typeof(UIModuleInfoBar), "UpdateResourceInfo");
                update.Invoke(infoBar, new object[] { faction, false });
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void FlushPendingInfoBarUpdate(GeoLevelController level)
        {
            TFTVLogger.Always($"[Workers] FlushPendingInfoBarUpdate called. Pending={_pendingInfoBarUpdate} LevelReady={(level != null)}");
            if (!_pendingInfoBarUpdate) return;

            GeoPhoenixFaction faction = _pendingFaction ?? level?.PhoenixFaction;
            if (faction == null)
            {
                TFTVLogger.Always("[Workers] Pending info bar update skipped: no faction available.");
            }
            if (faction == null) return;

            TryUpdateInfoBar(faction);
        }

        // Track if we've logged the missing infobar once (avoid spamming each slot set).
        private static bool _pendingInfoBarLog = false;
        private static bool _pendingInfoBarUpdate = false;
        private static GeoPhoenixFaction _pendingFaction;


    }
}
