using Base;
using Base.Core;
using Base.Defs;
using Base.Entities;
using Base.Entities.Abilities;
using Base.Entities.Effects;
using Base.Entities.Effects.ApplicationConditions;
using Base.Entities.Statuses;
using HarmonyLib;
using PhoenixPoint.Common.ContextHelp;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Entities.Abilities;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Events.Eventus.Filters;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical;
using PhoenixPoint.Tactical.ContextHelp;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.ActorsInstance;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.StructuralTargets;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using PhoenixPoint.Tactical.View;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;


namespace TFTV
{
    internal class TFTVAncients
    {

        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefRepository Repo = TFTVMain.Repo;

        private static DamageMultiplierStatusDef _addAutoRepairStatus = null;

        private static readonly WeaponDef RightDrill = DefCache.GetDef<WeaponDef>("HumanoidGuardian_Drill_WeaponDef");
        private static readonly WeaponDef RightShield = DefCache.GetDef<WeaponDef>("HumanoidGuardian_RightShield_WeaponDef");
        private static readonly EquipmentDef LeftShield = DefCache.GetDef<EquipmentDef>("HumanoidGuardian_LeftShield_EquipmentDef");
        private static readonly WeaponDef BeamHead = DefCache.GetDef<WeaponDef>("HumanoidGuardian_Head_WeaponDef");
        private static readonly EquipmentDef LeftCrystalShield = DefCache.GetDef<EquipmentDef>("HumanoidGuardian_CrystalShield_EquipmentDef");

        private static readonly ClassTagDef cyclopsTag = DefCache.GetDef<ClassTagDef>("MediumGuardian_ClassTagDef");
        private static readonly ClassTagDef hopliteTag = DefCache.GetDef<ClassTagDef>("HumanoidGuardian_ClassTagDef");

        private static PassiveModifierAbilityDef _ancientsPowerUpAbility = null;
        private static DamageMultiplierStatusDef _ancientsPowerUpStatus = null;
        public static PassiveModifierAbilityDef SelfRepairAbility = null;


        public static readonly string CyclopsBuiltVariable = "CyclopsBuiltVariable";
        //   public static bool LOTAReworkActive = false;
        //  public static bool AutomataResearched = TFTVAncientsGeo.AutomataResearched;

        //This is the number of previous encounters with Ancients. It is added to the Difficulty to determine the number of fully repaired MediumGuardians in battle
        private static int AncientsEncounterCounter = TFTVAncientsGeo.AncientsEncounterCounter;
        private static readonly AlertedStatusDef AlertedStatus = DefCache.GetDef<AlertedStatusDef>("Alerted_StatusDef");
        public static DamageMultiplierStatusDef CyclopsDefenseStatus = null;
        private static readonly StanceStatusDef AncientGuardianStealthStatus = DefCache.GetDef<StanceStatusDef>("AncientGuardianStealth_StatusDef");
        public static DamageMultiplierStatusDef RoboticSelfRepairStatus = null;
        // private static readonly GameTagDef SelfRepairTag = DefCache.GetDef<GameTagDef>("SelfRepair");
        // private static readonly GameTagDef MaxPowerTag = DefCache.GetDef<GameTagDef>("MaxPower");
        public static Dictionary<int, int> CyclopsMolecularDamageBuff = new Dictionary<int, int> { }; //turn number + 0 = none, 1 = mutation, 2 = bionic

        public static List<string> AlertedHoplites = new List<string>();

        private static WipeEnemyFactionObjectiveDef _dummyObjectiveSmelter = null;
        private static StanceStatusDef _dormantStatus = null;

        internal class Defs
        {
            private static readonly ResearchTagDef CriticalResearchTag = DefCache.GetDef<ResearchTagDef>("CriticalPath_ResearchTagDef");


