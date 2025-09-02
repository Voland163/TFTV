using Base.Core;
using Base.Defs;
using Base.Entities.Statuses;
using Base.Levels;
using Base.UI;
using Base.UI.MessageBox;
using HarmonyLib;
using PhoenixPoint.Common.ContextHelp;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Game;
using PhoenixPoint.Common.Saves;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Objectives;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Effects.ApplicationConditions;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static PhoenixPoint.Tactical.Entities.Statuses.TacStatusDef;

namespace TFTV
{
    internal class TFTVCommonMethods
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;


        public static void CreateNewGeoObjectivePhoenixFaction(GeoLevelController controller, string titleKey, string descriptionKey)
        {
            try
            {
                DiplomaticGeoFactionObjective newObjective = new DiplomaticGeoFactionObjective(controller.PhoenixFaction, controller.PhoenixFaction)
                {
                    Title = new LocalizedTextBind(titleKey),
                    Description = new LocalizedTextBind(descriptionKey),
                };

                controller.PhoenixFaction.AddObjective(newObjective);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        public static int D12DifficultyModifiedRoll(int unModifiedDifficultyOrder)
        {
            try
            {
                return UnityEngine.Random.Range(1, 13 - TFTVSpecialDifficulties.DifficultyOrderConverter(unModifiedDifficultyOrder));

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }


        public static void CheckGeoUIfunctionality(GeoLevelController controller)
        {
            try
            {
                if (!controller.PhoenixFaction.GameTags.Contains(DefCache.GetDef<FactionFunctionalityTagDef>("SDI_FactionFunctionalityTagDef")))
                {
                    controller.PhoenixFaction.AddTag(DefCache.GetDef<FactionFunctionalityTagDef>("SDI_FactionFunctionalityTagDef"));
                }
                // UIModuleInfoBar uIModuleInfoBar = (UIModuleInfoBar)UnityEngine.Object.FindObjectOfType(typeof(UIModuleInfoBar));


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        public static string ConvertKeyToString(string key)
        {
            try
            {

                return new LocalizedTextBind(key).Localize();


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

        public static void ClearInternalVariablesOnStateChangeAndLoad()
        {
            try
            {
                TFTVChangesToDLC4Events.ClearDataOnLoad();

                TFTVBehemothAndRaids.InternalData.BehemothDataToClearOnStateChangeAndLoad();
                
                TFTVBaseDefenseTactical.InternalData.BaseDefenseDataToClearOnStateChangeAndLoad();
               
                TFTVRevenant.InternalData.RevenantDataToClearOnStateChangeAndLoad();

                TFTVBaseDefenseGeoscape.ClearInternalDataOnStateLoadAndChange();
               
                TFTVTouchedByTheVoid.TBTVVariable = 0;
                TFTVTouchedByTheVoid.UmbraResearched = false;

                TFTVStamina.charactersWithDisabledBodyParts = new Dictionary<int, List<string>>();

                TFTVHumanEnemies.HumanEnemiesAndTactics = new Dictionary<string, int>();
                TFTVHumanEnemies.HumanEnemiesGangNames = new List<string>();
                TFTVHumanEnemiesNames.names.Clear();
                TFTVHumanEnemiesNames.CreateNamesDictionary();

                TFTVDiplomacyPenalties.VoidOmensImplemented = false;
                
                TFTVInfestation.InfestationMissionWon = false;

                TFTVUI.ShowWithoutHelmet.uIModuleSoldierCustomization = null;
                TFTVTactical.TurnZeroMethodsExecuted = false;

                TFTVAncients.CyclopsMolecularDamageBuff.Clear();
                TFTVAncientsGeo.AutomataResearched = false;
                TFTVAncients.AlertedHoplites.Clear();

                TFTVUI.EditScreen.LoadoutsAndHelmetToggle.CharacterLoadouts?.Clear();
             
                TFTVCapturePandoransGeoscape.ToxinsInCirculation = 0;
                TFTVCapturePandoransGeoscape.PandasForFoodProcessing = 0;
                TFTVCapturePandorans.ContainmentFacilityPresent = false;
                
                TFTVNewGameOptions.ConfigImplemented = false;
                TFTVNewGameOptions.InternalDifficultyCheck = 0;

                TFTVNewGameMenu.EnterStateRun = false;
              
                TFTVAmbushes.AN_FallenOnes_Hotspots = new List<int>();
                TFTVAmbushes.NJ_Purists_Hotspots = new List<int>();
              
                TFTVDelirium.CharactersDeliriumPerksAndMissions.Clear();

                TFTVPandoranProgress.ScyllaCount = 0;

                TFTVTacticalDeploymentEnemies.UndesirablesSpawned.Clear();
             
                ClearHints();
                TFTVCustomPortraits.CharacterPortrait.ClearPortraitData();

                TFTVUIGeoMap.UnpoweredFacilitiesInfo.ClearInternalDataForUIGeo();

                TFTVUITactical.ClearDataOnLoadAndStateChange();

                TFTVEvacAll.ClearData();

                TFTVDragandDropFunctionality.VehicleRoster.PlayerVehicles = new List<int>();
                TFTVDragandDropFunctionality.VehicleRoster.AircraftHotkeysBindingsApplied = false;
                TFTVVanillaFixes.UI.ShowPerceptionCirclesBindingApplied = false;    

                TFTVNJQuestline.IntroMission.ClearDataOnMissionRestartLoadAndStateChange();
              TFTVAircraftRework.InternalData.ClearDataOnStateChange();
                TFTVHavenRecruitsScreen.ClearInternalData();

                TFTVLogger.Always($"Internal variables cleared on State change or Load");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void ClearHints()
        {
            try
            {
                ContextHelpHintDbDef alwaysDisplayedTacticalHintsDbDef = DefCache.GetDef<ContextHelpHintDbDef>("AlwaysDisplayedTacticalHintsDbDef");

                foreach (ContextHelpHintDef contextHelpHintDef in TFTVHumanEnemies.TacticsHint)
                {
                    if (alwaysDisplayedTacticalHintsDbDef.Hints.Contains(contextHelpHintDef))
                    {
                        alwaysDisplayedTacticalHintsDbDef.Hints.Remove(contextHelpHintDef);
                        TFTVLogger.Always("Squad hint " + contextHelpHintDef.name + " removed");
                    }
                }

                TFTVHumanEnemies.TacticsHint.Clear();

                if (TFTVRevenant.revenantResistanceHintGUID != null)
                {
                    ContextHelpHintDef revenantResistanceHint = (ContextHelpHintDef)Repo.GetDef(TFTVRevenant.revenantResistanceHintGUID);

                    // DefCache.GetDef<ContextHelpHintDef>("RevenantResistanceSighted");
                    if (revenantResistanceHint != null && alwaysDisplayedTacticalHintsDbDef.Hints.Contains(revenantResistanceHint))
                    {
                        alwaysDisplayedTacticalHintsDbDef.Hints.Remove(revenantResistanceHint);
                        TFTVLogger.Always("Revenant resistance hint removed");
                        TFTVRevenant.revenantResistanceHintGUID = null;
                    }
                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void VariablesClearedOnlyOnLoad()
        {
            try
            {

                TFTVRevenant.InternalData.RevenantDataToClearOnLoadOnly();


                TFTVTactical.TurnZeroMethodsExecuted = false;

                TFTVAncients.CyclopsMolecularDamageBuff.Clear();
                TFTVEconomyExploitsFixes.AttackedLairSites = new Dictionary<int, int>();

                TFTVCapturePandorans.AircraftCaptureCapacity = 0;

                TFTVNewGameOptions.EtermesResistanceAndVulnerability = 0;

                TFTVBaseDefenseTactical.InternalData.BaseDefenseDataToClearOnLoadOnly();

                TFTVAircraftRework.InternalData.ClearDataOnLoad();

                TFTVLogger.Always($"Variables cleared on load");

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void VariablesClearedOnMissionRestart()
        {
            try
            {
                TFTVRevenant.InternalData.RevenantDataToClearOnMissionRestartOnly();
                TFTVBaseDefenseTactical.InternalData.BaseDefenseDataToClearOnMissionRestartOnly();


                TFTVHumanEnemies.HumanEnemiesAndTactics = new Dictionary<string, int>();
                TFTVHumanEnemies.HumanEnemiesGangNames = new List<string>();
                TFTVHumanEnemiesNames.names.Clear();
                TFTVHumanEnemiesNames.CreateNamesDictionary();

                TFTVStamina.charactersWithDisabledBodyParts = new Dictionary<int, List<string>>();

                ClearHints();
                TFTVTactical.TurnZeroMethodsExecuted = false;

                TFTVAncients.CyclopsMolecularDamageBuff.Clear();
                TFTVAncients.AlertedHoplites.Clear();
                TFTVUITactical.ClearDataOnMissionRestart();

                TFTVEvacAll.ClearData();
                TFTVTacticalDeploymentEnemies.UndesirablesSpawned.Clear();
                TFTVNJQuestline.IntroMission.ClearDataOnMissionRestartLoadAndStateChange();

                TFTVLogger.Always($"Internal variables cleared on Mission Restart");

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }


        [HarmonyPatch(typeof(UIModulePauseScreen), "ShowModule")]
        public static class TFTV_UIModulePauseScreen_ShowModule_patch
        {
            public static void Postfix(UIModulePauseScreen __instance)
            {
                try
                {
                    TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                    
                    if (controller!=null && controller.TacMission.MissionData.MissionType.name.Contains("Tutorial")) 
                    {
                        TFTVLogger.Always($"disabling save button in Tutorial");
                        __instance.SaveButton.GetComponent<PhoenixGeneralButton>().SetInteractable(false);
                    }

                   
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }



        [HarmonyPatch(typeof(UIModulePauseScreen), "OnRestartConfirmed")]
        public static class TFTV_UIModulePauseScreen_OnRestartConfirmed_RestartMission_patch
        {
            public static void Postfix(MessageBoxCallbackResult res)
            {
                try
                {
                    if (res.DialogResult == MessageBoxResult.OK || res.DialogResult == MessageBoxResult.Yes)
                    {
                        VariablesClearedOnMissionRestart();
                        TFTVLogger.Always("Game restarted");
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }



        [HarmonyPatch(typeof(PhoenixSaveManager), "LoadGame")]
        public static class BG_PhoenixSaveManager_ClearInternalData_patch
        {
            public static void Prefix()
            {
                try
                {
                    TFTVLogger.Always("LoadGame method invoked");
                    ClearInternalVariablesOnStateChangeAndLoad();
                    VariablesClearedOnlyOnLoad();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        //Method to remove manually set objective
        public static void RemoveManuallySetObjective(GeoLevelController controller, string title)
        {
            try
            {
                List<GeoFactionObjective> listOfObjectives = controller.PhoenixFaction.Objectives.ToList();

                foreach (GeoFactionObjective objective1 in listOfObjectives)
                {
                    if (objective1.Title == null)
                    {
                        // TFTVLogger.Always("objective1.Title is missing!");
                    }
                    else
                    {
                        if (objective1.Title.LocalizationKey == null)
                        {
                            // TFTVLogger.Always("objective1.Title.LocalizationKey is missing!");
                        }
                        else
                        {
                            //  TFTVLogger.Always("objective1.Title.LocalizationKey is " + objective1.Title.LocalizationKey);

                            if (objective1.Title.LocalizationKey == title)
                            {
                                controller.PhoenixFaction.RemoveObjective(objective1);
                            }
                        }
                    }

                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        [HarmonyPatch(typeof(GeoFaction), "AddTag")]
        public static class GeoPhoenix_AddTag_FavorForAFriend_Patch
        {

            public static void Postfix(GameTagDef tag, GeoFaction __instance)
            {
                try
                {
                    FactionFunctionalityTagDef alienContainmentFunctionality = DefCache.GetDef<FactionFunctionalityTagDef>("AlienContainment_FactionFunctionalityTagDef");

                    if (tag == alienContainmentFunctionality && __instance.GeoLevel.PhoenixFaction.Research.HasCompleted("PX_Alien_Acheron_ResearchDef")
                        && __instance.GeoLevel.EventSystem.GetEventRecord("PROG_CH0") == null)
                    {


                        TFTVLogger.Always($"Built containment facility and has completed PX_Alien_Acheron_ResearchDef, triggering CH0");

                        //   __instance.GeoLevel.EventSystem.SetVariable("FavorForAFriend", 1);

                        GeoscapeEventContext context = new GeoscapeEventContext(__instance.GeoLevel.PhoenixFaction.Bases.First().Site, __instance.GeoLevel.AlienFaction);
                        __instance.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_CH0", context);

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        public static void SetStaminaToZero(GeoCharacter __instance)
        {
            try
            {
                if (__instance.Fatigue != null && __instance.Fatigue.Stamina > 0 && (__instance.TemplateDef.IsHuman || __instance.TemplateDef.IsMutoid))
                {
                    __instance.Fatigue.Stamina.SetToMin();
                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static DamageMultiplierStatusDef CreateNewDescriptiveTacticalStatus(string statusName, string gUIDStatus,
            string gUIDVisuals, string title, string description, string iconFileName)
        {
            try
            {
                DamageMultiplierStatusDef source = DefCache.GetDef<DamageMultiplierStatusDef>("BionicResistances_StatusDef");
                DamageMultiplierStatusDef newStatus = Helper.CreateDefFromClone(
                    source,
                    gUIDStatus,
                    statusName);

                newStatus.Visuals = Helper.CreateDefFromClone(
                    source.Visuals,
                    gUIDVisuals,
                    statusName + "VisualsDef");

                newStatus.EffectName = statusName;
                newStatus.VisibleOnHealthbar = HealthBarVisibility.AlwaysVisible;
                newStatus.VisibleOnPassiveBar = true;
                newStatus.VisibleOnStatusScreen = StatusScreenVisibility.VisibleOnStatusesList;
                newStatus.DamageTypeDefs = new DamageTypeBaseEffectDef[1];
                newStatus.Visuals.DisplayName1.LocalizationKey = title;
                newStatus.Visuals.Description.LocalizationKey = description;
                newStatus.Visuals.LargeIcon = Helper.CreateSpriteFromImageFile(iconFileName);
                newStatus.Visuals.SmallIcon = Helper.CreateSpriteFromImageFile(iconFileName);

                //  TacticalAbilityViewElementDef visuals = (TacticalAbilityViewElementDef)newStatus.Visuals;
                //  visuals.HideFromPassives = true;
                //  visuals.ShowInStatusScreen = false;


                return newStatus;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }

        internal static ActorHasStatusEffectConditionDef CreateNewStatusEffectCondition(string gUID, StatusDef status, bool hasEffect = true)
        {
            try
            {

                string name = status.EffectName + "_ActorHasStatusEffectConditionDef";
                ActorHasStatusEffectConditionDef source = DefCache.GetDef<ActorHasStatusEffectConditionDef>("HasParalysisStatus_ApplicationCondition");

                ActorHasStatusEffectConditionDef newCondition = Helper.CreateDefFromClone(source, gUID, name);
                newCondition.StatusDef = status;
                newCondition.HasStatus = hasEffect;



                return newCondition;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        public static void GenerateGeoEventChoiceWithEventTrigger(GeoscapeEventDef geoEvent, string choice, string outcome, string eventToTrigger)
        {
            try
            {
                geoEvent.GeoscapeEventData.Choices.Add(new GeoEventChoice()
                {
                    Text = new LocalizedTextBind(choice),
                    Outcome = new GeoEventChoiceOutcome()
                    {
                        OutcomeText = new EventTextVariation()
                        {
                            General = new LocalizedTextBind(outcome)
                        },
                        TriggerEncounterID = eventToTrigger
                    }
                    
                });
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void GenerateGeoEventChoice(GeoscapeEventDef geoEvent, string choice, string outcome)
        {
            try
            {
                geoEvent.GeoscapeEventData.Choices.Add(new GeoEventChoice()
                {
                    Text = new LocalizedTextBind(choice),
                    Outcome = new GeoEventChoiceOutcome()
                    {
                        OutcomeText = new EventTextVariation()
                        {
                            General = new LocalizedTextBind(outcome)
                        }
                    }
                });
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        public static OutcomeDiplomacyChange GenerateDiplomacyOutcome(GeoFactionDef partyFaction, GeoFactionDef targetFaction, int value)
        {
            try
            {
                return new OutcomeDiplomacyChange()
                {
                    PartyFaction = partyFaction,
                    TargetFaction = targetFaction,
                    Value = value,
                    PartyType = (OutcomeDiplomacyChange.ChangeTarget)1,
                };
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }
        public static OutcomeVariableChange GenerateVariableChange(string variableName, int value, bool isSet)
        {
            try
            {
                return new OutcomeVariableChange()
                {
                    VariableName = variableName,
                    Value = { Min = value, Max = value },
                    IsSetOperation = isSet,
                };
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }

        public static GeoscapeEventDef CreateNewEventWithFixedGUID(string name, string title, string description, string outcome, string gUID)
        {
            try
            {
                GeoscapeEventDef sourceLoseGeoEvent = DefCache.GetDef<GeoscapeEventDef>("PROG_PU12_FAIL_GeoscapeEventDef");
                GeoscapeEventDef newEvent = Helper.CreateDefFromClone(sourceLoseGeoEvent, gUID, name);
                newEvent.GeoscapeEventData.Choices[0].Outcome.ReEneableEvent = false;
                newEvent.GeoscapeEventData.Choices[0].Outcome.ReactiveEncounters.Clear();
                newEvent.GeoscapeEventData.EventID = name;
                newEvent.GeoscapeEventData.Title.LocalizationKey = title;
                newEvent.GeoscapeEventData.Description[0].General.LocalizationKey = description;
                if (outcome != null)
                {
                    newEvent.GeoscapeEventData.Choices[0].Outcome.OutcomeText.General.LocalizationKey = outcome;
                }
                return newEvent;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }



        public static GeoscapeEventDef CreateNewEvent(string name, string title, string description, string outcome)
        {
            try
            {

                string gUID = Guid.NewGuid().ToString();
                GeoscapeEventDef sourceLoseGeoEvent = DefCache.GetDef<GeoscapeEventDef>("PROG_PU12_FAIL_GeoscapeEventDef");
                GeoscapeEventDef newEvent = Helper.CreateDefFromClone(sourceLoseGeoEvent, gUID, name);
                newEvent.GeoscapeEventData.Choices[0].Outcome.ReEneableEvent = false;
                newEvent.GeoscapeEventData.Choices[0].Outcome.ReactiveEncounters.Clear();
                newEvent.GeoscapeEventData.EventID = name;
                newEvent.GeoscapeEventData.Title.LocalizationKey = title;
                newEvent.GeoscapeEventData.Description[0].General.LocalizationKey = description;
                if (outcome != null)
                {
                    newEvent.GeoscapeEventData.Choices[0].Outcome.OutcomeText.General.LocalizationKey = outcome;
                }
                return newEvent;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }


        public static ResearchDef CreateResearch(string id, int cost, string key, List<string> guids, ResearchRequirementDef[] revealRequirements,
                ResearchRequirementDef[] unlockRequirements, ResearchRewardDef[] rewards, ResearchViewElementDef imageSource, ResearchContainerOperation containerOperationReveal = ResearchContainerOperation.ALL,
                ResearchContainerOperation containerOperationUnlock = ResearchContainerOperation.ALL, ResearchTagDef[] tags = null)

        {
            try
            {
                string keyName = key + "_NAME";
                string keyReveal = key + "_REVEAL";
                string keyUnlock = key + "_UNLOCK";
                string keyComplete = key + "_COMPLETE";
                string keyBenefits = key + "_BENEFITS";

                ResearchDef research = CreateNewPXResearch(id, cost, guids[0], guids[1], keyName, keyReveal, keyUnlock, keyComplete, keyBenefits, imageSource);


                if (revealRequirements != null)
                {

                    ReseachRequirementDefOpContainer[] revealRequirementContainer = new ReseachRequirementDefOpContainer[1];
                    ResearchRequirementDef[] revealResearchRequirementDefs = revealRequirements;
                    revealRequirementContainer[0].Requirements = revealResearchRequirementDefs;
                    research.RevealRequirements.Container = revealRequirementContainer;
                    research.RevealRequirements.Operation = containerOperationReveal;
                }

                if (unlockRequirements != null)
                {
                    ReseachRequirementDefOpContainer[] unlockRequirementContainer = new ReseachRequirementDefOpContainer[1];
                    ResearchRequirementDef[] unlockResearchRequirementDefs = unlockRequirements;
                    unlockRequirementContainer[0].Requirements = unlockResearchRequirementDefs;
                    research.UnlockRequirements.Container = unlockRequirementContainer;
                    research.UnlockRequirements.Operation = containerOperationUnlock;
                }

                if (rewards != null)
                {
                    research.Unlocks = rewards;
                }

                if (tags != null)
                {
                    research.Tags = tags;
                }

                return research;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        public static ExistingResearchRequirementDef[] CreateExistingResearchRequirementDefs(List <ResearchDef> requiredResearches, List <string> guids)
        {
            try 
            {
                ExistingResearchRequirementDef[] existingResearchRequirementDefs = new ExistingResearchRequirementDef[requiredResearches.Count];


                for (int i = 0; i < requiredResearches.Count; i++)
                {
                    existingResearchRequirementDefs[i] = CreateNewExistingResearchResearchRequirementDef(guids[i], requiredResearches[i].Id);
                }

                return existingResearchRequirementDefs;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

        public static ResearchDef CreateNewPXResearch(string id, int cost, string gUID, string gUID2, string name, string reveal, string unlock, string complete, string benefits, ResearchViewElementDef imageSource)

        {
            try
            {

                ResearchDef sourceResearchDef = DefCache.GetDef<ResearchDef>("PX_AtmosphericAnalysis_ResearchDef");
                ResearchDef researchDef = Helper.CreateDefFromClone(sourceResearchDef, gUID, id);
                ResearchDef secondarySourceResearchDef = DefCache.GetDef<ResearchDef>("PX_AlienGoo_ResearchDef");

                ResearchDbDef researchDB = DefCache.GetDef<ResearchDbDef>("pp_ResearchDB");
                researchDef.Id = id;
                researchDef.InitialStates[0].State = ResearchState.Hidden;
                researchDef.ResearchCost = cost;

                researchDef.Unlocks = secondarySourceResearchDef.Unlocks;
                researchDef.Tags = secondarySourceResearchDef.Tags;
                researchDB.Researches.Add(researchDef);

                ResearchViewElementDef sourceResearchViewDef = DefCache.GetDef<ResearchViewElementDef>("PX_SDI_ViewElementDef");
                ResearchViewElementDef researchViewDef = Helper.CreateDefFromClone(sourceResearchViewDef, gUID2, id + "_ViewElementDef");
                researchViewDef.DisplayName1.LocalizationKey = name;
                researchViewDef.RevealText.LocalizationKey = reveal;
                researchViewDef.UnlockText.LocalizationKey = unlock;
                researchViewDef.CompleteText.LocalizationKey = complete;
                researchViewDef.BenefitsText.LocalizationKey = benefits;

                if (imageSource != null)
                {
                    researchViewDef.ResearchIcon = imageSource.ResearchIcon;
                }

                researchDef.ViewElementDef = researchViewDef;

                return researchDef;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }
        public static ResearchViewElementDef CreateNewResearchViewElement(string def, string gUID, string name, string reveal, string unlock, string complete)

        {
            try
            {

                ResearchViewElementDef sourceResearchViewDef = DefCache.GetDef<ResearchViewElementDef>("PX_SDI_ViewElementDef");
                ResearchViewElementDef researchViewDef = Helper.CreateDefFromClone(sourceResearchViewDef, gUID, def);
                researchViewDef.DisplayName1.LocalizationKey = name;
                researchViewDef.RevealText.LocalizationKey = reveal;
                researchViewDef.UnlockText.LocalizationKey = unlock;
                researchViewDef.CompleteText.LocalizationKey = complete;
                return researchViewDef;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }
        public static ResearchViewElementDef CreateNewResearchViewElementNoKeys(string def, string gUID)

        {
            try
            {
                string shouldNotAppear = "Should Not Appear";
                ResearchViewElementDef sourceResearchViewDef = DefCache.GetDef<ResearchViewElementDef>("PX_Alien_CorruptionNode_ViewElementDef");
                ResearchViewElementDef researchViewDef = Helper.CreateDefFromClone(sourceResearchViewDef, gUID, def);
                researchViewDef.DisplayName1.LocalizationKey = shouldNotAppear;
                researchViewDef.RevealText.LocalizationKey = shouldNotAppear;
                researchViewDef.UnlockText.LocalizationKey = shouldNotAppear;
                researchViewDef.CompleteText.LocalizationKey = shouldNotAppear;
                return researchViewDef;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }
        public static CaptureActorResearchRequirementDef CreateCaptureActorResearchRequirementDef(string gUID, string defName, string revealText, GameTagDef actorTag = null, TacticalActorDef actorDef = null)
        {
            try
            {
                CaptureActorResearchRequirementDef captureActorResearchRequirementDef
                    = DefCache.GetDef<CaptureActorResearchRequirementDef>("PX_Alien_EvolvedAliens_ResearchDef_CaptureActorResearchRequirementDef_0");
                CaptureActorResearchRequirementDef newCaptureActorResearchRequirementDef = Helper.CreateDefFromClone(captureActorResearchRequirementDef, gUID, defName);
                newCaptureActorResearchRequirementDef.RequirementText.LocalizationKey = revealText;

                if (actorDef != null)
                {
                    newCaptureActorResearchRequirementDef.Actor = actorDef;
                }
                else
                {
                    newCaptureActorResearchRequirementDef.Actor = null;
                }

                if (actorTag != null) 
                { 
                   newCaptureActorResearchRequirementDef.Tag = actorTag;

                }

                return newCaptureActorResearchRequirementDef;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();

        }
        public static EncounterVariableResearchRequirementDef CreateNewEncounterVariableResearchRequirementDef(string nameDef, string gUID, string variable, int value)
        {
            try
            {
                EncounterVariableResearchRequirementDef sourceVarResReq =
                      DefCache.GetDef<EncounterVariableResearchRequirementDef>("NJ_Bionics1_ResearchDef_EncounterVariableResearchRequirementDef_0");

                EncounterVariableResearchRequirementDef newResReq = Helper.CreateDefFromClone(sourceVarResReq, gUID, nameDef);
                newResReq.Operation = EncounterVariableResearchRequirementDef.EncounterVariableRequirementOperation.GreaterOrEqual;
                newResReq.VariableName = variable;
                newResReq.Value = value;
                return newResReq;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();

        }

        public static ExistingResearchRequirementDef CreateNewExistingResearchResearchRequirementDef(string gUID, string researchID)
        {
            try
            {
                ExistingResearchRequirementDef sourceExisitingResReq =
                      DefCache.GetDef<ExistingResearchRequirementDef>("PX_PhoenixProject_ResearchDef_ExistingResearchRequirementDef_0");



                ExistingResearchRequirementDef newResReq = Helper.CreateDefFromClone(sourceExisitingResReq, gUID, $"TFTV_ExistingResearchRequirement_{researchID}");
                newResReq.ResearchID = researchID;

                return newResReq;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();

        }

        public static KeepSoldiersAliveFactionObjectiveDef CreateObjectiveReminder(string guid, string description_key, int experienceReward)
        {
            try
            {

                string objectiveName = description_key;
                KeepSoldiersAliveFactionObjectiveDef keepSoldiersAliveObjectiveSource = DefCache.GetDef<KeepSoldiersAliveFactionObjectiveDef>("KeepSoldiersAliveFactionObjectiveDef");
                KeepSoldiersAliveFactionObjectiveDef objective = Helper.CreateDefFromClone(keepSoldiersAliveObjectiveSource, guid, objectiveName);
                objective.IsVictoryObjective = false;
                objective.IsDefeatObjective = false;
                objective.MissionObjectiveData.ExperienceReward = experienceReward;
                objective.MissionObjectiveData.Description.LocalizationKey = description_key;
                objective.MissionObjectiveData.Summary.LocalizationKey = description_key;
                objective.IsUiSummaryHidden = true;
                //   TFTVLogger.Always("FactionObjective " + DefCache.GetDef<FactionObjectiveDef>(objectiveName).name + " created");

                return objective;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

        internal static GameTagDef CreateNewMissionTag(string name, string guid)
        {
            try
            {

                MissionTypeTagDef source = DefCache.GetDef<MissionTypeTagDef>("MissionTypeStoryMissionPX_MissionTagDef");
                return Helper.CreateDefFromClone(
                    source,
                    guid,
                    name);



            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }


        internal static GameTagDef CreateNewTag(string name, string guid)
        {
            try
            {

                GameTagDef source = DefCache.GetDef<GameTagDef>("Takeshi_Tutorial3_GameTagDef");
                return Helper.CreateDefFromClone(
                    source,
                    guid,
                    name + "_GameTagDef");



            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

        public static void RevealHavenUnderAttack(GeoSite geoSite, GeoLevelController controller)
        {
            try
            {
                TFTVConfig config = TFTVMain.Main.Config;

                if (config.HavenSOS)
                {
                    string sOSBroadcast = new LocalizedTextBind() { LocalizationKey = "KEY_SOS_BROADCAST" }.Localize();

                    geoSite.RevealSite(controller.PhoenixFaction);
                    GeoscapeLogEntry entry = new GeoscapeLogEntry
                    {
                        Text = new LocalizedTextBind($"{geoSite.LocalizedSiteName} ({geoSite.Owner}) {sOSBroadcast}", true)
                    };

                    typeof(GeoscapeLog).GetMethod("AddEntry", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(controller.Log, new object[] { entry, null });
                    controller.View.SetGamePauseState(true);
                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }



        internal static GeoVehicle LocateSoldier(GeoCharacter geoCharacter)
        {
            try
            {
                
                GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                foreach (GeoVehicle aircraft in controller.PhoenixFaction.Vehicles)
                {
                    if (aircraft.GetAllCharacters().Contains(geoCharacter))
                    {

                        return aircraft;

                    }
                }


                return null;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        internal static List<int> LocateOtherVehicles(int id)
        {
            try
            {
                List<int> vehicleIDs = new List<int>();

                if (id != 0)
                {
                    GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                    List<GeoVehicle> geoVehiclesAtSite = controller.PhoenixFaction?.Vehicles?.FirstOrDefault(v => v?.VehicleID == id)?.CurrentSite?.Vehicles?.Where(vs => vs?.Owner == controller.PhoenixFaction && vs?.VehicleID != id)?.ToList();

                    if (geoVehiclesAtSite != null && geoVehiclesAtSite.Count > 0)
                    {

                        foreach (GeoVehicle vehicle in geoVehiclesAtSite)
                        {
                            vehicleIDs.Add(vehicle.VehicleID);

                        }
                    }
                }

                return vehicleIDs;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        internal static bool CheckPhoenixBasePresent(int vehicleID)
        {
            try
            {
                GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                if (vehicleID == 0)
                {
                    return true;

                }

                if (controller.PhoenixFaction.Vehicles.FirstOrDefault(v => v.VehicleID == vehicleID)?.CurrentSite?.GetComponent<GeoPhoenixBase>() != null)
                {
                    return true;
                }

                return false;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }



    }
}

