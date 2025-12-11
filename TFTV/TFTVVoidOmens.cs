using Base;
using Base.Defs;
using Base.Entities.Effects;
using Base.Levels;
using Base.UI;
using com.ootii.Helpers;
using Epic.OnlineServices;
using HarmonyLib;
using PhoenixPoint.Common.ContextHelp;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Missions;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.Levels.Objectives;
using PhoenixPoint.Geoscape.View.ViewStates;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.FactionEffects;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using PhoenixPoint.Tactical.Levels.Mist;
using PhoenixPoint.Tactical.View;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV
{
    internal class TFTVVoidOmens
    {
        private static readonly TFTVConfig Config = new TFTVConfig();
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData sharedData = TFTVMain.Shared;

        private static readonly FesteringSkiesSettingsDef festeringSkiesSettingsDef = DefCache.GetDef<FesteringSkiesSettingsDef>("FesteringSkiesSettingsDef");
        private static readonly TacticalPerceptionDef tacticalPerceptionDef = DefCache.GetDef<TacticalPerceptionDef>("Soldier_PerceptionDef");
        private static readonly TacCrateDataDef cratesNotResources = DefCache.GetDef<TacCrateDataDef>("Default_TacCrateDataDef");
        private static readonly TacticalFactionEffectDef defendersCanBeRecruited = DefCache.GetDef<TacticalFactionEffectDef>("CanBeRecruitedByPhoenix_FactionEffectDef");
        private static readonly GeoHavenZoneDef havenLab = DefCache.GetDef<GeoHavenZoneDef>("Research_GeoHavenZoneDef");

        private static readonly List<CustomMissionTypeDef> _ambushMissions = new List<CustomMissionTypeDef>()
        {DefCache.GetDef<CustomMissionTypeDef>("AmbushFallen_CustomMissionTypeDef"),
            DefCache.GetDef<CustomMissionTypeDef>("AmbushAlien_CustomMissionTypeDef"),
            DefCache.GetDef<CustomMissionTypeDef>("AmbushAN_CustomMissionTypeDef"),
        DefCache.GetDef<CustomMissionTypeDef>("AmbushBandits_CustomMissionTypeDef"),
        DefCache.GetDef<CustomMissionTypeDef>("AmbushNJ_CustomMissionTypeDef"),
        DefCache.GetDef<CustomMissionTypeDef>("AmbushPure_CustomMissionTypeDef"),
        DefCache.GetDef<CustomMissionTypeDef>("AmbushAlien_CustomMissionTypeDef"),
        DefCache.GetDef<CustomMissionTypeDef>("AmbushSY_CustomMissionTypeDef")};

        private static readonly PartyDiplomacySettingsDef partyDiplomacySettingsDef = DefCache.GetDef<PartyDiplomacySettingsDef>("PartyDiplomacySettingsDef");

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


       
      
        public static void ImplementHavenDefendersAlwaysHostile(TacticalLevelController controller)
        {
            try
            {
                if (controller.TacMission.MissionData.MissionType.MissionTypeTag != sharedData.SharedGameTags.HavenDefenseMissionTag)
                {
                    return;
                }

                if (VoidOmensCheck[5])
                {
                    TFTVLogger.Always("Haven defenders always hostile, but crates available for looting");

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
                }
                else
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
                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        public static void ImplementVoidOmens(GeoLevelController controller)
        {
            try
            {
                // Snapshot current VOs in play
                int[] inPlayArray = CheckFordVoidOmensInPlay(controller);
                var inPlay = new HashSet<int>(inPlayArray.Where(vo => vo != 0));

                // Helper: toggle VO check flag
                void SetVoFlag(int vo, bool active)
                {
                    VoidOmensCheck[vo] = active;
                    TFTVLogger.Always($"The check for VO#{vo} went ok");
                }

                // Helper: apply ambush mission settings
                void SetAmbushMissions(float reinfPart, int reinfTurns, int cratesMin, int cratesMax)
                {
                    foreach (CustomMissionTypeDef ambushMission in _ambushMissions)
                    {
                        ambushMission.ParticipantsData[0].ReinforcementsDeploymentPart.Max = reinfPart;
                        ambushMission.ParticipantsData[0].ReinforcementsDeploymentPart.Min = reinfPart;
                        ambushMission.ParticipantsData[0].ReinforcementsTurns.Max = reinfTurns;
                        ambushMission.ParticipantsData[0].ReinforcementsTurns.Min = reinfTurns;
                        ambushMission.CratesDeploymentPointsRange.Min = cratesMin;
                        ambushMission.CratesDeploymentPointsRange.Max = cratesMax;
                    }
                }

                // Helper: toggle haven defenders hostility and crates/hints
                void ToggleHavenDefenders(bool hostile)
                {
                    ContextHelpHintDbDef hintsDb = DefCache.GetDef<ContextHelpHintDbDef>("AlwaysDisplayedTacticalHintsDbDef");
                    ContextHelpHintDef hostileDefenders = DefCache.GetDef<ContextHelpHintDef>("HostileDefenders");

                    if (hostile)
                    {
                        if (!hintsDb.Hints.Contains(hostileDefenders))
                        {
                            hintsDb.Hints.Add(hostileDefenders);
                        }
                    }
                    else
                    {
                        if (hintsDb.Hints.Contains(hostileDefenders))
                        {
                            hintsDb.Hints.Remove(hostileDefenders);
                        }
                    }

                    foreach (CustomMissionTypeDef missionTypeDef in Repo.GetAllDefs<CustomMissionTypeDef>())
                    {
                        if (!missionTypeDef.name.Contains("Haven") || missionTypeDef.name.Contains("Infestation"))
                        {
                            continue;
                        }

                        // Civ missions have resident index 1, others 2
                        int relationIndex = missionTypeDef.name.Contains("Civ") ? 1 : 2;
                        missionTypeDef.ParticipantsRelations[relationIndex].MutualRelation = hostile ? FactionRelation.Enemy : FactionRelation.Friend;

                        if (hostile)
                        {
                            missionTypeDef.ParticipantsData[1].PredeterminedFactionEffects = missionTypeDef.ParticipantsData[0].PredeterminedFactionEffects;
                            missionTypeDef.MissionSpecificCrates = cratesNotResources;
                            missionTypeDef.FactionItemsRange.Min = 2;
                            missionTypeDef.FactionItemsRange.Max = 7;
                            missionTypeDef.CratesDeploymentPointsRange.Min = 20;
                            missionTypeDef.CratesDeploymentPointsRange.Max = 30;
                            missionTypeDef.DontRecoverItems = true;
                        }
                        else
                        {
                            missionTypeDef.ParticipantsData[1].PredeterminedFactionEffects = new EffectDef[] { defendersCanBeRecruited };
                            missionTypeDef.FactionItemsRange.Min = 0;
                            missionTypeDef.FactionItemsRange.Max = 0;
                            missionTypeDef.CratesDeploymentPointsRange.Min = 0;
                            missionTypeDef.CratesDeploymentPointsRange.Max = 0;
                            missionTypeDef.DontRecoverItems = false;
                        }
                    }
                }

                // VO1: Harder ambushes
                if (inPlay.Contains(1))
                {
                    SetAmbushMissions(0.2f, 1, 50, 70);
                    VoidOmensCheck[1] = true;
                }
                else if (VoidOmensCheck[1])
                {
                    SetAmbushMissions(0.3f, 3, 30, 50);
                    SetVoFlag(1, false);
                }

                // VO2: Halved diplomatic penalties/rewards
                if (inPlay.Contains(2))
                {
                    partyDiplomacySettingsDef.InfiltrationFactionMultiplier = 0.5f;
                    partyDiplomacySettingsDef.InfiltrationLeaderMultiplier = 0.75f;
                    VoidOmensCheck[2] = true;
                }
                else if (VoidOmensCheck[2])
                {
                    partyDiplomacySettingsDef.InfiltrationFactionMultiplier = 1f;
                    partyDiplomacySettingsDef.InfiltrationLeaderMultiplier = 1.5f;
                    SetVoFlag(2, false);
                }

                // VO3: WP cost +50%
                VoidOmensCheck[3] = inPlay.Contains(3);
                if (!inPlay.Contains(3) && VoidOmensCheck[3]) SetVoFlag(3, false);

                // VO4: Limited deployment (xp handled elsewhere)
                VoidOmensCheck[4] = inPlay.Contains(4);
                if (!inPlay.Contains(4) && VoidOmensCheck[4]) SetVoFlag(4, false);

                // VO5: Haven defenders hostile and crates available
                if (inPlay.Contains(5))
                {
                    ToggleHavenDefenders(true);
                    VoidOmensCheck[5] = true;
                }
                else if (VoidOmensCheck[5])
                {
                    ToggleHavenDefenders(false);
                    SetVoFlag(5, false);
                }

                // VO6: Reactive evolution and research boost
                if (inPlay.Contains(6))
                {
                    controller.CurrentDifficultyLevel.EvolutionPointsGainOnMissionLoss = 20;
                    controller.CurrentDifficultyLevel.AlienBaseTypeEvolutionParams[0].EvolutionPerDestroyedBase = 30;
                    controller.CurrentDifficultyLevel.AlienBaseTypeEvolutionParams[1].EvolutionPerDestroyedBase = 60;
                    controller.CurrentDifficultyLevel.AlienBaseTypeEvolutionParams[2].EvolutionPerDestroyedBase = 90;
                    DefCache.GetDef<GeoAlienFactionDef>("Alien_GeoAlienFactionDef").ProgressEvolutionWhenAlienMissionIsWon = true;
                    VoidOmensCheck[6] = true;
                    controller.PhoenixFaction.Research.Update();
                   
                }
                else if (VoidOmensCheck[6])
                {
                    controller.CurrentDifficultyLevel.EvolutionPointsGainOnMissionLoss = 0;
                    controller.CurrentDifficultyLevel.AlienBaseTypeEvolutionParams[0].EvolutionPerDestroyedBase = 0;
                    controller.CurrentDifficultyLevel.AlienBaseTypeEvolutionParams[1].EvolutionPerDestroyedBase = 0;
                    controller.CurrentDifficultyLevel.AlienBaseTypeEvolutionParams[2].EvolutionPerDestroyedBase = 0;
                    DefCache.GetDef<GeoAlienFactionDef>("Alien_GeoAlienFactionDef").ProgressEvolutionWhenAlienMissionIsWon = false;
                    SetVoFlag(6, false);
                }

                // VO7: More mist (tactical patch reads VoidOmensCheck)
                VoidOmensCheck[7] = inPlay.Contains(7);
                if (!inPlay.Contains(7) && VoidOmensCheck[7]) SetVoFlag(7, false);

                // VO8: Haven lab provides more research
                if (inPlay.Contains(8))
                {
                    havenLab.ProvidesResearch = 2;
                    VoidOmensCheck[8] = true;
                }
                else if (VoidOmensCheck[8])
                {
                    havenLab.ProvidesResearch = 1;
                    SetVoFlag(8, false);
                }

                // VO9: Adjust FS haven attack counter
                if (inPlay.Contains(9))
                {
                    festeringSkiesSettingsDef.HavenAttackCounterModifier = 0.66f;
                    VoidOmensCheck[9] = true;
                }
                else if (VoidOmensCheck[9])
                {
                    festeringSkiesSettingsDef.HavenAttackCounterModifier = 1.33f;
                    SetVoFlag(9, false);
                }

                // VO10: No delirium limit
                VoidOmensCheck[10] = inPlay.Contains(10);
                if (!inPlay.Contains(10) && VoidOmensCheck[10]) SetVoFlag(10, false);

                // VO11: Behemoth roams more (forcing UpdateHourly both ways)
                if (inPlay.Contains(11))
                {
                    VoidOmensCheck[11] = true;
                    controller.AlienFaction.Behemoth?.UpdateHourly();
                }
                else if (VoidOmensCheck[11])
                {
                    VoidOmensCheck[11] = false;
                    controller.AlienFaction.Behemoth?.UpdateHourly();
                   
                }

                // VO12: Stronger haven defense attackers (applied elsewhere)
                VoidOmensCheck[12] = inPlay.Contains(12);
                if (!inPlay.Contains(12) && VoidOmensCheck[12]) SetVoFlag(12, false);

                // VO13: Faster alien base construction
                if (inPlay.Contains(13))
                {
                    controller.CurrentDifficultyLevel.NestLimitations.HoursBuildTime = 45;
                    controller.CurrentDifficultyLevel.LairLimitations.HoursBuildTime = 50;
                    controller.CurrentDifficultyLevel.CitadelLimitations.HoursBuildTime = 90;
                    VoidOmensCheck[13] = true;
                }
                else if (VoidOmensCheck[13])
                {
                    if (controller.CurrentDifficultyLevel.Order > 5)
                    {
                        controller.CurrentDifficultyLevel.NestLimitations.HoursBuildTime = 73;
                        controller.CurrentDifficultyLevel.LairLimitations.HoursBuildTime = 80;
                        controller.CurrentDifficultyLevel.CitadelLimitations.HoursBuildTime = 144;
                    }
                    else
                    {
                        controller.CurrentDifficultyLevel.NestLimitations.HoursBuildTime = 90;
                        controller.CurrentDifficultyLevel.LairLimitations.HoursBuildTime = 100;
                        controller.CurrentDifficultyLevel.CitadelLimitations.HoursBuildTime = 180;
                    }
                    SetVoFlag(13, false);
                    TFTVLogger.Always("The check for VO#13 went ok so Pandoran nests take " + controller.CurrentDifficultyLevel.NestLimitations.HoursBuildTime + " hours");
                }

                // VO14: Reduced perception range
                if (inPlay.Contains(14))
                {
                    tacticalPerceptionDef.PerceptionRange = 20;
                    VoidOmensCheck[14] = true;
                }
                else if (VoidOmensCheck[14])
                {
                    tacticalPerceptionDef.PerceptionRange = 30;
                    SetVoFlag(14, false);
                }

                // VO15: More Umbra
                VoidOmensCheck[15] = inPlay.Contains(15);
                if (!inPlay.Contains(15) && VoidOmensCheck[15]) SetVoFlag(15, false);

                // VO16: Umbras anywhere
                VoidOmensCheck[16] = inPlay.Contains(16);
                if (!inPlay.Contains(16) && VoidOmensCheck[16]) SetVoFlag(16, false);

                // VO17: Extra global condition (used elsewhere)
                VoidOmensCheck[17] = inPlay.Contains(17);
                if (!inPlay.Contains(17) && VoidOmensCheck[17]) SetVoFlag(17, false);

                // VO18: Extra defense points, less rewards (applied in patch)
                VoidOmensCheck[18] = inPlay.Contains(18);
                if (!inPlay.Contains(18) && VoidOmensCheck[18]) SetVoFlag(18, false);

                // VO19: Reactive evolution price changes (currently toggles a flag)
                VoidOmensCheck[19] = inPlay.Contains(19);
                if (!inPlay.Contains(19) && VoidOmensCheck[19]) SetVoFlag(19, false);

                // Logging and bookkeeping preserved
                var voidOmensInPlayLog = new List<int>(inPlayArray);
                var alreadyRolledLog = CheckForAlreadyRolledVoidOmens(controller).ToList();

                foreach (int vo in voidOmensInPlayLog)
                {
                    TFTVLogger.Always("Void Omen " + vo + " is in play");
                }

                foreach (int vo in alreadyRolledLog)
                {
                    TFTVLogger.Always("Void Omen " + vo + " rolled at some point");
                }

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

                TFTVLogger.Always("List of already rolled Void Omens cleared");
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
                int difficulty = Math.Max(geoLevelController.CurrentDifficultyLevel.Order - 1, 1);
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

        public static string RemoveEarliestVoidOmen(GeoLevelController geoLevelController, int reason)
        {
            try
            {
                if (CheckFordVoidOmensInPlay(geoLevelController).Any(vo => vo != 0))
                {

                    string triggeredVoidOmensString = "TriggeredVoidOmen_";
                    string voidOmenTitleString = "VOID_OMEN_TITLE_";
                    string voidOmenString = "VoidOmen_";
                    int difficulty = Math.Max(geoLevelController.CurrentDifficultyLevel.Order - 1, 1);

                    GeoFactionObjective earliestVO = geoLevelController.PhoenixFaction.Objectives?.FirstOrDefault(o => o.Title.LocalizationKey.Contains(voidOmenTitleString));

                    //    TFTVLogger.Always($" 2");

                    if (earliestVO != null)
                    {
                        string voidOmenTitleLocKey = earliestVO?.Title?.LocalizationKey;
                        int voidOmen = int.Parse(voidOmenTitleLocKey.Substring(voidOmenTitleString.Length));
                        TFTVLogger.Always($"The earliest VO is {voidOmenTitleLocKey}, {voidOmen}");

                        int[] voidOmensinPlay = CheckFordVoidOmensInPlay(geoLevelController);

                        for (int x = 0; x < voidOmensinPlay.Count(); x++)
                        {
                            if (voidOmensinPlay[x] == voidOmen)
                            {
                                int voidOmenSlot = x + 1;
                                TFTVLogger.Always($"vo slot {voidOmenSlot} emptied");

                                geoLevelController.EventSystem.SetVariable(voidOmenString + voidOmenSlot, 0);

                                if (!CheckForAlreadyRolledVoidOmens(geoLevelController).Contains(voidOmen))
                                {
                                    for (int y = 1; x < 100; x++)
                                    {
                                        if (geoLevelController.EventSystem.GetVariable(triggeredVoidOmensString + y) == 0)
                                        {
                                            geoLevelController.EventSystem.SetVariable(triggeredVoidOmensString + y, voidOmen);
                                            TFTVLogger.Always($"Recording that this VO has already rolled before");
                                            break;
                                        }
                                    }
                                }

                                TFTVLogger.Always($" VO {voidOmen} will be removed ");
                                RemoveVoidOmenObjective(voidOmenTitleLocKey, geoLevelController);

                            }
                        }
                        ImplementVoidOmens(geoLevelController);
                        /*   string explanation =
                               $"{TFTVCommonMethods.ConvertKeyToString("KEY_VOID_OMEN_REMOVED"+reason)}" +
                               $"{TFTVCommonMethods.ConvertKeyToString("KEY_VOID_OMEN_REMOVED_TEXT0")} " +
                               $"<i>{TFTVCommonMethods.ConvertKeyToString(voidOmenTitleLocKey)}</i> " +
                               $"{TFTVCommonMethods.ConvertKeyToString("KEY_VOID_OMEN_REMOVED_TEXT1")}";

                           if (reason == 1) 
                           {
                               explanation = $"{TFTVCommonMethods.ConvertKeyToString("KEY_VOID_OMEN_REMOVED_TEXT0")} " +
                               $"<i>{TFTVCommonMethods.ConvertKeyToString(voidOmenTitleLocKey)}</i> " +
                               $"{TFTVCommonMethods.ConvertKeyToString("KEY_VOID_OMEN_REMOVED_TEXT1")}";
                           }


                           GameUtl.GetMessageBox().ShowSimplePrompt(explanation, MessageBoxIcon.None, MessageBoxButtons.OK, null);*/
                        return voidOmenTitleLocKey;
                    }
                }

                else
                {
                    TFTVLogger.Always($"Wanted to remove earliest Void Omen, but failed to find it!");

                }
                return null;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        public static void RemoveAllVoidOmens(GeoLevelController geoLevelController)
        {
            try
            {


                string triggeredVoidOmensString = "TriggeredVoidOmen_";
                string voidOmenTitleString = "VOID_OMEN_TITLE_";
                string voidOmenString = "VoidOmen_";
                int[] voidOmensinPlay = CheckFordVoidOmensInPlay(geoLevelController);
                int difficulty = Math.Max(geoLevelController.CurrentDifficultyLevel.Order - 1, 1);

                List<GeoFactionObjective> allVoidOmenObjectives = FindVoidOmenObjectives(geoLevelController);

                // TFTVLogger.Always($"");

                if (allVoidOmenObjectives.Count > 0)
                {
                    foreach (GeoFactionObjective voObjective in allVoidOmenObjectives)
                    {
                        string voidOmenTitleLocKey = voObjective?.Title?.LocalizationKey;
                        TFTVLogger.Always($"voidOmenTitleLocKey is {voidOmenTitleLocKey}");
                        int voidOmen = int.Parse(voidOmenTitleLocKey.Substring(voidOmenTitleString.Length));
                        TFTVLogger.Always($"VO is {voidOmenTitleString}, {voidOmen}");

                        for (int x = 0; x < voidOmensinPlay.Count(); x++)
                        {
                            if (voidOmensinPlay[x] == voidOmen)
                            {
                                int voidOmenSlot = x + 1;
                                TFTVLogger.Always($"vo slot {voidOmenSlot} emptied");

                                geoLevelController.EventSystem.SetVariable(voidOmenString + voidOmenSlot, 0);

                                if (!CheckForAlreadyRolledVoidOmens(geoLevelController).Contains(voidOmen))
                                {
                                    for (int y = 1; x < 100; x++)
                                    {
                                        if (geoLevelController.EventSystem.GetVariable(triggeredVoidOmensString + y) == 0)
                                        {
                                            geoLevelController.EventSystem.SetVariable(triggeredVoidOmensString + y, voidOmen);
                                            TFTVLogger.Always($"Recording that this VO has already rolled before");
                                            break;
                                        }
                                    }
                                }

                                TFTVLogger.Always($" VO {voidOmen} will be removed ");
                                RemoveVoidOmenObjective(voidOmenTitleLocKey, geoLevelController);
                            }
                        }
                        /*   string explanation =
                             $"{TFTVCommonMethods.ConvertKeyToString("KEY_VOID_OMEN_REMOVED_BEHEMOTH")}";

                           GameUtl.GetMessageBox().ShowSimplePrompt(explanation, MessageBoxIcon.None, MessageBoxButtons.OK, null);*/

                    }

                    ImplementVoidOmens(geoLevelController);
                }






                /*   int difficulty = geoLevelController.CurrentDifficultyLevel.Order;
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
                   }*/
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

        public static List<GeoFactionObjective> FindVoidOmenObjectives(GeoLevelController level)
        {
            try
            {
                string voidOmenTitleString = "VOID_OMEN_TITLE_";

                List<GeoFactionObjective> listOfObjectives = level.PhoenixFaction.Objectives.ToList();

                List<GeoFactionObjective> voidOmens = new List<GeoFactionObjective>();

                foreach (GeoFactionObjective objective in listOfObjectives)
                {
                    if (objective.Title == null)
                    {
                        TFTVLogger.Always("objective1.Title is missing!");
                    }
                    else
                    {
                        if (objective.Title.LocalizationKey == null)
                        {
                            TFTVLogger.Always("objective1.Title.LocalizationKey is missing!");
                        }
                        else
                        {
                            if (objective.Title.LocalizationKey.Contains(voidOmenTitleString))
                            {
                                voidOmens.Add(objective);
                            }
                        }
                    }

                }

                return voidOmens;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
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


        [HarmonyPatch(typeof(FactionObjective), nameof(FactionObjective.GetCompletion))]
        public static class FactionObjective_GetCompletion_VO4_Patch
        {
            public static void Postfix(ref float __result)
            {
                try
                {
                    TFTVConfig config = TFTVMain.Main.Config;

                    if (VoidOmensCheck[4] && config.LimitedDeploymentVO)
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
                                            ActorClassIconElement actorClassIconElement = tacticalActor.TacticalActorViewBase.UIActorElement.GetComponent<HealthbarUIActorElement>().ActorClassIconElement;
                                            TFTVUITactical.Enemies.ChangeHealthBarIcon(actorClassIconElement, tacticalActor);

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


        //Adjust Research output based on difficulty/VO6



        [HarmonyPatch(typeof(Research), "GetHourlyResearchProduction")] //VERIFIED
        public static class TFTV_Research_GetHourlyResearchProductionVO6_Patch
        {
            public static void Postfix(ref float __result, Research __instance)
            {
                try
                {
                    //TFTVLogger.Always("GetHourlyResearchProduction invoked");

                    GeoLevelController controller = __instance.Faction.GeoLevel;

                    if (__instance.Faction == controller.PhoenixFaction && VoidOmensCheck[6])
                    {
                        //TFTVLogger.Always($"VO6 should be working");
                        float multiplier = 1.5f;
                        __result *= multiplier;

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        [HarmonyPatch(typeof(TacticalAbility), "get_WillPointCost")] //VERIFIED
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
                        PassiveModifierAbilityDef feralDeliriumPerk = DefCache.GetDef<PassiveModifierAbilityDef>("FeralNew_AbilityDef");

                        if (__instance.TacticalActor != null && __instance.TacticalActor.GetAbilityWithDef<PassiveModifierAbility>(feralDeliriumPerk) != null)
                        {

                            __result += 1;

                        }

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(TacticalVoxelMatrix), nameof(TacticalVoxelMatrix.SpawnAndPropagateMist))]
        public static class TacticalVoxelMatrix_SpawnAndPropagateMist_VoidOmenMoreMistOnTactical_Patch
        {
            public static void Prefix(TacticalVoxelMatrix __instance)
            {
                try
                {
                    TFTVConfig config = TFTVMain.Main.Config;

                    if (VoidOmensCheck[7] && config.MoreMistVO)
                    {
                        int difficultyLevel = TFTVSpecialDifficulties.DifficultyOrderConverter(__instance.TacticalLevel.Difficulty.Order);

                        MissionTagDef nestAssaultTag = DefCache.GetDef<MissionTagDef>("MissionTypeAlienNestAssault_MissionTagDef");
                        MissionTagDef lairAssaultTag = DefCache.GetDef<MissionTagDef>("MissionTypeAlienLairAssault_MissionTagDef");
                        MissionTagDef citadelAssaultTag = DefCache.GetDef<MissionTagDef>("MissionTypeAlienCitadelAssault_MissionTagDef");

                        TFTVLogger.Always($"More Mist VO is in effect and it is turned on in the config options");

                        float missionTypeModifer = 1;

                        MissionTagDef missionTag = __instance.TacticalLevel.TacMission.MissionData.MissionType.MissionTypeTag;

                        //  string saveDefaultName = __instance.TacticalLevel.TacMission.MissionData.MissionType.SaveDefaultName;

                        if (missionTag != null)
                        {

                            if (missionTag == nestAssaultTag)
                            {
                                TFTVLogger.Always($"The mission is in a nest");

                                missionTypeModifer = 0.25f;
                            }
                            else if (missionTag == citadelAssaultTag || missionTag == lairAssaultTag)
                            {
                                TFTVLogger.Always($"The mission is in a lair or in a Citadel");
                                missionTypeModifer = 0.5f;
                            }
                        }



                        __instance.VoxelMatrixData.InitialMistEntitiesToSpawn.Min = Mathf.FloorToInt(3 + difficultyLevel * (int)(6 * missionTypeModifer));
                        __instance.VoxelMatrixData.InitialMistEntitiesToSpawn.Max = Mathf.FloorToInt(8 + difficultyLevel * (int)(6 * missionTypeModifer));
                        TFTVLogger.Always($"min blobs: {__instance.VoxelMatrixData.InitialMistEntitiesToSpawn.Min}, max blobs: {__instance.VoxelMatrixData.InitialMistEntitiesToSpawn.Max}");
                        
                    }
                    else
                    {

                        __instance.VoxelMatrixData.InitialMistEntitiesToSpawn.Min = 1;
                        __instance.VoxelMatrixData.InitialMistEntitiesToSpawn.Max = Mathf.FloorToInt(3);
                        
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

       




        [HarmonyPatch(typeof(UIStateRosterDeployment), "get__squadMaxDeployment")] //VERIFIED
        public static class UIStateRosterDeployment_get_SquadMaxDeployment_VoidOmenLimitedDeployment_Patch
        {
            public static void Postfix(ref int __result, UIStateRosterDeployment __instance)
            {
                try
                {
                    TFTVConfig config = TFTVMain.Main.Config;

                    if (VoidOmensCheck[7] && config.MoreMistVO && !TFTVAircraftReworkMain.AircraftReworkOn)
                    {
                        __result += 1;
                    }
                    if (VoidOmensCheck[4] && config.LimitedDeploymentVO)
                    {
                        __result -= 2;
                    }
                    if (__instance.Mission.MissionDef.name.Equals("StoryFS2_CustomMissionTypeDef"))
                    {
                        __result = 8;
                    }
                    if (__instance.Mission.MissionDef.MissionTypeTag == DefCache.GetDef<MissionTagDef>("MissionTypePhoenixBaseDefence_MissionTagDef"))
                    {
                        TFTVLogger.Always($"Base defense mission: setting max deployment to 9");
                        __result = 9;
                    }

                    if (config.UnLimitedDeployment) 
                    {
                        __result = 99;
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        /*  [HarmonyPatch(typeof(GeoBehemothActor), "get_DisruptionMax")]
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
          }*/

        public static void ImplementStrongerHavenDefenseVO(ref HavenAttacker attacker)
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

        [HarmonyPatch(typeof(GeoHavenDefenseMission), "GetDefenseDeployment")] //VERIFIED
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


        private static LocalizedTextBind _destroyedAlienBase = new LocalizedTextBind();

        [HarmonyPatch(typeof(GeoAlienFaction), "AlienBaseDestroyed")] //VERIFIED
        public static class GeoAlienFaction_AlienBaseDestroyed_RemoveVoidOmenDestroyedPC_patch
        {
            public static void Prefix(GeoAlienBase alienBase, GeoAlienFaction __instance)
            {
                try
                {
                    TFTVLogger.Always($"{alienBase.AlienBaseTypeDef.Name.Localize()} destroyed");

                    _destroyedAlienBase = alienBase.AlienBaseTypeDef.Name;

                    if (alienBase.AlienBaseTypeDef.Keyword == "lair" || alienBase.AlienBaseTypeDef.Keyword == "citadel"
                        || (alienBase.AlienBaseTypeDef.Keyword == "nest" && Math.Max(__instance.GeoLevel.CurrentDifficultyLevel.Order - 1, 1) == 1))
                    {
                        TFTVLogger.Always("Lair or Citadal destroyed, Void Omen should be removed and Void Omen event triggered");

                        string removedVoidOmen = RemoveEarliestVoidOmen(__instance.GeoLevel, 0);

                        if (removedVoidOmen != null)
                        {
                            TFTVODIandVoidOmenRoll.GenerateVoidOmenEvent(__instance.GeoLevel, TFTVODIandVoidOmenRoll.GenerateReportData(__instance.GeoLevel), false, removedVoidOmen);
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(GeoscapeLog), "AddEntry")] //VERIFIED
        public static void Prefix(ref GeoscapeLogEntry entry, GeoActor actor, GeoscapeLogMessagesDef ____messagesDef, GeoscapeLog __instance)
        {
            try
            {
                if (entry.Text == ____messagesDef.AlienBaseDestroyedMessage && actor is GeoSite geoSite)
                {
                    if (entry.Parameters[0] == geoSite.SiteName)
                    {
                        entry.Parameters[0] = _destroyedAlienBase;
                    }
                }
               
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        //VO2 apply penalty to diplo reward from sabotage missions
        [HarmonyPatch(typeof(GeoSabotageZoneMission), "AddFactionRequestReward")] //VERIFIED
        public static class TFTV_GeoSabotageZoneMission_AddFactionRequestReward
        {
            public static void Postfix(GeoSabotageZoneMission __instance, ref MissionRewardDescription reward)
            {
                try
                {
                    if (TFTVVoidOmens.CheckFordVoidOmensInPlay(__instance.Level).Contains(2))
                    {
                        foreach (FactionAggressionRequest factionRequest in __instance.FactionRequests)
                        {
                            reward.SetDiplomacyChange(__instance.Site.GeoLevel.GetFaction(factionRequest.FromFaction), __instance.Site.GeoLevel.ViewerFaction, (int)(factionRequest.FactionDiplomacyReward * 0.5f));
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



        //VO5 increase chance to spawn weapons in crates
        [HarmonyPatch(typeof(GeoMission), "PrepareTacticalGame")] //VERIFIED
        public static class TFTV_GeoMission_ModifyCratesVO5_Patch
        {
            public static void Prefix(GeoMission __instance, ref List<WeaponDef> __state)
            {
                try
                {
                    // TFTVLogger.Always($"PrepareTacticalGame is running");

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

       

    }
}
