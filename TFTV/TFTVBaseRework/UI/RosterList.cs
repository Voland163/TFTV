using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static TFTV.TFTVBaseRework.PersonnelData;
using static TFTV.TFTVBaseRework.Workers;
using Object = UnityEngine.Object;

namespace TFTV.TFTVBaseRework
{
    /// <summary>
    /// The roster column: every person Phoenix has, filtered by whether they are working, with the
    /// per-row action strip that assigns them. Field operatives appear here too, so they can be
    /// dismissed from field duty into the base workforce.
    /// </summary>
    public static partial class PersonnelManagementUI
    {
        #region State

        internal enum RosterFilter
        {
            All,
            Assigned,
            Unassigned
        }

        private static RosterFilter _rosterFilter = RosterFilter.All;

        /// <summary>Character id of the row whose action strip is open; 0 when none is.</summary>
        private static int _expandedCharacterId;

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

            public string Name => Character?.DisplayName ?? "Unknown";

            public PersonnelAssignment Assignment =>
                Personnel?.Assignment ?? PersonnelAssignment.Unassigned;

            public bool IsWorkingOrTraining =>
                Personnel != null && Personnel.Assignment != PersonnelAssignment.Unassigned;

            public bool IsAssigned => IsFieldOperative || IsWorkingOrTraining;
        }

        #endregion

        #region Column

        internal static void CreateRosterColumn(Transform parent, GeoLevelController level, GeoPhoenixFaction phoenix,
            SoldierSlotController slotPrefab)
        {
            GameObject panel = CreateFramedPanel(parent, "RosterPanel", out Transform content);
            LayoutElement panelElement = panel.GetComponent<LayoutElement>() ?? panel.AddComponent<LayoutElement>();
            panelElement.flexibleWidth = 30f;
            panelElement.flexibleHeight = 1f;

            List<RosterEntry> entries = BuildRosterEntries(phoenix);
            List<RosterEntry> visible = FilterEntries(entries, _rosterFilter);

            Transform header = CreateSectionHeader(content, "PHOENIX PERSONNEL",
                GetColumnIconSprite(PersonnelAssignment.Unassigned), TextPrimaryColor);
            Text count = CreateLabel(header, "Count", visible.Count.ToString(), TitleFontSize, AccentOrangeColor,
                TextAnchor.MiddleRight);
            SetSize(count.gameObject, 90f, 0f);

            CreateFilterTabs(content, entries);

            CreateScrollList(content, "RosterList", out Transform list);

            FacilitySlotPools pools = ResearchManufacturingSlotsManager.RecalculateSlots(phoenix);

            int index = 0;
            foreach (RosterEntry entry in visible)
            {
                CreateRosterRow(list, entry, index++, level, phoenix, slotPrefab, pools);
            }

            if (visible.Count == 0)
            {
                Text empty = CreateLabel(list, "Empty", "Nobody here.", BodyFontSize, TextDimColor, TextAnchor.MiddleCenter);
                SetSize(empty.gameObject, 0f, RosterRowHeight);
            }

            CreateRosterOptions(content, level, phoenix);
        }

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

