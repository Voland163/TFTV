using Base.Defs;
using Epic.OnlineServices.Sessions;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Tactical.Levels.ActorDeployment;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections.LowLevel.Unsafe;
using HarmonyLib;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Tactical.Levels.Missions;
using PhoenixPoint.Tactical.Entities;

namespace TFTV
{
    internal class TFTVHavenDefense
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;


        public static void ConvertCiviliansToPX(TacticalLevelController level)
        {
            try
            {
                TacticalFaction phoenix = level.GetFactionByCommandName("PX");
                

                foreach (TacticalFaction faction in level.Factions)
                {

                    foreach (TacticalActorBase tacticalActorBase in faction.Actors)
                    {
                        if (tacticalActorBase.BaseDef.name == "Civilian_ActorDef")
                        {
                            tacticalActorBase.SetFaction(phoenix, TacMissionParticipant.Player);

                        }

                    }
                
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void ConvertDefendersToPX(TacticalLevelController level)
        {
            try
            {
                TacticalFaction phoenix = level.GetFactionByCommandName("PX");


                foreach (TacticalFaction faction in level.Factions)
                {
                    if (faction.GetRelationTo(phoenix) == FactionRelation.Enemy)
                    {
                        faction.ParticipantKind = TacMissionParticipant.Player;

                        foreach (TacticalActorBase tacticalActorBase in faction.Actors)
                        {
                            if (tacticalActorBase.BaseDef.name == "Civilian_ActorDef")
                            {
                                tacticalActorBase.SetFaction(phoenix, TacMissionParticipant.Player);
                                
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

        public static void SaveOriginalData()     
        {
            try 
            {
                foreach (CustomMissionTypeDef missionTypeDef in Repo.GetAllDefs<CustomMissionTypeDef>())
                {

                    if (missionTypeDef.name.Contains("Haven") && !missionTypeDef.name.Contains("Infestation"))
                    {
                        TacCrateDataDef cratesNotResources = Repo.GetAllDefs<TacCrateDataDef>().FirstOrDefault(ged => ged.name.Equals("Default_TacCrateDataDef"));
                        if (missionTypeDef.name.Contains("Civ"))
                        {
                            missionTypeDef.ParticipantsRelations[1].MutualRelation = FactionRelation.Enemy;
                        }
                        else if (!missionTypeDef.name.Contains("Civ"))
                        {
                            missionTypeDef.ParticipantsRelations[2].MutualRelation = FactionRelation.Enemy;
                        }
                        missionTypeDef.ParticipantsData[1].PredeterminedFactionEffects = missionTypeDef.ParticipantsData[0].PredeterminedFactionEffects;
                        missionTypeDef.ParticipantsData[1].ReinforcementsTurns.Max = 2;
                        missionTypeDef.ParticipantsData[1].ReinforcementsTurns.Min = 2;
                        missionTypeDef.ParticipantsData[1].InfiniteReinforcements = true;
                        missionTypeDef.ParticipantsData[1].ReinforcementsDeploymentPart.Max = 0.5f;
                        missionTypeDef.ParticipantsData[1].ReinforcementsDeploymentPart.Min = 0.5f;
                        missionTypeDef.MissionSpecificCrates = cratesNotResources;
                        missionTypeDef.FactionItemsRange.Min = 2;
                        missionTypeDef.FactionItemsRange.Max = 7;
                        missionTypeDef.CratesDeploymentPointsRange.Min = 20;
                        missionTypeDef.CratesDeploymentPointsRange.Max = 30;
                        missionTypeDef.DontRecoverItems = true;

                    }
                }



            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

      /*  public static TacticalFaction TacticalFaction = null;

        [HarmonyPatch(typeof(TacParticipantSpawn), "GetMissionSpawnZones")]
        public static class TacParticipantSpawn_GetMissionSpawnZones_patch
        {
            public static bool Prefix (TacParticipantSpawn __instance, ref List<TacticalDeployZone> __result)
            {
                try
                {
                    if (__instance.MissionFactionData.FactionDef.MatchesShortName("PX")) 
                    { 
                        TacticalFaction = __instance.TacticalFaction;
                        TFTVLogger.Always("TacticalFaction is " + TacticalFaction.Faction.FactionDef.name);
                        return true;
                    }
                   
                    TFTVLogger.Always("GetMissionSpawnZones");
                    if (__instance.MissionFactionData.FactionDef.MatchesShortName("neut")) //&& __instance.TacticalFaction.DeployZones.Where((TacticalDeployZone z) => z.IsMissionZone).ToList() == null)
                    {
                        TFTVLogger.Always("Neut faction if triggered");
                        TacticalFaction tacticalFaction = TacticalFaction;
                        TFTVLogger.Always("Anu found " + tacticalFaction.Faction.FactionDef.name);
                        __result = tacticalFaction.DeployZones.ToList();
                        return false;
                    }
                    return true;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
                return true;
               // throw new InvalidOperationException();
            }
        }*/

        //need harmony patch this:


        public static void VO5ChangesToHD()
        {
            try
            {
                //need to check this:
              
                foreach (CustomMissionTypeDef missionTypeDef in Repo.GetAllDefs<CustomMissionTypeDef>())
                {

                    if (missionTypeDef.name.Contains("Haven") && !missionTypeDef.name.Contains("Infestation"))
                    {
                        TacCrateDataDef cratesNotResources = Repo.GetAllDefs<TacCrateDataDef>().FirstOrDefault(ged => ged.name.Equals("Default_TacCrateDataDef"));
                        if (missionTypeDef.name.Contains("Civ"))
                        {
                            missionTypeDef.ParticipantsRelations[1].MutualRelation = FactionRelation.Enemy;
                            TacMissionTypeParticipantData civilians = missionTypeDef.ParticipantsData[1];
                            missionTypeDef.ParticipantsData.Add(civilians);
                            missionTypeDef.ParticipantsData[2].FactionDef = Repo.GetAllDefs<PPFactionDef>().FirstOrDefault(ged => ged.name.Equals("Neutral_FactionDef")); //missionTypeDef.ParticipantsData[1].FactionDef;
                                //
                            missionTypeDef.ParticipantsData[2].ParticipantKind = TacMissionParticipant.Residents;
                            List <MutualParticipantsRelations> mutualParticipantRelatons = missionTypeDef.ParticipantsRelations.ToList();
                            MutualParticipantsRelations aliensVScivilians
                                = new MutualParticipantsRelations { FirstParticipant = TacMissionParticipant.Intruder, SecondParticipant = TacMissionParticipant.Residents, MutualRelation = FactionRelation.Enemy };
                            MutualParticipantsRelations playerAndcivilians =
                                new MutualParticipantsRelations { FirstParticipant = TacMissionParticipant.Player, SecondParticipant = TacMissionParticipant.Residents, MutualRelation = FactionRelation.Friend };
                            MutualParticipantsRelations soldiersAndcivilians =
                               new MutualParticipantsRelations { FirstParticipant = TacMissionParticipant.Residents, SecondParticipant = TacMissionParticipant.Residents, MutualRelation = FactionRelation.Friend };
                            mutualParticipantRelatons.Add(aliensVScivilians);
                            mutualParticipantRelatons.Add(playerAndcivilians);
                            mutualParticipantRelatons.Add(soldiersAndcivilians);
                            missionTypeDef.ParticipantsRelations = mutualParticipantRelatons.ToArray();
                        }
                        else if (!missionTypeDef.name.Contains("Civ"))
                        {
                            missionTypeDef.ParticipantsRelations[2].MutualRelation = FactionRelation.Enemy;
                        }
                        missionTypeDef.ParticipantsData[1].PredeterminedFactionEffects = missionTypeDef.ParticipantsData[0].PredeterminedFactionEffects;
                        missionTypeDef.ParticipantsData[1].ParticipantKind = TacMissionParticipant.Residents;
                        missionTypeDef.ParticipantsData[1].ReinforcementsTurns.Max = 2;
                        missionTypeDef.ParticipantsData[1].ReinforcementsTurns.Min = 2;
                        missionTypeDef.ParticipantsData[1].InfiniteReinforcements = true;
                        missionTypeDef.ParticipantsData[1].ReinforcementsDeploymentPart.Max = 0.5f;
                        missionTypeDef.ParticipantsData[1].ReinforcementsDeploymentPart.Min = 0.5f;
                        missionTypeDef.ParticipantsData[1].DeploymentRule.DeploymentPoints = 1000;
                        missionTypeDef.MissionSpecificCrates = cratesNotResources;
                        missionTypeDef.FactionItemsRange.Min = 2;
                        missionTypeDef.FactionItemsRange.Max = 7;
                        missionTypeDef.CratesDeploymentPointsRange.Min = 20;
                        missionTypeDef.CratesDeploymentPointsRange.Max = 30;
                        missionTypeDef.DontRecoverItems = true;

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
