using Base.Defs;
using PhoenixPoint.Geoscape.Entities.Interception.Equipments;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV
{
    internal class TFTVDefsWithConfigDependency
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        private static readonly TFTVConfig config = TFTVMain.Main.Config;
        public static Dictionary<string, float[,]> ResourceRewardsFromEvents = new Dictionary<string, float[,]>();
        public static Dictionary<string, int[,]> DiplomacyRewardsFromEvents = new Dictionary<string, int[,]>();


          

        public static void InjectDefsWithDynamicConfigDependency()
        {

            HibernationModuleStaminaRecuperation();
           // ModifyAmountResourcesEvents(config.ResourceMultiplier);
        }


        public static void InjectDefsWithStaticConfigDependency() 
        {
            HarderDiplomacy();  
        }

        public static void HibernationModuleStaminaRecuperation()
        {

            try
            {
                TFTVConfig config = TFTVMain.Main.Config;
                GeoVehicleModuleDef hibernationmodule = DefCache.GetDef<GeoVehicleModuleDef>("SY_HibernationPods_GeoVehicleModuleDef");

                if (config.ActivateStaminaRecuperatonModule)
                {
                    hibernationmodule.GeoVehicleModuleBonusValue = 0.35f;

                }
                else
                {
                    hibernationmodule.GeoVehicleModuleBonusValue = 0;
                }

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void HarderDiplomacy()
        {
            try
            {
                if (config.DiplomaticPenalties)
                {

                    //ID all the factions for later
                    GeoFactionDef PhoenixPoint = DefCache.GetDef<GeoFactionDef>("Phoenix_GeoPhoenixFactionDef");
                    GeoFactionDef NewJericho = DefCache.GetDef<GeoFactionDef>("NewJericho_GeoFactionDef");
                    GeoFactionDef Anu = DefCache.GetDef<GeoFactionDef>("Anu_GeoFactionDef");
                    GeoFactionDef Synedrion = DefCache.GetDef<GeoFactionDef>("Synedrion_GeoFactionDef");

                    //Source for creating new events
                    GeoscapeEventDef sourceLoseGeoEvent = DefCache.GetDef<GeoscapeEventDef>("PROG_PU12_FAIL_GeoscapeEventDef");

                    //Testing increasing diplomacy penalties 
                    GeoPhoenixFactionDef geoPhoenixFaction = DefCache.GetDef<GeoPhoenixFactionDef>("Phoenix_GeoPhoenixFactionDef");

                    
                    //Increase diplo penalties in 25, 50 and 75 diplo missions
                    GeoscapeEventDef ProgAnuSupportive = DefCache.GetDef<GeoscapeEventDef>("PROG_AN2_GeoscapeEventDef");
                    GeoscapeEventDef ProgNJSupportive = DefCache.GetDef<GeoscapeEventDef>("PROG_NJ1_GeoscapeEventDef");
                    GeoscapeEventDef ProgSynSupportive = DefCache.GetDef<GeoscapeEventDef>("PROG_SY1_GeoscapeEventDef");

                    GeoscapeEventDef ProgAnuPact = DefCache.GetDef<GeoscapeEventDef>("PROG_AN4_GeoscapeEventDef");
                    GeoscapeEventDef ProgNJPact = DefCache.GetDef<GeoscapeEventDef>("PROG_NJ2_GeoscapeEventDef");
                    GeoscapeEventDef ProgSynPact = DefCache.GetDef<GeoscapeEventDef>("PROG_SY3_WIN_GeoscapeEventDef");


                    GeoscapeEventDef ProgAnuAlliance = DefCache.GetDef<GeoscapeEventDef>("PROG_AN6_GeoscapeEventDef");
                    GeoscapeEventDef ProgNJAlliance = DefCache.GetDef<GeoscapeEventDef>("PROG_NJ3_GeoscapeEventDef");
                    GeoscapeEventDef ProgSynAllianceTerra = DefCache.GetDef<GeoscapeEventDef>("PROG_SY4_T_GeoscapeEventDef");
                    GeoscapeEventDef ProgSynAlliancePoly = DefCache.GetDef<GeoscapeEventDef>("PROG_SY4_P_GeoscapeEventDef");

                    //Anu
                    ProgAnuSupportive.GeoscapeEventData.Choices[0].Outcome.SetDiplomaticObjectives.Clear();
                    ProgAnuSupportive.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -4));
                  //  ProgAnuSupportive.GeoscapeEventData.Choices[0].Outcome.ReEneableEvent = true;
                //    ProgAnuSupportive.GeoscapeEventData.Choices[0].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("RefusedAnu", 1, true));
                    ProgAnuSupportive.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -10));
                    ProgAnuSupportive.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -10));


                    ProgAnuPact.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -15));
                    ProgAnuPact.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -15));
                    TFTVCommonMethods.GenerateGeoEventChoice(ProgAnuPact, "PROG_AN4_CHOICE_1_TEXT", "PROG_AN4_CHOICE_1_OUTCOME_GENERAL");
                    ProgAnuPact.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -6));
                   // ProgAnuPact.GeoscapeEventData.Choices[1].Outcome.ReEneableEvent = true;
                 //   ProgAnuPact.GeoscapeEventData.Choices[1].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("RefusedAnu", 2, true));

                    ProgAnuAlliance.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -10));
                    ProgAnuAlliance.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -20));
                    ProgAnuAlliance.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -15));
                    TFTVCommonMethods.GenerateGeoEventChoice(ProgAnuAlliance, "PROG_AN4_CHOICE_1_TEXT", "PROG_AN4_CHOICE_1_OUTCOME_GENERAL");
                    ProgAnuAlliance.GeoscapeEventData.Choices[2].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -8));
                 //   ProgAnuAlliance.GeoscapeEventData.Choices[2].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("RefusedAnu", 3, true));

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
                 //   ProgSynSupportive.GeoscapeEventData.Choices[2].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("RefusedSynedrion", 1, true));


                    //Aligned
                    ProgSynPact.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -18));
                    ProgSynPact.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -18));
                    ProgSynPact.GeoscapeEventData.Choices[2].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -15));
                    ProgSynPact.GeoscapeEventData.Choices[2].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -15));

                    //Aliance Polyphonic             
                    ProgSynAlliancePoly.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -8));
                    TFTVCommonMethods.GenerateGeoEventChoice(ProgSynAlliancePoly, "PROG_SY_POSTPONE_CHOICE", "PROG_SY_POSTPONE_TEXT");
                    ProgSynAlliancePoly.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -8));
                //    ProgSynAlliancePoly.GeoscapeEventData.Choices[1].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("RefusedSynedrion", 3, true));

                    //Alliance Terra
                    ProgSynAllianceTerra.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -20));
                    TFTVCommonMethods.GenerateGeoEventChoice(ProgSynAllianceTerra, "PROG_SY_POSTPONE_CHOICE", "PROG_SY_POSTPONE_TEXT");
                    ProgSynAllianceTerra.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -8));
                  //  ProgSynAllianceTerra.GeoscapeEventData.Choices[1].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("RefusedSynedrion", 3, true));

                    //New Jericho
                    ProgNJSupportive.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -10));
                    ProgNJSupportive.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -10));
                    TFTVCommonMethods.GenerateGeoEventChoice(ProgNJSupportive, "PROG_NJ_POSTPONE_CHOICE", "PROG_NJ_POSTPONE_TEXT");
                    ProgNJSupportive.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -4));
                //    ProgNJSupportive.GeoscapeEventData.Choices[1].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("RefusedNewJericho", 1, true));


                    ProgNJPact.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -15));
                    ProgNJPact.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -15));
                    TFTVCommonMethods.GenerateGeoEventChoice(ProgNJPact, "PROG_NJ_POSTPONE_CHOICE", "PROG_NJ_POSTPONE_TEXT");
                    ProgNJPact.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -6));
                 //   ProgNJPact.GeoscapeEventData.Choices[1].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("RefusedNewJericho", 2, true));


                    ProgNJAlliance.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -20));
                    ProgNJAlliance.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -20));
                    TFTVCommonMethods.GenerateGeoEventChoice(ProgNJAlliance, "PROG_NJ_POSTPONE_CHOICE", "PROG_NJ_POSTPONE_TEXT");
                    ProgNJAlliance.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -8));
                 //   ProgNJAlliance.GeoscapeEventData.Choices[1].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("RefusedNewJericho", 3, true));

                    //Change Reward introductory mission Synedrion
                    GeoscapeEventDef ProgSynIntroWin = DefCache.GetDef<GeoscapeEventDef>("PROG_SY0_WIN_GeoscapeEventDef");
                    ProgSynIntroWin.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Clear();

                }
                //remove Pirate King mission
                RemovePirateKing();
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
                GeoscapeEventDef fireBirdMiss = DefCache.GetDef<GeoscapeEventDef>("PROG_SY2_MISS_GeoscapeEventDef");
                fireBirdMiss.GeoscapeEventData.Choices[0].Outcome.StartMission.WonEventID = "PROG_SY3_WIN";

                GeoscapeEventDef pirateKingWin = DefCache.GetDef<GeoscapeEventDef>("PROG_SY3_WIN_GeoscapeEventDef");
                pirateKingWin.GeoscapeEventData.Title.LocalizationKey = "PROG_SY2_WIN_TITLE";


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

    }
}
