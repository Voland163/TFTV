using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static TFTV.TFTVBaseRework.PersonnelData;

namespace TFTV.TFTVBaseRework
{
    /// <summary>
    /// The training column: who is in the training facilities and for how much longer, the shared
    /// skill points that pay for it, and the two entry points - train someone, or deploy someone to
    /// a base.
    /// </summary>
    public static partial class PersonnelManagementUI
    {
        private const float TrainingRowHeight = 54f;

        internal static void CreateTrainingPanel(Transform parent, GeoLevelController level, GeoPhoenixFaction phoenix,
            SoldierSlotController slotPrefab)
        {
            int provided = TrainingFacilityRework.GetProvidedTrainingSlots(phoenix);
            int used = TrainingFacilityRework.GetUsedTrainingSlots();

            GameObject panel = CreateFramedPanel(parent, "TrainingPanel", out Transform content);
            LayoutElement panelElement = panel.GetComponent<LayoutElement>() ?? panel.AddComponent<LayoutElement>();
            panelElement.flexibleWidth = 26f;
            panelElement.flexibleHeight = 1f;

            CreateSectionHeader(content, "TRAINING", GetColumnIconSprite(PersonnelAssignment.Training), TextPrimaryColor);

            Text counter = CreateLabel(content, "Counter", $"{used} / {provided}", 44,
                provided > 0 ? AccentOrangeColor : TextDisabledColor, TextAnchor.MiddleCenter);
            SetSize(counter.gameObject, 0f, 56f);

            CreateScrollList(content, "TraineeList", out Transform list);

            List<PersonnelInfo> trainees = Assignments.Values
                .Where(p => p != null && p.Character != null && p.Character.Faction == phoenix
                    && p.Assignment == PersonnelAssignment.Training)
                .OrderBy(p => GetPersonnelName(p))
                .ToList();

            if (trainees.Count == 0)
            {
                Text empty = CreateLabel(list, "Empty", "Nobody in training.", SmallFontSize, TextDimColor,
                    TextAnchor.MiddleCenter);
                SetSize(empty.gameObject, 0f, TrainingRowHeight);
            }
            else
            {
                int index = 0;
                foreach (PersonnelInfo trainee in trainees)
                {
                    CreateTraineeRow(list, trainee, index++, level, slotPrefab);
                }
            }

            CreateSkillpointsReadout(content, phoenix);

            bool trainingSlotFree = used < provided;
            CreateTextButton(content, "TrainButton", "TRAIN", () => ShowTrainingCandidateSelection(level, phoenix),
                height: 64f, enabled: trainingSlotFree,
                fillColor: ButtonFillColor);

            CreateTextButton(content, "DeployButton", "DEPLOY", () => ShowDeployCandidateSelection(level, phoenix),
                height: 78f, fontSize: TitleFontSize,
                fillColor: AccentOrangeColor, captionColor: Color.black);
        }

        private static void CreateSkillpointsReadout(Transform parent, GeoPhoenixFaction phoenix)
        {
            GameObject box = CreateUIObject("Skillpoints", parent);
            var layout = box.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            SetSize(box, 0f, 92f);

            Text caption = CreateLabel(box.transform, "Caption", "PHOENIX SP", SmallFontSize, TextDimColor,
                TextAnchor.MiddleCenter);
            SetSize(caption.gameObject, 0f, 28f);

            Text value = CreateLabel(box.transform, "Value", phoenix.Skillpoints.ToString(), 52, AccentOrangeColor,
                TextAnchor.MiddleCenter);
            SetSize(value.gameObject, 0f, 60f);
        }

