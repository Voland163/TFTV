using Epic.OnlineServices;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using SoftMasking.Samples;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TFTV.TFTVHavenRecruitsUI;
using UnityEngine;
using UnityEngine.UI;
using static RootMotion.FinalIK.GrounderQuadruped;
using static TFTV.HavenRecruitsMain;
using static TFTV.HavenRecruitsMain.RecruitOverlayManager;
using static TFTV.TFTVHavenRecruitsUI.HavenRecruitsOverlayAnimator;
using HavenRecruitsUtils = TFTV.TFTVHavenRecruitsUI.HavenRecruitsUtils;

namespace TFTV
{
    internal static class HavenRecruitsDetailsPanel
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        internal const int DetailAbilityIconSize = 40;
        internal const float DetailClassIconSize = 36f;
        internal const float DetailFactionIconSize = 36f;
        internal const int DetailInventorySlotSize = 70;
        internal const float DetailStatIconSize = 25f;
        internal const float DetailStatsGridCellWidth = 210f;
        internal const float DetailStatsGridCellHeight = 44f;
        internal const int DetailStatsGridColumnCount = 2;
        internal const float DetailStatsGridSpacingX = 12f;
        internal const float DetailStatsGridSpacingY = 2f;
        internal const float DetailAbilityRowHeight = DetailAbilityIconSize;
        internal const float DetailArmorRowHeight = DetailClassIconSize;
        internal const float DetailEquipmentRowHeight = DetailClassIconSize;
        internal const float DetailLevelLabelPreferredWidth = DetailClassIconSize * 0.65f;
        internal const float DetailHeaderSpacing = 8f;
        internal const int DetailHeaderHorizontalPadding = 24;
        internal const int DetailHeaderVerticalPadding = 12;
        internal const float DetailFactionHeaderSpacing = 12f;
        internal const float DetailFactionTextSpacing = 2f;
        internal const float DetailClassInfoSpacing = 4f;
        internal const int DetailClassInfoPaddingTop = -20;
        internal const int DetailClassInfoPaddingBottom = 0;
        internal const float DetailClassTextSpacing = 4f;
        internal const float DetailCostSpacing = 0f;
        internal const int DetailCostPaddingTop = -20;
        internal const int DetailCostPaddingLeft = 0;
        internal const int DetailCostPaddingBottom = 0;
        internal const float DetailStatsSectionSpacing = 0f;
        internal const float DetailStatRowSpacing = 6f;
        internal const float DetailDeliriumIconSize = DetailStatIconSize;
        internal const float DetailDeliriumRowHeight = DetailStatsGridCellHeight;
        internal const float DetailAbilityRowHorizontalSpacing = 25f;
        internal const float DetailGearRowSpacing = 30f;
        internal const int DetailStatsSectionPaddingTop = -20;
        internal const int DetailStatsSectionPaddingBottom = 8;
        internal const int DetailAbilitySectionPaddingTop = 80;
        internal const int DetailAbilitySectionPaddingBottom = 0;
        internal const int DetailGearSectionPaddingTop = 140;
        internal const int DetailGearSectionPaddingBottom = 0;
        // Spacing between the major layout groups is intentionally tiny because the visible gaps
        // are dominated by the min/preferred heights we assign via LayoutElement (for example the
        // ability rows use AbilityIconSize and the stat grid uses its cell heights). Adjusting these
        // values will only add or remove a pixel or two of separation – the RectOffset padding
        // constants below should be used when a more noticeable gap between sections is required.
        private const float DetailContentSpacing = 1f;
        private const float DetailInfoSpacing = 1f;
        private const float DetailAbilityRowSpacing = 40f;
        private const float DetailAbilityReservedPadding = 1f;
        private const float DetailGearSpacing = 1f;
        private const float DetailAbilityLockedIconAlpha = 0.5f;

        private static LayoutElement _detailAbilityLayoutElement;
        private static readonly Color AbilityUnlockedIconColor = Color.white;
        private static readonly Color AbilityLockedIconColor = new Color(0.45f, 0.45f, 0.45f, DetailAbilityLockedIconAlpha);
        private static readonly Color AbilityUnlockedBackgroundColor = Color.white;
        private static readonly Color AbilityLockedBackgroundColor = new Color(1f, 1f, 1f, 0.35f);
        
        internal static Text _detailFactionNameLabel;
        internal static GameObject _detailPanel;
        internal static GameObject _detailEmptyState;
        internal static Transform _detailInfoRoot;
        internal static Image _detailClassIconImage;
        internal static Text _detailLevelNameLabel;
      
        internal static Image _detailFactionIconImage;

        internal static GameObject _detailAbilitySection;
        internal static Transform _detailClassAbilityRowRoot;
        internal static Transform _detailPersonalAbilityRowRoot;
        internal static GameObject _detailArmorSection;
        internal static Transform _detailArmorRoot;
        internal static GameObject _detailEquipmentSection;
        internal static Transform _detailEquipmentRoot;
        internal static Transform _detailStatsGridRoot;
        internal static readonly Dictionary<string, StatCell> _detailStatCells = new Dictionary<string, StatCell>();
        internal static GameObject _detailCostSection;
        internal static Transform _detailCostRoot;
        private static GameObject _detailDeliriumRow;
        private static Image _detailDeliriumIcon;
        private static Text _detailDeliriumLabel;
        private static Sprite _detailDeliriumIconSprite;
        private static Shadow _detailPanelGlowShadow;

