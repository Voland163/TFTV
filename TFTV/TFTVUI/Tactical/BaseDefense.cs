using Base.Cameras;
using Base.Core;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Levels;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static TFTV.TFTVUI.Tactical.Data;

namespace TFTV.TFTVUI.Tactical
{
    internal class BaseDefenseUI
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

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
                    float resolutionFactorWidth = Mathf.Max((float)resolution.width / 1920f,1);
                    float resolutionFactorHeight = Mathf.Max((float)resolution.height / 1080f,1);


                    float baseScale = Mathf.Max(Mathf.Min(resolutionFactorWidth, resolutionFactorHeight),1);


                    // Access UIModuleNavigation and set widgetContainer as its transform
                    TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();
                    UIModuleNavigation uIModuleNavigation = controller.View.TacticalModules.NavigationModule;
                    widgetContainer = uIModuleNavigation.transform;

                    // Dynamically create the leaderWidgetPrefab structure
                    baseDefenseWidgetPrefab = new GameObject("BaseDefenseWidget");
                    RectTransform rectTransform = baseDefenseWidgetPrefab.AddComponent<RectTransform>();

                    rectTransform.sizeDelta = new Vector2(410 * resolutionFactorWidth, 180 * resolutionFactorHeight);
                    rectTransform.position = new Vector2(245 * resolutionFactorWidth, 600 * resolutionFactorHeight);

                    GameObject backgroundImage = new GameObject("Background", typeof(RectTransform), typeof(Image));

                    backgroundImage.transform.SetParent(baseDefenseWidgetPrefab.transform); // Attach to the existing GameObject

                    RectTransform bgRect = backgroundImage.GetComponent<RectTransform>();
                    bgRect.sizeDelta = 
                        new Vector2(rectTransform.sizeDelta.x, rectTransform.sizeDelta.y / 1.7f);
                    bgRect.anchoredPosition = Vector2.zero + new Vector2(7 * resolutionFactorWidth, -40 * resolutionFactorHeight);

                    Image bgImage = backgroundImage.GetComponent<Image>();
                    bgImage.color = new Color(0, 0, 0, 0.7f); // Black with 50% opacity
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
                    iconImageRect.sizeDelta = new Vector2(30 * baseScale, 30 * baseScale);
                    iconImageRect.anchoredPosition = Vector2.zero + new Vector2(-185*resolutionFactorWidth, 38*resolutionFactorHeight);//Vector2.zero + new Vector2(-150, 38);
                    _iconImage = iconImage;

                    // Set up the name text
                    GameObject generatorHealthTextObj = new GameObject("PowerGeneratorsHealthText");
                    generatorHealthTextObj.transform.SetParent(baseDefenseWidgetPrefab.transform);
                    Text generatorHealthText = generatorHealthTextObj.AddComponent<Text>();
                    generatorHealthText.text = TFTVCommonMethods.ConvertKeyToString("TFTV_KEY_BASE_DEFENSE_GENERATORS").Replace("{0}", powerGeneratorHP.ToString()).Replace("{1}", consolesLeft.ToString()); // $"Generators at {powerGeneratorHP}%, can be vented {consolesLeft} times";
                    generatorHealthText.alignment = TextAnchor.MiddleLeft;
                    generatorHealthText.fontSize = (int)(35 * baseScale);
                    generatorHealthText.color = color;
                    generatorHealthText.font = PuristaSemiboldFontCache ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                    RectTransform rectGeneratorsHealthText = generatorHealthText.GetComponent<RectTransform>();
                    rectGeneratorsHealthText.sizeDelta = new Vector2(800*baseScale, 60* baseScale);
                    rectGeneratorsHealthText.localScale = new Vector2(0.5f, 0.5f);
                    rectGeneratorsHealthText.anchoredPosition = Vector2.zero + new Vector2(35*resolutionFactorWidth, 40*resolutionFactorHeight); //Vector2.zero + new Vector2(20, 40);
                    _generatorsHealth = generatorHealthText;

                    // Set up the tactic name text
                    GameObject reinforcementTitleTextObj = new GameObject("ReinforcementTitleText");
                    reinforcementTitleTextObj.transform.SetParent(backgroundImage.transform);
                    Text reinforcementTitleText = reinforcementTitleTextObj.AddComponent<Text>();
                    reinforcementTitleText.text = reinforcementsTitle; //need to complete
                    reinforcementTitleText.fontSize = (int)(40 * baseScale);
                    reinforcementTitleText.color = WhiteColor;
                    reinforcementTitleText.alignment = TextAnchor.MiddleLeft;
                    reinforcementTitleText.font = PuristaSemiboldFontCache ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                    RectTransform rectReinforcementTitleText = reinforcementTitleText.GetComponent<RectTransform>();
                    rectReinforcementTitleText.sizeDelta = new Vector2(820*baseScale, 60 * baseScale);
                    rectReinforcementTitleText.localScale = new Vector2(0.5f, 0.5f);
                    rectReinforcementTitleText.anchoredPosition = Vector2.zero + new Vector2(35 * resolutionFactorWidth, 30*resolutionFactorHeight);
                    _reinforcementTitle = reinforcementTitleText;

                    // Set up the tactic description text
                    GameObject reinforcementDescriptionTextObj = new GameObject("ReinforcementDescriptionText");
                    reinforcementDescriptionTextObj.transform.SetParent(backgroundImage.transform);
                    Text reinforcementDescriptionText = reinforcementDescriptionTextObj.AddComponent<Text>();
                    reinforcementDescriptionText.text = reinforcementsDescription;//need to complete
                    reinforcementDescriptionText.font = PuristaSemiboldFontCache ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                    reinforcementDescriptionText.fontSize = (int)(30 * baseScale);
                    reinforcementDescriptionText.color = Color.grey;
                    reinforcementDescriptionText.alignment = TextAnchor.MiddleLeft;
                    reinforcementDescriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
                    reinforcementDescriptionText.verticalOverflow = VerticalWrapMode.Overflow;
                    RectTransform recttacticDescriptionText = reinforcementDescriptionText.GetComponent<RectTransform>();
                    recttacticDescriptionText.sizeDelta = new Vector2(700 * baseScale, 60 * baseScale);
                    recttacticDescriptionText.localScale = new Vector2(0.5f, 0.5f);
                    recttacticDescriptionText.anchoredPosition = Vector2.zero + new Vector2(5 * resolutionFactorWidth, -20*resolutionFactorHeight);
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
}
