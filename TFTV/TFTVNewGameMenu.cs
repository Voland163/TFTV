using Base;
using Base.Core;
using Base.Defs;
using Base.Platforms;
using Epic.OnlineServices.Platform;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Levels.Params;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Home.View;
using PhoenixPoint.Home.View.ViewControllers;
using PhoenixPoint.Home.View.ViewModules;
using PhoenixPoint.Home.View.ViewStates;
using PhoenixPoint.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV
{
    internal class TFTVNewGameMenu
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        //You will need to edit scene hierarchy to add new objects under GameSettingsModule, it has a UIModuleGameSettings script
        //Class UIStateNewGeoscapeGameSettings is responsible for accepting selected settings and start the game, so you'll have to dig inside
        //for changing behaviour.





        //This is for new game start screen. Commented out for the moment.
      /*  [HarmonyPatch(typeof(GameOptionViewController), "OnClicked")]
        public static class OptionListViewController_Element_PointerExit_Patch
        {
            private static void Postfix(GameOptionViewController __instance)
            {
                try
                {
                    //  TFTVLogger.Always($"Element is: {__instance.Description.Localize()}");


                    if (__instance.name.Contains("TFTVDifficulty_RadioButton"))
                    {

                        TFTVLogger.Always($"Element is: {__instance.Description.Localize()}");

                        HomeScreenViewContext context = HomeScreenViewContext;
                        UIModuleGameSettings gameSettings = context.View.HomeScreenModules.GameSettings;

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
      */

        public static HomeScreenViewContext HomeScreenViewContext = null;
        public static ModSettingController ModSettingControllerHook = null;

        [HarmonyPatch(typeof(HomeScreenView), "InitView")]
        public static class HomeScreenView_InitView_Patch
        {
            private static void Postfix(HomeScreenView __instance, HomeScreenViewContext ____context)
            {
                try
                {
                    HomeScreenViewContext = ____context;
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

/*
        [HarmonyPatch(typeof(UIModuleGameSettings), "InitFullContent")]
        internal static class UIStateNewGeoscapeGameSettings_InitFullContent_patch
        {

            private static ArrowPickerController _startingFaction = null;
            private static ArrowPickerController _startingBase = null;
            private static ArrowPickerController _startingSquad = null;
            private static ArrowPickerController _startingScavSites = null;
            private static ArrowPickerController _resCratePriority = null;
            private static ArrowPickerController _recruitsPriority = null;
            private static ArrowPickerController _vehiclePriority = null;

            private static string _titleResCratePriority = "RESOURCE CRATE PRIORITY";
            private static string _titleRecruitsPriority = "RECRUITS PRIORITY";
            private static string _titleVehiclePriority = "VEHICLE PRIORITY";
            private static string _descriptionScavPriority = "In Vanilla and default TFTV, resource crate scavenging sites are much more likely to spawn than either vehicle or personnel rescues. " +
                "You can modify the relative chances of each type of scavenging site being generated. Choose none to have 0 scavenging sites of this type (for reference, high/medium/low ratio is 6/4/1)";
            private static string [] _optionsResCratePriority = {"HIGH", "MEDIUM", "LOW", "NONE"};
            private static string[] _optionsRecruitsPriority = { "HIGH", "MEDIUM", "LOW", "NONE" };
            private static string[] _optionsVehiclePriority = { "HIGH", "MEDIUM", "LOW", "NONE" };

            private static string _titleScavSites = "SCAVENGING SITES #";
            private static string _descriptionScavSites = "Total number of scavenging sites generated on game start, not counting overgrown sites. (Vanilla: 16, TFTV default 8, because Ambushes generate additional resources).";
            private static string [] _optionsScavSites = {"0","1","2","3","4","5","6","7","8","9","10","11","12","13","14","15","16","17","18","19","20","21","22","23","24","25","26","27","28","29","30","31","32"} ;


            private static string _titleStartingFaction = "FACTION BACKGROUND";
            private static string _descriptionStartingFaction = "You can choose a different faction background. " +
                        "If you do, one of your Assaults and your starting Heavy on Legend and Hero," +
                        "Assault on Veteran, or Sniper on Rookie will be replaced by an operative of the elite class of the Faction of your choice. " +
                        "You will also get the corresponding faction technology once the faction researches it.";
            private static string[] _optionsStartingFaction = { "PHOENIX PROJECT", "DISCIPLES OF ANU", "NEW JERICHO", "SYNEDRION" };

            private static string _titleStartingBase = "STARTING BASE LOCATION";
            private static string _descriptionStartingBase = "Select your starting base. You can choose a specific location to start from. Please note that some locations are harder to start from than others!";
            private static string[] _optionsStartingBase = {"Vanilla Random (remote bases excluded)",
                  "Random (ALL bases included)",
                  "Antarctica",
                  "Asia (China)",
                  "Australia",
                  "Central America (Honduras)",
                  "Central America (Mexico)",
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
                  "West Africa (Ghana)" };

            private static string _titleStartingSquad = "STARTING SQUAD";
            private static string _descriptionStartingSquad = "You can choose to get a completely random squad (as in Vanilla without doing the tutorial), " +
              "the Vanilla tutorial starting squad (with higher stats), " +
              "or a squad that will include Sophia Brown and Jacob with unbuffed stats (default on TFTV). " +
              "Note that Jacob is a sniper, as in the title screen :)";
            private static string[] _optionsStartingSquad = { "UNBUFFED", "BUFFED", "RANDOM" };

            private static void InstantiateArrowPickerController(ModSettingController modSettingController, 
                ArrowPickerController arrowPickerController, string title, string description, string[] options, int currentValue, Action<int> onValueChanged)
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
 
                    arrowPickerController.Init(options.Length, currentValue, onValueChanged);
                   
                    arrowPickerController.SetEnabled(true);
                  
                    arrowPickerController.CurrentItemText.text = options[currentValue];
       
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

                   // __instance.DlcTitleRoot.GetComponent<Text>().text = "";

                    ModSettingController startingFactionController = UnityEngine.Object.Instantiate(ModSettingControllerHook, rectTransform);
                    ModSettingController startingBaseController = UnityEngine.Object.Instantiate(startingFactionController, rectTransform);
                    ModSettingController startingSquadController = UnityEngine.Object.Instantiate(startingBaseController, rectTransform);
                    ModSettingController startingScavSitesController = UnityEngine.Object.Instantiate(startingSquadController, rectTransform);
                    ModSettingController crateScavSitesController = UnityEngine.Object.Instantiate(startingScavSitesController, rectTransform);
                    ModSettingController recruitScavSitesController = UnityEngine.Object.Instantiate(ModSettingControllerHook, rectTransform);
                    ModSettingController vehicleScavSitesController = UnityEngine.Object.Instantiate(ModSettingControllerHook, rectTransform);

                    _startingFaction = startingFactionController.ListField;
                    _startingBase = startingBaseController.ListField;
                    _startingSquad = startingSquadController.ListField;
                    _startingScavSites = startingScavSitesController.ListField;
                    _resCratePriority = crateScavSitesController.ListField;
                    _recruitsPriority = recruitScavSitesController.ListField;
                    _vehiclePriority = vehicleScavSitesController.ListField;

                    InstantiateArrowPickerController(startingFactionController, _startingFaction, _titleStartingFaction, _descriptionStartingFaction, _optionsStartingFaction, 0, OnStartingFactionValueChangedCallback);
                    InstantiateArrowPickerController(startingBaseController, _startingBase, _titleStartingBase, _descriptionStartingBase, _optionsStartingBase, 0, OnStartingBaseValueChangedCallback);
                    InstantiateArrowPickerController(startingSquadController, _startingSquad, _titleStartingSquad, _descriptionStartingSquad, _optionsStartingSquad, 0, OnStartingSquadValueChangedCallback);
                    InstantiateArrowPickerController(startingScavSitesController, _startingScavSites, _titleScavSites, _descriptionScavSites, _optionsScavSites, 8, OnStartingScavSitesValueChangedCallback);
                    InstantiateArrowPickerController(crateScavSitesController, _resCratePriority, _titleResCratePriority, _descriptionScavPriority, _optionsResCratePriority, 0, OnResScavPriorityValueChangedCallback);
                    InstantiateArrowPickerController(recruitScavSitesController, _recruitsPriority, _titleRecruitsPriority, _descriptionScavPriority, _optionsRecruitsPriority, 2, OnRecruitsPriorityValueChangedCallback);
                    InstantiateArrowPickerController(vehicleScavSitesController, _vehiclePriority, _titleVehiclePriority, _descriptionScavPriority, _optionsVehiclePriority, 2, OnVehiclePriorityValueChangedCallback);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void OnStartingFactionValueChangedCallback(int newValue)
            {        

                _startingFaction.CurrentItemText.text = _optionsStartingFaction[newValue];
            }

            private static void OnStartingSquadValueChangedCallback(int newValue)
            {
            
                _startingSquad.CurrentItemText.text = _optionsStartingSquad[newValue];
            }

            private static void OnStartingBaseValueChangedCallback(int newValue)
            {
                _startingBase.CurrentItemText.text = _optionsStartingBase[newValue];
            }

            private static void OnStartingScavSitesValueChangedCallback(int newValue)
            {

                _startingScavSites.CurrentItemText.text = _optionsScavSites[newValue];
            }

            private static void OnResScavPriorityValueChangedCallback(int newValue)
            {
                string[] options = { "HIGH", "MEDIUM", "LOW", "NONE"};
                _resCratePriority.CurrentItemText.text = options[newValue];
            }

            private static void OnRecruitsPriorityValueChangedCallback(int newValue)
            {
                string[] options = { "HIGH", "MEDIUM", "LOW", "NONE" };
                _recruitsPriority.CurrentItemText.text = options[newValue];
            }

            private static void OnVehiclePriorityValueChangedCallback(int newValue)
            {
                string[] options = { "HIGH", "MEDIUM", "LOW", "NONE" };
                _vehiclePriority.CurrentItemText.text = options[newValue];
            }


            private static void PopulateOptions(ArrowPickerController arrowPickerController, string[] options)
            {
                for (int i = 0; i < options.Length; i++)
                {
                   
                    arrowPickerController.CurrentItemText.text = options[i];

                    MethodInfo OnNewValue = arrowPickerController.GetType().GetMethod("OnNewValue", BindingFlags.NonPublic | BindingFlags.Instance);
                  
                    OnNewValue.Invoke(arrowPickerController, null);
                }
            }
        }



        */



        public static bool EnterStateRun = false;

    /*    [HarmonyPatch(typeof(UIStateNewGeoscapeGameSettings), "EnterState")]
        public static class UIStateNewGeoscapeGameSettings_EnterState_Patch
        {
            private static void Prefix(UIStateNewGeoscapeGameSettings __instance)
            {
                try
                {
                    if (!EnterStateRun)
                    {

                        GameDifficultyLevelDef[] difficultyLevels = GameUtl.GameComponent<SharedData>().DifficultyLevels;



                        HomeScreenViewContext context = HomeScreenViewContext;

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

                        GameOptionViewController[] componentsInChildren = gameSettings.MainOptions.Container.GetComponentsInChildren<GameOptionViewController>();

                        MethodInfo method = typeof(PhoenixGeneralButton).GetMethod("SetNormalState", BindingFlags.NonPublic | BindingFlags.Instance);
                        MethodInfo methodAnim = typeof(PhoenixGeneralButton).GetMethod("SetAnimationState", BindingFlags.NonPublic | BindingFlags.Instance);

                        foreach (GameOptionViewController component in componentsInChildren)
                        {
                            TFTVLogger.Always($"{component.name}");

                            component.SelectButton.IsSelected = false;
                            method.Invoke(component.SelectButton, null);
                            methodAnim.Invoke(component.SelectButton, new object[] { "HighlightedStateParameter", false });
                        }

                        //  gameSettings.MainOptions.IsMultiselectable = false;
                        //  GameOptionViewController gameOptionViewController = gameSettings.MainOptions.Container.GetComponentInChildren<GameOptionViewController>();
                        //  gameSettings.MainOptions.GameOptionPrefab = gameOptionViewController;

                        gameSettings.MainOptions.Container.gameObject.transform.localScale = new Vector3(0.90f, 0.90f, 0.90f);
                        gameSettings.MainOptions.Container.gameObject.transform.Translate(new Vector3(0f, 20f, 0f));

                        // TFTVLogger.Always($"There are {difficultyLevels.Length} difficulty level. Number of elements is {gameSettings.MainOptions.Elements.Count}");
                        TFTVLogger.Always($"Enter state invoked");
                        EnterStateRun = true;
                    }



                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        */
        //  public static bool BindSecondaryOptionRun = false;

        [HarmonyPatch(typeof(UIStateNewGeoscapeGameSettings), "BindSecondaryOptions")]
        internal static class UIStateNewGeoscapeGameSettings_BindSecondaryOptions_patch
        {
            private static void Postfix(UIStateNewGeoscapeGameSettings __instance, GameOptionViewController element)
            {
                try
                {
                    TFTVLogger.Always($"Element is {element.OptionText.text}");
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

        [HarmonyPatch(typeof(UIStateNewGeoscapeGameSettings), "CreateSceneBinding")]
        public static class UIStateNewGeoscapeGameSettings_GameSettings_OnConfirm_patch
        {
            public static void Prefix(GeoscapeGameParams gameParams)
            {
                try
                {
                    if (gameParams.TutorialEnabled)
                    {
                        gameParams.TutorialEnabled = true;
                        TFTVConfig config = TFTVMain.Main.Config;
                        config.tutorialCharacters = TFTVConfig.StartingSquadCharacters.BUFFED;
                    }
                    HomeScreenViewContext context = HomeScreenViewContext;
                    UIModuleGameSettings gameSettings = context.View.HomeScreenModules.GameSettings;
                    List<EntitlementDef> entitlementDefs = new List<EntitlementDef>();
                    entitlementDefs.AddRange(gameSettings.GetActivatedEntitlements());
                    entitlementDefs.AddRange(gameSettings.GetDeactivatedEntitlements());
                    gameParams.EnabledDlc = entitlementDefs.ToArray();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


    }
}

