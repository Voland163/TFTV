using Base.AI;
using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Tactical.AI;
using PhoenixPoint.Tactical.AI.Actions;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TFTV
{
    internal class TFTVSuppressionAI
    {
        [HarmonyPatch(typeof(AIActionMoveAndAttack), nameof(AIActionMoveAndAttack.Execute))]
        public static class AIActionMoveAndAttack_Execute_Patch
        {
            private static readonly IgnoredAbilityDisabledStatesFilter AttackDisabledStateFilter = IgnoredAbilityDisabledStatesFilter.IgnoreNoValidTargetsAndEquipmentNotSelected;

            public static bool Prefix(AIActionMoveAndAttack __instance, AIScoredTarget aiTarget, ref IEnumerator<NextUpdate> __result)
            {
                if (__instance == null)
                {
                    return true;
                }

                __result = ExecuteWithFallback(__instance, aiTarget);
                return false;
            }

            private static IEnumerator<NextUpdate> ExecuteWithFallback(AIActionMoveAndAttack action, AIScoredTarget aiTarget)
            {
                TacticalActorBase tacActor = (TacticalActorBase)aiTarget.Actor;
                TacAITarget tacTarget = (TacAITarget)aiTarget.Target;

                MethodInfo methodInfoGetAttackAbility = typeof(AIActionMoveAndAttack).GetMethod("GetAttackAbility", BindingFlags.NonPublic | BindingFlags.Instance);
                MethodInfo methodInfoGetAttackTarget = typeof(AIActionMoveAndAttack).GetMethod("GetAttackTarget", BindingFlags.NonPublic | BindingFlags.Instance);

                if (!tacActor.IsOnPosition(tacTarget.Pos, 0f))
                {
                    TacticalAbility tacticalAbility = tacTarget.MoveAbility as TacticalAbility ?? tacActor.GetAbility<MoveAbility>();
                    yield return tacticalAbility.ExecuteAndWait(new TacticalAbilityTarget(tacTarget.Pos));
                    if (tacActor.IsDead)
                    {
                        Debug.Log(string.Format("{0} can't execute attack - died while moving to position.", tacActor));
                        yield break;
                    }
                }
                TacticalAbility attackAbility = (TacticalAbility)methodInfoGetAttackAbility.Invoke(action, new object[] { tacActor, tacTarget });
                if (tacActor.IsOnPosition(tacTarget.Pos, 0f) && attackAbility != null)
                {
                    TacticalActorBase actor = tacTarget.Actor;
                    if (actor != null && actor.IsDead)
                    {
                        Debug.Log(string.Format("{0} can't execute attack - target {1} died while actor was moving to position.", tacActor, tacTarget.Actor));
                        yield break;
                    }
                    TacticalAbilityTarget attackAbilityTarget = (TacticalAbilityTarget)methodInfoGetAttackTarget.Invoke(action, new object[] { attackAbility, tacTarget });
                    if (attackAbilityTarget != null)
                    {
                        yield return attackAbility.ExecuteAndWait(attackAbilityTarget);
                        TacticalActorBase actor2 = attackAbilityTarget.Actor;
                        if (actor2 != null && actor2.IsAlive)
                        {
                            tacActor.TacticalFaction.AIBlackboard.MarkActorAsHighPriorityTarget(attackAbilityTarget.Actor);
                        }
                    }
                    else
                    {
                        bool handled = false;
                        AbilityDisabledState disabledState = attackAbility.GetDisabledState(AttackDisabledStateFilter);
                        if (disabledState == AbilityDisabledState.NotEnoughActionPoints && TryFindFallbackAttack(action, tacActor, tacTarget, attackAbility, out TacticalAbility fallbackAbility, out TacticalAbilityTarget fallbackTarget))
                        {
                            handled = true;
                            Debug.Log(string.Format("{0} switching to fallback attack {1} after {2} became unavailable due to low AP.", tacActor, GetAbilityName(fallbackAbility), GetAbilityName(attackAbility)));
                            yield return fallbackAbility.ExecuteAndWait(fallbackTarget);
                            TacticalActorBase actor3 = fallbackTarget.Actor;
                            if (actor3 != null && actor3.IsAlive)
                            {
                                tacActor.TacticalFaction.AIBlackboard.MarkActorAsHighPriorityTarget(fallbackTarget.Actor);
                            }
                        }
                        if (!handled)
                        {

                           Debug.LogError(string.Format("{0} can't execute {1}. Ability target is null", tacActor, GetAbilityName(attackAbility)));
                            tacActor.ActivateAbility<EndTurnAbility>(null);
                        }
                    }
                    attackAbilityTarget = null;
                }
                else if (attackAbility == null)
                {
                    Debug.Log(string.Format("{0} can't execute attack ability. Ability is null. Probably weapon got dropped / destroyed while running during overwatch.", tacActor));
                }
                else if (!attackAbility.IsEnabled(AttackDisabledStateFilter))
                {
                    Debug.Log(string.Format("{0} can't execute {1}. Not enabled", tacActor, GetAbilityName(attackAbility)));
                }
                else if (!tacActor.IsOnPosition(tacTarget.Pos, 0f))
                {
                    Debug.Log(string.Format("{0} can't execute {1}. Invalid position: 'ActorPos={2} | TargetPos={3}'. The actor was probably interrupted during movement.", new object[]
                    {
                    tacActor,
                    GetAbilityName(attackAbility),
                    tacActor.Pos,
                    tacTarget.Pos
                    }));
                }
                yield break;
            }

            private static bool TryFindFallbackAttack(AIActionMoveAndAttack action, TacticalActorBase tacActor, TacAITarget tacTarget, TacticalAbility rejectedAbility, out TacticalAbility selectedAbility, out TacticalAbilityTarget selectedTarget)
            {

                MethodInfo methodInfoGetAttackTarget = typeof(AIActionMoveAndAttack).GetMethod("GetAttackTarget", BindingFlags.NonPublic | BindingFlags.Instance);

                selectedAbility = null;
                selectedTarget = null;
                IEnumerable<TacticalAbility> abilities = tacActor.GetAbilities<TacticalAbility>();
                IEnumerable<TacticalAbility> enumerable = abilities.Where((TacticalAbility ability) => ability != null && ability != rejectedAbility && (ability is ShootAbility || ability is BashAbility));
                foreach (TacticalAbility ability2 in enumerable.OrderBy((TacticalAbility ability) => ability.ActionPointCost))
                {
                    if (!ability2.IsEnabled(AttackDisabledStateFilter))
                    {
                        continue;
                    }
                    TacticalAbilityTarget attackTarget = (TacticalAbilityTarget)methodInfoGetAttackTarget.Invoke(action, new object[] { ability2, tacTarget });
                    if (attackTarget != null)
                    {
                        selectedAbility = ability2;
                        selectedTarget = attackTarget;
                        return true;
                    }
                }
                return false;
            }

            private static string GetAbilityName(TacticalAbility ability)
            {
                if (ability == null)
                {
                    return "<null>";
                }
                return ability.AbilityDef != null ? ability.AbilityDef.name : ability.GetType().Name;
            }
        }
    }
}
