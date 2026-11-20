using Base.AI;
using Base.AI.Defs;
using Base.Core;
using Base.Defs;
using Base.Levels;
using HarmonyLib;
using PhoenixPoint.Tactical.AI;
using PhoenixPoint.Tactical.AI.Actions;
using PhoenixPoint.Tactical.AI.Considerations;
using PhoenixPoint.Tactical.AI.TargetGenerators;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV
{
    /// <summary>
    /// A Mindfragger's leap that gets cut short by a newly revealed soldier carries on.
    ///
    /// The stock AIActionMoveAndMindControl moves to the spot next to its target, then mind controls from there. Any
    /// AI move stops on the spot when a soldier it didn't know about comes into view (StopReason EnemySeen), and the
    /// action then gives up: "Can't execute Facehugger_MindControl_AbilityDef. Invalid position". The AI picks again,
    /// and since every mind control scores the same flat 50, it usually picks another soldier and another path.
    /// Against stealthed soldiers each move revealed one more and stopped again: one Mindfragger tried Sammer, then
    /// Tatiana (stopped at once by the next reveal), then Sophia, whose path ran into overwatch (playtest 2026-11-19).
    ///
    /// Here, when the move stops short and the Mindfragger is unhurt, still has its mind control, its target is still
    /// there to take and it can still walk to the same spot with the AP for the mind control, it carries on to that
    /// spot (at most MaxResumes times) and mind controls as planned. If it was hit on the way, or any of that fails,
    /// it stops as before and the AI decides again from where it stands.
    /// </summary>
    internal static class TFTVAIMindfraggerMove
    {
        private const int MaxResumes = 3;

        #region advance to leap range

        /// <summary>
        /// The stock Mindfragger advance (FaceHugger_Advance) scores spots by straight-line closeness to the nearest soldier
        /// (7-32 m) times x0.2 for any spot a soldier can see, so it creeps to the nearest hidden spot, however short the
        /// step, and ends its turn with AP to spare: two Mindfraggers sat 15 m out, one leap short, for two turns (playtest
        /// 2026-11-19). Its line-of-sight check is replaced by this one: a hidden spot from which a soldier is within next
        /// turn's leap counts in full, and with none, a spot in view but within leap range beats a hidden one out of it.
        /// </summary>
        private const float HiddenInReach = 1f;
        private const float SeenInReach = 0.6f;
        private const float HiddenOutOfReach = 0.5f;
        private const float SeenOutOfReach = 0.2f;

        /// <summary>Walks are longer than straight lines: next turn's reach is its full AP in metres times this, less a step.</summary>
        private const float ReachFactor = 0.85f;

        private static AIRoofAboveConsiderationDef _leapRangeDef;

        internal static void CreateDefs()
        {
            try
            {
                AIRoofAboveConsiderationDef roof = TFTVMain.Main.DefCache.GetDef<AIRoofAboveConsiderationDef>("RoofAbove_AIConsiderationDef");
                _leapRangeDef = Helper.CreateDefFromClone(roof, "4b7e2c90-1d58-4a63-b9f0-6e3a8d25c714", "TFTV_MindfraggerLeapRange_AIConsiderationDef");

                AIActionDef advance = TFTVMain.Main.DefCache.GetDef<AIActionDef>("FaceHugger_Advance_AIActionDef");
                AITargetEvaluation positions = advance?.Evaluations?.FirstOrDefault(e => e.TargetGeneratorDef is AIActorMovementZoneTargetGeneratorDef);

                if (positions?.Considerations == null)
                {
                    TFTVLogger.Always("[Mindfragger] FaceHugger_Advance_AIActionDef or its positions not found: advance left as is");
                    return;
                }

                AIAdjustedConsideration leap = new AIAdjustedConsideration
                {
                    Consideration = _leapRangeDef,
                    ScoreCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f)
                };

                positions.Considerations = positions.Considerations
                    .Where(c => !(c.Consideration is AILineOfSightToEnemiesConsiderationDef))
                    .Append(leap)
                    .ToArray();

                TFTVLogger.Always("[Mindfragger] advance aims for a hidden spot within next turn's leap of a soldier");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static bool TryScorePosition(BaseDef def, TacticalActor actor, TacAITarget target, out float score)
        {
            score = 1f;

            if (_leapRangeDef == null || def != _leapRangeDef)
            {
                return false;
            }

            try
            {
                if (actor == null || target == null)
                {
                    return true;
                }

                List<TacticalActorBase> enemies = TFTVAICombatAbilities.Stances.KnownEnemies(actor).ToList();

                if (enemies.Count == 0)
                {
                    return true;
                }

                float reach = actor.CharacterStats.ActionPoints.Max * ReachFactor - 1f;
                bool inReach = enemies.Any(e => TFTVAICombatAbilities.Stances.FlatDistance(target.Pos, e.Pos) <= reach && Mathf.Abs(target.Pos.y - e.Pos.y) < 3f);
                bool seen = TFTVAICombatAbilities.AlertState.SeenBySide(actor).Any(e => TFTVAICombatAbilities.Stances.CanSee(e, actor, target.Pos));

                score = inReach ? (seen ? SeenInReach : HiddenInReach) : (seen ? SeenOutOfReach : HiddenOutOfReach);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                score = 1f;
            }

            return true;
        }

        #endregion

        [HarmonyPatch(typeof(AIActionMoveAndMindControl), nameof(AIActionMoveAndMindControl.Execute))]
        internal static class AIActionMoveAndMindControl_Execute_Resume_Patch
        {
            public static bool Prefix(AIScoredTarget aiTarget, ref IEnumerator<NextUpdate> __result)
            {
                if (!(aiTarget?.Actor is TacticalActor) || !(aiTarget.Target is TacAITarget))
                {
                    return true;
                }

                __result = Execute(aiTarget);
                return false;
            }
        }

        /// <summary>The stock action, with the move carried on after a stop that wasn't a hit.</summary>
        private static IEnumerator<NextUpdate> Execute(AIScoredTarget aiTarget)
        {
            TacticalActor actor = (TacticalActor)aiTarget.Actor;
            TacAITarget target = (TacAITarget)aiTarget.Target;
            int resumes = 0;

            while (!actor.IsOnPosition(target.Pos, 0f))
            {
                TacticalAbility move = target.MoveAbility as TacticalAbility ?? actor.GetAbility<MoveAbility>();
                float healthBefore = actor.CharacterStats.Health;
                Vector3 from = actor.Pos;

                yield return move.ExecuteAndWait(new TacticalAbilityTarget(target.Pos));

                if (actor.IsDead)
                {
                    Debug.Log(string.Format("{0} can't mind control - died while moving to position.", actor));
                    yield break;
                }

                if (target.Actor != null && target.Actor.IsDead)
                {
                    Debug.Log(string.Format("{0} can't mind control target {1} - target died while moving to position.", actor, target.Actor));
                    yield break;
                }

                if (actor.IsOnPosition(target.Pos, 0f))
                {
                    break;
                }

                string why = WhyNotCarryOn(actor, target, resumes, healthBefore, from);

                if (why != null)
                {
                    TFTVLogger.Always($"[Mindfragger] {actor.DisplayName}'s leap at {target.Actor?.DisplayName} stopped at {actor.Pos}, " +
                        $"{Vector3.Distance(actor.Pos, target.Pos):0.0}m short: {why}; it decides again from there");
                    break;
                }

                resumes++;
                TFTVLogger.Always($"[Mindfragger] {actor.DisplayName}'s leap at {target.Actor?.DisplayName} stopped at {actor.Pos} " +
                    $"({Vector3.Distance(actor.Pos, target.Pos):0.0}m short, {(float)actor.CharacterStats.ActionPoints:0.#} AP): carries on to {target.Pos}");
            }

            MindControlAbility mindControl = actor.GetAbility<MindControlAbility>();

            if (mindControl == null)
            {
                Debug.Log(string.Format("{0} can't execute mind control ability. Ability is null. Probably the body part giving the ability got disabled", actor));
                yield break;
            }

            if (!actor.IsOnPosition(target.Pos, 0f))
            {
                Debug.Log(string.Format("Can't execute {0}. Invalid position: 'ActorPos={1} | TargetPos={2}'", mindControl.MindControlAbilityDef.name, actor.Pos, target.Pos));
                yield break;
            }

            if (!mindControl.IsEnabled(null))
            {
                Debug.Log("Can't execute " + mindControl.MindControlAbilityDef.name + ". It's not enabled");
                yield break;
            }

            TacticalAbilityTarget abilityTarget = target.ToAbilityTarget();

            if (abilityTarget.GetTargetActor().IsDead)
            {
                Debug.Log("Can't execute " + mindControl.MindControlAbilityDef.name + ". Target actor is dead.");
                yield break;
            }

            yield return mindControl.ExecuteAndWait(abilityTarget);
        }

        /// <summary>Null when the Mindfragger should carry on to its spot; otherwise why not.</summary>
        private static string WhyNotCarryOn(TacticalActor actor, TacAITarget target, int resumes, float healthBefore, Vector3 from)
        {
            try
            {
                if (resumes >= MaxResumes)
                {
                    return $"already carried on {resumes} times";
                }

                if ((float)actor.CharacterStats.Health < healthBefore)
                {
                    return "it was hit on the way";
                }

                if (resumes > 0 && (actor.Pos - from).sqrMagnitude < 0.01f)
                {
                    return "it didn't move at all";
                }

                if (actor.Status != null && (actor.Status.HasStatus<PanicStatus>() || actor.Status.HasStatus<MindControlStatus>()))
                {
                    return "it is no longer itself";
                }

                TacticalActorBase victim = target.Actor;

                if (victim == null || !victim.IsAlive || actor.RelationTo(victim) != PhoenixPoint.Common.Core.FactionRelation.Enemy)
                {
                    return "its target is no longer an enemy to take";
                }

                if (victim.Status != null && victim.Status.HasStatus<MindControlStatus>())
                {
                    return "its target is already mind controlled";
                }

                MindControlAbility mindControl = actor.GetAbility<MindControlAbility>();

                if (mindControl == null)
                {
                    return "it lost its mind control";
                }

                IMoveAbility move = target.MoveAbility ?? actor.GetAbility<MoveAbility>();
                float range = actor.GetMaxMoveAndActRange(mindControl, move);
                float walk = WalkLength(actor, target.Pos);

                if (walk < 0f)
                {
                    return "no walk to the spot any more";
                }

                if (walk > range + 0.01f)
                {
                    return $"the {walk:0.0}m left is beyond its {range:0.0}m with AP kept for the mind control";
                }

                return null;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return "error";
            }
        }

        /// <summary>The AI's walking distance to the spot, or -1 when there is no complete path.</summary>
        private static float WalkLength(TacticalActor actor, Vector3 to)
        {
            if (actor.AIActor == null || actor.TacticalLevel == null)
            {
                return -1f;
            }

            TacticalPathRequest request = actor.TacticalLevel.GetAiPathRequest(actor, actor.AIActor.MaxPathLength);
            request.Src = actor.Pos;
            request.Dst = to;
            request.UseAStar = true;
            request.DestinationRadius = 0f;
            request.Calculate();

            return request.Status == PathStatus.Complete ? request.Length : -1f;
        }
    }
}
