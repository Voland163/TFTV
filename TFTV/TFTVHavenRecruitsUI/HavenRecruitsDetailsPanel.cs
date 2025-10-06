using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.View.ViewControllers;
using PhoenixPoint.Tactical.View.ViewModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TFTV.TFTVHavenRecruitsUI;
using UnityEngine;
using UnityEngine.UI;
using static TFTV.HavenRecruitsMain;
using static TFTV.HavenRecruitsMain.RecruitOverlayManager;
using static TFTV.TFTVHavenRecruitsUI.HavenRecruitsOverlayAnimator;
using HavenRecruitsUtils = TFTV.TFTVHavenRecruitsUI.HavenRecruitsUtils;

namespace TFTV
{
    internal static class HavenRecruitsDetailsPanel
    {
        internal const float DetailClassIconSize = 48f;
        internal const float DetailFactionIconSize = 36f;

        internal static GameObject _detailPanel;
        internal static GameObject _detailEmptyState;
        internal static Transform _detailInfoRoot;
        internal static Image _detailClassIconImage;
        internal static Text _detailLevelNameLabel;
        internal static Image _detailFactionIconImage;
        internal static Text _detailHavenLabel;
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
        internal static Image _detailFactionLogoImage;

        private const string StatPlaceholder = "--";
        private static readonly Dictionary<string, Sprite> _statIconCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private static readonly Color StatPositiveColor = new Color(0.3137f, 0.7843f, 0.3921f);
        private static readonly Color StatNegativeColor = new Color(0.8627f, 0.3529f, 0.3529f);
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
            public StatCell(Image icon, Text value)
            {
                Icon = icon;
                Value = value;
            }

            public Image Icon { get; }

            public Text Value { get; }
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
                layout.spacing = 8f;
                layout.childControlWidth = false;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
                layout.padding = new RectOffset(24, 24, 12, 12);

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

                var rt = _detailPanel.GetComponent<RectTransform>();
                float width = GetOverlayWidthFraction(out float detailPixels);
                rt.anchorMin = new Vector2(OverlayLeftMargin, OverlayBottomMargin);
                rt.anchorMax = new Vector2(OverlayLeftMargin + width, 1f - OverlayTopMargin);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                var (logoGO, logoRT) = RecruitOverlayManagerHelpers.NewUI("FactionLogo", _detailPanel.transform);
                logoRT.anchorMin = new Vector2(0.5f, 0.5f);
                logoRT.anchorMax = new Vector2(0.5f, 0.5f);
                logoRT.pivot = new Vector2(0.5f, 0.5f);
                logoRT.sizeDelta = new Vector2(120f, 120f);
                logoRT.anchoredPosition = Vector2.zero;
                logoGO.transform.SetAsFirstSibling();

                var logoImage = logoGO.AddComponent<Image>();
                logoImage.color = new Color(1f, 1f, 1f, 0.3f);
                logoImage.raycastTarget = false;
                logoImage.preserveAspect = true;
                logoImage.enabled = false;
                _detailFactionLogoImage = logoImage;

                CreateDetailHeader(_detailPanel.transform);

                var (contentGO, contentRT) = RecruitOverlayManagerHelpers.NewUI("Content", _detailPanel.transform);
                contentRT.anchorMin = new Vector2(0f, 0f);
                contentRT.anchorMax = new Vector2(1f, 0.94f);
                contentRT.offsetMin = new Vector2(24f, 24f);
                contentRT.offsetMax = new Vector2(-24f, -24f);

                var layout = contentGO.AddComponent<VerticalLayoutGroup>();
                layout.childAlignment = TextAnchor.UpperLeft;
                layout.spacing = 12f;
                layout.childControlWidth = true;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;

                _detailEmptyState = CreateDetailPlaceholder(contentGO.transform);

                var (infoRootGO, _) = RecruitOverlayManagerHelpers.NewUI("InfoRoot", contentGO.transform);
                var infoLayout = infoRootGO.AddComponent<VerticalLayoutGroup>();
                infoLayout.childAlignment = TextAnchor.UpperLeft;
                infoLayout.spacing = 8f;
                infoLayout.childControlWidth = true;
                infoLayout.childControlHeight = false;
                infoLayout.childForceExpandWidth = true;
                infoLayout.childForceExpandHeight = false;

