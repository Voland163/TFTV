using AK.Wwise;
using HarmonyLib;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Events.Eventus.Filters;
using PhoenixPoint.Geoscape.Levels;
using System;
using System.Collections.Generic;

namespace TFTV
{
    internal class TFTVChangesToDLC3Events
    {
        // private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static Event AugeryChant = null;

        public static void ChangesToDLC3Defs()
        {
            try
            {
                GeoFactionDef phoenixPoint = DefCache.GetDef<GeoFactionDef>("Phoenix_GeoPhoenixFactionDef");
                GeoFactionDef newJericho = DefCache.GetDef<GeoFactionDef>("NewJericho_GeoFactionDef");
                GeoFactionDef anu = DefCache.GetDef<GeoFactionDef>("Anu_GeoFactionDef");
                GeoFactionDef synedrion = DefCache.GetDef<GeoFactionDef>("Synedrion_GeoFactionDef");

                //Festering Skies changes
                // copy Augury chant from PROG_FS0 to PROG_FS9 and remove from PROG_FS0, because Augury doesn't happen and FS0 event will be used for a Sleeping Beauty Awakens
                GeoscapeEventDef geoEventFS0 = DefCache.GetDef<GeoscapeEventDef>("PROG_FS0_GeoscapeEventDef");
                if (AugeryChant == null && geoEventFS0.GeoscapeEventData.Description[0].Voiceover != null)
                {
                    AugeryChant = geoEventFS0.GeoscapeEventData.Description[0].Voiceover;
                }
                GeoscapeEventDef geoEventFS9 = DefCache.GetDef<GeoscapeEventDef>("PROG_FS9_GeoscapeEventDef");
                geoEventFS9.GeoscapeEventData.Description[0].Voiceover = AugeryChant;
                geoEventFS0.GeoscapeEventData.Description[0].Voiceover = null;
                geoEventFS9.GeoscapeEventData.Flavour = "";
                geoEventFS9.GeoscapeEventData.Choices[0].Outcome.OutcomeText.General.LocalizationKey = "PROG_FS9_OUTCOME";
                //set event timer for meteor arrival (Mount Egg)
                GeoTimePassedEventFilterDef timePassedFS9 = DefCache.GetDef<GeoTimePassedEventFilterDef>("E_PROG_FS9_TimePassed [GeoTimePassedEventFilterDef]");
                timePassedFS9.TimePassedHours = UnityEngine.Random.Range(48, 72);
                // set event timer for former Augury, now A Sleeping Beauty Awakens
                GeoTimePassedEventFilterDef timePassedFS0 = DefCache.GetDef<GeoTimePassedEventFilterDef>("E_PROG_FS0_TimePassed [GeoTimePassedEventFilterDef]");
                timePassedFS0.TimePassedHours = UnityEngine.Random.Range(200, 250);
                // set background and leader images for A Sleeping Beauty Awakens and break the panel in 2
                geoEventFS0.GeoscapeEventData.Flavour = "";
                geoEventFS0.GeoscapeEventData.Leader = "SY_Eileen";
                geoEventFS0.GeoscapeEventData.Choices[0].Outcome.OutcomeText.General.LocalizationKey = "PROG_FS0_TEXT_OUTCOME_0";
                // Give Charun research to aliens
                geoEventFS0.GeoscapeEventData.Choices[0].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("CharunAreComing", 1, true));
                // change leader image from Athena to Eileen for We Are Still Collating (former the Invitation)
                GeoscapeEventDef geoEventFS1 = DefCache.GetDef<GeoscapeEventDef>("PROG_FS1_GeoscapeEventDef");
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
                GeoscapeEventDef geoEventFS1WIN = DefCache.GetDef<GeoscapeEventDef>("PROG_FS1_WIN_GeoscapeEventDef");
                // geoEventFS1WIN.GeoscapeEventData.Choices[0].Outcome.HavenPopulationChange = -20000;
                //Allow equipment before The Hatching
                CustomMissionTypeDef storyFS1_CustomMissionTypeDef = DefCache.GetDef<CustomMissionTypeDef>("StoryFS1_CustomMissionTypeDef");
                storyFS1_CustomMissionTypeDef.SkipDeploymentSelection = false;

                // set event timer for the former The Gift mission reveal, now The Hatching
                GeoTimePassedEventFilterDef timePassedFS1 = DefCache.GetDef<GeoTimePassedEventFilterDef>("E_PROG_FS1_TimePassed [GeoTimePassedEventFilterDef]");
                timePassedFS1.TimePassedHours = UnityEngine.Random.Range(528, 600);

                // set event timer for Behemoth Egg hatching without completing, The Hatching
                GeoTimePassedEventFilterDef timePassedFS10 = DefCache.GetDef<GeoTimePassedEventFilterDef>("E_PROG_FS10_TimePassed [GeoTimePassedEventFilterDef]");
                timePassedFS10.TimePassedHours = UnityEngine.Random.Range(725, 755);

                //change event FS10 to add an Outcome panel
                GeoscapeEventDef geoEventFS10 = DefCache.GetDef<GeoscapeEventDef>("PROG_FS10_GeoscapeEventDef");
                geoEventFS10.GeoscapeEventData.Choices[0].Outcome.OutcomeText.General.LocalizationKey = "PROG_FS10_CHOICE_0_OUTCOME_GENERAL";

                //change research needed to defeat Behemoth
                //need to change the Corruption Node research, and remove reward 
                ResearchDef nodeResearchDef = DefCache.GetDef<ResearchDef>("PX_Alien_CorruptionNode_ResearchDef");
                nodeResearchDef.Unlocks = new ResearchRewardDef[] { };

                //Change FS3 event
                //Reinstating FS3 event to ensure player has access to Node
               GeoscapeEventDef geoEventFS3 = DefCache.GetDef<GeoscapeEventDef>("PROG_FS3_GeoscapeEventDef");
                geoEventFS3.GeoscapeEventData.Choices[0].Outcome.TrackEncounters.Clear();
                // geoEventFS3.GeoscapeEventData.Mute = true;
               // geoEventFS3.GeoscapeEventData.Choices[0].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("Mobilization", 1, true));
              //  geoEventFS3.GeoscapeEventData.Choices[0].Outcome.SetEvents.Clear();
                GeoTimePassedEventFilterDef timePassedFS3 = DefCache.GetDef<GeoTimePassedEventFilterDef>("E_PROG_FS3_TimePassed [GeoTimePassedEventFilterDef]");
                timePassedFS3.TimePassedHours = UnityEngine.Random.Range(950, 1100);


          

                //Remove CH2 miss 
                GeoscapeEventDef CH2_Event = DefCache.GetDef<GeoscapeEventDef>("PROG_CH2_GeoscapeEventDef");
                CH2_Event.GeoscapeEventData.Mute = true;
                //  GeoscapeEventDef CH2WIN_Event = DefCache.GetDef<GeoscapeEventDef>("PROG_CH2_WIN_GeoscapeEventDef");
                GeoscapeEventDef FS2WIN_Event = DefCache.GetDef<GeoscapeEventDef>("PROG_FS2_WIN_GeoscapeEventDef");
                //Changing for relase 1.0.2.6, Killing Behemoth will not remove Delirium  // FS2WIN_Event.GeoscapeEventData.Choices[0].Outcome.VariablesChange = CH2WIN_Event.GeoscapeEventData.Choices[0].Outcome.VariablesChange;
                FS2WIN_Event.GeoscapeEventData.Choices[0].Outcome.Diplomacy = new List<OutcomeDiplomacyChange>()
                {
                    TFTVCommonMethods.GenerateDiplomacyOutcome(anu, newJericho, 50), TFTVCommonMethods.GenerateDiplomacyOutcome(anu, synedrion, 50),
                TFTVCommonMethods.GenerateDiplomacyOutcome(newJericho, anu, 50), TFTVCommonMethods.GenerateDiplomacyOutcome(newJericho, synedrion, 50),
                TFTVCommonMethods.GenerateDiplomacyOutcome(synedrion, anu, 50), TFTVCommonMethods.GenerateDiplomacyOutcome(synedrion, newJericho, 50),
                };


                //All the stuff below was removed after new implementation, 23/9/2022


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        //Patch to destory Eillen's haven.
        [HarmonyPatch(typeof(GeoscapeEventSystem), "TriggerGeoscapeEvent")]

        public static class GeoscapeEventSystem_TriggerGeoscapeEvent_DestroyEileenHaven_patch
        {
            public static void Prefix(string eventId, GeoscapeEventSystem __instance, out GeoSite __state)
            {
                __state = null;

                try
                { 
                    if (eventId == "PROG_FS1_WIN")
                    {
                        __state = __instance.FindEventLocation("PROG_FS1_MISS");
                        TFTVLogger.Always($"eillenHaven is {__state.LocalizedSiteName}. It shall be destroyed!");
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }

            public static void Postfix(GeoSite __state)
            {
                try
                {

                    __state?.DestroySite();

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }

        }


        public static void ModifyMaskedManticoreResearch()
        {
            try
            {
                ResearchDef maskedManticoreResearchDef = DefCache.GetDef<ResearchDef>("PX_Aircraft_MaskedManticore_ResearchDef");
                ResearchViewElementDef maskedManticoreViewElementDef = DefCache.GetDef<ResearchViewElementDef>("PX_Aircraft_MaskedManticore_ViewElementDef");
                maskedManticoreViewElementDef.CompleteText.LocalizationKey = "MASKED_MANTICORE_RESEARCHDEF_TFTV";


                //In Vanilla Masked Manticore research requires researching Virophage weapons(PX_Aircraft_MaskedManticore_ResearchDef_ExistingResearchRequirementDef_0)
                //and Node autopsy(PX_Aircraft_MaskedManticore_ResearchDef_ExistingResearchRequirementDef_1), 
                //It unlocks building the Masked Manticore, PX_Aircraft_MaskedManticore_ResearchDef_ManufactureResearchRewardDef_0
                //
                //We want player to be able to build the Masked Manticore after
                //a) Node autopsy (already in) / Behemoth roaming
                //b) YuggothianEntity (PX_YuggothianEntity_ResearchDef) instead of Virophage weapons (already in)
                //C) + Citadel research if before Roaming


                //So, first let's change ResearchReqDef to replace virophage weaponry with YuggothianEntity research
                string nameNewResearchReq = "PX_Aircraft_MaskedManticore_ResearchDef_ExistingResearchRequirementDef_2";
                ExistingResearchRequirementDef virophageResearchRequirementDef = DefCache.GetDef<ExistingResearchRequirementDef>("PX_Aircraft_MaskedManticore_ResearchDef_ExistingResearchRequirementDef_0");
                virophageResearchRequirementDef.ResearchID = "PX_YuggothianEntity_ResearchDef";
                //Next,let's clone ResearchReqDef to create the AlienCitadel ReserachReq           
                ExistingResearchRequirementDef newExistingResearchRequirementDef = Helper.CreateDefFromClone(virophageResearchRequirementDef, "769F336B-DBA6-4401-BAC2-152854336DF0", nameNewResearchReq);
                newExistingResearchRequirementDef.ResearchID = "PX_Alien_Citadel_ResearchDef";
                //Next the box with Behemoth rampage or whatever
                //First need to create new encounterVariableReq
                //create new research requirement variable from a clone
                EncounterVariableResearchRequirementDef sourceVarResReq =
                      DefCache.GetDef<EncounterVariableResearchRequirementDef>("NJ_Bionics1_ResearchDef_EncounterVariableResearchRequirementDef_0");
                string name = "BehemothPatternEventTriggered";
                EncounterVariableResearchRequirementDef variableResReqBehemoth = Helper.CreateDefFromClone(sourceVarResReq, "9515C87C-0AE1-493A-ABFC-31F9B6D5B3E3", name + "ResReqDef");
                variableResReqBehemoth.VariableName = name;


                //Now, we need to create 2 separate reveal requirement containers
                //But first we need a box to put them all in
                ReseachRequirementDefOpContainer[] reseachRequirementsMaskedManticoreContainer = new ReseachRequirementDefOpContainer[1];

                // ReseachRequirementDefContainer reseachRequirementDefContainer = new ReseachRequirementDefContainer();
                // reseachRequirementDefContainer.Container = reseachRequirementsMaskedManticoreContainer;

                //Now the box with node autopsy, ye and citadel 

                //We need to extract the reveal requirement box, add a new element to it, and put it back in
                ResearchRequirementDef[] researchRequirementWithNodeDefs = new ResearchRequirementDef[4];
                researchRequirementWithNodeDefs[0] = maskedManticoreResearchDef.RevealRequirements.Container[0].Requirements[0];
                researchRequirementWithNodeDefs[1] = maskedManticoreResearchDef.RevealRequirements.Container[0].Requirements[1];
                researchRequirementWithNodeDefs[2] = newExistingResearchRequirementDef;
                researchRequirementWithNodeDefs[3] = variableResReqBehemoth;

                reseachRequirementsMaskedManticoreContainer[0].Requirements = researchRequirementWithNodeDefs;




                /*
                //now let's create the second container
                ResearchRequirementDef[] researchRequirementWithAbbadonsDefs = new ResearchRequirementDef[2];
                researchRequirementWithNodeDefs[0] = maskedManticoreResearchDef.RevealRequirements.Container[0].Requirements[0];
                researchRequirementWithNodeDefs[1] = variableResReqBehemoth;

                //now let's put both containers in the big box
                reseachRequirementsMaskedManticoreContainer[0].Requirements = researchRequirementWithNodeDefs;
                reseachRequirementsMaskedManticoreContainer[1].Requirements = researchRequirementWithAbbadonsDefs;
                reseachRequirementDefContainer.Container = reseachRequirementsMaskedManticoreContainer;
                reseachRequirementDefContainer.Operation = ResearchContainerOperation.ANY;*/

                maskedManticoreResearchDef.RevealRequirements.Container = reseachRequirementsMaskedManticoreContainer;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
    }
}