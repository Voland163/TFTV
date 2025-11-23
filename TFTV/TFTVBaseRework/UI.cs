using Base.Core;
using Base.Serialization.General;
using HarmonyLib;
using PhoenixPoint.Common.Entities;
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
using UnityEngine;
using UnityEngine.UI;
using static TFTV.TFTVBaseRework.Workers;
using Object = UnityEngine.Object;

namespace TFTV.TFTVBaseRework
{
    public enum PersonnelAssignment
    {
        Unassigned,
        Research,
        Manufacturing,
        Training
    }

    internal class PersonnelInfo
    {
        public GeoUnitDescriptor Descriptor;
        public PersonnelAssignment Assignment;
        public SpecializationDef TrainingSpec;
        public GeoCharacter CreatedCharacter;
        public GeoPhoenixFacility TrainingFacility;
        public bool TrainingCompleteNotDeployed;
        public bool DeploymentUIOpened;
    }

    public static class PersonnelManagementUI
    {
        private const string PersonnelContainerName = "TFTV_PersonnelContainer";
        private const string LogPrefix = "[PersonnelUI]";

        // Mapping of naked recruit descriptors (vanilla) to our assignment metadata.
        private static readonly Dictionary<GeoUnitDescriptor, PersonnelInfo> _assignments = new Dictionary<GeoUnitDescriptor, PersonnelInfo>();

        private static GameObject _personnelPanel;
        private static GameObject _modalRoot;
        private static bool _deploymentUIActive;

        private static readonly Dictionary<GeoUnitDescriptor, Guid> _descriptorIdCache = new Dictionary<GeoUnitDescriptor, Guid>();

        // ADDED: prevent UI rebuild recursion while we build the panel
        private static bool _isBuildingUI;

        internal static IEnumerable<PersonnelInfo> CurrentPersonnel => _assignments.Values;

        internal static void ResetDescriptorIdCache()
        {
            _descriptorIdCache.Clear();
        }

