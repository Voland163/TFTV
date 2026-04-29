using Base.Core;
using Base.Serialization.General;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Missions;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using System;
using System.Collections.Generic;
using System.Linq;
using TFTV.TFTVBaseRework;
using UnityEngine;

namespace TFTV.TFTVIncidents
{

    [SerializeType(SerializeMembersByDefault = SerializeMembersType.SerializeAll)]
    public sealed class OperativeAffinitySave
    {
        public int GeoUnitId;
        public string Approach;
        public int Rank;
    }

    [SerializeType(SerializeMembersByDefault = SerializeMembersType.SerializeAll)]
    public sealed class BankedAffinitySave
    {
        public string Approach;
        public int Rank;
    }

    internal static class AffinityInheritance
    {
        private const string DiagTag = "[AffinityInheritance]";
        private const string NurseAbilityDefName = "Helpful_AbilityDef";
        private const string TransferToRecipientKey = "KEY_AFFINITY_INHERITANCE_TRANSFER_TO_RECIPIENT";
        private const string TransferBankedKey = "KEY_AFFINITY_INHERITANCE_TRANSFER_BANKED";
        private const int SameAircraftWeightBonus = 100;

        // Placeholder tuning hooks.
        private const int BiotechNurseWeightBonus = 20;
        private const int OccultMutationWeightBonus = 20;
        private const int MachineryBionicsWeightBonus = 20;

        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly Dictionary<int, string> TransferSummaryByDeadUnitId = new Dictionary<int, string>();
        private static readonly List<BankedAffinityEntry> BankedAffinities = new List<BankedAffinityEntry>();
        private static readonly Dictionary<int, OperativeAffinityEntry> OperativeAffinities = new Dictionary<int, OperativeAffinityEntry>();


        private static PassiveModifierAbilityDef _nurseAbility;

        private sealed class OperativeAffinityEntry
        {
            public LeaderSelection.AffinityApproach Approach;
            public int Rank;
        }

        private sealed class BankedAffinityEntry
        {
            public LeaderSelection.AffinityApproach Approach;
            public int Rank;
        }

        private sealed class WeightedCandidate
        {
            public GeoCharacter Character;
            public int Weight;
            public int Missions;
            public bool SameAircraft;
        }

