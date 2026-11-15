using Base.AI.Defs;
using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Tactical.AI;
using PhoenixPoint.Tactical.AI.Actions;
using PhoenixPoint.Tactical.AI.Considerations;
using PhoenixPoint.Tactical.AI.TargetGenerators;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV
{
    internal partial class TFTVAICombatAbilities
    {
        /// <summary>
        /// Melee reach by the real path: whether a melee fighter can walk up to a target and still have the AP to
        /// strike. Straight lines lied: berserkers 17 m from a civilian were planned to kill it, couldn't get there,
        /// and ran into the open instead. Path lengths are in AP (TacAITarget.PathLength is subtracted from AP).
        /// </summary>
        internal static class MeleeReach
        {
            /// <summary>Distance from the target at which a strike lands.</summary>
            internal const float StrikeReach = 1.5f;

            /// <summary>Nearest targets checked by path per actor and frame; the rest are further by straight line.</summary>
            private const int MaxPathChecks = 4;

            private static GameTagDef _meleeTag;
            private static readonly Dictionary<KeyValuePair<TacticalActor, TacticalActorBase>, KeyValuePair<int, float>> PathByFrame =
                new Dictionary<KeyValuePair<TacticalActor, TacticalActorBase>, KeyValuePair<int, float>>();

            internal static bool IsMelee(Weapon weapon)
            {
                if (_meleeTag == null)
                {
                    _meleeTag = DefCache.GetDef<GameTagDef>("MeleeWeapon_TagDef");
                }

                return weapon?.WeaponDef != null && weapon.IsUsable && _meleeTag != null && weapon.WeaponDef.Tags.Contains(_meleeTag);
            }

            /// <summary>AP the walk to striking distance of the target costs, by path; infinity if there's no way.</summary>
            internal static float PathCost(TacticalActor actor, TacticalActorBase target)
            {
                if (Vector3.Distance(actor.Pos, target.Pos) <= StrikeReach)
                {
                    return 0f;
                }

                KeyValuePair<TacticalActor, TacticalActorBase> key = new KeyValuePair<TacticalActor, TacticalActorBase>(actor, target);
                int frame = Time.frameCount;

                if (PathByFrame.TryGetValue(key, out KeyValuePair<int, float> cached) && cached.Key == frame)
                {
                    return cached.Value;
                }

                float cost;

                try
                {
                    cost = AIUtil.GetPathLength(actor, actor.Pos, target.Pos, true, StrikeReach);
                }
                catch
                {
                    cost = float.PositiveInfinity;
                }

                if (cost <= 0f && Vector3.Distance(actor.Pos, target.Pos) > StrikeReach + 0.5f)
                {
                    // No path found reads as 0 in some cases: treat as out of reach.
                    cost = float.PositiveInfinity;
                }

                PathByFrame[key] = new KeyValuePair<int, float>(frame, cost);
                return cost;
            }

            /// <summary>Strikes the actor can land on the target this turn with this weapon, walking there first: 0 if out of reach.</summary>
            internal static int StrikesThisTurn(TacticalActor actor, Weapon weapon, TacticalActorBase target, float actionPoints)
            {
                float strikeCost = actor.Equipments.GetAPToUseEquipment(weapon);

                if (strikeCost <= 0f || actionPoints < strikeCost)
                {
                    return 0;
                }

                // Cheap bound first: a straight line can't be longer than the path.
                if (Vector3.Distance(actor.Pos, target.Pos) > actor.MaxMoveRange + StrikeReach + 0.5f)
                {
                    return 0;
                }

                float path = PathCost(actor, target);

                if (float.IsInfinity(path) || path > actionPoints - strikeCost)
                {
                    return 0;
                }

                return Mathf.Max(1, Mathf.FloorToInt((actionPoints - path + 0.001f) / strikeCost));
            }

            /// <summary>A revealed enemy it can walk up to and strike this turn with any melee weapon.</summary>
            internal static bool CanStrikeThisTurn(TacticalActor actor, out string why)
            {
                List<Weapon> melee = actor.Equipments.GetWeapons().Where(IsMelee).ToList();
                float ap = actor.CharacterStats.ActionPoints;

                List<TacticalActorBase> enemies = actor.TacticalFaction.Vision
                    .GetKnownActors(KnownState.Revealed, FactionRelation.Enemy, false)
                    .Where(e => e != null && e.IsAlive && e is TacticalActor)
                    .OrderBy(e => (e.Pos - actor.Pos).sqrMagnitude)
                    .Take(MaxPathChecks)
                    .ToList();

                if (melee.Count == 0 || enemies.Count == 0)
                {
                    why = melee.Count == 0 ? "no melee weapon" : "no revealed enemy";
                    return false;
                }

                foreach (TacticalActorBase enemy in enemies)
                {
                    foreach (Weapon weapon in melee)
                    {
                        if (StrikesThisTurn(actor, weapon, enemy, ap) > 0)
                        {
                            why = $"can reach {enemy.DisplayName}";
                            return true;
                        }
                    }
                }

                TacticalActorBase nearest = enemies[0];
                float path = PathCost(actor, nearest);
                why = $"nearest revealed enemy {nearest.DisplayName} {Vector3.Distance(actor.Pos, nearest.Pos):0.0}m, " +
                    $"path {(float.IsInfinity(path) ? "none" : path.ToString("0.#") + " AP")}, has {ap:0.#} AP, strike {actor.Equipments.GetAPToUseEquipment(melee[0]):0.#} AP";
                return false;
            }

            internal static void ClearTurn()
            {
                PathByFrame.Clear();
            }
        }

        /// <summary>
        /// A gunner with no shot from where it stands, and a known enemy near enough to matter, advances toward a
        /// spot from which it has a line of fire on that enemy. Tester report (2026-11-10): a Triton sniper 5 m
        /// from a soldier behind a wall spent the whole mission shuffling a tile back and forth and ending its turn
        /// with 14 of 15 AP. Its shot costs 11 AP, so no move-and-shoot was ever on offer, and Advance_Normal
        /// counts every spot within 7 m of an enemy as equally good. Done with a position score on Advance_Normal:
        /// a spot with a line of fire (Stances.HasLineOfFire, the step-out rules included) scores 1, any other
        /// <see cref="NoLineScore"/>.
        /// </summary>
        internal static class FiringLine
        {
            private const float NoLineScore = 0.35f;

            private static AIRoofAboveConsiderationDef _lineDef;

            private static readonly Dictionary<TacticalActor, KeyValuePair<int, TacticalActorBase>> TargetByFrame = new Dictionary<TacticalActor, KeyValuePair<int, TacticalActorBase>>();
            private static readonly HashSet<TacticalActor> LoggedThisTurn = new HashSet<TacticalActor>();

            internal static void CreateDefs()
            {
                try
                {
                    _lineDef = Helper.CreateDefFromClone(DefCache.GetDef<AIRoofAboveConsiderationDef>("RoofAbove_AIConsiderationDef"),
                        "7e2d41c9-a05b-4f36-8d71-c4b9e3f0a258", "TFTV_AdvanceToFiringLine_AIConsiderationDef");

                    AIActionDef advance = DefCache.GetDef<AIActionDef>("Advance_Normal_AIActionDef");
                    AITargetEvaluation positions = advance?.Evaluations?.FirstOrDefault(e =>
                        e.TargetGeneratorDef is AIActorMovementZoneTargetGeneratorDef || e.TargetGeneratorDef is AIActorRangeZoneTargetGeneratorDef);

                    if (positions == null)
                    {
                        TFTVLogger.Always("TFTV AI: no position evaluation in Advance_Normal_AIActionDef; no advance to a firing line");
                        return;
                    }

                    positions.Considerations = positions.Considerations.AddToArray(new AIAdjustedConsideration
                    {
                        Consideration = _lineDef,
                        ScoreCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f)
                    });
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            internal static bool TryScorePosition(BaseDef def, TacticalActor actor, TacAITarget target, out float score)
            {
                score = 1f;

                if (_lineDef == null || def != _lineDef || actor == null || target == null)
                {
                    return false;
                }

                TacticalActorBase enemy = BlindTarget(actor);

                if (enemy != null)
                {
                    score = Stances.HasLineOfFire(actor, target.Pos, enemy.Pos) ? 1f : NoLineScore;
                }

                if (IsTooClose(actor, target.Pos))
                {
                    score *= TooCloseScore;
                }

                return true;
            }

            /// <summary>
            /// A gunner's advance shouldn't end right next to an enemy: it gets there with its AP spent and stands in
            /// the open for the enemy's turn. Tester log (2026-11-14): The Puppeteer ran to 3 m from a Kaos Buggy with
            /// 0.3 AP left, and the buggy killed it. Advance_Normal scores every spot within 7 m of an enemy the same.
            /// </summary>
            private const float TooCloseRange = 3.5f;
            private const float TooCloseScore = 0.25f;

            /// <summary>Per shot, after armour: below this the gun barely scratches the target and isn't worth closing in for.</summary>
            private const float MinShotDamage = 10f;

            private static readonly Dictionary<TacticalActor, KeyValuePair<int, List<Vector3>>> GunnerEnemiesByFrame =
                new Dictionary<TacticalActor, KeyValuePair<int, List<Vector3>>>();

            /// <summary>A gunner (a gun it can fire) on a spot within <see cref="TooCloseRange"/> of a known enemy.</summary>
            private static bool IsTooClose(TacticalActor actor, Vector3 pos)
            {
                int frame = Time.frameCount;

                if (!GunnerEnemiesByFrame.TryGetValue(actor, out KeyValuePair<int, List<Vector3>> cached) || cached.Key != frame)
                {
                    List<Vector3> enemies = !actor.IsControlledByPlayer && actor.Equipments != null && actor.Equipments.GetWeapons().Any(SuppressAI.CanFire)
                        ? Stances.KnownEnemies(actor).Select(e => e.Pos).ToList()
                        : new List<Vector3>();

                    cached = new KeyValuePair<int, List<Vector3>>(frame, enemies);
                    GunnerEnemiesByFrame[actor] = cached;
                }

                foreach (Vector3 enemy in cached.Value)
                {
                    if ((enemy - pos).sqrMagnitude < TooCloseRange * TooCloseRange)
                    {
                        return true;
                    }
                }

                return false;
            }

            /// <summary>What one shot of the weapon does to the target's health, after its best armour.</summary>
            private static float ShotDamage(Weapon weapon, TacticalActorBase target)
            {
                DamageResult perRound = default(DamageResult);
                weapon.GetDamagePayload().CalculateDamageResultForAiTarget(target, target, ref perRound);

                DamagePayload payload = weapon.WeaponDef.DamagePayload;
                return perRound.HealthDamage * Mathf.Max(1, payload.AutoFireShotCount) * Mathf.Max(1, payload.ProjectilesPerShot);
            }

            /// <summary>
            /// The enemy a gunner with no shot from here should get a line on: the nearest one its gun can hurt. Null if
            /// it has a shot, or nobody it can hurt is near. Cached per frame.
            /// </summary>
            private static TacticalActorBase BlindTarget(TacticalActor actor)
            {
                int frame = Time.frameCount;

                if (TargetByFrame.TryGetValue(actor, out KeyValuePair<int, TacticalActorBase> cached) && cached.Key == frame)
                {
                    return cached.Value;
                }

                TacticalActorBase enemy = null;
                Weapon weapon = actor.Equipments?.SelectedWeapon;

                if (!actor.IsControlledByPlayer && weapon != null && SuppressAI.CanFire(weapon)
                    && actor.CharacterStats.ActionPoints >= actor.Equipments.GetAPToUseEquipment(weapon)
                    && !Stances.HasShotFromHere(actor, weapon))
                {
                    float reach = weapon.WeaponDef.EffectiveRange * 1.5f + actor.MaxMoveRange;

                    List<TacticalActorBase> near = Stances.KnownEnemies(actor)
                        .Where(e => Vector3.Distance(actor.Pos, e.Pos) <= reach)
                        .OrderBy(e => (e.Pos - actor.Pos).sqrMagnitude)
                        .ToList();

                    enemy = near.FirstOrDefault(e => ShotDamage(weapon, e) >= MinShotDamage);

                    if (LoggedThisTurn.Add(actor))
                    {
                        if (enemy != null)
                        {
                            TFTVLogger.Always($"TFTV AI: {actor.DisplayName} has no shot from where it stands; its advance looks for a line of fire on " +
                                $"{enemy.DisplayName} {Vector3.Distance(actor.Pos, enemy.Pos):0.0}m away");
                        }
                        else if (near.Count > 0)
                        {
                            TFTVLogger.Always($"TFTV AI: {actor.DisplayName} has no shot from where it stands, and its {weapon.WeaponDef.name} can't hurt " +
                                $"anyone near ({string.Join(", ", near.Select(e => $"{e.DisplayName} {ShotDamage(weapon, e):0}"))} per shot): no firing line sought");
                        }
                    }
                }

                TargetByFrame[actor] = new KeyValuePair<int, TacticalActorBase>(frame, enemy);
                return enemy;
            }

            internal static void ClearTurn()
            {
                TargetByFrame.Clear();
                LoggedThisTurn.Clear();
                GunnerEnemiesByFrame.Clear();
            }
        }

        /// <summary>
        /// Stalking. A melee fighter (no gun it can fire) that can't reach anyone to strike this turn closes in only
        /// by spots no known enemy can see, so it ends its turn hidden within a charge of its prey instead of in the
        /// open next to it. Done with a position score on the plain moves (Advance_Normal and Dash): a spot an
        /// enemy sees scores <see cref="SeenSpotScore"/>, a hidden one 1. Strikes are untouched: as soon as a target
        /// is in reach, MoveAndStrike takes it.
        /// </summary>
        internal static class Stalking
        {
            private const float SeenSpotScore = 0.15f;

            private static AIRoofAboveConsiderationDef _hiddenDef;

            private static readonly Dictionary<TacticalActor, KeyValuePair<int, bool>> StalkerByFrame = new Dictionary<TacticalActor, KeyValuePair<int, bool>>();
            private static readonly HashSet<TacticalActor> LoggedThisTurn = new HashSet<TacticalActor>();

            internal static void CreateDefs()
            {
                try
                {
                    _hiddenDef = Helper.CreateDefFromClone(DefCache.GetDef<AIRoofAboveConsiderationDef>("RoofAbove_AIConsiderationDef"),
                        "c3a95e1f-7b24-4d86-9f02-5e8b1a6d4c73", "TFTV_StalkHidden_AIConsiderationDef");

                    int added = 0;

                    foreach (string name in new[] { "Advance_Normal_AIActionDef", "Dash_AIActionDef" })
                    {
                        AIActionDef move = DefCache.GetDef<AIActionDef>(name);
                        AITargetEvaluation positions = move?.Evaluations?.FirstOrDefault(e =>
                            e.TargetGeneratorDef is AIActorMovementZoneTargetGeneratorDef || e.TargetGeneratorDef is AIActorRangeZoneTargetGeneratorDef);

                        if (positions == null)
                        {
                            TFTVLogger.Always($"TFTV AI: no position evaluation in {name}; no stalking on it");
                            continue;
                        }

                        positions.Considerations = positions.Considerations.AddToArray(new AIAdjustedConsideration
                        {
                            Consideration = _hiddenDef,
                            ScoreCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f)
                        });

                        added++;
                    }

                    TFTVLogger.Always($"TFTV AI: stalking added to {added} plain moves");
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            internal static bool TryScorePosition(BaseDef def, TacticalActor actor, TacAITarget target, out float score)
            {
                score = 1f;

                if (_hiddenDef == null || def != _hiddenDef || actor == null || target == null)
                {
                    return false;
                }

                if (!IsStalker(actor))
                {
                    return true;
                }

                score = Stances.KnownEnemies(actor).Any(e => Stances.CanSee(e, actor, target.Pos)) ? SeenSpotScore : 1f;
                return true;
            }

            /// <summary>Melee only, and nobody in reach this turn. Cached per frame.</summary>
            private static bool IsStalker(TacticalActor actor)
            {
                int frame = Time.frameCount;

                if (StalkerByFrame.TryGetValue(actor, out KeyValuePair<int, bool> cached) && cached.Key == frame)
                {
                    return cached.Value;
                }

                bool stalker = false;
                string why = null;

                if (!actor.IsControlledByPlayer && actor.Equipments != null
                    && !actor.Equipments.GetWeapons().Any(SuppressAI.CanFire)
                    && actor.Equipments.GetWeapons().Any(MeleeReach.IsMelee)
                    && (actor.Status == null || !actor.Status.HasStatus<PhoenixPoint.Tactical.Entities.Statuses.PanicStatus>()))
                {
                    stalker = !MeleeReach.CanStrikeThisTurn(actor, out why);
                }

                if (stalker && LoggedThisTurn.Add(actor))
                {
                    TFTVLogger.Always($"TFTV AI stalk: {actor.DisplayName} can't strike anyone this turn ({why}); it closes in by hidden spots");
                }

                StalkerByFrame[actor] = new KeyValuePair<int, bool>(frame, stalker);
                return stalker;
            }

            internal static void ClearTurn()
            {
                StalkerByFrame.Clear();
                LoggedThisTurn.Clear();
                MeleeReach.ClearTurn();
            }
        }
    }
}
