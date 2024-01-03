using Base;
using Base.Core;
using Base.Defs;
using Base.Platforms;
using Base.UI;
using Base.UI.MessageBox;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Home;
using PhoenixPoint.Home.View;
using PhoenixPoint.Home.View.ViewControllers;
using PhoenixPoint.Home.View.ViewModules;
using PhoenixPoint.Home.View.ViewStates;
using PhoenixPoint.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV
{
    internal class TFTVNewGameMenu
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly TFTVConfig config = TFTVMain.Main.Config;
        private static readonly SharedData Shared = TFTVMain.Shared;

        public static bool NewGameOptionsSetUp = false;

        private static int SelectedDifficulty = 0;

      

        [HarmonyPatch(typeof(HomeScreenView), "InitView")]
        public static class HomeScreenView_InitView_Patch
        {
            private static void Postfix(HomeScreenView __instance)
            {
                try
                {
                    UIModuleModManager modManagerUI = __instance.HomeScreenModules.ModManagerModule;
                    ModManager modManager = TFTVMain.Main.GetGame().ModManager;

                    foreach (ModEntry modEntry in modManager.Mods)
                    {

                        TFTVLogger.Always($"{modEntry.ID} is enabled {modEntry.Enabled}");
                        if (modEntry.Enabled && (modEntry.ID == "com.example.Better_Enemies" || modEntry.ID == "com.example.BetterVehicles"))
                        {
                            //  TFTVLogger.Always($"Should disable {modEntry.LocalizedName}");
                            //  modManager.TryDisableMod(modEntry);

                            string warning = $"{TFTVCommonMethods.ConvertKeyToString("KEY_Warning_Disable_Mod0")} {modEntry.LocalizedName.ToUpper()}{TFTVCommonMethods.ConvertKeyToString("KEY_Warning_Disable_Mod1")}";

                            GameUtl.GetMessageBox().ShowSimplePrompt(warning, MessageBoxIcon.Warning, MessageBoxButtons.OK, null);

                            //    string filePathRoot = GameUtl.GameComponent<PlatformComponent>().Platform.GetPlatformData().GetFilePathRoot();

                            //   string path = Path.Combine(filePathRoot, "ModConfig.json");
                            /*   if (File.Exists(path))
                               {
                                   File.Delete(path);

                                   //   PropertyInfo fieldInfoEnabled = typeof(ModEntry).GetProperty("Enabled", BindingFlags.Public | BindingFlags.Instance);

                                   //   fieldInfoEnabled.SetValue(modEntry, false);
                               }*/
                        }
                    }

                    Resolution resolution = Screen.currentResolution;
                    float resolutionFactorWidth = (float)resolution.width / 1920f;
                    //   TFTVLogger.Always("ResolutionFactorWidth is " + resolutionFactorWidth);
                    float resolutionFactorHeight = (float)resolution.height / 1080f;


                    GameObject source = __instance.HomeScreenModules.MainMenuButtonsModule.VanillaVisuals[0];

                    Image logoText = __instance.HomeScreenModules.MainMenuButtonsModule.VanillaVisuals[0].GetComponentsInChildren<Image>().FirstOrDefault(i => i.name == "PhoenixLogo_text");


                    Image logoImage = __instance.HomeScreenModules.MainMenuButtonsModule.VanillaVisuals[0].GetComponentsInChildren<Image>().FirstOrDefault(i => i.name == "PhoenixLogo_symbol");


                    Transform tftvLogo = UnityEngine.Object.Instantiate(source.GetComponentsInChildren<Transform>().FirstOrDefault(i => i.name == "PhoenixLogo_text"),
                        source.GetComponentsInChildren<Transform>().FirstOrDefault(i => i.name == "PhoenixLogo_text"));

                    //   TFTVLogger.Always($"logo null? {tftvLogo==null}? source.transform null? {source.transform==null} tftvLogo.gameObject.GetComponent<Image>() null? {tftvLogo.gameObject.GetComponent<Image>()==null}");

                    tftvLogo.gameObject.GetComponent<Image>().sprite = Helper.CreateSpriteFromImageFile("TFTV_logo4.png");

                    // logoText.sprite = Helper.CreateSpriteFromImageFile("TFTV_logo3.png");
                    //  logoText.transform.position += new Vector3(0, 500 * resolutionFactorHeight, 0);
                    //    tftvLogo.position += new Vector3(0, 825 * resolutionFactorHeight, 0); //470 //825
                    //  tftvLogo.position += new Vector3(220 * resolutionFactorWidth,0, 0);
                    tftvLogo.localScale *= 2.8f;
                    Vector3 pos = new Vector3() { x = tftvLogo.position.x, y = tftvLogo.position.y - 50 * resolutionFactorHeight, z = tftvLogo.position.z };
                    logoText.transform.localScale *= 0.4f;//0.65f; 
                    logoText.transform.position += new Vector3(0, -150 * resolutionFactorWidth, 0); //-25 //700
                    logoText.transform.position += new Vector3(-770 * resolutionFactorHeight, 0, 0); //-850
                    logoImage.transform.localScale *= 0.4f; //0.65f;
                    logoImage.transform.position += new Vector3(0, -260 * resolutionFactorHeight, 0); //-95 //590
                    logoImage.transform.position += new Vector3(-770 * resolutionFactorWidth, 0, 0); //-850
                    tftvLogo.transform.position = pos;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(EditionVisualsController), "DetermineEdition")]
        public static class EditionVisualsController_DetermineEdition_Patch
        {
            private static bool Prefix(EditionVisualsController __instance)
            {
                try
                {

                    __instance.SwitchToVanillaVisuals();

                    return false;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        private static int ConvertDifficultyToIndexExoticResources()
        {
            try
            {
                int difficultyOrder = SelectedDifficulty;

                switch (difficultyOrder)
                {
                    case 1: return 8;

                    case 2: return 8;

                    case 3: return 7;

                    case 4: return 5;

                    case 5: return 3;

                    case 6: return 1;


                        // { 0: "25%", 1: "50%", 2: "75%", 3: "100%", 4: "125%", 5: "150%", 6: "175%", 7: "200", 8: "250%", 9: "300%", 10 "400%"}

                        //      By default, this is set by the difficulty level: 250% on Rookie, 200% on Veteran, 150% on Hero, 100% on Legend, 50% on ETERMES
                }

                return 7;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static int ConvertDifficultyToIndexEventsResources()
        {
            try
            {
                int difficultyOrder = SelectedDifficulty;

                switch (difficultyOrder)
                {
                    case 1: return 7;

                    case 2: return 5;

                    case 3: return 4;

                    case 4: return 3;

                    case 5: return 3;

                    case 6: return 2;


                        // { 0: "25%", 1: "50%", 2: "75%", 3: "100%", 4: "125%", 5: "150%", 6: "175%", 7: "200", 8: "250%", 9: "300%", 10 "400%"}


                }

                return 7;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }




        }

        private static void UpdateOptionsOnSelectingDifficutly()
        {
            try
            {
                int diploPenalty = 1;
                int staminaDrain = 1;
                int harderAmnbush = 1;
                int strongerPandorans = 1;
                int impossibleWeapons = 1;
                int limitedHarvesting = 1;
                int limitedCapture = 1;
                int noSecondChances = 1;

                if (SelectedDifficulty > 4)
                {
                    diploPenalty = 0;
                    staminaDrain = 0;
                    harderAmnbush = 0;
                    strongerPandorans = 0;
                    impossibleWeapons = 0;
                    limitedCapture = 0;
                    limitedHarvesting = 0;
                    noSecondChances = 0;
                }
                else if (SelectedDifficulty > 2)
                {
                    diploPenalty = 0;
                    staminaDrain = 0;
                    harderAmnbush = 0;
                    impossibleWeapons = 0;
                    limitedCapture = 0;
                    limitedHarvesting = 0;
                }
                UIModuleGameSettings gameSettings = GameUtl.CurrentLevel().GetComponent<HomeScreenView>().HomeScreenModules.GameSettings;

                MethodInfo diploPenaltiesChangedCallback = typeof(UIStateNewGeoscapeGameSettings_InitFullContent_patch).GetMethod("OnDiploPenaltiesValueChangedCallback", BindingFlags.NonPublic | BindingFlags.Static);

                diploPenaltiesChangedCallback.Invoke(gameSettings, new object[] { diploPenalty });

                MethodInfo staminaDrainValueChangedCallback = typeof(UIStateNewGeoscapeGameSettings_InitFullContent_patch).GetMethod("OnStaminaDrainValueChangedCallback", BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo harderAmbushValueChangedCallback = typeof(UIStateNewGeoscapeGameSettings_InitFullContent_patch).GetMethod("OnHarderAmbushValueChangedCallback", BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo strongerPandoransValueChangedCallback = typeof(UIStateNewGeoscapeGameSettings_InitFullContent_patch).GetMethod("OnStrongerPandoransValueChangedCallback", BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo impossibleWeaponsValueChangedCallback = typeof(UIStateNewGeoscapeGameSettings_InitFullContent_patch).GetMethod("OnImpossibleWeaponsValueChangedCallback", BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo limitedHarvestingValueChangedCallback = typeof(UIStateNewGeoscapeGameSettings_InitFullContent_patch).GetMethod("OnLimitedHarvestingValueChangedCallback", BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo limitedCaptureValueChangedCallback = typeof(UIStateNewGeoscapeGameSettings_InitFullContent_patch).GetMethod("OnLimitedCaptureValueChangedCallback", BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo noSecondChancesValueChangedCallback = typeof(UIStateNewGeoscapeGameSettings_InitFullContent_patch).GetMethod("OnNoSecondChancesValueChangedCallback", BindingFlags.NonPublic | BindingFlags.Static);


                staminaDrainValueChangedCallback.Invoke(gameSettings, new object[] { staminaDrain });
                harderAmbushValueChangedCallback.Invoke(gameSettings, new object[] { harderAmnbush });
                strongerPandoransValueChangedCallback.Invoke(gameSettings, new object[] { strongerPandorans });
                impossibleWeaponsValueChangedCallback.Invoke(gameSettings, new object[] { impossibleWeapons });
                limitedHarvestingValueChangedCallback.Invoke(gameSettings, new object[] { limitedHarvesting });
                limitedCaptureValueChangedCallback.Invoke(gameSettings, new object[] { limitedCapture });
                noSecondChancesValueChangedCallback.Invoke(gameSettings, new object[] { noSecondChances });

                MethodInfo exoticResourcesChangedCallback = typeof(UIStateNewGeoscapeGameSettings_InitFullContent_patch).GetMethod("OnExoticResourcesValueChangedCallback", BindingFlags.NonPublic | BindingFlags.Static);
                exoticResourcesChangedCallback.Invoke(gameSettings, new object[] { ConvertDifficultyToIndexExoticResources() });

                MethodInfo eventResourcesChangedCallback = typeof(UIStateNewGeoscapeGameSettings_InitFullContent_patch).GetMethod("OnResourcesEventsValueChangedCallback", BindingFlags.NonPublic | BindingFlags.Static);
                eventResourcesChangedCallback.Invoke(gameSettings, new object[] { ConvertDifficultyToIndexEventsResources() });

            }


            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        [HarmonyPatch(typeof(GameOptionViewController), "OnClicked")]
        public static class OptionListViewController_Element_PointerExit_Patch
        {
            private static void Postfix(GameOptionViewController __instance)
            {
                try
                {
                    //  TFTVLogger.Always($"Element is: {__instance.Description.Localize()}");


                    if (__instance.name.Contains("TFTVDifficulty_RadioButton"))
                    {

                        UIModuleGameSettings gameSettings = GameUtl.CurrentLevel().GetComponent<HomeScreenView>().HomeScreenModules.GameSettings;

                        SelectedDifficulty = int.Parse(__instance.name.Last().ToString()) + 1;
                        TFTVLogger.Always($"Element is: {__instance.name} and the selected difficulty is now {SelectedDifficulty}");

                        UpdateOptionsOnSelectingDifficutly();

                        GameOptionViewController[] componentsInChildren = gameSettings.MainOptions.Container.GetComponentsInChildren<GameOptionViewController>();

                        MethodInfo method = typeof(PhoenixGeneralButton).GetMethod("SetNormalState", BindingFlags.NonPublic | BindingFlags.Instance);
                        MethodInfo methodAnim = typeof(PhoenixGeneralButton).GetMethod("SetAnimationState", BindingFlags.NonPublic | BindingFlags.Instance);

                        foreach (GameOptionViewController gameOptionViewController in componentsInChildren)
                        {

                            if (gameOptionViewController != __instance)
                            {
                                gameOptionViewController.SelectButton.IsSelected = false;
                                method.Invoke(gameOptionViewController.SelectButton, null);
                                methodAnim.Invoke(gameOptionViewController.SelectButton, new object[] { "HighlightedStateParameter", false });
                                gameOptionViewController.SelectButton.ResetButtonAnimations();

                            }
                        }

                        __instance.SelectButton.IsSelected = true;

                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(UIModuleGameSettings), "InitFullContent")]
        internal static class UIStateNewGeoscapeGameSettings_InitFullContent_patch
        {

            private static GameOptionViewController _scavengingOptionsVisibilityController = null;
            //   private static GameOptionViewController _geoscapeOptionsVisibilityController = null;
            //   private static GameOptionViewController _tacticalOptionsVisibilityController = null;
            //   private static GameOptionViewController _otherOptionsVisibilityController = null;

            private static GameOptionViewController _additionalStartOptionsVisibilityController = null;

            private static readonly string _titleAdditionalStartOptions = "TFTV_ADDITIONAL_START_OPTIONS_TITLE";
            private static readonly string _descriptionAdditionalStartOptions = "TFTV_ADDITIONAL_START_DESCRIPTION";

            private static GameOptionViewController _anytimeOptionsVisibilityController = null;

            private static readonly string _titleAnytimeOptions = "TFTV_ANYTIME_OPTIONS_TITLE";
            private static readonly string _descriptionAnytimeOptions = "TFTV_ANYTIME_OPTIONS_DESCRIPTION";


            private static ModSettingController _startingFactionModSettings = null;
            private static ArrowPickerController _startingFaction = null;
            private static ModSettingController _startingBaseModSettings = null;
            private static ArrowPickerController _startingBase = null;
            private static ModSettingController _startingSquadModSettings = null;
            private static ArrowPickerController _startingSquad = null;
            private static ModSettingController _startingScavSitesModSettings = null;
            private static ArrowPickerController _startingScavSites = null;
            private static ModSettingController _resCratePriorityModSettings = null;
            private static ArrowPickerController _resCratePriority = null;
            private static ModSettingController _recruitsPriorityModSettings = null;
            private static ArrowPickerController _recruitsPriority = null;
            private static ModSettingController _vehiclePriorityModSettings = null;
            private static ArrowPickerController _vehiclePriority = null;

            private static ModSettingController _limitedCaptureModSettings = null;
            private static ArrowPickerController _limitedCapture = null;
            private static ModSettingController _limitedHarvestingModSettings = null;
            private static ArrowPickerController _limitedHarvesting = null;
            private static ModSettingController _tacticalDifficultyModSettings = null;
            private static ArrowPickerController _tacticalDifficulty = null;

            private static ModSettingController _tradingModSettings = null;
            private static ArrowPickerController _trading = null;




            private static readonly string _titleTrading = "KEY_EqualizeTrade"; //"NO PROFIT FROM TRADING";
            private static readonly string _descriptionTrading = "KEY_EqualizeTrade_DESCRIPTION"; //"Trade is always 1 tech for 5 food or 5 materials, so no profit can be made from trading.";

            private static ModSettingController _limitedRaidingModSettings = null;
            private static ArrowPickerController _limitedRaiding = null;

            private static readonly string _titleLimitedRaiding = "KEY_LimitedRaiding";//"LIMITED RAIDING";
            private static readonly string _descriptionLimitedRaiding = "KEY_LimitedRaiding_DESCRIPTION";//"After a raid, all faction havens are immediately set to highest alert and may not be raided in the next 7 days.";


            private static ModSettingController _noDropReinforcementsModSettings = null;
            private static ArrowPickerController _noDropReinforcements = null;

            private static readonly string _titleNoDropReinforcements = "KEY_ReinforcementsNoDrops";//"NO ITEM DROPS FROM REINFORCEMENTS";
            private static readonly string _descriptionNoDropReinforcements = "KEY_ReinforcementsNoDrops_DESCRIPTION";//"Enemy reinforcements do not drop items on death; disallows farming for weapons on missions with infinite reinforcements.";

            private static ModSettingController _flinchingModSettings = null;
            private static ArrowPickerController _flinching = null;

            private static readonly string _titleFlinching = "KEY_AnimateWhileShooting";
            private static readonly string _descriptionFlinching = "KEY_AnimateWhileShooting_DESCRIPTION";//"The characters will continue to animate during shooting sequences and targets that are hit may flinch, causing subsequent shots in a burst to miss when shooting in freeaim mode.";

            private static ModSettingController _strongerPandoransModSettings = null;
            private static ArrowPickerController _strongerPandorans = null;

            private static readonly string _titleStrongerPandorans = "STRONGER_PANDORANS";//"STRONGER PANDORANS";
            private static readonly string _descriptionStrongerPandorans = "STRONGER_PANDORANS_DESCRIPTION";//"Applies the changes from Dtony BetterEnemies that make Pandorans more of a challenge.";

            private static ModSettingController _moreMistVOModSettings = null;
            private static ArrowPickerController _moreMistVO = null;

            private static readonly string _titleMoreMistVO = "KEY_MoreMistVO";//"PLAY WITH MORE MIST VOID OMEN";
            private static readonly string _descriptionMoreMistVO = "KEY_MoreMistVO_DESCRIPTION";//"If you are playing on a Low-end system and experience lag with this Void Omen, you can turn it off here. This will prevent it from rolling.";


            private static ModSettingController _limitedDeploymentVOModSettings = null;
            private static ArrowPickerController _limitedDeploymentVO = null;

            private static readonly string _titlelimitedDeploymentVO = "KEY_LimitedDeploymentVO";//"PLAY WITH MORE MIST VOID OMEN";
            private static readonly string _descriptionlimitedDeploymentVO = "KEY_LimitedDeploymentVO_DESCRIPTION";

            private static ModSettingController _skipMoviesModSettings = null;
            private static ArrowPickerController _skipMovies = null;

            private static readonly string _titleSkipMovies = "KEY_SkipMovies";//"SKIP MOVIES";
            private static readonly string _descriptionSkipMovies = "KEY_SkipMovies_DESCRIPTION";//"Choose whether to skip Logos on game launch, Intro and Landing cinematics. Adapted from Mad's Assorted Adjustments.";

            private static ModSettingController _exoticResourcesModSettings = null;
            private static ArrowPickerController _exoticResources = null;

            private static readonly string _titleExoticResources = "EXOTIC_RESOURCES_AMOUNT";//"AMOUNT OF EXOTIC RESOURCES";
            private static readonly string _descriptionExoticResources = "EXOTIC_RESOURCES_AMOUNT_DESCRIPTION";//"Choose the amount of Exotic Resources you want to have in your game per playthrough. Each unit provides enough resources to manufacture one set of Impossible Weapons. So, if you want to have two full sets, set this number to 2, and so on. By default, this is set by the difficulty level: 2.5 on Rookie, 2 on Veteran, 1.5 on Hero, 1 on Legend.";
            private static readonly string[] _amountPercentageResources = { "25%", "50%", "75%", "100%", "125%", "150%", "175%", "200%", "250%", "300%", "400%" };
            private static readonly float[] _amountMultiplierResources = { 0.25f, 0.5f, 0.75f, 1f, 1.25f, 1.5f, 1.75f, 2f, 2.5f, 3f, 4f };

            //   private static float[] _amountMultiplier = {0.2f, 0.4f, 0.6f, 0.8f, 1f, 1.2f, 1.4f, 1.6f, 1.8f, 2f, 2.2f, 2.4f, 2.6f, 2.8f, 3f, 3.2f, 3.4f, 3.6f, 3.8f, 4f, 4.2f, 4.4f, 4.6f, 4.8f, 5, 5.2f, 5.4f, 5.6f, 5.8f, 6};
            //   private static string[] _amountMultiplierString = { "20%", "40%", "60%", 0.8f, 1f, 1.2f, 1.4f, 1.6f, 1.8f, 2f, 2.2f, 2.4f, 2.6f, 2.8f, 3f, 3.2f, 3.4f, 3.6f, 3.8f, 4f, 4.2f, 4.4f, 4.6f, 4.8f, 5, 5.2f, 5.4f, 5.6f, 5.8f, 6 };
            private static ModSettingController _resourcesEventsModSettings = null;
            private static ArrowPickerController _resourcesEvents = null;

            private static readonly string _titleResourcesEvents = "SCALE_RESOURCE_ACQUISITION";// "SCALE RESOURCE ACQUISITION";
            private static readonly string _descriptionResourcesEvents = "SCALE_RESOURCE_ACQUISITION_DESCRIPTION";//"TFTV adjusts the amount of resources gained in Missions and Events by difficulty level. Please be aware that, for Events, TFTV 100% refers to Vanilla Pre-Azazoth patch levels (so it's actually 80% of current Vanilla amount).";

            private static ModSettingController _impossibleWeaponsModSettings = null;
            private static ArrowPickerController _impossibleWeapons = null;

            private static readonly string _titleImpossibleWeapons = "ADJUST_IMPOSSIBLE_WEAPONS";//"ADJUST IMPOSSIBLE WEAPONS";
            private static readonly string _descriptionImpossibleWeapons = "ADJUST_IMPOSSIBLE_WEAPONS_DESCRIPTION";//"In TFTV, Ancient Weapons are replaced by the Impossible Weapons (IW) " +
                                                                                                                   //  "counterparts. They have different functionality (read: they are nerfed) " +
                                                                                                                   //  "and some of them require additional faction research.  " +
                                                                                                                   //  "Check this option off to keep Impossible Weapons with the same stats and functionality as Ancient Weapons in Vanilla and without requiring additional faction research. Set to false by default on Rookie.";

            private static ModSettingController _diploPenaltiesModSettings = null;
            private static ArrowPickerController _diploPenalties = null;

            private static readonly string _titleDiploPenalties = "HIGHER_DIPLOMATIC_PENALTIES";// "HIGHER DIPLOMATIC PENALTIES";
            private static readonly string _descriptionDiploPenalties = "HIGHER_DIPLOMATIC_PENALTIES_DESCRIPTION";//"Diplomatic penalties from choices in events are doubled and revealing diplomatic missions for one faction gives a diplomatic penalty with the other factions. Can be applied to a game in progress. Set to false on Rookie by default";

            private static ModSettingController _staminaDrainModSettings = null;
            private static ArrowPickerController _staminaDrain = null;

            private static readonly string _titleStaminaDrain = "STAMINA_DRAIN";//"STAMINA DRAIN ON INJURY/AUGMENTATION";
            private static readonly string _descriptionStaminaDrain = "STAMINA_DRAIN_DESCRIPTION";//"The stamina of any operative that sustains an injury in combat that results in a disabled body part will be set to zero after the mission. The stamina of any operative that undergoes a mutation or bionic augmentation will also be set to zero.";

            private static ModSettingController _harderAmbushModSettings = null;
            private static ArrowPickerController _harderAmbush = null;

            private static readonly string _titleHarderAmbush = "HARDER_AMBUSHES";// "HARDER AMBUSHES";
            private static readonly string _descriptionHarderAmbush = "HARDER_AMBUSHES_DESCRIPTION";//"Ambushes will happen more often and will be harder. Regardless of this setting, all ambushes will have crates in them. Set to false on Rookie by default";

            private static ModSettingController _staminaRecuperationModSettings = null;
            private static ArrowPickerController _staminaRecuperation = null;

            private static readonly string _titleStaminaRecuperation = "KEY_ActivateStaminaRecuperatonModule";// STAMINA RECUPERATION FAR-M";
            private static readonly string _descriptionStaminaRecuperation = "KEY_ActivateStaminaRecuperatonModule_DESCRIPTION";//"The starting type of passenger module, FAR-M, will slowly recuperate the stamina of the operatives on board. Switch off if you prefer to have to return to base more often.";

            private static ModSettingController _disableTacSavesModSettings = null;
            private static ArrowPickerController _disableTacSaves = null;

            private static readonly string _titleDisableTacSaves = "KEY_disableSavingOnTactical";//"DISABLE SAVING ON TACTICAL";
            private static readonly string _descriptionDisableTacSaves = "KEY_disableSavingOnTactical_DESCRIPTION";//"You can still restart the mission though.";


            /*      private static ModSettingController _reverseEngineeringModSettings = null;
                  private static ArrowPickerController _reverseEngineering = null;

                  private static string _titleReverseEngineering = "ENHANCED REVERSE ENGINEERING";
                  private static string _descriptionReverseEngineering = "Reversing engineering an item allows to research the faction technology that allows manufacturing the item.";*/

            private static ModSettingController _havenSOSModSettings = null;
            private static ArrowPickerController _havenSOS = null;

            private static readonly string _titleHavenSOS = "KEY_HavenSOS";//"HAVENS SEND SOS";
            private static readonly string _descriptionHavenSOS = "KEY_HavenSOS_DESCRIPTION";//"Havens under attack will send an SOS, revealing their location to the player.";


            private static ModSettingController _learnFirstSkillModSettings = null;
            private static ArrowPickerController _learnFirstSkill = null;

            private static readonly string _titleLearnFirstSkill = "KEY_LearnFirstPersonalSkill";//"LEARN FIRST BACKGROUND PERK";
            private static readonly string _descriptionLearnFirstSkill = "KEY_LearnFirstPersonalSkill_DESCRIPTION";//"If enabled, the first personal skill (level 1) is set right after a character is created (starting soldiers, new recruits in havens, rewards, etc).";


            private static readonly string _titleTacticalDifficulty = "KEY_difficultyOnTactical";//"DIFFICULTY ON TACTICAL";
            private static readonly string _descriotionTacticalDifficulty = "KEY_difficultyOnTactical_DESCRIPTION";//"You can choose a different difficulty setting for the tactical portion of the game at any time.";
            private static readonly string[] _optionsTacticalDifficulty = { "NO_CHANGE", "TFTV_DIFFICULTY_ROOKIE_TITLE", "KEY_DIFFICULTY_EASY", "KEY_DIFFICULTY_STANDARD", "KEY_DIFFICULTY_DIFFICULT", "KEY_DIFFICULTY_VERY_DIFFICULT", "TFTV_DIFFICULTY_ETERMES_TITLE" };


            private static readonly string _titleResCratePriority = "RESOURCE_CRATE_PRIORITY";//"RESOURCE CRATE PRIORITY";
            private static readonly string _titleRecruitsPriority = "RECRUITS_PRIORITY";//"RECRUITS PRIORITY";
            private static readonly string _titleVehiclePriority = "VEHICLE_PRIORITY";// "VEHICLE PRIORITY";
            private static readonly string _descriptionScavPriority = "SCAV_PRIORITY_DESCRIPTION";//In Vanilla and default TFTV, resource crate scavenging sites are much more likely to spawn than either vehicle or personnel rescues. " +
                                                                                                  // "You can modify the relative chances of each type of scavenging site being generated. Choose none to have 0 scavenging sites of this type (for reference, high/medium/low ratio is 6/4/1)";
            private static readonly string[] _optionsResCratePriority = { "HIGH", "MEDIUM", "LOW", "NONE" };
            private static readonly string[] _optionsRecruitsPriority = { "HIGH", "MEDIUM", "LOW", "NONE" };
            private static readonly string[] _optionsVehiclePriority = { "HIGH", "MEDIUM", "LOW", "NONE" };

            private static readonly string _titleScavSites = "SCAVENGING_SITES";//"SCAVENGING SITES #";
            private static readonly string _titleLimitedCapture = "KEY_LimitedCapture";//"LIMITED CAPTURING";
            private static readonly string _titleLimitedHarvesting = "KEY_LimitedHarvesting";//"LIMITED HARVESTING";

            private static readonly string _descriptionLimitedCapture = "KEY_LimitedCapture_DESCRIPTION";//"Play with game mechanics that set a limit to how many Pandorans you can capture per mission.";
            private static readonly string _descriptionLimitedHarvesting = "KEY_LimitedHarvesting_DESCRIPTION";//"Play with game mechanics that make obtaining food or mutagens from captured Pandorans harder.";

            private static readonly string[] _optionsBool = { "YES", "NO" };


            private static readonly string _descriptionScavSites = "SCAVENGING_SITES_DESCRIPTION";//"Total number of scavenging sites generated on game start, not counting overgrown sites. (Vanilla: 16, TFTV default 8, because Ambushes generate additional resources).";
            private static readonly string[] _optionsScavSites = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23", "24", "25", "26", "27", "28", "29", "30", "31", "32" };


            private static readonly string _titleStartingFaction = "FACTION_BACKGROUND";//"FACTION BACKGROUND";
            private static readonly string _descriptionStartingFaction = "FACTION_BACKGROUND_DESCRIPTION";// "You can choose a different faction background. " +
                                                                                                          //   "If you do, one of your Assaults and your starting Heavy on Legend and Hero, " +
                                                                                                          //   "Assault on Veteran, or Sniper on Rookie will be replaced by an operative of the elite class of the Faction of your choice. " +
                                                                                                          //   "You will also get the corresponding faction technology once the faction researches it.";
            private static readonly string[] _optionsStartingFaction = { "KEY_FACTION_NAME_PHOENIX", "KEY_FACTION_NAME_ANU", "KEY_FACTION_NAME_NEW_JERICHO", "KEY_FACTION_NAME_SYNEDRION" };

            private static readonly string _titleStartingBase = "STARTING_BASE";//"STARTING BASE LOCATION";
            private static readonly string _descriptionStartingBase = "STARTING_BASE_DESCRIPTION";//"Select your starting base. You can choose a specific location to start from. Please note that some locations are harder to start from than others!";
            private static readonly string[] _optionsStartingBase = {
                "Vanilla_Random",//"Vanilla Random",
                     "Random", //"Random (ALL bases included)",
                     "Antarctica",//"Antarctica",
                     "Asia",//"Asia (China)",
                     "Australia",//"Australia",
                     "Central_America",//"Central America (Honduras)",
                     "East_Africa",//"East Africa (Ethiopia)",
                     "Eastern_Europe",//"Eastern Europe (Ukraine)",
                     "Greenland",//"Greenland",
                     "Middle_East",//"Middle East (Afghanistan)",
                     "North_Africa",//"North Africa (Algeria)",
                     "Alaska",//"North America (Alaska)",
                     "Mexico",//"North America (Mexico)",
                     "Quebec", //"North America (Quebec)",
                     "Siberia",//"Northern Asia (Siberia)",
                     "South_Africa",//"South Africa (Zimbabwe)",
                     "Bolivia",// "South America (Bolivia)",
                     "Tierra_de_Fuego",//"South America (Tierra de Fuego)",
                     "Southeast_Asia",//"Southeast Asia (Cambodia)",
                     "West_Africa"//"West Africa (Ghana)"
               };

            private static readonly string _titleStartingSquad = "STARTING_SQUAD";//"STARTING SQUAD";
            private static readonly string _descriptionStartingSquad = "STARTING_SQUAD_DESCRIPTION";//"You can choose to get a squad with random identities (as in Vanilla without doing the tutorial), " +
            //  "the Vanilla tutorial starting squad (with higher stats), " +
            //  "or a squad that will include Sophia Brown and Jacob with unbuffed stats (default on TFTV). " +
            //  "Note that Jacob is a sniper, as in the title screen :)";
            private static readonly string[] _optionsStartingSquad = { "UNBUFFED", "BUFFED", "RANDOM" };

            private static ModSettingController _noSecondChancesModSettings = null;
            private static ArrowPickerController _noSecondChances = null;


            private static readonly string _titleNoSecondChances = "NO_SECOND_CHANCES";
            private static readonly string _descriptionNoSecondChances = "NO_SECOND_CHANCES_DESCRIPTION";

            private static ModSettingController _noBarksModSettings = null;
            private static ArrowPickerController _noBarks = null;


            private static readonly string _titleNoBarks = "KEY_NoBarks";
            private static readonly string _descriptionNoBarks = "KEY_NoBarks_DESCRIPTION";

   

            private static GameOptionViewController InstantiateGameOptionViewController(RectTransform rectTransform, UIModuleGameSettings uIModuleGameSettings, string titleKey, string descriptionKey, string onToggleMethod)
            {
                try
                {

                    Resolution resolution = Screen.currentResolution;
                    float resolutionFactorWidth = (float)resolution.width / 1920f;
                    float resolutionFactorHeight = (float)resolution.height / 1080f;

                    bool ultrawideresolution = resolutionFactorWidth / resolutionFactorHeight > 2;

                    GameOptionViewController gameOptionViewController = UnityEngine.Object.Instantiate(uIModuleGameSettings.SecondaryOptions.Container.GetComponentsInChildren<GameOptionViewController>().First(), rectTransform);

                    LocalizedTextBind description = new LocalizedTextBind
                    {
                        LocalizationKey = descriptionKey // Replace with the actual localization key
                    };

                    LocalizedTextBind text = new LocalizedTextBind
                    {
                        LocalizationKey = titleKey // Replace with the actual localization key
                    };

                    gameOptionViewController.Set(text, null);

                    MethodInfo method = typeof(UIStateNewGeoscapeGameSettings_InitFullContent_patch).GetMethod(onToggleMethod, BindingFlags.NonPublic | BindingFlags.Static);
                    Action<bool> setNewSettingsVisibility = (Action<bool>)Delegate.CreateDelegate(typeof(Action<bool>), method);
                    gameOptionViewController.CheckedToggle.onValueChanged.AddListener((value) =>
                    {
                        setNewSettingsVisibility.Invoke(value);
                    });

                    // TFTVLogger.Always($"position {gameOptionViewController.transform.position} local pos {gameOptionViewController.transform.localPosition} button {gameOptionViewController.SelectButton.transform.position}");
                    if (!ultrawideresolution)
                    {
                        gameOptionViewController.SelectButton.transform.position -= new Vector3(250 * resolutionFactorWidth, 0, 0);
                    }
                    gameOptionViewController.transform.localScale *= 0.70f;


                    //  gameOptionViewController.SelectButton.transform.position += new Vector3(270 * resolutionFactorWidth, 0, 0);
                    UITooltipText uITooltipText = gameOptionViewController.gameObject.AddComponent<UITooltipText>();

                    uITooltipText.TipKey = description;

                    return gameOptionViewController;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void SetNewStartVisibility(bool show)
            {
                try
                {
                    _startingFaction.gameObject.SetActive(show);
                    _startingFactionModSettings.gameObject.SetActive(show);
                    _startingBase.gameObject.SetActive(show);
                    _startingBaseModSettings.gameObject.SetActive(show);
                    _startingSquad.gameObject.SetActive(show);
                    _startingSquadModSettings.gameObject.SetActive(show);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
            private static void SetNewStartScavVisibility(bool show)
            {
                try
                {

                    _startingScavSites.gameObject.SetActive(show);
                    _startingScavSitesModSettings.gameObject.SetActive(show);
                    _resCratePriority.gameObject.SetActive(show);
                    _resCratePriorityModSettings.gameObject.SetActive(show);
                    _recruitsPriority.gameObject.SetActive(show);
                    _recruitsPriorityModSettings.gameObject.SetActive(show);
                    _vehiclePriority.gameObject.SetActive(show);
                    _vehiclePriorityModSettings.gameObject.SetActive(show);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void SetAllVisibility(bool show)
            {
                try
                {
                    _scavengingOptionsVisibilityController.IsSelected = show;
                    _additionalStartOptionsVisibilityController.IsSelected = show;
                    _anytimeOptionsVisibilityController.IsSelected = show;
                    //   _tacticalOptionsVisibilityController.IsSelected = show;
                    //   _geoscapeOptionsVisibilityController.IsSelected = show;
                    //   _otherOptionsVisibilityController.IsSelected = show;

                    SetNewStartScavVisibility(show);
                    SetAdditionalStartOptionsVisibility(show);
                    SetAnyTimeOptionsVisibility(show);
                    //   SetMinorOptionsVisibility(show);
                    //   SetTacticalOptionsVisibility(show);
                    //   SetGeoscapeOptionsVisibility(show);            
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }


            private static void SetAdditionalStartOptionsVisibility(bool show)
            {
                try
                {
                    _staminaDrainModSettings.gameObject.SetActive(show);
                    _staminaDrain.gameObject.SetActive(show);
                    _strongerPandoransModSettings.gameObject.SetActive(show);
                    _strongerPandorans.gameObject.SetActive(show);
                    _limitedCaptureModSettings.gameObject.SetActive(show);
                    _limitedHarvestingModSettings.gameObject.SetActive(show);
                    _limitedCapture.gameObject.SetActive(show);
                    _limitedHarvesting.gameObject.SetActive(show);
                    _diploPenalties.gameObject.SetActive(show);
                    _exoticResources.gameObject.SetActive(show);
                    _harderAmbush.gameObject.SetActive(show);
                    _resourcesEvents.gameObject.SetActive(show);
                    _diploPenaltiesModSettings.gameObject.SetActive(show);
                    _exoticResourcesModSettings.gameObject.SetActive(show);
                    _harderAmbushModSettings.gameObject.SetActive(show);
                    _impossibleWeaponsModSettings.gameObject.SetActive(show);
                    _resourcesEventsModSettings.gameObject.SetActive(show);
                    _impossibleWeapons.gameObject.SetActive(show);
                    _noSecondChances.gameObject.SetActive(show);
                    _noSecondChancesModSettings.gameObject.SetActive(show);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }

            private static void SetAnyTimeOptionsVisibility(bool show)
            {
                try
                {
                    _skipMoviesModSettings.gameObject.SetActive(show);
                    _noBarksModSettings.gameObject.SetActive(show);
                    _havenSOSModSettings.gameObject.SetActive(show);
                    _staminaRecuperationModSettings.gameObject.SetActive(show);
                    _learnFirstSkillModSettings.gameObject.SetActive(show);
                    _moreMistVOModSettings.gameObject.SetActive(show);
                    _limitedDeploymentVOModSettings.gameObject.SetActive(show);
                    //    _staminaDrainModSettings.gameObject.SetActive(show);

                    _skipMovies.gameObject.SetActive(show);
                    _noBarks.gameObject.SetActive(show);

                    _havenSOS.gameObject.SetActive(show);

                    _learnFirstSkill.gameObject.SetActive(show);
                    _moreMistVO.gameObject.SetActive(show);
                    _limitedDeploymentVO.gameObject.SetActive(show);
                    _noDropReinforcements.gameObject.SetActive(show);
                    _noDropReinforcementsModSettings.gameObject.SetActive(show);
                    _flinching.gameObject.SetActive(show);
                    _flinchingModSettings.gameObject.SetActive(show);
                    _tradingModSettings.gameObject.SetActive(show);
                    _limitedRaidingModSettings.gameObject.SetActive(show);
                    //  _staminaDrain.gameObject.SetActive(show);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }




            private static void InstantiateArrowPickerController(ModSettingController modSettingController,
                ArrowPickerController arrowPickerController, string titleKey, string descriptionKey, string[] optionsKeys, int currentValue, Action<int> onValueChanged, float lengthScale)
            {
                try
                {


                    Resolution resolution = Screen.currentResolution;
                    float resolutionFactorWidth = (float)resolution.width / 1920f;
                    float resolutionFactorHeight = (float)resolution.height / 1080f;

                    bool ultrawideresolution = resolutionFactorWidth / resolutionFactorHeight > 2;

                    LocalizedTextBind titleTextBindKey = new LocalizedTextBind() { LocalizationKey = titleKey };
                    LocalizedTextBind descriptionTextBindKey = new LocalizedTextBind() { LocalizationKey = descriptionKey };

                    string title = titleTextBindKey.Localize();
                    string description = descriptionTextBindKey.Localize();


                    string[] options = new string[optionsKeys.Length];
                    if (optionsKeys[0] != "0" || optionsKeys[0] != "25%")
                    {
                        for (int i = 0; i < optionsKeys.Length; i++)
                        {
                            LocalizedTextBind optionTextBindKey = new LocalizedTextBind() { LocalizationKey = optionsKeys[i] };
                            options[i] = optionTextBindKey.Localize();
                        }

                    }
                    else
                    {
                        options = optionsKeys;
                    }

                    modSettingController.Label.text = title;
                    modSettingController.transform.localScale *= 0.75f;

                    if (!ultrawideresolution)
                    {
                        arrowPickerController.transform.position += new Vector3(270 * resolutionFactorWidth, 0, 0);

                        //   TFTVLogger.Always($"{resolutionFactorWidth} {lengthScale}");

                        if (lengthScale != 1)
                        {

                            arrowPickerController.transform.position += new Vector3(150 * resolutionFactorWidth * lengthScale, 0, 0);

                        }
                        //  TFTVLogger.Always($"{resolutionFactorWidth} {lengthScale} {arrowPickerController.transform.position}");

                        modSettingController.Label.rectTransform.Translate(new Vector3(-270 * resolutionFactorWidth, 0, 0), arrowPickerController.transform);
                    }
                    modSettingController.Label.alignment = TextAnchor.MiddleLeft;
                    UnityEngine.Object.Destroy(modSettingController.GetComponentInChildren<UITooltipText>());

                    UITooltipText uITooltipText = modSettingController.Label.gameObject.AddComponent<UITooltipText>();

                    uITooltipText.TipText = description;

                    arrowPickerController.Init(options.Length, currentValue, onValueChanged);


                    arrowPickerController.CurrentItemText.text = options[currentValue];
                    //  if (lengthScale != 1)
                    //  {
                    arrowPickerController.GetComponent<RectTransform>().sizeDelta = new Vector2(arrowPickerController.GetComponent<RectTransform>().sizeDelta.x * lengthScale, arrowPickerController.GetComponent<RectTransform>().sizeDelta.y);
                    //  }
                    // TFTVLogger.Always($"{arrowPickerController.GetComponent<RectTransform>().sizeDelta}");
                    PopulateOptions(arrowPickerController, options);
                    //TFTVLogger.Always($"instantiating {title}, got to the end");
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            /*   private static void InstantiateArrowPickerControllerForAmount(ModSettingController modSettingController,
                  ArrowPickerController arrowPickerController, string title, string description, float currentValue, Action<int> onValueChanged)
               {
                   try
                   {
                       Resolution resolution = Screen.currentResolution;
                       float resolutionFactorWidth = (float)resolution.width / 1920f;
                       float resolutionFactorHeight = (float)resolution.height / 1080f;

                       modSettingController.Label.text = title;
                       modSettingController.transform.localScale *= 0.75f;
                       arrowPickerController.transform.position += new Vector3(270 * resolutionFactorWidth, 0, 0);

                       modSettingController.Label.rectTransform.Translate(new Vector3(-270 * resolutionFactorWidth, 0, 0), arrowPickerController.transform);
                       modSettingController.Label.alignment = TextAnchor.MiddleLeft;
                       UnityEngine.Object.Destroy(modSettingController.GetComponentInChildren<UITooltipText>());

                       UITooltipText uITooltipText = modSettingController.Label.gameObject.AddComponent<UITooltipText>();

                       uITooltipText.TipText = description;

                       arrowPickerController.Init(1, 0, onValueChanged);

                       arrowPickerController.SetEnabled(true);

                       arrowPickerController.CurrentItemText.text = currentValue.ToString();

                       string[] options = { currentValue.ToString() };

                       PopulateOptions(arrowPickerController, options);

                   }

                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                       throw;
                   }
               }*/

            private static void Postfix(UIModuleGameSettings __instance)
            {
                try
                {
                    ModSettingController ModSettingControllerHook = GameUtl.CurrentLevel().GetComponent<HomeScreenView>().HomeScreenModules.ModManagerModule.SettingsModSettingPrefab;
                    RectTransform rectTransform = __instance.GameAddiotionalContentGroup.GetComponentInChildren<RectTransform>();

                   

                    rectTransform.DestroyChildren();


                    //  InstantiateGameOptionViewController(rectTransform, __instance, "testing_title", "testing_description", "SetNewStartVisibility");

                    _startingFactionModSettings = UnityEngine.Object.Instantiate(ModSettingControllerHook, rectTransform);
                    _startingBaseModSettings = UnityEngine.Object.Instantiate(ModSettingControllerHook, rectTransform);
                    _startingSquadModSettings = UnityEngine.Object.Instantiate(ModSettingControllerHook, rectTransform);
                    _tacticalDifficultyModSettings = UnityEngine.Object.Instantiate(ModSettingControllerHook, rectTransform);
                    _disableTacSavesModSettings = UnityEngine.Object.Instantiate(ModSettingControllerHook, rectTransform);

                    InstantiateGameOptionViewController(rectTransform, __instance, "TFTV_ALL_OPTIONS_TITLE", "TFTV_ALL_OPTIONS_DESCRIPTION", "SetAllVisibility");

                    _scavengingOptionsVisibilityController = InstantiateGameOptionViewController(rectTransform, __instance, "TFTV_SCAVENGING_OPTIONS_TITLE", "TFTV_SCAVENGING_OPTIONS_DESCRIPTION", "SetNewStartScavVisibility");

                    _startingScavSitesModSettings = UnityEngine.Object.Instantiate(ModSettingControllerHook, rectTransform);
                    _resCratePriorityModSettings = UnityEngine.Object.Instantiate(ModSettingControllerHook, rectTransform);
                    _recruitsPriorityModSettings = UnityEngine.Object.Instantiate(ModSettingControllerHook, rectTransform);
                    _vehiclePriorityModSettings = UnityEngine.Object.Instantiate(ModSettingControllerHook, rectTransform);

                    _additionalStartOptionsVisibilityController = InstantiateGameOptionViewController(rectTransform, __instance, _titleAdditionalStartOptions, _descriptionAdditionalStartOptions, "SetAdditionalStartOptionsVisibility");
                    //  _geoscapeOptionsVisibilityController = InstantiateGameOptionViewController(rectTransform, __instance, "TFTV_GEOSCAPE_OPTIONS_TITLE", "TFTV_GEOSCAPE_OPTIONS_DESCRIPTION", "SetGeoscapeOptionsVisibility");

                    _strongerPandoransModSettings = UnityEngine.Object.Instantiate(ModSettingControllerHook, rectTransform);
                    _staminaDrainModSettings = UnityEngine.Object.Instantiate(ModSettingControllerHook, rectTransform);

                    //Geoscape
                    _limitedCaptureModSettings = UnityEngine.Object.Instantiate(ModSettingControllerHook, rectTransform);
                    _limitedHarvestingModSettings = UnityEngine.Object.Instantiate(ModSettingControllerHook, rectTransform);


                    _diploPenaltiesModSettings = UnityEngine.Object.Instantiate(ModSettingControllerHook, rectTransform);
                    _exoticResourcesModSettings = UnityEngine.Object.Instantiate(ModSettingControllerHook, rectTransform);
                    _harderAmbushModSettings = UnityEngine.Object.Instantiate(ModSettingControllerHook, rectTransform);
                    _impossibleWeaponsModSettings = UnityEngine.Object.Instantiate(ModSettingControllerHook, rectTransform);
                    _noSecondChancesModSettings = UnityEngine.Object.Instantiate(ModSettingControllerHook, rectTransform);
                    //    _reverseEngineeringModSettings = UnityEngine.Object.Instantiate(ModSettingControllerHook, rectTransform);
                    _resourcesEventsModSettings = UnityEngine.Object.Instantiate(ModSettingControllerHook, rectTransform);

                    _anytimeOptionsVisibilityController = InstantiateGameOptionViewController(rectTransform, __instance, _titleAnytimeOptions, _descriptionAnytimeOptions, "SetAnyTimeOptionsVisibility");

                    //_tacticalOptionsVisibilityController = InstantiateGameOptionViewController(rectTransform, __instance, "TFTV_TACTICAL_OPTIONS_TITLE", "TFTV_TACTICAL_OPTIONS_DESCRIPTION", "SetTacticalOptionsVisibility");

                    //Tactical

                    _noDropReinforcementsModSettings = UnityEngine.Object.Instantiate(ModSettingControllerHook, rectTransform);
                    _flinchingModSettings = UnityEngine.Object.Instantiate(ModSettingControllerHook, rectTransform);


                    //  _otherOptionsVisibilityController = InstantiateGameOptionViewController(rectTransform, __instance, "TFTV_MISC_OPTIONS_TITLE", "TFTV_MISC_OPTIONS_DESCRIPTION", "SetMinorOptionsVisibility");

                    //Minor settings
                    _skipMoviesModSettings = UnityEngine.Object.Instantiate(ModSettingControllerHook, rectTransform);
                    _havenSOSModSettings = UnityEngine.Object.Instantiate(ModSettingControllerHook, rectTransform);
                    _staminaRecuperationModSettings = UnityEngine.Object.Instantiate(ModSettingControllerHook, rectTransform);
                    _learnFirstSkillModSettings = UnityEngine.Object.Instantiate(ModSettingControllerHook, rectTransform);
                    _moreMistVOModSettings = UnityEngine.Object.Instantiate(ModSettingControllerHook, rectTransform);
                    _tradingModSettings = UnityEngine.Object.Instantiate(ModSettingControllerHook, rectTransform);
                    _limitedDeploymentVOModSettings = UnityEngine.Object.Instantiate(ModSettingControllerHook, rectTransform);
                    _limitedRaidingModSettings = UnityEngine.Object.Instantiate(ModSettingControllerHook, rectTransform);
                    _noBarksModSettings = UnityEngine.Object.Instantiate(ModSettingControllerHook, rectTransform);


                    _startingFaction = _startingFactionModSettings.ListField;
                    _startingBase = _startingBaseModSettings.ListField;
                    _startingSquad = _startingSquadModSettings.ListField;
                    _startingScavSites = _startingScavSitesModSettings.ListField;
                    _resCratePriority = _resCratePriorityModSettings.ListField;
                    _recruitsPriority = _recruitsPriorityModSettings.ListField;
                    _vehiclePriority = _vehiclePriorityModSettings.ListField;
                    _limitedCapture = _limitedCaptureModSettings.ListField;
                    _limitedHarvesting = _limitedHarvestingModSettings.ListField;
                    _tacticalDifficulty = _tacticalDifficultyModSettings.ListField;
                    _trading = _tradingModSettings.ListField;
                    _limitedRaiding = _limitedRaidingModSettings.ListField;
                    _noDropReinforcements = _noDropReinforcementsModSettings.ListField;
                    _diploPenalties = _diploPenaltiesModSettings.ListField;
                    _exoticResources = _exoticResourcesModSettings.ListField;
                    _flinching = _flinchingModSettings.ListField;
                    _harderAmbush = _harderAmbushModSettings.ListField;
                    _havenSOS = _havenSOSModSettings.ListField;
                    _impossibleWeapons = _impossibleWeaponsModSettings.ListField;
                    _noSecondChances = _noSecondChancesModSettings.ListField;
                    _learnFirstSkill = _learnFirstSkillModSettings.ListField;
                    _moreMistVO = _moreMistVOModSettings.ListField;
                    _limitedDeploymentVO = _limitedDeploymentVOModSettings.ListField;
                    //   _reverseEngineering = _reverseEngineeringModSettings.ListField;
                    _skipMovies = _skipMoviesModSettings.ListField;
                    _resourcesEvents = _resourcesEventsModSettings.ListField;
                    // _reverseEngineering = _reverseEngineeringModSettings.ListField;
                    _disableTacSaves = _disableTacSavesModSettings.ListField;
                    _staminaDrain = _staminaDrainModSettings.ListField;
                    _staminaRecuperation = _staminaRecuperationModSettings.ListField;
                    _strongerPandorans = _strongerPandoransModSettings.ListField;
                    _noBarks = _noBarksModSettings.ListField;


                    InstantiateArrowPickerController(_startingFactionModSettings, _startingFaction, _titleStartingFaction, _descriptionStartingFaction, _optionsStartingFaction, (int)(TFTVNewGameOptions.startingSquad), OnStartingFactionValueChangedCallback, 1f);
                    InstantiateArrowPickerController(_startingBaseModSettings, _startingBase, _titleStartingBase, _descriptionStartingBase, _optionsStartingBase, (int)(TFTVNewGameOptions.startingBaseLocation), OnStartingBaseValueChangedCallback, 1f);
                    InstantiateArrowPickerController(_startingSquadModSettings, _startingSquad, _titleStartingSquad, _descriptionStartingSquad, _optionsStartingSquad, (int)(TFTVNewGameOptions.startingSquadCharacters), OnStartingSquadValueChangedCallback, 1f);
                    InstantiateArrowPickerController(_startingScavSitesModSettings, _startingScavSites, _titleScavSites, _descriptionScavSites, _optionsScavSites, TFTVNewGameOptions.initialScavSites, OnStartingScavSitesValueChangedCallback, 1f);
                    InstantiateArrowPickerController(_resCratePriorityModSettings, _resCratePriority, _titleResCratePriority, _descriptionScavPriority, _optionsResCratePriority, (int)(TFTVNewGameOptions.chancesScavCrates), OnResScavPriorityValueChangedCallback, 1f);
                    InstantiateArrowPickerController(_recruitsPriorityModSettings, _recruitsPriority, _titleRecruitsPriority, _descriptionScavPriority, _optionsRecruitsPriority, (int)(TFTVNewGameOptions.chancesScavSoldiers), OnRecruitsPriorityValueChangedCallback, 1f);
                    InstantiateArrowPickerController(_vehiclePriorityModSettings, _vehiclePriority, _titleVehiclePriority, _descriptionScavPriority, _optionsVehiclePriority, (int)(TFTVNewGameOptions.chancesScavGroundVehicleRescue), OnVehiclePriorityValueChangedCallback, 1f);
                    InstantiateArrowPickerController(_tacticalDifficultyModSettings, _tacticalDifficulty, _titleTacticalDifficulty, _descriotionTacticalDifficulty, _optionsTacticalDifficulty, (int)config.difficultyOnTactical, OnTacticalDifficultyValueChangedCallback, 1f);
                    InstantiateArrowPickerController(_limitedCaptureModSettings, _limitedCapture, _titleLimitedCapture, _descriptionLimitedCapture, _optionsBool, ConvertBoolToInt(TFTVNewGameOptions.LimitedCaptureSetting), OnLimitedCaptureValueChangedCallback, 0.5f);
                    InstantiateArrowPickerController(_limitedHarvestingModSettings, _limitedHarvesting, _titleLimitedHarvesting, _descriptionLimitedHarvesting, _optionsBool, ConvertBoolToInt(TFTVNewGameOptions.LimitedHarvestingSetting), OnLimitedHarvestingValueChangedCallback, 0.5f);
                    InstantiateArrowPickerController(_tradingModSettings, _trading, _titleTrading, _descriptionTrading, _optionsBool, ConvertBoolToInt(config.EqualizeTrade), OnTradingValueChangedCallback, 0.5f);
                    InstantiateArrowPickerController(_limitedRaidingModSettings, _limitedRaiding, _titleLimitedRaiding, _descriptionLimitedRaiding, _optionsBool, ConvertBoolToInt(config.LimitedRaiding), OnLimitedRaidingValueChangedCallback, 0.5f);
                    InstantiateArrowPickerController(_noDropReinforcementsModSettings, _noDropReinforcements, _titleNoDropReinforcements, _descriptionNoDropReinforcements, _optionsBool, ConvertBoolToInt(config.ReinforcementsNoDrops), OnNoDropValueChangedCallback, 0.5f);

                    InstantiateArrowPickerController(_strongerPandoransModSettings, _strongerPandorans, _titleStrongerPandorans, _descriptionStrongerPandorans, _optionsBool, ConvertBoolToInt(TFTVNewGameOptions.StrongerPandoransSetting), OnStrongerPandoransValueChangedCallback, 0.5f);

                    InstantiateArrowPickerController(_staminaRecuperationModSettings, _staminaRecuperation, _titleStaminaRecuperation, _descriptionStaminaRecuperation, _optionsBool, ConvertBoolToInt(config.ActivateStaminaRecuperatonModule), OnStaminaRecuperationValueChangedCallback, 0.5f);
                    InstantiateArrowPickerController(_staminaDrainModSettings, _staminaDrain, _titleStaminaDrain, _descriptionStaminaDrain, _optionsBool, ConvertBoolToInt(TFTVNewGameOptions.StaminaPenaltyFromInjurySetting), OnStaminaDrainValueChangedCallback, 0.5f);
                    InstantiateArrowPickerController(_diploPenaltiesModSettings, _diploPenalties, _titleDiploPenalties, _descriptionDiploPenalties, _optionsBool, ConvertBoolToInt(TFTVNewGameOptions.DiplomaticPenaltiesSetting), OnDiploPenaltiesValueChangedCallback, 0.5f);

                    InstantiateArrowPickerController(_flinchingModSettings, _flinching, _titleFlinching, _descriptionFlinching, _optionsBool, ConvertBoolToInt(config.AnimateWhileShooting), OnFlinchingValueChangedCallback, 0.5f);
                    InstantiateArrowPickerController(_harderAmbushModSettings, _harderAmbush, _titleHarderAmbush, _descriptionHarderAmbush, _optionsBool, ConvertBoolToInt(TFTVNewGameOptions.MoreAmbushesSetting), OnHarderAmbushValueChangedCallback, 0.5f);
                    InstantiateArrowPickerController(_havenSOSModSettings, _havenSOS, _titleHavenSOS, _descriptionHavenSOS, _optionsBool, ConvertBoolToInt(config.HavenSOS), OnHavenSOSValueChangedCallback, 0.5f);
                    InstantiateArrowPickerController(_impossibleWeaponsModSettings, _impossibleWeapons, _titleImpossibleWeapons, _descriptionImpossibleWeapons, _optionsBool, ConvertBoolToInt(TFTVNewGameOptions.ImpossibleWeaponsAdjustmentsSetting), OnImpossibleWeaponsValueChangedCallback, 0.5f);
                    InstantiateArrowPickerController(_noSecondChancesModSettings, _noSecondChances, _titleNoSecondChances, _descriptionNoSecondChances, _optionsBool, ConvertBoolToInt(TFTVNewGameOptions.NoSecondChances), OnNoSecondChancesValueChangedCallback, 0.5f);
                    InstantiateArrowPickerController(_learnFirstSkillModSettings, _learnFirstSkill, _titleLearnFirstSkill, _descriptionLearnFirstSkill, _optionsBool, ConvertBoolToInt(config.LearnFirstPersonalSkill), OnLearnFirstSchoolValueChangedCallback, 0.5f);
                    InstantiateArrowPickerController(_moreMistVOModSettings, _moreMistVO, _titleMoreMistVO, _descriptionMoreMistVO, _optionsBool, ConvertBoolToInt(config.MoreMistVO), OnMoreMistValueChangedCallback, 0.5f);
                    InstantiateArrowPickerController(_limitedDeploymentVOModSettings, _limitedDeploymentVO, _titlelimitedDeploymentVO, _descriptionlimitedDeploymentVO, _optionsBool, ConvertBoolToInt(config.LimitedDeploymentVO), OnLimitedDeploymentValueChangedCallback, 0.5f);

                    //     InstantiateArrowPickerController(_reverseEngineeringModSettings, _reverseEngineering, _titleReverseEngineering, _descriptionReverseEngineering, _optionsBool, ConvertBoolToInt(config.ActivateReverseEngineeringResearch), OnReverseEngineeringValueChangedCallback, 0.5f);

                    InstantiateArrowPickerController(_disableTacSavesModSettings, _disableTacSaves, _titleDisableTacSaves, _descriptionDisableTacSaves, _optionsBool, ConvertBoolToInt(config.disableSavingOnTactical), OnDisableTacSavesValueChangedCallback, 0.5f);
                    InstantiateArrowPickerController(_skipMoviesModSettings, _skipMovies, _titleSkipMovies, _descriptionSkipMovies, _optionsBool, ConvertBoolToInt(config.SkipMovies), OnSkipMoviesValueChangedCallback, 0.5f);
                    InstantiateArrowPickerController(_noBarksModSettings, _noBarks, _titleNoBarks, _descriptionNoBarks, _optionsBool, ConvertBoolToInt(config.NoBarks), OnNoBarksValueChangedCallback, 0.5f);

                    InstantiateArrowPickerController(_exoticResourcesModSettings, _exoticResources, _titleExoticResources, _descriptionExoticResources, _amountPercentageResources, ConvertDifficultyToIndexExoticResources(), OnExoticResourcesValueChangedCallback, 0.5f);
                    InstantiateArrowPickerController(_resourcesEventsModSettings, _resourcesEvents, _titleResourcesEvents, _descriptionResourcesEvents, _amountPercentageResources, ConvertDifficultyToIndexEventsResources(), OnResourcesEventsValueChangedCallback, 0.5f);


                    SetAllVisibility(false);
                    UpdateOptionsOnSelectingDifficutly();

                    NewGameOptionsSetUp = true;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static int ConvertBoolToInt(bool value)
            {
                try
                {

                    if (value == true)
                    {

                        return 0;
                    }
                    return 1;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


            private static void OnExoticResourcesValueChangedCallback(int newValue)
            {
                try
                {
                    _exoticResources.CurrentItemText.text = _amountPercentageResources[newValue];
                    TFTVNewGameOptions.AmountOfExoticResourcesSetting = _amountMultiplierResources[newValue];
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void OnResourcesEventsValueChangedCallback(int newValue)
            {
                try
                {
                    _resourcesEvents.CurrentItemText.text = _amountPercentageResources[newValue];
                    TFTVNewGameOptions.ResourceMultiplierSetting = _amountMultiplierResources[newValue];
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void OnDisableTacSavesValueChangedCallback(int newValue)
            {
                try
                {
                    if (newValue == 1 && NewGameOptionsSetUp)
                    {
                        TFTVLogger.Always($"disable tactical saving warning called now");

                        string warning = TFTVCommonMethods.ConvertKeyToString("KEY_OPTIONS_TACTICAL_SAVING_WARNING");// $"Saving and loading on Tactical can result in odd behavior and bugs (Vanilla issues). It is recommended to save only on Geoscape (and use several saves, in case one of them gets corrupted). And, you know what... losing soldiers in TFTV is fun :)";

                        GameUtl.GetMessageBox().ShowSimplePrompt(warning, MessageBoxIcon.Warning, MessageBoxButtons.OK, null);
                    }

                    bool option = newValue == 0;


                    string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                    _disableTacSaves.CurrentItemText.text = options[newValue];
                    config.disableSavingOnTactical = option;
                    
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void OnDiploPenaltiesValueChangedCallback(int newValue)
            {
                try
                {
                    bool option = newValue == 0;
                    string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                    _diploPenalties.CurrentItemText.text = options[newValue];
                    TFTVNewGameOptions.DiplomaticPenaltiesSetting = option;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void OnStaminaDrainValueChangedCallback(int newValue)
            {
                try
                {
                    bool option = newValue == 0;
                    string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                    _staminaDrain.CurrentItemText.text = options[newValue];
                    TFTVNewGameOptions.StaminaPenaltyFromInjurySetting = option;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void OnHarderAmbushValueChangedCallback(int newValue)
            {
                try
                {
                    bool option = newValue == 0;
                    string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                    _harderAmbush.CurrentItemText.text = options[newValue];
                    TFTVNewGameOptions.MoreAmbushesSetting = option;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void OnStaminaRecuperationValueChangedCallback(int newValue)
            {
                try
                {
                    bool option = newValue == 0;
                    string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                    _staminaRecuperation.CurrentItemText.text = options[newValue];
                    config.ActivateStaminaRecuperatonModule = option;
                  
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void OnHavenSOSValueChangedCallback(int newValue)
            {
                try
                {
                    bool option = newValue == 0;
                    string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                    _havenSOS.CurrentItemText.text = options[newValue];
                    config.HavenSOS = option;
                   
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void OnLearnFirstSchoolValueChangedCallback(int newValue)
            {
                try
                {
                    bool option = newValue == 0;
                    string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                    _learnFirstSkill.CurrentItemText.text = options[newValue];
                    config.LearnFirstPersonalSkill = option;
                    
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void OnStrongerPandoransValueChangedCallback(int newValue)
            {
                try
                {
                    /*  if (TFTVDefsWithConfigDependency.StrongerPandorans.StrongerPandoransImplemented && newValue == 1 && NewGameOptionsSetUp)
                      {
                          string warning = $"{TFTVCommonMethods.ConvertKeyToString("KEY_OPTIONS_CHANGED_SETTING_WARNING0")} {_titleStrongerPandorans} {TFTVCommonMethods.ConvertKeyToString("KEY_OPTIONS_CHANGED_SETTING_WARNING1")}";
                          GameUtl.GetMessageBox().ShowSimplePrompt(warning, MessageBoxIcon.Warning, MessageBoxButtons.OK, null);
                      }*/

                    bool option = newValue == 0;
                    string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                    _strongerPandorans.CurrentItemText.text = options[newValue];
                    TFTVNewGameOptions.StrongerPandoransSetting = option;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void OnImpossibleWeaponsValueChangedCallback(int newValue)
            {
                try
                {
                    bool option = newValue == 0;
                    string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                    _impossibleWeapons.CurrentItemText.text = options[newValue];
                    TFTVNewGameOptions.ImpossibleWeaponsAdjustmentsSetting = option;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


            private static void OnNoSecondChancesValueChangedCallback(int newValue)
            {
                try
                {
                    bool option = newValue == 0;
                    string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                    _noSecondChances.CurrentItemText.text = options[newValue];
                    TFTVNewGameOptions.NoSecondChances = option;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


            private static void OnSkipMoviesValueChangedCallback(int newValue)
            {
                try
                {
                    bool option = newValue == 0;
                    string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                    _skipMovies.CurrentItemText.text = options[newValue];
                    config.SkipMovies = option;
                   
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void OnNoBarksValueChangedCallback(int newValue)
            {
                try
                {
                    bool option = newValue == 0;
                    string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                    _noBarks.CurrentItemText.text = options[newValue];
                    config.NoBarks = option;
                 
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }



            private static void OnFlinchingValueChangedCallback(int newValue)
            {
                try
                {
                    bool option = newValue == 0;
                    string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                    _flinching.CurrentItemText.text = options[newValue];
                    config.AnimateWhileShooting = option;
                  
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void OnMoreMistValueChangedCallback(int newValue)
            {
                try
                {
                    bool option = newValue == 0;
                    string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                    _moreMistVO.CurrentItemText.text = options[newValue];
                    config.MoreMistVO = option;
                   
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void OnLimitedDeploymentValueChangedCallback(int newValue)
            {
                try
                {
                    bool option = newValue == 0;
                    string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                    _limitedDeploymentVO.CurrentItemText.text = options[newValue];
                    config.LimitedDeploymentVO = option;
                   
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }



            private static void OnTradingValueChangedCallback(int newValue)
            {
                try
                {
                    bool option = newValue == 0;
                    string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                    _trading.CurrentItemText.text = options[newValue];
                    config.EqualizeTrade = option;
                  

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void OnLimitedRaidingValueChangedCallback(int newValue)
            {

                try
                {
                    bool option = newValue == 0;
                    string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                    _limitedRaiding.CurrentItemText.text = options[newValue];
                    config.LimitedRaiding = option;
                   
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void OnNoDropValueChangedCallback(int newValue)
            {

                try
                {
                    bool option = newValue == 0;
                    string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                    _noDropReinforcements.CurrentItemText.text = options[newValue];
                    config.ReinforcementsNoDrops = option;
                  
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void OnTacticalDifficultyValueChangedCallback(int newValue)
            {
                try
                {

                    string[] options = new string[_optionsTacticalDifficulty.Length];

                    for (int i = 0; i < _optionsTacticalDifficulty.Length; i++)
                    {
                        LocalizedTextBind optionTextBindKey = new LocalizedTextBind() { LocalizationKey = _optionsTacticalDifficulty[i] };
                        options[i] = optionTextBindKey.Localize();

                    }

                    _tacticalDifficulty.CurrentItemText.text = options[newValue];
                    config.difficultyOnTactical = (TFTVConfig.DifficultyOnTactical)newValue;
                    //TFTVLogger.Always($"new difficulty on tactical showing in config: {config.difficultyOnTactical}");

                    TFTVConfig.DifficultyOnTactical difficulty = (TFTVConfig.DifficultyOnTactical)newValue;
                  

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }


            }

            private static void OnStartingFactionValueChangedCallback(int newValue)
            {
                try
                {
                    string[] options = new string[_optionsStartingFaction.Length];

                    for (int i = 0; i < _optionsStartingFaction.Length; i++)
                    {
                        LocalizedTextBind optionTextBindKey = new LocalizedTextBind() { LocalizationKey = _optionsStartingFaction[i] };
                        options[i] = optionTextBindKey.Localize();

                    }


                    _startingFaction.CurrentItemText.text = options[newValue];
                    TFTVNewGameOptions.startingSquad = (TFTVNewGameOptions.StartingSquadFaction)newValue;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }


            }

            private static void OnStartingSquadValueChangedCallback(int newValue)
            {
                try
                {
                    string[] options = new string[_optionsStartingSquad.Length];

                    for (int i = 0; i < _optionsStartingSquad.Length; i++)
                    {
                        LocalizedTextBind optionTextBindKey = new LocalizedTextBind() { LocalizationKey = _optionsStartingSquad[i] };
                        options[i] = optionTextBindKey.Localize();

                    }

                    _startingSquad.CurrentItemText.text = options[newValue];
                    TFTVNewGameOptions.startingSquadCharacters = (TFTVNewGameOptions.StartingSquadCharacters)newValue;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void OnStartingBaseValueChangedCallback(int newValue)
            {
                try
                {
                    string[] options = new string[_optionsStartingBase.Length];

                    for (int i = 0; i < _optionsStartingBase.Length; i++)
                    {
                        LocalizedTextBind optionTextBindKey = new LocalizedTextBind() { LocalizationKey = _optionsStartingBase[i] };
                        options[i] = optionTextBindKey.Localize();

                    }


                    _startingBase.CurrentItemText.text = options[newValue];
                    TFTVNewGameOptions.startingBaseLocation = (TFTVNewGameOptions.StartingBaseLocation)newValue;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void OnStartingScavSitesValueChangedCallback(int newValue)
            {
                try
                {
                    _startingScavSites.CurrentItemText.text = _optionsScavSites[newValue];
                    TFTVNewGameOptions.initialScavSites = newValue;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void OnResScavPriorityValueChangedCallback(int newValue)
            {
                try
                {

                    string[] options = { new LocalizedTextBind() { LocalizationKey = "HIGH" }.Localize(), new LocalizedTextBind() { LocalizationKey = "MEDIUM" }.Localize(),
                    new LocalizedTextBind() { LocalizationKey = "LOW" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NONE" }.Localize() };
                    //  string[] options = { "HIGH", "MEDIUM", "LOW", "NONE" };
                    _resCratePriority.CurrentItemText.text = options[newValue];
                    TFTVNewGameOptions.chancesScavCrates = (TFTVNewGameOptions.ScavengingWeight)newValue;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void OnRecruitsPriorityValueChangedCallback(int newValue)
            {
                try
                {
                    string[] options = { new LocalizedTextBind() { LocalizationKey = "HIGH" }.Localize(), new LocalizedTextBind() { LocalizationKey = "MEDIUM" }.Localize(),
                    new LocalizedTextBind() { LocalizationKey = "LOW" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NONE" }.Localize() };
                    _recruitsPriority.CurrentItemText.text = options[newValue];
                    TFTVNewGameOptions.chancesScavSoldiers = (TFTVNewGameOptions.ScavengingWeight)newValue;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void OnVehiclePriorityValueChangedCallback(int newValue)
            {
                try
                {
                    string[] options = { new LocalizedTextBind() { LocalizationKey = "HIGH" }.Localize(), new LocalizedTextBind() { LocalizationKey = "MEDIUM" }.Localize(), new LocalizedTextBind() { LocalizationKey = "LOW" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NONE" }.Localize() };
                    _vehiclePriority.CurrentItemText.text = options[newValue];
                    TFTVNewGameOptions.chancesScavGroundVehicleRescue = (TFTVNewGameOptions.ScavengingWeight)newValue;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void OnLimitedCaptureValueChangedCallback(int newValue)
            {
                try
                {
                    /* if (TFTVDefsWithConfigDependency.ChangesToCapturingPandoransImplemented && newValue == 1 && NewGameOptionsSetUp)
                     {
                         string warning = $"{TFTVCommonMethods.ConvertKeyToString("KEY_OPTIONS_CHANGED_SETTING_WARNING0")} {_titleLimitedCapture} {TFTVCommonMethods.ConvertKeyToString("KEY_OPTIONS_CHANGED_SETTING_WARNING1")}";

                         GameUtl.GetMessageBox().ShowSimplePrompt(warning, MessageBoxIcon.Warning, MessageBoxButtons.OK, null);

                     }*/

                    bool limitedCapture = newValue == 0;
                    string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                    _limitedCapture.CurrentItemText.text = options[newValue];
                    TFTVNewGameOptions.LimitedCaptureSetting = limitedCapture;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void OnLimitedHarvestingValueChangedCallback(int newValue)
            {
                try
                {
                    /* if (TFTVDefsWithConfigDependency.ChangesToFoodAndMutagenGenerationImplemented && newValue == 1 && NewGameOptionsSetUp)
                     {
                         string warning = $"{TFTVCommonMethods.ConvertKeyToString("KEY_OPTIONS_CHANGED_SETTING_WARNING0")} {_titleLimitedHarvesting} {TFTVCommonMethods.ConvertKeyToString("KEY_OPTIONS_CHANGED_SETTING_WARNING1")}";

                         GameUtl.GetMessageBox().ShowSimplePrompt(warning, MessageBoxIcon.Warning, MessageBoxButtons.OK, null);

                     }*/


                    bool limitedHarvesting = newValue == 0;
                    string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                    _limitedHarvesting.CurrentItemText.text = options[newValue];
                    TFTVNewGameOptions.LimitedHarvestingSetting = limitedHarvesting;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void PopulateOptions(ArrowPickerController arrowPickerController, string[] options)
            {
                try
                {
                    for (int i = 0; i < options.Length; i++)
                    {

                        arrowPickerController.CurrentItemText.text = options[i];

                        MethodInfo OnNewValue = arrowPickerController.GetType().GetMethod("OnNewValue", BindingFlags.NonPublic | BindingFlags.Instance);

                        OnNewValue.Invoke(arrowPickerController, null);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }

        public static bool EnterStateRun = false;




    /*    [HarmonyPatch(typeof(UIStateHomeLoadGame), "EnterState")]
        public static class UIStateHomeLoadGame_EnterState_Patch
        {
            private static void Postfix(UIStateHomeLoadGame __instance)
            {
                try
                {
                    TFTVLogger.Always($"UIStateHomeLoadGame.EnterState PostFix running");

                    TFTVLogger.Always($"Config settings:" +
                    $"\nAmountOfExoticResourcesSetting: {TFTVNewGameOptions.AmountOfExoticResourcesSetting}\nResourceMultiplierSetting: {TFTVNewGameOptions.ResourceMultiplierSetting}" +
                    $"\nDiplomaticPenaltiesSetting: {TFTVNewGameOptions.DiplomaticPenaltiesSetting}\nStaminaPenaltyFromInjurySetting: {TFTVNewGameOptions.StaminaPenaltyFromInjurySetting}" +
                    $"\nMoreAmbushesSetting: {TFTVNewGameOptions.MoreAmbushesSetting}\nLimitedCaptureSetting: {TFTVNewGameOptions.LimitedCaptureSetting}\nLimitedHarvestingSetting: {TFTVNewGameOptions.LimitedHarvestingSetting}" +
                    $"\nStrongerPandoransSetting {TFTVNewGameOptions.StrongerPandoransSetting}\nImpossibleWeaponsAdjustmentsSetting: {TFTVNewGameOptions.ImpossibleWeaponsAdjustmentsSetting}" +
                    $"\nNoSecondChances: {TFTVNewGameOptions.NoSecondChances}");

                    TFTVDefsWithConfigDependency.ImplementConfigChoices();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }*/

        [HarmonyPatch(typeof(UIStateNewGeoscapeGameSettings), "EnterState")]
        public static class UIStateNewGeoscapeGameSettings_EnterState_Patch
        {
            private static void Prefix(UIStateNewGeoscapeGameSettings __instance)
            {
                try
                {
                    GameDifficultyLevelDef[] difficultyLevels = GameUtl.GameComponent<SharedData>().DifficultyLevels;
                    HomeScreenView homescreenview = GameUtl.CurrentLevel().GetComponent<HomeScreenView>();
                    UIModuleGameSettings gameSettings = homescreenview.HomeScreenModules.GameSettings;
                    if (!EnterStateRun)
                    {



                        //  HomeScreenViewContext context = HomeScreenViewContextHook;

                        //context.View.HomeScreenModules.GameSettings;

                        Transform container = gameSettings.MainOptions.Container;

                        Transform transformForCloning = UnityEngine.Object.Instantiate(container.GetComponentsInChildren<GameOptionViewController>().First().transform);

                        container.DetachChildren();

                        for (int x = 0; x < difficultyLevels.Length; x++)
                        {
                            Transform newController = UnityEngine.Object.Instantiate(transformForCloning);
                            newController.name = "TFTVDifficulty_RadioButton" + x;
                            newController.SetParent(container, false);
                        }


                        gameSettings.MainOptions.Container.gameObject.transform.localScale = new Vector3(0.90f, 0.90f, 0.90f);
                        gameSettings.MainOptions.Container.gameObject.transform.Translate(new Vector3(0f, 20f, 0f));

                        // TFTVLogger.Always($"There are {difficultyLevels.Length} difficulty level. Number of elements is {gameSettings.MainOptions.Elements.Count}");
                        //    TFTVLogger.Always($"Enter state invoked");
                        EnterStateRun = true;
                    }

                    OptionsManager optionsManager = GameUtl.GameComponent<OptionsManager>();
                    OptionsComponent optionsComponent = GameUtl.GameComponent<OptionsComponent>();

                    OptionsManagerDef optionsManagerDef = optionsManager.OptionsManagerDef;
                    int defaultVal = Math.Min(2, difficultyLevels.Length) - 1;
                    defaultVal = optionsComponent.Options.Get(optionsManagerDef.NewGameDifficultyOption, defaultVal);

                    //   TFTVLogger.Always($"default value is {defaultVal}");


                    GameOptionViewController[] componentsInChildren = gameSettings.MainOptions.Container.GetComponentsInChildren<GameOptionViewController>();

                    foreach (GameOptionViewController component in componentsInChildren)
                    {
                        if (component.name.EndsWith((defaultVal).ToString()))
                        {

                            component.SelectButton.IsSelected = true;
                            SelectedDifficulty = defaultVal + 1;
                        }
                        else
                        {
                            //  TFTVLogger.Always($"{component.name}  {component.IsSelected} {component.CheckedToggle}");
                            component.SelectButton.IsSelected = false;

                        }
                    }



                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIStateNewGeoscapeGameSettings), "BindSecondaryOptions")]
        internal static class UIStateNewGeoscapeGameSettings_BindSecondaryOptions_patch
        {
            private static void Postfix(UIStateNewGeoscapeGameSettings __instance, GameOptionViewController element)
            {
                try
                {
                    //  TFTVLogger.Always($"Element is {element.OptionText.text}");
                    //   GameOptionViewController viewController = new GameOptionViewController() { };

                    if (element.OptionText.text == "PLAY PROLOGUE AND TUTORIAL")
                    {
                        //  element.OptionText.text = "START WITH VANILLA TUTORIAL SQUAD";                      
                    }
                    else
                    {

                        element.CheckedToggle.isOn = false;

                    }

                    /* if (!BindSecondaryOptionRun)
                     {
                         Transform transformForCloning = UnityEngine.Object.Instantiate(element.gameObject.transform);
                         GameOptionViewController gameOptionViewController = transformForCloning.GetComponent<GameOptionViewController>();
                         gameOptionViewController.Set(new Base.UI.LocalizedTextBind(""), new Base.UI.LocalizedTextBind("), false);
                         BindSecondaryOptionRun = true;
                     }*/
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }

        [HarmonyPatch(typeof(UIModuleGameSettings), "GetActivatedEntitlements")]
        public static class UIModuleGameSettings_GetActivatedEntitlements_Experiment_Patch
        {
            public static bool Prefix(UIModuleGameSettings __instance, ref List<EntitlementDef> __result, List<GameAdditionalContentEntry> ____gameEntitlementContentEntries)
            {
                try
                {

                    // TFTVLogger.Always("Patch running");

                    DefCache.GetDef<EntitlementDef>("BloodAndTitaniumEntitlementDef");
                    DefCache.GetDef<EntitlementDef>("CorruptedHorizonsEntitlementDef");
                    DefCache.GetDef<EntitlementDef>("FesteringSkiesEntitlementDef");
                    DefCache.GetDef<EntitlementDef>("KaosEnginesEntitlementDef");
                    DefCache.GetDef<EntitlementDef>("LegacyOfTheAncientsEntitlementDef");
                    DefCache.GetDef<EntitlementDef>("LivingWeaponsEntitlementDef");
                    //  DefCache.GetDef<EntitlementDef>("YOE_YearOneEditionEntitlementDef");


                    __result = new List<EntitlementDef>() {
                        DefCache.GetDef<EntitlementDef>("BloodAndTitaniumEntitlementDef"), DefCache.GetDef<EntitlementDef>("CorruptedHorizonsEntitlementDef"), DefCache.GetDef<EntitlementDef>("FesteringSkiesEntitlementDef"),
                   DefCache.GetDef<EntitlementDef>("KaosEnginesEntitlementDef"), DefCache.GetDef<EntitlementDef>("LegacyOfTheAncientsEntitlementDef"), DefCache.GetDef<EntitlementDef>("LivingWeaponsEntitlementDef")};


                    TFTVCommonMethods.ClearInternalVariables();
                    TFTVNewGameOptions.ConfigImplemented = true;
                   // TFTVNewGameOptions.Update35Check = true;
                    NewGameOptionsSetUp = false;

                    return false;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIModuleGameSettings), "GetDeactivatedEntitlements")]
        public static class UIModuleGameSettings_GetDeactivatedEntitlements_Experiment_Patch
        {
            public static bool Prefix(UIModuleGameSettings __instance, ref List<EntitlementDef> __result, List<GameAdditionalContentEntry> ____gameEntitlementContentEntries)
            {
                try
                {

                    //  TFTVLogger.Always("Patch GetDeactivatedEntitlements running");
                    return false;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIStateNewGeoscapeGameSettings), "GameSettings_OnConfirm")]
        public static class UIStateNewGeoscapeGameSettings_GameSettings_OnConfirm_patch
        {
            public static void Prefix(UIStateNewGeoscapeGameSettings __instance)
            {
                try
                {
                    TFTVStarts.RevertIntroToNormalStart();
                    TFTVLogger.Always($"selected option: {GameUtl.CurrentLevel().GetComponent<HomeScreenView>().HomeScreenModules.GameSettings?.MainOptions?.Selected?.First()?.OptionIndex}");
                    EnterStateRun = false;


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(UIStateNewGeoscapeGameSettings), "ExitState")]
        public static class UIStateNewGeoscapeGameSettings_ExitState_patch
        {
            public static void Postfix(UIStateNewGeoscapeGameSettings __instance)
            {
                try
                {
                    ModManager modManager = TFTVMain.Main.GetGame().ModManager;

                    modManager.OnConfigChanged(TFTVMain.Main.Instance.Entry);
                    modManager.SaveModConfig();
                    TFTVLogger.Always($"Exiting new game, saving config");
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

    }
}

