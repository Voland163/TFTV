using Base.Core;
using Base.UI.MessageBox;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Linq;
using TFTV.TFTVIncidents;
using UnityEngine;
using UnityEngine.EventSystems;
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
                // Root panel with Canvas
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

                // VerticalLayoutGroup on root so we can stack: top bar + columns
                var panelLayout = _personnelPanel.AddComponent<VerticalLayoutGroup>();
                panelLayout.spacing = 4f;
                panelLayout.padding = new RectOffset(0, 0, 0, 0);
                panelLayout.childControlWidth = true;
                panelLayout.childControlHeight = true;
                panelLayout.childForceExpandWidth = true;
                panelLayout.childForceExpandHeight = false;

                var phoenix = level.PhoenixFaction;

                // Top bar with auto-assign toggle
                CreateAutoAssignToggleBar(_personnelPanel.transform, phoenix);

                // Horizontal layout for 4 columns
                var columnsContainer = new GameObject("ColumnsContainer", typeof(RectTransform));
                columnsContainer.transform.SetParent(_personnelPanel.transform, false);
                var columnsLE = columnsContainer.AddComponent<LayoutElement>();
                columnsLE.flexibleHeight = 1;

                var hLayout = columnsContainer.AddComponent<HorizontalLayoutGroup>();
                hLayout.spacing = 12f;
                hLayout.padding = new RectOffset(8, 8, 8, 8);
                hLayout.childControlWidth = true;
                hLayout.childControlHeight = true;
                hLayout.childForceExpandWidth = true;
                hLayout.childForceExpandHeight = true;

                FacilitySlotPools pools = ResearchManufacturingSlotsManager.GetOrCreatePools(phoenix);

                // Resolve SoldierSlotController prefab
                SoldierSlotController slotPrefab = level.View.GeoscapeModules.SoldierEquipModule.SoldierSlotPrefab;

                // Training slot counts
                int trainProvided = TrainingFacilityRework.GetProvidedTrainingSlots(phoenix);
                int trainUsed = TrainingFacilityRework.GetUsedTrainingSlots();

                // Create 4 columns
                CreateColumn(columnsContainer.transform, PersonnelAssignment.Unassigned, "Unassigned", null, level, phoenix, slotPrefab);
                // Occupied counts come from the personnel records, matching the info bar and the income.
                int researchUsed = ResearchAndManufacturing.GetOccupiedSlots(phoenix, PersonnelAssignment.Research);
                int manufacturingUsed = ResearchAndManufacturing.GetOccupiedSlots(phoenix, PersonnelAssignment.Manufacturing);

                CreateColumn(columnsContainer.transform, PersonnelAssignment.Research, $"Research ({researchUsed}/{pools.Research.ProvidedSlots})", pools.Research, level, phoenix, slotPrefab);
                CreateColumn(columnsContainer.transform, PersonnelAssignment.Manufacturing, $"Manufacturing ({manufacturingUsed}/{pools.Manufacturing.ProvidedSlots})", pools.Manufacturing, level, phoenix, slotPrefab);
                CreateColumn(columnsContainer.transform, PersonnelAssignment.Training, $"Deploy / Train ({trainUsed}/{trainProvided})", null, level, phoenix, slotPrefab);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        private static void CreateAutoAssignToggleBar(Transform parent, GeoPhoenixFaction phoenix)
        {
            GeoLevelController level = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
            PersonnelData.EnsureAutoAssignSettingInitialized(level);

            var bar = new GameObject("AutoAssignBar", typeof(RectTransform));
            bar.transform.SetParent(parent, false);
            bar.AddComponent<Image>().color = new Color(0.08f, 0.10f, 0.14f, 0.90f);

            var barLayout = bar.AddComponent<HorizontalLayoutGroup>();
            barLayout.spacing = 10f;
            barLayout.padding = new RectOffset(12, 12, 8, 8);
            barLayout.childAlignment = TextAnchor.MiddleLeft;
            barLayout.childControlWidth = true;
            barLayout.childForceExpandWidth = false;
            barLayout.childControlHeight = true;
            barLayout.childForceExpandHeight = false;

            var barLE = bar.AddComponent<LayoutElement>();
            barLE.minHeight = 88;
            barLE.preferredHeight = 88;

            bool isOn = PersonnelData.AutoAssignEnabled;
            string label = isOn ? "Auto-Assign: ON" : "Auto-Assign: OFF";

            var toggleGO = new GameObject("AutoAssignToggle", typeof(RectTransform));
            toggleGO.transform.SetParent(bar.transform, false);

            var toggleImg = toggleGO.AddComponent<Image>();
            toggleImg.color = isOn ? ToggleOnColor : ToggleOffColor;

            var toggleBtn = toggleGO.AddComponent<Button>();
            toggleBtn.onClick.AddListener(() =>
            {
                try
                {
                    GeoLevelController currentLevel = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
                    PersonnelData.SetAutoAssignEnabled(currentLevel, !PersonnelData.AutoAssignEnabled);

                    if (PersonnelData.AutoAssignEnabled && phoenix != null)
                    {
                        PersonnelData.TryAutoAssignUnassignedPersonnel(phoenix, "ToggleUI");
                    }

                    RefreshPanel();
                }
                catch (Exception e) { TFTVLogger.Error(e); }
            });

            var toggleLE = toggleGO.AddComponent<LayoutElement>();
            toggleLE.minWidth = 540;
            toggleLE.preferredWidth = 540;
            toggleLE.minHeight = 72;
            toggleLE.preferredHeight = 72;

            var txtGO = new GameObject("Text", typeof(RectTransform));
            txtGO.transform.SetParent(toggleGO.transform, false);

            var txt = txtGO.AddComponent<Text>();
            txt.font = PuristaSemibold;
            txt.text = label;
            txt.fontSize = ColumnFontSize;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Truncate;
            txt.resizeTextForBestFit = true;
            txt.resizeTextMinSize = 22;
            txt.resizeTextMaxSize = 40;

            var txtRect = txtGO.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = new Vector2(8, 4);
            txtRect.offsetMax = new Vector2(-8, -4);
        }

        private static void CreateColumn(Transform parent, PersonnelAssignment assignment, string headerText, FacilitySlotPool slotPool, GeoLevelController level, GeoPhoenixFaction phoenix, SoldierSlotController slotPrefab)
        {
            // Column root
            var column = new GameObject($"Column_{assignment}", typeof(RectTransform));
            column.transform.SetParent(parent, false);
            var columnLayout = column.AddComponent<VerticalLayoutGroup>();
            columnLayout.spacing = 0f;
            columnLayout.padding = new RectOffset(0, 0, 0, 0);
            columnLayout.childControlWidth = true;
            columnLayout.childControlHeight = true;
            columnLayout.childForceExpandWidth = true;
            columnLayout.childForceExpandHeight = false;

            // Header area
            var header = new GameObject("Header", typeof(RectTransform));
            header.transform.SetParent(column.transform, false);
            header.AddComponent<Image>().color = ColHeaderBg;
            var headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.spacing = 6f;
            headerLayout.padding = new RectOffset(8, 8, 4, 4);
            headerLayout.childAlignment = TextAnchor.MiddleCenter;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = false;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = false;
            var headerLE = header.AddComponent<LayoutElement>();
            headerLE.minHeight = ColumnHeaderHeight;
            headerLE.preferredHeight = ColumnHeaderHeight;

            // [+] button — only for Research, Manufacturing, and Training
            if (assignment != PersonnelAssignment.Unassigned)
            {
                CreateHeaderButton(header.transform, "+", () => OnPlusClicked(assignment, level, phoenix));
            }

            // Column icon — scaled to match header proportions
            var iconGO = new GameObject($"ColumnIcon_{assignment}", typeof(RectTransform));
            iconGO.transform.SetParent(header.transform, false);
            var iconImg = iconGO.AddComponent<Image>();
            Sprite columnSprite = GetColumnIconSprite(assignment);
            if (columnSprite != null)
            {
                iconImg.sprite = columnSprite;
                iconImg.color = Color.white;
                iconImg.preserveAspect = true;
            }
            else
            {
                iconImg.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }

            float size = ColumnHeaderIconSize; //32

            var iconLE = iconGO.AddComponent<LayoutElement>();
            iconLE.minWidth = size;
            iconLE.preferredWidth = size;
            iconLE.minHeight = size;
            iconLE.preferredHeight = size;

            // Header label
            var labelGO = new GameObject("HeaderLabel", typeof(RectTransform));
            labelGO.transform.SetParent(header.transform, false);
            var labelText = labelGO.AddComponent<Text>();
            labelText.font = PuristaSemibold;
            labelText.text = headerText;
            labelText.fontSize = ColumnFontSize;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.horizontalOverflow = HorizontalWrapMode.Wrap;
            labelText.verticalOverflow = VerticalWrapMode.Truncate;
            labelText.resizeTextForBestFit = true;
            labelText.resizeTextMinSize = ColumnFontSize / 2;
            labelText.resizeTextMaxSize = ColumnFontSize;
            var labelLE = labelGO.AddComponent<LayoutElement>();
            labelLE.flexibleWidth = 1;
            labelLE.minHeight = ColumnHeaderLabelHeight;
            labelLE.preferredHeight = ColumnHeaderLabelHeight;

            // [-] button — only for Research and Manufacturing
            if (assignment != PersonnelAssignment.Unassigned && assignment != PersonnelAssignment.Training)
            {
                CreateHeaderButton(header.transform, "−", () => OnMinusClicked(assignment, level, phoenix));
            }

            // Scroll view body
            var scrollView = new GameObject("ScrollView", typeof(RectTransform));
            scrollView.transform.SetParent(column.transform, false);
            var scrollViewLE = scrollView.AddComponent<LayoutElement>();
            scrollViewLE.flexibleHeight = 1;
            var scrollRect = scrollView.AddComponent<ScrollRect>();

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
            viewport.transform.SetParent(scrollView.transform, false);
            viewport.GetComponent<Mask>().showMaskGraphic = true;
            viewport.GetComponent<Image>().color = ColBodyBg;
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = Vector2.zero;

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;

            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 4f;
            contentLayout.padding = new RectOffset(4, 4, 4, 4);
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            var contentFitter = content.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Attach drop zone handler to the viewport (all columns accept drops;
            // Training slots simply have no drag handler so they can't be dragged OUT)
            var dropZone = viewport.AddComponent<PersonnelColumnDropZone>();
            dropZone.ColumnAssignment = assignment;

            // Populate slots
            var personnelInColumn = Assignments.Values
                .Where(p => p != null && p.Assignment == assignment)
                .OrderBy(p => GetPersonnelName(p))
                .ToList();

            foreach (var person in personnelInColumn)
            {
                CreatePersonnelSlot(content.transform, person, assignment, level, phoenix, slotPrefab, scrollRect);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }

        private static void CreatePersonnelSlot(Transform parent, PersonnelInfo person, PersonnelAssignment column, GeoLevelController level, GeoPhoenixFaction phoenix, SoldierSlotController slotPrefab, ScrollRect parentScrollRect)
        {
            if (person?.Character == null) return;

            GameObject slotGO;
            SoldierSlotController slotController = null;

            if (slotPrefab != null)
            {
                slotController = Object.Instantiate(slotPrefab, parent, false);
                slotGO = slotController.gameObject;
                slotGO.SetActive(true);
                slotController.SetSoldierData((ICommonActor)person.Character);

                // Hide class icon and level number for non-dismissed personnel
                // (civilians have no real class — only dismissed operatives retain theirs)
                if (!PersonnelRestrictions.IsDismissedOperative(person.Character))
                {
                    if (slotController.IconElement != null)
                        slotController.IconElement.gameObject.SetActive(false);
                    if (slotController.LevelLabel != null)
                        slotController.LevelLabel.gameObject.SetActive(false);
                }

                // Normalize the RectTransform so layout works correctly
                RectTransform slotRect = slotGO.GetComponent<RectTransform>();
                if (slotRect != null)
                {
                    slotRect.anchorMin = new Vector2(0, 1);
                    slotRect.anchorMax = new Vector2(1, 1);
                    slotRect.pivot = new Vector2(0.5f, 1);
                }
            }
            else
            {
                // Fallback: create a simple text-based slot
                slotGO = new GameObject($"Slot_{person.Character.DisplayName}", typeof(RectTransform));
                slotGO.transform.SetParent(parent, false);
                slotGO.AddComponent<Image>().color = SlotNormalBg;
                var le = slotGO.AddComponent<LayoutElement>();
                le.minHeight = 48;
                le.preferredHeight = 48;

                var nameGO = new GameObject("NameLabel", typeof(RectTransform));
                nameGO.transform.SetParent(slotGO.transform, false);
                var txt = nameGO.AddComponent<Text>();
                txt.font = PuristaSemibold;
                txt.text = GetPersonnelName(person);
                txt.fontSize = 24;
                txt.color = Color.white;
                txt.alignment = TextAnchor.MiddleLeft;
                txt.horizontalOverflow = HorizontalWrapMode.Overflow;
                var nameRect = nameGO.GetComponent<RectTransform>();
                nameRect.anchorMin = Vector2.zero;
                nameRect.anchorMax = Vector2.one;
                nameRect.offsetMin = new Vector2(8, 0);
                nameRect.offsetMax = new Vector2(-8, 0);
            }

            slotGO.name = $"PersonnelSlot_{person.Id}";

            // Ensure LayoutElement exists for proper sizing
            if (slotGO.GetComponent<LayoutElement>() == null)
            {
                var le = slotGO.AddComponent<LayoutElement>();
                le.minHeight = 48;
                le.preferredHeight = 48;
            }

            // Add assignment display under name for Training column
            if (column == PersonnelAssignment.Training)
            {
                string statusText = GetAssignmentDisplay(person, level);
                var statusGO = new GameObject("StatusLabel", typeof(RectTransform));
                statusGO.transform.SetParent(slotGO.transform, false);
                var statusTxt = statusGO.AddComponent<Text>();
                statusTxt.font = PuristaSemibold;
                statusTxt.text = statusText;
                statusTxt.fontSize = 18;
                statusTxt.color = Color.cyan;
                statusTxt.alignment = TextAnchor.LowerLeft;
                statusTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
                var statusRect = statusGO.GetComponent<RectTransform>();
                statusRect.anchorMin = new Vector2(0, 0);
                statusRect.anchorMax = new Vector2(1, 0.45f);
                statusRect.offsetMin = new Vector2(8, 2);
                statusRect.offsetMax = new Vector2(-8, 0);

                // Make slot taller for training info
                var tlLE = slotGO.GetComponent<LayoutElement>();
                if (tlLE != null) { tlLE.minHeight = 64; tlLE.preferredHeight = 64; }
            }

            // Add Dismissed badge for dismissed operatives in Unassigned column
            if (column == PersonnelAssignment.Unassigned && PersonnelRestrictions.IsDismissedOperative(person.Character))
            {
                var badgeGO = new GameObject("DismissedBadge", typeof(RectTransform));
                badgeGO.transform.SetParent(slotGO.transform, false);
                var badgeTxt = badgeGO.AddComponent<Text>();
                badgeTxt.font = PuristaSemibold;
                badgeTxt.text = "[Dismissed]";
                badgeTxt.fontSize = 18;
                badgeTxt.color = new Color(1f, 0.5f, 0.3f);
                badgeTxt.alignment = TextAnchor.MiddleRight;
                badgeTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
                var badgeRect = badgeGO.GetComponent<RectTransform>();
                badgeRect.anchorMin = new Vector2(0.5f, 0);
                badgeRect.anchorMax = new Vector2(1, 1);
                badgeRect.offsetMin = new Vector2(0, 0);
                badgeRect.offsetMax = new Vector2(-8, 0);
            }

            // Affinity badge — icon in the top-right corner for personnel with an affinity
            AddAffinityBadge(slotGO, person.Character);

            // Ensure there is a background Image for selection highlighting
            Image bgImage = slotGO.GetComponent<Image>();
            if (bgImage == null)
            {
                bgImage = slotGO.AddComponent<Image>();
                bgImage.color = SlotNormalBg;
            }

            // Disable the existing Button click behavior from SoldierSlotController 
            // (we handle selection ourselves via PersonnelSlotSelector)
            Button existingButton = slotGO.GetComponent<Button>();
            if (existingButton != null)
            {
                existingButton.onClick.RemoveAllListeners();
            }
            if (slotController != null)
            {
                slotController.ActorSelected = null;
            }

            // Add our selection component
            var selector = slotGO.AddComponent<PersonnelSlotSelector>();
            selector.PersonnelId = person.Id;
            selector.Column = column;
            selector.BackgroundImage = bgImage;

            // Add drag handler only for non-Training columns
            // (Training personnel cannot be moved back out)
            if (column != PersonnelAssignment.Training)
            {
                var dragHandler = slotGO.AddComponent<PersonnelSlotDragHandler>();
                dragHandler.PersonnelId = person.Id;
                dragHandler.Column = column;
                dragHandler.ParentScrollRect = parentScrollRect;
            }

            // Apply selection visual if already selected
            if (_selectedIds.Contains(person.Id))
            {
                bgImage.color = SlotSelectedBg;
            }
        }

        private static void AddAffinityBadge(GameObject slotGO, GeoCharacter character)
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
                // Raycasts have to reach the badge for the hover tooltip. Clicks and drags still
                // bubble up to the slot, which is where the selector and drag handler live.
                badgeImg.raycastTarget = true;

                var badgeRect = badgeGO.GetComponent<RectTransform>();
                badgeRect.anchorMin = new Vector2(1, 1);
                badgeRect.anchorMax = new Vector2(1, 1);
                badgeRect.pivot = new Vector2(1, 1);
                badgeRect.anchoredPosition = new Vector2(-6, -6);
                badgeRect.sizeDelta = new Vector2(36, 36);

                var tooltip = badgeGO.AddComponent<AffinityBadgeTooltip>();
                tooltip.Approach = approach;
                tooltip.Rank = rank;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void CreateHeaderButton(Transform parent, string caption, Action onClick)
        {
            var go = new GameObject($"Btn_{caption}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = BtnColor;
            img.preserveAspect = true;
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                try { onClick?.Invoke(); } catch (Exception e) { TFTVLogger.Error(e); }
            });
            var le = go.AddComponent<LayoutElement>();
            le.minWidth = ColumnHeaderButtonSize;
            le.preferredWidth = ColumnHeaderButtonSize;
            le.minHeight = ColumnHeaderButtonSize;
            le.preferredHeight = ColumnHeaderButtonSize;

            var txtGO = new GameObject("Text", typeof(RectTransform));
            txtGO.transform.SetParent(go.transform, false);
            var txt = txtGO.AddComponent<Text>();
            txt.font = PuristaSemibold;
            txt.text = caption;
            txt.fontSize = ColumnFontSize + 20;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            var txtRect = txtGO.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;
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
                        GameUtl.GetMessageBox().ShowSimplePrompt(
                            "Living quarters are full.\nBuild or repair a Living Quarters facility to assign more personnel to Research.",
                            MessageBoxIcon.Warning, MessageBoxButtons.OK, null);
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
                        GameUtl.GetMessageBox().ShowSimplePrompt(
                            "Living quarters are full.\nBuild or repair a Living Quarters facility to assign more personnel to Manufacturing.",
                            MessageBoxIcon.Warning, MessageBoxButtons.OK, null);
                        return;
                    }
                    AssignWorker(person, phoenix, FacilitySlotType.Manufacturing);
                    break;

                case PersonnelAssignment.Training:
                    if (currentAssignment == PersonnelAssignment.Unassigned && PersonnelData.IsLivingCapacityFull(phoenix))
                    {
                        GameUtl.GetMessageBox().ShowSimplePrompt(
                            "Living quarters are full.\nBuild or repair a Living Quarters facility to assign more personnel to Training.",
                            MessageBoxIcon.Warning, MessageBoxButtons.OK, null);
                        return;
                    }
                    ShowDeployOrTrainSelection(level, person, phoenix, () => RefreshPanel());
                    return; // Don't refresh yet — modal is open
            }
        }

        #endregion

        #region Selection Logic

        internal static void ToggleSelection(int personnelId, PersonnelAssignment column)
        {
            if (_selectedIds.Contains(personnelId))
            {
                _selectedIds.Remove(personnelId);
            }
            else
            {
                // If selecting from a different column, clear previous selection
                if (_selectedIds.Count > 0 && _selectionSourceColumn != column)
                {
                    _selectedIds.Clear();
                }
                _selectedIds.Add(personnelId);
                _selectionSourceColumn = column;
            }
        }

        internal static bool IsSelected(int personnelId)
        {
            return _selectedIds.Contains(personnelId);
        }

        internal static void ClearSelection()
        {
            _selectedIds.Clear();
        }

        internal static List<PersonnelInfo> GetSelectedPersonnel()
        {
            return _selectedIds
                .Select(id => GetPersonnelByUnitId(id))
                .Where(p => p != null)
                .ToList();
        }

        internal static void HandleDropOnColumn(PersonnelAssignment targetColumn)
        {
            try
            {
                var level = _cachedLevel ?? GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
                var phoenix = level?.PhoenixFaction;
                if (level == null || phoenix == null) return;

                List<PersonnelInfo> selected = GetSelectedPersonnel();
                if (selected.Count == 0) return;

                // Training column with multiple selected — only handle first, open modal
                if (targetColumn == PersonnelAssignment.Training)
                {
                    PersonnelInfo first = selected.FirstOrDefault();
                    _selectedIds.Clear();
                    if (first != null)
                    {
                        // MovePersonnelToColumn opens a modal for Training and returns
                        // without refreshing — don't call RefreshPanel here or the modal
                        // gets destroyed immediately.
                        MovePersonnelToColumn(first, targetColumn, level, phoenix);
                    }
                    return;
                }

                foreach (var person in selected)
                {
                    MovePersonnelToColumn(person, targetColumn, level, phoenix);
                }

                _selectedIds.Clear();
                RefreshPanel();
            }
            catch (Exception e) { TFTVLogger.Error(e); }
        }

        #endregion

        #region Assignment Display
        private static string GetAssignmentDisplay(PersonnelInfo person, GeoLevelController level)
        {
            if (person?.Character == null)
            {
                return person?.Assignment.ToString() ?? "Unknown";
            }

            if (person.Assignment == PersonnelAssignment.Unassigned && PersonnelRestrictions.IsDismissedOperative(person.Character))
            {
                return "Dismissed";
            }

            switch (person.Assignment)
            {
                case PersonnelAssignment.Training:
                    var session = TrainingFacilityRework.GetRecruitSession(person.Character);
                    if (session == null) return "Training (queued)";
                    bool complete = TrainingFacilityRework.IsRecruitTrainingComplete(person.Character, level);
                    double remainingHours =TrainingFacilityRework.GetRecruitRemainingHours(person.Character, level);
                    string specName = person.TrainingSpec?.ViewElementDef.DisplayName1.Localize() ?? person.TrainingSpec?.name ?? "Class";
                    if (complete)
                    {
                        return $"Complete ({specName})";
                    }
                    return $"{specName} (Lv {session.VirtualLevelAchieved}/{session.TargetLevel}, {FormatDuration(remainingHours)})";
                default:
                    return person.Assignment.ToString();
            }
        }
        #endregion

        #region Context Menu
        /// <summary>
        /// Opens the deploy/redeploy base-selection dialog for a personnel slot.
        /// Used by left-click on Training slots and right-click on any slot.
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