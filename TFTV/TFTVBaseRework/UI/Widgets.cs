using Base.Audio;
using Base.Core;
using Base.UI;
using PhoenixPoint.Geoscape.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TFTV.TFTVBaseRework
{
    /// <summary>
    /// Shared building blocks for the personnel screen: framed panels, section headers, scroll
    /// lists, buttons and checkboxes. Nothing here touches game state, so the panel files can stay
    /// about behaviour.
    /// </summary>
    public static partial class PersonnelManagementUI
    {
        #region Palette

        internal static readonly Color PanelBorderColor = new Color(0.38f, 0.45f, 0.55f, 0.90f);
        internal static readonly Color PanelFillColor = new Color(0.04f, 0.06f, 0.09f, 0.93f);
        internal static readonly Color RowFillColor = new Color(0.11f, 0.14f, 0.18f, 0.92f);
        internal static readonly Color RowFillAltColor = new Color(0.08f, 0.10f, 0.14f, 0.92f);

        internal static readonly Color AccentOrangeColor = new Color(1.00f, 0.62f, 0.10f, 1f);
        internal static readonly Color AccentCyanColor = new Color(0.25f, 0.83f, 0.90f, 1f);
        internal static readonly Color TextPrimaryColor = new Color(0.92f, 0.94f, 0.96f, 1f);
        internal static readonly Color TextDimColor = new Color(0.60f, 0.65f, 0.72f, 1f);
        internal static readonly Color TextDisabledColor = new Color(0.40f, 0.43f, 0.48f, 1f);

        internal static readonly Color ButtonFillColor = new Color(0.15f, 0.19f, 0.25f, 0.95f);
        internal static readonly Color ButtonFillDisabledColor = new Color(0.10f, 0.11f, 0.13f, 0.85f);
        internal static readonly Color ButtonFillDangerColor = new Color(0.45f, 0.18f, 0.14f, 0.95f);

        internal static readonly Color ScrollTrackColor = new Color(0.06f, 0.08f, 0.11f, 0.90f);
        internal static readonly Color ScrollHandleColor = new Color(0.34f, 0.41f, 0.50f, 0.95f);

        /// <summary>In canvas units, which this screen draws at roughly twice screen scale.</summary>
        internal const float ScrollbarWidth = 24f;

        internal const int TitleFontSize = 34;
        internal const int BodyFontSize = 26;
        internal const int SmallFontSize = 20;

        #endregion

        #region Primitives

        internal static void Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        internal static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        internal static Text CreateLabel(Transform parent, string name, string text, int fontSize, Color color,
            TextAnchor alignment = TextAnchor.MiddleLeft, bool wrap = false)
        {
            GameObject go = CreateUIObject(name, parent);
            var label = go.AddComponent<Text>();
            label.font = PuristaSemibold;
            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = alignment;
            label.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            // Truncating drops a whole line once its line height passes the rect height - at 56pt in
            // a 68px box the slot counters simply disappeared - and every label here is sized by its
            // own layout element anyway, so overflow is the safe setting.
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }

        /// <summary>
        /// Fixes an element's size along the dimensions given. The flexible size on a fixed dimension
        /// is pinned to zero: an object carrying a layout group of its own otherwise reports the
        /// group's flexible size (1 whenever the group force-expands its children) and competes with
        /// the scroll lists for the leftover space, which stretches headers and tab rows to hundreds
        /// of pixels and squeezes whatever sits below them.
        /// </summary>
        internal static LayoutElement SetSize(GameObject go, float width, float height)
        {
            LayoutElement element = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            if (width > 0f)
            {
                element.minWidth = width;
                element.preferredWidth = width;
                element.flexibleWidth = 0f;
            }
            if (height > 0f)
            {
                element.minHeight = height;
                element.preferredHeight = height;
                element.flexibleHeight = 0f;
            }
            return element;
        }

        /// <summary>
        /// A bordered box in the vanilla idiom: a thin outline drawn as the outer image with the
        /// fill inset by a couple of pixels. Returns the outer object; <paramref name="content"/>
        /// is the vertically stacked interior everything else is parented to.
        /// </summary>
        internal static GameObject CreateFramedPanel(Transform parent, string name, out Transform content,
            float borderThickness = 2f, int padding = 6, float spacing = 4f)
        {
            GameObject frame = CreateUIObject(name, parent);
            frame.AddComponent<Image>().color = PanelBorderColor;

            GameObject fill = CreateUIObject("Fill", frame.transform);
            fill.AddComponent<Image>().color = PanelFillColor;
            Stretch(fill.GetComponent<RectTransform>(), borderThickness);

            var layout = fill.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            content = fill.transform;
            return frame;
        }

        /// <summary>
        /// Section header: optional icon, a coloured title, and room on the right for a count or a
        /// button parented to the returned transform.
        /// </summary>
        internal static Transform CreateSectionHeader(Transform parent, string title, Sprite icon, Color titleColor,
            float height = 56f, int fontSize = TitleFontSize)
        {
            GameObject header = CreateUIObject("Header", parent);
            var layout = header.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(6, 6, 0, 0);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            SetSize(header, 0f, height);

            if (icon != null)
            {
                GameObject iconGO = CreateUIObject("Icon", header.transform);
                var image = iconGO.AddComponent<Image>();
                image.sprite = icon;
                image.color = titleColor;
                image.preserveAspect = true;
                image.raycastTarget = false;
                SetSize(iconGO, height - 12f, height - 12f);
            }

            Text label = CreateLabel(header.transform, "Title", title, fontSize, titleColor);
            LayoutElement labelElement = SetSize(label.gameObject, 0f, height);
            labelElement.flexibleWidth = 1f;

            return header.transform;
        }

        /// <summary>
        /// Vertical scroll list. The returned content transform stacks rows top-down; the viewport
        /// clips them. A scrollbar is drawn down the right-hand gutter and hides itself whenever the
        /// rows already fit, so a list that has run past the bottom of its panel says so.
        /// </summary>
        internal static ScrollRect CreateScrollList(Transform parent, string name, out Transform content,
            float spacing = 3f, int padding = 3, bool scrollbar = true)
        {
            GameObject scrollGO = CreateUIObject(name, parent);
            LayoutElement scrollElement = scrollGO.AddComponent<LayoutElement>();
            scrollElement.flexibleHeight = 1f;
            scrollElement.minHeight = 60f;
            var scrollRect = scrollGO.AddComponent<ScrollRect>();

            GameObject viewport = CreateUIObject("Viewport", scrollGO.transform);
            var mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            var viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
            // The gutter is held open whether or not the bar is currently showing: letting the
            // ScrollRect resize the viewport instead would reflow every row the moment one more
            // arrived, and the rows would jitter sideways as the list filled up.
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = new Vector2(scrollbar ? -ScrollbarWidth : 0f, 0f);

            GameObject contentGO = CreateUIObject("Content", viewport.transform);
            RectTransform contentRect = contentGO.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = Vector2.zero;

            var contentLayout = contentGO.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = spacing;
            contentLayout.padding = new RectOffset(padding, padding, padding, padding);
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            var fitter = contentGO.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;

            if (scrollbar)
            {
                scrollRect.verticalScrollbar = CreateVerticalScrollbar(scrollGO.transform);
                scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            }

            content = contentGO.transform;
            return scrollRect;
        }

        /// <summary>
        /// The bar itself: a track down the right edge of the scroll area with a handle the
        /// <see cref="ScrollRect"/> drives. Built by hand rather than from a prefab so it carries
        /// the same palette as the panels it sits in.
        /// </summary>
        private static Scrollbar CreateVerticalScrollbar(Transform parent)
        {
            GameObject barGO = CreateUIObject("Scrollbar", parent);
            barGO.AddComponent<Image>().color = ScrollTrackColor;

            RectTransform barRect = barGO.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(1f, 0f);
            barRect.anchorMax = new Vector2(1f, 1f);
            barRect.pivot = new Vector2(1f, 0.5f);
            barRect.offsetMin = new Vector2(-ScrollbarWidth, 0f);
            barRect.offsetMax = Vector2.zero;

            GameObject slidingArea = CreateUIObject("SlidingArea", barGO.transform);
            Stretch(slidingArea.GetComponent<RectTransform>(), 2f);

            GameObject handleGO = CreateUIObject("Handle", slidingArea.transform);
            var handleImage = handleGO.AddComponent<Image>();
            handleImage.color = ScrollHandleColor;

            RectTransform handleRect = handleGO.GetComponent<RectTransform>();
            // The scrollbar drives the handle's anchors every frame; its offsets have to start at
            // zero or the handle is drawn inset from wherever it was left.
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;

            var scrollbar = barGO.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;

            return scrollbar;
        }

        #endregion

        #region Buttons

        /// <summary>
        /// Remembers how a button looks enabled and disabled, so the screen can switch it either way
        /// in place instead of building a new one - which is what lets the panels update after an
        /// action rather than being torn down and rebuilt.
        /// </summary>
        internal sealed class ButtonLook : MonoBehaviour
        {
            internal Button Button;
            internal Image Background;
            internal Graphic Foreground;
            internal Image Edge;

            internal Color Fill;
            internal Color ForegroundColor;
            internal Color EdgeColor;

            internal void SetEnabled(bool enabled)
            {
                if (Button != null)
                {
                    Button.interactable = enabled;
                }

                if (Background != null)
                {
                    Background.color = enabled ? Fill : ButtonFillDisabledColor;
                }

                if (Foreground != null)
                {
                    Foreground.color = enabled ? ForegroundColor : TextDisabledColor;
                }

                if (Edge != null)
                {
                    Edge.color = enabled ? EdgeColor : TextDisabledColor;
                }
            }

            /// <summary>Changes the enabled colours, for buttons that also mark a selection.</summary>
            internal void SetColors(Color fill, Color foreground)
            {
                Fill = fill;
                ForegroundColor = foreground;
                SetEnabled(Button == null || Button.interactable);
            }
        }

        internal static void SetButtonEnabled(Button button, bool enabled)
        {
            if (button == null)
            {
                return;
            }

            ButtonLook look = button.GetComponent<ButtonLook>();
            if (look != null)
            {
                look.SetEnabled(enabled);
            }
            else
            {
                button.interactable = enabled;
            }
        }

        private static ButtonLook AttachLook(GameObject go, Button button, Image background, Graphic foreground,
            Color fill, Color foregroundColor, bool enabled, Image edge = null, Color? edgeColor = null)
        {
            ButtonLook look = go.AddComponent<ButtonLook>();
            look.Button = button;
            look.Background = background;
            look.Foreground = foreground;
            look.Edge = edge;
            look.Fill = fill;
            look.ForegroundColor = foregroundColor;
            look.EdgeColor = edgeColor ?? AccentOrangeColor;
            look.SetEnabled(enabled);
            return look;
        }

        /// <summary>
        /// The listener is always wired, whatever the starting state: a button that starts disabled
        /// can be enabled later by <see cref="SetButtonEnabled"/>, and Unity does not raise onClick
        /// for a button that is not interactable anyway.
        /// </summary>
        private static Button CreateButtonBase(GameObject go, Image background, Action onClick)
        {
            var button = go.AddComponent<Button>();
            button.targetGraphic = background;
            if (onClick != null)
            {
                button.onClick.AddListener(() =>
                {
                    try { onClick(); } catch (Exception e) { TFTVLogger.Error(e); }
                });
            }

            AddButtonSounds(go);
            return button;
        }

        internal static Button CreateTextButton(Transform parent, string name, string caption, Action onClick,
            float width = 0f, float height = 52f, int fontSize = BodyFontSize, bool enabled = true,
            Color? fillColor = null, Color? captionColor = null)
        {
            GameObject go = CreateUIObject(name, parent);
            var image = go.AddComponent<Image>();
            Button button = CreateButtonBase(go, image, onClick);

            SetSize(go, width, height);

            Text label = CreateLabel(go.transform, "Text", caption, fontSize, TextPrimaryColor, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, 6f);

            AttachLook(go, button, image, label, fillColor ?? ButtonFillColor, captionColor ?? TextPrimaryColor, enabled);
            return button;
        }

        /// <summary>
        /// Square icon button - the assign, unassign and dismiss controls on personnel rows.
        /// </summary>
        internal static Button CreateIconButton(Transform parent, string name, Sprite icon, Action onClick,
            float size = 44f, bool enabled = true, Color? fillColor = null, Color? iconColor = null,
            string fallbackCaption = null)
        {
            GameObject go = CreateUIObject(name, parent);
            var background = go.AddComponent<Image>();
            Button button = CreateButtonBase(go, background, onClick);

            SetSize(go, size, size);

            Graphic foreground = null;
            if (icon != null)
            {
                GameObject iconGO = CreateUIObject("Icon", go.transform);
                var image = iconGO.AddComponent<Image>();
                image.sprite = icon;
                image.preserveAspect = true;
                image.raycastTarget = false;
                Stretch(image.rectTransform, size * 0.16f);
                foreground = image;
            }
            else if (!string.IsNullOrEmpty(fallbackCaption))
            {
                Text label = CreateLabel(go.transform, "Text", fallbackCaption, (int)(size * 0.55f),
                    TextPrimaryColor, TextAnchor.MiddleCenter);
                Stretch(label.rectTransform);
                foreground = label;
            }

            AttachLook(go, button, background, foreground, fillColor ?? ButtonFillColor, iconColor ?? TextPrimaryColor, enabled);
            return button;
        }

        /// <summary>
        /// The large square step buttons that seat and unseat a worker, carrying the orange edge the
        /// vanilla screens use to mark an active control.
        /// </summary>
        internal static Button CreateStepperButton(Transform parent, string name, string caption, Action onClick,
            float size = 76f, bool enabled = true)
        {
            GameObject go = CreateUIObject(name, parent);
            var background = go.AddComponent<Image>();
            Button button = CreateButtonBase(go, background, onClick);

            SetSize(go, size, size);

            GameObject edge = CreateUIObject("Edge", go.transform);
            var edgeImage = edge.AddComponent<Image>();
            edgeImage.raycastTarget = false;
            RectTransform edgeRect = edge.GetComponent<RectTransform>();
            edgeRect.anchorMin = new Vector2(0f, 0f);
            edgeRect.anchorMax = new Vector2(0f, 1f);
            edgeRect.pivot = new Vector2(0f, 0.5f);
            edgeRect.offsetMin = Vector2.zero;
            edgeRect.offsetMax = new Vector2(5f, 0f);

            Text label = CreateLabel(go.transform, "Text", caption, (int)(size * 0.62f),
                TextPrimaryColor, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform);

            AttachLook(go, button, background, label, ButtonFillColor, TextPrimaryColor, enabled, edgeImage, AccentOrangeColor);
            return button;
        }

        /// <summary>The parts of a checkbox that show its value, so the value can be redrawn.</summary>
        internal sealed class CheckboxLook : MonoBehaviour
        {
            internal Image Fill;
            internal Text Label;

            internal void SetValue(bool value)
            {
                if (Fill != null)
                {
                    Fill.color = value ? AccentOrangeColor : PanelFillColor;
                }

                if (Label != null)
                {
                    Label.color = value ? TextPrimaryColor : TextDimColor;
                }
            }
        }

        /// <summary>
        /// Checkbox row: a square that fills when set, plus a clickable label.
        /// </summary>
        internal static Button CreateCheckbox(Transform parent, string name, string caption, bool value, Action onToggle,
            float height = 44f, int fontSize = BodyFontSize)
        {
            GameObject row = CreateUIObject(name, parent);
            var rowImage = row.AddComponent<Image>();
            rowImage.color = new Color(0f, 0f, 0f, 0.01f);

            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.padding = new RectOffset(6, 6, 0, 0);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            SetSize(row, 0f, height);

            Button button = CreateButtonBase(row, rowImage, onToggle);

            float boxSize = height - 14f;
            GameObject box = CreateUIObject("Box", row.transform);
            box.AddComponent<Image>().color = PanelBorderColor;
            SetSize(box, boxSize, boxSize);

            GameObject boxFill = CreateUIObject("Fill", box.transform);
            Image fill = boxFill.AddComponent<Image>();
            Stretch(boxFill.GetComponent<RectTransform>(), 3f);

            Text label = CreateLabel(row.transform, "Text", caption, fontSize, TextDimColor);
            LayoutElement labelElement = SetSize(label.gameObject, 0f, height);
            labelElement.flexibleWidth = 1f;

            CheckboxLook look = row.AddComponent<CheckboxLook>();
            look.Fill = fill;
            look.Label = label;
            look.SetValue(value);

            return button;
        }

        #endregion

        #region Sounds

        private static UIButtonSounds _buttonSoundSource;
        private static GeoLevelController _buttonSoundLevel;

        /// <summary>
        /// Gives a control the game's own button sounds - the click, the hover in and out, and the
        /// dull click of a disabled button - copied from a vanilla button rather than chosen here, so
        /// every action on this screen sounds like pressing any other button on the geoscape.
        /// </summary>
        private static void AddButtonSounds(GameObject go)
        {
            try
            {
                UIButtonSounds source = ResolveButtonSoundSource();
                if (source == null)
                {
                    return;
                }

                // Added after the Button, which it finds on Awake and hooks its pointer events to.
                UIButtonSounds sounds = go.AddComponent<UIButtonSounds>();
                sounds.Click = source.Click;
                sounds.Enter = source.Enter;
                sounds.Exit = source.Exit;
                sounds.ClickDisabled = source.ClickDisabled;
                sounds.EnterDisabled = source.EnterDisabled;
                sounds.ExitDisabled = source.ExitDisabled;

                // The sounds hang off an EventTrigger, which takes the mouse wheel as well as the
                // pointer, so a list would stop scrolling wherever the wheel met one of its buttons.
                if (go.GetComponentInParent<ScrollRect>() != null)
                {
                    go.AddComponent<ScrollPassthrough>();
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static UIButtonSounds ResolveButtonSoundSource()
        {
            GeoLevelController level = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();

            // Per geoscape: the source is a component on a prefab or a live module of that level.
            if (_buttonSoundSource != null && _buttonSoundLevel == level)
            {
                return _buttonSoundSource;
            }

            _buttonSoundLevel = level;
            _buttonSoundSource = null;

            GeoscapeModulesData modules = level?.View?.GeoscapeModules;
            if (modules == null)
            {
                return null;
            }

            Component[] candidates =
            {
                modules.SoldierEquipModule?.SoldierSlotPrefab,
                modules.SiteEncountersModule?.ChoiceButtonsContainer != null
                    ? modules.SiteEncountersModule.ChoiceButtonsContainer.transform
                    : null,
                modules.TradeModule?.ExitBtn,
            };

            foreach (Component candidate in candidates)
            {
                if (candidate == null)
                {
                    continue;
                }

                UIButtonSounds found = candidate.GetComponentInChildren<UIButtonSounds>(true);
                if (found != null && found.Click != null)
                {
                    _buttonSoundSource = found;
                    return found;
                }
            }

            TFTVLogger.Always($"{LogPrefix} No vanilla button sounds found; the personnel screen will be silent.");
            return null;
        }

        /// <summary>Hands the mouse wheel on to the list the element sits in.</summary>
        internal sealed class ScrollPassthrough : MonoBehaviour, IScrollHandler
        {
            private ScrollRect _scrollRect;

            public void OnScroll(PointerEventData eventData)
            {
                if (_scrollRect == null)
                {
                    _scrollRect = GetComponentInParent<ScrollRect>();
                }

                _scrollRect?.OnScroll(eventData);
            }
        }

        #endregion

        #region Keyed rows

        /// <summary>A built row in a list the screen keeps in step with the records.</summary>
        internal class RowView
        {
            internal GameObject Row;
            internal Image Background;
        }

        /// <summary>
        /// The rows of one list, by key. <see cref="SyncRows"/> brings them in line with the records:
        /// rows whose person has gone are removed, rows for new people are built, and the rest are
        /// updated and put in order - so an action builds the one row that changed rather than every
        /// row on the screen.
        /// </summary>
        internal sealed class KeyedRows<TView> where TView : RowView
        {
            internal Transform Content;
            internal GameObject Empty;
            internal readonly Dictionary<int, TView> Views = new Dictionary<int, TView>();
            internal readonly List<TView> Ordered = new List<TView>();
        }

        internal static void SyncRows<TItem, TView>(KeyedRows<TView> rows, IList<TItem> items, Func<TItem, int> key,
            Func<TItem, TView> create, Action<TItem, TView> update) where TView : RowView
        {
            if (rows?.Content == null)
            {
                return;
            }

            var wanted = new HashSet<int>();
            foreach (TItem item in items)
            {
                wanted.Add(key(item));
            }

            foreach (int stale in rows.Views.Keys.Where(k => !wanted.Contains(k)).ToList())
            {
                DetachAndDestroy(rows.Views[stale]?.Row);
                rows.Views.Remove(stale);
            }

            rows.Ordered.Clear();
            for (int i = 0; i < items.Count; i++)
            {
                TItem item = items[i];
                int k = key(item);

                if (!rows.Views.TryGetValue(k, out TView view) || view?.Row == null)
                {
                    view = create(item);
                    if (view?.Row == null)
                    {
                        rows.Views.Remove(k);
                        continue;
                    }

                    rows.Views[k] = view;
                }

                update?.Invoke(item, view);

                // Reordering dirties the list's layout even when nothing moves, and most refreshes
                // leave nearly every row where it was.
                if (view.Row.transform.GetSiblingIndex() != i)
                {
                    view.Row.transform.SetSiblingIndex(i);
                }

                rows.Ordered.Add(view);
            }

            if (rows.Empty != null)
            {
                rows.Empty.SetActive(rows.Ordered.Count == 0);
                rows.Empty.transform.SetAsLastSibling();
            }
        }

        /// <summary>Alternates the banding over the rows that are showing, in their order.</summary>
        internal static void Restripe<TView>(IEnumerable<TView> views) where TView : RowView
        {
            int shown = 0;
            foreach (TView view in views)
            {
                if (view?.Row == null || !view.Row.activeSelf)
                {
                    continue;
                }

                if (view.Background != null)
                {
                    view.Background.color = shown % 2 == 0 ? RowFillColor : RowFillAltColor;
                }

                shown++;
            }
        }

        /// <summary>
        /// Destroy() only takes effect at the end of the frame, so the row is unparented first:
        /// otherwise it still holds its place in the list, and the layout that runs this frame is
        /// worked out with it there.
        /// </summary>
        internal static void DetachAndDestroy(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            go.SetActive(false);
            go.transform.SetParent(null, false);
            Object.Destroy(go);
        }

        #endregion
    }
}
