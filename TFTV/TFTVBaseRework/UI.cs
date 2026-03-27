using Base.Core;
using Base.Serialization.General;
using HarmonyLib;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static TFTV.TFTVBaseRework.BaseReworkUtils;
using static TFTV.TFTVBaseRework.PersonnelData;
using static TFTV.TFTVBaseRework.TrainingFacilityRework;
using static TFTV.TFTVBaseRework.Workers;
using Object = UnityEngine.Object;

namespace TFTV.TFTVBaseRework
{




    [HarmonyPatch(typeof(UIModuleGeoRosterTabs), nameof(UIModuleGeoRosterTabs.CheckAvailableTabs))]
    public static class UIModuleGeoRosterTabs_CheckAvailableTabs_Patch
    {
        private static void Postfix(UIModuleGeoRosterTabs __instance)
        {
            if (!BaseReworkEnabled)
            {
                return;
            }

            if (__instance?.SoldiersTab == null)
                return;

            string replacement = TFTVCommonMethods.ConvertKeyToString("KEY_FIELD_OPERATIVES");


            Text tmp = __instance.SoldiersTab.GetComponentInChildren<Text>(true);
            if (tmp != null)
            {
                tmp.text = replacement;
                return;
            }

        }
    }

    public static class PersonnelManagementUI
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        private const string PersonnelContainerName = "TFTV_PersonnelContainer";
        private const string LogPrefix = "[PersonnelUI]";

        internal static Font PuristaSemibold = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.PhoenixpediaModule.EntryTitle.font;

        private static GameObject _personnelPanel;
        private static GameObject _modalRoot;
        private static bool _deploymentUIActive;

        // Multi-select state: personnel IDs currently selected.
        private static readonly HashSet<int> _selectedIds = new HashSet<int>();
        // Track which column the selection originated from.
        private static PersonnelAssignment _selectionSourceColumn = PersonnelAssignment.Unassigned;

        // Cached state/level for refresh inside MonoBehaviours.
        private static UIStateRosterRecruits _cachedState;
        private static GeoLevelController _cachedLevel;

        // Colors
        private static readonly Color ColHeaderBg = new Color(0.10f, 0.12f, 0.16f, 0.95f);
        private static readonly Color ColBodyBg = new Color(0.06f, 0.07f, 0.10f, 0.70f);
        private static readonly Color SlotNormalBg = new Color(0.12f, 0.14f, 0.18f, 0.85f);
        private static readonly Color SlotSelectedBg = new Color(0.25f, 0.45f, 0.70f, 0.90f);
        private static readonly Color DropHighlightColor = new Color(0.20f, 0.55f, 0.20f, 0.40f);
        private static readonly Color BtnColor = new Color(0.25f, 0.35f, 0.55f, 0.9f);
        private static readonly Color ToggleOnColor = new Color(0.20f, 0.50f, 0.20f, 0.9f);
        private static readonly Color ToggleOffColor = new Color(0.50f, 0.20f, 0.20f, 0.9f);

        private const float ColumnHeaderHeight = 100f;
        private const float ColumnHeaderIconSize = 75f;
        private const float ColumnHeaderLabelHeight = 75f;
        private const float ColumnHeaderButtonSize = 75;
        private const int ColumnFontSize = 40;

        #region Column Icon Placeholder
        /// <summary>
        /// Placeholder: replace with actual sprite lookup per assignment.
        /// Return null to show a gray square placeholder.
        /// </summary>
        /// 

        private static Sprite _unassignedSprite = null;
        private static Sprite _researchSprite = null;
        private static Sprite _manufacturingSprite = null;
        private static Sprite _deployTrainSprite = null;

        private static Sprite GetColumnIconSprite(PersonnelAssignment assignment)
        {
            // TODO: Return actual sprites here. Example:
            switch (assignment)
            {
                 case PersonnelAssignment.Unassigned: 
                    
                    if(_unassignedSprite != null)
                    {
                       return _unassignedSprite;
                    }
                    else 
                    {
                        _unassignedSprite = Helper.CreateSpriteFromImageFile("personnel.png");
                        return _unassignedSprite;
                    }

                    
                 case PersonnelAssignment.Research:
                    
                    if(_researchSprite != null) 
                    { 
                         return _researchSprite;
                    }
                    else
                    {
                        _researchSprite = DefCache.GetDef<ViewElementDef>("E_ViewElement [ResearchLab_PhoenixFacilityDef]").SmallIcon;
                        return _researchSprite;
                    }

                 case PersonnelAssignment.Manufacturing:
                    if(_manufacturingSprite != null) 
                    { 
                        return _manufacturingSprite;
                    }
                    else
                    {
                        _manufacturingSprite = DefCache.GetDef<ViewElementDef>("E_ViewElement [FabricationPlant_PhoenixFacilityDef]").SmallIcon;
                        return _manufacturingSprite;
                    }

                 case PersonnelAssignment.Training: 
                    if(_deployTrainSprite != null) 
                    
                    { 
                    return _deployTrainSprite;
                    }
                    else
                    {
                        _deployTrainSprite = DefCache.GetDef<ViewElementDef>("E_ViewElement [TrainingFacility_PhoenixFacilityDef]").SmallIcon;
                        return _deployTrainSprite;

                    }

            }
            return null;
        }
        #endregion

