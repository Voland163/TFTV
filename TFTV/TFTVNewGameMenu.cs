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
using PhoenixPoint.Home.View;
using PhoenixPoint.Home.View.ViewControllers;
using PhoenixPoint.Home.View.ViewModules;
using PhoenixPoint.Home.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TFTV
{
    internal class TFTVNewGameMenu
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly TFTVConfig config = TFTVMain.Main.Config;
        private static readonly SharedData Shared = TFTVMain.Shared;
        //You will need to edit scene hierarchy to add new objects under GameSettingsModule, it has a UIModuleGameSettings script
        //Class UIStateNewGeoscapeGameSettings is responsible for accepting selected settings and start the game, so you'll have to dig inside
        //for changing behaviour.


        private static bool NewGameOptionsSetUp = false;

        private static int SelectedDifficulty = 0;

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

                if (SelectedDifficulty > 4)
                {
                    diploPenalty = 0;
                    staminaDrain = 0;
                    harderAmnbush = 0;
                    strongerPandorans = 0;
                    impossibleWeapons = 0;
                    limitedCapture = 0;
                    limitedHarvesting = 0;
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


                MethodInfo diploPenaltiesChangedCallback = typeof(UIStateNewGeoscapeGameSettings_InitFullContent_patch).GetMethod("OnDiploPenaltiesValueChangedCallback", BindingFlags.NonPublic | BindingFlags.Static);

                diploPenaltiesChangedCallback.Invoke(gameSettings, new object[] { diploPenalty });

                MethodInfo staminaDrainValueChangedCallback = typeof(UIStateNewGeoscapeGameSettings_InitFullContent_patch).GetMethod("OnStaminaDrainValueChangedCallback", BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo harderAmbushValueChangedCallback = typeof(UIStateNewGeoscapeGameSettings_InitFullContent_patch).GetMethod("OnHarderAmbushValueChangedCallback", BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo strongerPandoransValueChangedCallback = typeof(UIStateNewGeoscapeGameSettings_InitFullContent_patch).GetMethod("OnStrongerPandoransValueChangedCallback", BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo impossibleWeaponsValueChangedCallback = typeof(UIStateNewGeoscapeGameSettings_InitFullContent_patch).GetMethod("OnImpossibleWeaponsValueChangedCallback", BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo limitedHarvestingValueChangedCallback = typeof(UIStateNewGeoscapeGameSettings_InitFullContent_patch).GetMethod("OnLimitedHarvestingValueChangedCallback", BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo limitedCaptureValueChangedCallback = typeof(UIStateNewGeoscapeGameSettings_InitFullContent_patch).GetMethod("OnLimitedCaptureValueChangedCallback", BindingFlags.NonPublic | BindingFlags.Static);

                staminaDrainValueChangedCallback.Invoke(gameSettings, new object[] { staminaDrain });
                harderAmbushValueChangedCallback.Invoke(gameSettings, new object[] { harderAmnbush });
                strongerPandoransValueChangedCallback.Invoke(gameSettings, new object[] { strongerPandorans });
                impossibleWeaponsValueChangedCallback.Invoke(gameSettings, new object[] { impossibleWeapons });
                limitedHarvestingValueChangedCallback.Invoke(gameSettings, new object[] { limitedHarvesting });
                limitedCaptureValueChangedCallback.Invoke(gameSettings, new object[] { limitedCapture });

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



        //This is for new game start screen. Commented out for the moment.
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
                        HomeScreenViewContext context = HomeScreenViewContextHook;
                        UIModuleGameSettings gameSettings = context.View.HomeScreenModules.GameSettings;

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

        public static HomeScreenViewContext HomeScreenViewContextHook = null;
        public static ModSettingController ModSettingControllerHook = null;

        [HarmonyPatch(typeof(HomeScreenView), "InitView")]
        public static class HomeScreenView_InitView_Patch
        {
            private static void Postfix(HomeScreenView __instance, HomeScreenViewContext ____context)
            {
                try
                {
                    HomeScreenViewContextHook = ____context;
                    ModSettingControllerHook = __instance.HomeScreenModules.ModManagerModule.SettingsModSettingPrefab;



                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        public static UIModuleGameSettings gameSettings = null;

        [HarmonyPatch(typeof(UIModuleGameSettings), "Awake")]
        public static class UIModuleGameSettings_Awake_Patch
        {
            private static void Postfix(UIModuleGameSettings __instance)
            {
                try
                {

                    gameSettings = __instance;


                    //TFTVLogger.Always("Awake run");

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

            private static string _titleAdditionalStartOptions = "TFTV_ADDITIONAL_START_OPTIONS_TITLE";
            private static string _descriptionAdditionalStartOptions = "TFTV_ADDITIONAL_START_DESCRIPTION";

            private static GameOptionViewController _anytimeOptionsVisibilityController = null;

            private static string _titleAnytimeOptions = "TFTV_ANYTIME_OPTIONS_TITLE";
            private static string _descriptionAnytimeOptions = "TFTV_ANYTIME_OPTIONS_DESCRIPTION";


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

            private static string _titleTrading = "NO PROFIT FROM TRADING";
            private static string _descriptionTrading = "Trade is always 1 tech for 5 food or 5 materials, so no profit can be made from trading.";

            private static ModSettingController _limitedRaidingModSettings = null;
            private static ArrowPickerController _limitedRaiding = null;

            private static string _titleLimitedRaiding = "LIMITED RAIDING";
            private static string _descriptionLimitedRaiding = "After a raid, all faction havens are immediately set to highest alert and may not be raided in the next 7 days.";


            private static ModSettingController _noDropReinforcementsModSettings = null;
            private static ArrowPickerController _noDropReinforcements = null;

            private static string _titleNoDropReinforcements = "NO ITEM DROPS FROM REINFORCEMENTS";
            private static string _descriptionNoDropReinforcements = "Enemy reinforcements do not drop items on death; disallows farming for weapons on missions with infinite reinforcements.";

            private static ModSettingController _flinchingModSettings = null;
            private static ArrowPickerController _flinching = null;

            private static string _titleFlinching = "FLINCHING";
            private static string _descriptionFlinching = "The characters will continue to animate during shooting sequences and targets that are hit may flinch, causing subsequent shots in a burst to miss when shooting in freeaim mode.";

            private static ModSettingController _strongerPandoransModSettings = null;
            private static ArrowPickerController _strongerPandorans = null;

            private static string _titleStrongerPandorans = "MAKE PANDORANS STRONGER";
            private static string _descriptionStrongerPandorans = "Applies the changes from Dtony BetterEnemies that make Pandorans more of a challenge.";

            private static ModSettingController _moreMistVOModSettings = null;
            private static ArrowPickerController _moreMistVO = null;

            private static string _titleMoreMistVO = "PLAY WITH MORE MIST VOID OMEN";
            private static string _descriptionMoreMistVO = "If you are playing on a Low-end system and experience lag with this Void Omen, you can turn it off here. This will prevent it from rolling.";

            private static ModSettingController _skipMoviesModSettings = null;
            private static ArrowPickerController _skipMovies = null;

            private static string _titleSkipMovies = "SKIP MOVIES";
            private static string _descriptionSkipMovies = "Choose whether to skip Logos on game launch, Intro and Landing cinematics. Adapted from Mad's Assorted Adjustments.";

            private static ModSettingController _exoticResourcesModSettings = null;
            private static ArrowPickerController _exoticResources = null;

            private static string _titleExoticResources = "AMOUNT OF EXOTIC RESOURCES";
            private static string _descriptionExoticResources = "Choose the amount of Exotic Resources you want to have in your game per playthrough. Each unit provides enough resources to manufacture one set of Impossible Weapons. So, if you want to have two full sets, set this number to 2, and so on. By default, this is set by the difficulty level: 2.5 on Rookie, 2 on Veteran, 1.5 on Hero, 1 on Legend.";
            private static string[] _amountPercentageResources = { "25%", "50%", "75%", "100%", "125%", "150%", "175%", "200%", "250%", "300%", "400%" };
            private static float[] _amountMultiplierResources = { 0.25f, 0.5f, 0.75f, 1f, 1.25f, 1.5f, 1.75f, 2f, 2.5f, 3f, 4f };

            //   private static float[] _amountMultiplier = {0.2f, 0.4f, 0.6f, 0.8f, 1f, 1.2f, 1.4f, 1.6f, 1.8f, 2f, 2.2f, 2.4f, 2.6f, 2.8f, 3f, 3.2f, 3.4f, 3.6f, 3.8f, 4f, 4.2f, 4.4f, 4.6f, 4.8f, 5, 5.2f, 5.4f, 5.6f, 5.8f, 6};
            //   private static string[] _amountMultiplierString = { "20%", "40%", "60%", 0.8f, 1f, 1.2f, 1.4f, 1.6f, 1.8f, 2f, 2.2f, 2.4f, 2.6f, 2.8f, 3f, 3.2f, 3.4f, 3.6f, 3.8f, 4f, 4.2f, 4.4f, 4.6f, 4.8f, 5, 5.2f, 5.4f, 5.6f, 5.8f, 6 };
            private static ModSettingController _resourcesEventsModSettings = null;
            private static ArrowPickerController _resourcesEvents = null;

            private static string _titleResourcesEvents = "SCALE RESOURCE ACQUISITION";
            private static string _descriptionResourcesEvents = "TFTV adjusts the amount of resources gained in Missions and Events by difficulty level. Please be aware that, for Events, TFTV 100% refers to Vanilla Pre-Azazoth patch levels (so it's actually 80% of current Vanilla amount).";

            private static ModSettingController _impossibleWeaponsModSettings = null;
            private static ArrowPickerController _impossibleWeapons = null;

            private static string _titleImpossibleWeapons = "IMPOSSIBLE WEAPONS ADJUSTMENTS";
            private static string _descriptionImpossibleWeapons = "In TFTV, Ancient Weapons are replaced by the Impossible Weapons (IW) " +
               "counterparts. They have different functionality (read: they are nerfed) " +
               "and some of them require additional faction research.  " +
               "Check this option off to keep Impossible Weapons with the same stats and functionality as Ancient Weapons in Vanilla and without requiring additional faction research. Set to false by default on Rookie.";

            private static ModSettingController _diploPenaltiesModSettings = null;
            private static ArrowPickerController _diploPenalties = null;

            private static string _titleDiploPenalties = "HIGHER DIPLOMATIC PENALTIES";
            private static string _descriptionDiploPenalties = "Diplomatic penalties from choices in events are doubled and revealing diplomatic missions for one faction gives a diplomatic penalty with the other factions. Can be applied to a game in progress. Set to false on Rookie by default";

            private static ModSettingController _staminaDrainModSettings = null;
            private static ArrowPickerController _staminaDrain = null;

            private static string _titleStaminaDrain = "STAMINA DRAIN ON INJURY/AUGMENTATION";
            private static string _descriptionStaminaDrain = "The stamina of any operative that sustains an injury in combat that results in a disabled body part will be set to zero after the mission. The stamina of any operative that undergoes a mutation or bionic augmentation will also be set to zero.";

            private static ModSettingController _harderAmbushModSettings = null;
            private static ArrowPickerController _harderAmbush = null;

            private static string _titleHarderAmbush = "HARDER AMBUSHES";
            private static string _descriptionHarderAmbush = "Ambushes will happen more often and will be harder. Regardless of this setting, all ambushes will have crates in them. Set to false on Rookie by default";

            private static ModSettingController _staminaRecuperationModSettings = null;
            private static ArrowPickerController _staminaRecuperation = null;

            private static string _titleStaminaRecuperation = "STAMINA RECUPERATION FAR-M";
            private static string _descriptionStaminaRecuperation = "The starting type of passenger module, FAR-M, will slowly recuperate the stamina of the operatives on board. Switch off if you prefer to have to return to base more often.";

            private static ModSettingController _disableTacSavesModSettings = null;
            private static ArrowPickerController _disableTacSaves = null;

            private static string _titleDisableTacSaves = "DISABLE SAVING ON TACTICAL";
            private static string _descriptionDisableTacSaves = "You can still restart the mission though.";


            /*      private static ModSettingController _reverseEngineeringModSettings = null;
                  private static ArrowPickerController _reverseEngineering = null;

                  private static string _titleReverseEngineering = "ENHANCED REVERSE ENGINEERING";
                  private static string _descriptionReverseEngineering = "Reversing engineering an item allows to research the faction technology that allows manufacturing the item.";*/

            private static ModSettingController _havenSOSModSettings = null;
            private static ArrowPickerController _havenSOS = null;

            private static string _titleHavenSOS = "HAVENS SEND SOS";
            private static string _descriptionHavenSOS = "Havens under attack will send an SOS, revealing their location to the player.";


            private static ModSettingController _learnFirstSkillModSettings = null;
            private static ArrowPickerController _learnFirstSkill = null;

            private static string _titleLearnFirstSkill = "LEARN FIRST BACKGROUND PERK";
            private static string _descriptionLearnFirstSkill = "If enabled, the first personal skill (level 1) is set right after a character is created (starting soldiers, new recruits in havens, rewards, etc).";


            private static string _titleTacticalDifficulty = "DIFFICULTY ON TACTICAL";
            private static string _descriotionTacticalDifficulty = "You can choose a different difficulty setting for the tactical portion of the game at any time.";
            private static string[] _optionsTacticalDifficulty = { "NO CHANGE", "STORY MODE", "ROOKIE", "VETERAN", "HERO", "LEGEND", "ETERMES" };


            private static string _titleResCratePriority = "RESOURCE CRATE PRIORITY";
            private static string _titleRecruitsPriority = "RECRUITS PRIORITY";
            private static string _titleVehiclePriority = "VEHICLE PRIORITY";
            private static string _descriptionScavPriority = "In Vanilla and default TFTV, resource crate scavenging sites are much more likely to spawn than either vehicle or personnel rescues. " +
                "You can modify the relative chances of each type of scavenging site being generated. Choose none to have 0 scavenging sites of this type (for reference, high/medium/low ratio is 6/4/1)";
            private static string[] _optionsResCratePriority = { "HIGH", "MEDIUM", "LOW", "NONE" };
            private static string[] _optionsRecruitsPriority = { "HIGH", "MEDIUM", "LOW", "NONE" };
            private static string[] _optionsVehiclePriority = { "HIGH", "MEDIUM", "LOW", "NONE" };

            private static string _titleScavSites = "SCAVENGING SITES #";
            private static string _titleLimitedCapture = "LIMITED CAPTURING";
            private static string _titleLimitedHarvesting = "LIMITED HARVESTING";

            private static string _descriptionLimitedCapture = "Play with game mechanics that set a limit to how many Pandorans you can capture per mission.";
            private static string _descriptionLimitedHarvesting = "Play with game mechanics that make obtaining food or mutagens from captured Pandorans harder.";

            private static string[] _optionsBool = { "YES", "NO" };


            private static string _descriptionScavSites = "Total number of scavenging sites generated on game start, not counting overgrown sites. (Vanilla: 16, TFTV default 8, because Ambushes generate additional resources).";
            private static string[] _optionsScavSites = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23", "24", "25", "26", "27", "28", "29", "30", "31", "32" };


            private static string _titleStartingFaction = "FACTION BACKGROUND";
            private static string _descriptionStartingFaction = "You can choose a different faction background. " +
                        "If you do, one of your Assaults and your starting Heavy on Legend and Hero, " +
                        "Assault on Veteran, or Sniper on Rookie will be replaced by an operative of the elite class of the Faction of your choice. " +
                        "You will also get the corresponding faction technology once the faction researches it.";
            private static string[] _optionsStartingFaction = { "PHOENIX PROJECT", "DISCIPLES OF ANU", "NEW JERICHO", "SYNEDRION" };

            private static string _titleStartingBase = "STARTING BASE LOCATION";
            private static string _descriptionStartingBase = "Select your starting base. You can choose a specific location to start from. Please note that some locations are harder to start from than others!";
            private static string[] _optionsStartingBase = {
                   "Vanilla Random",
                     "Random (ALL bases included)",
                     "Antarctica",
                     "Asia (China)",
                     "Australia",
                     "Central America (Honduras)",
                     "East Africa (Ethiopia)",
                     "Eastern Europe (Ukraine)",
                     "Greenland",
                     "Middle East (Afghanistan)",
                     "North Africa (Algeria)",
                     "North America (Alaska)",
                     "North America (Quebec)",
                     "Northern Asia (Siberia)",
                     "South Africa (Zimbabwe)",
                     "South America (Bolivia)",
                     "South America (Tierra de Fuego)",
                     "Southeast Asia (Cambodia)",
                     "West Africa (Ghana)"
               };

            private static string _titleStartingSquad = "STARTING SQUAD";
            private static string _descriptionStartingSquad = "You can choose to get a squad with random identities (as in Vanilla without doing the tutorial), " +
              "the Vanilla tutorial starting squad (with higher stats), " +
              "or a squad that will include Sophia Brown and Jacob with unbuffed stats (default on TFTV). " +
              "Note that Jacob is a sniper, as in the title screen :)";
            private static string[] _optionsStartingSquad = { "UNBUFFED", "BUFFED", "RANDOM" };



            private static GameOptionViewController InstantiateGameOptionViewController(RectTransform rectTransform, UIModuleGameSettings uIModuleGameSettings, string titleKey, string descriptionKey, string onToggleMethod)
            {
                try
                {
                    Resolution resolution = Screen.currentResolution;
                    float resolutionFactorWidth = (float)resolution.width / 1920f;
                    float resolutionFactorHeight = (float)resolution.height / 1080f;

                    GameOptionViewController gameOptionViewController = UnityEngine.Object.Instantiate(uIModuleGameSettings.SecondaryOptions.Container.GetComponentsInChildren<GameOptionViewController>().First(), rectTransform);

                    LocalizedTextBind description = new LocalizedTextBind();
                    description.LocalizationKey = descriptionKey; // Replace with the actual localization key

                    LocalizedTextBind text = new LocalizedTextBind();
                    text.LocalizationKey = titleKey; // Replace with the actual localization key

                    gameOptionViewController.Set(text, null);
                    //   gameOptionViewController.PointerEnter += OnPointerEnterCallback;
                    //   gameOptionViewController.PointerExit += OnPointerExitCallback;


                    MethodInfo method = typeof(UIStateNewGeoscapeGameSettings_InitFullContent_patch).GetMethod(onToggleMethod, BindingFlags.NonPublic | BindingFlags.Static);
                    Action<bool> setNewSettingsVisibility = (Action<bool>)Delegate.CreateDelegate(typeof(Action<bool>), method);
                    gameOptionViewController.CheckedToggle.onValueChanged.AddListener((value) =>
                    {
                        setNewSettingsVisibility.Invoke(value);
                    });

                    // TFTVLogger.Always($"position {gameOptionViewController.transform.position} local pos {gameOptionViewController.transform.localPosition} button {gameOptionViewController.SelectButton.transform.position}");

                    gameOptionViewController.SelectButton.transform.position -= new Vector3(250 * resolutionFactorWidth, 0, 0);
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
                    _havenSOSModSettings.gameObject.SetActive(show);
                    _staminaRecuperationModSettings.gameObject.SetActive(show);
                    _learnFirstSkillModSettings.gameObject.SetActive(show);
                    _moreMistVOModSettings.gameObject.SetActive(show);
                    //    _staminaDrainModSettings.gameObject.SetActive(show);

                    _skipMovies.gameObject.SetActive(show);
                    _havenSOS.gameObject.SetActive(show);

                    _learnFirstSkill.gameObject.SetActive(show);
                    _moreMistVO.gameObject.SetActive(show);
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



            /*   private static void SetMinorOptionsVisibility(bool show)
               {
                   try
                   {
                       _skipMoviesModSettings.gameObject.SetActive(show); 
                       _havenSOSModSettings.gameObject.SetActive(show);
                       _staminaRecuperationModSettings.gameObject.SetActive(show); 
                       _learnFirstSkillModSettings.gameObject.SetActive(show); 
                       _moreMistVOModSettings.gameObject.SetActive(show); 
                   //    _staminaDrainModSettings.gameObject.SetActive(show);

                       _skipMovies.gameObject.SetActive(show);
                       _havenSOS.gameObject.SetActive(show);

                       _learnFirstSkill.gameObject.SetActive(show);
                       _moreMistVO.gameObject.SetActive(show);
                       _noDropReinforcementsModSettings.gameObject.SetActive(show);
                       _flinchingModSettings.gameObject.SetActive(show);



                       //    _reverseEngineeringModSettings.gameObject.SetActive(show); 



                       _trading.gameObject.SetActive(show);
                       _limitedRaiding.gameObject.SetActive(show);
                       //  _staminaDrain.gameObject.SetActive(show);

                   }
                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                       throw;
                   }

               }*/

            /*   private static void SetTacticalOptionsVisibility(bool show)
               {
                   try
                   {
                       _noDropReinforcementsModSettings.gameObject.SetActive(show);
                       _flinchingModSettings.gameObject.SetActive(show);

                       _noDropReinforcements.gameObject.SetActive(show);
                       _flinching.gameObject.SetActive(show);


                   }
                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                       throw;
                   }

               }

               private static void SetGeoscapeOptionsVisibility(bool show)
               {
                   try
                   {

                       _tradingModSettings.gameObject.SetActive(show);
                       _limitedRaidingModSettings.gameObject.SetActive(show);

                   //    _reverseEngineeringModSettings.gameObject.SetActive(show); 



                       _trading.gameObject.SetActive(show);
                       _limitedRaiding.gameObject.SetActive(show);


                     //  _reverseEngineering.gameObject.SetActive(show);


                   }
                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                       throw;
                   }

               }*/


            private static void OnPointerEnterCallback(GameOptionViewController gameOptionViewController)
            {
                // Handle pointer enter event if needed
            }

            private static void OnPointerExitCallback(GameOptionViewController gameOptionViewController)
            {
                // Handle pointer exit event if needed
            }





            private static void InstantiateArrowPickerController(ModSettingController modSettingController,
                ArrowPickerController arrowPickerController, string title, string description, string[] options, int currentValue, Action<int> onValueChanged, float lengthScale)
            {
                try
                {
                  

                    Resolution resolution = Screen.currentResolution;
                    float resolutionFactorWidth = (float)resolution.width / 1920f;
                    float resolutionFactorHeight = (float)resolution.height / 1080f;

                    modSettingController.Label.text = title;
                    modSettingController.transform.localScale *= 0.75f;
                    arrowPickerController.transform.position += new Vector3(270 * resolutionFactorWidth, 0, 0);

                    if (lengthScale != 1)
                    {

                        arrowPickerController.transform.position += new Vector3(150 * resolutionFactorWidth * lengthScale, 0, 0);

                    }

                    modSettingController.Label.rectTransform.Translate(new Vector3(-270 * resolutionFactorWidth, 0, 0), arrowPickerController.transform);
                    modSettingController.Label.alignment = TextAnchor.MiddleLeft;
                    UnityEngine.Object.Destroy(modSettingController.GetComponentInChildren<UITooltipText>());

                    UITooltipText uITooltipText = modSettingController.Label.gameObject.AddComponent<UITooltipText>();

                    uITooltipText.TipText = description;

                    arrowPickerController.Init(options.Length, currentValue, onValueChanged);


                    arrowPickerController.CurrentItemText.text = options[currentValue];
                  //  if (lengthScale != 1)
                  //  {
                        arrowPickerController.GetComponent<RectTransform>().sizeDelta = new Vector2(arrowPickerController.GetComponent<RectTransform>().sizeDelta.x * lengthScale * resolutionFactorWidth, arrowPickerController.GetComponent<RectTransform>().sizeDelta.y);
                  //  }

                    PopulateOptions(arrowPickerController, options);
                    TFTVLogger.Always($"instantiating {title}, got to the end");
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void InstantiateArrowPickerControllerForAmount(ModSettingController modSettingController,
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
            }

            private static void Postfix(UIModuleGameSettings __instance)
            {
                try
                {

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
                    _limitedRaidingModSettings = UnityEngine.Object.Instantiate(ModSettingControllerHook, rectTransform);


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
                    _learnFirstSkill = _learnFirstSkillModSettings.ListField;
                    _moreMistVO = _moreMistVOModSettings.ListField;
                    //   _reverseEngineering = _reverseEngineeringModSettings.ListField;
                    _skipMovies = _skipMoviesModSettings.ListField;
                    _resourcesEvents = _resourcesEventsModSettings.ListField;
                    // _reverseEngineering = _reverseEngineeringModSettings.ListField;
                    _disableTacSaves = _disableTacSavesModSettings.ListField;
                    _staminaDrain = _staminaDrainModSettings.ListField;
                    _staminaRecuperation = _staminaRecuperationModSettings.ListField;
                    _strongerPandorans = _strongerPandoransModSettings.ListField;

                   

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
                    InstantiateArrowPickerController(_learnFirstSkillModSettings, _learnFirstSkill, _titleLearnFirstSkill, _descriptionLearnFirstSkill, _optionsBool, ConvertBoolToInt(config.LearnFirstPersonalSkill), OnLearnFirstSchoolValueChangedCallback, 0.5f);
                    InstantiateArrowPickerController(_moreMistVOModSettings, _moreMistVO, _titleMoreMistVO, _descriptionMoreMistVO, _optionsBool, ConvertBoolToInt(config.MoreMistVO), OnMoreMistValueChangedCallback, 0.5f);
                    //     InstantiateArrowPickerController(_reverseEngineeringModSettings, _reverseEngineering, _titleReverseEngineering, _descriptionReverseEngineering, _optionsBool, ConvertBoolToInt(config.ActivateReverseEngineeringResearch), OnReverseEngineeringValueChangedCallback, 0.5f);

                    InstantiateArrowPickerController(_disableTacSavesModSettings, _disableTacSaves, _titleDisableTacSaves, _descriptionDisableTacSaves, _optionsBool, ConvertBoolToInt(config.disableSavingOnTactical), OnDisableTacSavesValueChangedCallback, 0.5f);
                    InstantiateArrowPickerController(_skipMoviesModSettings, _skipMovies, _titleSkipMovies, _descriptionSkipMovies, _optionsBool, ConvertBoolToInt(config.SkipMovies), OnSkipMoviesValueChangedCallback, 0.5f);

                    InstantiateArrowPickerController(_exoticResourcesModSettings, _exoticResources, _titleExoticResources, _descriptionExoticResources, _amountPercentageResources, ConvertDifficultyToIndexExoticResources(), OnExoticResourcesValueChangedCallback, 0.5f);
                    InstantiateArrowPickerController(_resourcesEventsModSettings, _resourcesEvents, _titleResourcesEvents, _descriptionResourcesEvents, _amountPercentageResources, ConvertDifficultyToIndexEventsResources(), OnResourcesEventsValueChangedCallback, 0.5f);


                    SetAllVisibility(false);
                    UpdateOptionsOnSelectingDifficutly();

                    NewGameOptionsSetUp = true;
                  //  TFTVLogger.Always($"finished running big method");
                    /*    _startingFaction.gameObject.SetActive(false);
                        _startingFactionModSettings.gameObject.SetActive(false);
                        _startingBase.gameObject.SetActive(false);
                        _startingBaseModSettings.gameObject.SetActive(false);
                        _startingSquad.gameObject.SetActive(false);
                        _startingSquadModSettings.gameObject.SetActive(false);*/
                    /*   _startingScavSites.gameObject.SetActive(false);
                       _startingScavSitesModSettings.gameObject.SetActive(false);
                       _resCratePriority.gameObject.SetActive(false);
                       _resCratePriorityModSettings.gameObject.SetActive(false);
                       _recruitsPriority.gameObject.SetActive(false);
                       _recruitsPriorityModSettings.gameObject.SetActive(false);
                       _vehiclePriority.gameObject.SetActive(false);
                       _vehiclePriorityModSettings.gameObject.SetActive(false);*/


                    /*       _limitedCapture.gameObject.SetActive(false);
                           _limitedCaptureModSettings.gameObject.SetActive(false);
                           _limitedHarvesting.gameObject.SetActive(false);
                           _limitedHarvestingModSettings.gameObject.SetActive(false);
                           _tacticalDifficulty.gameObject.SetActive(false);
                           _tacticalDifficultyModSettings.gameObject.SetActive(false);*/


                    /*   _trading.gameObject.SetActive(false);
                       _tradingModSettings.gameObject.SetActive(false);
                       _limitedRaiding.gameObject.SetActive(false);
                       _limitedRaidingModSettings.gameObject.SetActive(false);
                       _noDropReinforcements.gameObject.SetActive(false);
                       _noDropReinforcementsModSettings.gameObject.SetActive(false);*/

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


            /*   private static ArrowPickerController _diploPenalties = null;


               private static ArrowPickerController _staminaDrain = null;


               private static ArrowPickerController _harderAmbush = null;


               private static ArrowPickerController _staminaRecuperation = null;


               private static ArrowPickerController _reverseEngineering = null;


               private static ArrowPickerController _havenSOS = null;


               private static ArrowPickerController _learnFirstSkill = null;*/




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

                        string warning = $"Saving and loading on Tactical can result in odd behavior and bugs (Vanilla issues). It is recommended to save only on Geoscape (and use several saves, in case one of them gets corrupted). And, you know what... losing soldiers in TFTV is fun :)";

                        GameUtl.GetMessageBox().ShowSimplePrompt(warning, MessageBoxIcon.Warning, MessageBoxButtons.OK, null);
                    }

                    bool option = newValue == 0;
                    string[] options = { "YES", "NO" };
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
                    string[] options = { "YES", "NO" };
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
                    string[] options = { "YES", "NO" };
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
                    string[] options = { "YES", "NO" };
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
                    string[] options = { "YES", "NO" };
                    _staminaRecuperation.CurrentItemText.text = options[newValue];
                    config.ActivateStaminaRecuperatonModule = option;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }



            /*    private static void OnReverseEngineeringValueChangedCallback(int newValue)
                {
                    try
                    {
                        bool option = newValue == 0;
                        string[] options = { "YES", "NO" };
                        _reverseEngineering.CurrentItemText.text = options[newValue];
                        config.ActivateReverseEngineeringResearch = option;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }*/


            private static void OnHavenSOSValueChangedCallback(int newValue)
            {
                try
                {
                    bool option = newValue == 0;
                    string[] options = { "YES", "NO" };
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
                    string[] options = { "YES", "NO" };
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
                    if (TFTVBetterEnemies.StrongerPandoransImplemented && newValue == 1 && NewGameOptionsSetUp)
                    {
                        string warning = $"Previous setting for {_titleStrongerPandorans} has already been implemetend on starting or a loading a game! PLEASE QUIT TO DESKTOP BEFORE STARTING OR LOADING A GAME";

                        GameUtl.GetMessageBox().ShowSimplePrompt(warning, MessageBoxIcon.Warning, MessageBoxButtons.OK, null);

                    }

                    bool option = newValue == 0;
                    string[] options = { "YES", "NO" };
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
                    string[] options = { "YES", "NO" };
                    _impossibleWeapons.CurrentItemText.text = options[newValue];
                    TFTVNewGameOptions.ImpossibleWeaponsAdjustmentsSetting = option;
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
                    string[] options = { "YES", "NO" };
                    _skipMovies.CurrentItemText.text = options[newValue];
                    config.SkipMovies = option;
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
                    string[] options = { "YES", "NO" };
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
                    string[] options = { "YES", "NO" };
                    _moreMistVO.CurrentItemText.text = options[newValue];
                    config.MoreMistVO = option;
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
                    string[] options = { "YES", "NO" };
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
                    string[] options = { "YES", "NO" };
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
                    string[] options = { "YES", "NO" };
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

                    _tacticalDifficulty.CurrentItemText.text = _optionsTacticalDifficulty[newValue];
                    config.difficultyOnTactical = (TFTVConfig.DifficultyOnTactical)newValue;
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

                    _startingFaction.CurrentItemText.text = _optionsStartingFaction[newValue];
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
                    _startingSquad.CurrentItemText.text = _optionsStartingSquad[newValue];
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
                    _startingBase.CurrentItemText.text = _optionsStartingBase[newValue];
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
                    string[] options = { "HIGH", "MEDIUM", "LOW", "NONE" };
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
                    string[] options = { "HIGH", "MEDIUM", "LOW", "NONE" };
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
                    string[] options = { "HIGH", "MEDIUM", "LOW", "NONE" };
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
                    if (TFTVDefsWithConfigDependency.ChangesToCapturingPandoransImplemented && newValue==1 && NewGameOptionsSetUp)
                    {
                        string warning = $"Previous setting for {_titleLimitedCapture} has already been implemetend on starting or a loading a game! PLEASE QUIT TO DESKTOP BEFORE STARTING OR LOADING A GAME";

                        GameUtl.GetMessageBox().ShowSimplePrompt(warning, MessageBoxIcon.Warning, MessageBoxButtons.OK, null);

                    }

                    bool limitedCapture = newValue == 0;
                    string[] options = { "YES", "NO" };
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
                    if (TFTVDefsWithConfigDependency.ChangesToFoodAndMutagenGenerationImplemented && newValue == 1 && NewGameOptionsSetUp)
                    {
                        string warning = $"Previous setting for {_titleLimitedHarvesting} has already been implemetend on starting or a loading a game! PLEASE QUIT TO DESKTOP BEFORE STARTING OR LOADING A GAME";

                        GameUtl.GetMessageBox().ShowSimplePrompt(warning, MessageBoxIcon.Warning, MessageBoxButtons.OK, null);

                    }


                    bool limitedHarvesting = newValue == 0;
                    string[] options = { "YES", "NO" };
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

        [HarmonyPatch(typeof(UIStateNewGeoscapeGameSettings), "EnterState")]
        public static class UIStateNewGeoscapeGameSettings_EnterState_Patch
        {
            private static void Prefix(UIStateNewGeoscapeGameSettings __instance)
            {
                try
                {
                    GameDifficultyLevelDef[] difficultyLevels = GameUtl.GameComponent<SharedData>().DifficultyLevels;

                    if (!EnterStateRun)
                    {
                        HomeScreenViewContext context = HomeScreenViewContextHook;

                        UIModuleGameSettings gameSettings = context.View.HomeScreenModules.GameSettings;

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


        //  public static bool BindSecondaryOptionRun = false;

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
                    TFTVNewGameOptions.Update35Check = true;
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

                    TFTVLogger.Always($"selected option: {gameSettings?.MainOptions?.Selected?.First()?.OptionIndex}");
                    EnterStateRun = false;


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

    }
}

