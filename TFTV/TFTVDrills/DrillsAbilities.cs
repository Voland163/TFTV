using Base.Core;
using Base.Defs;
using Base.Entities.Abilities;
using Base.Entities.Effects.ApplicationConditions;
using Base.Entities.Statuses;
using Base.Serialization.General;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.AI;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Animations;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Effects.ApplicationConditions;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.View.ViewStates;
using PRMBetterClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TFTV.Tactical.Entities.Statuses;
using UnityEngine;
using UnityEngine.UI;
using static TFTV.TFTVDrills.DrillsPublicClasses;



namespace TFTV
{
    internal class DrillsAbilities
    {

        //DefCache.GetDef<ApplyStatusAbilityDef>("MarkedForDeath_AbilityDef"); this for mark overwatch status



        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;

        private static SkillTagDef _drillSkillTagDef;
        private static DamageMultiplierStatusDef _drawfireStatus;
        private static DamageMultiplierStatusDef _markedwatchStatus;
        private static PassiveModifierAbilityDef _causticJamming;
        private static PassiveModifierAbilityDef _mentorProtocol;
        private static PassiveModifierAbilityDef _virulentGrip;
        private static PassiveModifierAbilityDef _viralPuppeteer;
        private static PassiveModifierAbilityDef _toxicLink;
        private static PassiveModifierAbilityDef _shieldedRiposte;
        private static ApplyStatusAbilityDef _mightMakesRight;
        private static PassiveModifierAbilityDef _packLoyalty;
        private static PassiveModifierAbilityDef _shockDiscipline;
        private static LightStunStatusDef _shockDisciplineStatus;
        private static PassiveModifierAbilityDef _snapBrace;
        private static ApplyStatusAbilityDef _partingShot;
        private static StatMultiplierStatusDef _partingshotAccuracyMalusStatus;

        //  private static ApplyStatusAbilityDef _veiledMarksman;


        public static List<TacticalAbilityDef> Drills = new List<TacticalAbilityDef>();

        public static StandardDamageTypeEffectDef MeleeStandardDamageType;
        public static DamageKeywordDef MeleeDamageKeywordDef;
        public static StandardDamageTypeEffectDef PsychicStandardDamageType;
        public static DamageKeywordDef PsychicStandardDamageKeywordDef;


       

        /*  [SerializeType(InheritCustomCreateFrom = typeof(BaseDef))]
          [CreateAssetMenu(fileName = "ActorIsInCoverEffectConditionDef",
                      menuName = "Defs/Effects/Conditions/Actor is in cover")]
          public class ActorIsInCoverEffectConditionDef : TacActorEffectConditionDef
          {
              protected override bool ActorChecks(TacticalActorBase actor)
              {
                  var tacticalActor = actor as TacticalActor;
                  if (tacticalActor == null || tacticalActor.IdleAbility == null || tacticalActor.IdleAbility.ActivePose.CoverInfo.CoverType == CoverType.None) return false;



                  return true;
              }
          }*/


        internal class Defs
        {



