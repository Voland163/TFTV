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

        private static void RefreshPanel()
        {
            if (_personnelPanel != null) { Object.Destroy(_personnelPanel); _personnelPanel = null; }
            if (_cachedState != null)
            {
                CreatePersonnelPanel(_cachedState);
            }
        }

        /// <summary>
        /// Three regions across the recruits screen: the roster on the left, the two work panels
        /// stacked in the middle, and training on the right.
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
                FacilitySlotPools pools = ResearchManufacturingSlotsManager.RecalculateSlots(phoenix);
                SoldierSlotController slotPrefab = level.View.GeoscapeModules.SoldierEquipModule.SoldierSlotPrefab;

                CreateRosterColumn(_personnelPanel.transform, level, phoenix, slotPrefab);

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

                CreateWorkPanel(workColumn.transform, PersonnelAssignment.Research, level, phoenix, slotPrefab, pools);
                CreateWorkPanel(workColumn.transform, PersonnelAssignment.Manufacturing, level, phoenix, slotPrefab, pools);

                CreateTrainingPanel(_personnelPanel.transform, level, phoenix, slotPrefab);
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
            // Find the first Unassigned personnel and move them to the target column
            var candidate = Assignments.Values
                .Where(p => p != null && p.Character != null && p.Assignment == PersonnelAssignment.Unassigned)
                .OrderBy(p => GetPersonnelName(p))
                .FirstOrDefault();

            if (candidate == null)
            {
                TFTVLogger.Always($"{LogPrefix} [+] No unassigned personnel to add to {targetColumn}.");
                return;
            }

            MovePersonnelToColumn(candidate, targetColumn, level, phoenix);

            // Don't refresh here if a modal was opened (Training column opens a modal)
            if (targetColumn != PersonnelAssignment.Training)
            {
                RefreshPanel();
            }
        }

        private static void OnMinusClicked(PersonnelAssignment sourceColumn, GeoLevelController level, GeoPhoenixFaction phoenix)
        {
            if (sourceColumn == PersonnelAssignment.Unassigned)
            {
                return;
            }

            // Find the first personnel in the source column and move them to Unassigned
            var candidate = Assignments.Values
                .Where(p => p != null && p.Character != null && p.Assignment == sourceColumn)
                .OrderBy(p => GetPersonnelName(p))
                .FirstOrDefault();

            if (candidate == null)
            {
                TFTVLogger.Always($"{LogPrefix} [-] No personnel in {sourceColumn} to remove.");
                return;
            }

            UnassignFromWork(candidate, phoenix);
            RefreshPanel();
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
