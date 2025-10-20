using Base;
using Base.Cameras;
using Base.Core;
using Base.Defs;
using Base.Entities.Statuses;
using Base.UI;
using HarmonyLib;
using hoenixPoint.Tactical.View.ViewControllers;
using I2.Loc;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using PhoenixPoint.Tactical.View;
using PhoenixPoint.Tactical.View.ViewControllers;
using PhoenixPoint.Tactical.View.ViewModules;
using PhoenixPoint.Tactical.View.ViewStates;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static TFTV.TFTVCapturePandorans;
using static TFTV.TFTVDrills.DrillsPublicClasses;
using static TFTV.TFTVUITactical.TFTVTacticalObjectives;
using static UITooltip;
using Component = UnityEngine.Component;
using Image = UnityEngine.UI.Image;
using Text = UnityEngine.UI.Text;

namespace TFTV
{
    internal class TFTVUITactical
    {


        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;

        public static Font PuristaSemiboldFontCache = null;
        public static Color LeaderColor = new Color(1f, 0.72f, 0f);
        public static Color NegativeColor = new Color(1.0f, 0.145f, 0.286f);
        private static Color _regularNoLOSColor = Color.gray;
        public static Color WhiteColor = new Color(0.820f, 0.859f, 0.914f);
        public static Color VoidColor = new Color(0.525f, 0.243f, 0.937f);

