using Base.Core;
using HarmonyLib;
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

namespace TFTV
{
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
