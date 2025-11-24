using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using UnityEngine;
using PhoenixPoint.Common.Entities;
using static TFTV.TFTVBaseRework.GeoCharacterFilter;
using static TFTV.TFTVBaseRework.Workers;
using Base.Core;

namespace TFTV.TFTVBaseRework
{

    [HarmonyPatch(typeof(GeoPhoenixFaction), "RecordExtendedInstanceData")]
    internal static class GeoPhoenixFaction_RecordExtendedInstanceData_HiddenPersonnel_patch
    {
        private static void Postfix(GeoPhoenixFaction __instance)
        {
            try
            {
                var save = TFTVBaseRework.HiddenPersonnelManager.CreateSnapshot();
                HiddenPersonnelRuntimeStore.Store(save);
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(GeoPhoenixFaction),"LevelStartLoadedGame")]
    internal static class GeoPhoenixFaction_LevelStartLoadedGame_HiddenPersonnel_patch
    {
        private static void Postfix(GeoPhoenixFaction __instance)
        {
            try
            {
                var saved = HiddenPersonnelRuntimeStore.Retrieve();
                TFTVBaseRework.HiddenPersonnelManager.LoadSnapshot(saved);
            }
            catch { }
        }
    }

    internal static class HiddenPersonnelRuntimeStore
    {
        private static TFTVBaseRework.HiddenPersonnelSave _cache;
        internal static void Store(TFTVBaseRework.HiddenPersonnelSave s) => _cache = s;
        internal static TFTVBaseRework.HiddenPersonnelSave Retrieve() => _cache;
    }


    internal enum HiddenPersonnelAssignment
    {
        Unassigned = 0,
        Research = 1,
        Manufacturing = 2,
        Training = 3
    }

    [Serializable]
    internal sealed class HiddenPersonnelRecord
    {
        public GeoTacUnitId Id;
        public HiddenPersonnelAssignment Assignment;
        public string TrainingSpecName;
        public int TrainingStartDay;
        public int TrainingDurationDays;
        public int TrainingTargetLevel;
        public int LastAppliedLevel;
        public bool TrainingComplete;
    }

    [Serializable]
    internal sealed class HiddenPersonnelSave
    {
        public List<HiddenPersonnelRecord> Records = new List<HiddenPersonnelRecord>();
    }

    internal static class HiddenPersonnelManager
    {
        private const string LogPrefix = "[HiddenPersonnel]";
        private static readonly Dictionary<GeoTacUnitId, HiddenPersonnelRecord> _records = new Dictionary<GeoTacUnitId, HiddenPersonnelRecord>();
        private static GameTagDef _hiddenTag;

        internal static IEnumerable<HiddenPersonnelRecord> Records => _records.Values;
        internal static IEnumerable<GeoCharacter> HiddenCharacters =>
            GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>()?.PhoenixFaction?.Soldiers.Where(IsHidden) ?? Enumerable.Empty<GeoCharacter>();

        private static GameTagDef EnsureHiddenTag()
        {
            if (_hiddenTag != null) return _hiddenTag;
            _hiddenTag = TFTVMain.Main.DefCache.GetDef<GameTagDef>(HiddenOperativeTagFilter.HiddenTagName);
            return _hiddenTag;
        }

        internal static bool IsHidden(GeoCharacter c)
        {
            if (c == null) return false;
            EnsureHiddenTag();
            return c.GameTags.Contains(_hiddenTag);
        }

        internal static HiddenPersonnelRecord Get(GeoTacUnitId id)
        {
            _records.TryGetValue(id, out var r);
            return r;
        }

        internal static void Register(GeoCharacter c)
        {
            if (c == null || _records.ContainsKey(c.Id)) return;
            EnsureHiddenTag();
            if (!c.GameTags.Contains(_hiddenTag))
                c.GameTags.Add(_hiddenTag);

            var rec = new HiddenPersonnelRecord
            {
                Id = c.Id,
                Assignment = HiddenPersonnelAssignment.Unassigned,
                LastAppliedLevel = c.LevelProgression?.Level ?? 1
            };
            _records[c.Id] = rec;
            TFTVLogger.Always($"{LogPrefix} Registered hidden operative #{c.Id} {c.DisplayName}");
        }

