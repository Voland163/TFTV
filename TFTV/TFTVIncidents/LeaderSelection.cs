using Base.Core;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Common.Game;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Tactical.Entities.Abilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TFTV.TFTVIncidents
{
    internal static class LeaderSelection
    {
        internal enum AffinityApproach
        {
            PsychoSociology,
            Exploration,
            Occult,
            Biotech,
            Machinery,
            Compute
        }

        internal sealed class LeaderSelectionResult
        {
            public GeoCharacter Character;
            public int Missions;
            public int Rank;
            public AffinityApproach Approach;

            public int LeaderId => Character?.Id ?? -1;
            public float BonusHours => 2f * Rank;
        }

        private static readonly Dictionary<string, AffinityApproach> ApproachTokenMap =
            new Dictionary<string, AffinityApproach>(StringComparer.OrdinalIgnoreCase)
            {
                { "P", AffinityApproach.PsychoSociology },
                { "E", AffinityApproach.Exploration },
                { "O", AffinityApproach.Occult },
                { "B", AffinityApproach.Biotech },
                { "M", AffinityApproach.Machinery },
                { "C", AffinityApproach.Compute }
            };

        internal static bool TrySelectLeader(GeoVehicle vehicle, GeoEventChoice choice, int choiceIndex, out LeaderSelectionResult result)
        {
            try
            {

                result = null;
                if (vehicle == null || choice == null)
                {
                    return false;
                }

                List<AffinityApproach> approaches = GetApproachesFromChoiceKey(choice.Text?.LocalizationKey, choiceIndex);
                if (approaches.Count == 0)
                {
                    return false;
                }

                List<LeaderSelectionResult> candidates = new List<LeaderSelectionResult>();
                IEnumerable<GeoCharacter> characters = vehicle.GetAllCharacters() ?? Enumerable.Empty<GeoCharacter>();

                foreach (GeoCharacter character in characters)
                {
                    if (character?.TemplateDef == null || !character.TemplateDef.IsHuman)
                    {
                        continue;
                    }

                    if (!TryGetMatchingAffinityRank(character, approaches, out int rank, out AffinityApproach approach))
                    {
                        continue;
                    }

                    candidates.Add(new LeaderSelectionResult
                    {
                        Character = character,
                        Missions = GetMissionCount(character),
                        Rank = rank,
                        Approach = approach
                    });
                }

                result = candidates
                    .OrderByDescending(c => c.Missions)
                    .ThenByDescending(c => c.Rank)
                    .FirstOrDefault();

                return result != null;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                throw;
            }
        }

        internal static GeoCharacter ResolveLeader(GeoLevelController level, GeoVehicle vehicle, int leaderId)
        {
            try
            {

                if (leaderId <= 0)
                {
                    return null;
                }

                GeoCharacter leader = (vehicle?.GetAllCharacters() ?? Enumerable.Empty<GeoCharacter>())
                    .FirstOrDefault(c => c != null && c.Id == leaderId);

                if (leader != null)
                {
                    return leader;
                }

                GeoPhoenixFaction phoenixFaction = level?.PhoenixFaction;
                if (phoenixFaction?.Characters == null)
                {
                    return null;
                }

                return phoenixFaction.Characters.FirstOrDefault(c => c != null && c.Id == leaderId);
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                throw;
            }
        }

        private static bool TryGetMatchingAffinityRank(
            GeoCharacter character,
            List<AffinityApproach> approaches,
            out int rank,
            out AffinityApproach approach)
        {

            try
            {
                rank = 0;
                approach = default(AffinityApproach);

                foreach (AffinityApproach candidate in approaches)
                {
                    int candidateRank = GetAffinityRank(character, candidate);
                    if (candidateRank > rank)
                    {
                        rank = candidateRank;
                        approach = candidate;
                    }
                }

                return rank > 0;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                throw;
            }
        }

        internal static float GetLeaderBonusHours(GeoCharacter leader, string approachTokens)
        {
            try
            {
                if (leader == null || string.IsNullOrEmpty(approachTokens))
                {
                    return 0f;
                }

                List<AffinityApproach> approaches = ParseApproachTokens(approachTokens);
                if (approaches.Count == 0)
                {
                    return 0f;
                }

                if (!TryGetMatchingAffinityRank(leader, approaches, out int rank, out _))
                {
                    return 0f;
                }

                return 2f * rank;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                throw;
            }
        }

        private static int GetAffinityRank(GeoCharacter character, AffinityApproach approach)
        {
            try
            {
                PassiveModifierAbilityDef[] abilities = GetAffinityAbilities(approach);
                if (abilities == null || abilities.Length < 3)
                {
                    return 0;
                }

                IEnumerable<TacticalAbilityDef> learned = character.Progression?.Abilities;
                if (learned == null)
                {
                    return 0;
                }

                if (learned.Contains(abilities[2]))
                {
                    return 3;
                }

                if (learned.Contains(abilities[1]))
                {
                    return 2;
                }

                if (learned.Contains(abilities[0]))
                {
                    return 1;
                }

                return 0;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                throw;
            }
        }

        private static PassiveModifierAbilityDef[] GetAffinityAbilities(AffinityApproach approach)
        {
            try
            {
                switch (approach)
                {
                    case AffinityApproach.PsychoSociology:
                        return Affinities.PsychoSociology;
                    case AffinityApproach.Exploration:
                        return Affinities.Exploration;
                    case AffinityApproach.Occult:
                        return Affinities.Occult;
                    case AffinityApproach.Biotech:
                        return Affinities.Biotech;
                    case AffinityApproach.Machinery:
                        return Affinities.Machinery;
                    case AffinityApproach.Compute:
                        return Affinities.Compute;
                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                throw;
            }
        }

        internal static string ExtractApproachTokens(string choiceKey, int choiceIndex)
        {
            try
            {
                if (string.IsNullOrEmpty(choiceKey))
                {
                    return string.Empty;
                }

                string marker = "_CHOICE_" + choiceIndex + "_";
                int index = choiceKey.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                {
                    return string.Empty;
                }

                string suffix = choiceKey.Substring(index + marker.Length);
                if (string.IsNullOrEmpty(suffix))
                {
                    return string.Empty;
                }

                string[] tokens = suffix.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
                List<string> validTokens = new List<string>();

                foreach (string token in tokens)
                {
                    if (ApproachTokenMap.ContainsKey(token))
                    {
                        validTokens.Add(token.ToUpperInvariant());
                    }
                }

                return validTokens.Count == 0 ? string.Empty : string.Join("_", validTokens);
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                throw;
            }
        }

        internal static List<AffinityApproach> ParseApproachTokens(string tokens)
        {
            try
            {

                List<AffinityApproach> approaches = new List<AffinityApproach>();
                if (string.IsNullOrEmpty(tokens))
                {
                    return approaches;
                }

                foreach (string token in tokens.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (ApproachTokenMap.TryGetValue(token, out AffinityApproach approach) && !approaches.Contains(approach))
                    {
                        approaches.Add(approach);
                    }
                }

                return approaches;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                throw;
            }
        }

        private static List<AffinityApproach> GetApproachesFromChoiceKey(string choiceKey, int choiceIndex)
        {
            try
            {

                List<AffinityApproach> approaches = new List<AffinityApproach>();
                if (string.IsNullOrEmpty(choiceKey))
                {
                    return approaches;
                }

                string marker = "_CHOICE_" + choiceIndex + "_";
                int index = choiceKey.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                {
                    return approaches;
                }

                string suffix = choiceKey.Substring(index + marker.Length);
                if (string.IsNullOrEmpty(suffix))
                {
                    return approaches;
                }

                string[] tokens = suffix.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string token in tokens)
                {
                    if (ApproachTokenMap.TryGetValue(token, out AffinityApproach approach))
                    {
                        if (!approaches.Contains(approach))
                        {
                            approaches.Add(approach);
                        }
                    }
                }

                return approaches;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                throw;
            }
        }

        private static int GetMissionCount(GeoCharacter character)
        {
            try
            {

                PhoenixStatisticsManager statsManager = GameUtl.GameComponent<PhoenixGame>()?.GetComponent<PhoenixStatisticsManager>();
                if (statsManager?.CurrentGameStats == null)
                {
                    return 0;
                }

                SoldierStats soldierStats = statsManager.CurrentGameStats.GetSoldierStat(character.Id, true)
                    ?? statsManager.CurrentGameStats.GetSoldierStat(character.Id, false);

                return Math.Max(0, soldierStats?.MissionsParticipated ?? 0);
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                throw;
            }
        }

        internal static bool TryGetCurrentAffinity(GeoCharacter character, out AffinityApproach approach, out int rank)
        {
            try
            {

                approach = default(AffinityApproach);
                rank = 0;

                if (character == null)
                {
                    return false;
                }

                foreach (AffinityApproach candidate in Enum.GetValues(typeof(AffinityApproach)))
                {
                    int candidateRank = GetAffinityRank(character, candidate);
                    if (candidateRank > rank)
                    {
                        rank = candidateRank;
                        approach = candidate;
                    }
                }

                return rank > 0;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                throw;
            }
        }

        internal static PassiveModifierAbilityDef GetAffinityAbility(AffinityApproach approach, int rank)
        {
            try
            {

                if (rank <= 0)
                {
                    return null;
                }

                PassiveModifierAbilityDef[] abilities = GetAffinityAbilities(approach);
                if (abilities == null || abilities.Length < rank)
                {
                    return null;
                }

                return abilities[rank - 1];
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                throw;
            }
        }

        internal static GeoCharacter SelectFallbackLeader(GeoVehicle vehicle)
        {
            IEnumerable<GeoCharacter> characters = vehicle?.GetAllCharacters() ?? Enumerable.Empty<GeoCharacter>();

            return characters
                .Where(c => c?.TemplateDef != null && c.TemplateDef.IsHuman)
                .OrderByDescending(GetMissionCount)
                .FirstOrDefault();
        }
    }
}