        internal static void ClearAssignments()
        {
            try
            {
                _assignments.Clear();
                ResetDescriptorIdCache();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static Guid GetOrCreateDescriptorId(GeoUnitDescriptor descriptor)
        {
            if (descriptor == null)
            {
                return Guid.Empty;
            }

            if (!_descriptorIdCache.TryGetValue(descriptor, out Guid id))
            {
                id = Guid.NewGuid();
                _descriptorIdCache[descriptor] = id;
            }

            return id;
        }

        #region Sync From Vanilla Naked Recruits
        internal static void SyncFromNakedRecruits(GeoPhoenixFaction phoenix)
        {
            try
            {
                if (phoenix == null) return;

                // Add new descriptors.
                foreach (var kv in phoenix.NakedRecruits)
                {
                    if (kv.Key == null) continue;
                    if (!_assignments.ContainsKey(kv.Key))
                    {
                        _assignments[kv.Key] = new PersonnelInfo
                        {
                            Descriptor = kv.Key,
                            Assignment = PersonnelAssignment.Unassigned
                        };
                        TFTVLogger.Always($"{LogPrefix} Added naked recruit to personnel mapping: {kv.Key.GetName()}");
                    }
                }

                // Remove those no longer present.
                var toRemove = _assignments.Keys.Where(d => !phoenix.NakedRecruits.ContainsKey(d)).ToList();
                foreach (var d in toRemove)
                {
                    _assignments.Remove(d);
                    TFTVLogger.Always($"{LogPrefix} Removed personnel mapping (descriptor no longer in NakedRecruits): {d.GetName()}");
                }

                // Rebuild panel if open, but only when not currently building it.
                if (_personnelPanel != null && !_isBuildingUI)
                {
                    if (GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>()
                        ?.View?.CurrentViewState is UIStateRosterRecruits state)
                    {
                        Object.Destroy(_personnelPanel);
                        _personnelPanel = null;
                        CreatePersonnelPanel(state);
                    }
                }
            }
            catch (Exception e) { TFTVLogger.Error(e); }
        }
        #endregion

        #region Daily Tick (training completion only)
        internal static void DailyTick(GeoLevelController level)
        {
            try
            {
                if (level?.PhoenixFaction == null) return;

                if (!_deploymentUIActive)
                {
                    foreach (var p in _assignments.Values)
                    {
                        if (p.Assignment == PersonnelAssignment.Training &&
                            p.Descriptor != null &&
                            !p.DeploymentUIOpened &&
                            TrainingFacilityRework.IsRecruitTrainingComplete(p.Descriptor, level))
                        {
                            p.TrainingCompleteNotDeployed = true;
                            AutoOpenVanillaDeploymentUI(level, level.PhoenixFaction, p);
                            break;
                        }
                    }
                }
            }
            catch (Exception e) { TFTVLogger.Error(e); }
        }
        #endregion

        #region Harmony Patches
        [HarmonyPatch(typeof(GeoLevelController), "DailyUpdate")]
        internal static class GeoLevelController_DailyUpdate_PersonnelPool
        {
            private static void Postfix(GeoLevelController __instance) => DailyTick(__instance);
        }

        // Sync whenever vanilla regenerates naked recruits.
        [HarmonyPatch(typeof(GeoPhoenixFaction), nameof(GeoPhoenixFaction.RegenerateNakedRecruits))]
        internal static class GeoPhoenixFaction_RegenerateNakedRecruits_PersonnelSync
        {
            private static void Postfix(GeoPhoenixFaction __instance)
            {
                try { SyncFromNakedRecruits(__instance); }
                catch (Exception e) { TFTVLogger.Error(e); }
            }
        }

        [HarmonyPatch(typeof(UIStateRosterRecruits), "EnterState")]
        internal static class UIStateRosterRecruits_EnterState_PersonnelManagement
        {
            private static void Postfix(UIStateRosterRecruits __instance)
            {
                try { CreatePersonnelPanel(__instance); }
                catch (Exception e) { TFTVLogger.Error(e); }
            }
        }

        [HarmonyPatch(typeof(UIStateRosterRecruits), "ExitState")]
        internal static class UIStateRosterRecruits_ExitState_PersonnelManagement
        {
            private static void Postfix()
            {
                try
                {
                    if (_personnelPanel != null) { Object.Destroy(_personnelPanel); _personnelPanel = null; }
                    CloseModal();
                    // ADDED: ensure we don't keep this latched after leaving the screen
                    _deploymentUIActive = false;
                }
                catch (Exception e) { TFTVLogger.Error(e); }
            }
        }

        [HarmonyPatch(typeof(UIModuleRecruitsList), nameof(UIModuleRecruitsList.SetRecruitsList))]
        public static class PersonnelManagementPatch
        {
            public static void Postfix(UIModuleRecruitsList __instance)
            {
                if (__instance == null || __instance.RecruitsListRoot == null) return;
                __instance.NoRecruitsMessage.SetActive(false);
                __instance.NoRecruitsMessageTextBackground.SetActive(false);
                __instance.SpecializationController.gameObject.SetActive(false);
                __instance.InfoController.gameObject.SetActive(false);
            }
        }
        #endregion

        #region Panel Construction
        private static void CreatePersonnelPanel(UIStateRosterRecruits state)
        {
            var level = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
            var recruitsModule = level?.View?.GeoscapeModules?.RecruitsListModule;
            if (recruitsModule == null) return;

            _isBuildingUI = true;
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

                var scrollView = new GameObject("PersonnelScrollView", typeof(RectTransform));
                scrollView.transform.SetParent(_personnelPanel.transform, false);
                var scrollRect = scrollView.AddComponent<ScrollRect>();
                var scrollRectTransform = scrollView.GetComponent<RectTransform>();
                scrollRectTransform.anchorMin = Vector2.zero;
                scrollRectTransform.anchorMax = Vector2.one;
                scrollRectTransform.offsetMin = Vector2.zero;
                scrollRectTransform.offsetMax = Vector2.zero;

                var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
                viewport.transform.SetParent(scrollView.transform, false);
                viewport.GetComponent<Mask>().showMaskGraphic = false;
                viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);
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

                var layout = content.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 10f;
                layout.padding = new RectOffset(10, 10, 10, 10);
                layout.childControlWidth = true;
                layout.childForceExpandWidth = true;

                var fitter = content.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                PopulatePersonnelUI(state, level, content.transform);
            }
            finally
            {
                _isBuildingUI = false;
            }
        }

