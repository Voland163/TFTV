using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TFTV.TFTVHavenRecruitsUI
{
    internal class HavenRecruitItemsTooltip
    {
        internal sealed class HavenRecruitMutationTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            private HavenRecruitsUtils.MutationIconData _mutationData;
            private bool _tooltipVisible;

            private static GeoRosterAbilityDetailTooltip _sharedTooltip;
            private static Canvas _tooltipCanvas;

            private GeoRosterAbilityDetailTooltip _tooltip;

            public void Initialize(HavenRecruitsUtils.MutationIconData data)
            {
                _mutationData = data;
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
                if (tooltip == null || _mutationData.View == null)
                {
                    return;
                }

                tooltip.Show(abilityDef: null, _mutationData.View, useMutagens: false, cost: 0);
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

                var canvas = _tooltipCanvas ?? (_tooltipCanvas = tooltip.GetComponentInParent<Canvas>());
                if (canvas == null)
                {
                    return;
                }

                if (!(canvas.transform is RectTransform canvasRect))
                {
                    return;
                }

                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    eventData.position,
                    canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                    out Vector2 localPoint);

                rectTransform.anchoredPosition = localPoint;
            }

            private GeoRosterAbilityDetailTooltip EnsureTooltip()
            {
                try
                {
                    if (_tooltip == null)
                    {
                        if (_sharedTooltip != null)
                        {
                            _tooltip = _sharedTooltip;
                        }
                        else
                        {
                            _tooltip = FindObjectsOfType<GeoRosterAbilityDetailTooltip>().FirstOrDefault();
                            if (_tooltip == null)
                            {
                                return null;
                            }

                            _sharedTooltip = _tooltip;
                        }
                    }

                    var overlayCanvas = HavenRecruitsMain.OverlayCanvas;
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
        }
    }
}

