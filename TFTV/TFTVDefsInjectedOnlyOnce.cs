using Base;
using Base.AI.Defs;
using Base.Core;
using Base.Defs;
using Base.Entities.Abilities;
using Base.Entities.Effects;
using Base.Entities.Effects.ApplicationConditions;
using Base.Entities.Statuses;
using Base.Serialization;
using Base.UI;
using Base.Utils;
using Code.PhoenixPoint.Tactical.Entities.Equipments;
using HarmonyLib;
using PhoenixPoint.Common.ContextHelp;
using PhoenixPoint.Common.ContextHelp.HintConditions;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Addons;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.Game;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Common.Saves;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Abilities;
using PhoenixPoint.Geoscape.Entities.Interception;
using PhoenixPoint.Geoscape.Entities.Interception.Equipments;
using PhoenixPoint.Geoscape.Entities.Missions.Outcomes;
using PhoenixPoint.Geoscape.Entities.PhoenixBases.FacilityComponents;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Events.Eventus.Filters;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.ContextHelp.HintConditions;
using PhoenixPoint.Tactical.AI;
using PhoenixPoint.Tactical.AI.Actions;
using PhoenixPoint.Tactical.AI.Considerations;
using PhoenixPoint.Tactical.AI.TargetGenerators;
using PhoenixPoint.Tactical.ContextHelp.HintConditions;
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
using PhoenixPoint.Tactical.Eventus;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using PhoenixPoint.Tactical.Prompts;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static PhoenixPoint.Tactical.Entities.Abilities.HealAbilityDef;
using static PhoenixPoint.Tactical.Entities.Statuses.ItemSlotStatsModifyStatusDef;
using ResourceType = PhoenixPoint.Common.Core.ResourceType;

namespace TFTV
{
    internal class TFTVDefsInjectedOnlyOnce
    {
        //  private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;

        public static readonly ResearchTagDef CriticalResearchTag = DefCache.GetDef<ResearchTagDef>("CriticalPath_ResearchTagDef");

        public static Sprite UmbraIcon = Helper.CreateSpriteFromImageFile("Void-03P.png");

        // ResurrectAbilityRulesDef to mess with later


       



        internal static void ModifyChironWormAndAoETargeting()
        {
            try
            {
                DefCache.GetDef<TacticalTargetingDataDef>("E_TargetingData [AreaStun_AbilityDef]").Target.CullTargetTags = new GameTagsList { DefCache.GetDef<GameTagDef>("DamageByCaterpillarTracks_TagDef") };
                DefCache.GetDef<TacticalTargetingDataDef>("E_TargetingData [AreaStun_AbilityDef]").Origin.CullTargetTags = new GameTagsList { DefCache.GetDef<GameTagDef>("DamageByCaterpillarTracks_TagDef") };

                DefCache.GetDef<TacticalTargetingDataDef>("E_TargetingData [LaunchMortar_ShootAbilityDef]").Target.CullTargetTags = new GameTagsList { DefCache.GetDef<GameTagDef>("DamageByCaterpillarTracks_TagDef") };

                DefCache.GetDef<TacticalTargetingDataDef>("E_TargetingData [LaunchMortar_ShootAbilityDef]").Origin.CullTargetTags = new GameTagsList { DefCache.GetDef<GameTagDef>("DamageByCaterpillarTracks_TagDef") };

                DefCache.GetDef<TacticalTargetingDataDef>("E_TargetingData [LaunchGoo_ShootAbilityDef]").Target.CullTargetTags = new GameTagsList { DefCache.GetDef<GameTagDef>("DamageByCaterpillarTracks_TagDef") };
                DefCache.GetDef<TacticalTargetingDataDef>("E_TargetingData [LaunchPoisonWorm_ShootAbilityDef]").Target.CullTargetTags = new GameTagsList { DefCache.GetDef<GameTagDef>("DamageByCaterpillarTracks_TagDef") };
                DefCache.GetDef<TacticalTargetingDataDef>("E_TargetingData [LaunchAcidWorm_ShootAbilityDef]").Target.CullTargetTags = new GameTagsList { DefCache.GetDef<GameTagDef>("DamageByCaterpillarTracks_TagDef") };
                DefCache.GetDef<TacticalTargetingDataDef>("E_TargetingData [LaunchFireWorm_ShootAbilityDef]").Target.CullTargetTags = new GameTagsList { DefCache.GetDef<GameTagDef>("DamageByCaterpillarTracks_TagDef") };

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }



        }


        public static void InjectDefsInjectedOnlyOnceBatch1()
        {

            //   ReEnableFlinching();
            CreateRoboticSelfRestoreAbility();
            CreateAcidImmunity();

            AugmentationEventsDefs();
            ChangesAmbushMissions();
            CreateHints();
            CreateIntro();
            Create_VoidOmen_Events();
            ChangeInfestationDefs();
            ChangesToMedbay();
            InjectAlistairAhsbyLines();
            InjectOlenaKimLines();
            MistOnAllMissions();
            ModifyAirCombatDefs();
            ModifyDefsForPassengerModules();
            ModifyMissionDefsToReplaceNeutralWithBandit();
            ModifyPandoranProgress();
            RemoveCorruptionDamageBuff();
            TFTVChangesToDLC1andDLC2Events.ChangesToDLC1andDLC2Defs();
            TFTVChangesToDLC3Events.ChangesToDLC3Defs();
            TFTVChangesToDLC4Events.ChangesToDLC4Defs();
            TFTVChangesToDLC5Events.ChangesToDLC5Defs();
            ChangesToAcherons();
            RemoveCensusResearch();
            AllowMedkitsToTargetMutoidsAndChangesToMutoidSkillSet();
            ChangesToLOTA();
            CreateSubject24();
            CreateVoidOmenRemindersInTactical();
            RemoveMindControlImmunityVFX();
            AddContributionPointsToPriestAndTech();
            SyphonAttackFix();
            AddLoreEntries();
            CreateFireQuenchers();
            ChangeMyrmidonAndFirewormResearchRewards();
            RemovePirateKing();
            CreateRookieVulnerability();
            CreateRookieProtectionStatus();
            CreateEtermesStatuses();
            ModifyRecruitsCost();
            RemoveScyllaAndNodeResearches();
            FixUnarmedAspida();
            AddLoadingScreens();
            AddTips();

            //   CreateMeleeChiron();
            FixMyrmidonFlee();
            //  Testing();
            CreateNewBaseDefense();

            // TestingKnockBack();
            CreateConsolePromptBaseDefense();
            ModifyDecoyAbility();
            ImproveScyllaAcheronsChironsAndCyclops();
            AddMissingElectronicTags();
            ChangeUmbra();
            FixPriestScream();
            ModifyChironWormAndAoETargeting();

            // TestingHavenDefenseFix();
            // TestingKnockBackRepositionAlternative();
            // CreateCyclopsScreamStatus();



            ChangesModulesAndAcid();
            ChangeVehicleInventorySlots();
            CreateReinforcementTag();
            CreateFoodPoisoningEvents();
            StealAircraftMissionsNoItemRecovery();

            ModifyCratesToAddArmor();
            TFTVReverseEngineering.ModifyReverseEngineering();
            CreateObjectiveCaptureCapacity();

            TFTVReleaseOnly.OnReleasePrototypeDefs();
            TFTVReleaseOnly.CreateStoryModeDifficultyLevel();
            TFTVReleaseOnly.ModifyVanillaDifficultiesOrder();
            // ReinitSaves();

            CreateScyllaDamageResistanceForStrongerPandorans();


            //   TFTVBaseDefenseNJ.CreateNewNJTemplates();
            CreateReinforcementStatuses();

            RestrictCanBeRecruitedIntoPhoenix();
            ChangePalaceMissions();
            FixBionic3ResearchNotGivingAccessToFacility();
            CreateFakeFacilityToFixBadBaseDefenseMaps();
            ChangeFireNadeCostAndDamage();
        }

        //NEU_Assault_Torso_BodyPartDef
        //NEU_Assault_Legs_ItemDef
        //NEU_Sniper_Helmet_BodyPartDef
        //NEU_Sniper_Torso_BodyPartDef
        //NEU_Sniper_Legs_ItemDef




        private static void ChangeFireNadeCostAndDamage()
        {
            try
            {
                WeaponDef fireNade = DefCache.GetDef<WeaponDef>("NJ_IncindieryGrenade_WeaponDef");


                //change fire damage to 30 from 40
                foreach (DamageKeywordPair damageKeywordPair in fireNade.DamagePayload.DamageKeywords)
                {
                    if (damageKeywordPair.DamageKeywordDef == Shared.SharedDamageKeywords.BurningKeyword)
                    {

                        damageKeywordPair.Value = 30;
                    }

                }

                fireNade.ManufactureTech = 5;
                fireNade.ManufactureMaterials = 20;

                WeaponDef healNade = DefCache.GetDef<WeaponDef>("PX_HealGrenade_WeaponDef");

                healNade.ManufactureTech = 6;
                healNade.ManufactureMaterials = 28;


            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }




        }



        private static void CreateFakeFacilityToFixBadBaseDefenseMaps()
        {
            try
            {
                PhoenixFacilityDef storesFacility = DefCache.GetDef<PhoenixFacilityDef>("SecurityStation_PhoenixFacilityDef");

                string fakeFacilityName = "FakeFacility";

                PhoenixFacilityDef newFakeFacility = Helper.CreateDefFromClone(storesFacility, "{FC1CF7B3-7355-4E28-BFA2-57B1D5A83576}", fakeFacilityName);
                newFakeFacility.ViewElementDef = Helper.CreateDefFromClone(storesFacility.ViewElementDef, "{DA2A6489-117C-49D9-BA4F-A01A47A021B2}", fakeFacilityName);


            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }



        private static void FixBionic3ResearchNotGivingAccessToFacility()
        {
            try
            {
                ResearchDef researchDef = DefCache.GetDef<ResearchDef>("SYN_Bionics3_ResearchDef");
                FacilityResearchRewardDef facilityRewardDef = DefCache.GetDef<FacilityResearchRewardDef>("NJ_Bionics2_ResearchDef_FacilityResearchRewardDef_0");
                List<ResearchRewardDef> rewards = new List<ResearchRewardDef>(researchDef.Unlocks) { facilityRewardDef };



                researchDef.Unlocks = rewards.ToArray();

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void ChangePalaceMissions()
        {
            try
            {

                CreateForceYuggothianReceptacleGatesAbilityAndStatus();
                CreateNewStatusOnDisablingYugothianEyes();
                AdjustYuggothianEntity();
                ChangePalaceMissionDefs();
                CreatePalaceMissionHints();
                CreateCharactersForPalaceMission();
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void CreatePalaceIntroHint(MissionTypeTagDef missionTag, string hintName, string titleKey, string textKey, string textKey2, string gUID1, string gUID2)
        {
            try
            {
                ContextHelpHintDef palaceStart0Hint = TFTVTutorialAndStory.CreateNewTacticalHint(hintName + "0", HintTrigger.MissionStart, missionTag.name, titleKey, textKey, 3, false, gUID1);
                ContextHelpHintDef palaceStart1Hint = TFTVTutorialAndStory.CreateNewManualTacticalHint(hintName + "1", gUID2, titleKey, textKey2);

                palaceStart0Hint.AnyCondition = true;
                // palaceStart1Hint.IsTutorialHint = false;
                palaceStart1Hint.Conditions = new List<HintConditionDef>() { TFTVTutorialAndStory.LevelHasTagHintConditionForTacticalHint(missionTag.name) };
                palaceStart1Hint.AnyCondition = true;
                palaceStart0Hint.NextHint = palaceStart1Hint;


            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }


        private static void CreatePalaceMissionHints()
        {
            try
            {
                CreatePalaceIntroHint(DefCache.GetDef<MissionTypeTagDef>("PXPalace"),
                    "TFTVPXPalaceStart",
                    "PX_VICTORY_MISSION_START_TITLE", "PX_VICTORY_MISSION_START0", "PX_VICTORY_MISSION_START1",
                    "{71C7DB4D-1C0D-4AF0-BE4E-2BB90E96CF61}",
                    "{A5AE9410-69F7-4DDF-9517-A85B8ADA118A}");

                CreatePalaceIntroHint(DefCache.GetDef<MissionTypeTagDef>("NJPalace"),
                   "TFTVNJPalaceStart",
                   "NJ_VICTORY_MISSION_START_TITLE", "NJ_VICTORY_MISSION_START0", "NJ_VICTORY_MISSION_START1",
                   "{C38C52CA-8CFA-4F4D-867F-024ED8BB1FFA}",
                   "{67041692-4508-4D90-AEFA-9E145DA5E830}");

                CreatePalaceIntroHint(DefCache.GetDef<MissionTypeTagDef>("ANPalace"),
                  "TFTVANPalaceStart",
                  "AN_VICTORY_MISSION_START_TITLE", "AN_VICTORY_MISSION_START0", "AN_VICTORY_MISSION_START1",
                  "{8C41089E-BD4A-4D99-A066-17C10570F10B}",
                  "{87256303-4DD6-4EDC-B907-F8C02F8CFD02}");

                CreatePalaceIntroHint(DefCache.GetDef<MissionTypeTagDef>("SYPolyPalace"),
                 "TFTVSYPolyPalaceStart",
                 "SY_POLY_VICTORY_MISSION_START_TITLE", "SY_POLY_VICTORY_MISSION_START0", "SY_POLY_VICTORY_MISSION_START1",
                 "{BEDF6DAD-9DF4-41C6-9A81-5913B0B8253A}",
                 "{D6C6CC71-A471-45CB-A59D-6EB52C3075EE}");

                CreatePalaceIntroHint(DefCache.GetDef<MissionTypeTagDef>("SYTerraPalace"),
                 "TFTVSYTerraPalaceStart",
                 "SY_TERRA_VICTORY_MISSION_START_TITLE", "SY_TERRA_VICTORY_MISSION_START0", "SY_TERRA_VICTORY_MISSION_START1",
                 "{634FF698-80B8-4859-8ACF-956B16BD5B90}",
                 "{CBE4D317-A0A4-49D0-963D-9EE646D601B8}");


                string nameGateHint0 = "ReceptacleGateHint0";
                string nameGateHint1 = "ReceptacleGateHint1";
                ContextHelpHintDef palaceGateHint0 = TFTVTutorialAndStory.CreateNewManualTacticalHint(nameGateHint0, "{589E3AA7-07AB-4F36-9C22-05937FE77486}", "VICTORY_MISSION_GATES_TITLE", "VICTORY_MISSION_GATES0");
                ContextHelpHintDef palaceGateHint1 = TFTVTutorialAndStory.CreateNewManualTacticalHint(nameGateHint1, "{8861E55F-486A-4A53-991C-E94F9917CFF1}", "VICTORY_MISSION_GATES_TITLE", "VICTORY_MISSION_GATES1");

                string nameRevenantHint0 = "PalaceRevenantHint0";
                string nameRevenantHint1 = "PalaceRevenantHint1";

                ContextHelpHintDef palaceRevenantHint0 = TFTVTutorialAndStory.CreateNewManualTacticalHint(nameRevenantHint0, "{7D5440F0-DF8B-44E2-BB67-A02F72FB1628}", "VICTORY_MISSION_REVENANT_TO_PX_TITLE", "VICTORY_MISSION_REVENANT_TO_PX");
                ContextHelpHintDef palaceRevenantHint1 = TFTVTutorialAndStory.CreateNewManualTacticalHint(nameRevenantHint1, "{8B9B2ACE-7790-4F1A-A5F4-4835FB16F972}", "VICTORY_MISSION_REVENANT_TO_YR_TITLE", "VICTORY_MISSION_REVENANT_TO_YR");

                string nameHisMinionsHint = "PalaceHisMinionsHint";
                ContextHelpHintDef palaceHisMinionsHint = TFTVTutorialAndStory.CreateNewManualTacticalHint(nameHisMinionsHint, "{9EB02D9C-CC19-4D2F-920F-32A8227B685C}", "VICTORY_MISSION_HIS_MINIONS_TITLE", "VICTORY_MISSION_HIS_MINIONS");

                string nameEyesHint = "PalaceEyesHint";
                string nameTag = "Yuggothian_ClassTagDef";

                TFTVTutorialAndStory.CreateNewTacticalHint(nameEyesHint, HintTrigger.ActorSeen, nameTag, "VICTORY_MISSION_FOR_THE_EYES_TITLE", "VICTORY_MISSION_FOR_THE_EYES_TEXT", 1, true, "{FF77A9F0-EB84-4CBE-AD78-298399B33956}");



            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }


        private static void CreateNewStatusOnDisablingYugothianEyes()
        {
            try
            {

                //Status to be applied when YR is disrupted, causing shields to be lowered.

                string statusName = "YR_Disrupted";

                DamageMultiplierStatusDef source = DefCache.GetDef<DamageMultiplierStatusDef>("BionicResistances_StatusDef");
                DamageMultiplierStatusDef newStatusDef = Helper.CreateDefFromClone(
                    source,
                    "{6DA5667A-5890-4746-AA2A-182EA82D0E4C}",
                    statusName);
                newStatusDef.EffectName = statusName;
                newStatusDef.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.AlwaysVisible;
                newStatusDef.VisibleOnPassiveBar = true;
                newStatusDef.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;
                newStatusDef.DurationTurns = 2;

                newStatusDef.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.Hidden;
                newStatusDef.VisibleOnPassiveBar = false;


                newStatusDef.Visuals = Helper.CreateDefFromClone(
                    source.Visuals,
                    "{42FF81F8-B4D7-494D-A651-010DD8807EFF}",
                    statusName);
                newStatusDef.Multiplier = 1;
                newStatusDef.DamageTypeDefs = new DamageTypeBaseEffectDef[] { };

                newStatusDef.Visuals.LargeIcon = Helper.CreateSpriteFromImageFile("cracked-shield.png");
                newStatusDef.Visuals.SmallIcon = Helper.CreateSpriteFromImageFile("cracked-shield.png");

                newStatusDef.Visuals.DisplayName1.LocalizationKey = "YR_DEFENSE_BROKEN_NAME";
                newStatusDef.Visuals.Description.LocalizationKey = "YR_DEFENSE_BROKEN_DESCRIPTION";



            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }



        }


        private static void CreateForceYuggothianReceptacleGatesAbilityAndStatus()
        {
            try
            {


                //how hacking works:
                //Hacking_Start_AbilityDef is conditioned on Objective not having ConsoleActivated_StatusDef and it applies
                //1) ActiveHackableChannelingConsole_StatusDef to the Console (this is just a tag)
                //2) Hacking_ConsoleToActorBridge_StatusDef to the Objective
                //
                //Hacking_ActorToConsoleBridge_StatusDef is paired with Hacking_ConsoleToActorBridge_StatusDef and it triggers an event when it is applied
                //This is event is E_EventOnApply [Hacking_ActorToConsoleBridge_StatusDef]
                //
                //E_EventOnApply [Hacking_ActorToConsoleBridge_StatusDef], provided that a game is not being loaded, applies
                //Hacking_Channeling_StatusDef, which
                //1) gives the ability Hacking_Cancel_AbilityDef
                //2) on UnApply triggers the event E_EventOnUnapply [Hacking_Channeling_StatusDef]
                //
                //Hacking_Cancel_AbilityDef has the effect RemoveActorHackingStatuses_EffectDef, which removes status with the effectname HackingChannel (Hacking_Channeling_StatusDef)
                //
                //E_EventOnUnapply [Hacking_Channeling_StatusDef] triggers 2 effects:
                //1) E_ActivateHackingFinishAbility [Hacking_Channeling_StatusDef]
                //2) E_RemoveBridgeStatusEffect [Hacking_Channeling_StatusDef], which removes the status with the effectname ActorToConsoleBridge (Hacking_ActorToConsoleBridge_StatusDef)
                //
                //E_ActivateHackingFinishAbility [Hacking_Channeling_StatusDef], provided that 
                //1) E_ActorIsAlive [Hacking_Channeling_StatusDef]
                //2) E_StatusElapsedInTurns for status Hacking_ActorToConsoleBridge_StatusDef is 2
                //will activate the ability Hacking_Finish_AbilityDef, which
                //1) looks at the status Hacking_ConsoleToActorBridge_StatusDef
                //2) and triggers the status ConsoleActivated_StatusDef


                //First create all the abilities

                //sources for new abilities
                InteractWithObjectAbilityDef startHackingDef = DefCache.GetDef<InteractWithObjectAbilityDef>("Hacking_Start_AbilityDef");
                ApplyEffectAbilityDef cancelHackingDef = DefCache.GetDef<ApplyEffectAbilityDef>("Hacking_Cancel_AbilityDef");
                InteractWithObjectAbilityDef finishHackingDef = DefCache.GetDef<InteractWithObjectAbilityDef>("Hacking_Finish_AbilityDef");

                //new abilities
                string forceGateAbilityName = "ForceYuggothianGateAbility";
                string cancelGateAbilityName = "CancelYuggothianGateAbility";
                string finishGateAbilityName = "FinishYuggothianGateAbility";

                InteractWithObjectAbilityDef newForceGateAbility = Helper.CreateDefFromClone
                    (startHackingDef,
                    "{AB869306-7AA4-417F-93E4-8A6CE63FFE45}", forceGateAbilityName);
                InteractWithObjectAbilityDef newFinishGateAbility = Helper.CreateDefFromClone(
                    finishHackingDef,
                    "{3E702D44-02EE-4BCC-9943-466441FAD3AF}", finishGateAbilityName);

                ApplyEffectAbilityDef newCancelGateAbility = Helper.CreateDefFromClone(
                    cancelHackingDef,
                    "{A020E779-FA4C-4D44-AA32-AF3D424B8324}", cancelGateAbilityName);


                newForceGateAbility.ViewElementDef = Helper.CreateDefFromClone(startHackingDef.ViewElementDef, "{BEAD489E-9B4D-4DF9-9B76-BCE653FF9F6D}", forceGateAbilityName);
                newFinishGateAbility.ViewElementDef = Helper.CreateDefFromClone(finishHackingDef.ViewElementDef, "{BE486198-CB7E-47D9-8041-64F747D9548A}", finishGateAbilityName);
                newCancelGateAbility.ViewElementDef = Helper.CreateDefFromClone(cancelHackingDef.ViewElementDef, "{{3309F86B-45F2-4C7A-A639-F12E1B17B5FD}}", cancelGateAbilityName);

                newForceGateAbility.ViewElementDef.DisplayName1.LocalizationKey = "FORCE_GATE_ABILITY";
                newForceGateAbility.ViewElementDef.Description.LocalizationKey = "FORCE_GATE_ABILITY_DESCRIPTION";
                newCancelGateAbility.ViewElementDef.DisplayName1.LocalizationKey = "CANCEL_FORCE_GATE_ABILITY";
                newCancelGateAbility.ViewElementDef.Description.LocalizationKey = "CANCEL_FORCE_GATE_ABILITY_DESCRIPTION";
                //  TFTVLogger.Always($"got here");

                //Then create the statuses

                //sources for new statuses 
                TacStatusDef activateHackableChannelingStatus = DefCache.GetDef<TacStatusDef>("ActiveHackableChannelingConsole_StatusDef"); //status on console, this is just a tag of sorts
                ActorBridgeStatusDef actorToConsoleBridgingStatusDef = DefCache.GetDef<ActorBridgeStatusDef>("Hacking_ActorToConsoleBridge_StatusDef");

                AddAbilityStatusDef hackingStatusDef = DefCache.GetDef<AddAbilityStatusDef>("Hacking_Channeling_StatusDef"); //status on actor
                ActorBridgeStatusDef consoleToActorBridgingStatusDef = DefCache.GetDef<ActorBridgeStatusDef>("Hacking_ConsoleToActorBridge_StatusDef");


                string statusOnObjectiveName = "ForceGateOnObjectiveStatus";
                string objectiveToActorBridgeStatusName = "ObjectiveToActorBridgeStatus";
                string actorToObjectiveBridgeStatusName = "ActorToObjectiveBridgeStatus";
                string statusOnActorName = "ForcingGateOnActorStatus";

                TacStatusDef newStatusOnObjective = Helper.CreateDefFromClone(activateHackableChannelingStatus, "{6A31787B-14AD-4143-AD57-C3AF04AF1E2B}", statusOnObjectiveName);
                ActorBridgeStatusDef newActorToObjectiveStatus = Helper.CreateDefFromClone(actorToConsoleBridgingStatusDef, "{D288280F-603D-4556-804A-9B8B63646C96}", actorToObjectiveBridgeStatusName);

                AddAbilityStatusDef newStatusOnActor = Helper.CreateDefFromClone(hackingStatusDef, "{23143337-5CF8-4AE8-8B7D-B5D0650CD629}", statusOnActorName);
                ActorBridgeStatusDef newObjectiveToActorStatus = Helper.CreateDefFromClone(consoleToActorBridgingStatusDef, "{897F88CC-2BB0-4E04-A0CD-AFF62463C199}", objectiveToActorBridgeStatusName);

                //need to create visuals for the new statuses

                newStatusOnObjective.Visuals = Helper.CreateDefFromClone(activateHackableChannelingStatus.Visuals, "{6934146B-0F91-4C34-8B58-EB115748B915}", statusOnObjectiveName);
                newActorToObjectiveStatus.Visuals = Helper.CreateDefFromClone(actorToConsoleBridgingStatusDef.Visuals, "{6B002C8D-F28D-4A61-83BB-81E06BFF51FE}", actorToObjectiveBridgeStatusName);
                newObjectiveToActorStatus.Visuals = Helper.CreateDefFromClone(consoleToActorBridgingStatusDef.Visuals, "{75E47B2A-6598-4635-882C-C763681E2C6D}", objectiveToActorBridgeStatusName);
                newStatusOnActor.Visuals = Helper.CreateDefFromClone(hackingStatusDef.Visuals, "{A315B3DF-7F7C-4887-B875-007EB58DB61F}", statusOnActorName);
                //   TFTVLogger.Always($"got here2");

                newActorToObjectiveStatus.Visuals.DisplayName1.LocalizationKey = "FORCE_GATE_STATUS";
                newActorToObjectiveStatus.Visuals.Description.LocalizationKey = "FORCE_GATE_STATUS_DESCRIPTION";

                // TFTVLogger.Always($"got here3");
                //Hacking_Start_AbilityDef is conditioned on Objective not having ConsoleActivated_StatusDef and it applies
                //1) ActiveHackableChannelingConsole_StatusDef to the Console (this is just a tag)
                //2) Hacking_ConsoleToActorBridge_StatusDef to the Objective


                //Force Gate ability
                newForceGateAbility.ActiveInteractableConsoleStatusDef = newStatusOnObjective; //status on the objective
                newForceGateAbility.ActivatedConsoleStatusDef = newObjectiveToActorStatus; //bridge status from objective to Actor
                                                                                           //we don't change newForceGateAbility.StatusesBlockingActivation because we keep using Console_ActivatedStatusDef unchanged, for now

                //Hacking_ActorToConsoleBridge_StatusDef is paired with Hacking_ConsoleToActorBridge_StatusDef
                //
                //
                //so let's pair the new bridging statuses
                newActorToObjectiveStatus.PairedStatusDef = newObjectiveToActorStatus;
                newObjectiveToActorStatus.PairedStatusDef = newActorToObjectiveStatus;


                //and it triggers an event when it is applied
                //This is event is E_EventOnApply [Hacking_ActorToConsoleBridge_StatusDef]
                //
                TacticalEventDef newEventOnApplyForcingGate = Helper.CreateDefFromClone(DefCache.GetDef<TacticalEventDef>("E_EventOnApply [Hacking_ActorToConsoleBridge_StatusDef]"), "{AD224294-3CFB-4003-90A5-5BE83755D171}", statusOnActorName);

                //E_EventOnApply [Hacking_ActorToConsoleBridge_StatusDef], provided that a game is not being loaded, applies
                //Hacking_Channeling_StatusDef,
                //
                TacStatusEffectDef newEffectToApplyActiveStatusOnActor = Helper.CreateDefFromClone(DefCache.GetDef<TacStatusEffectDef>("E_ApplyHackingChannelingStatus [Hacking_ActorToConsoleBridge_StatusDef]"), "{133AEE94-FAB8-44BF-B796-1A6A4A367745}", statusOnObjectiveName);
                newEffectToApplyActiveStatusOnActor.StatusDef = newStatusOnActor;
                newEventOnApplyForcingGate.EffectData.EffectDefs = new EffectDef[] { newEffectToApplyActiveStatusOnActor };

                newActorToObjectiveStatus.EventOnApply = newEventOnApplyForcingGate;
                //
                //
                //which
                //1) gives the ability Hacking_Cancel_AbilityDef
                newStatusOnActor.AbilityDef = newCancelGateAbility; //the status gives the actor the ability to cancel the hacking/forcing the gate
                //2) on UnApply triggers the event E_EventOnUnapply [Hacking_Channeling_StatusDef]


                //we need to create a new event for when the effect is unapplied, to apply 2 new effects:
                //1) finish executing the ability (original E_ActivateHackingFinishAbility [Hacking_Channeling_StatusDef]),
                //2) remove bridge effect from ActorToObjective, and we don't change the original because the effect name hasn't been changed E_RemoveBridgeStatusEffect [Hacking_Channeling_StatusDef]
                TacticalEventDef newEventOnUnApplyForcingGate = Helper.CreateDefFromClone(DefCache.GetDef<TacticalEventDef>("E_EventOnUnapply [Hacking_Channeling_StatusDef]"), "{1739F6E1-21C2-45B5-9944-0B1A042DD9C4}", statusOnActorName);
                ActivateAbilityEffectDef newActivateFinishForcingGateEffect = Helper.CreateDefFromClone(DefCache.GetDef<ActivateAbilityEffectDef>("E_ActivateHackingFinishAbility [Hacking_Channeling_StatusDef]"), "{CA7D54EE-2086-4FF7-9F5C-EBE66A92D67A}", statusOnActorName);
                newEventOnUnApplyForcingGate.EffectData.EffectDefs = new EffectDef[] { newActivateFinishForcingGateEffect, newEventOnUnApplyForcingGate.EffectData.EffectDefs[1] };

                newStatusOnActor.EventOnUnapply = newEventOnUnApplyForcingGate;

                //we start by changing the ability our clone is pointing at
                newActivateFinishForcingGateEffect.AbilityDef = newFinishGateAbility;

                //but it has also 2 application conditions:
                //1) E_ActorIsAlive [Hacking_Channeling_StatusDef], we can probably keep it as it is
                //2) "E_StatusElapsedInTurns", and this one we have to replace because it is pointing at ActorToConsole bridge, and we want it pointing at our new ActorToObjective bridge
                MinStatusDurationInTurnsEffectConditionDef newTurnDurationCondition = Helper.CreateDefFromClone
                    (DefCache.GetDef<MinStatusDurationInTurnsEffectConditionDef>("E_StatusElapsedInTurns"), "{9D190470-2C5A-45BD-B95D-2E96A8723E49}", newFinishGateAbility + "ElapsedTurnsCondition");

                newTurnDurationCondition.TacStatusDef = newActorToObjectiveStatus;
                newActivateFinishForcingGateEffect.ApplicationConditions = new EffectConditionDef[] { newActivateFinishForcingGateEffect.ApplicationConditions[0], newTurnDurationCondition };

                //
                //Hacking_Cancel_AbilityDef has the effect RemoveActorHackingStatuses_EffectDef, which removes status with the effectname HackingChannel (Hacking_Channeling_StatusDef)
                //
                //E_EventOnUnapply [Hacking_Channeling_StatusDef] triggers 2 effects:
                //1) E_ActivateHackingFinishAbility [Hacking_Channeling_StatusDef]
                //2) E_RemoveBridgeStatusEffect [Hacking_Channeling_StatusDef], which removes the status with the effectname ActorToConsoleBridge (Hacking_ActorToConsoleBridge_StatusDef)
                //
                //E_ActivateHackingFinishAbility [Hacking_Channeling_StatusDef], provided that 
                //1) E_ActorIsAlive [Hacking_Channeling_StatusDef]
                //2) E_StatusElapsedInTurns for status Hacking_ActorToConsoleBridge_StatusDef is 2
                //will activate the ability Hacking_Finish_AbilityDef, which
                //1) looks at the status Hacking_ConsoleToActorBridge_StatusDef
                //2) and triggers the status ConsoleActivated_StatusDef


                //Force Gate Cancel ability shouldn't require changing, as the effect in RemoveActorHackingStatuses_EffectDef is still called "HackingChannel"

                //Force Gate Finish ability activatedConsoleStatus is the same, for now, Console_ActivatedStatusDef,  but we need to change ActiveInteractableConsoleStatusDef to the new objective to actor Bridge
                newFinishGateAbility.ActiveInteractableConsoleStatusDef = newObjectiveToActorStatus;




                //We need to add the forcegateability to the actor template
                //and apparently the finishgateability too
                TacticalActorDef soldierActorDef = DefCache.GetDef<TacticalActorDef>("Soldier_ActorDef");

                List<AbilityDef> abilityDefs = new List<AbilityDef>(soldierActorDef.Abilities) { newForceGateAbility, newFinishGateAbility };
                soldierActorDef.Abilities = abilityDefs.ToArray();


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }



        private static void RestrictCanBeRecruitedIntoPhoenix()
        {
            try
            {
                TriggerAbilityZoneOfControlStatusDef canBeRecruited1x1 = DefCache.GetDef<TriggerAbilityZoneOfControlStatusDef>("CanBeRecruitedIntoPhoenix_1x1_StatusDef");
                TriggerAbilityZoneOfControlStatusDef canBeRecruited3x3 = DefCache.GetDef<TriggerAbilityZoneOfControlStatusDef>("CanBeRecruitedIntoPhoenix_3x3_StatusDef");

                List<EffectConditionDef> effectConditionDefs1x1 = canBeRecruited1x1.TriggerConditions.ToList();
                List<EffectConditionDef> effectConditionDefs3x3 = canBeRecruited3x3.TriggerConditions.ToList();
                ActorHasTagEffectConditionDef source = DefCache.GetDef<ActorHasTagEffectConditionDef>("HasCombatantTag_ApplicationCondition");
                ActorHasTagEffectConditionDef notDroneCondition = Helper.CreateDefFromClone(source, "{87709AA5-4B10-44A7-9810-1E0502726A48}", "NotADroneEffectConditionDef");

                notDroneCondition.HasTag = false;
                notDroneCondition.GameTag = DefCache.GetDef<GameTagDef>("DamageByCaterpillarTracks_TagDef");

                ActorHasTagEffectConditionDef notAlienCondition = Helper.CreateDefFromClone(source, "{5EDDD493-F5BF-4942-BD12-594B76CFE0EF}", "NotAlienEffectConditionDef");
                notAlienCondition.HasTag = false;
                notAlienCondition.GameTag = DefCache.GetDef<GameTagDef>("Alien_RaceTagDef");

                effectConditionDefs1x1.Add(notAlienCondition);
                effectConditionDefs1x1.Add(notDroneCondition);

                effectConditionDefs3x3.Add(notAlienCondition);
                effectConditionDefs3x3.Add(notDroneCondition);

                canBeRecruited1x1.TriggerConditions = effectConditionDefs1x1.ToArray();
                canBeRecruited3x3.TriggerConditions = effectConditionDefs3x3.ToArray();

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        private static void AdjustYuggothianEntity()
        {
            try
            {

                TacticalPerceptionDef yugothianPercpetion = DefCache.GetDef<TacticalPerceptionDef>("Yugothian_PerceptionDef");
                yugothianPercpetion.SizeSpottingMultiplier = 1.0f;
                // yugothianPercpetion.PermanentReveal = false;
                yugothianPercpetion.AlwaysVisible = false;

                DefCache.GetDef<TacticalActorYuggothDef>("Yugothian_ActorDef").EnduranceToHealthMultiplier = 100;

                DefCache.GetDef<TacticalItemDef>("Yugothian_Head_BodyPartDef").HitPoints = 900000;
                DefCache.GetDef<TacticalItemDef>("Yugothian_Roots_BodyPartDef").HitPoints = 900000;

                DefCache.GetDef<SpawnActorAbilityDef>("DeployInjectorBomb2_AbilityDef");

                DefCache.GetDef<TacCharacterDef>("YugothianMain_TacCharacterDef").Data.Will = 500;

                // ActionCamDef deployCam = DefCache.GetDef<ActionCamDef>("DeployInjectorBombCamDef");

                // deployCam.PositionOffset.x = -5;
                //  DefCache.GetDef<CameraAnyFilterDef>("E_AnyDeployInjectorBombAbilityFilter [NoDieCamerasTacticalCameraDirectorDef]").Conditions.Clear();




            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }




        }

        private static void CreateReinforcementStatuses()
        {
            try
            {
                StatsModifyEffectDef modifyAPEffect = DefCache.GetDef<StatsModifyEffectDef>("ModifyAP_EffectDef");
                modifyAPEffect.StatModifications = new List<StatModification>()
                {new StatModification()
                {
                    Modification = StatModificationType.MultiplyRestrictedToBounds,
                    Value = 0.2f,
                    StatName = "ActionPoints"

                }

                };

                string reinforcementStatusUnder1AP = "ReinforcementStatusUnder1AP";
                string reinforcementStatus1AP = "ReinforcementStatus1AP";
                string reinforcementStatusUnder2AP = "ReinforcementStatusUnder2AP";


                StatsModifyEffectDef newEffect1AP = Helper.CreateDefFromClone(modifyAPEffect, "{A52F2DD5-92F4-4D31-B4E1-32454D67435A}", reinforcementStatus1AP);
                StatsModifyEffectDef newEffectUnder2AP = Helper.CreateDefFromClone(modifyAPEffect, "{D6090754-5A2C-45E3-888D-60E825CB619F}", reinforcementStatusUnder2AP);
                newEffect1AP.StatModifications = new List<StatModification>()
                {new StatModification()
                {
                    Modification = StatModificationType.MultiplyRestrictedToBounds,
                    Value = 0.25f,
                    StatName = "ActionPoints"

                }

                };

                newEffectUnder2AP.StatModifications = new List<StatModification>()
                {new StatModification()
                {
                    Modification = StatModificationType.MultiplyRestrictedToBounds,
                    Value = 0.4f,
                    StatName = "ActionPoints"

                }

                };
                DelayedEffectStatusDef source = DefCache.GetDef<DelayedEffectStatusDef>("E_Status [WarCry_AbilityDef]");

                DelayedEffectStatusDef newReinforcementStatusUnder1APStatus = Helper.CreateDefFromClone(source, "{60D48AD5-CCC5-4D99-9B59-C5B7041B5818}", reinforcementStatusUnder1AP);

                TacticalAbilityViewElementDef viewElementSource = DefCache.GetDef<TacticalAbilityViewElementDef>("E_ViewElement [Acheron_CallReinforcements_AbilityDef]");

                newReinforcementStatusUnder1APStatus.EffectName = "RecentReinforcementUnder1AP";
                newReinforcementStatusUnder1APStatus.Visuals = Helper.CreateDefFromClone(source.Visuals, "{4E808CF0-7E73-4CC9-B642-E8CEFE663FA6}", reinforcementStatusUnder1AP);
                //  Sprite icon = Helper.CreateSpriteFromImageFile("TBTV_CallReinforcements.png");

                newReinforcementStatusUnder1APStatus.Visuals.SmallIcon = viewElementSource.SmallIcon;
                newReinforcementStatusUnder1APStatus.Visuals.LargeIcon = viewElementSource.LargeIcon;
                newReinforcementStatusUnder1APStatus.Visuals.DisplayName1 = viewElementSource.DisplayName1; //for testing, adjust later
                newReinforcementStatusUnder1APStatus.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.Hidden;
                newReinforcementStatusUnder1APStatus.EffectDef = modifyAPEffect;
                newReinforcementStatusUnder1APStatus.EventOnApply = new TacticalEventDef();
                newReinforcementStatusUnder1APStatus.ShowNotification = false;
                newReinforcementStatusUnder1APStatus.ShowNotificationOnUnApply = false;


                DelayedEffectStatusDef newReinforcementStatus1APStatus = Helper.CreateDefFromClone(source, "{D32F42E3-97F5-4EE4-BDAC-36A07767593B}", reinforcementStatus1AP);

                newReinforcementStatus1APStatus.EffectName = "RecentReinforcement1AP";
                newReinforcementStatus1APStatus.Visuals = Helper.CreateDefFromClone(source.Visuals, "{49715088-BD6C-4104-A7D0-A08796A517DD}", reinforcementStatus1AP);
                //  Sprite icon = Helper.CreateSpriteFromImageFile("TBTV_CallReinforcements.png");

                newReinforcementStatus1APStatus.Visuals.SmallIcon = viewElementSource.SmallIcon;
                newReinforcementStatus1APStatus.Visuals.LargeIcon = viewElementSource.LargeIcon;
                newReinforcementStatus1APStatus.Visuals.DisplayName1 = viewElementSource.DisplayName1; //for testing, adjust later
                newReinforcementStatus1APStatus.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.Hidden;
                newReinforcementStatus1APStatus.EffectDef = newEffect1AP;
                newReinforcementStatus1APStatus.EventOnApply = new TacticalEventDef();
                newReinforcementStatus1APStatus.ShowNotification = false;
                newReinforcementStatus1APStatus.ShowNotificationOnUnApply = false;
                //     newReinforcementStatus1APStatus.EventOnApply = new TacticalEventDef();


                DelayedEffectStatusDef newReinforcementStatusUnder2APStatus = Helper.CreateDefFromClone(source, "{C3AB59A4-0579-4B3C-89FA-2370BB982071}", reinforcementStatusUnder2AP);

                newReinforcementStatusUnder2APStatus.EffectName = "RecentReinforcementUnder2AP";
                newReinforcementStatusUnder2APStatus.Visuals = Helper.CreateDefFromClone(source.Visuals, "{{466FAEDC-0CEE-4ADB-8A58-089B1B783348}}", reinforcementStatusUnder2AP);
                newReinforcementStatusUnder2APStatus.EffectDef = newEffectUnder2AP;
                //  Sprite icon = Helper.CreateSpriteFromImageFile("TBTV_CallReinforcements.png");

                newReinforcementStatusUnder2APStatus.Visuals.SmallIcon = viewElementSource.SmallIcon;
                newReinforcementStatusUnder2APStatus.Visuals.LargeIcon = viewElementSource.LargeIcon;
                newReinforcementStatusUnder2APStatus.Visuals.DisplayName1 = viewElementSource.DisplayName1; //for testing, adjust later
                newReinforcementStatusUnder2APStatus.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.Hidden;
                newReinforcementStatusUnder2APStatus.EventOnApply = new TacticalEventDef();
                newReinforcementStatusUnder2APStatus.ShowNotification = false;
                newReinforcementStatusUnder2APStatus.ShowNotificationOnUnApply = false;
                //   newReinforcementStatusUnder2APStatus.EventOnApply = new TacticalEventDef();



            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }




        }


        private static void CreateCharactersForPalaceMission()
        {

            /*Nikolai
    Stas
    Zhara
    Sophia_Villanova
    Colonel_Jack_Harlson
    Captain_Richter*/
            try
            {
                CreateHarlson();
                CreateRichter();
                CreateSofia();

                CreateZhara();
                CreateStas();
                CreateNikolai();

                CreateTaxiarchNergal();
                ChangeExalted();



            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }







        }
        private static void ChangeExalted()
        {


            try
            {

                TacCharacterDef exalted = DefCache.GetDef<TacCharacterDef>("AN_Exalted_TacCharacterDef");

                List<TacticalAbilityDef> tacticalAbilities = exalted.Data.Abilites.ToList();



                ApplyStatusAbilityDef sowerOfChange = DefCache.GetDef<ApplyStatusAbilityDef>("SowerOfChange_AbilityDef");
                ApplyStatusAbilityDef bioChemist = DefCache.GetDef<ApplyStatusAbilityDef>("BC_Biochemist_AbilityDef");
                ApplyEffectAbilityDef layWaste = DefCache.GetDef<ApplyEffectAbilityDef>("LayWaste_AbilityDef");

                tacticalAbilities.Add(sowerOfChange);
                tacticalAbilities.Add(bioChemist);
                tacticalAbilities.Add(layWaste);

                exalted.Data.Abilites = tacticalAbilities.ToArray();

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }


        private static void CreateHarlson()
        {
            try
            {

                JetJumpAbilityDef jetpackControl = DefCache.GetDef<JetJumpAbilityDef>("JetpackControl_AbilityDef");
                ApplyStatusAbilityDef boomBlast = DefCache.GetDef<ApplyStatusAbilityDef>("BigBooms_AbilityDef");
                ApplyStatusAbilityDef takedown = DefCache.GetDef<ApplyStatusAbilityDef>("BC_Takedown_AbilityDef");
                PassiveModifierAbilityDef punisher = DefCache.GetDef<PassiveModifierAbilityDef>("Punisher_AbilityDef");



                string nameDef = "Harlson_TacCharacterDef";

                TacCharacterDef harlson = Helper.CreateDefFromClone(DefCache.GetDef<TacCharacterDef>("NJ_Heavy7_CharacterTemplateDef"), "{88465F1E-64E1-4EAC-BCB2-A42CC8F915A8}", nameDef);
                harlson.Data.Name = "Colonel_Jack_Harlson";

                List<TacticalAbilityDef> abilities = new List<TacticalAbilityDef>()
                {
                jetpackControl, boomBlast, takedown, punisher


                };

                harlson.Data.Abilites = abilities.ToArray();


                WeaponDef archangel = DefCache.GetDef<WeaponDef>("NJ_HeavyRocketLauncher_WeaponDef");
                WeaponDef fireNade = DefCache.GetDef<WeaponDef>("NJ_IncindieryGrenade_WeaponDef");
                WeaponDef deceptor = DefCache.GetDef<WeaponDef>("NJ_Gauss_MachineGun_WeaponDef");

                WeaponDef guidedMissileLauncher = DefCache.GetDef<WeaponDef>("NJ_GuidedMissileLauncherPack_WeaponDef");

                TacticalItemDef hmgAmmo = DefCache.GetDef<TacticalItemDef>("NJ_Gauss_MachineGun_AmmoClip_ItemDef");
                TacticalItemDef hrAmmo = DefCache.GetDef<TacticalItemDef>("NJ_HeavyRocketLauncher_AmmoClip_ItemDef");
                TacticalItemDef gmAmmo = DefCache.GetDef<TacticalItemDef>("NJ_GuidedMissileLauncher_AmmoClip_ItemDef");


                EquipmentDef medkit = DefCache.GetDef<EquipmentDef>("Medkit_EquipmentDef");


                harlson.Data.EquipmentItems = new ItemDef[] { archangel, fireNade, medkit };
                harlson.Data.InventoryItems = new ItemDef[] { gmAmmo, gmAmmo, hrAmmo, hrAmmo, hrAmmo, hrAmmo };

                harlson.Data.LevelProgression.SetLevel(7);
                harlson.Data.Strength = 20;
                harlson.Data.Will = 14;
                harlson.Data.Speed = 10;

                GameTagDef characterTag = TFTVCommonMethods.CreateNewTag(nameDef, "{8AF3B063-8B77-4B3C-94BC-93A3D90B18C7}");
                GenderTagDef maleGenderTag = DefCache.GetDef<GenderTagDef>("Male_GenderTagDef");
                // FacialHairTagDef noFacialHairTag = DefCache.GetDef<FacialHairTagDef>("FacialHairTagDef_Empty");

                //  VoiceProfileTagDef newEmptyVoiceTag = Helper.CreateDefFromClone<VoiceProfileTagDef>(DefCache.GetDef<VoiceProfileTagDef>("1_VoiceProfileTagDef"), "{6935EA8D-95AB-4035-AB9B-B7390138733F}", "EmptyVoiceTag");

                // CustomizationColorTagDef_10 green
                // CustomizationColorTagDef_14 pink
                // CustomizationColorTagDef_0 grey
                // CustomizationColorTagDef_7 red

                CustomizationSecondaryColorTagDef secondaryBlackColor = DefCache.GetDef<CustomizationSecondaryColorTagDef>("CustomizationSecondaryColorTagDef_9");
                CustomizationSecondaryColorTagDef secondaryBlueColor = DefCache.GetDef<CustomizationSecondaryColorTagDef>("CustomizationSecondaryColorTagDef_2");

                CustomizationPrimaryColorTagDef whitePrimaryColor = DefCache.GetDef<CustomizationPrimaryColorTagDef>("CustomizationColorTagDef_5");
                CustomizationPrimaryColorTagDef blackPrimaryColor = DefCache.GetDef<CustomizationPrimaryColorTagDef>("CustomizationColorTagDef_9");
                CustomizationPrimaryColorTagDef greyPrimaryColor = DefCache.GetDef<CustomizationPrimaryColorTagDef>("CustomizationColorTagDef_0");

                CustomizationPatternTagDef noPattern = DefCache.GetDef<CustomizationPatternTagDef>("CustomizationPatternTagDef_0");
                CustomizationPatternTagDef linesPattern = DefCache.GetDef<CustomizationPatternTagDef>("CustomizationPatternTagDef_11");
                CustomizationPatternTagDef pattern9 = DefCache.GetDef<CustomizationPatternTagDef>("CustomizationPatternTagDef_8");

                // CustomizationPrimaryColorTagDef blackColor = DefCache.GetDef<CustomizationPrimaryColorTagDef>("CustomizationColorTagDef_9");
                List<GameTagDef> gameTags = harlson.Data.GameTags.ToList();
                gameTags.Add(greyPrimaryColor);
                gameTags.Add(secondaryBlackColor);
                gameTags.Add(noPattern);
                gameTags.Add(maleGenderTag);
                gameTags.Add(characterTag);

                harlson.SpawnCommandId = "HarlsonTFTV";
                harlson.Data.GameTags = gameTags.ToArray();
                harlson.CustomizationParams.KeepExistingCustomizationTags = true;



                TacticalItemDef head = DefCache.GetDef<TacticalItemDef>("NJ_Heavy_Helmet_BodyPartDef");

                SquadPortraitsDef squadPortraits = DefCache.GetDef<SquadPortraitsDef>("SquadPortraitsDef");

                TacticalItemDef newHead = Helper.CreateDefFromClone(head, "{D5A73379-CAA3-4C49-B9E3-FE37F4A2DD9A}", nameDef);
                newHead.ViewElementDef = Helper.CreateDefFromClone(head.ViewElementDef, "{879D3FB4-BCDF-4E79-BF27-E5100B60ECCC}", nameDef);
                newHead.BodyPartAspectDef = Helper.CreateDefFromClone(head.BodyPartAspectDef, "{99281C28-6764-444A-B06E-458B4374ED3B}", nameDef);

                TacticalItemDef legs = DefCache.GetDef<TacticalItemDef>("NJ_Heavy_Legs_ItemDef");
                TacticalItemDef torso = DefCache.GetDef<TacticalItemDef>("NJ_Heavy_Torso_BodyPartDef");

                harlson.Data.BodypartItems = new ItemDef[] { newHead, legs, torso, guidedMissileLauncher };


                squadPortraits.ManualPortraits.Add(new SquadPortraitsDef.ManualPortrait { HeadPart = newHead, Portrait = Helper.CreateSpriteFromImageFile("PM_Jack.jpg") });




                List<TacMissionTypeParticipantData.UniqueChatarcterBind> tacCharacterDefs = DefCache.GetDef<CustomMissionTypeDef>("NJVictory_CustomMissionTypeDef").ParticipantsData[0].UniqueUnits.ToList();
                TacMissionTypeParticipantData.UniqueChatarcterBind uniqueChatarcterBind = new TacMissionTypeParticipantData.UniqueChatarcterBind
                {
                    Character = harlson,
                    Amount = new RangeDataInt { Max = 1, Min = 1 },
                };
                tacCharacterDefs.Add(uniqueChatarcterBind);
                DefCache.GetDef<CustomMissionTypeDef>("NJVictory_CustomMissionTypeDef").ParticipantsData[0].UniqueUnits = tacCharacterDefs.ToArray();




            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }




        }

        private static void CreateRichter()
        {
            try
            {
                ShootAbilityDef aimedBurst = DefCache.GetDef<ShootAbilityDef>("AimedBurst_AbilityDef");
                PassiveModifierAbilityDef quarterback = DefCache.GetDef<PassiveModifierAbilityDef>("Pitcher_AbilityDef");
                ApplyStatusAbilityDef takedown = DefCache.GetDef<ApplyStatusAbilityDef>("BC_Takedown_AbilityDef");
                PassiveModifierAbilityDef punisher = DefCache.GetDef<PassiveModifierAbilityDef>("Punisher_AbilityDef");

                string nameDef = "Richter_TacCharacterDef";

                TacCharacterDef richter = Helper.CreateDefFromClone(DefCache.GetDef<TacCharacterDef>("NJ_Assault7_CharacterTemplateDef"), "{A275168C-03EA-4734-8B6D-A373E988C19B}", nameDef);
                richter.Data.Name = "Captain_Richter";
                List<TacticalAbilityDef> abilities = new List<TacticalAbilityDef>()
                {
                aimedBurst, quarterback, takedown, punisher


                };

                richter.Data.Abilites = abilities.ToArray();

                EquipmentDef medkit = DefCache.GetDef<EquipmentDef>("Medkit_EquipmentDef");
                WeaponDef deimosWhite = DefCache.GetDef<WeaponDef>("SY_LaserAssaultRifle_WhiteNeon_WeaponDef");
                WeaponDef poisonGrenade = DefCache.GetDef<WeaponDef>("SY_PoisonGrenade_WeaponDef");
                WeaponDef sonicGrenade = DefCache.GetDef<WeaponDef>("SY_SonicGrenade_WeaponDef");

                TacticalItemDef laserAmmo = DefCache.GetDef<TacticalItemDef>("SY_LaserAssaultRifle_AmmoClip_ItemDef");

                //  sofia.Data.EquipmentItems = new ItemDef[] { deimosWhite, poisonGrenade, sonicGrenade };
                //  sofia.Data.InventoryItems = new ItemDef[] { laserAmmo, laserAmmo, laserAmmo, medkit, medkit };

                richter.Data.LevelProgression.SetLevel(7);
                richter.Data.Strength = 16;
                richter.Data.Will = 14;
                richter.Data.Speed = 14;

                GameTagDef characterTag = TFTVCommonMethods.CreateNewTag(nameDef, "{AFCAF5E5-1E97-4564-9249-370AF8170756}");
                GenderTagDef maleGenderTag = DefCache.GetDef<GenderTagDef>("Male_GenderTagDef");
                // FacialHairTagDef noFacialHairTag = DefCache.GetDef<FacialHairTagDef>("FacialHairTagDef_Empty");

                //  VoiceProfileTagDef newEmptyVoiceTag = Helper.CreateDefFromClone<VoiceProfileTagDef>(DefCache.GetDef<VoiceProfileTagDef>("1_VoiceProfileTagDef"), "{6935EA8D-95AB-4035-AB9B-B7390138733F}", "EmptyVoiceTag");

                // CustomizationColorTagDef_10 green
                // CustomizationColorTagDef_14 pink
                // CustomizationColorTagDef_0 grey
                // CustomizationColorTagDef_7 red

                FacialHairTagDef beard = DefCache.GetDef<FacialHairTagDef>("FacialHairTagDef_Ind1");

                CustomizationSecondaryColorTagDef secondaryBlackColor = DefCache.GetDef<CustomizationSecondaryColorTagDef>("CustomizationSecondaryColorTagDef_9");
                CustomizationSecondaryColorTagDef secondaryBlueColor = DefCache.GetDef<CustomizationSecondaryColorTagDef>("CustomizationSecondaryColorTagDef_2");

                CustomizationPrimaryColorTagDef whitePrimaryColor = DefCache.GetDef<CustomizationPrimaryColorTagDef>("CustomizationColorTagDef_5");
                CustomizationPrimaryColorTagDef blackPrimaryColor = DefCache.GetDef<CustomizationPrimaryColorTagDef>("CustomizationColorTagDef_9");


                CustomizationPatternTagDef linesPattern = DefCache.GetDef<CustomizationPatternTagDef>("CustomizationPatternTagDef_11");
                CustomizationPatternTagDef pattern9 = DefCache.GetDef<CustomizationPatternTagDef>("CustomizationPatternTagDef_8");

                CustomizationHairColorTagDef whiteFacialHair = DefCache.GetDef<CustomizationHairColorTagDef>("CustomizationHairColorTagDef_6");
                RaceTagDef caucasian = DefCache.GetDef<RaceTagDef>("Caucasian_RaceTagDef");

                FaceTagDef face3 = DefCache.GetDef<FaceTagDef>("3_FaceTagDef");


                // CustomizationPrimaryColorTagDef blackColor = DefCache.GetDef<CustomizationPrimaryColorTagDef>("CustomizationColorTagDef_9");
                List<GameTagDef> gameTags = richter.Data.GameTags.ToList();
                gameTags.Add(caucasian);
                gameTags.Add(face3);
                gameTags.Add(beard);
                gameTags.Add(pattern9);
                gameTags.Add(whitePrimaryColor);
                gameTags.Add(secondaryBlueColor);
                gameTags.Add(whiteFacialHair);
                gameTags.Add(maleGenderTag);
                gameTags.Add(characterTag);

                richter.SpawnCommandId = "RichterTFTV";
                richter.Data.GameTags = gameTags.ToArray();
                richter.CustomizationParams.KeepExistingCustomizationTags = true;



                TacticalItemDef head = DefCache.GetDef<TacticalItemDef>("NJ_Assault_Helmet_BodyPartDef");

                SquadPortraitsDef squadPortraits = DefCache.GetDef<SquadPortraitsDef>("SquadPortraitsDef");

                TacticalItemDef newHead = Helper.CreateDefFromClone(head, "{9DC824DF-50CE-408C-9804-E37B9ECFD74C}", nameDef);
                newHead.ViewElementDef = Helper.CreateDefFromClone(head.ViewElementDef, "{346BF292-8F76-417F-B30E-83709F592A84}", nameDef);
                newHead.BodyPartAspectDef = Helper.CreateDefFromClone(head.BodyPartAspectDef, "{89A1F3F2-DB35-45D8-AE6E-C1C9C3F33704}", nameDef);

                TacticalItemDef legs = DefCache.GetDef<TacticalItemDef>("NJ_Assault_Legs_ItemDef");
                TacticalItemDef torso = DefCache.GetDef<TacticalItemDef>("NJ_Assault_Torso_BodyPartDef");

                richter.Data.BodypartItems = new ItemDef[] { newHead, legs, torso };


                squadPortraits.ManualPortraits.Add(new SquadPortraitsDef.ManualPortrait { HeadPart = newHead, Portrait = Helper.CreateSpriteFromImageFile("PM_Richter.jpg") });




                List<TacMissionTypeParticipantData.UniqueChatarcterBind> tacCharacterDefs = DefCache.GetDef<CustomMissionTypeDef>("NJVictory_CustomMissionTypeDef").ParticipantsData[0].UniqueUnits.ToList();
                TacMissionTypeParticipantData.UniqueChatarcterBind uniqueChatarcterBind = new TacMissionTypeParticipantData.UniqueChatarcterBind
                {
                    Character = richter,
                    Amount = new RangeDataInt { Max = 1, Min = 1 },
                };
                tacCharacterDefs.Add(uniqueChatarcterBind);
                DefCache.GetDef<CustomMissionTypeDef>("NJVictory_CustomMissionTypeDef").ParticipantsData[0].UniqueUnits = tacCharacterDefs.ToArray();





            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
        private static void CreateSofia()
        {
            try
            {
                ApplyStatusAbilityDef manualControl = DefCache.GetDef<ApplyStatusAbilityDef>("ManualControl_AbilityDef");
                PassiveModifierAbilityDef remoteDeployment = DefCache.GetDef<PassiveModifierAbilityDef>("RemoteDeployment_AbilityDef");
                ApplyStatusAbilityDef takedown = DefCache.GetDef<ApplyStatusAbilityDef>("BC_Takedown_AbilityDef");
                ApplyStatusAbilityDef arTargeting = DefCache.GetDef<ApplyStatusAbilityDef>("BC_ARTargeting_AbilityDef");

                string nameDef = "Sofia_TacCharacterDef";

                TacCharacterDef sofia = Helper.CreateDefFromClone(DefCache.GetDef<TacCharacterDef>("NJ_Technician7_CharacterTemplateDef"), "{033AA4BB-AA41-45AF-B84B-CFD3F1C76014}", nameDef);
                sofia.Data.Name = "Sophia_Villanova";

                List<TacticalAbilityDef> abilities = new List<TacticalAbilityDef>()
                {
                manualControl, remoteDeployment, takedown, arTargeting


                };

                sofia.Data.Abilites = abilities.ToArray();


                WeaponDef scorcher = DefCache.GetDef<WeaponDef>("PX_LaserPDW_WeaponDef");
                WeaponDef mechArms = DefCache.GetDef<WeaponDef>("NJ_Technician_MechArms_WeaponDef");

                TacticalItemDef laserAmmo = DefCache.GetDef<TacticalItemDef>("PX_LaserPDW_AmmoClip_ItemDef");
                TacticalItemDef mechArmsAmmo = DefCache.GetDef<TacticalItemDef>("MechArms_AmmoClip_ItemDef");
                EquipmentDef medkit = DefCache.GetDef<EquipmentDef>("Medkit_EquipmentDef");
                WeaponDef fireNade = DefCache.GetDef<WeaponDef>("NJ_IncindieryGrenade_WeaponDef");
                TacticalItemDef laserTurret = DefCache.GetDef<TacticalItemDef>("PX_LaserTechTurretItem_ItemDef");

                sofia.Data.EquipmentItems = new ItemDef[] { scorcher, laserTurret, laserTurret };
                sofia.Data.InventoryItems = new ItemDef[] { laserAmmo, mechArmsAmmo, mechArmsAmmo, laserAmmo };

                sofia.Data.LevelProgression.SetLevel(7);
                sofia.Data.Strength = 16;
                sofia.Data.Will = 14;
                sofia.Data.Speed = 14;

                GameTagDef sofiaTag = TFTVCommonMethods.CreateNewTag(nameDef, "{1B969433-9925-454D-9EF5-15AC081EC607}");
                GenderTagDef femaleGenderTag = DefCache.GetDef<GenderTagDef>("Female_GenderTagDef");
                // FacialHairTagDef noFacialHairTag = DefCache.GetDef<FacialHairTagDef>("FacialHairTagDef_Empty");

                //  VoiceProfileTagDef newEmptyVoiceTag = Helper.CreateDefFromClone<VoiceProfileTagDef>(DefCache.GetDef<VoiceProfileTagDef>("1_VoiceProfileTagDef"), "{6935EA8D-95AB-4035-AB9B-B7390138733F}", "EmptyVoiceTag");

                // CustomizationColorTagDef_10 green
                // CustomizationColorTagDef_14 pink
                // CustomizationColorTagDef_0 grey
                // CustomizationColorTagDef_7 red

                CustomizationSecondaryColorTagDef secondaryBlackColor = DefCache.GetDef<CustomizationSecondaryColorTagDef>("CustomizationSecondaryColorTagDef_9");
                CustomizationSecondaryColorTagDef secondaryBlueColor = DefCache.GetDef<CustomizationSecondaryColorTagDef>("CustomizationSecondaryColorTagDef_2");

                CustomizationPrimaryColorTagDef whitePrimaryColor = DefCache.GetDef<CustomizationPrimaryColorTagDef>("CustomizationColorTagDef_5");
                CustomizationPrimaryColorTagDef blackPrimaryColor = DefCache.GetDef<CustomizationPrimaryColorTagDef>("CustomizationColorTagDef_9");

                CustomizationPatternTagDef linesPattern = DefCache.GetDef<CustomizationPatternTagDef>("CustomizationPatternTagDef_11");
                CustomizationPatternTagDef pattern9 = DefCache.GetDef<CustomizationPatternTagDef>("CustomizationPatternTagDef_8");

                // CustomizationPrimaryColorTagDef blackColor = DefCache.GetDef<CustomizationPrimaryColorTagDef>("CustomizationColorTagDef_9");
                List<GameTagDef> gameTags = sofia.Data.GameTags.ToList();
                gameTags.Add(blackPrimaryColor);
                gameTags.Add(secondaryBlueColor);
                gameTags.Add(pattern9);
                gameTags.Add(femaleGenderTag);
                gameTags.Add(sofiaTag);

                sofia.SpawnCommandId = "SofiaTFTV";
                sofia.Data.GameTags = gameTags.ToArray();
                sofia.CustomizationParams.KeepExistingCustomizationTags = true;



                TacticalItemDef head = DefCache.GetDef<TacticalItemDef>("NJ_Technician_Helmet_BodyPartDef");

                SquadPortraitsDef squadPortraits = DefCache.GetDef<SquadPortraitsDef>("SquadPortraitsDef");

                TacticalItemDef newHead = Helper.CreateDefFromClone(head, "{354840D7-6543-4381-854E-472B5B126CE7}", nameDef);
                newHead.ViewElementDef = Helper.CreateDefFromClone(head.ViewElementDef, "{358AB930-0194-419B-BE25-2AADFFE8E97E}", nameDef);
                newHead.BodyPartAspectDef = Helper.CreateDefFromClone(head.BodyPartAspectDef, "{34155BEC-A605-4A0A-91A6-E1723606F118}", nameDef);

                TacticalItemDef legs = DefCache.GetDef<TacticalItemDef>("NJ_Technician_Legs_ItemDef");
                TacticalItemDef torso = DefCache.GetDef<TacticalItemDef>("NJ_Technician_Torso_BodyPartDef");

                sofia.Data.BodypartItems = new ItemDef[] { newHead, legs, torso, mechArms };


                squadPortraits.ManualPortraits.Add(new SquadPortraitsDef.ManualPortrait { HeadPart = newHead, Portrait = Helper.CreateSpriteFromImageFile("PM_Sofia.jpg") });




                List<TacMissionTypeParticipantData.UniqueChatarcterBind> tacCharacterDefs = DefCache.GetDef<CustomMissionTypeDef>("NJVictory_CustomMissionTypeDef").ParticipantsData[0].UniqueUnits.ToList();
                TacMissionTypeParticipantData.UniqueChatarcterBind uniqueChatarcterBind = new TacMissionTypeParticipantData.UniqueChatarcterBind
                {
                    Character = sofia,
                    Amount = new RangeDataInt { Max = 1, Min = 1 },
                };
                tacCharacterDefs.Add(uniqueChatarcterBind);
                DefCache.GetDef<CustomMissionTypeDef>("NJVictory_CustomMissionTypeDef").ParticipantsData[0].UniqueUnits = tacCharacterDefs.ToArray();



            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }





        private static void CreateStas()
        {
            try
            {
                OverwatchFocusAbilityDef overwatchFocus = DefCache.GetDef<OverwatchFocusAbilityDef>("OverwatchFocus_AbilityDef");
                ApplyStatusAbilityDef saboteur = DefCache.GetDef<ApplyStatusAbilityDef>("Saboteur_AbilityDef");
                RepositionAbilityDef vanish = DefCache.GetDef<RepositionAbilityDef>("Vanish_AbilityDef");
                ShootAbilityDef deployDronePack = DefCache.GetDef<ShootAbilityDef>("DeployDronePack_ShootAbilityDef");

                string nameDef = "Stas_TacCharacterDef";

                TacCharacterDef stas = Helper.CreateDefFromClone(DefCache.GetDef<TacCharacterDef>("SY_Infiltrator7_CharacterTemplateDef"), "{FBB2FE80-E86B-4C0F-9B02-19E52FF1F745}", nameDef);
                stas.Data.Name = "Stas";

                List<TacticalAbilityDef> abilities = new List<TacticalAbilityDef>()
                {
                saboteur, overwatchFocus, vanish, deployDronePack


                };

                stas.Data.Abilites = abilities.ToArray();


                WeaponDef laserSniper = DefCache.GetDef<WeaponDef>("SY_LaserSniperRifle_WeaponDef");
                WeaponDef laserPistol = DefCache.GetDef<WeaponDef>("SY_LaserPistol_WeaponDef");
                EquipmentDef medkit = DefCache.GetDef<EquipmentDef>("Medkit_EquipmentDef");
                WeaponDef poisonGrenade = DefCache.GetDef<WeaponDef>("SY_PoisonGrenade_WeaponDef");
                WeaponDef sonicGrenade = DefCache.GetDef<WeaponDef>("SY_SonicGrenade_WeaponDef");

                TacticalItemDef laserRifleAmmo = DefCache.GetDef<TacticalItemDef>("SY_LaserSniperRifle_AmmoClip_ItemDef");
                TacticalItemDef laserPistolAmmo = DefCache.GetDef<TacticalItemDef>("SY_LaserPistol_AmmoClip_ItemDef");


                //   stas.Data.EquipmentItems = new ItemDef[] { laserSniper, laserPistol, medkit };
                //  stas.Data.InventoryItems = new ItemDef[] { laserRifleAmmo, laserRifleAmmo, laserPistolAmmo, laserPistolAmmo, medkit };

                stas.Data.LevelProgression.SetLevel(7);
                stas.Data.Strength = 16;
                stas.Data.Will = 14;
                stas.Data.Speed = 14;

                GameTagDef nikolaiTag = TFTVCommonMethods.CreateNewTag(nameDef, "{17647EF3-1D4D-4F9C-8525-6F8C3ADD9B5A}");
                GenderTagDef maleGenderTag = DefCache.GetDef<GenderTagDef>("Male_GenderTagDef");
                // FacialHairTagDef noFacialHairTag = DefCache.GetDef<FacialHairTagDef>("FacialHairTagDef_Empty");

                //  VoiceProfileTagDef newEmptyVoiceTag = Helper.CreateDefFromClone<VoiceProfileTagDef>(DefCache.GetDef<VoiceProfileTagDef>("1_VoiceProfileTagDef"), "{6935EA8D-95AB-4035-AB9B-B7390138733F}", "EmptyVoiceTag");

                // CustomizationColorTagDef_10 green
                // CustomizationColorTagDef_14 pink
                // CustomizationColorTagDef_0 grey
                // CustomizationColorTagDef_7 red

                //CustomizationPatternTagDef_11
                //CustomizationSecondaryColorTagDef_9



                List<GameTagDef> gameTags = stas.Data.GameTags.ToList();

                gameTags.Add(maleGenderTag);
                gameTags.Add(nikolaiTag);

                stas.SpawnCommandId = "StasTFTV";
                stas.Data.GameTags = gameTags.ToArray();
                stas.CustomizationParams.KeepExistingCustomizationTags = true;


                TacticalItemDef head = DefCache.GetDef<TacticalItemDef>("SY_Infiltrator_Bonus_Helmet_BodyPartDef");

                SquadPortraitsDef squadPortraits = DefCache.GetDef<SquadPortraitsDef>("SquadPortraitsDef");

                TacticalItemDef newHead = Helper.CreateDefFromClone(head, "{734C5B3A-DA43-4045-B10D-E3799866D98D}", nameDef);
                newHead.ViewElementDef = Helper.CreateDefFromClone(head.ViewElementDef, "{BDC0706A-8A86-4E20-B479-CAA65856E4FC}", nameDef);
                newHead.BodyPartAspectDef = Helper.CreateDefFromClone(head.BodyPartAspectDef, "{F4D611AB-B89D-40E4-AAD6-6382BCE5D74B}", nameDef);

                TacticalItemDef legs = DefCache.GetDef<TacticalItemDef>("SY_Infiltrator_Bonus_Legs_ItemDef");
                TacticalItemDef torso = DefCache.GetDef<TacticalItemDef>("SY_Infiltrator_Bonus_Torso_BodyPartDef");

                stas.Data.BodypartItems = new ItemDef[] { newHead, legs, torso };


                squadPortraits.ManualPortraits.Add(new SquadPortraitsDef.ManualPortrait { HeadPart = newHead, Portrait = Helper.CreateSpriteFromImageFile("PM_Stas.jpg") });




                List<TacMissionTypeParticipantData.UniqueChatarcterBind> tacCharacterDefs = DefCache.GetDef<CustomMissionTypeDef>("SYPolyVictory_CustomMissionTypeDef").ParticipantsData[0].UniqueUnits.ToList();
                TacMissionTypeParticipantData.UniqueChatarcterBind uniqueChatarcterBind = new TacMissionTypeParticipantData.UniqueChatarcterBind
                {
                    Character = stas,
                    Amount = new RangeDataInt { Max = 1, Min = 1 },
                };
                tacCharacterDefs.Add(uniqueChatarcterBind);
                DefCache.GetDef<CustomMissionTypeDef>("SYPolyVictory_CustomMissionTypeDef").ParticipantsData[0].UniqueUnits = tacCharacterDefs.ToArray();
                DefCache.GetDef<CustomMissionTypeDef>("SYTerraVictory_CustomMissionTypeDef").ParticipantsData[0].UniqueUnits = tacCharacterDefs.ToArray();










            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }


        private static void CreateNikolai()
        {
            try
            {
                PassiveModifierAbilityDef endurance = DefCache.GetDef<PassiveModifierAbilityDef>("Endurance_AbilityDef");
                OverwatchFocusAbilityDef overwatchFocus = DefCache.GetDef<OverwatchFocusAbilityDef>("OverwatchFocus_AbilityDef");

                ShootAbilityDef gunslinger = DefCache.GetDef<ShootAbilityDef>("BC_Gunslinger_AbilityDef");
                PassiveModifierAbilityDef killzone = DefCache.GetDef<PassiveModifierAbilityDef>("KillZone_AbilityDef");

                List<TacticalAbilityDef> abilitiesToAdd = new List<TacticalAbilityDef>()
                {
                endurance, overwatchFocus, gunslinger, killzone

                };

                string nameDef = "Nikolai_TacCharacterDef";

                TacCharacterDef nikolai = Helper.CreateDefFromClone(DefCache.GetDef<TacCharacterDef>("SY_Sniper7_CharacterTemplateDef"), "{99DA6A62-BF24-471C-B966-1954C6F5A9E1}", nameDef);
                nikolai.Data.Name = "Nikolai";

                nikolai.Data.Abilites = abilitiesToAdd.ToArray();


                WeaponDef laserSniper = DefCache.GetDef<WeaponDef>("SY_LaserSniperRifle_WeaponDef");
                WeaponDef laserPistol = DefCache.GetDef<WeaponDef>("SY_LaserPistol_WeaponDef");
                EquipmentDef medkit = DefCache.GetDef<EquipmentDef>("Medkit_EquipmentDef");
                WeaponDef poisonGrenade = DefCache.GetDef<WeaponDef>("SY_PoisonGrenade_WeaponDef");
                WeaponDef sonicGrenade = DefCache.GetDef<WeaponDef>("SY_SonicGrenade_WeaponDef");

                TacticalItemDef laserRifleAmmo = DefCache.GetDef<TacticalItemDef>("SY_LaserSniperRifle_AmmoClip_ItemDef");
                TacticalItemDef laserPistolAmmo = DefCache.GetDef<TacticalItemDef>("SY_LaserPistol_AmmoClip_ItemDef");


                nikolai.Data.EquipmentItems = new ItemDef[] { laserSniper, laserPistol, medkit };
                nikolai.Data.InventoryItems = new ItemDef[] { laserRifleAmmo, laserRifleAmmo, laserPistolAmmo, laserPistolAmmo, medkit };

                nikolai.Data.LevelProgression.SetLevel(7);
                nikolai.Data.Strength = 16;
                nikolai.Data.Will = 14;
                nikolai.Data.Speed = 14;

                GameTagDef nikolaiTag = TFTVCommonMethods.CreateNewTag(nameDef, "{E9013ABC-E6C3-4F43-876D-B1DE64053F75}");
                GenderTagDef maleGenderTag = DefCache.GetDef<GenderTagDef>("Male_GenderTagDef");
                // FacialHairTagDef noFacialHairTag = DefCache.GetDef<FacialHairTagDef>("FacialHairTagDef_Empty");

                //  VoiceProfileTagDef newEmptyVoiceTag = Helper.CreateDefFromClone<VoiceProfileTagDef>(DefCache.GetDef<VoiceProfileTagDef>("1_VoiceProfileTagDef"), "{6935EA8D-95AB-4035-AB9B-B7390138733F}", "EmptyVoiceTag");

                // CustomizationColorTagDef_10 green
                // CustomizationColorTagDef_14 pink
                // CustomizationColorTagDef_0 grey
                // CustomizationColorTagDef_7 red

                //CustomizationPatternTagDef_11
                //CustomizationSecondaryColorTagDef_9

                CustomizationSecondaryColorTagDef secondaryBlackColor = DefCache.GetDef<CustomizationSecondaryColorTagDef>("CustomizationSecondaryColorTagDef_9");

                CustomizationPrimaryColorTagDef whitePrimaryColor = DefCache.GetDef<CustomizationPrimaryColorTagDef>("CustomizationColorTagDef_5");

                CustomizationPatternTagDef linesPattern = DefCache.GetDef<CustomizationPatternTagDef>("CustomizationPatternTagDef_11");

                List<GameTagDef> gameTags = nikolai.Data.GameTags.ToList();
                gameTags.Add(secondaryBlackColor);
                gameTags.Add(whitePrimaryColor);
                gameTags.Add(linesPattern);
                gameTags.Add(maleGenderTag);
                gameTags.Add(nikolaiTag);

                nikolai.SpawnCommandId = "NikolaiTFTV";
                nikolai.Data.GameTags = gameTags.ToArray();
                nikolai.CustomizationParams.KeepExistingCustomizationTags = true;


                TacticalItemDef head = DefCache.GetDef<TacticalItemDef>("SY_Sniper_Helmet_BodyPartDef");

                SquadPortraitsDef squadPortraits = DefCache.GetDef<SquadPortraitsDef>("SquadPortraitsDef");

                TacticalItemDef newHead = Helper.CreateDefFromClone(head, "{FF4FF18F-B701-4CE5-94F6-DF513A349072}", nameDef);
                newHead.ViewElementDef = Helper.CreateDefFromClone(head.ViewElementDef, "{9F811161-BC19-45F6-BA4B-B17910101CA7}", nameDef);
                newHead.BodyPartAspectDef = Helper.CreateDefFromClone(head.BodyPartAspectDef, "{06D4E1A5-B036-4683-9B5B-DE2864F2D4A9}", nameDef);

                TacticalItemDef legs = DefCache.GetDef<TacticalItemDef>("SY_Sniper_Legs_ItemDef");
                TacticalItemDef torso = DefCache.GetDef<TacticalItemDef>("SY_Sniper_Torso_BodyPartDef");

                nikolai.Data.BodypartItems = new ItemDef[] { newHead, legs, torso };


                squadPortraits.ManualPortraits.Add(new SquadPortraitsDef.ManualPortrait { HeadPart = newHead, Portrait = Helper.CreateSpriteFromImageFile("PM_Nikolai.jpg") });




                List<TacMissionTypeParticipantData.UniqueChatarcterBind> tacCharacterDefs = DefCache.GetDef<CustomMissionTypeDef>("SYPolyVictory_CustomMissionTypeDef").ParticipantsData[0].UniqueUnits.ToList();
                TacMissionTypeParticipantData.UniqueChatarcterBind uniqueChatarcterBind = new TacMissionTypeParticipantData.UniqueChatarcterBind
                {
                    Character = nikolai,
                    Amount = new RangeDataInt { Max = 1, Min = 1 },
                };
                tacCharacterDefs.Add(uniqueChatarcterBind);
                DefCache.GetDef<CustomMissionTypeDef>("SYPolyVictory_CustomMissionTypeDef").ParticipantsData[0].UniqueUnits = tacCharacterDefs.ToArray();
                DefCache.GetDef<CustomMissionTypeDef>("SYTerraVictory_CustomMissionTypeDef").ParticipantsData[0].UniqueUnits = tacCharacterDefs.ToArray();






            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }



        private static void CreateZhara()
        {
            try
            {

                PassiveModifierAbilityDef endurance = DefCache.GetDef<PassiveModifierAbilityDef>("Endurance_AbilityDef");
                OverwatchFocusAbilityDef overwatchFocus = DefCache.GetDef<OverwatchFocusAbilityDef>("OverwatchFocus_AbilityDef");

                ShootAbilityDef aimedBurst = DefCache.GetDef<ShootAbilityDef>("AimedBurst_AbilityDef");
                PassiveModifierAbilityDef quarterback = DefCache.GetDef<PassiveModifierAbilityDef>("Pitcher_AbilityDef");

                string nameDef = "Zhara_TacCharacterDef";

                TacCharacterDef zhara = Helper.CreateDefFromClone(DefCache.GetDef<TacCharacterDef>("SY_Assault7_CharacterTemplateDef"), "{CBC16AB7-7469-4251-AF06-35122B4412DD}", nameDef);
                zhara.Data.Name = "Zhara";

                List<TacticalAbilityDef> abilities = new List<TacticalAbilityDef>()
                {
                endurance, overwatchFocus, aimedBurst, quarterback


                };

                zhara.Data.Abilites = abilities.ToArray();

                EquipmentDef medkit = DefCache.GetDef<EquipmentDef>("Medkit_EquipmentDef");
                WeaponDef deimosWhite = DefCache.GetDef<WeaponDef>("SY_LaserAssaultRifle_WhiteNeon_WeaponDef");
                WeaponDef poisonGrenade = DefCache.GetDef<WeaponDef>("SY_PoisonGrenade_WeaponDef");
                WeaponDef sonicGrenade = DefCache.GetDef<WeaponDef>("SY_SonicGrenade_WeaponDef");

                TacticalItemDef laserAmmo = DefCache.GetDef<TacticalItemDef>("SY_LaserAssaultRifle_AmmoClip_ItemDef");

                zhara.Data.EquipmentItems = new ItemDef[] { deimosWhite, poisonGrenade, sonicGrenade };
                zhara.Data.InventoryItems = new ItemDef[] { laserAmmo, laserAmmo, laserAmmo, medkit, medkit };

                zhara.Data.LevelProgression.SetLevel(7);
                zhara.Data.Strength = 16;
                zhara.Data.Will = 14;
                zhara.Data.Speed = 14;

                GameTagDef zharaTag = TFTVCommonMethods.CreateNewTag(nameDef, "{24DB53A2-3710-4900-A15B-D1B673BED535}");
                GenderTagDef femaleGenderTag = DefCache.GetDef<GenderTagDef>("Female_GenderTagDef");
                // FacialHairTagDef noFacialHairTag = DefCache.GetDef<FacialHairTagDef>("FacialHairTagDef_Empty");

                //  VoiceProfileTagDef newEmptyVoiceTag = Helper.CreateDefFromClone<VoiceProfileTagDef>(DefCache.GetDef<VoiceProfileTagDef>("1_VoiceProfileTagDef"), "{6935EA8D-95AB-4035-AB9B-B7390138733F}", "EmptyVoiceTag");

                // CustomizationColorTagDef_10 green
                // CustomizationColorTagDef_14 pink
                // CustomizationColorTagDef_0 grey
                // CustomizationColorTagDef_7 red

                // CustomizationPrimaryColorTagDef blackColor = DefCache.GetDef<CustomizationPrimaryColorTagDef>("CustomizationColorTagDef_9");
                List<GameTagDef> gameTags = zhara.Data.GameTags.ToList();
                //   gameTags.Add(blackColor);
                gameTags.Add(femaleGenderTag);
                gameTags.Add(zharaTag);

                zhara.SpawnCommandId = "ZharaTFTV";
                zhara.Data.GameTags = gameTags.ToArray();
                zhara.CustomizationParams.KeepExistingCustomizationTags = true;


                TacticalItemDef assaultHead = DefCache.GetDef<TacticalItemDef>("SY_Assault_Helmet_WhiteNeon_BodyPartDef");

                SquadPortraitsDef squadPortraits = DefCache.GetDef<SquadPortraitsDef>("SquadPortraitsDef");

                TacticalItemDef zharaHead = Helper.CreateDefFromClone(assaultHead, "{D583A19E-2238-431D-BD70-4A058E2B46EC}", "ZharaHead_ItemDef");
                zharaHead.ViewElementDef = Helper.CreateDefFromClone(assaultHead.ViewElementDef, "{3ADA66FA-2307-4D48-96CB-959882176617}", "ZharaHead_ItemDef");
                zharaHead.BodyPartAspectDef = Helper.CreateDefFromClone(assaultHead.BodyPartAspectDef, "{B1160987-6DD3-410E-B6D9-536274CC0645}", "ZharaHead_ItemDef");

                TacticalItemDef assaultLegs = DefCache.GetDef<TacticalItemDef>("SY_Assault_Legs_WhiteNeon_ItemDef");
                TacticalItemDef assaultTorso = DefCache.GetDef<TacticalItemDef>("SY_Assault_Torso_WhiteNeon_BodyPartDef");

                zhara.Data.BodypartItems = new ItemDef[] { zharaHead, assaultLegs, assaultTorso };


                squadPortraits.ManualPortraits.Add(new SquadPortraitsDef.ManualPortrait { HeadPart = zharaHead, Portrait = Helper.CreateSpriteFromImageFile("PM_Zhara.jpg") });




                List<TacMissionTypeParticipantData.UniqueChatarcterBind> tacCharacterDefs = DefCache.GetDef<CustomMissionTypeDef>("SYPolyVictory_CustomMissionTypeDef").ParticipantsData[0].UniqueUnits.ToList();
                TacMissionTypeParticipantData.UniqueChatarcterBind uniqueChatarcterBind = new TacMissionTypeParticipantData.UniqueChatarcterBind
                {
                    Character = zhara,
                    Amount = new RangeDataInt { Max = 1, Min = 1 },
                };
                tacCharacterDefs.Add(uniqueChatarcterBind);
                DefCache.GetDef<CustomMissionTypeDef>("SYPolyVictory_CustomMissionTypeDef").ParticipantsData[0].UniqueUnits = tacCharacterDefs.ToArray();
                DefCache.GetDef<CustomMissionTypeDef>("SYTerraVictory_CustomMissionTypeDef").ParticipantsData[0].UniqueUnits = tacCharacterDefs.ToArray();

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }




        private static void CreateTaxiarchNergal()
        {
            try
            {
                // ApplyEffectAbilityDef LayWaste_AbilityDef
                //ApplyStatusAbilityDef BC_Biochemist_AbilityDef

                ApplyEffectAbilityDef mistBreather = DefCache.GetDef<ApplyEffectAbilityDef>("MistBreather_AbilityDef");
                ApplyStatusAbilityDef sowerOfChange = DefCache.GetDef<ApplyStatusAbilityDef>("SowerOfChange_AbilityDef");


                ShootAbilityDef aimedBurst = DefCache.GetDef<ShootAbilityDef>("AimedBurst_AbilityDef");
                PassiveModifierAbilityDef quarterback = DefCache.GetDef<PassiveModifierAbilityDef>("Pitcher_AbilityDef");

                string nameDef = "TaxiarchNergal_TacCharacterDef";

                TacCharacterDef taxiarchNergal = Helper.CreateDefFromClone(DefCache.GetDef<TacCharacterDef>("AN_Assault7_CharacterTemplateDef"), "{3AA9BBC1-FCE2-4274-AEA1-7CD00E3677DC}", nameDef);
                taxiarchNergal.Data.Name = "Taxiarch_Nergal";

                List<TacticalAbilityDef> abilities = new List<TacticalAbilityDef>()
                {
                mistBreather, sowerOfChange, aimedBurst, quarterback


                };

                taxiarchNergal.Data.Abilites = abilities.ToArray();

                WeaponDef shreddingShotgun = DefCache.GetDef<WeaponDef>("AN_ShreddingShotgun_WeaponDef");
                WeaponDef acidGrenade = DefCache.GetDef<WeaponDef>("AN_AcidGrenade_WeaponDef");
                TacticalItemDef shreddingAmmo = DefCache.GetDef<TacticalItemDef>("AN_ShreddingShotgun_AmmoClip_ItemDef");
                EquipmentDef medkit = DefCache.GetDef<EquipmentDef>("Medkit_EquipmentDef");

                taxiarchNergal.Data.EquipmentItems = new ItemDef[] { shreddingShotgun, acidGrenade, medkit };
                taxiarchNergal.Data.InventoryItems = new ItemDef[] { shreddingAmmo, shreddingAmmo, shreddingAmmo };

                taxiarchNergal.Data.LevelProgression.SetLevel(7);
                taxiarchNergal.Data.Strength = 16;
                taxiarchNergal.Data.Will = 14;
                taxiarchNergal.Data.Speed = 14;

                GameTagDef taxiarchTag = TFTVCommonMethods.CreateNewTag(nameDef, "{AD9711B0-2A39-4E82-BF9C-BDB8111C3697}");
                GenderTagDef maleGenderTag = DefCache.GetDef<GenderTagDef>("Male_GenderTagDef");
                FacialHairTagDef noFacialHairTag = DefCache.GetDef<FacialHairTagDef>("FacialHairTagDef_Empty");

                //  VoiceProfileTagDef newEmptyVoiceTag = Helper.CreateDefFromClone<VoiceProfileTagDef>(DefCache.GetDef<VoiceProfileTagDef>("1_VoiceProfileTagDef"), "{6935EA8D-95AB-4035-AB9B-B7390138733F}", "EmptyVoiceTag");

                // CustomizationColorTagDef_10 green
                // CustomizationColorTagDef_14 pink
                // CustomizationColorTagDef_0 grey
                // CustomizationColorTagDef_7 red

                CustomizationPrimaryColorTagDef blackColor = DefCache.GetDef<CustomizationPrimaryColorTagDef>("CustomizationColorTagDef_9");
                List<GameTagDef> gameTags = taxiarchNergal.Data.GameTags.ToList();
                gameTags.Add(blackColor);
                gameTags.Add(maleGenderTag);
                gameTags.Add(taxiarchTag);
                gameTags.Add(noFacialHairTag);
                //   gameTags.Add(newEmptyVoiceTag);
                taxiarchNergal.SpawnCommandId = "TaxiarchNergalTFTV";
                taxiarchNergal.Data.GameTags = gameTags.ToArray();
                taxiarchNergal.CustomizationParams.KeepExistingCustomizationTags = true;


                TacticalItemDef assaultHead = DefCache.GetDef<TacticalItemDef>("AN_Assault_Helmet_BodyPartDef");

                SquadPortraitsDef squadPortraits = DefCache.GetDef<SquadPortraitsDef>("SquadPortraitsDef");

                TacticalItemDef taxiarchNergalHead = Helper.CreateDefFromClone(assaultHead, "{6BA24E77-F104-4979-A8CC-720B988AB344}", "TaxiarchNergalHead_ItemDef");
                taxiarchNergalHead.ViewElementDef = Helper.CreateDefFromClone(assaultHead.ViewElementDef, "{064E1B24-E796-4E6D-97CF-00EF59BF1FC6}", "TaxiarchNergalHead_ItemDef");

                taxiarchNergalHead.BodyPartAspectDef = Helper.CreateDefFromClone(assaultHead.BodyPartAspectDef, "{A7FAAFE1-3EF6-4DB7-A5B1-43FC3DE2A335}", "TaxiarchNergalHead_ItemDef");

                TacticalItemDef assaultLegs = DefCache.GetDef<TacticalItemDef>("AN_Assault_Legs_ItemDef");
                TacticalItemDef assaultTorso = DefCache.GetDef<TacticalItemDef>("AN_Assault_Torso_BodyPartDef");

                taxiarchNergal.Data.BodypartItems = new ItemDef[] { taxiarchNergalHead, assaultLegs, assaultTorso };


                squadPortraits.ManualPortraits.Add(new SquadPortraitsDef.ManualPortrait { HeadPart = taxiarchNergalHead, Portrait = Helper.CreateSpriteFromImageFile("PM_Taxiarch_Nergal.jpg") });




                List<TacMissionTypeParticipantData.UniqueChatarcterBind> tacCharacterDefs = DefCache.GetDef<CustomMissionTypeDef>("ANVictory_CustomMissionTypeDef").ParticipantsData[1].UniqueUnits.ToList();
                TacMissionTypeParticipantData.UniqueChatarcterBind uniqueChatarcterBind = new TacMissionTypeParticipantData.UniqueChatarcterBind
                {
                    Character = taxiarchNergal,
                    Amount = new Base.Utils.RangeDataInt { Max = 1, Min = 1 },
                };
                tacCharacterDefs.Add(uniqueChatarcterBind);
                DefCache.GetDef<CustomMissionTypeDef>("ANVictory_CustomMissionTypeDef").ParticipantsData[0].UniqueUnits = tacCharacterDefs.ToArray();


            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }





        }

        private static void ChangePalaceMissionDefs()
        {
            try
            {

                string newActivatedStatusName = "YuggothianThingyActivated";

                TacStatusDef tacStatusDef = DefCache.GetDef<TacStatusDef>("ActiveHackableChannelingConsole_StatusDef");
                TacStatusDef newActivatedStatusDef = Helper.CreateDefFromClone(tacStatusDef, "{813BC5B3-143C-4B0A-B449-6AFBAA3B3792}", newActivatedStatusName);
                newActivatedStatusDef.EffectName = newActivatedStatusName;

                ActivateConsoleFactionObjectiveDef interactWithYRObjectivePX = DefCache.GetDef<ActivateConsoleFactionObjectiveDef>("InteractWithYuggothianPX_CustomMissionObjective");
                ActivateConsoleFactionObjectiveDef interactWithYRObjectiveNJ = DefCache.GetDef<ActivateConsoleFactionObjectiveDef>("InteractWithYuggothianBeacon_CustomMissionObjective");
                ActivateConsoleFactionObjectiveDef interactWithYRObjectiveAnu = DefCache.GetDef<ActivateConsoleFactionObjectiveDef>("InteractWithYuggothianExalted_CustomMissionObjective");
                ActivateConsoleFactionObjectiveDef interactWithYRObjectiveSyPoly = DefCache.GetDef<ActivateConsoleFactionObjectiveDef>("InteractWithYuggothianPoly_CustomMissionObjective");
                ActivateConsoleFactionObjectiveDef interactWithYRObjectiveSyTerra = DefCache.GetDef<ActivateConsoleFactionObjectiveDef>("InteractWithYuggothianTerra_CustomMissionObjective");

                List<ActivateConsoleFactionObjectiveDef> victoryMissionInteractObjectives = new List<ActivateConsoleFactionObjectiveDef>()
                    {
                        interactWithYRObjectivePX,
                        interactWithYRObjectiveAnu,
                        interactWithYRObjectiveNJ,
                        interactWithYRObjectiveSyPoly,
                        interactWithYRObjectiveSyTerra
                    };

                foreach (ActivateConsoleFactionObjectiveDef activateConsoleFactionObjectiveDef in victoryMissionInteractObjectives)
                {
                    activateConsoleFactionObjectiveDef.ObjectiveData.ActivatedInteractableStatusDef = newActivatedStatusDef;
                    activateConsoleFactionObjectiveDef.IsDefeatObjective = false;

                }

                CustomMissionTypeDef pxPalaceMissionDef = DefCache.GetDef<CustomMissionTypeDef>("PXVictory_CustomMissionTypeDef");

                pxPalaceMissionDef.Tags.Add(TFTVCommonMethods.CreateNewMissionTag("PXPalace", "{0CF66B9B-2E8F-4195-A688-A52DECD1982A}"));

                CustomMissionTypeDef njPalaceMissionDef = DefCache.GetDef<CustomMissionTypeDef>("NJVictory_CustomMissionTypeDef");
                njPalaceMissionDef.Tags.Add(TFTVCommonMethods.CreateNewMissionTag("NJPalace", "{5D7A9365-7BC2-4CAA-9D0E-2B6A06FA67A3}"));
                njPalaceMissionDef.MaxPlayerUnits = 7;

                CustomMissionTypeDef anuPalaceMissionDef = DefCache.GetDef<CustomMissionTypeDef>("ANVictory_CustomMissionTypeDef");
                anuPalaceMissionDef.Tags.Add(TFTVCommonMethods.CreateNewMissionTag("ANPalace", "{AAFC6643-110D-48AB-8730-AC7A86C6B8F3}"));
                anuPalaceMissionDef.MaxPlayerUnits = 7;

                CustomMissionTypeDef syPolyPalaceMissionDef = DefCache.GetDef<CustomMissionTypeDef>("SYPolyVictory_CustomMissionTypeDef");
                syPolyPalaceMissionDef.Tags.Add(TFTVCommonMethods.CreateNewMissionTag("SYPolyPalace", "{B8156DBC-5188-436C-A6B1-B00EA5362A11}"));
                syPolyPalaceMissionDef.MaxPlayerUnits = 7;

                CustomMissionTypeDef syTerraPalaceMissionDef = DefCache.GetDef<CustomMissionTypeDef>("SYTerraVictory_CustomMissionTypeDef");
                syTerraPalaceMissionDef.Tags.Add(TFTVCommonMethods.CreateNewMissionTag("SYTerraPalace", "{D2049387-C2C7-426A-82DB-E367851B5437}"));
                syTerraPalaceMissionDef.MaxPlayerUnits = 7;

                List<CustomMissionTypeDef> victoryMissions = new List<CustomMissionTypeDef>()
                    {
                    pxPalaceMissionDef,
                    njPalaceMissionDef,
                   // anuPalaceMissionDef,
                    syPolyPalaceMissionDef,
                    syTerraPalaceMissionDef
                    };

                anuPalaceMissionDef.ParticipantsData[1].ActorDeployParams.Clear();
                anuPalaceMissionDef.CustomObjectives = new FactionObjectiveDef[] { anuPalaceMissionDef.CustomObjectives[0].NextOnSuccess[0].NextOnSuccess[0], anuPalaceMissionDef.CustomObjectives[1] };

                foreach (CustomMissionTypeDef customMissionTypeDef in victoryMissions)
                {
                    customMissionTypeDef.ParticipantsData[1].ActorDeployParams.Clear();
                    customMissionTypeDef.CustomObjectives = new FactionObjectiveDef[] { customMissionTypeDef.CustomObjectives[0], customMissionTypeDef.CustomObjectives[1], customMissionTypeDef.CustomObjectives[2].NextOnSuccess[0].NextOnSuccess[0] };



                    //  pxPalaceMissionDef.ParticipantsData[1].ActorDeployParams.Clear();
                    //  pxPalaceMissionDef.CustomObjectives = new FactionObjectiveDef[] { pxPalaceMissionDef.CustomObjectives[0], pxPalaceMissionDef.CustomObjectives[1], interactWithYRObjectivePX };



                }



            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }





        /// <summary>
        /// This DR is only used when Stronger Pandorans is switched on. However, it has to be created always in case a tactical save is loaded
        /// straight from title screen; otherwise the game will never finish loading.
        /// </summary>

        private static void CreateScyllaDamageResistanceForStrongerPandorans()
        {

            try
            {

                StandardDamageTypeEffectDef fireDamage = DefCache.GetDef<StandardDamageTypeEffectDef>("Fire_StandardDamageTypeEffectDef");
                StandardDamageTypeEffectDef standardDamageTypeEffectDef = DefCache.GetDef<StandardDamageTypeEffectDef>("Projectile_StandardDamageTypeEffectDef");
                AcidDamageTypeEffectDef acidDamage = DefCache.GetDef<AcidDamageTypeEffectDef>("Acid_DamageOverTimeDamageTypeEffectDef");

                string statusName = "ScyllaDamageResistance";
                string gUID = "{CE61D05C-5A75-4354-BEC8-73EC0357F971}";
                string gUIDVisuals = "{6272B177-49AA-4F81-9C05-9CB9026A26C5}";

                DamageMultiplierStatusDef source = DefCache.GetDef<DamageMultiplierStatusDef>("BionicResistances_StatusDef");

                //   TFTVLogger.Always($"{source.DamageTypeDefs.Count()}");

                DamageMultiplierStatusDef newStatus = Helper.CreateDefFromClone(
                    source,
                    gUID,
                    statusName);
                newStatus.EffectName = statusName;
                newStatus.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.Hidden;
                newStatus.VisibleOnPassiveBar = true;
                newStatus.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;
                newStatus.ApplicationConditions = new EffectConditionDef[] { };

                newStatus.Visuals = Helper.CreateDefFromClone(
                    source.Visuals,
                    gUIDVisuals,
                    statusName + "Visuals");
                newStatus.Multiplier = 0.75f;
                newStatus.MultiplierType = DamageMultiplierType.Incoming;
                newStatus.Range = -1;
                newStatus.DamageTypeDefs = source.DamageTypeDefs;

                //   TFTVLogger.Always($"{newStatus.DamageTypeDefs.Count()}");

                List<DamageTypeBaseEffectDef> damageTypeBaseEffectDefs = new List<DamageTypeBaseEffectDef>();
                damageTypeBaseEffectDefs.AddRange(newStatus.DamageTypeDefs);
                damageTypeBaseEffectDefs.Add(fireDamage);
                damageTypeBaseEffectDefs.Add(standardDamageTypeEffectDef);
                damageTypeBaseEffectDefs.Add(acidDamage);

                //     TFTVLogger.Always($"damageTypeBaseEffectDefs {damageTypeBaseEffectDefs.Count()}");

                newStatus.DamageTypeDefs = damageTypeBaseEffectDefs.ToArray();

                //  TFTVLogger.Always($"{newStatus.DamageTypeDefs.Count()}");

                newStatus.Visuals.LargeIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_HunkerDown_2-2.png");
                newStatus.Visuals.SmallIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_HunkerDown_2-2.png");


                newStatus.Visuals.DisplayName1.LocalizationKey = "SCYLLA_DAMAGERESISTANCE_NAME";
                newStatus.Visuals.Description.LocalizationKey = "SCYLLA_DAMAGERESISTANCE_DESCRIPTION";



            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
       

        private static void CreateObjectiveCaptureCapacity()
        {
            try
            {
                TFTVCommonMethods.CreateObjectiveReminder("{25590AE4-872B-4679-A15C-300C3DC48A53}", "CAPTURE_CAPACITY_AIRCRAFT", 0);
                TFTVCommonMethods.CreateObjectiveReminder("{4EB4A290-8FE7-45CC-BF8B-914C52441EF4}", "CAPTURE_CAPACITY_BASE", 0);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        private static void StealAircraftMissionsNoItemRecovery()
        {
            try
            {
                DefCache.GetDef<CustomMissionTypeDef>("StealAircraftAN_CustomMissionTypeDef").DontRecoverItems = true;
                DefCache.GetDef<CustomMissionTypeDef>("StealAircraftNJ_CustomMissionTypeDef").DontRecoverItems = true;
                DefCache.GetDef<CustomMissionTypeDef>("StealAircraftSY_CustomMissionTypeDef").DontRecoverItems = true;


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        private static void CreateFoodPoisoningEvents()
        {
            try
            {
                string event1Name = "FoodPoisoning1";
                string event1Title = "FOOD_POISONING_TITLE_1";
                string event1Description = "FOOD_POISONING_DESCRIPTION_1";
                string event1Outcome = "FOOD_POISONING_OUTCOME_1";

                string event2Name = "FoodPoisoning2";
                string event2Title = "FOOD_POISONING_TITLE_2";
                string event2Description = "FOOD_POISONING_DESCRIPTION_2";
                string event2Outcome = "FOOD_POISONING_OUTCOME_2";

                string event3Name = "FoodPoisoning3";
                string event3Title = "FOOD_POISONING_TITLE_3";
                string event3Description = "FOOD_POISONING_DESCRIPTION_3";
                string event3Outcome = "FOOD_POISONING_OUTCOME_3";

                GeoscapeEventDef foodPoisoning1 = TFTVCommonMethods.CreateNewEvent(event1Name, event1Title, event1Description, event1Outcome);
                foodPoisoning1.GeoscapeEventData.Choices[0].Outcome.DamageAllSoldiers = 20;
                foodPoisoning1.GeoscapeEventData.Choices[0].Outcome.TireAllSoldiers = 10;


                GeoscapeEventDef foodPoisoning2 = TFTVCommonMethods.CreateNewEvent(event2Name, event2Title, event2Description, event2Outcome);
                foodPoisoning2.GeoscapeEventData.Choices[0].Outcome.DamageAllSoldiers = 40;
                foodPoisoning2.GeoscapeEventData.Choices[0].Outcome.TireAllSoldiers = 20;

                GeoscapeEventDef foodPoisoning3 = TFTVCommonMethods.CreateNewEvent(event3Name, event3Title, event3Description, event3Outcome);
                foodPoisoning3.GeoscapeEventData.Choices[0].Outcome.DamageAllSoldiers = 80;
                foodPoisoning3.GeoscapeEventData.Choices[0].Outcome.TireAllSoldiers = 40;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        private static void CreateReinforcementTag()
        {
            try
            {
                TFTVCommonMethods.CreateNewTag("ReinforcementTag", "{19762255-93FC-4A7B-877D-914A3BD152C9}");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        private static void ChangeVehicleInventorySlots()
        {
            try
            {

                DefCache.GetDef<BackpackFilterDef>("VehicleBackpackFilterDef").MaxItems = 12;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void MakeVestsOnlyForOrganicMeatbags()
        {
            try
            {
                GameTagDef organicMeatbagTorsoTag = TFTVCommonMethods.CreateNewTag("MeatBagTorso", "{8D13AAD6-BA65-4907-B3C8-C977B819BF48}");

                foreach (TacticalItemDef item in Repo.GetAllDefs<TacticalItemDef>()

                    .Where(ti => ti.name.Contains("Torso")).Where(ti => !ti.name.Contains("BIO"))

                    .Where(ti => ti.name.StartsWith("AN_") || ti.name.StartsWith("SY_") || ti.name.StartsWith("NJ_")
                    || ti.name.StartsWith("NEU") || ti.name.StartsWith("PX_") || ti.name.StartsWith("IN_")))

                {
                    if (!item.Tags.Contains(organicMeatbagTorsoTag))
                    {
                        item.Tags.Add(organicMeatbagTorsoTag);
                    }
                }


                TacticalItemDef fireVest = DefCache.GetDef<TacticalItemDef>("NJ_FireResistanceVest_Attachment_ItemDef");
                TacticalItemDef poisonVest = DefCache.GetDef<TacticalItemDef>("SY_PoisonResistanceVest_Attachment_ItemDef");
                TacticalItemDef blastVest = DefCache.GetDef<TacticalItemDef>("PX_BlastResistanceVest_Attachment_ItemDef");
                TacticalItemDef nanoVest = DefCache.GetDef<TacticalItemDef>("NanotechVest");

                blastVest.RequiredSlotBinds[0].GameTagFilter = organicMeatbagTorsoTag;
                poisonVest.RequiredSlotBinds[0].GameTagFilter = organicMeatbagTorsoTag;
                fireVest.RequiredSlotBinds[0].GameTagFilter = organicMeatbagTorsoTag;
                nanoVest.RequiredSlotBinds[0].GameTagFilter = organicMeatbagTorsoTag;

                TacticalItemDef indepHeavyArmor = DefCache.GetDef<TacticalItemDef>("IN_Heavy_Torso_BodyPartDef");
                TacticalItemDef njHeavyArmor = DefCache.GetDef<TacticalItemDef>("NJ_Heavy_Torso_BodyPartDef");
                indepHeavyArmor.ProvidedSlots = new AddonDef.ProvidedSlotBind[] { indepHeavyArmor.ProvidedSlots[0], njHeavyArmor.ProvidedSlots[1] };
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }


        internal static void ChangesModulesAndAcid()
        {
            try
            {
                ChangeModulePictures();
                RemoveAcidAsVulnerability();
                CreateNanoVestAbilityAndStatus();
                CreateHealingMultiplierAbility();
                CreateParalysisDamageResistance();
                ModifyPoisonResVest();
                ModifyBlastAndFireResVests();
                CreateAcidResistantVest();
                CreateNanotechVest();
                AdjustResearches();
                RemoveRepairKitFromPure();
                AdjustAcidDamage();
                MakeVestsOnlyForOrganicMeatbags();
                MakeMistRepellerLegModule();
                CreateNanotechFieldkit();
            }


            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }


        internal static void AdjustAcidDamage()
        {
            try
            {

                DefCache.GetDef<DamagePayloadEffectDef>("E_Element0 [SwarmerAcidExplosion_Die_AbilityDef]").DamagePayload.DamageKeywords[1].Value = 30;

                DefCache.GetDef<WeaponDef>("AcidSwarmer_Torso_BodyPartDef").DamagePayload.DamageKeywords[1].Value = 20;

                //All Acheron acid attacks reduced by 10.

                /*[TFTV @ 7/13/2023 2:15:32 PM] AN_AcidGrenade_WeaponDef does 20 acid damage
[TFTV @ 7/13/2023 2:15:32 PM] AN_AcidHandGun_WeaponDef does 10 acid damage
[TFTV @ 7/13/2023 2:15:32 PM] KS_Redemptor_WeaponDef does 5 acid damage
[TFTV @ 7/13/2023 2:15:32 PM] FS_AssaultGrenadeLauncher_WeaponDef does 10 acid damage
[TFTV @ 7/13/2023 2:15:32 PM] PX_AcidCannon_WeaponDef does 40 acid damage
[TFTV @ 7/13/2023 2:15:32 PM] PX_AcidAssaultRifle_WeaponDef does 10 acid damage
[TFTV @ 7/13/2023 2:15:32 PM] Mutoid_Head_AcidSpray_WeaponDef does 30 acid damage
[TFTV @ 7/13/2023 2:15:32 PM] AcheronAchlys_Arms_WeaponDef does 30 acid damage
[TFTV @ 7/13/2023 2:15:32 PM] AcheronAchlysChampion_Arms_WeaponDef does 30 acid damage
[TFTV @ 7/13/2023 2:15:32 PM] Acheron_Arms_WeaponDef does 20 acid damage
[TFTV @ 7/13/2023 2:15:32 PM] AcheronPrime_Arms_WeaponDef does 30 acid damage
[TFTV @ 7/13/2023 2:15:32 PM] Chiron_Abdomen_Acid_Mortar_WeaponDef does 10 acid damage
[TFTV @ 7/13/2023 2:15:32 PM] Crabman_LeftHand_Acid_Grenade_WeaponDef does 10 acid damage
[TFTV @ 7/13/2023 2:15:32 PM] Crabman_LeftHand_Acid_EliteGrenade_WeaponDef does 20 acid damage
[TFTV @ 7/13/2023 2:15:32 PM] Siren_Torso_AcidSpitter_WeaponDef does 30 acid damage
[TFTV @ 7/13/2023 2:15:32 PM] Siren_Torso_Orichalcum_WeaponDef does 40 acid damage
[TFTV @ 7/13/2023 2:15:32 PM] AcidSwarmer_Torso_BodyPartDef does 30 acid damage*/



            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }




        }

        private static void MakeMistRepellerLegModule()
        {
            try
            {

                TacticalItemDef gooRepeller = DefCache.GetDef<TacticalItemDef>("PX_GooRepeller_Attachment_ItemDef");
                TacticalItemDef mistRepeller = DefCache.GetDef<TacticalItemDef>("SY_MistRepeller_Attachment_ItemDef");
                mistRepeller.RequiredSlotBinds = gooRepeller.RequiredSlotBinds;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

        internal static void RemoveRepairKitFromPure()
        {

            try
            {
                EquipmentDef repairKit = DefCache.GetDef<EquipmentDef>("FieldRepairKit_EquipmentDef");
                WeaponDef grenade = DefCache.GetDef<WeaponDef>("PX_HandGrenade_WeaponDef");
                foreach (TacCharacterDef tacCharacterDef in Repo.GetAllDefs<TacCharacterDef>().Where(tc => tc.Data.EquipmentItems.Any(ei => ei == repairKit)))
                {


                    List<ItemDef> itemDefs = tacCharacterDef.Data.EquipmentItems.ToList();
                    itemDefs.Remove(repairKit);
                    itemDefs.Add(grenade);
                    tacCharacterDef.Data.EquipmentItems = itemDefs.ToArray();
                    //  TFTVLogger.Always($"removed {repairKit.name} and gave {grenade.name} to {tacCharacterDef.name}");

                }





            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        private static void CreateNanoVestAbilityAndStatus()
        {
            string skillName = "NanoVest_AbilityDef";
            ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("CloseQuarters_AbilityDef");
            ApplyStatusAbilityDef nanoVestAbility = Helper.CreateDefFromClone(
                source,
                "{FEF02379-A90F-4670-8FD7-574CDCB5753F}",
                skillName);
            nanoVestAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "{15458996-77C2-4F9B-8E31-0DD1A6D77571}",
                skillName);
            nanoVestAbility.TargetingDataDef = DefCache.GetDef<ApplyStatusAbilityDef>("QuickAim_AbilityDef").TargetingDataDef;
            nanoVestAbility.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "{8959A8C5-0405-4D46-8632-0CCA9EF029DB}",
                skillName);
            nanoVestAbility.ViewElementDef.ShowInInventoryItemTooltip = true;

            nanoVestAbility.ViewElementDef.DisplayName1.LocalizationKey = "NANOVEST_ABILITY_NAME";
            nanoVestAbility.ViewElementDef.Description.LocalizationKey = "NANOVEST_ABILITY_DESCRIPTION";
            nanoVestAbility.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile("module_nanovest_ability.png");
            nanoVestAbility.ViewElementDef.SmallIcon = nanoVestAbility.ViewElementDef.LargeIcon;

            string statusName = "NanoVest_StatusDef";
            ItemSlotStatsModifyStatusDef nanoVestBuffStatus = Helper.CreateDefFromClone(
                DefCache.GetDef<ItemSlotStatsModifyStatusDef>("E_Status [ElectricReinforcement_AbilityDef]"),
                "{57E033FC-FECD-4A9E-8AE6-CC17FB5116A9}",
                statusName);
            nanoVestBuffStatus.Visuals = Helper.CreateDefFromClone(
                nanoVestAbility.ViewElementDef,
                "{8F111A9C-020C-4166-9444-1211CF517884}",
                statusName);

            nanoVestBuffStatus.Duration = -1;

            nanoVestBuffStatus.Visuals.DisplayName1.LocalizationKey = "NANOVEST_ABILITY_NAME";
            nanoVestBuffStatus.Visuals.Description.LocalizationKey = "NANOVEST_ABILITY_DESCRIPTION";
            nanoVestBuffStatus.Visuals.LargeIcon = nanoVestAbility.ViewElementDef.LargeIcon;
            nanoVestBuffStatus.Visuals.SmallIcon = nanoVestAbility.ViewElementDef.LargeIcon;
            nanoVestBuffStatus.StatsModifications = new ItemSlotModification[]
            {
        new ItemSlotModification()
        {
            Type = StatType.Health,
            ModificationType = StatModificationType.AddMax,
            Value = 10f,
            ShowsNotification = false,
            NotifyOnce = false
        },
        new ItemSlotModification()
        {
            Type = StatType.Health,
            ModificationType = StatModificationType.AddRestrictedToBounds,
            Value = 10f,
            ShowsNotification = true,
            NotifyOnce = true
        }
            };

            nanoVestAbility.StatusDef = nanoVestBuffStatus;
        }

        internal static void ModifyBlastAndFireResVests()
        {
            try
            {


                TacticalItemDef blastVest = DefCache.GetDef<TacticalItemDef>("PX_BlastResistanceVest_Attachment_ItemDef");
                TacticalItemDef fireVest = DefCache.GetDef<TacticalItemDef>("NJ_FireResistanceVest_Attachment_ItemDef");
                blastVest.Abilities = new AbilityDef[] { fireVest.Abilities[0], blastVest.Abilities[0] };
                blastVest.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile("modules_blastresvest.png");
                blastVest.ViewElementDef.InventoryIcon = blastVest.ViewElementDef.LargeIcon;



            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }



        }

        internal static void CreateNanotechVest()
        {
            try
            {

                TacticalItemDef fireVest = DefCache.GetDef<TacticalItemDef>("NJ_FireResistanceVest_Attachment_ItemDef");

                FactionTagDef nJTag = DefCache.GetDef<FactionTagDef>("NewJerico_FactionTagDef");
                FactionTagDef pXTag = DefCache.GetDef<FactionTagDef>("PhoenixPoint_FactionTagDef");


                if (fireVest.Tags.Contains(nJTag))
                {
                    fireVest.Tags.Remove(nJTag);

                }

                if (!fireVest.Tags.Contains(pXTag))
                {
                    fireVest.Tags.Add(pXTag);

                }

                TacticalItemDef newNanoVest = Helper.CreateDefFromClone(fireVest, "{D07B639A-E1F4-46F4-91BB-1CCDCCCE8EC1}", "NanotechVest");
                newNanoVest.ViewElementDef = Helper.CreateDefFromClone(fireVest.ViewElementDef, "{0F1BD9BA-1895-46C7-90AF-26FB92D702F6}", "Nanotech_ViewElement");


                newNanoVest.Abilities = new AbilityDef[] { DefCache.GetDef<ApplyStatusAbilityDef>("NanoVest_AbilityDef") };
                newNanoVest.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile("modules_nanovest.png");
                newNanoVest.ViewElementDef.DisplayName1.LocalizationKey = "NANOVEST_NAME";
                newNanoVest.ViewElementDef.DisplayName2.LocalizationKey = "NANOVEST_NAME";
                newNanoVest.ViewElementDef.Description.LocalizationKey = "NANOVEST_DESCRIPTION";
                newNanoVest.ViewElementDef.InventoryIcon = newNanoVest.ViewElementDef.LargeIcon;

                newNanoVest.ManufactureTech = 20;
                newNanoVest.ManufactureMaterials = 30;

            }


            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

        internal static void CreateAcidResistantVest()
        {
            try
            {
                TacticalItemDef fireVest = DefCache.GetDef<TacticalItemDef>("NJ_FireResistanceVest_Attachment_ItemDef");
                fireVest.Abilities = new AbilityDef[] { DefCache.GetDef<DamageMultiplierAbilityDef>("AcidResistant_DamageMultiplierAbilityDef") };
                fireVest.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile("modules_fireresvest.png");
                fireVest.ViewElementDef.InventoryIcon = fireVest.ViewElementDef.LargeIcon;


            }


            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

        internal static void CreateNanotechFieldkit()
        {
            try
            {

                EquipmentDef repairKit = DefCache.GetDef<EquipmentDef>("FieldRepairKit_EquipmentDef");
                HealAbilityDef repairKitAbility = DefCache.GetDef<HealAbilityDef>("FieldRepairKit_AbilityDef");

                Sprite nanotechFieldkitAbilityIcon = Helper.CreateSpriteFromImageFile("nanotechfieldkit.png");

                repairKit.ManufactureMaterials = 30;
                repairKit.ManufactureTech = 10;


                //need to create a new heal ability

                string nameAbility = "DoTMedkit";
                string gUIDAbility = "{180418BE-8DC8-467E-80FE-D012A51BE5A9}";
                HealAbilityDef sourceHealAbility = DefCache.GetDef<HealAbilityDef>("Medkit_AbilityDef");
                HealAbilityDef newDoTMedkitAbility = Helper.CreateDefFromClone(sourceHealAbility, gUIDAbility, nameAbility);
                newDoTMedkitAbility.ViewElementDef = Helper.CreateDefFromClone(sourceHealAbility.ViewElementDef, "{DB136772-7CDF-4FC4-B07B-72867E43E16E}", nameAbility);

                newDoTMedkitAbility.ViewElementDef.InventoryIcon = nanotechFieldkitAbilityIcon;
                newDoTMedkitAbility.ViewElementDef.LargeIcon = nanotechFieldkitAbilityIcon;
                newDoTMedkitAbility.ViewElementDef.SmallIcon = nanotechFieldkitAbilityIcon;
                newDoTMedkitAbility.ViewElementDef.DisplayName1.LocalizationKey = "KEY_REPAIR_KIT_ABILITY_NAME";
                newDoTMedkitAbility.ViewElementDef.Description.LocalizationKey = "KEY_REPAIR_KIT_ABILITY_DESCRIPTION";

                repairKit.Abilities[0] = newDoTMedkitAbility;


                //modify ability
                //need new MultiEffectDef. Copy from CureSpray, because it has everything we need.

                //Make Cure Spray/Cure Cloud remove acid
                string abilityName = "AcidStatusRemover";
                StatusRemoverEffectDef sourceStatusRemoverEffect = DefCache.GetDef<StatusRemoverEffectDef>("StrainedRemover_EffectDef");
                StatusRemoverEffectDef newAcidStatusRemoverEffect = Helper.CreateDefFromClone(sourceStatusRemoverEffect, "0AE26C25-A67D-4F2F-B036-F7649B26B695", abilityName);
                newAcidStatusRemoverEffect.StatusToRemove = "Acid";



                string nameMultiEffect = "DoTMedkitMultiEffect";
                string gUIDMultiEffect = "{5B0EBBAE-F126-418C-B6F2-7E2FA44EBFBD}";
                MultiEffectDef sourceMultiEffect = DefCache.GetDef<MultiEffectDef>("Cure_MultiEffectDef");
                MultiEffectDef newMultiEffect = Helper.CreateDefFromClone(sourceMultiEffect, gUIDMultiEffect, nameMultiEffect);

                List<EffectDef> effectDefsList = newMultiEffect.EffectDefs.ToList();
                effectDefsList.Add(newAcidStatusRemoverEffect);
                newMultiEffect.EffectDefs = effectDefsList.ToArray();

                //  TFTVLogger.Always($"{newMultiEffect.EffectDefs.Count()}");

                OrEffectConditionDef sourceOrEffectCondition = DefCache.GetDef<OrEffectConditionDef>("CanBeHealed_StandardMedkit_ApplicationCondition");

                OrEffectConditionDef newEffectCondtions = Helper.CreateDefFromClone(sourceOrEffectCondition, "{ECFC2136-17BA-4FD0-A5BA-B9A1C456353E}", "DoTMedkitEffectCondtiions");

                newEffectCondtions.OrConditions = new EffectConditionDef[]
                {
                TFTVCommonMethods.CreateNewStatusEffectCondition("{0D32B04B-8EAA-4C76-9F24-F92F0FE8CD74}", DefCache.GetDef<StatusDef>("ActorStunned_StatusDef")),
                TFTVCommonMethods.CreateNewStatusEffectCondition("{BF5726D7-5E9C-4145-85E8-79545CBB3261}", DefCache.GetDef<StatusDef>("Acid_StatusDef")),
               TFTVCommonMethods.CreateNewStatusEffectCondition("{177E042A-B8F8-4302-9520-CC0610C045B0}", DefCache.GetDef<StatusDef>("Blinded_StatusDef")),
                TFTVCommonMethods.CreateNewStatusEffectCondition("{A054A669-8C7B-4005-8749-BA6CD71163CA}", DefCache.GetDef<StatusDef>("Slowed_StatusDef")),
               TFTVCommonMethods.CreateNewStatusEffectCondition("{F574791A-FAD0-4E1F-9295-5F2A3D9AAB2C}", DefCache.GetDef<StatusDef>("Trembling_StatusDef")),
                TFTVCommonMethods.CreateNewStatusEffectCondition("{A66DC742-B60F-409B-8B63-2D6AC7B5AD1D}", DefCache.GetDef<StatusDef>("Bleed_StatusDef")),
                DefCache.GetDef< ActorHasStatusEffectConditionDef>("HasParalysisStatus_ApplicationCondition"),
                DefCache.GetDef< ActorHasStatusEffectConditionDef>("HasParalysedStatus_ApplicationCondition"),
                DefCache.GetDef< ActorHasStatusEffectConditionDef>("HasInfectedStatus_ApplicationCondition"),
                DefCache.GetDef< ActorHasStatusEffectConditionDef>("HasPoisonStatus_ApplicationCondition"),


            };

                LocalizedTextBind nanotTechDescription = new LocalizedTextBind("KEY_REPAIR_KIT_ABILITY_DESCRIPTION");

                // string effectDescriptionText = "Removes all acid, bleeding, blind, paralyzed, poisoned, slowed, stun, trembling, viral status from the target.";
                ConditionalHealEffect conditionalHealEffect = new ConditionalHealEffect()

                {
                    HealerConditions = new EffectConditionDef[] { },
                    TargetGenerationConditions = new EffectConditionDef[] { newEffectCondtions },
                    AdditionalEffectDef = newMultiEffect,
                    EffectDescription = nanotTechDescription
                };

                newDoTMedkitAbility.HealEffects = new List<ConditionalHealEffect>() { conditionalHealEffect };
                newDoTMedkitAbility.GeneralHealAmount = 0.1f;



            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        internal static void CreateHealingMultiplierAbility()
        {
            try
            {
                DamageMultiplierAbilityDef damageMultiplierAbilityDefSource = DefCache.GetDef<DamageMultiplierAbilityDef>("EMPResistant_DamageMultiplierAbilityDef");
                DamageMultiplierAbilityDef healingMultiplierAbility = Helper.CreateDefFromClone(damageMultiplierAbilityDefSource, "{39D33BA7-726A-417F-9DC7-42CD4E6762FD}", "ExtraHealing_DamageMultiplierAbilityDef");
                healingMultiplierAbility.DamageTypeDef = DefCache.GetDef<StandardDamageTypeEffectDef>("Healing_StandardDamageTypeEffectDef");
                healingMultiplierAbility.Multiplier = 1.25f;
                healingMultiplierAbility.MultiplierType = DamageMultiplierType.Incoming;
                healingMultiplierAbility.ViewElementDef = Helper.CreateDefFromClone(damageMultiplierAbilityDefSource.ViewElementDef, "{63C00610-5CAE-4152-9002-7A0F7C90AE30}", "ExtraHealing_ViewElementDef");
                healingMultiplierAbility.ViewElementDef.DisplayName1.LocalizationKey = "EXTRAHEALING_NAME";
                healingMultiplierAbility.ViewElementDef.Description.LocalizationKey = "EXTRAHEALING_DESCRIPTION";
                healingMultiplierAbility.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_ExpertHealer-2.png");
                healingMultiplierAbility.ViewElementDef.SmallIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_ExpertHealer-2.png");

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        internal static void CreateParalysisDamageResistance()
        {
            try
            {
                DamageMultiplierAbilityDef damageMultiplierAbilityDefSource = DefCache.GetDef<DamageMultiplierAbilityDef>("EMPResistant_DamageMultiplierAbilityDef");
                DamageMultiplierAbilityDef ParalysisNotShcokResistance = Helper.CreateDefFromClone(damageMultiplierAbilityDefSource, "{A044047F-A462-46FC-B06A-191181B67800}", "ParalysisNotShockImmunityResistance_DamageMultiplierAbilityDef");
                ParalysisNotShcokResistance.DamageTypeDef = DefCache.GetDef<DamageOverTimeDamageTypeEffectDef>("Paralysis_DamageOverTimeDamageTypeEffectDef");
                ParalysisNotShcokResistance.Multiplier = 0.5f;
                ParalysisNotShcokResistance.ViewElementDef = Helper.CreateDefFromClone(damageMultiplierAbilityDefSource.ViewElementDef, "{F157A5A2-16A0-491A-ABE8-6CF88DEBE1DF}", "ParalysisNotShockImmunityResistance_ViewElementDef");
                ParalysisNotShcokResistance.ViewElementDef.DisplayName1.LocalizationKey = "RESISTANCE_TO_PARALYSIS_NAME";
                ParalysisNotShcokResistance.ViewElementDef.Description.LocalizationKey = "RESISTANCE_TO_PARALYSIS_DESCRIPTION";
                ParalysisNotShcokResistance.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile("ParalysisImmunity.png");
                ParalysisNotShcokResistance.ViewElementDef.SmallIcon = Helper.CreateSpriteFromImageFile("ParalysisImmunity.png");

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        internal static void ModifyPoisonResVest()
        {
            try
            {
                TacticalItemDef poisonVest = DefCache.GetDef<TacticalItemDef>("SY_PoisonResistanceVest_Attachment_ItemDef");
                DamageMultiplierAbilityDef ParalysisNotShcokResistance = DefCache.GetDef<DamageMultiplierAbilityDef>("ParalysisNotShockImmunityResistance_DamageMultiplierAbilityDef");

                //Not working correctly
                //DamageMultiplierAbilityDef ExtraHealing = DefCache.GetDef<DamageMultiplierAbilityDef>("ExtraHealing_DamageMultiplierAbilityDef");

                poisonVest.Abilities = new AbilityDef[] { poisonVest.Abilities[0], ParalysisNotShcokResistance };




            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }



        }

        internal static void RemoveAcidAsVulnerability()
        {
            try
            {

                DefCache.GetDef<DamageMultiplierStatusDef>("BionicVulnerabilities_StatusDef").DamageTypeDefs = new DamageTypeBaseEffectDef[] { };
                DefCache.GetDef<DamageMultiplierStatusDef>("BionicVulnerabilities_StatusDef").Multiplier = 1;

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }



        }

        internal static void AdjustResearches()
        {
            try
            {
                ResearchDef terrorSentinelResearch = DefCache.GetDef<ResearchDef>("PX_Alien_TerrorSentinel_ResearchDef");
                ManufactureResearchRewardDef advNanotechRewards = DefCache.GetDef<ManufactureResearchRewardDef>("SYN_NanoTech_ResearchDef_ManufactureResearchRewardDef_0");
                ManufactureResearchRewardDef newRewardsForTerrorSentinel = Helper.CreateDefFromClone(advNanotechRewards, "{41636380-9889-4D4A-8E0A-8D32A9196DD1}", terrorSentinelResearch.name + "ManuReward");

                ResearchDef reverseEngineeringMVS = DefCache.GetDef<ResearchDef>("PX_SY_MultiVisualSensor_Attachment_ItemDef_ResearchDef");

                ResearchDef reverseEngineeringMotionDetector = DefCache.GetDef<ResearchDef>("PX_SY_MotionDetector_Attachment_ItemDef_ResearchDef");

                ResearchDef reverseEngineeringAcidVest = DefCache.GetDef<ResearchDef>("PX_NJ_FireResistanceVest_Attachment_ItemDef_ResearchDef");

                ResearchDbDef pxResearch = DefCache.GetDef<ResearchDbDef>("pp_ResearchDB");

                if (pxResearch.Researches.Contains(reverseEngineeringMVS))
                {
                    pxResearch.Researches.Remove(reverseEngineeringMVS);
                }

                if (pxResearch.Researches.Contains(reverseEngineeringMotionDetector))
                {
                    pxResearch.Researches.Remove(reverseEngineeringMotionDetector);

                }

                if (pxResearch.Researches.Contains(reverseEngineeringAcidVest))
                {
                    pxResearch.Researches.Remove(reverseEngineeringAcidVest);

                }

                //Moving Motion Detection Module to Terror Sentinel Autopsy               
                terrorSentinelResearch.Unlocks = new ResearchRewardDef[] { terrorSentinelResearch.Unlocks[0], newRewardsForTerrorSentinel };

                //Remove adv nanotech buff and add Repair Kit to manufacturing reward

                ResearchDef advNanotechRes = DefCache.GetDef<ResearchDef>("SYN_NanoTech_ResearchDef");
                //  advNanotechRes.ViewElementDef.BenefitsText = new LocalizedTextBind() { }; // DefCache.GetDef<ResearchViewElementDef>("PX_ShardGun_ViewElementDef").BenefitsText;
                advNanotechRes.Unlocks = new ResearchRewardDef[] { advNanotechRes.Unlocks[0] };

                EquipmentDef repairKit = DefCache.GetDef<EquipmentDef>("FieldRepairKit_EquipmentDef");


                //removing nanokit from Bionic Reserach
                ManufactureResearchRewardDef bionicsReward = DefCache.GetDef<ManufactureResearchRewardDef>("NJ_Bionics1_ResearchDef_ManufactureResearchRewardDef_0");
                bionicsReward.Items = new ItemDef[] { bionicsReward.Items[0], bionicsReward.Items[1], bionicsReward.Items[2] };


                TacticalItemDef newNanoVest = DefCache.GetDef<TacticalItemDef>("NanotechVest");

                List<ItemDef> manuRewards = new List<ItemDef>() { repairKit, newNanoVest };
                advNanotechRewards.Items = manuRewards.ToArray();

                TacticalItemDef blastVest = DefCache.GetDef<TacticalItemDef>("PX_BlastResistanceVest_Attachment_ItemDef");
                ManufactureResearchRewardDef njFireReward = DefCache.GetDef<ManufactureResearchRewardDef>("NJ_PurificationTech_ResearchDef_ManufactureResearchRewardDef_0");
                List<ItemDef> itemDefs = new List<ItemDef>(njFireReward.Items) { blastVest };
                njFireReward.Items = itemDefs.ToArray();
                //remove NJ Fire Resistance tech, folding it into fire tech

                ResearchDef fireTech = DefCache.GetDef<ResearchDef>("NJ_PurificationTech_ResearchDef");

                /* List<ResearchRewardDef> fireTechRewards = fireTech.Unlocks.ToList();
                 fireTechRewards.Add(njFireResReward);
                 fireTech.Unlocks = fireTechRewards.ToArray();*/

                ResearchDbDef njResearch = DefCache.GetDef<ResearchDbDef>("nj_ResearchDB");
                njResearch.Researches.Remove(DefCache.GetDef<ResearchDef>("NJ_FireResistanceTech_ResearchDef"));

                //Fireworm unlocks Vidar
                DefCache.GetDef<ExistingResearchRequirementDef>("PX_AGL_ResearchDef_ExistingResearchRequirementDef_0").ResearchID = "PX_Alien_Fireworm_ResearchDef";

                //Blast res research changed to acid res, because blast vest moved to NJ Fire Tech research
                //Acidworm unlocks BlastResTech, which is now AcidResTech
                TacticalItemDef acidVest = DefCache.GetDef<TacticalItemDef>("NJ_FireResistanceVest_Attachment_ItemDef");
                ManufactureResearchRewardDef pxBlastResReward = DefCache.GetDef<ManufactureResearchRewardDef>("PX_BlastResistanceVest_ResearchDef_ManufactureResearchRewardDef_0");
                pxBlastResReward.Items = new ItemDef[] { acidVest };
                DefCache.GetDef<ExistingResearchRequirementDef>("PX_BlastResistanceVest_ResearchDef_ExistingResearchRequirementDef_0").ResearchID = "PX_Alien_Acidworm_ResearchDef";

                DefCache.GetDef<ResearchDef>("PX_Alien_Acidworm_ResearchDef").ViewElementDef.BenefitsText.LocalizationKey = "PX_ALIEN_ACIDWORM_RESEARCHDEF_BENEFITS";



            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }





        internal static void ChangeModulePictures()
        {
            try
            {


                TacticalItemDef nightVisionModule = DefCache.GetDef<TacticalItemDef>("SY_MultiVisualSensor_Attachment_ItemDef");
                nightVisionModule.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile("modules_nightvision.png");
                nightVisionModule.ViewElementDef.InventoryIcon = nightVisionModule.ViewElementDef.LargeIcon;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }



        }



        internal static void CreateAcidImmunity()
        {
            try
            {
                string abilityName = "AcidImmunityAbility";
                string gUID = "{4915CA1F-5DA2-4F7D-9455-BC775EA1D8CB}";
                // string characterProgressionGUID = "AA24A50E-C61A-4CD8-97FE-3F8BAC5F7BAA";
                string viewElementGUID = "{85B86FF6-3EB4-492A-9775-D01611DEDE5B}";

                DamageMultiplierAbilityDef source = DefCache.GetDef<DamageMultiplierAbilityDef>("AcidResistant_DamageMultiplierAbilityDef");
                DamageMultiplierAbilityDef newAbility = Helper.CreateDefFromClone(
                    source,
                   gUID,
                    abilityName);

                /*newAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    characterProgressionGUID,
                   abilityName + "CharacterProgression");*/
                newAbility.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    viewElementGUID,
                    abilityName + "ViewElement");
                newAbility.ViewElementDef.DisplayName1.LocalizationKey = "ACID_IMMUNITY_NAME";
                newAbility.ViewElementDef.Description.LocalizationKey = "ACID_IMMUNITY_DESCRIPTION";
                newAbility.ViewElementDef.ShowInStatusScreen = true;
                newAbility.ViewElementDef.ShowInFreeAimMode = true;

                newAbility.Multiplier = 0.0f;



            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        internal static void FixPriestScream()
        {
            try
            {
                AIAbilityNumberOfTargetsConsiderationDef numberOfTargetsConsiderationDefSource = DefCache.GetDef<AIAbilityNumberOfTargetsConsiderationDef>("Siren_PsychicScreamNumberOfTargets_AIConsiderationDef");

                AIAbilityNumberOfTargetsConsiderationDef newNumberOfTargetsConsideration = Helper.CreateDefFromClone(numberOfTargetsConsiderationDefSource, "{EBF0A605-B3DA-45C8-88CD-8CB9832B584E}", "PsychicScreamNumberOfTargets_AIConsiderationDef");
                AIActionMoveAndExecuteAbilityDef priestMoveAndScreamAIAction = DefCache.GetDef<AIActionMoveAndExecuteAbilityDef>("MoveAndDoPsychicScream_AIActionDef");
                priestMoveAndScreamAIAction.Evaluations[0].Considerations[1].Consideration = newNumberOfTargetsConsideration;
                newNumberOfTargetsConsideration.Ability = priestMoveAndScreamAIAction.AbilityToExecute;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        //Maybe will be used later

        internal static void CreateRoboticSelfRestoreAbility()
        {
            try
            {
                CreateRoboticHealingStatus();
                CreateRoboticHealingAbility();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void CreateRoboticHealingAbility()
        {
            try
            {

                string abilityGUID = "{5056F0F1-0FDE-4C5B-B69D-A436310CC72E}";

                string abilityName = "RoboticSelfRepair_AbilityDef";
                PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("SelfDefenseSpecialist_AbilityDef");
                PassiveModifierAbilityDef ambushAbility = Helper.CreateDefFromClone(
                    source,
                   abilityGUID,
                    abilityName);
                ambushAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "{76838266-6249-46AF-A541-66065F102BD5}",
                    abilityName);
                ambushAbility.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "{C464B230-D5D9-4798-A765-CF2398B3A49C}",
                    abilityName);
                ambushAbility.StatModifications = new ItemStatModification[] { };
                ambushAbility.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                ambushAbility.ViewElementDef.DisplayName1.LocalizationKey = "ROBOTIC_SELF_REPAIR_TITLE";
                ambushAbility.ViewElementDef.Description.LocalizationKey = "ROBOTIC_SELF_REPAIR_ABILITY_TEXT";

                Sprite icon = Helper.CreateSpriteFromImageFile("TFTV_status_self_repair.png");
                ambushAbility.ViewElementDef.LargeIcon = icon;
                ambushAbility.ViewElementDef.SmallIcon = icon;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void CreateRoboticHealingStatus()
        {
            try
            {
                //Creating status effect to show that Guardian will repair a body part next turn. Need to create a status to show small icon.

                DamageMultiplierStatusDef sourceAbilityStatusDef = DefCache.GetDef<DamageMultiplierStatusDef>("BionicResistances_StatusDef");

                string statusSelfRepairAbilityName = "RoboticSelfRepair_AddAbilityStatusDef";
                DamageMultiplierStatusDef statusSelfRepairAbilityDef = Helper.CreateDefFromClone(sourceAbilityStatusDef, "609D0304-8BA3-4103-BC0D-6BE440E69F3D", statusSelfRepairAbilityName);
                statusSelfRepairAbilityDef.EffectName = "SelfRoboticRepair";
                statusSelfRepairAbilityDef.ApplicationConditions = new EffectConditionDef[] { };
                statusSelfRepairAbilityDef.Visuals = Helper.CreateDefFromClone(sourceAbilityStatusDef.Visuals, "36414ABA-B535-4C4C-AADD-2F3A64D5101C", statusSelfRepairAbilityName);
                statusSelfRepairAbilityDef.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.AlwaysVisible;
                statusSelfRepairAbilityDef.VisibleOnPassiveBar = true;
                statusSelfRepairAbilityDef.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;
                statusSelfRepairAbilityDef.Visuals.DisplayName1.LocalizationKey = "ROBOTIC_SELF_REPAIR_TITLE";
                statusSelfRepairAbilityDef.Visuals.Description.LocalizationKey = "ROBOTIC_SELF_REPAIR_TEXT_TEXT";
                statusSelfRepairAbilityDef.Visuals.LargeIcon = Helper.CreateSpriteFromImageFile("TFTV_status_self_repair.png");
                statusSelfRepairAbilityDef.Visuals.SmallIcon = Helper.CreateSpriteFromImageFile("TFTV_status_self_repair.png");
                statusSelfRepairAbilityDef.DamageTypeDefs = new DamageTypeBaseEffectDef[] { };
                statusSelfRepairAbilityDef.Multiplier = 1;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void AddMissingElectronicTags()
        {
            try
            {
                ItemMaterialTagDef electronic = DefCache.GetDef<ItemMaterialTagDef>("Electronic_ItemMaterialTagDef");

                TacticalItemDef juggHead = DefCache.GetDef<TacticalItemDef>("NJ_Jugg_BIO_Helmet_BodyPartDef");

                foreach (TacticalItemDef tacticalItemDef in Repo.GetAllDefs<TacticalItemDef>().Where(ti => ti.Tags.Contains(Shared.SharedGameTags.BionicalTag)))
                {
                    if (tacticalItemDef.Tags.CanAdd(electronic))
                    {
                        tacticalItemDef.Tags.Add(electronic);
                        // TFTVLogger.Always($"added electronic tag to {tacticalItemDef.name}");

                    }

                }

                foreach (GroundVehicleWeaponDef groundVehicleWeaponDef in Repo.GetAllDefs<GroundVehicleWeaponDef>())
                {
                    if (groundVehicleWeaponDef.Tags.CanAdd(electronic))
                    {
                        groundVehicleWeaponDef.Tags.Add(electronic);
                        //   TFTVLogger.Always($"added electronic tag to {groundVehicleWeaponDef.name}");

                    }


                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        internal static void ModifyDecoyAbility()
        {
            try
            {
                SpawnActorAbilityDef decoyAbility = DefCache.GetDef<SpawnActorAbilityDef>("Decoy_AbilityDef");
                decoyAbility.UseSelfAsTemplate = false;

                TacCharacterDef dcoyTacCharacter = DefCache.GetDef<TacCharacterDef>("SY_Decoy_TacCharacterDef");

                ClassTagDef assaultClassTag = DefCache.GetDef<ClassTagDef>("Assault_ClassTagDef");
                ActorDeploymentTagDef deploymentTagDef = DefCache.GetDef<ActorDeploymentTagDef>("1x1_Grunt_DeploymentTagDef");

                WeaponDef ares = DefCache.GetDef<WeaponDef>("PX_AssaultRifle_WeaponDef");
                dcoyTacCharacter.Data.EquipmentItems = new ItemDef[] { ares };

                GameTagDef decoyTag = TFTVCommonMethods.CreateNewTag("DecoyTag", "{55D78B77-AE12-452B-B3FB-BB559DDBF8AE}");

                TacticalActorDef dcoy = DefCache.GetDef<TacticalActorDef>("Decoy_ActorDef");
                dcoy.EnduranceToHealthMultiplier = 20;

                List<GameTagDef> gameTagDefs = new List<GameTagDef>(dcoy.GameTags) { assaultClassTag, deploymentTagDef, decoyTag };

                dcoyTacCharacter.Data.GameTags = gameTagDefs.ToArray();
                //  OnActorDazedEffectStatus.ShouldApplyEffect

                TacticalNavigationComponentDef navigationSource = DefCache.GetDef<TacticalNavigationComponentDef>("Soldier_NavigationDef");
                //  navigationSource.CreateNavObstacle = false;

                TacticalNavigationComponentDef newNavigation = Helper.CreateDefFromClone(navigationSource, "{AAED2DCB-6269-42D0-ADCF-474576B16258}", "DecoyNavigationComponentDef");
                newNavigation.CreateNavObstacle = false;

                ComponentSetDef componentSetDef = DefCache.GetDef<ComponentSetDef>("Decoy_Template_ComponentSetDef");
                componentSetDef.Components[4] = newNavigation;

                RagdollDieAbilityDef dieAbilityDef = DefCache.GetDef<RagdollDieAbilityDef>("Decoy_Die_AbilityDef");
                dieAbilityDef.EventOnActivate = new TacticalEventDef();

                string hintDecoyPlacedName = "HintDecoyPlaced";
                string hintDecoyPlacedGUID = "{E86C3A8A-B3E3-4A52-9BEB-1FFFE1506F60}";
                string hintDecoyPlacedTitle = "HINT_DECOYPLACED_TITLE";
                string hintDecoyPlacedText = "HINT_DECOYPLACED_TEXT";

                TFTVTutorialAndStory.CreateNewTacticalHint(hintDecoyPlacedName, HintTrigger.AbilityExecuted, decoyAbility.name, hintDecoyPlacedTitle, hintDecoyPlacedText, 4, true, hintDecoyPlacedGUID);

                IsDefHintConditionDef conditionDef = DefCache.GetDef<IsDefHintConditionDef>(decoyAbility.name + "_HintConditionDef");
                conditionDef.TargetDef = decoyAbility;

                string hintDecoyDiscoveredName = "HintDecoyDiscovered";
                string hintDecoyDiscoveredGUID = "{D75AC0EA-89C1-4DF7-8E67-CFD83F8F6ED1}";
                string hintDecoyDiscoveredTitle = "HINT_DECOYDISCOVERED_TITLE";
                string hintDecoyDiscoveredText = "HINT_DECOYDISCOVERED_TEXT";
                TFTVTutorialAndStory.CreateNewTacticalHint(hintDecoyDiscoveredName, HintTrigger.Manual, decoyTag.name, hintDecoyDiscoveredTitle, hintDecoyDiscoveredText, 1, true, hintDecoyDiscoveredGUID);



                string hintDecoyScyllaName = "HintDecoyScylla";
                string hintDecoyScyllaGUID = "{06D96E1B-758C-4178-9D9B-13A40686E90F}";
                string hintDecoyScyllaTitle = "HINT_DECOYSCYLLA_TITLE";
                string hintDecoyScyllaText = "HINT_DECOYSCYLLA_TEXT";
                TFTVTutorialAndStory.CreateNewTacticalHint(hintDecoyScyllaName, HintTrigger.ActorDied, dcoyTacCharacter.name, hintDecoyScyllaTitle, hintDecoyScyllaText, 0, true, hintDecoyScyllaGUID);

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        internal static void CreateConsolePromptBaseDefense()
        {
            try
            {
                string gUID = "{444AE91B-2FA4-4296-914A-72F0310D8D46}";
                string name = "TFTVBaseDefensePrompt";
                TacticalPromptDef source = DefCache.GetDef<TacticalPromptDef>("ActivateObjectivePromptDef");
                TacticalPromptDef newPrompt = Helper.CreateDefFromClone(source, gUID, name);

                newPrompt.PromptText.LocalizationKey = "BASEDEFENSE_VENTING_PROMPT";
                newPrompt.PromptIcon = Base.UI.MessageBox.MessageBoxIcon.Warning;


            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }



        internal static void ImproveScyllaAcheronsChironsAndCyclops()
        {
            try
            {
                ModifyScyllaAIAndHeads();
                MedAndBigMonstersSquishers();
                ModifyGuardianAIandStomp();
                CreateNewScreamForCyclops();
                //  MakeUmbraNotObstacle();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }



        internal static void CreateNewScreamForCyclops()
        {
            try
            {
                string abilityName = "CyclopsScream";


                ApplyDamageEffectAbilityDef guardianStomp = DefCache.GetDef<ApplyDamageEffectAbilityDef>("Guardian_Stomp_AbilityDef");
                PsychicScreamAbilityDef screamAbilitySource = DefCache.GetDef<PsychicScreamAbilityDef>("Siren_PsychicScream_AbilityDef");
                //blast damage is limited by scenery 
                PsychicScreamAbilityDef newScreamAbility = Helper.CreateDefFromClone(screamAbilitySource, "{7CD25D07-441C-4E57-8680-FB7B06E9DDE5}", abilityName);
                newScreamAbility.ViewElementDef = Helper.CreateDefFromClone(screamAbilitySource.ViewElementDef, "{86253E40-9034-4089-A71E-1C1D78B28ECE}", abilityName);


                newScreamAbility.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile("ANC_CyclopsScream.png");
                newScreamAbility.ViewElementDef.SmallIcon = Helper.CreateSpriteFromImageFile("ANC_CyclopsScream.png");
                newScreamAbility.ViewElementDef.DisplayName1.LocalizationKey = "CYCLOPS_SCREAM_TITLE";
                newScreamAbility.ViewElementDef.Description.LocalizationKey = "CYCLOPS_SCREAM_DESCRIPTION";

                //Only to show in the UI
                newScreamAbility.DamagePayload.DamageKeywords = new List<DamageKeywordPair>
                {
                    new DamageKeywordPair { DamageKeywordDef = Shared.SharedDamageKeywords.PsychicKeyword, Value = 1 },
                };


                AIAbilityNumberOfTargetsConsiderationDef numberOfTargetsConsiderationDefSource = DefCache.GetDef<AIAbilityNumberOfTargetsConsiderationDef>("Siren_PsychicScreamNumberOfTargets_AIConsiderationDef");

                AIAbilityNumberOfTargetsConsiderationDef newNumberOfTargetsConsideration = Helper.CreateDefFromClone(numberOfTargetsConsiderationDefSource, "{5C5D22BC-0E48-4697-9525-0AA7BBE0D06B}", abilityName);
                newNumberOfTargetsConsideration.Ability = newScreamAbility;

                TacticalActorDef cyclops = DefCache.GetDef<TacticalActorDef>("MediumGuardian_ActorDef");
                List<AbilityDef> cyclopsAbilities = new List<AbilityDef>(cyclops.Abilities.ToList());
                cyclopsAbilities.Remove(guardianStomp);
                cyclopsAbilities.Add(newScreamAbility);
                cyclops.Abilities = cyclopsAbilities.ToArray();

                newScreamAbility.ActionPointCost = 0.5f;
                newScreamAbility.WillPointCost = 0.0f;
                newScreamAbility.UsesPerTurn = 1;



                DefCache.GetDef<TacActorSimpleAbilityAnimActionDef>("E_Stomp [MediumGuardian_AnimActionsDef]").AbilityDefs = new AbilityDef[] { newScreamAbility };

                AIActionMoveAndExecuteAbilityDef moveAndStompAIAction = DefCache.GetDef<AIActionMoveAndExecuteAbilityDef>("MediumGuardian_MoveAndDoStomp_AIActionDef");
                moveAndStompAIAction.AbilityToExecute = newScreamAbility;
                moveAndStompAIAction.Evaluations[0].Considerations[0].Consideration = newNumberOfTargetsConsideration;
                moveAndStompAIAction.Weight = 20.0f;

                DefCache.GetDef<AIAttackPositionConsiderationDef>("MediumGuardian_StompAttackPosition_AIConsiderationDef").AbilityDef = newScreamAbility;

                DefCache.GetDef<AIAbilityDisabledStateConsiderationDef>("MediumGuardian_StompAbilityEnabled_AIConsiderationDef").Ability = newScreamAbility;


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }


        internal static void ModifyGuardianAIandStomp()
        {
            try
            {

                AIActorMovementZoneTargetGeneratorDef movementZoneTargetGeneratorDef = DefCache.GetDef<AIActorMovementZoneTargetGeneratorDef>("MediumGuardian_MovementZone_AITargetGeneratorDef");
                AIActorMovementZoneTargetGeneratorDef halfZone = DefCache.GetDef<AIActorMovementZoneTargetGeneratorDef>("MediumGuardian_ActionZoneHalf_AITargetGeneratorDef");
                AIActorMovementZoneTargetGeneratorDef actionZone = DefCache.GetDef<AIActorMovementZoneTargetGeneratorDef>("MediumGuardian_ActionZone_AITargetGeneratorDef");


                //   DefCache.GetDef<ApplyDamageEffectAbilityDef>("Guardian_Stomp_AbilityDef").IgnoreFriendlies = true;
                AIActionsTemplateDef mediumGuardianAITemplate = DefCache.GetDef<AIActionsTemplateDef>("MediumGuardian_AIActionsTemplateDef");

                AIActionDef queenAdvance = DefCache.GetDef<AIActionDef>("Queen_Advance_AIActionDef");
                AIActionDef guardianAdvance = Helper.CreateDefFromClone(queenAdvance, "{BC0497A4-ED7A-427C-910F-35B453B5F205}", "Guardian_Advance_AIActionDef");
                guardianAdvance.Weight = 1.0f;


                guardianAdvance.Evaluations[0].TargetGeneratorDef = DefCache.GetDef<AIActorMovementZoneTargetGeneratorDef>("MediumGuardian_MovementZone_AITargetGeneratorDef");

                List<AIActionDef> aIActions = new List<AIActionDef>(mediumGuardianAITemplate.ActionDefs.ToList())
                {
                    guardianAdvance
                };
                mediumGuardianAITemplate.ActionDefs = aIActions.ToArray();

                TacAIActorDef cyclopsAIActor = DefCache.GetDef<TacAIActorDef>("MediumGuardian_AIActorDef");
                cyclopsAIActor.TurnOrderPriority = 1000;

                AIActionDef aggresiveAdvance = DefCache.GetDef<AIActionDef>("MediumGuardian_Advance_Aggressive_AIActionDef");



                AIActionMoveAndAttackDef moveAndShootAIActionDef = DefCache.GetDef<AIActionMoveAndAttackDef>("MediumGuardian_MoveAndShoot_AIActionDef");


                moveAndShootAIActionDef.Evaluations[1].TargetGeneratorDef = actionZone;
                moveAndShootAIActionDef.Weight = 100.0f;
                aggresiveAdvance.Weight = 2.0f;


                CaterpillarMoveAbilityDef caterPillarMoveDef = DefCache.GetDef<CaterpillarMoveAbilityDef>("ScyllaSquisher");

                movementZoneTargetGeneratorDef.MoveAbilityDef = caterPillarMoveDef;
                halfZone.MoveAbilityDef = caterPillarMoveDef;
                actionZone.MoveAbilityDef = caterPillarMoveDef;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        internal static void ModifyScyllaAIAndHeads()
        {
            try
            {
                WeaponDef headSpitter = DefCache.GetDef<WeaponDef>("Queen_Head_Spitter_Goo_WeaponDef");
                // DamageKeywordDef blast = DefCache.GetDef<DamageKeywordDef>("Blast_DamageKeywordDataDef");
                StandardDamageTypeEffectDef blastDamage = DefCache.GetDef<StandardDamageTypeEffectDef>("Blast_StandardDamageTypeEffectDef");

                //switch to blast damage from goo damage
                headSpitter.DamagePayload.DamageType = blastDamage;
                //increase blast annd poison damage to 40 from 30
                headSpitter.DamagePayload.DamageKeywords[0].Value = 40;
                headSpitter.DamagePayload.DamageKeywords[2].Value = 40;
                //shouldn't make a difference
                headSpitter.DamagePayload.AoeRadius = 2f;

                //Reduce Move and SpitGoo/SonicBlast weight, so she also uses Smashers sometimes
                // DefCache.GetDef<AIActionDef>("Queen_MoveAndSpitGoo_AIActionDef").Weight = 50.0f;
                //  DefCache.GetDef<AIActionDef>("Queen_MoveAndSonicBlast_AIActionDef").Weight = 50.0f;
                DefCache.GetDef<AIActionDef>("Queen_MoveAndPrepareShooting_AIActionDef").Weight = 50.0f;


                //Reduce range of Sonic and Spitter Heads from 20 to 15 so that cannons are more effective
                WeaponDef headSonic = DefCache.GetDef<WeaponDef>("Queen_Head_Sonic_WeaponDef");
                headSpitter.DamagePayload.Range = 15;
                headSonic.DamagePayload.Range = 15;

                DefCache.GetDef<AIActionDef>("Queen_Recover_AIActionDef").Weight = 0.01f;


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void MedAndBigMonstersSquishers()
        {
            try
            {
                //Create new Caterpillar ability to (eventually) show different View elements (for now just hidden) 
                string abilityName = "ScyllaSquisher";
                string abilityGUID = "{B7EBE715-69CE-4163-8E7D-88034ED4DE2A}";
                string viewElementGUID = "{C74C16D0-98DB-4717-B5E8-D04004151A69}";
                CaterpillarMoveAbilityDef source = DefCache.GetDef<CaterpillarMoveAbilityDef>("CaterpillarMoveAbilityDef");
                CaterpillarMoveAbilityDef scyllaCaterpillarAbility = Helper.CreateDefFromClone(source, abilityGUID, abilityName);
                scyllaCaterpillarAbility.ViewElementDef = Helper.CreateDefFromClone(source.ViewElementDef, viewElementGUID, abilityName);
                scyllaCaterpillarAbility.ViewElementDef.ShowInStatusScreen = false;

                //Make all small critters and things not an obstacle for Scylla, MedMonster (Chiron, Cyclops), Acheron movement

                TacticalNavigationComponentDef spiderDroneNav = DefCache.GetDef<TacticalNavigationComponentDef>("SpiderDrone_NavigationDef");
                spiderDroneNav.ignoreObstacleFor = new string[] { "BigMonster", "MedMonster", "ArmadilloWormsDestroyer", "MedMonsterHigh" };

                TacticalNavigationComponentDef wormNav = DefCache.GetDef<TacticalNavigationComponentDef>("Fireworm_NavigationDef");
                wormNav.ignoreObstacleFor = new string[] { "BigMonster", "MedMonster", "ArmadilloWormsDestroyer", "MedMonsterHigh" };

                TacticalNavigationComponentDef faceHuggerNav = DefCache.GetDef<TacticalNavigationComponentDef>("Facehugger_NavigationDef");
                faceHuggerNav.ignoreObstacleFor = new string[] { "BigMonster", "MedMonster", "ArmadilloWormsDestroyer", "MedMonsterHigh" };

                TacticalNavigationComponentDef swarmerNav = DefCache.GetDef<TacticalNavigationComponentDef>("Swarmer_NavigationDef");
                swarmerNav.ignoreObstacleFor = new string[] { "BigMonster", "MedMonster", "ArmadilloWormsDestroyer", "MedMonsterHigh" };

                TacticalNavigationComponentDef turret1 = DefCache.GetDef<TacticalNavigationComponentDef>("NJ_TechTurret_NavigationDef");
                turret1.ignoreObstacleFor = new string[] { "BigMonster", "MedMonster", "ArmadilloWormsDestroyer", "MedMonsterHigh" };

                TacticalNavigationComponentDef turret2 = DefCache.GetDef<TacticalNavigationComponentDef>("NJ_PRCRTechTurret_NavigationDef");
                turret2.ignoreObstacleFor = new string[] { "BigMonster", "MedMonster", "ArmadilloWormsDestroyer", "MedMonsterHigh" };

                TacticalNavigationComponentDef turret3 = DefCache.GetDef<TacticalNavigationComponentDef>("PX_LaserTechTurret_NavigationDef");
                turret3.ignoreObstacleFor = new string[] { "BigMonster", "MedMonster", "ArmadilloWormsDestroyer", "MedMonsterHigh" };


                TacticalAbilityDef fireImmunity = DefCache.GetDef<TacticalAbilityDef>("FireImmunity_DamageMultiplierAbilityDef");
                TacticalAbilityDef poisonImmunity = DefCache.GetDef<TacticalAbilityDef>("PoisonImmunity_DamageMultiplierAbilityDef");
                TacticalAbilityDef acidImmunity = DefCache.GetDef<TacticalAbilityDef>("AcidImmunityAbility");

                //Scylla and Chirons with Heavy Legs, as well as all Cyclops get caterpillar ability + fire immunity
                foreach (TacCharacterDef tacCharacterDef in Repo.GetAllDefs<TacCharacterDef>())
                {
                    //  TFTVLogger.Always($"{tacCharacterDef.name}");
                    List<TacticalAbilityDef> monsterAbilities = new List<TacticalAbilityDef>(tacCharacterDef.Data.Abilites.ToList());

                    if (
                        (tacCharacterDef.name.Contains("Scylla")
                        && tacCharacterDef.Data.BodypartItems.Contains(DefCache.GetDef<TacticalItemDef>("Queen_Legs_Heavy_ItemDef")))
                        || (tacCharacterDef.name.Contains("Chiron")
                        && !tacCharacterDef.Data.BodypartItems.Contains(DefCache.GetDef<TacticalItemDef>("Chiron_Legs_Agile_ItemDef")))
                        || tacCharacterDef.name.StartsWith("MediumGuardian")

                        )
                    {
                        monsterAbilities.Add(scyllaCaterpillarAbility);
                        monsterAbilities.Add(fireImmunity);
                    }

                    if (tacCharacterDef.Data.BodypartItems.Contains(DefCache.GetDef<WeaponDef>("Chiron_Abdomen_FireWorm_Launcher_WeaponDef")))
                    {


                        if (!monsterAbilities.Contains(fireImmunity))
                        {
                            monsterAbilities.Add(fireImmunity);
                        }

                    }
                    else if (tacCharacterDef.Data.BodypartItems.Contains(DefCache.GetDef<WeaponDef>("Chiron_Abdomen_PoisonWorm_Launcher_WeaponDef")))
                    {
                        monsterAbilities.Add(poisonImmunity);

                    }
                    else if (tacCharacterDef.Data.BodypartItems.Contains(DefCache.GetDef<WeaponDef>("Chiron_Abdomen_AcidWorm_Launcher_WeaponDef")))
                    {
                        monsterAbilities.Add(acidImmunity);

                    }


                    tacCharacterDef.Data.Abilites = monsterAbilities.ToArray();



                }

                //ensure that small critters and things have the damagedByCaterpillarTracks_Tag; if check in case BetterEnemies is active
                GameTagDef damagedByCaterpillar = DefCache.GetDef<GameTagDef>("DamageByCaterpillarTracks_TagDef");

                foreach (TacticalActorDef actor in Repo.GetAllDefs<TacticalActorDef>().Where(a => a.name.Contains("worm") || a.name.Contains("SpiderDrone") || a.name.Contains("TechTurret")))
                {
                    if (!actor.GameTags.Contains(damagedByCaterpillar))
                    {
                        actor.GameTags.Add(damagedByCaterpillar);
                    }
                }

                TacticalDemolitionComponentDef demoCyclops = DefCache.GetDef<TacticalDemolitionComponentDef>("MediumGuardian_DemolitionComponentDef");

                //increase size of demo rectangle to squash worms and stuff
                demoCyclops.RectangleSize = new Vector3
                {
                    x = 2.5f,
                    y = 2.6f,
                    z = 2.9f,
                };

                TacticalDemolitionComponentDef demoChiron = DefCache.GetDef<TacticalDemolitionComponentDef>("Chiron_DemolitionComponentDef");

                demoChiron.SphereCenter = new Vector3(0f, 0f, 0f);
                demoChiron.CapsuleStart = new Vector3(0f, 1f, 0f);
                demoChiron.CapsuleEnd = new Vector3(0f, 3f, 0f);




                //improve special Scylla cheat skill to avoid getting damaged from squishing things that blow up

                DamageMultiplierStatusDef scyllaImmunitySource = DefCache.GetDef<DamageMultiplierStatusDef>("E_BlastImmunityStatus [Queen_GunsFire_ShootAbilityDef]");
                DamageMultiplierStatusDef newScyllaImmunityStatus = Helper.CreateDefFromClone(scyllaImmunitySource, "{D4CF7113-AF7D-42CA-BBF6-2CB06B8DB31E}", abilityName);

                TacStatusEffectDef scyllaImmunityEffect = DefCache.GetDef<TacStatusEffectDef>("E_MakeImmuneToBlastDamageEffect [Queen_GunsFire_ShootAbilityDef]");
                TacStatusEffectDef newScyllaImmunityEffect = Helper.CreateDefFromClone(scyllaImmunityEffect, "{6FDC9695-F561-446E-9D8F-D9AF29A35F0F}", abilityName);

                newScyllaImmunityEffect.StatusDef = newScyllaImmunityStatus;


                // not good, makes them immune to fire...
                /*   StandardDamageTypeEffectDef fireDamage = DefCache.GetDef<StandardDamageTypeEffectDef>("Fire_StandardDamageTypeEffectDef"); 
                   AcidDamageTypeEffectDef acidDamage = DefCache.GetDef<AcidDamageTypeEffectDef>("Acid_DamageOverTimeDamageTypeEffectDef");

                   DamageMultiplierStatusDef scyllaCheatSkill = DefCache.GetDef<DamageMultiplierStatusDef>("E_BlastImmunityStatus [Queen_GunsFire_ShootAbilityDef]");

                   List<DamageTypeBaseEffectDef> damageTypeBaseEffectDefs = new List<DamageTypeBaseEffectDef>(scyllaCheatSkill.DamageTypeDefs) { fireDamage, acidDamage };
                   scyllaCheatSkill.DamageTypeDefs = damageTypeBaseEffectDefs.ToArray();*/




            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }


        internal static void CreateHintsForBaseDefense()
        {
            try
            {
                MissionTypeTagDef baseDefenseMissionTag = DefCache.GetDef<MissionTypeTagDef>("MissionTypePhoenixBaseDefence_MissionTagDef");

                TFTVTutorialAndStory.CreateNewManualTacticalHint("BaseDefenseUmbraStrat", "{B6B31D16-82B6-462D-A220-388447F2C9D8}", "BASEDEFENSE_UMBRASTRAT_TITLE", "BASEDEFENSE_UMBRASTRAT_TEXT");
                TFTVTutorialAndStory.CreateNewManualTacticalHint("BaseDefenseWormsStrat", "{1CA6F9FB-BD41-430B-A4BF-04867245BEBF}", "BASEDEFENSE_WORMSSTRAT_TITLE", "BASEDEFENSE_WORMSSTRAT_TEXT");
                TFTVTutorialAndStory.CreateNewManualTacticalHint("BaseDefenseForce2Strat", "{22DF1F91-2D1A-4F34-AD9A-E9881E60CCD5}", "BASEDEFENSE_FORCE2STRAT_TITLE", "BASEDEFENSE_FORCE2STRAT_TEXT");
                TFTVTutorialAndStory.CreateNewTacticalHint("TFTVBaseDefense", HintTrigger.MissionStart, baseDefenseMissionTag.name, "BASEDEFENSE_TACTICAL_ADVANTAGE_TITLE", "BASEDEFENSE_TACTICAL_ADVANTAGE_DESCRIPTION", 3, false, "{DB7CF4DE-D59F-4990-90AE-4C0B43550468}");
                /*TFTVTutorialAndStory.CreateNewManualTacticalHint("BaseDefenseInfestation", "{34A92A89-DECF-45F2-82E5-52E1D721A1B3}", "BASEDEFENSE_INFESTATION_TITLE", "BASEDEFENSE_INFESTATION_DESCRIPTION");
                TFTVTutorialAndStory.CreateNewManualTacticalHint("BaseDefenseNesting", "{34A92A89-DECF-45F2-82E5-52E1D721A1B3}", "BASEDEFENSE_NESTING_TITLE ", "BASEDEFENSE_NESTING_DESCRIPTION");
                TFTVTutorialAndStory.CreateNewManualTacticalHint("BaseDefenseTacticalAdvantage", "{34A92A89-DECF-45F2-82E5-52E1D721A1B3}", "BASEDEFENSE_TACTICAL_ADVANTAGE_TITLE", "BASEDEFENSE_TACTICAL_ADVANTAGE_DESCRIPTION"); */
                ContextHelpHintDef baseDefenseStartHint = DefCache.GetDef<ContextHelpHintDef>("TFTVBaseDefense");
                baseDefenseStartHint.AnyCondition = true;

                TFTVTutorialAndStory.CreateNewManualTacticalHint("BaseDefenseVenting", "{AE6CE201-816F-4363-A80E-5CD07D8263CF}", "BASEDEFENSE_VENTING_TITLE", "BASEDEFENSE_VENTING_TEXT");



            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }


        internal static void CreateFireExplosion()
        {
            try
            {
                SpawnTacticalVoxelEffectDef spawnFire = Helper.CreateDefFromClone<SpawnTacticalVoxelEffectDef>(null, "{96C92F1C-CA61-4FB3-8147-809ED0E70108}", "FireVoxelSpawnerEffect");
                spawnFire.ApplicationConditions = new EffectConditionDef[] { };
                spawnFire.SpawnDelay = 2f;
                spawnFire.Radius = 2;
                spawnFire.TacticalVoxelType = PhoenixPoint.Tactical.Levels.Mist.TacticalVoxelType.Fire;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        internal static void CreateCosmeticExplosion()
        {
            try
            {
                string name = "FakeExplosion_ExplosionEffectDef";
                string gUIDDelayedEffect = "{82F49470-B14D-4C73-8B91-9D3EEE7CCB44}";
                DelayedEffectDef sourceDelayedEffect = DefCache.GetDef<DelayedEffectDef>("ExplodingBarrel_ExplosionEffectDef");
                DelayedEffectDef newDelayedEffect = Helper.CreateDefFromClone(sourceDelayedEffect, gUIDDelayedEffect, name);
                newDelayedEffect.SecondsDelay = 0.2f;

                string gUIDExplosionEffect = "{8054419B-6410-47A4-8BD5-C2CC5A4B8B62}";
                ExplosionEffectDef sourceExplosionEffect = DefCache.GetDef<ExplosionEffectDef>("E_ShrapnelExplosion [ExplodingBarrel_ExplosionEffectDef]");
                ExplosionEffectDef newExplosionEffect = Helper.CreateDefFromClone(sourceExplosionEffect, gUIDExplosionEffect, name);


                //  SpawnVoxelDamageTypeEffectDef mistDamage = DefCache.GetDef<SpawnVoxelDamageTypeEffectDef>("Goo_SpawnVoxelDamageTypeEffectDef");

                string gUIDDamageEffect = "{CD3D8BC8-C90D-40A6-BBA3-0FD7FE629F15}";
                DamageEffectDef sourceDamageEffect = DefCache.GetDef<DamageEffectDef>("E_DamageEffect [ExplodingBarrel_ExplosionEffectDef]");
                DamageEffectDef newDamageEffect = Helper.CreateDefFromClone(sourceDamageEffect, gUIDDamageEffect, name);
                newDamageEffect.MinimumDamage = 1;
                newDamageEffect.MaximumDamage = 1;
                newDamageEffect.ObjectMultiplier = 100000f;
                newDamageEffect.ArmourShred = 0;
                newDamageEffect.ArmourShredProbabilityPerc = 0;
                //  newDamageEffect.DamageTypeDef = mistDamage;
                newExplosionEffect.DamageEffect = newDamageEffect;
                newDelayedEffect.EffectDef = newExplosionEffect;

                newDelayedEffect.SecondsDelay = 0.5f;


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        public static void CreateBaseDefenseEvents()
        {
            try
            {
                GeoscapeEventDef baseDefense = TFTVCommonMethods.CreateNewEvent("OlenaBaseDefense", "BASEDEFENSE_EVENT_TITLE", "BASEDEFENSE_EVENT_TEXT", null);
                baseDefense.GeoscapeEventData.Flavour = "DLC4_C1_S2";

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }


        internal static void CreateNewBaseDefense()
        {
            try
            {
                ChangeBaseDefense();
                CreateObjectivesBaseDefense();
                CreateBaseDefenseEvents();
                CreateCosmeticExplosion();
                CreateFireExplosion();
                CreateHintsForBaseDefense();
                //  CreateSpawnCrabmanAbility();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void CreateSpawnCrabmanAbility()
        {
            try
            {


                string abilityName = "SpawnerySpawnAbility";
                string abilityGUID = "{7FBEB256-1F44-4F3A-8B3A-17A8A8AF9F8F}";

                SpawnActorAbilityDef source = DefCache.GetDef<SpawnActorAbilityDef>("Queen_SpawnFacehugger_AbilityDef");
                SpawnActorAbilityDef newSpawnerySpawnAbility = Helper.CreateDefFromClone(source, abilityGUID, abilityName);

                newSpawnerySpawnAbility.WillPointCost = 0;
                newSpawnerySpawnAbility.AnimType = -1;
                newSpawnerySpawnAbility.EndsTurn = true;

                TacCharacterDef crabman = DefCache.GetDef<TacCharacterDef>("Crabman3_AdvancedCharger_AlienMutationVariationDef");
                newSpawnerySpawnAbility.TacCharacterDef = crabman;
                ComponentSetDef crabmanComponent = DefCache.GetDef<ComponentSetDef>("Crabman_Template_ComponentSetDef");
                newSpawnerySpawnAbility.ActorComponentSetDef = crabmanComponent;
                newSpawnerySpawnAbility.PlaySpawningActorAnimation = true;
                newSpawnerySpawnAbility.FacePosition = false;
                newSpawnerySpawnAbility.OverrideDefaultActionAnimation = false;
                newSpawnerySpawnAbility.WaitsForActionEnd = false;

                TacCharacterDef spawnery = DefCache.GetDef<TacCharacterDef>("SpawningPoolCrabman_AlienMutationVariationDef");
                spawnery.Data.Abilites = new TacticalAbilityDef[] { newSpawnerySpawnAbility };

                AIActionsTemplateDef spawneryAIActionsTemplate = DefCache.GetDef<AIActionsTemplateDef>("SpawningPool_AIActionsTemplateDef");

                string aIActionName = "SpawnerySpawnAIAction";
                string aIActionGUID = "{0598ABF5-6ECF-4AB8-BF3F-DF15636B633A}";
                AIActionExecuteAbilityDef sourceExecuteAbilityAction = DefCache.GetDef<AIActionExecuteAbilityDef>("Queen_SpawnFacehugger_AIActionDef");
                AIActionExecuteAbilityDef spawneryAIAction = Helper.CreateDefFromClone(sourceExecuteAbilityAction, aIActionGUID, aIActionName);


                string aIConsiderationGUID = "{41BE5653-27D3-456D-A76C-E54F8744DAF7}";
                AIAbilityMaxUsesInTheTurnConsiderationDef sourceAbilityMaxUseConsiderion = DefCache.GetDef<AIAbilityMaxUsesInTheTurnConsiderationDef>("Queen_SpawnFacehuggerNotUsed_AIConsiderationDef");
                AIAbilityMaxUsesInTheTurnConsiderationDef spawneryAIConsideration = Helper.CreateDefFromClone(sourceAbilityMaxUseConsiderion, aIConsiderationGUID, aIActionName);


                string aITargetGeneratorGUID = "{20CBA94D-ADF0-42DA-BE90-182096E1B119}";
                AISpawnActorPositionTargetGeneratorDef sourceTargetGenerator = DefCache.GetDef<AISpawnActorPositionTargetGeneratorDef>("Queen_SpawnActorPosition_AITargetGeneratorDef");
                AISpawnActorPositionTargetGeneratorDef spawneryTargetGenerator = Helper.CreateDefFromClone(sourceTargetGenerator, aITargetGeneratorGUID, aIActionName);

                //  List<AIActionDef> aIActionDefs = new List<AIActionDef>(spawneryAIActionsTemplate.ActionDefs.ToList()) { spawneryAIAction };
                //   spawneryAIActionsTemplate.ActionDefs = aIActionDefs.ToArray();

                spawneryAIActionsTemplate.ActionDefs = new AIActionDef[] { spawneryAIAction };

                spawneryAIAction.AbilityDefs = new TacticalAbilityDef[] { newSpawnerySpawnAbility };
                spawneryAIAction.Weight = 1000;
                spawneryAIAction.EarlyExitConsiderations = new AIAdjustedConsideration[] { };
                spawneryAIAction.Evaluations[0].TargetGeneratorDef = spawneryTargetGenerator;
                spawneryAIAction.Evaluations[0].Considerations.RemoveAt(0);

                spawneryTargetGenerator.SpawnActorAbility = newSpawnerySpawnAbility;


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }
        }

        public static void CreateObjectivesBaseDefense()
        {
            try
            {
                KillActorFactionObjectiveDef killActorFactionObjectiveSource = DefCache.GetDef<KillActorFactionObjectiveDef>("E_KillSentinels [Nest_AlienBase_CustomMissionTypeDef]");

                string nameObjectiveDestroySpawnery = "PhoenixBaseInfestation";
                GameTagDef source = DefCache.GetDef<GameTagDef>("Takeshi_Tutorial3_GameTagDef");
                GameTagDef gameTagMainObjective = Helper.CreateDefFromClone(
                    source,
                    "{B42E4079-EDC6-4E7A-9720-8F8839FCD3CE}",
                    nameObjectiveDestroySpawnery + "_GameTagDef");

                KillActorFactionObjectiveDef killInfestation = Helper.CreateDefFromClone(killActorFactionObjectiveSource, "5BDA1D39-80A8-4EB8-A34F-92FB08AF2CB5", nameObjectiveDestroySpawnery);
                killInfestation.MissionObjectiveData.Description.LocalizationKey = "BASEDEFENSE_INFESTATION_OBJECTIVE";
                killInfestation.MissionObjectiveData.Summary.LocalizationKey = "BASEDEFENSE_INFESTATION_OBJECTIVE";
                killInfestation.KillTargetGameTag = gameTagMainObjective;
                killInfestation.IsVictoryObjective = false;

                string nameObjectiveDestroySentinel = "PhoenixBaseDestroySentinel";

                KillActorFactionObjectiveDef killSentinel = Helper.CreateDefFromClone(killActorFactionObjectiveSource, "{97745084-836A-4D5C-A1F5-052EDEC307A5}", nameObjectiveDestroySentinel);
                killSentinel.MissionObjectiveData.Description.LocalizationKey = "BASEDEFENSE_SENTINEL_OBJECTIVE";
                killSentinel.MissionObjectiveData.Summary.LocalizationKey = "BASEDEFENSE_SENTINEL_OBJECTIVE";
                killSentinel.KillTargetGameTag = gameTagMainObjective;
                killSentinel.IsVictoryObjective = false;

                string nameObjectiveScatterEnemies = "ScatterRemainingAttackers";
                GameTagDef gameTagSecondObjective = Helper.CreateDefFromClone(
                    source,
                    "{ADACF6A2-A969-4518-AD36-C94D1A1C6A82}",
                    nameObjectiveScatterEnemies + "_GameTagDef");
                KillActorFactionObjectiveDef secondKillAll = Helper.CreateDefFromClone(killActorFactionObjectiveSource, "{B7BB4BFF-E7DC-4FD1-A307-FF348FC87946}", nameObjectiveScatterEnemies);
                secondKillAll.KillTargetGameTag = gameTagSecondObjective;
                secondKillAll.MissionObjectiveData.Description.LocalizationKey = "BASEDEFENSE_SECOND_OBJECTIVE";
                secondKillAll.MissionObjectiveData.Summary.LocalizationKey = "BASEDEFENSE_SECOND_OBJECTIVE";
                secondKillAll.ParalysedCounts = true;
                secondKillAll.AchievedWhenEnemiesAreDefeated = true;
                //secondKillAll.IsDefeatObjective = false;

                //infestation BD mission, destroy Spawnery, then scatter attackers
                killInfestation.NextOnSuccess = new FactionObjectiveDef[] { secondKillAll };

                SurviveTurnsFactionObjectiveDef sourceSurviveObjective = DefCache.GetDef<SurviveTurnsFactionObjectiveDef>("SurviveAmbush_CustomMissionObjective");
                string nameObjective = "SurviveFiveTurns";

                SurviveTurnsFactionObjectiveDef surviveFiveTurns = Helper.CreateDefFromClone(sourceSurviveObjective, "{EC7E94DD-199B-41BF-B6D7-7933CE40E0C1}", nameObjective);
                surviveFiveTurns.SurviveTurns = 5;
                surviveFiveTurns.MissionObjectiveData.Description.LocalizationKey = "BASEDEFENSE_SURVIVE5_OBJECTIVE";
                surviveFiveTurns.MissionObjectiveData.Summary.LocalizationKey = "BASEDEFENSE_SURVIVE_COMPLETE";
                surviveFiveTurns.IsDefeatObjective = false;

                //early BD mission, survive 5 turns, then scatter attackers
                surviveFiveTurns.NextOnSuccess = new FactionObjectiveDef[] { secondKillAll };



                string nameObjectivSurviveThreeTurns = "SurviveThreeTurns";

                SurviveTurnsFactionObjectiveDef surviveThreeTurns = Helper.CreateDefFromClone(sourceSurviveObjective, "{B817A3CD-482B-472F-85EC-7259451E8F88}", nameObjectivSurviveThreeTurns);
                surviveThreeTurns.SurviveTurns = 3;
                surviveThreeTurns.MissionObjectiveData.Description.LocalizationKey = "BASEDEFENSE_SURVIVE3_OBJECTIVE";
                surviveThreeTurns.MissionObjectiveData.Summary.LocalizationKey = "BASEDEFENSE_SURVIVE_COMPLETE";
                surviveThreeTurns.NextOnSuccess = new FactionObjectiveDef[] { secondKillAll };
                surviveThreeTurns.IsDefeatObjective = false;


                //Mid BD mission, first kill Sentinel, then Survive 3 turns, then scatter attackers //changed to survive 5 turns, to make it harder then first scenario
                killSentinel.NextOnSuccess = new FactionObjectiveDef[] { surviveFiveTurns };

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        public static void ChangeBaseDefense()
        {
            try
            {
                CustomMissionTypeDef baseDefenseMissionTypeDef = DefCache.GetDef<CustomMissionTypeDef>("PXBaseAlien_CustomMissionTypeDef");
                baseDefenseMissionTypeDef.MandatoryMission = false;
                baseDefenseMissionTypeDef.SkipDeploymentSelection = false;
                //  baseDefenseMissionTypeDef.ClearMissionOnCancel = false;
                baseDefenseMissionTypeDef.MaxPlayerUnits = 9;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void FixMyrmidonFlee()
        {
            try
            {
                AIActionsTemplateDef swarmerAI = DefCache.GetDef<AIActionsTemplateDef>("Swarmer_AIActionsTemplateDef");
                AIActionDef flee = DefCache.GetDef<AIActionDef>("Flee_AIActionDef");

                List<AIActionDef> aIActionDefs = new List<AIActionDef>(swarmerAI.ActionDefs)
                {
                    flee
                };
                swarmerAI.ActionDefs = aIActionDefs.ToArray();

                TacticalActorDef swarmer = DefCache.GetDef<TacticalActorDef>("Swarmer_ActorDef");

                ExitMissionAbilityDef exitMissionAbilityDef = DefCache.GetDef<ExitMissionAbilityDef>("ExitMission_AbilityDef");


                List<AbilityDef> abilityDefs = new List<AbilityDef>();
                abilityDefs = swarmer.Abilities.ToList();
                abilityDefs.Add(exitMissionAbilityDef);
                swarmer.Abilities = abilityDefs.ToArray();


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        //Not used
        public static void CreateMeleeChiron()
        {
            try


            {

                // TacticalItemDef sourceHead =DefCache.GetDef<TacticalItemDef>("Crabman_Head_Humanoid_BodyPartDef");
                //  TacticalItemDef newHead = Helper.CreateDefFromClone(sourceHead, "{A322DA22-7ED0-49D9-9F32-C4127351ABD3}", "NewChironHead");



                // newHead.RequiredSlotBinds[0].RequiredSlot = DefCache.GetDef<ItemSlotDef>("Chiron_Head_SlotDef");


                TacCharacterDef source = DefCache.GetDef<TacCharacterDef>("Chiron2_FireWormHeavy_AlienMutationVariationDef");
                string name = "MeleeChiron";
                string gUID = "{95AA563B-4EC8-4232-BB7D-A35765AD2055}";

                TacCharacterDef newChiron = Helper.CreateDefFromClone(source, gUID, name);
                newChiron.SpawnCommandId = "MeleeChiron";
                List<ItemDef> bodyParts = newChiron.Data.BodypartItems.ToList();

                bodyParts.RemoveLast();
                //    bodyParts.Add(DefCache.GetDef<WeaponDef>("Chiron_Abdomen_Mortar_WeaponDef"));
                //  bodyParts[0] = newHead; 
                //   bodyParts.Add(DefCache.GetDef<TacticalItemDef>("Crabman_Carapace_BodyPartDef"));
                newChiron.Data.BodypartItems = bodyParts.ToArray();




            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }



        }


        public static void ModifyCratesToAddArmor()
        {
            try
            {
                GameTagDef armourTag = DefCache.GetDef<GameTagDef>("ArmourItem_TagDef");
                GameTagDef synedrion = DefCache.GetDef<GameTagDef>("Synedrion_FactionTagDef");
                GameTagDef anu = DefCache.GetDef<GameTagDef>("Anu_FactionTagDef");
                GameTagDef nj = DefCache.GetDef<GameTagDef>("NewJerico_FactionTagDef");

                List<ItemDef> synArmors = new List<ItemDef>() {
                DefCache.GetDef<ItemDef>("SY_Infiltrator_Bonus_Helmet_BodyPartDef"),
                DefCache.GetDef<ItemDef>("SY_Infiltrator_Bonus_Legs_ItemDef"),
                DefCache.GetDef<ItemDef>("SY_Infiltrator_Bonus_Torso_BodyPartDef"),
                DefCache.GetDef<ItemDef>("SY_MistRepeller_Attachment_ItemDef"),
                DefCache.GetDef<ItemDef>("SY_MotionDetector_Attachment_ItemDef"),
                DefCache.GetDef<ItemDef>("SY_MultiVisualSensor_Attachment_ItemDef"),
                DefCache.GetDef<ItemDef>("SY_PoisonResistanceVest_Attachment_ItemDef"),
                DefCache.GetDef<ItemDef>("SY_Assault_Helmet_Neon_BodyPartDef"),
                DefCache.GetDef<ItemDef>("SY_Assault_Legs_Neon_ItemDef"),
                DefCache.GetDef<ItemDef>("SY_Assault_Torso_Neon_BodyPartDef"),
                DefCache.GetDef<ItemDef>("SY_Assault_Helmet_WhiteNeon_BodyPartDef"),
                DefCache.GetDef<ItemDef>("SY_Assault_Legs_WhiteNeon_ItemDef"),
                DefCache.GetDef<ItemDef>("SY_Assault_Torso_WhiteNeon_BodyPartDef"),
                DefCache.GetDef<ItemDef>("SY_Assault_Helmet_BodyPartDef"),
                DefCache.GetDef<ItemDef>("SY_Assault_Legs_ItemDef"),
                DefCache.GetDef<ItemDef>("SY_Assault_Torso_BodyPartDef"),
                DefCache.GetDef<ItemDef>("SY_Infiltrator_Helmet_BodyPartDef"),
                DefCache.GetDef<ItemDef>("SY_Infiltrator_Legs_ItemDef"),
                DefCache.GetDef<ItemDef>("SY_Infiltrator_Torso_BodyPartDef"),
                DefCache.GetDef<ItemDef>("SY_Infiltrator_Venom_Helmet_BodyPartDef"),
                DefCache.GetDef<ItemDef>("SY_Infiltrator_Venom_Legs_ItemDef"),
                DefCache.GetDef<ItemDef>("SY_Infiltrator_Venom_Torso_BodyPartDef"),
                DefCache.GetDef<ItemDef>("SY_Sniper_Helmet_BodyPartDef"),
                DefCache.GetDef<ItemDef>("SY_Sniper_Legs_ItemDef"),
                DefCache.GetDef<ItemDef>("SY_Sniper_Torso_BodyPartDef"),
                };

                List<ItemDef> njArmors = new List<ItemDef>() {
                DefCache.GetDef<ItemDef>("NJ_Assault_Helmet_BodyPartDef"),
                DefCache.GetDef<ItemDef>("NJ_Assault_Legs_ItemDef"),
                DefCache.GetDef<ItemDef>("NJ_Assault_Torso_BodyPartDef"),
                DefCache.GetDef<ItemDef>("NJ_Heavy_Helmet_BodyPartDef"),
                DefCache.GetDef<ItemDef>("NJ_Heavy_Legs_ItemDef"),
                DefCache.GetDef<ItemDef>("NJ_Heavy_Torso_BodyPartDef"),
                DefCache.GetDef<ItemDef>("NJ_Sniper_Helmet_BodyPartDef"),
                DefCache.GetDef<ItemDef>("NJ_Sniper_Legs_ItemDef"),
                DefCache.GetDef<ItemDef>("NJ_Sniper_Torso_BodyPartDef"),
                DefCache.GetDef<ItemDef>("NJ_Technician_Helmet_BodyPartDef"),
                DefCache.GetDef<ItemDef>("NJ_Technician_Legs_ItemDef"),
                DefCache.GetDef<ItemDef>("NJ_Technician_Torso_BodyPartDef"),
                DefCache.GetDef<ItemDef>("NJ_FireResistanceVest_Attachment_ItemDef"),
                DefCache.GetDef<ItemDef>("NJ_Technician_Helmet_ALN_BodyPartDef"),
                DefCache.GetDef<ItemDef>("NJ_Technician_Legs_ALN_ItemDef"),
                DefCache.GetDef<ItemDef>("NJ_Technician_Torso_ALN_BodyPartDef"),
                };

                List<ItemDef> anuArmors = new List<ItemDef>() {
                DefCache.GetDef<ItemDef>("AN_Berserker_Helmet_Viking_BodyPartDef"),
                DefCache.GetDef<ItemDef>("AN_Berserker_Legs_Viking_ItemDef"),
                DefCache.GetDef<ItemDef>("AN_Berserker_Torso_Viking_BodyPartDef"),
                DefCache.GetDef<ItemDef>("AN_Assault_Helmet_BodyPartDef"),
                DefCache.GetDef<ItemDef>("AN_Assault_Legs_ItemDef"),
                DefCache.GetDef<ItemDef>("AN_Assault_Torso_BodyPartDef"),
                DefCache.GetDef<ItemDef>("AN_Berserker_Helmet_BodyPartDef"),
                DefCache.GetDef<ItemDef>("AN_Berserker_Legs_ItemDef"),
                DefCache.GetDef<ItemDef>("AN_Berserker_Torso_BodyPartDef"),
                DefCache.GetDef<ItemDef>("AN_Priest_Legs_ItemDef"),
                DefCache.GetDef<ItemDef>("AN_Priest_Torso_BodyPartDef"),
                DefCache.GetDef<ItemDef>("AN_Priest_Legs_AZ_ItemDef"),
                DefCache.GetDef<ItemDef>("AN_Priest_Torso_AZ_BodyPartDef"),

                };

                foreach (TacticalItemDef item in anuArmors)
                {
                    item.CrateSpawnWeight = 200;
                    item.IsPickable = true;

                    //    TFTVLogger.Always(item.name + " has a chance of " + item.CrateSpawnWeight + " to spawn");

                }

                foreach (TacticalItemDef item in njArmors)
                {
                    item.CrateSpawnWeight = 200;
                    item.IsPickable = true;
                    //  TFTVLogger.Always(item.name + " has a chance of " + item.CrateSpawnWeight + " to spawn");

                }

                foreach (TacticalItemDef item in synArmors)
                {
                    item.CrateSpawnWeight = 200;
                    item.IsPickable = true;

                    //TFTVLogger.Always(item.name + " has a chance of " + item.CrateSpawnWeight + " to spawn");

                }



                InventoryComponentDef anuCrates = DefCache.GetDef<InventoryComponentDef>("Crate_AN_InventoryComponentDef");

                anuCrates.ItemDefs.AddRangeToArray(anuArmors.ToArray());


                InventoryComponentDef njCrates = DefCache.GetDef<InventoryComponentDef>("Crate_NJ_InventoryComponentDef");

                njCrates.ItemDefs.AddRangeToArray(njArmors.ToArray());

                InventoryComponentDef synCrates = DefCache.GetDef<InventoryComponentDef>("Crate_SY_InventoryComponentDef");

                synCrates.ItemDefs.AddRangeToArray(synArmors.ToArray());


                /* List<ItemDef> synCratesList = new List<ItemDef>();
                 synCratesList.AddRange(synCrates.ItemDefs.ToList());
                 synCratesList.AddRange(synArmors);

                 synCrates.ItemDefs = synCratesList.ToArray();*/

                /*   foreach(TacticalItemDef tacticalItemDef in Repo.GetAllDefs<TacticalItemDef>().Where(tid => tid.Tags.Contains(armourTag)))
                       {

                       if (tacticalItemDef.Tags.Contains(anu)) 
                       { 
                       anuCrates.ItemDefs.AddToArray(tacticalItemDef);
                           TFTVLogger.Always("Added " + tacticalItemDef.name + " to Anu crates");

                       }
                       else if (tacticalItemDef.Tags.Contains(nj))
                       {
                           njCrates.ItemDefs.AddToArray(tacticalItemDef);
                           TFTVLogger.Always("Added " + tacticalItemDef.name + " to NJ crates");

                       }
                       else if (tacticalItemDef.Tags.Contains(synedrion))
                       {
                           synCrates.ItemDefs.AddToArray(tacticalItemDef);
                           TFTVLogger.Always("Added " + tacticalItemDef.name + " to Syn crates");

                       }


                   }*/


            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }



        public static void AddLoadingScreens()
        {
            try
            {
                LoadingScreenArtCollectionDef loadingScreenArtCollectionDef = DefCache.GetDef<LoadingScreenArtCollectionDef>("LoadingScreenArtCollectionDef");

                Sprite forsaken = Helper.CreateSpriteFromImageFile("fo_squad.jpg");
                Sprite pure = Helper.CreateSpriteFromImageFile("squad_pu.jpg");

                loadingScreenArtCollectionDef.LoadingScreenImages.Add(Helper.CreateSpriteFromImageFile("CrisisAnu_1_uinomipmaps.jpg"));
                loadingScreenArtCollectionDef.LoadingScreenImages.Add(Helper.CreateSpriteFromImageFile("CrisisNJ_1_uinomipmaps.jpg"));
                loadingScreenArtCollectionDef.LoadingScreenImages.Add(Helper.CreateSpriteFromImageFile("CrisisNJ_2_uinomipmaps.jpg"));
                loadingScreenArtCollectionDef.LoadingScreenImages.Add(Helper.CreateSpriteFromImageFile("CrisisOther_1_uinomipmaps.jpg"));
                loadingScreenArtCollectionDef.LoadingScreenImages.Add(Helper.CreateSpriteFromImageFile("CrisisSyn_1_uinomipmaps.jpg"));
                loadingScreenArtCollectionDef.LoadingScreenImages.Add(Helper.CreateSpriteFromImageFile("DebateNJ_1_uinomipmaps.jpg"));
                loadingScreenArtCollectionDef.LoadingScreenImages.Add(Helper.CreateSpriteFromImageFile("Encounter_1_scarab_uinomipmaps.jpg"));
                loadingScreenArtCollectionDef.LoadingScreenImages.Add(Helper.CreateSpriteFromImageFile("Encounter_2_armadillo_uinomipmaps.jpg"));
                loadingScreenArtCollectionDef.LoadingScreenImages.Add(Helper.CreateSpriteFromImageFile("Encounter_3_aspida_uinomipmaps.jpg"));

                loadingScreenArtCollectionDef.LoadingScreenImages.Add(forsaken);
                loadingScreenArtCollectionDef.LoadingScreenImages.Add(pure);

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void AddTips()
        {
            try
            {
                LoadingTipsRepositoryDef loadingTipsRepositoryDef = DefCache.GetDef<LoadingTipsRepositoryDef>("LoadingTipsRepositoryDef");
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_1" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_2" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_3" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_4" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_5" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_6" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_7" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_8" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_9" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_10" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_11" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_12" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_13" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_14" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_15" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_16" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_17" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_18" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_19" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_20" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_21" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_22" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_23" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_24" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_25" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_26" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_27" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_28" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_29" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_30" });


                loadingTipsRepositoryDef.TacticalLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_TACTICAL_1" });
                loadingTipsRepositoryDef.TacticalLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_TACTICAL_2" });
                loadingTipsRepositoryDef.TacticalLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_TACTICAL_3" });
                loadingTipsRepositoryDef.TacticalLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_TACTICAL_4" });
                loadingTipsRepositoryDef.TacticalLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_TACTICAL_5" });
                loadingTipsRepositoryDef.TacticalLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_TACTICAL_6" });
                loadingTipsRepositoryDef.TacticalLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_TACTICAL_7" });
                loadingTipsRepositoryDef.TacticalLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_TACTICAL_8" });
                loadingTipsRepositoryDef.TacticalLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_TACTICAL_9" });
                loadingTipsRepositoryDef.TacticalLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_TACTICAL_10" });

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }





        public static void FixUnarmedAspida()
        {
            try
            {
                DefCache.GetDef<TacCharacterDef>("SY_AspidaInfested_TacCharacterDef").Data.EquipmentItems
                        = new ItemDef[] { DefCache.GetDef<ItemDef>("SY_Aspida_Arms_GroundVehicleWeaponDef") };

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }



        }


        public static void RemoveScyllaAndNodeResearches()
        {
            try
            {
                ResearchDbDef researchDbDef = DefCache.GetDef<ResearchDbDef>("aln_ResearchDB");

                ResearchDef scylla1Research = DefCache.GetDef<ResearchDef>("ALN_Scylla1_ResearchDef");
                ResearchDef scylla2Research = DefCache.GetDef<ResearchDef>("ALN_Scylla2_ResearchDef");
                ResearchDef scylla3Research = DefCache.GetDef<ResearchDef>("ALN_Scylla3_ResearchDef");
                ResearchDef scylla4Research = DefCache.GetDef<ResearchDef>("ALN_Scylla4_ResearchDef");
                ResearchDef scylla5Research = DefCache.GetDef<ResearchDef>("ALN_Scylla5_ResearchDef");
                ResearchDef scylla6Research = DefCache.GetDef<ResearchDef>("ALN_Scylla6_ResearchDef");
                ResearchDef scylla7Research = DefCache.GetDef<ResearchDef>("ALN_Scylla7_ResearchDef");
                ResearchDef scylla8Research = DefCache.GetDef<ResearchDef>("ALN_Scylla8_ResearchDef");
                ResearchDef scylla9Research = DefCache.GetDef<ResearchDef>("ALN_Scylla9_ResearchDef");
                ResearchDef scylla10Research = DefCache.GetDef<ResearchDef>("ALN_Scylla10_ResearchDef");
                ResearchDef nodeResearch = DefCache.GetDef<ResearchDef>("ALN_CorruptionNode_ResearchDef");

                researchDbDef.Researches.RemoveAll(r => r.name.Contains("ALN_Scylla"));
                researchDbDef.Researches.Remove(nodeResearch);

                ResearchDef alnMFSoldiersResearch = DefCache.GetDef<ResearchDef>("ALN_MindfraggedSoldiers_ResearchDef");
                alnMFSoldiersResearch.RevealRequirements = DefCache.GetDef<ResearchDef>("ALN_CrabmanBasic_ResearchDef").RevealRequirements;
                alnMFSoldiersResearch.InitialStates = DefCache.GetDef<ResearchDef>("ALN_CrabmanBasic_ResearchDef").InitialStates;
                alnMFSoldiersResearch.Priority = DefCache.GetDef<ResearchDef>("ALN_CrabmanBasic_ResearchDef").Priority;
                alnMFSoldiersResearch.ResearchCost = DefCache.GetDef<ResearchDef>("ALN_CrabmanBasic_ResearchDef").ResearchCost;



            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void ModifyRecruitsCost()
        {
            try
            {
                GameDifficultyLevelDef veryhard = DefCache.GetDef<GameDifficultyLevelDef>("VeryHard_GameDifficultyLevelDef");
                //Hero
                GameDifficultyLevelDef hard = DefCache.GetDef<GameDifficultyLevelDef>("Hard_GameDifficultyLevelDef");
                //Standard
                GameDifficultyLevelDef standard = DefCache.GetDef<GameDifficultyLevelDef>("Standard_GameDifficultyLevelDef");
                //Easy
                GameDifficultyLevelDef easy = DefCache.GetDef<GameDifficultyLevelDef>("Easy_GameDifficultyLevelDef");


                easy.RecruitCostPerLevelMultiplier = 0.5f;
                standard.RecruitCostPerLevelMultiplier = 0.75f;


            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void CreateEtermesStatuses()
        {
            try
            {
                CreateEtermesProtectionStatus();
                CreateEtermesVulnerability();

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        public static void CreateEtermesProtectionStatus()
        {
            try
            {

                StandardDamageTypeEffectDef fireDamage = DefCache.GetDef<StandardDamageTypeEffectDef>("Fire_StandardDamageTypeEffectDef");
                StandardDamageTypeEffectDef standardDamageTypeEffectDef = DefCache.GetDef<StandardDamageTypeEffectDef>("Projectile_StandardDamageTypeEffectDef");
                AcidDamageTypeEffectDef acidDamage = DefCache.GetDef<AcidDamageTypeEffectDef>("Acid_DamageOverTimeDamageTypeEffectDef");

                string statusName = "EtermesProtectionStatus";
                string gUID = "35EC0B4B-C0C7-4EB4-B3F0-64E71507CB6D";
                string gUIDVisuals = "EB475735-E388-49BE-80B6-6AA6907C9138";

                DamageMultiplierStatusDef source = DefCache.GetDef<DamageMultiplierStatusDef>("BionicResistances_StatusDef");
                DamageMultiplierStatusDef newStatus = Helper.CreateDefFromClone(
                    source,
                    gUID,
                    statusName);
                newStatus.EffectName = statusName;
                newStatus.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.Hidden;
                newStatus.VisibleOnPassiveBar = true;
                newStatus.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;
                newStatus.ApplicationConditions = new EffectConditionDef[] { };

                newStatus.Visuals = Helper.CreateDefFromClone(
                    source.Visuals,
                    gUIDVisuals,
                    statusName + "Visuals");
                newStatus.Multiplier = 0.75f;
                newStatus.MultiplierType = DamageMultiplierType.Incoming;
                newStatus.Range = -1;
                List<DamageTypeBaseEffectDef> damageTypeBaseEffectDefs = new List<DamageTypeBaseEffectDef>();
                damageTypeBaseEffectDefs.AddRange(newStatus.DamageTypeDefs);
                damageTypeBaseEffectDefs.Add(fireDamage);
                damageTypeBaseEffectDefs.Add(standardDamageTypeEffectDef);
                damageTypeBaseEffectDefs.Add(acidDamage);

                newStatus.DamageTypeDefs = damageTypeBaseEffectDefs.ToArray();

                newStatus.Visuals.LargeIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_HunkerDown_2-2.png");
                newStatus.Visuals.SmallIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_HunkerDown_2-2.png");


                newStatus.Visuals.DisplayName1.LocalizationKey = "ETERMES_VULNERABILITY_NAME";
                newStatus.Visuals.Description.LocalizationKey = "ETERMES_PROTECTION_DESCRIPTION";

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }


        public static void CreateEtermesVulnerability()
        {
            try
            {
                StandardDamageTypeEffectDef fireDamage = DefCache.GetDef<StandardDamageTypeEffectDef>("Fire_StandardDamageTypeEffectDef");
                StandardDamageTypeEffectDef standardDamageTypeEffectDef = DefCache.GetDef<StandardDamageTypeEffectDef>("Projectile_StandardDamageTypeEffectDef");
                AcidDamageTypeEffectDef acidDamage = DefCache.GetDef<AcidDamageTypeEffectDef>("Acid_DamageOverTimeDamageTypeEffectDef");

                string statusName = "EtermesVulnerabilityStatus";
                string gUID = "B5135532-82F2-48B3-8B2A-3B3433D438AF";
                string gUIDVisuals = "30F37D69-5629-403E-A610-6B245B7665CD";

                DamageMultiplierStatusDef source = DefCache.GetDef<DamageMultiplierStatusDef>("BionicResistances_StatusDef");
                DamageMultiplierStatusDef newStatus = Helper.CreateDefFromClone(
                    source,
                    gUID,
                    statusName);
                newStatus.EffectName = statusName;
                newStatus.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.Hidden;
                newStatus.VisibleOnPassiveBar = true;
                newStatus.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;
                newStatus.ApplicationConditions = new EffectConditionDef[] { };

                newStatus.Visuals = Helper.CreateDefFromClone(
                    source.Visuals,
                    gUIDVisuals,
                    statusName + "Visuals");
                newStatus.Multiplier = 1.25f;
                newStatus.MultiplierType = DamageMultiplierType.Incoming;
                newStatus.Range = -1;
                List<DamageTypeBaseEffectDef> damageTypeBaseEffectDefs = new List<DamageTypeBaseEffectDef>();
                damageTypeBaseEffectDefs.AddRange(newStatus.DamageTypeDefs);
                damageTypeBaseEffectDefs.Add(fireDamage);
                damageTypeBaseEffectDefs.Add(standardDamageTypeEffectDef);
                damageTypeBaseEffectDefs.Add(acidDamage);


                newStatus.DamageTypeDefs = damageTypeBaseEffectDefs.ToArray();

                newStatus.Visuals.DisplayName1.LocalizationKey = "ETERMES_VULNERABILITY_NAME";
                newStatus.Visuals.Description.LocalizationKey = "ETERMES_VULNERABILITY_DESCRIPTION";
                newStatus.Visuals.LargeIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_HunkerDown_1-2.png");
                newStatus.Visuals.SmallIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_HunkerDown_1-2.png");
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }




        public static void CreateRookieProtectionStatus()
        {
            try
            {

                StandardDamageTypeEffectDef fireDamage = DefCache.GetDef<StandardDamageTypeEffectDef>("Fire_StandardDamageTypeEffectDef");
                StandardDamageTypeEffectDef standardDamageTypeEffectDef = DefCache.GetDef<StandardDamageTypeEffectDef>("Projectile_StandardDamageTypeEffectDef");
                AcidDamageTypeEffectDef acidDamage = DefCache.GetDef<AcidDamageTypeEffectDef>("Acid_DamageOverTimeDamageTypeEffectDef");


                string statusName = "RookieProtectionStatus";
                string gUID = "B7F811AF-D919-462D-8045-D42C08B1706D";
                string gUIDVisuals = "DD77459C-6B4E-42B7-81C9-B425EB305E3B";

                DamageMultiplierStatusDef source = DefCache.GetDef<DamageMultiplierStatusDef>("BionicResistances_StatusDef");
                DamageMultiplierStatusDef newStatus = Helper.CreateDefFromClone(
                    source,
                    gUID,
                    statusName);
                newStatus.EffectName = statusName;
                newStatus.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.Hidden;
                newStatus.VisibleOnPassiveBar = true;
                newStatus.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;
                newStatus.ApplicationConditions = new EffectConditionDef[] { };

                newStatus.Visuals = Helper.CreateDefFromClone(
                    source.Visuals,
                    gUIDVisuals,
                    statusName + "Visuals");
                newStatus.Multiplier = 0.5f;
                newStatus.MultiplierType = DamageMultiplierType.Incoming;
                newStatus.Range = -1;
                List<DamageTypeBaseEffectDef> damageTypeBaseEffectDefs = new List<DamageTypeBaseEffectDef>();
                damageTypeBaseEffectDefs.AddRange(newStatus.DamageTypeDefs);
                damageTypeBaseEffectDefs.Add(fireDamage);
                damageTypeBaseEffectDefs.Add(standardDamageTypeEffectDef);
                damageTypeBaseEffectDefs.Add(acidDamage);

                newStatus.DamageTypeDefs = damageTypeBaseEffectDefs.ToArray();

                newStatus.Visuals.LargeIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_HunkerDown_2-2.png");
                newStatus.Visuals.SmallIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_HunkerDown_2-2.png");


                newStatus.Visuals.DisplayName1.LocalizationKey = "ROOKIE_VULNERABILITY_NAME";
                newStatus.Visuals.Description.LocalizationKey = "ROOKIE_PROTECTION_DESCRIPTION";

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }


        public static void CreateRookieVulnerability()
        {
            try
            {
                StandardDamageTypeEffectDef fireDamage = DefCache.GetDef<StandardDamageTypeEffectDef>("Fire_StandardDamageTypeEffectDef");
                StandardDamageTypeEffectDef standardDamageTypeEffectDef = DefCache.GetDef<StandardDamageTypeEffectDef>("Projectile_StandardDamageTypeEffectDef");
                AcidDamageTypeEffectDef acidDamage = DefCache.GetDef<AcidDamageTypeEffectDef>("Acid_DamageOverTimeDamageTypeEffectDef");

                string statusName = "RookieVulnerabilityStatus";
                string gUID = "C8468900-F4A0-4E47-92B2-AA7CBEB9EE13";
                string gUIDVisuals = "3F3697B6-487B-4610-A2B0-B2A17AA67C72";

                DamageMultiplierStatusDef source = DefCache.GetDef<DamageMultiplierStatusDef>("BionicResistances_StatusDef");
                DamageMultiplierStatusDef newStatus = Helper.CreateDefFromClone(
                    source,
                    gUID,
                    statusName);
                newStatus.EffectName = statusName;
                newStatus.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.Hidden;
                newStatus.VisibleOnPassiveBar = true;
                newStatus.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;
                newStatus.ApplicationConditions = new EffectConditionDef[] { };

                newStatus.Visuals = Helper.CreateDefFromClone(
                    source.Visuals,
                    gUIDVisuals,
                    statusName + "Visuals");
                newStatus.Multiplier = 1.5f;
                newStatus.MultiplierType = DamageMultiplierType.Incoming;
                newStatus.Range = -1;
                List<DamageTypeBaseEffectDef> damageTypeBaseEffectDefs = new List<DamageTypeBaseEffectDef>();
                damageTypeBaseEffectDefs.AddRange(newStatus.DamageTypeDefs);
                damageTypeBaseEffectDefs.Add(fireDamage);
                damageTypeBaseEffectDefs.Add(standardDamageTypeEffectDef);
                damageTypeBaseEffectDefs.Add(acidDamage);

                newStatus.DamageTypeDefs = damageTypeBaseEffectDefs.ToArray();

                newStatus.Visuals.DisplayName1.LocalizationKey = "ROOKIE_VULNERABILITY_NAME";
                newStatus.Visuals.Description.LocalizationKey = "ROOKIE_VULNERABILITY_DESCRIPTION";
                newStatus.Visuals.LargeIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_HunkerDown_1-2.png");
                newStatus.Visuals.SmallIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_HunkerDown_1-2.png");
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }


        public static void RemovePirateKing()
        {
            try
            {

                GeoscapeEventDef ProgSynIntroWin = DefCache.GetDef<GeoscapeEventDef>("PROG_SY0_WIN_GeoscapeEventDef");
                ProgSynIntroWin.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Clear();

                GeoscapeEventDef fireBirdMiss = DefCache.GetDef<GeoscapeEventDef>("PROG_SY2_MISS_GeoscapeEventDef");
                fireBirdMiss.GeoscapeEventData.Choices[0].Outcome.StartMission.WonEventID = "PROG_SY3_WIN";

                GeoscapeEventDef pirateKingWin = DefCache.GetDef<GeoscapeEventDef>("PROG_SY3_WIN_GeoscapeEventDef");
                pirateKingWin.GeoscapeEventData.Title.LocalizationKey = "PROG_SY2_WIN_TITLE";


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        public static void ChangeMyrmidonAndFirewormResearchRewards()
        {
            try
            {
                ResearchDef myrmidonResearch = DefCache.GetDef<ResearchDef>("PX_Alien_Swarmer_ResearchDef");
                DefCache.GetDef<ExistingResearchRequirementDef>("PX_LightSniperRifle_ResearchDef_ExistingResearchRequirementDef_0").ResearchID = myrmidonResearch.Id;
                myrmidonResearch.Resources.Add(ResourceType.Supplies, 100);
                myrmidonResearch.Resources.Add(ResourceType.Materials, 100);

                DefCache.GetDef<ResearchDef>("PX_Alien_Fireworm_ResearchDef").Resources.Add(ResourceType.Supplies, 150);
                DefCache.GetDef<ResearchDef>("PX_Alien_SwarmerEgg_ResearchDef").Resources.Add(ResourceType.Supplies, 400);




            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }



        }


        public static void CreateFireQuenchers()
        {
            try
            {
                CloneFireImmunityAbility();
                CreateFireQuencherStatus();
                CreateFireQuencherAbility();
                CreateFireQuencherHint();

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }


        public static void CreateFireQuencherHint()
        {
            try
            {
                DamageMultiplierStatusDef status = DefCache.GetDef<DamageMultiplierStatusDef>("FireQuencherStatus");



                string hintName = "FIRE_QUENCHER";
                string hintTitle = "HINT_FIRE_QUENCHER_TITLE";
                string hintText = "HINT_FIRE_QUENCHER_TEXT";


                TFTVTutorialAndStory.CreateNewTacticalHint(hintName, HintTrigger.ActorSeen, status.name, hintTitle, hintText, 2, true, "5F24B699-455E-44E5-831D-1CA79B9E3EED");



            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }



        }



        //cloning the DamageMultiplierAbilityDef fire immunity ability because because through Status effect only achieving immunity for body part
        //Need to clone to make it invisible in status panel
        public static void CloneFireImmunityAbility()
        {
            try
            {
                string abilityName = "FireImmunityInvisibleAbility";
                string gUID = "9A55315E-4694-4D95-8811-476C524EBAAE";
                // string characterProgressionGUID = "AA24A50E-C61A-4CD8-97FE-3F8BAC5F7BAA";
                string viewElementGUID = "231F088F-A4F0-4E6D-BC78-614AD0EF4594";



                DamageMultiplierAbilityDef source = DefCache.GetDef<DamageMultiplierAbilityDef>("FireImmunity_DamageMultiplierAbilityDef");
                DamageMultiplierAbilityDef newAbility = Helper.CreateDefFromClone(
                    source,
                   gUID,
                    abilityName);


                /*newAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    characterProgressionGUID,
                   abilityName + "CharacterProgression");*/
                newAbility.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    viewElementGUID,
                    abilityName + "ViewElement");
                newAbility.ViewElementDef.ShowInStatusScreen = false;
                newAbility.ViewElementDef.ShowInFreeAimMode = false;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }



        public static void CreateFireQuencherAbility()
        {
            try
            {
                string abilityName = "FireQuencherAbility";
                string gUID = "020679B9-A7AD-45F9-BCD5-0EC13FB0D396";
                string characterProgressionGUID = "EEBE2E43-C8CC-4E05-9777-149FC0DBB874";
                string viewElementGUID = "AA346C20-3163-4A95-AD9B-E9C5678CB282";

                DamageMultiplierStatusDef status = DefCache.GetDef<DamageMultiplierStatusDef>("FireQuencherStatus");

                ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("BionicDamageMultipliers_AbilityDef");
                ApplyStatusAbilityDef newAbility = Helper.CreateDefFromClone(
                    source,
                   gUID,
                    abilityName);


                newAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    characterProgressionGUID,
                   abilityName + "CharacterProgression");
                newAbility.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    viewElementGUID,
                    abilityName + "ViewElement");
                newAbility.ViewElementDef.ShowInStatusScreen = true;
                newAbility.ViewElementDef.ShowInFreeAimMode = true;
                //  newAbility.ViewElementDef.ShowInStatusScreen = false;

                DamageMultiplierAbilityDef fireImmunity = DefCache.GetDef<DamageMultiplierAbilityDef>("FireImmunity_DamageMultiplierAbilityDef");

                newAbility.ViewElementDef.DisplayName1.LocalizationKey = "FIRE_QUENCHER_NAME";
                newAbility.ViewElementDef.Description.LocalizationKey = "FIRE_QUENCHER_DESCRIPTION";
                newAbility.ViewElementDef.LargeIcon = fireImmunity.ViewElementDef.LargeIcon;
                newAbility.ViewElementDef.SmallIcon = fireImmunity.ViewElementDef.SmallIcon;
                newAbility.StatusDef = status;
                newAbility.TargetApplicationConditions = new EffectConditionDef[] { };
                newAbility.StatusApplicationTrigger = StatusApplicationTrigger.ActorEnterPlay;
                newAbility.StatusSource = StatusSource.AbilitySource;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateFireQuencherStatus()
        {

            try
            {
                StandardDamageTypeEffectDef fireDamage = DefCache.GetDef<StandardDamageTypeEffectDef>("Fire_StandardDamageTypeEffectDef");

                string statusName = "FireQuencherStatus";
                string gUID = "CC8B3A1B-E25D-43F4-9469-52FBE6F9C926";
                string gUIDVisuals = "2B927AA0-7CA5-473D-9847-31718002B552";

                DamageMultiplierStatusDef source = DefCache.GetDef<DamageMultiplierStatusDef>("BionicResistances_StatusDef");
                DamageMultiplierStatusDef newStatus = Helper.CreateDefFromClone(
                    source,
                    gUID,
                    statusName);
                newStatus.EffectName = statusName;
                newStatus.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.AlwaysVisible;
                newStatus.VisibleOnPassiveBar = true;
                newStatus.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnBodyPartStatusList;
                newStatus.ApplicationConditions = new EffectConditionDef[] { };

                newStatus.Visuals = Helper.CreateDefFromClone(
                    source.Visuals,
                    gUIDVisuals,
                    statusName + "Visuals");
                newStatus.Multiplier = 1f;
                newStatus.MultiplierType = DamageMultiplierType.Incoming;
                newStatus.Range = -1;
                newStatus.DamageTypeDefs = new StandardDamageTypeEffectDef[] { };

                DamageMultiplierAbilityDef fireImmunity = DefCache.GetDef<DamageMultiplierAbilityDef>("FireImmunity_DamageMultiplierAbilityDef");

                newStatus.Visuals.LargeIcon = fireImmunity.ViewElementDef.LargeIcon;
                newStatus.Visuals.SmallIcon = fireImmunity.ViewElementDef.SmallIcon;


                newStatus.Visuals.DisplayName1.LocalizationKey = "FIRE_QUENCHER_NAME";
                newStatus.Visuals.Description.LocalizationKey = "FIRE_QUENCHER_DESCRIPTION";
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }



        public static void Testing()
        {
            try
            {
                AddingSafetyConsiderationToMoveAndAttack();
                AddingSafetyConsiderationToRandomMove();
                AddingSafetyConsiderationToRegularAdvance();
                ModifySafetyConsiderationDef();
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void ModifySafetyConsiderationDef()
        {
            try
            {
                AISafePositionConsiderationDef safePositionConsideration = DefCache.GetDef<AISafePositionConsiderationDef>("DefenseSafePosition_AIConsiderationDef");

                safePositionConsideration.HighCoverProtection = 1f;
                safePositionConsideration.LowCoverProtection = 0.5f;
                safePositionConsideration.NoneCoverProtection = 0f;
                safePositionConsideration.VisionScoreWhenVisibleByAllEnemies = 1f;
                safePositionConsideration.VisionRange = 20;


            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }


        public static void AddingSafetyConsiderationToMoveAndAttack()
        {
            try
            {

                AIActionMoveAndAttackDef moveAndAttack = DefCache.GetDef<AIActionMoveAndAttackDef>("MoveAndShoot_AIActionDef");

                AISafePositionConsiderationDef safePositionConsideration = DefCache.GetDef<AISafePositionConsiderationDef>("DefenseSafePosition_AIConsiderationDef");


                AIAdjustedConsideration aIAdjustedConsideration = new AIAdjustedConsideration()
                {
                    Consideration = safePositionConsideration,
                    ScoreCurve = moveAndAttack.Evaluations[1].Considerations.First().ScoreCurve
                };

                List<AIAdjustedConsideration> aIAdjustedConsiderations = moveAndAttack.Evaluations[1].Considerations.ToList();
                aIAdjustedConsiderations.Add(aIAdjustedConsideration);
                moveAndAttack.Evaluations.First().Considerations = aIAdjustedConsiderations.ToArray();





            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void AddingSafetyConsiderationToRegularAdvance()
        {
            try
            {
                AIActionMoveToPositionDef advanceNormal = DefCache.GetDef<AIActionMoveToPositionDef>("Advance_Normal_AIActionDef");

                AISafePositionConsiderationDef safePositionConsideration = DefCache.GetDef<AISafePositionConsiderationDef>("DefenseSafePosition_AIConsiderationDef");


                AIAdjustedConsideration aIAdjustedConsideration = new AIAdjustedConsideration()
                {
                    Consideration = safePositionConsideration,
                    ScoreCurve = advanceNormal.Evaluations.First().Considerations.First().ScoreCurve
                };

                List<AIAdjustedConsideration> aIAdjustedConsiderations = advanceNormal.Evaluations.First().Considerations.ToList();
                aIAdjustedConsiderations.Add(aIAdjustedConsideration);
                advanceNormal.Evaluations.First().Considerations = aIAdjustedConsiderations.ToArray();


            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void AddingSafetyConsiderationToRandomMove()
        {
            try
            {

                AIActionMoveToPositionDef moveToRandom = DefCache.GetDef<AIActionMoveToPositionDef>("MoveToRandomWaypoint_AIActionDef");


                AISafePositionConsiderationDef safePositionConsideration = DefCache.GetDef<AISafePositionConsiderationDef>("DefenseSafePosition_AIConsiderationDef");



                AIAdjustedConsideration aIAdjustedConsideration = new AIAdjustedConsideration()
                {
                    Consideration = safePositionConsideration,
                    ScoreCurve = moveToRandom.Evaluations.First().Considerations.First().ScoreCurve
                };

                List<AIAdjustedConsideration> aIAdjustedConsiderations = moveToRandom.Evaluations.First().Considerations.ToList();
                aIAdjustedConsiderations.Add(aIAdjustedConsideration);
                moveToRandom.Evaluations.First().Considerations = aIAdjustedConsiderations.ToArray();


            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void AddLoreEntries()
        {
            try
            {
                CreateAlistairLoreEntry();
                CreateOlenaLoreEntry();
                CreateBennuLoreEntry();
                CreateHelenaLoreEntry();

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateHelenaLoreEntry()
        {
            try
            {
                string gUID = "217EB721-52E6-4401-90D1-3287D6CC8DC2";
                string name = "Helena_Lore";
                string title = "TFTV_LORE_HELENA_TITLE";
                string description = "TFTV_LORE_HELAN_DESCRIPTION";
                string pic = "lore_helena.png";
                GeoPhoenixpediaEntryDef alistairEntry = CreateLoreEntry(name, gUID, title, description, pic);
                DefCache.GetDef<GeoscapeEventDef>("HelenaOnOlena").GeoscapeEventData.Choices[0].Outcome.GivePhoenixpediaEntries = new List<GeoPhoenixpediaEntryDef>() { alistairEntry };
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }


        public static void CreateBennuLoreEntry()
        {
            try
            {
                string gUID = "0A67EB59-5E9B-46A9-95EE-EC6C47417B7C";
                string name = "Bennu_Lore";
                string title = "TFTV_LORE_BENNU_TITLE";
                string description = "TFTV_LORE_BENNU_DESCRIPTION";
                string pic = "lore_bennu.png";
                GeoPhoenixpediaEntryDef alistairEntry = CreateLoreEntry(name, gUID, title, description, pic);
                DefCache.GetDef<GeoscapeEventDef>("IntroBetterGeo_2").GeoscapeEventData.Choices[0].Outcome.GivePhoenixpediaEntries = new List<GeoPhoenixpediaEntryDef>() { alistairEntry };
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }


        public static void CreateOlenaLoreEntry()
        {
            try
            {
                string gUID = "38ACBF41-7D2D-479F-981E-10FED4FC6800";
                string name = "Olena_Lore";
                string title = "TFTV_LORE_OLENA_TITLE";
                string description = "TFTV_LORE_OLENA_DESCRIPTION";
                string pic = "lore_olena.png";
                GeoPhoenixpediaEntryDef alistairEntry = CreateLoreEntry(name, gUID, title, description, pic);
                DefCache.GetDef<GeoscapeEventDef>("IntroBetterGeo_1").GeoscapeEventData.Choices[0].Outcome.GivePhoenixpediaEntries = new List<GeoPhoenixpediaEntryDef>() { alistairEntry };
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }



        public static void CreateAlistairLoreEntry()
        {
            try
            {
                string gUID = "B955090F-62E0-41F2-9036-3548A1DC5F46";
                string name = "Alistair_Lore";
                string title = "TFTV_LORE_ALISTAIR_TITLE";
                string description = "TFTV_LORE_ALISTAIR_DESCRIPTION";
                string pic = "lore_alistair.png";
                GeoPhoenixpediaEntryDef alistairEntry = CreateLoreEntry(name, gUID, title, description, pic);
                DefCache.GetDef<GeoscapeEventDef>("IntroBetterGeo_0").GeoscapeEventData.Choices[0].Outcome.GivePhoenixpediaEntries = new List<GeoPhoenixpediaEntryDef>() { alistairEntry };
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }


        public static GeoPhoenixpediaEntryDef CreateLoreEntry(string name, string gUID, string title, string description, string pic)
        {
            try
            {
                GeoPhoenixpediaEntryDef source = DefCache.GetDef<GeoPhoenixpediaEntryDef>("AntediluvianArchaeology_GeoPhoenixpediaEntryDef");
                GeoPhoenixpediaEntryDef newLoreEntry = Helper.CreateDefFromClone(source, gUID, name);
                newLoreEntry.Category = PhoenixpediaCategoryType.Lore;
                newLoreEntry.Entry.Title.LocalizationKey = title;
                newLoreEntry.Entry.Description.LocalizationKey = description;
                newLoreEntry.Entry.DetailsImage = Helper.CreateSpriteFromImageFile(pic);
                return newLoreEntry;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }


        }



        public static void SyphonAttackFix()
        {
            try
            {
                DefCache.GetDef<SyphoningDamageKeywordDataDef>("Syphon_DamageKeywordDataDef").SyphonBasedOnHealthDamageDealt = false;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        public static void AddContributionPointsToPriestAndTech()
        {
            try
            {
                DefCache.GetDef<MindControlAbilityDef>("Priest_MindControl_AbilityDef").ContributionPointsOnUse = 500;
                DefCache.GetDef<ApplyStatusAbilityDef>("InducePanic_AbilityDef").ContributionPointsOnUse = 500;
                DefCache.GetDef<InstilFrenzyAbilityDef>("Priest_InstilFrenzy_AbilityDef").ContributionPointsOnUse = 500;
                DefCache.GetDef<PsychicScreamAbilityDef>("Priest_PsychicScream_AbilityDef").ContributionPointsOnUse = 500;
                DefCache.GetDef<TacticalAbilityDef>("ThrowTurret_AbilityDef").ContributionPointsOnUse = 500;
                DefCache.GetDef<TacticalAbilityDef>("ThrowPRCRTurret_AbilityDef").ContributionPointsOnUse = 500;
                DefCache.GetDef<TacticalAbilityDef>("ThrowLaserTurret_AbilityDef").ContributionPointsOnUse = 500;
                DefCache.GetDef<ApplyStatusAbilityDef>("ElectricReinforcement_AbilityDef").ContributionPointsOnUse = 500;
                DefCache.GetDef<TacticalAbilityDef>("DeployLaserTurret_AbilityDef").ContributionPointsOnUse = 500;
                DefCache.GetDef<TacticalAbilityDef>("DeployPRCRTurret_AbilityDef").ContributionPointsOnUse = 500;
                DefCache.GetDef<TacticalAbilityDef>("DeployTurret_AbilityDef").ContributionPointsOnUse = 500;
                DefCache.GetDef<ApplyStatusAbilityDef>("ManualControl_AbilityDef").ContributionPointsOnUse = 500;
                DefCache.GetDef<HealAbilityDef>("FieldMedic_AbilityDef").ContributionPointsOnUse = 500;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        public static void ChangesToLOTA()
        {
            try
            {
                CreateAbilitiesForAncients();
                CreateAncientStatusEffects();
                CreateParalysisDamageImmunity();
                CreateLOTAHints();
                CreateAncientAutomataResearch();
                CreateExoticMaterialsResearch();
                CreateLivingCrystalResearch();
                CreateProteanMutaneResearch();
                ChangeCostAncientProbe();
                AddHoplitePortrait();
                CyclopsMindCrushEffect();
                ChangeHoplites();
                ChangeAncientsWeapons();
                ChangeSchemataMissionRequirement();
                ChangeAncientSiteExploration();
                ChangeImpossibleWeapons();
                RemovePandoranVirusResearchRequirement();
                CreateEventsForLOTA();
                ChangeAncientDefenseMission();
                ChangeAncientSiteMissions();
                ModifyCyclops();
                CyclopsJoinStreamsAttack();
                ImplementHintsAndResearches();


                //  CreateImpossibleWeaponsManufactureRequirements();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }


        private static void ImplementHintsAndResearches()
        {
            try
            {

                ContextHelpHintDef hintStory1 = DefCache.GetDef<ContextHelpHintDef>("ANCIENTS_STORY1");
                ContextHelpHintDef hintCyclops = DefCache.GetDef<ContextHelpHintDef>("ANCIENTS_CYCLOPS");
                ContextHelpHintDef hintCyclopsDefense = DefCache.GetDef<ContextHelpHintDef>("ANCIENTS_CYCLOPSDEFENSE");
                ContextHelpHintDef hintHoplites = DefCache.GetDef<ContextHelpHintDef>("ANCIENTS_HOPLITS");
                ContextHelpHintDef hintHopliteRepair = DefCache.GetDef<ContextHelpHintDef>("ANCIENTS_HOPLITSREPAIR");
                ContextHelpHintDef hintHopliteMaxPower = DefCache.GetDef<ContextHelpHintDef>("ANCIENTS_HOPLITSMAXPOWER");

                ContextHelpHintDbDef alwaysDisplayedTacticalHintsDbDef = DefCache.GetDef<ContextHelpHintDbDef>("AlwaysDisplayedTacticalHintsDbDef");

                ResearchDbDef researchDB = DefCache.GetDef<ResearchDbDef>("pp_ResearchDB");

                ResearchDef ancientAutomataResearch = DefCache.GetDef<ResearchDef>("AncientAutomataResearch");
                ResearchDef pX_LivingCrystalResearch = DefCache.GetDef<ResearchDef>("PX_LivingCrystalResearchDef");
                ResearchDef pX_ProteanMutaneResearch = DefCache.GetDef<ResearchDef>("PX_ProteanMutaneResearchDef");


                AncientSiteProbeItemDef ancientSiteProbeItemDef = DefCache.GetDef<AncientSiteProbeItemDef>("AncientSiteProbeItemDef");



                if (!researchDB.Researches.Contains(ancientAutomataResearch))
                {
                    researchDB.Researches.Add(ancientAutomataResearch);
                }
                if (!researchDB.Researches.Contains(pX_LivingCrystalResearch))
                {
                    researchDB.Researches.Add(pX_LivingCrystalResearch);
                }
                if (!researchDB.Researches.Contains(pX_ProteanMutaneResearch))
                {
                    researchDB.Researches.Add(pX_ProteanMutaneResearch);
                }
                if (!alwaysDisplayedTacticalHintsDbDef.Hints.Contains(hintStory1))
                {
                    alwaysDisplayedTacticalHintsDbDef.Hints.Add(hintStory1);
                }
                if (!alwaysDisplayedTacticalHintsDbDef.Hints.Contains(hintCyclops))
                {
                    alwaysDisplayedTacticalHintsDbDef.Hints.Add(hintCyclops);
                }
                if (!alwaysDisplayedTacticalHintsDbDef.Hints.Contains(hintCyclopsDefense))
                {
                    alwaysDisplayedTacticalHintsDbDef.Hints.Add(hintCyclopsDefense);
                }
                if (!alwaysDisplayedTacticalHintsDbDef.Hints.Contains(hintHoplites))
                {
                    alwaysDisplayedTacticalHintsDbDef.Hints.Add(hintHoplites);
                }
                if (!alwaysDisplayedTacticalHintsDbDef.Hints.Contains(hintHopliteRepair))
                {
                    alwaysDisplayedTacticalHintsDbDef.Hints.Add(hintHopliteRepair);
                }
                if (!alwaysDisplayedTacticalHintsDbDef.Hints.Contains(hintHopliteMaxPower))
                {
                    alwaysDisplayedTacticalHintsDbDef.Hints.Add(hintHopliteMaxPower);
                }


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        internal static void CyclopsMindCrushEffect()
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

        internal static void AddHoplitePortrait()
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


        public static void RemoveMindControlImmunityVFX()
        {
            try
            {
                DefCache.GetDef<TacStatusDef>("MindControlImmunity_StatusDef").ParticleEffectPrefab = new GameObject() { };

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        public static void CreateVoidOmenRemindersInTactical()
        {
            try
            {
                TFTVCommonMethods.CreateObjectiveReminder("818B37C5-AC05-4245-A629-D84761838DE6", "VOID_OMEN_TITLE_3", 0);
                TFTVCommonMethods.CreateObjectiveReminder("F0CCE047-352C-4AE4-8D12-6856FA57A5C7", "VOID_OMEN_TITLE_5", 0);
                TFTVCommonMethods.CreateObjectiveReminder("BDBBD195-D07C-43CF-AB0F-50C7CEA8B044", "VOID_OMEN_TITLE_7", 0);
                TFTVCommonMethods.CreateObjectiveReminder("EC9011E4-2C01-485B-8E89-7D0A20996899", "VOID_OMEN_TITLE_10", 0);
                TFTVCommonMethods.CreateObjectiveReminder("023E1C64-2FC7-48CB-BFD1-007509907FEE", "VOID_OMEN_TITLE_14", 0);
                TFTVCommonMethods.CreateObjectiveReminder("3CBE9291-2241-428B-B6DD-776EFF316D4F", "VOID_OMEN_TITLE_15", 0);
                TFTVCommonMethods.CreateObjectiveReminder("D25FC8F1-DB31-4BA2-9B9F-3787B9D3A664", "VOID_OMEN_TITLE_16", 0);
                TFTVCommonMethods.CreateObjectiveReminder("BA859656-03E9-4BCD-AAAC-2A0B09506FEC", "VOID_OMEN_TITLE_19", 0);

                //3, 5, 7, 10, 15, 16, 19

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }



        internal static void ModifyCyclops()
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


        public static void CreateParalysisDamageImmunity()
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

        public static void ChangeAncientSiteMissions()
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

                //  TacCharacterDef crystalScylla = DefCache.GetDef<TacCharacterDef>("Scylla10_Crystal_AlienMutationVariationDef");


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
                    /*  TacMissionTypeParticipantData.UniqueChatarcterBind scyllaLegend = new TacMissionTypeParticipantData.UniqueChatarcterBind()
                      {
                          Amount = { Min = 1, Max = 1 },
                          Character = crystalScylla,
                          Difficulty = legend,

                      };*/

                    List<TacMissionTypeParticipantData.UniqueChatarcterBind> uniqueUnits = customMissionTypeDef.ParticipantsData[0].UniqueUnits.ToList();
                    uniqueUnits.Add(drillerVeteran);
                    uniqueUnits.Add(shielderVeteran);
                    uniqueUnits.Add(drillerHero);
                    uniqueUnits.Add(shielderHero);
                    uniqueUnits.Add(drillerLegend);
                    uniqueUnits.Add(shielderLegend);
                    // uniqueUnits.Add(scyllaLegend);
                    customMissionTypeDef.ParticipantsData[0].UniqueUnits = uniqueUnits.ToArray();

                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void ChangeAncientDefenseMission()
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
                        Difficulty = easy,

                    },


                        new TacMissionTypeParticipantData.UniqueChatarcterBind()
                        {
                            Amount = { Min = 3, Max = 3 },
                            Character = driller,
                            Difficulty = easy,

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
                    // customMissionType.MandatoryMission = true; //to prevent being able to cancel it
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

        public static void CreateLivingCrystalResearch()
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

                ExistingResearchRequirementDef requiremen2tDef = TFTVCommonMethods.CreateNewExistingResearchResearchRequirementDef(id + "ResearchReq", "F0D428A7-9D51-4746-9C60-1EFADD5457B8", "ExoticMaterialsResearch");



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



            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateProteanMutaneResearch()
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

                ExistingResearchRequirementDef requiremen2tDef = TFTVCommonMethods.CreateNewExistingResearchResearchRequirementDef(id + "ResearchReq", "041C8F9B-0F7F-467F-9DD6-183C3B901B56", "ExoticMaterialsResearch");


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

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateEventsForLOTA()
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

        public static void CreateLOTAHints()
        {
            try
            {


                ClassTagDef cyclopsTag = DefCache.GetDef<ClassTagDef>("MediumGuardian_ClassTagDef");
                ClassTagDef hopliteTag = DefCache.GetDef<ClassTagDef>("HumanoidGuardian_ClassTagDef");
                MissionTypeTagDef ancientMissionTag = DefCache.GetDef<MissionTypeTagDef>("MissionTypeAncientSiteAttack_MissionTagDef");

                DamageMultiplierStatusDef AddAutoRepairStatusAbility = DefCache.GetDef<DamageMultiplierStatusDef>("AutoRepair_AddAbilityStatusDef");
                DamageMultiplierStatusDef ancientsPowerUpStatus = DefCache.GetDef<DamageMultiplierStatusDef>("AncientsPoweredUp");

                string hintStory1 = "ANCIENTS_STORY1";
                string story1Title = "HINT_ANCIENTS_STORY1_TITLE";
                string story1Text = "HINT_ANCIENTS_STORY1_TEXT";
                string hintCyclops = "ANCIENTS_CYCLOPS";
                string cyclopsTitle = "HINT_ANCIENTS_CYCLOPS_TITLE";
                string cyclopsText = "HINT_ANCIENTS_CYCLOPS_TEXT";
                string hintCyclopsDefense = "ANCIENTS_CYCLOPSDEFENSE";
                string cyclopsDefenseTitle = "HINT_ANCIENTS_CYCLOPSDEFENSE_TITLE";
                string cyclopsDefenseText = "HINT_ANCIENTS_CYCLOPSDEFENSE_TEXT";
                string hintHoplites = "ANCIENTS_HOPLITS";
                string hoplitesTitle = "HINT_ANCIENTS_HOPLITS_TITLE";
                string hoplitesText = "HINT_ANCIENTS_HOPLITS_TEXT";
                string hintHopliteRepair = "ANCIENTS_HOPLITSREPAIR";
                string hoplitesRepairTitle = "HINT_ANCIENTS_HOPLITSREPAIR_TITLE";
                string hoplitesRepairText = "HINT_ANCIENTS_HOPLITSREPAIR_TEXT";
                string hintHopliteMaxPower = "ANCIENTS_HOPLITSMAXPOWER";
                string hopliteMaxPowerTitle = "HINT_ANCIENTS_HOPLITSMAXPOWER_TITLE";
                string hopliteMaxPowerText = "HINT_ANCIENTS_HOPLITSMAXPOWER_TEXT";

                TFTVTutorialAndStory.CreateNewTacticalHint(hintCyclops, HintTrigger.ActorSeen, cyclopsTag.name, cyclopsTitle, cyclopsText, 1, true, "41B73D60-433A-4F75-9E8B-CA30FBE45622");
                TFTVTutorialAndStory.CreateNewTacticalHint(hintHoplites, HintTrigger.ActorSeen, hopliteTag.name, hoplitesTitle, hoplitesText, 1, true, "2DC1BC66-F42F-4E84-9680-826A57C28E48");
                TFTVTutorialAndStory.CreateNewTacticalHint(hintCyclopsDefense, HintTrigger.ActorHurt, cyclopsTag.name, cyclopsDefenseTitle, cyclopsDefenseText, 1, true, "E4A4FB8B-10ED-49CF-870A-6ED9497F6895");
                TFTVTutorialAndStory.CreateNewTacticalHint(hintStory1, HintTrigger.MissionStart, ancientMissionTag.name, story1Title, story1Text, 3, true, "24C57D44-3CBA-4310-AB09-AE9444822C91");
                ContextHelpHintDef hoplitesHint = DefCache.GetDef<ContextHelpHintDef>(hintHoplites);
                hoplitesHint.Conditions.Add(TFTVTutorialAndStory.ActorHasStatusHintConditionDefCreateNewConditionForTacticalHint("Alerted_StatusDef"));
                TFTVTutorialAndStory.CreateNewTacticalHint(hintHopliteRepair, HintTrigger.ActorSeen, AddAutoRepairStatusAbility.name, hoplitesRepairTitle, hoplitesRepairText, 2, true, "B25F1794-5641-40D3-88B5-0AA104FC75A1");
                TFTVTutorialAndStory.CreateNewTacticalHint(hintHopliteMaxPower, HintTrigger.ActorSeen, ancientsPowerUpStatus.name, hopliteMaxPowerTitle, hopliteMaxPowerText, 2, true, "0DC75121-325A-406E-AC37-5F1AAB4E7778");



            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }



        }

        public static void RemovePandoranVirusResearchRequirement()
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


        public static void ChangeImpossibleWeapons()
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

        public static void ChangeCostAncientProbe()
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

        public static void ChangeAncientSiteExploration()
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

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
        public static void ChangeSchemataMissionRequirement()
        {
            try
            {

                DefCache.GetDef<GeoResearchEventFilterDef>("E_PROG_LE1_ResearchCompleted [GeoResearchEventFilterDef]").ResearchID = "ExoticMaterialsResearch";

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }

        }

        public static void CreateAncientAutomataResearch()
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
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void CreateExoticMaterialsResearch()
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

                ExistingResearchRequirementDef requirementDef = TFTVCommonMethods.CreateNewExistingResearchResearchRequirementDef(defName + "ResearchReq", "47BA0BD6-F622-4EC7-A49B-B93C0A955D3C", "AncientAutomataResearch");

                ReseachRequirementDefOpContainer[] revealRequirementContainer = new ReseachRequirementDefOpContainer[1];
                ResearchRequirementDef[] revealResearchRequirementDefs = new ResearchRequirementDef[1];
                revealResearchRequirementDefs[0] = requirementDef; //small box
                revealRequirementContainer[0].Requirements = revealResearchRequirementDefs; //medium box
                research.RevealRequirements.Container = revealRequirementContainer;
                research.RevealRequirements.Operation = ResearchContainerOperation.ALL;
                research.Tags = new ResearchTagDef[] { CriticalResearchTag };

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void CreateAbilitiesForAncients()
        {
            try
            {
                //Creating status effect to show that Guardian will repair a body part next turn. Need to create a status to show small icon.

                DamageMultiplierStatusDef sourceAbilityStatusDef = DefCache.GetDef<DamageMultiplierStatusDef>("BionicResistances_StatusDef");

                string statusSelfRepairAbilityName = "AutoRepair_AddAbilityStatusDef";
                DamageMultiplierStatusDef statusSelfRepairAbilityDef = Helper.CreateDefFromClone(sourceAbilityStatusDef, "17A2DF06-6BA5-46F3-92B5-D85F74193ABD", statusSelfRepairAbilityName);
                statusSelfRepairAbilityDef.EffectName = "Selfrepair";
                statusSelfRepairAbilityDef.ApplicationConditions = new EffectConditionDef[] { };
                statusSelfRepairAbilityDef.Visuals = Helper.CreateDefFromClone(sourceAbilityStatusDef.Visuals, "2330D0AE-547B-492E-8C49-16BFDD498653", statusSelfRepairAbilityName);
                statusSelfRepairAbilityDef.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.AlwaysVisible;
                statusSelfRepairAbilityDef.VisibleOnPassiveBar = true;
                statusSelfRepairAbilityDef.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;
                statusSelfRepairAbilityDef.Visuals.DisplayName1.LocalizationKey = "HOPLITES_SELF_REPAIR_NAME";
                statusSelfRepairAbilityDef.Visuals.Description.LocalizationKey = "HOPLITES_SELF_REPAIR_DESCRIPTION";
                statusSelfRepairAbilityDef.Visuals.LargeIcon = Helper.CreateSpriteFromImageFile("TFTV_status_self_repair.png");
                statusSelfRepairAbilityDef.Visuals.SmallIcon = Helper.CreateSpriteFromImageFile("TFTV_status_self_repair.png");
                statusSelfRepairAbilityDef.DamageTypeDefs = new DamageTypeBaseEffectDef[] { };
                statusSelfRepairAbilityDef.Multiplier = 1;


            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateAncientStatusEffects()
        {
            try
            {

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
                statusCyclopsDefense.Visuals.SmallIcon = Helper.CreateSpriteFromImageFile("TFTV_status_cyclops_defense.png");

                statusCyclopsDefense.Visuals.DisplayName1.LocalizationKey = "CYCLOPS_DEFENSE_NAME";
                statusCyclopsDefense.Visuals.Description.LocalizationKey = "CYCLOPS_DEFENSE_DESCRIPTION";


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

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
      


        public static void ChangeAncientsWeapons()
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

        internal static void CyclopsJoinStreamsAttack()
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

        public static void ChangeHoplites()
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
      
        public static void CreateSubject24()
        {
            try
            {
                string nameDef = "Subject24_TacCharacerDef";

                TacCharacterDef subject24 = Helper.CreateDefFromClone(DefCache.GetDef<TacCharacterDef>("NJ_Jugg_TacCharacterDef"), "A4F0335E-BF41-4175-8C28-7B0DE5352224", nameDef);
                subject24.Data.Name = "Subject 24";

                // CustomizationColorTagDef_10 green
                // CustomizationColorTagDef_14 pink
                // CustomizationColorTagDef_0 grey
                // CustomizationColorTagDef_7 red

                CustomizationPrimaryColorTagDef blackColor = DefCache.GetDef<CustomizationPrimaryColorTagDef>("CustomizationColorTagDef_9");
                List<GameTagDef> gameTags = subject24.Data.GameTags.ToList();
                gameTags.Add(blackColor);
                subject24.SpawnCommandId = "Subject24TFTV";
                subject24.Data.GameTags = gameTags.ToArray();

                List<TacMissionTypeParticipantData.UniqueChatarcterBind> tacCharacterDefs = DefCache.GetDef<CustomMissionTypeDef>("StoryPU14_CustomMissionTypeDef").ParticipantsData[1].UniqueUnits.ToList();
                TacMissionTypeParticipantData.UniqueChatarcterBind uniqueChatarcterBind = new TacMissionTypeParticipantData.UniqueChatarcterBind
                {
                    Character = subject24,
                    Amount = new Base.Utils.RangeDataInt { Max = 1, Min = 1 },
                };
                tacCharacterDefs.Add(uniqueChatarcterBind);
                DefCache.GetDef<CustomMissionTypeDef>("StoryPU14_CustomMissionTypeDef").ParticipantsData[1].UniqueUnits = tacCharacterDefs.ToArray();

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void AllowMedkitsToTargetMutoidsAndChangesToMutoidSkillSet()
        {
            try
            {
                //Allow medkits to target Mutoids
                DefCache.GetDef<TacticalTargetingDataDef>("E_TargetingData [Medkit_AbilityDef]").Origin.CullTargetTags.Clear();


                //Skill toss per Belial's suggestions

                //need to change/clone CharacterProgressionData 
                AbilityCharacterProgressionDef sourceFirstLevel = DefCache.GetDef<AbilityCharacterProgressionDef>("E_CharacterProgressionData [GooImmunity_AbilityDef]");


                DefCache.GetDef<AbilityCharacterProgressionDef>("E_CharacterProgressionData [VirusResistant_DamageMultiplierAbilityDef]").MutagenCost = 10;
                AbilityCharacterProgressionDef demolitionStanceCPD =
                    Helper.CreateDefFromClone(sourceFirstLevel, "F4DA4D75-8FCE-4414-BB88-7A065A45105C", "E_CharacterProgressionData [Demolition_AbilityDef]");



                AbilityCharacterProgressionDef mindControlImmunityCPD = DefCache.GetDef<AbilityCharacterProgressionDef>("E_CharacterProgressionData [MindControlImmunity_AbilityDef]");
                mindControlImmunityCPD.MutagenCost = 15;
                mindControlImmunityCPD.SkillPointCost = 0;
                mindControlImmunityCPD.RequiredSpeed = 0;
                mindControlImmunityCPD.RequiredStrength = 0;
                mindControlImmunityCPD.RequiredWill = 0;

                AbilityCharacterProgressionDef poisonImmunityCPD = Helper.CreateDefFromClone(sourceFirstLevel, "67418B3A-C666-41CE-B504-853C6C705284", "E_CharacterProgressionData [PoisonImmunity_AbilityDef]");
                poisonImmunityCPD.MutagenCost = 20;
                DamageMultiplierAbilityDef poisonImmunity = DefCache.GetDef<DamageMultiplierAbilityDef>("PoisonImmunity_DamageMultiplierAbilityDef");
                poisonImmunity.CharacterProgressionData = poisonImmunityCPD;


                AbilityCharacterProgressionDef acidResistanceCPD = Helper.CreateDefFromClone(sourceFirstLevel, "03367F73-97B9-4E65-919B-D31DF147EAA0", "E_CharacterProgressionData [AcidResistance_AbilityDef]");
                acidResistanceCPD.MutagenCost = 25;
                DamageMultiplierAbilityDef acidResistance = DefCache.GetDef<DamageMultiplierAbilityDef>("AcidResistant_DamageMultiplierAbilityDef");
                acidResistance.CharacterProgressionData = acidResistanceCPD;

                AbilityCharacterProgressionDef leapCPD = Helper.CreateDefFromClone(sourceFirstLevel, "99339FAB-3FA5-4472-89B6-52A816464637", "E_CharacterProgressionData [RocketLeap_AbilityDef]");
                leapCPD.MutagenCost = 30;

                JetJumpAbilityDef leap = DefCache.GetDef<JetJumpAbilityDef>("Exo_Leap_AbilityDef");
                leap.CharacterProgressionData = leapCPD;




                ApplyStatusAbilityDef demolitionAbility = DefCache.GetDef<ApplyStatusAbilityDef>("DemolitionMan_AbilityDef");
                demolitionAbility.CharacterProgressionData = demolitionStanceCPD;



                AbilityTrackDef arthronAbilityTrack = DefCache.GetDef<AbilityTrackDef>("E_AbilityTrack [ArthronSpecializationDef]");
                //    AbilityTrackDef tritonAbilityTrack = DefCache.GetDef<AbilityTrackDef>("E_AbilityTrack [TritonSpecializationDef]");
                AbilityTrackDef sirenAbilityTrack = DefCache.GetDef<AbilityTrackDef>("E_AbilityTrack [SirenSpecializationDef]");
                AbilityTrackDef scyllaAbilityTrack = DefCache.GetDef<AbilityTrackDef>("E_AbilityTrack [ScyllaSpecializationDef]");
                AbilityTrackDef acheronAbilityTrack = DefCache.GetDef<AbilityTrackDef>("E_AbilityTrack [AcheronSpecializationDef]");



                arthronAbilityTrack.AbilitiesByLevel[0].Ability = DefCache.GetDef<DamageMultiplierAbilityDef>("VirusResistant_DamageMultiplierAbilityDef");
                arthronAbilityTrack.AbilitiesByLevel[2].Ability = poisonImmunity;
                arthronAbilityTrack.AbilitiesByLevel[4].Ability = DefCache.GetDef<ApplyEffectAbilityDef>("MistBreather_AbilityDef");

                //Reduce cost of Mutoid Syphon attack to 1AP
                DefCache.GetDef<BashAbilityDef>("Mutoid_Syphon_Strike_AbilityDef").ActionPointCost = 0.25f;


                scyllaAbilityTrack.AbilitiesByLevel[0].Ability = demolitionAbility;
                scyllaAbilityTrack.AbilitiesByLevel[4].Ability = leap;

                sirenAbilityTrack.AbilitiesByLevel[3].Ability = acidResistance;

                acheronAbilityTrack.AbilitiesByLevel[1].Ability = DefCache.GetDef<ApplyStatusAbilityDef>("MindControlImmunity_AbilityDef");

            }


            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void RemoveCensusResearch()
        {
            try
            {
                DefCache.GetDef<ResearchDbDef>("pp_ResearchDB").Researches.Remove(DefCache.GetDef<ResearchDef>("PX_SDI_ResearchDef"));
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateHints()
        {
            try
            {
                TFTVTutorialAndStory.CreateNewTacticalHint("UmbraSighted", HintTrigger.ActorSeen, "Oilcrab_TacCharacterDef", "UMBRA_SIGHTED_TITLE", "UMBRA_SIGHTED_TEXT", 0, true, "C63F5953-9D29-4245-8FCD-1B8B875C007D");
                TFTVTutorialAndStory.CreateNewTacticalHint("UmbraSightedTriton", HintTrigger.ActorSeen, "Oilfish_TacCharacterDef", "UMBRA_SIGHTED_TITLE", "UMBRA_SIGHTED_TEXT", 0, true, "7F85AF7F-D7F0-41F3-B6EF-839509FCCF00");
                TFTVTutorialAndStory.CreateNewTacticalHint("AcheronPrime", HintTrigger.ActorSeen, "AcheronPrime_TacCharacterDef", "HINT_ACHERON_PRIME_TITLE", "HINT_ACHERON_PRIME_DESCRIPTION", 0, true, "0266C7C5-B5A4-41B8-9987-653248113CC5");
                TFTVTutorialAndStory.CreateNewTacticalHint("AcheronAsclepius", HintTrigger.ActorSeen, "AcheronAsclepius_TacCharacterDef", "HINT_ACHERON_ASCLEPIUS_TITLE", "HINT_ACHERON_ASCLEPIUS_DESCRIPTION", 0, true, "F34ED218-BF6D-44CD-B653-9EC8C7AB0D84");
                TFTVTutorialAndStory.CreateNewTacticalHint("AcheronAsclepiusChampion", HintTrigger.ActorSeen, "AcheronAsclepiusChampion_TacCharacterDef", "HINT_ACHERON_ASCLEPIUS_CHAMPION_TITLE", "HINT_ACHERON_ASCLEPIUS_CHAMPION_DESCRIPTION", 0, true, "2FA6F938-0928-4C3A-A514-91F3BD90E048");
                TFTVTutorialAndStory.CreateNewTacticalHint("AcheronAchlys", HintTrigger.ActorSeen, "AcheronAchlys_TacCharacterDef", "HINT_ACHERON_ACHLYS_TITLE", "HINT_ACHERON_ACHLYS_DESCRIPTION", 0, true, "06EEEA6B-1264-4616-AC78-1A2A56911E72");
                TFTVTutorialAndStory.CreateNewTacticalHint("AcheronAchlysChampion", HintTrigger.ActorSeen, "AcheronAchlysChampion_TacCharacterDef", "HINT_ACHERON_ACHLYS_CHAMPION_TITLE", "HINT_ACHERON_ACHLYS_CHAMPION_DESCRIPTION", 0, true, "760FDBB6-1556-4B1D-AFE0-59C906672A5D");
                TFTVTutorialAndStory.CreateNewTacticalHint("RevenantSighted", HintTrigger.ActorSeen, "Any_Revenant_TagDef", "REVENANT_SIGHTED_TITLE", "REVENANT_SIGHTED_TEXT", 1, true, "194317EC-67DF-4775-BAFD-98499F82C2D7");

                TFTVTutorialAndStory.CreateNewTacticalHintInfestationMission("InfestationMissionIntro", "BBC5CAD0-42FF-4BBB-8E13-7611DC5695A6", "1ED63949-4375-4A9D-A017-07CF483F05D5", "2A01E924-A26B-44FB-AD67-B1B590B4E1D5");
                TFTVTutorialAndStory.CreateNewTacticalHintInfestationMission("InfestationMissionIntro2", "164A4170-F7DC-4350-90C0-D5C1A0284E0D", "CA236EF2-6E6B-4CE4-89E9-17157930F91A", "422A7D39-0110-4F5B-98BB-66B1B5F616DD");
                TFTVTutorialAndStory.CreateNewManualTacticalHint("TFTV_Tutorial1", "0D36F3D5-9A39-4A5C-B6A4-85B5A3007655", "KEY_TUT3_TFTV1_TITLE", "KEY_TUT3_TFTV1_DESCRIPTION");
                TFTVTutorialAndStory.CreateNewManualTacticalHint("TFTV_Tutorial2", "EA319607-D2F3-4293-AECE-91AC26C9BD5E", "KEY_TUT3_TFTV2_TITLE", "KEY_TUT3_TFTV2_DESCRIPTION");
                ContextHelpHintDef tutorialTFTV1 = DefCache.GetDef<ContextHelpHintDef>("TFTV_Tutorial1");
                ContextHelpHintDef tutorialTFTV2 = DefCache.GetDef<ContextHelpHintDef>("TFTV_Tutorial2");
                ContextHelpHintDef tutorial3MissionEnd = DefCache.GetDef<ContextHelpHintDef>("TUT3_MissionSuccess_HintDef");
                tutorial3MissionEnd.NextHint = tutorialTFTV1;
                tutorialTFTV1.NextHint = tutorialTFTV2;
                tutorialTFTV1.Conditions = tutorial3MissionEnd.Conditions;
                tutorialTFTV2.Conditions = tutorial3MissionEnd.Conditions;

                HasSeenHintHintConditionDef seenOilCrabConditionDef = DefCache.GetDef<HasSeenHintHintConditionDef>("UmbraSightedHasSeenHintConditionDef");
                HasSeenHintHintConditionDef seenFishCrabConditionDef = DefCache.GetDef<HasSeenHintHintConditionDef>("UmbraSightedTritonHasSeenHintConditionDef");
                ContextHelpHintDef oilCrabHint = DefCache.GetDef<ContextHelpHintDef>("UmbraSighted");
                ContextHelpHintDef oilFishHint = DefCache.GetDef<ContextHelpHintDef>("UmbraSightedTriton");
                oilCrabHint.Conditions.Add(seenFishCrabConditionDef);
                oilFishHint.Conditions.Add(seenOilCrabConditionDef);

                TFTVTutorialAndStory.CreateNewTacticalHintInfestationMissionEnd("InfestationMissionEnd");
                CreateStaminaHint();
                CreateUIDeliriumHint();



                TFTVTutorialAndStory.CreateNewTacticalHint("HostileDefenders", HintTrigger.MissionStart, "MissionTypeHavenDefense_MissionTagDef", "HINT_HOSTILE_DEFENDERS_TITLE", "HINT_HOSTILE_DEFENDERS_TEXT", 3, true, "F2F5E5B1-5B9B-4F5B-8F5C-9B5E5B5F5B5F");
                ContextHelpHintDbDef alwaysDisplayedTacticalHintsDbDef = DefCache.GetDef<ContextHelpHintDbDef>("AlwaysDisplayedTacticalHintsDbDef");
                ContextHelpHintDef hostileDefenders = DefCache.GetDef<ContextHelpHintDef>("HostileDefenders");
                alwaysDisplayedTacticalHintsDbDef.Hints.Remove(hostileDefenders);


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void ChangesToAcherons()
        {
            try
            {
                ChangesAcheronResearches();
                CreateAcheronAbilitiesAndStatus();
                ChangesAcheronTemplates();
                ChangesAcheronsAI();
                ChangesAcheronAbilities();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        public static void ChangesAcheronAbilities()
        {
            try
            {
                string nameOfNotMetallicConditionTag = "Organic_ApplicationCondition";
                ActorHasTagEffectConditionDef sourceHasTagConditionDef = DefCache.GetDef<ActorHasTagEffectConditionDef>("HasHumanTag_ApplicationCondition");
                ActorHasTagEffectConditionDef organicEffectConditionDef = Helper.CreateDefFromClone(sourceHasTagConditionDef, "E1ADF8A5-746D-4176-9FFA-99296F96B9BE", nameOfNotMetallicConditionTag);
                organicEffectConditionDef.GameTag = DefCache.GetDef<SubstanceTypeTagDef>("Organic_SubstanceTypeTagDef");

                StatMultiplierStatusDef trembling = DefCache.GetDef<StatMultiplierStatusDef>("Trembling_StatusDef");
                trembling.ApplicationConditions = new EffectConditionDef[] { organicEffectConditionDef };

                DefCache.GetDef<CallReinforcementsAbilityDef>("Acheron_CallReinforcements_AbilityDef").WillPointCost = 10;



            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        public static void ChangesAcheronsAI()
        {
            try
            {
                //Adjusting Delirium Spray
                AINonHealthDamageAttackPositionConsiderationDef deliriumSprayConsideration = DefCache.GetDef<AINonHealthDamageAttackPositionConsiderationDef>("Acheron_CorruptionSprayAttackPosition_AIConsiderationDef");
                deliriumSprayConsideration.EnemyMask = PhoenixPoint.Tactical.AI.ActorType.Combatant;
                AcidStatusDef acidStatus = DefCache.GetDef<AcidStatusDef>("Acid_StatusDef");
                deliriumSprayConsideration.DamageTypeStatusDef = acidStatus;

                //Removes exclusions to metallic and ancients targets
                HasTagSuitabilityDef deliriumSprayExcludedTags = DefCache.GetDef<HasTagSuitabilityDef>("E_TargetSuitability [Acheron_CorruptionTagsTargetsSuitability_AIConsiderationDef]");
                List<GameTagDef> deliriumSprayCheckTargetsByTag = deliriumSprayExcludedTags.GameTagDefs.ToList();
                deliriumSprayCheckTargetsByTag.Add(DefCache.GetDef<SubstanceTypeTagDef>("Organic_SubstanceTypeTagDef"));
                deliriumSprayExcludedTags.GameTagDefs = deliriumSprayCheckTargetsByTag.ToArray();
                deliriumSprayExcludedTags.HasTag = true;

                //Adjusting GooSpray
                AINonHealthDamageAttackPositionConsiderationDef gooSprayConsideration = DefCache.GetDef<AINonHealthDamageAttackPositionConsiderationDef>("Acheron_GooSprayAttackPosition_AIConsiderationDef");
                gooSprayConsideration.EnemyMask = PhoenixPoint.Tactical.AI.ActorType.Combatant;
                deliriumSprayConsideration.DamageTypeStatusDef = acidStatus;



            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void ChangesAcheronResearches()
        {
            try
            {

                //Researches
                ResearchDef acheronResearch1 = DefCache.GetDef<ResearchDef>("ALN_Acheron1_ResearchDef");
                ResearchDef acheronResearch2 = DefCache.GetDef<ResearchDef>("ALN_Acheron2_ResearchDef");
                ResearchDef acheronResearch3 = DefCache.GetDef<ResearchDef>("ALN_Acheron3_ResearchDef");
                ResearchDef acheronResearch4 = DefCache.GetDef<ResearchDef>("ALN_Acheron4_ResearchDef");
                ResearchDef acheronResearch5 = DefCache.GetDef<ResearchDef>("ALN_Acheron5_ResearchDef");
                ResearchDef acheronResearch6 = DefCache.GetDef<ResearchDef>("ALN_Acheron6_ResearchDef");



                ExistingResearchRequirementDef acheronResearchReq2 = DefCache.GetDef<ExistingResearchRequirementDef>("ALN_Acheron2_ResearchDef_ExistingResearchRequirementDef_0");
                ExistingResearchRequirementDef acheronResearchReq3 = DefCache.GetDef<ExistingResearchRequirementDef>("ALN_Acheron3_ResearchDef_ExistingResearchRequirementDef_0");
                ExistingResearchRequirementDef acheronResearchReq4 = DefCache.GetDef<ExistingResearchRequirementDef>("ALN_Acheron4_ResearchDef_ExistingResearchRequirementDef_0");
                ExistingResearchRequirementDef acheronResearchReq5 = DefCache.GetDef<ExistingResearchRequirementDef>("ALN_Acheron5_ResearchDef_ExistingResearchRequirementDef_0");
                ExistingResearchRequirementDef acheronResearchReq6 = DefCache.GetDef<ExistingResearchRequirementDef>("ALN_Acheron6_ResearchDef_ExistingResearchRequirementDef_0");

                //Acheron Prime will require heavy Chirons
                acheronResearchReq2.ResearchID = "ALN_Chiron2_ResearchDef";
                //Acheron Ascepius & Acheron Achlys will require Goo Chirons
                acheronResearchReq3.ResearchID = "ALN_Chiron7_ResearchDef";
                acheronResearchReq5.ResearchID = "ALN_Chiron7_ResearchDef";
                //Ascepius and Achlys Champions will require Bombard Chirons
                acheronResearchReq4.ResearchID = "ALN_Chiron9_ResearchDef";
                acheronResearchReq6.ResearchID = "ALN_Chiron9_ResearchDef";


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        public static void CreateAcheronAbilitiesAndStatus()
        {
            try
            {
                //Create Acheron Harbinger ability, to be used as a flag/counter when calculating chances of getting Void Touched
                string acheronHarbingerAbilityName = "Acheron_Harbinger_AbilityDef";
                PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("SelfDefenseSpecialist_AbilityDef");
                PassiveModifierAbilityDef acheronHarbingerAbility = Helper.CreateDefFromClone(
                    source,
                    "3ABB6347-5ABA-4B4D-B786-C962B7A0540C",
                    acheronHarbingerAbilityName);
                acheronHarbingerAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "336671D9-281F-4985-8F7A-8EF424EF1FB8",
                    acheronHarbingerAbilityName);
                acheronHarbingerAbility.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "FB0B38FC-CDB7-4EDF-9E39-89111528A84B",
                    acheronHarbingerAbilityName);
                acheronHarbingerAbility.StatModifications = new ItemStatModification[0];
                acheronHarbingerAbility.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                acheronHarbingerAbility.ViewElementDef.DisplayName1.LocalizationKey = "ACHERON_HARBINGER_NAME";
                acheronHarbingerAbility.ViewElementDef.Description.LocalizationKey = "ACHERON_HARBINGER_DESCRIPTION";
                acheronHarbingerAbility.ViewElementDef.LargeIcon = TFTVDefsRequiringReinjection.VoidIcon;
                acheronHarbingerAbility.ViewElementDef.SmallIcon = TFTVDefsRequiringReinjection.VoidIcon;

                //Creating Tributary to the Void, to spread TBTV on nearby allies
                string acheronTributaryAbilityName = "Acheron_Tributary_AbilityDef";
                PassiveModifierAbilityDef acheronTributaryAbility = Helper.CreateDefFromClone(
                    source,
                    "2CDB184A-4E8D-4E9A-B957-983A1FD23313",
                    acheronTributaryAbilityName);
                acheronTributaryAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "0770EB67-52CD-4E17-9A3B-CB6C91E86BC5",
                    acheronTributaryAbilityName);
                acheronTributaryAbility.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "A82EFDD3-8ED7-46C8-8B52-D8051910419D",
                    acheronTributaryAbilityName);
                acheronTributaryAbility.StatModifications = new ItemStatModification[0];
                acheronTributaryAbility.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                acheronTributaryAbility.ViewElementDef.DisplayName1.LocalizationKey = "ACHERON_TRIBUTARY_NAME";
                acheronTributaryAbility.ViewElementDef.Description.LocalizationKey = "ACHERON_TRIBUTARY_DESCRIPTION";
                acheronTributaryAbility.ViewElementDef.LargeIcon = TFTVDefsRequiringReinjection.VoidIcon;
                acheronTributaryAbility.ViewElementDef.SmallIcon = TFTVDefsRequiringReinjection.VoidIcon;



                //Creating special status that will allow Umbra to target the character
                string umbraTargetStatusDefName = "TBTV_Target";
                DamageMultiplierStatusDef sourceForTargetAbility = DefCache.GetDef<DamageMultiplierStatusDef>("BionicResistances_StatusDef");

                DamageMultiplierStatusDef umbraTargetStatus = Helper.CreateDefFromClone(
                   sourceForTargetAbility,
                   "0C4558E8-2791-4669-8F5B-2DA1D20B2ADD",
                   umbraTargetStatusDefName);

                umbraTargetStatus.EffectName = "UmbraTarget";
                umbraTargetStatus.Visuals = Helper.CreateDefFromClone(
                    sourceForTargetAbility.Visuals,
                    "49A5DC8D-50B9-4CCC-A3D4-7576A1DDD375",
                    umbraTargetStatus.EffectName);
                umbraTargetStatus.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.AlwaysVisible;
                umbraTargetStatus.VisibleOnPassiveBar = true;
                umbraTargetStatus.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;
                umbraTargetStatus.Visuals.DisplayName1.LocalizationKey = "VOID_BLIGHT_NAME";
                umbraTargetStatus.Visuals.Description.LocalizationKey = "VOID_BLIGHT_DESCRIPTION";
                umbraTargetStatus.Visuals.LargeIcon = DefCache.GetDef<DeathBelcherAbilityDef>("Oilcrab_Die_DeathBelcher_AbilityDef").ViewElementDef.LargeIcon;
                umbraTargetStatus.Visuals.SmallIcon = DefCache.GetDef<DeathBelcherAbilityDef>("Oilcrab_Die_DeathBelcher_AbilityDef").ViewElementDef.SmallIcon;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void ChangesAcheronTemplates()
        {
            try
            {
                TacCharacterDef acheron = DefCache.GetDef<TacCharacterDef>("Acheron_TacCharacterDef");
                TacCharacterDef acheronPrime = DefCache.GetDef<TacCharacterDef>("AcheronPrime_TacCharacterDef");
                TacCharacterDef acheronAsclepius = DefCache.GetDef<TacCharacterDef>("AcheronAsclepius_TacCharacterDef");
                TacCharacterDef acheronAsclepiusChampion = DefCache.GetDef<TacCharacterDef>("AcheronAsclepiusChampion_TacCharacterDef");
                TacCharacterDef acheronAchlys = DefCache.GetDef<TacCharacterDef>("AcheronAchlys_TacCharacterDef");
                TacCharacterDef acheronAchlysChampion = DefCache.GetDef<TacCharacterDef>("AcheronAchlysChampion_TacCharacterDef");

                acheron.DeploymentCost = 180;
                acheronPrime.DeploymentCost = 240;
                acheronAsclepius.DeploymentCost = 310;
                acheronAchlys.DeploymentCost = 310;
                acheronAsclepiusChampion.DeploymentCost = 350;
                acheronAchlysChampion.DeploymentCost = 350;

                //Adding Harbinger of the Void to Acheron, Acheron Prime and Acheron Asclepius Champion
                //Adding Co-Delirium To Acheron Prime
                PassiveModifierAbilityDef harbinger = DefCache.GetDef<PassiveModifierAbilityDef>("Acheron_Harbinger_AbilityDef");
                PassiveModifierAbilityDef tributary = DefCache.GetDef<PassiveModifierAbilityDef>("Acheron_Tributary_AbilityDef");
                ApplyStatusAbilityDef coDeliriumAbility = DefCache.GetDef<ApplyStatusAbilityDef>("Acheron_CoCorruption_AbilityDef");

                List<TacticalAbilityDef> acheronBasicAbilities = acheron.Data.Abilites.ToList();
                acheronBasicAbilities.Add(harbinger);
                acheron.Data.Abilites = acheronBasicAbilities.ToArray();

                List<TacticalAbilityDef> acheronPrimeBasicAbilities = acheronPrime.Data.Abilites.ToList();
                acheronPrimeBasicAbilities.Add(harbinger);
                acheronPrimeBasicAbilities.Add(coDeliriumAbility);
                acheronPrime.Data.Abilites = acheronPrimeBasicAbilities.ToArray();

                /* List<TacticalAbilityDef> acheronAsclepiusChampionBasicAbilities = acheronAsclepiusChampion.Data.Abilites.ToList();
                 acheronAsclepiusChampionBasicAbilities.Add(harbinger);
                 acheronAsclepiusChampion.Data.Abilites = acheronAsclepiusChampionBasicAbilities.ToArray();*/

                List<TacticalAbilityDef> acheronAchlysChampionBasicAbilities = acheronAchlysChampion.Data.Abilites.ToList();
                acheronAchlysChampionBasicAbilities.Add(tributary);
                acheronAchlysChampion.Data.Abilites = acheronAchlysChampionBasicAbilities.ToArray();

                //Removes leap from all Acherons
                /*
                DefCache.GetDef<TacticalItemDef>("Acheron_RearRightLeg_BodyPartDef").Abilities = new AbilityDef[] { };
                DefCache.GetDef<TacticalItemDef>("AcheronAchlys_RearRightLeg_BodyPartDef").Abilities = new AbilityDef[] { };
                DefCache.GetDef<TacticalItemDef>("AcheronAchlysChampion_RearRightLeg_BodyPartDef").Abilities = new AbilityDef[] { };
                DefCache.GetDef<TacticalItemDef>("AcheronAsclepius_RearRightLeg_BodyPartDef").Abilities = new AbilityDef[] { };
                DefCache.GetDef<TacticalItemDef>("AcheronAsclepiusChampion_RearRightLeg_BodyPartDef").Abilities = new AbilityDef[] { };
                DefCache.GetDef<TacticalItemDef>("AcheronPrime_RearRightLeg_BodyPartDef").Abilities = new AbilityDef[] { };*/

                //Removing reinforcements from Acheron, Acheron Prime, Acheron Achlys and Acheron Achlys Champion
                DefCache.GetDef<TacticalItemDef>("Acheron_Head_BodyPartDef").Abilities = new AbilityDef[] { };
                DefCache.GetDef<TacticalItemDef>("AcheronPrime_Head_BodyPartDef").Abilities = new AbilityDef[] { };
                DefCache.GetDef<TacticalItemDef>("AcheronAchlys_Head_BodyPartDef").Abilities = new AbilityDef[] { };
                DefCache.GetDef<TacticalItemDef>("AcheronAchlysChampion_Head_BodyPartDef").Abilities = new AbilityDef[] { };

                //Limiting Delirium cloud to one use per turn
                ApplyDamageEffectAbilityDef deliriumCloud = DefCache.GetDef<ApplyDamageEffectAbilityDef>("Acheron_CorruptionCloud_AbilityDef");
                ApplyStatusAbilityDef pepperCloud = DefCache.GetDef<ApplyStatusAbilityDef>("Acheron_PepperCloud_ApplyStatusAbilityDef");
                deliriumCloud.UsesPerTurn = 1;
                ApplyEffectAbilityDef confusionCloud = DefCache.GetDef<ApplyEffectAbilityDef>("Acheron_ParalyticCloud_AbilityDef");
                ResurrectAbilityDef resurrectAbility = DefCache.GetDef<ResurrectAbilityDef>("Acheron_ResurrectAbilityDef");

                //Removing Restore Armor from Acheron Prime
                DefCache.GetDef<TacticalItemDef>("AcheronPrime_Husk_BodyPartDef").Abilities = new AbilityDef[] { pepperCloud };

                ApplyDamageEffectAbilityDef corrosiveCloud = DefCache.GetDef<ApplyDamageEffectAbilityDef>("Acheron_CorrosiveCloud_AbilityDef");

                //Removes CorrosiveCloud from AchlysChampion
                TacticalItemDef achlysChampionHusk = DefCache.GetDef<TacticalItemDef>("AcheronAchlysChampion_Husk_BodyPartDef");
                List<AbilityDef> achlysChampionHuskAbilities = achlysChampionHusk.Abilities.ToList();
                achlysChampionHuskAbilities.Remove(corrosiveCloud);
                achlysChampionHusk.Abilities = achlysChampionHuskAbilities.ToArray();

                //Remove Confusion cloud from Acheron Achlys
                TacticalItemDef achlysHusk = DefCache.GetDef<TacticalItemDef>("AcheronAchlys_Husk_BodyPartDef");
                List<AbilityDef> achlysHuskAbilities = achlysChampionHusk.Abilities.ToList();
                achlysHuskAbilities.Remove(confusionCloud);
                achlysChampionHusk.Abilities = achlysHuskAbilities.ToArray();

                //Adjust Acheron leap so it can only be used once per turn and doesn't cost any AP
                JetJumpAbilityDef acheronLeap = DefCache.GetDef<JetJumpAbilityDef>("Acheron_Leap_AbilityDef");
                acheronLeap.UsesPerTurn = 1;
                acheronLeap.ActionPointCost = 0;

                //Removing Resurrect and Delirium Clouds from Asclepius Husks

                TacticalItemDef asclepiusChampionHusk = DefCache.GetDef<TacticalItemDef>("AcheronAsclepiusChampion_Husk_BodyPartDef");
                List<AbilityDef> asclepiusChampionHuskAbilities = asclepiusChampionHusk.Abilities.ToList();
                asclepiusChampionHuskAbilities.Remove(resurrectAbility);
                asclepiusChampionHuskAbilities.Remove(deliriumCloud);
                asclepiusChampionHusk.Abilities = asclepiusChampionHuskAbilities.ToArray();


                TacticalItemDef asclepiusHusk = DefCache.GetDef<TacticalItemDef>("AcheronAsclepius_Husk_BodyPartDef");
                List<AbilityDef> asclepiusHuskAbilities = asclepiusHusk.Abilities.ToList();
                asclepiusHuskAbilities.Remove(resurrectAbility);
                asclepiusHuskAbilities.Remove(deliriumCloud);
                asclepiusHusk.Abilities = asclepiusHuskAbilities.ToArray();


                DamageKeywordDef poison = DefCache.GetDef<DamageKeywordDef>("Poisonous_DamageKeywordDataDef");
                DamageKeywordDef acid = DefCache.GetDef<DamageKeywordDef>("Acid_DamageKeywordDataDef");
                DamageKeywordDef standard = DefCache.GetDef<DamageKeywordDef>("Damage_DamageKeywordDataDef");

                DamageKeywordDef blast = DefCache.GetDef<DamageKeywordDef>("Blast_DamageKeywordDataDef");
                StandardDamageTypeEffectDef blastDamage = DefCache.GetDef<StandardDamageTypeEffectDef>("Blast_StandardDamageTypeEffectDef");

                WeaponDef spitArmsAcheronAchlysChampion = DefCache.GetDef<WeaponDef>("AcheronAchlysChampion_Arms_WeaponDef");
                spitArmsAcheronAchlysChampion.DamagePayload.DamageKeywords[1].DamageKeywordDef = blast;
                spitArmsAcheronAchlysChampion.DamagePayload.DamageKeywords[1].Value = 30;
                spitArmsAcheronAchlysChampion.DamagePayload.DamageKeywords.Add(new DamageKeywordPair
                {
                    DamageKeywordDef = poison,
                    Value = 30
                });
                spitArmsAcheronAchlysChampion.DamagePayload.DamageKeywords.Add(new DamageKeywordPair
                {
                    DamageKeywordDef = acid,
                    Value = 20
                });
                spitArmsAcheronAchlysChampion.DamagePayload.DamageType = blastDamage;
                spitArmsAcheronAchlysChampion.DamagePayload.AoeRadius = 2f;
                spitArmsAcheronAchlysChampion.DamagePayload.DamageDeliveryType = DamageDeliveryType.Cone;

                WeaponDef spitArmsAcheronAsclepiusChampion = DefCache.GetDef<WeaponDef>("AcheronAsclepiusChampion_Arms_WeaponDef");

                WeaponDef achlysArms = DefCache.GetDef<WeaponDef>("AcheronAchlys_Arms_WeaponDef");

                //   string guid = "2B294E66-1BE9-425B-B088-F5A9075167A6";
                WeaponDef neuroArmsCopy = new WeaponDef();//Repo.CreateDef<WeaponDef>(guid);
                ReflectionHelper.CopyFields(achlysArms, neuroArmsCopy);
                ReflectionHelper.CopyFields(spitArmsAcheronAchlysChampion, achlysArms);
                ReflectionHelper.CopyFields(neuroArmsCopy, spitArmsAcheronAsclepiusChampion);

                DamageKeywordDef mistDamageKeyword = DefCache.GetDef<DamageKeywordDef>("Mist_DamageKeywordEffectorDef");
                SpawnVoxelDamageTypeEffectDef mistDamageTypeEffect = DefCache.GetDef<SpawnVoxelDamageTypeEffectDef>("Mist_SpawnVoxelDamageTypeEffectDef");

                // Change_AcheronCorruptiveSpray();
                //Add acid and mist, increase range
                WeaponDef acheronArms = DefCache.GetDef<WeaponDef>("Acheron_Arms_WeaponDef");
                acheronArms.DamagePayload.DamageKeywords.Add(new DamageKeywordPair
                {
                    DamageKeywordDef = acid,
                    Value = 10
                });
                acheronArms.DamagePayload.DamageKeywords.Add(new DamageKeywordPair
                {
                    DamageKeywordDef = mistDamageKeyword,
                    Value = 1

                });

                acheronArms.DamagePayload.DamageType = mistDamageTypeEffect;
                acheronArms.DamagePayload.AoeRadius = 5;
                acheronArms.DamagePayload.Range = 30;
                acheronArms.DamagePayload.DamageDeliveryType = DamageDeliveryType.Cone;
                //   acheronArms.Abilities[0] = DefCache.GetDef<ShootAbilityDef>("MistLaunch_ShootAbilityDef"); 


                WeaponDef acheronPrimeArms = DefCache.GetDef<WeaponDef>("AcheronPrime_Arms_WeaponDef");
                acheronPrimeArms.DamagePayload.DamageKeywords.Add(new DamageKeywordPair
                {
                    DamageKeywordDef = acid,
                    Value = 20
                });
                acheronPrimeArms.DamagePayload.DamageKeywords.Add(new DamageKeywordPair
                {
                    DamageKeywordDef = mistDamageKeyword,
                    Value = 1

                });

                acheronPrimeArms.DamagePayload.DamageType = mistDamageTypeEffect;
                acheronPrimeArms.DamagePayload.AoeRadius = 5;
                acheronPrimeArms.DamagePayload.Range = 30;
                acheronPrimeArms.DamagePayload.DamageDeliveryType = DamageDeliveryType.Cone;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }
        public static void CreateUIDeliriumHint()
        {
            try
            {
                GeoTimeElapsedGeoHintConditionDef geoTimeElapsedGeoHintConditionDef = DefCache.GetDef<GeoTimeElapsedGeoHintConditionDef>("E_MinTimeElapsedForCustomizationHint [GeoscapeHintsManagerDef]");
                geoTimeElapsedGeoHintConditionDef.TimeRangeInDays.Min = 14.0f;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void CreateStaminaHint()
        {
            try
            {
                string name = "TFTV_StaminaHintDef";
                ContextHelpHintDef sourceHint = DefCache.GetDef<ContextHelpHintDef>("TUT4_BodyPartDisabled_HintDef");
                ContextHelpHintDef staminaHint = Helper.CreateDefFromClone(sourceHint, "DE4949BA-D178-4036-9827-00A0E1C9BE5E", name);

                staminaHint.IsTutorialHint = false;
                HasSeenHintHintConditionDef sourceHasSeenHintConditionDef = DefCache.GetDef<HasSeenHintHintConditionDef>("HasSeenHint_TUT2_Overwatch_HintDef-False_HintConditionDef");

                HasSeenHintHintConditionDef newHasSeenHintConditionDef = Helper.CreateDefFromClone(sourceHasSeenHintConditionDef, "DC1E6A07-F1DA-47F4-875B-CA18144F56C4", name + "HasSeenHintConditionDef");
                newHasSeenHintConditionDef.HintDef = staminaHint;
                staminaHint.Conditions[1] = newHasSeenHintConditionDef;
                staminaHint.AnyCondition = false;
                staminaHint.Text.LocalizationKey = "TFTV_STAMINAHINT_TEXT";
                staminaHint.Title.LocalizationKey = "TFTV_STAMINAHINT_TITLE";

                ActorHasTagHintConditionDef sourceActorHasTagHintConditionDef = DefCache.GetDef<ActorHasTagHintConditionDef>("ActorHasTag_Takeshi_Tutorial3_GameTagDef_HintConditionDef");

                ActorHasTagHintConditionDef newActorHasTemplateHintConditionDef = Helper.CreateDefFromClone(sourceActorHasTagHintConditionDef, "3DC53C38-BB43-4F2B-9165-475F7CE2D237", "ActorHasTag_" + name + "_HintConditionDef");
                GameTagDef gameTagDef = DefCache.GetDef<GameTagDef>("PhoenixPoint_UniformTagDef");
                newActorHasTemplateHintConditionDef.GameTagDef = gameTagDef;
                staminaHint.Conditions.Add(newActorHasTemplateHintConditionDef);

                ContextHelpHintDbDef alwaysDisplayedTacticalHintsDbDef = DefCache.GetDef<ContextHelpHintDbDef>("AlwaysDisplayedTacticalHintsDbDef");
                alwaysDisplayedTacticalHintsDbDef.Hints.Add(staminaHint);

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
        public static void ChangeInfestationDefs()
        {
            try
            {
                AlienRaidsSetupDef raidsSetup = DefCache.GetDef<AlienRaidsSetupDef>("_AlienRaidsSetupDef");
                raidsSetup.RaidBands[0].RollResultMax = 60;
                raidsSetup.RaidBands[1].RollResultMax = 80;
                raidsSetup.RaidBands[2].RollResultMax = 100;
                raidsSetup.RaidBands[3].RollResultMax = 130;
                raidsSetup.RaidBands[4].RollResultMax = 9999;
                raidsSetup.RaidBands[4].AircraftTypesAllowed = 0;

                CustomMissionTypeDef Anu_Infestation = DefCache.GetDef<CustomMissionTypeDef>("HavenInfestationAN_CustomMissionTypeDef");
                CustomMissionTypeDef NewJericho_Infestation = DefCache.GetDef<CustomMissionTypeDef>("HavenInfestationSY_CustomMissionTypeDef");
                CustomMissionTypeDef Synderion_Infestation = DefCache.GetDef<CustomMissionTypeDef>("HavenInfestationNJ_CustomMissionTypeDef");

                ResourceMissionOutcomeDef sourceMissonResourceReward = DefCache.GetDef<ResourceMissionOutcomeDef>("HavenDefAN_ResourceMissionOutcomeDef");
                ResourceMissionOutcomeDef mutagenRewardInfestation = Helper.CreateDefFromClone(sourceMissonResourceReward, "2E579AB8-3744-4994-8036-B5018B5E2E15", "InfestationReward");
                mutagenRewardInfestation.Resources.Values.Clear();
                mutagenRewardInfestation.Resources.Values.Add(new ResourceUnit { Type = PhoenixPoint.Common.Core.ResourceType.Mutagen, Value = 800 });

                foreach (CustomMissionTypeDef missionTypeDef in Repo.GetAllDefs<CustomMissionTypeDef>())
                {
                    if (missionTypeDef.name.Contains("Haven") && missionTypeDef.name.Contains("Infestation"))
                    {
                        missionTypeDef.Outcomes[0].DestroySite = true;
                        missionTypeDef.Outcomes[0].Outcomes[2] = mutagenRewardInfestation;
                        missionTypeDef.Outcomes[0].BriefingModalBind.Title.LocalizationKey = "KEY_MISSION_HAVEN_INFESTED_VICTORY_NAME";
                        missionTypeDef.Outcomes[0].BriefingModalBind.Description.LocalizationKey = "KEY_MISSION_HAVEN_INFESTED_VICTORY_DESCRIPTION";
                        missionTypeDef.BriefingModalBind.Title.LocalizationKey = "KEY_MISSION_HAVEN_INFESTED_NAME";
                        missionTypeDef.BriefingModalBind.Description.LocalizationKey = "KEY_MISSION_HAVEN_INFESTED_DESCRIPTION";
                    }
                }

                //  GeoscapeEventDef rewardEvent = TFTVCommonMethods.CreateNewEvent("InfestationReward", "KEY_INFESTATION_REWARD_TITLE", "KEY_INFESTATION_REWARD_DESCRIPTION", null);
                GeoscapeEventDef lW1MissWin = DefCache.GetDef<GeoscapeEventDef>("PROG_LW1_WIN_GeoscapeEventDef");
                lW1MissWin.GeoscapeEventData.Choices[0].Outcome.SetEvents.Clear();
                lW1MissWin.GeoscapeEventData.Choices[0].Outcome.TrackEncounters.Clear();
                lW1MissWin.GeoscapeEventData.Choices[0].Outcome.UntrackEncounters.Clear();
                GeoscapeEventDef lW2MissWin = DefCache.GetDef<GeoscapeEventDef>("PROG_LW2_WIN_GeoscapeEventDef");
                lW2MissWin.GeoscapeEventData.Choices[0].Outcome.SetEvents.Clear();
                lW2MissWin.GeoscapeEventData.Choices[0].Outcome.TrackEncounters.Clear();
                lW2MissWin.GeoscapeEventData.Choices[0].Outcome.UntrackEncounters.Clear();
                GeoscapeEventDef lW3MissWin = DefCache.GetDef<GeoscapeEventDef>("PROG_LW3_WIN_GeoscapeEventDef");
                lW2MissWin.GeoscapeEventData.Choices[0].Outcome.SetEvents.Clear();
                lW2MissWin.GeoscapeEventData.Choices[0].Outcome.TrackEncounters.Clear();
                lW2MissWin.GeoscapeEventData.Choices[0].Outcome.UntrackEncounters.Clear();
                //Muting Living Weapons
                GeoscapeEventDef lwstartingEvent = DefCache.GetDef<GeoscapeEventDef>("PROG_LW1_GeoscapeEventDef");
                lwstartingEvent.GeoscapeEventData.Mute = true;
                DefCache.GetDef<KillActorFactionObjectiveDef>("KillCorruptionNode_CustomMissionObjective").MissionObjectiveData.ExperienceReward = 1000;


                //

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }
        }
        public static void ModifyMissionDefsToReplaceNeutralWithBandit()
        {
            try
            {
                PPFactionDef banditFaction = DefCache.GetDef<PPFactionDef>("NEU_Bandits_FactionDef");

                foreach (CustomMissionTypeDef missionTypeDef in Repo.GetAllDefs<CustomMissionTypeDef>())
                {
                    // TFTVLogger.Always("The first foreach went ok");


                    foreach (MutualParticipantsRelations relations in missionTypeDef.ParticipantsRelations)
                    {
                        // TFTVLogger.Always("The second foreach went ok");
                        if (relations.FirstParticipant == TacMissionParticipant.Player && relations.MutualRelation == FactionRelation.Enemy)
                        {
                            //   TFTVLogger.Always("The if inside the second foreach went ok");

                            if (missionTypeDef.ParticipantsData != null)
                            {
                                foreach (TacMissionTypeParticipantData data in missionTypeDef.ParticipantsData)
                                {
                                    //TFTVLogger.Always("The third foreach went Ok");

                                    if (data.ParticipantKind == relations.SecondParticipant)
                                    {
                                        // TFTVLogger.Always("The if inside the third foreach went ok");
                                        if (data.FactionDef != null)
                                        {
                                            if (missionTypeDef.name == "StoryAN1_CustomMissionTypeDef" ||
                                                missionTypeDef.name == "StoryNJ_Chain1_CustomMissionTypeDef" ||
                                                missionTypeDef.name == "StoryPX13_CustomMissionTypeDef" ||
                                                missionTypeDef.name == "StorySYN0_CustomMissionTypeDef" ||
                                                missionTypeDef.name == "StorySYN4_CustomMissionTypeDef" ||
                                                    missionTypeDef.name == "StorySYN5_CustomMissionTypeDef")
                                            {
                                                data.FactionDef = banditFaction;
                                                //  TFTVLogger.Always("In mission " + missionTypeDef.name + " the enemy faction is " + data.FactionDef.name);
                                            }
                                        }
                                    }
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
        public static void ChangesAmbushMissions()
        {
            try
            {
                //Changing ambush missions so that all of them have crates
                CustomMissionTypeDef AmbushALN = DefCache.GetDef<CustomMissionTypeDef>("AmbushAlien_CustomMissionTypeDef");
                CustomMissionTypeDef SourceScavCratesALN = DefCache.GetDef<CustomMissionTypeDef>("ScavCratesALN_CustomMissionTypeDef");
                var pickResourceCratesObjective = SourceScavCratesALN.CustomObjectives[2];
                AmbushALN.ParticipantsData[0].ReinforcementsTurns.Max = 2;
                AmbushALN.ParticipantsData[0].ReinforcementsTurns.Min = 2;
                AmbushALN.CratesDeploymentPointsRange = SourceScavCratesALN.CratesDeploymentPointsRange;
                AmbushALN.MissionSpecificCrates = SourceScavCratesALN.MissionSpecificCrates;
                AmbushALN.FactionItemsRange = SourceScavCratesALN.FactionItemsRange;
                AmbushALN.CratesDeploymentPointsRange.Min = 30;
                AmbushALN.CratesDeploymentPointsRange.Max = 50;
                AmbushALN.CustomObjectives[2] = pickResourceCratesObjective;

                CustomMissionTypeDef AmbushAN = DefCache.GetDef<CustomMissionTypeDef>("AmbushAN_CustomMissionTypeDef");
                AmbushAN.ParticipantsData[0].ReinforcementsTurns.Max = 2;
                AmbushAN.ParticipantsData[0].ReinforcementsTurns.Min = 2;
                AmbushAN.CratesDeploymentPointsRange = SourceScavCratesALN.CratesDeploymentPointsRange;
                AmbushAN.MissionSpecificCrates = SourceScavCratesALN.MissionSpecificCrates;
                AmbushAN.FactionItemsRange = SourceScavCratesALN.FactionItemsRange;
                AmbushAN.CratesDeploymentPointsRange.Min = 30;
                AmbushAN.CratesDeploymentPointsRange.Max = 50;
                AmbushAN.CustomObjectives[2] = pickResourceCratesObjective;

                CustomMissionTypeDef AmbushBandits = DefCache.GetDef<CustomMissionTypeDef>("AmbushBandits_CustomMissionTypeDef");
                AmbushBandits.ParticipantsData[0].ReinforcementsTurns.Max = 2;
                AmbushBandits.ParticipantsData[0].ReinforcementsTurns.Min = 2;
                AmbushBandits.CratesDeploymentPointsRange = SourceScavCratesALN.CratesDeploymentPointsRange;
                AmbushBandits.MissionSpecificCrates = SourceScavCratesALN.MissionSpecificCrates;
                AmbushBandits.FactionItemsRange = SourceScavCratesALN.FactionItemsRange;
                AmbushBandits.CratesDeploymentPointsRange.Min = 30;
                AmbushBandits.CratesDeploymentPointsRange.Max = 50;
                AmbushBandits.CustomObjectives[2] = pickResourceCratesObjective;

                CustomMissionTypeDef AmbushFallen = DefCache.GetDef<CustomMissionTypeDef>("AmbushFallen_CustomMissionTypeDef");
                AmbushFallen.ParticipantsData[0].ReinforcementsTurns.Max = 2;
                AmbushFallen.ParticipantsData[0].ReinforcementsTurns.Min = 2;
                AmbushFallen.CratesDeploymentPointsRange = SourceScavCratesALN.CratesDeploymentPointsRange;
                AmbushFallen.MissionSpecificCrates = SourceScavCratesALN.MissionSpecificCrates;
                AmbushFallen.FactionItemsRange = SourceScavCratesALN.FactionItemsRange;
                AmbushFallen.CratesDeploymentPointsRange.Min = 30;
                AmbushFallen.CratesDeploymentPointsRange.Max = 50;
                AmbushFallen.CustomObjectives[2] = pickResourceCratesObjective;

                CustomMissionTypeDef AmbushNJ = DefCache.GetDef<CustomMissionTypeDef>("AmbushNJ_CustomMissionTypeDef");
                AmbushNJ.ParticipantsData[0].ReinforcementsTurns.Max = 2;
                AmbushNJ.ParticipantsData[0].ReinforcementsTurns.Min = 2;
                AmbushNJ.CratesDeploymentPointsRange = SourceScavCratesALN.CratesDeploymentPointsRange;
                AmbushNJ.MissionSpecificCrates = SourceScavCratesALN.MissionSpecificCrates;
                AmbushNJ.FactionItemsRange = SourceScavCratesALN.FactionItemsRange;
                AmbushNJ.CratesDeploymentPointsRange.Min = 30;
                AmbushNJ.CratesDeploymentPointsRange.Max = 50;
                AmbushNJ.CustomObjectives[2] = pickResourceCratesObjective;

                CustomMissionTypeDef AmbushPure = DefCache.GetDef<CustomMissionTypeDef>("AmbushPure_CustomMissionTypeDef");
                AmbushPure.ParticipantsData[0].ReinforcementsTurns.Max = 2;
                AmbushPure.ParticipantsData[0].ReinforcementsTurns.Min = 2;
                AmbushPure.CratesDeploymentPointsRange = SourceScavCratesALN.CratesDeploymentPointsRange;
                AmbushPure.MissionSpecificCrates = SourceScavCratesALN.MissionSpecificCrates;
                AmbushPure.FactionItemsRange = SourceScavCratesALN.FactionItemsRange;
                AmbushPure.CratesDeploymentPointsRange.Min = 30;
                AmbushPure.CratesDeploymentPointsRange.Max = 50;
                AmbushPure.CustomObjectives[2] = pickResourceCratesObjective;

                CustomMissionTypeDef AmbushSY = DefCache.GetDef<CustomMissionTypeDef>("AmbushSY_CustomMissionTypeDef");
                AmbushSY.ParticipantsData[0].ReinforcementsTurns.Max = 2;
                AmbushSY.ParticipantsData[0].ReinforcementsTurns.Min = 2;
                AmbushSY.CratesDeploymentPointsRange = SourceScavCratesALN.CratesDeploymentPointsRange;
                AmbushSY.MissionSpecificCrates = SourceScavCratesALN.MissionSpecificCrates;
                AmbushSY.FactionItemsRange = SourceScavCratesALN.FactionItemsRange;
                AmbushSY.CratesDeploymentPointsRange.Min = 30;
                AmbushSY.CratesDeploymentPointsRange.Max = 50;
                AmbushSY.CustomObjectives[2] = pickResourceCratesObjective;

                //Reduce XP for Ambush mission
                SurviveTurnsFactionObjectiveDef surviveAmbush_CustomMissionObjective = DefCache.GetDef<SurviveTurnsFactionObjectiveDef>("SurviveAmbush_CustomMissionObjective");
                surviveAmbush_CustomMissionObjective.MissionObjectiveData.ExperienceReward = 100;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
        public static void ModifyDefsForPassengerModules()
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
                //Change Cruise Control module
                GeoVehicleModuleDef cruisecontrolmodule = DefCache.GetDef<GeoVehicleModuleDef>("NJ_CruiseControl_GeoVehicleModuleDef");
                //Increase cost to 50% of Vanilla Manti
                cruisecontrolmodule.ManufactureMaterials = 600;
                cruisecontrolmodule.ManufactureTech = 75;
                cruisecontrolmodule.ManufacturePointsCost = 505;
                //increasing bonus to speed 
                cruisecontrolmodule.GeoVehicleModuleBonusValue = 250;
                //Change Fuel Tank module
                GeoVehicleModuleDef fueltankmodule = DefCache.GetDef<GeoVehicleModuleDef>("NJ_FuelTanks_GeoVehicleModuleDef");
                //Increase cost to 50% of Vanilla Manti
                fueltankmodule.ManufactureMaterials = 600;
                fueltankmodule.ManufactureTech = 75;
                fueltankmodule.ManufacturePointsCost = 505;
                fueltankmodule.GeoVehicleModuleBonusValue = 2500;


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
        public static void ModifyPandoranProgress()
        {

            try
            {

                // All sources of evolution due to scaling removed, leaving only evolution per day
                // Additional source of evolution will be number of surviving Pandoran colonies, modulated by difficulty level
                GameDifficultyLevelDef veryhard = DefCache.GetDef<GameDifficultyLevelDef>("VeryHard_GameDifficultyLevelDef");
                //Hero
                GameDifficultyLevelDef hard = DefCache.GetDef<GameDifficultyLevelDef>("Hard_GameDifficultyLevelDef");
                //Standard
                GameDifficultyLevelDef standard = DefCache.GetDef<GameDifficultyLevelDef>("Standard_GameDifficultyLevelDef");
                //Easy
                GameDifficultyLevelDef easy = DefCache.GetDef<GameDifficultyLevelDef>("Easy_GameDifficultyLevelDef");

                veryhard.NestLimitations.MaxNumber = 3; //vanilla 6
                veryhard.NestLimitations.HoursBuildTime = 90; //vanilla 45
                veryhard.LairLimitations.MaxNumber = 3; // vanilla 5
                veryhard.LairLimitations.MaxConcurrent = 3; //vanilla 4
                veryhard.LairLimitations.HoursBuildTime = 100; //vanilla 50
                veryhard.CitadelLimitations.HoursBuildTime = 180; //vanilla 60
                veryhard.EvolutionPointsGainOnMissionLoss = 0; //vanilla 10
                veryhard.AlienBaseTypeEvolutionParams[0].EvolutionPerDestroyedBase = 0; //vanilla 10
                veryhard.AlienBaseTypeEvolutionParams[1].EvolutionPerDestroyedBase = 0; //vanilla 20
                veryhard.AlienBaseTypeEvolutionParams[2].EvolutionPerDestroyedBase = 0; //vanilla 40
                veryhard.ApplyInfestationOutcomeChange = 0;
                veryhard.ApplyDamageHavenOutcomeChange = 0;
                veryhard.StartingSquadTemplate[0] = hard.TutorialStartingSquadTemplate[1];
                veryhard.StartingSquadTemplate[1] = hard.TutorialStartingSquadTemplate[2];

                //Making HPC residual
                veryhard.MinPopulationThreshold = 3;
                hard.MinPopulationThreshold = 3;
                standard.MinPopulationThreshold = 3;
                easy.MinPopulationThreshold = 3;


                veryhard.RecruitCostPerLevelMultiplier = 0.4f;


                // PX_Jacob_Tutorial2_TacCharacterDef replace [3], with hard starting squad [1]
                // PX_Sophia_Tutorial2_TacCharacterDef replace [1], with hard starting squad [2]

                //reducing evolution per day because there other sources of evolution points now
                veryhard.EvolutionProgressPerDay = 70; //vanilla 100



                hard.NestLimitations.MaxNumber = 3; //vanilla 5
                hard.NestLimitations.HoursBuildTime = 90; //vanilla 50
                hard.LairLimitations.MaxNumber = 3; // vanilla 4
                hard.LairLimitations.MaxConcurrent = 3; //vanilla 3
                hard.LairLimitations.HoursBuildTime = 100; //vanilla 80
                hard.CitadelLimitations.HoursBuildTime = 180; //vanilla 100
                hard.EvolutionPointsGainOnMissionLoss = 0; //vanilla 10
                hard.AlienBaseTypeEvolutionParams[0].EvolutionPerDestroyedBase = 0; //vanilla 10
                hard.AlienBaseTypeEvolutionParams[1].EvolutionPerDestroyedBase = 0; //vanilla 20
                hard.AlienBaseTypeEvolutionParams[2].EvolutionPerDestroyedBase = 0; //vanilla 40
                hard.ApplyInfestationOutcomeChange = 0;
                hard.ApplyDamageHavenOutcomeChange = 0;
                hard.StartingSquadTemplate[1] = hard.TutorialStartingSquadTemplate[1];
                hard.StartingSquadTemplate[3] = hard.TutorialStartingSquadTemplate[2];

                hard.RecruitCostPerLevelMultiplier = 0.3f;

                //reducing evolution per day because there other sources of evolution points now
                hard.EvolutionProgressPerDay = 50; //vanilla 70; moved from 60 in Update#6


                standard.NestLimitations.MaxNumber = 3; //vanilla 4
                standard.NestLimitations.HoursBuildTime = 90; //vanilla 55
                standard.LairLimitations.MaxNumber = 3; // vanilla 3
                standard.LairLimitations.MaxConcurrent = 3; //vanilla 3
                standard.LairLimitations.HoursBuildTime = 100; //vanilla 120
                standard.CitadelLimitations.HoursBuildTime = 180; //vanilla 145
                standard.EvolutionPointsGainOnMissionLoss = 0; //vanilla 10
                standard.AlienBaseTypeEvolutionParams[0].EvolutionPerDestroyedBase = 0; //vanilla 10
                standard.AlienBaseTypeEvolutionParams[1].EvolutionPerDestroyedBase = 0; //vanilla 20
                standard.AlienBaseTypeEvolutionParams[2].EvolutionPerDestroyedBase = 0; //vanilla 40
                standard.ApplyDamageHavenOutcomeChange = 0;
                standard.ApplyInfestationOutcomeChange = 0;
                standard.StartingSquadTemplate[1] = hard.TutorialStartingSquadTemplate[1];
                standard.StartingSquadTemplate[3] = hard.TutorialStartingSquadTemplate[2];

                //reducing evolution per day because there other sources of evolution points now
                standard.EvolutionProgressPerDay = 40; //vanilla 55


                easy.NestLimitations.HoursBuildTime = 90; //vanilla 60 
                easy.LairLimitations.HoursBuildTime = 100; // vanilla 150
                easy.CitadelLimitations.HoursBuildTime = 180; // vanilla 180
                easy.EvolutionPointsGainOnMissionLoss = 0; //vanilla 10
                easy.AlienBaseTypeEvolutionParams[0].EvolutionPerDestroyedBase = 0; //vanilla 10
                easy.AlienBaseTypeEvolutionParams[1].EvolutionPerDestroyedBase = 0; //vanilla 20
                easy.AlienBaseTypeEvolutionParams[2].EvolutionPerDestroyedBase = 0; //vanilla 40
                easy.ApplyInfestationOutcomeChange = 0;
                easy.ApplyDamageHavenOutcomeChange = 0;
                easy.StartingSquadTemplate[1] = hard.TutorialStartingSquadTemplate[1];
                easy.StartingSquadTemplate[3] = hard.TutorialStartingSquadTemplate[2];

                //keeping evolution per day because low enough already
                easy.EvolutionProgressPerDay = 35; //vanilla 35

                //Remove faction diplo penalties for not destroying revealed PCs and increase rewards for haven leader
                GeoAlienBaseTypeDef nestType = DefCache.GetDef<GeoAlienBaseTypeDef>("Nest_GeoAlienBaseTypeDef");
                GeoAlienBaseTypeDef lairType = DefCache.GetDef<GeoAlienBaseTypeDef>("Lair_GeoAlienBaseTypeDef");
                GeoAlienBaseTypeDef citadelType = DefCache.GetDef<GeoAlienBaseTypeDef>("Citadel_GeoAlienBaseTypeDef");
                GeoAlienBaseTypeDef palaceType = DefCache.GetDef<GeoAlienBaseTypeDef>("Palace_GeoAlienBaseTypeDef");

                nestType.FactionDiplomacyPenaltyPerHaven = 0; //vanilla -1
                nestType.HavenLeaderDiplomacyReward = 12; //vanilla 8 
                lairType.FactionDiplomacyPenaltyPerHaven = 0; //vanilla -1
                lairType.HavenLeaderDiplomacyReward = 16; //vanilla 12 
                citadelType.FactionDiplomacyPenaltyPerHaven = 0; //vanilla -1
                citadelType.HavenLeaderDiplomacyReward = 20; //vanilla 16 

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void InjectAlistairAhsbyLines()
        {
            try
            {
                //Alistair speaks about Symes after completing Symes Retreat
                GeoscapeEventDef alistairOnSymes1 = TFTVCommonMethods.CreateNewEvent("AlistairOnSymes1", "PROG_PX10_WIN_TITLE", "KEY_ALISTAIRONSYMES_1_DESCRIPTION", null);
                alistairOnSymes1.GeoscapeEventData.Flavour = "IntroducingSymes";

                //Alistair speaks about Barnabas after Barnabas asks for help
                GeoscapeEventDef alistairOnBarnabas = TFTVCommonMethods.CreateNewEvent("AlistairOnBarnabas", "PROG_CH0_TITLE", "KEY_ALISTAIRONBARNABAS_DESCRIPTION", null);
                alistairOnBarnabas.GeoscapeEventData.Flavour = "DLC4_Generic_NJ";

                //Alistair speaks about Symes after Antarctica discovery
                GeoscapeEventDef alistairOnSymes2 = TFTVCommonMethods.CreateNewEvent("AlistairOnSymes2", "PROG_PX1_WIN_TITLE", "KEY_ALISTAIRONSYMES_2_DESCRIPTION", null);
                alistairOnSymes2.GeoscapeEventData.Flavour = "AntarcticSite_Victory";

                AlistairRoadsEvent();
                CreateEventMessagesFromTheVoid();
                CreateBehemothPattern();
                CreateTrappedInMist();

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void InjectOlenaKimLines()

        {
            try
            {
                //Helena reveal about Olena
                GeoscapeEventDef helenaOnOlena = TFTVCommonMethods.CreateNewEvent("HelenaOnOlena", "PROG_LE0_WIN_TITLE", "KEY_OLENA_HELENA_DESCRIPTION", null);
                //Olena about West
                GeoscapeEventDef olenaOnWest = TFTVCommonMethods.CreateNewEvent("OlenaOnWest", "PROG_NJ1_WIN_TITLE", "KEY_OLENAONWEST_DESCRIPTION", null);
                //Olena about Synod
                GeoscapeEventDef olenaOnSynod = TFTVCommonMethods.CreateNewEvent("OlenaOnSynod", "PROG_AN6_WIN2_TITLE", "KEY_OLENAONSYNOD_DESCRIPTION", null);
                //Olena about the Ancients
                GeoscapeEventDef olenaOnAncients = TFTVCommonMethods.CreateNewEvent("OlenaOnAncients", "KEY_OLENAONANCIENTS_TITLE", "KEY_OLENAONANCIENTS_DESCRIPTION", null);
                //Olena about the Behemeoth
                GeoscapeEventDef olenaOnBehemoth = TFTVCommonMethods.CreateNewEvent("OlenaOnBehemoth", "PROG_FS1_WIN_TITLE", "KEY_OLENAONBEHEMOTH_DESCRIPTION", null);
                //Olena about Alistair - missing an event hook!!
                GeoscapeEventDef olenaOnAlistair = TFTVCommonMethods.CreateNewEvent("OlenaOnAlistair", "", "KEY_OLENAONALISTAIR_DESCRIPTION", null);
                //Olena about Symes
                GeoscapeEventDef olenaOnSymes = TFTVCommonMethods.CreateNewEvent("OlenaOnSymes", "PROG_PX1_WIN_TITLE", "KEY_OLENAONSYMES_DESCRIPTION", null);
                //Olena about ending 
                GeoscapeEventDef olenaOnEnding = TFTVCommonMethods.CreateNewEvent("OlenaOnEnding", "KEY_ALISTAIR_ROADS_TITLE", "KEY_OLENAONENDING_DESCRIPTION", null);
                //Olena about Bionics Lab sabotage
                GeoscapeEventDef olenaOnBionicsLabSabotage = TFTVCommonMethods.CreateNewEvent("OlenaOnBionicsLabSabotage", "ANU_REALLY_PISSED_BIONICS_TITLE", "ANU_REALLY_PISSED_BIONICS_CHOICE_0_OUTCOME", null);
                //Olena about Mutations Lab sabotage
                GeoscapeEventDef olenaOnMutationsLabSabotage = TFTVCommonMethods.CreateNewEvent("OlenaOnMutationsLabSabotage", "NJ_REALLY_PISSED_MUTATIONS_TITLE", "NJ_REALLY_PISSED_MUTATIONS_CHOICE_0_OUTCOME", null);
                //Olena First LOTA Event 
                TFTVCommonMethods.CreateNewEvent("OlenaLotaStart", "TFTV_LOTA_START_EVENT_TITLE", "TFTV_LOTA_START_EVENT_DESCRIPTION", null);

                CreateEventFirstFlyer();
                CreateEventFirstHavenTarget();
                CreateEventFirstHavenAttack();

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void AlistairRoadsEvent()
        {
            try
            {
                string title = "KEY_ALISTAIR_ROADS_TITLE";
                string description = "KEY_ALISTAIR_ROADS_DESCRIPTION";
                string passToOlena = "OlenaOnEnding";

                string startingEvent = "AlistairRoads";
                string afterWest = "AlistairRoadsNoWest";
                string afterSynedrion = "AlistairRoadsNoSynedrion";
                string afterAnu = "AlistairRoadsNoAnu";
                string afterVirophage = "AlistairRoadsNoVirophage";

                string questionAboutWest = "KEY_ALISTAIRONWEST_CHOICE";
                string questionAboutSynedrion = "KEY_ALISTAIRONSYNEDRION_CHOICE";
                string questionAboutAnu = "KEY_ALISTAIRONANU_CHOICE";
                string questionAboutVirophage = "KEY_ALISTAIRONVIROPHAGE_CHOICE";
                //   string questionAboutHelena = "KEY_ALISTAIRONHELENA_CHOICE";
                string noMoreQuestions = "KEY_ALISTAIR_ROADS_ALLDONE";

                string answerAboutWest = "KEY_ALISTAIRONWEST_DESCRIPTION";
                string answerAboutSynedrion = "KEY_ALISTAIRONSYNEDRION_DESCRIPTION";
                string answerAboutAnu = "KEY_ALISTAIRONANU_DESCRIPTION";
                string answerAboutVirophage = "KEY_ALISTAIRONVIROPHAGE_DESCRIPTION";
                //   string answerAboutHelena = "KEY_ALISTAIRONHELENA_DESCRIPTION";
                string promptMoreQuestions = "KEY_ALISTAIR_ROADS_DESCRIPTION_2";

                GeoscapeEventDef alistairRoads = TFTVCommonMethods.CreateNewEvent(startingEvent, title, description, null);
                GeoscapeEventDef alistairRoadsAfterWest = TFTVCommonMethods.CreateNewEvent(afterWest, title, promptMoreQuestions, null);
                GeoscapeEventDef alistairRoadsAfterSynedrion = TFTVCommonMethods.CreateNewEvent(afterSynedrion, title, promptMoreQuestions, null);
                GeoscapeEventDef alistairRoadsAfterAnu = TFTVCommonMethods.CreateNewEvent(afterAnu, title, promptMoreQuestions, null);
                GeoscapeEventDef alistairRoadsAfterVirophage = TFTVCommonMethods.CreateNewEvent(afterVirophage, title, promptMoreQuestions, null);

                alistairRoads.GeoscapeEventData.Choices[0].Text.LocalizationKey = noMoreQuestions;
                alistairRoads.GeoscapeEventData.Choices[0].Outcome.TriggerEncounterID = passToOlena;
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoads, questionAboutWest, answerAboutWest);
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoads, questionAboutSynedrion, answerAboutSynedrion);
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoads, questionAboutAnu, answerAboutAnu);

                alistairRoadsAfterWest.GeoscapeEventData.Choices[0].Text.LocalizationKey = noMoreQuestions;
                alistairRoadsAfterWest.GeoscapeEventData.Choices[0].Outcome.TriggerEncounterID = passToOlena;
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoadsAfterWest, questionAboutSynedrion, answerAboutSynedrion);
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoadsAfterWest, questionAboutAnu, answerAboutAnu);
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoadsAfterWest, questionAboutVirophage, answerAboutVirophage);

                alistairRoadsAfterSynedrion.GeoscapeEventData.Choices[0].Text.LocalizationKey = noMoreQuestions;
                alistairRoadsAfterSynedrion.GeoscapeEventData.Choices[0].Outcome.TriggerEncounterID = passToOlena;
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoadsAfterSynedrion, questionAboutWest, answerAboutWest);
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoadsAfterSynedrion, questionAboutAnu, answerAboutAnu);
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoadsAfterSynedrion, questionAboutVirophage, answerAboutVirophage);

                alistairRoadsAfterAnu.GeoscapeEventData.Choices[0].Text.LocalizationKey = noMoreQuestions;
                alistairRoadsAfterAnu.GeoscapeEventData.Choices[0].Outcome.TriggerEncounterID = passToOlena;
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoadsAfterAnu, questionAboutWest, answerAboutWest);
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoadsAfterAnu, questionAboutSynedrion, answerAboutSynedrion);
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoadsAfterAnu, questionAboutVirophage, answerAboutVirophage);

                alistairRoadsAfterVirophage.GeoscapeEventData.Choices[0].Text.LocalizationKey = noMoreQuestions;
                alistairRoadsAfterVirophage.GeoscapeEventData.Choices[0].Outcome.TriggerEncounterID = passToOlena;
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoadsAfterVirophage, questionAboutWest, answerAboutWest);
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoadsAfterVirophage, questionAboutSynedrion, answerAboutSynedrion);
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoadsAfterVirophage, questionAboutAnu, answerAboutAnu);


                alistairRoads.GeoscapeEventData.Choices[1].Outcome.TriggerEncounterID = afterWest;
                alistairRoads.GeoscapeEventData.Choices[2].Outcome.TriggerEncounterID = afterSynedrion;
                alistairRoads.GeoscapeEventData.Choices[3].Outcome.TriggerEncounterID = afterAnu;

                alistairRoadsAfterWest.GeoscapeEventData.Choices[1].Outcome.TriggerEncounterID = afterSynedrion;
                alistairRoadsAfterWest.GeoscapeEventData.Choices[2].Outcome.TriggerEncounterID = afterAnu;
                alistairRoadsAfterWest.GeoscapeEventData.Choices[3].Outcome.TriggerEncounterID = afterVirophage;

                alistairRoadsAfterSynedrion.GeoscapeEventData.Choices[1].Outcome.TriggerEncounterID = afterWest;
                alistairRoadsAfterSynedrion.GeoscapeEventData.Choices[2].Outcome.TriggerEncounterID = afterAnu;
                alistairRoadsAfterSynedrion.GeoscapeEventData.Choices[3].Outcome.TriggerEncounterID = afterVirophage;

                alistairRoadsAfterAnu.GeoscapeEventData.Choices[1].Outcome.TriggerEncounterID = afterWest;
                alistairRoadsAfterAnu.GeoscapeEventData.Choices[2].Outcome.TriggerEncounterID = afterSynedrion;
                alistairRoadsAfterAnu.GeoscapeEventData.Choices[3].Outcome.TriggerEncounterID = afterVirophage;

                alistairRoadsAfterVirophage.GeoscapeEventData.Choices[1].Outcome.TriggerEncounterID = afterWest;
                alistairRoadsAfterVirophage.GeoscapeEventData.Choices[2].Outcome.TriggerEncounterID = afterSynedrion;
                alistairRoadsAfterVirophage.GeoscapeEventData.Choices[3].Outcome.TriggerEncounterID = afterAnu;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void CreateIntro()
        {
            try
            {
                string introEvent_0 = "IntroBetterGeo_0";
                string introEvent_1 = "IntroBetterGeo_1";
                string introEvent_2 = "IntroBetterGeo_2";
                GeoscapeEventDef intro0 = TFTVCommonMethods.CreateNewEvent(
                    introEvent_0, "BG_INTRO_0_TITLE", "BG_INTRO_0_DESCRIPTION", null);
                GeoscapeEventDef intro1 = TFTVCommonMethods.CreateNewEvent(
                    introEvent_1, "BG_INTRO_1_TITLE", "BG_INTRO_1_DESCRIPTION", null);
                intro1.GeoscapeEventData.Choices[0].Text.LocalizationKey = "BG_INTRO1_CHOICE_1";
                GeoscapeEventDef intro2 = TFTVCommonMethods.CreateNewEvent(
                    introEvent_2, "BG_INTRO_2_TITLE", "BG_INTRO_2_DESCRIPTION", null);
                intro2.GeoscapeEventData.Choices[0].Text.LocalizationKey = "BG_INTRO_2_CHOICE_0";


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

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
                //  ppResearchDB.Researches.Remove(DefCache.GetDef<ResearchDef>("PX_Aircraft_EscapePods_ResearchDef"));
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

        public static void AddItemToManufacturingReward(string researchReward, string reward, string research)
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
                    anuResearchDB.Researches.Remove(researchDef);
                }
                if (synResearchDB.Researches.Contains(researchDef))
                {
                    anuResearchDB.Researches.Remove(researchDef);
                }


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

        public static void RemoveCorruptionDamageBuff()
        {
            try
            {
                CorruptionStatusDef corruption_StatusDef = DefCache.GetDef<CorruptionStatusDef>("Corruption_StatusDef");
                corruption_StatusDef.Multiplier = 0.0f;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void ChangesToMedbay()
        {
            try
            {
                HealFacilityComponentDef e_HealMedicalBay_PhoenixFacilityDe = DefCache.GetDef<HealFacilityComponentDef>("E_Heal [MedicalBay_PhoenixFacilityDef]");
                e_HealMedicalBay_PhoenixFacilityDe.BaseHeal = 16;
                PhoenixFacilityDef medbay = DefCache.GetDef<PhoenixFacilityDef>("MedicalBay_PhoenixFacilityDef");
                medbay.ConstructionTimeDays = 1.5f;
                medbay.ResourceCost = new ResourcePack
                {
                    new ResourceUnit { Type = ResourceType.Materials, Value = 200 },
                    new ResourceUnit { Type = ResourceType.Tech, Value = 50 }
                };

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void MistOnAllMissions()
        {
            try
            {
                foreach (CustomMissionTypeDef missionTypeDef in Repo.GetAllDefs<CustomMissionTypeDef>())
                {
                    missionTypeDef.SpawnMistAtLevelStart = true;
                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        internal static void CreateUmbraImmunities()
        {
            try
            {
                AbilityDef paralysisImmunity = DefCache.GetDef<AbilityDef>("ParalysisNotShockImmunity_DamageMultiplierAbilityDef");
                AbilityDef poisonImmunity = DefCache.GetDef<AbilityDef>("PoisonImmunity_DamageMultiplierAbilityDef");
                AbilityDef psychicResistance = DefCache.GetDef<AbilityDef>("PsychicResistant_DamageMultiplierAbilityDef");

                List<AbilityDef> abilityDefs = new List<AbilityDef>() { paralysisImmunity, poisonImmunity, psychicResistance };

                TacticalActorDef oilcrabDef = DefCache.GetDef<TacticalActorDef>("Oilcrab_ActorDef");

                List<AbilityDef> ocAbilities = new List<AbilityDef>(oilcrabDef.Abilities);
                ocAbilities.AddRange(abilityDefs);
                oilcrabDef.Abilities = ocAbilities.ToArray();

                TacticalActorDef oilfishDef = DefCache.GetDef<TacticalActorDef>("Oilfish_ActorDef");
                List<AbilityDef> ofAbilities = new List<AbilityDef>(oilfishDef.Abilities);
                ofAbilities.AddRange(abilityDefs);
                oilfishDef.Abilities = ofAbilities.ToArray();
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        public static void ChangeUmbra()

        {
            try
            {
                RandomValueEffectConditionDef randomValueFishUmbra = DefCache.GetDef<RandomValueEffectConditionDef>("E_RandomValue [UmbralFishmen_FactionEffectDef]");
                RandomValueEffectConditionDef randomValueCrabUmbra = DefCache.GetDef<RandomValueEffectConditionDef>("E_RandomValue [UmbralCrabmen_FactionEffectDef]");
                randomValueCrabUmbra.ThresholdValue = 0;
                randomValueFishUmbra.ThresholdValue = 0;
                EncounterVariableResearchRequirementDef sourceVarResReq =
                   DefCache.GetDef<EncounterVariableResearchRequirementDef>("NJ_Bionics1_ResearchDef_EncounterVariableResearchRequirementDef_0");
                //Changing Umbra Crab and Triton to appear after SDI event 3;
                ResearchDef umbraCrabResearch = DefCache.GetDef<ResearchDef>("ALN_CrabmanUmbra_ResearchDef");

                //Creating new Research Requirement, requiring a variable to be triggered  
                string variableUmbraALNResReq = "Umbra_Encounter_Variable";
                EncounterVariableResearchRequirementDef variableResReqUmbra = Helper.CreateDefFromClone(sourceVarResReq, "0CCC30E0-4DB1-44CD-9A60-C1C8F6588C8A", "UmbraResReqDef");
                variableResReqUmbra.VariableName = variableUmbraALNResReq;
                // This changes the Umbra reserach so that 2 conditions have to be fulfilled: 1) a) nest has to be researched, or b) exotic material has to be found
                // (because 1)a) is fufilled at start of the game, b) is redundant but harmless), and 2) a special variable has to be triggered, assigned to event sdi3
                umbraCrabResearch.RevealRequirements.Operation = ResearchContainerOperation.ALL;
                umbraCrabResearch.RevealRequirements.Container[0].Operation = ResearchContainerOperation.ANY;
                umbraCrabResearch.RevealRequirements.Container[1].Requirements[0] = variableResReqUmbra;
                //Now same thing for Triton Umbra, but it will use same variable because we want them to appear at the same time
                ResearchDef umbraFishResearch = DefCache.GetDef<ResearchDef>("ALN_FishmanUmbra_ResearchDef");
                umbraFishResearch.RevealRequirements.Operation = ResearchContainerOperation.ALL;
                umbraFishResearch.RevealRequirements.Container[0].Operation = ResearchContainerOperation.ANY;
                umbraFishResearch.RevealRequirements.Container[1].Requirements[0] = variableResReqUmbra;
                //Because Triton research has 2 requirements in the second container, we set them to any
                umbraFishResearch.RevealRequirements.Container[1].Operation = ResearchContainerOperation.ANY;

                ViewElementDef oilCrabViewElementDef = DefCache.GetDef<ViewElementDef>("E_View [Oilcrab_Torso_BodyPartDef]");
                oilCrabViewElementDef.DisplayName1.LocalizationKey = "TFTV_KEY_UMBRA_TARGET_DISPLAY_NAME";
                oilCrabViewElementDef.Description.LocalizationKey = "TFTV_KEY_UMBRA_TARGET_DISPLAY_DESCRIPTION";
                oilCrabViewElementDef.SmallIcon = UmbraIcon;
                oilCrabViewElementDef.LargeIcon = UmbraIcon;
                oilCrabViewElementDef.InventoryIcon = UmbraIcon;

                ViewElementDef oilFishViewElementDef = DefCache.GetDef<ViewElementDef>("E_View [Oilfish_Torso_BodyPartDef]");
                oilFishViewElementDef.DisplayName1.LocalizationKey = "TFTV_KEY_UMBRA_TARGET_DISPLAY_NAME";
                oilFishViewElementDef.Description.LocalizationKey = "TFTV_KEY_UMBRA_TARGET_DISPLAY_DESCRIPTION";
                oilFishViewElementDef.SmallIcon = UmbraIcon;
                oilFishViewElementDef.LargeIcon = UmbraIcon;
                oilFishViewElementDef.InventoryIcon = UmbraIcon;

                TacticalPerceptionDef oilCrabPerceptionDef = DefCache.GetDef<TacticalPerceptionDef>("Oilcrab_PerceptionDef");
                TacticalPerceptionDef oilFishPerceptionDef = DefCache.GetDef<TacticalPerceptionDef>("Oilfish_PerceptionDef");
                oilCrabPerceptionDef.PerceptionRange = 30.0f;
                oilFishPerceptionDef.PerceptionRange = 30.0f;
                //
                AddAbilityStatusDef oilTritonAddAbilityStatus = DefCache.GetDef<AddAbilityStatusDef>("OilFish_AddAbilityStatusDef");
                oilTritonAddAbilityStatus.ApplicationConditions = new EffectConditionDef[] { };
                AddAbilityStatusDef oilCrabAddAbilityStatus = DefCache.GetDef<AddAbilityStatusDef>("OilCrab_AddAbilityStatusDef");
                oilCrabAddAbilityStatus.ApplicationConditions = new EffectConditionDef[] { };

                CreateUmbraImmunities();

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }
        public static void Create_VoidOmen_Events()

        {
            TFTVCommonMethods.CreateNewEvent("VoidOmen", "", "", null);
            TFTVCommonMethods.CreateNewEvent("VoidOmenIntro", "", "", null);

        }

        public static void AugmentationEventsDefs()
        {
            try
            {
                //ID all the factions for later
                GeoFactionDef phoenixPoint = DefCache.GetDef<GeoFactionDef>("Phoenix_GeoPhoenixFactionDef");
                GeoFactionDef newJericho = DefCache.GetDef<GeoFactionDef>("NewJericho_GeoFactionDef");
                GeoFactionDef anu = DefCache.GetDef<GeoFactionDef>("Anu_GeoFactionDef");
                GeoFactionDef synedrion = DefCache.GetDef<GeoFactionDef>("Synedrion_GeoFactionDef");

                //Anu pissed at player for doing Bionics
                GeoscapeEventDef anuPissedAtBionics = TFTVCommonMethods.CreateNewEvent("Anu_Pissed1", "ANU_PISSED_BIONICS_TITLE", "ANU_PISSED_BIONICS_TEXT_GENERAL_0", "ANU_PISSED_BIONICS_CHOICE_0_OUTCOME");
                anuPissedAtBionics.GeoscapeEventData.Leader = "AN_Synod";

                anuPissedAtBionics.GeoscapeEventData.Choices[0].Text.LocalizationKey = "ANU_PISSED_BIONICS_CHOICE_0";

                anuPissedAtBionics.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(anu, phoenixPoint, -8));
                anuPissedAtBionics.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(synedrion, phoenixPoint, +2));
                TFTVCommonMethods.GenerateGeoEventChoice(anuPissedAtBionics, "ANU_PISSED_BIONICS_CHOICE_1", "ANU_PISSED_BIONICS_CHOICE_1_OUTCOME");
                anuPissedAtBionics.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(anu, phoenixPoint, -8));
                anuPissedAtBionics.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(newJericho, phoenixPoint, +2));
                TFTVCommonMethods.GenerateGeoEventChoice(anuPissedAtBionics, "ANU_PISSED_BIONICS_CHOICE_2", "ANU_PISSED_BIONICS_CHOICE_2_OUTCOME");
                anuPissedAtBionics.GeoscapeEventData.Choices[2].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("BG_Anu_Pissed_Made_Promise", 1, true));


                //Anu really pissed at player for doing Bionics
                GeoscapeEventDef anuReallyPissedAtBionics = TFTVCommonMethods.CreateNewEvent("Anu_Pissed2", "ANU_REALLY_PISSED_BIONICS_TITLE", "ANU_REALLY_PISSED_BIONICS_TEXT_GENERAL_0", null);
                anuReallyPissedAtBionics.GeoscapeEventData.Leader = "AN_Synod";
                anuReallyPissedAtBionics.GeoscapeEventData.Choices[0].Text.LocalizationKey = "ANU_REALLY_PISSED_BIONICS_CHOICE_0";
                anuReallyPissedAtBionics.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(anu, phoenixPoint, -6));

                //NJ pissed at player for doing Mutations
                GeoscapeEventDef nJPissedAtMutations = TFTVCommonMethods.CreateNewEvent("NJ_Pissed1", "NJ_PISSED_MUTATIONS_TITLE", "NJ_PISSED_MUTATIONS_TEXT_GENERAL_0", "NJ_PISSED_MUTATIONS_CHOICE_0_OUTCOME");
                nJPissedAtMutations.GeoscapeEventData.Leader = "NJ_TW";
                nJPissedAtMutations.GeoscapeEventData.Choices[0].Text.LocalizationKey = "NJ_PISSED_MUTATIONS_CHOICE_0";
                nJPissedAtMutations.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(newJericho, phoenixPoint, -5));
                TFTVCommonMethods.GenerateGeoEventChoice(nJPissedAtMutations, "NJ_PISSED_MUTATIONS_CHOICE_1", "NJ_PISSED_MUTATIONS_CHOICE_1_OUTCOME");
                nJPissedAtMutations.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(newJericho, phoenixPoint, -8));
                nJPissedAtMutations.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(anu, phoenixPoint, +2));
                TFTVCommonMethods.GenerateGeoEventChoice(nJPissedAtMutations, "NJ_PISSED_MUTATIONS_CHOICE_2", "NJ_PISSED_MUTATIONS_CHOICE_2_OUTCOME");
                nJPissedAtMutations.GeoscapeEventData.Choices[2].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("BG_NJ_Pissed_Made_Promise", 1, true));

                //NJ really pissed at player for doing Mutations
                GeoscapeEventDef nJReallyPissedAtMutations = TFTVCommonMethods.CreateNewEvent("NJ_Pissed2", "NJ_REALLY_PISSED_MUTATIONS_TITLE", "NJ_REALLY_PISSED_MUTATIONS_TEXT_GENERAL_0", null);
                nJReallyPissedAtMutations.GeoscapeEventData.Leader = "NJ_TW";
                nJReallyPissedAtMutations.GeoscapeEventData.Choices[0].Text.LocalizationKey = "NJ_REALLY_PISSED_MUTATIONS_CHOICE_0";
                nJReallyPissedAtMutations.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(newJericho, phoenixPoint, -6));
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        //Story events:

        public static void CreateEventFirstFlyer()
        {
            try
            {
                string eventID = "OlenaOnFirstFlyer";
                GeoscapeEventDef newEvent = TFTVCommonMethods.CreateNewEvent(eventID + "_GeoscapeEventDef", "OLENA_ON_FIRST_FLYER_TITLE", "OLENA_ON_FIRST_FLYER_TEXT", null);
                newEvent.GeoscapeEventData.EventID = eventID;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateEventFirstHavenTarget()
        {
            try
            {
                string eventID = "OlenaOnFirstHavenTarget";
                GeoscapeEventDef newEvent = TFTVCommonMethods.CreateNewEvent(eventID + "_GeoscapeEventDef", "OLENA_ON_FIRST_HAVEN_TARGET_TITLE", "OLENA_ON_FIRST_HAVEN_TARGET_TEXT", null);
                newEvent.GeoscapeEventData.EventID = eventID;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateEventFirstHavenAttack()
        {
            try
            {
                string eventID = "OlenaOnFirstHavenAttack";
                GeoscapeEventDef newEvent = TFTVCommonMethods.CreateNewEvent(eventID + "_GeoscapeEventDef", "FIRST_HAVEN_ATTACK_TITLE", "FIRST_HAVEN_ATTACK_TEXT", "FIRST_HAVEN_ATTACK_OUTCOME");
                newEvent.GeoscapeEventData.EventID = eventID;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateEventMessagesFromTheVoid()
        {
            try
            {
                string eventID = "AlistairOnMessagesFromTheVoid";
                GeoscapeEventDef newEvent = TFTVCommonMethods.CreateNewEvent(eventID + "_GeoscapeEventDef", "AFTER_YE_SIGNAL_TITLE", "AFTER_YE_SIGNAL_TEXT", "AFTER_YE_SIGNAL_OUTCOME");
                newEvent.GeoscapeEventData.EventID = eventID;
                newEvent.GeoscapeEventData.Choices[0].Text.LocalizationKey = "AFTER_YE_SIGNAL_CHOICE";

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateBehemothPattern()
        {
            try
            {
                string eventID = "OlenaOnBehemothPattern";
                GeoscapeEventDef newEvent = TFTVCommonMethods.CreateNewEvent(eventID + "_GeoscapeEventDef", "BEHEMOTH_PATTERN_TITLE", "BEHEMOTH_PATTERN_TEXT", null);
                newEvent.GeoscapeEventData.EventID = eventID;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateTrappedInMist()
        {
            try
            {
                string eventID = "OlenaOnHavenInfested";
                GeoscapeEventDef newEvent = TFTVCommonMethods.CreateNewEvent(eventID + "_GeoscapeEventDef", "OLENA_ON_HAVEN_INFESTED_TITLE", "OLENA_ON_HAVEN_INFESTED_TEXT", null);
                newEvent.GeoscapeEventData.EventID = eventID;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>
        /// Leftovers, not used.
        /// </summary>

        internal static void TestingKnockBackRepositionAlternative()
        {
            try
            {
                string nameKnockBack = "KnockBackAbility";
                string gUIDAbility = "{B4238D2D-3E25-4EE5-A3C0-23CFED493D42}";

                RepositionAbilityDef source = DefCache.GetDef<RepositionAbilityDef>("Dash_AbilityDef");
                RepositionAbilityDef newKnockBackAbility = Helper.CreateDefFromClone(source, gUIDAbility, nameKnockBack);
                newKnockBackAbility.ActionPointCost = 0.0f;
                newKnockBackAbility.WillPointCost = 0.0f;
                newKnockBackAbility.UsesPerTurn = -1;
                newKnockBackAbility.EventOnActivate = new TacticalEventDef();
                newKnockBackAbility.AmountOfMovementToUseAsRange = 0;
                // newKnockBackAbility.FumblePerc = 0;
                newKnockBackAbility.TraitsRequired = new string[] { };
                //    newKnockBackAbility.HeightToWidth = 0.01f;
                //  newKnockBackAbility.TesellationPoints = 10;
                // newKnockBackAbility.UseLeapAnimation = true;


                string gUIDTargeting = "{8B266029-F014-4514-865A-C51201944385}";
                TacticalTargetingDataDef tacticalTargetingDataDef = Helper.CreateDefFromClone(source.TargetingDataDef, gUIDTargeting, nameKnockBack);
                tacticalTargetingDataDef.Origin.Range = 3;

                /*   string gUIDAnim = "{B1ADC473-1AD8-431F-8953-953E4CB3E584}";
                   TacActorJumpAbilityAnimActionDef animSource = DefCache.GetDef<TacActorJumpAbilityAnimActionDef>("E_JetJump [Soldier_Utka_AnimActionsDef]");
                   TacActorJumpAbilityAnimActionDef knockBackAnimation = Helper.CreateDefFromClone(animSource, gUIDAnim, nameKnockBack);
                   TacActorNavAnimActionDef someAnimations = DefCache.GetDef<TacActorNavAnimActionDef>("E_CrabmanNav [Crabman_AnimActionsDef]");
                   TacActorSimpleReactionAnimActionDef hurtReaction = DefCache.GetDef<TacActorSimpleReactionAnimActionDef>("E_Hurt_Reaction [Crabman_AnimActionsDef]");
                   /*  knockBackAnimation.Clip = hurtReaction.GetAllClips().First();
                     knockBackAnimation.ClipEnd = someAnimations.FallNoSupport.Stop;
                     knockBackAnimation.ClipStart = hurtReaction.GetAllClips().First();*/
                /*  knockBackAnimation.Clip = someAnimations.JetJump.Loop;
                  knockBackAnimation.ClipEnd = hurtReaction.GetAllClips().First();
                  knockBackAnimation.ClipStart = someAnimations.JetJump.Loop;

                  knockBackAnimation.AbilityDefs = new AbilityDef[] { newKnockBackAbility };



                  TacActorAnimActionsDef crabAnimActions = DefCache.GetDef<TacActorAnimActionsDef>("Crabman_AnimActionsDef");
                  List<TacActorAnimActionBaseDef> crabAnimations = new List<TacActorAnimActionBaseDef>(crabAnimActions.AnimActions.ToList());
                  crabAnimations.Add(knockBackAnimation);
                  crabAnimActions.AnimActions = crabAnimations.ToArray();*/


            }


            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void TestingKnockBack()
        {
            try
            {
                string nameKnockBack = "KnockBackAbility";
                string gUIDAbility = "{B4238D2D-3E25-4EE5-A3C0-23CFED493D42}";

                JetJumpAbilityDef source = DefCache.GetDef<JetJumpAbilityDef>("JetJump_AbilityDef");
                JetJumpAbilityDef newKnockBackAbility = Helper.CreateDefFromClone(source, gUIDAbility, nameKnockBack);
                newKnockBackAbility.ActionPointCost = 0.0f;
                newKnockBackAbility.WillPointCost = 0.0f;
                newKnockBackAbility.FumblePerc = 0;
                newKnockBackAbility.TraitsRequired = new string[] { };
                newKnockBackAbility.HeightToWidth = 0.01f;
                //  newKnockBackAbility.TesellationPoints = 10;
                // newKnockBackAbility.UseLeapAnimation = true;


                string gUIDTargeting = "{8B266029-F014-4514-865A-C51201944385}";
                TacticalTargetingDataDef tacticalTargetingDataDef = Helper.CreateDefFromClone(source.TargetingDataDef, gUIDTargeting, nameKnockBack);
                tacticalTargetingDataDef.Origin.Range = 1;

                string gUIDAnim = "{B1ADC473-1AD8-431F-8953-953E4CB3E584}";
                TacActorJumpAbilityAnimActionDef animSource = DefCache.GetDef<TacActorJumpAbilityAnimActionDef>("E_JetJump [Soldier_Utka_AnimActionsDef]");
                TacActorJumpAbilityAnimActionDef knockBackAnimation = Helper.CreateDefFromClone(animSource, gUIDAnim, nameKnockBack);
                TacActorNavAnimActionDef someAnimations = DefCache.GetDef<TacActorNavAnimActionDef>("E_CrabmanNav [Crabman_AnimActionsDef]");
                TacActorSimpleReactionAnimActionDef hurtReaction = DefCache.GetDef<TacActorSimpleReactionAnimActionDef>("E_Hurt_Reaction [Crabman_AnimActionsDef]");
                /*  knockBackAnimation.Clip = hurtReaction.GetAllClips().First();
                  knockBackAnimation.ClipEnd = someAnimations.FallNoSupport.Stop;
                  knockBackAnimation.ClipStart = hurtReaction.GetAllClips().First();*/
                knockBackAnimation.Clip = someAnimations.JetJump.Loop;
                knockBackAnimation.ClipEnd = hurtReaction.GetAllClips().First();
                knockBackAnimation.ClipStart = someAnimations.JetJump.Loop;

                knockBackAnimation.AbilityDefs = new AbilityDef[] { newKnockBackAbility };



                TacActorAnimActionsDef crabAnimActions = DefCache.GetDef<TacActorAnimActionsDef>("Crabman_AnimActionsDef");
                List<TacActorAnimActionBaseDef> crabAnimations = new List<TacActorAnimActionBaseDef>(crabAnimActions.AnimActions.ToList());
                crabAnimations.Add(knockBackAnimation);
                crabAnimActions.AnimActions = crabAnimations.ToArray();


            }


            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        //Causes too many issues
        internal static void MakeUmbraNotObstacle()
        {
            try
            {

                TacticalActorDef oilcrab = DefCache.GetDef<TacticalActorDef>("Oilcrab_ActorDef");
                TacticalActorDef oilfish = DefCache.GetDef<TacticalActorDef>("Oilfish_ActorDef");

                DefCache.GetDef<TacticalNavigationComponentDef>("Oilcrab_NavigationDef").CreateNavObstacle = false;
                DefCache.GetDef<TacticalNavigationComponentDef>("Oilfish_NavigationDef").CreateNavObstacle = false;

                DieAbilityDef source = DefCache.GetDef<DieAbilityDef>("ArmadilloHulk_DieAbilityDef");
                DieAbilityDef newDieAbility = Helper.CreateDefFromClone(source, "{8654CB01-602D-4204-8A03-2BA50999C1B8}", "DieNoRagDoll");

                RagdollDieAbilityDef oilMonsterRagDollDie = DefCache.GetDef<RagdollDieAbilityDef>("OilMonster_Die_AbilityDef");

                newDieAbility.EventOnActivate = oilMonsterRagDollDie.EventOnActivate;
                newDieAbility.DeathEffect = oilMonsterRagDollDie.DeathEffect;
                newDieAbility.DestroyItems = false;

                oilcrab.Abilities[4] = newDieAbility;
                oilfish.Abilities[4] = newDieAbility;


            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void ReEnableFlinching()
        {
            try
            {
                // TacticalLevelController

                TacticalLevelControllerDef tacticalLevelControllerDef = DefCache.GetDef<TacticalLevelControllerDef>("TacticalLevelControllerDef");
                tacticalLevelControllerDef.UseFlinching = true;

                AddonsComponentDef fishmanAddons = DefCache.GetDef<AddonsComponentDef>("Fishman_AddonsComponentDef");
                TacActorSimpleReactionAnimActionDef fishReactionAnim = DefCache.GetDef<TacActorSimpleReactionAnimActionDef>("E_Hurt_Reaction_01Hands [Fishman_AnimActionsDef]");
                TacActorAnimActionsDef fishAnimations = DefCache.GetDef<TacActorAnimActionsDef>("Fishman_AnimActionsDef");
                fishAnimations.DefaultReactionClip = fishReactionAnim.GetAllClips().First();

                //  fishmanAddons.InitialRagdollMode = CollidersRagdollActivationMode.Ragdoll;

                RagdollDummyDef ragdollDummyDef = DefCache.GetDef<RagdollDummyDef>("Generic_RagdollDummyDef");
                ragdollDummyDef.FlinchForceMultiplier = 200f; //2f //4f //5f
                                                              //    ragdollDummyDef.OverrideAngularDrag = 40;
                                                              //   ragdollDummyDef.OverrideDrag = 10;
                ragdollDummyDef.FlinchForceMultiplierSecondary = 50f;
                //   ragdollDummyDef.LeashDamper = 1f;
                ComponentSetDef crabmanComponent = DefCache.GetDef<ComponentSetDef>("Crabman_Template_ComponentSetDef");


                List<ObjectDef> crabComponentSetDefs = new List<ObjectDef>(crabmanComponent.Components);
                crabComponentSetDefs.Insert(crabComponentSetDefs.Count - 2, ragdollDummyDef);
                crabmanComponent.Components = crabComponentSetDefs.ToArray();


                ComponentSetDef fishmanComponent = DefCache.GetDef<ComponentSetDef>("Fishman_ComponentSetDef");


                List<ObjectDef> fishComponentSetDefs = new List<ObjectDef>(fishmanComponent.Components);
                fishComponentSetDefs.Insert(fishComponentSetDefs.Count - 2, ragdollDummyDef);
                fishmanComponent.Components = fishComponentSetDefs.ToArray();


            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void CreateCyclopsScreamStatus()
        {
            try
            {
                BleedStatusDef sourceBleedStatusDef = DefCache.GetDef<BleedStatusDef>("Bleed_StatusDef");

                string statusScreamedLevel1Name = "CyclopsScreamLevel1_BleedStatusDef";
                BleedStatusDef statusScreamedLevel1 = Helper.CreateDefFromClone(sourceBleedStatusDef, "{73C5B78E-E9CB-4558-95AA-807B7AE2755A}", statusScreamedLevel1Name);
                statusScreamedLevel1.EffectName = "CyclopsScreamLevel1";
                statusScreamedLevel1.ApplicationConditions = new EffectConditionDef[] { };
                statusScreamedLevel1.Visuals = Helper.CreateDefFromClone(sourceBleedStatusDef.Visuals, "{A7BADADA-F936-4D28-B171-A4A770A673E7}", statusScreamedLevel1Name);
                statusScreamedLevel1.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.AlwaysVisible;
                statusScreamedLevel1.VisibleOnPassiveBar = true;
                statusScreamedLevel1.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;
                statusScreamedLevel1.Visuals.DisplayName1.LocalizationKey = "SCREAMED_LEVEL1_TITLE";
                statusScreamedLevel1.Visuals.Description.LocalizationKey = "SCREAMED_LEVEL1_TEXT";
                statusScreamedLevel1.Visuals.LargeIcon = Helper.CreateSpriteFromImageFile("TFTV_status_self_repair.png");
                statusScreamedLevel1.Visuals.SmallIcon = Helper.CreateSpriteFromImageFile("TFTV_status_self_repair.png");

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }



    }


}
