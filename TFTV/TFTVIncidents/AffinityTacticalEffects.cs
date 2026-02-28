using Base.Core;
using Base.Entities.Statuses;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions.Modifiers;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Levels;
using RootMotion.FinalIK;
using System;
using System.Linq;
using UnityEngine;

namespace TFTV.TFTVIncidents
{
    internal class AffinityTacticalEffects
    {
        private const string DiagTag = "[Incidents][AffinityTacticalEffects]";
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly RecoverWillAbilityDef RecoverWillAbilityDef = DefCache.GetDef<RecoverWillAbilityDef>("RecoverWill_AbilityDef");
        private static readonly MissionTagDef HavenDefenseTag = DefCache.GetDef<MissionTagDef>("MissionTypeHavenDefense_MissionTagDef");
        private static readonly ClassTagDef CivilianTag = GameUtl.GameComponent<SharedData>().SharedGameTags.CivilianTag;


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

        private static bool IsHavenDefenseMission(TacticalLevelController level)
        {
            try
            {
                return level?.TacMission?.MissionData?.MissionType?.MissionTags != null
                    && HavenDefenseTag != null
                    && level.TacMission.MissionData.MissionType.MissionTags.Contains(HavenDefenseTag);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return false;
            }
        }

        internal class PyschoSociolgyTacticalBenefits
        {

