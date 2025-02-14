using Base;
using Base.Cameras;
using Base.Defs;
using Base.Entities;
using Base.Entities.Statuses;
using HarmonyLib;
using I2.Loc;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.Levels.Missions;
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
using PhoenixPoint.Tactical.View.ViewControllers;
using PhoenixPoint.Tactical.View.ViewModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static PhoenixPoint.Tactical.View.ViewControllers.SquadMemberScrollerController;
using static TFTV.TFTVTauntsAndQuips;

namespace TFTV
{
    internal class TFTVNJQuestline
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;

        public static bool NewNJIntroMission = false;

        internal class IntroMission
        {
            private static readonly CustomMissionTypeDef _introMission = DefCache.GetDef<CustomMissionTypeDef>("StoryNJ0_CustomMissionTypeDef");
            private static StructuralTarget _commsConsole = null;
            private static KillActorFactionObjectiveDef _killInfiltratorObjective = null;
            private static ActorDeploymentTagDef _priestDeploymentTag = null;
            private static ActorDeploymentTagDef _infiltratorDeploymentTag = null;

            public static bool IsIntroMission(TacticalLevelController controller)
            {
                try
                {
                    return controller.TacMission.MissionData.MissionType == _introMission;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            public static void ClearDataOnMissionLoadAndStateChange()
            {
                try
                {
                    _commsConsole = null;

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
                    if (!NewNJIntroMission)
                    {
                        return;
                    }

                    MissionStartChanges.CreateObjectiveToTurnOffPropaganda(controller);
                    MissionQuips.Propaganda.PropagandaQuips(controller);
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

                        newInfiltratorSpawn.GetComponent<BoxCollider>().size = new Vector3(6f, 6f, 4f);

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
                        if (!NewNJIntroMission || !IsIntroMission(controller))
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
                        if (!NewNJIntroMission)
                        {
                            return;
                        }

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

                public static void RunPhoenixOverlayQuip(TacticalActor actor, string text)
                {
                    try
                    {
                        TFTVLogger.Always($"running Phoenix quip for {actor.DisplayName}");

                        UIModuleSquadManagement uIModuleSquadManagement = actor.TacticalLevel.View.TacticalModules.SquadManagementModule;

                        SquadMemberScrollerController squadMemberScrollerController = uIModuleSquadManagement.SquadMemberScroller;

                        FieldInfo fieldInfo_soldierPortraits = typeof(SquadMemberScrollerController).GetField("_soldierPortraits", BindingFlags.Instance | BindingFlags.NonPublic);



                        Dictionary<TacticalActor, PortraitSprites> soldierPortraits = (Dictionary<TacticalActor, PortraitSprites>)fieldInfo_soldierPortraits.GetValue(squadMemberScrollerController);
                        Sprite sprite = null;

                        if (soldierPortraits[actor].RenderedPortrait != null)
                        {
                            sprite = ConvertTexture2DToSprite(soldierPortraits[actor].RenderedPortrait);

                        }
                        else
                        {
                            sprite = soldierPortraits[actor].Portrait;

                        }
                        // Sprite portrait = Helper.CreatePortraitFromImageFile(CharacterPortrait.characterPics[actor.GeoUnitId]);

                        OverlayQuip.Create(sprite, text);

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


                [HarmonyPatch(typeof(TacContextHelpManager), "OnActorSelected")]
                public static class TacContextHelpManager_OnActorSelected_patch
                {

                    public static void Postfix(TacContextHelpManager __instance, TacticalActor actor)
                    {
                        try
                        {

                            if (actor != null)
                            {
                                if (NewNJIntroMission && IsIntroMission(actor.TacticalLevel))
                                {

                                    TFTVLogger.Always($"{actor.name} selected");
                                    RunQuips(_pendingNJQuips, actor);



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


                private static readonly string _propogandaKeyBase = "TFTV_KEY_NJ_INTRO_MISS_BANTER_PROPAGANDA_";
                private static readonly string _mcQuipsKeyBase = "TFTV_KEY_NJ_INTRO_MISS_BANTER_MC_";
                private static readonly string _postMCQuipsKeyBase = "TFTV_KEY_NJ_INTRO_MISS_BANTER_AFTER_MC_";


                private static List<string> _propagandaLines = new List<string>();
                private static List<string> _mindControlledQuips = new List<string>();
                private static List<string> _postMindControlledQuips = new List<string>();

                private static Dictionary<TacticalActor, string> _pendingNJQuips = new Dictionary<TacticalActor, string>();

                public static void PopulateQuipList(string keyBase, List<string> listToPopulate)
                {
                    try
                    {
                        TFTVLogger.Always($"running PopulatePropagandaList for {keyBase}");

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
                                listToPopulate.Add(TFTVCommonMethods.ConvertKeyToString(key));

                            }
                            else
                            {
                                keysRemain = false;
                            }
                        }

                        /*  foreach (string text in _propagandaLines)
                          {
                              TFTVLogger.Always($"{text}", false);
                          }*/

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


                private static void RunQuips(Dictionary<TacticalActor, string> quipList, TacticalActor listener)
                {
                    try
                    {
                        if (_pendingNJQuips.Count == 0)
                        {
                            return;
                        }

                        bool quipShown = false;

                        foreach (TacticalActor tacticalActor in quipList.Keys)
                        {
                            if (CheckActorInFrame(tacticalActor)
                                && tacticalActor.TacticalFaction.Vision.IsRevealed(listener)
                                && TacticalFactionVision.CheckVisibleLineBetweenActors(tacticalActor, tacticalActor.Pos, listener, true))
                            {
                                FloatingTextManager.ShowFloatingText(tacticalActor, quipList[tacticalActor], TFTVUITactical.WhiteColor);
                                quipShown = true;
                                break;
                            }
                        }

                        if (quipShown)
                        {
                            quipList.Clear();
                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }


                internal class NJQuips
                {

                    private static void QuipsFromMCedNJ(TacticalLevelController controller)
                    {
                        try
                        {
                            TacticalFaction phoenixFaction = controller.GetFactionByCommandName("px");

                            List<TacticalActor> mindcontrolledActors = controller.Map.GetTacActors<TacticalActor>(phoenixFaction, FactionRelation.Enemy).Where(ta => ta.IsAlive && ta.IsRevealedToViewer && ta.Status.Statuses.Any(s => s is MindControlStatus)).ToList();

                            TFTVLogger.Always($"mindcontrolledActors.Count: {mindcontrolledActors.Count}");

                            MethodInfo methodInfo = typeof(PlanarScrollCamera).GetMethod("IsInsideCinemachineFrame", BindingFlags.NonPublic | BindingFlags.Instance);
                            PlanarScrollCamera planarScrollCamera = controller.View.CameraManager.CurrentBehavior as PlanarScrollCamera;

                            if (mindcontrolledActors.Count != 0 && _mindControlledQuips.Count == 0)
                            {
                                PopulateQuipList(_mcQuipsKeyBase, _mindControlledQuips);
                            }

                            // int counter = 0;

                            for (int x = 0; x < Mathf.Min(mindcontrolledActors.Count, 2); x++)
                            {
                                string quip = _mindControlledQuips[UnityEngine.Random.Range(0, _mindControlledQuips.Count)];
                                TacticalActor tacticalActor = mindcontrolledActors[x];

                                _pendingNJQuips.Add(tacticalActor, quip);
                                _mindControlledQuips.Remove(quip);

                            }

                        }

                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }

                    }

                    private static void QuipsFromPostMCedNJ(TacticalLevelController controller)
                    {
                        try
                        {
                            TacticalFaction phoenixFaction = controller.GetFactionByCommandName("px");

                            List<TacticalActor> neutralNJactors = controller.Map.GetTacActors<TacticalActor>(phoenixFaction, FactionRelation.Neutral).Where(ta => ta.IsAlive && ta.IsRevealedToViewer).ToList();

                            TFTVLogger.Always($"neutralNJactors.Count: {neutralNJactors.Count}");

                            if (neutralNJactors.Count != 0 && _postMindControlledQuips.Count == 0)
                            {
                                PopulateQuipList(_postMCQuipsKeyBase, _postMindControlledQuips);
                            }

                            for (int x = 0; x < Mathf.Min(2, neutralNJactors.Count); x++)
                            {
                                string quip = _postMindControlledQuips[UnityEngine.Random.Range(0, _postMindControlledQuips.Count)];
                                TacticalActor tacticalActor = neutralNJactors[x];
                                _pendingNJQuips.Add(tacticalActor, quip);
                                _postMindControlledQuips.Remove(quip);
                            }
                        }

                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }

                    }


                    public static void OnNewTurn(TacticalFaction faction)
                    {
                        try
                        {
                            if (!NewNJIntroMission)
                            {
                                return;
                            }

                            TacticalLevelController controller = faction.TacticalLevel;

                            if (!IsIntroMission(controller))
                            {
                                return;
                            }

                            if (faction == controller.GetFactionByCommandName("px"))
                            {
                                QuipsFromMCedNJ(controller);
                                QuipsFromPostMCedNJ(controller);
                            }

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
                            if (!NewNJIntroMission)
                            {
                                return;
                            }


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
