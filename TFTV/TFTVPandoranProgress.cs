using Base.Core;
using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Statuses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TFTV
{
    internal class TFTVPandoranProgress
    {

        //  private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;

        private static readonly GeoAlienBaseTypeDef nestType = DefCache.GetDef<GeoAlienBaseTypeDef>("Nest_GeoAlienBaseTypeDef");
        private static readonly GeoAlienBaseTypeDef lairType = DefCache.GetDef<GeoAlienBaseTypeDef>("Lair_GeoAlienBaseTypeDef");
        private static readonly GeoAlienBaseTypeDef citadelType = DefCache.GetDef<GeoAlienBaseTypeDef>("Citadel_GeoAlienBaseTypeDef");
        private static readonly GeoAlienBaseTypeDef palaceType = DefCache.GetDef<GeoAlienBaseTypeDef>("Palace_GeoAlienBaseTypeDef");
        public static int ScyllaCount = 0;

        /*  [HarmonyPatch(typeof(NavObstacle), "IsPassable")]
          internal static class TFTV_NavObstacle_AbilityAdded_ScyllaCaterpillar_patch
          {

              public static void Prefix(NavObstacle __instance, bool __result, float maxDist, params NavAreas[] areas)
              {
                  try
                  {
                     TFTVLogger.Always($"obstacle {__instance.NavSettings.Name} is passable {__result} maxDist is {maxDist}");

                     foreach(NavAreas area in areas) 
                      {
                          TFTVLogger.Always($"areaMask is {area.AreaMask}");

                      }


                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);

                  }
              }
          }*/





        [HarmonyPatch(typeof(ShreddingDamageKeywordData), "ProcessKeywordDataInternal")]
        internal static class TFTV_ShreddingDamageKeywordData_ProcessKeywordDataInternal_ScyllaImmunity_patch
        {

            public static void Postfix(ref DamageAccumulation.TargetData data)
            {
                try
                {
                    DamageMultiplierStatusDef queenImmunity = DefCache.GetDef<DamageMultiplierStatusDef>("E_BlastImmunityStatus [Queen_GunsFire_ShootAbilityDef]");

                    if (data.Target.GetActor() != null && data.Target.GetActor().Status != null)
                    {
                        TacticalActorBase actor = data.Target.GetActor();

                        if (actor.Status.HasStatus(queenImmunity))
                        {
                            data.DamageResult.ArmorDamage = Mathf.Floor(data.DamageResult.ArmorDamage * 0);
                            TFTVLogger.Always($"armor damage is {data.DamageResult.ArmorDamage}");
                        }
                    }
                }


                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }





        [HarmonyPatch(typeof(GeoAlienFaction), "UpdateFactionDaily")]
        internal static class BC_GeoAlienFaction_UpdateFactionDaily_patch
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            private static void Postfix(GeoAlienFaction __instance)//, List<GeoAlienBase> ____bases)
            {
                try
                {
                    List<GeoAlienBase> listOfAlienBases = __instance.Bases.ToList();

                    int nests = 0;
                    int lairs = 0;
                    int citadels = 0;
                    int palace = 0;

                    foreach (GeoAlienBase alienBase in listOfAlienBases)
                    {
                        if (alienBase.AlienBaseTypeDef.Equals(nestType))
                        {
                            nests++;
                        }
                        else if (alienBase.AlienBaseTypeDef.Equals(lairType))
                        {
                            lairs++;
                        }
                        else if (alienBase.AlienBaseTypeDef.Equals(citadelType))
                        {
                            citadels++;
                        }
                        else if (alienBase.AlienBaseTypeDef.Equals(palaceType))
                        {
                            palace++;
                        }
                    }
                    int difficulty = TFTVReleaseOnly.DifficultyOrderConverter(__instance.GeoLevel.CurrentDifficultyLevel.Order);
                    if (__instance.GeoLevel.EventSystem.GetVariable("Pandorans_Researched_Citadel") == 1)
                    {
                        __instance.AddEvolutionProgress(nests * 5 + lairs * 10 + citadels * 15);
                        __instance.AddEvolutionProgress(__instance.GeoLevel.EventSystem.GetVariable(TFTVInfestation.InfestedHavensVariable) * 10);
                        TFTVLogger.Always("There are " + nests + " nests, " + lairs + " lairs and " + citadels + " citadels on " + __instance.GeoLevel.ElaspedTime);
                        TFTVLogger.Always("The evolution points per day from Pandoran Colonies are " + (nests * 5 + lairs * 10 + citadels * 15)
                            + " And from Infested Havens " + __instance.GeoLevel.EventSystem.GetVariable(TFTVInfestation.InfestedHavensVariable) * 10);
                    }
                    else
                    {
                        __instance.AddEvolutionProgress(nests * 10 + lairs * 20 + citadels * 30);
                        __instance.AddEvolutionProgress(__instance.GeoLevel.EventSystem.GetVariable(TFTVInfestation.InfestedHavensVariable) * 20);
                        TFTVLogger.Always("There are " + nests + " nests, " + lairs + " lairs and " + citadels + " citadels on " + __instance.GeoLevel.ElaspedTime);
                        TFTVLogger.Always("The evolution points per day from Pandoran Colonies are " + (nests * 10 + lairs * 20 + citadels * 30)
                            + " And from Infested Havens " + __instance.GeoLevel.EventSystem.GetVariable(TFTVInfestation.InfestedHavensVariable) * 20);
                    }
                }


                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(GeoAlienFaction), "ProgressEvolution")]
        internal static class GameDifficultyLevelDef_get_evolutionProgress_patch
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            private static bool Prefix(GeoAlienFaction __instance)
            {
                try
                {
                    if (__instance.GeoLevel.EventSystem.GetVariable("Pandorans_Researched_Citadel") == 1)
                    {
                        __instance.AddEvolutionProgress(__instance.GeoLevel.CurrentDifficultyLevel.EvolutionProgressPerDay / 2);
                        TFTVLogger.Always("Evolution progress is reduced to " + __instance.GeoLevel.CurrentDifficultyLevel.EvolutionProgressPerDay / 2 + " per day");
                        return false;
                    }
                    else
                        return true;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
                throw new InvalidOperationException();
            }
        }


        //patch to modify what Scylla spawn
        [HarmonyPatch(typeof(GeoAlienBase), "SpawnMonster")]
        internal static class TFTV_GeoAlienBase_SpawnMonster_patch
        {
            public static bool Prefix(GeoAlienBase __instance)
            {
                try
                {

                    //PhoenixStatisticsManager statisticsManager = (PhoenixStatisticsManager)UnityEngine.Object.FindObjectOfType(typeof(PhoenixStatisticsManager));

                    /*int citadelCount = statisticsManager.CurrentGameStats.GeoscapeStats.SurvivingCitadels + statisticsManager.CurrentGameStats.GeoscapeStats.DestroyedCitadels;
                    TFTVLogger.Always("There are " + statisticsManager.CurrentGameStats.GeoscapeStats.SurvivingCitadels + " existing citadels and " + statisticsManager.CurrentGameStats.GeoscapeStats.DestroyedCitadels
                        + " have been destroyed, so Citadel counter is " + citadelCount);*/
                    GeoscapeEventSystem eventSystem = __instance.Site.GeoLevel.EventSystem;


                    
                    SpawnScylla(__instance, RollScylla(eventSystem.GetVariable("ScyllaCounter")));

                  

                    eventSystem.SetVariable("ScyllaCounter", eventSystem.GetVariable("ScyllaCounter") + 1);

                   

                  

                    TFTVLogger.Always($"Scylla spawned! Count is now {eventSystem.GetVariable("ScyllaCounter")}");

                    return false;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }


            }

        }

        public static void SpawnScylla(GeoAlienBase geoAlienBase, TacCharacterDef scyllaTemplate)
        {
            try
            {
                GeoAlienMonster scylla = geoAlienBase.Site.GeoLevel.CreateAlienMonster(scyllaTemplate);
                typeof(GeoAlienBase).GetMethod("set_Monster", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(geoAlienBase, new object[] { scylla });


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        public static TacCharacterDef RollScylla(int scyllasAlreadySpawned)
        {
            try
            {
                GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                TacCharacterDef startingScylla = DefCache.GetDef<TacCharacterDef>("Scylla1_FrenzyMistSmasherAgileSpawner_AlienMutationVariationDef");
                TacCharacterDef scylla2 = DefCache.GetDef<TacCharacterDef>("Scylla2_SpitMistSmashAgileSpawn_AlienMutationVariationDef");
                TacCharacterDef scylla3 = DefCache.GetDef<TacCharacterDef>("Scylla3_SonicMistSmashAgileSpawn_AlienMutationVariationDef");
                TacCharacterDef scylla4 = DefCache.GetDef<TacCharacterDef>("Scylla4_SpitLaunchGunAgileBelch_AlienMutationVariationDef");
                TacCharacterDef scylla5 = DefCache.GetDef<TacCharacterDef>("Scylla5_SonicMistGunAgileBelch_AlienMutationVariationDef");
                TacCharacterDef scylla6 = DefCache.GetDef<TacCharacterDef>("Scylla6_FrenzyArmorSmashHeavySpawn_AlienMutationVariationDef");
                TacCharacterDef scylla7 = DefCache.GetDef<TacCharacterDef>("Scylla7_SpitArmorGunHeavyBelch_AlienMutationVariationDef");
                TacCharacterDef scylla8 = DefCache.GetDef<TacCharacterDef>("Scylla8_SonicMistSmashHeavyBelch_AlienMutationVariationDef");
                TacCharacterDef scylla9 = DefCache.GetDef<TacCharacterDef>("Scylla9_SonicArmorGunHeavyBelch_AlienMutationVariationDef");
                TacCharacterDef scylla10 = DefCache.GetDef<TacCharacterDef>("Scylla10_Crystal_AlienMutationVariationDef");


                List<TacCharacterDef> allScyllas = new List<TacCharacterDef>()
                { startingScylla, scylla2, scylla3, scylla4, scylla5, scylla6, scylla7, scylla8, scylla9, scylla10 };

                DateTime myDate = new DateTime(1, 1, 1);

                UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                int roll = UnityEngine.Random.Range(scyllasAlreadySpawned, scyllasAlreadySpawned + 1);
                if (roll > allScyllas.Count - 1)
                {
                    int newRoll = UnityEngine.Random.Range(allScyllas.Count - 4, allScyllas.Count - 1);
                    TFTVLogger.Always("It's " + myDate.Add(new TimeSpan(controller.Timing.Now.TimeSpan.Ticks)) + " and " + allScyllas[newRoll].name + " will spawn");
                    return allScyllas[newRoll];
                }
                TFTVLogger.Always("It's " + myDate.Add(new TimeSpan(controller.Timing.Now.TimeSpan.Ticks)) + " and " + allScyllas[roll].name + " will spawn");
                return allScyllas[roll];

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }




        // Harmony patch to change the reveal of alien bases when in scanner range, so increases the reveal chance instead of revealing it right away
        [HarmonyPatch(typeof(GeoAlienFaction), "TryRevealAlienBase")]
        internal static class BC_GeoAlienFaction_TryRevealAlienBase_patch
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            private static bool Prefix(ref bool __result, GeoSite site, GeoFaction revealToFaction, GeoLevelController ____level)
            {
                try
                {
                    if (!site.GetVisible(revealToFaction))
                    {
                        GeoAlienBase component = site.GetComponent<GeoAlienBase>();
                        if (revealToFaction is GeoPhoenixFaction && ((GeoPhoenixFaction)revealToFaction).IsSiteInBaseScannerRange(site, true))
                        {
                            component.IncrementBaseAttacksRevealCounter();
                            // original code:
                            //site.RevealSite(____level.PhoenixFaction);
                            //__result = true;
                            //return false;
                        }
                        if (component.CheckForBaseReveal())
                        {
                            site.RevealSite(____level.PhoenixFaction);
                            __result = true;
                            return false;
                        }
                        component.IncrementBaseAttacksRevealCounter();
                    }
                    __result = false;
                    return false; // Return without calling the original method
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
                throw new InvalidOperationException();
            }
        }
    }
}
