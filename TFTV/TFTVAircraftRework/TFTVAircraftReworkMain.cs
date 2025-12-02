using Assets.Code.PhoenixPoint.Geoscape.Entities.Sites.TheMarketplace;
using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Abilities;
using PhoenixPoint.Geoscape.Entities.Interception.Equipments;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Statuses;
using System;
using System.Collections.Generic;
using TFTV.TFTVAircraftRework;

namespace TFTV
{
    internal class TFTVAircraftReworkMain
    {
        public static bool AircraftReworkOn = false;
        internal static readonly float _mistSpeedMalus = 0.2f;
        //  internal static readonly float _mistSpeedBuff = 0.5f;
        internal static readonly float _mistSpeedModuleBuff = 150;
        internal static readonly float _maintenanceSpeedThreshold = 200;


        internal static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        internal static readonly DefRepository Repo = TFTVMain.Repo;
        internal static readonly SharedData Shared = TFTVMain.Shared;


        internal static readonly GeoVehicleDef manticore = DefCache.GetDef<GeoVehicleDef>("PP_Manticore_Def");
        internal static readonly GeoVehicleDef helios = DefCache.GetDef<GeoVehicleDef>("SYN_Helios_Def");
        internal static readonly GeoVehicleDef thunderbird = DefCache.GetDef<GeoVehicleDef>("NJ_Thunderbird_Def");
        internal static readonly GeoVehicleDef blimp = DefCache.GetDef<GeoVehicleDef>("ANU_Blimp_Def");

        internal static readonly GeoVehicleDef maskedManticore = DefCache.GetDef<GeoVehicleDef>("PP_MaskedManticore_Def");

        internal static readonly ResearchDef _aspidaResearchDef = DefCache.GetDef<ResearchDef>("SYN_Rover_ResearchDef");
        internal static readonly ResearchDef _heliosMoonMissionResearchDef = DefCache.GetDef<ResearchDef>("SYN_MoonMission_ResearchDef");
        internal static readonly ClassTagDef _aspidaClassTag = DefCache.GetDef<ClassTagDef>("Aspida_ClassTagDef");

        internal static GeoScanComponentDef _basicScannerComponent = null;
        internal static GeoScanComponentDef _thunderbirdScannerComponent = null;
        internal static ScanAbilityDef _scanAbilityDef = null;
        internal static ScanAbilityDef _thunderbirdScanAbilityDef = null;

        internal static GeoVehicleModuleDef _basicRangeModule = null; //implemented works! v2
        internal static GeoVehicleModuleDef _basicSpeedModule = null; //implemented works! v2
        internal static GeoVehicleModuleDef _basicScannerModule = null; //implemented works! v2 (ability adjustments pending)
        internal static GeoVehicleModuleDef _basicPassengerModule = null; //implemented works! v2
        internal static GeoVehicleModuleDef _basicClinicModule = null; //implemented works! v2
        internal static GeoVehicleModuleDef _vehicleHarnessModule = null; //implemented v2
        internal static GeoVehicleModuleDef _captureDronesModule = null; //implemented v2
        internal static GeoVehicleModuleDef _blimpSpeedModule = null; //implemented v2 (pending adding research benefits)
        internal static GeoVehicleModuleDef _blimpMutationLabModule = null; //implemented v2 (pending adding research benefits)
        internal static GeoVehicleModuleDef _blimpMutogPenModule = null; //implemented v2
        internal static GeoVehicleModuleDef _blimpMistModule = null; //implemented v2
                                                                    // internal static GeoVehicleModuleDef _heliosSpeedModule = null; removed in v2
        internal static GeoVehicleModuleDef _heliosMistRepellerModule = null; //implemented v2
        internal static GeoVehicleModuleDef _heliosPanaceaModule = null; //implemented v2 
        internal static GeoVehicleModuleDef _heliosStealthModule = null; //implemented v2 
        internal static GeoVehicleModuleDef _thunderbirdRangeModule = null; //implemented v2
        internal static GeoVehicleModuleDef _thunderbirdWorkshopModule = null;  //implemented v2 (pending special tactical effect selfrepair)
        internal static GeoVehicleModuleDef _thunderbirdGroundAttackModule = null; //implemented v2 (pending special damages)
        internal static GeoVehicleModuleDef _thunderbirdScannerModule = null; //implemented v2 
        internal static GeoVehicleModuleDef _scyllaCaptureModule = null; //implemented (unmodified from base TFTV)

