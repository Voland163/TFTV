using HarmonyLib;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.Entities.Abilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
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

            private const float CrewPanelTopPadding = 8f;
            private const float GridSpacingX = 10f;
            private const float GridSpacingY = 6f;
            private const float HeaderToGridSpacing = 50f;
            private const float MaxGridWidth = 1000f;// 840f;
            private const float FallbackGridWidth = 900f;// 700f;
            private const float MinCellHeight = 42f;
            private const int MaxVisibleEntries = 8;
            private const int GridColumns = 2;

            public static Func<SoldierSlotController> SoldierSlotPrefabProvider;
            public static Func<GeoCharacter, Sprite> ExtraIconResolver;
            public static Func<GeoCharacter, bool> CrewFilter;

            private static SoldierSlotController _selectedRow;
            private static int _selectedLeaderId = -1;
            private static int _selectedVehicleId = -1;
            private static string _selectedEventId = string.Empty;

            private sealed class CrewRowHighlightState : MonoBehaviour
            {
                public Image Background;
                public Outline Border;
                public Color BackgroundNormal;
                public Color BorderNormal;
                public bool Initialized;
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

            public static void Postfix(UIModuleSiteEncounters __instance, GeoscapeEvent geoEvent, bool pagingEvent)
            {
                if (__instance == null || geoEvent?.Context == null || pagingEvent)
                {
                    return;
                }

                Transform parent = __instance.SiteEncounterTextContainer != null ? __instance.SiteEncounterTextContainer.transform : null;
                if (parent == null)
                {
                    return;
                }

                RemovePreviousInjectedRows(parent);
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
                GameObject grid = new GameObject(CrewGridName, typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
                grid.transform.SetParent(root.transform, false);
                ConfigureGrid(grid.GetComponent<RectTransform>(), grid.GetComponent<GridLayoutGroup>(), grid.GetComponent<ContentSizeFitter>(), panelWidth, rowPrefab);

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
                }
            }

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

                return Resources.FindObjectsOfTypeAll<SoldierSlotController>().FirstOrDefault();
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

                string approachAHours = ResolveApproachHoursText(geoEvent, vehicle, 0, selectedCharacter);
                string approachBHours = ResolveApproachHoursText(geoEvent, vehicle, 1, selectedCharacter);

                header.text = $"Select Leading Operative for the Incident. Approach A hours: {approachAHours} | Approach B hours: {approachBHours}";
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
                if (hours <= 0f)
                {
                    return "N/A";
                }

                return hours.ToString("0.#", CultureInfo.InvariantCulture) + "h";
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
                iconRect.anchoredPosition = new Vector2(row.NameLabel.rectTransform.anchoredPosition.x + nameWidth + 10f, row.NameLabel.rectTransform.anchoredPosition.y);
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

            private static void RemovePreviousInjectedRows(Transform parent)
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
    }
}
