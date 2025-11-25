using Base.Core;
using Base.Entities.Abilities;
using Base.UI;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.Entities.Abilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static TFTV.TFTVDrills.DrillsUI;

namespace TFTV.TFTVDrills
{
    internal static partial class DrillsUI
    {
        private static class UIBuilder
        {
            private static readonly Color PanelColor = new Color(0f, 0.05098039f, 0.08627451f, 1f);
            private static readonly Color ButtonNormalColor = new Color(1f, 1f, 1f, 0.08f);
            private static readonly Color ButtonHighlightColor = new Color(0.2f, 0.0588f, 0f, 1f);
            private static readonly Color ButtonPressedColor = new Color(0.3137255f, 0.11764706f, 0.019607844f, 1f);
            private static readonly Color ButtonDisabledColor = new Color(0.1f, 0.1f, 0.1f, 0.4f);
            
            private static readonly Color LockedFrameColor = new Color(0.15294118f, 0.15294118f, 0.15294118f, 1f);
            private static readonly Color FacilityRequirementTextColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            private static Font _defaultFont;

            public static GameObject CreateHoverOverlay(UIModuleCharacterProgression ui, AbilityTrackSkillEntryElement entry, out RectTransform panelRect, out RectTransform contentRect, out RectTransform viewportRect, out DrillOverlayController controller, out Transform tooltipParent)
            {
                panelRect = null;
                contentRect = null;
                viewportRect = null;
                controller = null;
                tooltipParent = null;

                if (ui == null)
                {
                    return null;
                }

                var canvas = ui.GetComponentInParent<Canvas>();
                Transform parent = canvas != null ? canvas.transform : ui.transform;
                tooltipParent = canvas != null ? canvas.transform : ui.transform;

                var overlay = new GameObject("TFTV_DrillOverlay", typeof(RectTransform), typeof(Image), typeof(Button));
                var overlayRect = (RectTransform)overlay.transform;
                overlayRect.SetParent(parent, false);
                overlayRect.anchorMin = Vector2.zero;
                overlayRect.anchorMax = Vector2.one;
                overlayRect.offsetMin = Vector2.zero;
                overlayRect.offsetMax = Vector2.zero;

                var backgroundImage = overlay.GetComponent<Image>();
                backgroundImage.color = new Color(0f, 0f, 0f, 0f);
                var backgroundButton = overlay.GetComponent<Button>();
                backgroundButton.transition = Selectable.Transition.None;

                var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                panelRect = (RectTransform)panel.transform;
                panelRect.SetParent(overlayRect, false);
                panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(1f, 1f);
                panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, MenuWidth);
                panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, MenuMaxHeight);

                var panelImage = panel.GetComponent<Image>();
                panelImage.color = PanelColor;
                panelImage.raycastTarget = true;

                var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
                viewportRect = (RectTransform)viewport.transform;
                viewportRect.SetParent(panelRect, false);
                viewportRect.anchorMin = new Vector2(0f, 0f);
                viewportRect.anchorMax = new Vector2(1f, 1f);
                viewportRect.offsetMin = Vector2.zero;
                viewportRect.offsetMax = Vector2.zero;

                var viewportImage = viewport.GetComponent<Image>();
                viewportImage.color = new Color(1f, 1f, 1f, 0f);
                viewportImage.raycastTarget = true;
                var rectMask = viewport.GetComponent<RectMask2D>();
                rectMask.enabled = true;

                var content = new GameObject("Content", typeof(RectTransform));
                contentRect = (RectTransform)content.transform;
                contentRect.SetParent(viewportRect, false);
                contentRect.anchorMin = new Vector2(0f, 1f);
                contentRect.anchorMax = new Vector2(1f, 1f);
                contentRect.pivot = new Vector2(0.5f, 1f);
                contentRect.anchoredPosition = Vector2.zero;

                var layout = content.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(18, 18, 20, 24);
                layout.spacing = 20f;
                layout.childAlignment = TextAnchor.UpperLeft;
                layout.childControlWidth = true;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;

                var fitter = content.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

