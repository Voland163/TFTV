using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.ActorDeployment;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV
{
    /// <summary>
    /// Where units may stand at the start of a base defense, applied inside the game's own zone methods so that the
    /// initial (automatic) deployment, TFTV's deployment code and the deployment phase's placement all obey it:
    ///
    ///  - Sealed rooms (every spawn): unbuilt sections behind the entrance gantry whose floors are valid navmesh.
    ///  - Phase I entrance limit (player zones): not past a line just in front of the gantry.
    ///  - Ground floor (3x3 and 5x5 units in player zones): not more than 2 m above the lowest player spot.
    ///
    /// Why it is needed at all: TFTV's phase-I setup (SetPlayerSpawnEntrancePhaseI) moves four zones onto the gantry
    /// and shrinks their colliders, but the shrink never reaches the game's spawn grid, which is rebuilt from the
    /// collider's bounds every time it is read. Each zone keeps its full ~33 m length centred on the gantry: down the
    /// corridor towards the entrance on one layout, sideways through the walls on another. Those long strips are also
    /// what lets a large entrance team spawn at all, so they are trimmed here rather than "fixed".
    /// </summary>
    internal static class TFTVPlayerDeploymentBaseDefenseCulls
    {
        private static readonly HashSet<TacticalDeployZone> _loggedZones = new HashSet<TacticalDeployZone>();
        private static TacticalLevelController _checkedLevel;
        private static bool _isBaseDefense;
        private static bool _entranceLimitChecked;
        private static bool _hasEntranceLimit;
        private static Vector3 _entranceExit;
        private static float _minEntranceDistance;
        private static float? _groundFloorY;

        /// <summary>Is this a base defense level? Answered once per level; resets the per-level caches when the level changes.</summary>
        private static bool IsBaseDefenseLevel(TacticalLevelController controller)
        {
            if (!ReferenceEquals(controller, _checkedLevel))
            {
                TacMissionTypeDef missionType = controller?.TacMission?.MissionData?.MissionType;

                // Only remember a definite answer: too early in loading there is no mission to ask yet.
                if (missionType == null)
                {
                    return false;
                }

                _checkedLevel = controller;
                _isBaseDefense = missionType.Tags.Contains(TFTVMain.Shared.SharedGameTags.BaseDefenseMissionTag);
                _entranceLimitChecked = false;
                _hasEntranceLimit = false;
                _groundFloorY = null;
                _roofCache.Clear();
                _loggedZones.Clear();
            }

            return _isBaseDefense;
        }

        private static bool IsPlayerZone(TacticalDeployZone zone)
        {
            return zone.MissionParticipant == TacMissionParticipant.Player && !zone.IsReinforcementZone;
        }

        #region sealed rooms

        /// <summary>Terrain roofs lower than this are a sealed room's; the hangar's rock ceiling starts at 7.5 m.</summary>
        private const float SealedRoofTopY = 7.3f;
        /// <summary>Another storey's floor this close overhead: the lower floor of a sealed two-storey block.</summary>
        private const float StoreyAboveRange = 3f;
        /// <summary>A spot this far or less above a sealed spot (same column) stands on top of the sealed room.</summary>
        private const float StandingOnSealedRange = 3f;
        private const float RoofSearchRange = 12f;
        private const float RayStartHeight = 0.3f;

        private static readonly string[] TerrainNames = { "GEN_Terrain", "PP_Tunnel_Block", "polySurface" };

        private static readonly Dictionary<Vector3Int, string> _roofCache = new Dictionary<Vector3Int, string>();
        private static readonly RaycastHit[] _roofHits = new RaycastHit[32];

        /// <summary>
        /// Sealed rooms are told apart by what is straight above them - measured on both base layouts (2026-10-14)
        /// with a dump of every player spawn position. A sealed room is roofed low by terrain (GEN_Terrain_*,
        /// PP_Tunnel_Block_* or the polySurface* rock collision meshes, at most 7.1 m up), or sits under another
        /// storey's GEN_Floor_* less than 3 m above it. The hangar floor and the gantry have the same rock mesh only
        /// higher (7.5-9.3 m); every other player zone (hangar, lift, rooms) has no terrain above it at all. Solids,
        /// navmesh reachability and a hand-cut box were all tried before and failed. Cached per spot for the level.
        /// </summary>
        private static bool UnderSealedRoof(Vector3 pos, out string roof)
        {
            Vector3Int key = new Vector3Int(Mathf.RoundToInt(pos.x * 10f), Mathf.RoundToInt(pos.y * 10f), Mathf.RoundToInt(pos.z * 10f));

            if (!_roofCache.TryGetValue(key, out roof))
            {
                roof = FindSealedRoof(pos);
                _roofCache[key] = roof;
            }

            return roof != null;
        }

        /// <summary>
        /// Raycast straight up with back faces counted: terrain is often a one-sided mesh facing the sky, which a
        /// ray from underneath would otherwise pass straight through.
        /// </summary>
        private static int RaycastUp(Vector3 pos, float range)
        {
            bool backfaces = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;

            try
            {
                return Physics.RaycastNonAlloc(pos + Vector3.up * RayStartHeight, Vector3.up, _roofHits, range, ~0, QueryTriggerInteraction.Ignore);
            }
            finally
            {
                Physics.queriesHitBackfaces = backfaces;
            }
        }

        /// <summary>The hits above a spot, nearest first, without units: whoever stands there must not hide the roof (the answer is cached).</summary>
        private static List<RaycastHit> HitsAbove(Vector3 pos, float range)
        {
            int count = RaycastUp(pos, range);

            return _roofHits.Take(count)
                .Where(h => h.collider != null && h.collider.GetComponentInParent<TacticalActorBase>() == null)
                .OrderBy(h => h.distance)
                .ToList();
        }

        private static string FindSealedRoof(Vector3 pos)
        {
            List<RaycastHit> hits = HitsAbove(pos, RoofSearchRange);

            if (hits.Count == 0)
            {
                return null;
            }

            RaycastHit nearest = hits[0];

            if (nearest.collider.name.StartsWith("GEN_Floor") && nearest.point.y - pos.y < StoreyAboveRange)
            {
                return $"{nearest.collider.name} {nearest.point.y - pos.y:0.0} m up";
            }

            foreach (RaycastHit hit in hits)
            {
                if (hit.point.y < SealedRoofTopY && TerrainNames.Any(name => hit.collider.name.StartsWith(name)))
                {
                    return $"{hit.collider.name} at y {hit.point.y:0.0}";
                }
            }

            return null;
        }

        private static Vector2Int Column(Vector3 pos)
        {
            return new Vector2Int(Mathf.RoundToInt(pos.x * 10f), Mathf.RoundToInt(pos.z * 10f));
        }

        #endregion

        #region phase I entrance limit

        /// <summary>
        /// How far toward the entrance units may stand in phase I of a base defense against aliens, measured from
        /// where TFTV puts the entrance team (Map.EntrancePhaseIPlayerSpawn, on the gantry 19 m in from the entrance).
        /// TFTV's entrance team rule also allows zones within 20 m of the entrance itself - right for phases II and
        /// III, when the defenders spawn there, but in phase I the long entrance-side zones then reach nearly to the
        /// door. 2.5 m keeps the gantry and a few tiles in front of it (set by playtest: 6 m reached 3-4 tiles too far).
        /// </summary>
        private const float PhaseIEntranceAdvance = 2.5f;

        /// <summary>The same offset TFTVBaseDefenseTactical uses to find the phase-I spawn from the entrance.</summary>
        private static readonly Vector3 PhaseISpawnOffset = new Vector3(1.5f, 0f, 19f);

        /// <summary>
        /// Phase I (TimeLeft >= 12, as in TFTVBaseDefenseTactical) of a base defense against aliens: the entrance,
        /// found the way TFTV finds it (the PlayerExitZone collider below 4 m) but once per level here, so it is never
        /// left over from an earlier mission, and the horizontal distance from it a player spot must keep.
        /// </summary>
        private static bool TryGetPhaseIEntranceLimit(TacticalLevelController controller, out Vector3 entranceExit, out float minDistance)
        {
            if (!_entranceLimitChecked)
            {
                _entranceLimitChecked = true;
                _hasEntranceLimit = false;

                if (TFTVBaseDefenseTactical.TimeLeft >= 12 && controller.Factions.Any(f => f.TacticalFactionDef.MatchesShortName("aln")))
                {
                    BoxCollider exitZone = UnityEngine.Object.FindObjectsOfType<BoxCollider>()
                        .FirstOrDefault(b => b.name.StartsWith("PlayerExitZone") && b.bounds.center.y < 4);

                    if (exitZone != null)
                    {
                        _entranceExit = exitZone.bounds.center;
                        _minEntranceDistance = HorizontalDistance(_entranceExit + PhaseISpawnOffset, _entranceExit) - PhaseIEntranceAdvance;
                        _hasEntranceLimit = _minEntranceDistance > 0f;
                    }
                }

                TFTVLogger.Always(_hasEntranceLimit
                    ? $"[DeploymentPhase] base defense phase I: player spots must keep {_minEntranceDistance:0.0} m from the entrance at {_entranceExit}"
                    : "[DeploymentPhase] base defense: no phase-I entrance limit on this mission");
            }

            entranceExit = _entranceExit;
            minDistance = _minEntranceDistance;
            return _hasEntranceLimit;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return (a - b).magnitude;
        }

        #endregion

        #region ground floor

        private const float UpperFloorHeight = 2f;

        private static ActorDeploymentTagDef[] _largeSizeTags;

        private static bool IsLarge(IEnumerable<ActorDeploymentTagDef> tags)
        {
            if (_largeSizeTags == null)
            {
                _largeSizeTags = new[] { "3x3_DeploymentTagDef", "5x5_DeploymentTagDef" }
                    .Select(n => TFTVMain.Main.DefCache.GetDef<ActorDeploymentTagDef>(n))
                    .Where(t => t != null)
                    .ToArray();
            }

            return tags != null && tags.Any(t => _largeSizeTags.Contains(t));
        }

        /// <summary>The lowest spot any player zone offers: the base's ground floor.</summary>
        private static float GroundFloorY(TacticalLevelController controller)
        {
            if (!_groundFloorY.HasValue)
            {
                float lowest = float.MaxValue;

                foreach (TacticalDeployZone zone in controller.Map.GetActors<TacticalDeployZone>().Where(IsPlayerZone).ToList())
                {
                    foreach (Vector3 pos in zone.GetOrderedSpawnPositions(false))
                    {
                        lowest = Mathf.Min(lowest, pos.y);
                    }
                }

                _groundFloorY = lowest == float.MaxValue ? 0f : lowest;
            }

            return _groundFloorY.Value;
        }

        #endregion

        [HarmonyPatch(typeof(TacticalDeployZone), nameof(TacticalDeployZone.GetOrderedSpawnPositions))]
        internal static class TacticalDeployZone_GetOrderedSpawnPositions_BaseDefenseCulls_Patch
        {
            public static void Postfix(TacticalDeployZone __instance, ref List<Vector3> __result)
            {
                try
                {
                    if (__result == null || __result.Count == 0 || !IsBaseDefenseLevel(__instance.TacticalLevel))
                    {
                        return;
                    }

                    int before = __result.Count;

                    // Sealed rooms first, over the whole list, so a spot on top of one can be recognised.
                    HashSet<Vector3> sealedSpots = new HashSet<Vector3>();
                    Dictionary<Vector2Int, List<float>> sealedColumns = new Dictionary<Vector2Int, List<float>>();
                    string sealedExample = null;

                    foreach (Vector3 pos in __result)
                    {
                        if (UnderSealedRoof(pos, out string roof))
                        {
                            sealedSpots.Add(pos);
                            sealedExample = sealedExample ?? $"{roof} above {pos}";

                            Vector2Int column = Column(pos);

                            if (!sealedColumns.TryGetValue(column, out List<float> heights))
                            {
                                sealedColumns[column] = heights = new List<float>();
                            }

                            heights.Add(pos.y);
                        }
                    }

                    bool entranceLimit = IsPlayerZone(__instance)
                        && TryGetPhaseIEntranceLimit(__instance.TacticalLevel, out _, out _);

                    int onTop = 0;
                    int nearEntrance = 0;

                    // A new list, so whatever the zone keeps for itself is untouched.
                    List<Vector3> kept = new List<Vector3>(before);

                    foreach (Vector3 pos in __result)
                    {
                        if (sealedSpots.Contains(pos))
                        {
                            continue;
                        }

                        // Standing on the roof of a sealed room is inside the same sealed block (its upper floor's front row).
                        if (sealedColumns.TryGetValue(Column(pos), out List<float> below)
                            && below.Any(y => pos.y - y > 0.5f && pos.y - y <= StandingOnSealedRange))
                        {
                            onTop++;
                            continue;
                        }

                        if (entranceLimit && HorizontalDistance(pos, _entranceExit) < _minEntranceDistance)
                        {
                            nearEntrance++;
                            continue;
                        }

                        kept.Add(pos);
                    }

                    if (kept.Count == before)
                    {
                        return;
                    }

                    __result = kept;

                    if (_loggedZones.Add(__instance))
                    {
                        TFTVLogger.Always($"[DeploymentPhase] base defense: cut {before - kept.Count} of {before} spawn position(s) from {__instance.name} at {__instance.Pos}:" +
                            $" {sealedSpots.Count} in sealed rooms{(sealedExample != null ? $" (e.g. {sealedExample})" : "")}, {onTop} on top of them," +
                            $" {nearEntrance} too close to the entrance (phase I)");
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        /// <summary>
        /// Large units (3x3 and 5x5) in base defense player zones only on the ground floor: the zones' upper tiles are
        /// walkways and rooms a vehicle should never start in. GetValidSpawnPosition is where both the game's own
        /// spawning and placement ask for a unit's spots, with its size tags. It hands back the zone's shared cache,
        /// so a filtered copy is returned instead.
        /// </summary>
        [HarmonyPatch(typeof(TacticalDeployZone), nameof(TacticalDeployZone.GetValidSpawnPosition))]
        internal static class TacticalDeployZone_GetValidSpawnPosition_BaseDefenseGroundFloor_Patch
        {
            public static void Postfix(TacticalDeployZone __instance, IEnumerable<ActorDeploymentTagDef> tags, ref IReadOnlyCollection<Vector3> __result)
            {
                try
                {
                    if (__result == null || __result.Count == 0 || !IsBaseDefenseLevel(__instance.TacticalLevel)
                        || !IsPlayerZone(__instance) || !IsLarge(tags))
                    {
                        return;
                    }

                    float maxY = GroundFloorY(__instance.TacticalLevel) + UpperFloorHeight;

                    if (__result.All(p => p.y <= maxY))
                    {
                        return;
                    }

                    __result = __result.Where(p => p.y <= maxY).ToList();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }
    }
}