        private static void CreateTraineeRow(Transform parent, PersonnelInfo person, int index, GeoLevelController level,
            SoldierSlotController slotPrefab)
        {
            bool complete = TrainingFacilityRework.IsRecruitTrainingComplete(person.Character, level);

            GameObject row = CreateUIObject($"Trainee_{person.Id}", parent);
            row.AddComponent<Image>().color = index % 2 == 0 ? RowFillColor : RowFillAltColor;

            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.padding = new RectOffset(4, 4, 2, 2);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            SetSize(row, 0f, TrainingRowHeight);

            var entry = new RosterEntry { Character = person.Character, Personnel = person };
            CreateNameCell(row.transform, entry, slotPrefab);

            string remaining = complete
                ? "Ready"
                : FormatDuration(TrainingFacilityRework.GetRecruitRemainingHours(person.Character, level));
            Text time = CreateLabel(row.transform, "Remaining", remaining, SmallFontSize,
                complete ? AccentOrangeColor : AccentCyanColor, TextAnchor.MiddleRight);
            SetSize(time.gameObject, 150f, TrainingRowHeight);

            // Pulling someone out of training finalises them early and takes them straight to the
            // deployment prompt, with the partial refund the deployment flow already calculates.
            CreateIconButton(row.transform, "Finalize", null, () => ShowSlotContextMenu(person),
                size: 40f, fallbackCaption: "X");
        }

        #region Candidate pickers

        /// <summary>
        /// The TRAIN button has no row selection behind it, so it asks who first, then hands over to
        /// the existing class and level flow.
        /// </summary>
        private static void ShowTrainingCandidateSelection(GeoLevelController level, GeoPhoenixFaction phoenix)
        {
            List<PersonnelInfo> candidates = Assignments.Values
                .Where(p => p != null && p.Character != null && p.Character.Faction == phoenix
                    && p.Assignment != PersonnelAssignment.Training)
                .OrderBy(p => GetPersonnelName(p))
                .ToList();

            CloseModal();
            _modalRoot = CreateModalRoot("TrainingCandidateModal");
            AddModalHeader("Who should train?");
            Transform content = CreateModalContentArea();

            if (candidates.Count == 0)
            {
                AddDisabledLabel(content, "No personnel available to train.");
            }

            foreach (PersonnelInfo person in candidates)
            {
                string label = PersonnelRestrictions.IsDismissedOperative(person.Character)
                    ? $"{GetPersonnelName(person)} (level {person.Character.LevelProgression?.Level ?? 1})"
                    : GetPersonnelName(person);

                AddModalOptionButton(content, label, () => StartTrainingFlow(level, person));
            }

            AddModalCloseButton();
        }

        /// <summary>
        /// Dismissed operatives keep the class they already have, so they skip class selection and go
        /// straight to picking a level; civilians choose a class first.
        /// </summary>
        private static void StartTrainingFlow(GeoLevelController level, PersonnelInfo person)
        {
            if (!PersonnelRestrictions.IsDismissedOperative(person.Character))
            {
                ShowTrainingSelection(level, person, ResolveAvailableMainSpecs(level), () => RefreshPanel());
                return;
            }

            SpecializationDef existingSpec = ResolveExistingSpecialization(person.Character);
            if (existingSpec == null)
            {
                ShowMessage($"Could not determine class for {person.Character?.DisplayName}.");
                return;
            }

            ShowTrainingLevelSelection(level, person, existingSpec, () => RefreshPanel());
        }

        private static void ShowDeployCandidateSelection(GeoLevelController level, GeoPhoenixFaction phoenix)
        {
            List<PersonnelInfo> candidates = Assignments.Values
                .Where(p => p != null && p.Character != null && p.Character.Faction == phoenix)
                .OrderBy(p => GetPersonnelName(p))
                .ToList();

            CloseModal();
            _modalRoot = CreateModalRoot("DeployCandidateModal");
            AddModalHeader("Who should deploy?");
            Transform content = CreateModalContentArea();

            if (candidates.Count == 0)
            {
                AddDisabledLabel(content, "No personnel available to deploy.");
            }

            foreach (PersonnelInfo person in candidates)
            {
                string suffix = person.Assignment == PersonnelAssignment.Training
                    ? $" - {GetAssignmentDisplay(person, level)}"
                    : PersonnelRestrictions.IsDismissedOperative(person.Character)
                        ? $" - dismissed, {PersonnelRestrictions.GetRedeployCost(person.Character)} SP"
                        : string.Empty;

                AddModalOptionButton(content, $"{GetPersonnelName(person)}{suffix}", () => ShowSlotContextMenu(person));
            }

            AddModalCloseButton();
        }

        #endregion
    }
}