            public static void ChangesToLOTA()
            {
                try
                {

                    Geo();
                    Tactical();


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void Geo()
            {
                try
                {

                    CreateAncientAutomataResearch();
                    CreateExoticMaterialsResearch();
                    CreateLivingCrystalResearch();
                    CreateProteanMutaneResearch();
                    ChangeCostAncientProbe();

                    ChangeSchemataMissionRequirement();
                    ChangeAncientSiteExploration();

                    RemovePandoranVirusResearchRequirement();
                    CreateEventsForLOTA();
                    ChangeAncientDefenseMission();
                    ChangeAncientSiteMissions();

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void Tactical()
            {
                try
                {
                    CreateAncientStatusEffects();
                    CreateParalysisDamageImmunity();

                    AddHoplitePortrait();
                    CyclopsMindCrushEffect();
                    ChangeHoplites();
                    ChangeAncientsWeapons();

                    ChangeImpossibleWeapons();

                    ModifyCyclops();
                    CyclopsJoinStreamsAttack();
                    CreateDormantStatus();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }


            }


            private static void CreateDormantStatus()
            {
                try
                {
                    StanceStatusDef newStealthStance = Helper.CreateDefFromClone(AncientGuardianStealthStatus, "{6A3330F9-8654-43EC-BEB0-ECB59E12E3AF}", "DormantStatus");
                    newStealthStance.StatModifications = new ItemStatModification[]
                    {
                        new ItemStatModification
                        {
                            TargetStat = StatModificationTarget.Stealth,
                            Value = 1,
                            Modification = StatModificationType.Add,
                        },
                        new ItemStatModification
                        {
                            TargetStat = StatModificationTarget.Perception,
                            Value = -60,
                            Modification = StatModificationType.Add

                        },
                        new ItemStatModification
                        {
                            TargetStat = StatModificationTarget.HearingRange,
                            Value = -15,
                            Modification = StatModificationType.Add
                        }
                    };

                    _dormantStatus = newStealthStance;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CyclopsMindCrushEffect()
            {
                try
                {

                    DamageEffectDef sourceMindCrushEffect = DefCache.GetDef<DamageEffectDef>("E_Effect [MindCrush_AbilityDef]");
                    DamageEffectDef newMindCrushEffect = Helper.CreateDefFromClone(sourceMindCrushEffect, "{83EC17D9-AC39-471D-AEF7-CEFB638EB2EA}", "Cyclops_MindCrush");
                    newMindCrushEffect.MaximumDamage = 60;
                    newMindCrushEffect.MinimumDamage = 60;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }

            private static void AddHoplitePortrait()
            {
                try
                {
                    SquadPortraitsDef squadPortraits = DefCache.GetDef<SquadPortraitsDef>("SquadPortraitsDef");
                    TacticalItemDef torsoDrill = DefCache.GetDef<TacticalItemDef>("HumanoidGuardian_TorsoDrill_BodyPartDef");
                    TacticalItemDef hopliteLegs = DefCache.GetDef<TacticalItemDef>("HumanoidGuardian_Legs_Armoured_ItemDef");


                    squadPortraits.ManualPortraits.Add(new SquadPortraitsDef.ManualPortrait { HeadPart = hopliteLegs, Portrait = Helper.CreateSpriteFromImageFile("hoplite_portrait.png") });

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }




            private static void ModifyCyclops()
            {
                try
                {
                    PassiveModifierAbilityDef selfRepairAbility = DefCache.GetDef<PassiveModifierAbilityDef>("RoboticSelfRepair_AbilityDef");

                    RecoverWillAbilityDef recoverWillAbilityDef = DefCache.GetDef<RecoverWillAbilityDef>("RecoverWill_AbilityDef");

                    TacticalActorDef cyclops = DefCache.GetDef<TacticalActorDef>("MediumGuardian_ActorDef");

                    List<AbilityDef> abilityDefs = new List<AbilityDef>(cyclops.Abilities)
                {
                    selfRepairAbility
                };

                    abilityDefs.Remove(recoverWillAbilityDef);

                    cyclops.Abilities = abilityDefs.ToArray();

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


            private static void CreateParalysisDamageImmunity()
            {
                try
                {
                    DamageMultiplierAbilityDef damageMultiplierAbilityDefSource = DefCache.GetDef<DamageMultiplierAbilityDef>("EMPResistant_DamageMultiplierAbilityDef");
                    DamageMultiplierAbilityDef ParalysisNotShcokImmunity = Helper.CreateDefFromClone(damageMultiplierAbilityDefSource, "38B0CBAB-EE97-4481-876B-71427E4C11CE", "ParalysisNotShockImmunity_DamageMultiplierAbilityDef");
                    ParalysisNotShcokImmunity.DamageTypeDef = DefCache.GetDef<DamageOverTimeDamageTypeEffectDef>("Paralysis_DamageOverTimeDamageTypeEffectDef");
                    ParalysisNotShcokImmunity.Multiplier = 0.0f;
                    ParalysisNotShcokImmunity.ViewElementDef = Helper.CreateDefFromClone(damageMultiplierAbilityDefSource.ViewElementDef, "FF08454A-CFD1-4BC9-AEF5-003E2ED646EC", "ParalysisNotShockImmunity_ViewElementDef");
                    ParalysisNotShcokImmunity.ViewElementDef.DisplayName1.LocalizationKey = "IMMUNITY_TO_PARALYSIS_NAME";
                    ParalysisNotShcokImmunity.ViewElementDef.Description.LocalizationKey = "IMMUNITY_TO_PARALYSIS_DESCRIPTION";
                    ParalysisNotShcokImmunity.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile("ParalysisImmunity.png");
                    ParalysisNotShcokImmunity.ViewElementDef.SmallIcon = Helper.CreateSpriteFromImageFile("ParalysisImmunity.png");

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }

            private static void ChangeAncientSiteMissions()
            {
                try
                {
                    CustomMissionTypeDef crystalRefinery = DefCache.GetDef<CustomMissionTypeDef>("CrystalsRefineryAttack_Ancient_CustomMissionTypeDef");
                    CustomMissionTypeDef orichalcumRefinery = DefCache.GetDef<CustomMissionTypeDef>("OrichalcumRefineryAttack_Ancient_CustomMissionTypeDef");
                    CustomMissionTypeDef proteanRefinery = DefCache.GetDef<CustomMissionTypeDef>("ProteanRefineryAttack_Ancient_CustomMissionTypeDef");

                    CustomMissionTypeDef crystalHarvest = DefCache.GetDef<CustomMissionTypeDef>("CrystalsHarvestAttack_Ancient_CustomMissionTypeDef");
                    CustomMissionTypeDef orichalcumHarvest = DefCache.GetDef<CustomMissionTypeDef>("OrichalcumHarvestAttack_Ancient_CustomMissionTypeDef");
                    CustomMissionTypeDef proteanHarvest = DefCache.GetDef<CustomMissionTypeDef>("ProteanHarvestAttack_Ancient_CustomMissionTypeDef");

                    List<CustomMissionTypeDef> customMissionTypeDefs = new List<CustomMissionTypeDef>() { crystalHarvest, crystalRefinery, orichalcumRefinery, proteanRefinery, orichalcumHarvest, proteanHarvest };

                    ClassTagDef hoplite = DefCache.GetDef<ClassTagDef>("HumanoidGuardian_ClassTagDef");
                    GameDifficultyLevelDef hero = DefCache.GetDef<GameDifficultyLevelDef>("Hard_GameDifficultyLevelDef");
                    GameDifficultyLevelDef veteran = DefCache.GetDef<GameDifficultyLevelDef>("Standard_GameDifficultyLevelDef");
                    GameDifficultyLevelDef legend = DefCache.GetDef<GameDifficultyLevelDef>("VeryHard_GameDifficultyLevelDef");

                    foreach (CustomMissionTypeDef customMissionTypeDef in customMissionTypeDefs)
                    {

                        TacMissionTypeParticipantData.UniqueChatarcterBind shielderVeteran = new TacMissionTypeParticipantData.UniqueChatarcterBind()
                        {
                            Amount = { Min = 1, Max = 1 },
                            Character = DefCache.GetDef<TacCharacterDef>("HumanoidGuardian_Shielder_TacCharacterDef"),
                            Difficulty = veteran,

                        };
                        TacMissionTypeParticipantData.UniqueChatarcterBind drillerVeteran = new TacMissionTypeParticipantData.UniqueChatarcterBind()
                        {
                            Amount = { Min = 1, Max = 1 },
                            Character = DefCache.GetDef<TacCharacterDef>("HumanoidGuardian_Driller_TacCharacterDef"),
                            Difficulty = veteran,

                        };
                        TacMissionTypeParticipantData.UniqueChatarcterBind shielderHero = new TacMissionTypeParticipantData.UniqueChatarcterBind()
                        {
                            Amount = { Min = 1, Max = 1 },
                            Character = DefCache.GetDef<TacCharacterDef>("HumanoidGuardian_Shielder_TacCharacterDef"),
                            Difficulty = hero,

                        };
                        TacMissionTypeParticipantData.UniqueChatarcterBind drillerHero = new TacMissionTypeParticipantData.UniqueChatarcterBind()
                        {
                            Amount = { Min = 1, Max = 1 },
                            Character = DefCache.GetDef<TacCharacterDef>("HumanoidGuardian_Driller_TacCharacterDef"),
                            Difficulty = hero,

                        };
                        TacMissionTypeParticipantData.UniqueChatarcterBind shielderLegend = new TacMissionTypeParticipantData.UniqueChatarcterBind()
                        {
                            Amount = { Min = 1, Max = 1 },
                            Character = DefCache.GetDef<TacCharacterDef>("HumanoidGuardian_Shielder_TacCharacterDef"),
                            Difficulty = legend,

                        };
                        TacMissionTypeParticipantData.UniqueChatarcterBind drillerLegend = new TacMissionTypeParticipantData.UniqueChatarcterBind()
                        {
                            Amount = { Min = 1, Max = 1 },
                            Character = DefCache.GetDef<TacCharacterDef>("HumanoidGuardian_Driller_TacCharacterDef"),
                            Difficulty = legend,

                        };


                        List<TacMissionTypeParticipantData.UniqueChatarcterBind> uniqueUnits = customMissionTypeDef.ParticipantsData[0].UniqueUnits.ToList();
                        uniqueUnits.Add(drillerVeteran);
                        uniqueUnits.Add(shielderHero);
                        uniqueUnits.Add(drillerLegend);

                        customMissionTypeDef.ParticipantsData[0].UniqueUnits = uniqueUnits.ToArray();

                        // ChangeOrichalcumSmelterMission();
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void ChangeOrichalcumSmelterMission()
            {
                try
                {
                    string key = "TFTV_KEY_ANCIENTS_SMELTER_OBJECTIVE";

                    CustomMissionTypeDef orichalcumRefinery = DefCache.GetDef<CustomMissionTypeDef>("OrichalcumRefineryAttack_Ancient_CustomMissionTypeDef");

                    _dummyObjectiveSmelter = CreateNewActivateConsoleObjective("{EE333CAA-E923-4D80-82B8-72DB6525A5A6}", "{5CFE4EF7-7313-4F64-A032-B1E47D11B254}", key);
                    orichalcumRefinery.CustomObjectives = orichalcumRefinery.CustomObjectives.AddToArray(_dummyObjectiveSmelter);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static WipeEnemyFactionObjectiveDef CreateNewActivateConsoleObjective(string gUID, string gUID1, string key)
            {
                try
                {
                    // StructuralTargetTypeTagDef interactableConsoleTag = DefCache.GetDef<StructuralTargetTypeTagDef>("TalkingPointConsoleTag");

                    WipeEnemyFactionObjectiveDef sourceWipeEnemyFactionObjective = DefCache.GetDef<WipeEnemyFactionObjectiveDef>("WipeEnemy_CustomMissionObjective");
                    WipeEnemyFactionObjectiveDef newDummyObjective = Helper.CreateDefFromClone(sourceWipeEnemyFactionObjective, gUID, "DummyObjective");
                    newDummyObjective.MissionObjectiveData.ExperienceReward = 0;
                    newDummyObjective.IsUiHidden = true;
                    newDummyObjective.MissionObjectiveData.Summary.LocalizationKey = key;
                    newDummyObjective.MissionObjectiveData.Description.LocalizationKey = key;
                    newDummyObjective.IsDefeatObjective = false;
                    newDummyObjective.IsVictoryObjective = false;

                    ActivateConsoleFactionObjectiveDef sourceActivateFactionObjective = DefCache.GetDef<ActivateConsoleFactionObjectiveDef>("Symes_CustomMissionObjective");
                    ActivateConsoleFactionObjectiveDef newObjective = Helper.CreateDefFromClone(sourceActivateFactionObjective, key, gUID1);

                    //  newObjective.ObjectiveData.InteractableTagDef = interactableConsoleTag;

                    newObjective.MissionObjectiveData.ExperienceReward = 300;
                    newObjective.IsDefeatObjective = false;//false; TESTING
                    newObjective.IsVictoryObjective = false;
                    newObjective.MissionObjectiveData.Summary.LocalizationKey = key;
                    newObjective.MissionObjectiveData.Description.LocalizationKey = key;
                    newObjective.NextOnSuccess = new FactionObjectiveDef[] { };
                    //  TacStatusDef activateHackableChannelingStatus = DefCache.GetDef<TacStatusDef>("ConvinceCivilianOnObjectiveStatus"); //status on console, this is just a tag of sorts

                    //   newObjective.ObjectiveData.InteractableStatusDef = activateHackableChannelingStatus;

                    newDummyObjective.NextOnSuccess = new FactionObjectiveDef[] { newObjective };
                    newDummyObjective.NextOnFail = new FactionObjectiveDef[] { newObjective };
                    //  newObjective.ObjectiveData.ActivatedInteractableStatusDef = 


                    return newDummyObjective;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


            private static void ChangeAncientDefenseMission()
            {
                try
                {
                    ClassTagDef hoplite = DefCache.GetDef<ClassTagDef>("HumanoidGuardian_ClassTagDef");
                    GameDifficultyLevelDef easy = DefCache.GetDef<GameDifficultyLevelDef>("Easy_GameDifficultyLevelDef");
                    GameDifficultyLevelDef hero = DefCache.GetDef<GameDifficultyLevelDef>("Hard_GameDifficultyLevelDef");
                    GameDifficultyLevelDef legend = DefCache.GetDef<GameDifficultyLevelDef>("VeryHard_GameDifficultyLevelDef");
                    TacCharacterDef shielder = DefCache.GetDef<TacCharacterDef>("HumanoidGuardian_Shielder_TacCharacterDef");
                    TacCharacterDef driller = DefCache.GetDef<TacCharacterDef>("HumanoidGuardian_Driller_TacCharacterDef");

                    TacCharacterDef mortarCrystalChiron = DefCache.GetDef<TacCharacterDef>("Chiron13_MortarCrystal_AlienMutationVariationDef");

                    //   TacCharacterDef crystalScylla =  DefCache.GetDef<TacCharacterDef>("Scylla10_Crystal_AlienMutationVariationDef");

                    ClassTagDef facehuggerTag = DefCache.GetDef<ClassTagDef>("Facehugger_ClassTagDef");

                    CustomMissionTypeDef sourceMission = DefCache.GetDef<CustomMissionTypeDef>("CrystalsHarvestAttack_Ancient_CustomMissionTypeDef");
                    CustomMissionTypeDef crystalHarvest = DefCache.GetDef<CustomMissionTypeDef>("CrystalsHarvestDefence_Alien_CustomMissionTypeDef");
                    CustomMissionTypeDef crystalRefinery = DefCache.GetDef<CustomMissionTypeDef>("CrystalsRefineryDefence_Alien_CustomMissionTypeDef");
                    CustomMissionTypeDef orichalcumHarvest = DefCache.GetDef<CustomMissionTypeDef>("OrichalcumHarvestDefence_Alien_CustomMissionTypeDef");
                    CustomMissionTypeDef orichalcumRefinery = DefCache.GetDef<CustomMissionTypeDef>("OrichalcumRefineryDefence_Alien_CustomMissionTypeDef");
                    CustomMissionTypeDef proteanHarvest = DefCache.GetDef<CustomMissionTypeDef>("ProteanHarvestDefence_Alien_CustomMissionTypeDef");
                    CustomMissionTypeDef proteanRefinery = DefCache.GetDef<CustomMissionTypeDef>("ProteanRefineryDefence_Alien_CustomMissionTypeDef");

                    TacMissionTypeParticipantData.UniqueChatarcterBind uniqueChironHero = new TacMissionTypeParticipantData.UniqueChatarcterBind()
                    {
                        Amount = { Min = 1, Max = 1 },
                        Character = mortarCrystalChiron,
                        Difficulty = hero,

                    };

                    TacMissionTypeParticipantData.UniqueChatarcterBind uniqueChironLegend = new TacMissionTypeParticipantData.UniqueChatarcterBind()
                    {
                        Amount = { Min = 1, Max = 1 },
                        Character = mortarCrystalChiron,
                        Difficulty = legend,

                    };


                    CustomMissionParticipantData participantData = new CustomMissionParticipantData()
                    {
                        ParticipantKind = TacMissionParticipant.Player,
                        UniqueUnits = new TacMissionTypeParticipantData.UniqueChatarcterBind[]


                        {new TacMissionTypeParticipantData.UniqueChatarcterBind()

                    {
                        Amount = { Min = 3, Max = 3 },
                        Character = shielder,
                        Difficulty = null,

                    },


                        new TacMissionTypeParticipantData.UniqueChatarcterBind()
                        {
                            Amount = { Min = 3, Max = 3 },
                            Character = driller,
                            Difficulty = null,

                        }

                        }

                    };

                    participantData.ActorDeployParams = sourceMission.ParticipantsData[0].ActorDeployParams;
                    participantData.FactionDef = Shared.FactionDefs[1];


                    List<CustomMissionTypeDef> allMissions = new List<CustomMissionTypeDef>
                { crystalHarvest, crystalRefinery, orichalcumHarvest, orichalcumRefinery, proteanHarvest, proteanRefinery };

                    ProtectActorFactionObjectiveDef protectActorObjectiveSource = DefCache.GetDef<ProtectActorFactionObjectiveDef>("RLE0_CustomMissionObjective");
                    ProtectActorFactionObjectiveDef protectCyclopsObjective =
                        Helper.CreateDefFromClone(protectActorObjectiveSource, "9E3ABDDC-C49D-4433-8338-483E6192AF8E", "ProtectCyclopsObjectiveDef");
                    ClassTagDef cyclopsTag = DefCache.GetDef<ClassTagDef>("MediumGuardian_ClassTagDef");
                    protectCyclopsObjective.Participant = TacMissionParticipant.Player;
                    protectCyclopsObjective.ProtectTargetGameTag = cyclopsTag;
                    protectCyclopsObjective.IsUiHidden = false;
                    protectCyclopsObjective.IsUiSummaryHidden = false;
                    protectCyclopsObjective.MissionObjectiveData.Description.LocalizationKey = "PROTECT_THE_CYCLOPS";
                    protectCyclopsObjective.MissionObjectiveData.Summary.LocalizationKey = "PROTECT_THE_CYCLOPS";

                    foreach (CustomMissionTypeDef customMissionType in allMissions)
                    {
                        List<FactionObjectiveDef> objectives = customMissionType.CustomObjectives.ToList();
                        objectives.Add(protectCyclopsObjective);
                        customMissionType.ParticipantsData.Add(participantData);

                        customMissionType.CustomObjectives = objectives.ToArray();
                        customMissionType.ClearMissionOnCancel = true; //first try this
                        customMissionType.MandatoryMission = true;
                        customMissionType.SkipDeploymentSelection = true;
                        customMissionType.MaxPlayerUnits = 0;

                        foreach (MissionDeployParams deployParams in customMissionType.ParticipantsData[0].ActorDeployParams)
                        {
                            if (deployParams.Limit.ActorTag == facehuggerTag)
                            {
                                deployParams.Limit.ActorLimit.Max = 0;

                            }
                        }
                        List<TacMissionTypeParticipantData.UniqueChatarcterBind> uniqueUnits = customMissionType.ParticipantsData[0].UniqueUnits.ToList();
                        uniqueUnits.Add(uniqueChironHero);
                        uniqueUnits.Add(uniqueChironLegend);
                        customMissionType.ParticipantsData[0].UniqueUnits = uniqueUnits.ToArray();

                    }


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }


            }

            private static void CreateLivingCrystalResearch()
            {
                try
                {
                    string id = "PX_LivingCrystalResearchDef";
                    int cost = 400;
                    string keyName = "LIVING_CRYSTAL_RESEARCH_TITLE";
                    string keyReveal = "";
                    string keyUnlock = "LIVING_CRYSTAL_RESEARCH_REVEAL";
                    string keyComplete = "LIVING_CRYSTAL_RESEARCH_COMPLETE";
                    string keyBenefits = "LIVING_CRYSTAL_RESEARCH_BENEFITS";

                    UnlockFunctionalityResearchRewardDef sourceUnlockFunctionality = DefCache.GetDef<UnlockFunctionalityResearchRewardDef>("PX_AtmosphericAnalysis_ResearchDef_UnlockFunctionalityResearchRewardDef_0");
                    UnlockFunctionalityResearchRewardDef livingCrystalFunctionalityResearchReward = Helper.CreateDefFromClone(sourceUnlockFunctionality, "6CEA3DBE-DC8C-4454-96A9-4A4D8FCB6927", "LivingCrystalFunctionalityResearchReward");

                    GameTagDef livingCrystalRequirement = DefCache.GetDef<GameTagDef>("RefineLivingCrystals_FactionFunctionalityTagDef");

                    livingCrystalFunctionalityResearchReward.Tag = livingCrystalRequirement;

                    ResourcesResearchRequirementDef sourceRequirement = DefCache.GetDef<ResourcesResearchRequirementDef>("ALN_CrabmanUmbra_ResearchDef_ResourcesResearchRequirementDef_0");
                    ResourcesResearchRequirementDef requirementDef = Helper.CreateDefFromClone(sourceRequirement, "C61983EE-F219-467A-B478-3451273ECB84", "LivingCrystalResearchRequirementDef");
                    requirementDef.Resources = new ResourcePack(new ResourceUnit { Type = ResourceType.LivingCrystals, Value = 1 });

                    ExistingResearchRequirementDef requiremen2tDef = TFTVCommonMethods.CreateNewExistingResearchResearchRequirementDef("F0D428A7-9D51-4746-9C60-1EFADD5457B8", "ExoticMaterialsResearch");

                    ResearchViewElementDef imageSource = DefCache.GetDef<ResearchViewElementDef>("PX_AntediluvianArchaeology_ViewElementDef");

                    string gUID = "14E1635F-6663-41C8-B04E-A8C91890BC5B";
                    string gUID2 = "0E7D8BB6-1139-4CE3-9402-D3F490838E55";

                    ResearchDef research = TFTVCommonMethods.CreateNewPXResearch(id, cost, gUID, gUID2, keyName, keyReveal, keyUnlock, keyComplete, keyBenefits, imageSource);

                    ReseachRequirementDefOpContainer[] revealRequirementContainer = new ReseachRequirementDefOpContainer[1];
                    ResearchRequirementDef[] revealResearchRequirementDefs = new ResearchRequirementDef[2];
                    revealResearchRequirementDefs[0] = requirementDef; //small box
                    revealResearchRequirementDefs[1] = requiremen2tDef;
                    revealRequirementContainer[0].Requirements = revealResearchRequirementDefs; //medium box
                    research.RevealRequirements.Container = revealRequirementContainer;
                    research.RevealRequirements.Operation = ResearchContainerOperation.ALL;
                    research.Unlocks = new ResearchRewardDef[] { livingCrystalFunctionalityResearchReward };
                    research.Tags = new ResearchTagDef[] { CriticalResearchTag };


                    //   ResearchDbDef researchDB = DefCache.GetDef<ResearchDbDef>("pp_ResearchDB");
                    //  researchDB.Researches.Add(research);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CreateProteanMutaneResearch()
            {
                try
                {
                    string id = "PX_ProteanMutaneResearchDef";
                    int cost = 400;
                    string keyName = "PROTEAN_MUTANE_RESEARCH_TITLE";
                    string keyReveal = "";
                    string keyUnlock = "PROTEAN_MUTANE_RESEARCH_REVEAL";
                    string keyComplete = "PROTEAN_MUTANE_RESEARCH_COMPLETE";
                    string keyBenefits = "PROTEAN_MUTANE_RESEARCH_BENEFITS";

                    UnlockFunctionalityResearchRewardDef sourceUnlockFunctionality = DefCache.GetDef<UnlockFunctionalityResearchRewardDef>("PX_AtmosphericAnalysis_ResearchDef_UnlockFunctionalityResearchRewardDef_0");
                    UnlockFunctionalityResearchRewardDef proteanMutaneFunctionalityResearchReward = Helper.CreateDefFromClone(sourceUnlockFunctionality, "E01EFEAF-70E3-4DDF-8644-AF12C1FA7AFD", "ProteanMutaneFunctionalityResearchReward");

                    GameTagDef proteanMutaneRequirement = DefCache.GetDef<GameTagDef>("RefineProteanMutane_FactionFunctionalityTagDef");

                    proteanMutaneFunctionalityResearchReward.Tag = proteanMutaneRequirement;



                    ResourcesResearchRequirementDef sourceRequirement = DefCache.GetDef<ResourcesResearchRequirementDef>("ALN_CrabmanUmbra_ResearchDef_ResourcesResearchRequirementDef_0");
                    ResourcesResearchRequirementDef requirementDef = Helper.CreateDefFromClone(sourceRequirement, "FA66ED06-A5F7-4A6F-B18F-F84BA4B97AB5", "ProteanMutaneResearchRequirementDef");

                    ExistingResearchRequirementDef requiremen2tDef = TFTVCommonMethods.CreateNewExistingResearchResearchRequirementDef("041C8F9B-0F7F-467F-9DD6-183C3B901B56", "ExoticMaterialsResearch");


                    ResearchViewElementDef imageSource = DefCache.GetDef<ResearchViewElementDef>("PX_AntediluvianArchaeology_ViewElementDef");

                    string gUID = "1BFAE176-45F4-4A8B-A1A7-8CE89CA2B224";
                    string gUID2 = "BF1A9ABD-B6F7-48CD-BE20-217C338FF421";

                    ResearchDef research = TFTVCommonMethods.CreateNewPXResearch(id, cost, gUID, gUID2, keyName, keyReveal, keyUnlock, keyComplete, keyBenefits, imageSource);

                    ReseachRequirementDefOpContainer[] revealRequirementContainer = new ReseachRequirementDefOpContainer[1];
                    ResearchRequirementDef[] revealResearchRequirementDefs = new ResearchRequirementDef[2];
                    revealResearchRequirementDefs[0] = requirementDef; //small box
                    revealResearchRequirementDefs[1] = requiremen2tDef;
                    revealRequirementContainer[0].Requirements = revealResearchRequirementDefs; //medium box
                    research.RevealRequirements.Container = revealRequirementContainer;
                    research.RevealRequirements.Operation = ResearchContainerOperation.ALL;
                    research.Unlocks = new ResearchRewardDef[] { proteanMutaneFunctionalityResearchReward };
                    research.Tags = new ResearchTagDef[] { CriticalResearchTag };

                    //   ResearchDbDef researchDB = DefCache.GetDef<ResearchDbDef>("pp_ResearchDB");
                    //   researchDB.Researches.Add(research);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CreateEventsForLOTA()
            {
                try
                {
                    //muting warning events:
                    GeoscapeEventDef livingCrystalObtainedEvent = DefCache.GetDef<GeoscapeEventDef>("PROG_LE1_WARN_GeoscapeEventDef");
                    GeoscapeEventDef orichalcumObtainedEvent = DefCache.GetDef<GeoscapeEventDef>("PROG_LE2_WARN_GeoscapeEventDef");
                    GeoscapeEventDef proteanMutaneObtainedEvent = DefCache.GetDef<GeoscapeEventDef>("PROG_LE3_WARN_GeoscapeEventDef");

                    livingCrystalObtainedEvent.GeoscapeEventData.Mute = true;
                    proteanMutaneObtainedEvent.GeoscapeEventData.Mute = true;
                    orichalcumObtainedEvent.GeoscapeEventData.Mute = true;

                    //Helena post-research Antediluvian Archaelogy event: 
                    TFTVCommonMethods.CreateNewEvent("Helena_Echoes", "HELENA_ECHOES_TITLE", "HELENA_ECHOES_TEXT", null);

                    //Olena Kim post-research Automata event:      
                    TFTVCommonMethods.CreateNewEvent("Olena_Styx", "OLENA_STYX_TITLE", "OLENA_STYX_TEXT", null);

                    //Helena post research Living Crystal event: 
                    GeoscapeEventDef helenaOneiromancy = TFTVCommonMethods.CreateNewEvent("Helena_Oneiromancy", "HELENA_ONEIROMANCY_TITLE", "HELENA_ONEIROMANCY_TEXT", null);
                    helenaOneiromancy.GeoscapeEventData.Choices[0].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("ProteanMutaneResearched", 1, false));
                    // helenaOneiromancy.GeoscapeEventData.Choices[0].Outcome.Cinematic = livingCrystalObtainedEvent.GeoscapeEventData.Choices[0].Outcome.Cinematic;

                    //Olena post-Helena event:    
                    GeoscapeEventDef olenaOneiromancy = TFTVCommonMethods.CreateNewEvent("Olena_Oneiromancy", "OLENA_ONEIROMANCY_TITLE", "OLENA_ONEIROMANCY_TEXT", "OLENA_ONEIROMANCY_OUTCOME");
                    olenaOneiromancy.GeoscapeEventData.Choices[0].Text.LocalizationKey = "OLENA_ONEIROMANCY_CHOICE";

                    //Post research Exotic mnaterials Olena event:
                    GeoscapeEventDef impossibleWeaponsEvent = DefCache.GetDef<GeoscapeEventDef>("PROG_LE1_GeoscapeEventDef");
                    impossibleWeaponsEvent.GeoscapeEventData.Description[0].General.LocalizationKey = "OLENA_PUZZLE_TEXT";
                    impossibleWeaponsEvent.GeoscapeEventData.Choices[0].Text.LocalizationKey = "OLENA_PUZZLE_CHOICE";
                    impossibleWeaponsEvent.GeoscapeEventData.Choices[0].Outcome.OutcomeText.General.LocalizationKey = "OLENA_PUZZLE_OUTCOME";

                    //Event on building the Cyclops
                    TFTVCommonMethods.CreateNewEvent("Helena_Beast", "HELENA_BEAST_TITLE", "HELENA_BEAST_TEXT", null);

                    //digitize my dreams: triggered on successful ancient site defense    
                    GeoscapeEventDef cyclopsDreams = TFTVCommonMethods.CreateNewEvent("Cyclops_Dreams", "CYCLOP_DREAMS_TITLE", "CYCLOP_DREAMS_TEXT", null);


                    //Alistair on manufacture of an ancient weapon:   
                    TFTVCommonMethods.CreateNewEvent("Alistair_Progress", "ALISTAIR_PROGRESS_TITLE", "ALISTAIR_PROGRESS_TEXT", null);

                    //Event on ProteanMutaneResearched reaching 2
                    GeoscapeEventDef canBuildCyclops = TFTVCommonMethods.CreateNewEvent("Helena_Can_Build_Cyclops", "CAN_BUILD_CYCLOPS_TITLE", "CAN_BUILD_CYCLOPS_TEXT", null);

                    //Event on researching Virophage weapons
                    GeoscapeEventDef virophage = TFTVCommonMethods.CreateNewEvent("Helena_Virophage", "HELENA_VIROPHAGE_TITLE", "HELENA_VIROPHAGE_TEXT", null);
                    virophage.GeoscapeEventData.Choices[0].Outcome.Cinematic =
                                   DefCache.GetDef<GeoscapeEventDef>("PROG_LE_FINAL_GeoscapeEventDef").GeoscapeEventData.Choices[0].Outcome.Cinematic;

                    // TFTVLogger.Always("Lota events created");

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }



            private static void RemovePandoranVirusResearchRequirement()
            {
                try
                {
                    ResearchDef alienPhysiologyResearch = DefCache.GetDef<ResearchDef>("NJ_AlienPhysiology_ResearchDef");
                    ResearchDef pandoraKeyResearch = DefCache.GetDef<ResearchDef>("PX_PandoraKey_ResearchDef");
                    ResearchDef virophageResearch = DefCache.GetDef<ResearchDef>("PX_VirophageWeapons_ResearchDef");

                    ExistingResearchRequirementDef reqForAlienPhysiology = DefCache.GetDef<ExistingResearchRequirementDef>("NJ_AlienPhysiology_ResearchDef_ExistingResearchRequirementDef_1");
                    reqForAlienPhysiology.ResearchID = "PX_AlienVirusInfection_ResearchDef";


                    ExistingResearchRequirementDef reqForVirophage = DefCache.GetDef<ExistingResearchRequirementDef>("PX_VirophageWeapons_ResearchDef_ExistingResearchRequirementDef_1");

                    ResearchDef copyOfPandoraKeyResearch = Helper.CreateDefFromClone(pandoraKeyResearch, "C6515480-4A96-4125-AA7F-CD3B4D0D5341", "CopyOf" + pandoraKeyResearch.name);
                    pandoraKeyResearch.RevealRequirements.Container = new ReseachRequirementDefOpContainer[] {
                    copyOfPandoraKeyResearch.RevealRequirements.Container[0],
                copyOfPandoraKeyResearch.RevealRequirements.Container[2],
                copyOfPandoraKeyResearch.RevealRequirements.Container[3]};

                    reqForVirophage.ResearchID = "PX_YuggothianEntity_ResearchDef";

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


            private static void ChangeImpossibleWeapons()
            {
                try
                {
                    WeaponDef shardGun = DefCache.GetDef<WeaponDef>("AC_ShardGun_WeaponDef");
                    WeaponDef crystalCrossbow = DefCache.GetDef<WeaponDef>("AC_CrystalCrossbow_WeaponDef");
                    WeaponDef mattock = DefCache.GetDef<WeaponDef>("AC_Mattock_WeaponDef");
                    WeaponDef rebuke = DefCache.GetDef<WeaponDef>("AC_Rebuke_WeaponDef");
                    WeaponDef scorpion = DefCache.GetDef<WeaponDef>("AC_Scorpion_WeaponDef");
                    WeaponDef scyther = DefCache.GetDef<WeaponDef>("AC_Scyther_WeaponDef");

                    FactionFunctionalityTagDef scytherManufacture = DefCache.GetDef<FactionFunctionalityTagDef>("ScytherManufacture_FactionFunctionalityTagDef");
                    FactionFunctionalityTagDef scorpionManufacture = DefCache.GetDef<FactionFunctionalityTagDef>("ScorpionManufacture_FactionFunctionalityTagDef");
                    FactionFunctionalityTagDef shardGunManufacture = DefCache.GetDef<FactionFunctionalityTagDef>("ShardGunManufacture_FactionFunctionalityTagDef");
                    FactionFunctionalityTagDef proteanMutane = DefCache.GetDef<FactionFunctionalityTagDef>("RefineProteanMutane_FactionFunctionalityTagDef");


                    mattock.ManufactureRequiredTagDefs.Clear();
                    scyther.ManufactureRequiredTagDefs.Clear();
                    scorpion.ManufactureRequiredTagDefs.Clear();
                    shardGun.ManufactureRequiredTagDefs.Clear();
                    crystalCrossbow.ManufactureRequiredTagDefs.Clear();
                    rebuke.ManufactureRequiredTagDefs.Clear();
                    scorpion.ManufactureOricalcum = 75;
                    mattock.ManufactureProteanMutane = 25;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);

                }
            }

            private static void ChangeCostAncientProbe()
            {
                try
                {

                    AncientSiteProbeItemDef ancientSiteProbeItemDef = DefCache.GetDef<AncientSiteProbeItemDef>("AncientSiteProbeItemDef");
                    ancientSiteProbeItemDef.ManufactureTech = 5;
                    ancientSiteProbeItemDef.ManufactureMaterials = 25;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);

                }

            }

            private static void ChangeAncientSiteExploration()
            {
                try
                {
                    DefCache.GetDef<ExcavateAbilityDef>("ExcavateAbilityDef").Cost.Values = new List<ResourceUnit>() { new ResourceUnit(ResourceType.Materials, value: 20), new ResourceUnit(ResourceType.Tech, value: 5) };
                    AncientGuardianGuardAbilityDef AncientGuardianGuardAbilityDef = DefCache.GetDef<AncientGuardianGuardAbilityDef>("AncientGuardianGuardAbilityDef");
                    AncientGuardianGuardAbilityDef.Cost.Values.Add(new ResourceUnit { Type = ResourceType.Tech, Value = 25 });
                    AncientGuardianGuardAbilityDef.LocalSiteResourceCost = 0;

                    ArcheologySettingsDef archeologySettingsDef = DefCache.GetDef<ArcheologySettingsDef>("ArcheologySettingsDef");
                    archeologySettingsDef.ArcheologyPassiveOutput = 0;
                    archeologySettingsDef.ArcheologyFacilityHarvestingPower = 0;
                    archeologySettingsDef.ExcavationCostReductionPerFacilityPercentage = 0;
                    archeologySettingsDef.MaxExcavationProbeCostReductionPerPercentage = 0;
                    archeologySettingsDef.MaxProbeCostReductionPerPercentage = 0;
                    archeologySettingsDef.ProbeCostReductionPerFacilityPercentage = 0;
                    archeologySettingsDef.ExcavationTimeHours = 8;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
            private static void ChangeSchemataMissionRequirement()
            {
                try
                {
                    GeoResearchEventFilterDef geoResearchEventFilterDef = (GeoResearchEventFilterDef)Repo.GetDef("83f708cc-2f34-d672-86e8-3a12958a7b42");
                    geoResearchEventFilterDef.ResearchID = "ExoticMaterialsResearch";

                   // DefCache.GetDef<GeoResearchEventFilterDef>("E_PROG_LE1_ResearchCompleted [GeoResearchEventFilterDef]").ResearchID = "ExoticMaterialsResearch";

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);

                }

            }



            private static void CreateAncientAutomataResearch()
            {
                try
                {

                    ActorResearchRequirementDef sourceActorResearchRequirement = DefCache.GetDef<ActorResearchRequirementDef>("PX_Alien_Fishman_ResearchDef_ActorResearchRequirementDef_0");
                    TacticalActorDef humanoidGuardianTacticalActor = DefCache.GetDef<TacticalActorDef>("HumanoidGuardian_ActorDef");
                    ActorResearchRequirementDef requirementDef = Helper.CreateDefFromClone(sourceActorResearchRequirement, "14163A38-C07E-4BFF-8366-6E30F524F85D", "HumanoidActorResearchRequirement");
                    requirementDef.Actor = humanoidGuardianTacticalActor;

                    string ancientAutomataResearchName = "AncientAutomataResearch";
                    string keyName = "AUTOMATA_RESEARCH_TITLE";
                    string keyReveal = "";
                    string keyUnlock = "AUTOMATA_RESEARCH_REVEAL";
                    string keyComplete = "AUTOMATA_RESEARCH_COMPLETE";
                    string keyBenefits = "AUTOMATA_RESEARCH_BENEFITS";
                    int cost = 400;
                    string gUID = "DEE22274-1E44-4D02-8773-2A22B5DA8213";
                    string gUID2 = "DD59BCB8-F197-4A3D-A8AD-6B1D0B9CB958";
                    ResearchViewElementDef imageSource = DefCache.GetDef<ResearchViewElementDef>("PX_AntediluvianArchaeology_ViewElementDef");

                    ResearchDef research = TFTVCommonMethods.CreateNewPXResearch(ancientAutomataResearchName, cost, gUID, gUID2, keyName, keyReveal, keyUnlock, keyComplete, keyBenefits, imageSource);

                    ReseachRequirementDefOpContainer[] revealRequirementContainer = new ReseachRequirementDefOpContainer[1];
                    ResearchRequirementDef[] revealResearchRequirementDefs = new ResearchRequirementDef[1];
                    revealResearchRequirementDefs[0] = requirementDef; //small box
                    revealRequirementContainer[0].Requirements = revealResearchRequirementDefs; //medium box
                    research.RevealRequirements.Container = revealRequirementContainer;
                    research.RevealRequirements.Operation = ResearchContainerOperation.ALL;
                    research.Tags = new ResearchTagDef[] { CriticalResearchTag };

                    //   ResearchDbDef researchDB = DefCache.GetDef<ResearchDbDef>("pp_ResearchDB");
                    //   researchDB.Researches.Add(research);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }

            private static void CreateExoticMaterialsResearch()
            {
                try
                {

                    string defName = "ExoticMaterialsResearch";
                    string keyName = "EXOTIC_RESEARCH_TITLE";
                    string keyReveal = "";
                    string keyUnlock = "EXOTIC_RESEARCH_REVEAL";
                    string keyComplete = "EXOTIC_RESEARCH_COMPLETE";
                    string keyBenefits = "EXOTIC_RESEARCH_BENEFITS";
                    int cost = 400;
                    string gUID = "17D9D851-3B75-48A7-A856-D72F101E4CE0";
                    string gUID2 = "05BA677D-52E4-4A16-B050-130EEDBB956D";
                    ResearchViewElementDef imageSource = DefCache.GetDef<ResearchViewElementDef>("PX_AntediluvianArchaeology_ViewElementDef");

                    ResearchDef research = TFTVCommonMethods.CreateNewPXResearch(defName, cost, gUID, gUID2, keyName, keyReveal, keyUnlock, keyComplete, keyBenefits, imageSource);

                    ExistingResearchRequirementDef requirementDef = TFTVCommonMethods.CreateNewExistingResearchResearchRequirementDef("47BA0BD6-F622-4EC7-A49B-B93C0A955D3C", "AncientAutomataResearch");

                    ReseachRequirementDefOpContainer[] revealRequirementContainer = new ReseachRequirementDefOpContainer[1];
                    ResearchRequirementDef[] revealResearchRequirementDefs = new ResearchRequirementDef[1];
                    revealResearchRequirementDefs[0] = requirementDef; //small box
                    revealRequirementContainer[0].Requirements = revealResearchRequirementDefs; //medium box
                    research.RevealRequirements.Container = revealRequirementContainer;
                    research.RevealRequirements.Operation = ResearchContainerOperation.ALL;
                    research.Tags = new ResearchTagDef[] { CriticalResearchTag };

                    //  ResearchDbDef researchDB = DefCache.GetDef<ResearchDbDef>("pp_ResearchDB");
                    // researchDB.Researches.Add(research);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
            private static void CreateAncientStatusEffects()
            {
                try
                {

                    //Creating status effect to show that Guardian will repair a body part next turn. Need to create a status to show small icon.

                    DamageMultiplierStatusDef sourceStatusDef = DefCache.GetDef<DamageMultiplierStatusDef>("BionicResistances_StatusDef");

                    string statusSelfRepairAbilityName = "AutoRepair_AddAbilityStatusDef";
                    DamageMultiplierStatusDef statusSelfRepairDef = Helper.CreateDefFromClone(sourceStatusDef, "17A2DF06-6BA5-46F3-92B5-D85F74193ABD", statusSelfRepairAbilityName);
                    statusSelfRepairDef.EffectName = "Selfrepair";
                    statusSelfRepairDef.ApplicationConditions = new EffectConditionDef[] { };
                    statusSelfRepairDef.Visuals = Helper.CreateDefFromClone(sourceStatusDef.Visuals, "2330D0AE-547B-492E-8C49-16BFDD498653", statusSelfRepairAbilityName);
                    statusSelfRepairDef.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.AlwaysVisible;
                    statusSelfRepairDef.VisibleOnPassiveBar = true;
                    statusSelfRepairDef.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;
                    statusSelfRepairDef.Visuals.DisplayName1.LocalizationKey = "HOPLITES_SELF_REPAIR_NAME";
                    statusSelfRepairDef.Visuals.Description.LocalizationKey = "HOPLITES_SELF_REPAIR_DESCRIPTION";
                    statusSelfRepairDef.Visuals.LargeIcon = Helper.CreateSpriteFromImageFile("TFTV_status_self_repair.png");
                    statusSelfRepairDef.Visuals.SmallIcon = Helper.CreateSpriteFromImageFile("TFTV_status_self_repair.png");
                    statusSelfRepairDef.DamageTypeDefs = new DamageTypeBaseEffectDef[] { };
                    statusSelfRepairDef.Multiplier = 1;

                    _addAutoRepairStatus = statusSelfRepairDef;


                    PassiveModifierAbilityDef ancientsPowerUpAbility = DefCache.GetDef<PassiveModifierAbilityDef>("AncientMaxPower_AbilityDef");

                    //Status for Cyclops Defense
                    //Cyclops is near invulnerable while most hoplites are alive
                    string statusCyclopsDefenseName = "CyclopsDefense_StatusDef";
                    StandardDamageTypeEffectDef projectileDamage = DefCache.GetDef<StandardDamageTypeEffectDef>("Projectile_StandardDamageTypeEffectDef");
                    StandardDamageTypeEffectDef blastDamage = DefCache.GetDef<StandardDamageTypeEffectDef>("Blast_StandardDamageTypeEffectDef");
                    DamageMultiplierStatusDef source = DefCache.GetDef<DamageMultiplierStatusDef>("BionicResistances_StatusDef");
                    DamageMultiplierStatusDef statusCyclopsDefense = Helper.CreateDefFromClone(
                        source,
                        "1C2318BF-9DF4-479A-B220-93471A6ED3D0",
                        statusCyclopsDefenseName);
                    statusCyclopsDefense.EffectName = "CyclopsDefense";
                    statusCyclopsDefense.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.AlwaysVisible;
                    statusCyclopsDefense.VisibleOnPassiveBar = true;
                    statusCyclopsDefense.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;


                    statusCyclopsDefense.Visuals = Helper.CreateDefFromClone(
                        source.Visuals,
                        "041002FC-A98F-4F09-B3AC-79089D7A9C63",
                        statusCyclopsDefenseName);
                    statusCyclopsDefense.Multiplier = 0.25f;
                    statusCyclopsDefense.Range = -1;
                    statusCyclopsDefense.DamageTypeDefs = new DamageTypeBaseEffectDef[] { projectileDamage, blastDamage };

                    statusCyclopsDefense.Visuals.LargeIcon = Helper.CreateSpriteFromImageFile("TFTV_status_cyclops_defense.png");
                    statusCyclopsDefense.Visuals.SmallIcon = statusCyclopsDefense.Visuals.LargeIcon;

                    statusCyclopsDefense.Visuals.DisplayName1.LocalizationKey = "CYCLOPS_DEFENSE_NAME";
                    statusCyclopsDefense.Visuals.Description.LocalizationKey = "CYCLOPS_DEFENSE_DESCRIPTION";

                    CyclopsDefenseStatus = statusCyclopsDefense;

                    //   DefCache.GetDef<AddAttackBoostStatusDef>("E_Status [Aura_ProteanMutaneFire_AbilityDef]");

                    //Status for hoplites; extra damage when charged
                    string skillName = "AncientsPoweredUp";
                    DamageMultiplierStatusDef ancientPowerUp = Helper.CreateDefFromClone(
                        source,
                        "94E23AE4-8089-41A2-98B1-899535FC577A",
                        skillName);
                    ancientPowerUp.EffectName = "AncientPowerUP";
                    ancientPowerUp.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.AlwaysVisible;
                    ancientPowerUp.VisibleOnPassiveBar = false;
                    ancientPowerUp.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;
                    ancientPowerUp.HealthbarPriority = -1;
                    ancientPowerUp.ExpireOnEndOfTurn = false;
                    ancientPowerUp.SingleInstance = true;
                    //  ancientPowerUp.StackMultipleStatusesAsSingleIcon = true;
                    ancientPowerUp.ParticleEffectPrefab = DefCache.GetDef<AddAttackBoostStatusDef>("E_Status [Aura_ProteanMutaneFire_AbilityDef]").ParticleEffectPrefab;



                    ancientPowerUp.Visuals = Helper.CreateDefFromClone(
                        source.Visuals,
                        "E24AFC45-FD5F-4E2A-BDFC-5D9A8B240813",
                        skillName);
                    ancientPowerUp.DamageTypeDefs = new DamageTypeBaseEffectDef[] { };
                    ancientPowerUp.Multiplier = 0.75f;

                    ancientPowerUp.Visuals.LargeIcon = Helper.CreateSpriteFromImageFile("TFTV_status_powered_up.png");
                    ancientPowerUp.Visuals.SmallIcon = Helper.CreateSpriteFromImageFile("TFTV_status_powered_up.png");

                    ancientPowerUp.Visuals.DisplayName1.LocalizationKey = "POWERED_UP_NAME";
                    ancientPowerUp.Visuals.Description.LocalizationKey = "POWERED_UP_DESCRIPTION";

                    _ancientsPowerUpStatus = ancientPowerUp;

                    string abilityName = "AncientMaxPower_AbilityDef";
                    PassiveModifierAbilityDef sourceAbility = DefCache.GetDef<PassiveModifierAbilityDef>("SelfDefenseSpecialist_AbilityDef");
                    PassiveModifierAbilityDef ancientMaxPowerAbility = Helper.CreateDefFromClone(
                        sourceAbility,
                        "6C77E15C-B1C4-46A3-A196-2BB6A6E7EB5E",
                        abilityName);
                    ancientMaxPowerAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                        sourceAbility.CharacterProgressionData,
                        "5125A077-0D6A-4AC1-A7A1-B554F68AEEBB",
                        abilityName);
                    ancientMaxPowerAbility.ViewElementDef = Helper.CreateDefFromClone(
                        sourceAbility.ViewElementDef,
                        "5936B6D6-9DEA-4087-8AA5-DC76DA6FFAE7",
                        abilityName);
                    ancientMaxPowerAbility.ViewElementDef.ShowInStatusScreen = false;
                    ancientMaxPowerAbility.StatModifications = new ItemStatModification[]
                    { new ItemStatModification {TargetStat = StatModificationTarget.BonusAttackDamage, Modification = StatModificationType.Multiply, Value = 1.50f},
                    };
                    ancientMaxPowerAbility.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                    ancientMaxPowerAbility.ViewElementDef.DisplayName1.LocalizationKey = "POWERED_UP_NAME";
                    ancientMaxPowerAbility.ViewElementDef.Description.LocalizationKey = "POWERED_UP_DESCRIPTION";

                    ancientMaxPowerAbility.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile("TFTV_status_powered_up.png");
                    ancientMaxPowerAbility.ViewElementDef.SmallIcon = Helper.CreateSpriteFromImageFile("TFTV_status_powered_up.png");

                    _ancientsPowerUpAbility = ancientMaxPowerAbility;


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }



            private static void ChangeAncientsWeapons()
            {
                try
                {
                    //Reducing "base" damage of automata's weapons; the idea is to then increase them dynamically during mission
                    WeaponDef drill = DefCache.GetDef<WeaponDef>("HumanoidGuardian_Drill_WeaponDef");
                    drill.DamagePayload.DamageKeywords[0].Value = 60;

                    WeaponDef hopliteBeam = DefCache.GetDef<WeaponDef>("HumanoidGuardian_Head_WeaponDef");
                    hopliteBeam.DamagePayload.DamageKeywords[0].Value = 70;

                    WeaponDef shield = DefCache.GetDef<WeaponDef>("HumanoidGuardian_RightShield_WeaponDef");
                    shield.DamagePayload.DamageKeywords[0].Value = 80;

                    WeaponDef cyclopsLCBeam = DefCache.GetDef<WeaponDef>("MediumGuardian_Head_LivingCrystal_WeaponDef");
                    cyclopsLCBeam.DamagePayload.DamageKeywords[0].Value = 120;
                    cyclopsLCBeam.APToUsePerc = 25;

                    WeaponDef cyclopsOBeam = DefCache.GetDef<WeaponDef>("MediumGuardian_Head_Orichalcum_WeaponDef");
                    cyclopsOBeam.DamagePayload.DamageKeywords[0].Value = 120;
                    cyclopsOBeam.APToUsePerc = 25;

                    WeaponDef cyclopsPBeam = DefCache.GetDef<WeaponDef>("MediumGuardian_Head_ProteanMutane_WeaponDef");
                    cyclopsPBeam.DamagePayload.DamageKeywords[0].Value = 120;
                    cyclopsPBeam.APToUsePerc = 25;



                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CyclopsJoinStreamsAttack()
            {
                try
                {
                    //For reference
                    TacticalActorDef hopliteActor = DefCache.GetDef<TacticalActorDef>("HumanoidGuardian_ActorDef");
                    ShootAbilityDef cyclopsOldShootAbility = DefCache.GetDef<ShootAbilityDef>("Guardian_Beam_ShootAbilityDef");

                    //Creating new ShootAbility + Effect
                    AdditionalEffectShootAbilityDef sourceAdditionalEffectShootAbility = DefCache.GetDef<AdditionalEffectShootAbilityDef>("TurretCombo_ShootAbilityDef");
                    string abilityName = "CyclopsBeamTFTVShootAbility";
                    string abilityGUID = "{DD029932-A3F2-45AC-8A47-0B6275EBE8B5}";

                    MassShootTargetActorEffectDef sourceAdditionalEffect = DefCache.GetDef<MassShootTargetActorEffectDef>("E_AdditionalEffect [TurretCombo_ShootAbilityDef]");
                    string additionalEffectGUID = "{FFE817B7-2702-4EE6-A456-FAC9A01D7BBA}";

                    AdditionalEffectShootAbilityDef newCyclopsShootAbility = Helper.CreateDefFromClone(sourceAdditionalEffectShootAbility, abilityGUID, abilityName);
                    MassShootTargetActorEffectDef newCyclopsAdditionalEffect = Helper.CreateDefFromClone(sourceAdditionalEffect, additionalEffectGUID, abilityName);
                    //Adjusting new created Defs
                    newCyclopsAdditionalEffect.ShootersActorDef = hopliteActor;
                    newCyclopsShootAbility.AdditionalEffectDef = newCyclopsAdditionalEffect;
                    newCyclopsShootAbility.ShownModeToTrack = cyclopsOldShootAbility.ShownModeToTrack;
                    newCyclopsShootAbility.TrackWithCamera = cyclopsOldShootAbility.TrackWithCamera;
                    newCyclopsShootAbility.WillPointCost = cyclopsOldShootAbility.WillPointCost;
                    newCyclopsShootAbility.EquipmentTags = cyclopsOldShootAbility.EquipmentTags;
                    newCyclopsShootAbility.IsDefault = cyclopsOldShootAbility.IsDefault;
                    newCyclopsShootAbility.CharacterProgressionData = cyclopsOldShootAbility.CharacterProgressionData;
                    newCyclopsShootAbility.ViewElementDef = cyclopsOldShootAbility.ViewElementDef;
                    newCyclopsShootAbility.UsesPerTurn = 1;
                    newCyclopsShootAbility.ActionPointCost = 0.25f;


                    WeaponDef cyclopsLCBeam = DefCache.GetDef<WeaponDef>("MediumGuardian_Head_LivingCrystal_WeaponDef");


                    WeaponDef cyclopsOBeam = DefCache.GetDef<WeaponDef>("MediumGuardian_Head_Orichalcum_WeaponDef");


                    WeaponDef cyclopsPBeam = DefCache.GetDef<WeaponDef>("MediumGuardian_Head_ProteanMutane_WeaponDef");

                    cyclopsPBeam.Abilities = new AbilityDef[] { newCyclopsShootAbility };

                    cyclopsOBeam.Abilities = new AbilityDef[] { newCyclopsShootAbility };
                    cyclopsLCBeam.Abilities = new AbilityDef[] { newCyclopsShootAbility };
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void ChangeHoplites()
            {
                try
                {

                    RecoverWillAbilityDef recoverWillAbilityDef = DefCache.GetDef<RecoverWillAbilityDef>("RecoverWill_AbilityDef");

                    TacticalActorDef hopliteActor = DefCache.GetDef<TacticalActorDef>("HumanoidGuardian_ActorDef");

                    List<AbilityDef> abilityDefs = new List<AbilityDef>(hopliteActor.Abilities)
                    {

                    };

                    abilityDefs.Remove(recoverWillAbilityDef);

                    hopliteActor.Abilities = abilityDefs.ToArray();




                    //string name = "BlackShielder";
                    TacCharacterDef shielder = DefCache.GetDef<TacCharacterDef>("HumanoidGuardian_Shielder_TacCharacterDef");
                    TacCharacterDef driller = DefCache.GetDef<TacCharacterDef>("HumanoidGuardian_Driller_TacCharacterDef");



                    BodyPartAspectDef headBodyAspect = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [HumanoidGuardian_Head_WeaponDef]");
                    headBodyAspect.Endurance = 0;
                    headBodyAspect.WillPower = 0;

                    BodyPartAspectDef torsoDrillBodyAspect = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [HumanoidGuardian_TorsoDrill_BodyPartDef]");
                    BodyPartAspectDef torsoShieldsBodyAspect = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [HumanoidGuardian_TorsoShields_BodyPartDef]");

                    torsoDrillBodyAspect.WillPower = 30;
                    torsoShieldsBodyAspect.WillPower = 30;

                    TacticalItemDef proteanMutaneTorso = DefCache.GetDef<TacticalItemDef>("MediumGuardian_Torso_ProteanMutane_BodyPartDef");
                    TacticalItemDef orichalcumTorso = DefCache.GetDef<TacticalItemDef>("MediumGuardian_Torso_Orichalcum_BodyPartDef");
                    TacticalItemDef livingCrystalTorso = DefCache.GetDef<TacticalItemDef>("MediumGuardian_Torso_LivingCrystal_BodyPartDef");

                    proteanMutaneTorso.Abilities = new AbilityDef[] { };
                    orichalcumTorso.Abilities = new AbilityDef[] { };
                    livingCrystalTorso.Abilities = new AbilityDef[] { };

                    /* ApplyDamageEffectAbilityDef stomp = DefCache.GetDef<ApplyDamageEffectAbilityDef>("Guardian_Stomp_AbilityDef");
                     stomp.DamagePayload.AoeRadius = 7;
                     stomp.TargetingDataDef.Origin.Range = 7;*/

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }



        }


        public static bool CheckIfAncientMap(TacticalLevelController controller)
        {
            try
            {
                if (controller.TacMission.MissionData.MissionType.MissionTags.Any(t => t.name.Contains("MissionTypeAncientSite")))

                {
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

        //Method to check if Ancients (as a faction) are present in the mission
        public static bool CheckIfAncientsPresent(TacticalLevelController controller)
        {
            try
            {
                if (controller.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("anc")))//MissionType.name.Contains("Attack_Alien_CustomMissionTypeDef"))
                {
                    //  TFTVLogger.Always("Ancients present");
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

        public static void UpdateAncientsWidget()
        {
            try
            {

                TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                //  TFTVLogger.Always($"running UpdateAncientsWidget; AutomataResearched {TFTVAncientsGeo.AutomataResearched} !CheckIfAncientsPresent(controller){CheckIfAncientsPresent(controller)}");

                if (!TFTVAncientsGeo.AutomataResearched || !CheckIfAncientsPresent(controller))
                {
                    return;
                }

                TacticalActor cyclops = controller.GetFactionByCommandName("anc").TacticalActors.FirstOrDefault(a => a.HasGameTag(cyclopsTag));

                TFTVUITactical.Ancients.ActivateOrAdjustAncientsWidget(Mathf.CeilToInt((1 - CyclopsDefenseStatus.Multiplier) * 100), cyclops);
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }



        internal class CyclopsAbilities
        {
            public static void AddMindCrushEffectToCyclposScream(TacticalAbility ability, TacticalActor actor, object parameter)
            {
                try
                {
                    // TFTVLogger.Always($"aptouseperc is {DefCache.GetDef<WeaponDef>("HumanoidGuardian_Head_WeaponDef").APToUsePerc}");
                    if (ability.TacticalAbilityDef.name.Equals("CyclopsScream"))
                    {
                        SilencedStatusDef silencedStatusDef = DefCache.GetDef<SilencedStatusDef>("ActorSilenced_StatusDef");
                        DamageEffectDef mindCrushEffect = DefCache.GetDef<DamageEffectDef>("E_Effect [Cyclops_MindCrush]");

                        foreach (TacticalAbilityTarget target in ability.GetTargets())
                        {
                            if (target.GetTargetActor() != null && target.GetTargetActor() is TacticalActor targetedTacticalActor)
                            {
                                targetedTacticalActor.ApplyDamage(new DamageResult
                                {
                                    ApplyStatuses = new List<StatusApplication>
                                { new StatusApplication
                                { StatusDef = silencedStatusDef, StatusSource = actor, StatusTarget = targetedTacticalActor } }
                                });
                                targetedTacticalActor.ApplyDamage(new DamageResult { ActorEffects = new List<EffectDef> { mindCrushEffect } });//, Source = __instance.Source });
                            }
                        }
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            internal class CyclopsCrossBeamShooting
            {
                //Not used
                private static IEnumerator<NextUpdate> RaiseShield(TacticalActor shooterActor)//, Weapon weapon, TacticalAbilityTarget shootTarget)
                {

                    //  ShieldDeployedStatusDef shieldDeployed = DefCache.GetDef<ShieldDeployedStatusDef>("ShieldDeployed_StatusDef");
                    RetrieveShieldAbilityDef retrieveShieldAbilityDef = DefCache.GetDef<RetrieveShieldAbilityDef>("RetrieveShield_AbilityDef");
                    // Raise the shield

                    RetrieveShieldAbility retrieveShieldAbility = shooterActor.GetAbilityWithDef<RetrieveShieldAbility>(retrieveShieldAbilityDef);

                    TacticalAbilityTarget actorAsTargetForShieldRetrieval = new TacticalAbilityTarget
                    { GameObject = shooterActor.gameObject, PositionToApply = shooterActor.gameObject.transform.position };


                    // Wait for the shield animation to complete
                    yield return retrieveShieldAbility.ExecuteAndWait(actorAsTargetForShieldRetrieval);

                }

                internal static void ReDeployHopliteShield(TacticalAbility ability, TacticalActor tacticalActor, object parameter)
                {
                    try
                    {

                        // TFTVLogger.Always($"{tacticalActor.TacticalActorDef.name} with ability {ability.AbilityDef.name}");

                        if (tacticalActor.TacticalActorDef.name.Equals("HumanoidGuardian_ActorDef") && ability.AbilityDef.name.Equals("Guardian_Beam_ShootAbilityDef") && !tacticalActor.IsControlledByPlayer && tacticalActor.IsAlive)


                        {

                            TFTVLogger.Always($"{tacticalActor.name} should be redeploying shield");

                            DeployShieldAbilityDef deployShieldAbilityDef = DefCache.GetDef<DeployShieldAbilityDef>("DeployShield_Guardian_AbilityDef");

                            DeployShieldAbilityDef deployShieldAbilityDualDef = DefCache.GetDef<DeployShieldAbilityDef>("DeployShield_Guardian_Dual_AbilityDef");

                            DeployShieldAbility deployShieldAbility = null;

                            if (tacticalActor.GetAbilityWithDef<DeployShieldAbility>(deployShieldAbilityDef) != null)
                            {

                                deployShieldAbility = tacticalActor.GetAbilityWithDef<DeployShieldAbility>(deployShieldAbilityDef);


                            }
                            else if (tacticalActor.GetAbilityWithDef<DeployShieldAbility>(deployShieldAbilityDualDef) != null)
                            {

                                deployShieldAbility = tacticalActor.GetAbilityWithDef<DeployShieldAbility>(deployShieldAbilityDualDef);

                            }

                            if (deployShieldAbility != null)
                            {

                                TFTVLogger.Always($"{tacticalActor.name} found the ability to activate");
                                TacticalAbilityTarget targetOfTheAttack = parameter as TacticalAbilityTarget;

                                Vector3 directionShieldDeploy = tacticalActor.gameObject.transform.position + 2 * (targetOfTheAttack.ActorGridPosition - tacticalActor.gameObject.transform.position).normalized;
                                //  TFTVLogger.Always($"directShieldDeploy {directionShieldDeploy}, hoplite position {__instance.gameObject.transform.position} and target{targetOfTheAttack.ActorGridPosition}");
                                TacticalAbilityTarget tacticalAbilitytaret = new TacticalAbilityTarget
                                { GameObject = tacticalActor.gameObject, PositionToApply = directionShieldDeploy };

                                deployShieldAbility.Activate(tacticalAbilitytaret);
                            }
                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
                 
                [HarmonyPatch(typeof(MassShootTargetActorEffect), "OnApply")] //VERIFIED

                public static class MassShootTargetActorEffect_OnApply_GuardiansCrossBeams_Patch
                {
                    public static bool Prefix(MassShootTargetActorEffect __instance, EffectTarget target)
                    {
                        try
                        {
                            MethodInfo tryGetShootTargetMethod = typeof(MassShootTargetActorEffect).GetMethod("TryGetShootTarget", BindingFlags.Instance | BindingFlags.NonPublic);

                            if (tryGetShootTargetMethod == null || target == null)
                            {
                                return false;
                            }

                            //  WeaponDef beamHead = DefCache.GetDef<WeaponDef>("HumanoidGuardian_Head_WeaponDef");
                            //   beamHead.APToUsePerc = 0;

                            TacticalAbilityTarget tacticalAbilityTarget = (TacticalAbilityTarget)tryGetShootTargetMethod.Invoke(__instance, new object[] { target });

                            if (tacticalAbilityTarget == null || tacticalAbilityTarget.Actor == null || tacticalAbilityTarget.Actor.IsDead)
                            {
                                return false;
                            }

                            TacticalActorBase sourceTacticalActorBase = TacUtil.GetSourceTacticalActorBase(__instance.Source);

                            if (sourceTacticalActorBase == null)
                            {
                                return false;

                            }

                            List<TacticalActor> list = sourceTacticalActorBase.TacticalFaction.TacticalActors.
                                Where((TacticalActor a) => a.TacticalActorBaseDef == __instance.MassShootTargetActorEffectDef.ShootersActorDef).
                                Where(ta => ta.IsAlive).
                                Where(ta => !ta.Status.HasStatus(AncientGuardianStealthStatus)).ToList();

                            TFTVLogger.Always($"Hoplites that can shoot in the cross-beam shooting {list.Count()}");

                            if (list.Count > 0)
                            {
                                using (new MultiForceTargetableLock(sourceTacticalActorBase.Map.GetActors<TacticalActor>()))
                                {
                                    foreach (TacticalActor hoplite in list)
                                    {
                                        ShieldDeployedStatusDef shieldDeployed = DefCache.GetDef<ShieldDeployedStatusDef>("ShieldDeployed_StatusDef");

                                        Weapon selectedWeapon = null;
                                        if (hoplite.Equipments != null)
                                        {
                                            foreach (Equipment equipment in hoplite.Equipments.Equipments)
                                            {
                                                if (equipment.TacticalItemDef.Equals(BeamHead) && equipment.IsUsable)
                                                {
                                                    selectedWeapon = equipment as Weapon;
                                                    TFTVLogger.Always($"{hoplite.name} has a beam weapon, check is null by any chance {selectedWeapon == null}");
                                                }
                                            }

                                            if (selectedWeapon != null)
                                            {
                                                TFTVLogger.Always($"{hoplite.name} can shoot");
                                                if (!hoplite.TacticalPerception.CheckFriendlyFire(selectedWeapon, hoplite.Pos, tacticalAbilityTarget, out TacticalActor hitFriend) && selectedWeapon.TryGetShootTarget(tacticalAbilityTarget) != null)
                                                {
                                                    TFTVLogger.Always($"{hoplite.name} won't hit a friendly");

                                                    if (hoplite.HasStatus(shieldDeployed))
                                                    {
                                                        TFTVLogger.Always($"{hoplite.name} has deployed shield");

                                                        //   Timing.Current.StartAndWaitFor(RaiseShield(hoplite));

                                                        hoplite.Equipments.SetSelectedEquipment(selectedWeapon);

                                                        //   TFTVLogger.Always($"selected weapon: {hoplite.Equipments.SelectedWeapon}");
                                                    }

                                                    MethodInfo faceAndShootAtTarget = typeof(MassShootTargetActorEffect).GetMethod("FaceAndShootAtTarget", BindingFlags.Instance | BindingFlags.NonPublic);

                                                    if (faceAndShootAtTarget != null)
                                                    {
                                                        Timing.Current.StartAndWaitFor((IEnumerator<NextUpdate>)faceAndShootAtTarget.Invoke(__instance, new object[] { hoplite, selectedWeapon, tacticalAbilityTarget }));
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                TFTVLogger.Always($"{hoplite.name} can't shoot because selectedWeapon null? {selectedWeapon == null}");
                                            }
                                        }
                                    }
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

                //  public static int HopliteBeamWeaponKludge = 0;

                public static Dictionary<TacticalActor, float> HopliteAPMassShoot = new Dictionary<TacticalActor, float>();

                [HarmonyPatch(typeof(MassShootTargetActorEffect), "FaceAndShootAtTarget")] //VERIFIED

                public static class MassShootTargetActorEffect_FaceAndShootAtTarget_GuardiansCrossBeams_Patch
                {
                    public static void Postfix(TacticalActor shooterActor)
                    {
                        try
                        {
                            HopliteAPMassShoot.Add(shooterActor, shooterActor?.CharacterStats?.ActionPoints);


                            TFTVLogger.Always($"{shooterActor?.name} has {shooterActor?.CharacterStats?.ActionPoints} action points");

                            /*  WeaponDef beamHead = DefCache.GetDef<WeaponDef>("HumanoidGuardian_Head_WeaponDef");
                              beamHead.APToUsePerc = 0;
                              HopliteBeamWeaponKludge += 1;
                              TFTVLogger.Always($"MassShoot in effect, count {HopliteBeamWeaponKludge}");*/

                        }

                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }

                }

                public static void RedeployHopliteShieldsAfterMassShootAttackAndRestoreTheirAP(TacticalAbility ability, TacticalActor actor, object parameter)
                {
                    try
                    {

                        ReDeployHopliteShield(ability, actor, parameter);

                        if (HopliteAPMassShoot.Count > 0)
                        {
                            if (HopliteAPMassShoot.ContainsKey(actor))
                            {
                                TFTVLogger.Always($"{actor?.name} has {actor?.CharacterStats?.ActionPoints} ");
                                actor?.CharacterStats?.ActionPoints?.Set(HopliteAPMassShoot[actor]);
                                TFTVLogger.Always($"but now {actor?.name} has {actor?.CharacterStats?.ActionPoints} ");
                                HopliteAPMassShoot.Remove(actor);
                            }
                        }
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }



                }
            }

            internal class CyclopsResistance
            {
                public static void ResetCyclopsDefense()
                {
                    try
                    {
                        TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                        if (CheckIfAncientMap(controller))
                        {
                            float baseMultiplier = 0.5f;

                            /*  if (TFTVSpecialDifficulties.CheckTacticalSpecialDifficultySettings(controller) == 2)
                              {
                                  baseMultiplier = 0.25f; //adjusted on 22/12 from 0.0f
                              }*/

                            IEnumerable<TacticalActor> allHoplites = from x in controller.Map.GetActors<TacticalActor>()
                                                                     where x.HasGameTag(hopliteTag)
                                                                     where x.IsAlive
                                                                     select x;



                            int deadHoplites = allHoplites.Where(h => h.IsDead).Count();
                            float proportion = ((float)deadHoplites / (float)(allHoplites.Count()));

                            if (allHoplites.Count() == 0)
                            {
                                proportion = 1;
                            }

                            CyclopsDefenseStatus.Multiplier = baseMultiplier + proportion * 0.5f; //+ HoplitesKilled * 0.1f;
                            TFTVLogger.Always($"There are {allHoplites.Count()} hoplites in total, {deadHoplites} are dead. Proportion is {proportion} and base multiplier is {baseMultiplier}. Cyclops Defense level is {CyclopsDefenseStatus.Multiplier}");
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void ReduceCyclopsResistance(TacticalFaction faction, TacticalActor actor)
                {
                    try
                    {
                        if (CyclopsDefenseStatus.Multiplier <= 0.99f)
                        {
                            float baseMultiplier = 0.5f;

                            /*  if (TFTVSpecialDifficulties.CheckTacticalSpecialDifficultySettings(controller) == 2)
                              {
                                  baseMultiplier = 0.25f;
                              }*/

                            List<TacticalActor> allHoplites = actor.TacticalFaction.TacticalActors.Where(ta => ta.HasGameTag(hopliteTag)).ToList();
                            int deadHoplites = allHoplites.Where(h => h.IsDead).Count();
                            float proportion = ((float)deadHoplites / (float)(allHoplites.Count));
                            CyclopsDefenseStatus.Multiplier = baseMultiplier + proportion * 0.5f; //+ HoplitesKilled * 0.1f;
                            TFTVLogger.Always($"There are {allHoplites.Count} hoplites in total, {deadHoplites} are dead. Proportion is {proportion} and base multiplier is {baseMultiplier}. Cyclops Defense level is {CyclopsDefenseStatus.Multiplier}");


                            //  CyclopsDefenseStatus.Multiplier += 0.1f;
                            TFTVLogger.Always("Hoplite killed, decreasing Cyclops defense. Cyclops defense now " + CyclopsDefenseStatus.Multiplier);
                        }
                        else
                        {
                            CyclopsDefenseStatus.Multiplier = 1;
                            if (TFTVAncientsGeo.AutomataResearched)
                            {
                                foreach (TacticalActorBase allyTacticalActorBase in faction.Actors)
                                {
                                    if (allyTacticalActorBase is TacticalActor && allyTacticalActorBase != actor)
                                    {
                                        TacticalActor actorAlly = allyTacticalActorBase as TacticalActor;
                                        if (actorAlly.HasStatus(CyclopsDefenseStatus))
                                        {
                                            Status status = actorAlly.Status.GetStatusByName(CyclopsDefenseStatus.EffectName);
                                            actorAlly.Status.Statuses.Remove(status);
                                            TFTVLogger.Always("Cyclops defense removed from " + actorAlly.name);

                                        }
                                    }
                                }

                            }
                        }

                        UpdateAncientsWidget();


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                public static void AncientKilled(TacticalLevelController controller, DeathReport deathReport)
                {
                    try
                    {
                        if (CheckIfAncientMap(controller))
                        {
                            ClassTagDef cyclopsTag = DefCache.GetDef<ClassTagDef>("MediumGuardian_ClassTagDef");
                            TacticalFaction faction = deathReport.Actor.TacticalFaction;

                            if (deathReport.Actor is TacticalActor)
                            {
                                TacticalActor actor = deathReport.Actor as TacticalActor;
                                if (actor.HasGameTag(hopliteTag))
                                {
                                    HoplitesAbilities.HoplitesAutoRepair.ApplyAutoRepairAbilityStatusOrHealNearbyHoplites(faction, actor);
                                    ReduceCyclopsResistance(faction, actor);
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
        }

        internal class HoplitesAbilities
        {
            public static void ApplyDamageResistanceToHopliteInHiding(ref DamageAccumulation.TargetData data)
            {
                try
                {

                    if (data.Target.GetActor() != null && data.Target.GetActor().Status != null && data.Target.GetActor().Status.HasStatus(AncientGuardianStealthStatus))
                    {
                        float multiplier = 0.1f;
                        data.DamageResult.HealthDamage = Math.Min(data.Target.GetHealth(), data.DamageResult.HealthDamage * multiplier);
                        data.AmountApplied = Math.Min(data.Target.GetHealth(), data.AmountApplied * multiplier);
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            internal class HoplitesMolecularTargeting
            {
                internal static void CyclopsMolecularTargeting(TacticalActor actor, IDamageDealer damageDealer)
                {
                    try
                    {
                        TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                        if (CyclopsMolecularDamageBuff.Count() == 0 || !CyclopsMolecularDamageBuff.ContainsKey(controller.TurnNumber))
                        {
                            ClassTagDef hopliteTag = DefCache.GetDef<ClassTagDef>("HumanoidGuardian_ClassTagDef");
                            ClassTagDef cyclopsTag = DefCache.GetDef<ClassTagDef>("MediumGuardian_ClassTagDef");

                            if (damageDealer != null && damageDealer.GetTacticalActorBase() != null && damageDealer.GetTacticalActorBase().GameTags.Contains(hopliteTag))
                            {
                                TacticalFaction tacticalFaction = damageDealer.GetTacticalActorBase().TacticalFaction;

                                bool cyclopsAlive = false;

                                foreach (TacticalActor tacticalActor in tacticalFaction.TacticalActors)
                                {
                                    if (tacticalActor.IsAlive && tacticalActor.GameTags.Contains(cyclopsTag))
                                    {
                                        cyclopsAlive = true;

                                    }
                                }

                                if (cyclopsAlive)
                                {

                                    int bionics = 0;
                                    int mutations = 0;

                                    foreach (TacticalItem bodypart in actor.BodyState.GetArmourItems())
                                    {
                                        if (bodypart.GameTags.Contains(Shared.SharedGameTags.AnuMutationTag))
                                        {
                                            mutations += 1;
                                        }
                                        else if (bodypart.GameTags.Contains(Shared.SharedGameTags.BionicalTag))
                                        {
                                            TFTVLogger.Always("bionics");

                                            bionics += 1;
                                        }
                                    }

                                    if (actor.TacticalActorDef.GameTags.Contains(Shared.SharedGameTags.VehicleTag))
                                    {
                                        bionics = 5;

                                    }


                                    if (bionics > mutations)
                                    {
                                        TFTVLogger.Always("more bionics");
                                        CyclopsMolecularDamageBuff.Add(controller.TurnNumber, 2);
                                        BeamsVsCyborgs();

                                        TFTVLogger.Always($"{actor.DisplayName} is primarily bionic or a vehicle");

                                    }
                                    else if (bionics < mutations || actor.HasGameTag(Shared.SharedGameTags.AlienTag))
                                    {
                                        CyclopsMolecularDamageBuff.Add(controller.TurnNumber, 1);
                                        BeamsVsMutants();
                                        TFTVLogger.Always($"{actor.DisplayName} is primarily mutated or an Alien");
                                    }
                                    else
                                    {
                                        BeamOriginal();
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

                internal static void BeamsVsCyborgs()
                {
                    try
                    {
                        WeaponDef originalBeam = DefCache.GetDef<WeaponDef>("HumanoidGuardian_Head_WeaponDef");

                        GameTagDamageKeywordDataDef virophageDamageKeyword = DefCache.GetDef<GameTagDamageKeywordDataDef>("Virophage_DamageKeywordDataDef");
                        GameTagDamageKeywordDataDef empDamageKeyword = DefCache.GetDef<GameTagDamageKeywordDataDef>("EMP_DamageKeywordDataDef");

                        DamageKeywordPair virophageDamage = new DamageKeywordPair { Value = 60, DamageKeywordDef = virophageDamageKeyword };
                        DamageKeywordPair empDamage = new DamageKeywordPair { Value = 40, DamageKeywordDef = empDamageKeyword };

                        WeaponDef cyclopsLCBeam = DefCache.GetDef<WeaponDef>("MediumGuardian_Head_LivingCrystal_WeaponDef");
                        WeaponDef cyclopsOBeam = DefCache.GetDef<WeaponDef>("MediumGuardian_Head_Orichalcum_WeaponDef");
                        WeaponDef cyclopsPBeam = DefCache.GetDef<WeaponDef>("MediumGuardian_Head_ProteanMutane_WeaponDef");

                        if (!originalBeam.DamagePayload.DamageKeywords.Contains(empDamage))
                        {
                            originalBeam.DamagePayload.DamageKeywords.Add(empDamage);
                        }
                        if (!cyclopsLCBeam.DamagePayload.DamageKeywords.Contains(empDamage))
                        {
                            cyclopsLCBeam.DamagePayload.DamageKeywords.Add(empDamage);
                        }
                        if (!cyclopsOBeam.DamagePayload.DamageKeywords.Contains(empDamage))
                        {
                            cyclopsOBeam.DamagePayload.DamageKeywords.Add(empDamage);
                        }
                        if (!cyclopsPBeam.DamagePayload.DamageKeywords.Contains(empDamage))
                        {
                            cyclopsPBeam.DamagePayload.DamageKeywords.Add(empDamage);
                        }
                        if (originalBeam.DamagePayload.DamageKeywords.Contains(virophageDamage))
                        {
                            originalBeam.DamagePayload.DamageKeywords.Remove(virophageDamage);
                        }
                        if (cyclopsLCBeam.DamagePayload.DamageKeywords.Contains(virophageDamage))
                        {
                            cyclopsLCBeam.DamagePayload.DamageKeywords.Remove(virophageDamage);
                        }
                        if (cyclopsOBeam.DamagePayload.DamageKeywords.Contains(virophageDamage))
                        {
                            cyclopsOBeam.DamagePayload.DamageKeywords.Remove(virophageDamage);
                        }
                        if (cyclopsPBeam.DamagePayload.DamageKeywords.Contains(virophageDamage))
                        {
                            cyclopsPBeam.DamagePayload.DamageKeywords.Remove(virophageDamage);
                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }


                }

                internal static void BeamsVsMutants()
                {
                    try
                    {
                        WeaponDef originalBeam = DefCache.GetDef<WeaponDef>("HumanoidGuardian_Head_WeaponDef");

                        GameTagDamageKeywordDataDef virophageDamageKeyword = DefCache.GetDef<GameTagDamageKeywordDataDef>("Virophage_DamageKeywordDataDef");
                        GameTagDamageKeywordDataDef empDamageKeyword = DefCache.GetDef<GameTagDamageKeywordDataDef>("EMP_DamageKeywordDataDef");

                        DamageKeywordPair virophageDamage = new DamageKeywordPair { Value = 60, DamageKeywordDef = virophageDamageKeyword };
                        DamageKeywordPair empDamage = new DamageKeywordPair { Value = 40, DamageKeywordDef = empDamageKeyword };

                        WeaponDef cyclopsLCBeam = DefCache.GetDef<WeaponDef>("MediumGuardian_Head_LivingCrystal_WeaponDef");
                        WeaponDef cyclopsOBeam = DefCache.GetDef<WeaponDef>("MediumGuardian_Head_Orichalcum_WeaponDef");
                        WeaponDef cyclopsPBeam = DefCache.GetDef<WeaponDef>("MediumGuardian_Head_ProteanMutane_WeaponDef");

                        if (!originalBeam.DamagePayload.DamageKeywords.Contains(virophageDamage))
                        {
                            originalBeam.DamagePayload.DamageKeywords.Add(virophageDamage);
                        }
                        if (!cyclopsLCBeam.DamagePayload.DamageKeywords.Contains(virophageDamage))
                        {
                            cyclopsLCBeam.DamagePayload.DamageKeywords.Add(virophageDamage);
                        }
                        if (!cyclopsOBeam.DamagePayload.DamageKeywords.Contains(virophageDamage))
                        {
                            cyclopsOBeam.DamagePayload.DamageKeywords.Add(virophageDamage);
                        }
                        if (!cyclopsPBeam.DamagePayload.DamageKeywords.Contains(virophageDamage))
                        {
                            cyclopsPBeam.DamagePayload.DamageKeywords.Add(virophageDamage);
                        }
                        if (originalBeam.DamagePayload.DamageKeywords.Contains(empDamage))
                        {
                            originalBeam.DamagePayload.DamageKeywords.Remove(empDamage);
                        }
                        if (cyclopsLCBeam.DamagePayload.DamageKeywords.Contains(empDamage))
                        {
                            cyclopsLCBeam.DamagePayload.DamageKeywords.Remove(empDamage);
                        }
                        if (cyclopsOBeam.DamagePayload.DamageKeywords.Contains(empDamage))
                        {
                            cyclopsOBeam.DamagePayload.DamageKeywords.Remove(empDamage);
                        }
                        if (cyclopsPBeam.DamagePayload.DamageKeywords.Contains(empDamage))
                        {
                            cyclopsPBeam.DamagePayload.DamageKeywords.Remove(empDamage);
                        }



                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                internal static void BeamOriginal()
                {
                    try
                    {
                        //  TFTVLogger.Always($"Changing Ancient beam to original damage payload");

                        WeaponDef originalBeam = DefCache.GetDef<WeaponDef>("HumanoidGuardian_Head_WeaponDef");

                        WeaponDef cyclopsLCBeam = DefCache.GetDef<WeaponDef>("MediumGuardian_Head_LivingCrystal_WeaponDef");
                        WeaponDef cyclopsOBeam = DefCache.GetDef<WeaponDef>("MediumGuardian_Head_Orichalcum_WeaponDef");
                        WeaponDef cyclopsPBeam = DefCache.GetDef<WeaponDef>("MediumGuardian_Head_ProteanMutane_WeaponDef");


                        //   originalBeam.ViewElementDef = DefCache.GetDef<ViewElementDef>("E_View [HumanoidGuardian_Head_WeaponDef]");
                        originalBeam.DamagePayload.DamageKeywords = new List<DamageKeywordPair>()
                                { new DamageKeywordPair
                                { Value = 70, DamageKeywordDef = Shared.SharedDamageKeywords.DamageKeyword }
                                };
                        cyclopsLCBeam.DamagePayload.DamageKeywords =
                           new List<DamageKeywordPair>()
                        { new DamageKeywordPair
                                    { Value = 120, DamageKeywordDef = Shared.SharedDamageKeywords.DamageKeyword }
                        };
                        //    cyclopsLCBeam.ViewElementDef = DefCache.GetDef<ViewElementDef>("E_View [MediumGuardian_Head_LivingCrystal_WeaponDef]");
                        cyclopsOBeam.DamagePayload.DamageKeywords = new List<DamageKeywordPair>()
                                { new DamageKeywordPair
                                    { Value = 120, DamageKeywordDef = Shared.SharedDamageKeywords.DamageKeyword }
                                };
                        //   cyclopsOBeam.ViewElementDef = DefCache.GetDef<ViewElementDef>("E_View [MediumGuardian_Head_Orichalcum_WeaponDef]");
                        cyclopsPBeam.DamagePayload.DamageKeywords = new List<DamageKeywordPair>()
                                { new DamageKeywordPair
                                    { Value = 120, DamageKeywordDef = Shared.SharedDamageKeywords.DamageKeyword }
                                };


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }


            }

            internal class HoplitesAutoRepair
            {
                internal static void ApplyAutoRepairAbilityStatusOrHealNearbyHoplites(TacticalFaction faction, TacticalActor actor)
                {
                    try
                    {
                        foreach (TacticalActor actorAlly in faction.TacticalActors)
                        {
                            if (actorAlly != actor && (actorAlly.HasGameTag(hopliteTag) || actorAlly.HasGameTag(cyclopsTag)))
                            {
                                // TacticalActor actorAlly = allyTacticalActorBase as TacticalActor;
                                float magnitude = 7;

                                if ((actorAlly.Pos - actor.Pos).magnitude <= magnitude)
                                {
                                    TFTVLogger.Always("Actor in range and will be receiving power from dead friendly");
                                    actorAlly.CharacterStats.WillPoints.AddRestrictedToMax(5);

                                    ActorClassIconElement actorClassIconElement = actorAlly.TacticalActorViewBase.UIActorElement.GetComponent<HealthbarUIActorElement>().ActorClassIconElement;
                                    TFTVUITactical.Enemies.ImplementAncientsChargeLevel(actorClassIconElement, actorAlly);

                                    if ((CheckGuardianBodyParts(actorAlly)[0] == null
                                    || CheckGuardianBodyParts(actorAlly)[1] == null
                                    || CheckGuardianBodyParts(actorAlly)[2] == null))
                                    {
                                        TFTVLogger.Always("Actor in range and missing bodyparts, getting spare parts");
                                        if (!actorAlly.HasStatus(_addAutoRepairStatus) && !actorAlly.HasGameTag(cyclopsTag))
                                        {
                                            actorAlly.Status.ApplyStatus(_addAutoRepairStatus);
                                            TFTVLogger.Always("AutoRepairStatus added to " + actorAlly.name);


                                            TacContextHelpManager tacContextHelpManager = (TacContextHelpManager)UnityEngine.Object.FindObjectOfType(typeof(TacContextHelpManager));
                                            tacContextHelpManager.EventTypeTriggered(HintTrigger.ActorSeen, actorAlly, actorAlly);
                                        }
                                    }
                                    else
                                    {
                                        if (actorAlly.GetHealth() < actorAlly.TotalMaxHealth)
                                        {
                                            if (actorAlly.GetHealth() + 50 >= actorAlly.TotalMaxHealth)
                                            {
                                                actorAlly.Health.Set(actorAlly.TotalMaxHealth);
                                            }
                                            else
                                            {
                                                actorAlly.Health.Set(actorAlly.GetHealth() + 50);
                                            }

                                        }
                                        TacContextHelpManager tacContextHelpManager = (TacContextHelpManager)UnityEngine.Object.FindObjectOfType(typeof(TacContextHelpManager));
                                        tacContextHelpManager.EventTypeTriggered(HintTrigger.ActorSeen, actorAlly, actorAlly);
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

                public static TacticalItem[] CheckGuardianBodyParts(TacticalActor actor)
                {
                    try
                    {
                        TacticalItem[] equipment = new TacticalItem[3];

                        foreach (Equipment item in actor.Equipments.Equipments)
                        {
                            if (item.TacticalItemDef.Equals(BeamHead))
                            {
                                equipment[0] = item;
                            }
                            else if (item.TacticalItemDef.Equals(RightShield) || item.TacticalItemDef.Equals(RightDrill))
                            {
                                equipment[1] = item;

                            }
                            else if (item.TacticalItemDef.Equals(LeftShield) || item.TacticalItemDef.Equals(LeftCrystalShield))
                            {
                                equipment[2] = item;
                            }
                        }
                        return equipment;
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        return new TacticalItem[3];
                    }
                }

            }

        }

        internal class StuckHopliteVanillaFix
        {
            public static void CheckHopliteKillList(TacticalFaction tacticalFaction)
            {
                try
                {

                    if (!tacticalFaction.TacticalFactionDef.ShortNames.Contains("anc"))
                    {
                        return;
                    }

                    TacticalLevelController controller = tacticalFaction.TacticalLevel;

                    if (controller.Map.GetActors<TacticalActor>().Any(ta => ta.HasGameTag(cyclopsTag) && ta.IsAlive))
                    {
                        return;

                    }

                    List<TacticalActor> aliveHoplites = new List<TacticalActor>(controller.Map.GetActors<TacticalActor>().Where(
                                                                ta => ta.HasGameTag(hopliteTag) && ta.IsAlive).ToList());

                    if (aliveHoplites.Count() > 3)
                    {
                        return;
                    }

                    TFTVLogger.Always($"Cyclops is dead and no more than 3 hoplites alive. Destroying them.");

                    foreach (TacticalActor tacticalActor in aliveHoplites)
                    {
                        tacticalActor.ApplyDamage(new DamageResult { HealthDamage = 500, Source = tacticalActor });
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }

        }

        internal class AncientsNewTurn
        {
            public static void AncientsNewTurnCheck(TacticalFaction tacticalFaction)
            {

                try
                {
                    if (!CheckIfAncientMap(tacticalFaction.TacticalLevel))
                    {
                        return;
                    }


                    if (!tacticalFaction.TacticalLevel.IsLoadingSavedGame)
                    {
                        TFTVLogger.Always($"starting turn {tacticalFaction.TurnNumber} for faction {tacticalFaction.Faction.FactionDef.name} in an Ancient Site map");
                        CheckRoboticSelfRepairStatus(tacticalFaction);
                        ApplyRoboticSelfHealingStatus(tacticalFaction);
                        StuckHopliteVanillaFix.CheckHopliteKillList(tacticalFaction);

                        if (tacticalFaction.TurnNumber > 0)
                        {
                            CheckForAutoRepairAbility(tacticalFaction);
                            AdjustAutomataStats(tacticalFaction);
                        }
                    }
                    UpdateAncientsWidget();
                    AdjustHopliteAndCyclopsBeam();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            internal static void CheckRoboticSelfRepairStatus(TacticalFaction tacticalFaction)
            {
                try
                {
                    foreach (TacticalActor tacticalActor in tacticalFaction.TacticalActors)
                    {
                        if (tacticalActor.HasStatus(RoboticSelfRepairStatus))
                        {
                            List<ItemSlot> bodyPartAspects = tacticalActor.BodyState.GetHealthSlots().Where(hs => !hs.Enabled).ToList();

                            foreach (ItemSlot bodyPart in bodyPartAspects)
                            {
                                TFTVLogger.Always($"{tacticalActor.name} has disabled {bodyPart.DisplayName}. Adding {bodyPart.GetHealth().Max / 2} health ");
                                bodyPart.GetHealth().Add(bodyPart.GetHealth().Max / 2);
                                tacticalActor.CharacterStats.WillPoints.Subtract(5);
                            }

                            Status status = tacticalActor.Status.GetStatusByName(RoboticSelfRepairStatus.EffectName);

                            tacticalActor.Status.Statuses.Remove(status);

                            ActorClassIconElement actorClassIconElement = tacticalActor.TacticalActorViewBase.UIActorElement.GetComponent<HealthbarUIActorElement>().ActorClassIconElement;
                            TFTVUITactical.Enemies.ImplementAncientsChargeLevel(actorClassIconElement, tacticalActor);
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            internal static void ApplyRoboticSelfHealingStatus(TacticalFaction tacticalFaction)
            {
                try
                {
                    foreach (TacticalActor tacticalActor in tacticalFaction.TacticalActors.Where(ta => ta.GetAbilityWithDef<PassiveModifierAbility>(SelfRepairAbility) != null && !ta.IsDead))
                    {
                        List<ItemSlot> bodyPartAspects = tacticalActor.BodyState.GetHealthSlots().Where(hs => !hs.Enabled).ToList();

                        TFTVLogger.Always($"{tacticalActor.name} has {SelfRepairAbility.name} and {bodyPartAspects.Count} disabled body parts. Applying Robotic Self Repair");

                        if (bodyPartAspects.Count > 0)
                        {
                            tacticalActor.Status.ApplyStatus(RoboticSelfRepairStatus);
                        }

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            public static void CheckForAutoRepairAbility(TacticalFaction faction)
            {
                try
                {
                    foreach (TacticalActor actor in faction.TacticalActors)
                    {
                        if (actor.HasStatus(_addAutoRepairStatus))
                        {
                            TacticalItem[] Bodyparts = HoplitesAbilities.HoplitesAutoRepair.CheckGuardianBodyParts(actor);

                            TFTVLogger.Always($"{actor.name} has spare parts, making repairs");

                            actor.Status.Statuses.Remove(actor.Status.GetStatusByName(_addAutoRepairStatus.EffectName));

                            if (Bodyparts[0] == null)
                            {
                                actor.Equipments.AddItem(BeamHead);
                                TFTVLogger.Always($"adding head to {actor.name}");
                            }
                            else if (Bodyparts[1] == null && Bodyparts[2] != null && Bodyparts[2].TacticalItemDef == LeftCrystalShield)
                            {
                                actor.Equipments.AddItem(RightDrill);
                                TFTVLogger.Always($"adding drill to {actor.name}");
                            }
                            else if (Bodyparts[1] == null && Bodyparts[2] != null && Bodyparts[2].TacticalItemDef == LeftShield)
                            {
                                actor.Equipments.AddItem(RightShield);
                                TFTVLogger.Always($"adding right shield to {actor.name}");
                            }
                            else if (Bodyparts[2] == null && Bodyparts[1] != null && Bodyparts[1].TacticalItemDef == RightDrill)
                            {
                                actor.Equipments.AddItem(LeftCrystalShield);
                                TFTVLogger.Always($"adding crystal shield to {actor.name}");
                            }
                            else if (Bodyparts[2] == null && Bodyparts[1] != null && Bodyparts[1].TacticalItemDef == RightShield)
                            {
                                TFTVLogger.Always($"adding left shield to {actor.name}");
                                actor.Equipments.AddItem(LeftShield);
                            }
                            else if (Bodyparts[1] == null && Bodyparts[2] == null)
                            {
                                UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                                int num = UnityEngine.Random.Range(0, 2);

                                if (num == 0)
                                {
                                    actor.Equipments.AddItem(LeftCrystalShield);
                                    TFTVLogger.Always($"adding left crystal shield to {actor.name}");
                                }
                                else
                                {
                                    actor.Equipments.AddItem(LeftShield);
                                    TFTVLogger.Always($"adding left shield to {actor.name}");
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

            private static int DetermineHopliteSpeed(TacticalActor tacticalActor, int currentWP)
            {
                try
                {
                    int divisor = 1;

                    if (tacticalActor.BodyState.GetSlot("RightLeg") != null && tacticalActor.BodyState.GetSlot("RightLeg").GetHealth() < 1)
                    {
                        divisor++;

                    }
                    if (tacticalActor.BodyState.GetSlot("LeftLeg") != null && tacticalActor.BodyState.GetSlot("LeftLeg").GetHealth() < 1)
                    {
                        divisor++;
                    }

                    return currentWP / divisor;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }

            private static int DetermineCyclopsSpeed(TacticalActor tacticalActor, int currentWP)
            {
                try
                {
                    int divisor = 1;

                    if (tacticalActor.BodyState.GetSlot("FrontRightLeg") != null && tacticalActor.BodyState.GetSlot("FrontRightLeg").GetHealth() < 1)
                    {
                        divisor++;
                    }
                    if (tacticalActor.BodyState.GetSlot("FrontLeftLeg") != null && tacticalActor.BodyState.GetSlot("FrontLeftLeg").GetHealth() < 1)
                    {
                        divisor++;
                    }
                    if (tacticalActor.BodyState.GetSlot("RearRightLeg") != null && tacticalActor.BodyState.GetSlot("RearRightLeg").GetHealth() < 1)
                    {
                        divisor++;
                    }
                    if (tacticalActor.BodyState.GetSlot("RearLeftLeg") != null && tacticalActor.BodyState.GetSlot("RearLeftLeg").GetHealth() < 1)
                    {
                        divisor++;
                    }

                    return currentWP / divisor;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }


            public static void AdjustAutomataStats(TacticalFaction faction)
            {
                try
                {

                    foreach (TacticalActor tacticalActor in faction.TacticalActors)
                    {
                        if (tacticalActor is TacticalActor guardian && tacticalActor.HasGameTag(hopliteTag) && !guardian.Status.HasStatus(AncientGuardianStealthStatus))
                        {
                            if (guardian.CharacterStats.WillPoints < 30)
                            {
                                if (guardian.CharacterStats.WillPoints > 25)
                                {
                                    guardian.CharacterStats.WillPoints.Set(30);
                                }
                                else
                                {
                                    guardian.CharacterStats.WillPoints.AddRestrictedToMax(5);

                                }
                            }

                            if (guardian.CharacterStats.WillPoints >= 30)
                            {
                                if (guardian.GetAbilityWithDef<PassiveModifierAbility>(_ancientsPowerUpAbility) == null)
                                {
                                    guardian.AddAbility(_ancientsPowerUpAbility, guardian);
                                    guardian.Status.ApplyStatus(_ancientsPowerUpStatus);

                                    TacContextHelpManager tacContextHelpManager = (TacContextHelpManager)UnityEngine.Object.FindObjectOfType(typeof(TacContextHelpManager));
                                    tacContextHelpManager.EventTypeTriggered(HintTrigger.ActorSeen, guardian, guardian);
                                }
                            }
                            else
                            {
                                if (guardian.GetAbilityWithDef<PassiveModifierAbility>(_ancientsPowerUpAbility) != null)
                                {
                                    guardian.RemoveAbility(_ancientsPowerUpAbility);
                                    guardian.Status.Statuses.Remove(guardian.Status.GetStatusByName(_ancientsPowerUpStatus.EffectName));

                                }

                            }

                            int hopliteSpeed = DetermineHopliteSpeed(tacticalActor, guardian.CharacterStats.WillPoints.IntValue);

                            guardian.CharacterStats.Speed.SetMax(hopliteSpeed);
                            guardian.CharacterStats.Speed.Set(hopliteSpeed);
                        }
                        else if (tacticalActor is TacticalActor cyclops && tacticalActor.HasGameTag(cyclopsTag))
                        {
                            if (cyclops.HasStatus(AlertedStatus) || cyclops.IsControlledByPlayer)
                            {
                                if (cyclops.CharacterStats.WillPoints < 40)
                                {
                                    if (cyclops.CharacterStats.WillPoints > 35)
                                    {
                                        cyclops.CharacterStats.WillPoints.Set(40);
                                    }
                                    else
                                    {
                                        cyclops.CharacterStats.WillPoints.AddRestrictedToMax(5);

                                    }
                                }
                            }

                            if (cyclops.CharacterStats.WillPoints >= 40)
                            {
                                if (cyclops.GetAbilityWithDef<PassiveModifierAbility>(_ancientsPowerUpAbility) == null)
                                {
                                    cyclops.AddAbility(_ancientsPowerUpAbility, cyclops);
                                    cyclops.Status.ApplyStatus(_ancientsPowerUpStatus);

                                    TacContextHelpManager tacContextHelpManager = (TacContextHelpManager)UnityEngine.Object.FindObjectOfType(typeof(TacContextHelpManager));
                                    tacContextHelpManager.EventTypeTriggered(HintTrigger.ActorSeen, cyclops, cyclops);
                                }
                            }
                            else
                            {
                                if (cyclops.GetAbilityWithDef<PassiveModifierAbility>(_ancientsPowerUpAbility) != null)
                                {
                                    cyclops.RemoveAbility(_ancientsPowerUpAbility);
                                    cyclops.Status.Statuses.Remove(cyclops.Status.GetStatusByName(_ancientsPowerUpStatus.EffectName));

                                }
                            }

                            int cyclopsSpeed = DetermineCyclopsSpeed(cyclops, cyclops.CharacterStats.WillPoints.IntValue);

                            cyclops.CharacterStats.Speed.SetMax(cyclopsSpeed);
                            cyclops.CharacterStats.Speed.Set(cyclopsSpeed);
                        }


                        ActorClassIconElement actorClassIconElement = tacticalActor.TacticalActorViewBase.UIActorElement.GetComponent<HealthbarUIActorElement>().ActorClassIconElement;
                        TFTVUITactical.Enemies.ImplementAncientsChargeLevel(actorClassIconElement, tacticalActor);

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);

                }

            }

            public static void AdjustHopliteAndCyclopsBeam()
            {
                try
                {
                    TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                    // TFTVLogger.Always($"AdjustingHopliteAndCyclopsBeams.CyclopsMolecularDamageBuff count {CyclopsMolecularDamageBuff.Count()}. Turn number is {controller.TurnNumber} ");

                    WeaponDef originalBeam = DefCache.GetDef<WeaponDef>("HumanoidGuardian_Head_WeaponDef");

                    GameTagDamageKeywordDataDef virophageDamageKeyword = DefCache.GetDef<GameTagDamageKeywordDataDef>("Virophage_DamageKeywordDataDef");
                    GameTagDamageKeywordDataDef empDamageKeyword = DefCache.GetDef<GameTagDamageKeywordDataDef>("EMP_DamageKeywordDataDef");

                    DamageKeywordPair virophageDamage = new DamageKeywordPair { Value = 60, DamageKeywordDef = virophageDamageKeyword };
                    DamageKeywordPair empDamage = new DamageKeywordPair { Value = 40, DamageKeywordDef = empDamageKeyword };

                    WeaponDef cyclopsLCBeam = DefCache.GetDef<WeaponDef>("MediumGuardian_Head_LivingCrystal_WeaponDef");
                    WeaponDef cyclopsOBeam = DefCache.GetDef<WeaponDef>("MediumGuardian_Head_Orichalcum_WeaponDef");
                    WeaponDef cyclopsPBeam = DefCache.GetDef<WeaponDef>("MediumGuardian_Head_ProteanMutane_WeaponDef");


                    //  WeaponDef cyclopsBeamVsMutants = DefCache.GetDef<WeaponDef>("CyclopsVSMutantsBeam");
                    //   WeaponDef cyclopsBeamVsCyborgs = DefCache.GetDef<WeaponDef>("CyclopsVSCyborgs");

                    if (CyclopsMolecularDamageBuff.Count() > 0)
                    {

                        if (CyclopsMolecularDamageBuff.ContainsKey(controller.TurnNumber))
                        {
                            WeaponDef beamVsMutants = DefCache.GetDef<WeaponDef>("HopliteVSMutantsBeam");
                            WeaponDef beamVsCyborgs = DefCache.GetDef<WeaponDef>("HopliteVSCyborgs");

                            if (CyclopsMolecularDamageBuff[controller.TurnNumber] == 1)
                            {

                                HoplitesAbilities.HoplitesMolecularTargeting.BeamsVsMutants();

                                TFTVLogger.Always($"{originalBeam.name} is switching to vs mutants and aliens");
                            }
                            else if (CyclopsMolecularDamageBuff[controller.TurnNumber] == 2)
                            {
                                HoplitesAbilities.HoplitesMolecularTargeting.BeamsVsCyborgs();


                                TFTVLogger.Always($"{originalBeam.name} is switching to vs cyborgs and vehicles");
                            }
                        }
                        else
                        {
                            HoplitesAbilities.HoplitesMolecularTargeting.BeamOriginal();
                            TFTVLogger.Always($"{originalBeam.name} is switching to neutral damage payload");
                        }

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

        }

        internal class AncientDeployment
        {
            //Adjusts deployment of Ancient Automata
            public static void AdjustAncientsOnDeployment(TacticalLevelController controller)
            {
                ClassTagDef cyclopsTag = DefCache.GetDef<ClassTagDef>("MediumGuardian_ClassTagDef");
                try
                {

                    TacticalFaction faction = new TacticalFaction();
                    int countUndamagedGuardians = 0;

                    if (controller.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("anc")))
                    {
                        faction = controller.GetFactionByCommandName("anc");
                        countUndamagedGuardians = AncientsEncounterCounter + TFTVSpecialDifficulties.DifficultyOrderConverter(controller.Difficulty.Order);
                    }
                    else
                    {
                        faction = controller.GetFactionByCommandName("px");
                        countUndamagedGuardians = 8 - TFTVSpecialDifficulties.DifficultyOrderConverter(controller.Difficulty.Order);
                    }

                    CyclopsAbilities.CyclopsResistance.ResetCyclopsDefense();
                    //CyclopsDefenseStatus.Multiplier = 0.5f;

                    List<TacticalActor> damagedGuardians = new List<TacticalActor>();

                    TFTVLogger.Always($"AdjustAncientsOnDeployment, undamaged hoplites count is {countUndamagedGuardians}");

                    foreach (TacticalActor tacticalActor in faction.TacticalActors)
                    {
                        // TFTVLogger.Always("Found tacticalactorbase");
                        if (tacticalActor.HasGameTag(hopliteTag))
                        {
                            //   TFTVLogger.Always("Found hoplite");
                            TacticalActor guardian = tacticalActor;
                            if (damagedGuardians.Count() + countUndamagedGuardians < faction.TacticalActors.Count() - 1)
                            {
                                TFTVLogger.Always($"damagedGuardians.Count() + countUndamagedGuardians {damagedGuardians.Count() + countUndamagedGuardians}, faction.TacticalActors.Count(){faction.TacticalActors.Count()}");
                                damagedGuardians.Add(guardian);
                            }
                            guardian.CharacterStats.WillPoints.Set(guardian.CharacterStats.WillPoints.IntMax / 3);
                            guardian.CharacterStats.Speed.SetMax(guardian.CharacterStats.WillPoints.IntValue);
                            guardian.CharacterStats.Speed.Set(guardian.CharacterStats.WillPoints.IntValue);

                        }
                        else if (tacticalActor.HasGameTag(cyclopsTag))
                        {
                            //  TFTVLogger.Always("Found cyclops");
                            TacticalActor cyclops = tacticalActor;
                            cyclops.Status.ApplyStatus(CyclopsDefenseStatus);
                            cyclops.CharacterStats.WillPoints.Set(cyclops.CharacterStats.WillPoints.IntMax / 4);
                            cyclops.CharacterStats.Speed.SetMax(cyclops.CharacterStats.WillPoints.IntValue);
                            cyclops.CharacterStats.Speed.Set(cyclops.CharacterStats.WillPoints.IntValue);
                        }

                        ActorClassIconElement actorClassIconElement = tacticalActor.TacticalActorViewBase.UIActorElement.GetComponent<HealthbarUIActorElement>().ActorClassIconElement;
                        TFTVUITactical.Enemies.ImplementAncientsChargeLevel(actorClassIconElement, tacticalActor);

                    }

                    foreach (TacticalActor tacticalActor in damagedGuardians)
                    {
                        UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                        int roll = UnityEngine.Random.Range(1, 101);
                        // TFTVLogger.Always("The roll is " + roll);


                        foreach (Equipment item in tacticalActor.Equipments.Equipments)
                        {
                            if (item.TacticalItemDef.Equals(BeamHead))
                            {
                                if (roll > 45)
                                {
                                    item.DestroyAll();
                                }
                            }
                            else if (item.TacticalItemDef.Equals(RightShield) || item.TacticalItemDef.Equals(RightDrill))
                            {
                                if (roll <= 45)
                                {
                                    item.DestroyAll();
                                }
                            }
                            else if (item.TacticalItemDef.Equals(LeftShield) || item.TacticalItemDef.Equals(LeftCrystalShield))
                            {
                                if (roll + 10 * countUndamagedGuardians >= 65)
                                {
                                    item.DestroyAll();
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


            private static readonly Vector3 _smelterObjectiveLocation = new Vector3(0.5f, 1.2f, -3.5f);
            private static StructuralTarget _smelterObjective = null;


            public static void ImplementSpecialMission(TacticalLevelController controller)
            {
                try
                {
                    HideAllAncients(controller);
                    SpawnInteractionPoint(controller);

                    if (CheckIfSmelterMission(controller))
                    {
                        TFTVLogger.Always($"Instantiating Smelter objectives on Mission Start, Load or Restart");


                        AdjustObjectives();
                    }


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void HideAllAncients(TacticalLevelController controller)
            {
                try
                {
                    TacticalFaction ancients = controller.GetFactionByCommandName("anc");

                    foreach (TacticalActor tacticalActor in ancients.TacticalActors)
                    {
                        StanceStatus stealthStatus = tacticalActor.Status.GetStatusByName(AncientGuardianStealthStatus.EffectName) as StanceStatus;
                        if (stealthStatus != null)
                        {
                            tacticalActor.Status.Statuses.Remove(stealthStatus);
                        }

                        if (!tacticalActor.HasStatus(_dormantStatus))
                        {
                            tacticalActor.Status.ApplyStatus(_dormantStatus);
                        }
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }



            private static bool CheckIfSmelterMission(TacticalLevelController controller)
            {
                try
                {
                    if (controller.TacMission.MissionData.MissionType == DefCache.GetDef<CustomMissionTypeDef>("OrichalcumRefineryAttack_Ancient_CustomMissionTypeDef"))
                    {
                        // TFTVLogger.Always($"The Smelter Mission!");
                        return true;
                    }
                    return false;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }



            private static void SpawnInteractionPoint(TacticalLevelController controller)
            {
                try
                {

                    //   controller.TacMission.MissionData.StructuralTargetsIntegrity = 1;
                    //   controller.TacMission.MissionData.CrateStructuralTargets = true;

                    string name = "SmelterConsole";
                    Vector3 position = _smelterObjectiveLocation;



                    StructuralTargetDeploymentDef stdDef = DefCache.GetDef<StructuralTargetDeploymentDef>("HackableConsoleStructuralTargetDeploymentDef");
                    StructuralTargetTypeTagDef structuralTargetTypeTagDef = DefCache.GetDef<StructuralTargetTypeTagDef>("InteractableConsole_StructuralTargetTypeTagDef");

                    TacActorData tacActorData = new TacActorData
                    {
                        ComponentSetTemplate = stdDef.ComponentSet
                    };



                    StructuralTargetInstanceData structuralTargetInstanceData = tacActorData.GenerateInstanceData() as StructuralTargetInstanceData;
                    structuralTargetInstanceData.SourceTemplate = stdDef;
                    structuralTargetInstanceData.Source = tacActorData;



                    StructuralTarget structuralTarget = ActorSpawner.SpawnActor<StructuralTarget>(tacActorData.GenerateInstanceComponentSetDef(), structuralTargetInstanceData, callEnterPlayOnActor: false);
                    GameObject obj = structuralTarget.gameObject;
                    structuralTarget.name = name;
                    structuralTarget.Source = obj;
                    structuralTarget.GameTags.Add(structuralTargetTypeTagDef);

                    var ipCols = new GameObject("InteractionPointColliders");
                    ipCols.transform.SetParent(obj.transform);
                    ipCols.tag = InteractWithObjectAbilityDef.ColliderTag;

                    ipCols.transform.SetPositionAndRotation(position, Quaternion.identity);
                    var collider = ipCols.AddComponent<BoxCollider>();


                    structuralTarget.Initialize();
                    //TFTVLogger.Always($"Spawning interaction point with name {name} at position {position}");
                    structuralTarget.DoEnterPlay();

                    //    TacticalActorBase

                    StatusDef activeConsoleStatusDef = DefCache.GetDef<StatusDef>("ActiveInteractableConsole_StatusDef");
                    structuralTarget.Status.ApplyStatus(activeConsoleStatusDef);

                    _smelterObjective = structuralTarget;

                    TFTVLogger.Always($"{name} is at position {position}");


                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void AdjustObjectives()
            {
                try
                {
                    TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();
                    ObjectivesManager factionObjectives = controller.GetFactionByCommandName("px").Objectives;
                    //    ActivateConsoleFactionObjectiveDef convinceCivilianObjectiveDef = DefCache.GetDef<ActivateConsoleFactionObjectiveDef>("ConvinceCivilianObjective");
                    WipeEnemyFactionObjective dummyObjective =
                        (WipeEnemyFactionObjective)factionObjectives.FirstOrDefault(
                            obj => obj is WipeEnemyFactionObjective objective &&
                            objective.Description.LocalizationKey == _dummyObjectiveSmelter.MissionObjectiveData.Description.LocalizationKey);

                    TFTVLogger.Always($"dummyObjective is {dummyObjective.Description.LocalizationKey}");

                    factionObjectives.Add(dummyObjective.NextOnSuccess[0]);
                    factionObjectives.Remove(dummyObjective);
                    //   factionObjectives.Add(convinceCivilianObjective);

                    //  TFTVLogger.Always($"objective should have been removed");


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);

                }
            }

            public static void SmelterConsoleActivated(StatusComponent statusComponent, Status status, TacticalLevelController controller)
            {
                try
                {
                    if (controller != null && CheckIfSmelterMission(controller) && !controller.IsLoadingSavedGame)
                    {
                        if (status.Def == DefCache.GetDef<StatusDef>("ConsoleActivated_StatusDef"))
                        {
                            StructuralTarget console = statusComponent.transform.GetComponent<StructuralTarget>();

                            if (console == _smelterObjective)
                            {
                                TFTVLogger.Always($"SmelterConsoleActivated");

                                ActivateAncients(controller);

                            }


                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);

                }
            }

            private static void ActivateAncients(TacticalLevelController controller)
            {
                try
                {
                    TacticalFaction ancients = controller.GetFactionByCommandName("anc");

                    foreach (TacticalActor tacticalActor in ancients.TacticalActors)
                    {
                        if (tacticalActor.HasStatus(_dormantStatus))
                        {
                            StanceStatus stanceStatus = tacticalActor.Status.GetStatusByName(_dormantStatus.EffectName) as StanceStatus;

                            tacticalActor.Status.Statuses.Remove(stanceStatus);
                        }

                        tacticalActor.ApplyDamage(new DamageResult { HealthDamage = 0, Source = tacticalActor });
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





