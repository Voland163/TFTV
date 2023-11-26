﻿using Base;
using Base.Audio;
using Base.Core;
using Base.Entities;
using Base.Entities.Effects;
using Base.Entities.Statuses;
using Base.Eventus;
using Base.Eventus.Filters;
using Base.Levels;
using Base.Utils;
using Epic.OnlineServices.RTCAudio;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.Levels.ActorDeployment;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Geoscape.View.DataObjects;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.ActorsInstance;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.StructuralTargets;
using PhoenixPoint.Tactical.Eventus.Filters;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.ActorDeployment;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using PhoenixPoint.Tactical.Levels.Mist;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TFTV
{

    public class CustomLerper
    {
        public Vector3 RaisedPos;

        public Vector3 LowerdPos;

        public Quaternion RaisedRot;

        public Quaternion LowerdRot;

        public Vector3 LerpRaise(Vector3 initPos, float t)
        {
            return Vector3.Lerp(initPos, RaisedPos, t);
        }

        public Vector3 LerpLower(Vector3 initPos, float t)
        {
            return Vector3.Lerp(initPos, LowerdPos, t);
        }

        public void LerpRaiseRot(Vector3 initPos, Quaternion initRot, float t, out Vector3 pos, out Quaternion rot)
        {
            pos = Vector3.Lerp(initPos, RaisedPos, t);
            rot = Quaternion.Lerp(initRot, RaisedRot, t);
        }

        public void LerpLowerRot(Vector3 initPos, Quaternion initRot, float t, out Vector3 pos, out Quaternion rot)
        {
            pos = Vector3.Lerp(initPos, LowerdPos, t);
            rot = Quaternion.Lerp(initRot, LowerdRot, t);
        }
    }


    internal class TFTVPalaceMission
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        // private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;

        // private static readonly GameTagDef revenantTier1GameTag = DefCache.GetDef<GameTagDef>("RevenantTier_1_GameTagDef");
        private static readonly GameTagDef revenantTier2GameTag = DefCache.GetDef<GameTagDef>("RevenantTier_2_GameTagDef");
        private static readonly GameTagDef revenantTier3GameTag = DefCache.GetDef<GameTagDef>("RevenantTier_3_GameTagDef");
        private static readonly GameTagDef anyRevenantGameTag = DefCache.GetDef<GameTagDef>("Any_Revenant_TagDef");

        private static readonly ClassTagDef crabTag = DefCache.GetDef<ClassTagDef>("Crabman_ClassTagDef");
        private static readonly ClassTagDef fishmanTag = DefCache.GetDef<ClassTagDef>("Fishman_ClassTagDef");
        private static readonly ClassTagDef sirenTag = DefCache.GetDef<ClassTagDef>("Siren_ClassTagDef");
        private static readonly ClassTagDef chironTag = DefCache.GetDef<ClassTagDef>("Chiron_ClassTagDef");
        private static readonly ClassTagDef acheronTag = DefCache.GetDef<ClassTagDef>("Acheron_ClassTagDef");
        private static readonly ClassTagDef queenTag = DefCache.GetDef<ClassTagDef>("Queen_ClassTagDef");

        private static readonly List<ClassTagDef> RevenantEligibleClasses = new List<ClassTagDef>() { crabTag, fishmanTag, sirenTag, chironTag, acheronTag, queenTag };

        private static readonly string ObjectiveName1 = "Objective1";
        private static readonly string ObjectiveName2 = "Objective2";
        private static readonly string ObjectiveName3 = "Objective3";

        private static readonly Vector3 ObjectivePosition1 = new Vector3(5.5f, 0.0f, 42.5f);
        private static readonly Vector3 ObjectivePosition2 = new Vector3(8.5f, 0.0f, 42.5f);
        private static readonly Vector3 ObjectivePosition3 = new Vector3(11.5f, 0.0f, 42.5f);

        private static List<string> ActivatedObjectives = new List<string>();


        //        private static readonly DelayedEffectStatusDef ReinforcementStatus = DefCache.GetDef<DelayedEffectStatusDef>("E_Status [ReinforcementStatus]");

        private static readonly DelayedEffectStatusDef reinforcementStatusUnder1AP = DefCache.GetDef<DelayedEffectStatusDef>("E_Status [ReinforcementStatusUnder1AP]");
        private static readonly DelayedEffectStatusDef reinforcementStatus1AP = DefCache.GetDef<DelayedEffectStatusDef>("E_Status [ReinforcementStatus1AP]");
        private static readonly DelayedEffectStatusDef reinforcementStatusUnder2AP = DefCache.GetDef<DelayedEffectStatusDef>("E_Status [ReinforcementStatusUnder2AP]");
        private static readonly DamageMultiplierStatusDef RecepctacleDisrupted = DefCache.GetDef<DamageMultiplierStatusDef>("YR_Disrupted");

        public static void CheckIfPlayerCloseToGate(TacticalActor tacticalActor)
        {
            try
            {

                TacticalLevelController controller = tacticalActor.TacticalLevel;

                if (controller != null && controller.TacMission.MissionData.IsFinalMission && tacticalActor.IsControlledByPlayer)
                {
                    //  TFTVLogger.Always($"passed if check");

                    if (tacticalActor.Pos.z <= 60)
                    {
                        //    TFTVLogger.Always($"passed second if check");

                        TFTVTutorialAndStory.ShowStoryPanel(controller, "ReceptacleGateHint0", "ReceptacleGateHint1");

                        //  GateHintShown = true;

                    }
                }



            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;

            }
        }


        //  private static int TurnsBeforeReceptacleResurrects = 2;



        //Structural Target must spawn every time mission is started or game is loaded
        //Console_Activated status prevents interaction with objective; this status must be removed if gates raise again
        //If at start of turn one of the consoles is not in the process of being activated, hacking being carried at other consoles must stop.


        /// <summary>
        /// This is run on TacticalStart, which cover starting the mission, loading a game, and restarting the mission
        /// </summary>


        public static void CheckPalaceMission()
        {
            try
            {
                TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                if (controller.TacMission.IsFinalMission)
                {
                    TFTVLogger.Always($"Instantiating gate objectives on Mission Start, Load or Restart");
                    SpawnInteractionPoints(controller);
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        internal static void SpawnInteractionPoints(TacticalLevelController controller)
        {
            try
            {
                TacticalActorYuggoth tacticalActorYuggoth = controller.Map.GetActors<TacticalActorYuggoth>().FirstOrDefault();

                int turnGatesLowered = -1;



                if (tacticalActorYuggoth != null)
                {
                    turnGatesLowered = tacticalActorYuggoth.QueenWallDownOnTurn;
                }

                TFTVLogger.Always($"yuggoth found? {tacticalActorYuggoth != null} gates lowered on turn {turnGatesLowered} (how many yuggoths found? {controller.Map.GetActors<TacticalActorYuggoth>().Count()}");


                if (turnGatesLowered == -1)
                {
                    Dictionary<string, Vector3> objectives = new Dictionary<string, Vector3>
                {
                    { ObjectiveName1, ObjectivePosition1 },
                    { ObjectiveName2, ObjectivePosition2 },
                    { ObjectiveName3, ObjectivePosition3 }
                };


                    for (int x = 0; x < objectives.Count(); x++)
                    {
                        string name = objectives.Keys.ToList()[x];
                        Vector3 position = objectives[name];

                        StructuralTarget existingObjective = UnityEngine.Object.FindObjectsOfType<StructuralTarget>().FirstOrDefault(b => b.name.Equals(name));

                        TFTVLogger.Always($"{name} objective exists? {existingObjective != null}. Position? {existingObjective?.Pos} should be {position}");

                        if (existingObjective != null && existingObjective.Pos != new Vector3(0, 0, 0))
                        {
                            TFTVLogger.Always($"{existingObjective.name} already exists");

                        }
                        else
                        {

                            StructuralTargetDeploymentDef stdDef = DefCache.GetDef<StructuralTargetDeploymentDef>("HackableConsoleStructuralTargetDeploymentDef");

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

                            var ipCols = new GameObject("InteractionPointColliders");
                            ipCols.transform.SetParent(obj.transform);
                            ipCols.tag = InteractWithObjectAbilityDef.ColliderTag;

                            ipCols.transform.SetPositionAndRotation(position, Quaternion.identity);
                            var collider = ipCols.AddComponent<BoxCollider>();


                            structuralTarget.Initialize();
                            //TFTVLogger.Always($"Spawning interaction point with name {name} at position {position}");
                            structuralTarget.DoEnterPlay();

                            TFTVLogger.Always($"structural target {name} spawned anew at position {position}");
                        }
                    }

                    CheckStatusInteractionPoints(controller);
                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }
        }


        /// <summary>
        /// This attempts to assign correct status to objective
        /// </summary>
        internal static void CheckStatusInteractionPoints(TacticalLevelController controller)
        {
            try
            {
                Dictionary<string, Vector3> objectives = new Dictionary<string, Vector3>
                {
                    { ObjectiveName1, ObjectivePosition1 },
                    { ObjectiveName2, ObjectivePosition2 },
                    { ObjectiveName3, ObjectivePosition3 }
                };

                StatusDef activeHackableChannelingStatusDef = DefCache.GetDef<StatusDef>("ForceGateOnObjectiveStatus");
                StatusDef hackingStatusDef = DefCache.GetDef<StatusDef>("ForcingGateOnActorStatus");
                StatusDef consoleToActorBridgingStatusDef = DefCache.GetDef<StatusDef>("ObjectiveToActorBridgeStatus");
                StatusDef actorToConsoleBridgingStatusDef = DefCache.GetDef<StatusDef>("ActorToObjectiveBridgeStatus");
                StatusDef activatedStatusDef = DefCache.GetDef<StatusDef>("ConsoleActivated_StatusDef");

                foreach (string objective in objectives.Keys)
                {
                    string name = objective;
                    Vector3 position = objectives[name];


                    StructuralTarget structuralTarget = UnityEngine.Object.FindObjectsOfType<StructuralTarget>().FirstOrDefault(b => b.name.Equals(name));

                    TacticalActor tacticalActor = controller.Map.FindActorOverlapping(position);

                    if (tacticalActor != null && tacticalActor.HasStatus(hackingStatusDef))
                    {

                        void reflectionSet(TacStatus status, int value)
                        {
                            var prop = status.GetType().GetProperty("TurnApplied", BindingFlags.Public | BindingFlags.Instance);
                            prop.SetValue(status, value);
                        }

                        TacStatus actorBridge = (TacStatus)tacticalActor.Status.GetStatusByName(actorToConsoleBridgingStatusDef.EffectName);
                        int turnApplied = actorBridge.TurnApplied;

                        tacticalActor.Status.UnapplyStatus(actorBridge);
                        //  TFTVLogger.Always($"{actorToConsoleBridgingStatusDef.EffectName} unapplied");
                        TacStatus newTargetBridge = (TacStatus)structuralTarget.Status.ApplyStatus(consoleToActorBridgingStatusDef, tacticalActor.GetActor());
                        TacStatus newActorBridge = (TacStatus)tacticalActor.Status.GetStatusByName(actorToConsoleBridgingStatusDef.EffectName);

                        TFTVLogger.Always($"found {tacticalActor?.DisplayName} trying to open Gate, {name} at {position}");

                        reflectionSet(newTargetBridge, turnApplied);
                        reflectionSet(newActorBridge, turnApplied);

                    }
                    else
                    {
                        TFTVLogger.Always($"are we here? has the activated status?{structuralTarget.Status.HasStatus(activatedStatusDef) == true} has the activatable status? {structuralTarget.Status.HasStatus(activeHackableChannelingStatusDef) == true} ");

                        if (!structuralTarget.Status.HasStatus(activatedStatusDef) && !structuralTarget.Status.HasStatus(activeHackableChannelingStatusDef))
                        {
                            structuralTarget.Status.ApplyStatus(activeHackableChannelingStatusDef);//(activeConsoleStatusDef);

                            TFTVLogger.Always($"applying {activeHackableChannelingStatusDef.name} at position {position}");

                        }
                    }
                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }
        }




        /// <summary>
        /// This checks AT THE START OF PHOENIX TURN if any one of the objectives is not being activated, and if so, cancels activating the rest.
        /// </summary>

        private static void CheckGateObjectives()
        {
            try
            {
                TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                StatusDef hackingStatusDef = DefCache.GetDef<StatusDef>("ForcingGateOnActorStatus");
                ApplyEffectAbilityDef cancelForcingGateAbility = DefCache.GetDef<ApplyEffectAbilityDef>("CancelYuggothianGateAbility");


                // StatusDef hackingStatusDef = DefCache.GetDef<StatusDef>("Hacking_Channeling_StatusDef");
                // ApplyEffectAbilityDef cancelForcingGateAbility = DefCache.GetDef<ApplyEffectAbilityDef>("Hacking_Cancel_AbilityDef");

                Dictionary<string, Vector3> keyValuePairs = new Dictionary<string, Vector3>
                {
                    { ObjectiveName1, ObjectivePosition1 },
                    { ObjectiveName2, ObjectivePosition2 },
                    { ObjectiveName3, ObjectivePosition3 }
                };

                bool objectiveInactive = false;



                foreach (string objective in keyValuePairs.Keys)
                {
                    TacticalActor tacticalActor = controller.Map.FindActorOverlapping(keyValuePairs[objective]);
                    if (tacticalActor != null && tacticalActor.HasStatus(hackingStatusDef))
                    {

                    }
                    else
                    {

                        objectiveInactive = true;

                    }

                }

                if (objectiveInactive)
                {

                    foreach (string objective in keyValuePairs.Keys)
                    {
                        TacticalActor tacticalActor = controller.Map.FindActorOverlapping(keyValuePairs[objective]);
                        if (tacticalActor != null && tacticalActor.HasStatus(hackingStatusDef))
                        {
                            TFTVLogger.Always($"canceling hacking");
                            tacticalActor.GetAbilityWithDef<ApplyEffectAbility>(cancelForcingGateAbility).Activate();

                        }


                    }

                    // SpawnInteractionPoints(controller);

                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }



        private static int CountReceptacleEyes(TacticalActorYuggoth receptacle)
        {
            try
            {
                int disabledEyes = receptacle.BodyState.GetAllBodyparts().Where(x => x.BodyPartAspectDef.name.Contains("Yugothian_Eye") && !x.Enabled).Count();

                return disabledEyes;


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

        [HarmonyPatch(typeof(RemoveStatusFromOneRandomEnemyEffect), "OnApply")]
        public static class TFTV_RemoveStatusFromOneRandomEnemyEffect_OnApply_patch
        {
            public static void Postfix(RemoveStatusFromOneRandomEnemyEffect __instance, EffectTarget target)
            {
                try
                {
                    TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                    if (controller.TacMission.IsFinalMission && !controller.IsLoadingSavedGame &&
                        __instance.RemoveStatusFromOneRandomEnemyEffectDef == DefCache.GetDef<RemoveStatusFromOneRandomEnemyEffectDef>("E_RemoveMarkOfTheVoid [RemoveMarkOfTheVoid_EffectEventDef]"))
                    {
                        TacticalActorYuggoth actor = controller.Map.GetActors<TacticalActorYuggoth>().Where(a => a.IsAlive).First();

                        int receptacleEyesDamaged = CountReceptacleEyes(actor);
                        TFTVLogger.Always($"Receptacle eye disabled! Eyes disabled count: {receptacleEyesDamaged}");

                        if (receptacleEyesDamaged >= Math.Max(TFTVReleaseOnly.DifficultyOrderConverter(controller.Difficulty.Order), 4))
                        {

                            actor.ApplyDamage(new DamageResult() { HealthDamage = 2000000 });
                            GiveAllPhoenixOperativesReceptacleDistrupredStatus(controller);

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

        /// <summary>
        /// ReceptacleDistruped status, which has a 2 turn duration, is used as a turn counter before Receptacle is "resurrected".
        /// It's given to all Phoenix operatives that are alive when YR is "killed", and it is checked when spawning a new Receptacle:
        /// if no Receptacle is alive, and nobody has the status, the YR is spawned anew.
        /// </summary>
        private static void GiveAllPhoenixOperativesReceptacleDistrupredStatus(TacticalLevelController controller)
        {
            try
            {
                foreach (TacticalActor tacticalActor in controller.GetFactionByCommandName("px").TacticalActors.Where(ta => ta.IsAlive))
                {

                    tacticalActor.Status.ApplyStatus(RecepctacleDisrupted);
                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static bool CheckPhoenixOperativesReceptacleDistrupredStatus(TacticalLevelController controller)
        {
            try
            {
                return controller.GetFactionByCommandName("px").TacticalActors.Where(ta => ta.IsAlive).Any(ta => ta.Status.HasStatus(RecepctacleDisrupted));

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static void ReSpawnYuggoth()
        {
            try
            {
                TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                if (!controller.Map.GetActors<TacticalActorYuggoth>().Any(a => a.IsAlive) && !CheckPhoenixOperativesReceptacleDistrupredStatus(controller))
                {
                    //  YuggothShieldsAbility yuggothShieldsAbility = controller.Map.GetActors<TacticalActorYuggoth>().First().GetAbility<YuggothShieldsAbility>();
                    //   yuggothShieldsAbility.RestoreShields();

                    int gateWentDownOnTurn = -1;

                    foreach (TacticalActorYuggoth tacticalActorYuggoth in controller.Map.GetActors<TacticalActorYuggoth>().Where(a => !a.IsAlive))
                    {
                        gateWentDownOnTurn = tacticalActorYuggoth.QueenWallDownOnTurn;
                        tacticalActorYuggoth.gameObject.SetActive(false);

                    }

                    TFTVLogger.Always($"gate recorded as going down on turn {gateWentDownOnTurn}; should apply to new Receptacle");

                    TacticalDeployZone tacticalDeployZone = FindTDZ("Deploy_Yuggothian_Resident_3x3");
                    Level level = controller.Level;
                    TacticalVoxelMatrix tacticalVoxelMatrix = level?.GetComponent<TacticalVoxelMatrix>();
                    MethodInfo spawnBlob = AccessTools.Method(typeof(TacticalVoxelMatrix), "SpawnBlob_Internal");
                    //spawnBlob.Invoke(tacticalVoxelMatrix, new object[] { TacticalVoxelType.Empty, zone.Pos + Vector3.up * -1.5f, 3, 1, false, true });



                    // TFTVLogger.Always($"pXOperative to be ghosted {pXOperative.DisplayName} at pos {position}");
                    spawnBlob.Invoke(tacticalVoxelMatrix, new object[] { TacticalVoxelType.Mist, tacticalDeployZone.Pos, 3, 1, false, true });


                    TacCharacterDef enemyToSpawn = DefCache.GetDef<TacCharacterDef>("YugothianMain_TacCharacterDef");

                    // TFTVLogger.Always($"is tdz for receptacle null? {tacticalDeployZone==null}");
                    ActorDeployData actorDeployData = enemyToSpawn.GenerateActorDeployData();

                    actorDeployData.InitializeInstanceData();

                    TacticalActorYuggoth currentReceptacle = (TacticalActorYuggoth)TacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, tacticalDeployZone.TacticalFaction.TacticalFactionDef, tacticalDeployZone.MissionParticipant, tacticalDeployZone.Pos, tacticalDeployZone.transform.rotation, tacticalDeployZone);

                    currentReceptacle.QueenWallDownOnTurn = gateWentDownOnTurn;

                    TFTVLogger.Always($"gateWentDown recorded as {currentReceptacle.QueenWallDownOnTurn}");

                    //   YuggothShieldsAbility yuggothShieldsAbility = currentReceptacle.GetAbility<YuggothShieldsAbility>();
                    //   currentReceptacle.LoweredShield = 4;
                    //  currentReceptacle.ShieldsAbility.LowerAllShields();
                    //   currentReceptacle.ShieldsAbility.InitShields();

                    //  RaiseReceptacleShields(currentReceptacle);

                    /*   foreach(GameObject gameObject in currentReceptacle.Shields) 
                       {
                           TFTVLogger.Always($"shield {gameObject.transform.position} and rotation {gameObject.transform.rotation}");

                       }*/

                    // yuggothShieldsAbility.InitShields();

                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }


        }


        public static void ForceSpecialCharacterPortraitInSetupProperPortrait(TacticalActor actor)
        {
            try
            {
                
             

                if (actor.TacticalLevel != null && actor.TacticalLevel.TacMission != null && actor.TacticalLevel.TacMission.IsFinalMission)
                {
                    List<GameTagDef> gameTagsToCheck = new List<GameTagDef>()
                {
                DefCache.GetDef<GameTagDef>("TaxiarchNergal_TacCharacterDef_GameTagDef"),
                DefCache.GetDef<GameTagDef>("Zhara_TacCharacterDef_GameTagDef"),
                DefCache.GetDef<GameTagDef>("Stas_TacCharacterDef_GameTagDef"),
                DefCache.GetDef<GameTagDef>("Nikolai_TacCharacterDef_GameTagDef"),
                DefCache.GetDef<GameTagDef>("Richter_TacCharacterDef_GameTagDef"),
                DefCache.GetDef<GameTagDef>("Harlson_TacCharacterDef_GameTagDef"),
                DefCache.GetDef<GameTagDef>("Sofia_TacCharacterDef_GameTagDef"),
                DefCache.GetDef<GameTagDef>("Exalted_ClassTagDef")

                };

                    if (gameTagsToCheck.Any(gt => actor.GameTags.Contains(gt)))

                    {
                        //  TFTVLogger.Always($"ForceSpecialCharacterPortraitInSetupProperPortrait actor is {actor.name}");
                        actor.TacticalActorView.TacticalActorViewDef.PortraitSource = TacticalActorViewDef.PortraitMode.ManualPortrait;

                    }
                    else
                    {
                        // TFTVLogger.Always($"ForceSpecialCharacterPortraitInSetupProperPortrait rendered actor is {actor.name}");
                        actor.TacticalActorView.TacticalActorViewDef.PortraitSource = TacticalActorViewDef.PortraitMode.RenderedPortrait;

                    }
                }


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }



        }





        private static IEnumerator<NextUpdate> RaiseQueensWallCrt(PlayingAction action)
        {

            TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

            TacticalActorYuggoth actor = controller.Map.GetActors<TacticalActorYuggoth>().First();

            YuggothShieldsAbility yuggothShieldsAbility = actor.GetAbility<YuggothShieldsAbility>();

            MethodInfo GetQueensWallCollidersMethod = typeof(YuggothShieldsAbility).GetMethod("GetQueensWallColliders", BindingFlags.NonPublic | BindingFlags.Instance);

            Collider[] queenColliders = (Collider[])GetQueensWallCollidersMethod.Invoke(yuggothShieldsAbility, null);

            CustomLerper queensWallLerp = new CustomLerper();

            Transform transform = actor.QueensWall.transform;

            queensWallLerp.LowerdPos = transform.position;




            if (Boxify.GetBoundsOfColliders(queenColliders, out var result))
            {
                queensWallLerp.RaisedPos = transform.position - new Vector3(0f, 0f - result.size.y, 0f);
            }
            // TFTVLogger.Always($"raised position {queensWallLerp.RaisedPos}"); // raised position(8.2, -14.8, 43.0)





            TacticalNavObstaclesHolder queensWallsNavObstaclesHolder = actor.QueensWall.GetComponent<TacticalNavObstaclesHolder>();




            MethodInfo playShieldMovedEventMethod = typeof(YuggothShieldsAbility).GetMethod("PlayShieldMovedEvent",
        BindingFlags.NonPublic | BindingFlags.Instance);



            playShieldMovedEventMethod.Invoke(yuggothShieldsAbility, new object[] { actor.QueensWall.transform });

            Vector3 initialPos = actor.QueensWall.transform.position;

            //  TFTVLogger.Always($"wall initial position: {initialPos}");


            //wall initial position: (8.2, -7.4, 43.0)

            float t = 0f;
            while (t <= 1f)
            {
                t += Time.deltaTime * 0.5f;
                float t2 = Mathf.Min(t, 1f);
                actor.QueensWall.transform.position = queensWallLerp.LerpRaise(initialPos, t2);
                yield return NextUpdate.NextFrame;
            }

            if (queensWallsNavObstaclesHolder != null)
            {
                queensWallsNavObstaclesHolder.InitNavObstacles();
            }

            MethodInfo updateMapMethod = typeof(YuggothShieldsAbility).GetMethod("UpdateMapForQueensWall", BindingFlags.NonPublic | BindingFlags.Instance);
            updateMapMethod.Invoke(yuggothShieldsAbility, null);


        }

        public static void RaiseQueensWall(TacticalActorYuggoth actor, YuggothShieldsAbility yuggothShieldsAbility)
        {
            try
            {

                actor.QueenWallDownOnTurn = actor.TacticalFaction.TurnNumber;
                if (actor.TacticalFaction != actor.TacticalLevel.CurrentFaction)
                {
                    actor.QueenWallDownOnTurn++;
                }

                actor.TacticalActorViewBase.DoCameraChaseParam(actor.QueensWall.transform);
                yuggothShieldsAbility.PlayAction(RaiseQueensWallCrt, null);

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CheckTimeToLowerGatesAgain()
        {
            try
            {
                TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                if (controller.TacMission.IsFinalMission && controller)
                {
                    TacticalActorYuggoth tacticalActorYuggoth = controller.Map.GetActors<TacticalActorYuggoth>().FirstOrDefault();

                    int turnGatesLowered = -1;
                    bool gatesToBeRaised = false;

                    if (tacticalActorYuggoth != null)
                    {
                        turnGatesLowered = tacticalActorYuggoth.QueenWallDownOnTurn;

                        TFTVLogger.Always($"gates lowered on turn {turnGatesLowered}");

                        if (controller.TurnNumber - turnGatesLowered == 2)
                        {
                            gatesToBeRaised = true;

                        }
                    }



                    /*   if (TurnsBeforeGatesRaised > 0)
                       {
                           TurnsBeforeGatesRaised -= 1;
                           TFTVLogger.Always($"Counter going down, now {TurnsBeforeGatesRaised}.");
                       }*/

                    if (gatesToBeRaised)
                    {

                        TacticalActorYuggoth actor = controller.Map.GetActors<TacticalActorYuggoth>().First();

                        YuggothShieldsAbility yuggothShieldsAbility = actor.GetAbility<YuggothShieldsAbility>();

                        RaiseQueensWall(actor, yuggothShieldsAbility);
                        KillStuffOnGatePath();
                        TFTVLogger.Always($"Counter expired! Time to raise the Gates again.");
                        RemoveActivatedStatusFromObjectives(controller);
                        tacticalActorYuggoth.QueenWallDownOnTurn = -1;
                        SpawnInteractionPoints(controller);
                    }
                }

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        private static void KillStuffOnGatePath()
        {
            try
            {
                TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                List<Vector3> deathVectors = new List<Vector3>()
                {
                   new Vector3 (14.5f, 0.0f, 43.5f),
                   new Vector3 (13.5f, 0.0f, 43.5f),
                   new Vector3 (12.5f, 0.0f, 42.5f),
                   new Vector3 (11.5f, 0.0f, 41.5f),
                   new Vector3 (10.5f, 0.0f, 41.5f),
                   new Vector3 (9.5f, 0.0f, 41.5f),
                   new Vector3 (8.5f, 0.0f, 41.5f),
                   new Vector3 (7.5f, 0.0f, 41.5f),
                   new Vector3 (6.5f, 0.0f, 41.5f),
                   new Vector3 (5.5f, 0.0f, 41.5f),
                   new Vector3 (4.5f, 0.0f, 42.5f),
                   new Vector3 (3.5f, 0.0f, 43.5f),
                   new Vector3 (2.5f, 0.0f, 43.5f),
                   new Vector3 (3.5f, 0.0f, 41.5f),
                   new Vector3 (13.5f, 0.0f, 41.5f),
                };

                for (int x = 0; x < deathVectors.Count; x++)
                {

                    TacticalActor tacticalActor = controller.Map.FindActorOverlapping(deathVectors[x]);

                    if (tacticalActor != null)
                    {

                        TFTVLogger.Always($"{tacticalActor.name} is on the gate's path as it raised! It should be killed!");

                        tacticalActor.ApplyDamage(new DamageResult() { HealthDamage = 10000 });
                    }


                }




            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }

        }


        public static void PalaceConsoleActivated(StatusComponent statusComponent, Status status, TacticalLevelController controller)
        {
            try
            {
                if (controller != null && controller.TacMission.IsFinalMission && !controller.IsLoadingSavedGame)
                {
                    if (status.Def == DefCache.GetDef<StatusDef>("ConsoleActivated_StatusDef"))
                    {


                        StructuralTarget console = statusComponent.transform.GetComponent<StructuralTarget>();
                        TacticalActorYuggoth yuggoth = controller.Map.GetActors<TacticalActorYuggoth>().First();
                        YuggothShieldsAbility lowerShields = yuggoth.GetAbility<YuggothShieldsAbility>();

                        TFTVLogger.Always($"console name {console.name} at position {console.Pos}");

                        if (console.Pos == new Vector3(0.5f, 0.0f, -15.5f))
                        {
                            //  TFTVGoo.DontUseGooNavigationPatch = true;
                        }
                        else if (console.Pos.z == 0)
                        {
                            TFTVLogger.Always($"Console {console.name} activated");

                            ActivatedObjectives.Add(ObjectiveName1);

                            if (ActivatedObjectives.Count() == 3)
                            {
                                TFTVLogger.Always($"All consoles activated! Lowering the gate!");
                                lowerShields.LowerQueensWall();
                                ActivatedObjectives.Clear();

                                TFTVLogger.Always($"shields down on turn {yuggoth.QueenWallDownOnTurn}");

                                // TurnsBeforeGatesRaised = 2;
                                //  SpawnReinforcements(QueenReinforcementsSpawn);
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

        /// <summary>
        /// Prevent Scylla death lowering palace walls.
        /// </summary>

        [HarmonyPatch(typeof(YuggothShieldsAbility), "OnSomeoneDied")]

        public static class TFTV_YuggothShieldsAbility_OnSomeoneDied_patch
        {
            public static bool Prefix()
            {
                try
                {
                    return false;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }



        private static void RemoveActivatedStatusFromObjectives(TacticalLevelController controller)
        {
            try
            {
                Dictionary<string, Vector3> objectives = new Dictionary<string, Vector3>
                {
                    { ObjectiveName1, ObjectivePosition1 },
                    { ObjectiveName2, ObjectivePosition2 },
                    { ObjectiveName3, ObjectivePosition3 }
                };

                StatusDef activatedStatusDef = DefCache.GetDef<StatusDef>("ConsoleActivated_StatusDef");


                foreach (string objective in objectives.Keys)
                {
                    string name = objective;
                    Vector3 position = objectives[name];


                    StructuralTarget structuralTarget = UnityEngine.Object.FindObjectsOfType<StructuralTarget>().FirstOrDefault(b => b.name.Equals(name));

                    TacticalActor tacticalActor = controller.Map.FindActorOverlapping(position);



                    if (structuralTarget.Status.HasStatus(activatedStatusDef))
                    {
                        structuralTarget.Status.UnapplyStatus(structuralTarget.Status.GetStatusByName(activatedStatusDef.EffectName));//(activeConsoleStatusDef);

                        TFTVLogger.Always($"removing {activatedStatusDef.name} at position {position}");

                    }

                }



            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }
        }





        private static TacCharacterDef GenerateRandomMyrmidonReinforcements(TacticalLevelController controller)
        {
            try
            {


                ClassTagDef myrmidonTag = DefCache.GetDef<ClassTagDef>("Swarmer_ClassTagDef");


                List<TacCharacterDef> availableTemplatesOrdered = new List<TacCharacterDef>(controller.TacMission.MissionData.UnlockedAlienTacCharacterDefs.OrderByDescending(tcd => tcd.DeploymentCost));

                List<TacCharacterDef> myrmidonOrdered = new List<TacCharacterDef>(availableTemplatesOrdered.Where(t => t.ClassTag == myrmidonTag));


                TacCharacterDef pickedEnemy = myrmidonOrdered.GetRandomElement();


                return pickedEnemy;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static TacCharacterDef GenerateRandomGruntAndSirenReinforcements(TacticalLevelController controller)
        {
            try
            {

                TacCharacterDef pickedEnemy = null;

                ClassTagDef sirenTag = DefCache.GetDef<ClassTagDef>("Siren_ClassTagDef");
                ClassTagDef myrmidonTag = DefCache.GetDef<ClassTagDef>("Swarmer_ClassTagDef");
                ClassTagDef mindfraggerTag = DefCache.GetDef<ClassTagDef>("Facehugger_ClassTagDef");

                ClassTagDef crabTag = DefCache.GetDef<ClassTagDef>("Crabman_ClassTagDef");
                ClassTagDef fishTag = DefCache.GetDef<ClassTagDef>("Fishman_ClassTagDef");

                List<TacCharacterDef> availableTemplatesOrdered = new List<TacCharacterDef>(controller.TacMission.MissionData.UnlockedAlienTacCharacterDefs.OrderByDescending(tcd => tcd.DeploymentCost));

                List<TacCharacterDef> crabsOrdered = new List<TacCharacterDef>(availableTemplatesOrdered.Where(t => t.ClassTag == crabTag));
                List<TacCharacterDef> tritonsOrdered = new List<TacCharacterDef>(availableTemplatesOrdered.Where(t => t.ClassTag == fishTag));
                List<TacCharacterDef> sirensOrdered = new List<TacCharacterDef>(availableTemplatesOrdered.Where(t => t.ClassTag == sirenTag));

                UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());

                int roll = UnityEngine.Random.Range(0, 3);

                if (roll == 0)
                {

                    pickedEnemy = crabsOrdered.GetRandomElement();

                }
                else if (roll == 1)
                {
                    pickedEnemy = tritonsOrdered.GetRandomElement();

                }
                else
                {
                    pickedEnemy = sirensOrdered.GetRandomElement();

                }

                return pickedEnemy;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static void MoveDeploymentZoneAndSpawnEnemy(TacticalDeployZone tacticalDeployZone, Vector3 position, int turn, TacCharacterDef tacCharacterDef)
        {
            try
            {

                tacticalDeployZone.SetPosition(position);
                tacticalDeployZone.FixedDeployment = new List<FixedDeployConditionData> { CreateFixedDeployConditionData(turn, tacCharacterDef) };


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }
        }

        private static void ClearAndMoveReinforcementDeploymentZone(TacticalDeployZone tacticalDeployZone, Vector3 position)
        {
            try
            {
                tacticalDeployZone.FixedDeployment.Clear();

                tacticalDeployZone.SetPosition(position);
                //   tacticalDeployZone.FixedDeployment = GenerateRandomDeplyConditionData(controller);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }

        }


        private static FixedDeployConditionData CreateFixedDeployConditionData(int turn, TacCharacterDef tacCharacterDef)
        {
            try
            {
                FixedDeployConditionData fixedDeployConditionData = new FixedDeployConditionData() { TurnNumber = turn, TacActorDef = tacCharacterDef };

                return fixedDeployConditionData;


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }




        private static readonly string QueenReinforcementsSpawn1 = "Deploy_Resident_5x5";
        private static readonly Vector3 QueenPosition1 = new Vector3(-18.5f, 0.0f, 94.5f);

        private static readonly string ChironSpawn1 = "Deploy_Resident_5x5 (1)";
        private static readonly Vector3 ChironPosition1 = new Vector3(26.5f, 0.0f, 103.5f);

        private static readonly string SniperSpawn1 = "Deploy_Resident_3x3 (4)";
        private static readonly Vector3 SniperPosition1 = new Vector3(21.5f, 5.5f, 73.5f);
        private static readonly string SniperSpawn2 = "Deploy_Resident_3x3 (5)";
        private static readonly Vector3 SniperPosition2 = new Vector3(19.5f, 5.5f, 53.5f);
        private static readonly string SniperSpawn3 = "Deploy_Resident_3x3 (6)";
        private static readonly Vector3 SniperPosition3 = new Vector3(-1.5f, 7.9f, 53.5f);
        private static readonly string RightBottomSpawn = "Deploy_Resident_3x3";
        private static readonly Vector3 RightBottomPosition = new Vector3(-11.5f, -2.4f, 73.5f);
        private static readonly string RightTopSpawn = "Deploy_Resident_3x3 (1)";
        private static readonly Vector3 RightTopPosition = new Vector3(-8.5f, -2.4f, 54.5f);

        //  private static readonly string ShouldNotBeUsedSpawn = "Deploy_Resident_1x1_Grunt_Elite_and_Tiny";
        //  private static readonly Vector3 ShouldNotBeUsedPosition = new Vector3(9.0f, 0.0f, 62.0f);

        private static readonly string LeftBottomSpawn = "Deploy_Resident_3x3 (7)";
        private static readonly Vector3 LeftBottomPosition = new Vector3(31.5f, -2.4f, 74.5f);

        //  private static readonly string PlayerGruntSpawn = "Deploy_Player_1x1_Elite_Grunt_Drone";
        //  private static readonly Vector3 PlayerGruntPosition = new Vector3(10.5f, 0.0f, 89.5f);

        private static readonly Vector3 altEggPosition0 = new Vector3(-4.5f, 0.0f, 75.5f);
        private static readonly Vector3 altEggPosition1 = new Vector3(-1.5f, 0.0f, 72.5f);


        public static void CheckFinalMissionWinConditionWhereDeployingItem(TacticalActorBase tacticalActorBase, TacticalLevelController controller)
        {
            try
            {

                TacticalActorBaseDef injectorBomb = DefCache.GetDef<TacticalActorBaseDef>("InjectorBomb_ActorDef");
                //  TFTVLogger.Always($"deploying {tacticalActorBase.TacticalActorBaseDef.name}");

                if (tacticalActorBase.TacticalActorBaseDef == injectorBomb)
                {
                    // TFTVLogger.Always($"");
                    TacticalFaction phoenix = controller.GetFactionByCommandName("px");

                    phoenix.State = TacFactionState.Won;


                    foreach (FactionObjective factionObjective in phoenix.Objectives)
                    {

                        PropertyInfo stateProperty = typeof(FactionObjective).GetProperty("State", BindingFlags.Public | BindingFlags.Instance);

                        // Set the State property to FactionObjectiveState.Completed
                        stateProperty.SetValue(factionObjective, FactionObjectiveState.Achieved);

                        TFTVLogger.Always($"{factionObjective.GetDescription()} is completed {factionObjective.GetCompletion()}");


                    }

                    controller.GetFactionByCommandName("aln").State = TacFactionState.Defeated;
                    controller.GameOver();

                }




            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CheckFinalMissionWinConditionForExalted(TacticalAbility ability)
        {
            try
            {


                TacticalLevelController controller = GameUtl.CurrentLevel()?.GetComponent<TacticalLevelController>();
                if (controller != null)
                {

                    // TFTVLogger.Always($"deploying {tacticalActorBase.TacticalActorBaseDef.name}");

                    if (ability.TacticalAbilityDef == DefCache.GetDef<InteractWithObjectAbilityDef>("ExaltedInteractWithYuggothian_AbilityDef"))
                    {
                        // TFTVLogger.Always($"");
                        TacticalFaction phoenix = controller.GetFactionByCommandName("px");

                        phoenix.State = TacFactionState.Won;


                        foreach (FactionObjective factionObjective in phoenix.Objectives)
                        {

                            PropertyInfo stateProperty = typeof(FactionObjective).GetProperty("State", BindingFlags.Public | BindingFlags.Instance);

                            // Set the State property to FactionObjectiveState.Completed
                            stateProperty.SetValue(factionObjective, FactionObjectiveState.Achieved);

                            TFTVLogger.Always($"{factionObjective.GetDescription()} is completed {factionObjective.GetCompletion()}");


                        }

                        controller.GetFactionByCommandName("aln").State = TacFactionState.Defeated;
                        controller.GameOver();

                    }
                }



            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        [HarmonyPatch(typeof(InteractWithObjectAbility), "ActorHasRequiredItems")]
        public static class TFTV_InteractWithObjectAbility_ActorHasRequiredItems_Patch
        {
            public static void Postfix(InteractWithObjectAbility __instance, ref bool __result)
            {
                try
                {


                    if (__instance.InteractWithObjectAbilityDef == DefCache.GetDef<InteractWithObjectAbilityDef>("InteractWithYuggothian_AbilityDef") || __instance.InteractWithObjectAbilityDef == DefCache.GetDef<InteractWithObjectAbilityDef>("ExaltedInteractWithYuggothian_AbilityDef"))
                    {
                        if (__instance.TacticalActor.TacticalLevel.Map.GetActors<TacticalActorYuggoth>().Any(a => a.IsAlive))
                        {

                            __result = false;

                        }
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(InteractWithObjectAbility), "GetStatusToApplyOnActor")]
        public static class TFTV_InteractWithObjectAbility_GetStatusToApplyOnActor_Patch
        {
            public static void Postfix(InteractWithObjectAbility __instance, ref StatusDef __result)
            {
                try
                {


                    if (__instance.InteractWithObjectAbilityDef == DefCache.GetDef<InteractWithObjectAbilityDef>("InteractWithYuggothian_AbilityDef") || __instance.InteractWithObjectAbilityDef == DefCache.GetDef<InteractWithObjectAbilityDef>("ExaltedInteractWithYuggothian_AbilityDef"))
                    {

                    }
                    else
                    {
                        TFTVLogger.Always($"not interacting with Yuggoth! set result to null");
                        __result = null;

                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(InteractWithObjectAbility), "ActivateTarget")]
        public static class TFTV_InteractWithObjectAbility_ActivateTarget_Patch
        {
            public static bool Prefix(InteractWithObjectAbility __instance, TacticalAbilityTarget target)
            {
                try
                {
                    if (target == null)
                    {

                        TFTVLogger.Always($"there is no target to activate!");
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

        [HarmonyPatch(typeof(TacticalDeployZone), "GetOrderedSpawnPositions")]
        public static class TFTV_TacticalDeployZone_GetOrderedSpawnPositions_Patch
        {
            public static void Postfix(TacticalDeployZone __instance, ref List<Vector3> __result)
            {
                try
                {
                    if (__instance.TacticalLevel != null && __instance.TacticalLevel.TacMission != null && __instance.TacticalLevel.TacMission.IsFinalMission)
                    {


                        if (__instance.name == RightBottomSpawn || __instance.name == LeftBottomSpawn || __instance.name == RightTopSpawn)
                        {
                            List<Vector3> verifiedPositions = new List<Vector3>();

                            foreach (Vector3 vector3 in __result)
                            {
                                if (vector3.y == -2.4f)
                                {
                                    verifiedPositions.Add(vector3);
                                    //  TFTVLogger.Always($"{vector3} added to verified positions for spawning");
                                }
                            }

                            __result = verifiedPositions;

                        }
                    }


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        private static void SpawnJumpingArthronsReinforcements(TacticalLevelController controller)
        {
            try
            {
                int difficulty = controller.Difficulty.Order;

                if (controller.TurnNumber > 7 - difficulty && controller.TurnNumber % 2 != 0)
                {
                    UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());

                    int roll = TFTVCommonMethods.D12DifficultyModifiedRoll(controller.Difficulty.Order);

                    if (roll <= 6)
                    {
                        TacticalDeployZone tacticalDeployZone = FindTDZ(LeftBottomSpawn);

                        TacCharacterDef crabToSpawn = controller.TacMission.MissionData.UnlockedAlienTacCharacterDefs.Where(tc => tc.ClassTag == crabTag && tc.Data.BodypartItems.Contains(DefCache.GetDef<ItemDef>("Crabman_Legs_EliteAgile_ItemDef"))).ToList().GetRandomElement();
                        TFTVLogger.Always($"jumping crab to spawn: {crabToSpawn.name}");

                        ActorDeployData actorDeployData = crabToSpawn.GenerateActorDeployData();

                        actorDeployData.InitializeInstanceData();

                        TacticalActorBase tacticalActorBase = tacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, tacticalDeployZone);
                        TacticalActor tacticalActor = tacticalActorBase as TacticalActor;

                        if (tacticalActor != null)
                        {
                            if (difficulty < 6)
                            {

                                if (tacticalActor != null)
                                {
                                    if (TFTVArtOfCrab.Has1APWeapon(crabToSpawn))
                                    {

                                        tacticalActor.Status.ApplyStatus(reinforcementStatus1AP);
                                        TFTVLogger.Always($"{tacticalActor.name} receiving {reinforcementStatus1AP.EffectName}");

                                    }
                                    else
                                    {

                                        tacticalActor.Status.ApplyStatus(reinforcementStatusUnder2AP);
                                        TFTVLogger.Always($"{tacticalActor.name} receiving {reinforcementStatusUnder2AP.EffectName}");
                                    }

                                }
                                tacticalActor.AIActor.IsAlerted = true;
                                controller.ActorAlerted(tacticalActor);
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

        private static void SpawnMyrmidonReinforcements(string spawnName, TacticalLevelController controller)
        {
            try
            {
                int difficulty = controller.Difficulty.Order;

                if (controller.TurnNumber > 7 - difficulty && controller.TurnNumber % 2 == 0)
                {

                    UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());

                    int roll = TFTVCommonMethods.D12DifficultyModifiedRoll(controller.Difficulty.Order);

                    if (roll <= 6)
                    {

                        TacticalDeployZone tacticalDeployZone = FindTDZ(spawnName);
                        TacCharacterDef enemyToSpawn = GenerateRandomMyrmidonReinforcements(controller);


                        ActorDeployData actorDeployData = enemyToSpawn.GenerateActorDeployData();

                        actorDeployData.InitializeInstanceData();


                        TacticalActorBase tacticalActorBase = tacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, tacticalDeployZone);
                        TacticalActor tacticalActor = tacticalActorBase as TacticalActor;
                        if (tacticalActor != null)
                        {
                            if (difficulty < 6)
                            {
                                tacticalActor.Status.ApplyStatus(reinforcementStatus1AP);
                                TFTVLogger.Always($"{tacticalActor.name} receiving {reinforcementStatus1AP.EffectName}");
                            }

                            tacticalActor.AIActor.IsAlerted = true;
                            controller.ActorAlerted(tacticalActor);
                        }
                    }
                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }
        }

        private static bool GruntOrSirenReinforcementAlreadySpawnedThisTurn = false;

        private static void SpawnGruntAndSirenReinforcements(string spawnName, TacticalLevelController controller)
        {
            try
            {
                int difficulty = controller.Difficulty.Order;

                if (controller.TurnNumber > 2 && !GruntOrSirenReinforcementAlreadySpawnedThisTurn)
                {
                    UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());

                    int roll = TFTVCommonMethods.D12DifficultyModifiedRoll(controller.Difficulty.Order);

                    if (roll <= 6)
                    {

                        TacticalDeployZone tacticalDeployZone = FindTDZ(spawnName);
                        TacCharacterDef enemyToSpawn = GenerateRandomGruntAndSirenReinforcements(controller);


                        ActorDeployData actorDeployData = enemyToSpawn.GenerateActorDeployData();

                        actorDeployData.InitializeInstanceData();


                        TacticalActorBase tacticalActorBase = tacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, tacticalDeployZone);
                        TacticalActor tacticalActor = tacticalActorBase as TacticalActor;

                        if (difficulty < 6)
                        {
                            if (tacticalActor != null)
                            {
                                if (tacticalActor.HasGameTag(crabTag))
                                {
                                    if (TFTVArtOfCrab.Has1APWeapon(enemyToSpawn))
                                    {

                                        tacticalActor.Status.ApplyStatus(reinforcementStatus1AP);

                                    }
                                    else
                                    {

                                        tacticalActor.Status.ApplyStatus(reinforcementStatusUnder2AP);
                                        TFTVLogger.Always($"{tacticalActor.name} receiving {reinforcementStatusUnder2AP.EffectName}");

                                    }
                                }
                                else if (tacticalActor.HasGameTag(sirenTag))
                                {
                                    tacticalActor.Status.ApplyStatus(reinforcementStatusUnder1AP);
                                    TFTVLogger.Always($"{tacticalActor.name} receiving {reinforcementStatusUnder1AP.EffectName}");
                                }
                                else
                                {

                                    tacticalActor.Status.ApplyStatus(reinforcementStatusUnder2AP);
                                    TFTVLogger.Always($"{tacticalActor.name} receiving {reinforcementStatusUnder2AP.EffectName}");

                                }
                            }

                            GruntOrSirenReinforcementAlreadySpawnedThisTurn = true;
                        }

                        if (tacticalActor != null)
                        {
                            tacticalActor.AIActor.IsAlerted = true;
                            controller.ActorAlerted(tacticalActor);
                        }

                    }
                }

            }


            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }
        }

        private static List<TacCharacterDef> GenerateFixedChironReinforcements(TacticalLevelController controller)
        {
            try
            {
                TacCharacterDef mortarChiron = DefCache.GetDef<TacCharacterDef>("Chiron10_MortarBurrow_AlienMutationVariationDef");
                TacCharacterDef acidChiron = DefCache.GetDef<TacCharacterDef>("Chiron12_AcidMortarBurrow_AlienMutationVariationDef");
                TacCharacterDef gooChiron = DefCache.GetDef<TacCharacterDef>("Chiron8_GooHeavy_AlienMutationVariationDef");
                TacCharacterDef acidWormChiron = DefCache.GetDef<TacCharacterDef>("Chiron5_AcidWormAgile_AlienMutationVariationDef");
                TacCharacterDef poisonWormChiron = DefCache.GetDef<TacCharacterDef>("Chiron3_PoisonWormAgile_AlienMutationVariationDef");
                TacCharacterDef fireWormChiron = DefCache.GetDef<TacCharacterDef>("Chiron1_FireWormAgile_AlienMutationVariationDef");

                List<TacCharacterDef> chironTemporaryList = new List<TacCharacterDef>();
                List<TacCharacterDef> chironReinforcements = new List<TacCharacterDef>();
                int difficulty = controller.Difficulty.Order;

                if (difficulty == 6) //Etermes
                {

                    chironReinforcements.Add(mortarChiron);
                    chironReinforcements.Add(acidChiron);
                    //chironReinforcements.Add(gooChiron);

                }
                else if (difficulty >= 5) //Legend + Etermes
                {

                    chironTemporaryList.Add(mortarChiron);
                    chironTemporaryList.Add(acidChiron);
                    //chironReinforcements.Add(gooChiron);
                    chironReinforcements.Add(chironTemporaryList.GetRandomElement());

                }
                else if (difficulty == 4) //hero
                {
                    chironTemporaryList.Add(mortarChiron);
                    chironTemporaryList.Add(acidWormChiron);
                    chironReinforcements.Add(chironTemporaryList.GetRandomElement());
                    // chironReinforcements.Add(gooChiron);
                }
                else if (difficulty == 3) //veteran
                {
                    chironTemporaryList.Add(acidChiron);
                    chironTemporaryList.Add(gooChiron);
                    chironReinforcements.Add(chironTemporaryList.GetRandomElement());

                }
                else
                {
                    chironTemporaryList.Add(fireWormChiron);
                    chironTemporaryList.Add(poisonWormChiron);
                    chironReinforcements.Add(chironTemporaryList.GetRandomElement());

                }

                foreach (TacCharacterDef tacCharacterDef in chironReinforcements)
                {

                    TFTVLogger.Always($"Chiron reinforcements on {difficulty} difficulty level. Adding a {tacCharacterDef.name}");

                }

                return chironReinforcements;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }





        }

        private static void SpawnChironReinforcements(string spawnName, TacticalLevelController controller)
        {
            try
            {
                if (controller.TurnNumber < 16 && controller.TurnNumber % 5 == 0)
                {

                    TacticalDeployZone tacticalDeployZone = FindTDZ(spawnName);

                    foreach (TacCharacterDef tacCharacterDef in GenerateFixedChironReinforcements(controller))
                    {
                        ActorDeployData actorDeployData = tacCharacterDef.GenerateActorDeployData();

                        actorDeployData.InitializeInstanceData();

                        TacticalActorBase tacticalActorBase = tacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, tacticalDeployZone);
                        TacticalActor tacticalActor = tacticalActorBase as TacticalActor;
                        if (tacticalActor != null)
                        {

                            tacticalActor?.Status.ApplyStatus(reinforcementStatusUnder2AP);
                            tacticalActor.AIActor.IsAlerted = true;
                            controller.ActorAlerted(tacticalActor);
                        }
                    }
                }

            }


            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }
        }

        private static List<TacCharacterDef> GenerateFixedAcheronReinforcements(TacticalLevelController controller)
        {
            try
            {
                TacCharacterDef acheronAsclepiusChampion = DefCache.GetDef<TacCharacterDef>("AcheronAsclepiusChampion_TacCharacterDef");
                TacCharacterDef acheronAchlysChampion = DefCache.GetDef<TacCharacterDef>("AcheronAchlysChampion_TacCharacterDef");
                //  TacCharacterDef acheronPrime = DefCache.GetDef<TacCharacterDef>("AcheronPrime_TacCharacterDef"); causes softlock for some reason
                TacCharacterDef acheronAsclepius = DefCache.GetDef<TacCharacterDef>("AcheronAsclepius_TacCharacterDef");
                TacCharacterDef acheronAchlys = DefCache.GetDef<TacCharacterDef>("AcheronAchlys_TacCharacterDef");

                List<TacCharacterDef> temporaryAcheronList = new List<TacCharacterDef>();
                List<TacCharacterDef> acheronReinforcements = new List<TacCharacterDef>();

                int difficulty = controller.Difficulty.Order;

                if (difficulty == 6) //Etermes
                {
                    acheronReinforcements.Add(acheronAsclepiusChampion);
                    acheronReinforcements.Add(acheronAchlysChampion);
                }
                else if (difficulty >= 5) //Legend
                {
                    temporaryAcheronList.Add(acheronAsclepiusChampion);
                    temporaryAcheronList.Add(acheronAchlysChampion);
                    acheronReinforcements.Add(temporaryAcheronList.GetRandomElement());
                    // chironReinforcements.Add(acheronPrime);

                }
                else if (difficulty == 4) //hero
                {
                    temporaryAcheronList.Add(acheronAsclepiusChampion);
                    temporaryAcheronList.Add(acheronAchlys);
                    acheronReinforcements.Add(temporaryAcheronList.GetRandomElement());
                    // chironReinforcements.Add(acheronPrime);
                }
                else if (difficulty == 3) //veteran
                {
                    temporaryAcheronList.Add(acheronAsclepiusChampion);
                    temporaryAcheronList.Add(acheronAchlys);
                    acheronReinforcements.Add(temporaryAcheronList.GetRandomElement());

                }
                else
                {
                    temporaryAcheronList.Add(acheronAchlys);
                    temporaryAcheronList.Add(acheronAsclepius);
                    acheronReinforcements.Add(temporaryAcheronList.GetRandomElement());

                }

                foreach (TacCharacterDef tacCharacterDef in acheronReinforcements)
                {

                    TFTVLogger.Always($"Acheron reinforcements on {difficulty} difficulty level. Adding a {tacCharacterDef.name}");

                }

                return acheronReinforcements;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static void SpawnAcheronReinforcements(string spawnName, TacticalLevelController controller)
        {
            try
            {

                if (controller.TurnNumber < 16 && controller.TurnNumber % 4 == 0)
                {

                    TacticalDeployZone tacticalDeployZone = FindTDZ(spawnName);

                    foreach (TacCharacterDef tacCharacterDef in GenerateFixedAcheronReinforcements(controller))
                    {
                        ActorDeployData actorDeployData = tacCharacterDef.GenerateActorDeployData();

                        actorDeployData.InitializeInstanceData();

                        TacticalActorBase tacticalActorBase = tacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, tacticalDeployZone);
                        TacticalActor tacticalActor = tacticalActorBase as TacticalActor;
                        tacticalActor?.Status.ApplyStatus(reinforcementStatus1AP);

                    }
                }

            }


            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }
        }

        private static void AlertAllPandorans()
        {
            try
            {
                TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                if (controller.TurnNumber > 1)
                {
                    controller.GetFactionByCommandName("aln").TacticalActors.ForEach(ta => ta.AIActor.IsAlerted = true);
                }

                // SetupReinforcementPoints();



            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }

        }

        private static void PermaKillRevenant(int id)
        {
            try
            {
                TFTVRevenant.DeadSoldiersDelirium.Remove(id);


            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }

        }

        private static int GetRequiredDelirium(ClassTagDef classTagDef)
        {
            try
            {
                TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                int DeliriumRequired = 1;

                if (classTagDef == queenTag)
                {
                    DeliriumRequired = 10;
                }
                else if (classTagDef == acheronTag)
                {
                    DeliriumRequired = 9;
                }
                else if (classTagDef == chironTag)
                {
                    DeliriumRequired = 7;
                }
                else if (classTagDef == sirenTag)
                {
                    DeliriumRequired = 6;
                }
                else if (classTagDef == fishmanTag)
                {
                    DeliriumRequired = 3;
                }

                return DeliriumRequired;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }

        private static GeoTacUnitId _choosenRevenant = new GeoTacUnitId();

        private static bool GetChosenRevenant(int delirium)
        {
            try
            {
                TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                List<int> allDeadSoldiers = TFTVRevenant.DeadSoldiersDelirium.Keys.ToList();
                List<GeoTacUnitId> candidates = new List<GeoTacUnitId>();


                TFTVLogger.Always($"Revenant check. There are {allDeadSoldiers.Count} dead soldiers with Delirium.");

                foreach (int deadSoldier in allDeadSoldiers)
                {
                    if (deadSoldier >= delirium)
                    {
                        candidates.Add(TFTVRevenant.GetDeadSoldiersIdFromInt(deadSoldier, controller));
                    }
                }

                if (candidates.Count > 1)
                {

                    UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                    int roll = UnityEngine.Random.Range(0, candidates.Count());
                    TFTVLogger.Always("The total number of candidates is " + candidates.Count() + " and the roll is " + roll);

                    GeoTacUnitId theChosen = candidates[roll];
                    TFTVLogger.Always($"The Chosen is {TFTVRevenant.GetDeadSoldiersNameFromID(theChosen, controller)}");

                    _choosenRevenant = theChosen;
                    return true;
                }
                else if (candidates.Count == 1)
                {
                    GeoTacUnitId theChosen = candidates[0];
                    TFTVLogger.Always($"The Chosen is {TFTVRevenant.GetDeadSoldiersNameFromID(theChosen, controller)}");

                    _choosenRevenant = theChosen;
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
            }
            throw new InvalidOperationException();
        }

        public static void TryToTurnIntoRevenant(TacticalActorBase tacticalActorBase, TacticalLevelController controller)
        {
            try
            {
                if (controller.TacMission.IsFinalMission && controller.TurnNumber > 1)
                {
                    //  TFTVLogger.Always($"this is at least the right version");

                    if (TFTVRevenant.DeadSoldiersDelirium.Keys.Count > 0 && RevenantEligibleClasses.Any(t => tacticalActorBase.HasGameTag(t)))
                    {
                        TFTVLogger.Always($"{tacticalActorBase.name} class is eligible to be a Revenant and there are still {TFTVRevenant.DeadSoldiersDelirium.Keys.Count} in the list");

                        ClassTagDef actorClassTag = tacticalActorBase.GameTags.FindTagsOfType<ClassTagDef>().FirstOrDefault(tag => RevenantEligibleClasses.Contains(tag));

                        if (GetChosenRevenant(GetRequiredDelirium(actorClassTag)))
                        {
                            int chance = 50;

                            UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                            int roll = UnityEngine.Random.Range(1, 101);
                            TFTVLogger.Always($"Chance to turn {tacticalActorBase.name} Revenant is {chance} and the roll is {roll}");

                            if (roll <= chance)
                            {

                                GeoTacUnitId theChosen = _choosenRevenant;

                                TFTVRevenant.SetRevenantTierTag(theChosen, tacticalActorBase, controller);

                                tacticalActorBase.name = TFTVRevenant.GetDeadSoldiersNameFromID(theChosen, controller);
                                // SetDeathTime(theChosen, __instance, timeOfMissionStart);

                                TacticalActor tacticalActor = tacticalActorBase as TacticalActor;
                                TFTVRevenant.AddRevenantStatusEffect(tacticalActorBase);
                                TFTVRevenant.SetRevenantClassAbility(theChosen, controller, tacticalActor);

                                //  SpreadResistance(__instance);
                                tacticalActorBase.UpdateStats();
                                PermaKillRevenant(theChosen);
                                //  TFTVLogger.Always("Crab's name has been changed to " + actor.GetDisplayName());
                                // revenantCanSpawn = false;
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

        private static void PhoenixFalling()
        {
            try
            {
                TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                if (controller.TacMission.IsFinalMission)
                {
                    List<TacticalActor> revenants = controller.GetFactionByCommandName("px").TacticalActors.Where(ta => ta.HasGameTag(anyRevenantGameTag)).ToList();

                    if (revenants.Count > 0)
                    {
                        foreach (TacticalActor revenant in revenants)
                        {
                            if (revenant.CharacterStats.WillPoints > 0)
                            {
                                revenant.CharacterStats.WillPoints.Set(revenant.CharacterStats.WillPoints - revenant.CharacterStats.Willpower / 4);

                            }
                            else
                            {
                                revenant.SetFaction(controller.GetFactionByCommandName("aln"), TacMissionParticipant.Residents);

                                if (revenant.GameTags.Contains(anyRevenantGameTag))
                                {
                                    revenant.GameTags.Remove(anyRevenantGameTag);


                                }


                                revenant.TacticalActorView.DoCameraChase();
                                TFTVTutorialAndStory.ShowStoryPanel(controller, "PalaceRevenantHint1");
                                TFTVLogger.Always($"Pheonix {revenant.name} is back on the Dark Side!");
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

        private static void TheyAreMyMinions()

        {
            try
            {
                TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                if (controller.TacMission.IsFinalMission)
                {
                    TacticalFaction wild = controller.GetFactionByCommandName("wild");
                    TacticalFaction phoenix = controller.GetFactionByCommandName("px");
                    TacticalFaction aliens = controller.GetFactionByCommandName("aln");

                    IEnumerable<TacticalActor> strayMinions = from x in controller.Map.GetActors<TacticalActor>()
                                                              where x.TacticalFaction == wild || x.TacticalFaction == phoenix && x.HasGameTag(Shared.SharedGameTags.AlienTag) && !x.HasGameTag(anyRevenantGameTag)
                                                              where x.IsAlive
                                                              select x;

                    if (strayMinions.Count() > 0)
                    {

                        foreach (TacticalActor minion in strayMinions)
                        {

                            TFTVLogger.Always($"stray {minion.name} heeds the Master's Call!");


                            minion.SetFaction(controller.GetFactionByCommandName("aln"), TacMissionParticipant.Residents);
                            minion.TacticalActorView.DoCameraChase();

                            TFTVTutorialAndStory.ShowStoryPanel(controller, "PalaceHisMinionsHint");

                        }



                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }

        }

        private static void PhoenixRising()
        {
            try
            {

                TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                if (controller.TacMission.IsFinalMission)
                {
                    TFTVLogger.Always($"Looking for Revenants to convert");

                    List<TacticalActor> revenants = controller.GetFactionByCommandName("aln").TacticalActors.Where(ta => ta.HasGameTag(anyRevenantGameTag)).ToList();
                    if (revenants.Count > 0)
                    {

                        foreach (TacticalActor revenant in revenants)
                        {
                            int chance = 10;
                            if (revenant.HasGameTag(revenantTier3GameTag))
                            {
                                chance += 20;

                            }
                            else if (revenant.HasGameTag(revenantTier2GameTag))
                            {

                                chance += 10;

                            }

                            UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                            int roll = UnityEngine.Random.Range(1, 101);
                            TFTVLogger.Always($"Chance to turn Pandoran {revenant.name} Revenant is {chance} and the roll is {roll}");

                            if (roll <= chance)
                            {
                                revenant.SetFaction(controller.GetFactionByCommandName("px"), TacMissionParticipant.Player);
                                revenant.CharacterStats.WillPoints.SetToMax();
                                revenant.UpdateStats();
                                TFTVLogger.Always($"{revenant.name} has {revenant.CharacterStats.WillPoints} willpoints, should be max");
                                revenant.TacticalActorView.DoCameraChase();
                                TFTVTutorialAndStory.ShowStoryPanel(controller, "PalaceRevenantHint0");

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
        private static bool AllOperativesNorthOfGates()
        {
            try
            {
                TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                List<TacticalActor> tacticalActors = controller.GetFactionByCommandName("px").TacticalActors.
                    Where(a => a.IsAlive && a.Pos.z >= 41.5).ToList();

                if (tacticalActors.Count > 0)
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

        private static bool AllOperativesSouthOfGates()
        {
            try
            {
                TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                List<TacticalActor> tacticalActors = controller.GetFactionByCommandName("px").TacticalActors.
                    Where(a => a.IsAlive && a.Pos.z < 41.5).ToList();

                if (tacticalActors.Count > 0)
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

        public static void PalaceReinforcements(TacticalFaction tacticalFaction)
        {
            try
            {
                TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                if (controller.TacMission.IsFinalMission && tacticalFaction.Faction.FactionDef.Equals(Shared.PhoenixFactionDef))
                {
                    TFTVLogger.Always($"Palace mission. End of Turn {controller.TurnNumber} for {tacticalFaction.Faction.FactionDef.name}. Reinforcements.");
                    if (!AllOperativesNorthOfGates() && controller.TurnNumber > 1)
                    {
                        GruntOrSirenReinforcementAlreadySpawnedThisTurn = false;
                        SpawnGruntAndSirenReinforcements(RightTopSpawn, controller);
                        SpawnAcheronReinforcements(QueenReinforcementsSpawn1, controller);
                        SpawnChironReinforcements(ChironSpawn1, controller);
                        SpawnGruntAndSirenReinforcements(RightBottomSpawn, controller);

                        List<string> myrmidonSpawns = new List<string>() { SniperSpawn1, SniperSpawn2, SniperSpawn3 };

                        SpawnMyrmidonReinforcements(myrmidonSpawns.GetRandomElement(), controller);
                        SpawnJumpingArthronsReinforcements(controller);
                        // AlertAllPandorans();
                    }

                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }

        }

        public static void SpawnMistToHideReceptacleBody(TacticalFaction tacticalFaction)
        {
            try
            {
                TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                if (controller.TacMission.IsFinalMission && !controller.Map.GetActors<TacticalActorYuggoth>().Any(a => a.IsAlive) && tacticalFaction.Faction.FactionDef.Equals(Shared.PhoenixFactionDef) && !CheckPhoenixOperativesReceptacleDistrupredStatus(controller))
                {
                    YuggothShieldsAbility yuggothShieldsAbility = controller.Map.GetActors<TacticalActorYuggoth>().First().GetAbility<YuggothShieldsAbility>();
                    yuggothShieldsAbility?.RestoreShields();

                    TacticalDeployZone tacticalDeployZone = FindTDZ("Deploy_Yuggothian_Resident_3x3");
                    Level level = controller.Level;
                    TacticalVoxelMatrix tacticalVoxelMatrix = level?.GetComponent<TacticalVoxelMatrix>();
                    MethodInfo spawnBlob = AccessTools.Method(typeof(TacticalVoxelMatrix), "SpawnBlob_Internal");
                    //spawnBlob.Invoke(tacticalVoxelMatrix, new object[] { TacticalVoxelType.Empty, zone.Pos + Vector3.up * -1.5f, 3, 1, false, true });



                    // TFTVLogger.Always($"pXOperative to be ghosted {pXOperative.DisplayName} at pos {position}");
                    spawnBlob.Invoke(tacticalVoxelMatrix, new object[] { TacticalVoxelType.Mist, tacticalDeployZone.Pos, 3, 1, false, true });


                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }




        }



        private static void RemoveAlertPandoransNorthOfTheGates(TacticalLevelController controller)
        {
            try
            {
                TFTVLogger.Always($"Player has no units north of the closed gates; setting all Pandorans to not alerted!");

                List<TacticalActor> pandorans = controller.GetFactionByCommandName("aln").TacticalActors.Where(ta => ta.IsAlive && ta.Pos.z < 42).ToList();

                TFTVLogger.Always($"count: {pandorans.Count}");

                foreach (TacticalActor tacticalActor in pandorans)
                {
                    //   TFTVLogger.Always($"{tacticalActor.name}");
                    tacticalActor.AIActor.IsAlerted = false;


                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }

        }

        private static void RemoveAlertPandoransSouthOfTheGates(TacticalLevelController controller)
        {
            try
            {
                TFTVLogger.Always($"Player has no units south of the closed gates; setting all Pandorans to not alerted!");

                List<TacticalActor> pandorans = controller.GetFactionByCommandName("aln").TacticalActors.Where(ta => ta.IsAlive && ta.Pos.z > 42).ToList();

                TFTVLogger.Always($"count: {pandorans.Count}");

                foreach (TacticalActor tacticalActor in pandorans)
                {
                    //   TFTVLogger.Always($"{tacticalActor.name}");
                    tacticalActor.AIActor.IsAlerted = false;


                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }

        }

        public static void PalaceTacticalNewTurn(TacticalFaction tacticalFaction)
        {
            try
            {
                TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                if (controller.TacMission.IsFinalMission && tacticalFaction.Faction.FactionDef.Equals(Shared.AlienFactionDef) && !tacticalFaction.TacticalLevel.IsLoadingSavedGame)
                {
                    TacticalActorYuggoth tacticalActorYuggoth = controller.Map.GetActors<TacticalActorYuggoth>().FirstOrDefault();
                    bool wallsDown = false;
                    if (tacticalActorYuggoth != null && tacticalActorYuggoth.QueenWallDownOnTurn == -1)
                    {
                        wallsDown = true;
                    }

                    TFTVLogger.Always($"Palace mission. Turn  {controller.TurnNumber} for {tacticalFaction.Faction.FactionDef.name}");
                    if (!AllOperativesNorthOfGates() && controller.TurnNumber > 1)
                    {
                        CheckTimeToLowerGatesAgain();

                        //  SpawnGruntAndSirenReinforcements(RightTopSpawn, controller);
                        //  SpawnMonsterReinforcements(QueenReinforcementsSpawn, controller);
                        //  SpawnGruntAndSirenReinforcements(RightBottomSpawn, controller);

                        //   List<string> myrmidonSpawns = new List<string>() { SniperSpawn1, SniperSpawn2, SniperSpawn3 };

                        //  SpawnMyrmidonReinforcements(myrmidonSpawns.GetRandomElement(), controller);
                        //  SpawnJumpingArthronsReinforcements(controller);
                        //  AlertAllPandorans();
                    }
                    else if (AllOperativesNorthOfGates() && wallsDown)
                    {
                        RemoveAlertPandoransSouthOfTheGates(controller);
                    }
                    else if (AllOperativesSouthOfGates() && wallsDown)
                    {
                        RemoveAlertPandoransNorthOfTheGates(controller);
                    }

                    TheyAreMyMinions();
                    PhoenixFalling();


                }
                if (controller.TacMission.IsFinalMission && tacticalFaction.Faction.FactionDef.Equals(Shared.PhoenixFactionDef) && !tacticalFaction.TacticalLevel.IsLoadingSavedGame)
                {
                    TFTVLogger.Always($"Palace mission. Turn  {controller.TurnNumber} for {tacticalFaction.Faction.FactionDef.name}");
                    PhoenixRising();
                    CheckGateObjectives();
                    ReSpawnYuggoth();
                    //   RestoreReceptacleShields(controller);
                }
                // SetupReinforcementPoints();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }

        }


        private static TacticalDeployZone FindTDZ(string name)
        {
            try
            {
                TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();
                TacticalDeployZone tacticalDeployZone = controller.Map.GetActors<TacticalDeployZone>(null).FirstOrDefault(tdz => tdz.name == name);

                return tacticalDeployZone;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;

            }

        }

        private static void DeployEggsAndSentinels()
        {
            try
            {
                TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();


                List<TacticalDeployZone> eggDZs = controller.Map.GetActors<TacticalDeployZone>().Where(tdz => tdz.name.Contains("Deploy_Resident_1x1_AnyEgg")).ToList();

                foreach (TacticalDeployZone tdz in eggDZs)
                {

                    UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                    int roll = UnityEngine.Random.Range(0, 2);
                    //  TFTVLogger.Always($"found {tdz.name} at {tdz.Pos}, roll is {roll}");

                    if (roll == 0)
                    {

                        tdz.FixedDeployment = new List<FixedDeployConditionData> {
                            (new FixedDeployConditionData
                            { TacActorDef = DefCache.GetDef<TacCharacterDef>("Facehugger_Egg_AlienMutationVariationDef"),
                                TurnNumber = 0
                            }
                            )
                            };
                    }
                    else
                    {

                        tdz.FixedDeployment = new List<FixedDeployConditionData> {
                            (new FixedDeployConditionData { TacActorDef = DefCache.GetDef<TacCharacterDef>("Swarmer_Egg_AlienMutationVariationDef"),
                                TurnNumber = 0
                            }
                            )
                        };

                    }

                }
                for (int i = 0; i < 2; i++)
                {
                    if (i == 0)
                    {

                        eggDZs[0].SetPosition(altEggPosition0);
                    }
                    else
                    {
                        eggDZs[1].SetPosition(altEggPosition1);

                    }

                }


                FindTDZ("Deploy_Resident_1x1_Sentinel_Any").FixedDeployment = new List<FixedDeployConditionData> { (new FixedDeployConditionData
                {
                    TacActorDef = DefCache.GetDef<TacCharacterDef>("SentinelHatching_AlienMutationVariationDef"),
                    TurnNumber = 0
                }

                )};


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;

            }


        }

        public static void InitDeployZonesForPalaceMission(TacticalLevelController controller)
        {
            try
            {
                if (controller.TacMission.IsFinalMission)
                {
                    List<TacCharacterDef> eliteTritons = new List<TacCharacterDef>()
                    {
                   DefCache.GetDef<TacCharacterDef> ("Fishman14_PiercerSniper_AlienMutationVariationDef"),
                   DefCache.GetDef<TacCharacterDef> ("Fishman18_EliteViralSniper_AlienMutationVariationDef"),
                   DefCache.GetDef<TacCharacterDef> ("S_Fishman_Praetorian_TacCharacterDef"),

                    };

                    // ClearDeployZoneFixedDeployment(controller);

                    DeployEggsAndSentinels();

                    UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                    int roll1 = UnityEngine.Random.Range(0, 3);
                    MoveDeploymentZoneAndSpawnEnemy(FindTDZ(SniperSpawn1), SniperPosition1, 0, eliteTritons[roll1]);
                    //  TFTVLogger.Always($"roll1 is {roll1}, so the picked Triton is {eliteTritons[roll1].name}");
                    eliteTritons.RemoveAt(roll1);

                    UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                    int roll2 = UnityEngine.Random.Range(0, 2);
                    MoveDeploymentZoneAndSpawnEnemy(FindTDZ(SniperSpawn2), SniperPosition2, 0, eliteTritons[roll2]);
                    //  TFTVLogger.Always($"roll2 is {roll2}, so the picked Truton is {eliteTritons[roll2].name}");
                    eliteTritons.RemoveAt(roll2);

                    MoveDeploymentZoneAndSpawnEnemy(FindTDZ(SniperSpawn3), SniperPosition3, 0, eliteTritons[0]);
                    //   TFTVLogger.Always($"so the last Truton is {eliteTritons[0].name}");

                    /*   TacticalDeployZone tacticalDeployZone = FindTDZ(PlayerGruntSpawn);
                       tacticalDeployZone.SetPosition(PlayerGruntPosition);
                       tacticalDeployZone.SetFaction(controller.GetFactionByCommandName("px"), TacMissionParticipant.Player);*/

                    TacticalDeployZone queenTDZ1 = FindTDZ(QueenReinforcementsSpawn1);
                    queenTDZ1.SetPosition(QueenPosition1);
                    queenTDZ1.FixedDeployment[0].TurnNumber = 7;


                    Quaternion rotation = Quaternion.Euler(0, 90, 0);
                    queenTDZ1.SetRotation(rotation);


                    TacticalDeployZone queenTDZ2 = FindTDZ(ChironSpawn1);
                    queenTDZ2.SetPosition(ChironPosition1);
                    queenTDZ2.FixedDeployment[0].TurnNumber = 4;


                    Quaternion rotation2 = Quaternion.Euler(0, 180, 0);
                    queenTDZ2.SetRotation(rotation2);

                    //Deploy mortar Chirons immediately
                    //  FindTDZ("Deploy_Resident_3x3 (2)").FixedDeployment[0].TurnNumber = 0;
                    //  FindTDZ("Deploy_Resident_3x3 (3)").FixedDeployment[0].TurnNumber = 0;

                    ClearAndMoveReinforcementDeploymentZone(FindTDZ(RightBottomSpawn), RightBottomPosition);
                    ClearAndMoveReinforcementDeploymentZone(FindTDZ(RightTopSpawn), RightTopPosition);
                    ClearAndMoveReinforcementDeploymentZone(FindTDZ(LeftBottomSpawn), LeftBottomPosition);

                    //   TacticalDeployZone tacticalDeployZone = controller.Map.GetActors<TacticalDeployZone>().Where(tdz => tdz.name == ShouldNotBeUsedSpawn && tdz.Pos==ShouldNotBeUsedPosition).FirstOrDefault();

                    //   TFTVLogger.Always($"deploy zone null? {tacticalDeployZone==null}");

                    //    MoveDeploymentZoneAndSpawnEnemy(FindTDZ(ShouldNotBeUsedSpawn), ShouldNotBeUsedPosition, 0, DefCache.GetDef<TacCharacterDef>("S_SirenSuper_TacCharacterDef"));




                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }

        }

        private static void ClearDeployZoneFixedDeployment(TacticalLevelController controller)
        {
            try
            {

                List<string> deployZonesToClear = new List<string>() { "Deploy_Resident_5x5 (1)" };


                List<TacticalDeployZone> enemyDeployZones =
                  new List<TacticalDeployZone>(controller.Map.GetActors<TacticalDeployZone>()
                  .Where(tdz => tdz.MissionParticipant.Equals(TacMissionParticipant.Residents) && deployZonesToClear.Contains(tdz.name)).ToList());

                foreach (TacticalDeployZone tacticalDeployZone in enemyDeployZones)
                {

                    tacticalDeployZone.FixedDeployment.Clear();

                }

                /* Yuggothian_Item1, 0, (14.3, 0.0, 91.5)
                Yuggothian_Item1 will deploy YuggothianDroppedPX_ItemsContainer_TacActorDef on turn 0
                Deploy_Yuggothian_Resident_3x3, 0, (0.5, 1.8, -21.5)
                Deploy_Yuggothian_Resident_3x3 will deploy YugothianMain_TacCharacterDef on turn 0
                Deploy_Resident_5x5 (1), 0, (26.0, 0.0, 56.0)
                Deploy_Resident_5x5 (1) will deploy Queen_Gatekeeper2_TacCharacterDef on turn 0
                Deploy_Resident_1x1_Sentinel_Any, 0, (18.5, 1.5, 71.5)
                Deploy_Resident_1x1_Grunt_Elite_and_Tiny, 0, (9.0, 0.0, 62.0)
                Deploy_Player_3x3_Vehicle, 0, (25.0, 0.0, 94.5)
                Deploy_Resident_5x5, 0, (13.0, 0.0, 73.0)
                Deploy_Resident_5x5 will deploy Queen_Gatekeeper_TacCharacterDef on turn 0
                Deploy_Resident_1x1_AnyEgg, 0, (18.5, 0.4, 65.5)
                Deploy_Resident_1x1_AnyEgg, 0, (18.5, 0.5, 74.5)
                Deploy_Resident_1x1_AnyEgg, 0, (23.5, 0.5, 64.5)
                Deploy_Resident_1x1_AnyEgg, 0, (19.5, 0.5, 63.5)
                Deploy_Resident_1x1_AnyEgg, 0, (24.5, 0.5, 78.5)
                Deploy_Resident_3x3, 0, (10.5, 0.0, 52.0)
                Deploy_Resident_3x3 will deploy Chiron_Goo_TacCharacterDef on turn 0
                Deploy_Resident_3x3 will deploy FishmanElite_Shrowder_TacCharacterDef on turn 0
                Deploy_Resident_3x3 (1), 0, (25.0, 0.0, 70.0)
                Deploy_Resident_3x3 (4), 0, (10.5, 0.0, 52.0)
                Deploy_Resident_3x3 (4) will deploy Chiron_Goo_TacCharacterDef on turn 0
                Deploy_Resident_3x3 (4) will deploy FishmanElite_Shrowder_TacCharacterDef on turn 0
                Deploy_Resident_3x3 (2), 0, (12.0, 6.8, 1.0)
                Deploy_Resident_3x3 (2) will deploy S_Chiron_Mortar_TacCharacterDef on turn 5
                Deploy_Resident_3x3 (3), 0, (-17.0, 4.2, 3.0)
                Deploy_Resident_3x3 (3) will deploy S_Chiron_Mortar_TacCharacterDef on turn 5
                Deploy_Resident_3x3 (5), 0, (10.5, 0.0, 52.0)
                Deploy_Resident_3x3 (5) will deploy FishmanElite_Shrowder_TacCharacterDef on turn 0
                Deploy_Resident_3x3 (6), 0, (12.0, 6.6, 1.0)
                Deploy_Resident_3x3 (6) will deploy S_Fishman_Praetorian_TacCharacterDef on turn 5
                Deploy_Resident_3x3 (7), 0, (-17.0, 4.2, 3.0)
                Deploy_Resident_3x3 (7) will deploy S_Fishman_Praetorian_TacCharacterDef on turn 5
                Deploy_Player_1x1_Elite_Grunt_Drone, 0, (27.5, 0.0, 101.5)
                Reinforcement_Resident_1x1_Grunt_and_Elite, 0, (15.5, 0.4, -11.5)
                Reinforcement_Resident_1x1_Grunt_and_Elite, 0, (21.5, -2.1, 38.5)
                Reinforcement_Resident_1x1_Grunt_and_Elite, 0, (-15.5, 0.3, 11.5)
                Reinforcement_Resident_1x1_Grunt_and_Elite, 0, (-10.5, -2.1, -13.5)*/

                /* Free/to clear DZ: 
                 * 
                 * used:
                 * Deploy_Resident_3x3
                 * Deploy_Resident_3x3 (1)
                 * Deploy_Resident_3x3 (4)
                 * Deploy_Resident_3x3 (5)
                 * Deploy_Resident_3x3 (6)
                 * 
                 * Deploy_Resident_5x5
                 *    
                 * 
                 * need to clear:
                 * Deploy_Resident_3x3 (7)
                 * Deploy_Resident_5x5 (1)    
                 * 
                 * unused but no need to clear:
                 * Deploy_Resident_1x1_Sentinel_Any, 0, (18.5, 1.5, 71.5)
                 * Deploy_Resident_1x1_Grunt_Elite_and_Tiny, 0, (9.0, 0.0, 62.0)
                 * Deploy_Resident_1x1_AnyEgg, 0, (18.5, 0.4, 65.5)
                 * Deploy_Resident_1x1_AnyEgg, 0, (18.5, 0.5, 74.5)
                 * Deploy_Resident_1x1_AnyEgg, 0, (23.5, 0.5, 64.5)
                 * Deploy_Resident_1x1_AnyEgg, 0, (19.5, 0.5, 63.5)
                 * Deploy_Resident_1x1_AnyEgg, 0, (24.5, 0.5, 78.5)
                 */



                /*    foreach(TacticalDeployZone tdz in controller.Map.GetActors<TacticalDeployZone>()) 
                    {
                        TFTVLogger.Always($"{tdz.name}, {tdz.Pos}");

                        foreach(FixedDeployConditionData fixedDeployConditionData in tdz.FixedDeployment) 
                        {
                            TFTVLogger.Always($"{tdz.name} will deploy {fixedDeployConditionData.TacActorDef.name} on turn {fixedDeployConditionData.TurnNumber}");


                        }          
                    }*/




                ///Deploy_Resident_5x5 (1)
                /// 
                ///
                /*  private static readonly string QueenReinforcementsSpawn = "Deploy_Resident_5x5";

         private static readonly string SniperSpawn1 = "Deploy_Resident_1x1_Grunt_Elite_and_Tiny";
         private static readonly Vector3 SniperPosition1 = new Vector3(21.5f, 5.5f, 73.5f);
         private static readonly string SniperSpawn2 = "Deploy_Resident_3x3 (5)";
         private static readonly Vector3 SniperPosition2 = new Vector3(19.5f, 5.5f, 53.5f);
         private static readonly string SniperSpawn3 = "Deploy_Resident_1x1_AnyEgg";
         private static readonly Vector3 SniperPosition3 = new Vector3(-1.5f, 7.9f, 53.5f);
         private static readonly string RightBottomSpawn = "Deploy_Resident_3x3";
         private static readonly Vector3 RightBottomPosition = new Vector3(-11.5f, -2.4f, 73.5f);
         private static readonly string RightTopSpawn = "Deploy_Resident_3x3 (1)";
         private static readonly Vector3 RightTopPosition = new Vector3(-8.5f, -2.4f, 54.5f);
         private static readonly string LeftBottomSpawn = "Deploy_Resident_3x3 (2)";
         private static readonly Vector3 LeftBottomPosition = new Vector3(31.5f, -2.4f, 74.5f);*/

                /*
                 * 


        [TFTV @ 8/29/2023 3:46:32 PM] Deploy_Resident_3x3 (2) at position (12.0, 6.8, 1.0), belongs to Residents
        [TFTV @ 8/29/2023 3:46:32 PM] Deploy_Resident_3x3 (2) will spawn S_Chiron_Mortar_TacCharacterDef
        [TFTV @ 8/29/2023 3:46:32 PM] Deploy_Resident_3x3 (3) at position (-17.0, 4.2, 3.0), belongs to Residents
        [TFTV @ 8/29/2023 3:46:32 PM] Deploy_Resident_3x3 (3) will spawn S_Chiron_Mortar_TacCharacterDef



        [TFTV @ 8/29/2023 3:46:32 PM] Deploy_Resident_3x3 (6) at position (12.0, 6.6, 1.0), belongs to Residents
        [TFTV @ 8/29/2023 3:46:32 PM] Deploy_Resident_3x3 (6) will spawn S_Fishman_Praetorian_TacCharacterDef
        [TFTV @ 8/29/2023 3:46:32 PM] Deploy_Resident_3x3 (7) at position (-17.0, 4.2, 3.0), belongs to Residents
        [TFTV @ 8/29/2023 3:46:32 PM] Deploy_Resident_3x3 (7) will spawn S_Fishman_Praetorian_TacCharacterDef*/




            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }




        }

        public static void LogEnemyAP(TacticalFaction tacticalFaction)
        {
            try
            {
                foreach (TacticalActor tacticalActor in tacticalFaction.TacticalActors)
                {
                    TFTVLogger.Always($"{tacticalActor.name} has {tacticalActor.CharacterStats.ActionPoints} action points");
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }

        }

        /*  [HarmonyPatch(typeof(TacticalDeployZone), "Initialize")]

          public static class TFTV_TacticalDeployZone_Initialize
          {
              private static bool sniper1Spawned = false;


              public static void Postfix(TacticalDeployZone __instance)
              {
                  try
                  {

                      //  if (__instance.TacticalLevel.TacMission.IsFinalMission)
                      //  {

                      TacticalLevelController controller = GameUtl.LevelOf(__instance.gameObject).GetComponent<TacticalLevelController>();


                      if (__instance.name == SniperSpawn1 && !sniper1Spawned)
                      {

                          MoveDeploymentZoneAndSpawnEnemy(__instance, SniperPosition1, 0, DefCache.GetDef<TacCharacterDef>("S_Fishman_Praetorian_TacCharacterDef"));

                          // __instance.SetPosition(new Vector3(21.5f, 5.5f, 73.5f));
                          sniper1Spawned = true;

                          TFTVLogger.Always($"{__instance.name} should now be at {__instance.Pos}, should spawn a {__instance.FixedDeployment.First().TacActorDef.name}");

                      }

                      else if (__instance.name == SniperSpawn2)
                      {

                          MoveDeploymentZoneAndSpawnEnemy(__instance, SniperPosition2, 0, DefCache.GetDef<TacCharacterDef>("S_Fishman_Praetorian_TacCharacterDef"));

                          // __instance.SetPosition(new Vector3(21.5f, 5.5f, 73.5f));

                          TFTVLogger.Always($"{__instance.name} should now be at {__instance.Pos}, should spawn a {__instance.FixedDeployment.First().TacActorDef.name}");

                      }

                      else if (__instance.name == SniperSpawn3)
                      {

                          MoveDeploymentZoneAndSpawnEnemy(__instance, SniperPosition3, 0, DefCache.GetDef<TacCharacterDef>("S_Fishman_Praetorian_TacCharacterDef"));

                          // __instance.SetPosition(new Vector3(21.5f, 5.5f, 73.5f));

                          TFTVLogger.Always($"{__instance.name} should now be at {__instance.Pos}, should spawn a {__instance.FixedDeployment.First().TacActorDef.name}");

                      }
                      else if (__instance.name == QueenReinforcementsSpawn)
                      {

                          MoveDeploymentZoneAndSpawnEnemy(__instance, QueenPosition, 3, DefCache.GetDef<TacCharacterDef>("Scylla10_Crystal_AlienMutationVariationDef"));


                          //    __instance.FixedDeployment.Clear();
                          //   MoveReinforcementDeploymentZone(__instance, QueenPosition);
                      }

                      else if (__instance.name == RightBottomSpawn)
                      {

                          __instance.FixedDeployment.Clear();
                          MoveReinforcementDeploymentZone(__instance, RightBottomPosition);
                      }
                      else if (__instance.name == LeftBottomSpawn)
                      {

                          __instance.FixedDeployment.Clear();
                          MoveReinforcementDeploymentZone(__instance, LeftBottomPosition);
                      }
                      else if (__instance.name == RightTopSpawn)
                      {


                          __instance.FixedDeployment.Clear();
                          MoveReinforcementDeploymentZone(__instance, RightTopPosition);
                      }


                      else if (__instance.name != "Deploy_Yuggothian_Resident_3x3")
                      {
                          __instance.FixedDeployment.Clear();
                          TFTVLogger.Always($"{__instance.name} at position {__instance.Pos}, belongs to {__instance.MissionParticipant.GetName()} fixed deployment cleared");
                      }

                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);

                  }
              }
          }*/



        private static IEnumerator<NextUpdate> RaiseReceptacleShieldCrt(PlayingAction action)
        {

            TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

            TacticalActorYuggoth actor = controller.Map.GetActors<TacticalActorYuggoth>().First();

            YuggothShieldsAbility yuggothShieldsAbility = actor.GetAbility<YuggothShieldsAbility>();

            MethodInfo GetShieldCollidersCollidersMethod = typeof(YuggothShieldsAbility).GetMethod("GetShieldColliders", BindingFlags.NonPublic | BindingFlags.Instance);

            Collider[] shieldColliders = (Collider[])GetShieldCollidersCollidersMethod.Invoke(yuggothShieldsAbility, new object[] { false });

            CustomLerper shieldsLerp = new CustomLerper();

            Transform transform = actor.Shields[0].transform;

            TFTVLogger.Always($"considering transform at position {transform.position}");

            shieldsLerp.LowerdPos = transform.position;




            if (Boxify.GetBoundsOfColliders(shieldColliders, out var result))
            {
                shieldsLerp.RaisedPos = transform.position - new Vector3(0f, 0f - result.size.y, 0f);
            }
            // TFTVLogger.Always($"raised position {queensWallLerp.RaisedPos}"); // raised position(8.2, -14.8, 43.0)





            // TacticalNavObstaclesHolder queensWallsNavObstaclesHolder = actor.QueensWall.GetComponent<TacticalNavObstaclesHolder>();


            MethodInfo playShieldMovedEventMethod = typeof(YuggothShieldsAbility).GetMethod("PlayShieldMovedEvent",
        BindingFlags.NonPublic | BindingFlags.Instance);



            playShieldMovedEventMethod.Invoke(yuggothShieldsAbility, new object[] { actor.QueensWall.transform });

            Vector3 initialPos = transform.position;

            //  TFTVLogger.Always($"wall initial position: {initialPos}");


            //wall initial position: (8.2, -7.4, 43.0)

            float t = 0f;
            while (t <= 1f)
            {
                t += Time.deltaTime * 0.5f;
                float t2 = Mathf.Min(t, 1f);
                actor.Shields[0].transform.position = shieldsLerp.LerpRaise(initialPos, t2);
                yield return NextUpdate.NextFrame;
            }

            /*  if (queensWallsNavObstaclesHolder != null)
              {
                  queensWallsNavObstaclesHolder.InitNavObstacles();
              }*/

            MethodInfo updateMapMethod = typeof(YuggothShieldsAbility).GetMethod("UpdateMapForShields", BindingFlags.NonPublic | BindingFlags.Instance);
            updateMapMethod.Invoke(yuggothShieldsAbility, null);


        }

        public static void RaiseReceptacleShields(TacticalActorYuggoth actor)
        {
            try
            {
                //   actor.TacticalActorViewBase.DoCameraChaseParam(actor.Shields[0].transform);
                actor.ShieldsAbility.PlayAction(RaiseReceptacleShieldCrt, null);

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        private static List<FixedDeployConditionData> GenerateRandomDeplyConditionData(TacticalLevelController controller)
        {
            try
            {
                List<FixedDeployConditionData> fixedDeployConditionDatas = new List<FixedDeployConditionData>();


                int difficulty = controller.Difficulty.Order;

                for (int x = 2; x < 23; x++)
                {
                    TacActorDef tacActorDef = GenerateRandomGruntAndSirenReinforcements(controller);
                    int turnNumber = x + 5 - difficulty;

                    fixedDeployConditionDatas.Add(new FixedDeployConditionData()
                    {
                        TacActorDef = tacActorDef,
                        TurnNumber = turnNumber


                    });

                    // TFTVLogger.Always($"{tacActorDef.name} will spawn on turn number {turnNumber}");
                }

                return fixedDeployConditionDatas;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }
        private static TacCharacterDef GenerateRandomChironReinforcements(TacticalLevelController controller)
        {
            try
            {

                TacCharacterDef pickedEnemy = null;

                ClassTagDef chironTag = DefCache.GetDef<ClassTagDef>("Chiron_ClassTagDef");

                List<TacCharacterDef> availableTemplatesOrdered = new List<TacCharacterDef>(controller.TacMission.MissionData.UnlockedAlienTacCharacterDefs.OrderByDescending(tcd => tcd.DeploymentCost));

                List<TacCharacterDef> chironsOrdered = new List<TacCharacterDef>(availableTemplatesOrdered.Where(t => t.ClassTag == chironTag));

                pickedEnemy = chironsOrdered.GetRandomElement();


                return pickedEnemy;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static TacCharacterDef GenerateRandomAcheronReinforcements(TacticalLevelController controller)
        {
            try
            {

                TacCharacterDef pickedEnemy = null;

                ClassTagDef acheronTag = DefCache.GetDef<ClassTagDef>("Acheron_ClassTagDef");


                List<TacCharacterDef> availableTemplatesOrdered = new List<TacCharacterDef>(controller.TacMission.MissionData.UnlockedAlienTacCharacterDefs.OrderByDescending(tcd => tcd.DeploymentCost));


                List<TacCharacterDef> acheronsOrdered = new List<TacCharacterDef>(availableTemplatesOrdered.Where(t => t.ClassTag == acheronTag));


                pickedEnemy = acheronsOrdered.GetRandomElement();


                return pickedEnemy;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }
    }
}