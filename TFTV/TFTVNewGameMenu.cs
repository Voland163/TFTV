using Base;
using Base.Core;
using Base.Defs;
using Base.Platforms;
using Base.UI;
using Base.UI.MessageBox;
using Base.UI.MessageBox.PromptControllers;
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
using PhoenixPoint.Tactical.Entities.Abilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using static TFTV.TFTVNewGameMenu.Options;

namespace TFTV
{
    internal class TFTVNewGameMenu
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly TFTVConfig config = TFTVMain.Main.Config;
        private static readonly SharedData Shared = TFTVMain.Shared;

        public static bool ShowedTacticalSavesWarning = false;

        private static int SelectedDifficulty = 0;

        private static ArrowPickerController _diploPenalties = null;
        private static ArrowPickerController _staminaDrain = null;
        private static ArrowPickerController _harderAmbush = null;
        private static ArrowPickerController _strongerPandorans = null;
        private static ArrowPickerController _imposssibleWeapons = null;
        private static ArrowPickerController _limitedHarvesting = null;
        private static ArrowPickerController _limitedCapturing = null;
        private static ArrowPickerController _noSecondChances = null;
        private static ArrowPickerController _etermesVulnerability = null;
        private static ArrowPickerController _exoticResources = null;
        private static ArrowPickerController _eventResources = null;

        // Eldritch warning gate (hardest difficulty confirmation)
        private static bool _eldritchWarningInProgress = false;
        private static bool _eldritchWarningAccepted = false;
        private static bool _suppressNextOnConfirm = false;

        private static bool IsHardestDifficultySelected()
        {
            // In this menu SelectedDifficulty is 1..6 (see ConvertDifficultyToIndex* and EnterState logic)
            return SelectedDifficulty == 6;
        }

        

        private static readonly MethodInfo _gameSettingsOnConfirmMethod =
            typeof(UIStateNewGeoscapeGameSettings).GetMethod("GameSettings_OnConfirm", BindingFlags.Instance | BindingFlags.NonPublic);
       

        private static void StartEldritchWarningsThenConfirm(UIStateNewGeoscapeGameSettings state)
        {
            try
            {
                if (_eldritchWarningInProgress || _eldritchWarningAccepted)
                {
                    return;
                }

                _eldritchWarningInProgress = true;

                ShowStep(0);

                void ShowStep(int step)
                {
                    string key = step == 0
                        ? "KEY_ELDRITCH_WARNING_0"
                        : step == 1
                            ? "KEY_ELDRITCH_WARNING_1"
                            : "KEY_ELDRITCH_WARNING_2";

                    string text = TFTVCommonMethods.ConvertKeyToString(key);

                    MessageBox mb = GameUtl.GetMessageBox();

                    mb.ShowSimplePrompt(text, MessageBoxIcon.Warning, MessageBoxButtons.OKCancel, res =>
                    {
                        try
                        {
                            if (res.DialogResult != MessageBoxResult.OK && res.DialogResult != MessageBoxResult.Yes)
                            {
                                _eldritchWarningInProgress = false;
                                return;
                            }

                            if (step < 2)
                            {
                                ShowStep(step + 1);
                                return;
                            }

                            _eldritchWarningAccepted = true;
                            _eldritchWarningInProgress = false;

                            _suppressNextOnConfirm = true;
                            if (_gameSettingsOnConfirmMethod == null)
                            {
                                TFTVLogger.Error(new InvalidOperationException("Failed to resolve UIStateNewGeoscapeGameSettings.GameSettings_OnConfirm via reflection."));
                                return;
                            }

                            _gameSettingsOnConfirmMethod.Invoke(state, null);
                        }
                        catch (Exception e)
                        {
                            _eldritchWarningInProgress = false;
                            TFTVLogger.Error(e);
                        }
                    });

                  
                    Image background = mb.GetComponentInChildren<Image>();

                    background.color = Color.black;

                 /*   foreach (Component component in mb.GetComponentsInChildren<Component>())
                    {
                        TFTVLogger.Always($"MessageBox component in children {component.name}: {component.GetType()}");

                        if(component is Image image) 
                        {
                            image.color = Color.black;
                        }

                    }*/

                }
            }
            catch (Exception e)
            {
                _eldritchWarningInProgress = false;
                TFTVLogger.Error(e);
            }
        }

        internal class TitleScreen
        {

