using Base.Core;
using Base.Defs;
using Base.Entities.Effects;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Geoscape.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Missions.Outcomes;
using PhoenixPoint.Geoscape.Entities.PhoenixBases.FacilityComponents;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.Levels.Objectives;
using PhoenixPoint.Geoscape.View.ViewStates;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.FactionEffects;
using PhoenixPoint.Tactical.Levels.Mist;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV
{
    internal class TFTVVoidOmens
    {
        public static readonly TFTVConfig Config = new TFTVConfig();
        private static readonly DefRepository Repo = TFTVMain.Repo;

        private static readonly FesteringSkiesSettingsDef festeringSkiesSettingsDef = Repo.GetAllDefs<FesteringSkiesSettingsDef>().FirstOrDefault(ged => ged.name.Equals("FesteringSkiesSettingsDef"));
        private static readonly TacticalPerceptionDef tacticalPerceptionDef = Repo.GetAllDefs<TacticalPerceptionDef>().FirstOrDefault((TacticalPerceptionDef a) => a.name.Equals("Soldier_PerceptionDef"));
        private static readonly CustomMissionTypeDef AmbushALN = Repo.GetAllDefs<CustomMissionTypeDef>().FirstOrDefault(ged => ged.name.Equals("AmbushAlien_CustomMissionTypeDef"));
        private static readonly TacCrateDataDef cratesNotResources = Repo.GetAllDefs<TacCrateDataDef>().FirstOrDefault(ged => ged.name.Equals("Default_TacCrateDataDef"));
        private static readonly TacticalFactionEffectDef defendersCanBeRecruited = Repo.GetAllDefs<TacticalFactionEffectDef>().FirstOrDefault(ged => ged.name.Equals("CanBeRecruitedByPhoenix_FactionEffectDef"));
        private static readonly GeoHavenZoneDef havenLab = Repo.GetAllDefs<GeoHavenZoneDef>().FirstOrDefault(ged => ged.name.Equals("Research_GeoHavenZoneDef"));
        private static readonly GeoFactionDef phoenixPoint = Repo.GetAllDefs<GeoFactionDef>().FirstOrDefault(ged => ged.name.Equals("Phoenix_GeoPhoenixFactionDef"));

        public static bool[] voidOmensCheck = new bool[18];
        //VO#1 is harder ambushes
        public static bool VoidOmen1Active = false;
        //VO#3 is WP cost +50%
        public static bool VoidOmen3Active = false;
        public static bool VoidOmen4Active = false;
        //VO#5 is haven defenders hostile; this is needed for victory kludge
        public static bool VoidOmen5Active = false;
        //VO#7 is more mist in missions
        public static bool VoidOmen7Active = false;
        //VO#10 is no limit to Delirium
        public static bool VoidOmen10Active = false;
        //VO#12 is +50% strength of alien attacks on Havens
        public static bool VoidOmen12Active = false;
        //VO#15 is more Umbra
        public static bool VoidOmen15Active = false;
        //VO#16 is Umbras can appear anywhere and attack anyone
        public static bool VoidOmen16Active = false;

        public static void ImplementVoidOmens(GeoLevelController level)
        {
            try
            {
                string voidOmen = "VoidOmen_";
                // TFTVLogger.Always("Checking if method invocation is working, these are the Void Omens in play " + voidOmensInPlay[0] + " "
                //   + voidOmensInPlay[1] + " " + voidOmensInPlay[2] + " " + voidOmensInPlay[3]);
                


                for (int i = 1; i < voidOmensCheck.Count() - 1; i++)
                {
                    if (i == 1 && CheckFordVoidOmensInPlay(level).Contains(i) && !voidOmensCheck[i])
                    {

                        VoidOmen1Active = true;

                        AmbushALN.ParticipantsData[0].ReinforcementsTurns.Max = 1;
                        AmbushALN.ParticipantsData[0].ReinforcementsTurns.Min = 1;
                        AmbushALN.CratesDeploymentPointsRange.Min = 50;
                        AmbushALN.CratesDeploymentPointsRange.Max = 70;
                        // Logger.Always(voidOmen + j + " is now in effect, held in variable "+voidOmen + i);
                        voidOmensCheck[i] = true;
                    }
                    else if (i == 1 && !CheckFordVoidOmensInPlay(level).Contains(1) && voidOmensCheck[1])
                    {
                        VoidOmen1Active = false;
                        AmbushALN.ParticipantsData[0].ReinforcementsTurns.Max = 2;
                        AmbushALN.ParticipantsData[0].ReinforcementsTurns.Min = 2;
                        AmbushALN.CratesDeploymentPointsRange.Min = 30;
                        AmbushALN.CratesDeploymentPointsRange.Max = 50;
                        voidOmensCheck[1] = false;
                        //   Logger.Always("Exploration ambush chance is now " + level.EventSystem.ExplorationAmbushChance);
                        //  Logger.Always("Alien ambushes can now have a max of  " + AmbushALN.CratesDeploymentPointsRange.Max / 10 + " crates");
                        TFTVLogger.Always("The check for VO#1 went ok");
                    }
                    if (i == 2 && CheckFordVoidOmensInPlay(level).Contains(i) && !voidOmensCheck[i])
                    {
                        foreach (GeoscapeEventDef geoEvent in Repo.GetAllDefs<GeoscapeEventDef>())
                        {
                            foreach (GeoEventChoice choice in geoEvent.GeoscapeEventData.Choices)
                            {
                                for (int t = 0; t < choice.Outcome.Diplomacy.Count; t++)
                                {
                                    if (choice.Outcome.Diplomacy[t].Value != 0)
                                    {
                                        OutcomeDiplomacyChange diplomacyChange = choice.Outcome.Diplomacy[t];
                                        //TFTVLogger.Always("Original value was " + diplomacyChange.Value);
                                        diplomacyChange.Value = Mathf.CeilToInt(diplomacyChange.Value * 0.5f);
                                        choice.Outcome.Diplomacy[t] = diplomacyChange;
                                        //TFTVLogger.Always("New value is " + diplomacyChange.Value);
                                    }
                                }
                            }
                        }
                        foreach (DiplomacyMissionOutcomeDef diplomacyMissionOutcomeDef in Repo.GetAllDefs<DiplomacyMissionOutcomeDef>())
                        {
                            diplomacyMissionOutcomeDef.DiplomacyToFaction.Max = Mathf.RoundToInt(diplomacyMissionOutcomeDef.DiplomacyToFaction.Max * 0.5f);
                            diplomacyMissionOutcomeDef.DiplomacyToFaction.Min = Mathf.RoundToInt(diplomacyMissionOutcomeDef.DiplomacyToFaction.Min * 0.5f);
                        }
                        // Logger.Always(voidOmen + j + " is now in effect, held in variable " + voidOmen + i);
                        voidOmensCheck[i] = true;
                    }
                    else if (i == 2 && !CheckFordVoidOmensInPlay(level).Contains(i) && voidOmensCheck[i])
                    {
                        foreach (GeoscapeEventDef geoEvent in Repo.GetAllDefs<GeoscapeEventDef>())
                        {
                            foreach (GeoEventChoice choice in geoEvent.GeoscapeEventData.Choices)
                            {
                                for (int t = 0; t < choice.Outcome.Diplomacy.Count; t++)
                                {
                                    if (choice.Outcome.Diplomacy[t].Value != 0)
                                    {
                                        OutcomeDiplomacyChange diplomacyChange = choice.Outcome.Diplomacy[t];
                                        diplomacyChange.Value = Mathf.CeilToInt(diplomacyChange.Value * 2f);
                                        choice.Outcome.Diplomacy[t] = diplomacyChange;
                                    }
                                }
                            }
                        }
                        foreach (DiplomacyMissionOutcomeDef diplomacyMissionOutcomeDef in Repo.GetAllDefs<DiplomacyMissionOutcomeDef>())
                        {
                            diplomacyMissionOutcomeDef.DiplomacyToFaction.Max = Mathf.RoundToInt(diplomacyMissionOutcomeDef.DiplomacyToFaction.Max * 2f);
                            diplomacyMissionOutcomeDef.DiplomacyToFaction.Min = Mathf.RoundToInt(diplomacyMissionOutcomeDef.DiplomacyToFaction.Min * 2f);
                        }
                        voidOmensCheck[2] = false;
                        TFTVLogger.Always("The check for VO#2 went ok");
                    }
                    if (i == 3 && CheckFordVoidOmensInPlay(level).Contains(i) && !voidOmensCheck[i])
                    {
                        // Logger.Always(voidOmen + j + " is now in effect, held in variable " + voidOmen + i);
                        voidOmensCheck[i] = true;
                    }
                    else if (i == 3 && !CheckFordVoidOmensInPlay(level).Contains(i) && voidOmensCheck[i])
                    {
                        voidOmensCheck[i] = false;
                        TFTVLogger.Always("The check for VO#3 went ok");
                    }
                    if (i == 4 && CheckFordVoidOmensInPlay(level).Contains(i) && !voidOmensCheck[i])
                    {
                        VoidOmen4Active = true;
                        voidOmensCheck[i] = true;
                    }
                    else if (i == 4 && !CheckFordVoidOmensInPlay(level).Contains(i) && voidOmensCheck[i])
                    {
                        VoidOmen4Active = false;
                        voidOmensCheck[4] = false;
                        TFTVLogger.Always("The check for VO#4 went ok");

                    }
                    if (i == 5 && CheckFordVoidOmensInPlay(level).Contains(i) && !voidOmensCheck[i])
                    {
                        // TFTVHavenDefense.VO5ChangesToHD();
                        foreach (CustomMissionTypeDef missionTypeDef in Repo.GetAllDefs<CustomMissionTypeDef>())
                        {

                            if (missionTypeDef.name.Contains("Haven") && !missionTypeDef.name.Contains("Infestation"))
                            {
                                //    List<FactionObjectiveDef> objectiveDefs = missionTypeDef.CustomObjectives.ToList();


                                if (missionTypeDef.name.Contains("Civ"))
                                {
                                    missionTypeDef.ParticipantsRelations[1].MutualRelation = FactionRelation.Enemy;
                                    // objectiveDefs.Remove(protectCivilians);
                                }
                                else if (!missionTypeDef.name.Contains("Civ"))
                                {
                                    missionTypeDef.ParticipantsRelations[2].MutualRelation = FactionRelation.Enemy;
                                }
                                missionTypeDef.ParticipantsData[1].PredeterminedFactionEffects = missionTypeDef.ParticipantsData[0].PredeterminedFactionEffects;
                                missionTypeDef.MissionSpecificCrates = cratesNotResources;
                                missionTypeDef.FactionItemsRange.Min = 2;
                                missionTypeDef.FactionItemsRange.Max = 7;
                                missionTypeDef.CratesDeploymentPointsRange.Min = 20;
                                missionTypeDef.CratesDeploymentPointsRange.Max = 30;
                                missionTypeDef.DontRecoverItems = true;
                                //  objectiveDefs.Remove(killAllObjective);                
                                //  missionTypeDef.CustomObjectives = objectiveDefs.ToArray();
                            }
                        }
                        // Logger.Always(voidOmen + j + " is now in effect, held in variable " + voidOmen + i);
                        voidOmensCheck[i] = true;
                    }
                    else if (i == 5 && !CheckFordVoidOmensInPlay(level).Contains(i) && voidOmensCheck[i])
                    {
                        foreach (CustomMissionTypeDef missionTypeDef in Repo.GetAllDefs<CustomMissionTypeDef>())
                        {
                            if (missionTypeDef.name.Contains("Haven") && !missionTypeDef.name.Contains("Infestation"))
                            {


                                if (missionTypeDef.name.Contains("Civ"))
                                {
                                    missionTypeDef.ParticipantsRelations[1].MutualRelation = FactionRelation.Friend;
                                }
                                else if (!missionTypeDef.name.Contains("Civ"))
                                {
                                    missionTypeDef.ParticipantsRelations[2].MutualRelation = FactionRelation.Friend;
                                }
                                EffectDef[] predeterminedFactionEffects = new EffectDef[1] { defendersCanBeRecruited };
                                missionTypeDef.ParticipantsData[1].PredeterminedFactionEffects = predeterminedFactionEffects;
                                missionTypeDef.FactionItemsRange.Min = 0;
                                missionTypeDef.FactionItemsRange.Max = 0;
                                missionTypeDef.CratesDeploymentPointsRange.Min = 0;
                                missionTypeDef.CratesDeploymentPointsRange.Max = 0;
                                missionTypeDef.DontRecoverItems = false;
                                missionTypeDef.ParticipantsData[1].ReinforcementsTurns.Max = 0;
                                missionTypeDef.ParticipantsData[1].ReinforcementsTurns.Min = 0;
                                missionTypeDef.ParticipantsData[1].InfiniteReinforcements = false;
                                missionTypeDef.ParticipantsData[1].ReinforcementsDeploymentPart.Max = 0;
                                missionTypeDef.ParticipantsData[1].ReinforcementsDeploymentPart.Min = 0;
                            }
                        }
                        voidOmensCheck[5] = false;
                        TFTVLogger.Always("The check for VO#5 went ok");

                    }
                    if (i == 6 && CheckFordVoidOmensInPlay(level).Contains(i) && !voidOmensCheck[i])
                    {
                        level.CurrentDifficultyLevel.EvolutionPointsGainOnMissionLoss = 20;
                        level.CurrentDifficultyLevel.AlienBaseTypeEvolutionParams[0].EvolutionPerDestroyedBase = 30;
                        level.CurrentDifficultyLevel.AlienBaseTypeEvolutionParams[1].EvolutionPerDestroyedBase = 60;
                        level.CurrentDifficultyLevel.AlienBaseTypeEvolutionParams[2].EvolutionPerDestroyedBase = 90;
                        foreach (ResourceGeneratorFacilityComponentDef lab in Repo.GetAllDefs<ResourceGeneratorFacilityComponentDef>())
                        {
                            if (lab.name == "E_ResourceGenerator [ResearchLab_PhoenixFacilityDef]" || lab.name == "E_ResourceGenerator [BionicsLab_PhoenixFacilityDef]")
                                lab.BaseResourcesOutput.Values.Add(new ResourceUnit { Type = ResourceType.Research, Value = 2 });
                        }
                        //  Logger.Always(voidOmen + j + " is now in effect, held in variable " + voidOmen + i);
                        voidOmensCheck[i] = true;
                    }
                    else if (i == 6 && !CheckFordVoidOmensInPlay(level).Contains(i) && voidOmensCheck[i])
                    {
                        level.CurrentDifficultyLevel.EvolutionPointsGainOnMissionLoss = 0;
                        level.CurrentDifficultyLevel.AlienBaseTypeEvolutionParams[0].EvolutionPerDestroyedBase = 0;
                        level.CurrentDifficultyLevel.AlienBaseTypeEvolutionParams[1].EvolutionPerDestroyedBase = 0;
                        level.CurrentDifficultyLevel.AlienBaseTypeEvolutionParams[2].EvolutionPerDestroyedBase = 0;
                        foreach (ResourceGeneratorFacilityComponentDef lab in Repo.GetAllDefs<ResourceGeneratorFacilityComponentDef>())
                        {
                            if (lab.name == "E_ResourceGenerator [ResearchLab_PhoenixFacilityDef]"
                                || lab.name == "E_ResourceGenerator [BionicsLab_PhoenixFacilityDef]"
                                && lab.BaseResourcesOutput.Values[1] != null)
                            {
                                lab.BaseResourcesOutput.Values.Remove(lab.BaseResourcesOutput.Values[1]);
                            }
                        }
                        voidOmensCheck[6] = false;
                        TFTVLogger.Always("The check for VO#6 went ok");

                    }
                    if (i == 7 && CheckFordVoidOmensInPlay(level).Contains(i) && !voidOmensCheck[i])
                    {
                        //  Logger.Always(voidOmen + j + " is now in effect, held in variable " + voidOmen + i);
                        voidOmensCheck[i] = true;
                    }
                    else if (i == 7 && !CheckFordVoidOmensInPlay(level).Contains(i) && voidOmensCheck[i])
                    {
                        voidOmensCheck[7] = false;
                        TFTVLogger.Always("The check for VO#7 went ok");

                    }
                    if (i == 8 && CheckFordVoidOmensInPlay(level).Contains(i) && !voidOmensCheck[i])
                    {

                        foreach (GeoscapeEventDef geoEvent in Repo.GetAllDefs<GeoscapeEventDef>())
                        {
                            foreach (GeoEventChoice choice in geoEvent.GeoscapeEventData.Choices)
                            {
                                for (int t = 0; t < choice.Outcome.Diplomacy.Count; t++)
                                {
                                    if (choice.Outcome.Diplomacy[t].Value <= 0 && choice.Outcome.Diplomacy[t].TargetFaction != phoenixPoint)
                                    {
                                        OutcomeDiplomacyChange diplomacyChange = choice.Outcome.Diplomacy[t];
                                        diplomacyChange.Value = Mathf.RoundToInt(diplomacyChange.Value * 1.5f);
                                        choice.Outcome.Diplomacy[t] = diplomacyChange;
                                    }
                                }
                            }
                        }

                        havenLab.ProvidesResearch = 2;
                        // Logger.Always(voidOmen + j + " is now in effect, held in variable " + voidOmen + i);
                        voidOmensCheck[i] = true;
                    }
                    else if (i == 8 && !CheckFordVoidOmensInPlay(level).Contains(i) && voidOmensCheck[i])
                    {

                        foreach (GeoscapeEventDef geoEvent in Repo.GetAllDefs<GeoscapeEventDef>())
                        {
                            foreach (GeoEventChoice choice in geoEvent.GeoscapeEventData.Choices)
                            {
                                for (int t = 0; t < choice.Outcome.Diplomacy.Count; t++)
                                {
                                    if (choice.Outcome.Diplomacy[t].Value <= 0 && choice.Outcome.Diplomacy[t].TargetFaction != phoenixPoint)
                                    {
                                        OutcomeDiplomacyChange diplomacyChange = choice.Outcome.Diplomacy[t];
                                        diplomacyChange.Value = Mathf.RoundToInt(diplomacyChange.Value * 2f);
                                        choice.Outcome.Diplomacy[t] = diplomacyChange;
                                    }
                                }
                            }
                        }

                        havenLab.ProvidesResearch = 1;
                        TFTVLogger.Always("The check for VO#8 went ok");
                        voidOmensCheck[8] = false;

                    }
                    if (i == 9 && CheckFordVoidOmensInPlay(level).Contains(i) && !voidOmensCheck[i])
                    {

                        festeringSkiesSettingsDef.HavenAttackCounterModifier = 0.66f;
                        //  Logger.Always(voidOmen + j + " is now in effect, held in variable " + voidOmen + i);
                        voidOmensCheck[i] = true;
                    }
                    else if (i == 9 && !CheckFordVoidOmensInPlay(level).Contains(i) && voidOmensCheck[i])
                    {

                        festeringSkiesSettingsDef.HavenAttackCounterModifier = 1.33f;
                        voidOmensCheck[9] = false;
                        TFTVLogger.Always("The check for VO#9 went ok");

                    }
                    if (i == 10 && CheckFordVoidOmensInPlay(level).Contains(i) && !voidOmensCheck[i])
                    {
                        // Logger.Always(voidOmen + j + " is now in effect, held in variable " + voidOmen + i);
                        VoidOmen10Active = true;
                        voidOmensCheck[i] = true;
                    }
                    else if (i == 10 && !CheckFordVoidOmensInPlay(level).Contains(i) && voidOmensCheck[i])
                    {
                        voidOmensCheck[10] = false;
                        VoidOmen10Active = false;
                        TFTVLogger.Always("The check for VO#10 went ok");

                    }
                    if (i == 11 && CheckFordVoidOmensInPlay(level).Contains(i) && !voidOmensCheck[i])
                    {
                        voidOmensCheck[i] = true;
                    }
                    else if (i == 11 && !CheckFordVoidOmensInPlay(level).Contains(i) && voidOmensCheck[i])
                    {
                        /*   FesteringSkiesSettingsDef festeringSkiesSettingsDef = Repo.GetAllDefs<FesteringSkiesSettingsDef>().FirstOrDefault(ged => ged.name.Equals("FesteringSkiesSettingsDef"));
                           festeringSkiesSettingsDef.DisruptionThreshholdBaseValue = 3;*/
                        voidOmensCheck[11] = false;
                        TFTVLogger.Always("The check for VO#11 went ok");

                    }
                    if (i == 12 && CheckFordVoidOmensInPlay(level).Contains(i) && !voidOmensCheck[i])
                    {
                        VoidOmen12Active = true;
                        // Logger.Always(voidOmen + j + " is now in effect, held in variable " + voidOmen + i);
                        voidOmensCheck[i] = true;
                    }
                    else if (i == 12 && !CheckFordVoidOmensInPlay(level).Contains(i) && voidOmensCheck[i])
                    {
                        VoidOmen12Active = false;
                        voidOmensCheck[12] = false;
                        TFTVLogger.Always("The check for VO#12 went ok");

                    }
                    if (i == 13 && CheckFordVoidOmensInPlay(level).Contains(i) && !voidOmensCheck[i])
                    {
                        level.CurrentDifficultyLevel.NestLimitations.HoursBuildTime /= 2;
                        level.CurrentDifficultyLevel.LairLimitations.HoursBuildTime /= 2;
                        level.CurrentDifficultyLevel.CitadelLimitations.HoursBuildTime /= 2;
                        TFTVLogger.Always(voidOmen + i + " is now in effect, held in variable " + voidOmen + i + ", so Pandoran nests take " + level.CurrentDifficultyLevel.NestLimitations.HoursBuildTime + " hours");
                        voidOmensCheck[i] = true;
                    }
                    else if (i == 13 && !CheckFordVoidOmensInPlay(level).Contains(i) && voidOmensCheck[i])
                    {
                        level.CurrentDifficultyLevel.NestLimitations.HoursBuildTime *= 2;
                        level.CurrentDifficultyLevel.LairLimitations.HoursBuildTime *= 2;
                        level.CurrentDifficultyLevel.CitadelLimitations.HoursBuildTime *= 2;

                        voidOmensCheck[13] = false;

                        TFTVLogger.Always("The check for VO#13 went ok" + " so Pandoran nests take " + level.CurrentDifficultyLevel.NestLimitations.HoursBuildTime + " hours");

                    }
                    if (i == 14 && CheckFordVoidOmensInPlay(level).Contains(i) && !voidOmensCheck[i])
                    {

                        tacticalPerceptionDef.PerceptionRange = 20;
                        // Logger.Always(voidOmen + j + " is now in effect, held in variable " + voidOmen + i);
                        voidOmensCheck[i] = true;
                    }
                    else if (i == 14 && !CheckFordVoidOmensInPlay(level).Contains(i) && voidOmensCheck[i])
                    {

                        tacticalPerceptionDef.PerceptionRange = 30;
                        voidOmensCheck[14] = false;
                        TFTVLogger.Always("The check for VO#14 went ok");

                    }
                    if (i == 15 && CheckFordVoidOmensInPlay(level).Contains(i) && !voidOmensCheck[i])
                    {
                        //  Logger.Always(voidOmen + j + " is now in effect, held in variable " + voidOmen + i);
                        voidOmensCheck[i] = true;
                    }
                    else if (i == 15 && !CheckFordVoidOmensInPlay(level).Contains(i) && voidOmensCheck[i])
                    {
                        voidOmensCheck[15] = false;
                        TFTVLogger.Always("The check for VO#15 went ok");

                    }
                    if (i == 16 && CheckFordVoidOmensInPlay(level).Contains(i) && !voidOmensCheck[i])
                    {
                        if (!voidOmensCheck[15])
                        {
                            TFTVUmbra.SetUmbraRandomValue(0.16f);
                        }
                        if (voidOmensCheck[15])
                        {
                            TFTVUmbra.SetUmbraRandomValue(0.32f);
                        }
                        //  Logger.Always(voidOmen + j + " is now in effect, held in variable " + voidOmen + i);
                        voidOmensCheck[i] = true;
                    }
                    else if (i == 16 && !CheckFordVoidOmensInPlay(level).Contains(i) && voidOmensCheck[i])
                    {
                        TFTVUmbra.SetUmbraRandomValue(0);

                        voidOmensCheck[16] = false;
                        TFTVLogger.Always("The check for VO#16 went ok");

                    }
                    if (i == 17 && CheckFordVoidOmensInPlay(level).Contains(i) && !voidOmensCheck[i])
                    {
                        voidOmensCheck[i] = true;
                    }
                    else if (i == 17 && !CheckFordVoidOmensInPlay(level).Contains(i) && voidOmensCheck[i])
                    {
                        voidOmensCheck[17] = false;
                        TFTVLogger.Always("The check for VO#17 went ok");

                    }
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CheckForVoidOmensRequiringTacticalPatching(GeoLevelController level)
        {
            try
            {

                VoidOmen3Active = false;
                VoidOmen5Active = false;
                VoidOmen7Active = false;
                VoidOmen10Active = false;
                VoidOmen15Active = false;
                VoidOmen16Active = false;

                int[] rolledVoidOmens = CheckFordVoidOmensInPlay(level);

                if (rolledVoidOmens.Contains(3))
                {
                    VoidOmen3Active = true;
                }

                if (rolledVoidOmens.Contains(5))
                {
                    VoidOmen5Active = true;
                }

                if (rolledVoidOmens.Contains(7))
                {
                    VoidOmen7Active = true;
                }
                if (rolledVoidOmens.Contains(10))
                {
                    VoidOmen10Active = true;
                }
                if (rolledVoidOmens.Contains(15))
                {
                    VoidOmen15Active = true;
                }
                if (rolledVoidOmens.Contains(16))
                {
                    VoidOmen16Active = true;
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static List<int> CheckForAlreadyRolledVoidOmens(GeoLevelController geoLevelController)
        {
            try
            {
                List<int> allVoidOmensAlreadyRolled = new List<int>();
                string triggeredVoidOmens = "TriggeredVoidOmen_";

                for (int x = 1; x < 20; x++)
                {
                    if (geoLevelController.EventSystem.GetVariable(triggeredVoidOmens + x) != 0)
                    {
                        allVoidOmensAlreadyRolled.Add(geoLevelController.EventSystem.GetVariable(triggeredVoidOmens + x));
                    }
                }
                return allVoidOmensAlreadyRolled;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

            throw new InvalidOperationException();
        }


        public static int[] CheckFordVoidOmensInPlay(GeoLevelController geoLevelController)
        {
            try
            {
                int difficulty = geoLevelController.CurrentDifficultyLevel.Order;
                string voidOmen = "VoidOmen_";

                // An array to record which variables hold which Void Omens
                int[] voidOmensInPlay = new int[difficulty];


                // We will look through the Void Omen variables in the order in which they were filled
                for (int y = 0; y < difficulty; y++)
                {
                    // And record which variable holds which Void Omen, by checking if it's empty or not
                    if (geoLevelController.EventSystem.GetVariable(voidOmen + (difficulty - y)) != 0)
                    {
                        voidOmensInPlay[difficulty - y - 1] = geoLevelController.EventSystem.GetVariable(voidOmen + (difficulty - y));
                    }
                    // We also have to record which variables are empty
                    else
                    {
                        voidOmensInPlay[difficulty - y - 1] = 0;
                    }
                }

                return voidOmensInPlay;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }

        public static void RemoveEarliestVoidOmen(GeoLevelController geoLevelController)
        {
            try
            {
                int difficulty = geoLevelController.CurrentDifficultyLevel.Order;
                string triggeredVoidOmens = "TriggeredVoidOmen_";
                string voidOmen = "VoidOmen_";
                string voidOmenTitle = "VOID_OMEN_TITLE_";

                // And an array to record which variables hold which Dark Events
                int[] voidOmensinPlay = CheckFordVoidOmensInPlay(geoLevelController);
                //    TFTVLogger.Always("Checking if method invocation is working, these are all the Void Omens in play " + voidOmensinPlay[0] + " "
                //       + voidOmensinPlay[1] + " " + voidOmensinPlay[2] + " " + voidOmensinPlay[3]);

                int replacedVoidOmen = 0;

                for (int x = 1; x < 20; x++)
                {
                    // We check, starting from the earliest, which Void Omen is still in play
                    if (voidOmensinPlay.Contains(geoLevelController.EventSystem.GetVariable(triggeredVoidOmens + x)))
                    {
                        // Then we locate in which Variable it is recorded
                        for (int y = 0; y < difficulty; y++)
                        {
                            // Once we find it, we want to remove it
                            // Added the check to skip empty Void Omen variables, to hopefully make this method work even when list is not full
                            if (voidOmensinPlay[difficulty - y - 1] == geoLevelController.EventSystem.GetVariable(triggeredVoidOmens + x) && voidOmensinPlay[difficulty - y - 1] != 0)
                            {
                                TFTVLogger.Always("The Void Omen Variable to be replaced is " + voidOmen + (difficulty - y) +
                                   " now holds VO " + geoLevelController.EventSystem.GetVariable(voidOmen + (difficulty - y)));
                                replacedVoidOmen = geoLevelController.EventSystem.GetVariable(voidOmen + (difficulty - y));
                                geoLevelController.EventSystem.SetVariable(voidOmen + (difficulty - y), 0);
                                TFTVLogger.Always("The Void Omen Variable " + voidOmen + (difficulty - y) +
                                    " is now " + geoLevelController.EventSystem.GetVariable(voidOmen + (difficulty - y)));
                                y = difficulty;
                                x = 20;
                            }
                        }
                    }
                }

                if (replacedVoidOmen != 0)
                {
                    TFTVLogger.Always("The target event that will be replaced is " + voidOmenTitle + replacedVoidOmen);
                    RemoveVoidOmenObjective(voidOmenTitle + replacedVoidOmen, geoLevelController);
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void RemoveAllVoidOmens(GeoLevelController geoLevelController)
        {
            try
            {
                int difficulty = geoLevelController.CurrentDifficultyLevel.Order;
                string triggeredVoidOmens = "TriggeredVoidOmen_";
                string voidOmen = "VoidOmen_";
                string voidOmenTitle = "VOID_OMEN_TITLE_";


                // And an array to record which variables hold which Dark Events
                int[] voidOmensinPlay = CheckFordVoidOmensInPlay(geoLevelController);
                //   TFTVLogger.Always("Checking if method invocation is working, these are all the Void Omens in play " + voidOmensinPlay[0] + " "
                //       + voidOmensinPlay[1] + " " + voidOmensinPlay[2] + " " + voidOmensinPlay[3]);

                int replacedVoidOmen = 0;

                for (int x = 1; x < 20; x++)
                {
                    // We check, starting from the earliest, which Void Omen is still in play
                    if (voidOmensinPlay.Contains(geoLevelController.EventSystem.GetVariable(triggeredVoidOmens + x)))
                    {
                        // Then we locate in which Variable it is recorded
                        for (int y = 0; y < difficulty; y++)
                        {
                            // Once we find it, we want to remove it
                            if (voidOmensinPlay[difficulty - y - 1] == geoLevelController.EventSystem.GetVariable(triggeredVoidOmens + x))
                            {
                                TFTVLogger.Always("The Void Omen Variable to be replaced is " + voidOmen + (difficulty - y) +
                                   " now holds VO " + geoLevelController.EventSystem.GetVariable(voidOmen + (difficulty - y)));
                                replacedVoidOmen = geoLevelController.EventSystem.GetVariable(voidOmen + (difficulty - y));
                                geoLevelController.EventSystem.SetVariable(voidOmen + (difficulty - y), 0);
                                TFTVLogger.Always("The Void Omen Variable " + voidOmen + (difficulty - y) +
                                    " is now " + geoLevelController.EventSystem.GetVariable(voidOmen + (difficulty - y)));
                            }
                        }
                    }
                    if (replacedVoidOmen != 0)
                    {
                        TFTVLogger.Always("The target event that will be replaced is " + voidOmenTitle + replacedVoidOmen);
                        RemoveVoidOmenObjective(voidOmenTitle + replacedVoidOmen, geoLevelController);
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void CreateVoidOmenObjective(string title, string description, GeoLevelController level)
        {
            try
            {
                DiplomaticGeoFactionObjective voidOmenObjective = new DiplomaticGeoFactionObjective(level.PhoenixFaction, level.PhoenixFaction)
                {
                    Title = new LocalizedTextBind(title),
                    Description = new LocalizedTextBind(description),
                };
                level.PhoenixFaction.AddObjective(voidOmenObjective);


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void RemoveVoidOmenObjective(string title, GeoLevelController level)
        {
            try
            {

                List<GeoFactionObjective> listOfObjectives = level.PhoenixFaction.Objectives.ToList();

                foreach (GeoFactionObjective objective1 in listOfObjectives)
                {
                    if (objective1.Title == null)
                    {
                        TFTVLogger.Always("objective1.Title is missing!");
                    }
                    else
                    {
                        if (objective1.Title.LocalizationKey == null)
                        {
                            TFTVLogger.Always("objective1.Title.LocalizationKey is missing!");
                        }
                        else
                        {
                            if (objective1.Title.LocalizationKey == title)
                            {
                                level.PhoenixFaction.RemoveObjective(objective1);
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

        // public void GameOver() for later 
        [HarmonyPatch(typeof(TacticalLevelController), "ActorDied")]
        public static class TacticalLevelController_ActorDied_HostileDefenders_Patch
        {
            public static void Postfix(DeathReport deathReport, TacticalLevelController __instance)
            {
                try
                {
                    if (VoidOmen5Active)
                    {
                        TacticalFaction phoenix = __instance.GetFactionByCommandName("PX");

                        // TFTVLogger.Always("ActorDied invoked, because " + deathReport.Actor.DisplayName + " died");

                        if (deathReport.Actor.TacticalFaction.ParticipantKind == TacMissionParticipant.Intruder)
                        {

                            // TFTVLogger.Always("If ActorDied passed, because " + deathReport.Actor.DisplayName + " was intruder");

                            if (deathReport.Actor.TacticalFaction.State == TacFactionState.Defeated)
                            {
                                foreach (TacticalFaction tacticalFaction in __instance.Factions)
                                {
                                    if (tacticalFaction.GetRelationTo(phoenix) == FactionRelation.Enemy)
                                    {
                                        tacticalFaction.ParticipantKind = TacMissionParticipant.Player;

                                    }
                                }
                                phoenix.State = TacFactionState.Won;

                                __instance.GameOver();

                                //  TFTVLogger.Always("Check passed, aliens lost");
                                //  TFTVHavenDefense.ConvertDefendersToPX(__instance);
                                //  TFTVHavenDefense.ConvertCiviliansToPX(__instance);


                                //  

                                // TFTVLogger.Always("This " + faction.Faction.ToString() + " won");

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


        [HarmonyPatch(typeof(TacticalAbility), "get_WillPointCost")]
        public static class TacticalAbility_get_WillPointCost_VoidOmenExtraWPCost_Patch
        {
            public static void Postfix(ref float __result, TacticalAbility __instance)
            {
                try
                {
                    if (__result > 0)
                    {
                        if (VoidOmen3Active && __instance.TacticalActor.IsControlledByPlayer)
                        {
                            __result += Mathf.RoundToInt(__result * 0.5f);
                            //TFTVLogger.Always("WP cost increased to " + __result);
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(TacticalVoxelMatrix), "SpawnAndPropagateMist")]
        public static class TacticalVoxelMatrix_SpawnAndPropagateMist_VoidOmenMoreMistOnTactical_Patch
        {
            public static void Prefix(TacticalVoxelMatrix __instance)
            {
                try
                {
                    if (VoidOmen7Active)
                    {
                        __instance.VoxelMatrixData.InitialMistEntitiesToSpawn.Min = 30;
                        __instance.VoxelMatrixData.InitialMistEntitiesToSpawn.Max = 40;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(UIStateRosterDeployment), "get__squadMaxDeployment")]
        public static class UIStateRosterDeployment_get_SquadMaxDeployment_VoidOmenLimitedDeployment_Patch
        {
            public static void Postfix(ref int __result)
            {
                try
                {
                    if (VoidOmen4Active)
                    {
                        __result -= 2;
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }

        [HarmonyPatch(typeof(GeoBehemothActor), "get_DisruptionMax")]
        public static class GeoBehemothActor_get_DisruptionMax_VoidOmenBehemothRoamsMore_Patch
        {
            public static void Postfix(ref int __result, GeoBehemothActor __instance)
            {
                try
                {
                    int[] voidOmensInEffect = CheckFordVoidOmensInPlay(__instance.GeoLevel);
                    if (voidOmensInEffect.Contains(11))
                    {
                        __result += 3 * __instance.GeoLevel.CurrentDifficultyLevel.Order;
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }



        [HarmonyPatch(typeof(GeoSite), "CreateHavenDefenseMission")]
        public static class GeoSite_CreateHavenDefenseMission_IncreaseAttackHavenVoidOmen_patch
        {
            public static void Prefix(ref HavenAttacker attacker)
            {
                try
                {
                    if (VoidOmen12Active)
                    {
                        SharedData sharedData = GameUtl.GameComponent<SharedData>();
                        if (attacker.Faction.PPFactionDef == sharedData.AlienFactionDef)
                        {
                            TFTVLogger.Always("Alien deployment was " + attacker.Deployment);
                            attacker.Deployment = Mathf.RoundToInt(attacker.Deployment * 1.5f);
                            TFTVLogger.Always("Alien deployment is now " + attacker.Deployment);
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        [HarmonyPatch(typeof(GeoAlienFaction), "AlienBaseDestroyed")]
        public static class GeoAlienFaction_AlienBaseDestroyed_RemoveVoidOmenDestroyedPC_patch
        {
            public static void Prefix(GeoAlienBase alienBase, GeoAlienFaction __instance)
            {
                try
                {
                    TFTVLogger.Always("Lair or Citadal destroyed");
                    if (alienBase.AlienBaseTypeDef.Keyword == "lair" || alienBase.AlienBaseTypeDef.Keyword == "citadel"
                        || (alienBase.AlienBaseTypeDef.Keyword == "nest" && __instance.GeoLevel.CurrentDifficultyLevel.Order == 1))
                    {
                        TFTVLogger.Always("Lair or Citadal destroyed, Void Omen should be removed");
                        RemoveEarliestVoidOmen(__instance.GeoLevel);

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
