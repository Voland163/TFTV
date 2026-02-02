using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TFTV
{

    [HarmonyPatch]
    public static class MissionaryCenterPopulationDrainPatches
    {
        private const float LowPopulationTrickleMultiplier = 0.1f;
        private static readonly Dictionary<GeoHaven, float> DrainOverrides = new Dictionary<GeoHaven, float>();
        private static readonly Action<HavenZonesStats, float> SetPopulationDrainPart = CreatePopulationDrainPartSetter();

        [HarmonyPatch(typeof(GeoHaven), "UpdatePerDay")]
        [HarmonyPrefix]
        private static bool GeoHaven_UpdatePerDay_Prefix(GeoHaven __instance, out float __state)
        {
            __state = 0f;
            if (__instance.Site.State != GeoSiteState.Functioning || __instance.IsInfested)
            {
                return true;
            }

            float drainPart = __instance.ZonesStats.PopulationDrainPart;
            if (drainPart <= 0f)
            {
                return true;
            }

            __state = drainPart;
            DrainOverrides[__instance] = drainPart;
            SetPopulationDrainPart?.Invoke(__instance.ZonesStats, 0f);

            foreach (GeoSite geoSite in from s in __instance.Range.SitesInRange
                                        where s.Type == GeoSiteType.Haven && s.State == GeoSiteState.Functioning
                                        select s)
            {
                GeoHaven target = geoSite.GetComponent<GeoHaven>();
                int drained = CalculateDrainAmount(__instance, target, drainPart);
                if (drained > 0)
                {
                    target.Population -= drained;
                }
            }

            return true;
        }

        [HarmonyPatch(typeof(GeoHaven), "UpdatePerDay")]
        [HarmonyPostfix]
        private static void GeoHaven_UpdatePerDay_Postfix(GeoHaven __instance, float __state)
        {
            if (__state <= 0f)
            {
                return;
            }

            SetPopulationDrainPart?.Invoke(__instance.ZonesStats, __state);
            DrainOverrides.Remove(__instance);
        }

        [HarmonyPatch(typeof(GeoHaven), "GetPopulationChange")]
        [HarmonyPrefix]
        private static bool GeoHaven_GetPopulationChange_Prefix(GeoHaven __instance, HavenZonesStats.HavenOnlyOutput output, ref int __result)
        {
            int populationChange = -__instance.GetDyingPopulation(output);
            float drainPart = __instance.ZonesStats.PopulationDrainPart;
            if (DrainOverrides.TryGetValue(__instance, out float overrideDrain))
            {
                drainPart = overrideDrain;
            }

            if (drainPart > 0f)
            {
                foreach (GeoSite geoSite in from s in __instance.Range.SitesInRange
                                            where s.Type == GeoSiteType.Haven && s.State == GeoSiteState.Functioning
                                            select s)
                {
                    GeoHaven target = geoSite.GetComponent<GeoHaven>();
                    int drained = CalculateDrainAmount(__instance, target, drainPart);
                    if (drained > 0)
                    {
                        populationChange += drained;
                    }
                }
            }

            __result = populationChange;
            return false;
        }

        private static int CalculateDrainAmount(GeoHaven source, GeoHaven target, float drainPart)
        {
            if (target == null || target.Site.Owner == source.Site.Owner)
            {
                return 0;
            }

            int drain = Math.Min(target.Population, Mathf.CeilToInt(target.Population * drainPart));
            if (drain <= 0)
            {
                return 0;
            }

            int minPopulation = target.HavenDef.MinPopulationFunctioningHaven;
            if (target.Population < minPopulation * 0.5f)
            {
                drain = Mathf.Max(1, Mathf.FloorToInt(drain * LowPopulationTrickleMultiplier));
            }

            return drain;
        }

        private static Action<HavenZonesStats, float> CreatePopulationDrainPartSetter()
        {
            var setter = AccessTools.PropertySetter(typeof(HavenZonesStats), nameof(HavenZonesStats.PopulationDrainPart));
            if (setter == null)
            {
                return null;
            }

            return (stats, value) => setter.Invoke(stats, new object[] { value });
        }
    }

    internal class TFTVHavenRecruitsGenerationAdjustments
    {
        private static readonly FieldInfo HavenZonesStatsHavenField = AccessTools.Field(typeof(HavenZonesStats), "_haven");
        private static readonly FieldInfo CanGenerateRecruitField = AccessTools.Field(typeof(HavenZonesStats), "<CanGenerateRecruit>k__BackingField");
        private static readonly FieldInfo CanRecruitField = AccessTools.Field(typeof(HavenZonesStats), "<CanRecruit>k__BackingField");

        [HarmonyPatch(typeof(HavenZonesStats), nameof(HavenZonesStats.UpdateZonesStats))]
        internal static class HavenZonesStats_UpdateZonesStats_Patch
        {
            private static void Postfix(HavenZonesStats __instance)
            {
                GeoHaven haven = HavenZonesStatsHavenField?.GetValue(__instance) as GeoHaven;
                if (haven == null)
                {
                    return;
                }

                bool hasEliteZoneBuilding = haven.Zones.Any(zone => zone.Def.ProvidesEliteRecruitment && zone.State == GeoHavenZoneState.Building);
                if (!hasEliteZoneBuilding)
                {
                    return;
                }

                CanGenerateRecruitField?.SetValue(__instance, true);
                CanRecruitField?.SetValue(__instance, true);
            }
        }

       /* [HarmonyPatch(typeof(GeoFaction), "GenerateRecruits", new Type[] { typeof(Timing) })]
        internal static class GeoFaction_GenerateRecruits_Patch
        {
            private static void Prefix(GeoFaction __instance)
            {
                if (__instance == null || !__instance.Def.HavensGenerateRecruits)
                {
                    return;
                }

                CharacterGenerationContext context = __instance.GeoLevel.CharacterGenerator.GenerateCharacterGeneratorContext(__instance);
                int operationalTrainingFacilities = __instance.Havens.Sum(haven => haven.Zones.Count(zone => zone.Def.ProvidesRecruitment && zone.IsOperational));
                StringBuilder builder = new StringBuilder();
                builder.AppendLine($"[RecruitGeneration] Faction '{__instance.Def.name}' recruit template pools (operational training facilities: {operationalTrainingFacilities}):");
                AppendTemplatePool(builder, "AllTemplates", context.Templates);
                AppendTemplatePool(builder, "NonEliteTemplates", context.NonEliteTemplates);
                TFTVLogger.Always(builder.ToString());
            }
        }*/

        private static void AppendTemplatePool(StringBuilder builder, string label, IList<TacCharacterDef> templates)
        {
            float totalWeight = templates.Sum(template => template.RecruitSpawnWeight);
            builder.AppendLine($"  - {label} (total weight: {totalWeight:0.##}, count: {templates.Count}):");
            foreach (TacCharacterDef template in templates.OrderByDescending(t => t.RecruitSpawnWeight))
            {
                float chance = totalWeight > 0f ? template.RecruitSpawnWeight / totalWeight : 0f;
                int level = GetTemplateLevel(template);
                builder.AppendLine($"    * {template.name}: level {level}, weight {template.RecruitSpawnWeight:0.##}, chance {chance:P2}");
            }
        }

        private static int GetTemplateLevel(TacCharacterDef template)
        {
            if (template?.Data?.LevelProgression != null && template.Data.LevelProgression.IsValid)
            {
                return template.Data.LevelProgression.Level;
            }
            return 1;
        }
    }

}
