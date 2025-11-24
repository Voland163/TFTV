using Base.Serialization.General;
using HarmonyLib;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Entities.PhoenixBases.FacilityComponents;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Tactical.Entities.Abilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV.TFTVBaseRework
{
    // Made public so RecruitTrainingSessionSave is fully public (resolves CS0052 when referenced by public save fields).
    public static class TrainingFacilityRework
    {
        #region Config
        private const int BaseDurationDays = 6;
        private const int SlotsPerFacility = 2;
        private const int BaseTargetLevel = 4;
        private const int UpgradedTargetLevel = 5;

        private const int EndurancePerLevel = 2;
        private const int WillpowerPerLevel = 1;
        private const int SpeedPerLevel = 1;

        private static readonly Dictionary<string, float> DurationReductionResearch = new Dictionary<string, float>
        {
            { "NJ_Training_ResearchDef", 0.15f },
            { "PX_AntediluvianArchaeology_ResearchDef", 0.10f },
        };

        private static readonly string AdvancedLevelResearchId = "PX_EliteTraining_ResearchDef";
        #endregion

        #region Data Model (Recruit descriptor training ONLY)
        public sealed class RecruitTrainingSession
        {
            public int PersonnelId;
            public GeoUnitDescriptor Descriptor;
            public SpecializationDef TargetSpecialization;
            public int StartDay;
            public int DurationDays;
            public int TargetLevel;
            public bool Completed;
            public int VirtualLevelAchieved; // starts at 1

            public float ProgressFraction(int currentDay)
            {
                if (Completed) return 1f;
                int elapsed = currentDay - StartDay;
                return Math.Max(0f, Math.Min(1f, (float)elapsed / DurationDays));
            }
        }

        private static readonly List<RecruitTrainingSession> RecruitSessions = new List<RecruitTrainingSession>();

        // Tracks recruits whose stat gains must be applied AFTER AddRecruit (UI path).
        private static readonly Dictionary<int, int> _pendingPostRecruitStatApply = new Dictionary<int, int>();

        // Tracks cumulative level-based gains already applied.
        private static readonly Dictionary<int, int> _appliedStatLevels = new Dictionary<int, int>();
        #endregion

        #region Public API (Recruit descriptor training)
        public static bool QueueDescriptorTraining(GeoLevelController level, GeoUnitDescriptor descriptor, SpecializationDef spec)
        {
            try
            {
                if (level == null || descriptor == null || spec == null) return false;

                if (RecruitSessions.Any(s => s.Descriptor == descriptor)) return false;

                int providedSlots = CalculateProvidedTrainingSlots(level.PhoenixFaction);
                int usedSlots = RecruitSessions.Count(s => !s.Completed);
                if (usedSlots >= providedSlots) return false;

                int currentDay = level.Timing.Now.TimeSpan.Days;
                int targetLevel = DetermineTargetLevel(level.PhoenixFaction);
                int effectiveDuration = CalculateEffectiveDurationDays(level.PhoenixFaction);

                var recruitSession = new RecruitTrainingSession
                {
                    PersonnelId = PersonnelData.GetOrCreatePersonnelId(descriptor),
                    Descriptor = descriptor,
                    TargetSpecialization = spec,
                    StartDay = currentDay,
                    DurationDays = effectiveDuration,
                    TargetLevel = targetLevel,
                    Completed = false,
                    VirtualLevelAchieved = 1
                };
                RecruitSessions.Add(recruitSession);

                TFTVLogger.Always($"[Training] Queued recruit training: {descriptor.GetName()} Spec={spec.name} StartDay={currentDay} Duration={effectiveDuration} TargetLevel={targetLevel} Used/Provided={usedSlots + 1}/{providedSlots}");
                return true;
            }
            catch (Exception e) { TFTVLogger.Error(e); return false; }
        }

        public static RecruitTrainingSession GetRecruitSession(GeoUnitDescriptor descriptor)
        {
            if (descriptor == null) return null;
            var id = PersonnelData.GetPersonnelByDescriptor(descriptor)?.Id ?? 0;
            return RecruitSessions.FirstOrDefault(s => s.PersonnelId == id || s.Descriptor == descriptor);
        }

        public static int GetRecruitRemainingDays(GeoUnitDescriptor descriptor, GeoLevelController level)
        {
            var s = GetRecruitSession(descriptor);
            if (s == null || level == null) return 0;
            int currentDay = level.Timing.Now.TimeSpan.Days;
            int remaining = (s.StartDay + s.DurationDays) - currentDay;
            return Math.Max(0, remaining);
        }

        public static bool IsRecruitTrainingComplete(GeoUnitDescriptor descriptor, GeoLevelController level)
        {
            var s = GetRecruitSession(descriptor);
            if (descriptor == null)
            {
                TFTVLogger.Always("[Training] IsRecruitTrainingComplete called with null descriptor.");
                return false;
            }
            if (s == null)
            {
                TFTVLogger.Always($"[Training] IsRecruitTrainingComplete: No session found for {descriptor.GetName()}");
                return false;
            }
            if (level == null)
            {
                TFTVLogger.Always($"[Training] IsRecruitTrainingComplete: level null for {descriptor.GetName()}");
                return false;
            }

            int currentDay = level.Timing.Now.TimeSpan.Days;
            ForceRecruitProgressUpdate(s, currentDay);

            bool complete = s.Completed || currentDay - s.StartDay >= s.DurationDays || s.VirtualLevelAchieved >= s.TargetLevel;

            TFTVLogger.Always($"[Training] Completion check for {descriptor.GetName()}: Completed={s.Completed} AutoComplete={complete} Day={currentDay} StartDay={s.StartDay} Duration={s.DurationDays} VirtualLevel={s.VirtualLevelAchieved}/{s.TargetLevel}");
            return complete;
        }

        private static void ForceRecruitProgressUpdate(RecruitTrainingSession session, int currentDay)
        {
            try
            {
                if (session == null || session.Completed) return;

                int beforeLevel = session.VirtualLevelAchieved;
                bool beforeCompleted = session.Completed;
                ProcessRecruitSessionProgress(session, currentDay);
                if (session.VirtualLevelAchieved != beforeLevel || session.Completed != beforeCompleted)
                {
                    TFTVLogger.Always($"[Training] Progress tick (inline) Session={session.Descriptor.GetName()} Level {beforeLevel}->{session.VirtualLevelAchieved} Completed={session.Completed}");
                }
            }
            catch (Exception e) { TFTVLogger.Error(e); }
        }
        #endregion

        #region Progression
        private static void AdvanceAllTraining(GeoLevelController geoLevel, int deltaDays)
        {
            if (geoLevel?.PhoenixFaction == null || deltaDays <= 0) return;
            int currentDay = geoLevel.Timing.Now.TimeSpan.Days;
            TFTVLogger.Always($"[Training] AdvanceAllTraining day={currentDay} delta={deltaDays}");

            // Recruit descriptor sessions only (operative sessions removed).
            foreach (var session in RecruitSessions.ToList())
            {
                if (session.Completed) continue;
                ProcessRecruitSessionProgress(session, currentDay);
            }

            RecruitSessions.RemoveAll(s => s.Descriptor == null);
        }

        private static void ProcessRecruitSessionProgress(RecruitTrainingSession session, int currentDay)
        {
            try
            {
                float progress = session.ProgressFraction(currentDay);
                int totalLevelsToGain = session.TargetLevel - 1;
                int projectedLevel = 1 + (int)Math.Floor(progress * totalLevelsToGain);
                projectedLevel = Math.Min(session.TargetLevel, projectedLevel);

                int prevLevel = session.VirtualLevelAchieved;
                bool prevCompleted = session.Completed;

                if (projectedLevel > session.VirtualLevelAchieved)
                {
                    session.VirtualLevelAchieved = projectedLevel;
                }

                if (progress >= 1f || session.VirtualLevelAchieved >= session.TargetLevel)
                {
                    session.Completed = true;
                }

                if (prevLevel != session.VirtualLevelAchieved || prevCompleted != session.Completed)
                {
                    TFTVLogger.Always($"[Training] Recruit progress: {session.Descriptor.GetName()} Level {prevLevel}->{session.VirtualLevelAchieved} Completed={session.Completed} Progress={progress:0.00}");
                }
            }
            catch (Exception e) { TFTVLogger.Error(e); }
        }
        #endregion

        #region Finalization
        public static GeoCharacter FinalizeRecruitTraining(GeoLevelController level, GeoUnitDescriptor descriptor, GeoPhoenixBase targetBase, bool early = false)
        {
            try
            {
                var session = GetRecruitSession(descriptor);
                if (level == null || descriptor == null || targetBase == null || session == null) return null;
                var phoenix = level.PhoenixFaction;
                if (phoenix == null) return null;

                int finalLevel = early ? session.VirtualLevelAchieved : session.TargetLevel;
                if (descriptor.Progression != null)
                {
                    descriptor.Progression.Level = finalLevel;
                    descriptor.Progression.LearnPrimaryAbilities = true;
                }

                GeoCharacter character = level.CreateCharacterFromDescriptor(descriptor);
                if (character == null) return null;

                EnsureSpecialization(character, session.TargetSpecialization);
                character.LevelProgression?.SetLevel(finalLevel);
                ApplyCumulativeLevelGains(character, finalLevel);
                phoenix.AddRecruit(character, targetBase.Site);

                session.Completed = true;
                RemoveRecruitSession(session);

                TFTVLogger.Always($"[Training] Finalized recruit training: {character.DisplayName} Level={finalLevel} Early={early}");
                return character;
            }
            catch (Exception e) { TFTVLogger.Error(e); return null; }
        }

        public static GeoCharacter FinalizeRecruitTrainingForUI(GeoLevelController level, GeoUnitDescriptor descriptor, bool early)
        {
            try
            {
                if (level == null || descriptor == null) return null;
                var session = GetRecruitSession(descriptor);
                if (session == null)
                {
                    GeoCharacter cFallback = level.CreateCharacterFromDescriptor(descriptor);
                    if (cFallback != null)
                    {
                        EnsureSpecialization(cFallback, descriptor.Progression?.MainSpecDef);
                        ApplyCumulativeLevelGains(cFallback, cFallback.LevelProgression?.Level ?? 1);
                        TFTVLogger.Always($"[Training] FinalizeForUI fallback spawn: {cFallback.DisplayName}");
                    }
                    return cFallback;
                }

                int finalLevel = early ? session.VirtualLevelAchieved : session.TargetLevel;
                var spec = session.TargetSpecialization;

                if (descriptor.Progression != null)
                {
                    descriptor.Progression.Level = finalLevel;
                    descriptor.Progression.LearnPrimaryAbilities = true;
                }
                if (spec != null)
                {
                    OverrideDescriptorMainSpec(descriptor, spec, rebuildPersonalAbilities: false);
                }

                GeoCharacter character = level.CreateCharacterFromDescriptor(descriptor);
                if (character == null) return null;

                if (spec != null) EnsureSpecialization(character, spec);
                character.LevelProgression?.SetLevel(finalLevel);

                _pendingPostRecruitStatApply[character.Id] = finalLevel;

                session.Completed = true;
                RemoveRecruitSession(session);

                TFTVLogger.Always($"[Training] FinalizeForUI created character (pending stat gains): {character.DisplayName} Level={finalLevel} Early={early}");
                return character;
            }
            catch (Exception e) { TFTVLogger.Error(e); return null; }
        }
        #endregion

        #region Helpers / Internal
        private static int CalculateEffectiveDurationDays(GeoPhoenixFaction faction)
        {
            float duration = BaseDurationDays;
            if (faction?.Research != null)
            {
                foreach (var kv in DurationReductionResearch)
                {
                    if (faction.Research.HasCompleted(kv.Key)) duration *= (1f - kv.Value);
                }
            }
            return Math.Max(1, (int)Math.Ceiling(duration));
        }

        private static int DetermineTargetLevel(GeoPhoenixFaction faction)
        {
            return faction?.Research?.HasCompleted(AdvancedLevelResearchId) == true ? UpgradedTargetLevel : BaseTargetLevel;
        }

        internal static bool IsValidFacility(GeoPhoenixFacility facility)
        {
            if (facility?.Def == null) return false;
            return facility.GetComponent<ExperienceFacilityComponent>() != null &&
                   facility.State == GeoPhoenixFacility.FacilityState.Functioning &&
                   facility.IsPowered;
        }

        private static int CalculateProvidedTrainingSlots(GeoPhoenixFaction faction)
        {
            if (faction?.Bases == null) return 0;

            int facilityCount = faction.Bases
                .Where(b => b?.Layout != null)
                .SelectMany(b => b.Layout.Facilities)
                .Count(IsValidFacility);

            return facilityCount * SlotsPerFacility;
        }

        private static int CalculateUsedTrainingSlots() => RecruitSessions.Count(s => !s.Completed);

        private static void RemoveRecruitSession(RecruitTrainingSession session)
        {
            if (session == null) return;
            RecruitSessions.Remove(session);
        }

        internal static void EnsureSpecialization(GeoCharacter character, SpecializationDef specialization)
        {
            try
            {
                if (specialization == null || character == null) return;
                if (character.ClassTags != null && character.ClassTags.Contains(specialization.ClassTag)) return;
                character.Progression.MainSpecDef.ClassTag = specialization.ClassTag;
            }
            catch (Exception e) { TFTVLogger.Error(e); }
        }
        #endregion

        #region Public Helper API
        public static int GetEffectiveDurationDays(GeoPhoenixFaction faction) => CalculateEffectiveDurationDays(faction);
        public static int GetTargetLevel(GeoPhoenixFaction faction) => DetermineTargetLevel(faction);

        public static int GetProvidedTrainingSlots(GeoPhoenixFaction faction) => CalculateProvidedTrainingSlots(faction);
        public static int GetUsedTrainingSlots() => CalculateUsedTrainingSlots();

        #endregion

        #region Cumulative Stat Gains
        private static void ApplyCumulativeLevelGains(GeoCharacter character, int finalLevel)
        {
            try
            {
                if (character?.CharacterStats == null) return;

                int targetAppliedLevels = Math.Max(0, finalLevel - 1);
                int alreadyApplied = _appliedStatLevels.TryGetValue(character.Id, out var a) ? a : 0;
                int remainingLevels = targetAppliedLevels - alreadyApplied;
                if (remainingLevels <= 0)
                {
                    TFTVLogger.Always($"[Training] Stat gains skipped for {character.DisplayName}: alreadyApplied={alreadyApplied}, target={targetAppliedLevels}");
                    return;
                }

                int bonusEndurance = remainingLevels * EndurancePerLevel;
                int bonusWill      = remainingLevels * WillpowerPerLevel;
                int bonusSpeed     = remainingLevels * SpeedPerLevel;

                // Persist via bonus stats (not overwritten by LevelProgression.SetLevel).
                character.BonusStrength  += bonusEndurance;
                character.BonusWillpower += bonusWill;
                character.BonusSpeed     += bonusSpeed;

                _appliedStatLevels[character.Id] = targetAppliedLevels;

                // Force a stat rebuild WITHOUT resetting progression values.
                RefreshCharacterStats(character);

                TFTVLogger.Always($"[Training] Applied training bonuses to {character.DisplayName}: +" +
                                  $"{bonusEndurance} END +" +
                                  $"{bonusWill} WILL +" +
                                  $"{bonusSpeed} SPD (final level {finalLevel}, cumulative levels {targetAppliedLevels})");
            }
            catch (Exception e) { TFTVLogger.Error(e); }
        }

        // Uses reflection to call private GeoCharacter.UpdateStats(false).
        private static void RefreshCharacterStats(GeoCharacter character)
        {
            try
            {
                if (character == null) return;
                var m = typeof(GeoCharacter).GetMethod("UpdateStats", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (m != null)
                {
                    // parameter recalculateBodyparts = false
                    m.Invoke(character, new object[] { false });
                }
            }
            catch (Exception e) { TFTVLogger.Error(e); }
        }
        #endregion

        #region Clear
        public static void ClearAllSessions()
        {
            try { RecruitSessions.Clear(); }
            catch (Exception e) { TFTVLogger.Error(e); }
        }
        #endregion

        #region Personnel Management (Immediate hires)
        public static GeoCharacter CreateOperativeFromDescriptor(
            GeoLevelController level,
            GeoUnitDescriptor descriptor,
            GeoPhoenixBase targetBase,
            SpecializationDef mainClass)
        {
            try
            {
                if (level == null || descriptor == null || targetBase == null || mainClass == null) return null;
                GeoPhoenixFaction phoenix = level.PhoenixFaction;
                if (phoenix == null) return null;

                GeoCharacter character = level.CreateCharacterFromDescriptor(descriptor);
                if (character == null) return null;

                character.LevelProgression?.SetLevel(1);
                EnsureSpecialization(character, mainClass);
                phoenix.AddRecruit(character, targetBase.Site);
                return character;
            }
            catch (Exception e) { TFTVLogger.Error(e); return null; }
        }
        #endregion

        #region Harmony Patches
        [HarmonyPatch(typeof(GeoLevelController), "DailyUpdate")]
        internal static class GeoLevelController_DailyUpdate_Training
        {
            private static void Postfix(GeoLevelController __instance)
            {
                try { AdvanceAllTraining(__instance, 1); }
                catch (Exception e) { TFTVLogger.Error(e); }
            }
        }

       

        [HarmonyPatch(typeof(GeoPhoenixFaction), "AddRecruit")]
        internal static class GeoPhoenixFaction_AddRecruit_TrainingStats_Postfix
        {
            private static void Postfix(GeoPhoenixFaction __instance, GeoCharacter recruit, IGeoCharacterContainer toContainer)
            {
                try { TryApplyDeferredTrainingStats(recruit); }
                catch (Exception e) { TFTVLogger.Error(e); }
            }
        }

        [HarmonyPatch(typeof(GeoPhoenixFaction), "AddRecruitToContainerFinal")]
        internal static class GeoPhoenixFaction_AddRecruitToContainerFinal_TrainingStats_Postfix
        {
            private static void Postfix(GeoPhoenixFaction __instance, GeoCharacter recruit)
            {
                try { TryApplyDeferredTrainingStats(recruit); }
                catch (Exception e) { TFTVLogger.Error(e); }
            }
        }

        private static void TryApplyDeferredTrainingStats(GeoCharacter recruit)
        {
            if (recruit == null) return;
            if (_pendingPostRecruitStatApply.TryGetValue(recruit.Id, out int finalLevel))
            {
                ApplyCumulativeLevelGains(recruit, finalLevel);
                _pendingPostRecruitStatApply.Remove(recruit.Id);
                TFTVLogger.Always($"[Training] Deferred stat gains applied to {recruit.DisplayName} (Level {finalLevel}) via {nameof(TryApplyDeferredTrainingStats)}.");
            }
        }

        // Optional fallback after daily update
        [HarmonyPatch(typeof(GeoLevelController), "DailyUpdate")]
        internal static class GeoLevelController_DailyUpdate_TrainingDeferredFallback
        {
            private static void Postfix(GeoLevelController __instance)
            {
                try
                {
                    if (_pendingPostRecruitStatApply.Count == 0) return;
                    foreach (var soldier in __instance?.PhoenixFaction?.Soldiers ?? Enumerable.Empty<GeoCharacter>())
                    {
                        if (_pendingPostRecruitStatApply.ContainsKey(soldier.Id))
                        {
                            TFTVLogger.Always($"[Training] Fallback daily scan applying deferred stats to {soldier.DisplayName}.");
                            TryApplyDeferredTrainingStats(soldier);
                        }
                    }
                }
                catch (Exception e) { TFTVLogger.Error(e); }
            }
        }
        #endregion

        #region Override Descriptor Spec
        public static bool OverrideDescriptorMainSpec(GeoUnitDescriptor descriptor, SpecializationDef newSpec, bool rebuildPersonalAbilities = true)
        {
            try
            {
                if (descriptor == null || newSpec == null) return false;

                var oldProg = descriptor.Progression;
                Dictionary<int, TacticalAbilityDef> personal = oldProg?.PersonalAbilities != null
                    ? new Dictionary<int, TacticalAbilityDef>(oldProg.PersonalAbilities)
                    : new Dictionary<int, TacticalAbilityDef>();

                if (rebuildPersonalAbilities)
                {
                    personal.Clear();
                    try
                    {
                        var settings = TFTVMain.Main.Settings;
                        if (settings?.OrderOfPersonalPerks != null && settings.PersonalPerks != null)
                        {
                            string className = newSpec.ClassTag?.className;
                            var order = settings.OrderOfPersonalPerks;
                            var classSpecCfg = settings.ClassSpecializations.FirstOrDefault(cs => cs.ClassName == className);
                            List<string> exclusion = classSpecCfg.MainSpec != null ? new List<string>(classSpecCfg.MainSpec) : new List<string>();

                            for (int i = 0; i < order.Length; i++)
                            {
                                var perkKey = order[i];
                                if (string.IsNullOrEmpty(perkKey)) continue;
                                var perkDef = settings.PersonalPerks.FirstOrDefault(pp => pp.PerkKey == perkKey);

                                string abilityName = null;
                                int spCost = 0;

                                var templateName = descriptor.UnitType?.TemplateDef?.Data?.Name;
                                if (!string.IsNullOrEmpty(templateName) &&
                                    settings.SpecialCharacterPersonalSkills.TryGetValue(templateName, out var specialMap) &&
                                    specialMap != null &&
                                    specialMap.TryGetValue(i, out var specialAbilityName))
                                {
                                    abilityName = specialAbilityName;
                                }
                                else
                                {
                                    (abilityName, spCost) = perkDef.GetPerk(settings, className, descriptor.Faction?.GetPPName(), exclusion);
                                }

                                if (string.IsNullOrEmpty(abilityName)) continue;

                                TacticalAbilityDef abilityDef = null;
                                try { abilityDef = TFTVMain.Main.DefCache.GetDef<TacticalAbilityDef>(abilityName); } catch { }
                                if (abilityDef == null) continue;

                                if (!personal.ContainsKey(i)) personal[i] = abilityDef;
                            }
                        }
                    }
                    catch (Exception e) { TFTVLogger.Error(e); }
                }

                var newProg = new GeoUnitDescriptor.ProgressionDescriptor(newSpec, personal);
                if (oldProg != null)
                {
                    newProg.Level = oldProg.Level;
                    newProg.SecondarySpecDef = oldProg.SecondarySpecDef;
                    newProg.LearnPrimaryAbilities = oldProg.LearnPrimaryAbilities;
                    newProg.ExtraAbilities.AddRange(oldProg.ExtraAbilities);
                }

                descriptor.Progression = newProg;

                try
                {
                    var f = typeof(GeoUnitDescriptor).GetField("_personalAbilityTrack", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    f?.SetValue(descriptor, null);
                }
                catch (Exception e) { TFTVLogger.Error(e); }

                return true;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return false;
            }
        }
        #endregion

        #region Recruit Training Sessions - Save/Load
        [SerializeType(SerializeMembersByDefault = SerializeMembersType.SerializeAll)]
        public sealed class RecruitTrainingSessionSave
        {
            public int PersonnelId;
            public string DescriptorName;
            public string MainSpecName;
            public int StartDay;
            public int DurationDays;
            public int TargetLevel;
            public int VirtualLevelAchieved;
            public bool Completed;
            public string IdentityName;
            public GeoCharacterSex IdentitySex;
        }

        public static List<RecruitTrainingSessionSave> CreateRecruitSessionsSnapshot()
        {
            var list = new List<RecruitTrainingSessionSave>();
            foreach (var s in RecruitSessions)
            {

                var personnelId = s.PersonnelId != 0 ? s.PersonnelId : PersonnelData.GetOrCreatePersonnelId(s.Descriptor);

                list.Add(new RecruitTrainingSessionSave
                {
                    PersonnelId = personnelId,
                    DescriptorName = s.Descriptor?.GetName(),
                    MainSpecName = s.TargetSpecialization?.name,
                    StartDay = s.StartDay,
                    DurationDays = s.DurationDays,
                    TargetLevel = s.TargetLevel,
                    VirtualLevelAchieved = s.VirtualLevelAchieved,
                    Completed = s.Completed,
                    IdentityName = s.Descriptor?.Identity?.Name,
                    IdentitySex = s.Descriptor?.Identity?.Sex ?? GeoCharacterSex.None
                });
            }
            return list;
        }

        public static void LoadRecruitSessionsSnapshot(GeoLevelController level, IEnumerable<RecruitTrainingSessionSave> snapshot)
        {
            if (level?.PhoenixFaction == null || snapshot == null) return;

            TFTVLogger.Always($"[Training] Loading {snapshot.Count()} recruit sessions. Phoenix bases={level.PhoenixFaction.Bases.Count()}");

            foreach (var save in snapshot)
            {
                try
                {
                    GeoUnitDescriptor descriptor = PersonnelData.GetPersonnelById(save.PersonnelId)?.Descriptor;

                    TFTVLogger.Always($"[Training] Restoring session PersonnelId={save.PersonnelId} DescriptorFound={(descriptor != null)} Completed={save.Completed}");

                    if (descriptor == null)
                    {
                        var context = level.CharacterGenerator.GenerateCharacterGeneratorContext(level.PhoenixFaction);
                        descriptor = level.CharacterGenerator.GenerateRandomUnit(context);
                    }

                    if (descriptor == null) continue;

                    var personnel = PersonnelData.EnsurePersonnelFromSave(save.PersonnelId, save.DescriptorName, save.IdentityName, save.IdentitySex, save.MainSpecName);
                    personnel.Descriptor = descriptor;
                    PersonnelData.SyncFromNakedRecruits(level.PhoenixFaction);

                    PersonnelData.EnsureDescriptorInPool(level.PhoenixFaction, descriptor);

                    if (!string.IsNullOrEmpty(save.IdentityName))
                    {
                        descriptor.Identity = new GeoUnitDescriptor.IdentityDescriptor(save.IdentityName, save.IdentitySex);
                        TFTVLogger.Always($"[Training] Set descriptor identity to {save.IdentityName} ({save.IdentitySex})");

                    }

                    SpecializationDef spec = null;
                    if (!string.IsNullOrEmpty(save.MainSpecName))
                    {
                        try { spec = TFTVMain.Main.DefCache.GetDef<SpecializationDef>(save.MainSpecName); } catch { }
                    }
                    if (spec != null)
                    {
                        OverrideDescriptorMainSpec(descriptor, spec, rebuildPersonalAbilities: true);
                        TFTVLogger.Always($"[Training] Restored main spec {spec.name} for {descriptor.GetName()}");
                    }

                    RecruitSessions.Add(new RecruitTrainingSession
                    {
                        PersonnelId = personnel.Id,
                        Descriptor = descriptor,
                        TargetSpecialization = spec,
                        StartDay = save.StartDay,
                        DurationDays = save.DurationDays,
                        TargetLevel = save.TargetLevel,
                        VirtualLevelAchieved = save.VirtualLevelAchieved,
                        Completed = save.Completed
                    });

                    TFTVLogger.Always($"[Training] Session restored: PersonnelId={personnel.Id} Descriptor={descriptor.GetName()} TargetLevel={save.TargetLevel} Completed={save.Completed}");
                }
                catch (Exception e) { TFTVLogger.Error(e); }
            }
        }
        #endregion

        #region Class Availability (Research-Gated)

        // Specialization Def IDs (adjust if your actual Def names differ)
        private const string AssaultSpecDefId = "AssaultSpecializationDef";
        private const string HeavySpecDefId = "HeavySpecializationDef";
        private const string SniperSpecDefId = "SniperSpecializationDef";
        private const string InfiltratorSpecDefId = "InfiltratorSpecializationDef";
        private const string PriestSpecDefId = "PriestSpecializationDef";
        private const string BerserkerSpecDefId = "BerserkerSpecializationDef";
        private const string TechnicianSpecDefId = "TechnicianSpecializationDef";

        // Research IDs gating optional classes
        private const string InfiltratorResearchId = "SYN_InfiltratorTech_ResearchDef";
        private const string PriestResearchId = "ANU_AnuPriest_ResearchDef";
        private const string BerserkerResearchId = "ANU_Berserker_ResearchDef";
        private const string TechnicianResearchId = "NJ_Technician_ResearchDef";

        /// <summary>
        /// Returns the set of SpecializationDefs the player can train right now based on completed research.
        /// Always includes Assault, Heavy, Sniper; adds others if their research is completed.
        /// </summary>
        public static IReadOnlyList<SpecializationDef> GetAvailableTrainingSpecializations(GeoPhoenixFaction faction)
        {
            var list = new List<SpecializationDef>();
            if (faction?.GeoLevel == null) return list;

            var defCache = TFTVMain.Main.DefCache;

            void TryAdd(string defId)
            {
                try
                {
                    var def = defCache.GetDef<SpecializationDef>(defId);
                    if (def != null && !list.Contains(def))
                    {
                        list.Add(def);
                    }
                }
                catch { /* ignore missing defs */ }
            }

            // Always available
            TryAdd(AssaultSpecDefId);
            TryAdd(HeavySpecDefId);
            TryAdd(SniperSpecDefId);

            var research = faction.Research;
            if (research != null)
            {
                if (research.HasCompleted(InfiltratorResearchId)) TryAdd(InfiltratorSpecDefId);
                if (research.HasCompleted(PriestResearchId))      TryAdd(PriestSpecDefId);
                if (research.HasCompleted(BerserkerResearchId))   TryAdd(BerserkerSpecDefId);
                if (research.HasCompleted(TechnicianResearchId))  TryAdd(TechnicianSpecDefId);
            }

            return list;
        }

        /// <summary>
        /// Queues training automatically picking a valid facility with a free slot; player supplies only class.
        /// Returns true on success.
        /// </summary>
        public static bool QueueDescriptorTrainingAutoFacility(GeoLevelController level, GeoUnitDescriptor descriptor, SpecializationDef spec)
        {
            try
            {
                if (level == null || descriptor == null || spec == null) return false;

                // Validate specialization is currently allowed.
                var allowed = GetAvailableTrainingSpecializations(level.PhoenixFaction);
                if (!allowed.Contains(spec))
                {
                    TFTVLogger.Always($"[Training] Spec {spec.name} not available via research gating.");
                    return false;
                }


                int providedSlots = CalculateProvidedTrainingSlots(level.PhoenixFaction);
                int usedSlots = CalculateUsedTrainingSlots();
                if (usedSlots >= providedSlots)
                {
                    TFTVLogger.Always("[Training] No available training facility slot found.");
                    return false;
                }

                // Delegate to existing queue logic (with facility).
                return QueueDescriptorTraining(level, descriptor, spec);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return false;
            }
        }
        #endregion
    }
}