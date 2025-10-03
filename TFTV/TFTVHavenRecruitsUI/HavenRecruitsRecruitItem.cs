using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static TFTV.HavenRecruitsMain;
using static TFTV.TFTVHavenRecruitsUI.HavenRecruitsUtils;
using static TFTV.HavenRecruitsMain.RecruitOverlayManager;

namespace TFTV.TFTVHavenRecruitsUI
{
    internal class HavenRecruitsRecruitItem
    {
        internal static void CreateRecruitItem(Transform parent, RecruitAtSite data, bool collapse)

        {
            _ = collapse;

            var card = new GameObject($"Recruit_{data.Recruit?.GetName()}");
            card.transform.SetParent(parent, false);

            var cardView = card.AddComponent<RecruitCardView>();

            // background
            var bg = card.AddComponent<Image>();
            bg.color = CardBackgroundColor;

            var border = card.AddComponent<Outline>();
            border.effectColor = CardBorderColor;
            border.effectDistance = new Vector2(2f, 2f);
            border.useGraphicAlpha = false;

            // button (keep it for hover/tint states, but don't use onClick directly)
            var btn = card.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;

            // click handler: single = focus; double = your hook
            var click = card.AddComponent<CardClickHandler>();
            click.OnSingle = () =>
            {
                HavenRecruitsGeoscapeInteractions.FocusOnSite(data.Site);
                HandleRecruitSelected(card, data);
            };
            click.OnDouble = () =>
            {
                HandleRecruitSelected(card, data);
                OnCardDoubleClick?.Invoke(data.Recruit, data.Site);
            };

            var layout = card.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 15f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(4, 6, 4, 4);

            // Let height fit content (no fixed height anymore)
            var fit = card.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var classIcon = GetClassIcon(data.Recruit);
            if (classIcon != null)
            {
                var iconImage = RecruitOverlayManagerHelpers.MakeFixedIcon(card.transform, classIcon, ClassIconSize);
                if (iconImage != null)
                {
                    cardView.ClassIconImage = iconImage;
                    cardView.ClassIconDefaultColor = iconImage.color;
                }
            }

            var (levelGO, levelRT) = RecruitOverlayManagerHelpers.NewUI("Level", card.transform);
            var levelText = levelGO.AddComponent<Text>();
            levelText.font = PuristaSemibold ? PuristaSemibold : Resources.GetBuiltinResource<Font>("Arial.ttf");
            levelText.fontSize = TextFontSize;
            levelText.color = Color.white;
            levelText.alignment = TextAnchor.MiddleLeft;
            levelText.text = $"{data.Recruit?.Level ?? 0}";
            cardView.LevelLabel = levelText;
            cardView.LevelDefaultColor = levelText.color;
            levelRT.pivot = new Vector2(0f, 0.5f);
            levelRT.anchorMin = new Vector2(0f, 0.5f);
            levelRT.anchorMax = new Vector2(0f, 0.5f);
            levelRT.anchoredPosition = Vector2.zero;

            var levelSizeFitter = levelGO.AddComponent<ContentSizeFitter>();
            levelSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            levelSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var (nameGO, nameRT) = RecruitOverlayManagerHelpers.NewUI("Name", card.transform);
            var nameText = nameGO.AddComponent<Text>();
            nameText.font = PuristaSemibold ? PuristaSemibold : Resources.GetBuiltinResource<Font>("Arial.ttf");
            nameText.fontSize = TextFontSize;
            nameText.color = Color.white;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.text = data.Recruit?.GetName() ?? "Unknown Recruit";
            cardView.NameLabel = nameText;
            cardView.NameDefaultColor = nameText.color;
            nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
            nameText.verticalOverflow = VerticalWrapMode.Truncate;
            nameRT.pivot = new Vector2(0f, 0.5f);
            nameRT.anchorMin = new Vector2(0f, 0.5f);
            nameRT.anchorMax = new Vector2(0f, 0.5f);
            nameRT.anchoredPosition = Vector2.zero;
            var nameFitter = nameGO.AddComponent<ContentSizeFitter>();
            nameFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            nameFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var abilityInfos = GetSelectedAbilityIcons(data.Recruit).ToList();
            var mutationIcons = GetMutationIcons(data.Recruit).ToList();

            Transform abilitiesTransform = null;

            if (abilityInfos.Count > 0 || mutationIcons.Count > 0)
            {
                var (abilitiesGO, abilitiesRT) = RecruitOverlayManagerHelpers.NewUI("Abilities", card.transform);
                abilitiesTransform = abilitiesGO.transform;

                var abilitiesLE = abilitiesGO.AddComponent<LayoutElement>();
                abilitiesLE.ignoreLayout = true;

                abilitiesRT.anchorMin = new Vector2(0.5f, 0.5f);
                abilitiesRT.anchorMax = new Vector2(0.5f, 0.5f);

                // Plan (pseudocode):
                // 1. Anchor the abilities container to the RIGHT side of its parent so it aligns to the right edge.
                // 2. Use a pivot of (1, 0.5) so positioning is calculated from the right edge.
                // 3. Provide a negative anchoredPosition.x to move the container leftwards by a "considerable offset".
                // 4. Keep other layout settings the same so the icons still size and layout correctly.
                // 5. Minimal change: replace the existing anchor/pivot/position lines with right-aligned equivalents.

                abilitiesRT.anchorMin = new Vector2(1f, 0.5f);
                abilitiesRT.anchorMax = new Vector2(1f, 0.5f);
                abilitiesRT.pivot = new Vector2(1f, 0.5f);
                // Move left from the right edge by a considerable offset. Adjust the multiplier or added value as needed.
                abilitiesRT.anchoredPosition = new Vector2(AbilityIconsCenterOffsetPx, 0f);

                var abilitiesLayout = abilitiesGO.AddComponent<HorizontalLayoutGroup>();
                abilitiesLayout.childAlignment = TextAnchor.MiddleCenter;
                abilitiesLayout.spacing = 20f;
                abilitiesLayout.childControlWidth = true;
                abilitiesLayout.childControlHeight = true;
                abilitiesLayout.childForceExpandWidth = false;
                abilitiesLayout.childForceExpandHeight = false;

                var abilitiesFitter = abilitiesGO.AddComponent<ContentSizeFitter>();
                abilitiesFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                abilitiesFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                foreach (var icon in mutationIcons)
                {
                    if (icon.Icon == null)
                    {
                        continue;
                    }
                    RecruitOverlayManagerHelpers.MakeMutationIcon(abilitiesGO.transform, icon, ArmorIconSize);
                }

                foreach (var ability in abilityInfos)
                {
                    if (ability.Icon == null)
                    {
                        continue;
                    }

                    var iconImage = RecruitOverlayManagerHelpers.MakeFixedIcon(abilitiesGO.transform, ability.Icon, AbilityIconSize, _abilityIconBackground);
                    if (iconImage == null)
                    {
                        continue;
                    }

                    iconImage.raycastTarget = true;

                    var tooltipTrigger = iconImage.gameObject.AddComponent<HavenRecruitAbilityTooltipTrigger>();
                    tooltipTrigger.Initialize(ability);
                }


            }

            var costRow = HavenRecruitsPrice.CreateCostRow(card.transform, data.Haven, data.Haven.Site.GeoLevel.PhoenixFaction, cardView);
            if (costRow != null)
            {
                var costLE = costRow.GetComponent<LayoutElement>() ?? costRow.AddComponent<LayoutElement>();
                costLE.minWidth = 0f;
                costLE.preferredWidth = 0f;
                costLE.flexibleWidth = 1f;
            }

            if (abilitiesTransform != null)
            {
                abilitiesTransform.SetAsLastSibling();
            }

        }

        internal static void CreateEmptyLabel(Transform parent, string msg)
        {
            var go = new GameObject("Empty");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.text = msg;
            t.font = PuristaSemibold ? PuristaSemibold : Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.fontSize = TextFontSize;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(0.85f, 0.85f, 0.9f, 0.9f);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 48);
        }
    }
}

