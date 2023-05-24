using Assets.Code.PhoenixPoint.Geoscape.Entities.Sites.TheMarketplace;
using Base.Core;
using Base.Defs;
using Base.Entities.Effects;
using Base.UI;
using com.ootii.Helpers;
using HarmonyLib;
using PhoenixPoint.Common.ContextHelp;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Geoscape.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.Levels.Objectives;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.FactionEffects;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using PhoenixPoint.Tactical.Levels.Mist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TFTV
{
    internal class TFTVVoidOmens
    {
        public static readonly TFTVConfig Config = new TFTVConfig();
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;

        private static readonly FesteringSkiesSettingsDef festeringSkiesSettingsDef = DefCache.GetDef<FesteringSkiesSettingsDef>("FesteringSkiesSettingsDef");
        private static readonly TacticalPerceptionDef tacticalPerceptionDef = DefCache.GetDef<TacticalPerceptionDef>("Soldier_PerceptionDef");
        private static readonly CustomMissionTypeDef AmbushALN = DefCache.GetDef<CustomMissionTypeDef>("AmbushAlien_CustomMissionTypeDef");
        private static readonly TacCrateDataDef cratesNotResources = DefCache.GetDef<TacCrateDataDef>("Default_TacCrateDataDef");
        private static readonly TacticalFactionEffectDef defendersCanBeRecruited = DefCache.GetDef<TacticalFactionEffectDef>("CanBeRecruitedByPhoenix_FactionEffectDef");
        private static readonly GeoHavenZoneDef havenLab = DefCache.GetDef<GeoHavenZoneDef>("Research_GeoHavenZoneDef");

        public static bool[] VoidOmensCheck = new bool[20];

        //VO#1 is harder ambushes
        //VO#2 is All diplomatic penalties and rewards halved
        //VO#3 is WP cost +50%

        //VO#4 is limited deplyoment, extra XP

        //VO#5 is haven defenders hostile; this is needed for victory kludge

        //VO#7 is more mist in missions

        //VO#10 is no limit to Delirium

        //VO#12 is +50% strength of alien attacks on Havens

        //VO#15 is more Umbra

        //VO#16 is Umbras can appear anywhere and attack anyone

        //V0#18 is extra defense points, less rewards


        public static void CheckVoidOmensBeforeImplementing(GeoLevelController level)
        {
            try

            {
                List<int> VoidOmensInPLay = new List<int>();
                List<int> AlreadyRolledVoidOmens = new List<int>();

                int difficulty = level.CurrentDifficultyLevel.Order;
                string voidOmen = "VoidOmen_";

                for (int i = 0; i < CheckFordVoidOmensInPlay(level).Count(); i++)
                {
                    TFTVLogger.Always("Void Omen " + CheckFordVoidOmensInPlay(level)[i] + " is in play");
                    VoidOmensInPLay.Add(CheckFordVoidOmensInPlay(level)[i]);
                }

                for (int x = 0; x < CheckForAlreadyRolledVoidOmens(level).Count; x++)
                {
                    TFTVLogger.Always("Void Omen " + CheckForAlreadyRolledVoidOmens(level)[x] + " rolled at some point");
                    AlreadyRolledVoidOmens.Add(CheckForAlreadyRolledVoidOmens(level)[x]);
                }

                bool foundError = false;

                foreach (int x in VoidOmensInPLay)
                {
                    if (x != 0 && !AlreadyRolledVoidOmens.Contains(x))
                    {
                        TFTVLogger.Always("Found a ghost VO " + x + ". Will try to remove");
                        foundError = true;
                        for (int y = 0; y < CheckFordVoidOmensInPlay(level).Count(); y++)
                        {
                            if (CheckFordVoidOmensInPlay(level)[y] == x)
                            {
                                level.EventSystem.SetVariable(voidOmen + (y + 1), 0);
                            }
                        }
                    }
                }
                if (foundError)
                {
                    TFTVLogger.Always("Verifying if error was corrected...");
                    for (int i = 0; i < CheckFordVoidOmensInPlay(level).Count(); i++)
                    {
                        TFTVLogger.Always("Void Omen " + CheckFordVoidOmensInPlay(level)[i] + " is in play");
                    }

                    for (int x = 0; x < CheckForAlreadyRolledVoidOmens(level).Count; x++)
                    {
                        TFTVLogger.Always("Void Omen " + CheckForAlreadyRolledVoidOmens(level)[x] + " rolled at some point");
                    }
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }
        public static void ImplementVoidOmens(GeoLevelController level)
        {
            try
            {
                // VoidOmensCheck = new bool[20];

                if (CheckFordVoidOmensInPlay(level).Contains(1))
                {
                    //VoidOmen1Active = true;
                    VoidOmensCheck[1] = true;


                    AmbushALN.ParticipantsData[0].ReinforcementsTurns.Max = 1;
                    AmbushALN.ParticipantsData[0].ReinforcementsTurns.Min = 1;
                    AmbushALN.CratesDeploymentPointsRange.Min = 50;
                    AmbushALN.CratesDeploymentPointsRange.Max = 70;
                    // VoidOmensCheck[1] = true;
                }
                else if (!CheckFordVoidOmensInPlay(level).Contains(1) && VoidOmensCheck[1])
                {
                    VoidOmensCheck[1] = false;
                    AmbushALN.ParticipantsData[0].ReinforcementsTurns.Max = 2;
                    AmbushALN.ParticipantsData[0].ReinforcementsTurns.Min = 2;
                    AmbushALN.CratesDeploymentPointsRange.Min = 30;
                    AmbushALN.CratesDeploymentPointsRange.Max = 50;

                    //  VoidOmensCheck[1] = false;
                    TFTVLogger.Always("The check for VO#1 went ok");
                }
                if (CheckFordVoidOmensInPlay(level).Contains(2))
                {

                    PartyDiplomacySettingsDef partyDiplomacySettingsDef = DefCache.GetDef<PartyDiplomacySettingsDef>("PartyDiplomacySettingsDef");
                    partyDiplomacySettingsDef.InfiltrationFactionMultiplier = 0.5f;
                    partyDiplomacySettingsDef.InfiltrationLeaderMultiplier = 0.75f;
                    VoidOmensCheck[2] = true;

                }
                else if (!CheckFordVoidOmensInPlay(level).Contains(2) && VoidOmensCheck[2])
                {

                    PartyDiplomacySettingsDef partyDiplomacySettingsDef = DefCache.GetDef<PartyDiplomacySettingsDef>("PartyDiplomacySettingsDef");
                    partyDiplomacySettingsDef.InfiltrationFactionMultiplier = 1f;
                    partyDiplomacySettingsDef.InfiltrationLeaderMultiplier = 1.5f;
                    VoidOmensCheck[2] = false;
                    TFTVLogger.Always("The check for VO#2 went ok");
                }
                if (CheckFordVoidOmensInPlay(level).Contains(3))
                {
                    VoidOmensCheck[3] = true;
                    // Logger.Always(voidOmen + j + " is now in effect, held in variable " + voidOmen + i);
                    //  VoidOmensCheck[3] = true;
                }
                else if (!CheckFordVoidOmensInPlay(level).Contains(3) && VoidOmensCheck[3])
                {
                    VoidOmensCheck[3] = false;
                    //  VoidOmensCheck[3] = false;
                    TFTVLogger.Always("The check for VO#3 went ok");
                }
                if (CheckFordVoidOmensInPlay(level).Contains(4))
                {
                    VoidOmensCheck[4] = true;

                    //  VoidOmensCheck[4] = true;
                }
                else if (!CheckFordVoidOmensInPlay(level).Contains(4) && VoidOmensCheck[4])
                {
                    VoidOmensCheck[4] = false;
                    // VoidOmensCheck[4] = false;
                    TFTVLogger.Always("The check for VO#4 went ok");

                }
                if (CheckFordVoidOmensInPlay(level).Contains(5))
                {
                    ContextHelpHintDbDef alwaysDisplayedTacticalHintsDbDef = DefCache.GetDef<ContextHelpHintDbDef>("AlwaysDisplayedTacticalHintsDbDef");
                    ContextHelpHintDef hostileDefenders = DefCache.GetDef<ContextHelpHintDef>("HostileDefenders");
                    if (!alwaysDisplayedTacticalHintsDbDef.Hints.Contains(hostileDefenders))
                    {
                        alwaysDisplayedTacticalHintsDbDef.Hints.Add(hostileDefenders);
                    }


                    foreach (CustomMissionTypeDef missionTypeDef in Repo.GetAllDefs<CustomMissionTypeDef>())
                    {
                        if (missionTypeDef.name.Contains("Haven") && !missionTypeDef.name.Contains("Infestation"))
                        {
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
                            missionTypeDef.DontRecoverItems = true;

                        }
                    }

                    //Logger.Always(voidOmen + j + " is now in effect, held in variable " + voidOmen + i);
                    VoidOmensCheck[5] = true;
                }
                else if (!CheckFordVoidOmensInPlay(level).Contains(5) && VoidOmensCheck[5])
                {
                    ContextHelpHintDbDef alwaysDisplayedTacticalHintsDbDef = DefCache.GetDef<ContextHelpHintDbDef>("AlwaysDisplayedTacticalHintsDbDef");
                    ContextHelpHintDef hostileDefenders = DefCache.GetDef<ContextHelpHintDef>("HostileDefenders");
                    if (alwaysDisplayedTacticalHintsDbDef.Hints.Contains(hostileDefenders))
                    {
                        alwaysDisplayedTacticalHintsDbDef.Hints.Remove(hostileDefenders);
                    }

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

                        }
                    }
                    VoidOmensCheck[5] = false;
                    TFTVLogger.Always("The check for VO#5 went ok");

                }
                if (CheckFordVoidOmensInPlay(level).Contains(6))
                {
                    level.CurrentDifficultyLevel.EvolutionPointsGainOnMissionLoss = 20;
                    level.CurrentDifficultyLevel.AlienBaseTypeEvolutionParams[0].EvolutionPerDestroyedBase = 30;
                    level.CurrentDifficultyLevel.AlienBaseTypeEvolutionParams[1].EvolutionPerDestroyedBase = 60;
                    level.CurrentDifficultyLevel.AlienBaseTypeEvolutionParams[2].EvolutionPerDestroyedBase = 90;
                    /*   ResourceGeneratorFacilityComponentDef researchLab = DefCache.GetDef<ResourceGeneratorFacilityComponentDef>("E_ResourceGenerator [ResearchLab_PhoenixFacilityDef]");
                       ResourceGeneratorFacilityComponentDef bionicsLab = DefCache.GetDef<ResourceGeneratorFacilityComponentDef>("E_ResourceGenerator [BionicsLab_PhoenixFacilityDef]");
                       researchLab.BaseResourcesOutput.Values[0] = new ResourceUnit { Type = ResourceType.Research, Value = 6 };
                       bionicsLab.BaseResourcesOutput.Values[0] = new ResourceUnit { Type = ResourceType.Research, Value = 6 };*/
                    VoidOmensCheck[6] = true;
                    level.PhoenixFaction.Research.Update();

                    /*    foreach(GeoPhoenixBase phoenixBase in level.PhoenixFaction.Bases) 
                        { 
                        foreach(GeoPhoenixFacility facility in phoenixBase.Layout.Facilities) 
                            {
                                facility.UpdateOutput();

                            }

                        }*/


                    //  TFTVLogger.Always("VoidOmen6 is " + VoidOmensCheck[6]);
                }
                else if (!CheckFordVoidOmensInPlay(level).Contains(6) && VoidOmensCheck[6])
                {
                    level.CurrentDifficultyLevel.EvolutionPointsGainOnMissionLoss = 0;
                    level.CurrentDifficultyLevel.AlienBaseTypeEvolutionParams[0].EvolutionPerDestroyedBase = 0;
                    level.CurrentDifficultyLevel.AlienBaseTypeEvolutionParams[1].EvolutionPerDestroyedBase = 0;
                    level.CurrentDifficultyLevel.AlienBaseTypeEvolutionParams[2].EvolutionPerDestroyedBase = 0;
                    /*    ResourceGeneratorFacilityComponentDef researchLab = DefCache.GetDef<ResourceGeneratorFacilityComponentDef>("E_ResourceGenerator [ResearchLab_PhoenixFacilityDef]");
                        ResourceGeneratorFacilityComponentDef bionicsLab = DefCache.GetDef<ResourceGeneratorFacilityComponentDef>("E_ResourceGenerator [BionicsLab_PhoenixFacilityDef]");
                        researchLab.BaseResourcesOutput.Values[0] = new ResourceUnit { Type = ResourceType.Research, Value = 4 };
                        bionicsLab.BaseResourcesOutput.Values[0] = new ResourceUnit { Type = ResourceType.Research, Value = 4 };*/
                    VoidOmensCheck[6] = false;
                    TFTVLogger.Always("The check for VO#6 went ok");

                }
                if (CheckFordVoidOmensInPlay(level).Contains(7))
                {
                    VoidOmensCheck[7] = true;
                    //  Logger.Always(voidOmen + j + " is now in effect, held in variable " + voidOmen + i);
                }
                else if (!CheckFordVoidOmensInPlay(level).Contains(7) && VoidOmensCheck[7])
                {
                    VoidOmensCheck[7] = false;

                    TFTVLogger.Always("The check for VO#7 went ok");

                }
                if (CheckFordVoidOmensInPlay(level).Contains(8))
                {

                    havenLab.ProvidesResearch = 2;
                    VoidOmensCheck[8] = true;
                }
                else if (!CheckFordVoidOmensInPlay(level).Contains(8) && VoidOmensCheck[8])
                {
                    havenLab.ProvidesResearch = 1;
                    TFTVLogger.Always("The check for VO#8 went ok");
                    VoidOmensCheck[8] = false;

                }
                if (CheckFordVoidOmensInPlay(level).Contains(9))
                {

                    festeringSkiesSettingsDef.HavenAttackCounterModifier = 0.66f;
                    //  Logger.Always(voidOmen + j + " is now in effect, held in variable " + voidOmen + i);
                    VoidOmensCheck[9] = true;
                }
                else if (!CheckFordVoidOmensInPlay(level).Contains(9) && VoidOmensCheck[9])
                {

                    festeringSkiesSettingsDef.HavenAttackCounterModifier = 1.33f;
                    VoidOmensCheck[9] = false;
                    TFTVLogger.Always("The check for VO#9 went ok");

                }
                if (CheckFordVoidOmensInPlay(level).Contains(10))
                {
                    // Logger.Always(voidOmen + j + " is now in effect, held in variable " + voidOmen + i);
                    VoidOmensCheck[10] = true;
                }
                else if (!CheckFordVoidOmensInPlay(level).Contains(10) && VoidOmensCheck[10])
                {
                    VoidOmensCheck[10] = false;

                    TFTVLogger.Always("The check for VO#10 went ok");

                }
                /*   if (CheckFordVoidOmensInPlay(level).Contains(11))
                   {
                       VoidOmensCheck[11] = true;
                   }
                   else if (!CheckFordVoidOmensInPlay(level).Contains(11) && CheckForAlreadyRolledVoidOmens(level).Contains(11))
                   {

                       VoidOmensCheck[11] = false;
                       TFTVLogger.Always("The check for VO#11 went ok");

                   }*/
                if (CheckFordVoidOmensInPlay(level).Contains(12))
                {

                    // Logger.Always(voidOmen + j + " is now in effect, held in variable " + voidOmen + i);
                    VoidOmensCheck[12] = true;
                }
                else if (!CheckFordVoidOmensInPlay(level).Contains(12) && VoidOmensCheck[12])
                {

                    VoidOmensCheck[12] = false;
                    TFTVLogger.Always("The check for VO#12 went ok");

                }
                if (CheckFordVoidOmensInPlay(level).Contains(13))
                {
                    level.CurrentDifficultyLevel.NestLimitations.HoursBuildTime = 45;
                    level.CurrentDifficultyLevel.LairLimitations.HoursBuildTime = 50;
                    level.CurrentDifficultyLevel.CitadelLimitations.HoursBuildTime = 90;
                    // TFTVLogger.Always(voidOmen + i + " is now in effect, held in variable " + voidOmen + i + ", so Pandoran nests take " + level.CurrentDifficultyLevel.NestLimitations.HoursBuildTime + " hours");
                    VoidOmensCheck[13] = true;
                }
                else if (!CheckFordVoidOmensInPlay(level).Contains(13) && VoidOmensCheck[13])
                {
                    level.CurrentDifficultyLevel.NestLimitations.HoursBuildTime = 90;
                    level.CurrentDifficultyLevel.LairLimitations.HoursBuildTime = 100;
                    level.CurrentDifficultyLevel.CitadelLimitations.HoursBuildTime = 180;

                    VoidOmensCheck[13] = false;

                    TFTVLogger.Always("The check for VO#13 went ok" + " so Pandoran nests take " + level.CurrentDifficultyLevel.NestLimitations.HoursBuildTime + " hours");

                }
                if (CheckFordVoidOmensInPlay(level).Contains(14))
                {

                    tacticalPerceptionDef.PerceptionRange = 20;
                    // Logger.Always(voidOmen + j + " is now in effect, held in variable " + voidOmen + i);
                    VoidOmensCheck[14] = true;
                }
                else if (!CheckFordVoidOmensInPlay(level).Contains(14) && VoidOmensCheck[14])
                {

                    tacticalPerceptionDef.PerceptionRange = 30;
                    VoidOmensCheck[14] = false;
                    TFTVLogger.Always("The check for VO#14 went ok");

                }
                if (CheckFordVoidOmensInPlay(level).Contains(15))
                {
                    //  Logger.Always(voidOmen + j + " is now in effect, held in variable " + voidOmen + i);
                    VoidOmensCheck[15] = true;
                }
                else if (!CheckFordVoidOmensInPlay(level).Contains(15) && VoidOmensCheck[15])
                {
                    VoidOmensCheck[15] = false;
                    TFTVLogger.Always("The check for VO#15 went ok");

                }
                if (CheckFordVoidOmensInPlay(level).Contains(16))
                {

                    VoidOmensCheck[16] = true;
                }
                else if (!CheckFordVoidOmensInPlay(level).Contains(16) && VoidOmensCheck[16])
                {
                    // TFTVUmbra.SetUmbraRandomValue(0);

                    VoidOmensCheck[16] = false;
                    TFTVLogger.Always("The check for VO#16 went ok");

                }
                if (CheckFordVoidOmensInPlay(level).Contains(17))
                {
                    VoidOmensCheck[17] = true;
                }
                else if (!CheckFordVoidOmensInPlay(level).Contains(17) && VoidOmensCheck[17])
                {
                    VoidOmensCheck[17] = false;
                    TFTVLogger.Always("The check for VO#17 went ok");
                }
                if (CheckFordVoidOmensInPlay(level).Contains(18))
                {


                    // Logger.Always(voidOmen + j + " is now in effect, held in variable " + voidOmen + i);
                    VoidOmensCheck[18] = true;
                }
                else if (!CheckFordVoidOmensInPlay(level).Contains(18) && VoidOmensCheck[18])
                {

                    VoidOmensCheck[18] = false;
                    TFTVLogger.Always("The check for VO#18 went ok");
                }
                if (CheckFordVoidOmensInPlay(level).Contains(19))
                {
                    GeoMarketplaceResearchOptionDef randomMarketResearch = DefCache.GetDef<GeoMarketplaceResearchOptionDef>("Random_MarketplaceResearchOptionDef");
                    randomMarketResearch.MaxPrice = 1200;
                    randomMarketResearch.MinPrice = 960;

                    // Logger.Always(voidOmen + j + " is now in effect, held in variable " + voidOmen + i);
                    VoidOmensCheck[19] = true;
                }
                else if (!CheckFordVoidOmensInPlay(level).Contains(19) && VoidOmensCheck[19])
                {
                    GeoMarketplaceResearchOptionDef randomMarketResearch = DefCache.GetDef<GeoMarketplaceResearchOptionDef>("Random_MarketplaceResearchOptionDef");
                    randomMarketResearch.MaxPrice = 1500;
                    randomMarketResearch.MinPrice = 1200;

                    VoidOmensCheck[19] = false;
                    TFTVLogger.Always("The check for VO#19 went ok");
                }

                List<int> VoidOmensInPLay = new List<int>();
                List<int> AlreadyRolledVoidOmens = new List<int>();

                int difficulty = level.CurrentDifficultyLevel.Order;

                for (int i = 0; i < CheckFordVoidOmensInPlay(level).Count(); i++)
                {
                    TFTVLogger.Always("Void Omen " + CheckFordVoidOmensInPlay(level)[i] + " is in play");
                    VoidOmensInPLay.Add(CheckFordVoidOmensInPlay(level)[i]);
                }

                for (int x = 0; x < CheckForAlreadyRolledVoidOmens(level).Count; x++)
                {
                    TFTVLogger.Always("Void Omen " + CheckForAlreadyRolledVoidOmens(level)[x] + " rolled at some point");
                    AlreadyRolledVoidOmens.Add(CheckForAlreadyRolledVoidOmens(level)[x]);
                }

                //pending baby abbadons
                /*  if (i == 20 && CheckFordVoidOmensInPlay(level).Contains(i) && !voidOmensCheck[i])
                {
                    foreach (GeoMarketplaceResearchOptionDef geoMarketplaceResearchOptionDef in DefCache.GetDef<GeoMarketplaceResearchOptionDef>())
                    {
                        geoMarketplaceResearchOptionDef.MinPrice *= 0.80f;
                        geoMarketplaceResearchOptionDef.MaxPrice *= 0.80f;
                    }

                    // Logger.Always(voidOmen + j + " is now in effect, held in variable " + voidOmen + i);
                    voidOmensCheck[i] = true;
                }
                else if (i == 20 && !CheckFordVoidOmensInPlay(level).Contains(i) && voidOmensCheck[i])
                {
                    foreach (GeoMarketplaceResearchOptionDef geoMarketplaceResearchOptionDef in DefCache.GetDef<GeoMarketplaceResearchOptionDef>())
                    {
                        geoMarketplaceResearchOptionDef.MinPrice *= 1.25f;
                        geoMarketplaceResearchOptionDef.MaxPrice *= 1.25f;
                    }
                    voidOmensCheck[20] = false;
                    TFTVLogger.Always("The check for VO#19 went ok");
                }
              */


                TFTVLogger.Always("Void Omens implemented");

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CheckForVoidOmensRequiringTacticalPatching(GeoLevelController level)
        {
            TFTVConfig config = TFTVMain.Main.Config;

            try
            {

                int[] rolledVoidOmens = CheckFordVoidOmensInPlay(level);

                if (rolledVoidOmens.Contains(3))
                {
                    VoidOmensCheck[3] = true;
                    TFTVLogger.Always("All abilities cost +50% WP, but Delirium has no effect on WP");
                }
                else
                {
                    VoidOmensCheck[3] = false;

                }

                if (rolledVoidOmens.Contains(5))
                {
                    VoidOmensCheck[5] = true;
                    TFTVLogger.Always("Haven defenders always hostile, but crates available for looting");
                }
                else
                {
                    VoidOmensCheck[5] = false;

                }

                if (rolledVoidOmens.Contains(7) && config.MoreMistVO)
                {
                    VoidOmensCheck[7] = true;
                    TFTVLogger.Always("More Mist in missions");
                }
                else
                {
                    VoidOmensCheck[7] = false;

                }
                if (rolledVoidOmens.Contains(10))
                {
                    VoidOmensCheck[10] = true;
                    TFTVLogger.Always("No limit to Delirium, regardless of ODI level");
                }
                else
                {
                    VoidOmensCheck[10] = false;

                }
                if (rolledVoidOmens.Contains(15))
                {
                    VoidOmensCheck[15] = true;
                    TFTVLogger.Always("More Umbras");
                }
                else
                {
                    VoidOmensCheck[15] = false;

                }
                if (rolledVoidOmens.Contains(16))
                {
                    VoidOmensCheck[16] = true;
                    TFTVLogger.Always("Umbras can appear anywhere and attack anyone");
                }
                else
                {
                    VoidOmensCheck[16] = false;

                }
                if (rolledVoidOmens.Contains(19))
                {
                    VoidOmensCheck[19] = true;
                    TFTVLogger.Always("Reactive evolution");
                }
                else
                {
                    VoidOmensCheck[19] = false;

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

                for (int x = 1; x < 100; x++)
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

        public static void ClearListOfAlreadyRolledVoidOmens(GeoLevelController geoLevelController)
        {
            try
            {
                string triggeredVoidOmens = "TriggeredVoidOmen_";

                for (int x = 1; x < 100; x++)
                {
                    geoLevelController.EventSystem.SetVariable(triggeredVoidOmens + x, 0);
                }
                TFTVLogger.Always("Void Omens cleared");
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
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

        [HarmonyPatch(typeof(UIModuleGeoObjectives), "RefreshObjectives")]
        public static class UIModuleGeoObjectives_RefreshObjectives_Experiment_patch
        {
            public static void Postfix(UIModuleGeoObjectives __instance, IEnumerable<GeoFactionObjective> objectives)
            {
                try
                {
                    MethodInfo initObjectiveMethod = typeof(UIModuleGeoObjectives).GetMethod("InitObjective", BindingFlags.NonPublic | BindingFlags.Instance);
                    // Filter the objectives to get only the third type objectives
                    List<GeoFactionObjective> voidOmens = objectives
                        .Where(obj => obj.Title?.LocalizationKey?.Contains("VOID_OMEN") == true)
                        .ToList();

                    TFTVLogger.Always($"voidobjectives count is {voidOmens.Count()}");

                    // Find the last objective container and add the third type objectives after it
                    Transform lastObjectiveContainer = __instance.ObjectivesContainer.transform.GetChild(__instance.ObjectivesContainer.transform.childCount - 1);
                    GameObject thirdObjectivesContainer = new GameObject("ThirdObjectivesContainer");
                    thirdObjectivesContainer.transform.SetParent(__instance.ObjectivesContainer.transform);
                    thirdObjectivesContainer.transform.SetSiblingIndex(lastObjectiveContainer.GetSiblingIndex() + 1);
                    thirdObjectivesContainer.SetActive(true);

                    foreach (GeoObjectiveElementController geoObjectiveElementController in __instance.GetComponents<GeoObjectiveElementController>())
                    {
                        foreach (GeoFactionObjective geoFactionObjective in voidOmens)
                        {
                            if (geoObjectiveElementController.ObjectiveText.text == geoFactionObjective?.Title?.Localize())
                            {
                                GeoObjectiveElementController objectiveElement = UnityEngine.Object.Instantiate(geoObjectiveElementController, thirdObjectivesContainer.transform);
                                GeoObjectiveElementController objectiveController = objectiveElement.GetComponent<GeoObjectiveElementController>();
                                initObjectiveMethod.Invoke(__instance, new object[] { geoObjectiveElementController, geoFactionObjective });


                            }
                        }
                    }

                
                    thirdObjectivesContainer.SetActive(voidOmens.Any());
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        /* [HarmonyPatch(typeof(UIModuleGeoObjectives), "RefreshObjectives")]
         public static class UIModuleGeoObjectives_RefreshObjectives_Experiment_patch
         {
             public static void Postfix(UIModuleGeoObjectives __instance, IEnumerable<GeoFactionObjective> objectives)
             {
                 try
                 {
                     MethodInfo initObjectiveMethod = typeof(UIModuleGeoObjectives).GetMethod("InitObjective", BindingFlags.NonPublic | BindingFlags.Instance);
                     // Filter the objectives to get only the third type objectives
                     List<GeoFactionObjective> voidOmens = objectives
                         .Where(obj => obj.Title?.LocalizationKey?.Contains("VOID_OMEN") == true)
                         .ToList();

                     TFTVLogger.Always($"voidobjectives count is {voidOmens.Count()}");
                     // Create a new container for the third type of objectives
                     GameObject thirdObjectivesContainer = new GameObject("ThirdObjectivesContainer");
                     thirdObjectivesContainer.transform.SetParent(__instance.ObjectivesContainer.transform);
                     thirdObjectivesContainer.SetActive(true);

                     foreach (GeoObjectiveElementController geoObjectiveElementController in __instance.GetComponents<GeoObjectiveElementController>())
                     {
                         foreach (GeoFactionObjective geoFactionObjective in voidOmens)
                         {
                             if (geoObjectiveElementController.ObjectiveText.text == geoFactionObjective?.Title?.Localize())
                             {

                                 initObjectiveMethod.Invoke(__instance, new object[] { geoObjectiveElementController, geoFactionObjective });


                             }
                         }
                     }

                     thirdObjectivesContainer.SetActive(voidOmens.Any());


                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                 }

             }
         }


         [HarmonyPatch(typeof(UIModuleGeoObjectives), "InitObjective")]
         public static class UIModuleGeoObjectives_InitObjective_Experiment_patch
         {
             public static void Postfix(UIModuleGeoObjectives __instance, GeoObjectiveElementController element, GeoFactionObjective objective)
             {
                 try
                 {
                     TFTVLogger.Always($"Objective is {objective?.Title?.Localize()}");
                     TFTVLogger.Always($"Element text is {element?.ObjectiveText?.text}");



                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                 }

             }
         }*/

        [HarmonyPatch(typeof(FactionObjective), "GetCompletion")]
        public static class FactionObjective_GetCompletion_VO4_Patch
        {
            public static void Postfix(ref float __result)
            {
                try
                {
                    /*
                    TFTVLogger.Always("GetCompletion experience is " + __result);
                    if (TFTVRevenantResearch.RevenantPoints == 10)
                    {
                        __result += 3;
                        TFTVLogger.Always("300 awarded for killing Revenant; result is now " + __result);
                    }
                    else if (TFTVRevenantResearch.RevenantPoints == 5)
                    {
                        __result += 2;
                        TFTVLogger.Always("200 awarded for killing Revenant; result is now " + __result);
                    }
                    else if (TFTVRevenantResearch.RevenantPoints == 1)
                    {
                        __result += 1;
                        TFTVLogger.Always("100 awarded for killing Revenant; result is now " + __result);
                    }*/

                    if (VoidOmensCheck[4])
                    {
                        __result *= 2;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }

        }

        public static void VO5TurnHostileCivviesFriendly(TacticalLevelController controller)
        {
            try
            {

                if (VoidOmensCheck[5])
                {
                    TacMissionTypeDef MissionType = controller.TacticalGameParams.MissionData.MissionType;

                    GameTagDef containsCivvies = DefCache.GetDef<GameTagDef>("Contains_Civilians_MissionTagDef");
                    GameTagDef havenDefenseMIssion = DefCache.GetDef<GameTagDef>("MissionTypeHavenDefense_MissionTagDef");

                    if (MissionType.Tags.Contains(containsCivvies) && MissionType.Tags.Contains(havenDefenseMIssion))
                    {
                        TFTVLogger.Always("Haven defense mission with VO5 in play; turning civvies friendly");
                        GameTagDef civvieTag = DefCache.GetDef<GameTagDef>("Civilian_ClassTagDef");
                        TacticalFaction environment = controller.GetFactionByCommandName("env");
                        // TFTVLogger.Always("Tag declared successfully");

                        foreach (TacticalFaction faction in controller.Factions)
                        {
                            if (faction.ParticipantKind == TacMissionParticipant.Residents)
                            {
                                TFTVLogger.Always("Found residents faction");

                                foreach (TacticalActorBase tacticalActorBase in faction.Actors)
                                {
                                    if (tacticalActorBase is TacticalActor)
                                    {
                                        TacticalActor tacticalActor = tacticalActorBase as TacticalActor;
                                        if (tacticalActor.GameTags.Contains(civvieTag))
                                        {
                                            TFTVLogger.Always("Found civvy");
                                            tacticalActor.SetFaction(environment, TacMissionParticipant.Environment);


                                        }
                                    }
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


        /*  [HarmonyPatch(typeof(TacticalLevelController), "GameOver")]
          public static class TacticalLevelController_GameOver_HostileDefenders_Patch
          {
              public static void Prefix(TacticalLevelController __instance)
              {
                  try
                  {
                      if (VoidOmensCheck[5])
                      {

                          TacticalFaction phoenix = __instance.GetFactionByCommandName("PX");
                          TacticalFaction intruderFaction = new TacticalFaction();
                          string MissionType = __instance.TacticalGameParams.MissionData.MissionType.SaveDefaultName;
                          int countEnemies = 0;
                          if (MissionType == "HavenDefense")
                          {
                              foreach (TacticalFaction faction in __instance.Factions)
                              {
                                  if (faction.ParticipantKind == TacMissionParticipant.Intruder)
                                  {
                                      // TFTVLogger.Always("The faction is " + faction.TacticalFactionDef.name);
                                      intruderFaction = faction;

                                      foreach (TacticalActorBase enemy in faction.Actors)
                                      {
                                          //  TFTVLogger.Always("Checking each enemy " + enemy.name);
                                          TacticalActor tacticalActor = enemy as TacticalActor;

                                          if (enemy.IsAlive && !tacticalActor.IsEvacuated && tacticalActor.Status.GetStatus<ParalysedStatus>(DefCache.GetDef<ParalysedStatusDef>("Paralysed_StatusDef")) == null)
                                          {
                                              TFTVLogger.Always("This enemy is alive and not offmap " + enemy.DisplayName);
                                              countEnemies++;
                                          }
                                      }
                                      TFTVLogger.Always("There are " + countEnemies + " enemies alive");
                                  }
                              }

                              if (countEnemies == 0 && intruderFaction != null)
                              {
                                  intruderFaction.State = TacFactionState.Defeated;

                                  foreach (TacticalFaction tacticalFaction in __instance.Factions)
                                  {
                                      if (tacticalFaction.GetRelationTo(phoenix) == FactionRelation.Enemy && tacticalFaction.ParticipantKind != TacMissionParticipant.Intruder)
                                      {
                                          tacticalFaction.ParticipantKind = TacMissionParticipant.Player;
                                          tacticalFaction.State = TacFactionState.Playing;
                                      }
                                  }
                                  phoenix.State = TacFactionState.Won;


                                  TFTVLogger.Always("Got here, GameOver method invoked");
                              }
                          }
                      }
                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                  }
              }
          }*/


        [HarmonyPatch(typeof(TacticalAbility), "get_WillPointCost")]
        public static class TacticalAbility_get_WillPointCost_VoidOmenExtraWPCost_Patch
        {
            public static void Postfix(ref float __result, TacticalAbility __instance)
            {
                try
                {
                    if (__result > 0)
                    {
                        if (VoidOmensCheck[3] && __instance.TacticalActor != null)
                        {
                            if (__instance.TacticalActor.IsControlledByPlayer)
                            {
                                __result += Mathf.RoundToInt(__result * 0.5f);
                                //TFTVLogger.Always("WP cost increased to " + __result);
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

        [HarmonyPatch(typeof(TacticalVoxelMatrix), "SpawnAndPropagateMist")]
        public static class TacticalVoxelMatrix_SpawnAndPropagateMist_VoidOmenMoreMistOnTactical_Patch
        {
            public static bool Prefix(TacticalVoxelMatrix __instance)
            {
                try
                {
                    TFTVConfig config = TFTVMain.Main.Config;
                    int difficultyLevel = __instance.TacticalLevel.Difficulty.Order;


                    if (VoidOmensCheck[7] && config.MoreMistVO)
                    {

                        float missionTypeModifer = 1;
                        string saveDefaultName = __instance.TacticalLevel.TacMission.MissionData.MissionType.SaveDefaultName;

                        if (saveDefaultName.Contains("Nest") || saveDefaultName.Contains("Lair"))
                        {
                            missionTypeModifer = 0.25f;
                        }
                        else if (saveDefaultName.Contains("Citadel"))
                        {
                            missionTypeModifer = 0.5f;
                        }

                        __instance.VoxelMatrixData.InitialMistEntitiesToSpawn.Min = 0 + difficultyLevel * (int)(7 * missionTypeModifer);
                        __instance.VoxelMatrixData.InitialMistEntitiesToSpawn.Max = 10 + difficultyLevel * (int)(7 * missionTypeModifer);
                        return true;
                    }
                    else
                    {
                        __instance.VoxelMatrixData.InitialMistEntitiesToSpawn.Min = 1;
                        __instance.VoxelMatrixData.InitialMistEntitiesToSpawn.Max = 3;
                        return true;
                    }



                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }
            }
        }


        [HarmonyPatch(typeof(UIStateRosterDeployment), "get__squadMaxDeployment")]
        public static class UIStateRosterDeployment_get_SquadMaxDeployment_VoidOmenLimitedDeployment_Patch
        {
            public static void Postfix(ref int __result, UIStateRosterDeployment __instance)
            {
                try
                {
                    TFTVConfig config = TFTVMain.Main.Config;

                    if (VoidOmensCheck[7] && __instance.Mission.MissionDef.MaxPlayerUnits == 8 && config.MoreMistVO)
                    {
                        __result += 1;
                    }
                    if (VoidOmensCheck[4])
                    {
                        __result -= 2;
                    }
                    if (__instance.Mission.MissionDef.name.Equals("StoryFS2_CustomMissionTypeDef"))
                    {
                        __result = 8;
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
            private static readonly SharedData sharedData = GameUtl.GameComponent<SharedData>();

            public static void Prefix(ref HavenAttacker attacker)
            {
                try
                {

                    if (VoidOmensCheck[12])
                    {
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



        [HarmonyPatch(typeof(GeoHavenDefenseMission), "GetDefenseDeployment")]
        public static class GeoHavenDefenseMission_GetDefenseDeployment_Mobilization_Patch
        {
            public static bool Prefix(GeoHaven haven, ref int __result)
            {
                try
                {
                    if (VoidOmensCheck[18])
                    {
                        __result = (int)(((float)haven.ZonesStats.GetTotalHavenOutput().Deployment * 1.5 * haven.Site.Owner.FactionStatModifiers?.HavenDefenseModifier) ?? 1f);
                        return false;
                    }

                    return true;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
                return true;
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




        //VO5 increase chance to spawn weapons in crates
        [HarmonyPatch(typeof(GeoMission), "PrepareTacticalGame")]
        public static class TFTV_GeoMission_ModifyCratesVO5_Patch
        {

            public static void Prefix(GeoMission __instance, ref List<WeaponDef> __state)
            {
                try
                {

                    if (VoidOmensCheck[5] && __instance.MissionDef.name.Contains("HavenDef"))
                    {
                        __state = new List<WeaponDef>();

                        foreach (WeaponDef weapon in Repo.GetAllDefs<WeaponDef>())
                        {

                            if ((weapon.name.Contains("AN_") || weapon.name.Contains("NJ_") || weapon.name.Contains("SY_"))
                                && !weapon.name.Contains("Grenade") && !weapon.name.Contains("Berserker") && !weapon.name.Contains("Armadillo")
                            && !weapon.name.Contains("Aspida"))
                            {
                                __state.Add(weapon);
                                weapon.CrateSpawnWeight *= 50;
                            }

                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            public static void Postfix(GeoMission __instance, List<WeaponDef> __state)
            {
                try
                {
                    if (VoidOmensCheck[5] && __instance.MissionDef.name.Contains("HavenDef"))
                    {
                        foreach (WeaponDef weapon in __state)
                        {
                            weapon.CrateSpawnWeight /= 50;
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        //Adjusted for all haven defenses because parapsychosis bug. Check if necessary for other WipeEnemyFactionObjective missions
        [HarmonyPatch(typeof(WipeEnemyFactionObjective), "EvaluateObjective")]
        public static class TFTV_HavenDefendersHostileFactionObjective_EvaluateObjective_Patch
        {
            public static bool Prefix(FactionObjective __instance, ref FactionObjectiveState __result,
                List<TacticalFaction> ____enemyFactions, List<TacticalFactionDef> ____overrideFactions, bool ____ignoreDeployment)
            {
                try
                {
                    //  TFTVLogger.Always($"evaluating {__instance.GetDescription()} and the result is {__result}");

                    //   if (VoidOmensCheck[5])
                    //   {
                    TacticalLevelController controller = __instance.Level;
                    string MissionType = controller.TacticalGameParams.MissionData.MissionType.SaveDefaultName;

                    if (MissionType == "HavenDefense")
                    {
                        if (!__instance.IsUiHidden)
                        {

                            //  TFTVLogger.Always("WipeEnemyFactionObjetive invoked");

                            if (!__instance.Faction.HasTacActorsThatCanWin() && !__instance.Faction.HasUndeployedTacActors())
                            {
                                __result = FactionObjectiveState.Failed;
                                //  TFTVLogger.Always("WipeEnemyFactionObjetive failed");
                                return false; // skip original method
                            }

                            foreach (TacticalFaction enemyFaction in controller.Factions)
                            {
                                if (enemyFaction.ParticipantKind == TacMissionParticipant.Intruder)
                                {
                                    // TFTVLogger.Always("The faction is " + faction.TacticalFactionDef.name);
                                    if (!enemyFaction.HasTacActorsThatCanWin())
                                    {
                                        //  TFTVLogger.Always("HavenDefense, no intruders alive, so mission should be a win");
                                        __result = FactionObjectiveState.Achieved;
                                        return false;
                                    }

                                }
                            }


                        }
                        return true;
                    }
                    return true;
                    //  }
                    //  return true;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }
        //Patch to set VO objective test in uppercase to match other objectives
        [HarmonyPatch(typeof(ObjectivesManager), "Add")]
        public static class FactionObjective_ModifyObjectiveColor_Patch
        {

            public static void Postfix(ObjectivesManager __instance, FactionObjective objective)
            {
                try
                {
                    //  TFTVLogger.Always("FactionObjective Invoked");
                    if (objective.Description.LocalizationKey.Contains("VOID"))
                    {
                        objective.Description = new LocalizedTextBind(objective.Description.Localize().ToUpper(), true);
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        //Patch to avoid triggering "failed" state for VO objectives when player loses a character
        [HarmonyPatch(typeof(KeepSoldiersAliveFactionObjective), "EvaluateObjective")]
        public static class KeepSoldiersAliveFactionObjective_EvaluateObjective_Patch
        {

            public static void Postfix(KeepSoldiersAliveFactionObjective __instance, ref FactionObjectiveState __result)
            {
                try
                {
                    KeepSoldiersAliveFactionObjectiveDef VOID_OMEN_TITLE_3 = DefCache.GetDef<KeepSoldiersAliveFactionObjectiveDef>("VOID_OMEN_TITLE_3");
                    KeepSoldiersAliveFactionObjectiveDef VOID_OMEN_TITLE_5 = DefCache.GetDef<KeepSoldiersAliveFactionObjectiveDef>("VOID_OMEN_TITLE_5");
                    KeepSoldiersAliveFactionObjectiveDef VOID_OMEN_TITLE_7 = DefCache.GetDef<KeepSoldiersAliveFactionObjectiveDef>("VOID_OMEN_TITLE_7");
                    KeepSoldiersAliveFactionObjectiveDef VOID_OMEN_TITLE_10 = DefCache.GetDef<KeepSoldiersAliveFactionObjectiveDef>("VOID_OMEN_TITLE_10");
                    KeepSoldiersAliveFactionObjectiveDef VOID_OMEN_TITLE_15 = DefCache.GetDef<KeepSoldiersAliveFactionObjectiveDef>("VOID_OMEN_TITLE_15");
                    KeepSoldiersAliveFactionObjectiveDef VOID_OMEN_TITLE_16 = DefCache.GetDef<KeepSoldiersAliveFactionObjectiveDef>("VOID_OMEN_TITLE_16");
                    KeepSoldiersAliveFactionObjectiveDef VOID_OMEN_TITLE_19 = DefCache.GetDef<KeepSoldiersAliveFactionObjectiveDef>("VOID_OMEN_TITLE_19");

                    List<KeepSoldiersAliveFactionObjectiveDef> voidOmens = new List<KeepSoldiersAliveFactionObjectiveDef> { VOID_OMEN_TITLE_3, VOID_OMEN_TITLE_5, VOID_OMEN_TITLE_7, VOID_OMEN_TITLE_10, VOID_OMEN_TITLE_15, VOID_OMEN_TITLE_16, VOID_OMEN_TITLE_19 };

                    //  TFTVLogger.Always("FactionObjective Evaluate " + __instance.Description.ToString());
                    foreach (KeepSoldiersAliveFactionObjectiveDef keepSoldiersAliveFactionObjectiveDef in voidOmens)
                    {
                        // TFTVLogger.Always(keepSoldiersAliveFactionObjectiveDef.MissionObjectiveData.Description.LocalizeEnglish());
                        // TFTVLogger.Always(__instance.Description.LocalizationKey);

                        if (keepSoldiersAliveFactionObjectiveDef.MissionObjectiveData.Description.LocalizeEnglish().ToUpper() == __instance.Description.LocalizationKey)
                        {
                            // TFTVLogger.Always("FactionObjective check passed");
                            __result = FactionObjectiveState.InProgress;
                        }

                    }


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }






        /* public static void CheckHostileDefendersVO(TacticalLevelController controller)
         {
             try 
             {
                 if (VoidOmensCheck[5] && controller.TacMission.MissionData.MissionType.SaveDefaultName.Contains("HavenDefense"))
                 {
                     object context = controller.GetFactionByCommandName("PX").Actors.First();
                     TacContextHelpManager tacContextHelpManager = (TacContextHelpManager)UnityEngine.Object.FindObjectOfType(typeof(TacContextHelpManager));
                     tacContextHelpManager.EventTypeTriggered(HintTrigger.Manual, context, context);
                 }

             }
             catch (Exception e)
             {
                 TFTVLogger.Error(e);
             }

         }*/

    }
}
