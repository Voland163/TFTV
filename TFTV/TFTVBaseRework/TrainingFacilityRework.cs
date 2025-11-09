using Base.Defs;
using Base.Serialization.General;
using PhoenixPoint.Common.Saves;
using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Entities.PhoenixBases.FacilityComponents;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Tactical.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using PhoenixPoint.Geoscape.Entities.Sites;
using static TFTV.TFTVBaseRework.Workers;


namespace TFTV.TFTVBaseRework
{
    /// <summary>
    /// Framework for redesigned Training Facilities:
    /// - Each Training Facility provides 2–4 training slots (configurable).
    /// - Only unassigned operatives physically at the base can be put into training.
    /// - Player (later via UI) chooses target class specialization for the trainee; framework exposes API.
    /// - Training lasts X (base) days to raise operative to target level (4 by default, 5 if upgrade research completed).
    /// - Levels and basic stats are incremented as milestones are reached (no combat XP awarded here).
    /// - Research / tech upgrades reduce total training duration.
    /// - Vanilla passive XP from ExperienceFacilityComponent is neutralized elsewhere (already set to zero).
    /// 
    /// This is data-only; no UI. Public API methods:
    ///   TryAssignTrainee(facility, character, specialization)
    ///   UnassignTrainee(facility, character)
    ///   GetSessions(facility)
    /// 
    /// Patches hook into time advancement to process progress.
    /// </summary>
    internal static class TrainingFacilityRework
    {
        #region Config

        // Base duration (in days) to reach target level without bonuses.
        private const int BaseDurationDays = 6;

        // Minimum & maximum slots per facility.
        private const int SlotsPerFacility = 2;

        // Target base level; can increase with upgrade research.
        private const int BaseTargetLevel = 4;
        private const int UpgradedTargetLevel = 5;

        // Stat gains per level acquired inside training (framework defaults).
        private const int EndurancePerLevel = 2;
        private const int WillpowerPerLevel = 1;
        private const int SpeedPerLevel = 1;

        // Research IDs that reduce training duration (additive percentage reduction).
        // Adjust / extend with actual game research definition names.
        private static readonly Dictionary<string, float> DurationReductionResearch = new Dictionary<string, float>
        {
            { "NJ_Training_ResearchDef", 0.15f }, // -15%
            { "PX_AntediluvianArchaeology_ResearchDef", 0.10f }, // -10%           
        };

        // Research enabling level 5 target.
        private static readonly string AdvancedLevelResearchId = "PX_EliteTraining_ResearchDef"; //pending

        #endregion

        #region Data Model

        internal sealed class TrainingSession
        {
            public GeoCharacter Trainee;
            public SpecializationDef TargetSpecialization;
            public int StartDay;
            public int DurationDays;       // Effective duration after reductions.
            public int TargetLevel;        // 4 or 5 depending on research.
            public bool Completed;
            public int LastAppliedLevel;   // Track stat increments only for new levels.
            public string FacilityGuid;    // Facility identity (Def.name + grid pos) for simple persistence.

            public float ProgressFraction(int currentDay)
            {
                if (Completed) return 1f;
                var elapsed = currentDay - StartDay;
                return Math.Max(0f, Math.Min(1f, (float)elapsed / DurationDays));
            }
        }

        // Facility -> sessions map.
        private static readonly Dictionary<GeoPhoenixFacility, List<TrainingSession>> FacilitySessions
            = new Dictionary<GeoPhoenixFacility, List<TrainingSession>>();

        #endregion

        #region Public API

        public static IReadOnlyList<TrainingSession> GetSessions(GeoPhoenixFacility facility)
        {
            if (facility == null)
            {
                return Array.Empty<TrainingSession>();
            }
            List<TrainingSession> list;
            if (FacilitySessions.TryGetValue(facility, out list))
            {
                return list;
            }
            else
            {
                return Array.Empty<TrainingSession>();
            }
        }