        #region Daily Tick
        internal static void DailyTick(GeoLevelController level)
        {
            if (!BaseReworkEnabled)
            {
                return;
            }

            try
            {
                if (level?.PhoenixFaction == null)
                {
                    TFTVLogger.Always($"{LogPrefix} DailyTick skipped: level or PhoenixFaction is null.");
                    return;
                }

                bool opened = TryOpenNextCompletedDeployment(level);

            }
            catch (Exception e) { TFTVLogger.Error(e); }
        }

        private static bool TryOpenNextCompletedDeployment(GeoLevelController level)
        {
            try
            {
                if (level?.PhoenixFaction == null)
                {
                    TFTVLogger.Always($"{LogPrefix} TryOpenNextCompletedDeployment aborted: level or PhoenixFaction is null.");
                    return false;
                }

                if (_deploymentUIActive)
                {
                    TFTVLogger.Always($"{LogPrefix} TryOpenNextCompletedDeployment blocked: deployment UI already active.");
                    return false;
                }

                foreach (PersonnelInfo p in Assignments.Values.OrderBy(x => GetPersonnelName(x)))
                {
                    if (p == null)
                    {
                        TFTVLogger.Always($"{LogPrefix} Candidate skipped: PersonnelInfo is null.");
                        continue;
                    }

                    string name = GetPersonnelName(p);
                    string assignment = p.Assignment.ToString();
                    bool hasCharacter = p.Character != null;


                    if (p.Assignment != PersonnelAssignment.Training)
                    {
                        continue;
                    }

                    if (p.Character == null)
                    {
                        TFTVLogger.Always($"{LogPrefix} Candidate {name} skipped: training assignment but Character is null.");
                        continue;
                    }

                    RecruitTrainingSession session = TrainingFacilityRework.GetRecruitSession(p.Character);
                    bool complete = TrainingFacilityRework.IsRecruitTrainingComplete(p.Character, level);


                    if (complete)
                    {

                        AutoOpenVanillaDeploymentUI(level, level.PhoenixFaction, p);

                        return true;
                    }
                }


            }
            catch (Exception e) { TFTVLogger.Error(e); }

            return false;
        }
        #endregion

        #region Harmony
        [HarmonyPatch(typeof(UIStateRosterRecruits), "EnterState")]
        internal static class UIStateRosterRecruits_EnterState_PersonnelManagement
        {
            private static void Postfix(UIStateRosterRecruits __instance)
            {
                if (!BaseReworkEnabled)
                {
                    return;
                }

                try
                {
                    CreatePersonnelPanel(__instance);
                }
                catch (Exception e) { TFTVLogger.Error(e); }
            }
        }

        [HarmonyPatch(typeof(UIStateRosterRecruits), "ExitState")]
        internal static class UIStateRosterRecruits_ExitState_PersonnelManagement
        {
            private static void Postfix()
            {
                if (!BaseReworkEnabled)
                {
                    return;
                }

                try
                {

                    if (_personnelPanel != null) { Object.Destroy(_personnelPanel); _personnelPanel = null; }
                    CloseModal();
                    _deploymentUIActive = false;
                    _selectedIds.Clear();
                    _cachedState = null;
                    _cachedLevel = null;

                }
                catch (Exception e) { TFTVLogger.Error(e); }
            }
        }



        [HarmonyPatch(typeof(UIStateAssetDeployment), "ExitState")]
        internal static class UIStateAssetDeployment_ExitState_PersonnelManagement
        {
            private static void Postfix()
            {
                if (!BaseReworkEnabled)
                {
                    return;
                }

                try
                {

                    _deploymentUIActive = false;

                    GeoLevelController level = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();

                    bool opened = TryOpenNextCompletedDeployment(level);

                }
                catch (Exception e) { TFTVLogger.Error(e); }
            }
        }


