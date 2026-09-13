using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static TFTV.TFTVBaseRework.PersonnelData;
using static TFTV.TFTVBaseRework.Workers;
using Object = UnityEngine.Object;

namespace TFTV.TFTVBaseRework
{
    /// <summary>
    /// The roster column: the pool of people Phoenix has not put to work yet, plus the operatives on
    /// field duty, who are here so they can be dismissed back into that pool. Anyone researching,
    /// fabricating or training is listed by the panel that owns them instead of being repeated here.
    /// </summary>
    public static partial class PersonnelManagementUI
    {
        #region State

        internal enum RosterFilter
        {
            All,
            FieldDuty,
            Dismissed,
            Civilians
        }

        private static RosterFilter _rosterFilter = RosterFilter.All;

        internal const string RosterPanelName = "RosterPanel";

        private const float RosterRowHeight = 68f;
        private const float RosterActionSize = 64f;
        private const float RosterBadgeSize = 46f;
        private const float RosterStatusWidth = 260f;

        /// <summary>The vanilla slot's class icon is drawn for a much larger row than this one.</summary>
        private const float ClassIconScale = 0.6f;

        private sealed class RosterEntry
        {
            public GeoCharacter Character;
            public PersonnelInfo Personnel;
            public bool IsFieldOperative;

            public string Name => Character?.DisplayName ?? PersonnelText.Get(PersonnelText.StatusUnknownName);

            public PersonnelAssignment Assignment =>
                Personnel?.Assignment ?? PersonnelAssignment.Unassigned;

            /// <summary>An operative who was taken off field duty: they keep their class and level.</summary>
            public bool IsDismissed => PersonnelRestrictions.IsDismissedOperative(Character);
        }

        /// <summary>
        /// A built row and the two facts the tabs sort on, so switching tabs is a pass over this
        /// list rather than a rebuild of every row.
        /// </summary>
        private sealed class RosterRowView
        {
            public GameObject Row;
            public Image Background;
            public bool IsFieldOperative;
            public bool IsDismissed;
        }

        private sealed class RosterTabView
        {
            public RosterFilter Filter;
            public Image Background;
            public Text Caption;
            public int Count;
        }

        private static readonly List<RosterRowView> _rosterRows = new List<RosterRowView>();
        private static readonly List<RosterTabView> _rosterTabViews = new List<RosterTabView>();
        private static Text _rosterCountLabel;
        private static GameObject _rosterEmptyLabel;

        #endregion

        #region Column

        internal static GameObject CreateRosterColumn(Transform parent, GeoLevelController level, GeoPhoenixFaction phoenix,
            SoldierSlotController slotPrefab, FacilitySlotPools pools)
        {
            GameObject panel = CreateFramedPanel(parent, RosterPanelName, out Transform content);
            LayoutElement panelElement = panel.GetComponent<LayoutElement>() ?? panel.AddComponent<LayoutElement>();
            panelElement.flexibleWidth = 30f;
            panelElement.flexibleHeight = 1f;

            _rosterRows.Clear();
            _rosterTabViews.Clear();

            // Every row is built once, for everyone. The tabs then only change which of them are
            // shown, which is why switching one costs nothing.
            List<RosterEntry> entries = BuildRosterEntries(phoenix);

            Transform header = CreateSectionHeader(content, PersonnelText.Get(PersonnelText.RosterTitle),
                GetColumnIconSprite(PersonnelAssignment.Unassigned), TextPrimaryColor);
            _rosterCountLabel = CreateLabel(header, "Count", string.Empty, TitleFontSize, AccentOrangeColor,
                TextAnchor.MiddleRight);
            SetSize(_rosterCountLabel.gameObject, 90f, 0f);

            CreateFilterTabs(content, entries);

            CreateScrollList(content, "RosterList", out Transform list);

            foreach (RosterEntry entry in entries)
            {
                CreateRosterRow(list, entry, level, phoenix, slotPrefab, pools);
            }

            Text empty = CreateLabel(list, "Empty", PersonnelText.Get(PersonnelText.RosterEmpty), BodyFontSize,
                TextDimColor, TextAnchor.MiddleCenter);
            SetSize(empty.gameObject, 0f, RosterRowHeight);
            _rosterEmptyLabel = empty.gameObject;

            CreateRosterOptions(content, level, phoenix);

            ApplyRosterFilter();

            return panel;
        }

