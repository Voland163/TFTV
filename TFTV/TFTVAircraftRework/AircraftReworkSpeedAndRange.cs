using Base.Core;
using HarmonyLib;
using PhoenixPoint.Geoscape;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using UnityEngine;
using static TFTV.AircraftReworkHelpers;
using static TFTV.TFTVAircraftReworkMain;
using TFTV.TFTVIncidents;
using Research = PhoenixPoint.Geoscape.Entities.Research.Research;

namespace TFTV
{

    internal class AircraftReworkSpeedAndRange
    {
        private static Dictionary<GeoVehicle, DateTime> _lastCallTime = new Dictionary<GeoVehicle, DateTime>();

        public static void ClearInternalData()
        {
            try
            {
                _lastCallTime.Clear();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        internal class OverdriveRange
        {

            private static float GetThunderbirdOverdriveRange(Research phoenixResearch)
            {
                try
                {


                    float rangeBuff = _thunderbirdRangeBuffPerLevel;

                    foreach (ResearchDef researchDef in _thunderbirdRangeBuffResearchDefs)
                    {
                        if (phoenixResearch.HasCompleted(researchDef.Id))
                        {
                            rangeBuff += _thunderbirdRangeBuffPerLevel;
                        }
                    }

                    return rangeBuff;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            public static void UpdateThunderbirdRange(GeoVehicle geoVehicle)
            {
                try
                {
                    if (!AircraftReworkOn)
                    {
                        return;
                    }
                    Research research = geoVehicle?.GeoLevel?.PhoenixFaction?.Research;
                    if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _thunderbirdRangeModule) && research != null)
                    {
                        float range = GetThunderbirdOverdriveRange(research);
                        geoVehicle.Stats.MaximumRange.Value = geoVehicle.VehicleDef.BaseStats.MaximumRange.Value + range;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


            /* [HarmonyPatch(typeof(GeoVehicle), "UpdateVehicleBonusCache")]
             internal static class GeoVehicle_UpdateVehicleBonusCache_patch
             {
                 private static void Prefix(GeoVehicle __instance)
                 {
                     try
                     {
                         if (!AircraftReworkOn)
                         {
                             return;
                         }

                         Research research = __instance?.GeoLevel?.PhoenixFaction?.Research;

                         if (__instance.Modules.Any(m => m != null && m.ModuleDef == _thunderbirdRangeModule) && research != null)
                         {
                             float range = GetThunderbirdOverdriveRange(research);
                             _thunderbirdRangeModule.GeoVehicleModuleBonusValue = range;
                         }

                     }
                     catch (Exception e)
                     {
                         TFTVLogger.Error(e);
                         throw;
                     }

                 }
             }*/
        }


        [HarmonyPatch(typeof(GeoVehicle), "UpdateVehicleStats")]
        public static class GeoVehicle_UpdateVehicleStats_Patch
        {
            static void Postfix(GeoVehicle __instance)
            {

                try
                {
                    if (!AircraftReworkOn && __instance != null)
                    {
                        return;
                    }

                    AdjustAircraftSpeed(__instance, true);
                    OverdriveRange.UpdateThunderbirdRange(__instance);

                    TFTVLogger.Always($"[GeoVehicleStatModifier.UpdateBaseVehicleStats] new speed: {__instance?.Stats?.Speed}");

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }



        internal static void Init(GeoLevelController controller)
        {
            try
            {
                if (!AircraftReworkOn)
                {
                    return;
                }

                foreach (GeoVehicle geoVehicle in controller.PhoenixFaction.Vehicles)
                {
                    AdjustAircraftSpeed(geoVehicle, true);
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static float GetBlimpSpeedBonus()
        {
            try
            {
                Research phoenixResearch = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction.Research;
                return Tiers.GetBlimpSpeedTier() * _mistSpeedModuleBuff;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

        internal static float GetThunderbirdOverdriveSpeed()
        {
            try
            {
                Research phoenixResearch = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>()?.PhoenixFaction?.Research;
                 
                float speedBuff = _thunderbirdSpeedBuffPerLevel;

                if (phoenixResearch != null)
                {
                    foreach (ResearchDef researchDef in _thunderbirdRangeBuffResearchDefs)
                    {
                        if (phoenixResearch.HasCompleted(researchDef.Id))
                        {
                            speedBuff += _thunderbirdSpeedBuffPerLevel;
                        }
                    }
                }

                return speedBuff;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }





        internal static float GetHeliosSpeed()
        {
            try
            {
                Research phoenixResearch = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>()?.PhoenixFaction?.Research;
                float speedBuff = 0;

                if (phoenixResearch != null)
                {
                    foreach (ResearchDef researchDef in _heliosSpeedBuffResearchDefs)
                    {
                        if (phoenixResearch.HasCompleted(researchDef.Id))
                        {
                            speedBuff += _heliosSpeedBuffPerLevel;
                        }
                    }
                }
                return speedBuff;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        public static void AdjustAircraftSpeed(GeoVehicle geoVehicle, bool forceTimer = false)
        {
            try
            {
                if (!AircraftReworkOn)
                {
                    return;
                }


                if (!forceTimer)
                {
                    DateTime currentTime = geoVehicle.GeoLevel.Timing.Now.DateTime;

                    if (_lastCallTime.ContainsKey(geoVehicle) && (currentTime - _lastCallTime[geoVehicle]).TotalMinutes < 10)
                    {
                        return;
                    }

                    if (_lastCallTime.ContainsKey(geoVehicle))
                    {
                        _lastCallTime[geoVehicle] = currentTime;
                    }
                    else
                    {
                        _lastCallTime.Add(geoVehicle, currentTime);
                    }
                }

                if (IsAircraftInMist(geoVehicle))
                {
                    if (CheckRightSpeedForMist(geoVehicle))
                    {
                        return;
                    }
                    else
                    {
                        ResetSpeed(geoVehicle, true);
                    }
                }
                else if (!CheckRightSpeedForOutsideMist(geoVehicle))
                {
                    ResetSpeed(geoVehicle);
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }



        private static bool CheckRightSpeedForMist(GeoVehicle geoVehicle)
        {
            try
            {
                float speed = GetSpeedInMist(geoVehicle, GetSpeedOutsideOfMist(geoVehicle));

                if (geoVehicle.Speed.Value == speed)
                {
                    return true;
                }
                else
                {
                    return false;
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static bool CheckRightSpeedForOutsideMist(GeoVehicle geoVehicle)
        {
            try
            {
                float speed = GetSpeedOutsideOfMist(geoVehicle);
                if (geoVehicle.Speed.Value == speed)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        private static float GetSpeedInMist(GeoVehicle geoVehicle, float speedOutsideOfMist)
        {
            try
            {
                float speed = speedOutsideOfMist;

                if (geoVehicle.VehicleDef != blimp)
                {
                    float penaltyMultiplier = TFTVIncidents.AffinityGeoscapeEffects.GetMistPenaltyMultiplier(geoVehicle);
                    speed *= 1 - (_mistSpeedMalus * penaltyMultiplier);

                    // TFTVLogger.Always($"speed in Mist of {geoVehicle.Name}, after applying malus of {_mistSpeedMalus} is {speed}", false);
                }
                else
                {
                    float buffMultiplier = TFTVIncidents.AffinityGeoscapeEffects.GetMistBuffMultiplier(geoVehicle);
                    speed += _mistSpeedModuleBuff * buffMultiplier;
                    // TFTVLogger.Always($"speed in Mist of {geoVehicle.Name}, after adding {_mistSpeedModuleBuff} is {speed}", false);
                }



                if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _blimpSpeedModule))
                {
                    speed += GetBlimpSpeedBonus();
                }

                //  TFTVLogger.Always($"speed in Mist of {geoVehicle.Name}, after applying bonus/malus and bonus from Bioflux, if there is one: {speed}", false);

                return speed;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static float GetSpeedOutsideOfMist(GeoVehicle geoVehicle)
        {
            try
            {
                float baseSpeed = geoVehicle.VehicleDef.BaseStats.Speed.Value;
                float totalSpeed = baseSpeed;

                //   TFTVLogger.Always($"SpeedOutsideOfMist for {geoVehicle.Name}");
                //  TFTVLogger.Always($"Base speed is {baseSpeed}", false);

                if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _basicSpeedModule))
                {
                    totalSpeed += _basicSpeedModule.GeoVehicleModuleBonusValue;
                    // TFTVLogger.Always($"Basic speed module bonus is {_basicSpeedModule.GeoVehicleModuleBonusValue}, so now speed is {totalSpeed}", false);
                }
                if (geoVehicle.VehicleDef == helios)
                {
                    totalSpeed += GetHeliosSpeed();
                    //  TFTVLogger.Always($"Helios speed bonus is {GetHeliosSpeed()}, so now speed is {totalSpeed}", false);
                }
                if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _thunderbirdRangeModule))
                {
                    totalSpeed += GetThunderbirdOverdriveSpeed();
                    //  TFTVLogger.Always($"Thunderbird speed bonus is {GetThunderbirdOverdriveSpeed()}, so now speed is {totalSpeed}", false);
                }
                if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _basicPassengerModule))
                {
                    totalSpeed += _basicPassengerModule.GeoVehicleModuleBonusValue;
                    //  TFTVLogger.Always($"Basic passenger module bonus is {_basicPassengerModule.GeoVehicleModuleBonusValue}, so now speed is {totalSpeed}", false);
                }
                if (geoVehicle.Stats.HitPoints <= _maintenanceSpeedThreshold)
                {
                    totalSpeed /= 2;
                    // TFTVLogger.Always($"Hitpoints are below {_maintenanceSpeedThreshold}, so speed is halved to {totalSpeed}", false);
                }
                //  TFTVLogger.Always($"Final speed outside of Mist for {geoVehicle.Name} is {totalSpeed}", false);

                if (geoVehicle.Owner is GeoPhoenixFaction)
                {
                    float affinityMultiplier = AffinityGeoscapeEffects.GetAircraftSpeedMultiplier(geoVehicle);
                    totalSpeed *= affinityMultiplier;
                }

                return totalSpeed;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static void ResetSpeed(GeoVehicle geoVehicle, bool inMist = false)
        {
            try
            {
                List<GeoSite> _destinationSites = typeof(GeoVehicle).GetField("_destinationSites", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(geoVehicle) as List<GeoSite>;

                float speed = 0;


                if (inMist)
                {
                    speed = GetSpeedInMist(geoVehicle, GetSpeedOutsideOfMist(geoVehicle));
                }
                else
                {
                    speed = GetSpeedOutsideOfMist(geoVehicle);
                }

                TFTVLogger.Always($"resetting speed for {geoVehicle.Name}, inMist? {inMist}. current speed is {geoVehicle.Stats.Speed.Value}, should be {speed}");

                geoVehicle.Stats.Speed.Value = speed;

                if (geoVehicle.Travelling && _destinationSites.Any())
                {
                    List<Vector3> path = _destinationSites.Select((GeoSite d) => d.WorldPosition).ToList();
                    geoVehicle.Navigation.Navigate(path);
                }
                
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        public static bool IsAircraftInMist(GeoVehicle aircraft)
        {
            try
            {
                GeoLevelController controller = aircraft.GeoLevel;
                MistRendererSystem mistRendererSystem = controller.MistRenderComponent;

                if (aircraft.CurrentSite!=null && aircraft.CurrentSite.GetComponent<GeoPhoenixBase>()!=null)
                {
                    GeoSite site = aircraft.CurrentSite;

                    if (site.IsInMist && !site.IsInMistRepeller) 
                    {
                        return true;
                    }
                }


                if (mistRendererSystem == null)
                {
                    return false;
                }

                Vector2Int resolution = (Vector2Int)typeof(MistRendererSystem)
                    .GetField("Resolution", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(mistRendererSystem);
                NativeArray<byte> mistData = (NativeArray<byte>)typeof(MistRendererSystem)
                    .GetField("_mistData", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(mistRendererSystem);

                //   TFTVLogger.Always($"resolution null? {resolution == null}");
                //   TFTVLogger.Always($"mistData null? {mistData == null}");

                Vector2 uv = MistRendererSystem.CoordsToUV((GeoActor)aircraft);

                //   TFTVLogger.Always($"uv null? {uv == null}");

                Vector2 coords;
                coords.x = Mathf.RoundToInt(uv.x * resolution.x);
                coords.y = Mathf.RoundToInt(uv.y * resolution.y);

                int index = (int)(coords.x + resolution.x * coords.y);

                if (!mistData.IsCreated)
                {
                    return false;
                }

                if (index >= mistData.Length / 4)
                {
                    TFTVLogger.Always("Actor " + aircraft.name + " has invalid coordinates");
                    return false;
                }

                bool isInMist = mistData[index * 4 + 3] > 0;
                return isInMist;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }
    }



}