        private static void PopulatePersonnelUI(UIStateRosterRecruits state, GeoLevelController level, Transform personnelRoot)
        {
            if (personnelRoot == null || level?.PhoenixFaction == null) return;
            ClearChildren(personnelRoot);

            var phoenix = level.PhoenixFaction;
            // Ensure sync in case UI opened before regeneration event patch executed.
            SyncFromNakedRecruits(phoenix);

            Action refresh = () =>
            {
                if (_personnelPanel != null) { Object.Destroy(_personnelPanel); _personnelPanel = null; }
                CreatePersonnelPanel(state);
            };

            foreach (var info in CurrentPersonnel.OrderBy(p => p.Descriptor.GetName()))
            {
                CreatePersonnelCard(personnelRoot, info, level, phoenix, refresh);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(personnelRoot.GetComponent<RectTransform>());
        }

        private static void ClearChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(root.GetChild(i).gameObject);
            }
        }
        #endregion

        #region Card
        private static void CreatePersonnelCard(Transform parent, PersonnelInfo person, GeoLevelController level, GeoPhoenixFaction phoenix, Action refresh)
        {
            var card = new GameObject($"Personnel_{person.Descriptor.GetName()}", typeof(RectTransform));
            card.transform.SetParent(parent, false);
            card.AddComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f, 0.85f);

            var hLayout = card.AddComponent<HorizontalLayoutGroup>();
            hLayout.padding = new RectOffset(10, 10, 10, 10);
            hLayout.spacing = 25;
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.childForceExpandHeight = false;
            hLayout.childForceExpandWidth = false;

            var nameText = AddLabel(card.transform, person.Descriptor.GetName(), 40, Color.white);
            nameText.gameObject.AddComponent<LayoutElement>().minWidth = 260;

            var assignmentText = AddLabel(card.transform, GetAssignmentDisplay(person, level), 32, Color.cyan);
            assignmentText.gameObject.AddComponent<LayoutElement>().minWidth = 260;

            var actionsRow = new GameObject("ActionsRow", typeof(RectTransform));
            actionsRow.transform.SetParent(card.transform, false);
            var actionsLayout = actionsRow.AddComponent<HorizontalLayoutGroup>();
            actionsLayout.spacing = 10;
            actionsLayout.childAlignment = TextAnchor.MiddleLeft;
            var actionsLE = actionsRow.AddComponent<LayoutElement>();
            actionsLE.flexibleWidth = 1;

            var specs = ResolveAvailableMainSpecs(level);

            AddActionButton(actionsRow.transform, "Research", () =>
            {
                AssignWorker(person, phoenix, FacilitySlotType.Research);
                refresh();
            });

            AddActionButton(actionsRow.transform, "Manufacturing", () =>
            {
                AssignWorker(person, phoenix, FacilitySlotType.Manufacturing);
                refresh();
            });

            AddActionButton(actionsRow.transform, "Training", () =>
            {
                ShowTrainingSelection(level, person, specs, refresh);
            });

