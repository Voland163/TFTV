using Base;
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
        public static Dictionary<int, List<int>> flyersAndHavens = new Dictionary<int, List<int>>();
        public static List<int> targetsForBehemoth = new List<int>();
      //  public static List<int> targetsVisitedByBehemoth = new List<int>();

        public static List<int> behemothScenicRoute = new List<int>();
        public static int behemothTarget = 0;
        public static int behemothWaitHours = 12;

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
               // ramWDef.Guidence = GeoVehicleWeaponGuidence.Psychic;
                ramWDef.HitPoints = 350;
                thunderboltWDef.Accuracy = 85;
                nomadWDef.Accuracy = 70;
                railGunWDef.Accuracy = 70;
                laserGunWDef.Accuracy = 85;

                //afterburnerMDef.AmmoCount = 3;
                //flaresMDef.AmmoCount = 3;
                //jammerMDef.AmmoCount = 3;

                //This is testing Belial's suggestions, unlocking flares via PX Aerial Warfare, etc.
                AddItemToManufacturingReward("PX_Aircraft_Flares_ResearchDef_ManufactureResearchRewardDef_0",
                    "PX_AerialWarfare_ResearchDef_ManufactureResearchRewardDef_0", "PX_Aircraft_Flares_ResearchDef");

                ManufactureResearchRewardDef fenrirReward = Repo.GetAllDefs<ManufactureResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_Aircraft_VirophageGun_ResearchDef_ManufactureResearchRewardDef_0"));
                ManufactureResearchRewardDef virophageWeaponsReward = Repo.GetAllDefs<ManufactureResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_VirophageWeapons_ResearchDef_ManufactureResearchRewardDef_0"));
                List<ItemDef> rewardsVirophage = virophageWeaponsReward.Items.ToList();
                rewardsVirophage.Add(fenrirReward.Items[0]);
                virophageWeaponsReward.Items = rewardsVirophage.ToArray();
                ResearchDef fenrirResearch = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_Aircraft_VirophageGun_ResearchDef"));
                fenrirResearch.HideInUI = true;

                ManufactureResearchRewardDef thunderboltReward = Repo.GetAllDefs<ManufactureResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_Aircraft_Electrolaser_ResearchDef_ManufactureResearchRewardDef_0"));
                ManufactureResearchRewardDef advancedLasersReward = Repo.GetAllDefs<ManufactureResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_AdvancedLaserTech_ResearchDef_ManufactureResearchRewardDef_0"));
                List<ItemDef> rewardsAdvancedLasers = advancedLasersReward.Items.ToList();
                rewardsAdvancedLasers.Add(thunderboltReward.Items[0]);
                advancedLasersReward.Items = rewardsAdvancedLasers.ToArray();
                ResearchDef electroLaserResearch = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_Aircraft_Electrolaser_ResearchDef"));
                electroLaserResearch.HideInUI = true;

                ManufactureResearchRewardDef handOfTyrReward = Repo.GetAllDefs<ManufactureResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_Aircraft_HypersonicMissile_ResearchDef_ManufactureResearchRewardDef_0"));
                ManufactureResearchRewardDef advancedShreddingReward = Repo.GetAllDefs<ManufactureResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_AdvancedShreddingTech_ResearchDef_ManufactureResearchRewardDef_0"));
                List<ItemDef> rewardsAdvancedShredding = advancedShreddingReward.Items.ToList();
                rewardsAdvancedShredding.Add(handOfTyrReward.Items[0]);
                advancedShreddingReward.Items = rewardsAdvancedShredding.ToArray();
                ResearchDef handOfTyrResearch = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_Aircraft_HypersonicMissile_ResearchDef"));
                handOfTyrResearch.HideInUI = true;

                AddItemToManufacturingReward("NJ_Aircraft_TacticalNuke_ResearchDef_ManufactureResearchRewardDef_0",
                    "NJ_GuidanceTech_ResearchDef_ManufactureResearchRewardDef_0", "NJ_Aircraft_TacticalNuke_ResearchDef");
                AddItemToManufacturingReward("NJ_Aircraft_FuelTank_ResearchDef_ManufactureResearchRewardDef_0",
                    "NJ_VehicleTech_ResearchDef_ManufactureResearchRewardDef_0", "NJ_Aircraft_FuelTank_ResearchDef");
                AddItemToManufacturingReward("NJ_Aircraft_CruiseControl_ResearchDef_ManufactureResearchRewardDef_0",
                    "SYN_Rover_ResearchDef_ManufactureResearchRewardDef_0", "NJ_Aircraft_CruiseControl_ResearchDef");

                ManufactureResearchRewardDef medusaAAM = Repo.GetAllDefs<ManufactureResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals("SYN_Aircraft_EMPMissile_ResearchDef_ManufactureResearchRewardDef_0"));
                ManufactureResearchRewardDef synAirCombat = Repo.GetAllDefs<ManufactureResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals("SYN_NanoTech_ResearchDef_ManufactureResearchRewardDef_0"));
                List<ItemDef> rewards = synAirCombat.Items.ToList();
                rewards.Add(medusaAAM.Items[0]);
                synAirCombat.Items = rewards.ToArray();
                ResearchDef medusaAAMResearch = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(gvw => gvw.name.Equals("SYN_Aircraft_EMPMissile_ResearchDef"));
                medusaAAMResearch.HideInUI = true;

                //This one is the source of the gamebreaking bug:
                /* AddItemToManufacturingReward("SY_EMPMissileMedusaAAM_VehicleWeaponDef",
                         "SYN_AerialWarfare_ResearchDef_ManufactureResearchRewardDef_0", "SYN_Aircraft_EMPMissile_ResearchDef");*/
                AddItemToManufacturingReward("ANU_Aircraft_Oracle_ResearchDef_ManufactureResearchRewardDef_0",
                    "ANU_AerialWarfare_ResearchDef_ManufactureResearchRewardDef_0", "ANU_Aircraft_Oracle_ResearchDef");
                CreateManufacturingReward("ANU_Aircraft_MutogCatapult_ResearchDef_ManufactureResearchRewardDef_0",
                    "ANU_Aircraft_ECMJammer_ResearchDef_ManufactureResearchRewardDef_0", "ANU_Aircraft_ECMJammer_ResearchDef", "ANU_Aircraft_MutogCatapult_ResearchDef",
                    "ANU_AdvancedBlimp_ResearchDef");
                CreateManufacturingReward("PX_Aircraft_Autocannon_ResearchDef_ManufactureResearchRewardDef_0", "SYN_Aircraft_SecurityStation_ResearchDef_ManufactureResearchRewardDef_0",
                      "SYN_Aircraft_SecurityStation_ResearchDef", "PX_Aircraft_Autocannon_ResearchDef",
                      "PX_Alien_Spawnery_ResearchDef");


                EncounterVariableResearchRequirementDef charunEncounterVariableResearchRequirement = Repo.GetAllDefs<EncounterVariableResearchRequirementDef>().
                   FirstOrDefault(ged => ged.name.Equals("ALN_Small_Flyer_ResearchDef_EncounterVariableResearchRequirementDef_0"));
                charunEncounterVariableResearchRequirement.VariableName = "CharunAreComing";

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
                aLN_Large_Flyer_ResearchDef.RevealRequirements.Container = reseachRequirementDefOpContainers;

                //Changes to FesteringSkies settings
                FesteringSkiesSettingsDef festeringSkiesSettingsDef = Repo.GetAllDefs<FesteringSkiesSettingsDef>().FirstOrDefault(gvw => gvw.name.Equals("FesteringSkiesSettingsDef"));
                festeringSkiesSettingsDef.SpawnInfestedAircraftChance = 0;
                festeringSkiesSettingsDef.InfestedAircraftChance = 0;
                festeringSkiesSettingsDef.InfestedAircrafts.Clear();
                festeringSkiesSettingsDef.InfestedAircraftRebuildHours = 100000;

                InterceptionGameDataDef interceptionGameDataDef = Repo.GetAllDefs<InterceptionGameDataDef>().FirstOrDefault(gvw => gvw.name.Equals("InterceptionGameDataDef"));
                interceptionGameDataDef.DisengageDuration = 3;

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

        public static void AddItemToManufacturingReward(string researchReward, string reward, string research)
        {

            try
            {

                ManufactureResearchRewardDef researchRewardDef = Repo.GetAllDefs<ManufactureResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals(researchReward));
                ManufactureResearchRewardDef rewardDef = Repo.GetAllDefs<ManufactureResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals(reward));

                ResearchDef researchDef = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(gvw => gvw.name.Equals(research));
                List<ItemDef> rewards = rewardDef.Items.ToList();
                rewards.Add(researchRewardDef.Items[0]);
                rewardDef.Items = rewards.ToArray();
                researchDef.HideInUI = true;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void CreateManufacturingReward(string researchReward1, string researchReward2, string research, string research2, string newResearch)
        {

            try
            {

                ManufactureResearchRewardDef researchReward1Def = Repo.GetAllDefs<ManufactureResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals(researchReward1));
                ManufactureResearchRewardDef researchReward2Def = Repo.GetAllDefs<ManufactureResearchRewardDef>().FirstOrDefault(gvw => gvw.name.Equals(researchReward2));
                ResearchDef researchDef = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(gvw => gvw.name.Equals(research));
                ResearchDef research2Def = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(gvw => gvw.name.Equals(research2));
                ResearchDef newResearchDef = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(gvw => gvw.name.Equals(newResearch));
                List<ItemDef> rewards = researchReward2Def.Items.ToList();
                rewards.Add(researchReward1Def.Items[0]);
                researchReward2Def.Items = rewards.ToArray();
                newResearchDef.Unlocks = researchDef.Unlocks;
                newResearchDef.Unlocks[0] = researchReward2Def;
                researchDef.HideInUI = true;
                research2Def.HideInUI = true;

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

            public static void Postfix(GeoBehemothActor __instance)
            {
                try
                {
                    if (__instance.CurrentBehemothStatus == GeoBehemothActor.BehemothStatus.Dormant)//first check
                    {
                        TFTVLogger.Always("Behemoth's target lists are cleared because he is sleeping");
                        targetsForBehemoth.Clear();
                      //  targetsVisitedByBehemoth.Clear();
                        behemothScenicRoute.Clear();
                        behemothTarget = 0;
                        return;
                    }
                    if (__instance.IsSubmerging)//second check
                    {
                        TFTVLogger.Always("Behemoth's target lists are cleared because he is going to sleep");
                        targetsForBehemoth.Clear();
                        behemothScenicRoute.Clear();
                        behemothTarget = 0;
                        return;
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
                            return;
                        }

                        typeof(GeoBehemothActor).GetMethod("TargetHaven", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { chosenHaven });
                        return;
                        
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

                }//end of try

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

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
                TFTVLogger.Always("TargetsForBehemoth counts " + targetsForBehemoth + " and/but counted as 0, so here we are");
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

                    foreach (GeoSite site in geoBehemothActor.GeoLevel.Map.AllSites)
                    {
                        if (behemothScenicRoute.Contains(site.SiteId))
                        {
                            chosenTarget = site;
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


