using Base;
using Base.Cameras;
using Base.Core;
using Base.Defs;
using Base.Entities;
using Base.Entities.Statuses;
using HarmonyLib;
using I2.Loc;
using PhoenixPoint.Common.ContextHelp;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.ContextHelp;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.ActorsInstance;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.StructuralTargets;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.ActorDeployment;
using PhoenixPoint.Tactical.Levels.Destruction;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using PhoenixPoint.Tactical.Levels.TacticalLevelLights;
using PhoenixPoint.Tactical.View.ViewControllers;
using PhoenixPoint.Tactical.View.ViewModules;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static PhoenixPoint.Tactical.View.ViewControllers.SquadMemberScrollerController;
using static TFTV.TFTVNJQuestline.IntroMission.MissionQuips;
using static TFTV.TFTVTacticalUtils;
using static TFTV.TFTVTauntsAndQuips;

namespace TFTV
{
    internal class TFTVNJQuestline
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;

        public static bool NewNJIntroMission = false;



        public static class DelayedInvoker
        {
            private class DelayedRunner : MonoBehaviour { }

            private static DelayedRunner _runner;
            private static DelayedRunner Runner
            {
                get
                {
                    if (_runner == null)
                    {
                        // Create a new GameObject to run our coroutines.
                        GameObject go = new GameObject("DelayedInvoker");
                        UnityEngine.Object.DontDestroyOnLoad(go);
                        _runner = go.AddComponent<DelayedRunner>();
                    }
                    return _runner;
                }
            }

            public static void Run(float delay, Action action)
            {
                Runner.StartCoroutine(RunCoroutine(delay, action));
            }

            private static IEnumerator RunCoroutine(float delay, Action action)
            {
                yield return new WaitForSeconds(delay);
                action?.Invoke();
            }
        }



        internal class IntroMission
        {
            private static readonly CustomMissionTypeDef _introMission = DefCache.GetDef<CustomMissionTypeDef>("StoryNJ0_CustomMissionTypeDef");
            private static MissionTagDef _newNJIntroMissionTag = null;
            private static StructuralTarget _commsConsole = null;
            private static KillActorFactionObjectiveDef _killInfiltratorObjective = null;
            private static ActorDeploymentTagDef _priestDeploymentTag = null;
            private static ActorDeploymentTagDef _infiltratorDeploymentTag = null;
            public static ContextHelpHintDef NewNJIntroMissionHint = null;
            private static readonly string _hintDescKey = "TFTV_KEY_NJ_INTRO_MISS_HINT_MISSION_START_DESC";
            private static string _havenName = "";
            public static List<string> UsedQuips = new List<string>();