        public static bool TryAssignTrainee(GeoPhoenixFacility facility, GeoCharacter character, SpecializationDef targetSpecialization)
        {
            try
            {
                if (!IsValidFacility(facility) || character == null || targetSpecialization == null)
                {
                    return false;
                }

                if (!IsCharacterEligibleForTraining(facility, character))
                {
                    return false;
                }

                var sessions = GetOrCreateFacilityList(facility);

                int slotCap = SlotsPerFacility;
                if (sessions.Count >= slotCap)
                {
                    return false;
                }

                if (sessions.Any(s => s.Trainee == character))
                {
                    return false; // Already training in this facility.
                }

                int currentDay = facility.PxBase.Site.GeoLevel.Timing.Now.TimeSpan.Days;
                int targetLevel = DetermineTargetLevel(facility.PxBase.Site.GeoLevel.PhoenixFaction);
                int effectiveDuration = CalculateEffectiveDurationDays(facility.PxBase.Site.GeoLevel.PhoenixFaction);

                var session = new TrainingSession
                {
                    Trainee = character,
                    TargetSpecialization = targetSpecialization,
                    StartDay = currentDay,
                    DurationDays = effectiveDuration,
                    TargetLevel = targetLevel,
                    Completed = false,
                    LastAppliedLevel = character.LevelProgression.Level,
                    FacilityGuid = BuildFacilityGuid(facility)
                };

                sessions.Add(session);

                // Apply specialization if not already present (without immediate level changes).
                EnsureSpecialization(character, targetSpecialization);

                return true;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return false;
            }
        }

        public static bool UnassignTrainee(GeoPhoenixFacility facility, GeoCharacter character)
        {
            try
            {
                if (!FacilitySessions.TryGetValue(facility, out var list) || character == null)
                {
                    return false;
                }

                var toRemove = list.FirstOrDefault(s => s.Trainee == character);
                if (toRemove == null) return false;

                list.Remove(toRemove);
                if (list.Count == 0)
                {
                    FacilitySessions.Remove(facility);
                }
                return true;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return false;
            }
        }

        #endregion

        #region Core Logic

        private static void AdvanceAllTraining(GeoLevelController geoLevel, int deltaDays)
        {
            if (geoLevel?.PhoenixFaction == null || deltaDays <= 0)
            {
                return;
            }

            int currentDay = geoLevel.Timing.Now.TimeSpan.Days;

            // Iterate copy to avoid modification during loop.
            var facilities = FacilitySessions.Keys.ToList();

            foreach (var facility in facilities)
            {
                if (!FacilitySessions.TryGetValue(facility, out var sessions) || sessions.Count == 0)
                {
                    continue;
                }

                foreach (var session in sessions)
                {
                    if (session.Completed || session.Trainee == null)
                    {
                        continue;
                    }

                    ProcessSessionProgress(session, currentDay);
                }

                // Clean completed with null trainee (edge cases).
                FacilitySessions[facility] = sessions.Where(s => s.Trainee != null).ToList();
                if (FacilitySessions[facility].Count == 0)
                {
                    FacilitySessions.Remove(facility);
                }
            }
        }

