using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Geoscape.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Entities.PhoenixBases.FacilityComponents;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static TFTV.TFTVBaseRework.Workers;
using Object = UnityEngine.Object;

namespace TFTV.TFTVBaseRework
{
    internal enum PersonnelAssignment
    {
        Unassigned,
        Research,
        Manufacturing,
        Training
    }

    internal class PersonnelInfo
    {
        public GeoUnitDescriptor Descriptor { get; set; }
        public PersonnelAssignment Assignment { get; set; }
        public SpecializationDef TrainingSpec { get; set; }
    }

    /// <summary>
    /// Replaces the vanilla base recruit tab with a personnel management screen.
    /// </summary>
    /// 


    internal static class PersonnelManagementUI
    {
        private const int DaysBetweenRefresh = 4;
        private const int MinNewPersonnel = 4;
        private const int MaxNewPersonnel = 5;
        private const string PersonnelContainerName = "TFTV_PersonnelContainer";
        private const string LogPrefix = "[PersonnelUI]";

        private static readonly List<GeoUnitDescriptor> _unassigned = new List<GeoUnitDescriptor>();
        private static int _lastGenerationDay = -1;
        private static readonly System.Random _rng = new System.Random();
        private static GameObject _personnelPanel;

        internal static IReadOnlyList<GeoUnitDescriptor> Unassigned => _unassigned;