        [HarmonyPatch(typeof(UIModuleRecruitsList), nameof(UIModuleRecruitsList.SetRecruitsList))]
        public static class PersonnelManagementPatch
        {
            public static void Postfix(UIModuleRecruitsList __instance)
            {
                if (!BaseReworkEnabled)
                {
                    return;
                }


                if (__instance == null || __instance.RecruitsListRoot == null) return;
                __instance.NoRecruitsMessage.SetActive(false);
                __instance.NoRecruitsMessageTextBackground.SetActive(false);
                __instance.SpecializationController.gameObject.SetActive(false);
                __instance.InfoController.gameObject.SetActive(false);
            }
        }
        #endregion

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
                SoldierSlotController slotPrefab = ResolveSoldierSlotPrefab();

                // Training slot counts
                int trainProvided = TrainingFacilityRework.GetProvidedTrainingSlots(phoenix);
                int trainUsed = TrainingFacilityRework.GetUsedTrainingSlots();

                // Create 4 columns
                CreateColumn(columnsContainer.transform, PersonnelAssignment.Unassigned, "Unassigned", null, level, phoenix, slotPrefab);
                CreateColumn(columnsContainer.transform, PersonnelAssignment.Research, $"Research ({pools.Research.UsedSlots}/{pools.Research.ProvidedSlots})", pools.Research, level, phoenix, slotPrefab);
                CreateColumn(columnsContainer.transform, PersonnelAssignment.Manufacturing, $"Manufacturing ({pools.Manufacturing.UsedSlots}/{pools.Manufacturing.ProvidedSlots})", pools.Manufacturing, level, phoenix, slotPrefab);
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
            labelText.resizeTextMinSize = ColumnFontSize/2;
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
            txt.fontSize = ColumnFontSize+20;
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
                    // Cannot move training personnel back to Unassigned
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
                    AssignWorker(person, phoenix, FacilitySlotType.Research);
                    break;

                case PersonnelAssignment.Manufacturing:
                    if (!PersonnelRestrictions.CanBeAssignedToManufacturingOrResearch(person.Character))
                    {
                        TFTVLogger.Always($"{LogPrefix} {person.Character?.DisplayName} cannot be assigned to Manufacturing (Just a Grunt).");
                        return;
                    }
                    AssignWorker(person, phoenix, FacilitySlotType.Manufacturing);
                    break;

                case PersonnelAssignment.Training:
                    // Deploy/Train column: prompt with deploy-now vs train-first
                   /* if (PersonnelRestrictions.IsDismissedOperative(person.Character))
                    {
                        ShowMessage($"{person.Character?.DisplayName} is a dismissed operative and cannot use the civilian training path.\nUse Deploy/Redeploy instead.");
                        return;
                    }*/
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

        #region Prefab Resolution

