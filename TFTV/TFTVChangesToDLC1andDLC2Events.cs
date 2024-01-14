using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Conditions;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Events.Eventus.Filters;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Tactical.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV
{
    internal class TFTVChangesToDLC1andDLC2Events
    {

        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        private static readonly GeoFactionDef PhoenixPoint = DefCache.GetDef<GeoFactionDef>("Phoenix_GeoPhoenixFactionDef");
        private static readonly GeoFactionDef NewJericho = DefCache.GetDef<GeoFactionDef>("NewJericho_GeoFactionDef");
        private static readonly GeoFactionDef Anu = DefCache.GetDef<GeoFactionDef>("Anu_GeoFactionDef");
        private static readonly GeoFactionDef Synedrion = DefCache.GetDef<GeoFactionDef>("Synedrion_GeoFactionDef");


      
        public static void ChangesToDLC1andDLC2Defs()
        {
            try
            {
                AdjustAugmenetationResearchTexts();
                ChangesToSavingHelena();
                ReplaceAllSchemataMissions();
            
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void ChangesToSavingHelena()
        {
            try
            {
                //Adding peaceful option for Saving Helena
                GeoscapeEventDef savingHelenaWin = DefCache.GetDef<GeoscapeEventDef>("PROG_LE0_WIN_GeoscapeEventDef");
                GeoscapeEventDef savingHelenaMiss = DefCache.GetDef<GeoscapeEventDef>("PROG_LE0_MISS_GeoscapeEventDef");
                savingHelenaMiss.GeoscapeEventData.Choices.Add(new GeoEventChoice()
                {
                    Text = new LocalizedTextBind("PROG_LE0_MISS_CHOICE_2_TEXT"),
                    Requirments = new GeoEventChoiceRequirements()
                    {
                        Diplomacy = new List<GeoEventChoiceDiplomacy>()

                        {
                            new GeoEventChoiceDiplomacy ()
                            {
                            Target = GeoEventChoiceDiplomacy.DiplomacyTarget.SiteFaction,
                            Operator = GeoEventChoiceDiplomacy.DiplomacyOperator.Greater,
                            Value = 24,
                            }
                         },
                    },

                    Outcome = new GeoEventChoiceOutcome()
                    {
                        UntrackEncounters = savingHelenaWin.GeoscapeEventData.Choices[0].Outcome.UntrackEncounters,
                        VariablesChange = savingHelenaWin.GeoscapeEventData.Choices[0].Outcome.VariablesChange,
                        Cinematic = savingHelenaWin.GeoscapeEventData.Choices[0].Outcome.Cinematic,
                        OutcomeText = new EventTextVariation()
                        {
                            General = new LocalizedTextBind("PROG_LE0_MISS_CHOICE_2_OUTCOME_GENERAL")
                        },
                        TriggerEncounterID = "HelenaOnOlena"
                    }
                });
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void AdjustAugmenetationResearchTexts()
        {
            try
            {
                ResearchViewElementDef njBionicsVEDef = DefCache.GetDef<ResearchViewElementDef>("NJ_Bionics1_ViewElementDef");
                njBionicsVEDef.CompleteText.LocalizationKey = "TFTV_BIONICS_RESEARCHDEF_COMPLETE";

                ResearchViewElementDef anuBionicsVEDef = DefCache.GetDef<ResearchViewElementDef>("ANU_MutationTech_ViewElementDef");
                anuBionicsVEDef.CompleteText.LocalizationKey = "TFTV_MUTATIONTECH_RESEARCHDEF_COMPLETE";


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }




        }

       

        private static void ReplaceAllSchemataMissions()
        {
            try
            {
                //Replace all LOTA Schemata missions with KE2 mission
                GeoscapeEventDef geoEventFS9 = DefCache.GetDef<GeoscapeEventDef>("PROG_FS9_GeoscapeEventDef");
                GeoscapeEventDef KE2Miss = DefCache.GetDef<GeoscapeEventDef>("PROG_KE2_GeoscapeEventDef");
                GeoscapeEventDef LE1Miss = DefCache.GetDef<GeoscapeEventDef>("PROG_LE1_MISS_GeoscapeEventDef");
                LE1Miss.GeoscapeEventData.Choices[0].Outcome.StartMission.MissionTypeDef = KE2Miss.GeoscapeEventData.Choices[0].Outcome.StartMission.MissionTypeDef;

                LE1Miss.GeoscapeEventData.Choices[0].Outcome.StartMission.MissionTypeDef.DontRecoverItems = true;

                //Don't generate next Schemata mission
                GeoscapeEventDef LE1Win = DefCache.GetDef<GeoscapeEventDef>("PROG_LE1_WIN_GeoscapeEventDef");
                //GeoscapeEventDef geoEventFS9 = DefCache.GetDef<GeoscapeEventDef>("PROG_FS9_GeoscapeEventDef"));
                LE1Win.GeoscapeEventData.Choices[0].Outcome.SetEvents.Clear();
                LE1Win.GeoscapeEventData.Choices[0].Outcome.TrackEncounters.Clear();
                //Unlock all ancient weapons research and add hidden variable to unlock final cinematic
                GeoscapeEventDef LE2Win = DefCache.GetDef<GeoscapeEventDef>("PROG_LE2_WIN_GeoscapeEventDef");
                GeoscapeEventDef LE3Win = DefCache.GetDef<GeoscapeEventDef>("PROG_LE3_WIN_GeoscapeEventDef");
                GeoscapeEventDef LE4Win = DefCache.GetDef<GeoscapeEventDef>("PROG_LE4_WIN_GeoscapeEventDef");
                GeoscapeEventDef LE5Win = DefCache.GetDef<GeoscapeEventDef>("PROG_LE5_WIN_GeoscapeEventDef");
                GeoscapeEventDef LE6Win = DefCache.GetDef<GeoscapeEventDef>("PROG_LE6_WIN_GeoscapeEventDef");
                OutcomeVariableChange Schemata2Res = LE2Win.GeoscapeEventData.Choices[0].Outcome.VariablesChange[0];
                OutcomeVariableChange Schemata3Res = LE3Win.GeoscapeEventData.Choices[0].Outcome.VariablesChange[0];
                OutcomeVariableChange Schemata4Res = LE4Win.GeoscapeEventData.Choices[0].Outcome.VariablesChange[0];
                OutcomeVariableChange Schemata5Res = LE5Win.GeoscapeEventData.Choices[0].Outcome.VariablesChange[0];
                OutcomeVariableChange Schemata6Res = LE6Win.GeoscapeEventData.Choices[0].Outcome.VariablesChange[0];
                OutcomeVariableChange var6LE = LE6Win.GeoscapeEventData.Choices[0].Outcome.VariablesChange[1];
                LE1Win.GeoscapeEventData.Choices[0].Outcome.VariablesChange.Add(Schemata2Res);
                LE1Win.GeoscapeEventData.Choices[0].Outcome.VariablesChange.Add(Schemata3Res);
                LE1Win.GeoscapeEventData.Choices[0].Outcome.VariablesChange.Add(Schemata4Res);
                LE1Win.GeoscapeEventData.Choices[0].Outcome.VariablesChange.Add(Schemata5Res);
                LE1Win.GeoscapeEventData.Choices[0].Outcome.VariablesChange.Add(Schemata6Res);
                LE1Win.GeoscapeEventData.Choices[0].Outcome.VariablesChange.Add(var6LE);
                //Remove 50 SP
                LE1Win.GeoscapeEventData.Choices[0].Outcome.FactionSkillPoints = 0;
                LE1Win.GeoscapeEventData.Leader = "Jack_Harlson01";

                //Require capturing ancient site for LOTA Schemata missions
                //GeoscapeEventDef LE1Event = DefCache.GetDef<GeoscapeEventDef>("PROG_LE1_GeoscapeEventDef"));
                //GeoscapeEventDef LEFinalEvent = DefCache.GetDef<GeoscapeEventDef>("PROG_LE_FINAL_GeoscapeEventDef"));
                //GeoLevelConditionDef sourceCondition = DefCache.GetDef<GeoLevelConditionDef>("[PROG_LE_FINAL] Condition 1"));
                //GeoLevelConditionDef newCondition = Helper.CreateDefFromClone(sourceCondition, "0358D502-421D-4D9A-9505-491FC80F1C56", "[PROG_LE_1] Condition 2");
                //newCondition.VariableCompareToNumber = 1;
                //LE1Event.GeoscapeEventData.Conditions.Add(newCondition);

                //Add choices for LE1Win

                LE1Win.GeoscapeEventData.Choices.Add(new GeoEventChoice()
                {
                    Text = new LocalizedTextBind("PROG_LE1_WIN_CHOICE_1_TEXT"),
                    Outcome = new GeoEventChoiceOutcome()
                    {
                        UntrackEncounters = LE1Win.GeoscapeEventData.Choices[0].Outcome.UntrackEncounters,
                        VariablesChange = LE1Win.GeoscapeEventData.Choices[0].Outcome.VariablesChange,
                        OutcomeText = new EventTextVariation()
                        {
                            General = new LocalizedTextBind("PROG_LE1_WIN_CHOICE_1_OUTCOME_GENERAL")
                        },
                        SetEvents = geoEventFS9.GeoscapeEventData.Choices[0].Outcome.SetEvents,
                        TrackEncounters = geoEventFS9.GeoscapeEventData.Choices[0].Outcome.TrackEncounters,
                        FactionSkillPoints = 0
                    }
                });
                LE1Win.GeoscapeEventData.Choices.Add(new GeoEventChoice()
                {
                    Text = new LocalizedTextBind("PROG_LE1_WIN_CHOICE_2_TEXT"),
                    Outcome = new GeoEventChoiceOutcome()
                    {
                        UntrackEncounters = LE1Win.GeoscapeEventData.Choices[0].Outcome.UntrackEncounters,
                        VariablesChange = LE1Win.GeoscapeEventData.Choices[0].Outcome.VariablesChange,
                        OutcomeText = new EventTextVariation()
                        {
                            General = new LocalizedTextBind("PROG_LE1_WIN_CHOICE_2_OUTCOME_GENERAL")
                        },
                        SetEvents = geoEventFS9.GeoscapeEventData.Choices[0].Outcome.SetEvents,
                        TrackEncounters = geoEventFS9.GeoscapeEventData.Choices[0].Outcome.TrackEncounters,
                        FactionSkillPoints = 0
                    }
                });
                LE1Win.GeoscapeEventData.Choices[0].Outcome.OutcomeText.General.LocalizationKey = "PROG_LE1_WIN_CHOICE_0_OUTCOME_GENERAL";
                TacCharacterDef armadillo = DefCache.GetDef<TacCharacterDef>("NJ_Armadillo_CharacterTemplateDef");
                LE1Win.GeoscapeEventData.Choices[0].Outcome.Units.Add(armadillo);
                LE1Win.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(new OutcomeDiplomacyChange()
                {
                    PartyFaction = NewJericho,
                    TargetFaction = PhoenixPoint,
                    PartyType = (OutcomeDiplomacyChange.ChangeTarget)1,
                    Value = -8

                });
                LE1Win.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(new OutcomeDiplomacyChange()
                {
                    PartyFaction = NewJericho,
                    TargetFaction = PhoenixPoint,
                    PartyType = (OutcomeDiplomacyChange.ChangeTarget)1,
                    Value = +8
                });
                LE1Win.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(new OutcomeDiplomacyChange()
                {
                    PartyFaction = Anu,
                    TargetFaction = PhoenixPoint,
                    PartyType = (OutcomeDiplomacyChange.ChangeTarget)1,
                    Value = -8
                });
                LE1Win.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(new OutcomeDiplomacyChange()
                {
                    PartyFaction = NewJericho,
                    TargetFaction = Anu,
                    PartyType = (OutcomeDiplomacyChange.ChangeTarget)1,
                    Value = -16
                });




            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }



        }





    }
}