                _detailInfoRoot = infoRootGO.transform;
                _detailInfoRoot.gameObject.SetActive(false);

                var (factionHeaderGO, _) = RecruitOverlayManagerHelpers.NewUI("FactionHeader", infoRootGO.transform);
                var factionHeaderLayout = factionHeaderGO.AddComponent<HorizontalLayoutGroup>();
                factionHeaderLayout.childAlignment = TextAnchor.UpperLeft;
                factionHeaderLayout.spacing = 4f;
                factionHeaderLayout.childControlWidth = false;
                factionHeaderLayout.childControlHeight = false;
                factionHeaderLayout.childForceExpandWidth = false;
                factionHeaderLayout.childForceExpandHeight = false;

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

                _detailHavenLabel = CreateDetailText(factionHeaderGO.transform, "HavenName", TextFontSize + 2, Color.white, TextAnchor.MiddleLeft);
                _detailHavenLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
                var havenLabelLE = _detailHavenLabel.gameObject.AddComponent<LayoutElement>();
                havenLabelLE.minWidth = 0f;
                havenLabelLE.flexibleWidth = 1f;

                var (classInfoGO, _) = RecruitOverlayManagerHelpers.NewUI("ClassInfo", infoRootGO.transform);
                var classInfoLayout = classInfoGO.AddComponent<HorizontalLayoutGroup>();
                classInfoLayout.childAlignment = TextAnchor.MiddleCenter;
                classInfoLayout.spacing = 0f;
                classInfoLayout.childControlWidth = false;
                classInfoLayout.childControlHeight = false;
                classInfoLayout.childForceExpandWidth = false;
                classInfoLayout.childForceExpandHeight = false;

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

                _detailLevelNameLabel = CreateDetailText(classInfoGO.transform, "LevelName", TextFontSize + 6, Color.white, TextAnchor.MiddleLeft);
                _detailLevelNameLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
                _detailLevelNameLabel.text = StatPlaceholder;
                var levelNameLE = _detailLevelNameLabel.gameObject.AddComponent<LayoutElement>();
                levelNameLE.minWidth = 0f;
                levelNameLE.flexibleWidth = 1f;

                var (costGO, _) = RecruitOverlayManagerHelpers.NewUI("CostRow", infoRootGO.transform);
                var costLayout = costGO.AddComponent<HorizontalLayoutGroup>();
                costLayout.childAlignment = TextAnchor.MiddleCenter;
                costLayout.spacing = 12f;
                costLayout.childControlWidth = false;
                costLayout.childControlHeight = false;
                costLayout.childForceExpandWidth = false;
                costLayout.childForceExpandHeight = false;
                _detailCostSection = costGO;
                _detailCostRoot = costGO.transform;
                _detailCostSection.SetActive(false);

                var (statsSectionGO, _) = RecruitOverlayManagerHelpers.NewUI("StatsSection", infoRootGO.transform);
                var statsSectionLayout = statsSectionGO.AddComponent<HorizontalLayoutGroup>();
                statsSectionLayout.childAlignment = TextAnchor.UpperLeft;
                statsSectionLayout.spacing = 0f;
                statsSectionLayout.childControlWidth = true;
                statsSectionLayout.childControlHeight = true;
                statsSectionLayout.childForceExpandWidth = true;
                statsSectionLayout.childForceExpandHeight = false;

                var (statsGridGO, statsGridRT) = RecruitOverlayManagerHelpers.NewUI("StatsGrid", statsSectionGO.transform);
                var statsGrid = statsGridGO.AddComponent<GridLayoutGroup>();
                statsGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                statsGrid.constraintCount = 2;
                statsGrid.cellSize = new Vector2(140f, 80f);
                statsGrid.spacing = new Vector2(16f, 12f);
                statsGrid.childAlignment = TextAnchor.UpperCenter;
                statsGridRT.anchorMin = new Vector2(0.5f, 0.5f);
                statsGridRT.anchorMax = new Vector2(0.5f, 0.5f);
                statsGridRT.pivot = new Vector2(0.5f, 0.5f);
                var statsGridLE = statsGridGO.AddComponent<LayoutElement>();
                float statsHeight = statsGrid.cellSize.y * 3f + statsGrid.spacing.y * 2f;
                float statsWidth = statsGrid.cellSize.x * statsGrid.constraintCount + statsGrid.spacing.x * (statsGrid.constraintCount - 1);
                statsGridLE.minHeight = statsHeight;
                statsGridLE.preferredHeight = statsHeight;
                statsGridLE.preferredWidth = statsWidth;
                statsGridLE.flexibleWidth = 1f;

