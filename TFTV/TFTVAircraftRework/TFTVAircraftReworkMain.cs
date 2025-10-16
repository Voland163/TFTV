using Assets.Code.PhoenixPoint.Geoscape.Entities.Sites.TheMarketplace;
using Base;
using Base.Cameras;
using Base.Core;
using Base.Defs;
using Base.Entities.Effects;
using Base.Entities.Statuses;
using Base.Levels;
using Base.UI;
using Base.UI.MessageBox;
using Base.Utils.Maths;
using HarmonyLib;
using I2.Loc;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Addons;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.Levels;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Common.Utils;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Abilities;
using PhoenixPoint.Geoscape.Entities.Interception;
using PhoenixPoint.Geoscape.Entities.Interception.Equipments;
using PhoenixPoint.Geoscape.Entities.PhoenixBases.FacilityComponents;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.DataObjects;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewControllers.AircraftEquipment;
using PhoenixPoint.Geoscape.View.ViewControllers.Inventory;
using PhoenixPoint.Geoscape.View.ViewControllers.PhoenixBase;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using PhoenixPoint.Geoscape.View.ViewControllers.VehicleEquipmentInventory;
using PhoenixPoint.Geoscape.View.ViewControllers.VehicleRoster;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Eventus;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using PhoenixPoint.Tactical.Levels.Mist;
using PhoenixPoint.Tactical.Levels.PathProcessors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using TFTV.TFTVAircraftRework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using Research = PhoenixPoint.Geoscape.Entities.Research.Research;

namespace TFTV
{
    class TFTVAircraftReworkMain
    {
        public static bool AircraftReworkOn = true;
        private static readonly float _mistSpeedMalus = 0.2f;
        //  private static readonly float _mistSpeedBuff = 0.5f;
        private static readonly float _mistSpeedModuleBuff = 150;
        private static readonly float _maintenanceSpeedThreshold = 200;


        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;


        private static readonly GeoVehicleDef manticore = DefCache.GetDef<GeoVehicleDef>("PP_Manticore_Def");
        private static readonly GeoVehicleDef helios = DefCache.GetDef<GeoVehicleDef>("SYN_Helios_Def");
        private static readonly GeoVehicleDef thunderbird = DefCache.GetDef<GeoVehicleDef>("NJ_Thunderbird_Def");
        private static readonly GeoVehicleDef blimp = DefCache.GetDef<GeoVehicleDef>("ANU_Blimp_Def");

        private static readonly GeoVehicleDef maskedManticore = DefCache.GetDef<GeoVehicleDef>("PP_MaskedManticore_Def");

        private static GeoScanComponentDef _basicScannerComponent = null;
        private static GeoScanComponentDef _thunderbirdScannerComponent = null;
        private static ScanAbilityDef _scanAbilityDef = null;
        private static ScanAbilityDef _thunderbirdScanAbilityDef = null;

        private static GeoVehicleModuleDef _basicRangeModule = null; //implemented works! v2
        private static GeoVehicleModuleDef _basicSpeedModule = null; //implemented works! v2
        private static GeoVehicleModuleDef _basicScannerModule = null; //implemented works! v2 (ability adjustments pending)
        private static GeoVehicleModuleDef _basicPassengerModule = null; //implemented works! v2
        private static GeoVehicleModuleDef _basicClinicModule = null; //implemented works! v2
        private static GeoVehicleModuleDef _vehicleHarnessModule = null; //implemented v2
        private static GeoVehicleModuleDef _captureDronesModule = null; //implemented v2
        private static GeoVehicleModuleDef _blimpSpeedModule = null; //implemented v2 (pending adding research benefits)
        private static GeoVehicleModuleDef _blimpMutationLabModule = null; //implemented v2 (pending adding research benefits)
        private static GeoVehicleModuleDef _blimpMutogPenModule = null; //implemented v2
        private static GeoVehicleModuleDef _blimpMistModule = null; //implemented v2
                                                                    // private static GeoVehicleModuleDef _heliosSpeedModule = null; removed in v2
        private static GeoVehicleModuleDef _heliosMistRepellerModule = null; //implemented v2
        private static GeoVehicleModuleDef _heliosPanaceaModule = null; //implemented v2 
        private static GeoVehicleModuleDef _heliosStealthModule = null; //implemented v2 
        private static GeoVehicleModuleDef _thunderbirdRangeModule = null; //implemented v2
        private static GeoVehicleModuleDef _thunderbirdWorkshopModule = null;  //implemented v2 (pending special tactical effect selfrepair)
        private static GeoVehicleModuleDef _thunderbirdGroundAttackModule = null; //implemented v2 (pending special damages)
        private static GeoVehicleModuleDef _thunderbirdScannerModule = null; //implemented v2 
        private static GeoVehicleModuleDef _scyllaCaptureModule = null; //implemented (unmodified from base TFTV)

        private static List<ResearchDef> _blimpSpeedModuleBuffResearches = new List<ResearchDef>();
        private static List<ResearchDef> _blimpMutationLabModuleBuffResearches = new List<ResearchDef>();
        private static List<ResearchDef> _heliosStatisChamberBuffResearchDefs = new List<ResearchDef>();
        private static List<ResearchDef> _thunderbirdWorkshopBuffResearchDefs = new List<ResearchDef>();
        private static List<ResearchDef> _heliosSpeedBuffResearchDefs = new List<ResearchDef>();
        private static List<ResearchDef> _thunderbirdRangeBuffResearchDefs = new List<ResearchDef>();
        private static List<ResearchDef> _thunderbirdGroundAttackBuffResearchDefs = new List<ResearchDef>();
        private static List<ResearchDef> _thunderbirdScannerBuffResearchDefs = new List<ResearchDef>();

        private static readonly float _heliosSpeedBuffPerLevel = 100;
        private static readonly float _thunderbirdRangeBuffPerLevel = 1000;
        private static readonly float _thunderbirdSpeedBuffPerLevel = 50;
        private static readonly float _thunderbirdScannerRangeBase = 2000;
        private static readonly float _thunderbirdScannerTime = 12;
        private static readonly float _basicScannerRangeBase = 2000;
        private static readonly float _basicScannerTime = 12;

        //  private static readonly float _basicClinicStaminaRecuperation = 0.35f;

        private static readonly float _healingHPBase = 10;
        private static readonly float _healingStaminaBase = 0.35f;
        private static readonly float _workshopBuffBionicRepairCostReduction = 0.333f;
        /* private static readonly float _mutationLabHPRecuperationBase = 10;
         private static readonly float _mutationLabRecuperationBase = 0.35f;
         private static readonly float _mutationLabRecuperationBuffPerLevel = 2f;

         private static readonly float _stasisHPRecuperationBase = 10;
         private static readonly float _stasisRecuperationBase = 0.35f;
         private static readonly float _stasisRecuperationBuffPerLevel = 2f;*/



        private static List<GeoVehicleModuleDef> _thunderbirdModules = new List<GeoVehicleModuleDef>();
        private static List<GeoVehicleModuleDef> _heliosModules = new List<GeoVehicleModuleDef>();
        private static List<GeoVehicleModuleDef> _blimpModules = new List<GeoVehicleModuleDef>();
        private static List<GeoVehicleModuleDef> _basicModules = new List<GeoVehicleModuleDef>();

        private static StanceStatusDef _heliosStealthModuleStatus = null;
        private static StanceStatusDef _argusEyeStatus = null;

        private static List<GeoMarketplaceItemOptionDef> _listOfModulesSoldInMarketplace = new List<GeoMarketplaceItemOptionDef>();
        private static GroundAttackWeaponAbilityDef _groundAttackAbility = null;
        private static List<DelayedEffectDef> _groundAttackWeaponExplosions = new List<DelayedEffectDef>();

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

                    Modules.Tactical.ClearTacticalDataOnLoad();
                    Modules.Geoscape.Scanning.AircraftScanningSites = new Dictionary<int, List<int>>();
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

                    AircraftSpeed.ClearInternalData();
                    Modules.Geoscape.Scanning.AircraftScanningSites = new Dictionary<int, List<int>>();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            public static int[] ModulesInTactical = new int[15];
        }

