using Base.Defs;
using Base.UI;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV
{
    internal class TFTVChangesToDLC1andDLC2Events
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        public static void Apply_Changes()
        {
            try
            {
                MakeLOTAAlternativeToSymes();
               // TFTVRevenant.CreateRevenantTemplate("Scylla1_FrenzyMistSmasherAgileSpawner_AlienMutationVariationDef");


                //ID all the factions for later
                GeoFactionDef PhoenixPoint = Repo.GetAllDefs<GeoFactionDef>().FirstOrDefault(ged => ged.name.Equals("Phoenix_GeoPhoenixFactionDef"));
                GeoFactionDef NewJericho = Repo.GetAllDefs<GeoFactionDef>().FirstOrDefault(ged => ged.name.Equals("NewJericho_GeoFactionDef"));
                GeoFactionDef Anu = Repo.GetAllDefs<GeoFactionDef>().FirstOrDefault(ged => ged.name.Equals("Anu_GeoFactionDef"));
                GeoFactionDef Synedrion = Repo.GetAllDefs<GeoFactionDef>().FirstOrDefault(ged => ged.name.Equals("Synedrion_GeoFactionDef"));

                //Source for creating new events
                GeoscapeEventDef sourceLoseGeoEvent = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_PU12_FAIL_GeoscapeEventDef"));

                //Adding peaceful option for Saving Helena
                GeoscapeEventDef savingHelenaWin = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_LE0_WIN_GeoscapeEventDef"));
                GeoscapeEventDef savingHelenaMiss = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_LE0_MISS_GeoscapeEventDef"));
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

                // Add new choices to DLC1
                // Snitch to NJ
                GeoscapeEventDef prog_PU2_Choice2Event = TFTVCommonMethods.CreateNewEvent("PROG_PU2_CHOICE2EVENT", "PROG_PU2_CHOICE2EVENT_TITLE", "PROG_PU2_CHOICE2EVENT_TEXT_GENERAL_0", null);
                prog_PU2_Choice2Event.GeoscapeEventData.Leader = "NJ_TW";

                prog_PU2_Choice2Event.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, 3));
                prog_PU2_Choice2Event.GeoscapeEventData.Choices[0].Outcome.Resources.Add(new ResourceUnit()

                {
                    Type = ResourceType.Materials,
                    Value = 300
                });

                //Publicly denounce NJ
                GeoscapeEventDef prog_PU2_Choice3Event = TFTVCommonMethods.CreateNewEvent("PROG_PU2_CHOICE3EVENT", "PROG_PU2_CHOICE3EVENT_TITLE", "PROG_PU2_CHOICE3EVENT_TEXT_GENERAL_0", null);
                prog_PU2_Choice3Event.GeoscapeEventData.Leader = "SY_Nikolai";
                prog_PU2_Choice3Event.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -5));
                prog_PU2_Choice3Event.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, 5));
                prog_PU2_Choice3Event.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, NewJericho, -5));
                prog_PU2_Choice3Event.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, Synedrion, -5));

                //Add the choices to the event
                //New events have to be created rather than using Outcomes within each choice to replace leader pic
                GeoscapeEventDef subject24offer = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_PU2_GeoscapeEventDef"));
                subject24offer.GeoscapeEventData.Choices.Add(new GeoEventChoice()
                {
                    Text = (new LocalizedTextBind("PROG_PU2_CHOICE_2_TEXT")),
                    Outcome = new GeoEventChoiceOutcome()
                    {
                        TriggerEncounterID = "PROG_PU2_CHOICE2EVENT",
                    }
                });

                subject24offer.GeoscapeEventData.Choices.Add(new GeoEventChoice()
                {
                    Text = (new LocalizedTextBind("PROG_PU2_CHOICE_3_TEXT")),
                    Outcome = new GeoEventChoiceOutcome()
                    {
                        TriggerEncounterID = "PROG_PU2_CHOICE3EVENT",
                    }
                });

                //Add options for DLC1MISS WIN
                GeoscapeEventDef DLC1missWIN = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_PU4_WIN_GeoscapeEventDef"));

                //Anu option
                GeoscapeEventDef an28event = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("AN28_GeoscapeEventDef"));
                DLC1missWIN.GeoscapeEventData.Choices[0].Outcome.Units = an28event.GeoscapeEventData.Choices[0].Outcome.Units;
                DLC1missWIN.GeoscapeEventData.Choices[0].Outcome.OutcomeText.General.LocalizationKey = "PROG_PU4_WIN_CHOICE_0_OUTCOME_GENERAL";

                //Syn choice
                DLC1missWIN.GeoscapeEventData.Choices.Add(new GeoEventChoice()
                {
                    Text = (new LocalizedTextBind("PROG_PU4_WIN_CHOICE_1_TEXT")),
                    Outcome = new GeoEventChoiceOutcome()
                    {
                        OutcomeText = new EventTextVariation()
                        {
                            General = new LocalizedTextBind("PROG_PU4_WIN_CHOICE_1_OUTCOME_GENERAL")
                        },
                        VariablesChange = DLC1missWIN.GeoscapeEventData.Choices[0].Outcome.VariablesChange,
                        UntrackEncounters = DLC1missWIN.GeoscapeEventData.Choices[0].Outcome.UntrackEncounters,
                        RemoveTimers = DLC1missWIN.GeoscapeEventData.Choices[0].Outcome.RemoveTimers,
                    },
                });
                DLC1missWIN.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(new OutcomeDiplomacyChange()
                {
                    PartyFaction = NewJericho,
                    TargetFaction = Synedrion,
                    PartyType = (OutcomeDiplomacyChange.ChangeTarget)1,
                    Value = +3
                });
                DLC1missWIN.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(new OutcomeDiplomacyChange()
                {
                    PartyFaction = Synedrion,
                    TargetFaction = NewJericho,
                    PartyType = (OutcomeDiplomacyChange.ChangeTarget)1,
                    Value = +3
                });
                DLC1missWIN.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(new OutcomeDiplomacyChange()
                {
                    PartyFaction = NewJericho,
                    TargetFaction = PhoenixPoint,
                    PartyType = (OutcomeDiplomacyChange.ChangeTarget)1,
                    Value = -6
                });
                DLC1missWIN.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(new OutcomeDiplomacyChange()
                {
                    PartyFaction = Synedrion,
                    TargetFaction = PhoenixPoint,
                    PartyType = (OutcomeDiplomacyChange.ChangeTarget)1,
                    Value = +2
                });

                //Deny deny deny option
                DLC1missWIN.GeoscapeEventData.Choices.Add(new GeoEventChoice()
                {
                    Text = (new LocalizedTextBind("PROG_PU4_WIN_CHOICE_2_TEXT")),
                    Outcome = new GeoEventChoiceOutcome()
                    {
                        OutcomeText = new EventTextVariation()
                        {
                            General = new LocalizedTextBind("PROG_PU4_WIN_CHOICE_2_OUTCOME_GENERAL")
                        },
                        VariablesChange = DLC1missWIN.GeoscapeEventData.Choices[0].Outcome.VariablesChange,
                        UntrackEncounters = DLC1missWIN.GeoscapeEventData.Choices[0].Outcome.UntrackEncounters,
                        RemoveTimers = DLC1missWIN.GeoscapeEventData.Choices[0].Outcome.RemoveTimers,
                    },
                });
                DLC1missWIN.GeoscapeEventData.Choices[2].Outcome.Diplomacy.Add(new OutcomeDiplomacyChange()
                {
                    PartyFaction = NewJericho,
                    TargetFaction = Anu,
                    PartyType = (OutcomeDiplomacyChange.ChangeTarget)1,
                    Value = -3
                });
                DLC1missWIN.GeoscapeEventData.Choices[2].Outcome.Diplomacy.Add(new OutcomeDiplomacyChange()
                {
                    PartyFaction = NewJericho,
                    TargetFaction = Synedrion,
                    PartyType = (OutcomeDiplomacyChange.ChangeTarget)1,
                    Value = -3
                });
                DLC1missWIN.GeoscapeEventData.Choices[2].Outcome.Diplomacy.Add(new OutcomeDiplomacyChange()
                {
                    PartyFaction = NewJericho,
                    TargetFaction = PhoenixPoint,
                    PartyType = (OutcomeDiplomacyChange.ChangeTarget)1,
                    Value = -3
                });

                //Add options to Guided by Whispers
                //If relations with Synedrion Aligned, can opt for HD vs Pure
                //Event if HD successful
                GeoscapeEventDef sourceWinGeoEvent = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_PU12_WIN_GeoscapeEventDef"));
                var pu12miss = sourceWinGeoEvent.GeoscapeEventData.Choices[0].Outcome.UntrackEncounters[0];
                GeoscapeEventDef newWinPU12 = Helper.CreateDefFromClone(sourceWinGeoEvent, "23435C5E-B933-484D-990E-5B4C0B2B32FE", "PROG_PU12_WIN2_GeoscapeEventDef");
                newWinPU12.GeoscapeEventData.EventID = "PROG_PU12WIN2";
                newWinPU12.GeoscapeEventData.Title.LocalizationKey = "PROG_PU12_WIN2_TITLE";
                newWinPU12.GeoscapeEventData.Description[0].General.LocalizationKey = "PROG_PU12_WIN2_TEXT_GENERAL_0";
                newWinPU12.GeoscapeEventData.Choices[0].Text.LocalizationKey = "PROG_PU12_WIN2_CHOICE_0_TEXT";
                newWinPU12.GeoscapeEventData.Choices[0].Outcome.Diplomacy[0] = new OutcomeDiplomacyChange()

                {
                    PartyFaction = Synedrion,
                    TargetFaction = PhoenixPoint,
                    Value = 6,
                    PartyType = (OutcomeDiplomacyChange.ChangeTarget)1,
                };

                //Event if HD Failure
                GeoscapeEventDef newFailPU12 = Helper.CreateDefFromClone(sourceLoseGeoEvent, "D77EB7A7-FE26-49EF-BB7A-449A51D4D519", "PROG_PU12_FAIL2_GeoscapeEventDef");
                newFailPU12.GeoscapeEventData.EventID = "PROG_PU12FAIL2";
                newFailPU12.GeoscapeEventData.Title.LocalizationKey = "PROG_PU12_FAIL2_TITLE";
                newFailPU12.GeoscapeEventData.Description[0].General.LocalizationKey = "PROG_PU12_FAIL2_TEXT_GENERAL_0";
                newFailPU12.GeoscapeEventData.Choices[0].Text.LocalizationKey = "PROG_PU12_FAIL2_CHOICE_0_TEXT";
                newFailPU12.GeoscapeEventData.Choices[0].Outcome.UntrackEncounters.Add(pu12miss);
                newFailPU12.GeoscapeEventData.Choices[0].Outcome.RemoveTimers.Add(pu12miss);

                //New event for outcome if selling info about lab or research after stealing it by completing original mission                
                GeoscapeEventDef newPU12NJOption = Helper.CreateDefFromClone(sourceLoseGeoEvent, "D556A16F-41D8-4852-8DC4-5FB945652C50", "PROG_PU12_NewNJOption_GeoscapeEventDef");
                newPU12NJOption.GeoscapeEventData.EventID = "PROG_PU12NewNJOption";
                newPU12NJOption.GeoscapeEventData.Leader = "NJ_Abongameli";
                newPU12NJOption.GeoscapeEventData.Title.LocalizationKey = "PROG_PU12_NEWNJOPT_TITLE";
                newPU12NJOption.GeoscapeEventData.Description[0].General.LocalizationKey = "PROG_PU12_NEWNJOPT_GENERAL";
                newPU12NJOption.GeoscapeEventData.Choices[0].Outcome.Resources.Add(new ResourceUnit()
                {
                    Type = ResourceType.Materials,
                    Value = 750
                });

                newPU12NJOption.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(new OutcomeDiplomacyChange()
                {
                    PartyFaction = NewJericho,
                    TargetFaction = PhoenixPoint,
                    Value = 3,
                    PartyType = (OutcomeDiplomacyChange.ChangeTarget)1,
                });
                newPU12NJOption.GeoscapeEventData.Choices[0].Outcome.UntrackEncounters.Add(pu12miss);
                newPU12NJOption.GeoscapeEventData.Choices[0].Outcome.RemoveTimers.Add(pu12miss);

                //Adding options to the original event, fetching it first
                GeoscapeEventDef guidedByWhispers = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_PU12_MISS_GeoscapeEventDef"));

                //Fetching Syn HD vs Pure with protect civillians type, to use as alternative mission
                CustomMissionTypeDef havenDefPureSY_CustomMissionTypeDef = Repo.GetAllDefs<CustomMissionTypeDef>().FirstOrDefault(ged => ged.name.Equals("HavenDefPureSY_Civ_CustomMissionTypeDef"));

                //Adding Syn Aligned options
                guidedByWhispers.GeoscapeEventData.Choices.Add(new GeoEventChoice()
                {
                    Text = (new LocalizedTextBind("PROG_PU12_MISS_CHOICE_2_TEXT")),
                    Outcome = new GeoEventChoiceOutcome()
                    {
                        OutcomeText = new EventTextVariation()
                        {
                            General = new LocalizedTextBind("PROG_PU12_MISS_CHOICE_2_OUTCOME_GENERAL")
                        },
                        StartMission = new OutcomeStartMission()
                        {
                            MissionTypeDef = havenDefPureSY_CustomMissionTypeDef,
                            WonEventID = "PROG_PU12WIN2",
                            LostEventID = "PROG_PU12_FAIL2"
                        }
                    },
                    Requirments = new GeoEventChoiceRequirements()
                    {
                        Diplomacy = new List<GeoEventChoiceDiplomacy>()

                        {
                            new GeoEventChoiceDiplomacy ()
                            {
                            Target = GeoEventChoiceDiplomacy.DiplomacyTarget.SiteFaction,
                            Operator = GeoEventChoiceDiplomacy.DiplomacyOperator.Greater,
                            Value = 49,
                            }
                         },
                    },
                });

                //Adding sell info to NJ option to original event
                guidedByWhispers.GeoscapeEventData.Choices.Add(new GeoEventChoice()
                {
                    Text = (new LocalizedTextBind("PROG_PU12_MISS_CHOICE_3_TEXT")),
                    Outcome = new GeoEventChoiceOutcome()
                    {
                        TriggerEncounterID = "PROG_PU12NewNJOption",
                    }
                });

                //Add option after winning original mission to sell research to NJ
                sourceWinGeoEvent.GeoscapeEventData.Choices.Add(new GeoEventChoice()
                {
                    Text = (new LocalizedTextBind("PROG_PU12_WIN_CHOICE_1_TEXT")),
                    Outcome = new GeoEventChoiceOutcome()
                    {
                        TriggerEncounterID = "PROG_PU12NewNJOption",
                    },
                });

                //Replace all LOTA Schemata missions with KE2 mission
                GeoscapeEventDef geoEventFS9 = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_FS9_GeoscapeEventDef"));
                GeoscapeEventDef KE2Miss = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_KE2_GeoscapeEventDef"));
                GeoscapeEventDef LE1Miss = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_LE1_MISS_GeoscapeEventDef"));
                LE1Miss.GeoscapeEventData.Choices[0].Outcome.StartMission.MissionTypeDef = KE2Miss.GeoscapeEventData.Choices[0].Outcome.StartMission.MissionTypeDef;
                //Don't generate next Schemata mission
                GeoscapeEventDef LE1Win = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_LE1_WIN_GeoscapeEventDef"));
                //GeoscapeEventDef geoEventFS9 = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_FS9_GeoscapeEventDef"));
                LE1Win.GeoscapeEventData.Choices[0].Outcome.SetEvents.Clear();
                LE1Win.GeoscapeEventData.Choices[0].Outcome.TrackEncounters.Clear();
                //Unlock all ancient weapons research and add hidden variable to unlock final cinematic
                GeoscapeEventDef LE2Win = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_LE2_WIN_GeoscapeEventDef"));
                GeoscapeEventDef LE3Win = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_LE3_WIN_GeoscapeEventDef"));
                GeoscapeEventDef LE4Win = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_LE4_WIN_GeoscapeEventDef"));
                GeoscapeEventDef LE5Win = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_LE5_WIN_GeoscapeEventDef"));
                GeoscapeEventDef LE6Win = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_LE6_WIN_GeoscapeEventDef"));
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
                //GeoscapeEventDef LE1Event = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_LE1_GeoscapeEventDef"));
                //GeoscapeEventDef LEFinalEvent = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_LE_FINAL_GeoscapeEventDef"));
                //GeoLevelConditionDef sourceCondition = Repo.GetAllDefs<GeoLevelConditionDef>().FirstOrDefault(ged => ged.name.Equals("[PROG_LE_FINAL] Condition 1"));
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
                TacCharacterDef armadillo = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(ged => ged.name.Equals("NJ_Armadillo_CharacterTemplateDef"));
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

        public static void MakeLOTAAlternativeToSymes()
        {
            try
            {
                GeoscapeEventDef leFINAL = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_LE_FINAL_GeoscapeEventDef"));
                leFINAL.GeoscapeEventData.Choices[0].Outcome.GiveResearches.Add("PX_VirophageWeapons_ResearchDef");
                leFINAL.GeoscapeEventData.Choices[0].Outcome.GiveResearches.Add("Pandoravirus");
                leFINAL.GeoscapeEventData.Choices[0].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("Sphere", 1, true));
                

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

    }
}