                _detailStatCells.Clear();
                _detailStatsGridRoot = statsGridGO.transform;
                foreach (var statName in DefaultStatOrder)
                {
                    var cell = CreateStatCell(statName, _detailStatsGridRoot);
                    _detailStatCells[statName] = cell;
                }

                var (armorSectionGO, _) = RecruitOverlayManagerHelpers.NewUI("ArmorSection", infoRootGO.transform);
                var armorLayout = armorSectionGO.AddComponent<HorizontalLayoutGroup>();
                armorLayout.childAlignment = TextAnchor.UpperLeft;
                armorLayout.spacing = 8f;
                armorLayout.childControlWidth = false;
                armorLayout.childControlHeight = false;
                armorLayout.childForceExpandWidth = false;
                armorLayout.childForceExpandHeight = false;
                var armorLE = armorSectionGO.AddComponent<LayoutElement>();
                armorLE.minHeight = DetailClassIconSize;
                armorLE.preferredHeight = DetailClassIconSize;
                armorLE.flexibleWidth = 1f;
                _detailArmorSection = armorSectionGO;
                _detailArmorRoot = armorSectionGO.transform;
                _detailArmorSection.SetActive(false);

                var (equipmentGO, _) = RecruitOverlayManagerHelpers.NewUI("EquipmentSection", infoRootGO.transform);
                var equipmentLayout = equipmentGO.AddComponent<HorizontalLayoutGroup>();
                equipmentLayout.childAlignment = TextAnchor.UpperLeft;
                equipmentLayout.spacing = 8f;
                equipmentLayout.childControlWidth = false;
                equipmentLayout.childControlHeight = false;
                equipmentLayout.childForceExpandWidth = false;
                equipmentLayout.childForceExpandHeight = false;
                var equipmentLE = equipmentGO.AddComponent<LayoutElement>();
                equipmentLE.minHeight = DetailClassIconSize;
                equipmentLE.preferredHeight = DetailClassIconSize;
                equipmentLE.flexibleWidth = 1f;
                _detailEquipmentSection = equipmentGO;
                _detailEquipmentRoot = equipmentGO.transform;
                _detailEquipmentSection.SetActive(false);

                var (abilitiesGO, _) = RecruitOverlayManagerHelpers.NewUI("AbilitySection", infoRootGO.transform);
                var abilityLayout = abilitiesGO.AddComponent<VerticalLayoutGroup>();
                abilityLayout.childAlignment = TextAnchor.MiddleCenter;
                abilityLayout.spacing = 6f;
                abilityLayout.childControlWidth = false;
                abilityLayout.childControlHeight = false;
                abilityLayout.childForceExpandWidth = false;
                abilityLayout.childForceExpandHeight = false;
                var abilityLE = abilitiesGO.AddComponent<LayoutElement>();
                abilityLE.minHeight = AbilityIconSize + 12f;
                abilityLE.flexibleWidth = 1f;
                _detailAbilitySection = abilitiesGO;
                _detailAbilitySection.SetActive(false);

                var (classAbilityRowGO, _) = RecruitOverlayManagerHelpers.NewUI("ClassAbilityRow", abilitiesGO.transform);
                var classAbilityLayout = classAbilityRowGO.AddComponent<HorizontalLayoutGroup>();
                classAbilityLayout.childAlignment = TextAnchor.MiddleCenter;
                classAbilityLayout.spacing = 8f;
                classAbilityLayout.childControlWidth = false;
                classAbilityLayout.childControlHeight = false;
                classAbilityLayout.childForceExpandWidth = false;
                classAbilityLayout.childForceExpandHeight = false;
                _detailClassAbilityRowRoot = classAbilityRowGO.transform;
                _detailClassAbilityRowRoot.gameObject.SetActive(false);

