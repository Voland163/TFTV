using Base.Core;
using Base.Defs;
using Base.Entities.Statuses;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.View.ViewControllers;
using PhoenixPoint.Tactical.View.ViewModules;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static TFTV.TFTVUI.Tactical.Data;
using static UITooltip;

namespace TFTV.TFTVUI.Tactical
{

    internal class DeliriumWidget
    {


        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;

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

                if (DeliriumWidget._ODISitrepList.Count > 0)
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
                    DeliriumWidget._ODISitrepList.Add((_tbtvOnDeathSprite, TFTVCommonMethods.ConvertKeyToString(_umbraTBTV), ODIDetailType.TBTVEffect));
                }

                DeliriumWidget._ODISitrepList.Add(((Sprite icon, string text, ODIDetailType))(null, _tbtvChances, ODIDetailType.TBTVChance));

                if (TFTVVoidOmens.VoidOmensCheck[16])
                {
                    DeliriumWidget._ODISitrepList.Add((_voidOmenSprite, TFTVCommonMethods.ConvertKeyToString(_tbtvChancesVOTBTVEverywhere), ODIDetailType.TBTVModifier));
                }
                else
                {
                    DeliriumWidget._ODISitrepList.Add((_voidOmenSprite, _tbtvChancesBase, ODIDetailType.TBTVModifier));
                }

                if (TFTVVoidOmens.VoidOmensCheck[15])
                {
                    DeliriumWidget._ODISitrepList.Add((_voidOmenSprite, TFTVCommonMethods.ConvertKeyToString(_tbtvChancesMoreTBTV), ODIDetailType.TBTVModifier));
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
}

