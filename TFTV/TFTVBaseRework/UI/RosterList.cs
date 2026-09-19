using Base.Audio;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using System;
using System.Collections.Generic;
using System.Linq;
using TFTV.TFTVIncidents;
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
    /// field duty, who are here so they can be dismissed back into that pool or sent to train.
    /// Anyone researching, fabricating or training is listed by the panel that owns them instead.
    /// </summary>
    public static partial class PersonnelManagementUI
    {
        #region State

        internal enum RosterFilter
        {
            Unassigned,
            FieldOperatives,
            All
        }

        internal enum RosterSort
        {
            Affinity,
            Class,
            Level
        }

        private static RosterFilter _rosterFilter = RosterFilter.Unassigned;
        private static RosterSort _rosterSort = RosterSort.Level;

        internal const string RosterPanelName = "RosterPanel";

        private const float RosterRowHeight = 68f;
        private const float RosterActionSize = 64f;
        private const float RosterBadgeSize = 46f;

        /// <summary>The vanilla slot's class icon is drawn for a much larger row than this one.</summary>
        private const float ClassIconScale = 0.6f;

        private sealed class RosterEntry
        {
            public GeoCharacter Character;
            public PersonnelInfo Personnel;
            public bool IsFieldOperative;

            public string Name => Character?.DisplayName ?? PersonnelText.Get(PersonnelText.StatusUnknownName);

            /// <summary>An operative who was taken off field duty: they keep their class and level.</summary>
            public bool IsDismissed => PersonnelRestrictions.IsDismissedOperative(Character);

            /// <summary>Civilians have no class or level to speak of until they are trained.</summary>
            public bool HasClass => IsFieldOperative || IsDismissed;

            // Sort keys, read once per refresh rather than once per comparison.
            public string SortName;
            public string SortClass;
            public int SortLevel;
            public int SortAffinity;
            public int SortAffinityRank;
        }

        private sealed class RosterRowView : RowView
        {
            internal bool IsFieldOperative;
            internal bool IsDismissed;

            internal Button Research;
            internal Button Manufacturing;
            internal Button Training;
            internal Button Deploy;
            internal Button Dismiss;

            // The hover text of the buttons that can be unavailable for a reason worth giving.
            internal PersonnelTooltipTrigger TrainingTooltip;
            internal PersonnelTooltipTrigger DeployTooltip;
        }

        private sealed class RosterTabView
        {
            public RosterFilter Filter;
            public ButtonLook Look;
            public int Count;
        }

        private sealed class RosterSortView
        {
            public RosterSort Sort;
            public ButtonLook Look;
        }

        /// <summary>Which duties have room this refresh, so each row's buttons can say so.</summary>
        private struct RosterAvailability
        {
            public GeoPhoenixFaction Phoenix;
            public bool ResearchFree;
            public bool ManufacturingFree;
            public bool TrainingFree;
        }

        private static readonly KeyedRows<RosterRowView> _rosterRows = new KeyedRows<RosterRowView>();
        private static readonly List<RosterTabView> _rosterTabViews = new List<RosterTabView>();
        private static readonly List<RosterSortView> _rosterSortViews = new List<RosterSortView>();
        private static Text _rosterCountLabel;
        private static CheckboxLook _autoAssignLook;

        #endregion

        #region Column

        /// <summary>
        /// Builds the parts of the column that do not change - header, tabs, sort buttons, the list
        /// and the auto-assign option. The rows are filled in by <see cref="SyncRoster"/>.
        /// </summary>
        internal static GameObject CreateRosterColumn(Transform parent, GeoLevelController level, GeoPhoenixFaction phoenix)
        {
            ResetRosterView();

            GameObject panel = CreateFramedPanel(parent, RosterPanelName, out Transform content);
            LayoutElement panelElement = panel.GetComponent<LayoutElement>() ?? panel.AddComponent<LayoutElement>();
            panelElement.flexibleWidth = 30f;
            panelElement.flexibleHeight = 1f;

            Transform header = CreateSectionHeader(content, PersonnelText.Get(PersonnelText.RosterTitle),
                GetColumnIconSprite(PersonnelAssignment.Unassigned), TextPrimaryColor);
            _rosterCountLabel = CreateLabel(header, "Count", string.Empty, TitleFontSize, AccentOrangeColor,
                TextAnchor.MiddleRight);
            SetSize(_rosterCountLabel.gameObject, 90f, 0f);

            CreateFilterTabs(content);
            CreateSortButtons(content);

            CreateScrollList(content, "RosterList", out Transform list);
            _rosterRows.Content = list;

            Text empty = CreateLabel(list, "Empty", PersonnelText.Get(PersonnelText.RosterEmpty), BodyFontSize,
                TextDimColor, TextAnchor.MiddleCenter);
            SetSize(empty.gameObject, 0f, RosterRowHeight);
            _rosterRows.Empty = empty.gameObject;

            CreateRosterOptions(content, level, phoenix);

            return panel;
        }

        /// <summary>Drops what the roster column left behind, once it has been destroyed.</summary>
        internal static void ResetRosterView()
        {
            _rosterRows.Views.Clear();
            _rosterRows.Ordered.Clear();
            _rosterRows.Content = null;
            _rosterRows.Empty = null;
            _rosterTabViews.Clear();
            _rosterSortViews.Clear();
            _rosterCountLabel = null;
            _autoAssignLook = null;
        }

        /// <summary>
        /// Brings the rows into line with the records: people who have been assigned elsewhere lose
        /// their row, people who have become free gain one, and everyone else is re-sorted and has
        /// their buttons re-enabled or disabled for the slots that are now free. Only the rows that
        /// actually came or went are built or destroyed.
        /// </summary>
        private static void SyncRoster(GeoLevelController level, GeoPhoenixFaction phoenix,
            SoldierSlotController slotPrefab, FacilitySlotPools pools)
        {
            if (_rosterRows.Content == null)
            {
                return;
            }

            List<RosterEntry> entries = BuildRosterEntries(phoenix);
            SortRosterEntries(entries);

            var availability = new RosterAvailability
            {
                Phoenix = phoenix,
                ResearchFree = pools.Research.ProvidedSlots
                    - ResearchAndManufacturing.GetOccupiedSlots(phoenix, PersonnelAssignment.Research) > 0,
                ManufacturingFree = pools.Manufacturing.ProvidedSlots
                    - ResearchAndManufacturing.GetOccupiedSlots(phoenix, PersonnelAssignment.Manufacturing) > 0,
                TrainingFree = TrainingFacilityRework.GetUsedTrainingSlots()
                    < TrainingFacilityRework.GetProvidedTrainingSlots(phoenix)
            };

            // A dismissed operative keeps their character id but becomes a different kind of row, so
            // the kind is part of the key and the old row is replaced rather than reused.
            SyncRows(_rosterRows, entries,
                entry => entry.Character.Id * 2 + (entry.IsFieldOperative ? 1 : 0),
                entry => CreateRosterRow(_rosterRows.Content, entry, level, phoenix, slotPrefab),
                (entry, view) => UpdateRosterRow(entry, view, availability));

            foreach (RosterTabView tab in _rosterTabViews)
            {
                tab.Count = entries.Count(entry => MatchesFilter(entry.IsFieldOperative, tab.Filter));
            }

            if (_autoAssignLook != null)
            {
                _autoAssignLook.SetValue(AutoAssignEnabled);
            }

            ApplyRosterFilter();
        }

        /// <summary>
        /// UNASSIGNED is the pool you assign from, FIELD OPERATIVES the people you can take off
        /// field duty, and ALL both at once.
        /// </summary>
        private static void CreateFilterTabs(Transform parent)
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

            CreateFilterTab(row.transform, RosterFilter.Unassigned, PersonnelText.FilterUnassigned);
            CreateFilterTab(row.transform, RosterFilter.FieldOperatives, PersonnelText.FilterFieldOperatives);
            CreateFilterTab(row.transform, RosterFilter.All, PersonnelText.FilterAll);
        }

        private static void CreateFilterTab(Transform parent, RosterFilter filter, string captionKey)
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

            _rosterTabViews.Add(new RosterTabView
            {
                Filter = filter,
                Look = button.GetComponent<ButtonLook>()
            });
        }

        private static void CreateSortButtons(Transform parent)
        {
            GameObject row = CreateUIObject("SortButtons", parent);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 4f;
            layout.padding = new RectOffset(4, 4, 2, 2);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            SetSize(row, 0f, 44f);

            Text caption = CreateLabel(row.transform, "Caption", PersonnelText.Get(PersonnelText.SortLabel), SmallFontSize,
                TextDimColor, TextAnchor.MiddleLeft);
            SetSize(caption.gameObject, 130f, 40f);

            CreateSortButton(row.transform, RosterSort.Affinity, PersonnelText.SortAffinity);
            CreateSortButton(row.transform, RosterSort.Class, PersonnelText.SortClass);
            CreateSortButton(row.transform, RosterSort.Level, PersonnelText.SortLevel);
        }

        private static void CreateSortButton(Transform parent, RosterSort sort, string captionKey)
        {
            Button button = CreateTextButton(parent, $"Sort_{sort}", PersonnelText.Get(captionKey), () =>
            {
                _rosterSort = sort;
                // Nobody has come or gone, so this only reorders the rows that are already built.
                SyncPanel(rosterOnly: true);
            },
            height: 40f,
            fontSize: SmallFontSize);

            LayoutElement element = button.gameObject.GetComponent<LayoutElement>();
            element.flexibleWidth = 1f;

            _rosterSortViews.Add(new RosterSortView
            {
                Sort = sort,
                Look = button.GetComponent<ButtonLook>()
            });
        }

        private static void CreateRosterOptions(Transform parent, GeoLevelController level, GeoPhoenixFaction phoenix)
        {
            EnsureAutoAssignSettingInitialized(level);

            Button checkbox = CreateCheckbox(parent, "AutoAssignToggle", PersonnelText.Get(PersonnelText.AutoAssign),
                AutoAssignEnabled, () => RunPanelAction(() =>
                {
                    SetAutoAssignEnabled(level, !AutoAssignEnabled);
                    if (AutoAssignEnabled)
                    {
                        TryAutoAssignUnassignedPersonnel(phoenix, "AutoAssignToggle");
                    }
                }), height: 58f, fontSize: TitleFontSize);

            _autoAssignLook = checkbox.GetComponent<CheckboxLook>();
        }

        /// <summary>
        /// Shows the rows the current tab asks for and hides the rest, restriping what is left so
        /// the banding still alternates, and redraws the tabs, sort buttons and count to match.
        /// </summary>
        private static void ApplyRosterFilter()
        {
            int visible = 0;
            foreach (RosterRowView view in _rosterRows.Ordered)
            {
                if (view?.Row == null)
                {
                    continue;
                }

                bool show = MatchesFilter(view.IsFieldOperative, _rosterFilter);
                if (view.Row.activeSelf != show)
                {
                    view.Row.SetActive(show);
                }

                if (show)
                {
                    visible++;
                }
            }

            Restripe(_rosterRows.Ordered);

            if (_rosterCountLabel != null)
            {
                _rosterCountLabel.text = visible.ToString();
            }

            if (_rosterRows.Empty != null)
            {
                _rosterRows.Empty.SetActive(visible == 0);
            }

            foreach (RosterTabView tab in _rosterTabViews)
            {
                bool active = tab.Filter == _rosterFilter;
                tab.Look?.SetColors(active ? AccentOrangeColor : ButtonFillColor,
                    active ? Color.black : (tab.Count > 0 ? TextDimColor : TextDisabledColor));
            }

            foreach (RosterSortView sort in _rosterSortViews)
            {
                bool active = sort.Sort == _rosterSort;
                sort.Look?.SetColors(active ? AccentOrangeColor : ButtonFillColor,
                    active ? Color.black : TextDimColor);
            }
        }

        private static bool MatchesFilter(bool isFieldOperative, RosterFilter filter)
        {
            switch (filter)
            {
                case RosterFilter.Unassigned:
                    return !isFieldOperative;
                case RosterFilter.FieldOperatives:
                    return isFieldOperative;
                default:
                    return true;
            }
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
                // control that ends the assignment.
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

            return entries;
        }

        /// <summary>
        /// Orders the roster for the chosen sort.
        ///
        /// Operatives, serving or dismissed, always come first and civilians after them: they are
        /// the people with a class and a level to compare, and the ones the player is usually
        /// looking for. Within each group the chosen sort decides, then level, highest first, so
        /// operatives without an affinity follow straight on from those with one; the name settles
        /// anything left, so the order holds still between refreshes.
        /// </summary>
        private static void SortRosterEntries(List<RosterEntry> entries)
        {
            foreach (RosterEntry entry in entries)
            {
                entry.SortName = entry.Name;

                bool hasClass = entry.HasClass;
                entry.SortLevel = hasClass ? entry.Character.LevelProgression?.Level ?? 1 : 0;
                entry.SortClass = hasClass ? GetClassName(entry.Character) : null;

                entry.SortAffinity = int.MaxValue;
                entry.SortAffinityRank = 0;

                try
                {
                    if (LeaderSelection.TryGetCurrentAffinity(entry.Character, out LeaderSelection.AffinityApproach approach, out int rank))
                    {
                        entry.SortAffinity = (int)approach;
                        entry.SortAffinityRank = rank;
                    }
                }
                catch (Exception e)
                {
                    // One unreadable affinity sorts that person last rather than emptying the list.
                    TFTVLogger.Error(e);
                }
            }

            entries.Sort((a, b) =>
            {
                // Operatives, current and past, before civilians.
                int cmp = b.HasClass.CompareTo(a.HasClass);
                if (cmp != 0)
                {
                    return cmp;
                }

                switch (_rosterSort)
                {
                    case RosterSort.Affinity:
                        cmp = a.SortAffinity.CompareTo(b.SortAffinity);
                        if (cmp == 0)
                        {
                            cmp = b.SortAffinityRank.CompareTo(a.SortAffinityRank);
                        }
                        break;

                    case RosterSort.Class:
                        cmp = string.Compare(a.SortClass, b.SortClass, StringComparison.CurrentCulture);
                        break;
                }

                if (cmp == 0)
                {
                    cmp = b.SortLevel.CompareTo(a.SortLevel);
                }

                return cmp != 0 ? cmp : string.Compare(a.SortName, b.SortName, StringComparison.CurrentCulture);
            });
        }

        private static string GetClassName(GeoCharacter character)
        {
            try
            {
                return character?.GetClassViewElementDefs()?.FirstOrDefault()?.DisplayName1?.Localize() ?? string.Empty;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return string.Empty;
            }
        }

        #endregion

        #region Rows

        /// <summary>
        /// One line per person: name, affinity, then their actions. The actions sit in four fixed
        /// columns so the training button lines up down the whole list - research, fabrication,
        /// training and deployment for the free pool; dismiss-and-train and dismiss for field
        /// operatives, with dismissal at the edge where deployment sits for everyone else.
        /// </summary>
        private static RosterRowView CreateRosterRow(Transform parent, RosterEntry entry, GeoLevelController level,
            GeoPhoenixFaction phoenix, SoldierSlotController slotPrefab)
        {
            if (entry?.Character == null)
            {
                return null;
            }

            GameObject row = CreateUIObject($"RosterRow_{entry.Character.Id}", parent);
            var view = new RosterRowView
            {
                Row = row,
                Background = row.AddComponent<Image>(),
                IsFieldOperative = entry.IsFieldOperative,
                IsDismissed = entry.IsDismissed
            };

            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 6f;
            rowLayout.padding = new RectOffset(2, 6, 2, 2);
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
            SetSize(row, 0f, RosterRowHeight);

            CreateNameCell(row.transform, entry, slotPrefab);

            // The badge centres itself on its parent, so it gets a cell of its own rather than being
            // laid out as a sibling of the name.
            GameObject affinityCell = CreateUIObject("AffinityCell", row.transform);
            SetSize(affinityCell, RosterBadgeSize + 6f, RosterRowHeight);
            AddAffinityBadge(affinityCell, entry.Character, RosterBadgeSize);

            if (entry.IsFieldOperative)
            {
                GeoCharacter character = entry.Character;

                for (int column = 0; column < 2; column++)
                {
                    GameObject spacer = CreateUIObject("Spacer", row.transform);
                    SetSize(spacer, RosterActionSize, RosterActionSize);
                }

                view.Training = CreateIconButton(row.transform, "DismissAndTrain", GetColumnIconSprite(PersonnelAssignment.Training),
                    () => ShowFieldOperativeTrainingSelection(level, character),
                    size: RosterActionSize);
                view.TrainingTooltip = AddTextTooltip(view.Training.gameObject,
                    PersonnelText.Get(PersonnelText.ActionTrainFieldOperative));

                view.Dismiss = CreateIconButton(row.transform, "Dismiss", GetDismissIconSprite(),
                    () => ConfirmDismissFromFieldDuty(character, level, phoenix),
                    size: RosterActionSize, fillColor: ButtonFillDangerColor, fallbackCaption: "X");
                AddTextTooltip(view.Dismiss.gameObject, PersonnelText.Get(PersonnelText.ActionDismiss));

                return view;
            }

            PersonnelInfo person = entry.Personnel;

            view.Research = CreateIconButton(row.transform, "ToResearch", GetColumnIconSprite(PersonnelAssignment.Research),
                () => RunPanelAction(() => MovePersonnelToColumn(person, PersonnelAssignment.Research, level, phoenix)),
                size: RosterActionSize);
            AddTextTooltip(view.Research.gameObject, PersonnelText.Get(PersonnelText.ActionResearch));

            view.Manufacturing = CreateIconButton(row.transform, "ToManufacturing", GetColumnIconSprite(PersonnelAssignment.Manufacturing),
                () => RunPanelAction(() => MovePersonnelToColumn(person, PersonnelAssignment.Manufacturing, level, phoenix)),
                size: RosterActionSize);
            AddTextTooltip(view.Manufacturing.gameObject, PersonnelText.Get(PersonnelText.ActionManufacturing));

            view.Training = CreateIconButton(row.transform, "ToTraining", GetColumnIconSprite(PersonnelAssignment.Training),
                () => StartRosterTraining(level, phoenix, person),
                size: RosterActionSize);
            view.TrainingTooltip = AddTextTooltip(view.Training.gameObject, PersonnelText.Get(PersonnelText.ActionTrain));

            // Deploying opens the base picker, which takes a civilian on to choosing a class and
            // charges a dismissed operative the redeploy fee.
            view.Deploy = CreateIconButton(row.transform, "Deploy", GetDeployIconSprite(),
                () => ShowSlotContextMenu(person),
                size: RosterActionSize, fallbackCaption: "D");
            view.DeployTooltip = AddTextTooltip(view.Deploy.gameObject, PersonnelText.Get(PersonnelText.ActionDeploy));

            return view;
        }

        private static void UpdateRosterRow(RosterEntry entry, RosterRowView view, RosterAvailability availability)
        {
            UpdateTrainingButton(entry, view, availability);

            if (view.IsFieldOperative)
            {
                return;
            }

            bool canWork = PersonnelRestrictions.CanBeAssignedToManufacturingOrResearch(entry.Character);
            SetButtonEnabled(view.Research, canWork && availability.ResearchFree);
            SetButtonEnabled(view.Manufacturing, canWork && availability.ManufacturingFree);

            // Only a dismissed operative's deployment costs anything, and it cannot go ahead short.
            int redeployCost = entry.IsDismissed ? PersonnelRestrictions.GetRedeployCost(entry.Character) : 0;
            int skillpoints = availability.Phoenix?.Skillpoints ?? 0;
            bool affordable = skillpoints >= redeployCost;

            SetButtonEnabled(view.Deploy, affordable);
            SetTooltip(view.DeployTooltip, affordable
                ? PersonnelText.Get(PersonnelText.ActionDeploy)
                : PersonnelText.Format(PersonnelText.NotEnoughSpRedeploy, entry.Name, redeployCost, skillpoints));
        }

        /// <summary>
        /// Training is greyed out when it cannot happen, and the hover says why: either the person
        /// is already as high as training can take them - which depends on research and on their
        /// affinity - or every training slot is taken. The cap is the reason given when both apply,
        /// since a free slot would not help.
        /// </summary>
        private static void UpdateTrainingButton(RosterEntry entry, RosterRowView view, RosterAvailability availability)
        {
            // Civilians train up from level 1; operatives, serving or dismissed, from where they are.
            int currentLevel = entry.HasClass ? entry.Character.LevelProgression?.Level ?? 1 : 1;
            int maxLevel = TrainingFacilityRework.GetMaxTargetLevel(availability.Phoenix, entry.Character);
            bool capped = Math.Max(2, currentLevel + 1) > maxLevel;

            string tooltip;
            if (capped)
            {
                tooltip = PersonnelText.Format(PersonnelText.TrainCapped, entry.Name, currentLevel);
            }
            else if (!availability.TrainingFree)
            {
                tooltip = PersonnelText.Get(PersonnelText.NoFacilitySlot);
            }
            else
            {
                tooltip = PersonnelText.Get(view.IsFieldOperative
                    ? PersonnelText.ActionTrainFieldOperative
                    : PersonnelText.ActionTrain);
            }

            SetButtonEnabled(view.Training, !capped && availability.TrainingFree);
            SetTooltip(view.TrainingTooltip, tooltip);
        }

        private static void SetTooltip(PersonnelTooltipTrigger trigger, string content)
        {
            if (trigger != null)
            {
                trigger.Content = content;
            }
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

                // The slot is a button in its own screen and brings that screen's sounds along; here
                // it is only a label, and hovering a list of names should not click at the player.
                foreach (UIButtonSounds sounds in slot.GetComponentsInChildren<UIButtonSounds>(true))
                {
                    sounds.enabled = false;
                }

                // Whatever still listens for pointer events on it would otherwise eat the mouse wheel.
                foreach (EventTrigger trigger in slot.GetComponentsInChildren<EventTrigger>(true))
                {
                    if (trigger.GetComponent<ScrollPassthrough>() == null)
                    {
                        trigger.gameObject.AddComponent<ScrollPassthrough>();
                    }
                }

                if (!entry.HasClass)
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

                // Serving operatives are the one group on this screen that cannot be put to work
                // directly, and the only thing that marks them out now there is no status column.
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

        #endregion

        #region Row actions

        /// <summary>
        /// Straight into training: a class and then a level for a civilian, a level alone for a
        /// dismissed operative, who keeps the class they had.
        /// </summary>
        private static void StartRosterTraining(GeoLevelController level, GeoPhoenixFaction phoenix, PersonnelInfo person)
        {
            if (person == null)
            {
                return;
            }

            if (PersonnelData.IsLivingCapacityFull(phoenix))
            {
                ShowLivingQuartersFull(PersonnelText.DutyTraining);
                return;
            }

            StartTrainingFlow(level, person);
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
                    bool dismissed = false;
                    RunPanelAction(() => dismissed = DismissFromFieldDuty(character, phoenix));

                    if (!dismissed)
                    {
                        // The refresh closes the modal it owns, so the report comes after.
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

        #region Dismiss and train

        /// <summary>
        /// Sends a serving operative to training in one go - the same as dismissing them and then
        /// assigning them to training, but decided together: the level is chosen first and a single
        /// confirmation covers both, so backing out part way never leaves them dismissed and idle.
        /// </summary>
        private static void ShowFieldOperativeTrainingSelection(GeoLevelController level, GeoCharacter character)
        {
            GeoPhoenixFaction faction = level?.PhoenixFaction;
            if (faction == null || character == null)
            {
                return;
            }

            SpecializationDef spec = ResolveExistingSpecialization(character);
            if (spec == null)
            {
                ShowMessage(PersonnelText.Format(PersonnelText.ClassUnknown, character.DisplayName));
                return;
            }

            if (TrainingFacilityRework.GetUsedTrainingSlots() >= TrainingFacilityRework.GetProvidedTrainingSlots(faction))
            {
                ShowMessage(PersonnelText.Get(PersonnelText.NoFacilitySlot));
                return;
            }

            int currentLevel = character.LevelProgression?.Level ?? 1;
            int maxLevel = TrainingFacilityRework.GetMaxTargetLevel(faction, character);
            int minTargetLevel = Math.Max(2, currentLevel + 1);

            if (minTargetLevel > maxLevel)
            {
                ShowMessage(PersonnelText.Format(PersonnelText.AlreadyMaxLevel, character.DisplayName, currentLevel));
                return;
            }

            CloseModal();
            _modalRoot = CreateModalRoot("FieldOperativeTrainingModal");
            AddModalHeader(PersonnelText.Get(PersonnelText.SelectLevel));
            Transform content = CreateModalContentArea();

            for (int targetLevel = minTargetLevel; targetLevel <= maxLevel; targetLevel++)
            {
                int levelsGained = targetLevel - currentLevel;
                int spCost = TrainingFacilityRework.GetTrainingSpCost(targetLevel);
                float duration = TrainingFacilityRework.GetEffectiveDurationHours(faction, targetLevel, currentLevel);
                string statGains = TrainingFacilityRework.GetStatGainDescription(levelsGained);
                string label = PersonnelText.Format(PersonnelText.TrainOption, targetLevel, spCost,
                    FormatDuration(duration), statGains);

                bool canAfford = faction.Skillpoints >= spCost;
                int chosenLevel = targetLevel;

                AddModalOptionButton(content,
                    canAfford ? label : PersonnelText.Format(PersonnelText.TrainOptionUnaffordable, label), () =>
                    {
                        if (!canAfford)
                        {
                            ShowMessage(PersonnelText.Format(PersonnelText.NotEnoughSp, spCost, faction.Skillpoints));
                            return;
                        }

                        ConfirmFieldOperativeTraining(level, character, spec, chosenLevel, spCost, duration,
                            statGains, levelsGained);
                    });
            }

            AddModalCloseButton();
        }

        private static void ConfirmFieldOperativeTraining(GeoLevelController level, GeoCharacter character,
            SpecializationDef spec, int targetLevel, int spCost, float duration, string statGains, int levelsGained)
        {
            GeoPhoenixFaction faction = level.PhoenixFaction;
            string name = character.DisplayName;

            // Completing training for a dismissed operative charges the redeploy fee at their
            // current level, so that is the figure the player is told now.
            string message = PersonnelText.Format(PersonnelText.TrainConfirm, name,
                    spec.ViewElementDef.DisplayName1.Localize(), targetLevel, spCost, FormatDuration(duration), statGains)
                + "\n\n"
                + PersonnelText.Format(PersonnelText.TrainFieldOperative, name,
                    PersonnelRestrictions.GetRedeployCost(character));

            List<GeoItem> returnedToStorage = PersonnelDismissal.GetLoadoutReturnedToStorage(character);
            ProjectedStats projected = BuildProjectedStats(character, levelsGained);

            ShowConfirmation(message,
                details => CreateCharacterSummary(details, character, returnedToStorage, projected,
                    showClassAndAbilities: true),
                () =>
                {
                    if (faction.Skillpoints < spCost)
                    {
                        ShowMessage(PersonnelText.Format(PersonnelText.NotEnoughSp, spCost, faction.Skillpoints));
                        return;
                    }

                    string failure = null;

                    RunPanelAction(() =>
                    {
                        if (!DismissFromFieldDuty(character, faction))
                        {
                            failure = PersonnelText.Format(PersonnelText.DismissFailed, name);
                            return;
                        }

                        // Training is queued for base personnel, which the dismissal has just made
                        // them; the record is looked up again because it did not exist until now.
                        PersonnelInfo person = PersonnelData.GetPersonnelByUnitId(character.Id);
                        if (person != null
                            && TrainingFacilityRework.QueueCharacterTrainingAutoFacility(level, character, spec, targetLevel))
                        {
                            AssignPersonnelToTraining(person, faction, spec);
                            return;
                        }

                        failure = PersonnelText.Format(PersonnelText.TrainQueueFailed, name);
                    });

                    if (failure != null)
                    {
                        TFTVLogger.Always($"{LogPrefix} Dismiss-and-train for {name} did not complete: {failure}");
                        ShowMessage(failure);
                    }
                },
                () => CloseModal());
        }

        #endregion
    }
}
