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
        internal static Text _detailLevelLabel;
        internal static Text _detailNameLabel;
        internal static Image _detailFactionIconImage;
        internal static Text _detailHavenLabel;
        internal static GameObject _detailAbilitySection;
        internal static Transform _detailAbilityRoot;
        internal static GameObject _detailEquipmentSection;
        internal static Transform _detailEquipmentRoot;
        internal static GameObject _detailCostSection;
        internal static Transform _detailCostRoot;
        internal static Image _detailFactionLogoImage;

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
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.spacing = 1f;
                layout.childControlWidth = true;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;

                _detailEmptyState = CreateDetailPlaceholder(contentGO.transform);

                var (infoRootGO, _) = RecruitOverlayManagerHelpers.NewUI("InfoRoot", contentGO.transform);
                var infoLayout = infoRootGO.AddComponent<VerticalLayoutGroup>();
                infoLayout.childAlignment = TextAnchor.UpperCenter;
                infoLayout.spacing = 1f;
                infoLayout.childControlWidth = true;
                infoLayout.childControlHeight = false;
                infoLayout.childForceExpandWidth = true;
                infoLayout.childForceExpandHeight = false;

                _detailInfoRoot = infoRootGO.transform;
                _detailInfoRoot.gameObject.SetActive(false);

                var (mainInfoGO, _) = RecruitOverlayManagerHelpers.NewUI("MainInfo", infoRootGO.transform);
                var mainInfoLE = mainInfoGO.AddComponent<LayoutElement>();
                mainInfoLE.flexibleWidth = 1f;
                mainInfoLE.minWidth = 0f;
                var mainLayout = mainInfoGO.AddComponent<HorizontalLayoutGroup>();
                mainLayout.childAlignment = TextAnchor.MiddleCenter;
                mainLayout.spacing = 1f;
                mainLayout.childControlWidth = false;
                mainLayout.childControlHeight = false;
                mainLayout.childForceExpandWidth = false;
                mainLayout.childForceExpandHeight = false;

                var (factionInfoGO, _) = RecruitOverlayManagerHelpers.NewUI("FactionInfo", infoRootGO.transform);
                var factionInfoLE = factionInfoGO.AddComponent<LayoutElement>();
                factionInfoLE.flexibleWidth = 1f;
                factionInfoLE.minWidth = 0f;
                var factionLayout = factionInfoGO.AddComponent<HorizontalLayoutGroup>();
                factionLayout.childAlignment = TextAnchor.MiddleCenter;
                factionLayout.spacing = 1f;
                factionLayout.childControlWidth = false;
                factionLayout.childControlHeight = false;
                factionLayout.childForceExpandWidth = false;
                factionLayout.childForceExpandHeight = false;

                var (factionIconGO, factionIconRT) = RecruitOverlayManagerHelpers.NewUI("FactionIcon", factionInfoGO.transform);
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

                _detailHavenLabel = CreateDetailText(factionInfoGO.transform, "HavenName", TextFontSize + 2, Color.white, TextAnchor.MiddleCenter);
                _detailHavenLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
                var havenLabelLE = _detailHavenLabel.gameObject.AddComponent<LayoutElement>();
                havenLabelLE.minWidth = 0f;
                havenLabelLE.flexibleWidth = 1f;

                var (costGO, _) = RecruitOverlayManagerHelpers.NewUI("CostRow", infoRootGO.transform);
                var costLayout = costGO.AddComponent<HorizontalLayoutGroup>();
                costLayout.childAlignment = TextAnchor.MiddleCenter;
                costLayout.spacing = 10f;
                costLayout.childControlWidth = false;
                costLayout.childControlHeight = false;
                costLayout.childForceExpandWidth = false;
                costLayout.childForceExpandHeight = false;
                _detailCostSection = costGO;
                _detailCostRoot = costGO.transform;
                _detailCostSection.SetActive(false);

                var (classIconGO, classIconRT) = RecruitOverlayManagerHelpers.NewUI("ClassIcon", mainInfoGO.transform);
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

                _detailLevelLabel = CreateDetailText(mainInfoGO.transform, "Level", TextFontSize + 4, Color.white, TextAnchor.MiddleCenter);
                _detailLevelLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
                var levelLabelLE = _detailLevelLabel.gameObject.AddComponent<LayoutElement>();
                levelLabelLE.minWidth = 28f;
                levelLabelLE.preferredWidth = 28f;

                _detailNameLabel = CreateDetailText(mainInfoGO.transform, "Name", TextFontSize + 6, Color.white, TextAnchor.MiddleCenter);
                _detailNameLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
                var nameLabelLE = _detailNameLabel.gameObject.AddComponent<LayoutElement>();
                nameLabelLE.minWidth = 0f;
                nameLabelLE.flexibleWidth = 1f;

                var (abilitiesGO, _) = RecruitOverlayManagerHelpers.NewUI("AbilityDetails", infoRootGO.transform);

                var abilityLayout = abilitiesGO.AddComponent<HorizontalLayoutGroup>();
                abilityLayout.childAlignment = TextAnchor.UpperRight;
                abilityLayout.spacing = 8f;
                abilityLayout.childControlWidth = true;
                abilityLayout.childControlHeight = false;
                abilityLayout.childForceExpandWidth = true;
                abilityLayout.childForceExpandHeight = false;
                abilityLayout.padding = new RectOffset(12, 12, 0, 0);
                var abilityLE = abilitiesGO.AddComponent<LayoutElement>();
                abilityLE.flexibleWidth = 1f;
                abilityLE.minWidth = 0f;
                _detailAbilitySection = abilitiesGO;
                _detailAbilityRoot = abilitiesGO.transform;
                _detailAbilitySection.SetActive(false);

                var (equipmentGO, _) = RecruitOverlayManagerHelpers.NewUI("EquipmentRow", infoRootGO.transform);
                var equipmentLayout = equipmentGO.AddComponent<HorizontalLayoutGroup>();
                equipmentLayout.childAlignment = TextAnchor.MiddleCenter;
                equipmentLayout.spacing = 10f;
                equipmentLayout.childControlWidth = false;
                equipmentLayout.childControlHeight = false;
                equipmentLayout.childForceExpandWidth = false;
                equipmentLayout.childForceExpandHeight = false;
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

                var abilityViews = GetRecruitAbilityViews(data.Recruit).ToList();
                PopulateAbilityDetails(abilityViews);

                if (_detailClassIconImage != null)
                {
                    var classIcon = HavenRecruitsUtils.GetClassIcon(data.Recruit);
                    _detailClassIconImage.sprite = classIcon;
                    _detailClassIconImage.color = Color.white;
                    _detailClassIconImage.enabled = classIcon != null;
                }

                if (_detailLevelLabel != null)
                {
                    _detailLevelLabel.text = $"{data.Recruit.Level}";
                }

                if (_detailNameLabel != null)
                {
                    _detailNameLabel.text = data.Recruit.GetName();
                }
            
                PopulateEquipmentIcons(data.Recruit);

          

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

        private static void PopulateAbilityDetails(IList<ViewElementDef> abilities)
        {
            try
            {
                if (_detailAbilityRoot == null)
                {
                    return;
                }

                RecruitOverlayManagerHelpers.ClearTransformChildren(_detailAbilityRoot);

                bool hasAbilities = abilities != null && abilities.Count > 0;

                if (hasAbilities)
                {
                    foreach (var view in abilities)
                    {
                        if (view == null)
                        {
                            continue;
                        }

                        string displayName = view.DisplayName1 != null ? view.DisplayName1.Localize() : view.name;
                        if (string.IsNullOrWhiteSpace(displayName))
                        {
                            displayName = "Unknown Ability";
                        }

                        string description = view.Description != null ? view.Description.Localize() : string.Empty;
                        AddDetailRow(_detailAbilityRoot, view.SmallIcon ?? view.InventoryIcon, AbilityIconSize, displayName, description, useAbilityBackground: true);
                    }
                }

                if (_detailAbilitySection != null)
                {
                    _detailAbilitySection.SetActive(hasAbilities);
                }
            }
            catch (Exception ex) { TFTVLogger.Error(ex); }
        }

        

        private static void PopulateEquipmentIcons(GeoUnitDescriptor recruit)
        {
            try
            {
                if (_detailEquipmentRoot == null)
                {
                    return;
                }

                RecruitOverlayManagerHelpers.ClearTransformChildren(_detailEquipmentRoot);

                var icons = new List<Sprite>();
                if (recruit != null)
                {
                    icons.AddRange(GetItemIcons(recruit.Equipment));
                    icons.AddRange(GetItemIcons(recruit.ArmorItems));
                    icons.AddRange(GetItemIcons(recruit.Inventory));
                }

                bool hasIcons = icons.Count > 0;

                foreach (var icon in icons)
                {
                    if (icon == null)
                    {
                        continue;
                    }

                    RecruitOverlayManagerHelpers.MakeFixedIcon(_detailEquipmentRoot, icon, ArmorIconSize);
                }

                if (_detailEquipmentSection != null)
                {
                    _detailEquipmentSection.SetActive(hasIcons);
                }
            }
            catch (Exception ex) { TFTVLogger.Error(ex); }
        }

        private static IEnumerable<Sprite> GetItemIcons(IEnumerable<ItemDef> items)
        {
            if (items == null)
            {
                yield break;
            }

            foreach (var item in items)
            {
                if (item == null)
                {
                    continue;
                }

                var view = item.ViewElementDef;
                if (view?.InventoryIcon != null)
                {
                    yield return view.InventoryIcon;
                    continue;
                }

                if (view?.SmallIcon != null)
                {
                    yield return view.SmallIcon;
                }
            }
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

        private static void AddDetailRow(Transform parent, Sprite icon, int iconSize, string title, string description, bool useAbilityBackground = false)
        {
            string sanitizedName = SanitizeName(title);
            var (rowGO, _) = RecruitOverlayManagerHelpers.NewUI($"{sanitizedName}Row{parent.childCount}", parent);

            var rowLayout = rowGO.AddComponent<HorizontalLayoutGroup>();
            rowLayout.childAlignment = TextAnchor.UpperLeft;
            rowLayout.spacing = 10f;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = false;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
            rowLayout.padding = new RectOffset(16, 16, 0, 0);

            if (icon != null)
            {
                Sprite background = null;
                if (useAbilityBackground)
                {
                    if (_abilityIconBackground == null)
                    {
                        _abilityIconBackground = Helper.CreateSpriteFromImageFile("UI_ButtonFrame_Main_Sliced.png");
                    }

                    background = _abilityIconBackground;
                }

                RecruitOverlayManagerHelpers.MakeFixedIcon(rowGO.transform, icon, iconSize, background);
            }

          
            var rowLE = rowGO.AddComponent<LayoutElement>();
            rowLE.minWidth = 0f;
            rowLE.flexibleWidth = 1f;
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

        internal static IEnumerable<ViewElementDef> GetItemViewElements(GeoUnitDescriptor recruit)
        {
            if (recruit?.ArmorItems == null)
            {
                yield break;
            }

            
            foreach (var item in recruit.ArmorItems.Where(i => i != null))
            {
 
                var view = item.ViewElementDef;
                if (view != null)
                {
                    yield return view;
                }
            }
        }

        internal static IEnumerable<ViewElementDef> GetRecruitAbilityViews(GeoUnitDescriptor recruit)
        {
            if (recruit == null)
            {
                yield break;
            }

            var track = recruit.GetPersonalAbilityTrack();
            var abilities = track?.AbilitiesByLevel?.ToList();
            if (abilities == null || abilities.Count == 0)
            {
                yield break;
            }

            int abilityCount = Math.Min(6, abilities.Count);
            for (int index = 0; index < abilityCount; index++)
            {
                var view = abilities[index]?.Ability?.ViewElementDef;
                if (view != null)
                {
                    yield return view;
                }
            }
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