                var (personalAbilityRowGO, _) = RecruitOverlayManagerHelpers.NewUI("PersonalAbilityRow", abilitiesGO.transform);
                var personalAbilityLayout = personalAbilityRowGO.AddComponent<HorizontalLayoutGroup>();
                personalAbilityLayout.childAlignment = TextAnchor.MiddleCenter;
                personalAbilityLayout.spacing = 8f;
                personalAbilityLayout.childControlWidth = false;
                personalAbilityLayout.childControlHeight = false;
                personalAbilityLayout.childForceExpandWidth = false;
                personalAbilityLayout.childForceExpandHeight = false;
                _detailPersonalAbilityRowRoot = personalAbilityRowGO.transform;
                _detailPersonalAbilityRowRoot.gameObject.SetActive(false);

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

            var layout = cellGO.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 4f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var (iconGO, iconRT) = RecruitOverlayManagerHelpers.NewUI($"{sanitized}Icon", cellGO.transform);
            var icon = iconGO.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.enabled = false;
            var iconLE = iconGO.AddComponent<LayoutElement>();
            iconLE.preferredWidth = 36f;
            iconLE.preferredHeight = 36f;
            iconLE.minWidth = 36f;
            iconLE.minHeight = 36f;
            iconRT.sizeDelta = new Vector2(36f, 36f);

            var label = CreateDetailText(cellGO.transform, $"{sanitized}Label", TextFontSize - 2, DetailSubTextColor, TextAnchor.MiddleCenter);
            label.text = name;

            var value = CreateDetailText(cellGO.transform, $"{sanitized}Value", TextFontSize + 2, Color.white, TextAnchor.MiddleCenter);
            value.text = StatPlaceholder;

            return new StatCell(icon, value);
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

                PopulateStats(null);
                PopulateArmorSlots(null);
                PopulateEquipmentSlots(null);
                PopulateAbilityRows(null, null);

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
            if (_detailFactionLogoImage != null)
            {
                _detailFactionLogoImage.sprite = null;
                _detailFactionLogoImage.enabled = false;
            }
            if (_detailFactionIconImage != null)
            {
                _detailFactionIconImage.sprite = null;
                _detailFactionIconImage.enabled = false;
                _detailFactionIconImage.color = Color.white;
            }
            if (_detailHavenLabel != null)
            {
                _detailHavenLabel.color = Color.white;
            }
        }

