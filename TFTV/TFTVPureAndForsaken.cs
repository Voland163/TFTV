using Base;
using Base.Defs;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.Levels.Missions;
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
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV
{
    internal class TFTVPureAndForsaken
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly GeoFactionDef PhoenixPoint = DefCache.GetDef<GeoFactionDef>("Phoenix_GeoPhoenixFactionDef");
        private static readonly GeoFactionDef NewJericho = DefCache.GetDef<GeoFactionDef>("NewJericho_GeoFactionDef");
        private static readonly GeoFactionDef Anu = DefCache.GetDef<GeoFactionDef>("Anu_GeoFactionDef");
        private static readonly GeoFactionDef Synedrion = DefCache.GetDef<GeoFactionDef>("Synedrion_GeoFactionDef");
        private static readonly string _puAmbushVariable = "PU13";
        private static GeoscapeEventDef _OlenaOnPureEvent;
        internal static GeoscapeEventDef PU_AmbushStartEvent = DefCache.GetDef<GeoscapeEventDef>("PROG_PU5_GeoscapeEventDef");


        internal class Defs
        {
            public static void InitDefs()
            {
                try
                {
                    Story.EventsAndTriggers();
                    Templates.AdjustPureTemplates();
                    Templates.AdjustForsakenTemplates();

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            internal class Story
            {

                //Synedrion complete Nanotech by Jan 15...

                //PROG_PU5_GeoscapeEventDef sets ambush chance to 100 (for our purposes, what matters is that it's above 0)
                //it's triggered by NJ completing NJ_Bionics1_ResearchDef
                //TFTV: triggered by 
                // NJ Researching NJ_Bionics2 (around Jan 24) done

                //PROG_PU6_GeoscapeEventDef sets FO ambush chance to 50
                //it's triggered by ANU researching Priest
                //TFTV: OK

                //PROG_PU7_GeoscapeEventDef sets the mission vs the FO (PROG_PU8_MISS_GeoscapeEventDef)
                //it's triggered by NJ researching Technician, around Jan 19
                //TFTV: OK

                //PROG_PU9_GeoscapeEventDef seems to be purely flavor, triggered by NJ researching NJ_Bionics2_ResearchDef
                //TFTV: trigger when player researches NJ Bionics2 done

                //PROG_PU10_GeoscapeEventDef offers mission vs Syn + Pure for 250 mats
                //It's triggered by the variable PU10 reaching 3;
                //PX researching NJ_Bionics1_ResearchDef gives +1, 
                // PX researching NJ_Bionics2_ResearchDef gives +1,
                // SYN researching SYN_InfiltratorTech_ResearchDef gives +2,
                // TFTV: SYN researching SYN_NanoHealing_ResearchDef gives +2 Done

                //PROG_PU121_GeoscapeEventDef seems to be purely flavor, triggered by PX researching SYN_Bionics3_ResearchDef
                //TFTV: OK

                //PROG_PU13_GeoscapeEventDef sets the Bionic Fortress mission
                //it's triggered by variable PU13 reaching 3
                //NJ_Bionics1_ResearchDef_EncounterVarResearchRewardDef_0 +1
                //NJ_Bionics2_ResearchDef_EncounterVarResearchRewardDef_0 +1  This happens on Jan 24th
                //SYN_Bionics3_ResearchDef_EncounterVarResearchRewardDef_0 +1
                //TFTV: Trigger when PU13 reaches 9 Done



                internal static void EventsAndTriggers()
                {
                    try
                    {
                        PU5TriggerOnNJBionics2();
                        PU9TriggerOnPXNJBionics2();
                        RemovePU10FromInfiltratorResearchAndGiveToNanoHealing();
                        PU13IncreaseRequiredVariable();
                        CreateTriangulationEvent();
                        ChangeGuidedByWhispers();
                        AddOptionsUndefendable();
                        AddOptionsSubject24Intro();
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void AddOptionsSubject24Intro()
                {
                    try
                    {
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
                        GeoscapeEventDef subject24offer = DefCache.GetDef<GeoscapeEventDef>("PROG_PU2_GeoscapeEventDef");
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



                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }




                private static void AddOptionsUndefendable()
                {
                    try
                    {
                        //Add options for DLC1MISS WIN
                        GeoscapeEventDef DLC1missWIN = DefCache.GetDef<GeoscapeEventDef>("PROG_PU4_WIN_GeoscapeEventDef");

                        //Anu option
                        GeoscapeEventDef an28event = DefCache.GetDef<GeoscapeEventDef>("AN28_GeoscapeEventDef");
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

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }


                }


                private static void PU5TriggerOnNJBionics2()
                {
                    try
                    {
                        DefCache.GetDef<GeoResearchEventFilterDef>("E_PROG_PU5_ResearchCompleted [GeoResearchEventFilterDef]").ResearchID = "NJ_Bionics2_ResearchDef";
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void PU9TriggerOnPXNJBionics2()
                {
                    try
                    {
                        GeoResearchEventFilterDef prog_pu9ResFilter = DefCache.GetDef<GeoResearchEventFilterDef>("E_PROG_PU9_ResearchCompleted [GeoResearchEventFilterDef]");
                        prog_pu9ResFilter.ResearchedBy = DefCache.GetDef<GeoFactionDef>("Phoenix_GeoPhoenixFactionDef");
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void RemovePU10FromInfiltratorResearchAndGiveToNanoHealing()
                {
                    try
                    {
                        EncounterVarResearchRewardDef infiltratorPu10 = DefCache.GetDef<EncounterVarResearchRewardDef>("SYN_InfiltratorTech_ResearchDef_EncounterVarResearchRewardDef_0");
                        ResearchDef infiltrator = DefCache.GetDef<ResearchDef>("SYN_InfiltratorTech_ResearchDef");
                        List<ResearchRewardDef> infiltratorUnlocks = new List<ResearchRewardDef>(infiltrator.Unlocks);
                        infiltratorUnlocks.Remove(infiltratorPu10);
                        infiltrator.Unlocks = infiltratorUnlocks.ToArray();

                        ResearchDef synNanoHealing = DefCache.GetDef<ResearchDef>("SYN_NanoHealing_ResearchDef");
                        synNanoHealing.Unlocks = synNanoHealing.Unlocks.AddItem(infiltratorPu10).ToArray();
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void PU13IncreaseRequiredVariable()
                {
                    try
                    {
                        GeoLevelConditionDef varConditonPU13 = DefCache.GetDef<GeoLevelConditionDef>("[PROG_PU13] Condition 1");
                        varConditonPU13.VariableCompareOperator = GeoEventVariationConditionDef.ComparisonOperator.GreaterOrEqual;
                        varConditonPU13.VariableCompareToNumber = 9;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void CreateTriangulationEvent()
                {
                    try
                    {
                        _OlenaOnPureEvent = TFTVCommonMethods.CreateNewEvent("OlenaOnPure", "KEY_OLENA_ON_PURE_TITLE", "KEY_OLENA_ON_PURE_DESCRIPTION", "KEY_OLENA_ON_PURE_OUTCOME");
                        _OlenaOnPureEvent.GeoscapeEventData.Choices[0].Text.LocalizationKey = "KEY_OLENA_ON_PURE_CHOICE_0";

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void GuidedByWhispersLoss()
                {
                    try
                    {

                        GeoscapeEventDef guidedByWhispersLost = DefCache.GetDef<GeoscapeEventDef>("PROG_PU12_FAIL_GeoscapeEventDef");
                        GeoscapeEventDef originalGuidedByWhispersWin = DefCache.GetDef<GeoscapeEventDef>("PROG_PU12_WIN_GeoscapeEventDef");
                        var pu12miss = originalGuidedByWhispersWin.GeoscapeEventData.Choices[0].Outcome.UntrackEncounters[0];

                        //Event if HD Failure
                        GeoscapeEventDef newFailPU12 = Helper.CreateDefFromClone(guidedByWhispersLost, "D77EB7A7-FE26-49EF-BB7A-449A51D4D519", "PROG_PU12_FAIL2_GeoscapeEventDef");
                        newFailPU12.GeoscapeEventData.EventID = "PROG_PU12FAIL2";
                        newFailPU12.GeoscapeEventData.Title.LocalizationKey = "PROG_PU12_FAIL2_TITLE";
                        newFailPU12.GeoscapeEventData.Description[0].General.LocalizationKey = "PROG_PU12_FAIL2_TEXT_GENERAL_0";
                        newFailPU12.GeoscapeEventData.Choices[0].Text.LocalizationKey = "PROG_PU12_FAIL2_CHOICE_0_TEXT";
                        newFailPU12.GeoscapeEventData.Choices[0].Outcome.UntrackEncounters.Add(pu12miss);
                        newFailPU12.GeoscapeEventData.Choices[0].Outcome.RemoveTimers.Add(pu12miss);


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }


                }

                private static void AddPeacefulOptions()

                {
                    try
                    {
                        //Adding options to the original event, fetching it first
                        GeoscapeEventDef guidedByWhispers = DefCache.GetDef<GeoscapeEventDef>("PROG_PU12_MISS_GeoscapeEventDef");

                        //Fetching Syn HD vs Pure with protect civillians type, to use as alternative mission
                        CustomMissionTypeDef havenDefPureSY_CustomMissionTypeDef = DefCache.GetDef<CustomMissionTypeDef>("HavenDefPureSY_Civ_CustomMissionTypeDef");

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
                                ReEneableEvent = false,
                                OutcomeText = new EventTextVariation()
                                {
                                    General = new LocalizedTextBind("", true)
                                },
                                TriggerEncounterID = "PROG_PU12NewNJOption",
                                Resources = new ResourcePack { new ResourceUnit()
                        {
                            Type = ResourceType.Materials,
                            Value = 750
                        }
                        },

                                Diplomacy = new List<OutcomeDiplomacyChange>() { new OutcomeDiplomacyChange()
                        {
                            PartyFaction = NewJericho,
                            TargetFaction = PhoenixPoint,
                            Value = 3,
                            PartyType = (OutcomeDiplomacyChange.ChangeTarget)1,
                        },
                        }

                            }

                        });
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void AddNewWinEvent()
                {
                    try
                    {
                        GeoscapeEventDef originalGuidedByWhispersWin = DefCache.GetDef<GeoscapeEventDef>("PROG_PU12_WIN_GeoscapeEventDef");

                        GeoscapeEventDef newWinPU12 = Helper.CreateDefFromClone(originalGuidedByWhispersWin, "23435C5E-B933-484D-990E-5B4C0B2B32FE", "PROG_PU12_WIN2_GeoscapeEventDef");
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
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void CreateNewNJOutcomePanel()
                {
                    try
                    {
                        //Event if HD successful
                        GeoscapeEventDef originalGuidedByWhispersWin = DefCache.GetDef<GeoscapeEventDef>("PROG_PU12_WIN_GeoscapeEventDef");
                        var pu12miss = originalGuidedByWhispersWin.GeoscapeEventData.Choices[0].Outcome.UntrackEncounters[0];

                        //New event for outcome if selling info about lab or research after stealing it by completing original mission                

                        GeoscapeEventDef guidedByWhispersLostSource = DefCache.GetDef<GeoscapeEventDef>("PROG_PU12_FAIL_GeoscapeEventDef");
                        GeoscapeEventDef newPU12NJOption = Helper.CreateDefFromClone(guidedByWhispersLostSource, "D556A16F-41D8-4852-8DC4-5FB945652C50", "PROG_PU12_NewNJOption_GeoscapeEventDef");
                        newPU12NJOption.GeoscapeEventData.EventID = "PROG_PU12NewNJOption";
                        newPU12NJOption.GeoscapeEventData.Leader = "NJ_Abongameli";
                        newPU12NJOption.GeoscapeEventData.Title.LocalizationKey = "PROG_PU12_NEWNJOPT_TITLE";
                        newPU12NJOption.GeoscapeEventData.Description[0].General.LocalizationKey = "PROG_PU12_NEWNJOPT_GENERAL";

                        newPU12NJOption.GeoscapeEventData.Choices[0].Outcome.UntrackEncounters.Add(pu12miss);
                        newPU12NJOption.GeoscapeEventData.Choices[0].Outcome.RemoveTimers.Add(pu12miss);

                        //Add option after winning original mission to sell research to NJ
                        originalGuidedByWhispersWin.GeoscapeEventData.Choices.Add(new GeoEventChoice()
                        {
                            Text = (new LocalizedTextBind("PROG_PU12_WIN_CHOICE_1_TEXT")),
                            Outcome = new GeoEventChoiceOutcome()
                            {
                                TriggerEncounterID = "PROG_PU12NewNJOption",
                                Resources = new ResourcePack {new ResourceUnit()
                {
                    Type = ResourceType.Materials,
                    Value = 750
                }
                },
                                Diplomacy = new List<OutcomeDiplomacyChange>() {new OutcomeDiplomacyChange()
                {
                    PartyFaction = NewJericho,
                    TargetFaction = PhoenixPoint,
                    Value = 3,
                    PartyType = (OutcomeDiplomacyChange.ChangeTarget)1,
                },
                new OutcomeDiplomacyChange()
                        {
                            PartyFaction = Synedrion,
                            TargetFaction = PhoenixPoint,
                            Value = -6,
                            PartyType = (OutcomeDiplomacyChange.ChangeTarget)1,
                        }

                },
                                VariablesChange = originalGuidedByWhispersWin.GeoscapeEventData.Choices[0].Outcome.VariablesChange
                            },

                        });
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void ChangeGuidedByWhispers()
                {
                    try
                    {
                        //Add options to Guided by Whispers
                        //If relations with Synedrion Aligned, can opt for HD vs Pure
                        CreateNewNJOutcomePanel();
                        GuidedByWhispersLoss();
                        AddPeacefulOptions();
                        AddNewWinEvent();

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

            }

            internal class Templates
            {
                private static readonly TacticalItemDef juggHead = DefCache.GetDef<TacticalItemDef>("NJ_Jugg_BIO_Helmet_BodyPartDef");
                private static readonly TacticalItemDef juggLegs = DefCache.GetDef<TacticalItemDef>("NJ_Jugg_BIO_Legs_ItemDef");
                private static readonly TacticalItemDef juggTorso = DefCache.GetDef<TacticalItemDef>("NJ_Jugg_BIO_Torso_BodyPartDef");
                private static readonly TacticalItemDef exoHead = DefCache.GetDef<TacticalItemDef>("NJ_Exo_BIO_Helmet_BodyPartDef");
                private static readonly TacticalItemDef exoLegs = DefCache.GetDef<TacticalItemDef>("NJ_Exo_BIO_Legs_ItemDef");
                private static readonly TacticalItemDef exoTorso = DefCache.GetDef<TacticalItemDef>("NJ_Exo_BIO_Torso_BodyPartDef");
                private static readonly TacticalItemDef shinobiHead = DefCache.GetDef<TacticalItemDef>("SY_Shinobi_BIO_Helmet_BodyPartDef");
                private static readonly TacticalItemDef shinobiLegs = DefCache.GetDef<TacticalItemDef>("SY_Shinobi_BIO_Legs_ItemDef");
                private static readonly TacticalItemDef shinobiTorso = DefCache.GetDef<TacticalItemDef>("SY_Shinobi_BIO_Torso_BodyPartDef");

                private static readonly ClassTagDef _infiltratorTag = DefCache.GetDef<ClassTagDef>("Infiltrator_ClassTagDef");
                private static readonly ClassTagDef _technicianTag = DefCache.GetDef<ClassTagDef>("Technician_ClassTagDef");

                internal static void AdjustPureTemplates()
                {
                    try
                    {
                        // NJ_Jugg_BIO_Helmet_BodyPartDef

                        TacticalItemDef[] exoSet = new TacticalItemDef[] { exoHead, exoTorso, exoLegs };
                        TacticalItemDef[] shinobiSet = new TacticalItemDef[] { shinobiHead, shinobiTorso, shinobiLegs };

                        Dictionary<UnitTemplateResearchRewardDef, List<TacCharacterDef>> researchRewardsAndTemplatesToRemove = new Dictionary<UnitTemplateResearchRewardDef, List<TacCharacterDef>>();

                        ResearchDef neuralTech = DefCache.GetDef<ResearchDef>("NJ_NeuralTech_ResearchDef");
                        ResearchDef fireTech = DefCache.GetDef<ResearchDef>("NJ_PurificationTech_ResearchDef");
                        ResearchDef advTechnicianWeapons = DefCache.GetDef<ResearchDef>("NJ_PRCRTechTurret_ResearchDef");
                        ResearchDef piercingTech = DefCache.GetDef<ResearchDef>("NJ_PiercerTech_ResearchDef");


                        //replacing all starting templates with EXO bodyparts
                        foreach (ResearchRewardDef researchRewardDef in neuralTech.Unlocks.Where(rr => rr is UnitTemplateResearchRewardDef))
                        {
                            UnitTemplateResearchRewardDef templateReward = researchRewardDef as UnitTemplateResearchRewardDef;

                            if (templateReward.Template.Data.BodypartItems.Contains(juggHead))
                            {
                                templateReward.Template.Data.BodypartItems = exoSet;
                            }

                        }



                        //giving one of the first technician an exo suit 
                        TacCharacterDef technician2 = DefCache.GetDef<TacCharacterDef>("PU_Technician2_CharacterTemplateDef");
                        technician2.Data.BodypartItems = exoSet;


                        //removing "dumb" infiltrators
                        foreach (ResearchRewardDef researchRewardDef in fireTech.Unlocks.Where(rr => rr is UnitTemplateResearchRewardDef))
                        {
                            UnitTemplateResearchRewardDef templateReward = researchRewardDef as UnitTemplateResearchRewardDef;
                            {
                                if (templateReward.Template.ClassTag == _infiltratorTag)
                                {
                                    if (researchRewardsAndTemplatesToRemove.ContainsKey(templateReward))
                                    {
                                        researchRewardsAndTemplatesToRemove[templateReward].Add(templateReward.Template);
                                    }
                                    else
                                    {
                                        researchRewardsAndTemplatesToRemove.Add(templateReward, new List<TacCharacterDef> { templateReward.Template });
                                    }
                                }
                            }
                        }
                    

                        List<ResearchRewardDef> fireRewards = new List<ResearchRewardDef>(fireTech.Unlocks);

                        foreach (UnitTemplateResearchRewardDef templateReward in researchRewardsAndTemplatesToRemove.Keys)
                        {
                            if (fireRewards.Contains(templateReward))
                            {
                                fireRewards.Remove(templateReward);
                            }
                        }

                        fireTech.Unlocks = fireRewards.ToArray();


                        //Moving all the templates from adv tech weapons to pierce, except Technicians

                        //collect the templates
                        List<UnitTemplateResearchRewardDef> piercingUnitRewards = new List<UnitTemplateResearchRewardDef>();
                        foreach (ResearchRewardDef researchRewardDef in advTechnicianWeapons.Unlocks.Where(rr => rr is UnitTemplateResearchRewardDef))
                        {
                            UnitTemplateResearchRewardDef templateReward = researchRewardDef as UnitTemplateResearchRewardDef;
                            {

                                if (templateReward.Template.ClassTag != _technicianTag
                                    && templateReward.Template.Data.BodypartItems.Any(bp => bp.Tags.Contains(Shared.SharedGameTags.BionicalTag)))
                                {
                                    piercingUnitRewards.Add(templateReward);
                                }
                            }
                        }
                      
                        //remove from adv tech
                        List<ResearchRewardDef> advTechRewards = new List<ResearchRewardDef>(advTechnicianWeapons.Unlocks);
                        advTechRewards.RemoveRange(piercingUnitRewards);
                        advTechnicianWeapons.Unlocks = advTechRewards.ToArray();

                        //add to pierce
                        List<ResearchRewardDef> pierceRewards = new List<ResearchRewardDef>(piercingTech.Unlocks);
                        pierceRewards.AddRange(piercingUnitRewards);
                        piercingTech.Unlocks = pierceRewards.ToArray();

                       
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }


                private static readonly TacticalItemDef heavyHead = DefCache.GetDef<TacticalItemDef>("AN_Berserker_Heavy_Helmet_BodyPartDef");
                private static readonly TacticalItemDef heavyLegs = DefCache.GetDef<TacticalItemDef>("AN_Berserker_Heavy_Legs_ItemDef");
                private static readonly TacticalItemDef heavyTorso = DefCache.GetDef<TacticalItemDef>("AN_Berserker_Heavy_Torso_BodyPartDef");
                private static readonly TacticalItemDef watcherHead = DefCache.GetDef<TacticalItemDef>("AN_Berserker_Watcher_Helmet_BodyPartDef");
                private static readonly TacticalItemDef watcherLegs = DefCache.GetDef<TacticalItemDef>("AN_Berserker_Watcher_Legs_ItemDef");
                private static readonly TacticalItemDef watcherTorso = DefCache.GetDef<TacticalItemDef>("AN_Berserker_Watcher_Torso_BodyPartDef");
                private static readonly TacticalItemDef shooterHead = DefCache.GetDef<TacticalItemDef>("AN_Berserker_Shooter_Helmet_BodyPartDef");
                private static readonly TacticalItemDef shooterLegs = DefCache.GetDef<TacticalItemDef>("AN_Berserker_Shooter_Legs_ItemDef");
                private static readonly TacticalItemDef shooterTorso = DefCache.GetDef<TacticalItemDef>("AN_Berserker_Shooter_Torso_BodyPartDef");

                internal static void AdjustForsakenTemplates()
                {
                    try
                    {
                        //AN_Priest_Head01_BodyPartDef synod head 
                        //AN_Priest_Head02_BodyPartDef frenzy head
                        //AN_Priest_Head03_BodyPartDef screaming head

                        WeaponDef bulldog = DefCache.GetDef<WeaponDef>("NJ_Gauss_AssaultRifle_WeaponDef");
                        ItemDef medkit = DefCache.GetDef<TacticalItemDef>("Medkit_EquipmentDef");

                        List<ItemDef> assaultRifleSet = new List<ItemDef>() { bulldog, bulldog.CompatibleAmmunition[0], medkit };
                        List<ItemDef> watcherArmorSet = new List<ItemDef>() { watcherHead, watcherTorso, watcherLegs }; //tentacle torso
                        List<ItemDef> shooterArmorSet = new List<ItemDef>() { shooterHead, shooterTorso, shooterLegs }; //shoots spikes

                        //Replace first assault armor with lighter one
                        TacCharacterDef fkAssault1 = DefCache.GetDef<TacCharacterDef>("FK_Assault1_CharacterTemplateDef");
                        fkAssault1.Data.BodypartItems = watcherArmorSet.ToArray();
                        //Replace Shotgun with Bulldog on one of the first assaults
                        TacCharacterDef fkAssault2 = DefCache.GetDef<TacCharacterDef>("FK_Assault2_CharacterTemplateDef");
                        fkAssault2.Data.EquipmentItems = assaultRifleSet.ToArray();


                        //Replace first berserkers armor with lighter one
                        TacCharacterDef berserker1 = DefCache.GetDef<TacCharacterDef>("FK_Berserker2_CharacterTemplateDef");
                        berserker1.Data.BodypartItems = shooterArmorSet.ToArray();

                        TacCharacterDef berserker2 = DefCache.GetDef<TacCharacterDef>("FK_Berserker1_CharacterTemplateDef");
                        berserker2.Data.BodypartItems = watcherArmorSet.ToArray();

                        ResearchDef acidTech = DefCache.GetDef<ResearchDef>("ANU_AcidTech_ResearchDef");

                        List<TacCharacterDef> fkMutogs = new List<TacCharacterDef>() {

                        DefCache.GetDef<TacCharacterDef>("FK_Mutog_PoisonAgileBasher_CharacterTemplateDef"),
                        DefCache.GetDef<TacCharacterDef>("FK_Mutog_PoisonAgileBladed_CharacterTemplateDef"),
                        DefCache.GetDef<TacCharacterDef>("FK_Mutog_PoisonRegenBasher_CharacterTemplateDef"),
                        DefCache.GetDef<TacCharacterDef>("FK_Mutog_PoisonRegenBladed_CharacterTemplateDef"),
                        DefCache.GetDef<TacCharacterDef>("FK_Mutog_RamAgileBasher_CharacterTemplateDef"),
                        DefCache.GetDef<TacCharacterDef>("FK_Mutog_RamAgileBladed_CharacterTemplateDef"),
                        DefCache.GetDef<TacCharacterDef>("FK_Mutog_RamRegenBasher_CharacterTemplateDef"),
                        DefCache.GetDef<TacCharacterDef>("FK_Mutog_RamRegenBladed_CharacterTemplateDef")
                    };

                        List<string> guids = new List<string>()
                        {
                            "{BBD9F470-5C4E-4180-8788-6FF9AD675FF0}",
                            "{F4F10F7D-4C03-420B-8737-CDF41153A267}",
                            "{DB3B30A9-37E5-4448-9CD7-1F7156107C05}",
                            "{BDA563D6-3B5E-4A08-B1E8-1339D923477F}",
                            "{AF590EED-8CC6-4BF3-810A-5F4895167F4E}",
                            "{5E441956-751E-4B88-AC2F-EFE24C8E4696}",
                            "{4BAA78C2-233F-433B-89BD-E34CEE392540}",
                            "{BC2AAC5F-B501-44AC-A180-995EDD8E81DF}"
                        };

                        for (int x = 0; x < fkMutogs.Count; x++)
                        {
                            CreateUnitResearchReward(acidTech, guids[x], fkMutogs[x]);

                        }
                     /*   ClassTagDef berserkerTag = DefCache.GetDef<ClassTagDef>("Berserker_ClassTagDef");
                        ItemClassificationTagDef gunWeaponTag = DefCache.GetDef<ItemClassificationTagDef>("GunWeapon_TagDef");
                        ItemTypeTagDef ammoTag= DefCache.GetDef<ItemTypeTagDef>("AmmoItem_TagDef");

                        foreach(TacCharacterDef tacCharacterDef in Repo.GetAllDefs<TacCharacterDef>().Where(tc => tc.ClassTag == berserkerTag)) 
                        {
                            TFTVLogger.Always($"looking at {tacCharacterDef.name}");

                            List<ItemDef> equipmentList = new List<ItemDef>(tacCharacterDef.Data.EquipmentItems);
                            List<ItemDef> equipmentToRemove = new List<ItemDef>();

                            foreach(ItemDef itemDef in equipmentList) 
                            {
                                if (itemDef.Tags.Contains(gunWeaponTag)|| itemDef.Tags.Contains(ammoTag)) 
                                {
                                    TFTVLogger.Always($"{itemDef.name} will be removed from {tacCharacterDef.name} equipment list");
                                    equipmentToRemove.Add(itemDef);                               
                                } 
                            }

                            equipmentList.RemoveRange(equipmentToRemove);
                            tacCharacterDef.Data.EquipmentItems = equipmentList.ToArray();
                        
                        }*/

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }

                }

                private static void CreateUnitResearchReward(ResearchDef researchDef, string guid, TacCharacterDef tacCharacterDef)
                {
                    try
                    {
                        UnitTemplateResearchRewardDef source = DefCache.GetDef<UnitTemplateResearchRewardDef>("ANU_MutogTech_ResearchDef_UnitTemplateResearchRewardDef_0");
                        UnitTemplateResearchRewardDef newReward = Helper.CreateDefFromClone(source, guid, $"{researchDef.Id}_UnitTemplateResearchRewardDef_{tacCharacterDef.name}");
                        newReward.Template = tacCharacterDef;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }




            }
        }

        public static void CheckMissionVSPure(GeoMission geoMission)
        {
            try
            {
                foreach (TacMissionTypeParticipantData tacMissionTypeParticipantData in geoMission.MissionDef.ParticipantsData)
                {
                    if (tacMissionTypeParticipantData.FactionDef == DefCache.GetDef<PPFactionDef>("NJ_Purists_FactionDef"))
                    {

                        GeoscapeEventSystem eventSystem = geoMission.Site.GeoLevel.EventSystem;
                        eventSystem.SetVariable(_puAmbushVariable, eventSystem.GetVariable(_puAmbushVariable) + 1);

                        TFTVLogger.Always($"Completed mission vs the Pure, adding + 1 to {_puAmbushVariable} so it's now {eventSystem.GetVariable(_puAmbushVariable)}");

                        if (eventSystem.GetVariable(_puAmbushVariable) >= 1 && eventSystem.GetEventRecord(_OlenaOnPureEvent.EventID)!= null && eventSystem.GetEventRecord(_OlenaOnPureEvent.EventID).TriggerCount == 0)
                        {
                            GeoPhoenixFaction phoenix = geoMission.Site.GeoLevel.PhoenixFaction;
                            eventSystem.TriggerGeoscapeEvent(_OlenaOnPureEvent.EventID, new GeoscapeEventContext(geoMission.Site, phoenix));
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
}
