using Base.Input;
using Base.UI;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using PhoenixPoint.Tactical.Entities.Abilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TFTV.TFTVDrills
{
    internal static partial class DrillsUI
    {
        private sealed class DrillTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            private TacticalAbilityDef _ability;
            private bool _isLocked;
            private string _missingRequirements;
            private Transform _tooltipParent;
            private RectTransform _menuRect;
            private RectTransform _selfRect;
            private Canvas _canvas;
            private bool _tooltipVisible;
            private bool _isTooltipOwner;
            private Coroutine _hideRoutine;
            private static GeoRosterAbilityDetailTooltip _sharedTooltip;
            private static Canvas _tooltipCanvas;
            private static bool _tooltipPrimed;
            private static DrillTooltipTrigger _currentTooltipOwner;
            private static readonly Vector3[] TooltipCorners = new Vector3[4];

            private const float TooltipGap = 16f;
            private int _skillPointCost = SwapSpCost;
            private bool _isAcquired;
            public static RectTransform ActiveTooltipRect { get; private set; }

            public void Initialize(TacticalAbilityDef ability, string missingRequirements, bool isLocked, bool isAcquired,
                Transform tooltipParent, RectTransform menuRect, Canvas canvas, int skillPointCost)
            {
                _ability = ability;
                _missingRequirements = missingRequirements;
                _isLocked = isLocked;
                _isAcquired = isAcquired;
                _tooltipParent = tooltipParent;
                _menuRect = menuRect;
                _canvas = canvas;
                _selfRect = transform as RectTransform;
                _skillPointCost = skillPointCost > 0 ? skillPointCost : 0;
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                if (_hideRoutine != null)
                {
                    StopCoroutine(_hideRoutine);
                    _hideRoutine = null;
                }

                if (_tooltipVisible && _isTooltipOwner)
                {
                    PositionTooltip();
                    return;
                }

                ShowTooltip();
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                if (_hideRoutine != null)
                {
                    StopCoroutine(_hideRoutine);
                }

                if (_isTooltipOwner)
                {
                    _isTooltipOwner = false;
                }

                _hideRoutine = StartCoroutine(DelayedHide());
            }

            private void LateUpdate()
            {
                if (_tooltipVisible && _isTooltipOwner)
                {
                    PositionTooltip();
                }
            }

            private void OnDisable()
            {
                if (_hideRoutine != null)
                {
                    StopCoroutine(_hideRoutine);
                    _hideRoutine = null;
                }

                HideTooltip();
            }

            private IEnumerator DelayedHide()
            {
                yield return null;
                yield return new WaitForSecondsRealtime(0.05f);

                if (!ShouldKeepTooltipVisible())
                {
                    HideTooltip();
                }

                _hideRoutine = null;
            }

            private bool ShouldKeepTooltipVisible()
            {
                if (!DrillInputHelper.TryGetCursorScreenPosition(out var pointer))
                {
                    return false;
                }

                Camera camera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null;

                if (_selfRect != null && RectTransformUtility.RectangleContainsScreenPoint(_selfRect, pointer, camera))
                {
                    return true;
                }

                if (_menuRect != null && RectTransformUtility.RectangleContainsScreenPoint(_menuRect, pointer, camera))
                {
                    return true;
                }

                var tooltipRect = ActiveTooltipRect;
                if (tooltipRect != null && tooltipRect.gameObject.activeInHierarchy)
                {
                    Canvas tooltipCanvas = tooltipRect.GetComponentInParent<Canvas>();
                    Camera tooltipCamera = tooltipCanvas != null && tooltipCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? tooltipCanvas.worldCamera : null;
                    if (RectTransformUtility.RectangleContainsScreenPoint(tooltipRect, pointer, tooltipCamera))
                    {
                        return true;
                    }
                }

                return false;
            }

            private void ShowTooltip()
            {
                var tooltip = EnsureTooltip();
                var view = _ability?.ViewElementDef;
                if (tooltip == null || view == null)
                {
                    return;
                }

                if (_currentTooltipOwner != null && _currentTooltipOwner != this)
                {
                    _currentTooltipOwner._isTooltipOwner = false;
                    _currentTooltipOwner._tooltipVisible = false;
                }

                var originalTitle = view.DisplayName1;
                string titleText = $"<color=#FF4C00>{originalTitle.Localize()}</color>";
                LocalizedTextBind temporaryTitle = new LocalizedTextBind(titleText, true);
                view.DisplayName1 = temporaryTitle;

                var originalDescription = view.Description;
                LocalizedTextBind temporaryDescription = null;

                string descriptionText = originalDescription?.Localize() ?? string.Empty;
                List<string> extraSections = null;

                if (_isLocked && !string.IsNullOrEmpty(_missingRequirements))
                {
                    if (extraSections == null)
                        extraSections = new List<string>();
                    extraSections.Add($"<color=#E21515><b>Missing requirements:</b>\n{_missingRequirements}</color>");
                }

                if (_isAcquired)
                {
                    if(extraSections==null) extraSections = new List<string>();
                    extraSections.Add("<color=#FF4C00><b>Already acquired</b></color>");
                }

                if (extraSections != null && extraSections.Count > 0)
                {

                    if (!string.IsNullOrEmpty(descriptionText))
                    {
                        descriptionText += "\n\n";
                    }

                    descriptionText += string.Join("\n\n", extraSections);
                    temporaryDescription = new LocalizedTextBind(descriptionText, true);

                    view.Description = temporaryDescription;
                }

                bool shouldPrime = !_tooltipPrimed;
                if (shouldPrime)
                {
                    _tooltipPrimed = true;
                }

                try
                {
                    tooltip.Show(_ability, view, useMutagens: false, cost: _skillPointCost);
                    if (shouldPrime)
                    {
                        tooltip.Hide();
                        tooltip.Show(_ability, view, useMutagens: false, cost: _skillPointCost);
                    }

                    ApplyTooltipCostOverrides(tooltip);
                    tooltip.transform.SetAsLastSibling();
                    ActiveTooltipRect = tooltip.transform as RectTransform;
                    PositionTooltip();
                    _tooltipVisible = true;
                    _isTooltipOwner = true;
                    _currentTooltipOwner = this;
                }
                finally
                {
                    if (temporaryDescription != null)
                    {
                        view.Description = originalDescription;
                    }

                    if (temporaryTitle != null)
                    {
                        view.DisplayName1 = originalTitle;
                    }
                }
            }

            private void HideTooltip()
            {
                if (!_tooltipVisible)
                {
                    return;
                }

                var tooltip = EnsureTooltip();
                tooltip?.Hide();

                if (ActiveTooltipRect != null && tooltip != null && tooltip.transform == ActiveTooltipRect.transform)
                {
                    ActiveTooltipRect = null;
                }

                _tooltipVisible = false;

                if (_isTooltipOwner)
                {
                    _isTooltipOwner = false;
                }
                if (_currentTooltipOwner == this)
                {
                    _currentTooltipOwner = null;
                }
            }

            private void PositionTooltip()
            {
                var tooltip = EnsureTooltip();
                if (tooltip == null || !tooltip.gameObject.activeInHierarchy)
                {
                    return;
                }

                if (!(tooltip.transform is RectTransform rectTransform))
                {
                    return;
                }

                var overlayCanvas = _menuRect != null ? _menuRect.GetComponentInParent<Canvas>() : null;
                if (_tooltipParent != null && tooltip.transform.parent != _tooltipParent)
                {
                    tooltip.transform.SetParent(_tooltipParent, false);
                    _tooltipCanvas = null;
                }

                var canvas = _tooltipCanvas;
                if (canvas == null)
                {
                    canvas = tooltip.GetComponentInParent<Canvas>();
                    _tooltipCanvas = canvas;
                }

                if (overlayCanvas != null && canvas != overlayCanvas)
                {
                    tooltip.transform.SetParent(overlayCanvas.transform, false);
                    canvas = overlayCanvas;
                    _tooltipCanvas = canvas;
                }

                if (canvas == null || !(canvas.transform is RectTransform canvasRect))
                {
                    return;
                }

                Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

                if (_menuRect == null)
                {
                    return;
                }

                _menuRect.GetWorldCorners(TooltipCorners);
                Vector3 overlayTopLeft = TooltipCorners[1];
                Vector2 overlayTopLeftScreen = RectTransformUtility.WorldToScreenPoint(camera, overlayTopLeft);
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, overlayTopLeftScreen, camera, out var localPoint))
                {
                    rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    rectTransform.pivot = new Vector2(1f, 1f);
                    rectTransform.anchoredPosition = new Vector2(localPoint.x - TooltipGap, localPoint.y);
                }
            }

            private void ApplyTooltipCostOverrides(GeoRosterAbilityDetailTooltip tooltip)
            {
                if (tooltip == null)
                {
                    return;
                }

                if (tooltip.AbilitySkillCostText != null && tooltip.AbilitySkillCostText.transform?.parent != null)
                {
                    tooltip.AbilitySkillCostText.transform.parent.gameObject.SetActive(true);
                    string spFormat = tooltip.SPCostTextKey != null ? tooltip.SPCostTextKey.Localize() : "Skill Points";
                    if (tooltip.SkillCostHeaderText != null)
                    {
                        tooltip.SkillCostHeaderText.text = spFormat;
                    }

                    string spPattern = !string.IsNullOrEmpty(tooltip.SPCostPattern) ? tooltip.SPCostPattern : "{0}";
                    tooltip.AbilitySkillCostText.text = string.Format(spPattern, _skillPointCost);
                }

                if (_ability == null)
                {
                    return;
                }

                int apCost = Mathf.RoundToInt(_ability.ActionPointCost);
                int wpCost = Mathf.RoundToInt(_ability.WillPointCost);

                if (tooltip.AbilitySkillCostGroup != null)
                {
                    tooltip.AbilitySkillCostGroup.SetActive(apCost > 0 || wpCost > 0);
                }

                if (tooltip.AbilitySkillAPCostText != null)
                {
                    tooltip.AbilitySkillAPCostText.gameObject.SetActive(apCost > 0);
                    if (apCost > 0)
                    {
                        string apFormat = tooltip.APCostTextKey != null ? tooltip.APCostTextKey.Localize() : "{0}";
                        tooltip.AbilitySkillAPCostText.text = string.Format(apFormat, apCost);
                    }
                }

                if (tooltip.AbilitySkillWPCostText != null)
                {
                    tooltip.AbilitySkillWPCostText.gameObject.SetActive(wpCost > 0);
                    if (wpCost > 0)
                    {
                        string wpFormat = tooltip.WPCostTextKey != null ? tooltip.WPCostTextKey.Localize() : "{0}";
                        tooltip.AbilitySkillWPCostText.text = string.Format(wpFormat, wpCost);
                    }
                }
            }

            private GeoRosterAbilityDetailTooltip EnsureTooltip()
            {
                try
                {
                    if (_sharedTooltip == null)
                    {
                        var template = Resources.FindObjectsOfTypeAll<GeoRosterAbilityDetailTooltip>().FirstOrDefault();
                        if (template == null)
                        {
                            return null;
                        }

                        Transform parent = _tooltipParent != null ? _tooltipParent : template.transform.parent;
                        var clone = UnityEngine.Object.Instantiate(template.gameObject, parent, worldPositionStays: false);
                        clone.name = "TFTV_DrillAbilityTooltip";
                        clone.SetActive(false);
                        _sharedTooltip = clone.GetComponent<GeoRosterAbilityDetailTooltip>();
                        _tooltipCanvas = null;
                    }

                    if (_tooltipParent != null && _sharedTooltip.transform.parent != _tooltipParent)
                    {
                        _sharedTooltip.transform.SetParent(_tooltipParent, false);
                        _tooltipCanvas = null;
                    }

                    return _sharedTooltip;
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    return null;
                }
            }

            public static bool IsSharedTooltip(GeoRosterAbilityDetailTooltip tooltip)
            {
                return tooltip != null && _sharedTooltip == tooltip;
            }
        }

        private static class TooltipSuppressor
        {
            private static readonly List<GeoRosterAbilityDetailTooltip> TooltipCache = new List<GeoRosterAbilityDetailTooltip>();
            private static bool _cacheInitialized;

            internal sealed class Handle : IDisposable
            {
                private readonly List<GameObject> _disabledTooltips;
                private bool _disposed;

                internal Handle(List<GameObject> disabledTooltips)
                {
                    _disabledTooltips = disabledTooltips;
                }

                public void Dispose()
                {
                    if (_disposed)
                    {
                        return;
                    }

                    foreach (var go in _disabledTooltips)
                    {
                        if (go != null)
                        {
                            go.SetActive(true);
                        }
                    }

                    _disabledTooltips.Clear();
                    _disposed = true;
                }
            }

            public static Handle Begin()
            {
                RefreshCacheIfNeeded();

                var disabled = new List<GameObject>();
                foreach (var tooltip in TooltipCache)
                {
                    if (tooltip == null || DrillTooltipTrigger.IsSharedTooltip(tooltip))
                    {
                        continue;
                    }

                    tooltip.Hide();

                    if (tooltip.gameObject.activeSelf)
                    {
                        tooltip.gameObject.SetActive(false);
                        disabled.Add(tooltip.gameObject);
                    }
                }

                return new Handle(disabled);
            }

            private static void RefreshCacheIfNeeded()
            {
                bool needsRefresh = !_cacheInitialized;

                for (int i = TooltipCache.Count - 1; i >= 0; i--)
                {
                    if (TooltipCache[i] == null)
                    {
                        TooltipCache.RemoveAt(i);
                        needsRefresh = true;
                    }
                }

                if (!needsRefresh)
                {
                    return;
                }

                TooltipCache.Clear();
                TooltipCache.AddRange(Resources.FindObjectsOfTypeAll<GeoRosterAbilityDetailTooltip>().Where(t => t != null));
                _cacheInitialized = true;
            }
        }
    }
}
