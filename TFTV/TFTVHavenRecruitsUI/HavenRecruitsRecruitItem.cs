using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static TFTV.HavenRecruitsMain;
using static TFTV.HavenRecruitsMain.RecruitOverlayManager;
using static TFTV.TFTVHavenRecruitsUI.HavenRecruitsUtils;

namespace TFTV.TFTVHavenRecruitsUI
{
    internal class HavenRecruitsRecruitItem
    {
        internal static RecruitCardView EnsureRecruitCard(Transform parent, RecruitAtSite data)

        {
            if (parent == null)
            {
                return null;
            }

            var cardName = data?.Recruit?.GetName();
            var card = new GameObject($"Recruit_{cardName ?? "Unknown"}", typeof(RectTransform));
            var rect = card.GetComponent<RectTransform>();
            rect.SetParent(parent, false);

            var cardView = card.AddComponent<RecruitCardView>();
            cardView.RectTransform = rect;
         
            var bg = card.AddComponent<Image>();
            bg.color = CardBackgroundColor;

            var border = card.AddComponent<Outline>();
            border.effectColor = CardBorderColor;
            border.effectDistance = new Vector2(2f, 2f);
            border.useGraphicAlpha = false;

     
            var btn = card.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;

            // click handler: single = focus; double = your hook
            var click = card.AddComponent<CardClickHandler>();

            cardView.ClickHandler = click;

            var layout = card.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 15f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(4, 6, 4, 4);

 
            var fit = card.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var iconImage = RecruitOverlayManagerHelpers.MakeFixedIcon(card.transform, null, ClassIconSize);
            if (iconImage != null)
            {
                cardView.ClassIconImage = iconImage;
                cardView.ClassIconDefaultColor = iconImage.color;
            }

            var (levelGO, levelRT) = RecruitOverlayManagerHelpers.NewUI("Level", card.transform);
            var levelText = levelGO.AddComponent<Text>();
            levelText.font = PuristaSemibold ? PuristaSemibold : Resources.GetBuiltinResource<Font>("Arial.ttf");
            levelText.fontSize = TextFontSize;
            levelText.color = Color.white;
            levelText.alignment = TextAnchor.MiddleLeft;
            levelText.text = string.Empty;
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
            nameText.text = string.Empty;
            nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
            nameText.verticalOverflow = VerticalWrapMode.Truncate;
            cardView.NameLabel = nameText;
            cardView.NameDefaultColor = nameText.color;
            nameRT.pivot = new Vector2(0f, 0.5f);
            nameRT.anchorMin = new Vector2(0f, 0.5f);
            nameRT.anchorMax = new Vector2(0f, 0.5f);
            nameRT.anchoredPosition = Vector2.zero;

            var nameFitter = nameGO.AddComponent<ContentSizeFitter>();
            nameFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            nameFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var (abilitiesGO, abilitiesRT) = RecruitOverlayManagerHelpers.NewUI("Abilities", card.transform);
            var abilitiesTransform = abilitiesGO.transform;
            var abilitiesLE = abilitiesGO.AddComponent<LayoutElement>();
            abilitiesLE.ignoreLayout = true;
            abilitiesRT.anchorMin = new Vector2(1f, 0.5f);
            abilitiesRT.anchorMax = new Vector2(1f, 0.5f);
            abilitiesRT.pivot = new Vector2(1f, 0.5f);
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

            abilitiesGO.SetActive(false);
            cardView.AbilityContainer = abilitiesTransform;
            cardView.AbilityContainerRect = abilitiesRT;

            GameObject costRow = null;
            if (data?.Haven != null && data.Haven.Site?.GeoLevel?.PhoenixFaction != null)
            {

                costRow = HavenRecruitsPrice.CreateCostRow(card.transform, data.Haven, data.Haven.Site.GeoLevel.PhoenixFaction, cardView);
            }
            if (costRow == null)
            {
                var (row, _) = RecruitOverlayManagerHelpers.NewUI("Row_Cost", card.transform);
                var h = row.AddComponent<HorizontalLayoutGroup>();
                h.childAlignment = TextAnchor.MiddleRight;
                h.spacing = 0f;
                h.childControlWidth = true;
                h.childControlHeight = true;
                h.childForceExpandWidth = false;
                h.childForceExpandHeight = false;
                h.padding = new RectOffset(0, 0, 0, 0);
                costRow = row;
            }

            var costLE = costRow.GetComponent<LayoutElement>() ?? costRow.AddComponent<LayoutElement>();
            costLE.minWidth = 0f;
            costLE.preferredWidth = 0f;
            costLE.flexibleWidth = 1f;

            cardView.CostRoot = costRow.transform;
            abilitiesTransform.SetAsLastSibling();

            return cardView;
        }
        internal static GameObject CreateEmptyLabel(Transform parent, string msg)
        {
            if (parent == null)
            {
                return null;
            }

            Transform existing = null;
            for (int i = 0; i < parent.childCount; i++)
            {

                var child = parent.GetChild(i);
                if (child != null && child.name == "Empty")
                {
                    existing = child;
                    break;
                }
            }
            if (existing != null)
            {
                var text = existing.GetComponent<Text>();
                if (text != null)
                {
                    text.text = msg;
                }
                existing.SetAsLastSibling();
                existing.gameObject.SetActive(true);
                return existing.gameObject;
            }

            var go = new GameObject("Empty", typeof(RectTransform));

            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.text = msg;
            t.font = PuristaSemibold ? PuristaSemibold : Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.fontSize = TextFontSize;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(0.85f, 0.85f, 0.9f, 0.9f);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 48);
            return go;
        }
    }
}

