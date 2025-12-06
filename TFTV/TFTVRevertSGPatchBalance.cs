using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Abilities;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV
{
    internal class TFTVRevertSGPatchBalance
    {
        internal static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        internal static readonly DefRepository Repo = TFTVMain.Repo;
        internal static readonly SharedData Shared = TFTVMain.Shared;


        public static void RevertSGPatchBalanceChanges()
        {
            IncreaseAmmoCost();
            IncreaseConstructionCosts();
            DecreaseManticoresRange();
            IncreasePXResearchTimes();
            ReBuffBlastChirons();
            RestoreExcavateAbilityToGeoVehicles();
        }


        private static void RestoreExcavateAbilityToGeoVehicles()
        {
            try
            {
               // DefCache.GetDef<GeoSiteDef>("AncientSite_GeoSiteDef").ExplorationTimeHours = 8;

                List<GeoVehicleDef> geoVehicleDefs = new List<GeoVehicleDef>()

                {
                    DefCache.GetDef<GeoVehicleDef>("PP_Manticore_Def"), 
                    DefCache.GetDef<GeoVehicleDef>("SYN_Helios_Def"), 
                    DefCache.GetDef<GeoVehicleDef>("NJ_Thunderbird_Def"),
                    DefCache.GetDef<GeoVehicleDef>("ANU_Blimp_Def"),
                    DefCache.GetDef<GeoVehicleDef>("PP_MaskedManticore_Def")
                };

                ExcavateAbilityDef excavateAbilityDef = DefCache.GetDef<ExcavateAbilityDef>("ExcavateAbilityDef");

                foreach (GeoVehicleDef geoVehicleDef in geoVehicleDefs)
                {
                    geoVehicleDef.Abilities = geoVehicleDef.Abilities.AddToArray(excavateAbilityDef);
                   // TFTVLogger.Always($"[RestoreAncientSiteExcavationTime] Added ExcavateAbilityDef to {geoVehicleDef.name}", false);
                }
                
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        //increases ammo cost by 25% from the patch values to revert 20% decrease 
        private static void IncreaseAmmoCost()
        {
            try
            {
                // AN
                DefCache.GetDef<TacticalItemDef>("AN_AcidHandGun_AmmoClip_ItemDef").ManufactureMaterials = 11;
                DefCache.GetDef<TacticalItemDef>("AN_AcidHandGun_AmmoClip_ItemDef").ManufactureTech = 2;
              

                DefCache.GetDef<TacticalItemDef>("AN_HandCannon_AmmoClip_ItemDef").ManufactureMaterials = 22;
                DefCache.GetDef<TacticalItemDef>("AN_HandCannon_AmmoClip_ItemDef").ManufactureTech = 1;
              

                DefCache.GetDef<TacticalItemDef>("AN_Redemptor_AmmoClip_ItemDef").ManufactureMaterials = 8;
                DefCache.GetDef<TacticalItemDef>("AN_Redemptor_AmmoClip_ItemDef").ManufactureTech = 2;
               
                DefCache.GetDef<TacticalItemDef>("AN_Shotgun_AmmoClip_ItemDef").ManufactureMaterials = 32;
                DefCache.GetDef<TacticalItemDef>("AN_Shotgun_AmmoClip_ItemDef").ManufactureTech = 2;
               
                DefCache.GetDef<TacticalItemDef>("AN_ShreddingShotgun_AmmoClip_ItemDef").ManufactureMaterials = 33;
                DefCache.GetDef<TacticalItemDef>("AN_ShreddingShotgun_AmmoClip_ItemDef").ManufactureTech = 4;
               

                DefCache.GetDef<TacticalItemDef>("AN_Subjector_AmmoClip_ItemDef").ManufactureMaterials = 14;
                DefCache.GetDef<TacticalItemDef>("AN_Subjector_AmmoClip_ItemDef").ManufactureTech = 3;
                
                // FS
                DefCache.GetDef<TacticalItemDef>("FS_AssaultGrenadeLauncher_AmmoClip_ItemDef").ManufactureMaterials = 34;
                DefCache.GetDef<TacticalItemDef>("FS_AssaultGrenadeLauncher_AmmoClip_ItemDef").ManufactureTech = 4;

                DefCache.GetDef<TacticalItemDef>("FS_Autocannon_AmmoClip_ItemDef").ManufactureMaterials = 19;
                DefCache.GetDef<TacticalItemDef>("FS_Autocannon_AmmoClip_ItemDef").ManufactureTech = 2;


                DefCache.GetDef<TacticalItemDef>("FS_LightSniperRifle_AmmoClip_ItemDef").ManufactureMaterials = 11;
                DefCache.GetDef<TacticalItemDef>("FS_LightSniperRifle_AmmoClip_ItemDef").ManufactureTech = 1;


                DefCache.GetDef<TacticalItemDef>("FS_SlamstrikeShotgun_AmmoClip_ItemDef").ManufactureMaterials = 13;
                DefCache.GetDef<TacticalItemDef>("FS_SlamstrikeShotgun_AmmoClip_ItemDef").ManufactureTech = 1;


                // Mech arms
                DefCache.GetDef<TacticalItemDef>("MechArms_AmmoClip_ItemDef").ManufactureMaterials = 55;
                DefCache.GetDef<TacticalItemDef>("MechArms_AmmoClip_ItemDef").ManufactureTech = 15;

                // NE
                DefCache.GetDef<TacticalItemDef>("NE_AssaultRifle_AmmoClip_ItemDef").ManufactureMaterials = 15;
                DefCache.GetDef<TacticalItemDef>("NE_AssaultRifle_AmmoClip_ItemDef").ManufactureTech = 0;
                TFTVLogger.Always("[AmmoCost]NE_AssaultRifle_AmmoClip_ItemDef mat cost 15 tech cost 0", false);

                DefCache.GetDef<TacticalItemDef>("NE_MachineGun_AmmoClip_ItemDef").ManufactureMaterials = 25;
                DefCache.GetDef<TacticalItemDef>("NE_MachineGun_AmmoClip_ItemDef").ManufactureTech = 0;
                TFTVLogger.Always("[AmmoCost]NE_MachineGun_AmmoClip_ItemDef mat cost 25 tech cost 0", false);

                DefCache.GetDef<TacticalItemDef>("NE_Pistol_AmmoClip_ItemDef").ManufactureMaterials = 6;
                DefCache.GetDef<TacticalItemDef>("NE_Pistol_AmmoClip_ItemDef").ManufactureTech = 0;
                TFTVLogger.Always("[AmmoCost]NE_Pistol_AmmoClip_ItemDef mat cost 5 tech cost 0", false);

                DefCache.GetDef<TacticalItemDef>("NE_SniperRifle_AmmoClip_ItemDef").ManufactureMaterials = 10;
                DefCache.GetDef<TacticalItemDef>("NE_SniperRifle_AmmoClip_ItemDef").ManufactureTech = 0;
                TFTVLogger.Always("[AmmoCost]NE_SniperRifle_AmmoClip_ItemDef mat cost 10 tech cost 0", false);

                // NJ
                DefCache.GetDef<TacticalItemDef>("NJ_Flamethrower_AmmoClip_ItemDef").ManufactureMaterials = 67;
                DefCache.GetDef<TacticalItemDef>("NJ_Flamethrower_AmmoClip_ItemDef").ManufactureTech = 14;
                TFTVLogger.Always("[AmmoCost]NJ_Flamethrower_AmmoClip_ItemDef mat cost 68 tech cost 15", false);

                DefCache.GetDef<TacticalItemDef>("NJ_Gauss_AssaultRifle_AmmoClip_ItemDef").ManufactureMaterials = 22;
                DefCache.GetDef<TacticalItemDef>("NJ_Gauss_AssaultRifle_AmmoClip_ItemDef").ManufactureTech = 3;
                TFTVLogger.Always("[AmmoCost]NJ_Gauss_AssaultRifle_AmmoClip_ItemDef mat cost 22 tech cost 2", false);

                DefCache.GetDef<TacticalItemDef>("NJ_Gauss_HandGun_AmmoClip_ItemDef").ManufactureMaterials = 11;
                DefCache.GetDef<TacticalItemDef>("NJ_Gauss_HandGun_AmmoClip_ItemDef").ManufactureTech = 1;
                TFTVLogger.Always("[AmmoCost]NJ_Gauss_HandGun_AmmoClip_ItemDef mat cost 12 tech cost 1", false);

                DefCache.GetDef<TacticalItemDef>("NJ_Gauss_MachineGun_AmmoClip_ItemDef").ManufactureMaterials = 38;
                DefCache.GetDef<TacticalItemDef>("NJ_Gauss_MachineGun_AmmoClip_ItemDef").ManufactureTech = 5;
                TFTVLogger.Always("[AmmoCost]NJ_Gauss_MachineGun_AmmoClip_ItemDef mat cost 38 tech cost 5", false);

                DefCache.GetDef<TacticalItemDef>("NJ_Gauss_PDW_AmmoClip_ItemDef").ManufactureMaterials = 21;
                DefCache.GetDef<TacticalItemDef>("NJ_Gauss_PDW_AmmoClip_ItemDef").ManufactureTech = 3;
                TFTVLogger.Always("[AmmoCost]NJ_Gauss_PDW_AmmoClip_ItemDef mat cost 20 tech cost 2", false);

                DefCache.GetDef<TacticalItemDef>("NJ_Gauss_SniperRifle_AmmoClip_ItemDef").ManufactureMaterials = 21;
                DefCache.GetDef<TacticalItemDef>("NJ_Gauss_SniperRifle_AmmoClip_ItemDef").ManufactureTech = 3;
                TFTVLogger.Always("[AmmoCost]NJ_Gauss_SniperRifle_AmmoClip_ItemDef mat cost 21 tech cost 2", false);

                DefCache.GetDef<TacticalItemDef>("NJ_GuidedMissileLauncher_AmmoClip_ItemDef").ManufactureMaterials = 36;
                DefCache.GetDef<TacticalItemDef>("NJ_GuidedMissileLauncher_AmmoClip_ItemDef").ManufactureTech = 18;
                TFTVLogger.Always("[AmmoCost]NJ_GuidedMissileLauncher_AmmoClip_ItemDef mat cost 36 tech cost 18", false);

                DefCache.GetDef<TacticalItemDef>("NJ_HeavyRocketLauncher_AmmoClip_ItemDef").ManufactureMaterials = 100;
                DefCache.GetDef<TacticalItemDef>("NJ_HeavyRocketLauncher_AmmoClip_ItemDef").ManufactureTech = 21;
                TFTVLogger.Always("[AmmoCost]NJ_HeavyRocketLauncher_AmmoClip_ItemDef mat cost 100 tech cost 21", false);

                DefCache.GetDef<TacticalItemDef>("NJ_PRCR_AssaultRifle_AmmoClip_ItemDef").ManufactureMaterials = 16;
                DefCache.GetDef<TacticalItemDef>("NJ_PRCR_AssaultRifle_AmmoClip_ItemDef").ManufactureTech = 5;
                TFTVLogger.Always("[AmmoCost]NJ_PRCR_AssaultRifle_AmmoClip_ItemDef mat cost 15 tech cost 5", false);

                DefCache.GetDef<TacticalItemDef>("NJ_PRCR_PDW_AmmoClip_ItemDef").ManufactureMaterials = 10;
                DefCache.GetDef<TacticalItemDef>("NJ_PRCR_PDW_AmmoClip_ItemDef").ManufactureTech = 3;
                TFTVLogger.Always("[AmmoCost]NJ_PRCR_PDW_AmmoClip_ItemDef mat cost 10 tech cost 4", false);

                DefCache.GetDef<TacticalItemDef>("NJ_PRCR_SniperRifle_AmmoClip_ItemDef").ManufactureMaterials = 16;
                DefCache.GetDef<TacticalItemDef>("NJ_PRCR_SniperRifle_AmmoClip_ItemDef").ManufactureTech = 5;
                TFTVLogger.Always("[AmmoCost]NJ_PRCR_SniperRifle_AmmoClip_ItemDef mat cost 16 tech cost 5", false);

                DefCache.GetDef<TacticalItemDef>("NJ_PRCRTechTurretGun_AmmoClip_ItemDef").ManufactureMaterials = 46;
                DefCache.GetDef<TacticalItemDef>("NJ_PRCRTechTurretGun_AmmoClip_ItemDef").ManufactureTech = 15;
                TFTVLogger.Always("[AmmoCost]NJ_PRCRTechTurretGun_AmmoClip_ItemDef mat cost 46 tech cost 15", false);

                DefCache.GetDef<TacticalItemDef>("NJ_RocketLauncher_AmmoClip_ItemDef").ManufactureMaterials = 24;
                DefCache.GetDef<TacticalItemDef>("NJ_RocketLauncher_AmmoClip_ItemDef").ManufactureTech = 8;
                TFTVLogger.Always("[AmmoCost]NJ_RocketLauncher_AmmoClip_ItemDef mat cost 24 tech cost 8", false);

                DefCache.GetDef<TacticalItemDef>("NJ_TechTurretGun_AmmoClip_ItemDef").ManufactureMaterials = 44;
                DefCache.GetDef<TacticalItemDef>("NJ_TechTurretGun_AmmoClip_ItemDef").ManufactureTech = 15;
                TFTVLogger.Always("[AmmoCost]NJ_TechTurretGun_AmmoClip_ItemDef mat cost 44 tech cost 15", false);

                // PX
                DefCache.GetDef<TacticalItemDef>("PX_AcidCannon_AmmoClip_ItemDef").ManufactureMaterials = 43;
                DefCache.GetDef<TacticalItemDef>("PX_AcidCannon_AmmoClip_ItemDef").ManufactureTech = 14;
                TFTVLogger.Always("[AmmoCost]PX_AcidCannon_AmmoClip_ItemDef mat cost 42 tech cost 14", false);

                DefCache.GetDef<TacticalItemDef>("PX_AssaultRifle_AmmoClip_ItemDef").ManufactureMaterials = 21;
                DefCache.GetDef<TacticalItemDef>("PX_AssaultRifle_AmmoClip_ItemDef").ManufactureTech = 1;
                TFTVLogger.Always("[AmmoCost]PX_AssaultRifle_AmmoClip_ItemDef mat cost 20 tech cost 1", false);

                DefCache.GetDef<TacticalItemDef>("PX_GrenadeLauncher_AmmoClip_ItemDef").ManufactureMaterials = 110;
                DefCache.GetDef<TacticalItemDef>("PX_GrenadeLauncher_AmmoClip_ItemDef").ManufactureTech = 14;
                TFTVLogger.Always("[AmmoCost]PX_GrenadeLauncher_AmmoClip_ItemDef mat cost 112 tech cost 14", false);

                DefCache.GetDef<TacticalItemDef>("PX_HeavyCannon_AmmoClip_ItemDef").ManufactureMaterials = 29;
                DefCache.GetDef<TacticalItemDef>("PX_HeavyCannon_AmmoClip_ItemDef").ManufactureTech = 4;
                TFTVLogger.Always("[AmmoCost]PX_HeavyCannon_AmmoClip_ItemDef mat cost 28 tech cost 4", false);

                DefCache.GetDef<TacticalItemDef>("PX_LaserArray_AmmoClip_ItemDef").ManufactureMaterials = 17;
                DefCache.GetDef<TacticalItemDef>("PX_LaserArray_AmmoClip_ItemDef").ManufactureTech = 9;
                TFTVLogger.Always("[AmmoCost]PX_LaserArray_AmmoClip_ItemDef mat cost 18 tech cost 9", false);

                DefCache.GetDef<TacticalItemDef>("PX_LaserPDW_AmmoClip_ItemDef").ManufactureMaterials = 19;
                DefCache.GetDef<TacticalItemDef>("PX_LaserPDW_AmmoClip_ItemDef").ManufactureTech = 10;
                TFTVLogger.Always("[AmmoCost]PX_LaserPDW_AmmoClip_ItemDef mat cost 20 tech cost 10", false);

                DefCache.GetDef<TacticalItemDef>("PX_LaserTechTurretGun_AmmoClip_ItemDef").ManufactureMaterials = 90;
                DefCache.GetDef<TacticalItemDef>("PX_LaserTechTurretGun_AmmoClip_ItemDef").ManufactureTech = 48;
                TFTVLogger.Always("[AmmoCost]PX_LaserTechTurretGun_AmmoClip_ItemDef mat cost 94 tech cost 49", false);

                DefCache.GetDef<TacticalItemDef>("PX_Pistol_AmmoClip_ItemDef").ManufactureMaterials = 7;
                DefCache.GetDef<TacticalItemDef>("PX_Pistol_AmmoClip_ItemDef").ManufactureTech = 0;
                TFTVLogger.Always("[AmmoCost]PX_Pistol_AmmoClip_ItemDef mat cost 6 tech cost 0", false);

                DefCache.GetDef<TacticalItemDef>("PX_ShotgunRifle_AmmoClip_ItemDef").ManufactureMaterials = 31;
                DefCache.GetDef<TacticalItemDef>("PX_ShotgunRifle_AmmoClip_ItemDef").ManufactureTech = 2;
                TFTVLogger.Always("[AmmoCost]PX_ShotgunRifle_AmmoClip_ItemDef mat cost 30 tech cost 1", false);

                DefCache.GetDef<TacticalItemDef>("PX_ShredingMissileLauncher_AmmoClip_ItemDef").ManufactureMaterials = 62;
                DefCache.GetDef<TacticalItemDef>("PX_ShredingMissileLauncher_AmmoClip_ItemDef").ManufactureTech = 24;
                TFTVLogger.Always("[AmmoCost]PX_ShredingMissileLauncher_AmmoClip_ItemDef mat cost 49 tech cost 16", false);

                DefCache.GetDef<TacticalItemDef>("PX_SniperRifle_AmmoClip_ItemDef").ManufactureMaterials = 16;
                DefCache.GetDef<TacticalItemDef>("PX_SniperRifle_AmmoClip_ItemDef").ManufactureTech = 1;
                TFTVLogger.Always("[AmmoCost]PX_SniperRifle_AmmoClip_ItemDef mat cost 16 tech cost 1", false);

                DefCache.GetDef<TacticalItemDef>("PX_VirophageSniperRifle_AmmoClip_ItemDef").ManufactureMaterials = 19;
                DefCache.GetDef<TacticalItemDef>("PX_VirophageSniperRifle_AmmoClip_ItemDef").ManufactureTech = 10;
                TFTVLogger.Always("[AmmoCost]PX_VirophageSniperRifle_AmmoClip_ItemDef mat cost 19 tech cost 10", false);

                // SY
                DefCache.GetDef<TacticalItemDef>("SY_Crossbow_AmmoClip_ItemDef").ManufactureMaterials = 7;
                DefCache.GetDef<TacticalItemDef>("SY_Crossbow_AmmoClip_ItemDef").ManufactureTech = 0;
                TFTVLogger.Always("[AmmoCost]SY_Crossbow_AmmoClip_ItemDef mat cost 8 tech cost 0", false);

                DefCache.GetDef<TacticalItemDef>("SY_LaserAssaultRifle_AmmoClip_ItemDef").ManufactureMaterials = 19;
                DefCache.GetDef<TacticalItemDef>("SY_LaserAssaultRifle_AmmoClip_ItemDef").ManufactureTech = 10;
                TFTVLogger.Always("[AmmoCost]SY_LaserAssaultRifle_AmmoClip_ItemDef mat cost 20 tech cost 10", false);

                DefCache.GetDef<TacticalItemDef>("SY_LaserPistol_AmmoClip_ItemDef").ManufactureMaterials = 11;
                DefCache.GetDef<TacticalItemDef>("SY_LaserPistol_AmmoClip_ItemDef").ManufactureTech = 6;
                TFTVLogger.Always("[AmmoCost]SY_LaserPistol_AmmoClip_ItemDef mat cost 11 tech cost 6", false);

                DefCache.GetDef<TacticalItemDef>("SY_LaserSniperRifle_AmmoClip_ItemDef").ManufactureMaterials = 18;
                DefCache.GetDef<TacticalItemDef>("SY_LaserSniperRifle_AmmoClip_ItemDef").ManufactureTech = 9;
                TFTVLogger.Always("[AmmoCost]SY_LaserSniper_AmmoClip_ItemDef mat cost 19 tech cost 9", false);

                DefCache.GetDef<TacticalItemDef>("SY_NeuralPistol_AmmoClip_ItemDef").ManufactureMaterials = 5;
                DefCache.GetDef<TacticalItemDef>("SY_NeuralPistol_AmmoClip_ItemDef").ManufactureTech = 2;
                TFTVLogger.Always("[AmmoCost]SY_NeuralPistol_AmmoClip_ItemDef mat cost 5 tech cost 2", false);

                DefCache.GetDef<TacticalItemDef>("SY_NeuralSniperRifle_AmmoClip_ItemDef").ManufactureMaterials = 19;
                DefCache.GetDef<TacticalItemDef>("SY_NeuralSniperRifle_AmmoClip_ItemDef").ManufactureTech = 9;
                TFTVLogger.Always("[AmmoCost]SY_NeuralSniperRifle_AmmoClip_ItemDef mat cost 19 tech cost 10", false);

                DefCache.GetDef<TacticalItemDef>("SY_SpiderDroneLauncher_AmmoClip_ItemDef").ManufactureMaterials = 20;
                DefCache.GetDef<TacticalItemDef>("SY_SpiderDroneLauncher_AmmoClip_ItemDef").ManufactureTech = 10;
                TFTVLogger.Always("[AmmoCost]SY_SpiderDroneLauncher_AmmoClip_ItemDef mat cost 20 tech cost 10", false);

                DefCache.GetDef<TacticalItemDef>("SY_Venombolt_AmmoClip_ItemDef").ManufactureMaterials = 9;
                DefCache.GetDef<TacticalItemDef>("SY_Venombolt_AmmoClip_ItemDef").ManufactureTech = 5;
                TFTVLogger.Always("[AmmoCost]SY_Venombolt_AmmoClip_ItemDef mat cost 9 tech cost 5", false);

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        //Patch increased range of Manticore and Masked Manticore from 2500 to 2600
        private static void DecreaseManticoresRange()
        {
            try
            {
                GeoVehicleDef manticore = DefCache.GetDef<GeoVehicleDef>("PP_Manticore_Def");
                GeoVehicleDef maskedManticore = DefCache.GetDef<GeoVehicleDef>("PP_MaskedManticore_Def");

                manticore.BaseStats.MaximumRange.Value = 2500;
                maskedManticore.BaseStats.MaximumRange.Value = 2500;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        //increases construction costs by 25% from the patch values to revert 20% decrease; also fixes Access Lift cost and time
        private static void IncreaseConstructionCosts()
        {
            try
            {
                float increaseCostFactor = 1.25f;

                // Specific override for Access Lift
                PhoenixFacilityDef accessLiftFacility = DefCache.GetDef<PhoenixFacilityDef>("AccessLift_PhoenixFacilityDef");
                accessLiftFacility.ResourceCost = new ResourcePack()
                {
                    new ResourceUnit(ResourceType.Materials, 200),
                    new ResourceUnit(ResourceType.Tech, 10)
                };
                accessLiftFacility.ConstructionTimeDays = 2;

                // Apply same increase to all listed facility defs
                string[] facilityDefNames =
                {
                    "AlienContainment_PhoenixFacilityDef",
                    "ArcheologyLab_PhoenixFacilityDef",
                    "BionicsLab_PhoenixFacilityDef",
                    "EnergyGenerator_PhoenixFacilityDef",
                    "Entrance_PhoenixFacilityDef",
                    "FabricationPlant_PhoenixFacilityDef",
                    "FoodProduction_PhoenixFacilityDef",
                    "LivingQuarters_PhoenixFacilityDef",
                    "MedicalBay_PhoenixFacilityDef",
                    "MistRepeller_PhoenixFacilityDef",
                    "MutationLab_PhoenixFacilityDef",
                    "ResearchLab_PhoenixFacilityDef",
                    "SatelliteUplink_PhoenixFacilityDef",
                    "SecurityStation_PhoenixFacilityDef",
                    "Stores_PhoenixFacilityDef",
                    "TrainingFacility_PhoenixFacilityDef",
                    "VehicleBay_PhoenixFacilityDef"
                };

                foreach (string defName in facilityDefNames)
                {
                    PhoenixFacilityDef facility = DefCache.GetDef<PhoenixFacilityDef>(defName);
                    for (int i = 0; i < facility.ResourceCost.Count; i++)
                    {
                        ResourceUnit unit = facility.ResourceCost[i];
                        unit.Value *= increaseCostFactor;
                        facility.ResourceCost[i] = unit;
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void IncreasePXResearchTimes()
        {
            try
            {
                float increaseTimeFactor = 1.25f;
                
                ResearchDbDef researchDbDef = DefCache.GetDef<ResearchDbDef>("pp_ResearchDB");

                foreach (ResearchDef researchDef in researchDbDef.Researches)
                {
                    float newDuration = researchDef.ResearchCost * increaseTimeFactor;
                    researchDef.ResearchCost = (int)newDuration;                    
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void ReBuffBlastChirons()
        {
            try 
            {
                DefCache.GetDef<WeaponDef>("Chiron_Abdomen_Mortar_WeaponDef").DamagePayload.Range = 40;

                WeaponDef crystalChiron = DefCache.GetDef<WeaponDef>("Chiron_Abdomen_Crystal_Mortar_WeaponDef");
                crystalChiron.DamagePayload.Range = 40;
                crystalChiron.HitPoints = 800;
                crystalChiron.BodyPartAspectDef.Endurance = 10;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

    }
}
