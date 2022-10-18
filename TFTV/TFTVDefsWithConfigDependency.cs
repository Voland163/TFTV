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


        public static void PopulateResourceRewardsDictionary()
        {
            try
            {
                //  TFTVLogger.Always("PopulateResourceRewardsDictionary is running");

                foreach (GeoscapeEventDef geoEvent in Repo.GetAllDefs<GeoscapeEventDef>())
                {
                    int geoEventChoice = 0;

                    foreach (GeoEventChoice choice in geoEvent.GeoscapeEventData.Choices)
                    {

                        if (choice.Outcome.Resources != null && !choice.Outcome.Resources.IsEmpty)
                        {

                            string EventID = geoEvent.GeoscapeEventData.EventID;

                            if (!ResourceRewardsFromEvents.ContainsKey(EventID))
                            {
                                //  TFTVLogger.Always("geoEvent is " + geoEvent.GeoscapeEventData.EventID + " and we got here, step 2");

                                ResourceRewardsFromEvents.Add(EventID, new float[geoEvent.GeoscapeEventData.Choices.Count, 4]);
                            }

                            //    TFTVLogger.Always("geoEvent is " + geoEvent.GeoscapeEventData.EventID + " and we got here, step 3");
                            for (int i = 0; i < choice.Outcome.Resources.Count; i++)
                            {

                                ResourceRewardsFromEvents[EventID][geoEventChoice, i] = choice.Outcome.Resources[i].Value;

                             //   TFTVLogger.Always("Event " + EventID + " Choice # " + geoEventChoice + " gives " + ResourceRewardsFromEvents[EventID][geoEventChoice, i]
                             //      + " of some resource");

                            }



                        }
                        geoEventChoice++;

                    }
                }

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void ModifyAmountResourcesEvents(float resourceMultiplier)
        {
            try
            {


              //  TFTVLogger.Always("ModifyAmountResourcesEvents running");

                /*  foreach (GeoscapeEventDef geoEvent in Repo.GetAllDefs<GeoscapeEventDef>())
                  {
                      string eventID = geoEvent.GeoscapeEventData.EventID;
                      int geoEventChoice = 0;

                      foreach (GeoEventChoice choice in geoEvent.GeoscapeEventData.Choices)
                      {
                          if (choice.Outcome.Resources != null && !choice.Outcome.Resources.IsEmpty)
                          {
                              for (int i = 0; i < choice.Outcome.Resources.Count; i++)
                              {
                                  choice.Outcome.Resources[i] =
                                  new PhoenixPoint.Common.Core.ResourceUnit(choice.Outcome.Resources[i].Type,
                                  ResourceRewardsFromEvents[eventID][geoEventChoice, i]);
                                  TFTVLogger.Always("Before adjutmen, event " + eventID + " Choice # " + geoEventChoice + " gives " + ResourceRewardsFromEvents[eventID][geoEventChoice, i]
                                  + " of some resource");
                              }
                          }
                          geoEventChoice++;

                      }
                  }*/



                foreach (GeoscapeEventDef geoEvent in Repo.GetAllDefs<GeoscapeEventDef>())
                {
                    string eventID = geoEvent.GeoscapeEventData.EventID;
                    int geoEventChoice = 0;

                    if (ResourceRewardsFromEvents.ContainsKey(eventID))
                    {

                        foreach (GeoEventChoice choice in geoEvent.GeoscapeEventData.Choices)
                        {
                            if (choice.Outcome.Resources != null && !choice.Outcome.Resources.IsEmpty)
                            {
                                for (int i = 0; i < choice.Outcome.Resources.Count; i++)
                                {
                                    choice.Outcome.Resources[i] =
                                    new PhoenixPoint.Common.Core.ResourceUnit(choice.Outcome.Resources[i].Type,
                                    ResourceRewardsFromEvents[eventID][geoEventChoice, i] * resourceMultiplier);
                                 //   TFTVLogger.Always("After adjustment, event " + eventID + " Choice # " + geoEventChoice + " gives " + choice.Outcome.Resources[i].Value
                                 //  + " of some resource");

                                }
                            }
                            geoEventChoice++;
                        }
                    }
                    else
                    {
                      //  TFTVLogger.Always("Event " + eventID + " not found");

                    }

                }

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

      /*  public static void PopulateDiplomacyRewardsDictionary()
          {
              try
              {
                  TFTVLogger.Always("PopulateDiplomacyRewardsDictionary is running");

                  foreach (GeoscapeEventDef geoEvent in Repo.GetAllDefs<GeoscapeEventDef>())
                  {
                      int geoEventChoice = 0;

                      foreach (GeoEventChoice choice in geoEvent.GeoscapeEventData.Choices)
                      {

                          if (choice.Outcome.Diplomacy != null && choice.Outcome.Diplomacy.Count>0)
                          {
                              string EventID = geoEvent.GeoscapeEventData.EventID;

                              if (!DiplomacyRewardsFromEvents.ContainsKey(EventID))
                              {
                                  // TFTVLogger.Always("geoEvent is " + geoEvent.GeoscapeEventData.EventID + " and we got here, step 2");

                                  DiplomacyRewardsFromEvents.Add(EventID, new int[geoEvent.GeoscapeEventData.Choices.Count, 8]);
                              }

                              //     TFTVLogger.Always("geoEvent is " + geoEvent.GeoscapeEventData.EventID + " and we got here, step 3");
                              for (int i = 0; i < choice.Outcome.Diplomacy.Count; i++)
                              {

                                  DiplomacyRewardsFromEvents[EventID][geoEventChoice, i] = choice.Outcome.Diplomacy[i].Value;

                                 // TFTVLogger.Always("Event " + EventID + " Choice # " + geoEventChoice + " gives " + ResourceRewardsFromEvents[EventID][geoEventChoice, i]
                                  //       + " of some resource");

                              }



                          }
                          geoEventChoice++;

                      }
                  }

              }

              catch (Exception e)
              {
                  TFTVLogger.Error(e);
              }

          }
      */
          

        public static void InjectDefsWithDynamicConfigDependency()
        {

            HibernationModuleStaminaRecuperation();
            ModifyAmountResourcesEvents(config.ResourceMultiplier);
        }

        /*  public static void ModifyAmountResourcesEvents(float resourceMultiplier)
          {
              try
              {

                  if (ApplyChangeReduceResources)
                  {

                      foreach (GeoscapeEventDef geoEvent in Repo.GetAllDefs<GeoscapeEventDef>())
                      {
                          foreach (GeoEventChoice choice in geoEvent.GeoscapeEventData.Choices)
                          {
                              if (choice.Outcome.Resources != null && !choice.Outcome.Resources.IsEmpty)
                              {
                                  choice.Outcome.Resources *= resourceMultiplier;
                              }
                          }
                      }
                      ApplyChangeReduceResources = false;
                  }

              }
              catch (Exception e)
              {
                  TFTVLogger.Error(e);
              }
          }
        */

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


      // private static bool ApplyChangeDiplomacy = true;

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

                    /*  if (ApplyChangeDiplomacy)
                      {
                      int count = 0;
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
                              // int geoChoiceNumber = 0;


                                  foreach (GeoEventChoice choice in geoEvent.GeoscapeEventData.Choices)
                                  {
                                      for (int i = 0; i < choice.Outcome.Diplomacy.Count; i++)
                                      {
                                          if (choice.Outcome.Diplomacy[i].TargetFaction == geoPhoenixFaction && choice.Outcome.Diplomacy[i].Value <= 0)
                                          {
                                              OutcomeDiplomacyChange diplomacyChange = choice.Outcome.Diplomacy[i];
                                              diplomacyChange.Value *= 2;
                                              choice.Outcome.Diplomacy[i] = diplomacyChange;
                                          count++;
                                            //  TFTVLogger.Always("GeoEvent " + geoEvent.GeoscapeEventData.EventID + " diplomacy change value is " + diplomacyChange.Value);
                                          }
                                      }
                                     // geoChoiceNumber++;
                                  }
                              }
                          }
                      TFTVLogger.Always("Harder Diplomacy is switched on, so " + count + " diplomacy penalties of Faction vs PX have been doubled");
                      ApplyChangeDiplomacy = false;
                     }*/

                    
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
                    ProgAnuSupportive.GeoscapeEventData.Choices[0].Outcome.ReEneableEvent = true;
                    ProgAnuSupportive.GeoscapeEventData.Choices[0].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("RefusedAnu", 1, true));
                    ProgAnuSupportive.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -10));
                    ProgAnuSupportive.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -10));


                    ProgAnuPact.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -15));
                    ProgAnuPact.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -15));
                    TFTVCommonMethods.GenerateGeoEventChoice(ProgAnuPact, "PROG_AN4_CHOICE_1_TEXT", "PROG_AN4_CHOICE_1_OUTCOME_GENERAL");
                    ProgAnuPact.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -6));
                    ProgAnuPact.GeoscapeEventData.Choices[1].Outcome.ReEneableEvent = true;
                    ProgAnuPact.GeoscapeEventData.Choices[1].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("RefusedAnu", 2, true));

                    ProgAnuAlliance.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -10));
                    ProgAnuAlliance.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -20));
                    ProgAnuAlliance.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -15));
                    TFTVCommonMethods.GenerateGeoEventChoice(ProgAnuAlliance, "PROG_AN4_CHOICE_1_TEXT", "PROG_AN4_CHOICE_1_OUTCOME_GENERAL");
                    ProgAnuAlliance.GeoscapeEventData.Choices[2].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -8));
                    ProgAnuAlliance.GeoscapeEventData.Choices[2].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("RefusedAnu", 3, true));

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
                    ProgSynAlliancePoly.GeoscapeEventData.Choices[1].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("RefusedSynedrion", 3, true));

                    //Alliance Terra
                    ProgSynAllianceTerra.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -20));
                    TFTVCommonMethods.GenerateGeoEventChoice(ProgSynAllianceTerra, "PROG_SY_POSTPONE_CHOICE", "PROG_SY_POSTPONE_TEXT");
                    ProgSynAllianceTerra.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -8));
                    ProgSynAllianceTerra.GeoscapeEventData.Choices[1].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("RefusedSynedrion", 3, true));

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
                    ProgNJPact.GeoscapeEventData.Choices[1].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("RefusedNewJericho", 2, true));


                    ProgNJAlliance.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -20));
                    ProgNJAlliance.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -20));
                    TFTVCommonMethods.GenerateGeoEventChoice(ProgNJAlliance, "PROG_NJ_POSTPONE_CHOICE", "PROG_NJ_POSTPONE_TEXT");
                    ProgNJAlliance.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -8));
                    ProgNJAlliance.GeoscapeEventData.Choices[1].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("RefusedNewJericho", 3, true));

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