                var scrollRect = panel.AddComponent<ScrollRect>();
                scrollRect.viewport = viewportRect;
                scrollRect.content = contentRect;
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;
                scrollRect.scrollSensitivity = 30f;
                scrollRect.inertia = true;

                var overlayController = overlay.AddComponent<DrillOverlayController>();
                overlayController.Initialize(canvas, overlayRect, panelRect, entry != null ? entry.GetComponent<RectTransform>() : null, backgroundButton);
                controller = overlayController;

                backgroundButton.onClick.AddListener(() => overlayController.Close());

                overlayRect.SetAsLastSibling();
                return overlay;
            }

            public static void AddHeader(
                RectTransform content,
                TacticalAbilityDef original,
                HeaderContext context,
                Transform tooltipParent = null,
                RectTransform panelRect = null,
                Canvas canvas = null,
                Action onAcquire = null)
            {
                if (content == null)
                {
                    return;
                }

                var headerRect = CreateChildRectTransform(content,
                   "Header",
                   anchorMin: new Vector2(0f, 0.5f),
                   anchorMax: new Vector2(1f, 0.5f),
                   pivot: new Vector2(0.5f, 0.5f),
                   anchoredPosition: Vector2.zero,
                   sizeDelta: new Vector2(0f, HeaderSectionHeight));

                ConfigureLayoutElement(headerRect,
                    minHeight: HeaderSectionHeight,
                    preferredHeight: HeaderSectionHeight);

                var buttonRect = CreateChildRectTransform(headerRect,
                    "AcquireBaseAbility",
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    pivot: new Vector2(0.5f, 0.5f),
                    anchoredPosition: Vector2.zero,
                    sizeDelta: new Vector2(GridCellWidth * 2f, HeaderSectionHeight),
                    typeof(Image), typeof(Button));

                ConfigureLayoutElement(buttonRect,
                    minWidth: GridCellWidth * 2f,
                    preferredWidth: GridCellWidth * 2f,
                    minHeight: HeaderSectionHeight,
                    preferredHeight: HeaderSectionHeight,
                    flexibleWidth: 0f,
                    flexibleHeight: 0f);

                var buttonImage = buttonRect.GetComponent<Image>();
                var normalColor = Color.black;
                var highlightColor = new Color(0.2f, 0.2f, 0.2f, 1f);
                var pressedColor = new Color(0.1f, 0.1f, 0.1f, 1f);
                var disabledColor = new Color(0.1f, 0.1f, 0.1f, 0.4f);
                var canAcquireColor = new Color(0.2509804f, 0.2509804f, 0.2509804f, 1f);
                buttonImage.color = normalColor;

                var border = buttonRect.gameObject.AddComponent<Outline>();
                border.effectColor = new Color(0.23137255f, 0.23137255f, 0.23137255f, 1f);
                border.effectDistance = new Vector2(2f, 2f);

                var button = buttonRect.GetComponent<Button>();
                button.transition = Selectable.Transition.ColorTint;
                var colors = button.colors;
                colors.normalColor = normalColor;
                colors.highlightedColor = highlightColor;
                colors.pressedColor = pressedColor;
                colors.selectedColor = highlightColor;
                colors.disabledColor = disabledColor;
                button.colors = colors;

                bool allowAcquire = context != null && !context.BaseAbilityLearned && onAcquire != null;
                bool canPurchase = allowAcquire && context.CanPurchaseBaseAbility;

                if (canPurchase)
                {
                    border.effectColor = StandardOutlineColor;
                    colors.highlightedColor = canAcquireColor;
                    colors.selectedColor = canAcquireColor;
                    button.colors = colors;
                    button.onClick.AddListener(() => onAcquire());
                }
                else
                {
                    button.interactable = false;
                    buttonImage.color = allowAcquire ? disabledColor : normalColor;
                }

                bool isLocked = allowAcquire && !context.CanPurchaseBaseAbility;

                bool baseAbilityLearned = context?.BaseAbilityLearned ?? false;
                Color iconColor;
                if (baseAbilityLearned)
                {
                    iconColor = context?.EntryElement != null ? context.EntryElement.KnownSkillColor : Color.white;
                }
                else
                {
                    iconColor = isLocked ? LockedIconTint : Color.white;
                }


                CreateHeaderIcon(buttonRect, original, iconColor, IsDrillAbility(original), isLocked);

                if (allowAcquire && tooltipParent != null && panelRect != null && canvas != null)
                {
                    var tooltipTrigger = buttonRect.gameObject.AddComponent<DrillTooltipTrigger>();
                    tooltipTrigger.Initialize(original, context.MissingRequirements, !context.CanPurchaseBaseAbility, false, tooltipParent, panelRect, canvas, context.BaseAbilityCost);
                }
            }
           
