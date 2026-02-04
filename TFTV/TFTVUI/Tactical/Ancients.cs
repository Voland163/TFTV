using Base.Core;
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

                    // Don't wipe out non-click triggers (like tooltip hover) that were added elsewhere.
                    // Remove only existing PointerClick entries we own.
                    for (int i = eventTrigger.triggers.Count - 1; i >= 0; i--)
                    {
                        if (eventTrigger.triggers[i].eventID == EventTriggerType.PointerClick)
                        {
                            eventTrigger.triggers.RemoveAt(i);
                        }
                    }

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
                    float resolutionFactorWidth = Mathf.Max((float)resolution.width / 1920f, 1);
                    float resolutionFactorHeight = Mathf.Max((float)resolution.height / 1080f, 1);

                    float baseScale = Mathf.Max(Mathf.Min(resolutionFactorWidth, resolutionFactorHeight), 1);

                    TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();
                    UIModuleNavigation uIModuleNavigation = controller.View.TacticalModules.NavigationModule;
                    widgetContainer = uIModuleNavigation.transform;

                    ancientsWidgetPrefab = new GameObject(widgetObjectName);
                    RectTransform rectTransform = ancientsWidgetPrefab.AddComponent<RectTransform>();

                    rectTransform.sizeDelta = new Vector2(410 * resolutionFactorWidth, 180 * resolutionFactorWidth);
                    rectTransform.position = new Vector2(245 * resolutionFactorWidth, 600 * resolutionFactorHeight);

                    GameObject backgroundImage = new GameObject("Background", typeof(RectTransform), typeof(Image));
                    backgroundImage.transform.SetParent(ancientsWidgetPrefab.transform, false);

                    RectTransform bgRect = backgroundImage.GetComponent<RectTransform>();
                    bgRect.sizeDelta = new Vector2(rectTransform.sizeDelta.x, rectTransform.sizeDelta.y / 3);
                    bgRect.anchoredPosition = Vector2.zero + new Vector2(0, 37.5f * resolutionFactorHeight);

                    Image bgImage = backgroundImage.GetComponent<Image>();
                    bgImage.color = new Color(0, 0, 0, 0.5f);
                    bgImage.raycastTarget = true;
                    _bgImage = bgImage;

                    // IMPORTANT: make icon/text children of Background so hovering them still keeps us "over background"
                    GameObject iconObj = new GameObject("Icon");
                    iconObj.transform.SetParent(backgroundImage.transform, false);
                    Image iconImage = iconObj.AddComponent<Image>();
                    iconImage.sprite = TFTVAncients.CyclopsDefenseStatus.Visuals.SmallIcon;
                    iconImage.color = color;
                    iconImage.preserveAspect = true;
                    iconImage.raycastTarget = true; // optional, but ensures it participates in UI raycasts
                    RectTransform iconImageRect = iconImage.GetComponent<RectTransform>();
                    iconImageRect.sizeDelta = new Vector2(30 * baseScale, 30 * baseScale);
                    iconImageRect.anchoredPosition = Vector2.zero + new Vector2(-180 * resolutionFactorWidth, 0.5f * resolutionFactorHeight);
                    _iconImage = iconImage;

                    GameObject generatorHealthTextObj = new GameObject("CyclopsResistanceText");
                    generatorHealthTextObj.transform.SetParent(backgroundImage.transform, false);
                    Text cyclopsResistanceText = generatorHealthTextObj.AddComponent<Text>();
                    cyclopsResistanceText.text = TFTVCommonMethods.ConvertKeyToString("TFTV_KEY_ANCIENTS_CYCLOPS_RESISTANCE")
                        .Replace("{0}", cyclopsDamageResistance.ToString());
                    cyclopsResistanceText.horizontalOverflow = HorizontalWrapMode.Overflow;
                    cyclopsResistanceText.alignment = TextAnchor.MiddleLeft;
                    cyclopsResistanceText.fontSize = (int)(35 * baseScale);
                    cyclopsResistanceText.color = Color.white;
                    cyclopsResistanceText.font = PuristaSemiboldFontCache ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                    cyclopsResistanceText.raycastTarget = true; // optional
                    RectTransform rectGeneratorsHealthText = cyclopsResistanceText.GetComponent<RectTransform>();
                    rectGeneratorsHealthText.sizeDelta = new Vector2(400 * resolutionFactorWidth, 60 * resolutionFactorHeight);
                    rectGeneratorsHealthText.localScale = new Vector2(0.5f, 0.5f);
                    rectGeneratorsHealthText.anchoredPosition = Vector2.zero + new Vector2(-50 * resolutionFactorWidth, 0);
                    _cyclopsResistance = cyclopsResistanceText;

                    AddClickChaseTarget(chaseTarget, iconObj.gameObject);

                    ancientsWidgetPrefab.transform.SetParent(widgetContainer);
                    ancientsWidgetPrefab.SetActive(true);

                    GameObject tooltipBgObj = new GameObject("TooltipBackground", typeof(RectTransform));
                    tooltipBgObj.transform.SetParent(ancientsWidgetPrefab.transform, false);
                    Image tooltipBgImage = tooltipBgObj.AddComponent<Image>();
                    tooltipBgImage.color = new Color(0, 0, 0, 0.75f);

                    RectTransform tooltipBgRect = tooltipBgObj.GetComponent<RectTransform>();
                    tooltipBgRect.sizeDelta = new Vector2(1500 * resolutionFactorWidth, 1500 * resolutionFactorHeight);
                    tooltipBgRect.anchoredPosition = new Vector2(600 * resolutionFactorWidth, 40 * resolutionFactorHeight);
                    tooltipBgRect.localScale = new Vector2(0.5f, 0.5f);

                    tooltipBgObj.SetActive(false);

                    GameObject tooltipObj = new GameObject("TooltipText");
                    tooltipObj.transform.SetParent(ancientsWidgetPrefab.transform, false);
                    tooltipText = tooltipObj.AddComponent<Text>();
                    tooltipText.text = TFTVCommonMethods.ConvertKeyToString("TFTV_KEY_ANCIENTS_RULESET");
                    tooltipText.alignment = TextAnchor.UpperLeft;
                    tooltipText.fontSize = (int)(40 * baseScale);
                    tooltipText.color = Color.white;
                    tooltipText.font = PuristaSemiboldFontCache ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                    tooltipText.verticalOverflow = VerticalWrapMode.Overflow;
                    tooltipText.horizontalOverflow = HorizontalWrapMode.Wrap;

                    RectTransform tooltipRect = tooltipText.GetComponent<RectTransform>();
                    tooltipRect.sizeDelta = new Vector2(1400 * resolutionFactorWidth, 100 * resolutionFactorHeight);
                    tooltipRect.anchoredPosition = new Vector2(600 * resolutionFactorWidth, 350 * resolutionFactorHeight);
                    tooltipRect.localScale = new Vector2(0.5f, 0.5f);

                    EventTrigger hoverTrigger = backgroundImage.GetComponent<EventTrigger>();
                    if (!hoverTrigger)
                    {
                        hoverTrigger = backgroundImage.AddComponent<EventTrigger>();
                    }

                    EventTrigger.Entry pointerEnter = new EventTrigger.Entry
                    {
                        eventID = EventTriggerType.PointerEnter
                    };
                    pointerEnter.callback.AddListener((eventData) =>
                    {
                        tooltipText.gameObject.SetActive(true);
                        tooltipBgObj.SetActive(true);
                    });
                    hoverTrigger.triggers.Add(pointerEnter);

                    EventTrigger.Entry pointerExit = new EventTrigger.Entry
                    {
                        eventID = EventTriggerType.PointerExit
                    };
                    pointerExit.callback.AddListener((eventData) =>
                    {
                        tooltipText.gameObject.SetActive(false);
                        tooltipBgObj.SetActive(false);
                    });
                    hoverTrigger.triggers.Add(pointerExit);

                    tooltipObj.SetActive(false);
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

                    _cyclopsResistance.text = TFTVCommonMethods.ConvertKeyToString("TFTV_KEY_ANCIENTS_CYCLOPS_RESISTANCE")
                        .Replace("{0}", cyclopsDamageResistance.ToString());
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
}