        internal static List<ResearchDef> _blimpSpeedModuleBuffResearches = new List<ResearchDef>();
        internal static List<ResearchDef> _blimpMutationLabModuleBuffResearches = new List<ResearchDef>();
        internal static List<ResearchDef> _heliosStatisChamberBuffResearchDefs = new List<ResearchDef>();
        internal static List<ResearchDef> _thunderbirdWorkshopBuffResearchDefs = new List<ResearchDef>();
        internal static List<ResearchDef> _heliosSpeedBuffResearchDefs = new List<ResearchDef>();
        internal static List<ResearchDef> _thunderbirdRangeBuffResearchDefs = new List<ResearchDef>();
        internal static List<ResearchDef> _thunderbirdGroundAttackBuffResearchDefs = new List<ResearchDef>();
        internal static List<ResearchDef> _thunderbirdScannerBuffResearchDefs = new List<ResearchDef>();

        internal static readonly float _heliosSpeedBuffPerLevel = 100;
        internal static readonly float _thunderbirdRangeBuffPerLevel = 1000;
        internal static readonly float _thunderbirdSpeedBuffPerLevel = 50;
        internal static readonly float _thunderbirdScannerRangeBase = 2000;
        internal static readonly float _thunderbirdScannerTime = 12;
        internal static readonly float _basicScannerRangeBase = 2000;
        internal static readonly float _basicScannerTime = 12;

        //  internal static readonly float _basicClinicStaminaRecuperation = 0.35f;

        internal static readonly float _healingHPBase = 10;
        internal static readonly float _healingStaminaBase = 0.35f;
        internal static readonly float _workshopBuffBionicRepairCostReduction = 0.333f;
        internal static readonly int _thunderbirdVehicleSpaceReduction = 1;
        /* internal static readonly float _mutationLabHPRecuperationBase = 10;
         internal static readonly float _mutationLabRecuperationBase = 0.35f;
         internal static readonly float _mutationLabRecuperationBuffPerLevel = 2f;

         internal static readonly float _stasisHPRecuperationBase = 10;
         internal static readonly float _stasisRecuperationBase = 0.35f;
         internal static readonly float _stasisRecuperationBuffPerLevel = 2f;*/



        internal static List<GeoVehicleModuleDef> _thunderbirdModules = new List<GeoVehicleModuleDef>();
        internal static List<GeoVehicleModuleDef> _heliosModules = new List<GeoVehicleModuleDef>();
        internal static List<GeoVehicleModuleDef> _blimpModules = new List<GeoVehicleModuleDef>();
        internal static List<GeoVehicleModuleDef> _basicModules = new List<GeoVehicleModuleDef>();

        internal static StanceStatusDef _heliosStealthModuleStatus = null;
        internal static StanceStatusDef _argusEyeStatus = null;

        internal static List<GeoMarketplaceItemOptionDef> _listOfModulesSoldInMarketplace = new List<GeoMarketplaceItemOptionDef>();
        internal static GroundAttackWeaponAbilityDef _groundAttackAbility = null;
        internal static List<DelayedEffectDef> _groundAttackWeaponExplosions = new List<DelayedEffectDef>();

        public static ItemSlotStatsModifyStatusDef NanoVestStatusDef = null;
        // public static DamageMultiplierAbilityDef BlastVestResistance = null; //"BlastResistant_DamageMultiplierAbilityDef"
        // public static DamageMultiplierAbilityDef FireVestResistance = null; //"FireResistant_DamageMultiplierAbilityDef"
        // public static DamageMultiplierAbilityDef AcidVestResistance = null;  //AcidResistant_DamageMultiplierAbilityDef
        // public static DamageMultiplierAbilityDef ParalysysVestResistance = null;
        // public static DamageMultiplierAbilityDef PoisonVestResistance = null; //PoisonResistant_DamageMultiplierAbilityDef

        public static List<DamageMultiplierAbilityDef> VestResistanceMultiplierAbilities = new List<DamageMultiplierAbilityDef>();

        //make all basic modules available in the Marketplace

        internal class InternalData
        {

            public static void ClearDataOnLoad()
            {
                try
                {
                    if (!AircraftReworkOn)
                    {
                        return;
                    }

                    AircraftReworkTacticalModules.ClearTacticalDataOnLoad();
                    AircraftReworkGeoscape.Scanning.AircraftScanningSites = new Dictionary<int, List<int>>();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            public static void ClearDataOnStateChange()
            {
                try
                {
                    if (!AircraftReworkOn)
                    {
                        return;
                    }

                    AircraftReworkSpeed.ClearInternalData();
                    AircraftReworkGeoscape.Scanning.AircraftScanningSites = new Dictionary<int, List<int>>();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            public static int[] ModulesInTactical = new int[15];
        }
        internal class MarketPlace
        {
            /// <summary>
            /// Will be available immediately.
            /// </summary>
            /// <param name="geoMarketplace"></param>
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




            internal static void ApplyGenerateFactionReward(GeoEventChoiceOutcome __instance, GeoFaction faction)
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
}