        internal class Defs
        {
            public static void CreateAndModifyDefs()
            {
                try
                {
                    CreateCaptureModule();
                    RemoveInfestedAircraft();

                    if (!AircraftReworkOn)
                    {
                        AircombatOption.AircraftAndAircombat();
                        return;
                    }

                    ModifyBaseStats();
                    ModifyLocKeys();
                    CreateModules();
                    CreateHeliosSpeedBuffs();
                    RemoveAircombat();
                    CreateHeliosStealthModuleStatus();
                    CreateArgusEyesStatus();
                    CreateGroundAttackWeaponExplosion();
                    CreateGroundAttackAbility(); 
                    MakeMyrmidonsAvailableWithoutFlyers();
                    ModifyVehicleBayHealing();
                    AddManufactureTagToResourceCrates();
                    //  AdjustLocKeysFesteringSkies();
                    /*  foreach(CustomMissionTypeDef customMissionTypeDef in Repo.GetAllDefs<CustomMissionTypeDef>()) 
                      {
                          TFTVLogger.Always($"Mission: {customMissionTypeDef.name}", false);
                          {
                              foreach (var tag in customMissionTypeDef.MissionTags)
                              {
                                  TFTVLogger.Always($"{tag.name}", false);
                              }
                          }

                      }*/

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void AdjustLocKeysFesteringSkies()
            {
                try
                {
                    FesteringSkiesSettingsDef festeringSkiesSettingsDef = DefCache.GetDef<FesteringSkiesSettingsDef>("FesteringSkiesSettingsDef");
                    festeringSkiesSettingsDef.UISettings.HitPoints.LocalizationKey = "DLC 3 - Behemoth/KEY_DLC3_HULL_POINTS";

                    // festeringSkiesSettingsDef.UISettings.

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }


            }

            private static void AddManufactureTagToResourceCrates()
            {
                try
                {
                    TacticalItemDef foodPack = DefCache.GetDef<TacticalItemDef>("FoodPack_ItemDef");
                    TacticalItemDef techPack = DefCache.GetDef<TacticalItemDef>("TechPack_ItemDef");
                    TacticalItemDef matPack = DefCache.GetDef<TacticalItemDef>("MaterialsPack_ItemDef");
                    TacticalItemDef mutagenPack = DefCache.GetDef<TacticalItemDef>("MutagenPack_ItemDef");

                    GameTagDef manufactureTag = Shared.SharedGameTags.ManufacturableTag;

                    foodPack.Tags.Add(manufactureTag);
                    techPack.Tags.Add(manufactureTag);
                    matPack.Tags.Add(manufactureTag);
                    mutagenPack.Tags.Add(manufactureTag);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            private static void RemoveInfestedAircraft()
            {
                try
                {
                    //Changes to FesteringSkies settings
                    FesteringSkiesSettingsDef festeringSkiesSettingsDef = DefCache.GetDef<FesteringSkiesSettingsDef>("FesteringSkiesSettingsDef");
                    festeringSkiesSettingsDef.SpawnInfestedAircraftChance = 0;
                    festeringSkiesSettingsDef.InfestedAircraftChance = 0;
                    festeringSkiesSettingsDef.InfestedAircrafts.Clear();
                    festeringSkiesSettingsDef.InfestedAircraftRebuildHours = 100000;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }



            }
            private static void ModifyVehicleBayHealing()
            {
                try
                {
                    DefCache.GetDef<VehicleSlotFacilityComponentDef>("E_Element0 [VehicleBay_PhoenixFacilityDef]").AircraftHealAmount = 100;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            private static void MakeMyrmidonsAvailableWithoutFlyers()
            {
                try
                {
                    DefCache.GetDef<ExistingResearchRequirementDef>("ALN_BasicSwarmer_ResearchDef_ExistingResearchRequirementDef_0").ResearchID = "ALN_Lair_ResearchDef";
                    DefCache.GetDef<ExistingResearchRequirementDef>("ALN_SwarmerEgg_ResearchDef_ExistingResearchRequirementDef_0").ResearchID = "ALN_Lair_ResearchDef";
                    DefCache.GetDef<ExistingResearchRequirementDef>("ALN_CorruptionNode_ResearchDef_ExistingResearchRequirementDef_0").ResearchID = "ALN_Lair_ResearchDef";
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
            private static void CreateHeliosSpeedBuffs()
            {
                try
                {
                    _heliosSpeedBuffResearchDefs = new List<ResearchDef>()
                    {
                        DefCache.GetDef<ResearchDef>("SYN_Aircraft_ResearchDef"),
                        DefCache.GetDef<ResearchDef>("SYN_FusionCellTech_ResearchDef"),
                        DefCache.GetDef<ResearchDef>("SYN_SentientAITech_ResearchDef"),
                        DefCache.GetDef<ResearchDef>("SYN_MoonMission_ResearchDef")
                    };

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            private static void CreateGroundAttackWeaponExplosion()
            {
                try
                {

                    string name = "GroundAttackWeaponExplosion_ExplosionEffectDef";
                    string gUIDDelayedEffect0 = "{0A457309-A00E-448C-846A-63E598431240}";
                    string gUIDDelayedEffect1 = "{FF7B0D09-6BA2-4E96-8423-A4690704184E}";
                    string gUIDDelayedEffect2 = "{271D4DFA-7396-430D-890D-DCF2A6A4CE8B}";
                    DelayedEffectDef sourceDelayedEffect = DefCache.GetDef<DelayedEffectDef>("ExplodingBarrel_ExplosionEffectDef");
                    DelayedEffectDef newDelayedEffect0 = Helper.CreateDefFromClone(sourceDelayedEffect, gUIDDelayedEffect0, name);


                    string gUIDExplosionEffect = "{53B8B5BE-8256-490B-9928-2447EF24F18D}";
                    ExplosionEffectDef sourceExplosionEffect = DefCache.GetDef<ExplosionEffectDef>("E_ShrapnelExplosion [ExplodingBarrel_ExplosionEffectDef]");
                    ExplosionEffectDef newExplosionEffect = Helper.CreateDefFromClone(sourceExplosionEffect, gUIDExplosionEffect, name);


                    //  SpawnVoxelDamageTypeEffectDef mistDamage = DefCache.GetDef<SpawnVoxelDamageTypeEffectDef>("Goo_SpawnVoxelDamageTypeEffectDef");

                    string gUIDDamageEffect = "{1EDC7AEA-FC22-4860-AAF9-298784658B1E}";
                    DamageEffectDef sourceDamageEffect = DefCache.GetDef<DamageEffectDef>("E_DamageEffect [ExplodingBarrel_ExplosionEffectDef]");
                    DamageEffectDef newDamageEffect = Helper.CreateDefFromClone(sourceDamageEffect, gUIDDamageEffect, name);
                    newDamageEffect.MinimumDamage = 70;
                    newDamageEffect.MaximumDamage = 70;
                    newDamageEffect.ObjectMultiplier = 10;
                    newDamageEffect.ArmourShred = 10;
                    newDamageEffect.ArmourShredProbabilityPerc = 100;
                    //  newDamageEffect.DamageTypeDef = mistDamage;
                    newExplosionEffect.DamageEffect = newDamageEffect;
                    newDelayedEffect0.EffectDef = newExplosionEffect;
                    newDelayedEffect0.SecondsDelay = 0.0f;

                    DelayedEffectDef newDelayedEffect1 = Helper.CreateDefFromClone(newDelayedEffect0, gUIDDelayedEffect1, name);
                    DelayedEffectDef newDelayedEffect2 = Helper.CreateDefFromClone(newDelayedEffect0, gUIDDelayedEffect2, name);

                    newDelayedEffect1.SecondsDelay = 0.0f;
                    newDelayedEffect2.SecondsDelay = 0.0f;

                    _groundAttackWeaponExplosions.Add(newDelayedEffect0);
                    _groundAttackWeaponExplosions.Add(newDelayedEffect1);
                    _groundAttackWeaponExplosions.Add(newDelayedEffect2);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            private static void CreateGroundAttackAbility()
            {
                try
                {
                    JetJumpAbilityDef jetJumpAbilityDef = DefCache.GetDef<JetJumpAbilityDef>("JetJump_AbilityDef");
                    ShootAbilitySceneViewDef shootAbilitySceneViewDef = DefCache.GetDef<ShootAbilitySceneViewDef>("_Sphere_ShootAbilitySceneViewElementDef");

                    string name = "TFTV_GroundAttackAbility";
                    string guid1 = "{2EAE36F5-CE9B-466E-9FDE-1DC868110A85}";
                    string guid2 = "{BD352977-59CB-41B1-A839-15CD817B84D3}";
                    string guid3 = "{8330134B-A2D2-4EF7-BAD9-C601B070463C}";
                    string guid4 = "{067F3041-EF22-41C8-A1DB-09E4EE6E5A3B}";

                    GroundAttackWeaponAbilityDef newAbility = Helper.CreateDefFromClone<GroundAttackWeaponAbilityDef>(null, guid1, name);
                    PRMBetterClasses.Helper.CopyFieldsByReflection(jetJumpAbilityDef, newAbility);
                    newAbility.name = name;
                    newAbility.AnimType = -1;
                    newAbility.ViewElementDef = Helper.CreateDefFromClone(jetJumpAbilityDef.ViewElementDef, guid2, name);
                    newAbility.TargetingDataDef = Helper.CreateDefFromClone(jetJumpAbilityDef.TargetingDataDef, guid3, name);
                    newAbility.ProjectileDef = DefCache.GetDef<ProjectileDef>("E_ProjectileVisuals [PX_Scarab_Missile_Turret_GroundVehicleWeaponDef]");
                    newAbility.TrackWithCamera = false;
                   
                    TacticalTargetingDataDef tacticalTargetingDataDef = newAbility.TargetingDataDef;

                    tacticalTargetingDataDef.Target.TargetResult = TargetResult.Position;
                    tacticalTargetingDataDef.Target.Range = 100f;
                    tacticalTargetingDataDef.Target.MinRange = 0f;
                    tacticalTargetingDataDef.Target.LineOfSight = LineOfSightType.Ignore;
                    tacticalTargetingDataDef.Target.FactionVisibility = LineOfSightType.Ignore;
                    tacticalTargetingDataDef.Target.FloorPositions = FloorPositionType.AllFloors;
                    tacticalTargetingDataDef.Target.TargetTags.Clear();
                    tacticalTargetingDataDef.Origin.TargetResult = TargetResult.Position;
                    tacticalTargetingDataDef.Origin.Range = 100;

                   // newAbility.EventOnActivate = DefCache.GetDef<TacticalEventDef>("LaunchDamageVoice_EventDef");

                    newAbility.SceneViewElementDef = Helper.CreateDefFromClone(jetJumpAbilityDef.SceneViewElementDef, guid4, name);

                    newAbility.ViewElementDef.DisplayName1.LocalizationKey = "TFTV_THUNDERBIRD_GAW_ABILITY_NAME";
                    newAbility.ViewElementDef.Description.LocalizationKey = "TFTV_THUNDERBIRD_GAW_ABILITY_DESCRIPTION";

                    Sprite iconLevel1 = Helper.CreateSpriteFromImageFile("TFTV_Thunderbird_GroundAttack_Ability1.png");
                    Sprite iconLevel2 = Helper.CreateSpriteFromImageFile("TFTV_Thunderbird_GroundAttack_Ability2.png");
                    Sprite iconLevel3 = Helper.CreateSpriteFromImageFile("TFTV_Thunderbird_GroundAttack_Ability3.png");

                    newAbility.LevelIcons = new Sprite[]
                   {
                        iconLevel1,
                        iconLevel2,
                        iconLevel3
                   };

                    newAbility.ViewElementDef.SmallIcon = iconLevel1;
                    newAbility.ViewElementDef.LargeIcon = iconLevel1;


                    newAbility.SceneViewElementDef.HoverMarker = PhoenixPoint.Tactical.View.GroundMarkerType.AttackGround;
                    newAbility.SceneViewElementDef.TargetPositionMarker  = PhoenixPoint.Tactical.View.GroundMarkerType.Invalid; 
                    newAbility.SceneViewElementDef.DrawCoverAtHoverMarker = false;



                    //  newAbility.SceneViewElementDef.MovementPositionMarker = PhoenixPoint.Tactical.View.GroundMarkerType.AttackGround;
                    //  newAbility.SceneViewElementDef.TargetPositionMarker = PhoenixPoint.Tactical.View.GroundMarkerType.Invalid;

                    newAbility.ActionPointCost = 0;
                    newAbility.WillPointCost = 0;

                    newAbility.ExplosionDefs = new List<DelayedEffectDef>(_groundAttackWeaponExplosions);
                    newAbility.ImpactOffsets = new List<Vector3>
                    {
                        Vector3.zero,
                        new Vector3(1.5f, 0f, 0f),
                        new Vector3(-1.5f, 0f, 0f),
                        new Vector3(0f, 0f, 1.5f),
                        new Vector3(0f, 0f, -1.5f)
                    };
                    newAbility.PatternRadius = 4f;
                    newAbility.PreImpactDelaySeconds = 0.25f;
                    newAbility.DelayBetweenStrikesSeconds = 0.5f;


                    _groundAttackAbility = newAbility;
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

                    newStatus.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnBodyPartStatusList;
                    newStatus.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.Hidden;
                    newStatus.VisibleOnPassiveBar = false;


                    newStatus.DurationTurns = 2;
                    newStatus.StatModifications = new ItemStatModification[] {new ItemStatModification()
                    {
                    Modification = StatModificationType.Add,
                    TargetStat = StatModificationTarget.Stealth,
                    Value = 0.1f
                    },
                        new ItemStatModification() {
                    Modification = StatModificationType.Add,
                    TargetStat = StatModificationTarget.Perception,
                    Value = 5f
                    },

                    };
                    newStatus.EffectName = "HeliosStealthModuleBuff";
                    _heliosStealthModuleStatus = newStatus;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }



            private static void CreateArgusEyesStatus()
            {
                try
                {
                    string name = "ArgusEyeStatus";
                    StanceStatusDef source = DefCache.GetDef<StanceStatusDef>("Stealth_StatusDef");
                    StanceStatusDef newStatus = Helper.CreateDefFromClone(source, "{43E45769-AA2B-40EC-BD1B-968C28650021}", name);

                    newStatus.Visuals = Helper.CreateDefFromClone(source.Visuals, "{F6EDB64C-7A17-4462-9570-0AB16C4D93B3}", name);
                    newStatus.Visuals.DisplayName1.LocalizationKey = "shouldnotappear";
                    newStatus.Visuals.Description.LocalizationKey = "shouldnotappear";

                    newStatus.DurationTurns = 1;
                    newStatus.StatModifications = new ItemStatModification[] {new ItemStatModification()
                    {
                    Modification = StatModificationType.Add,
                    TargetStat = StatModificationTarget.Perception,
                    Value = 10
                    },
                        new ItemStatModification() {
                    Modification = StatModificationType.Add,
                    TargetStat = StatModificationTarget.Accuracy,
                    Value = 25
                    },

                    };
                    newStatus.EffectName = "ArgusEyeStatus";

                    _argusEyeStatus = newStatus;


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
                    captureModule.ViewElementDef.SmallIcon = Helper.CreateSpriteFromImageFile("TFTVScyllaCaptureSmallIcon.png");
                    captureModule.ViewElementDef.LargeIcon = captureModule.ViewElementDef.SmallIcon;
                    captureModule.ViewElementDef.RosterIcon = captureModule.ViewElementDef.SmallIcon;
                    captureModule.ViewElementDef.InventoryIcon = captureModule.ViewElementDef.SmallIcon;
                    captureModule.ViewElementDef.DeselectIcon = captureModule.ViewElementDef.SmallIcon;


                    captureModule.ManufactureMaterials = 600;
                    captureModule.ManufactureTech = 75;
                    captureModule.ManufacturePointsCost = 505;

                    //Needs to be removed because it's a config option
                    ResearchDbDef ppResearchDB = DefCache.GetDef<ResearchDbDef>("pp_ResearchDB");
                    ppResearchDB.Researches.Remove(scyllaCaptureModule);

                    _scyllaCaptureModule = captureModule;

                    if (AircraftReworkOn)
                    {
                        _basicModules.Add(_scyllaCaptureModule);
                    }

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

                    DefCache.GetDef<ResearchDbDef>("pp_ResearchDB").Researches.Remove(DefCache.GetDef<ResearchDef>("PX_Aircraft_Electrolaser_ResearchDef"));
                    DefCache.GetDef<ResearchDbDef>("pp_ResearchDB").Researches.Remove(DefCache.GetDef<ResearchDef>("PX_Aircraft_HypersonicMissile_ResearchDef"));
                    DefCache.GetDef<ResearchDbDef>("pp_ResearchDB").Researches.Remove(DefCache.GetDef<ResearchDef>("PX_Aircraft_MaskedManticore_ResearchDef"));
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

                    helios.BaseStats.Speed = new EarthUnits(625);
                    thunderbird.BaseStats.Speed = new EarthUnits(405);
                    blimp.BaseStats.Speed = new EarthUnits(325);

                    manticore.BaseStats.HitPoints = 400;
                    thunderbird.BaseStats.HitPoints = 400;
                    blimp.BaseStats.HitPoints = 400;
                    helios.BaseStats.HitPoints = 400;

                    manticore.BaseStats.MaxHitPoints = 400;
                    thunderbird.BaseStats.MaxHitPoints = 400;
                    blimp.BaseStats.MaxHitPoints = 400;
                    helios.BaseStats.MaxHitPoints = 400;

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

                    // Example of initializing the dictionary with the provided values
                    Dictionary<string, string> keyReplacements = new Dictionary<string, string>
                    {
                        { "VOID_OMEN_TITLE_7", "VOID_OMEN_TITLE_7_ALT" },
                       { "VOID_OMEN_DESCRIPTION_TEXT_7", "VOID_OMEN_DESCRIPTION_TEXT_7_ALT"},
                        {"VOID_OMEN_REMOVAL_TEXT_7", "VOID_OMEN_REMOVAL_TEXT_7_ALT" }
                    };



                    // Get the TermData for the key

                    foreach (string key in keyReplacements.Keys)
                    {
                        TermData termData = LocalizationManager.GetTermData(key);
                        if (termData != null)
                        {
                            // Get the current language index
                            int languageIndex = -1;
                            foreach (var source in LocalizationManager.Sources)
                            {
                                languageIndex = source.GetLanguageIndex(LocalizationManager.CurrentLanguage);
                                if (languageIndex >= 0)
                                {
                                    // Set the new translation
                                    termData.SetTranslation(languageIndex, keyReplacements[key]);
                                    break;
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
                    CreateBlimpMutationLabModule();
                    CreateBlimpMutogPenModule();
                    CreateBlimpMistModule();
                    // CreateHeliosSpeedModule();
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
                    ResearchDef unlockResearch = DefCache.GetDef<ResearchDef>("NJ_NeuralTech_ResearchDef");
                    string guid3 = "{5F0542BC-6816-4434-A404-C459D82D8518}";
                    AddToResearchUnlock(unlockResearch, module, guid3);

                    _thunderbirdGroundAttackModule = module;
                    _thunderbirdModules.Add(_thunderbirdGroundAttackModule);

                    _thunderbirdGroundAttackBuffResearchDefs = new List<ResearchDef>()
                    {
                        DefCache.GetDef<ResearchDef>("NJ_PurificationTech_ResearchDef"), //NJ_PurificationTech_ResearchDef Incendiary Tech
                        DefCache.GetDef<ResearchDef>("NJ_GuidanceTech_ResearchDef"),  //NJ_GuidanceTech_ResearchDef Advanced Missile Technology
                        DefCache.GetDef<ResearchDef>("NJ_ExplosiveTech_ResearchDef"), //NJ_ExplosiveTech_ResearchDef Advanced Rocket Technology
                    };



                    //PX_VirophageWeapons_ResearchDef
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
                    ResearchDef unlockResearch = DefCache.GetDef<ResearchDef>("NJ_Aircraft_ResearchDef");
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

                    _thunderbirdScannerComponent = newScanComponent;

                    GeoScannerDef scannerDef = Helper.CreateDefFromClone(scannerSource, "{72331364-A129-4232-A79F-F352DC1972F6}", id);
                    scannerDef.MaximumRange.Value = _thunderbirdScannerRangeBase;
                    scannerDef.ExpansionTimeHours = _thunderbirdScannerTime;
                    //  newGeoScanComponent.SitesToFind = new List<GeoSiteType>() { GeoSiteType.Haven };
                    //  newGeoScanComponent.RevealSites = true;

                    // newGeoScan.RangeEffectPrefab = null;

                    ComponentSetDef scannerCompSource = DefCache.GetDef<ComponentSetDef>("PP_Scanner");
                    ComponentSetDef scannerComp = Helper.CreateDefFromClone(scannerCompSource, "{B09B8F9F-99C7-4BCA-8ED2-9461628EF059}", id);
                    // scannerComp.Prefab = ancientProbeComp.Prefab;
                    scannerComp.Components[0] = newRangeComponent;
                    scannerComp.Components[1] = newScanComponent;
                    scannerComp.Components[2] = scannerDef;

                    newAbility.ScanActorDef = scannerComp;

                    newAbility.ViewElementDef = Helper.CreateDefFromClone(scanAbilitySource.ViewElementDef, "{3AF75FE6-4483-484A-862B-5C34214EEF02}", id);
                    newAbility.ViewElementDef.ShowCharges = false;

                    _thunderbirdScanAbilityDef = newAbility;

                    _thunderbirdModules.Add(_thunderbirdScannerModule);

                    _thunderbirdScannerBuffResearchDefs = new List<ResearchDef>()
                    {
                        DefCache.GetDef<ResearchDef>("NJ_NeuralTech_ResearchDef"),
                        DefCache.GetDef<ResearchDef>("NJ_SateliteUplink_ResearchDef"),
                        DefCache.GetDef<ResearchDef>("PX_Alien_Citadel_ResearchDef"),
                    };


                    //PX_Alien_Colony_ResearchDef
                    //PX_Alien_Lair_ResearchDef

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

                    GeoVehicleModuleDef module = CreateModule(name, guid1, guid2, nameKey, descriptionKey, smallIcon, largeIcon,
                        GeoVehicleModuleDef.GeoVehicleModuleBonusType.None, _healingStaminaBase);
                    ResearchDef unlockResearch = DefCache.GetDef<ResearchDef>("NJ_Technician_ResearchDef");
                    string guid3 = "{7CFC62CB-008C-4545-9636-5DD3C738E565}";
                    AddToResearchUnlock(unlockResearch, module, guid3);

                    _thunderbirdWorkshopModule = module;
                    _thunderbirdModules.Add(_thunderbirdWorkshopModule);

                    _thunderbirdWorkshopBuffResearchDefs = new List<ResearchDef>()
                    {
                        DefCache.GetDef<ResearchDef>("NJ_Bionics2_ResearchDef"),
                        DefCache.GetDef<ResearchDef>("SYN_Bionics3_ResearchDef"),
                    };

                    //  DefCache.GetDef<ResearchDef>("SYN_NanoTech_ResearchDef"), //Advanced Nanotechnology
                    //PX_BlastResistanceVest_ResearchDef //acid resistance tech
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

                    GeoVehicleModuleDef module = CreateModule(name, guid1, guid2, nameKey, descriptionKey, smallIcon, largeIcon, GeoVehicleModuleDef.GeoVehicleModuleBonusType.Range, 1000);
                    ResearchDef unlockResearch = DefCache.GetDef<ResearchDef>("NJ_VehicleTech_ResearchDef");
                    string guid3 = "{364448DA-316F-46E1-8F80-E5DAA9DCB454}";
                    AddToResearchUnlock(unlockResearch, module, guid3);

                    _thunderbirdRangeModule = module;
                    _thunderbirdModules.Add(_thunderbirdRangeModule);

                    _thunderbirdRangeBuffResearchDefs = new List<ResearchDef>()
                    {
                        DefCache.GetDef<ResearchDef>("NJ_PRCRTechTurret_ResearchDef"), //Advanced Technician Weapons
                        DefCache.GetDef<ResearchDef>("NJ_AutomatedFactories_ResearchDef"),
                        DefCache.GetDef<ResearchDef>("NJ_CentralizedAI_ResearchDef"),
                    };

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

                    string id = "HELIOS_HEALING";
                    string name = $"TFTV{id}Module";
                    string guid1 = "{B4BB88F8-75CC-4B02-84A9-991E3180E0AD}";
                    string guid2 = "{68F476D0-5856-41DC-B803-0BEE8C0977A5}";
                    string nameKey = $"TFTV_{id.ToUpper()}_MODULE_NAME";
                    string descriptionKey = $"TFTV_{id.ToUpper()}_MODULE_DESCRIPTION";
                    Sprite smallIcon = Helper.CreateSpriteFromImageFile($"TFTV_{id}_Small.png");
                    Sprite largeIcon = smallIcon; //Helper.CreateSpriteFromImageFile("TFTVCaptureDronesModuleLargeIcon.png");

                    GeoVehicleModuleDef module = CreateModule(name, guid1, guid2, nameKey, descriptionKey, smallIcon, largeIcon,
                        GeoVehicleModuleDef.GeoVehicleModuleBonusType.None, _healingStaminaBase);
                    ResearchDef unlockResearch = DefCache.GetDef<ResearchDef>("SYN_Rover_ResearchDef");
                    string guid3 = "{89428921-FDC6-4D84-A657-85C899A4DC55}";
                    AddToResearchUnlock(unlockResearch, module, guid3);

                    _heliosPanaceaModule = module;
                    _heliosModules.Add(_heliosPanaceaModule);

                    _heliosStatisChamberBuffResearchDefs = new List<ResearchDef>()
                    {
                        DefCache.GetDef<ResearchDef>("SYN_NanoTech_ResearchDef"), //Advanced Nanotechnology
                        DefCache.GetDef<ResearchDef>("SYN_NanoHealing_ResearchDef"), //Medical Nanites
                        DefCache.GetDef<ResearchDef>("SYN_PoisonResistance_ResearchDef"),
                    };
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
                    ResearchDef unlockResearch = DefCache.GetDef<ResearchDef>("SYN_SentientAITech_ResearchDef");
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

            /*   private static void CreateHeliosSpeedModule()
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

                       GeoVehicleModuleDef module = CreateModule(name, guid1, guid2, nameKey, descriptionKey, smallIcon, largeIcon, GeoVehicleModuleDef.GeoVehicleModuleBonusType.Speed, 400);
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
               }*/


            private static void CreateBlimpMistModule()
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
                    ResearchDef unlockResearch = DefCache.GetDef<ResearchDef>("ANU_MutationTech_ResearchDef");
                    string guid3 = "{8D90B9FD-FF4B-4C51-B679-1CABF65DAC73}";
                    AddToResearchUnlock(unlockResearch, module, guid3);

                    _blimpMistModule = module;
                    _blimpModules.Add(_blimpMistModule);
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

                    List<ResearchDef> requiredResearches = new List<ResearchDef>()
                    {
                        DefCache.GetDef<ResearchDef>("PX_Alien_LiveQueen_ResearchDef"),
                        DefCache.GetDef<ResearchDef>("ANU_MutogTech_ResearchDef"),
                        DefCache.GetDef<ResearchDef>("ANU_AdvancedInfectionTech_ResearchDef")
                    };

                    List<string> researchRequirementGuids = new List<string>()
                    {
                        "{6CEDC675-EDA2-4FC8-9FDE-F37C050CCEE3}",
                        "{126E569E-3D8D-4D80-A807-FD36250DA84F}",
                        "{2B17D886-0F69-4E78-AC9D-658B3D6FEA88}"
                    };

                    ExistingResearchRequirementDef[] existingResearchRequirementDefs = TFTVCommonMethods.CreateExistingResearchRequirementDefs(
                        requiredResearches, researchRequirementGuids);
                    List<string> newResearchGuids = new List<string>() { "{474DDA70-14EA-40DE-8F0A-B5F5F56766E9}", "{D949E332-9D31-4100-8752-80EB573E8CAA}" };

                    ResearchViewElementDef backgroundViewElement = DefCache.GetDef<ResearchViewElementDef>("PX_ExperimentalKaosBuggyTechnology_ViewElementDef");

                    ResearchDef newResearch = TFTVCommonMethods.CreateResearch(
                        name, 800, $"TFTV_{id.ToUpper()}_MODULE_RESEARCH", newResearchGuids, existingResearchRequirementDefs, null, null, backgroundViewElement);

                    string guid3 = "{0935D009-2AE0-4246-8B6F-346D122D38D5}";

                    AddToResearchUnlock(newResearch, module, guid3);
                    _blimpMutogPenModule = module;
                    _blimpModules.Add(_blimpMutogPenModule);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


            private static void CreateBlimpMutationLabModule()
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

                    GeoVehicleModuleDef module = CreateModule(name, guid1, guid2, nameKey, descriptionKey, smallIcon, largeIcon, GeoVehicleModuleDef.GeoVehicleModuleBonusType.None, _healingStaminaBase);
                    ResearchDef unlockResearch = DefCache.GetDef<ResearchDef>("ANU_MutationTech_ResearchDef");
                    string guid3 = "{178647BB-9EE3-4207-AA19-A8AF89DF2C50}";
                    AddToResearchUnlock(unlockResearch, module, guid3);

                    ResearchDef buffResearch0 = DefCache.GetDef<ResearchDef>("ANU_AnuFungusFood_ResearchDef");
                    ResearchDef buffResearch1 = DefCache.GetDef<ResearchDef>("ANU_StimTech_ResearchDef");

                    _blimpMutationLabModuleBuffResearches = new List<ResearchDef>() { buffResearch0, buffResearch1 };

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



                    ResearchDef unlockResearch = DefCache.GetDef<ResearchDef>("ANU_Blimp_ResearchDef");
                    ResearchDef buffResearch0 = DefCache.GetDef<ResearchDef>("ANU_AdvancedBlimp_ResearchDef");
                    ResearchDef buffResearch1 = DefCache.GetDef<ResearchDef>("ANU_AcidTech_ResearchDef");
                    ResearchDef buffResearch2 = DefCache.GetDef<ResearchDef>("PX_AdvancedAcidTech_ResearchDef");


                    string guid3 = "{8DB1B5E5-EC74-4318-9AF6-F1A4A27EE317}";
                    AddToResearchUnlock(unlockResearch, _blimpSpeedModule, guid3);
                    _blimpModules.Add(_blimpSpeedModule);

                    _blimpSpeedModuleBuffResearches = new List<ResearchDef>() { buffResearch0, buffResearch1, buffResearch2 };


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
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
                        //  TFTVLogger.Always($"{research.Id} should grant {module.name} via {researchRewardDef.name}");
                    }
                    else
                    {
                        ManufactureResearchRewardDef researchRewardDefSource = DefCache.GetDef<ManufactureResearchRewardDef>("SYN_Aircraft_HybernationPods_ResearchDef_ManufactureResearchRewardDef_0");
                        researchRewardDef = Helper.CreateDefFromClone(researchRewardDefSource, guid, $"{module.name}");
                        researchRewardDef.Items = new ItemDef[] { module };
                        research.Unlocks = research.Unlocks.AddToArray(researchRewardDef);
                        // TFTVLogger.Always($"{research.Id} should grant {module.name} via {researchRewardDef.name}");

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
                    speedModule.GeoVehicleModuleBonusValue = 150;
                    speedModule.ViewElementDef.SmallIcon = Helper.CreateSpriteFromImageFile("TFTV_SpeedModuleSmallIcon.png");
                    speedModule.ViewElementDef.LargeIcon = speedModule.ViewElementDef.SmallIcon;
                    speedModule.ViewElementDef.RosterIcon = speedModule.ViewElementDef.SmallIcon;
                    speedModule.ViewElementDef.InventoryIcon = speedModule.ViewElementDef.SmallIcon;
                    speedModule.ViewElementDef.DeselectIcon = speedModule.ViewElementDef.SmallIcon;
                    speedModule.Tags.RemoveAt(1);
                    CreateMarketplaceItem(speedModule.name, "{EA57516D-AACF-41FA-BBDD-02248B4F45BD}", 400, 1, speedModule);
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
                    rangeModule.ViewElementDef.SmallIcon = Helper.CreateSpriteFromImageFile("TFTV_RangeModuleSmallIcon.png");
                    rangeModule.ViewElementDef.LargeIcon = rangeModule.ViewElementDef.SmallIcon;
                    rangeModule.ViewElementDef.RosterIcon = rangeModule.ViewElementDef.SmallIcon;
                    rangeModule.ViewElementDef.InventoryIcon = rangeModule.ViewElementDef.SmallIcon;
                    rangeModule.ViewElementDef.DeselectIcon = rangeModule.ViewElementDef.SmallIcon;

                    rangeModule.Tags.RemoveAt(1);
                    CreateMarketplaceItem(rangeModule.name, "{C0E985A9-E180-4C9C-A98B-B8E8E38FD9B1}", 400, 1, rangeModule);
                    _basicRangeModule = rangeModule;
                    _basicModules.Add(rangeModule);


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

                    _scanAbilityDef = DefCache.GetDef<ScanAbilityDef>("ScanAbilityDef");

                    // ScanAbilityDef newAbility = Helper.CreateDefFromClone(source, "{F13000CF-3F7F-45CB-A435-8F85B909D294}", "TFTVScanVehicleAbility");

                    _scanAbilityDef.ViewElementDef.ShowCharges = false;

                    ComponentSetDef ancientProbeComp = DefCache.GetDef<ComponentSetDef>("PP_AncientSiteProbe");

                    GeoRangeComponentDef sourceGeoScan = DefCache.GetDef<GeoRangeComponentDef>("E_SiteScannerRange [PhoenixBase_GeoSite]");


                    GeoRangeComponentDef newRangeComponent = Helper.CreateDefFromClone(sourceGeoScan, "{0F449AF2-3754-4923-9143-31E2A1A02660}", "TFTVGeoScanRangeComponent");
                    newRangeComponent.RangeTransformPath = "GlobeOffset";

                    GeoScannerDef geoScannerDefSource = DefCache.GetDef<GeoScannerDef>("E_PP_Scanner_Actor_ [PP_Scanner]");
                    GeoScannerDef newScannerComponent = Helper.CreateDefFromClone(geoScannerDefSource, "{39036AB1-C8A5-480E-B647-596A9EC12FFC}", "TFTVGeoScanScannerComponent");

                    newScannerComponent.ExpansionTimeHours = _basicScannerTime;
                    newScannerComponent.MaximumRange.Value = _basicScannerRangeBase;

                    GeoScanComponentDef geoScanComponentSource = DefCache.GetDef<GeoScanComponentDef>("E_Scan [PP_Scanner]");

                    GeoScanComponentDef newScanComponent = Helper.CreateDefFromClone(geoScanComponentSource, "{7853D47E-C0BD-4F0A-888D-C4AEF2B0983F}", "TFTVGeoScanScanComponent");
                    //  newGeoScanComponent.SitesToFind = new List<GeoSiteType>() { GeoSiteType.Haven };
                    //  newGeoScanComponent.RevealSites = true;

                    // newGeoScan.RangeEffectPrefab = null;

                    ComponentSetDef scannerComp = DefCache.GetDef<ComponentSetDef>("PP_Scanner");
                    //  scannerComp.Prefab = ancientProbeComp.Prefab;
                    scannerComp.Components[0] = newRangeComponent;
                    scannerComp.Components[1] = newScanComponent;
                    scannerComp.Components[2] = newScannerComponent;

                    _basicScannerComponent = newScanComponent;

                    //ResearchDef unlockResearch = DefCache.GetDef<ResearchDef>("PX_Alien_Colony_ResearchDef");
                    string guid3 = "{9EBC92B5-80C2-453B-8757-7320972F5512}";
                    // AddToResearchUnlock(unlockResearch, _basicScannerModule, guid);
                    CreateMarketplaceItem(name, guid3, 400, 1, _basicScannerModule);
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
                    // CreateMarketplaceItem(name, guid3, 400, 1, _basicPassengerModule);
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

                    _basicClinicModule = CreateModule(name, guid1, guid2, nameKey, descriptionKey, smallIcon, largeIcon, GeoVehicleModuleDef.GeoVehicleModuleBonusType.Recuperation, _healingStaminaBase);

                    // ResearchDef unlockResearch = DefCache.GetDef<ResearchDef>("PX_Alien_Acheron_ResearchDef");
                    string guid3 = "{62336005-44DB-4696-B1D1-83A2B7DD68E7}";
                    // AddToResearchUnlock(unlockResearch, _basicClinicModule, guid3);
                    _basicModules.Add(_basicClinicModule);
                    CreateMarketplaceItem(name, guid3, 400, 1, _basicClinicModule);
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

                    List<ResearchDef> requiredResearches = new List<ResearchDef>() { DefCache.GetDef<ResearchDef>("PX_Alien_LiveQueen_ResearchDef") };
                    List<string> researchRequirementGuids = new List<string>() { "{406DAC75-FCA7-471D-B393-6FDF3B075B21}" };
                    ExistingResearchRequirementDef[] existingResearchRequirementDefs = TFTVCommonMethods.CreateExistingResearchRequirementDefs(
                        requiredResearches, researchRequirementGuids);
                    List<string> newResearchGuids = new List<string>() { "{0DD24726-4E0F-46CC-A580-23FF27A19D60}", "{CB19740C-1506-4A7B-BA8F-1B41EC466DA3}" };

                    ResearchViewElementDef backgroundViewElement = DefCache.GetDef<ResearchViewElementDef>("PX_ExperimentalScarabTechnology_ViewElementDef");

                    ResearchDef newResearch = TFTVCommonMethods.CreateResearch(
                        name, 800, "TFTV_VEHICLE_HARNESS_MODULE_RESEARCH", newResearchGuids, existingResearchRequirementDefs, null, null, backgroundViewElement);

                    string guid3 = "{A5C7C767-ABBA-4B5A-9C7C-2A41EC6597CC}";
                    AddToResearchUnlock(newResearch, _vehicleHarnessModule, guid3);
                    _basicModules.Add(_vehicleHarnessModule);
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




            [HarmonyPatch(typeof(GeoEventChoiceOutcome), "GenerateFactionReward")]
            public static class GeoEventChoiceOutcome_GenerateFactionReward_Patch
            {
                public static void Postfix(GeoEventChoiceOutcome __instance, GeoscapeEventContext context, string eventID, GeoFaction faction)
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
        internal class Modules
        {
            internal class Tiers
            {
                internal static int GetBlimpSpeedTier()
                {
                    try
                    {
                        Research phoenixResearch = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction.Research;
                        int speedBuff = 1;

                        foreach (ResearchDef researchDef in _blimpSpeedModuleBuffResearches)
                        {
                            if (phoenixResearch.HasCompleted(researchDef.Id))
                            {

                                speedBuff += 1;
                                //TFTVLogger.Always($"{researchDef.Id} completed, so adding {_mistSpeedModuleBuff} to speed. Current speedbuff for Bioflux is {speedBuff}", false);
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

                internal static int GetBuffLevelFromResearchDefs(List<ResearchDef> researchDefs)
                {
                    try
                    {
                        int buffLevel = 1;
                        Research phoenixResearch = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction.Research;


                        foreach (ResearchDef researchDef in researchDefs)
                        {
                            if (phoenixResearch.HasCompleted(researchDef.Id))
                            {
                                buffLevel++;
                            }
                        }

                        return buffLevel;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                internal static int GetMistModuleBuffLevel()
                {
                    try
                    {
                        int buffLevel = 1;
                        Research phoenixResearch = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction.Research;

                        if (phoenixResearch.HasCompleted("ANU_MutationTech3_ResearchDef"))
                        {
                            buffLevel = 3;
                        }
                        else if (phoenixResearch.HasCompleted("ANU_MutationTech2_ResearchDef"))
                        {
                            buffLevel = 2;
                        }

                        return buffLevel;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                internal static int GetStealthTierForUI()
                {
                    try
                    {
                        int buffLevel = 1;
                        Research phoenixResearch = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction.Research;

                        if (phoenixResearch.HasCompleted("SYN_SafeZoneProject_ResearchDef"))
                        {
                            buffLevel += 1;
                        }

                        if (phoenixResearch.HasCompleted("SYN_InfiltratorTech_ResearchDef"))
                        {
                            buffLevel += 1;
                        }

                        if (phoenixResearch.HasCompleted("SYN_NightVision_ResearchDef"))
                        {
                            buffLevel += 1;
                        }



                        return buffLevel;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }


                }





                internal static int GetGWABuffLevel()
                {
                    try
                    {
                        int buffLevel = 1;
                        Research phoenixResearch = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction.Research;


                        if (phoenixResearch.HasCompleted("NJ_GuidanceTech_ResearchDef")) // Advanced Missile Technology
                        {
                            buffLevel += 1;
                        }

                        if (phoenixResearch.HasCompleted("NJ_ExplosiveTech_ResearchDef"))//Advanced Rocket Technology

                        {
                            buffLevel += 1;
                        }

                        return buffLevel;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }


            }
            internal class Tactical
            {
                private static int _thunderBirdScannerPresent = 0;
                private static int _captureDronesPresent = 0;
                private static int _mistRepellerPresent = 0;
                private static int _heliosStealthPresent = 0;
                private static int _blimpMistPresent = 0;
                private static int _heliosPresent = 0;
                private static int _heliosNanotechPresent = 0;
                private static int _thunderbirdGroundAttackWeaponPresent = 0;
                private static int _blimpMutationLabFrenzyPresent = 0;
                private static int _blimpPriestResearch = 0;
                private static int _heliosVestBuff = 0;
                private static int _heliosStealthModulePerceptionBuff = 0;
                private static int _thunderbirdWorkshopPresent = 0;
                private static int _nestResearched = 0;
                private static int _lairResearched = 0;

                internal static string ReportModulesPresent()
                {
                    try
                    {
                        string report = "";

                        if (_thunderBirdScannerPresent > 0)
                        {
                            report += "Scanner Module Present\n";
                        }

                        if (_captureDronesPresent > 0)
                        {
                            report += "Capture Drones Module Present\n";
                        }

                        if (_mistRepellerPresent > 0)
                        {
                            report += "Mist Repeller Module Present\n";
                        }

                        if (_heliosStealthPresent > 0)
                        {
                            report += $"Helios Stealth Module Present, level {_heliosStealthPresent}\n";
                        }

                        if (_blimpMistPresent > 0)
                        {
                            report += $"Blimp WP Module Present, level {_blimpMistPresent}\n";
                        }

                        if (_heliosPresent > 0)
                        {
                            report += "Helios Present\n";
                        }

                        if (_heliosNanotechPresent > 0)
                        {
                            report += "Helios Statis Chamber Present\n";
                        }

                        if (_thunderbirdGroundAttackWeaponPresent > 0)
                        {
                            report += $"Thunderbird Ground Attack Weapon Present, level {_thunderbirdGroundAttackWeaponPresent}\n";
                        }

                        if (_blimpMutationLabFrenzyPresent > 0)
                        {
                            report += "Blimp Mutation Lab Frenzy Module Present\n";
                        }

                        if (_blimpPriestResearch > 0)
                        {
                            report += "Blimp Mutation Lab Priest Research Module Present\n";
                        }

                        if (_heliosVestBuff > 0)
                        {
                            report += "Helios Vest Buff Present\n";
                        }

                        if (_heliosStealthModulePerceptionBuff > 0)
                        {
                            report += $"Helios Stealth Module Perception Buff Present, level {_heliosStealthModulePerceptionBuff}\n";
                        }
                        if (_thunderbirdWorkshopPresent > 0)
                        {
                            report += "Thunderbird Workshop Present\n";
                        }
                        if (_nestResearched > 0)
                        {
                            report += "Nest Researched\n";
                        }
                        if (_lairResearched > 0)
                        {
                            report += "Lair Researched\n";
                        }

                        return report;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }

                }

                internal static void LoadInternalDataForTactical()
                {
                    try
                    {
                        _thunderBirdScannerPresent = InternalData.ModulesInTactical[0];
                        _captureDronesPresent = InternalData.ModulesInTactical[1];
                        _mistRepellerPresent = InternalData.ModulesInTactical[2];
                        _heliosStealthPresent = InternalData.ModulesInTactical[3];
                        _blimpMistPresent = InternalData.ModulesInTactical[4];
                        _heliosPresent = InternalData.ModulesInTactical[5];
                        _heliosNanotechPresent = InternalData.ModulesInTactical[6];
                        _thunderbirdGroundAttackWeaponPresent = InternalData.ModulesInTactical[7];
                        _blimpMutationLabFrenzyPresent = InternalData.ModulesInTactical[8];
                        _blimpPriestResearch = InternalData.ModulesInTactical[9];
                        _heliosVestBuff = InternalData.ModulesInTactical[10];
                        _heliosStealthModulePerceptionBuff = InternalData.ModulesInTactical[11];
                        _thunderbirdWorkshopPresent = InternalData.ModulesInTactical[12];
                        _nestResearched = InternalData.ModulesInTactical[13];
                        _lairResearched = InternalData.ModulesInTactical[14];

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                internal static void SaveInternalDataForTactical()
                {
                    try
                    {
                        InternalData.ModulesInTactical[0] = _thunderBirdScannerPresent;
                        InternalData.ModulesInTactical[1] = _captureDronesPresent;
                        InternalData.ModulesInTactical[2] = _mistRepellerPresent;
                        InternalData.ModulesInTactical[3] = _heliosStealthPresent;
                        InternalData.ModulesInTactical[4] = _blimpMistPresent;
                        InternalData.ModulesInTactical[5] = _heliosPresent;
                        InternalData.ModulesInTactical[6] = _heliosNanotechPresent;
                        InternalData.ModulesInTactical[7] = _thunderbirdGroundAttackWeaponPresent;
                        InternalData.ModulesInTactical[8] = _blimpMutationLabFrenzyPresent;
                        InternalData.ModulesInTactical[9] = _blimpPriestResearch;
                        InternalData.ModulesInTactical[10] = _heliosVestBuff;
                        InternalData.ModulesInTactical[11] = _heliosStealthModulePerceptionBuff;
                        InternalData.ModulesInTactical[12] = _thunderbirdWorkshopPresent;
                        InternalData.ModulesInTactical[13] = _nestResearched;
                        InternalData.ModulesInTactical[14] = _lairResearched;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                internal static void ClearTacticalDataOnLoad()
                {
                    try
                    {
                        _thunderBirdScannerPresent = 0;
                        _captureDronesPresent = 0;
                        _mistRepellerPresent = 0;
                        _heliosStealthPresent = 0;
                        _blimpMistPresent = 0;
                        _heliosPresent = 0;
                        _heliosNanotechPresent = 0;
                        _thunderbirdGroundAttackWeaponPresent = 0;
                        _blimpMutationLabFrenzyPresent = 0;
                        _blimpPriestResearch = 0;
                        _heliosVestBuff = 0;
                        _heliosStealthModulePerceptionBuff = 0;
                        _thunderbirdWorkshopPresent = 0;
                        _nestResearched = 0;
                        _lairResearched = 0;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                public static void CheckTacticallyRelevantModulesOnVehicle(GeoVehicle geoVehicle, GeoMission geoMission = null)
                {
                    try
                    {

                        ClearTacticalDataOnLoad();

                        if (!AircraftReworkOn || geoVehicle == null)
                        {
                            return;
                        }

                        Research phoenixResearch = geoVehicle.GeoLevel.PhoenixFaction.Research;

                        if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _thunderbirdScannerModule))
                        {
                            _thunderBirdScannerPresent = 0;

                            if (phoenixResearch.HasCompleted("PX_Alien_Colony_ResearchDef"))
                            {
                                _nestResearched = 1;
                            }

                            if (phoenixResearch.HasCompleted("PX_Alien_Lair_ResearchDef"))
                            {
                                _lairResearched = 1;
                            }

                            if (phoenixResearch.HasCompleted("NJ_NeuralTech_ResearchDef"))
                            {
                                _thunderBirdScannerPresent = 1;
                            }

                        }

                        if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _captureDronesModule))
                        {
                            _captureDronesPresent = 1;
                        }

                        if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _heliosMistRepellerModule))
                        {
                            _mistRepellerPresent = 1;
                        }

                        if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _heliosStealthModule))
                        {
                            _heliosStealthPresent = 1;
                            _heliosStealthModulePerceptionBuff = 1;

                            if (phoenixResearch.HasCompleted("SYN_SafeZoneProject_ResearchDef"))
                            {
                                _heliosStealthPresent += 1;
                            }

                            if (phoenixResearch.HasCompleted("SYN_InfiltratorTech_ResearchDef"))
                            {
                                _heliosStealthPresent += 1;
                            }

                            if (phoenixResearch.HasCompleted("SYN_NightVision_ResearchDef"))
                            {
                                _heliosStealthModulePerceptionBuff += 1;
                            }

                        }

                        if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _blimpMistModule))
                        {
                            _blimpMistPresent = Tiers.GetMistModuleBuffLevel();
                        }

                        if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _heliosPanaceaModule))
                        {
                            if (phoenixResearch.HasCompleted("SYN_NanoTech_ResearchDef"))
                            {
                                _heliosNanotechPresent = 1;
                            }

                            if (phoenixResearch.HasCompleted("SYN_NanoHealing_ResearchDef"))
                            {
                                _heliosVestBuff = 1;
                            }
                        }

                        if (geoVehicle.VehicleDef == helios)
                        {
                            _heliosPresent = 1;
                        }

                        if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _thunderbirdGroundAttackModule))
                        {
                            //must exclude, Palace, Alien Colony, Base Defense, 

                            TFTVLogger.Always($"Checking Thunderbird Ground Attack module for mission {geoMission.MissionDef?.name}");

                            if (geoMission.MissionDef.MissionTags.Contains(Shared.SharedGameTags.BaseDefenseMissionTag) ||
                                geoMission.MissionDef.MissionTags.Contains(Shared.SharedGameTags.BaseInfestationMissionTag) ||
                                geoMission.MissionDef.MissionTags.Contains(DefCache.GetDef<MissionTypeTagDef>("MissionTypeAlienCitadelAssault_MissionTagDef")) ||
                                geoMission.MissionDef.MissionTags.Contains(DefCache.GetDef<MissionTypeTagDef>("MissionTypeAlienLairAssault_MissionTagDef")) ||
                                geoMission.MissionDef.MissionTags.Contains(DefCache.GetDef<MissionTypeTagDef>("MissionTypeAlienNestAssault_MissionTagDef")) ||
                                (geoMission.MissionDef.MapPlotDef != null && geoMission.MissionDef.MapPlotDef.name.Contains("ALN_PLT")))

                            {
                            }
                            else
                            {
                                TFTVLogger.Always($"Setting _thunderbirdGroundAttackWeapon to true");

                                _thunderbirdGroundAttackWeaponPresent = Tiers.GetGWABuffLevel();
                            }

                        }

                        if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _blimpMutationLabModule) && phoenixResearch.HasCompleted("ANU_StimTech_ResearchDef"))
                        {
                            _blimpMutationLabFrenzyPresent = 1;
                        }
                        if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _blimpMistModule) && phoenixResearch.HasCompleted("ANU_AnuPriest_ResearchDef"))
                        {
                            _blimpPriestResearch = 1;
                        }

                        if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _thunderbirdWorkshopModule))
                        {
                            //pending acid res implementation, + self-repair implementation
                            _thunderbirdWorkshopPresent = 1;

                            if (phoenixResearch.HasCompleted("PX_BlastResistanceVest_ResearchDef"))
                            {

                                _thunderbirdWorkshopPresent = 2;
                            }
                            // TFTVLogger.Always($"_thunderbirdWorkshopPresent: {_thunderbirdWorkshopPresent} ");
                        }

                        HeliosStatisChamber.ImplementVestBuff();

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }

                }

                internal class WorkshopModule
                {

                    [HarmonyPatch(typeof(DamageOverTimeStatus), "ApplyEffect")]
                    public static class DamageOverTimeStatus_ApplyEffect_Patch
                    {
                        public static void Postfix(DamageOverTimeStatus __instance)
                        {
                            try
                            {
                                TacticalActor tacticalActor = __instance.TacticalActor;
                                //   TFTVLogger.Always($"running ApplyEffect. ta null? {tacticalActor == null} {__instance.DamageOverTimeStatusDef.name}");

                                if (!AircraftReworkOn || _thunderbirdWorkshopPresent < 2 || !tacticalActor.IsControlledByPlayer)
                                {
                                    return;
                                }

                                ItemSlot itemSlot1 = __instance.Target as ItemSlot;

                                /*  TFTVLogger.Always($"{tacticalActor.DisplayName} {} {tacticalActor.TacticalFaction==tacticalActor.TacticalLevel.GetFactionByCommandName("px")}");

                                  foreach(TacticalItem tacticalItem in itemSlot1?.GetAllDirectItems(false))
                                  {
                                      TFTVLogger.Always($"tacticalItem: {tacticalItem.DisplayName} {tacticalItem.GetTopMainAddon()?.GameTags.Contains(Shared.SharedGameTags.BionicalTag)}" +
                                          $";{tacticalItem.GetTopMainAddon()?.AddonDef?.name}"); //.
                                  }*/

                                if (__instance.Target is ItemSlot itemSlot && (itemSlot.GetAllDirectItems(false).
                                    Any(ti => ti.GameTags.Contains(Shared.SharedGameTags.BionicalTag) ||
                                    ti.GetTopMainAddon() != null && ti.GetTopMainAddon().GameTags.Contains(Shared.SharedGameTags.BionicalTag))
                                    || tacticalActor.HasGameTag(Shared.SharedGameTags.VehicleTag)))
                                {
                                    TFTVLogger.Always($"Lowering acid status for {__instance.TacticalActor.DisplayName}.");

                                    __instance.LowerDamageOverTimeLevel(__instance.DamageOverTimeStatusDef.LowerLevelPerTurn);
                                }

                            }
                            catch (Exception e)
                            {
                                TFTVLogger.Error(e);
                            }
                        }
                    }


                    /* [HarmonyPatch(typeof(DamageOverTimeStatus), "LowerDamageOverTimeLevelProportional")]
                     public static class DamageOverTimeStatus_LowerDamageOverTimeLevelProportional_Patch
                     {
                         public static void Prefix(DamageOverTimeStatus __instance, ref float multiplier)
                         {
                             try
                             {

                                 TacticalActor tacticalActor = __instance.TacticalActor;
                                 TFTVLogger.Always($"running LowerDamageOverTimeLevelProportional. ta null? {tacticalActor == null} multiplier: {multiplier}");

                                 if (!AircraftReworkOn || _thunderbirdWorkshopPresent < 2)
                                 {
                                     return;
                                 }

                                 TFTVLogger.Always($"affected bodypart?  {__instance.Target?.GetType()}; {__instance.Target?.ToString()}; {__instance.GetTargetSlotsNames()?.FirstOrDefault()}; {__instance.GetTargetSlotsNames()?.Count()}");


                                 if (tacticalActor != null && tacticalActor.BodyState.GetArmourItems().Any(a => a.GameTags.Contains(Shared.SharedGameTags.BionicalTag)))
                                 {
                                     TFTVLogger.Always($"Lowering acid status for {__instance.TacticalActor.DisplayName}.");

                                     multiplier = 2;
                                 }

                             }
                             catch (Exception e)
                             {
                                 TFTVLogger.Error(e);
                             }
                         }
                     }



                     [HarmonyPatch(typeof(DamageOverTimeStatus), "LowerDamageOverTimeLevel")]
                     public static class DamageOverTimeStatus_LowerDamageOverTimeLevel_Patch
                     {
                         public static void Prefix(DamageOverTimeStatus __instance, ref float amount)
                         {
                             try
                             {

                                 TacticalActor tacticalActor = __instance.TacticalActor;
                                 TFTVLogger.Always($"running LowerDamageOverTimeLevel. ta null? {tacticalActor == null}");

                                 if (!AircraftReworkOn || _thunderbirdWorkshopPresent < 2)
                                 {
                                     return;
                                 }




                                 TFTVLogger.Always($"affected bodypart?  {__instance.Target?.GetType()}; {__instance.Target?.ToString()}; {__instance.GetTargetSlotsNames()?.FirstOrDefault()}; {__instance.GetTargetSlotsNames()?.Count()}");


                                 if (tacticalActor != null && tacticalActor.BodyState.GetArmourItems().Any(a => a.GameTags.Contains(Shared.SharedGameTags.BionicalTag)))
                                 {
                                     TFTVLogger.Always($"Lowering acid status for {__instance.TacticalActor.DisplayName}.");

                                     amount = 20;
                                 }

                             }
                             catch (Exception e)
                             {
                                 TFTVLogger.Error(e);
                             }
                         }
                     }*/

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
                            //  ImplementBlimpWPModule(controller);
                            HeliosStatisChamber.ImplementPanaceaNanotechTactical(controller);
                            ImplementGroundAttackWeaponModule(controller);
                            ImplementMutationLabFrenzy(controller);
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }

                    }

                    private static void ImplementMutationLabFrenzy(TacticalLevelController controller)
                    {
                        try
                        {
                            if (_blimpMutationLabFrenzyPresent == 0)
                            {
                                return;
                            }

                            FrenzyStatusDef frenzyStatusDef = DefCache.GetDef<FrenzyStatusDef>("Frenzy_StatusDef");

                            foreach (TacticalActor tacticalActor in controller.GetFactionByCommandName("px").TacticalActors)
                            {
                              //  TFTVLogger.Always($"{tacticalActor.DisplayName} has {tacticalActor.BodyState.CharacterAddonsManager.RootAddon.Count()} addon items.");

                                foreach (Addon item in tacticalActor.BodyState.CharacterAddonsManager.RootAddon)
                                {
                                    if (item is TacticalItem tacticalItem)
                                    {
                                       // TFTVLogger.Always($"tacticalItem: {tacticalItem.DisplayName} {tacticalItem.GameTags.Contains(Shared.SharedGameTags.AnuMutationTag)}" +
                                       //     $";{tacticalItem.GetTopMainAddon()?.AddonDef?.name}"); //.

                                        if (tacticalItem.GameTags.Contains(Shared.SharedGameTags.AnuMutationTag) ||
                                            tacticalItem.GameTags.Contains(DefCache.GetDef<ItemMaterialTagDef>("MutatedTissue_ItemMaterialTagDef")))
                                        {
                                            if (tacticalActor.Status != null && !tacticalActor.HasStatus(frenzyStatusDef))
                                            {
                                                tacticalActor.Status.ApplyStatus(frenzyStatusDef);
                                            }


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

                    private static void ImplementGroundAttackWeaponModule(TacticalLevelController controller)
                    {
                        try
                        {
                            if (_thunderbirdGroundAttackWeaponPresent == 0)
                            {
                                return;
                            }

                            Sprite icon = null;
                            if (_groundAttackAbility.LevelIcons != null && _thunderbirdGroundAttackWeaponPresent - 1 < _groundAttackAbility.LevelIcons.Length)
                            {
                                icon = _groundAttackAbility.LevelIcons[Math.Max(_thunderbirdGroundAttackWeaponPresent - 1, 0)];
                            }


                            if (icon != null)
                            {
                                _groundAttackAbility.ViewElementDef.SmallIcon = icon;
                                _groundAttackAbility.ViewElementDef.LargeIcon = icon;
                            }

                            foreach (TacticalActor tacticalActor in controller.GetFactionByCommandName("px").TacticalActors)
                            {
                                GroundAttackWeaponAbility ability = tacticalActor.GetAbilityWithDef<GroundAttackWeaponAbility>(_groundAttackAbility) ?? (GroundAttackWeaponAbility)tacticalActor.AddAbility(_groundAttackAbility, tacticalActor);
                                ability.ConfigureForLevel(_thunderbirdGroundAttackWeaponPresent);
                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }

                    //Stealth module
                    private static void ImplementHeliosStealthModule(TacticalLevelController controller)
                    {
                        try
                        {
                            if (_heliosStealthPresent == 0)
                            {
                                return;
                            }

                            if (_heliosStealthPresent == 3)
                            {
                                _heliosStealthModuleStatus.StatModifications[0].Value = 0.5f;
                            }
                            else if (_heliosStealthPresent == 2)
                            {
                                _heliosStealthModuleStatus.StatModifications[0].Value = 0.3f;
                            }
                            else
                            {
                                _heliosStealthModuleStatus.StatModifications[0].Value = 0.1f;

                            }

                            if (_heliosStealthModulePerceptionBuff == 2)
                            {
                                _heliosStealthModuleStatus.StatModifications[1].Value = 15;
                            }
                            else
                            {
                                _heliosStealthModuleStatus.StatModifications[1].Value = 5;
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
                            if (_blimpMistPresent == 0)
                            {
                                return;
                            }

                            foreach (TacticalActor tacticalActor in controller.GetFactionByCommandName("px").TacticalActors)
                            {
                                if (tacticalActor.CharacterStats.Willpower != null && !tacticalActor.GameTags.Contains(Shared.SharedGameTags.VehicleTag))
                                {
                                    TFTVLogger.Always($"{tacticalActor.DisplayName} has {tacticalActor.CharacterStats.WillPoints} WPs");
                                    tacticalActor.CharacterStats.Willpower.SetOverchargeCapacity(3);
                                    tacticalActor.CharacterStats.Willpower.Add(3);
                                    TFTVLogger.Always($"after module is applied, has {tacticalActor.CharacterStats.WillPoints} WPs");
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
                internal class EveryTurn
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


                            ImplementArgusEye(tacticalFaction);


                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                    }

                    private static void ImplementArgusEye(TacticalFaction faction)
                    {
                        try
                        {
                            if (_thunderBirdScannerPresent == 0)
                            {
                                return;
                            }


                            TacticalFaction phoenixFaction = faction.TacticalLevel.GetFactionByCommandName("px");
                            if (faction != phoenixFaction)
                            {
                                foreach (TacticalActor tacticalActor in phoenixFaction.TacticalActors)
                                {
                                    if (!tacticalActor.Status.HasStatus(_argusEyeStatus))
                                    {
                                        TFTVLogger.Always($"applying {_argusEyeStatus.name} to {tacticalActor.DisplayName}. " +
                                            $"\ncurrent accuracy and perception: {tacticalActor.CharacterStats.Accuracy.Value.EndValue} " +
                                            $"{tacticalActor.CharacterStats.Perception.Value.EndValue}", false);
                                        tacticalActor.Status.ApplyStatus(_argusEyeStatus);
                                        TFTVLogger.Always($"new accuracy and perception: {tacticalActor.CharacterStats.Accuracy.Value.EndValue} " +
                                             $"{tacticalActor.CharacterStats.Perception.Value.EndValue}", false);

                                    }
                                }

                            }

                            if (faction == phoenixFaction)
                            {
                                foreach (TacticalActor tacticalActor in phoenixFaction.TacticalActors)
                                {
                                    if (tacticalActor.Status.HasStatus(_argusEyeStatus))
                                    {
                                        tacticalActor.Status.UnapplyStatus(tacticalActor.Status.GetStatusByName(_argusEyeStatus.EffectName));
                                        TFTVLogger.Always($"removing {_argusEyeStatus.name} to " +
                                            $"{tacticalActor.DisplayName} \naccuracy and perception: {tacticalActor.CharacterStats.Accuracy.Value.EndValue}", false);
                                    }
                                }

                            }

                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }


                    }

                    //Thunderbird Range module big Panda detection
                    private static void ImplementScannerTacticalAbility(TacticalLevelController controller)
                    {
                        try
                        {
                            if (_nestResearched == 0 && _lairResearched == 0)
                            {
                                return;
                            }

                            //  TFTVLogger.Always($"ImplementScannerTacticalAbility: {_thunderBirdScannerPresent}");

                            ClassTagDef hatchingSentinelTag = DefCache.GetDef<ClassTagDef>("SentinelHatching_ClassTagDef");
                            ClassTagDef spawneryTag = DefCache.GetDef<ClassTagDef>("SpawningPoolCrabman_ClassTagDef");

                            List<ClassTagDef> tags = new List<ClassTagDef>();

                            if (_nestResearched == 1)
                            {
                                tags.Add(hatchingSentinelTag);
                            }

                            if (_lairResearched == 1)
                            {
                                tags.Add(spawneryTag);
                            }

                            List<TacticalActor> list = (from a in controller.Map.GetTacActors<TacticalActor>(controller.CurrentFaction, FactionRelation.Enemy)
                                                        where !controller.CurrentFaction.Vision.KnownActors.ContainsKey(a)
                                                        && a.IsActive
                                                        && a.GameTags.Any(t => tags.Contains(t))
                                                        select a).ToList();
                            if (list.Count > 0)
                            {
                                foreach (TacticalActor a in list)
                                {
                                    TFTVLogger.Always($"actor spotted by scanner: {a.DisplayName}");
                                    controller.CurrentFaction.Vision.IncrementKnownCounter(a, KnownState.Revealed, 1, true);
                                }
                            }

                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                    }
                }
                internal class GroundAttackWeapon
                {



                    internal static void RemoveGroundAttackWeaponModuleAbility(TacticalLevelController controller)
                    {
                        try
                        {
                            if (_thunderbirdGroundAttackWeaponPresent == 0)
                            {
                                return;
                            }

                            foreach (TacticalActor tacticalActor in controller.GetFactionByCommandName("px").TacticalActors)
                            {
                                if (tacticalActor.GetAbilityWithDef<GroundAttackWeaponAbility>(_groundAttackAbility) != null)
                                {
                                    tacticalActor.RemoveAbility(_groundAttackAbility);
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
                internal class AnuMistModule
                {
                    /// <summary>
                    /// Modifying the perception range cost of the mist blob
                    /// </summary>
                    [HarmonyPatch(typeof(TacticalPerceptionBase), "get_MistBlobPerceptionRangeCost")]
                    public static class TacticalPerceptionBase_get_MistBlobPerceptionRangeCost_Patch
                    {
                        public static void Postfix(TacticalPerceptionBase __instance, ref float __result)
                        {
                            try
                            {
                                TFTVConfig config = TFTVMain.Main.Config;

                                if (TFTVVoidOmens.VoidOmensCheck[7] && config.MoreMistVO && AircraftReworkOn)
                                {
                                    __result /= 3;
                                }

                                TacticalActor tacticalActor = __instance.TacActorBase as TacticalActor;

                                if (tacticalActor == null)
                                {
                                    return;
                                }

                                int mistSymbiosisLevel = CheckForAnuBlimpMistModule(tacticalActor.TacticalFaction);

                                //if aircraft rework is on and player has researched Mutations2 or 3:

                                if (tacticalActor != null && mistSymbiosisLevel > 1
                                    && tacticalActor.BodyState.GetArmourItems().Any(a => a.GameTags.Contains(Shared.SharedGameTags.AnuMutationTag)))
                                {

                                    if (mistSymbiosisLevel == 2)
                                    {
                                        __result /= 2;
                                    }
                                    else
                                    {
                                        __result = 0;
                                    }
                                }

                                // TFTVLogger.Always($"MistBlobPerceptionRangeCost for {tacticalActor.DisplayName} is {__result}");


                            }
                            catch (Exception e)
                            {
                                TFTVLogger.Error(e);
                            }
                        }
                    }


                    private static int CheckForAnuBlimpMistModule(TacticalFaction tacticalFaction)
                    {
                        try
                        {
                            return AircraftReworkOn && _blimpMistPresent > 0 && tacticalFaction.IsControlledByPlayer ? _blimpMistPresent : 0;
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }


                    /// <summary>
                    /// Mist effects on WP
                    /// </summary>
                    [HarmonyPatch(typeof(TacticalActor), "ApplyMistEffects")]
                    public static class TacticalActor_ApplyMistEffects_Patch
                    {
                        public static bool Prefix(TacticalActor __instance)
                        {
                            try
                            {
                                //if aircraft rework is on and the Mist module is present and the actor has the Anu mutation tag,
                                //if the actor is in mist and player has research Mutations2, apply WP regen
                                //else, don't subtract WP

                                int mistSymbiosisLevel = CheckForAnuBlimpMistModule(__instance.TacticalFaction);

                                if (mistSymbiosisLevel == 0 ||
                                    !__instance.BodyState.GetArmourItems().Any(a => a.GameTags.Contains(Shared.SharedGameTags.AnuMutationTag)))
                                {
                                    return true;
                                }

                                if (!__instance.TacticalPerception.IsTouchingVoxel(TacticalVoxelType.Mist))
                                {
                                    return false;
                                }

                                TacticalVoxelMatrixDataDef voxelMatrixData = __instance.TacticalLevel.VoxelMatrix.VoxelMatrixData;

                                if (mistSymbiosisLevel > 1)
                                {
                                    __instance.CharacterStats.WillPoints.Add(voxelMatrixData.MistRecoverWillPointsValue);
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


                    /// <summary>
                    /// These 2 patches reveal non-Pandoran actors in mist to the player with the Anu blimp module and Priest Research.
                    /// </summary>
                    [HarmonyPatch(typeof(TacticalFactionVision))]
                    public static class TacticalFactionVision_PrefixPatch
                    {
                        //────────────────────────────────────────────────────────────
                        // 1. Replacement for GatherKnowableActors
                        //────────────────────────────────────────────────────────────
                        [HarmonyPrefix]
                        [HarmonyPatch("GatherKnowableActors")]
                        public static bool GatherKnowableActorsPrefix(TacticalFactionVision __instance,
                            TacticalActorBase fromActor,
                            Vector3 fromActorPos,
                            float basePerceptionRange,
                            ICollection<TacticalActorBase> visible,
                            ICollection<TacticalActorBase> located)
                        {
                            try
                            {

                                //also checks for AircraftRework


                                int mistSymbiosisLevel = CheckForAnuBlimpMistModule(fromActor.TacticalFaction);


                                if (mistSymbiosisLevel == 0 || _blimpPriestResearch == 0)
                                {
                                    return true;
                                }

                                //skip patch if fromActor tactical faction is Pandoran.

                                TacticalFaction fromActorTacticalFaction = fromActor.TacticalFaction;

                                if (fromActorTacticalFaction.TacticalFactionDef.MatchesShortName("ALN"))
                                {
                                    return true;
                                }


                                foreach (TacticalActorBase actor in fromActor.Map.GetActors<TacticalActorBase>())
                                {

                                    // Skip if the actor is the same as fromActor or if they are on the same faction, if the actor has null PerceptionBase, or actor is evaced
                                    if (actor == fromActor ||
                                        actor.TacticalFaction == fromActorTacticalFaction ||
                                        actor.TacticalPerceptionBase == null ||
                                        (actor.Status != null && actor.Status.HasStatus<EvacuatedStatus>()))
                                    {
                                        continue;
                                    }


                                    //if fromActor faction is Phoenix, the actor is in mist, the actor is not Pandoran, should be revealed to Player if _blimpPriestResearch!=0
                                    //note that will be revealed to Pandoran fromActor too because og method will run
                                    if (fromActorTacticalFaction.IsControlledByPlayer && actor.TacticalPerceptionBase.IsTouchingVoxel(TacticalVoxelType.Mist) &&
                                        actor.TacticalLevel.VoxelMatrix.VoxelMatrixData.MistOwnerFactionDef != actor.TacticalFaction.TacticalFactionDef)
                                    {
                                        visible.Add(actor);
                                        // TFTVLogger.Always($"{actor.DisplayName} touching Mist, so revealing to {fromActor.DisplayName}");
                                    }

                                    else if ((bool)TacticalFactionVision.CheckVisibleLineBetweenActors(
                                                 fromActor, fromActorPos, actor,
                                                 true, null,
                                                 1, null))
                                    {
                                        visible.Add(actor);
                                        //   TFTVLogger.Always($"{actor.DisplayName} in LOS at a distance of {(fromActorPos - actor.Pos).magnitude}, so revealing to {fromActor.DisplayName}");
                                    }
                                    else if (actor is TacticalActor && actor.IsAlive && !actor.IsCloaked)
                                    {
                                        TacticalLevelController tacticalLevel = fromActor.TacticalLevel;
                                        if ((fromActorPos - actor.Pos).magnitude <= tacticalLevel.TacticalLevelControllerDef.DetectionRange)
                                        {
                                            located.Add(actor);
                                        }
                                    }
                                }
                                // Returning false skips the original method.
                                return false;
                            }
                            catch (Exception e)
                            {
                                TFTVLogger.Error(e);
                                throw;
                            }
                        }

                        //────────────────────────────────────────────────────────────
                        // 2. Replacement for ReUpdateVisibilityTowardsActorImpl
                        //────────────────────────────────────────────────────────────
                        [HarmonyPrefix]
                        [HarmonyPatch("ReUpdateVisibilityTowardsActorImpl")]
                        public static bool ReUpdateVisibilityTowardsActorImplPrefix(
                            TacticalFactionVision __instance,
                            TacticalActorBase fromActor,
                            TacticalActorBase targetActor,
                            float basePerceptionRange,
                            bool notifyChange,
                            ref bool __result)
                        {
                            try
                            {
                                //if the viewing actor is evaced, early exit to fix Vanilla bug
                                if (fromActor is TacticalActor tacticalActor && tacticalActor.IsEvacuated)
                                {
                                    __result = false;
                                    return false;
                                }

                                if (targetActor == null || !targetActor.InPlay)
                                {
                                    return true; // let OG handle; it's safer during enter-play
                                }


                                int mistSymbiosisLevel = CheckForAnuBlimpMistModule(fromActor.TacticalFaction);


                                // If Aircratf rework is off/the module is not present/Priest is not researched, use og method
                                if (mistSymbiosisLevel == 0 || _blimpPriestResearch == 0)
                                {
                                    return true;
                                }

                                //If viewing faction is not Phoenix, use og method
                                if (!fromActor.TacticalFaction.TacticalFactionDef.MatchesShortName("px"))
                                {
                                    return true;
                                }

                                //If viewing actor is dead, early exit as per OG method
                                if (fromActor.IsDead)
                                {
                                    __result = false;
                                    return false;
                                }

                                //condition to reveal targetactor to viewingfaction
                                bool condition = false;

                                //condition is true if the target actor is in mist and not Pandoran 

                                if (targetActor.TacticalPerceptionBase != null && targetActor.TacticalPerceptionBase.IsTouchingVoxel(TacticalVoxelType.Mist)
                                    && targetActor.TacticalLevel.VoxelMatrix.VoxelMatrixData.MistOwnerFactionDef != targetActor.TacticalFaction.TacticalFactionDef)
                                {

                                    // TFTVLogger.Always($"{targetActor.DisplayName} in Mist revealed to {fromActor.DisplayName}");
                                    condition = true;
                                }
                                else if (TacticalFactionVision.CheckVisibleLineBetweenActors(fromActor, fromActor.Pos, targetActor, true, null, 1, null))
                                {
                                    // TFTVLogger.Always($"{targetActor.DisplayName} revealed to {fromActor.DisplayName} because LOS");
                                    condition = true;
                                }



                                if (condition)
                                {
                                    // Call IncrementKnownCounterImpl on targetActor.
                                    MethodInfo mIncrement = AccessTools.Method(__instance.GetType(), "IncrementKnownCounterImpl");
                                    __result = (bool)mIncrement.Invoke(__instance, new object[] { targetActor, KnownState.Revealed, 1, notifyChange, null });
                                }
                                else
                                {
                                    __result = false;
                                }



                                return false;
                            }
                            catch (Exception e)
                            {
                                TFTVLogger.Error(e);
                                throw;
                            }
                        }

                        /// <summary>
                        /// This patch hides player Mutants from factions other than Pandorans when they enter Mist
                        /// Removed, because actors are still located, like Pandorans. 
                        /// </summary> 
                        //────────────────────────────────────────────────────────────
                        // 3. Replacement for OnActorMoved(TacticalActorBase)
                        //────────────────────────────────────────────────────────────
                        /*  [HarmonyPrefix]
                          [HarmonyPatch("OnActorMoved", new[] { typeof(TacticalActorBase) })]
                          public static bool OnActorMovedPrefix(TacticalFactionVision __instance, TacticalActorBase movedActor)
                          {
                              try
                              {

                                  TacticalLevelController tacticalLevel = __instance.Faction.TacticalLevel;

                                  //Should only work if movedActor is controlled by Phoenix, blimp Mist module and Priest Research are present,
                                  //and if the Faction doing the viewing isn't Pandoran 
                                  if (!AircraftReworkOn || _blimpMistPresent == 0 || _blimpPriestResearch == 0 || !movedActor.TacticalFaction.IsControlledByPlayer
                                      || tacticalLevel.VoxelMatrix.VoxelMatrixData.MistOwnerFactionDef == __instance.Faction.TacticalFactionDef)
                                  {
                                      return true;
                                  }

                                  TacticalActor tacticalActor = movedActor as TacticalActor;

                                  //only works for mutants
                                  if (tacticalActor == null || !tacticalActor.BodyState.GetArmourItems().Any(a => a.GameTags.Contains(Shared.SharedGameTags.AnuMutationTag)))
                                  {
                                      TFTVLogger.Always($"{tacticalActor.DisplayName} not a mutant, so early exit for OnActorMoved");
                                      return true;
                                  } 

                                  // Early out if we are not in turn or the actor should not be processed.
                                  if (!tacticalLevel.TurnIsPlaying ||
                                      !movedActor.InPlay ||
                                      (movedActor.Status != null && movedActor.Status.HasStatus<EvacuatedStatus>()))
                                  {
                                      return false;
                                  }

                                  // Update faction knowledge on actor movement
                                  if (movedActor.TacticalFaction == __instance.Faction)
                                  {
                                      MethodInfo methodInfoUpdateVisibilityForImpl = typeof(TacticalFactionVision).GetMethod("UpdateVisibilityForImpl", BindingFlags.Instance | BindingFlags.NonPublic);
                                      bool changed = (bool)methodInfoUpdateVisibilityForImpl.Invoke(__instance, new object[] { movedActor, tacticalLevel.TacticalLevelControllerDef.DetectionRange, true });
                                      if (changed)
                                      {
                                          tacticalLevel.FactionKnowledgeChanged(__instance.Faction);
                                      }
                                      return false;
                                  }

                                  bool flag = false;
                                  bool flag2 = false;
                                  bool flag3 = false;

                                  //Should hide moved actor if moves into Mist
                                  if (movedActor.TacticalPerceptionBase != null &&
                                      movedActor.TacticalPerceptionBase.IsTouchingVoxel(TacticalVoxelType.Mist)) 
                                  {
                                      TFTVLogger.Always($"hiding {movedActor.DisplayName} in Mist");
                                      MethodInfo methodInfoResetKnownCounterImpl = typeof(TacticalFactionVision).GetMethod("ResetKnownCounterImpl", BindingFlags.Instance | BindingFlags.NonPublic);
                                      flag2 = (bool)methodInfoResetKnownCounterImpl.Invoke(__instance, new object[] { movedActor, KnownState.Revealed, false, null });
                                  }

                                  // Process each actor in the faction.
                                  foreach (TacticalActorBase actor in __instance.Faction.Actors)
                                  {
                                      if (actor.TacticalPerceptionBase != null &&
                                         (tacticalLevel.CurrentFaction == __instance.Faction ||
                                          actor.TacticalPerceptionBase.TacticalPerceptionBaseDef.UpdateOnOthersTurn))
                                      {

                                          MethodInfo mReUpdateVis = AccessTools.Method(__instance.GetType(), "ReUpdateVisibilityTowardsActorImpl");

                                          bool res1 = (bool)mReUpdateVis.Invoke(__instance, new object[]
                                          { actor, movedActor, tacticalLevel.TacticalLevelControllerDef.DetectionRange, !flag2 });
                                          flag |= res1;

                                          MethodInfo mReUpdateHear = AccessTools.Method(__instance.GetType(), "ReUpdateHearingImpl");

                                          //  TFTVLogger.Always($"mReUpdateHear null? {mReUpdateHear == null}");

                                          bool res2 = (bool)mReUpdateHear.Invoke(__instance, new object[] { actor, movedActor, true });
                                          flag3 |= res2;
                                      }
                                  }
                                  if ((flag ^ flag2) || flag3)
                                  {
                                      tacticalLevel.FactionKnowledgeChanged(__instance.Faction);
                                  }
                                  return false;
                              }
                              catch (Exception e)
                              {
                                  TFTVLogger.Error(e);
                                  throw;
                              }
                          }*/







                    }
                }
                internal class CaptureDrones
                {

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
                                        _captureDronesPresent = 1;
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

                                if (_captureDronesPresent > 0)
                                {
                                    mission.MissionDef.DontRecoverItems = true;
                                    // CaptureDronesModulePresent = false;
                                }
                            }
                            catch (Exception e)
                            {
                                TFTVLogger.Error(e);
                                throw;
                            }
                        }
                    }

                    /*  [HarmonyPatch(typeof(GeoMission), "GetItemsOnTheGround")]
                      public static class GeoMission_GetItemsOnTheGround2_Patch
                      {
                          public static void Postfix(GeoMission __instance, TacMissionResult result, ref IEnumerable<GeoItem> __result)
                          {
                              try
                              {


                                  if (!AircraftReworkOn)
                                  {
                                      return;
                                  }

                                  TFTVLogger.Always($"__result==null? {__result==null}");

                                  foreach(GeoItem geoItem in __result)
                                  {
                                      TFTVLogger.Always($"item on the ground: {geoItem?.ItemDef?.name}");
                                  }

                              }
                              catch (Exception e)
                              {
                                  TFTVLogger.Error(e);
                                  throw;

                              }
                          }

                      }*/






                    [HarmonyPatch(typeof(GeoMission), "ManageGear")]
                    public static class GeoMission_ManageGear_Patch
                    {
                        public static void Prefix(GeoMission __instance, TacMissionResult result, GeoSquad squad, out bool __state)
                        {
                            try
                            {
                                __state = false;

                                if (!AircraftReworkOn)
                                {
                                    return;
                                }


                                if (_captureDronesPresent > 0 && __instance.MissionDef.DontRecoverItems)
                                {
                                    TFTVLogger.Always($"got here; salvage drone module changing missionDef {__instance.MissionDef.name} to recover items");

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



                                if (_captureDronesPresent > 0)
                                {
                                    TFTVLogger.Always($"changing missionDef {__instance.MissionDef.name} back to not recover items");
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


                    [HarmonyPatch(typeof(TacticalLevelController), "GetMissionResult")]
                    internal static class TacticalLevelController_GetMissionResult_Prefix
                    {
                        // Prefix fully replaces original; __result is set and we return false.
                        static void Prefix(TacticalLevelController __instance)
                        {
                            try
                            {
                                //  TFTVLogger.Always($"Running GetMissionResult");

                                foreach (TacticalActorBase actor in __instance.Map.GetActors<TacticalActorBase>().
                                    Where(tab => tab is CrateItemContainer crateItemContainer && crateItemContainer.GetComponent<CrateComponent>() != null
                                    && !crateItemContainer.GetComponent<CrateComponent>().IsOpen()))
                                {
                                    // TFTVLogger.Always($"Unopened crate: {actor?.name}");

                                    CrateItemContainer crate = actor as CrateItemContainer;

                                    TFTVLogger.Always($"container not open, contains {actor?.Inventory?.Items?.Count} items");
                                    actor.Inventory.Items.Clear();
                                    TFTVLogger.Always($"emptied! new count: {actor?.Inventory?.Items?.Count} items");

                                }


                            }
                            catch (Exception e)
                            {
                                TFTVLogger.Error(e);

                            }
                        }

                    }

                    /*      [HarmonyPatch(typeof(GeoMission), "GetItemsOnTheGround")]
                      internal static class GeoMission_GetItemsOnTheGround_Prefix
                      {
                          // Prefix fully replaces original; __result is set and we return false.
                          static bool Prefix(GeoMission __instance, TacMissionResult result, ref IEnumerable<GeoItem> __result)
                          {
                              try
                              {
                                  var site = __instance.Site;
                                  var level = site.GeoLevel;

                                  // Re-derive the private ManufactureTag the original uses
                                  var manufactureTag = __instance.GameController
                                      .GetComponent<SharedData>()
                                      .SharedGameTags
                                      .ManufacturableTag;

                                  // Alien "items" tag used to exclude alien-only gear
                                  var alienItemsRaceTag = level.AlienFaction.FactionDef.RaceTagDef;

                                  // This is how the original gets the Environment faction's results
                                  var envResult = result.GetResultByFacionDef(level.EnvironmentFactionDef.PPFactionDef);

                                  // Defensive guard
                                  if (envResult == null)
                                  {
                                      TFTVLogger.Always("[TFTV] GetItemsOnTheGround: No environment faction result found.");
                                      __result = Enumerable.Empty<GeoItem>();
                                      return false;
                                  }

                                  // Pull every ItemContainerResult the mission produced for the environment faction
                                  var containers = envResult.UnitResults
                                      .Select(u => u.Data)
                                      .OfType<ItemContainerResult>()
                                      .ToList();

                                  // ---- Custom logging per ItemContainerResult (requested) ----
                                  // We don't know extra metadata on ItemContainerResult beyond InventoryItems,
                                  // so we log index + counts + a few item names for quick inspection.
                                  for (int i = 0; i < containers.Count; i++)
                                  {


                                      var c = containers[i];


                                      int count = c?.InventoryItems?.Count() ?? 0;
                                      var sampleNames = (c?.InventoryItems ?? Enumerable.Empty<ItemData>())
                                          .Take(5)
                                          .Select(it => it?.ItemDef?.name ?? "<null>")
                                          .ToArray();



                                      TFTVLogger.Always($"[TFTV] ItemContainerResult[{i}] -> items: {count}, sample: {string.Join(", ", sampleNames)}");
                                  }
                                  // ------------------------------------------------------------

                                  var picked = new List<GeoItem>();

                                  foreach (var container in containers)
                                  {
                                      TFTVLogger.Always($"container count: {container.InventoryItems.Count}");

                                      if (container.InventoryItems == null)
                                          continue;

                                      foreach (var invItem in container.InventoryItems)
                                      {
                                          // Mirror original filters:
                                          // 1) Must be manufacturable (has manufactureTag)
                                          // 2) Must NOT be alien-only (own-tags include alienItemsRaceTag)
                                          // 3) Exclude permanent augments (permanent TacticalItemDef augments)
                                          bool isManufacturable = (invItem.ItemDef.Tags?.Contains(manufactureTag) ?? false);
                                          bool isAlienOnly = (invItem.OwnTags?.Contains(alienItemsRaceTag) ?? false);

                                          bool isPermanentAugment =
                                              invItem.ItemDef is TacticalItemDef tItem &&
                                              tItem.IsPermanentAugment;

                                          if (isManufacturable && !isAlienOnly && !isPermanentAugment)
                                          {
                                              picked.Add(new GeoItem(invItem));
                                          }
                                          else
                                          {
                                              // Detailed log on excluded items to help validate filters
                                              TFTVLogger.Always($"[TFTV] Excluded ground item '{invItem?.ItemDef?.name}': " +
                                                        $"Manufacturable={isManufacturable}, AlienOnly={isAlienOnly}, PermanentAugment={isPermanentAugment}");
                                          }
                                      }
                                  }

                                  __result = picked;
                                  return false; // skip original
                              }
                              catch (Exception e)
                              {
                                  TFTVLogger.Always($"[TFTV] GetItemsOnTheGround Prefix failed: {e}");
                                  // On error, let original run to avoid breaking gameplay
                                  return true;
                              }
                          }
                      }*/

                }
                //Helios:
                internal class HeliosStatisChamber
                {

                    internal static void ImplementPanaceaNanotechTactical(TacticalLevelController controller)
                    {
                        try
                        {
                            if (_heliosNanotechPresent == 0)
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

                    private static void ResetVestDefs()
                    {
                        try
                        {
                            NanoVestStatusDef.StatsModifications[0].Value = 10;
                            NanoVestStatusDef.StatsModifications[1].Value = 10;

                            foreach (DamageMultiplierAbilityDef damageMultiplierAbilityDef in VestResistanceMultiplierAbilities)
                            {
                                damageMultiplierAbilityDef.Multiplier = 0.5f;
                            }

                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }

                    private static void SetVestDefsForVestBuff()
                    {
                        try
                        {
                            NanoVestStatusDef.StatsModifications[0].Value = 15;
                            NanoVestStatusDef.StatsModifications[1].Value = 15;
                            foreach (DamageMultiplierAbilityDef damageMultiplierAbilityDef in VestResistanceMultiplierAbilities)
                            {
                                damageMultiplierAbilityDef.Multiplier = 0.25f;
                                TFTVLogger.Always($"Setting {damageMultiplierAbilityDef.name} multiplier to {damageMultiplierAbilityDef.Multiplier} for Helios vest buff");
                            }
                            TFTVLogger.Always($"NanoVest buffs: {NanoVestStatusDef.StatsModifications[0].Value}, {NanoVestStatusDef.StatsModifications[1].Value}");
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }



                    internal static void ImplementVestBuff()
                    {
                        try
                        {
                            if (!AircraftReworkOn || _heliosVestBuff == 0)
                            {
                                ResetVestDefs();
                                return;
                            }

                            SetVestDefsForVestBuff();

                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }


                    [HarmonyPatch(typeof(DamageOverTimeStatus), "OnApply")]
                    public static class DamageOverTimeStatus_OnApply_Patch
                    {
                        public static void Postfix(DamageOverTimeStatus __instance, StatusComponent statusComponent)
                        {
                            try
                            {
                                if (!AircraftReworkOn || _heliosNanotechPresent == 0)
                                {
                                    return;
                                }

                                TacticalActor tacticalActor = __instance.TacticalActor;

                                DamageOverTimeResistanceStatusDef nanotechStatus = DefCache.GetDef<DamageOverTimeResistanceStatusDef>("NanoTech_StatusDef");

                                if (nanotechStatus.StatusDefs.Contains(__instance.DamageOverTimeStatusDef))
                                {
                                    if (tacticalActor != null)
                                    {
                                        if (tacticalActor.HasStatus(nanotechStatus))
                                        {
                                            DamageOverTimeResistanceStatus status = (DamageOverTimeResistanceStatus)tacticalActor.Status.GetStatusByName(nanotechStatus.EffectName);
                                            tacticalActor.Status.UnapplyStatus(status);
                                        }
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

                    [HarmonyPatch(typeof(FireStatus), "CalculateFireDamage")]
                    public static class FireStatus_CalculateFireDamage_Patch
                    {
                        public static void Postfix(FireStatus __instance)
                        {
                            try
                            {
                                if (!AircraftReworkOn || _heliosNanotechPresent == 0)
                                {
                                    return;
                                }

                                TacticalActor tacticalActor = __instance.TacticalActor;
                                DamageOverTimeResistanceStatusDef nanotechStatus = DefCache.GetDef<DamageOverTimeResistanceStatusDef>("NanoTech_StatusDef");


                                if (tacticalActor != null)
                                {
                                    if (tacticalActor.HasStatus(nanotechStatus))
                                    {
                                        DamageOverTimeResistanceStatus status = (DamageOverTimeResistanceStatus)tacticalActor.Status.GetStatusByName(nanotechStatus.EffectName);
                                        tacticalActor.Status.UnapplyStatus(status);
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

                    /*  [HarmonyPatch(typeof(DamageOverTimeResistanceStatus), "ApplyResistance")]
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
                      }*/
                }
                //Mist repeller effects

                internal class MistRepeller
                {

                    [HarmonyPatch(typeof(TacticalLevelController), "OnLevelStart")]//OnLevelStateChanged")]
                    public static class OnLevelStart_Patch
                    {
                        public static void Postfix(TacticalLevelController __instance) //Level.State prevState, Level.State state, )
                        {
                            try
                            {
                                if (!AircraftReworkOn)
                                {
                                    return;
                                }

                                if (_mistRepellerPresent > 0)
                                {
                                    TFTVLogger.Always("Mist repeller module present");

                                    __instance.TacticalGameParams.IsCorruptionActive = false;
                                }


                            }
                            catch (Exception e)
                            {
                                TFTVLogger.Error(e);
                                throw;
                            }
                        }
                    }

                    public static void ImplementMistRepellerTurnStart(TacticalVoxelMatrix tacticalVoxelMatrix, TacticalVoxel[] _voxels)
                    {
                        try
                        {
                            if (!AircraftReworkOn || _mistRepellerPresent == 0)
                            {
                                return;
                            }


                            var mistVoxels = _voxels.Where(v => v != null && v.GetVoxelType() == TacticalVoxelType.Mist).ToList();

                            // Calculate the number of voxels to remove
                            int voxelsToRemove = mistVoxels.Count / 2;

                            TFTVLogger.Always($"Activating Mist Repeller! Current mist voxels: {mistVoxels.Count}. Mist voxels to remove {voxelsToRemove}");

                            // Shuffle the list to randomize which voxels are removed
                            mistVoxels = mistVoxels.OrderBy(v => UnityEngine.Random.value).ToList();

                            // Remove mist from half of the voxels
                            for (int i = 0; i < voxelsToRemove; i++)
                            {
                                mistVoxels[i].SetVoxelType(TacticalVoxelType.Empty);
                            }

                            // Update the voxel matrix to reflect the changes
                            tacticalVoxelMatrix.UpdateVoxelMatrix();

                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
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

                            if (AircraftReworkOn && _heliosPresent > 0)
                            {
                                //   TFTVLogger.Always($"got here, {__instance.SurviveTurns}");

                                __instance.SurviveTurns -= 1;
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
                            if (AircraftReworkOn && _heliosPresent > 0)
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
            }
            internal class MissionDeployment
            {

                [HarmonyPatch(typeof(UIStateRosterDeployment), "OnEnrollmentChanged")]
                public static class UIStateRosterDeployment_OnEnrollmentChanged_Patch
                {

                    private static void NoVehicleMutogWarning(UIStateRosterDeployment __instance, GeoRosterDeploymentItem item, MessageBox ____confirmationBox, List<GeoCharacter> ____selectedDeployment, List<GeoRosterDeploymentItem> ____deploymentItems)
                    {
                        try
                        {
                            UIModuleDeploymentMissionBriefing missionBriefingModule = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.DeploymentMissionBriefingModule;
                            MethodInfo methodInfoCheckForDeployment = AccessTools.Method(typeof(UIStateRosterDeployment), "CheckForDeployment", new Type[] { typeof(IEnumerable<GeoCharacter>) });

                            item.EnrollForDeployment = !item.EnrollForDeployment;
                            item.RefreshCheckVisuals();

                            ____selectedDeployment.Clear();
                            ____selectedDeployment.AddRange(from s in ____deploymentItems
                                                            where s.EnrollForDeployment
                                                            select s.Character);

                            methodInfoCheckForDeployment.Invoke(__instance, new object[] { ____selectedDeployment });
                        }

                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }

                    }

                    public static bool Prefix(UIStateRosterDeployment __instance, GeoRosterDeploymentItem item, MessageBox ____confirmationBox, List<GeoCharacter> ____selectedDeployment, List<GeoRosterDeploymentItem> ____deploymentItems)
                    {
                        try
                        {
                            TFTVConfig config = TFTVMain.Main.Config;

                            if (!item.Character.TemplateDef.IsVehicle && !item.Character.TemplateDef.IsMutog)
                            {
                                return true;
                            }

                            if (config.MultipleVehiclesInAircraftAllowed)
                            {
                                NoVehicleMutogWarning(__instance, item, ____confirmationBox, ____selectedDeployment, ____deploymentItems);
                                return false;
                            }
                            else
                            {
                                if (!AircraftReworkOn)
                                {
                                    return true;
                                }
                            }


                            if (__instance.Mission.MissionDef.Tags.Contains(DefCache.GetDef<MissionTypeTagDef>("MissionTypeAncientSiteDefense_MissionTagDef"))
                               || __instance.Mission.MissionDef.Tags.Contains(DefCache.GetDef<MissionTypeTagDef>("MissionTypePhoenixBaseDefence_MissionTagDef")))
                            {
                                return true;
                            }


                            GeoVehicle geoVehicle = __instance.Mission.Site.GetPlayerVehiclesOnSite()?.FirstOrDefault(v => v.Units.Contains(item.Character));

                            if (geoVehicle == null)
                            {
                                return true;
                            }

                            bool hasHarness = geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _vehicleHarnessModule);
                            bool hasMutogPen = geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _blimpMutogPenModule);
                            bool isThunderbird = geoVehicle.VehicleDef == thunderbird;

                            if ((hasHarness && item.Character.TemplateDef.IsVehicle) || (hasMutogPen && item.Character.TemplateDef.IsMutog))
                            {

                            }
                            else
                            {
                                return true;
                            }

                            NoVehicleMutogWarning(__instance, item, ____confirmationBox, ____selectedDeployment, ____deploymentItems);

                            return false;

                        }



                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }
                }




                [HarmonyPatch(typeof(UIStateRosterDeployment), "CheckForDeployment")]
                public static class UIStateRosterDeployment_CheckForDeployment_Patch
                {
                    public static bool Prefix(UIStateRosterDeployment __instance, IEnumerable<GeoCharacter> squad, GeoMission ____mission)
                    {
                        try
                        {
                            TFTVConfig config = TFTVMain.Main.Config;
                            UIModuleDeploymentMissionBriefing missionBriefingModule = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.DeploymentMissionBriefingModule;

                            if (AircraftReworkOn)
                            {

                                missionBriefingModule.DeployButton.SetInteractable(squad.Any());
                                missionBriefingModule.DeployButton.ResetButtonAnimations();

                                missionBriefingModule.SquadSlotsUsedText.text = "";
                                return false;
                            }

                            if (config.MultipleVehiclesInAircraftAllowed)
                            {
                                int maxUnits = ____mission.MissionDef.MaxPlayerUnits;

                                if (config.UnLimitedDeployment)
                                {
                                    maxUnits = 99;
                                }
                                bool flag = squad.Any();
                                int num = squad.Sum((GeoCharacter s) => s.OccupingSpace);
                                // int num2 = squad.Count((GeoCharacter c) => c.TemplateDef.IsVehicle || c.TemplateDef.IsMutog);
                                missionBriefingModule.SetCurrentDeployment(num, maxUnits);
                                bool flag2 = num <= maxUnits;
                                //  bool flag3 = num2 < 2;
                                missionBriefingModule.DeployButton.SetInteractable(flag && flag2);
                                missionBriefingModule.DeployButton.ResetButtonAnimations();
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

                            if (__instance.MissionDef.Tags.Contains(DefCache.GetDef<MissionTypeTagDef>("MissionTypeAncientSiteDefense_MissionTagDef"))
                                || __instance.MissionDef.Tags.Contains(DefCache.GetDef<MissionTypeTagDef>("MissionTypePhoenixBaseDefence_MissionTagDef")))
                            {
                                return;
                            }

                            if (priorityContainer == null)
                            {
                                // Debug.LogError($"no vehicle was passed from sources!");
                                // __result = new List<IGeoCharacterContainer>();

                                priorityContainer = __instance.Site.Vehicles.FirstOrDefault((GeoVehicle v) => v.Owner == faction && v.Units.Count() > 0);

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
            internal class Geoscape
            {
                public static void AddAbilityToGeoVehicle(GeoVehicle geoVehicle, GeoAbilityDef geoAbility)
                {
                    try
                    {

                        geoVehicle.AddAbility(geoAbility, geoVehicle);
                        TFTVLogger.Always($"Added {geoAbility.name} to {geoVehicle.Name}");

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
                                    if (AircraftReworkOn)
                                    {
                                        RemoveExtraVehicleOrMutog(geoVehicle);
                                        AdjustPassengerManifestAircraftRework(geoVehicle);
                                    }
                                    else
                                    {

                                        //  TFTVLogger.Always($"{geoVehicle.Name} used capacity {geoVehicle.UsedCharacterSpace} max cap {geoVehicle.MaxCharacterSpace}");

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

                    private static void RemoveExtraVehicleOrMutog(GeoVehicle geoVehicle)
                    {
                        try

                        {
                            int countVehicles = geoVehicle.Units.Where(c => c.GameTags.Any(t => t == Shared.SharedGameTags.VehicleTag)).Count();
                            int countMutogs = geoVehicle.Units.Where(c => c.GameTags.Any(t => t == Shared.SharedGameTags.MutogTag)).Count();

                            bool hasHarness = geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _vehicleHarnessModule);
                            bool hasMutogPen = geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _blimpMutogPenModule);
                            bool isThunderbird = geoVehicle.VehicleDef == thunderbird;

                            // TFTVLogger.Always($"{geoVehicle.Name} has {countVehicles} vehicles, {geoCharacter.DisplayName}, has harness: {hasHarness} is thunderbird {thunderbird}");

                            if (countVehicles > 1)
                            {
                                if (isThunderbird && hasHarness && countVehicles < 3)
                                {

                                }
                                else
                                {
                                    GeoCharacter geoCharacter = geoVehicle.Units.FirstOrDefault(c => c.GameTags.Any(t => t == Shared.SharedGameTags.VehicleTag));
                                    geoVehicle.RemoveCharacter(geoCharacter);
                                    geoVehicle.CurrentSite.AddCharacter(geoCharacter);

                                }
                            }

                            if (countMutogs > 1)
                            {
                                if (hasMutogPen && countMutogs < 2)
                                {

                                }
                                else
                                {

                                    GeoCharacter geoCharacter = geoVehicle.Units.FirstOrDefault(c => c.GameTags.Any(t => t == Shared.SharedGameTags.MutogTag));
                                    geoVehicle.RemoveCharacter(geoCharacter);
                                    geoVehicle.CurrentSite.AddCharacter(geoCharacter);

                                }
                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }




                    }


                    private static void AdjustPassengerManifestAircraftRework(GeoVehicle geoVehicle)
                    {
                        try
                        {
                            bool hasHarness = geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _vehicleHarnessModule);
                            bool hasMutogPen = geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _blimpMutogPenModule);
                            bool isThunderbird = geoVehicle.VehicleDef == thunderbird;

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

                            if (occupiedSpace >= geoVehicle.MaxCharacterSpace)
                            {
                                List<GeoCharacter> list = new List<GeoCharacter>(from u in geoVehicle.Units orderby u.OccupingSpace descending select u);
                                foreach (GeoCharacter character in list)
                                {
                                    if (occupiedSpace <= geoVehicle.MaxCharacterSpace)
                                    {
                                        break;
                                    }
                                    geoVehicle.RemoveCharacter(character);
                                    geoVehicle.CurrentSite.AddCharacter(character);

                                    if (character.TemplateDef.Volume == 3 && hasHarness)
                                    {
                                        occupiedSpace -= 1;
                                    }
                                    else if (character.TemplateDef.Volume == 2 && hasMutogPen)
                                    {
                                        occupiedSpace -= 1;
                                    }
                                    else if (character.TemplateDef.Volume == 3 && isThunderbird)
                                    {
                                        occupiedSpace -= 2;
                                    }
                                    else
                                    {
                                        occupiedSpace -= character.TemplateDef.Volume;
                                    }

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
                internal class Scanning
                {






                    [HarmonyPatch(typeof(GeoPhoenixFaction), "OnVehicleAdded")]
                    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
                    public static class GeoPhoenixFaction_OnVehicleAdded_Patch
                    {
                        static void Postfix(GeoPhoenixFaction __instance, GeoVehicle vehicle)
                        {
                            try
                            {
                                if (!AircraftReworkOn)
                                {
                                    return;
                                }

                                CheckAircraftScannerAbility(vehicle);
                                TFTVLogger.Always($"scanner ability added to {vehicle.Name}");
                            }
                            catch (Exception e)
                            {
                                TFTVLogger.Error(e);
                                throw;
                            }
                        }
                    }

                    [HarmonyPatch(typeof(GeoAbility), "GetAbilityFaction")]
                    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
                    public static class GeoAbility_GetTargetDisabledState_Patch
                    {
                        static void Postfix(GeoAbility __instance, ref GeoFaction __result)
                        {
                            try
                            {
                                if (!AircraftReworkOn || __instance.GeoscapeAbilityDef != _scanAbilityDef && __instance.GeoscapeAbilityDef != _thunderbirdScanAbilityDef)
                                {
                                    return;
                                }

                                GeoVehicle geoVehicle = __instance.Actor as GeoVehicle;

                                __result = geoVehicle.Owner;

                            }
                            catch (Exception e)
                            {
                                TFTVLogger.Error(e);
                                throw;
                            }
                        }
                    }


                    [HarmonyPatch(typeof(GeoAbilityView), "CanActivate")]
                    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
                    public static class GeoAbilityView_CanActivate_Patch
                    {
                        static void Postfix(GeoAbilityView __instance, GeoAbilityTarget target, ref bool __result)
                        {
                            try
                            {
                                if (!AircraftReworkOn || __instance.GeoAbility.GeoscapeAbilityDef != _scanAbilityDef && __instance.GeoAbility.GeoscapeAbilityDef != _thunderbirdScanAbilityDef)
                                {
                                    return;
                                }

                                GeoVehicle geoVehicle = __instance.GeoAbility.Actor as GeoVehicle;

                                if (target.Actor is GeoSite geoSite && geoVehicle.CurrentSite == geoSite && geoVehicle.CanRedirect && __result)
                                {

                                }
                                else
                                {
                                    __result = false;
                                }
                            }
                            catch (Exception e)
                            {
                                TFTVLogger.Error(e);
                                throw;
                            }
                        }
                    }



                    [HarmonyPatch(typeof(UIModuleActionsBar), "UpdateAbilityInformation")]
                    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
                    public static class UIModuleActionsBar_UpdateAbilityInformation_Patch
                    {
                        static void Postfix(UIModuleActionsBar __instance, GeoAbility geoAbility, bool showAbilityState)
                        {
                            try
                            {
                                if (!AircraftReworkOn)
                                {
                                    return;
                                }

                                if (geoAbility.GeoscapeAbilityDef != _scanAbilityDef && geoAbility.GeoscapeAbilityDef != _thunderbirdScanAbilityDef)
                                {
                                    return;
                                }

                                __instance.MainDescriptionController.ActionHeaderChargesText.gameObject.SetActive(value: false);
                                __instance.MainDescriptionController.CallToActionButton.gameObject.SetActive(value: false);
                                //__instance.MainDescriptionController.SuppliesText.gameObject.SetActive(false);

                            }
                            catch (Exception e)
                            {
                                TFTVLogger.Error(e);
                                throw;
                            }
                        }
                    }


                    [HarmonyPatch(typeof(ScanAbility), "GetCharges")]
                    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
                    public static class ScanAbility_GetCharges_Patch
                    {
                        static void Postfix(ScanAbility __instance, ref int __result)
                        {
                            try
                            {
                                if (!AircraftReworkOn)
                                {
                                    return;
                                }

                                __result = 1;

                            }
                            catch (Exception e)
                            {
                                TFTVLogger.Error(e);
                                throw;
                            }
                        }
                    }


                    [HarmonyPatch(typeof(ScanAbility), "GetDisabledStateInternal")]
                    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
                    public static class ScanAbility_GetDisabledStateInternal_Patch
                    {
                        static void Postfix(ScanAbility __instance, ref GeoAbilityDisabledState __result)
                        {
                            try
                            {
                                if (!AircraftReworkOn)
                                {
                                    return;
                                }

                                GeoVehicle geoVehicle = __instance.Actor as GeoVehicle;

                                if (geoVehicle != null && !geoVehicle.CanRedirect)
                                {
                                    __result = GeoAbilityDisabledState.NoScanChargesLeft;
                                }

                            }
                            catch (Exception e)
                            {
                                TFTVLogger.Error(e);
                                throw;
                            }
                        }
                    }

                    /* [HarmonyPatch(typeof(ScanAbility), "GetTargetDisabledStateInternal")]
                     [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
                     public static class ScanAbility_GetTargetDisabledStateInternal_Patch
                     {
                         static void Postfix(ScanAbility __instance, GeoAbilityTarget target, GeoAbilityTargetDisabledState __result)
                         {
                             try
                             {
                                 if (!AircraftReworkOn)
                                 {
                                     return;
                                 }

                                 GeoVehicle geoVehicle = __instance.Actor as GeoVehicle;

                                 TFTVLogger.Always($"{geoVehicle.Name} target: {target.Actor?.name} {__result} ");

                             }
                             catch (Exception e)
                             {
                                 TFTVLogger.Error(e);
                                 throw;
                             }
                         }
                     }*/

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

                    private static float GetThunderbirdScannerRange()
                    {
                        try
                        {
                            Research phoenixResearch = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction.Research;

                            if (phoenixResearch.HasCompleted("NJ_SateliteUplink_ResearchDef"))
                            {
                                return 3000;
                            }
                            else
                            {
                                return 2000;
                            }

                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }

                    }

                    private static void AdjustArgusArrayRange()
                    {
                        try
                        {


                            GeoScannerDef geoScannerDef = (GeoScannerDef)_thunderbirdScanAbilityDef.ScanActorDef.Components.FirstOrDefault(c => c is GeoScannerDef);

                            geoScannerDef.MaximumRange.Value = GetThunderbirdScannerRange();



                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }

                    }



                    [HarmonyPatch(typeof(ScanAbility), "ActivateInternal")]
                    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
                    public static class ScanAbility_ActivateInternal_Patch
                    {
                        static void Prefix(ScanAbility __instance)
                        {
                            try
                            {
                                if (!AircraftReworkOn)
                                {
                                    return;
                                }

                                AdjustArgusArrayRange();

                                GeoVehicle geoVehicle = __instance.Actor as GeoVehicle;

                                if (AircraftScanningSites == null)
                                {
                                    AircraftScanningSites = new Dictionary<int, List<int>>();
                                }


                                if (!AircraftScanningSites.ContainsKey(geoVehicle.VehicleID))
                                {
                                    AircraftScanningSites.Add(geoVehicle.VehicleID, new List<int>());
                                    TFTVLogger.Always($"{geoVehicle?.Name} started scan!");
                                }


                            }
                            catch (Exception e)
                            {
                                TFTVLogger.Error(e);
                                throw;
                            }
                        }


                        static void Postfix(ScanAbility __instance)
                        {
                            try
                            {
                                if (!AircraftReworkOn)
                                {
                                    return;
                                }

                                GeoVehicle geoVehicle = __instance.Actor as GeoVehicle;
                                geoVehicle.CanRedirect = false;


                            }
                            catch (Exception e)
                            {
                                TFTVLogger.Error(e);
                                throw;
                            }
                        }
                    }


                    [HarmonyPatch(typeof(GeoScanner), "CompleteScan")]
                    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
                    public static class GeoScanner_CompleteScan_Patch
                    {
                        static void Prefix(GeoScanner __instance)
                        {
                            try
                            {
                                if (!AircraftReworkOn)
                                {
                                    return;
                                }

                                GeoVehicle geoVehicle = __instance?.Location?.Vehicles?.FirstOrDefault(v => v.IsOwnedByViewer && AircraftScanningSites.ContainsKey(v.VehicleID) &&
                                 (v.Modules.Any(m => m != null && m.ModuleDef == _thunderbirdScannerModule) || v.Modules.Any(m => m != null && m.ModuleDef == _basicScannerModule)) && !v.CanRedirect);

                                if (geoVehicle == null)
                                {
                                    return;
                                }

                                geoVehicle.CanRedirect = true;

                                AircraftScanningSites.Remove(geoVehicle.VehicleID);

                                TFTVLogger.Always($"{geoVehicle.Name} finished scan!");
                            }
                            catch (Exception e)
                            {
                                TFTVLogger.Error(e);
                                throw;
                            }
                        }
                    }

                    public static Dictionary<int, List<int>> AircraftScanningSites = new Dictionary<int, List<int>>();


                    [HarmonyPatch(typeof(GeoScanComponent), "DetectSite")]
                    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
                    public static class GeoScanComponent_DetectSite_Patch
                    {

                        static bool Prefix(GeoScanComponent __instance, GeoSite site, GeoActor ____actor)
                        {
                            try
                            {
                                if (!AircraftReworkOn)
                                {
                                    return true;
                                }

                                GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                                if (controller == null || controller.PhoenixFaction == null || controller.PhoenixFaction.Research == null)
                                {
                                    return true;
                                }

                                Research phoenixResearch = controller.PhoenixFaction.Research;

                                if (__instance.ScanDef == _thunderbirdScannerComponent && site.Type == GeoSiteType.AlienBase)
                                {
                                    if (phoenixResearch.HasCompleted("NJ_SateliteUplink_ResearchDef") && phoenixResearch.HasCompleted("PX_Alien_Citadel_ResearchDef"))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        return false;
                                    }
                                }


                                if (__instance.ScanDef == _basicScannerComponent ||
                                    __instance.ScanDef == _thunderbirdScannerComponent && !phoenixResearch.HasCompleted("NJ_NeuralTech_ResearchDef"))
                                {

                                    GeoScanner scanner = (GeoScanner)____actor;


                                    if (scanner == null)
                                    {
                                        TFTVLogger.Always($"scanner is null! This is unexpected");
                                        return false;
                                    }

                                    GeoSite geoSite = scanner.Location;

                                    if (geoSite == null)
                                    {
                                        TFTVLogger.Always($"geoSite is null! This is unexpected");
                                        return false;
                                    }

                                    if (AircraftScanningSites == null)
                                    {
                                        AircraftScanningSites = new Dictionary<int, List<int>>();
                                    }

                                    GeoVehicle geoVehicle = geoSite.Vehicles.FirstOrDefault(v => v.IsOwnedByViewer && AircraftScanningSites.ContainsKey(v.VehicleID) &&
                                     (v.Modules.Any(m => m != null && m.ModuleDef == _thunderbirdScannerModule) || v.Modules.Any(m => m != null && m.ModuleDef == _basicScannerModule)));

                                    if (geoVehicle == null)
                                    {
                                        TFTVLogger.Always($"no geo vehicle found in _aircraftScanningSites! This is unexpected");
                                        return false;
                                    }
                                    else
                                    {
                                        if (!AircraftScanningSites[geoVehicle.VehicleID].Contains(site.SiteId))
                                        {
                                            AircraftScanningSites[geoVehicle.VehicleID].Add(site.SiteId);
                                        }
                                        else
                                        {
                                            //  TFTVLogger.Always($"site {site.name} already scanned by {geoVehicle.Name}, not rolling again");
                                            return false;
                                        }
                                    }

                                    int chance = 50;

                                    if (__instance.ScanDef == _thunderbirdScannerComponent)
                                    {
                                        chance = 75;
                                    }

                                    UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                                    int num = UnityEngine.Random.Range(0, 100);
                                    if (num < chance)
                                    {
                                        TFTVLogger.Always($"rolled {num} Not revealing {site?.name}");
                                        return false;
                                    }

                                    TFTVLogger.Always($"rolled {num} revealing {site?.name}");
                                }

                                return true;

                            }
                            catch (Exception e)
                            {
                                TFTVLogger.Error(e);
                                throw;
                            }
                        }


                        static void Postfix(GeoScanComponent __instance, GeoSite site, GeoFaction owner)
                        {
                            try
                            {
                                if (!AircraftReworkOn || site == null)
                                {
                                    return;
                                }

                                GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                                if (controller == null || controller.PhoenixFaction == null || controller.PhoenixFaction.Research == null)
                                {
                                    return;
                                }

                                Research phoenixResearch = controller.PhoenixFaction.Research;

                                // TFTVLogger.Always($"owner null? {owner==null}");

                                /* if (__instance.ScanDef == _thunderbirdScannerComponent && site.Type == GeoSiteType.Haven && !site.GetInspected(__instance.Owner))
                                 {
                                     site.SetInspected(owner, inspected: true);
                                 }*/

                                if (__instance.ScanDef == _thunderbirdScannerComponent && site.Type == GeoSiteType.AlienBase && !site.GetInspected(__instance.Owner)
                                    && phoenixResearch.HasCompleted("PX_Alien_Citadel_ResearchDef") && phoenixResearch.HasCompleted("NJ_SateliteUplink_ResearchDef"))
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

                    // Harmony patch to change the reveal of alien bases when in scanner range, so increases the reveal chance instead of revealing it right away
                    [HarmonyPatch(typeof(GeoAlienFaction), "TryRevealAlienBase")]
                    internal static class BC_GeoAlienFaction_TryRevealAlienBase_patch
                    {
                        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
                        private static bool Prefix(ref bool __result, GeoSite site, GeoFaction revealToFaction, GeoLevelController ____level)
                        {
                            try
                            {

                                if (!site.GetVisible(revealToFaction))
                                {
                                    GeoAlienBase component = site.GetComponent<GeoAlienBase>();

                                    if (revealToFaction is GeoPhoenixFaction geoPhoenixFaction)
                                    {
                                        EarthUnits thunderbirdScannerRange = AircraftReworkOn ? new EarthUnits() { Value = GetThunderbirdScannerRange() } : new EarthUnits() { Value = 0 };

                                        bool anyThunderbirdScannerInRange = AircraftReworkOn && geoPhoenixFaction.Research.HasCompleted("NJ_SateliteUplink_ResearchDef") && geoPhoenixFaction.Vehicles
                                            .Any(v => v.Modules.Any(m => m != null && m.ModuleDef == _thunderbirdScannerModule) && v.CurrentSite != null
                                            && ____level.Map.SitesInRange(v.CurrentSite, thunderbirdScannerRange, true).Contains(site));

                                        if (geoPhoenixFaction.IsSiteInBaseScannerRange(site, true) || anyThunderbirdScannerInRange)
                                        {
                                            component.IncrementBaseAttacksRevealCounter();
                                            // original code:
                                            //site.RevealSite(____level.PhoenixFaction);
                                            //__result = true;
                                            //return false;
                                        }
                                    }

                                    if (component.CheckForBaseReveal())
                                    {
                                        site.RevealSite(____level.PhoenixFaction);
                                        __result = true;
                                        return false;
                                    }
                                    component.IncrementBaseAttacksRevealCounter();
                                }
                                __result = false;
                                return false; // Return without calling the original method
                            }

                            catch (Exception e)
                            {
                                TFTVLogger.Error(e);
                            }
                            throw new InvalidOperationException();
                        }
                    }

                }
                internal class Healing
                {




                    public static float GetRepairBionicsCostFactor(GeoCharacter geoCharacter)
                    {
                        try
                        {
                            if (geoCharacter.Faction.Vehicles.Any(v => v.Modules.Any(m => m != null && m.ModuleDef == _thunderbirdWorkshopModule)
                            && v.Units.Contains(geoCharacter)))
                            {
                                int buffLevel = Tiers.GetBuffLevelFromResearchDefs(_thunderbirdWorkshopBuffResearchDefs) - 1;
                                float repairCostFactor = _workshopBuffBionicRepairCostReduction * buffLevel;
                                if (buffLevel > 0)
                                {
                                    return (1 - repairCostFactor);
                                }
                            }

                            return 1f;

                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }



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

                                float healingFactor = _healingHPBase; //10
                                                                      //  float buffPerlevel = _healingBuffPerLevel; //2

                                foreach (GeoVehicle geoVehicle in __instance.Vehicles)
                                {
                                    if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _heliosPanaceaModule))
                                    {
                                        int buffLevel = Tiers.GetBuffLevelFromResearchDefs(_heliosStatisChamberBuffResearchDefs) == 4 ? 1 : 0;
                                        float healAmount = healingFactor + healingFactor * buffLevel; //10+10*(0 OR 1)*2, so 10 or 20
                                        float staminaAmount = _heliosPanaceaModule.GeoVehicleModuleBonusValue +
                                            _heliosPanaceaModule.GeoVehicleModuleBonusValue * buffLevel; //0.35f + 0.35f * 2 * 1, so 0.35 or 0.7

                                        foreach (GeoCharacter geoCharacter in geoVehicle.Soldiers)
                                        {
                                            geoCharacter.Heal(healAmount);
                                            geoCharacter.Fatigue.Stamina.AddRestrictedToMax(staminaAmount);
                                        }
                                    }


                                    if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _thunderbirdWorkshopModule))
                                    {
                                        int buffLevel = Tiers.GetBuffLevelFromResearchDefs(_thunderbirdWorkshopBuffResearchDefs) - 1; //0-2
                                        float healAmount = healingFactor + healingFactor * buffLevel; //10+10*(0-3), so 10, 20 or 30
                                        float staminaAmount = _thunderbirdWorkshopModule.GeoVehicleModuleBonusValue
                                            + _thunderbirdWorkshopModule.GeoVehicleModuleBonusValue * buffLevel; // 0.35f + 0.35f * (0-2), so 0, 0.7, or 1.4

                                        foreach (GeoCharacter geoCharacter in geoVehicle.Units)
                                        {
                                            if (geoCharacter.ArmourItems.Any(a => a.ItemDef.Tags.Contains(Shared.SharedGameTags.BionicalTag))
                                                || geoCharacter.GameTags.Contains(Shared.SharedGameTags.VehicleTag))
                                            {
                                                // TFTVLogger.Always($"{geoCharacter.DisplayName} in {geoVehicle.Name} has {geoCharacter.Health} HP, {geoCharacter.Fatigue?.Stamina} Stamina");
                                                geoCharacter.Heal(healAmount);
                                                geoCharacter.Fatigue?.Stamina?.AddRestrictedToMax(staminaAmount);
                                            }
                                        }
                                    }

                                    if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _blimpMutationLabModule))
                                    {
                                        int buffLevel = Tiers.GetBuffLevelFromResearchDefs(_blimpMutationLabModuleBuffResearches) - 1;
                                        float healAmount = healingFactor + healingFactor * buffLevel; //10 + 10 * (0-2), so 10, 20 or 30
                                        float staminaAmount = _blimpMutationLabModule.GeoVehicleModuleBonusValue +
                                            _blimpMutationLabModule.GeoVehicleModuleBonusValue * buffLevel;

                                        foreach (GeoCharacter geoCharacter in geoVehicle.Units)
                                        {
                                            /*  TFTVLogger.Always($"{geoCharacter.DisplayName}");

                                              foreach(GameTagDef gameTagDef in geoCharacter.GameTags) 
                                              {
                                                  TFTVLogger.Always($"has {gameTagDef.name}", false);

                                              }*/


                                            if (geoCharacter.ArmourItems.Any(a => a.ItemDef.Tags.Contains(Shared.SharedGameTags.AnuMutationTag))
                                                || geoCharacter.GameTags.Contains(Shared.SharedGameTags.MutogTag)
                                                || geoCharacter.GameTags.Contains(DefCache.GetDef<ClassTagDef>("Mutoid_ClassTagDef")))
                                            {
                                                if (geoCharacter.IsAlive && geoCharacter.IsInjured)
                                                {
                                                    geoCharacter.Health.AddRestrictedToMax(healAmount);
                                                }
                                                geoCharacter.Fatigue?.Stamina?.AddRestrictedToMax(staminaAmount);
                                                // TFTVLogger.Always($"{geoCharacter.DisplayName} getting {healAmount} healing, {extraStamina} stamina");
                                            }
                                        }
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
                                TFTVConfig config = TFTVMain.Main.Config;

                                if (config.VehicleAndMutogSize1)
                                {
                                    List<GeoCharacter> geoCharacters = __instance.Units.ToList();

                                    int occupiedSpace = 0;

                                    foreach (GeoCharacter geoCharacter in geoCharacters)
                                    {
                                        occupiedSpace += geoCharacter.OccupingSpace;
                                    }
                                    __result = occupiedSpace;
                                }



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
                        public static void Postfix(GeoCharacter __instance, ref int __result)
                        {
                            try
                            {
                                TFTVConfig config = TFTVMain.Main.Config;

                                if (config.VehicleAndMutogSize1)
                                {
                                    __result = 1;
                                }

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
                        public static bool Prefix(TransferActionMenuElement __instance, IGeoCharacterContainer targetContainer, GeoRosterItem targetItem)
                        {
                            try
                            {
                                TFTVConfig config = TFTVMain.Main.Config;

                                if (!AircraftReworkOn && !config.MultipleVehiclesInAircraftAllowed && !config.VehicleAndMutogSize1)
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


                                if (targetItem.Character == null || targetItem.Character.TemplateDef.Volume < 2 && !config.VehicleAndMutogSize1)
                                {
                                    return true;
                                }

                                GeoVehicle geoVehicle = targetContainer as GeoVehicle;
                                GeoCharacter geoCharacter = targetItem.Character;

                                int occupyingSpace = geoCharacter.TemplateDef.Volume;

                                if (config.VehicleAndMutogSize1)
                                {
                                    occupyingSpace = 1;
                                }


                                //  TFTVLogger.Always($"{geoCharacter.DisplayName} occupying space {occupyingSpace}");

                                PropertyInfo propertyInfo = typeof(TransferActionMenuElement).GetProperty("TargetContainer", BindingFlags.Public | BindingFlags.Instance);

                                propertyInfo.SetValue(__instance, targetContainer);

                                __instance.ContainerTextLabel.text = targetContainer.Name;
                                bool interactable = true;

                                bool hasHarness = false;
                                bool hasMutogPen = false;
                                bool isThunderbird = false;

                                if (AircraftReworkOn)
                                {
                                    hasHarness = geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _vehicleHarnessModule);
                                    hasMutogPen = geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _blimpMutogPenModule);
                                    isThunderbird = geoVehicle.VehicleDef == thunderbird;

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
                                }


                                __instance.ContainerTextLabel.text += $" [{targetContainer.CurrentOccupiedSpace}/{targetContainer.MaxCharacterSpace}]";
                                interactable = ((targetContainer.MaxCharacterSpace - targetContainer.CurrentOccupiedSpace >= occupyingSpace));

                                __instance.ErrorTooltip.gameObject.SetActive(value: false);

                                if (AircraftReworkOn)
                                {
                                    if (!CheckVehicleMutogVehicleCapacity(geoVehicle, geoCharacter, isThunderbird, hasHarness, hasMutogPen))
                                    {
                                        __instance.ErrorTooltip.TipKey = __instance.VehicleErrorTextBind;
                                        __instance.ErrorTooltip.gameObject.SetActive(value: true);
                                        interactable = false;
                                    }
                                }
                                else if (targetItem.Character.TemplateDef.Volume == 3)
                                {
                                    foreach (GeoCharacter allCharacter in targetContainer.GetAllCharacters())
                                    {
                                        if (allCharacter.TemplateDef.Volume == targetItem.Character.TemplateDef.Volume)
                                        {
                                            __instance.ErrorTooltip.TipKey = __instance.VehicleErrorTextBind;
                                            __instance.ErrorTooltip.gameObject.SetActive(value: true);
                                            interactable = false;
                                        }
                                    }
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

                    private static bool CheckVehicleMutogVehicleCapacity(GeoVehicle geoVehicle, GeoCharacter geoCharacter, bool thunderBird, bool hasHarness, bool mutogPen)
                    {
                        try
                        {
                            int countVehicles = geoVehicle.Units.Where(c => c.GameTags.Any(t => t == Shared.SharedGameTags.VehicleTag)).Count();
                            int countMutogs = geoVehicle.Units.Where(c => c.GameTags.Any(t => t == Shared.SharedGameTags.MutogTag)).Count();

                            // TFTVLogger.Always($"{geoVehicle.Name} has {countVehicles} vehicles, {geoCharacter.DisplayName}, has harness: {hasHarness} is thunderbird {thunderbird}");

                            if (geoCharacter.GameTags.Any(t => t == Shared.SharedGameTags.VehicleTag) && countVehicles > 0)
                            {
                                if (thunderBird && hasHarness && countVehicles < 2)
                                {
                                    //       TFTVLogger.Always($"{geoCharacter.DisplayName} should return true");
                                    return true;
                                }
                                return false;
                            }

                            if (geoCharacter.GameTags.Any(t => t == Shared.SharedGameTags.MutogTag) && countMutogs > 0)
                            {
                                if (mutogPen && countMutogs < 2)
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
                }
                internal class OverdriveRange
                {

                    internal static float GetThunderbirdOverdriveRange(Research phoenixResearch)
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

                    [HarmonyPatch(typeof(GeoVehicle), "UpdateVehicleBonusCache")]
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
                    }
                }
            }
            internal class UI
            {
                public static int GetTier(ItemDef moduleDef)
                {
                    try
                    {
                        int tier = 0;

                        if (moduleDef == _blimpSpeedModule)
                        {
                            tier = Tiers.GetBlimpSpeedTier();
                        }
                        else if (moduleDef == _blimpMutationLabModule)
                        {
                            tier = Tiers.GetBuffLevelFromResearchDefs(_blimpMutationLabModuleBuffResearches);
                        }
                        else if (moduleDef == _blimpMistModule)
                        {
                            tier = Tiers.GetMistModuleBuffLevel();
                        }
                        else if (moduleDef == _thunderbirdGroundAttackModule)
                        {
                            tier = Tiers.GetGWABuffLevel();
                        }
                        else if (moduleDef == _heliosStealthModule)
                        {
                            tier = Tiers.GetStealthTierForUI();
                        }
                        else if (moduleDef == _heliosPanaceaModule)
                        {
                            tier = Tiers.GetBuffLevelFromResearchDefs(_heliosStatisChamberBuffResearchDefs);
                        }
                        else if (moduleDef == _thunderbirdWorkshopModule)
                        {
                            tier = Tiers.GetBuffLevelFromResearchDefs(_thunderbirdWorkshopBuffResearchDefs);
                        }
                        else if (moduleDef == _thunderbirdScannerModule)
                        {
                            tier = Tiers.GetBuffLevelFromResearchDefs(_thunderbirdScannerBuffResearchDefs);
                        }
                        else if (moduleDef == _thunderbirdRangeModule)
                        {
                            tier = Tiers.GetBuffLevelFromResearchDefs(_thunderbirdRangeBuffResearchDefs);
                        }


                        return tier;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                public static Sprite GetTierSprite(ItemDef moduleDef)
                {
                    try
                    {
                        Sprite sprite = moduleDef.ViewElementDef.SmallIcon;

                        if (moduleDef == _blimpSpeedModule)
                        {
                            int tier = Tiers.GetBlimpSpeedTier();

                            if (tier > 1)
                            {
                                //TFTVLogger.Always($"adjusting BlimpSpeed module picture in GetSmallIcon");
                                sprite = Helper.CreateSpriteFromImageFile($"TFTV_Blimp_Speed_Small{tier}.png");
                            }
                        }
                        else if (moduleDef == _blimpMutationLabModule)
                        {
                            int tier = Tiers.GetBuffLevelFromResearchDefs(_blimpMutationLabModuleBuffResearches);

                            if (tier > 1)
                            {
                                sprite = Helper.CreateSpriteFromImageFile($"TFTV_Blimp_MutationLab_Small{tier}.png");
                            }
                        }
                        else if (moduleDef == _blimpMistModule)
                        {
                            int tier = Tiers.GetMistModuleBuffLevel();

                            if (tier > 1)
                            {
                                sprite = Helper.CreateSpriteFromImageFile($"TFTV_Blimp_WP_Small{tier}.png");
                            }

                        }
                        else if (moduleDef == _thunderbirdGroundAttackModule)
                        {
                            int tier = Tiers.GetGWABuffLevel();

                            if (tier > 1)
                            {
                                sprite = Helper.CreateSpriteFromImageFile($"TFTV_Thunderbird_GroundAttack_Small{tier}.png");
                            }

                        }

                        return sprite;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }


                [HarmonyPatch(typeof(ItemDef), "GetDetailedImage")]
                public static class ItemDef_GetDetailedImage_Patch
                {
                    public static void Postfix(ItemDef __instance, ref Sprite __result)
                    {
                        try
                        {
                            // Your master toggle for this feature
                            if (!AircraftReworkOn)
                            {
                                return;
                            }

                            if (__instance is GeoVehicleEquipmentDef)
                            {
                                __result = GetTierSprite(__instance);
                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                    }
                }


                [HarmonyPatch(typeof(ItemDef), "GetSmallIcon")]
                public static class ItemDef_GetSmallIcon_Patch
                {
                    public static void Postfix(ItemDef __instance, ref Sprite __result)
                    {
                        try
                        {
                            // Your master toggle for this feature
                            if (!AircraftReworkOn)
                            {
                                return;
                            }

                            if (__instance is GeoVehicleEquipmentDef)
                            {
                                __result = GetTierSprite(__instance);
                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                    }
                }


                [HarmonyPatch(typeof(GeoLevelController), "get_HasFesteringSkies")]
                public static class GeoLevelController_get_HasFesteringSkies_Patch
                {
                    public static void Postfix(GeoLevelController __instance, ref bool __result)
                    {
                        try
                        {

                            if (!AircraftReworkOn)
                            {
                                return;
                            }

                            __result = false;

                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                    }
                }



                [HarmonyPatch(typeof(GeoscapeTutorial), "UnlockUIForStep")]
                public static class GeoscapeTutorial_UnlockUIForStep_Patch
                {
                    public static void Postfix(GeoscapeTutorial __instance, GeoscapeTutorialStepType step)
                    {
                        try
                        {

                            if (!AircraftReworkOn)
                            {
                                return;
                            }

                            if (step == GeoscapeTutorialStepType.TutorialCompleted)
                            {
                                GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                                controller.View.GeoscapeModules.GeoSectionBarModule.VehicleRosterButton.SetState(true);
                                controller.View.GeoscapeModules.ActionsBarModule.AircraftEquipmentDisplayEnabled = true;
                            }

                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                    }
                }






                [HarmonyPatch(typeof(UIModuleGeoSectionBar), "Show")]
                public static class UIModuleGeoSectionBar_Show_Patch
                {
                    public static bool Prefix(UIModuleGeoSectionBar __instance, bool showSections)
                    {
                        try
                        {

                            if (!AircraftReworkOn)
                            {
                                return true;
                            }

                            __instance.VehicleRosterButton.gameObject.SetActive(true);
                            __instance.SectionsRoot.SetActive(showSections);

                            return false;
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }
                }

                [HarmonyPatch(typeof(UIModuleGeoSectionBar), "ActivateVehicleRosterContent")]
                public static class UIModuleGeoSectionBar_ActivateVehicleRosterContent_Patch
                {
                    public static bool Prefix(UIModuleGeoSectionBar __instance, GeoscapeViewContext ____context, UIGeoSection ____section)
                    {
                        try
                        {

                            if (!AircraftReworkOn)
                            {
                                return true;
                            }


                            if (____section == UIGeoSection.VehicleRoster)
                            {
                                __instance.ActivateGeoscapeContent();
                                return false;
                            }
                            ____context.View.SetGamePauseState(true);
                            ____context.View.ToVehicleRosterState(StateStackAction.ClearStackAndPush, null);

                            return false;
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }
                }


                [HarmonyPatch(typeof(GeoscapeView), "InitView")]
                public static class GeoscapeView_InitView_Patch
                {
                    public static void Postfix(GeoscapeView __instance)
                    {
                        try
                        {

                            if (!AircraftReworkOn)
                            {
                                return;
                            }


                            __instance.GeoscapeModules.ActionsBarModule.AircraftEquipmentDisplayEnabled = true;

                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                    }
                }



                [HarmonyPatch(typeof(ShortEquipmentInfoButton), "SetEquipment")]
                public static class ShortEquipmentInfoButton_SetEquipment_Patch
                {
                    public static void Postfix(ShortEquipmentInfoButton __instance, GeoVehicleEquipment equipment)
                    {
                        try
                        {

                            if (!AircraftReworkOn)
                            {
                                return;
                            }

                            if (equipment != null && equipment.EquipmentDef != null)
                            {
                                __instance.WeaponIcon.sprite = GetTierSprite(equipment.EquipmentDef);
                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                    }
                }


                [HarmonyPatch(typeof(AircraftEquipmentViewController), "SetEquipmentUIData")]
                public static class AircraftEquipmentViewController_SetEquipmentUIData_Patch
                {
                    public static void Postfix(AircraftEquipmentViewController __instance, GeoVehicleEquipmentUIData data)
                    {

                        try
                        {
                            if (!AircraftReworkOn)
                            {
                                return;
                            }

                            // TFTVLogger.Always($"SetEquipmentUIData {data == null} {data?.AircraftEquipmentDef?.name}");

                            if (data == null)
                            {
                                return;
                            }


                            ItemDef itemDef = data.AircraftEquipmentDef;

                            int tier = GetTier(itemDef);
                            __instance.Health.text = "";

                            //  TFTVLogger.Always($"for {__instance.name}, tier {tier}, itemDef {itemDef?.name}");

                            if (tier > 0)
                            {
                                __instance.Health.text = $"{TFTVCommonMethods.ConvertKeyToString("TFTV_KEY_TIER")} {tier}";
                            }

                            __instance.HealthBar.HealthBar.gameObject.SetActive(false);

                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }

                    }
                }

                [HarmonyPatch(typeof(UIAircraftEquipmentTooltip), "DisplayAllStats")]
                public static class Patch_UIAircraftEquipmentTooltip_DisplayAllStats
                {
                    public static bool Prefix(UIAircraftEquipmentTooltip __instance)
                    {
                        try
                        {
                            if (!AircraftReworkOn)
                            {
                                return true;
                            }

                            // Access private fields via reflection
                            var type = typeof(UIAircraftEquipmentTooltip);

                            // Icon
                            var Icon = (Image)type.GetField("Icon").GetValue(__instance);
                            // UISettings

                            // DisplayedData
                            var DisplayedData = type.GetField("DisplayedData").GetValue(__instance);

                            // ItemNameLocComp, ItemDescriptionLocComp
                            var ItemNameLocComp = (I2.Loc.Localize)type.GetField("ItemNameLocComp").GetValue(__instance);
                            var ItemDescriptionLocComp = (I2.Loc.Localize)type.GetField("ItemDescriptionLocComp").GetValue(__instance);

                            // ExpansionSettings
                            // var ExpansionSettings = type.GetField("ExpansionSettings").GetValue(__instance);

                            // Call: _ = Icon != null;
                            _ = Icon != null;

                            if (__instance.UISettings.ShowNameDescription)
                            {
                                type.GetMethod("DisplayNameDescription", BindingFlags.NonPublic | BindingFlags.Instance)
                                     .Invoke(__instance, null);
                            }
                            else
                            {
                                ItemNameLocComp.gameObject.SetActive(value: false);
                                ItemDescriptionLocComp.gameObject.SetActive(value: false);
                            }

                            // GeoVehicleWeaponDef geoVehicleWeaponDef = DisplayedData.AircraftEquipmentDef as GeoVehicleWeaponDef;
                            var AircraftEquipmentDef = DisplayedData.GetType().GetField("AircraftEquipmentDef").GetValue(DisplayedData);

                            var geoVehicleModuleDef = AircraftEquipmentDef as GeoVehicleModuleDef;
                            if (geoVehicleModuleDef != null)
                            {

                                type.GetMethod("DisplayGeoscapeBonus", BindingFlags.NonPublic | BindingFlags.Instance)
                                .Invoke(__instance, new object[] { geoVehicleModuleDef });

                                int tier = GetTier(geoVehicleModuleDef);

                                if (tier > 0)
                                {
                                    LocalizedTextBind localizedTextBindTest2 = new LocalizedTextBind("TFTV_KEY_TIER", true);

                                    MethodInfo methodInfo = type.GetMethod("AddStatObject", BindingFlags.NonPublic | BindingFlags.Instance);
                                    methodInfo.Invoke(__instance, new object[] { localizedTextBindTest2, null, tier.ToString() });
                                }
                                //  
                                //   type.GetMethod("DisplayCountermeasureType", BindingFlags.NonPublic | BindingFlags.Instance)
                                //      .Invoke(__instance, new object[] { geoVehicleModuleDef });
                                //   type.GetMethod("AddSeparator", BindingFlags.NonPublic | BindingFlags.Instance)
                                //      .Invoke(__instance, null);
                                //  type.GetMethod("DisplayCharges", BindingFlags.NonPublic | BindingFlags.Instance)
                                //      .Invoke(__instance, new object[] { geoVehicleModuleDef });
                                //  type.GetMethod("DisplayDuration", BindingFlags.NonPublic | BindingFlags.Instance)
                                //  .Invoke(__instance, new object[] { geoVehicleModuleDef });
                                //  type.GetMethod("DisplayPreparation", BindingFlags.NonPublic | BindingFlags.Instance)
                                //    .Invoke(__instance, new object[] { geoVehicleModuleDef });
                                //  type.GetMethod("AddSeparator", BindingFlags.NonPublic | BindingFlags.Instance)
                                //  .Invoke(__instance, null);
                                //  type.GetMethod("DisplayHitPoints", BindingFlags.NonPublic | BindingFlags.Instance)
                                //    .Invoke(__instance, null);
                            }


                            Text text = ItemDescriptionLocComp.transform.GetComponent<Text>();

                            if (text != null)
                            {
                                // TFTVLogger.Always($"found text {text.text}");
                                text.verticalOverflow = VerticalWrapMode.Overflow;
                            }


                            /* foreach (Component component in __instance.transform.GetComponentsInChildren<Component>()) 
                             {
                                 TFTVLogger.Always($"{component.name} {component.GetType()}");                            
                             }*/


                            return false;
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }
                }



                [HarmonyPatch(typeof(GeoVehicleRosterEquipmentSlot), "SetItem")]
                public static class GeoVehicleRosterEquipmentSlot_SetItem_Patch
                {
                    public static void Postfix(GeoVehicleRosterEquipmentSlot __instance, GeoVehicleEquipmentUIData item)
                    {

                        try
                        {
                            if (!AircraftReworkOn)
                            {
                                return;
                            }


                            if (item != null && item.AircraftEquipmentDef != null)
                            {
                                __instance.ItemImage.overrideSprite = GetTierSprite(item.AircraftEquipmentDef);
                            }



                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }

                    }
                }



                [HarmonyPatch(typeof(UIModuleSoldierEquip), "DoFilter")]
                public static class UIModuleSoldierEquip_DoFilter_Patch
                {
                    public static void Prefix(UIModuleSoldierEquip __instance)
                    {
                        try
                        {
                            if (!AircraftReworkOn)
                            {
                                return;
                            }


                            // Get the private method 'TypeFilter' using Harmony's AccessTools.
                            MethodInfo typeFilterMethod = AccessTools.Method(typeof(UIModuleSoldierEquip), "TypeFilter");
                            if (typeFilterMethod == null)
                            {
                                // TFTVLogger.Always("Could not locate the private method 'TypeFilter' in UIModuleSoldierEquip.");
                                return;
                            }

                            // Use the public StorageList property.
                            var storageList = __instance.StorageList;
                            if (storageList == null)
                            {
                                // TFTVLogger.Always("StorageList is null in UIModuleSoldierEquip.");
                                return;
                            }

                            if (storageList.UnfilteredItems == null)
                            {
                                // TFTVLogger.Always("UnfilteredItems is null in StorageList.");
                                return;
                            }

                            // Iterate over each unfiltered item.
                            foreach (var item in storageList.UnfilteredItems)
                            {

                                // Attempt to cast the item’s ItemDef to TacticalItemDef.
                                var tacticalItemDef = item.ItemDef as TacticalItemDef;
                                if (tacticalItemDef == null)
                                {
                                    TFTVLogger.Always($"ItemDef is null or not a TacticalItemDef. Item: {item} {item.GetType()}");

                                    continue;
                                }

                                // Invoke the private TypeFilter method on the instance.
                                bool passesTypeFilter = (bool)typeFilterMethod.Invoke(__instance, new object[] { tacticalItemDef });
                                // Optionally log the result:
                                // TFTVLogger.Info($"Item: {item}, passesTypeFilter: {passesTypeFilter}");

                            }

                            int removedCount = storageList.UnfilteredItems.RemoveAll(item => !(item.ItemDef is TacticalItemDef));
                            if (removedCount > 0)
                            {
                                TFTVLogger.Info($"Removed {removedCount} items that are not TacticalItemDef from UnfilteredItems.");
                            }

                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }
                }

                //Commenting out removes info about HP, crew and modules on mouseover from all aircraft, also faction aircraft
                [HarmonyPatch(typeof(UIModuleSelectionInfoBox), "SetActorInfo")]
                public static class UIModuleSelectionInfoBox_SetActorInfo_Patch
                {
                    public static bool Prefix(UIModuleSelectionInfoBox __instance,
                        GeoscapeViewContext context, GeoActor actor, Vector3 tooltipPosition, float fov,
                        ref GeoscapeViewContext ____context, ref RectTransform ____moduleRect, ref RectTransform ____panelRect, ref bool ____showTooltip)
                    {
                        try
                        {

                            if (!AircraftReworkOn)
                            {
                                return true;
                            }


                            MethodInfo methodInfoSetExtendedGeoVehicleInfo = typeof(UIModuleSelectionInfoBox).GetMethod("SetExtendedGeoVehicleInfo", BindingFlags.Instance | BindingFlags.NonPublic);
                            MethodInfo methodInfoSetGeoVehicleInfo = typeof(UIModuleSelectionInfoBox).GetMethod("SetGeoVehicleInfo", BindingFlags.Instance | BindingFlags.NonPublic);
                            MethodInfo methodInfoSetGeoSiteInfo = typeof(UIModuleSelectionInfoBox).GetMethod("SetGeoSiteInfo", BindingFlags.Instance | BindingFlags.NonPublic);


                            ____context = context;
                            __instance.ClearSelectionInfo();
                            __instance.PanelAlpha.alpha = 0f;


                            if (____moduleRect == null)
                            {
                                ____moduleRect = __instance.gameObject.GetComponent<RectTransform>();
                            }

                            ____moduleRect.position = new Vector3(tooltipPosition.x, tooltipPosition.y, 0f);
                            if (____panelRect == null)
                            {
                                ____panelRect = __instance.PanelAlpha.gameObject.GetComponent<RectTransform>();
                            }

                            ____panelRect.anchoredPosition = new Vector3(0f, 0f, 0f);

                            if (actor is GeoSite && (actor.GetComponent<GeoAlienBase>() == null || !actor.GetComponent<GeoAlienBase>().IsPalace))
                            {
                                ____panelRect.anchoredPosition = new Vector3(____panelRect.anchoredPosition.x + __instance.CenterXOffset.Evaluate(fov), ____panelRect.anchoredPosition.y + __instance.CenterYOffset.Evaluate(fov), 0f);
                                GeoSite geoSite = (GeoSite)actor;
                                __instance.BaseInformation.SetActive(value: true);
                                __instance.VehicleTooltipInfoRoot.SetActive(value: false);
                                if (geoSite.GetVisible(____context.ViewerFaction))
                                {
                                    methodInfoSetGeoSiteInfo.Invoke(__instance, new object[] { geoSite });

                                }

                                ____showTooltip = true;
                            }
                            else if (actor is GeoVehicle)
                            {
                                ____panelRect.anchoredPosition = new Vector3(____panelRect.anchoredPosition.x + __instance.AircraftCenterXOffset.Evaluate(fov), ____panelRect.anchoredPosition.y + __instance.AircraftCenterYOffset.Evaluate(fov), 0f);
                                GeoVehicle geoVehicle = (GeoVehicle)actor;
                                if (geoVehicle.IsVisible && geoVehicle.IsOwnedByViewer)
                                {
                                    __instance.BaseInformation.SetActive(value: false);
                                    __instance.VehicleTooltipInfoRoot.SetActive(value: true);
                                    methodInfoSetExtendedGeoVehicleInfo.Invoke(__instance, new object[] { geoVehicle });
                                    // __instance.SetExtendedGeoVehicleInfo(geoVehicle);
                                }
                                else if (geoVehicle.IsVisible)
                                {
                                    __instance.BaseInformation.SetActive(value: true);
                                    __instance.VehicleTooltipInfoRoot.SetActive(value: false);
                                    methodInfoSetGeoVehicleInfo.Invoke(__instance, new object[] { geoVehicle });
                                }

                                ____showTooltip = true;
                            }
                            else
                            {
                                ____showTooltip = false;
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
                            __instance.RootSeparator.gameObject.SetActive(true);//aircraftInfo.IsOwnedByViewer);
                            __instance.PersonalCrewContainer.SetActive(vehicle.IsOwnedByViewer);

                            // This sets up common visuals (unchanged)
                            AircraftEquipmentViewController.AircraftCommonEquipmentVisualData aircraftCommonEquipmentVisualData
                                = new AircraftEquipmentViewController.AircraftCommonEquipmentVisualData(vehicle);

                            float maintenanceLevel = (float)aircraftInfo.CurrentHitPoints / (float)aircraftInfo.MaxHitPoints;

                            //   TFTVLogger.Always($"{aircraftInfoData.DisplayName}: {maintenanceLevel}");

                            int maintenancePercentage = (int)(maintenanceLevel * 100);
                            aircraftCommonEquipmentVisualData.Health = maintenancePercentage;
                            aircraftCommonEquipmentVisualData.MaxHealth = 100;

                            aircraftCommonEquipmentVisualData.IsFriendlyModule = vehicle.IsOwnedByViewer;
                            __instance.AircraftInfo.SetEquipmentData(aircraftCommonEquipmentVisualData);

                            __instance.DisengageText.gameObject.SetActive(!vehicle.CanRedirect);
                            __instance.VehicleCrewText.text = $"{aircraftInfo.CurrentCrew}/{aircraftInfo.MaxCrew}";
                            //  __instance.VehicleArmorText.text = aircraftInfo.CurrentArmor.ToString();

                            List<GeoVehicleModuleDef> modules = vehicle.Modules.Select(m => m?.ModuleDef).ToList();



                            float maintenanceFactor = AircraftMaintenance.GetMaintenanceFactor(modules);
                            float currentHitPoints = aircraftInfo.CurrentHitPoints;

                            if (maintenanceFactor > 0)
                            {

                                int flightHours = (int)Mathf.Max((currentHitPoints - 200) / maintenanceFactor, 0);
                                int flightHoursTotal = (int)Mathf.Max(currentHitPoints / maintenanceFactor, 0);

                                __instance.VehicleArmorText.text = $"{flightHours}({flightHoursTotal})";

                            }
                            else
                            {
                                __instance.VehicleArmorText.text = $"UNLIMITED";

                            }

                            __instance.VehicleArmorText.gameObject.SetActive(true);



                            Transform parent = __instance.VehicleArmorText.transform.parent;

                            //  TFTVLogger.Always($"parent is {parent.name}");

                            foreach (Component component in parent.GetComponentsInChildren<Component>())
                            {
                                if (component is Image image)
                                {
                                    // TFTVLogger.Always($"image: {component.name} {component.GetType()}");
                                    component.gameObject.SetActive(false);
                                }
                            }



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
                    public static void Prefix(ref List<GeoVehicleEquipment> equipments, ref List<ShortEquipmentInfoButton> ____shortEquipmentInfoButtons)
                    {
                        try
                        {
                            if (!AircraftReworkOn)
                            {
                                return;
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

                    }
                }



                [HarmonyPatch(typeof(UIModuleVehicleSelection), "RefreshVehicleBars")]
                public static class UIModuleVehicleSelection_RefreshVehicleBars_Patch
                {
                    public static void Postfix(UIModuleVehicleSelection __instance)
                    {
                        try
                        {
                            if (!AircraftReworkOn)
                            {
                                return;
                            }

                            Slider slider = __instance.VehicleHPBar;

                            // Try to get the fill image
                            var fill = slider.fillRect?.GetComponent<Image>();
                            if (fill == null) return;

                            // TFTVLogger.Always($"got here");

                            // Choose color based on value
                            Color color;
                            if (slider.value > 0.5f)
                                color = Color.green;
                            else if (slider.value > 0.25f)
                                color = Color.yellow;
                            else
                                color = Color.red;

                            fill.color = color;
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
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


                            float maintenanceLevel = (float)aircraftInfoData.CurrentHitPoints / (float)aircraftInfoData.MaxHitPoints;

                            //   TFTVLogger.Always($"{aircraftInfoData.DisplayName}: {maintenanceLevel}");

                            int maintenancePercentage = (int)(maintenanceLevel * 100);

                            __instance.AircraftHitPoints.text = $"{maintenancePercentage}%";

                            /*  if (aircraftInfoData.MaxHitPoints != aircraftInfoData.CurrentHitPoints)
                              {
                                  __instance.AircraftHitPoints.text = $"{aircraftInfoData.CurrentHitPoints}/{aircraftInfoData.MaxHitPoints}";
                              }
                              else
                              {
                                  __instance.AircraftHitPoints.text = aircraftInfoData.MaxHitPoints.ToString();
                              }*/



                            Transform parent = __instance.AircraftArmor.transform.parent;

                            //  TFTVLogger.Always($"parent is {parent.name}");

                            foreach (Component component in parent.GetComponentsInChildren<Component>())
                            {

                                if (component is Image image)
                                {
                                    // TFTVLogger.Always($"image: {component.name} {component.GetType()}");
                                    component.gameObject.SetActive(false);
                                }

                            }

                            Transform grandParent = parent.parent;

                            foreach (Component component in grandParent.GetComponentsInChildren<Component>())
                            {
                                if (component is Text text && text.name == "UITextGeneric_Small_StatName")
                                {
                                    text.text = TFTVCommonMethods.ConvertKeyToString("Geoscape/KEY_AIRCRAFT_STATS_DURABILITY");
                                    break;
                                }
                            }

                            List<GeoVehicleModuleDef> aircraftEquipmentDefs = new List<GeoVehicleModuleDef>();

                            if (modules != null)
                            {
                                foreach (GeoVehicleEquipmentUIData module in modules)
                                {
                                    if (module != null && module.AircraftEquipmentDef is GeoVehicleModuleDef moduleDef)
                                    {
                                        aircraftEquipmentDefs.Add(moduleDef);
                                    }
                                }
                            }


                            float maintenanceFactor = AircraftMaintenance.GetMaintenanceFactor(aircraftEquipmentDefs);
                            float currentHitPoints = aircraftInfoData.CurrentHitPoints;

                            if (maintenanceFactor > 0)
                            {

                                int flightHours = (int)Mathf.Max((currentHitPoints - 200) / maintenanceFactor, 0);
                                int flightHoursTotal = (int)Mathf.Max(currentHitPoints / maintenanceFactor, 0);

                                __instance.AircraftArmor.text = $"{flightHours}({flightHoursTotal})";
                                __instance.AircraftArmor.gameObject.SetActive(true);


                            }
                            else
                            {
                                __instance.AircraftArmor.text = $"UNLIMITED";
                                __instance.AircraftArmor.gameObject.SetActive(true);
                            }

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

                                /*  foreach (GeoVehicleEquipmentUIData module in modules)
                                  {
                                      TFTVLogger.Always($"Module: {module?.AircraftEquipmentDef.name}");
                                  }*/


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

                                UIStatBar uIStatBar = __instance.VehicleHealthBar;

                                float maintenanceLevel = (float)baseObject.Stats.HitPoints / (float)baseObject.Stats.MaxHitPoints;

                                uIStatBar.SetValuePercent(maintenanceLevel);

                                if (uIStatBar != null && uIStatBar.CurrentValueBar != null)
                                {
                                    if (maintenanceLevel > 0.5f)
                                    {
                                        uIStatBar.CurrentValueBar.color = Color.green;
                                    }
                                    else if (maintenanceLevel > 0.25f)
                                    {
                                        uIStatBar.CurrentValueBar.color = Color.yellow;
                                    }
                                    else
                                    {
                                        uIStatBar.CurrentValueBar.color = Color.red;
                                    }
                                }

                                Transform statBarParentTransform = uIStatBar.transform.parent;


                                Text healthText = null;
                                foreach (Text text in statBarParentTransform.GetComponentsInChildren<Text>(true))
                                {
                                    if (text.name == "Health_Text")
                                    {
                                        healthText = text;
                                        break;
                                    }
                                }
                                if (healthText != null)
                                {
                                    healthText.text = TFTVCommonMethods.ConvertKeyToString("DLC 3 - Behemoth/KEY_DLC3_HULL_POINTS");
                                }


                                int maintenancePercentage = (int)(maintenanceLevel * 100);

                                __instance.VehicleHealthText.text = $"{maintenancePercentage}%";

                                //   __instance.VehicleHealthText.text = baseObject.Stats.HitPoints.ToString() + "/" + baseObject.Stats.MaxHitPoints;
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
                public static class UIVehicleEquipmentInventoryList_Init_Patch
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
                                    UnityEngine.Debug.Log("Module already equipped on aircraft. Duplicate not allowed.");
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
                                        UnityEngine.Debug.Log("Module already equipped on aircraft. Duplicate not allowed.");
                                        __result = false;
                                        return false;
                                    }

                                    // Save the module in slot 0 (the oldest) for removal.
                                    GeoVehicleEquipmentUIData oldModule = slots[0].Equipment;
                                    // Find a free slot in storage for the module being removed.
                                    var freeStorageSlot = storageList.GetFirstAvailableSlot(oldModule, false);
                                    if (freeStorageSlot == null)
                                    {
                                        UnityEngine.Debug.LogError("No free storage slot available for the replaced module.");
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
        internal class AircraftMaintenance
        {

            /* private static bool CheckSpeedAdjustedForMaintenance(GeoVehicle geoVehicle)
             {
                 try
                 {
                     float baseSpeed = geoVehicle.VehicleDef.BaseStats.Speed.Value;
                     float totalSpeed = baseSpeed;

                     if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _basicSpeedModule))
                     {
                         totalSpeed += _basicSpeedModule.GeoVehicleModuleBonusValue;
                     }

                     if (geoVehicle.VehicleDef == helios)
                     {
                         totalSpeed += AircraftSpeed.GetHeliosSpeed();
                     }

                     if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _basicPassengerModule))
                     {
                         totalSpeed += _basicPassengerModule.GeoVehicleModuleBonusValue;
                     }

                     if (geoVehicle.Stats.HitPoints <= 50)
                     {
                         totalSpeed /= 2;
                     }

                     return geoVehicle.Speed.Value == totalSpeed;
                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                     throw;
                 }
             }*/


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

                            AircraftSpeed.AdjustAircraftSpeed(__instance, true);

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

                            geoVehicle.SetHitpoints(geoVehicle.Stats.HitPoints - GetMaintenanceFactor(modules));
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

                        AircraftSpeed.AdjustAircraftSpeed(__instance, true);

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            [HarmonyPatch(typeof(GeoMission), "ApplyMissionResults")]
            public static class GeoMission_ApplyMissionResults_Patch
            {
                public static void Postfix(GeoMission __instance, TacMissionResult result, GeoSquad squad)
                {
                    try
                    {
                        if (!AircraftReworkOn)
                        {
                            return;
                        }

                        bool ambushMission = __instance.MissionDef.Tags.Contains(Shared.SharedGameTags.AmbushMissionTag);
                        bool phoenixLostMission = result.FactionResults.Any(fr => fr.FactionDef == __instance.Level.PhoenixFaction.FactionDef.PPFactionDef
                            && fr.State == TacFactionState.Defeated);

                        TFTVLogger.Always($"Mission results: {__instance.MissionDef.name} ambush? {ambushMission} lost? {phoenixLostMission}");

                        if (ambushMission && phoenixLostMission)
                        {
                            __instance.GetLocalAircraft(squad).SetHitpoints(0);
                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
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
        internal class AircraftSpeed
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
                    return Modules.Tiers.GetBlimpSpeedTier() * _mistSpeedModuleBuff;

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
                    Research phoenixResearch = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction.Research;
                    float speedBuff = _thunderbirdSpeedBuffPerLevel;

                    foreach (ResearchDef researchDef in _thunderbirdRangeBuffResearchDefs)
                    {
                        if (phoenixResearch.HasCompleted(researchDef.Id))
                        {
                            speedBuff += _thunderbirdSpeedBuffPerLevel;
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
                    Research phoenixResearch = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction.Research;
                    float speedBuff = 0;

                    foreach (ResearchDef researchDef in _heliosSpeedBuffResearchDefs)
                    {
                        if (phoenixResearch.HasCompleted(researchDef.Id))
                        {
                            speedBuff += _heliosSpeedBuffPerLevel;
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
                        speed *= 1 - _mistSpeedMalus;

                        // TFTVLogger.Always($"speed in Mist of {geoVehicle.Name}, after applying malus of {_mistSpeedMalus} is {speed}", false);
                    }
                    else
                    {
                        speed += _mistSpeedModuleBuff;
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