        internal static void Assign(GeoCharacter c, HiddenPersonnelAssignment assignment, SpecializationDef spec = null)
        {
            if (c == null) return;
            var phoenix = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>()?.PhoenixFaction;
            var rec = Get(c.Id);
            if (rec == null) return;

            // release previous slot
            switch (rec.Assignment)
            {
                case HiddenPersonnelAssignment.Research:
                    ResearchManufacturingSlotsManager.DecrementUsedSlot(phoenix, FacilitySlotType.Research);
                    break;
                case HiddenPersonnelAssignment.Manufacturing:
                    ResearchManufacturingSlotsManager.DecrementUsedSlot(phoenix, FacilitySlotType.Manufacturing);
                    break;
            }

            if (assignment == HiddenPersonnelAssignment.Research)
            {
                if (ResearchManufacturingSlotsManager.IncrementUsedSlot(phoenix, FacilitySlotType.Research))
                    rec.Assignment = assignment;
                return;
            }

            if (assignment == HiddenPersonnelAssignment.Manufacturing)
            {
                if (ResearchManufacturingSlotsManager.IncrementUsedSlot(phoenix, FacilitySlotType.Manufacturing))
                    rec.Assignment = assignment;
                return;
            }

            if (assignment == HiddenPersonnelAssignment.Training && spec != null)
            {
                // init training
                rec.Assignment = assignment;
                rec.TrainingSpecName = spec.name;
                c.Progression.MainSpecDef.ClassTag = spec.ClassTag; // choose class now
                rec.TrainingStartDay = phoenix.GeoLevel.Timing.Now.TimeSpan.Days;
                rec.TrainingDurationDays = TrainingFacilityRework.GetEffectiveDurationDays(phoenix);
                rec.TrainingTargetLevel = TrainingFacilityRework.GetTargetLevel(phoenix);
                rec.TrainingComplete = false;
                TFTVLogger.Always($"{LogPrefix} {c.DisplayName} -> Training {spec.name} target L{rec.TrainingTargetLevel} duration {rec.TrainingDurationDays}d");
                return;
            }

            rec.Assignment = HiddenPersonnelAssignment.Unassigned;
        }

        internal static void DailyProgress(GeoLevelController level)
        {
            if (level == null) return;
            int day = level.Timing.Now.TimeSpan.Days;

            foreach (var rec in _records.Values)
            {
                if (rec.Assignment != HiddenPersonnelAssignment.Training || rec.TrainingComplete) continue;
                var c = level.PhoenixFaction.Soldiers.FirstOrDefault(s => s.Id == rec.Id);
                if (c == null) continue;

                float progress = Mathf.Clamp01((float)(day - rec.TrainingStartDay) / Math.Max(1, rec.TrainingDurationDays));
                int desiredLevel = 1 + Mathf.FloorToInt(progress * (rec.TrainingTargetLevel - 1));
                desiredLevel = Mathf.Min(rec.TrainingTargetLevel, Math.Max(1, desiredLevel));

                if (desiredLevel > rec.LastAppliedLevel)
                {
                    int delta = desiredLevel - rec.LastAppliedLevel;
                    c.LevelProgression.SetLevel(desiredLevel);
                    c.BonusStrength += delta * 2;
                    c.BonusWillpower += delta;
                    c.BonusSpeed += delta;
                    rec.LastAppliedLevel = desiredLevel;
                    try
                    {
                        var m = typeof(GeoCharacter).GetMethod("UpdateStats", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        m?.Invoke(c, new object[] { false });
                    }
                    catch { }
                    TFTVLogger.Always($"{LogPrefix} {c.DisplayName} level {desiredLevel}");
                }

                if (progress >= 1f || desiredLevel >= rec.TrainingTargetLevel)
                {
                    rec.TrainingComplete = true;
                    TFTVLogger.Always($"{LogPrefix} Training complete {c.DisplayName}");
                }
            }
        }

        internal static string GetAssignmentText(HiddenPersonnelRecord rec)
        {
            switch (rec.Assignment)
            {
                case HiddenPersonnelAssignment.Research:
                    return "Research";
                case HiddenPersonnelAssignment.Manufacturing:
                    return "Manufacturing";
                case HiddenPersonnelAssignment.Training:
                    return rec.TrainingComplete
                        ? $"Training Complete ({rec.TrainingSpecName})"
                        : $"Training {rec.TrainingSpecName} L{rec.LastAppliedLevel}/{rec.TrainingTargetLevel}";
                case HiddenPersonnelAssignment.Unassigned:
                    throw new NotImplementedException();
                default:
                    return "Unassigned";
            }
        }

        internal static HiddenPersonnelSave CreateSnapshot()
        {
            var save = new HiddenPersonnelSave();
            save.Records.AddRange(_records.Values.Select(r => new HiddenPersonnelRecord
            {
                Id = r.Id,
                Assignment = r.Assignment,
                TrainingSpecName = r.TrainingSpecName,
                TrainingStartDay = r.TrainingStartDay,
                TrainingDurationDays = r.TrainingDurationDays,
                TrainingTargetLevel = r.TrainingTargetLevel,
                LastAppliedLevel = r.LastAppliedLevel,
                TrainingComplete = r.TrainingComplete
            }));
            return save;
        }

        internal static void LoadSnapshot(HiddenPersonnelSave save)
        {
            _records.Clear();
            if (save == null) return;
            var phoenix = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>()?.PhoenixFaction;
            foreach (var r in save.Records)
            {
                var c = phoenix?.Soldiers.FirstOrDefault(s => s.Id == r.Id);
                if (c == null) continue;
                EnsureHiddenTag();
                if (!c.GameTags.Contains(_hiddenTag))
                    c.GameTags.Add(_hiddenTag);
                _records[r.Id] = r;
            }
            TFTVLogger.Always($"{LogPrefix} Loaded { _records.Count } hidden personnel.");
        }
    }