        /// <summary>Drops what the roster column left behind, once it has been destroyed.</summary>
        internal static void ResetRosterView()
        {
            RosterRowActions.Forget();
            _rosterRows.Clear();
            _rosterTabViews.Clear();
            _rosterCountLabel = null;
            _rosterEmptyLabel = null;
        }

        /// <summary>
        /// Shows the rows the current tab asks for and hides the rest, restriping what is left so
        /// the banding still alternates, and retitles the tabs and the count to match.
        /// </summary>
        private static void ApplyRosterFilter()
        {
            RosterRowActions.CloseOpen();

            int visible = 0;
            foreach (RosterRowView view in _rosterRows)
            {
                if (view.Row == null)
                {
                    continue;
                }

                bool show = MatchesFilter(view, _rosterFilter);
                if (view.Row.activeSelf != show)
                {
                    view.Row.SetActive(show);
                }

                if (!show)
                {
                    continue;
                }

                if (view.Background != null)
                {
                    view.Background.color = visible % 2 == 0 ? RowFillColor : RowFillAltColor;
                }

                visible++;
            }

            if (_rosterCountLabel != null)
            {
                _rosterCountLabel.text = visible.ToString();
            }

            if (_rosterEmptyLabel != null)
            {
                _rosterEmptyLabel.SetActive(visible == 0);
            }

            foreach (RosterTabView tab in _rosterTabViews)
            {
                bool active = tab.Filter == _rosterFilter;

                if (tab.Background != null)
                {
                    tab.Background.color = active ? AccentOrangeColor : ButtonFillColor;
                }

                if (tab.Caption != null)
                {
                    tab.Caption.color = active
                        ? Color.black
                        : (tab.Count > 0 ? TextDimColor : TextDisabledColor);
                }
            }
        }

        private static bool MatchesFilter(RosterRowView view, RosterFilter filter)
        {
            switch (filter)
            {
                case RosterFilter.FieldDuty:
                    return view.IsFieldOperative;
                case RosterFilter.Dismissed:
                    return !view.IsFieldOperative && view.IsDismissed;
                case RosterFilter.Civilians:
                    return !view.IsFieldOperative && !view.IsDismissed;
                default:
                    return true;
            }
        }

        /// <summary>
        /// The tabs sort the pool into the three kinds of people in it - operatives still in the
        /// field, operatives who have been dismissed, and civilians - rather than into assigned and
        /// unassigned, which the panels on the right already answer.
        /// </summary>
        private static void CreateFilterTabs(Transform parent, List<RosterEntry> entries)
        {
            GameObject row = CreateUIObject("FilterTabs", parent);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 4f;
            layout.padding = new RectOffset(4, 4, 2, 2);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            SetSize(row, 0f, 48f);

            CreateFilterTab(row.transform, RosterFilter.All, PersonnelText.FilterAll, entries.Count);
            CreateFilterTab(row.transform, RosterFilter.FieldDuty, PersonnelText.FilterFieldDuty,
                entries.Count(e => e.IsFieldOperative));
            CreateFilterTab(row.transform, RosterFilter.Dismissed, PersonnelText.FilterDismissed,
                entries.Count(e => !e.IsFieldOperative && e.IsDismissed));
            CreateFilterTab(row.transform, RosterFilter.Civilians, PersonnelText.FilterCivilians,
                entries.Count(e => !e.IsFieldOperative && !e.IsDismissed));
        }

        private static void CreateFilterTab(Transform parent, RosterFilter filter, string captionKey, int count)
        {
            Button button = CreateTextButton(parent, $"Tab_{filter}", PersonnelText.Get(captionKey), () =>
            {
                _rosterFilter = filter;
                ApplyRosterFilter();
            },
            height: 44f,
            fontSize: SmallFontSize);

            LayoutElement element = button.gameObject.GetComponent<LayoutElement>();
            element.flexibleWidth = 1f;

            // Colours are left to ApplyRosterFilter, which is also what repaints them on a switch.
            _rosterTabViews.Add(new RosterTabView
            {
                Filter = filter,
                Background = button.targetGraphic as Image,
                Caption = button.GetComponentInChildren<Text>(true),
                Count = count
            });
        }