            public static RectTransform CreateOptionGrid(RectTransform content, out GridLayoutGroup grid)
            {
                grid = null;
                if (content == null)
                {
                    return null;
                }

                var gridRoot = new GameObject("OptionsGrid", typeof(RectTransform), typeof(GridLayoutGroup));
                var gridRect = (RectTransform)gridRoot.transform;
                gridRect.SetParent(content, false);
                gridRect.anchorMin = new Vector2(0f, 1f);
                gridRect.anchorMax = new Vector2(1f, 1f);
                gridRect.pivot = new Vector2(0.5f, 1f);

                grid = gridRoot.GetComponent<GridLayoutGroup>();
                grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
                grid.startAxis = GridLayoutGroup.Axis.Horizontal;
                grid.childAlignment = TextAnchor.UpperCenter;
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = GridColumns;
                grid.cellSize = new Vector2(GridCellWidth, GridCellHeight);
                grid.spacing = new Vector2(GridSpacing, GridSpacing);
                grid.padding = new RectOffset((int)GridPadding, (int)GridPadding, (int)GridPadding, (int)GridPadding);

                var layoutElement = gridRoot.AddComponent<LayoutElement>();
                layoutElement.minHeight = 0f;
                layoutElement.preferredHeight = 0f;
                layoutElement.flexibleHeight = 0f;
                layoutElement.minWidth = 0f;
                layoutElement.preferredWidth = 0f;
                layoutElement.flexibleWidth = 0f;

                return gridRect;
            }

            public static GameObject CreateTrainingFacilityOverlay(RectTransform panelRect)
            {
                if (panelRect == null)
                {
                    return null;
                }

                DefCache DefCache = TFTVMain.Main.DefCache;

                var overlayRect = CreateChildRectTransform(
     panelRect,
     "TrainingFacilityRequirementOverlay",
     anchorMin: new Vector2(0f, 0f),
     anchorMax: new Vector2(1f, 1f),
     pivot: new Vector2(0.5f, 0.5f),
     anchoredPosition: Vector2.zero,
     components: new[] { typeof(Image), typeof(CanvasGroup) });

                overlayRect.offsetMin = Vector2.zero;
                float topOffset = ContentTopPadding + HeaderSectionHeight + ContentSpacing;
                overlayRect.offsetMax = new Vector2(0f, -topOffset);
                overlayRect.SetAsLastSibling();

                var overlayImage = overlayRect.GetComponent<Image>();
                var color = PanelColor;
                overlayImage.color = new Color(color.r, color.g, color.b, FacilityOverlayOpacity);
                overlayImage.raycastTarget = true;

                var canvasGroup = overlayRect.GetComponent<CanvasGroup>();
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = true;
                var messageRect = CreateChildRectTransform(overlayRect,
                    "Message",
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    pivot: new Vector2(0.5f, 0.5f),
                    anchoredPosition: Vector2.zero,
                    components: new[] { typeof(VerticalLayoutGroup), typeof(ContentSizeFitter) });


                var messageLayout = messageRect.GetComponent<VerticalLayoutGroup>();
                messageLayout.spacing = 8f;
                messageLayout.childAlignment = TextAnchor.MiddleCenter;
                messageLayout.childControlWidth = false;
                messageLayout.childControlHeight = false;
                messageLayout.childForceExpandWidth = false;
                messageLayout.childForceExpandHeight = false;

                var messageFitter = messageRect.GetComponent<ContentSizeFitter>();
                messageFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                messageFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                var iconRect = CreateChildRectTransform(messageRect,
                    "TrainingFacilityIcon",
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    pivot: new Vector2(0.5f, 0.5f),
                    anchoredPosition: Vector2.zero,
                    sizeDelta: new Vector2(FacilityIconSize, FacilityIconSize),
                    typeof(Image), typeof(LayoutElement));

                var iconImage = iconRect.GetComponent<Image>();
                iconImage.color = Color.white;
                iconImage.sprite = DefCache.GetDef<ViewElementDef>("E_ViewElement [TrainingFacility_PhoenixFacilityDef]").SmallIcon;
                iconImage.raycastTarget = false;

                ConfigureLayoutElement(iconRect,
                    minWidth: FacilityIconSize,
                    minHeight: FacilityIconSize,
                    preferredWidth: FacilityIconSize,
                    preferredHeight: FacilityIconSize);

                var placeholderRect = CreateChildRectTransform(iconRect,
                    "PlaceholderLabel",
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    pivot: new Vector2(0.5f, 0.5f),
                    anchoredPosition: Vector2.zero,
                    components: typeof(Text));

                var placeholderText = placeholderRect.GetComponent<Text>();


                placeholderText.font = GetDefaultFont();
                placeholderText.text = "ICON";
                placeholderText.fontSize = 18;
                placeholderText.alignment = TextAnchor.MiddleCenter;
                placeholderText.color = new Color(0.8f, 0.8f, 0.8f, 0.8f);
                placeholderText.raycastTarget = false;

                AddTrainingFacilityOverlayLabel(messageRect, "REQUIRES A FUNCTIONING", FacilityRequirementTextColor, 40);
                AddTrainingFacilityOverlayLabel(messageRect, "TRAINING FACILITY", Color.white, 50);

                return overlayRect.gameObject;
            }