            internal static void ApplyRecoveryWillpowerBonus(TacticalAbility ability, TacticalActor actor)
            {
                try
                {
                    if (!TFTVBaseRework.BaseReworkUtils.BaseReworkEnabled) return;

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

            /// <summary>
            /// Need to wire this to PsychoSociology benefit 2, and change description to increase haven defense deployment by 50% per rank.
            /// </summary>
            [HarmonyPatch(typeof(GeoHavenDefenseMission), "CalculatePerParticipantDeployment")]
            public static class GeoHavenDefenseMission_CalculatePerParticipantDeployment_PsychoSociologyBonus_Patch
            {

                private static int GetAffinityRankForApproach(GeoCharacter character, LeaderSelection.AffinityApproach approach)
                {
                    if (character?.Progression?.Abilities == null)
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
                        if (def != null && character.Progression.Abilities.Contains(def))
                        {
                            return i + 1;
                        }
                    }

                    return 0;
                }

                private static int GetBestGeoRankForTacticalBenefit(
                    GeoLevelController level,
                    GeoHavenDefenseMission mission,
                    LeaderSelection.AffinityApproach approach,
                    int requiredOption)
                {
                    try
                    {
                        if (level == null || mission?.Squad?.Soldiers == null)
                        {
                            return 0;
                        }

                        int selectedOption = Affinities.AffinityBenefitsChoices.GetTacticalBenefitChoice(level, approach);
                        if (selectedOption != requiredOption)
                        {
                            return 0;
                        }

                        int bestRank = 0;

                        foreach (GeoCharacter soldier in mission.Squad.Soldiers)
                        {
                            int rank = GetAffinityRankForApproach(soldier, approach);
                            if (rank > bestRank)
                            {
                                bestRank = rank;
                            }
                        }

                        return bestRank;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        return 0;
                    }
                }



                public static void Postfix(GeoHavenDefenseMission __instance, TacMissionTypeParticipantData participant, ref int __result)
                {
                    try
                    {
                        if (!TFTVBaseRework.BaseReworkUtils.BaseReworkEnabled)
                        {
                            return;
                        }

                        if (__result <= 0 || participant == null || __instance?.Site?.Owner?.Def == null)
                        {
                            return;
                        }

                        if (participant.FactionDef != __instance.Site.Owner.Def.PPFactionDef)
                        {
                            return;
                        }

                        GeoLevelController level = __instance.Site.GeoLevel;
                        int bestRank = GetBestGeoRankForTacticalBenefit(
                            level,
                            __instance,
                            LeaderSelection.AffinityApproach.PsychoSociology,
                            requiredOption: 2);

                        if (bestRank <= 0)
                        {
                            return;
                        }

                        int originalDeployment = __result;
                        __result += Mathf.RoundToInt(originalDeployment * 0.5f * bestRank);

                        TFTVLogger.Always(
                            $"{DiagTag} Psycho-Sociology tactical benefit increased Haven defender deployment from {originalDeployment} to {__result} (rank {bestRank}).");
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }


        }

        internal class ExplorationTacticalBenefits
        {
            //Enemies more likely to drop loot
            internal static bool ShouldForceLootDrop(TacticalActor deadActor)
            {
                try
                {
                    if (!TFTVBaseRework.BaseReworkUtils.BaseReworkEnabled) return false;

                    if (deadActor == null || deadActor.TacticalLevel == null)
                    {
                        return false;
                    }

                    TacticalFaction phoenixFaction = deadActor.TacticalLevel.GetFactionByCommandName("PX");
                    if (deadActor.TacticalFaction == phoenixFaction)
                    {
                        return false;
                    }

                    if (!ShouldApplyGlobalBenefit(deadActor.TacticalLevel, LeaderSelection.AffinityApproach.Exploration, requiredOption: 1, out int bestRank))
                    {
                        return false;
                    }

                    float lootDropChance = Mathf.Clamp01(0.10f * bestRank);
                    return UnityEngine.Random.Range(0f, 1f) < lootDropChance;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return false;
                }
            }

            //Gain immediate control of friendly Haven defenders at the start of a Haven Defense mission
            private static void TakeControlOfFriendlyHavenDefender(TacticalLevelController level, int defendersToTransfer)
            {
                if (level == null || defendersToTransfer <= 0)
                {
                    return;
                }

                TacticalFaction playerFaction = level.GetFactionByCommandName("PX");
                if (playerFaction == null || level.Map == null)
                {
                    return;
                }

                TacticalActor[] defenders = level.Map.GetActors<TacticalActor>(null)
                    .Where(actor => actor != null
                        && actor.IsAlive
                        && actor.InPlay
                        && actor.TacticalFaction != null
                        && actor.TacticalFaction.ParticipantKind == TacMissionParticipant.Residents
                        && !actor.GameTags.Contains(CivilianTag))
                    .Take(defendersToTransfer)
                    .ToArray();

                if (defenders.Length == 0)
                {
                    return;
                }

                foreach (TacticalActor defender in defenders)
                {
                    defender.SetFaction(playerFaction, TacMissionParticipant.Player);
                }

                TFTVLogger.Always($"{DiagTag} Exploration tactical benefit transferred control of {defenders.Length} Haven defender(s) to player.");
            }

            //Gain WP when extracting civilians
            internal static void ApplyExplorationCivilianExtractionWillpowerBonus(TacticalActor extractedActor)
            {
                try
                {
                    TacticalLevelController level = extractedActor?.TacticalLevel;
                    if (level == null || !IsHavenDefenseMission(level) || !IsExtractedCivilian(extractedActor))
                    {
                        return;
                    }

                    if (!ShouldApplyGlobalBenefit(level, LeaderSelection.AffinityApproach.Exploration, requiredOption: 2, out int bestRank))
                    {
                        return;
                    }

                    TacticalFaction phoenixFaction = level.GetFactionByCommandName("PX");
                    if (phoenixFaction?.Actors == null)
                    {
                        return;
                    }

                    float willpowerBonus = bestRank;

                    foreach (TacticalActorBase actorBase in phoenixFaction.Actors)
                    {
                        if (!(actorBase is TacticalActor actor) || !actor.IsAlive || actor.IsEvacuated)
                        {
                            continue;
                        }

                        actor.CharacterStats.WillPoints.AddRestrictedToMax(willpowerBonus);
                    }

                    TFTVLogger.Always($"{DiagTag} Exploration tactical benefit granted +{willpowerBonus} WP to Phoenix operatives after civilian extraction.");
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static bool IsExtractedCivilian(TacticalActor actor)
            {
                return actor != null && actor.IsEvacuated && actor.GameTags.Contains(CivilianTag);
            }

            internal static void ApplyHavenDefenseMissionStartBenefits(TacticalLevelController level)
            {
                try
                {
                    if (!IsHavenDefenseMission(level))
                    {
                        return;
                    }

                    if (ShouldApplyGlobalBenefit(level, LeaderSelection.AffinityApproach.Exploration, requiredOption: 2, out int bestRank))
                    {
                        TakeControlOfFriendlyHavenDefender(level, bestRank);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        internal class OccultTacticalBenefits
        {
            internal static int ApplyStaminaDeliriumReductionBonusIfNeeded(GeoCharacter character, int deliriumReduction)
            {
                try
                {
                    if (!TFTVBaseRework.BaseReworkUtils.BaseReworkEnabled)
                    {
                        return deliriumReduction;
                    }

                    GeoLevelController level = character?.Faction?.GeoLevel;
                    if (level == null)
                    {
                        return deliriumReduction;
                    }

                    if (Affinities.AffinityBenefitsChoices.GetTacticalBenefitChoice(level, LeaderSelection.AffinityApproach.Occult) != 2)
                    {
                        return deliriumReduction;
                    }

                    int rank = GetOccultRank(character);
                    if (rank <= 0)
                    {
                        return deliriumReduction;
                    }

                    return Mathf.RoundToInt(deliriumReduction * 1.5f * rank);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return deliriumReduction;
                }
            }

            private static int GetOccultRank(GeoCharacter character)
            {
                try
                {
                    if (character?.Progression?.Abilities == null || Affinities.Occult == null || Affinities.Occult.Length < 3)
                    {
                        return 0;
                    }

                    if (character.Progression.Abilities.Contains(Affinities.Occult[2]))
                    {
                        return 3;
                    }

                    if (character.Progression.Abilities.Contains(Affinities.Occult[1]))
                    {
                        return 2;
                    }

                    return character.Progression.Abilities.Contains(Affinities.Occult[0]) ? 1 : 0;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            internal static int ApplyTBTVChanceReductionIfNeeded(TacticalLevelController level, int chance)
            {
                try
                {
                    if (!TFTVBaseRework.BaseReworkUtils.BaseReworkEnabled)
                    {
                        return chance;
                    }

                    if (!ShouldApplyGlobalBenefit(level, LeaderSelection.AffinityApproach.Occult, requiredOption: 1, out int bestRank))
                    {
                        return chance;
                    }

                    int reduction = 5 * bestRank;
                    return Math.Max(0, chance - reduction);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return chance;
                }
            }
        }

        internal class ComputeTacticalBenefits
        {
            private static readonly object DeliriumPerceptionBonusSource = new object();

            internal static void ApplyMissionStartBenefits(TacticalLevelController level)
            {
                try
                {
                    
                    if (!TFTVBaseRework.BaseReworkUtils.BaseReworkEnabled || level == null)
                    {
                        return;
                    }

                    ApplyMountedVehicleBonusAbility(level);
                   // RefreshDeliriumPerceptionBonus(level, logApplication: true);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            internal static void RefreshDeliriumPerceptionBonus(TacticalLevelController level, bool logApplication = false)
            {
                try
                {
                    if (!TFTVBaseRework.BaseReworkUtils.BaseReworkEnabled || level == null || level.Map == null)
                    {
                        return;
                    }

                    ClearDeliriumPerceptionBonus(level);

                    if (!ShouldApplyGlobalBenefit(level, LeaderSelection.AffinityApproach.Compute, requiredOption: 2, out int bestRank))
                    {
                        return;
                    }

                    if (!HasPandoranFaction(level))
                    {
                        return;
                    }

                    TacticalFaction phoenixFaction = level.GetFactionByCommandName("PX");
                    if (phoenixFaction == null)
                    {
                        return;
                    }

                    float totalDelirium = GetTotalPhoenixSquadDelirium(level);
                    float perceptionBonus = GetDeliriumPerceptionBonus(totalDelirium, bestRank);

                    if (perceptionBonus <= 0f)
                    {
                        return;
                    }

                    TacticalActor[] alliedActors = level.Map.GetActors<TacticalActor>(null)
                        .Where(actor => actor != null
                            && actor.IsAlive
                            && actor.InPlay
                            && IsPhoenixAlly(actor, phoenixFaction))
                        .ToArray();

                    foreach (TacticalActor alliedActor in alliedActors)
                    {
                        ApplyPerceptionBonus(alliedActor, perceptionBonus);
                    }

                    if (logApplication)
                    {
                        TFTVLogger.Always(
                            $"{DiagTag} Compute tactical benefit granted +{perceptionBonus:0.##} Perception to {alliedActors.Length} allied actor(s) while Pandorans are present (rank {bestRank}, squad Delirium {totalDelirium:0.##}).");
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static float GetTotalPhoenixSquadDelirium(TacticalLevelController level)
            {
                TacticalFaction phoenixFaction = level.GetFactionByCommandName("PX");
                if (phoenixFaction?.Actors == null)
                {
                    return 0f;
                }

                float totalDelirium = 0f;

                foreach (TacticalActorBase actorBase in phoenixFaction.Actors)
                {
                    TacticalActor actor = actorBase as TacticalActor;
                    if (actor == null || !actor.IsAlive || actor.IsEvacuated)
                    {
                        continue;
                    }

                    if (actor.CharacterStats?.Corruption != null)
                    {
                        totalDelirium += Mathf.Max(0f, actor.CharacterStats.Corruption.Value.BaseValue);
                    }
                }

                return totalDelirium;
            }

            private static float GetDeliriumPerceptionBonus(float totalDelirium, int bestRank)
            {
                int normalizedRank = Mathf.Clamp(bestRank, 1, 3);
                float multiplier = normalizedRank / 3f;
                float cap = 10f * normalizedRank;

                return Mathf.Min(cap, totalDelirium * multiplier);
            }

            private static void ApplyMountedVehicleBonusAbility(TacticalLevelController level)
            {
                try
                {
                    TFTVLogger.Always($"{DiagTag} Checking Compute mounted driver passive bonus at mission start: " +
                        $"{Affinities.AffinityBenefitsChoices.GetTacticalBenefitChoiceFromSnapshot(LeaderSelection.AffinityApproach.Compute)}");

                    if (Affinities.AffinityBenefitsChoices.GetTacticalBenefitChoiceFromSnapshot(LeaderSelection.AffinityApproach.Compute) != 1)
                    {
                        return;
                    }

                    TacticalFaction phoenixFaction = level.GetFactionByCommandName("PX");
                    
                    TFTVLogger.Always($"{DiagTag} Retrieved Phoenix faction: {(phoenixFaction != null ? phoenixFaction.Faction.FactionDef.GetName() : "null")} with actors count: {(phoenixFaction?.Actors != null ? phoenixFaction.Actors.Count() : 0)}");

                    if (phoenixFaction?.Actors == null)
                    {
                        return;
                    }

                    foreach (TacticalActorBase actorBase in phoenixFaction.Actors)
                    {
                        TacticalActor actor = actorBase as TacticalActor;
                    TFTVLogger.Always($"{DiagTag} Evaluating actor for mounted driver passive: {actor?.DisplayName ?? "null"}");    

                        if (actor == null || !actor.IsAlive || actor.IsEvacuated)
                        {
                            continue;
                        }

                        

                        int rank = GetAffinityRankForApproach(actor, LeaderSelection.AffinityApproach.Compute);
                        TFTVLogger.Always($"{DiagTag} Actor {actor.DisplayName ?? actor.name} is alive and in play. rank: {rank}");
                        if (rank <= 0)
                        {
                            continue;
                        }

                        ComputeMountedAbility.MountedDriverPassiveAbilityDef abilityDef =
                            ComputeMountedAbility.Defs.GetMountedDriverPassiveAbilityDef(rank);

                        TFTVLogger.Always($"{DiagTag} Retrieved ability def for rank {rank}: {(abilityDef != null ? abilityDef.name : "null")}");

                        if (abilityDef == null)
                        {
                            continue;
                        }

                        if (actor.GetAbilityWithDef<ComputeMountedAbility.MountedDriverPassiveAbility>(abilityDef) != null)
                        {
                            continue;
                        }

                        actor.AddAbility(abilityDef, actor);

                        TFTVLogger.Always(
                            $"{DiagTag} Compute tactical benefit added mounted driver passive to {actor.DisplayName ?? actor.name} at rank {rank}.");
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void ClearDeliriumPerceptionBonus(TacticalLevelController level)
            {
                TacticalFaction phoenixFaction = level.GetFactionByCommandName("PX");
                if (phoenixFaction == null || level.Map == null)
                {
                    return;
                }

                foreach (TacticalActor actor in level.Map.GetActors<TacticalActor>(null))
                {
                    if (actor == null || actor.CharacterStats == null)
                    {
                        continue;
                    }

                    BaseStat perceptionStat = actor.CharacterStats.TryGetStat(StatModificationTarget.Perception);
                    if (perceptionStat == null)
                    {
                        continue;
                    }

                    perceptionStat.RemoveStatModificationsWithSource(DeliriumPerceptionBonusSource, true);
                    actor.UpdateStats();
                }
            }

            private static void ApplyPerceptionBonus(TacticalActor actor, float bonus)
            {
                BaseStat perceptionStat = actor.CharacterStats.TryGetStat(StatModificationTarget.Perception);
                if (perceptionStat == null)
                {
                    return;
                }

                perceptionStat.RemoveStatModificationsWithSource(DeliriumPerceptionBonusSource, true);
                perceptionStat.AddStatModification(
                    new StatModification(
                        StatModificationType.Add,
                        "ComputeDeliriumPerceptionBonus",
                        bonus,
                        DeliriumPerceptionBonusSource,
                        bonus),
                    true);

                actor.UpdateStats();
            }

            private static bool HasPandoranFaction(TacticalLevelController level)
            {
                try
                {
                    return level?.Factions != null
                        && level.Factions.Any(f => f?.Faction?.FactionDef != null && f.Faction.FactionDef.MatchesShortName("aln"));
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return false;
                }
            }

            private static bool IsPhoenixAlly(TacticalActor actor, TacticalFaction phoenixFaction)
            {
                if (actor?.TacticalFaction == null || phoenixFaction == null)
                {
                    return false;
                }

                if (actor.TacticalFaction == phoenixFaction)
                {
                    return true;
                }

                if (actor.TacticalFaction.Faction?.FactionDef != null
                    && actor.TacticalFaction.Faction.FactionDef.MatchesShortName("aln"))
                {
                    return false;
                }

                TacMissionParticipant participantKind = actor.TacticalFaction.ParticipantKind;
                return participantKind == TacMissionParticipant.Player
                    || participantKind == TacMissionParticipant.Residents;
            }
        }
    }
}

      
    

