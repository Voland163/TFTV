using Base.Core;
using Base.UI.MessageBox;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewStates;
using System;
using System.Linq;
using TFTV.TFTVIncidents;
using UnityEngine;
using UnityEngine.UI;
using static TFTV.TFTVBaseRework.BaseReworkCheck;
using static TFTV.TFTVBaseRework.PersonnelData;
using static TFTV.TFTVBaseRework.Workers;
using Object = UnityEngine.Object;

namespace TFTV.TFTVBaseRework
{
    public static partial class PersonnelManagementUI
    {
        #region Panel Construction

        /// <summary>
        /// Brings the screen into line with the records after anything has changed. The panel is
        /// built once, when the screen opens; from then on this updates what it shows in place,
        /// building only the rows of people who have just arrived in a list and destroying only
        /// those who have just left one. Rebuilding everything on each click - three dozen rows,
        /// each an instance of the vanilla soldier slot, under a fresh canvas - was the stutter.
        /// </summary>
        private static void RefreshPanel()
        {
            if (_personnelPanel == null)
            {
                if (_cachedState != null)
                {
                    CreatePersonnelPanel(_cachedState);
                }

                return;
            }

            CloseModal();
            SyncPanel();
        }

        /// <summary>
        /// Runs a change to the records and then refreshes the screen, holding the info bar back
        /// until the change is complete so the game recalculates production once for the action
        /// rather than once for every counter it touches.
        /// </summary>
        private static void RunPanelAction(Action action, bool refresh = true)
        {
            using (DeferInfoBarUpdates())
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            if (refresh)
            {
                RefreshPanel();
            }
        }