            public static GameObject CreateDrillOption(RectTransform gridRect,
                RectTransform panelRect, TacticalAbilityDef def, bool isLocked, bool isAcquired,
                string missingRequirements, Transform tooltipParent, Canvas canvas, Action onChoose, int skillPointCost)
            {
                if (gridRect == null || def == null)
                {
                    return null;
                }

                var optionRect = CreateChildRectTransform(gridRect,
                      def.name ?? "Ability",
                      anchorMin: new Vector2(0f, 1f),
                      anchorMax: new Vector2(0f, 1f),
                      pivot: new Vector2(0.5f, 0.5f),
                      anchoredPosition: Vector2.zero,
                      sizeDelta: Vector2.zero,
                      typeof(Image), typeof(Button));

                var background = optionRect.GetComponent<Image>();
                background.color = ButtonNormalColor;

                var button = optionRect.GetComponent<Button>();
                button.transition = Selectable.Transition.ColorTint;
                var colors = button.colors;
                colors.normalColor = ButtonNormalColor;
                colors.highlightedColor = ButtonHighlightColor;
                colors.pressedColor = ButtonPressedColor;
                colors.selectedColor = ButtonHighlightColor;
                colors.disabledColor = ButtonDisabledColor;
                colors.colorMultiplier = 1f;
                button.colors = colors;

                bool canSelect = !isLocked && !isAcquired && onChoose != null;
                if (canSelect)
                {
                    button.onClick.AddListener(() => onChoose());
                }
                else
                {
                    button.interactable = false;
                    background.color = isAcquired ? ButtonNormalColor : ButtonDisabledColor;
                }

                var layout = optionRect.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(8, 8, 8, 8);
                layout.spacing = 0f;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;

                var frameRect = CreateChildRectTransform(optionRect,
                    "IconFrame",
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    pivot: new Vector2(0.5f, 0.5f),
                    anchoredPosition: Vector2.zero,
                     sizeDelta: new Vector2(OptionIconFrameSize, OptionIconFrameSize));

                ConfigureLayoutElement(frameRect,
                    minWidth: OptionIconFrameSize,
                    minHeight: OptionIconFrameSize,
                    preferredWidth: OptionIconFrameSize,
                    preferredHeight: OptionIconFrameSize);

                float borderThickness = HeaderFrameBorderThickness;

                Color frameColor = isLocked ? LockedFrameColor : StandardOutlineColor;
                if (isAcquired)
                {
                    frameColor = DrillPulseColor;
                }

                CreateFrameBorders(frameRect, frameColor, borderThickness);

                var frameBackgroundRect = CreateChildRectTransform(frameRect,
                    "FrameBackground",
                    anchorMin: new Vector2(0f, 0f),
                    anchorMax: new Vector2(1f, 1f),
                    pivot: new Vector2(0.5f, 0.5f),
                    anchoredPosition: Vector2.zero,
                    components: typeof(Image));
                frameBackgroundRect.offsetMin = new Vector2(borderThickness, borderThickness);
                frameBackgroundRect.offsetMax = new Vector2(-borderThickness, -borderThickness);

                var frameBackgroundImage = frameBackgroundRect.GetComponent<Image>();
                frameBackgroundImage.color = OptionFillColor;
                frameBackgroundImage.raycastTarget = false;

                var iconRect = CreateChildRectTransform(frameBackgroundRect,
                   "Icon",
                   anchorMin: new Vector2(0.5f, 0.5f),
                   anchorMax: new Vector2(0.5f, 0.5f),
                   pivot: new Vector2(0.5f, 0.5f),
                   anchoredPosition: Vector2.zero,
                   sizeDelta: new Vector2(OptionIconSize, OptionIconSize),
                   components: typeof(Image));


                var iconImage = iconRect.GetComponent<Image>();
                iconImage.sprite = def.ViewElementDef?.LargeIcon ?? def.ViewElementDef?.SmallIcon;
                iconImage.preserveAspect = true;
                iconImage.color = isLocked ? LockedIconTint : (isAcquired ? DrillPulseColor : Color.white);
                iconImage.raycastTarget = false;

                var tooltipTrigger = optionRect.gameObject.AddComponent<DrillTooltipTrigger>();
                tooltipTrigger.Initialize(def, missingRequirements, isLocked, isAcquired, tooltipParent, panelRect, canvas, skillPointCost);

                return optionRect.gameObject;
            }

