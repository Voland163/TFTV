using Base;
using Base.AI;
using Base.Utils.Maths;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Tactical.AI;
using PhoenixPoint.Tactical.AI.Considerations;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Statuses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TFTV.TFTVVanillaFixes.Tactical
{
    internal class AITacticalVanillaFixes
    {
        [HarmonyPatch(typeof(AIAttackPositionConsideration), "EvaluateWithAbility")] //VERIFIED
        public static class AIAttackPositionConsideration_EvaluateWithAbilityPatch
        {
            public static bool Prefix(AIAttackPositionConsideration __instance, IAIActor actor, IAITarget target, TacticalAbilityDef abilityDef, ref float __result)
            {
                try
                {

                    MethodInfo getDamagePayloadMethodInfo = typeof(AIAttackPositionConsideration).GetMethod("GetPayloadMaxDamage", BindingFlags.NonPublic | BindingFlags.Instance);

                    //TFTVLogger.Always($"getDamagePayloadMethodInfo null {getDamagePayloadMethodInfo==null}");

                    TacticalActor tacActor = (TacticalActor)actor;
                    TacAITarget tacAITarget = (TacAITarget)target;
                    float eps = 0.01f;
                    if (abilityDef == null)
                    {
                        __result = 0f;
                        return false;
                    }

                    TacticalAbility abilityWithDef = tacActor.GetAbilityWithDef<TacticalAbility>(abilityDef);
                    if (abilityWithDef == null)
                    {
                        __result = 0f;
                        return false;
                    }

                    if (!abilityWithDef.IsEnabled(IgnoredAbilityDisabledStatesFilter.IgnoreNoValidTargetsAndEquipmentNotSelected))
                    {
                        __result = 0f;
                        return false;
                    }

                    float maxMoveAndActRange = tacActor.GetMaxMoveAndActRange(abilityWithDef, tacAITarget.MoveAbility);
                    if (Utl.GreaterThan(tacAITarget.PathLength, maxMoveAndActRange, eps))
                    {
                        __result = 0f;
                        return false;
                    }

                    DamagePayload damagePayload = (abilityWithDef as IDamageDealer)?.GetDamagePayload();
                    if (damagePayload == null)
                    {
                        __result = 0f;
                        return false;
                    }

                    IEnumerable<TacticalActorBase> enemies = from a in tacActor.TacticalFaction.AIBlackboard.GetEnemies(tacActor.AIActor.GetEnemyMask(__instance.Def.EnemyMask), checkKnowledge: false)
                                                             where tacActor.TacticalFaction.Vision.IsRevealed(a)
                                                             select a;
                    IEnumerable<TacticalAbilityTarget> enumerable = abilityWithDef.GetTargetsAt(tacAITarget.Pos);
                    if (!__instance.Def.InclideAlliesAsTargets)
                    {
                        enumerable = enumerable.Where((TacticalAbilityTarget x) => enemies.Contains(x.Actor));
                    }

                    List<TacticalActorBase> list = new List<TacticalActorBase>(10);
                    float num = 0f;
                    foreach (TacticalAbilityTarget item in enumerable)
                    {
                        float num2 = 1f;
                        list.Clear();
                        if (abilityWithDef.OriginTargetData.TargetSelf)
                        {
                            list.AddRange(AIUtil.GetAffectedTargetsByDamageAbility(tacActor, tacAITarget.Pos, abilityWithDef as IDamageDealer));
                        }
                        else
                        {
                            list.AddRange(AIUtil.GetAffectedTargetsByDamageAbility(tacActor, item.Actor.Pos, abilityWithDef as IDamageDealer));
                        }

                        list.RemoveWhere(t => tacActor.RelationTo(t) == FactionRelation.Enemy && !tacActor.TacticalFaction.Vision.IsLocated(t) && !tacActor.TacticalFaction.Vision.IsRevealed(t));

                        int num3 = 0;
                        foreach (TacticalActorBase item2 in list)
                        {
                            if (tacActor.RelationTo(item2) == FactionRelation.Friend)
                            {
                                if ((__instance.Def.IgnoreDamageOnSelf && item2 as TacticalActor != tacActor) || !__instance.Def.IgnoreDamageOnSelf)
                                {
                                    num2 *= __instance.Def.FriendlyHitScoreMultiplier;
                                }
                            }
                            else if (tacActor.RelationTo(item2) == FactionRelation.Neutral)
                            {
                                num2 *= __instance.Def.NeutralHitScoreMultiplier;
                            }

                            if (tacActor.RelationTo(item2) == FactionRelation.Enemy)
                            {
                                if (item2.Status != null && item2.Status.HasStatus<MindControlStatus>() && item2.Status.GetStatus<MindControlStatus>().OriginalFaction == tacActor.TacticalFaction.TacticalFactionDef)
                                {
                                }
                                else
                                {
                                    num3++;
                                }


                            }
                        }

                        if (num2 < Mathf.Epsilon || num3 == 0)
                        {
                            continue;
                        }

                        object[] parameters = new object[] { tacAITarget.Pos, damagePayload, list.Where((TacticalActorBase ac) => tacActor.RelationTo(ac) == FactionRelation.Enemy), null };

                        float payloadMaxDamage = (float)getDamagePayloadMethodInfo.Invoke(__instance, parameters);
                        if (!(payloadMaxDamage < 10f))
                        {
                            float num4 = payloadMaxDamage.ClampHigh(__instance.Def.MaxDamage);
                            num2 *= num4 / __instance.Def.MaxDamage;
                            num2 *= AIUtil.GetEnemyWeight(tacActor.TacticalFaction.AIBlackboard, item.Actor);
                            num2 = Mathf.Clamp(num2, 0f, 1f);
                            if (num < num2)
                            {
                                tacAITarget.Actor = item.Actor;
                                num = num2;
                            }
                        }
                    }

                    __result = num;
                    return false;



                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }




    }
}
