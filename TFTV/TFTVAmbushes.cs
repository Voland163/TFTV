using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using System;
using System.Linq;

namespace TFTV
{
    internal class TFTVAmbushes
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        public static void Apply_Changes_Ambush_Missions()
        {
            try
            {
                //Changing ambush missions so that all of them have crates
                CustomMissionTypeDef AmbushALN = Repo.GetAllDefs<CustomMissionTypeDef>().FirstOrDefault(ged => ged.name.Equals("AmbushAlien_CustomMissionTypeDef"));
                CustomMissionTypeDef SourceScavCratesALN = Repo.GetAllDefs<CustomMissionTypeDef>().FirstOrDefault(ged => ged.name.Equals("ScavCratesALN_CustomMissionTypeDef"));
                var pickResourceCratesObjective = SourceScavCratesALN.CustomObjectives[2];
                AmbushALN.ParticipantsData[0].ReinforcementsTurns.Max = 2;
                AmbushALN.ParticipantsData[0].ReinforcementsTurns.Min = 2;
                AmbushALN.CratesDeploymentPointsRange = SourceScavCratesALN.CratesDeploymentPointsRange;
                AmbushALN.MissionSpecificCrates = SourceScavCratesALN.MissionSpecificCrates;
                AmbushALN.FactionItemsRange = SourceScavCratesALN.FactionItemsRange;
                AmbushALN.CratesDeploymentPointsRange.Min = 30;
                AmbushALN.CratesDeploymentPointsRange.Max = 50;
                AmbushALN.CustomObjectives[2] = pickResourceCratesObjective;

                CustomMissionTypeDef AmbushAN = Repo.GetAllDefs<CustomMissionTypeDef>().FirstOrDefault(ged => ged.name.Equals("AmbushAN_CustomMissionTypeDef"));
                AmbushAN.ParticipantsData[0].ReinforcementsTurns.Max = 2;
                AmbushAN.ParticipantsData[0].ReinforcementsTurns.Min = 2;
                AmbushAN.CratesDeploymentPointsRange = SourceScavCratesALN.CratesDeploymentPointsRange;
                AmbushAN.MissionSpecificCrates = SourceScavCratesALN.MissionSpecificCrates;
                AmbushAN.FactionItemsRange = SourceScavCratesALN.FactionItemsRange;
                AmbushAN.CratesDeploymentPointsRange.Min = 30;
                AmbushAN.CratesDeploymentPointsRange.Max = 50;
                AmbushAN.CustomObjectives[2] = pickResourceCratesObjective;

                CustomMissionTypeDef AmbushBandits = Repo.GetAllDefs<CustomMissionTypeDef>().FirstOrDefault(ged => ged.name.Equals("AmbushBandits_CustomMissionTypeDef"));
                AmbushBandits.ParticipantsData[0].ReinforcementsTurns.Max = 2;
                AmbushBandits.ParticipantsData[0].ReinforcementsTurns.Min = 2;
                AmbushBandits.CratesDeploymentPointsRange = SourceScavCratesALN.CratesDeploymentPointsRange;
                AmbushBandits.MissionSpecificCrates = SourceScavCratesALN.MissionSpecificCrates;
                AmbushBandits.FactionItemsRange = SourceScavCratesALN.FactionItemsRange;
                AmbushBandits.CratesDeploymentPointsRange.Min = 30;
                AmbushBandits.CratesDeploymentPointsRange.Max = 50;
                AmbushBandits.CustomObjectives[2] = pickResourceCratesObjective;

                CustomMissionTypeDef AmbushFallen = Repo.GetAllDefs<CustomMissionTypeDef>().FirstOrDefault(ged => ged.name.Equals("AmbushFallen_CustomMissionTypeDef"));
                AmbushFallen.ParticipantsData[0].ReinforcementsTurns.Max = 2;
                AmbushFallen.ParticipantsData[0].ReinforcementsTurns.Min = 2;
                AmbushFallen.CratesDeploymentPointsRange = SourceScavCratesALN.CratesDeploymentPointsRange;
                AmbushFallen.MissionSpecificCrates = SourceScavCratesALN.MissionSpecificCrates;
                AmbushFallen.FactionItemsRange = SourceScavCratesALN.FactionItemsRange;
                AmbushFallen.CratesDeploymentPointsRange.Min = 30;
                AmbushFallen.CratesDeploymentPointsRange.Max = 50;
                AmbushFallen.CustomObjectives[2] = pickResourceCratesObjective;

                CustomMissionTypeDef AmbushNJ = Repo.GetAllDefs<CustomMissionTypeDef>().FirstOrDefault(ged => ged.name.Equals("AmbushNJ_CustomMissionTypeDef"));
                AmbushNJ.ParticipantsData[0].ReinforcementsTurns.Max = 2;
                AmbushNJ.ParticipantsData[0].ReinforcementsTurns.Min = 2;
                AmbushNJ.CratesDeploymentPointsRange = SourceScavCratesALN.CratesDeploymentPointsRange;
                AmbushNJ.MissionSpecificCrates = SourceScavCratesALN.MissionSpecificCrates;
                AmbushNJ.FactionItemsRange = SourceScavCratesALN.FactionItemsRange;
                AmbushNJ.CratesDeploymentPointsRange.Min = 30;
                AmbushNJ.CratesDeploymentPointsRange.Max = 50;
                AmbushNJ.CustomObjectives[2] = pickResourceCratesObjective;

                CustomMissionTypeDef AmbushPure = Repo.GetAllDefs<CustomMissionTypeDef>().FirstOrDefault(ged => ged.name.Equals("AmbushPure_CustomMissionTypeDef"));
                AmbushPure.ParticipantsData[0].ReinforcementsTurns.Max = 2;
                AmbushPure.ParticipantsData[0].ReinforcementsTurns.Min = 2;
                AmbushPure.CratesDeploymentPointsRange = SourceScavCratesALN.CratesDeploymentPointsRange;
                AmbushPure.MissionSpecificCrates = SourceScavCratesALN.MissionSpecificCrates;
                AmbushPure.FactionItemsRange = SourceScavCratesALN.FactionItemsRange;
                AmbushPure.CratesDeploymentPointsRange.Min = 30;
                AmbushPure.CratesDeploymentPointsRange.Max = 50;
                AmbushPure.CustomObjectives[2] = pickResourceCratesObjective;

                CustomMissionTypeDef AmbushSY = Repo.GetAllDefs<CustomMissionTypeDef>().FirstOrDefault(ged => ged.name.Equals("AmbushSY_CustomMissionTypeDef"));
                AmbushSY.ParticipantsData[0].ReinforcementsTurns.Max = 2;
                AmbushSY.ParticipantsData[0].ReinforcementsTurns.Min = 2;
                AmbushSY.CratesDeploymentPointsRange = SourceScavCratesALN.CratesDeploymentPointsRange;
                AmbushSY.MissionSpecificCrates = SourceScavCratesALN.MissionSpecificCrates;
                AmbushSY.FactionItemsRange = SourceScavCratesALN.FactionItemsRange;
                AmbushSY.CratesDeploymentPointsRange.Min = 30;
                AmbushSY.CratesDeploymentPointsRange.Max = 50;
                AmbushSY.CustomObjectives[2] = pickResourceCratesObjective;

                //Reduce XP for Ambush mission
                SurviveTurnsFactionObjectiveDef surviveAmbush_CustomMissionObjective = Repo.GetAllDefs<SurviveTurnsFactionObjectiveDef>().FirstOrDefault(ged => ged.name.Equals("SurviveAmbush_CustomMissionObjective"));
                surviveAmbush_CustomMissionObjective.MissionObjectiveData.ExperienceReward = 100;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
        public static void Apply_Changes_Ambush_Chances(GeoscapeEventSystem eventSystem)
        {
            try
            {
                TFTVLogger.Always("Ambush chances modified");
                eventSystem.ExplorationAmbushChance = 70;
                eventSystem.AmbushExploredSitesProtection = 0;
                eventSystem.StartingAmbushProtection = 0;
           
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        [HarmonyPatch(typeof(GeoscapeEventSystem), "OnLevelStart")]

        public static class GeoscapeEventSystem_PhoenixFaction_OnLevelStart_Patch
        {
            public static void Prefix(GeoscapeEventSystem __instance)
            {

                try
                {                    
                    __instance.AmbushExploredSitesProtection = 0;
                    __instance.StartingAmbushProtection = 0;
                    if (TFTVVoidOmens.VoidOmen1Active)
                    {
                        __instance.ExplorationAmbushChance = 100;

                    }
                    else
                    {
                        __instance.ExplorationAmbushChance = 70;
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

