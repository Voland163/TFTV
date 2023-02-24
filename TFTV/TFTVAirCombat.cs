using Base;
using Base.Core;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static PhoenixPoint.Geoscape.Entities.GeoBehemothActor;

namespace TFTV
{
    internal class TFTVAirCombat
    {

        // private static readonly DefRepository Repo = TFTVMain.Repo;
        public static Dictionary<int, List<int>> flyersAndHavens = new Dictionary<int, List<int>>();
        public static List<int> targetsForBehemoth = new List<int>();
        //  public static List<int> targetsVisitedByBehemoth = new List<int>();

        public static List<int> behemothScenicRoute = new List<int>();
        public static int behemothTarget = 0;
        public static int behemothWaitHours = 12;
        // public static int roaming = 0;
        //public static bool firstPandoranFlyerSpawned = false;

        public static bool checkHammerfall = false;
        private static readonly string BehemothRoamings = "BehemothRoamings";

        //Trigger event on first Pandoran flyer
        [HarmonyPatch(typeof(GeoFaction), "CreateVehicleAtPosition")]
        public static class GeoFaction_CreateVehicleAtPosition_patch
        {
            public static void Postfix(ComponentSetDef vehicleDef, GeoFaction __instance)
            {
                try
                {
                    // TFTVLogger.Always("Method CreateVehicleAtPosition is inovked, re vehicleDef" + vehicleDef.name);

                    if (vehicleDef.name == "ALN_GeoscapeFlyer_Small" && __instance.GeoLevel.EventSystem.GetVariable("FirstPandoranFlyerSpawned") != 1)
                    {
                        // TFTVLogger.Always("If check passed");
                        // firstPandoranFlyerSpawned=true;
                        GeoscapeEventContext geoscapeEventContext = new GeoscapeEventContext(__instance, __instance.GeoLevel.ViewerFaction);
                        __instance.GeoLevel.EventSystem.TriggerGeoscapeEvent("OlenaOnFirstFlyer", geoscapeEventContext);
                        __instance.GeoLevel.EventSystem.SetVariable("FirstPandoranFlyerSpawned", 1);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

        }

        //Behemoth roaming
        [HarmonyPatch(typeof(GeoBehemothActor), "OnBehemothEmerged")]
        public static class GeoBehemothActor_OnBehemothEmerged_patch
        {
            public static bool Prepare()
            {
                TFTVConfig config = TFTVMain.Main.Config;
                return config.ActivateAirCombatChanges;
            }
            public static void Postfix(GeoBehemothActor __instance)

            {
                try
                {
                    TFTVLogger.Always("Behemoth emerging");

                    __instance.GeoLevel.EventSystem.SetVariable(BehemothRoamings, __instance.GeoLevel.EventSystem.GetVariable(BehemothRoamings) + 1);

                    if (__instance.GeoLevel.PhoenixFaction.Research.HasCompleted("PX_YuggothianEntity_ResearchDef")
                        && __instance.GeoLevel.PhoenixFaction.Research.HasCompleted("PX_Alien_Citadel_ResearchDef")
                        && __instance.GeoLevel.EventSystem.GetVariable("BehemothPatternEventTriggered") != 1)
                    {
                        GeoscapeEventContext context = new GeoscapeEventContext(__instance.GeoLevel.AlienFaction, __instance.GeoLevel.PhoenixFaction);
                        __instance.GeoLevel.EventSystem.TriggerGeoscapeEvent("OlenaOnBehemothPattern", context);
                        __instance.GeoLevel.EventSystem.SetVariable("BehemothPatternEventTriggered", 1);
                        TFTVLogger.Always("Event on Behemoth pattern should trigger");

                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }

        }

        //Hammerfall
        [HarmonyPatch(typeof(GeoAlienFaction), "SpawnEgg", new Type[] { typeof(Vector3) })]
        public static class GeoAlienFaction_SpawnEgg_DestroyHavens_Patch
        {

            public static void Postfix(GeoAlienFaction __instance, Vector3 worldPos)
            {
                try
                {
                    if (!checkHammerfall)
                    {

                        List<GeoHaven> geoHavens = __instance.GeoLevel.AnuFaction.Havens.ToList();
                        geoHavens.AddRange(__instance.GeoLevel.NewJerichoFaction.Havens.ToList());
                        geoHavens.AddRange(__instance.GeoLevel.SynedrionFaction.Havens.ToList());
                        int count = 0;
                        int damage = UnityEngine.Random.Range(25, 200);

                        foreach (GeoHaven haven in geoHavens)
                        {
                            //TFTVLogger.Always("Got Here");
                            if (Vector3.Distance(haven.Site.WorldPosition, worldPos) <= 1)

                            {
                                // TFTVLogger.Always("This haven " + haven.Site.LocalizedSiteName + "is getting whacked by the asteroid");
                                if (!haven.Site.HasActiveMission && count < 3 && Vector3.Distance(haven.Site.WorldPosition, worldPos) <= 0.4)
                                {
                                    GeoscapeLogEntry entry = new GeoscapeLogEntry
                                    {
                                        Text = new LocalizedTextBind(haven.Site.Owner + " " + haven.Site.LocalizedSiteName + " was destroyed by Hammerfall!", true)
                                    };
                                    typeof(GeoscapeLog).GetMethod("AddEntry", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance.GeoLevel.Log, new object[] { entry, null });
                                    haven.Site.DestroySite();
                                    count++;
                                }
                                else
                                {
                                    int startingPopulation = haven.Population;
                                    float havenPopulation = haven.Population * (float)(Vector3.Distance(haven.Site.WorldPosition, worldPos));
                                    haven.Population = Mathf.CeilToInt(havenPopulation);
                                    int damageToZones = Mathf.CeilToInt(150 / (Vector3.Distance(haven.Site.WorldPosition, worldPos)));
                                    haven.Zones.ToArray().ForEach(zone => zone.AddDamage(UnityEngine.Random.Range(damageToZones - 25, damageToZones + 25)));
                                    string destructionDescription;
                                    if (haven.Zones.First().Health <= 500 || startingPopulation >= haven.Population + 1000)
                                    {
                                        destructionDescription = " suffered heavy damage from Harmmerfall!";
                                    }
                                    else
                                    {
                                        destructionDescription = " suffered some damage from Hammerfall";

                                    }
                                    GeoscapeLogEntry entry = new GeoscapeLogEntry
                                    {
                                        Text = new LocalizedTextBind(haven.Site.Owner + " " + haven.Site.LocalizedSiteName + destructionDescription, true)
                                    };
                                    typeof(GeoscapeLog).GetMethod("AddEntry", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance.GeoLevel.Log, new object[] { entry, null });
                                    checkHammerfall = true;
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

        }

        //patch to reveal havens under attack
        [HarmonyPatch(typeof(GeoscapeRaid), "StartAttackEffect")]
        public static class GeoscapeRaid_StartAttackEffect_patch
        {
            public static bool Prepare()
            {
                TFTVConfig config = TFTVMain.Main.Config;
                return config.HavenSOS;
            }

            public static void Postfix(GeoscapeRaid __instance)
            {
                try
                {
                    __instance.GeoVehicle.CurrentSite.RevealSite(__instance.GeoVehicle.GeoLevel.PhoenixFaction);
                    GeoscapeLogEntry entry = new GeoscapeLogEntry
                    {
                        Text = new LocalizedTextBind(__instance.GeoVehicle.CurrentSite.Owner + " " + __instance.GeoVehicle.CurrentSite.LocalizedSiteName + " is broadcasting an SOS, they are under attack!", true)
                    };
                    typeof(GeoscapeLog).GetMethod("AddEntry", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance.GeoVehicle.GeoLevel.Log, new object[] { entry, null });
                    __instance.GeoVehicle.GeoLevel.View.SetGamePauseState(true);
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        //Controlling Pandoran flyers visiting havens
        [HarmonyPatch(typeof(GeoVehicle), "OnArrivedAtDestination")]
        public static class GeoVehicle_OnArrivedAtDestination
        {
            public static bool Prepare()
            {
                TFTVConfig config = TFTVMain.Main.Config;
                return config.ActivateAirCombatChanges;
            }

            public static void Postfix(GeoVehicle __instance, bool justPassing)
            {
                try
                {
                    if (!justPassing && __instance.Owner.IsAlienFaction && __instance.CurrentSite.Type == GeoSiteType.Haven)
                    {

                        if (flyersAndHavens.ContainsKey(__instance.VehicleID))
                        {
                            flyersAndHavens[__instance.VehicleID].Add(__instance.CurrentSite.SiteId);
                        }
                        else
                        {
                            flyersAndHavens.Add(__instance.VehicleID, new List<int> { __instance.CurrentSite.SiteId });
                        }


                        TFTVLogger.Always("Added to list of havens visited by flyer " + __instance.CurrentSite.LocalizedSiteName);
                    }

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

        }

        //Clear lists of internal variables on Behemoth submerge + add Berith and Abbadon researches depending on number of roamings
        [HarmonyPatch(typeof(GeoBehemothActor), "PickSubmergeLocation")]
        public static class GeoBehemothActor_PickSubmergeLocation_patch
        {

            public static bool Prepare()
            {
                TFTVConfig config = TFTVMain.Main.Config;
                return config.ActivateAirCombatChanges;
            }
            public static void Postfix(GeoBehemothActor __instance)


            {
                try
                {
                    TFTVLogger.Always("Behemoth submerging");
                    //  BehemothSubmerging = true;
                    flyersAndHavens.Clear();
                    targetsForBehemoth.Clear();
                    behemothScenicRoute.Clear();
                    //  BehemothSubmerging = true;
                    if (__instance.GeoLevel.EventSystem.GetVariable(BehemothRoamings) < 1)//4 - __instance.GeoLevel.CurrentDifficultyLevel.Order <= roaming) 
                    {
                        __instance.GeoLevel.EventSystem.SetVariable("BerithResearchVariable", 1);
                        TFTVLogger.Always("Aliens should now have Beriths");
                    }
                    else if (__instance.GeoLevel.EventSystem.GetVariable(BehemothRoamings) == 2)//4 - __instance.GeoLevel.CurrentDifficultyLevel.Order <= roaming) 
                    {
                        __instance.GeoLevel.EventSystem.SetVariable("AbbadonResearchVariable", 1);
                        TFTVLogger.Always("Aliens should now have Abbadons");
                    }
                    else if (__instance.GeoLevel.EventSystem.GetVariable(BehemothRoamings) == 3)//4 - __instance.GeoLevel.CurrentDifficultyLevel.Order <= roaming) 
                    {

                        if (__instance.GeoLevel.EventSystem.GetVariable("BehemothPatternEventTriggered") == 0)
                        {
                            GeoscapeEventContext context = new GeoscapeEventContext(__instance.GeoLevel.AlienFaction, __instance.GeoLevel.PhoenixFaction);
                            __instance.GeoLevel.EventSystem.TriggerGeoscapeEvent("OlenaOnBehemothPattern", context);
                            __instance.GeoLevel.EventSystem.SetVariable("BehemothPatternEventTriggered", 1);
                            TFTVLogger.Always("Event on Behemoth pattern should trigger");

                        }
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }

        }

        //Verifiying if flyer returning to Behemoth has visited a haven
        [HarmonyPatch(typeof(GeoscapeRaid), "StopBehemothFollowing")]
        public static class GeoscapeRaid_StopBehemothFollowing_patch
        {
            public static bool Prepare()
            {
                TFTVConfig config = TFTVMain.Main.Config;
                return config.ActivateAirCombatChanges;
            }
            public static void Prefix(GeoscapeRaid __instance)
            {
                try
                {
                    GeoBehemothActor behemoth = (GeoBehemothActor)UnityEngine.Object.FindObjectOfType(typeof(GeoBehemothActor));
                    // TFTVLogger.Always("Behemoth is submerging? " + behemoth.IsSubmerging);

                    if (flyersAndHavens.ContainsKey(__instance.GeoVehicle.VehicleID))
                    {
                        TFTVLogger.Always("Flyer returning to B passed first check");

                        foreach (int haven in flyersAndHavens[__instance.GeoVehicle.VehicleID])
                        {
                            TFTVLogger.Always("Checking each haven visited by the flyer");

                            if (!targetsForBehemoth.Contains(haven)) //&& !targetsVisitedByBehemoth.Contains(haven)) //&& (behemoth != null && !behemoth.IsSubmerging && behemoth.CurrentBehemothStatus != GeoBehemothActor.BehemothStatus.Dormant))
                            {
                                targetsForBehemoth.Add(haven);

                                TFTVLogger.Always("Haven " + haven + " added to the list of targets");
                            }
                        }
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        public static GeoSite GetTargetHaven(GeoLevelController level)
        {
            try
            {
                List<GeoHaven> geoHavens = level.AnuFaction.Havens.ToList();
                geoHavens.AddRange(level.NewJerichoFaction.Havens.ToList());
                geoHavens.AddRange(level.SynedrionFaction.Havens.ToList());

                int idOfHaven = targetsForBehemoth.First();
                GeoSite target = new GeoSite();
                foreach (GeoHaven haven in geoHavens)
                {
                    if (haven.Site.SiteId == idOfHaven)
                    {
                        target = haven.Site;
                    }
                }
                return target;


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }

        public static GeoSite ConvertIntIDToGeosite(GeoLevelController controller, int siteID)
        {
            try
            {
                List<GeoSite> allGeoSites = controller.Map.AllSites.ToList();
                foreach (GeoSite site in allGeoSites)
                {
                    if (site != null && site.SiteId == siteID)
                    {
                        return site;
                    }
                }
                behemothTarget = 0;
                return null;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }

        [HarmonyPatch(typeof(GeoBehemothActor), "UpdateHourly")]
        public static class GeoBehemothActor_UpdateHourly_Patch
        {

            public static bool Prepare()
            {
                TFTVConfig config = TFTVMain.Main.Config;
                return config.ActivateAirCombatChanges;
            }

            public static bool Prefix(GeoBehemothActor __instance, ref int ____disruptionThreshhold, int ____disruptionPoints, int ____nextActionHoursLeft)
            {
                try
                {
                    //TFTVLogger.Always("Total sites count is " + __instance.GeoLevel.Map.AllSites.Count);

                    GeoLevelController controller = __instance.GeoLevel;

                    if (__instance.CurrentBehemothStatus == GeoBehemothActor.BehemothStatus.Dormant)//first check
                    {
                        //   TFTVLogger.Always("Behemoth's target lists are cleared because he is sleeping");
                        targetsForBehemoth.Clear();
                        //  targetsVisitedByBehemoth.Clear();
                        behemothScenicRoute.Clear();
                        behemothTarget = 0;
                        return true;
                    }

                    /*  if (__instance.GeoLevel.EventSystem.GetVariable("ThirdActStarted") == 1)
                      {
                          ____disruptionThreshhold = 200;
                      }*/

                    if (____disruptionThreshhold <= 0)
                    {
                        FesteringSkiesSettingsDef festeringSkiesSettings = __instance.GeoLevel.FesteringSkiesSettings;
                        GameDifficultyLevelDef currentDifficultyLevel = __instance.GeoLevel.CurrentDifficultyLevel;
                        int num = festeringSkiesSettings.DisruptionThreshholdBaseValue + currentDifficultyLevel.DisruptionDueToDifficulty;
                        //  TFTVLogger.Always("The num is " + num);

                        foreach (BonusesToDisruptionMeter researchDisruptionBonuse in festeringSkiesSettings.ResearchDisruptionBonuses)
                        {
                            if (__instance.GeoLevel.AlienFaction.Research.HasCompleted(researchDisruptionBonuse.ResearchDefId))
                            {
                                num += researchDisruptionBonuse.DisruptionBonus;
                            }
                        }
                        ____disruptionThreshhold = num;

                        // BehemothDisplayController behemothDisplayController = (BehemothDisplayController)UnityEngine.Object.FindObjectOfType(typeof(BehemothDisplayController));
                        // behemothDisplayController.UpdateDisruptionMeter(____disruptionPoints, ____disruptionThreshhold);


                        TFTVLogger.Always("Behemoth hourly update, disruption threshold set to " + ____disruptionThreshhold + ", disruption points are " + ____disruptionPoints);
                    }

                    if (!__instance.IsSubmerging && ____disruptionPoints >= ____disruptionThreshhold)
                    {
                        if (__instance.CurrentSite != null)
                        {

                            MethodInfo method_GenerateTargetData = AccessTools.Method(typeof(GeoBehemothActor), "PickSubmergeLocation");

                            method_GenerateTargetData.Invoke(__instance, null);
                            TFTVLogger.Always("Behemoth hourly update, disruption points at " + ____disruptionPoints + ", while threshold set to " + ____disruptionThreshhold + ". Behemoth should submerge");
                            return false;
                        }
                    }

                    ____nextActionHoursLeft = Mathf.Clamp(____nextActionHoursLeft - 1, 0, int.MaxValue);

                    if (____nextActionHoursLeft <= 0)
                    {
                        MethodInfo method_GenerateTargetData = AccessTools.Method(typeof(GeoBehemothActor), "PerformAction");
                        method_GenerateTargetData.Invoke(__instance, null);
                        TFTVLogger.Always("Behemoth hourly update, " + ____nextActionHoursLeft + " hours left to move, so time to move");

                    }


                    if (__instance.IsSubmerging)//second check
                    {
                        // TFTVLogger.Always("Behemoth's target lists are cleared because he is going to sleep");
                        targetsForBehemoth.Clear();
                        behemothScenicRoute.Clear();
                        behemothTarget = 0;
                        return false;
                    }

                    if (behemothTarget != 0 && ConvertIntIDToGeosite(__instance.GeoLevel, behemothTarget) != null && ConvertIntIDToGeosite(__instance.GeoLevel, behemothTarget).State == GeoSiteState.Destroyed)
                    {
                        behemothTarget = 0;
                    }
                    else if (behemothTarget != 0)
                    {
                        // TFTVLogger.Always("TargetHavenEvent should trigger");
                        if (__instance.GeoLevel.EventSystem.GetVariable("BehemothTargettedFirstHaven") != 1)
                        {
                            GeoscapeEventContext geoscapeEventContext = new GeoscapeEventContext(__instance.GeoLevel.AlienFaction, __instance.GeoLevel.ViewerFaction);
                            __instance.GeoLevel.EventSystem.TriggerGeoscapeEvent("OlenaOnFirstHavenTarget", geoscapeEventContext);
                            __instance.GeoLevel.EventSystem.SetVariable("BehemothTargettedFirstHaven", 1);
                            TFTVLogger.Always("OlenaOnFirstHavenTarget event triggered");
                        }
                    }

                    if (behemothTarget == 0 && targetsForBehemoth.Count > 0)
                    {
                        TFTVLogger.Always("Behemoth has no current target and there are " + targetsForBehemoth.Count() + " available targets");

                        GeoSite chosenHaven = GetTargetHaven(__instance.GeoLevel);
                        targetsForBehemoth.Remove(chosenHaven.SiteId);
                        behemothTarget = chosenHaven.SiteId;
                        if (__instance.CurrentSite != null && __instance.CurrentSite == chosenHaven && targetsForBehemoth.Count > 0)
                        {

                            chosenHaven = GetTargetHaven(__instance.GeoLevel);
                            targetsForBehemoth.Remove(chosenHaven.SiteId);
                            behemothTarget = chosenHaven.SiteId;

                        }
                        else if (__instance.CurrentSite != null && __instance.CurrentSite == chosenHaven && targetsForBehemoth.Count == 0)
                        {
                            TFTVLogger.Always("Behemoth is at a haven, the target is the haven and has no other targets: has to move somewhere");
                            typeof(GeoBehemothActor).GetMethod("TargetHaven", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { GetSiteForBehemothToMoveTo(__instance) });
                            return false;
                        }

                        typeof(GeoBehemothActor).GetMethod("TargetHaven", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { chosenHaven });
                        return false;

                    }
                    else if (behemothTarget == 0 && targetsForBehemoth.Count == 0) // no potential targets, set Behemoth to roam
                    {
                        if (behemothWaitHours == 0)
                        {
                            TFTVLogger.Always("No targets, waiting time is up, Behemoth moves somewhere");
                            if (GetSiteForBehemothToMoveTo(__instance) != null)
                            {
                                typeof(GeoBehemothActor).GetMethod("TargetHaven", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { GetSiteForBehemothToMoveTo(__instance) });
                                behemothWaitHours = 12;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            behemothWaitHours--;
                        }
                    }
                    return false;
                }//end of try

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
                return true;
            }
        }



        //Patch to ensure that Behemoth emerges near exploration sites, written with the help of my new best friend, chatgpt
        [HarmonyPatch(typeof(GeoBehemothActor), "OnBehemothEmerged")]
        class TFTV_OnBehemothEmerged_Patch
        {

            public static bool Prefix(GeoBehemothActor __instance, ref int ____disruptionPoints, ref int ____disruptionThreshhold, ref BehemothStatus ____currentBehemothStatus,
                 ref int ____nextActionHoursLeft, ref Vector3 ____submergeEmergeEndPoint, ref Vector3 ____submergeEmergeStartPoint,
                 IUpdateable ____submergeOrEmergeCrt, GameObject ____submergeOrEmergeVfx)
            {
                try
                {
                    GeoLevelController controller = __instance.GeoLevel;

                    TFTVLogger.Always("OnBehemothEmerged invoked");
                    GeoSite randomElement = __instance.GeoLevel.Map.SitesByType[GeoSiteType.MistGenerator]
                        .Where(s => s != __instance.CurrentSite)
                        .Where(s => controller.Map.SitesByType[GeoSiteType.Exploration]
                            .Count(e => Vector3.Distance(e.WorldPosition, s.WorldPosition) <= 5) >= 5)
                         .Where(s => controller.Map.SitesByType[GeoSiteType.Haven]
                             .Count(e => Vector3.Distance(e.WorldPosition, s.WorldPosition) <= 5) >= 5)
                        .ToList().GetRandomElement();

                    TFTVLogger.Always("Random Element is " + randomElement.SiteId);

                    Type targetType = typeof(GeoBehemothActor);
                    FieldInfo eventField = targetType.GetField("OnEmerged", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    BehemothSiteEventHandler OnEmerged = (BehemothSiteEventHandler)eventField.GetValue(__instance);

                    if (randomElement == null) return false;

                    __instance.TeleportToSite(randomElement);

                    // rest of the original method, adapted for patch
                    __instance.ModelRoot.transform.localPosition = new Vector3(0f, -0.375f, 0f);
                    __instance.VisualsRoot.gameObject.SetActive(value: true);
                    OnEmerged?.Invoke(__instance.CurrentSite);
                    __instance.GeoLevel.View.ChaseTarget(__instance.CurrentSite, instant: true);
                    ____currentBehemothStatus = BehemothStatus.None;
                    ____nextActionHoursLeft = 0;

                    MethodInfo performActionMethod = AccessTools.Method(typeof(GeoBehemothActor), "PerformAction");
                    performActionMethod.Invoke(__instance, null);

                    MethodInfo getEmergePointMethod = AccessTools.Method(typeof(GeoBehemothActor), "GetEmergePoint");
                    ____submergeEmergeEndPoint = (Vector3)getEmergePointMethod.Invoke(__instance, null);
                    ____submergeEmergeStartPoint = __instance.WorldPosition;

                    MethodInfo EmergeCrtMethod = AccessTools.Method(typeof(GeoBehemothActor), "EmergeCrt");
                    object emergeCrtResult = EmergeCrtMethod.Invoke(__instance, new object[] { new Timing() });

                    if (__instance.BehemothDef.EmergeVFX != null)
                    {
                        ____submergeOrEmergeVfx = UnityEngine.Object.Instantiate(__instance.BehemothDef.EmergeVFX, __instance.VFXRoot);
                        ____submergeOrEmergeVfx.transform.localPosition = Vector3.zero;
                    }

                    MethodInfo CalculateDisruptionThreshholdMethod = AccessTools.Method(typeof(GeoBehemothActor), "CalculateDisruptionThreshhold");

                    ____disruptionPoints = 0;
                    ____disruptionThreshhold = (int)CalculateDisruptionThreshholdMethod.Invoke(__instance, null);

                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);

                }
                return true;

            }
        }


        [HarmonyPatch(typeof(GeoBehemothActor), "DamageHavenOutcome")]

        public static class GeoBehemothActor_DamageHavenOutcome_Patch
        {
            public static bool Prepare()
            {
                TFTVConfig config = TFTVMain.Main.Config;
                return config.ActivateAirCombatChanges;
            }

            public static void Postfix(GeoBehemothActor __instance, ref int ____disruptionPoints)
            {
                try
                {
                    TFTVLogger.Always("DamageHavenOutcome method invoked");

                    if (__instance.GeoLevel.EventSystem.GetVariable("BehemothAttackedFirstHaven") != 1)
                    {
                        GeoscapeEventContext geoscapeEventContext = new GeoscapeEventContext(__instance.GeoLevel.PhoenixFaction, __instance.GeoLevel.ViewerFaction);
                        __instance.GeoLevel.EventSystem.TriggerGeoscapeEvent("OlenaOnFirstHavenAttack", geoscapeEventContext);
                        __instance.GeoLevel.EventSystem.SetVariable("BehemothAttackedFirstHaven", 1);
                        TFTVLogger.Always("FirstHavenTarget event triggered");
                    }


                    behemothTarget = 0;
                    ____disruptionPoints += 1;
                    TFTVLogger.Always("The DP are " + ____disruptionPoints);
                    // TFTVLogger.Always("DamageHavenOutcome method invoked and Behemoth target is now " + behemothTarget);
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }

        [HarmonyPatch(typeof(GeoBehemothActor), "ChooseNextHavenTarget")]
        public static class GeoBehemothActor_ChooseNextHavenTarget_Patch
        {
            public static bool Prepare()
            {
                TFTVConfig config = TFTVMain.Main.Config;
                return config.ActivateAirCombatChanges;
            }

            public static bool Prefix(GeoBehemothActor __instance)
            {
                try
                {
                    if (targetsForBehemoth.Count == 0 && behemothTarget == 0)
                    {
                        GeoSite site = GetSiteForBehemothToMoveTo(__instance);
                        typeof(GeoBehemothActor).GetMethod("TargetHaven", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { site });

                        return false;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
                return false;
            }
        }

        public static GeoSite GetSiteForBehemothToMoveTo(GeoBehemothActor geoBehemothActor)

        {
            try
            {
                TFTVLogger.Always("TargetsForBehemoth counts " + targetsForBehemoth.Count() + " and/but counted as 0, so here we are");
                List<GeoHaven> geoHavens = geoBehemothActor.GeoLevel.AnuFaction.Havens.ToList();
                geoHavens.AddRange(geoBehemothActor.GeoLevel.NewJerichoFaction.Havens.ToList());
                geoHavens.AddRange(geoBehemothActor.GeoLevel.SynedrionFaction.Havens.ToList());
                List<GeoSite> geoSites = new List<GeoSite>();
                GeoSite chosenTarget = geoBehemothActor.GeoLevel.Map.GetClosestSite_Land(geoBehemothActor.WorldPosition);

                foreach (GeoHaven haven in geoHavens)
                {
                    if (Vector3.Distance(haven.Site.WorldPosition, geoBehemothActor.WorldPosition) <= 5)
                    {
                        geoSites.Add(haven.Site);
                    }
                }

                if (geoSites.Count == 0)
                {
                    foreach (GeoHaven haven in geoHavens)
                    {
                        if (Vector3.Distance(haven.Site.WorldPosition, geoBehemothActor.WorldPosition) <= 15)
                        {
                            geoSites.Add(haven.Site);
                        }
                    }
                }

                if (behemothScenicRoute.Count > geoBehemothActor.GeoLevel.Map.AllSites.Count / 2)
                {
                    TFTVLogger.Always("Total sites count is " + geoBehemothActor.GeoLevel.Map.AllSites.Count + " and scenic route has " + behemothScenicRoute.Count + " sites.");
                    behemothScenicRoute.Clear();
                }

                if (geoSites.Count > 0 && behemothScenicRoute.Count == 0)
                {

                    GeoSite targetReference = geoSites.GetRandomElement();

                    IOrderedEnumerable<GeoSite> orderedEnumerable = from s in geoBehemothActor.GeoLevel.Map.GetConnectedSitesOfType_Land(targetReference, GeoSiteType.Exploration, activeOnly: false)
                                                                    orderby GeoMap.Distance(targetReference, s)
                                                                    select s;

                    foreach (GeoSite target in orderedEnumerable)
                    {
                        behemothScenicRoute.Add(target.SiteId);
                    }

                }
                if (behemothScenicRoute.Count > 0)
                {
                    TFTVLogger.Always("Actually got to the scenic Route count, and it's " + behemothScenicRoute.Count);

                    foreach (GeoSite site in geoBehemothActor.GeoLevel.Map.AllSites)
                    {
                        if (behemothScenicRoute.Contains(site.SiteId))
                        {
                            chosenTarget = site;
                            // TFTVLogger.Always("The site is " + site.Name);
                            behemothScenicRoute.Remove(site.SiteId);
                            return chosenTarget;
                        }
                    }
                }
                TFTVLogger.Always("Didn't find a site on the scenic route, so defaulting to nearest site, which is " + chosenTarget.Name);
                return chosenTarget;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }

        /*
         [HarmonyPatch(typeof(InterceptionAircraft), "get_CurrentHitPoints")]
         public static class InterceptionAircraft_TransferStatsToVehicle_DisengageDestroyRandomWeapon_patch
         {
             public static bool Prepare()
             {
                 TFTVConfig config = TFTVMain.Main.Config;
                 return config.ActivateAirCombatChanges;
             }


             public static void Postfix(ref float __result, InterceptionAircraft __instance)
             {
                 try
                 {
                     TFTVLogger.Always("Method get hitpoints is called");

                     if (PlayerVehicle!=null && __instance == PlayerVehicle)
                     {
                         TFTVLogger.Always("PlayerVehicle HP in second method are " + PlayerVehicle.CurrentHitPoints);
                         __result = PlayerVehicle.CurrentHitPoints;
                         PlayerVehicle = null;

                     }

                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                 }
             }
         }

         [HarmonyPatch(typeof(InterceptionGameController), "DisengagePlayer")]
         public static class InterceptionGameController_DisengagePlayer_DisengageDestroyRandomWeapon_patch
         {
             public static bool Prepare()
             {
                 TFTVConfig config = TFTVMain.Main.Config;
                 return config.ActivateAirCombatChanges;
             }


             public static void Postfix(InterceptionGameController __instance)
             {
                 try
                 {
                     int numberOfActiveWeaponsEnemy = 0;
                     int num = 0;

                     for (int i = 0; i < __instance.EnemyAircraft.Weapons.Count(); i++)
                     {
                         InterceptionAircraftWeapon enemyWeapon = __instance.EnemyAircraft.GetWeapon(i);
                         if (enemyWeapon != null && !enemyWeapon.IsDisabled)
                         {
                             TFTVLogger.Always("Weapon " + i + "is " + enemyWeapon.WeaponDef.GetDisplayName().LocalizeEnglish());
                             numberOfActiveWeaponsEnemy++;
                         }
                     }

                     TFTVLogger.Always("Number of active enemy weapons: " + numberOfActiveWeaponsEnemy);
                     if (numberOfActiveWeaponsEnemy > 0)
                     {
                         num = UnityEngine.Random.Range(0, 100 + 25 * numberOfActiveWeaponsEnemy);
                         TFTVLogger.Always("Roll: " + num);

                         // if (num > 100)
                         // {
                         GeoVehicle playerCraft = __instance.CurrentMission.PlayerAircraft.Vehicle;
                         TFTVLogger.Always("Hitpoints are " + playerCraft.Stats.HitPoints);
                         if (playerCraft.Stats.HitPoints > num || playerCraft.Stats.HitPoints < 10)
                         {

                             // GeoVehicleEquipment randomWeapon = playerCraft.Weapons.ToList().GetRandomElement();
                             playerCraft.DamageAircraft(num);
                             TFTVLogger.Always("We pass the if test and current Hitpoints are" + playerCraft.Stats.HitPoints);
                         }
                         else
                         {
                             int hitpoints = playerCraft.Stats.HitPoints;
                             playerCraft.DamageAircraft(hitpoints - 1);
                             TFTVLogger.Always("We pass the else test and current Hitpoints are" + playerCraft.Stats.HitPoints);
                         }
                         PlayerVehicle=__instance.PlayerAircraft;
                         TFTVLogger.Always("PlayerVehicle HP in first method are " + PlayerVehicle.CurrentHitPoints);
                         //   playerCraft.RemoveEquipment(randomWeapon);
                         GameUtl.GetMessageBox().ShowSimplePrompt($"{playerCraft.Name}" + " suffered " + num + " damage " 
                                         + " during " + "disengagement maneuvers.", MessageBoxIcon.None, MessageBoxButtons.OK, null);
                     }

                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                 }
             }
         }*/
    }
}