        private static void CreateRosterOptions(Transform parent, GeoLevelController level, GeoPhoenixFaction phoenix)
        {
            EnsureAutoAssignSettingInitialized(level);

            CreateCheckbox(parent, "AutoAssignToggle", PersonnelText.Get(PersonnelText.AutoAssign), AutoAssignEnabled, () =>
                {
                    SetAutoAssignEnabled(level, !AutoAssignEnabled);
                    if (AutoAssignEnabled)
                    {
                        TryAutoAssignUnassignedPersonnel(phoenix, "AutoAssignToggle");
                    }
                    RefreshPanel();
                }, height: 58f, fontSize: TitleFontSize);
        }

        #endregion

        #region Entries

        private static List<RosterEntry> BuildRosterEntries(GeoPhoenixFaction phoenix)
        {
            var entries = new List<RosterEntry>();
            if (phoenix == null)
            {
                return entries;
            }

            // Everyone the personnel records own, whatever they are doing. Soldiers who are working
            // a duty are deliberately left in GeoPhoenixFaction.Soldiers by GeoCharacterFilter, so
            // without this they come back round as field operatives and are listed twice over.
            var managed = new HashSet<int>();

            foreach (PersonnelInfo person in Assignments.Values)
            {
                if (person?.Character == null || person.Character.Faction != phoenix)
                {
                    continue;
                }

                managed.Add(person.Character.Id);

                // Whoever is researching, fabricating or training is listed in that panel, with the
                // control that ends the assignment. Repeating them here made the roster twice as
                // long as it needed to be and left the same person on screen in two places.
                if (person.Assignment != PersonnelAssignment.Unassigned)
                {
                    continue;
                }

                entries.Add(new RosterEntry { Character = person.Character, Personnel = person });
            }

            // What is left of GeoPhoenixFaction.Soldiers once the base's own people are set aside is
            // genuinely on field duty.
            foreach (GeoCharacter soldier in phoenix.Soldiers)
            {
                if (soldier?.TemplateDef == null || !soldier.TemplateDef.IsHuman)
                {
                    continue;
                }

                if (managed.Contains(soldier.Id))
                {
                    continue;
                }

                entries.Add(new RosterEntry { Character = soldier, IsFieldOperative = true });
            }

            return entries.OrderBy(e => e.Name).ToList();
        }

        #endregion

        #region Rows

        private static void CreateRosterRow(Transform parent, RosterEntry entry, GeoLevelController level,
            GeoPhoenixFaction phoenix, SoldierSlotController slotPrefab, FacilitySlotPools pools)
        {
            if (entry?.Character == null)
            {
                return;
            }

            GameObject row = CreateUIObject($"RosterRow_{entry.Character.Id}", parent);
            var background = row.AddComponent<Image>();

            _rosterRows.Add(new RosterRowView
            {
                Row = row,
                Background = background,
                IsFieldOperative = entry.IsFieldOperative,
                IsDismissed = entry.IsDismissed
            });

            // One line per person, so the list does not grow a row taller when its actions open.
            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 6f;
            rowLayout.padding = new RectOffset(2, 2, 2, 2);
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
            SetSize(row, 0f, RosterRowHeight);

            CreateNameCell(row.transform, entry, slotPrefab);

            Text status = CreateLabel(row.transform, "Status", GetRosterStatusText(entry), BodyFontSize,
                GetRosterStatusColor(entry), TextAnchor.MiddleRight);
            SetSize(status.gameObject, RosterStatusWidth, RosterRowHeight);

            // The badge centres itself on its parent, so it gets a cell of its own rather than being
            // laid out as a sibling of the name and status.
            GameObject affinityCell = CreateUIObject("AffinityCell", row.transform);
            SetSize(affinityCell, RosterBadgeSize + 6f, RosterRowHeight);
            AddAffinityBadge(affinityCell, entry.Character, RosterBadgeSize);

            if (entry.IsFieldOperative)
            {
                // Field operatives have exactly one thing that can be done to them from here, so
                // they get that control directly instead of a "+" that opens a strip of one.
                Button dismiss = CreateIconButton(row.transform, "Dismiss", GetDismissIconSprite(),
                    () => ConfirmDismissFromFieldDuty(entry.Character, level, phoenix),
                    size: RosterActionSize, fillColor: ButtonFillDangerColor, fallbackCaption: "X");
                AddTextTooltip(dismiss.gameObject, PersonnelText.Get(PersonnelText.ActionDismiss));
                return;
            }

            var actions = row.AddComponent<RosterRowActions>();
            Button expand = CreateIconButton(row.transform, "Expand", null, () => actions.Toggle(),
                size: RosterActionSize, fallbackCaption: RosterRowActions.ExpandCaption);

            actions.Initialize(status.gameObject, affinityCell, expand,
                () => CreateRowActionStrip(row.transform, entry, level, phoenix, pools));
        }