        public static void CachePuristaSemiboldFont(UIModuleObjectives uIModuleObjectives)
        {
            try
            {
                // TFTVLogger.Always($"uIModuleObjectives.transform.Find(\"Objectives_Text\").GetComponentInChildren<Text>().font: {uIModuleObjectives.transform.Find("Objectives_Text").GetComponentInChildren<Text>().font}");

                if (PuristaSemiboldFontCache == null)
                {
                    PuristaSemiboldFontCache = uIModuleObjectives.transform.Find("Objectives_Text").GetComponentInChildren<Text>().font;
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }


        }

        public static void ClearDataOnLoadAndStateChange()
        {
            try
            {
                ODITactical.ClearDataOnLoadAndStateChange();
                CaptureTacticalWidget.ClearData();
                SecondaryObjectivesTactical.ClearDataOnGameLoadAndStateChange();
                Enemies.ClearData();
                Ancients.ClearData();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        public static void ClearDataOnMissionRestart()
        {
            try
            {
                ODITactical.ClearDataOnMissionRestart();
                CaptureTacticalWidget.ClearData();
                Enemies.ClearData();
                Ancients.ClearData();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        internal class Ancients
        {
            private static GameObject _ancientWidget = null;

            internal static void ClearData()
            {
                try
                {
                    _ancientWidget = null;

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


            public class AncientsWidget : MonoBehaviour
            {
                private GameObject ancientsWidgetPrefab;
                private Transform widgetContainer;
                private Text _cyclopsResistance;
                private Image _iconImage;
                private Image _bgImage;
                private Text tooltipText;

                private void AddClickChaseTarget(TacticalActor tacticalActor, GameObject gameObject)
                {
                    try
                    {
                        if (!gameObject.GetComponent<EventTrigger>())
                        {
                            gameObject.AddComponent<EventTrigger>();
                        }

                        EventTrigger eventTrigger = gameObject.GetComponent<EventTrigger>();
                        eventTrigger.triggers.Clear();

                        if (tacticalActor != null)
                        {

                            EventTrigger.Entry click = new EventTrigger.Entry
                            {
                                eventID = EventTriggerType.PointerClick
                            };

                            click.callback.AddListener((eventData) =>
                            {
                                if (tacticalActor.IsRevealedToViewer)
                                {
                                    tacticalActor.TacticalActorView.DoCameraChase(tacticalActor);
                                }

                            });

                            eventTrigger.triggers.Add(click);
                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }


                public void InitializeAncientsWidget(int cyclopsDamageResistance, TacticalActor chaseTarget)
                {
                    try
                    {
                        TFTVLogger.Always($"InitializeAncientsWidget running");

                        Color color = NegativeColor;
                        string widgetObjectName = "AncientsWidget";

                        Resolution resolution = Screen.currentResolution;
                        float resolutionFactorWidth = (float)resolution.width / 1920f;
                        float resolutionFactorHeight = (float)resolution.height / 1080f;

                        // Access UIModuleNavigation and set widgetContainer as its transform
                        TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();
                        UIModuleNavigation uIModuleNavigation = controller.View.TacticalModules.NavigationModule;
                        widgetContainer = uIModuleNavigation.transform;

                        // Dynamically create the leaderWidgetPrefab structure
                        ancientsWidgetPrefab = new GameObject(widgetObjectName);
                        RectTransform rectTransform = ancientsWidgetPrefab.AddComponent<RectTransform>();

                        rectTransform.sizeDelta = new Vector2(410, 180);
                        rectTransform.position = new Vector2(245 * resolutionFactorWidth, 600 * resolutionFactorHeight);

                        GameObject backgroundImage = new GameObject("Background", typeof(RectTransform), typeof(Image));

                        backgroundImage.transform.SetParent(ancientsWidgetPrefab.transform); // Attach to the existing GameObject
                                                                                             // backgroundImage.AddComponent<UITooltipText>().TipText = TFTVCommonMethods.ConvertKeyToString("TFTV_KEY_ANCIENTS_RULESET");

                        RectTransform bgRect = backgroundImage.GetComponent<RectTransform>();
                        bgRect.sizeDelta = new Vector2(rectTransform.sizeDelta.x, rectTransform.sizeDelta.y / 3);
                        bgRect.anchoredPosition = Vector2.zero + new Vector2(0, 37.5f);

                        Image bgImage = backgroundImage.GetComponent<Image>();
                        bgImage.color = new Color(0, 0, 0, 0.5f); // Black with 50% opacity
                        _bgImage = bgImage;

                        // Set up the icon
                        GameObject iconObj = new GameObject("Icon");
                        iconObj.transform.SetParent(ancientsWidgetPrefab.transform);
                        Image iconImage = iconObj.AddComponent<Image>();
                        iconImage.sprite = TFTVAncients.CyclopsDefenseStatus.Visuals.SmallIcon;
                        iconImage.color = color;
                        iconImage.preserveAspect = true;
                        RectTransform iconImageRect = iconImage.GetComponent<RectTransform>();
                        iconImageRect.sizeDelta = new Vector2(30, 30);
                        iconImageRect.anchoredPosition = Vector2.zero + new Vector2(-180, 38);//Vector2.zero + new Vector2(-150, 38);
                        _iconImage = iconImage;

                        // Set up the name text
                        GameObject generatorHealthTextObj = new GameObject("CyclopsResistanceText");
                        generatorHealthTextObj.transform.SetParent(ancientsWidgetPrefab.transform);
                        Text cyclopsResistanceText = generatorHealthTextObj.AddComponent<Text>();
                        cyclopsResistanceText.text = TFTVCommonMethods.ConvertKeyToString("TFTV_KEY_ANCIENTS_CYCLOPS_RESISTANCE").Replace("{0}", cyclopsDamageResistance.ToString()); // CYCLOPS DAMAGE RESISTANCE AT {0}%;
                        cyclopsResistanceText.alignment = TextAnchor.MiddleLeft;
                        cyclopsResistanceText.fontSize = 35;
                        cyclopsResistanceText.color = Color.white;
                        cyclopsResistanceText.font = PuristaSemiboldFontCache ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                        RectTransform rectGeneratorsHealthText = cyclopsResistanceText.GetComponent<RectTransform>();
                        rectGeneratorsHealthText.sizeDelta = new Vector2(800, 60);
                        rectGeneratorsHealthText.localScale = new Vector2(0.5f, 0.5f);
                        rectGeneratorsHealthText.anchoredPosition = Vector2.zero + new Vector2(40, 37.5f); //Vector2.zero + new Vector2(20, 40);
                        _cyclopsResistance = cyclopsResistanceText;

                        AddClickChaseTarget(chaseTarget, iconObj.gameObject);


                        ancientsWidgetPrefab.transform.SetParent(widgetContainer);
                        ancientsWidgetPrefab.SetActive(true);


                        GameObject tooltipBgObj = new GameObject("TooltipBackground", typeof(RectTransform));
                        tooltipBgObj.transform.SetParent(ancientsWidgetPrefab.transform);
                        Image tooltipBgImage = tooltipBgObj.AddComponent<Image>();
                        tooltipBgImage.color = new Color(0, 0, 0, 0.75f); // Semi-transparent black

                        RectTransform tooltipBgRect = tooltipBgObj.GetComponent<RectTransform>();
                        tooltipBgRect.sizeDelta = new Vector2(1500, 1500); // Slightly larger than text for padding
                        tooltipBgRect.anchoredPosition = new Vector2(600, 40);
                        tooltipBgRect.localScale = new Vector2(0.5f, 0.5f);

                        // Ensure it appears behind the text
                        //  tooltipText.transform.SetParent(tooltipBgObj.transform);
                        //   tooltipText.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                        // Hide both initially
                        tooltipBgObj.SetActive(false);

                        // Modify EventTriggers to show/hide both the text and background


                        // Create the tooltip text element
                        GameObject tooltipObj = new GameObject("TooltipText");
                        tooltipObj.transform.SetParent(ancientsWidgetPrefab.transform);
                        tooltipText = tooltipObj.AddComponent<Text>();
                        tooltipText.text = TFTVCommonMethods.ConvertKeyToString("TFTV_KEY_ANCIENTS_RULESET");
                        tooltipText.alignment = TextAnchor.UpperLeft;
                        tooltipText.fontSize = 40; // Adjust as needed
                        tooltipText.color = Color.white; // Adjust text color as needed
                        tooltipText.font = PuristaSemiboldFontCache ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                        tooltipText.verticalOverflow = VerticalWrapMode.Overflow;
                        tooltipText.horizontalOverflow = HorizontalWrapMode.Wrap;

                        RectTransform tooltipRect = tooltipText.GetComponent<RectTransform>();
                        tooltipRect.sizeDelta = new Vector2(1400, 100); // Adjust size as needed
                        tooltipRect.anchoredPosition = new Vector2(600, 350); // Adjust position relative to widget element
                        tooltipRect.localScale = new Vector2(0.5f, 0.5f);

                        // Add EventTrigger to handle tooltip visibility
                        EventTrigger eventTrigger = ancientsWidgetPrefab.AddComponent<EventTrigger>();

                        // Pointer Enter event to show tooltip
                        EventTrigger.Entry pointerEnter = new EventTrigger.Entry
                        {
                            eventID = EventTriggerType.PointerEnter
                        };
                        pointerEnter.callback.AddListener((eventData) =>
                        {
                            tooltipText.gameObject.SetActive(true);
                        });
                        eventTrigger.triggers.Add(pointerEnter);

                        // Pointer Exit event to hide tooltip
                        EventTrigger.Entry pointerExit = new EventTrigger.Entry
                        {
                            eventID = EventTriggerType.PointerExit
                        };
                        pointerExit.callback.AddListener((eventData) =>
                        {
                            tooltipText.gameObject.SetActive(false);
                        });
                        eventTrigger.triggers.Add(pointerExit);

                        pointerEnter.callback.AddListener((eventData) =>
                        {
                            tooltipBgObj.SetActive(true);
                        });

                        pointerExit.callback.AddListener((eventData) =>
                        {
                            tooltipBgObj.SetActive(false);
                        });

                        tooltipObj.SetActive(false);

                        // Create the tooltip background



                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private Color GetColor(TacticalActor tacticalActor)
                {
                    try
                    {
                        if (tacticalActor.CharacterStats.WillPoints >= 30)
                        {
                            return LeaderColor;
                        }

                        return NegativeColor;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                public void ModifyWidget(float cyclopsDamageResistance, TacticalActor chaseTarget)
                {
                    try
                    {
                        Color color = GetColor(chaseTarget);

                        _cyclopsResistance.text = TFTVCommonMethods.ConvertKeyToString("TFTV_KEY_ANCIENTS_CYCLOPS_RESISTANCE").Replace("{0}", cyclopsDamageResistance.ToString());
                        _iconImage.color = color;

                        AddClickChaseTarget(chaseTarget, _bgImage.gameObject);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }





            private static void CreateAncientsWidget(int cyclopsDamageResistance, TacticalActor chaseTarget)
            {
                try
                {

                    TFTVLogger.Always($"CreateAncientsWidget running");

                    _ancientWidget = new GameObject("AncientsWidgetObject");
                    AncientsWidget defenseWidget = _ancientWidget.AddComponent<AncientsWidget>();

                    TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();
                    UIModuleNavigation uIModuleNavigation = controller.View.TacticalModules.NavigationModule;
                    _ancientWidget.transform.SetParent(uIModuleNavigation.transform, false);

                    defenseWidget.InitializeAncientsWidget(cyclopsDamageResistance, chaseTarget);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            public static void ActivateOrAdjustAncientsWidget(int cyclopsDamageResistance, TacticalActor chaseTarget)
            {
                try
                {
                    TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                    if (_ancientWidget == null)
                    {
                        CreateAncientsWidget(cyclopsDamageResistance, chaseTarget);
                    }
                    else
                    {
                        AncientsWidget ancientsWidget = _ancientWidget.GetComponent<AncientsWidget>();

                        ancientsWidget.ModifyWidget(cyclopsDamageResistance, chaseTarget);

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }



        }

        internal class BaseDefenseUI
        {
            public class BaseDefenseWidget : MonoBehaviour
            {
                private GameObject baseDefenseWidgetPrefab;
                private Transform widgetContainer;
                private Text _reinforcementTitle;
                private Text _reinforcementDescription;
                private Text _generatorsHealth;
                private Image _iconImage;
                private Image _bgImage;

                private Color GetColor(int consolesLeft)
                {
                    try
                    {
                        Color color = Color.green;

                        if (consolesLeft == 1)
                        {
                            color = Color.yellow;
                        }

                        if (consolesLeft == 0)
                        {
                            color = Color.red;
                        }

                        return color;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private void AddClickChaseTarget(TacticalActorBase tacticalActorBase, GameObject gameObject)
                {
                    try
                    {
                        if (!gameObject.GetComponent<EventTrigger>())
                        {
                            gameObject.AddComponent<EventTrigger>();
                        }

                        EventTrigger eventTrigger = _bgImage.GetComponent<EventTrigger>();
                        eventTrigger.triggers.Clear();

                        if (tacticalActorBase != null)
                        {

                            EventTrigger.Entry click = new EventTrigger.Entry
                            {
                                eventID = EventTriggerType.PointerClick
                            };

                            click.callback.AddListener((eventData) =>
                            {
                                tacticalActorBase.CameraDirector.Hint(CameraHint.ChaseTarget, new CameraChaseParams
                                {
                                    ChaseVector = tacticalActorBase.Pos,
                                    ChaseTransform = null,
                                    ChaseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
                                    LockCameraMovement = false,
                                    Instant = false,
                                    ChaseOnlyOutsideFrame = false,
                                    SnapToFloorHeight = true

                                });

                            });

                            eventTrigger.triggers.Add(click);
                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }


                public void InitializeBaseDefenseWidget(string reinforcementsTitle, string reinforcementsDescription, float powerGeneratorHP, int consolesLeft, TacticalActorBase chaseTarget)
                {
                    try
                    {
                        Color color = GetColor(consolesLeft);

                        Resolution resolution = Screen.currentResolution;
                        float resolutionFactorWidth = (float)resolution.width / 1920f;
                        float resolutionFactorHeight = (float)resolution.height / 1080f;

                        // Access UIModuleNavigation and set widgetContainer as its transform
                        TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();
                        UIModuleNavigation uIModuleNavigation = controller.View.TacticalModules.NavigationModule;
                        widgetContainer = uIModuleNavigation.transform;

                        // Dynamically create the leaderWidgetPrefab structure
                        baseDefenseWidgetPrefab = new GameObject("BaseDefenseWidget");
                        RectTransform rectTransform = baseDefenseWidgetPrefab.AddComponent<RectTransform>();

                        rectTransform.sizeDelta = new Vector2(410, 180);
                        rectTransform.position = new Vector2(245 * resolutionFactorWidth, 600 * resolutionFactorHeight);

                        GameObject backgroundImage = new GameObject("Background", typeof(RectTransform), typeof(Image));

                        backgroundImage.transform.SetParent(baseDefenseWidgetPrefab.transform); // Attach to the existing GameObject

                        RectTransform bgRect = backgroundImage.GetComponent<RectTransform>();
                        bgRect.sizeDelta = new Vector2(rectTransform.sizeDelta.x, rectTransform.sizeDelta.y / 1.7f);
                        bgRect.anchoredPosition = Vector2.zero + new Vector2(7, -40);

                        Image bgImage = backgroundImage.GetComponent<Image>();
                        bgImage.color = new Color(0, 0, 0, 0.5f); // Black with 50% opacity
                        _bgImage = bgImage;

                        AddClickChaseTarget(chaseTarget, bgImage.gameObject);

                        // Set up the icon
                        GameObject iconObj = new GameObject("Icon");
                        iconObj.transform.SetParent(baseDefenseWidgetPrefab.transform);
                        Image iconImage = iconObj.AddComponent<Image>();
                        iconImage.sprite = DefCache.GetDef<ViewElementDef>("E_ViewElement [EnergyGenerator_PhoenixFacilityDef]").SmallIcon;
                        iconImage.color = color;
                        iconImage.preserveAspect = true;
                        RectTransform iconImageRect = iconImage.GetComponent<RectTransform>();
                        iconImageRect.sizeDelta = new Vector2(30, 30);
                        iconImageRect.anchoredPosition = Vector2.zero + new Vector2(-185, 38);//Vector2.zero + new Vector2(-150, 38);
                        _iconImage = iconImage;

                        // Set up the name text
                        GameObject generatorHealthTextObj = new GameObject("PowerGeneratorsHealthText");
                        generatorHealthTextObj.transform.SetParent(baseDefenseWidgetPrefab.transform);
                        Text generatorHealthText = generatorHealthTextObj.AddComponent<Text>();
                        generatorHealthText.text = TFTVCommonMethods.ConvertKeyToString("TFTV_KEY_BASE_DEFENSE_GENERATORS").Replace("{0}", powerGeneratorHP.ToString()).Replace("{1}", consolesLeft.ToString()); // $"Generators at {powerGeneratorHP}%, can be vented {consolesLeft} times";
                        generatorHealthText.alignment = TextAnchor.MiddleLeft;
                        generatorHealthText.fontSize = 35;
                        generatorHealthText.color = color;
                        generatorHealthText.font = PuristaSemiboldFontCache ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                        RectTransform rectGeneratorsHealthText = generatorHealthText.GetComponent<RectTransform>();
                        rectGeneratorsHealthText.sizeDelta = new Vector2(800, 60);
                        rectGeneratorsHealthText.localScale = new Vector2(0.5f, 0.5f);
                        rectGeneratorsHealthText.anchoredPosition = Vector2.zero + new Vector2(35, 40); //Vector2.zero + new Vector2(20, 40);
                        _generatorsHealth = generatorHealthText;

                        // Set up the tactic name text
                        GameObject reinforcementTitleTextObj = new GameObject("ReinforcementTitleText");
                        reinforcementTitleTextObj.transform.SetParent(backgroundImage.transform);
                        Text reinforcementTitleText = reinforcementTitleTextObj.AddComponent<Text>();
                        reinforcementTitleText.text = reinforcementsTitle; //need to complete
                        reinforcementTitleText.fontSize = 40;
                        reinforcementTitleText.color = WhiteColor;
                        reinforcementTitleText.alignment = TextAnchor.MiddleLeft;
                        reinforcementTitleText.font = PuristaSemiboldFontCache ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                        RectTransform rectReinforcementTitleText = reinforcementTitleText.GetComponent<RectTransform>();
                        rectReinforcementTitleText.sizeDelta = new Vector2(820, 60);
                        rectReinforcementTitleText.localScale = new Vector2(0.5f, 0.5f);
                        rectReinforcementTitleText.anchoredPosition = Vector2.zero + new Vector2(35, 30);
                        _reinforcementTitle = reinforcementTitleText;

                        // Set up the tactic description text
                        GameObject reinforcementDescriptionTextObj = new GameObject("ReinforcementDescriptionText");
                        reinforcementDescriptionTextObj.transform.SetParent(backgroundImage.transform);
                        Text reinforcementDescriptionText = reinforcementDescriptionTextObj.AddComponent<Text>();
                        reinforcementDescriptionText.text = reinforcementsDescription;//need to complete
                        reinforcementDescriptionText.font = PuristaSemiboldFontCache ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                        reinforcementDescriptionText.fontSize = 30;
                        reinforcementDescriptionText.color = Color.grey;
                        reinforcementDescriptionText.alignment = TextAnchor.MiddleLeft;
                        reinforcementDescriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
                        reinforcementDescriptionText.verticalOverflow = VerticalWrapMode.Overflow;
                        RectTransform recttacticDescriptionText = reinforcementDescriptionText.GetComponent<RectTransform>();
                        recttacticDescriptionText.sizeDelta = new Vector2(700, 60);
                        recttacticDescriptionText.localScale = new Vector2(0.5f, 0.5f);
                        recttacticDescriptionText.anchoredPosition = Vector2.zero + new Vector2(5, -20);
                        _reinforcementDescription = reinforcementDescriptionText;


                        baseDefenseWidgetPrefab.transform.SetParent(widgetContainer);
                        baseDefenseWidgetPrefab.SetActive(true);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                public void ModifyWidget(string reinforcementTitle, string reinforcementDescription, float powerGeneratorHP, int consolesLeft, TacticalActorBase chaseTarget)
                {
                    try
                    {
                        Color color = GetColor(consolesLeft);

                        _reinforcementTitle.text = reinforcementTitle;
                        _reinforcementDescription.text = reinforcementDescription;
                        _generatorsHealth.text = $"Generators at {powerGeneratorHP}%, can be vented {consolesLeft} times";
                        _generatorsHealth.color = color;
                        _iconImage.color = color;

                        _reinforcementTitle.gameObject.SetActive(true);
                        _reinforcementDescription.gameObject.SetActive(true);
                        _bgImage.gameObject.SetActive(true);

                        if (reinforcementTitle == "")
                        {
                            _reinforcementTitle.gameObject.SetActive(false);
                            _reinforcementDescription.gameObject.SetActive(false);
                            _bgImage.gameObject.SetActive(false);
                        }

                        AddClickChaseTarget(chaseTarget, _bgImage.gameObject);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }


            public static void ActivateOrAdjustBaseDefenseWidget(string reinforcementName, string reinforcementDescription, float powerGeneratorHP, int consolesLeft, TacticalActorBase chaseTarget)
            {
                try
                {
                    TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                    if (_baseDefenseWidget == null)
                    {
                        CreateBaseDefenseWidget(reinforcementName, reinforcementDescription, powerGeneratorHP, consolesLeft, chaseTarget);
                    }
                    else
                    {
                        BaseDefenseWidget baseDefenseWidget = _baseDefenseWidget.GetComponent<BaseDefenseWidget>();

                        baseDefenseWidget.ModifyWidget(reinforcementName, reinforcementDescription, powerGeneratorHP, consolesLeft, chaseTarget);

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


            private static GameObject _baseDefenseWidget;

            private static void CreateBaseDefenseWidget(string reinforcementName, string reinforcementDescription, float powerGeneratorHP, int consolesLeft, TacticalActorBase chaseTarget)
            {
                try
                {

                    _baseDefenseWidget = new GameObject("BaseDefenseWidgetObject");
                    BaseDefenseWidget defenseWidget = _baseDefenseWidget.AddComponent<BaseDefenseWidget>();

                    TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();
                    UIModuleNavigation uIModuleNavigation = controller.View.TacticalModules.NavigationModule;
                    _baseDefenseWidget.transform.SetParent(uIModuleNavigation.transform, false);

                    defenseWidget.InitializeBaseDefenseWidget(reinforcementName, reinforcementDescription, powerGeneratorHP, consolesLeft, chaseTarget);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        internal class TFTVTacticalObjectives
        {
            private static readonly TFTVConfig Config = new TFTVConfig();
            private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
            private static readonly DefRepository Repo = TFTVMain.Repo;
            private static readonly SharedData sharedData = TFTVMain.Shared;


            public class AddOutlineToIcon : MonoBehaviour
            {
                public GameObject icon; // The icon GameObject to outline

                public void InitOrUpdate()
                {
                    try
                    {
                        // Ensure the icon has a Graphic component, like Image or Text
                        var graphic = icon.GetComponent<Graphic>();
                        if (graphic != null)
                        {
                            // Add the Outline component
                            Outline outline = icon.GetComponent<Outline>() ?? icon.AddComponent<Outline>();
                            outline.effectColor = Color.black;         // Outline color

                            if (icon.GetComponent<Image>() != null && icon.GetComponent<Image>().sprite == DefCache.GetDef<ViewElementDef>("E_ViewElement [TBTV_Hidden_AbilityDef]").SmallIcon)
                            {
                                // TFTVLogger.Always($"got here");
                                outline.effectColor = VoidColor;
                                icon.GetComponent<Image>().color = Color.black;
                            }
                            outline.effectDistance = new Vector2(2.5f, 2.5f); // Outline thickness
                        }
                        else
                        {
                            TFTVLogger.Always("No Graphic component found on icon.");
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }


            [HarmonyPatch(typeof(UIModuleBattleSummary), "Initialize")]
            public static class UIModuleBattleSummary_Initialize_patch
            {
                public static void Prefix(UIModuleBattleSummary __instance)
                {
                    try
                    {
                        RectTransform rectTransformToCopy = null;

                        foreach (Component component in __instance.ObjectivesResultContainer)
                        {
                            if (component is RectTransform transform)
                            {
                                rectTransformToCopy = transform;
                                break;
                            }
                        }

                        UnityEngine.Object.Instantiate(rectTransformToCopy, __instance.ObjectivesResultContainer.transform);
                        UnityEngine.Object.Instantiate(rectTransformToCopy, __instance.ObjectivesResultContainer.transform);
                        UnityEngine.Object.Instantiate(rectTransformToCopy, __instance.ObjectivesResultContainer.transform);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }



            [HarmonyPatch(typeof(ObjectiveElement), "SetObjective", new Type[] { typeof(FactionObjective) })]
            public static class ObjectiveElement_SetObjective_patch
            {


                public static void Postfix(FactionObjective objective, ObjectiveElement __instance)
                {
                    try
                    {
                        if (objective.State == FactionObjectiveState.Achieved)
                        {
                            __instance.Description.color = Color.green;
                        }
                        else if (__instance.Description.color == Color.green && objective.State != FactionObjectiveState.Achieved)
                        {
                            __instance.Description.color = WhiteColor;
                        }

                        SecondaryObjectivesTactical.AdjustSecondaryObjective(objective, __instance);
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }

                }
            }




            //Adjusted for all haven defenses because parapsychosis bug. Check if necessary for other WipeEnemyFactionObjective missions
            [HarmonyPatch(typeof(WipeEnemyFactionObjective), "EvaluateObjective")]
            public static class TFTV_HavenDefendersHostileFactionObjective_EvaluateObjective_Patch
            {
                public static bool Prefix(FactionObjective __instance, ref FactionObjectiveState __result,
                    List<TacticalFaction> ____enemyFactions, List<TacticalFactionDef> ____overrideFactions, bool ____ignoreDeployment)
                {
                    try
                    {
                        //  TFTVLogger.Always($"evaluating {__instance.GetDescription()} and the result is {__result}");

                        //   if (VoidOmensCheck[5])
                        //   {
                        TacticalLevelController controller = __instance.Level;
                        string MissionType = controller.TacticalGameParams.MissionData.MissionType.SaveDefaultName;

                        if (MissionType == "HavenDefense")
                        {
                            if (!__instance.IsUiHidden)
                            {

                                //  TFTVLogger.Always("WipeEnemyFactionObjetive invoked");

                                if (!__instance.Faction.HasTacActorsThatCanWin() && !__instance.Faction.HasUndeployedTacActors())
                                {
                                    __result = FactionObjectiveState.Failed;
                                    //  TFTVLogger.Always("WipeEnemyFactionObjetive failed");
                                    return false; // skip original method
                                }

                                foreach (TacticalFaction enemyFaction in controller.Factions)
                                {
                                    if (enemyFaction.ParticipantKind == TacMissionParticipant.Intruder)
                                    {
                                        // TFTVLogger.Always("The faction is " + faction.TacticalFactionDef.name);
                                        if (!enemyFaction.HasTacActorsThatCanWin())
                                        {
                                            //  TFTVLogger.Always("HavenDefense, no intruders alive, so mission should be a win");
                                            __result = FactionObjectiveState.Achieved;
                                            return false;
                                        }

                                    }
                                }


                            }
                            return true;
                        }
                        return true;
                        //  }
                        //  return true;
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }
        }

        internal class SecondaryObjectivesTactical
        {
            private static Transform _secondaryObjectives = null;
            private static Transform _secondaryObjectivesTitle = null;

            private static ActorHasStatusFactionObjectiveDef _captureAnyRevenant;
            private static ActorHasStatusFactionObjectiveDef _captureScylla;
            private static ActorHasStatusFactionObjectiveDef _captureSiren;
            private static ActorHasStatusFactionObjectiveDef _captureGooAlien;
            private static ActorHasStatusFactionObjectiveDef _captureViralAlien;
            private static ActorHasStatusFactionObjectiveDef _captureMistAlien;
            private static ActorHasStatusFactionObjectiveDef _capturePsychicAlien;

            private static KillActorFactionObjectiveDef _killRevenantMimic;
            private static KillActorFactionObjectiveDef _killRevenantDybbuk;
            private static KillActorFactionObjectiveDef _killRevenantNemesis;
            private static KillActorFactionObjectiveDef _killScylla;

            public static List<string> AvailableSecondaryObjectivesTactical = new List<string>();
            private static List<FactionObjectiveDef> _secondaryObjectiveDefsInPlay = new List<FactionObjectiveDef>();
            private static List<FactionObjectiveDef> _secondaryObjectiveDefsALL = new List<FactionObjectiveDef>();

            private static Dictionary<FactionObjective, List<TacticalActor>> _objectivesTargetsDictionary = new Dictionary<FactionObjective, List<TacticalActor>>();
            private static NewObjectiveUI _newObjectiveWidget = null;

            public static void AddAdditionalSecondaryObjective(FactionObjectiveDef objectiveDef)
            {
                try
                {
                    _secondaryObjectiveDefsInPlay.Add(objectiveDef);
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }


            public class NewObjectiveUI : MonoBehaviour
            {
                public Sprite IconSprite;

                public GameObject uiObject;
                private float fadeDuration = 1f; // Duration for fade in/out
                private float displayDuration = 6f; // Duration to display at full opacity
                public CanvasGroup CanvasGroup;
                private List<string> descriptions;

                private float timer = 0f;
                private enum FadeState { None, FadingIn, Displaying, FadingOut, FadingInText }
                private FadeState fadeState = FadeState.None;
                private Text _description;
                Color greenColor = new Color(0.4f, 0.729f, 0.416f);

                public void ShowNewObjective(string title, List<string> descriptions, Sprite icon, bool isRevenant = false, bool victoryObjective = false, float displayDuration = 3f)
                {
                    try
                    {
                        this.descriptions = descriptions;
                        this.displayDuration = displayDuration;
                        currentDescriptionIndex = 0; // Reset index for each new objective
                        descriptionDisplayTime = displayDuration / descriptions.Count; // Time per description

                        CreateUIElement(title, descriptions[currentDescriptionIndex], icon, victoryObjective, isRevenant);
                        StartFadeIn();
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private int currentDescriptionIndex = 0;
                private float descriptionDisplayTime;


                private void CreateUIElement(string title, string initialDescription, Sprite classIcon, bool victoryObjective = false, bool isRevenant = false)
                {
                    try
                    {
                        UIModuleNavigation uIModuleNavigation = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>().View.TacticalModules.NavigationModule;

                        uiObject = new GameObject("NewObjectiveUI", typeof(RectTransform));
                        RectTransform rectTransform = uiObject.GetComponent<RectTransform>();
                        rectTransform.SetParent(uIModuleNavigation.transform, false);
                        rectTransform.sizeDelta = new Vector2(900, 140);
                        rectTransform.anchorMin = new Vector2(0.5f, 0.8f);
                        rectTransform.anchorMax = new Vector2(0.5f, 0.8f);
                        rectTransform.pivot = new Vector2(0.5f, 0.5f);
                        rectTransform.position = new Vector3(Screen.width / 2, Screen.height * 4 / 5, 0);

                        CanvasGroup = uiObject.AddComponent<CanvasGroup>();
                        CanvasGroup.alpha = 0;

                        // _newObjectiveWidget = uiObject;

                        GameObject background = new GameObject("Background", typeof(RectTransform));
                        background.transform.SetParent(uiObject.transform);
                        Image bgImage = background.AddComponent<Image>();
                        background.GetComponent<RectTransform>().sizeDelta = new Vector2(rectTransform.sizeDelta.x, rectTransform.sizeDelta.y - 40);
                        bgImage.sprite = Helper.CreateSpriteFromImageFile("pp_obj_update_background.png");
                        bgImage.color = new Color(0, 0, 0, 0.75f); // Adjusted alpha for background only
                        RectTransform bgRect = background.GetComponent<RectTransform>();
                        bgRect.anchorMin = new Vector2(0, 0);
                        bgRect.anchorMax = new Vector2(1, 1);
                        bgRect.offsetMin = Vector2.zero;
                        bgRect.offsetMax = Vector2.zero;


                        // Top Border Line with Icon in the Middle
                        GameObject topLine = new GameObject("TopLine");
                        topLine.transform.SetParent(uiObject.transform);
                        RectTransform lineRect = topLine.AddComponent<RectTransform>();
                        lineRect.sizeDelta = new Vector2(500, 1);
                        lineRect.anchorMin = new Vector2(0.5f, 1);
                        lineRect.anchorMax = new Vector2(0.5f, 1);
                        lineRect.pivot = new Vector2(0.5f, 1);
                        lineRect.anchoredPosition = new Vector2(0, 65);

                        Image lineImage = topLine.AddComponent<Image>();
                        lineImage.sprite = Helper.CreateSpriteFromImageFile("pp_obj_update_line.png");

                        if (victoryObjective)
                        {
                            lineImage.color = greenColor;
                        }


                        // Icon on the Top Border
                        GameObject icon = new GameObject("Icon");
                        icon.transform.SetParent(uiObject.transform);
                        Image iconImage = icon.AddComponent<Image>();
                        iconImage.sprite = IconSprite;
                        RectTransform iconRect = icon.GetComponent<RectTransform>();
                        iconRect.sizeDelta = new Vector2(60, 60);
                        iconRect.anchorMin = new Vector2(0.5f, 1);
                        iconRect.anchorMax = new Vector2(0.5f, 1);
                        iconRect.pivot = new Vector2(0.5f, 1);
                        iconRect.anchoredPosition = new Vector2(0, 130); // Moved higher

                        AddOutlineToIcon addOutlineToObjectiveIcon = icon.GetComponent<AddOutlineToIcon>() ?? icon.AddComponent<AddOutlineToIcon>();
                        addOutlineToObjectiveIcon.icon = icon;
                        addOutlineToObjectiveIcon.InitOrUpdate();

                        // Title Text
                        GameObject titleTextObj = new GameObject("TitleText");
                        titleTextObj.transform.SetParent(uiObject.transform);
                        Text titleText = titleTextObj.AddComponent<Text>();
                        titleText.text = title;
                        titleText.font = PuristaSemiboldFontCache ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                        titleText.fontSize = 80; // Doubled font size
                        titleText.color = Color.gray;
                        titleText.alignment = TextAnchor.MiddleCenter;
                        RectTransform titleRect = titleTextObj.GetComponent<RectTransform>();
                        titleRect.sizeDelta = new Vector2(1500, 160);
                        titleRect.anchorMin = new Vector2(0.5f, 0.8f);
                        titleRect.anchorMax = new Vector2(0.5f, 0.8f);
                        titleRect.pivot = new Vector2(0.5f, 1);
                        titleRect.localScale = new Vector3(0.5f, 0.5f, 0.5f); // Scaling down to improve text clarity
                        titleRect.anchoredPosition = new Vector2(0, 60);


                        // Description Text
                        GameObject descriptionTextObj = new GameObject("DescriptionText");
                        descriptionTextObj.transform.SetParent(uiObject.transform);
                        Text descriptionText = descriptionTextObj.AddComponent<Text>();
                        descriptionText.text = initialDescription;
                        descriptionText.horizontalOverflow = HorizontalWrapMode.Overflow;
                        descriptionText.verticalOverflow = VerticalWrapMode.Overflow;
                        descriptionText.font = PuristaSemiboldFontCache ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                        descriptionText.fontSize = 120; // Doubled font size
                        descriptionText.color = WhiteColor;
                        descriptionText.alignment = TextAnchor.MiddleCenter;
                        RectTransform descRect = descriptionTextObj.GetComponent<RectTransform>();
                        descRect.sizeDelta = new Vector2(3000, 240);
                        descRect.anchorMin = new Vector2(0.5f, 0.6f);
                        descRect.anchorMax = new Vector2(0.5f, 0.6f);
                        descRect.pivot = new Vector2(0.5f, 1);
                        descRect.localScale = new Vector3(0.5f, 0.5f, 0.5f); // Scaling down to improve text clarity
                        descRect.anchoredPosition = new Vector2(0, 40);


                        // Class Icon below
                        GameObject targetIcon = new GameObject("TargetIcon");
                        targetIcon.transform.SetParent(uiObject.transform);
                        Image targetIconImage = targetIcon.AddComponent<Image>();
                        targetIconImage.sprite = classIcon;
                        targetIconImage.color = NegativeColor;

                        if (isRevenant)
                        {
                            targetIconImage.color = LeaderColor;
                        }

                        RectTransform targetIconRect = targetIcon.GetComponent<RectTransform>();
                        targetIconRect.sizeDelta = new Vector2(120, 120);
                        targetIconRect.anchorMin = new Vector2(0.5f, 1);
                        targetIconRect.anchorMax = new Vector2(0.5f, 1);
                        targetIconRect.pivot = new Vector2(0.5f, 1);
                        targetIconRect.anchoredPosition = new Vector2(0, -500); // Moved higher

                        AddOutlineToIcon addOutlineToIcon = targetIcon.GetComponent<AddOutlineToIcon>() ?? targetIcon.AddComponent<AddOutlineToIcon>();
                        addOutlineToIcon.icon = targetIcon;
                        addOutlineToIcon.InitOrUpdate();

                        if (victoryObjective)
                        {
                            descriptionText.color = greenColor;
                        }

                        // Set display time for each description segment
                        //  descriptionDisplayTime = displayDuration / descriptions.Length;


                        _description = descriptionText;


                        AdjustAlphaElements(0);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private void Update()
                {
                    try
                    {
                        if (fadeState == FadeState.FadingIn)
                        {
                            timer += Time.deltaTime;
                            AdjustAlphaElements(Mathf.Lerp(0, 1, timer / fadeDuration));

                            if (timer >= fadeDuration)
                            {
                                timer = 0f;
                                fadeState = FadeState.Displaying;
                            }
                        }
                        else if (fadeState == FadeState.Displaying)
                        {
                            timer += Time.deltaTime;

                            if (timer >= descriptionDisplayTime)
                            {
                                timer = 0f;
                                fadeState = FadeState.FadingOut;
                            }
                        }
                        else if (fadeState == FadeState.FadingOut)
                        {
                            timer += Time.deltaTime;
                            AdjustAlphaElements(Mathf.Lerp(1, 0, timer / fadeDuration));

                            if (timer >= fadeDuration)
                            {
                                currentDescriptionIndex++;
                                if (currentDescriptionIndex >= descriptions.Count)
                                {
                                    fadeState = FadeState.None;
                                    AdjustAlphaElements(0); // Ensure alpha is fully 0 before destroying
                                    DestroyUIObject();
                                }
                                else
                                {
                                    _description.text = descriptions[currentDescriptionIndex];
                                    StartFadeIn(); // Restart fade-in for the next description
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }


                private void AdjustAlphaElements(float alpha)
                {
                    try
                    {
                        if (CanvasGroup != null)
                        {
                            CanvasGroup.alpha = alpha;
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private void DestroyUIObject()
                {
                    if (uiObject != null)
                    {
                        Destroy(uiObject);
                        uiObject = null;
                    }
                }

                private void StartFadeIn()
                {
                    try
                    {
                        timer = 0.0f;
                        fadeState = FadeState.FadingIn;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            private static List<TacticalActor> GetTargetsForObjective(FactionObjective objective, bool onlyAlive = true, bool onlyRevealed = true)
            {
                try
                {
                    GameTagDef relevantTag = null;

                    TacticalLevelController controller = objective.Level;

                    /*  if (!controller.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("aln")))
                      {
                          return null;
                      }*/

                    if (objective is KillActorFactionObjective killObjective)
                    {
                        FieldInfo fieldInfo = typeof(KillActorFactionObjective).GetField("_killTargetsGameTag", BindingFlags.Instance | BindingFlags.NonPublic);

                        relevantTag = (GameTagDef)fieldInfo.GetValue(killObjective);
                    }
                    else if (objective is ActorHasStatusFactionObjective captureObjective)
                    {
                        FieldInfo fieldInfo = typeof(ActorHasStatusFactionObjective).GetField("_targetsGameTag", BindingFlags.Instance | BindingFlags.NonPublic);
                        relevantTag = (GameTagDef)fieldInfo.GetValue(captureObjective);
                    }

                    //   TacticalFaction aliens = controller.GetFactionByCommandName("aln");
                    TacticalFaction phoenix = controller.GetFactionByCommandName("px");

                    List<TacticalActor> enemyActors = controller.Factions
                                         .Where(f => f.GetRelationTo(phoenix) == FactionRelation.Enemy)
                                         .SelectMany(f => f.TacticalActors)
                                         .ToList();




                    List<TacticalActor> relevantActors = enemyActors.
                        Where(ta =>
                        (ta.HasGameTag(relevantTag) || ta.BodyState.GetAllBodyparts().Any(b => b.OwnerItem.TacticalItemDef.Tags.Contains(relevantTag)) || ta.Equipments.GetWeapons().Any(w => w.GameTags.Contains(relevantTag)))).ToList().
                        Concat(phoenix.TacticalActors.Where(ta =>
                        (ta.HasGameTag(relevantTag) || ta.Equipments.GetWeapons().Any(w => w.GameTags.Contains(relevantTag))) && ta.Status != null && ta.Status.HasStatus<MindControlStatus>())).ToList();

                    if (onlyAlive)
                    {
                        relevantActors = relevantActors.Where(ta => ta.IsAlive).ToList();
                    }

                    if (onlyRevealed)
                    {
                        relevantActors = relevantActors.Where(ta => phoenix.Vision.IsRevealed(ta)).ToList();
                    }

                    //  TFTVLogger.Always($"objective {objective.Description.Localize()} relevantActors count: {relevantActors.Count}");

                    return relevantActors;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }

            private static void AddChaseTargetOnClick(GameObject parent, FactionObjective factionObjective)
            {
                try
                {

                    if (!parent.GetComponent<EventTrigger>())
                    {
                        parent.AddComponent<EventTrigger>();
                    }

                    EventTrigger eventTrigger = parent.GetComponent<EventTrigger>();
                    eventTrigger.triggers.Clear();
                    EventTrigger.Entry click = new EventTrigger.Entry
                    {
                        eventID = EventTriggerType.PointerClick
                    };

                    click.callback.AddListener((eventData) =>
                    {
                        TacticalActor target = _objectivesTargetsDictionary[factionObjective].FirstOrDefault();

                        target.TacticalActorView.DoCameraChase();
                        if (_objectivesTargetsDictionary[factionObjective].Count > 0)
                        {
                            _objectivesTargetsDictionary[factionObjective].Remove(target);
                            _objectivesTargetsDictionary[factionObjective].Add(target);
                        }

                    });

                    eventTrigger.triggers.Add(click);

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }



            }

            private static UIHider _uIHider = null;
            private static bool _objectiveVictoryAnnouncementRunning = false;

            public static void CreateSecondaryObjectiveAnnouncement(List<string> descriptions, Sprite icon, bool isRevenant = false, bool victoryObjective = false)
            {
                try
                {
                    //  if (_newObjectiveWidget == null)
                    //   {
                    TFTVLogger.Always($"Running objective announcement: {descriptions.First()}, _objectiveVictoryAnnouncementRunning? {_objectiveVictoryAnnouncementRunning}");

                    string title = TFTVCommonMethods.ConvertKeyToString("TFTV_KEY_NEW_SECONDARY_OBJECTIVE");

                    if (victoryObjective)
                    {
                        title = TFTVCommonMethods.ConvertKeyToString("TFTV_KEY_OBJECTIVE_ACCOMPLISHED");
                    }

                    if (!_objectiveVictoryAnnouncementRunning)
                    {
                        GameObject gameObject = new GameObject();
                        NewObjectiveUI newObjectiveUI = gameObject.AddComponent<NewObjectiveUI>();
                        newObjectiveUI.IconSprite = Helper.CreateSpriteFromImageFile("objective.png");
                        newObjectiveUI.ShowNewObjective(title, descriptions, icon, isRevenant, victoryObjective);
                        _newObjectiveWidget = newObjectiveUI;
                        //  TacticalUIOverlayController.ToggleUI();

                        _uIHider = new GameObject("UIHider").AddComponent<UIHider>();
                        _uIHider.HideUI();
                        ExecuteAfterDelay(2f, RestoreUI);
                    }
                    if (victoryObjective)
                    {
                        _objectiveVictoryAnnouncementRunning = true;
                    }

                    //ExecuteAfterDelay(10f, );
                    //  }
                    /* else
                     {
                         _objectiveDescription = description.ToUpper();
                         ExecuteAfterDelay(2f, ChangeDescription);
                     }*/
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


            public static void AdjustSecondaryObjective(FactionObjective objective, ObjectiveElement objectiveElement)
            {
                try
                {

                    if (!IsSecondaryObjective(objective))
                    {
                        return;
                    }


                    List<TacticalActor> targets = GetTargetsForObjective(objective);

                    if (targets.Count == 0)
                    {
                        return;
                    }

                    AdjustSecondaryObjectiveTextRevenant(objective, objectiveElement);

                    if (_objectivesTargetsDictionary.ContainsKey(objective))
                    {
                        if (objective is ActorHasStatusFactionObjective)
                        {
                            _objectivesTargetsDictionary[objective] = targets;
                        }
                    }
                    else
                    {
                        _objectivesTargetsDictionary.Add(objective, targets);
                    }

                    AddChaseTargetOnClick(objectiveElement.Description.gameObject, objective);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


            private static string GetTextForRevenantObjective(FactionObjective objective)
            {
                try
                {

                    string input = objective.GetDescription();
                    string result = "";
                    if (TFTVRevenant.TFTVRevenantResearch.ProjectOsiris)
                    {
                        if (input.Contains("."))
                        {
                            result = input.Split('.')[0];

                        }
                    }
                    else
                    {
                        int revenantPoints = TFTVRevenant.TFTVRevenantResearch.PreviousRevenantPoints + TFTVRevenant.TFTVRevenantResearch.RevenantPoints;
                        result = input + $" {revenantPoints}/{TFTVRevenant.TFTVRevenantResearch.RequieredRevenantPoints}";
                    }

                    return result;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }


            }


            public static void AdjustSecondaryObjectiveTextRevenant(FactionObjective objective, ObjectiveElement objectiveElement)
            {
                try
                {

                    if (IsKillOrCaptureRevenantObjective(objective))
                    {
                        objectiveElement.Description.text = GetTextForRevenantObjective(objective);
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }



            public class UIHider : MonoBehaviour
            {
                private CanvasGroup mainCanvasGroup = _secondaryObjectives.GetComponentInParent<CanvasGroup>();            // Main CanvasGroup containing the UI

                // Dictionary to store original alpha values
                private Dictionary<Graphic, float> originalAlphas = new Dictionary<Graphic, float>();

                public void HideUI()
                {
                    try
                    {
                        if (mainCanvasGroup == null)
                        {
                            TFTVLogger.Always("Cannot hide UI. Main CanvasGroup is missing.");
                            return;
                        }

                        // Ensure the main CanvasGroup alpha is set to 1 to keep the UI interactable
                        mainCanvasGroup.alpha = 1f;


                        UIModuleSquadManagement uIModuleSquadManagement = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>().View.TacticalModules.SquadManagementModule;

                        uIModuleSquadManagement.WillpointsBar.enabled = false;

                        foreach (UIColorController uIColorController in uIModuleSquadManagement.GetComponentsInChildren<UIColorController>(true))
                        {
                            uIColorController.Enabled = false;
                        }

                        // Loop through all Image, Text, and RawImage components in the main CanvasGroup
                        foreach (Graphic graphic in mainCanvasGroup.GetComponentsInChildren<Graphic>(true))
                        {
                            //  TFTVLogger.Always($"{graphic.name}");

                            if (_newObjectiveWidget.CanvasGroup.GetComponentsInChildren<Graphic>(true).Contains(graphic))
                            {
                                continue;
                            }

                            // Store the original alpha if not already stored
                            if (!originalAlphas.ContainsKey(graphic))
                            {
                                originalAlphas[graphic] = graphic.color.a;
                            }

                            // Set alpha to 0 to make invisible
                            Color color = graphic.color;
                            color.a = 0f;
                            graphic.color = color;
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private IEnumerator FadeInGraphic(Graphic graphic, float targetAlpha, float duration)
                {
                    if (graphic == null) yield break;

                    Color color = graphic.color;
                    float startAlpha = color.a;
                    float elapsedTime = 0f;

                    while (elapsedTime < duration)
                    {
                        elapsedTime += Time.deltaTime;
                        float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / duration);
                        color.a = newAlpha;
                        graphic.color = color;
                        yield return null; // Wait for the next frame
                    }

                    // Ensure the final alpha is set
                    color.a = targetAlpha;
                    graphic.color = color;
                }

                public void RestoreUIVisibility(float fadeDuration = 0.5f)
                {
                    try
                    {
                        if (originalAlphas == null || originalAlphas.Count == 0)
                        {
                            return;
                        }

                        foreach (var kvp in originalAlphas)
                        {

                            Graphic graphic = kvp.Key;
                            float originalAlpha = kvp.Value;

                            if (graphic != null && _uIHider != null)
                            {
                                // Start a coroutine to fade in this graphic

                                _uIHider.StartCoroutine(FadeInGraphic(graphic, originalAlpha, fadeDuration));

                            }
                        }

                        // Clear the dictionary after restoring
                        originalAlphas.Clear();

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

            }

            public class CoroutineHelper : MonoBehaviour { }

            public static void ExecuteAfterDelay(float delay, Action callback)
            {
                var helper = new GameObject("CoroutineHelper").AddComponent<CoroutineHelper>();
                helper.StartCoroutine(ExecuteAfterDelayCoroutine(delay, callback, helper));
            }

            private static IEnumerator ExecuteAfterDelayCoroutine(float delay, Action callback, CoroutineHelper helper)
            {
                yield return new WaitForSeconds(delay);
                callback?.Invoke();
                GameObject.Destroy(helper.gameObject); // Clean up helper
            }



            public static void RestoreUI()
            {
                try
                {
                    TacticalLevelController tacticalLevelController = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();
                    tacticalLevelController.View.SelectedActor?.TacticalActorView?.DoCameraChase();
                    UIModuleObjectives uIModuleObjectives = tacticalLevelController.View.TacticalModules.ObjectivesModule;
                    //  TFTVLogger.Always($"_uIHider null? {_uIHider == null}");

                    _uIHider?.RestoreUIVisibility();
                    _objectiveVictoryAnnouncementRunning = false;
                    InitObjectivesTFTV(uIModuleObjectives);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }// Code to execute after 3 seconds
            }

            private static bool CheckCapacityForCaptureObjective(GeoPhoenixFaction phoenixFaction, int spaceRequired)
            {
                try
                {
                    int captureCapacity = AircraftCaptureCapacity;
                    int availableContainmentSpace = phoenixFaction.ContaimentCapacity - phoenixFaction.ContaimentUsage;
                    bool limitedCaptureSetting = TFTVNewGameOptions.LimitedCaptureSetting;

                    if (limitedCaptureSetting && Mathf.Min(captureCapacity, availableContainmentSpace) > spaceRequired || !limitedCaptureSetting && availableContainmentSpace > spaceRequired)
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

            public static void PopulateAvailableObjectives(GeoLevelController controller)
            {
                try
                {
                    GeoPhoenixFaction phoenixFaction = controller.PhoenixFaction;



                    if (TFTVRevenant.DeadSoldiersDelirium.Count > 0 && controller.EventSystem.GetVariable(TFTVRevenant.TFTVRevenantResearch.RevenantCapturedVariable) == 0
                        && CheckCapacityForCaptureObjective(phoenixFaction, 2))
                    {
                        if (!AvailableSecondaryObjectivesTactical.Contains(_captureAnyRevenant.Guid))
                        {
                            AvailableSecondaryObjectivesTactical.Add(_captureAnyRevenant.Guid);
                            //  TFTVLogger.Always($"_captureAnyRevenant available");
                        }
                    }

                    if (phoenixFaction.Research.HasCompleted("PX_Aircraft_EscapePods_ResearchDef") && phoenixFaction.Research.GetResearchById("PX_Alien_LiveQueen_ResearchDef").IsRevealed
                        && CheckCapacityForCaptureObjective(phoenixFaction, 8))
                    {
                        if (!AvailableSecondaryObjectivesTactical.Contains(_captureScylla.Guid))
                        {
                            AvailableSecondaryObjectivesTactical.Add(_captureScylla.Guid);
                            //  TFTVLogger.Always($"_captureScylla available");
                        }
                    }

                    if (phoenixFaction.Research.GetResearchById("PX_Alien_LiveSiren_ResearchDef").IsRevealed)
                    {
                        if (!AvailableSecondaryObjectivesTactical.Contains(_captureSiren.Guid) && CheckCapacityForCaptureObjective(phoenixFaction, 3))
                        {
                            AvailableSecondaryObjectivesTactical.Add(_captureSiren.Guid);
                            //  TFTVLogger.Always($"_captureSiren available");
                        }
                    }

                    if (phoenixFaction.Research.GetResearchById("PX_GooRepeller_ResearchDef").IsRevealed)
                    {
                        if (!AvailableSecondaryObjectivesTactical.Contains(_captureGooAlien.Guid) && CheckCapacityForCaptureObjective(phoenixFaction, 4))
                        {
                            AvailableSecondaryObjectivesTactical.Add(_captureGooAlien.Guid);
                            // TFTVLogger.Always($"_captureGooAlien available");
                        }
                    }

                    if (phoenixFaction.Research.GetResearchById("PX_AlienVirusInfection_ResearchDef").IsRevealed)
                    {
                        if (!AvailableSecondaryObjectivesTactical.Contains(_captureViralAlien.Guid) && CheckCapacityForCaptureObjective(phoenixFaction, 2))
                        {
                            AvailableSecondaryObjectivesTactical.Add(_captureViralAlien.Guid);
                            TFTVLogger.Always($"_captureViralAlien available, Aircraft capture capacity is {AircraftCaptureCapacity}");
                        }
                    }

                    if (phoenixFaction.Research.GetResearchById(TFTVChangesToDLC4Events.MistResearch.Id).IsRevealed)
                    {
                        if (!AvailableSecondaryObjectivesTactical.Contains(_captureMistAlien.Guid) && CheckCapacityForCaptureObjective(phoenixFaction, 2))
                        {
                            AvailableSecondaryObjectivesTactical.Add(_captureMistAlien.Guid);
                            TFTVLogger.Always($"_captureMistAlien available, Aircraft capture capacity is {AircraftCaptureCapacity}");
                        }
                    }


                    if (phoenixFaction.Research.GetResearchById("PX_PyschicAttack_ResearchDef").IsRevealed && CheckCapacityForCaptureObjective(phoenixFaction, 3))
                    {
                        if (!AvailableSecondaryObjectivesTactical.Contains(_capturePsychicAlien.Guid))
                        {
                            AvailableSecondaryObjectivesTactical.Add(_capturePsychicAlien.Guid);
                            // TFTVLogger.Always($"_captureViralAlien available");
                        }
                    }


                    if (phoenixFaction.Research.GetResearchById("PX_Alien_Queen_ResearchDef").IsHidden)
                    {
                        if (!AvailableSecondaryObjectivesTactical.Contains(_killScylla.Guid))
                        {
                            AvailableSecondaryObjectivesTactical.Add(_killScylla.Guid);
                            // TFTVLogger.Always($"_killScylla available");
                        }
                    }

                    if (controller.EventSystem.GetVariable(TFTVRevenant.TFTVRevenantResearch.RevenantCapturedVariable) > 0)
                    {
                        if (!AvailableSecondaryObjectivesTactical.Contains(_killRevenantMimic.Guid))
                        {
                            AvailableSecondaryObjectivesTactical.Add(_killRevenantMimic.Guid);
                            //TFTVLogger.Always($"_killMimic available");
                        }

                        if (!AvailableSecondaryObjectivesTactical.Contains(_killRevenantDybbuk.Guid))
                        {
                            AvailableSecondaryObjectivesTactical.Add(_killRevenantDybbuk.Guid);
                            // TFTVLogger.Always($"_killDybbuk available");
                        }

                        if (!AvailableSecondaryObjectivesTactical.Contains(_killRevenantNemesis.Guid))
                        {
                            AvailableSecondaryObjectivesTactical.Add(_killRevenantNemesis.Guid);
                            //TFTVLogger.Always($"_killNemesis available");
                        }
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            public static void AddAllAvailableSecondaryObjectivesToMission(TacMissionTypeDef missionType)
            {
                try
                {
                    if (!missionType.FinalMission && !missionType.name.Contains("Tutorial") && missionType.ParticipantsData.Any(pd => pd.FactionDef == DefCache.GetDef<PPFactionDef>("Alien_FactionDef")
                        && AvailableSecondaryObjectivesTactical != null && AvailableSecondaryObjectivesTactical.Count > 0))
                    {
                        //  TFTVLogger.Always($"Add {AvailableSecondaryObjectivesTactical.Count} SecondaryObjectives");

                        List<FactionObjectiveDef> listOfFactionObjectives = missionType.CustomObjectives.ToList();

                        listOfFactionObjectives.RemoveAll(obj => _secondaryObjectiveDefsALL.Contains(obj));

                        missionType.CustomObjectives = listOfFactionObjectives.ToArray();

                        foreach (string factionObjectiveDefGUID in AvailableSecondaryObjectivesTactical)
                        {
                            FactionObjectiveDef factionObjectiveDef = (FactionObjectiveDef)Repo.GetDef(factionObjectiveDefGUID);
                            //     TFTVLogger.Always($"Considering Adding Secondary Objective {factionObjectiveDef.name}");

                            if (!missionType.CustomObjectives.Contains(factionObjectiveDef))
                            {
                                TFTVLogger.Always($"Adding Secondary Objective {factionObjectiveDef.name}");
                                listOfFactionObjectives.Add(factionObjectiveDef);
                                if (!_secondaryObjectiveDefsInPlay.Contains(factionObjectiveDef))
                                {
                                    _secondaryObjectiveDefsInPlay.Add(factionObjectiveDef);
                                }
                            }
                        }



                        missionType.CustomObjectives = listOfFactionObjectives.ToArray();
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            internal class Defs
            {

                public static void CreateDefs()
                {
                    try
                    {
                        CreateCaptureRevenantObjective();
                        CreateCaptureGooAlienObjective();
                        CreateCaptureScyllaObjective();
                        CreateCaptureSirenObjective();
                        CreateCapturePsychicAlienObjective();
                        CreateCaptureViralAlienObjective();
                        CreateCaptureMistPandoranObjective();

                        CreateKillScyllaObjective();
                        CreateKillOrCaptureMimic();
                        CreateKillOrCaptureDybbuk();
                        CreateKillOrCaptureNemesis();
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static void CreateCaptureMistPandoranObjective()
                {
                    try
                    {
                        string name = "CaptureMistAlien";
                        string guid = "{E90CF725-1C02-4D48-B2A0-766266FDC132}";
                        GameTagDef tag = TFTVChangesToDLC4Events.MistTag;
                        string descLocKey = "TFTV_KEY_CAPTURE_MIST_OBJECTIVE";
                        int expReward = 300;

                        _captureMistAlien = CreateSecondaryObjectiveCapture(name, guid, tag, descLocKey, expReward);


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }

                }

                private static void CreateKillScyllaObjective()
                {
                    try
                    {

                        string name = "KilLScylla";
                        string guid = "{CB92D1A0-0D3F-49D1-BA26-BE1AE7EA1F03}";
                        GameTagDef tag = DefCache.GetDef<GameTagDef>("Queen_ClassTagDef");
                        string descLocKey = "TFTV_KEY_KILL_SCYLLA_OBJECTIVE";
                        int expReward = 500;

                        _killScylla = CreateSecondaryObjectiveKill(name, guid, tag, descLocKey, expReward, false);

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static void CreateKillOrCaptureMimic()
                {
                    try
                    {
                        string name = "KillMimic";
                        string guid = "{32568945-79C5-41A4-8168-90D1C238BAFE}";

                        GameTagDef tag = TFTVRevenant.RevenantTier1GameTag;
                        string descLocKey = "TFTV_KEY_KILL_REVENANT_OBJECTIVE";
                        string sumLocKey = "TFTV_KEY_KILL_REVENANT_OBJECTIVE_SUMMARY";
                        int expReward = 200;

                        _killRevenantMimic = CreateSecondaryObjectiveKill(name, guid, tag, descLocKey, expReward, false, sumLocKey);

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static void CreateKillOrCaptureDybbuk()
                {
                    try
                    {
                        string name = "KillDybbuk";
                        string guid = "{F2D70D32-D15C-422E-A1EA-E8ED9A8B5334}";

                        GameTagDef tag = TFTVRevenant.RevenantTier2GameTag;
                        string descLocKey = "TFTV_KEY_KILL_REVENANT_OBJECTIVE";
                        string sumLocKey = "TFTV_KEY_KILL_REVENANT_OBJECTIVE_SUMMARY";
                        int expReward = 400;

                        _killRevenantDybbuk = CreateSecondaryObjectiveKill(name, guid, tag, descLocKey, expReward, false, sumLocKey);

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static void CreateKillOrCaptureNemesis()
                {
                    try
                    {
                        string name = "KillNemesis";
                        string guid = "{EF91C6AB-82F1-48E0-868C-421E036A376A}";

                        GameTagDef tag = TFTVRevenant.RevenantTier3GameTag;
                        string descLocKey = "TFTV_KEY_KILL_REVENANT_OBJECTIVE";
                        string sumLocKey = "TFTV_KEY_KILL_REVENANT_OBJECTIVE_SUMMARY";
                        int expReward = 600;

                        _killRevenantNemesis = CreateSecondaryObjectiveKill(name, guid, tag, descLocKey, expReward, false, sumLocKey);

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static void CreateCaptureRevenantObjective()
                {
                    try
                    {
                        string name = "CaptureRevenant";
                        string guid = "{1C220EB6-F02C-467F-8565-B85130B2A641}";

                        GameTagDef tag = TFTVRevenant.AnyRevenantGameTag;
                        string descLocKey = "TFTV_KEY_CAPTURE_REVENANT_OBJECTIVE";
                        int expReward = 300;

                        _captureAnyRevenant = CreateSecondaryObjectiveCapture(name, guid, tag, descLocKey, expReward);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static void CreateCaptureScyllaObjective()
                {
                    try
                    {
                        string name = "CaptureScylla";
                        string guid = "{85DF80E8-DEB0-4719-86CA-86B1BD5B215A}";
                        GameTagDef tag = DefCache.GetDef<GameTagDef>("Queen_ClassTagDef");
                        string descLocKey = "TFTV_KEY_CAPTURE_SCYLLA_OBJECTIVE";
                        int expReward = 300;

                        _captureScylla = CreateSecondaryObjectiveCapture(name, guid, tag, descLocKey, expReward);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static void CreateCaptureSirenObjective()
                {
                    try
                    {
                        string name = "CaptureSiren";
                        string guid = "{7FA442A3-EC6E-4272-A4A3-F12457176028}";
                        GameTagDef tag = DefCache.GetDef<GameTagDef>("Siren_ClassTagDef");
                        string descLocKey = "TFTV_KEY_CAPTURE_SIREN_OBJECTIVE";
                        int expReward = 300;

                        _captureSiren = CreateSecondaryObjectiveCapture(name, guid, tag, descLocKey, expReward);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static void CreateCapturePsychicAlienObjective()
                {
                    try
                    {
                        string name = "CapturePsychicAlien";
                        string guid = "{88732F4A-D028-4A79-B5BF-DB414368DB26}";
                        GameTagDef tag = DefCache.GetDef<GameTagDef>("PsychicBodypart_TagDef");
                        string descLocKey = "TFTV_KEY_CAPTURE_PSYCHIC_OBJECTIVE";
                        int expReward = 300;

                        _capturePsychicAlien = CreateSecondaryObjectiveCapture(name, guid, tag, descLocKey, expReward);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static void CreateCaptureGooAlienObjective()
                {
                    try
                    {
                        string name = "CaptureGooAlien";
                        string guid = "{E1B7F1FE-F431-469B-B164-414872758FDA}";
                        GameTagDef tag = DefCache.GetDef<GameTagDef>("GooBodypart_TagDef");
                        string descLocKey = "TFTV_KEY_CAPTURE_GOO_OBJECTIVE";
                        int expReward = 300;

                        _captureGooAlien = CreateSecondaryObjectiveCapture(name, guid, tag, descLocKey, expReward);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static void CreateCaptureViralAlienObjective()
                {
                    try
                    {
                        string name = "CaptureViralAlien";
                        string guid = "{780CF0CB-6C7F-43DE-BF54-B421EC039243}";
                        GameTagDef tag = DefCache.GetDef<GameTagDef>("ViralBodypart_TagDef");
                        string descLocKey = "TFTV_KEY_CAPTURE_VIRUS_OBJECTIVE";
                        int expReward = 300;

                        _captureViralAlien = CreateSecondaryObjectiveCapture(name, guid, tag, descLocKey, expReward);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static ActorHasStatusFactionObjectiveDef CreateSecondaryObjectiveCapture(string name, string guid, GameTagDef targetTag, string descLocKey, int expReward)
                {
                    try
                    {
                        ActorHasStatusFactionObjectiveDef source =
                            (ActorHasStatusFactionObjectiveDef)Repo.GetDef("2f3ea3b1-49b1-7cbe-ef82-07a75d820259"); //E_CaptureAcheron [StoryCH1_CustomMissionTypeDef]
                        ActorHasStatusFactionObjectiveDef newObjective = Helper.CreateDefFromClone(source, guid, name);
                        newObjective.TargetGameTag = targetTag;
                        newObjective.IsVictoryObjective = false;
                        newObjective.CanRegress = true;
                        newObjective.IsDefeatObjective = false;
                        newObjective.IsUiHidden = true;
                        newObjective.IsUiSummaryHidden = true;
                        newObjective.MissionObjectiveData.Description.LocalizationKey = descLocKey;
                        newObjective.MissionObjectiveData.Summary.LocalizationKey = descLocKey;
                        newObjective.MissionObjectiveData.ExperienceReward = expReward;

                        _secondaryObjectiveDefsALL.Add(newObjective);

                        return newObjective;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                public static KillActorFactionObjectiveDef CreateSecondaryObjectiveKill(string name, string guid, GameTagDef targetTag, string descLocKey, int expReward, bool paralyzedCounts, string summaryKey = null)
                {
                    try
                    {
                        KillActorFactionObjectiveDef source =
                            (KillActorFactionObjectiveDef)Repo.GetDef("bd7a9d69-fbdf-3424-db3e-6566c3595d47"); //KillChiron_CustomMissionObjective]
                        KillActorFactionObjectiveDef newObjective = Helper.CreateDefFromClone(source, guid, name);
                        newObjective.KillTargetGameTag = targetTag;
                        newObjective.IsVictoryObjective = false;
                        newObjective.IsDefeatObjective = false;
                        newObjective.IsUiHidden = false;
                        newObjective.IsUiSummaryHidden = true;
                        newObjective.MissionObjectiveData.Description.LocalizationKey = descLocKey;
                        newObjective.MissionObjectiveData.Summary.LocalizationKey = summaryKey ?? descLocKey;
                        newObjective.MissionObjectiveData.ExperienceReward = expReward;
                        newObjective.ParalysedCounts = paralyzedCounts;

                        _secondaryObjectiveDefsALL.Add(newObjective);

                        return newObjective;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }


            }


            public static bool IsKillOrCaptureRevenantObjective(FactionObjective objective)
            {
                try
                {
                    if (objective.Description.LocalizationKey == _killRevenantMimic.MissionObjectiveData.Description.LocalizationKey)
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

            public static void ClearDataOnGameLoadAndStateChange()
            {
                try
                {
                    _secondaryObjectivesTitle = null;
                    _secondaryObjectives = null;
                    AvailableSecondaryObjectivesTactical?.Clear();
                    _secondaryObjectiveDefsInPlay?.Clear();
                    _objectivesTargetsDictionary?.Clear();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


            public static bool IsSecondaryObjective(FactionObjective objective)
            {
                try
                {
                    if (_secondaryObjectiveDefsInPlay.Any(o => o.MissionObjectiveData.Description.LocalizationKey == objective.Description.LocalizationKey))
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

            private static bool IsNeverRelevantObjective(FactionObjective objective)
            {
                try
                {
                    GameTagDef relevantTag = null;

                    if (objective == null || objective.Faction == null)
                    {
                        return false;
                    }

                    TacticalLevelController controller = objective.Level;

                    if (objective is KillActorFactionObjective killObjective)
                    {
                        TacticalFaction pxFaction = controller.GetFactionByCommandName("px");

                        FieldInfo fieldInfo = typeof(KillActorFactionObjective).GetField("_killTargetsGameTag", BindingFlags.Instance | BindingFlags.NonPublic);

                        if (fieldInfo != null)
                        {
                            relevantTag = (GameTagDef)fieldInfo.GetValue(killObjective);
                        }

                        //    if (relevantTag != null && !controller.GetFactionByCommandName("aln").TacticalActors.Any(ta => ta.HasGameTag(relevantTag))
                        //        && !controller.GetFactionByCommandName("px").TacticalActors.Any(ta => ta.HasGameTag(relevantTag) && ta.Status != null && ta.Status.HasStatus<MindControlStatus>()))

                        if (relevantTag != null && !controller.Factions.Any(f => f.GetRelationTo(pxFaction) == FactionRelation.Enemy && f.TacticalActors.Any(ta => ta.HasGameTag(relevantTag))
                           && !pxFaction.TacticalActors.Any(ta => ta.HasGameTag(relevantTag) && ta.Status != null && ta.Status.HasStatus<MindControlStatus>())))
                        {
                            TFTVLogger.Always($"Not adding {objective.Description.Localize()} because it's never relevant");
                            objective.IsUiSummaryHidden = true;

                            PropertyInfo propertyInfo = typeof(FactionObjective).GetProperty("State", BindingFlags.Instance | BindingFlags.Public);
                            propertyInfo.SetValue(objective, FactionObjectiveState.InProgress);

                            return true;
                        }
                    }

                    return false;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }

            private static Dictionary<TacticalActor, List<FactionObjective>> _pendingObjectivesTargets = new Dictionary<TacticalActor, List<FactionObjective>>();

            private static void RunPendingObjectives()
            {
                try
                {
                    if (_pendingObjectivesTargets.Count > 0)
                    {
                        _pendingObjectivesTargets.Keys.First().StartCoroutine(RunPendingObjectivesCoroutine());
                    }


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            // private static UIActorElement _targetUIActorElement = null;

            private static IEnumerator RunPendingObjectivesCoroutine()
            {
                for (int x = 0; x < _pendingObjectivesTargets.Keys.Count; x++)
                {
                    // Wait for x * 3f seconds
                    yield return new WaitForSeconds(x * 5f);

                    TacticalActor target = _pendingObjectivesTargets.Keys.ElementAt(x);
                    target.TacticalActorView.DoCameraChase(true);

                    /*  _targetUIActorElement = target.TacticalActorViewBase.UIActorElement;

                      //  _targetUIActorElement.SetVisible(true);
                      _targetUIActorElement.GetComponent<HealthbarUIActorElement>().HealthBar.gameObject.SetActive(false);
                      _targetUIActorElement.GetComponent<HealthbarUIActorElement>().ArmorBar.gameObject.SetActive(false);
                      _targetUIActorElement.SetHighlighted(true);*/


                    List<FactionObjective> factionObjectives = _pendingObjectivesTargets[target];

                    List<string> description = new List<string>();

                    bool isRevenant = false;

                    foreach (FactionObjective factionObjective in factionObjectives)
                    {
                        string descriptionToAdd = "";

                        if (factionObjective.Description.LocalizationKey == _captureAnyRevenant.MissionObjectiveData.Description.LocalizationKey)
                        {
                            isRevenant = true;
                        }

                        if (IsKillOrCaptureRevenantObjective(factionObjective))
                        {
                            descriptionToAdd = GetTextForRevenantObjective(factionObjective);
                            isRevenant = true;
                        }
                        else
                        {
                            descriptionToAdd = factionObjective.GetDescription();
                        }

                        description.Add(descriptionToAdd);

                        // TFTVLogger.Always($"RunPendingObjectivesCoroutine: {factionObjective.GetDescription()}, {description.Count()}");
                    }

                    _pendingObjectivesTargets.Clear();

                    Sprite icon = target.TacticalActorViewBase.UIActorElement.GetComponent<HealthbarUIActorElement>().ActorClassIconElement.MainClassIcon.sprite;



                    //  TFTVLogger.Always($"target.ViewElementDef.name: {target.ViewElementDef.name}", false);
                    //target.ViewElementDef.SmallIcon
                    CreateSecondaryObjectiveAnnouncement(description, icon, isRevenant);
                }
            }

            private static bool CheckTargetIsNotToCaptureUncapturableScylla(TacticalActor tacticalActor, FactionObjective objective)
            {
                try
                {
                    ClassTagDef scyllaTag = DefCache.GetDef<ClassTagDef>("Queen_ClassTagDef");

                    return !TFTVNewGameOptions.LimitedCaptureSetting
                        || !tacticalActor.HasGameTag(scyllaTag)
                        || !_secondaryObjectiveDefsInPlay.Contains(_killScylla)
                        || objective.Description.LocalizationKey == _killScylla.MissionObjectiveData.Description.LocalizationKey;



                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }

            private static bool IsRelevantObjective(FactionObjective objective)
            {
                try
                {
                    GameTagDef relevantTag = null;

                    TacticalLevelController controller = objective.Level;

                    if (objective is KillActorFactionObjective killObjective)
                    {
                        FieldInfo fieldInfo = typeof(KillActorFactionObjective).GetField("_killTargetsGameTag", BindingFlags.Instance | BindingFlags.NonPublic);

                        relevantTag = (GameTagDef)fieldInfo.GetValue(killObjective);
                    }
                    else if (objective is ActorHasStatusFactionObjective captureObjective)
                    {
                        FieldInfo fieldInfo = typeof(ActorHasStatusFactionObjective).GetField("_targetsGameTag", BindingFlags.Instance | BindingFlags.NonPublic);
                        relevantTag = (GameTagDef)fieldInfo.GetValue(captureObjective);
                    }

                    //  TacticalFaction aliens = controller.GetFactionByCommandName("aln");
                    TacticalFaction phoenix = controller.GetFactionByCommandName("px");

                    /* if (relevantTag != null)
                     {
                         TFTVLogger.Always($"relevantTag: {relevantTag.name}");

                         foreach (TacticalActor actor in controller.GetFactionByCommandName("aln").TacticalActors)
                         {
                             TFTVLogger.Always($"Alien: {actor.name}");

                             foreach (BodyPartAspect bpa in actor.BodyState.GetAllBodyparts())
                             {
                                 TFTVLogger.Always($"bpa: {bpa?.OwnerItem?.TacticalItemDef?.name}");

                                 foreach (GameTagDef gameTagDef in bpa.OwnerItem.TacticalItemDef.Tags)
                                 {
                                     TFTVLogger.Always($"Tag: {gameTagDef.name}");
                                 }
                             }
                         }
                     }*/
                    List<TacticalActor> enemyActors = controller.Factions
                                        .Where(f => f.GetRelationTo(phoenix) == FactionRelation.Enemy)
                                        .SelectMany(f => f.TacticalActors)
                                        .ToList();


                    List<TacticalActor> relevantActors = enemyActors.
                        Where(ta =>
                        ta.IsAlive && phoenix.Vision.IsRevealed(ta)
                        && TacticalActorHasTagAnywhere(ta, relevantTag)
                        && CheckTargetIsNotToCaptureUncapturableScylla(ta, objective)
                        ).ToList().
                        Concat(phoenix.TacticalActors.Where(ta =>
                        ta.IsAlive
                        && TacticalActorHasTagAnywhere(ta, relevantTag)
                        && CheckTargetIsNotToCaptureUncapturableScylla(ta, objective)
                        && ta.Status != null && ta.Status.HasStatus<MindControlStatus>())).ToList();


                    List<TacticalActor> notCapturableActors = new List<TacticalActor>();

                    if (objective is ActorHasStatusFactionObjective && TFTVNewGameOptions.LimitedCaptureSetting)
                    {

                        //   TFTVLogger.Always($"AircraftCaptureCapacity, ContainmentSpaceAvailable {AircraftCaptureCapacity} {ContainmentSpaceAvailable}");

                        int availableSpace = Math.Min(AircraftCaptureCapacity, ContainmentSpaceAvailable);

                        foreach (TacticalActor tacticalActor in relevantActors)
                        {
                            int requiredSlots = CalculateCaptureSlotCost(tacticalActor.GameTags.ToList());
                            //     TFTVLogger.Always($"required slots is {requiredSlots} for {tacticalActor.name}");
                            if (requiredSlots > availableSpace)
                            {
                                notCapturableActors.Add(tacticalActor);
                            }
                        }
                    }

                    relevantActors.RemoveRange(notCapturableActors);

                    if (relevantTag != null && relevantActors.Count > 0)
                    {
                        if (objective.IsUiSummaryHidden)
                        {
                            TFTVLogger.Always($"considering summary hidden objective {objective.GetDescription()}", false);

                            TacticalActor tacticalActor = relevantActors.FirstOrDefault();

                            if (_pendingObjectivesTargets.ContainsKey(tacticalActor))
                            {
                                _pendingObjectivesTargets[tacticalActor].Add(objective);
                            }
                            else
                            {
                                _pendingObjectivesTargets.Add(tacticalActor, new List<FactionObjective>() { objective });
                            }

                            objective.IsUiHidden = false;
                            objective.IsUiSummaryHidden = false;
                            return false;
                        }

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


            [HarmonyPatch(typeof(ActorHasStatusFactionObjective), "GetTargets")]
            public static class ActorHasStatusFactionObjective_GetTargets_Patch
            {
                public static bool Prefix(ActorHasStatusFactionObjective __instance, GameTagDef ____targetsGameTag, ref IEnumerable<TacticalActorBase> __result)
                {
                    try
                    {
                        if (__instance.Faction == null || !__instance.Level.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("aln")))
                        {
                            return true;
                        }


                        if (_secondaryObjectiveDefsInPlay.Any(o => o.MissionObjectiveData.Description.LocalizationKey == __instance.Description.LocalizationKey)
                            && ____targetsGameTag is ItemTypeTagDef)
                        {
                            List<TacticalActorBase> allValidTargets = new List<TacticalActorBase>();

                            foreach (TacticalActor tacticalActor in __instance.Level.GetFactionByCommandName("aln").TacticalActors.Where(ta => ta.IsAlive && ta.Status != null).Concat
                                (__instance.Level.GetFactionByCommandName("px").TacticalActors.Where(ta => ta.IsAlive && ta.Status != null && ta.Status.HasStatus<MindControlStatus>())))
                            {
                                if (TacticalActorHasTagAnywhere(tacticalActor, ____targetsGameTag))
                                {
                                    allValidTargets.Add(tacticalActor);
                                }
                            }

                            __result = allValidTargets;

                            return false;
                        }

                        return true;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            private static FactionObjectiveState CaptureObjectiveWinEvaluate(ActorHasStatusFactionObjective objective)
            {
                try
                {

                    if (!objective.Faction.HasTacActorsThatCanWin() && !objective.Faction.HasUndeployedTacActors())
                    {
                        return FactionObjectiveState.Failed;
                    }

                    List<TacticalActor> targets = new List<TacticalActor>();

                    FieldInfo fieldInfo = typeof(ActorHasStatusFactionObjective).GetField("_targetsGameTag", BindingFlags.Instance | BindingFlags.NonPublic);

                    GameTagDef targetTag = fieldInfo.GetValue(objective) as GameTagDef;


                    foreach (TacticalActor tacticalActor in objective.Level.GetFactionByCommandName("aln").TacticalActors.Where(ta => ta.IsAlive && ta.Status != null).
                        Concat(objective.Level.GetFactionByCommandName("px").TacticalActors.Where(ta => ta.IsAlive && ta.Status != null && ta.Status.HasStatus<MindControlStatus>())))
                    {
                        if (TacticalActorHasTagAnywhere(tacticalActor, targetTag))
                        {
                            targets.Add(tacticalActor);
                        }
                    }

                    if (!targets.Any())
                    {
                        TFTVLogger.Always($"returning fail state for {objective.Description.Localize()}");
                        return FactionObjectiveState.Failed;
                    }

                    if (targets.Any((TacticalActorBase x) => x.Status.HasStatus<ParalysedStatus>() && (CaptureTacticalWidget.CaptureUI == null || CaptureTacticalWidget.CaptureUI.capturedAliens.Contains(x))))
                    {
                        TFTVLogger.Always($"returning achieved state for {objective.Description.Localize()}");
                        return FactionObjectiveState.Achieved;
                    }

                    TFTVLogger.Always($"returning inprogress state for {objective.Description.Localize()}");
                    return FactionObjectiveState.InProgress;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void ShowObjectiveCompletePanel(FactionObjective objective)
            {
                try
                {
                    //  TFTVLogger.Always($"ShowObjectiveCompletePanel Running");

                    if ((objective is KillActorFactionObjective || objective is KillActorAlienBaseFactionObjective) && objective.State == FactionObjectiveState.Achieved)
                    {
                        // TFTVLogger.Always($"{objective.GetDescription()} {objective.State}");

                        var targets = GetTargetsForObjective(objective, false, false);

                        if (targets == null)
                        {
                            return;
                        }

                        TacticalActor target = targets.FirstOrDefault();

                        if (target == null)
                        {
                            return;
                        }

                        ClassTagDef scyllaTag = DefCache.GetDef<ClassTagDef>("Queen_ClassTagDef");

                        if (_secondaryObjectiveDefsInPlay.Any(def => def.MissionObjectiveData.Description.LocalizationKey == _killScylla.MissionObjectiveData.Description.LocalizationKey)
                            && target.HasGameTag(scyllaTag) && objective.Description.LocalizationKey != _killScylla.MissionObjectiveData.Description.LocalizationKey)
                        {
                            TFTVLogger.Always($"not announcing {objective.Description.Localize()}");
                            return;

                        }

                        target.TacticalActorView.DoCameraChase(true);

                        string descriptionToAdd = objective.GetDescription().ToUpper();
                        bool isRevenant = false;

                        if (IsKillOrCaptureRevenantObjective(objective))
                        {
                            descriptionToAdd = GetTextForRevenantObjective(objective);
                        }

                        List<string> description = new List<string>() { descriptionToAdd };

                        CreateSecondaryObjectiveAnnouncement(description, target.ViewElementDef.SmallIcon, isRevenant, true);

                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static bool TacticalActorHasTagAnywhere(TacticalActor ta, GameTagDef relevantTag)
            {
                try
                {
                    List<GameTagDef> excludTags = new List<GameTagDef>() { DefCache.GetDef<GameTagDef>("SentinelTerror_ClassTagDef"), DefCache.GetDef<GameTagDef>("SentinelMist_ClassTagDef") };


                    if (!ta.GameTags.Any(t => excludTags.Contains(t)) && (ta.HasGameTag(relevantTag)
                            || ta.Equipments.GetWeapons().Any(w => w.WeaponDef.Tags.Contains(relevantTag))
                            || ta.BodyState.GetAllBodyparts().Any(b => b.OwnerItem.TacticalItemDef.Tags.Contains(relevantTag))))
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


            private static bool CheckCaptureObjectiveCompleted(ActorHasStatusFactionObjective captureObjective)
            {
                try
                {
                    TacticalFaction alienFaction = captureObjective.Faction.TacticalLevel.GetFactionByCommandName("aln");

                    var captureTargetsGameTagField = AccessTools.Field(typeof(ActorHasStatusFactionObjective), "_targetsGameTag");

                    GameTagDef captureTag = (GameTagDef)captureTargetsGameTagField.GetValue(captureObjective);




                    if (!alienFaction.TacticalActors.Any(ta => TacticalActorHasTagAnywhere(ta, captureTag)
                    && !ta.IsEvacuated && ta.Status != null
                    && ta.Status.HasStatus<ParalysedStatus>()))
                    {
                        return false;
                    }

                    List<TacticalActor> eligibleAliens = alienFaction.TacticalActors.Where(ta => TacticalActorHasTagAnywhere(ta, captureTag)
                    && !ta.IsEvacuated && ta.Status != null
                    && ta.Status.HasStatus<ParalysedStatus>()).ToList();

                    if (CaptureTacticalWidget.CaptureUI == null)
                    {
                        return true;
                    }
                    else if (eligibleAliens.Any(ta => CaptureTacticalWidget.CaptureUI.capturedAliens.Contains(ta)))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


            [HarmonyPatch(typeof(ActorHasStatusFactionObjective), "EvaluateObjective")]
            public static class ActorHasStatusFactionObjective_EvaluateObjective_Patch
            {
                public static void Postfix(ActorHasStatusFactionObjective __instance, ref FactionObjectiveState __result)
                {
                    try
                    {
                        if (_secondaryObjectiveDefsInPlay.Any(o =>
                        o.MissionObjectiveData.Description.LocalizationKey == __instance.Description.LocalizationKey) && __result == FactionObjectiveState.Failed)
                        {
                            // TFTVLogger.Always($"{__instance.GetDescription()} {__result}");

                            if (CheckCaptureObjectiveCompleted(__instance))
                            {
                                __result = FactionObjectiveState.Achieved;
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


            [HarmonyPatch(typeof(FactionObjective), "Evaluate")]
            public static class FactionObjective_Evaluate_Patch
            {
                public static bool Prefix(FactionObjective __instance, out FactionObjectiveState __state)
                {
                    try
                    {
                        // Capture the initial state before Evaluate runs
                        __state = __instance.State;

                        // TFTVLogger.Always($"Objective {__instance.Description.Localize()} state {__instance.State}");

                        if (__instance.Faction != null && __instance.Faction.State == TacFactionState.Won)
                        {
                            TFTVLogger.Always($"Objective {__instance.Description.Localize()} state {__instance.State}");

                            if (__instance is ActorHasStatusFactionObjective captureObjective &&
                                    _secondaryObjectiveDefsInPlay.Any(o => o.MissionObjectiveData.Description.LocalizationKey == __instance.Description.LocalizationKey))
                            {
                                TFTVLogger.Always($"Got here and examining {__instance.Description.Localize()}");

                                if (!CheckCaptureObjectiveCompleted(captureObjective))
                                {
                                    TFTVLogger.Always($"Objective {__instance.Description.Localize()} should fail!");
                                    return false;
                                }
                            }
                            else if (__instance is KillActorFactionObjective killObjective &&
                                   _secondaryObjectiveDefsInPlay.Any(o => o.MissionObjectiveData.Description.LocalizationKey == __instance.Description.LocalizationKey))
                            {
                                // TFTVLogger.Always($"Not going to evaluate {__instance.Description.Localize()}");
                                TFTVLogger.Always($"Got here and examining {__instance.Description.Localize()}");
                                var killTargetsGameTagField = AccessTools.Field(typeof(KillActorFactionObjective), "_killTargetsGameTag");

                                GameTagDef killTag = (GameTagDef)killTargetsGameTagField.GetValue(killObjective);

                                if (!CheckIfActorDeadNotEvaced(__instance.Faction.TacticalLevel, killTag))
                                {
                                    TFTVLogger.Always($"Objective {__instance.Description.Localize()} should fail!");
                                    return false;
                                }
                            }


                        }

                        return true;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }

                }

                public static void Postfix(FactionObjective __instance, ref FactionObjectiveState __result, FactionObjectiveState __state)
                {
                    try
                    {
                        if (__instance is EvacuateFactionObjective && __instance.State == FactionObjectiveState.Achieved)
                        {
                            TFTVLogger.Always($"EvacuateFactionObjective: Objective {__instance.Description.Localize()} state {__instance.State}");

                            foreach (FactionObjective factionObjective in __instance.Faction.Objectives)
                            {
                                //  TFTVLogger.Always($"examining in Postfix  {factionObjective?.Description?.Localize()}");

                                if (factionObjective is KillActorFactionObjective killObjective2 &&
                                    _secondaryObjectiveDefsInPlay.Any(o => o.MissionObjectiveData.Description.LocalizationKey == killObjective2.Description.LocalizationKey))
                                {
                                    // TFTVLogger.Always($"Evac! Got here and examining {factionObjective.Description.Localize()}");

                                    var killTargetsGameTagField = AccessTools.Field(typeof(KillActorFactionObjective), "_killTargetsGameTag");

                                    GameTagDef killTag = (GameTagDef)killTargetsGameTagField.GetValue(killObjective2);

                                    if (CheckIfActorDeadNotEvaced(__instance.Faction.TacticalLevel, killTag))
                                    {
                                        var property = typeof(FactionObjective).GetProperty("State", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                                        property.SetValue(killObjective2, FactionObjectiveState.Achieved);

                                        TFTVLogger.Always($"Objective {killObjective2.Description.Localize()} should succeed, state is {killObjective2.State}");

                                    }
                                }
                            }
                        }


                        ModifySecondaryObjectivesEvaluateBehavior(__instance, ref __result, __state);
                        // TFTVVanillaFixes.ModifyVehicleRescueEvaluateBehavior(__instance, ref __result);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            public static void ModifySecondaryObjectivesEvaluateBehavior(FactionObjective __instance, ref FactionObjectiveState __result, FactionObjectiveState __state)
            {
                try
                {


                    if (__instance.Faction != null &&
                        __instance.Level.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("aln")))
                    {


                        if (__state != FactionObjectiveState.Achieved)
                        {
                            ShowObjectiveCompletePanel(__instance);
                        }

                        if (__instance.Faction != null && __instance.Faction.State == TacFactionState.Won)

                        {
                            if (__instance is ActorHasStatusFactionObjective captureObjective &&
                                _secondaryObjectiveDefsInPlay.Any(o => o.MissionObjectiveData.Description.LocalizationKey == __instance.Description.LocalizationKey))
                            {
                                TFTVLogger.Always($"{__instance.Description.Localize()} Initial State: {__state}, faction state: {__instance.Faction.State}");

                                if (__instance.Faction.State == TacFactionState.Won)
                                {
                                    // Check if this specific objective should be marked as Achieved
                                    __result = CaptureObjectiveWinEvaluate(captureObjective);
                                }

                                if (__result == FactionObjectiveState.Failed)
                                {
                                    __result = FactionObjectiveState.InProgress;
                                }
                            }
                            else if (__instance is KillActorFactionObjective killObjective &&
                                _secondaryObjectiveDefsInPlay.Any(o => o.MissionObjectiveData.Description.LocalizationKey == __instance.Description.LocalizationKey))
                            {

                                if (__instance.Faction.State == TacFactionState.Won)
                                {
                                    TFTVLogger.Always($"Got here and examining {__instance.Description.Localize()}");
                                    var killTargetsGameTagField = AccessTools.Field(typeof(KillActorFactionObjective), "_killTargetsGameTag");

                                    GameTagDef killTag = (GameTagDef)killTargetsGameTagField.GetValue(killObjective);

                                    if (CheckIfActorDeadNotEvaced(__instance.Faction.TacticalLevel, killTag))
                                    {
                                        TFTVLogger.Always($"Objective {__instance.Description.Localize()} achieved!");
                                        __result = FactionObjectiveState.Achieved;
                                    }
                                    else
                                    {
                                        TFTVLogger.Always($"Objective {__instance.Description.Localize()} should fail!");
                                        __result = FactionObjectiveState.Failed;
                                    }

                                }

                            }

                        }
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static bool CheckIfActorDeadNotEvaced(TacticalLevelController controller, GameTagDef killTag)
            {
                try
                {
                    //TacticalFaction alienFaction = controller.GetFactionByCommandName("aln");

                    List<TacticalActor> enemyActors = controller.Factions
                                        .Where(f => f.GetRelationTo(controller.GetFactionByCommandName("px")) == FactionRelation.Enemy)
                                        .SelectMany(f => f.TacticalActors)
                                        .ToList();

                    TFTVLogger.Always($"paralyzed enemy actor with killTag? " +
                        $"{enemyActors.Any(ta => ta.HasGameTag(killTag) && !ta.IsEvacuated && (ta.IsDead || ta.Status != null && ta.Status.HasStatus<ParalysedStatus>()))}");

                    return enemyActors.Any(ta => ta.HasGameTag(killTag) && !ta.IsEvacuated && (ta.IsDead || ta.Status != null && ta.Status.HasStatus<ParalysedStatus>()));

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }

            private static void InitObjectivesTFTV(UIModuleObjectives __instance)
            {
                try
                {
                    if (_secondaryObjectives == null)
                    {
                        CreateSecondaryObjectivesWidget(__instance);
                    }

                    TacticalLevelController tacticalLevelController = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();
                    // Get the objectives from the context
                    ObjectivesManager objectives = tacticalLevelController.CurrentFaction.Objectives;

                    objectives.Evaluate();

                    // Separate primary and secondary objectives
                    List<FactionObjective> primaryObjectives = objectives.Where(p => !p.IsUiHidden && !IsSecondaryObjective(p)).ToList();
                    List<FactionObjective> secondaryObjectives = objectives.Where(p => IsSecondaryObjective(p) && IsRelevantObjective(p)).ToList();

                    RunPendingObjectives();

                    if (secondaryObjectives.Count == 0)
                    {
                        _secondaryObjectivesTitle.gameObject.SetActive(false);
                    }
                    else
                    {
                        _secondaryObjectivesTitle.gameObject.SetActive(true);
                    }

                    // Update primary objectives
                    UIUtil.EnsureActiveComponentsInContainer(__instance.ObjectivesContainer, __instance.ObjectivePrefab, primaryObjectives, (element, objective) =>
                    {
                        element.SetObjective(objective);
                    });

                    // Update secondary objectives
                    UIUtil.EnsureActiveComponentsInContainer(_secondaryObjectives, __instance.ObjectivePrefab, secondaryObjectives, (element, objective) =>
                    {
                        element.SetObjective(objective);
                    });



                    // FailsafeRestoreUI();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }



            }

            private static void CreateSecondaryObjectivesWidget(UIModuleObjectives moduleObjectives)
            {
                try
                {
                    TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();
                    UIModuleNavigation uIModuleNavigation = controller.View.TacticalModules.NavigationModule;

                    Resolution resolution = Screen.currentResolution;
                    float resolutionFactorWidth = (float)resolution.width / 1920f;
                    float resolutionFactorHeight = (float)resolution.height / 1080f;

                    Transform primaryObjectivesContainer = moduleObjectives.ObjectivesContainer;
                    Vector3 primaryPosition = primaryObjectivesContainer.position;

                    //   TFTVLogger.Always($"primaryPosition: {primaryPosition}");

                    _secondaryObjectivesTitle = UnityEngine.Object.Instantiate(moduleObjectives.transform.Find("Objectives_Text"), uIModuleNavigation.transform);
                    _secondaryObjectivesTitle.position = new Vector3(144 * resolutionFactorWidth, 900 * resolutionFactorHeight, 0.0f);

                    _secondaryObjectives = UnityEngine.Object.Instantiate(moduleObjectives.ObjectivesContainer, _secondaryObjectivesTitle);

                    //  TFTVLogger.Always($"_secondaryObjectives.GetComponent<RectTransform>().position: {_secondaryObjectives.GetComponent<RectTransform>().position}" +
                    //      $"_secondaryObjectives.GetComponent<RectTransform>().anchoredPosition: {_secondaryObjectives.GetComponent<RectTransform>().anchoredPosition}");



                    _secondaryObjectives.GetComponent<RectTransform>().position = new Vector3(primaryPosition.x, _secondaryObjectivesTitle.position.y + 35 * resolutionFactorHeight, 0);

                    // _secondaryObjectives.transform.position = new Vector3(primaryPosition.x, primaryPosition.y - 50 * resolutionFactorHeight, primaryPosition.z);

                    foreach (Component component in _secondaryObjectives.GetComponentsInChildren<Component>())
                    {
                        if (component is Image image && image.name.Equals("Image"))
                        {
                            image.color = new Color(0, 0, 0, 0);
                        }

                        //  TFTVLogger.Always($"{component.name} {component.GetType()}");
                    }


                    foreach (Component component in _secondaryObjectivesTitle.GetComponents<Component>())
                    {
                        if (component is Localize localize)
                        {
                            localize.enabled = false;
                        }
                        if (component is Text text)
                        {
                            /*   GlowEffect glowEffect = component.gameObject.AddComponent<GlowEffect>();
                               //  TFTVLogger.Always($"glowEffect==null? {glowEffect == null}");
                               glowEffect.Start();*/

                            text.font = moduleObjectives.ObjectivePrefab.GetComponentInChildren<Text>().font;
                            text.text = TFTVCommonMethods.ConvertKeyToString("TFTV_KEY_SECONDARY_OBJECTIVES");
                        }
                        if (component is UIColorController colorController)
                        {
                            colorController.Enabled = false;
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            [HarmonyPatch(typeof(UIModuleObjectives), "Init")]
            public static class UIModuleObjectives_Init_Patch
            {
                public static void Prefix(UIModuleObjectives __instance, TacticalViewContext Context)
                {
                    try
                    {
                        ObjectivesManager objectives = Context.LevelController.CurrentFaction.Objectives;

                        List<FactionObjective> objectivesToRemove = new List<FactionObjective>();

                        foreach (FactionObjective factionObjective in objectives)
                        {
                            if (IsSecondaryObjective(factionObjective) && IsNeverRelevantObjective(factionObjective))
                            {
                                objectivesToRemove.Add(factionObjective);
                            }
                        }

                        foreach (FactionObjective factionObjective1 in objectivesToRemove)
                        {
                            objectives.Remove(factionObjective1);

                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }


                public static void Postfix(UIModuleObjectives __instance)
                {
                    try
                    {
                        InitObjectivesTFTV(__instance);

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }

                }
            }
        }

        internal class Enemies
        {

            private static Sprite _umbraArthronIcon = null;
            private static Sprite _umbraTritonIcon = null;

            private static Dictionary<string, OpposingLeaderWidget> _leaderWidgets = new Dictionary<string, OpposingLeaderWidget>();

            public static void SetUmbraIcons()
            {
                try
                {
                    _umbraArthronIcon = Helper.CreateSpriteFromImageFile("umbra_arthron_icon.png");
                    _umbraTritonIcon = Helper.CreateSpriteFromImageFile("umbra_triton_icon.png");

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }

            public static Sprite GetUmbraArthronIcon()
            {
                try
                {
                    return _umbraArthronIcon;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }

            public static Sprite GetUmbraTritonIcon()
            {
                try
                {
                    return _umbraTritonIcon;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }


            public static void ClearData()
            {
                try
                {
                    TFTVLogger.Always($"enemies data cleared");
                    _leaderWidgets.Clear();

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            /// <summary>
            /// This is used to add rank triangles in the info panel and color leaders gold
            /// </summary>
            /// <param name="actorClassIconElement"></param>
            /// <param name="rankTag"></param>
            /// <param name="friendly"></param>
            public static void AdjustIconInfoPanel(ActorClassIconElement actorClassIconElement, GameTagDef rankTag, bool friendly)
            {

                try
                {

                    //the ranks are a mess... because I made tags that go from 4 to 1 from rookie to boss, but for the rank triangles 4 actually means boss and 1 means rookie.
                    //I'm an idiot, what can say? but fixing it too much work...

                    RankIconCreator rankIconCreator = new RankIconCreator();

                    int rank = 1;


                    if (rankTag == TFTVHumanEnemies.HumanEnemyTier1GameTag)
                    {
                        rank = 4;
                        actorClassIconElement.MainClassIcon.color = LeaderColor;
                    }
                    else
                    {
                        // actorClassIconElement.MainClassIcon.color = NegativeColor;

                        if (rankTag == TFTVHumanEnemies.HumanEnemyTier2GameTag)
                        {
                            rank = 3;
                        }
                        else if (rankTag == TFTVHumanEnemies.HumanEnemyTier3GameTag)
                        {
                            rank = 2;
                        }
                    }

                    rankIconCreator.SetIconWithRank(actorClassIconElement.MainClassIcon.gameObject, actorClassIconElement.MainClassIcon.sprite, rank, true, false, false, friendly);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            /// <summary>
            /// This is used to remove rank triangles from the info panel, necessary for cleanup
            /// </summary>
            /// <param name="actorClassIconElement"></param>
            public static void RemoveRankFromInfoPanel(ActorClassIconElement actorClassIconElement)
            {

                try
                {
                    RankIconCreator rankIconCreator = new RankIconCreator();
                    rankIconCreator.RemoveRankTriangles(actorClassIconElement.MainClassIcon.gameObject);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }




            public class RankIconCreator : MonoBehaviour
            {
                public Sprite topLeftTriangleSprite = Helper.CreateSpriteFromImageFile("rank_1.png");     // Triangle for the top-left corner
                public Sprite topRightTriangleSprite = Helper.CreateSpriteFromImageFile("rank_2.png");       // Triangle for the top-right corner
                public Sprite bottomLeftTriangleSprite = Helper.CreateSpriteFromImageFile("rank_4.png");     // Triangle for the bottom-left corner
                public Sprite bottomRightTriangleSprite = Helper.CreateSpriteFromImageFile("rank_3.png");    // Triangle for the bottom-right corner

                public void AddRankTriangles(GameObject iconObject, int rank, bool bigCorners = false, bool noLOSColor = false, bool shootState = false, bool friendly = false)
                {
                    try
                    {
                        Color color = NegativeColor;

                        if (rank == 4)
                        {
                            color = LeaderColor;
                        }

                        if (noLOSColor)
                        {
                            color = Color.gray;
                        }

                        if (friendly)
                        {
                            color = WhiteColor;
                        }


                        Sprite[] cornerSprites = {
            topLeftTriangleSprite,    // Rank 1: Top-left corner
            topRightTriangleSprite,   // Rank 2: Top-right corner
            bottomLeftTriangleSprite, // Rank 3: Bottom-left corner
            bottomRightTriangleSprite // Rank 4: Bottom-right corner
        };

                        Vector2[] cornerPositions = {
            new Vector2(0, 1), // Top-left
            new Vector2(1, 1), // Top-right
            new Vector2(0, 0), // Bottom-left
            new Vector2(1, 0)  // Bottom-right
        };



                        Vector2[] offsetPositions = new Vector2[4];

                        if (!bigCorners && !shootState)
                        {
                            offsetPositions = new Vector2[] {
            new Vector2(7, -7),   // Offset for top-left
            new Vector2(-7, -7),  // Offset for top-right
            new Vector2(7, 7),    // Offset for bottom-left
            new Vector2(-7, 7)    // Offset for bottom-right
                                  
                        };
                        }

                        if (shootState)
                        {
                            offsetPositions = new Vector2[] {
            new Vector2(35, -70),   // Offset for top-left
            new Vector2(-35, -70),  // Offset for top-right
            new Vector2(35, 70),    // Offset for bottom-left
            new Vector2(-35, 70)    // Offset for bottom-right
                            };

                        }
                        else if (bigCorners)
                        {

                            offsetPositions = new Vector2[] {
            new Vector2(14, -14),   // Offset for top-left
            new Vector2(-14, -14),  // Offset for top-right
            new Vector2(14, 14),    // Offset for bottom-left
            new Vector2(-14, 14)    // Offset for bottom-right
        };
                        }




                        for (int i = 0; i < rank; i++)
                        {
                            CreateTriangleSpriteAtCorner(iconObject, cornerSprites[i], cornerPositions[i], offsetPositions[i], color, bigCorners, shootState);
                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private void CreateTriangleSpriteAtCorner(GameObject parentIcon, Sprite triangleSprite, Vector2 anchorPosition, Vector2 offset, Color color, bool bigCorners = false, bool shootState = false)
                {
                    try
                    {
                        GameObject triangleIcon = new GameObject("RankTriangle");
                        triangleIcon.transform.SetParent(parentIcon.transform, false);

                        // Add Image component and assign the specific corner sprite
                        Image triangleImage = triangleIcon.AddComponent<Image>();
                        triangleImage.sprite = triangleSprite;
                        triangleImage.color = color; // Set color if needed


                        if (bigCorners)
                        {

                            AddOutlineToIcon addOutlineToIcon = triangleIcon.GetComponent<AddOutlineToIcon>() ?? triangleIcon.AddComponent<AddOutlineToIcon>();
                            addOutlineToIcon.icon = triangleIcon;
                            addOutlineToIcon.InitOrUpdate();
                        }



                        // Set RectTransform to position the triangle in the specified corner with an offset
                        RectTransform rectTransform = triangleIcon.GetComponent<RectTransform>();
                        rectTransform.anchorMin = anchorPosition;
                        rectTransform.anchorMax = anchorPosition;
                        rectTransform.pivot = new Vector2(0.5f, 0.5f);


                        if (shootState)
                        {
                            rectTransform.sizeDelta = new Vector2(30, 30);

                        }
                        else if (bigCorners)
                        {
                            rectTransform.sizeDelta = new Vector2(14, 14);
                        }
                        else
                        {
                            rectTransform.sizeDelta = new Vector2(7, 7);
                        }

                        rectTransform.anchoredPosition = offset;


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                public void SetIconWithRank(GameObject iconObject, Sprite iconSprite, int rank, bool biggerCorner = false, bool noLOSColor = false, bool shootState = false, bool friendly = false)
                {
                    try
                    {
                        Image iconImage = iconObject.GetComponent<Image>();
                        if (iconImage == null)
                        {
                            iconImage = iconObject.AddComponent<Image>();
                        }
                        iconImage.sprite = iconSprite;

                        AddRankTriangles(iconObject, rank, biggerCorner, noLOSColor, shootState, friendly);

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                public void RemoveRankTriangles(GameObject iconObject)
                {
                    try
                    {
                        // Find all child objects named "RankTriangle" and remove them
                        foreach (Transform child in iconObject.transform)
                        {
                            if (child.name == "RankTriangle")
                            {
                                Destroy(child.gameObject);
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

            /// <summary>
            /// This adjusts how class icon is displayed next to the HP bar, also during FPS aiming
            /// </summary>
            /// <param name="actorClassIconElement"></param>
            /// <param name="tacticalActorBase"></param>
            /// 

            public static bool IsReallyEnemy(TacticalActorBase tacticalActorBase)
            {
                try
                {


                    MindControlStatus mindControlStatus = tacticalActorBase.Status?.GetStatus<MindControlStatus>();

                    if (mindControlStatus != null)
                    {
                        TacticalFaction originalFaction = tacticalActorBase.TacticalLevel.GetFactionByCommandName(mindControlStatus.OriginalFaction.FactionDef.ShortName);

                        if (originalFaction.GetRelationTo(tacticalActorBase.TacticalLevel.GetFactionByCommandName("px")) == FactionRelation.Enemy)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (tacticalActorBase.TacticalFaction.GetRelationTo(tacticalActorBase.TacticalLevel.GetFactionByCommandName("px")) == FactionRelation.Enemy)
                        {
                            return true;
                        }
                    }

                    return false;


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


            private static int GetAncientsChargeLevelFromWP(TacticalActor tacticalActor)
            {
                try
                {
                    ClassTagDef cyclopsTag = DefCache.GetDef<ClassTagDef>("MediumGuardian_ClassTagDef");
                    ClassTagDef hopliteTag = DefCache.GetDef<ClassTagDef>("HumanoidGuardian_ClassTagDef");

                    int rank = 0;

                    if (tacticalActor.HasGameTag(hopliteTag))
                    {

                        if (tacticalActor.CharacterStats.WillPoints >= 30)
                        {
                            rank = 4;
                        }
                        else if (tacticalActor.CharacterStats.WillPoints >= 25)
                        {
                            rank = 3;
                        }
                        else if (tacticalActor.CharacterStats.WillPoints >= 20)
                        {
                            rank = 2;
                        }
                        else if (tacticalActor.CharacterStats.WillPoints >= 15)
                        {
                            rank = 1;
                        }
                    }

                    if (tacticalActor.HasGameTag(cyclopsTag))
                    {

                        if (tacticalActor.CharacterStats.WillPoints >= 40)
                        {
                            rank = 4;
                        }
                        else if (tacticalActor.CharacterStats.WillPoints >= 30)
                        {
                            rank = 3;
                        }
                        else if (tacticalActor.CharacterStats.WillPoints >= 20)
                        {
                            rank = 2;
                        }
                        else if (tacticalActor.CharacterStats.WillPoints >= 15)
                        {
                            rank = 1;
                        }

                    }

                    return rank;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            public static void ImplementAncientsChargeLevel(ActorClassIconElement actorClassIconElement, TacticalActorBase tacticalActorBase, bool hasNoLos = false)
            {
                try
                {
                    ClassTagDef cyclopsTag = DefCache.GetDef<ClassTagDef>("MediumGuardian_ClassTagDef");
                    ClassTagDef hopliteTag = DefCache.GetDef<ClassTagDef>("HumanoidGuardian_ClassTagDef");

                    TacticalActor tacticalActor = tacticalActorBase as TacticalActor;

                    if (tacticalActor == null || (!tacticalActor.HasGameTag(cyclopsTag) && !tacticalActor.HasGameTag(hopliteTag)))
                    {
                        return;
                    }

                    bool shootState = actorClassIconElement.MainClassIcon.rectTransform.sizeDelta.x > 100;


                    RankIconCreator rankIconCreator = new RankIconCreator();
                    rankIconCreator.RemoveRankTriangles(actorClassIconElement.MainClassIcon.gameObject);

                    int rank = GetAncientsChargeLevelFromWP(tacticalActor);


                    if (rank > 0)
                    {
                        rankIconCreator.SetIconWithRank(actorClassIconElement.MainClassIcon.gameObject,
                               actorClassIconElement.MainClassIcon.sprite, rank, true, hasNoLos, shootState);
                    }

                    if (hasNoLos)
                    {
                        actorClassIconElement.MainClassIcon.color = _regularNoLOSColor;
                    }
                    else
                    {

                        if (rank == 4)
                        {
                            actorClassIconElement.MainClassIcon.color = LeaderColor;
                        }
                        else
                        {
                            actorClassIconElement.MainClassIcon.color = NegativeColor;

                        }
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }



            private static int GetRankFromHumanEnemyTag(TacticalActorBase tacticalActorBase)
            {
                try
                {
                    int rank = 4;

                    if (tacticalActorBase.HasGameTag(TFTVHumanEnemies.HumanEnemyTier2GameTag))
                    {
                        rank = 3;
                    }
                    else if (tacticalActorBase.HasGameTag(TFTVHumanEnemies.HumanEnemyTier3GameTag))
                    {
                        rank = 2;
                    }
                    else if (tacticalActorBase.HasGameTag(TFTVHumanEnemies.HumanEnemyTier4GameTag))
                    {
                        rank = 1;
                    }

                    return rank;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }


            }

            /// <summary>
            /// This method is for adjusting the actor's icon NOT in the targets panel
            /// </summary>
            /// <param name="actorClassIconElement"></param>
            /// <param name="tacticalActorBase"></param>
            public static void ChangeHealthBarIcon(ActorClassIconElement actorClassIconElement, TacticalActorBase tacticalActorBase)
            {
                try
                {
                    RankIconCreator rankIconCreator = new RankIconCreator();
                    rankIconCreator.RemoveRankTriangles(actorClassIconElement.MainClassIcon.gameObject);

                    if (tacticalActorBase.HasGameTag(TFTVHumanEnemies.humanEnemyTagDef))
                    {
                        int rank = GetRankFromHumanEnemyTag(tacticalActorBase);

                        bool shootState = actorClassIconElement.MainClassIcon.rectTransform.sizeDelta.x > 100;
                        bool isFriendly = false;

                        Color color = NegativeColor;

                        if (rank == 4)
                        {
                            color = LeaderColor;
                        }

                        if (IsReallyEnemy(tacticalActorBase))
                        {
                            actorClassIconElement.MainClassIcon.color = color;
                        }
                        else
                        {
                            actorClassIconElement.MainClassIcon.color = WhiteColor;
                            isFriendly = true;
                        }

                        rankIconCreator.SetIconWithRank(actorClassIconElement.MainClassIcon.gameObject,
                         actorClassIconElement.MainClassIcon.sprite, rank, true, false, shootState, isFriendly);

                        // actorClassIconElement.MainClassIcon.color = color;

                    }
                    else if (tacticalActorBase.GameTags.Contains(TFTVRevenant.AnyRevenantGameTag))
                    {
                        actorClassIconElement.MainClassIcon.color = LeaderColor;
                        actorClassIconElement.SecondaryClassIcon.color = LeaderColor;
                    }
                    else
                    {
                        if (IsReallyEnemy(tacticalActorBase))
                        {
                            if (tacticalActorBase.ActorDef.name.Equals("Oilcrab_ActorDef") || tacticalActorBase.ActorDef.name.Equals("Oilfish_ActorDef"))
                            {
                                actorClassIconElement.MainClassIcon.color = VoidColor;
                            }
                            else
                            {
                                actorClassIconElement.MainClassIcon.color = NegativeColor;
                            }
                        }
                        else
                        {
                            actorClassIconElement.MainClassIcon.color = WhiteColor;
                        }
                    }

                    ImplementAncientsChargeLevel(actorClassIconElement, tacticalActorBase);


                    AddOutlineToIcon addOutlineToIcon = actorClassIconElement.MainClassIcon.GetComponent<AddOutlineToIcon>() ?? actorClassIconElement.MainClassIcon.gameObject.AddComponent<AddOutlineToIcon>();
                    addOutlineToIcon.icon = actorClassIconElement.MainClassIcon.gameObject;
                    addOutlineToIcon.InitOrUpdate();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }


            public static void ManageRankIconToSpottedEnemies(SpottedTargetsElement spottedTargetsElement, GameObject obj, TacticalActorBase target)
            {
                try
                {
                    ActorClassIconElement actorClassIconElement = obj.GetComponentInChildren<ActorClassIconElement>();
                    RankIconCreator rankIconCreator = new RankIconCreator();
                    rankIconCreator.RemoveRankTriangles(actorClassIconElement.MainClassIcon.gameObject);

                    bool hasNoLOS = false;

                    if (spottedTargetsElement.UiSpottedEnemyNoLosButton.isActiveAndEnabled)
                    {
                        hasNoLOS = true;
                    }

                    if (target.HasGameTag(TFTVHumanEnemies.humanEnemyTagDef))
                    {
                        int rank = GetRankFromHumanEnemyTag(target);

                        Color color = actorClassIconElement.MainClassIcon.color;

                        bool friendly = false;

                        if (hasNoLOS)
                        {
                            color = _regularNoLOSColor;
                        }
                        else
                        {
                            if (rank == 4)
                            {
                                color = LeaderColor;
                            }
                            else
                            {
                                if (IsReallyEnemy(target))
                                {
                                    color = NegativeColor;
                                }
                                else
                                {
                                    color = WhiteColor;
                                    friendly = true;
                                }

                                // color = NegativeColor;
                            }

                        }

                        rankIconCreator.SetIconWithRank(actorClassIconElement.MainClassIcon.gameObject,
                          actorClassIconElement.MainClassIcon.sprite, rank, true, hasNoLOS, false, friendly);

                        actorClassIconElement.MainClassIcon.color = color;
                    }
                    else if (target.GameTags.Contains(TFTVRevenant.AnyRevenantGameTag))
                    {
                        Color color = actorClassIconElement.MainClassIcon.color;

                        if (hasNoLOS)
                        {
                            color = _regularNoLOSColor;
                        }
                        else
                        {

                            color = LeaderColor;
                        }

                        actorClassIconElement.MainClassIcon.color = color;

                    }
                    else if (IsReallyEnemy(target))
                    {

                        Color color = actorClassIconElement.MainClassIcon.color;

                        if (hasNoLOS)
                        {
                            color = _regularNoLOSColor;
                        }
                        else
                        {
                            if (target.ActorDef.name.Equals("Oilcrab_ActorDef") || target.ActorDef.name.Equals("Oilfish_ActorDef"))
                            {

                                color = VoidColor;
                            }
                            else
                            {
                                color = NegativeColor;
                            }
                        }

                        actorClassIconElement.MainClassIcon.color = color;

                    }
                    else
                    {
                        actorClassIconElement.MainClassIcon.color = WhiteColor;

                    }

                    ImplementAncientsChargeLevel(actorClassIconElement, target, hasNoLOS);

                    AddOutlineToIcon addOutlineToIcon = actorClassIconElement.MainClassIcon.GetComponent<AddOutlineToIcon>() ?? actorClassIconElement.MainClassIcon.gameObject.AddComponent<AddOutlineToIcon>();
                    addOutlineToIcon.icon = actorClassIconElement.MainClassIcon.gameObject;
                    addOutlineToIcon.InitOrUpdate();


                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }



            [HarmonyPatch(typeof(HealthbarUIActorElement), "InitHealthbar")]
            public static class HealthbarUIActorElement_InitHealthbar_patch
            {
                public static void Postfix(HealthbarUIActorElement __instance, TacticalActorBase ____tacActorBase)
                {
                    try
                    {
                        ChangeHealthBarIcon(__instance.ActorClassIconElement, ____tacActorBase);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }


            public class OpposingLeaderWidgetTooltip : MonoBehaviour
            {
                private GameObject _uIElement;
                private GameObject tooltipInstance;
                private GameObject _status;
                private GameObject _factionIcon;
                private GameObject _classIcon;
                private GameObject _leaderName;
                private GameObject _squadName;
                private GameObject _factionName;
                private GameObject _tacticName;
                private GameObject _tacticDescription;
                private GameObject CreateTooltipPanel(Transform parent)
                {
                    try
                    {

                        // Create the root object for the tooltip
                        GameObject tooltip = new GameObject("Tooltip", typeof(RectTransform), typeof(Image)); //typeof(CanvasRenderer), 
                        RectTransform tooltipRect = tooltip.GetComponent<RectTransform>();
                        tooltipRect.SetParent(parent, false);
                        tooltipRect.sizeDelta = new Vector2(300, 300); // Adjust default size
                        tooltipRect.pivot = new Vector2(-0.5f, 1.1f);

                        // Style the background
                        Image background = tooltip.GetComponent<Image>();
                        background.color = new Color(0, 0, 0, 0.8f); // Semi-transparent black

                        tooltip.SetActive(false); // Hide by default
                        return tooltip;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                public void CreateUIElement(Transform parentTransform, string status, GameObject factionIcon, GameObject classIcon,
                    GameObject leaderName, string squadName, string factionName, GameObject tacticName, string tacticDescription, bool tacticActive)
                {
                    try
                    {

                        if (_uIElement == null)
                        {

                            _uIElement = parentTransform.gameObject;
                            // Add Event Triggers for hover behavior
                            _uIElement.AddComponent<EventTrigger>();

                        }

                        UpdateInfo(status, factionIcon, classIcon, leaderName, squadName, factionName, tacticName, tacticDescription, tacticActive);

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }

                }

                public void UpdateInfo(string status, GameObject factionIcon, GameObject classIcon,
                    GameObject leaderName, string squadName, string factionName, GameObject tacticName, string tacticDescription, bool tacticActive)
                {
                    try
                    {

                        EventTrigger trigger = _uIElement.GetComponent<EventTrigger>();
                        trigger.triggers.Clear();

                        // Show tooltip on hover
                        EventTrigger.Entry entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                        entryEnter.callback.AddListener((_) => ShowTooltip(_uIElement.transform, status, factionIcon, classIcon,
                            leaderName, squadName, factionName, tacticName, tacticDescription, tacticActive));
                        trigger.triggers.Add(entryEnter);

                        // Hide tooltip on exit
                        EventTrigger.Entry entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                        entryExit.callback.AddListener((_) => HideTooltip());
                        trigger.triggers.Add(entryExit);


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }


                }

                private void ShowTooltip(Transform parent, string status, GameObject factionIcon, GameObject classIcon,
                    GameObject leaderName, string squadName, string factionName, GameObject tacticName, string tacticDescription, bool tacticActive)
                {
                    try
                    {

                        if (tooltipInstance == null)
                        {
                            tooltipInstance = CreateTooltipPanel(parent);

                            RectTransform tooltipRect = tooltipInstance.GetComponent<RectTransform>();
                            tooltipRect.sizeDelta = new Vector2(300, 300);

                            float xPositionOffest = tooltipRect.sizeDelta.x / 2;
                            float yPositionOffset = tooltipRect.sizeDelta.y / 2;

                            _status = new GameObject("Status", typeof(RectTransform), typeof(Text));
                            //      textObj.GetComponent<RectTransform>().localScale = new Vector3(0.5f, 0.5f, 0.5f);
                            RectTransform statusRect = _status.GetComponent<RectTransform>();
                            statusRect.SetParent(tooltipRect, false);
                            statusRect.anchoredPosition = new Vector2(-60, yPositionOffset - 20);
                            statusRect.sizeDelta = new Vector2(300, 50);
                            statusRect.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                            //   textObj.GetComponent<RectTransform>().pivot = new Vector2(5, -5);
                            Text statusText = _status.GetComponent<Text>();
                            statusText.text = status.ToUpper();
                            statusText.font = PuristaSemiboldFontCache ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                            statusText.fontSize = 40; //70

                            if (status == "DEAD" || status == "FLED")
                            {
                                statusText.color = NegativeColor;
                            }
                            else
                            {
                                statusText.color = WhiteColor;
                            }

                            statusText.horizontalOverflow = HorizontalWrapMode.Wrap;
                            statusText.verticalOverflow = VerticalWrapMode.Overflow;
                            statusText.alignment = TextAnchor.MiddleLeft;

                            _factionIcon = UnityEngine.Object.Instantiate(factionIcon, tooltipRect);
                            _factionIcon.GetComponent<RectTransform>().anchoredPosition = new Vector2(-120, yPositionOffset - 55);

                            _classIcon = UnityEngine.Object.Instantiate(classIcon, tooltipRect);
                            _classIcon.GetComponent<RectTransform>().anchoredPosition = new Vector2(-80, yPositionOffset - 55);

                            _leaderName = UnityEngine.Object.Instantiate(leaderName, tooltipRect);
                            _leaderName.GetComponent<RectTransform>().anchoredPosition = new Vector2(94, yPositionOffset - 52.5f);

                            _squadName = new GameObject("SquadName", typeof(RectTransform), typeof(Text));
                            //      textObj.GetComponent<RectTransform>().localScale = new Vector3(0.5f, 0.5f, 0.5f);
                            RectTransform squadNameRect = _squadName.GetComponent<RectTransform>();
                            squadNameRect.SetParent(tooltipRect, false);
                            squadNameRect.anchoredPosition = new Vector2(10, yPositionOffset - 85);
                            squadNameRect.sizeDelta = new Vector2(580, 50);
                            squadNameRect.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                            //   textObj.GetComponent<RectTransform>().pivot = new Vector2(5, -5);
                            Text squadNameText = _squadName.GetComponent<Text>();

                            if (leaderName.GetComponent<Text>().text == TFTVCommonMethods.ConvertKeyToString("KEY_LORE_TITLE_SUBJECT24"))
                            {
                                squadNameText.text = $"{TFTVCommonMethods.ConvertKeyToString("HUMAN_ENEMIES_KEY_LEADER")}{TFTVCommonMethods.ConvertKeyToString("KEY_TFTV_GRAMMAR_OF")}{factionName.ToUpper()}";
                            }
                            else
                            {
                                squadNameText.text = $"{TFTVCommonMethods.ConvertKeyToString("HUMAN_ENEMIES_KEY_LEADER")}{TFTVCommonMethods.ConvertKeyToString("KEY_TFTV_GRAMMAR_OF")}{squadName.ToUpper()}";
                            }


                            squadNameText.font = PuristaSemiboldFontCache ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                            squadNameText.fontSize = 30; //70                          
                            squadNameText.color = WhiteColor;
                            squadNameText.horizontalOverflow = HorizontalWrapMode.Wrap;
                            squadNameText.verticalOverflow = VerticalWrapMode.Overflow;
                            squadNameText.alignment = TextAnchor.MiddleLeft;

                            _factionName = new GameObject("FactionName", typeof(RectTransform), typeof(Text));
                            //      textObj.GetComponent<RectTransform>().localScale = new Vector3(0.5f, 0.5f, 0.5f);
                            RectTransform factionNameRect = _factionName.GetComponent<RectTransform>();
                            factionNameRect.SetParent(tooltipRect, false);
                            factionNameRect.anchoredPosition = new Vector2(-60, yPositionOffset - 105);
                            factionNameRect.sizeDelta = new Vector2(300, 50);
                            factionNameRect.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                            //   textObj.GetComponent<RectTransform>().pivot = new Vector2(5, -5);
                            Text factionNameText = _factionName.GetComponent<Text>();
                            factionNameText.text = factionName;
                            factionNameText.font = PuristaSemiboldFontCache ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                            factionNameText.fontSize = 30; //70
                            factionNameText.color = WhiteColor;
                            factionNameText.horizontalOverflow = HorizontalWrapMode.Wrap;
                            factionNameText.verticalOverflow = VerticalWrapMode.Overflow;
                            factionNameText.alignment = TextAnchor.MiddleLeft;

                            _tacticName = UnityEngine.Object.Instantiate(tacticName, tooltipRect);
                            _tacticName.GetComponent<RectTransform>().anchoredPosition = new Vector2(-7, yPositionOffset - 140);


                            _tacticDescription = new GameObject("TacticDescription", typeof(RectTransform), typeof(Text));
                            //      textObj.GetComponent<RectTransform>().localScale = new Vector3(0.5f, 0.5f, 0.5f);
                            RectTransform tacticDescriptionRect = _tacticDescription.GetComponent<RectTransform>();
                            tacticDescriptionRect.SetParent(tooltipRect, false);
                            tacticDescriptionRect.anchoredPosition = new Vector2(10, yPositionOffset - 210);
                            tacticDescriptionRect.sizeDelta = new Vector2(580, 200);
                            tacticDescriptionRect.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                            //   textObj.GetComponent<RectTransform>().pivot = new Vector2(5, -5);
                            Text tacticDescriptionText = _tacticDescription.GetComponent<Text>();
                            tacticDescriptionText.text = tacticDescription;
                            tacticDescriptionText.font = PuristaSemiboldFontCache ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                            tacticDescriptionText.fontSize = 40; //70
                            tacticDescriptionText.color = WhiteColor;
                            tacticDescriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
                            tacticDescriptionText.verticalOverflow = VerticalWrapMode.Overflow;
                            tacticDescriptionText.alignment = TextAnchor.UpperLeft;


                            if (!tacticActive)
                            {
                                tacticDescriptionText.color = _regularNoLOSColor;
                                tacticName.GetComponent<Text>().color = _regularNoLOSColor;
                            }

                        }


                        tooltipInstance.SetActive(true);


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private void HideTooltip()
                {
                    try
                    {
                        if (tooltipInstance != null)
                        {
                            Destroy(tooltipInstance);
                            //  tooltipInstance.SetActive(false);
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            public class OpposingLeaderWidget : MonoBehaviour
            {
                private GameObject leaderWidgetPrefab;
                private Transform widgetContainer;
                private Image _factionIcon;
                private Image _leaderClassIcon;
                private Text _nameOfLeader;
                private Text _titleOfTactic;
                private OpposingLeaderWidgetTooltip _widgetTooltip;




                public void InitializeLeaderWidget(Sprite factionIcon, Sprite classIcon, TacticalActor leader, string tacticName,
                    string tacticDescription, string factionName, string squadName, string status, bool leaderDead, bool leaderFled, bool tacticActive)
                {
                    try
                    {
                        Resolution resolution = Screen.currentResolution;
                        float resolutionFactorWidth = (float)resolution.width / 1920f;
                        float resolutionFactorHeight = (float)resolution.height / 1080f;

                        // Access UIModuleNavigation and set widgetContainer as its transform
                        TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();
                        UIModuleNavigation uIModuleNavigation = controller.View.TacticalModules.NavigationModule;

                        widgetContainer = uIModuleNavigation.transform;

                        // Dynamically create the leaderWidgetPrefab structure
                        leaderWidgetPrefab = new GameObject("LeaderWidget");
                        RectTransform rectTransform = leaderWidgetPrefab.AddComponent<RectTransform>();

                        rectTransform.sizeDelta = new Vector2(255, 180);
                        rectTransform.position = new Vector2(170 * resolutionFactorWidth, 600 * resolutionFactorHeight + (100 * (_leaderWidgets.Count - 1) * resolutionFactorHeight)); // Left margin of 20, 1/3 height down


                        //Header
                        GameObject headerObject = new GameObject("Header", typeof(RectTransform), typeof(Image));
                        headerObject.transform.SetParent(leaderWidgetPrefab.transform); // Attach to the existing GameObject
                        RectTransform headerRect = headerObject.GetComponent<RectTransform>();
                        headerRect.sizeDelta = new Vector2(rectTransform.sizeDelta.x, 40);
                        headerRect.anchoredPosition = Vector2.zero;
                        Image headerImage = headerObject.GetComponent<Image>();
                        headerImage.sprite = Helper.CreateSpriteFromImageFile("pp_leader_heading.png"); //Helper.CreateSpriteFromImageFile("UI_MainButton_ChippedBackground.png"); // 

                        Color color = headerImage.color;
                        color.a = 0.9f;
                        headerImage.color = color;

                        GameObject iconFactionObj = new GameObject("FactionIcon");
                        iconFactionObj.transform.SetParent(headerObject.transform);
                        Image iconFactionImage = iconFactionObj.AddComponent<Image>();
                        iconFactionImage.sprite = factionIcon;
                        iconFactionImage.color = NegativeColor; //factionColor;
                        iconFactionImage.preserveAspect = true;
                        RectTransform iconFactionImageRect = iconFactionImage.GetComponent<RectTransform>();
                        iconFactionImageRect.sizeDelta = new Vector2(30, 30);
                        iconFactionImageRect.anchoredPosition = Vector2.zero + new Vector2(-105, 0);


                        AddOutlineToIcon addOutlineToIcon = iconFactionObj.GetComponent<AddOutlineToIcon>();

                        if (addOutlineToIcon == null)
                        {
                            addOutlineToIcon = iconFactionObj.AddComponent<AddOutlineToIcon>();
                        }

                        addOutlineToIcon.icon = iconFactionObj;
                        addOutlineToIcon.InitOrUpdate();



                        _factionIcon = iconFactionImage;
                        // Set up the icon
                        GameObject iconObj = new GameObject("Icon");
                        iconObj.transform.SetParent(headerObject.transform);
                        Image iconImage = iconObj.AddComponent<Image>();
                        iconImage.sprite = classIcon;
                        iconImage.color = LeaderColor;
                        iconImage.preserveAspect = true;
                        RectTransform iconImageRect = iconImage.GetComponent<RectTransform>();
                        iconImageRect.sizeDelta = new Vector2(30, 30);
                        iconImageRect.anchoredPosition = Vector2.zero + new Vector2(-65, 0);//Vector2.zero + new Vector2(-150, 38);

                        RankIconCreator rankIconCreator = new RankIconCreator();
                        rankIconCreator.SetIconWithRank(iconObj, classIcon, 4);

                        _leaderClassIcon = iconImage;

                        // Set up the name text
                        GameObject nameTextObj = new GameObject("NameText");
                        nameTextObj.transform.SetParent(headerObject.transform);
                        Text nameText = nameTextObj.AddComponent<Text>();
                        nameText.text = TFTVTacticalUtils.ShortenName(leader?.name, 11);
                        nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
                        nameText.alignment = TextAnchor.MiddleLeft;
                        nameText.fontSize = 40;
                        nameText.color = LeaderColor;
                        nameText.font = PuristaSemiboldFontCache ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                        RectTransform rectNameText = nameText.GetComponent<RectTransform>();
                        rectNameText.sizeDelta = new Vector2(600, 60);
                        rectNameText.localScale = new Vector2(0.5f, 0.5f);
                        rectNameText.anchoredPosition = Vector2.zero + new Vector2(110, 0f); //Vector2.zero + new Vector2(20, 40);
                        _nameOfLeader = nameText;

                        /*  if (iconObj.GetComponent<AddOutlineToIcon>() == null)
                          {
                              AddOutlineToIcon addOutlineToIcon = iconObj.AddComponent<AddOutlineToIcon>();
                              addOutlineToIcon.icon = iconObj;
                              addOutlineToIcon.Init();
                          }*/

                        //TitleBackground 
                        GameObject titleBackgroundObject = new GameObject("TitleBackground", typeof(RectTransform), typeof(Image));
                        titleBackgroundObject.transform.SetParent(leaderWidgetPrefab.transform); // Attach to the existing GameObject
                        RectTransform titleBackgroundRect = titleBackgroundObject.GetComponent<RectTransform>();
                        titleBackgroundRect.sizeDelta = new Vector2(rectTransform.sizeDelta.x, 40);
                        titleBackgroundRect.anchoredPosition = Vector2.zero + new Vector2(0, -40);
                        Image titleBackgroundImage = titleBackgroundObject.GetComponent<Image>();
                        titleBackgroundImage.color = new Color(0, 0, 0, 0.75f);
                        //  headerImage.color = new Color(0.224f, 0.149f, 0.102f, 0.75f);


                        // Set up the tactic name text
                        GameObject tacticNameTextObj = new GameObject("TacticNameText");
                        tacticNameTextObj.transform.SetParent(titleBackgroundObject.transform);
                        Text tacticNameText = tacticNameTextObj.AddComponent<Text>();
                        tacticNameText.text = tacticName.ToUpper();
                        tacticNameText.horizontalOverflow = HorizontalWrapMode.Overflow;
                        tacticNameText.fontSize = 40;
                        tacticNameText.color = WhiteColor;
                        tacticNameText.alignment = TextAnchor.MiddleLeft;
                        tacticNameText.font = PuristaSemiboldFontCache ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                        RectTransform recttacticNameText = tacticNameText.GetComponent<RectTransform>();
                        recttacticNameText.sizeDelta = titleBackgroundRect.sizeDelta * 2;
                        recttacticNameText.localScale = new Vector2(0.5f, 0.5f);
                        recttacticNameText.anchoredPosition = Vector2.zero + new Vector2(10, 0);

                        _titleOfTactic = tacticNameText;

                        /* UITooltipText uITooltipText = leaderWidgetPrefab.AddComponent<UITooltipText>();
                         uITooltipText.TipText = $"<b>{tacticName.ToUpper()}</b>\n{tacticDescription}";*/

                        leaderWidgetPrefab.transform.SetParent(widgetContainer);
                        leaderWidgetPrefab.SetActive(true);

                        if (leader != null)
                        {
                            AddChaseTargetOnClick(_factionIcon.transform.parent.gameObject, leader);
                        }

                        _widgetTooltip = leaderWidgetPrefab.AddComponent<OpposingLeaderWidgetTooltip>();
                        _widgetTooltip.CreateUIElement(leaderWidgetPrefab.transform, status, iconFactionObj, iconObj, nameTextObj,
                            squadName, factionName, tacticNameTextObj, tacticDescription, tacticActive);

                        ModifyWidget(leader, status, squadName, factionName, tacticDescription, leaderDead, leaderFled, tacticActive);


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                public void ModifyWidget(TacticalActor leader, string status, string squadName, string factionName, string tacticDescription, bool leaderDead = false, bool leaderFled = false, bool tacticActive = true)
                {
                    try
                    {

                        if (leaderDead || leaderFled)
                        {
                            _nameOfLeader.color = Color.gray;
                            AddStrikethrough(_nameOfLeader, new Vector2(-160, 10), 20);
                            _factionIcon.color = Color.gray;
                            _leaderClassIcon.color = Color.gray;

                            RankIconCreator rankIconCreator = new RankIconCreator();

                            rankIconCreator.RemoveRankTriangles(_leaderClassIcon.gameObject);
                            rankIconCreator.SetIconWithRank(_leaderClassIcon.gameObject, _leaderClassIcon.sprite, 4, false, true);
                        }
                        else
                        {
                            AddStrikethrough(_nameOfLeader, Vector2.one, 0, true);
                            _factionIcon.color = NegativeColor;
                            _leaderClassIcon.color = LeaderColor;
                            _nameOfLeader.color = LeaderColor;

                            RankIconCreator rankIconCreator = new RankIconCreator();
                            rankIconCreator.RemoveRankTriangles(_leaderClassIcon.gameObject);
                            rankIconCreator.SetIconWithRank(_leaderClassIcon.gameObject, _leaderClassIcon.sprite, 4);
                        }


                        if (!tacticActive)
                        {
                            AddStrikethrough(_titleOfTactic, new Vector2(-40, 5), 32);
                            _titleOfTactic.color = Color.gray;
                            //  _titleOfTactic.text += "<color=#ec1c24><b> X</b></color>";
                        }
                        else
                        {
                            AddStrikethrough(_titleOfTactic, Vector2.one, 0, true);
                            _titleOfTactic.color = WhiteColor;

                        }

                        if (leader != null)
                        {
                            AddChaseTargetOnClick(leaderWidgetPrefab, leader);
                        }


                        _widgetTooltip.CreateUIElement(leaderWidgetPrefab.transform, status, _factionIcon.gameObject,
                            _leaderClassIcon.gameObject, _nameOfLeader.gameObject, squadName, factionName, _titleOfTactic.gameObject, tacticDescription, tacticActive);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private void AddStrikethrough(Text textElement, Vector2 offset, int size, bool remove = false)
                {
                    try
                    {
                        // Check if the strikethrough already exists
                        Transform existingStrikethrough = textElement.transform.Find("Strikethrough");
                        if (existingStrikethrough != null)
                        {
                            if (remove)
                            {
                                existingStrikethrough.gameObject.SetActive(false);
                            }
                            else
                            {
                                existingStrikethrough.gameObject.SetActive(true);
                            }

                            return; // Strikethrough already exists
                        }

                        if (remove)
                        {
                            return;
                        }


                        // Create a new Text element for the strikethrough
                        GameObject strikethrough = new GameObject("Strikethrough");
                        Text strikethroughText = strikethrough.AddComponent<Text>();
                        RectTransform rectTransform = strikethrough.GetComponent<RectTransform>();

                        // Configure the strikethrough Text
                        strikethroughText.text = new string('_', size); // Create a line of underscores
                        strikethroughText.font = textElement.font; // Match the font
                        strikethroughText.fontSize = textElement.fontSize; // Match the font size
                        strikethroughText.color = Color.gray; // Set the color to red
                        strikethroughText.alignment = TextAnchor.UpperCenter; // Center the line
                        strikethrough.transform.SetParent(textElement.transform);

                        // Position the strikethrough over the original text
                        rectTransform.sizeDelta = textElement.rectTransform.sizeDelta; // Match size
                        rectTransform.anchorMin = new Vector2(0, 0.5f); // Middle of the text vertically
                        rectTransform.anchorMax = new Vector2(1, 0.5f);
                        rectTransform.anchoredPosition = offset;// Slightly below the middle
                        rectTransform.localScale = Vector3.one;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private void AddChaseTargetOnClick(GameObject parent, TacticalActor target)
                {
                    try
                    {

                        if (parent.GetComponent<EventTrigger>() == null)
                        {
                            parent.AddComponent<EventTrigger>();
                        }

                        EventTrigger eventTrigger = parent.GetComponent<EventTrigger>();
                        eventTrigger.triggers.Clear();
                        EventTrigger.Entry click = new EventTrigger.Entry
                        {
                            eventID = EventTriggerType.PointerClick
                        };

                        click.callback.AddListener((eventData) =>
                        {
                            TFTVLogger.Always($"checking for whether should chase target");

                            if (target.TacticalLevel.GetFactionByCommandName("px").Vision.IsRevealed(target))
                            {
                                TFTVLogger.Always($"should chase target");
                                target.TacticalActorView.DoCameraChase();
                            }
                        });

                        eventTrigger.triggers.Add(click);

                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }


                }
            }
            private static Dictionary<PPFactionDef, GeoFactionViewDef> _dictionaryFactionViewElement = new Dictionary<PPFactionDef, GeoFactionViewDef>();

            public static void PopulateFactionViewElementDictionary()
            {
                try
                {
                    if (_dictionaryFactionViewElement.Count == 0)
                    {

                        _dictionaryFactionViewElement.Add(
                        DefCache.GetDef<PPFactionDef>("Anu_FactionDef"),
                        DefCache.GetDef<GeoFactionViewDef>("E_Anu_GeoFactionView [Anu_GeoFactionDef]"));

                        _dictionaryFactionViewElement.Add(
                        DefCache.GetDef<PPFactionDef>("NewJericho_FactionDef"),
                        DefCache.GetDef<GeoFactionViewDef>("E_NewJericho_GeoFactionView [NewJericho_GeoFactionDef]"));

                        _dictionaryFactionViewElement.Add(
                        DefCache.GetDef<PPFactionDef>("Synedrion_FactionDef"),
                        DefCache.GetDef<GeoFactionViewDef>("E_Synedrion_GeoFactionView [Synedrion_GeoFactionDef]"));

                        _dictionaryFactionViewElement.Add(
                        DefCache.GetDef<PPFactionDef>("AN_FallenOnes_FactionDef"),
                        DefCache.GetDef<GeoFactionViewDef>("E_AN_FallenOnes_GeoFactionView [AN_FallenOnes_GeoSubFactionDef]"));

                        _dictionaryFactionViewElement.Add(
                        DefCache.GetDef<PPFactionDef>("NJ_Purists_FactionDef"),
                        DefCache.GetDef<GeoFactionViewDef>("E_NJ_Purists_GeoFactionView [NJ_Purists_GeoSubFactionDef]"));

                        _dictionaryFactionViewElement.Add(
                        DefCache.GetDef<PPFactionDef>("NEU_Bandits_FactionDef"),
                        DefCache.GetDef<GeoFactionViewDef>("E_NEU_Bandits [NEU_Bandits_GeoSubFactionDef]"));
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static TacticalActor FindMissingLeader(TacticalLevelController controller, string factionName)
            {
                try
                {


                    foreach (TacticalFaction tacticalFaction in controller.Factions)
                    {
                        if (tacticalFaction.TacticalActors.Any(ta => ta.HasGameTag(TFTVHumanEnemies.HumanEnemyTier1GameTag) && ta.GameTags.Any(t => t.name.Contains(factionName))))
                        {
                            TacticalActor leader = tacticalFaction.TacticalActors.FirstOrDefault(ta => ta.HasGameTag(TFTVHumanEnemies.HumanEnemyTier1GameTag) && ta.GameTags.Any(t => t.name.Contains(factionName)));

                            // TFTVLogger.Always($"found leader {leader.name}");
                            return leader;

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

            public static void ActivateOrAdjustLeaderWidgets(bool hintShown = false)
            {
                try
                {
                    TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                    Dictionary<string, int> HumanEnemiesAndTactics = TFTVHumanEnemies.HumanEnemiesAndTactics;

                    if (HumanEnemiesAndTactics.Count > 0)
                    {
                        for (int x = 0; x < HumanEnemiesAndTactics.Keys.Count(); x++)
                        {
                            string factionCode = HumanEnemiesAndTactics.Keys.ElementAt(x);
                            TacticalFaction humanEnemy = controller.GetFactionByCommandName(factionCode);

                            string[] tacticData = TFTVHumanEnemies.GetTacticNameAndDescription(HumanEnemiesAndTactics[factionCode], humanEnemy);
                            GeoFactionViewDef geoFactionViewDef = _dictionaryFactionViewElement[humanEnemy.Faction.FactionDef];

                            Sprite factionIcon = geoFactionViewDef.FactionIcon;
                            string factionName = geoFactionViewDef.Name.Localize();
                            string squadName = "INGLOURIOUS BASTERDS";

                            if (TFTVHumanEnemies.HumanEnemiesGangNames != null && TFTVHumanEnemies.HumanEnemiesGangNames.Count > 0)
                            {
                                squadName = TFTVHumanEnemies.HumanEnemiesGangNames.ElementAt(x);
                            }

                            TacticalActor leader = humanEnemy.TacticalActors.FirstOrDefault(ta => ta.HasGameTag(TFTVHumanEnemies.HumanEnemyTier1GameTag));
                            string status = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_STATUS_ALIVE");

                            bool leaderDead = false;
                            bool leaderFled = false;
                            bool tacticActive = true;

                            if (leader == null)
                            {
                                leaderFled = true;
                                status = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_STATUS_FLED");
                                leader = FindMissingLeader(controller, factionCode);
                            }
                            else if (!leader.IsAlive)
                            {
                                leaderDead = true;
                                status = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_STATUS_DEAD");
                            }
                            else if (leader.IsEvacuated)
                            {
                                leaderFled = true;
                                status = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_STATUS_FLED");
                            }

                            if (leaderDead || leaderFled)
                            {
                                int tactic = HumanEnemiesAndTactics[factionCode];

                                if (tactic != 3 && tactic != 6 && tactic != 7)
                                {
                                    tacticActive = false;
                                }
                            }

                            if (!_leaderWidgets.ContainsKey(factionCode))
                            {
                                if (!hintShown && !humanEnemy.TacticalActors.Any(ta => controller.GetFactionByCommandName("px").Vision.IsRevealed(ta)))
                                {
                                    continue;
                                }

                                CreateOpposingLeaderWidget(factionCode,
                                    leader.ClassViewElementDefs.First(), leader, tacticData[0], tacticData[1], factionIcon, factionName, squadName, status, leaderDead, leaderFled, tacticActive);
                            }
                            else
                            {
                                _leaderWidgets[factionCode].ModifyWidget(leader, status, squadName, factionName, tacticData[1], leaderDead, leaderFled, tacticActive);

                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            public static void CreateOpposingLeaderWidget(string factionCode, ViewElementDef viewElementDef,
                TacticalActor leader, string tacticName, string tacticDescription, Sprite factionIcon, string factionName, string squadName, string status, bool leaderDead, bool leaderFled, bool tacticActive)
            {
                try
                {
                    GameObject leaderWidgetObject = new GameObject("OpposingLeaderWidgetObject");
                    _leaderWidgets.Add(factionCode, leaderWidgetObject.AddComponent<OpposingLeaderWidget>());

                    TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();
                    UIModuleNavigation uIModuleNavigation = controller.View.TacticalModules.NavigationModule;
                    leaderWidgetObject.transform.SetParent(uIModuleNavigation.transform, false);

                    Sprite classIcon = viewElementDef.SmallIcon;

                    _leaderWidgets[factionCode].InitializeLeaderWidget(factionIcon, classIcon, leader, tacticName, tacticDescription, factionName, squadName, status, leaderDead, leaderFled, tacticActive);


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

        }

        internal class ODITactical
        {
            private static Sprite _tbtvGeneralSprite;
            private static Sprite _tbtvOnDeathSprite;
            private static Sprite _tbtvOnTurnEndSprite;
            private static Sprite _tbtvOnAttackSprite;
            private static Sprite _deliriumStatusSprite;
            private static Sprite _voidOmenSprite;



            public static void ManageTBTVIconToSpottedEnemies(SpottedTargetsElement __instance, GameObject obj, TacticalActorBase target)
            {
                try
                {
                    // Look for an existing StatusIcon under the target object
                    Transform existingIconTransform = obj.transform.Find("TBTVStatusIcon");
                    Image statusIconImage = existingIconTransform ? existingIconTransform.GetComponent<Image>() : null;


                    TacticalAbilityDef revenantAbility = TFTVRevenant.RevenantAbility;

                    // Check if the target satisfies the condition to display the icon
                    bool shouldDisplayIcon = //target.GetAbilityWithDef<PassiveModifierAbility>(revenantAbility)!=null || (
                        target.Status != null &&
                                             target.Status.Statuses.Any(s => TFTVTouchedByTheVoid.tbtvStatuses.Contains(s.Def));

                    if (shouldDisplayIcon)
                    {
                        // Get the appropriate sprite from the target's status
                        StatusDef statusDef = target.Status.Statuses
     .Where(s => TFTVTouchedByTheVoid.tbtvStatuses.Contains(s.Def) && s.Def != TFTVTouchedByTheVoid.hiddenTBTVAddAbilityStatus)
     .Select(s => s.Def)
     .FirstOrDefault() ?? (target.Status.Statuses
                                .FirstOrDefault(s => s.Def == TFTVTouchedByTheVoid.hiddenTBTVAddAbilityStatus)?.Def);


                        ViewElementDef viewElementDef = statusDef is AddAbilityStatusDef addAbilityStatusDef
                            ? addAbilityStatusDef.AbilityDef.ViewElementDef
                            : (statusDef is DamageMultiplierStatusDef damageMultiplierStatusDef ? damageMultiplierStatusDef.Visuals : null);

                        Sprite sprite = viewElementDef?.SmallIcon;

                        if (sprite != null)
                        {

                            // If the icon already exists, update the sprite and activate it
                            if (statusIconImage != null)
                            {
                                statusIconImage.sprite = sprite;
                                UITooltipText uITooltipText = obj.GetComponent<UITooltipText>();

                                uITooltipText.TipText = $"{viewElementDef.DisplayName1.Localize()}: {viewElementDef.Description.Localize()}";
                                uITooltipText.Enabled = true;

                                statusIconImage.color = VoidColor;

                                // statusIconImage.gameObject.SetActive(false);
                                statusIconImage.enabled = true;

                                AddOutlineToIcon addOutlineToIcon = statusIconImage.GetComponent<AddOutlineToIcon>() ?? statusIconImage.gameObject.AddComponent<AddOutlineToIcon>();
                                addOutlineToIcon.icon = statusIconImage.gameObject;
                                addOutlineToIcon.InitOrUpdate();
                            }
                            else
                            {

                                //  TFTVLogger.Always($"should create icon for {viewElementDef.name}");
                                // Create a new icon if it doesn't exist
                                GameObject newIcon = GameObject.Instantiate(__instance.ReturnFire.gameObject, obj.transform);
                                newIcon.name = "TBTVStatusIcon";
                                newIcon.SetActive(true);

                                // Set the sprite and size
                                Image newIconImage = newIcon.GetComponent<Image>();
                                UITooltipText uITooltipText = obj.AddComponent<UITooltipText>();
                                uITooltipText.TipText = $"{viewElementDef.DisplayName1.Localize()}: {viewElementDef.Description.Localize()}";
                                uITooltipText.Position = Position.Top;
                                uITooltipText.enabled = true;
                                newIconImage.sprite = sprite;

                                newIconImage.color = VoidColor;

                                // Adjust the size and position of the icon
                                RectTransform rt = newIcon.GetComponent<RectTransform>();

                                rt.anchorMin = new Vector2(0, 0);  // Anchor to bottom-left
                                rt.anchorMax = new Vector2(0, 0);  // Anchor to bottom-left
                                rt.pivot = new Vector2(0, 0);      // Set pivot to bottom-left
                                rt.anchoredPosition = new Vector2(20, 55);  // Offset slightly from the corner if needed
                                rt.localScale = new Vector2(1.3f, 1.3f);


                                AddOutlineToIcon addOutlineToIcon = newIcon.GetComponent<AddOutlineToIcon>() ?? newIcon.AddComponent<AddOutlineToIcon>();
                                addOutlineToIcon.icon = newIcon;
                                addOutlineToIcon.InitOrUpdate();


                                // rt.sizeDelta = new Vector2(15, 15); // Set to (15, 15) for small size
                            }
                        }
                    }
                    else
                    {
                        // If the condition is not met, deactivate the icon if it exists
                        if (statusIconImage != null)
                        {
                            statusIconImage.enabled = false;

                            UITooltipText uITooltipText = obj.GetComponent<UITooltipText>();

                            if (uITooltipText != null)
                            {
                                uITooltipText.enabled = false;
                            }
                            ;
                        }
                    }

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }

            //  private static Transform _voidOmens = null;
            private static GameObject _oDIWidget = null;
            private static Sprite _voidIcon = null;

            private static List<(Sprite icon, string text, ODIDetailType)> _ODISitrepList = new List<(Sprite icon, string text, ODIDetailType)>();
            public enum ODIDetailType
            {
                ODIDescription, VoidOmen, TBTVDescription, TBTVEffect, TBTVChance, TBTVModifier
            }

            public class ODIWidgetTooltip : MonoBehaviour
            {
                private GameObject _uIElement;
                private GameObject tooltipInstance;
                private Transform _parentTransform;
                private List<(Sprite icon, string text, ODIDetailType)> _details;
                private GameObject CreateTooltipPanel()
                {
                    try
                    {

                        // Create the root object for the tooltip
                        GameObject tooltip = new GameObject("Tooltip", typeof(RectTransform), typeof(Image)); //typeof(CanvasRenderer), 
                        RectTransform tooltipRect = tooltip.GetComponent<RectTransform>();
                        tooltipRect.sizeDelta = new Vector2(1200, 1750); // Adjust default size
                        tooltipRect.pivot = new Vector2(0.25f, 1.035f); // Top-left pivot
                        tooltipRect.SetParent(_parentTransform, false);

                        // Style the background
                        Image background = tooltip.GetComponent<Image>();
                        background.color = new Color(0, 0, 0, 0.8f); // Semi-transparent black


                        // Add Vertical Layout Group for dynamic content
                        //VerticalLayoutGroup layout = tooltip.AddComponent<VerticalLayoutGroup>();
                        //layout.padding = new RectOffset(10, 10, 10, 10);
                        //  layout.spacing = 50;

                        //  layout.childAlignment = TextAnchor.UpperLeft;

                        tooltip.SetActive(false); // Hide by default
                        return tooltip;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }



                private GameObject CreateDetailItem(Sprite icon, string text, float yOffset, bool tab = false, bool separator = false)
                {
                    try
                    {


                        // Create a container for the detail item
                        GameObject detail = new GameObject("DetailItem", typeof(RectTransform));
                        RectTransform detailRect = detail.GetComponent<RectTransform>();
                        detailRect.pivot = new Vector2(0.5f, -4f);
                        detailRect.sizeDelta = new Vector2(1000, 200);


                        float tabOffest = 0;

                        if (tab)
                        {
                            tabOffest = 100;
                        }

                        // Optional Icon
                        if (icon != null)
                        {
                            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
                            iconRect.sizeDelta = new Vector2(60, 200); // Icon size
                            iconRect.anchoredPosition += new Vector2(-30 - detailRect.sizeDelta.x / 2 + tabOffest, -yOffset);
                            iconObj.GetComponent<Image>().sprite = icon;
                            iconObj.GetComponent<Image>().preserveAspect = true;
                            iconObj.transform.SetParent(detail.transform, false);

                            if (icon != _voidOmenSprite)
                            {
                                iconObj.GetComponent<Image>().color = VoidColor;
                            }

                            AddOutlineToIcon addOutlineToIcon = iconObj.GetComponent<AddOutlineToIcon>() ?? iconObj.AddComponent<AddOutlineToIcon>();
                            addOutlineToIcon.icon = iconObj;
                            addOutlineToIcon.InitOrUpdate();


                            // iconRect.pivot = new Vector2(0f, 0f);
                        }

                        // Text
                        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
                        //      textObj.GetComponent<RectTransform>().localScale = new Vector3(0.5f, 0.5f, 0.5f);
                        textObj.GetComponent<RectTransform>().anchoredPosition += new Vector2(tabOffest, -yOffset);
                        textObj.GetComponent<RectTransform>().sizeDelta = new Vector2(940 - tabOffest * 2, 200);
                        //   textObj.GetComponent<RectTransform>().pivot = new Vector2(5, -5);
                        Text textComponent = textObj.GetComponent<Text>();
                        textComponent.text = text;
                        textComponent.font = PuristaSemiboldFontCache ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                        textComponent.fontSize = 40; //70
                        textComponent.color = WhiteColor;
                        textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
                        textComponent.verticalOverflow = VerticalWrapMode.Overflow;

                        textComponent.alignment = TextAnchor.MiddleLeft;
                        textObj.transform.SetParent(detail.transform, false);

                        if (separator)
                        {
                            GameObject separatorObject = new GameObject($"Separator", typeof(RectTransform), typeof(Image));
                            RectTransform separatorRect = separatorObject.GetComponent<RectTransform>();
                            separatorRect.SetParent(detail.transform, false);

                            separatorRect.sizeDelta = new Vector2(1000, 2); // Width: 800, Height: 2 (Adjust as needed)
                                                                            //  separatorRect.anchorMin = new Vector2(0, 0.5f); // Anchors to stretch across the width
                                                                            //   separatorRect.anchorMax = new Vector2(1, 0.5f);
                            separatorRect.pivot = new Vector2(0.5f, 1f);
                            separatorRect.anchoredPosition = new Vector2(0, -yOffset - 90);

                            // Style the separator
                            Image separatorImage = separatorObject.GetComponent<Image>();
                            separatorImage.sprite = Helper.CreateSpriteFromImageFile("pp_obj_update_line.png");
                            separatorImage.preserveAspect = true;
                        }

                        return detail;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                public void CreateUIElement(Vector3 position, string mainText, Sprite mainIcon, List<(Sprite icon, string text, ODIDetailType)> details, Transform parentTransform)
                {
                    try
                    {

                        if (_uIElement == null)
                        {

                            // Create the main UI element
                            GameObject uiElement = new GameObject("UIElement", typeof(RectTransform), typeof(CanvasRenderer));
                            RectTransform rectTransform = uiElement.GetComponent<RectTransform>();
                            rectTransform.sizeDelta = new Vector2(800, 100); // Adjust size
                            rectTransform.SetParent(parentTransform, false);
                            rectTransform.position = position;

                            _parentTransform = parentTransform;

                            GameObject iconObj = new GameObject("MainIcon", typeof(RectTransform), typeof(Image));
                            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
                            iconRect.sizeDelta = new Vector2(80, 80);
                            iconRect.anchoredPosition = new Vector2(-80, 0);
                            iconObj.GetComponent<Image>().sprite = mainIcon;
                            iconObj.transform.SetParent(uiElement.transform, false);

                            AddOutlineToIcon addOutlineToIcon = iconObj.GetComponent<AddOutlineToIcon>() ?? iconObj.AddComponent<AddOutlineToIcon>();
                            addOutlineToIcon.icon = iconObj;
                            addOutlineToIcon.InitOrUpdate();

                            // Add Text
                            GameObject textObj = new GameObject("MainText", typeof(RectTransform), typeof(Text));
                            RectTransform textRect = textObj.GetComponent<RectTransform>();
                            textRect.sizeDelta = new Vector2(1340, 160);
                            textRect.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                            textRect.anchoredPosition = new Vector2(330, 0);
                            textRect.SetParent(uiElement.transform, false);

                            Text textComponent = textObj.GetComponent<Text>();
                            textComponent.text = mainText;
                            textComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
                            textComponent.font = PuristaSemiboldFontCache ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                            textComponent.fontSize = 100;
                            textComponent.alignment = TextAnchor.MiddleLeft;

                            // Add Event Triggers for hover behavior
                            uiElement.AddComponent<EventTrigger>();
                            _uIElement = uiElement;
                            _details = details;
                        }

                        UpdateDetails();

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }

                }

                public void UpdateDetails()
                {
                    try
                    {
                        Vector3 position = _uIElement.GetComponent<RectTransform>().position;

                        List<(Sprite icon, string text, ODIDetailType)> adjustedDetails = new List<(Sprite icon, string text, ODIDetailType)>();

                        for (int x = 0; x < _details.Count; x++)
                        {
                            string description = AdjustODIElementText(_details.ElementAt(x).text);

                            if (description != null && !description.Contains("{0}"))
                            {
                                adjustedDetails.Add((_details.ElementAt(x).icon, description, _details.ElementAt(x).Item3));
                            }
                            /* else
                             {
                                 adjustedDetails.Add((_details.ElementAt(x).icon, _details.ElementAt(x).text, _details.ElementAt(x).Item3));
                             }*/
                        }


                        EventTrigger trigger = _uIElement.GetComponent<EventTrigger>();
                        trigger.triggers.Clear();

                        // Show tooltip on hover
                        EventTrigger.Entry entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                        entryEnter.callback.AddListener((_) => ShowTooltip(position, adjustedDetails));
                        trigger.triggers.Add(entryEnter);

                        // Hide tooltip on exit
                        EventTrigger.Entry entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                        entryExit.callback.AddListener((_) => HideTooltip());
                        trigger.triggers.Add(entryExit);


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }


                }

                private void ShowTooltip(Vector3 position, List<(Sprite icon, string text, ODIDetailType)> details)
                {
                    try
                    {
                        Sprite umbraIcon = _tbtvOnDeathSprite;
                        Sprite modIcon = _tbtvOnAttackSprite;
                        Sprite onTurnEndIcon = _tbtvOnTurnEndSprite;
                        Sprite tbtvIcon = _tbtvGeneralSprite;
                        Sprite corruptionIcon = _deliriumStatusSprite;
                        Sprite voidOmenIcon = _voidOmenSprite;

                        tooltipInstance = CreateTooltipPanel();


                        RectTransform tooltipRect = tooltipInstance.GetComponent<RectTransform>();
                        // tooltipRect.sizeDelta = new Vector2(1200, details.Count * 200);
                        tooltipRect.position = position;

                        // Clear existing content
                        foreach (Transform child in tooltipInstance.transform)
                        {
                            Destroy(child.gameObject);
                        }

                        float distanceCounter = 100;

                        //   TFTVLogger.Always($"{distanceCounter}");

                        for (int x = 0; x < details.Count; x++)
                        {
                            float distanceToPreviousElement = Mathf.Min(200, 200 * x);

                            Sprite icon = details.ElementAt(x).icon;
                            ODIDetailType type = details.ElementAt(x).Item3;

                            bool tab = false;

                            if (type == ODIDetailType.ODIDescription || type == ODIDetailType.VoidOmen || type == ODIDetailType.TBTVDescription || type == ODIDetailType.TBTVChance)
                            {
                                distanceToPreviousElement *= 0.75f;
                            }

                            if (type == ODIDetailType.TBTVEffect || type == ODIDetailType.TBTVModifier && details.ElementAt(x - 1).Item3 != ODIDetailType.TBTVModifier)
                            {
                                distanceToPreviousElement *= 0.75f;
                                tab = true;
                            }

                            if (type == ODIDetailType.TBTVModifier && details.ElementAt(x - 1).Item3 == ODIDetailType.TBTVModifier)
                            {
                                distanceToPreviousElement *= 0.55f;
                                tab = true;
                            }

                            bool separator = false;

                            //Adds separator if general description or last TBTVEffect
                            if (type == ODIDetailType.ODIDescription || type == ODIDetailType.VoidOmen && details.Count() >= x + 2 && details.ElementAt(x + 1).Item3 != ODIDetailType.VoidOmen
                                || type == ODIDetailType.TBTVEffect && details.ElementAt(x).Item3 != ODIDetailType.TBTVEffect)
                            {
                                separator = true;
                            }


                            distanceCounter += distanceToPreviousElement;

                            GameObject detailItem = CreateDetailItem(details.ElementAt(x).icon, details.ElementAt(x).text, distanceCounter, tab, separator);
                            detailItem.transform.SetParent(tooltipInstance.transform, false);
                        }

                        tooltipInstance.SetActive(true);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                public void HideTooltip()
                {
                    try
                    {
                        if (tooltipInstance != null)
                        {
                            Destroy(tooltipInstance);
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            public static void ClearDataOnMissionRestart()
            {
                try
                {
                    _voidIcon = null;
                    _oDIWidget = null;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }

            public static void ClearDataOnLoadAndStateChange()
            {
                try
                {
                    _voidIcon = null;
                    _oDIWidget = null;
                    _ODISitrepList.Clear();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }

            private static Sprite GetRightVoidIcon()
            {
                try
                {
                    if (_voidIcon != null)
                    {
                        return _voidIcon;
                    }

                    if (TFTVTouchedByTheVoid.TBTVVariable >= 4 || TFTVVoidOmens.VoidOmensCheck[10])
                    {
                        _voidIcon = Helper.CreateSpriteFromImageFile("Void-04P.png");
                    }
                    else if (TFTVTouchedByTheVoid.TBTVVariable >= 2)
                    {
                        _voidIcon = Helper.CreateSpriteFromImageFile("Void-04Phalf.png");
                    }
                    else
                    {
                        _voidIcon = Helper.CreateSpriteFromImageFile("Void-04Pthird.png");
                    }

                    return _voidIcon;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }



            private static void GetVoidOmensTacticalText(bool tbtvRelevant = true)
            {
                try
                {
                    TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                    GameTagDef havenDefenseTag = DefCache.GetDef<GameTagDef>("MissionTypeHavenDefense_MissionTagDef");
                    bool havenDefense = controller.TacMission.MissionData.MissionType.Tags.Contains(havenDefenseTag);
                    List<int> voidOmens = new List<int> { 3, 5, 7, 10, 14, 15, 16, 19 };

                    //VO#1 is harder ambushes
                    //VO#2 is All diplomatic penalties and rewards halved
                    //VO#3 is WP cost +50%

                    //VO#4 is limited deplyoment, extra XP

                    //VO#5 is haven defenders hostile; this is needed for victory kludge

                    //VO#7 is more mist in missions

                    //VO#10 is no limit to Delirium

                    //VO#12 is +50% strength of alien attacks on Havens

                    //VO#15 is more Umbra

                    //VO#16 is Umbras can appear anywhere and attack anyone

                    //V0#18 is extra defense points, less rewards



                    if (!tbtvRelevant)
                    {
                        voidOmens.Remove(15);
                        voidOmens.Remove(16);
                        voidOmens.Remove(19);
                    }

                    // Add faction objectives for void omens that are in play
                    foreach (int vo in voidOmens)
                    {
                        if (TFTVVoidOmens.VoidOmensCheck[vo])
                        {
                            if (vo != 5 || vo == 5 && havenDefense)
                            {
                                _ODISitrepList.Add((_voidOmenSprite, TFTVCommonMethods.ConvertKeyToString("VOID_OMEN_TITLE_" + vo), ODIDetailType.VoidOmen));
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static readonly string _deliriumMaxTipKey = "KEY_DELIRIUM_UI_MAX_TIP";
            private static readonly string _deliriumMedTipKey = "KEY_DELIRIUM_UI_MED_TIP";
            private static readonly string _deliriumMinTipKey = "KEY_DELIRIUM_UI_LOW_TIP";
            private static readonly string _justVoidOmens = "TFTV_ODI_WIDGET_VOID_OMENS";
            private static readonly string _umbraTBTV = "TFTV_TBTV_UMBRA_EFFECT_DESC";
            private static readonly string _evolvedUmbraTBTV = "TFTV_TBTV_UMBRA_EVOLVED_EFFECT_DESC";
            private static readonly string _tbtvGeneralDescription = "TFTV_TBTV_GENERAL_DESC";
            private static readonly string _tbtvMFDStatus = "TFTV_TBTV_MFD_EFFECT_DESC";
            private static readonly string _tbtvCallReinforcements = "TFTV_TBTV_CALLREINFORCEMENTS_EFFECT_DESC";
            private static readonly string _tbtvChances = "TFTV_TBTV_CHANCES_DESC";
            private static readonly string _tbtvChancesVOTBTVEverywhere = "TFTV_TBTV_CHANCES_VO_TBTV_EVERYWHERE";
            private static readonly string _tbtvChancesBase = "TFTV_TBTV_CHANCES_BASE";
            private static readonly string _tbtvChancesMoreTBTV = "TFTV_TBTV_CHANCES_VO_MORE_TBTV";
            private static readonly string _tbtvChancesAcheron = "TFTV_TBTV_CHANCES_ACHERONS";


            private static void PopulateDictionary(bool TBTVRelevant)
            {
                try
                {
                    _tbtvGeneralSprite = DefCache.GetDef<ViewElementDef>("E_ViewElement [TBTV_Hidden_AbilityDef]").SmallIcon;
                    _tbtvOnAttackSprite = DefCache.GetDef<ViewElementDef>("E_Visuals [TBTV_OnAttack_StatusDef]").SmallIcon;
                    _tbtvOnDeathSprite = DefCache.GetDef<ViewElementDef>("E_Visuals [OilCrabOnDeath]").SmallIcon;
                    _tbtvOnTurnEndSprite = DefCache.GetDef<ViewElementDef>("E_Visuals [TBTV_OnTurnEnd_StatusDef]").SmallIcon;
                    _voidOmenSprite = DefCache.GetDef<ViewElementDef>("E_ViewElement [Acheron_Harbinger_AbilityDef]").SmallIcon;
                    _deliriumStatusSprite = DefCache.GetDef<ViewElementDef>("E_Visuals [Corruption_StatusDef]").SmallIcon;

                    if (ODITactical._ODISitrepList.Count > 0)
                    {
                        return;
                    }

                    if (!TBTVRelevant)
                    {

                        _ODISitrepList.Add((null, TFTVCommonMethods.ConvertKeyToString(_justVoidOmens), ODIDetailType.ODIDescription));
                        GetVoidOmensTacticalText(TBTVRelevant);
                        return;
                    }

                    if (TFTVTouchedByTheVoid.TBTVVariable >= 4)
                    {
                        _ODISitrepList.Add((_deliriumStatusSprite, TFTVCommonMethods.ConvertKeyToString(_deliriumMaxTipKey).Replace("-", ""), ODIDetailType.ODIDescription));
                        GetVoidOmensTacticalText();
                        _ODISitrepList.Add((_tbtvGeneralSprite, TFTVCommonMethods.ConvertKeyToString(_tbtvGeneralDescription), ODIDetailType.TBTVDescription));
                        _ODISitrepList.Add((_tbtvOnDeathSprite, TFTVCommonMethods.ConvertKeyToString(_evolvedUmbraTBTV), ODIDetailType.TBTVEffect));
                        _ODISitrepList.Add((_tbtvOnAttackSprite, TFTVCommonMethods.ConvertKeyToString(_tbtvMFDStatus), ODIDetailType.TBTVEffect));
                        _ODISitrepList.Add((_tbtvOnTurnEndSprite, TFTVCommonMethods.ConvertKeyToString(_tbtvCallReinforcements), ODIDetailType.TBTVEffect));

                    }
                    else if (TFTVTouchedByTheVoid.TBTVVariable >= 2)
                    {
                        if (TFTVVoidOmens.VoidOmensCheck[10])
                        {
                            _ODISitrepList.Add((_deliriumStatusSprite, TFTVCommonMethods.ConvertKeyToString(_deliriumMaxTipKey).Replace("-", ""), ODIDetailType.ODIDescription));
                        }
                        else
                        {
                            _ODISitrepList.Add((_deliriumStatusSprite, TFTVCommonMethods.ConvertKeyToString(_deliriumMedTipKey).Replace("-", ""), ODIDetailType.ODIDescription));
                        }
                        GetVoidOmensTacticalText();
                        _ODISitrepList.Add((_tbtvGeneralSprite, TFTVCommonMethods.ConvertKeyToString(_tbtvGeneralDescription), ODIDetailType.TBTVDescription));
                        if (TFTVTouchedByTheVoid.TBTVVariable == 3)
                        {

                            _ODISitrepList.Add((_tbtvOnDeathSprite, TFTVCommonMethods.ConvertKeyToString(_evolvedUmbraTBTV), ODIDetailType.TBTVEffect));
                        }
                        else
                        {
                            _ODISitrepList.Add((_tbtvOnDeathSprite, TFTVCommonMethods.ConvertKeyToString(_umbraTBTV), ODIDetailType.TBTVEffect));
                        }

                        _ODISitrepList.Add((_tbtvOnAttackSprite, TFTVCommonMethods.ConvertKeyToString(_tbtvMFDStatus), ODIDetailType.TBTVEffect));

                    }
                    else
                    {
                        if (TFTVVoidOmens.VoidOmensCheck[10])
                        {
                            _ODISitrepList.Add((_deliriumStatusSprite, TFTVCommonMethods.ConvertKeyToString(_deliriumMaxTipKey).Replace("-", ""), ODIDetailType.ODIDescription));
                        }
                        else
                        {
                            _ODISitrepList.Add((_deliriumStatusSprite, TFTVCommonMethods.ConvertKeyToString(_deliriumMinTipKey).Replace("-", ""), ODIDetailType.ODIDescription));
                        }

                        GetVoidOmensTacticalText();
                        _ODISitrepList.Add((_tbtvGeneralSprite, TFTVCommonMethods.ConvertKeyToString(_tbtvGeneralDescription), ODIDetailType.TBTVDescription));
                        ODITactical._ODISitrepList.Add((_tbtvOnDeathSprite, TFTVCommonMethods.ConvertKeyToString(_umbraTBTV), ODIDetailType.TBTVEffect));
                    }

                    ODITactical._ODISitrepList.Add(((Sprite icon, string text, ODIDetailType))(null, _tbtvChances, ODIDetailType.TBTVChance));

                    if (TFTVVoidOmens.VoidOmensCheck[16])
                    {
                        ODITactical._ODISitrepList.Add((_voidOmenSprite, TFTVCommonMethods.ConvertKeyToString(_tbtvChancesVOTBTVEverywhere), ODIDetailType.TBTVModifier));
                    }
                    else
                    {
                        ODITactical._ODISitrepList.Add((_voidOmenSprite, _tbtvChancesBase, ODIDetailType.TBTVModifier));
                    }

                    if (TFTVVoidOmens.VoidOmensCheck[15])
                    {
                        ODITactical._ODISitrepList.Add((_voidOmenSprite, TFTVCommonMethods.ConvertKeyToString(_tbtvChancesMoreTBTV), ODIDetailType.TBTVModifier));
                    }

                    _ODISitrepList.Add((_voidOmenSprite, _tbtvChancesAcheron, ODIDetailType.TBTVModifier));

                    //  _ODISitrepDictionary.Add("TFTV_TBTV_CHANCES_TOTAL", null);
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }



            //   private static int _baseChanceTBTV = 0;
            //  private static int _acheronChanceTBTV = 0;


            public static int GetBaseTouchedByTheVoidChances(TacticalLevelController controller)
            {
                try
                {
                    TacticalFaction phoenix = controller.GetFactionByCommandName("px");

                    int totalDeliriumOnMission = 0;

                    if (TFTVVoidOmens.VoidOmensCheck[16])
                    {
                        totalDeliriumOnMission = 16;
                    }
                    else
                    {
                        foreach (TacticalActor actor in phoenix.TacticalActors)
                        {
                            if (actor.CharacterStats.Corruption.Value > 0)
                            {
                                totalDeliriumOnMission += (int)actor.CharacterStats.Corruption.Value.BaseValue;
                            }
                        }

                        totalDeliriumOnMission /= 2;
                    }

                    // _baseChanceTBTV = totalDeliriumOnMission;

                    return totalDeliriumOnMission;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


            private static bool CheckIfODIWidgetRelevant(TacticalLevelController controller)
            {
                try
                {

                    bool pandoransPresent = controller.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("aln"));
                    bool canHaveTBTV = TFTVTouchedByTheVoid.UmbraResearched && pandoransPresent;

                    GameTagDef havenDefenseTag = DefCache.GetDef<GameTagDef>("MissionTypeHavenDefense_MissionTagDef");
                    bool havenDefense = controller.TacMission.MissionData.MissionType.Tags.Contains(havenDefenseTag);
                    List<int> voidOmens = new List<int> { 3, 7, 10, 14 };

                    if (havenDefense)
                    {
                        voidOmens.Add(5);
                    }

                    if (pandoransPresent)
                    {
                        voidOmens.Add(15);
                        voidOmens.Add(16);
                        voidOmens.Add(19);
                    }

                    bool relevantVoidOmens = false;
                    // Add faction objectives for void omens that are in play
                    foreach (int vo in voidOmens)
                    {
                        if (TFTVVoidOmens.VoidOmensCheck[vo])
                        {
                            relevantVoidOmens = true;
                            break;
                        }
                    }

                    // TFTVLogger.Always($"canHaveTBTV: {canHaveTBTV}, relevantVoidOmens: {relevantVoidOmens}");

                    if (relevantVoidOmens || canHaveTBTV)
                    {
                        PopulateDictionary(canHaveTBTV);

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


            private static string AdjustODIElementText(string element)
            {

                try
                {
                    TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                    string adjustedText = null;

                    float chances = GetBaseTouchedByTheVoidChances(controller);

                    float totalChances = chances;

                    if (TFTVVoidOmens.VoidOmensCheck[15])
                    {
                        totalChances *= 2;
                    }

                    int harbingers = TFTVTouchedByTheVoid.Umbra.UmbraTactical.CheckForAcheronHarbingers(controller);

                    totalChances += harbingers * 10;

                    if (element == _tbtvChancesBase || element == _tbtvChancesAcheron)
                    {
                        if (element == _tbtvChancesBase)
                        {
                            adjustedText = TFTVCommonMethods.ConvertKeyToString(element).Replace("{0}", chances.ToString());
                        }
                        else if (element == _tbtvChancesAcheron)
                        {
                            if (harbingers == 0)
                            {
                                adjustedText = null;
                            }
                            else
                            {
                                //  int _acheronChanceTBTV = harbingers * 10;
                                adjustedText = TFTVCommonMethods.ConvertKeyToString(element).Replace("{0}", (harbingers).ToString());
                            }
                        }

                    }
                    else if (element == _tbtvChances)
                    {
                        adjustedText = TFTVCommonMethods.ConvertKeyToString(element).Replace("{0}", totalChances.ToString());
                    }
                    else
                    {
                        adjustedText = element;
                    }


                    return adjustedText;

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }

            public static void HideODITooltipFailSafe()
            {
                try
                {
                    //  TFTVLogger.Always($"running hideODITooltipFailSafe");
                    _oDIWidget?.GetComponent<ODIWidgetTooltip>()?.HideTooltip();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            public static void CreateODITacticalWidget(UIModuleObjectives moduleObjectives)
            {
                try
                {
                    TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                    UIModuleObjectives uIModuleObjectives = controller.View.TacticalModules.ObjectivesModule;

                    UIModuleNavigation uIModuleNavigation = controller.View.TacticalModules.NavigationModule;

                    Resolution resolution = Screen.currentResolution;
                    float resolutionFactorWidth = (float)resolution.width / 1920f;
                    float resolutionFactorHeight = (float)resolution.height / 1080f;

                    if (_oDIWidget == null && CheckIfODIWidgetRelevant(controller))
                    {
                        Vector3 position = new Vector3(resolution.width / 2 - 200, uIModuleObjectives.transform.Find("Objectives_Text").position.y, 0);

                        _oDIWidget = new GameObject("ODIWidgetObject", typeof(RectTransform));
                        _oDIWidget.GetComponent<RectTransform>().SetParent(uIModuleNavigation.transform, false);
                        ODIWidgetTooltip dynamicTooltipWithHover = _oDIWidget.AddComponent<ODIWidgetTooltip>();
                        dynamicTooltipWithHover.transform.SetParent(_oDIWidget.transform, false);
                        _oDIWidget.transform.position = position;

                        string mainText = TFTVCommonMethods.ConvertKeyToString("TFTV_ODI_WIDGET");
                        Sprite voidIcon = GetRightVoidIcon();

                        dynamicTooltipWithHover.CreateUIElement(position, mainText, voidIcon, _ODISitrepList, uIModuleNavigation.transform);
                    }
                    else
                    {
                        _oDIWidget?.GetComponent<ODIWidgetTooltip>().UpdateDetails();
                        _oDIWidget?.GetComponent<ODIWidgetTooltip>().HideTooltip();
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        internal class CaptureTacticalWidget
        {
            public class AircraftUI : MonoBehaviour
            {


                public Transform uiParent;  // Parent object for the entire UI panel
                private GameObject _spacesPanel;
                public Sprite aircraftSprite;  // Background image of the aircraft
                public Sprite aircraftTypeIcon;  // Icon for the type of aircraft
                public string aircraftName;
                public int totalCapacity;  // Example total capacity
                public int filledSpaces;    // Example filled spaces
                //private readonly Font _font = ; // Resources.FindObjectsOfTypeAll<Font>().FirstOrDefault(f => f.name.Equals("Purista Semibold"));

                // List of currently captured aliens (to be updated dynamically)
                public List<TacticalActor> capturedAliens = new List<TacticalActor>();

                private readonly Color _dimWhite = new Color(0.757f, 0.988f, 1.0f);
                private GameObject _alienPanel1;
                private GameObject _alienPanel2;
                private GameObject _alienPanel3;// Reference to the alien panel
                private GameObject _alienPanel4;
                List<GameObject> _alienPanels = new List<GameObject>();
                private GameObject freeSpaceTextObject; // Reference to the free space text object

                public void Init()
                {
                    try
                    {
                        CreateAircraftUI();
                        UpdateUI();  // Initial UI update         
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                // Method to create the entire UI for the aircraft
                void CreateAircraftUI()
                {
                    try
                    {
                        Resolution resolution = Screen.currentResolution;
                        float resolutionFactorWidth = (float)resolution.width / 1920f;
                        float resolutionFactorHeight = (float)resolution.height / 1080f;

                        // Create a parent panel for the entire UI
                        GameObject aircraftPanel = new GameObject("AircraftPanel");
                        aircraftPanel.transform.SetParent(uiParent, false);
                        RectTransform panelRect = aircraftPanel.AddComponent<RectTransform>();

                        if (aircraftSprite != null)
                        {
                            panelRect.position = new Vector3(1750 * resolutionFactorWidth, 830 * resolutionFactorHeight, 0.0f);
                        }
                        else
                        {
                            panelRect.position = new Vector3(1750 * resolutionFactorWidth, 930 * resolutionFactorHeight, 0.0f);

                        }
                        panelRect.sizeDelta = new Vector2(600, 500);
                        aircraftPanel.AddComponent<CanvasRenderer>();

                        // 1. Background Image (aircraft picture)
                        Image backgroundImage = aircraftPanel.AddComponent<Image>();
                        backgroundImage.sprite = aircraftSprite;  // Set the aircraft image
                        backgroundImage.type = Image.Type.Simple;
                        backgroundImage.preserveAspect = true;  // Make sure the image is not stretched
                                                                //  backgroundImage.rectTransform.sizeDelta = new Vector2(600 * resolutionFactorWidth, 330 * resolutionFactorHeight);
                        if (aircraftSprite != null)
                        {
                            backgroundImage.color = new Color(backgroundImage.color.r, backgroundImage.color.g, backgroundImage.color.b, 0.4f);
                        }
                        else
                        {
                            backgroundImage.color = new Color(0, 0, 0, 0);
                            panelRect.sizeDelta = new Vector2(600, 300);
                        }

                        GameObject backgroundContainer = new GameObject("BackgroundContainer");
                        backgroundContainer.transform.SetParent(aircraftPanel.transform, false);
                        RectTransform containerRect = backgroundContainer.AddComponent<RectTransform>();
                        containerRect.sizeDelta = backgroundImage.rectTransform.sizeDelta;

                        // Step 2: Move backgroundImage as a child of this container
                        backgroundImage.transform.SetParent(backgroundContainer.transform, false);

                        // Step 3: Flip the background image by setting localScale on x-axis
                        containerRect.localScale = new Vector3(-1, 1, 1);


                        // 2. Aircraft Name Text
                        GameObject nameTextObject = new GameObject("AircraftName");
                        nameTextObject.transform.SetParent(aircraftPanel.transform, false);
                        Text nameText = nameTextObject.AddComponent<Text>();
                        nameText.text = aircraftName;
                        nameText.font = PuristaSemiboldFontCache;
                        nameText.fontSize = 35;
                        nameText.color = WhiteColor;
                        nameText.alignment = TextAnchor.MiddleLeft;
                        RectTransform nameTextRect = nameText.GetComponent<RectTransform>();
                        nameTextRect.sizeDelta = new Vector2(290, 100);
                        nameTextRect.anchoredPosition = new Vector2(100, -70);

                        // 3. Aircraft Type Icon
                        GameObject iconObject = new GameObject("AircraftIcon");
                        iconObject.transform.SetParent(aircraftPanel.transform, false);
                        Image iconImage = iconObject.AddComponent<Image>();
                        iconImage.sprite = aircraftTypeIcon;  // Set the aircraft type icon
                        RectTransform iconRect = iconImage.GetComponent<RectTransform>();
                        iconRect.sizeDelta = new Vector2(60, 60);  // Example size
                        iconRect.anchoredPosition = new Vector2(-80, -70);  // Adjusted position to the left



                        AddOutlineToIcon addOutlineToIcon = iconObject.GetComponent<AddOutlineToIcon>() ?? iconObject.AddComponent<AddOutlineToIcon>();
                        addOutlineToIcon.icon = iconObject;
                        addOutlineToIcon.InitOrUpdate();



                        // 4. Free Space Text
                        freeSpaceTextObject = new GameObject("FreeSpaceText");
                        freeSpaceTextObject.transform.SetParent(aircraftPanel.transform, false);
                        Text freeSpaceText = freeSpaceTextObject.AddComponent<Text>();
                        freeSpaceText.font = PuristaSemiboldFontCache; //Resources.GetBuiltinResource<Font>("Arial.ttf");
                        freeSpaceText.fontSize = 30;
                        freeSpaceText.color = WhiteColor;
                        RectTransform freeSpaceTextRect = freeSpaceText.GetComponent<RectTransform>();
                        freeSpaceTextRect.sizeDelta = new Vector2(350, 50);  // Increased width for text
                        freeSpaceTextRect.anchoredPosition = new Vector2(70, -130);  // Moved further down and to the right

                        // 5. Create a Horizontal Layout Group for filled/empty spaces
                        GameObject spacesPanel = new GameObject("SpacesPanel");
                        spacesPanel.transform.SetParent(aircraftPanel.transform, false);
                        RectTransform spacesRect = spacesPanel.AddComponent<RectTransform>();
                        spacesRect.sizeDelta = new Vector2(350, 20);  // Increased size for more space
                        spacesRect.anchoredPosition = new Vector2(70, -160);  // Moved down and to the right
                        HorizontalLayoutGroup layoutGroupSpacePanel = spacesPanel.AddComponent<HorizontalLayoutGroup>();
                        layoutGroupSpacePanel.spacing = 5;  // Set spacing between icons
                        layoutGroupSpacePanel.childAlignment = TextAnchor.MiddleLeft;
                        _spacesPanel = spacesPanel;

                        // 6. Add the alien capture panel (new section)

                        int alienPanelsNeeded = (totalCapacity + 5) / 6;

                        for (int x = 0; x < alienPanelsNeeded; x++)
                        {
                            CreateAlienCapturePanel(aircraftPanel.transform, x);
                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                // Method to create a space icon
                private void CreateSpaceIcon(Transform parent, Color color, bool hasOutline = false)
                {
                    try
                    {

                        GameObject icon = new GameObject("SpaceIcon");
                        icon.transform.SetParent(parent, false);
                        Image iconImage = icon.AddComponent<Image>();

                        iconImage.preserveAspect = true;

                        // Add and configure the Outline component

                        iconImage.color = color;  // Red for filled, white for empty

                        AddOutlineToIcon addOutlineToIcon = icon.GetComponent<AddOutlineToIcon>() ?? icon.AddComponent<AddOutlineToIcon>();
                        addOutlineToIcon.icon = icon;
                        addOutlineToIcon.InitOrUpdate();

                        RectTransform rt = icon.GetComponent<RectTransform>();
                        rt.sizeDelta = new Vector2(20, 20);  // Adjust size for the icon
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }


                // Method to create the alien capture panel
                private void CreateAlienCapturePanel(Transform parent, int rowNumber)
                {
                    try
                    {
                        //  TFTVLogger.Always($"creating alien panel {rowNumber}");

                        GameObject alienPanel = new GameObject($"AlienCapturePanel{rowNumber}");
                        alienPanel.transform.SetParent(parent, false);
                        RectTransform alienPanelRect = alienPanel.AddComponent<RectTransform>();
                        alienPanelRect.sizeDelta = new Vector2(370, 85);  // Adjust as necessary
                        alienPanelRect.anchoredPosition = new Vector2(75, -90 * rowNumber - 220);  // Adjust position

                        // Add a black background image to the alien capture panel
                        Image background = alienPanel.AddComponent<Image>();
                        background.color = new Color(0, 0, 0, 0.5f);
                        background.type = Image.Type.Sliced;  // Optionally make it sliced for better scaling
                        background.rectTransform.sizeDelta = new Vector2(370, 70);

                        if (rowNumber == 0)
                        {
                            _alienPanel1 = alienPanel;
                        }
                        else if (rowNumber == 1)
                        {
                            _alienPanel2 = alienPanel;
                        }
                        else if (rowNumber == 2)
                        {
                            _alienPanel3 = alienPanel;
                        }
                        else
                        {
                            _alienPanel4 = alienPanel;

                        }

                        alienPanel.SetActive(false);

                        _alienPanels.Add(alienPanel);
                        // Adjust size
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                // Method to update the UI dynamically
                public void UpdateUI()
                {
                    try
                    {
                        Resolution resolution = Screen.currentResolution;
                        float resolutionFactorWidth = (float)resolution.width / 1920f;
                        float resolutionFactorHeight = (float)resolution.height / 1080f;

                        foreach (GameObject alienPanel in _alienPanels)
                        {
                            alienPanel.SetActive(false);

                            // Clear existing alien icons and space squares
                            foreach (Transform child in alienPanel.transform)
                            {
                                Destroy(child.gameObject);
                            }
                        }

                        foreach (Transform child in _spacesPanel.transform)
                        {
                            if (child.gameObject.name.Contains("SpaceIcon"))
                            {
                                Destroy(child.gameObject);
                            }
                        }

                        for (int i = 0; i < filledSpaces; i++)
                        {
                            CreateSpaceIcon(_spacesPanel.transform, _dimWhite); //new Color(0.329f, 0.376f, 0.455f));  // grey for filled
                        }
                        for (int i = filledSpaces; i < Mathf.Max(16, totalCapacity); i++)
                        {
                            if (i < totalCapacity)
                            {
                                CreateSpaceIcon(_spacesPanel.transform, Color.gray);
                            }
                            else
                            {
                                CreateSpaceIcon(_spacesPanel.transform, Color.clear);
                            }
                            // white for empty
                        }

                        // Loop through captured aliens and create the icons and squares
                        for (int i = 0; i < capturedAliens.Count; i++)
                        {
                            TacticalActor alien = capturedAliens[i];

                            // Alien Icon
                            GameObject alienIconObject = new GameObject($"AlienIcon_{i}");
                            int position = i;
                            if (i < 6)
                            {
                                _alienPanel1.SetActive(true);
                                alienIconObject.transform.SetParent(_alienPanel1.transform, false);
                            }
                            else if (i >= 6 && i < 13)
                            {
                                _alienPanel2.SetActive(true);
                                alienIconObject.transform.SetParent(_alienPanel2.transform, false);
                                position -= 6;
                            }
                            else if (i >= 13 && i < 19)
                            {
                                _alienPanel3.SetActive(true);
                                alienIconObject.transform.SetParent(_alienPanel3.transform, false);
                                position -= 12;
                            }
                            else
                            {
                                _alienPanel3.SetActive(true);
                                alienIconObject.transform.SetParent(_alienPanel3.transform, false);
                                position -= 18;
                            }


                            Image alienIconImage = alienIconObject.AddComponent<Image>();
                            alienIconImage.sprite = alien.ViewElementDef.SmallIcon;  // Use the alien sprite
                            RectTransform alienIconRect = alienIconObject.GetComponent<RectTransform>();
                            alienIconRect.sizeDelta = new Vector2(50, 50);  // Adjust size for the alien icon
                            alienIconRect.anchoredPosition = new Vector2((position * 60 - 150) * resolutionFactorWidth, 0);  // Adjust the position of each alien icon


                            AddOutlineToIcon addOutlineToIcon = alienIconObject.GetComponent<AddOutlineToIcon>() ?? alienIconObject.AddComponent<AddOutlineToIcon>();
                            addOutlineToIcon.icon = alienIconObject;
                            addOutlineToIcon.InitOrUpdate();


                            if (!alienIconObject.GetComponent<EventTrigger>())
                            {
                                alienIconObject.AddComponent<EventTrigger>();
                            }


                            EventTrigger eventTrigger = alienIconObject.GetComponent<EventTrigger>();
                            eventTrigger.triggers.Clear();
                            EventTrigger.Entry click = new EventTrigger.Entry
                            {
                                eventID = EventTriggerType.PointerClick
                            };

                            click.callback.AddListener((eventData) =>
                            {
                                alien.TacticalActorView.DoCameraChase();
                            });

                            eventTrigger.triggers.Add(click);


                            // Alien Space Squares - two rows if slots > 4
                            int spaceUsage = TFTVCapturePandorans.CalculateCaptureSlotCost(alien.GameTags.ToList());  // Retrieve the space usage for the alien
                            int maxPerRow = 4;  // Max number of squares per row
                            for (int j = 0; j < spaceUsage; j++)
                            {
                                GameObject spaceSquare = new GameObject($"AlienSpace_{i}_{j}");
                                spaceSquare.transform.SetParent(alienIconObject.transform, false);

                                // Add and configure the Image component
                                Image spaceSquareImage = spaceSquare.AddComponent<Image>();
                                spaceSquareImage.color = _dimWhite;  // Space color

                                // Add and configure the Outline component
                                Outline outline = spaceSquare.AddComponent<Outline>();
                                outline.effectColor = Color.black;     // Outline color
                                outline.effectDistance = new Vector2(4, -4);  // Outline thickness (adjust as needed)

                                // Configure the RectTransform
                                RectTransform spaceRect = spaceSquare.GetComponent<RectTransform>();
                                spaceRect.sizeDelta = new Vector2(8, 8);  // Adjust size

                                // Row and column logic (Start from bottom-left corner)
                                int row = j / maxPerRow;     // Row (resets after 4 squares)
                                int column = j % maxPerRow;  // Column

                                // Adjust position to start from bottom-left of the icon
                                spaceRect.anchoredPosition = new Vector2(column * 12 - (alienIconRect.sizeDelta.x / 3), -row * 12 - (alienIconRect.sizeDelta.y / 2));  // Align from bottom-left
                            }

                        }

                        // Update free space text
                        int freeSpaces = totalCapacity - filledSpaces;

                        //   TFTVLogger.Always($"Free capture slots: {freeSpaces.ToString()}/{totalCapacity.ToString()}");

                        freeSpaceTextObject.GetComponent<Text>().text = $"{TFTVCommonMethods.ConvertKeyToString("TFTV_KEY_CAPTURE_FREE_SLOTS")} {freeSpaces}/{totalCapacity}";

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }


            public static void ClearData()
            {
                try
                {
                    CaptureUI = null;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }

            public static AircraftUI CaptureUI = null;


            /// <summary>
            /// This is run on UIModuleObjectives.Init(). It's aborted if Limite Capture is off, no capture is possible due to lack of space/capability, or if there are no aliens.
            /// </summary>
            public static void CreateCaptureTacticalWidget(UIModuleObjectives moduleObjectives)
            {
                try
                {
                    TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                    //  TFTVLogger.Always($"CreatCaptureTacticalWidget: {TFTVNewGameOptions.LimitedCaptureSetting} {ContainmentFacilityPresent} {AircraftCaptureCapacity}");

                    if (!TFTVNewGameOptions.LimitedCaptureSetting || !ContainmentFacilityPresent && AircraftCaptureCapacity <= 0)
                    {
                        return;
                    }

                    if (!controller.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("aln")))
                    {
                        return;
                    }

                    if (CaptureUI == null)
                    {

                        // TFTVLogger.Always($"got here");

                        UIModuleNavigation uIModuleNavigation = controller.View.TacticalModules.NavigationModule;

                        Resolution resolution = Screen.currentResolution;
                        float resolutionFactorWidth = (float)resolution.width / 1920f;
                        float resolutionFactorHeight = (float)resolution.height / 1080f;

                        // Create the aircraft UI panel as a child of uIModuleNavigation
                        GameObject aircraftUIPanel = new GameObject("AircraftCaptureWidget", typeof(RectTransform));
                        RectTransform aircraftRectTransform = aircraftUIPanel.GetComponent<RectTransform>();

                        // Set it as a child of uIModuleNavigation
                        aircraftRectTransform.SetParent(uIModuleNavigation.transform, false);

                        // Set anchors to ensure proper scaling and positioning relative to parent
                        aircraftRectTransform.anchorMin = new Vector2(0.5f, 0.5f); // Center the UI element
                        aircraftRectTransform.anchorMax = new Vector2(0.5f, 0.5f); // Center the UI element
                        aircraftRectTransform.pivot = new Vector2(0.5f, 0.5f);     // Set pivot to center

                        // Add your UI logic (AircraftUI)
                        AircraftUI uiScript = aircraftUIPanel.AddComponent<AircraftUI>();
                        uiScript.transform.SetParent(aircraftUIPanel.transform, false);
                        uiScript.uiParent = aircraftUIPanel.transform;
                        //   uiScript.transform.position = new Vector3(550 * resolutionFactorWidth, 1045 * resolutionFactorHeight, 0.0f);
                        // Parent the UI

                        if (AircraftViewElement != null && AircraftViewElement.Length > 5)
                        {
                            //  TFTVLogger.Always($"AircraftName: {AircraftName}, {AircraftViewElement}");

                            ViewElementDef viewElementDef = (ViewElementDef)Repo.GetDef(AircraftViewElement);

                            uiScript.aircraftSprite = viewElementDef.InventoryIcon ?? null;

                            if (viewElementDef.InventoryIcon == null)
                            {
                                uiScript.aircraftTypeIcon = viewElementDef.SmallIcon;
                            }
                            else
                            {
                                uiScript.aircraftTypeIcon = viewElementDef.LargeIcon;
                            }
                        }
                        else
                        {
                            uiScript.aircraftSprite = Helper.CreateSpriteFromImageFile("Manticore_transparent_uinomipmaps.png");
                            uiScript.aircraftTypeIcon = DefCache.GetDef<ViewElementDef>("PP_Manticore_View").LargeIcon;
                        }

                        uiScript.aircraftName = AircraftName;  // Set dynamically if needed

                        if (AircraftCaptureCapacity != -1)
                        {
                            uiScript.totalCapacity = Math.Min(ContainmentSpaceAvailable, AircraftCaptureCapacity);
                        }
                        else
                        {
                            uiScript.totalCapacity = ContainmentSpaceAvailable;
                        }

                        uiScript.filledSpaces = 0;    // Example, adjust accordingly

                        CaptureUI = uiScript;

                        //  TFTVLogger.Always($"widget setup");
                        // Call the UI creation method to build the interface
                        uiScript.Init();
                    }

                    UpdateCaptureUI();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


            /// <summary>
            /// This is run when a Pandoran is chosen or removed from priority extraction list and on UIModuleObjectives.Init()
            /// </summary>
            public static void UpdateCaptureUI()
            {
                try
                {
                    TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                    if (!controller.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("aln")))
                    {
                        return;
                    }

                    TacticalFaction aliens = controller.GetFactionByCommandName("aln");

                    GameTagDef captureTag = Shared.SharedGameTags.CapturableTag;

                    List<TacticalActor> priorityAliens = aliens.TacticalActors.Where(a => a.Status.HasStatus<ReadyForCapturesStatus>()).ToList();
                    List<TacticalActor> paralyzedAliens = aliens.TacticalActors.Where(a =>
                    a.IsAlive
                    && a.Status.HasStatus<ParalysedStatus>()
                    && a.GameTags.Contains(captureTag)
                    && !priorityAliens.Contains(a))
                        .ToList();

                    CaptureUI.capturedAliens.Clear();
                    CaptureUI.filledSpaces = 0;
                    int availableCaptureslotsCounter = CaptureUI.totalCapacity;//CachedACC;

                    foreach (TacticalActor priorityCaptureAlien in priorityAliens)
                    {
                        int slotCost = CalculateCaptureSlotCost(priorityCaptureAlien.GameTags.ToList());

                        availableCaptureslotsCounter -= slotCost;
                        CaptureUI.capturedAliens.Add(priorityCaptureAlien);
                        CaptureUI.filledSpaces += slotCost;
                    }

                    paralyzedAliens = paralyzedAliens.OrderByDescending(taur => CalculateCaptureSlotCost(taur.GameTags.ToList())).ToList();

                    foreach (TacticalActor otherParalyzedAlien in paralyzedAliens)
                    {
                        //  TFTVLogger.Always($"paralyzed {otherParalyzedAlien.TacticalActorBaseDef.name}, aircraftCaptureCapacity is {availableCaptureslotsCounter}");
                        int slotCost = CalculateCaptureSlotCost(otherParalyzedAlien.GameTags.ToList());

                        if (availableCaptureslotsCounter >= slotCost)
                        {
                            //  TFTVLogger.Always($"{tacActorUnitResult1.TacticalActorBaseDef.name} added to capture list; available slots before that {availableCaptureslotsCounter}");
                            CaptureUI.capturedAliens.Add(otherParalyzedAlien);
                            CaptureUI.filledSpaces += slotCost;
                            availableCaptureslotsCounter -= slotCost;
                            //  TFTVLogger.Always($"{otherParalyzedAlien.TacticalActorBaseDef.name} added to capture list; available slots after {availableCaptureslotsCounter}");
                        }
                    }

                    CaptureUI.UpdateUI();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }
        }

        internal class DamagePrediction
        {

            //Patch to show correct damage prediction with mutations and Delirium 
            [HarmonyPatch(typeof(PhoenixPoint.Tactical.UI.Utils), "GetDamageKeywordValue")]
            public static class TFTV_Utils_GetDamageKeywordValue_DamagePredictionMutations_Patch
            {
                public static void Postfix(DamagePayload payload, DamageKeywordDef damageKeyword, TacticalActor tacticalActor, ref float __result)
                {
                    try
                    {
                        SharedData shared = GameUtl.GameComponent<SharedData>();
                        SharedDamageKeywordsDataDef damageKeywords = shared.SharedDamageKeywords;
                       
                        CorruptionStatusDef corruptionStatusDef = DefCache.GetDef<CorruptionStatusDef>("Corruption_StatusDef");


                        if (tacticalActor != null && corruptionStatusDef.DamageTypeDefs.Contains(damageKeyword.DamageTypeDef) && damageKeyword != damageKeywords.SyphonKeyword) //&& damageKeyword is PiercingDamageKeywordDataDef == false) 
                        {

                            float numberOfMutations = 0;

                            //   TFTVLogger.Always("GetDamageKeywordValue check passed");

                            foreach (TacticalItem armourItem in tacticalActor.BodyState.GetArmourItems())
                            {
                                if (armourItem.GameTags.Contains(Shared.SharedGameTags.AnuMutationTag))
                                {
                                    numberOfMutations++;
                                }
                            }

                            if (numberOfMutations > 0)
                            {
                                // TFTVLogger.Always("damage value is " + payload.GenerateDamageValue(tacticalActor.CharacterStats.BonusAttackDamage));

                                __result = payload.GenerateDamageValue(tacticalActor.CharacterStats.BonusAttackDamage) * (1f + (numberOfMutations * 2) / 100 * (float)tacticalActor.CharacterStats.Corruption);
                                // TFTVLogger.Always($"GetDamageKeywordValue invoked for {tacticalActor.DisplayName} and result is {__result}");
                                //  TFTVLogger.Always("result is " + __result +", damage increase is " + (1f + (((numberOfMutations * 2) / 100) * (float)tacticalActor.CharacterStats.Corruption)));
                            }

                            if(tacticalActor.Status!=null && tacticalActor.Status.HasStatus<TacStrengthDamageMultiplierStatus>()) 
                            {

                                float endurance = tacticalActor.CharacterStats.Endurance.Value.EndValue;
                                __result += payload.GenerateDamageValue(tacticalActor.CharacterStats.BonusAttackDamage) * endurance / 200f;

                            }

                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }








            /// <summary>
            /// This method is run on Tactical Start and it removes the damage prediction bar when aiming, because it's inaccurate
            /// </summary>

            public static void RemoveDamagePredictionBar()
            {
                try
                {
                    if (GameUtl.CurrentLevel() != null && GameUtl.CurrentLevel().GetComponent<TacticalLevelController>() != null)
                    {
                        UIModuleShootTargetHealthbar uIModuleShootTargetHealthbar = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>().View.TacticalModules.ShootTargetHealthBar;
                        uIModuleShootTargetHealthbar.ModulePanel.GetComponentsInChildren<Image>().FirstOrDefault(c => c.name.Equals("DamagePrediction")).gameObject.SetActive(false);
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


            [HarmonyPatch(typeof(UIStateShoot), "GetMinMaxPossibleDamage")]
            public static class UIStateShoot_GetMinMaxPossibleDamage_patch
            {
                public static bool Prefix(UIStateShoot __instance)
                {
                    try
                    {

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





        /*  private static void AddClickChaseTarget(TacticalActorBase tacticalActorBase, GameObject gameObject)
          {
              try
              {
                  if (!gameObject.GetComponent<EventTrigger>())
                  {
                      gameObject.AddComponent<EventTrigger>();
                  }

                  RectTransform rectTransform = gameObject.GetComponent<RectTransform>();

                  if(rectTransform == null)
                  {
                      rectTransform = gameObject.AddComponent<RectTransform>();
                  }

                  rectTransform.sizeDelta = new Vector2(100, 100);


                  EventTrigger eventTrigger = gameObject.GetComponent<EventTrigger>();
                  eventTrigger.triggers.Clear();

                  if (tacticalActorBase != null)
                  {
                      TFTVLogger.Always($"tacticalActorBase {tacticalActorBase}");

                      EventTrigger.Entry click = new EventTrigger.Entry
                      {
                          eventID = EventTriggerType.PointerClick
                      };

                      click.callback.AddListener((eventData) =>
                      {
                          TFTVLogger.Always($"clicked on {tacticalActorBase}");
                          tacticalActorBase.TacticalActorViewBase.DoCameraChase();

                      });

                      eventTrigger.triggers.Add(click);
                  }

              }
              catch (Exception e)
              {
                  TFTVLogger.Error(e);
                  throw;
              }
          }*/








        /* [HarmonyPatch(typeof(HealthbarStatusElement), "SetStatus")]
         public static class HealthbarStatusElement_SetStatus_MindControl_patch
         {
             public static void Postfix(HealthbarStatusElement __instance, ViewElementDef viewDef)
             {
                 try
                 {
                     TFTVLogger.Always($"viewDef {viewDef.name}");

                     if (viewDef.name == "E_Visuals [MindControl_StatusDef]")
                     {
                         HealthbarUIActorElement healthbarUIActorElement = __instance.GetComponentInParent<HealthbarUIActorElement>();

                         if (healthbarUIActorElement != null)
                         {
                             TacticalActorBase tacticalActorBase = typeof(HealthbarUIActorElement).GetField("_tacActorBase", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(healthbarUIActorElement) as TacticalActorBase;
                             TFTVLogger.Always($"found {tacticalActorBase}");

                             MindControlStatus mindControlStatus = tacticalActorBase.Status.GetStatus<MindControlStatus>();
                             if (mindControlStatus != null)
                             {
                                 TFTVLogger.Always($"got here");

                                 TacticalActor controllerActor = mindControlStatus.ControllerActor;

                                 if (controllerActor != null)
                                 {
                                     TFTVLogger.Always($"got here too");

                                     AddClickChaseTarget(controllerActor, __instance.gameObject);

                                 }
                             }
                         }
                     }
                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                     throw;
                 }
             }
         }*/


        /*[HarmonyPatch(typeof(InputController), "RefreshActions")]
            public static class InputController_Init_patch
            {
                public static bool Prefix(InputController __instance, ref InputAction[] ____activeActionsMap,
                   ref List<int> ____activeActionHashes, ref List<InputAction> ____actionList, ref List<AxisValue> ____axisValues,
                  ref List<KeyValue> ____keyValuesList, ref KeyValue[] ____keyValues, ref List<ActionAxisValue> ____actionAxisValues)
                {
                    try
                    {
                        MethodInfo InitSuperChordsMethodInfo = typeof(InputController).GetMethod("InitSuperChords", BindingFlags.Instance | BindingFlags.NonPublic);
                        MethodInfo IsAllowedAndSupportedMethodInfo = typeof(InputController).GetMethod("IsAllowedAndSupported", BindingFlags.Instance | BindingFlags.NonPublic);
                        MethodInfo GetAxisRawMethodInfo = typeof(InputController).GetMethod("GetAxisRaw", BindingFlags.Static | BindingFlags.NonPublic);
                        PropertyInfo KeyMaxHashFieldInfo = typeof(InputController).GetProperty("KeyMaxHash", BindingFlags.Instance | BindingFlags.NonPublic);



                        for (int i = 0; i < ____activeActionsMap.Length; i++)
                        {
                            ____activeActionsMap[i] = null;
                        }

                        TFTVLogger.Always($"got here0");

                        List<InputAction> list = ____activeActionHashes.Select(__instance.GetDefaultAction).ToList();
                        TFTVLogger.Always($"got here0bis; list count is {list.Count}, while  ____activeActionsMap.Length is {____activeActionsMap.Length}");

                        foreach (InputAction item in list)
                        {
                            TFTVLogger.Always($"item is {item?.Name} hash: {item?.Hash} ____activeActionsMap.Contains(item): {____activeActionsMap.Contains(item)}");

                            //  if (____activeActionsMap.Contains(item))

                                ____activeActionsMap[item.Hash] = item;

                        }
                        TFTVLogger.Always($"got here0ter");

                        ____actionList = list;
                        List<InputKey> source = (from action in list
                                                 from chord in action.Chords
                                                 from key in chord.Keys
                                                 where (bool)IsAllowedAndSupportedMethodInfo.Invoke(__instance, new object[] { key.GetInputType() })// IsAllowedAndSupported(key.GetInputType())
                                                 select key).Distinct().ToList();

                        TFTVLogger.Always($"got here1");

                        IEnumerable<InputKey> source2 = source.Where((InputKey key) => key.InputSource == InputSource.AxisTriggerPositive || key.InputSource == InputSource.AxisTriggerNegative || key.InputSource == InputSource.Axis);
                        List<AxisValue> prevAxisValues = ____axisValues ?? new List<AxisValue>();
                        ____axisValues = source2.Select((InputKey k) => new AxisValue(k.Name, k.GetInputType(), (AxisValue)GetAxisRawMethodInfo.Invoke(__instance, new object[] { k.Name, prevAxisValues }), __instance)).ToList();

                        TFTVLogger.Always($"and here2");

                        IEnumerable<InputKey> source3 = source.Where((InputKey key) => key.InputSource == InputSource.Key || key.InputSource == InputSource.AxisTriggerNegative || key.InputSource == InputSource.AxisTriggerPositive);
                        KeyValue[] prevKeyValues = ____keyValues ?? new KeyValue[(int)KeyMaxHashFieldInfo.GetValue(__instance)];

                        TFTVLogger.Always($"and here3");
                        ____keyValuesList = source3.Select((InputKey k) => new KeyValue(k, prevKeyValues[k.GetHashCode()], __instance)).ToList();
                        ____keyValues = new KeyValue[(int)KeyMaxHashFieldInfo.GetValue(__instance)];

                        TFTVLogger.Always($"whatabout here4");
                        for (int j = 0; j < ____keyValuesList.Count; j++)
                        {
                            int hashCode = ____keyValuesList[j].Key.GetHashCode();
                            ____keyValues[hashCode] = ____keyValuesList[j];
                        }

                        ____actionAxisValues = (from a in list
                                                where a.IsAxis
                                                select new ActionAxisValue(a, __instance)).ToList();

                        TFTVLogger.Always($"dont tell me this is the problem!");
                        InitSuperChordsMethodInfo.Invoke(__instance, new object[] { });// InitSuperChords();
                        __instance.IsInputSetLoaded = true;

                        TFTVLogger.Always($"got to the end! A miracle");

                        return false;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }*/

    }
}