        private static void ProcessSessionProgress(TrainingSession session, int currentDay)
        {
            try
            {
                var character = session.Trainee;
                if (character.LevelProgression == null)
                {
                    // Cannot progress - invalid character state. Abort session.
                    session.Completed = true;
                    return;
                }

                // Determine target incremental levels based on progress fraction.
                float progress = session.ProgressFraction(currentDay);

                // Expected level based on linear interpolation.
                int expectedLevel = Math.Min(session.TargetLevel,
                    Math.Max(character.LevelProgression.Level, session.LastAppliedLevel + ComputeNewLevels(progress, session)));

                if (expectedLevel > character.LevelProgression.Level)
                {
                    // Apply per-level stat bonuses for each new level between current and expected.
                    for (int lvl = character.LevelProgression.Level + 1; lvl <= expectedLevel; lvl++)
                    {
                        ApplyLevelStatGains(character);
                        character.LevelProgression.SetLevel(lvl);
                        session.LastAppliedLevel = lvl;
                    }
                }

                // Completion check.
                if (progress >= 1f || character.LevelProgression.Level >= session.TargetLevel)
                {
                    session.Completed = true;
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static int ComputeNewLevels(float progress, TrainingSession session)
        {
            // Framework: evenly distribute levels over progress; does not downgrade existing levels.
            int levelsToGainTotal = session.TargetLevel - session.LastAppliedLevel;
            if (levelsToGainTotal <= 0) return 0;
            // Fraction of remaining.
            int projected = (int)Math.Floor(progress * (session.TargetLevel - session.StartDayLevelBaseline()));
            int currentGain = session.LastAppliedLevel - session.StartDayLevelBaseline();
            return Math.Max(0, projected - currentGain);
        }

        private static int StartDayLevelBaseline(this TrainingSession session)
        {
            return session.Trainee?.LevelProgression.Level ?? session.LastAppliedLevel;
        }

        private static void ApplyLevelStatGains(GeoCharacter character)
        {
            try
            {
                var stats = character.CharacterStats;
                if (stats == null)
                {
                    return;
                }

                // Basic increments; adjust if design changes.
                stats.Endurance.SetMax(stats.Endurance.Max + EndurancePerLevel);
                stats.Endurance.SetToMax();

                stats.Willpower.SetMax(stats.Willpower.Max + WillpowerPerLevel);
                stats.Willpower.SetToMax();

                stats.Speed.SetMax(stats.Speed.Max + SpeedPerLevel);
                stats.Speed.SetToMax();

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static int CalculateEffectiveDurationDays(GeoPhoenixFaction faction)
        {
            float duration = BaseDurationDays;
            if (faction?.Research != null)
            {
                foreach (var kv in DurationReductionResearch)
                {
                    if (faction.Research.HasCompleted(kv.Key))
                    {
                        duration *= (1f - kv.Value);
                    }
                }
            }
            return Math.Max(1, (int)Math.Ceiling(duration));
        }

        private static int DetermineTargetLevel(GeoPhoenixFaction faction)
        {
            if (faction?.Research?.HasCompleted(AdvancedLevelResearchId) == true)
            {
                return UpgradedTargetLevel;
            }
            return BaseTargetLevel;
        }

        internal static bool IsValidFacility(GeoPhoenixFacility facility)
        {
            if (facility?.Def == null) return false;
            // Must have ExperienceFacilityComponent to be considered a training facility.
            return facility.GetComponent<ExperienceFacilityComponent>() != null &&
                   facility.State == GeoPhoenixFacility.FacilityState.Functioning &&
                   facility.IsPowered;
        }

        private static bool IsCharacterEligibleForTraining(GeoPhoenixFacility facility, GeoCharacter character)
        {
            if (character?.Progression == null) return false;
            // Must be physically in base and not aboard a vehicle/aircraft.
            if (!facility.PxBase.SoldiersInBase.Contains(character))
            {
                return false;
            }
            // Do not retrain over target level.
            if (character.LevelProgression.Level >= UpgradedTargetLevel)
            {
                return false;
            }
            return true;
        }

        private static List<TrainingSession> GetOrCreateFacilityList(GeoPhoenixFacility facility)
        {
            if (!FacilitySessions.TryGetValue(facility, out var list))
            {
                list = new List<TrainingSession>();
                FacilitySessions[facility] = list;
            }
            return list;
        }

        internal static void EnsureSpecialization(GeoCharacter character, SpecializationDef specialization)
        {
            try
            {
                if (specialization == null || character == null) return;
                if (character.ClassTags != null && character.ClassTags.Contains(specialization.ClassTag))
                {
                    return; // Already has the class tag.
                }

                character.Progression.MainSpecDef.ClassTag = specialization.ClassTag;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static string BuildFacilityGuid(GeoPhoenixFacility facility)
        {
            return $"{facility.Def.name}@{facility.GridPosition.x},{facility.GridPosition.y}";
        }

        #endregion

        #region Harmony Patches

        /// <summary>
        /// Advance training on each geoscape time advancement (daily granularity).
        /// </summary>
        [HarmonyPatch(typeof(GeoLevelController), "DailyUpdate")]
        internal static class GeoLevelController_DailyUpdate_Training
        {
            private static void Postfix(GeoLevelController __instance)
            {
                try
                {

                    AdvanceAllTraining(__instance, 1);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        /// <summary>
        /// Clean up sessions if facility is demolished or powered off permanently.
        /// </summary>
        [HarmonyPatch(typeof(GeoPhoenixFacility), "DestroyFacility")]
        internal static class GeoPhoenixFacility_DestroyFacility_Training
        {
            private static void Prefix(GeoPhoenixFacility __instance)
            {
                try
                {
                    if (FacilitySessions.ContainsKey(__instance))
                    {
                        FacilitySessions.Remove(__instance);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        /// <summary>
        /// If facility becomes non-functioning (damaged/unpowered), pause sessions (they do not progress).
        /// We keep them in place (do not remove). Completion is only processed when functioning.
        /// </summary>
        [HarmonyPatch(typeof(GeoPhoenixFacility), "UpdateFacility")]
        internal static class GeoPhoenixFacility_UpdateFacility_Training
        {
            private static void Postfix(GeoPhoenixFacility __instance)
            {
                try
                {
                    if (!FacilitySessions.TryGetValue(__instance, out var sessions))
                    {
                        return;
                    }

                    if (!IsValidFacility(__instance))
                    {
                        // Facility not valid -> sessions remain but no progression. Nothing to do.
                    }
                    else
                    {
                        // Valid again: no action needed; progression will resume on next time tick.
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [SerializeType(SerializeMembersByDefault = SerializeMembersType.SerializeAll)]
        public sealed class TrainingSessionSave
        {
            public uint FacilityId;
            public int TraineeId;
            public string TargetSpecializationName;
            public int StartDay;
            public int DurationDays;
            public int TargetLevel;
            public bool Completed;
            public int LastAppliedLevel;
        }

        public static List<TrainingSessionSave> CreateSaveSnapshot()
        {
            try
            {
                var snapshot = new List<TrainingSessionSave>();
                foreach (var kv in FacilitySessions)
                {
                    var facility = kv.Key;
                    var sessions = kv.Value;
                    if (facility == null || sessions == null) continue;

                    foreach (var s in sessions)
                    {
                        if (s == null || s.Trainee == null) continue;
                        snapshot.Add(new TrainingSessionSave
                        {
                            FacilityId = facility.FacilityId,
                            TraineeId = s.Trainee.Id,
                            TargetSpecializationName = s.TargetSpecialization != null ? s.TargetSpecialization.name : null,
                            StartDay = s.StartDay,
                            DurationDays = s.DurationDays,
                            TargetLevel = s.TargetLevel,
                            Completed = s.Completed,
                            LastAppliedLevel = s.LastAppliedLevel
                        });
                    }
                }
                return snapshot;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return new List<TrainingSessionSave>();
            }
        }

        public static void LoadFromSnapshot(GeoLevelController controller, IEnumerable<TrainingSessionSave> snapshot)
        {
            try
            {
                FacilitySessions.Clear();

                if (controller == null || controller.PhoenixFaction == null || snapshot == null)
                {
                    return;
                }

                var allFacilities = controller.PhoenixFaction.Bases
                    .SelectMany(b => b.Layout.Facilities)
                    .ToList();

                var allHumans = controller.PhoenixFaction.HumanSoldiers?.ToList() ?? new List<GeoCharacter>();

                foreach (var s in snapshot)
                {
                    if (s == null || s.TraineeId <= 0) continue;

                    var facility = allFacilities.FirstOrDefault(f => f.FacilityId == s.FacilityId);
                    if (facility == null) continue;

                    var character = allHumans.FirstOrDefault(c => c.Id == s.TraineeId);
                    if (character == null) continue;

                    SpecializationDef spec = null;
                    if (!string.IsNullOrEmpty(s.TargetSpecializationName))
                    {
                        try
                        {
                            spec = TFTVMain.Main.DefCache.GetDef<SpecializationDef>(s.TargetSpecializationName);
                        }
                        catch
                        {
                            spec = null;
                        }
                    }

                    var list = GetOrCreateFacilityList(facility);
                    list.Add(new TrainingSession
                    {
                        Trainee = character,
                        TargetSpecialization = spec,
                        StartDay = s.StartDay,
                        DurationDays = s.DurationDays,
                        TargetLevel = s.TargetLevel,
                        Completed = s.Completed,
                        LastAppliedLevel = s.LastAppliedLevel,
                        FacilityGuid = BuildFacilityGuid(facility)
                    });
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void ClearAllSessions()
        {
            try
            {
                FacilitySessions.Clear();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        #region Personnel Management API (for Recruit/Personnel screen)

        /// <summary>
        /// Creates a new operative from a GeoUnitDescriptor at the target base, sets the chosen main specialization and level 1,
        /// then adds the operative to the Phoenix roster at that base.
        /// Returns the created GeoCharacter or null on failure.
        /// </summary>
        public static GeoCharacter CreateOperativeFromDescriptor(
            GeoLevelController level,
            GeoUnitDescriptor descriptor,
            GeoPhoenixBase targetBase,
            SpecializationDef mainClass)
        {
            try
            {
                if (level == null || descriptor == null || targetBase == null || mainClass == null)
                {
                    return null;
                }

                GeoPhoenixFaction phoenix = level.PhoenixFaction;
                if (phoenix == null)
                {
                    return null;
                }

                // Turn the descriptor into an actual character
                GeoCharacter character = level.CreateCharacterFromDescriptor(descriptor);
                if (character == null)
                {
                    return null;
                }

                // Force level 1 and class selection
                if (character.LevelProgression != null)
                {
                    character.LevelProgression.SetLevel(1);
                }
                EnsureSpecialization(character, mainClass);

                // Add to base (same as hiring into a container)
                phoenix.AddRecruit(character, (IGeoCharacterContainer)targetBase);
                return character;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return null;
            }
        }

        /// <summary>
        /// Creates an operative from descriptor at the base, sets the chosen class, and immediately assigns to training
        /// at the specified valid Training Facility. Returns true on success.
        /// </summary>
        public static bool TryAssignDescriptorToTraining(
            GeoLevelController level,
            GeoUnitDescriptor descriptor,
            GeoPhoenixFacility trainingFacility,
            SpecializationDef mainClass)
        {
            try
            {
                if (level == null || descriptor == null || trainingFacility == null || mainClass == null)
                {
                    return false;
                }

                // Safety: the facility must be valid and powered (same check used by the rework)
                if (!IsValidFacility(trainingFacility))
                {
                    return false;
                }

                // Create the operative in the facility's base
                GeoCharacter character = CreateOperativeFromDescriptor(
                    level,
                    descriptor,
                    trainingFacility.PxBase,
                    mainClass);

                if (character == null)
                {
                    return false;
                }

                // Assign to training via the rework framework
                return TryAssignTrainee(trainingFacility, character, mainClass);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return false;
            }
        }

        /// <summary>
        /// Tries to consume one Research/Manufacturing slot (global across all bases) and
        /// increases used slot count accordingly. Returns true if a slot was available and consumed.
        /// </summary>
        public static bool TryAssignToWork(GeoPhoenixFaction faction, FacilitySlotType slotType)
        {
            try
            {
                if (faction == null) return false;

                var pools = ResearchManufacturingSlotsManager.GetOrCreatePools(faction);
                FacilitySlotPool pool = slotType == FacilitySlotType.Research ? pools.Research : pools.Manufacturing;

                if (pool.ProvidedSlots <= 0 || pool.UsedSlots >= pool.ProvidedSlots)
                {
                    return false; // no free slots
                }

                ResearchManufacturingSlotsManager.SetUsedSlots(faction, slotType, pool.UsedSlots + 1);
                return true;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return false;
            }
        }

        /// <summary>
        /// Frees one Research/Manufacturing slot (if any is currently used). Returns true on success.
        /// </summary>
        public static bool TryUnassignFromWork(GeoPhoenixFaction faction, FacilitySlotType slotType)
        {
            try
            {
                if (faction == null) return false;

                var pools = ResearchManufacturingSlotsManager.GetOrCreatePools(faction);
                FacilitySlotPool pool = slotType == FacilitySlotType.Research ? pools.Research : pools.Manufacturing;

                if (pool.UsedSlots <= 0) return false;

                ResearchManufacturingSlotsManager.SetUsedSlots(faction, slotType, pool.UsedSlots - 1);
                return true;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return false;
            }
        }

        /// <summary>
        /// Returns (used, provided) for Research and Manufacturing. Useful for UI visibility/validation.
        /// </summary>
        public static (int used, int provided) GetWorkSlotUsage(GeoPhoenixFaction faction, FacilitySlotType slotType)
        {
            if (faction == null)
            {
                return (0, 0);
            }

            var pools = ResearchManufacturingSlotsManager.GetOrCreatePools(faction);
            FacilitySlotPool pool = slotType == FacilitySlotType.Research ? pools.Research : pools.Manufacturing;
            return (pool.UsedSlots, pool.ProvidedSlots);
        }

        #endregion // Personnel Management API (for Recruit/Personnel screen)
    }
    #endregion // Training Facility Rework
}