        internal static void DailyTick(GeoLevelController level)
        {
            try
            {
                if (level?.PhoenixFaction == null) return;
                int day = level.Timing.Now.TimeSpan.Days;
                if (_lastGenerationDay < 0)
                {
                    _lastGenerationDay = day;
                    GenerateBatch(level);
                    return;
                }
                if (day - _lastGenerationDay >= DaysBetweenRefresh)
                {
                    _lastGenerationDay = day;
                    GenerateBatch(level);
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void GenerateBatch(GeoLevelController level)
        {
            try
            {
                CleanupInvalid(level);
                int count = _rng.Next(MinNewPersonnel, MaxNewPersonnel + 1);
                GeoPhoenixFaction phoenix = level.PhoenixFaction;
                for (int i = 0; i < count; i++)
                {
                    GeoUnitDescriptor descriptor = GenerateDescriptor(level, phoenix);
                    if (descriptor != null) _unassigned.Add(descriptor);
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void CleanupInvalid(GeoLevelController level)
        {
            _unassigned.RemoveAll(d => d == null || level?.PhoenixFaction == null);
        }

        private static GeoUnitDescriptor GenerateDescriptor(GeoLevelController level, GeoPhoenixFaction phoenix)
        {
            try
            {
                if (level == null || phoenix == null) return null;
                CharacterGenerationContext context = level.CharacterGenerator.GenerateCharacterGeneratorContext(phoenix);
                GeoUnitDescriptor descriptor = level.CharacterGenerator.GenerateRandomUnit(context);
                if (descriptor == null) return null;
                level.CharacterGenerator.ApplyRecruitDifficultyParameters(descriptor);
                return descriptor;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return null;
            }
        }

        internal static void RemoveDescriptor(GeoUnitDescriptor descriptor)
        {
            _unassigned.Remove(descriptor);

        }

        #region UI Patching

        [HarmonyPatch(typeof(GeoLevelController), "DailyUpdate")]
        internal static class GeoLevelController_DailyUpdate_PersonnelPool
        {
            private static void Postfix(GeoLevelController __instance) => DailyTick(__instance);
        }

        [HarmonyPatch(typeof(UIStateRosterRecruits), "EnterState")]
        internal static class UIStateRosterRecruits_EnterState_PersonnelManagement
        {
            private static void Postfix(UIStateRosterRecruits __instance)
            {
                try
                {
                    CreatePersonnelPanel(__instance);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(UIStateRosterRecruits), "ExitState")]
        internal static class UIStateRosterRecruits_ExitState_PersonnelManagement
        {
            private static void Postfix()
            {
                try
                {
                    if (_personnelPanel != null)
                    {
                        Object.Destroy(_personnelPanel);
                        _personnelPanel = null;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(UIModuleRecruitsList), nameof(UIModuleRecruitsList.SetRecruitsList))]
        public static class PersonnelManagementPatch
        {

            public static void Postfix(UIModuleRecruitsList __instance)
            {
                if (__instance == null || __instance.RecruitsListRoot == null)
                {
                    return;
                }

                __instance.NoRecruitsMessage.SetActive(false);
                __instance.NoRecruitsMessageTextBackground.SetActive(false);
                __instance.SpecializationController.gameObject.SetActive(false);
                __instance.InfoController.gameObject.SetActive(false);

            }
        }

        #endregion

        private static void EnsurePoolIsReady(GeoLevelController level)
        {
            if (level != null && _lastGenerationDay < 0)
            {
                _lastGenerationDay = level.Timing.Now.TimeSpan.Days;
            }
            if (Unassigned.Count == 0)
            {
                GenerateBatch(level);
            }
        }

        private static void CreatePersonnelPanel(UIStateRosterRecruits state)
        {
            var level = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
            var recruitsModule = level?.View?.GeoscapeModules?.RecruitsListModule;
            if (recruitsModule == null) return;

            _personnelPanel = new GameObject(PersonnelContainerName, typeof(RectTransform));
            _personnelPanel.transform.SetParent(recruitsModule.transform, false);

            var canvas = _personnelPanel.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 10;
            _personnelPanel.AddComponent<GraphicRaycaster>();

            var rect = _personnelPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, 0.05f);
            rect.anchorMax = new Vector2(0.95f, 0.95f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(250, 50);
            rect.offsetMax = new Vector2(-50, -50);

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
            layout.spacing = 20f;
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            PopulatePersonnelUI(state, level, content.transform);
        }

        private static List<PersonnelInfo> GetCurrentPersonnelList(GeoLevelController level)
        {
            var list = new List<PersonnelInfo>();
            list.AddRange(Unassigned.Select(u => new PersonnelInfo { Descriptor = u, Assignment = PersonnelAssignment.Unassigned }));

            return list;
        }

        private static void PopulatePersonnelUI(UIStateRosterRecruits state, GeoLevelController level, Transform personnelRoot)
        {
            if (personnelRoot == null || level?.PhoenixFaction == null) return;
            ClearChildren(personnelRoot);
            EnsurePoolIsReady(level);

            var allPersonnel = GetCurrentPersonnelList(level);

            Action refresh = () =>
            {
                if (_personnelPanel != null)
                {
                    Object.Destroy(_personnelPanel);
                    CreatePersonnelPanel(state);
                }
            };

            foreach (var person in allPersonnel)
            {
                CreatePersonnelCard(personnelRoot, person, level, level.PhoenixFaction, refresh);
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

        private static void CreatePersonnelCard(Transform parent, PersonnelInfo person, GeoLevelController level, GeoPhoenixFaction phoenix, Action refresh)
        {
            var card = new GameObject($"Personnel_{person.Descriptor.GetName()}", typeof(RectTransform));
            card.transform.SetParent(parent, false);
            card.AddComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f, 0.85f);
            var vLayout = card.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(20, 20, 20, 20);
            vLayout.spacing = 10;

            var mainRow = new GameObject("MainRow", typeof(RectTransform));
            mainRow.transform.SetParent(card.transform, false);
            var hLayout = mainRow.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 20;

            var infoCol = new GameObject("InfoCol", typeof(RectTransform));
            infoCol.transform.SetParent(mainRow.transform, false);
            var infoVLayout = infoCol.AddComponent<VerticalLayoutGroup>();
            AddLabel(infoCol.transform, person.Descriptor.GetName(), 88, Color.white);
            AddLabel(infoCol.transform, person.Assignment.ToString(), 72, Color.cyan);
            infoCol.AddComponent<LayoutElement>().flexibleWidth = 1;

            var actionsCol = new GameObject("ActionsCol", typeof(RectTransform));
            actionsCol.transform.SetParent(mainRow.transform, false);
            var gridLayout = actionsCol.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(600, 140);
            gridLayout.spacing = new Vector2(20, 20);
            actionsCol.AddComponent<LayoutElement>().preferredWidth = 1220;

            var specs = ResolveAvailableMainSpecs(level);
            var currentSpec = person.TrainingSpec ?? (specs.Any() ? specs[_rng.Next(specs.Count)] : null);

            AddActionButton(actionsCol.transform, "Make Field Agent", () => { MakeOperative(person.Descriptor, currentSpec, phoenix, level); refresh(); });
            AddActionButton(actionsCol.transform, "Assign to Research", () => { AssignWorker(person.Descriptor, phoenix, FacilitySlotType.Research); refresh(); });
            AddActionButton(actionsCol.transform, "Assign to Manufacturing", () => { AssignWorker(person.Descriptor, phoenix, FacilitySlotType.Manufacturing); refresh(); });
            AddActionButton(actionsCol.transform, "Assign to Training", () => { AssignTraining(person.Descriptor, currentSpec, level); refresh(); });
        }

        private static Text AddLabel(Transform parent, string text, int size, Color color)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.text = text;
            t.fontSize = size;
            t.color = color;
            return t;
        }

        private static void AddActionButton(Transform parent, string caption, Action onClick)
        {
            var go = new GameObject($"Btn_{caption}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = new Color(0.25f, 0.35f, 0.55f, 0.9f);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            var txt = new GameObject("Text", typeof(RectTransform)).AddComponent<Text>();
            txt.transform.SetParent(go.transform, false);
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = caption;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.fontSize = 64;
        }

        private static List<SpecializationDef> ResolveAvailableMainSpecs(GeoLevelController level)
        {
            var cache = TFTVMain.Main.DefCache;
            var names = new[] { "AssaultSpecializationDef", "HeavySpecializationDef", "SniperSpecializationDef", "PriestSpecializationDef", "BerserkerSpecializationDef", "InfiltratorSpecializationDef", "TechnicianSpecializationDef" };
            return names.Select(n => cache.GetDef<SpecializationDef>(n)).Where(s => s != null).ToList();
        }

        private static void MakeOperative(GeoUnitDescriptor proto, SpecializationDef spec, GeoPhoenixFaction faction, GeoLevelController level)
        {
            if (proto == null || spec == null) return;
            var baseToUse = faction.Bases.FirstOrDefault();
            if (baseToUse == null) return;
            if (TrainingFacilityRework.CreateOperativeFromDescriptor(level, proto, baseToUse, spec) != null)
            {
                RemoveDescriptor(proto);
            }
        }

        private static void AssignWorker(GeoUnitDescriptor proto, GeoPhoenixFaction faction, FacilitySlotType slotType)
        {
            if (proto == null) return;
            // Unassign from other roles first
            RemoveDescriptor(proto);
            if (TrainingFacilityRework.TryAssignToWork(faction, slotType))
            {
                // Success
            }
            else
            {
                // Failed, put back to unassigned
                _unassigned.Add(proto);
            }
        }

        private static void AssignTraining(GeoUnitDescriptor proto, SpecializationDef spec, GeoLevelController level)
        {
            if (proto == null || spec == null) return;
            // Unassign from other roles first
            RemoveDescriptor(proto);
            var facility = FindAnyValidTrainingFacility(level.PhoenixFaction);
            if (facility == null)
            {
                MakeOperative(proto, spec, level.PhoenixFaction, level);
                return;
            }
            if (!TrainingFacilityRework.TryAssignDescriptorToTraining(level, proto, facility, spec))
            {
                // Failed, put back to unassigned
                _unassigned.Add(proto);
            }
        }

        private static GeoPhoenixFacility FindAnyValidTrainingFacility(GeoPhoenixFaction phoenix)
        {
            return phoenix.Bases.SelectMany(b => b.Layout.Facilities)
                .FirstOrDefault(f => f != null && f.GetComponent<ExperienceFacilityComponent>() != null && f.IsWorking);
        }
    }
}