        internal static void ProcessMissionResults(GeoMission mission, TacMissionResult result, GeoSquad squad)
        {
            try
            {
                if (!BaseReworkCheck.BaseReworkEnabled || mission?.Site?.GeoLevel == null || result == null)
                {
                    return;
                }

                GeoLevelController geoLevel = mission.Site.GeoLevel;
                GeoPhoenixFaction phoenixFaction = geoLevel.PhoenixFaction;
                if (phoenixFaction?.Characters == null)
                {
                    return;
                }

                var factionResult = result.GetResultByFacionDef(geoLevel.ViewerFaction?.Def?.PPFactionDef);
                if (factionResult == null)
                {
                    return;
                }

                List<GeoTacUnitId> deadOperatives = factionResult
                    .GetUnitResultsData<TacActorUnitResult>()
                    .Where(unitResult => unitResult != null && !unitResult.IsAlive && unitResult.GeoUnitId != GeoTacUnitId.None)
                    .Select(unitResult => unitResult.GeoUnitId)
                    .Distinct()
                    .ToList();

                if (deadOperatives.Count == 0)
                {
                    return;
                }

                GeoVehicle missionVehicle = ResolveMissionVehicle(mission, squad);
                HashSet<int> deadOperativeIds = new HashSet<int>(deadOperatives.Select(ToInt));
                HashSet<int> reservedRecipientIds = new HashSet<int>();

                foreach (GeoTacUnitId deadOperativeId in deadOperatives)
                {
                    ProcessDeadOperative(geoLevel, missionVehicle, deadOperativeIds, reservedRecipientIds, deadOperativeId);
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void TryAssignBankedAffinity(GeoPhoenixFaction faction, GeoCharacter recruit)
        {
            try
            {
                if (!BaseReworkCheck.BaseReworkEnabled || faction == null || !IsEligibleRecruit(recruit))
                {
                    return;
                }

                if (LeaderSelection.TryGetCurrentAffinity(recruit, out _, out _))
                {
                    return;
                }

                if (BankedAffinities.Count == 0)
                {
                    return;
                }

                BankedAffinityEntry banked = BankedAffinities[0];
                if (!LeaderSelection.TrySetAffinityRank(recruit, banked.Approach, banked.Rank))
                {
                    return;
                }

                BankedAffinities.RemoveAt(0);
                RecordOrUpdateOperativeAffinity(recruit.Id, banked.Approach, banked.Rank);

                TFTVLogger.Always(
                    $"{DiagTag} Assigned banked {FormatAffinity(banked.Approach, banked.Rank)} to {recruit.DisplayName}. Remaining banked={BankedAffinities.Count}");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static string GetTransferSummary(int deadOperativeId)
        {
            return TransferSummaryByDeadUnitId.TryGetValue(deadOperativeId, out string summary) ? summary : string.Empty;
        }

        internal static void RecordOrUpdateOperativeAffinity(
    int geoUnitId,
    LeaderSelection.AffinityApproach approach,
    int rank)
        {
            try
            {
                if (geoUnitId <= 0 || rank <= 0)
                {
                    return;
                }

                OperativeAffinities[geoUnitId] = new OperativeAffinityEntry
                {
                    Approach = approach,
                    Rank = Mathf.Clamp(rank, 1, 3)
                };
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void RemoveOperativeAffinity(int geoUnitId)
        {
            try
            {
                if (geoUnitId <= 0)
                {
                    return;
                }

                OperativeAffinities.Remove(geoUnitId);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static bool TryGetRecordedAffinity(
            int geoUnitId,
            out LeaderSelection.AffinityApproach approach,
            out int rank)
        {
            try
            {
                approach = default(LeaderSelection.AffinityApproach);
                rank = 0;

                if (geoUnitId <= 0)
                {
                    return false;
                }

                if (!OperativeAffinities.TryGetValue(geoUnitId, out OperativeAffinityEntry entry) || entry == null)
                {
                    return false;
                }

                approach = entry.Approach;
                rank = entry.Rank;
                return rank > 0;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                approach = default(LeaderSelection.AffinityApproach);
                rank = 0;
                return false;
            }
        }

        internal static void ReconcileOperativeAffinities(GeoLevelController level)
        {
            try
            {
                OperativeAffinities.Clear();

                IEnumerable<GeoCharacter> characters = level?.PhoenixFaction?.Characters ?? Enumerable.Empty<GeoCharacter>();
                foreach (GeoCharacter character in characters)
                {
                    if (character == null || !PersonnelRestrictions.CanGainAffinities(character))
                    {
                        continue;
                    }

                    if (LeaderSelection.TryGetCurrentAffinity(character, out LeaderSelection.AffinityApproach approach, out int rank))
                    {
                        RecordOrUpdateOperativeAffinity(character.Id, approach, rank);
                    }
                }

                TFTVLogger.Always($"{DiagTag} Reconciled operative affinities: {OperativeAffinities.Count}");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static List<OperativeAffinitySave> CreateOperativeAffinitySnapshot()
        {
            try
            {
                return OperativeAffinities
                    .Select(kvp => new OperativeAffinitySave
                    {
                        GeoUnitId = kvp.Key,
                        Approach = kvp.Value.Approach.ToString(),
                        Rank = kvp.Value.Rank
                    })
                    .ToList();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return new List<OperativeAffinitySave>();
            }
        }

        internal static void LoadOperativeAffinitySnapshot(IEnumerable<OperativeAffinitySave> saves)
        {
            try
            {
                OperativeAffinities.Clear();

                if (saves == null)
                {
                    return;
                }

                foreach (OperativeAffinitySave save in saves)
                {
                    if (save == null || save.GeoUnitId <= 0 || string.IsNullOrEmpty(save.Approach))
                    {
                        continue;
                    }

                    if (!Enum.TryParse(save.Approach, true, out LeaderSelection.AffinityApproach approach))
                    {
                        continue;
                    }

                    RecordOrUpdateOperativeAffinity(save.GeoUnitId, approach, save.Rank);
                }

                TFTVLogger.Always($"{DiagTag} Loaded operative affinities: {OperativeAffinities.Count}");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static List<BankedAffinitySave> CreateBankedAffinitySnapshot()
        {
            try
            {
                return BankedAffinities
                    .Select(entry => new BankedAffinitySave
                    {
                        Approach = entry.Approach.ToString(),
                        Rank = entry.Rank
                    })
                    .ToList();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return new List<BankedAffinitySave>();
            }
        }

        internal static void LoadBankedAffinitySnapshot(IEnumerable<BankedAffinitySave> saves)
        {
            try
            {
                BankedAffinities.Clear();

                if (saves == null)
                {
                    return;
                }

                foreach (BankedAffinitySave save in saves)
                {
                    if (save == null || string.IsNullOrEmpty(save.Approach))
                    {
                        continue;
                    }

                    if (!Enum.TryParse(save.Approach, true, out LeaderSelection.AffinityApproach approach))
                    {
                        continue;
                    }

                    int rank = Mathf.Clamp(save.Rank, 1, 3);

                    BankedAffinities.Add(new BankedAffinityEntry
                    {
                        Approach = approach,
                        Rank = rank
                    });
                }

                TFTVLogger.Always($"{DiagTag} Loaded banked affinities: {BankedAffinities.Count}");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void ProcessDeadOperative(
            GeoLevelController geoLevel,
            GeoVehicle missionVehicle,
            HashSet<int> deadOperativeIds,
            HashSet<int> reservedRecipientIds,
            GeoTacUnitId deadOperativeId)
        {
            int deadOperativeKey = ToInt(deadOperativeId);
            TransferSummaryByDeadUnitId.Remove(deadOperativeKey);

            if (!geoLevel.DeadSoldiers.TryGetValue(deadOperativeId, out GeoUnitDescriptor descriptor) || descriptor == null || descriptor.UnitType.IsVehicle)
            {
                return;
            }

            if (!TryGetRecordedAffinity(deadOperativeKey, out LeaderSelection.AffinityApproach approach, out int rank))
            {
                return;
            }

            GeoCharacter recipient = SelectRecipient(geoLevel, missionVehicle, deadOperativeIds, reservedRecipientIds, approach);
            if (recipient != null && LeaderSelection.TrySetAffinityRank(recipient, approach, rank))
            {
                reservedRecipientIds.Add(recipient.Id);
                RecordOrUpdateOperativeAffinity(recipient.Id, approach, rank);
                RemoveOperativeAffinity(deadOperativeKey);

                string summary = BuildTransferToRecipientSummary(approach, rank, recipient.DisplayName);
                TransferSummaryByDeadUnitId[deadOperativeKey] = summary;

                TFTVLogger.Always(
                    $"{DiagTag} {descriptor.GetName()} passed {FormatAffinity(approach, rank)} to {recipient.DisplayName}.");
                return;
            }

            RemoveOperativeAffinity(deadOperativeKey);

            BankedAffinities.Add(new BankedAffinityEntry
            {
                Approach = approach,
                Rank = rank
            });

            TransferSummaryByDeadUnitId[deadOperativeKey] = BuildTransferBankedSummary(approach, rank);

            TFTVLogger.Always(
                $"{DiagTag} {descriptor.GetName()} had no eligible recipient for {FormatAffinity(approach, rank)}. Banked={BankedAffinities.Count}");
        }

        private static GeoCharacter SelectRecipient(
            GeoLevelController geoLevel,
            GeoVehicle missionVehicle,
            HashSet<int> deadOperativeIds,
            HashSet<int> reservedRecipientIds,
            LeaderSelection.AffinityApproach approach)
        {
            List<WeightedCandidate> candidates = new List<WeightedCandidate>();
            int missionVehicleId = missionVehicle?.VehicleID ?? -1;

            foreach (GeoCharacter character in geoLevel.PhoenixFaction.Characters.Where(c => c != null))
            {
                if (!IsEligibleCandidate(geoLevel, character, missionVehicleId, deadOperativeIds, reservedRecipientIds))
                {
                    continue;
                }

                int candidateVehicleId = ResolveCharacterVehicleId(geoLevel, character);
                bool sameAircraft = missionVehicleId > 0 && candidateVehicleId == missionVehicleId;
                int missions = LeaderSelection.GetMissionCount(character);
                int weight = BuildCandidateWeight(character, approach, missions, sameAircraft);

                candidates.Add(new WeightedCandidate
                {
                    Character = character,
                    Missions = missions,
                    SameAircraft = sameAircraft,
                    Weight = weight
                });
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            int totalWeight = candidates.Sum(candidate => Math.Max(1, candidate.Weight));
            if (totalWeight <= 0)
            {
                return candidates
                    .OrderByDescending(candidate => candidate.SameAircraft)
                    .ThenByDescending(candidate => candidate.Missions)
                    .Select(candidate => candidate.Character)
                    .FirstOrDefault();
            }

            int roll = UnityEngine.Random.Range(0, totalWeight);
            foreach (WeightedCandidate candidate in candidates)
            {
                roll -= Math.Max(1, candidate.Weight);
                if (roll < 0)
                {
                    return candidate.Character;
                }
            }

            return candidates[candidates.Count - 1].Character;
        }

        private static bool IsEligibleCandidate(
            GeoLevelController geoLevel,
            GeoCharacter character,
            int missionVehicleId,
            HashSet<int> deadOperativeIds,
            HashSet<int> reservedRecipientIds)
        {
            if (character == null || character.Progression == null || character.TemplateDef == null || !character.TemplateDef.IsHuman)
            {
                return false;
            }

            if (!PersonnelRestrictions.CanGainAffinities(character))
            {
                return false;
            }

            if (deadOperativeIds.Contains(character.Id) || reservedRecipientIds.Contains(character.Id))
            {
                return false;
            }

            if (LeaderSelection.TryGetCurrentAffinity(character, out _, out _))
            {
                return false;
            }

            int candidateVehicleId = ResolveCharacterVehicleId(geoLevel, character);
            if (candidateVehicleId > 0 && Resolution.IncidentController.IsVehicleResolvingIncident(candidateVehicleId, missionVehicleId))
            {
                return false;
            }

            return true;
        }

        private static bool IsEligibleRecruit(GeoCharacter recruit)
        {
            return recruit != null
                && recruit.Progression != null
                && recruit.TemplateDef != null
                && recruit.TemplateDef.IsHuman
                && PersonnelRestrictions.CanGainAffinities(recruit);
        }

        private static int BuildCandidateWeight(
            GeoCharacter character,
            LeaderSelection.AffinityApproach approach,
            int missions,
            bool sameAircraft)
        {
            int weight = 1 + Math.Max(0, missions);

            if (sameAircraft)
            {
                weight += SameAircraftWeightBonus;
            }

            weight += GetBackgroundWeightBonus(character, approach);
            weight += GetBodyStateWeightBonus(character, approach);

            return Math.Max(1, weight);
        }

        private static int GetBackgroundWeightBonus(GeoCharacter character, LeaderSelection.AffinityApproach approach)
        {
            switch (approach)
            {
                case LeaderSelection.AffinityApproach.Biotech:
                    return HasAbility(character, GetNurseAbility()) ? BiotechNurseWeightBonus : 0;

                default:
                    return 0;
            }
        }

        private static int GetBodyStateWeightBonus(GeoCharacter character, LeaderSelection.AffinityApproach approach)
        {
            switch (approach)
            {
                case LeaderSelection.AffinityApproach.Occult:
                    return HasArmourTag(character, TFTVMain.Shared?.SharedGameTags?.AnuMutationTag) ? OccultMutationWeightBonus : 0;

                case LeaderSelection.AffinityApproach.Machinery:
                    return HasArmourTag(character, TFTVMain.Shared?.SharedGameTags?.BionicalTag) ? MachineryBionicsWeightBonus : 0;

                default:
                    return 0;
            }
        }

        private static PassiveModifierAbilityDef GetNurseAbility()
        {
            if (_nurseAbility != null)
            {
                return _nurseAbility;
            }

            try
            {
                _nurseAbility = DefCache.GetDef<PassiveModifierAbilityDef>(NurseAbilityDefName);
            }
            catch
            {
                _nurseAbility = null;
            }

            return _nurseAbility;
        }

        private static bool HasAbility(GeoCharacter character, TacticalAbilityDef abilityDef)
        {
            if (character?.Progression?.Abilities == null || abilityDef == null)
            {
                return false;
            }

            return character.Progression.Abilities.Any(ability =>
                ability == abilityDef
                || (ability != null && string.Equals(ability.name, abilityDef.name, StringComparison.OrdinalIgnoreCase)));
        }

        private static bool HasArmourTag(GeoCharacter character, GameTagDef tag)
        {
            if (character?.ArmourItems == null || tag == null)
            {
                return false;
            }

            return character.ArmourItems.Any(item =>
                item != null
                && item.ItemDef != null
                && item.ItemDef.Tags != null
                && item.ItemDef.Tags.Contains(tag));
        }

        private static GeoVehicle ResolveMissionVehicle(GeoMission mission, GeoSquad squad)
        {
            try
            {
                if (mission?.Site == null)
                {
                    return null;
                }

                List<GeoVehicle> vehicles = mission.Site.GetPlayerVehiclesOnSite()?.Where(vehicle => vehicle != null).ToList();
                if (vehicles == null || vehicles.Count == 0)
                {
                    return null;
                }

                IEnumerable<GeoCharacter> squadUnits = Enumerable.Empty<GeoCharacter>();
                if (squad?.Units != null)
                {
                    squadUnits = squad.Units.Where(unit => unit != null);
                }
                else if (mission.Squad?.Units != null)
                {
                    squadUnits = mission.Squad.Units.Where(unit => unit != null);
                }

                foreach (GeoCharacter unit in squadUnits)
                {
                    GeoVehicle vehicle = vehicles.FirstOrDefault(candidate => candidate.Units.Any(vehicleUnit => vehicleUnit == unit));
                    if (vehicle != null)
                    {
                        return vehicle;
                    }
                }

                return vehicles.OrderByDescending(vehicle => vehicle.MaxCharacterSpace).FirstOrDefault();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return null;
            }
        }

        private static int ResolveCharacterVehicleId(GeoLevelController geoLevel, GeoCharacter character)
        {
            if (geoLevel?.PhoenixFaction?.Vehicles == null || character == null)
            {
                return -1;
            }

            GeoVehicle vehicle = geoLevel.PhoenixFaction.Vehicles.FirstOrDefault(candidate =>
                candidate != null
                && candidate.Units != null
                && candidate.Units.Any(unit => unit == character));

            return vehicle?.VehicleID ?? -1;
        }

        private static int ToInt(GeoTacUnitId unitId)
        {
            return (int)unitId;
        }

        private static string FormatAffinity(LeaderSelection.AffinityApproach approach, int rank)
        {
            PassiveModifierAbilityDef ability = LeaderSelection.GetAffinityAbility(approach, 1);
            string affinityName = ability?.ViewElementDef?.DisplayName1?.Localize() ?? approach.ToString();
            return $"{affinityName} {ToRoman(rank)}";
        }

        private static string BuildTransferToRecipientSummary(
            LeaderSelection.AffinityApproach approach,
            int rank,
            string recipientName)
        {
            string template = GetLocalizedTemplate(
                TransferToRecipientKey,
                "Affinity Passed On: {0} -> {1}");

            return string.Format(template, FormatAffinity(approach, rank), recipientName ?? "-");
        }

        private static string BuildTransferBankedSummary(LeaderSelection.AffinityApproach approach, int rank)
        {
            string template = GetLocalizedTemplate(
                TransferBankedKey,
                "Affinity Passed On: {0} banked");

            return string.Format(template, FormatAffinity(approach, rank));
        }

        private static string GetLocalizedTemplate(string key, string fallback)
        {
            string localized = global::TFTV.TFTVCommonMethods.ConvertKeyToString(key);
            if (string.IsNullOrEmpty(localized) || string.Equals(localized, key, StringComparison.Ordinal))
            {
                return fallback;
            }

            return localized;
        }

        private static string ToRoman(int rank)
        {
            switch (rank)
            {
                case 1:
                    return "I";
                case 2:
                    return "II";
                case 3:
                    return "III";
                default:
                    return rank.ToString();
            }
        }
    }
}
