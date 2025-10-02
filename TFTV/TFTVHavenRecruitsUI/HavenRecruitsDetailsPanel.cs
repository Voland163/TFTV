using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static TFTV.HavenRecruitsMain;
using static TFTV.TFTVHavenRecruitsUI.HavenRecruitsOverlayAnimator;
using HavenRecruitsUtils = TFTV.TFTVHavenRecruitsUI.HavenRecruitsUtils;

using static TFTV.HavenRecruitsMain.RecruitOverlayManager;

namespace TFTV
{
    internal static class HavenRecruitsDetailsPanel
    {
        internal static GameObject _detailPanel;
        internal static GameObject _detailEmptyState;
        internal static Transform _detailInfoRoot;
        internal static Image _detailClassIconImage;
        internal static Text _detailLevelLabel;
        internal static Text _detailNameLabel;
        internal static Image _detailFactionIconImage;
        internal static Text _detailHavenLabel;
        internal static GameObject _detailAbilityDescriptionGroup;
        internal static Transform _detailAbilityDescriptionRoot;
        internal static GameObject _detailMutationGroup;
        internal static Transform _detailMutationRoot;
        internal static GameObject _detailCostGroup;
        internal static Transform _detailCostRoot;
        internal static Image _detailFactionLogoImage;

        private static GameObject CreateDetailGroup(Transform parent, string headerText, out Transform contentRoot)
        {
            var (groupGO, _) = RecruitOverlayManagerHelpers.NewUI($"{headerText.Replace(" ", string.Empty)}Group", parent);
            var groupLayout = groupGO.AddComponent<VerticalLayoutGroup>();
            groupLayout.childAlignment = TextAnchor.UpperLeft;
            groupLayout.spacing = 6f;
            groupLayout.childControlWidth = true;
            groupLayout.childControlHeight = false;
            groupLayout.childForceExpandWidth = true;
            groupLayout.childForceExpandHeight = false;

            var header = HavenRecruitsDetailsPanel.CreateDetailText(groupGO.transform, "Header", TextFontSize - 2, TabHighlightColor, TextAnchor.UpperLeft);
            header.text = headerText;

            var (contentGO, _) = RecruitOverlayManagerHelpers.NewUI("Content", groupGO.transform);
            var contentLayout = contentGO.AddComponent<HorizontalLayoutGroup>();
            contentLayout.childAlignment = TextAnchor.MiddleLeft;
            contentLayout.spacing = 6f;
            contentLayout.childControlWidth = false;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = false;
            contentLayout.childForceExpandHeight = false;

            contentRoot = contentGO.transform;
            return groupGO;
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
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
                layout.padding = new RectOffset(24, 24, 12, 12);

                var (titleGO, _) = RecruitOverlayManagerHelpers.NewUI("Title", headerGO.transform);
                var title = titleGO.AddComponent<Text>();
                title.font = PuristaSemibold ? PuristaSemibold : Resources.GetBuiltinResource<Font>("Arial.ttf");
                title.fontSize = TextFontSize + 2;
                title.color = Color.white;
                title.alignment = TextAnchor.MiddleLeft;
                title.text = "RECRUIT DETAILS";

                var (spacer, _) = RecruitOverlayManagerHelpers.NewUI("Spacer", headerGO.transform);
                var spacerElement = spacer.AddComponent<LayoutElement>();
                spacerElement.flexibleWidth = 1f;
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
                logoRT.sizeDelta = new Vector2(420f, 420f);
                logoRT.anchoredPosition = Vector2.zero;
                logoGO.transform.SetAsFirstSibling();

                var logoImage = logoGO.AddComponent<Image>();
                logoImage.color = new Color(1f, 1f, 1f, 0.18f);
                logoImage.raycastTarget = false;
                logoImage.preserveAspect = true;
                logoImage.enabled = false;
                _detailFactionLogoImage = logoImage;
                HavenRecruitsDetailsPanel.
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
                infoLayout.childAlignment = TextAnchor.UpperCenter;
                infoLayout.spacing = 12f;
                infoLayout.childControlWidth = true;
                infoLayout.childControlHeight = false;
                infoLayout.childForceExpandWidth = true;
                infoLayout.childForceExpandHeight = false;

                _detailInfoRoot = infoRootGO.transform;
                _detailInfoRoot.gameObject.SetActive(false);

                var (mainInfoGO, _) = RecruitOverlayManagerHelpers.NewUI("MainInfo", infoRootGO.transform);
                var mainLayout = mainInfoGO.AddComponent<VerticalLayoutGroup>();
                mainLayout.childAlignment = TextAnchor.UpperCenter;
                mainLayout.spacing = 6f;
                mainLayout.childControlWidth = true;
                mainLayout.childControlHeight = false;
                mainLayout.childForceExpandWidth = true;
                mainLayout.childForceExpandHeight = false;

                var (classIconGO, classIconRT) = RecruitOverlayManagerHelpers.NewUI("ClassIcon", mainInfoGO.transform);
                _detailClassIconImage = classIconGO.AddComponent<Image>();
                _detailClassIconImage.preserveAspect = true;
                _detailClassIconImage.raycastTarget = false;
                _detailClassIconImage.enabled = false;
                var classIconLE = classIconGO.AddComponent<LayoutElement>();
                classIconLE.preferredWidth = DetailClassIconSize;
                classIconLE.preferredHeight = DetailClassIconSize;
                classIconRT.sizeDelta = new Vector2(DetailClassIconSize, DetailClassIconSize);

                _detailLevelLabel = HavenRecruitsDetailsPanel.CreateDetailText(mainInfoGO.transform, "Level", TextFontSize + 2, Color.white, TextAnchor.MiddleCenter);
                _detailNameLabel = HavenRecruitsDetailsPanel.CreateDetailText(mainInfoGO.transform, "Name", TextFontSize + 6, Color.white, TextAnchor.MiddleCenter);

                var (factionInfoGO, _) = RecruitOverlayManagerHelpers.NewUI("FactionInfo", infoRootGO.transform);
                var factionLayout = factionInfoGO.AddComponent<VerticalLayoutGroup>();
                factionLayout.childAlignment = TextAnchor.UpperCenter;
                factionLayout.spacing = 4f;
                factionLayout.childControlWidth = true;
                factionLayout.childControlHeight = false;
                factionLayout.childForceExpandWidth = true;
                factionLayout.childForceExpandHeight = false;

                var (factionIconGO, factionIconRT) = RecruitOverlayManagerHelpers.NewUI("FactionIcon", factionInfoGO.transform);
                _detailFactionIconImage = factionIconGO.AddComponent<Image>();
                _detailFactionIconImage.preserveAspect = true;
                _detailFactionIconImage.raycastTarget = false;
                _detailFactionIconImage.enabled = false;
                var factionIconLE = factionIconGO.AddComponent<LayoutElement>();
                factionIconLE.preferredWidth = DetailFactionIconSize;
                factionIconLE.preferredHeight = DetailFactionIconSize;
                factionIconRT.sizeDelta = new Vector2(DetailFactionIconSize, DetailFactionIconSize);

                _detailHavenLabel = HavenRecruitsDetailsPanel.CreateDetailText(factionInfoGO.transform, "HavenName", TextFontSize + 2, Color.white, TextAnchor.MiddleCenter);

                _detailAbilityDescriptionGroup = CreateAbilityDescriptionGroup(infoRootGO.transform, "Key Abilities", out _detailAbilityDescriptionRoot);
                _detailMutationGroup = HavenRecruitsDetailsPanel.CreateDetailGroup(infoRootGO.transform, "Mutation Traits", out _detailMutationRoot);
                _detailCostGroup = HavenRecruitsDetailsPanel.CreateDetailGroup(infoRootGO.transform, "Recruitment Cost", out _detailCostRoot);

                if (_detailAbilityDescriptionGroup != null)
                {
                    _detailAbilityDescriptionGroup.SetActive(false);
                }
                if (_detailMutationGroup != null)
                {
                    _detailMutationGroup.SetActive(false);
                }
                if (_detailCostGroup != null)
                {
                    _detailCostGroup.SetActive(false);
                }

                if (_detailCostRoot != null)
                {
                    var costLayout = _detailCostRoot.GetComponent<HorizontalLayoutGroup>();
                    if (costLayout != null)
                    {
                        costLayout.childAlignment = TextAnchor.MiddleLeft;
                    }
                }

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

        private static void PopulateDetailPanel(RecruitAtSite data)
        {
            try
            {
                if (data?.Recruit == null)
                {
                    return;
                }

                if (_detailEmptyState != null)
                {
                    _detailEmptyState.SetActive(false);
                }

                if (_detailInfoRoot != null)
                {
                    _detailInfoRoot.gameObject.SetActive(true);
                }

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
                    _detailNameLabel.text = data.Recruit.GetName() ?? "Unknown Recruit";
                }

                if (_detailHavenLabel != null)
                {
                    string location = data.Site?.LocalizedSiteName;
                    _detailHavenLabel.text = location;
                }


                UpdateDetailPanelFactionVisuals(data);

                var abilityViews = GetSelectedAbilityViews(data.Recruit).ToList();
                PopulateAbilityDescriptions(_detailAbilityDescriptionGroup, _detailAbilityDescriptionRoot, abilityViews);

                var mutationIcons = HavenRecruitsUtils.GetMutatedArmorIcons(data.Recruit).Where(sp => sp != null).ToList();
                PopulateIconGroup(_detailMutationGroup, _detailMutationRoot, mutationIcons, ArmorIconSize);

                PopulateRecruitCost(data);

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
                    tintColor = new Color(factionColor.r, factionColor.g, factionColor.b, 0.22f);
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
    }
}