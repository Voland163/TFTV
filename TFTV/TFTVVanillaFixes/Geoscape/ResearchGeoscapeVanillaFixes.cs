using HarmonyLib;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Equipments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TFTV.TFTVVanillaFixes.Geoscape
{
    internal class ResearchGeoscapeVanillaFixes
    {
        [HarmonyPatch(typeof(GeoAlienFaction), "OnResearchUpdated")]
        public static class AlienResearchQueueSeeder
        {
            private const int MaxSeedAttempts = 5;

            public static void Postfix(GeoAlienFaction __instance)
            {
                // TFTVLogger.Always("[AlienResearchCadence] OnResearchUpdated postfix called.");

                if (__instance == null || __instance.Research == null || __instance.Research.Paused)
                {
                    return;
                }

                //    TFTVLogger.Always("[AlienResearchCadence] Alien faction research is not paused.");

                Research research = __instance.Research;
                if (research.Count > 0)
                {
                    return;
                }

                TFTVLogger.Always("[AlienResearchCadence] Research queue is empty, attempting to seed.");

                int attempts = 0;
                while (attempts < MaxSeedAttempts)
                {
                    ResearchElement candidate = research.Researchable.FirstOrDefault();

                    TFTVLogger.Always(string.Format("[AlienResearchCadence] Seed attempt {0}, candidate: {1}", attempts + 1, candidate != null ? candidate.ResearchDef.name : "<null>"));

                    if (candidate == null)
                    {
                        break;
                    }

                    try
                    {
                        research.AddResearchToQueue(candidate);
                    }
                    catch (Exception ex)
                    {
                        TFTVLogger.Always(string.Format("[AlienResearchCadence] Failed to seed research {0}: {1}", candidate.ResearchDef.name, ex.Message));
                        break;
                    }

                    if (research.Current != null)
                    {
                        TFTVLogger.Always(string.Format("[AlienResearchCadence] Successfully seeded research queue with {0}", research.Current.ResearchDef.name));

                        return;
                    }

                    attempts++;
                }

                if (research.Current == null && research.Researchable.Any())
                {
                    TFTVLogger.Always(string.Format("[AlienResearchCadence] Research queue still empty after seeding attempts. Researchable={0}", research.Researchable.Count()));
                }
            }
        }



        //fixes requiring killing actor required for research even when it is already captured

        [HarmonyPatch(typeof(ActorResearchRequirement), "OnMissionEnd")] //VERIFIED
        public static class TFTV_ActorResearchRequirement_OnMissionEnd
        {
            public static bool Prefix(ActorResearchRequirement __instance, GeoFaction faction, GeoMission mission, GeoSite site, GeoFaction ____faction)
            {
                try
                {
                    _ = site.GeoLevel;
                    ActorResearchRequirementDef actorResearchRequirementDef = __instance.ActorResearchRequirementDef;

                    //TFTVLogger.Always($"actorResearchRequirementDef: {actorResearchRequirementDef.name}");

                    foreach (FactionResult factionResult in mission.Result.FactionResults)
                    {
                        if (factionResult.FactionDef == ____faction.Def.PPFactionDef || (__instance.ActorResearchRequirementDef.Faction != null && factionResult.FactionDef != actorResearchRequirementDef.Faction))
                        {
                            continue;
                        }

                        foreach (TacActorUnitResult item in from t in factionResult.UnitResults.Select((UnitResult s) => s.Data).OfType<TacActorUnitResult>()
                                                            where !t.IsAlive || t.Statuses.Any(s => s.Def.EffectName == "Paralysed")
                                                            select t)
                        {
                            // TFTVLogger.Always($"item: {item?.TacticalActorBaseDef?.name} is valid? {__instance.IsValidUnit(item)}");

                            if (__instance.IsValidUnit(item))
                            {
                                MethodInfo updateProgressMethod = typeof(ResearchRequirement).GetMethod("UpdateProgress", BindingFlags.Instance | BindingFlags.NonPublic);

                                updateProgressMethod.Invoke(__instance, new object[] { 1 });

                                if (__instance.IsCompleted)
                                {
                                    break;
                                }
                            }
                        }
                    }

                    return false;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        /// <summary>
        /// Fixes not recognizing required research tags
        /// </summary>
        [HarmonyPatch(typeof(ActorResearchRequirementDef), nameof(ActorResearchRequirementDef.IsValidActor))]
        public static class TFTV_ActorResearchRequirementDef_IsValidActor
        {
            public static bool Prefix(ActorResearchRequirementDef __instance, GeoUnitDescriptor unit, TacticalActorDef actorRequirement, GameTagDef tagRequirement, ref bool __result)
            {
                try
                {

                    if (unit == null)
                    {
                        // TFTVLogger.Always("early exit 1");
                        __result = false;
                        return false;
                    }

                    TacticalActorBaseDef tacticalActorBaseDef = unit.UnitType.TemplateDef.TacticalActorBaseDef;
                    if (actorRequirement != null && actorRequirement != tacticalActorBaseDef)
                    {
                        //  TFTVLogger.Always("early exit 2");
                        __result = false;
                        return false;
                    }

                    if (tagRequirement != null)
                    {
                        // TFTVLogger.Always("got here");

                        bool flag = tacticalActorBaseDef.GameTags.Contains(tagRequirement);
                        if (!flag)
                        {
                            List<TacticalItemDef> enumerable = unit.ArmorItems;
                            List<TacticalItemDef> equipment = unit.Equipment;
                            if (enumerable == null)
                            {
                                enumerable = new List<TacticalItemDef>();
                            }

                            if (equipment != null)
                            {
                                enumerable.Concat(equipment);
                            }

                            enumerable.AddRange(unit.UnitType.TemplateDef.GetTemplateBodyparts());



                            flag = enumerable.Any(b => b.Tags.Contains(tagRequirement));

                        }

                        if (!flag)
                        {
                            __result = false;
                            return false;
                        }

                        __result = true;
                    }

                    __result = true;
                    return false;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        [HarmonyPatch(typeof(ResearchRequirement), nameof(ResearchRequirement.Initialize))]
        internal static class CaptureActorResearchRequirementInitializePatch
        {
            private static readonly MethodInfo UpdateProgressMethod = AccessTools.Method(typeof(ResearchRequirement), "UpdateProgress");

            [HarmonyPostfix]
            private static void ApplyExistingCapturedUnits(ResearchRequirement __instance, ResearchRequirementDef def, GeoFaction faction)
            {
                CaptureActorResearchRequirement captureRequirement = __instance as CaptureActorResearchRequirement;
                GeoPhoenixFaction phoenixFaction = faction as GeoPhoenixFaction;
                if (captureRequirement == null || phoenixFaction == null || def == null || !def.IsRetroactive || __instance.IsCompleted)
                {
                    return;
                }

                int matchingCapturedUnits = phoenixFaction.CapturedUnits.Count((unit) => captureRequirement.IsValidUnit(unit));
                int progressToApply = Math.Min(matchingCapturedUnits, __instance.Total) - __instance.Progress;
                if (progressToApply <= 0)
                {
                    return;
                }

                UpdateProgressMethod.Invoke(__instance, new object[]
                {
                progressToApply
                });
            }
        }

    }

}
