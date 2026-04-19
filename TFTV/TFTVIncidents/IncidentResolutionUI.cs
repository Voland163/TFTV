using Base.Core;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View.ViewControllers.SiteEncounters;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.Entities.Abilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TFTV.TFTVIncidents
{
    internal class IncidentResolutionUI
    {
        [HarmonyPatch(typeof(UIModuleSiteEncounters), "SetEncounter")]
        internal static class GeoscapeEventCrewListPatch
        {
            private const string CrewRootName = "[Mod]EventCrewListRoot";
            private const string HeaderName = "[Mod]EventCrewListHeader";
            private const string CrewGridName = "[Mod]EventCrewListGrid";
            private const string AffinityTagDefName = "Affinity_SkillTagDef";
            private const string InlineAffinityIconName = "[Mod]AffinityAbilityIcon";
            private const string CrewRowHighlightName = "[Mod]CrewRowHighlight";

            private const string ChoiceIconsRootName = "[Mod]ChoiceApproachIcons";
            private const string ChoiceIconNamePrefix = "[Mod]ChoiceApproachIcon_";

            private const float CrewPanelTopPadding = 8f;
            private const float GridSpacingX = 10f;
            private const float GridSpacingY = 6f;
            private const float HeaderToGridSpacing = 50f;
            private const float MaxGridWidth = 1000f;
            private const float FallbackGridWidth = 900f;
            private const float MinCellHeight = 42f;
            private const int MaxVisibleEntries = 8;
            private const int GridColumns = 2;

            private const float ApproachIconSize = 72f;
            private const float ApproachIconSpacing = 8f;
            private const float ApproachIconButtonGap = 14f;
            private const float ApproachIconOutlineWidth = 3f;

            private const string GainRowName = "[Mod]EventCrewGainRow";
            private const float GainIconSize = 52f;
            private const float GainRowSpacing = 8f;
            private const float ButtonShrinkMultiplier = 2f;

            private static readonly Color ApproachSelectedOutlineColor = new Color(1f, 0.84f, 0f, 1f);
            private static readonly Color ApproachLockedTint = new Color(1f, 1f, 1f, 0.3f);
            private static readonly Color ApproachUnselectedTint = new Color(1f, 1f, 1f, 0.55f);

            public static Func<SoldierSlotController> SoldierSlotPrefabProvider;
            public static Func<GeoCharacter, Sprite> ExtraIconResolver;
            public static Func<GeoCharacter, bool> CrewFilter;

            private static SoldierSlotController _selectedRow;
            private static int _selectedLeaderId = -1;
            private static int _selectedVehicleId = -1;
            private static string _selectedEventId = string.Empty;

            private static Font _cachedFont;

            // Approach selection state per choice index (0 or 1).
            private static readonly Dictionary<int, List<LeaderSelection.AffinityApproach>> _choiceApproaches =
                new Dictionary<int, List<LeaderSelection.AffinityApproach>>();
            private static readonly Dictionary<int, LeaderSelection.AffinityApproach?> _selectedApproach =
                new Dictionary<int, LeaderSelection.AffinityApproach?>();
            private static readonly Dictionary<int, bool> _approachSelectionLocked =
                new Dictionary<int, bool>();

            private static Text _headerGainLabel;
            private static Image _headerGainIcon;
            private static Text _headerGainDetail;
            private static ApproachIconTooltipTrigger _headerGainTooltip;
            private static GeoCharacter _currentSelectedCharacter;
            private static int _lastInteractedChoiceIndex;

            private sealed class CrewRowHighlightState : MonoBehaviour
            {
                public Image Background;
                public Outline Border;
                public Color BackgroundNormal;
                public Color BorderNormal;
                public bool Initialized;
            }

            private sealed class ChoiceButtonVisualState : MonoBehaviour
            {
                public Text Label;
                public string BaseText;
            }

            private sealed class ApproachIconState : MonoBehaviour
            {
                public int ChoiceIndex;
                public LeaderSelection.AffinityApproach Approach;
                public SiteBaseChoiceButton ParentChoiceButton;
            }

          

            private sealed class ApproachIconTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
            {
                public LeaderSelection.AffinityApproach Approach;
                public int ChoiceIndex = -1;

                private const string TooltipObjectName = "[Mod]ApproachIconTooltip";
                private const float TooltipWidth = 700f;
                private const float TooltipOffsetY = 16f;
                private const float TooltipOffsetX = 100f;
                private const int TooltipFontSize = 36;
                private const float TooltipPadH = 18f;
                private const float TooltipPadV = 14f;

                private static RectTransform _tooltipRect;
                private static Text _tooltipLabel;
                private static CanvasGroup _tooltipGroup;

                public void OnPointerEnter(PointerEventData eventData)
                {
                    ShowFor(this);
                }

                public void OnPointerExit(PointerEventData eventData)
                {
                    Hide();
                }

                private void OnDisable()
                {
                    Hide();
                }

                internal static void DestroyTooltip()
                {
                    if (_tooltipRect != null)
                    {
                        UnityEngine.Object.Destroy(_tooltipRect.gameObject);
                        _tooltipRect = null;
                        _tooltipLabel = null;
                        _tooltipGroup = null;
                    }
                }

                private static void ShowFor(ApproachIconTooltipTrigger trigger)
                {
                    RectTransform tooltip = EnsureTooltip(trigger.transform);
                    if (tooltip == null)
                    {
                        return;
                    }

                    string content = BuildContent(trigger.Approach);
                    if (string.IsNullOrEmpty(content))
                    {
                        Hide();
                        return;
                    }

                    _tooltipLabel.text = content;
                    LayoutRebuilder.ForceRebuildLayoutImmediate(_tooltipLabel.rectTransform);
                    LayoutRebuilder.ForceRebuildLayoutImmediate(tooltip);

                    PositionAbove(tooltip, trigger.transform as RectTransform, trigger.ChoiceIndex);

                    tooltip.gameObject.SetActive(true);
                    tooltip.transform.SetAsLastSibling();
                    if (_tooltipGroup != null)
                    {
                        _tooltipGroup.alpha = 1f;
                    }
                }

                private static void Hide()
                {
                    if (_tooltipRect != null && _tooltipRect.gameObject.activeSelf)
                    {
                        if (_tooltipGroup != null)
                        {
                            _tooltipGroup.alpha = 0f;
                        }
                        _tooltipRect.gameObject.SetActive(false);
                    }
                }

                private static RectTransform EnsureTooltip(Transform reference)
                {
                    if (_tooltipRect != null)
                    {
                        return _tooltipRect;
                    }

                    Canvas canvas = reference.GetComponentInParent<Canvas>();
                    if (canvas == null)
                    {
                        return null;
                    }

                    GameObject go = new GameObject(
                        TooltipObjectName,
                        typeof(RectTransform),
                        typeof(CanvasGroup),
                        typeof(VerticalLayoutGroup),
                        typeof(ContentSizeFitter),
                        typeof(Image));
                    go.transform.SetParent(canvas.transform, false);

                    _tooltipRect = go.GetComponent<RectTransform>();
                    _tooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
                    _tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
                    _tooltipRect.pivot = new Vector2(0.5f, 0f);
                    _tooltipRect.sizeDelta = new Vector2(TooltipWidth, 0f);

                    _tooltipGroup = go.GetComponent<CanvasGroup>();
                    _tooltipGroup.alpha = 0f;
                    _tooltipGroup.blocksRaycasts = false;
                    _tooltipGroup.interactable = false;

                    Image bg = go.GetComponent<Image>();
                    bg.color = new Color(0.06f, 0.06f, 0.10f, 0.94f);
                    bg.raycastTarget = false;

                    VerticalLayoutGroup vlg = go.GetComponent<VerticalLayoutGroup>();
                    vlg.padding = new RectOffset((int)TooltipPadH, (int)TooltipPadH, (int)TooltipPadV, (int)TooltipPadV);
                    vlg.childAlignment = TextAnchor.UpperLeft;
                    vlg.childControlWidth = true;
                    vlg.childControlHeight = true;
                    vlg.childForceExpandWidth = true;
                    vlg.childForceExpandHeight = false;

                    ContentSizeFitter csf = go.GetComponent<ContentSizeFitter>();
                    csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                    csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                    GameObject textGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
                    textGO.transform.SetParent(go.transform, false);

                    _tooltipLabel = textGO.GetComponent<Text>();
                    _tooltipLabel.fontSize = TooltipFontSize;
                    _tooltipLabel.color = Color.white;
                    _tooltipLabel.alignment = TextAnchor.UpperLeft;
                    _tooltipLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
                    _tooltipLabel.verticalOverflow = VerticalWrapMode.Overflow;
                    _tooltipLabel.supportRichText = true;
                    _tooltipLabel.raycastTarget = false;

                    Font font = _cachedFont;
                    if (font == null)
                    {
                        Text anyText = canvas.GetComponentInChildren<Text>(true);
                        if (anyText != null)
                        {
                            font = anyText.font;
                        }
                    }
                    _tooltipLabel.font = font;

                    go.SetActive(false);
                    return _tooltipRect;
                }

                private static void PositionAbove(RectTransform tooltip, RectTransform iconRect, int choiceIndex)
                {
                    if (tooltip == null || iconRect == null)
                    {
                        return;
                    }

                    Canvas canvas = tooltip.GetComponentInParent<Canvas>();
                    if (canvas == null)
                    {
                        return;
                    }

                    RectTransform canvasRect = canvas.transform as RectTransform;
                    if (canvasRect == null)
                    {
                        return;
                    }

                    Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

                    Vector3[] corners = new Vector3[4];
                    iconRect.GetWorldCorners(corners);
                    Vector3 topCenter = (corners[1] + corners[2]) * 0.5f;
                    Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, topCenter);

                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRect, screenPoint, cam, out Vector2 localPoint))
                    {
                        float offsetX = (choiceIndex == 0) ? TooltipOffsetX : 0f;
                        tooltip.anchoredPosition = new Vector2(localPoint.x + offsetX, localPoint.y + TooltipOffsetY);
                    }
                }

                private static string BuildContent(LeaderSelection.AffinityApproach approach)
                {
                    string key;
                    switch (approach)
                    {
                        case LeaderSelection.AffinityApproach.PsychoSociology:
                            key = "KEY_AFFINITY_PSYCHO_SOCIOLOGY_ALL_BENEFITS";
                            break;
                        case LeaderSelection.AffinityApproach.Exploration:
                            key = "KEY_AFFINITY_EXPLORATION_ALL_BENEFITS";
                            break;
                        case LeaderSelection.AffinityApproach.Occult:
                            key = "KEY_AFFINITY_OCCULT_ALL_BENEFITS";
                            break;
                        case LeaderSelection.AffinityApproach.Biotech:
                            key = "KEY_AFFINITY_BIOTECH_ALL_BENEFITS";
                            break;
                        case LeaderSelection.AffinityApproach.Machinery:
                            key = "KEY_AFFINITY_MACHINERY_ALL_BENEFITS";
                            break;
                        case LeaderSelection.AffinityApproach.Compute:
                            key = "KEY_AFFINITY_COMPUTE_ALL_BENEFITS";
                            break;
                        default:
                            return approach.ToString();
                    }

                    return new LocalizedTextBind() { LocalizationKey = key }.Localize();
                }
            }

            private static void CleanupChoiceButtonDecorations(UIModuleSiteEncounters module)
            {
                if (module?.ChoiceButtonsContainer == null)
                {
                    return;
                }

                SiteBaseChoiceButton[] buttons = module.ChoiceButtonsContainer.GetComponentsInChildren<SiteBaseChoiceButton>(true);
                if (buttons == null)
                {
                    return;
                }

                foreach (SiteBaseChoiceButton button in buttons)
                {
                    if (button == null)
                    {
                        continue;
                    }

                    Transform iconRoot = button.transform.Find(ChoiceIconsRootName);
                    if (iconRoot != null)
                    {
                        UnityEngine.Object.DestroyImmediate(iconRoot.gameObject); // was Destroy — deferred destroy caused reuse in same frame
                    }

                    ChoiceButtonVisualState state = button.GetComponent<ChoiceButtonVisualState>();
                    if (state != null)
                    {
                        UnityEngine.Object.DestroyImmediate(state); // was Destroy — deferred destroy caused stale BaseText to persist
                    }
                }
            }

            public static void Postfix(UIModuleSiteEncounters __instance, GeoscapeEvent geoEvent, bool pagingEvent)
            {
                if (!TFTVBaseRework.BaseReworkUtils.BaseReworkEnabled)
                {
                    return;
                }

                if (__instance == null || geoEvent?.Context == null || pagingEvent)
                {
                    return;
                }

                Transform parent = __instance.SiteEncounterTextContainer != null ? __instance.SiteEncounterTextContainer.transform : null;
                if (parent == null)
                {
                    return;
                }

                RemovePreviousRows(parent);
                IncidentIntroTutorialPanel.ClearPanel(parent);   // ADD THIS LINE
                ApproachIconTooltipTrigger.DestroyTooltip();
                RestoreChoiceButtons(__instance);
                CleanupChoiceButtonDecorations(__instance);
                _selectedRow = null;
                ResetSelectedLeaderContext(null, null);

                if (!IsIncidentIntroEvent(geoEvent))
                {
                    return;
                }

                GeoVehicle vehicle = ResolveVehicle(geoEvent);
                if (vehicle == null)
                {
                    return;
                }

                ResetSelectedLeaderContext(geoEvent, vehicle);

                List<GeoCharacter> crew = ResolveCrew(vehicle);
                if (crew.Count == 0)
                {
                    return;
                }

                SoldierSlotController rowPrefab = ResolveSoldierSlotPrefab();
                if (rowPrefab == null)
                {
                    Debug.LogWarning("[GeoscapeEventCrewListPatch] SoldierSlotController prefab could not be resolved.");
                    return;
                }

                if (__instance.EncounterDescriptionText != null && __instance.EncounterDescriptionText.font != null)
                {
                    _cachedFont = __instance.EncounterDescriptionText.font;
                }

                List<GeoEventChoice> choices = geoEvent.EventData?.Choices ?? new List<GeoEventChoice>();
                GeoCharacter initialLeader = ResolveInitialLeader(vehicle, choices);

                GameObject root = new GameObject(CrewRootName, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                root.transform.SetParent(parent, false);

                RectTransform rootRect = root.GetComponent<RectTransform>();
                ConfigureRootRect(rootRect, __instance.EncounterDescriptionText);

                VerticalLayoutGroup verticalLayout = root.GetComponent<VerticalLayoutGroup>();
                verticalLayout.childAlignment = TextAnchor.UpperCenter;
                verticalLayout.childControlWidth = true;
                verticalLayout.childControlHeight = true;
                verticalLayout.childForceExpandWidth = false;
                verticalLayout.childForceExpandHeight = false;
                verticalLayout.padding = new RectOffset(0, 0, 4, 4);
                verticalLayout.spacing = 4f;

                ContentSizeFitter rootFitter = root.GetComponent<ContentSizeFitter>();
                rootFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                rootFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                Text header = CreateHeader(root.transform, __instance.EncounterDescriptionText);
                CreateGainRow(root.transform, __instance.EncounterDescriptionText);
                GameObject headerSpacer = new GameObject("[Mod]CrewHeaderSpacer", typeof(RectTransform), typeof(LayoutElement));
                headerSpacer.transform.SetParent(root.transform, false);
                LayoutElement spacerLayout = headerSpacer.GetComponent<LayoutElement>();
                spacerLayout.minHeight = HeaderToGridSpacing;

                float panelWidth = ResolvePanelWidth(__instance.EncounterDescriptionText);

                GameObject grid = new GameObject(CrewGridName, typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
                grid.transform.SetParent(root.transform, false);
                ConfigureGrid(
                    grid.GetComponent<RectTransform>(),
                    grid.GetComponent<GridLayoutGroup>(),
                    grid.GetComponent<ContentSizeFitter>(),
                    panelWidth,
                    rowPrefab);

                int entriesToShow = Math.Min(crew.Count, MaxVisibleEntries);
                List<SoldierSlotController> rows = new List<SoldierSlotController>(entriesToShow);
                Dictionary<int, SoldierSlotController> rowsById = new Dictionary<int, SoldierSlotController>();

                for (int i = 0; i < entriesToShow; i++)
                {
                    GeoCharacter character = crew[i];
                    SoldierSlotController row = UnityEngine.Object.Instantiate(rowPrefab, grid.transform, false);
                    row.gameObject.name = $"[Mod]CrewRow_{character.GetName()}";
                    row.gameObject.SetActive(true);

                    NormalizeRowRect(row.transform as RectTransform);
                    row.SetSoldierData((ICommonActor)character);
                    ResetRowSelectionVisualState(row);

                    Sprite abilityIcon = ResolveAbilityIcon(character);
                    SetAffinityIconAfterName(row, abilityIcon);

                    if (character != null && character.Id > 0)
                    {
                        rowsById[character.Id] = row;
                    }

                    GeoCharacter selectedCharacter = character;
                    SoldierSlotController selectedRow = row;
                    row.ActorSelected = (ICommonActor _) =>
                    {
                        SetSelectedRow(selectedRow);
                        SetSelectedLeader(selectedCharacter, geoEvent, vehicle);
                        UpdateHeaderForSelectedOperative(header, selectedCharacter, geoEvent, vehicle);
                        UpdateChoiceButtonsForSelectedOperative(__instance, geoEvent, vehicle, selectedCharacter);
                        RefreshHeaderGainDisplay();
                    };

                    rows.Add(row);
                }

                if (rows.Count > 0)
                {
                    GeoCharacter selectedCharacter = initialLeader;
                    SoldierSlotController selectedRow = null;

                    if (selectedCharacter != null && selectedCharacter.Id > 0)
                    {
                        rowsById.TryGetValue(selectedCharacter.Id, out selectedRow);
                    }

                    if (selectedRow == null)
                    {
                        selectedRow = rows[0];
                        selectedCharacter = crew[0];
                    }

                    SetSelectedRow(selectedRow);
                    SetSelectedLeader(selectedCharacter, geoEvent, vehicle);
                    UpdateHeaderForSelectedOperative(header, selectedCharacter, geoEvent, vehicle);
                    UpdateChoiceButtonsForSelectedOperative(__instance, geoEvent, vehicle, selectedCharacter);
                    RefreshHeaderGainDisplay();

                    // Shrink choice buttons after approach data is populated to make room for icons.
                    AdjustChoiceButtons(__instance, geoEvent);
                }

                IncidentIntroTutorialPanel.TryShowPanel(__instance, geoEvent);   // ADD THIS LINE after crew list is built, before AdjustChoiceButtons
            }

            private static void UpdateChoiceButtonsForSelectedOperative(
                UIModuleSiteEncounters module,
                GeoscapeEvent geoEvent,
                GeoVehicle vehicle,
                GeoCharacter selectedCharacter)
            {
                if (module == null || geoEvent?.EventData?.Choices == null || module.ChoiceButtonsContainer == null)
                {
                    return;
                }

                UpdateApproachSelectionForOperative(geoEvent, selectedCharacter);

                SiteBaseChoiceButton[] buttons = module.ChoiceButtonsContainer.GetComponentsInChildren<SiteBaseChoiceButton>(true);
                if (buttons == null || buttons.Length == 0)
                {
                    return;
                }

                int max = Math.Min(2, Math.Min(geoEvent.EventData.Choices.Count, buttons.Length));

                for (int i = 0; i < max; i++)
                {
                    SiteBaseChoiceButton button = buttons[i];
                    if (button == null)
                    {
                        continue;
                    }

                    ChoiceButtonVisualState state = EnsureChoiceButtonVisualState(button);
                    if (state == null || state.Label == null)
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(state.BaseText))
                    {
                        string fromLabel = state.Label.text;
                        if (!string.IsNullOrEmpty(fromLabel))
                        {
                            state.BaseText = fromLabel;
                        }
                        else if (button.Choice?.Text != null && geoEvent?.Context != null)
                        {
                            state.BaseText = geoEvent.Context.ReplaceEventTokens(button.Choice.Text.Localize(null));
                        }
                    }

                    string baseText = string.IsNullOrEmpty(state.BaseText) ? (state.Label.text ?? string.Empty) : state.BaseText;
                    string hoursText = ResolveApproachHoursText(geoEvent, vehicle, i, selectedCharacter);
                    state.Label.text = $"{baseText} ({hoursText})";

                    ApplyChoiceIcons(button, i);
                }
            }

            private static ChoiceButtonVisualState EnsureChoiceButtonVisualState(SiteBaseChoiceButton button)
            {
                ChoiceButtonVisualState state = button.GetComponent<ChoiceButtonVisualState>();
                if (state == null)
                {
                    state = button.gameObject.AddComponent<ChoiceButtonVisualState>();
                }

                if (state.Label == null)
                {
                    Text[] texts = button.GetComponentsInChildren<Text>(true);
                    state.Label = texts
                        .Where(t => t != null)
                        .OrderByDescending(t => string.IsNullOrEmpty(t.text) ? 0 : t.text.Length)
                        .FirstOrDefault();
                }

                if (state.Label != null && string.IsNullOrEmpty(state.BaseText) && !string.IsNullOrEmpty(state.Label.text))
                {
                    state.BaseText = state.Label.text;
                }

                return state;
            }

            // ── Choice button sizing ─────────────────────────────────────

            /// <summary>
            /// Computes the horizontal space (in pixels) needed for approach icons
            /// on the given choice side: total icon width + inter-icon spacing + gap
            /// between icons and the button edge. Returns 0 if no icons are needed.
            /// </summary>
            private static float ComputeApproachPadding(int choiceIndex)
            {
                if (!_choiceApproaches.TryGetValue(choiceIndex, out List<LeaderSelection.AffinityApproach> approaches)
                    || approaches == null || approaches.Count == 0)
                {
                    return 0f;
                }

                int count = approaches.Count;
                float totalIconWidth = (count * ApproachIconSize) + (Mathf.Max(0, count - 1) * ApproachIconSpacing);
                return totalIconWidth + ApproachIconButtonGap;
            }

            /// <summary>
            /// Stores the original <see cref="GridLayoutGroup"/> cellSize and padding
            /// on the <c>ChoiceButtonsContainer</c> so they can be restored when
            /// switching away from an incident intro event.
            /// </summary>
            private sealed class GridPaddingState : MonoBehaviour
            {
                public Vector2 OriginalCellSize;
                public RectOffset OriginalPadding;
            }

            /// <summary>
            /// Shrinks choice buttons by modifying the parent
            /// <see cref="GridLayoutGroup"/>'s <c>cellSize</c> and <c>padding</c>.
            /// Individual button resizing is impossible because the GridLayoutGroup
            /// overrides all child RectTransforms to its cellSize.
            /// </summary>
            private static void AdjustChoiceButtons(UIModuleSiteEncounters module, GeoscapeEvent geoEvent)
            {
                if (module?.ChoiceButtonsContainer == null || geoEvent?.EventData?.Choices == null)
                {
                    return;
                }

                GridLayoutGroup grid = module.ChoiceButtonsContainer.GetComponent<GridLayoutGroup>();
                if (grid == null)
                {
                    return;
                }

                float pad0 = ComputeApproachPadding(0);
                float pad1 = ComputeApproachPadding(1);
                if (pad0 <= 0f && pad1 <= 0f)
                {
                    return;
                }

                GridPaddingState state = grid.GetComponent<GridPaddingState>();
                if (state == null)
                {
                    state = grid.gameObject.AddComponent<GridPaddingState>();
                    state.OriginalCellSize = grid.cellSize;
                    state.OriginalPadding = new RectOffset(
                        grid.padding.left, grid.padding.right,
                        grid.padding.top, grid.padding.bottom);
                }

                // Double the shrink so buttons are visually smaller than the icon strip alone requires.
                float shrunkPad0 = pad0 * ButtonShrinkMultiplier;
                float shrunkPad1 = pad1 * ButtonShrinkMultiplier;

                float newCellWidth = state.OriginalCellSize.x - (shrunkPad0 + shrunkPad1) * 0.5f;
                grid.cellSize = new Vector2(newCellWidth, state.OriginalCellSize.y);
                grid.padding = new RectOffset(
                    state.OriginalPadding.left + Mathf.CeilToInt(shrunkPad0),
                    state.OriginalPadding.right + Mathf.CeilToInt(shrunkPad1),
                    state.OriginalPadding.top,
                    state.OriginalPadding.bottom);
            }

            /// <summary>
            /// Restores the original <see cref="GridLayoutGroup"/> cellSize and
            /// padding saved by <see cref="AdjustChoiceButtons"/>.
            /// </summary>
            private static void RestoreChoiceButtons(UIModuleSiteEncounters module)
            {
                if (module?.ChoiceButtonsContainer == null)
                {
                    return;
                }

                GridLayoutGroup grid = module.ChoiceButtonsContainer.GetComponent<GridLayoutGroup>();
                if (grid == null)
                {
                    return;
                }

                GridPaddingState state = grid.GetComponent<GridPaddingState>();
                if (state == null)
                {
                    return;
                }

                grid.cellSize = state.OriginalCellSize;
                grid.padding = new RectOffset(
                    state.OriginalPadding.left, state.OriginalPadding.right,
                    state.OriginalPadding.top, state.OriginalPadding.bottom);

                UnityEngine.Object.DestroyImmediate(state); // was Destroy — deferred destroy caused AdjustChoiceButtons to reuse stale state in same frame, progressively compounding the shrink
            }


            // ── Approach selection logic ──────────────────────────────────

            private static void UpdateApproachSelectionForOperative(GeoscapeEvent geoEvent, GeoCharacter selectedCharacter)
            {
                _choiceApproaches.Clear();
                _selectedApproach.Clear();
                _approachSelectionLocked.Clear();

                List<GeoEventChoice> choices = geoEvent?.EventData?.Choices;
                if (choices == null)
                {
                    return;
                }

                bool hasAffinity = LeaderSelection.TryGetCurrentAffinity(
                    selectedCharacter, out LeaderSelection.AffinityApproach charApproach, out int charRank);

                int max = Math.Min(2, choices.Count);
                for (int i = 0; i < max; i++)
                {
                    GeoEventChoice choice = choices[i];
                    string tokens = LeaderSelection.ExtractApproachTokens(choice?.Text?.LocalizationKey, i);
                    List<LeaderSelection.AffinityApproach> approaches = LeaderSelection.ParseApproachTokens(tokens);
                    _choiceApproaches[i] = approaches;

                    TFTVLogger.Always($"[IncidentUI] Choice {i}: key='{choice?.Text?.LocalizationKey}' tokens='{tokens}' approachCount={approaches.Count}");

                    if (approaches.Count == 0)
                    {
                        _selectedApproach[i] = null;
                        _approachSelectionLocked[i] = true;
                        continue;
                    }

                    if (hasAffinity)
                    {
                        if (approaches.Contains(charApproach))
                        {
                            // Operative has a matching affinity → auto-select, lock.
                            _selectedApproach[i] = charApproach;
                            _approachSelectionLocked[i] = true;
                        }
                        else
                        {
                            // Operative has a non-matching affinity → no selection possible, lock.
                            _selectedApproach[i] = null;
                            _approachSelectionLocked[i] = true;
                        }
                    }
                    else
                    {
                        // No affinity → either selectable, first selected by default.
                        _selectedApproach[i] = approaches[0];
                        _approachSelectionLocked[i] = false;
                    }
                }
            }

            private static Sprite GetApproachSprite(LeaderSelection.AffinityApproach approach)
            {
                PassiveModifierAbilityDef ability = LeaderSelection.GetAffinityAbility(approach, 1);
                return ability?.ViewElementDef?.SmallIcon;
            }

            private static void OnApproachIconClicked(
    int choiceIndex,
    LeaderSelection.AffinityApproach approach,
    SiteBaseChoiceButton parentButton)
            {
                if (_approachSelectionLocked.TryGetValue(choiceIndex, out bool locked) && locked)
                {
                    return;
                }

                _selectedApproach[choiceIndex] = approach;
                _lastInteractedChoiceIndex = choiceIndex;
                RefreshApproachIconVisuals(parentButton, choiceIndex);
                RefreshHeaderGainDisplay();
            }

            private static void RefreshApproachIconVisuals(SiteBaseChoiceButton button, int choiceIndex)
            {
                if (button == null)
                {
                    return;
                }

                Transform root = button.transform.Find(ChoiceIconsRootName);
                if (root == null)
                {
                    return;
                }

                _selectedApproach.TryGetValue(choiceIndex, out LeaderSelection.AffinityApproach? selected);
                _approachSelectionLocked.TryGetValue(choiceIndex, out bool locked);

                for (int i = 0; i < root.childCount; i++)
                {
                    Transform child = root.GetChild(i);
                    ApproachIconState state = child.GetComponent<ApproachIconState>();
                    if (state == null)
                    {
                        continue;
                    }

                    Image img = child.GetComponent<Image>();
                    Outline outline = child.GetComponent<Outline>();
                    bool isSelected = selected.HasValue && selected.Value == state.Approach;

                    if (img != null)
                    {
                        if (isSelected)
                        {
                            img.color = Color.white;
                        }
                        else if (locked)
                        {
                            img.color = ApproachLockedTint;
                        }
                        else
                        {
                            img.color = ApproachUnselectedTint;
                        }
                    }

                    if (outline != null)
                    {
                        outline.effectColor = isSelected ? ApproachSelectedOutlineColor : Color.clear;
                    }
                }
            }

            /// <summary>
            /// Returns the single selected approach token for the given choice, or false
            /// if no specific approach was selected (e.g. operative has a non-matching affinity).
            /// Used by Resolution.StartTimedProblem to narrow stored approach tokens for award tracking.
            /// </summary>
            internal static bool TryGetSelectedApproachToken(int choiceIndex, out string token)
            {
                token = string.Empty;
                if (_selectedApproach.TryGetValue(choiceIndex, out LeaderSelection.AffinityApproach? selected) && selected.HasValue)
                {
                    token = ApproachToToken(selected.Value);
                    return !string.IsNullOrEmpty(token);
                }
                return false;
            }

            private static string ApproachToToken(LeaderSelection.AffinityApproach approach)
            {
                switch (approach)
                {
                    case LeaderSelection.AffinityApproach.PsychoSociology: return "P";
                    case LeaderSelection.AffinityApproach.Exploration: return "E";
                    case LeaderSelection.AffinityApproach.Occult: return "O";
                    case LeaderSelection.AffinityApproach.Biotech: return "B";
                    case LeaderSelection.AffinityApproach.Machinery: return "M";
                    case LeaderSelection.AffinityApproach.Compute: return "C";
                    default: return string.Empty;
                }
            }

            // ── Approach icons on choice buttons ─────────────────────────

            private static void ApplyChoiceIcons(SiteBaseChoiceButton button, int choiceIndex)
            {
                if (button == null)
                {
                    return;
                }

                RectTransform buttonRect = button.GetComponent<RectTransform>();
                if (buttonRect == null)
                {
                    return;
                }

                _choiceApproaches.TryGetValue(choiceIndex, out List<LeaderSelection.AffinityApproach> approaches);
                int count = approaches != null ? approaches.Count : 0;

                Transform existingRoot = button.transform.Find(ChoiceIconsRootName);
                GameObject rootObject;
                if (existingRoot == null)
                {
                    rootObject = new GameObject(ChoiceIconsRootName, typeof(RectTransform), typeof(HorizontalLayoutGroup));
                    rootObject.transform.SetParent(button.transform, false);
                }
                else
                {
                    rootObject = existingRoot.gameObject;
                }

                RectTransform rootRect = rootObject.GetComponent<RectTransform>();
                HorizontalLayoutGroup h = rootObject.GetComponent<HorizontalLayoutGroup>();
                h.childAlignment = TextAnchor.MiddleCenter;
                h.childControlHeight = true;
                h.childControlWidth = true;
                h.childForceExpandHeight = false;
                h.childForceExpandWidth = false;
                h.spacing = ApproachIconSpacing;

                float totalWidth = (count * ApproachIconSize) + (Mathf.Max(0, count - 1) * h.spacing);
                rootRect.sizeDelta = new Vector2(count > 0 ? totalWidth : 0f, ApproachIconSize + 4f);

                // Choice 0 => left side outside; Choice 1 => right side outside.
                bool leftSide = (choiceIndex == 0);
                rootRect.anchorMin = new Vector2(leftSide ? 0f : 1f, 0.5f);
                rootRect.anchorMax = new Vector2(leftSide ? 0f : 1f, 0.5f);
                rootRect.pivot = new Vector2(leftSide ? 1f : 0f, 0.5f);
                rootRect.anchoredPosition = new Vector2(leftSide ? -ApproachIconButtonGap : ApproachIconButtonGap, 0f);

                _approachSelectionLocked.TryGetValue(choiceIndex, out bool locked);

                // Create/update exactly the icons needed.
                for (int i = 0; i < count; i++)
                {
                    string iconName = ChoiceIconNamePrefix + i;
                    Transform iconTransform = rootObject.transform.Find(iconName);
                    GameObject iconObject;

                    if (iconTransform == null)
                    {
                        iconObject = new GameObject(iconName, typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(Outline));
                        iconObject.transform.SetParent(rootObject.transform, false);
                    }
                    else
                    {
                        iconObject = iconTransform.gameObject;
                    }

                    LayoutElement le = iconObject.GetComponent<LayoutElement>();
                    if (le == null) le = iconObject.AddComponent<LayoutElement>();
                    le.preferredWidth = ApproachIconSize;
                    le.preferredHeight = ApproachIconSize;
                    le.minWidth = ApproachIconSize;
                    le.minHeight = ApproachIconSize;

                    Image img = iconObject.GetComponent<Image>();
                    if (img == null) img = iconObject.AddComponent<Image>();
                    img.raycastTarget = true;

                    Outline outline = iconObject.GetComponent<Outline>();
                    if (outline == null) outline = iconObject.AddComponent<Outline>();
                    outline.effectDistance = new Vector2(ApproachIconOutlineWidth, ApproachIconOutlineWidth);
                    outline.useGraphicAlpha = false;

                    // Button intercepts clicks so the parent SiteBaseChoiceButton is not triggered.
                    Button iconButton = iconObject.GetComponent<Button>();
                    if (iconButton == null) iconButton = iconObject.AddComponent<Button>();
                    iconButton.transition = Selectable.Transition.None;
                    iconButton.navigation = new Navigation { mode = Navigation.Mode.None };
                    iconButton.targetGraphic = img;
                    iconButton.onClick.RemoveAllListeners();

                    LeaderSelection.AffinityApproach approach = approaches[i];
                    Sprite sprite = GetApproachSprite(approach);
                    img.sprite = sprite;
                    img.enabled = sprite != null;

                    ApproachIconState iconState = iconObject.GetComponent<ApproachIconState>();
                    if (iconState == null) iconState = iconObject.AddComponent<ApproachIconState>();
                    iconState.ChoiceIndex = choiceIndex;
                    iconState.Approach = approach;
                    iconState.ParentChoiceButton = button;

                    ApproachIconTooltipTrigger tooltipTrigger = iconObject.GetComponent<ApproachIconTooltipTrigger>();
                    if (tooltipTrigger == null) tooltipTrigger = iconObject.AddComponent<ApproachIconTooltipTrigger>();
                    tooltipTrigger.Approach = approach;
                    tooltipTrigger.ChoiceIndex = choiceIndex;

                    if (!locked)
                    {
                        int capturedChoice = choiceIndex;
                        LeaderSelection.AffinityApproach capturedApproach = approach;
                        SiteBaseChoiceButton capturedButton = button;
                        iconButton.onClick.AddListener(() => OnApproachIconClicked(capturedChoice, capturedApproach, capturedButton));
                    }

                    iconObject.SetActive(sprite != null);
                }

                // Remove stale icon children left over from a previous render with more approaches.
                for (int i = count; i < count + 4; i++)
                {
                    Transform stale = rootObject.transform.Find(ChoiceIconNamePrefix + i);
                    if (stale != null)
                    {
                        UnityEngine.Object.Destroy(stale.gameObject);
                    }
                }

                rootObject.SetActive(count > 0);
                RefreshApproachIconVisuals(button, choiceIndex);
            }

            // ── Unchanged helpers below ──────────────────────────────────

            private static bool IsIncidentIntroEvent(GeoscapeEvent geoEvent)
            {
                if (geoEvent == null || string.IsNullOrEmpty(geoEvent.EventID))
                {
                    return false;
                }

                return GeoscapeEvents.IncidentDefinitions != null
                    && GeoscapeEvents.IncidentDefinitions.Any(i =>
                        i != null
                        && i.IntroEvent != null
                        && string.Equals(i.IntroEvent.EventID, geoEvent.EventID, StringComparison.OrdinalIgnoreCase));
            }

            private static CrewRowHighlightState EnsureHighlightState(SoldierSlotController row)
            {
                if (row == null)
                {
                    return null;
                }

                CrewRowHighlightState state = row.GetComponent<CrewRowHighlightState>();
                if (state == null)
                {
                    state = row.gameObject.AddComponent<CrewRowHighlightState>();
                }

                if (!state.Initialized)
                {
                    Transform existing = row.transform.Find(CrewRowHighlightName);
                    GameObject highlightObject;
                    if (existing == null)
                    {
                        highlightObject = new GameObject(CrewRowHighlightName, typeof(RectTransform), typeof(Image), typeof(Outline));
                        highlightObject.transform.SetParent(row.transform, false);
                        highlightObject.transform.SetAsFirstSibling();
                    }
                    else
                    {
                        highlightObject = existing.gameObject;
                    }

                    RectTransform highlightRect = highlightObject.GetComponent<RectTransform>();
                    highlightRect.anchorMin = Vector2.zero;
                    highlightRect.anchorMax = Vector2.one;
                    highlightRect.offsetMin = Vector2.zero;
                    highlightRect.offsetMax = Vector2.zero;
                    highlightRect.localScale = Vector3.one;

                    state.Background = highlightObject.GetComponent<Image>();
                    if (state.Background != null)
                    {
                        state.Background.raycastTarget = false;
                        state.Background.color = Color.clear;
                    }

                    state.Border = highlightObject.GetComponent<Outline>();
                    if (state.Border != null)
                    {
                        state.Border.effectDistance = new Vector2(2f, 2f);
                        state.Border.useGraphicAlpha = false;
                        state.Border.effectColor = Color.clear;
                    }

                    state.BackgroundNormal = state.Background != null ? state.Background.color : Color.clear;
                    state.BorderNormal = state.Border != null ? state.Border.effectColor : Color.clear;
                    state.Initialized = true;
                }

                return state;
            }

            private static Color EnsureVisibleAlpha(Color color, float minAlpha)
            {
                if (color.a < minAlpha)
                {
                    color.a = minAlpha;
                }
                return color;
            }

            private static void SetRowHighlight(SoldierSlotController row, bool selected)
            {
                if (row == null || row.GetComponent<Button>() == null)
                {
                    return;
                }

                Button button = row.GetComponent<Button>();
                CrewRowHighlightState state = EnsureHighlightState(row);
                if (state == null)
                {
                    return;
                }

                ColorBlock colors = button.colors;
                Color selectedFill = EnsureVisibleAlpha(colors.selectedColor, 0.35f);
                Color selectedBorder = EnsureVisibleAlpha(colors.highlightedColor, 0.35f);

                if (state.Background != null)
                {
                    state.Background.color = selected ? selectedFill : state.BackgroundNormal;
                }

                if (state.Border != null)
                {
                    state.Border.effectColor = selected ? selectedBorder : state.BorderNormal;
                }
            }

            private static void SetSelectedRow(SoldierSlotController row)
            {
                if (_selectedRow != null && _selectedRow != row)
                {
                    SetRowHighlight(_selectedRow, false);
                }

                _selectedRow = row;
                if (_selectedRow != null)
                {
                    SetRowHighlight(_selectedRow, true);
                }
            }

            private static void ResetSelectedLeaderContext(GeoscapeEvent geoEvent, GeoVehicle vehicle)
            {
                _selectedEventId = geoEvent?.EventID ?? string.Empty;
                _selectedVehicleId = vehicle?.VehicleID ?? -1;
                _selectedLeaderId = -1;
                _currentSelectedCharacter = null;
                _lastInteractedChoiceIndex = 0;


                _choiceApproaches.Clear();
                _selectedApproach.Clear();
                _approachSelectionLocked.Clear();

                _headerGainLabel = null;
                _headerGainIcon = null;
                _headerGainDetail = null;
                _headerGainTooltip = null;
            }

            private static void SetSelectedLeader(GeoCharacter character, GeoscapeEvent geoEvent, GeoVehicle vehicle)
            {
                if (geoEvent == null || vehicle == null)
                {
                    ResetSelectedLeaderContext(null, null);
                    return;
                }

                _selectedEventId = geoEvent.EventID ?? string.Empty;
                _selectedVehicleId = vehicle.VehicleID;
                _selectedLeaderId = character?.Id ?? -1;
                _currentSelectedCharacter = character;
            }

            internal static bool TryGetSelectedLeader(GeoscapeEvent geoEvent, GeoVehicle vehicle, out GeoCharacter leader)
            {
                leader = null;
                if (geoEvent == null || vehicle == null)
                {
                    return false;
                }

                if (!string.Equals(_selectedEventId, geoEvent.EventID ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (_selectedVehicleId != vehicle.VehicleID || _selectedLeaderId <= 0)
                {
                    return false;
                }

                leader = vehicle.GetAllCharacters().FirstOrDefault(c => c != null && c.Id == _selectedLeaderId);
                return leader != null;
            }

            private static GeoCharacter ResolveInitialLeader(GeoVehicle vehicle, IList<GeoEventChoice> choices)
            {
                if (vehicle == null)
                {
                    return null;
                }

                if (choices == null || choices.Count == 0)
                {
                    return LeaderSelection.SelectFallbackLeader(vehicle);
                }

                LeaderSelection.LeaderSelectionResult best = null;
                int maxChoices = Math.Min(2, choices.Count);

                for (int i = 0; i < maxChoices; i++)
                {
                    GeoEventChoice choice = choices[i];
                    if (LeaderSelection.TrySelectLeader(vehicle, choice, i, out LeaderSelection.LeaderSelectionResult candidate)
                        && IsBetterCandidate(candidate, best))
                    {
                        best = candidate;
                    }
                }

                return best?.Character ?? LeaderSelection.SelectFallbackLeader(vehicle);
            }

            private static bool IsBetterCandidate(LeaderSelection.LeaderSelectionResult candidate, LeaderSelection.LeaderSelectionResult current)
            {
                if (candidate == null)
                {
                    return false;
                }

                if (current == null)
                {
                    return true;
                }

                if (candidate.Missions != current.Missions)
                {
                    return candidate.Missions > current.Missions;
                }

                return candidate.Rank > current.Rank;
            }

            private static GeoVehicle ResolveVehicle(GeoscapeEvent geoEvent)
            {
                if (geoEvent.Context.Vehicle != null)
                {
                    return geoEvent.Context.Vehicle;
                }

                return geoEvent.Context.Site?.Vehicles?.FirstOrDefault();
            }

            private static List<GeoCharacter> ResolveCrew(GeoVehicle vehicle)
            {
                IEnumerable<GeoCharacter> source = vehicle.GetAllCharacters().Where(c => c != null && c.TemplateDef != null && c.TemplateDef.IsHuman);
                if (CrewFilter != null)
                {
                    source = source.Where(CrewFilter);
                }

                return source.ToList();
            }

            private static SoldierSlotController ResolveSoldierSlotPrefab()
            {
                if (SoldierSlotPrefabProvider != null)
                {
                    SoldierSlotController provided = SoldierSlotPrefabProvider();
                    if (provided != null)
                    {
                        return provided;
                    }
                }

                SoldierSlotController[] candidates = Resources.FindObjectsOfTypeAll<SoldierSlotController>();
                return candidates
                    .Where(c => c != null)
                    .OrderBy(c => c.gameObject.scene.IsValid() ? 1 : 0)
                    .ThenBy(c => c.gameObject.activeInHierarchy ? 1 : 0)
                    .FirstOrDefault();
            }

            private static void ResetRowSelectionVisualState(SoldierSlotController row)
            {
                if (row == null)
                {
                    return;
                }

                Button button = row.GetComponent<Button>();
                if (button == null)
                {
                    return;
                }

                if (button.targetGraphic != null)
                {
                    Color baseColor = button.IsInteractable() ? button.colors.normalColor : button.colors.disabledColor;
                    button.targetGraphic.canvasRenderer.SetColor(baseColor);
                    button.targetGraphic.CrossFadeColor(baseColor, 0f, true, true);
                }

                if (button.animator != null)
                {
                    button.animator.Rebind();
                    button.animator.Update(0f);
                }

                SetRowHighlight(row, false);
            }

            private static Text CreateHeader(Transform parent, Text styleSource)
            {

                GameObject headerObject = new GameObject(HeaderName, typeof(RectTransform), typeof(Text));
                headerObject.transform.SetParent(parent, false);

                Text header = headerObject.GetComponent<Text>();

                header.alignment = TextAnchor.MiddleCenter;
                header.horizontalOverflow = HorizontalWrapMode.Wrap;
                header.verticalOverflow = VerticalWrapMode.Overflow;

                if (styleSource != null)
                {
                    header.font = styleSource.font;
                    header.fontSize = styleSource.fontSize;
                    header.color = styleSource.color;
                    header.fontStyle = FontStyle.Bold;
                }

                RectTransform headerRect = header.GetComponent<RectTransform>();
                if (headerRect != null)
                {
                    headerRect.anchorMin = new Vector2(0f, 1f);
                    headerRect.anchorMax = new Vector2(1f, 1f);
                    headerRect.pivot = new Vector2(0.5f, 1f);
                    headerRect.anchoredPosition = Vector2.zero;
                    headerRect.sizeDelta = new Vector2(0f, styleSource != null ? styleSource.fontSize * 2.25f : 52f);
                }

                return header;
            }

            private static void UpdateHeaderForSelectedOperative(Text header, GeoCharacter selectedCharacter, GeoscapeEvent geoEvent, GeoVehicle vehicle)
            {
                if (header == null || selectedCharacter == null || geoEvent == null || vehicle == null)
                {
                    return;
                }

                header.text = "Select Leading Operative for the Incident.";
                _currentSelectedCharacter = selectedCharacter;
            }

            private static void CreateGainRow(Transform parent, Text styleSource)
            {
                GameObject row = new GameObject(GainRowName, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                row.transform.SetParent(parent, false);

                HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;
                hlg.spacing = GainRowSpacing;

                LayoutElement rowLE = row.GetComponent<LayoutElement>();
                rowLE.minHeight = GainIconSize + 8f;
                rowLE.preferredHeight = GainIconSize + 8f;

                int fontSize = styleSource != null ? Mathf.RoundToInt(styleSource.fontSize * 0.85f) : 38;

                // Label: "Leading operative will gain:"
                GameObject labelGO = new GameObject("[Mod]GainLabel", typeof(RectTransform), typeof(Text));
                labelGO.transform.SetParent(row.transform, false);
                _headerGainLabel = labelGO.GetComponent<Text>();
                _headerGainLabel.text = "Leading operative will gain:";
                _headerGainLabel.alignment = TextAnchor.MiddleRight;
                _headerGainLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
                _headerGainLabel.verticalOverflow = VerticalWrapMode.Overflow;
                _headerGainLabel.raycastTarget = false;
                if (styleSource != null)
                {
                    _headerGainLabel.font = styleSource.font;
                    _headerGainLabel.fontSize = fontSize;
                    _headerGainLabel.color = styleSource.color;
                }

                // Icon
                GameObject iconGO = new GameObject("[Mod]GainIcon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                iconGO.transform.SetParent(row.transform, false);
                _headerGainIcon = iconGO.GetComponent<Image>();
                _headerGainIcon.raycastTarget = true;
                _headerGainIcon.enabled = false;

                LayoutElement iconLE = iconGO.GetComponent<LayoutElement>();
                iconLE.preferredWidth = GainIconSize;
                iconLE.preferredHeight = GainIconSize;
                iconLE.minWidth = GainIconSize;
                iconLE.minHeight = GainIconSize;

                _headerGainTooltip = iconGO.AddComponent<ApproachIconTooltipTrigger>();

                // Detail: "Exploration  Rank: 2"
                GameObject detailGO = new GameObject("[Mod]GainDetail", typeof(RectTransform), typeof(Text));
                detailGO.transform.SetParent(row.transform, false);
                _headerGainDetail = detailGO.GetComponent<Text>();
                _headerGainDetail.text = string.Empty;
                _headerGainDetail.alignment = TextAnchor.MiddleLeft;
                _headerGainDetail.horizontalOverflow = HorizontalWrapMode.Overflow;
                _headerGainDetail.verticalOverflow = VerticalWrapMode.Overflow;
                _headerGainDetail.raycastTarget = false;
                if (styleSource != null)
                {
                    _headerGainDetail.font = styleSource.font;
                    _headerGainDetail.fontSize = fontSize;
                    _headerGainDetail.color = styleSource.color;
                    _headerGainDetail.fontStyle = FontStyle.Bold;
                }

                row.SetActive(false);
            }

            private static void RefreshHeaderGainDisplay()
            {
                if (_headerGainIcon == null || _headerGainDetail == null || _currentSelectedCharacter == null)
                {
                    return;
                }

                bool hasAffinity = LeaderSelection.TryGetCurrentAffinity(
                    _currentSelectedCharacter,
                    out LeaderSelection.AffinityApproach charApproach,
                    out int charRank);

                LeaderSelection.AffinityApproach? gainApproach = null;
                int gainRank = 0;
                bool alreadyMax = false;

                if (hasAffinity)
                {
                    // Check if any choice has the operative's affinity selected.
                    for (int i = 0; i < 2; i++)
                    {
                        if (_selectedApproach.TryGetValue(i, out LeaderSelection.AffinityApproach? sel)
                            && sel.HasValue && sel.Value == charApproach)
                        {
                            gainApproach = charApproach;
                            if (charRank >= 3)
                            {
                                gainRank = 3;
                                alreadyMax = true;
                            }
                            else
                            {
                                gainRank = charRank + 1;
                            }
                            break;
                        }
                    }
                }
                else
                {
                    // No affinity — prefer the last-interacted choice so the header
                    // reacts to whichever approach icon the player just clicked.
                    int first = _lastInteractedChoiceIndex;
                    int second = first == 0 ? 1 : 0;

                    if (_selectedApproach.TryGetValue(first, out LeaderSelection.AffinityApproach? selFirst) && selFirst.HasValue)
                    {
                        gainApproach = selFirst.Value;
                        gainRank = 1;
                    }
                    else if (_selectedApproach.TryGetValue(second, out LeaderSelection.AffinityApproach? selSecond) && selSecond.HasValue)
                    {
                        gainApproach = selSecond.Value;
                        gainRank = 1;
                    }
                }

                Transform gainRow = _headerGainIcon.transform.parent;

                if (!gainApproach.HasValue || gainRank <= 0)
                {
                    if (gainRow != null)
                    {
                        gainRow.gameObject.SetActive(false);
                    }
                    return;
                }

                PassiveModifierAbilityDef ability = LeaderSelection.GetAffinityAbility(gainApproach.Value, gainRank);
                Sprite icon = ability?.ViewElementDef?.SmallIcon;

                _headerGainIcon.sprite = icon;
                _headerGainIcon.enabled = icon != null;

                if (_headerGainTooltip != null)
                {
                    _headerGainTooltip.Approach = gainApproach.Value;
                }

                string name = GetApproachDisplayName(gainApproach.Value);
                if (alreadyMax)
                {
                    _headerGainDetail.text = $"{name}  Rank: 3 (max)";
                }
                else
                {
                    _headerGainDetail.text = $"{name}  Rank: {gainRank}";
                }

                if (gainRow != null)
                {
                    gainRow.gameObject.SetActive(true);
                }
            }

            private static string GetApproachDisplayName(LeaderSelection.AffinityApproach approach)
            {
                switch (approach)
                {
                    case LeaderSelection.AffinityApproach.PsychoSociology: return "Psycho-Sociology";
                    case LeaderSelection.AffinityApproach.Exploration: return "Exploration";
                    case LeaderSelection.AffinityApproach.Occult: return "Occult";
                    case LeaderSelection.AffinityApproach.Biotech: return "Biotech";
                    case LeaderSelection.AffinityApproach.Machinery: return "Machinery";
                    case LeaderSelection.AffinityApproach.Compute: return "Compute";
                    default: return approach.ToString();
                }
            }

            private static string ResolveApproachHoursText(GeoscapeEvent geoEvent, GeoVehicle vehicle, int choiceIndex, GeoCharacter leader)
            {
                if (Resolution.IncidentController.TryComputeIncidentHours(geoEvent, vehicle, choiceIndex, leader, out float hours))
                {
                    return FormatHours(hours);
                }

                return "N/A";
            }

            private static string FormatHours(float hours)
            {
                TFTVLogger.Always($"[FormatHours] Computed hours: {hours}");

                if (hours <= 0f)
                {
                    return "N/A";
                }

                return hours.ToString("0", CultureInfo.InvariantCulture) + "h";
            }

            private static Sprite ResolveAbilityIcon(GeoCharacter character)
            {
                if (ExtraIconResolver != null)
                {
                    Sprite external = ExtraIconResolver(character);
                    if (external != null)
                    {
                        return external;
                    }
                }

                TacticalAbilityDef affinityAbility = GetAffinityAbility(character);
                return affinityAbility?.ViewElementDef?.SmallIcon;
            }

            private static TacticalAbilityDef GetAffinityAbility(GeoCharacter character)
            {
                if (character == null)
                {
                    return null;
                }

                List<TacticalAbilityDef> abilities = character.GetTacticalAbilities();
                for (int i = 0; i < abilities.Count; i++)
                {
                    TacticalAbilityDef ability = abilities[i];
                    if (ability?.SkillTags == null)
                    {
                        continue;
                    }

                    for (int j = 0; j < ability.SkillTags.Length; j++)
                    {
                        if (ability.SkillTags[j] != null && ability.SkillTags[j].name == AffinityTagDefName)
                        {
                            return ability;
                        }
                    }
                }

                return null;
            }

            private static void SetAffinityIconAfterName(SoldierSlotController row, Sprite icon)
            {
                if (row == null || row.NameLabel == null)
                {
                    return;
                }

                Transform existing = row.NameLabel.transform.parent.Find(InlineAffinityIconName);
                Image iconImage;
                RectTransform iconRect;

                if (existing == null)
                {
                    GameObject iconObject = new GameObject(InlineAffinityIconName, typeof(RectTransform), typeof(Image));
                    iconObject.transform.SetParent(row.NameLabel.transform.parent, false);
                    iconRect = iconObject.GetComponent<RectTransform>();
                    iconImage = iconObject.GetComponent<Image>();
                }
                else
                {
                    iconRect = existing as RectTransform;
                    iconImage = existing.GetComponent<Image>();
                }

                if (iconRect == null || iconImage == null)
                {
                    return;
                }

                iconImage.sprite = icon;
                bool show = icon != null;
                iconImage.enabled = show;
                iconImage.gameObject.SetActive(show);

                if (!show)
                {
                    return;
                }

                float classIconSize = 18f;
                if (row.IconElement != null)
                {
                    RectTransform classIconRect = row.IconElement.GetComponent<RectTransform>();
                    if (classIconRect != null)
                    {
                        classIconSize = Mathf.Max(classIconRect.rect.width, classIconRect.rect.height);
                        if (classIconSize < 18f)
                        {
                            classIconSize = 18f;
                        }
                    }
                }

                float nameWidth = row.NameLabel.preferredWidth;
                iconRect.anchorMin = new Vector2(0f, 0.5f);
                iconRect.anchorMax = new Vector2(0f, 0.5f);
                iconRect.pivot = new Vector2(0f, 0.5f);
                iconRect.anchoredPosition = new Vector2(
                    row.NameLabel.rectTransform.anchoredPosition.x + nameWidth + 10f,
                    row.NameLabel.rectTransform.anchoredPosition.y);
                iconRect.sizeDelta = new Vector2(classIconSize, classIconSize);
            }

            private static void ConfigureRootRect(RectTransform rootRect, Text descriptionText)
            {
                if (rootRect == null)
                {
                    return;
                }

                rootRect.anchorMin = new Vector2(0.5f, 1f);
                rootRect.anchorMax = new Vector2(0.5f, 1f);
                rootRect.pivot = new Vector2(0.5f, 1f);
                rootRect.localScale = Vector3.one;

                float width = ResolvePanelWidth(descriptionText);
                rootRect.sizeDelta = new Vector2(width, 0f);

                if (descriptionText != null)
                {
                    RectTransform descRect = descriptionText.rectTransform;
                    rootRect.anchoredPosition = new Vector2(descRect.anchoredPosition.x, descRect.anchoredPosition.y - descRect.rect.height - CrewPanelTopPadding);
                }
                else
                {
                    rootRect.anchoredPosition = new Vector2(0f, -CrewPanelTopPadding);
                }
            }

            private static float ResolvePanelWidth(Text descriptionText)
            {
                if (descriptionText == null)
                {
                    return FallbackGridWidth;
                }

                float width = descriptionText.rectTransform.rect.width;
                if (width <= 1f)
                {
                    width = FallbackGridWidth;
                }

                return Mathf.Min(width, MaxGridWidth);
            }

            private static void ConfigureGrid(RectTransform gridRect, GridLayoutGroup grid, ContentSizeFitter gridFitter, float panelWidth, SoldierSlotController rowPrefab)
            {
                if (gridRect != null)
                {
                    gridRect.anchorMin = new Vector2(0.5f, 1f);
                    gridRect.anchorMax = new Vector2(0.5f, 1f);
                    gridRect.pivot = new Vector2(0.5f, 1f);
                    gridRect.anchoredPosition = Vector2.zero;
                    gridRect.sizeDelta = new Vector2(panelWidth, 0f);
                }

                float rowHeight = MinCellHeight;
                RectTransform prefabRect = rowPrefab.transform as RectTransform;
                if (prefabRect != null && prefabRect.rect.height > MinCellHeight)
                {
                    rowHeight = prefabRect.rect.height;
                }

                float cellWidth = (panelWidth - GridSpacingX) / GridColumns;

                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = GridColumns;
                grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
                grid.startAxis = GridLayoutGroup.Axis.Horizontal;
                grid.childAlignment = TextAnchor.UpperCenter;
                grid.cellSize = new Vector2(cellWidth, rowHeight);
                grid.spacing = new Vector2(GridSpacingX, GridSpacingY);

                gridFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                gridFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            private static void NormalizeRowRect(RectTransform rowRect)
            {
                if (rowRect == null)
                {
                    return;
                }

                rowRect.anchorMin = new Vector2(0.5f, 0.5f);
                rowRect.anchorMax = new Vector2(0.5f, 0.5f);
                rowRect.pivot = new Vector2(0.5f, 0.5f);
                rowRect.localScale = Vector3.one;
                rowRect.anchoredPosition = Vector2.zero;

                LayoutElement layoutElement = rowRect.GetComponent<LayoutElement>();
                if (layoutElement != null)
                {
                    UnityEngine.Object.Destroy(layoutElement);
                }
            }

            private static void RemovePreviousRows(Transform parent)
            {
                List<Transform> toRemove = new List<Transform>();
                foreach (Transform child in parent)
                {
                    if (child != null && child.name == CrewRootName)
                    {
                        toRemove.Add(child);
                    }
                }

                for (int i = 0; i < toRemove.Count; i++)
                {
                    UnityEngine.Object.Destroy(toRemove[i].gameObject);
                }
            }
        }

        /// <summary>
        /// Injects a one-time explanatory text panel into the UIModuleSiteEncounters UI
        /// the first time an incident intro event opens in a playthrough.
        /// Uses the same panel construction pattern as IncidentOutcomeSummaryUI.
        /// </summary>
        internal static class IncidentIntroTutorialPanel
        {
            private const string PanelRootName = "[Mod]IncidentTutorialPanelRoot";
            private const string PanelTextName = "[Mod]IncidentTutorialPanelText";
            private const string VarShown = "TFTV_INCIDENT_INTRO_TUTORIAL_SHOWN";

            private const float PanelWidth = 800f;
            private const float GapFromEventText = 1000f;

            // Localization keys — add matching entries to your localization table.
            private const string TitleKey = "TUTORIAL_INCIDENTS_TITLE0";
            private const string TextKey = "TUTORIAL_INCIDENTS_TEXT1";

            internal static void TryShowPanel(UIModuleSiteEncounters module, GeoscapeEvent geoEvent)
            {
                try
                {
                    GeoLevelController controller = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
                    if (controller == null)
                    {
                        return;
                    }

                    if (controller.EventSystem.GetVariable(VarShown) != 0)
                    {
                        return;
                    }

                    controller.EventSystem.SetVariable(VarShown, 1);

                    Transform parent = module?.SiteEncounterTextContainer != null
                        ? module.SiteEncounterTextContainer.transform
                        : null;
                    if (parent == null)
                    {
                        return;
                    }

                    string title = TFTVCommonMethods.ConvertKeyToString(TitleKey);
                    string body = TFTVCommonMethods.ConvertKeyToString(TextKey);

                    string panelText = string.Empty;
                    if (!string.IsNullOrEmpty(title))
                    {
                        panelText = $"<b>{title}</b>\n";
                    }
                    if (!string.IsNullOrEmpty(body))
                    {
                        panelText += body;
                    }

                    if (string.IsNullOrEmpty(panelText))
                    {
                        return;
                    }

                    CreateOrUpdatePanel(parent, module, panelText);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            internal static void ClearPanel(Transform parent)
            {
                if (parent == null)
                {
                    return;
                }

                List<Transform> toRemove = new List<Transform>();
                foreach (Transform child in parent)
                {
                    if (child != null && child.name == PanelRootName)
                    {
                        toRemove.Add(child);
                    }
                }

                for (int i = 0; i < toRemove.Count; i++)
                {
                    UnityEngine.Object.Destroy(toRemove[i].gameObject);
                }
            }

            private static void CreateOrUpdatePanel(Transform parent, UIModuleSiteEncounters module, string panelText)
            {
                Transform existingRoot = parent.Find(PanelRootName);
                GameObject root;
                if (existingRoot == null)
                {
                    root = new GameObject(
                        PanelRootName,
                        typeof(RectTransform),
                        typeof(Image),
                        typeof(Outline),
                        typeof(VerticalLayoutGroup),
                        typeof(ContentSizeFitter),
                        typeof(LayoutElement));
                    root.transform.SetParent(parent, false);
                }
                else
                {
                    root = existingRoot.gameObject;
                }

                LayoutElement layoutElement = root.GetComponent<LayoutElement>();
                if (layoutElement != null)
                {
                    layoutElement.ignoreLayout = true;
                }

                RectTransform rootRect = root.GetComponent<RectTransform>();
                ConfigurePanelRect(rootRect, module);

                Image background = root.GetComponent<Image>();
                background.raycastTarget = false;
                background.color = new Color(0f, 0f, 0f, 0.55f);

                Outline border = root.GetComponent<Outline>();
                border.effectColor = new Color(1f, 1f, 1f, 0.25f);
                border.effectDistance = new Vector2(1f, 1f);

                VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
                layout.childAlignment = TextAnchor.UpperLeft;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
                layout.padding = new RectOffset(12, 12, 10, 10);
                layout.spacing = 4f;

                ContentSizeFitter fitter = root.GetComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                Text textComponent;
                Transform existingText = root.transform.Find(PanelTextName);
                if (existingText == null)
                {
                    GameObject textObj = new GameObject(PanelTextName, typeof(RectTransform), typeof(Text));
                    textObj.transform.SetParent(root.transform, false);
                    textComponent = textObj.GetComponent<Text>();
                }
                else
                {
                    textComponent = existingText.GetComponent<Text>();
                }

                Text style = module.EncounterDescriptionText;
                if (style != null)
                {
                    textComponent.font = style.font;
                    textComponent.fontSize = style.fontSize > 2 ? style.fontSize - 2 : style.fontSize;
                    textComponent.color = style.color;
                }

                textComponent.alignment = TextAnchor.UpperLeft;
                textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
                textComponent.verticalOverflow = VerticalWrapMode.Overflow;
                textComponent.supportRichText = true;
                textComponent.text = panelText;
            }

            private static void ConfigurePanelRect(RectTransform rootRect, UIModuleSiteEncounters module)
            {
                if (rootRect == null)
                {
                    return;
                }

                rootRect.anchorMin = new Vector2(0.5f, 1f);
                rootRect.anchorMax = new Vector2(0.5f, 1f);
                rootRect.pivot = new Vector2(1f, 1f);
                rootRect.localScale = Vector3.one;
                rootRect.sizeDelta = new Vector2(PanelWidth, 0f);

                Text desc = module?.EncounterDescriptionText;
                if (desc != null)
                {
                    RectTransform descRect = desc.rectTransform;
                    float leftEdgeX = descRect.anchoredPosition.x - (descRect.rect.width * descRect.pivot.x);
                    rootRect.anchoredPosition = new Vector2(leftEdgeX - GapFromEventText, descRect.anchoredPosition.y + 600f);
                }
            }
        }
    }
}