        /// <summary>
        /// Name cell: the vanilla soldier slot, which brings the class icon, level and name styling
        /// with it. Civilians have neither class nor level, so those two elements are switched off
        /// for them - operatives, dismissed or serving, keep them.
        /// </summary>
        private static void CreateNameCell(Transform parent, RosterEntry entry, SoldierSlotController slotPrefab)
        {
            if (slotPrefab != null)
            {
                SoldierSlotController slot = Object.Instantiate(slotPrefab, parent, false);
                slot.gameObject.name = $"Name_{entry.Character.Id}";
                slot.gameObject.SetActive(true);
                slot.SetSoldierData((ICommonActor)entry.Character);
                slot.ActorSelected = null;

                Button slotButton = slot.GetComponent<Button>();
                if (slotButton != null)
                {
                    slotButton.onClick.RemoveAllListeners();
                    slotButton.interactable = false;
                }

                bool showsClass = entry.IsFieldOperative || entry.IsDismissed;
                if (!showsClass)
                {
                    if (slot.IconElement != null)
                    {
                        slot.IconElement.gameObject.SetActive(false);
                    }
                    if (slot.LevelLabel != null)
                    {
                        slot.LevelLabel.gameObject.SetActive(false);
                    }
                }
                else if (slot.IconElement != null)
                {
                    // The prefab sizes its icon through its own layout, so scaling is what actually
                    // takes effect here.
                    slot.IconElement.transform.localScale = new Vector3(ClassIconScale, ClassIconScale, 1f);
                }

                // Serving operatives are the one group on this screen that cannot be put to work, so
                // their names carry the same orange as their status rather than only the status
                // doing the telling - a dismissed operative reads the same at a glance otherwise.
                if (entry.IsFieldOperative && slot.NameLabel != null)
                {
                    slot.NameLabel.color = AccentOrangeColor;
                }

                RectTransform slotRect = slot.GetComponent<RectTransform>();
                if (slotRect != null)
                {
                    slotRect.anchorMin = new Vector2(0f, 0.5f);
                    slotRect.anchorMax = new Vector2(0f, 0.5f);
                    slotRect.pivot = new Vector2(0f, 0.5f);
                }

                LayoutElement slotElement = SetSize(slot.gameObject, 0f, RosterRowHeight);
                slotElement.flexibleWidth = 1f;
                return;
            }

            Text fallback = CreateLabel(parent, "Name", entry.Name, BodyFontSize,
                entry.IsFieldOperative ? AccentOrangeColor : TextPrimaryColor);
            LayoutElement element = SetSize(fallback.gameObject, 0f, RosterRowHeight);
            element.flexibleWidth = 1f;
        }

        private static string GetRosterStatusText(RosterEntry entry)
        {
            if (entry.IsFieldOperative)
            {
                return PersonnelText.Get(PersonnelText.StatusFieldDuty);
            }

            return PersonnelText.Get(entry.IsDismissed
                ? PersonnelText.StatusDismissed
                : PersonnelText.StatusIdle);
        }

        private static Color GetRosterStatusColor(RosterEntry entry)
        {
            return entry.IsFieldOperative ? AccentOrangeColor : TextDimColor;
        }

        #endregion

        #region Row actions

