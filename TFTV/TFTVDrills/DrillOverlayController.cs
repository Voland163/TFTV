using Base.Core;
using Base.Input;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV.TFTVDrills
{
    internal static partial class DrillsUI
    {
        private sealed class DrillOverlayController : MonoBehaviour
        {
            private const float HoverPadding = 40f;
            private const float TooltipPadding = 24f;
            private const float Gap = 12f;
            private const float IntroDistance = 40f;
            private const float IntroDuration = 0.18f;
            private const float OutroDuration = 0.14f;

            private static readonly Vector3[] CornerBuffer = new Vector3[4];

            private Canvas _canvas;
            private RectTransform _overlayRect;
            private RectTransform _panelRect;
            private RectTransform _anchorRect;
            private Button _backgroundButton;
            private CanvasGroup _canvasGroup;
            private RectTransform _viewportRect;
            private RectTransform _contentRect;
            private Vector2 _targetPosition;
            private bool _animating;
            private bool _closing;
            private Action _onClosed;
            private InputController _inputController;
            private TooltipSuppressor.Handle _tooltipSuppression;

            public void Initialize(Canvas canvas, RectTransform overlayRect, RectTransform panelRect, RectTransform anchorRect, Button backgroundButton)
            {
                _canvas = canvas;
                _overlayRect = overlayRect;
                _panelRect = panelRect;
                _anchorRect = anchorRect;
                _backgroundButton = backgroundButton;
                _canvasGroup = panelRect != null ? panelRect.GetComponent<CanvasGroup>() : null;
                _inputController = GameUtl.GameComponent<InputController>();
            }

            public void AttachTooltipSuppression(TooltipSuppressor.Handle handle)
            {
                _tooltipSuppression?.Dispose();
                _tooltipSuppression = handle;
            }

            public void ConfigureContent(RectTransform viewportRect, RectTransform contentRect, float width, float maxHeight)
            {
                _viewportRect = viewportRect;
                _contentRect = contentRect;

                if (_panelRect == null)
                {
                    return;
                }

                _panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = 0f;
                }

                if (_contentRect != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRect);
                    float preferred = LayoutUtility.GetPreferredHeight(_contentRect);
                    float height = Mathf.Clamp(preferred, 140f, maxHeight);
                    _panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                    if (_viewportRect != null)
                    {
                        _viewportRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                        _viewportRect.offsetMin = new Vector2(_viewportRect.offsetMin.x, 0f);
                        _viewportRect.offsetMax = new Vector2(_viewportRect.offsetMax.x, 0f);
                    }
                }

                _targetPosition = CalculateTargetPosition();
                _panelRect.anchoredPosition = _targetPosition + new Vector2(IntroDistance, 0f);
                _animating = true;
                StopAllCoroutines();
                StartCoroutine(PlayIntro());
            }

            public void Close(Action onClosed = null)
            {
                if (_closing)
                {
                    return;
                }

                _closing = true;
                _onClosed = onClosed;

                StopAllCoroutines();
                StartCoroutine(PlayOutro());
            }

            private IEnumerator PlayIntro()
            {
                float elapsed = 0f;
                Vector2 start = _panelRect.anchoredPosition;
                Vector2 end = _targetPosition;
                while (elapsed < IntroDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / IntroDuration);
                    t = Mathf.SmoothStep(0f, 1f, t);
                    _panelRect.anchoredPosition = Vector2.Lerp(start, end, t);
                    if (_canvasGroup != null)
                    {
                        _canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                    }
                    yield return null;
                }

                _panelRect.anchoredPosition = end;
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = 1f;
                }
                _animating = false;
            }

            private IEnumerator PlayOutro()
            {
                if (_panelRect == null)
                {
                    yield break;
                }

                Vector2 start = _panelRect.anchoredPosition;
                Vector2 end = start + new Vector2(20f, 0f);
                float startAlpha = _canvasGroup != null ? _canvasGroup.alpha : 1f;
                float elapsed = 0f;

                while (elapsed < OutroDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / OutroDuration);
                    t = Mathf.SmoothStep(0f, 1f, t);
                    _panelRect.anchoredPosition = Vector2.Lerp(start, end, t);
                    if (_canvasGroup != null)
                    {
                        _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                    }
                    yield return null;
                }

                _panelRect.anchoredPosition = end;
                _canvasGroup?.SetAlpha(0f);

                try
                {
                    _onClosed?.Invoke();
                }
                finally
                {
                    var handle = _tooltipSuppression;
                    _tooltipSuppression = null;
                    handle?.Dispose();
                    UnityEngine.Object.Destroy(gameObject);
                }
            }

            private void LateUpdate()
            {
                if (_panelRect == null)
                {
                    return;
                }

                if (_anchorRect == null || !_anchorRect.gameObject.activeInHierarchy)
                {
                    Close();
                    return;
                }

                if (!_closing && !_animating)
                {
                    _targetPosition = CalculateTargetPosition();
                    _panelRect.anchoredPosition = Vector2.Lerp(_panelRect.anchoredPosition, _targetPosition, Time.unscaledDeltaTime * 12f);
                }

                if (_closing)
                {
                    return;
                }

                if (_inputController == null)
                {
                    _inputController = GameUtl.GameComponent<InputController>();
                }

                if (!DrillInputHelper.TryGetCursorScreenPosition(_inputController, out var pointer))
                {
                    return;
                }

                if (!IsPointerNear(pointer))
                {
                    Close();
                }
            }

            private void OnDestroy()
            {
                var handle = _tooltipSuppression;
                _tooltipSuppression = null;
                handle?.Dispose();
            }

            private Vector2 CalculateTargetPosition()
            {
                if (_overlayRect == null || _panelRect == null)
                {
                    return Vector2.zero;
                }

                Vector2 anchorLocal = Vector2.zero;
                if (_anchorRect != null)
                {
                    _anchorRect.GetWorldCorners(CornerBuffer);
                    Vector3 topLeftWorld = CornerBuffer[1];
                    Camera camera = GetCameraForCanvas(_canvas);
                    Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, topLeftWorld);
                    if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_overlayRect, screenPoint, camera, out anchorLocal))
                    {
                        anchorLocal = Vector2.zero;
                    }
                }

                float canvasHalfWidth = _overlayRect.rect.width * 0.5f;
                float canvasHalfHeight = _overlayRect.rect.height * 0.5f;
                float panelHeight = _panelRect.rect.height;

                float minTop = -canvasHalfHeight + panelHeight + 8f;
                float maxTop = canvasHalfHeight - 8f;
                float topEdge = Mathf.Clamp(anchorLocal.y, minTop, maxTop);
                float pivotOffsetY = (1f - _panelRect.pivot.y) * panelHeight;
                float anchoredY = topEdge - pivotOffsetY;

                float desiredRight = anchorLocal.x - Gap;
                float leftEdge = desiredRight - _panelRect.rect.width;
                float minLeft = -canvasHalfWidth + 12f;
                if (leftEdge < minLeft)
                {
                    desiredRight += minLeft - leftEdge;
                }

                return new Vector2(desiredRight, anchoredY);
            }

            private bool IsPointerNear(Vector2 screenPoint)
            {
                if (ContainsWithPadding(_panelRect, screenPoint, HoverPadding, _canvas))
                {
                    return true;
                }

                if (_anchorRect != null && ContainsWithPadding(_anchorRect, screenPoint, HoverPadding, _anchorRect.GetComponentInParent<Canvas>()))
                {
                    return true;
                }

                var tooltipRect = DrillTooltipTrigger.ActiveTooltipRect;
                if (tooltipRect != null && tooltipRect.gameObject.activeInHierarchy)
                {
                    var tooltipCanvas = tooltipRect.GetComponentInParent<Canvas>();
                    if (ContainsWithPadding(tooltipRect, screenPoint, TooltipPadding, tooltipCanvas))
                    {
                        return true;
                    }
                }

                return false;
            }

            private static bool ContainsWithPadding(RectTransform rect, Vector2 screenPoint, float padding, Canvas canvas)
            {
                if (rect == null)
                {
                    return false;
                }

                rect.GetWorldCorners(CornerBuffer);
                Camera camera = GetCameraForCanvas(canvas);
                Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, CornerBuffer[0]);
                Vector2 max = min;
                for (int i = 1; i < 4; i++)
                {
                    Vector2 corner = RectTransformUtility.WorldToScreenPoint(camera, CornerBuffer[i]);
                    min = Vector2.Min(min, corner);
                    max = Vector2.Max(max, corner);
                }

                min -= new Vector2(padding, padding);
                max += new Vector2(padding, padding);
                return screenPoint.x >= min.x && screenPoint.x <= max.x && screenPoint.y >= min.y && screenPoint.y <= max.y;
            }

            private static Camera GetCameraForCanvas(Canvas canvas)
            {
                if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    return null;
                }

                return canvas.worldCamera;
            }
        }
    }
}