            CreateFilterTab(row.transform, RosterFilter.All, "ALL", entries.Count);
            CreateFilterTab(row.transform, RosterFilter.Assigned, "ASSIGNED", entries.Count(e => e.IsAssigned));
            CreateFilterTab(row.transform, RosterFilter.Unassigned, "UNASSIGNED", entries.Count(e => !e.IsAssigned));
        }

        private static void CreateFilterTab(Transform parent, RosterFilter filter, string caption, int count)
        {
            bool active = _rosterFilter == filter;
            Button button = CreateTextButton(parent, $"Tab_{filter}", $"{caption} ({count})", () =>
            {
                _rosterFilter = filter;
                _expandedCharacterId = 0;
                RefreshPanel();
            },
            height: 44f,
            fontSize: SmallFontSize,
            fillColor: active ? AccentOrangeColor : ButtonFillColor,
            captionColor: active ? Color.black : TextDimColor);

            LayoutElement element = button.gameObject.GetComponent<LayoutElement>();
            element.flexibleWidth = 1f;
        }

        private static void CreateRosterOptions(Transform parent, GeoLevelController level, GeoPhoenixFaction phoenix)
        {
            EnsureAutoAssignSettingInitialized(level);

            CreateCheckbox(parent, "AutoAssignToggle", "Auto-assign", AutoAssignEnabled, () =>
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

            foreach (PersonnelInfo person in Assignments.Values)
            {
                if (person?.Character == null || person.Character.Faction != phoenix)
                {
                    continue;
                }

                entries.Add(new RosterEntry { Character = person.Character, Personnel = person });
            }

            // GeoPhoenixFaction.Soldiers is already filtered of the operatives this rework hides, so
            // what is left is genuinely on field duty.
            foreach (GeoCharacter soldier in phoenix.Soldiers)
            {
                if (soldier?.TemplateDef == null || !soldier.TemplateDef.IsHuman)
                {
                    continue;
                }

                if (entries.Any(e => e.Character == soldier))
                {
                    continue;
                }

                entries.Add(new RosterEntry { Character = soldier, IsFieldOperative = true });
            }

            return entries.OrderBy(e => e.Name).ToList();
        }

        private static List<RosterEntry> FilterEntries(List<RosterEntry> entries, RosterFilter filter)
        {
            switch (filter)
            {
                case RosterFilter.Assigned:
                    return entries.Where(e => e.IsAssigned).ToList();
                case RosterFilter.Unassigned:
                    return entries.Where(e => !e.IsAssigned).ToList();
                default:
                    return entries;
            }
        }

        #endregion

        #region Rows

        private static void CreateRosterRow(Transform parent, RosterEntry entry, int index, GeoLevelController level,
            GeoPhoenixFaction phoenix, SoldierSlotController slotPrefab, FacilitySlotPools pools)
        {
            if (entry?.Character == null)
            {
                return;
            }

            bool expanded = _expandedCharacterId == entry.Character.Id;

            GameObject row = CreateUIObject($"RosterRow_{entry.Character.Id}", parent);
            var rowImage = row.AddComponent<Image>();
            rowImage.color = expanded
                ? RowExpandedColor
                : (index % 2 == 0 ? RowFillColor : RowFillAltColor);

            var rowLayout = row.AddComponent<VerticalLayoutGroup>();
            rowLayout.spacing = 2f;
            rowLayout.padding = new RectOffset(2, 2, 2, 2);
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = false;

            GameObject line = CreateUIObject("Line", row.transform);
            var lineLayout = line.AddComponent<HorizontalLayoutGroup>();
            lineLayout.spacing = 6f;
            lineLayout.childAlignment = TextAnchor.MiddleLeft;
            lineLayout.childControlWidth = true;
            lineLayout.childControlHeight = true;
            lineLayout.childForceExpandWidth = false;
            lineLayout.childForceExpandHeight = false;
            SetSize(line, 0f, RosterRowHeight);

            CreateNameCell(line.transform, entry, slotPrefab);

            Text status = CreateLabel(line.transform, "Status", GetRosterStatusText(entry, level), BodyFontSize,
                GetRosterStatusColor(entry), TextAnchor.MiddleRight);
            SetSize(status.gameObject, 260f, RosterRowHeight);

            // The badge centres itself on its parent, so it gets a cell of its own rather than being
            // laid out as a sibling of the name and status.
            GameObject affinityCell = CreateUIObject("AffinityCell", line.transform);
            SetSize(affinityCell, RosterBadgeSize + 6f, RosterRowHeight);
            AddAffinityBadge(affinityCell, entry.Character, RosterBadgeSize);

            CreateIconButton(line.transform, "Expand", null, () =>
            {
                _expandedCharacterId = expanded ? 0 : entry.Character.Id;
                RefreshPanel();
            },
            size: RosterActionSize,
            fillColor: expanded ? AccentOrangeColor : ButtonFillColor,
            iconColor: expanded ? Color.black : TextPrimaryColor,
            fallbackCaption: expanded ? "-" : "+");

            if (expanded)
            {
                CreateRowActionStrip(row.transform, entry, level, phoenix, pools);
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

                bool showsClass = entry.IsFieldOperative || PersonnelRestrictions.IsDismissedOperative(entry.Character);
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

            Text fallback = CreateLabel(parent, "Name", entry.Name, BodyFontSize, TextPrimaryColor);
            LayoutElement element = SetSize(fallback.gameObject, 0f, RosterRowHeight);
            element.flexibleWidth = 1f;
        }

        private static string GetRosterStatusText(RosterEntry entry, GeoLevelController level)
        {
            if (entry.IsFieldOperative)
            {
                return "Field duty";
            }

            switch (entry.Assignment)
            {
                case PersonnelAssignment.Research:
                    return "Research";
                case PersonnelAssignment.Manufacturing:
                    return "Fabrication";
                case PersonnelAssignment.Training:
                    return GetAssignmentDisplay(entry.Personnel, level);
                default:
                    return PersonnelRestrictions.IsDismissedOperative(entry.Character) ? "Dismissed" : "Idle";
            }
        }

        private static Color GetRosterStatusColor(RosterEntry entry)
        {
            if (entry.IsFieldOperative)
            {
                return AccentOrangeColor;
            }

            switch (entry.Assignment)
            {
                case PersonnelAssignment.Training:
                    return AccentCyanColor;
                case PersonnelAssignment.Research:
                case PersonnelAssignment.Manufacturing:
                    return TextPrimaryColor;
                default:
                    return TextDimColor;
            }
        }

        #endregion

        #region Row actions

        private static void CreateRowActionStrip(Transform parent, RosterEntry entry, GeoLevelController level,
            GeoPhoenixFaction phoenix, FacilitySlotPools pools)
        {
            GameObject strip = CreateUIObject("Actions", parent);
            var layout = strip.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.padding = new RectOffset(6, 6, 2, 4);
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            SetSize(strip, 0f, RosterActionSize + 8f);

            if (entry.IsFieldOperative)
            {
                CreateTextButton(strip.transform, "Dismiss", "DISMISS FROM FIELD DUTY",
                    () => ConfirmDismissFromFieldDuty(entry.Character, level, phoenix),
                    width: 430f, height: RosterActionSize, fontSize: SmallFontSize,
                    fillColor: ButtonFillDangerColor);
                return;
            }

            PersonnelInfo person = entry.Personnel;
            if (person == null)
            {
                return;
            }

            if (entry.Assignment == PersonnelAssignment.Training)
            {
                CreateTextButton(strip.transform, "Deploy", "DEPLOY", () => ShowSlotContextMenu(person),
                    width: 220f, height: RosterActionSize, fontSize: SmallFontSize, fillColor: AccentOrangeColor,
                    captionColor: Color.black);
                return;
            }

            bool canWork = PersonnelRestrictions.CanBeAssignedToManufacturingOrResearch(entry.Character);
            int researchFree = pools.Research.ProvidedSlots - ResearchAndManufacturing.GetOccupiedSlots(phoenix, PersonnelAssignment.Research);
            int manufacturingFree = pools.Manufacturing.ProvidedSlots - ResearchAndManufacturing.GetOccupiedSlots(phoenix, PersonnelAssignment.Manufacturing);

            CreateIconButton(strip.transform, "ToResearch", GetColumnIconSprite(PersonnelAssignment.Research),
                () => ApplyRowAssignment(person, PersonnelAssignment.Research, level, phoenix),
                size: RosterActionSize,
                enabled: canWork && entry.Assignment != PersonnelAssignment.Research && researchFree > 0);

            CreateIconButton(strip.transform, "ToManufacturing", GetColumnIconSprite(PersonnelAssignment.Manufacturing),
                () => ApplyRowAssignment(person, PersonnelAssignment.Manufacturing, level, phoenix),
                size: RosterActionSize,
                enabled: canWork && entry.Assignment != PersonnelAssignment.Manufacturing && manufacturingFree > 0);

            CreateIconButton(strip.transform, "ToTraining", GetColumnIconSprite(PersonnelAssignment.Training),
                () => ApplyRowAssignment(person, PersonnelAssignment.Training, level, phoenix),
                size: RosterActionSize);

            CreateIconButton(strip.transform, "Unassign", null,
                () => ApplyRowAssignment(person, PersonnelAssignment.Unassigned, level, phoenix),
                size: RosterActionSize,
                enabled: entry.IsWorkingOrTraining,
                fallbackCaption: "X");
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
            int maxTrainingLevel = TrainingFacilityRework.GetMaxTargetLevel(phoenix, character);

            string message = $"Dismiss {character.DisplayName} from field duty?\n\n"
                + $"Redeploying them to a base later costs {redeployCost} shared skill points.";

            message += isGrunt
                ? $"\n\n{character.DisplayName} is rank and file: they cannot be assigned to research or "
                  + "manufacturing, and cannot be used to activate an outpost or a base. They can only be "
                  + $"trained further in their class, up to level {maxTrainingLevel}."
                : "\n\nThey join base personnel and can be assigned to research or manufacturing.";

            string name = character.DisplayName;

            ShowConfirmation(message,
                () =>
                {
                    bool dismissed = DismissFromFieldDuty(character, phoenix);

                    CloseModal();
                    RefreshPanel();

                    if (!dismissed)
                    {
                        // Refreshing the panel destroys the modal it owns, so the report comes after.
                        ShowMessage($"{name} could not be dismissed from field duty. They are still on the roster; see TFTV.log for the reason.");
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
}
