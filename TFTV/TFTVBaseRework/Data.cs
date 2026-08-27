using Base;
using Base.Core;
using Base.Serialization.General;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using TFTV.TFTVIncidents;
using static TFTV.TFTVBaseRework.Workers;

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
        public int Id;
        public GeoCharacter Character;
        public PersonnelAssignment Assignment;
        public SpecializationDef TrainingSpec;
    }

    [SerializeType(SerializeMembersByDefault = SerializeMembersType.SerializeAll)]
    public sealed class PersonnelAssignmentSave
    {
        public int GeoUnitId;
        public string MainSpecName;
        public PersonnelAssignment Assignment;
    }

    internal static class PersonnelRestrictions
    {
        private const string JustAGruntAbilityDefName = "JustAGrunt_AbilityDef";
        private const string HiddenFromOperativesAbilityDefName = "HiddenFromOperatives_AbilityDef";
        private const string DismissedOperativeAbilityDefName = "DismissedOperative_AbilityDef";

        internal static PassiveModifierAbilityDef JustAGruntAbility;
        internal static PassiveModifierAbilityDef HiddenFromOperativesAbility;
        internal static PassiveModifierAbilityDef DismissedOperativeAbility;

        internal static bool EnsureJustAGrunt(GeoCharacter character, string source)
        {
            if (!BaseReworkCheck.BaseReworkEnabled || character?.Progression == null)
            {
                return false;
            }

            PassiveModifierAbilityDef justAGrunt = ResolveJustAGruntAbility();
            if (justAGrunt == null)
            {
                TFTVLogger.Always($"[JustAGrunt] Ability def not available. Source={source ?? "Unknown"}");
                return false;
            }

            if (HasMarkerAbility(character, justAGrunt))
            {
                return false;
            }

            character.Progression.AddAbility(justAGrunt);
            TFTVLogger.Always($"[JustAGrunt] Added to {character.DisplayName}. Source={source ?? "Unknown"}");
            return true;
        }

        internal static bool IsHiddenFromOperatives(GeoCharacter character)
        {
            return HasMarkerAbility(character, ResolveHiddenFromOperativesAbility());
        }

        internal static bool MarkHiddenFromOperatives(GeoCharacter character)
        {
            return AddMarkerAbility(character, ResolveHiddenFromOperativesAbility(), "HiddenFromOperatives");
        }

        internal static bool ClearHiddenFromOperatives(GeoCharacter character)
        {
            return RemoveMarkerAbility(character, ResolveHiddenFromOperativesAbility(), "HiddenFromOperatives");
        }

        internal static bool IsDismissedOperative(GeoCharacter character)
        {
            return HasMarkerAbility(character, ResolveDismissedOperativeAbility());
        }

        internal static bool MarkDismissedOperative(GeoCharacter character)
        {
            return AddMarkerAbility(character, ResolveDismissedOperativeAbility(), "DismissedOperative");
        }

        internal static bool ClearDismissedOperative(GeoCharacter character)
        {
            return RemoveMarkerAbility(character, ResolveDismissedOperativeAbility(), "DismissedOperative");
        }

        internal static int GetRedeployCost(GeoCharacter character)
        {
            int level = character?.LevelProgression?.Level ?? 1;
            return Math.Max(0, (level - 1) * 10);
        }

        private static PassiveModifierAbilityDef ResolveJustAGruntAbility()
        {
            if (JustAGruntAbility != null)
            {
                return JustAGruntAbility;
            }

            try
            {
                JustAGruntAbility = TFTVMain.Main.DefCache.GetDef<PassiveModifierAbilityDef>(JustAGruntAbilityDefName);
            }
            catch
            {
                JustAGruntAbility = null;
            }

            return JustAGruntAbility;
        }

        private static PassiveModifierAbilityDef ResolveHiddenFromOperativesAbility()
        {
            if (HiddenFromOperativesAbility != null)
            {
                return HiddenFromOperativesAbility;
            }

            try
            {
                HiddenFromOperativesAbility = TFTVMain.Main.DefCache.GetDef<PassiveModifierAbilityDef>(HiddenFromOperativesAbilityDefName);
            }
            catch
            {
                HiddenFromOperativesAbility = null;
            }

            return HiddenFromOperativesAbility;
        }

        private static PassiveModifierAbilityDef ResolveDismissedOperativeAbility()
        {
            if (DismissedOperativeAbility != null)
            {
                return DismissedOperativeAbility;
            }

            try
            {
                DismissedOperativeAbility = TFTVMain.Main.DefCache.GetDef<PassiveModifierAbilityDef>(DismissedOperativeAbilityDefName);
            }
            catch
            {
                DismissedOperativeAbility = null;
            }

            return DismissedOperativeAbility;
        }

        /// <summary>
        /// Memo of marker lookups per character.
        ///
        /// HasMarkerAbility is reached from the GeoPhoenixFaction.Soldiers postfix, so it runs for
        /// every soldier on every enumeration of that property - which the geoscape UI does
        /// constantly. Scanning the character's whole ability list each time (with a case-insensitive
        /// string compare per ability that is not reference-equal) was costing thousands of string
        /// comparisons per enumeration.
        ///
        /// The memo is keyed on the character object, so it disappears by itself when a save is
        /// loaded, and it is rechecked whenever the character's ability count changes - which covers
        /// both the marker add/remove paths below and any external change to the ability list.
        /// </summary>
        private sealed class MarkerAbilityMemo
        {
            public int AbilityCount = -1;
            public readonly Dictionary<TacticalAbilityDef, bool> Results = new Dictionary<TacticalAbilityDef, bool>();
        }

        private static readonly ConditionalWeakTable<GeoCharacter, MarkerAbilityMemo> _markerMemos =
            new ConditionalWeakTable<GeoCharacter, MarkerAbilityMemo>();

        private static bool HasMarkerAbility(GeoCharacter character, TacticalAbilityDef marker)
        {
            IReadOnlyList<TacticalAbilityDef> abilities = character?.Progression?.Abilities;
            if (abilities == null || marker == null)
            {
                return false;
            }

            MarkerAbilityMemo memo = _markerMemos.GetOrCreateValue(character);

            if (memo.AbilityCount != abilities.Count)
            {
                memo.AbilityCount = abilities.Count;
                memo.Results.Clear();
            }
            else if (memo.Results.TryGetValue(marker, out bool cached))
            {
                return cached;
            }

            bool found = false;
            for (int i = 0; i < abilities.Count; i++)
            {
                if (AbilityMatches(abilities[i], marker))
                {
                    found = true;
                    break;
                }
            }

            memo.Results[marker] = found;
            return found;
        }

        /// <summary>
        /// Drops the memo for a character whose marker abilities were just changed in place, i.e.
        /// without the ability count moving.
        /// </summary>
        private static void InvalidateMarkerMemo(GeoCharacter character)
        {
            if (character == null)
            {
                return;
            }

            if (_markerMemos.TryGetValue(character, out MarkerAbilityMemo memo))
            {
                memo.AbilityCount = -1;
                memo.Results.Clear();
            }
        }

        private static bool AddMarkerAbility(GeoCharacter character, PassiveModifierAbilityDef marker, string markerName)
        {
            if (character?.Progression == null || marker == null)
            {
                return false;
            }

            if (HasMarkerAbility(character, marker))
            {
                return false;
            }

            character.Progression.AddAbility(marker);
            InvalidateMarkerMemo(character);
            TFTVLogger.Always($"[{markerName}] Added marker to {character.DisplayName}.");
            return true;
        }

        private static bool RemoveMarkerAbility(GeoCharacter character, PassiveModifierAbilityDef marker, string markerName)
        {
            if (character?.Progression == null || marker == null)
            {
                return false;
            }

            List<TacticalAbilityDef> abilities = Traverse.Create(character.Progression)
                .Field("_abilities")
                .GetValue<List<TacticalAbilityDef>>();

            if (abilities == null)
            {
                return false;
            }

            int removed = abilities.RemoveAll(ability => AbilityMatches(ability, marker));
            if (removed > 0)
            {
                InvalidateMarkerMemo(character);
                TFTVLogger.Always($"[{markerName}] Removed marker from {character.DisplayName}.");
                return true;
            }

            return false;
        }

        private static bool AbilityMatches(TacticalAbilityDef ability, TacticalAbilityDef marker)
        {
            if (ability == marker)
            {
                return true;
            }

            if (ability == null || marker == null)
            {
                return false;
            }

            return string.Equals(ability.name, marker.name, StringComparison.OrdinalIgnoreCase);
        }

        internal static bool HasJustAGrunt(GeoCharacter character)
        {
            return HasMarkerAbility(character, ResolveJustAGruntAbility());
        }

        internal static bool CanGainAffinities(GeoCharacter character)
        {
            return character != null && !HasJustAGrunt(character);
        }

        internal static bool CanContributeToIncidents(GeoCharacter character)
        {
            return character != null && !HasJustAGrunt(character);
        }

        internal static bool CanBeAssignedToManufacturingOrResearch(GeoCharacter character)
        {
            return character != null && !HasJustAGrunt(character);
        }

        /// <summary>
        /// Grunts are rank and file: they can be housed and fed, but they cannot be spent on
        /// standing up an Outpost or activating a Base.
        /// </summary>
        internal static bool CanBeUsedForBaseActivation(GeoCharacter character)
        {
            return character != null && !HasJustAGrunt(character);
        }
    }

    internal static class PersonnelData
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;



        private const string LogPrefix = "[PersonnelData]";

        private static readonly Dictionary<int, PersonnelInfo> _assignments = new Dictionary<int, PersonnelInfo>();
        public static Dictionary<int, PersonnelInfo> Assignments => _assignments;

        private static bool _pendingInitialPersonnelGrant;

        internal static void MarkNewGameForInitialPersonnel()
        {
            if (!BaseReworkCheck.BaseReworkEnabled)
            {
                return;
            }

            _pendingInitialPersonnelGrant = true;
        }

        internal static void TryGrantInitialPersonnel(GeoLevelController level)
        {
            if (!BaseReworkCheck.BaseReworkEnabled || !_pendingInitialPersonnelGrant)
            {
                return;
            }


            _pendingInitialPersonnelGrant = false;

            int difficulty = level?.CurrentDifficultyLevel?.Order ?? 0;
            int count = Math.Max(0, 7 - difficulty);
            if (count <= 0)
            {
                return;
            }

            int added = AddIncidentPersonnelReward(level.PhoenixFaction, count);
            TFTVLogger.Always($"[PersonnelData] Granted {added}/{count} initial personnel for difficulty {difficulty}.");
        }





        /// <summary>
        /// Only unlocks the Recruit tab in the roster UI.
        /// Does not modify recruitment gameplay functionality flags.
        /// </summary>
        [HarmonyPatch(typeof(UIModuleGeoRosterTabs), "CheckAvailableTabs")]
        public static class AlwaysUnlockRecruitTabPatch
        {
            private static readonly AccessTools.FieldRef<UIModuleGeoRosterTabs, bool> RecruitsUnlockedRef =
                AccessTools.FieldRefAccess<UIModuleGeoRosterTabs, bool>("_recruitsUnlocked");

            [HarmonyPostfix]
            public static void Postfix(UIModuleGeoRosterTabs __instance)
            {
                if (!BaseReworkCheck.BaseReworkEnabled)
                {
                    return;
                }

                RecruitsUnlockedRef(__instance) = true;
                __instance.RecruitsTab.SetInteractable(true);
                __instance.RecruitsTab.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Prevents overflow when opening recruit UI without the underlying recruitment timer initialized.
        /// </summary>
        [HarmonyPatch(typeof(GeoPhoenixFaction), "GetNextRecruitRegeneration")]
        public static class SafeRecruitRegenerationTimePatch
        {
            private static readonly AccessTools.FieldRef<GeoPhoenixFaction, TimeUnit> LastNakedRecruitRefreshRef =
                AccessTools.FieldRefAccess<GeoPhoenixFaction, TimeUnit>("_lastNakedRecruitRefresh");

            [HarmonyPrefix]
            public static bool Prefix(GeoPhoenixFaction __instance, ref TimeUnit __result)
            {
                if (!LastNakedRecruitRefreshRef(__instance).IsValid)
                {
                    __result = __instance.GeoLevel.Timing.Now;
                    return false;
                }

                return true;
            }
        }

        private const int PsychoSociologyActivationWeight = 3;

        /// <summary>
        /// How many personnel this character counts as when consumed for base activation.
        /// Psycho-Sociology personnel count triple (Administration).
        /// </summary>
        internal static int GetActivationWeight(GeoCharacter character)
        {
            if (character != null
                && LeaderSelection.TryGetCurrentAffinity(character, out LeaderSelection.AffinityApproach approach, out _)
                && approach == LeaderSelection.AffinityApproach.PsychoSociology)
            {
                return PsychoSociologyActivationWeight;
            }

            return 1;
        }

        /// <summary>
        /// Personnel that may be spent on setting up an Outpost or activating a Base:
        /// everyone who is not on field duty, not already in training, and not just a grunt.
        /// </summary>
        internal static List<PersonnelInfo> GetPersonnelEligibleForBaseActivation(GeoPhoenixFaction faction)
        {
            if (!BaseReworkCheck.BaseReworkEnabled || faction == null)
            {
                return new List<PersonnelInfo>();
            }

            return _assignments.Values
                .Where(person => person?.Character != null && person.Character.Faction == faction)
                .Where(person => PersonnelRestrictions.CanBeUsedForBaseActivation(person.Character))
                .Where(person => person.Assignment == PersonnelAssignment.Unassigned
                    || person.Assignment == PersonnelAssignment.Research
                    || person.Assignment == PersonnelAssignment.Manufacturing)
                .OrderBy(person => GetBaseActivationPriority(person.Assignment))
                .ThenByDescending(person => GetActivationWeight(person.Character))
                .ThenBy(person => person.Id)
                .ToList();
        }

        /// <summary>
        /// The set the game would spend on its own, used to pre-fill the selection dialog and as the
        /// fallback when no selection is made. Returns null when the requirement cannot be met.
        /// </summary>
        internal static List<PersonnelInfo> PickDefaultPersonnelForBaseActivation(GeoPhoenixFaction faction, int requiredPersonnel)
        {
            if (requiredPersonnel <= 0)
            {
                return null;
            }

            List<PersonnelInfo> ordered = GetPersonnelEligibleForBaseActivation(faction);

            if (ordered.Sum(person => GetActivationWeight(person.Character)) < requiredPersonnel)
            {
                return null;
            }

            // Fill by assignment priority (Unassigned, then Research, then Manufacturing).
            // Within a priority group, spend Psycho-Sociology administrators (weight 3) while their
            // full weight still fits the remaining requirement, and top up with regular personnel.
            List<PersonnelInfo> picked = new List<PersonnelInfo>();
            int remaining = requiredPersonnel;

            foreach (PersonnelInfo person in ordered)
            {
                if (remaining <= 0)
                {
                    break;
                }

                int weight = GetActivationWeight(person.Character);
                if (weight > remaining)
                {
                    continue;
                }

                picked.Add(person);
                remaining -= weight;
            }

            // Only Psycho-Sociology personnel are left and the remainder is 1-2:
            // overshoot with the highest-priority one rather than fail.
            if (remaining > 0)
            {
                PersonnelInfo filler = ordered.FirstOrDefault(person => !picked.Contains(person));
                if (filler == null)
                {
                    return null;
                }

                picked.Add(filler);
                remaining -= GetActivationWeight(filler.Character);
            }

            return remaining > 0 ? null : picked;
        }

        /// <summary>
        /// Spends exactly the personnel the player picked. They are dismissed for good, as with the
        /// automatic selection.
        /// </summary>
        internal static bool TryConsumeSelectedPersonnelForBaseActivation(
            GeoPhoenixFaction faction,
            IList<PersonnelInfo> selection,
            int requiredPersonnel)
        {
            if (!BaseReworkCheck.BaseReworkEnabled || faction == null || requiredPersonnel <= 0 || selection == null)
            {
                return false;
            }

            HashSet<int> eligibleIds = new HashSet<int>(GetPersonnelEligibleForBaseActivation(faction).Select(person => person.Id));

            List<PersonnelInfo> toConsume = selection
                .Where(person => person?.Character != null && eligibleIds.Contains(person.Id))
                .Distinct()
                .ToList();

            if (toConsume.Count != selection.Count
                || toConsume.Sum(person => GetActivationWeight(person.Character)) < requiredPersonnel)
            {
                TFTVLogger.Always($"[PersonnelData] Rejected personnel selection for base activation: {toConsume.Count}/{selection.Count} still eligible, {requiredPersonnel} required.");
                return false;
            }

            ConsumePersonnelForBaseActivation(faction, toConsume);
            return true;
        }

        internal static bool TryConsumePersonnelForBaseActivation(GeoPhoenixFaction faction, int requiredPersonnel)
        {
            if (!BaseReworkCheck.BaseReworkEnabled || faction == null)
            {
                return false;
            }

            List<PersonnelInfo> toConsume = PickDefaultPersonnelForBaseActivation(faction, requiredPersonnel);
            if (toConsume == null)
            {
                return false;
            }

            ConsumePersonnelForBaseActivation(faction, toConsume);
            return true;
        }

        private static void ConsumePersonnelForBaseActivation(GeoPhoenixFaction faction, IEnumerable<PersonnelInfo> toConsume)
        {
            foreach (PersonnelInfo person in toConsume)
            {
                RemovePersonnel(faction, person);
                faction.KillCharacter(person.Character, CharacterDeathReason.Dismissed);
            }
        }

        private static int GetBaseActivationPriority(PersonnelAssignment assignment)
        {
            switch (assignment)
            {
                case PersonnelAssignment.Unassigned:
                    return 0;
                case PersonnelAssignment.Research:
                    return 1;
                case PersonnelAssignment.Manufacturing:
                    return 2;
                default:
                    return int.MaxValue;
            }
        }

        internal static void ClearAssignments()
        {
            _assignments.Clear();
            TFTVLogger.Always("[PersonnelData] Cleared assignments and personnel pool.");
        }



        private static PersonnelInfo FindPersonnel(GeoCharacter character)
        {
            if (character == null) return null;
            _assignments.TryGetValue(character.Id, out var info);
            return info;
        }

        private static PersonnelInfo FindPersonnel(int unitId)
        {
            if (unitId <= 0) return null;
            _assignments.TryGetValue(unitId, out var info);
            return info;
        }

        private static PersonnelInfo CreatePersonnelRecord(GeoCharacter character)
        {
            var info = new PersonnelInfo
            {
                Id = character.Id,
                Character = character,
                Assignment = PersonnelAssignment.Unassigned,
            };

            if (character != null && !_assignments.ContainsKey(character.Id))
            {
                _assignments[character.Id] = info;
            }

            return info;
        }

        internal static void UpdateDismissedPersonnelRecord(GeoCharacter character)
        {
            if (character == null)
            {
                return;
            }

            PersonnelInfo info = FindPersonnel(character) ?? FindPersonnel(character.Id) ?? CreatePersonnelRecord(character);
            int previousId = info.Id;

            info.Character = character;
            info.Id = character.Id;
            info.Assignment = PersonnelAssignment.Unassigned;
            info.TrainingSpec = null;
            _assignments[character.Id] = info;

            TFTVLogger.Always($"{LogPrefix} UpdateDismissedPersonnelRecord Name={character.DisplayName} PreviousId={previousId} NewId={character.Id} Hidden={GeoCharacterFilter.HiddenOperativeMarkerFilter.ShouldHide(character)} Dismissed={PersonnelRestrictions.IsDismissedOperative(character)}");
        }


        internal static int GetOrCreatePersonnelId(GeoCharacter character)
        {
            var info = FindPersonnel(character) ?? CreatePersonnelRecord(character);
            return info.Id;
        }


        internal static PersonnelInfo GetPersonnelByUnitId(int unitId)
        {
            return _assignments.TryGetValue(unitId, out var info) ? info : null;
        }




        private static GeoCharacter CreateHiddenCharacterFromDescriptor(GeoLevelController level, GeoPhoenixFaction faction, GeoUnitDescriptor descriptor)
        {
            try
            {
                GeoPhoenixBase targetBase = faction?.Bases?.FirstOrDefault();
                GeoSite site = targetBase?.Site;
                if (level == null || faction == null || descriptor == null || site == null)
                {
                    return null;
                }

                GeoCharacter character = level.CreateCharacterFromDescriptor(descriptor);
                if (character == null)
                {
                    return null;
                }

                GeoCharacterFilter.HiddenOperativeMarkerFilter.ApplyHiddenMarker(character);
                character.LevelProgression?.SetLevel(1);
                faction.AddRecruit(character, site);
                return character;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return null;
            }

        }

        private static void AttachCharacter(GeoCharacter character)
        {
            if (character == null) return;

            var info = FindPersonnel(character) ?? FindPersonnel(character.Id) ?? CreatePersonnelRecord(character);
            info.Character = character;
            info.Id = character.Id;
            if (!_assignments.ContainsKey(character.Id))
            {
                _assignments[character.Id] = info;
            }

            TryAutoAssignUnassignedPersonnel(character.Faction as GeoPhoenixFaction, "AttachCharacter");
        }

        private static IEnumerable<PersonnelInfo> GetAutoAssignablePersonnel(GeoPhoenixFaction faction)
        {
            return _assignments.Values
                .Where(person => person?.Character != null && person.Character.Faction == faction)
                .Where(person => person.Assignment == PersonnelAssignment.Unassigned)
                .Where(person => PersonnelRestrictions.CanBeAssignedToManufacturingOrResearch(person.Character))
                .OrderBy(person => person.Id);
        }

        private static bool TryAssignUnassignedWorkerToSlot(PersonnelInfo person, GeoPhoenixFaction faction, FacilitySlotType slotType)
        {
            if (person?.Character == null || faction == null)
            {
                return false;
            }

            if (person.Assignment != PersonnelAssignment.Unassigned)
            {
                return false;
            }

            // Block assignment if living quarters are full.
            if (IsLivingCapacityFull(faction))
            {
                return false;
            }

            // Assign before reserving the slot: reserving refreshes the info bar, which recalculates
            // income from these records and would not yet see this person.
            person.Assignment = slotType == FacilitySlotType.Research
                ? PersonnelAssignment.Research
                : PersonnelAssignment.Manufacturing;

            if (!ResearchManufacturingSlotsManager.IncrementUsedSlot(faction, slotType))
            {
                person.Assignment = PersonnelAssignment.Unassigned;
                return false;
            }

            person.TrainingSpec = null;

            TFTVLogger.Always($"{LogPrefix} Auto-assigned {person.Character.DisplayName} to {person.Assignment}.");
            return true;
        }

        private const string AutoAssignEnabledVariableName = "TFTV_BaseRework_AutoAssignEnabled";
        private const string AutoAssignInitializedVariableName = "TFTV_BaseRework_AutoAssignInitialized";

        internal static bool AutoAssignEnabled = true;

        internal static void EnsureAutoAssignSettingInitialized(GeoLevelController level)
        {
            if (!BaseReworkCheck.BaseReworkEnabled || level?.EventSystem == null)
            {
                return;
            }

            if (level.EventSystem.GetVariable(AutoAssignInitializedVariableName) == 0)
            {
                level.EventSystem.SetVariable(AutoAssignInitializedVariableName, 1);
                level.EventSystem.SetVariable(AutoAssignEnabledVariableName, 1);
                AutoAssignEnabled = true;

                TFTVLogger.Always($"{LogPrefix} Auto-assign setting initialized from default: ON.");
                return;
            }

            AutoAssignEnabled = level.EventSystem.GetVariable(AutoAssignEnabledVariableName) != 0;
        }

        internal static void SetAutoAssignEnabled(GeoLevelController level, bool enabled)
        {
            AutoAssignEnabled = enabled;

            if (level?.EventSystem == null)
            {
                return;
            }

            level.EventSystem.SetVariable(AutoAssignInitializedVariableName, 1);
            level.EventSystem.SetVariable(AutoAssignEnabledVariableName, enabled ? 1 : 0);

            TFTVLogger.Always($"{LogPrefix} Auto-assign setting saved to Geoscape variable: {(enabled ? "ON" : "OFF")}.");
        }

        private static void SyncAutoAssignSettingFromCurrentGeoscape()
        {
            GeoLevelController level = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
            if (level != null)
            {
                EnsureAutoAssignSettingInitialized(level);
            }
        }

        internal static void TryAutoAssignUnassignedPersonnel(GeoPhoenixFaction faction, string source)
        {
            if (!BaseReworkCheck.BaseReworkEnabled || faction == null)
            {
                return;
            }

            SyncAutoAssignSettingFromCurrentGeoscape();

            if (!AutoAssignEnabled)
            {
                return;
            }

            ResearchManufacturingSlotsManager.RecalculateSlots(faction);

            int assignedResearch = 0;
            foreach (PersonnelInfo person in GetAutoAssignablePersonnel(faction).ToList())
            {
                if (!TryAssignUnassignedWorkerToSlot(person, faction, FacilitySlotType.Research))
                {
                    break;
                }

                assignedResearch++;
            }

            int assignedManufacturing = 0;
            foreach (PersonnelInfo person in GetAutoAssignablePersonnel(faction).ToList())
            {
                if (!TryAssignUnassignedWorkerToSlot(person, faction, FacilitySlotType.Manufacturing))
                {
                    break;
                }

                assignedManufacturing++;
            }

            if (assignedResearch > 0 || assignedManufacturing > 0)
            {
                FacilitySlotPools pools = ResearchManufacturingSlotsManager.GetOrCreatePools(faction);
                TFTVLogger.Always(
                    $"{LogPrefix} Auto-assignment from {source}: " +
                    $"Research +{assignedResearch}, Manufacturing +{assignedManufacturing}. " +
                    $"Research {pools.Research.UsedSlots}/{pools.Research.ProvidedSlots}, " +
                    $"Manufacturing {pools.Manufacturing.UsedSlots}/{pools.Manufacturing.ProvidedSlots}");
            }
        }
        internal static void RemovePersonnel(GeoPhoenixFaction faction, PersonnelInfo person)
        {
            if (person == null)
            {
                return;
            }

            int requestedId = person.Id;
            int characterId = person.Character?.Id ?? 0;
            string name = person.Character?.DisplayName ?? $"Personnel {requestedId}";

            TFTVLogger.Always($"{LogPrefix} RemovePersonnel requested Name={name} RequestedId={requestedId} CharacterId={characterId} Assignment={person.Assignment}");

            // Clear the assignment before releasing the slot, so the income recalculation the release
            // triggers no longer counts this person's output.
            PersonnelAssignment vacatedAssignment = person.Assignment;
            person.Assignment = PersonnelAssignment.Unassigned;

            if (vacatedAssignment == PersonnelAssignment.Research || vacatedAssignment == PersonnelAssignment.Manufacturing)
            {
                ReleaseWorkSlotIfNeeded(faction, vacatedAssignment);
            }

            bool removed = _assignments.Remove(requestedId);

            if (!removed && characterId > 0)
            {
                removed = _assignments.Remove(characterId);
            }

            if (!removed)
            {
                int fallbackKey = _assignments
                    .Where(kv => kv.Value != null)
                    .Where(kv => ReferenceEquals(kv.Value, person)
                        || (characterId > 0 && kv.Value.Character != null && kv.Value.Character.Id == characterId))
                    .Select(kv => kv.Key)
                    .FirstOrDefault();

                if (fallbackKey != 0)
                {
                    removed = _assignments.Remove(fallbackKey);
                    TFTVLogger.Always($"{LogPrefix} RemovePersonnel fallback removal used Key={fallbackKey} Name={name}");
                }
            }

            if (removed)
            {
                TryAutoAssignUnassignedPersonnel(faction, "RemovePersonnel");
            }

            TFTVLogger.Always($"{LogPrefix} RemovePersonnel result Name={name} Removed={removed} RemainingAssignments={_assignments.Count}");
        }

        internal static List<SpecializationDef> ResolveAvailableMainSpecs(GeoLevelController level)
        {
            var faction = level?.PhoenixFaction;
            if (faction == null) return new List<SpecializationDef>();
            return TrainingFacilityRework.GetAvailableTrainingSpecializations(faction).ToList();
        }

        internal static bool AssignWorker(PersonnelInfo person, GeoPhoenixFaction faction, FacilitySlotType slotType)
        {
            if (!BaseReworkCheck.BaseReworkEnabled)
            {
                return false;
            }

            if (person?.Character == null || faction == null)
            {
                return false;
            }

            if (!PersonnelRestrictions.CanBeAssignedToManufacturingOrResearch(person.Character))
            {
                TFTVLogger.Always($"{LogPrefix} {person.Character.DisplayName} cannot be assigned to {slotType} because of Just a grunt.");
                return false;
            }

            ResearchManufacturingSlotsManager.RecalculateSlots(faction);

            PersonnelAssignment desired = slotType == FacilitySlotType.Research
                ? PersonnelAssignment.Research
                : PersonnelAssignment.Manufacturing;

            if (person.Assignment == desired)
            {
                return true;
            }

            PersonnelAssignment previous = person.Assignment;

            // If coming from Unassigned, check living cap (R→M or M→R swaps don't change living usage).
            if (previous == PersonnelAssignment.Unassigned && IsLivingCapacityFull(faction))
            {
                TFTVLogger.Always($"{LogPrefix} Cannot assign {person.Character.DisplayName} to {slotType}: living quarters full.");
                return false;
            }

            // Move the person first. Both slot counters refresh the info bar, which recalculates income
            // from these records, so they have to see the new assignment or the figure comes out stale.
            person.Assignment = desired;

            if (!ResearchManufacturingSlotsManager.IncrementUsedSlot(faction, slotType))
            {
                person.Assignment = previous;
                TFTVLogger.Always($"{LogPrefix} No free {slotType} slots available (used >= provided).");
                return false;
            }

            ReleaseWorkSlotIfNeeded(faction, previous);

            GeoLevelController level = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
            UIModuleInfoBar infoBar = level.View.GeoscapeModules.ResourcesModule;
            var update = AccessTools.Method(typeof(UIModuleInfoBar), "UpdateResourceInfo");
            update.Invoke(infoBar, new object[] { faction, false });
            return true;
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

        internal static List<PersonnelAssignmentSave> CreateAssignmentsSnapshot()
        {
            if (!BaseReworkCheck.BaseReworkEnabled)
            {
                return new List<PersonnelAssignmentSave>();
            }

            var list = new List<PersonnelAssignmentSave>();
            foreach (var pi in _assignments.Values)
            {
                string mainSpecName = null;

                if (pi != null &&
                    pi.Assignment == PersonnelAssignment.Training &&
                    pi.TrainingSpec != null)
                {
                    mainSpecName = pi.TrainingSpec.name;
                }

                list.Add(new PersonnelAssignmentSave
                {
                    GeoUnitId = pi.Id,
                    MainSpecName = mainSpecName,
                    Assignment = pi.Assignment,
                });
            }

            return list;
        }

        internal static void LoadAssignmentsSnapshot(GeoLevelController level, IEnumerable<PersonnelAssignmentSave> snapshot)
        {
            if (!BaseReworkCheck.BaseReworkEnabled)
            {
                return;
            }

            try
            {
                if (level?.PhoenixFaction == null || snapshot == null)
                {
                    return;
                }

                var phoenix = level.PhoenixFaction;

                TFTVLogger.Always($"[PersonnelData] Loading assignments snapshot.");

                foreach (PersonnelAssignmentSave save in snapshot)
                {
                    PersonnelInfo info = new PersonnelInfo
                    {
                        Id = save.GeoUnitId,
                        Character = phoenix.Characters.FirstOrDefault(s => s.Id == save.GeoUnitId),
                        Assignment = save.Assignment,
                        TrainingSpec = null
                    };

                    if (info.Character == null)
                    {
                        TFTVLogger.Always(
                            $"[PersonnelData] Skipping personnel id={info.Id}: character not found in PhoenixFaction.Characters.");
                        continue;
                    }

                    if (info.Character != null)
                    {
                        GeoCharacterFilter.HiddenOperativeMarkerFilter.ApplyHiddenMarker(info.Character);
                    }

                    if (save.Assignment == PersonnelAssignment.Training &&
                        !string.IsNullOrEmpty(save.MainSpecName))
                    {
                        info.TrainingSpec = TFTVMain.Main.DefCache.GetDef<SpecializationDef>(save.MainSpecName);
                    }

                    _assignments.Add(info.Id, info);

                    TFTVLogger.Always(
                        $"[PersonnelData] Restoring personnel id={info.Id} " +
                        $"name={info.Character?.DisplayName ?? "null"} " +
                        $"assignment={info.Assignment} " +
                        $"MainSpecName={info.TrainingSpec?.name ?? "null"}");
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void RestoreAssignments(GeoLevelController level)
        {
            if (!BaseReworkCheck.BaseReworkEnabled)
            {
                return;
            }

            try
            {
                EnsureAutoAssignSettingInitialized(level);
                ResyncWorkSlots(level.PhoenixFaction);
                EnforceLivingCapacity(level.PhoenixFaction);
                TryAutoAssignUnassignedPersonnel(level.PhoenixFaction, "RestoreAssignments");
                RefreshInfoBar(level);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void ResyncWorkSlots(GeoPhoenixFaction phoenix)
        {
            if (phoenix == null) return;

            ResearchManufacturingSlotsManager.RecalculateSlots(phoenix);
            ResearchManufacturingSlotsManager.SetUsedSlots(phoenix, FacilitySlotType.Research,
                _assignments.Values.Count(pi => pi.Assignment == PersonnelAssignment.Research));
            ResearchManufacturingSlotsManager.SetUsedSlots(phoenix, FacilitySlotType.Manufacturing,
                _assignments.Values.Count(pi => pi.Assignment == PersonnelAssignment.Manufacturing));

            TFTVLogger.Always($"[PersonnelData] After load: ResearchUsed={_assignments.Values.Count(pi => pi.Assignment == PersonnelAssignment.Research)} ManufacturingUsed={_assignments.Values.Count(pi => pi.Assignment == PersonnelAssignment.Manufacturing)} Total={_assignments.Values.Count}");
        }

        private static void RefreshInfoBar(GeoLevelController level)
        {
            FlushPendingInfoBarUpdate(level);
        }
        internal static void DailyUpdatePersonnelPool(GeoLevelController level)
        {
            if (!BaseReworkCheck.BaseReworkEnabled)
            {
                return;

            }
            try
            {
                PersonnelManagementUI.DailyTick(level);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        [HarmonyPatch(typeof(GeoscapeLog), "PhoenixFaction_OnRecruitsRegenerated")]

        internal static class Patch_GeoscapeLog_PhoenixFaction_OnRecruitsRegenerated
        {
            static bool Prefix(IEnumerable<GeoUnitDescriptor> nakedRecruits, GeoLevelController ____level)
            {
                if (!BaseReworkCheck.BaseReworkEnabled)
                {
                    return true;
                }

                int target = GetTargetCount(____level.CurrentDifficultyLevel);

                if (target == 0)
                {
                    return false;
                }

                return true;

            }
        }

        private static int GetTargetCount(GameDifficultyLevelDef diff)
        {
            if (TFTVNewGameOptions.ConfigImplemented)
            {
                return TFTVNewGameOptions.PersonnelInfluxLevel;
            }

            int order = diff.Order;
            if (order <= 2) return 3;   // Story/Rookie
            if (order <= 3) return 2;   // Veteran
            if (order <= 4) return 1;   // Hero
            return 0;                   // Legend/Eldritch
        }

        [HarmonyPatch(typeof(GeoPhoenixFaction), nameof(GeoPhoenixFaction.RegenerateNakedRecruits))]
        internal static class GeoPhoenixFaction_RegenerateNakedRecruits_PersonnelSync
        {

            private static void Postfix(GeoPhoenixFaction __instance, ref TimeUnit ____lastNakedRecruitRefresh, ref Dictionary<GeoUnitDescriptor, ResourcePack> ____nakedRecruits)
            {
                try
                {

                    if (!BaseReworkCheck.BaseReworkEnabled)
                    {
                        return;
                    }

                  

                    GeoLevelController controller = __instance.GeoLevel;

                    int target = GetTargetCount(controller.CurrentDifficultyLevel);

                    TFTVLogger.Always($"GeoPhoenixFaction.RegenerateNakedRecruits running. target {target}");

                    // Add recruits if below target
                    if (____nakedRecruits.Count < target)
                    {
                        var context = controller.CharacterGenerator.GenerateCharacterGeneratorContext(__instance);

                        int safety = 0;
                        while (____nakedRecruits.Count < target && safety++ < 50)
                        {
                            var unit = controller.CharacterGenerator.GenerateRandomUnit(context);
                            controller.CharacterGenerator.ApplyRecruitDifficultyParameters(unit);
                            var cost = __instance.GenerateNakedRecruitsCost();

                            // Avoid key collisions if any
                            if (!____nakedRecruits.ContainsKey(unit))
                            {
                                ____nakedRecruits.Add(unit, cost);
                            }
                        }
                    }
                    // Remove recruits if above target
                    else if (____nakedRecruits.Count > target)
                    {
                        int toRemove = ____nakedRecruits.Count - target;
                        foreach (var key in ____nakedRecruits.Keys.Take(toRemove).ToList())
                        {
                            ____nakedRecruits.Remove(key);
                        }
                    }

                    if (target <= 0)
                    {
                        __instance.SpawnedRecruitNotification = false;
                    }

                    SyncFromNakedRecruits(__instance);
                }
                catch (Exception e) { TFTVLogger.Error(e); }
            }
        }

        internal static void SyncFromNakedRecruits(GeoPhoenixFaction phoenix)
        {
            try
            {

                GeoLevelController level = phoenix.GeoLevel;

                foreach (var kv in phoenix.NakedRecruits.ToList())
                {
                    GeoUnitDescriptor descriptor = kv.Key;
                    if (descriptor == null) continue;

                    GeoCharacter character = CreateHiddenCharacterFromDescriptor(level, phoenix, descriptor);

                    if (!_assignments.ContainsKey(character.Id))
                    {
                        AttachCharacter(character);

                    }
                }
                CleanNakedRecruits(phoenix);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        private static void CleanNakedRecruits(GeoPhoenixFaction phoenix)
        {
            try
            {
                FieldInfo fieldInfo = AccessTools.Field(typeof(GeoPhoenixFaction), "_nakedRecruits");

                var _nakedRecruits = (Dictionary<GeoUnitDescriptor, ResourcePack>)fieldInfo.GetValue(phoenix);
                _nakedRecruits.Clear();
                fieldInfo.SetValue(phoenix, _nakedRecruits);

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        // Affinity the incident leader had when the incident completed. While set, the personnel
        // gained from that incident inherit it: leader rank 2+ gives the first recruit the leader's
        // affinity at rank 1; leader rank 3 additionally gives the second recruit a random
        // different affinity at rank 1.
        private static bool _pendingRecruitAffinityActive;
        private static LeaderSelection.AffinityApproach _pendingRecruitAffinityApproach;
        private static int _pendingRecruitAffinityLeaderRank;

        internal static void SetPendingIncidentRecruitAffinity(GeoCharacter leader)
        {
            ClearPendingIncidentRecruitAffinity();

            if (leader == null
                || !LeaderSelection.TryGetCurrentAffinity(leader, out LeaderSelection.AffinityApproach approach, out int rank)
                || rank < 2)
            {
                return;
            }

            _pendingRecruitAffinityActive = true;
            _pendingRecruitAffinityApproach = approach;
            _pendingRecruitAffinityLeaderRank = rank;

            TFTVLogger.Always($"[PersonnelData] Pending incident recruit affinity set from leader {leader.DisplayName}: {approach} (leader rank {rank}).");
        }

        internal static void ClearPendingIncidentRecruitAffinity()
        {
            _pendingRecruitAffinityActive = false;
            _pendingRecruitAffinityLeaderRank = 0;
        }

        private static void ApplyPendingRecruitAffinity(GeoCharacter recruit, int recruitIndex)
        {
            if (!_pendingRecruitAffinityActive || recruit == null || recruitIndex > 1)
            {
                return;
            }

            LeaderSelection.AffinityApproach approach;

            if (recruitIndex == 0)
            {
                approach = _pendingRecruitAffinityApproach;
            }
            else
            {
                if (_pendingRecruitAffinityLeaderRank < 3)
                {
                    return;
                }

                List<LeaderSelection.AffinityApproach> otherApproaches = Enum
                    .GetValues(typeof(LeaderSelection.AffinityApproach))
                    .Cast<LeaderSelection.AffinityApproach>()
                    .Where(candidate => candidate != _pendingRecruitAffinityApproach)
                    .ToList();

                approach = otherApproaches.GetRandomElement();
            }

            if (!LeaderSelection.TrySetAffinityRank(recruit, approach, 1))
            {
                return;
            }

            AffinityInheritance.RecordOrUpdateOperativeAffinity(recruit.Id, approach, 1);
            TFTVLogger.Always($"[PersonnelData] Incident recruit {recruit.DisplayName} gained {approach} rank 1 (leader rank {_pendingRecruitAffinityLeaderRank}).");
        }

        internal static int AddIncidentPersonnelReward(GeoPhoenixFaction faction, int count)
        {
            if (!BaseReworkCheck.BaseReworkEnabled || faction == null || count <= 0)
            {
                return 0;
            }

            TFTVLogger.Always($"[PersonnelData] Adding {count} incident personnel reward(s) for faction {faction.Name}.");

            GeoLevelController level = faction.GeoLevel;
            TFTVLogger.Always($"[PersonnelData] level == null? {level == null}.");
            if (level == null)
            {
                return 0;
            }



            int added = 0;
            for (int i = 0; i < count; i++)
            {
                GeoUnitDescriptor descriptor = GenerateIncidentPersonnelDescriptor(level, faction);

                TFTVLogger.Always($"[PersonnelData] descriptor == null? {descriptor == null}.");
                if (descriptor == null)
                {
                    break;
                }

                GeoCharacter character = CreateHiddenCharacterFromDescriptor(level, faction, descriptor);
                TFTVLogger.Always($"[PersonnelData] character == null? {character == null}.");
                if (character == null)
                {
                    continue;
                }

                ApplyPendingRecruitAffinity(character, added);
                AttachCharacter(character);
                added++;
            }

            if (added > 0)
            {
                TFTVLogger.Always($"[PersonnelData] Added {added} incident personnel reward(s).");
            }

            return added;
        }

        private static GeoUnitDescriptor GenerateIncidentPersonnelDescriptor(GeoLevelController level, GeoPhoenixFaction faction)
        {
            TacCharacterDef template = GetIncidentPersonnelTemplate(faction);
            if (level?.CharacterGenerator == null || template == null)
            {
                return null;
            }

            MethodInfo generateUnit = AccessTools.Method(level.CharacterGenerator.GetType(), "GenerateUnit",
                new[] { typeof(GeoFaction), typeof(TacCharacterDef) });
            if (generateUnit == null)
            {
                TFTVLogger.Always("[PersonnelData] GenerateUnit(GeoFaction, TacCharacterDef) not found.");
                return null;
            }

            return generateUnit.Invoke(level.CharacterGenerator, new object[] { faction, template }) as GeoUnitDescriptor;
        }

        private static TacCharacterDef GetIncidentPersonnelTemplate(GeoPhoenixFaction faction)
        {
            List<TacCharacterDef> templates = faction?.UnlockedUnitTemplates?
                .Where(t => t != null && !t.IsVehicle && !t.IsMutog)
                .ToList();

            if (templates == null || templates.Count == 0)
            {
                return DefCache.GetDef<TacCharacterDef>("PX_Assault1_CharacterTemplateDef");
            }

            // TFTVLogger.Always($"[PersonnelData] Found {templates?.Count ?? 0} unlocked unit templates for faction {faction?.Name}.");

            return templates.GetRandomElement();
        }

        internal static int GetAvailablePersonnelCount(GeoPhoenixFaction faction)
        {
            if (!BaseReworkCheck.BaseReworkEnabled || faction == null)
            {
                return 0;
            }

            return _assignments.Values
                .Where(person => person?.Character != null && person.Character.Faction == faction)
                .Count(person => person.Assignment == PersonnelAssignment.Unassigned
                    || person.Assignment == PersonnelAssignment.Research
                    || person.Assignment == PersonnelAssignment.Manufacturing);
        }

        /// <summary>
        /// Total base-activation weight of available personnel
        /// (Psycho-Sociology personnel count as 3, everyone else as 1).
        /// </summary>
        internal static int GetAvailableActivationWeight(GeoPhoenixFaction faction)
        {
            if (!BaseReworkCheck.BaseReworkEnabled || faction == null)
            {
                return 0;
            }

            return _assignments.Values
                .Where(person => person?.Character != null && person.Character.Faction == faction)
                .Where(person => PersonnelRestrictions.CanBeUsedForBaseActivation(person.Character))
                .Where(person => person.Assignment == PersonnelAssignment.Unassigned
                    || person.Assignment == PersonnelAssignment.Research
                    || person.Assignment == PersonnelAssignment.Manufacturing)
                .Sum(person => GetActivationWeight(person.Character));
        }

        internal static bool AssignPersonnelToTraining(PersonnelInfo person, GeoPhoenixFaction faction, SpecializationDef spec)
        {
            if (!BaseReworkCheck.BaseReworkEnabled)
            {
                return false;
            }

            if (person?.Character == null || faction == null || spec == null)
            {
                return false;
            }

            // If coming from Unassigned, assigning to Training increases living usage — block if full.
            if (person.Assignment == PersonnelAssignment.Unassigned && IsLivingCapacityFull(faction))
            {
                TFTVLogger.Always($"{LogPrefix} Cannot assign {person.Character.DisplayName} to Training: living quarters full.");
                return false;
            }

            PersonnelAssignment previous = person.Assignment;

            // Move to Training before releasing the work slot, so the income recalculation the release
            // triggers already sees this person off research/manufacturing duty.
            person.Assignment = PersonnelAssignment.Training;
            person.TrainingSpec = spec;

            if (previous == PersonnelAssignment.Research || previous == PersonnelAssignment.Manufacturing)
            {
                ReleaseWorkSlotIfNeeded(faction, previous);
            }

            TryAutoAssignUnassignedPersonnel(faction, "AssignPersonnelToTraining");
            return true;
        }

        internal static void UnassignFromWork(PersonnelInfo person, GeoPhoenixFaction faction)
        {
            if (!BaseReworkCheck.BaseReworkEnabled) return;
            if (person?.Character == null || faction == null) return;

            PersonnelAssignment previous = person.Assignment;
            if (previous == PersonnelAssignment.Unassigned) return;

            // Clear the assignment before releasing the slot: releasing refreshes the info bar, which
            // recalculates income from these records and would otherwise still count this person.
            person.Assignment = PersonnelAssignment.Unassigned;

            if (previous == PersonnelAssignment.Research || previous == PersonnelAssignment.Manufacturing)
            {
                ReleaseWorkSlotIfNeeded(faction, previous);
            }

            TFTVLogger.Always($"{LogPrefix} Unassigned {person.Character.DisplayName} from {previous} to Unassigned.");

            try
            {
                GeoLevelController level = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
                UIModuleInfoBar infoBar = level?.View?.GeoscapeModules?.ResourcesModule;
                var update = AccessTools.Method(typeof(UIModuleInfoBar), "UpdateResourceInfo");
                if (infoBar != null && update != null)
                {
                    update.Invoke(infoBar, new object[] { faction, false });
                }
            }
            catch (Exception e) { TFTVLogger.Error(e); }
        }


        // ── Living-capacity helpers ──────────────────────────────────────────────

        internal static bool IsLivingCapacityFull(GeoPhoenixFaction faction)
        {
            return faction != null
                && FoodAndLivingSpacePolicy.GetTotalLivingSpaceUsed(faction) >= faction.SoldierCapacity;
        }

        /// <summary>
        /// If living space used exceeds capacity, removes workers from Manufacturing
        /// then Research (never Training) until usage is back within capacity.
        /// Triggers a single infobar + production refresh at the end.
        /// </summary>
        internal static void EnforceLivingCapacity(GeoPhoenixFaction faction)
        {
            if (!BaseReworkCheck.BaseReworkEnabled || faction == null)
            {
                return;
            }

            int over = FoodAndLivingSpacePolicy.GetTotalLivingSpaceUsed(faction) - faction.SoldierCapacity;
            if (over <= 0)
            {
                return;
            }

            TFTVLogger.Always($"{LogPrefix} Living capacity exceeded by {over}. Evicting workers from R/M slots.");

            // Evict Manufacturing first, then Research. Never touch Training.
            foreach (PersonnelAssignment targetAssignment in new[] { PersonnelAssignment.Manufacturing, PersonnelAssignment.Research })
            {
                if (over <= 0)
                {
                    break;
                }

                List<PersonnelInfo> victims = _assignments.Values
                    .Where(p => p?.Character != null && p.Character.Faction == faction && p.Assignment == targetAssignment)
                    .ToList();

                foreach (PersonnelInfo person in victims)
                {
                    if (over <= 0)
                    {
                        break;
                    }

                    PersonnelAssignment vacated = person.Assignment;
                    person.Assignment = PersonnelAssignment.Unassigned;
                    ReleaseWorkSlotIfNeeded(faction, vacated);
                    over--;

                    TFTVLogger.Always($"{LogPrefix} Evicted {person.Character.DisplayName} from {targetAssignment} due to living capacity.");
                }
            }

            // Single refresh after all evictions.
            ResearchAndManufacturing.ApplyProductionAdjustments(faction);
            Workers.RefreshInfoBar(faction);
        }
    }
}