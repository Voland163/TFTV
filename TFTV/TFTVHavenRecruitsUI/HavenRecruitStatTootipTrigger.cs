using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static TFTV.HavenRecruitsMain;

namespace TFTV.TFTVHavenRecruitsUI
{
    internal sealed class HavenRecruitStatTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private string _tooltipText = string.Empty;
        private bool _tooltipVisible;

        private static RectTransform _tooltipRect;
        private static Text _tooltipLabel;
        private static CanvasGroup _tooltipCanvasGroup;
        private static Canvas _tooltipCanvas;

        private const float TooltipHorizontalPadding = 290f;
        private const float TooltipVerticalPadding = 80f;
        private const float TooltipMaxWidth = 210f;
        private const float TooltipScale = 0.5f;

        private static readonly Vector3[] OverlayCornerBuffer = new Vector3[4];

        internal void SetTooltipText(string tooltipText)
        {
            _tooltipText = tooltipText ?? string.Empty;

            if (_tooltipVisible)
            {
                UpdateTooltipContent();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ShowTooltip(eventData);
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (_tooltipVisible)
            {
                UpdateTooltipPosition(eventData);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HideTooltip();
        }

        private void OnDisable()
        {
            HideTooltip();
        }

        private void ShowTooltip(PointerEventData eventData)
        {
            var tooltip = EnsureTooltip();
            if (tooltip == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_tooltipText))
            {
                return;
            }

            UpdateTooltipContent();

            tooltip.gameObject.SetActive(true);
            if (_tooltipCanvasGroup != null)
            {
                _tooltipCanvasGroup.alpha = 1f;
            }

            UpdateTooltipPosition(eventData);
            _tooltipVisible = true;
        }

        private void HideTooltip()
        {
            if (!_tooltipVisible)
            {
                return;
            }

            var tooltip = EnsureTooltip();
            if (tooltip != null)
            {
                if (_tooltipCanvasGroup != null)
                {
                    _tooltipCanvasGroup.alpha = 0f;
                }
                tooltip.gameObject.SetActive(false);
            }

            _tooltipVisible = false;
        }

        private void UpdateTooltipContent()
        {
            var tooltip = EnsureTooltip();
            if (tooltip == null)
            {
                return;
            }

            if (_tooltipLabel != null)
            {
                _tooltipLabel.text = _tooltipText;
                LayoutRebuilder.ForceRebuildLayoutImmediate(_tooltipLabel.rectTransform);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltip);
        }

        private void UpdateTooltipPosition(PointerEventData eventData)
        {
            var tooltip = EnsureTooltip();
            if (tooltip == null)
            {
                return;
            }

            if (_tooltipCanvas == null)
            {
                _tooltipCanvas = tooltip.GetComponentInParent<Canvas>();
            }

            var canvas = _tooltipCanvas;
            if (canvas == null)
            {
                return;
            }

            if (!(canvas.transform is RectTransform canvasRect))
            {
                return;
            }

            var referenceCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                eventData.position,
                referenceCamera,
                out Vector2 localPoint);

            float anchoredY = localPoint.y + TooltipVerticalPadding;
            float anchoredX = canvasRect.rect.xMin + TooltipHorizontalPadding;

            var overlayRect = HavenRecruitsMain.OverlayRootRect;
            if (overlayRect != null)
            {
                overlayRect.GetWorldCorners(OverlayCornerBuffer);
                var overlayLeftScreen = RectTransformUtility.WorldToScreenPoint(referenceCamera, OverlayCornerBuffer[0]);
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, overlayLeftScreen, referenceCamera, out var overlayLeftLocal))
                {
                    anchoredX = overlayLeftLocal.x + TooltipHorizontalPadding;
                }
            }

            tooltip.anchoredPosition = new Vector2(anchoredX, anchoredY);

            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltip);

            float tooltipHeight = tooltip.rect.height;
            float topLimit = canvasRect.rect.yMax - TooltipVerticalPadding;
            float bottomLimit = canvasRect.rect.yMin + TooltipVerticalPadding + tooltipHeight;

            if (bottomLimit > topLimit)
            {
                bottomLimit = topLimit;
            }

            float clampedY = Mathf.Clamp(tooltip.anchoredPosition.y, bottomLimit, topLimit);

            tooltip.anchoredPosition = new Vector2(anchoredX, clampedY);
        }

        private static RectTransform EnsureTooltip()
        {
            if (_tooltipRect != null)
            {
                return _tooltipRect;
            }

            try
            {
                var canvas = HavenRecruitsMain.OverlayCanvas;
                if (canvas == null)
                {
                    return null;
                }

                var (tooltipGO, tooltipRect) = RecruitOverlayManagerHelpers.NewUI("TFTV_RecruitStatTooltip", canvas.transform);
                tooltipRect.anchorMin = new Vector2(0f, 1f);
                tooltipRect.anchorMax = new Vector2(0f, 1f);
                tooltipRect.pivot = new Vector2(0f, 1f);
                tooltipRect.anchoredPosition = Vector2.zero;
                tooltipRect.offsetMin = Vector2.zero;
                tooltipRect.offsetMax = Vector2.zero;
                tooltipRect.localScale = Vector3.one * TooltipScale;

                var background = tooltipGO.AddComponent<Image>();
                background.color = new Color(0f, 0f, 0f, 0.92f);
                background.raycastTarget = false;

                _tooltipCanvasGroup = tooltipGO.AddComponent<CanvasGroup>();
                _tooltipCanvasGroup.alpha = 0f;

                var layout = tooltipGO.AddComponent<VerticalLayoutGroup>();
                layout.childAlignment = TextAnchor.UpperLeft;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
                layout.padding = new RectOffset(16, 16, 12, 12);
                layout.spacing = 4f;

                var fitter = tooltipGO.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                var (labelGO, _) = RecruitOverlayManagerHelpers.NewUI("Label", tooltipGO.transform);
                _tooltipLabel = labelGO.AddComponent<Text>();
                _tooltipLabel.font = PuristaSemibold ? PuristaSemibold : Resources.GetBuiltinResource<Font>("Arial.ttf");
                _tooltipLabel.fontSize = TextFontSize;
                _tooltipLabel.color = Color.white;
                _tooltipLabel.alignment = TextAnchor.UpperLeft;
                _tooltipLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
                _tooltipLabel.verticalOverflow = VerticalWrapMode.Overflow;
                _tooltipLabel.supportRichText = true;
                _tooltipLabel.raycastTarget = false;

                var labelLayout = labelGO.AddComponent<LayoutElement>();
                labelLayout.preferredWidth = TooltipMaxWidth;
                labelLayout.flexibleWidth = 0f;

                tooltipGO.SetActive(false);

                _tooltipCanvas = canvas;
                _tooltipRect = tooltipRect;

                return _tooltipRect;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                return null;
            }
        }
    }
}