            internal static void SetTFTVLogo(HomeScreenView homeScreenView)
            {
                try
                {
                    Resolution resolution = Screen.currentResolution;
                    float resolutionFactorWidth = (float)resolution.width / 1920f;
                    //   TFTVLogger.Always("ResolutionFactorWidth is " + resolutionFactorWidth);
                    float resolutionFactorHeight = (float)resolution.height / 1080f;


                    GameObject source = homeScreenView.HomeScreenModules.MainMenuButtonsModule.VanillaVisuals[0];

                    Image logoText = homeScreenView.HomeScreenModules.MainMenuButtonsModule.VanillaVisuals[0].GetComponentsInChildren<Image>().FirstOrDefault(i => i.name == "PhoenixLogo_text");
                    Image logoImage = homeScreenView.HomeScreenModules.MainMenuButtonsModule.VanillaVisuals[0].GetComponentsInChildren<Image>().FirstOrDefault(i => i.name == "PhoenixLogo_symbol");


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


            [HarmonyPatch(typeof(HomeScreenView), "InitView")] //VERIFIED
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

                        SetTFTVLogo(__instance);

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            [HarmonyPatch(typeof(EditionVisualsController), nameof(EditionVisualsController.DetermineEdition))]
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
        }

        internal class DefaultDifficulties
        {


            public static int ConvertDifficultyToIndexExoticResources()
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

            public static int ConvertDifficultyToIndexEventsResources()
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

            public static void UpdateOptionsOnSelectingDifficutly()
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
                    int etermesVulnerability = 1;

                    if (SelectedDifficulty > 5)
                    {
                        noSecondChances = 0;
                        etermesVulnerability = 0;
                    }

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
                    UIModuleGameSettings gameSettings = GameUtl.CurrentLevel().GetComponent<HomeScreenView>().HomeScreenModules.GameSettings;

                    MethodInfo diploPenaltiesChangedCallback = typeof(UIStateNewGeoscapeGameSettings_InitFullContent_patch).GetMethod("OnDiploPenaltiesValueChangedCallback", BindingFlags.NonPublic | BindingFlags.Static);

                    diploPenaltiesChangedCallback.Invoke(gameSettings, new object[] { diploPenalty, _diploPenalties });

                    MethodInfo staminaDrainValueChangedCallback = typeof(UIStateNewGeoscapeGameSettings_InitFullContent_patch).GetMethod("OnStaminaDrainValueChangedCallback", BindingFlags.NonPublic | BindingFlags.Static);
                    MethodInfo harderAmbushValueChangedCallback = typeof(UIStateNewGeoscapeGameSettings_InitFullContent_patch).GetMethod("OnHarderAmbushValueChangedCallback", BindingFlags.NonPublic | BindingFlags.Static);
                    MethodInfo strongerPandoransValueChangedCallback = typeof(UIStateNewGeoscapeGameSettings_InitFullContent_patch).GetMethod("OnStrongerPandoransValueChangedCallback", BindingFlags.NonPublic | BindingFlags.Static);
                    MethodInfo impossibleWeaponsValueChangedCallback = typeof(UIStateNewGeoscapeGameSettings_InitFullContent_patch).GetMethod("OnImpossibleWeaponsValueChangedCallback", BindingFlags.NonPublic | BindingFlags.Static);
                    MethodInfo limitedHarvestingValueChangedCallback = typeof(UIStateNewGeoscapeGameSettings_InitFullContent_patch).GetMethod("OnLimitedHarvestingValueChangedCallback", BindingFlags.NonPublic | BindingFlags.Static);
                    MethodInfo limitedCaptureValueChangedCallback = typeof(UIStateNewGeoscapeGameSettings_InitFullContent_patch).GetMethod("OnLimitedCaptureValueChangedCallback", BindingFlags.NonPublic | BindingFlags.Static);
                    MethodInfo noSecondChancesValueChangedCallback = typeof(UIStateNewGeoscapeGameSettings_InitFullContent_patch).GetMethod("OnNoSecondChancesValueChangedCallback", BindingFlags.NonPublic | BindingFlags.Static);
                    MethodInfo etermesVulnerabilityResistanceValueChangedCallback = typeof(UIStateNewGeoscapeGameSettings_InitFullContent_patch).GetMethod("OnEtermesVulnerabilityResistanceValueChangedCallback", BindingFlags.NonPublic | BindingFlags.Static);

                    staminaDrainValueChangedCallback.Invoke(gameSettings, new object[] { staminaDrain, _staminaDrain });
                    harderAmbushValueChangedCallback.Invoke(gameSettings, new object[] { harderAmnbush, _harderAmbush });
                    strongerPandoransValueChangedCallback.Invoke(gameSettings, new object[] { strongerPandorans, _strongerPandorans });
                    impossibleWeaponsValueChangedCallback.Invoke(gameSettings, new object[] { impossibleWeapons, _imposssibleWeapons });
                    limitedHarvestingValueChangedCallback.Invoke(gameSettings, new object[] { limitedHarvesting, _limitedHarvesting });
                    limitedCaptureValueChangedCallback.Invoke(gameSettings, new object[] { limitedCapture, _limitedCapturing });
                    noSecondChancesValueChangedCallback.Invoke(gameSettings, new object[] { noSecondChances, _noSecondChances });
                    etermesVulnerabilityResistanceValueChangedCallback.Invoke(gameSettings, new object[] { etermesVulnerability, _etermesVulnerability });

                    MethodInfo exoticResourcesChangedCallback = typeof(UIStateNewGeoscapeGameSettings_InitFullContent_patch).GetMethod("OnExoticResourcesValueChangedCallback", BindingFlags.NonPublic | BindingFlags.Static);
                    exoticResourcesChangedCallback.Invoke(gameSettings, new object[] { ConvertDifficultyToIndexExoticResources(), _exoticResources });

                    MethodInfo eventResourcesChangedCallback = typeof(UIStateNewGeoscapeGameSettings_InitFullContent_patch).GetMethod("OnResourcesEventsValueChangedCallback", BindingFlags.NonPublic | BindingFlags.Static);
                    eventResourcesChangedCallback.Invoke(gameSettings, new object[] { ConvertDifficultyToIndexEventsResources(), _eventResources });

                }


                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            [HarmonyPatch(typeof(GameOptionViewController), "OnClicked")] //VERIFIED
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
                            TFTVMain.Main.Config.Difficulty = SelectedDifficulty - 1;
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

        }

        internal class Options
        {

            private static float _resolutionFactor = 0f;
            private static float _resolutionFactorWidth = 0f;
            private static float _resolutionFactorHeight = 0f;

            private static List<GameOptionViewController> _gameOptionViewControllers = new List<GameOptionViewController>();

            private static readonly string _titleAnytimeOptions = "TFTV_ANYTIME_OPTIONS_TITLE";
            private static readonly string _descriptionAnytimeOptions = "TFTV_ANYTIME_OPTIONS_DESCRIPTION";

            private static readonly string _titleAdditionalStartOptions = "TFTV_ADDITIONAL_START_OPTIONS_TITLE";
            private static readonly string _descriptionAdditionalStartOptions = "TFTV_ADDITIONAL_START_DESCRIPTION";

            private static readonly string[] _amountPercentageResources = { "25%", "50%", "75%", "100%", "125%", "150%", "175%", "200%", "250%", "300%", "400%" };
            private static readonly float[] _amountMultiplierResources = { 0.25f, 0.5f, 0.75f, 1f, 1.25f, 1.5f, 1.75f, 2f, 2.5f, 3f, 4f };

            private static readonly string[] _optionsTacticalDifficulty = { "NO_CHANGE", "TFTV_DIFFICULTY_ROOKIE_TITLE", "KEY_DIFFICULTY_EASY", "KEY_DIFFICULTY_STANDARD", "KEY_DIFFICULTY_DIFFICULT", "KEY_DIFFICULTY_VERY_DIFFICULT", "TFTV_DIFFICULTY_ETERMES_TITLE" };

            private static readonly string[] _optionsResCratePriority = { "HIGH", "MEDIUM", "LOW", "NONE" };
            private static readonly string[] _optionsRecruitsPriority = { "HIGH", "MEDIUM", "LOW", "NONE" };
            private static readonly string[] _optionsVehiclePriority = { "HIGH", "MEDIUM", "LOW", "NONE" };


            private static readonly string[] _optionsBool = { "YES", "NO" };

            private static readonly string[] _optionsScavSites = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23", "24", "25", "26", "27", "28", "29", "30", "31", "32" };


            private static readonly string[] _optionsStartingFaction = { "KEY_FACTION_NAME_PHOENIX", "KEY_FACTION_NAME_ANU", "KEY_FACTION_NAME_NEW_JERICHO", "KEY_FACTION_NAME_SYNEDRION" };


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


            private static readonly string[] _optionsStartingSquad = { "UNBUFFED", "BUFFED", "RANDOM" };

            private static List<ModSettingController> _newStartScavSettings = new List<ModSettingController>();
            private static List<ModSettingController> _additionalStartOptionsSettings = new List<ModSettingController>();
            private static List<ModSettingController> _anytimeOptionsSettings = new List<ModSettingController>();
            private static List<ModSettingController> _cheatOptionsSettings = new List<ModSettingController>();

            internal class HelperMethods
            {