            public static void CreateDefs()
            {
                try
                {
                    CreateSkillDrillTag();
                    CreateDrills();
                    CreateMeleeDamageType();
                    CreatePsychicStandardDamageType();
                    ReplaceCorruptionDamageTypes();
                    ReplaceStunStatusWithNewConditionalStatusDef();
                    // DefCache.GetDef<EquipmentDef>("FS_RiotShield_WeaponDef").HolsterWhenNotSelected = false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                } 
            }


            private static void ReplaceStunStatusWithNewConditionalStatusDef() 
            {
                try 
                {
                    string name = "TFTV_ConditionalStunStatus";

                    StunStatusDef stunStatusDef = DefCache.GetDef<StunStatusDef>("ActorStunned_StatusDef");

                    ConditionalStunStatusDef conditionalStunStatusDef = Helper.CreateDefFromClone<ConditionalStunStatusDef>(null, "{B688BC06-5562-48A3-91BB-1C54FDDB0A2A}", name);
                    conditionalStunStatusDef.Visuals = Helper.CreateDefFromClone(stunStatusDef.Visuals, "{02081B04-3C32-4A70-B6E7-2F6AB2F6363D}", name);


                    conditionalStunStatusDef.EffectName = "TFTVConditionalStun";
                    conditionalStunStatusDef.ApplicationConditions = new EffectConditionDef[0];
                    conditionalStunStatusDef.SingleInstance = true;
                    conditionalStunStatusDef.ShowNotification = true;
                    conditionalStunStatusDef.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.AlwaysVisible;
                    conditionalStunStatusDef.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;
                    conditionalStunStatusDef.ResistAbility = _shockDiscipline;
                    conditionalStunStatusDef.AlternativeStunDef = _shockDisciplineStatus;
                    conditionalStunStatusDef.EffectDef = stunStatusDef.EffectDef;
                    conditionalStunStatusDef.ActionPointsReduction = 0.75f;

                    DefCache.GetDef<StunDamageEffectDef>("ConditionalStun_StunDamageEffectDef").StunStatusDef = conditionalStunStatusDef;
                    DefCache.GetDef<StunDamageKeywordDataDef>("Shock_DamageKeywordDataDef").StatusDef = conditionalStunStatusDef;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }

            private static void CreateShockDisciplineStatus()
            {
                try


                {
                    string name = "shockdiscipline";
                    string guid0 = "{ACE8F8F1-B07B-43A4-A807-7700D5981AD4}";
                    string guid1 = "{F91586BE-098B-4F5B-8577-F1F835EF4B96}";
                    string locKeyName = $"TFTV_DRILL_{name}_NAME";
                    string locKeyDesc = $"TFTV_DRILL_{name}_DESC";


                    StunStatusDef stunStatusDef = DefCache.GetDef<StunStatusDef>("ActorStunned_StatusDef");
                    var light = Helper.CreateDefFromClone<LightStunStatusDef>(null, guid0, "shockdiscipline_light");
                    light.Visuals = Helper.CreateDefFromClone(stunStatusDef.Visuals, guid1, name);

                    light.Visuals.DisplayName1.LocalizationKey = locKeyName;
                    light.Visuals.Description.LocalizationKey = locKeyDesc;

                    light.ActionPointsReduction = 0.5f;

                    light.EffectName = "ReducedStun";
                    light.ApplicationConditions = new EffectConditionDef[0];
                    light.SingleInstance = true;
                    light.ShowNotification = true;
                    light.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.AlwaysVisible;
                    light.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;
                    light.EffectDef = stunStatusDef.EffectDef;


                    _shockDisciplineStatus = light; 
                   




                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }


            }


            private static void ReplaceCorruptionDamageTypes()
            {
                try
                {
                    CorruptionStatusDef corruptionStatusDef = DefCache.GetDef<CorruptionStatusDef>("Corruption_StatusDef");
                    corruptionStatusDef.DamageTypeDefs = new List<DamageTypeBaseEffectDef> { MeleeStandardDamageType, PsychicStandardDamageType };

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }

            private static void CreatePsychicStandardDamageType()
            {
                try
                {

                    StandardDamageTypeEffectDef sourceStandardDamageTypeEffectDef = DefCache.GetDef<StandardDamageTypeEffectDef>("Projectile_StandardDamageTypeEffectDef");
                    StandardDamageTypeEffectDef newStandardDamageTypeEffectDef = Helper.CreateDefFromClone(sourceStandardDamageTypeEffectDef, "{14084440-1682-4620-BF4D-62774EBD644A}", "TFTV_PsychicStandard_damageType");

                    DamageKeywordDef sourceDamageKeywordDef = DefCache.GetDef<DamageKeywordDef>("Damage_DamageKeywordDataDef");
                    DamageKeywordDef newDamageKeywordDef = Helper.CreateDefFromClone(sourceDamageKeywordDef, "{8AE7DE3C-72EA-4562-A221-2286AEB2ED20}", "TFTV_PsychicStandard_damageKeyword");

                    newDamageKeywordDef.DamageTypeDef = newStandardDamageTypeEffectDef;

                    PsychicStandardDamageKeywordDef = newDamageKeywordDef;
                    PsychicStandardDamageType = newStandardDamageTypeEffectDef;

                    DamageEffectDef mindCrushDamageEffectDef = DefCache.GetDef<DamageEffectDef>("E_Effect [MindCrush_AbilityDef]");
                    mindCrushDamageEffectDef.DamageTypeDef = PsychicStandardDamageType;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CreateMeleeDamageType()
            {
                try
                {

                    StandardDamageTypeEffectDef sourceStandardDamageTypeEffectDef = DefCache.GetDef<StandardDamageTypeEffectDef>("Projectile_StandardDamageTypeEffectDef");
                    StandardDamageTypeEffectDef newMeleeStandardDamageTypeEffectDef = Helper.CreateDefFromClone(sourceStandardDamageTypeEffectDef, "{4BAEF84E-5985-47D2-897F-99C863C7E71D}", "TFTV_Melee_damageType");

                    DamageKeywordDef sourceDamageKeywordDef = DefCache.GetDef<DamageKeywordDef>("Damage_DamageKeywordDataDef");
                    DamageKeywordDef newDamageKeywordDef = Helper.CreateDefFromClone(sourceDamageKeywordDef, "{701EBF00-6BDB-48E2-9635-C854ABC63AFA}", "TFTV_Melee_damageKeyword");

                    newDamageKeywordDef.DamageTypeDef = newMeleeStandardDamageTypeEffectDef;

                    //"Mutog_HeadRamming_BodyPartDef"
                    WeaponDef RammingHead = (WeaponDef)Repo.GetDef("c29d4fc0-cb86-0e54-383c-513f8926e6c1");
                    RammingHead.Tags.Add(DefCache.GetDef<GameTagDef>("MeleeWeapon_TagDef"));


                    foreach (WeaponDef weaponDef in Repo.GetAllDefs<WeaponDef>())
                    {
                        if (weaponDef.Tags.Contains(DefCache.GetDef<GameTagDef>("MeleeWeapon_TagDef")) && weaponDef.DamagePayload.DamageKeywords.Any(p => p.DamageKeywordDef == sourceDamageKeywordDef))
                        {
                            // TFTVLogger.Always($"{weaponDef.name} has melee weapon tag, replacing damage type");
                            foreach (DamageKeywordPair damageKeywordPair in weaponDef.DamagePayload.DamageKeywords)
                            {
                                if (damageKeywordPair.DamageKeywordDef == sourceDamageKeywordDef)
                                {
                                    damageKeywordPair.DamageKeywordDef = newDamageKeywordDef;
                                    // TFTVLogger.Always($"replaced");
                                }
                            }
                        }
                    }



                    MeleeStandardDamageType = newMeleeStandardDamageTypeEffectDef;
                    MeleeDamageKeywordDef = newDamageKeywordDef;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }



            }

   

            private static void CreateHeavyconditioning()
            {
                try
                {
                    string name = "heavyconditioning";
                    string guid0 = "{7BE87D08-08EB-4906-97AD-132045854758}";
                    string guid1 = "{5B384A78-B9BC-4618-8619-BAEA9BF8A94F}";
                    string guid2 = "{26C6DF5A-5DB1-4197-A9D8-AEB1A6DAB49F}";
                    string guid3 = "{15AD6ABA-F572-4BC8-B922-5DE586B5F92A}";
                    string guid4 = "{DADA4422-287E-40D4-8D67-2990C61B4DDD}";

                    StaticArmorTacStatsStatusDef newStatus = Helper.CreateDefFromClone<StaticArmorTacStatsStatusDef>(null, guid0, name);

                    List<TacticalItemDef> armourItems = Repo.GetAllDefs<TacticalItemDef>().Where(i => i.Tags.Contains(Shared.SharedGameTags.ArmorTag)
                   && i.Tags.Contains(DefCache.GetDef<GameTagDef>("Heavy_ClassTagDef")) && !i.Tags.Contains(DefCache.GetDef<GameTagDef>("Bionic_TagDef"))).ToList();

                    int requiredCount = 3;

                    newStatus.ApplicationConditions = new EffectConditionDef[] { CreateActorHasAtLeastItemsEffectConditionDef(name, guid1, armourItems, requiredCount) };

                    string locKeyName = $"TFTV_DRILL_{name}_NAME";
                    string locKeyDesc = $"TFTV_DRILL_{name}_DESC";
                    Sprite icon = Helper.CreateSpriteFromImageFile($"Drill_{name}.png");

                    ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("MasterMarksman_AbilityDef");
                    ApplyStatusAbilityDef newAbility = Helper.CreateDefFromClone(source, guid2, name);
                    newAbility.CharacterProgressionData = Helper.CreateDefFromClone(source.CharacterProgressionData, guid3, name);

                    newAbility.ViewElementDef = Helper.CreateDefFromClone(source.ViewElementDef, guid4, name);
                    newAbility.ViewElementDef.DisplayName1.LocalizationKey = locKeyName;
                    newAbility.ViewElementDef.Description.LocalizationKey = locKeyDesc;
                    newAbility.ViewElementDef.LargeIcon = icon;
                    newAbility.ViewElementDef.SmallIcon = icon;
                    newAbility.AnimType = -1;
                    newAbility.StatusDef = newStatus;
                    newAbility.TargetApplicationConditions = new EffectConditionDef[] { };

                    Drills.Add(newAbility);


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
            private static ApplyStatusAbilityDef CreateOverride()
            {
                try
                {

                    string name = "override";
                    string locKeyName = $"TFTV_DRILL_{name}_NAME";
                    string locKeyDesc = $"TFTV_DRILL_{name}_DESC";

                    string guid0 = "f2711bfc-b4cb-46dd-bb9f-599a88c1ebff";
                    string guid1 = "f7ce1c44-1447-41a3-8112-666c82451e25";
                    string guid2 = "0324925f-e318-40b6-ac8c-b68033823cd9";
                    string guid3 = "c3d9e8f0-3b4a-4c5d-e6f7-8091a2b3c4d5";
                    string guid4 = "d4e5f607-1829-4a5b-6c7d-8e9f0a1b2c3d";
                    string guid5 = "{8D86FE5B-8577-4DA6-B5B7-3D969B98C1A5}";





                    Sprite icon = Helper.CreateSpriteFromImageFile($"Drill_{name}.png");

                    ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("ManualControl_AbilityDef");


                    source.TargetingDataDef.Origin.TargetEnemies = true;


                    ApplyStatusAbilityDef newAbility = Helper.CreateDefFromClone(
                        source,
                       guid0,
                        name);
                    newAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                        source.CharacterProgressionData,
                        guid1,
                        name);
                    newAbility.ViewElementDef = Helper.CreateDefFromClone(
                        source.ViewElementDef,
                       guid2,
                        name);
                    newAbility.TargetingDataDef = Helper.CreateDefFromClone(
                        source.TargetingDataDef,
                        guid3,
                        name);

                    newAbility.CharacterProgressionData.RequiredStrength = 0;
                    newAbility.CharacterProgressionData.RequiredWill = 0;
                    newAbility.CharacterProgressionData.RequiredSpeed = 0;
                    newAbility.ViewElementDef.DisplayName1.LocalizationKey = locKeyName; // displayName;
                    newAbility.ViewElementDef.Description.LocalizationKey = locKeyDesc; // = description;
                    newAbility.ViewElementDef.LargeIcon = icon;
                    newAbility.ViewElementDef.SmallIcon = icon;
                    newAbility.TargetApplicationConditions = new EffectConditionDef[] { source.TargetApplicationConditions[0] };
                    newAbility.TargetingDataDef.Origin.TargetEnemies = true;
                    newAbility.TargetingDataDef.Origin.TargetFriendlies = false;
                    newAbility.AnimType = -1;

                    // Adding new ability to proper animations
                    foreach (TacActorAimingAbilityAnimActionDef animActionDef in Repo.GetAllDefs<TacActorAimingAbilityAnimActionDef>().Where(aad => aad.name.Contains("Soldier_Utka_AnimActionsDef")))
                    {
                        if (animActionDef.AbilityDefs != null && animActionDef.AbilityDefs.Contains(source) && !animActionDef.AbilityDefs.Contains(newAbility))
                        {
                            animActionDef.AbilityDefs = animActionDef.AbilityDefs.Append(newAbility).ToArray();
                            PRMLogger.Debug("Anim Action '" + animActionDef.name + "' set for abilities:");
                            foreach (AbilityDef ad in animActionDef.AbilityDefs)
                            {
                                PRMLogger.Debug("  " + ad.name);
                            }
                            PRMLogger.Debug("----------------------------------------------------", false);
                        }
                    }

                    newAbility.StatusDef = Helper.CreateDefFromClone(
                        source.StatusDef,
                        guid4,
                        name);

                    MindControlStatusDef newMindControlStatusDef = Helper.CreateDefFromClone(
                        DefCache.GetDef<MindControlStatusDef>("MindControl_StatusDef"),
                        guid5,
                        name
                        );

                    newMindControlStatusDef.ControlFactionDef = DefCache.GetDef<PPFactionDef>("Phoenix_FactionDef");

                    AddAttackBoostStatusDef addAttackBoostStatusDef = (AddAttackBoostStatusDef)newAbility.StatusDef;
                    addAttackBoostStatusDef.AdditionalStatusesToApply = addAttackBoostStatusDef.AdditionalStatusesToApply.AddToArray(newMindControlStatusDef);


                    Drills.Add(newAbility);

                    return newAbility;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
            private static DamageMultiplierStatusDef CreateDummyStatus(string name, string guid0, string guid1)
            {
                try
                {
                    string locKeyName = $"TFTV_DRILL_{name}_NAME";
                    string locKeyDesc = $"TFTV_DRILL_{name}_DESC";
                    Sprite icon = Helper.CreateSpriteFromImageFile($"Drill_{name}.png");

                    DamageMultiplierStatusDef sourceStatus = DefCache.GetDef<DamageMultiplierStatusDef>("BionicResistances_StatusDef");
                    DamageMultiplierStatusDef newStatus = Helper.CreateDefFromClone(sourceStatus, guid0, name);
                    newStatus.Visuals = Helper.CreateDefFromClone(sourceStatus.Visuals, guid1, name);
                    newStatus.Visuals.DisplayName1.LocalizationKey = locKeyName;
                    newStatus.Visuals.Description.LocalizationKey = locKeyDesc;
                    newStatus.Visuals.LargeIcon = icon;
                    newStatus.Visuals.SmallIcon = icon;
                    newStatus.DamageTypeDefs = new DamageTypeBaseEffectDef[] { };
                    newStatus.DurationTurns = 1;

                    return newStatus;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void CreatePartingShootAbility()
            {
                try
                {
                   
                    string name = $"partingshotpistol";
                    string locKeyName = $"TFTV_DRILL_{name}_NAME";
                    string locKeyDesc = $"TFTV_DRILL_{name}_DESC";
                    string guid0 = "490a1b2c-5d6e-7f80-980a-1b2c3d4e5f60";
                    string guid1 = "3e4f5061-7283-9a4b-5c6d-7e8f9010ab1c";
                    string guid2 = "4f506172-839a-4b5c-6d7e-8f9010ab1c2d";

                    Sprite icon = Helper.CreateSpriteFromImageFile($"Drill_{name}.png");

                    ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("BC_QuickAim_AbilityDef");
                    ApplyStatusAbilityDef newAbility = Helper.CreateDefFromClone(source, guid0, name);
                    newAbility.ViewElementDef = Helper.CreateDefFromClone(source.ViewElementDef, guid1, name);
                    newAbility.ViewElementDef.DisplayName1.LocalizationKey = locKeyName;
                    newAbility.ViewElementDef.Description.LocalizationKey = locKeyDesc;
                    newAbility.ViewElementDef.LargeIcon = icon;
                    newAbility.ViewElementDef.SmallIcon = icon;
                    newAbility.AnimType = -1;

                    DesperateUseStatusDef desperateUseStatusDef = Helper.CreateDefFromClone<DesperateUseStatusDef>(null, guid2, name);
                    desperateUseStatusDef.Visuals = Helper.CreateDefFromClone(newAbility.ViewElementDef, "{1B0856A7-9267-449B-82C8-27BD5821C6BD}", name);
                    
                    desperateUseStatusDef.EffectName = "TFTVDesperateUse";
                    desperateUseStatusDef.ApplicationConditions = new EffectConditionDef[0];
                    desperateUseStatusDef.SingleInstance = true;
                    desperateUseStatusDef.ShowNotification = true;
                    desperateUseStatusDef.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.AlwaysVisible;
                    desperateUseStatusDef.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;
                    desperateUseStatusDef.AccuracyPenaltyStatus = _partingshotAccuracyMalusStatus;

                    desperateUseStatusDef.AllowedTags = new GameTagDef[] {DefCache.GetDef<GameTagDef>("HandgunItem_TagDef")};
                    desperateUseStatusDef.AllowedAbilities = new TacticalAbilityDef[] { DefCache.GetDef<ShootAbilityDef>("Handgun_ShootAbilityDef") };
                    desperateUseStatusDef.RequiresAnyActionPoint = true;

                    newAbility.StatusDef = desperateUseStatusDef;

                    _partingShot = newAbility;
                    Drills.Add(newAbility);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }

            private static ApplyStatusAbilityDef CreateApplyStatusAbilityDef(string name, string guid0, string guid1, string guid2, int wpCost, int apCost, StatusDef statusDef)
            {
                try
                {
                    string locKeyName = $"TFTV_DRILL_{name}_NAME";
                    string locKeyDesc = $"TFTV_DRILL_{name}_DESC";
                    Sprite icon = Helper.CreateSpriteFromImageFile($"Drill_{name}.png");

                    ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("BigBooms_AbilityDef");
                    ApplyStatusAbilityDef newAbility = Helper.CreateDefFromClone(source, guid0, name);
                    newAbility.CharacterProgressionData = Helper.CreateDefFromClone(source.CharacterProgressionData, guid1, name);

                    newAbility.ViewElementDef = Helper.CreateDefFromClone(source.ViewElementDef, guid2, name);
                    newAbility.ViewElementDef.DisplayName1.LocalizationKey = locKeyName;
                    newAbility.ViewElementDef.Description.LocalizationKey = locKeyDesc;
                    newAbility.ViewElementDef.LargeIcon = icon;
                    newAbility.ViewElementDef.SmallIcon = icon;
                    newAbility.AnimType = -1;
                    newAbility.StatusDef = statusDef;

                    newAbility.WillPointCost = wpCost;
                    newAbility.ActionPointCost = apCost;

                    return newAbility;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }



            /* private static void CreateVeiledmarksman()
             {
                 try
                 {

                     string name = "veiledmarksman";
                     string guid0 = "f4c5d6e7-8091-4a2b-b3c4-5d6e7f08192a";
                     string guid1 = "5b6c7d8e-901a-0b1c-2d3e-4f506172839a";
                     string guid2 = "6c7d8e90-1a0b-1c2d-3e4f-506172839a4b";
                     string guid3 = "{1BD0CBE9-5194-4AA6-B569-ADCDC4694F15}";
                     string guid4 = "{2C90DD59-BBEC-47D6-A55A-C72CF5ADE172}";
                     string guid5 = "{B0AC46FD-D060-4391-9BC3-9F763672D318}";

                     string locKeyName = $"TFTV_DRILL_{name}_NAME";
                     string locKeyDesc = $"TFTV_DRILL_{name}_DESC";
                     Sprite icon = Helper.CreateSpriteFromImageFile($"Drill_{name}.png");

                     TacStatsModifyStatusDef sourceStatus = DefCache.GetDef<TacStatsModifyStatusDef>("Slowed_StatusDef");
                     TacStatsModifyStatusDef newStatus = Helper.CreateDefFromClone(sourceStatus, guid0, name);

                     List<TacticalItemDef> armourItems = Repo.GetAllDefs<TacticalItemDef>().Where(i => i.Tags.Contains(Shared.SharedGameTags.ArmorTag)
                    && i.Tags.Contains(DefCache.GetDef<GameTagDef>("Sniper_ClassTagDef"))).ToList();

                     int requiredCount = 3;

                     ActorHasAtLeastItemsEffectConditionDef newStatusApplicationCondition = Helper.CreateDefFromClone<ActorHasAtLeastItemsEffectConditionDef>(null, guid1, name);
                     newStatusApplicationCondition.Items = armourItems;
                     newStatusApplicationCondition.RequiredCount = requiredCount;

                     // ActorIsInCoverEffectConditionDef newStatusApplicationCondition2 = Helper.CreateDefFromClone<ActorIsInCoverEffectConditionDef>(null, guid2, name);

                     newStatus.ApplicationConditions = new EffectConditionDef[] { newStatusApplicationCondition };
                     newStatus.StatsModifiers = new StatsModifierPopup[]
                     {
                     new StatsModifierPopup
                         {
                         StatModification = new StatModification(
                         StatModificationType.Add,
                         "Stealth",
                         0.5f,
                         null, // source argument required by constructor
                         0f    // applicationValue argument required by constructor
                     ),

                         PopupInfoMessageId = null
                     }
                     };

                     ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("MasterMarksman_AbilityDef");
                     ApplyStatusAbilityDef newAbility = Helper.CreateDefFromClone(source, guid3, name);
                     newAbility.CharacterProgressionData = Helper.CreateDefFromClone(source.CharacterProgressionData, guid4, name);

                     newAbility.ViewElementDef = Helper.CreateDefFromClone(source.ViewElementDef, guid5, name);
                     newAbility.ViewElementDef.DisplayName1.LocalizationKey = locKeyName;
                     newAbility.ViewElementDef.Description.LocalizationKey = locKeyDesc;
                     newAbility.ViewElementDef.LargeIcon = icon;
                     newAbility.ViewElementDef.SmallIcon = icon;
                     newAbility.AnimType = -1;
                     newAbility.StatusDef = newStatus;
                     newAbility.TargetApplicationConditions = new EffectConditionDef[] { };
                     newAbility.StatusApplicationTrigger = StatusApplicationTrigger.ActorMovedAura;
                     _veiledMarksman = newAbility;

                     _drills.Add(newAbility);
                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                     throw;
                 }


             }*/

            private static void CreateMightMakesRightAddStatusAbilityDef(string name, string guid0, string guid1, string guid2)
            {
                try
                {
                    string locKeyName = $"TFTV_DRILL_{name}_NAME";
                    string locKeyDesc = $"TFTV_DRILL_{name}_DESC";
                    Sprite icon = Helper.CreateSpriteFromImageFile($"Drill_{name}.png");

                    var MightMakesRightStatusDef = CreateMightMakesRightStatus();


                    ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("MasterMarksman_AbilityDef");
                    ApplyStatusAbilityDef newAbility = Helper.CreateDefFromClone(source, guid0, name);
                    newAbility.CharacterProgressionData = Helper.CreateDefFromClone(source.CharacterProgressionData, guid1, name);

                    newAbility.ViewElementDef = Helper.CreateDefFromClone(source.ViewElementDef, guid2, name);
                    newAbility.ViewElementDef.DisplayName1.LocalizationKey = locKeyName;
                    newAbility.ViewElementDef.Description.LocalizationKey = locKeyDesc;
                    newAbility.ViewElementDef.LargeIcon = icon;
                    newAbility.ViewElementDef.SmallIcon = icon;
                    newAbility.AnimType = -1;
                    newAbility.StatusDef = MightMakesRightStatusDef;
                    newAbility.TargetApplicationConditions = new EffectConditionDef[] { };


                    _mightMakesRight = newAbility;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void CreateAksuSprint()
            {
                try
                {

                    //ActorHasItemsEffectConditionDef

                    string name = "aksusprintdrill";
                    string guid0 = "{DE4F59B3-E04A-42EC-891A-E234A75D7F43}";
                    string guid1 = "{AE8A4E57-571E-4916-9E30-81F3968325EC}";
                    string guid2 = "05d6e7f8-192a-4b3c-c4d5-6e7f08192a3b";
                    string guid3 = "7d8e901a-0b1c-2d3e-4f50-6172839a4b5c";
                    string guid4 = "8e901a0b-1c2d-3e4f-5061-72839a4b5c6d";

                    TacStatsModifyStatusDef sourceStatus = DefCache.GetDef<TacStatsModifyStatusDef>("Slowed_StatusDef");
                    TacStatsModifyStatusDef newStatus = Helper.CreateDefFromClone(sourceStatus, guid0, name);

                    List<TacticalItemDef> armourItems = new List<TacticalItemDef>()
                    {
                        DefCache.GetDef<TacticalItemDef>("AN_Berserker_Helmet_BodyPartDef"),
                        DefCache.GetDef<TacticalItemDef>("AN_Berserker_Torso_BodyPartDef"),
                        DefCache.GetDef<TacticalItemDef>("AN_Berserker_Legs_ItemDef"),
                    };

                    int requiredCount = 3;

                    float bonusSpeed = armourItems.Sum(i => i.BodyPartAspectDef.Speed);

                    newStatus.ApplicationConditions = new EffectConditionDef[] { CreateActorHasAtLeastItemsEffectConditionDef(name, guid1, armourItems, requiredCount) };

                    newStatus.StatsModifiers = new StatsModifierPopup[]
                    {
                    new StatsModifierPopup
                        {
                        StatModification = new StatModification(
                        StatModificationType.Add,
                        "Speed",
                        6,
                        null, // source argument required by constructor
                        0f    // applicationValue argument required by constructor
                    ),

                        PopupInfoMessageId = null
                    }
                    };

                    string locKeyName = $"TFTV_DRILL_{name}_NAME";
                    string locKeyDesc = $"TFTV_DRILL_{name}_DESC";
                    Sprite icon = Helper.CreateSpriteFromImageFile($"Drill_{name}.png");

                    ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("MasterMarksman_AbilityDef");
                    ApplyStatusAbilityDef newAbility = Helper.CreateDefFromClone(source, guid2, name);
                    newAbility.CharacterProgressionData = Helper.CreateDefFromClone(source.CharacterProgressionData, guid3, name);

                    newAbility.ViewElementDef = Helper.CreateDefFromClone(source.ViewElementDef, guid4, name);
                    newAbility.ViewElementDef.DisplayName1.LocalizationKey = locKeyName;
                    newAbility.ViewElementDef.Description.LocalizationKey = locKeyDesc;
                    newAbility.ViewElementDef.LargeIcon = icon;
                    newAbility.ViewElementDef.SmallIcon = icon;
                    newAbility.AnimType = -1;
                    newAbility.StatusDef = newStatus;
                    newAbility.TargetApplicationConditions = new EffectConditionDef[] { };

                    Drills.Add(newAbility);


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }

            private static ActorHasAtLeastItemsEffectConditionDef CreateActorHasAtLeastItemsEffectConditionDef(string name, string guid, List<TacticalItemDef> items, int requiredCount)
            {
                try
                {
                    ActorHasAtLeastItemsEffectConditionDef newStatusApplicationCondition = Helper.CreateDefFromClone<ActorHasAtLeastItemsEffectConditionDef>(null, guid, name);
                    newStatusApplicationCondition.Items = items;
                    newStatusApplicationCondition.RequiredCount = requiredCount;

                    return newStatusApplicationCondition;


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }


            private static ApplyStatusAbilityDef ForLater()
            {
                try
                {

                    string name = "override";
                    string locKeyName = $"TFTV_DRILL_{name}_NAME";
                    string locKeyDesc = $"TFTV_DRILL_{name}_DESC";

                    string guid0 = "f2711bfc-b4cb-46dd-bb9f-599a88c1ebff";
                    string guid1 = "f7ce1c44-1447-41a3-8112-666c82451e25";
                    string guid2 = "0324925f-e318-40b6-ac8c-b68033823cd9";
                    string guid3 = "c3d9e8f0-3b4a-4c5d-e6f7-8091a2b3c4d5";
                    string guid4 = "d4e5f607-1829-4a5b-6c7d-8e9f0a1b2c3d";
                    string guid5 = "e5f60718-293a-4b5c-6d7e-8f901a2b3c4d";
                    string guid6 = "f6071829-3a4b-5c6d-7e8f-9010a1b2c3d4";
                    string guid7 = "0718293a-4b5c-6d7e-8f90-10a1b2c3d4e5";
                    string guid8 = "18293a4b-5c6d-7e8f-9010-a1b2c3d4e5f6";


                    Sprite icon = Helper.CreateSpriteFromImageFile($"Drill_{name}.png");

                    ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("ManualControl_AbilityDef");


                    source.TargetingDataDef.Origin.TargetEnemies = true;


                    ApplyStatusAbilityDef newAbility = Helper.CreateDefFromClone(
                        source,
                       guid0,
                        name);
                    newAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                        source.CharacterProgressionData,
                        guid1,
                        name);
                    newAbility.ViewElementDef = Helper.CreateDefFromClone(
                        source.ViewElementDef,
                       guid2,
                        name);
                    newAbility.TargetingDataDef = Helper.CreateDefFromClone(
                        source.TargetingDataDef,
                        guid3,
                        name);

                    newAbility.CharacterProgressionData.RequiredStrength = 0;
                    newAbility.CharacterProgressionData.RequiredWill = 0;
                    newAbility.CharacterProgressionData.RequiredSpeed = 0;
                    newAbility.ViewElementDef.DisplayName1.LocalizationKey = locKeyName; // displayName;
                    newAbility.ViewElementDef.Description.LocalizationKey = locKeyDesc; // = description;
                    newAbility.ViewElementDef.LargeIcon = icon;
                    newAbility.ViewElementDef.SmallIcon = icon;
                    newAbility.TargetApplicationConditions = new EffectConditionDef[] { source.TargetApplicationConditions[0] };
                    newAbility.TargetingDataDef.Origin.TargetEnemies = true;
                    newAbility.TargetingDataDef.Origin.TargetFriendlies = false;


                    // Create a new Bash ability by cloning from standard Bash with fixed damage and shock values


                    ApplyStatusAbilityDef newActualAbility = Helper.CreateDefFromClone(
                        source,
                       guid3,
                        $"{name}_actual");

                    newActualAbility.ViewElementDef = Helper.CreateDefFromClone(
                        source.ViewElementDef,
                        guid4,
                        name);
                    newActualAbility.ViewElementDef.ShowInStatusScreen = false;
                    newActualAbility.ViewElementDef.DisplayName1.LocalizationKey = locKeyName; // displayName;
                    newActualAbility.ViewElementDef.Description.LocalizationKey = locKeyDesc; // = new LocalizedTextBind($"Deal {(int)bashDamage} damage and {(int)bashShock} shock damage to an adjacent target.", PRMBetterClassesMain.Main.Settings.DoNotLocalizeChangedTexts);
                    newActualAbility.ViewElementDef.LargeIcon = icon;
                    newActualAbility.ViewElementDef.SmallIcon = icon;
                    //  newActualAbility.TargetApplicationConditions = new EffectConditionDef[] { newActualAbility.TargetApplicationConditions[0]};



                    // Create a status to apply the bash ability to the actor
                    AddAbilityStatusDef addNewAbiltyStatus = Helper.CreateDefFromClone( // Borrow status from Deplay Beacon (final mission)
                        DefCache.GetDef<AddAbilityStatusDef>("E_AddAbilityStatus [DeployBeacon_StatusDef]"),
                        guid5,
                        $"E_ApplyNewBashAbilityEffect [{name}]");
                    addNewAbiltyStatus.DurationTurns = -1;
                    addNewAbiltyStatus.SingleInstance = true;
                    addNewAbiltyStatus.ExpireOnEndOfTurn = false;
                    addNewAbiltyStatus.AbilityDef = newActualAbility;

                    // Create an effect that removes the standard Bash from the actors abilities
                    RemoveAbilityEffectDef removeRegularAbilityEffect = Helper.CreateDefFromClone(
                        DefCache.GetDef<RemoveAbilityEffectDef>("RemoveAuraAbilities_EffectDef"),
                        guid6,
                        $"E_RemoveRegularAbilityEffect [{name}]");
                    removeRegularAbilityEffect.AbilityDefs = new AbilityDef[] { source };

                    // Create a status that applies the remove ability effect to the actor
                    TacEffectStatusDef applyRemoveAbilityEffectStatus = Helper.CreateDefFromClone(
                        DefCache.GetDef<TacEffectStatusDef>("Mist_spawning_StatusDef"),
                        guid7,
                        $"E_ApplyRemoveAbilityEffect [{name}]");
                    applyRemoveAbilityEffectStatus.EffectName = "";
                    applyRemoveAbilityEffectStatus.DurationTurns = -1;
                    applyRemoveAbilityEffectStatus.ExpireOnEndOfTurn = false;
                    applyRemoveAbilityEffectStatus.Visuals = null;
                    applyRemoveAbilityEffectStatus.EffectDef = removeRegularAbilityEffect;
                    applyRemoveAbilityEffectStatus.StatusAsEffectSource = false;
                    applyRemoveAbilityEffectStatus.ApplyOnStatusApplication = true;
                    applyRemoveAbilityEffectStatus.ApplyOnTurnStart = true;

                    // Create a multi status to hold all statuses that Takedown applies to the actor
                    MultiStatusDef multiStatus = Helper.CreateDefFromClone( // Borrow multi status from Rapid Clearance
                        DefCache.GetDef<MultiStatusDef>("E_MultiStatus [RapidClearance_AbilityDef]"),
                        guid8,
                        name);
                    multiStatus.Statuses = new StatusDef[] { addNewAbiltyStatus, applyRemoveAbilityEffectStatus };

                    newAbility.StatusDef = multiStatus;

                    //TacActorAimingAbilityAnimActionDef noWeaponBashAnim = Repo.GetAllDefs<TacActorAimingAbilityAnimActionDef>().FirstOrDefault(aa => aa.name.Equals("E_NoWeaponBash [Soldier_Utka_AnimActionsDef]"));
                    //if (!noWeaponBashAnim.AbilityDefs.Contains(bashAbility))
                    //{
                    //    noWeaponBashAnim.AbilityDefs = noWeaponBashAnim.AbilityDefs.Append(bashAbility).ToArray();
                    //}

                    // Adding new bash ability to proper animations
                    foreach (TacActorAimingAbilityAnimActionDef animActionDef in Repo.GetAllDefs<TacActorAimingAbilityAnimActionDef>().Where(aad => aad.name.Contains("Soldier_Utka_AnimActionsDef")))
                    {
                        if (animActionDef.AbilityDefs != null && animActionDef.AbilityDefs.Contains(source) && !animActionDef.AbilityDefs.Contains(newActualAbility))
                        {
                            animActionDef.AbilityDefs = animActionDef.AbilityDefs.Append(newActualAbility).ToArray();
                            PRMLogger.Debug("Anim Action '" + animActionDef.name + "' set for abilities:");
                            foreach (AbilityDef ad in animActionDef.AbilityDefs)
                            {
                                PRMLogger.Debug("  " + ad.name);
                            }
                            PRMLogger.Debug("----------------------------------------------------", false);
                        }
                    }

                    Drills.Add(newAbility);

                    return newAbility;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static TacStrengthDamageMultiplierStatusDef CreateMightMakesRightStatus()
            {
                try
                {

                    TacStrengthDamageMultiplierStatusDef def = Helper.CreateDefFromClone<TacStrengthDamageMultiplierStatusDef>(null, "{0CF1645E-B231-4A1F-BADB-A6F137790FCD}", "TFTV_TacStrengthDM_StatusDef");
                    def.MultiplierType = DamageMultiplierType.Outgoing;
                    def.MeleeStandardDamageType = DefCache.GetDef<DamageTypeBaseEffectDef>("Melee_Standard_DamageTypeDef");

                    def.DurationTurns = -1;
                    def.ExpireOnEndOfTurn = true;
                    def.SingleInstance = true;
                    def.ShowNotification = false;
                    def.VisibleOnPassiveBar = false;
                    def.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.Hidden;
                    def.ApplicationConditions = new EffectConditionDef[] { };
                    def.Visuals = Helper.CreateDefFromClone(DefCache.GetDef<ViewElementDef>("E_ViewElement [ApplyStatus_MindControlImmunity_AbilityDef]"), "{A2DE7E41-09E3-4AF2-A49C-7E19A91972F4}", def.name);
                    def.EventOnApply = null;
                    def.EventOnUnapply = null;


                    return def;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            

           

            private static void CreateSkillDrillTag()
            {
                try
                {
                    _drillSkillTagDef = Helper.CreateDefFromClone(
                        DefCache.GetDef<SkillTagDef>("AttackAbility_SkillTagDef"),
                        "b1a4c8e1-6f4d-4d3b-9f7a-1c2e5f3e6b7c",
                        "Drill_SkillTagDef");


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            


            private static void CreateDrills()
            {
                try
                {
                    CreateShockDisciplineStatus();

                    _causticJamming = CreateDrillNominalAbility("causticjamming", "8d4e5f60-9192-122b-d4e5-f60718293a4b", "b5c6d7e8-9010-ab1c-2d3e-4f506172839a", "c5d6e7f8-0910-a1b2-c3d4-e5f60718293a");

                    CreateDrillNominalAbility("fieldpromotion", "6a2e2b2d-5c8f-4a09-bb27-d4df9e34a6a1", "5b4d8b47-8e7e-4a5f-8c4b-5e2f5e9f7a31", "df3b3e32-1b04-4f75-8fd9-0f3c3d8a9f63");
                    _mentorProtocol = CreateDrillNominalAbility("mentorprotocol", "a2b1f9c3-0c32-4d9f-9a7b-0c2d18ce6ab0", "9d2a3f7b-1a53-4a8c-a1ab-4b6d3e2f9a22", "7b1f8e9c-3d4a-4e8c-9b8a-2c5f7a9e0b31");
                    PassiveModifierAbilityDef pinpointToss = CreateDrillNominalAbility("pinpointtoss", "b59a3b5a-0b6e-4abf-9c7f-1db713e0b7a0", "c0e37c4a-4b1f-4f3e-8c2a-5f4e6d7c8a91", "e7f1a0b2-6d38-4d1e-9c3b-7a1d9e0f2b64");
                    CreateDrillNominalAbility("shockdrop", "c1f7c2e4-9a2d-4b8c-ae3e-2c4b5d6e7f81", "f0a1b2c3-4d5e-6f70-8a91-b2c3d4e5f607", "0a1b2c3d-4e5f-6071-8293-a4b5c6d7e8f9");
                    CreateDrillNominalAbility("onehandedgrip", "16e7f809-2a3b-4c5d-d6e7-8f091a2b3c4d", "901a0b1c-2d3e-4f50-6172-839a4b5c6d7e", "0a0b1c2d-3e4f-5061-7283-9a4b5c6d7e8f");
                   _shockDiscipline = CreateDrillNominalAbility("shockdiscipline", "27f8091a-3b4c-5d6e-e7f8-091a2b3c4d5e", "1a0b1c2d-3e4f-5061-7283-9a4b5c6d7e8f", "2b1c2d3e-4f50-6172-839a-4b5c6d7e8f90");

                    _snapBrace = CreateDrillNominalAbility("snapbrace", "38091a2b-4c5d-6e7f-f809-1a2b3c4d5e6f", "2c3d4e5f-6172-839a-4b5c-6d7e8f9010ab", "3d4e5f61-7283-9a4b-5c6d-7e8f9010ab1c");

                   
                    CreateDrillNominalAbility("partingshotpdw", "5a1b2c3d-6e7f-8090-a1b2-c3d4e5f60718", "50617283-9a4b-5c6d-7e8f-9010ab1c2d3e", "6172839a-4b5c-6d7e-8f90-10ab1c2d3e4f");
                    CreateDrillNominalAbility("quickaimfinisher", "6b2c3d4e-7f80-9010-b2c3-d4e5f6071829", "72839a4b-5c6d-7e8f-9010-ab1c2d3e4f50", "839a4b5c-6d7e-8f90-10ab-1c2d3e4f5061");
                    _shieldedRiposte = CreateDrillNominalAbility("shieldedriposte", "7c3d4e5f-8091-011a-c3d4-e5f60718293a", "9a4b5c6d-7e8f-9010-ab1c-2d3e4f506172", "a94b5c6d-7e8f-9010-ab1c-2d3e4f506173");

                    _toxicLink = CreateDrillNominalAbility("toxiclink", "9e5f6071-a2a3-233c-e5f6-0718293a4b5c", "c6d7e8f9-1011-b2c3-d4e5-f60718293a4b", "d6e7f809-1112-c3d4-e5f6-0718293a4b5c");
                    CreateDrillNominalAbility("pounceprotocol", "af607182-b3b4-344d-f607-18293a4b5c6d", "d7e8f901-1213-c4d5-e6f7-08192a3b4c5d", "e7f80912-1314-d5e6-f7f8-192a3b4c5d6e");

                    CreateDrillNominalAbility("lightenedharness", "c172839a-d5d6-566f-0809-1a2b3c4d5e6f", "f9012334-1617-e8f9-f001-4c5d6e7f8091", "09012334-1718-f9f0-0102-5d6e7f8091a2");


                    CreateDrillNominalAbility("ordnanceresupply", "d2839a4b-e6e7-6770-0910-1b2c3d4e5f60", "01233445-1819-f0f1-0203-6e7f8091a2b3", "12334456-1920-01f2-0304-7f8091a2b3c4");
                    _viralPuppeteer = CreateDrillNominalAbility("viralpuppeteer", "e39a4b5c-f7f8-7881-0a11-2c3d4e5f6071", "23344556-2021-10f3-0405-8091a2b3c4d5", "33445566-2122-11f4-0506-91a2b3c4d5e6");
                    _virulentGrip = CreateDrillNominalAbility("virulentgrip", "f4a5b6c7-0809-8992-1b22-3d4e5f607182", "34455667-2223-12f5-0607-a2b3c4d5e6f7", "45566778-2324-13f6-0708-b3c4d5e6f708");
                    _packLoyalty = CreateDrillNominalAbility("packloyalty", "05b6c7d8-191a-9aa3-2c33-4e5f60718293", "45566789-2425-14f7-0809-c4d5e6f70819", "56677889-2526-15f8-0910-d5e6f708192a");




                    CreateMightMakesRightAddStatusAbilityDef("mightmakesright", "d2a3b4c5-6e7f-4819-9a0b-1c2d3e4f5a60", "1e2f3a4b-5c6d-7081-92a3-b4c5d6e7f809", "2a3b4c5d-6e7f-8091-a2b3-c4d5e6f70819");

                    Drills.Add(_mightMakesRight);

                    _drawfireStatus = CreateDummyStatus("drawfire", "{65B5A8AC-FBB0-42CC-BC2E-EB9DB7460FC8}", "{7557CA9F-DAB8-4AE1-AF1A-853261A4CF05}");
                    Drills.Add(
                        CreateApplyStatusAbilityDef("drawfire", "8f7c0a6a-6b63-4b01-9d69-6f7e3d4a4b9a", "f2a5a2d1-0c1f-4c28-8a3a-2f4a0cc2fd3c", "3a0f4d0b-0a8f-4b8f-a8cc-1f4f4f3c3f9d", 2, 0, _drawfireStatus));

                    GameTagDef grenadeTag = (GameTagDef)Repo.GetDef("318dd3ff-28f0-1bb4-98bc-39164b7292b6"); // GrenadeItem_TagDef

                    pinpointToss.ItemTagStatModifications = new EquipmentItemTagStatModification[]
                    {
                        new EquipmentItemTagStatModification
                        {
                            ItemTag = grenadeTag,
                            EquipmentStatModification = new ItemStatModification
                            {
                                Modification = StatModificationType.Add,
                                TargetStat = PhoenixPoint.Common.Entities.StatModificationTarget.Accuracy,
                                Value = 0.5f
                            }
                        }
                    };



                    CreateMarkedWatch();
                    CreateOverride();
                    CreateAksuSprint();
                    CreateHeavyconditioning();

                    CreatePartingShotAccuracyaMalusStatus();
                    CreatePartingShootAbility();
                    //CreateVeiledmarksman();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CreatePartingShotAccuracyaMalusStatus()
            {
                try 
                {
                    string name = "partingshot";

                    StatMultiplierStatusDef newStatusDef = Helper.CreateDefFromClone(
                (StatMultiplierStatusDef)Repo.GetDef("4a6f7cc4-1bd6-45a5-b572-053963966b07"),
                "{6F7D17D0-477F-4371-BCCA-684E1F42376D}",
                name);

                    _partingshotAccuracyMalusStatus = newStatusDef;
                   
                }  
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }


            private static void CreateMarkedWatch()
            {
                try
                {
                    string name = "markedwatch";
                    string guid0 = "83dbb6f5-6a07-4f66-8b24-88a9c2c3b2e2";
                    string guid1 = "a7ce3b3d-3db5-4e52-b83e-1b6c1f1f9b22";
                    string guid2 = "c90b0e5a-5e70-4bb8-96d0-8b7e8cc6d7f4";
                    string guid3 = "{A986E625-0E3A-4569-AAF6-9DF759ADFF6F}";
                    string guid4 = "{5E5D24F1-A230-45D8-9CBA-1C9C1D640468}";


                    string locKeyName = $"TFTV_DRILL_{name}_NAME";
                    string locKeyDesc = $"TFTV_DRILL_{name}_DESC";

                    Sprite icon = Helper.CreateSpriteFromImageFile($"Drill_{name}.png");

                    DamageMultiplierStatusDef newStatus = CreateDummyStatus(name, guid3, guid4);


                    ApplyStatusAbilityDef sourceAbility = DefCache.GetDef<ApplyStatusAbilityDef>("MarkedForDeath_AbilityDef");


                    ApplyStatusAbilityDef newTacticalAbility = Helper.CreateDefFromClone(
                        sourceAbility,
                        guid0,
                        name);

                    newTacticalAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                        sourceAbility.CharacterProgressionData,
                        guid1,
                        name);

                    newTacticalAbility.ViewElementDef = Helper.CreateDefFromClone(
                        sourceAbility.ViewElementDef,
                        guid2,
                        name);


                    newTacticalAbility.WillPointCost = 2;
                    newTacticalAbility.StatusDef = newStatus;
                    newTacticalAbility.TargetApplicationConditions = new EffectConditionDef[] { };
                    newTacticalAbility.ViewElementDef.DisplayName1.LocalizationKey = locKeyName;
                    newTacticalAbility.ViewElementDef.Description.LocalizationKey = locKeyDesc;
                    newTacticalAbility.ViewElementDef.LargeIcon = icon;
                    newTacticalAbility.ViewElementDef.SmallIcon = icon;

                    newTacticalAbility.SkillTags = new SkillTagDef[] { _drillSkillTagDef };

                    Drills.Add(newTacticalAbility);
                    _markedwatchStatus = newStatus;



                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


            private static PassiveModifierAbilityDef CreateDrillNominalAbility(string name, string guid0, string guid1, string guid2)
            {
                try
                {
                    string locKeyName = $"TFTV_DRILL_{name}_NAME";
                    string locKeyDesc = $"TFTV_DRILL_{name}_DESC";
                    Sprite icon = Helper.CreateSpriteFromImageFile($"Drill_{name}.png");

                    PassiveModifierAbilityDef sourceAbility = DefCache.GetDef<PassiveModifierAbilityDef>("SelfDefenseSpecialist_AbilityDef");


                    PassiveModifierAbilityDef newTacticalAbility = Helper.CreateDefFromClone(
                        sourceAbility,
                        guid0,
                        name);

                    newTacticalAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                        sourceAbility.CharacterProgressionData,
                        guid1,
                        name);

                    newTacticalAbility.ViewElementDef = Helper.CreateDefFromClone(
                        sourceAbility.ViewElementDef,
                        guid2,
                        name);


                    newTacticalAbility.StatModifications = new ItemStatModification[0];
                    newTacticalAbility.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                    newTacticalAbility.ViewElementDef.DisplayName1.LocalizationKey = locKeyName;
                    newTacticalAbility.ViewElementDef.Description.LocalizationKey = locKeyDesc;
                    newTacticalAbility.ViewElementDef.LargeIcon = icon;
                    newTacticalAbility.ViewElementDef.SmallIcon = icon;

                    newTacticalAbility.SkillTags = new SkillTagDef[] { _drillSkillTagDef };

                    Drills.Add(newTacticalAbility);

                    return newTacticalAbility;
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
            internal class DesperateShot 
            {
                // Utility to find our status on an actor
                static class DesperateUtil
                {
                    public static DesperateUseStatus GetDesperateStatus(TacticalActor actor)
                        => actor?.Status?.GetStatuses<DesperateUseStatus>()?.FirstOrDefault();
                }



                //Allow activation even with insufficient AP (when our status says OK)
                [HarmonyPatch(typeof(TacticalAbility), "get_ActionPointRequirementSatisfied")]
                static class TacticalAbility_CanActivate_Desperate_Patch
                {
                    static void Postfix(TacticalAbility __instance, ref bool __result)
                    {
                        try
                        {
                            if (__result) return; // already allowed

                            var actor = __instance.TacticalActor;

                            if(actor.GetAbilityWithDef<PassiveModifierAbility>(_snapBrace)!=null && __instance is DeployShieldAbility) 
                            {
                                __result = true;
                                return;
                            }

                            var des = DesperateUtil.GetDesperateStatus(actor);
                            if (des == null) return;

                            // Must match allow-lists and meet extra requirements
                            if (!des.Matches(__instance) || !des.RequirementsMet())
                                return;

                            __result = true;
                        }
                        catch (Exception e) { TFTVLogger.Always($"[TFTV] Desperate CanActivate patch failed: {e}"); }
                    }
                }

               
            }


            internal class MarkedWatch
            {


                [HarmonyPatch(typeof(TacticalLevelController), "TriggerOverwatch")]
                internal static class TacticalLevelController_TriggerOverwatch
                {
                    static bool Prefix(TacticalLevelController __instance, TacticalActor target)
                    {
                        try
                        {
                            if (target == null || !target.TacticalFaction.TacticalActors.Any(a => a.Status != null && a.Status.HasStatus(_markedwatchStatus)))
                            {
                                return true;
                            }




                            if (__instance.OverwatchTarget == null)
                            {
                                List<Status> listOfMarkedForOverwatch = (from actor in target.TacticalFaction.TacticalActors
                                                                         let status = actor.Status.GetStatusesByName(_markedwatchStatus.EffectName).FirstOrDefault()
                                                                         where status != null
                                                                         select status).ToList();


                                List<OverwatchStatus> listOfOverwatches = (from actor in __instance.Map.GetActors<TacticalActor>()
                                                                           let status = actor.Status.GetStatus<OverwatchStatus>()
                                                                           where status != null && !actor.DuringOwnTurn && actor.RelationTo(target) == FactionRelation.Enemy
                                                                           orderby (actor.Pos - target.Pos).sqrMagnitude
                                                                           select status).ToList();




                                if (target.Status.HasStatus(_markedwatchStatus) && listOfMarkedForOverwatch.Count() == 1)
                                {
                                    // TFTVLogger.Always($"target.Status.HasStatus(_markedwatchStatus) && listOfMarkedForOverwatch.Count()==1");

                                }
                                else
                                {


                                    List<TacticalActor> actorsWithMarkedForOverwatchTargets = (from actor in listOfMarkedForOverwatch
                                                                                               select actor.Source as TacticalActor).ToList();


                                    if (!target.Status.HasStatus(_markedwatchStatus))
                                    {

                                        listOfOverwatches.RemoveAll(s => actorsWithMarkedForOverwatchTargets.Contains(s.TacticalActor));
                                    }
                                    else
                                    {
                                        TacStatus tacStatus = (TacStatus)target.Status.GetStatusesByName(_markedwatchStatus.EffectName).FirstOrDefault();
                                        TacticalActor sourceActor = tacStatus.Source as TacticalActor;
                                        if (sourceActor != null)
                                        {
                                            listOfOverwatches.RemoveAll(s => actorsWithMarkedForOverwatchTargets.Contains(s.TacticalActor) && s.TacticalActor != sourceActor);
                                        }
                                    }
                                }

                                if (listOfOverwatches.Count > 0)
                                {
                                    // TFTVLogger.Always($"before final check");

                                    MethodInfo methodInfo = typeof(TacticalLevelController)
        .GetMethod("ExecuteOverwatch", BindingFlags.Instance | BindingFlags.NonPublic);

                                    var enumerator = (IEnumerator<NextUpdate>)methodInfo.Invoke(
                                        __instance,
                                        new object[] { target, listOfOverwatches }
                                    );

                                    __instance.Timing.Start(enumerator, NextUpdate.ThisFrame);

                                    //   TFTVLogger.Always($"final check cleared!");
                                }
                            }
                            return false;
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            return true;
                        }
                    }
                }
            }
            internal class DrawFire
            {



                [HarmonyPatch(typeof(AIUtil), nameof(AIUtil.GetEnemyWeight))]
                internal static class Patch_AIUtil_GetEnemyWeight
                {
                    // Config
                    private const float Multiplier = 100f;



                    static void Postfix(AIBlackboard blackboard, TacticalActorBase enemy, ref float __result)
                    {
                        try
                        {
                            if (HasTauntStatus(enemy))
                            {
                                TFTVLogger.Always($"{enemy?.DisplayName} initial score is {__result}");
                                __result *= Multiplier;
                                TFTVLogger.Always($"{enemy?.DisplayName} new score is {__result}");
                            }


                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }

                    }
                }

                private static bool HasTauntStatus(TacticalActorBase actor)
                {
                    try
                    {

                        if (actor.Status != null && actor.Status.HasStatus(_drawfireStatus))
                        {
                            TFTVLogger.Always($"{actor.DisplayName} has drawFireStatus! should be aggroed");
                            return true;

                        }

                        return false;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        return false;
                    }


                }
            }
            internal class MentorProtocol
            {
                public static bool CheckForMentorProtocolAbility(TacticalActor tacticalActor)
                {
                    try
                    {
                        return tacticalActor.GetAbilityWithDef<PassiveModifierAbility>(_mentorProtocol) != null;

                    }
                    catch (Exception ex)
                    {
                        TFTVLogger.Error(ex);
                        throw;
                    }

                }

            }
            internal class PounceProtocol
            {


            }

           

            internal class ViralPuppeteerToxicLink
            {


                [HarmonyPatch(typeof(TacticalAbility), "TargetFilterPredicate")]
                internal static class TacticalAbility_TargetFilterPredicate_Postfix
                {
                    static void Postfix(
                        TacticalAbility __instance,
                        TacticalTargetData targetData,
                        TacticalActorBase sourceActor,
                        Vector3 sourcePosition,
                        TacticalActorBase targetActor,
                        Vector3 targetPosition,
                        ref bool __result)
                    {
                        try
                        {
                            // If already valid, don’t touch it.
                            if (__result) return;

                            // Ability defs we care about
                            var mindControlAbilityDef = DefCache.GetDef<MindControlAbilityDef>("Priest_MindControl_AbilityDef");
                            var inducePanicAbilityDef = DefCache.GetDef<ApplyStatusAbilityDef>("InducePanic_AbilityDef");
                            var parasychosisAbilityDef = DefCache.GetDef<ApplyEffectAbilityDef>("Parasychosis_AbilityDef");

                            // Status defs / ability mods
                            var poisonStatusDef = DefCache.GetDef<DamageOverTimeStatusDef>("Poison_DamageOverTimeStatusDef");

                            var def = __instance != null ? __instance.TacticalAbilityDef : null;
                            if (def == null) return;

                            // Only proceed if we're on one of the three abilities
                            bool isMindOrPanic = (def == mindControlAbilityDef || def == inducePanicAbilityDef);
                            bool isParasychosis = (def == parasychosisAbilityDef);
                            if (!isMindOrPanic && !isParasychosis) return;

                            // --- Combo gates ---
                            bool comboA =
                                isMindOrPanic &&
                                HasInfected(targetActor) &&
                                HasPassive(sourceActor, _viralPuppeteer);

                            bool comboB =
                                isParasychosis &&
                                HasPoison(targetActor, poisonStatusDef) &&
                                HasPassive(sourceActor, _toxicLink);

                            if (!comboA && !comboB) return;

                            // Re-run the non-range/LOS parts of the predicate. If any of these fail, bail.
                            // 1) Interactable gate
                            if (!def.UsableOnNonInteractableActor && !(targetActor != null && targetActor.Interactable)) return;

                            // 2) Cull tags
                            if (targetData.CullTargetTags.Any() && targetActor.HasGameTags(targetData.CullTargetTags, false)) return;

                            // 3) Self-targeting rules
                            bool isSelf = sourceActor == targetActor;
                            if (isSelf && !targetData.TargetSelf) return;

                            // 4) Require target tags if any are specified
                            if (targetData.TargetTags.Any() && !targetActor.HasGameTags(targetData.TargetTags, false)) return;

                            // 5) Friend/Neutral/Enemy filter
                            bool friendOk = targetData.TargetFriendlies && sourceActor.RelationTo(targetActor) == FactionRelation.Friend;
                            bool neutralOk = targetData.TargetNeutrals && sourceActor.RelationTo(targetActor) == FactionRelation.Neutral;
                            bool enemyOk = targetData.TargetEnemies && sourceActor.RelationTo(targetActor) == FactionRelation.Enemy;
                            if (!friendOk && !neutralOk && !enemyOk) return;

                            // 6) Faction-visibility (NOT line-of-sight) check — preserve original intent
                            bool isRevealed = sourceActor.TacticalFaction.Vision.IsRevealed(targetActor);

                            // Ignore/Any -> leave as true

                            if (!isRevealed) return;

                            // If we reached here, the only reasons it could have failed are range or LOS.
                            // Grant an exception: ignore MinRange/MaxRange and LOS for this specific target.
                            __result = true;
                        }
                        catch (Exception ex)
                        {
                            TFTVLogger.Error(ex);
                        }
                    }

                    // --- Helpers ---

                    // A: target must be infected
                    private static bool HasInfected(TacticalActorBase actor)
                    {
                        if (actor == null || actor.Status == null) return false;
                        // Original API used in your code:
                        return actor.Status.HasStatus<InfectedStatus>();
                    }

                    // B: target must be poisoned (by specific Poison DoT def)
                    private static bool HasPoison(TacticalActorBase actor, DamageOverTimeStatusDef poisonDef)
                    {
                        if (actor == null || actor.Status == null || poisonDef == null) return false;


                        // If this call exists in your version, it will work and is fastest.
                        return actor.Status.HasStatus(poisonDef);

                    }

                    // C: source must have the given passive ability def (works for Viral Puppeteer / Toxic Link)
                    private static bool HasPassive(TacticalActorBase actor, TacticalAbilityDef passiveDef)
                    {
                        if (actor == null || passiveDef == null) return false;

                        // Most passives of this type derive from PassiveModifierAbility.
                        var a = actor.GetAbilityWithDef<PassiveModifierAbility>(passiveDef);
                        return a != null;
                    }
                }

            }

            internal class VirulentGrip
            {
                public static bool CheckForVirulentGripAbility(TacticalActor controllerActor, TacticalActor controlledActor)
                {
                    try
                    {
                        return controllerActor.GetAbilityWithDef<PassiveModifierAbility>(_virulentGrip) != null && controlledActor.Status.HasStatus<InfectedStatus>();

                    }
                    catch (Exception ex)
                    {
                        TFTVLogger.Error(ex);
                        throw;
                    }
                }

            }

            internal class ShieldedRiposte
            {
                private static bool _shieldRiposteDeployingShield = false;

                [HarmonyPatch(typeof(DeployShieldAbility), "Activate")]
                public static class Patch_DeployShieldAbility_Activate
                {
                    public static void Postfix(DeployShieldAbility __instance)
                    {
                        try
                        {
                            TacticalActor tacticalActor = __instance.TacticalActor;

                            if (tacticalActor.GetAbilityWithDef<PassiveModifierAbility>(_shieldedRiposte) != null)
                            {
                                _shieldRiposteDeployingShield = true;

                                TFTVLogger.Always($"tacticalActor?.DisplayName: {tacticalActor?.DisplayName} deploying shield, deployed status? " +
                                    $"{tacticalActor.Status.HasStatus<ShieldDeployedStatus>()}");

                                List<Weapon> weapons = new List<Weapon>(tacticalActor.Equipments.GetWeapons().Where(
                                w => w.IsUsable && w.HasCharges && w.TacticalItemDef.Tags.Contains(DefCache.GetDef<ItemClassificationTagDef>("GunWeapon_TagDef"))
                                && !w.WeaponDef.Tags.Contains(DefCache.GetDef<GameTagDef>("SpitterWeapon_TagDef"))
                                ));

                                if (weapons.Count == 0)
                                {
                                    return;
                                }

                                Weapon bestWeapon = weapons.OrderByDescending(w => w.WeaponDef.EffectiveRange).ToList().First();

                                if (tacticalActor.Equipments.SelectedWeapon == null || tacticalActor.Equipments.SelectedWeapon != bestWeapon)
                                {
                                    TFTVLogger.Always($"Getting ready for shield riposte {tacticalActor.name} was holding {tacticalActor.Equipments?.SelectedWeapon?.DisplayName}, switching to {bestWeapon.DisplayName}");
                                    tacticalActor.Equipments.SetSelectedEquipment(bestWeapon);
                                }

                                _shieldRiposteDeployingShield = false;

                                // TFTVLogger.Always($"at the end, shield now deployed? {tacticalActor.Status.HasStatus<ShieldDeployedStatus>()}");

                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw; // Run original
                        }
                    }
                }

                [HarmonyPatch(typeof(EquipmentComponent), "SetSelectedEquipment")]
                public static class EquipmentComponent_SetSelectedEquipment_Patch
                {
                    static bool IsRiotShield(Equipment equipment) =>
                        equipment != null && equipment.EquipmentDef == DefCache.GetDef<EquipmentDef>("FS_RiotShield_WeaponDef");

                    static bool IsShieldDeployed(TacticalActor actor) =>
                        actor?.Status != null && actor.Status.HasStatus<ShieldDeployedStatus>();

                    public static bool Prefix(EquipmentComponent __instance, Equipment equipment)
                    {
                        var actor = __instance.TacticalActor;
                        var prev = __instance.SelectedEquipment;

                        //   TFTVLogger.Always($"Set selected equipment for {actor?.DisplayName}. Currently selected equipment: {prev?.ItemDef?.name}. Shield deployed? {_shieldRiposteDeployingShield}");

                        // Only intercept when switching away from a deployed riot shield
                        if (!(IsRiotShield(prev) && _shieldRiposteDeployingShield))
                            return true;

                        // TFTVLogger.Always($"passed the check; shield deployed and was wielding it");

                        // 1) Set SelectedEquipment via the property (non-public setter)
                        var selProp = AccessTools.Property(typeof(EquipmentComponent), "SelectedEquipment");
                        selProp?.SetValue(__instance, equipment, null);

                        // Unwire previous selection
                        if (prev != null)
                        {
                            var handlerMethod = AccessTools.Method(typeof(EquipmentComponent), "SelectedEquipmentIsDisabled");
                            var handler = (DamageReceiverImplementation.DamageReceiverStatusChanged)
                                Delegate.CreateDelegate(typeof(DamageReceiverImplementation.DamageReceiverStatusChanged), __instance, handlerMethod);

                            prev.DamageImplementation.ReachedZeroHealth -= handler;
                            prev.SetSelected(selected: false);

                            //TFTVLogger.Always($"Unwired previous equipment {prev.ItemDef.name}");


                        }

                        // Wire new selection
                        if (equipment != null)
                        {
                            var handlerMethod = AccessTools.Method(typeof(EquipmentComponent), "SelectedEquipmentIsDisabled");
                            var handler = (DamageReceiverImplementation.DamageReceiverStatusChanged)
                                Delegate.CreateDelegate(typeof(DamageReceiverImplementation.DamageReceiverStatusChanged), __instance, handlerMethod);

                            equipment.DamageImplementation.ReachedZeroHealth += handler;
                            equipment.SetSelected(selected: true);

                            // TFTVLogger.Always($"Wired new equipment {equipment.ItemDef.name}");
                        }

                        // DrawOut as normal if needed
                        if (equipment != null && equipment.HolsterSlot != null && equipment.EquipmentDef.HolsterWhenNotSelected)
                        {
                            AccessTools.Method(typeof(EquipmentComponent), "DrawOut")
                                ?.Invoke(__instance, new object[] { new AnimationEvent() });

                            TFTVLogger.Always($"Drew out new equipment {equipment.ItemDef.name}");
                        }

                        // 2) Invoke the event by fetching the PRIVATE backing field
                        //    (field-like events compile to a private field with the same name)
                        var evtField = AccessTools.Field(typeof(EquipmentComponent), "EquipmentChangedEvent");
                        var evt = (EquipmentComponent.EquipmentChangedHandler)evtField?.GetValue(__instance);

                        //    TFTVLogger.Always($"Invoking EquipmentChangedEvent, evtField==null: {evtField == null} evt==null: {evt == null}");

                        evt?.Invoke(equipment);

                        return false; // skip original
                    }
                }


            }

            internal class MightMakesRight
            {

                public static float CheckForMightMakesRightDrill(TacticalActor tacticalActor)
                {
                    try
                    {
                        if (tacticalActor.GetAbilityWithDef<PassiveModifierAbility>(_mightMakesRight) != null)
                        {
                            return 1f + tacticalActor.CharacterStats.Endurance.Value.EndValue / 2 / 100;
                        }

                        return 0;

                    }
                    catch (Exception ex)
                    {
                        TFTVLogger.Error(ex);
                        throw;
                    }
                }

            }

            /*  internal class VeiledMarksman
              {
                  [HarmonyPatch(typeof(ApplyStatusAbility), "OnActorMovedInNewTile")]
                  public static class Patch_ApplyStatusAbility_OnActorMovedInNewTile
                  {
                      public static bool Prefix(ApplyStatusAbility __instance, TacticalActorBase movedActor)
                      {
                          try
                          {
                              if (__instance.TacticalAbilityDef != _veiledMarksman) return true;

                              if (movedActor != __instance.TacticalActor) return false;

                              TacticalActor tacticalActor = __instance.TacticalActor;




                              IdleAbility idleAbility = tacticalActor.IdleAbility;
                              CoverType? coverType = (idleAbility != null) ? new CoverType?(idleAbility.ActivePose.CoverInfo.CoverType) : null;

                              TFTVLogger.Always($"{tacticalActor?.DisplayName} moving to a new tile. cover null: {coverType==null}");

                              if (coverType == null || coverType ==CoverType.None)
                              {
                                  TFTVLogger.Always($"the position is not in cover");

                                  Status status = tacticalActor.Status.GetStatusByName(_veiledMarksman.StatusDef.EffectName);
                                  if (status != null)
                                  {
                                      TFTVLogger.Always($"removing veiledMarksman status from {tacticalActor?.DisplayName}");
                                      movedActor.Status.UnapplyStatus(status);
                                  }

                              }
                              else
                              {


                                  TFTVLogger.Always($"the position is in cover");

                                  if (tacticalActor.Status.GetStatusByName(_veiledMarksman.StatusDef.EffectName) == null)
                                  {
                                      TFTVLogger.Always($"adding veiledMarksman status to {tacticalActor?.DisplayName}");
                                      movedActor.Status.ApplyStatus(_veiledMarksman.StatusDef);
                                  }

                              }

                              return false;

                          }
                          catch (Exception ex)
                          {
                              TFTVLogger.Error(ex);
                              throw;
                          }
                      }

                      private static bool TryGetGroundPoint(TacticalActor actor, Vector3 approxPos, out Vector3 ground)
                      {
                          // Same pattern used elsewhere in the game (see IsValidStepOutPos):
                          var cast = actor.TacticalPerception.TacMap.CastFirstFloorAt(
                              approxPos + Vector3.up * actor.TacticalPerception.Height * 0.5f,
                              actor.TacticalNav.FloorLayers);

                          if (cast.HitIsValid)
                          {
                              ground = cast.Point;
                              return true;
                          }

                          ground = approxPos; // fall back
                          return false;
                      }

                      // Most robust “am I in ANY cover?” probe:
                      private static CoverType GetCoverAtTileRobust(TacticalActor actor, bool existingOnly = true)
                      {
                          var tp = actor.TacticalPerception;
                          if (tp == null || !tp.UsesCovers) return CoverType.None;

                          Vector3 pos = actor.Pos;

                          // 1) Snap to floor to avoid voxel/height tolerance issues
                          if (!TryGetGroundPoint(actor, pos, out var ground)) ground = pos;

                          // 2) Use the engine helper that inspects cover in the proper directions
                          //    (this is what many internal systems rely on).
                          var around = tp.TacMap.GetCoversAround(ground, tp.Height, existingOnly);

                          bool sawLow = false;
                          foreach (var c in around)
                          {
                              if (c.CoverType == CoverType.High) return CoverType.High;
                              if (c.CoverType == CoverType.Low) sawLow = true;
                          }

                          // 3) If nothing came back, do a defensive second-pass:
                          //    small back-off + orthogonal rays (handles being flush with the wall)
                          if (!sawLow)
                          {
                              const float EPS = 0.12f; // small local nudge (meters)
                              foreach (var dir in TacticalMap.OrthogonalGridDirections)
                              {
                                  // back off slightly so the ray won’t start inside the collider
                                  Vector3 sample = ground - dir.normalized * EPS;

                                  var info = tp.TacMap.GetCoverInfoInDirection(sample, dir, tp.Height);

                                  TFTVLogger.Always($"dir: {dir} cover: {info.CoverType} at pos {actor.Pos}");

                                  if (info.CoverType == CoverType.High) return CoverType.High;
                                  if (info.CoverType == CoverType.Low) sawLow = true;
                              }
                          }

                          return sawLow ? CoverType.Low : CoverType.None;
                      }



                  }
              }*/

            internal class PackLoyalty
            {
                public static void CheckForPackLoyaltyDrill(TacticalLevelController controller)
                {
                    try
                    {




                    }
                    catch (Exception ex)
                    {
                        TFTVLogger.Error(ex);
                        throw;
                    }

                }


            }

        }
                
    }
}


















