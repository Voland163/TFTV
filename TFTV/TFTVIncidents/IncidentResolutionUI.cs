using Base.Core;
using Base.Levels;
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
using TFTV.TFTVBaseRework;
using TFTV.TFTVUI.Common;
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
            static bool Prepare() => TFTVAircraftReworkMain.AircraftReworkOn;
            private const string CrewRootName = "[Mod]EventCrewListRoot";
            private const string HeaderName = "[Mod]EventCrewListHeader";

            private const string ChoiceIconsRootName = "[Mod]ChoiceApproachIcons";
            private const string ChoiceIconNamePrefix = "[Mod]ChoiceApproachIcon_";
            private const string ChoicePayoffRootName = "[Mod]ChoicePayoffRow";
            private const string ApproachArrowName = "[Mod]ApproachLevelUpArrow";
            private const string ApproachGlyphName = "[Mod]ApproachGlyph";

            /// <summary>
            /// How much of an approach icon's box the affinity glyph fills.
            ///
            /// The box keeps the button's full height - it is the click target, and the space the
            /// icon occupies in the layout - while the glyph is drawn smaller inside it, which is how
            /// the mockup has them. This is a fraction of the box, and the glyph also keeps its
            /// aspect ratio, so a mark that is wider than it is tall ends up shorter again than this
            /// number alone suggests: at an even half the glyphs read as though the whole icon had
            /// been shrunk rather than the picture in it.
            /// </summary>
            private const float ApproachGlyphFraction = 0.55f;

            private const string NoEligibleOperativeKey = "KEY_TFTV_INCIDENT_NO_ELIGIBLE_OPERATIVE";
            private const string SelectLeaderKey = "KEY_TFTV_INCIDENT_SELECT_LEADER";
            private const string HoursKey = "KEY_TFTV_INCIDENT_HOURS";

            private const float CrewPanelTopPadding = 8f;
            private const float HeaderToGridSpacing = 50f;

            /// <summary>
            /// Ceiling on the crew panel's width. The encounter's description column is wider than
            /// this on a wide screen, and a row of cards stretched the whole way across stops reading
            /// as a group.
            /// </summary>
            private const float MaxGridWidth = 1700f;

            private const float FallbackGridWidth = 900f;

            /// <summary>
            /// How large an approach icon is drawn when the response button has room for it. The
            /// buttons are the game's own and their height is not ours to assume, so this is a
            /// ceiling rather than a size - see <see cref="ResolveApproachIconSize"/>.
            /// </summary>
            private const float ApproachIconSize = 80f;

            /// <summary>Floor for that, below which an affinity glyph is no longer recognisable.</summary>
            private const float MinApproachIconSize = 40f;

            /// <summary>
            /// Gap between two approach icons on the same response. None: their plates are the same
            /// black, so a gap between them only breaks up what reads better as one strip.
            /// </summary>
            private const float ApproachIconSpacing = 0f;

            private const float ApproachIconOutlineWidth = 3f;

            /// <summary>Gap between the button's left edge and its approach icons.</summary>
            private const float ApproachIconInset = 8f;

            /// <summary>Gap between the approach icons and the response text beside them.</summary>
            private const float ApproachIconTextGap = 44f;

            /// <summary>Clear space kept at the response text's right edge.</summary>
            private const float ChoiceTextRightPadding = 20f;

            /// <summary>
            /// Height of the hours-and-payoff strip along the bottom of a response. Tall enough for a
            /// line of the response's own text, which is the size the strip is set in.
            /// </summary>
            private const float PayoffRowHeight = 56f;

            /// <summary>The mark repeated once per step of a payoff's one-to-three scale.</summary>
            private const char TierMark = '+';

            private const float PayoffIconSize = 42f;
            private const float PayoffEntrySpacing = 18f;
            private const float PayoffIconLabelSpacing = 3f;

            /// <summary>
            /// The level-up arrow badge on an approach icon: big enough to read the rank off at the
            /// size the icons are drawn.
            /// </summary>
            private const float ApproachArrowSize = 38f;

            /// <summary>
            /// Responses per row. The mockup puts the two side by side, which is what makes them a
            /// pair to choose between rather than a list to read down - and with the walk-away choice
            /// off the grid there are exactly two left to place.
            /// </summary>
            private const int ChoiceColumns = 2;

            /// <summary>Gap between the two responses.</summary>
            private const float ChoiceColumnSpacing = 76f;

            /// <summary>
            /// Clear space left at each side of the responses, as a fraction of the container - which
            /// is the whole screen width. Matches where the mockup puts the outer edges of the pair.
            /// </summary>
            private const float ChoiceRowSideMargin = 0.12f;

            /// <summary>
            /// Clear space above and below an approach icon inside its response. The plate runs
            /// nearly the response's full height, but stops short of its border rather than sitting
            /// on it - at zero the black ran over the button's own edge.
            /// </summary>
            private const float ApproachIconVerticalInset = 8f;

            /// <summary>Clear space above the response text, so it does not sit on the button's edge.</summary>
            private const float TextTopPadding = 20f;

            /// <summary>
            /// Clear space below the payoff strip. With the response text against the top and the
            /// strip against the bottom, the two lines were each pressed into a border; pulling both
            /// in closes the gap between them and opens one against the edges instead.
            /// </summary>
            private const float PayoffBottomPadding = 16f;

            public static Func<GeoCharacter, bool> CrewFilter;

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

            // Whether the selected operative's existing affinity is one this choice's approaches
            // call for - what makes a response worth giving to this operative rather than another,
            // and the only thing the green response text claims.
            private static readonly Dictionary<int, bool> _choiceAffinityMatch =
                new Dictionary<int, bool>();

            // The rank the selected operative would come back from this choice at, or 0 when it
            // would not raise them - what the level-up arrow on the approach icon carries.
            private static readonly Dictionary<int, int> _approachArrowRank =
                new Dictionary<int, int>();

            /// <summary>Top affinity rank, past which a matching response grants no further rank.</summary>
            private const int MaxAffinityRank = 3;

            /// <summary>The shipped arrow image the level-up badge is drawn with, tinted at runtime.</summary>
            private const string LevelUpArrowImageName = "incident_affinity_up.png";

            private static Sprite _levelUpArrowSprite;
            private static bool _levelUpArrowResolved;

            private static GeoCharacter _currentSelectedCharacter;

            /// <summary>
            /// A response button's own text, and the layout it was found in.
            ///
            /// The label is restyled and repositioned to make room for the approach icons beside it
            /// and the payoff strip beneath it, and these buttons outlive the incident that borrowed
            /// them - so what they looked like before has to be recorded somewhere to put back.
            /// </summary>
            private sealed class ChoiceButtonVisualState : MonoBehaviour
            {
                public Text Label;
                public string BaseText;

                public bool LayoutCaptured;
                public Vector2 LabelAnchorMin;
                public Vector2 LabelAnchorMax;
                public Vector2 LabelOffsetMin;
                public Vector2 LabelOffsetMax;
                public TextAnchor LabelAlignment;
                public Color LabelColor;
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

                private const string TooltipObjectName = "[Mod]ApproachIconTooltip";
                private const float TooltipWidth = 700f;
                private const float TooltipOffsetY = 16f;

                /// <summary>Clear space kept between the tooltip and the edge of the screen.</summary>
                private const float TooltipEdgeMargin = 24f;
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

                    PositionAbove(tooltip, trigger.transform as RectTransform);

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

                private static void PositionAbove(RectTransform tooltip, RectTransform iconRect)
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
                        // Centred over the icon and then pulled back inside the screen. The icons used
                        // to hang off the outer edges of the two responses, where a fixed nudge was
                        // enough to keep the left one on screen; inside the buttons they can be
                        // anywhere along the row, so the tooltip is clamped rather than nudged.
                        float halfCanvas = canvasRect.rect.width * 0.5f;
                        float halfTooltip = tooltip.rect.width * 0.5f;
                        float limit = Mathf.Max(0f, halfCanvas - halfTooltip - TooltipEdgeMargin);

                        tooltip.anchoredPosition = new Vector2(
                            Mathf.Clamp(localPoint.x, -limit, limit),
                            localPoint.y + TooltipOffsetY);
                    }
                }

                private static string BuildContent(LeaderSelection.AffinityApproach approach)
                {
                    string key = LeaderSelection.GetAllBenefitsLocalizationKey(approach);
                    if (string.IsNullOrEmpty(key))
                    {
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

                    Transform payoffRoot = button.transform.Find(ChoicePayoffRootName);
                    if (payoffRoot != null)
                    {
                        UnityEngine.Object.DestroyImmediate(payoffRoot.gameObject);
                    }

                    ChoiceButtonVisualState state = button.GetComponent<ChoiceButtonVisualState>();
                    if (state != null)
                    {
                        // The label's own rect and colour were changed to make room for the icons and
                        // the payoff strip, and this button outlives the incident that borrowed it.
                        RestoreChoiceLayout(state);
                        UnityEngine.Object.DestroyImmediate(state); // was Destroy — deferred destroy caused stale BaseText to persist
                    }
                }
            }

            /// <summary>
            /// Hands the choices row back to the game: the grid at the size it ships at, the response
            /// buttons without the icons and payoff strips put inside them, and the walk-away choice
            /// visible again in place of the cancel button.
            ///
            /// Called both before an encounter is shown and as one is closed. The closing screen is
            /// the one that needs it: an incident that is cancelled answers with an outcome screen
            /// drawn on these same buttons, and without this it inherits a row grown to fit decoration
            /// that is no longer there.
            /// </summary>
            internal static void RestoreVanillaChoiceLayout(UIModuleSiteEncounters module)
            {
                RestoreChoiceButtons(module);
                CleanupChoiceButtonDecorations(module);
                IncidentCancelButton.Remove(module);
            }

            public static void Postfix(UIModuleSiteEncounters __instance, GeoscapeEvent geoEvent, bool pagingEvent)
            {
                if (!TFTVBaseRework.BaseReworkCheck.BaseReworkEnabled)
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
                RestoreVanillaChoiceLayout(__instance);
                OperativeCards.Clear();
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
                PortraitGenerator.ClearCache();
                PortraitGenerator.CancelPendingCardPortraits();
                ChoicePayoff.ClearCaches();
                LeaderAbilityIcons.Clear();

                List<GeoCharacter> crew = ResolveCrew(vehicle);
                if (crew.Count == 0)
                {
                    DisableChoiceButtons(__instance, TFTVCommonMethods.ConvertKeyToString(NoEligibleOperativeKey));
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
                GameObject headerSpacer = new GameObject("[Mod]CrewHeaderSpacer", typeof(RectTransform), typeof(LayoutElement));
                headerSpacer.transform.SetParent(root.transform, false);
                LayoutElement spacerLayout = headerSpacer.GetComponent<LayoutElement>();
                spacerLayout.minHeight = HeaderToGridSpacing;

                float panelWidth = ResolvePanelWidth(__instance.EncounterDescriptionText);

                // The cancel choice becomes a cancel button before the responses are decorated, so
                // the two that remain are the only ones the decoration has to lay out - and the grid
                // is resized before that, because how much room the icons and the payoff strip get
                // is decided by the size of the button they are going into.
                IncidentCancelButton.Install(__instance);
                AdjustChoiceButtons(__instance, geoEvent);

                // The large leader picture waits for the crew row: the row is what the player looks
                // at first, and rendered after it the leader picture finds the selected operative's
                // armour already loaded. Until then the latest selection is held, not rendered.
                bool facesShown = false;
                GeoCharacter leaderAwaitingFaces = null;

                Action<GeoCharacter> selectOperative = character =>
                {
                    OperativeCards.SetSelected(character);
                    SetSelectedLeader(character, geoEvent, vehicle);

                    if (facesShown)
                    {
                        PortraitGenerator.RequestLeaderPortrait(__instance, character);
                    }
                    else
                    {
                        leaderAwaitingFaces = character;
                    }

                    LeaderAbilityIcons.Show(__instance, character);
                    UpdateHeaderForSelectedOperative(header, character, geoEvent, vehicle);
                    UpdateChoiceButtonsForSelectedOperative(__instance, geoEvent, vehicle, character);
                };

                List<Selectable> cards = OperativeCards.Build(
                    root.transform,
                    crew,
                    panelWidth,
                    _cachedFont,
                    __instance.EncounterDescriptionText != null ? __instance.EncounterDescriptionText.fontSize : 40,
                    selectOperative,
                    () =>
                    {
                        facesShown = true;
                        if (leaderAwaitingFaces != null)
                        {
                            PortraitGenerator.RequestLeaderPortrait(__instance, leaderAwaitingFaces);
                            leaderAwaitingFaces = null;
                        }
                    });

                if (cards.Count > 0)
                {
                    GeoCharacter selectedCharacter = initialLeader;
                    if (selectedCharacter == null || !crew.Take(OperativeCards.MaxCards).Any(c => c != null && c.Id == selectedCharacter.Id))
                    {
                        // The suggested leader is not one of the crew the row has room for, so the
                        // selection has to fall to somebody the player can actually see selected.
                        selectedCharacter = crew[0];
                    }

                    selectOperative(selectedCharacter);
                    SetupApproachIconNavigation(__instance);

                    // Last, because it measures the response buttons: their size is settled by the
                    // grid above and their contents by the selection just made.
                    IncidentCancelButton.PositionUnderChoices(__instance);
                }

                IncidentIntroTutorialPanel.TryShowPanel(__instance, geoEvent);   // ADD THIS LINE after crew list is built
            }

            /// <summary>
            /// Makes the approach icons reachable with a gamepad by laying the encounter screen's own
            /// navigation holder out as rows: each row is a choice button followed by its approach icons,
            /// so up and down move between choices and left and right run along a choice's approaches.
            ///
            /// This retargets the vanilla holder rather than registering a competing one, so it keeps its
            /// place in the navigation order and the module's own ForceJoystickCursorToPosition still
            /// works. It only takes over when there are icons to reach - without them the vanilla grid
            /// layout is left completely alone.
            /// </summary>
            private static void SetupApproachIconNavigation(UIModuleSiteEncounters module)
            {
                try
                {
                    UINavigationalElementsHolder holder = module?.InteractableElementsHolder;
                    if (holder == null || module.ChoiceButtonsContainer == null)
                    {
                        return;
                    }

                    List<IList<Selectable>> rows = new List<IList<Selectable>>();
                    List<int> heads = new List<int>();

                    // Crew cards first - they sit above the choices, and picking the operative is the
                    // first decision. Without them the leader can only be chosen with a mouse.
                    AddCrewRows(module, rows, heads);

                    bool anyIcons = AddChoiceRows(module, rows, heads);

                    // Cancel last, below the choices, which is where it is on screen.
                    AddCancelRow(rows, heads);

                    // Nothing mod-specific to reach: leave vanilla's own grid layout completely alone.
                    if (!anyIcons && rows.Count == 0)
                    {
                        return;
                    }

                    ControllerNav.ApplyRowsToExistingHolder(holder, rows, heads);
                }
                catch (Exception ex) { TFTVLogger.Error(ex); }
            }

            /// <summary>
            /// The crew cards as one navigation row, which is what they are on screen: left and right
            /// walk the crew, down leaves for the responses.
            ///
            /// Read back out of the hierarchy rather than from the list the cards were built into,
            /// because this runs again on every leader change and has to survive the row being rebuilt
            /// underneath it.
            ///
            /// Deliberately not gated on activeInHierarchy: the row is built here but its container is
            /// not switched on until the encounter is actually presented, so requiring it to be live at
            /// this point drops every card. Listing not-yet-active elements is what the game's own
            /// holders do - the navigation code skips inactive ones at selection time and picks them up
            /// once they appear.
            /// </summary>
            private static void AddCrewRows(
                UIModuleSiteEncounters module,
                List<IList<Selectable>> rows,
                List<int> heads)
            {
                Transform cardRow = FindDeepChild(module.transform, OperativeCards.RowName);
                if (cardRow == null)
                {
                    return;
                }

                List<Selectable> cards = new List<Selectable>();

                for (int i = 0; i < cardRow.childCount; i++)
                {
                    Transform child = cardRow.GetChild(i);
                    if (child == null || !child.gameObject.activeSelf)
                    {
                        continue;
                    }

                    Selectable selectable = child.GetComponent<Selectable>();
                    if (selectable != null)
                    {
                        cards.Add(selectable);
                    }
                }

                if (cards.Count > 0)
                {
                    rows.Add(cards);
                    heads.Add(0);
                }
            }

            /// <summary>
            /// Cancel, on its own row under the responses. Nothing is added when the encounter kept its
            /// cancel choice, since that choice is then already a button the navigation picks up.
            /// </summary>
            private static void AddCancelRow(List<IList<Selectable>> rows, List<int> heads)
            {
                Selectable cancel = IncidentCancelButton.CancelSelectable;
                if (cancel == null)
                {
                    return;
                }

                rows.Add(new List<Selectable> { cancel });
                heads.Add(0);
            }

            /// <summary>
            /// One row per choice: the choice button plus its approach icons, ordered by where they
            /// actually sit on screen rather than by which button owns what - the icons now sit inside
            /// their response's left edge, so a response and the icons of the response beside it can
            /// share a line. The choice button stays the row's head, so vertical movement always lands
            /// back on a response rather than on one of the icons inside it.
            /// </summary>
            /// <returns>True if any approach icon was found.</returns>
            private static bool AddChoiceRows(
                UIModuleSiteEncounters module,
                List<IList<Selectable>> rows,
                List<int> heads)
            {
                SiteBaseChoiceButton[] buttons =
                    module.ChoiceButtonsContainer.GetComponentsInChildren<SiteBaseChoiceButton>(includeInactive: false);

                List<Selectable> all = new List<Selectable>();
                HashSet<Selectable> choiceSelectables = new HashSet<Selectable>();
                bool anyIcons = false;

                foreach (SiteBaseChoiceButton button in buttons)
                {
                    if (button == null)
                    {
                        continue;
                    }

                    Selectable choiceSelectable = button.Button != null
                        ? (button.Button.BaseButton ?? button.Button.GetComponent<Selectable>())
                        : button.GetComponent<Selectable>();

                    if (choiceSelectable != null)
                    {
                        all.Add(choiceSelectable);
                        choiceSelectables.Add(choiceSelectable);
                    }

                    Transform iconsRoot = button.transform.Find(ChoiceIconsRootName);
                    if (iconsRoot == null || !iconsRoot.gameObject.activeSelf)
                    {
                        continue;
                    }

                    for (int i = 0; i < iconsRoot.childCount; i++)
                    {
                        Transform icon = iconsRoot.GetChild(i);
                        if (icon == null || !icon.gameObject.activeSelf)
                        {
                            continue;
                        }

                        Button iconButton = icon.GetComponent<Button>();
                        if (iconButton != null && iconButton.interactable)
                        {
                            all.Add(iconButton);
                            anyIcons = true;
                        }
                    }
                }

                // Grouped by screen position, not by which button owns what: two choices sit side by side
                // on one line, each wearing its approach icons on its outer edge, so the whole line is one
                // navigation row - icons, choice, choice, icons - and left/right walks it in visual order.
                foreach (IList<Selectable> row in ControllerNav.GroupIntoRowsByPosition(all))
                {
                    // The row's head is its first actual choice, so vertical movement always lands on a
                    // choice rather than on one of the approach icons flanking it.
                    int head = 0;
                    for (int i = 0; i < row.Count; i++)
                    {
                        if (choiceSelectables.Contains(row[i]))
                        {
                            head = i;
                            break;
                        }
                    }

                    rows.Add(row);
                    heads.Add(head);
                }

                return anyIcons;
            }

            private static Transform FindDeepChild(Transform root, string name)
            {
                foreach (Transform child in root.GetComponentsInChildren<Transform>(includeInactive: true))
                {
                    if (child != null && child.name == name)
                    {
                        return child;
                    }
                }

                return null;
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

                // Measured once per pass from the buttons the game actually gave us, so everything
                // laid out inside them agrees about how much room the icons take.
                float iconSize = CurrentApproachIconSize(module);

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

                    // Green is the whole point of picking this operative for this response: their
                    // affinity applies here and not to the other one. The hours underneath drop with
                    // it, but a shorter number is only a signal next to the number it is shorter
                    // than, and the two responses are read one at a time - the colour is not.
                    //
                    // Written as markup rather than set on the Text, because the button recolours its
                    // own label as the pointer moves over it; markup is applied under that tint, so
                    // the response still lights up on hover and still reads as the matching one.
                    _choiceAffinityMatch.TryGetValue(i, out bool matches);
                    state.Label.supportRichText = true;
                    state.Label.text = matches
                        ? $"<color=#{ColorUtility.ToHtmlStringRGB(IncidentUIStyle.AffinityMatch)}>{baseText}</color>"
                        : baseText;

                    ApplyChoiceIcons(button, i, iconSize);
                    ApplyChoiceLayout(state, i, iconSize);
                    ApplyChoicePayoff(button, geoEvent, vehicle, i, selectedCharacter, iconSize, state.Label.fontSize);
                }

                // ApplyChoiceIcons resets every icon's navigation to Mode.None, which strips the row links
                // and leaves the cursor with nowhere to go once it lands on an icon. This runs on every
                // leader change, so the layout has to be rewritten each time.
                SetupApproachIconNavigation(module);
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

                if (state.Label != null && !state.LayoutCaptured)
                {
                    RectTransform labelRect = state.Label.rectTransform;
                    state.LabelAnchorMin = labelRect.anchorMin;
                    state.LabelAnchorMax = labelRect.anchorMax;
                    state.LabelOffsetMin = labelRect.offsetMin;
                    state.LabelOffsetMax = labelRect.offsetMax;
                    state.LabelAlignment = state.Label.alignment;
                    state.LabelColor = state.Label.color;
                    state.LayoutCaptured = true;
                }

                return state;
            }

            // ── Choice button layout ─────────────────────────────────────

            /// <summary>
            /// Width of the approach icon strip that sits inside a response's left edge, spacing
            /// included. Zero for a response with no approaches, which then keeps the full button.
            /// </summary>
            private static float ComputeApproachStripWidth(int choiceIndex, float iconSize)
            {
                if (!_choiceApproaches.TryGetValue(choiceIndex, out List<LeaderSelection.AffinityApproach> approaches)
                    || approaches == null || approaches.Count == 0)
                {
                    return 0f;
                }

                int count = approaches.Count;
                return (count * iconSize) + (Mathf.Max(0, count - 1) * ApproachIconSpacing);
            }

            /// <summary>
            /// Where a response's text and its payoff strip start: clear of the approach icons when
            /// there are any, at the normal margin when there are not.
            /// </summary>
            private static float ComputeChoiceContentLeft(int choiceIndex, float iconSize)
            {
                float strip = ComputeApproachStripWidth(choiceIndex, iconSize);
                return strip <= 0f
                    ? ChoiceTextRightPadding
                    : ApproachIconInset + strip + ApproachIconTextGap;
            }

            /// <summary>
            /// Moves a response's own text out of the way of what has been put inside the button with
            /// it - right of the approach icons, above the payoff strip - and left-aligns it, since a
            /// line centred in what is left of the button no longer lines up with the strip under it.
            /// </summary>
            private static void ApplyChoiceLayout(ChoiceButtonVisualState state, int choiceIndex, float iconSize)
            {
                if (state?.Label == null)
                {
                    return;
                }

                RectTransform labelRect = state.Label.rectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(ComputeChoiceContentLeft(choiceIndex, iconSize), PayoffBottomPadding + PayoffRowHeight);
                labelRect.offsetMax = new Vector2(-ChoiceTextRightPadding, -TextTopPadding);

                // Along the top of the band above the payoff strip, not centred in it. Centring put a
                // single line halfway down the button, which reads as floating; against the top the
                // response and its payoff strip sit as two lines of one block, and a response long
                // enough to wrap grows down into the band rather than pushing anything about.
                state.Label.alignment = TextAnchor.UpperLeft;
            }

            /// <summary>
            /// Puts a response's text back the way the button shipped it.
            /// </summary>
            private static void RestoreChoiceLayout(ChoiceButtonVisualState state)
            {
                if (state?.Label == null || !state.LayoutCaptured)
                {
                    return;
                }

                RectTransform labelRect = state.Label.rectTransform;
                labelRect.anchorMin = state.LabelAnchorMin;
                labelRect.anchorMax = state.LabelAnchorMax;
                labelRect.offsetMin = state.LabelOffsetMin;
                labelRect.offsetMax = state.LabelOffsetMax;
                state.Label.alignment = state.LabelAlignment;
                state.Label.color = state.LabelColor;
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
                public Vector2 OriginalSpacing;
                public GridLayoutGroup.Constraint OriginalConstraint;
                public int OriginalConstraintCount;
            }

            /// <summary>
            /// Lays the two responses out side by side and makes room inside them for what has been
            /// put there.
            ///
            /// The encounter ships its choices as a column of full-width buttons, which is right for
            /// an event with a list of answers and wrong for this screen: an incident asks the player
            /// to weigh two responses against each other, and a pair is compared side by side, not
            /// read down. With the walk-away choice moved to its own button there are exactly two
            /// left to place.
            ///
            /// Everything is set on the parent <see cref="GridLayoutGroup"/> rather than per button,
            /// because the grid overrides every child RectTransform to its own cell size - a button
            /// resized directly is resized straight back on the next layout pass.
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

                GridPaddingState state = grid.GetComponent<GridPaddingState>();
                if (state == null)
                {
                    state = grid.gameObject.AddComponent<GridPaddingState>();
                    state.OriginalCellSize = grid.cellSize;
                    state.OriginalPadding = new RectOffset(
                        grid.padding.left, grid.padding.right,
                        grid.padding.top, grid.padding.bottom);
                    state.OriginalSpacing = grid.spacing;
                    state.OriginalConstraint = grid.constraint;
                    state.OriginalConstraintCount = grid.constraintCount;
                }

                RectTransform containerRect = module.ChoiceButtonsContainer.GetComponent<RectTransform>();
                float containerWidth = containerRect != null ? containerRect.rect.width : 0f;

                // The container runs the full width of the screen, and the choices are not meant to.
                // Vanilla fills about half of it with one column; two responses side by side want
                // more than that and nowhere near all of it - edge to edge they stop reading as a
                // pair of cards and start reading as two bands across the screen.
                float available = (containerWidth * (1f - (ChoiceRowSideMargin * 2f))) - ChoiceColumnSpacing;

                float cellWidth = available > 0f
                    ? available / ChoiceColumns
                    : state.OriginalCellSize.x;

                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = ChoiceColumns;
                grid.spacing = new Vector2(ChoiceColumnSpacing, state.OriginalSpacing.y);

                // The button keeps the height the game gave it, and the payoff strip is fitted inside
                // that rather than added to it. Growing the row pushed it down the screen and left
                // the responses looking like two empty boxes with a line of text at the bottom; the
                // space a centred single line was wasting is exactly where the strip goes.
                grid.cellSize = new Vector2(cellWidth, state.OriginalCellSize.y);
            }

            /// <summary>
            /// How big an approach icon is drawn inside a response: as tall as the button leaves it.
            ///
            /// An affinity is the single thing that makes one response better for this operative than
            /// the other, so the icon saying which affinity is not an annotation on the response - it
            /// is half of what the response is. Sized to the button rather than to a constant, since
            /// the button's height is the game's to decide and changes with what is put in it.
            /// </summary>
            private static float ResolveApproachIconSize(GridPaddingState state)
            {
                if (state == null)
                {
                    return ApproachIconSize;
                }

                return Mathf.Max(MinApproachIconSize, state.OriginalCellSize.y - (ApproachIconVerticalInset * 2f));
            }

            /// <summary>
            /// The icon size the response buttons are currently drawn with, or the configured size
            /// when the grid has not been measured yet.
            /// </summary>
            private static float CurrentApproachIconSize(UIModuleSiteEncounters module)
            {
                GridLayoutGroup grid = module?.ChoiceButtonsContainer != null
                    ? module.ChoiceButtonsContainer.GetComponent<GridLayoutGroup>()
                    : null;

                return ResolveApproachIconSize(grid != null ? grid.GetComponent<GridPaddingState>() : null);
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
                EnableChoiceButtons(module);
            }


            // ── Approach selection logic ──────────────────────────────────

            private static void UpdateApproachSelectionForOperative(GeoscapeEvent geoEvent, GeoCharacter selectedCharacter)
            {
                _choiceApproaches.Clear();
                _selectedApproach.Clear();
                _approachSelectionLocked.Clear();
                _choiceAffinityMatch.Clear();
                _approachArrowRank.Clear();

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

                    if (approaches.Count == 0)
                    {
                        _selectedApproach[i] = null;
                        _approachSelectionLocked[i] = true;
                        _choiceAffinityMatch[i] = false;
                        _approachArrowRank[i] = 0;
                        continue;
                    }

                    if (hasAffinity)
                    {
                        if (approaches.Contains(charApproach))
                        {
                            // Operative has a matching affinity → auto-select, lock.
                            _selectedApproach[i] = charApproach;
                            _approachSelectionLocked[i] = true;
                            _choiceAffinityMatch[i] = true;

                            // An operative already at the top of their affinity has nothing left to
                            // gain from this response, so it is not promised one - the response is
                            // still theirs to take, it just stops claiming a level-up it cannot give.
                            _approachArrowRank[i] = charRank < MaxAffinityRank ? charRank + 1 : 0;
                        }
                        else
                        {
                            // Operative has a non-matching affinity → no selection possible, lock.
                            _selectedApproach[i] = null;
                            _approachSelectionLocked[i] = true;
                            _choiceAffinityMatch[i] = false;
                            _approachArrowRank[i] = 0;
                        }
                    }
                    else
                    {
                        // No affinity → either selectable, first selected by default. Whichever is
                        // selected when the response is taken is the one that develops, so both are
                        // worth a level-up mark and neither is worth colouring the response for.
                        _selectedApproach[i] = approaches[0];
                        _approachSelectionLocked[i] = false;
                        _choiceAffinityMatch[i] = false;
                        _approachArrowRank[i] = 1;
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
                RefreshApproachIconVisuals(parentButton, choiceIndex);
            }

            /// <summary>
            /// Restates what each approach icon means for the operative currently selected.
            ///
            /// Three states, and every one of them is about this operative rather than about the
            /// approach: the approach they bring an affinity to is at full strength with an amber
            /// outline, an approach still open to them (a rookie choosing what to develop) is legible
            /// but unlit, and one they can do nothing with is dimmed almost out. The level-up arrow
            /// rides the lit icon and carries the rank they would come back at.
            /// </summary>
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
                _approachArrowRank.TryGetValue(choiceIndex, out int arrowRank);

                for (int i = 0; i < root.childCount; i++)
                {
                    Transform child = root.GetChild(i);
                    ApproachIconState state = child.GetComponent<ApproachIconState>();
                    if (state == null)
                    {
                        continue;
                    }

                    Image img = FindApproachGlyph(child);
                    Outline outline = img != null ? img.GetComponent<Outline>() : null;
                    bool isSelected = selected.HasValue && selected.Value == state.Approach;

                    if (img != null)
                    {
                        Sprite original = GetApproachSprite(state.Approach);

                        if (isSelected)
                        {
                            img.sprite = original;
                            img.color = Color.white;
                        }
                        else if (locked)
                        {
                            // An approach this operative can do nothing with. Greyed, not faded: a
                            // faded orange mark still reads as orange, and so as available.
                            Sprite grey = GreyscaleSprites.For(original);
                            img.sprite = grey != null ? grey : original;
                            img.color = grey != null ? Color.white : IncidentUIStyle.ApproachInactive;
                        }
                        else
                        {
                            img.sprite = original;
                            img.color = IncidentUIStyle.ApproachSelectable;
                        }
                    }

                    if (outline != null)
                    {
                        outline.effectColor = isSelected ? IncidentUIStyle.Amber : Color.clear;
                    }

                    SetApproachArrow(child, isSelected ? arrowRank : 0);
                }
            }

            /// <summary>
            /// Shows or hides an approach icon's level-up arrow, and sets the rank it promises.
            ///
            /// The arrow is what turns the icon from a label into an offer: without it the player
            /// can see that an operative's affinity fits a response, but not that taking it is how
            /// that affinity grows. The rank rides on the arrow rather than in a line of prose above
            /// the crew, so the whole claim - which response, whose affinity, to what rank - is made
            /// in one place and is read by looking at the response being considered.
            /// </summary>
            private static void SetApproachArrow(Transform icon, int rank)
            {
                Transform existing = icon.Find(ApproachArrowName);

                if (rank <= 0)
                {
                    if (existing != null)
                    {
                        existing.gameObject.SetActive(false);
                    }

                    return;
                }

                GameObject arrowObject;
                if (existing == null)
                {
                    arrowObject = new GameObject(ApproachArrowName, typeof(RectTransform), typeof(HorizontalLayoutGroup));
                    arrowObject.transform.SetParent(icon, false);

                    RectTransform arrowRect = arrowObject.GetComponent<RectTransform>();

                    // Centred on the top of its own icon. In the corner it fell in the gap between
                    // two icons and there was no telling which of them it was promising - and which
                    // one it belongs to is the whole point of it.
                    arrowRect.anchorMin = new Vector2(0.5f, 1f);
                    arrowRect.anchorMax = new Vector2(0.5f, 1f);
                    arrowRect.pivot = new Vector2(0.5f, 0.5f);
                    arrowRect.anchoredPosition = new Vector2(0f, -ApproachArrowSize * 0.5f);
                    arrowRect.sizeDelta = new Vector2(ApproachArrowSize * 1.8f, ApproachArrowSize);

                    HorizontalLayoutGroup arrowLayout = arrowObject.GetComponent<HorizontalLayoutGroup>();
                    arrowLayout.childAlignment = TextAnchor.MiddleCenter;
                    arrowLayout.childControlWidth = true;
                    arrowLayout.childControlHeight = true;
                    arrowLayout.childForceExpandWidth = false;
                    arrowLayout.childForceExpandHeight = false;
                    arrowLayout.spacing = 1f;

                    CreateArrowGlyph(arrowObject.transform);
                    CreateArrowRankLabel(arrowObject.transform);
                }
                else
                {
                    arrowObject = existing.gameObject;
                }

                Text label = arrowObject.GetComponentInChildren<Text>(true);
                if (label != null)
                {
                    label.text = rank.ToString(CultureInfo.InvariantCulture);
                }

                arrowObject.SetActive(true);
            }

            private static void CreateArrowGlyph(Transform parent)
            {
                GameObject glyphObject = new GameObject("Glyph", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                glyphObject.transform.SetParent(parent, false);

                LayoutElement glyphLayout = glyphObject.GetComponent<LayoutElement>();
                glyphLayout.preferredWidth = ApproachArrowSize * 0.72f;
                glyphLayout.preferredHeight = ApproachArrowSize * 0.72f;
                glyphLayout.minWidth = ApproachArrowSize * 0.72f;
                glyphLayout.minHeight = ApproachArrowSize * 0.72f;

                Image glyph = glyphObject.GetComponent<Image>();
                glyph.sprite = ResolveLevelUpArrowSprite();
                glyph.color = IncidentUIStyle.AffinityMatch;
                glyph.preserveAspect = true;
                glyph.raycastTarget = false;
                glyph.enabled = glyph.sprite != null;
            }

            private static void CreateArrowRankLabel(Transform parent)
            {
                GameObject labelObject = new GameObject("Rank", typeof(RectTransform), typeof(Text), typeof(Outline));
                labelObject.transform.SetParent(parent, false);

                Text label = labelObject.GetComponent<Text>();
                label.font = _cachedFont;
                label.fontSize = Mathf.RoundToInt(ApproachArrowSize * 0.85f);
                label.fontStyle = FontStyle.Bold;
                label.alignment = TextAnchor.MiddleLeft;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.verticalOverflow = VerticalWrapMode.Overflow;
                label.raycastTarget = false;
                label.color = IncidentUIStyle.AffinityMatch;

                // The badge sits over a bright glyph on a button that is not a flat colour, so both
                // halves of it are outlined rather than trusted to contrast.
                Outline outline = labelObject.GetComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
                outline.effectDistance = new Vector2(1.5f, -1.5f);
                outline.useGraphicAlpha = false;
            }

            /// <summary>
            /// The level-up arrow, loaded once. A shipped image rather than a character, because the
            /// game's UI font has no arrow glyph and a missing one draws nothing at all.
            /// </summary>
            private static Sprite ResolveLevelUpArrowSprite()
            {
                if (!_levelUpArrowResolved)
                {
                    _levelUpArrowResolved = true;
                    _levelUpArrowSprite = Helper.CreateSpriteFromImageFile(LevelUpArrowImageName);

                    if (_levelUpArrowSprite == null)
                    {
                        TFTVLogger.Always($"[IncidentUI] {LevelUpArrowImageName} could not be loaded; level-up badges show the rank only.");
                    }
                }

                return _levelUpArrowSprite;
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

            private static void ApplyChoiceIcons(SiteBaseChoiceButton button, int choiceIndex, float iconSize)
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

                float totalWidth = (count * iconSize) + (Mathf.Max(0, count - 1) * h.spacing);
                rootRect.sizeDelta = new Vector2(count > 0 ? totalWidth : 0f, iconSize);

                // Inside the response's left edge, level with its text. The icons used to flank the
                // buttons, which read as decoration alongside them; inside, they are part of what the
                // response says - this is what taking it asks of the operative - and both responses
                // carry theirs in the same place, so the pair can be compared at a glance.
                rootRect.anchorMin = new Vector2(0f, 0.5f);
                rootRect.anchorMax = new Vector2(0f, 0.5f);
                rootRect.pivot = new Vector2(0f, 0.5f);
                rootRect.anchoredPosition = new Vector2(ApproachIconInset, 0f);

                _approachSelectionLocked.TryGetValue(choiceIndex, out bool locked);

                // Create/update exactly the icons needed.
                for (int i = 0; i < count; i++)
                {
                    string iconName = ChoiceIconNamePrefix + i;
                    Transform iconTransform = rootObject.transform.Find(iconName);
                    GameObject iconObject;

                    if (iconTransform == null)
                    {
                        iconObject = new GameObject(iconName, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                        iconObject.transform.SetParent(rootObject.transform, false);
                    }
                    else
                    {
                        iconObject = iconTransform.gameObject;
                    }

                    LayoutElement le = iconObject.GetComponent<LayoutElement>();
                    if (le == null) le = iconObject.AddComponent<LayoutElement>();
                    le.preferredWidth = iconSize;
                    le.preferredHeight = iconSize;
                    le.minWidth = iconSize;
                    le.minHeight = iconSize;

                    // The icon's box is a black plate running the response's full height, and the
                    // affinity mark is drawn smaller on top of it. The artwork is already an orange
                    // mark on a black plate of its own, so scaling the artwork scales its plate with
                    // it - which is why shrinking the mark shrank the whole icon. Painting the plate
                    // here instead separates the two: the mark is sized on its own, and the black it
                    // sits on is the box.
                    Image img = iconObject.GetComponent<Image>();
                    if (img == null) img = iconObject.AddComponent<Image>();
                    img.sprite = null;
                    img.color = IncidentUIStyle.ApproachPlate;
                    img.raycastTarget = true;
                    img.enabled = true;

                    Image glyph = EnsureApproachGlyph(iconObject.transform, iconSize);

                    Outline outline = glyph.GetComponent<Outline>();
                    if (outline == null) outline = glyph.gameObject.AddComponent<Outline>();
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
                    glyph.sprite = sprite;
                    glyph.enabled = sprite != null;

                    ApproachIconState iconState = iconObject.GetComponent<ApproachIconState>();
                    if (iconState == null) iconState = iconObject.AddComponent<ApproachIconState>();
                    iconState.ChoiceIndex = choiceIndex;
                    iconState.Approach = approach;
                    iconState.ParentChoiceButton = button;

                    ApproachIconTooltipTrigger tooltipTrigger = iconObject.GetComponent<ApproachIconTooltipTrigger>();
                    if (tooltipTrigger == null) tooltipTrigger = iconObject.AddComponent<ApproachIconTooltipTrigger>();
                    tooltipTrigger.Approach = approach;

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

            /// <summary>
            /// The affinity glyph drawn inside an approach icon's box, at half the box's size and
            /// centred in it - the proportion the mockup draws them at.
            /// </summary>
            private static Image EnsureApproachGlyph(Transform icon, float iconSize)
            {
                Transform existing = icon.Find(ApproachGlyphName);
                GameObject glyphObject;

                if (existing == null)
                {
                    glyphObject = new GameObject(ApproachGlyphName, typeof(RectTransform), typeof(Image));
                    glyphObject.transform.SetParent(icon, false);
                }
                else
                {
                    glyphObject = existing.gameObject;
                }

                RectTransform glyphRect = glyphObject.GetComponent<RectTransform>();
                glyphRect.anchorMin = new Vector2(0.5f, 0.5f);
                glyphRect.anchorMax = new Vector2(0.5f, 0.5f);
                glyphRect.pivot = new Vector2(0.5f, 0.5f);
                glyphRect.anchoredPosition = Vector2.zero;
                glyphRect.sizeDelta = new Vector2(iconSize * ApproachGlyphFraction, iconSize * ApproachGlyphFraction);

                Image glyph = glyphObject.GetComponent<Image>();
                glyph.preserveAspect = true;
                glyph.raycastTarget = false;
                return glyph;
            }

            /// <summary>
            /// The glyph inside an approach icon's box, or null for a box that has none yet.
            /// </summary>
            private static Image FindApproachGlyph(Transform icon)
            {
                Transform glyph = icon != null ? icon.Find(ApproachGlyphName) : null;
                return glyph != null ? glyph.GetComponent<Image>() : null;
            }

            // ── Payoff strip ─────────────────────────────────────────────

            /// <summary>
            /// The strip along the bottom of a response: how long it takes this operative, and what
            /// it pays if it works.
            ///
            /// The hours were appended to the response's own text before, which put a number in the
            /// middle of a sentence and left the two responses with nothing comparable about them but
            /// prose. On its own line with the payoff icons, the hours become one of two axes the
            /// player is choosing along - how long, and for what - and the pair of responses can be
            /// read against each other a column at a time.
            /// </summary>
            private static void ApplyChoicePayoff(
                SiteBaseChoiceButton button,
                GeoscapeEvent geoEvent,
                GeoVehicle vehicle,
                int choiceIndex,
                GeoCharacter leader,
                float iconSize,
                int fontSize)
            {
                if (button == null)
                {
                    return;
                }

                Transform existing = button.transform.Find(ChoicePayoffRootName);
                GameObject root;
                if (existing == null)
                {
                    root = new GameObject(ChoicePayoffRootName, typeof(RectTransform), typeof(HorizontalLayoutGroup));
                    root.transform.SetParent(button.transform, false);
                }
                else
                {
                    root = existing.gameObject;

                    // Rebuilt from scratch on every leader change: the hours move with the operative,
                    // and reusing entries in place means matching up two lists that need not be the
                    // same length.
                    for (int i = root.transform.childCount - 1; i >= 0; i--)
                    {
                        UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
                    }
                }

                RectTransform rootRect = root.GetComponent<RectTransform>();
                rootRect.anchorMin = new Vector2(0f, 0f);
                rootRect.anchorMax = new Vector2(1f, 0f);
                rootRect.pivot = new Vector2(0f, 0f);
                rootRect.offsetMin = new Vector2(ComputeChoiceContentLeft(choiceIndex, iconSize), PayoffBottomPadding);
                rootRect.offsetMax = new Vector2(-ChoiceTextRightPadding, PayoffBottomPadding + PayoffRowHeight);

                HorizontalLayoutGroup layout = root.GetComponent<HorizontalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
                layout.spacing = PayoffEntrySpacing;

                bool anything = false;

                string hours = ResolveApproachHoursText(geoEvent, vehicle, choiceIndex, leader);
                if (!string.IsNullOrEmpty(hours))
                {
                    _choiceAffinityMatch.TryGetValue(choiceIndex, out bool matches);
                    AddPayoffLabel(root.transform, hours, matches ? IncidentUIStyle.AffinityMatch : IncidentUIStyle.Payoff, fontSize);
                    anything = true;
                }

                foreach (ChoicePayoff.Entry entry in ChoicePayoff.Resolve(geoEvent, choiceIndex))
                {
                    AddPayoffEntry(root.transform, entry, fontSize);
                    anything = true;
                }

                root.SetActive(anything);
            }

            private static void AddPayoffEntry(Transform parent, ChoicePayoff.Entry entry, int fontSize)
            {
                GameObject group = new GameObject("Payoff", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                group.transform.SetParent(parent, false);

                HorizontalLayoutGroup layout = group.GetComponent<HorizontalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
                layout.spacing = PayoffIconLabelSpacing;

                GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                iconObject.transform.SetParent(group.transform, false);

                LayoutElement iconLayout = iconObject.GetComponent<LayoutElement>();
                iconLayout.preferredWidth = PayoffIconSize;
                iconLayout.preferredHeight = PayoffIconSize;
                iconLayout.minWidth = PayoffIconSize;
                iconLayout.minHeight = PayoffIconSize;

                Image icon = iconObject.GetComponent<Image>();
                icon.sprite = entry.Icon;
                icon.color = entry.Tint;
                icon.preserveAspect = true;
                icon.raycastTarget = false;

                // One plus per step of the scale. A number here would be a promise the incident does
                // not make - it can still fail, and the size is what is being compared, not the total.
                AddPayoffLabel(group.transform, new string(TierMark, Mathf.Clamp(entry.Tier, 1, ChoicePayoff.MaxTier)), entry.Tint, fontSize);
            }

            private static void AddPayoffLabel(Transform parent, string text, Color color, int fontSize)
            {
                GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelObject.transform.SetParent(parent, false);

                Text label = labelObject.GetComponent<Text>();
                label.text = text;
                label.font = _cachedFont;

                // The same size as the response it belongs to. The hours and the payoff are not a
                // footnote on the response - they are the half of it the player is comparing - and
                // set smaller they read as small print.
                label.fontSize = fontSize;
                label.fontStyle = FontStyle.Bold;
                label.alignment = TextAnchor.MiddleLeft;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.verticalOverflow = VerticalWrapMode.Overflow;
                label.raycastTarget = false;
                label.color = color;
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

            private static void ResetSelectedLeaderContext(GeoscapeEvent geoEvent, GeoVehicle vehicle)
            {
                _selectedEventId = geoEvent?.EventID ?? string.Empty;
                _selectedVehicleId = vehicle?.VehicleID ?? -1;
                _selectedLeaderId = -1;
                _currentSelectedCharacter = null;

                _choiceApproaches.Clear();
                _selectedApproach.Clear();
                _approachSelectionLocked.Clear();
                _choiceAffinityMatch.Clear();
                _approachArrowRank.Clear();
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
                IEnumerable<GeoCharacter> source = vehicle.GetAllCharacters()
                    .Where(c => c != null && c.TemplateDef != null && c.TemplateDef.IsHuman
                             && PersonnelRestrictions.CanContributeToIncidents(c));
                if (CrewFilter != null)
                {
                    source = source.Where(CrewFilter);
                }
                return source.ToList();
            }

            private static void DisableChoiceButtons(UIModuleSiteEncounters module, string reason)
            {
                if (module?.ChoiceButtonsContainer == null) return;
                foreach (SiteBaseChoiceButton cb in module.ChoiceButtonsContainer
                         .GetComponentsInChildren<SiteBaseChoiceButton>(true))
                {
                    if (cb == null) continue;

                    // Never disable the cancel choice (CHOICE_2).
                    string key = cb.Choice?.Text?.LocalizationKey ?? string.Empty;
                    if (key.IndexOf("_CHOICE_2", StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;

                    Button btn = cb.GetComponent<Button>();
                    if (btn != null) btn.interactable = false;

                    ChoiceButtonVisualState state = EnsureChoiceButtonVisualState(cb);
                    if (state?.Label != null && !string.IsNullOrEmpty(reason))
                        state.Label.text = reason;
                }
            }

            private static void EnableChoiceButtons(UIModuleSiteEncounters module)
            {
                if (module?.ChoiceButtonsContainer == null) return;
                foreach (SiteBaseChoiceButton cb in module.ChoiceButtonsContainer
                         .GetComponentsInChildren<SiteBaseChoiceButton>(true))
                {
                    if (cb == null) continue;
                    Button btn = cb.GetComponent<Button>();
                    if (btn != null) btn.interactable = true;
                }
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

                header.text = TFTVCommonMethods.ConvertKeyToString(SelectLeaderKey);
                _currentSelectedCharacter = selectedCharacter;
            }


            /// <summary>
            /// How long this response takes the given operative, or empty when that cannot be worked
            /// out. Empty rather than a placeholder: the payoff strip simply leaves the hours out, which
            /// says nothing, where a placeholder would say the incident takes an unknown length of time.
            /// </summary>
            private static string ResolveApproachHoursText(GeoscapeEvent geoEvent, GeoVehicle vehicle, int choiceIndex, GeoCharacter leader)
            {
                if (!Resolution.IncidentController.TryComputeIncidentHours(geoEvent, vehicle, choiceIndex, leader, out float hours)
                    || hours <= 0f)
                {
                    return string.Empty;
                }

                return TFTVCommonMethods.FormatKey(HoursKey, hours.ToString("0", CultureInfo.InvariantCulture));
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
                    // Immediate, not deferred: the rendered heads these cards are drawing are freed
                    // in the same pass, and a deferred destroy would leave the outgoing row holding
                    // textures that no longer exist for the rest of the frame.
                    UnityEngine.Object.DestroyImmediate(toRemove[i].gameObject);
                }
            }
        }

        /// <summary>
        /// Undecorates the choices row as an encounter closes.
        ///
        /// Choosing a response or cancelling out of an incident answers on a closing screen drawn on
        /// the same buttons the incident borrowed, and that screen is not shown through SetEncounter -
        /// so without this it inherits the grown grid and whatever was put inside the buttons.
        /// </summary>
        [HarmonyPatch(typeof(UIModuleSiteEncounters), "SetClosingEncounter")]
        internal static class GeoscapeEventClosingEncounterPatch
        {
            static bool Prepare() => TFTVAircraftReworkMain.AircraftReworkOn;

            public static void Prefix(UIModuleSiteEncounters __instance)
            {
                try
                {
                    if (!TFTVBaseRework.BaseReworkCheck.BaseReworkEnabled)
                    {
                        return;
                    }

                    GeoscapeEventCrewListPatch.RestoreVanillaChoiceLayout(__instance);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
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
                background.color = new Color(0f, 0f, 0f, 1f);

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

                rootRect.anchorMin = new Vector2(0f, 0f);
                rootRect.anchorMax = new Vector2(0f, 0f);
                rootRect.pivot = new Vector2(0f, 0f);
                rootRect.localScale = Vector3.one;
                rootRect.sizeDelta = new Vector2(PanelWidth, 0f);
                rootRect.anchoredPosition = new Vector2(100f, 20f);
            }
        }
    }
}