using Base.Defs;
using Base.Eventus.Filters;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Events.Eventus.Filters;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.ContextHelp.HintConditions;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Linq;

namespace TFTV
{




    internal class TFTVChangesToDLC4Events
    {

        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;

        public static GeoscapeEventDef FiveDeliriumReachedGeoEvent = null;
        private static string _fiveDeliriumReachedVariableName = "FiveDeliriumReached";
        public static ItemTypeTagDef MistTag = null;
        public static ResearchDef MistResearch = null;

        public static void ClearDataOnLoad()
        {
            try
            {
                FiveDeliriumReachedGeoEvent.GeoscapeEventData.Description[0].General = new Base.UI.LocalizedTextBind("KEY_FIVE_DELIRIUM_REACHED_DESC", false);
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void SoldierReachesFiveDelirium(GeoLevelController controller)
        {
            try
            {
                GeoPhoenixFaction phoenixFaction = controller.PhoenixFaction;

                if (!phoenixFaction.Soldiers.Any(s => s.CharacterStats.Corruption != null && s.CharacterStats.Corruption >= 5))
                {
                    return;//Do something
                }


                GeoCharacter geoCharacter = controller.PhoenixFaction.Soldiers.FirstOrDefault(c => c.CharacterStats.Corruption != null && c.CharacterStats.Corruption >= 5);

                if (geoCharacter == null)
                {
                    return;
                }

                if (!phoenixFaction.CorruptionHealUnlocked && controller.EventSystem.GetVariable(_fiveDeliriumReachedVariableName) != 1)
                {

                    TFTVLogger.Always($"Soldier {geoCharacter.Identity.Name} has reached 5 delirium; " +
                    $"phoenixFaction.CorruptionHealUnlocked: {phoenixFaction.CorruptionHealUnlocked}\n" +
                    $"controller.EventSystem.GetVariable(_fiveDeliriumReachedVariableName)!=1: {controller.EventSystem.GetVariable(_fiveDeliriumReachedVariableName) != 1}");

                string name = geoCharacter.Identity.Name;
                bool male = geoCharacter.Identity.SexTag == Shared.SharedGameTags.Genders.MaleTag;

                string pronoun = "KEY_GRAMMAR_PRONOUNS_HIS";

                if (!male)
                {
                    pronoun = "KEY_GRAMMAR_PRONOUNS_HER";

                }

                pronoun = TFTVCommonMethods.ConvertKeyToString(pronoun);

                string text = TFTVCommonMethods.ConvertKeyToString("TFTV_KEY_FIVE_DELIRIUM_REACHED_DESC").Replace("{Name}", name).Replace("{her/his}", pronoun);

                FiveDeliriumReachedGeoEvent.GeoscapeEventData.Description[0].General = new Base.UI.LocalizedTextBind(text, true);

                

                    GeoscapeEventContext context = new GeoscapeEventContext(phoenixFaction, phoenixFaction);

                    controller.EventSystem.TriggerGeoscapeEvent(FiveDeliriumReachedGeoEvent.EventID, context);

                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal class Defs
        {

            public static void ChangeOrCreateDefs()
            {
                try
                {
                    CreateMistTag();
                    AddMistTagToMistEmittingBodyparts();
                    CreateExperimentalTreatmentResearch();
                    CreateEventFiveDeliriumReached();
                    OriginalChangesToDLC4Defs();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void AddMistTagToMistEmittingBodyparts()
            {
                try
                {

                    DefCache.GetDef<WeaponDef>("Acheron_Arms_WeaponDef").Tags.Add(MistTag);
                    DefCache.GetDef<WeaponDef>("AcheronPrime_Arms_WeaponDef").Tags.Add(MistTag);
                    DefCache.GetDef<TacticalItemDef>("Fishman_Upper_LeftArm_MistEmitter_BodyPartDef").Tags.Add(MistTag);
                    DefCache.GetDef<TacticalItemDef>("Fishman_Upper_RightArm_MistEmitter_BodyPartDef").Tags.Add(MistTag);
                    DefCache.GetDef<TacticalItemDef>("FishmanElite_Upper_LeftArm_MistEmitter_BodyPartDef").Tags.Add(MistTag);
                    DefCache.GetDef<TacticalItemDef>("FishmanElite_Upper_RightArm_MistEmitter_BodyPartDef").Tags.Add(MistTag);
                    DefCache.GetDef<TacticalItemDef>("Queen_Carapace_MistEmitter_BodyPartDef").Tags.Add(MistTag);
                    DefCache.GetDef<WeaponDef>("Queen_Carapace_MistLauncher_WeaponDef").Tags.Add(MistTag);
                    DefCache.GetDef<TacticalItemDef>("SentinelMist_Head_BodyPartDef").Tags.Add(MistTag);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }




            private static void CreateEventFiveDeliriumReached()
            {
                try
                {
                    string name = "FiveDeliriumReached";
                    string title = "TFTV_KEY_FIVE_DELIRIUM_REACHED_TITLE";
                    string description = "TFTV_KEY_FIVE_DELIRIUM_REACHED_DESC";
                    string gUID = "{7173CBC0-D4CA-4429-AF42-764FC7BDE37F}";

                    FiveDeliriumReachedGeoEvent = TFTVCommonMethods.CreateNewEventWithFixedGUID(name, title, description, null, gUID);
                    FiveDeliriumReachedGeoEvent.GeoscapeEventData.Choices[0].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange(_fiveDeliriumReachedVariableName, 1, true));
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CreateMistTag()
            {
                try
                {
                    MistTag = Helper.CreateDefFromClone(DefCache.GetDef<ItemTypeTagDef>("ViralBodypart_TagDef"), "{2AFAD3A8-DB14-44E8-B5EC-51F4912E1774}", "TFTV_MistBodyPart_TagDef");


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CreateExperimentalTreatmentResearch()
            {
                try
                {
                    string defName = "PX_Project_Delirium_Treatment_Research";

                    string title = "PX_PROJECT_OD_TREATMENT_TITLE";
                    string reveal = "PX_PROJECT_OD_TREATMENT_REVEAL";
                    string complete = "PX_PROJECT_OD_TREATMENT_COMPLETE";
                    string unlock = "PX_PROJECT_OD_TREATMENT_UNLOCK";
                    string benefits = "PX_PROJECT_OD_TREATMENT_BENEFITS";
                    string geoObjective = "PX_PROJECT_OD_TREATMENT_GEO_OBJECTIVE";
                    string gUID = "{51C325C4-E3FD-42F6-97B1-D542798AD0BA}";
                    string gUID2 = "{BCD636C4-7D80-442C-A84B-360FE41CC930}";
                    int cost = 300;

                    ResearchDef researchDefSource = DefCache.GetDef<ResearchDef>("PX_Alien_LiveMindfragger_ResearchDef");
                    ResearchDef newResearch = TFTVCommonMethods.CreateNewPXResearch(defName, cost, gUID, gUID2, title, reveal, unlock, complete, benefits, null);

                    EncounterVariableResearchRequirementDef encounterVariableResearch =
                       TFTVCommonMethods.CreateNewEncounterVariableResearchRequirementDef(defName + "EncounterVariableResearchReq", "{D93A1ACE-1636-4908-95E6-3CA8E6ACA1F3}",
                       _fiveDeliriumReachedVariableName, 1);

                    ReseachRequirementDefOpContainer[] revealRequirementContainer = new ReseachRequirementDefOpContainer[1];
                    ResearchRequirementDef[] revealResearchRequirementDefs = new ResearchRequirementDef[1];
                    revealResearchRequirementDefs[0] = encounterVariableResearch; //small box
                    revealRequirementContainer[0].Requirements = revealResearchRequirementDefs; //medium box

                    newResearch.RevealRequirements.Container = revealRequirementContainer;
                    newResearch.RevealRequirements.Operation = ResearchContainerOperation.ALL;

                    newResearch.Tags = new ResearchTagDef[] { DefCache.GetDef<ResearchTagDef>("CriticalPath_ResearchTagDef") };


                    CaptureActorResearchRequirementDef captureActorResearchRequirement =
                        TFTVCommonMethods.CreateCaptureActorResearchRequirementDef("{46AFC14B-F1DD-4E65-9794-5F1834726B04}",
                        defName + "CaptureActorResearchReq", geoObjective, MistTag);


                    ReseachRequirementDefOpContainer[] unlockRequirementContainer = new ReseachRequirementDefOpContainer[1];
                    ResearchRequirementDef[] unlockResearchRequirementDefs = new ResearchRequirementDef[1];
                    unlockResearchRequirementDefs[0] = captureActorResearchRequirement; //small box
                    unlockRequirementContainer[0].Requirements = unlockResearchRequirementDefs; //medium box

                    newResearch.UnlockRequirements.Container = unlockRequirementContainer;
                    newResearch.UnlockRequirements.Operation = ResearchContainerOperation.ALL;

                    newResearch.Unlocks = new ResearchRewardDef[]
                    {DefCache.GetDef<UnlockFunctionalityResearchRewardDef>("PX_Alien_Acheron_ResearchDef_UnlockFunctionalityResearchRewardDef_0") };
                    MistResearch = newResearch;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void OriginalChangesToDLC4Defs()
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
}
