using Base.Defs;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                ProgAnuSupportive.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -10));
                ProgAnuSupportive.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -10));
                ProgAnuPact.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -15));
                ProgAnuPact.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -15));
                ProgAnuAlliance.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -10));
                ProgAnuAlliance.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -20));
                ProgAnuAlliance.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -15));

                //Synedrion
                //Supportive Polyphonic
                ProgSynSupportive.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -15));
                ProgSynSupportive.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -5));

                //Supportive Terra
                ProgSynSupportive.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -15));
                ProgSynSupportive.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -5));

                //Aligned
                ProgSynPact.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -18));
                ProgSynPact.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -18));
                ProgSynPact.GeoscapeEventData.Choices[2].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -15));
                ProgSynPact.GeoscapeEventData.Choices[2].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -15));
                //Aliance Polyphonic             
                ProgSynAlliancePoly.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -8));
                //Alliance Terra
                ProgSynAllianceTerra.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -20));

                //New Jericho
                ProgNJSupportive.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -10));
                ProgNJSupportive.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -10));
                ProgNJPact.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -15));
                ProgNJPact.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -15));
                ProgNJAlliance.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -20));
                ProgNJAlliance.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -20));

                //Change Reward introductory mission Synedrion
                GeoscapeEventDef ProgSynIntroWin = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_SY0_WIN_GeoscapeEventDef"));
                ProgSynIntroWin.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Clear();
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
    }
}