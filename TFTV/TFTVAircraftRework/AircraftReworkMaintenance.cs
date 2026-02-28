using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Interception.Equipments;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Research = PhoenixPoint.Geoscape.Entities.Research.Research;
using static TFTV.TFTVAircraftReworkMain;
using static TFTV.AircraftReworkHelpers;
using PhoenixPoint.Tactical.View.ViewStates;

namespace TFTV
{

    internal class AircraftReworkMaintenance
    {
        internal class Repairing
        {
            [HarmonyPatch(typeof(GeoVehicle), "RepairAircraftHp")]
            public static class GeoVehicle_RepairAircraftHp_Patch
            {
                public static void Prefix(GeoVehicle __instance, ref int points)
                {
                    try
                    {
                        if (!AircraftReworkOn)
                        {
                            return;
                        }

                        if (!ResearchCompleted(__instance))
                        {
                            points /= 40;
                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                public static void Postfix(GeoVehicle __instance)
                {
                    try
                    {
                        if (!AircraftReworkOn)
                        {
                            return;
                        }

                        AircraftReworkSpeedAndRange.AdjustAircraftSpeed(__instance, true);

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }
        }

        internal static int GetMaintenanceFactor(List<GeoVehicleModuleDef> modules)
        {
            try
            {
                int factor = 4;

                GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                Research phoenixResearch = controller.PhoenixFaction.Research;

                if (modules != null && modules.Any(m => m != null && m == _thunderbirdRangeModule))
                {
                    factor -= 1;

                    foreach (ResearchDef researchDef in _thunderbirdRangeBuffResearchDefs)
                    {
                        if (phoenixResearch.HasCompleted(researchDef.Id))
                        {
                            factor -= 1;
                        }
                    }
                }

                if (modules != null && modules.Any(m => m != null && m == _blimpSpeedModule))
                {
                    factor -= 1;

                    foreach (ResearchDef researchDef in _blimpSpeedModuleBuffResearches)
                    {
                        if (phoenixResearch.HasCompleted(researchDef.Id))
                        {
                            factor -= 1;
                        }
                    }
                }

                if (factor < 0)
                {
                    factor = 0;
                }

                return factor;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        public static void MaintenanceToll(GeoLevelController controller)
        {
            try
            {
                if (!AircraftReworkOn)
                {
                    return;
                }

                foreach (GeoVehicle geoVehicle in controller.PhoenixFaction.Vehicles)
                {
                    if (geoVehicle.Travelling)
                    {
                        List<GeoVehicleModuleDef> modules = geoVehicle.Modules.Select(m => m?.ModuleDef).ToList();
                        int baseMaintenanceLoss = GetMaintenanceFactor(modules);
                        float maintenanceMultiplier = TFTVIncidents.AffinityGeoscapeEffects.GetAircraftMaintenanceLossMultiplier(geoVehicle);
                        int adjustedMaintenanceLoss = Mathf.Max(0, Mathf.RoundToInt(baseMaintenanceLoss * maintenanceMultiplier));
                        geoVehicle.SetHitpoints(geoVehicle.Stats.HitPoints - adjustedMaintenanceLoss);
                    }
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }




        private static bool ResearchCompleted(GeoVehicle geoVehicle)
        {
            try
            {
                GeoPhoenixFaction phoenixFaction = geoVehicle.GeoLevel.PhoenixFaction;

                if (geoVehicle.VehicleDef == manticore)
                {
                    return true;
                }
                else if (geoVehicle.VehicleDef == helios)
                {
                    if (phoenixFaction.Research.HasCompleted("SYN_Aircraft_ResearchDef"))
                    {
                        return true;
                    }
                }
                else if (geoVehicle.VehicleDef == blimp)
                {
                    if (phoenixFaction.Research.HasCompleted("ANU_Blimp_ResearchDef"))
                    {
                        return true;
                    }
                }
                else if (geoVehicle.VehicleDef == thunderbird)
                {
                    if (phoenixFaction.Research.HasCompleted("NJ_Aircraft_ResearchDef"))
                    {
                        return true;
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





        [HarmonyPatch(typeof(GeoVehicle), "SetHitpoints")]
        public static class GeoVehicle_SetHitpoints_Patch
        {
            public static void Postfix(GeoVehicle __instance)
            {
                try
                {
                    if (!AircraftReworkOn)
                    {
                        return;
                    }

                    AircraftReworkSpeedAndRange.AdjustAircraftSpeed(__instance, true);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        public static void AfterLosingAmbushGeoMissionApplyMissionResults(GeoMission geoMission, TacMissionResult result, GeoSquad squad)
        {
            try
            {
                if (!AircraftReworkOn)
                {
                    return;
                }
                bool ambushMission = geoMission.MissionDef.Tags.Contains(Shared.SharedGameTags.AmbushMissionTag);
                bool phoenixLostMission = result.FactionResults.Any(fr => fr.FactionDef == geoMission.Level.PhoenixFaction.FactionDef.PPFactionDef
                    && fr.State == TacFactionState.Defeated);
                TFTVLogger.Always($"Mission results: {geoMission.MissionDef.name} ambush? {ambushMission} lost? {phoenixLostMission}");
                if (ambushMission && phoenixLostMission)
                {
                    geoMission.GetLocalAircraft(squad).SetHitpoints(0);
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        [HarmonyPatch(typeof(GeoVehicle), "OnAircraftBreakingDown")]
        public static class GeoVehicle_OnAircraftBreakingDown_Patch
        {
            public static bool Prefix(GeoVehicle __instance)
            {
                try
                {
                    if (!AircraftReworkOn)
                    {
                        return true;
                    }

                    if (!__instance.IsOwnedByViewer)
                    {
                        return true;
                    }

                    /* GeoSite closestBase = __instance.GeoLevel.Map.ActiveSites.FirstOrDefault(
                         (GeoSite site) => site.Type == GeoSiteType.PhoenixBase &&
                         site.State == GeoSiteState.Functioning &&
                         site.Owner == __instance.GeoLevel.PhoenixFaction);*/

                    GeoSite closestBase = __instance.GeoLevel.Map.ActiveSites
    .Where(site => site.Type == GeoSiteType.PhoenixBase &&
               site.State == GeoSiteState.Functioning &&
               site.Owner == __instance.GeoLevel.PhoenixFaction)
    .OrderBy(site => GeoMap.Distance(__instance, site))
    .FirstOrDefault();



                    Vector3 src = ((__instance.CurrentSite != null) ? __instance.CurrentSite.WorldPosition : __instance.WorldPosition);
                    bool foundPath = false;
                    IList<SitePathNode> source = __instance.Navigation.FindPath(src, closestBase.WorldPosition, out foundPath);


                    List<GeoSite> geoSites = new List<GeoSite>();

                    geoSites.AddRange(from pn in source
                                      where pn.Site != null && pn.Site != __instance.CurrentSite
                                      select pn.Site);
                    __instance.StartTravel(geoSites);

                    __instance.CanRedirect = false;

                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        [HarmonyPatch(typeof(GeoscapeLog), "OnFactionVehicleMaintenaceChanged")]
        public static class GeoscapeLog_OnFactionVehicleMaintenaceChanged
        {
            public static bool Prefix(GeoscapeLog __instance, GeoFaction faction, GeoVehicle vehicle, int oldValue, int newValue)
            {
                try
                {
                    if (!AircraftReworkOn)
                    {
                        return true;
                    }

                    if (newValue < oldValue)
                    {
                        /* if (newValue > 50)
                         {
                             return false;
                         }

                         MethodInfo methodInfo = typeof(GeoscapeLog).GetMethod("AddEntry", BindingFlags.NonPublic | BindingFlags.Instance);

                         LocalizedTextBind localizedTextBindReducedSpeed = new LocalizedTextBind($"Maintenance is at 50%! Speed halved", true);
                         LocalizedTextBind localizedTextBindNeedUrgentRepairs = new LocalizedTextBind($"In need of urgent repairs! Returning to nearest base", true);



                         GeoscapeLogEntry entry = new GeoscapeLogEntry
                         {
                             Text = localizedTextBindReducedSpeed,
                             Parameters = new LocalizedTextBind[1] { new LocalizedTextBind(vehicle.Name, doNotLocalize: true) }
                         };

                         if (newValue == 0)
                         {
                             entry.Text = localizedTextBindNeedUrgentRepairs;     
                         }

                         methodInfo.Invoke(__instance, new object[] { entry, vehicle });
                        */
                        return false;
                    }

                    if (vehicle?.Travelling == true)
                    {
                        return false;
                    }


                    return true;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        /*  private static void AircraftRecoverSpeed(GeoVehicle geoVehicle)
          {
              try
              {
                  if (geoVehicle.Stats.HitPoints > 50)
                  {
                      if (AircraftSpeed.IsAircraftInMist(geoVehicle) || CheckSpeedAdjustedForMaintenance(geoVehicle))
                      {
                          return;
                      }

                      TFTVLogger.Always($"Adjusting speed of {geoVehicle.Name} from {geoVehicle.Stats.Speed}; recovery");

                      geoVehicle.Stats.Speed *= 2;

                      List<GeoSite> _destinationSites = typeof(GeoVehicle).GetField("_destinationSites", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(geoVehicle) as List<GeoSite>;

                      if (geoVehicle.Travelling && _destinationSites.Any())
                      {
                          List<Vector3> path = _destinationSites.Select((GeoSite d) => d.WorldPosition).ToList();
                          geoVehicle.Navigation.Navigate(path);
                      }
                  }
              }
              catch (Exception e)
              {
                  TFTVLogger.Error(e);
              }
          }

          private static void AircraftReduceSpeed(GeoVehicle geoVehicle)
          {
              try
              {
                  if (geoVehicle.Stats.HitPoints <= 50)
                  {
                      if (AircraftSpeed.IsAircraftInMist(geoVehicle) || !CheckSpeedAdjustedForMaintenance(geoVehicle))
                      {
                          return;
                      }

                      TFTVLogger.Always($"Adjusting speed of {geoVehicle.Name} from {geoVehicle.Stats.Speed}; reduction");

                      geoVehicle.Stats.Speed /= 2;

                      List<GeoSite> _destinationSites = typeof(GeoVehicle).GetField("_destinationSites", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(geoVehicle) as List<GeoSite>;

                      if (geoVehicle.Travelling && _destinationSites.Any())
                      {
                          List<Vector3> path = _destinationSites.Select((GeoSite d) => d.WorldPosition).ToList();
                          geoVehicle.Navigation.Navigate(path);
                      }

                  }
              }
              catch (Exception e)
              {
                  TFTVLogger.Error(e);
              }
          }*/



    }



}

