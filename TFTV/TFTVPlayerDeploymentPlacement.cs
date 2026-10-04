using Base.Entities;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Levels.ActorDeployment;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.ActorDeployment;
using PhoenixPoint.Tactical.Levels.Missions;
using PhoenixPoint.Tactical.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace TFTV
{
    /// <summary>
    /// Placement during the deployment phase (the phase itself is in TFTVPlayerDeploymentPhase.cs): with a soldier
    /// selected, clicking - or pressing select on a pad - over a green square moves them there.
    ///
    /// Valid spots are the game's own, computed the way TacParticipantSpawn.DeployForTurn does for turn 0: the
    /// non-reinforcement mission zones that GetEligibleDeployZones allows for that soldier's deploy entry (so TFTV's
    /// own culls on it apply), filtered by TacticalDeployZone.GetValidSpawnPosition for the soldier's size.
    /// That filter casts against actors, so a spot with a soldier on it never counts as free.
    ///
    /// The move is the game's console Teleport (SetPosition, OnFinishedMovingActor, OnAbilityExecuteFinished with the
    /// move ability, invalidate the situation cache, reset the view state), plus what the end of a normal move does
    /// that the console skips: re-carve the nav obstacle and raise ActorFinishedMoving.
    /// </summary>
    internal static partial class TFTVPlayerDeploymentPhase
    {
        private const float CursorSnapRange = 0.75f;
        private const float SameFloorHeight = 1.0f;
        private const float MarkerSize = 0.7f;

        private static readonly MethodInfo GetMissionSpawnZonesMethod = AccessTools.Method(typeof(TacParticipantSpawn), "GetMissionSpawnZones");
        private static readonly MethodInfo GetEligibleDeployZonesMethod = AccessTools.Method(typeof(TacParticipantSpawn), "GetEligibleDeployZones");

        private static readonly List<GameObject> _markers = new List<GameObject>();
        private static object _markersFor;
        private static bool _markersDirty;
        private static int _markersDirtyFrame;
        private static Material _markerMaterial;

        private struct DeploySpot
        {
            public TacticalDeployZone Zone;
            public Vector3 Pos;
        }

        private static float Radius(TacticalActor actor)
        {
            return actor.NavigationComponent?.AgentNavSettings.AgentRadius ?? 0.5f;
        }

        private static bool IsSquadMember(TacticalActor actor)
        {
            return actor != null && _pending != null && _pending.SquadDeployData.ContainsKey(actor);
        }

        private static List<Vector3> GetFreeDeploySpots(TacticalActor actor)
        {
            return _pending != null && _pending.SquadDeployData.TryGetValue(actor, out ActorDeployData data)
                ? GetFreeDeploySpots(data).Select(s => s.Pos).ToList()
                : new List<Vector3>();
        }

        /// <summary>
        /// Spots in the squad's deploy zones where a unit with this deploy entry could spawn right now (occupied
        /// spots excluded). Works from the entry rather than an actor, so it serves reserve units too.
        ///
        /// Zones are those the game allows the unit (GetEligibleDeployZones, with TFTV's culls), plus those it would
        /// allow the same unit at another size: zones pick their occupants by size tag rather than by room, so a
        /// vehicle may use grunt tiles and a grunt vehicle tiles wherever it physically fits. Fit is then the game's
        /// own GetValidSpawnPosition, with the unit's real size, followed by TFTV's checks: clearance from enemies
        /// already on the map and (base defense) no 3x3/5x5 unit on an upper floor. Spots inside the base map's
        /// sealed rooms never get this far: the GetOrderedSpawnPositions patch below removes them from the zones.
        ///
        /// Each zone tile costs the game a physics check, so the result is cached per unit until something on the
        /// map moves (MarkSpotsDirty). Callers only read the list.
        /// </summary>
        private static List<DeploySpot> GetFreeDeploySpots(ActorDeployData data)
        {
            if (_pending?.PlayerSpawn == null || data == null)
            {
                return new List<DeploySpot>();
            }

            if (_spotCache.TryGetValue(data, out List<DeploySpot> cached))
            {
                return cached;
            }

            List<DeploySpot> spots = ComputeFreeDeploySpots(data);

            // Not while a benched unit's GameObject is still waiting for its end-of-frame destroy: its colliders
            // would still be blocking the spots it stood on.
            if (Time.frameCount > _markersDirtyFrame)
            {
                _spotCache[data] = spots;
            }

            return spots;
        }

        private static List<DeploySpot> ComputeFreeDeploySpots(ActorDeployData data)
        {
            List<DeploySpot> spots = new List<DeploySpot>();
            HashSet<Vector3Int> taken = new HashSet<Vector3Int>();

            List<TacticalDeployZone> zones = PlayerZones();
            List<TacticalDeployZone> eligible = EligibleZones(zones, data);
            int ownZones = eligible.Count;

            foreach (ActorDeployData variant in OtherSizeVariants(data))
            {
                eligible = eligible.Union(EligibleZones(zones, variant)).ToList();
            }

            bool firstLook = _loggedSpotSummaries.Add(data);

            if (firstLook && eligible.Count > ownZones)
            {
                TFTVLogger.Always($"[DeploymentPhase] {data.ComponentSetDef?.name}: {ownZones} zone(s) for its own size, {eligible.Count - ownZones} more for another size where it may fit");
            }

            // The same clearance vanilla gives the squad from enemies already on the map. Normally none are, but
            // base defense spawns eggs, sentinels or the Scylla straight from its zone setup, before the squad.
            float clearance = PlayerClearanceRange();
            TacticalFaction phoenix = _pending.PlayerSpawn.TacticalFaction;
            List<Vector3> hostiles = clearance > 0f
                ? _pending.Controller.Map.GetActors<TacticalActorBase>()
                    .Where(a => a.IsAlive && a.TacticalFaction != null && a.TacticalFaction.GetRelationTo(phoenix) == FactionRelation.Enemy)
                    .Select(a => a.Pos).ToList()
                : new List<Vector3>();

            bool keepOnGroundFloor = IsLargeUnit(data) && IsBaseDefense();
            float groundY = keepOnGroundFloor ? GroundFloorY(zones) : 0f;
            int upstairs = 0;

            foreach (TacticalDeployZone zone in eligible)
            {
                // GetValidSpawnPosition hands back the zone's shared cache, so copy it before the next call.
                foreach (Vector3 pos in zone.GetValidSpawnPosition(data.ComponentSetDef, data.DeploymentTags, zone.GetOrderedSpawnPositions(false)).ToList())
                {
                    if (!taken.Add(SpotKey(pos)) || hostiles.Any(h => (h - pos).magnitude < clearance))
                    {
                        continue;
                    }

                    if (keepOnGroundFloor && pos.y > groundY + UpperFloorHeight)
                    {
                        upstairs++;
                        continue;
                    }

                    spots.Add(new DeploySpot { Zone = zone, Pos = pos });
                }
            }

            if (firstLook)
            {
                TFTVLogger.Always($"[DeploymentPhase] {data.ComponentSetDef?.name}: {spots.Count} free spot(s)" +
                    (keepOnGroundFloor ? $"; dropped {upstairs} above the ground floor (ground y {groundY:0.0})" : ""));
            }

            return spots;
        }

        // Free spots per unit, valid until something moves; and the base's ground floor height, per phase.
        private static readonly Dictionary<ActorDeployData, List<DeploySpot>> _spotCache = new Dictionary<ActorDeployData, List<DeploySpot>>();
        private static float? _groundFloorY;

        private static Vector3Int SpotKey(Vector3 pos)
        {
            return new Vector3Int(Mathf.RoundToInt(pos.x * 10f), Mathf.RoundToInt(pos.y * 10f), Mathf.RoundToInt(pos.z * 10f));
        }

        private static void ResetPlacementCaches()
        {
            _loggedSpotSummaries.Clear();
            _spotCache.Clear();
            _groundFloorY = null;
        }

        /// <summary>
        /// The base defense map has sealed rooms (an unbuilt section, roofed with terrain the camera cuts away)
        /// whose floors are valid navmesh inside player and enemy deploy zones, so units - vehicles included, in
        /// the vanilla initial deployment - could spawn shut in there. No general test told them apart (a closed
        /// room is not a solid, and its navmesh connects through), so the area is cut by hand, at every height:
        /// x -15.5..-0.5, z 19.5..25.5 (the in-game console's grid positions there read e.g. (-14.5, 2.4, 20.5)).
        /// Applied where the game reads a zone's spawn positions, so vanilla spawning, TFTV's and placement all
        /// skip it. Runs on every mission's spawns, so it asks "base defense?" first (cached per level) and only
        /// then looks at the positions.
        /// </summary>
        [HarmonyPatch(typeof(TacticalDeployZone), nameof(TacticalDeployZone.GetOrderedSpawnPositions))]
        internal static class TacticalDeployZone_GetOrderedSpawnPositions_BaseDefenseSealedRooms_Patch
        {
            private const float MinX = -15.5f, MaxX = -0.5f, MinZ = 19.5f, MaxZ = 25.5f, Margin = 0.01f;

            private static readonly HashSet<TacticalDeployZone> _loggedZones = new HashSet<TacticalDeployZone>();
            private static TacticalLevelController _checkedLevel;
            private static bool _checkedLevelIsBaseDefense;

            private static bool InSealedRooms(Vector3 pos)
            {
                return pos.x >= MinX - Margin && pos.x <= MaxX + Margin && pos.z >= MinZ - Margin && pos.z <= MaxZ + Margin;
            }

            public static void Postfix(TacticalDeployZone __instance, ref List<Vector3> __result)
            {
                try
                {
                    if (__result == null || __result.Count == 0 || !IsBaseDefenseLevel(__instance.TacticalLevel))
                    {
                        return;
                    }

                    if (__result.Any(InSealedRooms))
                    {
                        int before = __result.Count;

                        // A new list, so whatever the zone keeps for itself is untouched.
                        __result = __result.Where(p => !InSealedRooms(p)).ToList();

                        if (_loggedZones.Add(__instance))
                        {
                            TFTVLogger.Always($"[DeploymentPhase] base defense: cut {before - __result.Count} of {before} spawn position(s) from {__instance.name} at {__instance.Pos} (sealed rooms)");
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

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
                    _checkedLevelIsBaseDefense = missionType.Tags.Contains(TFTVMain.Shared.SharedGameTags.BaseDefenseMissionTag);
                }

                return _checkedLevelIsBaseDefense;
            }
        }

        private const float UpperFloorHeight = 2f;

        private static readonly HashSet<ActorDeployData> _loggedSpotSummaries = new HashSet<ActorDeployData>();

        // True only while placement asks the game which zones a unit may use: TFTV's base defense team rule
        // (entrance, hangar, lift) shapes the initial deployment, but during placement any player zone is fair game.
        internal static bool IgnoreBaseDefenseTeamZones;

        private static List<TacticalDeployZone> PlayerZones()
        {
            return ((IEnumerable<TacticalDeployZone>)GetMissionSpawnZonesMethod.Invoke(_pending.PlayerSpawn, null))
                .Where(z => !z.IsReinforcementZone)
                .ToList();
        }

        private static List<TacticalDeployZone> EligibleZones(IEnumerable<TacticalDeployZone> zones, ActorDeployData data)
        {
            IgnoreBaseDefenseTeamZones = true;

            try
            {
                return ((IEnumerable<TacticalDeployZone>)GetEligibleDeployZonesMethod
                    .Invoke(_pending.PlayerSpawn, new object[] { zones, data, 0, false })).ToList();
            }
            finally
            {
                IgnoreBaseDefenseTeamZones = false;
            }
        }

        private static bool IsBaseDefense()
        {
            return _pending.Controller.TacMission.MissionData.MissionType.Tags.Contains(TFTVMain.Shared.SharedGameTags.BaseDefenseMissionTag);
        }

        /// <summary>
        /// The lowest spot any player zone offers: the base's ground floor. Upper floors are anything more than
        /// UpperFloorHeight above it.
        /// </summary>
        private static float GroundFloorY(List<TacticalDeployZone> zones)
        {
            if (_groundFloorY.HasValue)
            {
                return _groundFloorY.Value;
            }

            float lowest = float.MaxValue;

            foreach (TacticalDeployZone zone in zones)
            {
                foreach (Vector3 pos in zone.GetOrderedSpawnPositions(false))
                {
                    lowest = Mathf.Min(lowest, pos.y);
                }
            }

            _groundFloorY = lowest == float.MaxValue ? 0f : lowest;
            return _groundFloorY.Value;
        }

        private static ActorDeploymentTagDef[] _largeSizeTags;
        private static ActorDeploymentTagDef[] _smallSizeTags;
        private static ActorDeploymentTagDef _vehicleSizeTag;
        private static readonly MethodInfo MemberwiseCloneMethod = AccessTools.Method(typeof(object), "MemberwiseClone");

        private static void EnsureSizeTags()
        {
            if (_largeSizeTags != null)
            {
                return;
            }

            DefCache defCache = TFTVMain.Main.DefCache;
            _largeSizeTags = new[] { "3x3_DeploymentTagDef", "5x5_DeploymentTagDef" }
                .Select(n => defCache.GetDef<ActorDeploymentTagDef>(n)).Where(t => t != null).ToArray();
            _smallSizeTags = new[] { "1x1_Grunt_DeploymentTagDef", "1x1_Elite_DeploymentTagDef" }
                .Select(n => defCache.GetDef<ActorDeploymentTagDef>(n)).Where(t => t != null).ToArray();
            _vehicleSizeTag = defCache.GetDef<ActorDeploymentTagDef>("3x3_DeploymentTagDef");
        }

        private static bool IsLargeUnit(ActorDeployData data)
        {
            EnsureSizeTags();
            return data.DeploymentTags.Any(t => _largeSizeTags.Contains(t));
        }

        /// <summary>
        /// Copies of the unit's deploy entry at the other size, for asking which zones it could use by tag: a 3x3 or
        /// 5x5 unit as a 1x1 grunt and elite, a 1x1 grunt or elite as 3x3. Everything else (identity, instance data)
        /// is the unit's own, so TFTV's per-unit zone rules still apply. DeploymentTags is ActorTags plus
        /// InstanceDef.DefaultDeploymentTags, so a copy carries the full swapped list in ActorTags and no InstanceDef.
        /// </summary>
        private static IEnumerable<ActorDeployData> OtherSizeVariants(ActorDeployData data)
        {
            List<ActorDeployData> variants = new List<ActorDeployData>();

            try
            {
                EnsureSizeTags();
                List<ActorDeploymentTagDef> tags = data.DeploymentTags.ToList();

                if (tags.Any(t => _largeSizeTags.Contains(t)) && _smallSizeTags.Length > 0)
                {
                    variants.Add(WithTags(data, tags.Where(t => !_largeSizeTags.Contains(t)).Concat(_smallSizeTags)));
                }
                else if (tags.Any(t => _smallSizeTags.Contains(t)) && _vehicleSizeTag != null)
                {
                    variants.Add(WithTags(data, tags.Where(t => !_smallSizeTags.Contains(t)).Concat(new[] { _vehicleSizeTag })));
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

            return variants;
        }

        private static ActorDeployData WithTags(ActorDeployData data, IEnumerable<ActorDeploymentTagDef> tags)
        {
            ActorDeployData copy = (ActorDeployData)MemberwiseCloneMethod.Invoke(data, null);
            copy.InstanceDef = null;
            copy.ActorTags = tags.Distinct().ToList();
            return copy;
        }

        private static bool IsNaN(Vector3 pos)
        {
            return float.IsNaN(pos.x) || float.IsNaN(pos.y) || float.IsNaN(pos.z);
        }

        /// <summary>
        /// The spot nearest <paramref name="clicked"/> on the same floor within <paramref name="range"/>, if any.
        /// </summary>
        private static bool TryNearestSpot(List<DeploySpot> spots, Vector3 clicked, float range, out DeploySpot spot)
        {
            spot = default;

            if (IsNaN(clicked))
            {
                return false;
            }

            List<DeploySpot> nearby = spots.Where(s => Mathf.Abs(s.Pos.y - clicked.y) < SameFloorHeight
                && new Vector2(s.Pos.x - clicked.x, s.Pos.z - clicked.z).magnitude <= range).ToList();

            if (nearby.Count == 0)
            {
                return false;
            }

            spot = nearby.OrderBy(s => (s.Pos - clicked).sqrMagnitude).First();
            return true;
        }

        /// <summary>
        /// A map select during the phase (left click, pad select, right-click move). Applies the armed reserve tool
        /// if there is one, otherwise moves the selected soldier. True only when vanilla should handle it, which is
        /// a plain click on a unit with nothing armed: that selects the unit.
        /// </summary>
        private static bool OnMapSelect(TacticalLevelController controller, TacticalActorBase pointedAt, Vector3 clicked)
        {
            TacticalActor pointedSquadMember = pointedAt as TacticalActor;

            if (!IsSquadMember(pointedSquadMember))
            {
                pointedSquadMember = null;
            }

            if (TryGetArmedReserve(out ActorDeployData armed))
            {
                if (pointedSquadMember != null)
                {
                    TrySwapIn(controller, armed, pointedSquadMember);
                }
                else if (pointedAt == null)
                {
                    TryBringIn(controller, armed, clicked);
                }

                return false;
            }

            if (IsBenchArmed())
            {
                if (pointedSquadMember != null)
                {
                    TryBench(controller, pointedSquadMember);
                    Disarm();
                }

                return false;
            }

            if (pointedAt != null)
            {
                return true;
            }

            TacticalActor selected = controller.View.SelectedActor;

            if (IsSquadMember(selected))
            {
                TryPlaceAt(controller, selected, clicked);
            }

            // Never fall through to vanilla on the ground: walking is not part of deployment.
            return false;
        }

        private static float PlayerClearanceRange()
        {
            try
            {
                return Traverse.Create(_pending.PlayerSpawn).Property("Rules").Field("InitialEnemyClearanceRange").GetValue<float>();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return 0f;
            }
        }

        private static TacticalActor FindBlocker(TacticalLevelController controller, TacticalActor mover, Vector3 pos)
        {
            float moverRadius = Radius(mover);

            foreach (TacticalActor other in controller.Map.GetActors<TacticalActor>())
            {
                if (other == mover || !other.IsAlive)
                {
                    continue;
                }

                Vector3 delta = other.Pos - pos;
                float horizontal = new Vector2(delta.x, delta.z).magnitude;

                if (Mathf.Abs(delta.y) < SameFloorHeight && horizontal < moverRadius + Radius(other) - 0.05f)
                {
                    return other;
                }
            }

            return null;
        }

        /// <summary>
        /// Moves the soldier to the free deploy spot nearest the clicked ground position, if one is close enough.
        /// </summary>
        private static void TryPlaceAt(TacticalLevelController controller, TacticalActor actor, Vector3 clicked)
        {
            try
            {
                if (!_pending.SquadDeployData.TryGetValue(actor, out ActorDeployData data)
                    || !TryNearestSpot(GetFreeDeploySpots(data), clicked, CursorSnapRange, out DeploySpot spot))
                {
                    return;
                }

                Vector3 target = spot.Pos;

                if (FindBlocker(controller, actor, target) != null)
                {
                    return;
                }

                Vector3 from = actor.Pos;
                MoveActor(controller, actor, target);
                FinishMoves(controller);
                TFTVLogger.Always($"[DeploymentPhase] placed {actor.DisplayName} {from} -> {actor.Pos}");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void MoveActor(TacticalLevelController controller, TacticalActor actor, Vector3 pos)
        {
            actor.SetPosition(pos);
            actor.OnFinishedMovingActor(actor);

            MoveAbility move = actor.GetAbility<MoveAbility>();

            if (move != null)
            {
                actor.OnAbilityExecuteFinished(move, new TacticalAbilityTarget(pos));
            }

            NavMeshNavigationComponent nav = actor.NavigationComponent;

            if (nav?.NavObstacle != null)
            {
                nav.NavObstacle.SetEnabled(false, false);
                nav.NavObstacle.SetEnabled(nav.ShouldCarveNavMesh, false);
            }

            controller.ActorFinishedMoving(actor);
        }

        private static MethodInfo _selectCharacterMethod;

        /// <summary>
        /// Selects a unit the way a click does, through the character-selected state's own SelectCharacter. Setting
        /// TacticalView.SelectedActor directly leaves the state drawing for the old unit, and the next ResetViewState
        /// then stops drawing for a unit that never started (an NRE in MoveAbilitySceneViewElement.StopDrawing).
        /// </summary>
        private static bool TrySelectThroughState(TacticalLevelController controller, TacticalActor actor)
        {
            try
            {
                object state = controller.View.CurrentState;

                if (state == null || state.GetType().Name != "UIStateCharacterSelected")
                {
                    return false;
                }

                if (_selectCharacterMethod == null)
                {
                    _selectCharacterMethod = AccessTools.Method(state.GetType(), "SelectCharacter", new[] { typeof(TacticalActor), typeof(bool) });
                }

                _selectCharacterMethod.Invoke(state, new object[] { actor, true });
                return controller.View.SelectedActor == actor;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return false;
            }
        }

        private static void FinishMoves(TacticalLevelController controller)
        {
            MarkSpotsDirty();
            controller.SituationCache.Invalidate();
            controller.View.ResetViewState();
        }

        /// <summary>
        /// Free spots changed. The markers are rebuilt a frame later: a benched unit's GameObject is only destroyed
        /// at the end of the frame, and until then its colliders still block the spots it stood on.
        /// </summary>
        private static void MarkSpotsDirty()
        {
            _markersDirty = true;
            _markersDirtyFrame = Time.frameCount;
            _spotCache.Clear();
        }

        /// <summary>
        /// Green squares on the free spots for whatever the next select would place: the armed reserve unit, or else
        /// the selected soldier. Rebuilt when that changes, or a frame after anything moved.
        /// </summary>
        private static void UpdateMarkers(TacticalLevelController controller)
        {
            object subject;
            ActorDeployData data = null;

            if (TryGetArmedReserve(out ActorDeployData armed))
            {
                subject = armed;
                data = armed;
            }
            else
            {
                TacticalActor selected = controller.View.SelectedActor;
                subject = selected;

                if (!IsBenchArmed() && IsSquadMember(selected))
                {
                    data = _pending.SquadDeployData[selected];
                }
            }

            bool waitingForDestroy = _markersDirty && Time.frameCount <= _markersDirtyFrame;

            if ((ReferenceEquals(subject, _markersFor) && !_markersDirty) || waitingForDestroy)
            {
                return;
            }

            ClearMarkers();
            _markersFor = subject;

            if (data == null)
            {
                return;
            }

            foreach (DeploySpot spot in GetFreeDeploySpots(data))
            {
                _markers.Add(CreateMarker(spot.Pos));
            }
        }

        private static GameObject CreateMarker(Vector3 spot)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Quad);
            marker.name = "TFTV_DeploySpotMarker";

            // No collider, so the game's cursor raycasts never hit the marker.
            Collider collider = marker.GetComponent<Collider>();

            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            marker.transform.position = spot + Vector3.up * 0.05f;
            marker.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            marker.transform.localScale = new Vector3(MarkerSize, MarkerSize, 1f);

            Renderer renderer = marker.GetComponent<Renderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            Material material = GetMarkerMaterial();

            if (material != null)
            {
                renderer.sharedMaterial = material;
            }

            return marker;
        }

        private static Material GetMarkerMaterial()
        {
            if (_markerMaterial != null)
            {
                return _markerMaterial;
            }

            foreach (string shaderName in new[] { "Sprites/Default", "UI/Default", "Unlit/Color" })
            {
                Shader shader = Shader.Find(shaderName);

                if (shader != null)
                {
                    _markerMaterial = new Material(shader) { color = new Color(0.2f, 1f, 0.3f, 0.6f) };
                    TFTVLogger.Always($"[DeploymentPhase] spot markers use shader {shaderName}");
                    break;
                }
            }

            return _markerMaterial;
        }

        /// <summary>
        /// Map selects (left click, the pad's select) during the phase go to OnMapSelect instead of walking.
        /// Clicking a unit with nothing armed keeps vanilla behaviour, which selects it. UIStateCharacterSelected is
        /// internal, hence TargetMethod.
        /// </summary>
        [HarmonyPatch]
        internal static class UIStateCharacterSelected_OnSelect_PlaceDuringDeployment_Patch
        {
            private static FieldInfo _hoverSelectionField;
            private static MethodInfo _cursorOverGuiGetter;

            private static MethodBase TargetMethod()
            {
                return AccessTools.Method("PhoenixPoint.Tactical.View.ViewStates.UIStateCharacterSelected:OnSelect");
            }

            public static bool Prefix(object __instance)
            {
                try
                {
                    TacticalLevelController controller = _pending?.Controller;

                    if (controller == null || _pending.Releasing)
                    {
                        return true;
                    }

                    if (_cursorOverGuiGetter == null)
                    {
                        _cursorOverGuiGetter = AccessTools.PropertyGetter(__instance.GetType(), "CursorOverGui");
                        _hoverSelectionField = AccessTools.Field(__instance.GetType(), "_hoverSelection");
                    }

                    if ((bool)_cursorOverGuiGetter.Invoke(__instance, null))
                    {
                        return true;
                    }

                    SelectionInfo hover = (SelectionInfo)_hoverSelectionField.GetValue(__instance);
                    return OnMapSelect(controller, hover.Actor, hover.ActionGridPos);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }
            }
        }

        /// <summary>
        /// Right-click move (and anything else that moves through MoveToGridPosition) does nothing during the phase:
        /// right-click benches instead, and walking is not part of deployment.
        /// </summary>
        [HarmonyPatch]
        internal static class UIStateCharacterSelected_MoveToGridPosition_PlaceDuringDeployment_Patch
        {
            private static MethodBase TargetMethod()
            {
                return AccessTools.Method("PhoenixPoint.Tactical.View.ViewStates.UIStateCharacterSelected:MoveToGridPosition");
            }

            public static bool Prefix(Vector3 __0)
            {
                try
                {
                    TacticalLevelController controller = _pending?.Controller;

                    if (controller == null || _pending.Releasing)
                    {
                        return true;
                    }

                    // Right-click benches during deployment (see the phase listener); never walk.
                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }
            }
        }

        /// <summary>
        /// Placement only: while the phase runs, moving, standby and every squad ability that costs AP or WP read as
        /// unavailable, so the action bar greys them out and nothing of turn 1 is spent before the enemy is on the
        /// map. Move, end turn and standby (an ApplyStatusAbility, the shared StandByAbility def) are named because none
        /// has a fixed cost. Passive abilities cost nothing and are untouched. Hot path, so it bails out first thing
        /// outside the phase.
        /// </summary>
        [HarmonyPatch(typeof(TacticalAbility), nameof(TacticalAbility.GetDisabledState))]
        internal static class TacticalAbility_GetDisabledState_PlacementOnly_Patch
        {
            public static void Postfix(TacticalAbility __instance, ref AbilityDisabledState __result)
            {
                try
                {
                    if (_pending == null || _pending.Releasing || __result != AbilityDisabledState.NotDisabled)
                    {
                        return;
                    }

                    TacticalActor actor = __instance.TacticalActor;

                    if (IsSquadMember(actor) && (__instance is MoveAbility || __instance is EndTurnAbility
                        || __instance.TacticalAbilityDef == TFTVMain.Shared.SharedGameTags.StandByAbility
                        || __instance.UsesActionPoints || __instance.WillPointCost > 0))
                    {
                        __result = AbilityDisabledState.RequirementsNotMet;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        private static void ClearMarkers()
        {
            foreach (GameObject marker in _markers)
            {
                if (marker != null)
                {
                    UnityEngine.Object.Destroy(marker);
                }
            }

            _markers.Clear();
            _markersFor = null;
            _markersDirty = false;
        }
    }
}
