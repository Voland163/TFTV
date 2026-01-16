using Base.Core;
using Base.Defs;
using Epic.OnlineServices.AntiCheatCommon;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static TFTV.TFTVUI.Tactical.Data;
using static TFTV.TFTVUI.Tactical.TargetIcons;

namespace TFTV.TFTVUI.Tactical
{
    internal class OpposingHumanoidForceWidget
    {
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private const float ReferenceWidth = 1920f;
        private const float ReferenceHeight = 1080f;

        private static Dictionary<string, OpposingLeaderWidget> _leaderWidgets = new Dictionary<string, OpposingLeaderWidget>();

        private static float GetResolutionScale()
        {
            Resolution resolution = Screen.currentResolution;
            float scaleWidth = resolution.width / ReferenceWidth;
            float scaleHeight = resolution.height / ReferenceHeight;
            return Mathf.Max(1f, Mathf.Max(scaleWidth, scaleHeight));
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

                        float resFactor = GetResolutionScale();

                        tooltipRect.localScale = Vector3.one * resFactor;

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
                        RectTransform leaderNameRect = _leaderName.GetComponent<RectTransform>();
                        leaderNameRect.anchoredPosition = new Vector2(94, yPositionOffset - 52.5f);
                        leaderNameRect.localScale = new Vector3(0.5f, 0.5f, 0.5f);

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
                        RectTransform tacticNameRect = _tacticName.GetComponent<RectTransform>();
                        tacticNameRect.anchoredPosition = new Vector2(-7, yPositionOffset - 140);
                        tacticNameRect.localScale = new Vector3(0.5f, 0.5f, 0.5f);


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
                            tacticDescriptionText.color = RegularNoLOSColor;
                            tacticName.GetComponent<Text>().color = RegularNoLOSColor;
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

                    float resolutionScale = GetResolutionScale();

                    // Access UIModuleNavigation and set widgetContainer as its transform
                    TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();
                    UIModuleNavigation uIModuleNavigation = controller.View.TacticalModules.NavigationModule;

                    widgetContainer = uIModuleNavigation.transform;

                    

                    // Dynamically create the leaderWidgetPrefab structure
                    leaderWidgetPrefab = new GameObject("LeaderWidget");
                    RectTransform rectTransform = leaderWidgetPrefab.AddComponent<RectTransform>();
                    rectTransform.SetParent(widgetContainer);
                    rectTransform.sizeDelta = new Vector2(255, 180);
                    rectTransform.position = new Vector2
                        (170 * resolutionFactorWidth, 
                        600 * resolutionFactorHeight + (100 * (_leaderWidgets.Count - 1) * resolutionFactorHeight)); // Left margin of 20, 1/3 height down

                    rectTransform.localScale = Vector3.one * resolutionScale;

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
                    rectNameText.localScale = new Vector3(0.5f, 0.5f, 0.5f);
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
                    recttacticNameText.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    recttacticNameText.anchoredPosition = Vector2.zero + new Vector2(10, 0);

                    _titleOfTactic = tacticNameText;

                    /* UITooltipText uITooltipText = leaderWidgetPrefab.AddComponent<UITooltipText>();
                     uITooltipText.TipText = $"<b>{tacticName.ToUpper()}</b>\n{tacticDescription}";*/
                    
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
}