        /// <summary>
        /// The strip of duties a free person can be put on, built on demand when their row is opened
        /// and thrown away when it closes. It takes the place of the status and affinity cells on the
        /// same line, so opening a row never moves the rows under it.
        /// </summary>
        private static GameObject CreateRowActionStrip(Transform parent, RosterEntry entry, GeoLevelController level,
            GeoPhoenixFaction phoenix, FacilitySlotPools pools)
        {
            GameObject strip = CreateUIObject("Actions", parent);
            var layout = strip.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.padding = new RectOffset(0, 6, 0, 0);
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            SetSize(strip, 0f, RosterRowHeight);

            PersonnelInfo person = entry.Personnel;
            if (person == null)
            {
                return strip;
            }

            bool canWork = PersonnelRestrictions.CanBeAssignedToManufacturingOrResearch(entry.Character);
            int researchFree = pools.Research.ProvidedSlots - ResearchAndManufacturing.GetOccupiedSlots(phoenix, PersonnelAssignment.Research);
            int manufacturingFree = pools.Manufacturing.ProvidedSlots - ResearchAndManufacturing.GetOccupiedSlots(phoenix, PersonnelAssignment.Manufacturing);

            CreateIconButton(strip.transform, "ToResearch", GetColumnIconSprite(PersonnelAssignment.Research),
                () => ApplyRowAssignment(person, PersonnelAssignment.Research, level, phoenix),
                size: RosterActionSize,
                enabled: canWork && researchFree > 0);

            CreateIconButton(strip.transform, "ToManufacturing", GetColumnIconSprite(PersonnelAssignment.Manufacturing),
                () => ApplyRowAssignment(person, PersonnelAssignment.Manufacturing, level, phoenix),
                size: RosterActionSize,
                enabled: canWork && manufacturingFree > 0);

            CreateIconButton(strip.transform, "ToTraining", GetColumnIconSprite(PersonnelAssignment.Training),
                () => ApplyRowAssignment(person, PersonnelAssignment.Training, level, phoenix),
                size: RosterActionSize);

            return strip;
        }

        private static void ApplyRowAssignment(PersonnelInfo person, PersonnelAssignment target,
            GeoLevelController level, GeoPhoenixFaction phoenix)
        {
            // Training opens a modal and refreshes through its own callback; refreshing here would
            // destroy that modal the moment it appeared.
            bool opensModal = target == PersonnelAssignment.Training;

            MovePersonnelToColumn(person, target, level, phoenix);

            if (!opensModal)
            {
                RefreshPanel();
            }
        }

        #endregion

        #region Dismissal

        /// <summary>
        /// Dismissing an operative is a one-way door with a price on the way back, and for a grunt it
        /// is close to permanent, so both facts are spelled out before it happens.
        /// </summary>
        private static void ConfirmDismissFromFieldDuty(GeoCharacter character, GeoLevelController level,
            GeoPhoenixFaction phoenix)
        {
            if (character == null || phoenix == null)
            {
                return;
            }

            int redeployCost = PersonnelRestrictions.GetRedeployCost(character);
            bool isGrunt = !PersonnelRestrictions.CanBeAssignedToManufacturingOrResearch(character);
            int currentLevel = character.LevelProgression?.Level ?? 1;
            int maxTrainingLevel = TrainingFacilityRework.GetMaxTargetLevel(phoenix, character);
            int spPerLevel = TrainingFacilityRework.GetTrainingSpCost(2);

            string message = PersonnelText.Format(PersonnelText.DismissPrompt, character.DisplayName)
                + "\n\n"
                + PersonnelText.Format(PersonnelText.DismissRedeployCost, redeployCost);

            string trainingLine = currentLevel >= maxTrainingLevel
                ? PersonnelText.Get(PersonnelText.DismissTrainingCapped)
                : PersonnelText.Format(PersonnelText.DismissTrainingAvailable, maxTrainingLevel, spPerLevel);

            message += "\n\n" + (isGrunt
                ? PersonnelText.Format(PersonnelText.DismissGrunt, character.DisplayName, trainingLine)
                : PersonnelText.Format(PersonnelText.DismissCivilian, trainingLine));

            string name = character.DisplayName;
            List<GeoItem> returnedToStorage = PersonnelDismissal.GetLoadoutReturnedToStorage(character);

            ShowConfirmation(message,
                details => CreateCharacterSummary(details, character, returnedToStorage),
                () =>
                {
                    bool dismissed = DismissFromFieldDuty(character, phoenix);

                    CloseModal();
                    RefreshPanel();

                    if (!dismissed)
                    {
                        // Refreshing the panel destroys the modal it owns, so the report comes after.
                        ShowMessage(PersonnelText.Format(PersonnelText.DismissFailed, name));
                    }
                },
                () => CloseModal());
        }

