using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using System;
using System.Linq;

namespace TFTV
{
    internal class TFTVDiplomacyPenalties
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static bool ApplyChangeDiplomacy = true;

        public static void Apply_Changes()
        {
            try
            {
                //ID all the factions for later
                GeoFactionDef PhoenixPoint = Repo.GetAllDefs<GeoFactionDef>().FirstOrDefault(ged => ged.name.Equals("Phoenix_GeoPhoenixFactionDef"));
                GeoFactionDef NewJericho = Repo.GetAllDefs<GeoFactionDef>().FirstOrDefault(ged => ged.name.Equals("NewJericho_GeoFactionDef"));
                GeoFactionDef Anu = Repo.GetAllDefs<GeoFactionDef>().FirstOrDefault(ged => ged.name.Equals("Anu_GeoFactionDef"));
                GeoFactionDef Synedrion = Repo.GetAllDefs<GeoFactionDef>().FirstOrDefault(ged => ged.name.Equals("Synedrion_GeoFactionDef"));

                //Source for creating new events
                GeoscapeEventDef sourceLoseGeoEvent = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_PU12_FAIL_GeoscapeEventDef"));

                //Testing increasing diplomacy penalties 
                GeoPhoenixFactionDef geoPhoenixFaction = Repo.GetAllDefs<GeoPhoenixFactionDef>().FirstOrDefault(ged => ged.name.Equals("Phoenix_GeoPhoenixFactionDef"));

                if (ApplyChangeDiplomacy)
                {
                    foreach (GeoscapeEventDef geoEvent in Repo.GetAllDefs<GeoscapeEventDef>())
                    {

                        if (geoEvent.GeoscapeEventData.EventID != "PROG_PU4_WIN"
                            && geoEvent.GeoscapeEventData.EventID != "PROG_SY7"
                            && geoEvent.GeoscapeEventData.EventID != "PROG_SY8"
                            && geoEvent.GeoscapeEventData.EventID != "PROG_AN3"
                            && geoEvent.GeoscapeEventData.EventID != "PROG_AN5"
                            && geoEvent.GeoscapeEventData.EventID != "PROG_NJ7"
                            && geoEvent.GeoscapeEventData.EventID != "PROG_NJ8")
                        {
                            foreach (GeoEventChoice choice in geoEvent.GeoscapeEventData.Choices)
                            {
                                for (int i = 0; i < choice.Outcome.Diplomacy.Count; i++)
                                {
                                    if (choice.Outcome.Diplomacy[i].TargetFaction == geoPhoenixFaction && choice.Outcome.Diplomacy[i].Value <= 0)
                                    {
                                        OutcomeDiplomacyChange diplomacyChange = choice.Outcome.Diplomacy[i];
                                        diplomacyChange.Value *= 2;
                                        choice.Outcome.Diplomacy[i] = diplomacyChange;
                                    }
                                }
                            }
                        }
                    }
                    ApplyChangeDiplomacy = false;
                }


                //Increase diplo penalties in 25, 50 and 75 diplo missions
                GeoscapeEventDef ProgAnuSupportive = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_AN2_GeoscapeEventDef"));
                GeoscapeEventDef ProgNJSupportive = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_NJ1_GeoscapeEventDef"));
                GeoscapeEventDef ProgSynSupportive = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_SY1_GeoscapeEventDef"));

                GeoscapeEventDef ProgAnuPact = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_AN4_GeoscapeEventDef"));
                GeoscapeEventDef ProgNJPact = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_NJ2_GeoscapeEventDef"));
                GeoscapeEventDef ProgSynPact = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_SY3_WIN_GeoscapeEventDef"));


                GeoscapeEventDef ProgAnuAlliance = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_AN6_GeoscapeEventDef"));
                GeoscapeEventDef ProgNJAlliance = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_NJ3_GeoscapeEventDef"));
                GeoscapeEventDef ProgSynAllianceTerra = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_SY4_T_GeoscapeEventDef"));
                GeoscapeEventDef ProgSynAlliancePoly = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_SY4_P_GeoscapeEventDef"));

                //Anu
                ProgAnuSupportive.GeoscapeEventData.Choices[0].Outcome.SetDiplomaticObjectives.Clear();
                ProgAnuSupportive.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -4));
                ProgAnuSupportive.GeoscapeEventData.Choices[0].Outcome.ReEneableEvent = true;
                ProgAnuSupportive.GeoscapeEventData.Choices[0].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("RefusedAnu", 1, true));
                ProgAnuSupportive.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -10));
                ProgAnuSupportive.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -10));


                ProgAnuPact.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -15));
                ProgAnuPact.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -15));
                TFTVCommonMethods.GenerateGeoEventChoice(ProgAnuPact, "PROG_AN4_CHOICE_1_TEXT", "PROG_AN4_CHOICE_1_OUTCOME_GENERAL");
                ProgAnuPact.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -6));
                ProgAnuPact.GeoscapeEventData.Choices[1].Outcome.ReEneableEvent = true;
                ProgAnuPact.GeoscapeEventData.Choices[1].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("RefusedAnu", 1, true));

                ProgAnuAlliance.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -10));
                ProgAnuAlliance.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -20));
                ProgAnuAlliance.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -15));
                TFTVCommonMethods.GenerateGeoEventChoice(ProgAnuAlliance, "PROG_AN4_CHOICE_1_TEXT", "PROG_AN4_CHOICE_1_OUTCOME_GENERAL");
                ProgAnuAlliance.GeoscapeEventData.Choices[2].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -8));
                ProgAnuAlliance.GeoscapeEventData.Choices[2].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("RefusedAnu", 1, true));

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
                ProgSynSupportive.GeoscapeEventData.Choices[2].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("RefusedSynedrion", 1, true));


                //Aligned
                ProgSynPact.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -18));
                ProgSynPact.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -18));
                ProgSynPact.GeoscapeEventData.Choices[2].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -15));
                ProgSynPact.GeoscapeEventData.Choices[2].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -15));

                //Aliance Polyphonic             
                ProgSynAlliancePoly.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -8));
                TFTVCommonMethods.GenerateGeoEventChoice(ProgSynAlliancePoly, "PROG_SY_POSTPONE_CHOICE", "PROG_SY_POSTPONE_TEXT");
                ProgSynAlliancePoly.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -8));
                ProgSynAlliancePoly.GeoscapeEventData.Choices[1].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("RefusedSynedrion", 1, true));

                //Alliance Terra
                ProgSynAllianceTerra.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -20));
                TFTVCommonMethods.GenerateGeoEventChoice(ProgSynAllianceTerra, "PROG_SY_POSTPONE_CHOICE", "PROG_SY_POSTPONE_TEXT");
                ProgSynAllianceTerra.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -8));
                ProgSynAllianceTerra.GeoscapeEventData.Choices[1].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("RefusedSynedrion", 1, true));

                //New Jericho
                ProgNJSupportive.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -10));
                ProgNJSupportive.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -10));
                TFTVCommonMethods.GenerateGeoEventChoice(ProgNJSupportive, "PROG_NJ_POSTPONE_CHOICE", "PROG_NJ_POSTPONE_TEXT");
                ProgNJSupportive.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -4));
                ProgNJSupportive.GeoscapeEventData.Choices[1].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("RefusedNewJericho", 1, true));


                ProgNJPact.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -15));
                ProgNJPact.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -15));
                TFTVCommonMethods.GenerateGeoEventChoice(ProgNJPact, "PROG_NJ_POSTPONE_CHOICE", "PROG_NJ_POSTPONE_TEXT");
                ProgNJPact.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -6));
                ProgNJPact.GeoscapeEventData.Choices[1].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("RefusedNewJericho", 1, true));


                ProgNJAlliance.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -20));
                ProgNJAlliance.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -20));
                TFTVCommonMethods.GenerateGeoEventChoice(ProgNJAlliance, "PROG_NJ_POSTPONE_CHOICE", "PROG_NJ_POSTPONE_TEXT");
                ProgNJAlliance.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -8));
                ProgNJAlliance.GeoscapeEventData.Choices[1].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("RefusedNewJericho", 1, true));

                //Change Reward introductory mission Synedrion
                GeoscapeEventDef ProgSynIntroWin = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_SY0_WIN_GeoscapeEventDef"));
                ProgSynIntroWin.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Clear();
                
                //remove Pirate King mission
                RemovePirateKing();
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }




        [HarmonyPatch(typeof(GeoFaction), "OnDiplomacyChanged")]
        public static class GeoBehemothActor_OnDiplomacyChanged_patch
        {
            public static bool Prepare()
            {
                TFTVConfig config = TFTVMain.Main.Config;
                return config.DiplomaticPenalties;
            }

            public static void Postfix(GeoFaction __instance, PartyDiplomacy.Relation relation, int newValue)

            {
                try
                {
                     CheckPostponedFactionMissions(__instance, relation, newValue);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }

        }

        public static void CheckPostponedFactionMissions(GeoFaction faction, PartyDiplomacy.Relation relation, int newValue)
        {
            try
            {
                GeoFaction targetFaction = faction.GeoLevel.GetFaction((PPFactionDef)relation.WithParty);
                GeoscapeEventContext geoscapeEventContext = new GeoscapeEventContext(targetFaction, faction.GeoLevel.ViewerFaction);

                if (faction.GetParticipatingFaction() == faction.GeoLevel.AnuFaction
                       && targetFaction == faction.GeoLevel.PhoenixFaction && faction.GeoLevel.EventSystem.GetVariable("RefusedAnu") == 1)
                {
                    if (newValue == 24)
                    {
                        faction.GeoLevel.EventSystem.SetVariable("RefusedAnu", 0);
                        faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_AN2", geoscapeEventContext);
                    }
                    else if (newValue == 49)
                    {

                        faction.GeoLevel.EventSystem.SetVariable("RefusedAnu", 0);
                        faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_AN4", geoscapeEventContext);

                    }
                    else if (newValue == 74)
                    {
                        faction.GeoLevel.EventSystem.SetVariable("RefusedAnu", 0);
                        faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_AN6", geoscapeEventContext);

                    }
                }
                else if (faction.GetParticipatingFaction() == faction.GeoLevel.NewJerichoFaction
                      && targetFaction == faction.GeoLevel.PhoenixFaction && faction.GeoLevel.EventSystem.GetVariable("RefusedNewJericho") == 1)
                {
                    if (newValue == 24)
                    {
                        faction.GeoLevel.EventSystem.SetVariable("RefusedNewJericho", 0);
                        faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_NJ1", geoscapeEventContext);
                    }
                    else if (newValue == 49)
                    {

                        faction.GeoLevel.EventSystem.SetVariable("RefusedNewJericho", 0);
                        faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_NJ2", geoscapeEventContext);

                    }
                    else if (newValue == 74)
                    {
                        faction.GeoLevel.EventSystem.SetVariable("RefusedNewJericho", 0);
                        faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_NJ3", geoscapeEventContext);
                    }
                }
                else if (faction.GetParticipatingFaction() == faction.GeoLevel.SynedrionFaction
                      && targetFaction == faction.GeoLevel.PhoenixFaction && faction.GeoLevel.EventSystem.GetVariable("RefusedSynedrion") == 1)
                {
                    if (newValue == 24)
                    {
                        faction.GeoLevel.EventSystem.SetVariable("RefusedSynedrion", 0);
                        faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_SY1", geoscapeEventContext);
                    }

                    else if (newValue == 74)
                    {
                        if (faction.GeoLevel.EventSystem.GetVariable("Polyphonic") > faction.GeoLevel.EventSystem.GetVariable("Terraformers"))
                        {

                            faction.GeoLevel.EventSystem.SetVariable("RefusedSynedrion", 0);
                            faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_SY4_P", geoscapeEventContext);
                        }
                        else
                        {
                            faction.GeoLevel.EventSystem.SetVariable("RefusedSynedrion", 0);
                            faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_SY4_T", geoscapeEventContext);

                        }
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void RemovePirateKing()
        {
            try
            {
                GeoscapeEventDef fireBirdMiss = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_SY2_MISS_GeoscapeEventDef"));
                fireBirdMiss.GeoscapeEventData.Choices[0].Outcome.StartMission.WonEventID = "PROG_SY3_WIN";

                GeoscapeEventDef pirateKingWin = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_SY3_WIN_GeoscapeEventDef"));
                pirateKingWin.GeoscapeEventData.Title.LocalizationKey = "PROG_SY2_WIN_TITLE";


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

    }
}