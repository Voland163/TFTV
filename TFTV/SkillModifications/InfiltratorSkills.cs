using Base.Cameras.ExecutionNodes;
using Base.Defs;
using Base.Entities.Abilities;
using Base.Entities.Effects;
using Base.Entities.Effects.ApplicationConditions;
using Base.Entities.Statuses;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Modding;
using PhoenixPoint.Tactical.Cameras.Filters;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Animations;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using PhoenixPoint.Tactical.Levels.Missions;
using PhoenixPoint.Tactical.View;
using System;
using System.Collections.Generic;
using System.Linq;
using TFTV;
using TFTV.Tactical.Entities.Statuses;
using UnityEngine;

namespace PRMBetterClasses.SkillModifications
{
    internal class InfiltratorSkills
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        public static ModLogger Logger = TFTVMain.Main.Logger;


        public static void ApplyChanges()
        {
            // Surprise Attack: Attack from behind gain x5 shock value
            Change_SurpriseAttack();

            // Bullet Time: Passive, Whenever you daze an enemy gain 1AP
            Create_NeuralFeedback();

            // Neural Feedback: Passive, Enemies within 10 tiles have 50% fumble chance with firearms
            Create_JammingField();

            // Parasychosis: 1AP / 6WP, Target human-sized enemy within 12 tiles becomes Wild
            Create_Parasychosis();

            // Sapper: Your Decoys and Drones explode when destroyed
            //Create_Sapper();

            // Spider Drone Pack: 3 AP 3 WP, rest vanilla
            Change_DeployDronePack();

            // Sneak Attack: Direct fire and melee +60 damage while not spotted
            Change_SneakAttack();

            // Cautious: +10% stealth
            Change_Cautious();

            //Phantom Protocol: Gain +15% stealth and accuracy, provides immunity to Sentinels surveillance ability
            Create_BC2_PhantomProtocol();

            // Old Phantom Protocol BC1: like above, but active and without deny Sentinel detection
            // ### DEPRECTATED, KEPT FOR SAVE GAME COMPATIBILTY ###
            Create_PhantomProtocol_Old();
        }

        private static void Change_SurpriseAttack()
        {
            //PassiveModifierAbilityDef SurpriseAttack = DefCache.GetDef<PassiveModifierAbilityDef>("SurpriseAttack_AbilityDef");
            StunDamageKeywordDataDef SurpriseAttack_DamageKeywordDataDef = DefCache.GetDef<StunDamageKeywordDataDef>("SurpriseAttack_DamageKeywordDataDef");
            SurpriseAttack_DamageKeywordDataDef.ValueMultiplier = 5.0f;
        }

