using AK.Wwise;
using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Events.Eventus.Filters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV
{
    internal class TFTVChangesToDLC3Events
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;

        private static Event AugeryChant = null;

        public static void ApplyChanges()
        {
            try
            {

                //Festering Skies changes
                // copy Augury chant from PROG_FS0 to PROG_FS9 and remove from PROG_FS0, because Augury doesn't happen and FS0 event will be used for a Sleeping Beauty Awakens
                GeoscapeEventDef geoEventFS0 = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_FS0_GeoscapeEventDef"));
                if (AugeryChant == null && geoEventFS0.GeoscapeEventData.Description[0].Voiceover != null)
                {
                    AugeryChant = geoEventFS0.GeoscapeEventData.Description[0].Voiceover;
                }
                GeoscapeEventDef geoEventFS9 = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_FS9_GeoscapeEventDef"));
                geoEventFS9.GeoscapeEventData.Description[0].Voiceover = AugeryChant;
                geoEventFS0.GeoscapeEventData.Description[0].Voiceover = null;
                geoEventFS9.GeoscapeEventData.Flavour = "";
                geoEventFS9.GeoscapeEventData.Choices[0].Outcome.OutcomeText.General.LocalizationKey = "PROG_FS9_OUTCOME";
                //set event timer for meteor arrival (Mount Egg)
                GeoTimePassedEventFilterDef timePassedFS9 = Repo.GetAllDefs<GeoTimePassedEventFilterDef>().FirstOrDefault(ged => ged.name.Equals("E_PROG_FS9_TimePassed [GeoTimePassedEventFilterDef]"));
                timePassedFS9.TimePassedHours = UnityEngine.Random.Range(48, 72);
                // set event timer for former Augury, now A Sleeping Beauty Awakens
                GeoTimePassedEventFilterDef timePassedFS0 = Repo.GetAllDefs<GeoTimePassedEventFilterDef>().FirstOrDefault(ged => ged.name.Equals("E_PROG_FS0_TimePassed [GeoTimePassedEventFilterDef]"));
                timePassedFS0.TimePassedHours = UnityEngine.Random.Range(200, 250);
                // set background and leader images for A Sleeping Beauty Awakens and break the panel in 2
                geoEventFS0.GeoscapeEventData.Flavour = "";
                geoEventFS0.GeoscapeEventData.Leader = "SY_Eileen";
                geoEventFS0.GeoscapeEventData.Choices[0].Outcome.OutcomeText.General.LocalizationKey = "PROG_FS0_TEXT_OUTCOME_0";
                // change leader image from Athena to Eileen for We Are Still Collating (former the Invitation)
                GeoscapeEventDef geoEventFS1 = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_FS1_GeoscapeEventDef"));
                geoEventFS1.GeoscapeEventData.Leader = "SY_Eileen";
                geoEventFS1.GeoscapeEventData.Choices[0].Outcome.OutcomeText.General.LocalizationKey = "PROG_FS1_OUTCOME";
                //Change FS1_Miss timer from 15 days to 5 days
                OutcomeActivateTimer outcomeActivateTimer = new OutcomeActivateTimer
                {
                    DurationDays = 5,
                    TimerID = "PROG_FS1_MISS"
                };
                geoEventFS1.GeoscapeEventData.Choices[0].Outcome.ActivateTimers[0] = outcomeActivateTimer;
                // Destroy Haven after mission
                GeoscapeEventDef geoEventFS1WIN = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_FS1_WIN_GeoscapeEventDef"));
                geoEventFS1WIN.GeoscapeEventData.Choices[0].Outcome.HavenPopulationChange = -20000;
                //Allow equipment before The Hatching
                CustomMissionTypeDef storyFS1_CustomMissionTypeDef = Repo.GetAllDefs<CustomMissionTypeDef>().FirstOrDefault(ged => ged.name.Equals("StoryFS1_CustomMissionTypeDef"));
                storyFS1_CustomMissionTypeDef.SkipDeploymentSelection = false;

                // set event timer for the former The Gift mission reveal, now The Hatching
                GeoTimePassedEventFilterDef timePassedFS1 = Repo.GetAllDefs<GeoTimePassedEventFilterDef>().FirstOrDefault(ged => ged.name.Equals("E_PROG_FS1_TimePassed [GeoTimePassedEventFilterDef]"));
                timePassedFS1.TimePassedHours = UnityEngine.Random.Range(528, 600);

                // set event timer for Behemoth Egg hatching without completing, The Hatching
                GeoTimePassedEventFilterDef timePassedFS10 = Repo.GetAllDefs<GeoTimePassedEventFilterDef>().FirstOrDefault(ged => ged.name.Equals("E_PROG_FS10_TimePassed [GeoTimePassedEventFilterDef]"));
                timePassedFS10.TimePassedHours = UnityEngine.Random.Range(725, 755);

                //change event FS10 to add an Outcome panel
                GeoscapeEventDef geoEventFS10 = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_FS10_GeoscapeEventDef"));
                geoEventFS10.GeoscapeEventData.Choices[0].Outcome.OutcomeText.General.LocalizationKey = "PROG_FS10_CHOICE_0_OUTCOME_GENERAL";

                //change research needed to defeat Behemoth
                //need to change the Corruption Node research, and remove reward 
                ResearchDef nodeResearchDef = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(ged => ged.name.Equals("PX_Alien_CorruptionNode_ResearchDef"));
                nodeResearchDef.Unlocks = new ManufactureResearchRewardDef[] { };

                //change the reward variable, in Vanilla this was node autopsy = 2, for later use
                EncounterVarResearchRewardDef encounterVarNodeAutopsyReward = Repo.GetAllDefs<EncounterVarResearchRewardDef>().FirstOrDefault
                    (ged => ged.name.Equals("PX_Alien_CorruptionNode_ResearchDef_EncounterVarResearchRewardDef_0"));
                encounterVarNodeAutopsyReward.VariableName = "IndependenceDay";
                encounterVarNodeAutopsyReward.VariableValue = 1;

                //create new research requirement variable from a clone
                EncounterVariableResearchRequirementDef sourceVarResReq =
                  Repo.GetAllDefs<EncounterVariableResearchRequirementDef>().
                  FirstOrDefault(ged => ged.name.Equals("NJ_Bionics1_ResearchDef_EncounterVariableResearchRequirementDef_0"));
                //Research to defeat Behemoth will become available after Behemoth starts the Rumpus
                EncounterVariableResearchRequirementDef variableResReqBehemoth = Helper.CreateDefFromClone(sourceVarResReq, "BABAAC81-3855-4218-B747-4FE926F34F69", "IndependenceDayResReqDef");
                variableResReqBehemoth.VariableName = "BehemothDestroyedAHaven";
                //This variable will be triggered by the event after Behemoth destroys a haven for the first time
                GeoscapeEventDef geoEventFS20 = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_FS20_GeoscapeEventDef"));
                geoEventFS20.GeoscapeEventData.Choices[0].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("BehemothDestroyedAHaven", 1, true));                

                //need to package the requirement in a new container
                //research reveal requirements go like this: it's boxes within boxes.
                //The big box is called ReseachRequirementDefContainer[], ResearchDef comes already with this box.
                //The medium box (that goes inside the big box) is called ReseachRequirementDefOpContainer[],
                //The small box (that goes inside the medium box) is called  ResearchRequirementDef[]
                //The small box is the one that will actually carry the variable, which when changed will reveal the research
                //So we start by creating the boxes and then we will put them inside each other in the right order

                ReseachRequirementDefOpContainer[] reseachRequirementIndependenceDayContainer = new ReseachRequirementDefOpContainer[1];
                ResearchRequirementDef[] researchRequirementDefs = new ResearchRequirementDef[1];
                researchRequirementDefs[0] = variableResReqBehemoth; //small box
                reseachRequirementIndependenceDayContainer[0].Requirements = researchRequirementDefs; //medium box

                //create view element
                ResearchViewElementDef independenceDayViewDef = TFTVCommonMethods.CreateNewResearchViewElement("IndependenceDayResearchViewElementDef",
                    "DC11E258-76E2-4DC8-BB60-D62AEDB2F862", "KEY_INDEPENDENCE_DAY_NAME", "KEY_INDEPENDENCE_DAY_REVEAL", "KEY_INDEPENDENCE_DAY_UNLOCK", "KEY_INDEPENDENCE_DAY_COMPLETE");
                //need to create a new research
                ResearchDef independenceDayResearchDef = TFTVCommonMethods.CreateNewPXResearch("IndependenceDayResearch", 300, "CDDFDD4D-BD1B-4434-BDD4-E0650B0DB5F2", independenceDayViewDef);
                //and add the reveal requirement we created earlier
                independenceDayResearchDef.RevealRequirements.Container.AddRangeToArray(reseachRequirementIndependenceDayContainer);
                independenceDayResearchDef.RevealRequirements.Operation = ResearchContainerOperation.ALL;
                //now add the reward
                independenceDayResearchDef.Unlocks.AddItem(encounterVarNodeAutopsyReward);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
    }

}