        private static bool DismissFromFieldDuty(GeoCharacter character, GeoPhoenixFaction phoenix)
        {
            try
            {
                // The dismissal patch in PersonnelDismissal intercepts this and converts the operative
                // into base personnel rather than letting the character be killed off.
                phoenix.KillCharacter(character, CharacterDeathReason.Dismissed);
                RefreshResourceInfo(phoenix);

                bool converted = PersonnelRestrictions.IsDismissedOperative(character);
                TFTVLogger.Always(converted
                    ? $"{LogPrefix} Dismissed {character.DisplayName} from field duty."
                    : $"{LogPrefix} Dismissal of {character.DisplayName} did not convert them to personnel.");

                return converted;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return false;
            }
        }

        #endregion
    }

    /// <summary>
    /// Opens and closes one roster row's action strip, in the row itself rather than by rebuilding
    /// the screen: the strip takes the place of the status and affinity cells, only one row's strip
    /// is open at a time, and the strip closes again as soon as the pointer leaves the row - the
    /// same way the base construction menus behave.
    /// </summary>
    internal sealed class RosterRowActions : MonoBehaviour, IPointerExitHandler
    {
        internal const string ExpandCaption = "+";
        private const string CollapseCaption = "-";

        private static RosterRowActions _open;

        private GameObject _status;
        private GameObject _affinity;
        private Button _toggle;
        private Text _toggleCaption;
        private Image _toggleBackground;
        private Func<GameObject> _buildStrip;
        private GameObject _strip;

        /// <summary>Forgets the open row, for when the screen the rows lived on has gone away.</summary>
        internal static void Forget()
        {
            _open = null;
        }

        /// <summary>Closes whichever row is open, leaving the rows themselves standing.</summary>
        internal static void CloseOpen()
        {
            if (_open != null)
            {
                _open.Collapse();
            }
        }

        internal void Initialize(GameObject status, GameObject affinity, Button toggle, Func<GameObject> buildStrip)
        {
            _status = status;
            _affinity = affinity;
            _toggle = toggle;
            _buildStrip = buildStrip;
            _toggleBackground = toggle != null ? toggle.targetGraphic as Image : null;
            _toggleCaption = toggle != null ? toggle.GetComponentInChildren<Text>(true) : null;
        }

        internal void Toggle()
        {
            if (_strip != null)
            {
                Collapse();
                return;
            }

            Expand();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Children of the row - the strip's own buttons included - do not count as leaving it,
            // so this only fires once the pointer is genuinely off the row.
            Collapse();
        }

        private void OnDisable()
        {
            if (_open == this)
            {
                _open = null;
            }
        }

        private void Expand()
        {
            try
            {
                if (_buildStrip == null || _toggle == null)
                {
                    return;
                }

                if (_open != null && _open != this)
                {
                    _open.Collapse();
                }

                SetCellsVisible(false);

                _strip = _buildStrip();
                if (_strip != null)
                {
                    _strip.transform.SetSiblingIndex(_toggle.transform.GetSiblingIndex());
                }

                SetToggleOpen(true);
                _open = this;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private void Collapse()
        {
            try
            {
                if (_strip == null)
                {
                    return;
                }

                // Destroy() runs at the end of the frame, and until then the strip would still be
                // laid out beside the cells coming back; deactivating takes it out of the row now.
                _strip.SetActive(false);
                Destroy(_strip);
                _strip = null;

                SetCellsVisible(true);
                SetToggleOpen(false);

                if (_open == this)
                {
                    _open = null;
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private void SetCellsVisible(bool visible)
        {
            if (_status != null)
            {
                _status.SetActive(visible);
            }

            if (_affinity != null)
            {
                _affinity.SetActive(visible);
            }
        }

        private void SetToggleOpen(bool open)
        {
            if (_toggleCaption != null)
            {
                _toggleCaption.text = open ? CollapseCaption : ExpandCaption;
                _toggleCaption.color = open ? Color.black : PersonnelManagementUI.TextPrimaryColor;
            }

            if (_toggleBackground != null)
            {
                _toggleBackground.color = open
                    ? PersonnelManagementUI.AccentOrangeColor
                    : PersonnelManagementUI.ButtonFillColor;
            }
        }
    }
}
