using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TFTV.TFTVHavenRecruitsUI
{
    internal class HavenRecruitsOverlayAnimator
    {
        internal const float OverlaySlideDuration = 0.3f;
        internal sealed class OverlayAnimator : MonoBehaviour
        {
            private RectTransform _rectTransform;
            private Coroutine _animation;
            private float _hiddenDirection = 1f;
            private float _resolvedWidth;

            public bool IsVisible { get; private set; }

            public void Initialize(RectTransform rectTransform, bool slideFromLeft = false, float resolvedWidth = 0f)
            {
                _rectTransform = rectTransform ? rectTransform : GetComponent<RectTransform>();
                _hiddenDirection = slideFromLeft ? -1f : 1f;
                _resolvedWidth = Mathf.Max(0f, resolvedWidth);
                Canvas.ForceUpdateCanvases();
                HideInstant();
            }
            public void SetResolvedWidth(float width)
            {
                _resolvedWidth = Mathf.Max(0f, width);
                if (!IsVisible && _animation == null)
                {
                    HideImmediate();
                }
            }

            public void Play(bool show, Action onComplete)
            {
                EnsureRect();

                if (_animation != null)
                {
                    StopCoroutine(_animation);
                    _animation = null;
                }

                if (show)
                {
                    Canvas.ForceUpdateCanvases();
                }

                float targetX = show ? 0f : GetHiddenOffset();
                float startX = _rectTransform.anchoredPosition.x;

                if (Mathf.Approximately(startX, targetX))
                {
                    SetPosition(targetX);
                    IsVisible = show;
                    onComplete?.Invoke();
                    return;
                }

                _animation = StartCoroutine(SlideRoutine(startX, targetX, show, onComplete));
            }

            private IEnumerator SlideRoutine(float startX, float targetX, bool show, Action onComplete)
            {
                float elapsed = 0f;

                while (elapsed < OverlaySlideDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / OverlaySlideDuration);
                    float eased = Mathf.SmoothStep(0f, 1f, t);
                    SetPosition(Mathf.Lerp(startX, targetX, eased));
                    yield return null;
                }

                SetPosition(targetX);
                IsVisible = show;
                _animation = null;
                onComplete?.Invoke();
            }
            public void HideImmediate()
            {
                EnsureRect();
                if (_animation != null)
                {
                    StopCoroutine(_animation);
                    _animation = null;
                }
                HideInstant();
            }

            private void HideInstant()
            {
                SetPosition(GetHiddenOffset());
                IsVisible = false;
            }

            private float GetHiddenOffset()
            {
                EnsureRect();

                float width = _rectTransform.rect.width;

                RectTransform parent = _rectTransform.parent as RectTransform;
                if (parent != null)
                {
                    float anchorWidth = parent.rect.width * (_rectTransform.anchorMax.x - _rectTransform.anchorMin.x);
                    if (anchorWidth > 0f)
                    {
                        width = anchorWidth;
                    }
                }

                if (width <= 0f)
                {
                    width = _resolvedWidth > 0f ? _resolvedWidth : Screen.width * HavenRecruitsMain.ColumnsWidthPercent;
                }

                if (_resolvedWidth > 0f)
                {
                    width = _resolvedWidth;
                }
                return Mathf.Max(0f, width) * _hiddenDirection;
            }

            private void SetPosition(float x)
            {
                Vector2 pos = _rectTransform.anchoredPosition;
                pos.x = x;
                _rectTransform.anchoredPosition = pos;
            }

            private void EnsureRect()
            {
                if (_rectTransform == null)
                {
                    _rectTransform = GetComponent<RectTransform>();
                }
            }
        }
    }
}