            public static bool IsIntroMission(TacticalLevelController controller = null, GeoMission mission = null)
            {
                try
                {
                    if (!NewNJIntroMission)
                    {
                        return false;
                    }

                    if (controller != null)
                    {
                        return controller.TacMission.MissionData.MissionType.Tags.Contains(_newNJIntroMissionTag);
                    }
                    else
                    {
                        return mission.MissionDef.Tags.Contains(_newNJIntroMissionTag);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            public static void ClearDataOnMissionRestartLoadAndStateChange()
            {
                try
                {
                    _commsConsole = null;
                    UsedQuips.Clear();

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }




            public static void RunOnTacticalStart(TacticalLevelController controller)
            {

                try
                {
                    if (!IsIntroMission(controller))
                    {
                        return;
                    }

                    MissionStartChanges.CreateObjectiveToTurnOffPropaganda(controller);
                    MissionQuips.Propaganda.PropagandaQuips(controller);
                    NJQuips.PopulateQuips();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }

            internal class Defs
            {


                public static void ModifyIntroMissionDefs()
                {
                    try
                    {
                        if (!NewNJIntroMission)
                        {
                            return;
                        }

                        CreateMissionTag();
                        AddIntroHint();
                        AddNJFaction();
                        MakeAnuPriestAppearOnAllDifficulties();

                        AddSynedrionInfiltrator();
                        CreateKillInfiltratorObjective();
                        ModifyKillObjective();

                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }



                private static void CreateMissionTag()
                {
                    try
                    {
                        _newNJIntroMissionTag = Helper.CreateDefFromClone(
                            DefCache.GetDef<MissionTagDef>("MissionTypeStoryMissionNJ_MissionTagDef"),
                            "{F3187A2C-92B9-4E0B-822B-26BB18F01F58}", "NewNJIntroMissionTag");

                        _introMission.Tags.Add(_newNJIntroMissionTag);

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }


                }


                private static void AddIntroHint()
                {
                    try

                    {
                        string hintTitle = "TFTV_KEY_NJ_INTRO_MISS_HINT_MISSION_START_TITLE";
                        string hintDesc = _hintDescKey;

                        NewNJIntroMissionHint = TFTVHints.HintDefs.CreateNewTacticalHint(
                            hintDesc, HintTrigger.MissionStart, _newNJIntroMissionTag.name, hintTitle, hintDesc, 3, true, "{2408C9C0-90F0-4BA3-8A25-36520B63ECED}", "NJ_VICTORY_START1.jpg");

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }


                }


                private static void ModifyKillObjective()
                {
                    try
                    {

                        WipeEnemyFactionObjectiveDef sourceDef = DefCache.GetDef<WipeEnemyFactionObjectiveDef>("300WipeEnemy_CustomMissionObjective");
                        WipeEnemyFactionObjectiveDef newObjective = Helper.CreateDefFromClone(sourceDef, "{D94D4DC7-AE2B-4165-811A-6132820F3D92}", "NJIntroMission_CommsObjectiveDef");

                        newObjective.NextOnSuccess = new FactionObjectiveDef[] { };
                        newObjective.MissionObjectiveData.Description.LocalizationKey = "TFTV_KEY_NJ_INTRO_MISS_OBJECTIVE_COMMS";
                        newObjective.MissionObjectiveData.Summary.LocalizationKey = "TFTV_KEY_NJ_INTRO_MISS_OBJECTIVE_COMMS";

                        List<FactionObjectiveDef> objectives = _introMission.CustomObjectives.ToList();
                        objectives.Add(newObjective);
                        objectives.Remove(sourceDef);
                        objectives.Add(_killInfiltratorObjective);
                        _introMission.CustomObjectives = objectives.ToArray();


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }

                }



                private static void CreateKillInfiltratorObjective()
                {
                    try
                    {
                        string name = "KillInfiltrator";
                        string guid = "{{ABA2D7E1-BBE6-40D5-9108-F443B76BC872}}";
                        GameTagDef tag = DefCache.GetDef<GameTagDef>("Infiltrator_ClassTagDef");
                        string descLocKey = "TFTV_KEY_NJ_INTRO_MISS_OBJECTIVE_INFILTRATOR";
                        int expReward = 200;

                        _killInfiltratorObjective = TFTVUITactical.SecondaryObjectivesTactical.Defs.CreateSecondaryObjectiveKill(name, guid, tag, descLocKey, expReward, true);

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }



                private static void AddNJFaction()
                {
                    try
                    {
                        TacMissionTypeParticipantData pxParticipantData = _introMission.ParticipantsData[0];
                        List<TacMissionTypeParticipantData.UniqueChatarcterBind> soldiers = _introMission.ParticipantsData[1].UniqueUnits.Where(u => u.Character.Data.GameTags.Contains(DefCache.GetDef<ClassTagDef>("Assault_ClassTagDef"))).ToList();


                        soldiers = new List<TacMissionTypeParticipantData.UniqueChatarcterBind>()
                        {
                        new TacMissionTypeParticipantData.UniqueChatarcterBind(){Character = DefCache.GetDef<TacCharacterDef>("NJ_Assault1_CharacterTemplateDef"),
                         Amount = new Base.Utils.RangeDataInt(4,4)}
                        };



                        // TFTVLogger.Always($"soldiers: {soldiers.Count}");
                        TacMissionTypeParticipantData njParticipantData = new TacMissionTypeParticipantData()
                        {
                            FactionDef = DefCache.GetDef<PPFactionDef>("NewJericho_FactionDef"),
                            PredeterminedFactionEffects = pxParticipantData.PredeterminedFactionEffects,
                            ParticipantKind = TacMissionParticipant.Residents,
                            ActorDeployParams = pxParticipantData.ActorDeployParams,
                            GenerateGeoCharacters = false,
                            DeploymentRule = pxParticipantData.DeploymentRule,
                            InfiniteReinforcements = pxParticipantData.InfiniteReinforcements,
                            ReinforcementsDeploymentPart = pxParticipantData.ReinforcementsDeploymentPart,
                            ReinforcementsTurns = pxParticipantData.ReinforcementsTurns,
                            UniqueUnits = soldiers.ToArray(),

                        };

                        _introMission.ParticipantsData.Add(njParticipantData);
                        _introMission.ParticipantsRelations = _introMission.ParticipantsRelations.AddToArray(new MutualParticipantsRelations
                        {
                            FirstParticipant = TacMissionParticipant.Intruder,
                            SecondParticipant = TacMissionParticipant.Residents,
                            MutualRelation = FactionRelation.Enemy

                        });
                        _introMission.ParticipantsRelations = _introMission.ParticipantsRelations.AddToArray(new MutualParticipantsRelations
                        {
                            FirstParticipant = TacMissionParticipant.Player,
                            SecondParticipant = TacMissionParticipant.Residents,
                            MutualRelation = FactionRelation.Neutral

                        });

                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static void CreateSpecialDeploymentTagForPriest(TacCharacterDef specialPriest)
                {
                    try
                    {
                        _priestDeploymentTag = Helper.CreateDefFromClone(specialPriest.DefaultDeploymentTags[0], "{18462022-511C-4742-8B23-B5B504B13447}", "NJIntroMission_PriestDeploymentTag");
                        specialPriest.DefaultDeploymentTags = new ActorDeploymentTagDef[] { _priestDeploymentTag };
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }

                }

                private static void MakeAnuPriestAppearOnAllDifficulties()
                {
                    try
                    {
                        _introMission.ParticipantsData[1].UniqueUnits = new TacMissionTypeParticipantData.UniqueChatarcterBind[] { _introMission.ParticipantsData[1].UniqueUnits[3] };
                        _introMission.ParticipantsData[1].UniqueUnits[0].Difficulty = DefCache.GetDef<GameDifficultyLevelDef>("StoryMode_DifficultyLevelDef");
                        CreateSpecialDeploymentTagForPriest(_introMission.ParticipantsData[1].UniqueUnits[0].Character);
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static void AddSynedrionInfiltrator()
                {
                    try
                    {
                        TacCharacterDef tacCharacterDefSource = DefCache.GetDef<TacCharacterDef>("SY_Infiltrator1_CharacterTemplateDef");
                        TacCharacterDef newTacCharacterDef = Helper.CreateDefFromClone(tacCharacterDefSource, "{B642C029-813E-4464-BA34-EDD6D151A3EF}", "NJIntroMission_SYInfiltratorDef");
                        newTacCharacterDef.Data.EquipmentItems = new ItemDef[] { };
                        newTacCharacterDef.Data.InventoryItems = new ItemDef[] { };

                        _infiltratorDeploymentTag = Helper.CreateDefFromClone(newTacCharacterDef.DefaultDeploymentTags[0], "{DD254E62-A32D-4631-8152-A35CF84D4E05}", "NJIntroMission_InfiltratorDeploymentTag");

                        newTacCharacterDef.DefaultDeploymentTags = new ActorDeploymentTagDef[] { _infiltratorDeploymentTag };

                        _introMission.ParticipantsData[1].UniqueUnits = _introMission.ParticipantsData[1].UniqueUnits.AddToArray(new TacMissionTypeParticipantData.UniqueChatarcterBind
                        {
                            Amount = new Base.Utils.RangeDataInt(1, 1),
                            Character = newTacCharacterDef,
                            Difficulty = DefCache.GetDef<GameDifficultyLevelDef>("StoryMode_DifficultyLevelDef")
                        });

                        TFTVLogger.Always($"_introMission.ParticipantsData[1].UniqueUnits count: {_introMission.ParticipantsData[1].UniqueUnits.Count()}");

                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

            }

            internal class Geoscape
            {

                public static void RecordHavenName(GeoMission mission)
                {
                    try
                    {
                        if (!IsIntroMission(null, mission))
                        {
                            return;
                        }

                        _havenName = mission.Site.LocalizedSiteName;
                        NewNJIntroMissionHint.Text = new Base.UI.LocalizedTextBind(TFTVCommonMethods.ConvertKeyToString(_hintDescKey).Replace("[HavenName]", _havenName), true);

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }

                }


            }


            internal class MissionStartChanges
            {

                private static void AddKillInfiltratorObjective()
                {
                    try
                    {
                        TFTVUITactical.SecondaryObjectivesTactical.AddAdditionalSecondaryObjective(_killInfiltratorObjective);
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void MovePlayerDeployZone(TacticalLevelController controller)
                {
                    try
                    {

                        TFTVLogger.Always($"Moving Player DeployZone", false);

                        TacticalDeployZone vehicleDeployZone = controller.Map.GetActors<TacticalDeployZone>().FirstOrDefault(tdz => tdz.name.StartsWith("Deploy_Player_3x3_Vehicle"));
                        List<TacticalDeployZone> gruntDeployZones = controller.Map.GetActors<TacticalDeployZone>().Where(tdz => tdz.name.StartsWith("Deploy_Player_1x1_Elite_Grunt_Drone")).ToList();

                        foreach (TacticalDeployZone deployZone in gruntDeployZones)
                        {
                            deployZone.SetPosition(vehicleDeployZone.Pos);
                            BoxCollider boxCollider = deployZone.GetComponent<BoxCollider>();
                            boxCollider.center = Vector3.zero; // Reset the center
                            boxCollider.size = Vector3.one; // Reset the size (if necessary)

                            // Calculate the new center position based on the GameObject's position
                            Vector3 newColliderCenter = deployZone.transform.InverseTransformPoint(vehicleDeployZone.Pos);
                            boxCollider.center = newColliderCenter;
                        }
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }

                }

                private static readonly Vector3 _ogSoldierSpawnPos = new Vector3(5.5f, 0.0f, -2.0f);
                private static readonly Vector3 _priestSpawnLocation = new Vector3(1.5f, 2.4f, -7.5f);
                //   private static readonly Vector3 _ogPriestInfiltratorSpawnPos = new Vector3(-4.0f, 4.0f, -6.0f);
                private static readonly List<Vector3> _newSoldierSpawnPos = new List<Vector3>()
                {
                    new Vector3(-8.5f, 2.4f, -6.5f),
                    new Vector3(-5.5f, 0.0f, 3.5f),
                    new Vector3(4.5f, 2.4f, 3.5f)

                };


                private static void ModifyEnemyDeployZones(TacticalLevelController controller)
                {
                    try
                    {


                        TacticalDeployZone soldierSpawn = controller.Map.GetActors<TacticalDeployZone>().FirstOrDefault(tdz => tdz.Pos == _ogSoldierSpawnPos);

                        List<FixedDeployConditionData> fixedConditions = soldierSpawn.FixedDeployment.ToList();
                        List<MissionDeployConditionData> missionConditions = soldierSpawn.MissionDeployment.ToList();

                        TFTVLogger.Always($"fixedConditions {fixedConditions.Count}");
                        TFTVLogger.Always($"missionConditions {missionConditions.Count}");

                        List<MissionDeployConditionData> missionConditionsPriest = new List<MissionDeployConditionData>() { new MissionDeployConditionData
                        { ActivateOnTurn = 0, ActorTagDef = _priestDeploymentTag, DeactivateAfterTurn = 0, ExcludeActor = false} };

                        List<MissionDeployConditionData> missionConditionsInfiltrator = new List<MissionDeployConditionData>() { new MissionDeployConditionData
                        { ActivateOnTurn = 0, ActorTagDef = _infiltratorDeploymentTag, DeactivateAfterTurn = 0, ExcludeActor = false} };

                        TacticalDeployZoneDef tacticalDeployZoneDef = DefCache.GetDef<TacticalDeployZoneDef>("Environment_DeployZoneDef");

                        TacticalDeployZone newSoldierSpawn0 = ActorSpawner.SpawnActor<TacticalDeployZone>(DefCache.GetDef<TacticalDeployZoneDef>("Environment_DeployZoneDef"), null, false);
                        TacticalDeployZone newSoldierSpawn1 = ActorSpawner.SpawnActor<TacticalDeployZone>(DefCache.GetDef<TacticalDeployZoneDef>("Environment_DeployZoneDef"), null, false);
                        TacticalDeployZone newPriestSpawn = ActorSpawner.SpawnActor<TacticalDeployZone>(DefCache.GetDef<TacticalDeployZoneDef>("Environment_DeployZoneDef"), null, false);
                        TacticalDeployZone newInfiltratorSpawn = ActorSpawner.SpawnActor<TacticalDeployZone>(DefCache.GetDef<TacticalDeployZoneDef>("Environment_DeployZoneDef"), null, false);

                        newPriestSpawn.SetPosition(_priestSpawnLocation);
                        newPriestSpawn.SetFaction(controller.GetFactionByCommandName("neut"), TacMissionParticipant.Intruder);
                        newPriestSpawn.FixedDeployment = fixedConditions;
                        newPriestSpawn.MissionDeployment = missionConditionsPriest;

                        newPriestSpawn.GetComponent<BoxCollider>().size = new Vector3(4f, 4f, 4f);


                        newInfiltratorSpawn.SetPosition(_commsVector3);
                        newInfiltratorSpawn.SetFaction(controller.GetFactionByCommandName("neut"), TacMissionParticipant.Intruder);
                        newInfiltratorSpawn.FixedDeployment = fixedConditions;
                        newInfiltratorSpawn.MissionDeployment = missionConditionsInfiltrator;

                        newInfiltratorSpawn.GetComponent<BoxCollider>().size = new Vector3(8f, 8f, 8f);

                        soldierSpawn.SetPosition(_newSoldierSpawnPos[0]);
                        soldierSpawn.SetFaction(controller.GetFactionByCommandName("nj"), TacMissionParticipant.Residents);

                        newSoldierSpawn0.SetPosition(_newSoldierSpawnPos[1]);
                        newSoldierSpawn0.SetFaction(controller.GetFactionByCommandName("nj"), TacMissionParticipant.Residents);
                        newSoldierSpawn0.FixedDeployment = soldierSpawn.FixedDeployment;
                        newSoldierSpawn0.MissionDeployment = soldierSpawn.MissionDeployment;

                        newSoldierSpawn1.SetPosition(_newSoldierSpawnPos[2]);
                        newSoldierSpawn1.SetFaction(controller.GetFactionByCommandName("nj"), TacMissionParticipant.Residents);
                        newSoldierSpawn1.FixedDeployment = soldierSpawn.FixedDeployment;
                        newSoldierSpawn1.MissionDeployment = soldierSpawn.MissionDeployment;

                        newSoldierSpawn0.DoEnterPlay();

                        newSoldierSpawn1.DoEnterPlay();


                        newPriestSpawn.DoEnterPlay();

                        newInfiltratorSpawn.DoEnterPlay();

                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                public static void ModifyDeployZones(TacticalLevelController controller)
                {
                    try
                    {
                        if (!IsIntroMission(controller))
                        {
                            return;
                        }

                        AddKillInfiltratorObjective();
                        MovePlayerDeployZone(controller);
                        ModifyEnemyDeployZones(controller);

                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }

                }

                public static void CreateObjectiveToTurnOffPropaganda(TacticalLevelController controller)
                {
                    try
                    {
                        if (_commsConsole != null)
                        {
                            return;
                        }

                        if (!IsIntroMission(controller))
                        {
                            return;
                        }

                        SpawnInteractionPoint();


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }


                private static Vector3 _commsVector3 = new Vector3(-2.5f, 2.4f, -7.5f);
                private static void SpawnInteractionPoint()
                {
                    try
                    {
                        string name = "CommsConsole";
                        Vector3 position = _commsVector3;

                        StructuralTargetDeploymentDef stdDef = DefCache.GetDef<StructuralTargetDeploymentDef>("HackableConsoleStructuralTargetDeploymentDef");

                        TacActorData tacActorData = new TacActorData
                        {
                            ComponentSetTemplate = stdDef.ComponentSet
                        };


                        StructuralTargetInstanceData structuralTargetInstanceData = tacActorData.GenerateInstanceData() as StructuralTargetInstanceData;
                        //  structuralTargetInstanceData.FacilityID = facilityID;
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

                        //    TacticalActorBase

                        StatusDef activeConsoleStatusDef = DefCache.GetDef<StatusDef>("ActiveInteractableConsole_StatusDef");
                        structuralTarget.Status.ApplyStatus(activeConsoleStatusDef);

                        _commsConsole = structuralTarget;

                        TFTVLogger.Always($"{name} is at position {position}");
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }


                public static void TurnNeutralGruntsOverToNJAndApplyMCStatus(TacticalLevelController controller)
                {
                    try
                    {


                        if (!IsIntroMission(controller))
                        {
                            return;
                        }

                        MindControlStatusDef mindControlStatusDef = DefCache.GetDef<MindControlStatusDef>("MindControl_StatusDef");

                        //  MindControlAbility

                        // TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();
                        TacticalFaction neutralFaction = controller.GetFactionByCommandName("neut");
                        TacticalFaction njFaction = controller.GetFactionByCommandName("nj");

                        TacticalActor priest = neutralFaction.TacticalActors.FirstOrDefault(ta => ta.HasGameTag(DefCache.GetDef<ClassTagDef>("Priest_ClassTagDef")));

                        TFTVHumanEnemies.AssignHumanEnemiesTags(njFaction, controller, false);

                        foreach (TacticalActor tacticalActor in njFaction.TacticalActors)
                        {
                            // tacticalActor.SetFaction(njFaction, TacMissionParticipant.Residents);
                            MindControlStatus mindControlStatus = tacticalActor.Status.ApplyStatus<MindControlStatus>(mindControlStatusDef, priest);
                            mindControlStatus.SetUpkeepCost(2);
                            priest.Status.ApplyStatus<TacStatus>(DefCache.GetDef<TacStatusDef>("ControlsActor_StatusDef"));
                            priest.GetComponent<TacActorEventusComponent>().RaiseEvent(DefCache.GetDef<MindControlAbilityDef>("Priest_MindControl_AbilityDef").EndEffect);

                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

            }



            internal class MissionQuips
            {


                public static Sprite ConvertTexture2DToSprite(Texture2D texture)
                {
                    // Create a new sprite from the texture
                    Sprite sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f)
                    );

                    return sprite;
                }

                private static void RunPhoenixConversation(TacticalActor startingQuipper, string startingQuip, string reactionQuip, string finalQuip, object context = null)
                {
                    try
                    {


                        TacticalActor seniorOperative = FindPXOperativeBySeniority(startingQuipper);

                        TFTVLogger.Always($"starting Quipper is {startingQuipper.DisplayName}, he will say {startingQuip}" +
                            $"\nseniorOperative is {seniorOperative.DisplayName}, he will say {reactionQuip}");

                        string processedStartingQuip = QuipProcessor(startingQuip, null, context);
                        string processedReactionQuip = QuipProcessor(reactionQuip, startingQuipper, context);

                        RunPhoenixOverlayQuip(startingQuipper, processedStartingQuip);
                        RunPhoenixOverlayQuip(seniorOperative, processedReactionQuip, 4);

                        if (finalQuip != "")
                        {
                            string processedFinalQuip = QuipProcessor(finalQuip, startingQuipper, context);
                            RunPhoenixOverlayQuip(startingQuipper, processedFinalQuip, 7);
                        }


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static string QuipProcessor(string quip, TacticalActor replyToActor = null, object context = null)
                {
                    try
                    {
                        quip = TFTVCommonMethods.ConvertKeyToString(quip);

                        TFTVLogger.Always($"quip before replace: {quip}");

                        if (replyToActor != null && quip.Contains("[OperativeName]"))
                        {
                            quip = quip.Replace("[OperativeName]", TFTVTacticalUtils.GetCharacterLastName(replyToActor.DisplayName));
                        }

                        TFTVLogger.Always($"quip after replace: {quip}");

                        if (context != null && context is TacticalActor talkedAboutActor)
                        {
                            bool male = true;

                            if (talkedAboutActor.GameTags.Any(gt => gt == Shared.SharedGameTags.Genders.FemaleTag))
                            {
                                male = false;
                            }

                            quip = TFTVTacticalUtils.AdjustTextForGender(quip, male);
                            TFTVLogger.Always($"quip after gender adjustment: {quip}");
                        }

                        return quip.Replace("\"", "");

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }





                private static TacticalActor FindPXOperativeBySeniority(TacticalActor excludeActor = null, bool senior = true)
                {
                    try
                    {
                        TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                        List<TacticalActor> tacticalActors = GetTacticalActorsPhoenix(controller);

                        // tacticalActors.AddRange(GetEligibleForQuipsPhoenixActors());

                        if (excludeActor != null && tacticalActors.Contains(excludeActor))
                        {
                            tacticalActors.Remove(excludeActor);
                        }

                        if (tacticalActors.Count == 0)
                        {
                            return null;
                        }

                        return tacticalActors[0];

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }


                public static void RunPhoenixOverlayQuip(TacticalActor actor, string text, float delay = 2)
                {
                    try
                    {


                        TFTVLogger.Always($"running Phoenix quip for {actor.DisplayName}");

                        UIModuleSquadManagement uIModuleSquadManagement = actor.TacticalLevel.View.TacticalModules.SquadManagementModule;

                        SquadMemberScrollerController squadMemberScrollerController = uIModuleSquadManagement.SquadMemberScroller;

                        FieldInfo fieldInfo_soldierPortraits = typeof(SquadMemberScrollerController).GetField("_soldierPortraits", BindingFlags.Instance | BindingFlags.NonPublic);

                        Dictionary<TacticalActor, PortraitSprites> soldierPortraits = (Dictionary<TacticalActor, PortraitSprites>)fieldInfo_soldierPortraits.GetValue(squadMemberScrollerController);
                        Sprite sprite = null;

                        if (!soldierPortraits.ContainsKey(actor))
                        {
                            return;
                        }


                        if (soldierPortraits[actor].RenderedPortrait != null)
                        {
                            sprite = ConvertTexture2DToSprite(soldierPortraits[actor].RenderedPortrait);

                        }
                        else
                        {
                            sprite = soldierPortraits[actor].Portrait;

                        }
                        // Sprite portrait = Helper.CreatePortraitFromImageFile(CharacterPortrait.characterPics[actor.GeoUnitId]);

                        DelayedInvoker.Run(delay, () =>
                        {
                            OverlayQuip.Create(sprite, text);
                        });

                        // OverlayQuip.Create(sprite, TFTVCommonMethods.ConvertKeyToString(text));

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }

                }




                /* [HarmonyPatch(typeof(SquadMemberScrollerController), "UpdatePortraitForSoldier")]
                 public static class SquadMemberScrollerController_UpdatePortraitForSoldier_patch
                 {

                     public static void Postfix(SquadMemberScrollerController __instance, TacticalActor actor)
                     {
                         try
                         {
                             TFTVLogger.Always($"{actor?.name}");

                             if (actor.TacticalFaction.ParticipantKind == TacMissionParticipant.Player)
                             {
                                 MissionQuips.RunPhoenixOverlayQuip(actor as TacticalActor);
                             }

                         }
                         catch (Exception e)
                         {
                             TFTVLogger.Error(e);
                             throw;
                         }
                     }
                 }*/


                public static bool QuipJustRun = false;

                [HarmonyPatch(typeof(TacContextHelpManager), "EventTypeTriggered")]
                public static class TacContextHelpManager_EventTypeTriggered_patch
                {

                    public static void Postfix(HintTrigger trigger, TacContextHelpManager __instance, object context, object conditionContext)
                    {
                        try
                        {


                            if (GameUtl.CurrentLevel() == null) 
                            {
                                return;
                            }

                            TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                            if (controller==null || !IsIntroMission(controller)) 
                            {
                                return;
                            }

                                if (controller.CurrentFaction != controller.GetFactionByCommandName("px") || QuipJustRun || context == null)
                            {
                                return;
                            }


                            if (trigger == HintTrigger.ActorMoved || trigger == HintTrigger.ActorSelected
                                || trigger == HintTrigger.ActorTagStartTurn) //|| trigger == HintTrigger.AbilityExecuted)
                            {

                                TacticalActor listener = context as TacticalActor;

                                //TFTVLogger.Always($"got here for listener {listener.DisplayName}");

                                if (listener != null && IsIntroMission(listener.TacticalLevel))
                                {
                                    NJIntroBanter(listener);
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


               
                private static void NJIntroPriestKilled(DeathReport deathReport)
                {
                    try
                    {
                        if (!deathReport.Actor.HasGameTag(_priestTag))
                        {
                            return;
                        }
                        
                        TacticalActor phoenixQuipper = deathReport.Killer as TacticalActor ?? GetTacticalActorsPhoenix(deathReport.Actor.TacticalLevel).First();

                        RunPhoenixOverlayQuip(phoenixQuipper, QuipProcessor(_priestKilledKey));

                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static void NJIntroInfiltratorKilled(DeathReport deathReport)
                {
                    try
                    {
                        if (!deathReport.Actor.HasGameTag(_infiltratorTag))
                        {
                            return;
                        }

                        TacticalActor phoenixQuipper = deathReport.Killer as TacticalActor ?? GetTacticalActorsPhoenix(deathReport.Actor.TacticalLevel).First();

                        RunPhoenixOverlayQuip(phoenixQuipper, QuipProcessor(_infiltratorKilledKey));

                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }

                }



                [HarmonyPatch(typeof(TacContextHelpManager), "OnActorDied")]
                public static class TacContextHelpManager_OnActorDied_patch
                {

                    public static void Postfix(TacContextHelpManager __instance, DeathReport deathReport)
                    {
                        try
                        {
                            TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                            if (controller.CurrentFaction != controller.GetFactionByCommandName("px") || QuipJustRun || deathReport.Actor == null)
                            {
                                return;
                            }

                            if (IsIntroMission(deathReport.Actor.TacticalLevel))
                            {
                                NJIntroInfiltratorKilled(deathReport);
                                NJIntroPriestKilled(deathReport);
                            }

                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }
                }





                private static readonly string _propogandaKeyBase = "TFTV_KEY_NJ_INTRO_MISS_BANTER_PROPAGANDA_";
                private static readonly string _mcQuipsKeyBase = "TFTV_KEY_NJ_INTRO_MISS_BANTER_MC_";
                private static readonly string _postMCQuipsKeyBase = "TFTV_KEY_NJ_INTRO_MISS_BANTER_AFTER_MC_";

                private static readonly string _njBanterMCFirstKey = "TFTV_KEY_NJ_INTRO_MISS_BANTER_PHOENIX_MC_0";
                private static readonly string _njBanterPostMCFirstKey = "TFTV_KEY_NJ_INTRO_MISS_BANTER_PHOENIX_MC_2";
                private static readonly string _priestSeenFirstKey = "TFTV_KEY_NJ_INTRO_MISS_BANTER_PRIEST_0";
                private static readonly string _infiltratorSeenFirstKey = "TFTV_KEY_NJ_INTRO_MISS_BANTER_PHOENIX_INF_0";
                private static readonly string _priestKilledKey = "TFTV_KEY_NJ_INTRO_MISS_BANTER_PHOENIX_ENDING_0";
                private static readonly string _infiltratorKilledKey = "TFTV_KEY_NJ_INTRO_MISS_BANTER_PHOENIX_ENDING_1";

                private static List<string> _propagandaLines = new List<string>();
                private static List<string> _mindControlledQuips = new List<string>();
                private static List<string> _postMindControlledQuips = new List<string>();

                private static readonly ClassTagDef _priestTag = DefCache.GetDef<ClassTagDef>("Priest_ClassTagDef");
                private static readonly ClassTagDef _infiltratorTag = DefCache.GetDef<ClassTagDef>("Infiltrator_ClassTagDef");
                public static void PopulateQuipList(string keyBase, List<string> listToPopulate)
                {
                    try
                    {
                        TFTVLogger.Always($"running PopulatePropagandaList for {keyBase}");

                        listToPopulate.Clear();

                        bool keysRemain = true;

                        for (int x = 0; keysRemain; x++)
                        {
                            string keyId = $"{keyBase}{x}";
                            string key = "";

                            foreach (var source in LocalizationManager.Sources)
                            {
                                var term = source.GetTermsList().FirstOrDefault(t => t.Contains(keyId));
                                if (!string.IsNullOrEmpty(term))
                                {
                                    // TFTVLogger.Always($"found {term}");
                                    key = term;
                                }
                            }

                            if (key != "")
                            {

                                if (!UsedQuips.Contains(key))
                                {
                                    listToPopulate.Add(TFTVCommonMethods.ConvertKeyToString(key));
                                }
                            }
                            else
                            {
                                keysRemain = false;
                            }
                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                public static void NJIntroBanter(TacticalActor listener)
                {
                    try
                    {

                        if (RunInfiltratorBanter(listener))
                        {

                        }
                        else if (RunPriestBanter(listener))
                        {

                        }
                        else if (RunNJBanter(listener))
                        {

                        }
                        else
                        {
                            return;

                        }


                        QuipJustRun = true;

                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }

                }

                private static bool CheckListenerAwareness(TacticalActor listener, TacticalActor quipper)
                {
                    try
                    {
                        return CheckActorInFrame(quipper)
                                      && quipper.TacticalFaction.Vision.IsRevealed(listener)
                                      && listener.TacticalFaction.Vision.IsRevealed(quipper)
                                      && TacticalFactionVision.CheckVisibleLineBetweenActors(quipper, quipper.Pos, listener, true);


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }

                }


                private static bool CheckActorInFrame(TacticalActor tacticalActor)
                {
                    try
                    {
                        if (!tacticalActor.IsAlive)
                        {
                            return false;
                        }

                        TacticalLevelController controller = tacticalActor.TacticalLevel;

                        MethodInfo methodInfo = typeof(PlanarScrollCamera).GetMethod("IsInsideCinemachineFrame", BindingFlags.NonPublic | BindingFlags.Instance);
                        PlanarScrollCamera planarScrollCamera = controller.View.CameraManager.CurrentBehavior as PlanarScrollCamera;

                        // TFTVLogger.Always($"considering {tacticalActor.name} at pos {tacticalActor.Pos}");

                        return (bool)methodInfo.Invoke(planarScrollCamera, new object[] { tacticalActor.Pos });

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static bool RunPriestBanter(TacticalActor listener)
                {
                    try
                    {

                        if (UsedQuips.Contains(_priestSeenFirstKey))
                        {
                            return false;
                        }

                        TacticalActor priest = GetEnemyActorWithClassTag(_priestTag);

                        TFTVLogger.Always($"found priest? {priest != null}");

                        if (priest == null)
                        {
                            return false;
                        }

                        if (!CheckListenerAwareness(listener, priest))
                        {
                            return false;
                        }

                        string quip = TFTVCommonMethods.ConvertKeyToString(_priestSeenFirstKey);

                        UsedQuips.Add(_priestSeenFirstKey);

                        FloatingTextManager.ShowFloatingText(priest, quip, TFTVUITactical.WhiteColor);

                        RunPhoenixConversation(listener, "TFTV_KEY_NJ_INTRO_MISS_BANTER_PHOENIX_PRIEST_0", "TFTV_KEY_NJ_INTRO_MISS_BANTER_PHOENIX_PRIEST_1", "");



                        return true;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static bool RunInfiltratorBanter(TacticalActor listener)
                {
                    try
                    {
                        if (UsedQuips.Contains(_infiltratorSeenFirstKey))
                        {
                            return false;
                        }

                        TacticalActor infiltrator = GetEnemyActorWithClassTag(_infiltratorTag);

                        TFTVLogger.Always($"found infiltrator? {infiltrator != null}");

                        if (infiltrator == null)
                        {
                            return false;
                        }

                        if (!CheckListenerAwareness(listener, infiltrator))
                        {
                            return false;
                        }

                        string quip = TFTVCommonMethods.ConvertKeyToString(_infiltratorSeenFirstKey);

                        UsedQuips.Add(_infiltratorSeenFirstKey);

                        // FloatingTextManager.ShowFloatingText(infiltrator, quip, TFTVUITactical.WhiteColor);

                        RunPhoenixConversation(listener, "TFTV_KEY_NJ_INTRO_MISS_BANTER_PHOENIX_INF_0", "TFTV_KEY_NJ_INTRO_MISS_BANTER_PHOENIX_INF_1", "TFTV_KEY_NJ_INTRO_MISS_BANTER_PHOENIX_INF_2");

                        return true;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }


                private static bool RunNJBanter(TacticalActor listener)
                {
                    try
                    {
                        List<string> eligibleQuips = new List<string>();
                        List<TacticalActor> eligibleQuippers = new List<TacticalActor>();

                        List<TacticalActor> mCedNJ = GetRevealedMindControlledByPhoenixEnemy();
                        List<TacticalActor> neutNJ = GetRevealedNeutralTacticalActors();

                        if (mCedNJ.Count > 0 && !UsedQuips.Contains(_njBanterMCFirstKey))
                        {
                            eligibleQuippers = mCedNJ;
                            eligibleQuips = _mindControlledQuips;
                        }
                        else if (neutNJ.Count > 0 && !UsedQuips.Contains(_njBanterPostMCFirstKey))
                        {
                            eligibleQuippers = neutNJ;
                            eligibleQuips = _postMindControlledQuips;

                        }
                        else
                        {
                            return false;
                        }

                        if (eligibleQuips.Count == 0 || eligibleQuippers.Count == 0)
                        {
                            return false;
                        }


                        foreach (TacticalActor tacticalActor in eligibleQuippers)
                        {
                            if (CheckListenerAwareness(listener, tacticalActor))
                            {
                                string quip = eligibleQuips.GetRandomElement();

                                FloatingTextManager.ShowFloatingText(tacticalActor, quip, TFTVUITactical.WhiteColor);
                                // NJQuips.quipShown = true;

                                if (_mindControlledQuips.Contains(quip))
                                {
                                    RunPhoenixConversation(listener, "TFTV_KEY_NJ_INTRO_MISS_BANTER_PHOENIX_MC_0", "TFTV_KEY_NJ_INTRO_MISS_BANTER_PHOENIX_MC_1", "", tacticalActor);
                                    _mindControlledQuips.Remove(quip);
                                    UsedQuips.Add("TFTV_KEY_NJ_INTRO_MISS_BANTER_PHOENIX_MC_0");
                                }

                                if (_postMindControlledQuips.Contains(quip))
                                {
                                    RunPhoenixConversation(listener, "TFTV_KEY_NJ_INTRO_MISS_BANTER_PHOENIX_MC_2", "TFTV_KEY_NJ_INTRO_MISS_BANTER_PHOENIX_MC_3", "");
                                    _postMindControlledQuips.Remove(quip);
                                    UsedQuips.Add("TFTV_KEY_NJ_INTRO_MISS_BANTER_PHOENIX_MC_2");
                                }

                                UsedQuips.Add(quip);

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


                internal class NJQuips
                {


                    public static void PopulateQuips()
                    {
                        try
                        {
                            PopulateQuipList(_mcQuipsKeyBase, _mindControlledQuips);
                            PopulateQuipList(_postMCQuipsKeyBase, _postMindControlledQuips);

                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }
                }


                internal class Propaganda
                {
                    public static void StopPropaganda(TacticalLevelController controller, Status status)
                    {
                        try
                        {


                            if (!IsIntroMission(controller))
                            {
                                return;
                            }

                            if (status.Def == DefCache.GetDef<StatusDef>("ConsoleActivated_StatusDef"))
                            {
                                TFTVTauntsAndQuips.FloatingTextManager.StopFloatingTextLoop();
                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }





                    public static void PropagandaQuips(TacticalLevelController controller)
                    {
                        try
                        {
                            if (!IsIntroMission(controller))
                            {
                                return;
                            }

                            if (_propagandaLines.Count == 0)
                            {
                                PopulateQuipList(_propogandaKeyBase, _propagandaLines);
                            }

                            List<string> propagandaLines = _propagandaLines;


                            List<DestructableBase> propagandaScreen =
                                UnityEngine.Object.FindObjectsOfType<DestructableBase>().Where(b => (b.name.StartsWith("NJ_Propaganda") || b.name.StartsWith("NJ_LoCov_LGT_Hologram") || b.name.StartsWith("NJ_Prop_HologramFrame"))).ToList();


                            int randomStart = UnityEngine.Random.Range(0, propagandaLines.Count());

                            foreach (DestructableBase breakable in propagandaScreen)
                            {
                                // TFTVLogger.Always($"found {breakable.name} at position {breakable.transform.position} toughness {breakable?.GetToughness()} rotation: {breakable.transform.rotation}");
                                TFTVTauntsAndQuips.FloatingTextManager.StartFloatingTextLoop(breakable, propagandaLines, 10, randomStart);
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


    }
}
