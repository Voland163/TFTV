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
        private const float WorkRowHeight = 58f;

        /// <summary>Everything in a work panel that changes when someone is seated or unseated.</summary>
        private sealed class WorkPanelView
        {
            internal PersonnelAssignment Assignment;
            internal Text Boost;
            internal Text Counter;
            internal Text Facilities;
            internal Button Remove;
            internal Button Add;
            internal Button UnassignAll;
            internal readonly KeyedRows<RowView> Workers = new KeyedRows<RowView>();
        }

        private static WorkPanelView _researchPanel;
        private static WorkPanelView _manufacturingPanel;

        private static WorkPanelView CreateWorkPanel(Transform parent, PersonnelAssignment assignment,
            GeoLevelController level, GeoPhoenixFaction phoenix)
        {
            bool isResearch = assignment == PersonnelAssignment.Research;
            var view = new WorkPanelView { Assignment = assignment };

            string title = PersonnelText.Get(isResearch ? PersonnelText.ResearchTitle : PersonnelText.FabricationTitle);
            Color accent = isResearch ? AccentCyanColor : AccentOrangeColor;

            GameObject panel = CreateFramedPanel(parent, $"WorkPanel_{assignment}", out Transform content);
            LayoutElement panelElement = panel.GetComponent<LayoutElement>() ?? panel.AddComponent<LayoutElement>();
            panelElement.flexibleWidth = 1f;
            panelElement.flexibleHeight = 1f;

            Transform header = CreateSectionHeader(content, title, GetColumnIconSprite(assignment), accent);
            view.UnassignAll = CreateTextButton(header, "UnassignAll", PersonnelText.Get(PersonnelText.UnassignAll),
                () => RunPanelAction(() => UnassignAllFrom(assignment, phoenix)),
                width: 240f, height: 44f, fontSize: SmallFontSize);

            GameObject body = CreateUIObject("Body", content.transform);
            var bodyLayout = body.AddComponent<HorizontalLayoutGroup>();
            bodyLayout.spacing = 10f;
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childForceExpandHeight = true;
            LayoutElement bodyElement = body.AddComponent<LayoutElement>();
            bodyElement.flexibleHeight = 1f;

            CreateWorkControls(body.transform, view, level, phoenix, accent);
            CreateWorkerList(body.transform, view);

            return view;
        }

        private static void CreateWorkControls(Transform parent, WorkPanelView view, GeoLevelController level,
            GeoPhoenixFaction phoenix, Color accent)
        {
            PersonnelAssignment assignment = view.Assignment;

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

            view.Boost = CreateLabel(column.transform, "Boost", string.Empty, 46, accent, TextAnchor.MiddleCenter);
            SetSize(view.Boost.gameObject, 0f, 62f);

            GameObject buttons = CreateUIObject("SlotButtons", column.transform);
            var buttonsLayout = buttons.AddComponent<HorizontalLayoutGroup>();
            buttonsLayout.spacing = 14f;
            buttonsLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonsLayout.childControlWidth = true;
            buttonsLayout.childControlHeight = true;
            buttonsLayout.childForceExpandWidth = false;
            buttonsLayout.childForceExpandHeight = false;
            SetSize(buttons, 0f, 112f);

            view.Remove = CreateStepperButton(buttons.transform, "Remove", "-",
                () => OnMinusClicked(assignment, level, phoenix), size: 104f);

            view.Add = CreateStepperButton(buttons.transform, "Add", "+",
                () => OnPlusClicked(assignment, level, phoenix), size: 104f);

            view.Counter = CreateLabel(column.transform, "Counter", string.Empty, 84, TextPrimaryColor, TextAnchor.MiddleCenter);
            SetSize(view.Counter.gameObject, 0f, 104f);

            view.Facilities = CreateLabel(column.transform, "Facilities", string.Empty, BodyFontSize, TextDimColor,
                TextAnchor.MiddleCenter);
            SetSize(view.Facilities.gameObject, 0f, 40f);
        }

        private static void CreateWorkerList(Transform parent, WorkPanelView view)
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

            CreateScrollList(column.transform, $"WorkerList_{view.Assignment}", out Transform list);
            view.Workers.Content = list;

            Text empty = CreateLabel(list, "Empty", PersonnelText.Get(PersonnelText.NoWorkers), BodyFontSize,
                TextDimColor, TextAnchor.MiddleCenter);
            SetSize(empty.gameObject, 0f, WorkRowHeight);
            view.Workers.Empty = empty.gameObject;
        }

        private static void SyncWorkPanel(WorkPanelView view, GeoPhoenixFaction phoenix,
            SoldierSlotController slotPrefab, FacilitySlotPools pools)
        {
            if (view == null)
            {
                return;
            }

            PersonnelAssignment assignment = view.Assignment;
            bool isResearch = assignment == PersonnelAssignment.Research;

            FacilitySlotPool pool = isResearch ? pools.Research : pools.Manufacturing;
            int occupied = ResearchAndManufacturing.GetOccupiedSlots(phoenix, assignment);

            ResearchAndManufacturing.GetOutputBonuses(phoenix, out float researchBonus, out float productionBonus);
            float bonus = isResearch ? researchBonus : productionBonus;

            ResearchManufacturingSlotsManager.CountFacilityProviders(phoenix, out int researchFacilities, out int manufacturingFacilities);
            int facilities = isResearch ? researchFacilities : manufacturingFacilities;

            view.Boost.text = PersonnelText.Format(
                isResearch ? PersonnelText.ResearchBoost : PersonnelText.ManufacturingBoost,
                bonus.ToString("0.#"));

            view.Counter.text = $"{occupied} / {pool.ProvidedSlots}";
            view.Counter.color = occupied > 0 ? AccentOrangeColor : TextPrimaryColor;

            view.Facilities.text = isResearch
                ? PersonnelText.Format(PersonnelText.LabsBuilt, researchFacilities)
                : PersonnelText.Format(PersonnelText.PlantsBuilt, manufacturingFacilities);
            view.Facilities.color = facilities > 0 ? TextDimColor : TextDisabledColor;

            bool canAdd = occupied < pool.ProvidedSlots && Assignments.Values.Any(p => p != null && p.Character != null
                && p.Character.Faction == phoenix
                && p.Assignment == PersonnelAssignment.Unassigned
                && PersonnelRestrictions.CanBeAssignedToManufacturingOrResearch(p.Character));

            SetButtonEnabled(view.Remove, occupied > 0);
            SetButtonEnabled(view.Add, canAdd);
            SetButtonEnabled(view.UnassignAll, occupied > 0);

            List<PersonnelInfo> workers = Assignments.Values
                .Where(p => p != null && p.Character != null && p.Character.Faction == phoenix && p.Assignment == assignment)
                .OrderBy(p => GetPersonnelName(p))
                .ToList();

            SyncRows(view.Workers, workers, person => person.Id,
                person => CreateWorkerRow(view.Workers.Content, person, phoenix, slotPrefab),
                null);

            Restripe(view.Workers.Ordered);
        }

        private static RowView CreateWorkerRow(Transform parent, PersonnelInfo person, GeoPhoenixFaction phoenix,
            SoldierSlotController slotPrefab)
        {
            GameObject row = CreateUIObject($"Worker_{person.Id}", parent);
            var view = new RowView { Row = row, Background = row.AddComponent<Image>() };

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
            SetSize(affinityCell, 48f, WorkRowHeight);
            AddAffinityBadge(affinityCell, person.Character, 42f);

            CreateIconButton(row.transform, "Unassign", null,
                () => RunPanelAction(() => UnassignFromWork(person, phoenix)),
                size: 48f, fallbackCaption: "X");

            return view;
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
        }
    }
}
