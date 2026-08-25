using PhoenixPoint.Geoscape.Levels.Factions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TFTV.TFTVIncidents;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TFTV.TFTVBaseRework
{
    /// <summary>
    /// Lets the player pick which Personnel are spent on setting up an Outpost or activating a Base,
    /// instead of letting the game choose. The pick is pre-filled with what the game would have taken,
    /// so confirming straight away keeps the old behaviour.
    /// </summary>
    internal static class BaseActivationPersonnelSelection
    {
        private const string OverlayName = "TFTV_BaseActivation_PersonnelSelection";

        private static readonly Color BackdropColor = new Color(0f, 0f, 0f, 0.75f);
        private static readonly Color PanelColor = new Color(0.07f, 0.08f, 0.12f, 0.98f);
        private static readonly Color RowNormalColor = new Color(0.12f, 0.14f, 0.18f, 0.9f);
        private static readonly Color RowSelectedColor = new Color(0.25f, 0.45f, 0.70f, 0.9f);
        private static readonly Color WarningColor = new Color(1f, 0.45f, 0.35f, 1f);
        private static readonly Color ConfirmColor = new Color(0.25f, 0.45f, 0.30f, 0.95f);
        private static readonly Color CancelColor = new Color(0.35f, 0.25f, 0.28f, 0.95f);
        private static readonly Color DisabledColor = new Color(0.22f, 0.22f, 0.26f, 0.9f);

        private const int TitleFontSize = 40;
        private const int BodyFontSize = 28;
        private const int RowFontSize = 26;
        private const float RowHeight = 56f;
        private const float ButtonHeight = 64f;

        private static GameObject _overlay;

        private sealed class Row
        {
            public PersonnelInfo Person;
            public int Weight;
            public bool Selected;
            public Image Background;
        }

        /// <summary>
        /// Opens the picker. Returns false when the UI could not be built or there is nothing to pick,
        /// in which case the caller should fall back to the automatic selection.
        /// </summary>
        internal static bool TryShow(
            Transform anchor,
            GeoPhoenixFaction faction,
            int requiredPersonnel,
            string actionLabel,
            Action<List<PersonnelInfo>> onConfirm)
        {
            try
            {
                if (anchor == null || faction == null || requiredPersonnel <= 0 || onConfirm == null)
                {
                    return false;
                }

                Canvas canvas = anchor.GetComponentInParent<Canvas>();
                if (canvas == null)
                {
                    return false;
                }

                List<PersonnelInfo> eligible = PersonnelData.GetPersonnelEligibleForBaseActivation(faction);
                List<PersonnelInfo> preselected = PersonnelData.PickDefaultPersonnelForBaseActivation(faction, requiredPersonnel);

                if (eligible.Count == 0 || preselected == null)
                {
                    return false;
                }

                Close();
                Build(canvas, eligible, preselected, requiredPersonnel, actionLabel, onConfirm);
                return _overlay != null;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                Close();
                return false;
            }
        }

        internal static void Close()
        {
            if (_overlay != null)
            {
                Object.Destroy(_overlay);
                _overlay = null;
            }
        }

        private static void Build(
            Canvas canvas,
            List<PersonnelInfo> eligible,
            List<PersonnelInfo> preselected,
            int requiredPersonnel,
            string actionLabel,
            Action<List<PersonnelInfo>> onConfirm)
        {
            _overlay = new GameObject(OverlayName, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(Image));
            _overlay.transform.SetParent(canvas.transform, false);
            // The host canvas may drive a layout group; the overlay fills the screen on its own.
            _overlay.AddComponent<LayoutElement>().ignoreLayout = true;

            Canvas overlayCanvas = _overlay.GetComponent<Canvas>();
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = 500;

            _overlay.GetComponent<Image>().color = BackdropColor;

            RectTransform overlayRect = _overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            panel.transform.SetParent(_overlay.transform, false);
            panel.GetComponent<Image>().color = PanelColor;

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.22f, 0.15f);
            panelRect.anchorMax = new Vector2(0.78f, 0.85f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup panelLayout = panel.GetComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(24, 24, 24, 24);
            panelLayout.spacing = 12f;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            AddLabel(panel.transform, TFTVCommonMethods.ConvertKeyToString("KEY_BASE_PERSONNEL_SELECT_TITLE"),
                TitleFontSize, Color.yellow, TextAnchor.MiddleCenter, 56f);

            AddLabel(panel.transform,
                string.Format(
                    CultureInfo.InvariantCulture,
                    TFTVCommonMethods.ConvertKeyToString("KEY_BASE_PERSONNEL_SELECT_REQUIRED"),
                    actionLabel,
                    requiredPersonnel),
                BodyFontSize, Color.white, TextAnchor.MiddleCenter, 40f);

            AddLabel(panel.transform, TFTVCommonMethods.ConvertKeyToString("KEY_BASE_PERSONNEL_SELECT_WARNING"),
                BodyFontSize, WarningColor, TextAnchor.MiddleCenter, 76f);

            Transform listContent = CreateScrollArea(panel.transform);

            HashSet<int> preselectedIds = new HashSet<int>(preselected.Select(person => person.Id));
            List<Row> rows = new List<Row>();

            Text counter = null;
            Image confirmImage = null;
            Button confirmButton = null;

            foreach (PersonnelInfo person in eligible)
            {
                Row row = new Row
                {
                    Person = person,
                    Weight = PersonnelData.GetActivationWeight(person.Character),
                    Selected = preselectedIds.Contains(person.Id)
                };

                CreateRow(listContent, row, () => RefreshFooter(rows, requiredPersonnel, counter, confirmImage, confirmButton));
                rows.Add(row);
            }

            counter = AddLabel(panel.transform, string.Empty, BodyFontSize, Color.white, TextAnchor.MiddleCenter, 40f);

            GameObject buttonRow = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            buttonRow.transform.SetParent(panel.transform, false);
            HorizontalLayoutGroup buttonLayout = buttonRow.GetComponent<HorizontalLayoutGroup>();
            buttonLayout.spacing = 16f;
            buttonLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonLayout.childControlWidth = true;
            buttonLayout.childControlHeight = true;
            buttonLayout.childForceExpandWidth = true;
            buttonLayout.childForceExpandHeight = false;
            buttonRow.AddComponent<LayoutElement>().minHeight = ButtonHeight;

            confirmButton = CreateButton(
                buttonRow.transform,
                TFTVCommonMethods.ConvertKeyToString("KEY_BASE_PERSONNEL_SELECT_CONFIRM"),
                ConfirmColor,
                () =>
                {
                    List<PersonnelInfo> chosen = rows.Where(r => r.Selected).Select(r => r.Person).ToList();
                    if (chosen.Sum(person => PersonnelData.GetActivationWeight(person.Character)) < requiredPersonnel)
                    {
                        return;
                    }

                    Close();
                    onConfirm(chosen);
                },
                out confirmImage);

            CreateButton(
                buttonRow.transform,
                TFTVCommonMethods.ConvertKeyToString("KEY_BASE_PERSONNEL_SELECT_CANCEL"),
                CancelColor,
                Close,
                out _);

            RefreshFooter(rows, requiredPersonnel, counter, confirmImage, confirmButton);
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
        }

        private static void RefreshFooter(
            List<Row> rows,
            int requiredPersonnel,
            Text counter,
            Image confirmImage,
            Button confirmButton)
        {
            int selectedWeight = rows.Where(row => row.Selected).Sum(row => row.Weight);
            bool enough = selectedWeight >= requiredPersonnel;

            if (counter != null)
            {
                counter.text = string.Format(
                    CultureInfo.InvariantCulture,
                    TFTVCommonMethods.ConvertKeyToString("KEY_BASE_PERSONNEL_SELECT_COUNTER"),
                    selectedWeight,
                    requiredPersonnel);
                counter.color = enough ? Color.white : WarningColor;
            }

            if (confirmButton != null)
            {
                confirmButton.interactable = enough;
            }

            if (confirmImage != null)
            {
                confirmImage.color = enough ? ConfirmColor : DisabledColor;
            }
        }

        private static Transform CreateScrollArea(Transform parent)
        {
            GameObject scrollView = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollView.transform.SetParent(parent, false);
            scrollView.AddComponent<LayoutElement>().flexibleHeight = 1f;

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
            viewport.transform.SetParent(scrollView.transform, false);
            viewport.GetComponent<Mask>().showMaskGraphic = true;
            viewport.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.08f, 0.9f);

            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);

            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = Vector2.zero;

            VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 4f;
            contentLayout.padding = new RectOffset(6, 6, 6, 6);
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scrollRect = scrollView.GetComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;

            return content.transform;
        }

        private static void CreateRow(Transform parent, Row row, Action onToggled)
        {
            GameObject go = new GameObject($"Personnel_{row.Person.Id}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().minHeight = RowHeight;

            row.Background = go.GetComponent<Image>();
            row.Background.color = row.Selected ? RowSelectedColor : RowNormalColor;

            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                try
                {
                    row.Selected = !row.Selected;
                    row.Background.color = row.Selected ? RowSelectedColor : RowNormalColor;
                    onToggled?.Invoke();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            });

            GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(go.transform, false);

            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16, 0);
            textRect.offsetMax = new Vector2(-16, 0);

            Text text = textGO.GetComponent<Text>();
            text.font = ResolveFont();
            text.text = DescribeRow(row);
            text.fontSize = RowFontSize;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private static string DescribeRow(Row row)
        {
            string name = row.Person.Character?.DisplayName ?? $"Personnel {row.Person.Id}";
            string assignment = DescribeAssignment(row.Person.Assignment);

            string affinity = LeaderSelection.TryGetCurrentAffinity(row.Person.Character, out LeaderSelection.AffinityApproach approach, out int rank)
                ? $"{LeaderSelection.GetApproachDisplayName(approach)} {rank}"
                : "-";

            string weight = row.Weight > 1
                ? string.Format(
                    CultureInfo.InvariantCulture,
                    TFTVCommonMethods.ConvertKeyToString("KEY_BASE_PERSONNEL_SELECT_COUNTS_AS"),
                    row.Weight)
                : string.Empty;

            return string.IsNullOrEmpty(weight)
                ? $"{name}   |   {assignment}   |   {affinity}"
                : $"{name}   |   {assignment}   |   {affinity}   |   {weight}";
        }

        private static string DescribeAssignment(PersonnelAssignment assignment)
        {
            switch (assignment)
            {
                case PersonnelAssignment.Research: return "Research";
                case PersonnelAssignment.Manufacturing: return "Manufacturing";
                default: return "Unassigned";
            }
        }

        private static Text AddLabel(Transform parent, string caption, int fontSize, Color color, TextAnchor anchor, float minHeight)
        {
            GameObject go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().minHeight = minHeight;

            Text text = go.GetComponent<Text>();
            text.font = ResolveFont();
            text.text = caption;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button CreateButton(Transform parent, string caption, Color color, Action onClick, out Image image)
        {
            GameObject go = new GameObject($"Btn_{caption}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().minHeight = ButtonHeight;

            image = go.GetComponent<Image>();
            image.color = color;

            Button button = go.GetComponent<Button>();
            button.onClick.AddListener(() =>
            {
                try { onClick?.Invoke(); } catch (Exception e) { TFTVLogger.Error(e); }
            });

            GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(go.transform, false);

            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text text = textGO.GetComponent<Text>();
            text.font = ResolveFont();
            text.text = caption;
            text.fontSize = BodyFontSize;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;

            return button;
        }

        private static Font ResolveFont()
        {
            return PersonnelManagementUI.PuristaSemibold ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
