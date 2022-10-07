using Base;
using HarmonyLib;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Tactical.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TFTV
{
    internal class TFTVPandoranProgress
    {

        //  private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        private static readonly GeoAlienBaseTypeDef nestType = DefCache.GetDef<GeoAlienBaseTypeDef>("Nest_GeoAlienBaseTypeDef");
        private static readonly GeoAlienBaseTypeDef lairType = DefCache.GetDef<GeoAlienBaseTypeDef>("Lair_GeoAlienBaseTypeDef");
        private static readonly GeoAlienBaseTypeDef citadelType = DefCache.GetDef<GeoAlienBaseTypeDef>("Citadel_GeoAlienBaseTypeDef");
        private static readonly GeoAlienBaseTypeDef palaceType = DefCache.GetDef<GeoAlienBaseTypeDef>("Palace_GeoAlienBaseTypeDef");

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
                    int difficulty = __instance.GeoLevel.CurrentDifficultyLevel.Order;
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

        public static GeoAlienMonster Monster = null;
        //patch to modify what Scylla spawn
        [HarmonyPatch(typeof(GeoAlienBase), "SpawnMonster")]
        internal static class TFTV_GeoAlienBase_SpawnMonster_patch
        {
            public static bool Prefix(GeoAlienBase __instance, ClassTagDef classTag, bool fallbackAllTemplates = false)
            {
                try
                {
                    ClassTagDef queenTag = DefCache.GetDef<ClassTagDef>("Queen_ClassTagDef");
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


                    List<TacCharacterDef> list = __instance.Site.Owner.UnlockedUnitTemplates.Where((TacCharacterDef t) => t.ClassTag == classTag).ToList();
                    if (list.Count == 0)
                    {
                      //  TFTVLogger.Debug($"No template exist for monster class '{classTag}' in faction '{__instance.Site.Owner}'.");
                      //  if (!fallbackAllTemplates)
                      //  {
                            Monster = __instance.Site.GeoLevel.CreateAlienMonster(startingScylla);
                            typeof(GeoAlienBase).GetMethod("set_Monster", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { Monster});
                            return false;
                      //  }

                       // TFTVLogger.Debug($"Fallback to all template for monster class '{classTag}'.");
                      //  list.AddRange(__instance.Site.GeoLevel.CharacterGenerator.UnitTemplates.Where((TacCharacterDef t) => t.ClassTag == classTag));
                    }
                    List<TacCharacterDef> shortList = new List<TacCharacterDef>();

                    if (list.Contains(scylla10))
                    {

                        shortList.Add(scylla10);

                    }
                    if (list.Contains(scylla9))
                    {

                        shortList.Add(scylla9);

                    }
                    if (list.Contains(scylla8))
                    {

                        shortList.Add(scylla8);

                    }
                    if (list.Contains(scylla7))
                    {

                        shortList.Add(scylla7);

                    }
                    if (list.Contains(scylla6))
                    {

                        shortList.Add(scylla6);
                    }
                    if (list.Contains(scylla5) && !list.Contains(scylla9))

                    {
                        shortList.Add(scylla5);

                    }
                    if (list.Contains(scylla4) && !list.Contains(scylla6))

                    {
                        shortList.Add(scylla4);

                    }
                    if (list.Contains(scylla3) && !list.Contains(scylla5))

                    {
                        shortList.Add(scylla3);

                    }
                    if (list.Contains(scylla2) && !list.Contains(scylla4))

                    {
                        shortList.Add(scylla2);

                    }
                    if (list.Contains(startingScylla) && !list.Contains(scylla2))

                    {
                        shortList.Add(startingScylla);

                    }

                    TacCharacterDef randomElement = list.GetRandomElement();
                    Monster = __instance.Site.GeoLevel.CreateAlienMonster(randomElement);
                    typeof(GeoAlienBase).GetMethod("set_Monster", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { Monster});
                    return false;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
                return true;

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