            AddActionButton(actionsRow.transform, "Deploy", () =>
            {
                ShowDeploymentSelection(level, person, phoenix, specs, refresh);
            });
        }

        private static string GetAssignmentDisplay(PersonnelInfo person, GeoLevelController level)
        {
            switch (person.Assignment)
            {
                case PersonnelAssignment.Training:
                    var session = TrainingFacilityRework.GetRecruitSession(person.Descriptor);
                    if (session == null) return "Training (queued)";
                    bool complete = TrainingFacilityRework.IsRecruitTrainingComplete(person.Descriptor, level);
                    int remaining = TrainingFacilityRework.GetRecruitRemainingDays(person.Descriptor, level);
                    string specName = person.TrainingSpec?.ViewElementDef.DisplayName1.Localize() ?? person.TrainingSpec?.name ?? "Class";
                    if (complete)
                    {
                        return $"Training Complete ({specName})";
                    }
                    return $"Training: {specName} (Lv {session.VirtualLevelAchieved}/{session.TargetLevel}, {remaining}d left)";
                default:
                    return person.Assignment.ToString();
            }
        }
        #endregion

        #region UI Helpers
        private static Text AddLabel(Transform parent, string text, int size, Color color)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAnchor.MiddleLeft;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            return t;
        }

        private static void AddActionButton(Transform parent, string caption, Action onClick)
        {
            var go = new GameObject($"Btn_{caption}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.35f, 0.55f, 0.9f);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            var le = go.AddComponent<LayoutElement>();

            var txtGO = new GameObject("Text", typeof(RectTransform));
            txtGO.transform.SetParent(go.transform, false);
            var txt = txtGO.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = caption;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Truncate;
            txt.resizeTextForBestFit = true;
            txt.resizeTextMinSize = 18;
            txt.resizeTextMaxSize = 36;

            Canvas.ForceUpdateCanvases();
            float padX = 48f, padY = 24f;
            float w = Mathf.CeilToInt(txt.preferredWidth + padX);
            float h = Mathf.CeilToInt(Mathf.Max(40f, txt.preferredHeight + padY));
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
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
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
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
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

        #region Training Selection & Deployment
        private static void ShowTrainingSelection(GeoLevelController level, PersonnelInfo person, List<SpecializationDef> specs, Action refresh)
        {
            if (level == null || person == null) return;
            CloseModal();
            _modalRoot = CreateModalRoot("TrainingSelectionModal");
            AddModalHeader("Select Class");
            var content = CreateModalContentArea();

            bool anyFacilityAvailable = level.PhoenixFaction.Bases
                .SelectMany(b => b.Layout.Facilities)
                .Any(f => TrainingFacilityRework.IsValidFacility(f));

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
                        if (!TrainingFacilityRework.OverrideDescriptorMainSpec(person.Descriptor, spec, rebuildPersonalAbilities: true))
                        {
                            TFTVLogger.Always($"{LogPrefix} Failed to override descriptor spec for training.");
                            refresh();
                            CloseModal();
                            return;
                        }

                        if (TrainingFacilityRework.QueueDescriptorTrainingAutoFacility(level, person.Descriptor, spec))
                        {
                            person.Assignment = PersonnelAssignment.Training;
                            person.TrainingSpec = spec;
                            var session = TrainingFacilityRework.GetRecruitSession(person.Descriptor);
                            if (session != null)
                            {
                                var facilityProp = session.GetType().GetProperty("Facility");
                                person.TrainingFacility = (GeoPhoenixFacility)facilityProp?.GetValue(session);
                            }
                        }
                        else
                        {
                            TFTVLogger.Always($"{LogPrefix} Failed to queue training (no slot?).");
                        }
                        refresh();
                        CloseModal();
                    });
                }
            }
            AddModalCloseButton();
        }

        private static void ShowDeploymentSelection(GeoLevelController level, PersonnelInfo person, GeoPhoenixFaction faction, List<SpecializationDef> specs, Action refresh)
        {
            if (level == null || faction == null || person == null) return;

            bool isTraining = person.Assignment == PersonnelAssignment.Training;
            bool trainingComplete = isTraining && TrainingFacilityRework.IsRecruitTrainingComplete(person.Descriptor, level);

            CloseModal();
            _modalRoot = CreateModalRoot("DeploymentSelectionModal");
            AddModalHeader("Select Deployment Base");
            var content = CreateModalContentArea();

            foreach (var baseObj in faction.Bases)
            {
                string label = baseObj.Site?.Name ?? baseObj.name;
                AddModalOptionButton(content, label, () =>
                {
                    if (isTraining)
                    {
                        Action finalize = () =>
                        {
                            var character = TrainingFacilityRework.FinalizeRecruitTraining(level, person.Descriptor, baseObj.GetComponent<GeoPhoenixBase>(), early: !trainingComplete);
                            if (character != null)
                            {
                                person.CreatedCharacter = character;
                                RemoveDescriptorFromNakedPool(faction, person.Descriptor);
                            }
                            refresh();
                            CloseModal();
                        };

                        if (!trainingComplete)
                        {
                            ShowConfirmation($"Training incomplete for {person.Descriptor.GetName()}.\nDeploy early?", finalize, () => CloseModal());
                        }
                        else
                        {
                            finalize();
                        }
                    }
                    else
                    {
                        if (person.TrainingSpec == null)
                        {
                            ShowClassSelectionForImmediateDeploy(level, person, baseObj.GetComponent<GeoPhoenixBase>(), specs, refresh);
                        }
                        else
                        {
                            DeployNow(level, person, baseObj.GetComponent<GeoPhoenixBase>(), person.TrainingSpec, refresh);
                        }
                    }
                });
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
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
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
            if (!TrainingFacilityRework.OverrideDescriptorMainSpec(person.Descriptor, spec, rebuildPersonalAbilities: true))
            {
                TFTVLogger.Always($"{LogPrefix} Override main spec failed before deploy.");
            }
            var character = TrainingFacilityRework.CreateOperativeFromDescriptor(level, person.Descriptor, baseObj, spec);
            if (character != null)
            {
                person.CreatedCharacter = character;
                person.TrainingSpec = spec;
                RemoveDescriptorFromNakedPool(level.PhoenixFaction, person.Descriptor);
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
                if (level == null || faction == null || person == null || person.DeploymentUIOpened) return;

                GeoCharacter character = TrainingFacilityRework.FinalizeRecruitTrainingForUI(level, person.Descriptor, early: false);
                if (character == null) return;

                person.CreatedCharacter = character;
                person.DeploymentUIOpened = true;
                RemoveDescriptorFromNakedPool(faction, person.Descriptor);

                CloseModal();

                _deploymentUIActive = true;
                level.View.PrepareDeployAsset(faction, character, null, null, manufactured: false, spaceFull: false);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void RemoveDescriptorFromNakedPool(GeoPhoenixFaction faction, GeoUnitDescriptor descriptor)
        {
            try
            {
                if (faction?.NakedRecruits.ContainsKey(descriptor) == true)
                {
                    faction.NakedRecruits.Remove(descriptor);
                }
                _assignments.Remove(descriptor);
            }
            catch (Exception e) { TFTVLogger.Error(e); }
        }
        #endregion

        #region Logic Actions
        private static List<SpecializationDef> ResolveAvailableMainSpecs(GeoLevelController level)
        {
            var faction = level?.PhoenixFaction;
            if (faction == null) return new List<SpecializationDef>();
            return TrainingFacilityRework.GetAvailableTrainingSpecializations(faction).ToList();
        }

        private static void AssignWorker(PersonnelInfo person, GeoPhoenixFaction faction, FacilitySlotType slotType)
        {
            if (person?.Descriptor == null || faction == null) return;
            ResearchManufacturingSlotsManager.RecalculateSlots(faction);

            PersonnelAssignment desired = slotType == FacilitySlotType.Research
                ? PersonnelAssignment.Research
                : PersonnelAssignment.Manufacturing;

            if (person.Assignment == desired) return;
            var previous = person.Assignment;

            bool slotAdded = ResearchManufacturingSlotsManager.IncrementUsedSlot(faction, slotType);
            if (!slotAdded)
            {
                TFTVLogger.Always($"{LogPrefix} No free {slotType} slots available (used >= provided).");
                return;
            }

            ReleaseWorkSlotIfNeeded(faction, previous);
            person.Assignment = desired;

            GeoLevelController level = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
            UIModuleInfoBar infoBar = level.View.GeoscapeModules.ResourcesModule;
            var update = AccessTools.Method(typeof(UIModuleInfoBar), "UpdateResourceInfo");
            update.Invoke(infoBar, new object[] { faction, false });
        }

        private static void ReleaseWorkSlotIfNeeded(GeoPhoenixFaction faction, PersonnelAssignment assignment)
        {
            if (faction == null) return;
            switch (assignment)
            {
                case PersonnelAssignment.Research:
                    ResearchManufacturingSlotsManager.DecrementUsedSlot(faction, FacilitySlotType.Research);
                    break;
                case PersonnelAssignment.Manufacturing:
                    ResearchManufacturingSlotsManager.DecrementUsedSlot(faction, FacilitySlotType.Manufacturing);
                    break;
            }
        }
        #endregion

        #region Persistence (Assignments Only)
        [SerializeType(SerializeMembersByDefault = SerializeMembersType.SerializeAll)]
        public sealed class PersonnelAssignmentSave
        {
            public Guid DescriptorId;
            [NonSerialized]
            public GeoUnitDescriptor Descriptor;
            public string DescriptorName;
            public string IdentityName;
            public GeoCharacterSex IdentitySex;
            public string MainSpecName;
            public PersonnelAssignment Assignment;
            public bool TrainingCompleteNotDeployed;
            public bool DeploymentUIOpened;
        }

        internal static List<PersonnelAssignmentSave> CreateAssignmentsSnapshot()
        {
            var list = new List<PersonnelAssignmentSave>();
            foreach (var pi in _assignments.Values)
            {
                if (pi.Descriptor == null) continue;
                list.Add(new PersonnelAssignmentSave
                {
                    DescriptorId = GetOrCreateDescriptorId(pi.Descriptor),
                    Descriptor = pi.Descriptor,
                    DescriptorName = pi.Descriptor.GetName(),
                    IdentityName = pi.Descriptor.Identity?.Name,
                    IdentitySex = pi.Descriptor.Identity?.Sex ?? GeoCharacterSex.None,
                    MainSpecName = pi.TrainingSpec?.name ?? pi.Descriptor.Progression?.MainSpecDef?.name,
                    Assignment = pi.Assignment,
                    TrainingCompleteNotDeployed = pi.TrainingCompleteNotDeployed,
                    DeploymentUIOpened = pi.DeploymentUIOpened
                });
            }
            return list;
        }

        private static GeoUnitDescriptor ResolveDescriptorFromSave(GeoLevelController level, PersonnelAssignmentSave save)
        {
            if (save == null)
            {
                return null;
            }

            if (save.Descriptor != null)
            {
                return save.Descriptor;
            }

            try
            {
                var generator = level?.CharacterGenerator;
                var phoenix = level?.PhoenixFaction;
                var descriptor = generator?.GenerateRandomUnit(generator.GenerateCharacterGeneratorContext(phoenix));

                if (descriptor == null)
                {
                    return null;
                }

                if (!string.IsNullOrEmpty(save.IdentityName))
                {
                    descriptor.Identity = new GeoUnitDescriptor.IdentityDescriptor(save.IdentityName, save.IdentitySex);
                }

                SpecializationDef spec = null;
                if (!string.IsNullOrEmpty(save.MainSpecName))
                {
                    try { spec = TFTVMain.Main.DefCache.GetDef<SpecializationDef>(save.MainSpecName); } catch { }
                }

                if (spec != null)
                {
                    TrainingFacilityRework.OverrideDescriptorMainSpec(descriptor, spec, rebuildPersonalAbilities: true);
                }

                return descriptor;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return null;
            }
        }

        private static void TryAddDescriptorToPool(object nakedRecruits, GeoUnitDescriptor descriptor, Type valueType, Func<object> valueFactory)
        {
            try
            {
                if (nakedRecruits == null || descriptor == null || valueType == null)
                {
                    return;
                }

                var poolType = nakedRecruits.GetType();
                var contains = poolType.GetMethod("ContainsKey", new[] { typeof(GeoUnitDescriptor) });
                if (contains != null && (bool)contains.Invoke(nakedRecruits, new object[] { descriptor }))
                {
                    return;
                }

                object value = valueFactory();
                var addMethod = poolType.GetMethod("Add", new[] { typeof(GeoUnitDescriptor), valueType });
                if (addMethod != null)
                {
                    addMethod.Invoke(nakedRecruits, new object[] { descriptor, value });
                }
                else if (nakedRecruits is IDictionary dict)
                {
                    dict[descriptor] = value;
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static Dictionary<Guid, GeoUnitDescriptor> RestoreNakedRecruitPool(GeoLevelController level, IEnumerable<PersonnelAssignmentSave> snapshot)
        {
            var map = new Dictionary<Guid, GeoUnitDescriptor>();

            try
            {
                if (level?.PhoenixFaction == null || snapshot == null)
                {
                    return map;
                }

                var phoenix = level.PhoenixFaction;
                var nakedRecruits = phoenix.NakedRecruits;
                if (nakedRecruits == null)
                {
                    return map;
                }

                var poolType = nakedRecruits.GetType();
                var genericArgs = poolType.GetGenericArguments();
                Type valueType = genericArgs.Length > 1 ? genericArgs[1] : typeof(object);

                List<object> sampleValues = new List<object>();
                if (nakedRecruits is IDictionary dict)
                {
                    foreach (DictionaryEntry entry in dict)
                    {
                        sampleValues.Add(entry.Value);
                    }
                }

                poolType.GetMethod("Clear", Type.EmptyTypes)?.Invoke(nakedRecruits, null);

                Func<object> valueFactory = () =>
                {
                    foreach (object sample in sampleValues)
                    {
                        if (sample == null)
                        {
                            continue;
                        }

                        try
                        {
                            return Activator.CreateInstance(sample.GetType());
                        }
                        catch
                        {
                            return sample;
                        }
                    }

                    if (valueType.IsValueType)
                    {
                        try { return Activator.CreateInstance(valueType); } catch { }
                    }

                    return null;
                };

                foreach (var save in snapshot)
                {
                    var descriptor = ResolveDescriptorFromSave(level, save);
                    if (descriptor == null)
                    {
                        continue;
                    }

                    TryAddDescriptorToPool(nakedRecruits, descriptor, valueType, valueFactory);

                    if (save.DescriptorId != Guid.Empty && !map.ContainsKey(save.DescriptorId))
                    {
                        map.Add(save.DescriptorId, descriptor);
                    }
                }

                SyncFromNakedRecruits(phoenix);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

            return map;
        }

        internal static void EnsureDescriptorInPool(GeoPhoenixFaction faction, GeoUnitDescriptor descriptor)
        {
            try
            {
                if (faction == null || descriptor == null)
                {
                    return;
                }

                var nakedRecruits = faction.NakedRecruits;
                var poolType = nakedRecruits.GetType();
                var genericArgs = poolType.GetGenericArguments();
                Type valueType = genericArgs.Length > 1 ? genericArgs[1] : typeof(object);
                Func<object> valueFactory = () => valueType.IsValueType ? Activator.CreateInstance(valueType) : null;

                TryAddDescriptorToPool(nakedRecruits, descriptor, valueType, valueFactory);
                SyncFromNakedRecruits(faction);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void LoadAssignmentsSnapshot(GeoLevelController level, IEnumerable<PersonnelAssignmentSave> snapshot, IDictionary<Guid, GeoUnitDescriptor> descriptorMap = null)
        {
            try
            {
                if (level?.PhoenixFaction == null || snapshot == null) return;
                var phoenix = level.PhoenixFaction;
                SyncFromNakedRecruits(phoenix);

                foreach (var save in snapshot)
                {
                    GeoUnitDescriptor entry = null;
                    if (descriptorMap != null && save.DescriptorId != Guid.Empty)
                    {
                        descriptorMap.TryGetValue(save.DescriptorId, out entry);
                    }

                    if (entry == null)
                    {
                        entry = _assignments.Keys.FirstOrDefault(d => d.GetName() == save.DescriptorName &&
                                                                      (d.Identity?.Name == save.IdentityName || string.IsNullOrEmpty(save.IdentityName)));
                    }
                    if (entry == null) continue;
                    var info = _assignments[entry];
                    info.Assignment = save.Assignment;
                    info.TrainingCompleteNotDeployed = save.TrainingCompleteNotDeployed;
                    info.DeploymentUIOpened = save.DeploymentUIOpened;

                    if (!string.IsNullOrEmpty(save.MainSpecName))
                    {
                        try
                        {
                            var spec = TFTVMain.Main.DefCache.GetDef<SpecializationDef>(save.MainSpecName);
                            if (spec != null) info.TrainingSpec = spec;
                        }
                        catch { }
                    } 
                }

                ResearchManufacturingSlotsManager.RecalculateSlots(phoenix);
                ResearchManufacturingSlotsManager.SetUsedSlots(phoenix, FacilitySlotType.Research,
                    _assignments.Values.Count(pi => pi.Assignment == PersonnelAssignment.Research));
                ResearchManufacturingSlotsManager.SetUsedSlots(phoenix, FacilitySlotType.Manufacturing,
                    _assignments.Values.Count(pi => pi.Assignment == PersonnelAssignment.Manufacturing));

            }
            catch (Exception e) { TFTVLogger.Error(e); }
        }
        #endregion
    }
}