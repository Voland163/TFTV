using Base;
using Base.Core;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using System;

namespace TFTV
{
    internal class TFTVDiplomacyPenalties
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        public static bool VoidOmensImplemented = false;




        //The Strates Solution
        public static void CheckPostponedFactionMissions(GeoFaction faction, PartyDiplomacy.Relation relation, int newValue)
        {
            GeoscapeEventSystem eventSystem = faction.GeoLevel.EventSystem; // endless dereferencing hurts my poor soul
                                                                            //  TFTVLogger.Always("Diplomacy changed, CheckPostponedFactionMissions invoked");
            try
            {

                if (!TFTVNewGameOptions.DiplomaticPenaltiesSetting)
                {
                    return;
                }

                GeoFaction targetFaction = faction.GeoLevel.GetFaction((PPFactionDef)relation.WithParty);
                GeoscapeEventContext geoscapeEventContext = new GeoscapeEventContext(targetFaction, faction.GeoLevel.ViewerFaction);

                if (faction.GetParticipatingFaction() == faction.GeoLevel.AnuFaction && targetFaction == faction.GeoLevel.PhoenixFaction)
                {
                    /* TFTVLogger.Always("The record for event PROG_AN2 states that choice " + eventSystem.GetEventRecord("PROG_AN2")?.SelectedChoice + " was chosen");
                     TFTVLogger.Always("The record for event PROG_AN4 states that choice " + eventSystem.GetEventRecord("PROG_AN4")?.SelectedChoice + " was chosen");
                     TFTVLogger.Always("The record for event PROG_AN6 states that choice " + eventSystem.GetEventRecord("PROG_AN6")?.SelectedChoice + " was chosen");
                     TFTVLogger.Always("The record shows PROG_AN4 was completed on " + eventSystem.GetEventRecord("PROG_AN4")?.CompletedAt + " it is now " + faction.GeoLevel.Timing.Now);
                    */
                    // GetEventRecord can return null, implying that this event has never spawned. Not sure that should happen in postpone check, but the choice conditional will be false either way
                    if (newValue == 24 && eventSystem.GetEventRecord("PROG_AN2")?.SelectedChoice == 0) // choice 0 is postpone for this event, according to TFTVDefsWithConfigDependency.cs
                    {
                        eventSystem.TriggerGeoscapeEvent("PROG_AN2", geoscapeEventContext);
                    }
                    else if (newValue == 49 && eventSystem.GetEventRecord("PROG_AN4")?.SelectedChoice == 1)
                    {
                        eventSystem.TriggerGeoscapeEvent("PROG_AN4", geoscapeEventContext);
                    }
                    else if (newValue == 74 && eventSystem.GetEventRecord("PROG_AN6")?.SelectedChoice == 2)
                    {
                        eventSystem.TriggerGeoscapeEvent("PROG_AN6", geoscapeEventContext);
                    }
                }
                else if (faction.GetParticipatingFaction() == faction.GeoLevel.NewJerichoFaction && targetFaction == faction.GeoLevel.PhoenixFaction)
                {
                    /* TFTVLogger.Always("The record for event PROG_NJ1 states that choice " + eventSystem.GetEventRecord("PROG_NJ1")?.SelectedChoice + " was chosen");
                     TFTVLogger.Always("The record for event PROG_NJ2 states that choice " + eventSystem.GetEventRecord("PROG_NJ2")?.SelectedChoice + " was chosen");
                     TFTVLogger.Always("The record for event PROG_NJ3 states that choice " + eventSystem.GetEventRecord("PROG_NJ3")?.SelectedChoice + " was chosen");*/

                    if (newValue == 24 && eventSystem.GetEventRecord("PROG_NJ1")?.SelectedChoice == 1)
                    {
                        eventSystem.TriggerGeoscapeEvent("PROG_NJ1", geoscapeEventContext);
                    }
                    else if (newValue == 49 && eventSystem.GetEventRecord("PROG_NJ2")?.SelectedChoice == 1)
                    {
                        eventSystem.TriggerGeoscapeEvent("PROG_NJ2", geoscapeEventContext);
                    }
                    else if (newValue == 74 && eventSystem.GetEventRecord("PROG_NJ3")?.SelectedChoice == 1)
                    {
                        eventSystem.TriggerGeoscapeEvent("PROG_NJ3", geoscapeEventContext);
                    }
                }
                else if (faction.GetParticipatingFaction() == faction.GeoLevel.SynedrionFaction && targetFaction == faction.GeoLevel.PhoenixFaction)
                {
                    /*  TFTVLogger.Always("The record for event PROG_SY1 states that choice " + eventSystem.GetEventRecord("PROG_SY1")?.SelectedChoice + " was chosen");
                      TFTVLogger.Always("The record for event PROG_SY4_P states that choice " + eventSystem.GetEventRecord("PROG_SY4_P")?.SelectedChoice + " was chosen");
                      TFTVLogger.Always("The record for event PROG_SY4_T states that choice " + eventSystem.GetEventRecord("PROG_SY4_T")?.SelectedChoice + " was chosen");*/


                    if (newValue == 24 && eventSystem.GetEventRecord("PROG_SY1")?.SelectedChoice == 2)
                    {
                        eventSystem.TriggerGeoscapeEvent("PROG_SY1", geoscapeEventContext);
                    }
                    else if (newValue == 74)
                    {
                        if (eventSystem.GetVariable("Polyphonic") > eventSystem.GetVariable("Terraformers"))
                        {
                            if (eventSystem.GetEventRecord("PROG_SY4_P")?.SelectedChoice == 1)
                            {
                                eventSystem.TriggerGeoscapeEvent("PROG_SY4_P", geoscapeEventContext);
                            }
                        }
                        else
                        {
                            if (eventSystem.GetEventRecord("PROG_SY4_T")?.SelectedChoice == 1)
                            {
                                eventSystem.TriggerGeoscapeEvent("PROG_SY4_T", geoscapeEventContext);
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


        public static void ImplementDiplomaticPenalties(GeoscapeEventData @event, GeoscapeEvent geoscapeEvent)
        {
            try
            {
                //  TFTVConfig config = TFTVMain.Main.Config;

                // GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                if (TFTVNewGameOptions.DiplomaticPenaltiesSetting)
                {
                    GeoFactionDef PhoenixPoint = DefCache.GetDef<GeoFactionDef>("Phoenix_GeoPhoenixFactionDef");
                    GeoFactionDef NewJericho = DefCache.GetDef<GeoFactionDef>("NewJericho_GeoFactionDef");
                    GeoFactionDef Anu = DefCache.GetDef<GeoFactionDef>("Anu_GeoFactionDef");
                    GeoFactionDef Synedrion = DefCache.GetDef<GeoFactionDef>("Synedrion_GeoFactionDef");

                    GeoscapeEventDef ProgAnuSupportive = DefCache.GetDef<GeoscapeEventDef>("PROG_AN2_GeoscapeEventDef");
                    GeoscapeEventDef ProgNJSupportive = DefCache.GetDef<GeoscapeEventDef>("PROG_NJ1_GeoscapeEventDef");
                    GeoscapeEventDef ProgSynSupportive = DefCache.GetDef<GeoscapeEventDef>("PROG_SY1_GeoscapeEventDef");

                    GeoscapeEventDef ProgAnuPact = DefCache.GetDef<GeoscapeEventDef>("PROG_AN4_GeoscapeEventDef");
                    GeoscapeEventDef ProgNJPact = DefCache.GetDef<GeoscapeEventDef>("PROG_NJ2_GeoscapeEventDef");
                    GeoscapeEventDef ProgSynPact = DefCache.GetDef<GeoscapeEventDef>("PROG_SY3_WIN_GeoscapeEventDef");


                    GeoscapeEventDef ProgAnuAlliance = DefCache.GetDef<GeoscapeEventDef>("PROG_AN6_GeoscapeEventDef");
                    GeoscapeEventDef ProgAnuAllianceNoSynod = DefCache.GetDef<GeoscapeEventDef>("PROG_AN6_2_GeoscapeEventDef");
                    GeoscapeEventDef ProgNJAlliance = DefCache.GetDef<GeoscapeEventDef>("PROG_NJ3_GeoscapeEventDef");
                    GeoscapeEventDef ProgSynAllianceTerra = DefCache.GetDef<GeoscapeEventDef>("PROG_SY4_T_GeoscapeEventDef");
                    GeoscapeEventDef ProgSynAlliancePoly = DefCache.GetDef<GeoscapeEventDef>("PROG_SY4_P_GeoscapeEventDef");

                    string eventID = @event?.EventID ?? geoscapeEvent.EventID;

                    if (eventID == ProgAnuSupportive.EventID)
                    {
                        ProgAnuSupportive.GeoscapeEventData.Choices[0].Outcome.SetDiplomaticObjectives.Clear();
                        ProgAnuSupportive.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -4));
                        ProgAnuSupportive.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -10));
                        ProgAnuSupportive.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -10));
                        TFTVLogger.Always("Harder diplomacy is on, changing event " + @event.EventID);
                    }
                    else if (eventID == ProgAnuPact.EventID)
                    {
                        ProgAnuPact.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -15));
                        ProgAnuPact.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -15));
                        TFTVCommonMethods.GenerateGeoEventChoice(ProgAnuPact, "PROG_AN4_CHOICE_1_TEXT", "PROG_AN4_CHOICE_1_OUTCOME_GENERAL");
                        ProgAnuPact.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -6));
                        TFTVLogger.Always("Harder diplomacy is on, changing event " + @event.EventID);
                    }
                    else if (eventID == ProgAnuAlliance.EventID)
                    {
                        ProgAnuAlliance.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -10));
                        ProgAnuAlliance.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -20));
                        ProgAnuAlliance.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -15));
                        TFTVCommonMethods.GenerateGeoEventChoice(ProgAnuAlliance, "PROG_AN4_CHOICE_1_TEXT", "PROG_AN4_CHOICE_1_OUTCOME_GENERAL");
                        ProgAnuAlliance.GeoscapeEventData.Choices[2].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -8));
                        TFTVLogger.Always("Harder diplomacy is on, changing event " + @event.EventID);
                    }
                    else if (eventID == ProgAnuAllianceNoSynod.EventID)
                    {
                        TFTVCommonMethods.GenerateGeoEventChoice(ProgAnuAllianceNoSynod, "PROG_AN4_CHOICE_1_TEXT", "PROG_AN4_CHOICE_1_OUTCOME_GENERAL");
                        ProgAnuAllianceNoSynod.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -8));
                        TFTVLogger.Always("Harder diplomacy is on, changing event " + @event.EventID);
                    }

                    else if (eventID == ProgSynSupportive.EventID)
                    {
                        //Synedrion
                        //Supportive Polyphonic
                        ProgSynSupportive.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -15));
                        ProgSynSupportive.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -5));

                        //Supportive Terra
                        ProgSynSupportive.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -15));
                        ProgSynSupportive.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -5));

                        //Postpone
                        TFTVCommonMethods.GenerateGeoEventChoice(ProgSynSupportive, "PROG_SY_POSTPONE_CHOICE", "PROG_SY_POSTPONE_TEXT");
                        ProgSynSupportive.GeoscapeEventData.Choices[2].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -4));
                        TFTVLogger.Always("Harder diplomacy is on, changing event " + @event.EventID);
                    }
                    else if (eventID == ProgSynPact.EventID)
                    {

                        //Aligned
                        ProgSynPact.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -18));
                        ProgSynPact.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -18));
                        ProgSynPact.GeoscapeEventData.Choices[2].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -15));
                        ProgSynPact.GeoscapeEventData.Choices[2].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -15));
                        TFTVLogger.Always("Harder diplomacy is on, changing event " + @event.EventID);
                    }
                    else if (eventID == ProgSynAlliancePoly.EventID)
                    {
                        //Aliance Polyphonic             
                        ProgSynAlliancePoly.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -8));
                        TFTVCommonMethods.GenerateGeoEventChoice(ProgSynAlliancePoly, "PROG_SY_POSTPONE_CHOICE", "PROG_SY_POSTPONE_TEXT");
                        ProgSynAlliancePoly.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -8));
                        TFTVLogger.Always("Harder diplomacy is on, changing event " + @event.EventID);
                    }

                    else if (eventID == ProgSynAllianceTerra.EventID)
                    {
                        //Alliance Terra
                        ProgSynAllianceTerra.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -20));
                        TFTVCommonMethods.GenerateGeoEventChoice(ProgSynAllianceTerra, "PROG_SY_POSTPONE_CHOICE", "PROG_SY_POSTPONE_TEXT");
                        ProgSynAllianceTerra.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -8));
                        TFTVLogger.Always("Harder diplomacy is on, changing event " + @event.EventID);
                    }
                    else if (eventID == ProgNJSupportive.EventID)
                    {
                        ProgNJSupportive.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -10));
                        ProgNJSupportive.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -10));
                        TFTVCommonMethods.GenerateGeoEventChoice(ProgNJSupportive, "PROG_NJ_POSTPONE_CHOICE", "PROG_NJ_POSTPONE_TEXT");
                        ProgNJSupportive.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -4));
                        TFTVLogger.Always("Harder diplomacy is on, changing event " + @event.EventID);
                    }
                    else if (eventID == ProgNJPact.EventID)
                    {
                        ProgNJPact.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -15));
                        ProgNJPact.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -15));
                        TFTVCommonMethods.GenerateGeoEventChoice(ProgNJPact, "PROG_NJ_POSTPONE_CHOICE", "PROG_NJ_POSTPONE_TEXT");
                        ProgNJPact.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -6));
                        TFTVLogger.Always("Harder diplomacy is on, changing event " + @event.EventID);
                    }

                    else if (eventID == ProgNJAlliance.EventID)
                    {
                        ProgNJAlliance.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -20));
                        ProgNJAlliance.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -20));
                        TFTVCommonMethods.GenerateGeoEventChoice(ProgNJAlliance, "PROG_NJ_POSTPONE_CHOICE", "PROG_NJ_POSTPONE_TEXT");
                        ProgNJAlliance.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -8));
                        TFTVLogger.Always("Harder diplomacy is on, changing event " + @event.EventID);
                    }
                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void RestoreStateDiplomaticPenalties(GeoscapeEvent __instance)
        {

            try
            {

                // TFTVConfig config = TFTVMain.Main.Config;
                GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                if (TFTVNewGameOptions.DiplomaticPenaltiesSetting)
                {

                    GeoFactionDef PhoenixPoint = DefCache.GetDef<GeoFactionDef>("Phoenix_GeoPhoenixFactionDef");
                    GeoFactionDef NewJericho = DefCache.GetDef<GeoFactionDef>("NewJericho_GeoFactionDef");
                    GeoFactionDef Anu = DefCache.GetDef<GeoFactionDef>("Anu_GeoFactionDef");
                    GeoFactionDef Synedrion = DefCache.GetDef<GeoFactionDef>("Synedrion_GeoFactionDef");

                    GeoscapeEventDef ProgAnuSupportive = DefCache.GetDef<GeoscapeEventDef>("PROG_AN2_GeoscapeEventDef");
                    GeoscapeEventDef ProgNJSupportive = DefCache.GetDef<GeoscapeEventDef>("PROG_NJ1_GeoscapeEventDef");
                    GeoscapeEventDef ProgSynSupportive = DefCache.GetDef<GeoscapeEventDef>("PROG_SY1_GeoscapeEventDef");

                    GeoscapeEventDef ProgAnuPact = DefCache.GetDef<GeoscapeEventDef>("PROG_AN4_GeoscapeEventDef");
                    GeoscapeEventDef ProgNJPact = DefCache.GetDef<GeoscapeEventDef>("PROG_NJ2_GeoscapeEventDef");
                    GeoscapeEventDef ProgSynPact = DefCache.GetDef<GeoscapeEventDef>("PROG_SY3_WIN_GeoscapeEventDef");


                    GeoscapeEventDef ProgAnuAlliance = DefCache.GetDef<GeoscapeEventDef>("PROG_AN6_GeoscapeEventDef");
                    GeoscapeEventDef ProgAnuAllianceNoSynod = DefCache.GetDef<GeoscapeEventDef>("PROG_AN6_2_GeoscapeEventDef");
                    GeoscapeEventDef ProgNJAlliance = DefCache.GetDef<GeoscapeEventDef>("PROG_NJ3_GeoscapeEventDef");
                    GeoscapeEventDef ProgSynAllianceTerra = DefCache.GetDef<GeoscapeEventDef>("PROG_SY4_T_GeoscapeEventDef");
                    GeoscapeEventDef ProgSynAlliancePoly = DefCache.GetDef<GeoscapeEventDef>("PROG_SY4_P_GeoscapeEventDef");

                    if (__instance.EventID == ProgAnuSupportive.EventID)
                    {

                        ProgAnuSupportive.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Clear();
                        ProgAnuSupportive.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Clear();

                        OutcomeSetDiplomaticObjective copyObjective = ProgAnuSupportive.GeoscapeEventData.Choices[1].Outcome.SetDiplomaticObjectives[0];
                        OutcomeSetDiplomaticObjective outcomeSetDiplomaticObjective = new OutcomeSetDiplomaticObjective() { Description = copyObjective.Description, EventID = copyObjective.EventID, WithFaction = copyObjective.WithFaction };
                        ProgAnuSupportive.GeoscapeEventData.Choices[0].Outcome.SetDiplomaticObjectives.Add(outcomeSetDiplomaticObjective);
                        TFTVLogger.Always("Harder diplomacy is on, changing event " + __instance.EventID + " back to keep things nice and tidy");
                    }
                    else if (__instance.EventID == ProgAnuPact.EventID)
                    {
                        ProgAnuPact.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Clear();
                        ProgAnuPact.GeoscapeEventData.Choices.RemoveLast();
                        TFTVLogger.Always("Harder diplomacy is on, changing event " + __instance.EventID + " back to keep things nice and tidy");

                    }
                    else if (__instance.EventID == ProgAnuAlliance.EventID)
                    {
                        ProgAnuAlliance.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Clear();
                        ProgAnuAlliance.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Clear();
                        ProgAnuAlliance.GeoscapeEventData.Choices.RemoveLast();
                        TFTVLogger.Always("Harder diplomacy is on, changing event " + __instance.EventID + " back to keep things nice and tidy");
                    }
                    else if (__instance.EventID == ProgAnuAllianceNoSynod.EventID)
                    {
                        ProgAnuAllianceNoSynod.GeoscapeEventData.Choices.RemoveLast();
                        TFTVLogger.Always("Harder diplomacy is on, changing event " + __instance.EventID + " back to keep things nice and tidy");
                    }

                    else if (__instance.EventID == ProgSynSupportive.EventID)
                    {

                        //Synedrion
                        //Supportive Polyphonic
                        ProgSynSupportive.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Clear();

                        //Supportive Terra
                        ProgSynSupportive.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Clear();

                        //Postpone
                        ProgSynSupportive.GeoscapeEventData.Choices.RemoveLast();
                        TFTVLogger.Always("Harder diplomacy is on, changing event " + __instance.EventID + " back to keep things nice and tidy");
                    }
                    else if (__instance.EventID == ProgSynPact.EventID)
                    {

                        //Aligned
                        ProgSynPact.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Clear();
                        ProgSynPact.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Clear();
                        ProgSynPact.GeoscapeEventData.Choices[2].Outcome.Diplomacy.Clear();
                        TFTVLogger.Always("Harder diplomacy is on, changing event " + __instance.EventID + " back to keep things nice and tidy");
                    }
                    else if (__instance.EventID == ProgSynAlliancePoly.EventID)
                    {
                        //Aliance Polyphonic             
                        ProgSynAlliancePoly.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Clear();
                        ProgSynAlliancePoly.GeoscapeEventData.Choices.RemoveLast();
                        TFTVLogger.Always("Harder diplomacy is on, changing event " + __instance.EventID + " back to keep things nice and tidy");
                    }

                    else if (__instance.EventID == ProgSynAllianceTerra.EventID)
                    {
                        //Alliance Terra
                        ProgSynAllianceTerra.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Clear();
                        ProgSynAllianceTerra.GeoscapeEventData.Choices.RemoveLast();
                        TFTVLogger.Always("Harder diplomacy is on, changing event " + __instance.EventID + " back to keep things nice and tidy");
                    }
                    else if (__instance.EventID == ProgNJSupportive.EventID)
                    {
                        ProgNJSupportive.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Clear();
                        ProgNJSupportive.GeoscapeEventData.Choices.RemoveLast();
                        TFTVLogger.Always("Harder diplomacy is on, changing event " + __instance.EventID + " back to keep things nice and tidy");
                    }
                    else if (__instance.EventID == ProgNJPact.EventID)
                    {
                        ProgNJPact.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Clear();
                        ProgNJPact.GeoscapeEventData.Choices.RemoveLast();
                        TFTVLogger.Always("Harder diplomacy is on, changing event " + __instance.EventID + " back to keep things nice and tidy");
                    }

                    else if (__instance.EventID == ProgNJAlliance.EventID)
                    {
                        ProgNJAlliance.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Clear();
                        ProgNJAlliance.GeoscapeEventData.Choices.RemoveLast();
                        TFTVLogger.Always("Harder diplomacy is on, changing event " + __instance.EventID + " back to keep things nice and tidy");
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