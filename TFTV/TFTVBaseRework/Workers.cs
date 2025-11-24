using Base;
using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Entities.PhoenixBases.FacilityComponents;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewModules;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace TFTV.TFTVBaseRework
{
    internal class Workers
    {
        internal const float WorkerOutputPerSlot = 2.0f;

        [HarmonyPatch(typeof(GeoPhoenixFaction), nameof(GeoPhoenixFaction.UpdateBasesHourly))]
        internal static class GeoPhoenixFaction_UpdateBasesHourly_Patch
        {
            private static void Postfix(GeoPhoenixFaction __instance)
            {
                ResearchManufacturingSlotsManager.RecalculateSlots(__instance);
            }
        }

        [HarmonyPatch(typeof(GeoPhoenixFaction), "OnFacilityStateChanged")]
        internal static class GeoPhoenixFaction_OnFacilityStateChanged_Patch
        {
            private static void Postfix(GeoPhoenixFaction __instance)
            {
                ResearchManufacturingSlotsManager.RecalculateSlots(__instance);
                TryUpdateInfoBar(__instance);
            }
        }

        [HarmonyPatch(typeof(UIModuleInfoBar), "UpdateResourceInfo")]
        internal static class UIModuleInfoBar_UpdateResourceInfo_Patch
        {
            private static void Postfix(UIModuleInfoBar __instance, GeoFaction faction)
            {
                if (!(faction is GeoPhoenixFaction phoenix))
                    return;

                FacilitySlotPools slotPools = ResearchManufacturingSlotsManager.GetOrCreatePools(phoenix);

                float baseProd = faction.ResourceIncome.GetTotalResouce(ResourceType.Production).Value;
                float baseResearch = faction.ResourceIncome.GetTotalResouce(ResourceType.Research).Value;

                int usedProd = slotPools.Manufacturing.UsedSlots;
                int usedResearch = slotPools.Research.UsedSlots;

                float prodBonus = usedProd * WorkerOutputPerSlot;
                float researchBonus = usedResearch * WorkerOutputPerSlot;

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
                    return $"{totalPerHour} (+{bonus}) ({used}/{provided})";
                }
                return $"{totalPerHour} ({used}/{provided})";
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
            public const int SlotsPerFacility = 2;

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
