using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Missions;
using PhoenixPoint.Geoscape.Events.Eventus.Filters;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using System;
using System.Reflection;
using Base.Defs;
using System.Linq;

namespace TFTV
{
    internal class TFTVThirdAct
    {
        
        //private static readonly DefRepository Repo = TFTVMain.Repo;
        public static SharedData sharedData = GameUtl.GameComponent<SharedData>();
        private static readonly DefRepository Repo = TFTVMain.Repo;
      //  private static readonly GeoHavenDef havendef = Repo.GetAllDefs<GeoHavenDef>().FirstOrDefault(ged => ged.name.Equals("GeoHavenDef"));

        public static void ActivateFS3Event(GeoLevelController level)
        {
            try
            {
                GeoTimePassedEventFilterDef timePassedFS3 = Repo.GetAllDefs<GeoTimePassedEventFilterDef>().FirstOrDefault(ged => ged.name.Equals("E_PROG_FS3_TimePassed [GeoTimePassedEventFilterDef]"));
                GeoscapeEventDef geoEventFS3 = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_FS3_GeoscapeEventDef"));
                timePassedFS3.TimePassedHours = UnityEngine.Random.Range(25, 38) + level.ElaspedTime.TimeSpan.Hours; 
               // TFTVLogger.Always("timePassedFS3.TimePassedHours is " + timePassedFS3.TimePassedHours + " and current time is " + level.Timing.Now.TimeSpan.Days);
                geoEventFS3.GeoscapeEventData.Mute = false;
                
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


       /* public static void ChangeHavenDeploymentDefense(GeoLevelController level)
        {
            try
            {

                if (level.EventSystem.GetVariable("Mobilization") == 1)
                {
                    TFTVLogger.Always("Deploymentw was " + havendef.PopulationAsDeployment);
                    havendef.PopulationAsDeployment = 0.5f;
                    TFTVLogger.Always("Deployment increased, now " + havendef.PopulationAsDeployment);
                }
                else
                {
                    havendef.PopulationAsDeployment = 0.1f;
                }

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }*/

        /*
        [HarmonyPatch(typeof(GeoBehemothActor), "StartTravel")]
        public static class GeoBehemothActor_ThirdAct_Patch
        {
            public static void Postfix(GeoBehemothActor __instance)
            {
                try
                {
                    TFTVLogger.Always("Behemoth started travelling method called");
                    if (__instance.GeoLevel.EventSystem.GetVariable("CorruptedLairDestroyed") == 1 && __instance.GeoLevel.EventSystem.GetVariable("ThirdActStarted") == 0)
                    {
                        TFTVLogger.Always("Behemoth rumpus has begun! Let the Third Act roll!");
                        __instance.GeoLevel.EventSystem.SetVariable("ThirdActStarted", 1);

                        SetBehemothOnRampageMod(__instance.GeoLevel);
                        MethodInfo method_GenerateTargetData = AccessTools.Method(typeof(GeoBehemothActor), "CalculateDisruptionThreshhold");
                        method_GenerateTargetData.Invoke(__instance, null);
                        MethodInfo method_GenerateTargetData2 = AccessTools.Method(typeof(AlienRaidManager), "RollForRaid");
                        method_GenerateTargetData2.Invoke(__instance.GeoLevel.AlienFaction.AlienRaidManager, null);
                        TFTVLogger.Always("Alien raid rolled");
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }*/

/*
        [HarmonyPatch(typeof(GeoBehemothActor), "get_DisruptionMax")]
        public static class GeoBehemothActor_get_DisruptionMax_RampageStart_Patch
        {
            public static void Postfix(ref int __result, GeoBehemothActor __instance)
            {
                try
                {
                    if (__instance.GeoLevel.EventSystem.GetVariable("ThirdActStarted") == 1)
                    {
                        __result = 200;
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }

        */
       
    }
}
