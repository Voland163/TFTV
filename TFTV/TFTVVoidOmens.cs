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
using PhoenixPoint.Tactical.Levels.FactionObjectives;
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

        public static void Create_VoidOmen_Events()

        {
            GeoscapeEventDef voidOmenEvent = TFTVCommonMethods.CreateNewEvent("VoidOmen", "", "", null);
            GeoscapeEventDef voidOmenIntro = TFTVCommonMethods.CreateNewEvent("VoidOmenIntro", "", "", null);

        }

        private static readonly DefRepository Repo = TFTVMain.Repo;

        public static bool[] voidOmensCheck = new bool[18];
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

        public static readonly string[] VoidOmens_Title = new string[]
        {
        "VOID_OMEN_TITLE_01","VOID_OMEN_TITLE_02","VOID_OMEN_TITLE_03","VOID_OMEN_TITLE_04","VOID_OMEN_TITLE_05","VOID_OMEN_TITLE_06",
        "VOID_OMEN_TITLE_07","VOID_OMEN_TITLE_08","VOID_OMEN_TITLE_09","VOID_OMEN_TITLE_10","VOID_OMEN_TITLE_11","VOID_OMEN_TITLE_12",
        "VOID_OMEN_TITLE_13","VOID_OMEN_TITLE_14","VOID_OMEN_TITLE_15","VOID_OMEN_TITLE_16","VOID_OMEN_TITLE_17","VOID_OMEN_TITLE_18",
        "VOID_OMEN_TITLE_19","VOID_OMEN_TITLE_20",
        };
        public static readonly string[] VoidOmens_Description = new string[]
        {
        "VOID_OMEN_DESCRIPTION_TEXT_01","VOID_OMEN_DESCRIPTION_TEXT_02","VOID_OMEN_DESCRIPTION_TEXT_03","VOID_OMEN_DESCRIPTION_TEXT_04",
        "VOID_OMEN_DESCRIPTION_TEXT_05","VOID_OMEN_DESCRIPTION_TEXT_06","VOID_OMEN_DESCRIPTION_TEXT_07","VOID_OMEN_DESCRIPTION_TEXT_08",
        "VOID_OMEN_DESCRIPTION_TEXT_09","VOID_OMEN_DESCRIPTION_TEXT_10","VOID_OMEN_DESCRIPTION_TEXT_11","VOID_OMEN_DESCRIPTION_TEXT_12",
        "VOID_OMEN_DESCRIPTION_TEXT_13","VOID_OMEN_DESCRIPTION_TEXT_14","VOID_OMEN_DESCRIPTION_TEXT_15","VOID_OMEN_DESCRIPTION_TEXT_16",
        "VOID_OMEN_DESCRIPTION_TEXT_17","VOID_OMEN_DESCRIPTION_TEXT_18","VOID_OMEN_DESCRIPTION_TEXT_19","VOID_OMEN_DESCRIPTION_TEXT_20",
        };


        public static void ImplementVoidOmens(GeoLevelController level)
        {
            try
            {
                string voidOmen = "VoidOmen_";
                int difficulty = level.CurrentDifficultyLevel.Order;
                int[] voidOmensInPlay = CheckFordVoidOmensInPlay(level);
                TFTVLogger.Always("Checking if method invocation is working, these are the Void Omens in play " + voidOmensInPlay[0] + " "
                    + voidOmensInPlay[1] + " " + voidOmensInPlay[2] + " " + voidOmensInPlay[3]);

                for (int i = 1; i < voidOmensCheck.Count() - 1; i++)
                {
                    if (i == 1 && voidOmensInPlay.Contains(i) && !voidOmensCheck[i])
                    {

                        level.EventSystem.ExplorationAmbushChance = 100;
                        CustomMissionTypeDef AmbushALN = Repo.GetAllDefs<CustomMissionTypeDef>().FirstOrDefault(ged => ged.name.Equals("AmbushAlien_CustomMissionTypeDef"));
                        AmbushALN.ParticipantsData[0].ReinforcementsTurns.Max = 1;
                        AmbushALN.ParticipantsData[0].ReinforcementsTurns.Min = 1;
                        AmbushALN.CratesDeploymentPointsRange.Min = 50;
                        AmbushALN.CratesDeploymentPointsRange.Max = 70;
                        // Logger.Always(voidOmen + j + " is now in effect, held in variable "+voidOmen + i);
                        voidOmensCheck[i] = true;
                    }
                    else if (i == 1 && !voidOmensInPlay.Contains(1) && voidOmensCheck[1])
                    {
                        level.EventSystem.ExplorationAmbushChance = 70;
                        CustomMissionTypeDef AmbushALN = Repo.GetAllDefs<CustomMissionTypeDef>().FirstOrDefault(ged => ged.name.Equals("AmbushAlien_CustomMissionTypeDef"));
                        AmbushALN.ParticipantsData[0].ReinforcementsTurns.Max = 2;
                        AmbushALN.ParticipantsData[0].ReinforcementsTurns.Min = 2;
                        AmbushALN.CratesDeploymentPointsRange.Min = 30;
                        AmbushALN.CratesDeploymentPointsRange.Max = 50;
                        voidOmensCheck[1] = false;
                        //   Logger.Always("Exploration ambush chance is now " + level.EventSystem.ExplorationAmbushChance);
                        //  Logger.Always("Alien ambushes can now have a max of  " + AmbushALN.CratesDeploymentPointsRange.Max / 10 + " crates");
                        TFTVLogger.Always("The check for VO#1 went ok");
                    }
                    if (i == 2 && voidOmensInPlay.Contains(i) && !voidOmensCheck[i])
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
                                        diplomacyChange.Value = Mathf.RoundToInt(diplomacyChange.Value * 0.5f);
                                        choice.Outcome.Diplomacy[t] = diplomacyChange;
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
                    else if (i == 2 && !voidOmensInPlay.Contains(i) && voidOmensCheck[i])
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
                                        diplomacyChange.Value = Mathf.RoundToInt(diplomacyChange.Value * 2f);
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
                    if (i == 3 && voidOmensInPlay.Contains(i) && !voidOmensCheck[i])
                    {
                        // Logger.Always(voidOmen + j + " is now in effect, held in variable " + voidOmen + i);
                        voidOmensCheck[i] = true;
                    }
                    else if (i == 3 && !voidOmensInPlay.Contains(i) && voidOmensCheck[i])
                    {
                        voidOmensCheck[i] = false;
                        TFTVLogger.Always("The check for VO#3 went ok");
                    }
                    if (i == 4 && voidOmensInPlay.Contains(i) && !voidOmensCheck[i])
                    {
                        VoidOmen4Active = true;
                        voidOmensCheck[i] = true;
                    }
                    else if (i == 4 && !voidOmensInPlay.Contains(i) && voidOmensCheck[i])
                    {
                        VoidOmen4Active = false;
                        voidOmensCheck[4] = false;
                        TFTVLogger.Always("The check for VO#4 went ok");

                    }
                    if (i == 5 && voidOmensInPlay.Contains(i) && !voidOmensCheck[i])
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
                                missionTypeDef.MissionSpecificCrates = cratesNotResources;
                                missionTypeDef.FactionItemsRange.Min = 2;
                                missionTypeDef.FactionItemsRange.Max = 7;
                                missionTypeDef.CratesDeploymentPointsRange.Min = 20;
                                missionTypeDef.CratesDeploymentPointsRange.Max = 30;
                               
                            }
                        }
                        // Logger.Always(voidOmen + j + " is now in effect, held in variable " + voidOmen + i);
                        voidOmensCheck[i] = true;
                    }
                    else if (i == 5 && !voidOmensInPlay.Contains(i) && voidOmensCheck[i])
                    {
                        foreach (CustomMissionTypeDef missionTypeDef in Repo.GetAllDefs<CustomMissionTypeDef>())
                        {
                            if (missionTypeDef.name.Contains("Haven") && !missionTypeDef.name.Contains("Infestation"))
                            {
                                TacticalFactionEffectDef defendersCanBeRecruited = Repo.GetAllDefs<TacticalFactionEffectDef>().FirstOrDefault(ged => ged.name.Equals("CanBeRecruitedByPhoenix_FactionEffectDef"));

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
                            }
                        }
                        voidOmensCheck[5] = false;
                        TFTVLogger.Always("The check for VO#5 went ok");

                    }
                    if (i == 6 && voidOmensInPlay.Contains(i) && !voidOmensCheck[i])
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
                    else if (i == 6 && !voidOmensInPlay.Contains(i) && voidOmensCheck[i])
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
                    if (i == 7 && voidOmensInPlay.Contains(i) && !voidOmensCheck[i])
                    {
                        //  Logger.Always(voidOmen + j + " is now in effect, held in variable " + voidOmen + i);
                        voidOmensCheck[i] = true;
                    }
                    else if (i == 7 && !voidOmensInPlay.Contains(i) && voidOmensCheck[i])
                    {
                        voidOmensCheck[7] = false;
                        TFTVLogger.Always("The check for VO#7 went ok");

                    }
                    if (i == 8 && voidOmensInPlay.Contains(i) && !voidOmensCheck[i])
                    {
                        GeoFactionDef phoenixPoint = Repo.GetAllDefs<GeoFactionDef>().FirstOrDefault(ged => ged.name.Equals("Phoenix_GeoPhoenixFactionDef"));
                        foreach (GeoscapeEventDef geoEvent in Repo.GetAllDefs<GeoscapeEventDef>())
                        {
                            foreach (GeoEventChoice choice in geoEvent.GeoscapeEventData.Choices)
                            {
                                for (int t = 0; t < choice.Outcome.Diplomacy.Count; t++)
                                {
                                    if (choice.Outcome.Diplomacy[t].Value <= 0 && choice.Outcome.Diplomacy[t].TargetFaction != phoenixPoint)
                                    {
                                        OutcomeDiplomacyChange diplomacyChange = choice.Outcome.Diplomacy[t];
                                        diplomacyChange.Value = Mathf.RoundToInt(diplomacyChange.Value * 0.5f);
                                        choice.Outcome.Diplomacy[t] = diplomacyChange;
                                    }
                                }
                            }
                        }
                        GeoHavenZoneDef havenLab = Repo.GetAllDefs<GeoHavenZoneDef>().FirstOrDefault(ged => ged.name.Equals("Research_GeoHavenZoneDef"));
                        havenLab.ProvidesResearch = 2;
                        // Logger.Always(voidOmen + j + " is now in effect, held in variable " + voidOmen + i);
                        voidOmensCheck[i] = true;
                    }
                    else if (i == 8 && !voidOmensInPlay.Contains(i) && voidOmensCheck[i])
                    {
                        GeoFactionDef phoenixPoint = Repo.GetAllDefs<GeoFactionDef>().FirstOrDefault(ged => ged.name.Equals("Phoenix_GeoPhoenixFactionDef"));
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
                        GeoHavenZoneDef havenLab = Repo.GetAllDefs<GeoHavenZoneDef>().FirstOrDefault(ged => ged.name.Equals("Research_GeoHavenZoneDef"));
                        havenLab.ProvidesResearch = 1;
                        TFTVLogger.Always("The check for VO#8 went ok");
                        voidOmensCheck[8] = false;

                    }
                    if (i == 9 && voidOmensInPlay.Contains(i) && !voidOmensCheck[i])
                    {
                        FesteringSkiesSettingsDef festeringSkiesSettingsDef = Repo.GetAllDefs<FesteringSkiesSettingsDef>().FirstOrDefault(ged => ged.name.Equals("FesteringSkiesSettingsDef"));
                        festeringSkiesSettingsDef.HavenAttackCounterModifier = 0.66f;
                        //  Logger.Always(voidOmen + j + " is now in effect, held in variable " + voidOmen + i);
                        voidOmensCheck[i] = true;
                    }
                    else if (i == 9 && !voidOmensInPlay.Contains(i) && voidOmensCheck[i])
                    {
                        FesteringSkiesSettingsDef festeringSkiesSettingsDef = Repo.GetAllDefs<FesteringSkiesSettingsDef>().FirstOrDefault(ged => ged.name.Equals("FesteringSkiesSettingsDef"));
                        festeringSkiesSettingsDef.HavenAttackCounterModifier = 1.33f;
                        voidOmensCheck[9] = false;
                        TFTVLogger.Always("The check for VO#9 went ok");

                    }
                    if (i == 10 && voidOmensInPlay.Contains(i) && !voidOmensCheck[i])
                    {
                        // Logger.Always(voidOmen + j + " is now in effect, held in variable " + voidOmen + i);
                        voidOmensCheck[i] = true;
                    }
                    else if (i == 10 && !voidOmensInPlay.Contains(i) && voidOmensCheck[i])
                    {
                        voidOmensCheck[10] = false;
                        TFTVLogger.Always("The check for VO#10 went ok");

                    }
                    if (i == 11 && voidOmensInPlay.Contains(i) && !voidOmensCheck[i])
                    {
                        voidOmensCheck[i] = true;
                    }
                    else if (i == 11 && !voidOmensInPlay.Contains(i) && voidOmensCheck[i])
                    {
                        /*   FesteringSkiesSettingsDef festeringSkiesSettingsDef = Repo.GetAllDefs<FesteringSkiesSettingsDef>().FirstOrDefault(ged => ged.name.Equals("FesteringSkiesSettingsDef"));
                           festeringSkiesSettingsDef.DisruptionThreshholdBaseValue = 3;*/
                        voidOmensCheck[11] = false;
                        TFTVLogger.Always("The check for VO#11 went ok");

                    }
                    if (i == 12 && voidOmensInPlay.Contains(i) && !voidOmensCheck[i])
                    {
                        VoidOmen12Active = true;
                        // Logger.Always(voidOmen + j + " is now in effect, held in variable " + voidOmen + i);
                        voidOmensCheck[i] = true;
                    }
                    else if (i == 12 && !voidOmensInPlay.Contains(i) && voidOmensCheck[i])
                    {
                        VoidOmen12Active = false;
                        voidOmensCheck[12] = false;
                        TFTVLogger.Always("The check for VO#12 went ok");

                    }
                    if (i == 13 && voidOmensInPlay.Contains(i) && !voidOmensCheck[i])
                    {
                        level.CurrentDifficultyLevel.NestLimitations.HoursBuildTime /= 2;
                        level.CurrentDifficultyLevel.LairLimitations.HoursBuildTime /= 2;
                        level.CurrentDifficultyLevel.CitadelLimitations.HoursBuildTime /= 2;
                        TFTVLogger.Always(voidOmen + i + " is now in effect, held in variable " + voidOmen + i + ", so Pandoran nests take " + level.CurrentDifficultyLevel.NestLimitations.HoursBuildTime + " hours");
                        voidOmensCheck[i] = true;
                    }
                    else if (i == 13 && !voidOmensInPlay.Contains(i) && voidOmensCheck[i])
                    {
                        level.CurrentDifficultyLevel.NestLimitations.HoursBuildTime *= 2;
                        level.CurrentDifficultyLevel.LairLimitations.HoursBuildTime *= 2;
                        level.CurrentDifficultyLevel.CitadelLimitations.HoursBuildTime *= 2;

                        voidOmensCheck[13] = false;

                        TFTVLogger.Always("The check for VO#13 went ok" + " so Pandoran nests take " + level.CurrentDifficultyLevel.NestLimitations.HoursBuildTime + " hours");

                    }
                    if (i == 14 && voidOmensInPlay.Contains(i) && !voidOmensCheck[i])
                    {
                        TacticalPerceptionDef tacticalPerceptionDef = Repo.GetAllDefs<TacticalPerceptionDef>().FirstOrDefault((TacticalPerceptionDef a) => a.name.Equals("Soldier_PerceptionDef"));
                        tacticalPerceptionDef.PerceptionRange = 20;
                        // Logger.Always(voidOmen + j + " is now in effect, held in variable " + voidOmen + i);
                        voidOmensCheck[i] = true;
                    }
                    else if (i == 14 && !voidOmensInPlay.Contains(i) && voidOmensCheck[i])
                    {
                        TacticalPerceptionDef tacticalPerceptionDef = Repo.GetAllDefs<TacticalPerceptionDef>().FirstOrDefault((TacticalPerceptionDef a) => a.name.Equals("Soldier_PerceptionDef"));
                        tacticalPerceptionDef.PerceptionRange = 30;
                        voidOmensCheck[14] = false;
                        TFTVLogger.Always("The check for VO#14 went ok");

                    }
                    if (i == 15 && voidOmensInPlay.Contains(i) && !voidOmensCheck[i])
                    {
                        //  Logger.Always(voidOmen + j + " is now in effect, held in variable " + voidOmen + i);
                        voidOmensCheck[i] = true;
                    }
                    else if (i == 15 && !voidOmensInPlay.Contains(i) && voidOmensCheck[i])
                    {
                        voidOmensCheck[15] = false;
                        TFTVLogger.Always("The check for VO#15 went ok");

                    }
                    if (i == 16 && voidOmensInPlay.Contains(i) && !voidOmensCheck[i])
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
                    else if (i == 16 && !voidOmensInPlay.Contains(i) && voidOmensCheck[i])
                    {
                        TFTVUmbra.SetUmbraRandomValue(0);

                        voidOmensCheck[16] = false;
                        TFTVLogger.Always("The check for VO#16 went ok");

                    }
                    if (i == 17 && voidOmensInPlay.Contains(i) && !voidOmensCheck[i])
                    {
                        voidOmensCheck[i] = true;
                    }
                    else if (i == 17 && !voidOmensInPlay.Contains(i) && voidOmensCheck[i])
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
                VoidOmen7Active = false;
                VoidOmen10Active = false;
                VoidOmen15Active = false;
                VoidOmen16Active = false;

                int[] rolledVoidOmens = CheckFordVoidOmensInPlay(level);

                if (rolledVoidOmens.Contains(3))
                {
                    VoidOmen3Active = true;
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

        public static int[] CheckForAlreadyRolledVoidOmens(GeoLevelController geoLevelController)
        {
            try
            {
                int difficulty = geoLevelController.CurrentDifficultyLevel.Order;
                int[] allVoidOmensAlreadyRolled = new int[20];
                string triggeredVoidOmens = "TriggeredVoidOmen_";

                for (int x = 1; x < 20; x++)
                {
                    if (geoLevelController.EventSystem.GetVariable(triggeredVoidOmens + x) != 0)
                    {
                        allVoidOmensAlreadyRolled[x] = geoLevelController.EventSystem.GetVariable(triggeredVoidOmens + x);
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

                // An array to record all the Void Omens rolled so far
                int[] allVoidOmensAlreadyRolled = CheckForAlreadyRolledVoidOmens(geoLevelController);
                // An array to record which variables hold which Void Omens
                int[] voidOmensInPlay = new int[difficulty];

                // This is a variable to close the loop below when the array of Void Omens in play is full               
                int variablesUsed = 0;

                // We will check our Void Omen variables to see which one has the earliest Void Omen already rolled                                
                for (int x = 1; x < 20; x++)
                {
                    // We will look through the Void Omen variables in the order in which they were filled
                    for (int y = 0; y < difficulty; y++)
                    {
                        // And record which variable holds which Void Omen, by checking it against the array of already rolled Void Omens
                        if (geoLevelController.EventSystem.GetVariable(voidOmen + (difficulty - y)) == allVoidOmensAlreadyRolled[x])
                        {
                            voidOmensInPlay[difficulty - y - 1] = allVoidOmensAlreadyRolled[x];
                            //  Logger.Always("Check Variable " + (difficulty - y) + " holding Void Omen " + voidOmensInPlay[difficulty - y - 1]);
                            variablesUsed++;
                        }
                        // We also have to record which variables are empty
                        else if (geoLevelController.EventSystem.GetVariable(voidOmen + (difficulty - y)) == 0)
                        {
                            voidOmensInPlay[difficulty - y - 1] = 0;
                            //  Logger.Always("Check Variable " + (difficulty - y) + " holding Void Omen " + voidOmensInPlay[difficulty - y - 1]);
                            variablesUsed++;
                        }
                    }
                    //  Logger.Always("the count of variables used is " + variablesUsed);
                    if (variablesUsed == difficulty)
                    {
                        x = 20;
                    }

                }
                TFTVLogger.Always("The Void Omens already in play are " + voidOmensInPlay[0] + " " + voidOmensInPlay[1] + " " + voidOmensInPlay[2] + " " + voidOmensInPlay[3]);
                return voidOmensInPlay;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }

        public static void RemoveEarliestVoidOmen
        (GeoLevelController geoLevelController)
        {
            try
            {
                int difficulty = geoLevelController.CurrentDifficultyLevel.Order;
                string triggeredVoidOmens = "TriggeredVoidOmen_";
                string voidOmen = "VoidOmen_";

                // And an array to record which variables hold which Dark Events
                int[] voidOmensinPlay = CheckFordVoidOmensInPlay(geoLevelController);
                TFTVLogger.Always("Checking if method invocation is working, these are all the Void Omens in play " + voidOmensinPlay[0] + " "
                    + voidOmensinPlay[1] + " " + voidOmensinPlay[2] + " " + voidOmensinPlay[3]);

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
                    string objectiveToBeReplaced = (string)VoidOmens_Title.GetValue(replacedVoidOmen - 1);
                    TFTVLogger.Always("The target event that will be replaced is " + objectiveToBeReplaced);
                    RemoveVoidOmenObjective(objectiveToBeReplaced, geoLevelController);
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

                // And an array to record which variables hold which Dark Events
                int[] voidOmensinPlay = CheckFordVoidOmensInPlay(geoLevelController);
                TFTVLogger.Always("Checking if method invocation is working, these are all the Void Omens in play " + voidOmensinPlay[0] + " "
                    + voidOmensinPlay[1] + " " + voidOmensinPlay[2] + " " + voidOmensinPlay[3]);

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
                        string objectiveToBeReplaced = (string)VoidOmens_Title.GetValue(replacedVoidOmen - 1);
                        TFTVLogger.Always("The target event that will be replaced is " + objectiveToBeReplaced);
                        RemoveVoidOmenObjective(objectiveToBeReplaced, geoLevelController);
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
                DiplomaticGeoFactionObjective voidOmenObjective =
            (DiplomaticGeoFactionObjective)level.PhoenixFaction.Objectives.FirstOrDefault(ged => ged.Title.LocalizationKey.Equals(title));
                string checktitle = voidOmenObjective.GetTitle();
                TFTVLogger.Always("the title in the RemoveVoidOmenObjective method is " + title);
                TFTVLogger.Always("if we found the objective, there should be something here " + checktitle);
                level.PhoenixFaction.RemoveObjective(voidOmenObjective);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
        
        [HarmonyPatch(typeof(TacticalLevelController), "ActorDied")]
        public static class PhoenixStatisticsManager_NewTurnEvent_CalculateDelirium_Patch
        {
            public static void Postfix(DeathReport deathReport, TacticalLevelController __instance)
            {
                try
                {
                    if (VoidOmen5Active)
                    {

                        // TFTVLogger.Always("ActorDied invoked, because " + deathReport.Actor.DisplayName + " died");

                        if (deathReport.Actor.TacticalFaction.ParticipantKind == TacMissionParticipant.Intruder)
                        {
                            // TFTVLogger.Always("If ActorDied passed, because " + deathReport.Actor.DisplayName + " was intruder");
                            TacticalFaction aliens = deathReport.Actor.TacticalFaction;
                            if (deathReport.Actor.TacticalFaction.State == TacFactionState.Defeated)
                            {
                                //  TFTVLogger.Always("Check passed, aliens lost");

                                List<TacticalFaction> factions = __instance.Factions.ToList();
                                foreach (TacticalFaction faction in factions)
                                {
                                    if (faction.IsControlledByPlayer)
                                    {
                                        faction.State = TacFactionState.Won;
                                        // TFTVLogger.Always("This " + faction.Faction.ToString() + " won");
                                    }
                                    else
                                    {
                                        faction.State = TacFactionState.Defeated;
                                        //  TFTVLogger.Always("This " + faction.Faction.ToString() + " lost");
                                    }

                                }
                            }

        }
        
        [HarmonyPatch(typeof(TacticalLevelController), "ActorDied")]
        public static class PhoenixStatisticsManager_NewTurnEvent_CalculateDelirium_Patch
        {
            public static void Postfix(DeathReport deathReport, TacticalLevelController __instance)
            {
                try
                {
                    if (VoidOmen5Active)
                    {

                        // TFTVLogger.Always("ActorDied invoked, because " + deathReport.Actor.DisplayName + " died");

                        if (deathReport.Actor.TacticalFaction.ParticipantKind == TacMissionParticipant.Intruder)
                        {
                            // TFTVLogger.Always("If ActorDied passed, because " + deathReport.Actor.DisplayName + " was intruder");
                            
                            if (deathReport.Actor.TacticalFaction.State == TacFactionState.Defeated)
                            {
                                //  TFTVLogger.Always("Check passed, aliens lost");

                                List<TacticalFaction> factions = __instance.Factions.ToList();
                                foreach (TacticalFaction faction in factions)
                                {
                                    if (faction.IsControlledByPlayer)
                                    {
                                        faction.State = TacFactionState.Won;
                                        // TFTVLogger.Always("This " + faction.Faction.ToString() + " won");
                                    }
                                    else
                                    {
                                        faction.State = TacFactionState.Defeated;
                                        //  TFTVLogger.Always("This " + faction.Faction.ToString() + " lost");
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
                            TFTVLogger.Always("WP cost increased to " + __result);
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
