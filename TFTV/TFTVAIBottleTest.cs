using Base.AI.Defs;
using Base.Core;
using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Tactical.AI.Actions;
using PhoenixPoint.Tactical.AI.Considerations;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.ActorDeployment;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using PhoenixPoint.Tactical.Levels.Missions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TFTV
{
    internal partial class TFTVAICombatAbilities
    {
        /// <summary>
        /// The bottle test. At the start of its turn, a Pandoran or human(oid) faction fighting the player, on a
        /// mission where it has no unlimited reinforcements, checks its nerve: with its strength (the hit points of
        /// its units) under 15% of its starting strength and under half of its enemies' current strength, it
        /// breaks, and from then on every unit that can move runs for an exit zone and leaves the map. A broken
        /// faction stays broken for the rest of the mission.
        ///
        /// Starting strength is the full hit points of every unit the faction has fielded, so reinforcements raise
        /// it as they arrive. Enemy strength counts the units of every faction at war with it that count toward the
        /// end of the mission. Units mind-controlled from another faction neither count nor run.
        ///
        /// Running is the game's own flee (AIActionMoveAndEscape to an exit zone of the unit's mission side, then the
        /// exit-mission ability), cloned with its own gate at the flee weight so it comes before anything else. The
        /// game only lets a unit leave through an exit zone of its own side (attacker or defender); when the map has
        /// none, one is added on the faction's deploy zone farthest from the player. Escaped units no longer count
        /// for the mission's end. A unit with no path to an exit fights on as usual.
        ///
        /// A unit the player must kill (a kill objective's target) never runs.
        /// </summary>
        internal static class BottleTest
        {
            private const float StrengthShareToBreak = 0.15f;
            private const float EnemyShareToBreak = 0.5f;

            internal static AIActionMoveAndEscapeDef RoutActionDef;
            private static AIHasEquipmentConsiderationDef _routingDef;
            private static ExitMissionAbilityDef _exitMissionDef;

            private static readonly string[] ExitZoneSetDefNames =
            {
                "ResidentMilitaryExitZone_ComponentSetDef",
                "IntruderExitZone_ComponentSetDef",
                "ExitZone_ComponentSetDef",
                "ResidentCivilianExitZone_ComponentSetDef",
            };

            private static readonly FieldInfo DeployZoneBoxColliderField = AccessTools.Field(typeof(TacticalDeployZone), "_boxCollider");

            private class FactionState
            {
                internal bool Eligible;
                internal float StartStrength;
                internal readonly HashSet<TacticalActor> Counted = new HashSet<TacticalActor>();
                internal bool Routing;
            }

            private static readonly Dictionary<TacticalFaction, FactionState> States = new Dictionary<TacticalFaction, FactionState>();

            /// <summary>From a save, by faction def name: start strength and whether it already broke.</summary>
            private static readonly Dictionary<string, KeyValuePair<float, bool>> Pending = new Dictionary<string, KeyValuePair<float, bool>>();

            #region defs

            internal static void CreateDefs()
            {
                try
                {
                    AIHasEquipmentConsiderationDef hasGun = DefCache.GetDef<AIHasEquipmentConsiderationDef>("HasGun_AIConsiderationDef");
                    AIActionMoveAndEscapeDef flee = DefCache.GetDef<AIActionMoveAndEscapeDef>("Flee_AIActionDef");
                    _exitMissionDef = DefCache.GetDef<ExitMissionAbilityDef>("ExitMission_AbilityDef");

                    _routingDef = Helper.CreateDefFromClone(hasGun, "9b3e6f12-48c7-4d5a-a1e0-7c25d84f3b96", "TFTV_Routing_AIConsiderationDef");

                    RoutActionDef = Helper.CreateDefFromClone(flee, "2f71c8a4-d95b-4e36-8c0f-b4a6e13d7520", "TFTV_Rout_AIActionDef");
                    RoutActionDef.Weight = flee.Weight;

                    // Only the bottle test decides: none of Flee's "can't use its weapons" style gates.
                    RoutActionDef.EarlyExitConsiderations = new AIAdjustedConsideration[]
                    {
                        new AIAdjustedConsideration { Consideration = _routingDef, ScoreCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f) }
                    };

                    int added = 0;

                    foreach (AIActionsTemplateDef template in TFTVMain.Repo.GetAllDefs<AIActionsTemplateDef>()
                        .Where(t => t.ActionDefs != null && t.ActionDefs.Any(a => a is AIActionMoveToPositionDef || a is AIActionMoveAndEscapeDef)))
                    {
                        if (!template.ActionDefs.Contains(RoutActionDef))
                        {
                            template.ActionDefs = template.ActionDefs.Append(RoutActionDef).ToArray();
                            added++;
                        }
                    }

                    TFTVLogger.Always($"TFTV AI: bottle test rout action added to {added} templates");
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            internal static bool TryScoreGate(BaseDef def, TacticalActor actor, out float score)
            {
                score = 0f;

                if (def == null || _routingDef == null || def != _routingDef)
                {
                    return false;
                }

                score = actor != null && Runs(actor) ? 1f : 0f;
                return true;
            }

            #endregion

            #region the test

            internal static void OnFactionStartTurn(TacticalFaction faction)
            {
                try
                {
                    if (faction == null || faction.IsControlledByPlayer || RoutActionDef == null)
                    {
                        return;
                    }

                    FactionState state = GetState(faction);

                    if (state == null || !state.Eligible)
                    {
                        return;
                    }

                    CountNewUnits(state, faction);

                    string name = faction.TacticalFactionDef.name;

                    if (state.Routing)
                    {
                        PrepareToRun(faction, $"{name} is still broken");
                        return;
                    }

                    float strength = Strength(faction);
                    float enemies = EnemyStrength(faction);
                    float share = state.StartStrength > 0f ? strength / state.StartStrength : 1f;
                    bool breaks = state.StartStrength > 0f && strength < StrengthShareToBreak * state.StartStrength && strength < EnemyShareToBreak * enemies;

                    TFTVLogger.Always($"[Bottle] {name} turn {faction.TurnNumber}: strength {strength:0} of {state.StartStrength:0} ({share:P0}), " +
                        $"enemies {enemies:0}; {(breaks ? "BREAKS: every unit runs for an exit" : "holds")}");

                    if (breaks)
                    {
                        state.Routing = true;
                        PrepareToRun(faction, $"{name} breaks");
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static FactionState GetState(TacticalFaction faction)
            {
                if (States.TryGetValue(faction, out FactionState state))
                {
                    return state;
                }

                state = new FactionState { Eligible = IsEligible(faction) };
                States.Add(faction, state);

                if (state.Eligible && Pending.TryGetValue(faction.TacticalFactionDef.name, out KeyValuePair<float, bool> saved))
                {
                    // Loaded mid-mission: the saved start stands, and whoever is on the map now is already in it.
                    state.StartStrength = saved.Key;
                    state.Routing = saved.Value;
                    state.Counted.UnionWith(faction.TacticalActors);
                    Pending.Remove(faction.TacticalFactionDef.name);
                }

                return state;
            }

            private static bool IsEligible(TacticalFaction faction)
            {
                TacticalLevelController level = faction.TacticalLevel;
                string name = faction.TacticalFactionDef.name;

                if (faction.Faction?.FactionDef == null || faction.Faction.FactionDef.MatchesShortName("env") || faction.Faction.FactionDef.MatchesShortName("anc"))
                {
                    return false;
                }

                if (!level.Factions.Any(f => f.IsControlledByPlayer && faction.GetRelationTo(f) == FactionRelation.Enemy))
                {
                    return false;
                }

                TacMissionTypeDef missionType = level.TacMission?.MissionData?.MissionType;
                TacMissionTypeParticipantData participant = missionType?.GetParticipantData(faction.ParticipantKind);

                if (participant != null && participant.InfiniteReinforcements)
                {
                    TFTVLogger.Always($"[Bottle] {name} has unlimited reinforcements on this mission: no bottle test");
                    return false;
                }

                TFTVLogger.Always($"[Bottle] {name} ({faction.ParticipantKind}) takes the bottle test each turn: breaks under {StrengthShareToBreak:P0} " +
                    $"of its starting strength and under {EnemyShareToBreak:P0} of its enemies'");
                return true;
            }

            /// <summary>Units the faction hasn't counted yet join its starting strength at full health.</summary>
            private static void CountNewUnits(FactionState state, TacticalFaction faction)
            {
                foreach (TacticalActor actor in faction.TacticalActors)
                {
                    if (actor == null || !actor.IsAlive || IsBorrowed(actor) || !state.Counted.Add(actor))
                    {
                        continue;
                    }

                    state.StartStrength += actor.CharacterStats.Health.Max;
                }
            }

            private static float Strength(TacticalFaction faction)
            {
                return faction.TacticalActors
                    .Where(a => a != null && a.IsAlive && !a.IsEvacuated && !IsBorrowed(a))
                    .Sum(a => (float)a.CharacterStats.Health);
            }

            private static float EnemyStrength(TacticalFaction faction)
            {
                return faction.TacticalLevel.Factions
                    .Where(f => f != faction && f.Faction?.FactionDef != null && !f.Faction.FactionDef.MatchesShortName("env") && faction.GetRelationTo(f) == FactionRelation.Enemy)
                    .SelectMany(f => f.TacticalActors)
                    .Where(a => a != null && a.IsAlive && !a.IsEvacuated && a.ConsiderForGameEnd)
                    .Sum(a => (float)a.CharacterStats.Health);
            }

            /// <summary>
            /// Mind-controlled from another faction: not this faction's to count or to send off the map. A soldier
            /// that Void Omen 32 has switched over for good has no mind control status left, so it counts (and joins
            /// the starting strength) like a reinforcement.
            /// </summary>
            private static bool IsBorrowed(TacticalActor actor)
            {
                return actor.Status != null && actor.Status.HasStatus<MindControlStatus>();
            }

            #endregion

            #region running

            private static bool Runs(TacticalActor actor)
            {
                return actor.TacticalFaction != null
                    && States.TryGetValue(actor.TacticalFaction, out FactionState state)
                    && state.Routing
                    && actor.IsAlive
                    && !actor.IsEvacuated
                    && !IsBorrowed(actor)
                    && actor.GetAbility<MoveAbility>() != null
                    && (actor.GetAbility<ExitMissionAbility>() != null || actor.GetAbility<EvacuateMountedActorsAbility>() != null)
                    && !IsObjectiveTarget(actor);
            }

            /// <summary>
            /// A unit the player has to kill for the mission (a kill objective's target) stands its ground: if it left
            /// the map, the objective would be won or lost by its escape instead of the fight.
            /// </summary>
            private static bool IsObjectiveTarget(TacticalActor actor)
            {
                TacticalLevelController level = actor.TacticalLevel;

                if (level == null)
                {
                    return false;
                }

                foreach (TacticalFaction player in level.Factions.Where(f => f.IsControlledByPlayer))
                {
                    if (player.Objectives != null && player.Objectives.OfType<KillActorFactionObjective>().Any(o => o.IsInterestedInActor(actor)))
                    {
                        return true;
                    }
                }

                return false;
            }

            /// <summary>Every unit gets the means to leave, and the faction an exit to leave by.</summary>
            private static void PrepareToRun(TacticalFaction faction, string why)
            {
                int granted = 0;

                foreach (TacticalActor actor in faction.TacticalActors.Where(a => a != null && a.IsAlive && !a.IsEvacuated && !IsBorrowed(a)))
                {
                    if (_exitMissionDef != null && actor.GetAbility<MoveAbility>() != null
                        && actor.GetAbility<ExitMissionAbility>() == null && actor.GetAbility<EvacuateMountedActorsAbility>() == null)
                    {
                        actor.AddAbility(_exitMissionDef, actor);
                        granted++;
                    }
                }

                int runners = faction.TacticalActors.Count(a => a != null && Runs(a));

                if (runners == 0)
                {
                    TFTVLogger.Always($"[Bottle] {why}: nobody runs (objective targets stand their ground, the rest can't move); {granted} given the exit ability");
                    return;
                }

                string exit = EnsureExitZone(faction);

                TFTVLogger.Always($"[Bottle] {why}: {runners} unit(s) run, {granted} given the exit ability; {exit}");
            }

            private static string EnsureExitZone(TacticalFaction faction)
            {
                TacticalLevelController level = faction.TacticalLevel;
                TacMissionParticipant side = faction.ParticipantKind;
                List<TacticalExitZone> exits = level.Map.GetActors<TacticalExitZone>().ToList();

                TacticalExitZone open = exits.FirstOrDefault(z => z != null && z.MissionParticipant == side && !z.IsLocked());

                if (open != null)
                {
                    return $"exit zone {open.name} at {open.Pos}";
                }

                ComponentSetDef setDef = ExitZoneSetDefFor(side);

                if (setDef == null)
                {
                    return $"no exit zone for {side} and no exit zone def to make one: units fight on";
                }

                List<Vector3> player = level.Factions.Where(f => f.IsControlledByPlayer)
                    .SelectMany(f => f.TacticalActors).Where(a => a != null && a.IsAlive).Select(a => a.Pos).ToList();

                List<TacticalDeployZone> zones = level.Map.GetActors<TacticalDeployZone>().Where(z => z != null).ToList();
                List<TacticalDeployZone> own = zones.Where(z => z.MissionParticipant == side).ToList();
                List<TacticalDeployZone> pool = own.Count > 0 ? own : zones.Where(z => z.MissionParticipant != TacMissionParticipant.Player).ToList();

                if (pool.Count == 0)
                {
                    return $"no exit zone for {side} and no deploy zone to put one on: units fight on";
                }

                TacticalDeployZone far = pool
                    .OrderByDescending(z => player.Count == 0 ? 0f : player.Min(p => Vector3.Distance(p, z.Pos)))
                    .First();

                // TacticalExitZoneDef.CreateInstanceData returns null (map exit zones are placed in the scene, never
                // spawned), and SpawnActor writes into whatever it gets: hand it the base actor instance data instead.
                // SpawnActor clones it, and the clone copies AdditionalGameTags without a null check (the other lists
                // are checked), so a blank one threw "Value cannot be null" (tester log 2026-11-14).
                TacActorBaseInstanceData blank = new TacActorBaseInstanceData
                {
                    AdditionalGameTags = new List<PhoenixPoint.Common.Entities.GameTags.GameTagDef>()
                };

                TacticalExitZone made = TacticalDeployZone.SpawnActor(setDef, blank, faction.TacticalFactionDef, side,
                    far.transform.position, far.transform.rotation, null) as TacticalExitZone;

                if (made == null)
                {
                    return $"could not spawn an exit zone from {setDef.name}: units fight on";
                }

                made.MissionParticipant = side;

                // Same footprint as the deploy zone it sits on.
                BoxCollider source = DeployZoneBoxColliderField?.GetValue(far) as BoxCollider ?? far.GetComponent<BoxCollider>();
                BoxCollider box = made.BoxCollider ?? made.GetComponent<BoxCollider>();

                if (source != null && box != null)
                {
                    made.transform.localScale = far.transform.lossyScale;
                    box.center = source.center;
                    box.size = source.size;
                    box.isTrigger = true;
                }

                return $"made exit zone from {setDef.name} on {far.name} at {far.Pos} (bounds {box?.bounds.size})";
            }

            private static ComponentSetDef ExitZoneSetDefFor(TacMissionParticipant side)
            {
                foreach (string name in ExitZoneSetDefNames)
                {
                    ComponentSetDef setDef = DefCache.GetDef<ComponentSetDef>(name);
                    TacticalExitZoneDef zoneDef = setDef?.Components?.OfType<TacticalExitZoneDef>().FirstOrDefault();

                    if (zoneDef != null && zoneDef.MissionParticipant == side)
                    {
                        return setDef;
                    }
                }

                return null;
            }

            #endregion

            #region save and reset

            internal static List<string> Export()
            {
                return States
                    .Where(s => s.Key != null && s.Value.Eligible)
                    .Select(s => string.Join("|", s.Key.TacticalFactionDef.name, s.Value.StartStrength.ToString(CultureInfo.InvariantCulture), s.Value.Routing ? "1" : "0"))
                    .ToList();
            }

            internal static void Import(List<string> saved)
            {
                Pending.Clear();
                States.Clear();

                foreach (string line in saved ?? new List<string>())
                {
                    string[] parts = line.Split('|');

                    if (parts.Length == 3 && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float start))
                    {
                        Pending[parts[0]] = new KeyValuePair<float, bool>(start, parts[2] == "1");
                    }
                }
            }

            internal static void ClearMission()
            {
                States.Clear();
                Pending.Clear();
            }

            #endregion
        }
    }
}
