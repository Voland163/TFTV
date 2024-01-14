using Base;
using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Missions;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewModules;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV
{
    internal class TFTVAmbushes
    {

        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        internal static readonly string Hotspot = "Hotspot_";
        public static List<int> NJ_Purists_Hotspots = new List<int>();
        public static List<int> AN_FallenOnes_Hotspots = new List<int>();


        [HarmonyPatch(typeof(GeoscapeEventSystem), "SetEventForSite")]
        public static class GeoscapeEventSystem_SetEventForSite_patch
        {

            public static void Postfix(GeoSite site, string eventID)
            {
                try
                {
                    TFTVLogger.Always($"setting event {eventID} for {site.SiteId}");

                    GeoLevelController controller = site.GeoLevel;

                    if (eventID != "PROG_PU8_MISS" && eventID != "PROG_PU12_MISS" && eventID != "PROG_PU14_MISS")
                    {
                        return;
                    }

                  //  TFTVLogger.Always($"got past the check, {eventID}");

                    GeoSubFactionDef forsakenDef = DefCache.GetDef<GeoSubFactionDef>("AN_FallenOnes_GeoSubFactionDef");
                    GeoSubFactionDef pureDef = DefCache.GetDef<GeoSubFactionDef>("NJ_Purists_GeoSubFactionDef");
                    
                    GeoSubFaction forsaken = controller.GetSubFaction(forsakenDef, true);
                    GeoSubFaction pure = controller.GetSubFaction(pureDef, true);

                    if (forsaken != null && eventID == "PROG_PU8_MISS")
                    {
                        TFTVLogger.Always($"Forsaken event {eventID} registered, setting hotspot");
                        SetFallenHotspotVariable(site.SiteId);
                    }
                    else if (pure != null)
                    {
                        TFTVLogger.Always($"Pure event {eventID} registered, setting hotspot");
                        SetPureHotspotVariable(site.SiteId);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        [HarmonyPatch(typeof(UIModuleSelectionInfoBox), "SetUnexploredSite")]
        public static class UIModuleSelectionInfoBox_SetUnexploredSite_patch
        {
            public static void Postfix(UIModuleSelectionInfoBox __instance, GeoSite ____site)
            {
                try
                {

                    if (GameUtl.CurrentLevel() == null || GameUtl.CurrentLevel().GetComponent<GeoLevelController>() == null)
                    {
                        return;
                    }

                    GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                    GeoPhoenixFaction phoenix = controller.PhoenixFaction;

                    if (!phoenix.IsSiteInBaseScannerRange(____site, true))
                    {
                        return;
                    }

                    List<Tuple<IGeoFactionMissionParticipant, int>> results = controller.GetAllPossibleEnemyMissionParticipants(____site);

                    results = results.OrderByDescending(r => r.Item2).ToList();

                    /*  PPFactionDef forsaken = DefCache.GetDef<PPFactionDef>("AN_FallenOnes_FactionDef");
                      PPFactionDef pure = DefCache.GetDef<PPFactionDef>("NJ_Purists_FactionDef");
                      PPFactionDef bandits = DefCache.GetDef<PPFactionDef>("NEU_Bandits_FactionDef");
                      PPFactionDef anu = DefCache.GetDef<PPFactionDef>("Anu_FactionDef");
                      PPFactionDef nj = DefCache.GetDef<PPFactionDef>("NewJericho_FactionDef");
                      PPFactionDef syn = DefCache.GetDef<PPFactionDef>("Synedrion_FactionDef");
                      PPFactionDef pandorans = DefCache.GetDef<PPFactionDef>("Alien_FactionDef");*/

                    string expectedFaction = results[0].Item1.ParticipantViewDef.Name.Localize();

                    string additionalText = $"{TFTVCommonMethods.ConvertKeyToString("KEY_AMBUSH_SAT_SCAN_TEXT0")} {expectedFaction.ToUpper()} {TFTVCommonMethods.ConvertKeyToString("KEY_AMBUSH_SAT_SCAN_TEXT1")}";

                    __instance.SiteTittleText.text += $"\n <i>{additionalText}</i>";

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        [HarmonyPatch(typeof(GeoSite), "RegisterMission")]
        public static class GeoSite_RegisterMission_patch
        {

            public static void Postfix(GeoSite __instance, GeoMission geoMission)
            {
                try
                {
                    GeoLevelController controller = __instance.GeoLevel;

                    GeoSubFactionDef forsakenDef = DefCache.GetDef<GeoSubFactionDef>("AN_FallenOnes_GeoSubFactionDef");
                    GeoSubFactionDef pureDef = DefCache.GetDef<GeoSubFactionDef>("NJ_Purists_GeoSubFactionDef");

                    GeoSubFaction forsaken = controller.GetSubFaction(forsakenDef, true);
                    GeoSubFaction pure = controller.GetSubFaction(pureDef, true);

                    if (forsaken != null && geoMission.GetEnemyFaction().PPFactionDef == forsakenDef.PPFactionDef)
                    {
                        TFTVLogger.Always($"Forsaken mission {geoMission?.MissionName.LocalizeEnglish()} registered, setting hotspot");
                        SetFallenHotspotVariable(__instance.SiteId);
                    }

                    if (pure != null && geoMission.GetEnemyFaction().PPFactionDef == pureDef.PPFactionDef)
                    {
                        TFTVLogger.Always($"Pure mission registered {geoMission?.MissionName.LocalizeEnglish()}, setting hotspot");
                        SetPureHotspotVariable(__instance.SiteId);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }



        [HarmonyPatch(typeof(GeoLevelController), "GetAllPossibleEnemyMissionParticipants")]
        public static class GeoLevelController_GetAllPossibleEnemyMissionParticipants_patch
        {
            public static bool Prefix(GeoLevelController __instance, ref List<Tuple<IGeoFactionMissionParticipant, int>> __result, GeoSite site)
            {
                try
                {
                    List<Tuple<IGeoFactionMissionParticipant, int>> list = new List<Tuple<IGeoFactionMissionParticipant, int>>();
                    GeoSite[] activeSitesInRange = (from s in GeoMap.GetSitesInRange(site, __instance.ScavengingAmbushSitesRange, activeOnly: true, excludeCurrentSite: true)
                                                    where s.State == GeoSiteState.Functioning
                                                    select s).ToArray();
                    list.Add(new Tuple<IGeoFactionMissionParticipant, int>(__instance.AlienFaction, __instance.AlienFaction.GetAmbushScavengingWeight(__instance.PhoenixFaction, site, activeSitesInRange)));
                    foreach (GeoFaction item in __instance.FactionsWithDiplomacy)
                    {
                        list.Add(new Tuple<IGeoFactionMissionParticipant, int>(item, item.GetAmbushScavengingWeight(__instance.PhoenixFaction, site, activeSitesInRange)));
                    }

                    GeoSubFactionDef banditsDef = DefCache.GetDef<GeoSubFactionDef>("NEU_Bandits_GeoSubFactionDef");

                    foreach (GeoSubFaction subFaction in __instance.SubFactions)
                    {
                        if (subFaction != null && subFaction.MissionParticipationWeight != 0 && subFaction.SubFactionDef!=banditsDef)
                        {
                            int weight = 30;

                            if (subFaction.GetPPName() == "NJ_Purists")
                            {
                                if (CheckProximityToHotspot(NJ_Purists_Hotspots, site))
                                {
                                    weight += 200;
                                }
                            }
                            else if(subFaction.GetPPName() == "AN_FallenOnes")
                            {
                                if (CheckProximityToHotspot(AN_FallenOnes_Hotspots, site))
                                {
                                    weight += 200;
                                }
                            }

                            list.Add(new Tuple<IGeoFactionMissionParticipant, int>(subFaction, weight));
                        }
                    }

                    list.RemoveAll((Tuple<IGeoFactionMissionParticipant, int> i) => i.Item2 <= 0);

                    int maxScoreForRaiderBoost = 70;
                    int raiderWeight = 30;
                    bool raiderBoost = true;

                    foreach (Tuple<IGeoFactionMissionParticipant, int> tuple in list)
                    {
                        if (tuple.Item2 > maxScoreForRaiderBoost) 
                        {
                            raiderBoost = false;
                        }                    
                    }

                    if(raiderBoost) 
                    {
                        raiderWeight = 80;                   
                    }

                  

                    GeoSubFaction bandits = __instance.GetSubFaction(banditsDef, true);



                    list.Add(new Tuple<IGeoFactionMissionParticipant, int>(bandits, raiderWeight));

     

                    __result = list;

                 /*   foreach (Tuple<IGeoFactionMissionParticipant, int> tuple in __result)
                    {
                       // TFTVLogger.Always($"{tuple.Item1.ParticipantName.LocalizeEnglish()} scored {tuple.Item2}");
                    }*/

                    return false;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        /*   public static void GetAllPureAndForsakenHotspots(GeoLevelController controller)
           {
               try
               {
                   GeoscapeEventSystem eventSystem = controller.EventSystem;

                   GeoSubFactionDef forsakenDef = DefCache.GetDef<GeoSubFactionDef>("AN_FallenOnes_GeoSubFactionDef");
                   GeoSubFactionDef pureDef = DefCache.GetDef<GeoSubFactionDef>("NJ_Purists_GeoSubFactionDef");


                   GeoSubFaction forsaken = controller.GetSubFaction(forsakenDef, true);
                   GeoSubFaction pure = controller.GetSubFaction(pureDef, true);

                   if (forsaken != null)
                   {
                       for (int x = 1; x < 4; x++)
                       {
                           TFTVLogger.Always($"{forsaken.GetPPName()}_{Hotspot}{x} {eventSystem.GetVariable($"{forsaken.GetPPName()}_{Hotspot}{x}")}");
                       }
                   }

                   if (pure != null)
                   {
                       for (int x = 1; x < 4; x++)
                       {
                           TFTVLogger.Always($"{pure.GetPPName()}_{Hotspot}{x} {eventSystem.GetVariable($"{forsaken.GetPPName()}_{Hotspot}{x}")}");
                       }
                   }
               }
               catch (Exception e)
               {
                   TFTVLogger.Error(e);
                   throw;
               }
           }*/

        private static void SetPureHotspotVariable(int siteID)
        {
            try
            {
                if (NJ_Purists_Hotspots == null)
                {
                    NJ_Purists_Hotspots = new List<int>();
                }


                for (int x = 0; x < NJ_Purists_Hotspots.Count; x++)
                {
                    if (NJ_Purists_Hotspots[x] == siteID)
                    {
                        TFTVLogger.Always($"{siteID} already a Pure hotspot");
                        return;
                    }
                }

                if (NJ_Purists_Hotspots.Count > 2)
                {
                    NJ_Purists_Hotspots[2] = NJ_Purists_Hotspots[1];
                    NJ_Purists_Hotspots[1] = NJ_Purists_Hotspots[0];
                    NJ_Purists_Hotspots[0] = siteID;
                }
                else
                {
                    NJ_Purists_Hotspots.Add(siteID);
                }

                TFTVLogger.Always($"{siteID} is now hotspot for the Pure");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static void SetFallenHotspotVariable(int siteID)
        {
            try
            {
                if (AN_FallenOnes_Hotspots == null) 
                {
                    AN_FallenOnes_Hotspots = new List<int>();               
                }

                for (int x = 0; x < AN_FallenOnes_Hotspots.Count; x++)
                {
                    if (AN_FallenOnes_Hotspots[x] == siteID)
                    {
                        TFTVLogger.Always($"{siteID} already a Fallen hotspot");
                        return;
                    }
                }
               
                if (AN_FallenOnes_Hotspots.Count > 2) 
                {
                    AN_FallenOnes_Hotspots[2] = AN_FallenOnes_Hotspots[1];
                    AN_FallenOnes_Hotspots[1] = AN_FallenOnes_Hotspots[0];
                    AN_FallenOnes_Hotspots[0] = siteID;
                }
                else 
                {
                    AN_FallenOnes_Hotspots.Add(siteID);
                
                }
                
                TFTVLogger.Always($"{siteID} is now hotspot for the Fallen");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        /*  private static void SetHotpostVariable(GeoSubFaction geoSubFaction, int siteID, GeoLevelController controller)
          {
              try
              {
                  GeoscapeEventSystem eventSystem = controller.EventSystem;

                  for (int x = 1; x < 4; x++)
                  {
                      if (eventSystem.GetVariable($"{geoSubFaction.GetPPName()}_{Hotspot}{x}") == siteID)
                      {
                          TFTVLogger.Always($"{siteID} already a hotspot in {geoSubFaction.GetPPName()}_{Hotspot}{x}");
                          return;
                      }
                  }

                  int spot = FindSpotForHotspot(geoSubFaction, controller);

                  eventSystem.SetVariable($"{geoSubFaction.GetPPName()}_{Hotspot}{spot}", siteID);

                  TFTVLogger.Always($"{siteID} is now hotspot # {spot} for {geoSubFaction.GetPPName()}, " +
                      $"check variable {geoSubFaction.GetPPName()}_{Hotspot}{spot}: {eventSystem.GetVariable($"{geoSubFaction.GetPPName()}_{Hotspot}{spot}")}");
              }
              catch (Exception e)
              {
                  TFTVLogger.Error(e);
                  throw;
              }
          }*/

        private static int FindSpotForHotspot(List<int> hotspotList)
        {
            try
            {


                for (int x = 0; x < 3; x++)
                {
                    if (hotspotList[x] == 0)
                    {
                        return x;
                    }
                }

                return 0;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static List<int> GetAllHotspotGeoSiteIDs(GeoSubFaction geoSubFaction, GeoLevelController controller)
        {
            try
            {
                GeoscapeEventSystem eventSystem = controller.EventSystem;
                List<int> list = new List<int>();

                for (int x = 1; x < 4; x++)
                {
                    if (eventSystem.GetVariable($"{geoSubFaction.GetPPName()}_{Hotspot}{x}") != 0)
                    {
                        list.Add(eventSystem.GetVariable($"{geoSubFaction.GetPPName()}_{Hotspot}{x}"));
                    }
                }

                return list;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static bool CheckProximityToHotspot(List<int> hotspotIds, GeoSite site)
        {
            try
            {

                if (hotspotIds == null)
                {
                    return false;
                }


                if (hotspotIds.Count == 0)
                {
                    return false;
                }

                EarthUnits range = new EarthUnits() { Value = 1500 };
                //range 1500km, same as for Aliens in Alien_GeoAlienFactionDef

                List<GeoSite> geoSites = GeoMap.GetSitesInRange(site, range, false).ToList();

             //   TFTVLogger.Always($"got here CheckProximityToHotspot; range is {range} and there are {geoSites} sites");

                if (hotspotIds.Any(id => geoSites.Any(s => s.SiteId == id))) 
                {
                    return true;
                }
              
                return false;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        [HarmonyPatch(typeof(GeoscapeEventSystem), "OnLevelStart")]

        public static class GeoscapeEventSystem_PhoenixFaction_OnLevelStart_Patch
        {
            /*  public static bool Prepare()
              {
                  TFTVConfig config = TFTVMain.Main.Config;
                  return config.MoreAmbushes;
              }*/

            public static void Prefix(GeoscapeEventSystem __instance)
            {
                try
                {
                    TFTVConfig config = TFTVMain.Main.Config;
                    GeoLevelController controller = __instance.GetComponent<GeoLevelController>();

                    if (TFTVNewGameOptions.MoreAmbushesSetting)
                    {
                        __instance.AmbushExploredSitesProtection = 0;
                        __instance.StartingAmbushProtection = 0;
                        if (TFTVVoidOmens.VoidOmensCheck[1])
                        {
                            __instance.ExplorationAmbushChance = 100;

                        }
                        else
                        {
                            __instance.ExplorationAmbushChance = 70;
                        }
                    }
                    else
                    {
                        if (TFTVVoidOmens.VoidOmensCheck[1])
                        {
                            __instance.ExplorationAmbushChance = 100;

                        }
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }
    }

}

