using Base.Entities;
using Base.Levels;
using Base.Levels.Nav;
using Base.Levels.Nav.Tiled;
using HarmonyLib;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TFTV
{
    /// <summary>
    /// Unseen units no longer give themselves away through movement.
    ///
    /// Vanilla: every unit carves its tile out of the navigation grid (a nav obstacle), seen or not. The move range,
    /// the path preview and the jump targets all leave that tile out, so the player could find hidden enemies by
    /// hovering the move cursor or the jet jump arc over the map.
    ///
    /// Now:
    /// - Path searches for the player's units ignore the obstacles of units their side does not know about (neither
    ///   seen nor located). The move range, the path line and the AP cost treat those tiles as free.
    /// - Ground moves (Move, Dash, vehicles): when the move starts, the path is checked against those units. The
    ///   unit stops on the last free tile before the first one it would walk into, whether that is the target tile
    ///   or a tile on the way.
    /// - Jumps (jet jump, leaps): a tile taken by an unseen unit can be targeted. The unit lands on the nearest tile
    ///   around it that it could jump to.
    /// - After the move or jump, a unit that blocked it and is still unseen becomes located (a marker), so the player
    ///   sees why the unit stopped short.
    /// The AI is unchanged: its path searches never ignore anything.
    /// </summary>
    internal static class TFTVHiddenBlockers
    {
        private const float PathSampleStep = 0.1f;
        private const float StopSearchStep = 0.25f;
        private const float OverlapSlack = 0.1f;
        private const float HeightSlack = 0.2f;
        private const float LandingSearchRadius = 2.5f;
        private const float LandingExtraReach = 1f;
        private const float LandingMaxRise = 0.5f;
        private const int LandingFloorCasts = 3;
        private const float SameTile = 0.3f;

        /// <summary>Obstacles the current path search ignores; null outside a search for a player's unit.</summary>
        private static HashSet<NodeObstacle> _ignored;
        private static readonly HashSet<NodeObstacle> _current = new HashSet<NodeObstacle>();
        private static readonly HashSet<NodeObstacle> _lastIgnored = new HashSet<NodeObstacle>();
        private static int _depth;

        /// <summary>Units that blocked a move or jump, located when the ability ends.</summary>
        private static readonly Dictionary<TacticalAbility, List<TacticalActor>> PendingLocate = new Dictionary<TacticalAbility, List<TacticalActor>>();

        private static readonly MethodInfo VersionSetter = AccessTools.PropertySetter(typeof(NodeGraph), nameof(NodeGraph.Version));
        private static readonly MethodInfo CanJumpToMethod = AccessTools.Method(typeof(JetJumpAbility), "CanJumpTo");
        private static readonly MethodInfo InitTrajectoryMethod = AccessTools.Method(typeof(JetJumpAbility), "InitTrajectory");

        private static bool _probingJump;
        private static bool _navTypeLogged;

        internal static void ClearMission()
        {
            _ignored = null;
            _depth = 0;
            _current.Clear();
            _lastIgnored.Clear();
            PendingLocate.Clear();
        }

        private static bool IsPlayerUnit(TacticalActorBase actorBase)
        {
            return actorBase is TacticalActor actor
                && actor.IsAlive
                && actor.TacticalFaction != null
                && actor.TacticalFaction.IsControlledByPlayer
                && actor.TacticalFaction.Vision != null;
        }

        /// <summary>Alive, not of the faction, and neither seen nor located by it.</summary>
        private static bool IsUnknownTo(TacticalFaction faction, TacticalActor other)
        {
            return other != null
                && other.IsAlive
                && other.TacticalFaction != faction
                && !faction.Vision.IsRevealed(other)
                && !faction.Vision.IsLocated(other);
        }

        private static IEnumerable<TacticalActor> OtherUnits(TacticalActor mover)
        {
            return mover.Map.GetActors<TacticalActor>(null).Where(a => a != null && a != mover && a.IsAlive && !a.IsEvacuated && !a.IsMounted && a.NavigationComponent != null);
        }

        private static float Radius(TacticalActorBase actor)
        {
            return actor.NavigationComponent?.AgentNavSettings?.AgentRadius ?? 0.4f;
        }

        private static float Height(TacticalActorBase actor)
        {
            return actor.NavigationComponent?.AgentNavSettings?.AgentHeight ?? 1.8f;
        }

        /// <summary>Would the mover, standing at pos, overlap the other unit.</summary>
        private static bool Overlaps(TacticalActor mover, Vector3 pos, TacticalActor other)
        {
            Vector3 o = other.Pos;
            float reach = Radius(mover) + Radius(other) - OverlapSlack;
            float dx = pos.x - o.x;
            float dz = pos.z - o.z;

            if (dx * dx + dz * dz >= reach * reach)
            {
                return false;
            }

            return pos.y < o.y + Height(other) - HeightSlack && pos.y + Height(mover) > o.y + HeightSlack;
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static string Names(IEnumerable<TacticalActor> actors)
        {
            return string.Join(", ", actors.Select(a => a.DisplayName));
        }

        #region Path searches ignore unknown units

        /// <summary>
        /// Around every path search for a player's unit: collect the obstacles of the units its side doesn't know
        /// about. Searches are cached per graph version, so the version is bumped whenever that set changes; a cached
        /// search made with an older set is then never reused.
        /// </summary>
        [HarmonyPatch(typeof(TacticalPathRequest), nameof(TacticalPathRequest.Calculate))]
        internal static class TacticalPathRequest_Calculate_HiddenBlockers_Patch
        {
            private static void Prefix(TacticalPathRequest __instance)
            {
                _depth++;

                try
                {
                    if (_depth != 1 || __instance.NavigationSettings?.OverridePath != null || !IsPlayerUnit(__instance.Actor))
                    {
                        return;
                    }

                    TacticalActor mover = (TacticalActor)__instance.Actor;

                    // The search key includes the mover's own obstacle; without one it could share a cached search
                    // with requests that have no actor.
                    if (mover.NavigationComponent?.NavObstacle == null)
                    {
                        return;
                    }

                    _current.Clear();
                    TacticalFaction faction = mover.TacticalFaction;

                    foreach (TacticalActor other in OtherUnits(mover))
                    {
                        if (IsUnknownTo(faction, other)
                            && other.NavigationComponent.NavObstacle is TiledNavObstacle obstacle
                            && obstacle.NodeObstacle != null)
                        {
                            _current.Add(obstacle.NodeObstacle);
                        }
                    }

                    if (!_current.SetEquals(_lastIgnored))
                    {
                        BumpGraphVersions(mover);
                        _lastIgnored.Clear();
                        _lastIgnored.UnionWith(_current);
                    }

                    if (_current.Count > 0)
                    {
                        _ignored = _current;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static Exception Finalizer(Exception __exception)
            {
                _depth = Mathf.Max(0, _depth - 1);

                if (_depth == 0)
                {
                    _ignored = null;
                }

                return __exception;
            }
        }

        private static void BumpGraphVersions(TacticalActor mover)
        {
            if (!(mover.TacticalNav?.INavMesh is TiledNav tiledNav))
            {
                if (!_navTypeLogged)
                {
                    _navTypeLogged = true;
                    TFTVLogger.Always($"[HiddenBlockers] navmesh is {mover.TacticalNav?.INavMesh?.GetType().Name ?? "null"}, not TiledNav; cached paths are not refreshed when the set of unseen units changes");
                }

                return;
            }

            foreach (NodeGraphBuilder builder in tiledNav.NodeBuilders)
            {
                NodeGraph graph = builder?.NodeGraph;

                if (graph != null)
                {
                    VersionSetter.Invoke(graph, new object[] { graph.Version + 1 });
                }
            }
        }

        /// <summary>
        /// A node's walkable areas are its own areas with each obstacle on it applied, except the mover's own. During
        /// a search for a player's unit the obstacles of unknown units are skipped as well. Only nodes with an
        /// obstacle on them get here.
        /// </summary>
        [HarmonyPatch(typeof(GraphNode), "CalculateObstacleAreas")]
        internal static class GraphNode_CalculateObstacleAreas_HiddenBlockers_Patch
        {
            private static bool Prefix(GraphNode __instance, NodeObstacle obstacleToIgnore, ref NodeAreas __result)
            {
                HashSet<NodeObstacle> ignored = _ignored;

                if (ignored == null)
                {
                    return true;
                }

                uint mask = __instance.OriginalAreas.AreaMask;
                List<GraphNode.ObstacleData> obstacles = __instance.Obstacles;

                for (int i = 0; i < obstacles.Count; i++)
                {
                    GraphNode.ObstacleData data = obstacles[i];

                    if (data.Obstacle == obstacleToIgnore || ignored.Contains(data.Obstacle))
                    {
                        continue;
                    }

                    mask = (mask & ~data.Mask.AreaMask) | (data.Value.AreaMask & data.Mask.AreaMask);
                }

                __result = new NodeAreas(mask);
                return false;
            }
        }

        #endregion

        #region Ground moves stop short

        private static TacticalPathRequest NewPathRequest(TacticalActor mover, Vector3 destination)
        {
            if (!(mover.TacticalNav?.CreatePathRequest() is TacticalPathRequest request))
            {
                return null;
            }

            request.Src = mover.Pos;
            request.Dst = destination;
            request.Calculate();

            return request.Status == PathStatus.Complete && request.Path != null && request.Path.Count > 0 ? request : null;
        }

        private static List<Vector3> PathPoints(TacticalActor mover, TacticalPathRequest request)
        {
            List<Vector3> points = new List<Vector3> { mover.Pos };
            points.AddRange(request.Path.Select(n => n.Position));
            return points;
        }

        private static Vector3 PointAt(List<Vector3> points, float distance)
        {
            float walked = 0f;

            for (int i = 1; i < points.Count; i++)
            {
                float length = Vector3.Distance(points[i - 1], points[i]);

                if (walked + length >= distance)
                {
                    return length < 0.0001f ? points[i] : Vector3.Lerp(points[i - 1], points[i], (distance - walked) / length);
                }

                walked += length;
            }

            return points[points.Count - 1];
        }

        /// <summary>The first point along the path where the mover would run into a unit in the list.</summary>
        private static bool FirstBlock(TacticalActor mover, List<Vector3> points, List<TacticalActor> candidates, out float blockedAt, out TacticalActor blocker)
        {
            blockedAt = 0f;
            blocker = null;

            if (candidates.Count == 0)
            {
                return false;
            }

            float walked = 0f;

            for (int i = 1; i < points.Count; i++)
            {
                Vector3 from = points[i - 1];
                Vector3 to = points[i];
                float length = Vector3.Distance(from, to);
                int steps = Mathf.Max(1, Mathf.CeilToInt(length / PathSampleStep));

                for (int s = 1; s <= steps; s++)
                {
                    float t = (float)s / steps;
                    Vector3 p = Vector3.Lerp(from, to, t);

                    foreach (TacticalActor other in candidates)
                    {
                        if (Overlaps(mover, p, other))
                        {
                            blockedAt = walked + length * t;
                            blocker = other;
                            return true;
                        }
                    }
                }

                walked += length;
            }

            return false;
        }

        /// <summary>
        /// The tile to stop on: going back along the path from where the mover runs into the blocker, the first tile
        /// no unit stands on that the mover can walk to without running into an unknown unit. Its own tile if none.
        /// </summary>
        private static Vector3 StopShort(TacticalActor mover, List<Vector3> points, float blockedAt, List<TacticalActor> unknown, List<TacticalActor> others)
        {
            HashSet<Vector3> tried = new HashSet<Vector3>();
            float validDelta = mover.TacticalNav.ValidPositionDelta();

            for (float d = blockedAt; d >= 0f; d -= StopSearchStep)
            {
                Vector3 p = PointAt(points, d);
                Vector3 tile = TacticalMap.SnapXZToGrid(p);
                tile.y = p.y;

                if (!tried.Add(new Vector3(tile.x, Mathf.Round(tile.y * 4f) / 4f, tile.z)))
                {
                    continue;
                }

                if (FlatDistance(tile, mover.Pos) < SameTile && Mathf.Abs(tile.y - mover.Pos.y) < 1f)
                {
                    return mover.Pos;
                }

                if (others.Any(o => Overlaps(mover, tile, o)))
                {
                    continue;
                }

                TacticalPathRequest request = NewPathRequest(mover, tile);

                if (request == null)
                {
                    continue;
                }

                Vector3 end = request.Path[request.Path.Count - 1].Position;

                if ((end - tile).sqrMagnitude > validDelta * validDelta || others.Any(o => Overlaps(mover, end, o)))
                {
                    continue;
                }

                if (FirstBlock(mover, PathPoints(mover, request), unknown, out _, out _))
                {
                    continue;
                }

                return end;
            }

            return mover.Pos;
        }

        /// <summary>
        /// Before a player's ground move starts: if its path runs into a unit the player's side doesn't know about,
        /// move to the last free tile before it instead. AP is charged for the shorter move.
        /// </summary>
        [HarmonyPatch(typeof(MoveAbility), nameof(MoveAbility.Activate))]
        internal static class MoveAbility_Activate_HiddenBlockers_Patch
        {
            private static void Prefix(MoveAbility __instance, ref object parameter)
            {
                try
                {
                    TacticalActor mover = __instance.TacticalActor;

                    if (!IsPlayerUnit(mover) || !(parameter is TacticalAbilityTarget target) || !target.HasPositionToApply)
                    {
                        return;
                    }

                    TacticalFaction faction = mover.TacticalFaction;
                    List<TacticalActor> others = OtherUnits(mover).ToList();
                    List<TacticalActor> unknown = others.Where(o => IsUnknownTo(faction, o)).ToList();

                    if (unknown.Count == 0)
                    {
                        return;
                    }

                    TacticalPathRequest request = NewPathRequest(mover, target.PositionToApply);

                    if (request == null)
                    {
                        return;
                    }

                    List<Vector3> points = PathPoints(mover, request);

                    if (!FirstBlock(mover, points, unknown, out float blockedAt, out TacticalActor blocker))
                    {
                        return;
                    }

                    Vector3 stop = StopShort(mover, points, blockedAt, unknown, others);

                    TacticalAbilityTarget shortened = new TacticalAbilityTarget(target) { PositionToApply = stop };
                    parameter = shortened;
                    PendingLocate[__instance] = new List<TacticalActor> { blocker };

                    TFTVLogger.Always($"[HiddenBlockers] {mover.DisplayName} {__instance.TacticalAbilityDef?.name} to {target.PositionToApply}: runs into unseen {blocker.DisplayName} at {blocker.Pos} after {blockedAt:0.0}m of {request.Length:0.0}m; "
                        + (FlatDistance(stop, mover.Pos) < SameTile ? "it is right next to it, so the unit stays put" : $"stops at {stop} ({FlatDistance(stop, mover.Pos):0.0}m away)"));
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        #endregion

        #region Jumps land next to unseen units

        private static bool CallCanJumpTo(JetJumpAbility ability, Vector3 target)
        {
            bool wasProbing = _probingJump;
            _probingJump = true;

            try
            {
                return (bool)CanJumpToMethod.Invoke(ability, new object[] { target });
            }
            finally
            {
                _probingJump = wasProbing;
            }
        }

        private static List<TacticalActor> UnknownUnitsAt(TacticalActor mover, Vector3 pos)
        {
            TacticalFaction faction = mover.TacticalFaction;
            return OtherUnits(mover).Where(o => IsUnknownTo(faction, o) && Overlaps(mover, pos, o)).ToList();
        }

        /// <summary>
        /// The nearest tile around target that no unit stands on and the unit can jump to, at most a little further
        /// from it than the target. The floor is looked for from just above the target height downward.
        /// </summary>
        private static bool TryFindLanding(JetJumpAbility ability, TacticalActor mover, Vector3 target, out Vector3 landing)
        {
            landing = Vector3.zero;

            TacticalMap map = mover.Map;
            TacticalNavigationComponent nav = mover.TacticalNav;

            if (map == null || nav == null)
            {
                return false;
            }

            List<TacticalActor> others = OtherUnits(mover).ToList();
            float maxReach = FlatDistance(mover.Pos, target) + LandingExtraReach;
            float lift = Height(mover) * 0.5f;
            LayerMask walkable = nav.WalkableLayers;

            IEnumerable<Vector3> columns = TacticalMap.GetPositionsInRange(TacticalMap.SnapXZToGrid(target), 0f, LandingSearchRadius)
                .Where(c => FlatDistance(c, mover.Pos) <= maxReach)
                .OrderBy(c => FlatDistance(c, target))
                .ThenBy(c => FlatDistance(c, mover.Pos))
                .ToList();

            foreach (Vector3 column in columns)
            {
                Vector3 origin = new Vector3(column.x, target.y + lift, column.z);

                for (int i = 0; i < LandingFloorCasts; i++)
                {
                    CastResult floor = map.CastFirstFloorAt(origin, walkable, null);

                    if (!floor.IsValid)
                    {
                        break;
                    }

                    Vector3 point = floor.Point;
                    origin = point - Vector3.up * 0.05f;

                    if (point.y > target.y + LandingMaxRise)
                    {
                        continue;
                    }

                    if (FlatDistance(point, target) < SameTile && Mathf.Abs(point.y - target.y) < 1f)
                    {
                        break;
                    }

                    if (others.Any(o => Overlaps(mover, point, o)))
                    {
                        break;
                    }

                    if (CallCanJumpTo(ability, point))
                    {
                        landing = point;
                        return true;
                    }

                    // The first floor in a column is its landing spot; lower ones are further away.
                    break;
                }
            }

            return false;
        }

        /// <summary>
        /// A jump to a tile taken by a unit the player's side doesn't know about counts as possible when the unit
        /// could land next to it. Runs after the floor-support check of TFTVFallRescue.
        /// </summary>
        [HarmonyPatch(typeof(JetJumpAbility), "CanJumpTo")]
        internal static class JetJumpAbility_CanJumpTo_HiddenBlockers_Patch
        {
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(JetJumpAbility __instance, Vector3 target, ref bool __result)
            {
                if (__result || _probingJump)
                {
                    return;
                }

                try
                {
                    TacticalActor mover = __instance.TacticalActor;

                    if (!IsPlayerUnit(mover) || mover.IsMounted || UnknownUnitsAt(mover, target).Count == 0)
                    {
                        return;
                    }

                    if (TryFindLanding(__instance, mover, target, out _))
                    {
                        __result = true;
                    }

                    // The probes moved the jump arc; put it back on the target the game asked about.
                    InitTrajectoryMethod.Invoke(__instance, new object[] { target });
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        /// <summary>Before a player's jump starts: a target tile taken by an unknown unit becomes the nearest landing.</summary>
        [HarmonyPatch(typeof(JetJumpAbility), nameof(JetJumpAbility.Activate))]
        internal static class JetJumpAbility_Activate_HiddenBlockers_Patch
        {
            private static void Prefix(JetJumpAbility __instance, ref object parameter)
            {
                try
                {
                    TacticalActor mover = __instance.TacticalActor;

                    if (!IsPlayerUnit(mover) || !(parameter is TacticalAbilityTarget target) || !target.HasPositionToApply)
                    {
                        return;
                    }

                    List<TacticalActor> blockers = UnknownUnitsAt(mover, target.PositionToApply);

                    if (blockers.Count == 0)
                    {
                        return;
                    }

                    if (!TryFindLanding(__instance, mover, target.PositionToApply, out Vector3 landing))
                    {
                        TFTVLogger.Always($"[HiddenBlockers] {mover.DisplayName} {__instance.TacticalAbilityDef?.name} to {target.PositionToApply}: tile taken by unseen {Names(blockers)} and no landing found next to it; vanilla jump goes ahead");
                        return;
                    }

                    parameter = new TacticalAbilityTarget(target) { PositionToApply = landing };
                    PendingLocate[__instance] = blockers;

                    TFTVLogger.Always($"[HiddenBlockers] {mover.DisplayName} {__instance.TacticalAbilityDef?.name} to {target.PositionToApply}: tile taken by unseen {Names(blockers)}; lands at {landing} ({FlatDistance(landing, target.PositionToApply):0.0}m from the target)");
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        #endregion

        #region Blockers become located

        private static void LocateBlockers(TacticalAbility ability)
        {
            try
            {
                if (ability == null || !PendingLocate.TryGetValue(ability, out List<TacticalActor> blockers))
                {
                    return;
                }

                PendingLocate.Remove(ability);
                TacticalFaction faction = ability.TacticalActor?.TacticalFaction;

                if (faction?.Vision == null)
                {
                    return;
                }

                foreach (TacticalActor blocker in blockers.Where(b => IsUnknownTo(faction, b)))
                {
                    faction.Vision.IncrementKnownCounter(blocker, KnownState.Located, 1, true);
                    TFTVLogger.Always($"[HiddenBlockers] {ability.TacticalActor.DisplayName} bumped into {blocker.DisplayName}, still unseen; it is now located");
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        [HarmonyPatch(typeof(MoveAbility), "OnPlayingActionEnd")]
        internal static class MoveAbility_OnPlayingActionEnd_HiddenBlockers_Patch
        {
            private static void Postfix(MoveAbility __instance)
            {
                LocateBlockers(__instance);
            }
        }

        [HarmonyPatch(typeof(JetJumpAbility), "OnPlayingActionEnd")]
        internal static class JetJumpAbility_OnPlayingActionEnd_HiddenBlockers_Patch
        {
            private static void Postfix(JetJumpAbility __instance)
            {
                LocateBlockers(__instance);
            }
        }

        #endregion
    }
}
