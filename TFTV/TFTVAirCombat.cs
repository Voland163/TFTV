using Base;
using Base.Core;
using Base.Defs;
using Base.UI;
using Base.UI.MessageBox;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Interception;
using PhoenixPoint.Geoscape.Entities.Interception.Equipments;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
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

                //Design decision
             //   GeoVehicleModuleDef afterburnerMDef = Repo.GetAllDefs<GeoVehicleModuleDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_Afterburner_GeoVehicleModuleDef"));
             //   GeoVehicleModuleDef flaresMDef = Repo.GetAllDefs<GeoVehicleModuleDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_Flares_GeoVehicleModuleDef"));
             //   GeoVehicleModuleDef jammerMDef = Repo.GetAllDefs<GeoVehicleModuleDef>().FirstOrDefault(gvw => gvw.name.Equals("AN_ECMJammer_GeoVehicleModuleDef"));

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

                //afterburnerMDef.AmmoCount = 3;
                //flaresMDef.AmmoCount = 3;
                //jammerMDef.AmmoCount = 3;

                //This is testing Belial's suggestions, unlocking flares via PX Aerial Warfare, etc.
                AddItemToManufacturingReward("PX_Flares_GeoVehicleModuleDef",
                    "PX_AerialWarfare_ResearchDef_ManufactureResearchRewardDef_0", "PX_Aircraft_Flares_ResearchDef");
                AddItemToManufacturingReward("PX_VirophageGunFenrirRC7_VehicleWeaponDef",
                    "PX_VirophageWeapons_ResearchDef_ManufactureResearchRewardDef_0", "PX_Aircraft_VirophageGun_ResearchDef");
                AddItemToManufacturingReward("PX_ElectrolaserThunderboltHC9_VehicleWeaponDef",
                    "PX_AdvancedLaserTech_ResearchDef_ManufactureResearchRewardDef_0", "PX_Aircraft_Electrolaser_ResearchDef");
                CreateManufacturingReward("PX_AutocannonBrokkrAC3_VehicleWeaponDef", "SY_SecurityStations_GeoVehicleModuleDef",
                    "PX_Aircraft_Autocannon_ResearchDef_ManufactureResearchRewardDef_0", "PX_Aircraft_Autocannon_ResearchDef",
                     "PX_Alien_Spawnery_ResearchDef");
                AddItemToManufacturingReward("PX_HypersonicMissileHandOfTyr_VehicleWeaponDef",
                    "PX_AdvancedShreddingTech_ResearchDef_ManufactureResearchRewardDef_0", "PX_Aircraft_HypersonicMissile_ResearchDef");
                AddItemToManufacturingReward("NJ_TacticalNuclearMissileArmageddonAAM_VehicleWeaponDef",
                    "NJ_GuidanceTech_ResearchDef_ManufactureResearchRewardDef_0", "NJ_Aircraft_TacticalNuke_ResearchDef");
                AddItemToManufacturingReward("NJ_FuelTanks_GeoVehicleModuleDef",
                    "NJ_VehicleTech_ResearchDef_ManufactureResearchRewardDef_0", "NJ_Aircraft_FuelTank_ResearchDef");
                AddItemToManufacturingReward("NJ_CruiseControl_GeoVehicleModuleDef",
                    "SYN_Rover_ResearchDef_ManufactureResearchRewardDef_0", "NJ_Aircraft_CruiseControl_ResearchDef");
                AddItemToManufacturingReward("SY_EMPMissileMedusaAAM_VehicleWeaponDef",
                    "SYN_NanoTech_ResearchDef_ManufactureResearchRewardDef_0", "SYN_Aircraft_EMPMissile_ResearchDef");
                AddItemToManufacturingReward("AN_Oracle_GeoVehicleModuleDef",
                    "ANU_AnuWarfare_ResearchDef_ManufactureResearchRewardDef_0", "ANU_Aircraft_Oracle_ResearchDef");
                CreateManufacturingReward("AN_ECMJammer_GeoVehicleModuleDef", "AN_MutogCatapultIsharasBane_VehicleWeaponDef",
                    "ANU_Aircraft_ECMJammer_ResearchDef_ManufactureResearchRewardDef_0", "ANU_Aircraft_ECMJammer_ResearchDef",
                    "ANU_AdvancedBlimp_ResearchDef");


                //Changing ALN Berith research req so that they only appear after certain ODI event
                EncounterVariableResearchRequirementDef berithEncounterVariable = Repo.GetAllDefs<EncounterVariableResearchRequirementDef>().
                   FirstOrDefault(ged => ged.name.Equals("ALN_Medium_Flyer_ResearchDef_EncounterVariableResearchRequirementDef_0"));
                berithEncounterVariable.VariableName = "BerithAreComing";

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

                //Changes to FesteringSkies settings
                FesteringSkiesSettingsDef festeringSkiesSettingsDef = Repo.GetAllDefs<FesteringSkiesSettingsDef>().FirstOrDefault(gvw => gvw.name.Equals("FesteringSkiesSettingsDef"));
                festeringSkiesSettingsDef.SpawnInfestedAircraftChance = 0;
                festeringSkiesSettingsDef.InfestedAircraftChance = 0;

                InterceptionGameDataDef interceptionGameDataDef = Repo.GetAllDefs<InterceptionGameDataDef>().FirstOrDefault(gvw => gvw.name.Equals("InterceptionGameDataDef"));
                interceptionGameDataDef.DisengageDuration = 0;

                RemoveHardFlyersTemplates();
            }


            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void RemoveHardFlyersTemplates()
        {
            try
            {
                GeoVehicleWeaponDef acidSpit = Repo.GetAllDefs<GeoVehicleWeaponDef>().FirstOrDefault(gvw => gvw.name.Equals("ALN_AcidSpit_VehicleWeaponDef"));
                GeoVehicleWeaponDef spikes = Repo.GetAllDefs<GeoVehicleWeaponDef>().FirstOrDefault(gvw => gvw.name.Equals("ALN_Spikes_VehicleWeaponDef"));
                GeoVehicleWeaponDef napalmBreath = Repo.GetAllDefs<GeoVehicleWeaponDef>().FirstOrDefault(gvw => gvw.name.Equals("ALN_NapalmBreath_VehicleWeaponDef"));
                GeoVehicleWeaponDef ram = Repo.GetAllDefs<GeoVehicleWeaponDef>().FirstOrDefault(gvw => gvw.name.Equals("ALN_Ram_VehicleWeaponDef"));
                GeoVehicleWeaponDef tick = Repo.GetAllDefs<GeoVehicleWeaponDef>().FirstOrDefault(gvw => gvw.name.Equals("ALN_Tick_VehicleWeaponDef"));
                GeoVehicleWeaponDef voidChamber = Repo.GetAllDefs<GeoVehicleWeaponDef>().FirstOrDefault(gvw => gvw.name.Equals("ALN_VoidChamber_VehicleWeaponDef"));

               /* GeoVehicleWeaponDamageDef shredDamage = Repo.GetAllDefs<GeoVehicleWeaponDamageDef>().FirstOrDefault(gvw => gvw.name.Equals("Shred_GeoVehicleWeaponDamageDef")); 
                GeoVehicleWeaponDamageDef regularDamage= Repo.GetAllDefs<GeoVehicleWeaponDamageDef>().FirstOrDefault(gvw => gvw.name.Equals("Regular_GeoVehicleWeaponDamageDef"));

                tick.DamagePayloads[0] = new GeoWeaponDamagePayload { Damage = shredDamage, Amount = 20 };
                tick.DamagePayloads.Add(new GeoWeaponDamagePayload { Damage = regularDamage, Amount = 60 });*/


                GeoVehicleLoadoutDef charun2 = Repo.GetAllDefs<GeoVehicleLoadoutDef>().FirstOrDefault(gvw => gvw.name.Equals("AL_Small2_VehicleLoadout"));
                GeoVehicleLoadoutDef charun4 = Repo.GetAllDefs<GeoVehicleLoadoutDef>().FirstOrDefault(gvw => gvw.name.Equals("AL_Small4_VehicleLoadout"));
                GeoVehicleLoadoutDef berith1 = Repo.GetAllDefs<GeoVehicleLoadoutDef>().FirstOrDefault(gvw => gvw.name.Equals("AL_Medium1_VehicleLoadout"));
                GeoVehicleLoadoutDef berith2 = Repo.GetAllDefs<GeoVehicleLoadoutDef>().FirstOrDefault(gvw => gvw.name.Equals("AL_Medium2_VehicleLoadout"));
                GeoVehicleLoadoutDef berith3 = Repo.GetAllDefs<GeoVehicleLoadoutDef>().FirstOrDefault(gvw => gvw.name.Equals("AL_Medium3_VehicleLoadout"));
                GeoVehicleLoadoutDef berith4 = Repo.GetAllDefs<GeoVehicleLoadoutDef>().FirstOrDefault(gvw => gvw.name.Equals("AL_Medium4_VehicleLoadout"));
                GeoVehicleLoadoutDef abbadon1 = Repo.GetAllDefs<GeoVehicleLoadoutDef>().FirstOrDefault(gvw => gvw.name.Equals("AL_Large1_VehicleLoadout"));
                GeoVehicleLoadoutDef abbadon2 = Repo.GetAllDefs<GeoVehicleLoadoutDef>().FirstOrDefault(gvw => gvw.name.Equals("AL_Large2_VehicleLoadout"));
                GeoVehicleLoadoutDef abbadon3 = Repo.GetAllDefs<GeoVehicleLoadoutDef>().FirstOrDefault(gvw => gvw.name.Equals("AL_Large3_VehicleLoadout"));

                charun2.EquippedItems[0] = napalmBreath;
                charun2.EquippedItems[1] = ram;

                charun4.EquippedItems[0] = voidChamber;
                charun4.EquippedItems[1] = spikes;

                berith1.EquippedItems[0] = acidSpit;
                berith1.EquippedItems[1] = acidSpit;
                berith1.EquippedItems[2] = spikes;
                berith1.EquippedItems[3] = ram;

                berith2.EquippedItems[0] = tick;
                berith2.EquippedItems[1] = ram;
                berith2.EquippedItems[2] = ram;
                berith2.EquippedItems[3] = spikes;

                berith3.EquippedItems[0] = napalmBreath;
                berith3.EquippedItems[1] = spikes;
                berith3.EquippedItems[2] = spikes;
                berith3.EquippedItems[3] = ram;

                berith4.EquippedItems[0] = voidChamber;
                berith4.EquippedItems[1] = napalmBreath;
                berith4.EquippedItems[2] = ram;
                berith4.EquippedItems[3] = ram;

                abbadon1.EquippedItems[0] = acidSpit;
                abbadon1.EquippedItems[1] = acidSpit;
                abbadon1.EquippedItems[2] = acidSpit;
                abbadon1.EquippedItems[3] = spikes;
                abbadon1.EquippedItems[4] = spikes;
                abbadon1.EquippedItems[5] = spikes;

                abbadon2.EquippedItems[0] = voidChamber;
                abbadon2.EquippedItems[1] = napalmBreath;
                abbadon2.EquippedItems[2] = ram;
                abbadon2.EquippedItems[3] = ram;
                abbadon2.EquippedItems[4] = ram;
                abbadon2.EquippedItems[5] = ram;

                abbadon3.EquippedItems[0] = voidChamber;
                abbadon3.EquippedItems[1] = voidChamber;
                abbadon3.EquippedItems[2] = ram;
                abbadon3.EquippedItems[3] = ram;
                abbadon3.EquippedItems[4] = spikes;
                abbadon3.EquippedItems[5] = spikes;



                /* Info about Vanilla loadouts:
               AlienFlyerResearchRewardDef aLN_Small_FlyerLoadouts= Repo.GetAllDefs<AlienFlyerResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals("ALN_Small_Flyer_ResearchDef_FlyerLoadoutResearchRewardDef_0"));
                AL_Small1_VehicleLoadout: ALN_AcidSpit_VehicleWeaponDef, ALN_Spikes_VehicleWeaponDef
                AL_Small2_VehicleLoadout: ALN_NapalmBreath_VehicleWeaponDef, ALN_AcidSpit_VehicleWeaponDef
                AL_Small3_VehicleLoadout: ALN_Ram_VehicleWeaponDef, ALN_Spikes_VehicleWeaponDef

                AlienFlyerResearchRewardDef aLN_Medium_FlyerLoadouts = Repo.GetAllDefs<AlienFlyerResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals("ALN_Medium_Flyer_ResearchDef_FlyerLoadoutResearchRewardDef_0"));
                AL_Medium1_VehicleLoadout: ALN_AcidSpit_VehicleWeaponDef, ALN_Spikes_VehicleWeaponDef, ALN_Spikes_VehicleWeaponDef, ALN_Tick_VehicleWeaponDef
                AL_Medium2_VehicleLoadout: ALN_NapalmBreath_VehicleWeaponDef, ALN_NapalmBreath_VehicleWeaponDef, ALN_Ram_VehicleWeaponDef, ALN_Ram_VehicleWeaponDef
                AL_Medium3_VehicleLoadout: ALN_AcidSpit_VehicleWeaponDef, ALN_Spikes_VehicleWeaponDef, ALN_Tick_VehicleWeaponDef, ALN_Ram_VehicleWeaponDef
                AL_Small4_VehicleLoadout: ALN_NapalmBreath_VehicleWeaponDef, ALN_Tick_VehicleWeaponDef

                AlienFlyerResearchRewardDef aLN_Large_FlyerLoadouts = Repo.GetAllDefs<AlienFlyerResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals("ALN_Large_Flyer_ResearchDef_FlyerLoadoutResearchRewardDef_0"));
                AL_Large1_VehicleLoadout: ALN_VoidChamber_VehicleWeaponDef, ALN_VoidChamber_VehicleWeaponDef, ALN_NapalmBreath_VehicleWeaponDef, ALN_NapalmBreath_VehicleWeaponDef, ALN_Tick_VehicleWeaponDef, ALN_Tick_VehicleWeaponDef
                AL_Large2_VehicleLoadout: ALN_AcidSpit_VehicleWeaponDef, ALN_AcidSpit_VehicleWeaponDef, ALN_AcidSpit_VehicleWeaponDef, ALN_Spikes_VehicleWeaponDef, ALN_Spikes_VehicleWeaponDef, ALN_Spikes_VehicleWeaponDef
                AL_Large3_VehicleLoadout: ALN_NapalmBreath_VehicleWeaponDef, ALN_Tick_VehicleWeaponDef, ALN_Ram_VehicleWeaponDef, ALN_VoidChamber_VehicleWeaponDef, ALN_AcidSpit_VehicleWeaponDef, ALN_Spikes_VehicleWeaponDef
                AL_Small5_VehicleLoadout: ALN_Ram_VehicleWeaponDef, ALN_VoidChamber_VehicleWeaponDef
                AL_Medium4_VehicleLoadout: ALN_AcidSpit_VehicleWeaponDef, ALN_Spikes_VehicleWeaponDef, ALN_VoidChamber_VehicleWeaponDef, ALN_Tick_VehicleWeaponDef

                */


            }


            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void AddItemToManufacturingReward(string module, string reward, string research)
        {

            try
            {
                ItemDef moduleDef = Repo.GetAllDefs<GeoVehicleModuleDef>().FirstOrDefault(gvw => gvw.name.Equals(module));
                ManufactureResearchRewardDef rewardDef = Repo.GetAllDefs<ManufactureResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals(reward));
                ResearchDef researchDef = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(gvw => gvw.name.Equals(research));
                List<ItemDef> rewards = rewardDef.Items.ToList();
                rewards.Add(moduleDef);
                rewardDef.Items = rewards.ToArray();
                researchDef.HideInUI = true;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void CreateManufacturingReward(string module1, string module2, string reward, string research, string newResearch)
        {
            try
            {
                ItemDef moduleDef = Repo.GetAllDefs<GeoVehicleModuleDef>().FirstOrDefault(gvw => gvw.name.Equals(module1));
                ItemDef moduleDef2 = Repo.GetAllDefs<GeoVehicleModuleDef>().FirstOrDefault(gvw => gvw.name.Equals(module2));
                ManufactureResearchRewardDef rewardDef = Repo.GetAllDefs<ManufactureResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals(reward));
                ResearchDef researchDef = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(gvw => gvw.name.Equals(research));
                ResearchDef newResearchDef = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(gvw => gvw.name.Equals(newResearch));
                List<ItemDef> rewards = rewardDef.Items.ToList();
                rewards.Add(moduleDef2);
                rewardDef.Items = rewards.ToArray();
                newResearchDef.Unlocks = researchDef.Unlocks;
                newResearchDef.Unlocks[0] = rewardDef;
                researchDef.HideInUI = true;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }



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

                        TFTVLogger.Always("Egg Spawned");

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

        public static List<GeoSite> targetsForBehemoth = new List<GeoSite>();

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

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        public static Dictionary<int, List<GeoSite>> flyersAndHavens = new Dictionary<int, List<GeoSite>>();

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
            public static bool Prepare()
            {
                TFTVConfig config = TFTVMain.Main.Config;
                return config.ActivateAirCombatChanges;
            }
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
            public static bool Prepare()
            {
                TFTVConfig config = TFTVMain.Main.Config;
                return config.ActivateAirCombatChanges;
            }
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
                        TFTVLogger.Always("Rol: " + num);

                        // if (num > 100)
                        // {
                        GeoVehicle playerCraft = __instance.CurrentMission.PlayerAircraft.Vehicle;
                        if (playerCraft.Stats.HitPoints > num || playerCraft.Stats.HitPoints < 10)
                        {

                            // GeoVehicleEquipment randomWeapon = playerCraft.Weapons.ToList().GetRandomElement();
                            playerCraft.DamageAircraft(num);
                        }
                        else
                        {
                            int hitpoints = playerCraft.Stats.HitPoints;
                            playerCraft.DamageAircraft(hitpoints - 1);
                        }

                        //   playerCraft.RemoveEquipment(randomWeapon);
                        GameUtl.GetMessageBox().ShowSimplePrompt($"{playerCraft.Name}" + " suffered damage "
                                        + " during " + "disengagement maneuvers.", MessageBoxIcon.None, MessageBoxButtons.OK, null);
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


