using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using System;
using System.Collections.Generic;
using System.Linq;

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
