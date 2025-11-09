using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Entities.PhoenixBases.FacilityComponents;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TFTV.TFTVBaseRework
{
    internal class Workers
    {
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
            }
        }


        [HarmonyPatch(typeof(UIModuleInfoBar), "UpdateResourceInfo")]
        internal static class UIModuleInfoBar_UpdateResourceInfo_Patch
        {
            private static void Postfix(UIModuleInfoBar __instance, GeoFaction faction)
            {
                GeoPhoenixFaction geoPhoenixFaction = faction as GeoPhoenixFaction;
                if (geoPhoenixFaction == null)
                {
                    return;
                }
                FacilitySlotPools slotPools = ResearchManufacturingSlotsManager.RecalculateSlots(geoPhoenixFaction);
                int manufacturingPerHour = Mathf.RoundToInt(faction.ResourceIncome.GetTotalResouce(ResourceType.Production).Value);
                int researchPerHour = Mathf.RoundToInt(faction.ResourceIncome.GetTotalResouce(ResourceType.Research).Value);
                __instance.ProductionLabel.text = FormatSlotString(manufacturingPerHour, slotPools.Manufacturing);
                __instance.ResearchLabel.text = FormatSlotString(researchPerHour, slotPools.Research);
            }

            private static string FormatSlotString(int outputPerHour, FacilitySlotPool pool)
            {
                int provided = pool.ProvidedSlots;
                int used = (provided > 0) ? pool.UsedSlots : 0;
                return string.Format("{0}/h ({1}/{2})", outputPerHour, used, provided);
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
                this.ProvidedSlots = Math.Max(0, value);
                if (this.ProvidedSlots == 0)
                {
                    this.UsedSlots = 0;
                    return;
                }
                this.UsedSlots = Math.Min(this.UsedSlots, this.ProvidedSlots);
            }

            internal void SetUsedSlots(int value)
            {
                if (this.ProvidedSlots == 0)
                {
                    this.UsedSlots = 0;
                    return;
                }
                this.UsedSlots = Math.Max(0, Math.Min(value, this.ProvidedSlots));
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

            private static readonly ConditionalWeakTable<GeoPhoenixFaction, FacilitySlotPools> Pools = new ConditionalWeakTable<GeoPhoenixFaction, FacilitySlotPools>();

            public static FacilitySlotPools GetOrCreatePools(GeoPhoenixFaction faction)
            {
                if (faction == null)
                {
                    throw new ArgumentNullException("faction");
                }
                return Pools.GetValue(faction, _ => new FacilitySlotPools());
            }

            public static FacilitySlotPools RecalculateSlots(GeoPhoenixFaction faction)
            {
                FacilitySlotPools pools = GetOrCreatePools(faction);
                int researchProviders = 0;
                int manufacturingProviders = 0;
                foreach (GeoPhoenixBase geoPhoenixBase in faction.Bases)
                {
                    if (geoPhoenixBase?.Layout == null)
                    {
                        continue;
                    }
                    foreach (GeoPhoenixFacility geoPhoenixFacility in geoPhoenixBase.Layout.Facilities)
                    {
                        if (geoPhoenixFacility == null || !geoPhoenixFacility.IsWorking || geoPhoenixFacility.Def?.GeoFacilityComponentDefs == null)
                        {
                            continue;
                        }
                        bool providesResearch = false;
                        bool providesManufacturing = false;
                        foreach (GeoFacilityComponentDef geoFacilityComponentDef in geoPhoenixFacility.Def.GeoFacilityComponentDefs)
                        {
                            if (!(geoFacilityComponentDef is ResourceGeneratorFacilityComponentDef resourceGeneratorFacilityComponentDef))
                            {
                                continue;
                            }
                            if (!providesResearch && resourceGeneratorFacilityComponentDef.BaseResourcesOutput.ByResourceType(ResourceType.Research).Value > 0f)
                            {
                                providesResearch = true;
                            }
                            if (!providesManufacturing && resourceGeneratorFacilityComponentDef.BaseResourcesOutput.ByResourceType(ResourceType.Production).Value > 0f)
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

            public static void SetUsedSlots(GeoPhoenixFaction faction, FacilitySlotType slotType, int usedSlots)
            {
                FacilitySlotPools pools = GetOrCreatePools(faction);
                switch (slotType)
                {
                    case FacilitySlotType.Research:
                        pools.Research.SetUsedSlots(usedSlots);
                        return;
                    case FacilitySlotType.Manufacturing:
                        pools.Manufacturing.SetUsedSlots(usedSlots);
                        return;
                    default:
                        return;
                }
            }
        }



    }
}
