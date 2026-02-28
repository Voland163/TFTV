using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Levels;
using System;

namespace TFTV.TFTVIncidents
{
    internal class AffinityTacticalEffects
    {
        private const string DiagTag = "[Incidents][AffinityTacticalEffects]";
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly RecoverWillAbilityDef RecoverWillAbilityDef = DefCache.GetDef<RecoverWillAbilityDef>("RecoverWill_AbilityDef");

        internal static bool ShouldApplyGlobalBenefit(
            TacticalLevelController level,
            LeaderSelection.AffinityApproach approach,
            int requiredOption,
            out int bestRank)
        {
            bestRank = 0;

            try
            {
                if (level == null)
                {
                    return false;
                }

                int selectedOption = Affinities.AffinityBenefitsChoices.GetTacticalBenefitChoiceFromSnapshot(approach);
                if (selectedOption != requiredOption)
                {
                    return false;
                }

                TacticalFaction phoenixFaction = level.GetFactionByCommandName("PX");
                if (phoenixFaction?.Actors == null)
                {
                    return false;
                }

                foreach (TacticalActorBase actorBase in phoenixFaction.Actors)
                {
                    if (!(actorBase is TacticalActor actor) || !actor.IsAlive || actor.IsEvacuated)
                    {
                        continue;
                    }

                    int rank = GetAffinityRankForApproach(actor, approach);
                    if (rank > bestRank)
                    {
                        bestRank = rank;
                    }
                }

                return bestRank > 0;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                bestRank = 0;
                return false;
            }
        }

        internal static void ApplyRecoveryWillpowerBonus(TacticalAbility ability, TacticalActor actor)
        {
            try
            {
                if (ability?.AbilityDef != RecoverWillAbilityDef || actor == null)
                {
                    return;
                }

                TacticalLevelController level = actor.TacticalLevel;
                if (!ShouldApplyGlobalBenefit(level, LeaderSelection.AffinityApproach.PsychoSociology, requiredOption: 1, out _))
                {
                    return;
                }

                if (actor.TacticalFaction != level.GetFactionByCommandName("PX"))
                {
                    return;
                }

                float before = actor.CharacterStats.WillPoints.Value.BaseValue;
                actor.CharacterStats.WillPoints.Add(2f);
                float after = actor.CharacterStats.WillPoints.Value.BaseValue;

                TFTVLogger.Always($"{DiagTag} Recovery bonus applied to {actor.name}: +2 WP ({before} -> {after}).");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static bool ShouldForceLootDrop(TacticalActor deadActor)
        {
            try
            {
                if (deadActor == null || deadActor.TacticalLevel == null)
                {
                    return false;
                }

                TacticalFaction phoenixFaction = deadActor.TacticalLevel.GetFactionByCommandName("PX");
                if (deadActor.TacticalFaction == phoenixFaction)
                {
                    return false;
                }

                if (!ShouldApplyGlobalBenefit(deadActor.TacticalLevel, LeaderSelection.AffinityApproach.Exploration, requiredOption: 1, out _))
                {
                    return false;
                }

                return UnityEngine.Random.Range(0f, 1f) < 0.10f;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return false;
            }
        }

        internal static int ApplyTBTVChanceReductionIfNeeded(TacticalLevelController level, int roll)
        {
            try
            {
                if (!ShouldApplyGlobalBenefit(level, LeaderSelection.AffinityApproach.Occult, requiredOption: 1, out _))
                {
                    return roll;
                }

                if (UnityEngine.Random.Range(0f, 1f) < 0.05f)
                {
                    TFTVLogger.Always($"{DiagTag} Occult tactical benefit converted TBTV roll {roll} to dud roll 31.");
                    return 31;
                }

                return roll;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return roll;
            }
        }

        private static int GetAffinityRankForApproach(TacticalActor actor, LeaderSelection.AffinityApproach approach)
        {
            if (actor == null)
            {
                return 0;
            }

            PassiveModifierAbilityDef[] affinityTrack = GetApproachAbilities(approach);
            if (affinityTrack == null || affinityTrack.Length < 3)
            {
                return 0;
            }

            for (int i = affinityTrack.Length - 1; i >= 0; i--)
            {
                PassiveModifierAbilityDef def = affinityTrack[i];
                if (def != null && actor.GetAbilityWithDef<PassiveModifierAbility>(def) != null)
                {
                    return i + 1;
                }
            }

            return 0;
        }

        private static PassiveModifierAbilityDef[] GetApproachAbilities(LeaderSelection.AffinityApproach approach)
        {
            switch (approach)
            {
                case LeaderSelection.AffinityApproach.PsychoSociology:
                    return Affinities.PsychoSociology;
                case LeaderSelection.AffinityApproach.Exploration:
                    return Affinities.Exploration;
                case LeaderSelection.AffinityApproach.Occult:
                    return Affinities.Occult;
                case LeaderSelection.AffinityApproach.Biotech:
                    return Affinities.Biotech;
                case LeaderSelection.AffinityApproach.Machinery:
                    return Affinities.Machinery;
                case LeaderSelection.AffinityApproach.Compute:
                    return Affinities.Compute;
                default:
                    return null;
            }
        }
    }
}