        private static void Create_NeuralFeedback()
        {
            string skillname = "NeuralFeedback_AbilityDef";
            ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("RapidClearance_AbilityDef");
            ApplyStatusAbilityDef neuralFeedback = Helper.CreateDefFromClone(
                source,
                "83A209AB-22E0-4B8F-AECB-19EF6C975126",
                skillname);
            neuralFeedback.StatusApplicationTrigger = StatusApplicationTrigger.ActorEnterPlay;
            neuralFeedback.Active = false;
            neuralFeedback.WillPointCost = 0;
            neuralFeedback.TraitsRequired = new string[0];
            neuralFeedback.LogStats = false;

            neuralFeedback.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "2BB58C99-E21A-4ABD-8F7A-AC40914982E3",
                $"E_CharacterProgressionData [{skillname}]");
            neuralFeedback.TargetingDataDef = DefCache.GetDef<TacticalTargetingDataDef>("_Self_TargetingDataDef");
            neuralFeedback.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "05B0A775-B409-4FEA-A432-671833D736B1",
                $"E_ViewElementDef [{skillname}]");
            neuralFeedback.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_NEURAL_FEEDBACK";
            neuralFeedback.ViewElementDef.Description.LocalizationKey = "PR_BC_NEURAL_FEEDBACK_DESC";
            neuralFeedback.ViewElementDef.SmallIcon = Helper.CreateSpriteFromImageFile("telepathy.png");
            neuralFeedback.ViewElementDef.LargeIcon = neuralFeedback.ViewElementDef.SmallIcon;

            OnActorDazedEffectStatusDef dazedEffectStatus = Helper.CreateDefFromClone<OnActorDazedEffectStatusDef>(
                null,
                "C3E39CA8-EAD4-479D-AD93-C6019FC3002F",
                $"E_OnActorDazedEffectStatus [{skillname}]");

            OnActorDeathEffectStatusDef rapidClearanceDeathEffectStatus = (OnActorDeathEffectStatusDef)source.StatusDef;
            dazedEffectStatus.EffectName = "NeuralFeedback";
            dazedEffectStatus.ApplicationConditions = new EffectConditionDef[0];
            //dazedEffectStatus.DurationTurns = -1;
            //dazedEffectStatus.ShowNotification = true;
            //dazedEffectStatus.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.VisibleWhenSelected;
            //dazedEffectStatus.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnBodyPartStatusList | TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;
            //dazedEffectStatus.TriggerOnRelation = FactionRelation.Enemy;
            dazedEffectStatus.Visuals = neuralFeedback.ViewElementDef;
            //dazedEffectStatus.EventOnApply = rapidClearanceDeathEffectStatus.EventOnApply;
            dazedEffectStatus.RestoreActionPointsFraction = 0.5f;
            dazedEffectStatus.EventOnSuccessfulTrigger = Helper.CreateDefFromClone(
                rapidClearanceDeathEffectStatus.EventOnSuccessfulTrigger,
                "5DCD7412-8AC3-43D1-A754-5577FCDFD9C6",
                $"E_EventOnSuccessfulTrigger [{skillname}]");

            neuralFeedback.StatusDef = dazedEffectStatus;

            //JFileManager jFileManager = new JFileManager("NeuralFeedback_DefSettings.json");
            //jFileManager.SetDefFieldsByReflection(bulletTime);
        }

        private static void Create_JammingField()
        {
            string skillname = "JammingFiled_AbilityDef";

            ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("PsychicWard_AbilityDef");

            ApplyStatusAbilityDef jammingFiled = Helper.CreateDefFromClone(
                source,
                "9CC2866D-B4A9-4A31-8AA1-80FD97DF316B",
                skillname);

            jammingFiled.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "A1DC4C52-0642-40F2-A94F-4C50F9A5996D",
                $"E_CharacterProgressionData [{skillname}]");

            jammingFiled.TargetingDataDef = Helper.CreateDefFromClone(
                source.TargetingDataDef,
                "9081A037-71D6-4220-83E1-A0D96AD830D0",
                $"E_TargetingData [{skillname}]");
            jammingFiled.TargetingDataDef.Origin.TargetSelf = false;
            jammingFiled.TargetingDataDef.Origin.TargetFriendlies = false;
            jammingFiled.TargetingDataDef.Origin.TargetEnemies = true;
            jammingFiled.TargetingDataDef.Origin.LineOfSight = LineOfSightType.Ignore;
            jammingFiled.TargetingDataDef.Origin.FactionVisibility = LineOfSightType.Ignore;
            jammingFiled.TargetingDataDef.Origin.Range = 10;
            jammingFiled.TargetingDataDef.Origin.HorizontalRangeOnly = false;

            jammingFiled.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "885B0B90-814C-4CDE-8F28-9B6D5CFC8D62",
                $"E_ViewElementDef [{skillname}]");
            jammingFiled.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_JAMMING_FIELD";
            jammingFiled.ViewElementDef.Description.LocalizationKey = "PR_BC_JAMMING_FIELD_DESC";
            jammingFiled.ViewElementDef.SmallIcon = Helper.CreateSpriteFromImageFile("jamming7.png");
            jammingFiled.ViewElementDef.LargeIcon = jammingFiled.ViewElementDef.SmallIcon;

            FumbleChanceStatusDef fumbleChanceStatus = Helper.CreateDefFromClone<FumbleChanceStatusDef>(
                null,
                "0C6293EE-1DFC-47A3-B02E-D169F1C0BC12",
                $"E_FumbleChanceStatus [{skillname}]");
            fumbleChanceStatus.EffectName = "JammingField";
            fumbleChanceStatus.ApplicationConditions = new EffectConditionDef[0];
            fumbleChanceStatus.ShowNotification = true;
            fumbleChanceStatus.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.AlwaysVisible;
            fumbleChanceStatus.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;
            fumbleChanceStatus.Visuals = jammingFiled.ViewElementDef;
            fumbleChanceStatus.FumbleChancePerc = 50;
            fumbleChanceStatus.RestrictedDeliveryType = DamageDeliveryType.DirectLine;
            //fumbleChanceStatus.AdditionalAbilitiesToFumble = new TacticalAbilityDef[]
            //{
            //    DefCache.GetDef<TacticalAbilityDef>("InducePanic_AbilityDef"),
            //    DefCache.GetDef<TacticalAbilityDef>("Priest_MindControl_AbilityDef"),
            //    DefCache.GetDef<TacticalAbilityDef>("MindCrush_AbilityDef"),
            //    DefCache.GetDef<TacticalAbilityDef>("Priest_PsychicScream_AbilityDef"),
            //    DefCache.GetDef<TacticalAbilityDef>("Siren_PsychicScream_AbilityDef"),
            //};
            jammingFiled.StatusDef = fumbleChanceStatus;
        }

        private static void Create_Parasychosis()
        {
            string skillName = "Parasychosis_AbilityDef";

            ApplyEffectAbilityDef source = DefCache.GetDef<ApplyEffectAbilityDef>("MindCrush_AbilityDef");

            ApplyEffectAbilityDef parasychosis = Helper.CreateDefFromClone(
                source,
                "1E188986-F287-4C31-A289-A85170C9C57E",
                skillName);
            parasychosis.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "F3B9BD03-4238-4AB6-BF33-068CC716FD1D",
                skillName);
            parasychosis.TargetingDataDef = Helper.CreateDefFromClone(
                source.TargetingDataDef,
                "46095AF8-9B32-4122-AF4A-775DE55F3E84",
                skillName);
            parasychosis.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "8A6BF07C-799C-46C9-ACCA-3138EB243B75",
                skillName);
            parasychosis.EffectDef = Helper.CreateDefFromClone(
                source.EffectDef,
                "4D1420F3-CDD2-4B11-B56D-194F8AA07266",
                skillName);

            parasychosis.TargetingDataDef.Origin.LineOfSight = LineOfSightType.InSight;
            parasychosis.TargetingDataDef.Origin.FactionVisibility = LineOfSightType.InSight;
            parasychosis.TargetingDataDef.Origin.CanPeekFromEdge = true;
            parasychosis.TargetingDataDef.Origin.Range = 14;
            parasychosis.TargetingDataDef.Origin.CullTargetTags = new GameTagsList()
            {
                DefCache.GetDef<GameTagDef>("Acheron_ClassTagDef"),
                DefCache.GetDef<GameTagDef>("Chiron_ClassTagDef"),
                DefCache.GetDef<GameTagDef>("CorruptionNode_ClassTagDef"),
                DefCache.GetDef<GameTagDef>("Mutog_ClassTagDef"),
                DefCache.GetDef<GameTagDef>("Queen_ClassTagDef"),
                DefCache.GetDef<GameTagDef>("Sentinel_ClassTagDef"),
                DefCache.GetDef<GameTagDef>("SpawningPoolCrabman_ClassTagDef"),
                DefCache.GetDef<GameTagDef>("Yuggothian_ClassTagDef"),
            };
            parasychosis.TargetingDataDef.Target.TargetEnemies = true;
            parasychosis.TargetingDataDef.Target.TargetResult = TargetResult.Actor;
            parasychosis.TargetingDataDef.Target.TargetTags = parasychosis.TargetingDataDef.Origin.TargetTags;
            parasychosis.TargetingDataDef.Target.CullTargetTags = parasychosis.TargetingDataDef.Origin.CullTargetTags;

            parasychosis.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_PARAPSYCHOSIS";
            parasychosis.ViewElementDef.Description.LocalizationKey = "PR_BC_PARAPSYCHOSIS_DESC";
            parasychosis.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile("parapsy1.png");
            parasychosis.ViewElementDef.SmallIcon = parasychosis.ViewElementDef.LargeIcon;

            parasychosis.TrackWithCamera = true;
            parasychosis.ShownModeToTrack = KnownState.Hidden;
            parasychosis.ActionPointCost = 0.25f;
            parasychosis.WillPointCost = 6;
            parasychosis.ApplyToAllTargets = false;
            parasychosis.SimulatesDamage = false;
            parasychosis.MultipleTargetSimulation = false;

            parasychosis.ContributionPointsOnUse = 1000;

            parasychosis.EffectDef = DefCache.GetDef<EffectDef>("ChangeFaction_WildBeast_EffectDef");

            // Animation related stuff
            FirstMatchExecutionDef cameraAbility = Helper.CreateDefFromClone(
                DefCache.GetDef<FirstMatchExecutionDef>("E_MindControlAbility [NoDieCamerasTacticalCameraDirectorDef]"),
                "4BBDECC9-3FB6-418D-AB4E-CADD2E072C43",
                "E_Parasychosis_CameraAbility [NoDieCamerasTacticalCameraDirectorDef]");
            cameraAbility.FilterDef = Helper.CreateDefFromClone(
                DefCache.GetDef<TacCameraAbilityFilterDef>("E_MindControlFilter [NoDieCamerasTacticalCameraDirectorDef]"),
                "D6D695BD-4B84-4611-90F9-2143F02C3A26",
                "E_Parasychosis_CameraAbilityFilter [NoDieCamerasTacticalCameraDirectorDef]");
            (cameraAbility.FilterDef as TacCameraAbilityFilterDef).TacticalAbilityDef = parasychosis;

            TacticalAbilityDef animSource = DefCache.GetDef<TacticalAbilityDef>("Priest_MindControl_AbilityDef");
            foreach (TacActorSimpleAbilityAnimActionDef animActionDef in Repo.GetAllDefs<TacActorSimpleAbilityAnimActionDef>().Where(aad => aad.name.Contains("Soldier_Utka_AnimActionsDef")))
            {
                if (animActionDef.AbilityDefs != null && animActionDef.AbilityDefs.Contains(animSource) && !animActionDef.AbilityDefs.Contains(parasychosis))
                {
                    animActionDef.AbilityDefs = animActionDef.AbilityDefs.Append(parasychosis).ToArray();
                    PRMLogger.Debug("Anim Action '" + animActionDef.name + "' set for abilities:");
                    foreach (AbilityDef ad in animActionDef.AbilityDefs)
                    {
                        PRMLogger.Debug("  " + ad.name);
                    }
                    PRMLogger.Debug("----------------------------------------------------", false);
                }
            }
        }
        //Patch to set a different health bar color for wild faction actors 
        [HarmonyPatch(typeof(HealthbarUIActorElement), "GetMainColor")]
        public static class HealthbarUIActorElement_GetMainColor_Patch
        {
            public static void Postfix(HealthbarUIActorElement __instance, ref Color __result)
            {
                if (__instance.Actor is TacticalActor tacticalActor
                    && tacticalActor.TacticalFaction == tacticalActor.TacticalLevel.GetTacticalFaction(tacticalActor.TacticalLevel.TacticalLevelControllerDef.WildBeastFaction))
                {
                    __result = Color.magenta;
                }
            }
        }
        //Patch to prevent wild faction actors targetting them self, vanilla they are set to be enemies to them self, this will set them to be friends
        [HarmonyPatch(typeof(TacMission), "SetDefaultFactionRelations")]
        public static class TacMission_SetDefaultFactionRelations_Patch
        {
            public static void Postfix(TacMission __instance)
            {
                TacticalFaction wildBeastFaction = __instance.TacticalLevel.GetTacticalFaction(__instance.TacticalLevel.TacticalLevelControllerDef.WildBeastFaction);
                TacticalFaction.SetMutualRelation(wildBeastFaction, wildBeastFaction, FactionRelation.Friend);
            }
        }

        //private static void Create_Sapper()
        //{
        //}

        private static void Change_DeployDronePack()
        {
            ShootAbilityDef DeployDronePack = DefCache.GetDef<ShootAbilityDef>("DeployDronePack_ShootAbilityDef");
            DeployDronePack.ActionPointCost = 0.75f;
            DeployDronePack.WillPointCost = 3.0f;
        }

        private static void Change_SneakAttack()
        {
            //float hiddenDamageMod = 1.5f;
            //float locatedDamageMod = 1.25f;

            ApplyStatusAbilityDef SneakAttack = DefCache.GetDef<ApplyStatusAbilityDef>("SneakAttack_AbilityDef");
            FactionVisibilityConditionStatusDef factionVisibilityConditionStatus = (FactionVisibilityConditionStatusDef)SneakAttack.StatusDef;
            StanceStatusDef hiddenStateStatus = (StanceStatusDef)factionVisibilityConditionStatus.HiddenStateStatusDef;
            hiddenStateStatus.Visuals = Helper.CreateDefFromClone(
                SneakAttack.ViewElementDef,
                "8981e175-124a-48eb-8644-69f4ae77d454",
                $"E_HiddenStatusViewElement [{SneakAttack.name}]");
            StanceStatusDef locatedStateStatus = Helper.CreateDefFromClone(
                hiddenStateStatus,
                "8cc0375e-1f2d-4e4a-85df-d626dab2a92a",
                $"E_DetectedSneakAttackStatus [{SneakAttack.name}]");
            locatedStateStatus.Visuals = Helper.CreateDefFromClone(
                SneakAttack.ViewElementDef,
                "63353d9f-97c3-46fb-b40b-b9eb9d32f1ce",
                $"E_DetectedStatusViewElement [{SneakAttack.name}]");

            // Setting fields
            SneakAttack.ViewElementDef.Description.LocalizationKey = "PR_BC_SNEAK_ATTACK_DESC";

            hiddenStateStatus.Visuals.Description.LocalizationKey = "PR_BC_SA_HIDDEN_DESC";
            hiddenStateStatus.Visuals.Color = Color.green;
            hiddenStateStatus.StatModifications[0].Value = 1.5f;

            locatedStateStatus.Visuals.Description.LocalizationKey = "PR_BC_SA_LOCATED_DESC";
            locatedStateStatus.Visuals.Color = Color.yellow;
            locatedStateStatus.StatModifications[0].Value = 1.25f;

            factionVisibilityConditionStatus.LocatedStateStatusDef = locatedStateStatus;
        }

        private static void Change_Cautious()
        {
            PassiveModifierAbilityDef cautious = DefCache.GetDef<PassiveModifierAbilityDef>("Cautious_AbilityDef");
            cautious.StatModifications = new ItemStatModification[]
            {
                new ItemStatModification()
                {
                   TargetStat = StatModificationTarget.Accuracy,
                   Modification = StatModificationType.Add,
                   Value = 0.2f
                },
                new ItemStatModification()
                {
                   TargetStat = StatModificationTarget.Stealth,
                   Modification = StatModificationType.Add,
                   Value = 0.1f
                }
            };
            cautious.ViewElementDef.Description.LocalizationKey = "PR_BC_CAUTIOUS_DESC";
        }

        public static PassiveModifierAbilityDef PhantomProtocolDef;
        private static void Create_BC2_PhantomProtocol()
        {
            string skillName = "BC2_PhantomProtocol_AbilityDef";

            PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("Cautious_AbilityDef");
            PhantomProtocolDef = Helper.CreateDefFromClone(
                source,
                "B503271D-CB9F-41B2-82C4-60D4F73DA417",
                skillName);
            PhantomProtocolDef.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "6E2079D2-1FD2-4427-819D-A71A28E06422",
                skillName);
            PhantomProtocolDef.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "B7F6B435-D6C5-4219-A569-D103F49B4C26",
                skillName);

            // Set necessary fields
            PhantomProtocolDef.StatModifications = new ItemStatModification[]
            {
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Accuracy,
                    Modification = StatModificationType.Add,
                    Value = 0.15f
                },
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Stealth,
                    Modification = StatModificationType.Add,
                    Value = 0.15f
                }
            };

            // Change neccesary fields
            PhantomProtocolDef.CharacterProgressionData.RequiredSpeed = 0;
            PhantomProtocolDef.CharacterProgressionData.RequiredStrength = 0;
            PhantomProtocolDef.CharacterProgressionData.RequiredWill = 0;
            PhantomProtocolDef.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_PHANTOM_PROTOCOL";
            PhantomProtocolDef.ViewElementDef.Description.LocalizationKey = "PR_BC_PHANTOM_PROTOCOL_DESC";
            Sprite PP_Icon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_Phantom_Protocol_2-2.png");
            PhantomProtocolDef.ViewElementDef.LargeIcon = PP_Icon;
            PhantomProtocolDef.ViewElementDef.SmallIcon = PP_Icon;

            // Grant immunity to Sentinels surveillance ability
            GameTagDef surveillanceImmunity = DefCache.GetDef<GameTagDef>("InvisibleToSentinels_SkillTagDef");
            PhantomProtocolDef.ActorTags = new GameTagDef[] { surveillanceImmunity };

            // Clear list of actors to rescue for rescue mission patch (see below)
            if (actorsToRescue != null && actorsToRescue.Count > 0)
            {
                actorsToRescue.Clear();
            }
        }
        // Patch to apply the SurveillanceImmunity_GameTagDef from BC_PhantomProtocol_AbilityDef.ActorTags to the actors game tags, vanilla does not do that
        [HarmonyPatch(typeof(PassiveModifierAbility), "AbilityAdded")]
        internal static class PassiveModifierAbility_AbilityAdded_Patch
        {
            public static void Postfix(PassiveModifierAbility __instance)
            {
                try
                {
                    if (__instance.PassiveModifierAbilityDef.name.Equals("BC2_PhantomProtocol_AbilityDef")
                        && !__instance.TacticalActorBase.HasGameTags(__instance.PassiveModifierAbilityDef.ActorTags))
                    {
                        __instance.TacticalActorBase.AddGameTags(new GameTagsList(__instance.PassiveModifierAbilityDef.ActorTags));
                    }
                }
                catch (Exception e)
                {
                    PRMLogger.Error(e);
                }
            }
        }
        // Patch the evaluation of rescue mission.
        // The immunity to surveillance with Phantom Protocol caused the mission to fail in the moment an actor to rescue is activated by an Infiltrator with this perk.
        // List for actors to rescue, will be cleared when objective is achieved and on each level start (see above)
        private static readonly List<TacticalActor> actorsToRescue = new List<TacticalActor>();
        [HarmonyPatch(typeof(RescueSoldiersFactionObjective), "EvaluateObjective")]
        internal static class RescueSoldiersFactionObjective_EvaluateObjective_Patch
        {
            public static void Postfix(ref FactionObjectiveState __result, RescueSoldiersFactionObjective __instance)
            {
                try
                {
                    string LogPrefix = "RescueSoldiersFactionObjective_EvaluateObjective_Patch.Postfix(..):".ToUpper();
                    switch (__result)
                    {
                        case FactionObjectiveState.InProgress:
                            foreach (TacticalActor tacticalActor in __instance.Level.Map.GetActors<TacticalActor>(null))
                            {
                                if (tacticalActor.TacticalFaction.Faction.FactionDef == __instance.RescuedFaction)
                                {
                                    if (!(tacticalActor.IsDead || tacticalActor.Status.HasStatus<EvacuatedStatus>()))
                                    {
                                        if (!actorsToRescue.Contains(tacticalActor))
                                        {
                                            actorsToRescue.Add(tacticalActor);
                                            PRMLogger.Always($"{LogPrefix} {tacticalActor} added to rescue list, total actors in list: {actorsToRescue.Count}");
                                        }
                                    }
                                    else
                                    {
                                        if (actorsToRescue.Contains(tacticalActor))
                                        {
                                            actorsToRescue.Remove(tacticalActor);
                                            string reason = tacticalActor.IsDead ? "died" : "evaced";
                                            PRMLogger.Always($"{LogPrefix} {tacticalActor} removed from rescue list because he {reason}, total actors in list: {actorsToRescue.Count}");
                                        }
                                    }
                                }
                            }
                            break;
                        case FactionObjectiveState.Failed:
                            if (actorsToRescue.Count > 0)
                            {
                                string[] messages = new string[]
                                {
                                    $"{LogPrefix} Rescue soldiers objective is failed but rescue list still has {actorsToRescue.Count} actor(s) to rescue!",
                                    $"{LogPrefix} List of actor(s) to rescue: {actorsToRescue.Join()}",
                                    $"{LogPrefix} Set result back to 'InProgress' ..."
                                };
                                foreach (string message in messages)
                                {
                                    Logger.LogWarning(message);
                                    PRMLogger.Always(message);
                                }
                                __result = FactionObjectiveState.InProgress;
                            }
                            break;
                        case FactionObjectiveState.Achieved:
                            PRMLogger.Always($"{LogPrefix} Rescue soldiers objective achieved, current total actors in rescue list: {actorsToRescue.Count}, clearing list ...");
                            actorsToRescue.Clear();
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception e)
                {
                    PRMLogger.Error(e);
                }
            }
        }

        // #############################################################################
        // OLD PHANTOM PROTOCOL, DEPRECATED AND UNUSED, KEPT FOR SAVE GAME COMPATIBILITY
        // #############################################################################
        private static void Create_PhantomProtocol_Old()
        {
            float mod = 0.25f;
            string skillName = "BC_PhantomProtocol_AbilityDef";
            ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("QuickAim_AbilityDef");
            ApplyStatusAbilityDef phantomProtocol = Helper.CreateDefFromClone(
                source,
                "5f3e257c-aff7-4296-9992-f6728bfa8af8",
                skillName);
            phantomProtocol.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "08545868-2bed-47a4-8628-371bbce5f718",
                skillName);
            phantomProtocol.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "c312e7f4-3339-4ee8-9717-d1f9c8bd2b32",
                skillName);
            phantomProtocol.StatusDef = Helper.CreateDefFromClone(
                DefCache.GetDef<StanceStatusDef>("E_VanishedStatus [Vanish_AbilityDef]"),
                "06ca77ea-223b-4ec0-a7e6-734e6b7fefe9",
                "E_AccAnd StealthMultiplier [BC_PhantomProtocol_AbilityDef]");

            phantomProtocol.ViewElementDef.DisplayName1 = new LocalizedTextBind("PHANTOM PROTOCOL", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            phantomProtocol.ViewElementDef.Description = new LocalizedTextBind("You gain +25% accuracy and stealth until next turn", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            Sprite icon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_Phantom_Protocol_2-2.png");
            phantomProtocol.ViewElementDef.LargeIcon = icon;
            phantomProtocol.ViewElementDef.SmallIcon = icon;
            phantomProtocol.ActionPointCost = 0;
            phantomProtocol.WillPointCost = 3;

            StanceStatusDef ppModStatus = (StanceStatusDef)phantomProtocol.StatusDef;
            ppModStatus.Visuals = phantomProtocol.ViewElementDef;
            ppModStatus.EventOnApply = null;
            ppModStatus.EventOnUnapply = null;
            ppModStatus.StatModifications = new ItemStatModification[]
            {
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Accuracy,
                    Modification = StatModificationType.Add,
                    Value = mod
                },
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Stealth,
                    Modification = StatModificationType.Add,
                    Value = mod
                }
            };
            ppModStatus.EquipmentsStatModifications = new EquipmentItemTagStatModification[0];
            ppModStatus.StanceShader = null;

            foreach (TacActorSimpleAbilityAnimActionDef animActionDef in Repo.GetAllDefs<TacActorSimpleAbilityAnimActionDef>().Where(aad => aad.name.Contains("Soldier_Utka_AnimActionsDef")))
            {
                if (animActionDef.AbilityDefs != null && animActionDef.AbilityDefs.Contains(source) && !animActionDef.AbilityDefs.Contains(phantomProtocol))
                {
                    animActionDef.AbilityDefs = animActionDef.AbilityDefs.Append(phantomProtocol).ToArray();
                    PRMLogger.Debug("Anim Action '" + animActionDef.name + "' set for abilities:");
                    foreach (AbilityDef ad in animActionDef.AbilityDefs)
                    {
                        PRMLogger.Debug("  " + ad.name);
                    }
                    PRMLogger.Debug("----------------------------------------------------", false);
                }
            }
        }
    }
}
