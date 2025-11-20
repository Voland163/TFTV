using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Geoscape.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Entities.PhoenixBases.FacilityComponents;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using static TFTV.TFTVBaseRework.TrainingFacilityRework;
using static TFTV.TFTVBaseRework.Workers;
using Object = UnityEngine.Object;

namespace TFTV.TFTVBaseRework
{
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

        internal static IReadOnlyList<GeoUnitDescriptor> Unassigned => _unassigned;

        internal static void DailyTick(GeoLevelController level)
        {
            try
            {
                if (level?.PhoenixFaction == null)
                {
                    return;
                }

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
                    if (descriptor != null)
                    {
                        _unassigned.Add(descriptor);
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void CleanupInvalid(GeoLevelController level)
        {
            _unassigned.RemoveAll(d => d == null || level == null || level.PhoenixFaction == null);
        }

        // Adjusted to match GeoPhoenixFaction.RegenerateNakedRecruits flow
        private static GeoUnitDescriptor GenerateDescriptor(GeoLevelController level, GeoPhoenixFaction phoenix)
        {
            try
            {
                if (level == null || phoenix == null)
                {
                    return null;
                }

                CharacterGenerationContext context = level.CharacterGenerator.GenerateCharacterGeneratorContext(phoenix);
                GeoUnitDescriptor descriptor = level.CharacterGenerator.GenerateRandomUnit(context);
                if (descriptor == null)
                {
                    return null;
                }

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
                    var level = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
                    var phoenix = level?.PhoenixFaction;
                    if (level == null || phoenix == null)
                    {
                        TFTVLogger.Always("PersonnelManagementUI: Level or PhoenixFaction null, letting vanilla stand.");
                        return;
                    }

                    EnsurePoolIsReady(level);

                    var recruitsModule = level.View?.GeoscapeModules?.RecruitsListModule;
                    if (recruitsModule == null)
                    {
                        TFTVLogger.Always("PersonnelManagementUI: recruitsModule null.");
                        return;
                    }

                    var baseRoot = recruitsModule.RecruitsListRoot != null
                        ? recruitsModule.RecruitsListRoot.transform
                        : recruitsModule.transform;

                    if (baseRoot == null)
                    {
                        TFTVLogger.Always("PersonnelManagementUI: baseRoot null.");
                        return;
                    }

                    // Defer our build to LateUpdate so we run AFTER vanilla finishes populating the list this frame.
                    var deferrer = baseRoot.gameObject.GetComponent<DeferredPersonnelBuilder>()
                                   ?? baseRoot.gameObject.AddComponent<DeferredPersonnelBuilder>();

                    deferrer.Build = () =>
                    {
                        try
                        {
                            // Hide vanilla cards/messages but keep controllers intact
                            for (int i = 0; i < baseRoot.childCount; i++)
                            {
                                var child = baseRoot.GetChild(i);
                                if (child != null) child.gameObject.SetActive(false);
                            }

                            var personnelRoot = EnsurePersonnelRoot(recruitsModule);
                            if (personnelRoot == null)
                            {
                                TFTVLogger.Always("PersonnelManagementUI (deferred): personnelRoot null.");
                                return;
                            }

                            PopulatePersonnelUI(__instance, level, personnelRoot);
                            TFTVLogger.Always("PersonnelManagementUI (deferred): custom list populated.");
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                    };
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
            private static void Postfix(UIStateRosterRecruits __instance)
            {
                try
                {
                    var level = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
                    var recruitsModule = level?.View?.GeoscapeModules?.RecruitsListModule;
                    if (recruitsModule == null) return;

                    var baseRoot = recruitsModule.RecruitsListRoot != null
                        ? recruitsModule.RecruitsListRoot.transform
                        : recruitsModule.transform;

                    if (baseRoot == null) return;

                    // Remove our container (if any) and re-enable vanilla children
                    var listContent = baseRoot.GetComponentInChildren<VerticalLayoutGroup>(true)?.transform ?? baseRoot;
                    var personnelRoot = listContent?.Find(PersonnelContainerName);
                    if (personnelRoot != null) Object.Destroy(personnelRoot.gameObject);

                    for (int i = 0; i < baseRoot.childCount; i++)
                    {
                        var child = baseRoot.GetChild(i);
                        if (child != null) child.gameObject.SetActive(true);
                    }

                    var def = baseRoot.gameObject.GetComponent<DeferredPersonnelBuilder>();
                    if (def != null) Object.Destroy(def);

                    if (baseRoot is RectTransform rt)
                        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

                    TFTVLogger.Always("PersonnelManagementUI: cleanup done on ExitState.");
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        #endregion

        // --- Helper Methods (previously referenced by the Prefix patch but missing) ---

        private static void EnsurePoolIsReady(GeoLevelController level)
        {
            try
            {
                if (level == null) return;

                if (_lastGenerationDay < 0)
                {
                    _lastGenerationDay = level.Timing.Now.TimeSpan.Days;
                }

                // If we have no entries (first open or cleared by load) create one batch.
                if (Unassigned.Count == 0)
                {
                    GenerateBatch(level);
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void HideVanillaRecruitUI(UIModuleRecruitsList recruitsListModule)
        {
            try
            {
                if (recruitsListModule == null) return;

                if (recruitsListModule.NoRecruitsMessage != null)
                    recruitsListModule.NoRecruitsMessage.SetActive(false);

                if (recruitsListModule.NoRecruitsMessageTextBackground != null)
                    recruitsListModule.NoRecruitsMessageTextBackground.SetActive(false);

                if (recruitsListModule.SpecializationController != null)
                    recruitsListModule.SpecializationController.enabled = false;

                if (recruitsListModule.InfoController != null)
                    recruitsListModule.InfoController.enabled = false;

                if (recruitsListModule.RecruitsListTabbingController != null)
                    recruitsListModule.RecruitsListTabbingController.enabled = false;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static Transform EnsurePersonnelRoot(UIModuleRecruitsList recruitsListModule)
        {
            Transform baseRoot = recruitsListModule?.RecruitsListRoot != null
                ? recruitsListModule.RecruitsListRoot.transform
                : recruitsListModule?.transform;

            if (baseRoot == null) return null;

            // Try to locate existing vertical content root (vanilla list container)
            Transform listContent = baseRoot.GetComponentInChildren<VerticalLayoutGroup>(true)?.transform ?? baseRoot;

            if (listContent == null) return null;

            Transform personnelRoot = listContent.Find(PersonnelContainerName);
            if (personnelRoot == null)
            {
                var container = new GameObject(PersonnelContainerName, typeof(RectTransform));
                container.transform.SetParent(listContent, false);

                var layout = container.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 10f;
                layout.padding = new RectOffset(12, 12, 12, 12);
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;

                var fitter = container.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                var le = container.AddComponent<LayoutElement>();
                le.flexibleWidth = 1f;

                personnelRoot = container.transform;
            }

            personnelRoot.SetAsLastSibling();
            personnelRoot.gameObject.SetActive(true);
            return personnelRoot;
        }

        private static void PopulatePersonnelUI(UIStateRosterRecruits state, GeoLevelController level, Transform personnelRoot)
        {
            if (personnelRoot == null || level?.PhoenixFaction == null) return;

            ClearChildren(personnelRoot);

            // Ensure pool not empty
            EnsurePoolIsReady(level);

            AddSummaryHeader(state, personnelRoot, level.PhoenixFaction);

            Action refresh = () => PopulatePersonnelUI(state, level, personnelRoot);

            foreach (var proto in Unassigned.ToList())
            {
                CreatePersonnelCard(personnelRoot, proto, level, level.PhoenixFaction, refresh);
            }

            if (personnelRoot is RectTransform rt)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
                if (rt.parent is RectTransform prt)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(prt);
                }
            }
        }

        private static void ClearChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var c = root.GetChild(i);
                if (c != null)
                {
                    UnityEngine.Object.Destroy(c.gameObject);
                }
            }
        }

        private static void AddSummaryHeader(UIStateRosterRecruits state, Transform parent, GeoPhoenixFaction phoenix)
        {
            var go = new GameObject("TFTV_Personnel_Header", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var txt = go.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = 18;

            var research = TrainingFacilityRework.GetWorkSlotUsage(phoenix, FacilitySlotType.Research);
            var manuf = TrainingFacilityRework.GetWorkSlotUsage(phoenix, FacilitySlotType.Manufacturing);

            txt.text =
                $"Unassigned Personnel: {Unassigned.Count}\n" +
                $"Research Workers: {research.used}/{research.provided}  Manufacturing Workers: {manuf.used}/{manuf.provided}";
            txt.color = Color.cyan;
        }

        private static void CreatePersonnelCard(Transform parent, GeoUnitDescriptor proto, GeoLevelController level, GeoPhoenixFaction phoenix, Action refresh)
        {
            if (proto == null) return;

            var card = new GameObject($"Personnel_{proto.GetName()}", typeof(RectTransform));
            card.transform.SetParent(parent, false);

            var bg = card.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.14f, 0.18f, 0.85f);

            var layout = card.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4;
            layout.padding = new RectOffset(6, 6, 6, 6);
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            AddLabel(card.transform, proto.GetName(), 16, Color.white);
            AddLabel(card.transform, $"Template Level: {proto.Level}", 12, Color.gray);

            var specs = ResolveAvailableMainSpecs(level);
            if (specs.Count == 0)
            {
                AddLabel(card.transform, "No available classes", 12, Color.red);
                return;
            }

            var classRoot = new GameObject("ClassSelect", typeof(RectTransform));
            classRoot.transform.SetParent(card.transform, false);
            var classText = classRoot.AddComponent<Text>();
            classText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            classText.fontSize = 12;
            classText.color = Color.yellow;

            int idx = _rng.Next(specs.Count);
            var currentSpec = specs[idx];
            classText.text = $"Chosen Class: {currentSpec?.name}";

            AddActionButton(card.transform, "Make Operative", () =>
            {
                MakeOperative(proto, currentSpec, phoenix, level);
                refresh();
            });

            AddActionButton(card.transform, "Assign Research", () =>
            {
                AssignWorker(proto, phoenix, FacilitySlotType.Research);
                refresh();
            });

            AddActionButton(card.transform, "Assign Manufacturing", () =>
            {
                AssignWorker(proto, phoenix, FacilitySlotType.Manufacturing);
                refresh();
            });

            AddActionButton(card.transform, "Start Training", () =>
            {
                AssignTraining(proto, currentSpec, level);
                refresh();
            });

            AddActionButton(card.transform, "Cycle Class", () =>
            {
                idx = (idx + 1) % specs.Count;
                currentSpec = specs[idx];
                classText.text = $"Chosen Class: {currentSpec?.name}";
            });
        }

        private static void AddLabel(Transform parent, string text, int size, Color color)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.text = text;
            t.fontSize = size;
            t.color = color;
        }

        private static void AddActionButton(Transform parent, string caption, Action onClick)
        {
            var go = new GameObject($"Btn_{caption}", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.35f, 0.55f, 0.9f);

            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            var txtGO = new GameObject("Text", typeof(RectTransform));
            txtGO.transform.SetParent(go.transform, false);
            var txt = txtGO.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = caption;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.fontSize = 12;

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 26;
            le.minWidth = 140;
        }

        private static List<SpecializationDef> ResolveAvailableMainSpecs(GeoLevelController level)
        {
            try
            {
                var cache = TFTVMain.Main.DefCache;
                var names = new[]
                {
                    "AssaultSpecializationDef",
                    "HeavySpecializationDef",
                    "SniperSpecializationDef",
                    "PriestSpecializationDef",
                    "BerserkerSpecializationDef",
                    "InfiltratorSpecializationDef",
                    "TechnicianSpecializationDef"
                };
                var list = new List<SpecializationDef>();
                foreach (var n in names)
                {
                    var spec = cache.GetDef<SpecializationDef>(n);
                    if (spec != null) list.Add(spec);
                }
                return list;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return new List<SpecializationDef>();
            }
        }

        private static void MakeOperative(GeoUnitDescriptor proto, SpecializationDef spec, GeoPhoenixFaction faction, GeoLevelController level)
        {
            try
            {
                if (proto == null || spec == null) return;
                var baseToUse = faction.Bases.FirstOrDefault();
                if (baseToUse == null) return;
                var character = TrainingFacilityRework.CreateOperativeFromDescriptor(level, proto, baseToUse, spec);
                if (character != null) RemoveDescriptor(proto);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void AssignWorker(GeoUnitDescriptor proto, GeoPhoenixFaction faction, FacilitySlotType slotType)
        {
            try
            {
                if (proto == null) return;
                if (TrainingFacilityRework.TryAssignToWork(faction, slotType))
                    RemoveDescriptor(proto);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void AssignTraining(GeoUnitDescriptor proto, SpecializationDef spec, GeoLevelController level)
        {
            try
            {
                if (proto == null || spec == null) return;
                var phoenix = level.PhoenixFaction;
                var facility = FindAnyValidTrainingFacility(phoenix);
                if (facility == null)
                {
                    MakeOperative(proto, spec, phoenix, level);
                    return;
                }

                if (TrainingFacilityRework.TryAssignDescriptorToTraining(level, proto, facility, spec))
                    RemoveDescriptor(proto);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static GeoPhoenixFacility FindAnyValidTrainingFacility(GeoPhoenixFaction phoenix)
        {
            foreach (var b in phoenix.Bases)
            {
                foreach (var f in b.Layout.Facilities)
                {
                    if (f != null &&
                        f.GetComponent<ExperienceFacilityComponent>() != null &&
                        f.IsPowered &&
                        f.State == GeoPhoenixFacility.FacilityState.Functioning)
                    {
                        return f;
                    }
                }
            }
            return null;
        }

        private sealed class DeferredPersonnelBuilder : MonoBehaviour
        {
            public Action Build;

            private void LateUpdate()
            {
                try
                {
                    var b = Build;
                    Build = null;
                    if (b != null) b();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
                finally
                {
                    // Run once
                    Destroy(this);
                }
            }
        }

        // ==================== RELIABLE HOOKS ====================
        [HarmonyPatch(typeof(UIModuleRecruitsList), "SetRecruitsList")]
        private static class UIModuleRecruitsList_MultiHooks
        {
            // Select all plausible methods that (re)build the list
           

            private static void Postfix(UIModuleRecruitsList __instance)
            {
                try
                {
                    // Build our overlay if not already present
                    TryOverlay(__instance, "SetRecruitsList");
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        // 3) Fallback when tab becomes visible
        [HarmonyPatch(typeof(UIModuleRecruitsList), "OnEnable")]
        private static class UIModuleRecruitsList_OnEnable_Overlay
        {
            private static void Postfix(UIModuleRecruitsList __instance)
            {
                try
                {
                    TryOverlay(__instance, "OnEnable");
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

    

        // Build our overlay once per “build cycle”
        private static void TryOverlay(UIModuleRecruitsList module, string source)
        {
            var level = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
            var phoenix = level?.PhoenixFaction;
            if (level == null || phoenix == null) return;

            EnsurePoolIsReady(level);

            var baseRoot = module.RecruitsListRoot != null ? module.RecruitsListRoot.transform : module.transform;
            if (baseRoot == null) return;

            // Use the inner vertical layout content as card parent (do not disable it!)
            var content = baseRoot.GetComponentInChildren<VerticalLayoutGroup>(true)?.transform;
            if (content == null)
            {
                TFTVLogger.Always($"{LogPrefix} no VerticalLayoutGroup content found ({source}).");
                return;
            }

            // If our container already exists, skip rebuilding
            var existing = content.Find(PersonnelContainerName);
            if (existing != null)
            {
                TFTVLogger.Always($"{LogPrefix} overlay already present ({source}).");
                return;
            }

            // Hide only vanilla recruit card GameObjects, keep structural parents (ScrollRect, Mask, Viewport, Content)
            int hidden = 0;
            for (int i = 0; i < content.childCount; i++)
            {
                var child = content.GetChild(i);
                if (child == null) continue;
                if (child.name == PersonnelContainerName) continue;

                // Heuristics: card objects usually have Image + LayoutElement and not ScrollRect/Mask
                bool looksLikeCard =
                    child.GetComponent<Button>() != null ||
                    (child.GetComponent<Image>() != null && child.GetComponent<VerticalLayoutGroup>() == null && child.GetComponent<HorizontalLayoutGroup>() == null);

                // If the game uses a specific card view type, detect it dynamically
                bool hasCardType = child.GetComponents<Component>()
                    .Any(c => c.GetType().Name.IndexOf("Recruit", StringComparison.OrdinalIgnoreCase) >= 0);

                if (looksLikeCard || hasCardType)
                {
                    child.gameObject.SetActive(false);
                    hidden++;
                }
            }

            TFTVLogger.Always($"{LogPrefix} hidden vanilla cards={hidden} ({source}).");

            // Create and populate our container under the active content
            var personnelRoot = new GameObject(PersonnelContainerName, typeof(RectTransform)).transform;
            personnelRoot.SetParent(content, false);

            var layout = personnelRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = personnelRoot.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var le = personnelRoot.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;

            PopulatePersonnelUI(null, level, personnelRoot);
            TFTVLogger.Always($"{LogPrefix} overlay populated ({source}), unassigned={Unassigned.Count}");
        }
    }

    
}