            private static void AddTrainingFacilityOverlayLabel(Transform parent, string text, Color color, int fontSize)
            {
                if (parent == null)
                {
                    return;
                }

                var rect = CreateChildRectTransform(parent,
                       "Label",
                       anchorMin: new Vector2(0.5f, 0.5f),
                       anchorMax: new Vector2(0.5f, 0.5f),
                       pivot: new Vector2(0.5f, 0.5f),
                       anchoredPosition: Vector2.zero,
                       components: new[] { typeof(Text), typeof(LayoutElement) });

                var label = rect.GetComponent<Text>();
                label.font = GetDefaultFont();
                label.text = text;
                label.color = color;
                label.fontSize = fontSize;
                label.fontStyle = FontStyle.Bold;
                label.alignment = TextAnchor.MiddleCenter;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.raycastTarget = false;

                var layout = rect.GetComponent<LayoutElement>();
                layout.minHeight = fontSize + 6f;
                layout.preferredHeight = fontSize + 6f;
            }

            public static RectTransform CreateChildRectTransform(Transform parent, string name,
                Vector2? anchorMin = null, Vector2? anchorMax = null, Vector2? pivot = null,
                Vector2? anchoredPosition = null, Vector2? sizeDelta = null, params Type[] components)
            {
                if (parent == null)
                {
                    return null;
                }

                var componentList = new List<Type> { typeof(RectTransform) };
                if (components != null && components.Length > 0)
                {
                    componentList.AddRange(components);
                }

                var child = new GameObject(name, componentList.Distinct().ToArray());
                var rect = (RectTransform)child.transform;
                rect.SetParent(parent, false);

                if (anchorMin.HasValue)
                {
                    rect.anchorMin = anchorMin.Value;
                }

                if (anchorMax.HasValue)
                {
                    rect.anchorMax = anchorMax.Value;
                }

                if (pivot.HasValue)
                {
                    rect.pivot = pivot.Value;
                }

                if (anchoredPosition.HasValue)
                {
                    rect.anchoredPosition = anchoredPosition.Value;
                }

                if (sizeDelta.HasValue)
                {
                    rect.sizeDelta = sizeDelta.Value;
                }

                return rect;
            }

