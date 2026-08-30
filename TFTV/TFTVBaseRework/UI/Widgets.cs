using System;
using UnityEngine;
using UnityEngine.UI;

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
        internal static readonly Color RowExpandedColor = new Color(0.16f, 0.21f, 0.28f, 0.95f);

        internal static readonly Color AccentOrangeColor = new Color(1.00f, 0.62f, 0.10f, 1f);
        internal static readonly Color AccentCyanColor = new Color(0.25f, 0.83f, 0.90f, 1f);
        internal static readonly Color TextPrimaryColor = new Color(0.92f, 0.94f, 0.96f, 1f);
        internal static readonly Color TextDimColor = new Color(0.60f, 0.65f, 0.72f, 1f);
        internal static readonly Color TextDisabledColor = new Color(0.40f, 0.43f, 0.48f, 1f);

        internal static readonly Color ButtonFillColor = new Color(0.15f, 0.19f, 0.25f, 0.95f);
        internal static readonly Color ButtonFillDisabledColor = new Color(0.10f, 0.11f, 0.13f, 0.85f);
        internal static readonly Color ButtonFillDangerColor = new Color(0.45f, 0.18f, 0.14f, 0.95f);

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
        /// clips them.
        /// </summary>
        internal static ScrollRect CreateScrollList(Transform parent, string name, out Transform content,
            float spacing = 3f, int padding = 3)
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
            Stretch(viewport.GetComponent<RectTransform>());

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

            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;

            content = contentGO.transform;
            return scrollRect;
        }

        #endregion

        #region Buttons

        internal static Button CreateTextButton(Transform parent, string name, string caption, Action onClick,
            float width = 0f, float height = 52f, int fontSize = BodyFontSize, bool enabled = true,
            Color? fillColor = null, Color? captionColor = null)
        {
            GameObject go = CreateUIObject(name, parent);
            var image = go.AddComponent<Image>();
            image.color = enabled ? (fillColor ?? ButtonFillColor) : ButtonFillDisabledColor;

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.interactable = enabled;
            if (enabled && onClick != null)
            {
                button.onClick.AddListener(() =>
                {
                    try { onClick(); } catch (Exception e) { TFTVLogger.Error(e); }
                });
            }

            SetSize(go, width, height);

            Text label = CreateLabel(go.transform, "Text", caption, fontSize,
                enabled ? (captionColor ?? TextPrimaryColor) : TextDisabledColor, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, 6f);

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
            background.color = enabled ? (fillColor ?? ButtonFillColor) : ButtonFillDisabledColor;

            var button = go.AddComponent<Button>();
            button.targetGraphic = background;
            button.interactable = enabled;
            if (enabled && onClick != null)
            {
                button.onClick.AddListener(() =>
                {
                    try { onClick(); } catch (Exception e) { TFTVLogger.Error(e); }
                });
            }

            SetSize(go, size, size);

            if (icon != null)
            {
                GameObject iconGO = CreateUIObject("Icon", go.transform);
                var image = iconGO.AddComponent<Image>();
                image.sprite = icon;
                image.preserveAspect = true;
                image.raycastTarget = false;
                image.color = enabled ? (iconColor ?? TextPrimaryColor) : TextDisabledColor;
                Stretch(image.rectTransform, size * 0.16f);
            }
            else if (!string.IsNullOrEmpty(fallbackCaption))
            {
                Text label = CreateLabel(go.transform, "Text", fallbackCaption, (int)(size * 0.55f),
                    enabled ? (iconColor ?? TextPrimaryColor) : TextDisabledColor, TextAnchor.MiddleCenter);
                Stretch(label.rectTransform);
            }

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
            background.color = enabled ? ButtonFillColor : ButtonFillDisabledColor;

            var button = go.AddComponent<Button>();
            button.targetGraphic = background;
            button.interactable = enabled;
            if (enabled && onClick != null)
            {
                button.onClick.AddListener(() =>
                {
                    try { onClick(); } catch (Exception e) { TFTVLogger.Error(e); }
                });
            }

            SetSize(go, size, size);

            GameObject edge = CreateUIObject("Edge", go.transform);
            var edgeImage = edge.AddComponent<Image>();
            edgeImage.color = enabled ? AccentOrangeColor : TextDisabledColor;
            edgeImage.raycastTarget = false;
            RectTransform edgeRect = edge.GetComponent<RectTransform>();
            edgeRect.anchorMin = new Vector2(0f, 0f);
            edgeRect.anchorMax = new Vector2(0f, 1f);
            edgeRect.pivot = new Vector2(0f, 0.5f);
            edgeRect.offsetMin = Vector2.zero;
            edgeRect.offsetMax = new Vector2(5f, 0f);

            Text label = CreateLabel(go.transform, "Text", caption, (int)(size * 0.62f),
                enabled ? TextPrimaryColor : TextDisabledColor, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform);

            return button;
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

            var button = row.AddComponent<Button>();
            button.targetGraphic = rowImage;
            if (onToggle != null)
            {
                button.onClick.AddListener(() =>
                {
                    try { onToggle(); } catch (Exception e) { TFTVLogger.Error(e); }
                });
            }

            float boxSize = height - 14f;
            GameObject box = CreateUIObject("Box", row.transform);
            box.AddComponent<Image>().color = PanelBorderColor;
            SetSize(box, boxSize, boxSize);

            GameObject boxFill = CreateUIObject("Fill", box.transform);
            boxFill.AddComponent<Image>().color = value ? AccentOrangeColor : PanelFillColor;
            Stretch(boxFill.GetComponent<RectTransform>(), 3f);

            Text label = CreateLabel(row.transform, "Text", caption, fontSize, value ? TextPrimaryColor : TextDimColor);
            LayoutElement labelElement = SetSize(label.gameObject, 0f, height);
            labelElement.flexibleWidth = 1f;

            return button;
        }

        #endregion
    }
}
