using Base;
using Base.Core;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV
{
    internal class TFTVDiplomacyPenalties
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        public static bool VoidOmensImplemented = false;

        /// <summary>
        /// What a PROG event def looked like before ImplementDiplomaticPenalties changed it. The defs are process-global,
        /// so this both stops a second patch while one is live (reload with the popup open, or the event firing again
        /// before CompleteEvent) and lets Restore put back exactly what was there, vanilla outcomes included.
        /// </summary>
        private sealed class PatchedEventSnapshot
        {
            public List<GeoEventChoice> Choices;
            public Dictionary<GeoEventChoice, List<OutcomeDiplomacyChange>> Diplomacy = new Dictionary<GeoEventChoice, List<OutcomeDiplomacyChange>>();
            public Dictionary<GeoEventChoice, List<OutcomeSetDiplomaticObjective>> DiplomaticObjectives = new Dictionary<GeoEventChoice, List<OutcomeSetDiplomaticObjective>>();
        }

        private static readonly Dictionary<string, PatchedEventSnapshot> _patchedEvents = new Dictionary<string, PatchedEventSnapshot>();

        private static readonly string[] _penaltyEventDefNames =
        {
            "PROG_AN2_GeoscapeEventDef", "PROG_NJ1_GeoscapeEventDef", "PROG_SY1_GeoscapeEventDef",
            "PROG_AN4_GeoscapeEventDef", "PROG_NJ2_GeoscapeEventDef", "PROG_SY3_WIN_GeoscapeEventDef",
            "PROG_AN6_GeoscapeEventDef", "PROG_AN6_2_GeoscapeEventDef", "PROG_NJ3_GeoscapeEventDef",
            "PROG_SY4_T_GeoscapeEventDef", "PROG_SY4_P_GeoscapeEventDef"
        };

        private static PatchedEventSnapshot TakeSnapshot(GeoscapeEventDef eventDef)
        {
            PatchedEventSnapshot snapshot = new PatchedEventSnapshot
            {
                Choices = new List<GeoEventChoice>(eventDef.GeoscapeEventData.Choices)
            };

            foreach (GeoEventChoice choice in snapshot.Choices)
            {
                if (choice?.Outcome == null)
                {
                    continue;
                }

                if (choice.Outcome.Diplomacy != null)
                {
                    snapshot.Diplomacy[choice] = new List<OutcomeDiplomacyChange>(choice.Outcome.Diplomacy);
                }

                if (choice.Outcome.SetDiplomaticObjectives != null)
                {
                    snapshot.DiplomaticObjectives[choice] = new List<OutcomeSetDiplomaticObjective>(choice.Outcome.SetDiplomaticObjectives);
                }
            }

            return snapshot;
        }

        private static void RestoreSnapshot(GeoscapeEventDef eventDef, PatchedEventSnapshot snapshot)
        {
            List<GeoEventChoice> choices = eventDef.GeoscapeEventData.Choices;
            choices.Clear();
            choices.AddRange(snapshot.Choices);

            foreach (KeyValuePair<GeoEventChoice, List<OutcomeDiplomacyChange>> entry in snapshot.Diplomacy)
            {
                entry.Key.Outcome.Diplomacy.Clear();
                entry.Key.Outcome.Diplomacy.AddRange(entry.Value);
            }

            foreach (KeyValuePair<GeoEventChoice, List<OutcomeSetDiplomaticObjective>> entry in snapshot.DiplomaticObjectives)
            {
                entry.Key.Outcome.SetDiplomaticObjectives.Clear();
                entry.Key.Outcome.SetDiplomaticObjectives.AddRange(entry.Value);
            }
        }




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
                  //  TFTVLogger.Always("The record for event PROG_AN2 states that choice " + eventSystem.GetEventRecord("PROG_AN2")?.SelectedChoice + " was chosen");
                  //   TFTVLogger.Always("The record for event PROG_AN4 states that choice " + eventSystem.GetEventRecord("PROG_AN4")?.SelectedChoice + " was chosen");
                  //   TFTVLogger.Always("The record for event PROG_AN6 states that choice " + eventSystem.GetEventRecord("PROG_AN6")?.SelectedChoice + " was chosen");
                  //   TFTVLogger.Always("The record shows PROG_AN4 was completed on " + eventSystem.GetEventRecord("PROG_AN4")?.CompletedAt + " it is now " + faction.GeoLevel.Timing.Now);
                    
                    // GetEventRecord can return null, implying that this event has never spawned. Not sure that should happen in postpone check, but the choice conditional will be false either way
                    if (newValue == 24 && eventSystem.GetEventRecord("PROG_AN2")?.SelectedChoice == 0) // choice 0 is postpone for this event, according to TFTVDefsWithConfigDependency.cs
                    {
                        eventSystem.TriggerGeoscapeEvent("PROG_AN2", geoscapeEventContext);
                    }
                    else if (newValue == 49 && eventSystem.GetEventRecord("PROG_AN4")?.SelectedChoice == 1)
                    {
                        eventSystem.TriggerGeoscapeEvent("PROG_AN4", geoscapeEventContext);
                    }
                    else if (newValue == 74 && (eventSystem.GetEventRecord("PROG_AN6")?.SelectedChoice == 2
                        || (eventSystem.GetEventRecord("PROG_AN6")?.SelectedChoice == 1 && eventSystem.GetEventRecord("PROG_AN6_2")?.SelectedChoice == 1)))
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
                        if (eventSystem.GetEventRecord("PROG_SY4_P")?.SelectedChoice == 1)
                        {
                            eventSystem.TriggerGeoscapeEvent("PROG_SY4_P", geoscapeEventContext);
                        }
                        else if(eventSystem.GetEventRecord("PROG_SY4_T")?.SelectedChoice == 1)
                        {
                            eventSystem.TriggerGeoscapeEvent("PROG_SY4_T", geoscapeEventContext);
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

                    GeoscapeEventDef penaltyEventDef = _penaltyEventDefNames
                        .Select(name => DefCache.GetDef<GeoscapeEventDef>(name))
                        .FirstOrDefault(def => def.EventID == eventID);

                    if (penaltyEventDef == null)
                    {
                        return;
                    }

                    if (_patchedEvents.ContainsKey(eventID))
                    {
                        // already patched and not yet restored: patching again would add a second postpone choice
                        // and double the penalties
                        return;
                    }

                    _patchedEvents[eventID] = TakeSnapshot(penaltyEventDef);

                    if (eventID == ProgAnuSupportive.EventID)
                    {
                        ProgAnuSupportive.GeoscapeEventData.Choices[0].Outcome.SetDiplomaticObjectives.Clear();
                        ProgAnuSupportive.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -4));
                        ProgAnuSupportive.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -10));
                        ProgAnuSupportive.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -10));
                        TFTVLogger.Always($"Harder diplomacy is on, changing event {eventID}");
                    }
                    else if (eventID == ProgAnuPact.EventID)
                    {
                        ProgAnuPact.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -15));
                        ProgAnuPact.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -15));
                        TFTVCommonMethods.GenerateGeoEventChoice(ProgAnuPact, "PROG_AN4_CHOICE_1_TEXT", "PROG_AN4_CHOICE_1_OUTCOME_GENERAL");
                        ProgAnuPact.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -6));
                        TFTVLogger.Always($"Harder diplomacy is on, changing event {eventID}");
                    }
                    else if (eventID == ProgAnuAlliance.EventID)
                    {
                        ProgAnuAlliance.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -10)); 
                        TFTVCommonMethods.GenerateGeoEventChoice(ProgAnuAlliance, "PROG_AN4_CHOICE_1_TEXT", "PROG_AN4_CHOICE_1_OUTCOME_GENERAL");
                        ProgAnuAlliance.GeoscapeEventData.Choices[2].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -8));
                        TFTVLogger.Always($"Harder diplomacy is on, changing event {eventID}");
                    }
                    else if (eventID == ProgAnuAllianceNoSynod.EventID)
                    {
                        ProgAnuAllianceNoSynod.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -20));
                        ProgAnuAllianceNoSynod.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -15));
                        TFTVCommonMethods.GenerateGeoEventChoice(ProgAnuAllianceNoSynod, "PROG_AN4_CHOICE_1_TEXT", "PROG_AN4_CHOICE_1_OUTCOME_GENERAL");
                        ProgAnuAllianceNoSynod.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -8));
                        TFTVLogger.Always($"Harder diplomacy is on, changing event {eventID}");
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
                        TFTVLogger.Always($"Harder diplomacy is on, changing event {eventID}");
                    }
                    else if (eventID == ProgSynPact.EventID)
                    {

                        //Aligned
                        ProgSynPact.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -18));
                        ProgSynPact.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -18));
                        ProgSynPact.GeoscapeEventData.Choices[2].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -15));
                        ProgSynPact.GeoscapeEventData.Choices[2].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -15));
                        TFTVLogger.Always($"Harder diplomacy is on, changing event {eventID}");
                    }
                    else if (eventID == ProgSynAlliancePoly.EventID)
                    {
                        //Aliance Polyphonic             
                        ProgSynAlliancePoly.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -8));
                        TFTVCommonMethods.GenerateGeoEventChoice(ProgSynAlliancePoly, "PROG_SY_POSTPONE_CHOICE", "PROG_SY_POSTPONE_TEXT");
                        ProgSynAlliancePoly.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -8));
                        TFTVLogger.Always($"Harder diplomacy is on, changing event {eventID}");
                    }

                    else if (eventID == ProgSynAllianceTerra.EventID)
                    {
                        //Alliance Terra
                        ProgSynAllianceTerra.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -20));
                        TFTVCommonMethods.GenerateGeoEventChoice(ProgSynAllianceTerra, "PROG_SY_POSTPONE_CHOICE", "PROG_SY_POSTPONE_TEXT");
                        ProgSynAllianceTerra.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -8));
                        TFTVLogger.Always($"Harder diplomacy is on, changing event {eventID}");
                    }
                    else if (eventID == ProgNJSupportive.EventID)
                    {
                        ProgNJSupportive.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -10));
                        ProgNJSupportive.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -10));
                        TFTVCommonMethods.GenerateGeoEventChoice(ProgNJSupportive, "PROG_NJ_POSTPONE_CHOICE", "PROG_NJ_POSTPONE_TEXT");
                        ProgNJSupportive.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -4));
                        TFTVLogger.Always($"Harder diplomacy is on, changing event {eventID}");
                    }
                    else if (eventID == ProgNJPact.EventID)
                    {
                        ProgNJPact.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -15));
                        ProgNJPact.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -15));
                        TFTVCommonMethods.GenerateGeoEventChoice(ProgNJPact, "PROG_NJ_POSTPONE_CHOICE", "PROG_NJ_POSTPONE_TEXT");
                        ProgNJPact.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -6));
                        TFTVLogger.Always($"Harder diplomacy is on, changing event {eventID}");
                    }

                    else if (eventID == ProgNJAlliance.EventID)
                    {
                        ProgNJAlliance.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -20));
                        ProgNJAlliance.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -20));
                        TFTVCommonMethods.GenerateGeoEventChoice(ProgNJAlliance, "PROG_NJ_POSTPONE_CHOICE", "PROG_NJ_POSTPONE_TEXT");
                        ProgNJAlliance.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -8));
                        TFTVLogger.Always($"Harder diplomacy is on, changing event {eventID}");
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
                // Undo exactly what ImplementDiplomaticPenalties changed. Restoring from the snapshot (rather than clearing
                // lists) keeps the vanilla outcomes, e.g. the diplomacy change on each PROG_SY3_WIN choice.
                if (__instance == null || !_patchedEvents.TryGetValue(__instance.EventID, out PatchedEventSnapshot snapshot))
                {
                    return;
                }

                GeoscapeEventDef penaltyEventDef = _penaltyEventDefNames
                    .Select(name => DefCache.GetDef<GeoscapeEventDef>(name))
                    .FirstOrDefault(def => def.EventID == __instance.EventID);

                if (penaltyEventDef != null)
                {
                    RestoreSnapshot(penaltyEventDef, snapshot);
                    TFTVLogger.Always("Harder diplomacy is on, changing event " + __instance.EventID + " back to keep things nice and tidy");
                }

                _patchedEvents.Remove(__instance.EventID);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
    }
}