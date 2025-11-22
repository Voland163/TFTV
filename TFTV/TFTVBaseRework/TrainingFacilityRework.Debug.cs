using HarmonyLib;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Geoscape.Levels;
using System;
using System.Linq;
using System.Reflection;
using PhoenixPoint.Geoscape.Entities;

namespace TFTV.TFTVBaseRework
{
    internal static class TrainingFacilityReworkDebug
    {
        // Toggle
        private const bool EnableTrainingDebug = true;

        [HarmonyPatch(typeof(GeoPhoenixFaction), "AddRecruit")]
        internal static class GeoPhoenixFaction_AddRecruit_Debug
        {
            static void Prefix(GeoPhoenixFaction __instance, GeoCharacter recruit, IGeoCharacterContainer toContainer)
            {
                if (!EnableTrainingDebug || recruit == null) return;
                TFTVLogger.Always($"[TrainingDBG] AddRecruit PREFIX {recruit.DisplayName} Id={recruit.Id} " +
                                  $"END={recruit.CharacterStats?.Endurance?.Max} WILL={recruit.CharacterStats?.Willpower?.Max} SPD={recruit.CharacterStats?.Speed?.Max} " +
                                  $"PendingKey={TrainingFacilityReworkPending(recruit.Id)}");
            }

            static void Postfix(GeoPhoenixFaction __instance, GeoCharacter recruit, IGeoCharacterContainer toContainer)
            {
                if (!EnableTrainingDebug || recruit == null) return;
                TFTVLogger.Always($"[TrainingDBG] AddRecruit POSTFIX {recruit.DisplayName} Id={recruit.Id} " +
                                  $"END={recruit.CharacterStats?.Endurance?.Max} WILL={recruit.CharacterStats?.Willpower?.Max} SPD={recruit.CharacterStats?.Speed?.Max}");
            }

            // Local accessor without exposing private dictionary.
            private static bool TrainingFacilityReworkPending(int id)
            {
                var t = typeof(TrainingFacilityRework);
                var f = t.GetField("_pendingPostRecruitStatApply", BindingFlags.NonPublic | BindingFlags.Static);
                var dict = f?.GetValue(null) as System.Collections.IDictionary;
                return dict != null && dict.Contains(id);
            }
        }

        // One-time signature dump.
        [HarmonyPatch(typeof(GeoLevelController), "DailyUpdate")]
        internal static class GeoLevelController_DailyUpdate_TrainingDbg
        {
            static bool _dumped;
            static void Postfix()
            {
                if (!EnableTrainingDebug || _dumped) return;
                _dumped = true;
                var methods = typeof(GeoPhoenixFaction)
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(m => m.Name == "AddRecruit");
                foreach (var m in methods)
                {
                    var sig = $"{m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name))})";
                    TFTVLogger.Always($"[TrainingDBG] Found AddRecruit overload: {sig} (IsPublic={m.IsPublic})");
                }
                // Patch ordering introspection
                var target = typeof(GeoPhoenixFaction).GetMethod("AddRecruit",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (target != null)
                {
                    var info = Harmony.GetPatchInfo(target);
                    if (info != null)
                    {
                        foreach (var p in info.Postfixes) TFTVLogger.Always($"[TrainingDBG] Postfix patch owner={p.owner} priority={p.priority}");
                        foreach (var p in info.Prefixes) TFTVLogger.Always($"[TrainingDBG] Prefix patch owner={p.owner} priority={p.priority}");
                    }
                }
            }
        }
    }
}