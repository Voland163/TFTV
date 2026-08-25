using Base.UI;
using PhoenixPoint.Tactical.Entities.Abilities;
using System;
using TFTV.TFTVIncidents;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TFTV.TFTVBaseRework
{
    /// <summary>
    /// Hover tooltip for the Affinity icon on a personnel slot: names the Affinity and its rank,
    /// then lists every benefit it grants, base duty included.
    /// </summary>
    internal sealed class AffinityBadgeTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        internal LeaderSelection.AffinityApproach Approach;
        internal int Rank;

        private const string TooltipObjectName = "TFTV_AffinityBadgeTooltip";
        private const float TooltipWidth = 700f;
        private const float TooltipMargin = 8f;
        private const int TooltipFontSize = 26;
        private const int TooltipPadH = 18;
        private const int TooltipPadV = 14;

        private static RectTransform _tooltipRect;
        private static Text _tooltipLabel;

        public void OnPointerEnter(PointerEventData eventData)
        {
            Show();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Hide();
        }

        private void OnDisable()
        {
            Hide();
        }

        private void Show()
        {
            try
            {
                RectTransform tooltip = EnsureTooltip(transform);
                if (tooltip == null)
                {
                    return;
                }

                string content = BuildContent();
                if (string.IsNullOrEmpty(content))
                {
                    Hide();
                    return;
                }

                _tooltipLabel.text = content;
                LayoutRebuilder.ForceRebuildLayoutImmediate(_tooltipLabel.rectTransform);
                LayoutRebuilder.ForceRebuildLayoutImmediate(tooltip);

                PositionNextTo(tooltip, transform as RectTransform);

                tooltip.gameObject.SetActive(true);
                tooltip.SetAsLastSibling();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void Hide()
        {
            if (_tooltipRect != null && _tooltipRect.gameObject.activeSelf)
            {
                _tooltipRect.gameObject.SetActive(false);
            }
        }

        private string BuildContent()
        {
            string header = null;

            PassiveModifierAbilityDef ability = LeaderSelection.GetAffinityAbility(Approach, Rank);
            if (ability?.ViewElementDef?.DisplayName1 != null)
            {
                header = ability.ViewElementDef.DisplayName1.Localize();
            }

            if (string.IsNullOrEmpty(header))
            {
                header = $"{LeaderSelection.GetApproachDisplayName(Approach)} {Rank}";
            }

            string benefitsKey = LeaderSelection.GetAllBenefitsLocalizationKey(Approach);
            string benefits = string.IsNullOrEmpty(benefitsKey)
                ? string.Empty
                : new LocalizedTextBind() { LocalizationKey = benefitsKey }.Localize();

            return string.IsNullOrEmpty(benefits) ? header : $"{header}\n\n{benefits}";
        }

        /// <summary>
        /// Anchors the tooltip to the left of the icon and keeps it inside the canvas.
        /// </summary>
        private static void PositionNextTo(RectTransform tooltip, RectTransform iconRect)
        {
            if (tooltip == null || iconRect == null)
            {
                return;
            }

            Canvas canvas = tooltip.GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas?.transform as RectTransform;
            if (canvasRect == null)
            {
                return;
            }

            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

            Vector3[] corners = new Vector3[4];
            iconRect.GetWorldCorners(corners);
            // corners[1] is the top-left corner of the icon.
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, corners[1]);

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, cam, out Vector2 localPoint))
            {
                return;
            }

            float halfWidth = canvasRect.rect.width * 0.5f;
            float halfHeight = canvasRect.rect.height * 0.5f;
            float tooltipWidth = tooltip.rect.width;
            float tooltipHeight = tooltip.rect.height;

            // Pivot is top-right, so the tooltip hangs down and to the left of the anchor point.
            float x = Mathf.Clamp(localPoint.x, -halfWidth + tooltipWidth + TooltipMargin, halfWidth - TooltipMargin);
            float y = Mathf.Clamp(localPoint.y, -halfHeight + tooltipHeight + TooltipMargin, halfHeight - TooltipMargin);

            tooltip.anchoredPosition = new Vector2(x, y);
        }

        private static RectTransform EnsureTooltip(Transform reference)
        {
            if (_tooltipRect != null)
            {
                return _tooltipRect;
            }

            Canvas canvas = reference.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return null;
            }

            GameObject go = new GameObject(
                TooltipObjectName,
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter),
                typeof(Image));
            go.transform.SetParent(canvas.transform, false);
            // The panel root drives a layout group; the tooltip places itself.
            go.AddComponent<LayoutElement>().ignoreLayout = true;

            _tooltipRect = go.GetComponent<RectTransform>();
            _tooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
            _tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
            _tooltipRect.pivot = new Vector2(1f, 1f);
            _tooltipRect.sizeDelta = new Vector2(TooltipWidth, 0f);

            Image bg = go.GetComponent<Image>();
            bg.color = new Color(0.06f, 0.06f, 0.10f, 0.94f);
            bg.raycastTarget = false;

            VerticalLayoutGroup vlg = go.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(TooltipPadH, TooltipPadH, TooltipPadV, TooltipPadV);
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = go.GetComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject textGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(go.transform, false);

            _tooltipLabel = textGO.GetComponent<Text>();
            _tooltipLabel.font = PersonnelManagementUI.PuristaSemibold
                ?? canvas.GetComponentInChildren<Text>(true)?.font
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            _tooltipLabel.fontSize = TooltipFontSize;
            _tooltipLabel.color = Color.white;
            _tooltipLabel.alignment = TextAnchor.UpperLeft;
            _tooltipLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            _tooltipLabel.verticalOverflow = VerticalWrapMode.Overflow;
            _tooltipLabel.supportRichText = true;
            _tooltipLabel.raycastTarget = false;

            go.SetActive(false);
            return _tooltipRect;
        }
    }
}