            public static void CreateFrameBorders(RectTransform parentRect, Color color, float thickness)
            {
                if (parentRect == null)
                {
                    return;
                }

                void CreateBorder(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta)
                {
                    var borderRect = CreateChildRectTransform(parentRect, name,
                        anchorMin: anchorMin,
                        anchorMax: anchorMax,
                        pivot: new Vector2(0.5f, 0.5f),
                        anchoredPosition: Vector2.zero,
                        sizeDelta: sizeDelta,
                        components: typeof(Image));

                    var image = borderRect.GetComponent<Image>();
                    image.color = color;
                    image.raycastTarget = false;
                }

                CreateBorder("Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(thickness, 0f));
                CreateBorder("Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(thickness, 0f));
                CreateBorder("Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, thickness));
                CreateBorder("Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, thickness));
            }

            public static LayoutElement ConfigureLayoutElement(Component owner,
                float? minWidth = null, float? minHeight = null,
                float? preferredWidth = null, float? preferredHeight = null,
                float? flexibleWidth = null, float? flexibleHeight = null)
            {
                if (owner == null)
                {
                    return null;
                }

                var layout = owner.GetComponent<LayoutElement>() ?? owner.gameObject.AddComponent<LayoutElement>();

                if (minWidth.HasValue)
                {
                    layout.minWidth = minWidth.Value;
                }

                if (minHeight.HasValue)
                {
                    layout.minHeight = minHeight.Value;
                }

                if (preferredWidth.HasValue)
                {
                    layout.preferredWidth = preferredWidth.Value;
                }

                if (preferredHeight.HasValue)
                {
                    layout.preferredHeight = preferredHeight.Value;
                }

                if (flexibleWidth.HasValue)
                {
                    layout.flexibleWidth = flexibleWidth.Value;
                }

                if (flexibleHeight.HasValue)
                {
                    layout.flexibleHeight = flexibleHeight.Value;
                }

                return layout;
            }

            public static void ResizeOptionGrid(RectTransform gridRect, GridLayoutGroup grid, int optionCount)
            {
                if (gridRect == null || grid == null)
                {
                    return;
                }

                int columns = grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount && grid.constraintCount > 0
                    ? grid.constraintCount
                    : Mathf.Max(1, GridColumns);

                int rows = optionCount > 0 ? Mathf.CeilToInt(optionCount / (float)columns) : 0;
                int visibleRows = Mathf.Max(rows, VisibleGridRows);

                float height = grid.padding.top + grid.padding.bottom;
                if (visibleRows > 0)
                {
                    height += visibleRows * grid.cellSize.y;
                    height += Mathf.Max(0, visibleRows - 1) * grid.spacing.y;
                }

                float width = grid.padding.left + grid.padding.right;
                width += columns * grid.cellSize.x;
                width += Mathf.Max(0, columns - 1) * grid.spacing.x;

                var layout = gridRect.GetComponent<LayoutElement>() ?? gridRect.gameObject.AddComponent<LayoutElement>();
                layout.minHeight = height;
                layout.preferredHeight = height;
                layout.flexibleHeight = 0f;
                layout.minWidth = width;
                layout.preferredWidth = width;
                layout.flexibleWidth = 0f;

                gridRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            }

            public static void AddEmptyLabel(RectTransform content, string label)
            {
                if (content == null)
                {
                    return;
                }

                var labelGO = new GameObject("Empty", typeof(RectTransform), typeof(Text));
                var rect = (RectTransform)labelGO.transform;
                rect.SetParent(content, false);
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(1f, 0.5f);
                rect.sizeDelta = new Vector2(0f, 48f);

                var text = labelGO.GetComponent<Text>();
                text.font = GetDefaultFont();
                text.text = label;
                text.fontSize = 18;
                text.color = Color.white;
                text.alignment = TextAnchor.MiddleCenter;
                text.raycastTarget = false;

                var layout = labelGO.AddComponent<LayoutElement>();
                layout.minHeight = 48f;
                layout.preferredHeight = 48f;
            }

            private static Font GetDefaultFont()
            {
                if (_defaultFont == null)
                {
                   _defaultFont = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.PhoenixpediaModule.EntryTitle.font;
                        

                  //  _defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }

                return _defaultFont;
            }
        }
    }
}
