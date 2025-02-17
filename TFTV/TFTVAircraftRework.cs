using Assets.Code.PhoenixPoint.Geoscape.Entities.Sites.TheMarketplace;
using Base.Core;
using Base.Defs;
using Base.Levels;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Abilities;
using PhoenixPoint.Geoscape.Entities.Interception;
using PhoenixPoint.Geoscape.Entities.Interception.Equipments;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.DataObjects;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewControllers.AircraftEquipment;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using PhoenixPoint.Geoscape.View.ViewControllers.VehicleEquipmentInventory;
using PhoenixPoint.Geoscape.View.ViewControllers.VehicleRoster;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using UnityEngine;

namespace TFTV
{
    class TFTVAircraftRework
    {
        public static bool AircraftReworkOn = false;
        public static float MistSpeedMalus = 0.2f;
        public static float MistSpeedBuff = 2f;

        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;


        private static readonly GeoVehicleDef manticore = DefCache.GetDef<GeoVehicleDef>("PP_Manticore_Def");
        private static readonly GeoVehicleDef helios = DefCache.GetDef<GeoVehicleDef>("SYN_Helios_Def");
        private static readonly GeoVehicleDef thunderbird = DefCache.GetDef<GeoVehicleDef>("NJ_Thunderbird_Def");
        private static readonly GeoVehicleDef blimp = DefCache.GetDef<GeoVehicleDef>("ANU_Blimp_Def");

        private static readonly GeoVehicleDef maskedManticore = DefCache.GetDef<GeoVehicleDef>("PP_MaskedManticore_Def");

        private static GeoScanComponentDef _geoScanVehicleScannerComponent = null;
        private static ScanAbilityDef _scanAbilityDef = DefCache.GetDef<ScanAbilityDef>("ScanAbilityDef");
        private static ScanAbilityDef _thunderbirdScanAbilityDef = null;

        private static GeoVehicleModuleDef _basicRangeModule = null; //implemented works!
        private static GeoVehicleModuleDef _basicSpeedModule = null; //implemented works!
        private static GeoVehicleModuleDef _basicScannerModule = null; //implemented works!
        private static GeoVehicleModuleDef _basicPassengerModule = null; //implemented works!
        private static GeoVehicleModuleDef _basicClinicModule = null; //implemented works!
        private static GeoVehicleModuleDef _vehicleHarnessModule = null; //implemented
        private static GeoVehicleModuleDef _captureDronesModule = null; //implemented
        private static GeoVehicleModuleDef _blimpSpeedModule = null; //implemented
        private static GeoVehicleModuleDef _blimpMutationLabModule = null; //implemented
        private static GeoVehicleModuleDef _blimpMutogPenModule = null; //implemented
        private static GeoVehicleModuleDef _blimpWPModule = null; //implemented
        private static GeoVehicleModuleDef _heliosSpeedModule = null; //implemented
        private static GeoVehicleModuleDef _heliosMistRepellerModule = null; //implemented
        private static GeoVehicleModuleDef _heliosStatisChamberModule = null; //implemented
        private static GeoVehicleModuleDef _heliosStealthModule = null; //implemented
        private static GeoVehicleModuleDef _thunderbirdRangeModule = null; //implemented
        private static GeoVehicleModuleDef _thunderbirdWorkshopModule = null;  //implemented
        private static GeoVehicleModuleDef _thunderbirdGroundAttackModule = null;
        private static GeoVehicleModuleDef _thunderbirdScannerModule = null; //implemented
        private static GeoVehicleModuleDef _scyllaCaptureModule = null; //implemented

        private static List<GeoVehicleModuleDef> _thunderbirdModules = new List<GeoVehicleModuleDef>();
        private static List<GeoVehicleModuleDef> _heliosModules = new List<GeoVehicleModuleDef>();
        private static List<GeoVehicleModuleDef> _blimpModules = new List<GeoVehicleModuleDef>();
        private static List<GeoVehicleModuleDef> _basicModules = new List<GeoVehicleModuleDef>();

        private static StanceStatusDef _heliosStealthModuleStatus = null;

        private static GeoMarketplaceItemOptionDef _rangeModuleMarketPlaceItemOptionDef = null;
        private static GeoMarketplaceItemOptionDef _speedModuleMarketPlaceItemOptionDef = null;

        private static List<GeoMarketplaceItemOptionDef> _listOfModulesSoldInMarketplace = new List<GeoMarketplaceItemOptionDef>();

        internal class MarketPlace
        {

