using Assets.Code.PhoenixPoint.Geoscape.Entities.Sites.TheMarketplace;
using Base.Core;
using Base.Entities.Statuses;
using Base.UI;
using HarmonyLib;
using I2.Loc;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Abilities;
using PhoenixPoint.Geoscape.Entities.Interception;
using PhoenixPoint.Geoscape.Entities.Interception.Equipments;
using PhoenixPoint.Geoscape.Entities.PhoenixBases.FacilityComponents;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Eventus;
using System;
using System.Collections.Generic;
using System.Linq;
using TFTV.TFTVAircraftRework;
using UnityEngine;
using static TFTV.TFTVAircraftReworkMain;

namespace TFTV
{

    internal class AircraftReworkDefs
    {
        internal static readonly DefCache DefCache = TFTVMain.Main.DefCache;

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
                AddLoadingTips();
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

        private static void AddLoadingTips()
        {
            try 
            {
                LoadingTipsRepositoryDef loadingTipsRepositoryDef = DefCache.GetDef<LoadingTipsRepositoryDef>("LoadingTipsRepositoryDef");
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_35" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_36" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_37" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_38" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_39" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_40" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_41" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_42" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_43" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_44" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_45" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_46" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_47" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_48" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_49" });
                loadingTipsRepositoryDef.TacticalLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_TACTICAL_29" });


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

                newAbility.EventOnActivate = DefCache.GetDef<TacticalEventDef>("LaunchDamageVoice_EventDef");

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
                newAbility.SceneViewElementDef.TargetPositionMarker = PhoenixPoint.Tactical.View.GroundMarkerType.Invalid;
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



}