    internal static class HiddenPersonnelPatches
    {
        [HarmonyPatch(typeof(GeoPhoenixFaction), nameof(GeoPhoenixFaction.RegenerateNakedRecruits))]
        private static class GeoPhoenixFaction_RegenerateNakedRecruits_Patch
        {
            private static bool Prefix(GeoPhoenixFaction __instance)
            {
                try
                {
                    if (!__instance.RecruitmentFunctionalityUnlocked) return true;

                    int count = __instance.FactionDef.MaxNakedRecruitsAvailability.RandomValue();
                    var level = __instance.GeoLevel;
                    var baseSite = __instance.Bases.FirstOrDefault()?.Site;
                    if (baseSite == null) return false;

                    var context = level.CharacterGenerator.GenerateCharacterGeneratorContext(__instance);
                    for (int i = 0; i < count; i++)
                    {
                        var descriptor = level.CharacterGenerator.GenerateRandomUnit(context);
                        level.CharacterGenerator.ApplyRecruitDifficultyParameters(descriptor);
                        level.CharacterGenerator.RandomizeIdentity(descriptor);
                        var character = descriptor.SpawnAsCharacter();
                        __instance.AddRecruit(character, baseSite);
                        HiddenPersonnelManager.Register(character);
                    }

                    AccessTools.Field(typeof(GeoPhoenixFaction), "_lastNakedRecruitRefresh")
                        ?.SetValue(__instance, level.Timing.Now);
                    __instance.SpawnedRecruitNotification = true;

                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(GeoLevelController), "DailyUpdate")]
        private static class GeoLevelController_DailyUpdate_Patch
        {
            private static void Postfix(GeoLevelController __instance)
            {
                try { HiddenPersonnelManager.DailyProgress(__instance); }
                catch (Exception e) { TFTVLogger.Error(e); }
            }
        }

       

        private static void BuildCard(Transform parent, GeoCharacter c, HiddenPersonnelRecord rec,
            GeoPhoenixFaction phoenix, GeoLevelController level, Action refresh)
        {
            var card = new GameObject($"HP_{c.DisplayName}", typeof(RectTransform));
            card.transform.SetParent(parent, false);
            card.AddComponent<UnityEngine.UI.Image>().color = new Color(0.12f, 0.15f, 0.20f, 0.85f);

            var h = card.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            h.padding = new RectOffset(8, 8, 8, 8);
            h.spacing = 18;
            h.childAlignment = TextAnchor.MiddleLeft;

            Label(card.transform, c.DisplayName, 32, Color.white, 230);
            Label(card.transform, HiddenPersonnelManager.GetAssignmentText(rec), 26, Color.cyan, 320);

            var actions = new GameObject("Actions", typeof(RectTransform));
            actions.transform.SetParent(card.transform, false);
            var hl = actions.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            hl.spacing = 10;
            hl.childAlignment = TextAnchor.MiddleLeft;

            Button(actions.transform, "Research", () => { HiddenPersonnelManager.Assign(c, HiddenPersonnelAssignment.Research); refresh(); });
            Button(actions.transform, "Manufacturing", () => { HiddenPersonnelManager.Assign(c, HiddenPersonnelAssignment.Manufacturing); refresh(); });

            Button(actions.transform, "Training", () =>
            {
                var spec = TrainingFacilityRework.GetAvailableTrainingSpecializations(phoenix).FirstOrDefault();
                if (spec != null) HiddenPersonnelManager.Assign(c, HiddenPersonnelAssignment.Training, spec);
                refresh();
            });

            if (rec.TrainingComplete)
                Label(actions.transform, "Ready", 24, Color.yellow, 80);
        }

        private static void Label(Transform parent, string text, int size, Color color, float minWidth)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<UnityEngine.UI.Text>();
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAnchor.MiddleLeft;
            var le = go.AddComponent<UnityEngine.UI.LayoutElement>();
            le.minWidth = minWidth;
        }

        private static void Button(Transform parent, string caption, Action onClick)
        {
            var go = new GameObject($"Btn_{caption}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.25f, 0.35f, 0.55f, 0.9f);
            var btn = go.AddComponent<UnityEngine.UI.Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            var txtGO = new GameObject("Text", typeof(RectTransform));
            txtGO.transform.SetParent(go.transform, false);
            var txt = txtGO.AddComponent<UnityEngine.UI.Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = caption;
            txt.fontSize = 22;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            var le = go.AddComponent<UnityEngine.UI.LayoutElement>();
            le.minWidth = 140;
            le.minHeight = 40;
        }
    }
}