            public static void GenerateMarketPlaceModules(GeoMarketplace geoMarketplace)
            {
                try
                {
                    foreach (GeoMarketplaceItemOptionDef geoMarketplaceItemOptionDef in _listOfModulesSoldInMarketplace)
                    {

                        int price = (int)(UnityEngine.Random.Range(geoMarketplaceItemOptionDef.MinPrice, geoMarketplaceItemOptionDef.MaxPrice));

                        GeoEventChoice item = TFTVChangesToDLC5.TFTVMarketPlaceGenerateOffers.GenerateItemChoice(geoMarketplaceItemOptionDef.ItemDef, price);

                        geoMarketplace.MarketplaceChoices.Add(item);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }




            [HarmonyPatch(typeof(GeoEventChoiceOutcome), "GenerateFactionReward")]
            public static class GeoEventChoiceOutcome_GenerateFactionReward_Patch
            {
                static void Postfix(GeoEventChoiceOutcome __instance, GeoscapeEventContext context, string eventID, GeoFaction faction)
                {
                    try
                    {
                        if (!AircraftReworkOn)
                        {
                            return;
                        }

                        // TFTVLogger.Always($"{eventID} __instance.Items.Count(): {__instance.Items?.Count()}");

                        foreach (ItemUnit item in __instance?.Items)
                        {
                            // TFTVLogger.Always($"{eventID} item.ItemDef: {item.ItemDef?.name} item.ItemDef is GeoVehicleEquipmentDef: {item.ItemDef is GeoVehicleEquipmentDef}");

                            if (item.ItemDef != null && item.ItemDef is GeoVehicleEquipmentDef)
                            {
                                GeoVehicleEquipment geoVehicleEquipment = new GeoVehicleEquipment(item.ItemDef as GeoVehicleEquipmentDef);
                                faction.AircraftItemStorage.AddItem(geoVehicleEquipment);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }
        }

        internal class Defs
        {
            public static void CreateAndModifyDefs()
            {
                try
                {
                    CreateCaptureModule();

                    if (!AircraftReworkOn)
                    {
                        AircombatOption.AircraftAndAircombat();
                        return;
                    }

                    ModifyBaseStats();
                    ModifyLocKeys();
                    CreateModules();
                    RemoveAircombat();
                    CreateHeliosStealthModuleStatus();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CreateHeliosStealthModuleStatus()
            {
                try
                {
                    string name = "HeliosStealthModuleStatus";
                    StanceStatusDef source = DefCache.GetDef<StanceStatusDef>("Stealth_StatusDef");
                    StanceStatusDef newStatus = Helper.CreateDefFromClone(source, "{5A113FEB-9BA8-43C8-873D-F0705AB8FFE5}", name);

                    newStatus.Visuals = Helper.CreateDefFromClone(source.Visuals, "{9FA1F375-7C0F-4E56-8FF5-1756E3900938}", name);
                    newStatus.Visuals.DisplayName1.LocalizationKey = "TFTV_HELIOS_STEALTH_MODULE_STATUS_NAME";
                    newStatus.Visuals.Description.LocalizationKey = "TFTV_HELIOS_STEALTH_MODULE_STATUS_DESCRIPTION";

                    newStatus.DurationTurns = 2;
                    newStatus.StatModifications = new PhoenixPoint.Tactical.Entities.Equipments.ItemStatModification[] {new PhoenixPoint.Tactical.Entities.Equipments.ItemStatModification()
                    {
                    Modification = Base.Entities.Statuses.StatModificationType.Add,
                    TargetStat = PhoenixPoint.Common.Entities.StatModificationTarget.Stealth,
                    Value = 0.5f
                    } };
                    newStatus.EffectName = "HeliosStealthModuleBuff";
                    _heliosStealthModuleStatus = newStatus;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }

            //JetJumpAbility

            internal class AircombatOption
            {

                internal static void AircraftAndAircombat()
                {
                    try
                    {

                        ModifyAirCombatDefs();

                        RemoveHardFlyersTemplates();

                        ModifyDefsForPassengerModules();

                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }

                }

                private static void ModifyAirCombatDefs()
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


                        GeoVehicleWeaponDef voidChamberWDef = DefCache.GetDef<GeoVehicleWeaponDef>("ALN_VoidChamber_VehicleWeaponDef");
                        GeoVehicleWeaponDef spikesWDef = DefCache.GetDef<GeoVehicleWeaponDef>("ALN_Spikes_VehicleWeaponDef");
                        GeoVehicleWeaponDef ramWDef = DefCache.GetDef<GeoVehicleWeaponDef>("ALN_Ram_VehicleWeaponDef");
                        GeoVehicleWeaponDef thunderboltWDef = DefCache.GetDef<GeoVehicleWeaponDef>("PX_ElectrolaserThunderboltHC9_VehicleWeaponDef");
                        GeoVehicleWeaponDef nomadWDef = DefCache.GetDef<GeoVehicleWeaponDef>("PX_BasicMissileNomadAAM_VehicleWeaponDef");
                        GeoVehicleWeaponDef railGunWDef = DefCache.GetDef<GeoVehicleWeaponDef>("NJ_RailgunMaradeurAC4_VehicleWeaponDef");
                        GeoVehicleWeaponDef laserGunWDef = DefCache.GetDef<GeoVehicleWeaponDef>("SY_LaserGunArtemisMkI_VehicleWeaponDef");

                        //Design decision
                        GeoVehicleModuleDef afterburnerMDef = DefCache.GetDef<GeoVehicleModuleDef>("PX_Afterburner_GeoVehicleModuleDef");
                        GeoVehicleModuleDef flaresMDef = DefCache.GetDef<GeoVehicleModuleDef>("PX_Flares_GeoVehicleModuleDef");
                        //   GeoVehicleModuleDef jammerMDef = DefCache.GetDef<GeoVehicleModuleDef>("AN_ECMJammer_GeoVehicleModuleDef");

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

                        afterburnerMDef.HitPoints = 250;
                        flaresMDef.HitPoints = 250;
                        //flaresMDef.AmmoCount = 3;
                        //jammerMDef.AmmoCount = 3;

                        ResearchDbDef ppResearchDB = DefCache.GetDef<ResearchDbDef>("pp_ResearchDB");
                        ResearchDbDef anuResearchDB = DefCache.GetDef<ResearchDbDef>("anu_ResearchDB");
                        ResearchDbDef njResearchDB = DefCache.GetDef<ResearchDbDef>("nj_ResearchDB");
                        ResearchDbDef synResearchDB = DefCache.GetDef<ResearchDbDef>("syn_ResearchDB");

                        //removing unnecessary researches 
                        synResearchDB.Researches.Remove(DefCache.GetDef<ResearchDef>("SYN_Aircraft_SecurityStation_ResearchDef"));
                        // ppResearchDB.Researches.Remove(DefCache.GetDef<ResearchDef>("PX_Aircraft_EscapePods_ResearchDef"));
                        njResearchDB.Researches.Remove(DefCache.GetDef<ResearchDef>("NJ_Aircraft_CruiseControl_ResearchDef"));
                        njResearchDB.Researches.Remove(DefCache.GetDef<ResearchDef>("NJ_Aircraft_FuelTank_ResearchDef"));


                        //Belial's suggestions, unlocking flares via PX Aerial Warfare, etc.
                        AddItemToManufacturingReward("PX_Aircraft_Flares_ResearchDef_ManufactureResearchRewardDef_0",
                            "PX_AerialWarfare_ResearchDef_ManufactureResearchRewardDef_0", "PX_Aircraft_Flares_ResearchDef");

                        ManufactureResearchRewardDef fenrirReward = DefCache.GetDef<ManufactureResearchRewardDef>("PX_Aircraft_VirophageGun_ResearchDef_ManufactureResearchRewardDef_0");
                        ManufactureResearchRewardDef virophageWeaponsReward = DefCache.GetDef<ManufactureResearchRewardDef>("PX_VirophageWeapons_ResearchDef_ManufactureResearchRewardDef_0");
                        List<ItemDef> rewardsVirophage = virophageWeaponsReward.Items.ToList();
                        rewardsVirophage.Add(fenrirReward.Items[0]);
                        virophageWeaponsReward.Items = rewardsVirophage.ToArray();
                        ResearchDef fenrirResearch = DefCache.GetDef<ResearchDef>("PX_Aircraft_VirophageGun_ResearchDef");
                        ppResearchDB.Researches.Remove(fenrirResearch);


                        ManufactureResearchRewardDef thunderboltReward = DefCache.GetDef<ManufactureResearchRewardDef>("PX_Aircraft_Electrolaser_ResearchDef_ManufactureResearchRewardDef_0");
                        ManufactureResearchRewardDef advancedLasersReward = DefCache.GetDef<ManufactureResearchRewardDef>("PX_AdvancedLaserTech_ResearchDef_ManufactureResearchRewardDef_0");
                        List<ItemDef> rewardsAdvancedLasers = advancedLasersReward.Items.ToList();
                        rewardsAdvancedLasers.Add(thunderboltReward.Items[0]);
                        advancedLasersReward.Items = rewardsAdvancedLasers.ToArray();
                        ResearchDef electroLaserResearch = DefCache.GetDef<ResearchDef>("PX_Aircraft_Electrolaser_ResearchDef");
                        ppResearchDB.Researches.Remove(electroLaserResearch);

                        ManufactureResearchRewardDef handOfTyrReward = DefCache.GetDef<ManufactureResearchRewardDef>("PX_Aircraft_HypersonicMissile_ResearchDef_ManufactureResearchRewardDef_0");
                        ManufactureResearchRewardDef advancedShreddingReward = DefCache.GetDef<ManufactureResearchRewardDef>("PX_AdvancedShreddingTech_ResearchDef_ManufactureResearchRewardDef_0");
                        List<ItemDef> rewardsAdvancedShredding = advancedShreddingReward.Items.ToList();
                        rewardsAdvancedShredding.Add(handOfTyrReward.Items[0]);
                        advancedShreddingReward.Items = rewardsAdvancedShredding.ToArray();
                        ResearchDef handOfTyrResearch = DefCache.GetDef<ResearchDef>("PX_Aircraft_HypersonicMissile_ResearchDef");
                        ppResearchDB.Researches.Remove(handOfTyrResearch);

                        AddItemToManufacturingReward("NJ_Aircraft_TacticalNuke_ResearchDef_ManufactureResearchRewardDef_0",
                            "NJ_GuidanceTech_ResearchDef_ManufactureResearchRewardDef_0", "NJ_Aircraft_TacticalNuke_ResearchDef");
                        ResearchDef tacticalNukeResearch = DefCache.GetDef<ResearchDef>("NJ_Aircraft_TacticalNuke_ResearchDef");
                        ResearchDef njGuidanceResearch = DefCache.GetDef<ResearchDef>("NJ_GuidanceTech_ResearchDef");
                        List<ResearchRewardDef> guidanceUnlocks = njGuidanceResearch.Unlocks.ToList();
                        guidanceUnlocks.Add(tacticalNukeResearch.Unlocks[1]);
                        njGuidanceResearch.Unlocks = guidanceUnlocks.ToArray();


                        AddItemToManufacturingReward("NJ_Aircraft_FuelTank_ResearchDef_ManufactureResearchRewardDef_0",
                            "NJ_VehicleTech_ResearchDef_ManufactureResearchRewardDef_0", "NJ_Aircraft_FuelTank_ResearchDef");

                        AddItemToManufacturingReward("NJ_Aircraft_CruiseControl_ResearchDef_ManufactureResearchRewardDef_0",
                            "SYN_Rover_ResearchDef_ManufactureResearchRewardDef_0", "NJ_Aircraft_CruiseControl_ResearchDef");

                        ManufactureResearchRewardDef medusaAAM = DefCache.GetDef<ManufactureResearchRewardDef>("SYN_Aircraft_EMPMissile_ResearchDef_ManufactureResearchRewardDef_0");
                        ManufactureResearchRewardDef synAirCombat = DefCache.GetDef<ManufactureResearchRewardDef>("SYN_NanoTech_ResearchDef_ManufactureResearchRewardDef_0");
                        List<ItemDef> rewards = synAirCombat.Items.ToList();
                        rewards.Add(medusaAAM.Items[0]);
                        synAirCombat.Items = rewards.ToArray();

                        ResearchDef nanotechResearch = DefCache.GetDef<ResearchDef>("SYN_NanoTech_ResearchDef");
                        ResearchDef medusaAAMResearch = DefCache.GetDef<ResearchDef>("SYN_Aircraft_EMPMissile_ResearchDef");
                        synResearchDB.Researches.Remove(medusaAAMResearch);
                        if (ppResearchDB.Researches.Contains(medusaAAMResearch))
                        {
                            ppResearchDB.Researches.Remove(medusaAAMResearch);
                        }
                        List<ResearchRewardDef> nanotechUnlocks = nanotechResearch.Unlocks.ToList();
                        nanotechUnlocks.Add(medusaAAMResearch.Unlocks[1]);
                        nanotechResearch.Unlocks = nanotechUnlocks.ToArray();

                        //This one is the source of the gamebreaking bug:
                        /* AddItemToManufacturingReward("SY_EMPMissileMedusaAAM_VehicleWeaponDef",
                                 "SYN_AerialWarfare_ResearchDef_ManufactureResearchRewardDef_0", "SYN_Aircraft_EMPMissile_ResearchDef");*/
                        AddItemToManufacturingReward("ANU_Aircraft_Oracle_ResearchDef_ManufactureResearchRewardDef_0",
                            "ANU_AerialWarfare_ResearchDef_ManufactureResearchRewardDef_0", "ANU_Aircraft_Oracle_ResearchDef");

                        ResearchDef anuAWResearch = DefCache.GetDef<ResearchDef>("ANU_AerialWarfare_ResearchDef");
                        ResearchDef oracleResearch = DefCache.GetDef<ResearchDef>("ANU_Aircraft_Oracle_ResearchDef");

                        List<ResearchRewardDef> anuAWUnlocks = anuAWResearch.Unlocks.ToList();
                        anuAWUnlocks.Add(oracleResearch.Unlocks[1]);
                        anuAWResearch.Unlocks = anuAWUnlocks.ToArray();


                        CreateManufacturingReward("ANU_Aircraft_MutogCatapult_ResearchDef_ManufactureResearchRewardDef_0",
                            "ANU_Aircraft_ECMJammer_ResearchDef_ManufactureResearchRewardDef_0", "ANU_Aircraft_ECMJammer_ResearchDef", "ANU_Aircraft_MutogCatapult_ResearchDef",
                            "ANU_AdvancedBlimp_ResearchDef");

                        ResearchDef advancedBlimpResearch = DefCache.GetDef<ResearchDef>("ANU_AdvancedBlimp_ResearchDef");
                        ResearchDef ecmResearch = DefCache.GetDef<ResearchDef>("ANU_Aircraft_ECMJammer_ResearchDef");
                        ResearchDef mutogCatapultResearch = DefCache.GetDef<ResearchDef>("ANU_Aircraft_MutogCatapult_ResearchDef");

                        List<ResearchRewardDef> advancedBlimpUnlocks = advancedBlimpResearch.Unlocks.ToList();
                        advancedBlimpUnlocks.Add(ecmResearch.Unlocks[1]);
                        advancedBlimpUnlocks.Add(mutogCatapultResearch.Unlocks[1]);
                        advancedBlimpResearch.Unlocks = advancedBlimpUnlocks.ToArray();

                        CreateManufacturingReward("PX_Aircraft_Autocannon_ResearchDef_ManufactureResearchRewardDef_0", "SYN_Aircraft_SecurityStation_ResearchDef_ManufactureResearchRewardDef_0",
                              "SYN_Aircraft_SecurityStation_ResearchDef", "PX_Aircraft_Autocannon_ResearchDef",
                              "PX_Alien_Spawnery_ResearchDef");

                        EncounterVariableResearchRequirementDef charunEncounterVariableResearchRequirement = DefCache.GetDef<EncounterVariableResearchRequirementDef>("ALN_Small_Flyer_ResearchDef_EncounterVariableResearchRequirementDef_0");
                        charunEncounterVariableResearchRequirement.VariableName = "CharunAreComing";

                        //Changing ALN Berith research req so that they only appear after certain ODI event
                        EncounterVariableResearchRequirementDef berithEncounterVariable = DefCache.GetDef<EncounterVariableResearchRequirementDef>("ALN_Medium_Flyer_ResearchDef_EncounterVariableResearchRequirementDef_0");
                        berithEncounterVariable.VariableName = "BerithResearchVariable";

                        //Changing ALN Abbadon research so they appear only in Third Act, or After ODI reaches apex
                        EncounterVariableResearchRequirementDef sourceVarResReq =
                           DefCache.GetDef<EncounterVariableResearchRequirementDef>("NJ_Bionics1_ResearchDef_EncounterVariableResearchRequirementDef_0");

                        //Creating new Research Requirements, each requiring a variable to be triggered  
                        EncounterVariableResearchRequirementDef variableResReqAbbadon = Helper.CreateDefFromClone(sourceVarResReq, "F8D9463A-69C5-47B1-B52A-061D898CEEF8", "AbbadonResReqDef");
                        variableResReqAbbadon.VariableName = "AbbadonResearchVariable";
                        //  EncounterVariableResearchRequirementDef variableResReqAbbadonAlt = Helper.CreateDefFromClone(sourceVarResReq, "F8D9463A-69C5-47B1-B52A-061D898CEEF8", "AbbadonResReqAltDef");
                        //  variableResReqAbbadonAlt.VariableName = "ODI_Complete";
                        //Altering researchDef, requiring Third Act to have started and adding an alternative way of revealing research if ODI is completed 
                        ResearchDef aLN_Large_Flyer_ResearchDef = DefCache.GetDef<ResearchDef>("ALN_Large_Flyer_ResearchDef");
                        //  aLN_Large_Flyer_ResearchDef.RevealRequirements.Operation = ResearchContainerOperation.ANY;

                        ReseachRequirementDefOpContainer[] reseachRequirementDefOpContainers = new ReseachRequirementDefOpContainer[1];
                        ResearchRequirementDef[] researchRequirementDefs = new ResearchRequirementDef[1];
                        researchRequirementDefs[0] = variableResReqAbbadon;

                        reseachRequirementDefOpContainers[0].Requirements = researchRequirementDefs;
                        aLN_Large_Flyer_ResearchDef.RevealRequirements.Container = reseachRequirementDefOpContainers;

                        //Changes to FesteringSkies settings
                        FesteringSkiesSettingsDef festeringSkiesSettingsDef = DefCache.GetDef<FesteringSkiesSettingsDef>("FesteringSkiesSettingsDef");
                        festeringSkiesSettingsDef.SpawnInfestedAircraftChance = 0;
                        festeringSkiesSettingsDef.InfestedAircraftChance = 0;
                        festeringSkiesSettingsDef.InfestedAircrafts.Clear();
                        festeringSkiesSettingsDef.InfestedAircraftRebuildHours = 100000;

                        InterceptionGameDataDef interceptionGameDataDef = DefCache.GetDef<InterceptionGameDataDef>("InterceptionGameDataDef");
                        interceptionGameDataDef.DisengageDuration = 3;

                        DefCache.GetDef<AlienRaidsSetupDef>("_AlienRaidsSetupDef").RaidPeriodHrs = 15;


                    }


                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void ModifyDefsForPassengerModules()
                {

                    try
                    {
                        //ID all the factions for later
                        GeoFactionDef PhoenixPoint = DefCache.GetDef<GeoFactionDef>("Phoenix_GeoPhoenixFactionDef");
                        GeoFactionDef NewJericho = DefCache.GetDef<GeoFactionDef>("NewJericho_GeoFactionDef");
                        GeoFactionDef Anu = DefCache.GetDef<GeoFactionDef>("Anu_GeoFactionDef");
                        GeoFactionDef Synedrion = DefCache.GetDef<GeoFactionDef>("Synedrion_GeoFactionDef");

                        //ID all craft for later
                        GeoVehicleDef manticore = DefCache.GetDef<GeoVehicleDef>("PP_Manticore_Def");
                        GeoVehicleDef helios = DefCache.GetDef<GeoVehicleDef>("SYN_Helios_Def");
                        GeoVehicleDef thunderbird = DefCache.GetDef<GeoVehicleDef>("NJ_Thunderbird_Def");
                        GeoVehicleDef blimp = DefCache.GetDef<GeoVehicleDef>("ANU_Blimp_Def");
                        GeoVehicleDef manticoreMasked = DefCache.GetDef<GeoVehicleDef>("PP_MaskedManticore_Def");

                        //Reduce all craft seating (except blimp) by 4 and create clones with previous seating

                        GeoVehicleDef manticoreNew = Helper.CreateDefFromClone(manticore, "83A7FD03-DB85-4CEE-BAED-251F5415B82B", "PP_Manticore_Def_6_Slots");
                        manticore.BaseStats.SpaceForUnits = 2;
                        GeoVehicleDef heliosNew = Helper.CreateDefFromClone(helios, "4F9026CB-EF42-44B8-B9C3-21181EC4E2AB", "SYN_Helios_Def_5_Slots");
                        helios.BaseStats.SpaceForUnits = 1;
                        GeoVehicleDef thunderbirdNew = Helper.CreateDefFromClone(thunderbird, "FDE7F0C2-8BA7-4046-92EB-F3462F204B2B", "NJ_Thunderbird_Def_7_Slots");
                        thunderbird.BaseStats.SpaceForUnits = 3;
                        GeoVehicleDef blimpNew = Helper.CreateDefFromClone(blimp, "B857B76D-BDDB-4CA9-A1CA-895A540B17C8", "ANU_Blimp_Def_12_Slots");
                        blimpNew.BaseStats.SpaceForUnits = 12;
                        GeoVehicleDef manticoreMaskedNew = Helper.CreateDefFromClone(manticoreMasked, "19B82FD8-67EE-4277-B982-F352A53ADE72", "PP_ManticoreMasked_Def_8_Slots");
                        manticoreMasked.BaseStats.SpaceForUnits = 4;

                        //Change Hibernation module
                        GeoVehicleModuleDef hibernationmodule = DefCache.GetDef<GeoVehicleModuleDef>("SY_HibernationPods_GeoVehicleModuleDef");
                        //Increase cost to 50% of Vanilla Manti
                        hibernationmodule.ManufactureMaterials = 600;
                        hibernationmodule.ManufactureTech = 75;
                        hibernationmodule.ManufacturePointsCost = 505;
                        hibernationmodule.ViewElementDef.DisplayName1.LocalizationKey = "TFTV_FARM_MODULE_NAME";
                        hibernationmodule.ViewElementDef.DisplayName2.LocalizationKey = "TFTV_FARM_MODULE_NAME";
                        hibernationmodule.ViewElementDef.Description.LocalizationKey = "TFTV_FARM_MODULE_DESCRIPTION";

                        //Change Cruise Control module
                        GeoVehicleModuleDef cruisecontrolmodule = DefCache.GetDef<GeoVehicleModuleDef>("NJ_CruiseControl_GeoVehicleModuleDef");
                        //Increase cost to 50% of Vanilla Manti
                        cruisecontrolmodule.ManufactureMaterials = 600;
                        cruisecontrolmodule.ManufactureTech = 75;
                        cruisecontrolmodule.ManufacturePointsCost = 505;
                        //increasing bonus to speed 
                        cruisecontrolmodule.GeoVehicleModuleBonusValue = 250;

                        cruisecontrolmodule.ViewElementDef.DisplayName1.LocalizationKey = "TFTV_CRUISE_CONTROL_MODULE_NAME";
                        cruisecontrolmodule.ViewElementDef.DisplayName2.LocalizationKey = "TFTV_CRUISE_CONTROL_MODULE_NAME";
                        cruisecontrolmodule.ViewElementDef.Description.LocalizationKey = "TFTV_CRUISE_CONTROL_MODULE_DESCRIPTION";

                        //Change Fuel Tank module
                        GeoVehicleModuleDef fueltankmodule = DefCache.GetDef<GeoVehicleModuleDef>("NJ_FuelTanks_GeoVehicleModuleDef");
                        //Increase cost to 50% of Vanilla Manti
                        fueltankmodule.ManufactureMaterials = 600;
                        fueltankmodule.ManufactureTech = 75;
                        fueltankmodule.ManufacturePointsCost = 505;
                        fueltankmodule.GeoVehicleModuleBonusValue = 2500;

                        fueltankmodule.ViewElementDef.DisplayName1.LocalizationKey = "TFTV_FUEL_TANK_MODULE_NAME";
                        fueltankmodule.ViewElementDef.DisplayName2.LocalizationKey = "TFTV_FUEL_TANK_MODULE_NAME";
                        fueltankmodule.ViewElementDef.Description.LocalizationKey = "TFTV_FUEL_TANK_MODULE_DESCRIPTION";

                        //Make Hibernation module available for manufacture from start of game - doesn't work because HM is not an ItemDef
                        //GeoPhoenixFactionDef phoenixFactionDef = DefCache.GetDef<GeoPhoenixFactionDef>("Phoenix_GeoPhoenixFactionDef");
                        //EntitlementDef festeringSkiesEntitlementDef = DefCache.GetDef<EntitlementDef>("FesteringSkiesEntitlementDef");
                        // phoenixFactionDef.AdditionalDLCItems.Add(new GeoFactionDef.DLCStartItems { DLC = festeringSkiesEntitlementDef, StartingManufacturableItems = hibernationmodule };               
                        //Change cost of Manti to 50% of Vanilla
                        VehicleItemDef mantiVehicle = DefCache.GetDef<VehicleItemDef>("PP_Manticore_VehicleItemDef");
                        mantiVehicle.ManufactureMaterials = 600;
                        mantiVehicle.ManufactureTech = 75;
                        mantiVehicle.ManufacturePointsCost = 505;
                        //Change cost of Helios to Vanilla minus cost of passenger module
                        VehicleItemDef heliosVehicle = DefCache.GetDef<VehicleItemDef>("SYN_Helios_VehicleItemDef");
                        heliosVehicle.ManufactureMaterials = 555;
                        heliosVehicle.ManufactureTech = 173;
                        heliosVehicle.ManufacturePointsCost = 510;
                        //Change cost of Thunderbird to Vanilla minus cost of passenger module
                        VehicleItemDef thunderbirdVehicle = DefCache.GetDef<VehicleItemDef>("NJ_Thunderbird_VehicleItemDef");
                        thunderbirdVehicle.ManufactureMaterials = 900;
                        thunderbirdVehicle.ManufactureTech = 113;
                        thunderbirdVehicle.ManufacturePointsCost = 660;

                        //Make HM research for PX, available after completing Phoenix Archives
                        ResearchDef hibernationModuleResearch = DefCache.GetDef<ResearchDef>("SYN_Aircraft_HybernationPods_ResearchDef");
                        ResearchDef sourcePX_SDI_ResearchDef = DefCache.GetDef<ResearchDef>("PX_SDI_ResearchDef");
                        hibernationModuleResearch.Faction = PhoenixPoint;
                        hibernationModuleResearch.RevealRequirements = sourcePX_SDI_ResearchDef.RevealRequirements;
                        hibernationModuleResearch.ResearchCost = 100;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void RemoveHardFlyersTemplates()
                {
                    try
                    {
                        GeoVehicleWeaponDef acidSpit = DefCache.GetDef<GeoVehicleWeaponDef>("ALN_AcidSpit_VehicleWeaponDef");
                        GeoVehicleWeaponDef spikes = DefCache.GetDef<GeoVehicleWeaponDef>("ALN_Spikes_VehicleWeaponDef");
                        GeoVehicleWeaponDef napalmBreath = DefCache.GetDef<GeoVehicleWeaponDef>("ALN_NapalmBreath_VehicleWeaponDef");
                        GeoVehicleWeaponDef ram = DefCache.GetDef<GeoVehicleWeaponDef>("ALN_Ram_VehicleWeaponDef");
                        GeoVehicleWeaponDef tick = DefCache.GetDef<GeoVehicleWeaponDef>("ALN_Tick_VehicleWeaponDef");
                        GeoVehicleWeaponDef voidChamber = DefCache.GetDef<GeoVehicleWeaponDef>("ALN_VoidChamber_VehicleWeaponDef");

                        /* GeoVehicleWeaponDamageDef shredDamage = DefCache.GetDef<GeoVehicleWeaponDamageDef>("Shred_GeoVehicleWeaponDamageDef"); 
                         GeoVehicleWeaponDamageDef regularDamage= DefCache.GetDef<GeoVehicleWeaponDamageDef>("Regular_GeoVehicleWeaponDamageDef");

                         tick.DamagePayloads[0] = new GeoWeaponDamagePayload { Damage = shredDamage, Amount = 20 };
                         tick.DamagePayloads.Add(new GeoWeaponDamagePayload { Damage = regularDamage, Amount = 60 });*/


                        GeoVehicleLoadoutDef charun2 = DefCache.GetDef<GeoVehicleLoadoutDef>("AL_Small2_VehicleLoadout");
                        GeoVehicleLoadoutDef charun4 = DefCache.GetDef<GeoVehicleLoadoutDef>("AL_Small4_VehicleLoadout");
                        GeoVehicleLoadoutDef berith1 = DefCache.GetDef<GeoVehicleLoadoutDef>("AL_Medium1_VehicleLoadout");
                        GeoVehicleLoadoutDef berith2 = DefCache.GetDef<GeoVehicleLoadoutDef>("AL_Medium2_VehicleLoadout");
                        GeoVehicleLoadoutDef berith3 = DefCache.GetDef<GeoVehicleLoadoutDef>("AL_Medium3_VehicleLoadout");
                        GeoVehicleLoadoutDef berith4 = DefCache.GetDef<GeoVehicleLoadoutDef>("AL_Medium4_VehicleLoadout");
                        GeoVehicleLoadoutDef abbadon1 = DefCache.GetDef<GeoVehicleLoadoutDef>("AL_Large1_VehicleLoadout");
                        GeoVehicleLoadoutDef abbadon2 = DefCache.GetDef<GeoVehicleLoadoutDef>("AL_Large2_VehicleLoadout");
                        GeoVehicleLoadoutDef abbadon3 = DefCache.GetDef<GeoVehicleLoadoutDef>("AL_Large3_VehicleLoadout");

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
                       AlienFlyerResearchRewardDef aLN_Small_FlyerLoadouts= DefCache.GetDef<AlienFlyerResearchRewardDef>("ALN_Small_Flyer_ResearchDef_FlyerLoadoutResearchRewardDef_0");
                        AL_Small1_VehicleLoadout: ALN_AcidSpit_VehicleWeaponDef, ALN_Spikes_VehicleWeaponDef
                        AL_Small2_VehicleLoadout: ALN_NapalmBreath_VehicleWeaponDef, ALN_AcidSpit_VehicleWeaponDef
                        AL_Small3_VehicleLoadout: ALN_Ram_VehicleWeaponDef, ALN_Spikes_VehicleWeaponDef

                        AlienFlyerResearchRewardDef aLN_Medium_FlyerLoadouts = DefCache.GetDef<AlienFlyerResearchRewardDef>("ALN_Medium_Flyer_ResearchDef_FlyerLoadoutResearchRewardDef_0");
                        AL_Medium1_VehicleLoadout: ALN_AcidSpit_VehicleWeaponDef, ALN_Spikes_VehicleWeaponDef, ALN_Spikes_VehicleWeaponDef, ALN_Tick_VehicleWeaponDef
                        AL_Medium2_VehicleLoadout: ALN_NapalmBreath_VehicleWeaponDef, ALN_NapalmBreath_VehicleWeaponDef, ALN_Ram_VehicleWeaponDef, ALN_Ram_VehicleWeaponDef
                        AL_Medium3_VehicleLoadout: ALN_AcidSpit_VehicleWeaponDef, ALN_Spikes_VehicleWeaponDef, ALN_Tick_VehicleWeaponDef, ALN_Ram_VehicleWeaponDef
                        AL_Small4_VehicleLoadout: ALN_NapalmBreath_VehicleWeaponDef, ALN_Tick_VehicleWeaponDef

                        AlienFlyerResearchRewardDef aLN_Large_FlyerLoadouts = DefCache.GetDef<AlienFlyerResearchRewardDef>("ALN_Large_Flyer_ResearchDef_FlyerLoadoutResearchRewardDef_0");
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

                private static void AddItemToManufacturingReward(string researchReward, string reward, string research)
                {

                    try
                    {

                        ResearchDbDef ppResearchDB = DefCache.GetDef<ResearchDbDef>("pp_ResearchDB");
                        ResearchDbDef anuResearchDB = DefCache.GetDef<ResearchDbDef>("anu_ResearchDB");
                        ResearchDbDef njResearchDB = DefCache.GetDef<ResearchDbDef>("nj_ResearchDB");
                        ResearchDbDef synResearchDB = DefCache.GetDef<ResearchDbDef>("syn_ResearchDB");

                        ManufactureResearchRewardDef researchRewardDef = DefCache.GetDef<ManufactureResearchRewardDef>(researchReward);
                        ManufactureResearchRewardDef rewardDef = DefCache.GetDef<ManufactureResearchRewardDef>(reward);

                        ResearchDef researchDef = DefCache.GetDef<ResearchDef>(research);
                        List<ItemDef> rewards = rewardDef.Items.ToList();
                        rewards.Add(researchRewardDef.Items[0]);
                        rewardDef.Items = rewards.ToArray();
                        if (ppResearchDB.Researches.Contains(researchDef))
                        {
                            ppResearchDB.Researches.Remove(researchDef);
                        }
                        if (anuResearchDB.Researches.Contains(researchDef))
                        {
                            anuResearchDB.Researches.Remove(researchDef);
                        }
                        if (njResearchDB.Researches.Contains(researchDef))
                        {
                            njResearchDB.Researches.Remove(researchDef);
                        }
                        if (synResearchDB.Researches.Contains(researchDef))
                        {
                            synResearchDB.Researches.Remove(researchDef);
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }

                }

                private static void CreateManufacturingReward(string researchReward1, string researchReward2, string research, string research2, string newResearch)
                {

                    try
                    {
                        ResearchDbDef ppResearchDB = DefCache.GetDef<ResearchDbDef>("pp_ResearchDB");
                        ResearchDbDef anuResearchDB = DefCache.GetDef<ResearchDbDef>("anu_ResearchDB");
                        ResearchDbDef njResearchDB = DefCache.GetDef<ResearchDbDef>("nj_ResearchDB");
                        ResearchDbDef synResearchDB = DefCache.GetDef<ResearchDbDef>("syn_ResearchDB");

                        ManufactureResearchRewardDef researchReward1Def = DefCache.GetDef<ManufactureResearchRewardDef>(researchReward1);
                        ManufactureResearchRewardDef researchReward2Def = DefCache.GetDef<ManufactureResearchRewardDef>(researchReward2);
                        ResearchDef researchDef = DefCache.GetDef<ResearchDef>(research);
                        ResearchDef research2Def = DefCache.GetDef<ResearchDef>(research2);
                        ResearchDef newResearchDef = DefCache.GetDef<ResearchDef>(newResearch);
                        List<ItemDef> rewards = researchReward2Def.Items.ToList();
                        rewards.Add(researchReward1Def.Items[0]);
                        researchReward2Def.Items = rewards.ToArray();
                        newResearchDef.Unlocks = researchDef.Unlocks;
                        newResearchDef.Unlocks[0] = researchReward2Def;

                        if (ppResearchDB.Researches.Contains(researchDef))
                        {
                            ppResearchDB.Researches.Remove(researchDef);
                        }
                        if (anuResearchDB.Researches.Contains(researchDef))
                        {
                            anuResearchDB.Researches.Remove(researchDef);
                        }
                        if (njResearchDB.Researches.Contains(researchDef))
                        {
                            anuResearchDB.Researches.Remove(researchDef);
                        }
                        if (synResearchDB.Researches.Contains(researchDef))
                        {
                            anuResearchDB.Researches.Remove(researchDef);
                        }
                        if (ppResearchDB.Researches.Contains(research2Def))
                        {
                            ppResearchDB.Researches.Remove(research2Def);
                        }
                        if (anuResearchDB.Researches.Contains(research2Def))
                        {
                            anuResearchDB.Researches.Remove(research2Def);
                        }
                        if (njResearchDB.Researches.Contains(research2Def))
                        {
                            anuResearchDB.Researches.Remove(research2Def);
                        }
                        if (synResearchDB.Researches.Contains(research2Def))
                        {
                            anuResearchDB.Researches.Remove(research2Def);
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }

                }

            }
            private static void CreateCaptureModule()
            {
                try
                {

                    ResearchDef scyllaCaptureModule = DefCache.GetDef<ResearchDef>("PX_Aircraft_EscapePods_ResearchDef");

                    scyllaCaptureModule.ViewElementDef.DisplayName1.LocalizationKey = "KEY_TFTV_CAPTURE_MODULE_RESEARCHDEF_NAME";
                    scyllaCaptureModule.ViewElementDef.RevealText.LocalizationKey = "KEY_TFTV_CAPTURE_MODULE_RESEARCHDEF_REVEAL";
                    scyllaCaptureModule.ViewElementDef.UnlockText.LocalizationKey = "KEY_TFTV_CAPTURE_MODULE_RESEARCHDEF_REVEAL";
                    scyllaCaptureModule.ViewElementDef.CompleteText.LocalizationKey = "KEY_TFTV_CAPTURE_MODULE_RESEARCHDEF_COMPLETE";

                    ExistingResearchRequirementDef existingResearchRequirementDef = DefCache.GetDef<ExistingResearchRequirementDef>("PX_Aircraft_EscapePods_ResearchDef_ExistingResearchRequirementDef_1");
                    existingResearchRequirementDef.ResearchID = "PX_Alien_Queen_ResearchDef";

                    scyllaCaptureModule.Tags = new ResearchTagDef[] { DefCache.GetDef<ResearchTagDef>("CriticalPath_ResearchTagDef") };
                    scyllaCaptureModule.RevealRequirements.Container =
                        new ReseachRequirementDefOpContainer[] { new ReseachRequirementDefOpContainer()
                    { Operation = ResearchContainerOperation.ANY, Requirements = new ResearchRequirementDef[] { existingResearchRequirementDef } } };
                    scyllaCaptureModule.ResearchCost = 500;

                    GeoVehicleModuleDef captureModule = DefCache.GetDef<GeoVehicleModuleDef>("PX_EscapePods_GeoVehicleModuleDef");

                    captureModule.ViewElementDef.DisplayName1.LocalizationKey = "KEY_TFTV_CAPTURE_MODULE_NAME";
                    captureModule.ViewElementDef.DisplayName2.LocalizationKey = "KEY_TFTV_CAPTURE_MODULE_NAME";
                    captureModule.ViewElementDef.Description.LocalizationKey = "KEY_TFTV_CAPTURE_MODULE_DESCRIPTION";

                    captureModule.ManufactureMaterials = 600;
                    captureModule.ManufactureTech = 75;
                    captureModule.ManufacturePointsCost = 505;

                    //Needs to be removed because it's a config option
                    ResearchDbDef ppResearchDB = DefCache.GetDef<ResearchDbDef>("pp_ResearchDB");
                    ppResearchDB.Researches.Remove(scyllaCaptureModule);

                    _scyllaCaptureModule = captureModule;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void RemoveAircombat()
            {
                try
                {
                    DefCache.GetDef<GeoscapeEventDef>("PROG_FS0_GeoscapeEventDef").GeoscapeEventData.Mute = true;
                    DefCache.GetDef<GeoscapeEventDef>("PROG_FS1_GeoscapeEventDef").GeoscapeEventData.Mute = true;
                    DefCache.GetDef<GeoscapeEventDef>("PROG_FS9_GeoscapeEventDef").GeoscapeEventData.Mute = true;
                    DefCache.GetDef<GeoscapeEventDef>("PROG_FS10_GeoscapeEventDef").GeoscapeEventData.Mute = true;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }

            private static void ModifyBaseStats()
            {
                try
                {
                    /* foreach (GeoRangeComponentDef geoRangeComponentDef1 in Repo.GetAllDefs<GeoRangeComponentDef>())
                      {
                          TFTVLogger.Always($"GeoRangeComponentDef: {geoRangeComponentDef1.name}", false);
                      }

                      foreach(GeoScanComponentDef geoScanComponentDef in Repo.GetAllDefs<GeoScanComponentDef>()) 
                      {
                          TFTVLogger.Always($"GeoScanComponentDef: {geoScanComponentDef.name}", false);

                      }

                      foreach(GeoScannerDef geoScannerDef in Repo.GetAllDefs<GeoScannerDef>())
                      {
                          TFTVLogger.Always($"GeoScannerDef: {geoScannerDef.name}", false);
                      }

                      foreach(ComponentSetDef componentSetDef in Repo.GetAllDefs<ComponentSetDef>().Where(c=>c.Components.Any(c2=>c2 is GeoScanComponentDef)))
                      {
                          TFTVLogger.Always($"ComponentSetDef: {componentSetDef.name}", false);
                      }

                      foreach (ComponentSetDef componentSetDef in Repo.GetAllDefs<ComponentSetDef>().Where(c => c.Components.Any(c2 => c2 is GeoRangeComponentDef)))
                      {
                          TFTVLogger.Always($"ComponentSetDef: {componentSetDef.name}", false);
                      }*/



                    helios.BaseStats.Speed = new EarthUnits(625);
                    thunderbird.BaseStats.Speed = new EarthUnits(405);
                    blimp.BaseStats.Speed = new EarthUnits(325);


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }

            private static void ModifyLocKeys()
            {
                try
                {
                    FesteringSkiesSettingsDef festeringSkiesSettingsDef = DefCache.GetDef<FesteringSkiesSettingsDef>("FesteringSkiesSettingsDef");
                    festeringSkiesSettingsDef.UISettings.GeoscapeModuleBonusStaminaString.LocalizationKey = "TFTV_CLINIC_BONUS";


                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }






            /* [HarmonyPatch(typeof(GeoRangeComponent), "OnActorInitialized")]
                public static class GeoRangeComponent_OnActorInitialized_Patch
                {
                    static bool Prefix(GeoRangeComponent __instance, ActorComponent actor, ref Transform ____rangeIndicator, ref GameObject ____rangeEffect)
                  {
                        try
                      {
                          PropertyInfo actorFieldInfo = typeof(GeoRangeComponent).GetProperty("Actor", BindingFlags.Public | BindingFlags.Instance);

                          TFTVLogger.Always($"actorFieldInfo null? {actorFieldInfo==null}");

                          actorFieldInfo.SetValue(__instance, (GeoActor)actor);

                          ____rangeIndicator = __instance.transform.Find(__instance.RangeDef.RangeTransformPath);

                          if (__instance.RangeDef.RangeEffectPrefab != null)
                          {
                              TFTVLogger.Always($"{__instance.RangeDef.name} passed check; ____rangeIndicator {____rangeIndicator?.name}");
                              ____rangeEffect = UnityEngine.Object.Instantiate(__instance.RangeDef.RangeEffectPrefab, ____rangeIndicator);
                              TFTVLogger.Always($"____rangeEffect.transform {____rangeEffect?.transform?.name}"); 

                              foreach(Component component in ____rangeIndicator.GetComponents<Component>())
                              {
                                  TFTVLogger.Always($"component {component.name}, {component.GetType()}");
                              }


                              ____rangeIndicator = ____rangeEffect.transform;
                          }

                          ____rangeIndicator.transform.localScale = new Vector3(0f, 2f, 0f);


                          return false;
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }
                }*/



            private static void CreateModules()
            {
                try
                {
                    CreateBasicRangeModule();
                    CreateBasicSpeedModule();
                    CreateBasicScannerModule();
                    CreateBasicPassengerModule();
                    CreateBasicClinicModule();
                    CreateVehicleHarnessModule();
                    CreateCaptureDronesModule();
                    CreateBlimpSpeedModule();
                    CreateBlimpMutationModule();
                    CreateBlimpMutogPenModule();
                    CreateBlimpWPModule();
                    CreateHeliosSpeedModule();
                    CreateHeliosStealthModule();
                    CreateHeliosMistRepellerModule();
                    CreateHeliosStatisChamberModule();
                    CreateThunderbirdRangeModule();
                    CreateThunderbirdWorkshopModule();
                    CreateThunderbirdScannerModule();
                    CreateThunderbirdGroundAttackModule();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CreateThunderbirdGroundAttackModule()
            {
                try
                {
                    string id = "Thunderbird_GroundAttack";
                    string name = $"TFTV{id}Module";
                    string guid1 = "{56D68AC9-907A-46E1-97E9-90E9061A9AF4}";
                    string guid2 = "{A35FDD7A-1EE8-4A25-8083-74148CDD9BAE}";
                    string nameKey = $"TFTV_{id.ToUpper()}_MODULE_NAME";
                    string descriptionKey = $"TFTV_{id.ToUpper()}_MODULE_DESCRIPTION";
                    Sprite smallIcon = Helper.CreateSpriteFromImageFile($"TFTV_{id}_Small.png");
                    Sprite largeIcon = smallIcon; //Helper.CreateSpriteFromImageFile("TFTVCaptureDronesModuleLargeIcon.png");

                    GeoVehicleModuleDef module = CreateModule(name, guid1, guid2, nameKey, descriptionKey, smallIcon, largeIcon);
                    ResearchDef unlockResearch = DefCache.GetDef<ResearchDef>("NJ_GuidanceTech_ResearchDef");
                    string guid3 = "{5F0542BC-6816-4434-A404-C459D82D8518}";
                    AddToResearchUnlock(unlockResearch, module, guid3);

                    _thunderbirdGroundAttackModule = module;
                    _thunderbirdModules.Add(_thunderbirdGroundAttackModule);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CreateThunderbirdScannerModule()
            {
                try
                {
                    string id = "Thunderbird_Scanner";
                    string name = $"TFTV{id}Module";
                    string guid1 = "{B85A6B0C-454E-466B-AA2D-C5D7212A093E}";
                    string guid2 = "{D3570385-799A-4E84-9DC1-5209CD5C14CB}";
                    string nameKey = $"TFTV_{id.ToUpper()}_MODULE_NAME";
                    string descriptionKey = $"TFTV_{id.ToUpper()}_MODULE_DESCRIPTION";
                    Sprite smallIcon = Helper.CreateSpriteFromImageFile($"TFTV_{id}_Small.png");
                    Sprite largeIcon = smallIcon; //Helper.CreateSpriteFromImageFile("TFTVCaptureDronesModuleLargeIcon.png");

                    GeoVehicleModuleDef module = CreateModule(name, guid1, guid2, nameKey, descriptionKey, smallIcon, largeIcon);
                    ResearchDef unlockResearch = DefCache.GetDef<ResearchDef>("NJ_SateliteUplink_ResearchDef");
                    string guid3 = "{0CE3E91D-77E8-4CF4-8949-58E72B63AFEB}";
                    AddToResearchUnlock(unlockResearch, module, guid3);

                    _thunderbirdScannerModule = module;

                    ScanAbilityDef scanAbilitySource = DefCache.GetDef<ScanAbilityDef>("ScanAbilityDef");
                    ScanAbilityDef newAbility = Helper.CreateDefFromClone(scanAbilitySource, "{7A5AC4ED-0560-43F6-8B03-DE7065134436}", id);

                    ComponentSetDef ancientProbeComp = DefCache.GetDef<ComponentSetDef>("PP_AncientSiteProbe");

                    GeoRangeComponentDef sourceGeoScan = DefCache.GetDef<GeoRangeComponentDef>("E_SiteScannerRange [PhoenixBase_GeoSite]");
                    GeoScannerDef scannerSource = DefCache.GetDef<GeoScannerDef>("E_PP_Scanner_Actor_ [PP_Scanner]");
                    GeoScanComponentDef geoScanComponentSource = DefCache.GetDef<GeoScanComponentDef>("E_Scan [PP_Scanner]");

                    GeoRangeComponentDef newRangeComponent = Helper.CreateDefFromClone(sourceGeoScan, "{6BCBACD1-887D-40B3-ABF0-7DAAA6CEBF93}", id);
                    newRangeComponent.RangeTransformPath = "GlobeOffset";


                    GeoScanComponentDef newScanComponent = Helper.CreateDefFromClone(geoScanComponentSource, "{C4296C4C-C890-446F-9423-DF9EF5774296}", id);
                    newScanComponent.SitesToFind.Add(GeoSiteType.AlienBase);

                    GeoScannerDef scannerDef = Helper.CreateDefFromClone(scannerSource, "{72331364-A129-4232-A79F-F352DC1972F6}", id);
                    scannerDef.MaximumRange.Value = 4000;
                    //  newGeoScanComponent.SitesToFind = new List<GeoSiteType>() { GeoSiteType.Haven };
                    //  newGeoScanComponent.RevealSites = true;

                    // newGeoScan.RangeEffectPrefab = null;

                    ComponentSetDef scannerCompSource = DefCache.GetDef<ComponentSetDef>("PP_Scanner");
                    ComponentSetDef scannerComp = Helper.CreateDefFromClone(scannerCompSource, "{B09B8F9F-99C7-4BCA-8ED2-9461628EF059}", id);
                    scannerComp.Prefab = ancientProbeComp.Prefab;
                    scannerComp.Components[0] = newRangeComponent;
                    scannerComp.Components[1] = newScanComponent;
                    scannerComp.Components[2] = scannerDef;

                    newAbility.ScanActorDef = scannerComp;

                    _thunderbirdScanAbilityDef = newAbility;

                    _thunderbirdModules.Add(_thunderbirdScannerModule);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


            private static void CreateThunderbirdWorkshopModule()
            {
                try
                {
                    string id = "Thunderbird_Workshop";
                    string name = $"TFTV{id}Module";
                    string guid1 = "{9B32615B-05E7-41B3-82D8-EFA46CA18B4D}";
                    string guid2 = "{EDA2B91B-27AA-40A9-A9CC-667F534A916B}";
                    string nameKey = $"TFTV_{id.ToUpper()}_MODULE_NAME";
                    string descriptionKey = $"TFTV_{id.ToUpper()}_MODULE_DESCRIPTION";
                    Sprite smallIcon = Helper.CreateSpriteFromImageFile($"TFTV_{id}_Small.png");
                    Sprite largeIcon = smallIcon; //Helper.CreateSpriteFromImageFile("TFTVCaptureDronesModuleLargeIcon.png");

                    GeoVehicleModuleDef module = CreateModule(name, guid1, guid2, nameKey, descriptionKey, smallIcon, largeIcon, GeoVehicleModuleDef.GeoVehicleModuleBonusType.Recuperation, 0.35f);
                    ResearchDef unlockResearch = DefCache.GetDef<ResearchDef>("NJ_Bionics2_ResearchDef");
                    string guid3 = "{7CFC62CB-008C-4545-9636-5DD3C738E565}";
                    AddToResearchUnlock(unlockResearch, module, guid3);

                    _thunderbirdWorkshopModule = module;
                    _thunderbirdModules.Add(_thunderbirdWorkshopModule);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }



            private static void CreateThunderbirdRangeModule()
            {
                try
                {
                    string id = "Thunderbird_Range";
                    string name = $"TFTV{id}Module";
                    string guid1 = "{6F33A029-1A03-46B2-BB1E-F77AA1AD7F0D}";
                    string guid2 = "{6FBCD03B-80B0-45B2-91AC-3E3DFD86E8C7}";
                    string nameKey = $"TFTV_{id.ToUpper()}_MODULE_NAME";
                    string descriptionKey = $"TFTV_{id.ToUpper()}_MODULE_DESCRIPTION";
                    Sprite smallIcon = Helper.CreateSpriteFromImageFile($"TFTV_{id}_Small.png");
                    Sprite largeIcon = smallIcon; //Helper.CreateSpriteFromImageFile("TFTVCaptureDronesModuleLargeIcon.png");

                    GeoVehicleModuleDef module = CreateModule(name, guid1, guid2, nameKey, descriptionKey, smallIcon, largeIcon, GeoVehicleModuleDef.GeoVehicleModuleBonusType.Range, 10000);
                    ResearchDef unlockResearch = DefCache.GetDef<ResearchDef>("NJ_SateliteUplink_ResearchDef");
                    string guid3 = "{364448DA-316F-46E1-8F80-E5DAA9DCB454}";
                    AddToResearchUnlock(unlockResearch, module, guid3);

                    _thunderbirdRangeModule = module;
                    _thunderbirdModules.Add(_thunderbirdRangeModule);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }



            private static void CreateHeliosStatisChamberModule()
            {
                try
                {
                    _heliosStatisChamberModule = DefCache.GetDef<GeoVehicleModuleDef>("SY_HibernationPods_GeoVehicleModuleDef");
                    _heliosStatisChamberModule.GeoVehicleModuleBonusValue = 40;
                    _heliosModules.Add(_heliosStatisChamberModule);

                    ResearchDef unlockResearch = DefCache.GetDef<ResearchDef>("SYN_NanoTech_ResearchDef");
                    string guid3 = "{E42A5F5B-4E0B-40C4-8A8B-D01CB3F5F5B0}";
                    AddToResearchUnlock(unlockResearch, _heliosStatisChamberModule, guid3);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CreateHeliosMistRepellerModule()
            {
                try
                {
                    string id = "Helios_MistRepeller";
                    string name = $"TFTV{id}Module";
                    string guid1 = "{E7DE3FC3-AE32-45C8-8DDF-F456F672F7C9}";
                    string guid2 = "{735B096D-2BB0-407C-81E7-164F03181921}";
                    string nameKey = $"TFTV_{id.ToUpper()}_MODULE_NAME";
                    string descriptionKey = $"TFTV_{id.ToUpper()}_MODULE_DESCRIPTION";
                    Sprite smallIcon = Helper.CreateSpriteFromImageFile($"TFTV_{id}_Small.png");
                    Sprite largeIcon = smallIcon; //Helper.CreateSpriteFromImageFile("TFTVCaptureDronesModuleLargeIcon.png");

                    GeoVehicleModuleDef module = CreateModule(name, guid1, guid2, nameKey, descriptionKey, smallIcon, largeIcon);
                    ResearchDef unlockResearch = DefCache.GetDef<ResearchDef>("SYN_MistRepellers_ResearchDef");
                    string guid3 = "{FF967692-7879-46DD-B792-183419B6CE49}";
                    AddToResearchUnlock(unlockResearch, module, guid3);

                    _heliosMistRepellerModule = module;
                    _heliosModules.Add(_heliosMistRepellerModule);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CreateHeliosStealthModule()
            {
                try
                {
                    string id = "Helios_Stealth";
                    string name = $"TFTV{id}Module";
                    string guid1 = "{F3725D18-5B02-494A-A181-066B4E84DE0D}";
                    string guid2 = "{2807D2A4-EA2D-442C-B6EC-A9821B0731B2}";
                    string nameKey = $"TFTV_{id.ToUpper()}_MODULE_NAME";
                    string descriptionKey = $"TFTV_{id.ToUpper()}_MODULE_DESCRIPTION";
                    Sprite smallIcon = Helper.CreateSpriteFromImageFile($"TFTV_{id}_Small.png");
                    Sprite largeIcon = smallIcon; //Helper.CreateSpriteFromImageFile("TFTVCaptureDronesModuleLargeIcon.png");

                    GeoVehicleModuleDef module = CreateModule(name, guid1, guid2, nameKey, descriptionKey, smallIcon, largeIcon);
                    ResearchDef unlockResearch = DefCache.GetDef<ResearchDef>("SYN_NanoHealing_ResearchDef");
                    string guid3 = "{3459C6F5-591E-4B5E-81D8-79A130A058AF}";
                    AddToResearchUnlock(unlockResearch, module, guid3);

                    _heliosStealthModule = module;
                    _heliosModules.Add(_heliosStealthModule);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CreateHeliosSpeedModule()
            {
                try
                {
                    string id = "Helios_Speed";
                    string name = $"TFTV{id}Module";
                    string guid1 = "{49DAB3D6-D06F-4F0B-A992-1C27FDE4F2D6}";
                    string guid2 = "{80405C7E-8993-4326-A830-384FF94257A7}";
                    string nameKey = $"TFTV_{id.ToUpper()}_MODULE_NAME";
                    string descriptionKey = $"TFTV_{id.ToUpper()}_MODULE_DESCRIPTION";
                    Sprite smallIcon = Helper.CreateSpriteFromImageFile($"TFTV_{id}_Small.png");
                    Sprite largeIcon = smallIcon; //Helper.CreateSpriteFromImageFile("TFTVCaptureDronesModuleLargeIcon.png");

                    GeoVehicleModuleDef module = CreateModule(name, guid1, guid2, nameKey, descriptionKey, smallIcon, largeIcon, GeoVehicleModuleDef.GeoVehicleModuleBonusType.Speed, 500);
                    ResearchDef unlockResearch = DefCache.GetDef<ResearchDef>("SYN_MoonMission_ResearchDef");
                    string guid3 = "{FFFC41F1-1289-4778-98E8-244868E3CA1C}";
                    AddToResearchUnlock(unlockResearch, module, guid3);

                    _heliosSpeedModule = module;
                    _heliosModules.Add(_heliosSpeedModule);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


            private static void CreateBlimpWPModule()
            {
                try
                {
                    string id = "Blimp_WP";
                    string name = $"TFTV{id}Module";
                    string guid1 = "{E5270335-8C55-400F-A60B-F16F5DC8C235}";
                    string guid2 = "{6C54E38F-6BC0-40C2-9FC5-4FB0DAAEC909}";
                    string nameKey = $"TFTV_{id.ToUpper()}_MODULE_NAME";
                    string descriptionKey = $"TFTV_{id.ToUpper()}_MODULE_DESCRIPTION";
                    Sprite smallIcon = Helper.CreateSpriteFromImageFile($"TFTV_{id}_Small.png");
                    Sprite largeIcon = smallIcon; //Helper.CreateSpriteFromImageFile("TFTVCaptureDronesModuleLargeIcon.png");

                    GeoVehicleModuleDef module = CreateModule(name, guid1, guid2, nameKey, descriptionKey, smallIcon, largeIcon);
                    ResearchDef unlockResearch = DefCache.GetDef<ResearchDef>("ANU_StimTech_ResearchDef");
                    string guid3 = "{8D90B9FD-FF4B-4C51-B679-1CABF65DAC73}";
                    AddToResearchUnlock(unlockResearch, module, guid3);

                    _blimpWPModule = module;
                    _blimpModules.Add(_blimpWPModule);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


            private static void CreateBlimpMutogPenModule()
            {
                try
                {
                    string id = "Blimp_Mutog_Pen";
                    string name = $"TFTV{id}Module";
                    string guid1 = "{C79BA8BF-9ECF-4DAB-B7D8-CC0B25FFB794}";
                    string guid2 = "{A8C80074-E919-46A4-BAB8-E5C1F4F9AE12}";
                    string nameKey = $"TFTV_{id.ToUpper()}_MODULE_NAME";
                    string descriptionKey = $"TFTV_{id.ToUpper()}_MODULE_DESCRIPTION";
                    Sprite smallIcon = Helper.CreateSpriteFromImageFile($"TFTV_{id}_Small.png");
                    Sprite largeIcon = smallIcon; //Helper.CreateSpriteFromImageFile("TFTVCaptureDronesModuleLargeIcon.png");

                    GeoVehicleModuleDef module = CreateModule(name, guid1, guid2, nameKey, descriptionKey, smallIcon, largeIcon);
                    ResearchDef unlockResearch = DefCache.GetDef<ResearchDef>("ANU_LeviathanChamber_ResearchDef");
                    string guid3 = "{0935D009-2AE0-4246-8B6F-346D122D38D5}";
                    AddToResearchUnlock(unlockResearch, module, guid3);

                    _blimpMutogPenModule = module;
                    _blimpModules.Add(_blimpMutogPenModule);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


            private static void CreateBlimpMutationModule()
            {
                try
                {
                    string id = "Blimp_MutationLab";
                    string name = $"TFTV{id}Module";
                    string guid1 = "{48B98461-66A5-4C43-BA78-F096B8D6D208}";
                    string guid2 = "{A137E6DB-C629-4E67-8A6C-218AD780F3C9}";
                    string nameKey = $"TFTV_{id.ToUpper()}_MODULE_NAME";
                    string descriptionKey = $"TFTV_{id.ToUpper()}_MODULE_DESCRIPTION";
                    Sprite smallIcon = Helper.CreateSpriteFromImageFile($"TFTV_{id}_Small.png");
                    Sprite largeIcon = smallIcon; //Helper.CreateSpriteFromImageFile("TFTVCaptureDronesModuleLargeIcon.png");

                    GeoVehicleModuleDef module = CreateModule(name, guid1, guid2, nameKey, descriptionKey, smallIcon, largeIcon);
                    ResearchDef unlockResearch = DefCache.GetDef<ResearchDef>("ANU_MutationTech3_ResearchDef");
                    string guid3 = "{178647BB-9EE3-4207-AA19-A8AF89DF2C50}";
                    AddToResearchUnlock(unlockResearch, module, guid3);

                    _blimpMutationLabModule = module;
                    _blimpModules.Add(_blimpMutationLabModule);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CreateBlimpSpeedModule()
            {
                try
                {
                    string id = "Blimp_Speed";
                    string name = $"TFTV{id}Module";
                    string guid1 = "{AB7DA352-15A5-49B3-85D9-BD9434F9FBFB}";
                    string guid2 = "{CB853BCD-9DF5-4AAC-8A41-DFBB33B542E5}";
                    string nameKey = $"TFTV_{id.ToUpper()}_MODULE_NAME";
                    string descriptionKey = $"TFTV_{id.ToUpper()}_MODULE_DESCRIPTION";
                    Sprite smallIcon = Helper.CreateSpriteFromImageFile($"TFTV_{id}_Small.png");
                    Sprite largeIcon = smallIcon; //Helper.CreateSpriteFromImageFile("TFTVCaptureDronesModuleLargeIcon.png");

                    _blimpSpeedModule = CreateModule(name, guid1, guid2, nameKey, descriptionKey, smallIcon, largeIcon);

                    ResearchDef unlockResearch = DefCache.GetDef<ResearchDef>("ANU_MutationTech2_ResearchDef");
                    string guid3 = "{8DB1B5E5-EC74-4318-9AF6-F1A4A27EE317}";
                    AddToResearchUnlock(unlockResearch, _blimpSpeedModule, guid3);
                    _blimpModules.Add(_blimpSpeedModule);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }




            [HarmonyPatch(typeof(ManufactureResearchReward), "GiveReward")]
            public static class ManufactureResearchReward_DetectSite_Patch
            {
                static void Postfix(ManufactureResearchReward __instance, GeoFaction faction)
                {
                    try
                    {
                        if (!AircraftReworkOn)
                        {
                            return;
                        }

                        TFTVLogger.Always($"ManufactureResearchReward: {__instance.Def.name}");


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            private static void AddToResearchUnlock(ResearchDef research, GeoVehicleModuleDef module, string guid)
            {
                try
                {
                    ManufactureResearchRewardDef researchRewardDef = null;

                    if (research.Unlocks.Any(u => u is ManufactureResearchRewardDef))
                    {
                        researchRewardDef = (ManufactureResearchRewardDef)research.Unlocks.FirstOrDefault(u => u is ManufactureResearchRewardDef);
                        researchRewardDef.Items = researchRewardDef.Items.AddToArray(module);
                        TFTVLogger.Always($"{research.Id} should grant {module.name} via {researchRewardDef.name}");
                    }
                    else
                    {
                        ManufactureResearchRewardDef researchRewardDefSource = DefCache.GetDef<ManufactureResearchRewardDef>("SYN_Aircraft_HybernationPods_ResearchDef_ManufactureResearchRewardDef_0");
                        researchRewardDef = Helper.CreateDefFromClone(researchRewardDefSource, guid, $"{module.name}");
                        researchRewardDef.Items = new ItemDef[] { module };
                        research.Unlocks = research.Unlocks.AddToArray(researchRewardDef);
                        TFTVLogger.Always($"{research.Id} should grant {module.name} via {researchRewardDef.name}");

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CreateBasicSpeedModule()
            {
                try
                {
                    GeoVehicleModuleDef speedModule = DefCache.GetDef<GeoVehicleModuleDef>("NJ_CruiseControl_GeoVehicleModuleDef");
                    speedModule.GeoVehicleModuleBonusValue = 250;
                    speedModule.Tags.RemoveAt(1);
                    _speedModuleMarketPlaceItemOptionDef = CreateMarketplaceItem(speedModule.name, "{EA57516D-AACF-41FA-BBDD-02248B4F45BD}", 400, 1, speedModule);
                    _basicSpeedModule = speedModule;
                    _basicModules.Add(speedModule);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }


            }

            private static void CreateBasicRangeModule()
            {
                try
                {
                    GeoVehicleModuleDef rangeModule = DefCache.GetDef<GeoVehicleModuleDef>("NJ_FuelTanks_GeoVehicleModuleDef");
                    rangeModule.GeoVehicleModuleBonusValue = 1000;
                    rangeModule.Tags.RemoveAt(1);
                    _rangeModuleMarketPlaceItemOptionDef = CreateMarketplaceItem(rangeModule.name, "{C0E985A9-E180-4C9C-A98B-B8E8E38FD9B1}", 400, 1, rangeModule);
                    _basicRangeModule = rangeModule;
                    _basicModules.Add(rangeModule);


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static GeoMarketplaceItemOptionDef CreateMarketplaceItem(string name, string gUID, int price, int availability, ItemDef itemDef)
            {
                try
                {

                    GeoMarketplaceItemOptionDef sourceItemOption = DefCache.GetDef<GeoMarketplaceItemOptionDef>("KasoBuggy_MarketplaceItemOptionDef");
                    GeoMarketplaceItemOptionDef newOption = Helper.CreateDefFromClone(sourceItemOption, gUID, name);

                    newOption.ItemDef = itemDef;

                    newOption.MinPrice = price - price / 10;
                    newOption.MaxPrice = price + price / 10;
                    newOption.Availability = availability;

                    TheMarketplaceSettingsDef marketplaceSettings = DefCache.GetDef<TheMarketplaceSettingsDef>("TheMarketplaceSettingsDef");
                    marketplaceSettings.PossibleOptions = marketplaceSettings.PossibleOptions.AddToArray(newOption);

                    _listOfModulesSoldInMarketplace.Add(newOption);

                    // TFTVLogger.Always($"{name}null? {DefCache.GetDef<GroundVehicleItemDef>($"{name}_VehicleItemDef") == null}");
                    return newOption;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void CreateCaptureDronesModule()
            {
                try
                {
                    string name = "TFTVCaptureDronesModule";
                    string guid1 = "{1BC45071-CC48-4F9B-B6AF-901C0F6C0637}";
                    string guid2 = "{7CC897A4-211A-459B-A398-3E4F640DDAE0}";
                    string nameKey = "TFTV_CAPTURE_DRONES_MODULE_NAME";
                    string descriptionKey = "TFTV_CAPTURE_DRONES_MODULE_DESCRIPTION";
                    Sprite smallIcon = Helper.CreateSpriteFromImageFile("TFTVCaptureDronesModuleSmallIcon.png");
                    Sprite largeIcon = smallIcon; //Helper.CreateSpriteFromImageFile("TFTVCaptureDronesModuleLargeIcon.png");

                    _captureDronesModule = CreateModule(name, guid1, guid2, nameKey, descriptionKey, smallIcon, largeIcon);

                    ResearchDef unlockResearch = DefCache.GetDef<ResearchDef>("PX_Alien_Spawnery_ResearchDef");
                    string guid3 = "{0ECC16DE-9DCA-4379-9D2A-0A828D89E8FF}";
                    AddToResearchUnlock(unlockResearch, _captureDronesModule, guid3);
                    _basicModules.Add(_captureDronesModule);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CreateVehicleHarnessModule()
            {
                try
                {
                    string name = "TFTVVehicleHarnessModule";
                    string guid1 = "{906A1418-0063-4728-99F7-2650AECB4E60}";
                    string guid2 = "{FBC4BC0A-6A6E-48BF-9710-D388576F8A6E}";
                    string nameKey = "TFTV_VEHICLE_HARNESS_MODULE_NAME";
                    string descriptionKey = "TFTV_VEHICLE_HARNESS_MODULE_DESCRIPTION";
                    Sprite smallIcon = Helper.CreateSpriteFromImageFile("TFTVVehicleHarnessModuleSmallIcon.png");
                    Sprite largeIcon = smallIcon;//Helper.CreateSpriteFromImageFile("TFTVVehicleHarnessModuleLargeIcon.png");

                    _vehicleHarnessModule = CreateModule(name, guid1, guid2, nameKey, descriptionKey, smallIcon, largeIcon);

                    ResearchDef unlockResearch = DefCache.GetDef<ResearchDef>("PX_Alien_LiveQueen_ResearchDef");
                    string guid3 = "{A5C7C767-ABBA-4B5A-9C7C-2A41EC6597CC}";
                    AddToResearchUnlock(unlockResearch, _vehicleHarnessModule, guid3);
                    _basicModules.Add(_vehicleHarnessModule);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }


            private static void CreateBasicScannerModule()
            {
                try
                {
                    string name = "TFTVScannerModule";
                    string guid1 = "{3664BA25-BC78-413F-BF8C-60E5F657F873}";
                    string guid2 = "{097DB01F-D289-451F-B4C5-34A9BE3CA72A}";
                    string nameKey = "TFTV_SCANNER_MODULE_NAME";
                    string descriptionKey = "TFTV_SCANNER_MODULE_DESCRIPTION";
                    Sprite smallIcon = Helper.CreateSpriteFromImageFile("TFTVScannerModuleSmallIcon.png");
                    Sprite largeIcon = smallIcon; //Helper.CreateSpriteFromImageFile("TFTVScannerModuleLargeIcon.png");

                    _basicScannerModule = CreateModule(name, guid1, guid2, nameKey, descriptionKey, smallIcon, largeIcon);

                    ScanAbilityDef scanAbility = DefCache.GetDef<ScanAbilityDef>("ScanAbilityDef");


                    // ScanAbilityDef newAbility = Helper.CreateDefFromClone(source, "{F13000CF-3F7F-45CB-A435-8F85B909D294}", "TFTVScanVehicleAbility");

                    scanAbility.ViewElementDef.ShowCharges = false;

                    ComponentSetDef ancientProbeComp = DefCache.GetDef<ComponentSetDef>("PP_AncientSiteProbe");

                    GeoRangeComponentDef sourceGeoScan = DefCache.GetDef<GeoRangeComponentDef>("E_SiteScannerRange [PhoenixBase_GeoSite]");


                    GeoRangeComponentDef newRangeComponent = Helper.CreateDefFromClone(sourceGeoScan, "{0F449AF2-3754-4923-9143-31E2A1A02660}", "TFTVGeoScan");
                    newRangeComponent.RangeTransformPath = "GlobeOffset";



                    GeoScanComponentDef geoScanComponentSource = DefCache.GetDef<GeoScanComponentDef>("E_Scan [PP_Scanner]");

                    GeoScanComponentDef newScanComponent = Helper.CreateDefFromClone(geoScanComponentSource, "{7853D47E-C0BD-4F0A-888D-C4AEF2B0983F}", "TFTVGeoScanComponent");
                    //  newGeoScanComponent.SitesToFind = new List<GeoSiteType>() { GeoSiteType.Haven };
                    //  newGeoScanComponent.RevealSites = true;

                    // newGeoScan.RangeEffectPrefab = null;

                    ComponentSetDef scannerComp = DefCache.GetDef<ComponentSetDef>("PP_Scanner");
                  //  scannerComp.Prefab = ancientProbeComp.Prefab;
                    scannerComp.Components[0] = newRangeComponent;
                    scannerComp.Components[1] = newScanComponent;

                    _geoScanVehicleScannerComponent = newScanComponent;

                    ResearchDef unlockResearch = DefCache.GetDef<ResearchDef>("PX_Alien_Colony_ResearchDef");
                    string guid = "{9EBC92B5-80C2-453B-8757-7320972F5512}";
                    AddToResearchUnlock(unlockResearch, _basicScannerModule, guid);
                    _basicModules.Add(_basicScannerModule);


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }

            private static void CreateBasicPassengerModule()
            {
                try
                {

                    string name = "TFTVManticorePassengerModule";
                    string guid1 = "{04CF7742-7C34-45A7-B71A-62466153CB92}";
                    string guid2 = "{7E44636D-1658-4989-9998-53E4D128FA14}";
                    string nameKey = "TFTV_PASSENGER_MODULE_NAME";
                    string descriptionKey = "TFTV_PASSENGER_MODULE_DESCRIPTION";
                    Sprite smallIcon = Helper.CreateSpriteFromImageFile("TFTVPassengerModuleSmallIcon.png");
                    Sprite largeIcon = smallIcon;

                    _basicPassengerModule = CreateModule(name, guid1, guid2, nameKey, descriptionKey, smallIcon, largeIcon, GeoVehicleModuleDef.GeoVehicleModuleBonusType.Speed, -200);

                    ResearchDef unlockResearch = DefCache.GetDef<ResearchDef>("PX_CaptureTech_ResearchDef");
                    string guid3 = "{D41E2FB9-9F94-4471-BA79-15BEA732AFFD}";
                    AddToResearchUnlock(unlockResearch, _basicPassengerModule, guid3);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }

            private static void CreateBasicClinicModule()
            {
                try
                {
                    string name = "TFTVBasicClinicModule";
                    string guid1 = "{AA0D3DE3-021A-4FB8-981C-E05CC36BBE75}";
                    string guid2 = "{7BA6E494-8128-44FA-8525-650D4B76B819}";
                    string nameKey = "TFTV_CLINIC_MODULE_NAME";
                    string descriptionKey = "TFTV_CLINIC_MODULE_DESCRIPTION";
                    Sprite smallIcon = Helper.CreateSpriteFromImageFile("TFTVBasicClinicSmallIcon.png");
                    Sprite largeIcon = smallIcon; // Helper.CreateSpriteFromImageFile("TFTVBasicClinicLargeIcon.png");

                    _basicClinicModule = CreateModule(name, guid1, guid2, nameKey, descriptionKey, smallIcon, largeIcon, GeoVehicleModuleDef.GeoVehicleModuleBonusType.Recuperation, 0.35f);

                    ResearchDef unlockResearch = DefCache.GetDef<ResearchDef>("PX_Alien_Acheron_ResearchDef");
                    string guid3 = "{62336005-44DB-4696-B1D1-83A2B7DD68E7}";
                    AddToResearchUnlock(unlockResearch, _basicClinicModule, guid3);
                    _basicModules.Add(_basicClinicModule);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }



            private static GeoVehicleModuleDef CreateModule(string name, string guid, string guid2, string nameKey, string descriptionKey, Sprite smallIcon, Sprite largeIcon,
                GeoVehicleModuleDef.GeoVehicleModuleBonusType bonusType = GeoVehicleModuleDef.GeoVehicleModuleBonusType.None, float bonusValue = 0)
            {
                try
                {
                    GeoVehicleModuleDef newModule = Helper.CreateDefFromClone(_basicRangeModule, guid, name);

                    newModule.ViewElementDef = Helper.CreateDefFromClone(_basicRangeModule.ViewElementDef, guid2, name + "ViewElementDef");
                    newModule.ViewElementDef.DisplayName1.LocalizationKey = nameKey;
                    newModule.ViewElementDef.DisplayName2.LocalizationKey = nameKey;
                    newModule.ViewElementDef.Description.LocalizationKey = descriptionKey;
                    newModule.ViewElementDef.SmallIcon = smallIcon;
                    newModule.ViewElementDef.LargeIcon = largeIcon;
                    newModule.ViewElementDef.InventoryIcon = largeIcon;
                    newModule.ViewElementDef.RosterIcon = largeIcon;
                    newModule.ViewElementDef.DeselectIcon = largeIcon;

                    newModule.BonusType = bonusType;
                    newModule.GeoVehicleModuleBonusValue = bonusValue;

                    return newModule;


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }

        }


        internal class Modules
        {
            internal class Tactical
            {
                public static bool ScannerModulePresent = false;
                public static bool CaptureDronesModulePresent = false;
                public static bool MistRepllerModulePresent = false;
                public static bool HeliosStealthModulePresent = false;
                public static bool BlimpWPModulePresent = false;
                public static bool HeliosPresent = false;
                public static bool HeliosStatisChamberPresent = false;

                [HarmonyPatch(typeof(DamageOverTimeResistanceStatus), "ApplyResistance")]
                public static class DamageOverTimeResistanceStatus_ApplyResistance_Patch
                {
                    static void Postfix(DamageOverTimeResistanceStatus __instance)
                    {
                        try
                        {
                            if (!AircraftReworkOn || !HeliosStatisChamberPresent)
                            {
                                return;
                            }

                            if (__instance.DamageOverTimeResistanceStatusDef == DefCache.GetDef<DamageOverTimeResistanceStatusDef>("NanoTech_StatusDef"))
                            {
                                TacticalActor tacticalActor = __instance.TacticalActor;

                                if (tacticalActor != null && tacticalActor.Status != null)
                                {
                                    tacticalActor.Status.UnapplyStatus(__instance);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }
                }

                public static void CheckTacticallyRelevantModulesOnVehicle(GeoVehicle geoVehicle)
                {
                    try
                    {
                        if (!AircraftReworkOn || geoVehicle == null)
                        {
                            return;
                        }


                        if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _basicScannerModule))
                        {
                            ScannerModulePresent = true;
                        }

                        if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _captureDronesModule))
                        {
                            CaptureDronesModulePresent = true;
                        }

                        if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _heliosMistRepellerModule))
                        {
                            MistRepllerModulePresent = true;
                        }

                        if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _heliosStealthModule))
                        {
                            HeliosStealthModulePresent = true;
                        }

                        if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _blimpWPModule))
                        {
                            BlimpWPModulePresent = true;
                        }

                        if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _heliosStatisChamberModule))
                        {
                            HeliosStatisChamberPresent = true;
                        }

                        if (geoVehicle.VehicleDef == helios)
                        {
                            HeliosPresent = true;
                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }

                }


                internal class FirstTurn
                {

                    public static void ImplementModuleEffectsOnFirstTurn(TacticalLevelController controller)
                    {
                        try
                        {
                            if (!AircraftReworkOn)
                            {
                                return;
                            }


                            ImplementHeliosStealthModule(controller);
                            ImplementBlimpWPModule(controller);
                            ImplementStasisChamberTactical(controller);
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }

                    }

                    //Stealth module
                    private static void ImplementHeliosStealthModule(TacticalLevelController controller)
                    {
                        try
                        {
                            if (!HeliosStealthModulePresent)
                            {
                                return;
                            }

                            foreach (TacticalActor tacticalActor in controller.GetFactionByCommandName("px").TacticalActors)
                            {
                                if (!tacticalActor.Status.HasStatus(_heliosStealthModuleStatus))
                                {
                                    tacticalActor.Status.ApplyStatus(_heliosStealthModuleStatus);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }

                    //Blimp WP Module
                    private static void ImplementBlimpWPModule(TacticalLevelController controller)
                    {
                        try
                        {
                            if (!BlimpWPModulePresent)
                            {
                                return;
                            }

                            foreach (TacticalActor tacticalActor in controller.GetFactionByCommandName("px").TacticalActors)
                            {
                                if (tacticalActor.CharacterStats.Willpower != null)
                                {
                                    tacticalActor.CharacterStats.WillPoints.Add(3);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }

                    //Helios Statis Chamber

                    private static void ImplementStasisChamberTactical(TacticalLevelController controller)
                    {
                        try
                        {
                            if (!HeliosStatisChamberPresent)
                            {
                                return;
                            }

                            DamageOverTimeResistanceStatusDef damageOverTimeResistanceStatusDef = DefCache.GetDef<DamageOverTimeResistanceStatusDef>("NanoTech_StatusDef");

                            foreach (TacticalActor tacticalActor in controller.GetFactionByCommandName("px").TacticalActors)
                            {
                                if (tacticalActor.Status != null && !tacticalActor.HasStatus(damageOverTimeResistanceStatusDef))
                                {
                                    tacticalActor.Status.ApplyStatus(damageOverTimeResistanceStatusDef);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }

                }
                internal class EveryPhoenixTurn
                {

                    public static void ImplementModuleEffectsOnEveryPhoenixTurn(TacticalFaction tacticalFaction)
                    {
                        try
                        {
                            if (!AircraftReworkOn)
                            {
                                return;
                            }

                            if (tacticalFaction.IsControlledByPlayer)
                            {
                                ImplementScannerTacticalAbility(tacticalFaction.TacticalLevel);
                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                    }

                    //Basic
                    //Range module big Panda detection
                    private static void ImplementScannerTacticalAbility(TacticalLevelController controller)
                    {
                        try
                        {
                            if (!ScannerModulePresent)
                            {
                                return;
                            }


                            List<TacticalActor> list = (from a in controller.Map.GetTacActors<TacticalActor>(controller.CurrentFaction, FactionRelation.Enemy)
                                                        where !controller.CurrentFaction.Vision.KnownActors.ContainsKey(a)
                                                        && a.IsActive
                                                        && a.TacticalPerception.TacticalPerceptionBaseDef.SizeSpottingMultiplier >= 2f
                                                        select a).ToList();
                            if (list.Count > 0)
                            {
                                foreach (TacticalActor a in list)
                                {
                                    controller.CurrentFaction.Vision.IncrementKnownCounter(a, KnownState.Located, 1, true);
                                }
                            }

                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                    }
                }


                //Capture drones
                // Always recover items

                [HarmonyPatch(typeof(UIStateRosterDeployment), "EnterState")]
                public static class UIStateRosterDeployment_EnterState_Patch
                {
                    public static void Prefix(UIStateRosterDeployment __instance)
                    {
                        try
                        {
                            if (!AircraftReworkOn)
                            {
                                return;
                            }

                            GeoMission mission = __instance.Mission;
                            GeoSite geoSite = mission.Site;
                            GeoVehicle geoVehicle = geoSite.GetPlayerVehiclesOnSite()?.FirstOrDefault();

                            if (geoVehicle != null)
                            {
                                if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _captureDronesModule) && mission.MissionDef.DontRecoverItems)
                                {
                                    CaptureDronesModulePresent = true;
                                    mission.MissionDef.DontRecoverItems = false;
                                }
                            }

                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }
                    public static void Postfix(UIStateRosterDeployment __instance)
                    {
                        try
                        {
                            if (!AircraftReworkOn)
                            {
                                return;
                            }

                            GeoMission mission = __instance.Mission;

                            if (CaptureDronesModulePresent)
                            {
                                mission.MissionDef.DontRecoverItems = true;
                                CaptureDronesModulePresent = false;
                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }
                }

                [HarmonyPatch(typeof(GeoMission), "ManageGear")]
                public static class GeoMission_ManageGear_Patch
                {
                    public static void Prefix(GeoMission __instance, out bool __state)
                    {
                        try
                        {
                            __state = false;

                            if (!AircraftReworkOn)
                            {
                                return;
                            }



                            if (CaptureDronesModulePresent && __instance.MissionDef.DontRecoverItems)
                            {
                                __instance.MissionDef.DontRecoverItems = false;
                                __state = true;
                            }

                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }
                    public static void Postfix(GeoMission __instance, in bool __state)
                    {
                        try
                        {
                            if (!AircraftReworkOn || !__state)
                            {
                                return;
                            }

                            if (CaptureDronesModulePresent)
                            {
                                __instance.MissionDef.DontRecoverItems = true;
                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }
                }

                //Helios:

                //Mist repeller effects

                [HarmonyPatch(typeof(TacticalLevelController), "OnLevelStateChanged")]
                public static class OnLevelStateChangedPatch
                {
                    // The __state parameter lets you pass data from prefix to postfix.
                    public static void Prefix(TacticalLevelController __instance, Level level, Level.State prevState, Level.State state, out bool? __state)
                    {
                        __state = null;
                        try
                        {
                            if (!AircraftReworkOn)
                            {
                                return;
                            }

                            // We only care about the Playing state, since that's when OnLevelStart is scheduled.
                            if (state == Level.State.Playing)
                            {
                                // Save the original value.
                                __state = __instance.TacticalGameParams.IsCorruptionActive;

                                // If our custom condition applies, disable corruption.
                                if (MistRepllerModulePresent)
                                {
                                    __instance.TacticalGameParams.IsCorruptionActive = false;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }

                    public static void Postfix(TacticalLevelController __instance, Level level, Level.State prevState, Level.State state, bool? __state)
                    {
                        try
                        {
                            // Only restore if we modified it.
                            if (state == Level.State.Playing && __state.HasValue)
                            {
                                __instance.TacticalGameParams.IsCorruptionActive = __state.Value;
                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }
                }


                public static float CheckForMistRepellerModuleOnAircraft()
                {
                    try
                    {
                        if (AircraftReworkOn && MistRepllerModulePresent)
                        {
                            return 0.5f;
                        }
                        else
                        {
                            return 1;
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                //Helios advantage
                [HarmonyPatch(typeof(SurviveTurnsFactionObjectiveDef), "GenerateObjective")]
                public static class SurviveTurnsFactionObjectiveDef_GenerateObjective_Patch
                {
                    public static void Prefix(SurviveTurnsFactionObjectiveDef __instance, TacticalLevelController level, TacticalFaction faction, out int? __state)//, int ____squadMaxDeployment)
                    {
                        try
                        {
                            __state = __instance.SurviveTurns;

                            if (AircraftReworkOn && MistRepllerModulePresent)
                            {
                                __instance.SurviveTurns = -1;
                            }

                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }

                    public static void Postfix(SurviveTurnsFactionObjectiveDef __instance, TacticalLevelController level, TacticalFaction faction, int? __state)//, int ____squadMaxDeployment)
                    {
                        try
                        {
                            if (AircraftReworkOn && MistRepllerModulePresent)
                            {
                                __instance.SurviveTurns = __state.Value;
                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }
                }




                //Stasis chamber nano protection effect

                //Thunderbird
                //Ground Attack Module


            }

            internal class Geoscape
            {

                public static void AddAbilityToGeoVehicle(GeoVehicle geoVehicle, GeoAbilityDef geoAbility)
                {
                    try
                    {
                        geoVehicle.AddAbility(geoAbility, geoVehicle);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }

                }

                public static void RemoveAbilityFromVehicle(GeoVehicle geoVehicle, GeoAbilityDef geoAbility)
                {
                    try
                    {
                        geoVehicle.RemoveAbility(geoAbility);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }

                }


                internal class PassengerModules
                {
                    private static bool CheckForPassengerModule(GeoVehicle geoVehicle)
                    {
                        try
                        {
                            return geoVehicle.Modules != null && geoVehicle.Modules.Count() > 0 && geoVehicle.Modules.Any(m =>
                               m != null && m.ModuleDef != null && (
                                    m.ModuleDef.BonusType == GeoVehicleModuleDef.GeoVehicleModuleBonusType.Speed ||
                                    m.ModuleDef.BonusType == GeoVehicleModuleDef.GeoVehicleModuleBonusType.SurvivalOdds ||
                                    m.ModuleDef.BonusType == GeoVehicleModuleDef.GeoVehicleModuleBonusType.Range ||
                                    m.ModuleDef.BonusType == GeoVehicleModuleDef.GeoVehicleModuleBonusType.Recuperation
                                )
                            );


                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }

                    public static void AdjustMaxCharacterSpacePassengerModules(GeoVehicle geoVehicle, ref int maxSpace)
                    {
                        try
                        {
                            if (AircraftReworkOn)
                            {
                                if (geoVehicle.Modules.Any(m => m?.ModuleDef == _basicPassengerModule))
                                {
                                    maxSpace += 2;
                                }
                            }
                            else
                            {
                                if (CheckForPassengerModule(geoVehicle))
                                {
                                    maxSpace += 4;
                                }
                            }

                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                    }

                    public static void AdjustAircraftInfoPassengerModules(GeoVehicle geoVehicle, ref AircraftInfoData aircraftInfo)
                    {
                        try
                        {
                            if (AircraftReworkOn)
                            {

                                if (geoVehicle.Modules.Any(m => m?.ModuleDef == _basicPassengerModule))
                                {
                                    aircraftInfo.MaxCrew += 2;
                                }
                            }
                            else
                            {
                                if (CheckForPassengerModule(geoVehicle))
                                {
                                    aircraftInfo.MaxCrew += 4;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                    }

                    public static void CheckAircraftNewPassengerCapacity(GeoVehicle geoVehicle)
                    {
                        try
                        {
                            if (geoVehicle.CurrentSite != null && geoVehicle.CurrentSite.Type == GeoSiteType.PhoenixBase)
                            {
                                /* if (!__instance.HasModuleBonusTo(GeoVehicleModuleDef.GeoVehicleModuleBonusType.Recuperation)
                                     || !__instance.HasModuleBonusTo(GeoVehicleModuleDef.GeoVehicleModuleBonusType.Speed)
                                     || !__instance.HasModuleBonusTo(GeoVehicleModuleDef.GeoVehicleModuleBonusType.Range)
                                     || !__instance.HasModuleBonusTo(GeoVehicleModuleDef.GeoVehicleModuleBonusType.SurvivalOdds))
                                 {*/
                                if (geoVehicle.UsedCharacterSpace > geoVehicle.MaxCharacterSpace)
                                {
                                    List<GeoCharacter> list = new List<GeoCharacter>(from u in geoVehicle.Units orderby u.OccupingSpace descending select u);
                                    foreach (GeoCharacter character in list)
                                    {
                                        if (geoVehicle.FreeCharacterSpace >= 0)
                                        {
                                            break;
                                        }
                                        geoVehicle.RemoveCharacter(character);
                                        geoVehicle.CurrentSite.AddCharacter(character);
                                    }
                                }
                                // }
                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }

                    }

                }
                internal class Scanning
                {

                    /*  [HarmonyPatch(typeof(PhoenixPoint.Geoscape.Levels.GeoscapeRegionDrawer))]
                      [HarmonyPatch("Init")]
                      public static class GeoscapeRegionDrawer_Init_Patch
                      {
                          static void Postfix(PhoenixPoint.Geoscape.Levels.GeoscapeRegionDrawer __instance)
                          {
                              try
                              {
                                  // Access the private fields using reflection
                                  var rendererField = typeof(PhoenixPoint.Geoscape.Levels.GeoscapeRegionDrawer).GetField("_renderer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                  var propertyBlockField = typeof(PhoenixPoint.Geoscape.Levels.GeoscapeRegionDrawer).GetField("_propertyBlock", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                                  if (rendererField != null && propertyBlockField != null)
                                  {
                                      var renderer = (MeshRenderer)rendererField.GetValue(__instance);
                                      var propertyBlock = (MaterialPropertyBlock)propertyBlockField.GetValue(__instance);

                                      // Clear any existing textures or materials
                                      propertyBlock.Clear();

                                      // Set a new color with transparency
                                      Color color = new Color(0f, 1f, 0f, 0.45f); // Green with 25% opacity
                                      propertyBlock.SetColor("_Color", color);

                                      // Ensure the shader supports transparency
                                      Material material = renderer.material;
                                      material.SetOverrideTag("RenderType", "Transparent");
                                      material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                                      material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                                      material.SetInt("_ZWrite", 0);
                                      material.DisableKeyword("_ALPHATEST_ON");
                                      material.EnableKeyword("_ALPHABLEND_ON");
                                      material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                                      material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                                      // Apply the property block to the renderer
                                      renderer.SetPropertyBlock(propertyBlock);
                                  }
                              }
                              catch (Exception e)
                              {
                                  TFTVLogger.Error(e);
                                  throw;
                              }
                          }
                      }*/


                    public static void CheckAircraftScannerAbility(GeoVehicle geoVehicle)
                    {
                        try
                        {
                            if (!AircraftReworkOn)
                            {
                                return;
                            }


                            if (geoVehicle.Modules != null && geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _basicScannerModule) && geoVehicle.GetAbility<ScanAbility>() == null)
                            {
                                AddAbilityToGeoVehicle(geoVehicle, _scanAbilityDef);
                            }
                            else if (geoVehicle.Modules != null && !geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _basicScannerModule) && geoVehicle.GetAbility<ScanAbility>() != null)
                            {

                                RemoveAbilityFromVehicle(geoVehicle, _scanAbilityDef);
                            }

                            if (geoVehicle.Modules != null && geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _thunderbirdScannerModule) && geoVehicle.GetAbility<ScanAbility>() == null)
                            {
                                AddAbilityToGeoVehicle(geoVehicle, _thunderbirdScanAbilityDef);
                            }
                            else if (geoVehicle.Modules != null && !geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _thunderbirdScannerModule) && geoVehicle.GetAbility<ScanAbility>() != null)
                            {
                                RemoveAbilityFromVehicle(geoVehicle, _thunderbirdScanAbilityDef);
                            }

                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }

                    }


                    [HarmonyPatch(typeof(GeoScanComponent), "DetectSite")]
                    public static class GeoScanComponent_DetectSite_Patch
                    {
                        static void Postfix(GeoScanComponent __instance, GeoSite site, GeoFaction owner)
                        {
                            try
                            {
                                if (!AircraftReworkOn)
                                {
                                    return;
                                }

                                if (__instance.ScanDef == _geoScanVehicleScannerComponent && site.Type == GeoSiteType.Haven && !site.GetInspected(__instance.Owner))
                                {
                                    site.SetInspected(owner, inspected: true);
                                }

                            }
                            catch (Exception e)
                            {
                                TFTVLogger.Error(e);
                                throw;
                            }
                        }
                    }

                }
                internal class Healing
                {

                    [HarmonyPatch(typeof(GeoPhoenixFaction), "UpdateCharactersInVehicles")]
                    public static class GeoPhoenixFaction_UpdateCharactersInVehicles_Patch
                    {
                        static void Postfix(GeoPhoenixFaction __instance)
                        {
                            try
                            {
                                if (!AircraftReworkOn)
                                {
                                    return;
                                }

                                foreach (GeoVehicle geoVehicle in __instance.Vehicles)
                                {
                                    if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _basicClinicModule))
                                    {
                                        foreach (GeoCharacter geoCharacter in geoVehicle.Soldiers)
                                        {
                                            geoCharacter.Heal(10);
                                        }
                                    }

                                    if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _heliosStatisChamberModule))
                                    {
                                        foreach (GeoCharacter geoCharacter in geoVehicle.Soldiers)
                                        {
                                            geoCharacter.Heal(500);
                                        }
                                    }


                                    if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _thunderbirdWorkshopModule))
                                    {
                                        foreach (GeoCharacter geoCharacter in geoVehicle.Units)
                                        {
                                            if (geoCharacter.ArmourItems.Any(a => a.ItemDef.Tags.Contains(Shared.SharedGameTags.BionicalTag))
                                                || geoCharacter.GameTags.Contains(Shared.SharedGameTags.VehicleTag))
                                            {
                                                geoCharacter.Heal(30);
                                                geoCharacter.Fatigue?.Stamina.AddRestrictedToMax(10);
                                            }
                                        }
                                    }

                                    if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _blimpMutationLabModule))
                                    {
                                        foreach (GeoCharacter geoCharacter in geoVehicle.Units)
                                        {
                                            if (geoCharacter.ArmourItems.Any(a => a.ItemDef.Tags.Contains(Shared.SharedGameTags.AnuMutationTag))
                                                || geoCharacter.GameTags.Contains(Shared.SharedGameTags.MutogTag)
                                                || geoCharacter.GameTags.Contains(Shared.SharedGameTags.MutoidTag))
                                            {
                                                geoCharacter.Heal(30);
                                                geoCharacter.Fatigue?.Stamina.AddRestrictedToMax(10);
                                            }
                                        }
                                    }


                                    //Modify to add healing as necessary depending on the module
                                    /* float moduleBonusFromCache = geoVehicle.GetModuleBonusFromCache(GeoVehicleModuleDef.GeoVehicleModuleBonusType.Recuperation);
                                     if (moduleBonusFromCache > 0f)
                                     {
                                         foreach (GeoCharacter geoCharacter in geoVehicle.Soldiers)
                                         {
                                             geoCharacter.Fatigue.Stamina.AddRestrictedToMax(moduleBonusFromCache);
                                         }
                                     }*/
                                }
                            }
                            catch (Exception e)
                            {
                                TFTVLogger.Error(e);
                                throw;
                            }
                        }
                    }

                }
                internal class VehiclesAndMutogs
                {
                    private static int CheckIfCharacterSpaceCostReduced(GeoVehicle geoVehicle, int occupancy)
                    {
                        try
                        {
                            bool hasHarness = geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _vehicleHarnessModule);
                            bool hasMutogPen = geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _blimpMutogPenModule);
                            bool isThunderbird = geoVehicle.VehicleDef == thunderbird;

                            if (!hasHarness && !hasMutogPen && !isThunderbird)
                            {
                                return occupancy;
                            }

                            List<GeoCharacter> geoCharacters = geoVehicle.Units.ToList();

                            int occupiedSpace = 0;

                            foreach (GeoCharacter geoCharacter in geoCharacters)
                            {
                                if (geoCharacter.TemplateDef.Volume == 3 && hasHarness)
                                {
                                    occupiedSpace += 1;
                                }
                                else if (geoCharacter.TemplateDef.Volume == 2 && hasMutogPen)
                                {
                                    occupiedSpace += 1;
                                }
                                else if (geoCharacter.TemplateDef.Volume == 3 && isThunderbird)
                                {
                                    occupiedSpace += 2;
                                }
                                else
                                {
                                    occupiedSpace += geoCharacter.TemplateDef.Volume;
                                }

                            }

                            return occupiedSpace;

                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }



                    [HarmonyPatch(typeof(GeoVehicle))]
                    [HarmonyPatch("CurrentOccupiedSpace", MethodType.Getter)]
                    public static class GeoVehicle_CurrentOccupiedSpace_Patch
                    {
                        public static void Postfix(GeoVehicle __instance, ref int __result)
                        {
                            try
                            {
                                if (!AircraftReworkOn)
                                {
                                    return;
                                }

                                __result = CheckIfCharacterSpaceCostReduced(__instance, __result);

                            }

                            catch (Exception e)
                            {
                                TFTVLogger.Error(e);
                                throw;
                            }
                        }
                    }


                    [HarmonyPatch(typeof(GeoCharacter), "get_OccupingSpace")]
                    public static class GeoCharacter_get_OccupingSpace_Patch
                    {
                        static void Postfix(GeoCharacter __instance, ref int __result)
                        {
                            try
                            {
                                if (!AircraftReworkOn)
                                {
                                    return;
                                }

                                if (__result > 1)
                                {
                                    GeoVehicle geoVehicle = TFTVCommonMethods.LocateSoldier(__instance);

                                    if (geoVehicle == null)
                                    {
                                        return;
                                    }

                                    bool hasHarness = geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _vehicleHarnessModule);
                                    bool hasMutogPen = geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _blimpMutogPenModule);
                                    bool isThunderbird = geoVehicle.VehicleDef == thunderbird;

                                    if (!hasHarness && !hasMutogPen && !isThunderbird)
                                    {
                                        return;
                                    }

                                    if (__instance.TemplateDef.Volume == 3 && hasHarness)
                                    {
                                        __result = 1;
                                    }
                                    else if (__instance.TemplateDef.Volume == 2 && hasMutogPen)
                                    {
                                        __result = 1;
                                    }
                                    else if (__instance.TemplateDef.Volume == 3 && isThunderbird)
                                    {
                                        __result = 2;
                                    }
                                    else
                                    {
                                        __result = __instance.TemplateDef.Volume;
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                TFTVLogger.Error(e);
                                throw;
                            }
                        }
                    }

                    [HarmonyPatch(typeof(TransferActionMenuElement), "Init")]
                    public static class TransferActionMenuElement_Init_Patch
                    {
                        static bool Prefix(TransferActionMenuElement __instance, IGeoCharacterContainer targetContainer, GeoRosterItem targetItem)
                        {
                            try
                            {
                                if (!AircraftReworkOn)
                                {
                                    return true;
                                }

                                if (targetContainer is GeoVehicle)
                                {

                                }
                                else
                                {
                                    return true;
                                }


                                if (targetItem.Character == null || targetItem.Character.TemplateDef.Volume < 2)
                                {
                                    return true;
                                }

                                GeoVehicle geoVehicle = targetContainer as GeoVehicle;
                                GeoCharacter geoCharacter = targetItem.Character;

                                bool hasHarness = false;//geoVehicle.Modules.Any(m =>m!=null && m.ModuleDef == _vehicleHarnessModule);
                                bool hasMutogPen = false;//geoVehicle.Modules.Any(m =>m!=null && m.ModuleDef == _blimpMutogPenModule);
                                bool isThunderbird = geoVehicle.VehicleDef == thunderbird;

                                int occupyingSpace = 1;

                                if (geoCharacter.TemplateDef.Volume == 3 && hasHarness)
                                {
                                    occupyingSpace = 1;
                                }
                                else if (geoCharacter.TemplateDef.Volume == 2 && hasMutogPen)
                                {
                                    occupyingSpace = 1;
                                }
                                else if (geoCharacter.TemplateDef.Volume == 3 && isThunderbird)
                                {
                                    occupyingSpace = 2;
                                }
                                else
                                {
                                    occupyingSpace = geoCharacter.TemplateDef.Volume;
                                }

                                PropertyInfo propertyInfo = typeof(TransferActionMenuElement).GetProperty("TargetContainer", BindingFlags.Public | BindingFlags.Instance);

                                propertyInfo.SetValue(__instance, targetContainer);

                                __instance.ContainerTextLabel.text = targetContainer.Name;
                                bool interactable = true;

                                __instance.ContainerTextLabel.text += $" [{targetContainer.CurrentOccupiedSpace}/{targetContainer.MaxCharacterSpace}]";
                                interactable = ((targetContainer.MaxCharacterSpace - targetContainer.CurrentOccupiedSpace >= occupyingSpace));


                                __instance.ErrorTooltip.gameObject.SetActive(value: false);

                                if (!CheckVehicleMutogVehicleCapacity(geoVehicle, geoCharacter, thunderbird, hasMutogPen))
                                {
                                    __instance.ErrorTooltip.TipKey = __instance.VehicleErrorTextBind;
                                    __instance.ErrorTooltip.gameObject.SetActive(value: true);
                                    interactable = false;
                                }

                                __instance.Button.interactable = interactable;

                                return false;
                            }
                            catch (Exception e)
                            {
                                TFTVLogger.Error(e);
                                throw;
                            }
                        }
                    }

                    private static bool CheckVehicleMutogVehicleCapacity(GeoVehicle geoVehicle, GeoCharacter geoCharacter, bool thunderBird = false, bool mutogPen = false)
                    {
                        try
                        {
                            bool alreadyHasVehicle = geoVehicle.Units.Any(c => c.GameTags.Any(t => t == Shared.SharedGameTags.VehicleTag));
                            bool alreadyHasMutog = geoVehicle.Units.Any(c => c.GameTags.Any(t => t == Shared.SharedGameTags.MutogTag));

                            if (geoCharacter.GameTags.Any(t => t == Shared.SharedGameTags.VehicleTag) && alreadyHasVehicle)
                            {
                                if (thunderBird)
                                {
                                    return true;
                                }
                                return false;
                            }

                            if (geoCharacter.GameTags.Any(t => t == Shared.SharedGameTags.MutogTag) && alreadyHasMutog)
                            {
                                if (mutogPen)
                                {
                                    return true;
                                }
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



                    [HarmonyPatch(typeof(UIStateRosterDeployment), "CheckForDeployment")]
                    public static class UIStateRosterDeployment_CheckForDeployment_Patch
                    {
                        public static bool Prefix(UIStateRosterDeployment __instance, IEnumerable<GeoCharacter> squad)//, int ____squadMaxDeployment)
                        {
                            try
                            {
                                if (!AircraftReworkOn)
                                {
                                    return true;
                                }
                                UIModuleDeploymentMissionBriefing missionBriefingModule = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.DeploymentMissionBriefingModule;
                                missionBriefingModule.DeployButton.SetInteractable(squad.Any());
                                missionBriefingModule.DeployButton.ResetButtonAnimations();

                                return false;

                            }
                            catch (Exception e)
                            {
                                TFTVLogger.Error(e);
                                throw;
                            }
                        }
                    }

                    [HarmonyPatch(typeof(GeoMission), "GetDeploymentSources")]
                    public static class GeoMission_GetDeploymentSource_Patch
                    {
                        public static void Postfix(GeoMission __instance, GeoFaction faction, IGeoCharacterContainer priorityContainer, ref List<IGeoCharacterContainer> __result)
                        {
                            try
                            {
                                if (!AircraftReworkOn)
                                {
                                    return;
                                }

                                if (priorityContainer == null)
                                {
                                    // Debug.LogError($"no vehicle was passed from sources!");
                                    // __result = new List<IGeoCharacterContainer>();

                                    priorityContainer = __instance.Site.Vehicles.FirstOrDefault((GeoVehicle v) => v.Owner == faction);

                                }

                                __result = new List<IGeoCharacterContainer> { priorityContainer };

                            }
                            catch (Exception e)
                            {
                                TFTVLogger.Error(e);
                                throw;
                            }
                        }
                    }


                }
            }
            internal class UI
            {


                [HarmonyPatch(typeof(UIModuleSelectionInfoBox), "SetExtendedGeoVehicleInfo")]
                public static class UIModuleSelectionInfoBox_SetExtendedGeoVehicleInfo_Patch
                {
                    public static bool Prefix(UIModuleSelectionInfoBox __instance, GeoVehicle vehicle)
                    {
                        try
                        {
                            if (!AircraftReworkOn)
                            {
                                return true;
                            }

                            AircraftInfoData aircraftInfo = vehicle.GetAircraftInfo();
                            __instance.VehicleCrewText.gameObject.SetActive(aircraftInfo.IsOwnedByViewer);
                            __instance.RootSeparator.gameObject.SetActive(aircraftInfo.IsOwnedByViewer);
                            __instance.PersonalCrewContainer.SetActive(vehicle.IsOwnedByViewer);

                            // This sets up common visuals (unchanged)
                            AircraftEquipmentViewController.AircraftCommonEquipmentVisualData aircraftCommonEquipmentVisualData
                                = new AircraftEquipmentViewController.AircraftCommonEquipmentVisualData(vehicle);
                            aircraftCommonEquipmentVisualData.IsFriendlyModule = vehicle.IsOwnedByViewer;
                            __instance.AircraftInfo.SetEquipmentData(aircraftCommonEquipmentVisualData);

                            __instance.DisengageText.gameObject.SetActive(!vehicle.CanRedirect);
                            __instance.VehicleCrewText.text = $"{aircraftInfo.CurrentCrew}/{aircraftInfo.MaxCrew}";
                            __instance.VehicleArmorText.text = aircraftInfo.CurrentArmor.ToString();

                            // --- Filter out weapons, show only modules, limit to 3 if desired ---
                            List<GeoVehicleEquipment> list = vehicle.Equipments
                                .Where(eq => eq != null && eq.IsModule) // only modules
                                .Take(3)                                // up to 3
                                .ToList();

                            // The original sets the bottom separator visible if there's at least 1 piece of equipment
                            __instance.BottomSeparator.SetActive(list.Count > 0);

                            // Then the original loops over each equipment, assigning it to an EquipmentButton
                            int num = 0;
                            for (num = 0; num < list.Count; num++)
                            {
                                __instance.EquipmentButtons[num].SetEquipment(list[num]);
                                __instance.EquipmentButtons[num].gameObject.SetActive(true);
                            }

                            // Hide any leftover buttons
                            for (int i = num; i < __instance.EquipmentButtons.Count; i++)
                            {
                                if (__instance.EquipmentButtons[i].gameObject.activeSelf)
                                {
                                    __instance.EquipmentButtons[i].gameObject.SetActive(false);
                                }
                            }

                            // Return false so the original method is skipped
                            return false;

                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }
                }





                [HarmonyPatch(typeof(UIModuleActionsBar), "SetEquipment")]
                public static class UIModuleActionsBar_SetEquipment_Patch
                {
                    static bool Prefix(ref List<GeoVehicleEquipment> equipments, ref List<ShortEquipmentInfoButton> ____shortEquipmentInfoButtons)
                    {
                        try
                        {
                            if (!AircraftReworkOn) 
                            {
                                return true;
                            }

                            // Filter out any equipment that is not a module.
                            // Assuming that modules have a definition of type GeoVehicleModuleDef.
                            equipments = equipments
                                .Where(e => e?.EquipmentDef is GeoVehicleModuleDef)
                                .ToList();



                            // Force the list to exactly 3 entries:
                            while (equipments.Count < 3)
                            {
                                equipments.Add(null);
                            }
                            if (equipments.Count > 3)
                            {
                                // If for some reason there are more than 3 modules, trim the list.
                                equipments = equipments.Take(3).ToList();
                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                        // Continue with the original SetEquipment using our modified list.
                        return true;
                    }
                }




                [HarmonyPatch(typeof(AircraftInfoController))]
                public static class AircraftInfoControllerPatch
                {
                    [HarmonyPatch(nameof(AircraftInfoController.SetInfo))]
                    [HarmonyPrefix]
                    public static bool SetInfoPrefix(AircraftInfoController __instance, AircraftInfoData aircraftInfoData, List<GeoCharacter> crew, string description, bool armorFieldEnabled, List<GeoVehicleEquipmentUIData> weapons, List<GeoVehicleEquipmentUIData> modules)
                    {
                        try
                        {
                            if (!AircraftReworkOn)
                            {
                                return true;
                            }

                            //  TFTVLogger.Always($"AircraftInfoController Set Info running for aircraft {aircraftInfoData.DisplayName}");

                            // Set the aircraft info as usual
                            if (__instance.AircraftArt != null)
                            {
                                __instance.AircraftArt.sprite = aircraftInfoData.DisplayImage;
                            }

                            if (__instance.AircraftName != null)
                            {
                                __instance.AircraftName.text = aircraftInfoData.DisplayName;
                            }

                            if (__instance.AircraftDescription != null)
                            {
                                __instance.AircraftDescription.text = description;
                            }

                            if (aircraftInfoData.MaxHitPoints != aircraftInfoData.CurrentHitPoints)
                            {
                                __instance.AircraftHitPoints.text = $"{aircraftInfoData.CurrentHitPoints}/{aircraftInfoData.MaxHitPoints}";
                            }
                            else
                            {
                                __instance.AircraftHitPoints.text = aircraftInfoData.MaxHitPoints.ToString();
                            }


                            __instance.AircraftArmor.gameObject.SetActive(false);


                            __instance.AircraftCapacity.text = aircraftInfoData.MaxCrew.ToString();
                            __instance.AircraftSpeed.text = aircraftInfoData.Speed.ToString();
                            __instance.AircraftRange.text = aircraftInfoData.Range.ToString();
                            __instance.AircraftCrewController.SetCrew(crew, aircraftInfoData.MaxCrew);

                            // TFTVLogger.Always($"__instance.showEquipments: {__instance.showEquipments}");

                            if (!__instance.showEquipments)
                            {
                                return false;
                            }

                            // Add three module slots
                            if (modules != null)
                            {
                               // TFTVLogger.Always($"Modules count: {modules.Count}");

                                foreach (GeoVehicleEquipmentUIData module in modules)
                                {
                                    TFTVLogger.Always($"Module: {module?.AircraftEquipmentDef.name}");
                                }


                                if (modules.Count >= 1)
                                {
                                    __instance.WeaponSlot01.SetItem(modules[0]);
                                }
                                else
                                {
                                    __instance.WeaponSlot01.ResetItem();
                                }

                                if (modules.Count >= 2)
                                {
                                    __instance.WeaponSlot02.SetItem(modules[1]);
                                }
                                else
                                {
                                    __instance.WeaponSlot02.ResetItem();
                                }

                                if (modules.Count >= 3)
                                {
                                    // Assuming you have added a second and third module slot in the AircraftInfoController class
                                    __instance.ModuleSlot.SetItem(modules[2]);
                                }
                                else
                                {
                                    __instance.ModuleSlot.ResetItem();
                                }
                            }

                            return false; // Skip the original method
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }
                }




                [HarmonyPatch(typeof(GeoVehicleRosterSlot), "UpdateVehicleEquipments")]
                public static class GeoVehicleRosterSlot_UpdateVehicleEquipments_Patch
                {
                    static bool Prefix(GeoVehicleRosterSlot __instance)
                    {

                        try
                        {
                            if (!AircraftReworkOn)
                            {
                                return true;
                            }

                            if (__instance.Vehicle != null)
                            {
                                GeoVehicle baseObject = __instance.Vehicle.GetBaseObject<GeoVehicle>();
                                __instance.VehicleHealthBar.SetValuePercent((float)baseObject.Stats.HitPoints / (float)baseObject.Stats.MaxHitPoints);
                                __instance.VehicleHealthText.text = baseObject.Stats.HitPoints.ToString() + "/" + baseObject.Stats.MaxHitPoints;
                                List<GeoVehicleEquipmentUIData> list = baseObject.Modules.Select((GeoVehicleEquipment m) => m?.CreateUIData()).ToList();
                                // List<GeoVehicleEquipmentUIData> list2 = baseObject.Modules.Select((GeoVehicleEquipment m) => m?.CreateUIData()).ToList();
                                if (list.Count >= 1)
                                {
                                    __instance.WeaponSlot01.SetItem(list[0]);
                                }
                                else
                                {
                                    __instance.WeaponSlot01.ResetItem();
                                }

                                if (list.Count >= 2)
                                {
                                    __instance.WeaponSlot02.SetItem(list[1]);
                                }
                                else
                                {
                                    __instance.WeaponSlot02.ResetItem();
                                }

                                if (list.Count >= 3)
                                {
                                    __instance.ModuleSlot.SetItem(list[2]);
                                }
                                else
                                {
                                    __instance.ModuleSlot.ResetItem();
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
                }



                [HarmonyPatch(typeof(UIVehicleEquipmentInventoryList), "Init")]
                public static class ModuleList_Init_Patch
                {
                    static void Prefix(UIVehicleEquipmentInventoryList __instance, ref IEnumerable<GeoVehicleEquipmentUIData> equipments)
                    {

                        try
                        {
                            if (!AircraftReworkOn)
                            {
                                return;
                            }

                            // Identify the module list. Adjust the condition as needed.
                            if (__instance.gameObject.name.Contains("Module"))
                            {
                                // Set the fixed slot count to 3.
                                __instance.FixedCount = 3;

                                // Ensure the ItemSlotPrefab is not null.
                                if (__instance.ItemSlotPrefab == null)
                                {
                                    // Try to use an existing slot as a template.
                                    var fallback = __instance.GetComponentsInChildren<UIVehicleEquipmentInventorySlot>(true).FirstOrDefault();
                                    if (fallback != null)
                                    {
                                        __instance.ItemSlotPrefab = fallback;
                                    }
                                    else
                                    {
                                        Debug.LogError("[ModuleList_Init_Patch] No existing slot found to use as fallback for ItemSlotPrefab");
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






                [HarmonyPatch(typeof(UIModuleVehicleEquip), "UpdateData")]
                public static class UIModuleVehicleEquip_UpdateData_Patch
                {
                    static void Postfix(UIModuleVehicleEquip __instance, IEnumerable<GeoVehicleEquipmentUIData> modules, bool ____inPhoenixBase)
                    {
                        try
                        {
                            if (!AircraftReworkOn)
                            {
                                return;
                            }

                            UIModuleVehicleRoster uIModuleVehicleRoster = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.VehicleRoster;
                            GeoVehicle selectedAircraft = uIModuleVehicleRoster.SelectedSlot.Vehicle.GetBaseObject<GeoVehicle>();

                            // 1) Hide weapon list
                            __instance.WeaponList?.gameObject.SetActive(false);

                            // 2) Ensure modules are not restricted
                            __instance.ModuleList.InventoryListFilter = null; // Option A: no filter at all
                                                                              //  __instance.ModuleList.ReplaceByDefault = false;

                            // 3) Let them be interactive (if needed)                     
                            __instance.ModuleList.EnableEventHandlers = ____inPhoenixBase || !__instance.DisableListsInTransit;
                            __instance.StorageList.EnableEventHandlers = ____inPhoenixBase || !__instance.DisableListsInTransit;

                            // 4) Reinit the module list
                            if (modules != null)
                            {
                                __instance.ModuleList.Deinit();
                                __instance.ModuleList.Init(modules);
                            }


                            bool IsModuleCompatible(GeoVehicleModuleDef moduleDef, GeoVehicleDef vehicleDef)
                            {

                                if (vehicleDef == manticore)
                                {
                                    return true;
                                }
                                else if (vehicleDef == blimp)
                                {
                                    if (_blimpModules.Contains(moduleDef) || _basicModules.Contains(moduleDef))
                                    {
                                        return true;
                                    }
                                }
                                else if (vehicleDef == helios)
                                {
                                    if (_heliosModules.Contains(moduleDef) || _basicModules.Contains(moduleDef))
                                    {
                                        return true;
                                    }
                                }
                                else if (vehicleDef == thunderbird)
                                {
                                    if (_thunderbirdModules.Contains(moduleDef) || _basicModules.Contains(moduleDef))
                                    {
                                        return true;
                                    }
                                }
                                return false;

                            }

                            // Now, set the filter on the storage list:
                            __instance.StorageList.SetFilter((GeoVehicleEquipmentDef def) =>
                            {
                                // Only show the module if it is a GeoVehicleModuleDef and is compatible with the current vehicle.
                                var moduleDef = def as GeoVehicleModuleDef;
                                if (moduleDef == null)
                                {
                                    return false;
                                }
                                return IsModuleCompatible(moduleDef, selectedAircraft.VehicleDef);
                            });

                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                    }
                }


                [HarmonyPatch(typeof(UIModuleVehicleEquip), "AttemptSlotSwap")]
                public static class PreventDuplicateModule_DragDrop_Patch
                {
                    static bool Prefix(UIModuleVehicleEquip __instance, UIVehicleEquipmentInventorySlot sourceSlot, UIVehicleEquipmentInventorySlot destinationSlot, ref bool __result)
                    {
                        try
                        {
                            if (!AircraftReworkOn)
                            {
                                return true;
                            }

                            // Only check if we're moving an item into the module list
                            // AND if the source is not already in the module list (i.e. we're equipping from storage).
                            if (sourceSlot.ParentList != __instance.ModuleList && destinationSlot.ParentList == __instance.ModuleList)
                            {
                                // If any module slot already has a module with the same definition as the one we're trying to add,
                                // then cancel the swap.
                                if (__instance.ModuleList.Slots.Any(s => !s.Empty && s.Equipment != null &&
                                       s.Equipment.AircraftEquipmentDef == sourceSlot.Equipment.AircraftEquipmentDef))
                                {
                                    __instance.DeselectSlot();
                                    __result = false;
                                    return false; // Skip original method.
                                }
                            }
                            // Otherwise, let the original method run.
                            return true;
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }
                }



                [HarmonyPatch(typeof(UIModuleVehicleEquip), "HandleDoubleclickOnSlot")]
                public static class ModuleDoubleClickRefinedPatch
                {
                    static bool Prefix(UIVehicleEquipmentInventorySlot clickedSlot, UIModuleVehicleEquip __instance, ref bool __result)
                    {
                        try
                        {
                            if (!AircraftReworkOn)
                            {
                                return true;
                            }

                            // Get references for clarity.
                            var moduleList = __instance.ModuleList;
                            var storageList = __instance.StorageList;

                            // Case 1: Double-click on a module in storage (attempting to equip it)
                            if (clickedSlot.ParentList == storageList)
                            {
                                // Save the new module (from storage) that we want to equip.
                                GeoVehicleEquipmentUIData newModule = clickedSlot.Equipment;
                                if (newModule == null)
                                {
                                    __result = false;
                                    return false;
                                }

                                // Check if a module with the same definition is already equipped.
                                if (moduleList.Slots.Any(s => !s.Empty && s.Equipment != null &&
                                        s.Equipment.AircraftEquipmentDef == newModule.AircraftEquipmentDef))
                                {
                                    Debug.Log("Module already equipped on aircraft. Duplicate not allowed.");
                                    __result = false;
                                    return false;
                                }

                                // Check for an empty slot in the module list.
                                var emptySlot = moduleList.Slots.FirstOrDefault(s => s.Empty);
                                if (emptySlot != null)
                                {
                                    // Use the original swap method to equip into an empty slot.
                                    var attemptMethod = AccessTools.Method(typeof(UIModuleVehicleEquip), "AttemptSlotSwap");
                                    bool success = (bool)attemptMethod.Invoke(__instance, new object[] { clickedSlot, emptySlot });
                                    __result = success;
                                    return false;
                                }
                                else
                                {
                                    // All module slots are filled.
                                    // We want to replace the oldest module by shifting the equipped modules.
                                    var slots = moduleList.Slots;

                                    // (Double-check for duplicates, though this should have been caught above.)
                                    if (slots.Any(s => !s.Empty && s.Equipment != null &&
                                        s.Equipment.AircraftEquipmentDef == newModule.AircraftEquipmentDef))
                                    {
                                        Debug.Log("Module already equipped on aircraft. Duplicate not allowed.");
                                        __result = false;
                                        return false;
                                    }

                                    // Save the module in slot 0 (the oldest) for removal.
                                    GeoVehicleEquipmentUIData oldModule = slots[0].Equipment;
                                    // Find a free slot in storage for the module being removed.
                                    var freeStorageSlot = storageList.GetFirstAvailableSlot(oldModule, false);
                                    if (freeStorageSlot == null)
                                    {
                                        Debug.LogError("No free storage slot available for the replaced module.");
                                        __result = false;
                                        return false;
                                    }

                                    // Swap the oldest module into storage.
                                    var attemptMethod = AccessTools.Method(typeof(UIModuleVehicleEquip), "AttemptSlotSwap");
                                    attemptMethod.Invoke(__instance, new object[] { slots[0], freeStorageSlot });

                                    // Build the new ordering:
                                    // • Slot 0 gets what was in slot 1.
                                    // • Slot 1 gets what was in slot 2.
                                    // • Slot 2 receives the new module.
                                    GeoVehicleEquipmentUIData[] newModules = new GeoVehicleEquipmentUIData[3];
                                    newModules[0] = slots[1].Equipment;
                                    newModules[1] = slots[2].Equipment;
                                    newModules[2] = newModule;

                                    // Remove the new module from storage.
                                    clickedSlot.Equipment = null;

                                    // Reinitialize the module list with the new ordering.
                                    moduleList.Deinit();
                                    moduleList.Init(newModules);
                                    __result = true;
                                    return false;
                                }
                            }
                            // Case 2: Double-click on a module that is already equipped (attempting to unequip it)
                            else if (clickedSlot.ParentList == moduleList)
                            {
                                // Unequip by moving it back to storage.
                                var storageSlot = storageList.GetFirstAvailableSlot(clickedSlot.Equipment, false);
                                if (storageSlot != null)
                                {
                                    var attemptMethod = AccessTools.Method(typeof(UIModuleVehicleEquip), "AttemptSlotSwap");
                                    bool success = (bool)attemptMethod.Invoke(__instance, new object[] { clickedSlot, storageSlot });
                                    __result = success;
                                }
                                else
                                {
                                    __result = false;
                                }
                                return false;
                            }

                            // For any other list, use the original behavior.
                            return true;
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }
                }
            }


        }


        internal class TravellingThroughMist
        {
            private static DateTime lastCallTime = DateTime.MinValue;

            public static void ModifySpeedInMist(GeoVehicle geoVehicle)
            {
                try
                {
                    if (!AircraftReworkOn)
                    {
                        return;
                    }

                    // Get the current in-game time
                    DateTime currentTime = geoVehicle.GeoLevel.Timing.Now.DateTime;

                    // Check if 10 minutes have passed since the last call
                    if ((currentTime - lastCallTime).TotalMinutes < 10)
                    {
                        return;
                    }

                    // Update the last call time
                    lastCallTime = currentTime;

                    if (IsAircraftInMist(geoVehicle) && !CheckSpeedAdjustedForMist(geoVehicle))
                    {
                        List<GeoSite> _destinationSites = typeof(GeoVehicle).GetField("_destinationSites", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(geoVehicle) as List<GeoSite>;

                        if (geoVehicle.VehicleDef != blimp)
                        {
                            geoVehicle.Stats.Speed = new EarthUnits(geoVehicle.Speed.Value * (1 - MistSpeedMalus));
                        }
                        else
                        {
                            geoVehicle.Stats.Speed = new EarthUnits(geoVehicle.Speed.Value * MistSpeedBuff);
                        }

                        if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _blimpSpeedModule))
                        {
                            geoVehicle.Stats.Speed *= MistSpeedBuff;
                        }


                        if (geoVehicle.Travelling && _destinationSites.Any())
                        {
                            List<Vector3> path = _destinationSites.Select((GeoSite d) => d.WorldPosition).ToList();
                            geoVehicle.Navigation.Navigate(path);
                        }
                    }
                    else if (!IsAircraftInMist(geoVehicle) && CheckSpeedAdjustedForMist(geoVehicle))
                    {
                        ResetSpeed(geoVehicle);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static bool CheckSpeedAdjustedForMist(GeoVehicle geoVehicle)
            {
                try
                {
                    float baseSpeed = geoVehicle.VehicleDef.BaseStats.Speed.Value;
                    float speedWithModules = baseSpeed;

                    if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _basicSpeedModule))
                    {
                        speedWithModules += _basicSpeedModule.GeoVehicleModuleBonusValue;
                    }

                    if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _heliosSpeedModule))
                    {
                        speedWithModules += _heliosSpeedModule.GeoVehicleModuleBonusValue;
                    }

                    if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _basicPassengerModule))
                    {
                        speedWithModules += _basicPassengerModule.GeoVehicleModuleBonusValue;
                    }

                    return geoVehicle.Speed.Value != speedWithModules;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void ResetSpeed(GeoVehicle geoVehicle)
            {
                try
                {
                    List<GeoSite> _destinationSites = typeof(GeoVehicle).GetField("_destinationSites", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(geoVehicle) as List<GeoSite>;

                    float baseSpeed = geoVehicle.VehicleDef.BaseStats.Speed.Value;
                    float speedWithModules = baseSpeed;

                    if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _basicSpeedModule))
                    {
                        speedWithModules += _basicSpeedModule.GeoVehicleModuleBonusValue;
                    }

                    if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _heliosSpeedModule))
                    {
                        speedWithModules += _heliosSpeedModule.GeoVehicleModuleBonusValue;
                    }

                    if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _basicPassengerModule))
                    {
                        speedWithModules += _basicPassengerModule.GeoVehicleModuleBonusValue;
                    }

                    geoVehicle.Stats.Speed = new EarthUnits(speedWithModules);

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
}
