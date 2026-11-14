using Base.AI.Defs;
using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Tactical.AI;
using PhoenixPoint.Tactical.AI.Actions;
using PhoenixPoint.Tactical.AI.Considerations;
using PhoenixPoint.Tactical.AI.TargetGenerators;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.StructuralTargets;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.ActorDeployment;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using PhoenixPoint.Tactical.Levels.Missions;
using PhoenixPoint.Tactical.Levels.Objectives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TFTV
{
    internal partial class TFTVAICombatAbilities
    {
        /// <summary>
        /// Two ways for an AI unit to wait for the player instead of walking about.
        ///
        /// Sniper nest. A unit with a gun of effective range <see cref="SniperEffectiveRange"/> or more, with no
        /// enemy it can attack this turn, may climb to a spot at least <see cref="MinNestElevation"/> above the
        /// enemies it knows of, behind cover toward them, with a firing lane their way, open to none of them, and
        /// reached without crossing an Overwatch cone. Once there it shoots from the spot (other shooting spots
        /// count for less) or watches from it, and its plain moves are off. It leaves the nest when an enemy sees it
        /// from a side with no cover, or after two of its turns with no known enemy in weapon range.
        ///
        /// Ambush. A unit nobody on the other side has spotted, that can't attack anyone this turn, near a place the
        /// player has to reach for an objective (a console, a zone to evacuate from, a unit to rescue or kill...),
        /// stays hidden: its plain moves are off, and once a turn it may shift, within half a move, to a spot no known
        /// enemy can see that covers that place. The ambush is sprung for good once the unit is spotted or an enemy
        /// is in its reach, and the unit fights as usual from then on.
        ///
        /// Both use the same gate on every plain move of the soldier, Triton, Siren and Arthron templates.
        /// </summary>
        internal static class NestAndAmbush
        {
            internal const float SniperEffectiveRange = 35f;
            private const float MinNestElevation = 2f;
            private const float FullNestElevation = 6f;
            private const float MinNestDistance = 8f;
            private const float NestZoneRange = 0.75f;
            private const float NestWeight = 50f;
            private const float MinNestCover = 0.5f;
            private const int NestIdleTurns = 2;

            /// <summary>Score of another shooting spot for a sniper in its nest, its own tile being 1.</summary>
            private const float LeaveNestShotFactor = 0.3f;

            private const float AmbushZoneRange = 0.5f;
            private const float AmbushWeight = 200f;
            private const float AmbushMaxGunReach = 20f;
            private const float AmbushMaxMeleeReach = 15f;
            private const float AmbushMoveFactor = 0.85f;

            /// <summary>Own turns an ambush is held before it is called off: hidden units must not stall the mission's end.</summary>
            private const int AmbushMaxTurns = 3;

            internal static AIActionMoveToPositionDef NestActionDef;
            internal static AIActionMoveToPositionDef AmbushActionDef;

            private static AIHasEquipmentConsiderationDef _shouldNestDef;
            private static AIHasEquipmentConsiderationDef _shouldAmbushDef;
            private static AIHasEquipmentConsiderationDef _freeToMoveDef;
            private static AIRoofAboveConsiderationDef _nestSpotDef;
            private static AIRoofAboveConsiderationDef _ambushSpotDef;
            private static AIRoofAboveConsiderationDef _shootFromNestDef;
            private static GameTagDef _meleeTag;

            private static readonly string[] AmbushTemplates =
            {
                SoldierAITemplate,
                "Fishman_AIActionsTemplateDef",
                "Siren_AIActionsTemplateDef",
                "Crabman_AIActionsTemplateDef",
                "CrabmanGunner_AIActionsTemplateDef",
            };

            private static readonly string[] NestTemplates =
            {
                SoldierAITemplate,
                "Fishman_AIActionsTemplateDef",
            };

            private class Nest
            {
                internal Vector3 Pos;
                internal int IdleTurns;
            }

            private static readonly Dictionary<TacticalActor, Nest> Nests = new Dictionary<TacticalActor, Nest>();
            private static readonly HashSet<TacticalActor> NestMovedThisTurn = new HashSet<TacticalActor>();
            private static readonly Dictionary<TacticalActor, KeyValuePair<int, bool>> NestedByFrame = new Dictionary<TacticalActor, KeyValuePair<int, bool>>();

            /// <summary>Ambusher -> the objective point it covers.</summary>
            private static readonly Dictionary<TacticalActor, Vector3> Ambushes = new Dictionary<TacticalActor, Vector3>();

            /// <summary>Ambusher -> its faction's turn number when the ambush started.</summary>
            private static readonly Dictionary<TacticalActor, int> AmbushStartTurn = new Dictionary<TacticalActor, int>();

            /// <summary>Units whose ambush is over for this mission.</summary>
            private static readonly HashSet<TacticalActor> Sprung = new HashSet<TacticalActor>();
            private static readonly HashSet<TacticalActor> AmbushConsideredThisTurn = new HashSet<TacticalActor>();
            private static readonly HashSet<TacticalActor> AmbushMovedThisTurn = new HashSet<TacticalActor>();
            private static readonly Dictionary<TacticalActor, KeyValuePair<int, bool>> CanAttackByFrame = new Dictionary<TacticalActor, KeyValuePair<int, bool>>();

            private static TacticalFaction _pointsFaction;
            private static int _pointsTurn = -1;
            private static List<KeyValuePair<Vector3, string>> _points = new List<KeyValuePair<Vector3, string>>();

            private static readonly FieldInfo InteractablesField = AccessTools.Field(typeof(ActivateConsoleFactionObjective), "_interactables");
            private static readonly MethodInfo IsConsoleActivatedMethod = AccessTools.Method(typeof(ActivateConsoleFactionObjective), "IsConsoleActvated");

            #region defs

            internal static void CreateDefs()
            {
                try
                {
                    AIHasEquipmentConsiderationDef hasGun = DefCache.GetDef<AIHasEquipmentConsiderationDef>("HasGun_AIConsiderationDef");
                    AIRoofAboveConsiderationDef roof = DefCache.GetDef<AIRoofAboveConsiderationDef>("RoofAbove_AIConsiderationDef");
                    _meleeTag = DefCache.GetDef<GameTagDef>("MeleeWeapon_TagDef");

                    _shouldNestDef = Helper.CreateDefFromClone(hasGun, "6a2f9d41-c3e8-4b57-a0d6-81e5f2c7b934", "TFTV_ShouldNest_AIConsiderationDef");
                    _shouldAmbushDef = Helper.CreateDefFromClone(hasGun, "d8b14e7a-2f65-4c09-9e3b-5a7c1d02f6e8", "TFTV_ShouldAmbush_AIConsiderationDef");
                    _freeToMoveDef = Helper.CreateDefFromClone(hasGun, "3c7e05b9-a41d-4f82-b6e9-0d2f8c5a71e3", "TFTV_FreeToMove_AIConsiderationDef");
                    _nestSpotDef = Helper.CreateDefFromClone(roof, "f4a91c63-7d28-4e0b-95c1-e6b3027d8fa5", "TFTV_NestSpot_AIConsiderationDef");
                    _ambushSpotDef = Helper.CreateDefFromClone(roof, "92e6b0d7-5c1a-4f38-8d4e-1b7a3f69c0e2", "TFTV_AmbushSpot_AIConsiderationDef");
                    _shootFromNestDef = Helper.CreateDefFromClone(roof, "0b5d83f2-e9c7-4a16-b2f0-7c48e1d5a9b6", "TFTV_ShootFromNest_AIConsiderationDef");

                    NestActionDef = CreateMove("a7c2e8f1-4b39-4d60-8e15-c9f0b3d27a64", "TFTV_SniperNest_AIActionDef",
                        "e1f7a3c9-62b8-4d05-a4e7-3b9d0c18f526", "TFTV_SniperNestZone_AITargetGeneratorDef",
                        NestZoneRange, NestWeight, _shouldNestDef, _nestSpotDef);

                    AmbushActionDef = CreateMove("5d9b3a70-e2c4-4f81-b7a6-0f3e9c52d8b1", "TFTV_Ambush_AIActionDef",
                        "8f04c6e2-1a7d-4b93-9c58-e2b6f0a3d714", "TFTV_AmbushZone_AITargetGeneratorDef",
                        AmbushZoneRange, AmbushWeight, _shouldAmbushDef, _ambushSpotDef);

                    HashSet<AIActionDef> exempt = new HashSet<AIActionDef>
                    {
                        NestActionDef, AmbushActionDef, PeekAndFallBackAI.FallBackActionDef
                    };

                    // A unit out of weapons goes for gear, and a Triton for its Mist: neither is a plain move.
                    foreach (AIActionDef def in new AIActionDef[] { Scavenging.GoForGearActionDef, MistSeeking.GoToMistActionDef })
                    {
                        if (def != null)
                        {
                            exempt.Add(def);
                        }
                    }

                    HashSet<AIActionDef> gated = new HashSet<AIActionDef>();

                    foreach (string templateName in AmbushTemplates)
                    {
                        AIActionsTemplateDef template = DefCache.GetDef<AIActionsTemplateDef>(templateName);

                        if (template == null)
                        {
                            TFTVLogger.Always($"TFTV AI: no {templateName}; no ambush for it");
                            continue;
                        }

                        foreach (AIActionMoveToPositionDef move in template.ActionDefs.OfType<AIActionMoveToPositionDef>())
                        {
                            // Fall back stays possible for an ambusher (it was seen, so the ambush is over anyway),
                            // but not for a sniper in a nest that still covers it.
                            if ((exempt.Contains(move) && move != PeekAndFallBackAI.FallBackActionDef) || !gated.Add(move))
                            {
                                continue;
                            }

                            PeekAndFallBackAI.AddEarlyExit(move, _freeToMoveDef);
                        }

                        AddActionToTemplate(templateName, AmbushActionDef);
                    }

                    foreach (string templateName in NestTemplates)
                    {
                        if (DefCache.GetDef<AIActionsTemplateDef>(templateName) != null)
                        {
                            AddActionToTemplate(templateName, NestActionDef);

                            // The watch from cover (TFTVAIStances), with stances off only for snipers in their nest.
                            if (!Stances.Enabled && Stances.WatchActionDef != null)
                            {
                                AddActionToTemplate(templateName, Stances.WatchActionDef);
                            }
                        }
                    }

                    // In the nest, the shot comes from the nest.
                    foreach (AIActionDef shot in new AIActionDef[]
                    {
                        DefCache.GetDef<AIActionMoveAndAttackDef>("MoveAndShoot_AIActionDef"),
                        DefCache.GetDef<AIActionMoveAndAttackDef>("BC_MoveAndShoot_AIActionDef"),
                        PeekAndFallBackAI.PeekActionDef,
                        SuppressAI.ActionDef
                    })
                    {
                        AITargetEvaluation positions = shot?.Evaluations.FirstOrDefault(e =>
                            e.TargetGeneratorDef is AIActorMovementZoneTargetGeneratorDef || e.TargetGeneratorDef is AIActorRangeZoneTargetGeneratorDef);

                        if (positions != null && !positions.Considerations.Any(c => c.Consideration == _shootFromNestDef))
                        {
                            positions.Considerations = positions.Considerations.AddToArray(new AIAdjustedConsideration
                            {
                                Consideration = _shootFromNestDef,
                                ScoreCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f)
                            });
                        }
                    }

                    TFTVLogger.Always($"TFTV AI: sniper nest and ambush added; {gated.Count} plain moves gated");
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static AIActionMoveToPositionDef CreateMove(string guid, string name, string zoneGuid, string zoneName,
                float zoneRange, float weight, AIConsiderationDef gate, AIConsiderationDef spot)
            {
                AIActionMoveToPositionDef move = Helper.CreateDefFromClone(
                    DefCache.GetDef<AIActionMoveToPositionDef>("MoveToSafePosition_AIActionDef"), guid, name);

                move.Weight = weight;

                RemoveConsiderations(move, typeof(AIVisibleEnemiesConsiderationDef));
                PeekAndFallBackAI.AddEarlyExit(move, gate);

                AITargetEvaluation positions = move.Evaluations[0];

                AIActorMovementZoneTargetGeneratorDef zone = Helper.CreateDefFromClone(
                    (AIActorMovementZoneTargetGeneratorDef)positions.TargetGeneratorDef, zoneGuid, zoneName);

                zone.ZoneRange = zoneRange;
                zone.IncludeSelfActorPosition = true;
                positions.TargetGeneratorDef = zone;

                List<AIAdjustedConsideration> considerations = new List<AIAdjustedConsideration>
                {
                    new AIAdjustedConsideration { Consideration = spot, ScoreCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f) }
                };

                AIAdjustedConsideration spread = positions.Considerations.FirstOrDefault(c => c.Consideration is AISpreadConsiderationDef);

                if (spread != null)
                {
                    considerations.Add(spread);
                }

                positions.Considerations = considerations.ToArray();
                return move;
            }

            #endregion

            #region gates and scores

            internal static bool TryScoreGate(BaseDef def, TacticalActor actor, out float score)
            {
                score = 0f;

                if (def == null || _freeToMoveDef == null || actor == null)
                {
                    return false;
                }

                if (def == _freeToMoveDef)
                {
                    score = IsNested(actor) || IsAmbushing(actor) ? 0f : 1f;
                    return true;
                }

                if (def == _shouldNestDef)
                {
                    score = ShouldNest(actor) ? 1f : 0f;
                    return true;
                }

                if (def == _shouldAmbushDef)
                {
                    score = IsAmbushing(actor) && !AmbushMovedThisTurn.Contains(actor) ? 1f : 0f;
                    return true;
                }

                return false;
            }

            internal static bool TryScorePosition(BaseDef def, TacticalActor actor, TacAITarget target, out float score)
            {
                score = 1f;

                if (def == null || _nestSpotDef == null || actor == null || target == null)
                {
                    return false;
                }

                if (def == _nestSpotDef)
                {
                    score = NestScore(actor, target, out _);
                    return true;
                }

                if (def == _ambushSpotDef)
                {
                    score = AmbushScore(actor, target.Pos);
                    return true;
                }

                if (def == _shootFromNestDef)
                {
                    score = !Stances.IsOwnTile(actor, target.Pos) && IsNested(actor) ? LeaveNestShotFactor : 1f;
                    return true;
                }

                return false;
            }

            #endregion

            #region sniper nest

            /// <summary>A gun with the reach of a sniper rifle, able to fire.</summary>
            internal static bool IsSniper(TacticalActor actor)
            {
                return SniperWeapon(actor) != null;
            }

            private static Weapon SniperWeapon(TacticalActor actor)
            {
                return actor?.Equipments?.GetWeapons().FirstOrDefault(w => SuppressAI.CanFire(w) && w.WeaponDef.EffectiveRange >= SniperEffectiveRange);
            }

            private static bool ShouldNest(TacticalActor actor)
            {
                return actor.IsAlive
                    && !actor.IsControlledByPlayer
                    && PeekAndFallBackAI.IsRegularSoldier(actor)
                    && !NestMovedThisTurn.Contains(actor)
                    && !IsNested(actor)
                    && !IsAmbushing(actor)
                    && IsSniper(actor)
                    && Stances.KnownEnemies(actor).Any()
                    && !CanAttack(actor);
            }

            /// <summary>In its nest, and the nest still does its job. Cached per frame: the gates ask often.</summary>
            internal static bool IsNested(TacticalActor actor)
            {
                if (actor == null || !Nests.TryGetValue(actor, out Nest nest))
                {
                    return false;
                }

                int frame = Time.frameCount;

                if (NestedByFrame.TryGetValue(actor, out KeyValuePair<int, bool> cached) && cached.Key == frame)
                {
                    return cached.Value;
                }

                string why = null;

                if (!actor.IsAlive || actor.IsDisabled)
                {
                    why = "it can't act";
                }
                else if ((actor.Pos - nest.Pos).sqrMagnitude > 0.5f)
                {
                    // Still on its way there this turn; a climb cut short ends the nest at the next turn.
                    if (NestMovedThisTurn.Contains(actor))
                    {
                        return true;
                    }

                    why = "it has left the spot";
                }
                else if (!IsSniper(actor))
                {
                    why = "its rifle can't fire";
                }
                else if (Flanker(actor, nest.Pos) is TacticalActorBase flanker)
                {
                    why = $"{flanker.DisplayName} sees it from a side with no cover";
                }

                if (why != null)
                {
                    LeaveNest(actor, why);
                }

                NestedByFrame[actor] = new KeyValuePair<int, bool>(frame, why == null);
                return why == null;
            }

            private static void LeaveNest(TacticalActor actor, string why)
            {
                if (Nests.Remove(actor))
                {
                    TFTVLogger.Always($"TFTV AI nest: {actor.DisplayName} leaves its nest: {why}");
                }
            }

            /// <summary>A known enemy that can see the spot with less than low cover in between.</summary>
            private static TacticalActorBase Flanker(TacticalActor actor, Vector3 pos)
            {
                foreach (TacticalActorBase enemy in Stances.KnownEnemies(actor))
                {
                    if (Stances.CoverToward(actor, pos, enemy.Pos) < MinNestCover && Stances.CanSee(enemy, actor, pos))
                    {
                        return enemy;
                    }
                }

                return null;
            }

            /// <summary>
            /// Elevated, behind cover toward the known enemies, a firing lane their way, open to none of them, and
            /// reached without crossing Overwatch. Cheap tests first: most spots fail on height.
            /// </summary>
            private static float NestScore(TacticalActor actor, TacAITarget target, out string why)
            {
                Vector3 pos = target.Pos;
                List<TacticalActorBase> enemies = Stances.KnownEnemies(actor).ToList();
                Weapon weapon = SniperWeapon(actor);

                if (enemies.Count == 0 || weapon == null)
                {
                    why = "no known enemy or no rifle";
                    return 0f;
                }

                Vector3 centre = Vector3.zero;

                foreach (TacticalActorBase enemy in enemies)
                {
                    centre += enemy.Pos;
                }

                centre /= enemies.Count;

                float elevation = pos.y - centre.y;

                if (elevation < MinNestElevation)
                {
                    why = $"only {elevation:0.0}m up";
                    return 0f;
                }

                float distance = Stances.FlatDistance(pos, centre);

                if (distance < MinNestDistance || distance > weapon.GetDamagePayload().Range)
                {
                    why = $"{distance:0}m from the enemies";
                    return 0f;
                }

                float cover = Stances.CoverToward(actor, pos, centre);

                if (cover < MinNestCover)
                {
                    why = $"cover {cover:0.00} toward the enemies";
                    return 0f;
                }

                if (Stances.CrossesOverwatch(actor, pos))
                {
                    why = "the way crosses Overwatch";
                    return 0f;
                }

                if (Flanker(actor, pos) is TacticalActorBase flanker)
                {
                    why = $"open to {flanker.DisplayName}";
                    return 0f;
                }

                float post = Stances.FiringPost(actor, pos, centre, out string kind);

                if (post <= 0f)
                {
                    why = $"no firing lane ({kind})";
                    return 0f;
                }

                // Leave the AP to watch or shoot from there.
                OverwatchAbility overwatch = actor.GetAbility<OverwatchAbility>(weapon);
                float needed = overwatch != null ? overwatch.ActionPointCost : actor.Equipments.GetAPToUseEquipment(weapon);
                float apFactor = actor.CharacterStats.ActionPoints - target.PathLength >= needed ? 1f : 0.6f;

                float height = Mathf.Lerp(0.6f, 1f, Mathf.InverseLerp(MinNestElevation, FullNestElevation, elevation));

                why = $"{elevation:0.0}m up, {distance:0}m from the enemies, cover {cover:0.00}, post {post:0.00} ({kind}), AP factor {apFactor:0.0}";
                return height * (0.5f + 0.5f * cover) * post * apFactor;
            }

            internal static void OnFactionStartTurn(TacticalFaction faction)
            {
                try
                {
                    if (faction == null || faction.IsControlledByPlayer)
                    {
                        return;
                    }

                    foreach (TacticalActor actor in Nests.Keys.Where(a => a == null || a.TacticalFaction == faction || !a.IsAlive).ToList())
                    {
                        if (actor == null || !actor.IsAlive)
                        {
                            Nests.Remove(actor);
                            continue;
                        }

                        Nest nest = Nests[actor];
                        Weapon weapon = SniperWeapon(actor);
                        float range = weapon != null ? weapon.GetDamagePayload().Range : 0f;
                        bool enemyInRange = Stances.KnownEnemies(actor).Any(e => Vector3.Distance(e.Pos, nest.Pos) <= range);

                        nest.IdleTurns = enemyInRange ? 0 : nest.IdleTurns + 1;

                        if (nest.IdleTurns >= NestIdleTurns)
                        {
                            LeaveNest(actor, $"no known enemy within {range:0}m for {nest.IdleTurns} turns");
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            #endregion

            #region ambush

            /// <summary>Holding an ambush: decided at its first look each turn, sprung for good when seen or in reach of an enemy.</summary>
            internal static bool IsAmbushing(TacticalActor actor)
            {
                if (actor == null || AmbushActionDef == null || Sprung.Contains(actor))
                {
                    return false;
                }

                if (AmbushConsideredThisTurn.Add(actor) && !Ambushes.ContainsKey(actor))
                {
                    TryStartAmbush(actor);
                }

                if (!Ambushes.TryGetValue(actor, out Vector3 point))
                {
                    return false;
                }

                string why = null;

                if (!actor.IsAlive || actor.IsDisabled)
                {
                    why = "it can't act";
                }
                else if (SpottedBy(actor) is TacticalFaction spotter)
                {
                    why = $"{spotter.TacticalFactionDef.name} has spotted it";
                }
                else if (CanAttack(actor))
                {
                    why = "an enemy is in its reach";
                }
                else if (AmbushStartTurn.TryGetValue(actor, out int started) && actor.TacticalFaction.TurnNumber - started >= AmbushMaxTurns)
                {
                    why = $"it has waited {actor.TacticalFaction.TurnNumber - started} turns";
                }
                else if (!OthersStillFighting(actor))
                {
                    why = "nobody else on its side is still fighting";
                }

                if (why != null)
                {
                    Ambushes.Remove(actor);
                    AmbushStartTurn.Remove(actor);
                    Sprung.Add(actor);
                    TFTVLogger.Always($"TFTV AI ambush: {actor.DisplayName} springs its ambush at {point}: {why}");
                    return false;
                }

                return true;
            }

            private static void TryStartAmbush(TacticalActor actor)
            {
                if (!actor.IsAlive || actor.IsDisabled || actor.IsControlledByPlayer
                    || actor.ActionsTemplateDef?.ActionDefs == null || !actor.ActionsTemplateDef.ActionDefs.Contains(AmbushActionDef)
                    || (actor.Status != null && (actor.Status.HasStatus<PanicStatus>() || actor.Status.HasStatus<MindControlStatus>()))
                    || !HasAttackWeapon(actor)
                    || !AtWarWithPlayer(actor.TacticalFaction)
                    || IsAttackingABase(actor.TacticalFaction)
                    || SpottedBy(actor) != null
                    || CanAttack(actor)
                    || !OthersStillFighting(actor))
                {
                    return;
                }

                float reach = AmbushReach(actor);
                float search = reach + actor.MaxMoveRange * AmbushZoneRange;
                KeyValuePair<Vector3, string> best = default(KeyValuePair<Vector3, string>);
                float bestDistance = float.MaxValue;

                foreach (KeyValuePair<Vector3, string> point in ObjectivePoints(actor.TacticalFaction))
                {
                    float distance = Stances.FlatDistance(actor.Pos, point.Key);

                    if (distance <= search && distance < bestDistance)
                    {
                        best = point;
                        bestDistance = distance;
                    }
                }

                if (best.Value == null)
                {
                    return;
                }

                Ambushes[actor] = best.Key;
                AmbushStartTurn[actor] = actor.TacticalFaction.TurnNumber;
                TFTVLogger.Always($"TFTV AI ambush: {actor.DisplayName} lies in wait {bestDistance:0.0}m from {best.Value} at {best.Key} " +
                    $"(reach {reach:0}m): unseen, nobody in reach");
            }

            /// <summary>Only factions the player fights lie in wait for it: haven defenders on the player's side don't.</summary>
            private static bool AtWarWithPlayer(TacticalFaction faction)
            {
                return faction?.TacticalLevel?.Factions != null
                    && faction.TacticalLevel.Factions.Any(f => f.IsControlledByPlayer && faction.GetRelationTo(f) == FactionRelation.Enemy);
            }

            /// <summary>
            /// The attackers of a haven or Phoenix base defence come to take the place: they don't hide in it. Tester
            /// report (2026-11-10): Pandorans sat out haven defences near the willpower zones and the mission
            /// wouldn't end.
            /// </summary>
            private static bool IsAttackingABase(TacticalFaction faction)
            {
                if (faction == null || faction.ParticipantKind != TacMissionParticipant.Intruder)
                {
                    return false;
                }

                TacMissionTypeDef missionType = faction.TacticalLevel?.TacMission?.MissionData?.MissionType;

                return IsMissionOfType(missionType, "MissionTypeHavenDefense_MissionTagDef")
                    || IsMissionOfType(missionType, "MissionTypePhoenixBaseDefence_MissionTagDef");
            }

            private static bool IsMissionOfType(TacMissionTypeDef missionType, string tagName)
            {
                MissionTagDef tag = DefCache.GetDef<MissionTagDef>(tagName);

                return missionType != null && tag != null
                    && (missionType.MissionTypeTag == tag || (missionType.MissionTags != null && missionType.MissionTags.Contains(tag)));
            }

            /// <summary>
            /// Another unit of its side that can move is alive and not lying in wait itself. Ambushers are never the
            /// last of their side: the player would be left hunting for them to end the mission.
            /// </summary>
            private static bool OthersStillFighting(TacticalActor actor)
            {
                return actor.TacticalFaction.TacticalActors.Any(a => a != null && a != actor && a.IsAlive && !a.IsEvacuated
                    && !Ambushes.ContainsKey(a) && a.GetAbility<MoveAbility>() != null
                    && (a.Status == null || !a.Status.HasStatus<MindControlStatus>()));
            }

            /// <summary>Only the player's sight counts: what allied civilians see isn't shown to the player.</summary>
            private static TacticalFaction SpottedBy(TacticalActor actor)
            {
                TacticalLevelController level = actor.TacticalLevel;

                return level?.Factions?.FirstOrDefault(f => f != actor.TacticalFaction && f.IsControlledByPlayer
                    && actor.TacticalFaction.GetRelationTo(f) == FactionRelation.Enemy
                    && f.Vision.IsRevealed(actor));
            }

            private static bool HasAttackWeapon(TacticalActor actor)
            {
                return actor.Equipments.GetWeapons().Any(w => SuppressAI.CanFire(w) || IsUsableMelee(w));
            }

            private static bool IsUsableMelee(Weapon weapon)
            {
                return weapon?.WeaponDef != null && weapon.IsUsable && _meleeTag != null && weapon.WeaponDef.Tags.Contains(_meleeTag);
            }

            /// <summary>How far from the objective point the unit can still hit what arrives there.</summary>
            private static float AmbushReach(TacticalActor actor)
            {
                float reach = 0f;

                foreach (Weapon weapon in actor.Equipments.GetWeapons())
                {
                    if (SuppressAI.CanFire(weapon))
                    {
                        reach = Mathf.Max(reach, Mathf.Min(weapon.WeaponDef.EffectiveRange, AmbushMaxGunReach));
                    }
                    else if (IsUsableMelee(weapon))
                    {
                        reach = Mathf.Max(reach, Mathf.Min(actor.MaxMoveRange * 0.8f + 1.5f, AmbushMaxMeleeReach));
                    }
                }

                return reach;
            }

            /// <summary>
            /// A revealed enemy it could attack this turn: a shot from where it stands, or a gun's effective range
            /// (three quarters of it, plus half a move) or a melee charge away. Cached per frame.
            /// </summary>
            private static bool CanAttack(TacticalActor actor)
            {
                int frame = Time.frameCount;

                if (CanAttackByFrame.TryGetValue(actor, out KeyValuePair<int, bool> cached) && cached.Key == frame)
                {
                    return cached.Value;
                }

                List<TacticalActorBase> revealed = actor.TacticalFaction.Vision
                    .GetKnownActors(KnownState.Revealed, FactionRelation.Enemy, false)
                    .Where(e => e != null && e.IsAlive && e is TacticalActor)
                    .ToList();

                bool can = false;

                if (revealed.Count > 0)
                {
                    foreach (Weapon weapon in actor.Equipments.GetWeapons())
                    {
                        float reach;

                        if (SuppressAI.CanFire(weapon))
                        {
                            if (Stances.ShotTargetsFromHere(actor, weapon).Any())
                            {
                                can = true;
                                break;
                            }

                            reach = weapon.WeaponDef.EffectiveRange * 0.75f + actor.MaxMoveRange * 0.5f;
                        }
                        else if (IsUsableMelee(weapon))
                        {
                            reach = actor.MaxMoveRange * 0.8f + 1.5f;
                        }
                        else
                        {
                            continue;
                        }

                        if (revealed.Any(e => Vector3.Distance(actor.Pos, e.Pos) <= reach))
                        {
                            can = true;
                            break;
                        }
                    }
                }

                CanAttackByFrame[actor] = new KeyValuePair<int, bool>(frame, can);
                return can;
            }

            /// <summary>Hidden from every known enemy, near the objective point, with a line to it for a gun; staying put breaks ties.</summary>
            private static float AmbushScore(TacticalActor actor, Vector3 pos)
            {
                if (!Ambushes.TryGetValue(actor, out Vector3 point) || Stances.CrossesOverwatch(actor, pos))
                {
                    return 0f;
                }

                float reach = AmbushReach(actor);
                float distance = Stances.FlatDistance(pos, point);
                float near = distance <= reach ? 1f : Mathf.Clamp01(1f - (distance - reach) / 8f);

                if (near <= 0f)
                {
                    return 0f;
                }

                float lane = 1f;

                if (actor.Equipments.GetWeapons().Any(SuppressAI.CanFire) && distance > 2f)
                {
                    lane = Stances.LaneScore(actor.TacticalPerception.GetVisionPointAt(pos), point, Mathf.Max(1f, distance - 1.5f)) > 0f ? 1f : 0.5f;
                }

                if (Stances.KnownEnemies(actor).Any(e => Stances.CanSee(e, actor, pos)))
                {
                    return 0f;
                }

                return near * lane * (Stances.IsOwnTile(actor, pos) ? 1f : AmbushMoveFactor);
            }

            /// <summary>
            /// Where the player has to go for its objectives still in progress, worked out once per AI faction turn:
            /// open exit zones of its side for an evacuation, consoles not yet activated, and the
            /// units or structures any other objective is about (to rescue, kill or destroy; not the ones to protect),
            /// and unopened loot crates.
            /// </summary>
            private static List<KeyValuePair<Vector3, string>> ObjectivePoints(TacticalFaction faction)
            {
                if (faction == _pointsFaction && faction.TurnNumber == _pointsTurn)
                {
                    return _points;
                }

                _pointsFaction = faction;
                _pointsTurn = faction.TurnNumber;
                _points = new List<KeyValuePair<Vector3, string>>();

                TacticalLevelController level = faction.TacticalLevel;

                if (level == null)
                {
                    return _points;
                }

                List<TacticalActorBase> mapActors = null;

                foreach (TacticalFaction player in level.Factions.Where(f => f.IsControlledByPlayer && faction.GetRelationTo(f) == FactionRelation.Enemy))
                {
                    foreach (FactionObjective objective in player.Objectives)
                    {
                        try
                        {
                            if (objective == null || objective.State != FactionObjectiveState.InProgress
                                || objective is WipeEnemyFactionObjective || objective is KeepSoldiersAliveFactionObjective
                                || objective is SurviveTurnsFactionObjective || objective is WinBeforeTurnFactionObjective
                                || objective is MinContributionObjective
                                // The units the player must protect are the AI's targets, not a place the player has to go.
                                || objective is ProtectActorFactionObjective)
                            {
                                continue;
                            }

                            string name = objective.GetType().Name.Replace("FactionObjective", "").Replace("Objective", "");

                            if (objective is EvacuateFactionObjective)
                            {
                                foreach (TacticalExitZone zone in level.Map.GetActors<TacticalExitZone>())
                                {
                                    if (zone != null && zone.MissionParticipant == player.ParticipantKind && !zone.IsLocked())
                                    {
                                        _points.Add(new KeyValuePair<Vector3, string>(zone.transform.position, name + " zone"));
                                    }
                                }

                                continue;
                            }

                            if (objective is ActivateConsoleFactionObjective consoles && InteractablesField != null)
                            {
                                if (InteractablesField.GetValue(consoles) is List<StructuralTarget> interactables)
                                {
                                    foreach (StructuralTarget console in interactables)
                                    {
                                        if (console != null && (IsConsoleActivatedMethod == null || !(bool)IsConsoleActivatedMethod.Invoke(consoles, new object[] { console })))
                                        {
                                            _points.Add(new KeyValuePair<Vector3, string>(console.Pos, "console"));
                                        }
                                    }
                                }

                                continue;
                            }

                            if (mapActors == null)
                            {
                                mapActors = level.Map.GetActors<TacticalActorBase>()
                                    .Where(a => a != null && a.IsAlive && a.TacticalFaction != player && !(a is TacticalExitZone) && !(a is TacticalDeployZone))
                                    .ToList();
                            }

                            foreach (TacticalActorBase target in mapActors)
                            {
                                if (objective.IsInterestedInActor(target))
                                {
                                    _points.Add(new KeyValuePair<Vector3, string>(target.Pos, $"{name} {target.DisplayName}"));
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Always($"TFTV AI ambush: couldn't read objective {objective?.GetType().Name}: {e.Message}");
                        }
                    }
                }

                // Loot crates (scavenging, ambushes, haven defences under the hostile defenders omen): the player
                // walks up to them to open them.
                foreach (CrateItemContainer crate in level.Map.GetActors<CrateItemContainer>())
                {
                    CrateComponent lid = crate?.GetComponent<CrateComponent>();

                    if (crate != null && lid != null && !lid.IsOpen() && crate.Inventory?.Items != null && crate.Inventory.Items.Count > 0)
                    {
                        _points.Add(new KeyValuePair<Vector3, string>(crate.Pos, "crate"));
                    }
                }

                // Map zone objectives (TacticalObjective) are left out: in havens they are the optional willpower
                // pickups (BoostWill_ZoneObjectiveDef), not somewhere the player has to go.

                TFTVLogger.Always($"TFTV AI ambush: {faction.TacticalFactionDef.name} turn {faction.TurnNumber}: places the player has to reach: " +
                    (_points.Count == 0 ? "none" : string.Join("; ", _points.Select(p => $"{p.Value} {p.Key}"))));

                return _points;
            }

            #endregion

            #region bookkeeping

            /// <summary>Called from StepOutOfCoverFix's move prefix: both moves go to spots nobody sees on purpose.</summary>
            internal static bool RecordMove(AIActionDef actionDef, TacticalActor actor, TacAITarget target)
            {
                if (actionDef == null || actor == null || target == null || (actionDef != NestActionDef && actionDef != AmbushActionDef))
                {
                    return false;
                }

                float moved = Vector3.Distance(actor.Pos, target.Pos);

                if (actionDef == NestActionDef)
                {
                    NestMovedThisTurn.Add(actor);
                    float score = NestScore(actor, target, out string why);
                    Nests[actor] = new Nest { Pos = target.Pos };
                    NestedByFrame.Remove(actor);

                    TFTVLogger.Always($"TFTV AI nest: {actor.DisplayName} {(moved < 0.5f ? "takes its spot as a nest" : $"climbs {moved:0.0}m to a nest")} " +
                        $"at {target.Pos}: {why}, score {score:0.00}");
                }
                else
                {
                    AmbushMovedThisTurn.Add(actor);
                    Ambushes.TryGetValue(actor, out Vector3 point);

                    TFTVLogger.Always($"TFTV AI ambush: {actor.DisplayName} {(moved < 0.5f ? "stays hidden where it is" : $"moves {moved:0.0}m to a hidden spot")} " +
                        $"{Stances.FlatDistance(target.Pos, point):0.0}m from {point}");
                }

                return true;
            }

            /// <summary>Called at the start of every faction's turn.</summary>
            internal static void ClearTurn()
            {
                NestMovedThisTurn.Clear();
                NestedByFrame.Clear();
                AmbushConsideredThisTurn.Clear();
                AmbushMovedThisTurn.Clear();
                CanAttackByFrame.Clear();
            }

            /// <summary>Called on every level change and load. Nests and ambushes are taken up again from the spot after a load.</summary>
            internal static void ClearMission()
            {
                Nests.Clear();
                Ambushes.Clear();
                AmbushStartTurn.Clear();
                Sprung.Clear();
                _pointsFaction = null;
                _pointsTurn = -1;
                _points = new List<KeyValuePair<Vector3, string>>();
                ClearTurn();
            }

            #endregion
        }
    }
}
