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
using System.Reflection;
using UnityEngine;
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
        private const string PersonnelContainerName = "TFTV_PersonnelContainer";
        private const string LogPrefix = "[PersonnelUI]";

        private static GameObject _personnelPanel;
        private static GameObject _modalRoot;
        private static bool _deploymentUIActive;

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
        private static void CreatePersonnelPanel(UIStateRosterRecruits state)
        {
            if (!BaseReworkEnabled)
            {
                return;
            }

            var level = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
            var recruitsModule = level?.View?.GeoscapeModules?.RecruitsListModule;
            if (recruitsModule == null) return;


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
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
        
        private static void PopulatePersonnelUI(UIStateRosterRecruits state, GeoLevelController level, Transform personnelRoot)
        {
            if (!BaseReworkEnabled)
            {
                return;
            }
            

            if (personnelRoot == null || level?.PhoenixFaction == null) return;
            ClearTransformChildren(personnelRoot);

            var phoenix = level.PhoenixFaction;

            Action refresh = () =>
            {
                if (_personnelPanel != null) { Object.Destroy(_personnelPanel); _personnelPanel = null; }
                CreatePersonnelPanel(state);
            };

            foreach (var info in Assignments.Values.OrderBy(p => GetPersonnelName(p)))
            {
                CreatePersonnelCard(personnelRoot, info, level, phoenix, refresh);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(personnelRoot.GetComponent<RectTransform>());
        }


        #endregion

        #region Card
        private static void CreatePersonnelCard(Transform parent, PersonnelInfo person, GeoLevelController level, GeoPhoenixFaction phoenix, Action refresh)
        {
            string personnelName = GetPersonnelName(person);

            var card = new GameObject($"Personnel_{personnelName}", typeof(RectTransform));
            card.transform.SetParent(parent, false);
            card.AddComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f, 0.85f);

            var hLayout = card.AddComponent<HorizontalLayoutGroup>();
            hLayout.padding = new RectOffset(10, 10, 10, 10);
            hLayout.spacing = 25;
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.childForceExpandHeight = false;
            hLayout.childForceExpandWidth = false;

            var nameText = AddLabel(card.transform, personnelName, 40, Color.white);
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
            bool isDismissedOperative = PersonnelRestrictions.IsDismissedOperative(person?.Character);

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
                if (isDismissedOperative)
                {
                    ShowMessage($"{person.Character?.DisplayName} is a dismissed operative and cannot use the civilian training path.\nUse Redeploy instead.");
                    return;
                }

                ShowTrainingSelection(level, person, specs, refresh);
            });

            AddActionButton(actionsRow.transform, isDismissedOperative ? "Redeploy" : "Deploy", () =>
            {
                ShowDeploymentSelection(level, person, phoenix, specs, refresh);
            });
        }

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
                        return $"Training Complete ({specName})";
                    }
                    return $"Training: {specName} (Lv {session.VirtualLevelAchieved}/{session.TargetLevel}, {remaining}d left)";
                default:
                    return person.Assignment.ToString();
            }
        }

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
                                var character = TrainingFacilityRework.RedeployDismissedOperative(level, person.Character, geoBase);
                                if (character != null)
                                {
                                    PersonnelData.RemovePersonnel(faction, person);
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
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
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

        private static string GetPersonnelName(PersonnelInfo person)
        {
            return person?.Character?.DisplayName
                   ?? $"Personnel {person?.Id}";
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
}