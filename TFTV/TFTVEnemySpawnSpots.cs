using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Levels.ActorDeployment;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.ActorsInstance;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.ActorDeployment;
using PhoenixPoint.Tactical.Levels.Missions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TFTV
{
    /// <summary>
    /// Where the game spawns the player's enemies, nudged so that they start out of the player's sight and not on
    /// top of each other, whenever the zone the game picked has such a spot.
    ///
    /// The game chooses a zone and a spot in it, then spawns there (TacParticipantSpawn.DoSpawnActor). Its own
    /// hidden-spot search only runs on missions whose rules ask for it, tries a handful of random spots and then
    /// spawns anywhere; its last-resort pass takes the zone's first free spot, so later units line up next to the
    /// first. Here the spot is re-chosen inside the same zone, from the same list the game uses: the zone's
    /// positions minus the enemy clearance range, then the zone's own check for the unit's size and free space
    /// (which also carries TFTV's base defense culls). The new spot is always one the game would have accepted,
    /// so no unit is ever lost to it; with nothing better in the zone the game's spot stands.
    ///
    /// Preference, nearest to the game's spot first: unseen and 2 m clear of every unit; then unseen; then 2 m
    /// clear. Squad members stay within their leader's squad range, and units the game put inside a building stay
    /// inside one. Classes the mission's rules let spawn in sight (and permanently revealed units, if the rules
    /// say so) are only spread out, not hidden.
    /// </summary>
    internal static class TFTVEnemySpawnSpots
    {
        /// <summary>Centre to centre distance to any other unit that counts as not bunched up.</summary>
        private const float MinSpacing = 2f;

        /// <summary>Sight checks (a line test against every player unit) allowed per spawn.</summary>
        private const int MaxSightChecks = 60;

        /// <summary>Spawn-time perception can be lower than at the start of the turn; this much more range is assumed.</summary>
        private const float SightRangeMargin = 1.2f;

        /// <summary>Sideways distance of the shoulder points from the body's centre line.</summary>
        private const float ShoulderOffset = 0.35f;

        /// <summary>Height of the chest point as a share of the eye height.</summary>
        private const float ChestHeightShare = 0.6f;

        private static readonly MethodInfo GetAllowedZonePositionsMethod = AccessTools.Method(typeof(TacParticipantSpawn), "GetAllowedZonePositions");
        private static readonly MethodInfo IsPositionInsideBuildingMethod = AccessTools.Method(typeof(TacParticipantSpawn), "IsPositionInsideBuilding");

        // Set while the game places a squad member next to its leader.
        private static TacticalActorBase _squadLeader;
        private static float _squadRange;

        [HarmonyPatch(typeof(TacParticipantSpawn), "TrySpawnActorNearLeader")]
        internal static class TacParticipantSpawn_TrySpawnActorNearLeader_SpawnSpots_Patch
        {
            public static void Prefix(TacticalActorBase squadLeader, float maxLeaderDistance)
            {
                _squadLeader = squadLeader;
                _squadRange = maxLeaderDistance;
            }

            public static Exception Finalizer(Exception __exception)
            {
                _squadLeader = null;
                return __exception;
            }
        }

        [HarmonyPatch(typeof(TacParticipantSpawn), "DoSpawnActor")]
        internal static class TacParticipantSpawn_DoSpawnActor_SpawnSpots_Patch
        {
            public static void Prefix(TacParticipantSpawn __instance, int turnNumber, ActorDeployData deploymentData, TacticalDeployZone zone, ref Vector3 pos)
            {
                _estimatedStealth = null;

                try
                {
                    Vector3? better = ChooseSpot(__instance, turnNumber, deploymentData, zone, pos);

                    if (better.HasValue)
                    {
                        pos = better.Value;
                    }
                }
                catch (Exception e)
                {
                    // Never in the way of a spawn: the game's own spot stands.
                    TFTVLogger.Error(e);
                }
            }

            /// <summary>Checks the stealth estimate against the unit's real one, so a wrong guess shows up in the log.</summary>
            public static void Postfix(TacticalActorBase __result)
            {
                try
                {
                    if (_estimatedStealth.HasValue && __result is TacticalActor actor && actor.CharacterStats != null)
                    {
                        float real = actor.CharacterStats.Stealth;

                        if (Mathf.Abs(real - _estimatedStealth.Value) > 0.01f)
                        {
                            TFTVLogger.Always($"[SpawnSpots] {actor.DisplayName} has stealth {real:+0%;-0%;0%} once spawned, estimated {_estimatedStealth.Value:+0%;-0%;0%}");
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
                finally
                {
                    _estimatedStealth = null;
                }
            }
        }

        /// <summary>Stealth worked out for the unit being spawned, for the postfix to compare.</summary>
        private static float? _estimatedStealth;

        /// <summary>
        /// The unit's stealth before it exists: the sum of its armour's (and any other equipped item's) stealth. The
        /// game's spawn sight test leaves stealth out, but the real one multiplies the watcher's range by
        /// 1 - stealth, so a unit in heavy armour (stealth -30%) is seen 30% further away.
        /// </summary>
        private static float EstimateStealth(ActorDeployData deploymentData)
        {
            TacCharacterData data = deploymentData.ActorInstance as TacCharacterData
                ?? (deploymentData.InstanceDef as TacCharacterDef)?.Data;

            if (data == null)
            {
                return 0f;
            }

            float stealth = 0f;

            foreach (ItemDef item in (data.BodypartItems ?? new ItemDef[0]).Concat(data.EquipmentItems ?? new ItemDef[0]))
            {
                if (item is TacticalItemDef tacticalItem && tacticalItem.BodyPartAspectDef != null)
                {
                    stealth += tacticalItem.BodyPartAspectDef.Stealth;
                }
            }

            return stealth;
        }

        private static Vector3? ChooseSpot(TacParticipantSpawn spawn, int turnNumber, ActorDeployData deploymentData, TacticalDeployZone zone, Vector3 pos)
        {
            if (spawn == null || deploymentData == null || zone == null || spawn.ParticipantKind == TacMissionParticipant.Player
                || GetAllowedZonePositionsMethod == null)
            {
                return null;
            }

            TacticalFaction faction = spawn.TacticalFaction;
            TacticalLevelController level = spawn.TacMission?.TacticalLevel;

            if (faction == null || level == null || faction.IsControlledByPlayer)
            {
                return null;
            }

            List<TacticalFaction> players = level.Factions.Where(f => f.IsControlledByPlayer && faction.GetRelationTo(f) == FactionRelation.Enemy).ToList();

            if (players.Count == 0)
            {
                return null;
            }

            List<TacticalActorBase> watchers = players
                .SelectMany(f => f.TacticalActors)
                .Where(a => a != null && a.IsAlive && a.IsActive && a.TacticalPerceptionBase != null)
                .Cast<TacticalActorBase>()
                .ToList();

            List<Vector3> others = level.Factions
                .SelectMany(f => f.TacticalActors)
                .Where(a => a != null && a.IsAlive)
                .Select(a => a.Pos)
                .ToList();

            ComponentSetDef setDef = deploymentData.ComponentSetDef;
            TacticalActorBaseDef actorDef = setDef?.Components?.OfType<TacticalActorBaseDef>().FirstOrDefault();
            TacticalPerceptionBaseDef perceptionDef = setDef?.Components?.OfType<TacticalPerceptionBaseDef>().FirstOrDefault();

            if (setDef == null)
            {
                return null;
            }

            bool mayHide = watchers.Count > 0 && !RulesSkipSight(spawn.Rules, actorDef, perceptionDef);
            int sightChecks = 0;
            float visionHeight = perceptionDef != null ? perceptionDef.VisionHeight : 1.6f;
            string seenBy = null;

            float stealth = EstimateStealth(deploymentData);
            _estimatedStealth = stealth;
            float rangeMultiplier = SightRangeMargin * Mathf.Max(0f, 1f - stealth);

            bool Seen(Vector3 p)
            {
                sightChecks++;

                foreach (TacticalActorBase watcher in watchers)
                {
                    if (SeesAnyBodyPoint(watcher, setDef, p, visionHeight, rangeMultiplier))
                    {
                        seenBy = $"{watcher.DisplayName} {Vector3.Distance(watcher.Pos, p):0.0}m";
                        return true;
                    }
                }

                return false;
            }

            bool Spaced(Vector3 p) => others.All(o => (o - p).sqrMagnitude >= MinSpacing * MinSpacing);

            bool hereSeen = mayHide && Seen(pos);
            string hereSeenBy = hereSeen ? seenBy : null;
            bool hereSpaced = Spaced(pos);
            string unit = deploymentData.InstanceDef?.name ?? setDef.name;

            if (!hereSeen && hereSpaced)
            {
                TacticalActorBase nearest = watchers.OrderBy(w => (w.Pos - pos).sqrMagnitude).FirstOrDefault();

                TFTVLogger.Always($"[SpawnSpots] {unit} ({faction.TacticalFactionDef.name}, turn {turnNumber}, stealth {stealth:+0%;-0%;0%}) keeps {pos} in {zone.name}: " +
                    (mayHide
                        ? $"unseen (nearest soldier {(nearest != null ? $"{nearest.DisplayName} {Vector3.Distance(nearest.Pos, pos):0.0}m" : "none")}), spaced"
                        : $"spaced; not hidden ({(watchers.Count == 0 ? "no soldier on the map yet" : "the mission's rules let it spawn in sight")})"));
                return null;
            }

            List<Vector3> candidates = Candidates(spawn, turnNumber, deploymentData, zone, pos);

            if (candidates.Count == 0)
            {
                return null;
            }

            List<Vector3> spaced = candidates.Where(Spaced).ToList();
            Vector3? choice = null;
            string reason = null;

            if (mayHide)
            {
                // Unseen and clear of everyone, else (when the game's spot is seen) just unseen.
                foreach (Vector3 p in spaced)
                {
                    if (sightChecks >= MaxSightChecks)
                    {
                        break;
                    }

                    if (!Seen(p))
                    {
                        choice = p;
                        reason = hereSeen ? "out of sight" : "spread out";
                        break;
                    }
                }

                if (choice == null && hereSeen)
                {
                    foreach (Vector3 p in candidates.Where(c => !spaced.Contains(c)))
                    {
                        if (sightChecks >= MaxSightChecks)
                        {
                            break;
                        }

                        if (!Seen(p))
                        {
                            choice = p;
                            reason = "out of sight, no room to spread out";
                            break;
                        }
                    }
                }
            }

            // Spreading out never costs a hidden spot: only when the game's spot is seen anyway, or hiding isn't asked for.
            if (choice == null && !hereSpaced && spaced.Count > 0 && (hereSeen || !mayHide))
            {
                choice = spaced[0];
                reason = hereSeen ? "spread out, no unseen spot in its zone" : "spread out";
            }

            if (choice == null)
            {
                TFTVLogger.Always($"[SpawnSpots] {unit} ({faction.TacticalFactionDef.name}, turn {turnNumber}, stealth {stealth:+0%;-0%;0%}) stays at {pos} in {zone.name}: " +
                    (hereSeen
                        ? $"seen by {hereSeenBy}, no unseen spot among {candidates.Count} ({sightChecks} sight checks)"
                        : $"crowded, no better spot among {candidates.Count}"));

                return null;
            }

            TFTVLogger.Always($"[SpawnSpots] {unit} ({faction.TacticalFactionDef.name}, turn {turnNumber}, stealth {stealth:+0%;-0%;0%}) {pos} -> {choice.Value} " +
                $"({Vector3.Distance(pos, choice.Value):0.0}m) in {zone.name}: {reason}{(hereSeen ? $" (was seen by {hereSeenBy})" : "")}; " +
                $"{candidates.Count} spots, {sightChecks} sight checks");

            return choice;
        }

        /// <summary>
        /// The game's own spawn sight test only looks at the unit's eye point, with the watcher's perception as it is
        /// at spawn. The real vision check at the start of the turn looks at every point of the body, with perception
        /// as it stands then, and stretches the range by the target's lack of stealth, so units it called hidden were
        /// seen. This looks at the eye, a shoulder either side (across the watcher's line of sight) and the chest, over
        /// <see cref="SightRangeMargin"/> times the range, times 1 - the unit's stealth.
        /// </summary>
        private static bool SeesAnyBodyPoint(TacticalActorBase watcher, ComponentSetDef setDef, Vector3 p, float visionHeight, float rangeMultiplier)
        {
            Vector3 across = p - watcher.Pos;
            across.y = 0f;
            Vector3 side = across.sqrMagnitude > 0.01f ? Vector3.Cross(Vector3.up, across.normalized) * ShoulderOffset : Vector3.right * ShoulderOffset;
            Vector3 chest = Vector3.up * (visionHeight * (ChestHeightShare - 1f));

            foreach (Vector3 offset in new[] { Vector3.zero, side, -side, chest })
            {
                if (TacticalFactionVision.CheckVisibleLineBetweenActorsInTheory(watcher, watcher.Pos, setDef, p + offset, rangeMultiplier))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The zone's spots for this unit as the game itself works them out (clearance from enemies, then size and
        /// free space), nearest to the game's spot first, with the squad and inside-building limits applied.
        /// </summary>
        private static List<Vector3> Candidates(TacParticipantSpawn spawn, int turnNumber, ActorDeployData deploymentData, TacticalDeployZone zone, Vector3 pos)
        {
            bool reinforcement = turnNumber > 0;
            TacDeploymentRulesDef.ParticipantDeploymentRule rules = spawn.Rules;
            float clearance = rules == null ? 0f : reinforcement ? rules.ReinforcementsEnemyClearanceRange : rules.InitialEnemyClearanceRange;

            List<Vector3> allowed = AllowedPositions(spawn, zone, clearance, reinforcement);

            // The game falls back to no clearance when a zone has too little room; so does this.
            if (allowed.Count == 0 && clearance > 0f)
            {
                allowed = AllowedPositions(spawn, zone, 0f, reinforcement);
            }

            if (allowed.Count == 0)
            {
                return allowed;
            }

            // A shared cache inside the zone: copy it before anything else asks.
            List<Vector3> valid = zone.GetValidSpawnPosition(deploymentData.ComponentSetDef, deploymentData.DeploymentTags, allowed)?.ToList() ?? new List<Vector3>();

            if (_squadLeader != null)
            {
                Vector3 leader = _squadLeader.Pos;
                valid.RemoveAll(p => Vector3.Distance(p, leader) > _squadRange);
            }

            if (IsPositionInsideBuildingMethod != null && InsideBuilding(spawn, pos))
            {
                valid.RemoveAll(p => !InsideBuilding(spawn, p));
            }

            valid.RemoveAll(p => (p - pos).sqrMagnitude < 0.01f);

            return valid.OrderBy(p => (p - pos).sqrMagnitude).ToList();
        }

        private static List<Vector3> AllowedPositions(TacParticipantSpawn spawn, TacticalDeployZone zone, float clearance, bool reinforcement)
        {
            object result = GetAllowedZonePositionsMethod.Invoke(spawn, new object[] { new List<TacticalDeployZone> { zone }, clearance, reinforcement });

            return result is Dictionary<TacticalDeployZone, List<Vector3>> byZone && byZone.TryGetValue(zone, out List<Vector3> positions) && positions != null
                ? new List<Vector3>(positions)
                : new List<Vector3>();
        }

        private static bool InsideBuilding(TacParticipantSpawn spawn, Vector3 p)
        {
            return IsPositionInsideBuildingMethod.Invoke(spawn, new object[] { p }) is bool inside && inside;
        }

        /// <summary>The game's own exceptions to its hidden-spawn check, from the mission's deployment rules.</summary>
        private static bool RulesSkipSight(TacDeploymentRulesDef.ParticipantDeploymentRule rules, TacticalActorBaseDef actorDef, TacticalPerceptionBaseDef perceptionDef)
        {
            if (rules == null)
            {
                return false;
            }

            if (rules.SkipInitialLOSChecksForPermanentReveal && perceptionDef != null && perceptionDef.PermanentReveal)
            {
                return true;
            }

            return actorDef?.GameTags != null && rules.SkipInitialLOSChecksFor != null
                && rules.SkipInitialLOSChecksFor.Any(tag => tag != null && actorDef.GameTags.Contains(tag));
        }
    }
}