        private static void SyncPanel(bool rosterOnly = false)
        {
            try
            {
                GeoLevelController level = _cachedLevel;
                GeoPhoenixFaction phoenix = level?.PhoenixFaction;
                if (phoenix == null || _personnelPanel == null)
                {
                    return;
                }

                FacilitySlotPools pools = ResearchManufacturingSlotsManager.RecalculateSlots(phoenix);
                SoldierSlotController slotPrefab = level.View.GeoscapeModules.SoldierEquipModule.SoldierSlotPrefab;

                SyncRoster(level, phoenix, slotPrefab, pools);

                if (rosterOnly)
                {
                    return;
                }

                SyncWorkPanel(_researchPanel, phoenix, slotPrefab, pools);
                SyncWorkPanel(_manufacturingPanel, phoenix, slotPrefab, pools);
                SyncTrainingPanel(level, phoenix, slotPrefab);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>Forgets every view the panel held, once the panel itself is gone.</summary>
        internal static void ResetPanelViews()
        {
            ResetRosterView();
            _researchPanel = null;
            _manufacturingPanel = null;
            _trainingView = null;
        }

        /// <summary>
        /// Three regions across the recruits screen: the roster on the left, the two work panels
        /// stacked in the middle, and training on the right. Only the frame is built here; the
        /// contents are filled in by the same sync every later change goes through.
        /// </summary>
        private static void CreatePersonnelPanel(UIStateRosterRecruits state)
        {
            if (!BaseReworkEnabled)
            {
                return;
            }

            var level = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
            var recruitsModule = level?.View?.GeoscapeModules?.RecruitsListModule;
            if (recruitsModule == null) return;

            _cachedState = state;
            _cachedLevel = level;

            try
            {
                if (_personnelPanel != null)
                {
                    Object.Destroy(_personnelPanel);
                    _personnelPanel = null;
                }

                ResetPanelViews();

                _personnelPanel = new GameObject(PersonnelContainerName, typeof(RectTransform));
                _personnelPanel.transform.SetParent(recruitsModule.transform, false);

                var canvas = _personnelPanel.AddComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingOrder = 10;
                _personnelPanel.AddComponent<GraphicRaycaster>();

                var rect = _personnelPanel.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.0f, 0.1f);
                rect.anchorMax = new Vector2(1.0f, 0.9f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.offsetMin = new Vector2(125, 25);
                rect.offsetMax = new Vector2(-25, -25);

                var panelLayout = _personnelPanel.AddComponent<HorizontalLayoutGroup>();
                panelLayout.spacing = 10f;
                panelLayout.padding = new RectOffset(4, 4, 4, 4);
                panelLayout.childControlWidth = true;
                panelLayout.childControlHeight = true;
                panelLayout.childForceExpandWidth = false;
                panelLayout.childForceExpandHeight = true;

                GeoPhoenixFaction phoenix = level.PhoenixFaction;

                CreateRosterColumn(_personnelPanel.transform, level, phoenix);

                GameObject workColumn = CreateUIObject("WorkColumn", _personnelPanel.transform);
                var workLayout = workColumn.AddComponent<VerticalLayoutGroup>();
                workLayout.spacing = 10f;
                workLayout.childControlWidth = true;
                workLayout.childControlHeight = true;
                workLayout.childForceExpandWidth = true;
                workLayout.childForceExpandHeight = false;
                LayoutElement workElement = workColumn.AddComponent<LayoutElement>();
                workElement.flexibleWidth = 44f;
                workElement.flexibleHeight = 1f;

                _researchPanel = CreateWorkPanel(workColumn.transform, PersonnelAssignment.Research, level, phoenix);
                _manufacturingPanel = CreateWorkPanel(workColumn.transform, PersonnelAssignment.Manufacturing, level, phoenix);

                // Training and deployment are related enough to sit together, but deploying someone
                // is the decision this screen exists to lead up to, so it gets the bottom-right
                // corner to itself rather than being read as one more training control.
                GameObject deployColumn = CreateUIObject("DeployColumn", _personnelPanel.transform);
                var deployLayout = deployColumn.AddComponent<VerticalLayoutGroup>();
                deployLayout.spacing = 16f;
                deployLayout.childControlWidth = true;
                deployLayout.childControlHeight = true;
                deployLayout.childForceExpandWidth = true;
                deployLayout.childForceExpandHeight = false;
                LayoutElement deployElement = deployColumn.AddComponent<LayoutElement>();
                deployElement.flexibleWidth = 26f;
                deployElement.flexibleHeight = 1f;

                CreateTrainingPanel(deployColumn.transform, level, phoenix);
                CreateDeployButton(deployColumn.transform, level, phoenix);

                SyncPanel();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void AddAffinityBadge(GameObject slotGO, GeoCharacter character, float size = 46f)
        {
            try
            {
                if (slotGO == null
                    || !LeaderSelection.TryGetCurrentAffinity(character, out LeaderSelection.AffinityApproach approach, out int rank))
                {
                    return;
                }

                Sprite icon = LeaderSelection.GetAffinityAbility(approach, rank)?.ViewElementDef?.SmallIcon;
                if (icon == null)
                {
                    return;
                }

                var badgeGO = new GameObject("AffinityBadge", typeof(RectTransform));
                badgeGO.transform.SetParent(slotGO.transform, false);

                var badgeImg = badgeGO.AddComponent<Image>();
                badgeImg.sprite = icon;
                badgeImg.preserveAspect = true;
                // Raycasts have to reach the badge for the hover tooltip.
                badgeImg.raycastTarget = true;

                var badgeRect = badgeGO.GetComponent<RectTransform>();
                badgeRect.anchorMin = new Vector2(0.5f, 0.5f);
                badgeRect.anchorMax = new Vector2(0.5f, 0.5f);
                badgeRect.pivot = new Vector2(0.5f, 0.5f);
                badgeRect.anchoredPosition = Vector2.zero;
                badgeRect.sizeDelta = new Vector2(size, size);

                var tooltip = badgeGO.AddComponent<AffinityBadgeTooltip>();
                tooltip.Approach = approach;
                tooltip.Rank = rank;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        #endregion

        #region +/- Button Logic

        private static void OnPlusClicked(PersonnelAssignment targetColumn, GeoLevelController level, GeoPhoenixFaction phoenix)
        {
            // The first free person who can actually do the work: a grunt heading the list used to
            // make the button do nothing at all.
            var candidate = Assignments.Values
                .Where(p => p != null && p.Character != null && p.Character.Faction == phoenix
                    && p.Assignment == PersonnelAssignment.Unassigned
                    && PersonnelRestrictions.CanBeAssignedToManufacturingOrResearch(p.Character))
                .OrderBy(p => GetPersonnelName(p))
                .FirstOrDefault();

            if (candidate == null)
            {
                TFTVLogger.Always($"{LogPrefix} [+] No unassigned personnel to add to {targetColumn}.");
                return;
            }

            // Training opens a modal and refreshes through its own callback; refreshing here would
            // close that modal the moment it appeared.
            RunPanelAction(() => MovePersonnelToColumn(candidate, targetColumn, level, phoenix),
                refresh: targetColumn != PersonnelAssignment.Training);
        }

        private static void OnMinusClicked(PersonnelAssignment sourceColumn, GeoLevelController level, GeoPhoenixFaction phoenix)
        {
            if (sourceColumn == PersonnelAssignment.Unassigned)
            {
                return;
            }

            // Find the first personnel in the source column and move them to Unassigned
            var candidate = Assignments.Values
                .Where(p => p != null && p.Character != null && p.Character.Faction == phoenix
                    && p.Assignment == sourceColumn)
                .OrderBy(p => GetPersonnelName(p))
                .FirstOrDefault();

            if (candidate == null)
            {
                TFTVLogger.Always($"{LogPrefix} [-] No personnel in {sourceColumn} to remove.");
                return;
            }

            RunPanelAction(() => UnassignFromWork(candidate, phoenix));
        }

        internal static void MovePersonnelToColumn(PersonnelInfo person, PersonnelAssignment targetColumn, GeoLevelController level, GeoPhoenixFaction phoenix)
        {
            if (person == null || phoenix == null) return;

            PersonnelAssignment currentAssignment = person.Assignment;
            if (currentAssignment == targetColumn) return;

            switch (targetColumn)
            {
                case PersonnelAssignment.Unassigned:
                    if (currentAssignment == PersonnelAssignment.Training)
                    {
                        TFTVLogger.Always($"{LogPrefix} Cannot move {person.Character?.DisplayName} from Training back to Unassigned.");
                        return;
                    }
                    UnassignFromWork(person, phoenix);
                    break;

                case PersonnelAssignment.Research:
                    if (!PersonnelRestrictions.CanBeAssignedToManufacturingOrResearch(person.Character))
                    {
                        TFTVLogger.Always($"{LogPrefix} {person.Character?.DisplayName} cannot be assigned to Research (Just a Grunt).");
                        return;
                    }
                    if (currentAssignment == PersonnelAssignment.Unassigned && PersonnelData.IsLivingCapacityFull(phoenix))
                    {
                        ShowLivingQuartersFull(PersonnelText.DutyResearch);
                        return;
                    }
                    AssignWorker(person, phoenix, FacilitySlotType.Research);
                    break;

                case PersonnelAssignment.Manufacturing:
                    if (!PersonnelRestrictions.CanBeAssignedToManufacturingOrResearch(person.Character))
                    {
                        TFTVLogger.Always($"{LogPrefix} {person.Character?.DisplayName} cannot be assigned to Manufacturing (Just a Grunt).");
                        return;
                    }
                    if (currentAssignment == PersonnelAssignment.Unassigned && PersonnelData.IsLivingCapacityFull(phoenix))
                    {
                        ShowLivingQuartersFull(PersonnelText.DutyManufacturing);
                        return;
                    }
                    AssignWorker(person, phoenix, FacilitySlotType.Manufacturing);
                    break;

                case PersonnelAssignment.Training:
                    if (currentAssignment == PersonnelAssignment.Unassigned && PersonnelData.IsLivingCapacityFull(phoenix))
                    {
                        ShowLivingQuartersFull(PersonnelText.DutyTraining);
                        return;
                    }
                    ShowDeployOrTrainSelection(level, person, phoenix, () => RefreshPanel());
                    return; // Don't refresh yet — modal is open
            }
        }

        private static void ShowLivingQuartersFull(string dutyKey)
        {
            GameUtl.GetMessageBox().ShowSimplePrompt(
                PersonnelText.Format(PersonnelText.LivingQuartersFull, PersonnelText.Get(dutyKey)),
                MessageBoxIcon.Warning, MessageBoxButtons.OK, null);
        }

        #endregion

        #region Assignment Display
        private static string GetAssignmentDisplay(PersonnelInfo person, GeoLevelController level)
        {
            if (person?.Character == null)
            {
                return PersonnelText.Get(PersonnelText.StatusUnknownName);
            }

            if (person.Assignment == PersonnelAssignment.Unassigned && PersonnelRestrictions.IsDismissedOperative(person.Character))
            {
                return PersonnelText.Get(PersonnelText.StatusDismissed);
            }

            switch (person.Assignment)
            {
                case PersonnelAssignment.Training:
                    var session = TrainingFacilityRework.GetRecruitSession(person.Character);
                    if (session == null) return PersonnelText.Get(PersonnelText.AssignmentTrainingQueued);
                    bool complete = TrainingFacilityRework.IsRecruitTrainingComplete(person.Character, level);
                    double remainingHours = TrainingFacilityRework.GetRecruitRemainingHours(person.Character, level);
                    string specName = person.TrainingSpec?.ViewElementDef.DisplayName1.Localize()
                        ?? person.TrainingSpec?.name
                        ?? PersonnelText.Get(PersonnelText.ClassFallback);

                    if (complete)
                    {
                        return PersonnelText.Format(PersonnelText.AssignmentTrainingComplete, specName);
                    }

                    return PersonnelText.Format(PersonnelText.AssignmentTrainingProgress, specName,
                        session.VirtualLevelAchieved, session.TargetLevel, FormatDuration(remainingHours));
                default:
                    return person.Assignment.ToString();
            }
        }
        #endregion

        #region Context Menu
        /// <summary>
        /// Opens the deploy/redeploy base-selection dialog for a personnel row.
        /// </summary>
        internal static void ShowSlotContextMenu(PersonnelInfo person)
        {
            if (person == null) return;
            var level = _cachedLevel ?? GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
            var phoenix = level?.PhoenixFaction;
            if (level == null || phoenix == null) return;

            var specs = ResolveAvailableMainSpecs(level);

            // Show deploy/redeploy option
            ShowDeploymentSelection(level, person, phoenix, specs, () => RefreshPanel());
        }
        #endregion
    }
}
