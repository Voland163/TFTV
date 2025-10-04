using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TFTV.TFTVHavenRecruitsUI
{
    internal sealed class HavenRecruitAbilityTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private HavenRecruitsUtils.AbilityIconData _abilityData;
        private bool _tooltipVisible;
        private static bool _tooltipPrimed;
        private static GeoRosterAbilityDetailTooltip _sharedTooltip;
        private static Canvas _tooltipCanvas;

        private GeoRosterAbilityDetailTooltip _tooltip;

        private const float TooltipHorizontalPadding = 290f;
        private const float TooltipVerticalPadding = 80f;
        private static readonly Vector3[] OverlayCornerBuffer = new Vector3[4];

        public void Initialize(HavenRecruitsUtils.AbilityIconData data)
        {
            _abilityData = data;
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
            if (tooltip == null || (_abilityData.Slot == null && _abilityData.View == null))
            {
                return;
            }

            bool shouldPrimeTooltip = !_tooltipPrimed;
            if (shouldPrimeTooltip)
            {
                _tooltipPrimed = true;
            }

            tooltip.Show(_abilityData.Slot, _abilityData.View, useMutagens: false, cost: _abilityData.SkillPointCost);

            if (shouldPrimeTooltip)
            {
                tooltip.Hide();
                tooltip.Show(_abilityData.Slot, _abilityData.View, useMutagens: false, cost: _abilityData.SkillPointCost);
            }

            tooltip.Show(_abilityData.Slot, _abilityData.View, useMutagens: false, cost: _abilityData.SkillPointCost);
            tooltip.transform.SetAsLastSibling();
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
            tooltip?.Hide();
            _tooltipVisible = false;
        }

        private void UpdateTooltipPosition(PointerEventData eventData)
        {
            var tooltip = EnsureTooltip();
            if (tooltip == null)
            {
                return;
            }

            if (!tooltip.gameObject.activeInHierarchy)
            {
                return;
            }

            var rectTransform = tooltip.transform as RectTransform;
            if (rectTransform == null)
            {
                return;
            }

            var canvas = _tooltipCanvas;
            if (canvas == null)
            {
                _tooltipCanvas = tooltip.GetComponentInParent<Canvas>();
                canvas = _tooltipCanvas;
            }
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
            TFTVLogger.Always($"canvas rect xMin: {canvasRect.rect.xMin} TooltipHorizontalPadding: {TooltipHorizontalPadding}");
            float anchoredX = canvasRect.rect.xMin + TooltipHorizontalPadding;
            TFTVLogger.Always($"{anchoredX}");
            var overlayRect = HavenRecruitsMain.OverlayRootRect;
            if (overlayRect != null)
            {
                overlayRect.GetWorldCorners(OverlayCornerBuffer);
                var overlayLeftScreen = RectTransformUtility.WorldToScreenPoint(referenceCamera, OverlayCornerBuffer[0]);

                rectTransform.anchoredPosition = new Vector2(anchoredX, anchoredY);
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, overlayLeftScreen, referenceCamera, out var overlayLeftLocal))
                {
                    TFTVLogger.Always($"overlayLeftLocal.x: {overlayLeftLocal.x} anchoredX: {anchoredX}");
                    float pivotOffset = rectTransform.rect.width * (1f - rectTransform.pivot.x);
                    anchoredX = overlayLeftLocal.x - TooltipHorizontalPadding - pivotOffset;
                }
            }
        }

        private GeoRosterAbilityDetailTooltip EnsureTooltip()
        {
            try
            {
                var overlayCanvas = HavenRecruitsMain.OverlayCanvas;

                if (_tooltip == null)
                {
                    _tooltip = HavenRecruitsMain.RecruitOverlayManager.EnsureOverlayTooltip();
                    _tooltipCanvas = null;

                    if (_tooltip == null)
                    {
                        var template = FindObjectsOfType<GeoRosterAbilityDetailTooltip>().FirstOrDefault();
                        if (template == null)
                        {
                            return null;
                        }


                        var parent = overlayCanvas != null ? overlayCanvas.transform : template.transform.parent;
                        var cloneGO = UnityEngine.Object.Instantiate(template.gameObject, parent, worldPositionStays: false);
                        cloneGO.name = "TFTV_RecruitAbilityTooltip_Fallback";
                        cloneGO.SetActive(false);
                        _tooltip = cloneGO.GetComponent<GeoRosterAbilityDetailTooltip>();
                        _tooltip.transform.localScale = Vector3.one * 0.5f;
                        HavenRecruitsMain.RegisterOverlayAbilityTooltip(_tooltip);
                    }
                }

                if (overlayCanvas != null && _tooltip.transform.parent != overlayCanvas.transform)
                {
                    _tooltip.transform.SetParent(overlayCanvas.transform, false);
                    _tooltipCanvas = null;
                }

                if (_tooltipCanvas == null)
                {
                    _tooltipCanvas = _tooltip.GetComponentInParent<Canvas>();
                }

                if (_tooltipCanvas != null && overlayCanvas != null)
                {
                    _tooltipCanvas.overrideSorting = true;
                    if (_tooltipCanvas.sortingOrder <= overlayCanvas.sortingOrder)
                    {
                        _tooltipCanvas.sortingOrder = overlayCanvas.sortingOrder + 1;
                    }
                }

                return _tooltip;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                return null;
            }
        }

        internal static void ResetCache()
        {
            _tooltipCanvas = null;
        }
    }
}

