using Base.Defs;
using Base.Eventus.Filters;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Geoscape.Entities.Abilities;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Conditions;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Events.Eventus.Filters;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.ContextHelp;
using PhoenixPoint.Geoscape.Levels.ContextHelp.HintConditions;
using PhoenixPoint.Geoscape.Levels.Factions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TFTV
{

    


    internal class TFTVChangesToDLC4Events
    {




     //   GeoPhoenixFaction AddTag

       //     ("AlienContainment_FactionFunctionalityTagDef");



        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;

        public static void ChangesToDLC4Defs()
        {
            
            try
            {
                GeoFactionDef PhoenixPoint = DefCache.GetDef<GeoFactionDef>("Phoenix_GeoPhoenixFactionDef");
                GeoFactionDef NewJericho = DefCache.GetDef<GeoFactionDef>("NewJericho_GeoFactionDef");
                GeoFactionDef Anu = DefCache.GetDef<GeoFactionDef>("Anu_GeoFactionDef");
                GeoFactionDef Synedrion = DefCache.GetDef<GeoFactionDef>("Synedrion_GeoFactionDef");

                ResearchTagDef CriticalResearchTag = DefCache.GetDef<ResearchTagDef>("CriticalPath_ResearchTagDef");

                // Put Barnabas in the [CHO] picture
                GeoscapeEventDef CH0Event = DefCache.GetDef<GeoscapeEventDef>("PROG_CH0_GeoscapeEventDef");
                CH0Event.GeoscapeEventData.Leader = "SY_Barnabas";

                GeoVariableChangedEventFilterDef favorForAFriendTrigger = Helper.CreateDefFromClone<GeoVariableChangedEventFilterDef>(null, "{F8133299-E340-4B54-9986-485C6F418D77}", "FakeVariableChange");
                favorForAFriendTrigger.VariableName = "FavorForAFriend";
                favorForAFriendTrigger.IsTimer = false;
                
                // Get corruption going from the start of the game... eh with intro to SDI
                GeoscapeEventDef geoEventCH0WIN = DefCache.GetDef<GeoscapeEventDef>("PROG_CH0_WIN_GeoscapeEventDef");
                var corruption = geoEventCH0WIN.GeoscapeEventData.Choices[0].Outcome.VariablesChange[1];
                GeoscapeEventDef sdi1 = DefCache.GetDef<GeoscapeEventDef>("SDI_01_GeoscapeEventDef");
                sdi1.GeoscapeEventData.Choices[0].Outcome.VariablesChange.Add(corruption);

                // Remove original Corruption variable change after winning first mission
                //geoEventCH0WIN.GeoscapeEventData.Choices[0].Outcome.VariablesChange.Remove(corruption);

                // Put Barnabas in the picture
                geoEventCH0WIN.GeoscapeEventData.Leader = "SY_Barnabas";

                // Make Acheron research available to Alien Faction without requiring completion of Unexpected Emergency, instead make it appear with Sirens and Chirons (ALN Lair Research)
                ResearchDef ALN_SirenResearch1 = DefCache.GetDef<ResearchDef>("ALN_Siren1_ResearchDef");
                var requirementForAlienAcheronResearch = ALN_SirenResearch1.RevealRequirements.Container[0];
                ResearchDef ALN_AcheronResearch1 = DefCache.GetDef<ResearchDef>("ALN_Acheron1_ResearchDef");
                ALN_AcheronResearch1.RevealRequirements.Container[0] = requirementForAlienAcheronResearch;


            
                // Make CH0 Mission appear when Player completes Acheron Autopsy and Capture and Containment 
                GeoResearchEventFilterDef PP_ResearchConditionCH0_Miss = DefCache.GetDef<GeoResearchEventFilterDef>("E_PROG_CH0_ResearchCompleted [GeoResearchEventFilterDef]");

                OrEventFilterDef triggerCH1 = DefCache.GetDef<OrEventFilterDef>("E_PROG_CH1_MultipleTriggers [OrEventFilterDef]");
                triggerCH1.OR_Filters[1] = PP_ResearchConditionCH0_Miss;
                GeoscapeEventDef CH0_Event = DefCache.GetDef<GeoscapeEventDef>("PROG_CH0_GeoscapeEventDef");
                //      CH0Event.Filters[0] = triggerCH1;
                CH0Event.Filters[0] = favorForAFriendTrigger;
               // CH0Event.GeoscapeEventData.Conditions = sdi1.GeoscapeEventData.Conditions;

                       // Make CH1 Mission appear when Player win CH0 Mission; CH1 Event will not be used!
                       GeoscapeEventDef CH1_Event = DefCache.GetDef<GeoscapeEventDef>("PROG_CH1_GeoscapeEventDef");
                CH0Event.GeoscapeEventData.Conditions.Add(CH1_Event.GeoscapeEventData.Conditions[1]);
                CH0Event.GeoscapeEventData.Conditions.Add(CH1_Event.GeoscapeEventData.Conditions[3]);

              //  CH0Event.GeoscapeEventData.Conditions = new List<GeoEventVariationConditionDef>();

                var revealSiteCH1_Miss = CH1_Event.GeoscapeEventData.Choices[0].Outcome.RevealSites[0];
                var setEventCH1_Miss = CH1_Event.GeoscapeEventData.Choices[0].Outcome.SetEvents[0];
                var trackEventCH1_Miss = CH1_Event.GeoscapeEventData.Choices[0].Outcome.TrackEncounters[0];
                GeoscapeEventDef CH0_Won = DefCache.GetDef<GeoscapeEventDef>("PROG_CH0_WIN_GeoscapeEventDef");
                CH0_Won.GeoscapeEventData.Choices[0].Outcome.RevealSites.Add(revealSiteCH1_Miss);
                CH0_Won.GeoscapeEventData.Choices[0].Outcome.SetEvents.Add(setEventCH1_Miss);
                CH0_Won.GeoscapeEventData.Choices[0].Outcome.TrackEncounters.Add(trackEventCH1_Miss);
                CH1_Event.GeoscapeEventData.Mute = true;

                // Make Treatment available after completing Research of Escaped Specimen, instead of after Acheron Autopsy
                // Copy unlock from Autopsy research to Specimen 2 (formerly Specimen 0) research and then remove it 
                ResearchDef specimen2Research = DefCache.GetDef<ResearchDef>("PX_OriginalAcheron_ResearchDef");
                specimen2Research.Unlocks = new ResearchRewardDef[]
                {       
                        DefCache.GetDef<ResearchRewardDef>("PX_Alien_Acheron_ResearchDef_UnlockFunctionalityResearchRewardDef_0")
                };
                ResearchDef acheronAutopsy = DefCache.GetDef<ResearchDef>("PX_Alien_Acheron_ResearchDef");
                acheronAutopsy.Unlocks = new ResearchRewardDef[] 
                {
                     DefCache.GetDef<ResearchRewardDef>("PX_Alien_Acheron_ResearchDef_UnlockPandoranSpecializationResearchRewardDef_0"),
                };
                acheronAutopsy.Tags = new ResearchTagDef[] { CriticalResearchTag };

                //Make Treatment hint appear when Specimen2 is researched
                DefCache.GetDef<ResearchGeoHintConditionDef>("E_AcheronAutopsyResearchCompleted [GeoscapeHintsManagerDef]").ResearchID = "PX_OriginalAcheron_ResearchDef";



                // Remove requirement to research Mutoid Technology to reserach Specimen 2 (former 0)
                ExistingResearchRequirementDef mutoidRequirement = DefCache.GetDef<ExistingResearchRequirementDef>("PX_OriginalAcheron_ResearchDef_ExistingResearchRequirementDef_0");
                mutoidRequirement.ResearchID = "PX_CaptureTech_ResearchDef";

                // Put Barnabas in the picture of CH1MISSWIN
                GeoscapeEventDef CH1_Won = DefCache.GetDef<GeoscapeEventDef>("PROG_CH1_WIN_GeoscapeEventDef");
                CH1_Won.GeoscapeEventData.Leader = "SY_Barnabas";
                //Break the panel in 2, to avoid text wall and give rep bonus with Syn
                CH1_Won.GeoscapeEventData.Choices[0].Outcome.OutcomeText.General.LocalizationKey = "PROG_CH1_WIN_OUTCOME_0";
               
                CH1_Won.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(new OutcomeDiplomacyChange()
                {
                    PartyFaction = Synedrion,
                    TargetFaction = PhoenixPoint,
                    PartyType = (OutcomeDiplomacyChange.ChangeTarget)1,
                    Value = +4
                });


                // Remove event reminding Lair is needed 
                GeoscapeEventDef CH_Event_NeedLair = DefCache.GetDef<GeoscapeEventDef>("PROG_CH_NEED_LAIR_GeoscapeEventDef");
                CH_Event_NeedLair.GeoscapeEventData.Mute = true;

                //CH2 miss removed completely in TFTVChangesToDLC3
                /*
                GeoscapeEventDef CH2_Event = DefCache.GetDef<GeoscapeEventDef>("PROG_CH2_GeoscapeEventDef");
                
                // Change requirements for appearance of CH2MISS works!
                // Create new research requirements
                // Clone trigger for CH2 re Research of Specimen 2 twice
                GeoResearchEventFilterDef sourceResearchTriggerCH2 = DefCache.GetDef<GeoResearchEventFilterDef>("E_PROG_CH2_ResearchCompleted [GeoResearchEventFilterDef]");
                GeoResearchEventFilterDef newResearchTrigger1CH2 = Helper.CreateDefFromClone(sourceResearchTriggerCH2, "4A1E4DA6-A89C-4D7E-B863-FB6B429882CE", "E_PROG_CH2_ResearchCompleted [GeoResearchEventFilterDef]");
                GeoResearchEventFilterDef newResearchTrigger2CH2 = Helper.CreateDefFromClone(sourceResearchTriggerCH2, "2FE2EC90-CBA4-4473-84D7-B343277B2225", "E_PROG_CH2_ResearchCompleted [GeoResearchEventFilterDef]");
                // Set new research triggers to complete virophage research and Scylla vivisection 
                newResearchTrigger1CH2.ResearchID = "PX_VirophageWeapons_ResearchDef";
                newResearchTrigger2CH2.ResearchID = "PX_Alien_Citadel_ResearchDef";
                // Add new Research triggers to CH2 event trigger;
                OrEventFilterDef triggerCH2 = DefCache.GetDef<OrEventFilterDef>("E_PROG_CH2_MultipleTriggers [OrEventFilterDef]");
                triggerCH2.OR_Filters[0] = newResearchTrigger1CH2;
                triggerCH2.OR_Filters[1] = newResearchTrigger2CH2;
                // Clone condition 3 (Research of Specimen 2) twice
                FactionConditionDef sourceConditionCH2Research = DefCache.GetDef<FactionConditionDef>("[PROG_CH2] Condition 3");
                FactionConditionDef newCond1CH2E = Helper.CreateDefFromClone(sourceConditionCH2Research, "67D454D6-0BF3-4A13-B503-5A297EEC22CE", "[PROG_CH2] Condition 4");
                FactionConditionDef newCond2CH2E = Helper.CreateDefFromClone(sourceConditionCH2Research, "FDD644C8-A209-4B23-B3A6-C05545E6DAC7", "[PROG_CH2] Condition 5");
                // Set new conditions to complete virophage research and Scylla vivisection               
                newCond1CH2E.CompletedResearchID = "PX_VirophageWeapons_ResearchDef";
                newCond2CH2E.CompletedResearchID = "PX_Alien_Citadel_ResearchDef";
                // Add the new conditions to CH2Event
                GeoscapeEventDef CH2_Event = DefCache.GetDef<GeoscapeEventDef>("PROG_CH2_GeoscapeEventDef");
                CH2_Event.GeoscapeEventData.Conditions.Add(newCond1CH2E);
                CH2_Event.GeoscapeEventData.Conditions.Add(newCond2CH2E);
                // Add Barnabas pic to CH2Event
                CH2_Event.GeoscapeEventData.Leader = "SY_Barnabas";
                // Remove final cinematic
                GeoscapeEventDef winCH2 = DefCache.GetDef<GeoscapeEventDef>("PROG_CH2_WIN_GeoscapeEventDef");
                winCH2.GeoscapeEventData.Choices[0].Outcome.Cinematic = CH_Event_NeedLair.GeoscapeEventData.Choices[0].Outcome.Cinematic;
                winCH2.GeoscapeEventData.Choices[0].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("CorruptedLairDestroyed", 1, true);
                */


                //Changes to SDI Events
                sdi1.GeoscapeEventData.Choices[0].Outcome.OutcomeText.General.LocalizationKey = "SDI1_OUTCOME";               
                
                
                GeoscapeEventDef sdi3 = DefCache.GetDef<GeoscapeEventDef>("SDI_03_GeoscapeEventDef");
               
          
                // GeoscapeEventDef sdi5 = DefCache.GetDef<GeoscapeEventDef>("SDI_05_GeoscapeEventDef");
               // sdi5.GeoscapeEventData.Choices[0].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("Umbra_Encounter_Variable", 1, false));
                
                GeoscapeEventDef sdi6 = DefCache.GetDef<GeoscapeEventDef>("SDI_06_GeoscapeEventDef");
                sdi6.GeoscapeEventData.Choices[0].Outcome.OutcomeText.General.LocalizationKey = "SDI6_OUTCOME";
               
                  
                //Need to fix a broken SDI event!
                GeoscapeEventDef brokenSDIEvent = DefCache.GetDef<GeoscapeEventDef>("SDI_07_GeoscapeEventDef");
                brokenSDIEvent.GeoscapeEventData.EventID = "BrokenSDIEvent";

                TFTVCommonMethods.CreateNewEvent("SDI_07", "SDI_07_TITLE", "SDI_07_TEXT_GENERAL_0", "SDI7_OUTCOME");
                GeoscapeEventDef sdi7 = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("SDI_07"));
                sdi7.GeoscapeEventData.Choices[0].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("Infestation_Encounter_Variable", 1, true));
                sdi7.GeoscapeEventData.Flavour = "SDI";

                GeoscapeEventDef sdi09 = DefCache.GetDef<GeoscapeEventDef>("SDI_09_GeoscapeEventDef");
               
                GeoscapeEventDef sdi10 = DefCache.GetDef<GeoscapeEventDef>("SDI_10_GeoscapeEventDef");
                sdi10.GeoscapeEventData.Choices[0].Outcome.OutcomeText.General.LocalizationKey = "SDI10_OUTCOME";
                GeoscapeEventDef sdi13 = DefCache.GetDef<GeoscapeEventDef>("SDI_13_GeoscapeEventDef");
               // sdi11.GeoscapeEventData.Choices[0].Outcome.OutcomeText.General.LocalizationKey = "SDI11_OUTCOME";
               

                GeoscapeEventDef sdi20 = DefCache.GetDef<GeoscapeEventDef>("SDI_20_GeoscapeEventDef");
                sdi20.GeoscapeEventData.Choices[0].Outcome.GameOverVictoryFaction = null;

                //Add Umbra research variable #1; this is the evolution variable for all Touched by the Void effects
                sdi3.GeoscapeEventData.Choices[0].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("Umbra_Encounter_Variable", 1, false));
                //TBTV Marked for Death effect becomes available #2
                sdi6.GeoscapeEventData.Choices[0].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("Umbra_Encounter_Variable", 1, false));
                // Umbra grow stronger #3
                sdi09.GeoscapeEventData.Choices[0].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("Umbra_Encounter_Variable", 1, false));
                sdi09.GeoscapeEventData.Choices[0].Outcome.OutcomeText.General.LocalizationKey = "SDI09_OUTCOME";
                //TBTV Reinforcements effect becomes available #4
                sdi10.GeoscapeEventData.Choices[0].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("Umbra_Encounter_Variable", 1, false));

                /*  sdi20.GeoscapeEventData.Choices[0].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("ODI_Complete", 1, true);
                  sdi20.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, NewJericho, -200);
                  sdi20.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -200);
                  sdi20.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, Synedrion, -200);
                  sdi20.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, Anu, -200);
                  sdi20.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, Synedrion, -200);
                  sdi20.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -200);
                  sdi20.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, Anu, -200);
                  sdi20.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, NewJericho, -200);
                  sdi20.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -200);*/

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


    }
}
