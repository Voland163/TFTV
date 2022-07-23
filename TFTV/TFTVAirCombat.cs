using Base;
using Base.Core;
using Base.Defs;
using Base.UI;
using Base.UI.MessageBox;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Interception.Equipments;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Interception;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TFTV
{
    internal class TFTVAirCombat
    {

        private static readonly DefRepository Repo = TFTVMain.Repo;

        public static void ModifyAirCombatDefs()
        {
            try
            {
                //implementing Belial's proposal: 

                // ALN_VoidChamber_VehicleWeaponDef  Fire rate increased 20s-> 10s, Damage decreased 400-> 200
                // ALN_Spikes_VehicleWeaponDef	Changed to Psychic Guidance (from Visual Guidance)
                // ALN_Ram_VehicleWeaponDef Changed to Psychic Guidance(from Visual Guidance), HP 250-> 350

                // PX_Afterburner_GeoVehicleModuleDef Charges 5-> 3
                // PX_Flares_GeoVehicleModuleDef 5-> 3
                //  AN_ECMJammer_GeoVehicleModuleDef Charges 5-> 3

                //PX_ElectrolaserThunderboltHC9_VehicleWeaponDef Accuracy 95 % -> 85 %
                // PX_BasicMissileNomadAAM_VehicleWeaponDef 80 % -> 70 %
                // NJ_RailgunMaradeurAC4_VehicleWeaponDef 80 % -> 70 %
                //SY_LaserGunArtemisMkI_VehicleWeaponDef Artemis Accuracy 95 % -> 85 %


                GeoVehicleWeaponDef voidChamberWDef = Repo.GetAllDefs<GeoVehicleWeaponDef>().FirstOrDefault(gvw => gvw.name.Equals("ALN_VoidChamber_VehicleWeaponDef"));
                GeoVehicleWeaponDef spikesWDef = Repo.GetAllDefs<GeoVehicleWeaponDef>().FirstOrDefault(gvw => gvw.name.Equals("ALN_Spikes_VehicleWeaponDef"));
                GeoVehicleWeaponDef ramWDef = Repo.GetAllDefs<GeoVehicleWeaponDef>().FirstOrDefault(gvw => gvw.name.Equals("ALN_Ram_VehicleWeaponDef"));
                GeoVehicleWeaponDef thunderboltWDef = Repo.GetAllDefs<GeoVehicleWeaponDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_ElectrolaserThunderboltHC9_VehicleWeaponDef"));
                GeoVehicleWeaponDef nomadWDef = Repo.GetAllDefs<GeoVehicleWeaponDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_BasicMissileNomadAAM_VehicleWeaponDef"));
                GeoVehicleWeaponDef railGunWDef = Repo.GetAllDefs<GeoVehicleWeaponDef>().FirstOrDefault(gvw => gvw.name.Equals("NJ_RailgunMaradeurAC4_VehicleWeaponDef"));
                GeoVehicleWeaponDef laserGunWDef = Repo.GetAllDefs<GeoVehicleWeaponDef>().FirstOrDefault(gvw => gvw.name.Equals("SY_LaserGunArtemisMkI_VehicleWeaponDef"));

                GeoVehicleModuleDef afterburnerMDef = Repo.GetAllDefs<GeoVehicleModuleDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_Afterburner_GeoVehicleModuleDef"));
                GeoVehicleModuleDef flaresMDef = Repo.GetAllDefs<GeoVehicleModuleDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_Flares_GeoVehicleModuleDef"));
                GeoVehicleModuleDef jammerMDef = Repo.GetAllDefs<GeoVehicleModuleDef>().FirstOrDefault(gvw => gvw.name.Equals("AN_ECMJammer_GeoVehicleModuleDef"));

                voidChamberWDef.ChargeTime = 10.0f;
                var voidDamagePayload = voidChamberWDef.DamagePayloads[0].Damage;
                voidChamberWDef.DamagePayloads[0] = new GeoWeaponDamagePayload { Damage = voidDamagePayload, Amount = 200 };

                spikesWDef.Guidence = GeoVehicleWeaponGuidence.Psychic;
                ramWDef.Guidence = GeoVehicleWeaponGuidence.Psychic;
                ramWDef.HitPoints = 350;
                thunderboltWDef.Accuracy = 85;
                nomadWDef.Accuracy = 70;
                railGunWDef.Accuracy = 70;
                laserGunWDef.Accuracy = 85;

                afterburnerMDef.AmmoCount = 3;
                flaresMDef.AmmoCount = 3;
                jammerMDef.AmmoCount = 3;

                //Changing ALN Abbadon research so they appear only in Third Act, or After ODI reaches apex
                EncounterVariableResearchRequirementDef sourceVarResReq =
                   Repo.GetAllDefs<EncounterVariableResearchRequirementDef>().
                   FirstOrDefault(ged => ged.name.Equals("NJ_Bionics1_ResearchDef_EncounterVariableResearchRequirementDef_0"));

                //Creating new Research Requirements, each requiring a variable to be triggered  
                EncounterVariableResearchRequirementDef variableResReqAbbadon = Helper.CreateDefFromClone(sourceVarResReq, "F8D9463A-69C5-47B1-B52A-061D898CEEF8", "AbbadonResReqDef");
                variableResReqAbbadon.VariableName = "ThirdActStarted";
                EncounterVariableResearchRequirementDef variableResReqAbbadonAlt = Helper.CreateDefFromClone(sourceVarResReq, "F8D9463A-69C5-47B1-B52A-061D898CEEF8", "AbbadonResReqAltDef");
                variableResReqAbbadonAlt.VariableName = "ODI_Complete";
                //Altering researchDef, requiring Third Act to have started and adding an alternative way of revealing research if ODI is completed 
                ResearchDef aLN_Large_Flyer_ResearchDef = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(gvw => gvw.name.Equals("ALN_Large_Flyer_ResearchDef"));
                aLN_Large_Flyer_ResearchDef.RevealRequirements.Operation = ResearchContainerOperation.ANY;
                aLN_Large_Flyer_ResearchDef.RevealRequirements.Container[0].Requirements.AddItem(variableResReqAbbadon);
                ReseachRequirementDefOpContainer[] reseachRequirementDefOpContainers = new ReseachRequirementDefOpContainer[1];
                ResearchRequirementDef[] researchRequirementDefs = new ResearchRequirementDef[1];
                researchRequirementDefs[0] = variableResReqAbbadonAlt;

                reseachRequirementDefOpContainers[0].Requirements = researchRequirementDefs;
                aLN_Large_Flyer_ResearchDef.RevealRequirements.Container.AddRangeToArray(reseachRequirementDefOpContainers);
            }


            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }



        [HarmonyPatch(typeof(GeoAlienFaction), "SpawnEgg", new Type[] { typeof(Vector3) })]

        public static class GeoAlienFaction_SpawnEgg_DestroyHavens_Patch
        {
            public static bool Prepare()
            {
                TFTVConfig Config = new TFTVConfig();
                return Config.ActivateAirCombatChanges;
            }


            public static void Postfix(GeoAlienFaction __instance, Vector3 worldPos)
            {
                try
                {
                    TFTVLogger.Always("Egg Spawned");

                    List<GeoHaven> geoHavens = __instance.GeoLevel.AnuFaction.Havens.ToList();
                    geoHavens.AddRange(__instance.GeoLevel.NewJerichoFaction.Havens.ToList());
                    geoHavens.AddRange(__instance.GeoLevel.SynedrionFaction.Havens.ToList());
                    int count = 0;
                    int damage = UnityEngine.Random.Range(25, 200);
                    foreach (GeoHaven haven in geoHavens)
                    {
                        TFTVLogger.Always("Got Here");
                        if (Vector3.Distance(haven.Site.WorldPosition, worldPos) <= 1)

                        {
                            TFTVLogger.Always("This haven " + haven.Site.LocalizedSiteName + "is getting whacked by the asteroid");
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
                                haven.Zones.ToArray().ForEach(zone => zone.AddDamage(UnityEngine.Random.Range(damageToZones-25, damageToZones+25)));
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
                            }
                            
                        }


                    }

                    /* (haven.Site.WorldPosition.x <= worldPos.x + 10 || haven.Site.WorldPosition.y <= worldPos.y + 100 ||
                                haven.Site.WorldPosition.x <= worldPos.x - 10 || haven.Site.WorldPosition.y <= worldPos.y - 100)*/
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

        }

        public static List<GeoSite> targetsForBehemoth = new List<GeoSite>();

        //patch to reveal havens under attack
        [HarmonyPatch(typeof(GeoscapeRaid), "StartAttackEffect")]
        public static class GeoscapeRaid_StartAttackEffect_patch
        {
            public static bool Prepare()
            {
                TFTVConfig Config = new TFTVConfig();
                return Config.ActivateAirCombatChanges;
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

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        /*public static List<int> flyers = new List<int>();
        public static Dictionary<GeoVehicle, List<GeoSite>> flyersAndHavens = new Dictionary<GeoVehicle, List<GeoSite>>();*/

        /*
        [HarmonyPatch(typeof(AlienRaidManager), "OnRaidGenerated")]
        public static class AlienRaidManager_OnRaidGenerated_patch
        {
            public static bool Prepare()
            {
                TFTVConfig Config = new TFTVConfig();
                return Config.ActivateAirCombatChanges;
            }

            public static void Postfix(GeoscapeRaid raid)
            {
                try
                {
                    if (raid.Type != 0)
                    {                     
                        foreach (GeoSite target in raid.Targets)
                        {
                            target.RevealSite(target.GeoLevel.PhoenixFaction);
                        }
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }*/
        /*
        [HarmonyPatch(typeof(GeoVehicle), "OnArrivedAtDestination")]

        public static class GeoVehicle_OnArrivedAtDestination
        {
            public static bool Prepare()
            {
                //TFTVConfig Config = new TFTVConfig();
                //return Config.ActivateAirCombatChanges;
                return true;
            }
            public static void Postfix(GeoVehicle __instance, bool justPassing)
            {
                try
                {
                    TFTVLogger.Always("OnArrived method invoked");

                    if (!justPassing && __instance.Owner.IsAlienFaction && __instance.CurrentSite.Type == GeoSiteType.Haven)
                    {

                        if (flyersAndHavens.Keys.Count>0 && flyersAndHavens.Keys.Any(f => f.VehicleID == __instance.VehicleID))
                        {
                            flyersAndHavens[__instance].Add(__instance.CurrentSite);
                        }
                        else
                        {
                            flyersAndHavens.Add(__instance, new List<GeoSite> { (__instance.CurrentSite) });
                        }


                        TFTVLogger.Always("Added to list of havens visisted " + __instance.CurrentSite);
                    }

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

        }*/

        /*
                [HarmonyPatch(typeof(GeoVehicle), "OnArrivedAtDestination")]

                public static class GeoVehicle_OnArrivedAtDestination
                {
                    public static bool Prepare()
                    {
                        TFTVConfig Config = new TFTVConfig();
                        return Config.ActivateAirCombatChanges;
                    }
                    public static void Postfix(GeoVehicle __instance, bool justPassing)
                    {
                        try
                        {
                            TFTVLogger.Always("OnArrived method invoked");

                            if (!justPassing && flyers.Contains(__instance.VehicleID) && __instance.CurrentSite.Type == GeoSiteType.Haven)
                            {

                                if (flyersAndHavens.Keys.Any(f => f.VehicleID == __instance.VehicleID))
                                {
                                    flyersAndHavens[__instance].Add(__instance.CurrentSite);
                                }
                                else
                                {
                                    flyersAndHavens.Add(__instance, new List<GeoSite> { (__instance.CurrentSite) });
                                }


                                TFTVLogger.Always("Added to list of havens visisted " + __instance.CurrentSite);
                            }

                        }

                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                    }

                }
        */
        /*
        [HarmonyPatch(typeof(GeoscapeRaid), "StopBehemothFollowing")]

        public static class GeoscapeRaid_StopBehemothFollowing_patch
        {
            public static bool Prepare()
            {
                TFTVConfig Config = new TFTVConfig();
                return Config.ActivateAirCombatChanges;
            }
            public static void Prefix(GeoscapeRaid __instance)
            {
                try
                {
                    if (flyersAndHavens.ContainsKey(__instance.GeoVehicle))
                    {
                        foreach (var haven in flyersAndHavens[__instance.GeoVehicle])
                        {
                            targetsForBehemoth.Add(haven);
                            TFTVLogger.Always("Haven " + haven + " added to the list of targets");
                        }
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }
        */

        /*

    [HarmonyPatch(typeof(AlienRaidManager), "OnRaidEnded")]
    public static class InterceptionGameController_OnRaidEnded_patch
    {
        public static bool Prepare()
        {
            TFTVConfig Config = new TFTVConfig();
            return Config.ActivateAirCombatChanges;
        }

        public static void Postfix(GeoscapeRaid raid, bool raidSuccessful)
        {
            try
            {
                TFTVLogger.Always("Check that Geoscape Raid method is invoked in a new way");

                if (raidSuccessful)
                {
                    if (raid.Targets != null)
                    {
                        foreach (GeoSite target in raid.Targets)
                        {
                            TFTVLogger.Always("The target is" + target.LocalizedSiteName);
                            TFTVLogger.Always("The type of raid is" + raid.Type.GetName());

                            if (target.Type == GeoSiteType.Haven)
                            {
                                targetsForBehemoth.Add(target);
                                TFTVLogger.Always("Haven " + target.LocalizedSiteName + " added to the list of targets");
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
    }*/

        public static Dictionary<int, List<GeoSite>> flyersAndHavens = new Dictionary<int, List<GeoSite>>();

        [HarmonyPatch(typeof(GeoVehicle), "OnArrivedAtDestination")]

        public static class GeoVehicle_OnArrivedAtDestination
        {

            public static void Postfix(GeoVehicle __instance, bool justPassing)
            {
                try
                {
                    TFTVLogger.Always("OnArrived method invoked");

                    if (!justPassing && __instance.Owner.IsAlienFaction && __instance.CurrentSite.Type == GeoSiteType.Haven)
                    {

                        if (flyersAndHavens.ContainsKey(__instance.VehicleID))
                        {
                            flyersAndHavens[__instance.VehicleID].Add(__instance.CurrentSite);
                        }
                        else
                        {
                            flyersAndHavens.Add(__instance.VehicleID, new List<GeoSite> { __instance.CurrentSite });
                        }


                        TFTVLogger.Always("Added to list of havens visisted " + __instance.CurrentSite);
                    }

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

        }

        //  public static bool BehemothSubmerging; 

        [HarmonyPatch(typeof(GeoBehemothActor), "PickSubmergeLocation")]
        public static class GeoBehemothActor_PickSubmergeLocation_patch
        {
            public static void Postfix()

            {
                try
                {
                    TFTVLogger.Always("Behemoth submerging");
                    // BehemothSubmerging = true;
                    flyersAndHavens.Clear();
                    targetsForBehemoth.Clear();

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

            public static void Prefix(GeoscapeRaid __instance)
            {
                try
                {
                    if (flyersAndHavens.ContainsKey(__instance.GeoVehicle.VehicleID))
                    {
                        foreach (GeoSite haven in flyersAndHavens[__instance.GeoVehicle.VehicleID])
                        {
                            targetsForBehemoth.Add(haven);

                            TFTVLogger.Always("Haven " + haven + " added to the list of targets");
                        }
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(GeoBehemothActor), "IsValidTarget")]
        public static class GeoBehemothActor_AttemptToPickTargetHaven_BehemothTargetting_Patch
        {
            public static bool Prefix(ref bool __result, GeoSite site)
            {
                try
                {
                    //  TFTVLogger.Always("IsValidTarget Method invoked for GeoSite " + site.LocalizedSiteName);

                    if (targetsForBehemoth != null && targetsForBehemoth.Contains(site))
                    {
                        TFTVLogger.Always("Site is in the list and a valid target for B");
                        __result = true;
                        return true;

                    }
                    // TFTVLogger.Always("Site is not in the list and not a valid target for B");
                    return false;

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return false;
                }

            }
        }



        [HarmonyPatch(typeof(InterceptionGameController), "DisengagePlayer")]
        public static class InterceptionGameController_DisengagePlayer_DisengageDestroyRandomWeapon_patch
        {
            public static bool Prepare()
            {
                TFTVConfig Config = new TFTVConfig();
                return Config.ActivateAirCombatChanges;
            }


            public static void Postfix(InterceptionGameController __instance)
            {
                try
                {
                    int numberOfActiveWeaponsEnemy = 0;

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

                    int num = UnityEngine.Random.Range(0, 100 + 25 * numberOfActiveWeaponsEnemy);
                    TFTVLogger.Always("Rol: " + num);
                    if (num > 100)
                    {
                        GeoVehicle playerCraft = __instance.CurrentMission.PlayerAircraft.Vehicle;
                        GeoVehicleEquipment randomWeapon = playerCraft.Weapons.ToList().GetRandomElement();
                        playerCraft.RemoveEquipment(randomWeapon);
                        GameUtl.GetMessageBox().ShowSimplePrompt($"<b>{randomWeapon.EquipmentDef.GetDisplayName().LocalizeEnglish()}</b>" + " was destroyed "
                                        + " during " + $"{playerCraft.Name}" + "'s" + " disengagement maneuvers.", MessageBoxIcon.None, MessageBoxButtons.OK, null);
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