        private static SoldierSlotController ResolveSoldierSlotPrefab()
        {
            SoldierSlotController[] candidates = Resources.FindObjectsOfTypeAll<SoldierSlotController>();
            return candidates
                .Where(c => c != null)
                .OrderBy(c => c.gameObject.scene.IsValid() ? 1 : 0)
                .ThenBy(c => c.gameObject.activeInHierarchy ? 1 : 0)
                .FirstOrDefault();
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
                    int remaining = TrainingFacilityRework.GetRecruitRemainingDays(person.Character, level);
                    string specName = person.TrainingSpec?.ViewElementDef.DisplayName1.Localize() ?? person.TrainingSpec?.name ?? "Class";
                    if (complete)
                    {
                        return $"Complete ({specName})";
                    }
                    return $"{specName} (Lv {session.VirtualLevelAchieved}/{session.TargetLevel}, {remaining}d)";
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

        #region UI Helpers
        private static string GetPersonnelName(PersonnelInfo person)
        {
            return person?.Character?.DisplayName
                   ?? $"Personnel {person?.Id}";
        }

        private static void AddSimpleButton(Transform parent, string caption, Action onClick)
        {
            var go = new GameObject($"Btn_{caption}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.35f, 0.35f, 0.55f, 0.9f);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            var le = go.AddComponent<LayoutElement>();

            var txtGO = new GameObject("Text", typeof(RectTransform));
            txtGO.transform.SetParent(go.transform, false);
            var txt = txtGO.AddComponent<Text>();
            txt.font = PuristaSemibold;
            txt.text = caption;
            txt.fontSize = 30;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Truncate;
            txt.resizeTextForBestFit = true;
            txt.resizeTextMinSize = 18;
            txt.resizeTextMaxSize = 36;

            Canvas.ForceUpdateCanvases();
            float padX = 48f, padY = 24f;
            float w = Mathf.CeilToInt(txt.preferredWidth + padX);
            float h = Mathf.CeilToInt(Mathf.Max(48f, txt.preferredHeight + padY));
            le.minWidth = w;
            le.preferredWidth = w;
            le.minHeight = h;
            le.preferredHeight = h;
        }

        private static void AddModalOptionButton(Transform parent, string caption, Action onClick)
        {
            var go = new GameObject($"Option_{caption}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.25f, 0.45f, 0.9f);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            var le = go.AddComponent<LayoutElement>();

            var txtGO = new GameObject("Text", typeof(RectTransform));
            txtGO.transform.SetParent(go.transform, false);
            var txt = txtGO.AddComponent<Text>();
            txt.font = PuristaSemibold;
            txt.text = caption;
            txt.fontSize = 28;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Truncate;
            txt.resizeTextForBestFit = true;
            txt.resizeTextMinSize = 18;
            txt.resizeTextMaxSize = 36;

            Canvas.ForceUpdateCanvases();
            float padX = 48f, padY = 24f;
            float w = Mathf.CeilToInt(txt.preferredWidth + padX);
            float h = Mathf.CeilToInt(Mathf.Max(48f, txt.preferredHeight + padY));
            le.minWidth = w;
            le.preferredWidth = w;
            le.minHeight = h;
            le.preferredHeight = h;
        }

        private static void AddDisabledLabel(Transform parent, string caption)
        {
            var go = new GameObject($"Disabled_{caption}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.20f, 0.20f, 0.25f, 0.6f);
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 48;
            le.preferredHeight = 48;

            var txtGO = new GameObject("Text", typeof(RectTransform));
            txtGO.transform.SetParent(go.transform, false);
            var txt = txtGO.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = caption;
            txt.fontSize = 24;
            txt.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
            txt.alignment = TextAnchor.MiddleCenter;
            var txtRect = txtGO.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;
        }

        private static void CloseModal()
        {
            if (_modalRoot != null)
            {
                Object.Destroy(_modalRoot);
                _modalRoot = null;
            }
        }

        private static GameObject CreateModalRoot(string name)
        {
            if (_personnelPanel == null) return null;
            var modal = new GameObject(name, typeof(RectTransform));
            modal.transform.SetParent(_personnelPanel.transform, false);
            var canvas = modal.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
            modal.AddComponent<GraphicRaycaster>();
            modal.AddComponent<Image>().color = new Color(0, 0, 0, 0.65f);
            var rect = modal.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.15f, 0.15f);
            rect.anchorMax = new Vector2(0.85f, 0.85f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var layout = modal.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.UpperCenter;

            return modal;
        }

        private static void AddModalHeader(string title)
        {
            if (_modalRoot == null) return;
            var header = new GameObject("Header", typeof(RectTransform));
            header.transform.SetParent(_modalRoot.transform, false);
            var txt = header.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = title;
            txt.fontSize = 42;
            txt.color = Color.yellow;
            txt.alignment = TextAnchor.MiddleCenter;
            header.AddComponent<LayoutElement>().minHeight = 70;
        }

        private static Transform CreateModalContentArea()
        {
            var content = new GameObject("ContentArea", typeof(RectTransform));
            content.transform.SetParent(_modalRoot.transform, false);
            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 15;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandWidth = true;
            content.AddComponent<LayoutElement>().flexibleHeight = 1;
            return content.transform;
        }

        private static void AddModalCloseButton()
        {
            if (_modalRoot == null) return;
            AddSimpleButton(_modalRoot.transform, "Close", () => CloseModal());
        }
        #endregion

        #region Deploy or Train Selection
        private static void ShowDeployOrTrainSelection(GeoLevelController level, PersonnelInfo person, GeoPhoenixFaction phoenix, Action refresh)
        {
            if (level == null || person == null || phoenix == null) return;

            CloseModal();
            _modalRoot = CreateModalRoot("DeployOrTrainModal");
            AddModalHeader($"Choose Action for {GetPersonnelName(person)}");
            var content = CreateModalContentArea();

            var specs = ResolveAvailableMainSpecs(level);

            // Option 1: Deploy Now (immediate class selection + base selection)
            AddModalOptionButton(content, "Deploy Now", () =>
            {
                ShowDeploymentSelection(level, person, phoenix, specs, refresh);
            });

            // Option 2: Train First (only if training slots are available)
            int providedSlots = TrainingFacilityRework.GetProvidedTrainingSlots(phoenix);
            int usedSlots = TrainingFacilityRework.GetUsedTrainingSlots();
            if (usedSlots < providedSlots)
            {
                int duration = TrainingFacilityRework.GetEffectiveDurationDays(phoenix);
                int freeSlots = providedSlots - usedSlots;
                AddModalOptionButton(content, $"Train First ({duration}d, {freeSlots} slot{(freeSlots != 1 ? "s" : "")} free)", () =>
                {
                    ShowTrainingSelection(level, person, specs, refresh);
                });
            }
            else
            {
                AddDisabledLabel(content, "Train First (no training slots available)");
            }

            AddModalCloseButton();
        }
        #endregion

        #region Deployment / Training Selection
        private static void ShowDeploymentSelection(GeoLevelController level, PersonnelInfo person, GeoPhoenixFaction faction, List<SpecializationDef> specs, Action refresh)
        {
            if (level == null || faction == null || person == null) return;

            bool isTraining = person.Assignment == PersonnelAssignment.Training;
            bool trainingComplete = isTraining && TrainingFacilityRework.IsRecruitTrainingComplete(person.Character, level);
            bool isDismissedOperative = PersonnelRestrictions.IsDismissedOperative(person.Character);

            CloseModal();
            _modalRoot = CreateModalRoot("DeploymentSelectionModal");
            AddModalHeader("Select Deployment Base");
            var content = CreateModalContentArea();

            foreach (var baseObj in faction.Bases)
            {
                GeoPhoenixBase geoBase = baseObj.GetComponent<GeoPhoenixBase>();
                string label = baseObj.Site?.Name ?? baseObj.name;
                AddModalOptionButton(content, label, () =>
                {
                    if (isTraining)
                    {
                        Action finalize = () =>
                        {
                            var character = TrainingFacilityRework.FinalizeRecruitTraining(level, person.Character, geoBase, early: !trainingComplete);
                            if (character != null)
                            {
                                PersonnelData.RemovePersonnel(faction, person);
                            }
                            refresh();
                            CloseModal();
                        };

                        if (!trainingComplete)
                        {
                            ShowConfirmation($"Training incomplete for {person.Character?.DisplayName}.\nDeploy early?", finalize, () => CloseModal());
                        }
                        else
                        {
                            finalize();
                        }
                    }
                    else if (isDismissedOperative)
                    {
                        int cost = PersonnelRestrictions.GetRedeployCost(person.Character);
                        if (faction.Skillpoints < cost)
                        {
                            ShowMessage($"Not enough shared skill points to redeploy {person.Character?.DisplayName}.\nRequired: {cost}\nAvailable: {faction.Skillpoints}");
                            return;
                        }

                        ShowConfirmation(
                            $"Redeploy {person.Character?.DisplayName} to {label} for {cost} shared skill points?",
                            () =>
                            {
                                var character = RedeployDismissedOperative(level, person.Character, geoBase);
                                if (character != null)
                                {
                                    RemovePersonnel(faction, person);
                                    RefreshResourceInfo(faction);
                                  
                                }
                                else
                                {
                                    TFTVLogger.Always($"{LogPrefix} Redeploy failed.");
                                }

                                refresh();
                                CloseModal();
                            },
                            () => CloseModal());
                    }
                    else
                    {
                        if (person.TrainingSpec == null)
                        {
                            ShowClassSelectionForImmediateDeploy(level, person, geoBase, specs, refresh);
                        }
                        else
                        {
                            DeployNow(level, person, geoBase, person.TrainingSpec, refresh);
                        }
                    }
                });
            }
            AddModalCloseButton();
        }

        private static void ShowMessage(string message)
        {
            CloseModal();
            _modalRoot = CreateModalRoot("MessageModal");
            AddModalHeader("Notice");
            var content = CreateModalContentArea();

            var msgGO = new GameObject("Message", typeof(RectTransform));
            msgGO.transform.SetParent(content, false);
            var txt = msgGO.AddComponent<Text>();
            txt.font = PuristaSemibold;
            txt.text = message;
            txt.fontSize = 28;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            msgGO.AddComponent<LayoutElement>().minHeight = 120;

            AddModalCloseButton();
        }

        private static void RefreshResourceInfo(GeoPhoenixFaction faction)
        {
            try
            {
                GeoLevelController level = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
                UIModuleInfoBar infoBar = level?.View?.GeoscapeModules?.ResourcesModule;
                MethodInfo update = AccessTools.Method(typeof(UIModuleInfoBar), "UpdateResourceInfo");
                if (faction != null && infoBar != null && update != null)
                {
                    update.Invoke(infoBar, new object[] { faction, false });
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void ShowTrainingSelection(GeoLevelController level, PersonnelInfo person, List<SpecializationDef> specs, Action refresh)
        {
            if (level == null || person == null) return;

            if (PersonnelRestrictions.IsDismissedOperative(person.Character))
            {
                ShowMessage($"{person.Character?.DisplayName} is a dismissed operative and cannot use the civilian training path.\nUse Redeploy instead.");
                return;
            }

            CloseModal();
            _modalRoot = CreateModalRoot("TrainingSelectionModal");
            AddModalHeader("Select Class");
            var content = CreateModalContentArea();

            int providedSlots = TrainingFacilityRework.GetProvidedTrainingSlots(level.PhoenixFaction);
            int usedSlots = TrainingFacilityRework.GetUsedTrainingSlots();
            bool anyFacilityAvailable = providedSlots > usedSlots;

            if (!anyFacilityAvailable)
            {
                AddModalOptionButton(content, "No training facility slot available", () => CloseModal());
            }
            else
            {
                int duration = TrainingFacilityRework.GetEffectiveDurationDays(level.PhoenixFaction);
                foreach (var spec in specs)
                {
                    string label = $"{spec.ViewElementDef.DisplayName1.Localize()} ({duration}d)";
                    AddModalOptionButton(content, label, () =>
                    {
                        if (TrainingFacilityRework.QueueCharacterTrainingAutoFacility(level, person.Character, spec))
                        {
                            AssignPersonnelToTraining(person, level.PhoenixFaction, spec);
                        }
                        else
                        {
                            TFTVLogger.Always($"{LogPrefix} Failed to queue training (no slot or dismissed operative).");
                        }
                        refresh();
                        CloseModal();
                    });
                }
            }
            AddModalCloseButton();
        }

        private static void ShowConfirmation(string message, Action onConfirm, Action onCancel)
        {
            CloseModal();
            _modalRoot = CreateModalRoot("ConfirmationModal");
            AddModalHeader("Confirm");
            var content = CreateModalContentArea();

            var msgGO = new GameObject("Message", typeof(RectTransform));
            msgGO.transform.SetParent(content, false);
            var txt = msgGO.AddComponent<Text>();
            txt.font = PuristaSemibold;
            txt.text = message;
            txt.fontSize = 28;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            msgGO.AddComponent<LayoutElement>().minHeight = 120;

            var buttonsRow = new GameObject("ButtonsRow", typeof(RectTransform));
            buttonsRow.transform.SetParent(content, false);
            var h = buttonsRow.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 30;
            h.childAlignment = TextAnchor.MiddleCenter;

            AddSimpleButton(buttonsRow.transform, "Yes", () => { onConfirm?.Invoke(); });
            AddSimpleButton(buttonsRow.transform, "No", () => { onCancel?.Invoke(); });
        }

        private static void ShowClassSelectionForImmediateDeploy(GeoLevelController level, PersonnelInfo person, GeoPhoenixBase baseObj, List<SpecializationDef> specs, Action refresh)
        {
            CloseModal();
            _modalRoot = CreateModalRoot("ClassSelectionForDeploy");
            AddModalHeader("Select Class");
            var content = CreateModalContentArea();

            foreach (var spec in specs)
            {
                string label = spec.ViewElementDef.DisplayName1.Localize();
                AddModalOptionButton(content, label, () =>
                {
                    DeployNow(level, person, baseObj, spec, refresh);
                });
            }

            AddModalCloseButton();
        }

        private static void DeployNow(GeoLevelController level, PersonnelInfo person, GeoPhoenixBase baseObj, SpecializationDef spec, Action refresh)
        {
            var character = TrainingFacilityRework.PromoteCivilianToOperative(level, person.Character, baseObj, spec);
            if (character != null)
            {
                person.TrainingSpec = spec;
                PersonnelData.RemovePersonnel(level.PhoenixFaction, person);
            }
            else
            {
                TFTVLogger.Always($"{LogPrefix} Immediate deploy failed.");
            }
            refresh();
            CloseModal();
        }

        private static void AutoOpenVanillaDeploymentUI(GeoLevelController level, GeoPhoenixFaction faction, PersonnelInfo person)
        {
            try
            {
                if (level == null || faction == null || person == null)
                {
                    TFTVLogger.Always($"{LogPrefix} AutoOpenVanillaDeploymentUI aborted: level/faction/person null.");
                    return;
                }


                GeoCharacter character = TrainingFacilityRework.FinalizeRecruitTrainingForUI(level, person.Character, early: false);
                if (character == null)
                {
                    TFTVLogger.Always($"{LogPrefix} AutoOpenVanillaDeploymentUI failed: FinalizeRecruitTrainingForUI returned null for {GetPersonnelName(person)}.");
                    return;
                }

                PersonnelData.RemovePersonnel(faction, person);

                CloseModal();

                _deploymentUIActive = true;


                faction.RemoveCharacter(character);


                level.View.PrepareDeployAsset(faction, character, null, null, manufactured: false, spaceFull: false);

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        #endregion
    }

    #region MonoBehaviours

    /// <summary>
    /// Handles click-to-select / click-to-deselect on a personnel slot.
    /// Left-click on Training column slots opens the deploy prompt instead of selecting.
    /// Right-click opens deploy/redeploy on any slot.
    /// </summary>
    internal class PersonnelSlotSelector : MonoBehaviour, IPointerClickHandler
    {
        public int PersonnelId;
        public PersonnelAssignment Column;
        public Image BackgroundImage;

        public void OnPointerClick(PointerEventData eventData)
        {
            try
            {
                if (eventData.button == PointerEventData.InputButton.Left)
                {
                    if (Column == PersonnelAssignment.Training)
                    {
                        // Training slots: left-click opens deploy prompt directly
                        PersonnelInfo person = GetPersonnelByUnitId(PersonnelId);
                        if (person != null)
                        {
                            PersonnelManagementUI.ShowSlotContextMenu(person);
                        }
                    }
                    else
                    {
                        PersonnelManagementUI.ToggleSelection(PersonnelId, Column);
                        UpdateVisual();
                        RefreshSiblingVisuals();
                    }
                }
                else if (eventData.button == PointerEventData.InputButton.Right)
                {
                    // Right-click: open deploy/context menu for this personnel
                    PersonnelInfo person = GetPersonnelByUnitId(PersonnelId);
                    if (person != null)
                    {
                        PersonnelManagementUI.ShowSlotContextMenu(person);
                    }
                }
            }
            catch (Exception e) { TFTVLogger.Error(e); }
        }

        internal void UpdateVisual()
        {
            if (BackgroundImage != null)
            {
                BackgroundImage.color = PersonnelManagementUI.IsSelected(PersonnelId)
                    ? new Color(0.25f, 0.45f, 0.70f, 0.90f)
                    : new Color(0.12f, 0.14f, 0.18f, 0.85f);
            }
        }

        private void RefreshSiblingVisuals()
        {
            Transform parent = transform.parent;
            if (parent == null) return;
            foreach (var selector in parent.GetComponentsInChildren<PersonnelSlotSelector>())
            {
                if (selector != this)
                {
                    selector.UpdateVisual();
                }
            }
        }
    }

    /// <summary>
    /// Handles drag of personnel slots between columns.
    /// Creates a ghost visual during drag, and routes to the drop zone on release.
    /// Not attached to Training column slots (personnel cannot be moved out of training).
    /// </summary>
    internal class PersonnelSlotDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public int PersonnelId;
        public PersonnelAssignment Column;
        public ScrollRect ParentScrollRect;

        private GameObject _ghost;
        private Canvas _ghostCanvas;
        private bool _isDragging;

        public void OnBeginDrag(PointerEventData eventData)
        {
            try
            {
                // If this item is not selected, clear selection and select only this one
                if (!PersonnelManagementUI.IsSelected(PersonnelId))
                {
                    PersonnelManagementUI.ClearSelection();
                    PersonnelManagementUI.ToggleSelection(PersonnelId, Column);
                    // Refresh all visuals
                    var selector = GetComponent<PersonnelSlotSelector>();
                    if (selector != null)
                    {
                        selector.UpdateVisual();
                    }
                }

                // Disable the parent scroll rect so it doesn't interfere
                if (ParentScrollRect != null)
                {
                    ParentScrollRect.enabled = false;
                }

                // Create ghost
                var selectedList = PersonnelManagementUI.GetSelectedPersonnel();
                int count = selectedList.Count;

                _ghost = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup));
                // Parent to the top-level personnel panel
                Transform panelRoot = GetPanelRoot();
                if (panelRoot != null)
                {
                    _ghost.transform.SetParent(panelRoot, false);
                }

                _ghostCanvas = _ghost.GetComponentInParent<Canvas>();

                var ghostRect = _ghost.GetComponent<RectTransform>();
                ghostRect.sizeDelta = new Vector2(200, 40);

                var ghostImg = _ghost.AddComponent<Image>();
                ghostImg.color = new Color(0.25f, 0.45f, 0.70f, 0.75f);
                ghostImg.raycastTarget = false;

                var ghostCG = _ghost.GetComponent<CanvasGroup>();
                ghostCG.blocksRaycasts = false;
                ghostCG.alpha = 0.85f;

                var txtGO = new GameObject("GhostText", typeof(RectTransform));
                txtGO.transform.SetParent(_ghost.transform, false);
                var txt = txtGO.AddComponent<Text>();
                txt.font = PersonnelManagementUI.PuristaSemibold;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = Color.white;
                txt.fontSize = 22;
                txt.raycastTarget = false;
                var txtRect = txtGO.GetComponent<RectTransform>();
                txtRect.anchorMin = Vector2.zero;
                txtRect.anchorMax = Vector2.one;
                txtRect.offsetMin = Vector2.zero;
                txtRect.offsetMax = Vector2.zero;

                if (count == 1)
                {
                    PersonnelInfo single = selectedList.FirstOrDefault();
                    txt.text = single?.Character?.DisplayName ?? $"Personnel {PersonnelId}";
                }
                else
                {
                    txt.text = $"{count} personnel";
                }

                _ghost.transform.position = eventData.position;
                _isDragging = true;
            }
            catch (Exception e) { TFTVLogger.Error(e); }
        }

        public void OnDrag(PointerEventData eventData)
        {
            try
            {
                if (_ghost != null && _isDragging)
                {
                    _ghost.transform.position = eventData.position;
                }
            }
            catch (Exception e) { TFTVLogger.Error(e); }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            try
            {
                _isDragging = false;

                if (_ghost != null)
                {
                    Object.Destroy(_ghost);
                    _ghost = null;
                }

                // Re-enable parent scroll rect
                if (ParentScrollRect != null)
                {
                    ParentScrollRect.enabled = true;
                }

                // Check what we dropped on
                // Use eventData.pointerCurrentRaycast to find the drop target
                GameObject hitObject = eventData.pointerCurrentRaycast.gameObject;
                if (hitObject != null)
                {
                    PersonnelColumnDropZone dropZone = hitObject.GetComponentInParent<PersonnelColumnDropZone>();
                    if (dropZone != null)
                    {
                        PersonnelManagementUI.HandleDropOnColumn(dropZone.ColumnAssignment);
                        return;
                    }
                }

                // Dropped outside any column — just deselect
                PersonnelManagementUI.ClearSelection();
            }
            catch (Exception e) { TFTVLogger.Error(e); }
        }

        private Transform GetPanelRoot()
        {
            // Walk up to find the TFTV_PersonnelContainer
            Transform current = transform;
            while (current != null)
            {
                if (current.name == "TFTV_PersonnelContainer")
                {
                    return current;
                }
                current = current.parent;
            }
            return transform.root;
        }
    }

    /// <summary>
    /// Attached to each column's viewport (except Training). Receives drops and highlights during drag-over.
    /// </summary>
    internal class PersonnelColumnDropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public PersonnelAssignment ColumnAssignment;
        private Image _image;
        private Color _originalColor;
        private bool _highlighted;

        private void Awake()
        {
            _image = GetComponent<Image>();
            if (_image != null)
            {
                _originalColor = _image.color;
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            try
            {
                ClearHighlight();
                PersonnelManagementUI.HandleDropOnColumn(ColumnAssignment);
            }
            catch (Exception e) { TFTVLogger.Error(e); }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Only highlight when something is being dragged
            if (eventData.dragging && _image != null && !_highlighted)
            {
                _highlighted = true;
                _image.color = new Color(
                    _originalColor.r + 0.10f,
                    _originalColor.g + 0.15f,
                    _originalColor.b + 0.05f,
                    Mathf.Min(1f, _originalColor.a + 0.20f));
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ClearHighlight();
        }

        private void ClearHighlight()
        {
            if (_highlighted && _image != null)
            {
                _image.color = _originalColor;
                _highlighted = false;
            }
        }
    }

    #endregion
}