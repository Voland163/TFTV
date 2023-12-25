using Base;
using Base.Core;
using Base.Entities;
using Base.Entities.Statuses;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Levels.ActorDeployment;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.ActorsInstance;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.StructuralTargets;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TFTV
{
    internal class TFTVRescueVIPMissions
    {
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        private static readonly List<CustomMissionTypeDef> _VIPRescueMissions = new List<CustomMissionTypeDef>()

                {DefCache.GetDef<CustomMissionTypeDef>("Bcr5_CustomMissionTypeDef"),
                DefCache.GetDef<CustomMissionTypeDef>("Bcr7_CustomMissionTypeDef"),
                DefCache.GetDef<CustomMissionTypeDef>("StoryLE0_CustomMissionTypeDef"),
                DefCache.GetDef<CustomMissionTypeDef>("Bcr1_CustomMissionTypeDef")

                };


        public static void CheckAndImplementVIPRescueMIssions(TacticalLevelController controller)
        {
            try
            {
                if (CheckIfMissionIsVIPRescue(controller))
                {
                    TFTVLogger.Always($"Instantiating rescue VIP objectives on Mission Start, Load or Restart");
                    ChangeRescueeAllegiance(controller);
                    CreateStructuralTargetForObjective(controller);
                    AdjustObjectives();
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        private static bool CheckIfMissionIsVIPRescue(TacticalLevelController controller)
        {
            try
            {
                if (_VIPRescueMissions.Contains(controller.TacMission.MissionData.MissionType))
                {
                    //TFTVLogger.Always($"The mission is to rescue Mr. Sparks!");
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

        private static void ChangeRescueeAllegiance(TacticalLevelController controller)
        {
            try
            {
                if (CheckIfMissionIsVIPRescue(controller)) //need another check after Sparks becomes Phoenix
                {
                    TFTVLogger.Always($"The mission is to rescue Mr. Sparks, Felipe, Dr. Calendar or Dr. Helena and we have to make him Neutral so he doesn't die!");
                    TacticalActor phoenixCivilian = controller.GetFactionByCommandName("px").TacticalActors.FirstOrDefault(a => a.HasGameTag(Shared.SharedGameTags.CivilianTag));

                    if (phoenixCivilian != null)
                    {
                        phoenixCivilian.SetFaction(controller.GetFactionByCommandName("neut"), TacMissionParticipant.Environment);
                    }
                    else
                    {
                        TacticalActor civilian = controller.GetFactionByCommandName("neut").TacticalActors.FirstOrDefault(a => a.HasGameTag(Shared.SharedGameTags.CivilianTag));

                        TriggerAbilityZoneOfControlStatusDef zoneOfControlStatusDef = DefCache.GetDef<TriggerAbilityZoneOfControlStatusDef>("CanBeRecruitedIntoPhoenix_1x1_StatusDef");

                        if (civilian != null && civilian.Status.HasStatus(zoneOfControlStatusDef))
                        {
                            civilian.Status.UnapplyStatus(civilian.Status.GetStatusByName(zoneOfControlStatusDef.EffectName));
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

        private static TacticalActor FindNeutralCivilian(TacticalLevelController controller)
        {

            try
            {
                if (CheckIfMissionIsVIPRescue(controller))
                {
                    return controller.GetFactionByCommandName("neut").TacticalActors.FirstOrDefault(a => a.HasGameTag(Shared.SharedGameTags.CivilianTag));
                }
                return null;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static Vector3 FindSpotForSpawnPoint(TacticalActor tacticalActor, TacticalLevelController controller)
        {
            try
            {
                Vector3 position = tacticalActor.Pos;

                List<Vector3> vector3s = new List<Vector3>()
                {
                    new Vector3(-1f, 0.0f, -1f) +position,
                     new Vector3(1f, 0.0f, -1f) +position,
                      new Vector3(-1f, 0.0f, 1f) +position,
                       new Vector3(1f, 0.0f, 1f) +position,
                       new Vector3(0.0f, 0.0f, -1f) +position,
                          new Vector3(0.0f, 0.0f, 1f) +position,
                          new Vector3(-1f, 0.0f, 0.0f) +position,
                new Vector3(1f, 0.0f, 0.0f) +position,

                };


                TacCharacterDef siren = DefCache.GetDef<TacCharacterDef>("Siren1_Basic_AlienMutationVariationDef");

                ActorDeployData actorDeployData = siren.GenerateActorDeployData();
                actorDeployData.InitializeInstanceData();



                foreach (Vector3 vector3 in vector3s)
                {
                    if (controller.Map.CanStandAt(tacticalActor.NavigationComponent.NavMeshDef, tacticalActor.TacticalPerception.TacticalPerceptionDef, vector3) &&
                        TacticalFactionVision.CheckVisibleLineBetweenActorsInTheory(tacticalActor, tacticalActor.Pos, actorDeployData.ComponentSetDef, vector3))
                    {
                        TFTVLogger.Always($"found suitable position {vector3}");
                        return vector3;

                    }


                }

                return vector3s.GetRandomElement();


            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }


        }

        private static void CreateStructuralTargetForObjective(TacticalLevelController controller)
        {
            try
            {

                TFTVLogger.Always($"Creating TalkingPointConsole");
                Vector3 position = FindSpotForSpawnPoint(FindNeutralCivilian(controller), controller);
                string name = "TalkingPoint";

                StructuralTargetTypeTagDef structuralTargetTypeTagDef = DefCache.GetDef<StructuralTargetTypeTagDef>("TalkingPointConsoleTag");

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
                structuralTarget.GameTags.Add(structuralTargetTypeTagDef);

                var ipCols = new GameObject("InteractionPointColliders");
                ipCols.transform.SetParent(obj.transform);
                ipCols.tag = InteractWithObjectAbilityDef.ColliderTag;

                ipCols.transform.SetPositionAndRotation(position, Quaternion.identity);
                var collider = ipCols.AddComponent<BoxCollider>();


                structuralTarget.Initialize();
                //TFTVLogger.Always($"Spawning interaction point with name {name} at position {position}");
                structuralTarget.DoEnterPlay();

                TFTVLogger.Always($"structural target {name} created at position {position}");



                CheckStatusInteractionPoint(controller, name);
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
                WipeEnemyFactionObjective dummyObjective = (WipeEnemyFactionObjective)factionObjectives.FirstOrDefault(obj => obj is WipeEnemyFactionObjective objective);

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


        /// <summary>
        /// This attempts to assign correct status to objective
        /// </summary>
        internal static void CheckStatusInteractionPoint(TacticalLevelController controller, string name)
        {
            try
            {


                StatusDef activeHackableChannelingStatusDef = DefCache.GetDef<StatusDef>("ConvinceCivilianOnObjectiveStatus");
                StatusDef hackingStatusDef = DefCache.GetDef<StatusDef>("ConvinceCivilianOnActorStatus");
                StatusDef consoleToActorBridgingStatusDef = DefCache.GetDef<StatusDef>("ConvinceCivilianObjectiveToActorBridgeStatus");
                StatusDef actorToConsoleBridgingStatusDef = DefCache.GetDef<StatusDef>("ConvinceCivilianActorToObjectiveBridgeStatus");
                StatusDef activatedStatusDef = DefCache.GetDef<StatusDef>("ConsoleActivated_StatusDef");


                StructuralTarget structuralTarget = UnityEngine.Object.FindObjectsOfType<StructuralTarget>().FirstOrDefault(b => b.name.Equals(name));

                TacticalActor tacticalActor = controller.Map.FindActorOverlapping(structuralTarget.Pos);

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

                    TFTVLogger.Always($"found {tacticalActor?.DisplayName} trying to convince civillian");

                    reflectionSet(newTargetBridge, turnApplied);
                    reflectionSet(newActorBridge, turnApplied);

                }
                else
                {
                    TFTVLogger.Always($"are we here? has the activated status?{structuralTarget.Status.HasStatus(activatedStatusDef) == true} has the activatable status? {structuralTarget.Status.HasStatus(activeHackableChannelingStatusDef) == true} ");

                    if (!structuralTarget.Status.HasStatus(activatedStatusDef) && !structuralTarget.Status.HasStatus(activeHackableChannelingStatusDef))
                    {
                        structuralTarget.Status.ApplyStatus(activeHackableChannelingStatusDef);//(activeConsoleStatusDef);

                        TFTVLogger.Always($"applying {activeHackableChannelingStatusDef.name} to TalkingPointConsole");

                    }
                }

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }
        }

        private static void TurnRescueeOverToPhoenix(TacticalLevelController controller)
        {
            try
            {
                TacticalActor sparks = FindNeutralCivilian(controller);
                sparks.SetFaction(controller.GetFactionByCommandName("px"), TacMissionParticipant.Player);
                sparks.ForceRestartTurn();


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }
        }

        public static void TalkingPointConsoleActivated(StatusComponent statusComponent, Status status, TacticalLevelController controller)
        {
            try
            {
                if (controller != null && CheckIfMissionIsVIPRescue(controller) && !controller.IsLoadingSavedGame)
                {
                    if (status.Def == DefCache.GetDef<StatusDef>("ConsoleActivated_StatusDef"))
                    {
                        StructuralTarget console = statusComponent.transform.GetComponent<StructuralTarget>();
                        TFTVLogger.Always($"console name {console.name} at position {console.Pos}");

                        TurnRescueeOverToPhoenix(controller);
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
