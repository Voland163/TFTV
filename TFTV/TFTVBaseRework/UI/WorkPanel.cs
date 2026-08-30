using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static TFTV.TFTVBaseRework.PersonnelData;
using static TFTV.TFTVBaseRework.Workers;

namespace TFTV.TFTVBaseRework
{
    /// <summary>
    /// The Research and Fabrication panels: what the assigned personnel are producing, the controls
    /// that seat and unseat them, and the list of who is currently working.
    /// </summary>
    public static partial class PersonnelManagementUI
    {
        private const float WorkRowHeight = 50f;

        internal static void CreateWorkPanel(Transform parent, PersonnelAssignment assignment, GeoLevelController level,
            GeoPhoenixFaction phoenix, SoldierSlotController slotPrefab, FacilitySlotPools pools)
        {
            bool isResearch = assignment == PersonnelAssignment.Research;

            string title = isResearch ? "RESEARCH" : "FABRICATION";
            Color accent = isResearch ? AccentCyanColor : AccentOrangeColor;
            FacilitySlotPool pool = isResearch ? pools.Research : pools.Manufacturing;
            int occupied = ResearchAndManufacturing.GetOccupiedSlots(phoenix, assignment);

            ResearchAndManufacturing.GetOutputBonuses(phoenix, out float researchBonus, out float productionBonus);
            float bonus = isResearch ? researchBonus : productionBonus;

            ResearchManufacturingSlotsManager.CountFacilityProviders(phoenix, out int researchFacilities, out int manufacturingFacilities);
            int facilities = isResearch ? researchFacilities : manufacturingFacilities;
            string facilityLine = isResearch
                ? $"Research Labs built: {researchFacilities}"
                : $"Fabrication Plants built: {manufacturingFacilities}";

            GameObject panel = CreateFramedPanel(parent, $"WorkPanel_{assignment}", out Transform content);
            LayoutElement panelElement = panel.GetComponent<LayoutElement>() ?? panel.AddComponent<LayoutElement>();
            panelElement.flexibleWidth = 1f;
            panelElement.flexibleHeight = 1f;

            Transform header = CreateSectionHeader(content, title, GetColumnIconSprite(assignment), accent);
            CreateTextButton(header, "UnassignAll", "UNASSIGN ALL", () => UnassignAllFrom(assignment, phoenix),
                width: 240f, height: 44f, fontSize: SmallFontSize, enabled: occupied > 0);

            GameObject body = CreateUIObject("Body", content.transform);
            var bodyLayout = body.AddComponent<HorizontalLayoutGroup>();
            bodyLayout.spacing = 10f;
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childForceExpandHeight = true;
            LayoutElement bodyElement = body.AddComponent<LayoutElement>();
            bodyElement.flexibleHeight = 1f;

            CreateWorkControls(body.transform, assignment, level, phoenix, accent, bonus, occupied, pool.ProvidedSlots, facilityLine, facilities);
            CreateWorkerList(body.transform, assignment, level, phoenix, slotPrefab);
        }