                private static float GetResolutionFactor()
                {
                    try
                    {
                        if (_resolutionFactor == 0)
                        {
                            Resolution resolution = Screen.currentResolution;
                            _resolutionFactor = (float)resolution.width / (float)resolution.height;
                            TFTVLogger.Always($"_resolutionFactor: {_resolutionFactor}");
                        }

                        return _resolutionFactor;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }

                }

                private static float GetResolutionFactorWidth()
                {
                    try
                    {
                        if (_resolutionFactorWidth == 0)
                        {
                            Resolution resolution = Screen.currentResolution;
                            _resolutionFactorWidth = (float)resolution.width / 1920f;
                            TFTVLogger.Always($"_resolutionFactorHeight: {_resolutionFactorWidth}");
                        }

                        return _resolutionFactorWidth;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }

                }

                private static float GetResolutionFactorHeight()
                {
                    try
                    {
                        if (_resolutionFactorHeight == 0)
                        {
                            Resolution resolution = Screen.currentResolution;
                            _resolutionFactorHeight = (float)resolution.height / 1080f;
                            TFTVLogger.Always($"_resolutionFactorWidth: {_resolutionFactorHeight}");
                        }

                        return _resolutionFactorHeight;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }

                }


                internal static void InstantiateGameOptionViewController(string titleKey, string descriptionKey, string onToggleMethod)
                {
                    try
                    {

                        Resolution resolution = Screen.currentResolution;
                        float resolutionFactorWidth = GetResolutionFactorWidth();
                        float resolutionFactorHeight = GetResolutionFactorHeight();
                        bool ultrawideresolution = GetResolutionFactor() > 2;
                        
                        UIModuleGameSettings uIModuleGameSettings = GameUtl.CurrentLevel().GetComponent<HomeScreenView>().HomeScreenModules.GameSettings;
                        
                        RectTransform rectTransform = uIModuleGameSettings.GameAddiotionalContentGroup.GetComponentInChildren<RectTransform>();
                        GameOptionViewController gameOptionViewController = UnityEngine.Object.Instantiate(uIModuleGameSettings.SecondaryOptions.Container.GetComponentsInChildren<GameOptionViewController>().First(), rectTransform);

                        LocalizedTextBind description = new LocalizedTextBind
                        {
                            LocalizationKey = descriptionKey
                        };

                        LocalizedTextBind text = new LocalizedTextBind
                        {
                            LocalizationKey = titleKey
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
                        else
                        {
                            // TFTVLogger.Always($"UW resolution detected!");
                            int additionalFactor = 2;

                            gameOptionViewController.SelectButton.transform.position -= new Vector3(370 * resolutionFactorWidth / additionalFactor, 0, 0);

                        }

                        gameOptionViewController.transform.localScale *= 0.70f;


                        //  gameOptionViewController.SelectButton.transform.position += new Vector3(270 * resolutionFactorWidth, 0, 0);
                        UITooltipText uITooltipText = gameOptionViewController.gameObject.AddComponent<UITooltipText>();

                        uITooltipText.TipKey = description;

                        _gameOptionViewControllers.Add(gameOptionViewController);

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                internal static void InstantiateArrowPickerController(string baseKey,
           string[] optionsKeys,
           int currentValue,
           Action<int, ArrowPickerController> onValueChangedWithController,
           float lengthScale, List <ModSettingController> optionsType = null)
                {
                    try
                    {
                        ModSettingController ModSettingControllerHook = GameUtl.CurrentLevel().GetComponent<HomeScreenView>().HomeScreenModules.ModManagerModule.SettingsModSettingPrefab;
                        RectTransform rectTransform = GameUtl.CurrentLevel().GetComponent<HomeScreenView>().HomeScreenModules.GameSettings.GameAddiotionalContentGroup.GetComponentInChildren<RectTransform>();

                        ModSettingController modSettingController = UnityEngine.Object.Instantiate(ModSettingControllerHook, rectTransform);
                        ArrowPickerController arrowPickerController = modSettingController.ListField;

                        // Extract title and description keys from the modSettingController's name                   
                        string titleKey = $"KEY_{baseKey}";
                        string descriptionKey = $"KEY_{baseKey}_DESCRIPTION";

                        // TFTVLogger.Always($"baseKey: {baseKey} titleKey {titleKey} descriptionKey: {descriptionKey} ");

                        //_startingFactionModSettings

                        string title = TFTVCommonMethods.ConvertKeyToString(titleKey);
                        string description = TFTVCommonMethods.ConvertKeyToString(descriptionKey);

                        // Localize options
                        string[] options = new string[] { };

                        options = optionsKeys.Select(key => new LocalizedTextBind { LocalizationKey = key }.Localize()).ToArray();

                        // Configure the modSettingController
                        modSettingController.Label.text = title;
                        modSettingController.transform.localScale *= 0.75f;

                        // Adjust arrowPickerController position and scale
                        AdjustArrowPickerPositionAndScale(arrowPickerController, modSettingController, lengthScale);

                        // Add tooltip for description
                        AddTooltip(modSettingController, description);

                        // Initialize the arrow picker
                        arrowPickerController.Init(options.Length, currentValue, newValue =>
                        {
                            onValueChangedWithController?.Invoke(newValue, arrowPickerController);
                        });
                        arrowPickerController.CurrentItemText.text = options[currentValue];

                        arrowPickerController.GetComponent<RectTransform>().sizeDelta = new Vector2(
                            arrowPickerController.GetComponent<RectTransform>().sizeDelta.x * lengthScale,
                            arrowPickerController.GetComponent<RectTransform>().sizeDelta.y
                        );

                        // Populate options
                        PopulateOptions(arrowPickerController, options);

                        optionsType?.Add(modSettingController);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static void AdjustArrowPickerPositionAndScale(
     ArrowPickerController arrowPickerController,
     ModSettingController modSettingController,
     float lengthScale)
                {
                    try
                    {
                        Resolution resolution = Screen.currentResolution;
                        float resolutionFactorWidth = (float)resolution.width / 1920f;
                        bool ultrawideResolution = (float)resolution.width / resolution.height > 2;

                        if (!ultrawideResolution)
                        {
                            arrowPickerController.transform.position += new Vector3(270 * resolutionFactorWidth, 0, 0);
                            if (lengthScale != 1)
                            {
                                arrowPickerController.transform.position += new Vector3(150 * resolutionFactorWidth * lengthScale, 0, 0);
                            }
                            modSettingController.Label.rectTransform.Translate(new Vector3(-270 * resolutionFactorWidth, 0, 0), arrowPickerController.transform);
                        }
                        else
                        {
                            int additionalFactor = 2;

                            // Small UW-only "pull back" to prevent right arrow from being clipped by the viewport.
                            // Tune 35f -> 25-60 depending on how aggressive the clip is.
                            float uwSafeInset = 35f * resolutionFactorWidth;

                            arrowPickerController.transform.position += new Vector3((370 * resolutionFactorWidth / additionalFactor) - uwSafeInset, 0, 0);

                            if (lengthScale <= 0.5f)
                            {
                                arrowPickerController.transform.position += new Vector3((300 * resolutionFactorWidth / additionalFactor * lengthScale) - uwSafeInset, 0, 0);
                            }
                            else
                            {
                                arrowPickerController.transform.position += new Vector3((130 * resolutionFactorWidth / additionalFactor * lengthScale) - uwSafeInset, 0, 0);
                            }

                            modSettingController.Label.rectTransform.Translate(new Vector3(-370 * resolutionFactorWidth / additionalFactor, 0, 0), arrowPickerController.transform);
                        }

                        modSettingController.Label.alignment = TextAnchor.MiddleLeft;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static void AddTooltip(ModSettingController modSettingController, string description)
                {
                    try
                    {
                        UnityEngine.Object.Destroy(modSettingController.GetComponentInChildren<UITooltipText>());

                        UITooltipText uITooltipText = modSettingController.Label.gameObject.AddComponent<UITooltipText>();

                        uITooltipText.TipText = description;

                       
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static void PopulateOptions(ArrowPickerController arrowPickerController, string[] options)
                {
                    try
                    {

                        for (int i = 0; i < options.Length; i++)
                        {
                            arrowPickerController.CurrentItemText.text = options[i];
                            MethodInfo onNewValue = arrowPickerController.GetType().GetMethod("OnNewValue", BindingFlags.NonPublic | BindingFlags.Instance);
                            onNewValue.Invoke(arrowPickerController, null);
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            [HarmonyPatch(typeof(UIModuleGameSettings), "InitFullContent")] //VERIFIED
            internal static class UIStateNewGeoscapeGameSettings_InitFullContent_patch
            {
               
                private static void SetNewStartScavVisibility(bool show)
                {
                    try
                    {
                        foreach (ModSettingController modSettingController in _newStartScavSettings)
                        {

                            if (modSettingController != null)
                            {
                                modSettingController.gameObject.SetActive(show);
                                modSettingController.ListField.gameObject.SetActive(show);
                            }
                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static void SetCheatOptionsVisibility(bool show)
                {
                    try
                    {
                        foreach (ModSettingController modSettingController in _cheatOptionsSettings)
                        {

                            if (modSettingController != null)
                            {
                                modSettingController.gameObject.SetActive(show);
                                modSettingController.ListField.gameObject.SetActive(show);
                            }
                        }

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
                        foreach(GameOptionViewController gameOptionViewController in _gameOptionViewControllers)
                        {
                            if (gameOptionViewController != null)
                            {
                                gameOptionViewController.IsSelected = show;
                            }
                        }

                        SetNewStartScavVisibility(show);
                        SetAdditionalStartOptionsVisibility(show);
                        SetAnyTimeOptionsVisibility(show);
                        SetCheatOptionsVisibility(show);

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
                        foreach (ModSettingController modSettingController in _additionalStartOptionsSettings)
                        {
                            if (modSettingController != null)
                            {
                                modSettingController.gameObject.SetActive(show);
                                modSettingController.ListField.gameObject.SetActive(show);
                            }
                        }

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
                        foreach (ModSettingController modSettingController in _anytimeOptionsSettings)
                        {
                            if (modSettingController != null)
                            {
                                modSettingController.gameObject.SetActive(show);
                                modSettingController.ListField.gameObject.SetActive(show);
                            }
                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }

                }





               


                public static void Postfix(UIModuleGameSettings __instance)
                {
                    try
                    {
                        ModSettingController ModSettingControllerHook = GameUtl.CurrentLevel().GetComponent<HomeScreenView>().HomeScreenModules.ModManagerModule.SettingsModSettingPrefab;
                        RectTransform rectTransform = GameUtl.CurrentLevel().GetComponent<HomeScreenView>().HomeScreenModules.GameSettings.GameAddiotionalContentGroup.GetComponentInChildren<RectTransform>();

                        rectTransform.DestroyChildren();


                        

                        HelperMethods.InstantiateArrowPickerController("StartingFaction", _optionsStartingFaction, (int)(TFTVNewGameOptions.startingSquad), OnStartingFactionValueChangedCallback, 1f);
                        HelperMethods.InstantiateArrowPickerController("StartingBase", _optionsStartingBase, (int)(TFTVNewGameOptions.startingBaseLocation), OnStartingBaseValueChangedCallback, 1f);
                        HelperMethods.InstantiateArrowPickerController("StartingSquad", _optionsStartingSquad, (int)(TFTVNewGameOptions.startingSquadCharacters), OnStartingSquadValueChangedCallback, 1f);
                        HelperMethods.InstantiateArrowPickerController("TacticalDifficulty", _optionsTacticalDifficulty, (int)config.TacticalDifficulty, OnTacticalDifficultyValueChangedCallback, 1f);
                        HelperMethods.InstantiateArrowPickerController("DisableTacSaves", _optionsBool, ConvertBoolToInt(config.DisableTacSaves), OnDisableTacSavesValueChangedCallback, 0.5f);
                        HelperMethods.InstantiateGameOptionViewController("TFTV_ALL_OPTIONS_TITLE", "TFTV_ALL_OPTIONS_DESCRIPTION", "SetAllVisibility");
                        HelperMethods.InstantiateGameOptionViewController("TFTV_SCAVENGING_OPTIONS_TITLE", "TFTV_SCAVENGING_OPTIONS_DESCRIPTION", "SetNewStartScavVisibility");
                        HelperMethods.InstantiateArrowPickerController("ScavSites", _optionsScavSites, TFTVNewGameOptions.initialScavSites, OnStartingScavSitesValueChangedCallback, 1f, _newStartScavSettings);
                        HelperMethods.InstantiateArrowPickerController("ResCratePriority", _optionsResCratePriority, (int)(TFTVNewGameOptions.chancesScavCrates), OnResScavPriorityValueChangedCallback, 1f, _newStartScavSettings);
                        HelperMethods.InstantiateArrowPickerController("RecruitsPriority", _optionsRecruitsPriority, (int)(TFTVNewGameOptions.chancesScavSoldiers), OnRecruitsPriorityValueChangedCallback, 1f, _newStartScavSettings);
                        HelperMethods.InstantiateArrowPickerController("VehiclePriority", _optionsVehiclePriority, (int)(TFTVNewGameOptions.chancesScavGroundVehicleRescue), OnVehiclePriorityValueChangedCallback, 1f, _newStartScavSettings);


                        HelperMethods.InstantiateGameOptionViewController(_titleAdditionalStartOptions, _descriptionAdditionalStartOptions, "SetAdditionalStartOptionsVisibility");
                        HelperMethods.InstantiateArrowPickerController("StrongerPandorans", _optionsBool, ConvertBoolToInt(TFTVNewGameOptions.StrongerPandoransSetting), OnStrongerPandoransValueChangedCallback, 0.5f, _additionalStartOptionsSettings);
                        HelperMethods.InstantiateArrowPickerController("StaminaDrain", _optionsBool, ConvertBoolToInt(TFTVNewGameOptions.StaminaPenaltyFromInjurySetting), OnStaminaDrainValueChangedCallback, 0.5f, _additionalStartOptionsSettings);
                        HelperMethods.InstantiateArrowPickerController("LimitedCapture", _optionsBool, ConvertBoolToInt(TFTVNewGameOptions.LimitedCaptureSetting), OnLimitedCaptureValueChangedCallback, 0.5f, _additionalStartOptionsSettings);
                        HelperMethods.InstantiateArrowPickerController("LimitedHarvesting", _optionsBool, ConvertBoolToInt(TFTVNewGameOptions.LimitedHarvestingSetting), OnLimitedHarvestingValueChangedCallback, 0.5f, _additionalStartOptionsSettings);
                        HelperMethods.InstantiateArrowPickerController("DiploPenalties", _optionsBool, ConvertBoolToInt(TFTVNewGameOptions.DiplomaticPenaltiesSetting), OnDiploPenaltiesValueChangedCallback, 0.5f, _additionalStartOptionsSettings);
                        HelperMethods.InstantiateArrowPickerController("ExoticResources", _amountPercentageResources, DefaultDifficulties.ConvertDifficultyToIndexExoticResources(), OnExoticResourcesValueChangedCallback, 0.5f, _additionalStartOptionsSettings);
                        HelperMethods.InstantiateArrowPickerController("HarderAmbush", _optionsBool, ConvertBoolToInt(TFTVNewGameOptions.MoreAmbushesSetting), OnHarderAmbushValueChangedCallback, 0.5f, _additionalStartOptionsSettings);
                        HelperMethods.InstantiateArrowPickerController("ImpossibleWeapons", _optionsBool, ConvertBoolToInt(TFTVNewGameOptions.ImpossibleWeaponsAdjustmentsSetting), OnImpossibleWeaponsValueChangedCallback, 0.5f, _additionalStartOptionsSettings);
                        HelperMethods.InstantiateArrowPickerController("NoSecondChances", _optionsBool, ConvertBoolToInt(TFTVNewGameOptions.NoSecondChances), OnNoSecondChancesValueChangedCallback, 0.5f, _additionalStartOptionsSettings);
                        HelperMethods.InstantiateArrowPickerController("EtermesVulnerabilityResistance", _optionsBool, Math.Max(TFTVNewGameOptions.EtermesResistanceAndVulnerability - 1, 0), OnEtermesVulnerabilityResistanceValueChangedCallback, 0.5f, _additionalStartOptionsSettings);
                        HelperMethods.InstantiateArrowPickerController("ResourcesEvents", _amountPercentageResources, DefaultDifficulties.ConvertDifficultyToIndexEventsResources(), OnResourcesEventsValueChangedCallback, 0.5f, _additionalStartOptionsSettings);

                        HelperMethods.InstantiateGameOptionViewController(_titleAnytimeOptions, _descriptionAnytimeOptions, "SetAnyTimeOptionsVisibility");
                        HelperMethods.InstantiateArrowPickerController("NoDropReinforcements", _optionsBool, ConvertBoolToInt(config.NoDropReinforcements), OnNoDropValueChangedCallback, 0.5f, _anytimeOptionsSettings);
                        HelperMethods.InstantiateArrowPickerController("Flinching", _optionsBool, ConvertBoolToInt(config.Flinching), OnFlinchingValueChangedCallback, 0.5f, _anytimeOptionsSettings);
                        HelperMethods.InstantiateArrowPickerController("SkipMovies", _optionsBool, ConvertBoolToInt(config.SkipMovies), OnSkipMoviesValueChangedCallback, 0.5f, _anytimeOptionsSettings);
                        HelperMethods.InstantiateArrowPickerController("HavenSOS", _optionsBool, ConvertBoolToInt(config.HavenSOS), OnHavenSOSValueChangedCallback, 0.5f, _anytimeOptionsSettings);
                        HelperMethods.InstantiateArrowPickerController("StaminaRecuperation", _optionsBool, ConvertBoolToInt(config.StaminaRecuperation), OnStaminaRecuperationValueChangedCallback, 0.5f, _anytimeOptionsSettings);
                        HelperMethods.InstantiateArrowPickerController("LearnFirstSkill", _optionsBool, ConvertBoolToInt(config.LearnFirstSkill), OnLearnFirstSchoolValueChangedCallback, 0.5f, _anytimeOptionsSettings);
                        HelperMethods.InstantiateArrowPickerController("Trading", _optionsBool, ConvertBoolToInt(config.Trading), OnTradingValueChangedCallback, 0.5f, _anytimeOptionsSettings);
                        HelperMethods.InstantiateArrowPickerController("MoreMistVO", _optionsBool, ConvertBoolToInt(config.MoreMistVO), OnMoreMistValueChangedCallback, 0.5f, _anytimeOptionsSettings);
                        HelperMethods.InstantiateArrowPickerController("LimitedDeploymentVO", _optionsBool, ConvertBoolToInt(config.LimitedDeploymentVO), OnLimitedDeploymentValueChangedCallback, 0.5f, _anytimeOptionsSettings);
                        HelperMethods.InstantiateArrowPickerController("LimitedRaiding", _optionsBool, ConvertBoolToInt(config.LimitedRaiding), OnLimitedRaidingValueChangedCallback, 0.5f, _anytimeOptionsSettings);
                        HelperMethods.InstantiateArrowPickerController("NoBarks", _optionsBool, ConvertBoolToInt(config.NoBarks), OnNoBarksValueChangedCallback, 0.5f, _anytimeOptionsSettings);
                        HelperMethods.InstantiateArrowPickerController("ShowAmbushExfil", _optionsBool, ConvertBoolToInt(config.ShowAmbushExfil), OnShowAmbushExfilValueChangedCallback, 0.5f, _anytimeOptionsSettings);
                        HelperMethods.InstantiateArrowPickerController("SkipFSTutorial", _optionsBool, ConvertBoolToInt(config.SkipFSTutorial), OnSkipFSTutorialValueChangedCallback, 0.5f, _anytimeOptionsSettings);
                        HelperMethods.InstantiateArrowPickerController("CustomPortraits", _optionsBool, ConvertBoolToInt(config.CustomPortraits), OnCustomPortraitsValueChangedCallback, 0.5f, _anytimeOptionsSettings);
                        HelperMethods.InstantiateArrowPickerController("HandGrenadeScatter", _optionsBool, ConvertBoolToInt(config.HandGrenadeScatter), OnHandGrenadeScatterValueChangedCallback, 0.5f, _anytimeOptionsSettings);
                        HelperMethods.InstantiateArrowPickerController("EquipBeforeAmbush", _optionsBool, ConvertBoolToInt(config.EquipBeforeAmbush), OnEquipBeforeAmbushValueChangedCallback, 0.5f, _anytimeOptionsSettings);
                        HelperMethods.InstantiateArrowPickerController("TFTVSuppression", _optionsBool, ConvertBoolToInt(config.TFTVSuppression), OnTFTVSuppressionValueChangedCallback, 0.5f, _anytimeOptionsSettings);

                        HelperMethods.InstantiateGameOptionViewController("TFTV_CHEAT_OPTIONS_TITLE", "TFTV_CHEAT_OPTIONS_DESCRIPTION", "SetCheatOptionsVisibility");

                        HelperMethods.InstantiateArrowPickerController("DeadDropAllLoot", _optionsBool, ConvertBoolToInt(config.DeadDropAllLoot), OnDeadDropAllLootValueChangedCallback, 0.5f, _cheatOptionsSettings);
                        HelperMethods.InstantiateArrowPickerController("UnLimitedDeployment", _optionsBool, ConvertBoolToInt(config.UnLimitedDeployment), OnUnLimitedDeploymentValueChangedCallback, 0.5f, _cheatOptionsSettings);
                        HelperMethods.InstantiateArrowPickerController("DeliriumCappedAt4", _optionsBool, ConvertBoolToInt(config.DeliriumCappedAt4), OnDeliriumCappedAt4ValueChangedCallback, 0.5f, _cheatOptionsSettings);
                        HelperMethods.InstantiateArrowPickerController("AllowFullAugmentations", _optionsBool, ConvertBoolToInt(config.AllowFullAugmentations), OnAllowFullAugmentationsValueChangedCallback, 0.5f, _cheatOptionsSettings);
                        HelperMethods.InstantiateArrowPickerController("MercsCanBeAugmented", _optionsBool, ConvertBoolToInt(config.MercsCanBeAugmented), OnMercsCanBeAugmentedValueChangedCallback, 0.5f, _cheatOptionsSettings);
                        HelperMethods.InstantiateArrowPickerController("VehicleAndMutogSize1", _optionsBool, ConvertBoolToInt(config.VehicleAndMutogSize1), OnVehicleAndMutogSize1ValueChangedCallback, 0.5f, _cheatOptionsSettings);
                        HelperMethods.InstantiateArrowPickerController("MultipleVehiclesInAircraftAllowed", _optionsBool, ConvertBoolToInt(config.MultipleVehiclesInAircraftAllowed), OnMultipleVehiclesInAircraftAllowedValueChangedCallback, 0.5f, _cheatOptionsSettings);
                        HelperMethods.InstantiateArrowPickerController("EasyAirCombat", _optionsBool, ConvertBoolToInt(config.EasyAirCombat), OnEasyAirCombatValueChangedCallback, 0.5f, _cheatOptionsSettings);
                        HelperMethods.InstantiateArrowPickerController("BehemothSubmergesForever", _optionsBool, ConvertBoolToInt(config.BehemothSubmergesForever), OnBehemothSubmergesForeverValueChangedCallback, 0.5f, _cheatOptionsSettings);
                        
                        SetAllVisibility(false);
                        DefaultDifficulties.UpdateOptionsOnSelectingDifficutly();
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

                private static void OnDeadDropAllLootValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                        bool option = newValue == 0;
                        string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                        config.DeadDropAllLoot = option;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnUnLimitedDeploymentValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                        bool option = newValue == 0;
                        string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                        config.UnLimitedDeployment = option;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnDeliriumCappedAt4ValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                        bool option = newValue == 0;
                        string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                       config.DeliriumCappedAt4 = option;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnAllowFullAugmentationsValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                        bool option = newValue == 0;
                        string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                        config.AllowFullAugmentations = option;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }


                private static void OnVehicleAndMutogSize1ValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                        bool option = newValue == 0;
                        string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                        config.VehicleAndMutogSize1 = option;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }


                private static void OnMercsCanBeAugmentedValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                        bool option = newValue == 0;
                        string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                        config.MercsCanBeAugmented = option;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnMultipleVehiclesInAircraftAllowedValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                        bool option = newValue == 0;
                        string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                       config.MultipleVehiclesInAircraftAllowed = option;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnEasyAirCombatValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                        bool option = newValue == 0;
                        string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                        config.EasyAirCombat = option;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnBehemothSubmergesForeverValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                        bool option = newValue == 0;
                        string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                        config.BehemothSubmergesForever = option;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnExoticResourcesValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                        arrowPickerController.CurrentItemText.text = _amountPercentageResources[newValue];
                        TFTVNewGameOptions.AmountOfExoticResourcesSetting = _amountMultiplierResources[newValue];
                        _exoticResources = arrowPickerController;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnResourcesEventsValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                        arrowPickerController.CurrentItemText.text = _amountPercentageResources[newValue];
                        TFTVNewGameOptions.ResourceMultiplierSetting = _amountMultiplierResources[newValue];
                        _eventResources = arrowPickerController;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnDisableTacSavesValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                       /* if (newValue == 1 && !ShowedTacticalSavesWarning)
                        {
                           

                            string warning = TFTVCommonMethods.ConvertKeyToString("KEY_OPTIONS_TACTICAL_SAVING_WARNING");// $"Saving and loading on Tactical can result in odd behavior and bugs (Vanilla issues). It is recommended to save only on Geoscape (and use several saves, in case one of them gets corrupted). And, you know what... losing soldiers in TFTV is fun :)";

                            GameUtl.GetMessageBox().ShowSimplePrompt(warning, MessageBoxIcon.Warning, MessageBoxButtons.OK, null);

                            ShowedTacticalSavesWarning = true;
                        }*/

                        bool option = newValue == 0;


                        string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                        config.DisableTacSaves = option;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnDiploPenaltiesValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                        bool option = newValue == 0;
                        string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                        TFTVNewGameOptions.DiplomaticPenaltiesSetting = option;
                        _diploPenalties = arrowPickerController;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnStaminaDrainValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                        bool option = newValue == 0;
                        string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                        TFTVNewGameOptions.StaminaPenaltyFromInjurySetting = option;
                        _staminaDrain = arrowPickerController;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnHarderAmbushValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                        bool option = newValue == 0;
                        string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                        TFTVNewGameOptions.MoreAmbushesSetting = option;
                        _harderAmbush = arrowPickerController;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnStaminaRecuperationValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                        bool option = newValue == 0;
                        string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                        config.StaminaRecuperation = option;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnHavenSOSValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                        bool option = newValue == 0;
                        string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                        config.HavenSOS = option;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnLearnFirstSchoolValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                        bool option = newValue == 0;
                        string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                        config.LearnFirstSkill = option;


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnStrongerPandoransValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                       
                        bool option = newValue == 0;
                        string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                        TFTVNewGameOptions.StrongerPandoransSetting = option;
                        _strongerPandorans = arrowPickerController;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnEtermesVulnerabilityResistanceValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {

                        string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                        TFTVNewGameOptions.EtermesResistanceAndVulnerability = newValue + 1;
                        _etermesVulnerability = arrowPickerController;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnImpossibleWeaponsValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                        bool option = newValue == 0;
                        string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                        TFTVNewGameOptions.ImpossibleWeaponsAdjustmentsSetting = option;
                        _imposssibleWeapons = arrowPickerController;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }


                private static void OnNoSecondChancesValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                        bool option = newValue == 0;
                        string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                        TFTVNewGameOptions.NoSecondChances = option;
                        _noSecondChances = arrowPickerController;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }


                private static void OnSkipMoviesValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                        bool option = newValue == 0;
                        string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                        config.SkipMovies = option;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnNoBarksValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                        bool option = newValue == 0;
                        string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                        config.NoBarks = option;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }



                private static void OnFlinchingValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                        bool option = newValue == 0;
                        string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                        config.Flinching = option;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnMoreMistValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                        bool option = newValue == 0;
                        string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                        config.MoreMistVO = option;

                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnLimitedDeploymentValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                        bool option = newValue == 0;
                        string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                        config.LimitedDeploymentVO = option;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnShowAmbushExfilValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                        bool option = newValue == 0;
                        string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                        config.ShowAmbushExfil = option;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnSkipFSTutorialValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                        bool option = newValue == 0;
                        string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                        config.SkipFSTutorial = option;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnCustomPortraitsValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                        bool option = newValue == 0;
                        string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                        config.CustomPortraits = option;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnHandGrenadeScatterValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                        bool option = newValue == 0;
                        string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                        config.HandGrenadeScatter = option;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnEquipBeforeAmbushValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                        bool option = newValue == 0;
                        string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                        config.EquipBeforeAmbush = option;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnTFTVSuppressionValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                        bool option = newValue == 0;
                        string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                        config.TFTVSuppression = option;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnTradingValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                        bool option = newValue == 0;
                        string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                        config.Trading = option;


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnLimitedRaidingValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {

                    try
                    {
                        bool option = newValue == 0;
                        string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                        config.LimitedRaiding = option;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnNoDropValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {

                    try
                    {
                        bool option = newValue == 0;
                        string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                        config.NoDropReinforcements = option;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnTacticalDifficultyValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {

                        string[] options = new string[_optionsTacticalDifficulty.Length];

                        for (int i = 0; i < _optionsTacticalDifficulty.Length; i++)
                        {
                            LocalizedTextBind optionTextBindKey = new LocalizedTextBind() { LocalizationKey = _optionsTacticalDifficulty[i] };
                            options[i] = optionTextBindKey.Localize();

                        }

                        arrowPickerController.CurrentItemText.text = options[newValue];
                        config.TacticalDifficulty = (TFTVConfig.DifficultyOnTactical)newValue;
                        //TFTVLogger.Always($"new difficulty on tactical showing in config: {config.difficultyOnTactical}");

                        TFTVConfig.DifficultyOnTactical difficulty = (TFTVConfig.DifficultyOnTactical)newValue;


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }


                }

                private static void OnStartingFactionValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                        string[] options = new string[_optionsStartingFaction.Length];

                        for (int i = 0; i < _optionsStartingFaction.Length; i++)
                        {
                            LocalizedTextBind optionTextBindKey = new LocalizedTextBind() { LocalizationKey = _optionsStartingFaction[i] };
                            options[i] = optionTextBindKey.Localize();

                        }

                        arrowPickerController.CurrentItemText.text = options[newValue];
                        TFTVNewGameOptions.startingSquad = (TFTVNewGameOptions.StartingSquadFaction)newValue;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }


                }

                private static void OnStartingSquadValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                        string[] options = new string[_optionsStartingSquad.Length];

                        for (int i = 0; i < _optionsStartingSquad.Length; i++)
                        {
                            LocalizedTextBind optionTextBindKey = new LocalizedTextBind() { LocalizationKey = _optionsStartingSquad[i] };
                            options[i] = optionTextBindKey.Localize();

                        }

                        arrowPickerController.CurrentItemText.text = options[newValue];
                        TFTVNewGameOptions.startingSquadCharacters = (TFTVNewGameOptions.StartingSquadCharacters)newValue;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnStartingBaseValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                        string[] options = new string[_optionsStartingBase.Length];

                        for (int i = 0; i < _optionsStartingBase.Length; i++)
                        {
                            LocalizedTextBind optionTextBindKey = new LocalizedTextBind() { LocalizationKey = _optionsStartingBase[i] };
                            options[i] = optionTextBindKey.Localize();

                        }


                        arrowPickerController.CurrentItemText.text = options[newValue];
                        TFTVNewGameOptions.startingBaseLocation = (TFTVNewGameOptions.StartingBaseLocation)newValue;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnStartingScavSitesValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                        arrowPickerController.CurrentItemText.text = _optionsScavSites[newValue];
                        TFTVNewGameOptions.initialScavSites = newValue;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnResScavPriorityValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {

                        string[] options = { new LocalizedTextBind() { LocalizationKey = "HIGH" }.Localize(), new LocalizedTextBind() { LocalizationKey = "MEDIUM" }.Localize(),
                    new LocalizedTextBind() { LocalizationKey = "LOW" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NONE" }.Localize() };
                        //  string[] options = { "HIGH", "MEDIUM", "LOW", "NONE" };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                        TFTVNewGameOptions.chancesScavCrates = (TFTVNewGameOptions.ScavengingWeight)newValue;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnRecruitsPriorityValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                        string[] options = { new LocalizedTextBind() { LocalizationKey = "HIGH" }.Localize(), new LocalizedTextBind() { LocalizationKey = "MEDIUM" }.Localize(),
                    new LocalizedTextBind() { LocalizationKey = "LOW" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NONE" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                        TFTVNewGameOptions.chancesScavSoldiers = (TFTVNewGameOptions.ScavengingWeight)newValue;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnVehiclePriorityValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                        string[] options = { new LocalizedTextBind() { LocalizationKey = "HIGH" }.Localize(), new LocalizedTextBind() { LocalizationKey = "MEDIUM" }.Localize(), new LocalizedTextBind() { LocalizationKey = "LOW" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NONE" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                        TFTVNewGameOptions.chancesScavGroundVehicleRescue = (TFTVNewGameOptions.ScavengingWeight)newValue;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnLimitedCaptureValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                       
                        bool limitedCapture = newValue == 0;
                        string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                        TFTVNewGameOptions.LimitedCaptureSetting = limitedCapture;
                        _limitedCapturing = arrowPickerController;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void OnLimitedHarvestingValueChangedCallback(int newValue, ArrowPickerController arrowPickerController)
                {
                    try
                    {
                      
                        bool limitedHarvesting = newValue == 0;
                        string[] options = { new LocalizedTextBind() { LocalizationKey = "YES" }.Localize(), new LocalizedTextBind() { LocalizationKey = "NO" }.Localize() };
                        arrowPickerController.CurrentItemText.text = options[newValue];
                        TFTVNewGameOptions.LimitedHarvestingSetting = limitedHarvesting;
                        _limitedHarvesting = arrowPickerController;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
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

        [HarmonyPatch(typeof(UIStateNewGeoscapeGameSettings), "EnterState")] //VERIFIED
        public static class UIStateNewGeoscapeGameSettings_EnterState_Patch
        {
            private static void Prefix(UIStateNewGeoscapeGameSettings __instance)
            {
                try
                {
                    _eldritchWarningInProgress = false;
                    _eldritchWarningAccepted = false;
                    _suppressNextOnConfirm = false;

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

                            /*  UITooltipText uITooltipText = newController.gameObject.GetComponent<UITooltipText>();

                              if (uITooltipText == null)
                              {
                                  uITooltipText = newController.gameObject.AddComponent<UITooltipText>();
                              }

                              uITooltipText.TipKey = new LocalizedTextBind($"Testing for {x+1} difficulty", true);*/
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

        [HarmonyPatch(typeof(UIStateNewGeoscapeGameSettings), "BindSecondaryOptions")] //VERIFIED
        internal static class UIStateNewGeoscapeGameSettings_BindSecondaryOptions_patch
        {
            private static void Postfix(UIStateNewGeoscapeGameSettings __instance, GameOptionViewController element)
            {
                try
                {
                    //  TFTVLogger.Always($"Element is {element.OptionText.text}");
                    //   GameOptionViewController viewController = new GameOptionViewController() { };

                    if (element.OptionText.text == TFTVCommonMethods.ConvertKeyToString("KEY_SELECT_TUTORIAL"))//"PLAY PROLOGUE AND TUTORIAL")
                    {
                        //  element.OptionText.text = "START WITH VANILLA TUTORIAL SQUAD";                      
                    }
                    else
                    {

                        element.CheckedToggle.isOn = false;

                        UITooltipText uITooltipText = element.CheckedToggle.gameObject.GetComponent<UITooltipText>();

                        if (uITooltipText == null)
                        {
                            uITooltipText = element.CheckedToggle.gameObject.AddComponent<UITooltipText>();
                        }

                        uITooltipText.TipKey = new LocalizedTextBind(TFTVCommonMethods.ConvertKeyToString("TFTV_PROMO_SKINS"), true);
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

        [HarmonyPatch(typeof(UIModuleGameSettings), nameof(UIModuleGameSettings.GetActivatedEntitlements))] 
        public static class UIModuleGameSettings_GetActivatedEntitlements_Experiment_Patch
        {
            public static bool Prefix(UIModuleGameSettings __instance, ref List<EntitlementDef> __result, List<GameAdditionalContentEntry> ____gameEntitlementContentEntries)
            {
                try
                {
                    // TFTVLogger.Always("Patch running");

                    List<EntitlementDef> allEntitlmentDefs = new List<EntitlementDef>()
                    {
                    DefCache.GetDef<EntitlementDef>("BloodAndTitaniumEntitlementDef"),
                    DefCache.GetDef<EntitlementDef>("CorruptedHorizonsEntitlementDef"),
                    DefCache.GetDef<EntitlementDef>("FesteringSkiesEntitlementDef"),
                    DefCache.GetDef<EntitlementDef>("KaosEnginesEntitlementDef"),
                    DefCache.GetDef<EntitlementDef>("LegacyOfTheAncientsEntitlementDef"),
                    DefCache.GetDef<EntitlementDef>("LivingWeaponsEntitlementDef"),
                 //   DefCache.GetDef<EntitlementDef>("YOE_YearOneEditionEntitlementDef")
                    };

                    PlatformEntitlement platformEntitlements = TFTVMain.Main.GetGame().Platform.GetPlatformEntitlement();


                    List<EntitlementDef> entitlementsUserHas = new List<EntitlementDef>();
                    List<EntitlementDef> dlcsMissing = new List<EntitlementDef>();

                    foreach (EntitlementDef entitlementDef in allEntitlmentDefs)
                    {
                        if (platformEntitlements.IsUserEntitledFor(entitlementDef))
                        {
                            entitlementsUserHas.Add(entitlementDef);
                        }
                        else
                        {
                            TFTVLogger.Always($"user not entitled to {entitlementDef.name}");
                            if (entitlementDef != DefCache.GetDef<EntitlementDef>("LivingWeaponsEntitlementDef"))
                            {
                                dlcsMissing.Add(entitlementDef);
                            }
                        }
                    }

                    if (dlcsMissing.Count > 0)
                    {
                        IEnumerable<string> values = dlcsMissing.Select((EntitlementDef d) => d.Name.Localize());
                        string content = string.Format(TFTVCommonMethods.ConvertKeyToString("KEY_MISSING_DLC_TFTV"), string.Join(", ", values));

                        GameUtl.GetMessageBox().ShowSimplePrompt(content, MessageBoxIcon.Error, MessageBoxButtons.OK, OnDLCRequiredResult);

                        void OnDLCRequiredResult(MessageBoxCallbackResult res)
                        {
                            /* if (res.DialogResult == MessageBoxResult.OK || res.DialogResult == MessageBoxResult.Yes)
                             {
                                 platformEntitlements.OpenEntitlementInfo(dlcsMissing[0]);
                             }*/
                        }

                        return false;
                    }

                    __result = entitlementsUserHas;

                    TFTVCommonMethods.ClearInternalVariablesOnStateChangeAndLoad();
                    TFTVNewGameOptions.ConfigImplemented = true;
                    TFTVNewGameOptions.NewTrainingFacilities = true;
                    ShowedTacticalSavesWarning = false;

                    return false;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIModuleGameSettings), nameof(UIModuleGameSettings.GetDeactivatedEntitlements))]
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

        [HarmonyPatch(typeof(UIStateNewGeoscapeGameSettings), "GameSettings_OnConfirm")] //VERIFIED
        public static class UIStateNewGeoscapeGameSettings_GameSettings_OnConfirm_patch
        {
            public static bool Prefix(UIStateNewGeoscapeGameSettings __instance)
            {
                try
                {
                    // If we're re-invoking confirm after accepting warnings, let it proceed.
                    if (_suppressNextOnConfirm)
                    {
                        _suppressNextOnConfirm = false;
                        return true;
                    }

                    // Only gate when pressing Start Game AND hardest difficulty is selected.
                    if (IsHardestDifficultySelected() && !_eldritchWarningAccepted)
                    {
                        StartEldritchWarningsThenConfirm(__instance);
                        return false; // block starting until warnings accepted
                    }

                    TFTVStarts.RevertIntroToNormalStart();
                    TFTVLogger.Always($"selected option: {GameUtl.CurrentLevel().GetComponent<HomeScreenView>().HomeScreenModules.GameSettings?.MainOptions?.Selected?.First()?.OptionIndex}");
                    EnterStateRun = false;

                    return true;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(UIStateNewGeoscapeGameSettings), "ExitState")] //VERIFIED
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

