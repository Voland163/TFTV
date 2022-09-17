using Base;
using Base.Core;
using Base.Defs;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Interception;
using PhoenixPoint.Geoscape.Entities.Interception.Equipments;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewControllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Home.View.ViewModules;

namespace TFTV
{
    internal class TFTVAirCombat
    {

        private static readonly DefRepository Repo = TFTVMain.Repo;
        public static Dictionary<int, List<int>> flyersAndHavens = new Dictionary<int, List<int>>();
        public static List<int> targetsForBehemoth = new List<int>();
      //  public static List<int> targetsVisitedByBehemoth = new List<int>();

        public static List<int> behemothScenicRoute = new List<int>();
        public static int behemothTarget = 0;
        public static int behemothWaitHours = 12;

        public static bool checkHammerfall = false;

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

        //  public static bool BehemothSubmerging = false; 

        [HarmonyPatch(typeof(GeoBehemothActor), "PickSubmergeLocation")]
        public static class GeoBehemothActor_PickSubmergeLocation_patch
        {
            public static bool Prepare()
            {
                TFTVConfig config = TFTVMain.Main.Config;
                return config.ActivateAirCombatChanges;
            }
            public static void Postfix()

            {
                try
                {
                    TFTVLogger.Always("Behemoth submerging");
                    //  BehemothSubmerging = true;
                    flyersAndHavens.Clear();
                    targetsForBehemoth.Clear();
                    behemothScenicRoute.Clear();
                    //  BehemothSubmerging = true;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }

        }

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
                    if (site!=null && site.SiteId == siteID)
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
                    if (__instance.CurrentBehemothStatus == GeoBehemothActor.BehemothStatus.Dormant)//first check
                    {
                        //   TFTVLogger.Always("Behemoth's target lists are cleared because he is sleeping");
                        targetsForBehemoth.Clear();
                        //  targetsVisitedByBehemoth.Clear();
                        behemothScenicRoute.Clear();
                        behemothTarget = 0;
                        return true;
                    }

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

                      BehemothDisplayController behemothDisplayController = (BehemothDisplayController)UnityEngine.Object.FindObjectOfType(typeof(BehemothDisplayController));
                        // behemothDisplayController.UpdateDisruptionMeter(____disruptionPoints, ____disruptionThreshhold);
                                         

                        TFTVLogger.Always("Behemoth hourly update, disruption threshold set to " + ____disruptionThreshhold + ", disruption points are " + ____disruptionPoints);
                    }

                    if (!__instance.IsSubmerging && ____disruptionPoints >= ____disruptionThreshhold)
                    {
                        MethodInfo method_GenerateTargetData = AccessTools.Method(typeof(GeoBehemothActor), "PickSubmergeLocation");

                        method_GenerateTargetData.Invoke(__instance, null);
                        TFTVLogger.Always("Behemoth hourly update, disruption points at " + ____disruptionPoints + ", while threshold set to " + ____disruptionThreshhold + ". Behemoth should submerge");
                        return false;
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
                        else if(__instance.CurrentSite != null && __instance.CurrentSite == chosenHaven && targetsForBehemoth.Count == 0) 
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
                            typeof(GeoBehemothActor).GetMethod("TargetHaven", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { GetSiteForBehemothToMoveTo(__instance) });
                            behemothWaitHours = 12;
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

        [HarmonyPatch(typeof(GeoBehemothActor), "DamageHavenOutcome")]

        public static class GeoBehemothActor_DamageHavenOutcome_Patch
        {
            public static bool Prepare()
            {
                TFTVConfig config = TFTVMain.Main.Config;
                return config.ActivateAirCombatChanges;
            }

            public static void Postfix()
            {
                try
                {
                    behemothTarget = 0;
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

                if (behemothScenicRoute.Count > geoBehemothActor.GeoLevel.Map.AllSites.Count)
                {
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
                            TFTVLogger.Always("The site is " + site.LocalizedSiteName);                           
                            behemothScenicRoute.Remove(site.SiteId);
                            return chosenTarget;
                        }
                    }
                }

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


