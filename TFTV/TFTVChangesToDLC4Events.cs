using Base.Defs;
using Base.Eventus.Filters;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Conditions;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Events.Eventus.Filters;
using PhoenixPoint.Geoscape.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFTV
{
    internal class TFTVChangesToDLC4Events
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;

        public static void Apply_Changes()
        {

            try
            {
                GeoFactionDef PhoenixPoint = Repo.GetAllDefs<GeoFactionDef>().FirstOrDefault(ged => ged.name.Equals("Phoenix_GeoPhoenixFactionDef"));
                GeoFactionDef NewJericho = Repo.GetAllDefs<GeoFactionDef>().FirstOrDefault(ged => ged.name.Equals("NewJericho_GeoFactionDef"));
                GeoFactionDef Anu = Repo.GetAllDefs<GeoFactionDef>().FirstOrDefault(ged => ged.name.Equals("Anu_GeoFactionDef"));
                GeoFactionDef Synedrion = Repo.GetAllDefs<GeoFactionDef>().FirstOrDefault(ged => ged.name.Equals("Synedrion_GeoFactionDef"));

                // Put Barnabas in the [CHO] picture
                GeoscapeEventDef CH0Event = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_CH0_GeoscapeEventDef"));
                CH0Event.GeoscapeEventData.Leader = "SY_Barnabas";
              

                // Get corruption going from the start of the game... eh with intro to SDI
                GeoscapeEventDef geoEventCH0WIN = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_CH0_WIN_GeoscapeEventDef"));
                var corruption = geoEventCH0WIN.GeoscapeEventData.Choices[0].Outcome.VariablesChange[1];
                GeoscapeEventDef sdi1 = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("SDI_01_GeoscapeEventDef"));
                sdi1.GeoscapeEventData.Choices[0].Outcome.VariablesChange.Add(corruption);

                // Remove original Corruption variable change after winning first mission
                //geoEventCH0WIN.GeoscapeEventData.Choices[0].Outcome.VariablesChange.Remove(corruption);

                // Put Barnabas in the picture
                geoEventCH0WIN.GeoscapeEventData.Leader = "SY_Barnabas";

                // Make Acheron research available to Alien Faction without requiring completion of Unexpected Emergency, instead make it appear with Sirens and Chirons (ALN Lair Research)
                ResearchDef ALN_SirenResearch1 = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(ged => ged.name.Equals("ALN_Siren1_ResearchDef"));
                var requirementForAlienAcheronResearch = ALN_SirenResearch1.RevealRequirements.Container[0];
                ResearchDef ALN_AcheronResearch1 = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(ged => ged.name.Equals("ALN_Acheron1_ResearchDef"));
                ALN_AcheronResearch1.RevealRequirements.Container[0] = requirementForAlienAcheronResearch;

                // Make CH0 Mission appear when Player completes Acheron Autopsy and Capture and Containment 
                GeoResearchEventFilterDef PP_ResearchConditionCH0_Miss = Repo.GetAllDefs<GeoResearchEventFilterDef>().FirstOrDefault(ged => ged.name.Equals("E_PROG_CH0_ResearchCompleted [GeoResearchEventFilterDef]"));

                OrEventFilterDef triggerCH1 = Repo.GetAllDefs<OrEventFilterDef>().FirstOrDefault(ged => ged.name.Equals("E_PROG_CH1_MultipleTriggers [OrEventFilterDef]"));
                triggerCH1.OR_Filters[1] = PP_ResearchConditionCH0_Miss;
                GeoscapeEventDef CH0_Event = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_CH0_GeoscapeEventDef"));
                CH0Event.Filters[0] = triggerCH1;

                // Make CH1 Mission appear when Player win CH0 Mission; CH1 Event will not be used!
                GeoscapeEventDef CH1_Event = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_CH1_GeoscapeEventDef"));
                CH0Event.GeoscapeEventData.Conditions.Add(CH1_Event.GeoscapeEventData.Conditions[1]);
                CH0Event.GeoscapeEventData.Conditions.Add(CH1_Event.GeoscapeEventData.Conditions[3]);

                var revealSiteCH1_Miss = CH1_Event.GeoscapeEventData.Choices[0].Outcome.RevealSites[0];
                var setEventCH1_Miss = CH1_Event.GeoscapeEventData.Choices[0].Outcome.SetEvents[0];
                var trackEventCH1_Miss = CH1_Event.GeoscapeEventData.Choices[0].Outcome.TrackEncounters[0];
                GeoscapeEventDef CH0_Won = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_CH0_WIN_GeoscapeEventDef"));
                CH0_Won.GeoscapeEventData.Choices[0].Outcome.RevealSites.Add(revealSiteCH1_Miss);
                CH0_Won.GeoscapeEventData.Choices[0].Outcome.SetEvents.Add(setEventCH1_Miss);
                CH0_Won.GeoscapeEventData.Choices[0].Outcome.TrackEncounters.Add(trackEventCH1_Miss);
                CH1_Event.GeoscapeEventData.Mute = true;

                // Make Treatment available after completing Research of Escaped Specimen, instead of after Acheron Autopsy
                // Copy unlock from Autopsy research to Specimen 2 (formerly Specimen 0) research and then remove it 
                ResearchDef specimen2Research = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(ged => ged.name.Equals("PX_OriginalAcheron_ResearchDef"));
                specimen2Research.Unlocks = new ResearchRewardDef[]
                {
                        Repo.GetAllDefs<ResearchRewardDef>().FirstOrDefault(ged => ged.name.Equals("PX_Alien_Acheron_ResearchDef_UnlockPandoranSpecializationResearchRewardDef_0")),
                        Repo.GetAllDefs<ResearchRewardDef>().FirstOrDefault(ged => ged.name.Equals("PX_Alien_Acheron_ResearchDef_UnlockFunctionalityResearchRewardDef_0"))
                };
                ResearchDef acheronAutopsy = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(ged => ged.name.Equals("PX_Alien_Acheron_ResearchDef"));
                acheronAutopsy.Unlocks = new ResearchRewardDef[0];

                // Remove requirement to research Mutoid Technology to reserach Specimen 2 (former 0)
                ExistingResearchRequirementDef mutoidRequirement = Repo.GetAllDefs<ExistingResearchRequirementDef>().FirstOrDefault(ged => ged.name.Equals("PX_OriginalAcheron_ResearchDef_ExistingResearchRequirementDef_0"));
                mutoidRequirement.ResearchID = "PX_CaptureTech_ResearchDef";

                // Put Barnabas in the picture of CH1MISSWIN
                GeoscapeEventDef CH1_Won = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_CH1_WIN_GeoscapeEventDef"));
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
                GeoscapeEventDef CH_Event_NeedLair = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_CH_NEED_LAIR_GeoscapeEventDef"));
                CH_Event_NeedLair.GeoscapeEventData.Mute = true;

                // Change requirements for appearance of CH2MISS works!
                // Create new research requirements
                // Clone trigger for CH2 re Research of Specimen 2 twice
                GeoResearchEventFilterDef sourceResearchTriggerCH2 = Repo.GetAllDefs<GeoResearchEventFilterDef>().FirstOrDefault(ged => ged.name.Equals("E_PROG_CH2_ResearchCompleted [GeoResearchEventFilterDef]"));
                GeoResearchEventFilterDef newResearchTrigger1CH2 = Helper.CreateDefFromClone(sourceResearchTriggerCH2, "4A1E4DA6-A89C-4D7E-B863-FB6B429882CE", "E_PROG_CH2_ResearchCompleted [GeoResearchEventFilterDef]");
                GeoResearchEventFilterDef newResearchTrigger2CH2 = Helper.CreateDefFromClone(sourceResearchTriggerCH2, "2FE2EC90-CBA4-4473-84D7-B343277B2225", "E_PROG_CH2_ResearchCompleted [GeoResearchEventFilterDef]");
                // Set new research triggers to complete virophage research and Scylla vivisection 
                newResearchTrigger1CH2.ResearchID = "PX_VirophageWeapons_ResearchDef";
                newResearchTrigger2CH2.ResearchID = "PX_Alien_Citadel_ResearchDef";
                // Add new Research triggers to CH2 event trigger;
                OrEventFilterDef triggerCH2 = Repo.GetAllDefs<OrEventFilterDef>().FirstOrDefault(ged => ged.name.Equals("E_PROG_CH2_MultipleTriggers [OrEventFilterDef]"));
                triggerCH2.OR_Filters[0] = newResearchTrigger1CH2;
                triggerCH2.OR_Filters[1] = newResearchTrigger2CH2;
                // Clone condition 3 (Research of Specimen 2) twice
                FactionConditionDef sourceConditionCH2Research = Repo.GetAllDefs<FactionConditionDef>().FirstOrDefault(ged => ged.name.Equals("[PROG_CH2] Condition 3"));
                FactionConditionDef newCond1CH2E = Helper.CreateDefFromClone(sourceConditionCH2Research, "67D454D6-0BF3-4A13-B503-5A297EEC22CE", "[PROG_CH2] Condition 4");
                FactionConditionDef newCond2CH2E = Helper.CreateDefFromClone(sourceConditionCH2Research, "FDD644C8-A209-4B23-B3A6-C05545E6DAC7", "[PROG_CH2] Condition 5");
                // Set new conditions to complete virophage research and Scylla vivisection               
                newCond1CH2E.CompletedResearchID = "PX_VirophageWeapons_ResearchDef";
                newCond2CH2E.CompletedResearchID = "PX_Alien_Citadel_ResearchDef";
                // Add the new conditions to CH2Event
                GeoscapeEventDef CH2_Event = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_CH2_GeoscapeEventDef"));
                CH2_Event.GeoscapeEventData.Conditions.Add(newCond1CH2E);
                CH2_Event.GeoscapeEventData.Conditions.Add(newCond2CH2E);
                // Add Barnabas pic to CH2Event
                CH2_Event.GeoscapeEventData.Leader = "SY_Barnabas";
                // Remove final cinematic
                GeoscapeEventDef winCH2 = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_CH2_WIN_GeoscapeEventDef"));
                winCH2.GeoscapeEventData.Choices[0].Outcome.Cinematic = CH_Event_NeedLair.GeoscapeEventData.Choices[0].Outcome.Cinematic;
                winCH2.GeoscapeEventData.Choices[0].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("CorruptedLairDestroyed", 1, true));
                //Changes to SDI Events
                sdi1.GeoscapeEventData.Choices[0].Outcome.OutcomeText.General.LocalizationKey = "SDI1_OUTCOME";
                GeoscapeEventDef sdi3 = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("SDI_03_GeoscapeEventDef"));
                sdi3.GeoscapeEventData.Choices[0].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("Umbra_Encounter_Variable", 1, false));        
                GeoscapeEventDef sdi6 = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("SDI_06_GeoscapeEventDef"));
                sdi6.GeoscapeEventData.Choices[0].Outcome.OutcomeText.General.LocalizationKey = "SDI6_OUTCOME";
                sdi6.GeoscapeEventData.Choices[0].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("Infestation_Encounter_Variable", 1, true));
                GeoscapeEventDef sdi7 = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("SDI_07_GeoscapeEventDef"));
                //Need to fix a broken SDI event!
                sdi7.GeoscapeEventData.Choices = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("SDI_06_GeoscapeEventDef")).GeoscapeEventData.Choices;
                sdi7.GeoscapeEventData.Choices[0].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("Infestation_Encounter_Variable", 1, true));
                sdi7.GeoscapeEventData.Choices[0].Outcome.OutcomeText.General.LocalizationKey = "SDI7_OUTCOME";               
                GeoscapeEventDef sdi09 = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("SDI_09_GeoscapeEventDef"));
                sdi09.GeoscapeEventData.Choices[0].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("Umbra_Encounter_Variable", 1, false));
                GeoscapeEventDef sdi10 = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("SDI_10_GeoscapeEventDef"));
                sdi10.GeoscapeEventData.Choices[0].Outcome.OutcomeText.General.LocalizationKey = "SDI10_OUTCOME";
                GeoscapeEventDef sdi11 = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("SDI_11_GeoscapeEventDef"));
                sdi11.GeoscapeEventData.Choices[0].Outcome.OutcomeText.General.LocalizationKey = "SDI11_OUTCOME";
                sdi11.GeoscapeEventData.Choices[0].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("BerithAreComing", 1, true));

                GeoscapeEventDef sdi20 = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("SDI_20_GeoscapeEventDef"));
                sdi20.GeoscapeEventData.Choices[0].Outcome.GameOverVictoryFaction = null;
                sdi20.GeoscapeEventData.Choices[0].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("ODI_Complete", 1, true));
                sdi20.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, NewJericho, -200));
                sdi20.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, PhoenixPoint, -200));
                sdi20.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Anu, Synedrion, -200));
                sdi20.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, Anu, -200));
                sdi20.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, Synedrion, -200));
                sdi20.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(NewJericho, PhoenixPoint, -200));
                sdi20.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, Anu, -200));
                sdi20.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, NewJericho, -200));
                sdi20.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(Synedrion, PhoenixPoint, -200));

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


    }
}
