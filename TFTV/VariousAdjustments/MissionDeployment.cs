using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Levels.ActorDeployment;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Geoscape.Entities.Missions;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.ActorDeployment;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV.VariousAdjustments
{
    internal class MissionDeployment
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        internal static Dictionary<TacCharacterDef, int> GenerateSecondaryForce(TacticalLevelController controller)
        {
            try
            {

                Dictionary<ClassTagDef, int> reinforcements = TFTVUmbra.PickReinforcements(controller);
                Dictionary<TacCharacterDef, int> secondaryForce = new Dictionary<TacCharacterDef, int>();

                TacCharacterDef mindFragger = DefCache.GetDef<TacCharacterDef>("Facehugger_AlienMutationVariationDef");

                int difficulty = controller.Difficulty.Order;

                List<TacCharacterDef> availableTemplatesOrdered = new List<TacCharacterDef>(controller.TacMission.MissionData.UnlockedAlienTacCharacterDefs.OrderByDescending(tcd => tcd.DeploymentCost));

                foreach (TacCharacterDef tacCharacterDef in availableTemplatesOrdered)
                {
                    if (tacCharacterDef.ClassTag != null && !secondaryForce.ContainsKey(tacCharacterDef)
                        && reinforcements.ContainsKey(tacCharacterDef.ClassTag) && reinforcements[tacCharacterDef.ClassTag] > 0)
                    {
                        secondaryForce.Add(tacCharacterDef, 1);
                        reinforcements[tacCharacterDef.ClassTag] -= 1;
                        TFTVLogger.Always("Added " + tacCharacterDef.name + " to the Seconday Force");

                    }
                }

                secondaryForce.Add(mindFragger, difficulty);

                return secondaryForce;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        internal static bool CheckLOSToPlayer(TacticalLevelController contoller, Vector3 pos)
        {
            try
            {
                bool lOS = false;

                List<TacticalActor> phoenixOperatives = new List<TacticalActor>(contoller.GetFactionByCommandName("PX").TacticalActors);

                foreach (TacticalActor actor in phoenixOperatives)
                {
                    if (TacticalFactionVision.CheckVisibleLine(actor, actor.Pos, pos, actor.CharacterStats.Perception))
                    {
                        
                        lOS = true;
                        return lOS;
                    }


                }


                return lOS;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }


        }


        internal static void CheckDepolyZones(TacticalLevelController controller)
        {
            TFTVLogger.Always($"MissionDeployment.CheckDepolyZones() called ...");
            MissionDeployCondition missionDeployConditionToAdd = new MissionDeployCondition()
            {
                MissionData = new MissionDeployConditionData()
                {
                    ActivateOnTurn = 0,
                    DeactivateAfterTurn = 0,
                    ActorTagDef = TFTVMain.Main.DefCache.GetDef<ActorDeploymentTagDef>("Queen_DeploymentTagDef"),
                    ExcludeActor = false
                }
            };

            int numberOfSecondaryForces = controller.Difficulty.Order / 2;

            TFTVLogger.Always($"The map has {controller.Map.GetActors<TacticalDeployZone>(null).ToList().Count} deploy zones");
            foreach (TacticalDeployZone tacticalDeployZone in controller.Map.GetActors<TacticalDeployZone>(null).ToList())
            {
                TFTVLogger.Always($"Deployment zone {tacticalDeployZone} with Def '{tacticalDeployZone.TacticalDeployZoneDef}'");
                TFTVLogger.Always($"Mission participant is {tacticalDeployZone.MissionParticipant}");

                //TacticalFactionVision.CheckVisibleLineBetweenActors need this
                //
                //controller.TacMission.MissionData.UnlockedAlienTacCharacterDefs
             
                if (tacticalDeployZone.MissionParticipant == TacMissionParticipant.Player 
                    && !CheckLOSToPlayer(controller, tacticalDeployZone.Pos) && numberOfSecondaryForces>0)
                {
                  
                    
                    
                   // TacCharacterDef actorDef = new TacCharacterDef();
                    TacticalFaction faction = controller.GetTacticalFaction(TacMissionParticipant.Intruder);
                    ActorDeployData actorDeployData = null;
                    Dictionary<TacCharacterDef, int> secondaryForce = GenerateSecondaryForce(controller);
                    
                    foreach(TacCharacterDef tacCharacterDef in secondaryForce.Keys) 
                    {
                        for (int x = 0; x < secondaryForce[tacCharacterDef]; x++)
                        {
                            actorDeployData = ((!(tacCharacterDef is IDeployableUnit)) ? new ActorDeployData(tacCharacterDef) : ((IDeployableUnit)tacCharacterDef).GenerateActorDeployData());
                            actorDeployData.InitializeInstanceData();

                            TacticalActorBase tacticalActorBase = TacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, faction.TacticalFactionDef, faction.ParticipantKind, tacticalDeployZone.Pos, Quaternion.identity, null);

                            tacticalActorBase.Source = tacticalActorBase;
                        }
                    
                    }
                    numberOfSecondaryForces -= 1;

                    controller.SituationCache.Invalidate();
                    controller.View.ResetCharacterSelectedState();

                }


                if (tacticalDeployZone.MissionParticipant == TacMissionParticipant.None)
                {
                    TFTVLogger.Always($"Skipping none participant deploy zone");
                    continue;
                }
                if (tacticalDeployZone.name.Contains("Deploy_Crate_"))
                {
                    TFTVLogger.Always($"Skipping because we don't care about crates ... for now ;-)");
                    continue;
                }
                TFTVLogger.Always($"Deploy conditions:");
                foreach (DeployCondition deployCondition in tacticalDeployZone.DeployConditions)
                {
                    if (deployCondition is MissionDeployCondition missionDeployCondition)
                    {
                        TFTVLogger.Always($"Type is MissionDeployCondition");
                        TFTVLogger.Always($"TurnNumber (readonly): {missionDeployCondition.TurnNumber}");
                        TFTVLogger.Always($"ActivateOnTurn (readonly): {missionDeployCondition.ActivateOnTurn}");
                        TFTVLogger.Always($"DeactivateAfterTurn (readonly): {missionDeployCondition.DeactivateAfterTurn}");
                        TFTVLogger.Always($"MissionData fields:");
                        TFTVLogger.Always($"MissionData.ActivateOnTurn: {missionDeployCondition.MissionData.ActivateOnTurn}");
                        TFTVLogger.Always($"MissionData.DeactivateAfterTurn: {missionDeployCondition.MissionData.DeactivateAfterTurn}");
                        TFTVLogger.Always($"MissionData.ActorTagDef: {missionDeployCondition.MissionData.ActorTagDef}");
                        TFTVLogger.Always($"MissionData.ExcludeActor: {missionDeployCondition.MissionData.ExcludeActor}");
                    }
                    else if (deployCondition is FixedDeployCondition fixedDeployCondition)
                    {
                        TFTVLogger.Always($"Type is FixedDeployCondition");
                        TFTVLogger.Always($"TurnNumber (readonly): {fixedDeployCondition.TurnNumber}");
                        TFTVLogger.Always($"FixedData fields:");
                        TFTVLogger.Always($"FixedData.TurnNumber: {fixedDeployCondition.FixedData.TurnNumber}");
                        TFTVLogger.Always($"FixedData.TacActorDef: {fixedDeployCondition.FixedData.TacActorDef}");
                        TFTVLogger.Always($"FixedData.ForceSpawnAt: {fixedDeployCondition.FixedData.ForceSpawnAt}");
                        TFTVLogger.Always($"FixedData.CanFailToSpawn: {fixedDeployCondition.FixedData.CanFailToSpawn}");
                        TFTVLogger.Always($"FixedData.SpawnChancePerc: {fixedDeployCondition.FixedData.SpawnChancePerc}");
                    }
                    else
                    {
                        TFTVLogger.Always($"Type is unknown, this message should never appear!");
                    }
                }
            }
        }
    }
}
