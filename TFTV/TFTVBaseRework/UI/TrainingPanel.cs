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
        private const float TrainingRowHeight = 62f;

        /// <summary>Everything in the training panel that changes as people come and go.</summary>
        private sealed class TrainingPanelView
        {
            internal Text Counter;
            internal Text Skillpoints;
            internal Button Train;
            internal readonly KeyedRows<TraineeRowView> Trainees = new KeyedRows<TraineeRowView>();
        }

        private sealed class TraineeRowView : RowView
        {
            internal Text Remaining;
        }

        private static TrainingPanelView _trainingView;

        internal static void CreateTrainingPanel(Transform parent, GeoLevelController level, GeoPhoenixFaction phoenix)
        {
            var view = new TrainingPanelView();

            GameObject panel = CreateFramedPanel(parent, "TrainingPanel", out Transform content);
            LayoutElement panelElement = panel.GetComponent<LayoutElement>() ?? panel.AddComponent<LayoutElement>();
            panelElement.flexibleWidth = 1f;
            panelElement.flexibleHeight = 1f;

            CreateSectionHeader(content, PersonnelText.Get(PersonnelText.TrainingTitle),
                GetColumnIconSprite(PersonnelAssignment.Training), TextPrimaryColor);

            view.Counter = CreateLabel(content, "Counter", string.Empty, 84, AccentOrangeColor, TextAnchor.MiddleCenter);
            SetSize(view.Counter.gameObject, 0f, 104f);

            CreateScrollList(content, "TraineeList", out Transform list);
            view.Trainees.Content = list;

            Text empty = CreateLabel(list, "Empty", PersonnelText.Get(PersonnelText.TrainingEmpty), BodyFontSize,
                TextDimColor, TextAnchor.MiddleCenter);
            SetSize(empty.gameObject, 0f, TrainingRowHeight);
            view.Trainees.Empty = empty.gameObject;

            view.Skillpoints = CreateSkillpointsReadout(content);

            view.Train = CreateTextButton(content, "TrainButton", PersonnelText.Get(PersonnelText.ButtonTrain),
                () => ShowTrainingCandidateSelection(level, phoenix),
                height: 76f, fontSize: TitleFontSize, fillColor: ButtonFillColor);

            _trainingView = view;
        }

        private static void SyncTrainingPanel(GeoLevelController level, GeoPhoenixFaction phoenix,
            SoldierSlotController slotPrefab)
        {
            TrainingPanelView view = _trainingView;
            if (view == null)
            {
                return;
            }

            int provided = TrainingFacilityRework.GetProvidedTrainingSlots(phoenix);
            int used = TrainingFacilityRework.GetUsedTrainingSlots();

            view.Counter.text = $"{used} / {provided}";
            view.Counter.color = provided > 0 ? AccentOrangeColor : TextDisabledColor;
            view.Skillpoints.text = phoenix.Skillpoints.ToString();
            SetButtonEnabled(view.Train, used < provided);

            List<PersonnelInfo> trainees = Assignments.Values
                .Where(p => p != null && p.Character != null && p.Character.Faction == phoenix
                    && p.Assignment == PersonnelAssignment.Training)
                .OrderBy(p => GetPersonnelName(p))
                .ToList();

            SyncRows(view.Trainees, trainees, person => person.Id,
                person => CreateTraineeRow(view.Trainees.Content, person, slotPrefab),
                (person, row) => UpdateTraineeRow(person, row, level));

            Restripe(view.Trainees.Ordered);
        }

        /// <summary>
        /// Sending someone into the field is the biggest thing that happens on this screen, so it is
        /// its own control in the bottom-right corner - next to training, which it follows on from,
        /// but outside the training panel so the two are not read as one setting.
        /// </summary>
        internal static void CreateDeployButton(Transform parent, GeoLevelController level, GeoPhoenixFaction phoenix)
        {
            CreateTextButton(parent, "DeployButton", PersonnelText.Get(PersonnelText.ButtonDeploy),
                () => ShowDeployCandidateSelection(level, phoenix),
                height: 118f, fontSize: 48,
                fillColor: AccentOrangeColor, captionColor: Color.black);
        }

        private static Text CreateSkillpointsReadout(Transform parent)
        {
            GameObject box = CreateUIObject("Skillpoints", parent);
            box.AddComponent<Image>().color = RowFillColor;

            var layout = box.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.padding = new RectOffset(12, 12, 0, 0);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            SetSize(box, 0f, 80f);

            Text caption = CreateLabel(box.transform, "Caption", PersonnelText.Get(PersonnelText.PhoenixSkillPoints),
                TitleFontSize, TextDimColor,
                TextAnchor.MiddleRight);
            LayoutElement captionElement = SetSize(caption.gameObject, 0f, 80f);
            captionElement.flexibleWidth = 1f;

            Text value = CreateLabel(box.transform, "Value", string.Empty, 56, AccentOrangeColor,
                TextAnchor.MiddleLeft);
            SetSize(value.gameObject, 130f, 80f);
            return value;
        }

        private static TraineeRowView CreateTraineeRow(Transform parent, PersonnelInfo person,
            SoldierSlotController slotPrefab)
        {
            GameObject row = CreateUIObject($"Trainee_{person.Id}", parent);
            var view = new TraineeRowView { Row = row, Background = row.AddComponent<Image>() };

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

            view.Remaining = CreateLabel(row.transform, "Remaining", string.Empty, BodyFontSize,
                AccentCyanColor, TextAnchor.MiddleRight);
            SetSize(view.Remaining.gameObject, 170f, TrainingRowHeight);

            // Pulling someone out of training finalises them early and takes them straight to the
            // deployment prompt, with the partial refund the deployment flow already calculates.
            CreateIconButton(row.transform, "Finalize", null, () => ShowSlotContextMenu(person),
                size: 48f, fallbackCaption: "X");

            return view;
        }

        private static void UpdateTraineeRow(PersonnelInfo person, TraineeRowView view, GeoLevelController level)
        {
            bool complete = TrainingFacilityRework.IsRecruitTrainingComplete(person.Character, level);

            view.Remaining.text = complete
                ? PersonnelText.Get(PersonnelText.TrainingReady)
                : FormatDuration(TrainingFacilityRework.GetRecruitRemainingHours(person.Character, level));
            view.Remaining.color = complete ? AccentOrangeColor : AccentCyanColor;
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
            AddModalHeader(PersonnelText.Get(PersonnelText.WhoTrains));
            Transform content = CreateModalContentArea();

            if (candidates.Count == 0)
            {
                AddDisabledLabel(content, PersonnelText.Get(PersonnelText.NoTrainCandidates));
            }

            foreach (PersonnelInfo person in candidates)
            {
                string label = PersonnelRestrictions.IsDismissedOperative(person.Character)
                    ? PersonnelText.Format(PersonnelText.CandidateLevel, GetPersonnelName(person),
                        person.Character.LevelProgression?.Level ?? 1)
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
                ShowMessage(PersonnelText.Format(PersonnelText.ClassUnknown, person.Character?.DisplayName));
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
            AddModalHeader(PersonnelText.Get(PersonnelText.WhoDeploys));
            Transform content = CreateModalContentArea();

            if (candidates.Count == 0)
            {
                AddDisabledLabel(content, PersonnelText.Get(PersonnelText.NoDeployCandidates));
            }

            foreach (PersonnelInfo person in candidates)
            {
                string label;
                if (person.Assignment == PersonnelAssignment.Training)
                {
                    label = PersonnelText.Format(PersonnelText.CandidateTraining, GetPersonnelName(person),
                        GetAssignmentDisplay(person, level));
                }
                else if (PersonnelRestrictions.IsDismissedOperative(person.Character))
                {
                    label = PersonnelText.Format(PersonnelText.CandidateDismissed, GetPersonnelName(person),
                        PersonnelRestrictions.GetRedeployCost(person.Character));
                }
                else
                {
                    label = GetPersonnelName(person);
                }

                AddModalOptionButton(content, label, () => ShowSlotContextMenu(person));
            }

            AddModalCloseButton();
        }

        #endregion
    }
}
