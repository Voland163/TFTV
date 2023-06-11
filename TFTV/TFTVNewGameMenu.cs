using Base;
using Base.Core;
using Base.Defs;
using Base.Platforms;
using HarmonyLib;
using I2.Loc;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Levels.Params;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Home.View;
using PhoenixPoint.Home.View.ViewControllers;
using PhoenixPoint.Home.View.ViewModules;
using PhoenixPoint.Home.View.ViewStates;
using System;
using System.CodeDom;
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
        }*/


        public static HomeScreenViewContext HomeScreenViewContext = null;

        [HarmonyPatch(typeof(HomeScreenView), "InitView")]
        public static class HomeScreenView_InitView_Patch
        {
            private static void Postfix(HomeScreenView __instance, HomeScreenViewContext ____context)
            {
                try
                {
                    HomeScreenViewContext = ____context;

                  //  TFTVLogger.Always($"Testing");

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        /*   public static UIModuleGameSettings gameSettings = null;

           [HarmonyPatch(typeof(UIModuleGameSettings), "Awake")]
           public static class UIModuleGameSettings_Awake_Patch
           {
               private static void Postfix(UIModuleGameSettings __instance)
               {
                   try
                   {

                       gameSettings = __instance;


                       TFTVLogger.Always("Awake run");

                   }
                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                   }
               }
           }*/

        /*  private class OptionEntry
          {
              public LocalizedTextBind Text;

              public LocalizedTextBind Description;

              public bool DefaultChecked;
          }*/

        //   private GeoscapeGameModeDef _gameModeDef;


     /*   [HarmonyPatch(typeof(ArrowPickerController), "Init")]
        internal static class ArrowPickerController_Init_patch
        {
            private static void Prefix(ArrowPickerController __instance, int valueRange, int currentValue, Action<int> onValueChanged)
            {
                try
                {
                    if (__instance == null)
                    {
                        TFTVLogger.Always($"Instance null, but valueRange is {valueRange} and currentValue is {currentValue}");

                    }
                    else
                    {
                       

                        TFTVLogger.Always($"{__instance.name} initiated");

                    }



                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }


        [HarmonyPatch(typeof(ArrowPickerController), "SetEnabled")]
        internal static class ArrowPickerController_SetEnabled_patch
        {
            private static void Postfix(ArrowPickerController __instance, bool enabled)
            {
                try
                {
                    TFTVLogger.Always($"Enabled is {enabled}");

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }*/



    /*    private static void OnDropdownValueChanged(int value)
        {

            // Perform desired actions based on the selected option
        }



        [HarmonyPatch(typeof(UIModuleGameSettings), "InitFullContent")]
        internal static class UIStateNewGeoscapeGameSettings_InitFullContent_patch
        {
            private static void Postfix(UIModuleGameSettings __instance)
            {
                try
                {



                    RectTransform rectTransform = __instance.GameAddiotionalContentGroup.GetComponentInChildren<RectTransform>();
                    GameObject parentGameObject = rectTransform.gameObject;

                    TFTVLogger.Always($"{parentGameObject.layer}");


                    __instance.GameAddiotionalContentGroup.GetComponentInChildren<RectTransform>();


                    //RectTransform dlcButton1 = (RectTransform)rectTransform.GetChildren().Where(rt => rt.name.Equals("DLC_Button")).FirstOrDefault();



                    //  dlcButton1.DetachChildren();

                    GameObject gameObject = new GameObject("ObjectForArrowPicker");
                   // gameObject.AddComponent<Renderer>().enabled = true;

                    RectTransform gameObjectRectTransform = gameObject.AddComponent<RectTransform>();
                    ArrowPickerController arrowPicker = gameObject.AddComponent<ArrowPickerController>();
                    arrowPicker.gameObject.SetActive(true);
                    arrowPicker.transform.SetParent(gameObjectRectTransform, false);

                    GameObject previousArrowObject = new GameObject("PreviousArrow");
                    GameObject nextArrowObject = new GameObject("NextArrow");
                    GameObject centralButtonObject = new GameObject("CentralButton");
                    GameObject textObject = new GameObject("TextBox");
                    GameObject localizeObject = new GameObject("Localize");

                    Text textBox = textObject.AddComponent<Text>();
                    Localize localizeThingy = localizeObject.AddComponent<Localize>();


                    // Add necessary components to the button objects (e.g., Image, Button, etc.)
                    Image previousArrowImage = previousArrowObject.AddComponent<Image>();
                    PhoenixGeneralButton previousArrowButton = previousArrowObject.AddComponent<PhoenixGeneralButton>();
                    Image nextArrowImage = nextArrowObject.AddComponent<Image>();
                    PhoenixGeneralButton nextArrowButton = nextArrowObject.AddComponent<PhoenixGeneralButton>();

                    Image centralButtonImage = centralButtonObject.AddComponent<Image>();
                    PhoenixGeneralButton centralButton = centralButtonObject.AddComponent<PhoenixGeneralButton>();

                    // Set the references in the arrowPicker
                    arrowPicker.PreviousArrow = previousArrowButton;
                    arrowPicker.NextArrow = nextArrowButton;
                    arrowPicker.CentralButton = centralButton;
                    arrowPicker.CurrentItem = localizeThingy;
                    arrowPicker.CurrentItemText = textBox;

                    // Activate the button objects
                    previousArrowButton.gameObject.SetActive(true);
                    nextArrowButton.gameObject.SetActive(true);
                    centralButton.gameObject.SetActive(true);
                    textBox.gameObject.SetActive(true);
                    localizeThingy.gameObject.SetActive(true);

                    previousArrowObject.transform.SetParent(gameObjectRectTransform, false);
                    previousArrowButton.transform.Translate(-200, 0, 0);
                    nextArrowObject.transform.SetParent(gameObjectRectTransform, false);
                    nextArrowButton.transform.Translate(200, 0, 0);
                    centralButtonObject.transform.SetParent(gameObjectRectTransform, false);
                    textBox.transform.SetParent(gameObjectRectTransform, false);
                    localizeThingy.transform.SetParent(gameObjectRectTransform, false);

                   

                    int valueRange = 10; // Example value range
                    int currentValue = 0; // Example current value
                    System.Action<int> onValueChanged = (index) =>
                    {
                        Debug.Log($"New value is {index}");
                    };


                    arrowPicker.Init(valueRange, currentValue, onValueChanged);

                    // Set the parent transform of the UI elements
                    gameObjectRectTransform.transform.SetParent(rectTransform, false);

                    gameObjectRectTransform.anchorMin = new Vector2(0, 0); // Adjust as needed
                    gameObjectRectTransform.anchorMax = new Vector2(1, 1); // Adjust as needed
                    gameObjectRectTransform.pivot = new Vector2(0.5f, 0.5f); // Adjust as needed
                    gameObjectRectTransform.anchoredPosition = new Vector2(0, 0); // Adjust as needed
                    gameObjectRectTransform.sizeDelta = new Vector2(1000, 500);

                    rectTransform.gameObject.SetChildrenVisibility(true);

                   

                    foreach (Component component in arrowPicker.GetComponentsInChildren<Transform>()) 
                    {
                        TFTVLogger.Always($"{component.name} {component?.gameObject?.layer}");
                        component.gameObject.SetActive(true);
                        

                    }

                    

                    

                    Renderer[] componentsInChildren = arrowPicker.GetComponentsInChildren<Renderer>(includeInactive: true);
                    for (int i = 0; i < componentsInChildren.Length; i++)
                    {
                        componentsInChildren[i].enabled = true;
                    }


                    TFTVLogger.Always($"RectTransform {rectTransform?.name}. it has {rectTransform.childCount} children");

                    foreach (RectTransform rTransform in rectTransform.GetComponentsInChildren<RectTransform>().ToList())
                    {

                        TFTVLogger.Always($"Found {rTransform.name}. It has {rTransform.childCount} children. size x {rTransform.rect.size.x} size y {rTransform.rect.size.y}. Layer is {rTransform.gameObject.layer}");
                        //   rTransform.DetachChildren();

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
                         gameOptionViewController.Set(new Base.UI.LocalizedTextBind("Testing"), new Base.UI.LocalizedTextBind("Testing Testing"), false);
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

