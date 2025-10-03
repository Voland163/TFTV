using Base.Core;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.View.ViewControllers.Inventory;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View.ViewControllers.Inventory;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TFTV.TFTVHavenRecruitsUI
{
    internal static class HavenRecruitItemsTooltip
    {
        internal sealed class HavenRecruitMutationItemTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            private HavenRecruitsUtils.MutationIconData _mutationData;
            private bool _tooltipVisible;

            private static readonly Dictionary<Type, MethodInfo> ShowMethodCache = new Dictionary<Type, MethodInfo>();
            private static readonly Dictionary<Type, MethodInfo> HideMethodCache = new Dictionary<Type, MethodInfo>();

       
            private UIInventoryTooltip _tooltip;
            private Canvas _tooltipCanvas;

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
                if (!_mutationData.HasItem)
                {
                    return;
                }

                var tooltip = EnsureTooltip();
                if (tooltip == null)
                {
                    return;
                }

                var showMethod = GetShowMethod(tooltip.GetType());
                if (showMethod == null)
                {
                    return;
                }

                var args = BuildShowArguments(showMethod, _mutationData.Item);
                showMethod.Invoke(tooltip, args);

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

                try
                {
                    var tooltip = EnsureTooltip();
                    var hideMethod = tooltip != null ? GetHideMethod(tooltip.GetType()) : null;
                    hideMethod?.Invoke(tooltip, Array.Empty<object>());
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                }
                finally
                {
                    _tooltipVisible = false;
                }

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

              
                if (!(tooltip.transform is RectTransform rectTransform))
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

                const float horizontalPadding = 20f;
                float pivotOffset = rectTransform.rect.width * rectTransform.pivot.x;
                float anchoredX = canvasRect.rect.xMin + horizontalPadding + pivotOffset;

                rectTransform.anchoredPosition = new Vector2(anchoredX, localPoint.y);
            }

            private UIInventoryTooltip EnsureTooltip()
            {
                if (_tooltip != null)
                {
                    return _tooltip;
                }

                try
                {

                    var overlayTooltip = HavenRecruitsMain.RecruitOverlayManager.EnsureOverlayItemTooltip();
                    if (overlayTooltip != null)

                    {
                        _tooltip = overlayTooltip;
                        _tooltipCanvas = null;
                        return _tooltip;
                    }

                  

                    var template = FindTooltipTemplate();
                    if (template == null)
                    {
                        return null;
                    }


                    var overlayCanvas = HavenRecruitsMain.OverlayCanvas;
                    var parent = overlayCanvas != null ? overlayCanvas.transform : template.transform.parent;
                    var clone = UnityEngine.Object.Instantiate(template.gameObject, parent, worldPositionStays: false);
                    clone.name = "TFTV_RecruitItemTooltip";
                    clone.SetActive(false);

                    _tooltip = clone.GetComponent<UIInventoryTooltip>();
                    HavenRecruitsMain.RegisterOverlayItemTooltip(_tooltip);
                    _tooltipCanvas = null;


                    return _tooltip;
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    return null;
                }
            }

            private static UIInventoryTooltip FindTooltipTemplate()
            {
                try
                {
                    UIInventoryTooltip template = null;

                    var geoLevel = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
                    var view = geoLevel?.View;
                    if (view != null) 
                    {
                        template = view.GetComponentsInChildren<UIInventoryTooltip>(true)
                          .FirstOrDefault(t => t != null && t.hideFlags == HideFlags.None);
                    }

                    if (template == null)
                    {
                        template = UnityEngine.Object.FindObjectsOfType<UIInventoryTooltip>()
                         .FirstOrDefault(t => t != null && t.hideFlags == HideFlags.None);
                    }
                    return template;
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    return null;
                }
            }

            private static MethodInfo GetShowMethod(Type tooltipType)
            {
                if (tooltipType == null)
                {
                    return null;
                }

                if (ShowMethodCache.TryGetValue(tooltipType, out var cached) && cached != null)
                {
                    return cached;
                }

                var method = tooltipType
                    .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(m =>
                    {
                        if (!string.Equals(m.Name, "Show", StringComparison.Ordinal))
                        {
                            return false;
                        }

                        var parameters = m.GetParameters();
                        if (parameters.Length == 0)
                        {
                            return false;
                        }

                        return typeof(ItemDef).IsAssignableFrom(parameters[0].ParameterType);
                    });

                ShowMethodCache[tooltipType] = method;
                return method;
            }

            private static MethodInfo GetHideMethod(Type tooltipType)
            {
                if (tooltipType == null)
                {
                    return null;
                }

                if (HideMethodCache.TryGetValue(tooltipType, out var cached) && cached != null)
                {
                    return cached;
                }

                var method = tooltipType
                    .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(m => string.Equals(m.Name, "Hide", StringComparison.Ordinal) && m.GetParameters().Length == 0);

                HideMethodCache[tooltipType] = method;
                return method;
            }

            private static object[] BuildShowArguments(MethodInfo method, ItemDef item)
            {
                var parameters = method.GetParameters();
                var args = new object[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    var parameter = parameters[i];
                    var parameterType = parameter.ParameterType;

                    if (i == 0 && parameterType.IsInstanceOfType(item))
                    {
                        args[i] = item;
                        continue;
                    }

                    if (i == 0 && typeof(ItemDef).IsAssignableFrom(parameterType))
                    {
                        args[i] = item;
                        continue;
                    }

                    if (!parameterType.IsValueType)
                    {
                        args[i] = null;
                        continue;
                    }

                    args[i] = Activator.CreateInstance(parameterType);
                }

                return args;
            }
        }

    }
}

