using Base.Levels;
using HarmonyLib;
using PhoenixPoint.Common.Utils;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV
{
    /// <summary>
    /// Vanilla kills units that fall without finding a landing spot. When the floor under a unit goes (destruction,
    /// a jump that ends on an edge), FallNoSupportAbility.FindFallPosition looks for a landing spot. It only looks
    /// downward, inside a 30 degree cone, at most 8 m below. A spot must be standable and reachable in a straight
    /// line (or around one corner). If no spot passes, FallToFreePosition deals damage equal to the unit's current
    /// Health. That is a certain kill, however short the drop.
    ///
    /// The fix:
    /// - FindFallPosition: when vanilla finds no free spot, search a wider circle, the unit's own level included,
    ///   and land there. The fall then costs the normal 10 HP per metre dropped (none if the spot is level).
    /// - JetJumpAbility.CanJumpTo: a landing spot must also pass the floor-support test that runs after the jump,
    ///   so a jump can no longer end on a spot that sets off a fall.
    /// - TacticalFaction.KillActorIfOffNav (AI turns): put a unit that is off the navmesh back on it before vanilla
    ///   decides to kill it.
    /// </summary>
    internal static class TFTVFallRescue
    {
        private const float SearchRadius = 6f;
        private const float MaxRise = 0.5f;
        private const int MaxFloorCastsPerColumn = 3;
        private const float OccupiedTolerance = 0.1f;
        private const float OffNavTolerance = 0.25f;
        private const float OffNavSearchRadius = 4f;

        private static readonly PhysicsCast _lineCast = new PhysicsCast
        {
            LayerMask = UnityLayers.FloorWallsAll,
            SingleResult = true
        };

        /// <summary>
        /// The nearest spot around center where the actor can stand and that passes the floor-support test the fall
        /// check uses. Columns are tried nearest first; in each column the first standable floor going down from
        /// just above center is taken, so the unit's own level counts. A spot reachable in a straight line (walls and
        /// floors) wins over a closer one that is not; a blocked spot is used only if nothing else is found.
        /// </summary>
        internal static bool TryFindStandableSpot(TacticalActor actor, Vector3 center, float radius, List<Vector3> occupied, out Vector3 spot, out bool clearLine)
        {
            spot = Vector3.zero;
            clearLine = false;

            TacticalPerception perception = actor.TacticalPerception;
            TacticalMap map = actor.Map;
            TacticalNavigationComponent nav = actor.TacticalNav;

            if (perception == null || map == null || nav == null)
            {
                return false;
            }

            float halfHeight = perception.Height * 0.5f;
            LayerMask walkable = nav.WalkableLayers;
            Vector3 lineLift = Vector3.up * halfHeight;
            bool foundBlocked = false;
            Vector3 blockedSpot = Vector3.zero;

            IEnumerable<Vector3> columns = TacticalMap.GetPositionsInRange(TacticalMap.SnapXZToGrid(center), 0f, radius)
                .OrderBy(p => (p.x - center.x) * (p.x - center.x) + (p.z - center.z) * (p.z - center.z))
                .ToList();

            foreach (Vector3 column in columns)
            {
                Vector3 origin = new Vector3(column.x, center.y + halfHeight, column.z);

                for (int i = 0; i < MaxFloorCastsPerColumn; i++)
                {
                    CastResult floor = map.CastFirstFloorAt(origin, walkable, null);

                    if (!floor.IsValid)
                    {
                        break;
                    }

                    Vector3 point = floor.Point;
                    origin = point - Vector3.up * 0.05f;

                    if (point.y > center.y + MaxRise)
                    {
                        continue;
                    }

                    if (occupied != null && occupied.Any(o => Base.Utils.Maths.Utl.Equals(o, point, OccupiedTolerance)))
                    {
                        continue;
                    }

                    if (!perception.CanStandAt(point, null)
                        || !perception.HasFloorSupportAt(point)
                        || !nav.INavMesh.ValidPosition(point, nav.NavAreas, OffNavTolerance, true))
                    {
                        continue;
                    }

                    _lineCast.SetLine(center + lineLift, point + lineLift);

                    if (!_lineCast.Cast())
                    {
                        spot = point;
                        clearLine = true;
                        return true;
                    }

                    if (!foundBlocked)
                    {
                        foundBlocked = true;
                        blockedSpot = point;
                    }

                    // The first standable floor in this column is the one to use; lower ones are further away.
                    break;
                }
            }

            if (foundBlocked)
            {
                spot = blockedSpot;
                return true;
            }

            return false;
        }

        /// <summary>Falls that have started (Activate) and not yet looked for their landing spot.</summary>
        private static readonly HashSet<FallNoSupportAbility> Falling = new HashSet<FallNoSupportAbility>();

        /// <summary>
        /// Marks a real fall. The fall's coroutine looks for its landing spot in its first step, which runs inside
        /// PlayAction before the ability's playing action is stored, so IsExecuting is still false there: the rescue
        /// skipped every fall, and a soldier standing 0.6 m up on a crate the Scarab drove through died at full HP
        /// (tester 2026-11-20).
        /// </summary>
        [HarmonyPatch(typeof(FallNoSupportAbility), nameof(FallNoSupportAbility.Activate))]
        internal static class FallNoSupportAbility_Activate_Patch
        {
            private static void Prefix(FallNoSupportAbility __instance)
            {
                Falling.Add(__instance);
            }
        }

        /// <summary>
        /// Runs at the start of every fall (FallToFreePosition), and also when SpawnActorEffect places a spawned
        /// actor. Spawns are left alone: vanilla destroys a spawned actor that has no free spot, and only a real
        /// fall goes through Activate first.
        /// </summary>
        [HarmonyPatch(typeof(FallNoSupportAbility), nameof(FallNoSupportAbility.FindFallPosition))]
        internal static class FallNoSupportAbility_FindFallPosition_Patch
        {
            private static void Postfix(FallNoSupportAbility __instance, List<Vector3> occupiedPositions, ref Vector3 fallPosition, ref bool positionIsFree)
            {
                try
                {
                    if (!Falling.Remove(__instance) && !__instance.IsExecuting)
                    {
                        return;
                    }

                    TacticalActor actor = __instance.TacticalActor;

                    if (actor == null)
                    {
                        return;
                    }

                    Vector3 from = actor.Pos;
                    float perMetre = __instance.FallDamagePerUnitHeight;

                    if (positionIsFree)
                    {
                        TFTVLogger.Always($"[FallRescue] {actor.DisplayName} falls from {from} to {fallPosition}: valid landing (vanilla), drop {from.y - fallPosition.y:0.0} m, about {Mathf.Max(0f, (from.y - fallPosition.y) * perMetre):0} HP");
                        return;
                    }

                    Vector3 vanillaDestination = fallPosition;

                    if (TryFindStandableSpot(actor, from, SearchRadius, occupiedPositions, out Vector3 spot, out bool clearLine))
                    {
                        fallPosition = spot;
                        positionIsFree = true;
                        TFTVLogger.Always($"[FallRescue] {actor.DisplayName} falls from {from}: vanilla found no landing (would land at {vanillaDestination} and die); fallback {(clearLine ? "clear-line" : "blocked-line")} spot {spot}, drop {from.y - spot.y:0.0} m, about {Mathf.Max(0f, (from.y - spot.y) * perMetre):0} HP");
                    }
                    else
                    {
                        TFTVLogger.Always($"[FallRescue] {actor.DisplayName} falls from {from}: vanilla found no landing and no fallback spot within {SearchRadius} m either; landing at {vanillaDestination}, vanilla kills the unit");
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        /// <summary>
        /// After a jump, the fall check tests the floor under the grid cell the unit landed in, with 0.3 m of slack.
        /// CanJumpTo only checks that the unit can stand at the target. Requiring both means no jump can end in a fall.
        /// </summary>
        [HarmonyPatch(typeof(JetJumpAbility), "CanJumpTo")]
        internal static class JetJumpAbility_CanJumpTo_Patch
        {
            private static void Postfix(JetJumpAbility __instance, Vector3 target, ref bool __result)
            {
                try
                {
                    if (!__result)
                    {
                        return;
                    }

                    TacticalActor actor = __instance.TacticalActor;

                    if (actor == null || actor.IsMounted || actor.GetAbility<FallNoSupportAbility>() == null || actor.TacticalPerception == null)
                    {
                        return;
                    }

                    if (!actor.TacticalPerception.HasFloorSupportAt(TacticalMap.SnapXZToGrid(target)))
                    {
                        __result = false;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        /// <summary>
        /// Vanilla runs this before and after every AI unit acts. A unit 0.25 m or more off its navmesh gets
        /// Health + 1 damage, with no message. Move it to the nearest standable spot first; vanilla then finds it on
        /// the navmesh and leaves it alone. Only a unit with no spot within 4 m is still killed.
        /// </summary>
        [HarmonyPatch(typeof(TacticalFaction), "KillActorIfOffNav")]
        internal static class TacticalFaction_KillActorIfOffNav_Patch
        {
            private static void Prefix(TacticalActor actor)
            {
                try
                {
                    if (actor == null || actor.IsDead || actor is TacticalActorYuggoth || actor.IsMounted)
                    {
                        return;
                    }

                    TacticalNavigationComponent nav = actor.TacticalNav;

                    if (nav?.INavMesh == null || nav.INavMesh.ValidPosition(actor.Pos, nav.NavAreas, OffNavTolerance, true))
                    {
                        return;
                    }

                    Vector3 from = actor.Pos;

                    if (TryFindStandableSpot(actor, from, OffNavSearchRadius, null, out Vector3 spot, out bool clearLine))
                    {
                        actor.SetPosition(spot);
                        TFTVLogger.Always($"[FallRescue] {actor.DisplayName} was off the navmesh at {from}; moved to {spot} ({(clearLine ? "clear line" : "blocked line")}) instead of being killed");
                    }
                    else
                    {
                        TFTVLogger.Always($"[FallRescue] {actor.DisplayName} is off the navmesh at {from} and has no standable spot within {OffNavSearchRadius} m; vanilla kills it");
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }
    }
}