        private static void PopulateStats(GeoUnitDescriptor recruit)
        {
            foreach (var cell in _detailStatCells.Values)
            {
                if (cell.Icon != null)
                {
                    cell.Icon.sprite = null;
                    cell.Icon.enabled = false;
                }

                if (cell.Value != null)
                {
                    cell.Value.text = StatPlaceholder;
                }
            }

            if (recruit == null)
            {
                RefreshStatsLayout();
                return;
            }
            try
            {
                var stats = recruit.GetStats();
                if (stats == null)
                {
                    RefreshStatsLayout();
                    return;
                }

                foreach (var statName in DefaultStatOrder)
                {
                    if (!_detailStatCells.TryGetValue(statName, out var cell))
                    {
                        continue;
                    }

                    var statObject = GetStatObject(stats, statName);
                    if (statObject == null)
                    {
                        continue;
                    }

                    var (baseValue, modifiedValue) = GetStatValues(statObject);
                    Sprite icon = GetStatIcon(statName, statObject);

                    if (cell.Icon != null)
                    {
                        cell.Icon.sprite = icon;
                        cell.Icon.enabled = icon != null;
                    }

                    if (cell.Value != null)
                    {
                        string formatted = FormatStatValue(baseValue, modifiedValue);
                        cell.Value.text = string.IsNullOrEmpty(formatted) ? StatPlaceholder : formatted;
                    }
                }
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
            }

            RefreshStatsLayout();
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

        private static Sprite GetStatIcon(string statName, object statObject)
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

            object statDef = GetMemberValue(statObject, "StatDef") ?? GetMemberValue(statObject, "StatDefinition");
            object viewElement = GetMemberValue(statObject, "ViewElementDef") ?? GetMemberValue(statDef, "ViewElementDef");

            if (viewElement is ViewElementDef ved)
            {
                icon = ved.SmallIcon ?? ved.InventoryIcon ?? ved.LargeIcon ?? ved.RosterIcon;
            }

            if (icon == null)
            {
                icon = GetMemberValue(statObject, "Icon") as Sprite;
            }

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

                    var slot = RecruitOverlayManagerHelpers.MakeInventorySlot(_detailArmorRoot, item, ArmorIconSize, "ArmorSlot");
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

                    var slot = RecruitOverlayManagerHelpers.MakeInventorySlot(_detailEquipmentRoot, item, ArmorIconSize, "EquipmentSlot");
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

                    var slot = RecruitOverlayManagerHelpers.MakeInventorySlot(_detailEquipmentRoot, item, ArmorIconSize, "InventorySlot");
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

        private static void PopulateAbilityRows(IEnumerable<HavenRecruitsUtils.AbilityIconData> classAbilities, IEnumerable<HavenRecruitsUtils.AbilityIconData> personalAbilities)
        {
            bool hasClassAbilities = PopulateAbilityRow(_detailClassAbilityRowRoot, classAbilities);
            bool hasPersonalAbilities = PopulateAbilityRow(_detailPersonalAbilityRowRoot, personalAbilities);

            if (_detailAbilitySection != null)
            {
                _detailAbilitySection.SetActive(hasClassAbilities || hasPersonalAbilities);
            }
        }

        private static bool PopulateAbilityRow(Transform root, IEnumerable<HavenRecruitsUtils.AbilityIconData> abilities)
        {
            if (root == null)
            {
                return false;
            }

            RecruitOverlayManagerHelpers.ClearTransformChildren(root);

            bool hasAny = false;
            if (abilities != null)
            {
                foreach (var ability in abilities)
                {
                    if (ability.Icon == null)
                    {
                        continue;
                    }

                    var iconImage = RecruitOverlayManagerHelpers.MakeFixedIcon(root, ability.Icon, AbilityIconSize, _abilityIconBackground);
                    if (iconImage == null)
                    {
                        continue;
                    }

                    var trigger = iconImage.gameObject.AddComponent<HavenRecruitAbilityTooltipTrigger>();
                    trigger.Initialize(ability);
                    hasAny = true;
                }
            }

            root.gameObject.SetActive(hasAny);
            return hasAny;
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

                UpdateDetailPanelFactionVisuals(data);

                if (_detailHavenLabel != null)
                {
                    string location = data.Site?.LocalizedSiteName;
                    _detailHavenLabel.text = location;
                }

                PopulateRecruitCost(data);

                PopulateStats(data.Recruit);

                var classAbilities = HavenRecruitsUtils.GetClassAbilityIcons(data.Recruit).ToList();
                var personalAbilities = HavenRecruitsUtils.GetPersonalAbilityIcons(data.Recruit).ToList();
                PopulateAbilityRows(classAbilities, personalAbilities);

                if (_detailClassIconImage != null)
                {
                    var classIcon = HavenRecruitsUtils.GetClassIcon(data.Recruit);
                    _detailClassIconImage.sprite = classIcon;
                    _detailClassIconImage.color = Color.white;
                    _detailClassIconImage.enabled = classIcon != null;
                }

                if (_detailLevelNameLabel != null)
                {
                   
                    var recruitName = data.Recruit.GetName();
                    _detailLevelNameLabel.text = $"{data.Recruit.Level} {recruitName}";
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

        private static void UpdateDetailPanelFactionVisuals(RecruitAtSite data)
        {
            try
            {
                if (_detailFactionLogoImage == null)
                {
                    return;
                }

                Sprite factionSprite = null;
                Color tintColor = new Color(1f, 1f, 1f, 0f);
                Color factionColorFull = Color.white;

                if (data?.HavenOwner != null && TryGetFactionFilter(data.HavenOwner, out var filter))
                {
                    factionSprite = RecruitOverlayManagerHelpers.GetFactionIcon(filter);
                    var factionColor = GetFactionColor(filter);
                    tintColor = new Color(factionColor.r, factionColor.g, factionColor.b, 0.7f);
                    factionColorFull = factionColor;
                }

                _detailFactionLogoImage.sprite = factionSprite;
                _detailFactionLogoImage.color = tintColor;
                _detailFactionLogoImage.enabled = factionSprite != null;

                if (_detailFactionIconImage != null)
                {
                    _detailFactionIconImage.sprite = factionSprite;
                    _detailFactionIconImage.color = factionSprite != null ? factionColorFull : Color.white;
                    _detailFactionIconImage.enabled = factionSprite != null;
                }

                if (_detailHavenLabel != null)
                {
                    _detailHavenLabel.color = factionSprite != null ? factionColorFull : Color.white;
                }
            }
            catch (Exception ex) { TFTVLogger.Error(ex); }
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
                        layout.childAlignment = TextAnchor.UpperLeft;
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