        private const string StatPlaceholder = "--";
        private static readonly Dictionary<string, Sprite> _statIconCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private static readonly Color StatPositiveColor = new Color(0.3137f, 0.7843f, 0.3921f);
        private static readonly Color StatNegativeColor = new Color(0.8627f, 0.3529f, 0.3529f);
        private static readonly Color DetailStatHighlightColor = new Color32(0xD0, 0xA4, 0x56, 0xFF);
        private static readonly Color DetailDeliriumHighlightColor = new Color32(0xA2, 0x48, 0xD1, 0xFF);
        private const string StatTooltipPlaceholderText = "placeholder";
        private static readonly string BaseColorHex = ColorUtility.ToHtmlStringRGB(DetailSubTextColor);
        private static readonly Dictionary<string, string[]> StatPropertyNames = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Strength"] = new[] { "Strength", "Endurance" },
            ["Perception"] = new[] { "Perception" },
            ["Willpower"] = new[] { "Willpower" },
            ["Accuracy"] = new[] { "Accuracy" },
            ["Speed"] = new[] { "Speed" },
            ["Stealth"] = new[] { "Stealth" }
        };
        private static readonly string[] DefaultStatOrder =
       {
            "Strength",
            "Perception",
            "Willpower",
            "Accuracy",
            "Speed",
            "Stealth"
        };
        internal readonly struct StatCell
        {
            public StatCell(GameObject root, Image icon, Text label, UITooltipText tooltip)
            {
                Root = root;
                Icon = icon;
                Label = label;
                Tooltip = tooltip;
            }
            public GameObject Root { get; }
            public Image Icon { get; }

            public Text Label { get; }

            public UITooltipText Tooltip { get; }
        }

        private static void CreateDetailHeader(Transform parent)
        {
            try
            {
                if (parent == null)
                {
                    return;
                }

                var (headerGO, headerRT) = RecruitOverlayManagerHelpers.NewUI("DetailHeader", parent);

                headerRT.anchorMin = new Vector2(0f, 0.94f);
                headerRT.anchorMax = new Vector2(1f, 1f);
                headerRT.offsetMin = Vector2.zero;
                headerRT.offsetMax = Vector2.zero;

                var background = headerGO.AddComponent<Image>();
                background.color = HeaderBackgroundColor;

                var outline = headerGO.AddComponent<Outline>();
                outline.effectColor = HeaderBorderColor;
                outline.effectDistance = new Vector2(2f, 2f);

                var layout = headerGO.AddComponent<HorizontalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.spacing = DetailHeaderSpacing;
                layout.childControlWidth = false;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
                layout.padding = new RectOffset(
                   DetailHeaderHorizontalPadding,
                   DetailHeaderHorizontalPadding,
                   DetailHeaderVerticalPadding,
                   DetailHeaderVerticalPadding);

                var (titleGO, _) = RecruitOverlayManagerHelpers.NewUI("Title", headerGO.transform);
                var title = CreateDetailText(titleGO.transform, "Title", TextFontSize + 2, Color.white, TextAnchor.MiddleLeft);

                title.text = "RECRUIT DETAILS";
                title.horizontalOverflow = HorizontalWrapMode.Overflow;

            }
            catch (Exception ex) { TFTVLogger.Error(ex); }
        }

        internal static void CreateDetailPanel(Canvas canvas)
        {
            try
            {
                if (canvas == null)
                {
                    return;
                }

                if (_detailPanel != null)
                {
                    return;
                }

                _detailPanel = new GameObject("TFTV_RecruitDetailPanel");
                _detailPanel.transform.SetParent(canvas.transform, false);

                var detailCanvas = _detailPanel.AddComponent<Canvas>();
                detailCanvas.overrideSorting = true;
                detailCanvas.sortingOrder = 5000;
                _detailPanel.AddComponent<GraphicRaycaster>();

                var image = _detailPanel.AddComponent<Image>();
                image.color = new Color(0f, 0f, 0f, 0.95f);

                var outline = _detailPanel.AddComponent<Outline>();
                outline.effectColor = HeaderBorderColor;
                outline.effectDistance = new Vector2(2f, 2f);

                _detailPanelGlowShadow = _detailPanel.AddComponent<Shadow>();
                _detailPanelGlowShadow.effectDistance = new Vector2(0f, -24f);
                _detailPanelGlowShadow.effectColor = new Color(DetailSubTextColor.r, DetailSubTextColor.g, DetailSubTextColor.b, 0.35f);
                _detailPanelGlowShadow.useGraphicAlpha = false;

                var rt = _detailPanel.GetComponent<RectTransform>();
                float width = GetOverlayWidthFraction(out float detailPixels, DetailPanelWidthPercent, DetailPanelMinWidthPx);
                rt.anchorMin = new Vector2(OverlayLeftMargin, OverlayBottomMargin);
                rt.anchorMax = new Vector2(OverlayLeftMargin + width, 1f - OverlayTopMargin);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                CreateDetailHeader(_detailPanel.transform);

                var (contentGO, contentRT) = RecruitOverlayManagerHelpers.NewUI("Content", _detailPanel.transform);
                contentRT.anchorMin = new Vector2(0f, 0f);
                contentRT.anchorMax = new Vector2(1f, 0.94f);
                contentRT.offsetMin = new Vector2(24f, 36f);
                contentRT.offsetMax = new Vector2(-24f, -24f);

                var layout = contentGO.AddComponent<VerticalLayoutGroup>();
                layout.childAlignment = TextAnchor.UpperLeft;
                layout.spacing = DetailContentSpacing;
                layout.childControlWidth = true;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;

                _detailEmptyState = CreateDetailPlaceholder(contentGO.transform);

                var (infoRootGO, _) = RecruitOverlayManagerHelpers.NewUI("InfoRoot", contentGO.transform);
                var infoLayout = infoRootGO.AddComponent<VerticalLayoutGroup>();
                infoLayout.childAlignment = TextAnchor.UpperLeft;
                infoLayout.spacing = DetailInfoSpacing;
                infoLayout.childControlWidth = true;
                infoLayout.childControlHeight = false;
                infoLayout.childForceExpandWidth = true;
                infoLayout.childForceExpandHeight = false;

                _detailInfoRoot = infoRootGO.transform;
                _detailInfoRoot.gameObject.SetActive(false);

                var (factionHeaderGO, _) = RecruitOverlayManagerHelpers.NewUI("FactionHeader", infoRootGO.transform);
                var factionHeaderLayout = factionHeaderGO.AddComponent<HorizontalLayoutGroup>();
                factionHeaderLayout.childAlignment = TextAnchor.MiddleLeft;
                factionHeaderLayout.spacing = DetailFactionHeaderSpacing;
                factionHeaderLayout.childControlWidth = false;
                factionHeaderLayout.childControlHeight = false;
                factionHeaderLayout.childForceExpandWidth = false;
                factionHeaderLayout.childForceExpandHeight = false;
                factionHeaderLayout.padding = new RectOffset(
                    0,
                    0,
                    DetailClassInfoPaddingTop,
                    DetailClassInfoPaddingBottom);

                var (factionIconGO, factionIconRT) = RecruitOverlayManagerHelpers.NewUI("FactionIcon", factionHeaderGO.transform);
                _detailFactionIconImage = factionIconGO.AddComponent<Image>();
                _detailFactionIconImage.preserveAspect = true;
                _detailFactionIconImage.raycastTarget = false;
                _detailFactionIconImage.enabled = false;
                var factionIconLE = factionIconGO.AddComponent<LayoutElement>();
                factionIconLE.preferredWidth = DetailFactionIconSize;
                factionIconLE.preferredHeight = DetailFactionIconSize;
                factionIconLE.minWidth = DetailFactionIconSize;
                factionIconLE.minHeight = DetailFactionIconSize;
                factionIconRT.sizeDelta = new Vector2(DetailFactionIconSize, DetailFactionIconSize);

                var (factionTextContainerGO, _) = RecruitOverlayManagerHelpers.NewUI("FactionTextContainer", factionHeaderGO.transform);
                var factionTextLayout = factionTextContainerGO.AddComponent<VerticalLayoutGroup>();
                factionTextLayout.childAlignment = TextAnchor.MiddleLeft;
                factionTextLayout.spacing = DetailFactionTextSpacing;
                factionTextLayout.childControlWidth = true;
                factionTextLayout.childControlHeight = false;
                factionTextLayout.childForceExpandWidth = false;
                factionTextLayout.childForceExpandHeight = false;

                var factionTextLE = factionTextContainerGO.AddComponent<LayoutElement>();
                factionTextLE.minWidth = 0f;
                factionTextLE.flexibleWidth = 1f;

                _detailFactionNameLabel = CreateDetailText(factionTextContainerGO.transform, "FactionName", TextFontSize + 2, DetailSubTextColor, TextAnchor.MiddleLeft);
                _detailFactionNameLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
                var factionNameLE = _detailFactionNameLabel.gameObject.AddComponent<LayoutElement>();
                factionNameLE.minWidth = 0f;
                factionNameLE.flexibleWidth = 1f;

                var (classInfoGO, classInfoRT) = RecruitOverlayManagerHelpers.NewUI("ClassInfo", infoRootGO.transform);
                var classInfoLayout = classInfoGO.AddComponent<HorizontalLayoutGroup>();
                classInfoLayout.childAlignment = TextAnchor.MiddleLeft;
                classInfoLayout.spacing = DetailClassInfoSpacing;
                classInfoLayout.childControlWidth = false;
                classInfoLayout.childControlHeight = true;
                classInfoLayout.childForceExpandWidth = false;
                classInfoLayout.childForceExpandHeight = false;
                classInfoLayout.padding = new RectOffset(
                    0,
                    0,
                    DetailClassInfoPaddingTop,
                    DetailClassInfoPaddingBottom);

                if (classInfoRT != null)
                {
                    var offsetMin = classInfoRT.offsetMin;
                    var offsetMax = classInfoRT.offsetMax;
                    var anchoredPosition = classInfoRT.anchoredPosition;

                    classInfoRT.anchorMin = new Vector2(0f, classInfoRT.anchorMin.y);
                    classInfoRT.anchorMax = new Vector2(1f, classInfoRT.anchorMax.y);
                    classInfoRT.pivot = new Vector2(0f, classInfoRT.pivot.y);
                    classInfoRT.offsetMin = new Vector2(0f, offsetMin.y);
                    classInfoRT.offsetMax = new Vector2(0f, offsetMax.y);
                    classInfoRT.anchoredPosition = new Vector2(0f, anchoredPosition.y);
                }

                var classInfoFitter = classInfoGO.AddComponent<ContentSizeFitter>();
                classInfoFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                classInfoFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                var (classIconGO, classIconRT) = RecruitOverlayManagerHelpers.NewUI("ClassIcon", classInfoGO.transform);
                _detailClassIconImage = classIconGO.AddComponent<Image>();
                _detailClassIconImage.preserveAspect = true;
                _detailClassIconImage.raycastTarget = false;
                _detailClassIconImage.enabled = false;
                var classIconLE = classIconGO.AddComponent<LayoutElement>();
                classIconLE.preferredWidth = DetailClassIconSize;
                classIconLE.preferredHeight = DetailClassIconSize;
                classIconLE.minWidth = DetailClassIconSize;
                classIconLE.minHeight = DetailClassIconSize;
                classIconRT.sizeDelta = new Vector2(DetailClassIconSize, DetailClassIconSize);

                _detailLevelNameLabel = CreateDetailText(classInfoGO.transform, "Level", TextFontSize + 12, Color.white, TextAnchor.MiddleLeft);
                _detailLevelNameLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
                _detailLevelNameLabel.text = StatPlaceholder;
                var levelLE = _detailLevelNameLabel.gameObject.AddComponent<LayoutElement>();
                levelLE.minWidth = 0f;
                levelLE.preferredWidth = -1;
                levelLE.flexibleWidth = 0f;


                var (costGO, _) = RecruitOverlayManagerHelpers.NewUI("CostRow", infoRootGO.transform);
                var costLayout = costGO.AddComponent<HorizontalLayoutGroup>();
                costLayout.childAlignment = TextAnchor.MiddleLeft;
                costLayout.spacing = DetailCostSpacing;
                costLayout.childControlWidth = false;
                costLayout.childControlHeight = false;
                costLayout.childForceExpandWidth = false;
                costLayout.childForceExpandHeight = false;
                costLayout.padding = new RectOffset(
                   DetailCostPaddingLeft,
                   0,
                   DetailCostPaddingTop,
                   DetailCostPaddingBottom);
                _detailCostSection = costGO;
                _detailCostRoot = costGO.transform;
                _detailCostSection.SetActive(false);
                var costLE = costGO.AddComponent<LayoutElement>();
                costLE.minWidth = 0f;
                costLE.flexibleWidth = 1f;

                var (statsSectionGO, _) = RecruitOverlayManagerHelpers.NewUI("StatsSection", infoRootGO.transform);
                var statsSectionLayout = statsSectionGO.AddComponent<VerticalLayoutGroup>();
                statsSectionLayout.childAlignment = TextAnchor.UpperCenter;
                statsSectionLayout.spacing = DetailStatRowSpacing;
                statsSectionLayout.childControlWidth = true;
                statsSectionLayout.childControlHeight = true;
                statsSectionLayout.childForceExpandWidth = true;
                statsSectionLayout.childForceExpandHeight = false;
                statsSectionLayout.padding = new RectOffset(
                    0,
                    0,
                    DetailStatsSectionPaddingTop,
                    DetailStatsSectionPaddingBottom);
                var (statsGridGO, statsGridRT) = RecruitOverlayManagerHelpers.NewUI("StatsGrid", statsSectionGO.transform);
                var statsGrid = statsGridGO.AddComponent<GridLayoutGroup>();
                statsGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                statsGrid.constraintCount = DetailStatsGridColumnCount;
                statsGrid.cellSize = new Vector2(DetailStatsGridCellWidth, DetailStatsGridCellHeight);
                statsGrid.spacing = new Vector2(DetailStatsGridSpacingX, DetailStatsGridSpacingY);
                statsGrid.childAlignment = TextAnchor.UpperLeft;
                statsGridRT.anchorMin = new Vector2(0.5f, 0.5f);
                statsGridRT.anchorMax = new Vector2(0.5f, 0.5f);
                statsGridRT.pivot = new Vector2(0.5f, 0.5f);
                var statsGridLE = statsGridGO.AddComponent<LayoutElement>();
                int statRows = Mathf.CeilToInt(DefaultStatOrder.Length / (float)DetailStatsGridColumnCount);
                float statsHeight = DetailStatsGridCellHeight * statRows + DetailStatsGridSpacingY * Mathf.Max(0, statRows - 1);
                float statsWidth = DetailStatsGridCellWidth * DetailStatsGridColumnCount + DetailStatsGridSpacingX * Mathf.Max(0, DetailStatsGridColumnCount - 1);
                statsGridLE.minHeight = statsHeight;
                statsGridLE.preferredHeight = statsHeight;
                statsGridLE.preferredWidth = statsWidth;
                statsGridLE.flexibleWidth = 0f;

                _detailStatCells.Clear();
                _detailStatsGridRoot = statsGridGO.transform;
                foreach (var statName in DefaultStatOrder)
                {
                    var cell = CreateStatCell(statName, _detailStatsGridRoot);
                    _detailStatCells[statName] = cell;
                }

                var (deliriumRowGO, _) = RecruitOverlayManagerHelpers.NewUI("DeliriumRow", statsSectionGO.transform);
                var deliriumLayout = deliriumRowGO.AddComponent<HorizontalLayoutGroup>();
                deliriumLayout.childAlignment = TextAnchor.MiddleCenter;
                deliriumLayout.spacing = DetailClassTextSpacing;
                deliriumLayout.childControlWidth = false;
                deliriumLayout.childControlHeight = false;
                deliriumLayout.childForceExpandWidth = false;
                deliriumLayout.childForceExpandHeight = false;
                var deliriumLE = deliriumRowGO.AddComponent<LayoutElement>();
                deliriumLE.minHeight = DetailDeliriumRowHeight;
                deliriumLE.preferredHeight = DetailDeliriumRowHeight;
                var (deliriumIconGO, _) = RecruitOverlayManagerHelpers.NewUI("Icon", deliriumRowGO.transform);

                _detailDeliriumIcon = deliriumIconGO.AddComponent<Image>();
                _detailDeliriumIcon.preserveAspect = true;
                _detailDeliriumIcon.raycastTarget = false;
                _detailDeliriumIcon.color = DetailDeliriumHighlightColor;
                // Ensure the RectTransform has the desired size instead of relying on SetNativeSize()
                var deliriumIconRT = deliriumIconGO.GetComponent<RectTransform>();
                if (deliriumIconRT != null)
                {
                    deliriumIconRT.sizeDelta = new Vector2(DetailDeliriumIconSize, DetailDeliriumIconSize);
                }

                // Also add a LayoutElement so layout systems have the correct preferred/min sizes
                var deliriumIconLE = deliriumIconGO.AddComponent<LayoutElement>();
                deliriumIconLE.preferredWidth = DetailDeliriumIconSize;
                deliriumIconLE.preferredHeight = DetailDeliriumIconSize;
                deliriumIconLE.minWidth = DetailDeliriumIconSize;
                deliriumIconLE.minHeight = DetailDeliriumIconSize;

                var (deliriumLabelGO, _) = RecruitOverlayManagerHelpers.NewUI("Label", deliriumRowGO.transform);
                _detailDeliriumLabel = CreateDetailText(deliriumLabelGO.transform, "Delirium", TextFontSize + 2, DetailDeliriumHighlightColor, TextAnchor.MiddleLeft);
                _detailDeliriumLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
                var deliriumLabelLE = _detailDeliriumLabel.gameObject.AddComponent<LayoutElement>();
                deliriumLabelLE.minWidth = 0f;
                deliriumLabelLE.flexibleWidth = 0f;
                _detailDeliriumRow = deliriumRowGO;
                _detailDeliriumRow.SetActive(false);


                var (abilitiesGO, _) = RecruitOverlayManagerHelpers.NewUI("AbilitySection", infoRootGO.transform);
                var abilityLayout = abilitiesGO.AddComponent<VerticalLayoutGroup>();
                abilityLayout.childAlignment = TextAnchor.UpperLeft;
                abilityLayout.spacing = DetailAbilityRowSpacing;
                abilityLayout.childControlWidth = true;
                abilityLayout.childControlHeight = true;
                abilityLayout.childForceExpandWidth = false;
                abilityLayout.childForceExpandHeight = false;
                abilityLayout.padding = new RectOffset(
                    0,
                    0,
                    DetailAbilitySectionPaddingTop,
                    DetailAbilitySectionPaddingBottom);
                var abilityLE = abilitiesGO.AddComponent<LayoutElement>();
                abilityLE.flexibleWidth = 1f;
                abilityLE.flexibleHeight = 0f;
                abilityLE.minHeight = DetailAbilityRowHeight * 1.5f;
                abilityLE.preferredHeight = DetailAbilityRowHeight*1.5f;
                _detailAbilityLayoutElement = abilityLE;
                _detailAbilitySection = abilitiesGO;
                _detailAbilitySection.SetActive(false);

                var (classAbilityRowGO, _) = RecruitOverlayManagerHelpers.NewUI("ClassAbilityRow", abilitiesGO.transform);
                var classAbilityLayout = classAbilityRowGO.AddComponent<HorizontalLayoutGroup>();
                classAbilityLayout.childAlignment = TextAnchor.MiddleLeft;
                classAbilityLayout.spacing = DetailAbilityRowHorizontalSpacing;
                classAbilityLayout.childControlWidth = false;
                classAbilityLayout.childControlHeight = false;
                classAbilityLayout.childForceExpandWidth = false;
                classAbilityLayout.childForceExpandHeight = false;
                var classAbilityLE = classAbilityRowGO.AddComponent<LayoutElement>();
                classAbilityLE.minHeight = DetailAbilityRowHeight;
                classAbilityLE.preferredHeight = DetailAbilityRowHeight;
                _detailClassAbilityRowRoot = classAbilityRowGO.transform;
                _detailClassAbilityRowRoot.gameObject.SetActive(false);

                var (personalAbilityRowGO, _) = RecruitOverlayManagerHelpers.NewUI("PersonalAbilityRow", abilitiesGO.transform);
                var personalAbilityLayout = personalAbilityRowGO.AddComponent<HorizontalLayoutGroup>();
                personalAbilityLayout.childAlignment = TextAnchor.MiddleLeft;
                personalAbilityLayout.spacing = DetailAbilityRowHorizontalSpacing;
                personalAbilityLayout.childControlWidth = false;
                personalAbilityLayout.childControlHeight = false;
                personalAbilityLayout.childForceExpandWidth = false;
                personalAbilityLayout.childForceExpandHeight = false;
                var personalAbilityLE = personalAbilityRowGO.AddComponent<LayoutElement>();
                personalAbilityLE.minHeight = DetailAbilityRowHeight;
                personalAbilityLE.preferredHeight = DetailAbilityRowHeight;
                _detailPersonalAbilityRowRoot = personalAbilityRowGO.transform;
                _detailPersonalAbilityRowRoot.gameObject.SetActive(false);

                var (gearSectionsGO, _) = RecruitOverlayManagerHelpers.NewUI("GearSections", infoRootGO.transform);
                var gearLayout = gearSectionsGO.AddComponent<VerticalLayoutGroup>();
                gearLayout.childAlignment = TextAnchor.UpperLeft;
                gearLayout.spacing = DetailGearSpacing;
                gearLayout.childControlWidth = false;
                gearLayout.childControlHeight = false;
                gearLayout.childForceExpandWidth = false;
                gearLayout.childForceExpandHeight = false;
                gearLayout.padding = new RectOffset(
                    0,
                    0,
                    DetailGearSectionPaddingTop,
                    DetailGearSectionPaddingBottom);
                var gearLE = gearSectionsGO.AddComponent<LayoutElement>();
                gearLE.minWidth = 0f;
                gearLE.flexibleWidth = 1f;

                var (armorSectionGO, _) = RecruitOverlayManagerHelpers.NewUI("ArmorSection", gearSectionsGO.transform);
                var armorLayout = armorSectionGO.AddComponent<HorizontalLayoutGroup>();
                armorLayout.childAlignment = TextAnchor.UpperLeft;
                armorLayout.spacing = DetailGearRowSpacing;
                armorLayout.childControlWidth = false;
                armorLayout.childControlHeight = false;
                armorLayout.childForceExpandWidth = false;
                armorLayout.childForceExpandHeight = false;
                var armorLE = armorSectionGO.AddComponent<LayoutElement>();
                armorLE.minHeight = DetailArmorRowHeight;
                armorLE.preferredHeight = DetailArmorRowHeight;
                armorLE.flexibleWidth = 1f;
                _detailArmorSection = armorSectionGO;
                _detailArmorRoot = armorSectionGO.transform;
                _detailArmorSection.SetActive(false);

                var (equipmentGO, _) = RecruitOverlayManagerHelpers.NewUI("EquipmentSection", gearSectionsGO.transform);
                var equipmentLayout = equipmentGO.AddComponent<HorizontalLayoutGroup>();
                equipmentLayout.childAlignment = TextAnchor.UpperLeft;
                equipmentLayout.spacing = DetailGearRowSpacing;
                equipmentLayout.childControlWidth = false;
                equipmentLayout.childControlHeight = false;
                equipmentLayout.childForceExpandWidth = false;
                equipmentLayout.childForceExpandHeight = false;
                var equipmentLE = equipmentGO.AddComponent<LayoutElement>();
                equipmentLE.minHeight = DetailEquipmentRowHeight;
                equipmentLE.preferredHeight = DetailEquipmentRowHeight;
                equipmentLE.flexibleWidth = 1f;
                _detailEquipmentSection = equipmentGO;
                _detailEquipmentRoot = equipmentGO.transform;
                _detailEquipmentSection.SetActive(false);

                _detailAnimator = _detailPanel.AddComponent<OverlayAnimator>();
                _detailAnimator.Initialize(rt, slideFromLeft: true, resolvedWidth: detailPixels);
                _isDetailVisible = false;

                _detailPanel.SetActive(false);
                EnsureOverlayLayout(force: true);
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
            }
        }

        private static StatCell CreateStatCell(string name, Transform parent)
        {
            var sanitized = SanitizeName(name);
            var (cellGO, cellRT) = RecruitOverlayManagerHelpers.NewUI($"{sanitized}StatCell", parent);
            cellRT.anchorMin = new Vector2(0.5f, 0.5f);
            cellRT.anchorMax = new Vector2(0.5f, 0.5f);
            cellRT.pivot = new Vector2(0.5f, 0.5f);

            var layout = cellGO.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = DetailStatRowSpacing;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var (iconGO, iconRT) = RecruitOverlayManagerHelpers.NewUI($"{sanitized}Icon", cellGO.transform);
            var icon = iconGO.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.enabled = false;
            icon.color = DetailStatHighlightColor;
            var iconLE = iconGO.AddComponent<LayoutElement>();
            iconLE.preferredWidth = DetailStatIconSize;
            iconLE.preferredHeight = DetailStatIconSize;
            iconLE.minWidth = DetailStatIconSize;
            iconLE.minHeight = DetailStatIconSize;
            iconRT.sizeDelta = new Vector2(DetailStatIconSize, DetailStatIconSize);

            var statText = CreateDetailText(cellGO.transform, $"{sanitized}Text", TextFontSize + 1, DetailStatHighlightColor, TextAnchor.MiddleLeft);
            statText.horizontalOverflow = HorizontalWrapMode.Overflow;
            statText.text = $"{name} {StatPlaceholder}";
            var textLE = statText.gameObject.AddComponent<LayoutElement>();
            textLE.minWidth = 0f;
            textLE.flexibleWidth = 1f;

            var tooltip = cellGO.AddComponent<UITooltipText>();
            tooltip.TipText = StatTooltipPlaceholderText;

            return new StatCell(cellGO, icon, statText, tooltip);
        }

        internal static Text CreateDetailText(Transform parent, string name, int fontSize, Color color, TextAnchor alignment)
        {
            var (go, _) = RecruitOverlayManagerHelpers.NewUI(name, parent);
            go.AddComponent<CanvasRenderer>();
            var text = go.AddComponent<Text>();
            text.font = PuristaSemibold ? PuristaSemibold : Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = true;
            return text;
        }

        internal static void HideRecruitDetails(bool immediate = false)
        {
            try
            {
                if (_detailPanel == null)
                {
                    _isDetailVisible = false;
                    return;
                }

                if (_detailInfoRoot != null)
                {
                    _detailInfoRoot.gameObject.SetActive(false);
                }

                if (_detailEmptyState != null)
                {
                    _detailEmptyState.SetActive(true);
                }

                if (_detailClassIconImage != null)
                {
                    _detailClassIconImage.sprite = null;
                    _detailClassIconImage.enabled = false;
                }

                if (_detailLevelNameLabel != null)
                {
                    _detailLevelNameLabel.text = StatPlaceholder;
                }

               

                if (_detailFactionNameLabel != null)
                {
                    _detailFactionNameLabel.text = string.Empty;
                }


                PopulateStats(null);
                PopulateArmorSlots(null);
                PopulateEquipmentSlots(null);
                PopulateAbilityRows(null, null, null, null);

                if (_detailArmorSection != null)
                {
                    _detailArmorSection.SetActive(false);
                }

                if (_detailEquipmentSection != null)
                {
                    _detailEquipmentSection.SetActive(false);
                }

                if (_detailAbilitySection != null)
                {
                    _detailAbilitySection.SetActive(false);
                }

                ResetDetailFactionVisuals();

                if (_detailAnimator != null)
                {
                    if (!_detailPanel.activeSelf && !_isDetailVisible)
                    {
                        _isDetailVisible = false;
                        return;
                    }

                    if (immediate)
                    {
                        _detailAnimator.HideImmediate();
                        _detailPanel.SetActive(false);
                    }
                    else
                    {
                        _detailAnimator.Play(false, () =>
                        {
                            if (_detailPanel != null)
                            {
                                _detailPanel.SetActive(false);
                            }
                        });
                    }
                }
                else
                {
                    _detailPanel.SetActive(false);
                }

                _isDetailVisible = false;
            }
            catch (Exception ex) { TFTVLogger.Error(ex); }
        }

        internal static void ResetDetailFactionVisuals()
        {

            if (_detailFactionIconImage != null)
            {
                _detailFactionIconImage.sprite = null;
                _detailFactionIconImage.enabled = false;
                _detailFactionIconImage.color = Color.white;
            }
            if (_detailFactionNameLabel != null)
            {
                _detailFactionNameLabel.text = string.Empty;
            }
            if (_detailPanelGlowShadow != null)
            {
                var glowColor = DetailSubTextColor;
                glowColor.a = 0.35f;
                _detailPanelGlowShadow.effectColor = glowColor;
            }
        }

        private static void PopulateStats(RecruitAtSite recruit)
        {
            foreach (var kvp in _detailStatCells)
            {
                var statName = kvp.Key;
                var cell = kvp.Value;

                if (cell.Icon != null)
                {
                    cell.Icon.sprite = null;
                    cell.Icon.enabled = false;
                    cell.Icon.color = DetailStatHighlightColor;
                }

                if (cell.Label != null)
                {
                    cell.Label.text = $"{statName} {StatPlaceholder}";
                    cell.Label.color = DetailStatHighlightColor;
                }

                if (cell.Tooltip != null)
                {
                    cell.Tooltip.TipText = StatTooltipPlaceholderText;
                }
            }

            if (recruit == null)
            {
                PopulateDeliriumRow(0);
                RefreshStatsLayout();
                return;
            }
            try
            {
                var stats = recruit.Recruit.GetStats();

                TFTVLogger.Always($"strength: {stats.Endurance.Value}");
                TFTVLogger.Always($"willpower: {stats.Willpower.Value}");
                TFTVLogger.Always($"perception: {stats.Perception.Value}");
                TFTVLogger.Always($"speed: {stats.Speed.Value}");
                TFTVLogger.Always($"accuracy: {stats.Accuracy.Value}");
                TFTVLogger.Always($"stealth: {stats.Stealth.Value}");

                int delirium = recruit.Haven.RecruitCorruption;

                if (stats == null)
                {
                    RefreshStatsLayout();
                    return;
                }

                for (int x = 0; x < 6; x++)
                {

                    string statName = DefaultStatOrder[x];
                    float statValue = 0f;

                    if (!_detailStatCells.TryGetValue(statName, out var cell))
                    {
                        continue;
                    }

                    switch (statName)
                    {
                        case "Strength":
                            statValue = stats.Endurance;
                            break;
                        case "Perception":
                            statValue = stats.GetPerception();
                            break;
                        case "Willpower":
                            statValue = stats.WillPoints;
                            break;
                        case "Accuracy":
                            statValue = stats.GetAccuracy();
                            break;
                        case "Speed":
                            statValue = stats.Speed;
                            break;
                        case "Stealth":
                            statValue = stats.Stealth;
                            break;

                    }

                    string valueText = statValue.ToString();

                    if (statName == "Accuracy" || statName == "Stealth")
                    {
                        valueText = Mathf.RoundToInt(statValue * 100f) + "%";
                    }


                    Sprite icon = GetStatIcon(statName);

                    if (cell.Icon != null)
                    {
                        cell.Icon.sprite = icon;
                        cell.Icon.enabled = icon != null;
                        cell.Icon.color = DetailStatHighlightColor;
                    }

                    if (cell.Label != null)
                    {
                        cell.Label.text = $"{statName} {valueText}";
                        cell.Label.color = DetailStatHighlightColor;
                    }
                }
                PopulateDeliriumRow(delirium);
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
            }

            RefreshStatsLayout();
        }
        private static void PopulateDeliriumRow(int deliriumValue)
        {
            TFTVLogger.Always($"_detailDeliriumRow == null: {_detailDeliriumRow == null}");

            if (_detailDeliriumRow == null)
            {
                return;
            }

            if (deliriumValue==0)
            {
                _detailDeliriumRow.SetActive(false);
                return;
            }
           
            if (_detailDeliriumLabel != null)
            {
                _detailDeliriumLabel.text = $"Delirium {deliriumValue}";
                _detailDeliriumLabel.color = DetailDeliriumHighlightColor;
            }

            if (_detailDeliriumIcon != null)
            {
                var sprite = DefCache.GetDef<ViewElementDef>("E_Visuals [Corruption_StatusDef]").SmallIcon;
                _detailDeliriumIcon.sprite = sprite;
                _detailDeliriumIcon.color = DetailDeliriumHighlightColor;
                _detailDeliriumIcon.enabled = sprite != null;
            }

            _detailDeliriumRow.SetActive(true);
        }

        private static Sprite GetDeliriumIcon()
        {
            if (_detailDeliriumIconSprite == null)
            {
                _detailDeliriumIconSprite = Helper.CreateSpriteFromImageFile("Stat_Delirium.png");
            }

            return _detailDeliriumIconSprite;
        }

       

        private static void RefreshStatsLayout()
        {
            if (_detailStatsGridRoot is RectTransform statsRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(statsRect);
            }
        }

        private static object GetStatObject(object stats, string statName)
        {
            if (stats == null || string.IsNullOrEmpty(statName))
            {
                return null;
            }

            if (StatPropertyNames.TryGetValue(statName, out var candidates))
            {
                foreach (var candidate in candidates)
                {
                    var value = GetMemberValue(stats, candidate);
                    if (value != null)
                    {
                        return value;
                    }
                }
            }

            var direct = GetMemberValue(stats, statName);
            if (direct != null)
            {
                return direct;
            }

            object methodResult = InvokeStatAccessor(stats, "GetStat", statName);
            if (methodResult != null)
            {
                return methodResult;
            }

            methodResult = InvokeStatAccessor(stats, "GetAttribute", statName);
            if (methodResult != null)
            {
                return methodResult;
            }

            return null;
        }

        private static object InvokeStatAccessor(object stats, string methodName, string statName)
        {
            if (stats == null)
            {
                return null;
            }

            var methods = stats.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => string.Equals(m.Name, methodName, StringComparison.Ordinal) && m.GetParameters().Length == 1);

            foreach (var method in methods)
            {
                try
                {
                    var parameter = method.GetParameters()[0];
                    object argument = null;

                    if (parameter.ParameterType == typeof(string))
                    {
                        argument = statName;
                    }
                    else if (parameter.ParameterType.IsEnum)
                    {
                        if (TryParseEnum(parameter.ParameterType, statName, out var enumValue))
                        {
                            argument = enumValue;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        continue;
                    }

                    var result = method.Invoke(stats, new[] { argument });
                    if (result != null)
                    {
                        return result;
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        private static bool TryParseEnum(Type enumType, string value, out object parsed)
        {
            parsed = null;
            if (enumType == null || string.IsNullOrEmpty(value) || !enumType.IsEnum)
            {
                return false;
            }

            try
            {
                parsed = Enum.Parse(enumType, value, true);
                return true;
            }
            catch
            {
            }

            if (StatPropertyNames.TryGetValue(value, out var synonyms))
            {
                foreach (var synonym in synonyms)
                {
                    try
                    {
                        parsed = Enum.Parse(enumType, synonym, true);
                        return true;
                    }
                    catch
                    {
                    }
                }
            }

            return false;
        }

        private static object GetMemberValue(object target, string memberName)
        {
            if (target == null || string.IsNullOrEmpty(memberName))
            {
                return null;
            }

            var type = target.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            try
            {
                var prop = type.GetProperty(memberName, flags);
                if (prop != null)
                {
                    return prop.GetValue(target);
                }
            }
            catch
            {
            }

            try
            {
                var field = type.GetField(memberName, flags);
                if (field != null)
                {
                    return field.GetValue(target);
                }
            }
            catch
            {
            }

            return null;
        }

        private static (float? BaseValue, float? ModifiedValue) GetStatValues(object statObject)
        {
            if (statObject == null)
            {
                return (null, null);
            }

            object valueContainer = GetMemberValue(statObject, "Value") ?? statObject;

            float? baseValue = TryExtractValue(valueContainer, statObject, new[]
            {
                "BaseValue", "BaseValueInt", "BaseValueFloat", "Base", "BaseAmount"
            });

            float? modifiedValue = TryExtractValue(valueContainer, statObject, new[]
            {
                "ModifiedValue", "ModifiedValueInt", "ModifiedValueFloat", "MutatedValue", "MutatedValueInt", "MutatedValueFloat",
                "Value", "ValueInt", "ValueFloat", "CurrentValue", "EffectiveValue", "FinalValue", "ResultValue", "TotalValue"
            });

            if (!baseValue.HasValue)
            {
                baseValue = modifiedValue;
            }

            if (!modifiedValue.HasValue)
            {
                modifiedValue = baseValue;
            }

            return (baseValue, modifiedValue);
        }

        private static float? TryExtractValue(object primary, object fallback, IEnumerable<string> memberNames)
        {
            foreach (var member in memberNames)
            {
                var value = TryConvertToFloat(GetMemberValue(primary, member));
                if (!value.HasValue)
                {
                    value = TryConvertToFloat(GetMemberValue(fallback, member));
                }

                if (value.HasValue)
                {
                    return value;
                }
            }

            return null;
        }

        private static float? TryConvertToFloat(object value)
        {
            if (value == null)
            {
                return null;
            }

            switch (value)
            {
                case float f:
                    return f;
                case double d:
                    return (float)d;
                case int i:
                    return i;
                case long l:
                    return l;
                case short s:
                    return s;
                case byte b:
                    return b;
                case decimal m:
                    return (float)m;
                default:
                    if (float.TryParse(value.ToString(), out float parsed))
                    {
                        return parsed;
                    }
                    break;
            }

            return null;
        }

        private static Sprite GetStatIcon(string statName)
        {
            if (string.IsNullOrEmpty(statName))
            {
                return null;
            }

            if (_statIconCache.TryGetValue(statName, out var cached) && cached != null)
            {
                return cached;
            }

            Sprite icon = null;

            string nameFile = $"Stat_{statName}.png";

            icon = Helper.CreateSpriteFromImageFile(nameFile);

            if (icon != null)
            {
                _statIconCache[statName] = icon;
            }

            return icon;
        }

        private static string FormatStatValue(float? baseValue, float? modifiedValue)
        {
            if (!baseValue.HasValue && !modifiedValue.HasValue)
            {
                return null;
            }

            if (!baseValue.HasValue || !modifiedValue.HasValue)
            {
                float value = modifiedValue ?? baseValue ?? 0f;
                return FormatColoredValue(value, modifiedValue, baseValue);
            }

            if (IsApproximatelyEqual(baseValue.Value, modifiedValue.Value))
            {
                return $"<color=#{BaseColorHex}>{FormatNumber(baseValue.Value)}</color>";
            }

            Color modColor = modifiedValue.Value >= baseValue.Value ? StatPositiveColor : StatNegativeColor;
            string modHex = ColorUtility.ToHtmlStringRGB(modColor);

            return $"<color=#{BaseColorHex}>{FormatNumber(baseValue.Value)}</color> / <color=#{modHex}>{FormatNumber(modifiedValue.Value)}</color>";
        }

        private static string FormatColoredValue(float value, float? modifiedValue, float? baseValue)
        {
            bool hasBase = baseValue.HasValue;
            bool hasModified = modifiedValue.HasValue;

            if (hasBase && hasModified && !IsApproximatelyEqual(baseValue.Value, modifiedValue.Value))
            {
                Color diffColor = modifiedValue.Value >= baseValue.Value ? StatPositiveColor : StatNegativeColor;
                string diffHex = ColorUtility.ToHtmlStringRGB(diffColor);
                return $"<color=#{diffHex}>{FormatNumber(modifiedValue.Value)}</color>";
            }

            if (hasBase)
            {
                return $"<color=#{BaseColorHex}>{FormatNumber(baseValue.Value)}</color>";
            }

            return FormatNumber(value);
        }

        private static string FormatNumber(float value)
        {
            if (Mathf.Approximately(value, Mathf.Round(value)))
            {
                return Mathf.RoundToInt(value).ToString();
            }

            return value.ToString("0.##");
        }

        private static bool IsApproximatelyEqual(float a, float b)
        {
            return Mathf.Abs(a - b) <= 0.01f;
            // Placeholder implementation until detailed stat data is wired up.
        }

        private static void PopulateArmorSlots(GeoUnitDescriptor recruit)
        {
            if (_detailArmorRoot == null)
            {
                return;
            }

            RecruitOverlayManagerHelpers.ClearTransformChildren(_detailArmorRoot);

            bool hasArmor = false;
            if (recruit?.ArmorItems != null)
            {
                foreach (var item in recruit.ArmorItems)
                {
                    if (item == null)
                    {
                        continue;
                    }

                    var slot = RecruitOverlayManagerHelpers.MakeInventorySlot(_detailArmorRoot, item, DetailInventorySlotSize, "ArmorSlot");
                    if (slot != null)
                    {
                        hasArmor = true;
                    }
                }
            }

            if (_detailArmorSection != null)
            {
                _detailArmorSection.SetActive(hasArmor);
            }
        }

        private static void PopulateEquipmentSlots(GeoUnitDescriptor recruit)
        {
            if (_detailEquipmentRoot == null)
            {
                return;
            }

            RecruitOverlayManagerHelpers.ClearTransformChildren(_detailEquipmentRoot);

            bool hasEquipment = false;
            if (recruit?.Equipment != null)
            {
                foreach (var item in recruit.Equipment)
                {
                    if (item == null)
                    {
                        continue;
                    }

                    var slot = RecruitOverlayManagerHelpers.MakeInventorySlot(_detailEquipmentRoot, item, DetailInventorySlotSize, "EquipmentSlot");
                    if (slot != null)
                    {
                        hasEquipment = true;
                    }
                }
            }

            if (recruit?.Inventory != null)
            {
                foreach (var item in recruit.Inventory)
                {
                    if (item == null)
                    {
                        continue;
                    }

                    var slot = RecruitOverlayManagerHelpers.MakeInventorySlot(_detailEquipmentRoot, item, DetailInventorySlotSize, "InventorySlot");
                    if (slot != null)
                    {
                        hasEquipment = true;
                    }
                }
            }

            if (_detailEquipmentSection != null)
            {
                _detailEquipmentSection.SetActive(hasEquipment);
            }
        }

        private static void PopulateAbilityRows(
            IReadOnlyList<HavenRecruitsUtils.AbilityIconData> classAbilities,
            IReadOnlyList<HavenRecruitsUtils.AbilityIconData> personalAbilities,
            ISet<int> lockedClassAbilityIndexes,
            ISet<int> lockedPersonalAbilityIndexes)
        {
            bool hasClassAbilities = PopulateAbilityRow(_detailClassAbilityRowRoot, classAbilities, lockedClassAbilityIndexes);
            bool hasPersonalAbilities = PopulateAbilityRow(_detailPersonalAbilityRowRoot, personalAbilities, lockedPersonalAbilityIndexes);

            _detailAbilitySection?.SetActive(hasClassAbilities || hasPersonalAbilities);

            UpdateAbilitySectionHeight(hasClassAbilities, hasPersonalAbilities);

            if (_detailInfoRoot != null)
            {
                var rt = _detailInfoRoot as RectTransform;
                if (rt != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
                }
            }
        }

        private static void UpdateAbilitySectionHeight(bool hasClassAbilities, bool hasPersonalAbilities)
        {
            if (_detailAbilityLayoutElement == null)
            {
                return;
            }

            int activeRows = 0;
            if (hasClassAbilities)
            {
                activeRows++;
            }

            if (hasPersonalAbilities)
            {
                activeRows++;
            }

            if (activeRows == 0)
            {
                _detailAbilityLayoutElement.minHeight = 0f;
                _detailAbilityLayoutElement.preferredHeight = 0f;
                return;
            }

            float totalHeight = DetailAbilityRowHeight * activeRows;
            if (activeRows > 1)
            {
                totalHeight += DetailAbilityRowSpacing * (activeRows - 1);
            }

            totalHeight += DetailAbilityReservedPadding;

            _detailAbilityLayoutElement.minHeight = totalHeight;
            _detailAbilityLayoutElement.preferredHeight = totalHeight;
        }

        private static bool PopulateAbilityRow(Transform root, IReadOnlyList<HavenRecruitsUtils.AbilityIconData> abilities, ISet<int> lockedAbilityIndexes)
        {
            if (root == null)
            {
                return false;
            }

            RecruitOverlayManagerHelpers.ClearTransformChildren(root);

            bool hasAny = false;
            if (abilities != null)
            {
                for (int i = 0; i < abilities.Count; i++)
                {
                    var ability = abilities[i];
                    if (ability.Icon == null)
                    {
                        continue;
                    }

                    var iconImage = RecruitOverlayManagerHelpers.MakeFixedIcon(root, ability.Icon, DetailAbilityIconSize, _abilityIconBackground);
                    if (iconImage == null)
                    {
                        continue;
                    }

                    bool isLocked = lockedAbilityIndexes != null && lockedAbilityIndexes.Contains(i);
                    ApplyAbilityLockState(iconImage, isLocked);

                    var triggerTarget = iconImage.transform?.parent != null ? iconImage.transform.parent.gameObject : iconImage.gameObject;
                    var trigger = triggerTarget.AddComponent<HavenRecruitAbilityTooltipTrigger>();
                    trigger.Initialize(ability);
                    hasAny = true;
                }
            }

            root.gameObject.SetActive(hasAny);
            return hasAny;
        }

        private static void ApplyAbilityLockState(Image iconImage, bool isLocked)
        {
            if (iconImage == null)
            {
                return;
            }

            iconImage.color = isLocked ? AbilityLockedIconColor : AbilityUnlockedIconColor;

            var frameTransform = iconImage.transform?.parent;
            if (frameTransform == null)
            {
                return;
            }

            var background = frameTransform.Find("Background")?.GetComponent<Image>();
            if (background != null)
            {
                background.color = isLocked ? AbilityLockedBackgroundColor : AbilityUnlockedBackgroundColor;
            }
        }

        private static ISet<int> GetLockedClassAbilityIndexes(int recruitLevel, int abilityCount)
        {
            var locked = new HashSet<int>();
            if (abilityCount <= 0)
            {
                return locked;
            }

            int unlockedCount;
            if (recruitLevel >= 7)
            {
                unlockedCount = abilityCount;
            }
            else if (recruitLevel == 6)
            {
                unlockedCount = Mathf.Clamp(abilityCount - 1, 0, abilityCount);
            }
            else if (recruitLevel == 5)
            {
                unlockedCount = Mathf.Clamp(abilityCount - 2, 0, abilityCount);
            }
            else if (recruitLevel == 4 || recruitLevel == 3)
            {
                unlockedCount = Mathf.Clamp(3, 0, abilityCount);
            }
            else if (recruitLevel == 2)
            {
                unlockedCount = Mathf.Clamp(2, 0, abilityCount);
            }
            else if (recruitLevel == 1)
            {
                unlockedCount = Mathf.Clamp(1, 0, abilityCount);
            }
            else
            {
                unlockedCount = Mathf.Clamp(0, 0, abilityCount);
            }

            for (int i = unlockedCount; i < abilityCount; i++)
            {
                locked.Add(i);
            }

            return locked;
        }

        private static ISet<int> GetLockedPersonalAbilityIndexes(int abilityCount)
        {
            var locked = new HashSet<int>();
            if (abilityCount <= 0)
            {
                return locked;
            }

            TFTVConfig config = TFTVMain.Main.Config;

            int startIndex = !config.LearnFirstSkill ? 0 : 1;
            startIndex = Mathf.Clamp(startIndex, 0, abilityCount);

            for (int i = startIndex; i < abilityCount; i++)
            {
                locked.Add(i);
            }

            return locked;
        }
        private static void PopulateDetailPanel(RecruitAtSite data)
        {
            try
            {
                if (data?.Recruit == null)
                {
                    return;
                }

                _detailEmptyState?.SetActive(false);

                _detailInfoRoot?.gameObject.SetActive(true);

                Color factionHeaderColor = UpdateDetailPanelFactionVisuals(data);
                if (_detailFactionNameLabel != null)
                {
                    string factionName = GetFactionDisplayName(data?.HavenOwner);
                    string location = data?.Site?.LocalizedSiteName;
                    _detailFactionNameLabel.text = BuildFactionHeaderText(factionName, location, factionHeaderColor);
                }

                PopulateRecruitCost(data);

                PopulateStats(data);

                var classAbilities = HavenRecruitsUtils.GetClassAbilityIcons(data.Recruit).ToList();
                var personalAbilities = HavenRecruitsUtils.GetPersonalAbilityIcons(data.Recruit).ToList();
                var lockedClassAbilities = GetLockedClassAbilityIndexes(data.Recruit.Level, classAbilities.Count);
                var lockedPersonalAbilities = GetLockedPersonalAbilityIndexes(personalAbilities.Count);
                PopulateAbilityRows(classAbilities, personalAbilities, lockedClassAbilities, lockedPersonalAbilities);

                if (_detailClassIconImage != null)
                {
                    var classIcon = HavenRecruitsUtils.GetClassIcon(data.Recruit);
                    _detailClassIconImage.sprite = classIcon;
                    _detailClassIconImage.color = Color.white;
                    _detailClassIconImage.enabled = classIcon != null;
                }

                if (_detailLevelNameLabel != null)
                {
                    _detailLevelNameLabel.text = $"{data.Recruit.Level.ToString()} {data.Recruit.GetName()}";
                }

                PopulateArmorSlots(data.Recruit);
                PopulateEquipmentSlots(data.Recruit);

                var infoRect = _detailInfoRoot as RectTransform;
                if (infoRect != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(infoRect);
                }
            }
            catch (Exception ex) { TFTVLogger.Error(ex); }
        }

        internal static void ShowRecruitDetails(RecruitAtSite data)
        {
            try
            {
                if (data == null)
                {
                    return;
                }

                _selectedRecruit = data;

                if (_detailPanel == null)
                {
                    return;
                }

                if (!_detailPanel.activeSelf)
                {
                    _detailPanel.SetActive(true);
                }

                PopulateDetailPanel(data);

                if (_detailAnimator != null && !_isDetailVisible)
                {
                    _detailAnimator.Play(true, null);
                }

                _isDetailVisible = true;
            }
            catch (Exception ex) { TFTVLogger.Error(ex); }
        }

        private static Color UpdateDetailPanelFactionVisuals(RecruitAtSite data)
        {
            try
            {


                Sprite factionSprite = null;
                Color factionColorFull = Color.white;

                if (data?.HavenOwner != null && TryGetFactionFilter(data.HavenOwner, out var filter))
                {
                    factionSprite = RecruitOverlayManagerHelpers.GetFactionIcon(filter);
                    var factionColor = GetFactionColor(filter);
                    factionColorFull = factionColor;
                }



                if (_detailFactionIconImage != null)
                {
                    _detailFactionIconImage.sprite = factionSprite;
                    _detailFactionIconImage.color = factionSprite != null ? factionColorFull : Color.white;
                    _detailFactionIconImage.enabled = factionSprite != null;
                }



                if (_detailFactionNameLabel != null)
                {
                    _detailFactionNameLabel.color = Color.white;
                }

                if (_detailPanelGlowShadow != null)
                {
                    var glowColor = factionSprite != null ? factionColorFull : DetailSubTextColor;
                    glowColor.a = 0.35f;
                    _detailPanelGlowShadow.effectColor = glowColor;
                }

                return factionSprite != null ? factionColorFull : DetailSubTextColor;
            }
            catch (Exception ex) { TFTVLogger.Error(ex); }
            return DetailSubTextColor;
        }

        private static string GetFactionDisplayName(GeoFaction faction)
        {
            try
            {
                if (faction == null)
                {
                    return string.Empty;
                }

                if(faction == faction.GeoLevel.AnuFaction)
                {
                    return HavenRecruitsUtils.AnuFactionName;
                }
                if (faction == faction.GeoLevel.NewJerichoFaction)
                {
                    return HavenRecruitsUtils.NJFactionName;
                }
                if (faction == faction.GeoLevel.SynedrionFaction)
                {
                    return HavenRecruitsUtils.SynFactionName;
                }

                return string.Empty;

            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                return string.Empty;
            }
        }

        private static string BuildFactionHeaderText(string factionName, string havenName, Color factionColor)
        {
            bool hasFaction = !string.IsNullOrWhiteSpace(factionName);
            bool hasHaven = !string.IsNullOrWhiteSpace(havenName);

            if (!hasFaction && !hasHaven)
            {
                return string.Empty;
            }

            string havenHex = ColorUtility.ToHtmlStringRGB(Color.white);
            string factionHex = ColorUtility.ToHtmlStringRGB(factionColor);

            if (!hasFaction)
            {
                return $"<color=#{havenHex}>{havenName}</color>";
            }

            if (!hasHaven)
            {
                return $"<color=#{factionHex}>{factionName}</color>";
            }

            return $"<color=#{factionHex}>{factionName}</color>\n<color=#{havenHex}>{havenName}</color>";
        }
        internal static bool TryGetFactionFilter(GeoFaction faction, out FactionFilter filter)
        {
            filter = default;

            try
            {
                if (faction == null)
                {
                    return false;
                }

                var geoLevel = faction.GeoLevel;
                if (geoLevel != null)
                {
                    if (faction == geoLevel.AnuFaction)
                    {
                        filter = FactionFilter.Anu;
                        return true;
                    }

                    if (faction == geoLevel.NewJerichoFaction)
                    {
                        filter = FactionFilter.NewJericho;
                        return true;
                    }

                    if (faction == geoLevel.SynedrionFaction)
                    {
                        filter = FactionFilter.Synedrion;
                        return true;
                    }
                }

                var defName = faction.Def?.name;
                if (!string.IsNullOrEmpty(defName))
                {
                    if (defName.IndexOf("Anu", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        filter = FactionFilter.Anu;
                        return true;
                    }

                    if (defName.IndexOf("Jericho", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        filter = FactionFilter.NewJericho;
                        return true;
                    }

                    if (defName.IndexOf("Synedrion", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        filter = FactionFilter.Synedrion;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
            }

            return false;
        }

        private static string SanitizeName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "Detail";
            }

            return new string(value.Where(char.IsLetterOrDigit).ToArray());
        }

        internal static void PopulateRecruitCost(RecruitAtSite data)
        {
            try
            {
                if (_detailCostRoot == null)
                {
                    return;
                }

                RecruitOverlayManagerHelpers.ClearTransformChildren(_detailCostRoot);

                if (data?.Haven == null || data.Haven.Site?.GeoLevel?.PhoenixFaction == null)
                {
                    if (_detailCostSection != null)
                    {
                        _detailCostSection.SetActive(false);
                    }
                    return;
                }

                var phoenix = data.Haven.Site.GeoLevel.PhoenixFaction;
                var row = HavenRecruitsPrice.CreateCostRow(_detailCostRoot, data.Haven, phoenix, null);
                if (row != null)
                {
                    var layout = row.GetComponent<HorizontalLayoutGroup>();
                    if (layout != null)
                    {
                        layout.childAlignment = TextAnchor.MiddleCenter;
                    }
                }

                bool hasCost = row != null && row.transform.childCount > 0;
                if (_detailCostSection != null)
                {
                    _detailCostSection.SetActive(hasCost);
                }
            }
            catch (Exception ex) { TFTVLogger.Error(ex); }
        }

        internal static GameObject CreateDetailPlaceholder(Transform parent)
        {
            var placeholder = CreateDetailText(parent, "Placeholder", TextFontSize, DetailSubTextColor, TextAnchor.MiddleCenter);
            placeholder.text = "Select a recruit to view details.";
            var le = placeholder.gameObject.AddComponent<LayoutElement>();
            le.flexibleHeight = 1f;
            return placeholder.gameObject;
        }
    }
}