        private static void CreateWorkControls(Transform parent, PersonnelAssignment assignment, GeoLevelController level,
            GeoPhoenixFaction phoenix, Color accent, float bonus, int occupied, int provided, string facilityLine, int facilities)
        {
            GameObject column = CreateUIObject("Controls", parent);
            var layout = column.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.padding = new RectOffset(8, 8, 4, 4);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            LayoutElement columnElement = column.AddComponent<LayoutElement>();
            columnElement.flexibleWidth = 1f;

            string boostWord = assignment == PersonnelAssignment.Research ? "research" : "manufacturing";
            Text boost = CreateLabel(column.transform, "Boost", $"+{bonus:0.#} {boostWord} boost", TitleFontSize, accent,
                TextAnchor.MiddleCenter);
            SetSize(boost.gameObject, 0f, 46f);

            GameObject buttons = CreateUIObject("SlotButtons", column.transform);
            var buttonsLayout = buttons.AddComponent<HorizontalLayoutGroup>();
            buttonsLayout.spacing = 14f;
            buttonsLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonsLayout.childControlWidth = true;
            buttonsLayout.childControlHeight = true;
            buttonsLayout.childForceExpandWidth = false;
            buttonsLayout.childForceExpandHeight = false;
            SetSize(buttons, 0f, 82f);

            bool canRemove = occupied > 0;
            bool canAdd = occupied < provided && Assignments.Values.Any(p => p != null && p.Character != null
                && p.Assignment == PersonnelAssignment.Unassigned
                && PersonnelRestrictions.CanBeAssignedToManufacturingOrResearch(p.Character));

            CreateStepperButton(buttons.transform, "Remove", "-", () =>
            {
                OnMinusClicked(assignment, level, phoenix);
            }, enabled: canRemove);

            CreateStepperButton(buttons.transform, "Add", "+", () =>
            {
                OnPlusClicked(assignment, level, phoenix);
            }, enabled: canAdd);

            Text counter = CreateLabel(column.transform, "Counter", $"{occupied} / {provided}", 56,
                occupied > 0 ? AccentOrangeColor : TextPrimaryColor, TextAnchor.MiddleCenter);
            SetSize(counter.gameObject, 0f, 68f);

            Text facilityLabel = CreateLabel(column.transform, "Facilities", facilityLine, SmallFontSize,
                facilities > 0 ? TextDimColor : TextDisabledColor, TextAnchor.MiddleCenter);
            SetSize(facilityLabel.gameObject, 0f, 32f);
        }

        private static void CreateWorkerList(Transform parent, PersonnelAssignment assignment, GeoLevelController level,
            GeoPhoenixFaction phoenix, SoldierSlotController slotPrefab)
        {
            GameObject column = CreateUIObject("Workers", parent);
            var layout = column.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 2f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            LayoutElement columnElement = column.AddComponent<LayoutElement>();
            columnElement.flexibleWidth = 1.2f;

            CreateScrollList(column.transform, $"WorkerList_{assignment}", out Transform list);

            List<PersonnelInfo> workers = Assignments.Values
                .Where(p => p != null && p.Character != null && p.Character.Faction == phoenix && p.Assignment == assignment)
                .OrderBy(p => GetPersonnelName(p))
                .ToList();

            if (workers.Count == 0)
            {
                Text empty = CreateLabel(list, "Empty", "No one assigned.", SmallFontSize, TextDimColor, TextAnchor.MiddleCenter);
                SetSize(empty.gameObject, 0f, WorkRowHeight);
                return;
            }

            int index = 0;
            foreach (PersonnelInfo person in workers)
            {
                CreateWorkerRow(list, person, index++, phoenix, slotPrefab);
            }
        }

        private static void CreateWorkerRow(Transform parent, PersonnelInfo person, int index, GeoPhoenixFaction phoenix,
            SoldierSlotController slotPrefab)
        {
            GameObject row = CreateUIObject($"Worker_{person.Id}", parent);
            row.AddComponent<Image>().color = index % 2 == 0 ? RowFillColor : RowFillAltColor;

            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.padding = new RectOffset(4, 4, 2, 2);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            SetSize(row, 0f, WorkRowHeight);

            var entry = new RosterEntry { Character = person.Character, Personnel = person };
            CreateNameCell(row.transform, entry, slotPrefab);

            GameObject affinityCell = CreateUIObject("AffinityCell", row.transform);
            SetSize(affinityCell, 40f, WorkRowHeight);
            AddAffinityBadge(affinityCell, person.Character);

            CreateIconButton(row.transform, "Unassign", null, () =>
            {
                UnassignFromWork(person, phoenix);
                RefreshPanel();
            }, size: 40f, fallbackCaption: "X");
        }

        private static void UnassignAllFrom(PersonnelAssignment assignment, GeoPhoenixFaction phoenix)
        {
            List<PersonnelInfo> workers = Assignments.Values
                .Where(p => p != null && p.Character != null && p.Character.Faction == phoenix && p.Assignment == assignment)
                .ToList();

            foreach (PersonnelInfo person in workers)
            {
                UnassignFromWork(person, phoenix);
            }

            TFTVLogger.Always($"{LogPrefix} Unassigned all {workers.Count} personnel from {assignment}.");
            RefreshPanel();
        }
    }
}
