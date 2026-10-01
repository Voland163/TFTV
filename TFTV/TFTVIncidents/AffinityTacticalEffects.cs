using Base.Core;
using Base.Entities.Statuses;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Levels.ActorDeployment;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.ActorsInstance;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.Missions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

             //   TFTVLogger.Always($"{DiagTag} Checking if should apply global tactical benefit for approach {approach}: selected option {selectedOption}, required option {requiredOption}.");

                if (selectedOption != requiredOption)
                {
                    return false;
                }

                TacticalFaction phoenixFaction = level.GetFactionByCommandName("PX");

              //  TFTVLogger.Always($"{DiagTag} Retrieved Phoenix faction: {(phoenixFaction != null ? phoenixFaction.Faction.FactionDef.GetName() : "null")} with actors count: {(phoenixFaction?.Actors != null ? phoenixFaction.Actors.Count() : 0)}");

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

               // TFTVLogger.Always($"{DiagTag} Best affinity rank for approach {approach} among Phoenix operatives: {bestRank}.");

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
                bool hasHavenDefenseTag = level?.TacMission?.MissionData?.MissionType?.MissionTags != null
                    && HavenDefenseTag != null
                    && level.TacMission.MissionData.MissionType.MissionTags.Contains(HavenDefenseTag);

                TFTVLogger.Always($"{DiagTag} Checking if mission is Haven Defense: {hasHavenDefenseTag} (mission: {level?.TacMission?.MissionData?.MissionType?.name ?? "null"})");

                return hasHavenDefenseTag;
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
                    if (!TFTVBaseRework.BaseReworkCheck.BaseReworkEnabled)
                    {
                        return;
                    }

                    if (ability?.AbilityDef != RecoverWillAbilityDef || actor == null)
                    {
                        return;
                    }

                    TacticalLevelController level = actor.TacticalLevel;
                    if (!ShouldApplyGlobalBenefit(
                        level,
                        LeaderSelection.AffinityApproach.PsychoSociology,
                        requiredOption: 1,
                        out int bestRank))
                    {
                        return;
                    }

                    if (actor.TacticalFaction != level.GetFactionByCommandName("PX"))
                    {
                        return;
                    }

                    float bonusWillpower = 2f * bestRank;
                    float before = actor.CharacterStats.WillPoints.Value.BaseValue;
                    actor.CharacterStats.WillPoints.Add(bonusWillpower);
                    float after = actor.CharacterStats.WillPoints.Value.BaseValue;

                    TFTVLogger.Always(
                        $"{DiagTag} Recovery bonus applied to {actor.name}: +{bonusWillpower} WP ({before} -> {after}, rank {bestRank}).");
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }



            /// <summary>
            /// Psycho-Sociology tactical benefit 2: one more friendly Haven defender per affinity rank
            /// in Haven Defense missions, drawn from the same pool as the others.
            ///
            /// The defenders' deployment is a point budget: at mission start the spawner keeps drawing
            /// weighted-random units from the haven's pool while the next draw still fits the remaining
            /// points, and stops at the first one that does not. Adding points would only buy more
            /// defenders on average, so instead, each time the spawner is about to stop, this grants
            /// exactly the points the unit it just drew costs - once per rank. Each bonus defender is
            /// then an ordinary draw from the pool, so it is the same kind and level as the rest.
            ///
            /// Initial deployment only: DeployForTurn(0) runs once, from TacMission.OnLevelStart, on a
            /// fresh mission (a loaded save restores the spawns instead). The rank is read from the
            /// Phoenix squad's deployment data rather than from Phoenix actors, because participants
            /// deploy in the order the mission's rules set and the squad may not be on the map yet.
            /// </summary>
            internal static class HavenDefenderBonus
            {
                private static readonly AccessTools.FieldRef<TacParticipantSpawn, ActorDeployData> NextDeploymentRef =
                    AccessTools.FieldRefAccess<TacParticipantSpawn, ActorDeployData>("_nextDeployment");

                private static readonly MethodInfo GenerateNextActorToDeployMethod =
                    AccessTools.Method(typeof(TacParticipantSpawn), "GenerateNextActorToDeploy");

                /// <summary>How many times a civilian draw is re-rolled before a bonus is given up.</summary>
                private const int MaxRedraws = 10;

                // The spawn being deployed with a bonus, and its bookkeeping. Only ever set for the
                // duration of one DeployForTurn call.
                private static TacParticipantSpawn _spawn;
                private static int _bonusRemaining;
                private static int _bonusSpawned;
                private static float _pendingGrant;

                private static int GetSquadRank(TacMission tacMission)
                {
                    PassiveModifierAbilityDef[] track = GetApproachAbilities(LeaderSelection.AffinityApproach.PsychoSociology);
                    TacMissionFactionData phoenix = tacMission?.MissionData?.MissionParticipants?
                        .FirstOrDefault(p => p != null && p.ParticipantKind == TacMissionParticipant.Player);

                    if (track == null || phoenix?.ActorDeployData == null)
                    {
                        return 0;
                    }

                    int bestRank = 0;

                    foreach (ActorDeployData deployData in phoenix.ActorDeployData)
                    {
                        TacticalAbilityDef[] abilities = (deployData?.ActorInstance as TacCharacterData)?.Abilites;
                        if (abilities == null)
                        {
                            continue;
                        }

                        for (int i = track.Length - 1; i >= bestRank; i--)
                        {
                            if (track[i] != null && abilities.Contains(track[i]))
                            {
                                bestRank = i + 1;
                                break;
                            }
                        }
                    }

                    return bestRank;
                }

                private static int GetBonusDefenders(TacParticipantSpawn spawn, int turnNumber)
                {
                    if (!TFTVBaseRework.BaseReworkCheck.BaseReworkEnabled
                        || turnNumber != 0
                        || spawn?.MissionFactionData == null
                        || spawn.ParticipantKind != TacMissionParticipant.Residents)
                    {
                        return 0;
                    }

                    if (spawn.MissionFactionData.InitialDeploymentPoints <= 0f
                        || spawn.TacMission?.MissionData?.MissionType?.MissionTags == null
                        || HavenDefenseTag == null
                        || !spawn.TacMission.MissionData.MissionType.MissionTags.Contains(HavenDefenseTag))
                    {
                        return 0;
                    }

                    // Haven defenders are hostile under Void Omen #5 - more of them would only help the enemy.
                    if (TFTVVoidOmens.VoidOmensCheck[5])
                    {
                        TFTVLogger.Always($"{DiagTag} Skipping Psycho-Sociology haven-defense bonus: haven defenders are hostile (VO#5 active).");
                        return 0;
                    }

                    if (Affinities.AffinityBenefitsChoices.GetTacticalBenefitChoiceFromSnapshot(LeaderSelection.AffinityApproach.PsychoSociology) != 2)
                    {
                        return 0;
                    }

                    return GetSquadRank(spawn.TacMission);
                }

                private static bool IsCivilian(ActorDeployData deployData)
                {
                    GameTagDef[] tags = deployData?.ActorInstance?.GameTags
                        ?? (deployData?.InstanceDef as TacCharacterDef)?.Data?.GameTags;

                    return tags != null && CivilianTag != null && tags.Contains(CivilianTag);
                }

                /// <summary>
                /// The bonus is for defenders. If the draw that would use it is a civilian, draw again.
                /// </summary>
                private static ActorDeployData EnsureDefenderDrawn(TacParticipantSpawn spawn, ActorDeployData next)
                {
                    for (int attempt = 0; attempt < MaxRedraws && next != null && IsCivilian(next); attempt++)
                    {
                        next = GenerateNextActorToDeployMethod?.Invoke(spawn, new object[] { new List<ActorDeployData>(), false }) as ActorDeployData;
                        NextDeploymentRef(spawn) = next;
                    }

                    return next != null && !IsCivilian(next) ? next : null;
                }

                [HarmonyPatch(typeof(TacParticipantSpawn), nameof(TacParticipantSpawn.DeployForTurn))]
                internal static class TacParticipantSpawn_DeployForTurn_Patch
                {
                    static bool Prepare() => TFTVAircraftReworkMain.AircraftReworkOn;

                    private static void Prefix(TacParticipantSpawn __instance, int turnNumber)
                    {
                        try
                        {
                            _spawn = null;
                            _pendingGrant = 0f;
                            _bonusSpawned = 0;
                            _bonusRemaining = GetBonusDefenders(__instance, turnNumber);

                            if (_bonusRemaining > 0)
                            {
                                _spawn = __instance;
                                TFTVLogger.Always($"{DiagTag} Psycho-Sociology tactical benefit (rank {_bonusRemaining}): deploying {_bonusRemaining} extra Haven defender(s).");
                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                    }

                    private static Exception Finalizer(TacParticipantSpawn __instance, Exception __exception)
                    {
                        try
                        {
                            if (_spawn != null && _spawn == __instance)
                            {
                                // Points granted for a defender that then could not be placed would otherwise
                                // be left over for reinforcements.
                                if (_pendingGrant > 0f)
                                {
                                    __instance.MissionFactionData.InitialDeploymentPoints -= _pendingGrant;
                                }

                                TFTVLogger.Always($"{DiagTag} Psycho-Sociology tactical benefit deployed {_bonusSpawned} extra Haven defender(s)" +
                                    (_bonusRemaining > 0 ? $"; {_bonusRemaining} could not be placed." : "."));
                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                        finally
                        {
                            _spawn = null;
                            _pendingGrant = 0f;
                            _bonusRemaining = 0;
                        }

                        return __exception;
                    }
                }

                [HarmonyPatch(typeof(TacParticipantSpawn), "GetDeploymentPointsForTurn")]
                internal static class TacParticipantSpawn_GetDeploymentPointsForTurn_Patch
                {
                    static bool Prepare() => TFTVAircraftReworkMain.AircraftReworkOn;

                    private static void Postfix(TacParticipantSpawn __instance, ref float __result)
                    {
                        try
                        {
                            if (_spawn == null || _spawn != __instance || _bonusRemaining <= 0 || _pendingGrant > 0f)
                            {
                                return;
                            }

                            ActorDeployData next = NextDeploymentRef(__instance);
                            if (next == null || next.DeployCost <= __result)
                            {
                                // The haven's own budget still covers it: not our turn yet.
                                return;
                            }

                            next = EnsureDefenderDrawn(__instance, next);
                            if (next == null)
                            {
                                return;
                            }

                            float grant = next.DeployCost - __result;
                            if (grant <= 0f)
                            {
                                return;
                            }

                            __instance.MissionFactionData.InitialDeploymentPoints += grant;
                            __result += grant;
                            _pendingGrant = grant;
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                    }
                }

                [HarmonyPatch(typeof(TacParticipantSpawn), "ActorSpawned")]
                internal static class TacParticipantSpawn_ActorSpawned_Patch
                {
                    static bool Prepare() => TFTVAircraftReworkMain.AircraftReworkOn;

                    private static void Postfix(TacParticipantSpawn __instance, ActorDeployData actorData)
                    {
                        if (_spawn == null || _spawn != __instance || _pendingGrant <= 0f)
                        {
                            return;
                        }

                        _pendingGrant = 0f;
                        _bonusRemaining--;
                        _bonusSpawned++;
                        TFTVLogger.Always($"{DiagTag} Extra Haven defender deployed: {actorData?.GetName()} (cost {actorData?.DeployCost}).");
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
                    if (!TFTVBaseRework.BaseReworkCheck.BaseReworkEnabled) return false;

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

                    float lootDropChance = Mathf.Clamp01(0.15f * bestRank);
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

                TFTVLogger.Always($"{DiagTag} Attempting to transfer control of friendly Haven defenders to player. Player faction: {(playerFaction != null ? playerFaction.Faction.FactionDef.GetName() : "null")}, defenders to transfer: {defendersToTransfer}");

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

                TFTVLogger.Always($"{DiagTag} Found {defenders.Length} eligible Haven defenders for control transfer.");

                if (defenders.Length == 0)
                {
                    return;
                }

                foreach (TacticalActor defender in defenders)
                {

                    MindControlStatusDef underPhoenixControlStatus = DefCache.GetDef<MindControlStatusDef>("UnderPhoenixControl_StatusDef");
                    TriggerAbilityZoneOfControlStatusDef triggerAbilityZoneOfControlStatus = DefCache.GetDef<TriggerAbilityZoneOfControlStatusDef>("CanBeRecruitedIntoPhoenix_1x1_StatusDef");

                    if (defender.Status.HasStatus(triggerAbilityZoneOfControlStatus))
                    {
                        defender.Status.UnapplyStatus(defender.Status.GetStatus<TriggerAbilityZoneOfControlStatus>());
                    }


                    if (!defender.Status.HasStatus(underPhoenixControlStatus))
                    {
                        defender.Status.ApplyStatus(underPhoenixControlStatus);
                    }

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
                    if (level == null || !IsExtractedCivilian(extractedActor))
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

                        actor?.CharacterStats?.WillPoints?.AddRestrictedToMax(willpowerBonus);
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

                    // Haven defenders are hostile under Void Omen #5 – affinity benefits that rely on
                    // friendly defenders must not run in that case.
                    if (TFTVVoidOmens.VoidOmensCheck[5])
                    {
                        TFTVLogger.Always($"{DiagTag} Skipping Exploration haven-defense benefit: haven defenders are hostile (VO#5 active).");
                        return;
                    }

                    if (ShouldApplyGlobalBenefit(level, LeaderSelection.AffinityApproach.Exploration, requiredOption: 2, out int bestRank))
                    {
                        TFTVLogger.Always($"{DiagTag} Applying Exploration tactical benefit for Haven Defense mission start: transferring control of {bestRank} friendly defender(s) to player.");

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
                    if (!TFTVBaseRework.BaseReworkCheck.BaseReworkEnabled)
                    {
                        return deliriumReduction;
                    }

                    if (character == null)
                    {
                        return deliriumReduction;
                    }

                    GeoFaction faction;
                    try { faction = character.Faction; }
                    catch { return deliriumReduction; }

                    GeoLevelController level = faction?.GeoLevel;
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

            private static bool TryGetBestOccultTBTVReductionSource(
    TacticalLevelController level,
    out TacticalActor sourceActor,
    out int bestRank)
            {
                sourceActor = null;
                bestRank = 0;

                try
                {
                    if (level == null)
                    {
                        return false;
                    }

                    if (Affinities.AffinityBenefitsChoices.GetTacticalBenefitChoiceFromSnapshot(LeaderSelection.AffinityApproach.Occult) != 1)
                    {
                        return false;
                    }

                    TacticalFaction phoenixFaction = level.GetFactionByCommandName("PX");
                    if (phoenixFaction?.Actors == null)
                    {
                        return false;
                    }

                    foreach (TacticalActor actor in phoenixFaction.Actors.OfType<TacticalActor>())
                    {
                        if (!actor.IsAlive || actor.IsEvacuated)
                        {
                            continue;
                        }

                        int rank = GetAffinityRankForApproach(actor, LeaderSelection.AffinityApproach.Occult);
                        if (rank > bestRank)
                        {
                            bestRank = rank;
                            sourceActor = actor;
                        }
                    }

                    return sourceActor != null && bestRank > 0;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    sourceActor = null;
                    bestRank = 0;
                    return false;
                }
            }

            internal static bool TryGetTBTVChanceReductionInfo(
                TacticalLevelController level,
                out TacticalActor sourceActor,
                out int reduction)
            {
                sourceActor = null;
                reduction = 0;

                try
                {
                    if (!TFTVBaseRework.BaseReworkCheck.BaseReworkEnabled)
                    {
                        return false;
                    }

                    if (!TryGetBestOccultTBTVReductionSource(level, out sourceActor, out int bestRank))
                    {
                        return false;
                    }

                    reduction = 5 * bestRank;
                    return reduction > 0;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    sourceActor = null;
                    reduction = 0;
                    return false;
                }
            }

            internal static int ApplyTBTVChanceReductionIfNeeded(TacticalLevelController level, int chance)
            {
                try
                {
                    if (!TryGetTBTVChanceReductionInfo(level, out TacticalActor _, out int reduction))
                    {
                        return chance;
                    }

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

                    if (!TFTVBaseRework.BaseReworkCheck.BaseReworkEnabled || level == null)
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
                    if (!TFTVBaseRework.BaseReworkCheck.BaseReworkEnabled || level == null || level.Map == null)
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

                    // TFTVLogger.Always($"{DiagTag} Retrieved Phoenix faction: {(phoenixFaction != null ? phoenixFaction.Faction.FactionDef.GetName() : "null")} with actors count: {(phoenixFaction?.Actors != null ? phoenixFaction.Actors.Count() : 0)}");

                    if (phoenixFaction?.Actors == null)
                    {
                        return;
                    }

                    foreach (TacticalActorBase actorBase in phoenixFaction.Actors)
                    {
                        TacticalActor actor = actorBase as TacticalActor;
                        // TFTVLogger.Always($"{DiagTag} Evaluating actor for mounted driver passive: {actor?.DisplayName ?? "null"}");    

                        if (actor == null || !actor.IsAlive || actor.IsEvacuated)
                        {
                            continue;
                        }



                        int rank = GetAffinityRankForApproach(actor, LeaderSelection.AffinityApproach.Compute);
                        // TFTVLogger.Always($"{DiagTag} Actor {actor.DisplayName ?? actor.name} is alive and in play. rank: {rank}");
                        if (rank <= 0)
                        {
                            continue;
                        }

                        ComputeMountedAbility.MountedDriverPassiveAbilityDef abilityDef =
                            ComputeMountedAbility.Defs.GetMountedDriverPassiveAbilityDef(rank);

                        //  TFTVLogger.Always($"{DiagTag} Retrieved ability def for rank {rank}: {(abilityDef != null ? abilityDef.name : "null")}");

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




