using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.View.ViewModules;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TFTV.TFTVUI.Tactical
{
    /// <summary>
    /// Draws a <see cref="StatusForecast.Forecast"/> beside the status chip it belongs to.
    ///
    /// The vanilla chip tooltip is a single Text, which cannot hold the status icon, cannot right-align
    /// a value column and cannot mix font sizes cleanly, so the forecast gets a panel of its own. It is
    /// built once per canvas and its rows are rebuilt on every show; the chip's own UITooltipText is
    /// switched off while a forecast is attached so the two never appear together.
    /// </summary>
    internal static class StatusForecastTooltip
    {
        private const string TooltipObjectName = "TFTV_StatusForecastTooltip";
        private const float Width = 460f;
        private const float WideWidth = 640f;
        private const float Margin = 8f;
        private const int PadH = 24;
        private const int PadV = 18;

        private const int TitleFontSize = 38;
        private const int BodyFontSize = 32;
        private const int SmallFontSize = 25;

        /// <summary>Indent of bracketed rows; the bracket line sits inside it.</summary>
        private const int BracketIndent = 20;
        private const float BracketLineWidth = 3f;

        private static readonly Color Background = new Color(0.11f, 0.11f, 0.13f, 1f);
        private static readonly Color BodyColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        private static readonly Color MutedColor = new Color(0.60f, 0.60f, 0.62f, 1f);
        private static readonly Color LossColor = new Color(0.93f, 0.24f, 0.24f, 1f);
        private static readonly Color BracketColor = new Color(0.85f, 0.85f, 0.85f, 0.8f);

        private static RectTransform _root;

        internal static void Show(StatusForecast.Forecast forecast, RectTransform anchor, Font font)
        {
            try
            {
                if (forecast == null || anchor == null)
                {
                    return;
                }

                RectTransform root = EnsureRoot(anchor);
                if (root == null)
                {
                    return;
                }

                Populate(root, forecast, font ?? Resources.GetBuiltinResource<Font>("Arial.ttf"));

                root.sizeDelta = new Vector2(forecast.Wide ? WideWidth : Width, root.sizeDelta.y);
                LayoutRebuilder.ForceRebuildLayoutImmediate(root);

                PositionBeside(root, anchor);

                root.gameObject.SetActive(true);
                root.SetAsLastSibling();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void Hide()
        {
            if (_root != null && _root.gameObject.activeSelf)
            {
                _root.gameObject.SetActive(false);
            }
        }

        #region building

        private static void Populate(RectTransform root, StatusForecast.Forecast forecast, Font font)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                GameObject child = root.GetChild(i).gameObject;
                child.SetActive(false);
                UnityEngine.Object.Destroy(child);
            }

            AddText(root, forecast.Title.ToUpper(), font, TitleFontSize, forecast.TitleColor, TextAnchor.MiddleLeft);
            AddText(root, TFTVCommonMethods.ConvertKeyToString("TFTV_ACID_NEXT_TURN"), font, SmallFontSize, MutedColor, TextAnchor.MiddleLeft);

            RectTransform group = null;

            foreach (StatusForecast.Row row in forecast.Rows)
            {
                if (!row.Bracketed)
                {
                    group = null;
                }
                else if (group == null)
                {
                    group = AddBracketGroup(root);
                }

                RectTransform parent = group ?? root;

                switch (row.Kind)
                {
                    case StatusForecast.RowKind.Stat:
                        AddStatRow(parent, row, forecast, font);
                        break;
                    case StatusForecast.RowKind.Level:
                        AddLevelRow(parent, row, forecast, font);
                        break;
                    case StatusForecast.RowKind.Note:
                        AddText(parent, row.Label, font, SmallFontSize, MutedColor, TextAnchor.UpperLeft);
                        break;
                    case StatusForecast.RowKind.Warning:
                        AddText(parent, row.Label, font, BodyFontSize, LossColor, TextAnchor.UpperLeft);
                        break;
                }
            }
        }

        private static RectTransform NewChild(Transform parent, string name, params Type[] components)
        {
            Type[] all = new Type[components.Length + 1];
            all[0] = typeof(RectTransform);
            Array.Copy(components, 0, all, 1, components.Length);

            GameObject go = new GameObject(name, all);
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static Text AddText(Transform parent, string content, Font font, int size, Color color, TextAnchor alignment)
        {
            Text text = NewChild(parent, "Text", typeof(Text)).GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = false;
            text.raycastTarget = false;
            text.text = content ?? string.Empty;
            return text;
        }

        private static HorizontalLayoutGroup AddRow(Transform parent)
        {
            HorizontalLayoutGroup layout = NewChild(parent, "Row", typeof(HorizontalLayoutGroup)).GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return layout;
        }

        private static void AddStatRow(Transform parent, StatusForecast.Row row, StatusForecast.Forecast forecast, Font font)
        {
            HorizontalLayoutGroup layout = AddRow(parent);

            Text label = AddText(layout.transform, row.Label.ToUpper(), font, BodyFontSize, BodyColor, TextAnchor.MiddleLeft);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            Text value = AddText(layout.transform, row.Value, font, BodyFontSize, row.IsLoss ? LossColor : BodyColor, TextAnchor.MiddleRight);
            value.horizontalOverflow = HorizontalWrapMode.Overflow;

            if (row.HasLevel)
            {
                // A fixed-width cell, so the levels of several limbs line up under each other.
                RectTransform cell = NewChild(layout.transform, "Level", typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                cell.GetComponent<LayoutElement>().preferredWidth = 190f;
                HorizontalLayoutGroup cellLayout = cell.GetComponent<HorizontalLayoutGroup>();
                cellLayout.childAlignment = TextAnchor.MiddleRight;
                cellLayout.spacing = 8f;
                cellLayout.childControlWidth = true;
                cellLayout.childControlHeight = true;
                cellLayout.childForceExpandWidth = false;
                cellLayout.childForceExpandHeight = false;

                AddLevel(cell, row, forecast, font, BodyFontSize);
            }
        }

        private static void AddLevelRow(Transform parent, StatusForecast.Row row, StatusForecast.Forecast forecast, Font font)
        {
            HorizontalLayoutGroup layout = AddRow(parent);
            layout.padding = new RectOffset(0, 0, 4, 0);
            AddLevel(layout.transform, row, forecast, font, BodyFontSize);
        }

        /// <summary>The status icon followed by "40 → 30".</summary>
        private static void AddLevel(Transform parent, StatusForecast.Row row, StatusForecast.Forecast forecast, Font font, int size)
        {
            if (forecast.Icon != null)
            {
                RectTransform iconRect = NewChild(parent, "Icon", typeof(Image), typeof(LayoutElement));
                Image icon = iconRect.GetComponent<Image>();
                icon.sprite = forecast.Icon;
                icon.color = forecast.IconColor;
                icon.preserveAspect = true;
                icon.raycastTarget = false;

                LayoutElement element = iconRect.GetComponent<LayoutElement>();
                element.preferredWidth = size * 1.4f;
                element.preferredHeight = size * 1.4f;
            }

            Text text = AddText(parent, $"{row.Before} → {row.After}", font, size, BodyColor, TextAnchor.MiddleLeft);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        /// <summary>
        /// Consecutive per-limb rows go into one container whose left edge carries a single line
        /// spanning all of them, so they read as the breakdown of the row above.
        /// </summary>
        private static RectTransform AddBracketGroup(Transform parent)
        {
            RectTransform group = NewChild(parent, "Limbs", typeof(VerticalLayoutGroup));
            VerticalLayoutGroup layout = group.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(BracketIndent, 0, 0, 0);
            layout.spacing = 2f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            RectTransform line = NewChild(group, "Bracket", typeof(Image), typeof(LayoutElement));
            line.GetComponent<LayoutElement>().ignoreLayout = true;
            line.anchorMin = new Vector2(0f, 0f);
            line.anchorMax = new Vector2(0f, 1f);
            line.pivot = new Vector2(0f, 0.5f);
            line.anchoredPosition = new Vector2(3f, 0f);
            line.sizeDelta = new Vector2(BracketLineWidth, 0f);

            Image image = line.GetComponent<Image>();
            image.color = BracketColor;
            image.raycastTarget = false;

            return group;
        }

        #endregion

        #region placement

        /// <summary>
        /// Hangs the panel from the chip's top-right corner, pulled back inside the canvas when the
        /// chip is near an edge.
        /// </summary>
        private static void PositionBeside(RectTransform tooltip, RectTransform anchor)
        {
            Canvas canvas = tooltip.GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas?.transform as RectTransform;
            if (canvasRect == null)
            {
                return;
            }

            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

            Vector3[] corners = new Vector3[4];
            anchor.GetWorldCorners(corners);
            // corners[2] is the top-right corner of the chip.
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, cam, out Vector2 localPoint))
            {
                return;
            }

            float halfWidth = canvasRect.rect.width * 0.5f;
            float halfHeight = canvasRect.rect.height * 0.5f;
            float width = tooltip.rect.width;
            float height = tooltip.rect.height;

            // Pivot is top-left, so the panel hangs down and to the right of the chip.
            float x = Mathf.Clamp(localPoint.x + Margin, -halfWidth + Margin, halfWidth - width - Margin);
            float y = Mathf.Clamp(localPoint.y, -halfHeight + height + Margin, halfHeight - Margin);

            tooltip.anchoredPosition = new Vector2(x, y);
        }

        /// <summary>
        /// The panel belongs to the status screen's canvas, so it dies with it; Unity's null check
        /// catches that and the next hover builds a fresh one.
        /// </summary>
        private static RectTransform EnsureRoot(Transform reference)
        {
            Canvas canvas = reference.GetComponentInParent<Canvas>()?.rootCanvas;
            if (canvas == null)
            {
                return null;
            }

            if (_root != null && _root.parent == canvas.transform)
            {
                return _root;
            }

            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root.gameObject);
            }

            RectTransform root = NewChild(canvas.transform, TooltipObjectName,
                typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement), typeof(CanvasGroup));
            root.GetComponent<LayoutElement>().ignoreLayout = true;
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0f, 1f);
            root.sizeDelta = new Vector2(Width, 0f);

            // Hovering the panel itself must not steal the pointer from the chip under it.
            CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            Image background = root.GetComponent<Image>();
            background.color = Background;
            background.raycastTarget = false;

            VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(PadH, PadH, PadV, PadV);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = root.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            root.gameObject.SetActive(false);
            _root = root;
            return root;
        }

        #endregion
    }

    /// <summary>
    /// Warning icons beside the bars on the character status screen, for what the statuses
    /// together will do at the start of next turn:
    ///
    ///  - a skull and crossbones by Hit Points when the damage over time will kill,
    ///  - the panic icon by Will Points when poison and virus will take the last of them,
    ///  - the paralysed icon by Action Points when stun, paralysis and suppression leave none.
    ///
    /// Each chip only forecasts itself, so without these a player has to add the chips up to see
    /// it coming; hovering an icon lists what each status contributes.
    /// </summary>
    internal static class NextTurnWarningMarkers
    {
        private const float Gap = 10f;
        private static readonly Color WarningColor = new Color(0.93f, 0.24f, 0.24f, 1f);

        private static Sprite _skull;

        private static Sprite Skull
        {
            get
            {
                if (_skull == null)
                {
                    _skull = Helper.CreateSpriteFromImageFile("TFTV_LethalDoT.png");
                }
                return _skull;
            }
        }

        /// <summary>A status def's own icon, falling back to the skull if the def or its visuals are missing.</summary>
        private static Sprite StatusIcon(string defName)
        {
            TacStatusDef def = TFTVMain.Main.DefCache.GetDef<TacStatusDef>(defName);
            return def?.Visuals?.SmallIcon ?? Skull;
        }

        [HarmonyLib.HarmonyPatch(typeof(UIModuleCharacterStatus), "SetData")]
        internal static class UIModuleCharacterStatus_SetData_NextTurnWarnings_Patch
        {
            private static void Postfix(UIModuleCharacterStatus __instance)
            {
                try
                {
                    if (__instance == null)
                    {
                        return;
                    }

                    TacticalActor actor = AcidReadout.UIStateCharacterStatus_SetData_TrackActor_Patch.Current;

                    Sprite panicIcon = StatusIcon("Panic_StatusDef");
                    Sprite paralysedIcon = StatusIcon("Paralysed_StatusDef");

                    Place(__instance.HitPointsBarText, "TFTV_LethalDoTMarker", Skull,
                        StatusForecast.BuildLethal(actor, Skull));
                    Place(__instance.WillPointsBarText, "TFTV_PanicDoTMarker", panicIcon,
                        StatusForecast.BuildPanic(actor, panicIcon));
                    Place(__instance.ActionPointsBarText, "TFTV_NoActionPointsMarker", paralysedIcon,
                        StatusForecast.BuildNoActionPoints(actor, paralysedIcon));
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            /// <summary>
            /// Shows the marker just left of the bar's value text ("225/270") when there is a
            /// forecast, and hides it otherwise. The screen is reused for every character, so a
            /// marker left over from the previous one must go.
            /// </summary>
            private static void Place(Text barText, string markerName, Sprite icon, StatusForecast.Forecast forecast)
            {
                if (barText == null)
                {
                    return;
                }

                Transform existing = barText.transform.Find(markerName);

                if (forecast == null)
                {
                    if (existing != null)
                    {
                        existing.gameObject.SetActive(false);
                    }
                    return;
                }

                RectTransform marker = existing as RectTransform ?? Create(barText, markerName);
                marker.gameObject.SetActive(true);
                marker.GetComponent<Image>().sprite = icon;

                float size = barText.fontSize * 1.3f;
                float textWidth = Mathf.Min(barText.preferredWidth, barText.rectTransform.rect.width);

                switch (barText.alignment)
                {
                    case TextAnchor.UpperRight:
                    case TextAnchor.MiddleRight:
                    case TextAnchor.LowerRight:
                        marker.anchorMin = marker.anchorMax = new Vector2(1f, 0.5f);
                        marker.anchoredPosition = new Vector2(-(textWidth + Gap), 0f);
                        break;
                    case TextAnchor.UpperCenter:
                    case TextAnchor.MiddleCenter:
                    case TextAnchor.LowerCenter:
                        marker.anchorMin = marker.anchorMax = new Vector2(0.5f, 0.5f);
                        marker.anchoredPosition = new Vector2(-(textWidth * 0.5f + Gap), 0f);
                        break;
                    default:
                        marker.anchorMin = marker.anchorMax = new Vector2(0f, 0.5f);
                        marker.anchoredPosition = new Vector2(-Gap, 0f);
                        break;
                }

                marker.sizeDelta = new Vector2(size, size);

                StatusForecastTrigger trigger = marker.GetComponent<StatusForecastTrigger>();
                trigger.Forecast = forecast;
                trigger.Font = barText.font;
            }

            private static RectTransform Create(Text barText, string markerName)
            {
                GameObject go = new GameObject(markerName,
                    typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(StatusForecastTrigger));
                go.transform.SetParent(barText.transform, false);
                go.GetComponent<LayoutElement>().ignoreLayout = true;

                Image image = go.GetComponent<Image>();
                image.color = WarningColor;
                image.preserveAspect = true;
                // Hoverable, for the breakdown.
                image.raycastTarget = true;

                RectTransform rect = go.GetComponent<RectTransform>();
                rect.pivot = new Vector2(1f, 0.5f);
                return rect;
            }
        }
    }

    /// <summary>
    /// Sits on a status chip and shows its forecast on hover. The row controllers are pooled and
    /// reused for other statuses, so the forecast is replaced (or cleared) on every SetData.
    /// </summary>
    internal sealed class StatusForecastTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        internal StatusForecast.Forecast Forecast;
        internal Font Font;

        public void OnPointerEnter(PointerEventData eventData)
        {
            StatusForecastTooltip.Show(Forecast, transform as RectTransform, Font);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            StatusForecastTooltip.Hide();
        }

        private void OnDisable()
        {
            StatusForecastTooltip.Hide();
        }
    }
}
