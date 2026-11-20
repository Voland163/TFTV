using Base.AI.Defs;
using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Tactical.AI;
using PhoenixPoint.Tactical.AI.Actions;
using PhoenixPoint.Tactical.AI.Considerations;
using PhoenixPoint.Tactical.AI.TargetGenerators;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.ActorDeployment;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV
{
    internal partial class TFTVAICombatAbilities
    {
        /// <summary>
        /// Rescuees defend. On a Rescue Soldiers mission the soldiers to rescue are AI units of their own faction until a
        /// Phoenix unit reaches them. Fighting like any squad, they advanced 10-15 m into the open towards the raiders and
        /// were cut down (tester report 2026-11-18); held in place, they stood idle on a roof with no shot (2026-11-19).
        ///
        /// Now a rescuee never advances or dashes, and never ends a move nearer the nearest known enemy than where it began
        /// its turn (<see cref="CloseInMargin"/>) or further than <see cref="DefendRadius"/> from it. With an enemy known and
        /// no shot from where it stands, it takes a defensive position (<see cref="TakePositionActionDef"/>): within half a
        /// move, a spot behind cover with a line of fire toward the nearest enemy (Stances.FiringPost). Its shots and
        /// Overwatch move it only to such a spot. With no shot to take, it sets the watch from cover (Stances' watch:
        /// Overwatch toward the nearest known enemy, seen or not). Once Phoenix takes it over it is the player's to move.
        ///
        /// After stepping out to shoot it goes back (<see cref="ReturnActionDef"/>). The soldiers' fall back only takes spots
        /// no enemy can see, so a sniper whose own spot was in view stayed where it had stepped out to, in the open
        /// (2026-11-19). The return takes the spot it began its turn on, or a better covered one in reach, seen or not;
        /// a fall back out of sight, when there is one, still comes first.
        ///
        /// Haven defenders with a gun follow the same rules (2026-11-19): NJ defenders stood where they spawned in the
        /// open on turn 1 and fell back to spots with no cover. With no enemy known yet, cover is measured toward the
        /// enemy sides' deploy zones, so defenders take cover on turn 1; every defender falls back only behind low cover
        /// or better.
        ///
        /// Haven defenders differ from rescuees in three ways (tester 2026-11-19, they "did nothing"): when no known enemy
        /// is within their guns' effective range they close in, cover to cover, up to <see cref="CloseInRadius"/> a turn;
        /// a shot from a spot with little cover counts x<see cref="DefenderOpenShotFactor"/>, so it beats the watch; and
        /// they move to a spot with no line of fire only to get out of the open.
        /// </summary>
        internal static class Rescuees
        {
            private const float DefendRadius = 8f;
            private const float CloseInMargin = 1.5f;

            /// <summary>Like Stances' hold: above Overwatch and the watch, below any shot.</summary>
            private const float TakePositionWeight = 20f;
            private const float TakePositionZoneRange = 0.5f;

            /// <summary>A spot to move to must be at least this good a firing post: a step-out from high cover or a lane over low cover.</summary>
            private const float MinPost = 0.85f;

            /// <summary>Every spot but the current one counts this much less, so a rescuee doesn't shuffle between equal spots.</summary>
            private const float MoveAwayFactor = 0.9f;

            /// <summary>The current spot never scores below this, so staying is always an option.</summary>
            private const float StayFloor = 0.2f;

            /// <summary>A spot with cover but no line of fire counts this share of its cover, below any good firing post.</summary>
            private const float CoverOnlyShare = 0.6f;

            /// <summary>Cover toward the enemy a spot needs to be worth moving to without a line of fire: low cover or better.</summary>
            private const float MinCoverToMoveTo = 0.5f;

            /// <summary>Below the fall back (1500), so a spot out of sight wins when there is one.</summary>
            private const float ReturnWeight = 1000f;

            /// <summary>Another spot than the starting one must have this much more cover toward the enemy than where it stands.</summary>
            private const float ReturnCoverGain = 0.2f;

            internal static AIActionMoveToPositionDef TakePositionActionDef;
            internal static AIActionMoveToPositionDef ReturnActionDef;

            private static AIHasEquipmentConsiderationDef _shouldReturnDef;
            private static AIRoofAboveConsiderationDef _returnSpotDef;
            private static readonly HashSet<TacticalActor> ReturnedThisTurn = new HashSet<TacticalActor>();

            private static AIHasEquipmentConsiderationDef _notAwaitingRescueDef;
            private static AIHasEquipmentConsiderationDef _shouldTakePositionDef;
            private static AIRoofAboveConsiderationDef _tetherDef;
            private static AIRoofAboveConsiderationDef _defensiveSpotDef;
            private static AIRoofAboveConsiderationDef _shotSpotDef;
            private static AIRoofAboveConsiderationDef _peekFromOpenDef;

            /// <summary>
            /// A shot taken from a spot in the open (from where it stands, or stepping out from it and back) counts this much,
            /// so a move to a covered firing post and the shot from there come first when one is in reach. A sniper rescuee
            /// started in the open, stepped out to shoot twice and went back to the same spot, and two berserkers cut it
            /// down (2026-11-19).
            /// </summary>
            private const float OpenShotFactor = 0.3f;

            /// <summary>
            /// Haven defenders' shots from a spot with little cover count this much: a New Jericho sniper on a roof set
            /// Overwatch (3.53) over shooting a Triton 14 m away (3.21 after x0.3), tester 2026-11-19. A covered post
            /// still comes first, but a shot always beats the watch.
            /// </summary>
            private const float DefenderOpenShotFactor = 0.8f;

            /// <summary>
            /// Haven defenders whose guns can't reach any known enemy move up, cover to cover, this far a turn. Under the
            /// rescuees' rules Anu defenders 25-36 m from the Purists never moved closer and spent the whole mission ending
            /// turns or watching with a shotgun (tester 2026-11-19).
            /// </summary>
            private const float CloseInRadius = 12f;

            /// <summary>Closing in: how much a spot's cover adds to the ground it gains.</summary>
            private const float CloseInCoverShare = 0.6f;

            /// <summary>Fallback effective range for a gun whose def gives none.</summary>
            private const float DefaultEffectiveRange = 20f;

            private static readonly Dictionary<KeyValuePair<TacticalActor, Vector3>, KeyValuePair<int, bool>> ExposedByFrame =
                new Dictionary<KeyValuePair<TacticalActor, Vector3>, KeyValuePair<int, bool>>();

            private static readonly Dictionary<TacticalActor, KeyValuePair<int, bool>> DefenderByFrame = new Dictionary<TacticalActor, KeyValuePair<int, bool>>();

            /// <summary>With no enemy known, cover is measured toward this many of the nearest enemy deploy zones.</summary>
            private const int ThreatZonesCounted = 3;

            private static readonly Dictionary<TacticalFaction, KeyValuePair<int, List<Vector3>>> ThreatZonesByFaction =
                new Dictionary<TacticalFaction, KeyValuePair<int, List<Vector3>>>();

            private static AIRoofAboveConsiderationDef _fallBackCoverDef;

            /// <summary>Where each rescuee began its current turn, by turn number.</summary>
            private static readonly Dictionary<TacticalActor, KeyValuePair<int, Vector3>> TurnStart = new Dictionary<TacticalActor, KeyValuePair<int, Vector3>>();

            private static readonly HashSet<TacticalActor> TookPositionThisTurn = new HashSet<TacticalActor>();
            private static readonly HashSet<TacticalActor> LoggedThisTurn = new HashSet<TacticalActor>();

            #region defs

            internal static void CreateDefs()
            {
                try
                {
                    AIHasEquipmentConsiderationDef hasGun = DefCache.GetDef<AIHasEquipmentConsiderationDef>("HasGun_AIConsiderationDef");
                    AIRoofAboveConsiderationDef roof = DefCache.GetDef<AIRoofAboveConsiderationDef>("RoofAbove_AIConsiderationDef");

                    _notAwaitingRescueDef = Helper.CreateDefFromClone(hasGun, "5e2c8b71-9d34-4f60-a1b7-3c8e06d4f219", "TFTV_NotAwaitingRescue_AIConsiderationDef");
                    _shouldTakePositionDef = Helper.CreateDefFromClone(hasGun, "d7a41e9c-2b58-4f03-9c6e-81f5a2d0b374", "TFTV_RescueeShouldTakePosition_AIConsiderationDef");
                    _tetherDef = Helper.CreateDefFromClone(roof, "b81f4a26-3c7e-4d95-8e02-6a9d17c5e348", "TFTV_RescueeTether_AIConsiderationDef");
                    _defensiveSpotDef = Helper.CreateDefFromClone(roof, "3f9b6c12-e84d-4a71-b5d0-9c27e1f6a843", "TFTV_RescueeDefensiveSpot_AIConsiderationDef");
                    _shotSpotDef = Helper.CreateDefFromClone(roof, "a62e0d85-7c13-4b9f-8e46-f1b3d9c27a05", "TFTV_RescueeShotSpot_AIConsiderationDef");
                    _shouldReturnDef = Helper.CreateDefFromClone(hasGun, "4d8e2a19-6f73-4c05-b1e8-a93c07d5f162", "TFTV_RescueeShouldReturn_AIConsiderationDef");
                    _returnSpotDef = Helper.CreateDefFromClone(roof, "e05c7b43-9a21-4f68-8d3e-1b6f4c92a7d8", "TFTV_RescueeReturnSpot_AIConsiderationDef");
                    _peekFromOpenDef = Helper.CreateDefFromClone(roof, "c39e7a15-8b02-4d64-a1f7-5e0d92b6c813", "TFTV_RescueePeekFromOpen_AIConsiderationDef");
                    _fallBackCoverDef = Helper.CreateDefFromClone(roof, "8a3d5f20-6c41-4e97-b2d8-1f7e9c04a356", "TFTV_DefenderFallBackCover_AIConsiderationDef");

                    foreach (string name in new[] { "Advance_Normal_AIActionDef", "Dash_AIActionDef" })
                    {
                        AIActionDef move = DefCache.GetDef<AIActionDef>(name);

                        if (move != null && (move.EarlyExitConsiderations == null || !move.EarlyExitConsiderations.Any(c => c.Consideration == _notAwaitingRescueDef)))
                        {
                            PeekAndFallBackAI.AddEarlyExit(move, _notAwaitingRescueDef);
                        }
                    }

                    CreateTakePosition();
                    CreateReturn();

                    // A shot or Overwatch that moves first ends on a covered firing post, or doesn't move. From a spot in
                    // the open, moving to such a post to shoot comes first, and so does it over stepping out and back.
                    foreach (string name in new[] { "MoveAndShoot_AIActionDef", "Overwatch_AIActionDef" })
                    {
                        AppendPositionConsideration(DefCache.GetDef<AIActionDef>(name), _shotSpotDef);
                    }

                    AppendPositionConsideration(PeekAndFallBackAI.PeekActionDef, _peekFromOpenDef);
                    AppendPositionConsideration(PeekAndFallBackAI.FallBackActionDef, _fallBackCoverDef);

                    int tethered = 0;

                    foreach (AIActionDef action in TFTVMain.Repo.GetAllDefs<AIActionDef>())
                    {
                        if (action?.Evaluations == null)
                        {
                            continue;
                        }

                        foreach (AITargetEvaluation evaluation in action.Evaluations.Where(IsPositionEvaluation))
                        {
                            if (evaluation.Considerations == null || evaluation.Considerations.Any(c => c.Consideration == _tetherDef))
                            {
                                continue;
                            }

                            evaluation.Considerations = evaluation.Considerations.Append(Linear(_tetherDef)).ToArray();
                            tethered++;
                        }
                    }

                    TFTVLogger.Always($"TFTV AI: rescuees and haven defenders with guns defend - no Advance/Dash, take position added to {SoldierAITemplate}, tether added to {tethered} move evaluations");
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CreateTakePosition()
            {
                TakePositionActionDef = Helper.CreateDefFromClone(
                    DefCache.GetDef<AIActionMoveToPositionDef>("MoveToSafePosition_AIActionDef"),
                    "6c0e8f37-4a92-4d1b-b7e5-2f8a9d13c60e",
                    "TFTV_RescueeTakePosition_AIActionDef");

                TakePositionActionDef.Weight = TakePositionWeight;

                RemoveConsiderations(TakePositionActionDef, typeof(AIVisibleEnemiesConsiderationDef));
                PeekAndFallBackAI.AddEarlyExit(TakePositionActionDef, _shouldTakePositionDef);

                AITargetEvaluation positions = TakePositionActionDef.Evaluations[0];

                AIActorMovementZoneTargetGeneratorDef zone = Helper.CreateDefFromClone(
                    (AIActorMovementZoneTargetGeneratorDef)positions.TargetGeneratorDef,
                    "9e4b2d70-15c8-4f3a-a6d9-c0e7b81f5a24",
                    "TFTV_RescueeTakePositionZone_AITargetGeneratorDef");

                zone.ZoneRange = TakePositionZoneRange;
                zone.IncludeSelfActorPosition = true;
                positions.TargetGeneratorDef = zone;

                List<AIAdjustedConsideration> considerations = new List<AIAdjustedConsideration> { Linear(_defensiveSpotDef) };
                AIAdjustedConsideration spread = positions.Considerations.FirstOrDefault(c => c.Consideration is AISpreadConsiderationDef);

                if (spread != null)
                {
                    considerations.Add(spread);
                }

                positions.Considerations = considerations.ToArray();

                AddActionToTemplate(SoldierAITemplate, TakePositionActionDef);
            }

            private static void CreateReturn()
            {
                ReturnActionDef = Helper.CreateDefFromClone(
                    DefCache.GetDef<AIActionMoveToPositionDef>("MoveToSafePosition_AIActionDef"),
                    "7b3f9e05-c1d4-4a82-96e7-0f5d2b8c4a61",
                    "TFTV_RescueeReturn_AIActionDef");

                ReturnActionDef.Weight = ReturnWeight;

                RemoveConsiderations(ReturnActionDef, typeof(AIVisibleEnemiesConsiderationDef));
                PeekAndFallBackAI.AddEarlyExit(ReturnActionDef, _shouldReturnDef);

                AITargetEvaluation positions = ReturnActionDef.Evaluations[0];

                AIActorMovementZoneTargetGeneratorDef zone = Helper.CreateDefFromClone(
                    (AIActorMovementZoneTargetGeneratorDef)positions.TargetGeneratorDef,
                    "2c6a0f84-b937-4e15-a7d2-58e1c3f09b76",
                    "TFTV_RescueeReturnZone_AITargetGeneratorDef");

                zone.IncludeSelfActorPosition = false;
                positions.TargetGeneratorDef = zone;
                positions.Considerations = new AIAdjustedConsideration[] { Linear(_returnSpotDef) };

                AddActionToTemplate(SoldierAITemplate, ReturnActionDef);
            }

            private static bool IsPositionEvaluation(AITargetEvaluation evaluation)
            {
                return evaluation.TargetGeneratorDef is AIActorMovementZoneTargetGeneratorDef || evaluation.TargetGeneratorDef is AIActorRangeZoneTargetGeneratorDef;
            }

            private static AIAdjustedConsideration Linear(AIConsiderationDef def)
            {
                return new AIAdjustedConsideration { Consideration = def, ScoreCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f) };
            }

            private static void AppendPositionConsideration(AIActionDef action, AIConsiderationDef def)
            {
                AITargetEvaluation positions = action?.Evaluations?.FirstOrDefault(IsPositionEvaluation);

                if (positions == null || positions.Considerations.Any(c => c.Consideration == def))
                {
                    return;
                }

                positions.Considerations = positions.Considerations.Append(Linear(def)).ToArray();
            }

            #endregion

            #region scoring

            internal static bool TryScoreGate(BaseDef def, TacticalActor actor, out float score)
            {
                score = 0f;

                if (def == null || _notAwaitingRescueDef == null || (def != _notAwaitingRescueDef && def != _shouldTakePositionDef && def != _shouldReturnDef))
                {
                    return false;
                }

                try
                {
                    if (def == _notAwaitingRescueDef)
                    {
                        score = actor != null && IsDefending(actor) ? 0f : 1f;
                    }
                    else if (def == _shouldReturnDef)
                    {
                        score = actor != null && ShouldReturn(actor) ? 1f : 0f;
                    }
                    else
                    {
                        score = actor != null && CanTakePosition(actor) ? 1f : 0f;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    score = def == _notAwaitingRescueDef ? 1f : 0f;
                }

                return true;
            }

            internal static bool TryScorePosition(BaseDef def, TacticalActor actor, TacAITarget target, out float score)
            {
                score = 1f;

                if (def == null || _tetherDef == null
                    || (def != _tetherDef && def != _defensiveSpotDef && def != _shotSpotDef && def != _returnSpotDef && def != _peekFromOpenDef && def != _fallBackCoverDef))
                {
                    return false;
                }

                // An error here would stop the AI's turn (the scoring patch rethrows), so it counts as no preference.
                try
                {
                    if (actor == null || target == null || !IsDefending(actor))
                    {
                        return true;
                    }

                    if (def == _tetherDef)
                    {
                        score = WithinTether(actor, target.Pos) ? 1f : 0f;
                    }
                    else if (def == _defensiveSpotDef)
                    {
                        score = DefensiveSpot(actor, target.Pos);
                    }
                    else if (def == _returnSpotDef)
                    {
                        score = ReturnSpot(actor, target.Pos);
                    }
                    else if (def == _fallBackCoverDef)
                    {
                        // Behind real cover, not just out of sight: NJ haven defenders fell back to spots with cover 0.00-0.05 (2026-11-18).
                        score = DefenderCover(actor, target.Pos) >= MinCoverToMoveTo ? 1f : 0f;
                    }
                    else if (def == _peekFromOpenDef)
                    {
                        score = IsExposed(actor, Anchor(actor)) ? OpenShotFactorFor(actor) : 1f;
                    }
                    else if (Stances.IsOwnTile(actor, target.Pos))
                    {
                        score = IsExposed(actor, actor.Pos) ? OpenShotFactorFor(actor) : 1f;
                    }
                    else
                    {
                        score = Post(actor, target.Pos) >= MinPost ? 1f : 0f;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    score = 1f;
                }

                return true;
            }

            /// <summary>
            /// Within reach of where the turn began, and no nearer the nearest known enemy than that - except a haven
            /// defender whose guns reach no known enemy, which may close in up to <see cref="CloseInRadius"/>.
            /// </summary>
            private static bool WithinTether(TacticalActor actor, Vector3 pos)
            {
                Vector3 anchor = Anchor(actor);
                bool closingIn = IsClosingIn(actor);

                if (Stances.FlatDistance(anchor, pos) > (closingIn ? CloseInRadius : DefendRadius))
                {
                    return false;
                }

                if (closingIn)
                {
                    return true;
                }

                TacticalActorBase enemy = NearestFromAnchor(actor);

                return enemy == null || Stances.FlatDistance(pos, enemy.Pos) >= Stances.FlatDistance(anchor, enemy.Pos) - CloseInMargin;
            }

            private static TacticalActorBase NearestFromAnchor(TacticalActor actor)
            {
                Vector3 anchor = Anchor(actor);
                return Stances.KnownEnemies(actor).OrderBy(e => (e.Pos - anchor).sqrMagnitude).FirstOrDefault();
            }

            private static float OpenShotFactorFor(TacticalActor actor)
            {
                return IsHavenDefender(actor) ? DefenderOpenShotFactor : OpenShotFactor;
            }

            /// <summary>A defender that is one of the haven's residents (not a soldier awaiting rescue).</summary>
            private static bool IsHavenDefender(TacticalActor actor)
            {
                return IsDefending(actor) && !AlertState.IsAwaitingRescue(actor.TacticalFaction);
            }

            /// <summary>Longest effective range among the guns it can fire.</summary>
            private static float EffectiveRange(TacticalActor actor)
            {
                float best = 0f;

                foreach (Weapon weapon in actor.Equipments?.GetWeapons() ?? Enumerable.Empty<Weapon>())
                {
                    if (weapon?.WeaponDef != null && SuppressAI.CanFire(weapon))
                    {
                        float range = weapon.WeaponDef.EffectiveRange;
                        best = Mathf.Max(best, range > 0f && !float.IsInfinity(range) ? range : DefaultEffectiveRange);
                    }
                }

                return best > 0f ? best : DefaultEffectiveRange;
            }

            private static readonly Dictionary<TacticalActor, KeyValuePair<int, bool>> ClosingInByFrame = new Dictionary<TacticalActor, KeyValuePair<int, bool>>();
            private static readonly HashSet<TacticalActor> ClosingInLogged = new HashSet<TacticalActor>();

            /// <summary>A haven defender with a known enemy, none of them within its guns' effective range of where its turn began. Cached per frame.</summary>
            private static bool IsClosingIn(TacticalActor actor)
            {
                int frame = Time.frameCount;

                if (ClosingInByFrame.TryGetValue(actor, out KeyValuePair<int, bool> cached) && cached.Key == frame)
                {
                    return cached.Value;
                }

                bool closing = false;

                if (IsHavenDefender(actor))
                {
                    TacticalActorBase enemy = NearestFromAnchor(actor);

                    if (enemy != null)
                    {
                        float distance = Stances.FlatDistance(Anchor(actor), enemy.Pos);
                        float range = EffectiveRange(actor);
                        closing = distance > range;

                        if (closing && ClosingInLogged.Add(actor))
                        {
                            TFTVLogger.Always($"TFTV AI defender: {actor.DisplayName} (haven defender) closes in: nearest known enemy {enemy.DisplayName} " +
                                $"{distance:0.0}m away, beyond its {range:0}m range; moves up to {CloseInRadius:0}m, cover to cover");
                        }
                    }
                }

                ClosingInByFrame[actor] = new KeyValuePair<int, bool>(frame, closing);
                return closing;
            }

            /// <summary>
            /// Closing in: the ground a spot gains toward the nearest enemy, weighted by its cover; a firing post within
            /// range of that enemy counts in full.
            /// </summary>
            private static float CloseInSpot(TacticalActor actor, Vector3 pos)
            {
                TacticalActorBase enemy = NearestFromAnchor(actor);

                if (enemy == null)
                {
                    return 0f;
                }

                float before = Stances.FlatDistance(Anchor(actor), enemy.Pos);
                float after = Stances.FlatDistance(pos, enemy.Pos);
                float gain = Mathf.Clamp01((before - after) / CloseInRadius);

                if (gain <= 0f)
                {
                    return 0f;
                }

                float cover = DefenderCover(actor, pos);
                float score = gain * ((1f - CloseInCoverShare) + CloseInCoverShare * cover);

                if (after <= EffectiveRange(actor))
                {
                    score = Mathf.Max(score, Post(actor, pos));
                }

                return score * MoveAwayFactor;
            }

            /// <summary>
            /// A good firing post scores most. With none in reach, a spot behind real cover toward the enemy still beats
            /// standing in the open: a heavy whose shot cost most of its AP stood exposed and blind for a whole mission
            /// (2026-11-19). The current spot scores the better of the two, never under <see cref="StayFloor"/>.
            /// </summary>
            private static float DefensiveSpot(TacticalActor actor, Vector3 pos)
            {
                bool closingIn = IsClosingIn(actor);

                if (Stances.IsOwnTile(actor, pos))
                {
                    // Closing in, staying put is the last choice.
                    return closingIn ? StayFloor : Mathf.Max(StayFloor, Mathf.Max(Post(actor, pos), CoverOnlyShare * DefenderCover(actor, pos)));
                }

                if (Stances.CrossesOverwatch(actor, pos))
                {
                    return 0f;
                }

                if (closingIn)
                {
                    return CloseInSpot(actor, pos);
                }

                float post = Post(actor, pos);

                if (post >= MinPost)
                {
                    return post * MoveAwayFactor;
                }

                // A spot with cover but no line of fire: for a haven defender, only to get out of the open. Defenders
                // moved between blind spots behind high cover ("post 0.00") and never fired (tester 2026-11-19).
                if (IsHavenDefender(actor) && !IsExposed(actor, actor.Pos))
                {
                    return 0f;
                }

                float cover = DefenderCover(actor, pos);
                return cover >= MinCoverToMoveTo ? CoverOnlyShare * cover * MoveAwayFactor : 0f;
            }

            private static bool IsStartSpot(TacticalActor actor, Vector3 pos)
            {
                Vector3 anchor = Anchor(actor);
                return Stances.FlatDistance(pos, anchor) <= 0.5f && Mathf.Abs(pos.y - anchor.y) < 1f;
            }

            /// <summary>
            /// Less than low cover toward the known enemies, and not a firing post toward the nearest one. A post over low
            /// cover toward the nearest enemy averaged only 0.10 cover over the three nearest, and the rescuee that had just
            /// moved there was scored as standing in the open: its shots counted x0.3 and it set Overwatch instead
            /// (2026-11-19). Cached per frame and spot.
            /// </summary>
            private static bool IsExposed(TacticalActor actor, Vector3 pos)
            {
                KeyValuePair<TacticalActor, Vector3> key = new KeyValuePair<TacticalActor, Vector3>(actor, pos);
                int frame = Time.frameCount;

                if (!ExposedByFrame.TryGetValue(key, out KeyValuePair<int, bool> cached) || cached.Key != frame)
                {
                    if (ExposedByFrame.Count > 256)
                    {
                        ExposedByFrame.Clear();
                    }

                    cached = new KeyValuePair<int, bool>(frame, DefenderCover(actor, pos) < MinCoverToMoveTo && Post(actor, pos) < MinPost);
                    ExposedByFrame[key] = cached;
                }

                return cached.Value;
            }

            /// <summary>
            /// 1 on the spot the turn began on, unless it is worse covered than where it stands now (it moved to a post on
            /// purpose); another spot only if clearly better covered than where it stands now.
            /// </summary>
            private static float ReturnSpot(TacticalActor actor, Vector3 pos)
            {
                if (IsStartSpot(actor, pos) && DefenderCover(actor, pos) >= DefenderCover(actor, actor.Pos) - 0.01f)
                {
                    return 1f;
                }

                if (Stances.CrossesOverwatch(actor, pos))
                {
                    return 0f;
                }

                float there = DefenderCover(actor, pos);
                return there >= DefenderCover(actor, actor.Pos) + ReturnCoverGain ? 0.8f * there : 0f;
            }

            private static float Post(TacticalActor actor, Vector3 pos)
            {
                return Stances.FiringPostTowardNearest(actor, pos, out _);
            }

            #endregion

            #region state

            /// <summary>
            /// A unit of the faction Phoenix has come to rescue that Phoenix doesn't control yet, or a haven defender with a
            /// gun (melee defenders keep charging in: Anu berserkers did the haven's best work with hammers, 2026-11-18).
            /// Cached per frame.
            /// </summary>
            internal static bool IsDefending(TacticalActor actor)
            {
                int frame = Time.frameCount;

                if (DefenderByFrame.TryGetValue(actor, out KeyValuePair<int, bool> cached) && cached.Key == frame)
                {
                    return cached.Value;
                }

                bool defending = false;
                string kind = null;
                TacticalFaction faction = actor.TacticalFaction;

                if (faction != null && !faction.IsControlledByPlayer && actor.IsAlive
                    && (actor.Status == null || !actor.Status.HasStatus<MindControlStatus>()))
                {
                    if (AlertState.IsAwaitingRescue(faction))
                    {
                        defending = true;
                        kind = "rescuee";
                    }
                    else if (AlertState.IsHavenDefenders(faction) && actor.Equipments != null && actor.Equipments.GetWeapons().Any(SuppressAI.CanFire))
                    {
                        defending = true;
                        kind = "haven defender";
                    }
                }

                DefenderByFrame[actor] = new KeyValuePair<int, bool>(frame, defending);

                if (defending && LoggedThisTurn.Add(actor))
                {
                    TFTVLogger.Always($"TFTV AI defender: {actor.DisplayName} ({kind}) defends: no advance, moves within {DefendRadius:0}m of {Anchor(actor)} and no nearer the enemy");
                }

                return defending;
            }

            /// <summary>
            /// Cover toward the known enemies; with none known yet, toward the nearest deploy zones of the sides at war with
            /// it, so a defender gets behind something before the attackers show up. Haven defenders stood where they spawned,
            /// in the open, on turn 1 (2026-11-18).
            /// </summary>
            private static float DefenderCover(TacticalActor actor, Vector3 pos)
            {
                if (Stances.KnownEnemies(actor).Any())
                {
                    return Stances.CoverAt(actor, pos);
                }

                List<Vector3> zones = ThreatZones(actor);

                if (zones.Count == 0)
                {
                    return Stances.CoverAt(actor, pos);
                }

                return zones.OrderBy(z => (z - pos).sqrMagnitude).Take(ThreatZonesCounted).Average(z => Stances.CoverToward(actor, pos, z));
            }

            /// <summary>Deploy zones of the participants at war with the actor's side. Worked out once per side and turn.</summary>
            private static List<Vector3> ThreatZones(TacticalActor actor)
            {
                TacticalFaction faction = actor.TacticalFaction;
                int turn = faction.TurnNumber;

                if (ThreatZonesByFaction.TryGetValue(faction, out KeyValuePair<int, List<Vector3>> cached) && cached.Key == turn)
                {
                    return cached.Value;
                }

                List<Vector3> zones = new List<Vector3>();

                try
                {
                    HashSet<TacMissionParticipant> enemies = new HashSet<TacMissionParticipant>(faction.TacticalLevel.Factions
                        .Where(f => f != faction && f.Faction?.FactionDef != null && !f.Faction.FactionDef.MatchesShortName("env")
                            && faction.GetRelationTo(f) == FactionRelation.Enemy)
                        .Select(f => f.ParticipantKind));

                    zones = actor.Map.GetActors<TacticalDeployZone>()
                        .Where(z => z != null && enemies.Contains(z.MissionParticipant))
                        .Select(z => z.Pos)
                        .ToList();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

                ThreatZonesByFaction[faction] = new KeyValuePair<int, List<Vector3>>(turn, zones);
                return zones;
            }

            /// <summary>An enemy known (or its deploy zones), no position taken yet this turn, and no shot from where it stands.</summary>
            private static bool CanTakePosition(TacticalActor actor)
            {
                // With no enemy known yet it may still take cover toward where the attackers will come from.
                if (TookPositionThisTurn.Contains(actor) || !IsDefending(actor) || (!Stances.KnownEnemies(actor).Any() && ThreatZones(actor).Count == 0))
                {
                    return false;
                }

                // Closing in, a long shot it has from here (a shotgun at 30 m) doesn't hold it back.
                if (IsClosingIn(actor))
                {
                    return true;
                }

                Weapon weapon = actor.Equipments?.SelectedWeapon;
                return weapon == null || !Stances.HasShotFromHere(actor, weapon);
            }

            /// <summary>
            /// Shot after stepping off its starting spot, not yet back or fallen back out of sight, and not standing on a
            /// covered spot at least as good as the start (a move to a post to shoot from stays there).
            /// </summary>
            private static bool ShouldReturn(TacticalActor actor)
            {
                if (ReturnedThisTurn.Contains(actor) || !IsDefending(actor) || !PeekAndFallBackAI.HasShotThisTurn(actor)
                    || PeekAndFallBackAI.HasFallenBackThisTurn(actor) || IsStartSpot(actor, actor.Pos))
                {
                    return false;
                }

                if (IsExposed(actor, actor.Pos))
                {
                    return true;
                }

                // On a firing post it stays; otherwise it goes back if the start was better covered.
                return Post(actor, actor.Pos) < MinPost && DefenderCover(actor, actor.Pos) < DefenderCover(actor, Anchor(actor)) - 0.01f;
            }

            /// <summary>The watch from cover (Stances): a rescuee with no shot sets it once it has taken its position.</summary>
            internal static bool ShouldWatch(TacticalActor actor)
            {
                return IsDefending(actor) && !CanTakePosition(actor) && !Stances.ShouldShootFirst(actor);
            }

            internal static bool RecordMove(AIActionDef actionDef, TacticalActor actor, TacAITarget target)
            {
                if (ReturnActionDef != null && actionDef == ReturnActionDef)
                {
                    ReturnedThisTurn.Add(actor);
                    PeekAndFallBackAI.MarkFellBack(actor);
                    TFTVLogger.Always($"TFTV AI defender: {actor.DisplayName} goes back after its shot, moving {Vector3.Distance(actor.Pos, target.Pos):0.0}m to {target.Pos} " +
                        $"({(IsStartSpot(actor, target.Pos) ? "its starting spot" : "better cover")}), cover {DefenderCover(actor, actor.Pos):0.00} -> {DefenderCover(actor, target.Pos):0.00}");
                    return true;
                }

                if (TakePositionActionDef == null || actionDef != TakePositionActionDef)
                {
                    return false;
                }

                TookPositionThisTurn.Add(actor);
                TFTVLogger.Always($"TFTV AI defender: {actor.DisplayName} takes position, moving {Vector3.Distance(actor.Pos, target.Pos):0.0}m to {target.Pos}, " +
                    $"post {Stances.FiringPostTowardNearest(actor, target.Pos, out string kind):0.00} ({kind}), cover {DefenderCover(actor, target.Pos):0.00}");

                return true;
            }

            private static Vector3 Anchor(TacticalActor actor)
            {
                int turn = actor.TacticalFaction.TurnNumber;

                if (!TurnStart.TryGetValue(actor, out KeyValuePair<int, Vector3> start) || start.Key != turn)
                {
                    start = new KeyValuePair<int, Vector3>(turn, actor.Pos);
                    TurnStart[actor] = start;
                }

                return start.Value;
            }

            // Rescuees start every mission alerted: AlertState (TFTVAIAlert.cs).

            internal static void ClearTurn()
            {
                DefenderByFrame.Clear();
                ThreatZonesByFaction.Clear();
                LoggedThisTurn.Clear();
                TookPositionThisTurn.Clear();
                ReturnedThisTurn.Clear();
                ClosingInByFrame.Clear();
                ClosingInLogged.Clear();
            }

            #endregion
        }
    }
}
