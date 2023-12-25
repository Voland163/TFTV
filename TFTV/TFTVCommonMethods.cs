using Base.Core;
using Base.Entities.Statuses;
using Base.Levels;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.ContextHelp;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.Saves;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.Levels.Objectives;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Effects.ApplicationConditions;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TFTV.PortedAATweaks;
using static PhoenixPoint.Tactical.Entities.Statuses.TacStatusDef;

namespace TFTV
{
    internal class TFTVCommonMethods
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        //  private static readonly DefRepository Repo = TFTVMain.Repo;


        public static int D12DifficultyModifiedRoll(int unModifiedDifficultyOrder)
        {
            try
            {
                return UnityEngine.Random.Range(1, 13 - TFTVReleaseOnly.DifficultyOrderConverter(unModifiedDifficultyOrder));

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

        public static void ClearInternalVariables()
        {
            try
            {
                TFTVBehemothAndRaids.targetsForBehemoth = new List<int>();
                TFTVBehemothAndRaids.flyersAndHavens = new Dictionary<int, List<int>>();
                TFTVBehemothAndRaids.checkHammerfall = false;
                TFTVRevenant.DeadSoldiersDelirium = new Dictionary<int, int>();
                TFTVTouchedByTheVoid.TBTVVariable = 0;
                TFTVRevenant.daysRevenantLastSeen = 0;
                TFTVStamina.charactersWithDisabledBodyParts = new Dictionary<int, List<string>>();
                TFTVBehemothAndRaids.behemothScenicRoute = new List<int>();
                TFTVBehemothAndRaids.behemothTarget = 0;
                TFTVBehemothAndRaids.behemothWaitHours = 12;
                TFTVRevenant.revenantSpecialResistance = new List<string>();
                TFTVRevenant.revenantCanSpawn = false;
                TFTVHumanEnemies.HumanEnemiesAndTactics = new Dictionary<string, int>();
                TFTVRevenantResearch.ProjectOsirisStats = new Dictionary<int, int[]>();
                TFTVRevenantResearch.ProjectOsiris = false;
                TFTVDiplomacyPenalties.VoidOmensImplemented = false;
                TFTVTouchedByTheVoid.UmbraResearched = false;
                TFTVHumanEnemiesNames.names.Clear();
                TFTVHumanEnemiesNames.CreateNamesDictionary();
                TFTVInfestation.InfestationMissionWon = false;
                ClearHints();
                TFTVUI.ShowWithoutHelmet.uIModuleSoldierCustomization = null;
                TFTVTactical.TurnZeroMethodsExecuted = false;
                TFTVBaseDefenseGeoscape.PhoenixBasesUnderAttack = new Dictionary<int, Dictionary<string, double>>();
                TFTVBaseDefenseGeoscape.PhoenixBasesInfested.Clear();
                TFTVBaseDefenseTactical.VentingHintShown = false;
                TFTVBaseDefenseTactical.ConsolePositions = new Dictionary<float, float>();
                TFTVAncients.CyclopsMolecularDamageBuff.Clear();
                TFTVPandoranProgress.ScyllaCount = 0;
                TFTVAncients.AutomataResearched = false;
                TFTVAncients.AlertedHoplites.Clear();
                TFTVUI.EditScreen.LoadoutsAndHelmetToggle.CharacterLoadouts?.Clear();
                TFTVCapturePandoransGeoscape.PandasForFoodProcessing = 0;
                TFTVCapturePandorans.ContainmentFacilityPresent = false;
                TFTVNewGameOptions.ConfigImplemented = false;
                TFTVNewGameOptions.InternalDifficultyCheck = 0;
                TFTVCapturePandoransGeoscape.ToxinsInCirculation = 0;
                TFTVNewGameMenu.NewGameOptionsSetUp = false;
                TFTVNewGameMenu.EnterStateRun = false;
             

                /*  TFTVNewGameOptions.AmountOfExoticResourcesSetting;
                  TFTVNewGameOptions.ResourceMultiplierSetting;
                  TFTVNewGameOptions.DiplomaticPenaltiesSetting;
                  TFTVNewGameOptions.StaminaPenaltyFromInjurySetting;
                  TFTVNewGameOptions.MoreAmbushesSetting;
                  TFTVNewGameOptions.LimitedCaptureSetting;
                  TFTVNewGameOptions.LimitedHarvestingSetting;
                  TFTVNewGameOptions.StrongerPandoransSetting;
                  TFTVNewGameOptions.ImpossibleWeaponsAdjustmentsSetting;*/

                //  TFTVUI.CurrentlyAvailableInv.Clear();
                //  TFTVUI.CurrentlyHiddenInv.Clear();
                TFTVLogger.Always($"Internal variables cleared");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


       

     /*   public static void ModifyGeoCharacterDef(GeoCharacter character, string name, List<TacticalAbilityDef> abilities,
            List<ItemDef> readySlots, List<ItemDef> armorSlots, List<ItemDef> inventorySlots, List<GameTagDef> customizationTags, int level, int[] stats) 
        {
            try
            {
                character.Rename(name);
                
                foreach(TacticalAbilityDef ab in abilities) 
                {
                    character.Progression.AddAbility(ab); 
                }

                character.Progression.LevelProgression.SetLevel(level);
               
              

                newCharacter.Data.GameTags = new List<GameTagDef>(customizationTags) { classTagDef }.ToArray();
                newCharacter.Data.Abilites = new List<TacticalAbilityDef>(abilities).ToArray();
                newCharacter.Data.EquipmentItems = new List<ItemDef>(readySlots).ToArray();
                newCharacter.Data.InventoryItems = new List<ItemDef>(inventorySlots).ToArray();
                newCharacter.Data.BodypartItems = new List<ItemDef>(armorSlots).ToArray();
                newCharacter.Data.LevelProgression.SetLevel(level);
                newCharacter.Data.Strength = stats[0];
                newCharacter.Data.Will = stats[1];
                newCharacter.Data.Speed = stats[2];


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }







        }*/




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
                if (TFTVRevenant.revenantResistanceHintCreated)
                {
                    ContextHelpHintDef revenantResistanceHint = DefCache.GetDef<ContextHelpHintDef>("RevenantResistanceSighted");
                    if (alwaysDisplayedTacticalHintsDbDef.Hints.Contains(revenantResistanceHint))
                    {
                        alwaysDisplayedTacticalHintsDbDef.Hints.Remove(revenantResistanceHint);
                        TFTVLogger.Always("Revenant resistance hint removed");
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
                TFTVRevenant.revenantSpawned = false;
                TFTVRevenant.revenantID = 0;
                TFTVTactical.TurnZeroMethodsExecuted = false;
                TFTVBaseDefenseTactical.ConsolePositions = new Dictionary<float, float>();
                TFTVBaseDefenseTactical.StratToBeAnnounced = 0;
                TFTVBaseDefenseTactical.StratToBeImplemented = 0;
                TFTVAncients.CyclopsMolecularDamageBuff.Clear();
                TFTVEconomyExploitsFixes.AttackedLairSites = new Dictionary<int, int>();
                //   TFTVAncients.AlertedHoplites.Clear();
                TFTVCapturePandorans.AircraftCaptureCapacity = 0;
                //  TFTVBaseDefenseTactical.VentingHintShown = false;
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
                // TFTVAncients.HoplitesKilled = 0;
                TFTVRevenant.revenantSpawned = false;
                TFTVRevenant.revenantID = 0;
                TFTVHumanEnemies.HumanEnemiesAndTactics = new Dictionary<string, int>();
                TFTVRevenantResearch.ProjectOsirisStats = new Dictionary<int, int[]>();
                TFTVStamina.charactersWithDisabledBodyParts = new Dictionary<int, List<string>>();
                TFTVHumanEnemiesNames.names.Clear();
                TFTVHumanEnemiesNames.CreateNamesDictionary();
                ClearHints();
                TFTVTactical.TurnZeroMethodsExecuted = false;
                TFTVAncients.CyclopsMolecularDamageBuff.Clear();
                TFTVBaseDefenseTactical.ConsolePositions = new Dictionary<float, float>();
                TFTVBaseDefenseTactical.StratToBeAnnounced = 0;
                TFTVBaseDefenseTactical.StratToBeImplemented = 0;
                TFTVBaseDefenseTactical.VentingHintShown = false;
                TFTVAncients.AlertedHoplites.Clear();

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }




        [HarmonyPatch(typeof(UIModulePauseScreen), "OnRestartConfirmed")]
        public static class TFTV_UIModulePauseScreen_OnRestartConfirmed_RestartMission_patch
        {
            public static void Postfix()
            {
                try
                {
                    VariablesClearedOnMissionRestart();
                    TFTVLogger.Always("Game restarted");

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
                    ClearInternalVariables();
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



        [HarmonyPatch(typeof(Research), "CompleteResearch")]
        public static class Research_CompleteResearch_TFTV_Patch
        {
            public static void Postfix(ResearchElement research)
            {

                try
                {
                    TFTVLogger.Always($"{research.ResearchID} completed by {research.Faction}");

                    GeoLevelController controller = research.Faction.GeoLevel;
                    GeoPhoenixFaction phoenixFaction = controller.PhoenixFaction;
                    ResearchDef mutationTech = DefCache.GetDef<ResearchDef>("ANU_MutationTech_ResearchDef");
                    ResearchElement mutationTechResearchElement = controller.PhoenixFaction.Research.GetResearchById(mutationTech.name);

                    if (research.ResearchID == "ALN_CrabmanUmbra_ResearchDef")
                    {
                        research.Faction.GeoLevel.EventSystem.SetVariable("UmbraResearched", 1);
                        TFTVLogger.Always("Umbra Researched variable is set to " + research.Faction.GeoLevel.EventSystem.GetVariable("UmbraResearched"));
                    }
                    else if (research.Faction != research.Faction.GeoLevel.PhoenixFaction && research.ResearchID == "ANU_AnuPriest_ResearchDef" && research.Faction.GeoLevel.EventSystem.GetVariable("BG_Start_Faction") == 1)
                    {

                        TFTVLogger.Always("Research completed " + research.ResearchID + " and corresponding flag triggered");

                        research.Faction.GeoLevel.PhoenixFaction.Research.GiveResearch(research, true);

                        ResearchElement phoenixResearch = controller.PhoenixFaction.Research.GetResearchById(research.ResearchID);
                        phoenixFaction.Research.CompleteResearch(phoenixResearch);
                    }

                    else if (research.Faction != phoenixFaction && research.ResearchID == "NJ_Technician_ResearchDef" && research.Faction.GeoLevel.EventSystem.GetVariable("BG_Start_Faction") == 2)
                    {
                        TFTVLogger.Always("Research completed " + research.ResearchID + " and corresponding flag triggered");

                        research.Faction.GeoLevel.PhoenixFaction.Research.GiveResearch(research, true);

                        ResearchElement phoenixResearch = controller.PhoenixFaction.Research.GetResearchById(research.ResearchID);
                        controller.PhoenixFaction.Research.CompleteResearch(phoenixResearch);
                    }
                    else if (research.Faction != phoenixFaction && research.ResearchID == "SYN_InfiltratorTech_ResearchDef" && controller.EventSystem.GetVariable("BG_Start_Faction") == 3)
                    {

                        TFTVLogger.Always("Research completed " + research.ResearchID + " and corresponding flag triggered");

                        research.Faction.GeoLevel.PhoenixFaction.Research.GiveResearch(research, true);

                        ResearchElement phoenixResearch = controller.PhoenixFaction.Research.GetResearchById(research.ResearchID);
                        phoenixFaction.Research.CompleteResearch(phoenixResearch);


                    }
                    //To trigger change of rate in Pandoran Evolution
                    else if (research.ResearchID == "ALN_Citadel_ResearchDef")
                    {
                        research.Faction.GeoLevel.EventSystem.SetVariable("Pandorans_Researched_Citadel", 1);
                        research.Faction.GeoLevel.AlienFaction.SpawnNewAlienBase();
                        GeoAlienBase citadel = research.Faction.GeoLevel.AlienFaction.Bases.FirstOrDefault(ab => ab.AlienBaseTypeDef.name == "Citadel_GeoAlienBaseTypeDef");
                        ClassTagDef queenTag = DefCache.GetDef<ClassTagDef>("Queen_ClassTagDef");
                        TacCharacterDef startingScylla = DefCache.GetDef<TacCharacterDef>("Scylla1_FrenzyMistSmasherAgileSpawner_AlienMutationVariationDef");

                        citadel.SpawnMonster(queenTag, startingScylla);

                    }
                    else if (research.ResearchID == "PX_VirophageWeapons_ResearchDef")
                    {
                        if (controller.EventSystem.GetVariable("SymesAlternativeCompleted") == 1)
                        {
                            GeoscapeEventContext context = new GeoscapeEventContext(research.Faction.GeoLevel.AlienFaction, research.Faction.GeoLevel.PhoenixFaction);
                            research.Faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("Helena_Virophage", context);

                        }
                    }


                    else if (research.ResearchID == "PX_YuggothianEntity_ResearchDef")
                    {

                        GeoscapeEventContext context = new GeoscapeEventContext(research.Faction.GeoLevel.AlienFaction, research.Faction.GeoLevel.PhoenixFaction);
                        research.Faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("AlistairOnMessagesFromTheVoid", context);

                    }
                    else if (research.ResearchID == "PX_AntediluvianArchaeology_ResearchDef")
                    {
                        GeoscapeEventContext context = new GeoscapeEventContext(research.Faction.GeoLevel.AlienFaction, research.Faction.GeoLevel.PhoenixFaction);
                        research.Faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("Helena_Echoes", context);
                    }
                    else if (research.ResearchID == "AncientAutomataResearch")
                    {
                        GeoscapeEventContext context = new GeoscapeEventContext(research.Faction.GeoLevel.AlienFaction, research.Faction.GeoLevel.PhoenixFaction);
                        research.Faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("Olena_Styx", context);

                        //  ResearchElement exoticMaterialsResearch = research.Faction.GeoLevel.PhoenixFaction.Research.GetResearchById("ExoticMaterialsResearch");
                        //  research.Faction.GeoLevel.FactionObjectiveSystem.CreateResearchObjective(research.Faction.GeoLevel.PhoenixFaction, exoticMaterialsResearch);
                    }

                    else if (research.ResearchID == "PX_LivingCrystalResearchDef")
                    {
                        GeoscapeEventContext context = new GeoscapeEventContext(research.Faction.GeoLevel.AlienFaction, research.Faction.GeoLevel.PhoenixFaction);
                        research.Faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("Helena_Oneiromancy", context);
                        // GeoscapeEventSystem eventSystem = research.Faction.GeoLevel.EventSystem;
                        // eventSystem.SetVariable("ProteanMutaneResearched", eventSystem.GetVariable("ProteanMutaneResearched") + 1);
                        TFTVAncientsGeo.DefendCyclopsStoryMission.SetReactivateCyclopsObjective(controller);
                    }
                    else if (research.ResearchID == "ExoticMaterialsResearch")
                    {
                        TFTVAncientsGeo.AncientsResearch.AncientsCheckResearchState(research.Faction.GeoLevel);
                        TFTVAncientsGeo.AncientsResearch.SetObtainLCandPMSamplesObjective(controller);

                        //   ResearchElement livingCrystalsResearch = research.Faction.GeoLevel.PhoenixFaction.Research.GetResearchById("PX_LivingCrystalResearchDef");
                        //   GeoFactionObjective researchLC = research.Faction.GeoLevel.FactionObjectiveSystem.CreateResearchObjective(research.Faction.GeoLevel.PhoenixFaction, livingCrystalsResearch);
                        //  controller.PhoenixFaction.AddObjective(researchLC);
                        //  ResearchElement proteanMutaneResearch = research.Faction.GeoLevel.PhoenixFaction.Research.GetResearchById("PX_ProteanMutaneResearchDef");
                        //  GeoFactionObjective researchPM = research.Faction.GeoLevel.FactionObjectiveSystem.CreateResearchObjective(research.Faction.GeoLevel.PhoenixFaction, proteanMutaneResearch);
                        //  controller.PhoenixFaction.AddObjective(researchPM);
                    }
                    else if (research.ResearchID == "PX_ProteanMutaneResearchDef")
                    {
                        GeoscapeEventSystem eventSystem = controller.EventSystem;
                        //  eventSystem.SetVariable("ProteanMutaneResearched", eventSystem.GetVariable("ProteanMutaneResearched") + 1);
                        TFTVAncientsGeo.DefendCyclopsStoryMission.SetReactivateCyclopsObjective(controller);
                    }

                    else if (research.ResearchID == "NJ_Bionics2_ResearchDef")
                    {

                        ResearchElement bionics3 = controller.SynedrionFaction.Research.GetResearchById("SYN_Bionics3_ResearchDef");
                        controller.SynedrionFaction.Research.GiveResearch(bionics3);
                        controller.SynedrionFaction.Research.CompleteResearch(bionics3);
                        //controller.SynedrionFaction.Research.FactionResearches.AddItem(research);
                        //controller.SynedrionFaction.Research.AddProgressToResearch(research, 700);

                    }

                    else if (research.ResearchID == "PX_Mutoid_ResearchDef" && !controller.PhoenixFaction.Research.HasCompleted(mutationTech.name) &&
                   !controller.PhoenixFaction.Research.Researchable.Any(re => re.ResearchDef == mutationTech))
                    {

                        mutationTechResearchElement.State = ResearchState.Unlocked;
                        TFTVLogger.Always($"{mutationTech.name} available to PX? {mutationTechResearchElement.IsAvailableToFaction(controller.PhoenixFaction)}");

                    }


                    FactionFunctionalityTagDef alienContainmentFunctionality = DefCache.GetDef<FactionFunctionalityTagDef>("AlienContainment_FactionFunctionalityTagDef");

                    if (research.ResearchID == "PX_Alien_Acheron_ResearchDef" && controller.PhoenixFaction.GameTags.Contains(alienContainmentFunctionality)
                        && controller.EventSystem.GetEventRecord("PROG_CH0") == null)
                    {


                        TFTVLogger.Always($"Built containment facility and has completed PX_Alien_Acheron_ResearchDef, triggering CH0");

                        //     controller.EventSystem.SetVariable("FavorForAFriend", 1);


                        GeoscapeEventContext context = new GeoscapeEventContext(controller.PhoenixFaction.Bases.First().Site, controller.AlienFaction);
                        controller.EventSystem.TriggerGeoscapeEvent("PROG_CH0", context);

                    }



                    TFTVCapturePandoransGeoscape.RefreshFoodAndMutagenProductionTooltupUI();
                    TFTVAncientsGeo.ImpossibleWeapons.CheckImpossibleWeaponsAdditionalRequirements(controller);


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
        public static CaptureActorResearchRequirementDef CreateNewTagCaptureActorResearchRequirementDef(string gUID, string defName, string revealText)
        {
            try
            {
                CaptureActorResearchRequirementDef captureActorResearchRequirementDef
                    = DefCache.GetDef<CaptureActorResearchRequirementDef>("PX_Alien_EvolvedAliens_ResearchDef_CaptureActorResearchRequirementDef_0");
                CaptureActorResearchRequirementDef newCaptureActorResearchRequirementDef = Helper.CreateDefFromClone(captureActorResearchRequirementDef, gUID, defName);
                newCaptureActorResearchRequirementDef.RequirementText = new LocalizedTextBind(revealText, true);
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

        public static ExistingResearchRequirementDef CreateNewExistingResearchResearchRequirementDef(string nameDef, string gUID, string researchID)
        {
            try
            {
                ExistingResearchRequirementDef sourceExisitingResReq =
                      DefCache.GetDef<ExistingResearchRequirementDef>("PX_PhoenixProject_ResearchDef_ExistingResearchRequirementDef_0");

                ExistingResearchRequirementDef newResReq = Helper.CreateDefFromClone(sourceExisitingResReq, gUID, nameDef);
                newResReq.ResearchID = researchID;

                return newResReq;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();

        }

        public static void CreateObjectiveReminder(string guid, string description_key, int experienceReward)
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

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
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
                        Text = new LocalizedTextBind($"{geoSite.Owner} {geoSite.LocalizedSiteName} {sOSBroadcast}", true)
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



        internal static int LocateSoldier(GeoCharacter geoCharacter)
        {
            try
            {
                int geoVehicleID = 0;
                GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                foreach (GeoVehicle aircraft in controller.PhoenixFaction.Vehicles)
                {
                    if (aircraft.GetAllCharacters().Contains(geoCharacter))
                    {

                        geoVehicleID = aircraft.VehicleID;
                        break;

                    }
                }


                return geoVehicleID;

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

